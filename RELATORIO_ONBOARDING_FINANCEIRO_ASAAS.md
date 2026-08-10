# Relatório — Onboarding Financeiro do Criador via Asaas BaaS (Subcontas)

**Data:** 10/08/2026
**Escopo:** substituição completa do fluxo "criador cola sua própria API Key da Asaas" por um
onboarding financeiro 100% nativo do Tuilow, em que a própria plataforma cria a subconta Asaas
de cada criador via API (modelo BaaS).

---

## 1. Diagnóstico

O fluxo anterior partia da premissa (documentada no código antigo) de que criar subconta via
`POST /v3/accounts` exigiria CNPJ, inviabilizando autônomos. Verificação direta na documentação
atual da Asaas (docs.asaas.com, ago/2026) mostrou que essa premissa está desatualizada: o
endpoint aceita tanto pessoa física (CPF + data de nascimento) quanto pessoa jurídica (CNPJ +
tipo de empresa). Isso tornou viável o modelo pedido — subconta criada pela Tuilow, sem o
criador nunca precisar acessar o painel da Asaas.

Módulos/arquivos relevantes antes da mudança:

- **Finance**: só existia `CreatorAsaasAccount`/`CreatorAsaasCustomer` (modelo "cole sua API
  Key"), com `IAsaasAccountOnboardingService` validando a chave colada e registrando webhook.
- **Sales**: `PurchaseCourseCommandHandler` escolhia entre cobrança via Marketplace (split
  direto na conta do criador) ou Legacy (conta da própria Tuilow) — sem nenhum bloqueio: um
  criador sem conta configurada simplesmente caía no modelo Legacy, silenciosamente.
- **Gating de publicação**: `PublishCourseCommandHandler` (Catalog) e `PublishProductCommandHandler`
  (CreatorStudio) não faziam nenhuma checagem financeira.
- **Segurança**: `ISecretProtector` (ASP.NET Data Protection API) já existia e foi inteiramente
  reaproveitado — não foi necessário criar nova infraestrutura de criptografia.
- **Testes**: não havia suíte ativa (`tests/Tuilow.Application.Tests` existia vazio, fora do
  `.sln`).

---

## 2. Arquitetura

### Novo agregado: `CreatorAsaasSubaccount` (Finance.Domain)

Paralelo ao `CreatorAsaasAccount` legado, que permanece intocado (FK histórica em
`CoursePurchase.CreatorAsaasAccountId`, tela de auditoria admin). Campos principais: `CreatorId`
(único), `AsaasAccountId`, `WalletId`, `ApiKeyEncrypted` (via `ISecretProtector`),
`WebhookTokenHash`, `OnboardingStatus` (`NotStarted → CollectingData → AccountCreationPending →
AccountCreated → DocumentsPending → UnderReview → Approved / Rejected / Blocked`), dados
pessoais/empresariais, `RejectionReason`, `ApprovedAt`. `CanSell => Status == Approved &&
!Blocked`.

Entidade filha `CreatorAsaasOnboardingDocument`: documentos são **sempre** sincronizados a
partir da resposta real da API da Asaas (`GET /v3/myAccount/documents`) — nunca uma lista fixa
no código.

Tabela `ProcessedAsaasAccountEvent`: dedup de webhooks de conta por `EventId` (a Asaas garante
entrega *at-least-once*, não *exactly-once*).

### Cliente Asaas: `IAsaasSubaccountClient` / `AsaasSubaccountClient`

Novo, ao lado do `IAsaasAccountOnboardingService` legado (que continua servindo só o modelo
antigo já persistido). Cobre: `CreateSubaccountAsync` (`POST /v3/accounts`, credencial da conta
pai), `GetPendingDocumentsAsync`/`UploadDocumentAsync` (com a API Key da própria subconta),
`GetAccountStatusAsync` (fallback de polling), `RegisterAccountStatusWebhookAsync` (idioma
idempotente get-then-put-or-post, extraído para `AsaasWebhookRegistrar` compartilhado).
`HttpClient` nomeado `"AsaasSubaccount"` com `.AddStandardResilienceHandler()`.

### Commands/Queries (Finance.Application)

`StartCreatorFinancialOnboardingCommand`, `SyncCreatorOnboardingDocumentsCommand`,
`UploadCreatorOnboardingDocumentCommand`, `ProcessAsaasAccountStatusWebhookCommand`,
`GetMyFinancialOnboardingStatusQuery` (traduz o estado interno para os 5 passos amigáveis da
UI), `AdminListCreatorOnboardingsQuery`, `AdminSetCreatorOnboardingBlockedCommand`.

### Endpoints (Finance.Api)

Novo `CreatorFinancialOnboardingController` (`api/v1/finance/onboarding`): `GET /`, `POST
/start`, `GET /documents`, `POST /documents/{id}/upload`. Nova rota de webhook: `POST
api/v1/webhooks/asaas-account-status` (payload de conta é estruturalmente diferente do payload
de pagamento — por isso rota e DTO isolados, sem tocar no caminho de pagamento já em produção).
O controller antigo (`api/v1/finance/asaas-account`) permanece no código, mas não é mais
referenciado pelo novo frontend.

### Gating de publicação e venda

- `PublishCourseCommandHandler` (Catalog) e `PublishProductCommandHandler` (CreatorStudio)
  passaram a checar `CanSell` via um port (`ICreatorFinancialStatusLookup` no Catalog;
  acoplamento direto a `Tuilow.Finance.Domain` no CreatorStudio, que já é uma exceção
  documentada na arquitetura deste repo) e lançam `BusinessException` amigável se o criador
  ainda não estiver aprovado — nunca bloqueia criação/edição, só publicação.
- `PurchaseCourseCommandHandler` (Sales) perde o fallback silencioso: se o criador não estiver
  `Approved`, a compra é recusada mesmo que o feature flag do Marketplace esteja desligado —
  defesa em profundidade, já que a publicação deveria ser inatingível sem aprovação.
- **Decisão já validada com você**: o bloqueio vale para **todos** os criadores, imediatamente,
  sem grandfathering. Isso significa que qualquer curso hoje publicado por um criador que ainda
  não passou pelo novo onboarding para de vender no momento do deploy, até a aprovação (que não
  é instantânea). Ver seção 9.
- `IMarketplaceFeatureFlag` continua sendo o kill-switch único — desligá-lo reverte para o
  comportamento anterior (sem exigir subconta), rollback barato em caso de problema.

### Migration

Uma migration nova e só aditiva (`Host/Tuilow.Host.Api/Migrations/20260810150000_AddCreatorFinancialOnboarding.cs`):
cria `finance.creator_asaas_subaccounts`, `finance.creator_asaas_onboarding_documents`,
`finance.processed_asaas_account_events`. Nenhuma tabela existente é alterada. **Importante**:
essa migration foi escrita manualmente (ver seção 9 — não foi possível gerar o par
migration+Designer.cs+snapshot com `dotnet ef` neste ambiente); revisar/regenerar antes de
aplicar em produção.

---

## 3. Alterações — inventário de arquivos

Todos os arquivos abaixo foram verificados por checksum (SHA-256) entre o ambiente de trabalho e
o seu repositório antes do envio — a lista é exatamente o que mudou, nem mais nem menos.

**Arquivos novos (backend, 42):** entidades/enums/interfaces de domínio
(`CreatorAsaasSubaccount`, `CreatorAsaasOnboardingDocument`, `ProcessedAsaasAccountEvent` e
enums/repositórios associados), configurações EF Core, repositórios, `AsaasSubaccountClient`,
`AsaasAccountStatusWebhookAuthenticator`, `AsaasWebhookRegistrar`, todos os
commands/queries/validators de onboarding, os dois novos controllers, e a migration.

**Arquivos novos (testes, 10):** projeto `tests/Tuilow.Finance.Tests` completo (csproj, fakes,
testes de domínio/aplicação/segurança).

**Arquivos editados (backend, 11):** `PublishCourseCommandHandler`, `Catalog.Infrastructure/
DependencyInjection.cs`, `Catalog.Infrastructure.csproj` (referência a Finance.Domain),
`PublishProductCommandHandler`, `Finance.Api/AdminFinanceController` (novas rotas de
onboarding), `Finance.Infrastructure/DependencyInjection.cs` e `.csproj` (novos registros e
pacote de resiliência), `Sales/PurchaseCourseCommandHandler`, `Sales/ICreatorPaymentAccountLookup`
e `FinanceCreatorPaymentAccountLookup` (novo método `HasApprovedFinancialOnboardingAsync`),
`Tuilow.sln` (novo projeto de testes registrado).

**Arquivos novos/editados (frontend, 4):** `src/lib/api.ts` (novos clients tipados,
`financialOnboardingApi`/`adminFinanceApi` estendido — exports legados preservados),
`admin/financeiro/page.tsx` (nova jornada de 5 passos do criador), `plataforma/financeiro/page.tsx`
(nova aba de onboarding + tabela legada preservada), `admin/produtos/novo/page.tsx` (banner
não-bloqueante no passo de publicação).

---

## 4. Fluxo do usuário (criador)

Tela em `/admin/financeiro` (rota mantida por já ser a usada pelo criador — ver nota sobre
nomenclatura confusa na seção 9), 5 passos:

1. **Dados**: formulário só com os campos que a Asaas realmente exige (CPF ou CNPJ, conforme
   o tipo escolhido; nome, e-mail, telefone, endereço, renda). Ao enviar, dispara a criação da
   subconta.
2. **Processando**: tela de espera curta enquanto a Tuilow chama a Asaas e persiste a subconta
   (polling automático).
3. **Documentos**: lista sincronizada em tempo real com a Asaas; item com link externo
   (`onboardingUrl`) abre página hospedada pela Asaas em nova aba; item sem link usa upload
   nativo.
4. **Em análise**: tela de espera com polling, sem nenhum jargão técnico.
5. **Aprovado / Rejeitado / Bloqueado**: aprovado libera venda; rejeitado explica em linguagem
   simples e leva de volta para reenvio de documentos (não é possível reeditar os dados
   cadastrais depois que a subconta já existe — limitação da própria Asaas); bloqueado (ação
   administrativa) explica que a conta foi suspensa.

Em nenhum momento o texto da tela usa "API Key", "Wallet ID" ou "Subconta".

## 5. Fluxo Asaas (técnico)

`POST /v3/accounts` (credencial da conta pai) → resposta traz `accountId`/`walletId`/`apiKey`
**uma única vez** (persistidos imediatamente, criptografados) → aguardar ≥15s → `GET
/v3/myAccount/documents` (com a API Key da subconta) para descobrir os documentos pendentes →
upload (`POST /v3/myAccount/documents/{id}`) para os que não têm `onboardingUrl` → webhook de
conta (`ACCOUNT_STATUS_*`) confirma aprovação/rejeição de cada categoria (documentos, dados
comerciais, dados bancários) e um evento guarda-chuva (`ACCOUNT_STATUS_GENERAL_APPROVAL_*`)
confirma o veredito final.

## 6. Segurança

- `ApiKeyEncrypted` segue o padrão já existente (`ISecretProtector`/Data Protection API) —
  nunca sai de Infrastructure, nunca aparece em DTO de Application/Api.
- Upload de documento é sempre um proxy de stream direto para a Asaas — o Tuilow nunca grava o
  arquivo em disco/banco/log.
- Teste dedicado (`NoSecretsInResponseDtosTests`, reflection sobre todo o namespace de
  Commands/Queries) falha automaticamente se qualquer DTO novo expuser `apiKey`/`accessToken`.
- Grep final no backend e no frontend por `ApiKey`/`AccessToken` fora de Domain/Infrastructure:
  limpo.

## 7. Webhooks

Autenticação por hash do `WebhookTokenHash` (mesmo idioma do fluxo de pagamento). Dedup por
`EventId` via `ProcessedAsaasAccountEvent` — evento repetido é no-op idempotente mesmo com
payload diferente. Evento de conta desconhecida ou tipo não mapeado é ignorado com segurança
(e ainda marcado como processado, para não reprocessar indefinidamente).

**Bug real encontrado e corrigido durante a escrita dos testes**: o handler tinha um comentário
afirmando resiliência a webhooks fora de ordem, mas o código não implementava isso — um evento
tardio de rejeição de categoria (ex.: `DOCUMENT_REJECTED`) podia reverter silenciosamente um
criador já `Approved`. Corrigido adicionando uma guarda de estado (evento de categoria não
regride mais um criador aprovado; só o evento guarda-chuva `GENERAL_APPROVAL_REJECTED` pode
reverter uma aprovação, por ser o veredito final da Asaas). Coberto por dois testes dedicados.

## 8. Testes

`tests/Tuilow.Finance.Tests` (novo, registrado no `.sln`), 29 testes com fakes (sem HTTP real):

- **Domínio (16)**: toda a máquina de estados de `CreatorAsaasSubaccount` — transições válidas
  e inválidas, imutabilidade pós-criação, idempotência de `MarkAccountCreated`, nunca regredir
  de `Approved`, precedência de `Block`.
- **Aplicação (12)**: criação idempotente (segunda chamada nunca duplica a subconta), falha na
  chamada à Asaas reverte para `CollectingData`, retry após falha soma exatamente 2 chamadas ao
  cliente mas só uma subconta lógica, falha no registro do webhook não desfaz a subconta já
  criada; processamento de webhook (aprovação, rejeição com mensagem genérica nunca expondo o
  nome bruto do evento da Asaas, evento duplicado é no-op, conta desconhecida ignorada com
  segurança, tipo de evento não mapeado não altera estado, os dois casos de ordem de chegada
  descritos na seção 7).
- **Segurança (1)**: nenhum DTO de resposta expõe segredo.

## 9. Pendências

1. **Bloqueador externo (o mais importante)**: o modelo BaaS ainda não foi habilitado pelo seu
   gerente de conta na Asaas. Todo o código foi construído e testado contra o sandbox/documentação;
   nada disto funciona em produção até esse contato ser feito.
2. **Teto de avaliação de 60 dias**: assim que habilitado, a Asaas limita a 10 subcontas e
   R$2.000 em cobranças por subconta nesse período — sequenciar lançamento gradual.
3. **Selfie/documento com `onboardingUrl`** é obrigatoriamente uma página externa hospedada pela
   Asaas — limitação da própria plataforma, não dá para internalizar.
4. **Recuperação de API Key perdida** exige um passo manual (habilitar acesso por 2h no painel
   Asaas) antes de chamar o endpoint de regeneração — vira nota de runbook, não é automatizável.
5. **Continuidade de receita**: o bloqueio de venda vale para todos os criadores imediatamente
   (decisão já validada com você) — cursos hoje publicados por criadores que ainda não passaram
   pelo novo onboarding param de vender no deploy, até a aprovação.
6. **Validação de build neste ambiente**: não foi possível rodar `dotnet build`/`test` nesta
   sandbox. Causa raiz identificada: o proxy de rede deste ambiente tem uma lista de domínios
   liberados que inclui npm/PyPI/crates/Go proxy, mas **não** nuget.org — nenhum projeto com
   `PackageReference` consegue restaurar aqui, independente da versão do SDK instalada.
   Substituí por uma verificação heurística (balanceamento de chaves em todos os 103 arquivos
   `.cs` tocados) e por validação real de frontend (`tsc`/`next lint`, que passaram limpos).
   **Recomendo fortemente rodar `dotnet build`/`dotnet test tests/Tuilow.Finance.Tests` no seu
   ambiente local antes de aplicar isso em produção.**
7. **Migration escrita manualmente**: não foi possível gerar o par oficial
   migration+Designer.cs+snapshot com `dotnet ef` neste ambiente (mesma causa acima). A migration
   entregue espelha exatamente as `EntityTypeConfiguration` novas, mas antes de aplicar em
   qualquer ambiente real, rode `dotnet ef migrations add AddCreatorFinancialOnboarding` num
   ambiente com SDK 9 — ele deve detectar que o modelo já bate com o `Up()` entregue e gerar só o
   Designer.cs/snapshot faltantes.
8. **Payout (carteira interna)**: com a migração forçada para o modelo de subconta/split, o
   módulo Payout deixa de receber novos créditos (dinheiro passa a ir direto para a subconta do
   criador, nunca pela Tuilow) — fica só para saldo histórico já existente. Nenhum código
   precisou mudar, mas vale documentar essa mudança de comportamento operacional.
9. **Nomenclatura de rotas confusa**: `/admin/financeiro` é a tela financeira do **criador**
   (faz parte de `creatorNavItems`, apesar do nome "Administração"); `/plataforma/financeiro` é
   a tela do **dono da plataforma**. Isso já está documentado em um comentário no próprio
   `(dashboard)/layout.tsx`, mas causou uma quase-confusão durante este trabalho — vale
   considerar renomear essas rotas num momento oportuno.

## 10. Próximos passos

1. Contatar o gerente de conta Asaas para habilitar o modelo BaaS (pendência 1).
2. Rodar `dotnet build`/`dotnet test tests/Tuilow.Finance.Tests` no seu ambiente local — este
   ambiente de sandbox não conseguiu validar compilação (pendência 6).
3. Rodar `dotnet ef migrations add AddCreatorFinancialOnboarding` num ambiente com SDK 9 para
   gerar o Designer.cs/snapshot oficiais antes de aplicar em produção (pendência 7).
4. Definir uma janela de comunicação para os criadores já ativos, avisando sobre a migração
   obrigatória para o novo onboarding antes do bloqueio de venda entrar em vigor (pendência 5).
5. Planejar o lançamento gradual dentro do teto de 60 dias/10 subcontas da avaliação inicial da
   Asaas (pendência 2).
6. Revisar a nomenclatura de rotas `/admin/financeiro` vs `/plataforma/financeiro` (pendência 9).
