using MediatR;

namespace HiMentor.Sales.Application.Commands.SimulateCoursePurchasePayment;

/// <summary>
/// Uso exclusivo de sandbox/desenvolvimento — ver <see cref="SimulateCoursePurchasePaymentCommandHandler"/>.
/// StudentId é opcional: com o checkout anônimo, quem simula o pagamento em dev pode não estar
/// logado como o comprador (a conta foi criada automaticamente na compra) — quando ausente, a
/// checagem de dono é pulada (sem risco: este comando não existe fora de Development).
/// </summary>
public sealed record SimulateCoursePurchasePaymentCommand(Guid? StudentId, Guid CoursePurchaseId) : IRequest;
