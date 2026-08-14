using MediatR;

namespace HiMentor.CreatorStudio.Application.Queries.GetProductDashboard;

/// <summary>Dashboard pós-publicação do produto (Views/Leads/Alunos/Vendas/Receita/Comissão).</summary>
public sealed record GetProductDashboardQuery(Guid CourseId, Guid InstructorId) : IRequest<ProductDashboardResponse>;

public sealed record ProductDashboardResponse(
    Guid CourseId,
    string ProductName,
    string Slug,
    int Views,
    int Leads,
    int Students,
    int Sales,
    decimal Revenue,
    decimal PlatformFee,
    decimal NetRevenue,
    decimal PlatformFeePercentage
);
