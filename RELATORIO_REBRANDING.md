# Relatório de Rebranding — DogMasterPro → Tuilow

Data: 2026-07-01

## Resumo

Rebranding completo de **DogMasterPro** para **Tuilow**, com generalização do nicho pet/adestramento para uma plataforma genérica de cursos online, incluindo refatoração profunda do módulo de perfil de cão (`Dog`/`DogProfile`) para um conceito genérico de **Perfil de Aprendizado** (`LearnerProfile`), nova identidade visual (azul `#2563EB` + laranja `#F97316`) e novo posicionamento de marca. Toda a arquitetura, regras de negócio e fluxos existentes foram preservados; a única mudança de banco de dados foi a refatoração do módulo de perfil (autorizada explicitamente como opção de "refatoração completa").

---

## 1. Arquivos alterados

### Solução / infraestrutura de projeto
- `DogMasterPro.sln` → `Tuilow.sln`
- `DogMasterPro.slnLaunch.user` → `Tuilow.slnLaunch.user`
- `README.md`, `docker-compose.yml`, `.env.example`, `scripts/init-db.sql`

### Backend (renomeação de pastas/projetos)
- `src/DogMaster.API` → `src/Tuilow.API` (+ `.csproj`)
- `src/DogMaster.Application` → `src/Tuilow.Application` (+ `.csproj`)
- `src/DogMaster.Domain` → `src/Tuilow.Domain` (+ `.csproj`)
- `src/DogMaster.Infrastructure` → `src/Tuilow.Infrastructure` (+ `.csproj`)
- `tests/DogMaster.Application.Tests` → `tests/Tuilow.Application.Tests`
- `tests/DogMaster.Domain.Tests` → `tests/Tuilow.Domain.Tests` (+ `.csproj`)
- Todos os `appsettings*.json`, `Program.cs`, `Dockerfile` e demais `.cs` (206 arquivos) tiveram `DogMaster`/`dogmaster` substituído por `Tuilow`/`tuilow`.

### Frontend (`src/Tuilow.Web`)
- `package.json` (`dogmaster-web` → `tuilow-web`)
- `tailwind.config.ts` — nova paleta de cores
- `src/app/globals.css` — classes de botão/card/badge/gradiente reescritas para o tema claro
- `src/app/layout.tsx` — metadados (title/description)
- `src/app/page.tsx` — home page reescrita (conteúdo + visual)
- `src/app/(dashboard)/layout.tsx` — navegação, logo, cores
- `src/app/(dashboard)/dashboard/page.tsx` — reescrita (conteúdo + visual)
- `src/app/(dashboard)/perfis/page.tsx` — **era `cachorros/page.tsx`**, reescrita completa
- `src/app/(dashboard)/cursos/page.tsx`, `cursos/[slug]/page.tsx`, `cursos/[slug]/[lessonId]/page.tsx`, `admin/cursos/page.tsx`, `assinatura/page.tsx`, `(auth)/login/page.tsx`, `(auth)/registro/page.tsx` — textos de nicho removidos e paleta de cores convertida de tema escuro para claro
- `src/types/index.ts` — interface `Dog` → `LearnerProfile`
- `src/lib/api.ts` — `dogsApi` → `learnerProfilesApi`, rota `/dogs` → `/learner-profiles`

### Banco de dados
- `scripts/init-db.sql`: schema `dog_profile` → `learner_profile`; `ai_trainer` → `ai_tutor`
- Migration `20260701014704_InitialCreate.cs` (+ `.Designer.cs` e `ApplicationDbContextModelSnapshot.cs`): tabela `dogs` → `learner_profiles`; `DogObjectives` → `LearningGoals`; colunas `Breed`→`Category`, remoção de `Sex`, `WeightKg`, `IsNeutered` (sem equivalente genérico)

### Testes
- `CourseTests.cs`, `EnrollmentTests.cs`: dados de teste "Adestramento" → "Programação"

---

## 2. Componentes alterados

| Componente antigo | Componente novo |
|---|---|
| Entidade `Dog` | Entidade `LearnerProfile` |
| Entidade `DogObjective` | Entidade `LearningGoal` |
| Enum `DogLevel` (Puppy/Beginner/…) | Enum `ProficiencyLevel` (Beginner/Intermediate/Advanced/Expert) |
| `DogRegisteredDomainEvent` | `LearnerProfileRegisteredDomainEvent` |
| `IDogRepository` / `DogRepository` | `ILearnerProfileRepository` / `LearnerProfileRepository` |
| `DogConfiguration` (EF) | `LearnerProfileConfiguration` (EF) |
| `DogsController` (`/api/v1/dogs`) | `LearnerProfilesController` (`/api/v1/learner-profiles`) |
| `RegisterDogCommand`/Handler/Validator | `RegisterLearnerProfileCommand`/Handler/Validator |
| `GetUserDogsQuery`/Handler | `GetUserLearnerProfilesQuery`/Handler |
| Página `/cachorros` ("Meus Cães" 🐕) | Página `/perfis` ("Meus Perfis" 🎓) |
| `dogsApi` (frontend) | `learnerProfilesApi` (frontend) |

