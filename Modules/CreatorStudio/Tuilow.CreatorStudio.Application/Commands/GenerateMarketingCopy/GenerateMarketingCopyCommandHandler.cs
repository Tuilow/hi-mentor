using Tuilow.SharedKernel.Application.Exceptions;
using Tuilow.Catalog.Domain.Interfaces;
using Tuilow.CreatorStudio.Application.Interfaces;
using MediatR;

namespace Tuilow.CreatorStudio.Application.Commands.GenerateMarketingCopy;

/// <summary>Mesmo padrão de GenerateSalesPageCopyCommandHandler — só compõe dados já existentes do Course.</summary>
public sealed class GenerateMarketingCopyCommandHandler(
    ICourseRepository courseRepository,
    IAiContentGenerator aiContentGenerator
) : IRequestHandler<GenerateMarketingCopyCommand, MarketingCopySuggestion>
{
    public async Task<MarketingCopySuggestion> Handle(GenerateMarketingCopyCommand request, CancellationToken ct)
    {
        var course = await courseRepository.GetByIdAsync(request.CourseId, ct)
            ?? throw new NotFoundException("Curso", request.CourseId);

        if (course.InstructorId != request.InstructorId)
            throw new ForbiddenException("Apenas o criador pode gerar textos de divulgação deste produto.");

        return await aiContentGenerator.GenerateMarketingCopyAsync(
            course.Title, request.Channel, course.Category, course.ShortDescription,
            course.SalesPageBenefits.ToList(), course.Price.Amount, ct);
    }
}
