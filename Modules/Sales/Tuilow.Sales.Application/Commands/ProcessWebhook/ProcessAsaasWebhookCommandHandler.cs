using Tuilow.SharedKernel.Application.Interfaces;
using Tuilow.Sales.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Tuilow.Sales.Application.Commands.ProcessWebhook;

/// <summary>
/// Um único webhook do Asaas atende tres fluxos de pagamento do Sales:
///   - Assinatura da plataforma (Subscription/SubscriptionPayment) — modelo legado, sempre na
///     conta da propria Tuilow.
///   - Compra avulsa de curso Legacy (CoursePurchase.PaymentModel == Legacy) — tambem na conta
///     da propria Tuilow.
///   - Compra avulsa de curso MarketplaceSplit — cobranca criada na conta Asaas do proprio
///     creator; o webhook chega autenticado com o token daquela conta especifica (ver
///     CreatorAsaasAccountId, resolvido pelo controller via IAsaasWebhookAuthenticator).
/// O evento chega sem indicar de qual fluxo se trata; o discriminador principal e a presenca
/// (ou nao) do campo "subscription" no payload — pagamentos avulsos nunca tem esse campo
/// preenchido.
/// </summary>
public sealed class ProcessAsaasWebhookCommandHandler(
    ISubscriptionRepository subscriptionRepository,
    ICoursePurchaseRepository coursePurchaseRepository,
    IUnitOfWork uow,
    ILogger<ProcessAsaasWebhookCommandHandler> logger
) : IRequestHandler<ProcessAsaasWebhookCommand>
{
    public async Task Handle(ProcessAsaasWebhookCommand request, CancellationToken ct)
    {
        var payload = request.Payload;

        // Eventos de divergencia de split (documentacao Asaas: PAYMENT_SPLIT_DIVERGENCE_BLOCK /
        // _FINISHED) nao correspondem ao ciclo de vida normal de uma cobranca -- so registramos
        // em log critico para o admin investigar manualmente (ver painel "Financeiro ->
        // Creators/Contas Asaas"); nao ha estado de CoursePurchase para atualizar aqui.
        if (payload.Event is "PAYMENT_SPLIT_DIVERGENCE_BLOCK" or "PAYMENT_SPLIT_DIVERGENCE_BLOCK_FINISHED")
        {
            logger.LogCritical(
                "Evento de divergencia de split recebido da Asaas: {Event} para o pagamento {PaymentId} " +
                "(CreatorAsaasAccountId {CreatorAsaasAccountId}) -- verificar manualmente no painel admin.",
                payload.Event, payload.Payment.Id, request.CreatorAsaasAccountId);
            return;
        }

        if (!string.IsNullOrEmpty(payload.Payment.Subscription))
        {
            await HandleSubscriptionPaymentAsync(payload, ct);
            return;
        }

        await HandleCoursePurchasePaymentAsync(payload, request.CreatorAsaasAccountId, ct);
    }

    // ─── Assinatura da plataforma (modelo legado) ──────────────────────────────

    private async Task HandleSubscriptionPaymentAsync(AsaasWebhookPayload payload, CancellationToken ct)
    {
        var subscription = await subscriptionRepository
            .GetByAsaasSubscriptionIdAsync(payload.Payment.Subscription!, ct);

        if (subscription is null)
        {
            logger.LogWarning("Assinatura não encontrada para ID Asaas: {Id}", payload.Payment.Subscription);
            return;
        }

        // Registra explicitamente como Added quando um novo SubscriptionPayment é criado —
        // evita DbUpdateConcurrencyException (mesmo padrão de Catalog.AddModule/Learning.CompleteLesson).
        switch (payload.Event)
        {
            case "PAYMENT_RECEIVED":
            case "PAYMENT_CONFIRMED":
                var confirmedPayment = subscription.ConfirmPayment(payload.Payment.Id, payload.Payment.Value);
                if (confirmedPayment is not null)
                    await subscriptionRepository.AddPaymentAsync(confirmedPayment, ct);
                logger.LogInformation("Pagamento de assinatura confirmado: {PaymentId}", payload.Payment.Id);
                break;

            case "PAYMENT_OVERDUE":
            case "PAYMENT_DELETED":
                var failedPayment = subscription.MarkPaymentFailed(payload.Payment.Id, payload.Payment.Value);
                if (failedPayment is not null)
                    await subscriptionRepository.AddPaymentAsync(failedPayment, ct);
                logger.LogWarning("Pagamento de assinatura falhou: {PaymentId}", payload.Payment.Id);
                break;

            case "PAYMENT_REFUNDED":
                // Faltava esse case — SubscriptionPayment.Refund() existia na entidade mas nunca
                // era chamado (achado M1 da auditoria). Revoga o acesso na hora (ver
                // Subscription.RefundPayment), diferente do cancelamento voluntário do aluno.
                subscription.RefundPayment(payload.Payment.Id);
                logger.LogInformation("Pagamento de assinatura reembolsado: {PaymentId}", payload.Payment.Id);
                break;

            default:
                logger.LogDebug("Evento Asaas ignorado (assinatura): {Event}", payload.Event);
                return;
        }

        subscriptionRepository.Update(subscription);
        await uow.SaveChangesAsync(ct);
    }

    // ─── Compra avulsa de curso (Legacy ou MarketplaceSplit) ───────────────────

    private async Task HandleCoursePurchasePaymentAsync(AsaasWebhookPayload payload, Guid? authenticatedCreatorAsaasAccountId, CancellationToken ct)
    {
        var purchase = await coursePurchaseRepository.GetByAsaasPaymentIdAsync(payload.Payment.Id, ct);

        if (purchase is null)
        {
            logger.LogInformation("Webhook Asaas sem compra de curso correspondente: {PaymentId}", payload.Payment.Id);
            return;
        }

        // Checagem de segurança: um webhook autenticado com o token de uma CreatorAsaasAccount
        // só pode afetar compras vinculadas àquela MESMA conta -- protege contra o token de um
        // creator sendo usado (por bug ou má-fé) para tentar confirmar/reembolsar a compra de
        // outro. Webhooks da conta legada (authenticatedCreatorAsaasAccountId == null) só devem
        // afetar compras Legacy (CreatorAsaasAccountId == null na compra).
        if (purchase.CreatorAsaasAccountId != authenticatedCreatorAsaasAccountId)
        {
            logger.LogCritical(
                "Webhook Asaas REJEITADO: pagamento {PaymentId} pertence à compra {PurchaseId} vinculada à " +
                "conta {ExpectedAccountId}, mas o webhook foi autenticado com a conta {ActualAccountId} — " +
                "possível uso indevido de token de webhook.",
                payload.Payment.Id, purchase.Id, purchase.CreatorAsaasAccountId, authenticatedCreatorAsaasAccountId);
            return;
        }

        switch (payload.Event)
        {
            case "PAYMENT_RECEIVED":
            case "PAYMENT_CONFIRMED":
                purchase.ConfirmPayment(); // idempotente — dispara CoursePurchaseConfirmedDomainEvent (Finance credita a carteira do criador, só no modelo Legacy)
                if (payload.Payment.NetValue is decimal netValue)
                    purchase.RecordAsaasNetValue(netValue);
                logger.LogInformation("Compra de curso confirmada: {PaymentId}", payload.Payment.Id);
                break;

            case "PAYMENT_OVERDUE":
            case "PAYMENT_DELETED":
                purchase.MarkFailed();
                logger.LogWarning("Compra de curso falhou: {PaymentId}", payload.Payment.Id);
                break;

            case "PAYMENT_REFUNDED":
                purchase.Refund(); // dispara CoursePurchaseRefundedDomainEvent (Finance estorna a carteira do criador, só no modelo Legacy — a Asaas já reverteu o split sozinha em MarketplaceSplit)
                logger.LogWarning("Compra de curso reembolsada: {PaymentId}", payload.Payment.Id);
                break;

            default:
                logger.LogDebug("Evento Asaas ignorado (compra de curso): {Event}", payload.Event);
                return;
        }

        coursePurchaseRepository.Update(purchase);
        await uow.SaveChangesAsync(ct);
    }
}
