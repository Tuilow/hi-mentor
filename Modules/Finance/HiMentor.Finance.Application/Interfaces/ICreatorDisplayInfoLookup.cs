namespace HiMentor.Finance.Application.Interfaces;

public sealed record CreatorDisplayInfo(string Name, string Email);

/// <summary>
/// Porta que abstrai "nome/e-mail deste creator" para o painel admin de contas Asaas mostrar
/// algo legível em vez de só o Guid -- sem o modulo Finance depender diretamente do dominio de
/// IdentidadeAcesso (mesmo padrao de ICreatorPaymentAccountLookup em Sales). Implementada em
/// Finance.Infrastructure.
/// </summary>
public interface ICreatorDisplayInfoLookup
{
    Task<IReadOnlyDictionary<Guid, CreatorDisplayInfo>> GetManyAsync(IEnumerable<Guid> creatorIds, CancellationToken ct = default);
}
