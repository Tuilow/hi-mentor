using HiMentor.SharedKernel.Application.Exceptions;
using HiMentor.SharedKernel.Application.Interfaces;
using HiMentor.Catalog.Domain.Interfaces;
using MediatR;

namespace HiMentor.Catalog.Application.Commands.UpdateCourseBasicInfo;

public sealed class UpdateCourseBasicInfoCommandHandler(
    ICourseRepository courseRepository, IUnitOfWork uow
) : IRequestHandler<UpdateCourseBasicInfoCommand>
{
    public async Task Handle(UpdateCourseBasicInfoCommand request, CancellationToken ct)
    {
        var course = await courseRepository.GetByIdAsync(request.CourseId, ct)
            ?? throw new NotFoundException("Curso", request.CourseId);

        if (course.InstructorId != request.InstructorId)
            throw new ForbiddenException("Apenas o criador pode editar este produto.");

        course.UpdateBasicInfo(request.Title, request.Category, request.Subcategory,
            request.ShortDescription, request.Description);

        courseRepository.Update(course);
        await uow.SaveChangesAsync(ct);
    }
}
