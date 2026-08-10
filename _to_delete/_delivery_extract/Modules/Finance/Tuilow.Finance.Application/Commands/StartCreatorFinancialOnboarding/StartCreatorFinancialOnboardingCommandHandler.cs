using System.Security.Cryptography;
using System.Text;
using Tuilow.SharedKernel.Application.Interfaces;
using Tuilow.Finance.Application.Interfaces;
using Tuilow.Finance.Domain.Entities;
using Tuilow.Finance.Domain.Enums;
using Tuilow.Finance.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Tuilow.Finance.Application.Commands.StartCreatorFinancialOnboarding;

/// <summary>
/// Idempotência (item 15 do briefing) e recuperação de falha (item 16) na prática:
///   1. Busca a subconta do criador; cria uma nova em memória só se não existir.
///   2. Grava os dados cadastrais (StartCollectingData) e salva — isto sozinho NUNCA chama a
///      Asaas, então clique duplo/refresh do browser nesta fase é inofensivo (upsert idempotente
///      pelos dados, sem efeito colateral externo).
///   3. Se a subconta JÁ tem AsaasAccountId (de uma execução anterior bem-sucedida), a criação é
///      pulada por completo — devolve o status atual sem tocar a Asaas de novo.
///   4. Antes de chamar a Asaas, persiste MarkAccountCreationPending() sozinho (SaveChanges
///      isolado) — se o processo cair logo depois (durante a chamada HTTP), o próximo
///      GetMyFinancialOnboardingStatusQuery já mostra "processando" em vez de nada, e uma
///      segunda tentativa deste mesmo comando encontra Status == AccountCreationPending e tenta
///      de novo (ainda não tem AsaasAccountId, então ainda é seguro chamar CreateSubaccountAsync
///      -- ver nota de runbook no relatório final sobre o caso raro em que a Asaas JÁ criou a
///      conta mas a resposta nunca chegou ao Tuilow: recuperação nesse caso é manual, via suporte
///      Asaas consultando por cpfCnpj).
///   5. A resposta da Asaas (accountId/walletId/apiKey) é persistida imediatamente após o 2xx,
///      antes de qualquer outra chamada (inclusive o registro do webhook) — para minimizar a
///      janela em que a apiKey só existe em memória.
/// </summary>
public sealed class StartCreatorFinancialOnboardingCommandHandler(
    ICreatorAsaasSubaccountRepository repository,
    IAsaasSubaccountClient asaasSubaccountClient,
    ISecretProtector secretProtector,
    IUnitOfWork uow,
    ILogger<StartCreatorFinancialOnboardingCommandHandler> logger
) : IRequestHandler<StartCreatorFinancialOnboardingCommand, StartCreatorFinancialOnboardingResult>
{
    public async Task<StartCreatorFinancialOnboardingResult> Handle(StartCreatorFinancialOnboardingCommand request, CancellationToken ct)
    {
        var subaccount = await repository.GetByCreatorIdAsync(request.CreatorId, ct);
        var isNew = subaccount is null;
        subaccount ??= CreatorAsaasSubaccount.Start(request.CreatorId);

        // Já criada de verdade na Asaas — não repete a criação, só devolve o status atual
        // (idempotência: reenvio do formulário / clique duplo depois de já ter sido criada).
        if (subaccount.AsaasAccountId is not null)
        {
            if (isNew) await repository.AddAsync(subaccount, ct); // defensivo, nunca deveria acontecer
            return new StartCreatorFinancialOnboardingResult(true, subaccount.Status.ToString(), null);
        }

        subaccount.StartCollectingData(
            request.LegalName, request.CpfCnpj, request.BirthDate, request.CompanyType,
            request.Email, request.MobilePhone, request.Phone, request.IncomeValue,
            request.Address, request.AddressNumber, request.AddressComplement, request.Province, request.PostalCode);

        await PersistAsync(subaccount, isNew, ct);

        // Passo 4 do racional acima: persistido ANTES da chamada à Asaas.
        subaccount.MarkAccountCreationPending();
        repository.Update(subaccount);
        await uow.SaveChangesAsync(ct);

        var creation = await asaasSubaccountClient.CreateSubaccountAsync(new CreateAsaasSubaccountRequest(
            request.LegalName, request.Email, request.CpfCnpj, request.MobilePhone, request.Phone,
            request.IncomeValue, request.Address, request.AddressNumber, request.AddressComplement,
            request.Province, request.PostalCode, request.BirthDate, request.CompanyType), ct);

        if (!creation.Success)
        {
            subaccount.MarkAccountCreationFailed(creation.ErrorMessage ?? "Não foi possível criar sua conta financeira. Tente novamente.");
            repository.Update(subaccount);
            await uow.SaveChangesAsync(ct);
            return new StartCreatorFinancialOnboardingResult(false, subaccount.Status.ToString(), subaccount.RejectionReason);
        }

        // Token de webhook novo — só o HASH (SHA-256) fica no nosso banco (mesmo idioma de
        // ConnectCreatorAsaasAccountCommandHandler no modelo legado).
        var webhookToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var webhookTokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(webhookToken)));
        var apiKeyEncrypted = secretProtector.Protect(creation.ApiKey!);

        // Persiste IMEDIATAMENTE — antes de tentar registrar o webhook — para minimizar a janela
        // em que a apiKey da subconta só existe em memória (ver racional da classe).
        subaccount.MarkAccountCreated(creation.AsaasAccountId!, creation.WalletId ?? string.Empty, apiKeyEncrypted, webhookTokenHash);
        repository.Update(subaccount);
        await uow.SaveChangesAsync(ct);

        var webhookRegistered = await asaasSubaccountClient.RegisterAccountStatusWebhookAsync(creation.ApiKey!, webhookToken, ct);
        if (!webhookRegistered)
        {
            // A subconta já existe de verdade na Asaas — não há como "desfazer" isso, e não
            // devemos tentar criar outra. Loga como crítico para acompanhamento manual (o admin
            // pode reprocessar via SyncCreatorOnboardingDocumentsCommand, que também tenta
            // reafirmar o webhook — ver handler correspondente) e segue o onboarding normalmente
            // (o criador não deve ficar travado por causa disso).
            logger.LogCritical(
                "Subconta Asaas {AccountId} criada para o criador {CreatorId}, mas o registro do webhook de status falhou — eventos ACCOUNT_STATUS_* não serão recebidos até isso ser corrigido.",
                creation.AsaasAccountId, request.CreatorId);
        }

        return new StartCreatorFinancialOnboardingResult(true, subaccount.Status.ToString(), null);
    }

    private async Task PersistAsync(CreatorAsaasSubaccount subaccount, bool isNew, CancellationToken ct)
    {
        if (isNew) await repository.AddAsync(subaccount, ct);
        else repository.Update(subaccount);
        await uow.SaveChangesAsync(ct);
    }
}
