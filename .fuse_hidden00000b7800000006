# 🐕 DogMaster Pro — Plataforma SaaS de Adestramento Canino

Sistema completo construído com **Clean Architecture, DDD, CQRS, ASP.NET Core 9 e Next.js 15**.

---

## 🏗️ Arquitetura

```
DogMasterPro/
├── src/
│   ├── DogMaster.Domain/          # Entidades, VOs, Eventos de Domínio
│   ├── DogMaster.Application/     # CQRS (Commands/Queries), Behaviors, Interfaces
│   ├── DogMaster.Infrastructure/  # EF Core, JWT, Asaas, Cloudflare, Repositórios
│   └── DogMaster.API/             # Controllers REST, Middleware, Program.cs
│   └── DogMaster.Web/             # Frontend Next.js 15 + React 19 + Tailwind
├── tests/
│   └── DogMaster.Domain.Tests/    # Testes unitários xUnit + FluentAssertions
├── scripts/
│   └── init-db.sql                # Criação dos schemas PostgreSQL
├── docker-compose.yml
├── .env.example
└── .gitignore
```

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
cd src/DogMaster.API
dotnet run
# API disponível em https://localhost:7000
# Swagger: https://localhost:7000/swagger
```

**Frontend:**
```bash
cd src/DogMaster.Web
npm install
cp .env.local.example .env.local
npm run dev
# App disponível em http://localhost:3000
```

### 4. Migrations EF Core

```bash
cd src/DogMaster.Infrastructure
dotnet ef migrations add InitialCreate --startup-project ../DogMaster.API
dotnet ef database update --startup-project ../DogMaster.API
```

### 5. Rodar testes

```bash
dotnet test tests/DogMaster.Domain.Tests/
```

---

## 📦 Stack Tecnológica

| Camada        | Tecnologia                                          |
|---------------|-----------------------------------------------------|
| Backend       | ASP.NET Core 9, C# 13, EF Core 9, PostgreSQL 16    |
| Padrões       | Clean Architecture, DDD, CQRS, SOLID, Domain Events|
| Mediação      | MediatR 12, FluentValidation 11, AutoMapper 14      |
| Autenticação  | JWT (15min) + Refresh Token (30d) + Google OAuth    |
| Pagamentos    | Asaas (PIX, Cartão, Boleto) com webhooks            |
| Streaming     | Cloudflare Stream (upload direto + playback assinado)|
| Storage       | Cloudflare R2 (S3-compatible)                       |
| Frontend      | Next.js 15, React 19, TypeScript, Tailwind CSS 3   |
| State         | TanStack Query v5, Zustand 5                        |
| Containers    | Docker, Docker Compose                              |
| Testes        | xUnit, FluentAssertions, Bogus                      |
| Documentação  | Swagger/OpenAPI                                     |

---

## 🔑 Bounded Contexts (DDD)

- **Identity** — Usuários, autenticação, perfis sociais, refresh tokens
- **Catalog** — Cursos, módulos, aulas, exercícios, anexos
- **Learning** — Matrículas, progresso de aulas, certificados
- **Subscription** — Planos, assinaturas Asaas, histórico de pagamentos
- **DogProfile** — Perfil dos cães (raça, idade, objetivos)
- **Streaming** — Vídeos no Cloudflare Stream
- **Notifications** — Emails transacionais (MailKit)

---

## 🔒 Segurança

- Senhas: BCrypt (workFactor 12)
- Tokens JWT: HMAC-SHA256, expiração 15 min
- Refresh Tokens: rotação automática (revoga token anterior)
- Webhooks Asaas: validação de assinatura HMAC-SHA256
- Vídeos Cloudflare: URLs de playback assinadas com expiração

---

## 📡 Endpoints Principais

| Método | Rota                                  | Descrição                    |
|--------|---------------------------------------|------------------------------|
| POST   | /api/v1/auth/register                 | Cadastro de usuário          |
| POST   | /api/v1/auth/login                    | Login com e-mail e senha     |
| POST   | /api/v1/auth/google                   | Login com Google             |
| POST   | /api/v1/auth/refresh-token            | Renovar access token         |
| GET    | /api/v1/auth/me                       | Perfil do usuário logado     |
| GET    | /api/v1/courses                       | Listar cursos publicados     |
| GET    | /api/v1/courses/{slug}                | Detalhes do curso            |
| POST   | /api/v1/enrollments                   | Matricular em curso          |
| POST   | /api/v1/enrollments/{id}/progress     | Registrar progresso          |
| GET    | /api/v1/subscriptions/plans           | Listar planos disponíveis    |
| POST   | /api/v1/subscriptions                 | Assinar plano                |
| GET    | /api/v1/dogs                          | Listar meus cães             |
| POST   | /api/v1/dogs                          | Cadastrar cão                |
| POST   | /api/v1/webhooks/asaas                | Webhook de pagamentos Asaas  |

Documentação interativa completa: **http://localhost:5000/swagger**
