using Tuilow.SharedKernel.Application.Interfaces;
using Tuilow.Catalog.Domain.Interfaces;
using Tuilow.Learning.Application.Interfaces;
using Tuilow.Learning.Domain.Entities;
using Tuilow.Learning.Domain.Interfaces;
using Tuilow.Sales.Domain.Events;
using Tuilow.Sales.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Tuilow.Learning.Application.EventHandlers;

/// <summary>
/// Reage à confirmação de um pagamento de assinatura (evento publicado pelo módulo Sales) para
/// liberar o acesso automaticamente — mesmo objetivo de
/// <see cref="CoursePurchaseConfirmedEventHandler"/>, mas para o modelo "Assinatura" (passo
/// Preço do assistente): se o plano é de um produto específico (Plan.CourseId preenchido),
/// cria a matrícula naquele curso; se é o plano legado da plataforma (CourseId nulo, dá acesso
/// a tudo), não há um único curso pra matricular — o acesso continua resolvido dinamicamente
/// por SalesCourseAccessChecker, então aqui só avisamos o pagamento por e-mail.
/// </summary>
public sealed class SubscriptionPaymentConfirmedEventHandler(
    ISubscriptionRepository subscriptionRepository,
    ICourseRepository courseRepository,
    IEnrollmentRepository enrollmentRepository,
    IUserContactLookup userContactLookup,
    IEmailService emailService,
    IUnitOfWork uow,
    ILogger<SubscriptionPaymentConfirmedEventHandler> logger
) : INotificationHandler<PaymentConfirmedDomainEvent>
{
    public async Task Handle(PaymentConfirmedDomainEvent notification, CancellationToken ct)
    {
        var subscription = await subscriptionRepository.GetByIdAsync(notification.SubscriptionId, ct);
        if (subscription is null)
        {
            logger.LogWarning(
                "Pagamento {AsaasPaymentId} confirmado para assinatura {SubscriptionId} que não foi encontrada.",
                notification.AsaasPaymentId, notification.SubscriptionId);
            return;
        }

        var plan = await subscriptionRepository.GetPlanByIdAsync(subscription.PlanId, ct);
        var contact = await userContactLookup.GetContactAsync(notification.UserId, ct);

        if (plan?.CourseId is not { } courseId)
        {
            // Plano legado da plataforma (sem produto específico) — acesso já é dinâmico, sem
            // matrícula própria; só notifica o pagamento.
            if (contact is not null)
                await emailService.SendPaymentConfirmedAsync(contact.Email, contact.FirstName, notification.Amount, ct);
            return;
        }

        var course = await courseRepository.GetByIdAsync(courseId, ct);
        if (course is null)
        {
            logger.LogWarning(
                "Assinatura {SubscriptionId} confirmada para curso {CourseId} que não foi encontrado — matrícula não criada.",
                notification.SubscriptionId, courseId);
            return;
        }

        if (!await enrollmentRepository.IsEnrolledAsync(notification.UserId, courseId, ct))
        {
            var enrollment = Enrollment.Create(notification.UserId, courseId, course.Title);
            await enrollmentRepository.AddAsync(enrollment, ct);
            await uow.SaveChangesAsync(ct);
        }

        if (contact is not null)
            await emailService.SendCourseAccessGrantedAsync(contact.Email, contact.FirstName, course.Title, course.Slug.Value, ct);

        logger.LogInformation(
            "Acesso liberado automaticamente via assinatura: assinante {UserId}, curso {CourseId}.",
            notification.UserId, courseId);
    }
}
