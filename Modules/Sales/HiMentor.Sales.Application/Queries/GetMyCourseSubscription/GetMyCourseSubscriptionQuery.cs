using HiMentor.Sales.Application.Queries.GetUserSubscription;
using MediatR;

namespace HiMentor.Sales.Application.Queries.GetMyCourseSubscription;

/// <summary>
/// Assinatura do aluno para o plano de UM produto específico (modelo Kiwify: pagamento
/// amarrado ao curso, não a uma "assinatura da plataforma"). Reaproveita o mesmo shape de
/// resposta de GetUserSubscriptionQuery (a assinatura legada de plataforma) — são conceitos
/// distintos (ver ISubscriptionRepository.GetActiveByUserAsync vs GetActiveByUserForCourseAsync),
/// mas a tela só precisa mostrar "plano/preço/validade", então o mesmo DTO serve aos dois.
/// </summary>
public sealed record GetMyCourseSubscriptionQuery(Guid UserId, Guid CourseId) : IRequest<UserSubscriptionResponse?>;
