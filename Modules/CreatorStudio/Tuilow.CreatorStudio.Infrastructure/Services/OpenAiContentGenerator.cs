using System.Text;
using System.Text.Json;
using Tuilow.CreatorStudio.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Tuilow.CreatorStudio.Infrastructure.Services;

/// <summary>
/// Implementação real do IAiContentGenerator, pronta para uso assim que uma chave de API for
/// configurada (AiContentGenerator:ApiKey). Fala o protocolo "chat completions" (compatível
/// com OpenAI e a maioria dos provedores compatíveis/proxies — a BaseUrl é configurável via
/// AiContentGenerator:BaseUrl, então também funciona com Azure OpenAI ou outro gateway).
/// Pede ao modelo para responder em JSON estrito e faz o parse para os mesmos contratos que o
/// MockAiContentGenerator usa — trocar de mock pra real é só mudar AiContentGenerator:MockMode
/// pra false e preencher a chave, nenhum outro código muda (mesmo padrão de
/// CloudflareStreamService/MockStreamingService).
/// </summary>
public sealed class OpenAiContentGenerator(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<OpenAiContentGenerator> logger
) : IAiContentGenerator
{
    private readonly string _apiKey = configuration["AiContentGenerator:ApiKey"]
        ?? throw new InvalidOperationException(
            "AiContentGenerator:ApiKey não configurado. Preencha appsettings.json > AiContentGenerator:ApiKey, " +
            "ou mantenha AiContentGenerator:MockMode = true para usar o gerador mock.");

    private readonly string _model = configuration["AiContentGenerator:Model"] ?? "gpt-4o-mini";

    public async Task<ProductCopySuggestion> GenerateProductCopyAsync(
        string productName, string? category, string? subcategory, CancellationToken ct = default)
    {
        var prompt =
            $"Você é um copywriter especialista em cursos online. Gere uma copy de vendas para o produto " +
            $"\"{productName}\" (categoria: {category ?? "não informada"}, subcategoria: {subcategory ?? "não informada"}). " +
            "Responda APENAS com um JSON no formato exato: " +
            "{\"shortDescription\": string, \"fullDescription\": string, \"benefits\": string[], \"targetAudience\": string, \"callToAction\": string}";

        var json = await CompleteAsync(prompt, ct);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        return new ProductCopySuggestion(
            root.GetProperty("shortDescription").GetString() ?? "",
            root.GetProperty("fullDescription").GetString() ?? "",
            root.GetProperty("benefits").EnumerateArray().Select(e => e.GetString() ?? "").ToList(),
            root.GetProperty("targetAudience").GetString() ?? "",
            root.GetProperty("callToAction").GetString() ?? "");
    }

    public async Task<SalesPageSuggestion> GenerateSalesPageAsync(
        string productName, string? category, string? shortDescription, decimal price, CancellationToken ct = default)
    {
        var prompt =
            $"Você é um copywriter especialista em páginas de vendas de cursos online. Gere o conteúdo da página " +
            $"de vendas do produto \"{productName}\" (categoria: {category ?? "não informada"}, preço: R$ {price:0.00}, " +
            $"descrição curta: {shortDescription ?? "não informada"}). " +
            "Responda APENAS com um JSON no formato exato: " +
            "{\"headline\": string, \"subheadline\": string, \"benefits\": string[], " +
            "\"faq\": [{\"question\": string, \"answer\": string}], \"callToAction\": string}";

        var json = await CompleteAsync(prompt, ct);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var faq = root.GetProperty("faq").EnumerateArray()
            .Select(e => new SalesPageFaqSuggestion(
                e.GetProperty("question").GetString() ?? "", e.GetProperty("answer").GetString() ?? ""))
            .ToList();

        return new SalesPageSuggestion(
            root.GetProperty("headline").GetString() ?? "",
            root.GetProperty("subheadline").GetString() ?? "",
            root.GetProperty("benefits").EnumerateArray().Select(e => e.GetString() ?? "").ToList(),
            faq,
            root.GetProperty("callToAction").GetString() ?? "");
    }

    private async Task<string> CompleteAsync(string prompt, CancellationToken ct)
    {
        var payload = new
        {
            model = _model,
            messages = new[] { new { role = "user", content = prompt } },
            response_format = new { type = "json_object" }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "/v1/chat/completions")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);

        var response = await httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            logger.LogError("Erro na API de geração de conteúdo: {Status} {Body}", response.StatusCode, errorBody);
            throw new InvalidOperationException($"Falha ao gerar conteúdo com IA ({(int)response.StatusCode}).");
        }

        var content = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(content);
        return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString()!;
    }
}
