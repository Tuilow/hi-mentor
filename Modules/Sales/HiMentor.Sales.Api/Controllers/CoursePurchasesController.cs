using HiMentor.Sales.Application.Commands.PurchaseCourse;
using HiMentor.Sales.Application.Commands.SimulateCoursePurchasePayment;
using HiMentor.Sales.Application.Queries.GetMyCoursePurchases;
using HiMentor.SharedKernel.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

namespace HiMentor.Sales.Api.Controllers;

/// <summary>
/// Compra avulsa de cursos — modelo principal de monetização do HiMentor. O aluno paga apenas
/// pelo curso que deseja acessar (sem assinatura da plataforma); a comissão da HiMentor é
/// calculada e retida automaticamente pelo módulo Finance quando o pagamento é confirmado.
/// </summary>
[ApiController]
[Route("api/v1/course-purchases")]
[Produces("application/json")]
public sealed class CoursePurchasesController(
    ISender sender, ICurrentUserService currentUser, IHostEnvironment env) : ControllerBase
{
    /// <summary>
    /// Inicia a compra de um curso — gera o link de pagamento (PIX/cartão/boleto). Não exige
    /// login: o visitante compra direto da Landing Page com nome/e-mail, e a conta é localizada
    /// ou criada automaticamente (checkout anônimo — ver PurchaseCourseCommandHandler). Se já
    /// estiver logado, a compra é vinculada à conta atual.
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Purchase([FromBody] PurchaseCourseRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new PurchaseCourseCommand(
            currentUser.UserId, request.CourseId,
            request.CustomerName, request.CustomerEmail,
            request.CpfCnpj, request.Phone), ct);
        return Ok(result);
    }

    /// <summary>Lista as compras de curso do aluno autenticado.</summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMyPurchases(CancellationToken ct)
    {
        var result = await sender.Send(new GetMyCoursePurchasesQuery(currentUser.UserId!.Value), ct);
        return Ok(result);
    }

    /// <summary>
    /// SANDBOX/DEV apenas: simula a confirmação do pagamento no lugar do webhook do Asaas, que
    /// não alcança localhost. Indisponível fora de Development (404) — nunca existe em produção.
    /// Não exige login: com o checkout anônimo, a conta do comprador pode ter sido criada
    /// automaticamente (sem senha), então não há como "logar como ele" para simular o pagamento.
    /// </summary>
    [HttpPost("{id:guid}/simulate-payment")]
    [AllowAnonymous]
    public async Task<IActionResult> SimulatePayment(Guid id, CancellationToken ct)
    {
        if (!env.IsDevelopment()) return NotFound();

        await sender.Send(new SimulateCoursePurchasePaymentCommand(currentUser.UserId, id), ct);
        return Ok();
    }
}

public sealed record PurchaseCourseRequest(Guid CourseId, string CustomerName, string CustomerEmail,
    string? CpfCnpj = null, string? Phone = null);
