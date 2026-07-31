# Relatório de Validação - Correções M11, B11 e B12

**Data:** 31/07/2026
**Status:** Validado e ajustado (ver nota sobre build no final)

## M11 - Único ponto de acoplamento Application-to-Application entre módulos

Confirmado: `Tuilow.IdentidadeAcesso.Application.csproj` referenciava `Tuilow.Streaming.Application`
(não só `Streaming.Domain`), usado dentro de `DeleteUserCommandHandler` para apagar em cascata os
vídeos do criador excluído via `IStreamingService`. Era a única exceção, em todo o repositório, ao
padrão Domain-to-Domain entre módulos.

Validado em:

- `Modules/IdentidadeAcesso/Tuilow.IdentidadeAcesso.Application/Tuilow.IdentidadeAcesso.Application.csproj`
- `Modules/IdentidadeAcesso/Tuilow.IdentidadeAcesso.Application/Commands/DeleteUser/DeleteUserCommandHandler.cs`
- `Modules/IdentidadeAcesso/Tuilow.IdentidadeAcesso.Domain/Entities/User.cs`
- `Modules/Streaming/Tuilow.Streaming.Application/Tuilow.Streaming.Application.csproj`
- `Host/Tuilow.Host.Api/Data/AppDbContext.cs`

### Correção aplicada

A exclusão em cascata de vídeos virou um domain event, seguindo exatamente a recomendação do
relatório:

- `User.MarkDeleted()` (IdentidadeAcesso.Domain) agora levanta `UserDeletedDomainEvent(UserId)`
  (novo, em `Modules/IdentidadeAcesso/Tuilow.IdentidadeAcesso.Domain/Events/`).
- `UserDeletedEventHandler` (novo, em `Modules/Streaming/Tuilow.Streaming.Application/EventHandlers/`)
  reage a esse evento: busca os cursos do criador via `ICourseRepository.ListByInstructorAsync`
  (Catalog.Domain, já referenciado por Streaming.Application antes desta correção), lista os vídeos
  de cada curso via `IVideoRepository`, tenta apagar cada um no Cloudflare Stream (melhor esforço,
  com try/catch e log — mesmo padrão já usado em `DeleteVideoCommandHandler`) e remove o registro
  local. Roda depois que a exclusão da conta já foi persistida (ver `AppDbContext.SaveChangesAsync`:
  salva primeiro, despacha eventos depois), numa transação própria.
- `DeleteUserCommandHandler` (IdentidadeAcesso.Application) foi reescrito: não depende mais de
  `IVideoRepository`, `IStreamingService` nem `ILogger` — só `IUserRepository`, `ICourseRepository`
  (Catalog.Domain, para arquivar os cursos) e `IUnitOfWork`.
- `Tuilow.IdentidadeAcesso.Application.csproj` perdeu a referência a `Tuilow.Streaming.Application`.
  Manteve `Tuilow.Streaming.Domain` — `GetPlatformStatsQueryHandler` ainda usa `IVideoRepository`
  para contar vídeos no painel administrativo, e isso é Domain-to-Domain, dentro do padrão.
- `Tuilow.Streaming.Application.csproj` ganhou uma nova referência a
  `Tuilow.IdentidadeAcesso.Domain` — necessária para consumir `UserDeletedDomainEvent`. Isso não
  reabre o problema original: é Application-to-**Domain**, o mesmo tipo de referência que
  Streaming.Application já tinha com Catalog.Domain.

Depois da correção, `IdentidadeAcesso.Application` não referencia mais nenhuma `.Application` de
outro módulo — o acoplamento Application-to-Application citado no achado deixou de existir.

### Ajuste adicional considerado e descartado

A primeira versão desta correção levantava o evento de limpeza diretamente na entidade `Video`
sendo apagada (`Video.RequestCloudflareCleanup()`), no mesmo `SaveChanges` que já a excluía. Não
implementei essa versão: entidades apagadas fisicamente (`repository.Delete`) saem do
`ChangeTracker` assim que o `SaveChanges` daquela transação termina, e
`AppDbContext.DispatchDomainEventsAsync` varre `ChangeTracker.Entries<AggregateRoot>()` **depois**
de `base.SaveChangesAsync` — ou seja, um evento levantado na mesma entidade que está sendo apagada,
na mesma chamada, seria perdido silenciosamente. Por isso o evento final é levantado em `User`
(que só sofre soft-delete via `MarkDeleted()`, continua rastreado) e não em `Video`.

