using Tuilow.SharedKernel.Application.Interfaces;
using Tuilow.Catalog.Domain.Interfaces;
using Tuilow.Channel.Domain.Interfaces;
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
///
/// Se o criador do curso tem um Canal público (Modules/Channel — vitrine com todos os cursos
/// dele), o comprador entra direto em /canal/{handle} em vez de só no curso comprado: assim,
/// caso o criador tenha outros vídeos/cursos, eles aparecem com cadeado (destravam com nova
/// compra) e o comprado já vem destravado — ver ICreatorChannelRepository e
/// GetPublicChannelQueryHandler (Channel.Application), que já calcula esse "IsUnlocked".
/// </summary>
public sealed class CoursePurchaseConfirmedEventHandler(
    ICourseRepository courseRepository,
    IEnrollmentRepository enrollmentRepository,
    IUserContactLookup userContactLookup,
    IMagicLinkIssuer magicLinkIssuer,
    IEmailService emailService,
    IWhatsAppService whatsAppService,
    ICreatorChannelRepository creatorChannelRepository,
    INotificationLogRepository notificationLogRepository,
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
            // SourcePurchaseId correlaciona a matrícula com a compra que a originou (achado M12
            // da auditoria) — permite ao suporte ir direto de Enrollment até CoursePurchase/AsaasPaymentId.
            var enrollment = Enrollment.Create(
                notification.StudentId, notification.CourseId, course.Title,
                sourcePurchaseId: notification.CoursePurchaseId);
            await enrollmentRepository.AddAsync(enrollment, ct);
            await uow.SaveChangesAsync(ct);
        }

        var channel = await creatorChannelRepository.GetByCreatorIdAsync(course.InstructorId, ct);

        var contact = await userContactLookup.GetContactAsync(notification.StudentId, ct);
        if (contact is not null)
        {
            var magicLinkToken = await magicLinkIssuer.IssueAsync(notification.StudentId, ct);

            if (magicLinkToken is not null)
            {
                await SendEmailAndLogAsync("MagicLinkAccess", contact.Email, notification, () =>
                    emailService.SendMagicLinkAccessAsync(
                        contact.Email, contact.FirstName, course.Title, course.Slug.Value, magicLinkToken, ct,
                        channelHandle: channel?.Handle.Value), ct);

                if (!string.IsNullOrWhiteSpace(contact.Phone))
                {
                    var redirectPath = channel is not null ? $"/canal/{channel.Handle.Value}" : $"/cursos/{course.Slug.Value}";
                    var magicLinkUrl = $"/acesso?token={magicLinkToken}&redirect={redirectPath}";
                    // Best-effort: hoje é um no-op (sem provedor configurado) — nunca deve
                    // bloquear a liberação de acesso nem o e-mail, que já foi enviado acima. Não
                    // registra em NotificationLog (o log cobre só os canais realmente em uso).
                    await whatsAppService.SendCourseAccessGrantedAsync(
                        contact.Phone, contact.FirstName, course.Title, magicLinkUrl, ct);
                }
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
            "Acesso liberado automaticamente: compra {PurchaseId}, aluno {StudentId}, curso {CourseId}.",
            notification.CoursePurchaseId, notification.StudentId, notification.CourseId);
    }

    /// <summary>
    /// Registra em NotificationLog (achado M12 da auditoria) toda tentativa de e-mail de acesso
    /// liberado, sucesso ou falha, correlacionada pelo mesmo AsaasPaymentId da compra — sem isso,
    /// a única evidência de "o e-mail foi enviado?" era uma linha de texto no ILogger, não
    /// pesquisável. Falha de e-mail é best-effort (não deve derrubar o handler nem impedir os
    /// outros efeitos colaterais já aplicados) — por isso não relança.
    /// </summary>
    private async Task SendEmailAndLogAsync(
        string template, string recipient, CoursePurchaseConfirmedDomainEvent notification,
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
                "Falha ao enviar e-mail ({Template}) da compra {PurchaseId} para {Recipient}.",
                template, notification.CoursePurchaseId, recipient);
        }

        await notificationLogRepository.AddAsync(
            NotificationLog.Record(
                "Email", template, recipient, notification.AsaasPaymentId,
                notification.CoursePurchaseId, error is null, error),
            ct);
        await uow.SaveChangesAsync(ct);
    }
}
