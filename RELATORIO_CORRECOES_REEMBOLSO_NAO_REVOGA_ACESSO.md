# Relatório de Correção — Reembolso via Asaas não revogava o acesso do aluno

**Data:** 12/08/2026
**Origem:** usuário reportou um estorno real (webhook Asaas `PAYMENT_REFUNDED`, `payment.id = pay_ef2w4d6tp0x8osuh`, compra "Curso: Aula de piano: Ensinar a tocar piano", R$ 5,00) que não aparecia refletido na plataforma, com a expectativa de que o curso fosse cancelado para o aluno.

## Diagnóstico

O webhook em si estava correto: `AsaasWebhookController` → `ProcessAsaasWebhookCommandHandler.HandleCoursePurchasePaymentAsync` já tratava `PAYMENT_REFUNDED` chamando `CoursePurchase.Refund()`, que marca `Status = Refunded`, `RefundedAt`, e dispara `CoursePurchaseRefundedDomainEvent`. Esse evento já era consumido pelo módulo Finance (`CoursePurchaseRefundedEventHandler`), que estorna corretamente o valor líquido da carteira interna do criador (modelo Legacy).

O problema: **nenhum handler cancelava a matrícula (`Enrollment`) do aluno no módulo Learning**. `Enrollment.Cancel()` e `EnrollmentStatus.Cancelled` já existiam na entidade, mas não havia nenhum caminho de código que os chamasse a partir de um reembolso — diferente do fluxo de confirmação de pagamento, que tem `CoursePurchaseConfirmedEventHandler` (Learning) criando a matrícula automaticamente.

Indo mais fundo: mesmo cancelando a matrícula, isso **não teria efeito nenhum** sobre o acesso, porque `EnrollmentRepository.IsEnrolledAsync` — usada tanto para decidir acesso em `LearningCourseAccessService.HasAccessAsync` quanto para bloquear matrícula duplicada — ignorava completamente o `Status`: bastava existir uma linha de `Enrollment` (`Cancelled` ou não) para contar como acesso válido. Ou seja, o gap tinha duas causas empilhadas.

Confirmado também que o valor da compra (`GetMyCoursePurchasesQueryHandler`, endpoint `GET /api/v1/course-purchases/me`) já expõe `Status.ToString()` corretamente — então o status "Refunded" da compra em si é visível via API; o que faltava era o efeito colateral de acesso (a tela "Minha Jornada" / `meus-cursos` continuar mostrando o curso, e a aula continuar reproduzindo normalmente).

## Correção aplicada

**4 arquivos** (3 editados, 1 novo):

1. `Modules/Sales/Tuilow.Sales.Domain/Events/CoursePurchaseRefundedDomainEvent.cs` — adicionados `StudentId` e `CourseId` ao evento (mesmo padrão de `CoursePurchaseConfirmedDomainEvent`), necessários para o novo handler de Learning localizar a matrícula.
2. `Modules/Sales/Tuilow.Sales.Domain/Entities/CoursePurchase.cs` — `Refund()` agora repassa `StudentId`/`CourseId` ao construir o evento.
3. `Modules/Learning/Tuilow.Learning.Infrastructure/Repositories/EnrollmentRepository.cs` — `IsEnrolledAsync` passou a filtrar `Status != Cancelled` (sem isso, cancelar o `Enrollment` não revogava acesso na prática); `GetByUserAndCourseAsync` passou a ordenar por `EnrolledAt desc` (proteção para o caso de recompra após reembolso, que agora pode gerar uma segunda linha de `Enrollment`).
4. **Novo:** `Modules/Learning/Tuilow.Learning.Application/EventHandlers/CoursePurchaseRefundedEventHandler.cs` — consome `CoursePurchaseRefundedDomainEvent`, localiza a matrícula do aluno no curso e chama `Enrollment.Cancel()`. Idempotente (ignora se já estava `Cancelled`, cobre reenvio de webhook).

## Ajuste adicional identificado, não aplicado (fora do escopo desta correção)

O mesmo problema de raiz existe no lado de **assinatura**: `Subscription.RefundPayment()` já revoga o período pago (`CurrentPeriodEnd = agora`) e dispara `SubscriptionCancelledDomainEvent`, mas não há handler no módulo Learning consumindo esse evento para cancelar a matrícula de um plano por produto (`Plan.CourseId` preenchido) — e, pelo mesmo motivo do achado acima (`IsEnrolledAsync` ignorando `Status`, já corrigido), o aluno manteria acesso via `Enrollment` mesmo com a assinatura cancelada/reembolsada. Não foi tocado agora porque o caso reportado foi especificamente uma compra avulsa (`CoursePurchase`), não assinatura — mas recomendo tratar como próximo achado.

## Validação executada

`dotnet build` não é executável neste sandbox (bloqueio de NuGet já conhecido — ver notas de sessões anteriores). Validação feita por:
- Balanceamento de chaves/parênteses nos 4 arquivos (todos batendo).
- Revisão manual cruzando as assinaturas: `CoursePurchase.Refund()` → `CoursePurchaseRefundedDomainEvent` → `CoursePurchaseRefundedEventHandler` (Learning) e `CoursePurchaseRefundedEventHandler` (Finance, inalterado) — ambos implementam `INotificationHandler<CoursePurchaseRefundedDomainEvent>` em módulos/namespaces diferentes, MediatR despacha para os dois (mesmo padrão já usado por `CoursePurchaseConfirmedDomainEvent`, que também tem handlers em Learning e Finance).
- `IEnrollmentRepository`/`IUnitOfWork` já eram dependências existentes, sem mudança de interface.
- Não há migration necessária (nenhuma mudança de schema — `EnrollmentStatus.Cancelled` e `Enrollment.Cancel()` já existiam).
- Os 4 arquivos entregues ao device foram confirmados **byte-idênticos via SHA-256** entre o container e o disco do usuário.

**Gap conhecido:** não existe projeto de testes automatizados cobrindo `Modules/Sales` nem `Modules/Learning` neste solution (mesma lacuna pré-existente já registrada em sessões anteriores) — sem cobertura automatizada, recomendo testar manualmente em Development: `POST /api/v1/course-purchases/{id}/simulate-payment` para confirmar uma compra, depois forçar um `PAYMENT_REFUNDED` (ou reprocessar via um teste do endpoint de webhook) e conferir que o curso some de "Minha Jornada" e a aula passa a bloquear.

## Conclusão

Corrigido: um reembolso via Asaas agora cancela a matrícula do aluno (ela some de "Minha Jornada"/`meus-cursos` e o acesso à aula é bloqueado), além de já estornar a carteira do criador como antes. Identificado e documentado, mas não corrigido agora, o mesmo gap no fluxo de assinatura por produto.
