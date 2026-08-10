using System.Security.Cryptography;
using System.Text;
using Tuilow.Finance.Application.Interfaces;
using Tuilow.Finance.Domain.Interfaces;

namespace Tuilow.Finance.Infrastructure.Services;

public sealed class AsaasAccountStatusWebhookAuthenticator(
    ICreatorAsaasSubaccountRepository repository
) : IAsaasAccountStatusWebhookAuthenticator
{
    public async Task<AsaasAccountStatusWebhookAuthResult> AuthenticateAsync(string accessToken, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(accessToken))
            return new AsaasAccountStatusWebhookAuthResult(false);

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(accessToken)));
        var subaccount = await repository.GetByWebhookTokenHashAsync(hash, ct);
        return new AsaasAccountStatusWebhookAuthResult(subaccount is not null);
    }
}
