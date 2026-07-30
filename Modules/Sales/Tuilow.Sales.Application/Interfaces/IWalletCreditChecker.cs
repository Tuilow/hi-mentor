namespace Tuilow.Sales.Application.Interfaces;

/// <summary>
/// Porta (anti-corruption layer) que abstrai "esta compra já foi creditada na carteira do
/// criador?" sem o módulo Sales depender diretamente do domínio de Finance — mesmo padrão de
/// <see cref="ICourseAccessChecker"/>-style ports já usados nesta base (ex.: Learning →
/// IUserContactLookup). Usada pela reconciliação do achado A5: sem retentativa automática nem
/// outbox, se o handler de Finance falhar depois do commit da venda, nada além de um log
/// avisava — este job periódico compara Confirmed × crédito na carteira e alerta.
/// </summary>
public interface IWalletCreditChecker
{
    Task<bool> HasCreditForPurchaseAsync(Guid coursePurchaseId, CancellationToken ct = default);
}
