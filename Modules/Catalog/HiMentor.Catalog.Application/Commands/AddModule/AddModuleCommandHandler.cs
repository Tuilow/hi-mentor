using HiMentor.SharedKernel.Application.Exceptions;
using HiMentor.SharedKernel.Application.Interfaces;
using HiMentor.Catalog.Domain.Interfaces;
using MediatR;

namespace HiMentor.Catalog.Application.Commands.AddModule;

public sealed class AddModuleCommandHandler(
    ICourseRepository courseRepository, IUnitOfWork uow
) : IRequestHandler<AddModuleCommand, Guid>
{
    public async Task<Guid> Handle(AddModuleCommand request, CancellationToken ct)
    {
        var course = await courseRepository.GetByIdAsync(request.CourseId, ct)
            ?? throw new NotFoundException("Curso", request.CourseId);

        if (course.InstructorId != request.InstructorId)
            throw new ForbiddenException("Apenas o criador pode adicionar módulos a este produto.");

        var module = course.AddModule(request.Title, request.Description);

        // Registra explicitamente como Added — evita DbUpdateConcurrencyException
        // (DetectChanges marcaria como Modified por causa do Guid.NewGuid() no Id)
        await courseRepository.AddModuleAsync(module, ct);

        await uow.SaveChangesAsync(ct);
        return module.Id;
    }
}
