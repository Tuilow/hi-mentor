using HiMentor.SharedKernel.Application.Exceptions;
using HiMentor.Sales.Domain.Enums;
using HiMentor.Sales.Domain.Events;
using HiMentor.Sales.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HiMentor.Sales.Application.Commands.ReprocessCoursePurchase;

public sealed class ReprocessCoursePurchaseCommandHandler(
    ICoursePurchaseRepository coursePurchaseRepository,
    IEnumerable<INotificationHandler<CoursePurchaseConfirmedDomainEvent>> handlers,
    ILogger<ReprocessCoursePurchaseCommandHandler> logger
) : IRequestHandler<ReprocessCoursePurchaseCommand, ReprocessResult>
{
    public async Task<ReprocessResult> Handle(ReprocessCoursePurchaseCommand request, CancellationToken ct)
    {
        var purchase = await coursePurchaseRepository.GetByIdAsync(request.CoursePurchaseId, ct)
            ?? throw new NotFoundException("Compra", request.CoursePurchaseId);

        if (purchase.Status != CoursePurchaseStatus.Confirmed)
            return new ReprocessResult(false, $"Compra esta com status {purchase.Status} (nao Confirmed) - nada a reprocessar.");

        var domainEvent = new CoursePurchaseConfirmedDomainEvent(
            purchase.Id, purchase.StudentId, purchase.CourseId, purchase.CreatorId,
            purchase.Amount.Amount, purchase.AsaasPaymentId);

        var failures = new List<string>();
        var handlerCount = 0;

        foreach (var handler in handlers)
        {
            handlerCount++;
            try
            {
                await handler.Handle(domainEvent, ct);
            }
            catch (Exception ex)
            {
                failures.Add($"{handler.GetType().Name}: {ex.Message}");
                logger.LogError(ex,
                    "Falha no handler {HandlerType} ao reprocessar manualmente a compra {PurchaseId}.",
                    handler.GetType().Name, purchase.Id);
            }
        }

        if (failures.Count > 0)
        {
            return new ReprocessResult(
                false,
                $"Reprocessamento parcial: {handlerCount - failures.Count} handler(s) concluido(s), {failures.Count} falharam. {string.Join(" | ", failures)}");
        }

        logger.LogInformation("Reprocessamento manual da compra {PurchaseId} concluido.", purchase.Id);
        return new ReprocessResult(true, "Reprocessado com sucesso - matricula/e-mail/comissao reexecutados (idempotente, seguro repetir).");
    }
}
