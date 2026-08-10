using Tuilow.SharedKernel.Domain.Common;

namespace Tuilow.Finance.Domain.Entities;

/// <summary>
/// Registro de dedupe para webhooks de status de conta (ACCOUNT_STATUS_*) — a Asaas garante
/// entrega "at-least-once" (documentação oficial: "eventos podem ser reenviados"), então o mesmo
/// evento pode chegar mais de uma vez. Uma linha aqui por EventId processado com sucesso; um
/// reenvio encontra a linha e é tratado como no-op (ver ProcessAsaasAccountStatusWebhookCommandHandler).
/// Não existe equivalente para o webhook de pagamento (PAYMENT_*) hoje — dedupe adicionado só
/// aqui, onde o briefing pede idempotência explícita (ver Pendências do relatório final sobre
/// retrofit no caminho de pagamento).
/// </summary>
public sealed class ProcessedAsaasAccountEvent : Entity
{
    /// <summary>Campo "id" do payload do webhook da Asaas — único por evento, mesmo em reenvio.</summary>
    public string AsaasEventId { get; private set; } = string.Empty;

    public string EventType { get; private set; } = string.Empty;

    private ProcessedAsaasAccountEvent() { }

    public static ProcessedAsaasAccountEvent Create(string asaasEventId, string eventType) => new()
    {
        AsaasEventId = asaasEventId,
        EventType = eventType
    };
}