## B11 - Documentação desatualizada em relação ao código atual

Confirmado nos dois arquivos:

- `README.md` descrevia a arquitetura antiga de projeto único (`src/Tuilow.Domain/Application/Infrastructure/API`), incompatível com a estrutura modular atual.
- `PLANO_CREATOR_HUB.md`, seção 2, listava como lacunas coisas que já foram implementadas.

### Correção aplicada em README.md

Reescrevi a seção de Arquitetura (árvore de diretórios, tabela de status por módulo), o Início
Rápido (comandos `cd Host/Tuilow.Host.Api` em vez de `cd src/Tuilow.API`, migrations sem
`--startup-project` já que `AppDbContext` mora no próprio Host), a tabela de Stack e a seção de
Bounded Contexts, usando os fatos confirmados em `Program.cs` e `docker-compose.yml`:

- Módulos ativos no Host: IdentidadeAcesso, Catalog, Learning, Sales, Streaming, Finance, Payout, CreatorStudio, Channel.
- `Journey` — código completo, deliberadamente desligado (`AddJourneyModule()` comentado em `Program.cs`).
- `Growth` — ainda reservado, sem domínio implementado.

Também corrigi a tabela de Endpoints Principais: removi `/api/v1/learner-profiles` (endpoint do
módulo Journey, hoje desligado — deixar listado sem aviso induziria a um erro 404 real) e adicionei
os endpoints públicos que hoje sustentam `/c/[slug]` e `/canal/[handle]` (ver B12/achado adicional
abaixo).

### Correção aplicada em PLANO_CREATOR_HUB.md

Este arquivo planejava 4 lacunas (A: página pública, B: kit de divulgação, C: QR/embed/botão, D:
copy por canal, E: Canal do Criador, F: cross-sell). Ao investigar o código para escrever o novo
README, encontrei evidência de que **todas** já foram implementadas — não só A e E, como o
relatório original da auditoria citava:

| Lacuna | Evidência confirmada no código |
|---|---|
| A — Página pública | `src/app/c/[slug]/page.tsx` fora do route group `(dashboard)`; `GET /api/v1/courses/{slug}` sem `[Authorize]` |
| E — Canal do Criador | Módulo `Channel` implementado por completo (`CreatorChannel`, `Handle`, `ChannelController`, `GetPublicChannelQuery`), rota pública `/canal/[handle]` |
| F — Cross-sell | `GET /api/v1/courses/by-instructor/{instructorId}` (`[AllowAnonymous]`), seção "Mais cursos deste professor" renderizada em `/c/[slug]` e `/canal/[handle]` |
| B, C, D — Kit de divulgação, QR/embed/botão, copy por canal | Aba "Divulgar" referenciada em `admin/produtos/page.tsx` e no dashboard do produto; dependência `qrcode`/`@types/qrcode` no `package.json` do front; endpoint `POST /api/v1/creator-studio/products/{courseId}/generate-marketing-copy` em `lib/api.ts` |

A, E e F foram confirmados lendo o código-fonte de controller/rota/componente. B, C e D foram
confirmados pela presença do endpoint/dependência/referência de UI correspondente (grep no
repositório), sem exercitar a interface manualmente nesta sessão — ver nota de validação abaixo.

Como o achado B11 pedia "atualizar ou arquivar" o documento para não confundir quem (ou qual IA
assistente) o ler depois, adicionei um aviso de status logo no topo do arquivo, corrigi a
afirmação da seção 1 de que `Channel` era só esqueleto, e troquei a lista de lacunas da seção 2 por
uma tabela de status (✅ Implementado / evidência) — mantendo o resto do documento (seções 3 a 10)
como registro histórico da decisão arquitetural, já que essa parte descreve corretamente por que
cada peça foi construída onde foi construída.

