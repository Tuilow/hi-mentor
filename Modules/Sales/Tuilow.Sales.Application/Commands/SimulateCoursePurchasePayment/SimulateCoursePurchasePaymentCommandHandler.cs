using Tuilow.SharedKernel.Application.Exceptions;
using Tuilow.SharedKernel.Application.Interfaces;
using Tuilow.Sales.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Tuilow.Sales.Application.Commands.SimulateCoursePurchasePayment;

/// <summary>
/// Confirma manualmente uma compra avulsa que está aguardando o webhook do Asaas. Necessário
/// porque, em ambiente local, o Asaas não consegue entregar o webhook em localhost (exige URL
/// pública) — sem isso, uma compra criada em sandbox.asaas.com fica presa em "Pending" para
/// sempre e o aluno nunca ganha acesso, mesmo pagando com os dados de teste do Asaas.
///
/// Reaproveita exatamente o mesmo método de domínio usado pelo webhook real
/// (<see cref="Domain.Entities.CoursePurchase.ConfirmPayment"/>), disparando o mesmo
/// CoursePurchaseConfirmedDomainEvent (matrícula automática em Learning + crédito ao criador em
/// Finance) — nenhuma regra de negócio nova, nenhum caminho de autorização paralelo. O
/// controller garante que este comando só é alcançável fora de Production.
/// </summary>
public sealed class SimulateCoursePurchasePaymentCommandHandler(
    ICoursePurchaseRepository coursePurchaseRepository,
    IUnitOfWork uow,
    ILogger<SimulateCoursePurchasePaymentCommandHandler> logger
) : IRequestHandler<SimulateCoursePurchasePaymentCommand>
{
    public async Task Handle(SimulateCoursePurchasePaymentCommand request, CancellationToken ct)
    {
        var purchase = await coursePurchaseRepository.GetByIdAsync(request.CoursePurchaseId, ct)
            ?? throw new NotFoundException("Compra", request.CoursePurchaseId);

        if (request.StudentId.HasValue && purchase.StudentId != request.StudentId.Value)
            throw new BusinessException("Esta compra não pertence a você.");

        purchase.ConfirmPayment(); // idempotente — mesma regra do webhook real
        coursePurchaseRepository.Update(purchase);
        await uow.SaveChangesAsync(ct);

        logger.LogInformation(
            "[SANDBOX] Pagamento simulado manualmente para a compra {PurchaseId} (aluno {StudentId}).",
            purchase.Id, purchase.StudentId);
    }
}
