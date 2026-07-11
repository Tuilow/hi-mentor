using Tuilow.SharedKernel.Application.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace Tuilow.SharedKernel.Infrastructure.Email;

/// <summary>
/// Reaproveitado de Tuilow.Infrastructure.Services.Email.EmailService — movido para o
/// SharedKernel porque IEmailService é usado por múltiplos módulos (IdentidadeAcesso, Sales, Learning).
/// </summary>
public sealed class EmailService(
    IConfiguration configuration,
    ILogger<EmailService> logger
) : IEmailService
{
    private readonly string _from = configuration["Email:From"] ?? "noreply@tuilow.com.br";
    private readonly string _fromName = configuration["Email:FromName"] ?? "Tuilow";
    private readonly string _host = configuration["Email:Host"] ?? "smtp.mailgun.org";
    private readonly int _port = int.Parse(configuration["Email:Port"] ?? "587");
    private readonly string _username = configuration["Email:Username"] ?? "";
    private readonly string _password = configuration["Email:Password"] ?? "";
    private readonly string _frontendUrl = configuration["FrontendUrl"] ?? "https://app.tuilow.com.br";

    public async Task SendWelcomeAsync(string to, string firstName, string confirmationToken, CancellationToken ct = default)
    {
        var confirmUrl = $"{_frontendUrl}/confirmar-email?token={confirmationToken}";
        var body = $"""
            <h2>Olá, {firstName}! Bem-vindo(a) à Tuilow 🎓</h2>
            <p>Estamos empolgados em ter você aqui! Confirme seu e-mail para ativar sua conta:</p>
            <a href="{confirmUrl}" style="background:#2563EB;color:white;padding:12px 24px;border-radius:6px;text-decoration:none;">
                Confirmar E-mail
            </a>
            <p>Se você não criou esta conta, ignore este e-mail.</p>
            """;
        await SendAsync(to, $"Bem-vindo(a) à Tuilow, {firstName}!", body, ct);
    }

    public async Task SendPasswordResetAsync(string to, string firstName, string resetToken, CancellationToken ct = default)
    {
        var resetUrl = $"{_frontendUrl}/redefinir-senha?token={resetToken}";
        var body = $"""
            <h2>Redefinição de Senha — Tuilow</h2>
            <p>Olá, {firstName}! Recebemos uma solicitação para redefinir sua senha.</p>
            <a href="{resetUrl}" style="background:#EA580C;color:white;padding:12px 24px;border-radius:6px;text-decoration:none;">
                Redefinir Senha
            </a>
            <p>Este link expira em 1 hora. Se você não solicitou, ignore este e-mail.</p>
            """;
        await SendAsync(to, "Redefinição de senha — Tuilow", body, ct);
    }

    public async Task SendPaymentConfirmedAsync(string to, string firstName, decimal amount, CancellationToken ct = default)
    {
        var body = $"""
            <h2>Pagamento confirmado! 🎉</h2>
            <p>Olá, {firstName}! Seu pagamento de R$ {amount:F2} foi confirmado.</p>
            <p>Sua assinatura está ativa. Bons estudos!</p>
            <a href="{_frontendUrl}/dashboard" style="background:#16A34A;color:white;padding:12px 24px;border-radius:6px;text-decoration:none;">
                Acessar a Plataforma
            </a>
            """;
        await SendAsync(to, "Pagamento confirmado — Tuilow", body, ct);
    }

    public async Task SendCourseAccessGrantedAsync(string to, string firstName, string courseTitle, string courseSlug, CancellationToken ct = default)
    {
        var courseUrl = $"{_frontendUrl}/cursos/{courseSlug}";
        var body = $"""
            <h2>Pagamento confirmado! 🎉</h2>
            <p>Olá, {firstName}! Seu acesso ao curso <strong>{courseTitle}</strong> já está liberado.</p>
            <p>Se ainda não tem uma conta na Tuilow com este e-mail, crie uma gratuitamente para acessar o conteúdo.</p>
            <a href="{courseUrl}" style="background:#2563EB;color:white;padding:12px 24px;border-radius:6px;text-decoration:none;">
                Acessar o Curso
            </a>
            """;
        await SendAsync(to, $"Acesso liberado: {courseTitle} — Tuilow", body, ct);
    }

    public async Task SendMagicLinkAccessAsync(string to, string firstName, string courseTitle, string courseSlug, string magicLinkToken, CancellationToken ct = default)
    {
        var magicLinkUrl = $"{_frontendUrl}/acesso?token={magicLinkToken}&redirect={Uri.EscapeDataString($"/cursos/{courseSlug}")}";
        var body = $"""
            <h2>Pagamento confirmado! 🎉</h2>
            <p>Olá, {firstName}! Seu acesso ao curso <strong>{courseTitle}</strong> já está liberado.</p>
            <a href="{magicLinkUrl}" style="background:#2563EB;color:white;padding:12px 24px;border-radius:6px;text-decoration:none;">
                Entrar no Curso Agora
            </a>
            <p style="color:#6B7280;font-size:13px;">Este link te leva direto para o curso, sem precisar de senha. Válido por 48 horas.</p>
            """;
        await SendAsync(to, $"Acesso liberado: {courseTitle} — Tuilow", body, ct);
    }

    public async Task SendCertificateAsync(string to, string firstName, string courseTitle, string certificateUrl, CancellationToken ct = default)
    {
        var body = $"""
            <h2>Parabéns, {firstName}! 🏆</h2>
            <p>Você concluiu o curso <strong>{courseTitle}</strong>!</p>
            <a href="{certificateUrl}" style="background:#1D4ED8;color:white;padding:12px 24px;border-radius:6px;text-decoration:none;">
                Baixar Certificado
            </a>
            """;
        await SendAsync(to, $"Certificado de Conclusão — {courseTitle}", body, ct);
    }

    private async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_fromName, _from));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;
            message.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = htmlBody };

            using var client = new SmtpClient();
            await client.ConnectAsync(_host, _port, SecureSocketOptions.StartTls, ct);
            await client.AuthenticateAsync(_username, _password, ct);
            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao enviar e-mail para {To}: {Subject}", to, subject);
        }
    }
}
