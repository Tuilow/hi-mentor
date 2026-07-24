using Tuilow.IdentidadeAcesso.Application.Commands.BecomeCreator;
using Tuilow.IdentidadeAcesso.Application.Commands.ConfirmEmail;
using Tuilow.IdentidadeAcesso.Application.Commands.ConsumeMagicLink;
using Tuilow.IdentidadeAcesso.Application.Commands.ForgotPassword;
using Tuilow.IdentidadeAcesso.Application.Commands.GoogleLogin;
using Tuilow.IdentidadeAcesso.Application.Commands.LoginUser;
using Tuilow.IdentidadeAcesso.Application.Commands.RefreshToken;
using Tuilow.IdentidadeAcesso.Application.Commands.RegisterUser;
using Tuilow.IdentidadeAcesso.Application.Commands.ResetPassword;
using Tuilow.IdentidadeAcesso.Application.Commands.UpdateProfile;
using Tuilow.IdentidadeAcesso.Application.Queries.GetUserProfile;
using Tuilow.SharedKernel.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace Tuilow.IdentidadeAcesso.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
public sealed class AuthController(ISender sender, ICurrentUserService currentUser) : ControllerBase
{
    /// <summary>
    /// Registra novo usuário com e-mail e senha. Não faz login automático (Sprint Item 4) — a
    /// conta nasce pendente de confirmação; um código de 6 dígitos é enviado por e-mail e deve
    /// ser confirmado em /auth/confirm-email antes que /auth/login funcione para esta conta.
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterUserCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        return Ok(result);
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

    /// <summary>
    /// Troca um Magic Link (recebido por e-mail/WhatsApp após pagamento confirmado) por um
    /// login completo — sem senha. Anônimo por natureza: quem tem o token é quem entra.
    /// </summary>
    [HttpPost("magic-link/consume")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ConsumeMagicLink([FromBody] ConsumeMagicLinkRequest request, CancellationToken ct)
    {
        var tokens = await sender.Send(
            new ConsumeMagicLinkCommand(request.Token, HttpContext.Connection.RemoteIpAddress?.ToString()), ct);
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

    /// <summary>Confirma e-mail do usuário com o código de 6 dígitos enviado no cadastro.</summary>
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

    /// <summary>Redefine a senha usando o token recebido por e-mail (ver /auth/forgot-password).</summary>
    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command, CancellationToken ct)
    {
        await sender.Send(command, ct);
        return Ok(new { message = "Senha redefinida com sucesso." });
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

    /// <summary>
    /// Edita nome/telefone/bio/avatar do usuário autenticado. Alimenta, entre outras telas, o
    /// editor do Canal do Criador (bio/avatar exibidos lá são deste perfil, não duplicados).
    /// </summary>
    [HttpPut("me")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken ct)
    {
        await sender.Send(new UpdateProfileCommand(
            currentUser.UserId!.Value, request.FirstName, request.LastName,
            request.Phone, request.BirthDate, request.Bio, request.AvatarUrl), ct);
        return Ok(new { message = "Perfil atualizado com sucesso." });
    }

    /// <summary>
    /// Auto-promoção: o próprio usuário autenticado se torna um Creator, sem depender de
    /// aprovação de um Admin — plataforma aberta, qualquer pessoa pode publicar cursos.
    /// Não remove o role Student existente (multi-role). Retorna AuthTokens já com o claim de
    /// role "Creator" no access token — o front não precisa (nem deve) chamar
    /// /auth/refresh-token depois disso.
    /// </summary>
    [HttpPost("become-creator")]
    [Authorize]
    public async Task<IActionResult> BecomeCreator(CancellationToken ct)
    {
        var tokens = await sender.Send(new BecomeCreatorCommand(currentUser.UserId!.Value), ct);
        return Ok(tokens);
    }
}

public sealed record UpdateProfileRequest(
    string FirstName, string LastName, string? Phone, DateOnly? BirthDate, string? Bio, string? AvatarUrl);

public sealed record ConsumeMagicLinkRequest(string Token);
