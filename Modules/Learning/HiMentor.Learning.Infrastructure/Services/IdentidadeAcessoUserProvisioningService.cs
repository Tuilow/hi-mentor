using HiMentor.IdentidadeAcesso.Domain.Entities;
using HiMentor.IdentidadeAcesso.Domain.Enums;
using HiMentor.IdentidadeAcesso.Domain.Interfaces;
using HiMentor.Learning.Application.Interfaces;

namespace HiMentor.Learning.Infrastructure.Services;

/// <summary>
/// Implementação real de <see cref="IUserProvisioningService"/> — mesma lógica de
/// HiMentor.Sales.Infrastructure.Services.IdentidadeAcessoUserProvisioningService (checkout
/// anônimo), copiada aqui em vez de reaproveitada porque Learning não pode depender do
/// Application de Sales (ver doc da interface). Não chama IUnitOfWork.SaveChangesAsync: quem
/// persiste é o handler da matrícula anônima, numa única transação junto com o Enrollment.
/// </summary>
public sealed class IdentidadeAcessoUserProvisioningService(
    IUserRepository userRepository,
    IRoleRepository roleRepository
) : IUserProvisioningService
{
    public async Task<Guid> FindOrCreateStudentAsync(string email, string fullName, CancellationToken ct = default)
    {
        var existing = await userRepository.GetByEmailAsync(email, ct);
        if (existing is not null) return existing.Id;

        var parts = fullName.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var firstName = parts.Length > 0 ? parts[0] : "Aluno";
        var lastName = parts.Length > 1 ? parts[1] : string.Empty;

        var studentRole = await roleRepository.GetByNameAsync(RoleNames.Student, ct);
        var user = User.RegisterFromPurchase(email, firstName, lastName, studentRole);

        await userRepository.AddAsync(user, ct);
        return user.Id;
    }
}
