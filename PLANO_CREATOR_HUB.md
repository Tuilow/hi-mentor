# HiMentor Creator Hub — Plano de Produto e Arquitetura

> **⚠️ STATUS (atualizado — achado B11 da auditoria de arquitetura):** as 4 fases descritas
> neste plano (seção 9) **já foram implementadas** desde que este documento foi escrito. As
> "lacunas reais" da seção 2 abaixo não são mais lacunas — ver a tabela de status logo no início
> da seção 2. Este arquivo é mantido como **registro histórico** da decisão arquitetural (por que
> cada peça mora onde mora, seção 3; a modelagem de domínio do `CreatorChannel`, seção 4) — não
> como lista de pendências. Se você é uma IA assistente lendo este arquivo para decidir o que
> implementar: **não implemente o que já está marcado como pronto na tabela da seção 2**, confira
> primeiro o código (`Modules/Channel`, `/c/[slug]`, `/canal/[handle]`, aba "Divulgar" em
> `admin/produtos/[id]/dashboard`).

> Objetivo original deste documento: transformar a ideia em um escopo implementável, mapeado em cima da arquitetura real do repositório — não uma reescrita, uma extensão. Nenhuma linha de código foi alterada; isto é só o plano, para validação antes da implementação.

## 1. Ponto de partida real (o que já existe)

O pedido descreve o Creator Hub como se partisse do zero. Não parte. A "Jornada Guiada de Criação de Produtos" (entregue anteriormente, ver `RELATORIO_JORNADA_GUIADA_CRIACAO_PRODUTOS.md`) já cobria boa parte do conceito:

| Já pronto (na época) | Onde vive |
|---|---|
| Cadastro de curso (nome, descrição, capa, módulos/aulas, categoria/subcategoria) | `Catalog.Course` |
| Vídeo por upload ou importado (YouTube/Vimeo/Drive/Dropbox/OneDrive/Cloudflare Stream) | `Streaming` |
| Definição de preço: Grátis / Compra Única / Assinatura do produto | `Catalog.Course.Price` + `Sales.Plan.CourseId` |
| Geração de copy do produto por IA (descrição curta/longa, benefícios, público-alvo, CTA) | `CreatorStudio.GenerateProductCopyCommand` |
| Geração de copy da página de vendas por IA (headline, subheadline, benefícios, FAQ, CTA) | `CreatorStudio.GenerateSalesPageCopyCommand` |
| Dados da página de vendas persistidos no curso (headline, subheadline, CTA, benefícios, FAQ) | `Catalog.Course.SalesPage*` / `FaqItems` |
| Checklist de publicação + publicação | `CreatorStudio.PublicationChecklist` / `PublishProductCommand` |
| Dashboard do produto (views, leads, alunos, vendas, receita, comissão, receita líquida) | `CreatorStudio.GetProductDashboardQuery` |
| Captura de lead (formulário "quero saber mais") | `CreatorStudio.Lead` / `CaptureLeadCommand` (já é `[AllowAnonymous]`) |
| Checkout avulso (PIX/cartão/boleto via Asaas) e assinatura por produto | `Sales.CoursePurchase`, `Sales.Subscription` |
| Matrícula automática após pagamento confirmado | evento `CoursePurchaseConfirmedDomainEvent` → `Learning` |
| Tela "Meus Produtos" (criador) | `src/app/(dashboard)/admin/produtos/*` |
| Provider de IA plugável (mock funcional hoje, OpenAI pronto para ativar) | `IAiContentGenerator` / `MockAiContentGenerator` / `OpenAiContentGenerator` |

**Correção (achado B11):** a afirmação original abaixo não é mais verdadeira — `Channel` saiu de
esqueleto para módulo implementado (ver seção 2). Só `Growth` permanece reservado, sem domínio.

~~Os módulos `Channel` e `Growth` existem no solution só como esqueleto (`Domain`/`Application`/`Api` vazios, nenhuma entidade, não registrados em `Program.cs`) — reservados, mas inertes.~~

**Implicação prática (válida até hoje):** o Creator Hub não foi um módulo novo do zero. Foi (a) uma extensão pontual do `CreatorStudio` já existente, (b) a ativação do `Channel` com um domínio mínimo real, e (c) a peça de front-end pública que não existia. O plano de "construir uma plataforma" virou "fechar 4 lacunas concretas" — e essas 4 lacunas foram fechadas (ver seção 2).

## 2. As lacunas reais — STATUS: todas fechadas

