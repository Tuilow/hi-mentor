using Tuilow.SharedKernel.Application.Exceptions;
using Tuilow.SharedKernel.Application.Interfaces;
using Tuilow.Catalog.Domain.Interfaces;
using Tuilow.Learning.Domain.Entities;
using Tuilow.Learning.Domain.Interfaces;
using MediatR;

namespace Tuilow.Learning.Application.Commands.RedeemAccessCode;

public sealed class RedeemAccessCodeCommandHandler(
    IAccessCodeRepository accessCodeRepository,
    ICourseRepository courseRepository,
    IEnrollmentRepository enrollmentRepository,
    IUnitOfWork uow
) : IRequestHandler<RedeemAccessCodeCommand, RedeemAccessCodeResult>
{
    public async Task<RedeemAccessCodeResult> Handle(RedeemAccessCodeCommand request, CancellationToken ct)
    {
        // BusinessException (não NotFoundException) pelas mensagens abaixo: o pedido original
        // especifica o texto exato exibido ao aluno em cada caso, diferente do formato genérico
        // de NotFoundException ("X com identificador 'Y' não encontrado").
        var normalizedCode = (request.Code ?? string.Empty).Trim().ToUpperInvariant();
        if (normalizedCode.Length == 0)
            throw new BusinessException("Informe o código de acesso.");

        var accessCode = await accessCodeRepository.GetByCodeAsync(normalizedCode, ct)
            ?? throw new BusinessException("Este código não é válido.");

        var course = await courseRepository.GetByIdAsync(accessCode.CourseId, ct)
            ?? throw new BusinessException("O programa vinculado a este código não está mais disponível.");

        // Já matriculado (ex.: comprou por outro caminho antes de ativar o código, ou reenviou o
        // formulário) — trata como sucesso idempotente em vez de erro. Precisa ser checado ANTES
        // de Enrollment.Create: o índice único (UserId, CourseId) em "enrollments" rejeitaria a
        // segunda tentativa com uma violação de constraint crua (não mapeada pelo
        // ExceptionHandlingMiddleware, viraria 500 em vez de uma mensagem amigável).
        var existingEnrollment = await enrollmentRepository.GetByUserAndCourseAsync(request.UserId, accessCode.CourseId, ct);
        if (existingEnrollment is not null)
            return new RedeemAccessCodeResult(existingEnrollment.Id, course.Id, course.Title, course.Slug.Value);

        // Lança InvalidOperationException (mesmo padrão de Course.Publish) quando o código está
        // inativo, expirado, já usado por este aluno, ou sem mais usos disponíveis — mensagens
        // exatas de cada caso ficam em AccessCode.Redeem; chegam ao aluno como vieram.
        var redemption = accessCode.Redeem(request.UserId);
        await accessCodeRepository.AddRedemptionAsync(redemption, ct);
        accessCodeRepository.Update(accessCode);

        var enrollment = Enrollment.Create(request.UserId, accessCode.CourseId, course.Title);
        await enrollmentRepository.AddAsync(enrollment, ct);

        await uow.SaveChangesAsync(ct);

        return new RedeemAccessCodeResult(enrollment.Id, course.Id, course.Title, course.Slug.Value);
    }
}
