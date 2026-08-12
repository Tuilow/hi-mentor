# Relatório — Dashboard Financeiro do Mentor

**Data:** 12/08/2026
**Pedido do usuário:** "quando ele [o mentor] clique em financeiro, tenha o controle dele de vendas, estornos, porcentagem de ganho... que ele tenha acesso a todo sua parte financeira... inclusive quem já comprou e pagou aparecer para ele, e quando o mentor logar que apareça um dashboard para ele desses resumos."

## Diagnóstico

Investigando antes de construir qualquer coisa nova, encontrei que **o backend já tinha praticamente tudo pronto** — só nunca tinha sido ligado ao frontend:

- `GET /api/v1/finance/dashboard` (`GetCreatorFinancialDashboardQuery`) já existia: saldo disponível/pendente, totais brutos/líquidos, comissão paga, ciclo de pagamento.
- `GET /api/v1/finance/sales` (`GetCreatorSalesHistoryQuery`) já existia, mas com um problema real: só devolvia lançamentos da **carteira interna (WalletTransaction)**, que só existe no modelo **Legacy**. O modelo que o mentor usa hoje — subconta própria na Asaas ("MarketplaceSplit") — **nunca gera WalletTransaction** (o dinheiro nunca passa pela Hi Mentor), então esse endpoint devolvia uma lista **vazia** pra qualquer mentor no modelo atual.
- `GET /api/v1/finance/fee` (percentual de comissão vigente) já existia e funcionava.
- No frontend, a tela "Financeiro" do mentor (`(dashboard)/admin/financeiro/page.tsx`) nunca chamava nenhum desses três endpoints — para um mentor já aprovado (`canSell = true`), ela só mostrava um card estático: "Sua conta financeira está pronta!". Nenhum número, nenhuma venda, nenhum estorno.

## O que foi feito

### Backend (4 arquivos, Finance.Application)

1. **`GetCreatorSalesHistoryQuery.cs`** — troquei o retorno de "lista de WalletTransaction" para uma lista de **`CreatorSaleItemResponse`**, uma linha por `CoursePurchase` (cobre os dois modelos de cobrança igualmente), com: comprador (nome/e-mail), curso, valor bruto, comissão %, valor líquido que o mentor recebeu, status (`Pending`/`Confirmed`/`Failed`/`Refunded`), status de liberação na carteira (só Legacy) e datas.
2. **`GetCreatorSalesHistoryQueryHandler.cs`** — reescrito: busca todas as `CoursePurchase` do criador (`ICoursePurchaseRepository.GetByCreatorAsync`, já existia), enriquece com nome/e-mail do aluno (reaproveitando `ICreatorDisplayInfoLookup`, que já é genérico o bastante para qualquer `UserId`, não só criador) e título do curso (`ICourseRepository.GetByIdsAsync`, do módulo Catalog — referência Domain-a-Domain entre módulos, mesmo padrão já usado em Learning). Para vendas Legacy, cruza com o `WalletTransaction` correspondente pra pegar o percentual de comissão real aplicado e o status de liberação (isso não existe no `CoursePurchase` nesse modelo).
3. **`GetCreatorFinancialDashboardQuery.cs`** — adicionei `TotalRefundedAmount`/`TotalRefundedCount` (Legacy) e `MarketplaceRefundedAmount`/`MarketplaceRefundedCount` (MarketplaceSplit), pra existir um KPI de "estornos" de cara no resumo, sem precisar contar linha a linha na lista de vendas.
4. **`GetCreatorFinancialDashboardQueryHandler.cs`** — calcula os novos totais de estorno a partir das `WalletTransaction` do tipo `SaleRefund` (Legacy) e das `CoursePurchase` com `Status == Refunded` (MarketplaceSplit).

Nenhuma mudança de schema/migration — tudo já existia no banco, só a leitura mudou.

### Frontend (2 arquivos)

5. **`lib/api.ts`** — adicionado `financeApi` (métodos `getDashboard`/`getSales`/`getCurrentFee`, nunca chamados antes) + os tipos `CreatorFinancialDashboard`/`CreatorSaleItem`/`PlatformFeeInfo`.
6. **`(dashboard)/admin/financeiro/page.tsx`** — quando `status.canSell === true`, a tela agora mostra um dashboard de verdade em vez do card estático:
   - Aviso de transparência com o percentual de comissão vigente (`/finance/fee`).
   - Seção "Vendas na sua conta Asaas" (modelo atual): total vendido bruto, comissão paga, quanto o mentor recebeu líquido, número de vendas confirmadas, e — se houver — quanto foi estornado.
   - Seção "Carteira Hi Mentor" (modelo legado, só aparece se o mentor tiver alguma movimentação nele): saldo disponível, saldo pendente, total já sacado, estornos, e data da próxima liberação.
   - Tabela "Suas vendas": uma linha por compra, com avatar/nome/e-mail do comprador, curso, valor, comissão, valor líquido, status (badge colorido: Pago/Aguardando pagamento/Falhou/Estornado) e data — isto é o "quem já comprou e pagou aparecer para ele" pedido.

## Fora do escopo, identificado mas não implementado

O módulo `Payout` (solicitação de saque) já existe no backend (`PayoutsController`, `RequestPayoutCommand`, etc.) mas **nunca foi ligado ao frontend** — não existe nenhuma chamada a rotas de payout em `lib/api.ts`. O pedido do usuário foi especificamente sobre **visibilidade** (vendas, estornos, percentual, quem comprou), não sobre iniciar saques, então não mexi nisso agora — mas é o próximo passo natural se o mentor quiser sacar o saldo da carteira Legacy pela própria plataforma (hoje isso não tem nenhuma tela).

## Validação executada

`dotnet build` não roda neste sandbox (bloqueio de NuGet já conhecido) — validado por balanceamento de chaves/parênteses nos 4 arquivos C# + revisão manual das assinaturas/DI (todas as dependências injetadas já são registradas globalmente no container do Host, mesmo padrão usado por `EnrollStudentCommandHandler` para `ICourseRepository` cross-módulo).

Frontend **validado de verdade** neste sandbox: montei uma árvore completa de `src/Tuilow.Web` (via tar do device), rodei `npm install` e depois `npx tsc --noEmit` (modo strict) — pegou 2 erros reais no primeiro run (`Card` exige `children`, dois skeletons estavam self-closing) e corrigi ambos; segunda rodada limpa. `npx next lint --dir src` limpo nos 2 arquivos tocados (só os mesmos achados pré-existentes de sempre em arquivos não tocados: aspas não escapadas em `admin/produtos/[id]/dashboard` e `<img>` sem `next/image` em 3 páginas — nenhum deles relacionado a esta mudança).

Os 6 arquivos entregues foram confirmados **byte-idênticos via SHA-256** direto no disco do usuário (comparação rodada no próprio device, não só no container).

## Conclusão

O mentor agora vê, ao clicar em "Financeiro": quanto vendeu, quanto a Hi Mentor reteve de comissão (e qual o percentual vigente), quanto ele recebeu líquido, quantas vendas foram estornadas e o valor, e uma lista completa de quem comprou/pagou — cobrindo tanto o modelo atual (subconta Asaas) quanto o legado, de forma transparente e sem precisar consultar a Asaas manualmente.
