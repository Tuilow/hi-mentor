using System.Net;
using System.Text;
using System.Text.Json;
using HiMentor.SharedKernel.Application.Exceptions;
using HiMentor.SharedKernel.Application.Interfaces;
using HiMentor.Sales.Application.Interfaces;
using HiMentor.Finance.Domain.Entities;
using HiMentor.Finance.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HiMentor.Sales.Infrastructure.Services;

/// <summary>
/// Cria clientes/cobrancas DIRETAMENTE na conta Asaas de um creator (marketplace de split) --
/// diferente de AsaasPaymentService (conta fixa da propria HiMentor, HttpClient tipado com
/// credencial estatica), aqui a credencial muda por chamada (a API Key do creator sendo
/// atendido), entao usamos IHttpClientFactory e montamos o cliente sob demanda a cada request,
/// igual ao client "AsaasMarketplace" registrado em DependencyInjection.cs.
///
/// Mantem exatamente o mesmo estilo de serializacao/parsing manual (JsonSerializer +
/// JsonDocument) do AsaasPaymentService legado, deliberadamente -- evita depender de
/// System.Net.Http.Json (PostAsJsonAsync/ReadFromJsonAsync) que nao e usado em nenhum outro
/// lugar desta base.
/// </summary>
public sealed class AsaasMarketplacePaymentService(
    IHttpClientFactory httpClientFactory,
    // Corrigido em ago/2026 (bug de produção: webhook de pagamento rejeitado por divergência de
    // conta) -- a cobrança/cliente do marketplace agora é criada na subconta BaaS (novo modelo),
    // nunca mais na conta legada "cole sua API Key".
    ICreatorAsaasSubaccountRepository creatorAsaasSubaccountRepository,
    ICreatorAsaasCustomerRepository creatorAsaasCustomerRepository,
    ISecretProtector secretProtector,
    IConfiguration configuration,
    IUnitOfWork uow,
    IFrontendUrlProvider frontendUrlProvider,
    ILogger<AsaasMarketplacePaymentService> logger
) : IMarketplacePaymentService
{
    // Mesma ideia de AsaasPaymentService.CreateChargeAsync (ver comentário lá), mas aqui a
    // cobrança é criada na SUBCONTA do creator (API Key dele), não na conta da HiMentor -- então
    // é a "Configurações da conta → Informações" DA SUBCONTA do creator que precisaria ter esse
    // domínio cadastrado, algo que a HiMentor não controla (cada creator cadastra a própria conta).
    // Por isso o fallback "repete sem callback" abaixo não é só uma rede de segurança teórica
    // aqui -- é o caminho esperado até esse cadastro existir (ou pra sempre, se a Asaas não
    // permitir usar um domínio de terceiro na subconta). Nunca compromete a venda por causa
    // disso: na pior hipótese, essa cobrança específica simplesmente não redireciona sozinha.
    private string PaymentSuccessUrl => frontendUrlProvider.BuildUrl("/pagamento-confirmado");

    private static bool LooksLikeCallbackRejection(string body) =>
        body.Contains("successUrl", StringComparison.OrdinalIgnoreCase)
        || body.Contains("callback", StringComparison.OrdinalIgnoreCase);

    private string BaseUrl
    {
        get
        {
            var baseUrl = configuration["Asaas:BaseUrl"] ?? "https://api-sandbox.asaas.com/v3";
            if (!baseUrl.EndsWith('/')) baseUrl += "/";
            return baseUrl;
        }
    }

    private string PlatformWalletId =>
        configuration["Asaas:PlatformWalletId"]
        ?? throw new InvalidOperationException(
            "Asaas:PlatformWalletId não configurado — obrigatório para o marketplace de split (walletId da HiMentor que recebe a comissão).");

    public async Task<MarketplaceCustomerResponse> CreateOrGetCustomerAsync(
        Guid creatorId, Guid studentId, MarketplaceCustomerRequest request, CancellationToken ct = default)
    {
        var (subaccount, apiKey) = await ResolveCredentialsAsync(creatorId, ct);

        // Cache local primeiro -- evita depender da busca por e-mail da Asaas (que é por conta,
        // então funciona igual, mas uma segunda compra do mesmo aluno com o mesmo creator não
        // precisa nem chamar a Asaas de novo).
        var existingMapping = await creatorAsaasCustomerRepository.GetAsync(subaccount.Id, studentId, ct);
        if (existingMapping is not null)
            return new MarketplaceCustomerResponse(existingMapping.AsaasCustomerId);

        var httpClient = CreateClient(apiKey);

        var searchResponse = await httpClient.GetAsync($"customers?email={Uri.EscapeDataString(request.Email)}&limit=1", ct);
        if (searchResponse.IsSuccessStatusCode)
        {
            var searchContent = await searchResponse.Content.ReadAsStringAsync(ct);
            var searchDoc = JsonDocument.Parse(searchContent);
            var data = searchDoc.RootElement.GetProperty("data");
            if (data.GetArrayLength() > 0)
            {
                var existingId = data[0].GetProperty("id").GetString()!;
                await SaveMappingAsync(subaccount.Id, studentId, existingId, ct);
                return new MarketplaceCustomerResponse(existingId);
            }
        }

        var payloadDict = new Dictionary<string, object?>
        {
            ["name"] = request.Name,
            ["email"] = request.Email,
        };

        var cpf = request.CpfCnpj?.Trim().Replace(".", "").Replace("-", "").Replace("/", "");
        if (!string.IsNullOrEmpty(cpf))
            payloadDict["cpfCnpj"] = cpf;

        var phoneDigits = new string((request.Phone ?? "").Where(char.IsDigit).ToArray());
        if (phoneDigits.Length == 11)
            payloadDict["mobilePhone"] = phoneDigits;
        else if (!string.IsNullOrEmpty(phoneDigits))
            payloadDict["phone"] = phoneDigits;

        var json = JsonSerializer.Serialize(payloadDict);
        var response = await httpClient.PostAsync("customers", new StringContent(json, Encoding.UTF8, "application/json"), ct);

        if (!response.IsSuccessStatusCode)
            await ThrowAsaasErrorAsync(response, "CreateMarketplaceCustomer", ct);

        var content = await response.Content.ReadAsStringAsync(ct);
        var doc = JsonDocument.Parse(content);
        if (!doc.RootElement.TryGetProperty("id", out var idProp))
        {
            logger.LogError("Asaas CreateMarketplaceCustomer (creator {CreatorId}) retornou {Status} sem campo 'id': {Body}",
                creatorId, (int)response.StatusCode, content);
            throw new ExternalServiceException("Asaas CreateMarketplaceCustomer: resposta inesperada (sem id do cliente).");
        }

        var newId = idProp.GetString()!;
        await SaveMappingAsync(subaccount.Id, studentId, newId, ct);
        return new MarketplaceCustomerResponse(newId);
    }

    public async Task<MarketplaceChargeResponse> CreateChargeAsync(
        Guid creatorId, MarketplaceChargeRequest request, decimal commissionPercentage, CancellationToken ct = default)
    {
        var (_, apiKey) = await ResolveCredentialsAsync(creatorId, ct);
        var httpClient = CreateClient(apiKey);

        Dictionary<string, object?> BuildChargePayload(bool includeCallback)
        {
            var p = new Dictionary<string, object?>
            {
                ["customer"] = request.AsaasCustomerId,
                ["billingType"] = "UNDEFINED", // Aluno escolhe PIX / Cartão / Boleto na hora do pagamento — mesmo padrão do modelo Legacy.
                ["value"] = request.Value,
                ["dueDate"] = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd"),
                ["description"] = request.Description,
                ["externalReference"] = request.ExternalReference,
                ["split"] = new object[]
                {
                    new { walletId = PlatformWalletId, percentualValue = commissionPercentage }
                },
            };
            if (includeCallback)
                p["callback"] = new Dictionary<string, object?> { ["successUrl"] = PaymentSuccessUrl, ["autoRedirect"] = true };
            return p;
        }

        var json = JsonSerializer.Serialize(BuildChargePayload(includeCallback: true));
        logger.LogDebug("Asaas CreateMarketplaceCharge payload (creator {CreatorId}): {Json}", creatorId, json);

        var response = await httpClient.PostAsync("payments", new StringContent(json, Encoding.UTF8, "application/json"), ct);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            if (!LooksLikeCallbackRejection(errorBody))
                ThrowAsaasErrorFromBody(response.StatusCode, errorBody, "CreateMarketplaceCharge");

            logger.LogWarning(
                "Asaas rejeitou callback.successUrl em CreateMarketplaceCharge (creator {CreatorId}) -- " +
                "provável domínio não cadastrado na subconta do creator -- repetindo sem callback. " +
                "Corpo original: {Body}", creatorId, errorBody);
            var retryJson = JsonSerializer.Serialize(BuildChargePayload(includeCallback: false));
            response = await httpClient.PostAsync("payments", new StringContent(retryJson, Encoding.UTF8, "application/json"), ct);

            if (!response.IsSuccessStatusCode)
                await ThrowAsaasErrorAsync(response, "CreateMarketplaceCharge", ct);
        }

        var content = await response.Content.ReadAsStringAsync(ct);
        var doc = JsonDocument.Parse(content);
        var id = doc.RootElement.GetProperty("id").GetString()!;
        var status = doc.RootElement.GetProperty("status").GetString()!;
        var invoiceUrl = doc.RootElement.TryGetProperty("invoiceUrl", out var invoiceUrlEl) ? invoiceUrlEl.GetString() : null;

        logger.LogInformation("Cobrança marketplace criada na conta do creator {CreatorId}: {Id} [{Status}], split {Pct}% -> {WalletId}",
            creatorId, id, status, commissionPercentage, PlatformWalletId);

        return new MarketplaceChargeResponse(id, status, invoiceUrl);
    }

    private async Task<(CreatorAsaasSubaccount Subaccount, string ApiKey)> ResolveCredentialsAsync(Guid creatorId, CancellationToken ct)
    {
        var subaccount = await creatorAsaasSubaccountRepository.GetByCreatorIdAsync(creatorId, ct)
            ?? throw new InvalidOperationException($"Creator {creatorId} não tem subconta Asaas conectada ao marketplace.");

        if (!subaccount.CanSell)
            throw new InvalidOperationException($"Subconta Asaas do creator {creatorId} não está apta a vender (status {subaccount.Status}).");

        if (subaccount.ApiKeyEncrypted is null)
            throw new InvalidOperationException($"Subconta Asaas do creator {creatorId} ainda não tem API Key persistida.");

        var apiKey = secretProtector.Unprotect(subaccount.ApiKeyEncrypted);
        return (subaccount, apiKey);
    }

    // Parâmetro segue chamado "creatorAsaasAccountId" porque é literalmente o nome do campo em
    // CreatorAsaasCustomer.Create (entidade sem FK/navegação -- ver Finance.Domain.Entities --
    // aceita tanto o Id da conta legada quanto o Id da subconta nova, dependendo de quem chama).
    private async Task SaveMappingAsync(Guid creatorAsaasAccountId, Guid studentId, string asaasCustomerId, CancellationToken ct)
    {
        await creatorAsaasCustomerRepository.AddAsync(
            CreatorAsaasCustomer.Create(creatorAsaasAccountId, studentId, asaasCustomerId), ct);
        await uow.SaveChangesAsync(ct);
    }

    private HttpClient CreateClient(string apiKey)
    {
        var client = httpClientFactory.CreateClient("AsaasMarketplace");
        client.BaseAddress = new Uri(BaseUrl);
        client.DefaultRequestHeaders.Add("access_token", apiKey);
        client.DefaultRequestHeaders.Add("User-Agent", "HiMentor/1.0");
        return client;
    }

    private async Task ThrowAsaasErrorAsync(HttpResponseMessage response, string operation, CancellationToken ct)
    {
        var body = await response.Content.ReadAsStringAsync(ct);
        logger.LogError("Asaas {Operation} falhou [{Status}]: {Body}", operation, (int)response.StatusCode, body);

        string? errorMessage = null;
        try
        {
            var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("errors", out var errors) && errors.GetArrayLength() > 0)
                errorMessage = errors[0].GetProperty("description").GetString();
        }
        catch { /* ignora falha no parse */ }

        throw new ExternalServiceException($"Asaas {operation}: {errorMessage ?? $"HTTP {(int)response.StatusCode}"}");
    }

    /// <summary>
    /// Mesma lógica de <see cref="ThrowAsaasErrorAsync"/>, mas para quando o corpo já foi lido
    /// antes (decidir se vale repetir sem `callback` -- ver CreateChargeAsync). HttpContent só
    /// pode ser lido uma vez; ler de novo aqui devolveria corpo vazio e esconderia o erro real.
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

        throw new ExternalServiceException($"Asaas {operation}: {errorMessage ?? $"HTTP {(int)statusCode}"}");
    }
}
