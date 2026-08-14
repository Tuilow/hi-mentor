using MediatR;

namespace HiMentor.Sales.Application.Commands.ReprocessCoursePurchase;

/// <summary>
/// Reprocessamento manual (achado C2 da auditoria): re-publica o evento de confirmação de uma
/// compra avulsa JÁ confirmada, para o suporte destravar matrícula/e-mail/comissão quando o
/// processamento original falhou depois do commit (ver AppDbContext.DispatchDomainEventsAsync).
/// Não depende de reenvio de webhook da Asaas — isso não ajudaria, já que
/// CoursePurchase.ConfirmPayment é idempotente e não dispara o evento de novo para um pagamento
/// já confirmado.
/// </summary>
public sealed record ReprocessCoursePurchaseCommand(Guid CoursePurchaseId) : IRequest<ReprocessResult>;

/// <summary>Resultado explícito (sucesso/erro) devolvido na hora para quem chamou — diferente do
/// fluxo original via webhook, que é assíncrono e não dava nenhum retorno em caso de falha.</summary>
public sealed record ReprocessResult(bool Success, string Message);
