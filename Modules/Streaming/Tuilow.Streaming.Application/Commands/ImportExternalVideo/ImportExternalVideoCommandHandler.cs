using Tuilow.SharedKernel.Application.Exceptions;
using Tuilow.SharedKernel.Application.Interfaces;
using Tuilow.Catalog.Domain.Interfaces;
using Tuilow.Streaming.Application.Interfaces;
using Tuilow.Streaming.Domain.Entities;
using Tuilow.Streaming.Domain.Interfaces;
using MediatR;

namespace Tuilow.Streaming.Application.Commands.ImportExternalVideo;

public sealed class ImportExternalVideoCommandHandler(
    IMediaImportService mediaImportService,
    IVideoRepository videoRepository,
    ICourseRepository courseRepository,
    IUnitOfWork uow
) : IRequestHandler<ImportExternalVideoCommand, ImportExternalVideoResponse>
{
    public async Task<ImportExternalVideoResponse> Handle(ImportExternalVideoCommand request, CancellationToken ct)
    {
        var course = await courseRepository.GetByIdAsync(request.CourseId, ct)
            ?? throw new NotFoundException("Curso", request.CourseId);

        if (course.InstructorId != request.InstructorId)
            throw new ForbiddenException("Apenas o criador pode importar vídeos para este produto.");

        var metadata = await mediaImportService.FetchMetadataAsync(request.Url, ct);

        var video = Video.CreateFromExternal(
            metadata.Source, metadata.ExternalUrl, metadata.ExternalId,
            metadata.Title, metadata.DurationSeconds, metadata.ThumbnailUrl, request.CourseId);

        await videoRepository.AddAsync(video, ct);
        await uow.SaveChangesAsync(ct);

        // Mesmo formato de resultado do upload (VideoId) — o passo seguinte do wizard
        // (vincular à aula) reaproveita o LinkVideoToLessonCommand já existente, sem distinguir
        // se o vídeo veio de upload ou importação.
        return new ImportExternalVideoResponse(
            video.Id, video.Source.ToString(), video.Title, video.ThumbnailUrl, video.DurationSeconds);
    }
}
