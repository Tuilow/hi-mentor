using System.Security.Cryptography;
using System.Text;
using Tuilow.SharedKernel.Application.Interfaces;
using Tuilow.Finance.Application.Interfaces;
using Tuilow.Finance.Domain.Entities;
using Tuilow.Finance.Domain.Enums;
using Tuilow.Finance.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Tuilow.Finance.Application.Commands.SyncCreatorOnboardingAccountStatus;

/// <summary>
/// Ver racional completo em SyncCreatorOnboardingAccountStatusCommand. Duas redes de segurança
/// independentes, disparadas pelo mesmo poll da tela de status e compartilhando o mesmo throttle:
///   1. Veredito GERAL de aprovação (ApplyAccountStatusSync) -- roda enquanto o status ainda não
///      é Approved/Rejected/Blocked.
///   2. Registro RETROATIVO do webhook de PAGAMENTO (PaymentWebhookRegisteredAt) -- roda até ter
///      sucesso uma vez, independente do status (inclusive já Approved): subcontas criadas antes
///      desta proteção existir (ver StartCreatorFinancialOnboardingCommandHandler) nunca tiveram
///      esse webhook registrado, e sem ele nenhuma compra no marketplace deste criador recebe
///      confirmação de volta da Asaas -- é exatamente o bug que motivou esta rede de segurança.
/// </summary>
public sealed class SyncCreatorOnboardingAccountStatusCommandHandler(
    ICreatorAsaasSubaccountRepository repository,
    IAsaasSubaccountClient asaasSubaccountClient,
    ISecretProtector secretProtector,
    IUnitOfWork uow,
    ILogger<SyncCreatorOnboardingAccountStatusCommandHandler> logger
) : IRequestHandler<SyncCreatorOnboardingAccountStatusCommand>
{
    // A tela de status é repolled com frequência pelo frontend (ver GetMyFinancialOnboardingStatusQuery)
    // -- sem throttle, cada poll bateria na Asaas à toa. Um único throttle cobre as duas operações
    // abaixo (status geral e webhook de pagamento), nunca dois relógios separados.
    private static readonly TimeSpan MinSyncInterval = TimeSpan.FromSeconds(20);

    public async Task Handle(SyncCreatorOnboardingAccountStatusCommand request, CancellationToken ct)
    {
        var subaccount = await repository.GetByCreatorIdAsync(request.CreatorId, ct);
        if (subaccount is null || subaccount.AsaasAccountId is null || subaccount.ApiKeyEncrypted is null)
            return;

        // Rejected/Blocked são estados finais/administrativos -- nada a reconciliar, nem veredito
        // geral (não vai voltar a vender por essa via) nem webhook de pagamento (não deveria
        // conseguir criar cobrança nenhuma enquanto não sair desse estado).
        if (subaccount.Status is CreatorOnboardingStatus.Rejected or CreatorOnboardingStatus.Blocked)
            return;

        var needsStatusRefresh = subaccount.Status != CreatorOnboardingStatus.Approved;
        var needsPaymentWebhook = subaccount.PaymentWebhookRegisteredAt is null;
        if (!needsStatusRefresh && !needsPaymentWebhook)
            return; // já Approved e com o webhook de pagamento confirmado -- nada mais a fazer aqui

        if (subaccount.LastAccountStatusSyncedAt is { } last && DateTime.UtcNow - last < MinSyncInterval)
            return;

        string apiKey;
        try
        {
            apiKey = secretProtector.Unprotect(subaccount.ApiKeyEncrypted);
        }
        catch (Exception ex)
        {
            // Nunca deveria acontecer com uma chave gravada por MarkAccountCreated -- se acontecer,
            // é um bug genuíno (ex.: rotação de chaves do Data Protection), vale logar alto, mas
            // ainda não deve derrubar a tela de status (é só um refresh best-effort).
            logger.LogError(ex, "Falha ao descriptografar a API Key da subconta do criador {CreatorId} para refresh de status.", request.CreatorId);
            return;
        }

        // Mesmo em falha (statusInfo null -- já logada dentro do client, ver
        // AsaasSubaccountClient.GetAccountStatusAsync) ou quando pulado por já estar Approved
        // (generalStatus null), registramos a tentativa via ApplyAccountStatusSync(null): sem
        // isso, o throttle acima nunca avança e cada poll tentaria de novo sem limite.
        var generalStatus = needsStatusRefresh
            ? (await asaasSubaccountClient.GetAccountStatusAsync(apiKey, ct))?.GeneralStatus
            : null;
        subaccount.ApplyAccountStatusSync(generalStatus);

        if (needsPaymentWebhook)
            await TryEnsurePaymentWebhookRegisteredAsync(subaccount, apiKey, ct);

        repository.Update(subaccount);

        if (!await uow.TrySaveChangesAsync(ct))
        {
            // Corrida com outra gravação concorrente na mesma subconta (ex.: o webhook chegando
            // bem nesse instante, ou duas abas do criador pollando ao mesmo tempo) -- inofensivo
            // aqui: é um refresh best-effort, não uma operação crítica. Nunca vale tentar de novo
            // dentro do mesmo request por uma leitura que já vai ser reconciliada no próximo poll
            // throttlado; propagar isso derrubaria a tela de status por causa de uma corrida
            // completamente benigna.
            logger.LogInformation(
                "Refresh de status de conta do criador {CreatorId} colidiu com uma gravação concorrente -- ignorado, reconciliado no próximo poll.",
                request.CreatorId);
        }
    }

    /// <summary>
    /// Subcontas criadas antes desta proteção existir nunca tiveram o webhook de PAGAMENTO
    /// registrado (só o de status de conta, na criação -- ver
    /// StartCreatorFinancialOnboardingCommandHandler) -- sem ele, uma compra no marketplace deste
    /// criador nunca recebe confirmação de volta da Asaas e fica presa em "Pending" para sempre
    /// (foi exatamente esse bug, reportado em produção, que motivou esta rede de segurança). O
    /// token original em texto puro não existe mais em lugar nenhum (só o hash é persistido, por
    /// design de segurança) -- por isso ROTACIONAMOS para um token novo e reafirmamos os DOIS
    /// webhooks (status + pagamento) com ele, nunca só o de pagamento isoladamente: isso deixaria
    /// os dois autenticando com tokens diferentes do hash salvo.
    /// </summary>
    private async Task TryEnsurePaymentWebhookRegisteredAsync(CreatorAsaasSubaccount subaccount, string apiKey, CancellationToken ct)
    {
        var newToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var newHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(newToken)));

        var accountStatusOk = await asaasSubaccountClient.RegisterAccountStatusWebhookAsync(apiKey, newToken, ct);
        if (!accountStatusOk)
        {
            logger.LogWarning(
                "Falha ao rotacionar o webhook de status de conta do criador {CreatorId} -- webhook de pagamento não registrado nesta rodada (evita token dessincronizado).",
                subaccount.CreatorId);
            return;
        }

        // A Asaas já está usando o token novo para o webhook de status a partir daqui -- o hash
        // salvo PRECISA acompanhar isso, independente do resultado do webhook de pagamento
        // abaixo, senão o webhook de status (que até então funcionava) passa a ser rejeitado por
        // hash desatualizado.
        subaccount.RotateWebhookToken(newHash);

        if (await asaasSubaccountClient.RegisterPaymentWebhookAsync(apiKey, newToken, ct))
        {
            subaccount.MarkPaymentWebhookRegistered();
        }
        else
        {
            logger.LogWarning(
                "Falha ao registrar o webhook de pagamento da subconta do criador {CreatorId} -- tentará de novo no próximo poll (o token já foi rotacionado com sucesso para o webhook de status).",
                subaccount.CreatorId);
        }
    }
}
