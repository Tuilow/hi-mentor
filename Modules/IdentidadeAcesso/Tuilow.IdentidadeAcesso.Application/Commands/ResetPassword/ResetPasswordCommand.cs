using MediatR;

namespace Tuilow.IdentidadeAcesso.Application.Commands.ResetPassword;

/// <summary>
/// Consome o token emitido por <see cref="ForgotPassword.ForgotPasswordCommand"/> (link enviado
/// por e-mail, ver EmailService.SendPasswordResetAsync) e define a nova senha. Só o token vai no
/// link — diferente do fluxo de confirmação de e-mail, aqui não há UserId disponível no front,
/// então o usuário é localizado por token (ver IUserRepository.GetByPasswordResetTokenAsync,
/// mesmo padrão já usado para Magic Link).
/// </summary>
public sealed record ResetPasswordCommand(string Token, string NewPassword) : IRequest;
