using DogMaster.Application.Common.Exceptions;
using DogMaster.Domain.Common.Interfaces;
using DogMaster.Domain.Contexts.Catalog.Interfaces;
using MediatR;

namespace DogMaster.Application.Contexts.Catalog.Commands.AddModule;

public sealed class AddModuleCommandHandler(
    ICourseRepository courseRepository, IUnitOfWork uow
) : IRequestHandler<AddModuleCommand, Guid>
{
    public async Task<Guid> Handle(AddModuleCommand request, CancellationToken ct)
    {
        var course = await courseRepository.GetByIdAsync(request.CourseId, ct)
            ?? throw new NotFoundException("Curso", request.CourseId);

        var module = course.AddModule(request.Title, request.Description);
        courseRepository.Update(course);
        await uow.SaveChangesAsync(ct);
        return module.Id;
    }
}
