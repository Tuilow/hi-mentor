using Tuilow.SharedKernel.Application.Interfaces;
using Tuilow.Catalog.Domain.Interfaces;
using Tuilow.Learning.Application.Interfaces;
using Tuilow.Learning.Domain.Entities;
using Tuilow.Learning.Domain.Interfaces;
using Tuilow.Sales.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Tuilow.Learning.Application.EventHandlers;

/// <summary>
/// Reage à confirmação de uma compra avulsa de curso (evento publicado pelo módulo Sales) para
/// liberar o acesso automaticamente: cria a matrícula (Enrollment), emite um Magic Link (login
/// sem senha) e avisa o aluno por e-mail — e por WhatsApp, quando houver telefone e um provedor
/// configurado (ver IWhatsAppService). Antes disso, o pagamento ficava confirmado mas ninguém
/// avisava o comprador — ele só ganhava acesso se voltasse à plataforma e clicasse em
/// "Matricular-se" por conta própria. Mesmo padrão de Tuilow.Finance.Application.EventHandlers
/// (que credita a carteira do criador reagindo ao mesmo evento).
/// </summary>
public sealed class CoursePurchaseConfirmedEventHandler(
    ICourseRepository courseRepository,
    IEnrollmentRepository enrollmentRepository,
    IUserContactLookup userContactLookup,
    IMagicLinkIssuer magicLinkIssuer,
    IEmailService emailService,
    IWhatsAppService whatsAppService,
    IUnitOfWork uow,
    ILogger<CoursePurchaseConfirmedEventHandler> logger
) : INotificationHandler<CoursePurchaseConfirmedDomainEvent>
{
    public async Task Handle(CoursePurchaseConfirmedDomainEvent notification, CancellationToken ct)
    {
        var course = await courseRepository.GetByIdAsync(notification.CourseId, ct);
        if (course is null)
        {
            logger.LogWarning(
                "Compra {PurchaseId} confirmada para curso {CourseId} que não foi encontrado — matrícula não criada.",
                notification.CoursePurchaseId, notification.CourseId);
            return;
        }

        if (!await enrollmentRepository.IsEnrolledAsync(notification.StudentId, notification.CourseId, ct))
        {
            var enrollment = Enrollment.Create(notification.StudentId, notification.CourseId, course.Title);
            await enrollmentRepository.AddAsync(enrollment, ct);
            await uow.SaveChangesAsync(ct);
        }

        var contact = await userContactLookup.GetContactAsync(notification.StudentId, ct);
        if (contact is not null)
        {
            var magicLinkToken = await magicLinkIssuer.IssueAsync(notification.StudentId, ct);

            if (magicLinkToken is not null)
            {
                await emailService.SendMagicLinkAccessAsync(
                    contact.Email, contact.FirstName, course.Title, course.Slug.Value, magicLinkToken, ct);

                if (!string.IsNullOrWhiteSpace(contact.Phone))
                {
                    var magicLinkUrl = $"/acesso?token={magicLinkToken}&redirect=/cursos/{course.Slug.Value}";
                    // Best-effort: hoje é um no-op (sem provedor configurado) — nunca deve
                    // bloquear a liberação de acesso nem o e-mail, que já foi enviado acima.
                    await whatsAppService.SendCourseAccessGrantedAsync(
                        contact.Phone, contact.FirstName, course.Title, magicLinkUrl, ct);
                }
            }
            else
            {
                // Fallback raríssimo (usuário sumiu entre a checagem de contato e a emissão do
                // link) — ainda assim avisa por e-mail com o link comum, sem magic link.
                await emailService.SendCourseAccessGrantedAsync(contact.Email, contact.FirstName, course.Title, course.Slug.Value, ct);
            }
        }

        logger.LogInformation(
            "Acesso liberado automaticamente: compra {PurchaseId}, aluno {StudentId}, curso {CourseId}.",
            notification.CoursePurchaseId, notification.StudentId, notification.CourseId);
    }
}
