using HiMentor.SharedKernel.Application.Interfaces;
using HiMentor.Learning.Domain.Enums;
using HiMentor.Learning.Domain.Interfaces;
using HiMentor.Sales.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HiMentor.Learning.Application.EventHandlers;

/// <summary>
/// Reage ao reembolso de uma compra avulsa de curso (evento publicado pelo módulo Sales)
/// revogando o acesso do aluno: cancela a matrícula (Enrollment) criada por
/// <see cref="CoursePurchaseConfirmedEventHandler"/>. Mesmo padrão de
/// HiMentor.Finance.Application.EventHandlers.CoursePurchaseRefundedEventHandler (que reage ao
/// mesmo evento para estornar a carteira do criador) -- este aqui cuida do lado Learning.
///
/// Achado 12/08/2026: até esta correção, um reembolso via Asaas (evento PAYMENT_REFUNDED)
/// marcava a CoursePurchase como Refunded e estornava a carteira do criador, mas NADA revogava
/// o acesso do aluno -- Enrollment.Cancel() existia na entidade mas nunca era chamado, e a tela
/// "Minha Jornada" continuava mostrando o curso normalmente. Também exigiu corrigir
/// EnrollmentRepository.IsEnrolledAsync, que ignorava o Status (uma matrícula Cancelled ainda
/// contava como acesso válido) -- ver comentário lá.
/// </summary>
public sealed class CoursePurchaseRefundedEventHandler(
    IEnrollmentRepository enrollmentRepository,
    IUnitOfWork uow,
    ILogger<CoursePurchaseRefundedEventHandler> logger
) : INotificationHandler<CoursePurchaseRefundedDomainEvent>
{
    public async Task Handle(CoursePurchaseRefundedDomainEvent notification, CancellationToken ct)
    {
        var enrollment = await enrollmentRepository.GetByUserAndCourseAsync(notification.StudentId, notification.CourseId, ct);
        if (enrollment is null)
        {
            logger.LogInformation(
                "Reembolso da compra {PurchaseId} ignorado: aluno {StudentId} não tem matrícula no curso {CourseId}.",
                notification.CoursePurchaseId, notification.StudentId, notification.CourseId);
            return;
        }

        // Idempotência: um evento de reembolso reentregue (retry de webhook) não deve falhar nem
        // logar como se tivesse revogado o acesso de novo.
        if (enrollment.Status == EnrollmentStatus.Cancelled)
        {
            logger.LogInformation(
                "Reembolso da compra {PurchaseId} ignorado: matrícula {EnrollmentId} já estava cancelada.",
                notification.CoursePurchaseId, enrollment.Id);
            return;
        }

        enrollment.Cancel();
        enrollmentRepository.Update(enrollment);
        await uow.SaveChangesAsync(ct);

        logger.LogInformation(
            "Acesso revogado: compra {PurchaseId} reembolsada, matrícula {EnrollmentId} do aluno {StudentId} cancelada no curso {CourseId}.",
            notification.CoursePurchaseId, enrollment.Id, notification.StudentId, notification.CourseId);
    }
}
