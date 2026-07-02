using Tuilow.SharedKernel.Application.Exceptions;
using Tuilow.SharedKernel.Application.Interfaces;
using Tuilow.Catalog.Domain.Interfaces;
using Tuilow.Streaming.Domain.Interfaces;
using MediatR;

namespace Tuilow.Streaming.Application.Commands.LinkVideoToLesson;

public sealed class LinkVideoToLessonCommandHandler(
    ICourseRepository courseRepository,
    IVideoRepository videoRepository,
    IUnitOfWork uow
) : IRequestHandler<LinkVideoToLessonCommand>
{
    public async Task Handle(LinkVideoToLessonCommand request, CancellationToken ct)
    {
        var course = await courseRepository.GetByIdAsync(request.CourseId, ct)
            ?? throw new NotFoundException("Curso", request.CourseId);

        var module = course.Modules.SingleOrDefault(m => m.Id == request.ModuleId)
            ?? throw new NotFoundException("Módulo", request.ModuleId);

        var lesson = module.Lessons.SingleOrDefault(l => l.Id == request.LessonId)
            ?? throw new NotFoundException("Aula", request.LessonId);

        var video = await videoRepository.GetByIdAsync(request.VideoId, ct)
            ?? throw new NotFoundException("Vídeo", request.VideoId);

        // Vincula o vídeo à aula (duração pode ser 0 até o webhook do Cloudflare atualizar)
        lesson.SetVideo(video.Id, video.DurationSeconds ?? 0);

        // Define se a aula é preview gratuito ou exige assinatura
        if (request.IsPreview)
            lesson.SetAsPreview();

        courseRepository.Update(course);
        await uow.SaveChangesAsync(ct);
    }
}
