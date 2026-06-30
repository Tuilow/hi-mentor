using DogMaster.Application.Contexts.Identity.Commands.ConfirmEmail;
using DogMaster.Application.Contexts.Identity.Commands.ForgotPassword;
using DogMaster.Application.Contexts.Identity.Commands.GoogleLogin;
using DogMaster.Application.Contexts.Identity.Commands.LoginUser;
using DogMaster.Application.Contexts.Identity.Commands.RefreshToken;
using DogMaster.Application.Contexts.Identity.Commands.RegisterUser;
using DogMaster.Application.Contexts.Identity.Queries.GetUserProfile;
using DogMaster.Application.Common.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DogMaster.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
public sealed class AuthController(ISender sender, ICurrentUserService currentUser) : ControllerBase
{
    /// <summary>Registra novo usuário com e-mail e senha.</summary>
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterUserCommand command, CancellationToken ct)
    {
        var tokens = await sender.Send(command, ct);
        return Ok(tokens);
    }

    /// <summary>Autentica usuário com e-mail e senha.</summary>
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginUserCommand command, CancellationToken ct)
    {
        var tokens = await sender.Send(
            command with { IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() }, ct);
        return Ok(tokens);
    }

    /// <summary>Renova o access token usando o refresh token.</summary>
    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenCommand command, CancellationToken ct)
    {
        var tokens = await sender.Send(
            command with { IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() }, ct);
        return Ok(tokens);
    }

    /// <summary>Login com Google OAuth.</summary>
    [HttpPost("google")]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginCommand command, CancellationToken ct)
    {
        var tokens = await sender.Send(command, ct);
        return Ok(tokens);
    }

    /// <summary>Confirma e-mail do usuário.</summary>
    [HttpPost("confirm-email")]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailCommand command, CancellationToken ct)
    {
        await sender.Send(command, ct);
        return Ok(new { message = "E-mail confirmado com sucesso." });
    }

    /// <summary>Solicita redefinição de senha.</summary>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordCommand command, CancellationToken ct)
    {
        await sender.Send(command, ct);
        return Ok(new { message = "Se este e-mail existe, você receberá as instruções em breve." });
    }

    /// <summary>Retorna perfil do usuário autenticado.</summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
    {
        var userId = currentUser.UserId!.Value;
        var profile = await sender.Send(new GetUserProfileQuery(userId), ct);
        return Ok(profile);
    }
}
