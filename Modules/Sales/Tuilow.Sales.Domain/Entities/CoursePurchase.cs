using Tuilow.SharedKernel.Domain.Common;
using Tuilow.Catalog.Domain.ValueObjects;
using Tuilow.Sales.Domain.Enums;
using Tuilow.Sales.Domain.Events;

namespace Tuilow.Sales.Domain.Entities;

/// <summary>
/// Compra avulsa de um curso por um aluno — pagamento único (não recorrente), diferente de
/// <see cref="Subscription"/> (assinatura da plataforma, modelo legado). É sobre esta entidade
/// que a comissão da plataforma é calculada.
///
/// Duas formas de cobrança coexistem (ver <see cref="CoursePurchasePaymentModel"/>):
///   - Legacy: cobrança criada na conta Asaas da própria Tuilow; a comissão é calculada e
///     creditada numa carteira interna do criador (módulo Finance, CreatorWallet) para saque
///     quinzenal manual. Modelo histórico, mantido intacto para compras já existentes e para
///     criadores que ainda não conectaram uma conta Asaas própria.
///   - MarketplaceSplit: cobrança criada DIRETAMENTE na conta Asaas do próprio criador (ele é
///     o emissor/vendedor da cobrança), com um split automático da Asaas mandando a comissão da
///     Tuilow para a walletId da plataforma — o restante já fica com o criador, sem passar pela
///     Tuilow em nenhum momento. Ver módulo Finance (CreatorAsaasAccount) e
///     Sales.Infrastructure.Services.AsaasMarketplacePaymentService.
///
/// O percentual/valores de comissão de uma venda MarketplaceSplit são um SNAPSHOT tirado no
/// momento da criação da compra — nunca recalculados depois, mesmo que a configuração de
/// comissão mude no futuro (ver CommissionPercentageSnapshot/PlatformCommissionAmount/CreatorNetAmount).
/// </summary>
public sealed class CoursePurchase : AggregateRoot
{
    public Guid StudentId { get; private set; }
    public Guid CourseId { get; private set; }
    public Guid CreatorId { get; private set; }
    public Money Amount { get; private set; } = null!;
    public CoursePurchaseStatus Status { get; private set; } = CoursePurchaseStatus.Pending;
    public string AsaasCustomerId { get; private set; } = string.Empty;
    public string AsaasPaymentId { get; private set; } = string.Empty;
    public DateTime? ConfirmedAt { get; private set; }
    public DateTime? RefundedAt { get; private set; }

    public CoursePurchasePaymentModel PaymentModel { get; private set; } = CoursePurchasePaymentModel.Legacy;

    /// <summary>Preenchido apenas quando PaymentModel == MarketplaceSplit — a conta Asaas do criador usada nesta cobrança.</summary>
    public Guid? CreatorAsaasAccountId { get; private set; }

    /// <summary>Percentual de comissão aplicado nesta venda (snapshot). Nulo em compras Legacy.</summary>
    public decimal? CommissionPercentageSnapshot { get; private set; }

    /// <summary>Valor previsto da comissão da Tuilow no momento da compra (bruto x percentual) — só para MarketplaceSplit.</summary>
    public Money? PlatformCommissionAmount { get; private set; }

    /// <summary>Valor líquido previsto do criador no momento da compra (bruto - comissão) — só para MarketplaceSplit.</summary>
    public Money? CreatorNetAmount { get; private set; }

    /// <summary>
    /// Valor líquido informado pela Asaas no webhook de confirmação (netValue, já descontadas
    /// as taxas de meio de pagamento da própria Asaas) — usado para conciliação dos valores
    /// previstos acima contra o que a Asaas efetivamente processou. Nulo até o webhook de
    /// confirmação chegar, ou se a Asaas não informar netValue para o método de pagamento usado.
    /// </summary>
    public Money? AsaasNetValueReceived { get; private set; }

    private CoursePurchase() { }

    /// <summary>Modelo legado — cobrança na conta Asaas da própria Tuilow. Assinatura preservada (compras antigas e o job de reconciliação dependem dela).</summary>
    public static CoursePurchase Create(
        Guid studentId, Guid courseId, Guid creatorId, decimal amount,
        string asaasCustomerId, string asaasPaymentId)
    {
        return new CoursePurchase
        {
            StudentId = studentId,
            CourseId = courseId,
            CreatorId = creatorId,
            Amount = Money.Of(amount),
            AsaasCustomerId = asaasCustomerId,
            AsaasPaymentId = asaasPaymentId,
            Status = CoursePurchaseStatus.Pending,
            PaymentModel = CoursePurchasePaymentModel.Legacy
        };
    }

    /// <summary>
    /// Modelo novo — cobrança criada na conta Asaas do próprio criador, com split de comissão
    /// para a Tuilow. commissionPercentage já vem resolvido pelo caller (override do criador ou
    /// padrão da plataforma vigente) e é gravado aqui como snapshot definitivo desta venda.
    /// </summary>
    public static CoursePurchase CreateForMarketplace(
        Guid studentId, Guid courseId, Guid creatorId, decimal amount,
        Guid creatorAsaasAccountId, string asaasCustomerId, string asaasPaymentId,
        decimal commissionPercentage)
    {
        var gross = Money.Of(amount);
        var commission = Money.Of(Math.Round(gross.Amount * commissionPercentage / 100m, 2));
        var net = gross.Subtract(commission);

        return new CoursePurchase
        {
            StudentId = studentId,
            CourseId = courseId,
            CreatorId = creatorId,
            Amount = gross,
            AsaasCustomerId = asaasCustomerId,
            AsaasPaymentId = asaasPaymentId,
            Status = CoursePurchaseStatus.Pending,
            PaymentModel = CoursePurchasePaymentModel.MarketplaceSplit,
            CreatorAsaasAccountId = creatorAsaasAccountId,
            CommissionPercentageSnapshot = commissionPercentage,
            PlatformCommissionAmount = commission,
            CreatorNetAmount = net
        };
    }

    public void ConfirmPayment()
    {
        if (Status == CoursePurchaseStatus.Confirmed) return; // idempotente (webhook pode repetir evento)

        Status = CoursePurchaseStatus.Confirmed;
        ConfirmedAt = DateTime.UtcNow;
        Touch();

        AddDomainEvent(new CoursePurchaseConfirmedDomainEvent(
            Id, StudentId, CourseId, CreatorId, Amount.Amount, AsaasPaymentId, PaymentModel));
    }

    /// <summary>Registra o netValue informado pela Asaas no webhook, para conciliação — não afeta liberacao de acesso nem status.</summary>
    public void RecordAsaasNetValue(decimal netValue)
    {
        AsaasNetValueReceived = Money.Of(netValue);
        Touch();
    }

    public void MarkFailed()
    {
        // Guarda de estado: um webhook atrasado/fora de ordem (ex.: PAYMENT_OVERDUE chegando
        // depois de um PAYMENT_CONFIRMED já processado) não pode revogar o acesso de um aluno
        // que já pagou, nem sobrescrever um reembolso já registrado.
        if (Status != CoursePurchaseStatus.Pending) return;

        Status = CoursePurchaseStatus.Failed;
        Touch();
    }

    public void Refund()
    {
        if (Status != CoursePurchaseStatus.Confirmed)
            throw new InvalidOperationException("Só é possível reembolsar uma compra confirmada.");

        Status = CoursePurchaseStatus.Refunded;
        RefundedAt = DateTime.UtcNow;
        Touch();

        AddDomainEvent(new CoursePurchaseRefundedDomainEvent(Id, CreatorId, Amount.Amount, PaymentModel));
    }
}
