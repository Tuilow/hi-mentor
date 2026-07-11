using Tuilow.IdentidadeAcesso.Domain.Entities;
using Tuilow.IdentidadeAcesso.Domain.Enums;
using Tuilow.IdentidadeAcesso.Domain.Interfaces;
using Tuilow.Sales.Application.Interfaces;

namespace Tuilow.Sales.Infrastructure.Services;

/// <summary>
/// Implementação real de <see cref="IUserProvisioningService"/> — consulta/grava no módulo
/// IdentidadeAcesso. Não chama IUnitOfWork.SaveChangesAsync: quem persiste é o
/// PurchaseCourseCommandHandler, numa única transação junto com a CoursePurchase (mesmo
/// DbContext, escopo por requisição — ver Host/Program.cs).
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
