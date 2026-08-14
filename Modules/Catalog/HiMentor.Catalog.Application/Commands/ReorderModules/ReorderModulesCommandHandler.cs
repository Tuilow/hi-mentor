using HiMentor.SharedKernel.Application.Exceptions;
using HiMentor.SharedKernel.Application.Interfaces;
using HiMentor.Catalog.Domain.Interfaces;
using MediatR;

namespace HiMentor.Catalog.Application.Commands.ReorderModules;

public sealed class ReorderModulesCommandHandler(
    ICourseRepository courseRepository, IUnitOfWork uow
) : IRequestHandler<ReorderModulesCommand>
{
    public async Task Handle(ReorderModulesCommand request, CancellationToken ct)
    {
        var course = await courseRepository.GetByIdAsync(request.CourseId, ct)
            ?? throw new NotFoundException("Curso", request.CourseId);

        if (course.InstructorId != request.InstructorId)
            throw new ForbiddenException("Apenas o criador pode reordenar os módulos deste produto.");

        var modulesById = course.Modules.ToDictionary(m => m.Id);

        // Exige a lista completa (todos os módulos existentes, sem repetição) — evita deixar
        // um módulo com Order duplicado ou "órfão" por causa de uma lista parcial vinda do front.
        if (request.OrderedModuleIds.Count != modulesById.Count
            || request.OrderedModuleIds.Any(id => !modulesById.ContainsKey(id)))
        {
            throw new BusinessException("A lista precisa conter exatamente os módulos existentes do curso, sem repetição.");
        }

        for (var i = 0; i < request.OrderedModuleIds.Count; i++)
            modulesById[request.OrderedModuleIds[i]].Reorder(i + 1);

        await uow.SaveChangesAsync(ct);
    }
}
