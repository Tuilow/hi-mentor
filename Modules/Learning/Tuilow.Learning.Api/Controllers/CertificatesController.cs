using Tuilow.Learning.Application.Queries.VerifyCertificate;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Tuilow.Learning.Api.Controllers;

/// <summary>
/// Achado A4 da avaliação: verificação pública de autenticidade de certificado — sem isto, um
/// certificado emitido não tinha como ser conferido por terceiros (ex.: um recrutador).
/// </summary>
[ApiController]
[Route("api/v1/certificates")]
[Produces("application/json")]
public sealed class CertificatesController(ISender sender) : ControllerBase
{
    /// <summary>Confirma se um código de certificado é autêntico. Sempre acessível — sem login.</summary>
    [HttpGet("verify/{code}")]
    [AllowAnonymous]
    public async Task<IActionResult> Verify(string code, CancellationToken ct)
    {
        var result = await sender.Send(new VerifyCertificateQuery(code), ct);
        return result is null ? NotFound() : Ok(result);
    }
}
