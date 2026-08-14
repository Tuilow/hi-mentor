namespace HiMentor.Sales.Application.Interfaces;

/// <summary>
/// Autentica um webhook recebido em /api/v1/webhooks/asaas contra QUALQUER credencial valida:
/// o secret global legado (Asaas:WebhookSecret -- conta da propria HiMentor, assinaturas e compras
/// Legacy) OU o token de webhook de uma CreatorAsaasAccount especifica (marketplace de split --
/// cada creator tem o seu proprio, ver ConnectCreatorAsaasAccountCommandHandler). Isola o
/// controller de precisar conhecer os dois modelos.
/// </summary>
public sealed record AsaasWebhookAuthResult(bool IsValid, bool IsMarketplace, Guid? CreatorAsaasAccountId);

public interface IAsaasWebhookAuthenticator
{
    Task<AsaasWebhookAuthResult> AuthenticateAsync(string accessToken, CancellationToken ct = default);
}
