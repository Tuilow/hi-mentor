using HiMentor.SharedKernel.Application.Exceptions;
using HiMentor.Catalog.Domain.Interfaces;
using HiMentor.Streaming.Domain.Interfaces;
using MediatR;

namespace HiMentor.Streaming.Application.Queries.GetVideosByCourse;

public sealed class GetVideosByCourseQueryHandler(
    IVideoRepository videoRepository,
    ICourseRepository courseRepository
) : IRequestHandler<GetVideosByCourseQuery, IEnumerable<VideoSummaryResponse>>
{
    public async Task<IEnumerable<VideoSummaryResponse>> Handle(GetVideosByCourseQuery request, CancellationToken ct)
    {
        var course = await courseRepository.GetByIdAsync(request.CourseId, ct)
            ?? throw new NotFoundException("Curso", request.CourseId);

        if (course.InstructorId != request.InstructorId)
            throw new ForbiddenException("Apenas o criador pode ver os vídeos deste produto.");

        var videos = await videoRepository.ListByCourseAsync(request.CourseId, ct);
        var linkedVideoIds = course.Modules
            .SelectMany(m => m.Lessons)
            .Where(l => l.VideoId.HasValue)
            .Select(l => l.VideoId!.Value)
            .ToHashSet();

        return videos.Select(v => new VideoSummaryResponse(
            v.Id,
            v.Title,
            v.Source.ToString(),
            v.DurationSeconds,
            v.ThumbnailUrl,
            linkedVideoIds.Contains(v.Id),
            v.Status.ToString()));
    }
}
