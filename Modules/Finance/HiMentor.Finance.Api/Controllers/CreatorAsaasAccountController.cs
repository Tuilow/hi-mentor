using HiMentor.Finance.Application.Commands.ConnectCreatorAsaasAccount;
using HiMentor.Finance.Application.Queries.GetMyAsaasAccountStatus;
using HiMentor.SharedKernel.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HiMentor.Finance.Api.Controllers;

/// <summary>
/// Painel do creator: conectar sua própria conta Asaas ao marketplace de split (Financeiro ->
/// Configurar recebimentos) e consultar o status da conexão. Autorização por role (Creator ou
/// Admin) -- nenhuma checagem de e-mail hardcoded, mesmo padrão do resto do painel.
/// </summary>
[ApiController]
[Route("api/v1/finance/asaas-account")]
[Produces("application/json")]
[Authorize(Roles = "Creator,Admin")]
public sealed class CreatorAsaasAccountController(ISender sender, ICurrentUserService currentUser) : ControllerBase
{
    /// <summary>Status atual da conexão do creator autenticado com o marketplace de split.</summary>
    [HttpGet]
    public async Task<IActionResult> GetStatus(CancellationToken ct)
    {
        var result = await sender.Send(new GetMyAsaasAccountStatusQuery(currentUser.UserId!.Value), ct);
        return Ok(result);
    }

    /// <summary>
    /// Conecta (ou reconecta) a conta Asaas própria do creator autenticado. A API Key é validada
    /// contra a Asaas e nunca é devolvida nem logada -- só o resultado (sucesso/erro) é retornado.
    /// </summary>
    [HttpPost("connect")]
    public async Task<IActionResult> Connect([FromBody] ConnectAsaasAccountRequest request, CancellationToken ct)
    {
        var result = await sender.Send(
            new ConnectCreatorAsaasAccountCommand(currentUser.UserId!.Value, request.ApiKey, request.CpfCnpj, request.LegalName), ct);

        return result.Success ? Ok(result) : UnprocessableEntity(result);
    }
}

public sealed record ConnectAsaasAccountRequest(string ApiKey, string? CpfCnpj, string? LegalName);
