using HiMentor.SharedKernel.Application.Exceptions;
using HiMentor.SharedKernel.Application.Interfaces;
using HiMentor.Catalog.Domain.Interfaces;
using MediatR;

namespace HiMentor.Catalog.Application.Commands.UpdateLesson;

public sealed class UpdateLessonCommandHandler(
    ICourseRepository courseRepository, IUnitOfWork uow
) : IRequestHandler<UpdateLessonCommand>
{
    public async Task Handle(UpdateLessonCommand request, CancellationToken ct)
    {
        var course = await courseRepository.GetByIdAsync(request.CourseId, ct)
            ?? throw new NotFoundException("Curso", request.CourseId);

        if (course.InstructorId != request.InstructorId)
            throw new ForbiddenException("Apenas o criador pode editar aulas deste produto.");

        var module = course.Modules.SingleOrDefault(m => m.Id == request.ModuleId)
            ?? throw new NotFoundException("Módulo", request.ModuleId);

        var lesson = module.Lessons.SingleOrDefault(l => l.Id == request.LessonId)
            ?? throw new NotFoundException("Aula", request.LessonId);

        lesson.UpdateDetails(request.Title, request.Description);

        // Entidade já veio rastreada pelo ChangeTracker via GetByIdAsync (Include) — mudar uma
        // propriedade escalar dela e chamar SaveChangesAsync basta, sem precisar de Update()
        // explícito (mesmo padrão de ReorderLessonsCommandHandler.Reorder).
        await uow.SaveChangesAsync(ct);
    }
}
