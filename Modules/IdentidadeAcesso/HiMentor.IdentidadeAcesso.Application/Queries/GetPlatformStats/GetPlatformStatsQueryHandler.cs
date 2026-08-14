using HiMentor.IdentidadeAcesso.Domain.Interfaces;
using HiMentor.Catalog.Domain.Interfaces;
using HiMentor.Streaming.Domain.Interfaces;
using MediatR;

namespace HiMentor.IdentidadeAcesso.Application.Queries.GetPlatformStats;

/// <summary>
/// Referencia ICourseRepository (Catalog) e IVideoRepository (Streaming) diretamente — mesmo
/// padrão de acoplamento entre módulos já usado por DeleteVideoCommandHandler (Streaming
/// referenciando ICourseRepository do Catalog).
/// </summary>
public sealed class GetPlatformStatsQueryHandler(
    IUserRepository userRepository,
    ICourseRepository courseRepository,
    IVideoRepository videoRepository
) : IRequestHandler<GetPlatformStatsQuery, PlatformStatsResponse>
{
    public async Task<PlatformStatsResponse> Handle(GetPlatformStatsQuery request, CancellationToken ct)
    {
        var counts = await userRepository.GetCountsSnapshotAsync(ct);

        // Reaproveita ListPublishedAsync só pelo Total (sem carregar os itens) — evita criar um
        // método de contagem novo no Catalog para uma única leitura no painel do dono.
        var (_, totalPublishedCourses) = await courseRepository.ListPublishedAsync(
            level: null, search: null, page: 1, pageSize: 1, ct);

        var totalVideos = (await videoRepository.GetAllAsync(ct)).Count();

        return new PlatformStatsResponse(
            counts.TotalUsers,
            counts.ActiveUsers,
            counts.SuspendedUsers,
            counts.TotalCreators,
            counts.ActiveLast24h,
            counts.ActiveLast7d,
            totalPublishedCourses,
            totalVideos);
    }
}
