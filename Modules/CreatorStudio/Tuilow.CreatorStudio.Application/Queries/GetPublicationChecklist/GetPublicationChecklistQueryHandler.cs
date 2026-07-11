using Tuilow.SharedKernel.Application.Exceptions;
using Tuilow.Catalog.Domain.Interfaces;
using Tuilow.CreatorStudio.Application.Common;
using MediatR;

namespace Tuilow.CreatorStudio.Application.Queries.GetPublicationChecklist;

public sealed class GetPublicationChecklistQueryHandler(
    ICourseRepository courseRepository
) : IRequestHandler<GetPublicationChecklistQuery, PublicationChecklistResult>
{
    public async Task<PublicationChecklistResult> Handle(GetPublicationChecklistQuery request, CancellationToken ct)
    {
        var course = await courseRepository.GetByIdAsync(request.CourseId, ct)
            ?? throw new NotFoundException("Curso", request.CourseId);

        if (course.InstructorId != request.InstructorId)
            throw new ForbiddenException("Apenas o criador pode ver o checklist deste produto.");

        return PublicationChecklist.Evaluate(course);
    }
}
