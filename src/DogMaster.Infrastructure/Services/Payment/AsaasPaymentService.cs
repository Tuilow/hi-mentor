using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DogMaster.Application.Common.Interfaces;
using DogMaster.Domain.Contexts.Subscription.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DogMaster.Infrastructure.Services.Payment;

public sealed class AsaasPaymentService(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<AsaasPaymentService> logger
) : IPaymentService
{
    private readonly string _webhookSecret = configuration["Asaas:WebhookSecret"] ?? "";

    public async Task<AsaasCustomerResponse> CreateOrGetCustomerAsync(
        AsaasCustomerRequest request, CancellationToken ct = default)
    {
        // Tenta encontrar customer existente por e-mail
        var searchResponse = await httpClient.GetAsync(
            $"/api/v3/customers?email={Uri.EscapeDataString(request.Email)}&limit=1", ct);

        if (searchResponse.IsSuccessStatusCode)
        {
            var searchContent = await searchResponse.Content.ReadAsStringAsync(ct);
            var searchDoc = JsonDocument.Parse(searchContent);
            var data = searchDoc.RootElement.GetProperty("data");
            if (data.GetArrayLength() > 0)
            {
                var existingId = data[0].GetProperty("id").GetString()!;
                return new AsaasCustomerResponse(existingId);
            }
        }

        // Cria novo customer
        var payload = new
        {
            name = request.Name,
            email = request.Email,
            cpfCnpj = request.CpfCnpj,
            phone = request.Phone
        };

        var response = await httpClient.PostAsync("/api/v3/customers",
            new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"), ct);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(ct);
        var doc = JsonDocument.Parse(content);
        var id = doc.RootElement.GetProperty("id").GetString()!;
        return new AsaasCustomerResponse(id);
    }

    public async Task<AsaasSubscriptionResponse> CreateSubscriptionAsync(
        AsaasSubscriptionRequest request, CancellationToken ct = default)
    {
        var cycle = request.Cycle switch
        {
            BillingCycle.Monthly => "MONTHLY",
            BillingCycle.Quarterly => "QUARTERLY",
            BillingCycle.Annual => "YEARLY",
            _ => "MONTHLY"
        };

        var payload = new
        {
            customer = request.CustomerId,
            billingType = "UNDEFINED", // Permite PIX, Cartão e Boleto
            value = request.Value,
            nextDueDate = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd"),
            cycle,
            description = "Assinatura DogMaster Pro"
        };

        var response = await httpClient.PostAsync("/api/v3/subscriptions",
            new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"), ct);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(ct);
        var doc = JsonDocument.Parse(content);
        var id = doc.RootElement.GetProperty("id").GetString()!;
        var status = doc.RootElement.GetProperty("status").GetString()!;
        return new AsaasSubscriptionResponse(id, status);
    }

    public async Task CancelSubscriptionAsync(string asaasSubscriptionId, CancellationToken ct = default)
    {
        var response = await httpClient.DeleteAsync($"/api/v3/subscriptions/{asaasSubscriptionId}", ct);
        if (!response.IsSuccessStatusCode)
            logger.LogWarning("Falha ao cancelar assinatura Asaas: {Id}", asaasSubscriptionId);
    }

    public bool ValidateWebhookSignature(string payload, string signature)
    {
        if (string.IsNullOrEmpty(_webhookSecret)) return true; // Dev mode

        var hmac = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(_webhookSecret),
            Encoding.UTF8.GetBytes(payload));
        var computed = Convert.ToHexString(hmac).ToLowerInvariant();
        return computed == signature.ToLowerInvariant();
    }
}
