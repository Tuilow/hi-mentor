using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Tuilow.Sales.Application.Interfaces;
using Tuilow.Sales.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Tuilow.Sales.Infrastructure.Services;

public sealed class AsaasPaymentService(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<AsaasPaymentService> logger
) : IPaymentService
{
    private readonly string _webhookSecret = configuration["Asaas:WebhookSecret"] ?? "";

    // ─── Helpers ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Lê o corpo da resposta e lança uma exceção com o conteúdo do erro Asaas,
    /// em vez de lançar HttpRequestException genérica sem contexto.
    /// </summary>
    private async Task ThrowAsaasErrorAsync(HttpResponseMessage response, string operation, CancellationToken ct)
    {
        var body = await response.Content.ReadAsStringAsync(ct);
        logger.LogError("Asaas {Operation} falhou [{Status}]: {Body}",
            operation, (int)response.StatusCode, body);

        // Tenta extrair a mensagem de erro do JSON do Asaas: { "errors": [{ "description": "..." }] }
        string? errorMessage = null;
        try
        {
            var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("errors", out var errors) && errors.GetArrayLength() > 0)
                errorMessage = errors[0].GetProperty("description").GetString();
        }
        catch { /* ignora falha no parse */ }

        throw new InvalidOperationException(
            $"Asaas {operation}: {errorMessage ?? $"HTTP {(int)response.StatusCode}"}");
    }

    // ─── Customer ─────────────────────────────────────────────────────────────

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
                logger.LogInformation("Customer Asaas existente encontrado: {Id}", existingId);
                return new AsaasCustomerResponse(existingId);
            }
        }

        // Monta payload omitindo campos opcionais nulos/vazios
        // (Asaas rejeita com 400 se cpfCnpj for string vazia)
        var payloadDict = new Dictionary<string, object?>
        {
            ["name"]  = request.Name,
            ["email"] = request.Email,
        };

        var cpf = request.CpfCnpj?.Trim().Replace(".", "").Replace("-", "").Replace("/", "");
        if (!string.IsNullOrEmpty(cpf))
            payloadDict["cpfCnpj"] = cpf;

        // Asaas distingue "phone" (fixo) de "mobilePhone" (celular) e rejeita um celular de
        // 11 dígitos (DDD + 9 + 8 dígitos) enviado no campo de fixo. O formulário de checkout
        // só tem um campo genérico "Telefone", então inferimos pelo tamanho do número: 11
        // dígitos = celular, 10 dígitos = fixo.
        var phoneDigits = new string((request.Phone ?? "").Where(char.IsDigit).ToArray());
        if (phoneDigits.Length == 11)
            payloadDict["mobilePhone"] = phoneDigits;
        else if (!string.IsNullOrEmpty(phoneDigits))
            payloadDict["phone"] = phoneDigits;

        var json = JsonSerializer.Serialize(payloadDict);
        logger.LogDebug("Asaas CreateCustomer payload: {Json}", json);

        var response = await httpClient.PostAsync("/api/v3/customers",
            new StringContent(json, Encoding.UTF8, "application/json"), ct);

        if (!response.IsSuccessStatusCode)
            await ThrowAsaasErrorAsync(response, "CreateCustomer", ct);

        var content = await response.Content.ReadAsStringAsync(ct);
        var doc = JsonDocument.Parse(content);
        var id = doc.RootElement.GetProperty("id").GetString()!;
        logger.LogInformation("Customer Asaas criado: {Id}", id);
        return new AsaasCustomerResponse(id);
    }

    // ─── Subscription ─────────────────────────────────────────────────────────

    public async Task<AsaasSubscriptionResponse> CreateSubscriptionAsync(
        AsaasSubscriptionRequest request, CancellationToken ct = default)
    {
        var cycle = request.Cycle switch
        {
            BillingCycle.Monthly    => "MONTHLY",
            BillingCycle.Quarterly  => "QUARTERLY",
            BillingCycle.Semiannual => "SEMIANNUALLY",
            BillingCycle.Annual     => "YEARLY",
            _                       => "MONTHLY"
        };

        var payload = new
        {
            customer    = request.CustomerId,
            billingType = "UNDEFINED",  // Cliente escolhe PIX / Cartão / Boleto na hora do pagamento
            value       = request.Value,
            nextDueDate = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd"),
            cycle,
            description = "Assinatura Tuilow"
        };

        var json = JsonSerializer.Serialize(payload);
        logger.LogDebug("Asaas CreateSubscription payload: {Json}", json);

        var response = await httpClient.PostAsync("/api/v3/subscriptions",
            new StringContent(json, Encoding.UTF8, "application/json"), ct);

        if (!response.IsSuccessStatusCode)
            await ThrowAsaasErrorAsync(response, "CreateSubscription", ct);

        var content = await response.Content.ReadAsStringAsync(ct);
        var doc = JsonDocument.Parse(content);
        var id     = doc.RootElement.GetProperty("id").GetString()!;
        var status = doc.RootElement.GetProperty("status").GetString()!;
        logger.LogInformation("Assinatura Asaas criada: {Id} [{Status}]", id, status);
        return new AsaasSubscriptionResponse(id, status);
    }

    // ─── Charge (pagamento único — compra avulsa de curso) ─────────────────────

    public async Task<AsaasChargeResponse> CreateChargeAsync(AsaasChargeRequest request, CancellationToken ct = default)
    {
        var payload = new
        {
            customer          = request.CustomerId,
            billingType       = "UNDEFINED", // Cliente escolhe PIX / Cartão / Boleto na hora do pagamento
            value             = request.Value,
            dueDate           = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd"),
            description       = request.Description,
            externalReference = request.ExternalReference
        };

        var json = JsonSerializer.Serialize(payload);
        logger.LogDebug("Asaas CreateCharge payload: {Json}", json);

        var response = await httpClient.PostAsync("/api/v3/payments",
            new StringContent(json, Encoding.UTF8, "application/json"), ct);

        if (!response.IsSuccessStatusCode)
            await ThrowAsaasErrorAsync(response, "CreateCharge", ct);

        var content = await response.Content.ReadAsStringAsync(ct);
        var doc = JsonDocument.Parse(content);
        var id     = doc.RootElement.GetProperty("id").GetString()!;
        var status = doc.RootElement.GetProperty("status").GetString()!;
        var invoiceUrl = doc.RootElement.TryGetProperty("invoiceUrl", out var invoiceUrlEl) ? invoiceUrlEl.GetString() : null;

        logger.LogInformation("Cobrança avulsa Asaas criada: {Id} [{Status}]", id, status);
        return new AsaasChargeResponse(id, status, invoiceUrl);
    }

    // ─── Payment URL ──────────────────────────────────────────────────────────

    public async Task<string?> GetSubscriptionPaymentUrlAsync(
        string asaasSubscriptionId, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.GetAsync(
                $"/api/v3/subscriptions/{asaasSubscriptionId}/payments?limit=1", ct);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Asaas GetPayments falhou [{Status}] para subscription {Id}",
                    (int)response.StatusCode, asaasSubscriptionId);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(ct);
            var doc  = JsonDocument.Parse(content);
            var data = doc.RootElement.GetProperty("data");

            if (data.GetArrayLength() == 0)
            {
                logger.LogWarning("Asaas: nenhum pagamento encontrado para subscription {Id}", asaasSubscriptionId);
                return null;
            }

            var payment = data[0];

            // invoiceUrl: link unificado onde o cliente escolhe PIX/cartão/boleto
            if (payment.TryGetProperty("invoiceUrl", out var invoiceUrl))
            {
                var url = invoiceUrl.GetString();
                if (!string.IsNullOrEmpty(url))
                {
                    logger.LogInformation("Asaas invoiceUrl obtido para subscription {Id}", asaasSubscriptionId);
                    return url;
                }
            }

            // Fallback: bankSlipUrl (boleto)
            if (payment.TryGetProperty("bankSlipUrl", out var boletoUrl))
                return boletoUrl.GetString();

            return null;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falha ao obter URL de pagamento para subscription {Id}", asaasSubscriptionId);
            return null;
        }
    }

    // ─── Cancel ───────────────────────────────────────────────────────────────

    public async Task CancelSubscriptionAsync(string asaasSubscriptionId, CancellationToken ct = default)
    {
        var response = await httpClient.DeleteAsync($"/api/v3/subscriptions/{asaasSubscriptionId}", ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            logger.LogWarning("Falha ao cancelar assinatura Asaas {Id} [{Status}]: {Body}",
                asaasSubscriptionId, (int)response.StatusCode, body);
        }
    }

    // ─── Webhook ──────────────────────────────────────────────────────────────

    public bool ValidateWebhookSignature(string payload, string signature)
    {
        if (string.IsNullOrEmpty(_webhookSecret)) return true; // Dev: aceita sem verificar

        var hmac     = HMACSHA256.HashData(
            Encoding.UTF8.GetBytes(_webhookSecret),
            Encoding.UTF8.GetBytes(payload));
        var computed = Convert.ToHexString(hmac).ToLowerInvariant();
        return computed == signature.ToLowerInvariant();
    }
}
