using HiMentor.SharedKernel.Application.Exceptions;
using HiMentor.SharedKernel.Application.Interfaces;
using HiMentor.Catalog.Domain.Interfaces;
using HiMentor.Streaming.Application.Interfaces;
using HiMentor.Streaming.Domain.Entities;
using HiMentor.Streaming.Domain.Interfaces;
using MediatR;

namespace HiMentor.Streaming.Application.Commands.GetVideoUploadUrl;

public sealed class GetVideoUploadUrlCommandHandler(
    IStreamingService streamingService,
    IVideoRepository videoRepository,
    ICourseRepository courseRepository,
    IUnitOfWork uow
) : IRequestHandler<GetVideoUploadUrlCommand, VideoUploadUrlResponse>
{
    public async Task<VideoUploadUrlResponse> Handle(GetVideoUploadUrlCommand request, CancellationToken ct)
    {
        var course = await courseRepository.GetByIdAsync(request.CourseId, ct)
            ?? throw new NotFoundException("Curso", request.CourseId);

        if (course.InstructorId != request.InstructorId)
            throw new ForbiddenException("Apenas o criador pode enviar vídeos para este produto.");

        // Obtém o slot de upload direto do Cloudflare Stream
        var upload = await streamingService.GetDirectUploadUrlAsync(ct);

        // Cria entidade Video e persiste com o CloudflareVideoId já definido
        var video = Video.Create(request.CourseId, request.Title);
        video.SetCloudflareVideoId(upload.CloudflareVideoId); // Status = Processing

        await videoRepository.AddAsync(video, ct);
        await uow.SaveChangesAsync(ct);

        return new VideoUploadUrlResponse(video.Id, upload.CloudflareVideoId, upload.UploadUrl);
    }
}
