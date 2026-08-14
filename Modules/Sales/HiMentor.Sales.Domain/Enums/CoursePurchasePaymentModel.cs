namespace HiMentor.Sales.Domain.Enums;

/// <summary>Ver HiMentor.Sales.Domain.Entities.CoursePurchase para o racional completo dos dois modelos de cobranca.</summary>
public enum CoursePurchasePaymentModel
{
    /// <summary>Cobranca na conta Asaas da propria HiMentor; comissao creditada na carteira interna do criador (CreatorWallet).</summary>
    Legacy = 0,

    /// <summary>Cobranca na conta Asaas do proprio criador, com split automatico da comissao para a HiMentor.</summary>
    MarketplaceSplit = 1
}
