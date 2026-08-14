using MediatR;

namespace HiMentor.CreatorStudio.Application.Commands.SaveRecordingTemplate;

/// <summary>
/// Cria (TemplateId nulo) ou atualiza um template de gravação do criador. Se IsDefault=true,
/// desmarca os demais templates do mesmo criador (só pode haver um padrão por vez).
/// </summary>
public sealed record SaveRecordingTemplateCommand(
    Guid CreatorId,
    string Name,
    IReadOnlyList<string> Sections,
    bool IsDefault,
    Guid? TemplateId = null
) : IRequest<Guid>;
