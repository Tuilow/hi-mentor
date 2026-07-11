using Tuilow.SharedKernel.Application.Exceptions;
using Tuilow.Catalog.Domain.Interfaces;
using Tuilow.CreatorStudio.Application.Interfaces;
using MediatR;

namespace Tuilow.CreatorStudio.Application.Commands.GenerateSalesPageCopy;

public sealed class GenerateSalesPageCopyCommandHandler(
    ICourseRepository courseRepository,
    IAiContentGenerator aiContentGenerator
) : IRequestHandler<GenerateSalesPageCopyCommand, SalesPageSuggestion>
{
    public async Task<SalesPageSuggestion> Handle(GenerateSalesPageCopyCommand request, CancellationToken ct)
    {
        var course = await courseRepository.GetByIdAsync(request.CourseId, ct)
            ?? throw new NotFoundException("Curso", request.CourseId);

        if (course.InstructorId != request.InstructorId)
            throw new ForbiddenException("Apenas o criador pode gerar a página de vendas deste produto.");

        return await aiContentGenerator.GenerateSalesPageAsync(
            course.Title, course.Category, course.ShortDescription, course.Price.Amount, ct);
    }
}
