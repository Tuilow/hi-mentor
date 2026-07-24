using System.Net.Http.Headers;
using System.Text;
using Tuilow.SharedKernel.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Tuilow.SharedKernel.Infrastructure.Email;

/// <summary>
/// Reaproveitado de Tuilow.Infrastructure.Services.Email.EmailService — movido para o
/// SharedKernel porque IEmailService é usado por múltiplos módulos (IdentidadeAcesso, Sales, Learning).
///
/// Envia via API HTTP do Mailgun (porta 443) em vez de SMTP (porta 587/465) — trocado porque em
/// produção (Railway) a conexão SMTP não estava nem chegando ao Mailgun (nenhum registro aparecia
/// nos Logs do Mailgun para as tentativas), possivelmente porque o container encerra a tarefa em
/// segundo plano do e-mail (fire-and-forget, ver RegisterUserCommandHandler) antes do handshake
/// SMTP terminar. A API HTTP é uma chamada única e rápida, sem handshake de conexão demorado,
/// então tem muito mais chance de completar antes do processo seguir em frente.
/// </summary>
public sealed class EmailService(
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory,
    ILogger<EmailService> logger
) : IEmailService
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient();
    private readonly string _from = configuration["Email:From"] ?? "noreply@tuilow.com.br";
    private readonly string _fromName = configuration["Email:FromName"] ?? "Tuilow";
    // Domínio cadastrado no Mailgun (Sending > Domains) — NÃO é necessariamente igual ao domínio
    // do endereço "From" acima, embora normalmente sejam o mesmo.
    private readonly string _domain = configuration["Email:Domain"] ?? "tuilow.com.br";
    // Chave de API do Mailgun (Settings > API Keys > Private API key) — DIFERENTE da senha SMTP
    // usada antes. Precisa ser configurada em Email__ApiKey no Railway.
    private readonly string _apiKey = configuration["Email:ApiKey"] ?? "";
    // https://api.mailgun.net para contas US, https://api.eu.mailgun.net para contas EU (mesma
    // região do host SMTP antigo — smtp.mailgun.org = US, smtp.eu.mailgun.org = EU).
    private readonly string _apiBaseUrl = configuration["Email:ApiBaseUrl"] ?? "https://api.mailgun.net";
    private readonly string _frontendUrl = configuration["FrontendUrl"] ?? "https://app.tuilow.com.br";

    public async Task SendWelcomeAsync(Guid userId, string to, string firstName, string confirmationToken, CancellationToken ct = default)
    {
        // Sprint Item 4: dupla confirmação por e-mail antes de liberar login — confirmationToken
        // agora é um código curto de 6 dígitos (ver User.Register), não mais um GUID. O link
        // abaixo continua funcionando (confirma automaticamente ao clicar), mas o código também
        // é exibido em destaque pra quem preferir digitá-lo manualmente na tela de confirmação.
        var confirmUrl = $"{_frontendUrl}/confirmar-email?email={Uri.EscapeDataString(to)}&code={confirmationToken}";
        var body = $"""
            <h2>Olá, {firstName}! Bem-vindo(a) à Tuilow 🎓</h2>
            <p>Use o código abaixo para confirmar seu e-mail e ativar sua conta:</p>
            <div style="font-size:32px;font-weight:bold;letter-spacing:6px;background:#F3F4F6;color:#111827;padding:16px 24px;border-radius:8px;text-align:center;margin:16px 0;">
                {confirmationToken}
            </div>
            <p>Ou, se preferir, clique no botão abaixo para confirmar automaticamente:</p>
            <a href="{confirmUrl}" style="background:#2563EB;color:white;padding:12px 24px;border-radius:6px;text-decoration:none;">
                Confirmar E-mail e Acessar
            </a>
            <p>Se você não criou esta conta, ignore este e-mail.</p>
            """;
        await SendAsync(to, $"Seu código de confirmação Tuilow: {confirmationToken}", body, ct);
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
            using var request = new HttpRequestMessage(HttpMethod.Post, $"{_apiBaseUrl}/v3/{_domain}/messages");
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"api:{_apiKey}")));
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["from"] = $"{_fromName} <{_from}>",
                ["to"] = to,
                ["subject"] = subject,
                ["html"] = htmlBody,
            });

            using var response = await _httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync(ct);
                logger.LogError(
                    "Erro ao enviar e-mail para {To} via Mailgun API ({Status}): {Body}",
                    to, response.StatusCode, responseBody);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao enviar e-mail para {To}: {Subject}", to, subject);
        }
    }
}
