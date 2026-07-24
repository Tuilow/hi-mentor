using Tuilow.SharedKernel.Application.Interfaces;
using Tuilow.Learning.Application.Interfaces;
using Tuilow.Learning.Domain.Interfaces;

namespace Tuilow.Learning.Infrastructure.Services;

/// <summary>
/// Implementação real de <see cref="IUserCourseAccessService"/> — ver a documentação completa
/// da regra de negócio na própria interface (SharedKernel.Application.Interfaces).
/// </summary>
public sealed class LearningCourseAccessService(
    IEnrollmentRepository enrollmentRepository,
    ICourseAccessChecker paidAccessChecker
) : IUserCourseAccessService
{
    public async Task<bool> HasAccessAsync(Guid userId, Guid courseId, CancellationToken ct = default)
    {
        // Matrícula (Enrollment) é criada em TODOS os caminhos de acesso vigentes: matrícula
        // direta em curso grátis (EnrollStudentCommandHandler), compra confirmada
        // (CoursePurchaseConfirmedEventHandler) e assinatura confirmada
        // (SubscriptionPaymentConfirmedEventHandler) — checar isso primeiro cobre os três com
        // uma única consulta.
        if (await enrollmentRepository.IsEnrolledAsync(userId, courseId, ct))
            return true;

        // Rede de segurança: reaproveita o MESMO ICourseAccessChecker já usado por
        // EnrollStudentCommandHandler (compra avulsa → assinatura por produto → assinatura
        // legada da plataforma) para cobrir o caso raro de um pagamento confirmado cujo
        // EventHandler de matrícula automática ainda não terminou de processar.
        return await paidAccessChecker.HasActivePaidAccessAsync(userId, courseId, ct);
    }
}
