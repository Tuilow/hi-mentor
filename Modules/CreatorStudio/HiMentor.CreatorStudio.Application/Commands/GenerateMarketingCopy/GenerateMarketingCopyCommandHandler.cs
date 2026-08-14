using HiMentor.SharedKernel.Application.Exceptions;
using HiMentor.Catalog.Domain.Interfaces;
using HiMentor.CreatorStudio.Application.Interfaces;
using HiMentor.Sales.Domain.Interfaces;
using MediatR;

namespace HiMentor.CreatorStudio.Application.Commands.GenerateMarketingCopy;

/// <summary>Mesmo padrão de GenerateSalesPageCopyCommandHandler — só compõe dados já existentes do Course.</summary>
public sealed class GenerateMarketingCopyCommandHandler(
    ICourseRepository courseRepository,
    ISubscriptionRepository subscriptionRepository,
    IAiContentGenerator aiContentGenerator
) : IRequestHandler<GenerateMarketingCopyCommand, MarketingCopySuggestion>
{
    public async Task<MarketingCopySuggestion> Handle(GenerateMarketingCopyCommand request, CancellationToken ct)
    {
        var course = await courseRepository.GetByIdAsync(request.CourseId, ct)
            ?? throw new NotFoundException("Curso", request.CourseId);

        if (course.InstructorId != request.InstructorId)
            throw new ForbiddenException("Apenas o criador pode gerar textos de divulgação deste produto.");

        // Mesmo motivo do GenerateSalesPageCopyCommandHandler: sem isso, um curso de assinatura
        // (Price=0 por design) virava "de graça"/"gratuito" nos textos de divulgação gerados.
        var activePlan = (await subscriptionRepository.GetPlansByCourseAsync(course.Id, ct))
            .FirstOrDefault(p => p.IsActive);
        var effectivePrice = activePlan?.Price.Amount ?? course.Price.Amount;

        return await aiContentGenerator.GenerateMarketingCopyAsync(
            course.Title, request.Channel, course.Category, course.ShortDescription,
            course.SalesPageBenefits.ToList(), effectivePrice, ct);
    }
}
