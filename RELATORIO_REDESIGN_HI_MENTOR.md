# Redesign "Hi Mentor" — Relatório de Entrega

**Data:** 11/08/2026
**Origem do design:** protótipo Figma Make conectado localmente (`hi-mentor/`), com
design system próprio (`src/components/ui.tsx`, `src/components/Sidebar.tsx`,
`src/components/Logo.tsx`) e 9 páginas de prototipagem (`src/pages/*.tsx`).
**Escopo:** recriação página a página no Tuilow.Web real, adotando a marca "Hi Mentor" e
a terminologia do protótipo (Programas/Mentorados/Mentor), **sem alterar nenhuma lógica
de negócio** — mesmas rotas, mesmos hooks (`useQuery`/`useMutation`), mesmos handlers,
mesmas chamadas de API.

## Princípio seguido em toda a entrega

Cada página recriada reproduz a estrutura visual do protótipo correspondente, mas exibe
**apenas dados reais que o backend do Tuilow já fornece hoje**. Onde o protótipo assume
um dado que o Tuilow não tem, a seção correspondente foi omitida ou adaptada — nunca
preenchida com números ou conteúdo inventado. Cada omissão está documentada abaixo.

## Fase 1 — Fundação do design system

- **`tailwind.config.ts`**: nova escala neutra OKLCH (`canvas`, `surface`, `surface-2`,
  `subtle`, `line`, `line-light`, `ink`/`ink-2`/`ink-3`/`ink-4`), idêntica à do
  protótipo. `brand`/`accent` (usados por classes globais e por páginas fora do escopo
  desta rodada) foram repontados para violet/amber — qualquer tela não tocada nesta
  rodada já herda a cor nova, em vez de continuar azul/laranja.
- **`src/app/globals.css`**: classes `@layer components` (`.btn-primary`, `.btn-secondary`,
  `.card`, `.badge-*`, `.input-field` etc.) restyladas para o visual arredondado/roxo do
  protótipo — como são usadas por páginas ainda não recriadas, essas páginas também
  ganham parte do novo visual de graça.
- **`src/app/layout.tsx`**: fonte trocada de Inter para Manrope (`next/font/google`);
  `metadata.title`/`description` atualizados para "Hi Mentor".
- **`src/components/ui/design-system.tsx`** (novo): `Button`, `Badge`, `Avatar`,
  `Progress`, `Input`, `Card`, `StatCard`, `SectionHeader`, `EmptyState`, `Divider` —
  portados do protótipo, reutilizados pelas páginas recriadas.
- **`src/components/brand/Logo.tsx`** (novo): `Logo`/`HiSymbol`/`LogoAppIcon` — logo
  "Hi Mentor" (símbolo + wordmark), SVG puro.

## Fase 2 — Sidebar do dashboard

**`src/app/(dashboard)/layout.tsx`** — sidebar reconstruído no visual do protótipo
(itens arredondados, estado ativo violeta, logo Hi Mentor, ícones `lucide-react` no
lugar de emojis). Nenhuma rota, nenhum hook (`user-profile`, `my-enrollments`) e nenhuma
regra de gating (`isCreator`, `isPlatformAdmin`, `hasAnyCourseAccess`) foi alterada — só
rótulos foram atualizados: "Meus Produtos" → "Meus Programas", "Meus Cursos" (biblioteca)
→ "Minha Jornada", "Gerenciar Cursos" → "Conteúdos", "Estúdio do Criador" → "Estúdio do
Mentor". Os itens do painel do dono da plataforma (Visão Geral/Usuários/Financeiro) não
têm contrapartida no protótipo e mantiveram os rótulos originais.

## Fase 3 — Páginas recriadas

