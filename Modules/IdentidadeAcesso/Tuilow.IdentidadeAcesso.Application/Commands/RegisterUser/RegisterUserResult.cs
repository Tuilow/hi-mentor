namespace Tuilow.IdentidadeAcesso.Application.Commands.RegisterUser;

/// <summary>
/// Cadastro não faz mais login automático (Sprint Item 4 — dupla confirmação por e-mail antes de
/// permitir login). Devolve só o necessário pra tela de confirmação saber para qual e-mail foi
/// mandado o código (ver ConfirmEmailCommand) — sem tokens: o usuário só ganha accessToken/
/// refreshToken depois de confirmar o e-mail e fazer login de verdade em /auth/login.
/// </summary>
public sealed record RegisterUserResult(Guid UserId, string Email);