Campos descartados por não terem equivalente genérico em uma plataforma de cursos: `Breed` (generalizado para `Category`/"área de interesse"), `Sex`, `WeightKg`, `IsNeutered`.

---

## 3. Namespaces renomeados

- `DogMaster.API` → `Tuilow.API`
- `DogMaster.Application` → `Tuilow.Application`
- `DogMaster.Domain` → `Tuilow.Domain`
- `DogMaster.Infrastructure` → `Tuilow.Infrastructure`
- `DogMaster.Domain.Tests` → `Tuilow.Domain.Tests`
- `*.Contexts.DogProfile.*` → `*.Contexts.Profiles.*`

> Nota técnica: o bounded context foi nomeado `Profiles` (não `LearnerProfile`) propositalmente — como a entidade principal se chama `LearnerProfile`, usar o mesmo nome para a pasta/namespace do contexto geraria ambiguidade de nome em C# (namespace vs. tipo).

---

## 4. Ajustes visuais realizados

- **Paleta**: azul `#2563EB` (primário/confiança) e laranja `#F97316` (CTA/ofertas/badges) configurados em `tailwind.config.ts` (`brand-*` e `accent-*`).
- **Fundos**: branco `#FFFFFF` e cinza-claro `#F8FAFC`; texto `#1F2937` (títulos) e `#6B7280`/cinza-médio (corpo).
- **Botões**: `.btn-primary` (azul, texto branco), `.btn-secondary` (laranja, texto branco — CTA/ofertas), `.btn-ghost` (neutro, usado em ações de cancelar/secundárias que não são CTA).
- **Cards**: fundo branco, borda suave (`gray-200`), sombra discreta (`shadow-sm`), hover com destaque azul.
- **Toda a aplicação foi convertida de tema escuro (zinc-900/950) para tema claro**, incluindo home page, dashboard, cursos, assinatura, login/registro e perfis.
- **Home page**: título "Aprenda e Ensine com a Tuilow", subtítulo e botões "Explorar Cursos" / "Criar Meu Curso" conforme especificado.
- **E-mails transacionais** (`EmailService.cs`): cores dos botões alinhadas à nova paleta; emoji de cão removido.
- Nenhum arquivo de logo/ícone foi encontrado no projeto (pasta `public` vazia) — não havia imagens de marca para substituir.

---

## 5. Pontos que precisam de revisão manual

1. **A pasta raiz do projeto continua se chamando `DogMasterPro`** (`C:\curso\Curso\DogMasterPro`). Não foi renomeada porque o ambiente indicou arquivos bloqueados durante a sessão (sinal de IDE/servidor de desenvolvimento aberto no projeto). Recomendo fechar qualquer editor/processo aberto e renomear a pasta manualmente (ou usar o "rename" do Visual Studio) antes de reabrir a solução.
2. A pasta `.vs/` (cache do Visual Studio) ainda referencia o nome antigo internamente — é seguro apagá-la, ela é regerada automaticamente.
3. Diretórios vazios legados não puderam ser removidos por bloqueio de arquivo: `src/Tuilow.Web/src/components/dogs` e `src/Tuilow.Application/Contexts/Profiles/Commands/UpdateDog`. Podem ser apagados manualmente.
4. Como a refatoração do módulo de perfil (`Dog` → `LearnerProfile`) alterou tabela, schema e colunas, será necessário recriar o banco de desenvolvimento ou gerar/aplicar a migration atualizada (`dotnet ef database update`) — dados existentes na tabela antiga `dogs` não migram automaticamente.
5. **Não foi possível rodar `dotnet build` neste ambiente** (sem SDK .NET pré-instalado e sem acesso de rede para instalá-lo) nem uma verificação 100% confiável de `npm run build`/`tsc` (o sandbox apresentou uma dessincronia pontual entre o editor de arquivos e o terminal Linux, aparentemente por haver outro processo — IDE ou dev server — acessando o projeto durante a sessão). Recomendo fortemente rodar `dotnet build` e `npm run build` localmente antes de considerar o rebranding finalizado.
6. O domínio de e-mail usado como placeholder é `tuilow.com.br` (mesmo padrão do antigo `dogmasterpro.com.br`) — trocar pelo domínio real de produção.
7. Alguns acentos de cor decorativos (amarelo para "plano Expert", verde para estados de sucesso) foram mantidos como estão por serem semânticos (sucesso/destaque premium), não fazendo parte estritamente da paleta azul/laranja — não conflitam com a nova identidade, mas vale revisar se quiser 100% de aderência à paleta.
