using Tuilow.SharedKernel.Application.Interfaces;
using Tuilow.Finance.Application.Interfaces;
using Tuilow.Finance.Domain.Enums;
using Tuilow.Finance.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Tuilow.Finance.Application.Commands.SyncCreatorOnboardingAccountStatus;

/// <summary>Ver racional completo em SyncCreatorOnboardingAccountStatusCommand.</summary>
public sealed class SyncCreatorOnboardingAccountStatusCommandHandler(
    ICreatorAsaasSubaccountRepository repository,
    IAsaasSubaccountClient asaasSubaccountClient,
    ISecretProtector secretProtector,
    IUnitOfWork uow,
    ILogger<SyncCreatorOnboardingAccountStatusCommandHandler> logger
) : IRequestHandler<SyncCreatorOnboardingAccountStatusCommand>
{
    // A tela de status é repolled com frequência pelo frontend (ver GetMyFinancialOnboardingStatusQuery)
    // -- sem throttle, cada poll bateria na Asaas à toa.
    private static readonly TimeSpan MinSyncInterval = TimeSpan.FromSeconds(20);

    // Só faz sentido reconsultar enquanto a subconta existe de fato e ainda está esperando um
    // veredito -- nunca em NotStarted/CollectingData/AccountCreationPending (ainda não tem
    // AsaasAccountId) nem em Approved/Rejected/Blocked (já é estado final/administrativo).
    private static readonly HashSet<CreatorOnboardingStatus> SyncableStatuses =
    [
        CreatorOnboardingStatus.AccountCreated,
        CreatorOnboardingStatus.DocumentsPending,
        CreatorOnboardingStatus.UnderReview
    ];

    public async Task Handle(SyncCreatorOnboardingAccountStatusCommand request, CancellationToken ct)
    {
        var subaccount = await repository.GetByCreatorIdAsync(request.CreatorId, ct);
        if (subaccount is null || subaccount.AsaasAccountId is null || subaccount.ApiKeyEncrypted is null)
            return;

        if (!SyncableStatuses.Contains(subaccount.Status))
            return;

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

        var statusInfo = await asaasSubaccountClient.GetAccountStatusAsync(apiKey, ct);

        // Mesmo em falha (statusInfo null -- já logada dentro do client, ver
        // AsaasSubaccountClient.GetAccountStatusAsync) registramos a tentativa via
        // ApplyAccountStatusSync(null): sem isso, uma Asaas fora do ar faria cada poll da tela de
        // status tentar de novo sem nenhum throttle, na prática martelando a Asaas em vez de só
        // este endpoint específico.
        subaccount.ApplyAccountStatusSync(statusInfo?.GeneralStatus);
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
}
