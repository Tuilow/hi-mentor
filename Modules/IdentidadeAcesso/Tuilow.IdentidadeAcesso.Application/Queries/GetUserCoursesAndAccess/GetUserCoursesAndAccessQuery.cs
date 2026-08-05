using MediatR;

namespace Tuilow.IdentidadeAcesso.Application.Queries.GetUserCoursesAndAccess;

/// <summary>
/// "Cursos e acessos" no detalhe do usuario (painel do dono da plataforma) -- cruza Enrollment
/// (Learning, fonte de verdade do acesso liberado), CoursePurchase (Sales, fonte de verdade do
/// pagamento) e Course (Catalog), para o suporte conseguir localizar rapidamente o acesso de um
/// aluno sem precisar consultar banco diretamente. Mesmo padrao de acoplamento entre modulos ja
/// usado por GetPlatformStatsQueryHandler (ICourseRepository/IVideoRepository direto).
///
/// Nao retorna nenhum link/token pronto -- so metadados (ver <see cref="UserCourseAccessResponse.CanReissueLink"/>).
/// O link em si so e gerado sob demanda por ReissueCourseAccessLinkCommand, para nunca expor um
/// token permanente pelo painel.
/// </summary>
public sealed record GetUserCoursesAndAccessQuery(Guid UserId) : IRequest<IReadOnlyList<UserCourseAccessResponse>>;

public sealed record UserCourseAccessResponse(
    Guid CourseId,
    string CourseTitle,
    string CourseSlug,
    string? InstructorName,
    Guid? PurchaseId,
    string? PurchaseStatus,
    decimal? PurchaseAmount,
    DateTime? PurchaseCreatedAt,
    DateTime? PurchaseConfirmedAt,
    DateTime? PurchaseRefundedAt,
    bool ViaSubscription,
    Guid? EnrollmentId,
    string? EnrollmentStatus,
    DateTime? EnrolledAt,
    DateTime? CompletedAt,
    DateTime? EmailSentAt,
    bool? EmailSuccess,
    bool CanReissueLink
);