## B12 - Resíduo de nome legado do rebranding anterior

Confirmado: `tests/DogMaster.Domain.Tests` existe ao lado de `tests/Tuilow.Application.Tests`.

### Achado adicional descoberto durante a validação

Ao inspecionar as duas pastas antes de mover qualquer coisa, encontrei uma situação mais extrema do
que o relatório original supunha:

- `tests/DogMaster.Domain.Tests` não tem `.csproj` nem nenhum arquivo `.cs` — só as pastas `bin/` e
  `obj/` com artefatos de uma build antiga. Não há nada de código para "perder".
- `tests/Tuilow.Application.Tests` — a suíte que o relatório original tratava como "a suíte atual"
  — está **completamente vazia** (nem `.csproj`).
- `Tuilow.sln` não referencia nenhum dos dois diretórios. Não há hoje nenhum projeto de testes
  automatizado ativo no solution.

Ou seja: não havia nada a comparar (a "suíte atual" citada como equivalente não existe de fato), e
`DogMaster.Domain.Tests` não continha nenhum código a preservar — só binários de build obsoletos.

### Correção aplicada

Como o ambiente de execução não permite apagar arquivos diretamente, movi a pasta para
`tests/_to_delete/DogMaster.Domain.Tests`. Ela contém só `bin/` e `obj/` (build cache antigo, sem
código-fonte). **Peço que você apague manualmente `tests/_to_delete/` no seu computador.**

Não mexi em `tests/Tuilow.Application.Tests` — o achado B12 falava só do diretório legado, e ele
está vazio mas ainda é a pasta reservada para a suíte de testes futura, não resíduo do rebranding.
Deixei uma nota sobre a ausência de testes ativos na seção de Arquitetura do `README.md` atualizado
(achado B11), para isso não passar despercebido.

## Validação executada

O `dotnet` SDK disponível neste ambiente de execução é a versão 8, e o repositório está em
`net9.0` — não consegui rodar `dotnet build Tuilow.sln` nesta sessão (mesma limitação já registrada
no relatório anterior, `RELATORIO_CORRECOES_UX_B2_B10.md`). A validação do M11 foi feita por leitura
cruzada de assinatura de método e referência de projeto:

- Toda interface/método usado em `UserDeletedEventHandler` (`ICourseRepository.ListByInstructorAsync`,
  `IVideoRepository.ListByCourseAsync`/`.Delete`, `IStreamingService.DeleteVideoAsync`,
  `IUnitOfWork.SaveChangesAsync`) já existia com a mesma assinatura em algum outro handler do
  repositório (`DeleteUserCommandHandler` original, `DeleteVideoCommandHandler`).
- Confirmei, antes de adicionar a referência `Streaming.Application → IdentidadeAcesso.Domain`, que
  `Tuilow.IdentidadeAcesso.Domain.csproj` só referencia `SharedKernel.Domain` — sem risco de
  dependência circular.
- `UserDeletedEventHandler` fica em `Tuilow.Streaming.Application/EventHandlers/`, dentro do
  assembly que `AddStreamingModule()` já registra via `RegisterServicesFromAssembly` — não precisa
  de registro manual de DI.

Para B11 e B12, a validação foi por leitura direta do código (controllers, rotas do front,
`Tuilow.sln`, conteúdo real das pastas de teste) — não é uma questão de compilação, e sim de
conferir o que existe hoje contra o que os documentos afirmavam.

## Conclusão

Os três achados do relatório de arquitetura procediam. M11 foi corrigido movendo a exclusão em
cascata de vídeos para um domain event consumido pelo próprio módulo Streaming, eliminando a única
referência Application-to-Application entre módulos do repositório. B11 foi corrigido atualizando
`README.md` para refletir a estrutura modular real e sinalizando em `PLANO_CREATOR_HUB.md` que as 4
fases do plano já foram implementadas (não só as 2 lacunas que a auditoria original citava
como resolvidas — as 6 estão). B12 foi corrigido movendo o diretório legado para
`tests/_to_delete/` (aguardando remoção manual), com a ressalva de que a "suíte atual" citada como
equivalente está vazia e fora do `Tuilow.sln`.
