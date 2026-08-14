using HiMentor.IdentidadeAcesso.Application.Commands.BecomeCreator;
using HiMentor.IdentidadeAcesso.Application.Commands.ConfirmEmail;
using HiMentor.IdentidadeAcesso.Application.Commands.ConsumeMagicLink;
using HiMentor.IdentidadeAcesso.Application.Commands.ForgotPassword;
using HiMentor.IdentidadeAcesso.Application.Commands.GoogleLogin;
using HiMentor.IdentidadeAcesso.Application.Commands.LoginUser;
using HiMentor.IdentidadeAcesso.Application.Commands.Logout;
using HiMentor.IdentidadeAcesso.Application.Commands.RefreshToken;
using HiMentor.IdentidadeAcesso.Application.Commands.RegisterUser;
using HiMentor.IdentidadeAcesso.Application.Commands.ResendAccessLink;
using HiMentor.IdentidadeAcesso.Application.Commands.ResetPassword;
using HiMentor.IdentidadeAcesso.Application.Commands.UpdateProfile;
using HiMentor.IdentidadeAcesso.Application.Common;
using HiMentor.IdentidadeAcesso.Application.Queries.GetUserProfile;
using HiMentor.SharedKernel.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
// Precisa vir explícito: IWebHostEnvironment (Microsoft.AspNetCore.Hosting, usado no tipo do
// parâmetro `environment` abaixo) implementa Microsoft.Extensions.Hosting.IHostEnvironment, e é
// nesse namespace que mora o método de extensão IsDevelopment() que faz sentido pra ele. Sem este
// using, o compilador só enxerga Microsoft.AspNetCore.Hosting.HostingEnvironmentExtensions.IsDevelopment
// — a sobrecarga OBSOLETA para a interface antiga IHostingEnvironment, incompatível com
// IWebHostEnvironment — e a build falha com CS1929.
using Microsoft.Extensions.Hosting;

namespace HiMentor.IdentidadeAcesso.Api.Controllers;

