using MediatR;

namespace HiMentor.Sales.Application.Commands.ProcessWebhook;

public sealed record AsaasWebhookPayload(
    string Event,
    AsaasPaymentData Payment
);

public sealed record AsaasPaymentData(
    string Id,
    string? Subscription,
    decimal Value,
    string Status,
    /// <summary>
    /// Valor liquido informado pela Asaas (bruto menos as taxas de meio de pagamento da propria
    /// Asaas) -- usado so para conciliacao em vendas MarketplaceSplit (CoursePurchase.RecordAsaasNetValue).
    /// Nulo quando a Asaas nao inclui o campo no payload (nem todo evento/metodo de pagamento traz).
    /// </summary>
    decimal? NetValue = null
);

/// <summary>
/// CreatorAsaasAccountId vem preenchido pelo controller quando o webhook foi autenticado contra
/// o token de uma conta de marketplace especifica (ver IAsaasWebhookAuthenticator) -- nulo para
/// webhooks da conta legada da propria HiMentor. Usado como checagem de seguranca extra: uma
/// compra MarketplaceSplit so pode ser confirmada por um webhook autenticado com o token da
/// MESMA CreatorAsaasAccount gravada nela (ver ProcessAsaasWebhookCommandHandler).
/// </summary>
public sealed record ProcessAsaasWebhookCommand(AsaasWebhookPayload Payload, Guid? CreatorAsaasAccountId = null) : IRequest;
