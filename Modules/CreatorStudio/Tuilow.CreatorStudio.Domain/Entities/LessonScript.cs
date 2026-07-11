using Tuilow.SharedKernel.Domain.Common;

namespace Tuilow.CreatorStudio.Domain.Entities;

/// <summary>
/// Roteiro de gravação gerado pela IA para uma aula (Gerador de Roteiros do Estúdio do
/// Criador): introdução, tópicos de desenvolvimento, sugestões de demonstração prática e
/// call-to-action de encerramento. Pode existir ANTES de a aula real existir no Catalog — o
/// criador pode gerar/salvar o roteiro e só depois criar o curso/aula de fato (por isso CourseId
/// e LessonId são opcionais; ver LinkToLesson). Sem FK real para Catalog — mesmo padrão de
/// referência solta por Guid já usado por Lead.
///
/// WasRecorded alimenta o "Clone do Professor": cada roteiro marcado como usado numa gravação
/// real conta para o total exigido (CreatorStyleProfile.ScriptsRequiredForClone).
/// </summary>
public sealed class LessonScript : AggregateRoot
{
    private readonly List<string> _developmentTopics = [];
    private readonly List<string> _demonstrationSuggestions = [];

    public Guid CreatorId { get; private set; }
    public Guid? CourseId { get; private set; }
    public Guid? LessonId { get; private set; }
    public string LessonTitle { get; private set; } = string.Empty;
    public string Introduction { get; private set; } = string.Empty;
    public IReadOnlyList<string> DevelopmentTopics => _developmentTopics.AsReadOnly();
    public IReadOnlyList<string> DemonstrationSuggestions => _demonstrationSuggestions.AsReadOnly();
    public string ClosingCta { get; private set; } = string.Empty;
    public bool WasRecorded { get; private set; }
    public DateTime? RecordedAt { get; private set; }

    private LessonScript() { }

    public static LessonScript Create(
        Guid creatorId, string lessonTitle, string introduction,
        IEnumerable<string> developmentTopics, IEnumerable<string> demonstrationSuggestions,
        string closingCta, Guid? courseId = null, Guid? lessonId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(lessonTitle);

        var script = new LessonScript
        {
            CreatorId = creatorId,
            LessonTitle = lessonTitle.Trim(),
            Introduction = introduction?.Trim() ?? string.Empty,
            ClosingCta = closingCta?.Trim() ?? string.Empty,
            CourseId = courseId,
            LessonId = lessonId
        };

        script._developmentTopics.AddRange(developmentTopics.Where(t => !string.IsNullOrWhiteSpace(t)));
        script._demonstrationSuggestions.AddRange(demonstrationSuggestions.Where(t => !string.IsNullOrWhiteSpace(t)));

        return script;
    }

    /// <summary>Vincula o roteiro a uma aula real do Catalog depois de o criador criar o curso a partir da estrutura sugerida.</summary>
    public void LinkToLesson(Guid courseId, Guid lessonId)
    {
        CourseId = courseId;
        LessonId = lessonId;
        Touch();
    }

    /// <summary>Marca que o criador gravou usando este roteiro — conta para o progresso do Clone do Professor.</summary>
    public void MarkAsRecorded()
    {
        if (WasRecorded) return;
        WasRecorded = true;
        RecordedAt = DateTime.UtcNow;
        Touch();
    }
}
