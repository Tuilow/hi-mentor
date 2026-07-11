namespace Tuilow.SharedKernel.Application.Interfaces;

/// <summary>
/// Reaproveitado de Tuilow.Application.Common.Interfaces.IEmailService — movido para o SharedKernel
/// porque é usado por múltiplos módulos (IdentidadeAcesso: welcome/reset; Sales: pagamento;
/// Learning: certificado), não é exclusivo de um bounded context.
/// </summary>
public interface IEmailService
{
    Task SendWelcomeAsync(string to, string firstName, string confirmationToken, CancellationToken ct = default);
    Task SendPasswordResetAsync(string to, string firstName, string resetToken, CancellationToken ct = default);
    Task SendPaymentConfirmedAsync(string to, string firstName, decimal amount, CancellationToken ct = default);
    Task SendCertificateAsync(string to, string firstName, string courseTitle, string certificateUrl, CancellationToken ct = default);

    /// <summary>
    /// Disparado quando o acesso a um curso é liberado (compra avulsa confirmada ou assinatura
    /// do produto confirmada) — avisa o aluno com um link direto para a página do curso, já que
    /// a conta/matrícula não são criadas automaticamente no ato do pagamento (ver
    /// Tuilow.Learning.Application.EventHandlers).
    /// </summary>
    Task SendCourseAccessGrantedAsync(string to, string firstName, string courseTitle, string courseSlug, CancellationToken ct = default);

    /// <summary>
    /// Disparado no lugar de <see cref="SendCourseAccessGrantedAsync"/> quando há um Magic Link
    /// disponível (compra avulsa de curso confirmada) — o botão do e-mail já entra o aluno
    /// direto na área do curso, sem senha. Cobre tanto quem já tinha conta quanto quem acabou
    /// de ser criado automaticamente no checkout anônimo.
    /// </summary>
    Task SendMagicLinkAccessAsync(string to, string firstName, string courseTitle, string courseSlug, string magicLinkToken, CancellationToken ct = default);
}
