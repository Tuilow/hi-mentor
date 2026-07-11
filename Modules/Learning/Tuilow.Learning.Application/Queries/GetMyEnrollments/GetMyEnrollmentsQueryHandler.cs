using Tuilow.Catalog.Domain.Interfaces;
using Tuilow.Learning.Domain.Enums;
using Tuilow.Learning.Domain.Interfaces;
using MediatR;

namespace Tuilow.Learning.Application.Queries.GetMyEnrollments;

/// <summary>
/// Acoplamento legítimo (mesmo padrão de <see cref="Commands.EnrollStudent.EnrollStudentCommandHandler"/>):
/// Learning.Application já referencia Tuilow.Catalog.Domain para conhecer o agregado Course.
/// Enrollment não guarda título/slug/preço do curso (só o CourseId), então a tela "meus cursos
/// matriculados" precisa buscar esses dados no Catalog — daqui, não o contrário, para não criar
/// uma referência nova de Catalog para Learning.
///
/// CompletedLessonsCount vem de Enrollment.LessonsProgress, já carregado via
/// EnrollmentRepository.GetByUserAsync (.Include(e => e.LessonsProgress)) — nenhuma consulta
/// nova, só contagem em memória do que já foi buscado. Alimenta o card "Aulas concluídas" do
/// dashboard, que antes ficava com valor fixo "—" (nunca tinha sido conectado a dado real).
/// </summary>
public sealed class GetMyEnrollmentsQueryHandler(
    IEnrollmentRepository enrollmentRepository,
    ICourseRepository courseRepository
) : IRequestHandler<GetMyEnrollmentsQuery, IEnumerable<MyEnrollmentResponse>>
{
    public async Task<IEnumerable<MyEnrollmentResponse>> Handle(GetMyEnrollmentsQuery request, CancellationToken ct)
    {
        var enrollments = (await enrollmentRepository.GetByUserAsync(request.UserId, ct))
            .Where(e => e.Status != EnrollmentStatus.Cancelled)
            .OrderByDescending(e => e.EnrolledAt)
            .ToList();

        if (enrollments.Count == 0)
            return [];

        var courses = (await courseRepository.GetByIdsAsync(enrollments.Select(e => e.CourseId), ct))
            .ToDictionary(c => c.Id);

        return enrollments
            .Where(e => courses.ContainsKey(e.CourseId)) // curso pode ter sido excluído — não quebra a listagem
            .Select(e =>
            {
                var course = courses[e.CourseId];
                return new MyEnrollmentResponse(
                    e.Id, e.CourseId, course.Title, course.Slug.Value, course.ThumbnailUrl,
                    course.Price.Amount, course.IsFree, course.Level.ToString(),
                    e.Status.ToString(), e.ProgressPercentage, e.EnrolledAt, e.CompletedAt,
                    e.LessonsProgress.Count(lp => lp.IsCompleted));
            });
    }
}
