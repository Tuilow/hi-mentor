# Relatório de Validação - Correções C2 e M12

**Data:** 30/07/2026  
**Status:** Validado, ajustado e compilando

## C2 - Pagamento confirmado sem matrícula/e-mail/comissão

Validado em:

- `Host/Tuilow.Host.Api/Data/AppDbContext.cs`
- `Modules/Sales/Tuilow.Sales.Api/Controllers/AdminSalesController.cs`
- `Modules/Sales/Tuilow.Sales.Application/Commands/ReprocessCoursePurchase`
- `Modules/Sales/Tuilow.Sales.Application/Commands/ReprocessSubscriptionPayment`
- `Modules/Finance/Tuilow.Finance.Application/EventHandlers/CoursePurchaseConfirmedEventHandler.cs`
- `Modules/Finance/Tuilow.Finance.Infrastructure/Repositories/CreatorWalletRepository.cs`

O dispatcher de domain events agora registra falhas como `Critical` sem derrubar a resposta do webhook depois que o pagamento já foi persistido.

### Ajuste adicional aplicado

O relatório original dizia que o `try/catch` individual impediria um handler de bloquear os demais. Na prática, `mediator.Publish` pode interromper a publicação do mesmo evento no primeiro handler que lança exceção.

Corrigi isso no `AppDbContext`: os handlers agora são resolvidos via DI e executados um a um. Uma falha em Learning não impede Finance, e vice-versa.

Os endpoints administrativos de reprocessamento também foram ajustados para executar handlers individualmente e retornar erro parcial se algum handler falhar.

## M12 - Correlação entre pagamento, matrícula e notificação

Validado em:

- `Modules/Learning/Tuilow.Learning.Domain/Entities/Enrollment.cs`
- `Modules/Learning/Tuilow.Learning.Domain/Entities/NotificationLog.cs`
- `Modules/Learning/Tuilow.Learning.Infrastructure/Data/Configurations/EnrollmentConfiguration.cs`
- `Modules/Learning/Tuilow.Learning.Infrastructure/Data/Configurations/NotificationLogConfiguration.cs`
- `Modules/Learning/Tuilow.Learning.Application/EventHandlers/CoursePurchaseConfirmedEventHandler.cs`
- `Modules/Learning/Tuilow.Learning.Application/EventHandlers/SubscriptionPaymentConfirmedEventHandler.cs`
- `Modules/Sales/Tuilow.Sales.Domain/Events/CoursePurchaseConfirmedDomainEvent.cs`

`Enrollment` agora possui `SourcePurchaseId` e `SourceSubscriptionId`, e o evento `CoursePurchaseConfirmedDomainEvent` carrega `AsaasPaymentId`.

`NotificationLog` registra tentativas reais de envio de e-mail, com sucesso ou falha, correlacionadas por `AsaasPaymentId` e pelo ID da compra ou assinatura.

### Ajuste adicional aplicado

A regra de que `SourcePurchaseId` e `SourceSubscriptionId` são mutuamente exclusivos estava só no comentário/domínio. Adicionei check constraint `ck_enrollments_single_source` no mapeamento EF, na migration, no designer e no snapshot.

## Migração EF

Diferente do relatório original, a migration já existe:

- `Host/Tuilow.Host.Api/Migrations/20260730121321_AddPaymentCorrelationAndNotificationLog.cs`
- `Host/Tuilow.Host.Api/Migrations/20260730121321_AddPaymentCorrelationAndNotificationLog.Designer.cs`
- `Host/Tuilow.Host.Api/Migrations/AppDbContextModelSnapshot.cs`

Ela adiciona:

- `learning.enrollments.SourcePurchaseId`
- `learning.enrollments.SourceSubscriptionId`
- `learning.notification_logs`
- `ck_enrollments_single_source`

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

## Conclusão

As correções C2 e M12 estão implementadas e compilando. O relatório do Claude estava correto no objetivo geral, mas estava desatualizado sobre a migration e superestimava a proteção do `mediator.Publish`; ambos os pontos foram corrigidos.
