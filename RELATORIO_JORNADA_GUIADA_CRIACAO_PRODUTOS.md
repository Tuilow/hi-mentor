# Tuilow — Jornada Guiada de Criação de Produtos Digitais

Relatório de entrega da funcionalidade que permite a qualquer criador (mesmo não-técnico) publicar um curso completo em menos de 15 minutos, com máximo reaproveitamento da arquitetura já existente.

## 1. Módulos alterados vs. módulo novo

| Módulo | O que mudou |
|---|---|
| **Catalog** (estendido) | `Course` ganhou Categoria/Subcategoria/Tipo/Views/Página de Vendas/FAQ; novo status `InReview`; novos comandos (editar info básica, definir preço, página de vendas, arquivar, duplicar, excluir, anexar material, registrar view) |
| **Streaming** (estendido) | `Video` ganha origem externa (YouTube/Vimeo/Cloudflare Stream/Drive/Dropbox/OneDrive); novo `IMediaImportService`; checagem de acesso ao vídeo corrigida para reconhecer compra avulsa e assinatura por produto |
| **Sales** (estendido) | `Plan` ganha vínculo opcional a um curso (assinatura por produto); ciclo `Semestral` adicionado; novo comando para o criador definir o plano de assinatura do seu produto |
| **Learning** (estendido) | Checagem de acesso a curso agora cobre os 3 modelos de monetização; novo contador de alunos por curso |
| **CreatorStudio** (**novo**) | Orquestra tudo acima: "Meus Produtos", geração de copy por IA, checklist de publicação, dashboard do produto, captura de leads |

Nenhuma funcionalidade existente foi removida. `Channel` e `Growth` seguem como stub, fora do escopo.

## 2. Novas entidades

- `Catalog.CourseFaqItem` — item de FAQ da página de vendas
- `Streaming.VideoSource` (enum) — Upload/YouTube/Vimeo/CloudflareStream/GoogleDrive/Dropbox/OneDrive
- `Catalog.ProductType` (enum) — Course/Ebook/Bundle (hoje só Course é suportado de ponta a ponta)
- `CreatorStudio.Lead` — contato capturado na página de vendas pública

Campos novos em entidades existentes: `Course` (Category, Subcategory, ProductType, ViewCount, SalesPageHeadline/Subheadline/CtaText, SalesPageBenefits, FaqItems), `Video` (Source, ExternalUrl, ExternalId, Title), `Plan` (CourseId nulável).

## 3. Novos endpoints (resumo)

**Catalog** — `PATCH /courses/{id}`, `PUT /courses/{id}/price`, `PUT /courses/{id}/sales-page`, `POST /courses/{id}/archive`, `POST /courses/{id}/duplicate`, `DELETE /courses/{id}`, `POST /courses/{slug}/view`, `POST /courses/{id}/modules/{m}/lessons/{l}/attachments`, `POST /materials/upload`, `GET /materials/{nome}`

**Streaming** — `POST /videos/import`

**Sales** — `GET/POST /subscriptions/plans/by-course/{courseId}`

**CreatorStudio** — `GET /creator-studio/my-products`, `POST /creator-studio/generate-product-copy`, `POST /creator-studio/products/{id}/generate-sales-page-copy`, `GET /creator-studio/products/{id}/publication-checklist`, `POST /creator-studio/products/{id}/publish`, `GET /creator-studio/products/{id}/dashboard`, `POST /creator-studio/leads`

## 4. Integrações externas (reais, sem exigir chave)

- **YouTube/Vimeo oEmbed** — busca título/thumbnail (Vimeo também duração) de verdade, sem autenticação
- **Google Drive/Dropbox/OneDrive/Cloudflare Stream** — reconhecimento da plataforma pela URL, guarda a referência (best-effort, sem oEmbed público)
- **Geração de conteúdo por IA** — arquitetura pronta (`IAiContentGenerator`) com mock funcional hoje (`MockAiContentGenerator`, gera copy de verdade por template) e implementação real pronta para plugar (`OpenAiContentGenerator`, protocolo chat-completions — OpenAI ou compatível), ativada só configurando `AiContentGenerator:MockMode=false` + `ApiKey`

## 5. Fluxo financeiro e de conteúdo — visão de ponta a ponta

1. Criador preenche Info Básica (com ajuda opcional da IA) → `Course` é criado em `Draft`
2. Envia ou importa vídeo(s) → `Video` fica `Ready` imediatamente (upload) ou nasce `Ready` (importação externa)
3. Organiza em Módulos → Aulas, vinculando os vídeos
4. Anexa materiais de apoio às aulas (armazenamento local, mesma filosofia do mock de vídeo)
5. Define preço: Grátis, Pagamento único (`CoursePurchase`) ou Assinatura do produto (`Plan` com `CourseId`)
6. Cria a página de vendas (manual ou sugestão de IA — a IA nunca substitui, só sugere)
7. Checklist revalida tudo no servidor → `Publicar Produto` → `Course.Publish()` (mesma regra já existente)

Acesso pago ao vídeo passa a checar, nesta ordem: compra avulsa confirmada → assinatura do produto ativa → assinatura legada da plataforma (compatibilidade, não removida).

## 6. Jornada do usuário (primeira vez)

Login → banner "Torne-se criador" (já existente) → menu **Meus Produtos** (novo, item principal do menu) → tela vazia com botão **+ Criar Produto** → assistente de 7 passos → produto publicado → dashboard com Views/Leads/Alunos/Vendas/Receita/Comissão/Receita líquida.

## 7. Máximo reaproveitamento — decisões explícitas

- Publicação: **reaproveita** `Course.Publish()` já existente (mesma validação de módulo/aula)
- Vincular vídeo à aula: **reaproveita** `LinkVideoToLessonCommand` sem nenhuma mudança de contrato
- Compra avulsa: **reaproveita 100%** o `CoursePurchase`/Asaas construído na Parte A
- Único módulo novo de fato: **CreatorStudio** — e mesmo ele não duplica nenhuma regra, só compõe dados de Catalog/Sales/Learning/Finance pelos repositórios já existentes

## 8. Limitações conhecidas (documentadas, não bloqueantes)

- Reordenar módulos/aulas por arrastar-e-soltar não foi implementado (endpoint de reordenação não existe no backend) — hoje a ordem é a de criação
- Receita de assinaturas por produto ainda não entra na soma do dashboard (só compra avulsa) — refinamento futuro
- Corrigidos durante a revisão: falha de segurança no download de materiais (path traversal), 3 endpoints que não checavam se o usuário era dono do curso antes de alterá-lo, e um bug de reconhecimento de domínio no importador de vídeo que permitiria uma URL forjada passar por YouTube/Vimeo

## 9. Execução necessária no ambiente do usuário

Como não há acesso a SDK .NET neste ambiente, é necessário rodar localmente:
```
dotnet ef migrations add JornadaCriacaoProdutos --project Host/Tuilow.Host.Api --startup-project Host/Tuilow.Host.Api
dotnet ef database update --project Host/Tuilow.Host.Api --startup-project Host/Tuilow.Host.Api
```
E no frontend: `npm install` (nenhuma dependência nova foi adicionada, mas confirme antes de rodar).
