# Relatório de Segurança - Webhook Asaas

**Data:** 29/07/2026  
**Status:** Implementado e validado por build  
**Prioridade:** Crítica

## Objetivo

Corrigir a validação do webhook da Asaas para aceitar apenas requisições autenticadas pelo token enviado no header `asaas-access-token`, removendo a validação incorreta por HMAC do corpo da requisição.

## Alterações registradas

- `AsaasPaymentService.ValidateWebhookSignature` agora recebe apenas o token do header e compara com `Asaas:WebhookSecret`.
- A comparação usa `CryptographicOperations.FixedTimeEquals`, evitando vazamento por timing.
- `AsaasWebhookController` lê o header `asaas-access-token` e não usa mais o corpo da requisição na validação.
- `IPaymentService` foi simplificada para `ValidateWebhookSignature(string accessToken)`.
- `Program.cs` bloqueia o startup fora de `Development` quando `Asaas:WebhookSecret` não está configurado.
- `README.md` foi atualizado para remover a referência antiga a HMAC.

## Base técnica

A documentação da Asaas define que o token de autenticação do webhook é enviado no header `asaas-access-token`. O valor deve ser o mesmo token configurado no painel ou criado via API como `authToken`.

## Validação realizada

Comando executado:

```text
dotnet build Tuilow.sln
```

Resultado:

```text
Compilação com êxito.
0 Aviso(s)
0 Erro(s)
```

Não foram encontrados projetos de teste `.csproj` dentro de `tests`, então não havia suíte local de testes automatizados para executar além do build.

## Impacto

O fluxo de webhook passa a seguir o formato esperado pela Asaas:

```text
Webhook Asaas
-> lê header asaas-access-token
-> compara com Asaas:WebhookSecret
-> processa evento apenas se o token for válido
```

Fora de `Development`, a aplicação não inicializa se `Asaas:WebhookSecret` estiver vazio. Esse comportamento é intencional para impedir deploy inseguro.

## Pendência de infraestrutura

Configurar `Asaas:WebhookSecret` no ambiente de produção com o mesmo valor cadastrado no webhook da Asaas.

Não gravar esse segredo em `appsettings.json`. Usar o mecanismo seguro do ambiente, como variáveis de ambiente, secrets do provedor, Docker Secrets, Kubernetes Secret ou configuração protegida do serviço de hospedagem.

## Próxima auditoria recomendada

- Confirmar idempotência do processamento de webhooks.
- Revisar transições de status de pagamento.
- Auditar autorização dos endpoints de cursos, compras e assinaturas.
- Validar que segredos nunca aparecem em logs.
- Monitorar falhas de webhook e fila pausada no painel da Asaas.
