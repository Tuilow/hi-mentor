using Tuilow.SharedKernel.Application.Exceptions;
using Tuilow.Catalog.Domain.Interfaces;
using Tuilow.Sales.Domain.Interfaces;
using Tuilow.Streaming.Application.Interfaces;
using Tuilow.Streaming.Domain.Interfaces;
using MediatR;

namespace Tuilow.Streaming.Application.Queries.GetLessonPlayUrl;

public sealed class GetLessonPlayUrlQueryHandler(
    ICourseRepository courseRepository,
    IVideoRepository videoRepository,
    ISubscriptionRepository subscriptionRepository,
    IStreamingService streamingService
) : IRequestHandler<GetLessonPlayUrlQuery, LessonPlayUrlResponse>
{
    public async Task<LessonPlayUrlResponse> Handle(GetLessonPlayUrlQuery request, CancellationToken ct)
    {
        // 1. Localiza o curso e a aula
        var course = await courseRepository.GetByIdAsync(request.CourseId, ct)
            ?? throw new NotFoundException("Curso", request.CourseId);

        var lesson = course.Modules
            .SelectMany(m => m.Lessons)
            .FirstOrDefault(l => l.Id == request.LessonId)
            ?? throw new NotFoundException("Aula", request.LessonId);

        if (!lesson.VideoId.HasValue)
            throw new BusinessException("Esta aula ainda não possui vídeo vinculado.");

        // 2. Busca o vídeo
        var video = await videoRepository.GetByIdAsync(lesson.VideoId.Value, ct)
            ?? throw new NotFoundException("Vídeo", lesson.VideoId.Value);

        if (string.IsNullOrEmpty(video.CloudflareVideoId))
            throw new BusinessException("O vídeo ainda está sendo processado. Tente novamente em instantes.");

        // 3. Controle de acesso
        if (!lesson.IsPreview)
        {
            // Conteúdo pago — exige usuário autenticado com assinatura ativa
            if (request.CurrentUserId is null)
                throw new UnauthorizedException("Faça login para assistir este conteúdo.");

            var subscription = await subscriptionRepository.GetActiveByUserAsync(request.CurrentUserId.Value, ct);
            if (subscription is null || !subscription.IsActive)
                throw new ForbiddenException("Assine um plano para ter acesso a este conteúdo.");

            // Retorna URL assinada com JWT (expira em 4h)
            var signedUrl = await streamingService.GetSignedPlaybackUrlAsync(
                video.CloudflareVideoId, expirationMinutes: 240, ct);

            return new LessonPlayUrlResponse(
                lesson.Id, lesson.Title, false,
                signedUrl, video.DurationSeconds, video.ThumbnailUrl);
        }

        // 4. Preview — delega ao streamingService (mock retorna vídeo de amostra; produção retorna URL pública)
        var previewUrl = await streamingService.GetSignedPlaybackUrlAsync(
            video.CloudflareVideoId, expirationMinutes: 240, ct);

        return new LessonPlayUrlResponse(
            lesson.Id, lesson.Title, true,
            previewUrl, video.DurationSeconds, video.ThumbnailUrl);
    }
}