1. **Login e Registro** (`(auth)/login`, `(auth)/registro`) — layout de dois painéis do
   protótipo. Todos os campos, validação Zod, submit, Google OAuth e mensagens de erro
   continuam exatamente os mesmos. O protótipo mostra estatísticas fixas ("2.400+
   mentorados", "98% satisfação") no painel esquerdo — como são números sem lastro real,
   foram substituídos por três ícones de proposta de valor (sem números).

2. **Dashboard** (`(dashboard)/dashboard`) — cabeçalho de saudação, card "continuar
   assistindo" e banner "torne-se mentor" mantidos com a mesma lógica; os 4 cards de
   estatística agora usam `StatCard`. **Omitido**: o feed de "atividade recente", a
   agenda de "próximos encontros" e a lista de "mentorados em risco" do protótipo — não
   existe, no backend do Tuilow, nenhum endpoint de atividade de mentorados, agenda de
   encontros ao vivo ou score de engajamento/risco de abandono. Mostrar essas seções
   exigiria inventar dados.

3. **Financeiro** (`(dashboard)/admin/financeiro`) — melhor mapeamento do lote: o
   stepper dinâmico (`status.steps`, vindo do backend) foi reestilado no formato de
   círculos numerados do protótipo; todas as etapas (dados pessoais, documentos,
   processamento, análise, bloqueado, pronto), campos, mutations e o polling automático
   (8s enquanto `AccountCreationPending`/`UnderReview`) continuam idênticos.

4. **Meus Programas — mentor** (`(dashboard)/admin/produtos`) — tabela convertida em
   grid de cards. **Nota de dado real**: `ProductListItem` (o DTO que esse endpoint
   retorna) não tem campo de imagem de capa — em vez de inventar uma foto, cada card
   recebe um dos 4 gradientes decorativos do protótipo, alternados por posição (mesmo
   tratamento que o próprio protótipo já dá a programas sem capa). Todas as ações
   (editar, duplicar, arquivar, excluir, ver dashboard, divulgar) e todos os dados
   (status, vendas, receita) continuam ligados às mesmas mutations/queries de antes.

5. **Minha Jornada — mentorado** (`(dashboard)/meus-cursos`) — cards restylados
   (`Card`/`Badge`/`Progress`), mesma query única (`my-enrollments`), mesma regra de
   nível/conclusão.

6. **Player de aula** (`(dashboard)/cursos/[slug]/[lessonId]`) — arquivo de maior risco
   (732 linhas de lógica de HLS/YouTube/Vimeo/salvamento de progresso). Tratamento
   deliberadamente mais conservador que as demais páginas: **somente classNames e ícones
   foram trocados, a estrutura do DOM e absolutamente toda a lógica (refs, effects,
   handlers, paywall) permanecem linha a linha as mesmas** — confirmado por
   `tsc --noEmit` e por revisão manual comparando com o arquivo original. O protótipo
   tem abas de Descrição/Materiais/Anotações/Discussão — **omitidas**: não existe, no
   backend, descrição de aula, lista de materiais anexos, sistema de anotações ou
   discussão por aula.

7. **Canal público** (`canal/[handle]`) — banner, bloco de perfil (avatar, bio, redes
   sociais — já eram dados reais) e grid de programas no visual do protótipo. O
   protótipo usa abas Programas/Sobre/Comunidade; como só há dado real para
   bio/redes/programas, **as abas foram removidas** em favor de uma página única — bio e
   redes já aparecem no bloco de perfil, e uma aba "Sobre" ou "Comunidade" vazia/fake
   seria pior do que não ter abas.

## Fora do escopo desta rodada

Sem página correspondente no protótipo — continuam com a lógica e o layout de antes,
só herdando a paleta/fonte novas via Fase 1 (classes `.card`/`.btn-primary`/`.badge-*`
globais): `estudio/gravador`, `estudio/teleprompter`, `admin/cursos` (gestão de
conteúdo/upload de vídeo), `admin/produtos/novo`, `admin/produtos/[id]/dashboard`,
`plataforma/*` (painel do dono da plataforma), `c/[slug]` (página de vendas pública),
`certificado/[code]`, `historico`, `assinatura`.

Também não existe hoje, no Tuilow, uma página "lista de todos os meus mentorados" — o
protótipo tem uma (`MentoradosPage.tsx`), mas seria uma funcionalidade nova (exigiria um
endpoint que não existe), não um redesign. Não foi criada.

## Verificação executada

- `npx tsc --noEmit` (modo strict) sobre a árvore completa do `src/Tuilow.Web`, com
  todos os arquivos desta entrega sobrepostos — **0 erros**.
- `npx next lint --dir src` — **0 erros/avisos novos** nos 14 arquivos desta entrega; os
  únicos achados do lint (2 erros de aspas não escapadas + avisos de `<img>`) estão em 4
  arquivos que esta entrega não tocou (`admin/produtos/[id]/dashboard`,
  `cursos/[slug]/page.tsx`, `cursos/page.tsx`, `c/[slug]/page.tsx`) — pré-existentes.
- Revisão manual arquivo a arquivo comparando com a versão anterior, confirmando que
  nenhum hook, handler, mutation ou prop foi removido ou teve assinatura alterada.
- Todos os arquivos entregues foram verificados byte-a-byte (SHA-256) entre o container
  e o dispositivo após a cópia.

## Arquivos alterados/criados (14 + este relatório)

Novos: `src/Tuilow.Web/src/components/ui/design-system.tsx`,
`src/Tuilow.Web/src/components/brand/Logo.tsx`.

Editados: `src/Tuilow.Web/tailwind.config.ts`, `src/Tuilow.Web/src/app/globals.css`,
`src/Tuilow.Web/src/app/layout.tsx`, `src/Tuilow.Web/src/app/(dashboard)/layout.tsx`,
`src/Tuilow.Web/src/app/(auth)/login/page.tsx`,
`src/Tuilow.Web/src/app/(auth)/registro/page.tsx`,
`src/Tuilow.Web/src/app/(dashboard)/dashboard/page.tsx`,
`src/Tuilow.Web/src/app/(dashboard)/admin/financeiro/page.tsx`,
`src/Tuilow.Web/src/app/(dashboard)/admin/produtos/page.tsx`,
`src/Tuilow.Web/src/app/(dashboard)/meus-cursos/page.tsx`,
`src/Tuilow.Web/src/app/(dashboard)/cursos/[slug]/[lessonId]/page.tsx`,
`src/Tuilow.Web/src/app/canal/[handle]/page.tsx`.

## Próximos passos sugeridos

- Se quiser, posso recriar o restante das páginas listadas em "Fora do escopo" numa
  próxima rodada, seguindo o mesmo princípio (nunca inventar dado).
- Considerar criar os endpoints que faltam (atividade de mentorados, agenda de
  encontros, engajamento/risco de abandono, lista de mentorados) se essas telas do
  protótipo forem, de fato, prioridade de produto — aí sim dá para recriá-las com dado
  real.
- `dotnet build`/`npm run build` completos não foram executados neste ambiente (só
  `tsc`/`lint`, que já cobrem erros de tipo e de sintaxe) — recomendo um `npm run build`
  local antes do deploy, como checagem final.
