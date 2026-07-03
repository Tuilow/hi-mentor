using Tuilow.CreatorStudio.Application.Interfaces;

namespace Tuilow.CreatorStudio.Infrastructure.Services;

/// <summary>
/// Implementação mock do IAiContentGenerator — gera copy de verdade (não é texto genérico
/// fixo) a partir de templates parametrizados por Nome/Categoria/Subcategoria, sem depender de
/// nenhuma chave de API. Mesma filosofia do MockStreamingService: funciona de ponta a ponta
/// hoje, e é só trocar o registro em DependencyInjection.cs (gated por
/// AiContentGenerator:MockMode) por OpenAiContentGenerator quando houver uma chave real.
/// </summary>
public sealed class MockAiContentGenerator : IAiContentGenerator
{
    public Task<ProductCopySuggestion> GenerateProductCopyAsync(
        string productName, string? category, string? subcategory, CancellationToken ct = default)
    {
        var topic = subcategory ?? category ?? "essa área";
        var categoryLabel = category is not null ? $" de {category}" : string.Empty;

        var shortDescription =
            $"Aprenda {productName} do zero, com aulas práticas e direto ao ponto — ideal para quem quer resultado rápido em {topic}.";

        var fullDescription =
            $"\"{productName}\" foi criado para levar você do básico ao avançado{categoryLabel}, com uma metodologia " +
            $"prática e passo a passo. Ao longo do curso você vai construir projetos reais, entender os conceitos " +
            $"fundamentais de {topic} e ganhar confiança para aplicar o que aprendeu no seu dia a dia ou trabalho. " +
            "Não é necessário conhecimento prévio — o conteúdo foi pensado para iniciantes, mas também traz dicas " +
            "avançadas para quem já tem alguma experiência.";

        var benefits = new List<string>
        {
            $"Aprenda {topic} do zero, sem enrolação",
            "Aulas em vídeo, no seu ritmo, para sempre",
            "Projetos práticos para aplicar o conhecimento na hora",
            "Suporte direto com o criador do curso",
            "Certificado de conclusão"
        };

        var targetAudience =
            $"Pessoas iniciantes ou com pouca experiência em {topic} que querem aprender de forma prática e aplicar o conhecimento rapidamente.";

        var callToAction = $"Quero aprender {productName} agora";

        return Task.FromResult(new ProductCopySuggestion(
            shortDescription, fullDescription, benefits, targetAudience, callToAction));
    }

    public Task<SalesPageSuggestion> GenerateSalesPageAsync(
        string productName, string? category, string? shortDescription, decimal price,
        CancellationToken ct = default)
    {
        var topic = category ?? "essa área";

        var headline = $"Domine {productName} e transforme seu conhecimento em resultado";
        var subheadline = shortDescription is not null
            ? shortDescription
            : $"O curso completo de {topic}, direto ao ponto, para você aprender no seu ritmo.";

        var benefits = new List<string>
        {
            "Conteúdo 100% prático, direto ao ponto",
            "Acesso vitalício às aulas",
            "Atualizações incluídas sem custo extra",
            "Certificado de conclusão",
            "Garantia de satisfação"
        };

        var faq = new List<SalesPageFaqSuggestion>
        {
            new("Preciso ter experiência prévia?", "Não — o curso foi pensado para levar você do zero ao avançado."),
            new("Por quanto tempo tenho acesso?", "O acesso é vitalício, você pode assistir quando e quantas vezes quiser."),
            new("Como funciona o pagamento?",
                price > 0 ? "O pagamento é único, via PIX, cartão ou boleto." : "Este curso é gratuito."),
            new("Tem certificado?", "Sim, você recebe um certificado de conclusão ao terminar o curso.")
        };

        var callToAction = price > 0 ? $"Quero garantir minha vaga por R$ {price:0.00}" : "Quero começar agora, é grátis";

        return Task.FromResult(new SalesPageSuggestion(headline, subheadline, benefits, faq, callToAction));
    }
}
