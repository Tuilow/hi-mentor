using Tuilow.SharedKernel.Application.Exceptions;
using Tuilow.SharedKernel.Application.Interfaces;
using Tuilow.Catalog.Domain.Interfaces;
using Tuilow.Streaming.Application.Interfaces;
using Tuilow.Streaming.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Tuilow.Streaming.Application.Commands.DeleteVideo;

public sealed class DeleteVideoCommandHandler(
    IVideoRepository videoRepository,
    ICourseRepository courseRepository,
    IStreamingService streamingService,
    IUnitOfWork uow,
    ILogger<DeleteVideoCommandHandler> logger
) : IRequestHandler<DeleteVideoCommand>
{
    public async Task Handle(DeleteVideoCommand request, CancellationToken ct)
    {
        var video = await videoRepository.GetByIdAsync(request.VideoId, ct)
            ?? throw new NotFoundException("Vídeo", request.VideoId);

        if (video.CourseId is null)
            throw new ForbiddenException("Este vídeo não pertence a você.");

        var course = await courseRepository.GetByIdAsync(video.CourseId.Value, ct)
            ?? throw new NotFoundException("Curso", video.CourseId.Value);

        if (course.InstructorId != request.InstructorId)
            throw new ForbiddenException("Este vídeo não pertence a você.");

        var isLinked = course.Modules.SelectMany(m => m.Lessons).Any(l => l.VideoId == video.Id);
        if (isLinked)
            throw new BusinessException("Este vídeo já está vinculado a uma aula — desvincule antes de remover.");

        // Se o vídeo já tem um arquivo hospedado no Cloudflare Stream (upload direto ou
        // download do YouTube que chegou a subir), tenta remover de lá também, pra não deixar
        // armazenamento órfão sendo cobrado. Uma falha aqui não deve travar a remoção do
        // registro local — o criador está tentando limpar a lista, não faz sentido bloquear
        // por causa de uma instabilidade da API do Cloudflare.
        if (!string.IsNullOrEmpty(video.CloudflareVideoId))
        {
            try
            {
                await streamingService.DeleteVideoAsync(video.CloudflareVideoId, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "Falha ao remover o vídeo {CloudflareVideoId} do Cloudflare Stream (Video {VideoId}) — removendo só o registro local.",
                    video.CloudflareVideoId, video.Id);
            }
        }

        videoRepository.Delete(video);
        await uow.SaveChangesAsync(ct);
    }
}
