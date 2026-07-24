using Tuilow.SharedKernel.Application.Exceptions;
using Tuilow.Catalog.Application.Queries.GetCourseBySlug;
using Tuilow.Catalog.Domain.Interfaces;
using MediatR;

namespace Tuilow.Catalog.Application.Queries.GetCourseByIdAdmin;

public sealed class GetCourseByIdAdminQueryHandler(ICourseRepository courseRepository)
    : IRequestHandler<GetCourseByIdAdminQuery, CourseDetailResponse>
{
    public async Task<CourseDetailResponse> Handle(GetCourseByIdAdminQuery request, CancellationToken ct)
    {
        var course = await courseRepository.GetByIdAsync(request.CourseId, ct)
            ?? throw new NotFoundException("Curso", request.CourseId);

        // Mesmo padrão de AddModuleCommandHandler/PublishCourseCommandHandler etc. — sem essa
        // checagem, qualquer Creator autenticado conseguia abrir o curso de outro só sabendo o
        // Id (IDOR), mesmo sem aparecer na própria listagem "Gerenciar Cursos".
        if (course.InstructorId != request.InstructorId)
            throw new ForbiddenException("Apenas o criador pode acessar este curso.");

        var modules = course.Modules
            .OrderBy(m => m.Order)
            .Select(m => new ModuleResponse(
                m.Id, m.Title, m.Description, m.Order,
                m.Lessons.OrderBy(l => l.Order).Select(l => new LessonResponse(
                    l.Id, l.Title, l.Description, l.Order,
                    l.DurationSeconds, l.IsPreview, l.VideoId.HasValue))));

        var faqItems = course.FaqItems
            .OrderBy(f => f.Order)
            .Select(f => new FaqItemResponse(f.Id, f.Question, f.Answer, f.Order));

        var testimonials = course.Testimonials
            .Select(t => new TestimonialResponse(t.AuthorName, t.AuthorRole, t.Quote, t.AvatarUrl));

        return new CourseDetailResponse(
            course.Id, course.Title, course.Slug.Value, course.Description,
            course.ShortDescription, course.ThumbnailUrl, course.Price.Amount, course.IsFree,
            course.Level.ToString(), course.TotalDurationMinutes, course.PublishedAt, modules,
            course.Status.ToString(), course.Category, course.Subcategory, course.ProductType.ToString(),
            course.ViewCount, course.SalesPageHeadline, course.SalesPageSubheadline, course.SalesPageCtaText,
            course.SalesPageBenefits, faqItems,
            // Tela de edição do próprio criador — não precisa de nome/avatar/bio (ele já sabe
            // quem é), só InstructorId (já é propriedade direta de Course, sem exigir o
            // ICreatorProfileLookup/IInstructorLookup usado na página de vendas pública).
            course.InstructorId, null, null, null,
            course.SalesPageVideoUrl, testimonials, course.GuaranteeDays, course.GuaranteeText);
    }
}
