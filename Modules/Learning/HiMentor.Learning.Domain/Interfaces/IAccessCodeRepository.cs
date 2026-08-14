using HiMentor.SharedKernel.Domain.Interfaces;
using HiMentor.Learning.Domain.Entities;

namespace HiMentor.Learning.Domain.Interfaces;

public interface IAccessCodeRepository : IRepository<AccessCode>
{
    Task<AccessCode?> GetByCodeAsync(string code, CancellationToken ct = default);

    /// <summary>Todos os códigos, mais recentes primeiro — painel Admin (GetAccessCodesAdminQuery).</summary>
    Task<IEnumerable<AccessCode>> GetAllAdminAsync(CancellationToken ct = default);

    /// <summary>Força EntityState.Added para o AccessCodeRedemption — evita DbUpdateConcurrencyException
    /// (mesmo padrão de IEnrollmentRepository.AddLessonProgressAsync).</summary>
    Task AddRedemptionAsync(AccessCodeRedemption redemption, CancellationToken ct = default);
}
