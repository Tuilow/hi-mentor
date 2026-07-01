using Tuilow.Application.Common.Interfaces;
using Tuilow.Domain.Common.Interfaces;
using Tuilow.Domain.Contexts.Streaming.Entities;
using Tuilow.Domain.Contexts.Streaming.Interfaces;
using MediatR;

namespace Tuilow.Application.Contexts.Streaming.Commands.GetVideoUploadUrl;

public sealed class GetVideoUploadUrlCommandHandler(
    IStreamingService streamingService,
    IVideoRepository videoRepository,
    IUnitOfWork uow
) : IRequestHandler<GetVideoUploadUrlCommand, VideoUploadUrlResponse>
{
    public async Task<VideoUploadUrlResponse> Handle(GetVideoUploadUrlCommand request, CancellationToken ct)
    {
        // Obtém o slot de upload direto do Cloudflare Stream
        var upload = await streamingService.GetDirectUploadUrlAsync(ct);

        // Cria entidade Video e persiste com o CloudflareVideoId já definido
        var video = Video.Create();
        video.SetCloudflareVideoId(upload.CloudflareVideoId); // Status = Processing

        await videoRepository.AddAsync(video, ct);
        await uow.SaveChangesAsync(ct);

        return new VideoUploadUrlResponse(video.Id, upload.CloudflareVideoId, upload.UploadUrl);
    }
}
