using Tuilow.Channel.Application.Commands.UpsertChannel;
using Tuilow.Channel.Application.Queries.GetMyChannel;
using Tuilow.Channel.Application.Queries.GetPublicChannel;
using Tuilow.SharedKernel.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Tuilow.Channel.Api.Controllers;

/// <summary>
/// Canal do Criador — vitrine pública em tuilow.com/canal/{handle} com todos os cursos
/// publicados do criador, e a tela de configuração ("Meu Canal") para o próprio criador.
/// </summary>
[ApiController]
[Route("api/v1/channel")]
[Produces("application/json")]
public sealed class ChannelController(ISender sender, ICurrentUserService currentUser) : ControllerBase
{
    /// <summary>Tela "Meu Canal" — dados do canal do criador autenticado (null se ainda não criou).</summary>
    [HttpGet("me")]
    [Authorize(Roles = "Creator,Admin")]
    public async Task<IActionResult> GetMyChannel(CancellationToken ct)
    {
        var result = await sender.Send(new GetMyChannelQuery(currentUser.UserId!.Value), ct);
        return Ok(result);
    }

    /// <summary>Cria ou atualiza o canal do criador autenticado (define @handle e redes sociais).</summary>
    [HttpPut("me")]
    [Authorize(Roles = "Creator,Admin")]
    public async Task<IActionResult> UpsertMyChannel([FromBody] UpsertChannelRequest request, CancellationToken ct)
    {
        var id = await sender.Send(
            new UpsertChannelCommand(currentUser.UserId!.Value, request.Handle, request.SocialLinks), ct);
        return Ok(new { id });
    }

    /// <summary>Perfil público do canal — vitrine com os cursos publicados do criador. Sempre acessível.</summary>
    [HttpGet("{handle}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPublicChannel(string handle, CancellationToken ct)
    {
        var result = await sender.Send(new GetPublicChannelQuery(handle, currentUser.UserId), ct);
        return result is null ? NotFound() : Ok(result);
    }
}

public sealed record UpsertChannelRequest(string Handle, IReadOnlyList<SocialLinkInput> SocialLinks);
