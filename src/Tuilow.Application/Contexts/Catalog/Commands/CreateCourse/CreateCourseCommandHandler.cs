using Tuilow.Domain.Common.Interfaces;
using Tuilow.Domain.Contexts.Catalog.Entities;
using Tuilow.Domain.Contexts.Catalog.Interfaces;
using MediatR;

namespace Tuilow.Application.Contexts.Catalog.Commands.CreateCourse;

public sealed class CreateCourseCommandHandler(
    ICourseRepository courseRepository, IUnitOfWork uow
) : IRequestHandler<CreateCourseCommand, Guid>
{
    public async Task<Guid> Handle(CreateCourseCommand request, CancellationToken ct)
    {
        var course = Course.Create(
            request.InstructorId, request.Title,
            request.Description, request.Level, request.Price);

        await courseRepository.AddAsync(course, ct);
        await uow.SaveChangesAsync(ct);
        return course.Id;
    }
}
