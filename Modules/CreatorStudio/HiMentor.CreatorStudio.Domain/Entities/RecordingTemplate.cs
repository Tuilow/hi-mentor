using HiMentor.SharedKernel.Domain.Common;

namespace HiMentor.CreatorStudio.Domain.Entities;

/// <summary>
/// Template de gravação reutilizável do criador (ex.: Vinheta inicial, Apresentação, Conteúdo,
/// Resumo, Chamada para ação). Ao criar uma nova aula no Estúdio, o template padrão do criador
/// é aplicado automaticamente como guia de estrutura no teleprompter/roteiro.
/// </summary>
public sealed class RecordingTemplate : AggregateRoot
{
    private readonly List<string> _sections = [];

    public Guid CreatorId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public IReadOnlyList<string> Sections => _sections.AsReadOnly();
    public bool IsDefault { get; private set; }

    private RecordingTemplate() { }

    public static RecordingTemplate Create(Guid creatorId, string name, IEnumerable<string> sections, bool isDefault = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var template = new RecordingTemplate
        {
            CreatorId = creatorId,
            Name = name.Trim(),
            IsDefault = isDefault
        };
        template._sections.AddRange(sections.Where(s => !string.IsNullOrWhiteSpace(s)));
        return template;
    }

    public void Update(string name, IEnumerable<string> sections)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name.Trim();
        _sections.Clear();
        _sections.AddRange(sections.Where(s => !string.IsNullOrWhiteSpace(s)));
        Touch();
    }

    /// <summary>Marca este como o template padrão do criador. Desmarcar os demais é responsabilidade do Application layer (precisa ver todos os templates do criador).</summary>
    public void SetAsDefault()
    {
        IsDefault = true;
        Touch();
    }

    public void UnsetAsDefault()
    {
        IsDefault = false;
        Touch();
    }
}
