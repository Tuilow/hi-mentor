using Tuilow.SharedKernel.Application.Exceptions;
using Tuilow.SharedKernel.Application.Interfaces;
using Tuilow.Catalog.Domain.Interfaces;
using Tuilow.Sales.Application.Interfaces;
using Tuilow.Sales.Domain.Interfaces;
using MediatR;
using CoursePurchaseEntity = Tuilow.Sales.Domain.Entities.CoursePurchase;

namespace Tuilow.Sales.Application.Commands.PurchaseCourse;

public sealed class PurchaseCourseCommandHandler(
    ICourseRepository courseRepository,
    ICoursePurchaseRepository coursePurchaseRepository,
    IPaymentService paymentService,
    IUnitOfWork uow
) : IRequestHandler<PurchaseCourseCommand, PurchaseCourseResponse>
{
    public async Task<PurchaseCourseResponse> Handle(PurchaseCourseCommand request, CancellationToken ct)
    {
        var course = await courseRepository.GetByIdAsync(request.CourseId, ct)
            ?? throw new NotFoundException("Curso", request.CourseId);

        if (course.IsFree)
            throw new BusinessException("Este curso é gratuito — não é necessário comprá-lo, basta se matricular.");

        if (await coursePurchaseRepository.HasConfirmedPurchaseAsync(request.StudentId, request.CourseId, ct))
            throw new BusinessException("Você já comprou este curso.");

        var customer = await paymentService.CreateOrGetCustomerAsync(
            new(request.CustomerName, request.CustomerEmail, request.CpfCnpj, request.Phone), ct);

        var charge = await paymentService.CreateChargeAsync(
            new(customer.Id, course.Price.Amount, $"Curso: {course.Title}", course.Id.ToString()), ct);

        var purchase = CoursePurchaseEntity.Create(
            request.StudentId, course.Id, course.InstructorId, course.Price.Amount,
            customer.Id, charge.Id);

        await coursePurchaseRepository.AddAsync(purchase, ct);
        await uow.SaveChangesAsync(ct);

        return new PurchaseCourseResponse(purchase.Id, charge.Id, charge.InvoiceUrl);
    }
}
