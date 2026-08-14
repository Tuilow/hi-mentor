namespace HiMentor.SharedKernel.Application.Interfaces;

/// <summary>
/// Reaproveitado de HiMentor.Application.Common.Interfaces.IEmailService — movido para o SharedKernel
/// porque é usado por múltiplos módulos (IdentidadeAcesso: welcome/reset; Sales: pagamento;
/// Learning: certificado), não é exclusivo de um bounded context.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// userId vai junto porque /auth/confirm-email exige UserId + Token juntos (não existe busca
    /// de usuário por token de confirmação sozinho) — sem ele o link do e-mail de boas-vindas
    /// não tinha como funcionar.
    /// </summary>
    Task SendWelcomeAsync(Guid userId, string to, string firstName, string confirmationToken, CancellationToken ct = default);
    Task SendPasswordResetAsync(string to, string firstName, string resetToken, CancellationToken ct = default);
    Task SendPaymentConfirmedAsync(string to, string firstName, decimal amount, CancellationToken ct = default);

    /// <summary>
    /// Achado A4 da avaliação: Certificate existia no domínio mas nunca era emitido nem enviado —
    /// ver CourseCompletedEventHandler (Learning.Application/EventHandlers), que agora chama isto
    /// ao emitir um certificado real. certificateCode (não mais uma URL pronta) — a implementação
    /// monta o link de verificação pública a partir dele, mesmo padrão de courseSlug em
    /// SendCourseAccessGrantedAsync/SendMagicLinkAccessAsync abaixo.
    /// </summary>
    Task SendCertificateAsync(string to, string firstName, string courseTitle, string certificateCode, CancellationToken ct = default);

    /// <summary>
    /// Disparado quando o acesso a um curso é liberado (compra avulsa confirmada ou assinatura
    /// do produto confirmada) — avisa o aluno com um link direto para a página do curso, já que
    /// a conta/matrícula não são criadas automaticamente no ato do pagamento (ver
    /// HiMentor.Learning.Application.EventHandlers).
    /// </summary>
    Task SendCourseAccessGrantedAsync(string to, string firstName, string courseTitle, string courseSlug, CancellationToken ct = default);

    /// <summary>
    /// Disparado no lugar de <see cref="SendCourseAccessGrantedAsync"/> quando há um Magic Link
    /// disponível (compra avulsa de curso confirmada ou assinatura do produto confirmada) — o
    /// botão do e-mail já entra o aluno direto na área liberada, sem senha. Cobre tanto quem já
    /// tinha conta quanto quem acabou de ser criado automaticamente no checkout anônimo.
    ///
    /// channelHandle: quando o criador do curso tem um Canal público (Modules/Channel), o
    /// comprador entra direto em /canal/{handle} — a vitrine com todos os cursos do criador,
    /// onde o curso comprado já aparece destravado e os demais ficam com cadeado — em vez de
    /// cair só na página do curso individual. Null (sem canal configurado) preserva o
    /// comportamento antigo, indo direto para /cursos/{courseSlug}.
    /// </summary>
    Task SendMagicLinkAccessAsync(string to, string firstName, string courseTitle, string courseSlug, string magicLinkToken, CancellationToken ct = default, string? channelHandle = null);

    /// <summary>
    /// Reenvio self-service de um Magic Link, disparado por /auth/resend-access-link — achado
    /// em teste manual: o Magic Link do e-mail pós-compra (ver SendMagicLinkAccessAsync) expira
    /// em 48h e é de uso único; quem perdia essa janela não tinha nenhum jeito self-service de
    /// voltar a entrar sem senha (a conta nasce sem senha, ver User.RegisterFromPurchase). Não
    /// amarrado a um curso específico (diferente de SendMagicLinkAccessAsync) porque o pedido de
    /// reenvio só tem o e-mail — o link cai no /dashboard, de onde dá pra ver todos os cursos
    /// matriculados.
    /// </summary>
    Task SendAccessLinkAsync(string to, string firstName, string magicLinkToken, CancellationToken ct = default);
}
