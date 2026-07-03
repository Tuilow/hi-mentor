using Tuilow.SharedKernel.Application.Interfaces;
using Tuilow.Streaming.Application.Interfaces;
using Tuilow.Streaming.Domain.Entities;
using Tuilow.Streaming.Domain.Interfaces;
using MediatR;

namespace Tuilow.Streaming.Application.Commands.ImportExternalVideo;

public sealed class ImportExternalVideoCommandHandler(
    IMediaImportService mediaImportService,
    IVideoRepository videoRepository,
    IUnitOfWork uow
) : IRequestHandler<ImportExternalVideoCommand, ImportExternalVideoResponse>
{
    public async Task<ImportExternalVideoResponse> Handle(ImportExternalVideoCommand request, CancellationToken ct)
    {
        var metadata = await mediaImportService.FetchMetadataAsync(request.Url, ct);

        var video = Video.CreateFromExternal(
            metadata.Source, metadata.ExternalUrl, metadata.ExternalId,
            metadata.Title, metadata.DurationSeconds, metadata.ThumbnailUrl);

        await videoRepository.AddAsync(video, ct);
        await uow.SaveChangesAsync(ct);

        // Mesmo formato de resultado do upload (VideoId) — o passo seguinte do wizard
        // (vincular à aula) reaproveita o LinkVideoToLessonCommand já existente, sem distinguir
        // se o vídeo veio de upload ou importação.
        return new ImportExternalVideoResponse(
            video.Id, video.Source.ToString(), video.Title, video.ThumbnailUrl, video.DurationSeconds);
    }
}
