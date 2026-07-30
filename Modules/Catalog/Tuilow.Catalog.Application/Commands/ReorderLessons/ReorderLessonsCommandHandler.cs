using Tuilow.SharedKernel.Application.Exceptions;
using Tuilow.SharedKernel.Application.Interfaces;
using Tuilow.Catalog.Domain.Interfaces;
using MediatR;

namespace Tuilow.Catalog.Application.Commands.ReorderLessons;

public sealed class ReorderLessonsCommandHandler(
    ICourseRepository courseRepository, IUnitOfWork uow
) : IRequestHandler<ReorderLessonsCommand>
{
    public async Task Handle(ReorderLessonsCommand request, CancellationToken ct)
    {
        var course = await courseRepository.GetByIdAsync(request.CourseId, ct)
            ?? throw new NotFoundException("Curso", request.CourseId);

        if (course.InstructorId != request.InstructorId)
            throw new ForbiddenException("Apenas o criador pode reordenar as aulas deste produto.");

        var module = course.Modules.FirstOrDefault(m => m.Id == request.ModuleId)
            ?? throw new NotFoundException("Módulo", request.ModuleId);

        var lessonsById = module.Lessons.ToDictionary(l => l.Id);

        // Mesma exigência de lista completa usada em ReorderModulesCommandHandler — ver comentário lá.
        if (request.OrderedLessonIds.Count != lessonsById.Count
            || request.OrderedLessonIds.Any(id => !lessonsById.ContainsKey(id)))
        {
            throw new BusinessException("A lista precisa conter exatamente as aulas existentes do módulo, sem repetição.");
        }

        for (var i = 0; i < request.OrderedLessonIds.Count; i++)
            lessonsById[request.OrderedLessonIds[i]].Reorder(i + 1);

        await uow.SaveChangesAsync(ct);
    }
}
