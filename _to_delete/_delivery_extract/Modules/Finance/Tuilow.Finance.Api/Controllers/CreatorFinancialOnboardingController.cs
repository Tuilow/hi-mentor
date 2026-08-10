using Tuilow.Finance.Application.Commands.StartCreatorFinancialOnboarding;
using Tuilow.Finance.Application.Commands.SyncCreatorOnboardingDocuments;
using Tuilow.Finance.Application.Commands.UploadCreatorOnboardingDocument;
using Tuilow.Finance.Application.Queries.GetMyFinancialOnboardingStatus;
using Tuilow.SharedKernel.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Tuilow.Finance.Api.Controllers;

/// <summary>
/// "Financeiro -&gt; Configurar recebimentos" do criador — onboarding financeiro via subconta
/// Asaas (BaaS) criada pela própria Tuilow. Substitui, para criadores novos, o fluxo de "cole sua
/// API Key" (ver <see cref="CreatorAsaasAccountController"/>, mantido só por compatibilidade com
/// conexões antigas). Nenhum endpoint aqui recebe ou devolve API Key/Wallet ID/"subconta" — só
/// dados pessoais e status amigável.
/// </summary>
[ApiController]
[Route("api/v1/finance/onboarding")]
[Produces("application/json")]
[Authorize(Roles = "Creator,Admin")]
public sealed class CreatorFinancialOnboardingController(ISender sender, ICurrentUserService currentUser) : ControllerBase
{
    /// <summary>Status atual da jornada de onboarding financeiro do criador autenticado.</summary>
    [HttpGet]
    public async Task<IActionResult> GetStatus(CancellationToken ct)
    {
        var result = await sender.Send(new GetMyFinancialOnboardingStatusQuery(currentUser.UserId!.Value), ct);
        return Ok(result);
    }

    /// <summary>Passo 1 ("Seus dados") — envia os dados pessoais/empresariais e dispara a criação da subconta (idempotente).</summary>
    [HttpPost("start")]
    public async Task<IActionResult> Start([FromBody] StartFinancialOnboardingRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new StartCreatorFinancialOnboardingCommand(
            currentUser.UserId!.Value, request.LegalName, request.CpfCnpj, request.BirthDate, request.CompanyType,
            request.Email, request.MobilePhone, request.Phone, request.IncomeValue,
            request.Address, request.AddressNumber, request.AddressComplement, request.Province, request.PostalCode), ct);

        return result.Success ? Ok(result) : UnprocessableEntity(result);
    }

    /// <summary>Passo 2 ("Documentação") — busca/atualiza a lista de documentos pendentes direto na Asaas.</summary>
    [HttpGet("documents")]
    public async Task<IActionResult> GetDocuments(CancellationToken ct)
    {
        var syncResult = await sender.Send(new SyncCreatorOnboardingDocumentsCommand(currentUser.UserId!.Value), ct);
        if (!syncResult.Success)
            return UnprocessableEntity(syncResult);

        var status = await sender.Send(new GetMyFinancialOnboardingStatusQuery(currentUser.UserId!.Value), ct);
        return Ok(status.Documents);
    }

    /// <summary>Envio de um documento pela própria Tuilow — só funciona para documentos sem link externo obrigatório (ver CreatorAsaasOnboardingDocument.OnboardingUrl).</summary>
    [HttpPost("documents/{asaasDocumentId}/upload")]
    [RequestSizeLimit(20_000_000)] // 20MB — mesma ordem de grandeza de um documento de identidade escaneado
    public async Task<IActionResult> UploadDocument(string asaasDocumentId, IFormFile file, CancellationToken ct)
    {
        if (file.Length == 0)
            return BadRequest(new { message = "Arquivo vazio." });

        await using var stream = file.OpenReadStream();
        var result = await sender.Send(new UploadCreatorOnboardingDocumentCommand(
            currentUser.UserId!.Value, asaasDocumentId, stream, file.FileName, file.ContentType), ct);

        return result.Success ? Ok(result) : UnprocessableEntity(result);
    }
}

public sealed record StartFinancialOnboardingRequest(
    string LegalName, string CpfCnpj, DateOnly? BirthDate, string? CompanyType,
    string Email, string MobilePhone, string? Phone, decimal IncomeValue,
    string Address, string AddressNumber, string? AddressComplement, string Province, string PostalCode
);
