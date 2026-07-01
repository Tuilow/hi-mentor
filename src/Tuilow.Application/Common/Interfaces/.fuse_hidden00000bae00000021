using DogMaster.Domain.Contexts.Subscription.Enums;

namespace DogMaster.Application.Common.Interfaces;

public record AsaasCustomerRequest(string Name, string Email, string? CpfCnpj, string? Phone);
public record AsaasSubscriptionRequest(string CustomerId, string PlanId, BillingCycle Cycle, decimal Value);
public record AsaasCustomerResponse(string Id);
public record AsaasSubscriptionResponse(string Id, string Status);

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
    bool ValidateWebhookSignature(string payload, string signature);
}
