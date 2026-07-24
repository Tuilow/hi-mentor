using Tuilow.SharedKernel.Application.Common;
using Tuilow.SharedKernel.Application.Exceptions;
using Tuilow.Catalog.Application.Interfaces;
using Tuilow.Catalog.Domain.Enums;
using Tuilow.Catalog.Domain.Interfaces;
using Tuilow.Sales.Domain.Interfaces;
using MediatR;

namespace Tuilow.Catalog.Application.Queries.GetCourseBySlug;

/// <summary>
/// Endpoint público (GET /courses/{slug} não exige autenticação) — é a mesma consulta usada
/// tanto pela página do curso autenticada (dashboard) quanto pela página de vendas pública
/// (/c/{slug}). InstructorName/AvatarUrl/Bio alimentam o bloco "Sobre o Professor" da página
/// de vendas; vêm de IInstructorLookup (IdentidadeAcesso) — nunca duplicados em Catalog.
/// </summary>
public sealed class GetCourseBySlugQueryHandler(
    ICourseRepository courseRepository,
    ISubscriptionRepository subscriptionRepository,
    IInstructorLookup instructorLookup
) : IRequestHandler<GetCourseBySlugQuery, CourseDetailResponse>
{
    public async Task<CourseDetailResponse> Handle(GetCourseBySlugQuery request, CancellationToken ct)
    {
        var course = await courseRepository.GetBySlugAsync(request.Slug, ct)
            ?? throw new NotFoundException("Curso", request.Slug);

        // Estado real de comercialização — ver CourseCommercializationResolver. Antes, o
        // front-end (/c/[slug]/page.tsx) buscava os planos de assinatura separadamente e
        // recalculava isso do zero para corrigir o bug "curso pago aparece como Grátis"; agora
        // o backend já devolve o estado pronto, uma única vez, para qualquer consumidor.
        var hasActivePlan = (await subscriptionRepository.GetPlansByCourseAsync(course.Id, ct))
            .Any(p => p.IsActive);
        var commercializationState = CourseCommercializationResolver.Resolve(
            course.Status == CourseStatus.Published, course.IsFree, hasActivePlan);

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

        var instructor = await instructorLookup.GetProfileAsync(course.InstructorId, ct);

        var testimonials = course.Testimonials
            .Select(t => new TestimonialResponse(t.AuthorName, t.AuthorRole, t.Quote, t.AvatarUrl));

        return new CourseDetailResponse(
            course.Id, course.Title, course.Slug.Value, course.Description,
            course.ShortDescription, course.ThumbnailUrl, course.Price.Amount, course.IsFree,
            course.Level.ToString(), course.TotalDurationMinutes, course.PublishedAt, modules,
            course.Status.ToString(), course.Category, course.Subcategory, course.ProductType.ToString(),
            course.ViewCount, course.SalesPageHeadline, course.SalesPageSubheadline, course.SalesPageCtaText,
            course.SalesPageBenefits, faqItems,
            course.InstructorId, instructor?.DisplayName, instructor?.AvatarUrl, instructor?.Bio,
            course.SalesPageVideoUrl, testimonials, course.GuaranteeDays, course.GuaranteeText,
            commercializationState.ToString());
    }
}
