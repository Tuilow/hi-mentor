using Tuilow.CreatorStudio.Application.Interfaces;
using Tuilow.CreatorStudio.Domain.Enums;

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

    public Task<MarketingCopySuggestion> GenerateMarketingCopyAsync(
        string productName, MarketingChannel channel, string? category, string? shortDescription,
        IReadOnlyList<string> benefits, decimal price, CancellationToken ct = default)
    {
        var topic = category ?? "essa área";
        var priceLabel = price > 0 ? $"por R$ {price:0.00}" : "de graça";
        var topBenefit = benefits.Count > 0 ? benefits[0] : $"aprender {topic} do zero";

        var suggestion = channel switch
        {
            MarketingChannel.InstagramPost => new MarketingCopySuggestion(
                $"🚀 {productName}\n\n" +
                $"Quer {topBenefit.ToLowerInvariant()}? Chegou o curso que faltava.\n\n" +
                $"✅ {(benefits.Count > 0 ? string.Join("\n✅ ", benefits.Take(3)) : $"Aulas práticas de {topic}")}\n\n" +
                $"Vagas abertas {priceLabel}. Link na bio 👆\n\n" +
                $"#{Slugify(productName)} #{Slugify(topic)} #cursoonline #aprenda{Slugify(topic)}",
                "Quero começar agora →"),

            MarketingChannel.InstagramStory => new MarketingCopySuggestion(
                $"👀 {productName}\n\n{topBenefit}\n\nArrasta pra cima e garante sua vaga ⬆️",
                "Arrasta pra cima →"),

            MarketingChannel.WhatsApp => new MarketingCopySuggestion(
                $"Oi! 👋 Bora {topBenefit.ToLowerInvariant()}?\n\n" +
                $"Lancei o curso *{productName}* {priceLabel}, com aulas práticas de {topic} do zero.\n\n" +
                "Só vou avisar aqui uma vez, quem quiser entrar é só clicar no link 👇",
                "Quero saber mais"),

            MarketingChannel.Email => new MarketingCopySuggestion(
                $"Assunto: {productName} — {(price > 0 ? "vagas abertas" : "acesso grátis liberado")}\n\n" +
                $"Olá!\n\nSe você quer {topBenefit.ToLowerInvariant()}, o curso *{productName}* foi feito pra você.\n\n" +
                $"Nele você vai encontrar:\n- {string.Join("\n- ", benefits.Count > 0 ? benefits.Take(4) : [$"Conteúdo prático de {topic}"])}\n\n" +
                $"As vagas estão abertas {priceLabel}. Clique no link abaixo para garantir a sua.\n\nUm abraço.",
                "Garantir minha vaga"),

            MarketingChannel.MetaAds => new MarketingCopySuggestion(
                $"Título: {productName} — {topBenefit}\n" +
                $"Texto principal: Aprenda {topic} do zero, no seu ritmo, com aulas 100% práticas. " +
                $"Inscrições abertas {priceLabel}.\n" +
                "Descrição: Vagas limitadas — comece hoje mesmo.",
                "Saiba mais"),

            MarketingChannel.Headline => new MarketingCopySuggestion(
                $"Aprenda {productName} do Zero\n" +
                $"Método Validado de {productName}\n" +
                $"{productName}: Domine {topic} em Poucas Semanas\n" +
                "Vagas Limitadas — Garanta a Sua Agora",
                null),

            _ => new MarketingCopySuggestion($"{productName} — {topBenefit}", "Saiba mais")
        };

        return Task.FromResult(suggestion);
    }

    public Task<CourseOutlineSuggestion> GenerateCourseOutlineAsync(
        string niche, string targetAudience, string objective, AudienceLevel level, CancellationToken ct = default)
    {
        var tone = NicheTone(niche);
        var levelLabel = level switch
        {
            AudienceLevel.Beginner => "iniciante",
            AudienceLevel.Intermediate => "intermediário",
            _ => "avançado",
        };

        var courseName = $"{niche}: {objective}";
        var courseDescription =
            $"Curso pensado para {targetAudience.ToLowerInvariant()}, nível {levelLabel}, com foco em {objective.ToLowerInvariant()}. " +
            $"{tone.Intro}";

        var modules = new List<CourseOutlineModule>
        {
            new($"Módulo 1 - Fundamentos de {niche}",
            [
                new CourseOutlineLesson($"Os principais conceitos de {niche.ToLowerInvariant()}", "Teórica"),
                new CourseOutlineLesson("Erros comuns e como evitá-los", "Teórica"),
                new CourseOutlineLesson("Definindo metas realistas", "Teórica"),
            ]),
            new($"Módulo 2 - Colocando em prática",
            [
                new CourseOutlineLesson($"Primeiros passos para {levelLabel}s", "Prática"),
                new CourseOutlineLesson("Aplicando na rotina do dia a dia", "Prática"),
                new CourseOutlineLesson("Estudo de caso real", "Estudo de caso"),
            ]),
            new("Módulo 3 - Consolidando resultados",
            [
                new CourseOutlineLesson("Acompanhando sua evolução", "Teórica"),
                new CourseOutlineLesson("Próximos passos e recursos extras", "Teórica"),
            ]),
        };

        return Task.FromResult(new CourseOutlineSuggestion(courseName, courseDescription, modules));
    }

    public Task<LessonScriptSuggestion> GenerateLessonScriptAsync(
        string lessonTitle, string niche, string targetAudience, AudienceLevel level, CancellationToken ct = default)
    {
        var tone = NicheTone(niche);

        var introduction =
            $"Olá, seja bem-vindo(a)! {tone.Greeting} Nesta aula você vai aprender sobre \"{lessonTitle}\" — " +
            $"conteúdo pensado especialmente para {targetAudience.ToLowerInvariant()}.";

        var developmentTopics = new List<string>
        {
            $"Explique o conceito central de \"{lessonTitle}\" com suas palavras",
            $"{tone.DevelopmentHint}",
            "Traga um exemplo prático do dia a dia",
            "Destaque os erros mais comuns nesse tema",
        };

        var demonstrationSuggestions = new List<string>
        {
            tone.DemonstrationHint,
            "Grave um passo a passo mostrando na prática o que foi explicado",
        };

        var closingCta =
            $"{tone.ClosingHint} Na próxima aula vamos continuar evoluindo em {niche.ToLowerInvariant()} — não perca!";

        return Task.FromResult(new LessonScriptSuggestion(
            introduction, developmentTopics, demonstrationSuggestions, closingCta));
    }

    private sealed record NicheToneProfile(
        string Intro, string Greeting, string DevelopmentHint, string DemonstrationHint, string ClosingHint);

    /// <summary>
    /// IA especialista por nicho (item 11 do Estúdio do Criador): adapta tom/linguagem/exemplos
    /// por palavra-chave do nicho informado. Classificação simples por Contains — o provider
    /// real (OpenAI) faz isso via prompt, aqui é um mock determinístico sem chamada de rede.
    /// </summary>
    private static NicheToneProfile NicheTone(string niche)
    {
        var n = niche.ToLowerInvariant();

        if (n.Contains("personal") || n.Contains("treino") || n.Contains("fitness") || n.Contains("academia"))
            return new NicheToneProfile(
                "Aulas com linguagem motivacional, pensadas para gerar resultado real no corpo e na rotina do aluno.",
                "Bora com tudo!",
                "Demonstre a execução correta do movimento ou técnica",
                "Grave a demonstração física do exercício/técnica, de frente e de lado, em ambiente bem iluminado",
                "Você é capaz — continue firme!");

        if (n.Contains("advoga") || n.Contains("direito") || n.Contains("jurídic"))
            return new NicheToneProfile(
                "Conteúdo com linguagem formal e tecnicamente precisa, com referência a casos e legislação aplicável.",
                "Vamos analisar este tema com o rigor que ele exige.",
                "Cite a base legal ou jurisprudência relevante ao tema",
                "Apresente um caso jurídico real (anonimizado) que ilustre o conceito",
                "Consulte sempre um profissional para o seu caso concreto.");

        if (n.Contains("nutri") || n.Contains("dieta") || n.Contains("alimenta"))
            return new NicheToneProfile(
                "Aulas com linguagem acolhedora e baseada em evidências, sem promessas milagrosas.",
                "Vamos falar sobre isso com calma e carinho.",
                "Explique o embasamento científico por trás da recomendação",
                "Mostre exemplos reais de pratos/cardápios aplicando o conceito",
                "Cuide-se — pequenos passos consistentes fazem toda a diferença.");

        if (n.Contains("inglês") || n.Contains("idioma") || n.Contains("professor") || n.Contains("ensino"))
            return new NicheToneProfile(
                "Aulas com linguagem didática, repletas de exemplos e oportunidades de prática.",
                "Vamos aprender juntos, passo a passo.",
                "Dê pelo menos dois exemplos de uso no contexto real",
                "Grave um exercício de fixação guiado, com pausa para o aluno responder",
                "Pratique o que aprendeu hoje antes da próxima aula!");

        if (n.Contains("financ") || n.Contains("investi") || n.Contains("consultor"))
            return new NicheToneProfile(
                "Conteúdo direto e orientado a resultado, com exemplos numéricos claros.",
                "Vamos direto ao ponto.",
                "Traga um exemplo numérico simples ilustrando o conceito",
                "Mostre uma planilha ou simulação real na tela",
                "Coloque isso em prática ainda esta semana.");

        return new NicheToneProfile(
            "Aulas práticas e diretas ao ponto, pensadas para gerar resultado rápido para o aluno.",
            "Vamos direto ao que interessa.",
            "Aprofunde o conceito com um exemplo prático",
            "Grave uma demonstração prática do que foi explicado",
            "Continue praticando — o próximo passo está logo ali.");
    }

    private static string Slugify(string text) =>
        new(text.Trim().ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
}
