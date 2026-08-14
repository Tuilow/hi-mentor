using HiMentor.CreatorStudio.Domain.Enums;
using MediatR;

namespace HiMentor.CreatorStudio.Application.Queries.GetCreatorStyleProfile;

/// <summary>Tela do Estúdio do Criador — perfil de nicho salvo (null se o criador ainda não preencheu) + progresso do Clone do Professor.</summary>
public sealed record GetCreatorStyleProfileQuery(Guid CreatorId) : IRequest<CreatorStyleProfileResponse?>;

public sealed record CreatorStyleProfileResponse(
    Guid Id,
    string Niche,
    string TargetAudience,
    string Objective,
    AudienceLevel Level,
    int RecordedScriptsCount,
    int ScriptsRequiredForClone,
    bool IsCloneReady
);