/// <summary>
/// Achado C1 do PROMPT de arquitetura www/app: o refresh token vivia em localStorage
/// (acessível a qualquer script — risco de XSS) e não havia nenhuma forma de invalidá-lo no
/// servidor no logout. Agora todo endpoint que emite AuthTokens também grava o refresh token
/// como cookie HttpOnly (ver SetRefreshTokenCookie) e a resposta JSON devolve só o access token
/// — o refresh token nunca mais chega a código JavaScript. O cookie é HttpOnly + (fora de
/// Development) Secure, com Domain configurável via "Cookies:Domain" (vazio em dev = cookie
/// preso ao host exato; em produção, ".himentor.com.br" faz o cookie valer tanto em
/// www.himentor.com.br quanto em app.himentor.com.br, viabilizando o subdomínio dedicado sem
/// deslogar o aluno ao trocar de host).
/// </summary>
[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
public sealed class AuthController(
    ISender sender, ICurrentUserService currentUser,
    IConfiguration configuration, IWebHostEnvironment environment
) : ControllerBase
{
    private const string RefreshTokenCookieName = "refresh_token";

    private void SetRefreshTokenCookie(AuthTokens tokens)
    {
        Response.Cookies.Append(RefreshTokenCookieName, tokens.RefreshToken, BuildCookieOptions(tokens.RefreshTokenExpires));
    }

    private void ClearRefreshTokenCookie()
    {
        Response.Cookies.Delete(RefreshTokenCookieName, BuildCookieOptions(DateTimeOffset.UnixEpoch));
    }

    private CookieOptions BuildCookieOptions(DateTimeOffset expires)
    {
        var domain = configuration["Cookies:Domain"];
        var isDev = environment.IsDevelopment();
        return new CookieOptions
        {
            HttpOnly = true,
            // Secure exige HTTPS — em Development (http://localhost) o cookie seria descartado
            // pelo navegador se marcado Secure.
            Secure = !isDev,
            // Achado de teste manual (deploy real): a suposição original era Lax porque a ideia
            // era front e API ficarem em subdomínios do MESMO site registrável (ex.:
            // www.himentor.com.br / app.himentor.com.br), onde Lax já basta. Mas o deploy atual usa
            // himentor.vercel.app (Vercel) e himentor-production.up.railway.app (Railway) — dois
            // domínios registráveis DE FATO diferentes (up.railway.app e vercel.app são entradas
            // próprias na Public Suffix List). Nesse caso o navegador trata toda chamada
            // fetch/XHR do front pra API como "cross-site" e um cookie Lax nunca é enviado nela
            // — por isso o login parecia funcionar ("Bem-vindo de volta") mas o cookie
            // refresh_token nunca chegava no navegador, e o middleware do front (que só checa a
            // presença desse cookie) redirecionava de volta pro login. None exige Secure=true
            // (already o caso fora de Development) e é o único valor que o navegador aceita
            // enviar em requisições cross-site. Em Development, isDev=true → Secure=false → só
            // Lax é aceitável (None sem Secure é rejeitado/descartado pelos navegadores).
            SameSite = isDev ? SameSiteMode.Lax : SameSiteMode.None,
            Domain = string.IsNullOrWhiteSpace(domain) ? null : domain,
            Expires = expires,
            Path = "/"
        };
    }

    /// <summary>
    /// A resposta ao cliente nunca mais inclui o refresh token (ele só existe no cookie
    /// HttpOnly, ver SetRefreshTokenCookie) — expor os dois ao mesmo tempo (cookie + corpo JSON)
    /// devolveria o mesmo risco de XSS que essa migração resolve.
    /// </summary>
    private static object ToClientResponse(AuthTokens tokens) => new
    {
        accessToken = tokens.AccessToken,
        accessTokenExpires = tokens.AccessTokenExpires
    };
    /// <summary>
    /// Registra novo usuário com e-mail e senha. Não faz login automático (Sprint Item 4) — a
    /// conta nasce pendente de confirmação; um código de 6 dígitos é enviado por e-mail e deve
    /// ser confirmado em /auth/confirm-email antes que /auth/login funcione para esta conta.
    /// Achado B8 da avaliação: rate limit por IP — sem isso, um script conseguia criar contas
    /// em massa (spam de e-mail de confirmação para terceiros, sobrecarga do provedor de e-mail).
    /// </summary>
    [HttpPost("register")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterUserCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        return Ok(result);
    }

    /// <summary>
    /// Autentica usuário com e-mail e senha. Achado B8 da avaliação: rate limit por IP — sem
    /// isso, nada impedia uma tentativa de força bruta de senha contra uma conta conhecida.
    /// </summary>
    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginUserCommand command, CancellationToken ct)
    {
        var tokens = await sender.Send(
            command with { IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() }, ct);
        SetRefreshTokenCookie(tokens);
        return Ok(ToClientResponse(tokens));
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
        SetRefreshTokenCookie(tokens);
        return Ok(ToClientResponse(tokens));
    }

    /// <summary>
    /// Renova o access token usando o refresh token. Achado C1: o refresh token agora chega
    /// preferencialmente pelo cookie HttpOnly "refresh_token" (fluxo cross-subdomínio
    /// www/app) — o campo no corpo (RefreshTokenRequestBody) fica só como fallback de
    /// compatibilidade para clientes que ainda não migraram (ex.: apps mobile futuros).
    /// </summary>
    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestBody? body, CancellationToken ct)
    {
        var refreshToken = Request.Cookies[RefreshTokenCookieName] ?? body?.RefreshToken;
        if (string.IsNullOrEmpty(refreshToken))
            return Unauthorized(new { message = "Refresh token ausente." });

        var tokens = await sender.Send(
            new RefreshTokenCommand(refreshToken, HttpContext.Connection.RemoteIpAddress?.ToString()), ct);
        SetRefreshTokenCookie(tokens);
        return Ok(ToClientResponse(tokens));
    }

    /// <summary>Login com Google OAuth.</summary>
    [HttpPost("google")]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginCommand command, CancellationToken ct)
    {
        var tokens = await sender.Send(command, ct);
        SetRefreshTokenCookie(tokens);
        return Ok(ToClientResponse(tokens));
    }

    /// <summary>
    /// Encerra a sessão: revoga o refresh token no servidor (achado C1 — hoje não havia como
    /// invalidar um refresh token vazado/roubado antes dele expirar sozinho) e limpa o cookie
    /// HttpOnly. Idempotente — chamar sem cookie/já deslogado apenas limpa e retorna 200.
    /// </summary>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var refreshToken = Request.Cookies[RefreshTokenCookieName];
        await sender.Send(new LogoutCommand(refreshToken), ct);
        ClearRefreshTokenCookie();
        return Ok(new { message = "Sessão encerrada." });
    }

    /// <summary>Confirma e-mail do usuário com o código de 6 dígitos enviado no cadastro.</summary>
    [HttpPost("confirm-email")]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailCommand command, CancellationToken ct)
    {
        await sender.Send(command, ct);
        return Ok(new { message = "E-mail confirmado com sucesso." });
    }

    /// <summary>
    /// Solicita redefinição de senha. Achado B8 da avaliação: rate limit por IP — sem isso,
    /// dava pra usar este endpoint para enumerar e-mails cadastrados em massa (timing/volume)
    /// ou para bombardear a caixa de entrada de terceiros com e-mails de redefinição.
    /// </summary>
    [HttpPost("forgot-password")]
    [EnableRateLimiting("auth")]
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

    /// <summary>
    /// Achado em teste manual: comprador que perde a janela de 48h do Magic Link do e-mail
    /// pós-compra (ver CoursePurchaseConfirmedEventHandler) ficava sem nenhum jeito self-service
    /// de entrar, já que a conta nasce sem senha (User.RegisterFromPurchase). Reenvia um novo
    /// Magic Link de 48h pro e-mail informado, se existir conta com ele. Mesmo rate limit e
    /// mensagem genérica de /auth/forgot-password — não revela se o e-mail existe.
    /// </summary>
    [HttpPost("resend-access-link")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ResendAccessLink([FromBody] ResendAccessLinkCommand command, CancellationToken ct)
    {
        await sender.Send(command, ct);
        return Ok(new { message = "Se este e-mail existe, você receberá um novo link de acesso em breve." });
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
        SetRefreshTokenCookie(tokens);
        return Ok(ToClientResponse(tokens));
    }
}

public sealed record UpdateProfileRequest(
    string FirstName, string LastName, string? Phone, DateOnly? BirthDate, string? Bio, string? AvatarUrl);

public sealed record ConsumeMagicLinkRequest(string Token);

/// <summary>Fallback de compatibilidade para /auth/refresh-token — ver comentário no controller.</summary>
public sealed record RefreshTokenRequestBody(string? RefreshToken);
