using HiMentor.SharedKernel.Application.Exceptions;
using HiMentor.SharedKernel.Application.Interfaces;
using HiMentor.Catalog.Domain.Interfaces;
using HiMentor.Streaming.Application.Interfaces;
using HiMentor.Streaming.Domain.Enums;
using HiMentor.Streaming.Domain.Interfaces;
using MediatR;

namespace HiMentor.Streaming.Application.Queries.GetLessonPlayUrl;

public sealed class GetLessonPlayUrlQueryHandler(
    ICourseRepository courseRepository,
    IVideoRepository videoRepository,
    IUserCourseAccessService courseAccessService,
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

        // Vídeo importado de plataforma externa (YouTube/Vimeo/Drive/...) sem pedido de download
        // já nasce Ready, sem CloudflareVideoId — a "URL de playback" é a própria URL externa
        // (embed), não passa pelo streamingService (que só sabe assinar URLs do Cloudflare
        // Stream/mock). Já um vídeo do YouTube BAIXADO (checkbox "baixar vídeo") ganha um
        // CloudflareVideoId assim que o YouTubeDownloadWorker termina de subir o arquivo — a
        // partir daí ele tem que ser tratado como um vídeo "nosso" (Cloudflare), não mais como
        // link externo, senão o aluno continuaria vendo a URL do YouTube mesmo depois do vídeo
        // já estar hospedado na plataforma.
        var isExternal = video.Source != VideoSource.Upload && string.IsNullOrEmpty(video.CloudflareVideoId);

        if (!isExternal && string.IsNullOrEmpty(video.CloudflareVideoId))
            throw new BusinessException("O vídeo ainda está sendo processado. Tente novamente em instantes.");

        // 3. Controle de acesso
        // O próprio criador do curso sempre pode assistir às próprias aulas, mesmo pagas —
        // nunca precisa comprar/assinar o próprio conteúdo.
        var isOwner = request.CurrentUserId.HasValue && course.InstructorId == request.CurrentUserId.Value;

        if (!lesson.IsPreview && !isOwner)
        {
            // Conteúdo pago — exige usuário autenticado com acesso pago (ou matrícula, se o curso for grátis).
            if (request.CurrentUserId is null)
                throw new UnauthorizedException("Faça login para assistir este conteúdo.");

            // Única checagem de acesso da plataforma (matrícula → compra avulsa → assinatura por
            // produto → assinatura legada da plataforma, nesta ordem) — ver
            // IUserCourseAccessService para a regra completa. Antes, este handler reimplementava
            // essa mesma checagem de forma duplicada e ligeiramente diferente da usada em
            // Learning/Channel; centralizar aqui elimina o risco de as duas regras divergirem.
            var hasAccess = await courseAccessService.HasAccessAsync(
                request.CurrentUserId.Value, request.CourseId, ct);

            if (!hasAccess)
                throw new ForbiddenException(course.IsFree
                    ? "Matricule-se neste curso gratuito para assistir a esta aula."
                    : "Compre o curso ou assine um plano para ter acesso a este conteúdo.");

            var paidUrl = isExternal
                ? video.ExternalUrl!
                : await streamingService.GetSignedPlaybackUrlAsync(video.CloudflareVideoId!, expirationMinutes: 240, ct);

            return new LessonPlayUrlResponse(
                lesson.Id, lesson.Title, false,
                paidUrl, video.DurationSeconds, video.ThumbnailUrl);
        }

        // 4. Preview, ou o próprio criador do curso — vídeo externo usa a própria URL; upload
        // delega ao streamingService (mock retorna vídeo de amostra; produção retorna URL
        // pública do Cloudflare Stream). IsPreview no response reflete o dado real da aula (não
        // fica "true" à toa quando quem está assistindo é o dono de uma aula paga).
        var previewUrl = isExternal
            ? video.ExternalUrl!
            : await streamingService.GetSignedPlaybackUrlAsync(video.CloudflareVideoId!, expirationMinutes: 240, ct);

        return new LessonPlayUrlResponse(
            lesson.Id, lesson.Title, lesson.IsPreview,
            previewUrl, video.DurationSeconds, video.ThumbnailUrl);
    }
}
