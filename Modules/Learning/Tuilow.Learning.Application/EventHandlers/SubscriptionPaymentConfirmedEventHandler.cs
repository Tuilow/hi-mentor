using Tuilow.SharedKernel.Application.Interfaces;
using Tuilow.Catalog.Domain.Interfaces;
using Tuilow.Channel.Domain.Interfaces;
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
/// cria a matrícula naquele curso, emite Magic Link (checkout anônimo também é suportado aqui —
/// ver Sales.Application.Commands.SubscribeToCourse) e leva o assinante para o Canal do criador
/// (/canal/{handle}, com os demais cursos do mesmo criador com cadeado), mesmo padrão do fluxo
/// de compra avulsa; se é o plano legado da plataforma (CourseId nulo, dá acesso a tudo), não há
/// um único curso pra matricular — o acesso continua resolvido dinamicamente por
/// SalesCourseAccessChecker, então aqui só avisamos o pagamento por e-mail.
/// </summary>
public sealed class SubscriptionPaymentConfirmedEventHandler(
    ISubscriptionRepository subscriptionRepository,
    ICourseRepository courseRepository,
    IEnrollmentRepository enrollmentRepository,
    IUserContactLookup userContactLookup,
    IMagicLinkIssuer magicLinkIssuer,
    IEmailService emailService,
    ICreatorChannelRepository creatorChannelRepository,
    INotificationLogRepository notificationLogRepository,
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
                await SendEmailAndLogAsync("PaymentConfirmed", contact.Email, notification, () =>
                    emailService.SendPaymentConfirmedAsync(contact.Email, contact.FirstName, notification.Amount, ct), ct);
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
            // SourceSubscriptionId correlaciona a matrícula com a assinatura que a originou
            // (achado M12 da auditoria).
            var enrollment = Enrollment.Create(
                notification.UserId, courseId, course.Title,
                sourceSubscriptionId: notification.SubscriptionId);
            await enrollmentRepository.AddAsync(enrollment, ct);
            await uow.SaveChangesAsync(ct);
        }

        if (contact is not null)
        {
            var magicLinkToken = await magicLinkIssuer.IssueAsync(notification.UserId, ct);

            if (magicLinkToken is not null)
            {
                var channel = await creatorChannelRepository.GetByCreatorIdAsync(course.InstructorId, ct);
                await SendEmailAndLogAsync("MagicLinkAccess", contact.Email, notification, () =>
                    emailService.SendMagicLinkAccessAsync(
                        contact.Email, contact.FirstName, course.Title, course.Slug.Value, magicLinkToken, ct,
                        channelHandle: channel?.Handle.Value), ct);
            }
            else
            {
                // Fallback raríssimo (usuário sumiu entre a checagem de contato e a emissão do
                // link) — ainda assim avisa por e-mail com o link comum, sem magic link.
                await SendEmailAndLogAsync("CourseAccessGranted", contact.Email, notification, () =>
                    emailService.SendCourseAccessGrantedAsync(contact.Email, contact.FirstName, course.Title, course.Slug.Value, ct), ct);
            }
        }

        logger.LogInformation(
            "Acesso liberado automaticamente via assinatura: assinante {UserId}, curso {CourseId}.",
            notification.UserId, courseId);
    }

    /// <summary>Mesma ideia de CoursePurchaseConfirmedEventHandler.SendEmailAndLogAsync — ver lá para o porquê.</summary>
    private async Task SendEmailAndLogAsync(
        string template, string recipient, PaymentConfirmedDomainEvent notification,
        Func<Task> send, CancellationToken ct)
    {
        string? error = null;
        try
        {
            await send();
        }
        catch (Exception ex)
        {
            error = ex.Message;
            logger.LogWarning(ex,
                "Falha ao enviar e-mail ({Template}) da assinatura {SubscriptionId} para {Recipient}.",
                template, notification.SubscriptionId, recipient);
        }

        await notificationLogRepository.AddAsync(
            NotificationLog.Record(
                "Email", template, recipient, notification.AsaasPaymentId,
                notification.SubscriptionId, error is null, error),
            ct);
        await uow.SaveChangesAsync(ct);
    }
}
