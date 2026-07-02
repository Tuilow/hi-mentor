using Tuilow.Sales.Domain.Interfaces;
using MediatR;

namespace Tuilow.Sales.Application.Queries.GetMyCoursePurchases;

public sealed class GetMyCoursePurchasesQueryHandler(ICoursePurchaseRepository repository)
    : IRequestHandler<GetMyCoursePurchasesQuery, IReadOnlyList<CoursePurchaseResponse>>
{
    public async Task<IReadOnlyList<CoursePurchaseResponse>> Handle(GetMyCoursePurchasesQuery request, CancellationToken ct)
    {
        var purchases = await repository.GetByStudentAsync(request.StudentId, ct);

        return purchases
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new CoursePurchaseResponse(
                p.Id, p.CourseId, p.Amount.Amount, p.Status.ToString(), p.ConfirmedAt, p.CreatedAt))
            .ToList();
    }
}
