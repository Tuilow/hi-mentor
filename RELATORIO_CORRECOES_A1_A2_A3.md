# Relatório de Validação - Correções A1, A2 e A3

**Data:** 29/07/2026  
**Status:** Validado e ajustado  

## A1 - Idempotência de webhook em Subscription

Validado em:

- `Modules/Sales/Tuilow.Sales.Domain/Entities/Subscription.cs`
- `Modules/Sales/Tuilow.Sales.Domain/Entities/SubscriptionPayment.cs`
- `Modules/Sales/Tuilow.Sales.Application/Commands/ProcessWebhook/ProcessAsaasWebhookCommandHandler.cs`

`Subscription.ConfirmPayment` agora retorna `null` quando o pagamento já existe e está `Confirmed`. Isso impede reinício do período pago e evita disparo duplicado de `PaymentConfirmedDomainEvent` em retentativas do webhook da Asaas.

`SubscriptionPayment.Confirm` também ficou idempotente, retornando imediatamente quando o status já é `Confirmed`.

## A2 - Cancelamento preserva acesso até o fim do período pago

Validado em:

- `Modules/Sales/Tuilow.Sales.Domain/Entities/Subscription.cs`
- `Modules/Sales/Tuilow.Sales.Infrastructure/Repositories/SubscriptionRepository.cs`
- `Modules/Learning/Tuilow.Learning.Infrastructure/Services/SalesCourseAccessChecker.cs`

`Subscription.IsActive` agora considera assinatura `Cancelled` como ativa enquanto `CurrentPeriodEnd > DateTime.UtcNow`.

As consultas `GetActiveByUserAsync` e `GetActiveByUserForCourseAsync` também retornam assinaturas canceladas dentro do período pago, permitindo que o controle de acesso enxergue esse estado.

### Ajuste adicional aplicado

Como essas consultas passaram a devolver assinaturas `Cancelled` ainda válidas, `CancelSubscriptionCommandHandler` poderia tentar cancelar novamente uma assinatura já cancelada e chamar o Asaas outra vez.

Foi aplicado um no-op quando a assinatura já está `Cancelled`, antes da chamada ao provedor. `Subscription.Cancel` também foi tornado idempotente para preservar o primeiro `CancelledAt` e evitar evento duplicado.

## A3 - Confirmação de e-mail exige posse real

Validado em:

- `Modules/IdentidadeAcesso/Tuilow.IdentidadeAcesso.Domain/Entities/User.cs`
- `Modules/IdentidadeAcesso/Tuilow.IdentidadeAcesso.Application/Commands/ConsumeMagicLink/ConsumeMagicLinkCommandHandler.cs`
- `Modules/IdentidadeAcesso/Tuilow.IdentidadeAcesso.Application/Commands/LoginUser/LoginUserCommandHandler.cs`

`User.RegisterFromPurchase` não ativa mais a conta criada no checkout anônimo. A conta nasce como `PendingConfirmation`, sem `EmailConfirmedAt`.

`User.ConsumeMagicLink` ativa a conta e preenche `EmailConfirmedAt` quando um Magic Link válido é consumido. Assim, a posse real do e-mail é confirmada pelo acesso ao link recebido.

## Validação executada

Comando:

```text
dotnet build Tuilow.sln
```

Resultado:

```text
Compilação com êxito.
0 Aviso(s)
0 Erro(s)
```

Não foram encontrados projetos `.csproj` dentro de `tests`, portanto não havia suíte de testes automatizados local para executar além do build da solução.

## Conclusão

As correções A1, A2 e A3 estão gravadas e compilando. O único problema encontrado durante a validação foi o risco de cancelamento duplicado após A2, já corrigido no handler e no agregado.
