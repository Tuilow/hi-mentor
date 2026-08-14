using HiMentor.Catalog.Domain.Interfaces;
using HiMentor.IdentidadeAcesso.Domain.Interfaces;
using HiMentor.Learning.Domain.Entities;
using HiMentor.Learning.Domain.Enums;
using HiMentor.Learning.Domain.Interfaces;
using HiMentor.Sales.Domain.Enums;
using HiMentor.Sales.Domain.Interfaces;
using MediatR;

namespace HiMentor.IdentidadeAcesso.Application.Queries.GetUserCoursesAndAccess;

public sealed class GetUserCoursesAndAccessQueryHandler(
    IUserRepository userRepository,
    ICourseRepository courseRepository,
    IEnrollmentRepository enrollmentRepository,
    ICoursePurchaseRepository coursePurchaseRepository,
    INotificationLogRepository notificationLogRepository
) : IRequestHandler<GetUserCoursesAndAccessQuery, IReadOnlyList<UserCourseAccessResponse>>
{
    public async Task<IReadOnlyList<UserCourseAccessResponse>> Handle(GetUserCoursesAndAccessQuery request, CancellationToken ct)
    {
        var enrollments = (await enrollmentRepository.GetByUserAsync(request.UserId, ct)).ToList();
        var purchases = (await coursePurchaseRepository.GetByStudentAsync(request.UserId, ct)).ToList();

        var courseIds = enrollments.Select(e => e.CourseId)
            .Union(purchases.Select(p => p.CourseId))
            .Distinct()
            .ToList();

        if (courseIds.Count == 0)
            return [];

        var courses = (await courseRepository.GetByIdsAsync(courseIds, ct)).ToDictionary(c => c.Id);

        var purchaseIds = purchases.Select(p => p.Id).ToList();
        IReadOnlyList<NotificationLog> notificationLogs = purchaseIds.Count > 0
            ? await notificationLogRepository.GetByCorrelationIdsAsync(purchaseIds, ct)
            : Array.Empty<NotificationLog>();
        var latestEmailByCorrelation = notificationLogs
            .Where(n => n.Channel == "Email" && n.CorrelationId.HasValue)
            .GroupBy(n => n.CorrelationId!.Value)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(n => n.CreatedAt).First());

        // Instrutor: cache por criador ja visto pra nao repetir a mesma consulta de usuario.
        var instructorNames = new Dictionary<Guid, string?>();
        async Task<string?> GetInstructorNameAsync(Guid instructorId)
        {
            if (instructorNames.TryGetValue(instructorId, out var cached)) return cached;
            var instructor = await userRepository.GetByIdAsync(instructorId, ct);
            var name = instructor is null ? null : $"{instructor.Profile.FirstName} {instructor.Profile.LastName}".Trim();
            instructorNames[instructorId] = name;
            return name;
        }

        // Compra mais recente por curso (um aluno pode ter mais de uma tentativa de compra do
        // mesmo curso -- ex.: uma Failed seguida de uma Confirmed).
        var purchaseByCourse = purchases
            .GroupBy(p => p.CourseId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(p => p.CreatedAt).First());

        var results = new List<UserCourseAccessResponse>();
        foreach (var courseId in courseIds)
        {
            if (!courses.TryGetValue(courseId, out var course)) continue; // curso excluido -- nao quebra a listagem

            var enrollment = enrollments.FirstOrDefault(e => e.CourseId == courseId);
            purchaseByCourse.TryGetValue(courseId, out var purchase);

            var instructorName = await GetInstructorNameAsync(course.InstructorId);

            DateTime? emailSentAt = null;
            bool? emailSuccess = null;
            if (purchase is not null && latestEmailByCorrelation.TryGetValue(purchase.Id, out var log))
            {
                emailSentAt = log.CreatedAt;
                emailSuccess = log.Success;
            }

            // Curso concluido (EnrollmentStatus.Completed) continua com acesso liberado normalmente --
            // so Cancelled de fato revoga. Antes exigia == Active, entao um aluno que ja tivesse
            // terminado o curso (ex.: comprou e assistiu tudo no mesmo dia) aparecia como
            // "Link indisponivel" no painel mesmo com pagamento confirmado e acesso valido.
            var canReissueLink = enrollment is not null
                && enrollment.Status != EnrollmentStatus.Cancelled
                && (purchase is null || purchase.Status == CoursePurchaseStatus.Confirmed);

            results.Add(new UserCourseAccessResponse(
                course.Id, course.Title, course.Slug.Value, instructorName,
                purchase?.Id, purchase?.Status.ToString(), purchase?.Amount.Amount,
                purchase?.CreatedAt, purchase?.ConfirmedAt, purchase?.RefundedAt,
                enrollment?.SourceSubscriptionId is not null,
                enrollment?.Id, enrollment?.Status.ToString(), enrollment?.EnrolledAt, enrollment?.CompletedAt,
                emailSentAt, emailSuccess,
                canReissueLink
            ));
        }

        return results
            .OrderByDescending(r => r.PurchaseCreatedAt ?? r.EnrolledAt ?? DateTime.MinValue)
            .ToList();
    }
}
