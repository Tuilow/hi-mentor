using Tuilow.Sales.Application.Commands.PurchaseCourse;
using Tuilow.Sales.Application.Queries.GetMyCoursePurchases;
using Tuilow.SharedKernel.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Tuilow.Sales.Api.Controllers;

/// <summary>
/// Compra avulsa de cursos — modelo principal de monetização do Tuilow. O aluno paga apenas
/// pelo curso que deseja acessar (sem assinatura da plataforma); a comissão da Tuilow é
/// calculada e retida automaticamente pelo módulo Finance quando o pagamento é confirmado.
/// </summary>
[ApiController]
[Route("api/v1/course-purchases")]
[Produces("application/json")]
[Authorize]
public sealed class CoursePurchasesController(ISender sender, ICurrentUserService currentUser) : ControllerBase
{
    /// <summary>Inicia a compra de um curso — gera o link de pagamento (PIX/cartão/boleto).</summary>
    [HttpPost]
    public async Task<IActionResult> Purchase([FromBody] PurchaseCourseRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new PurchaseCourseCommand(
            currentUser.UserId!.Value, request.CourseId,
            request.CustomerName, request.CustomerEmail,
            request.CpfCnpj, request.Phone), ct);
        return Ok(result);
    }

    /// <summary>Lista as compras de curso do aluno autenticado.</summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetMyPurchases(CancellationToken ct)
    {
        var result = await sender.Send(new GetMyCoursePurchasesQuery(currentUser.UserId!.Value), ct);
        return Ok(result);
    }
}

public sealed record PurchaseCourseRequest(Guid CourseId, string CustomerName, string CustomerEmail,
    string? CpfCnpj = null, string? Phone = null);