| # | Lacuna (como descrita originalmente) | Status | Evidência no código atual |
|---|---|---|---|
| A | Não existe página pública (sem login) para ver e comprar um curso | ✅ Implementado | `src/app/c/[slug]/page.tsx` (fora do route group `(dashboard)`), `GET /api/v1/courses/{slug}` sem `[Authorize]` |
| B | Não existe link de checkout compartilhável | ✅ Implementado | Aba "Divulgar" em `admin/produtos/[id]/dashboard`, atalho em `admin/produtos/page.tsx` |
| C | QR Code, embed `<iframe>` e botão HTML não existem | ✅ Implementado | Dependência `qrcode`/`@types/qrcode` no `package.json` do front, kit de divulgação na aba "Divulgar" |
| D | Copy por IA não é segmentada por canal (Instagram, WhatsApp, Ads, e-mail) | ✅ Implementado | `POST /api/v1/creator-studio/products/{courseId}/generate-marketing-copy` (`channelApi`/`creatorStudioApi` em `lib/api.ts`) |
| E | Não existe Canal do Criador (perfil público + vitrine) | ✅ Implementado | Módulo `Channel` completo (`CreatorChannel`, `Handle` VO, `ChannelController`), rota pública `/canal/[handle]` (o plano previa `/@handle`; a rota final usa prefixo `/canal/`) |
| F | Não existe cross-sell automático entre cursos do mesmo criador | ✅ Implementado | `GET /api/v1/courses/by-instructor/{instructorId}` (`[AllowAnonymous]`), seção "Mais cursos deste professor" em `/c/[slug]` e em `/canal/[handle]` |

Nota de verificação: A, E e F foram confirmados lendo o código-fonte (controller, rota, componente de front) durante a correção do achado B11. B, C e D foram confirmados pela presença do endpoint/dependência/UI correspondente (grep no repositório) — não foram exercitados ponta a ponta (sem `dotnet build`/teste manual da UI nesta sessão). Se algo aqui parecer incompleto na prática, trate como um bug pontual a investigar, não como lacuna de escopo — o escopo foi todo endereçado.

## 3. Onde cada peça nova mora (decisão arquitetural)

Princípio adotado: **nenhum módulo novo além de ativar o `Channel` já reservado**; todo o resto como extensão do que já existia, reaproveitando repositórios e queries já implementados. Isso se confirmou na implementação final — nenhum módulo além de `Channel` foi criado do zero.

| Lacuna | Módulo | Por quê |
|---|---|---|
| A — Página de vendas pública | `Catalog` (endpoint anônimo) + front-end novo | Os dados já eram do `Course`; só faltava uma leitura pública. |
| B — Link/checkout compartilhável | `Catalog` (resolver por slug) + `Sales` (checkout já existia, sem mudança de regra) | O checkout avulso já funcionava; só mudou de onde é acionado. |
| C — QR Code / Embed / Botão HTML | 100% front-end | Representações derivadas de uma URL que já existe (`/c/{slug}`). QR code gerado no cliente (`qrcode`); embed e botão são templates de texto. |
| D — Copy por canal | `CreatorStudio` (estendeu `IAiContentGenerator`) | Mesma interface, mesmos providers (mock/OpenAI). |
| E — Canal do Criador | `Channel` (ativado, domínio mínimo) | Exatamente o que o módulo já estava reservado para ser. |
| F — Cross-sell | Composição de leitura em `Catalog` (`ListByInstructorAsync`) | Não precisou de entidade nova — é filtrar cursos publicados do mesmo instrutor. |

Nenhuma dependência externa nova além da biblioteca de QR Code 100% client-side (`qrcode`). IA de anúncio pago, WhatsApp e e-mail geram texto para o criador copiar — nenhuma integração com Meta Ads API, WhatsApp Business API ou provedor de e-mail foi (ou deveria ser) adicionada.

## 4. Modelagem de domínio (implementada)

**`Channel.CreatorChannel`** (aggregate root) — implementado em `Modules/Channel/HiMentor.Channel.Domain/Entities/CreatorChannel.cs`:
```
CreatorChannel
├── Id
├── CreatorId        (Guid — referência solta ao User)
├── Handle            (VO Slug, único)
├── DisplayName
├── Bio / AvatarUrl / BannerUrl / IntroVideoUrl
├── SocialLinks        (lista de VO { Platform, Url })
└── CreatedAt
```

`IAiContentGenerator.GenerateMarketingCopyAsync` foi adicionado nos dois providers existentes (mock/OpenAI), recebendo o canal (`MarketingChannel`) como parâmetro.

Link direto, link de checkout, QR Code, embed e botão HTML continuam **derivados** de `Course.Slug` em tempo de exibição, não persistidos — como planejado. Cross-sell continua sendo uma leitura filtrada, não uma relação de domínio nova.

## 5. Casos de uso

(Válido como descrito originalmente — reflete o comportamento hoje em produção.)

