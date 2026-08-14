using HiMentor.CreatorStudio.Domain.Enums;
using MediatR;

namespace HiMentor.CreatorStudio.Application.Commands.SetCreatorNiche;

/// <summary>
/// Estúdio do Criador, passo 1 — "Identificação do Nicho". Upsert: cria o perfil na primeira
/// vez, atualiza depois. Um único perfil por criador (mesmo padrão de UpsertChannelCommand).
/// </summary>
public sealed record SetCreatorNicheCommand(
    Guid CreatorId,
    string Niche,
    string TargetAudience,
    string Objective,
    AudienceLevel Level
) : IRequest<Guid>;
