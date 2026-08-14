using MediatR;

namespace HiMentor.CreatorStudio.Application.Queries.GetMyProducts;

/// <summary>Tela "Meus Produtos" — hub central do criador.</summary>
public sealed record GetMyProductsQuery(Guid InstructorId) : IRequest<IEnumerable<ProductListItemResponse>>;

public sealed record ProductListItemResponse(
    Guid Id,
    string Name,
    string Slug,
    string? Category,
    string ProductType,
    string Status,
    DateTime CreatedAt,
    int TotalSales,
    decimal RevenueGenerated
);
