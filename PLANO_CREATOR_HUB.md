# Tuilow Creator Hub — Plano de Produto e Arquitetura

> Objetivo deste documento: transformar a ideia em um escopo implementável, mapeado em cima da arquitetura real do repositório — não uma reescrita, uma extensão. Nenhuma linha de código foi alterada; isto é só o plano, para validação antes da implementação.

## 1. Ponto de partida real (o que já existe)

O pedido descreve o Creator Hub como se partisse do zero. Não parte. A "Jornada Guiada de Criação de Produtos" (entregue anteriormente, ver `RELATORIO_JORNADA_GUIADA_CRIACAO_PRODUTOS.md`) já cobre boa parte do conceito:

| Já pronto | Onde vive |
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

Os módulos `Channel` e `Growth` existem no solution só como esqueleto (`Domain`/`Application`/`Api` vazios, nenhuma entidade, não registrados em `Program.cs`) — reservados, mas inertes.

**Implicação prática:** o Creator Hub não é um módulo novo do zero. É (a) uma extensão pontual do `CreatorStudio` já existente, (b) a ativação do `Channel` com um domínio mínimo real, e (c) uma peça de front-end pública que hoje não existe. Isso muda o plano de "construir uma plataforma" para "fechar 4 lacunas concretas".

## 2. As lacunas reais

Comparando o pedido item a item com o que existe, sobra isto:

**A. Não existe página pública (sem login) para um visitante ver e comprar um curso.** A página de curso hoje (`/cursos/[slug]`) vive dentro do route group `(dashboard)`, atrás de autenticação. Os dados de página de vendas (`SalesPageHeadline`, benefícios, FAQ) já existem no banco mas nunca são servidos para quem não está logado. Sem isso, nenhum link de divulgação tem para onde apontar.

**B. Não existe link de checkout compartilhável nem vídeo de vendas dedicado.** O "checkout" atual é: logar → abrir o curso → preencher formulário → ser redirecionado para o link de pagamento do Asaas (fora do domínio Tuilow). Não há uma URL curta e pública do tipo `tuilow.com/c/{slug}`.

**C. QR Code, embed `<iframe>` e botão HTML não existem.** Não há geração de nenhum dos três hoje.

**D. A geração de copy por IA é genérica (produto + página de vendas), não segmentada por canal.** Não existem variações prontas para Instagram, Story, WhatsApp, Meta Ads ou e-mail.

**E. Não existe Canal do Criador (perfil público + vitrine).** `Channel` está vazio.

**F. Não existe cross-sell automático entre cursos do mesmo criador.**

Isto é o escopo real do Creator Hub.

## 3. Onde cada peça nova mora (decisão arquitetural)

Princípio: **nenhum módulo novo além de ativar o `Channel` já reservado**; tudo o mais é extensão do que já existe, reaproveitando repositórios e queries já implementados.

| Lacuna | Módulo | Por quê |
|---|---|---|
| A — Página de vendas pública | `Catalog` (novo endpoint anônimo) + front-end novo | Os dados já são do `Course`; só falta uma leitura pública. Não é conceito novo, é visibilidade nova. |
| B — Link/checkout compartilhável | `Catalog` (resolver por slug) + `Sales` (checkout já existe, sem mudança de regra) | O checkout avulso já funciona; só muda de onde ele é acionado. |
| C — QR Code / Embed / Botão HTML | 100% front-end, nenhum back-end novo | São representações derivadas de uma URL que já existe (`/c/{slug}`). QR code gerado no cliente; embed e botão são só templates de texto. |
| D — Copy por canal | `CreatorStudio` (estende `IAiContentGenerator`) | Mesma interface, mesmos providers (mock/OpenAI), mesmo princípio "IA sugere, criador decide" já estabelecido — só mais um método. |
| E — Canal do Criador | `Channel` (ativado, domínio mínimo) | É exatamente o que esse módulo já foi reservado para ser. |
| F — Cross-sell | Composição de leitura em `Catalog` (`ListByInstructorAsync` já existe) | Não precisa de entidade nem de módulo — é filtrar cursos publicados do mesmo instrutor, método que já existe no repositório. |

