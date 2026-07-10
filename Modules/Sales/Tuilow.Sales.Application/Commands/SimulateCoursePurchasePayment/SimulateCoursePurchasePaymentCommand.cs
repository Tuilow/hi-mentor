using MediatR;

namespace Tuilow.Sales.Application.Commands.SimulateCoursePurchasePayment;

/// <summary>
/// Uso exclusivo de sandbox/desenvolvimento — ver <see cref="SimulateCoursePurchasePaymentCommandHandler"/>.
/// </summary>
public sealed record SimulateCoursePurchasePaymentCommand(Guid StudentId, Guid CoursePurchaseId) : IRequest;
