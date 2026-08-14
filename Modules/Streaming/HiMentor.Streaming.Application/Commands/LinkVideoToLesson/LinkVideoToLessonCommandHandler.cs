using HiMentor.SharedKernel.Application.Exceptions;
using HiMentor.SharedKernel.Application.Interfaces;
using HiMentor.Catalog.Domain.Interfaces;
using HiMentor.Streaming.Domain.Interfaces;
using MediatR;

namespace HiMentor.Streaming.Application.Commands.LinkVideoToLesson;

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

        if (course.InstructorId != request.InstructorId)
            throw new ForbiddenException("Apenas o criador pode vincular vídeos a este produto.");

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