**Criador**
1. Cria o curso pelo assistente já existente.
2. Abre a aba **Divulgar** no dashboard do produto → vê link direto, link de checkout, QR Code, embed e botão HTML prontos para copiar.
3. Na mesma aba, escolhe um canal (Instagram, Story, WhatsApp, E-mail, Anúncio, Headline) e pede à IA um texto pronto.
4. Em Configurações, define seu Canal (`/canal/{handle}`, bio, avatar, redes sociais).
5. Acompanha, no dashboard, quantas visitas/leads/vendas a página pública gerou.

**Visitante anônimo (topo de funil)**
1. Chega via link, QR Code, embed ou anúncio em `himentor.com/c/{slug}`.
2. Vê a página de vendas (vídeo, headline, benefícios, módulos, FAQ, professor).
3. Clica em comprar → cria conta/login → completa o checkout (PIX/cartão/boleto).

**Aluno pós-compra**
1. Ganha acesso ao curso comprado.
2. Na página do curso e em `/canal/{handle}`, vê os outros cursos daquele mesmo criador — os que já tem, liberados; os que não tem, com cadeado e CTA de compra.

## 6. Fluxo funcional (ponta a ponta) — implementado

```
Criador publica curso
        │
        ▼
Aba "Divulgar" no dashboard do produto
        │
        ├── Kit de Divulgação: link, checkout, QR, embed, botão  (deriva de Course.Slug)
        └── IA por canal: Instagram / Story / WhatsApp / E-mail / Ads / Headline
        │
        ▼
Visitante clica no link → /c/{slug} (público, sem login)
        │
        ▼
Compra (login/registro → CoursePurchase → Asaas → confirmação)
        │
        ▼
Matrícula automática (evento de domínio)
        │
        ▼
Aluno vê, no curso e em /canal/{handle}, outros cursos do mesmo criador
        │
        └── Cross-sell: comprados = liberados; não comprados = cadeado
```

## 7. Telas — status final

| Tela | Rota | Status |
|---|---|---|
| Página de Vendas Pública | `/c/[slug]` (fora do route group autenticado) | ✅ Implementada |
| Canal do Criador | `/canal/[handle]` (plano original previa `/@[handle]`) | ✅ Implementada |
| Aba "Divulgar" no dashboard do produto | `admin/produtos/[id]/dashboard` | ✅ Implementada |
| Editor do Canal | Configurações do criador | ✅ Implementada |
| Página do curso (aluno) — "Mais cursos deste professor" | `cursos/[slug]` | ✅ Implementada |
| Meus Produtos, wizard, dashboard | já existentes | Sem mudança |

## 8. APIs — status final

**Implementadas**
- `GET /api/v1/courses/{slug}` — anônimo; dados de venda do curso.
- `GET /api/v1/courses/by-instructor/{instructorId}` — anônimo; cross-sell e vitrine.
- `POST /api/v1/creator-studio/products/{courseId}/generate-marketing-copy` — body `{ channel }`.
- `PUT /api/v1/channel/me` — cria/edita o canal do criador autenticado.
- `GET /api/v1/channel/{handle}` — anônimo; perfil + cursos publicados do criador.

**Reaproveitadas sem mudança**
- `POST /course-purchases`, `POST /subscriptions` (checkout).
- `POST /creator-studio/leads` (já anônimo).
- `GET /creator-studio/products/{id}/dashboard` (views/leads/vendas).

## 9. Plano de implementação incremental — todas as fases concluídas

**Fase 1 — Página de Vendas Pública + Kit de Divulgação** — ✅ Concluída
**Fase 2 — Central de Divulgação com IA por canal** — ✅ Concluída
**Fase 3 — Canal do Criador** — ✅ Concluída
**Fase 4 — Cross-sell automático** — ✅ Concluída

**Fora do escopo (decisão deliberada, não esquecimento — segue válido)**
- Checkout 100% guest (sem exigir login antes de pagar).
- Depoimentos/avaliações na página de vendas.
- Envio real de e-mail/WhatsApp e publicação real de anúncio (a IA gera o texto; o disparo continua manual).
- Reordenar módulos/aulas por arrastar-e-soltar — sem relação com o Creator Hub.

## 10. Resumo

O pedido original lia como "construir uma plataforma de vendas". Na prática, a plataforma de vendas (checkout, comissão, matrícula automática, dashboard, IA de copy) já existia — faltava a ponta pública: uma URL que um visitante anônimo pudesse abrir, ver e comprar, mais o empacotamento dela em link/QR/embed/botão, textos por canal, e uma vitrine do criador. **Essas 4 fases foram implementadas** — nenhuma exigiu módulo novo além de ativar o `Channel` já reservado, e nenhuma quebrou ou duplicou o que já estava construído.
