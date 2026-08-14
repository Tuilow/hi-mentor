using HiMentor.Catalog.Domain.Interfaces;
using HiMentor.Sales.Domain.Enums;
using HiMentor.Sales.Domain.Interfaces;
using MediatR;

namespace HiMentor.CreatorStudio.Application.Queries.GetMyProducts;

/// <summary>
/// Compoe dados de Catalog (produto) + Sales (vendas confirmadas) sem duplicar nenhuma das
/// duas regras - so le pelos repositorios ja existentes de cada modulo.
/// </summary>
public sealed class GetMyProductsQueryHandler(
    ICourseRepository courseRepository,
    ICoursePurchaseRepository coursePurchaseRepository
) : IRequestHandler<GetMyProductsQuery, IEnumerable<ProductListItemResponse>>
{
    public async Task<IEnumerable<ProductListItemResponse>> Handle(GetMyProductsQuery request, CancellationToken ct)
    {
        var courses = await courseRepository.ListByInstructorAsync(request.InstructorId, ct);
        var purchases = await coursePurchaseRepository.GetByCreatorAsync(request.InstructorId, null, null, ct);

        var salesByCourse = purchases
            .Where(p => p.Status == CoursePurchaseStatus.Confirmed)
            .GroupBy(p => p.CourseId)
            .ToDictionary(g => g.Key, g => (Count: g.Count(), Revenue: g.Sum(p => p.Amount.Amount)));

        return courses.Select(c =>
        {
            salesByCourse.TryGetValue(c.Id, out var stats);
            return new ProductListItemResponse(
                c.Id, c.Title, c.Slug.Value, c.Category, c.ProductType.ToString(), c.Status.ToString(),
                c.CreatedAt, stats.Count, stats.Revenue);
        });
    }
}
