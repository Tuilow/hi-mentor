using Tuilow.Application.Common.Exceptions;
using Tuilow.Domain.Common.Interfaces;
using Tuilow.Domain.Contexts.Catalog.Interfaces;
using MediatR;

namespace Tuilow.Application.Contexts.Catalog.Commands.PublishCourse;

public sealed class PublishCourseCommandHandler(
    ICourseRepository courseRepository, IUnitOfWork uow
) : IRequestHandler<PublishCourseCommand>
{
    public async Task Handle(PublishCourseCommand request, CancellationToken ct)
    {
        var course = await courseRepository.GetByIdAsync(request.CourseId, ct)
            ?? throw new NotFoundException("Curso", request.CourseId);

        if (course.InstructorId != request.InstructorId)
            throw new ForbiddenException("Apenas o instrutor pode publicar o curso.");

        course.Publish();
        courseRepository.Update(course);
        await uow.SaveChangesAsync(ct);
    }
}
