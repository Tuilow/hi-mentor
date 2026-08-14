using HiMentor.Catalog.Domain.Interfaces;
using HiMentor.Channel.Domain.Interfaces;
using HiMentor.IdentidadeAcesso.Domain.Entities;
using HiMentor.IdentidadeAcesso.Domain.Interfaces;
using HiMentor.Learning.Domain.Enums;
using HiMentor.Learning.Domain.Interfaces;
using HiMentor.Sales.Domain.Enums;
using HiMentor.Sales.Domain.Interfaces;
using HiMentor.SharedKernel.Application.Exceptions;
using HiMentor.SharedKernel.Application.Interfaces;
using MediatR;

namespace HiMentor.IdentidadeAcesso.Application.Commands.ReissueCourseAccessLink;

public sealed class ReissueCourseAccessLinkCommandHandler(
    IUserRepository userRepository,
    ICourseRepository courseRepository,
    IEnrollmentRepository enrollmentRepository,
    ICoursePurchaseRepository coursePurchaseRepository,
    ICreatorChannelRepository creatorChannelRepository,
    IAdminCourseAccessAuditLogRepository auditLogRepository,
    IFrontendUrlProvider frontendUrlProvider,
    IUnitOfWork uow
) : IRequestHandler<ReissueCourseAccessLinkCommand, ReissueCourseAccessLinkResult>
{
    public async Task<ReissueCourseAccessLinkResult> Handle(ReissueCourseAccessLinkCommand request, CancellationToken ct)
    {
        var student = await userRepository.GetByIdAsync(request.StudentUserId, ct)
            ?? throw new NotFoundException("Usuario", request.StudentUserId);

        var course = await courseRepository.GetByIdAsync(request.CourseId, ct)
            ?? throw new NotFoundException("Curso", request.CourseId);

        var enrollment = await enrollmentRepository.GetByUserAndCourseAsync(request.StudentUserId, request.CourseId, ct);
        // EnrollmentStatus.Completed continua com acesso liberado normalmente -- so Cancelled de
        // fato revoga (ver mesmo ajuste em GetUserCoursesAndAccessQueryHandler.canReissueLink).
        if (enrollment is null || enrollment.Status == EnrollmentStatus.Cancelled)
            throw new BusinessException("Este usuario nao possui acesso liberado a este curso.");

        // Checagem extra de seguranca: hoje o reembolso de uma CoursePurchase (CoursePurchase.Refund)
        // nao cancela automaticamente o Enrollment correspondente -- so o modulo Finance reage a
        // CoursePurchaseRefundedDomainEvent (pra estornar a comissao do criador). Sem esta
        // checagem, o painel poderia reemitir um link de acesso valido pra uma compra ja
        // reembolsada/cancelada. Nao altera o comportamento do restante do sistema (o Enrollment
        // continua Active no banco), so bloqueia a reemissao pelo painel administrativo.
        if (enrollment.SourcePurchaseId is not null)
        {
            var purchases = await coursePurchaseRepository.GetByStudentAsync(request.StudentUserId, ct);
            var purchase = purchases.FirstOrDefault(p => p.Id == enrollment.SourcePurchaseId);
            if (purchase is not null && purchase.Status != CoursePurchaseStatus.Confirmed)
                throw new BusinessException(
                    $"A compra vinculada a este acesso nao esta confirmada (status atual: {purchase.Status}). Link nao gerado.");
        }

        // Mesma regra de destino usada pelo e-mail automatico de acesso liberado
        // (CoursePurchaseConfirmedEventHandler, modulo Learning): se o criador tem Canal publico,
        // o aluno entra direto em /canal/{handle} (vitrine com todos os cursos dele ja
        // destravados); senao vai direto para o curso comprado.
        var channel = await creatorChannelRepository.GetByCreatorIdAsync(course.InstructorId, ct);
        var redirectPath = channel is not null ? $"/canal/{channel.Handle.Value}" : $"/cursos/{course.Slug.Value}";

        // Mesma construcao de token usada em IdentidadeAcessoMagicLinkIssuer/ResendAccessLinkCommandHandler
        // (dois GUIDs opacos concatenados) -- reimplementada aqui pelo mesmo motivo de
        // ResendAccessLinkCommandHandler: IMagicLinkIssuer vive no modulo Learning, e
        // IdentidadeAcesso nao depende de Learning (dependencia estritamente na direcao
        // contraria em todo o resto do codigo).
        var token = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
        var magicLink = student.IssueMagicLink(token);
        await userRepository.AddMagicLinkTokenAsync(magicLink, ct);

        await auditLogRepository.AddAsync(
            AdminCourseAccessAuditLog.Record(request.AdminUserId, request.StudentUserId, request.CourseId, "ReissueAccessLink"),
            ct);

        await uow.SaveChangesAsync(ct);

        var url = frontendUrlProvider.BuildUrl($"/acesso?token={token}&redirect={Uri.EscapeDataString(redirectPath)}");
        return new ReissueCourseAccessLinkResult(url);
    }
}
