using System.Security.Cryptography;
using System.Text;
using Tuilow.SharedKernel.Application.Interfaces;
using Tuilow.Finance.Application.Interfaces;
using Tuilow.Finance.Domain.Entities;
using Tuilow.Finance.Domain.Interfaces;
using MediatR;

namespace Tuilow.Finance.Application.Commands.ConnectCreatorAsaasAccount;

public sealed class ConnectCreatorAsaasAccountCommandHandler(
    ICreatorAsaasAccountRepository repository,
    IAsaasAccountOnboardingService onboardingService,
    ISecretProtector secretProtector,
    IUnitOfWork uow
) : IRequestHandler<ConnectCreatorAsaasAccountCommand, ConnectCreatorAsaasAccountResult>
{
    public async Task<ConnectCreatorAsaasAccountResult> Handle(ConnectCreatorAsaasAccountCommand request, CancellationToken ct)
    {
        var account = await repository.GetByCreatorIdAsync(request.CreatorId, ct);
        var isNew = account is null;
        account ??= CreatorAsaasAccount.CreateConnecting(request.CreatorId, request.CpfCnpj, request.LegalName);

        var validation = await onboardingService.ValidateAndFetchAccountAsync(request.ApiKey, ct);
        if (!validation.Success)
        {
            account.MarkValidationFailed(validation.ErrorMessage ?? "Não foi possível validar a API Key informada.");
            await PersistAsync(account, isNew, ct);
            return new ConnectCreatorAsaasAccountResult(false, account.Status.ToString(), account.LastValidationError);
        }

        // Token de webhook novo a cada conexão/reconexão -- alta entropia, nunca reaproveitado.
        // Só o HASH (SHA-256) fica no nosso banco (CreatorAsaasAccount.WebhookTokenHash); o
        // valor em si só existe em memória aqui e dentro da própria conta Asaas do creator (é
        // ela quem vai ecoar de volta no header do webhook, exatamente como no modelo legado —
        // ver AsaasPaymentService.ValidateWebhookSignature).
        var webhookToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var webhookRegistered = await onboardingService.RegisterWebhookAsync(request.ApiKey, webhookToken, ct);
        if (!webhookRegistered)
        {
            account.MarkValidationFailed(
                "A API Key foi validada, mas não foi possível registrar o webhook de pagamentos na sua conta Asaas. Tente novamente.");
            await PersistAsync(account, isNew, ct);
            return new ConnectCreatorAsaasAccountResult(false, account.Status.ToString(), account.LastValidationError);
        }

        var apiKeyEncrypted = secretProtector.Protect(request.ApiKey);
        var webhookTokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(webhookToken)));

        account.MarkValidated(validation.AsaasAccountId!, validation.WalletId!, apiKeyEncrypted, webhookTokenHash);
        await PersistAsync(account, isNew, ct);

        return new ConnectCreatorAsaasAccountResult(true, account.Status.ToString(), null);
    }

    private async Task PersistAsync(CreatorAsaasAccount account, bool isNew, CancellationToken ct)
    {
        if (isNew) await repository.AddAsync(account, ct);
        else repository.Update(account);
        await uow.SaveChangesAsync(ct);
    }
}
