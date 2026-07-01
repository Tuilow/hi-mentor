using Tuilow.Application.Common.Exceptions;
using Tuilow.Domain.Common.Interfaces;
using Tuilow.Domain.Contexts.Catalog.Interfaces;
using MediatR;

namespace Tuilow.Application.Contexts.Catalog.Commands.AddModule;

public sealed class AddModuleCommandHandler(
    ICourseRepository courseRepository, IUnitOfWork uow
) : IRequestHandler<AddModuleCommand, Guid>
{
    public async Task<Guid> Handle(AddModuleCommand request, CancellationToken ct)
    {
        var course = await courseRepository.GetByIdAsync(request.CourseId, ct)
            ?? throw new NotFoundException("Curso", request.CourseId);

        var module = course.AddModule(request.Title, request.Description);

        // Registra explicitamente como Added — evita DbUpdateConcurrencyException
        // (DetectChanges marcaria como Modified por causa do Guid.NewGuid() no Id)
        await courseRepository.AddModuleAsync(module, ct);

        await uow.SaveChangesAsync(ct);
        return module.Id;
    }
}
