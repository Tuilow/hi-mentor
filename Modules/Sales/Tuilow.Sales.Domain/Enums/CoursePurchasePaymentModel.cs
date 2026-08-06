namespace Tuilow.Sales.Domain.Enums;

/// <summary>Ver Tuilow.Sales.Domain.Entities.CoursePurchase para o racional completo dos dois modelos de cobranca.</summary>
public enum CoursePurchasePaymentModel
{
    /// <summary>Cobranca na conta Asaas da propria Tuilow; comissao creditada na carteira interna do criador (CreatorWallet).</summary>
    Legacy = 0,

    /// <summary>Cobranca na conta Asaas do proprio criador, com split automatico da comissao para a Tuilow.</summary>
    MarketplaceSplit = 1
}
