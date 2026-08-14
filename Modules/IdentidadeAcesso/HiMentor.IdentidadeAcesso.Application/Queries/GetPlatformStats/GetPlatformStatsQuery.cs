using MediatR;

namespace HiMentor.IdentidadeAcesso.Application.Queries.GetPlatformStats;

/// <summary>Visão geral do painel do dono da plataforma: contagens de usuários, criadores e conteúdo.</summary>
public sealed record GetPlatformStatsQuery : IRequest<PlatformStatsResponse>;
