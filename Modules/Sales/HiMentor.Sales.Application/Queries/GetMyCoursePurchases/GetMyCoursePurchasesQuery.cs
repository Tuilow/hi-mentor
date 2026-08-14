using MediatR;

namespace HiMentor.Sales.Application.Queries.GetMyCoursePurchases;

public sealed record GetMyCoursePurchasesQuery(Guid StudentId) : IRequest<IReadOnlyList<CoursePurchaseResponse>>;

public sealed record CoursePurchaseResponse(
    Guid Id, Guid CourseId, decimal Amount, string Status, DateTime? ConfirmedAt, DateTime CreatedAt
);
