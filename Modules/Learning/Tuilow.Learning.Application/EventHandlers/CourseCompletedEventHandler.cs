using Tuilow.SharedKernel.Application.Interfaces;
using Tuilow.Catalog.Domain.Interfaces;
using Tuilow.Learning.Application.Interfaces;
using Tuilow.Learning.Domain.Entities;
using Tuilow.Learning.Domain.Events;
using Tuilow.Learning.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Tuilow.Learning.Application.EventHandlers;

/// <summary>
/// Achado A4 da avaliação: a entidade Certificate existia no domínio (código no formato
/// TUI-{ano}-{hash}, tabela mapeada) mas estava completamente órfã — nada em todo o repositório
/// chamava Certificate.Issue(). O gatilho natural (conclusão do curso) já existia como domain
/// event (CourseCompletedDomainEvent, disparado por Enrollment.Complete()) mas ninguém o ouvia.
/// Este handler fecha esse ciclo: emite o certificado e avisa o aluno por e-mail com um link de
/// verificação pública (ver CertificatesController.Verify).
///
/// Escopo desta implementação (para não prometer mais do que existe): isto emite um registro
/// verificável — código, aluno, curso, data — consultável publicamente. NÃO gera um PDF
/// automaticamente (Certificate.PdfUrl fica null); ver relatório desta rodada para a análise de
/// custo/benefício de adicionar geração de PDF depois.
/// </summary>
public sealed class CourseCompletedEventHandler(
    ICourseRepository courseRepository,
    ICertificateRepository certificateRepository,
    IUserContactLookup userContactLookup,
    IEmailService emailService,
    IUnitOfWork uow,
    ILogger<CourseCompletedEventHandler> logger
) : INotificationHandler<CourseCompletedDomainEvent>
{
    public async Task Handle(CourseCompletedDomainEvent notification, CancellationToken ct)
    {
        // Idempotência: ver ICertificateRepository.ExistsForUserAndCourseAsync.
        if (await certificateRepository.ExistsForUserAndCourseAsync(notification.UserId, notification.CourseId, ct))
        {
            logger.LogInformation(
                "Certificado já emitido para usuário {UserId} no curso {CourseId} — ignorando reprocessamento.",
                notification.UserId, notification.CourseId);
            return;
        }

        var course = await courseRepository.GetByIdAsync(notification.CourseId, ct);
        if (course is null)
        {
            logger.LogWarning(
                "Curso {CourseId} concluído pelo usuário {UserId} não foi encontrado — certificado não emitido.",
                notification.CourseId, notification.UserId);
            return;
        }

        var certificate = Certificate.Issue(notification.UserId, notification.CourseId);
        await certificateRepository.AddAsync(certificate, ct);
        await uow.SaveChangesAsync(ct);

        logger.LogInformation(
            "Certificado {Code} emitido para usuário {UserId}, curso {CourseId} ({CourseTitle}).",
            certificate.Code, notification.UserId, notification.CourseId, course.Title);

        var contact = await userContactLookup.GetContactAsync(notification.UserId, ct);
        if (contact is null) return;

        try
        {
            await emailService.SendCertificateAsync(contact.Email, contact.FirstName, course.Title, certificate.Code, ct);
        }
        catch (Exception ex)
        {
            // Best-effort — o certificado já está emitido e consultável mesmo se o e-mail falhar;
            // não faz sentido reverter a emissão nem relançar (mesmo padrão dos outros handlers
            // de notificação desta base, ver Learning.EventHandlers.CoursePurchaseConfirmedEventHandler).
            logger.LogWarning(ex,
                "Falha ao enviar e-mail de certificado {Code} para {Email}.", certificate.Code, contact.Email);
        }
    }
}