Nenhuma dependência externa nova, exceto uma biblioteca de geração de QR Code 100% client-side (ex. `qrcode`, sem custo de infraestrutura, sem chamada de rede). IA de anúncio pago, WhatsApp e e-mail **geram texto para o criador copiar** — nenhuma integração com Meta Ads API, WhatsApp Business API ou provedor de e-mail é proposta agora: seria dependência externa desnecessária para o que foi pedido (o pedido é "gerar texto pronto", não "publicar/enviar automaticamente").

## 4. Modelagem de domínio

### Novo (mínimo)

**`Channel.CreatorChannel`** (aggregate root, novo — único conceito novo de fato):
```
CreatorChannel
├── Id
├── CreatorId        (Guid — referência solta ao User, mesmo padrão já usado entre módulos)
├── Handle            (VO Slug, único, ex.: "joaosilva")
├── DisplayName        (ex.: "João Silva Academy")
├── Bio
├── AvatarUrl
├── SocialLinks        (lista de VO { Platform, Url })
└── CreatedAt
```
Regra de domínio: `Handle` único (checado no repositório antes de criar, mesmo padrão de `SlugExistsAsync` em Catalog).

**`CreatorStudio.MarketingChannel`** (enum, não entidade):
```
InstagramPost, InstagramStory, WhatsApp, Email, MetaAds, Headline
```

### Estendido (campos/métodos novos em entidades existentes — nenhuma migração estrutural grande)

- `IAiContentGenerator` ganha `GenerateMarketingCopyAsync(courseId contexto, MarketingChannel canal)` → `MarketingCopySuggestion(string Content, string? Cta)`. Mesma interface, implementada nos dois providers já existentes.
- `Course` ganha (opcional) `SalesVideoUrl` — hoje o "vídeo de apresentação" do curso não tem um campo dedicado separado das aulas; reaproveita o mesmo `IMediaImportService` que já reconhece YouTube/Vimeo/upload.

### Nada novo precisa ser criado para:
- Link direto, link de checkout, QR Code, embed, botão HTML — são **derivados** de `Course.Slug` em tempo de exibição, não persistidos.
- Cross-sell — é uma leitura (`ListByInstructorAsync` filtrado por `Published`, excluindo o curso atual), não uma relação de domínio nova.

## 5. Casos de uso

**Criador**
1. Cria o curso pelo assistente já existente (inalterado).
2. Abre a aba **Divulgar** no dashboard do produto → vê link direto, link de checkout, QR Code, embed e botão HTML prontos para copiar.
3. Na mesma aba, escolhe um canal (Instagram, Story, WhatsApp, E-mail, Anúncio, Headline) e pede à IA um texto pronto — copia e usa onde quiser.
4. Em Configurações, define seu Canal (`@handle`, bio, avatar, redes sociais) uma única vez — vale para todos os cursos.
5. Acompanha, no dashboard já existente, quantas visitas/leads/vendas a página pública gerou.

**Visitante anônimo (topo de funil)**
1. Chega via link, QR Code, embed ou anúncio em `tuilow.com/c/{slug}`.
2. Vê a página de vendas (vídeo, headline, benefícios, módulos, FAQ, professor).
3. Clica em comprar → é levado a criar conta/login (reaproveitando fluxo de auth existente) → completa o checkout já existente (PIX/cartão/boleto).

**Aluno pós-compra**
1. Ganha acesso ao curso comprado (fluxo já existente, inalterado).
2. Na página do curso e em `/@{handle do professor}`, vê os outros cursos daquele mesmo criador — os que já tem, liberados; os que não tem, com cadeado e CTA de compra.

## 6. Fluxo funcional (ponta a ponta)

```
Criador publica curso (já existe)
        │
        ▼
Aba "Divulgar" no dashboard do produto (NOVO)
        │
        ├── Kit de Divulgação: link, checkout, QR, embed, botão  (deriva de Course.Slug)
        └── IA por canal: Instagram / Story / WhatsApp / E-mail / Ads / Headline (NOVO)
        │
        ▼
Visitante clica no link → /c/{slug} (NOVO, público, sem login)
        │
        ▼
Compra (fluxo já existente: login/registro → CoursePurchase → Asaas → confirmação)
        │
        ▼
Matrícula automática (já existente — evento de domínio)
        │
        ▼
Aluno vê, no curso e em /@{handle} (NOVO), outros cursos do mesmo criador
        │
        └── Cross-sell: comprados = liberados; não comprados = cadeado (NOVO, é composição de leitura)
```

