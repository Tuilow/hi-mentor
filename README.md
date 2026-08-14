# 🎓 HiMentor — Plataforma de Cursos Online

Uma plataforma moderna onde pessoas, profissionais, especialistas e empresas podem criar, publicar, vender e
gerenciar cursos online. Backend em **monolito modular (.NET 9)** com DDD, CQRS e Domain Events; frontend em
**Next.js 15**.

---

## 🏗️ Arquitetura

O backend é um **monolito modular**: cada módulo de negócio tem suas próprias camadas Domain/Application/
Infrastructure/Api, mas todos são hospedados por um único processo ASP.NET Core (`Host/HiMentor.Host.Api`) — sem
a complexidade operacional de microsserviços, mas com o desacoplamento de módulos independentes. A regra entre
módulos é referenciar só a camada `Domain` uns dos outros (nunca `Application`); comunicação que não é uma
simples leitura de dado passa por Domain Events (MediatR), despachados pelo `AppDbContext` depois que a
transação já foi salva.

```
HiMentor/
├── Host/
│   └── HiMentor.Host.Api/            # Processo único: Program.cs compõe os módulos, AppDbContext, migrations
├── SharedKernel/
│   ├── HiMentor.SharedKernel.Domain/         # AggregateRoot, IDomainEvent, VOs compartilhados
│   ├── HiMentor.SharedKernel.Application/    # ICurrentUserService, IUnitOfWork, IRepository
│   └── HiMentor.SharedKernel.Infrastructure/
├── Modules/
│   ├── IdentidadeAcesso/    # Usuários, autenticação, roles, Magic Link
│   ├── Catalog/             # Cursos, módulos, aulas, categorias
│   ├── Learning/            # Matrículas, progresso, certificados
│   ├── Sales/               # Checkout (Asaas), assinaturas, compra avulsa
│   ├── Streaming/           # Vídeos no Cloudflare Stream (+ YouTube/Vimeo/Drive embutidos)
│   ├── Finance/             # Carteira do criador, comissão da plataforma
│   ├── Payout/              # Saques do criador
│   ├── CreatorStudio/       # Jornada guiada de criação de produto (orquestra os módulos acima)
│   ├── Channel/             # Canal do Criador — vitrine pública em /canal/{handle}
│   ├── Journey/             # "Meus Perfis" — código intacto, desligado do pipeline (ver Program.cs)
│   └── Growth/              # Reservado — ainda stub, sem domínio implementado
│       └── {Domain,Application,Infrastructure,Api}/   # cada módulo segue essa mesma estrutura interna
├── src/
│   └── HiMentor.Web/          # Frontend Next.js 15 + React 19 + Tailwind
├── tests/
│   └── HiMentor.Application.Tests/   # Projeto reservado para testes automatizados (ver nota abaixo)
├── scripts/
│   └── init-db.sql                # Criação dos schemas PostgreSQL
├── docker-compose.yml
├── .env.example
└── .gitignore
```

Cada módulo em `Modules/` segue a mesma estrutura interna: `HiMentor.<Módulo>.Domain`, `.Application`,
`.Infrastructure` e `.Api` — quatro `.csproj` próprios, referenciados pelo Host.

**Status de cada módulo** (ver `Program.cs` para a lista viva):

| Módulo | Status |
|---|---|
| IdentidadeAcesso, Catalog, Learning, Sales, Streaming, Finance, Payout, CreatorStudio, Channel | Ativos, registrados no Host |
| Journey | Código completo (entidades, tabelas), **desligado deliberadamente** do pipeline — `AddJourneyModule()` comentado em `Program.cs` |
| Growth | Reservado — ainda sem domínio implementado, não registrado no Host |

> Nota sobre `tests/`: não há atualmente um projeto de testes automatizados ativo referenciado em
> `HiMentor.sln`. `HiMentor.Application.Tests` existe como pasta reservada. Um diretório legado do nome anterior
> à marca (`DogMaster.Domain.Tests`, sem `.csproj` nem código-fonte — só artefatos de build antigos) foi
> movido para `tests/_to_delete/` para remoção manual.

## 🚀 Início Rápido

