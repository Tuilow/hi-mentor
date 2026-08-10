namespace Tuilow.Finance.Application.Interfaces;

public sealed record AsaasAccountStatusWebhookAuthResult(bool IsValid);

/// <summary>
/// Autentica o webhook de status de conta (ACCOUNT_STATUS_*) comparando o header
/// "asaas-access-token" contra o hash (SHA-256) do token registrado em alguma
/// CreatorAsaasSubaccount -- mesmo idioma de IAsaasWebhookAuthenticator (Sales), mantido como uma
/// implementação própria e separada porque autentica contra um agregado diferente (
/// CreatorAsaasSubaccount, não CreatorAsaasAccount/legado) -- este repositório segue a regra de
/// não criar acoplamento Application-a-Application entre módulos só para reaproveitar ~10 linhas.
/// </summary>
public interface IAsaasAccountStatusWebhookAuthenticator
{
    Task<AsaasAccountStatusWebhookAuthResult> AuthenticateAsync(string accessToken, CancellationToken ct = default);
}
