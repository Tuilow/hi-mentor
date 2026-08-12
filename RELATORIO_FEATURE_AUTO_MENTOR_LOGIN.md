# Relatório — Auto-promoção a mentor no login para quem não tem nenhum programa

**Data:** 12/08/2026
**Pedido do usuário:** "Assim que o cliente logar se a pessoa não tiver nenhum curso ela entra na parte automatica como mentor, não necessita entrar como aluno sem ter nada criar essa regra."

## Diagnóstico

Antes de mexer em qualquer coisa, investiguei onde a decisão de "pra onde o usuário vai depois do login" é tomada hoje. Achei um único ponto de decisão: a função `goHome()` dentro de `(auth)/login/page.tsx`, chamada tanto depois do login por e-mail/senha quanto depois do login com Google. Ela fazia só uma pergunta — "esse usuário já é Creator?" — e mandava pra `/admin/produtos` (Creator) ou `/dashboard` (todo o resto), sem olhar se a pessoa tinha algum programa.

Já existia, em `(dashboard)/dashboard/page.tsx`, um botão manual "Tornar-se mentor" (`handleBecomeCreator`) que chama `POST /auth/become-creator` — endpoint que já promove o usuário a Creator sem aprovação de Admin, de forma idempotente (não remove o papel de Aluno, plataforma multi-role), e devolve um token novo já com a claim de Creator. Ou seja: o backend já fazia exatamente o que faltava — só nunca era chamado automaticamente, dependia da pessoa ver a tela vazia e clicar.

Também confirmei que `middleware.ts` não tem nenhuma visão de papéis (só olha se existe cookie de sessão), então essa regra não podia morar ali — precisava ficar no mesmo lugar onde a decisão de destino já é tomada hoje: `goHome()`.

## O que foi feito

**`(auth)/login/page.tsx`** — 1 arquivo alterado:

1. Import de `enrollmentsApi` adicionado (já existe em `lib/api.ts`, reaproveitado sem criar nada novo — é o mesmo `GET /enrollments/me` que o dashboard do aluno já usa pra contar programas).
2. `goHome()` ganhou um passo novo, só quando o usuário **não** é Creator: busca a lista de matrículas (`enrollmentsApi.getMyEnrollments()`) e, se vier vazia, chama `authApi.becomeCreator()` — mesmo padrão exato do `handleBecomeCreator` do dashboard (grava o token novo com `setAccessToken`, sem precisar de `/auth/refresh-token` depois) — e manda a pessoa direto pra `/admin/produtos`, em vez de `/dashboard`.
3. Quem já tem pelo menos 1 programa continua caindo em `/dashboard`, sem nenhuma mudança de comportamento.
4. `returnUrl` (voltar pra uma página de vendas específica depois de logar) continua tendo prioridade sobre tudo isso — verificado antes de qualquer chamada nova, exatamente como já era.
5. Se `become-creator` falhar por algum motivo (rede, etc.), o login não trava: cai no fluxo normal e manda pra `/dashboard`.

Repetição de login não é um problema: quem já é Creator é identificado ANTES da checagem de matrículas e nunca chama `become-creator` de novo — a promoção só acontece uma vez, na primeira vez que a pessoa loga sem nenhum programa.

## Decisão de design que vale registrar

O botão manual "Tornar-se mentor" em `/dashboard` **não foi removido**. Ele continua funcionando como estava, e serve de rede de segurança pra dois casos que a regra automática não cobre: (1) se a chamada a `become-creator` falhar silenciosamente no login (ver item 5 acima) e (2) sessões que já estavam abertas antes desse deploy — a regra só roda no momento do login, então quem já está logado e sem programa nenhum só vira mentor automaticamente no próximo login, ou pode clicar no botão manual antes disso.

## Validação executada

Backend não foi tocado (o endpoint `POST /auth/become-creator` já existia e já fazia exatamente o necessário) — nenhuma mudança em C#, nenhum risco de build.

Frontend validado de verdade: reaproveitei o sandbox já montado em rodadas anteriores (`npm install` completo de `src/Tuilow.Web`), copiei o arquivo alterado e rodei `npx tsc --noEmit` (modo strict) — limpo, sem erros. Rodei `npx next lint --dir src` — limpo no arquivo alterado (os únicos achados são os mesmos de sempre, pré-existentes, em arquivos não tocados por esta mudança: aspas não escapadas em `admin/produtos/[id]/dashboard` e `<img>` sem `next/image` em outras páginas).

O arquivo entregue foi confirmado **byte-idêntico via SHA-256** rodado diretamente no disco do usuário (`sha256sum` no device, não só no container): `ac6e9e1c...438d2133a` nos dois lados.

## Conclusão

A partir de agora, qualquer pessoa que logar (e-mail/senha ou Google) e não tiver nenhum programa/matrícula ainda é automaticamente promovida a mentor e cai direto em "Meus Produtos" — sem precisar passar pela tela de aluno vazia nem clicar em nada. Quem já tem pelo menos um programa, ou já é Creator, ou está voltando pra uma página de checkout específica (`returnUrl`), continua com o comportamento de sempre.