### Pré-requisitos
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Node.js 20+](https://nodejs.org)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)

### 1. Configurar variáveis de ambiente

```bash
cp .env.example .env
# Edite .env com suas chaves: Asaas, Cloudflare, JWT Secret, etc.
```

### 2. Subir com Docker Compose

```bash
docker-compose up -d
```

Isso sobe: **API (porta 5000)**, **Frontend (porta 3000)**, **PostgreSQL (5432)**, **Redis (6379)**.

### 3. Desenvolvimento local (sem Docker)

**Backend:**
```bash
cd Host/HiMentor.Host.Api
dotnet run
# API disponível em http://localhost:5000 (ou a porta configurada no launchSettings/appsettings)
# Swagger: http://localhost:5000/swagger
```

**Frontend:**
```bash
cd src/HiMentor.Web
npm install
cp .env.local.example .env.local
npm run dev
# App disponível em http://localhost:3000
```

### 4. Migrations EF Core

O `AppDbContext` e as migrations vivem no próprio Host (ele referencia o `Domain`/`Infrastructure` de cada
módulo via `ApplyConfigurationsFromAssembly`), então não é preciso indicar um `--startup-project` separado:

```bash
cd Host/HiMentor.Host.Api
dotnet ef migrations add NomeDaMigration
dotnet ef database update
```

### 5. Testes

Não há hoje uma suíte de testes automatizados ativa no `HiMentor.sln` (ver nota na seção Arquitetura acima).

---

## 📦 Stack Tecnológica

| Camada        | Tecnologia                                          |
|---------------|-----------------------------------------------------|
| Backend       | ASP.NET Core 9, C# 13, EF Core 9, PostgreSQL 16     |
| Padrões       | Monolito Modular, DDD, CQRS, SOLID, Domain Events   |
| Mediação      | MediatR 12, FluentValidation 11                     |
| Autenticação  | JWT (15min) + Refresh Token (30d) + Google OAuth + Magic Link |
| Pagamentos    | Asaas (PIX, Cartão, Boleto) com webhooks            |
| Streaming     | Cloudflare Stream (upload direto + playback assinado) + embed YouTube/Vimeo/Drive |
| Frontend      | Next.js 15, React 19, TypeScript, Tailwind CSS      |
| State         | TanStack Query v5                                    |
| Containers    | Docker, Docker Compose                              |
| Documentação  | Swagger/OpenAPI                                     |

---

## 🔑 Módulos (Bounded Contexts)

- **IdentidadeAcesso** — Usuários, autenticação (senha, Google, Magic Link), roles/permissões
- **Catalog** — Cursos, módulos, aulas, categorias, página de vendas pública (`/c/{slug}`)
- **Learning** — Matrículas, progresso de aulas, certificados
- **Sales** — Checkout avulso e por assinatura (Asaas), planos
- **Streaming** — Vídeos no Cloudflare Stream, importação de YouTube/Vimeo/Drive
- **Finance** — Carteira do criador, cálculo de comissão da plataforma
- **Payout** — Solicitação e histórico de saques do criador
- **CreatorStudio** — Jornada guiada de criação de produto (orquestra Catalog/Sales/Learning/Finance), geração de copy por IA, dashboard do produto
- **Channel** — Canal do Criador: vitrine pública em `/canal/{handle}` com os cursos publicados
- **Journey** — "Meus Perfis"; módulo completo, desligado do pipeline por decisão de produto
- **Growth** — Reservado para funcionalidades futuras de aquisição/retenção; ainda sem domínio implementado

---

## 🔒 Segurança

- Senhas: BCrypt (workFactor 12)
- Tokens JWT: HMAC-SHA256, expiração 15 min
- Refresh Tokens: rotação automática (revoga token anterior)
- Magic Link: acesso sem senha para contas criadas via checkout anônimo
- Webhooks Asaas: validação do token `asaas-access-token` configurado no painel da Asaas
- Vídeos Cloudflare: URLs de playback assinadas com expiração

---

## 📡 Endpoints Principais

| Método | Rota                                  | Descrição                    |
|--------|---------------------------------------|-------------------------------|
| POST   | /api/v1/auth/register                 | Cadastro de usuário           |
| POST   | /api/v1/auth/login                    | Login com e-mail e senha      |
| POST   | /api/v1/auth/google                   | Login com Google              |
| POST   | /api/v1/auth/refresh-token            | Renovar access token          |
| GET    | /api/v1/auth/me                       | Perfil do usuário logado      |
| GET    | /api/v1/courses                       | Listar cursos publicados      |
| GET    | /api/v1/courses/{slug}                | Detalhes do curso (público — alimenta `/c/{slug}`) |
| GET    | /api/v1/courses/by-instructor/{instructorId} | Outros cursos do mesmo criador (público, cross-sell) |
| POST   | /api/v1/enrollments                   | Matricular em curso           |
| POST   | /api/v1/enrollments/free              | Matrícula em curso grátis, sem login (Magic Link) |
| POST   | /api/v1/enrollments/{id}/progress     | Registrar progresso           |
| GET    | /api/v1/course-subscription-plans     | Listar planos de assinatura por curso |
| POST   | /api/v1/subscriptions                 | Assinar plano                 |
| GET    | /api/v1/channel/{handle}              | Canal público do criador (`/canal/{handle}`) |
| PUT    | /api/v1/channel/me                    | Criar/editar o próprio canal (criador autenticado) |
| POST   | /api/v1/webhooks/asaas                | Webhook de pagamentos Asaas   |

Documentação interativa completa: **http://localhost:5000/swagger**
