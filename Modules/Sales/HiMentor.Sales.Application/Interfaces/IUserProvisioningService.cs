namespace HiMentor.Sales.Application.Interfaces;

/// <summary>
/// Porta (anti-corruption layer) que resolve "quem é este comprador?" sem o módulo Sales
/// depender diretamente do domínio de IdentidadeAcesso — mesmo padrão de IInstructorLookup
/// (Catalog) e IUserContactLookup (Learning). Usada pelo checkout anônimo: quando a compra é
/// feita sem login, localiza uma conta existente pelo e-mail informado ou cria uma nova
/// (sem senha — o acesso pós-pagamento é só por Magic Link).
/// </summary>
public interface IUserProvisioningService
{
    Task<Guid> FindOrCreateStudentAsync(string email, string fullName, CancellationToken ct = default);
}
