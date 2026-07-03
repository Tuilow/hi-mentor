using Tuilow.SharedKernel.Application.Exceptions;
using Tuilow.Catalog.Domain.Interfaces;
using Tuilow.Sales.Domain.Interfaces;
using Tuilow.Streaming.Application.Interfaces;
using Tuilow.Streaming.Domain.Enums;
using Tuilow.Streaming.Domain.Interfaces;
using MediatR;

namespace Tuilow.Streaming.Application.Queries.GetLessonPlayUrl;

public sealed class GetLessonPlayUrlQueryHandler(
    ICourseRepository courseRepository,
    IVideoRepository videoRepository,
    ICoursePurchaseRepository coursePurchaseRepository,
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

        // Vídeo importado de plataforma externa (YouTube/Vimeo/Drive/...) já nasce Ready, sem
        // CloudflareVideoId — a "URL de playback" é a própria URL externa (embed), não passa
        // pelo streamingService (que só sabe assinar URLs do Cloudflare Stream/mock).
        var isExternal = video.Source != VideoSource.Upload;

        if (!isExternal && string.IsNullOrEmpty(video.CloudflareVideoId))
            throw new BusinessException("O vídeo ainda está sendo processado. Tente novamente em instantes.");

        // 3. Controle de acesso
        if (!lesson.IsPreview)
        {
            // Conteúdo pago — exige usuário autenticado com acesso pago ao curso.
            if (request.CurrentUserId is null)
                throw new UnauthorizedException("Faça login para assistir este conteúdo.");

            // Mesma checagem de 3 caminhos usada em Learning.SalesCourseAccessChecker: compra
            // individual → assinatura por produto → assinatura legada da plataforma (fallback).
            // Sem isso, quem comprou o curso avulso ou assinou o plano do produto (mas nunca
            // teve assinatura da plataforma) não conseguia pegar a URL de playback mesmo já
            // tendo sido matriculado pelo Learning.
            var hasPaidAccess = await coursePurchaseRepository.HasConfirmedPurchaseAsync(
                request.CurrentUserId.Value, request.CourseId, ct);

            if (!hasPaidAccess)
            {
                var courseSubscription = await subscriptionRepository.GetActiveByUserForCourseAsync(
                    request.CurrentUserId.Value, request.CourseId, ct);
                hasPaidAccess = courseSubscription is not null && courseSubscription.IsActive;
            }

            if (!hasPaidAccess)
            {
                var subscription = await subscriptionRepository.GetActiveByUserAsync(request.CurrentUserId.Value, ct);
                hasPaidAccess = subscription is not null && subscription.IsActive;
            }

            if (!hasPaidAccess)
                throw new ForbiddenException("Compre o curso ou assine um plano para ter acesso a este conteúdo.");

            var paidUrl = isExternal
                ? video.ExternalUrl!
                : await streamingService.GetSignedPlaybackUrlAsync(video.CloudflareVideoId!, expirationMinutes: 240, ct);

            return new LessonPlayUrlResponse(
                lesson.Id, lesson.Title, false,
                paidUrl, video.DurationSeconds, video.ThumbnailUrl);
        }

        // 4. Preview — vídeo externo usa a própria URL; upload delega ao streamingService
        // (mock retorna vídeo de amostra; produção retorna URL pública do Cloudflare Stream).
        var previewUrl = isExternal
            ? video.ExternalUrl!
            : await streamingService.GetSignedPlaybackUrlAsync(video.CloudflareVideoId!, expirationMinutes: 240, ct);

        return new LessonPlayUrlResponse(
            lesson.Id, lesson.Title, true,
            previewUrl, video.DurationSeconds, video.ThumbnailUrl);
    }
}
