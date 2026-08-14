using HiMentor.SharedKernel.Domain.Common;
using HiMentor.CreatorStudio.Domain.Enums;

namespace HiMentor.CreatorStudio.Domain.Entities;

/// <summary>
/// Perfil de nicho do criador no Estúdio do Criador — nicho principal, público-alvo, objetivo
/// do curso e nível dos alunos. É o contexto usado pela IA para gerar estrutura de curso e
/// roteiros de aula "especialistas" naquele nicho (linguagem motivacional para Personal
/// Trainer, formal para Advogado, didática para Professor, etc.).
///
/// Também é a base de dados do "Clone do Professor" (funcionalidade premium): a contagem de
/// roteiros marcados como gravados (ver LessonScript.WasRecorded, somado via
/// ILessonScriptRepository) determina quando o criador atinge o volume mínimo para a IA
/// aprender seu estilo. Por ora só ACUMULAMOS o dado — nenhuma lógica de "aprendizado" ainda
/// (decisão explícita: modelar o domínio agora, sem implementar o clone em si nesta rodada).
/// </summary>
public sealed class CreatorStyleProfile : AggregateRoot
{
    /// <summary>Quantidade de roteiros gravados necessária para o Clone do Professor ficar disponível.</summary>
    public const int ScriptsRequiredForClone = 20;

    public Guid CreatorId { get; private set; }
    public string Niche { get; private set; } = string.Empty;
    public string TargetAudience { get; private set; } = string.Empty;
    public string Objective { get; private set; } = string.Empty;
    public AudienceLevel Level { get; private set; }

    private CreatorStyleProfile() { }

    public static CreatorStyleProfile Create(
        Guid creatorId, string niche, string targetAudience, string objective, AudienceLevel level)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(niche);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetAudience);
        ArgumentException.ThrowIfNullOrWhiteSpace(objective);

        return new CreatorStyleProfile
        {
            CreatorId = creatorId,
            Niche = niche.Trim(),
            TargetAudience = targetAudience.Trim(),
            Objective = objective.Trim(),
            Level = level
        };
    }

    public void Update(string niche, string targetAudience, string objective, AudienceLevel level)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(niche);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetAudience);
        ArgumentException.ThrowIfNullOrWhiteSpace(objective);

        Niche = niche.Trim();
        TargetAudience = targetAudience.Trim();
        Objective = objective.Trim();
        Level = level;
        Touch();
    }
}
