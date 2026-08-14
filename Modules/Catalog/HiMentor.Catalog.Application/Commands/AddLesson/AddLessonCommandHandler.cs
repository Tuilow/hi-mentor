using HiMentor.SharedKernel.Application.Exceptions;
using HiMentor.SharedKernel.Application.Interfaces;
using HiMentor.Catalog.Domain.Interfaces;
using MediatR;

namespace HiMentor.Catalog.Application.Commands.AddLesson;

public sealed class AddLessonCommandHandler(
    ICourseRepository courseRepository, IUnitOfWork uow
) : IRequestHandler<AddLessonCommand, Guid>
{
    public async Task<Guid> Handle(AddLessonCommand request, CancellationToken ct)
    {
        var course = await courseRepository.GetByIdAsync(request.CourseId, ct)
            ?? throw new NotFoundException("Curso", request.CourseId);

        if (course.InstructorId != request.InstructorId)
            throw new ForbiddenException("Apenas o criador pode adicionar aulas a este produto.");

        var module = course.Modules.SingleOrDefault(m => m.Id == request.ModuleId)
            ?? throw new NotFoundException("Módulo", request.ModuleId);

        var lesson = module.AddLesson(request.Title, request.Description, request.IsPreview);

        // Registra explicitamente como Added — evita DbUpdateConcurrencyException
        await courseRepository.AddLessonAsync(lesson, ct);

        await uow.SaveChangesAsync(ct);
        return lesson.Id;
    }
}
