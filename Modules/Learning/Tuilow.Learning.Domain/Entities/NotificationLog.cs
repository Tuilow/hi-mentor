using Tuilow.SharedKernel.Domain.Common;

namespace Tuilow.Learning.Domain.Entities;

/// <summary>
/// Log mínimo de tentativas de notificação (achado M12 da auditoria) — cobre o e-mail de acesso
/// liberado disparado por CoursePurchaseConfirmedEventHandler/SubscriptionPaymentConfirmedEventHandler.
/// Antes disso, investigar um chamado de suporte do tipo "paguei e não recebi acesso" exigia ler
/// logs de texto do ILogger (não pesquisável, não correlacionado). Cada linha aqui guarda o mesmo
/// AsaasPaymentId que já existe em CoursePurchase/SubscriptionPayment — é o identificador comum
/// que amarra as três pontas (pagamento em Sales, matrícula em Learning via
/// Enrollment.SourcePurchaseId/SourceSubscriptionId, e a tentativa de notificação aqui).
/// </summary>
public sealed class NotificationLog : Entity
{
    public string Channel { get; private set; } = string.Empty; // "Email" | "WhatsApp"
    public string Template { get; private set; } = string.Empty; // ex.: "MagicLinkAccess", "CourseAccessGranted"
    public string Recipient { get; private set; } = string.Empty;
    public string? AsaasPaymentId { get; private set; }
    public Guid? CorrelationId { get; private set; } // CoursePurchaseId ou SubscriptionId
    public bool Success { get; private set; }
    public string? Error { get; private set; }

    private NotificationLog() { }

    public static NotificationLog Record(
        string channel, string template, string recipient,
        string? asaasPaymentId, Guid? correlationId, bool success, string? error) =>
        new()
        {
            Channel = channel,
            Template = template,
            Recipient = recipient,
            AsaasPaymentId = asaasPaymentId,
            CorrelationId = correlationId,
            Success = success,
            // Truncado defensivamente — mensagens de exceção podem ser bem longas (stack de
            // provedores de e-mail de terceiros) e isto é só um log de auditoria, não a fonte de
            // verdade do erro (esse continua indo pro ILogger/log crítico do AppDbContext).
            Error = error?.Length > 500 ? error[..500] : error
        };
}
