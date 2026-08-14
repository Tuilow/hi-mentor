using MediatR;

namespace HiMentor.Learning.Application.Commands.EnrollFreeCourseAnonymous;

/// <summary>
/// Matrícula em curso grátis sem exigir cadastro completo prévio (achado B2 da avaliação de UX:
/// o curso grátis pedia cadastro completo com senha, mais fricção que o checkout anônimo do
/// curso pago, que só pede nome/e-mail). UserId vem preenchido quando quem clicou já estava
/// logado (aí CustomerName/CustomerEmail são ignorados) — mesmo padrão de PurchaseCourseCommand
/// (Sales), que aceita tanto o comprador anônimo quanto um usuário já autenticado.
/// </summary>
public sealed record EnrollFreeCourseAnonymousCommand(
    Guid? UserId, Guid CourseId, string CustomerName, string CustomerEmail
) : IRequest<EnrollFreeCourseAnonymousResponse>;

/// <summary>MagicLinkSent indica se um e-mail com Magic Link foi disparado (só quando UserId é null).</summary>
public sealed record EnrollFreeCourseAnonymousResponse(Guid EnrollmentId, bool MagicLinkSent);
