namespace Tuilow.IdentidadeAcesso.Application.Queries.GetPlatformStats;

public sealed record PlatformStatsResponse(
    int TotalUsers,
    int ActiveUsers,
    int SuspendedUsers,
    int TotalCreators,
    int ActiveLast24h,
    int ActiveLast7d,
    int TotalPublishedCourses,
    int TotalVideos
);
