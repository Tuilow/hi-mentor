using Tuilow.Sales.Domain.Enums;

namespace Tuilow.Sales.Application.Interfaces;

public record AsaasCustomerRequest(string Name, string Email, string? CpfCnpj, string? Phone);
public record AsaasSubscriptionRequest(string CustomerId, string PlanId, BillingCycle Cycle, decimal Value);
public record AsaasCustomerResponse(string Id);
public record AsaasSubscriptionResponse(string Id, string Status);

/// <summary>Cobrança avulsa (pagamento único) — usada na compra individual de um curso.</summary>
public record AsaasChargeRequest(string CustomerId, decimal Value, string Description, string? ExternalReference);
public record AsaasChargeResponse(string Id, string Status, string? InvoiceUrl);

public interface IPaymentService
{
    Task<AsaasCustomerResponse> CreateOrGetCustomerAsync(AsaasCustomerRequest request, CancellationToken ct = default);
    Task<AsaasSubscriptionResponse> CreateSubscriptionAsync(AsaasSubscriptionRequest request, CancellationToken ct = default);
    /// <summary>
    /// Retorna o invoiceUrl do primeiro pagamento gerado pela assinatura.
    /// O cliente acessa esse link para escolher PIX, cartão ou boleto.
    /// </summary>
    Task<string?> GetSubscriptionPaymentUrlAsync(string asaasSubscriptionId, CancellationToken ct = default);
    Task CancelSubscriptionAsync(string asaasSubscriptionId, CancellationToken ct = default);

    /// <summary>
    /// Cria uma cobrança avulsa (pagamento único, não recorrente) — usada na compra individual
    /// de um curso pelo aluno. Diferente de <see cref="CreateSubscriptionAsync"/>, que gera
    /// cobranças recorrentes para a assinatura da plataforma (modelo legado).
    /// </summary>
    Task<AsaasChargeResponse> CreateChargeAsync(AsaasChargeRequest request, CancellationToken ct = default);

    bool ValidateWebhookSignature(string payload, string signature);
}
