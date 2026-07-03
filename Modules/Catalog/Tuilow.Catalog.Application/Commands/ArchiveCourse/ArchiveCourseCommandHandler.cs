using Tuilow.SharedKernel.Application.Exceptions;
using Tuilow.SharedKernel.Application.Interfaces;
using Tuilow.Catalog.Domain.Interfaces;
using MediatR;

namespace Tuilow.Catalog.Application.Commands.ArchiveCourse;

public sealed class ArchiveCourseCommandHandler(
    ICourseRepository courseRepository, IUnitOfWork uow
) : IRequestHandler<ArchiveCourseCommand>
{
    public async Task Handle(ArchiveCourseCommand request, CancellationToken ct)
    {
        var course = await courseRepository.GetByIdAsync(request.CourseId, ct)
            ?? throw new NotFoundException("Curso", request.CourseId);

        if (course.InstructorId != request.InstructorId)
            throw new ForbiddenException("Apenas o criador pode arquivar este produto.");

        course.Archive();
        courseRepository.Update(course);
        await uow.SaveChangesAsync(ct);
    }
}
