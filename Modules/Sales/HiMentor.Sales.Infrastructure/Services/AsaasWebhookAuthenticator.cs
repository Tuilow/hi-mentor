using System.Security.Cryptography;
using System.Text;
using HiMentor.Sales.Application.Interfaces;
using HiMentor.Finance.Domain.Interfaces;

namespace HiMentor.Sales.Infrastructure.Services;

/// <summary>
/// Tenta autenticar primeiro contra o secret global legado (conta da própria HiMentor — mais
/// barato, sem consulta ao banco). Se não bater, tenta o hash do token contra qualquer
/// CreatorAsaasAccount conectada (modelo legado, "cole sua API Key" — ainda existe para
/// creators não migrados). Se também não bater, tenta o hash contra CreatorAsaasSubaccount (novo
/// modelo de subconta BaaS — ver StartCreatorFinancialOnboardingCommandHandler/
/// SyncCreatorOnboardingAccountStatusCommandHandler, que registram/rotacionam esse webhook).
/// Nunca decripta nenhuma API Key para autenticar, só compara hash SHA-256.
///
/// Corrigido em ago/2026: antes desta terceira tentativa, um webhook de pagamento de uma compra
/// no marketplace de um creator já migrado para o novo modelo nunca autenticava (a subconta não
/// existe em CreatorAsaasAccount), e a compra ficava presa em "Pending" para sempre.
/// </summary>
public sealed class AsaasWebhookAuthenticator(
    IPaymentService legacyPaymentService,
    ICreatorAsaasAccountRepository creatorAsaasAccountRepository,
    ICreatorAsaasSubaccountRepository creatorAsaasSubaccountRepository
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
        if (account is not null)
        {
            // Observabilidade: marca a última vez que recebemos um webhook desta conta (painel
            // admin usa isso para diagnosticar "webhook parou de chegar"). O SaveChanges real
            // acontece mais adiante no mesmo escopo de requisição, dentro de
            // ProcessAsaasWebhookCommandHandler (mesmo DbContext, escopo por requisição).
            account.RecordWebhookReceived();
            creatorAsaasAccountRepository.Update(account);

            return new AsaasWebhookAuthResult(true, true, account.Id);
        }

        var subaccount = await creatorAsaasSubaccountRepository.GetByWebhookTokenHashAsync(hash, ct);
        if (subaccount is null)
            return new AsaasWebhookAuthResult(false, false, null);

        return new AsaasWebhookAuthResult(true, true, subaccount.Id);
    }
}
