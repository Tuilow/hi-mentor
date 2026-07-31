using Tuilow.SharedKernel.Application.Exceptions;
using Tuilow.SharedKernel.Application.Interfaces;
using Tuilow.Catalog.Domain.Interfaces;
using Tuilow.Learning.Application.Interfaces;
using Tuilow.Learning.Domain.Entities;
using Tuilow.Learning.Domain.Interfaces;
using Tuilow.Sales.Domain.Interfaces;
using MediatR;

namespace Tuilow.Learning.Application.Commands.EnrollFreeCourseAnonymous;

/// <summary>
/// Achado B2 da avaliação de UX: curso grátis exigia passar por /registro (nome, sobrenome,
/// e-mail, senha, confirmar senha) antes de matricular — mais fricção que o checkout anônimo do
/// curso pago (só nome/e-mail, sem senha, sem sair da página de vendas). Este handler espelha
/// exatamente o checkout anônimo do módulo Sales (ver PurchaseCourseCommandHandler): localiza ou
/// cria a conta pelo e-mail informado, sem senha, e o acesso chega por Magic Link — só que sem
/// nenhuma cobrança, já que o curso é gratuito.
/// </summary>
public sealed class EnrollFreeCourseAnonymousCommandHandler(
    ICourseRepository courseRepository,
    IEnrollmentRepository enrollmentRepository,
    ISubscriptionRepository subscriptionRepository,
    IUserProvisioningService userProvisioningService,
    IMagicLinkIssuer magicLinkIssuer,
    IEmailService emailService,
    IUnitOfWork uow
) : IRequestHandler<EnrollFreeCourseAnonymousCommand, EnrollFreeCourseAnonymousResponse>
{
    public async Task<EnrollFreeCourseAnonymousResponse> Handle(
        EnrollFreeCourseAnonymousCommand request, CancellationToken ct)
    {
        var course = await courseRepository.GetByIdAsync(request.CourseId, ct)
            ?? throw new NotFoundException("Curso", request.CourseId);

        // Mesma checagem de EnrollStudentCommandHandler: curso REALMENTE gratuito = Course.IsFree
        // E sem nenhum Plan de assinatura ativo (um curso em modo "Assinatura" grava Price = 0 por
        // design — ver Course.SetPrice). Sem isso, este endpoint anônimo viraria uma forma de
        // pular o pagamento de qualquer curso de assinatura só chamando /enrollments/free direto.
        var hasActiveSubscriptionPlan = (await subscriptionRepository.GetPlansByCourseAsync(request.CourseId, ct))
            .Any(p => p.IsActive);
        var isActuallyFree = course.IsFree && !hasActiveSubscriptionPlan;
        if (!isActuallyFree)
            throw new BusinessException("Este curso não é gratuito — é necessário comprá-lo ou assiná-lo.");

        var studentId = request.UserId
            ?? await userProvisioningService.FindOrCreateStudentAsync(request.CustomerEmail, request.CustomerName, ct);

        // Idempotente: um segundo clique/reenvio do formulário (ex.: duplo clique, aba
        // duplicada) não deve criar uma segunda matrícula nem falhar — só devolve a existente.
        var existingEnrollment = await enrollmentRepository.GetByUserAndCourseAsync(studentId, request.CourseId, ct);
        Guid enrollmentId;
        if (existingEnrollment is not null)
        {
            enrollmentId = existingEnrollment.Id;
        }
        else
        {
            var enrollment = Enrollment.Create(studentId, request.CourseId, course.Title);
            await enrollmentRepository.AddAsync(enrollment, ct);
            enrollmentId = enrollment.Id;
        }

        await uow.SaveChangesAsync(ct);

        // Só emite Magic Link quando o visitante NÃO estava logado — a conta pode ter acabado de
        // ser criada agora mesmo, sem senha, e o Magic Link é o único jeito de entrar. Quem já
        // estava logado (UserId preenchido) já tem uma sessão válida e o front-end redireciona
        // direto para o curso, sem precisar de e-mail.
        var magicLinkSent = false;
        if (request.UserId is null)
        {
            var magicLinkToken = await magicLinkIssuer.IssueAsync(studentId, ct);
            if (magicLinkToken is not null)
            {
                var firstName = request.CustomerName.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries)
                    .FirstOrDefault() ?? "Aluno";
                await emailService.SendMagicLinkAccessAsync(
                    request.CustomerEmail, firstName, course.Title, course.Slug.Value, magicLinkToken, ct);
                magicLinkSent = true;
            }
        }

        return new EnrollFreeCourseAnonymousResponse(enrollmentId, magicLinkSent);
    }
}
