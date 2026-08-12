using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Tuilow.SharedKernel.Application.Exceptions;
using Tuilow.SharedKernel.Application.Interfaces;
using Tuilow.Sales.Application.Interfaces;
using Tuilow.Sales.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Tuilow.Sales.Infrastructure.Services;

public sealed class AsaasPaymentService(
    HttpClient httpClient,
    IConfiguration configuration,
    IFrontendUrlProvider frontendUrlProvider,
    ILogger<AsaasPaymentService> logger
) : IPaymentService
{
    private readonly string _webhookSecret = configuration["Asaas:WebhookSecret"] ?? "";

    // Achado 12/08/2026: depois de pagar por PIX/cartão (únicos métodos com confirmação
    // imediata na Asaas), o cliente ficava numa tela genérica da própria Asaas, sem nenhum
    // caminho de volta pro site nem aviso do que fazer a seguir. `callback.successUrl` faz a
    // Asaas redirecionar automaticamente pra essa página nossa (ver
    // docs.asaas.com/docs/redirecionamento-apos-o-pagamento) — que só avisa "verifique seu
    // e-mail" (o Magic Link, mecanismo real de acesso pós-compra, é disparado pelo webhook de
    // confirmação, não por este redirect, que pode chegar antes do webhook processar).
    // IMPORTANTE (passo manual, fora do código): a Asaas só aceita essa URL se o domínio dela
    // estiver cadastrado em "Configurações da conta → Informações" na própria Asaas. Até isso
    // ser configurado, CreateChargeAsync/CreateSubscriptionAsync detectam a rejeição e repetem
    // a chamada sem esse campo — a compra nunca quebra por causa de uma personalização opcional.
    private readonly string _paymentSuccessUrl = frontendUrlProvider.BuildUrl("/pagamento-confirmado");

    /// <summary>
    /// Mensagens de erro da Asaas para callback/successUrl inválido mencionam esses termos —
    /// checagem por substring (não regex) porque não vale depender do texto exato/idioma da
    /// resposta, só evitar confundir com outros motivos de rejeição da cobrança.
    /// </summary>
    private static bool LooksLikeCallbackRejection(string body) =>
        body.Contains("successUrl", StringComparison.OrdinalIgnoreCase)
        || body.Contains("callback", StringComparison.OrdinalIgnoreCase);

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

        // Achado M5 da avaliação: era InvalidOperationException — o middleware global tratava
        // isso como 422 e devolvia a Message (incluindo texto cru vindo da Asaas) direto ao
        // cliente. ExternalServiceException é sanitizada pelo middleware; a Message completa
        // (com o corpo de erro da Asaas) já foi logada acima para investigação interna.
        throw new ExternalServiceException(
            $"Asaas {operation}: {errorMessage ?? $"HTTP {(int)response.StatusCode}"}");
    }

    /// <summary>
    /// Mesma lógica de <see cref="ThrowAsaasErrorAsync"/>, mas para quando o corpo da resposta
    /// já foi lido antes (ex.: pra decidir se vale repetir a chamada sem `callback` — ver
    /// CreateChargeAsync/CreateSubscriptionAsync). HttpContent só pode ser lido uma vez; ler de
    /// novo aqui devolveria corpo vazio e escondería a mensagem de erro real do log.
    /// </summary>
    private void ThrowAsaasErrorFromBody(HttpStatusCode statusCode, string body, string operation)
    {
        logger.LogError("Asaas {Operation} falhou [{Status}]: {Body}", operation, (int)statusCode, body);

        string? errorMessage = null;
        try
        {
            var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("errors", out var errors) && errors.GetArrayLength() > 0)
                errorMessage = errors[0].GetProperty("description").GetString();
        }
        catch { /* ignora falha no parse */ }

        throw new ExternalServiceException(
            $"Asaas {operation}: {errorMessage ?? $"HTTP {(int)statusCode}"}");
    }

    // ─── Customer ─────────────────────────────────────────────────────────────

    public async Task<AsaasCustomerResponse> CreateOrGetCustomerAsync(
        AsaasCustomerRequest request, CancellationToken ct = default)
    {
        // Tenta encontrar customer existente por e-mail
        var searchResponse = await httpClient.GetAsync(
            $"customers?email={Uri.EscapeDataString(request.Email)}&limit=1", ct);

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

        var response = await httpClient.PostAsync("customers",
            new StringContent(json, Encoding.UTF8, "application/json"), ct);

        if (!response.IsSuccessStatusCode)
            await ThrowAsaasErrorAsync(response, "CreateCustomer", ct);

        var content = await response.Content.ReadAsStringAsync(ct);
        var doc = JsonDocument.Parse(content);

        // Achado em teste manual (produção): a Asaas pode responder 2xx pra POST
        // /customers com um corpo que não é o cliente esperado (ex.: conta em análise/
        // restrição, campo obrigatório adicional só exigido em produção) -- sem essa
        // checagem, GetProperty("id") lançava KeyNotFoundException genérica, sem logar o
        // corpo da resposta, e a compra inteira derrubava com 500 sem pista nenhuma do
        // motivo real. Agora loga o corpo bruto e lança um erro claro e sanitizado.
        if (!doc.RootElement.TryGetProperty("id", out var idProp))
        {
            logger.LogError(
                "Asaas CreateCustomer retornou {Status} sem campo 'id' no corpo: {Body}",
                (int)response.StatusCode, content);
            throw new ExternalServiceException(
                "Asaas CreateCustomer: resposta inesperada (sem id do cliente).");
        }

        var id = idProp.GetString()!;
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

        Dictionary<string, object?> BuildSubscriptionPayload(bool includeCallback)
        {
            var p = new Dictionary<string, object?>
            {
                ["customer"]    = request.CustomerId,
                ["billingType"] = "UNDEFINED",  // Cliente escolhe PIX / Cartão / Boleto na hora do pagamento
                ["value"]       = request.Value,
                ["nextDueDate"] = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd"),
                ["cycle"]       = cycle,
                ["description"] = "Assinatura Tuilow",
            };
            if (includeCallback)
                p["callback"] = new Dictionary<string, object?> { ["successUrl"] = _paymentSuccessUrl, ["autoRedirect"] = true };
            return p;
        }

        var json = JsonSerializer.Serialize(BuildSubscriptionPayload(includeCallback: true));
        logger.LogDebug("Asaas CreateSubscription payload: {Json}", json);

        var response = await httpClient.PostAsync("subscriptions",
            new StringContent(json, Encoding.UTF8, "application/json"), ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            if (!LooksLikeCallbackRejection(errorBody))
                ThrowAsaasErrorFromBody(response.StatusCode, errorBody, "CreateSubscription");

            logger.LogWarning(
                "Asaas rejeitou callback.successUrl em CreateSubscription (domínio provavelmente " +
                "não cadastrado na conta) — repetindo sem callback. Corpo original: {Body}", errorBody);
            var retryJson = JsonSerializer.Serialize(BuildSubscriptionPayload(includeCallback: false));
            response = await httpClient.PostAsync("subscriptions",
                new StringContent(retryJson, Encoding.UTF8, "application/json"), ct);

            if (!response.IsSuccessStatusCode)
                await ThrowAsaasErrorAsync(response, "CreateSubscription", ct);
        }

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
        Dictionary<string, object?> BuildChargePayload(bool includeCallback)
        {
            var p = new Dictionary<string, object?>
            {
                ["customer"]          = request.CustomerId,
                ["billingType"]       = "UNDEFINED", // Cliente escolhe PIX / Cartão / Boleto na hora do pagamento
                ["value"]             = request.Value,
                ["dueDate"]           = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd"),
                ["description"]       = request.Description,
                ["externalReference"] = request.ExternalReference,
            };
            if (includeCallback)
                p["callback"] = new Dictionary<string, object?> { ["successUrl"] = _paymentSuccessUrl, ["autoRedirect"] = true };
            return p;
        }

        var json = JsonSerializer.Serialize(BuildChargePayload(includeCallback: true));
        logger.LogDebug("Asaas CreateCharge payload: {Json}", json);

        var response = await httpClient.PostAsync("payments",
            new StringContent(json, Encoding.UTF8, "application/json"), ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            if (!LooksLikeCallbackRejection(errorBody))
                ThrowAsaasErrorFromBody(response.StatusCode, errorBody, "CreateCharge");

            logger.LogWarning(
                "Asaas rejeitou callback.successUrl em CreateCharge (domínio provavelmente não " +
                "cadastrado na conta) — repetindo sem callback. Corpo original: {Body}", errorBody);
            var retryJson = JsonSerializer.Serialize(BuildChargePayload(includeCallback: false));
            response = await httpClient.PostAsync("payments",
                new StringContent(retryJson, Encoding.UTF8, "application/json"), ct);

            if (!response.IsSuccessStatusCode)
                await ThrowAsaasErrorAsync(response, "CreateCharge", ct);
        }

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
                $"subscriptions/{asaasSubscriptionId}/payments?limit=1", ct);

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
        var response = await httpClient.DeleteAsync($"subscriptions/{asaasSubscriptionId}", ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            logger.LogWarning("Falha ao cancelar assinatura Asaas {Id} [{Status}]: {Body}",
                asaasSubscriptionId, (int)response.StatusCode, body);
        }
    }

    // ─── Webhook ──────────────────────────────────────────────────────────────

    public bool ValidateWebhookSignature(string accessToken)
    {
        if (string.IsNullOrEmpty(_webhookSecret))
        {
            // Sem secret configurado: Program.cs falha o startup fora de Development quando
            // Asaas:WebhookSecret vem vazio, então isto só deveria ser alcançado em
            // desenvolvimento local, antes de configurar o webhook de verdade na Asaas.
            logger.LogWarning(
                "Asaas:WebhookSecret vazio — aceitando webhook sem validação (esperado só em Development).");
            return true;
        }

        if (string.IsNullOrEmpty(accessToken)) return false;

        // A Asaas NÃO assina o corpo da requisição do webhook: ela apenas ecoa de volta, no
        // header "asaas-access-token", o mesmo token estático cadastrado no painel ao criar o
        // webhook. A validação correta é comparar esse token diretamente contra o secret
        // configurado, em tempo constante (evita vazar informação por timing) — calcular um
        // HMAC do corpo (como antes) nunca bate de forma legítima, pois a Asaas nunca assina
        // o payload dessa forma.
        var expected = Encoding.UTF8.GetBytes(_webhookSecret);
        var actual = Encoding.UTF8.GetBytes(accessToken);

        return expected.Length == actual.Length && CryptographicOperations.FixedTimeEquals(expected, actual);
    }
}
