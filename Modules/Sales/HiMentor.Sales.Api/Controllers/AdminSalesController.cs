using HiMentor.Sales.Application.Commands.ReprocessCoursePurchase;
using HiMentor.Sales.Application.Commands.ReprocessSubscriptionPayment;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HiMentor.Sales.Api.Controllers;

/// <summary>
/// Área administrativa do módulo Sales — hoje só o reprocessamento manual do achado C2 da
/// auditoria: os domain events de confirmação de pagamento são publicados de forma síncrona logo
/// após o commit, sem Outbox nem fila. Se o handler de Learning (matrícula/e-mail) ou Finance
/// (comissão) falhar depois do commit, o aluno já pagou mas nunca recebe o efeito colateral, e
/// reenviar o webhook da Asaas não ajuda (a confirmação já é idempotente). Suporte usa estes
/// endpoints depois de identificar o problema (log crítico do AppDbContext, ou reclamação do
/// aluno) para forçar o reprocessamento sem depender da Asaas.
/// </summary>
[ApiController]
[Route("api/v1/admin/sales")]
[Produces("application/json")]
[Authorize(Roles = "Admin")]
public sealed class AdminSalesController(ISender sender) : ControllerBase
{
    /// <summary>Reprocessa a confirmação de uma compra avulsa já paga (idempotente — seguro chamar mais de uma vez).</summary>
    [HttpPost("course-purchases/{id:guid}/reprocess")]
    public async Task<IActionResult> ReprocessCoursePurchase(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new ReprocessCoursePurchaseCommand(id), ct);
        return result.Success ? Ok(result) : UnprocessableEntity(result);
    }

    /// <summary>Reprocessa a confirmação de um pagamento de assinatura já confirmado (idempotente).</summary>
    [HttpPost("subscriptions/{id:guid}/reprocess-payment")]
    public async Task<IActionResult> ReprocessSubscriptionPayment(Guid id, [FromQuery] string asaasPaymentId, CancellationToken ct)
    {
        var result = await sender.Send(new ReprocessSubscriptionPaymentCommand(id, asaasPaymentId), ct);
        return result.Success ? Ok(result) : UnprocessableEntity(result);
    }
}