## 7. Telas sugeridas

| Tela | Rota | Status |
|---|---|---|
| Página de Vendas Pública | `/c/[slug]` (fora do route group autenticado) | Nova |
| Canal do Criador | `/@[handle]` | Nova |
| Aba "Divulgar" no dashboard do produto | `admin/produtos/[id]/dashboard` (nova aba) | Nova |
| Editor do Canal | dentro de Configurações do criador | Nova |
| Página do curso (aluno) — seção "Mais cursos deste professor" | `cursos/[slug]` (já existe) | Extensão |
| Meus Produtos, wizard, dashboard | já existentes | Sem mudança |

## 8. APIs necessárias

**Novas**
- `GET /public/courses/{slug}` — anônimo; dados de venda do curso (dados que já existem em `Course`, só uma leitura sem exigir auth).
- `GET /public/courses/by-instructor/{instructorId}` — anônimo; cross-sell e vitrine do canal (filtra `ListByInstructorAsync` por `Published`).
- `POST /creator-studio/products/{courseId}/generate-marketing-copy` — body `{ channel }`, retorna texto pronto.
- `POST /channel/me` — cria/edita o canal do criador autenticado.
- `GET /channel/@{handle}` — anônimo; perfil + cursos publicados do criador.

**Reaproveitadas sem mudança**
- `POST /course-purchases`, `POST /subscriptions` (checkout).
- `POST /creator-studio/leads` (já anônimo).
- `GET /creator-studio/products/{id}/dashboard` (views/leads/vendas).

## 9. Plano de implementação incremental

**Fase 1 — Página de Vendas Pública + Kit de Divulgação**
Maior impacto, menor risco: sem página pública, nada mais no pedido funciona (não há para onde levar tráfego). Endpoint público em Catalog, rota `/c/[slug]`, e a aba "Divulgar" com link/checkout/QR/embed/botão (tudo derivado, sem dado novo).

**Fase 2 — Central de Divulgação com IA por canal**
Estende `IAiContentGenerator` com o método por canal e a UI de botões na mesma aba "Divulgar". Depende só da Fase 1 estar no ar (para os textos gerados linkarem pra algo real).

**Fase 3 — Canal do Criador**
Ativa `Channel` com o domínio mínimo (`CreatorChannel`), editor simples, rota pública `/@[handle]`.

**Fase 4 — Cross-sell automático**
Seção "Mais cursos deste professor" na página do curso e no Canal — leitura composta, sem domínio novo. Pode andar em paralelo com a Fase 3.

**Fora do escopo inicial (decisão deliberada, não esquecimento)**
- Checkout 100% guest (sem exigir login antes de pagar) — o MVP exige criar conta antes de comprar, reaproveitando 100% do auth já existente; guest checkout é uma mudança de regra de negócio maior e não foi pedido explicitamente.
- Depoimentos/avaliações na página de vendas — não existe entidade de review hoje; ficaria de fora até haver pedido explícito.
- Envio real de e-mail/WhatsApp e publicação real de anúncio — a IA gera o texto; o disparo continua manual, para não introduzir integrações externas (Meta Ads API, WhatsApp Business API, ESP) que o pedido não justifica.
- Reordenar módulos/aulas por arrastar-e-soltar — limitação já documentada, sem relação com o Creator Hub.

## 10. Resumo

O pedido original lê como "construir uma plataforma de vendas". Na prática, a plataforma de vendas (checkout, comissão, matrícula automática, dashboard, IA de copy) já existe — falta só a ponta pública: uma URL que um visitante anônimo possa abrir, ver e comprar, mais o empacotamento dela em link/QR/embed/botão, textos por canal, e uma vitrine do criador. Isso é 4 fases pequenas, nenhuma delas exige módulo novo além de ativar o `Channel` já reservado, e nenhuma quebra ou duplica o que já está construído.
