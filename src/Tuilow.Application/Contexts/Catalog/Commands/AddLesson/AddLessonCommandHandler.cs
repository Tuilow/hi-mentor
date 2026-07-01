using Tuilow.Application.Common.Exceptions;
using Tuilow.Domain.Common.Interfaces;
using Tuilow.Domain.Contexts.Catalog.Interfaces;
using MediatR;

namespace Tuilow.Application.Contexts.Catalog.Commands.AddLesson;

public sealed class AddLessonCommandHandler(
    ICourseRepository courseRepository, IUnitOfWork uow
) : IRequestHandler<AddLessonCommand, Guid>
{
    public async Task<Guid> Handle(AddLessonCommand request, CancellationToken ct)
    {
        var course = await courseRepository.GetByIdAsync(request.CourseId, ct)
            ?? throw new NotFoundException("Curso", request.CourseId);

        var module = course.Modules.SingleOrDefault(m => m.Id == request.ModuleId)
            ?? throw new NotFoundException("Módulo", request.ModuleId);

        var lesson = module.AddLesson(request.Title, request.Description, request.IsPreview);

        // Registra explicitamente como Added — evita DbUpdateConcurrencyException
        await courseRepository.AddLessonAsync(lesson, ct);

        await uow.SaveChangesAsync(ct);
        return lesson.Id;
    }
}
