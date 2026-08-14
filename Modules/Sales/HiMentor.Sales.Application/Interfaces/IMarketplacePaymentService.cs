namespace HiMentor.Sales.Application.Interfaces;

public sealed record MarketplaceCustomerRequest(string Name, string Email, string? CpfCnpj, string? Phone);
public sealed record MarketplaceCustomerResponse(string AsaasCustomerId);
public sealed record MarketplaceChargeRequest(string AsaasCustomerId, decimal Value, string Description, string? ExternalReference);
public sealed record MarketplaceChargeResponse(string AsaasPaymentId, string Status, string? InvoiceUrl);

/// <summary>
/// Cria clientes/cobrancas DIRETAMENTE na conta Asaas de um creator especifico (ele e o
/// emissor/vendedor da cobranca) -- nao na conta da propria HiMentor. Usado apenas quando o
/// creator tem uma CreatorAsaasAccount ativa (marketplace de split de pagamentos). A
/// implementacao (Sales.Infrastructure) resolve as credenciais do creator (API Key decriptada
/// via ISecretProtector, walletId) e monta o split de comissao apontando para a walletId da
/// HiMentor -- o caller (Application) so passa creatorId, nunca ve a API Key em nenhum momento.
///
/// Contrato deliberadamente separado de IPaymentService (que continua servindo o modelo Legacy,
/// cobranca na conta da propria HiMentor) -- evita qualquer risco de um caminho de codigo
/// acidentalmente usar a credencial errada.
/// </summary>
public interface IMarketplacePaymentService
{
    /// <summary>studentId identifica o aluno na nossa base -- usado para reaproveitar o AsaasCustomerId ja mapeado (CreatorAsaasCustomer) numa segunda compra do mesmo aluno com o mesmo creator.</summary>
    Task<MarketplaceCustomerResponse> CreateOrGetCustomerAsync(
        Guid creatorId, Guid studentId, MarketplaceCustomerRequest request, CancellationToken ct = default);

    /// <summary>
    /// commissionPercentage define o split (percentualValue) enviado a Asaas -- deve ser
    /// exatamente o mesmo valor gravado como snapshot em CoursePurchase.CommissionPercentageSnapshot,
    /// nunca recalculado aqui.
    /// </summary>
    Task<MarketplaceChargeResponse> CreateChargeAsync(
        Guid creatorId, MarketplaceChargeRequest request, decimal commissionPercentage, CancellationToken ct = default);
}
