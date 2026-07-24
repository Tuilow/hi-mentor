using Tuilow.SharedKernel.Application.Exceptions;
using Tuilow.Catalog.Domain.Interfaces;
using Tuilow.CreatorStudio.Application.Interfaces;
using Tuilow.Sales.Domain.Interfaces;
using MediatR;

namespace Tuilow.CreatorStudio.Application.Commands.GenerateSalesPageCopy;

public sealed class GenerateSalesPageCopyCommandHandler(
    ICourseRepository courseRepository,
    ISubscriptionRepository subscriptionRepository,
    IAiContentGenerator aiContentGenerator
) : IRequestHandler<GenerateSalesPageCopyCommand, SalesPageSuggestion>
{
    public async Task<SalesPageSuggestion> Handle(GenerateSalesPageCopyCommand request, CancellationToken ct)
    {
        var course = await courseRepository.GetByIdAsync(request.CourseId, ct)
            ?? throw new NotFoundException("Curso", request.CourseId);

        if (course.InstructorId != request.InstructorId)
            throw new ForbiddenException("Apenas o criador pode gerar a página de vendas deste produto.");

        // Curso vendido por assinatura grava Course.Price = 0 por design (o preço real está no
        // Plan) — mandar 0 pra IA fazia ela gerar copy de "curso grátis" (CTA "é grátis", FAQ
        // "este curso é gratuito") para um curso que na verdade cobra por mês. Mesma causa raiz
        // do bug "Grátis" corrigido no restante da sprint — ver CourseCommercializationResolver.
        var activePlan = (await subscriptionRepository.GetPlansByCourseAsync(course.Id, ct))
            .FirstOrDefault(p => p.IsActive);
        var effectivePrice = activePlan?.Price.Amount ?? course.Price.Amount;

        return await aiContentGenerator.GenerateSalesPageAsync(
            course.Title, course.Category, course.ShortDescription, effectivePrice, ct);
    }
}
