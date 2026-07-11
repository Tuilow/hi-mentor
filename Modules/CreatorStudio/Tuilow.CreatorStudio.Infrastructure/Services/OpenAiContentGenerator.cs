using System.Text;
using System.Text.Json;
using Tuilow.CreatorStudio.Application.Interfaces;
using Tuilow.CreatorStudio.Domain.Enums;
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

    private static readonly Dictionary<MarketingChannel, string> ChannelInstruction = new()
    {
        [MarketingChannel.InstagramPost] = "um post de Instagram (legenda com emojis, benefícios em bullet e hashtags relevantes ao final)",
        [MarketingChannel.InstagramStory] = "um Story de Instagram (texto bem curto, direto, chamando para arrastar para cima)",
        [MarketingChannel.WhatsApp] = "uma mensagem para enviar em grupos de WhatsApp (tom pessoal e direto, sem parecer spam)",
        [MarketingChannel.Email] = "um e-mail de vendas (com uma linha 'Assunto:' no início do texto, seguida do corpo do e-mail)",
        [MarketingChannel.MetaAds] = "um anúncio para o Meta Ads Manager (com linhas 'Título:', 'Texto principal:' e 'Descrição:')",
        [MarketingChannel.Headline] = "uma lista de 4 variações de headline (título de impacto) para a página de vendas, uma por linha",
    };

    public async Task<MarketingCopySuggestion> GenerateMarketingCopyAsync(
        string productName, MarketingChannel channel, string? category, string? shortDescription,
        IReadOnlyList<string> benefits, decimal price, CancellationToken ct = default)
    {
        var instruction = ChannelInstruction.GetValueOrDefault(channel, "um texto de divulgação");
        var benefitsText = benefits.Count > 0 ? string.Join(", ", benefits) : "não informados";

        var prompt =
            $"Você é um copywriter especialista em marketing digital para cursos online. Gere {instruction} " +
            $"para divulgar o curso \"{productName}\" (categoria: {category ?? "não informada"}, " +
            $"descrição curta: {shortDescription ?? "não informada"}, benefícios: {benefitsText}, " +
            $"preço: {(price > 0 ? $"R$ {price:0.00}" : "gratuito")}). " +
            "Responda APENAS com um JSON no formato exato: {\"content\": string, \"cta\": string ou null}";

        var json = await CompleteAsync(prompt, ct);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var cta = root.TryGetProperty("cta", out var ctaEl) && ctaEl.ValueKind != JsonValueKind.Null
            ? ctaEl.GetString()
            : null;

        return new MarketingCopySuggestion(root.GetProperty("content").GetString() ?? "", cta);
    }

    public async Task<CourseOutlineSuggestion> GenerateCourseOutlineAsync(
        string niche, string targetAudience, string objective, AudienceLevel level, CancellationToken ct = default)
    {
        var levelLabel = level switch
        {
            AudienceLevel.Beginner => "iniciante",
            AudienceLevel.Intermediate => "intermediário",
            _ => "avançado",
        };

        var prompt =
            "Você é um especialista em design instrucional e no nicho informado, criando a estrutura de um " +
            $"curso online. Nicho: \"{niche}\". Público-alvo: \"{targetAudience}\". Objetivo do curso: \"{objective}\". " +
            $"Nível dos alunos: {levelLabel}. Adapte a linguagem e os exemplos ao nicho (ex.: motivacional para " +
            "Personal Trainer, formal para Advogado, didática para Professor). Sugira 3 módulos, cada um com 2 a 4 " +
            "aulas, em ordem recomendada de aprendizado. Para cada aula, classifique o formato (\"Teórica\", " +
            "\"Prática\", \"Estudo de caso\", ou outro rótulo curto apropriado ao nicho). " +
            "Responda APENAS com um JSON no formato exato: {\"courseName\": string, \"courseDescription\": string, " +
            "\"modules\": [{\"title\": string, \"lessons\": [{\"title\": string, \"format\": string}]}]}";

        var json = await CompleteAsync(prompt, ct);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var modules = root.GetProperty("modules").EnumerateArray()
            .Select(m => new CourseOutlineModule(
                m.GetProperty("title").GetString() ?? "",
                m.GetProperty("lessons").EnumerateArray()
                    .Select(l => new CourseOutlineLesson(
                        l.GetProperty("title").GetString() ?? "", l.GetProperty("format").GetString() ?? "Teórica"))
                    .ToList()))
            .ToList();

        return new CourseOutlineSuggestion(
            root.GetProperty("courseName").GetString() ?? "",
            root.GetProperty("courseDescription").GetString() ?? "",
            modules);
    }

    public async Task<LessonScriptSuggestion> GenerateLessonScriptAsync(
        string lessonTitle, string niche, string targetAudience, AudienceLevel level, CancellationToken ct = default)
    {
        var levelLabel = level switch
        {
            AudienceLevel.Beginner => "iniciante",
            AudienceLevel.Intermediate => "intermediário",
            _ => "avançado",
        };

        var prompt =
            "Você é um especialista em roteiros de gravação de aulas para o nicho informado. Nicho: " +
            $"\"{niche}\". Público-alvo: \"{targetAudience}\". Nível: {levelLabel}. Gere um roteiro de gravação " +
            $"completo para a aula \"{lessonTitle}\", com linguagem e exemplos adaptados ao nicho (ex.: " +
            "motivacional e com demonstrações físicas para Personal Trainer, formal e com casos jurídicos para " +
            "Advogado, didática e com exercícios para Professor). Inclua: uma introdução de abertura, de 3 a 5 " +
            "tópicos de desenvolvimento a abordar, de 1 a 3 sugestões práticas do que gravar/demonstrar, e um " +
            "call-to-action de encerramento chamando para a próxima aula. " +
            "Responda APENAS com um JSON no formato exato: {\"introduction\": string, \"developmentTopics\": " +
            "string[], \"demonstrationSuggestions\": string[], \"closingCta\": string}";

        var json = await CompleteAsync(prompt, ct);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        return new LessonScriptSuggestion(
            root.GetProperty("introduction").GetString() ?? "",
            root.GetProperty("developmentTopics").EnumerateArray().Select(e => e.GetString() ?? "").ToList(),
            root.GetProperty("demonstrationSuggestions").EnumerateArray().Select(e => e.GetString() ?? "").ToList(),
            root.GetProperty("closingCta").GetString() ?? "");
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
