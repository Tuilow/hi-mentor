using HiMentor.SharedKernel.Application.Interfaces;
using HiMentor.Catalog.Domain.Entities;
using HiMentor.Catalog.Domain.Interfaces;
using MediatR;

namespace HiMentor.Catalog.Application.Commands.CreateCourse;

public sealed class CreateCourseCommandHandler(
    ICourseRepository courseRepository, IUnitOfWork uow
) : IRequestHandler<CreateCourseCommand, Guid>
{
    public async Task<Guid> Handle(CreateCourseCommand request, CancellationToken ct)
    {
        var course = Course.Create(
            request.InstructorId, request.Title,
            request.Description, request.Level, request.Price, request.ProductType);

        await courseRepository.AddAsync(course, ct);
        await uow.SaveChangesAsync(ct);
        return course.Id;
    }
}
