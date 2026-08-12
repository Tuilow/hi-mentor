using Tuilow.Learning.Application.Queries.DownloadCertificate;
using Tuilow.Learning.Application.Queries.GetCertificateForCourse;
using Tuilow.Learning.Application.Queries.GetMyCertificates;
using Tuilow.Learning.Application.Queries.VerifyCertificate;
using Tuilow.SharedKernel.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Tuilow.Learning.Api.Controllers;

/// <summary>
/// Achado A4 da avaliação: verificação pública de autenticidade de certificado — sem isto, um
/// certificado emitido não tinha como ser conferido por terceiros (ex.: um recrutador).
///
/// Feature 12/08/2026 ("Baixar certificado" + aba "Certificados"): os três endpoints novos
/// (GetMine/GetForCourse/Download) exigem login — só o próprio aluno vê/baixa os seus
/// certificados. [Authorize] na classe cobre os três; Verify continua público via
/// [AllowAnonymous] explícito (mesmo padrão de EnrollmentsController.EnrollFree).
/// </summary>
[ApiController]
[Route("api/v1/certificates")]
[Authorize]
[Produces("application/json")]
public sealed class CertificatesController(ISender sender, ICurrentUserService currentUser) : ControllerBase
{
    /// <summary>Confirma se um código de certificado é autêntico. Sempre acessível — sem login.</summary>
    [HttpGet("verify/{code}")]
    [AllowAnonymous]
    public async Task<IActionResult> Verify(string code, CancellationToken ct)
    {
        var result = await sender.Send(new VerifyCertificateQuery(code), ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Certificados do aluno autenticado — alimenta a aba "Certificados" do sidebar.</summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        var result = await sender.Send(new GetMyCertificatesQuery(currentUser.UserId!.Value), ct);
        return Ok(result);
    }

    /// <summary>
    /// Certificado do aluno autenticado para um curso específico, se já emitido — usado pela
    /// tela de jornada para decidir se mostra "Baixar certificado" no bloco "Programa concluído".
    /// 404 quando o curso ainda não foi concluído (não é erro).
    /// </summary>
    [HttpGet("course/{courseId:guid}")]
    public async Task<IActionResult> GetForCourse(Guid courseId, CancellationToken ct)
    {
        var result = await sender.Send(new GetCertificateForCourseQuery(currentUser.UserId!.Value, courseId), ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Gera e baixa o PDF do certificado (sob demanda, ver ICertificatePdfGenerator).</summary>
    [HttpGet("{certificateId:guid}/download")]
    public async Task<IActionResult> Download(Guid certificateId, CancellationToken ct)
    {
        var result = await sender.Send(new DownloadCertificateQuery(currentUser.UserId!.Value, certificateId), ct);
        if (result is null) return NotFound();
        return File(result.Bytes, "application/pdf", result.FileName);
    }
}
