using System.Security.Cryptography;
using System.Text;
using Tuilow.Sales.Application.Interfaces;
using Tuilow.Finance.Domain.Interfaces;

namespace Tuilow.Sales.Infrastructure.Services;

/// <summary>
/// Tenta autenticar primeiro contra o secret global legado (conta da própria Tuilow — mais
/// barato, sem consulta ao banco). Se não bater, tenta o hash do token contra qualquer
/// CreatorAsaasAccount conectada (marketplace de split) — nunca decripta nenhuma API Key para
/// isso, só compara hash SHA-256 (ver CreatorAsaasAccount.WebhookTokenHash).
/// </summary>
public sealed class AsaasWebhookAuthenticator(
    IPaymentService legacyPaymentService,
    ICreatorAsaasAccountRepository creatorAsaasAccountRepository
) : IAsaasWebhookAuthenticator
{
    public async Task<AsaasWebhookAuthResult> AuthenticateAsync(string accessToken, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(accessToken))
            return new AsaasWebhookAuthResult(false, false, null);

        if (legacyPaymentService.ValidateWebhookSignature(accessToken))
            return new AsaasWebhookAuthResult(true, false, null);

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(accessToken)));
        var account = await creatorAsaasAccountRepository.GetByWebhookTokenHashAsync(hash, ct);
        if (account is null)
            return new AsaasWebhookAuthResult(false, false, null);

        // Observabilidade: marca a última vez que recebemos um webhook desta conta (painel
        // admin usa isso para diagnosticar "webhook parou de chegar"). O SaveChanges real
        // acontece mais adiante no mesmo escopo de requisição, dentro de
        // ProcessAsaasWebhookCommandHandler (mesmo DbContext, escopo por requisição).
        account.RecordWebhookReceived();
        creatorAsaasAccountRepository.Update(account);

        return new AsaasWebhookAuthResult(true, true, account.Id);
    }
}
