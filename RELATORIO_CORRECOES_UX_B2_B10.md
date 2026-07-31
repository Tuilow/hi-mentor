# Relatório de Correções - Auditoria de UX (item 9)

**Data:** 30/07/2026
**Status:** B2 e B10 (2ª parte) corrigidos e validados no front-end; M4, M6, M7/A4 e B10 (1ª parte) já estavam corrigidos numa rodada anterior — confirmados nesta revisão, sem alteração.

## Situação encontrada

Dos 5 pontos listados no item 9 (UX) da auditoria, uma leitura do código mostrou que **M4, M6, M7/A4 e a 1ª parte de B10** já tinham sido corrigidos numa sessão anterior (comentários `Achado M4/M6/M7/B10 da avaliação` presentes no código, sem relatório de validação registrado até agora). Só restavam pendentes:

- **B2** — curso grátis exigia cadastro completo (nome, sobrenome, e-mail, senha, confirmar senha, confirmação de e-mail) antes de matricular, enquanto o curso pago tinha checkout anônimo embutido na página (só nome/e-mail/CPF).
- **B10 (2ª parte)** — vídeos importados via iframe (YouTube/Vimeo/Google Drive) não salvavam nem retomavam progresso; só a 1ª parte (perda de progresso ao fechar a aba abruptamente, para vídeo nativo) tinha sido corrigida.

Esta rodada corrige os dois pontos pendentes e confirma os demais.

## B2 - Fricção assimétrica entre curso grátis e curso pago (CORRIGIDO)

Novo endpoint de matrícula anônima em curso grátis, espelhando o checkout anônimo já existente para curso pago (mesma conta localizada/criada por e-mail, sem senha, acesso por Magic Link):

- `Modules/Learning/Tuilow.Learning.Application/Interfaces/IUserProvisioningService.cs` (novo)
- `Modules/Learning/Tuilow.Learning.Infrastructure/Services/IdentidadeAcessoUserProvisioningService.cs` (novo)
- `Modules/Learning/Tuilow.Learning.Application/Commands/EnrollFreeCourseAnonymous/EnrollFreeCourseAnonymousCommand.cs` (novo)
- `Modules/Learning/Tuilow.Learning.Application/Commands/EnrollFreeCourseAnonymous/EnrollFreeCourseAnonymousCommandHandler.cs` (novo)
- `Modules/Learning/Tuilow.Learning.Infrastructure/DependencyInjection.cs` (registro do novo serviço)
- `Modules/Learning/Tuilow.Learning.Api/Controllers/EnrollmentsController.cs` (`POST /api/v1/enrollments/free`, `[AllowAnonymous]`)
- `src/Tuilow.Web/src/lib/api.ts` (`enrollmentsApi.enrollFreeAnonymous`)
- `src/Tuilow.Web/src/app/c/[slug]/page.tsx` (`FreeEnrollModal` — nome/e-mail, embutido na página, sem navegação)

O handler reaplica exatamente as mesmas regras de negócio do `EnrollStudentCommandHandler` (curso realmente grátis = `IsFree` **e** sem Plan de assinatura ativo — evita que o endpoint vire atalho pra pular pagamento de curso de assinatura) e do checkout anônimo de Sales (localizar/criar conta por e-mail, Magic Link, idempotência em matrícula repetida).

No front-end, o CTA "Quero começar agora" do curso grátis não manda mais o visitante para `/registro`: abre um modal embutido (mesmo padrão visual do `CheckoutModal` do curso pago), pedindo só nome e e-mail — menos campos que o pago, já que não há CPF/telefone envolvidos. Quem já está logado continua indo direto (matrícula automática existente, sem modal).

## B10 (2ª parte) - Sem resumo de progresso em vídeo YouTube/Vimeo (CORRIGIDO)

- `src/Tuilow.Web/src/app/(dashboard)/cursos/[slug]/[lessonId]/page.tsx`

YouTube e Vimeo têm API de controle de embed via `postMessage` (YouTube IFrame API / Vimeo Player.js), carregada sob demanda (sem adicionar dependência npm nova — mesmo padrão de script solto usado pelas duas plataformas). O player de embed agora:

- retoma a aula de onde parou (`seekTo`/`setCurrentTime`), igual ao `<video>` nativo;
- salva progresso periodicamente durante a reprodução e imediatamente em pause/fim;
- no YouTube, também participa do "salvar ao fechar a aba abruptamente" (`getCurrentTime`/`getDuration` são síncronos ali, sem round-trip de rede) — o `flushOnExit` existente foi refatorado (função `sendBeacon` extraída) para cobrir os dois casos sem duplicar código.

**Google Drive continua sem suporte** — o preview embutido do Drive não expõe nenhuma API pública de progresso/seek, então em vez de fingir que funciona, a tela agora mostra um aviso explícito ("sua posição não pode ser salva automaticamente por aqui") abaixo do player.

## M4, M6, M7/A4, B10 (1ª parte) - Já corrigidos, confirmados sem alteração

Revisão de código confirmou as correções já presentes:

- **M4** — `src/Tuilow.Web/src/middleware.ts`: guard de rota agora roda no servidor (Next.js Middleware), checando a presença do cookie HttpOnly `refresh_token` antes de servir qualquer HTML protegido — elimina o flash de conteúdo.
- **M6** — `clientCapturedAt` viaja em todo `POST /enrollments/{id}/progress`, permitindo o backend distinguir evento atrasado de evento realmente mais novo entre dispositivos.
- **M7/A4** — dashboard renomeou "Certificados conquistados" para "Cursos concluídos" (métrica correta, rótulo que não existia mais prometia documento); certificado real passou a ser emitido (`CourseCompletedEventHandler`) e verificável em `/certificado/[code]`.
- **B10 (1ª parte)** — `visibilitychange`/`pagehide` com `fetch(..., { keepalive: true })` cobrem perda de progresso ao fechar a aba sem pausar o vídeo nativo.

## Validação executada

**Front-end** (`src/Tuilow.Web`): `npx tsc --noEmit` (strict mode) e `npx next lint` rodados contra o projeto completo com os 3 arquivos alterados — 0 erros de tipo, 0 warnings novos (só os `<img>`/`no-img-element` pré-existentes em arquivos não tocados nesta rodada).

**Back-end** (`Tuilow.sln`): **não foi possível rodar `dotnet build`** neste ambiente — nem o container da sessão nem a ponte para esta máquina têm o SDK do .NET 9 instalado, e a instalação via `apt`/`dotnet-install.sh` não teve acesso de rede aqui. O código novo foi conferido manualmente linha a linha contra os handlers já existentes e compilando (`PurchaseCourseCommandHandler`, `EnrollStudentCommandHandler`, `IdentidadeAcessoUserProvisioningService` de Sales) — mesmos namespaces, mesmas assinaturas de interface, mesmos padrões de idempotência — mas isso não substitui um build real. **Recomendo rodar `dotnet build Tuilow.sln` localmente antes de subir esta correção.**

## Conclusão

B2 e a 2ª parte de B10 — os dois pontos pendentes da auditoria de UX — foram corrigidos com implementação real (não só rótulo/copy), seguindo os mesmos padrões arquiteturais já usados no restante do módulo Learning/Sales. Os demais achados do item 9 já estavam corrigidos e foram confirmados nesta revisão. Pendência única: validar o build do back-end (`dotnet build Tuilow.sln`) num ambiente com o SDK do .NET 9 antes de considerar esta rodada encerrada.
