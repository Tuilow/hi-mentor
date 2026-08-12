# Relatório — Redirecionamento e aviso pós-pagamento (Asaas)

**Data:** 12/08/2026
**Pedido do usuário:** "assim que pagar dar um jeito de direcionar para a tela onde a pessoa loga para fazer a aula, ou redirecionar para o site, dar um aviso quando pagar, por favor verifique seu email para ter acesso ao curso — como fazer essa personalização já que o ambiente é do Asaas."

## Diagnóstico

Hoje, quando alguém compra um curso, o app cria a cobrança na Asaas e abre o link de pagamento (`invoiceUrl`) numa **nova aba** — a aba original do site continua aberta, mostrando "⏳ Aguardando confirmação do pagamento / Assim que o pagamento for confirmado, você recebe um Magic Link por e-mail". Isso já existia e continua existindo sem mudança.

O problema é a aba onde a pessoa efetivamente paga: ela é 100% da Asaas — depois de pagar, a Asaas mostra a própria tela de "pagamento recebido" dela, sem nenhum link de volta pro site nem aviso sobre o e-mail. É exatamente essa aba que você queria personalizar, e você está certo que o ambiente ali é da Asaas — o site não tem controle nenhum sobre o que é renderizado naquela página.

Pesquisei a documentação oficial da Asaas (link nas fontes abaixo) e confirmei que existe sim um mecanismo pra isso: ao criar a cobrança pela API, dá pra mandar um campo `callback` com `successUrl` (pra onde redirecionar) e `autoRedirect` (se o redirecionamento é automático ou só um botão). Duas restrições importantes:

1. **Só funciona pra Pix e cartão** (confirmação imediata). Boleto não redireciona sozinho — a compensação bancária leva dias, a pessoa já não está mais na tela quando confirma. Pra boleto, o aviso "verifique seu e-mail" continua vindo só pela aba original do site (como já era) e pelo e-mail em si.
2. **A URL de redirecionamento só é aceita se o domínio dela estiver cadastrado nos dados comerciais da conta Asaas que criou a cobrança** ("Configurações da conta → Informações", no painel da Asaas).

## O que foi feito

**3 arquivos** (2 backend + 1 frontend):

1. **`AsaasPaymentService.cs`** (modelo Legacy — cobrança na conta própria da Tuilow) e **`AsaasMarketplacePaymentService.cs`** (modelo atual — cobrança direto na subconta do mentor) — `CreateChargeAsync` (e `CreateSubscriptionAsync`, no modelo Legacy) agora enviam `callback: { successUrl: "<seu domínio>/pagamento-confirmado", autoRedirect: true }` pra Asaas.
2. **Nova página `/pagamento-confirmado`** no site — tela simples: "🎉 Pagamento confirmado!" + "Enviamos um e-mail com o link de acesso ao seu curso — confira sua caixa de entrada (e o spam)" + botão "Entrar" (ou "Ir para meus programas", se a pessoa já estiver logada no navegador) + link "Voltar para o site" + um link pra pedir um novo link de acesso caso o e-mail não chegue.

**Rede de segurança embutida no código:** como a restrição nº 2 acima depende de uma configuração manual na Asaas (ver abaixo) — e no modelo atual (subconta de cada mentor), é a subconta **do mentor**, não a sua, que precisaria ter esse domínio cadastrado, algo que eu não controlo por código —, os dois arquivos backend detectam se a Asaas rejeitou a cobrança especificamente por causa do `callback` e, nesse caso, **repetem a chamada sem esse campo automaticamente**, registrando um aviso no log. Ou seja: a compra nunca quebra por causa dessa personalização opcional — na pior hipótese, ela simplesmente não redireciona sozinha (fica como está hoje).

## O que só você consegue fazer (fora do código)

Isso é o "como fazer essa personalização" que você perguntou — a parte que realmente é do lado da Asaas:

1. Entre no painel da Asaas (**conta principal da Tuilow**, a que hoje processa as cobranças do modelo antigo/Legacy) → **Configurações da conta → Informações** → cadastre o domínio do site (o mesmo valor da variável `FrontendUrl` do backend, ex.: `app.tuilow.com.br`) nos dados comerciais da conta.
2. **Sobre as subcontas dos mentores** (modelo atual, onde a cobrança é criada na conta do próprio mentor): não tenho certeza se a Asaas aceita cadastrar o domínio da Tuilow na subconta de um terceiro, e não consigo testar isso sem uma subconta real em produção. Enquanto isso não estiver configurado (ou se a Asaas não permitir), essas cobranças específicas vão continuar caindo no fallback automático (sem redirecionamento próprio) — sem quebrar nada, só sem o "toque a mais" do redirecionamento. Se quiser, o próximo passo seria testar com uma subconta real e ver se a Asaas aceita; posso ajustar o código dependendo do resultado.

## Validação executada

Backend: `dotnet build` não roda neste sandbox (bloqueio de NuGet já conhecido) — validado por balanceamento de chaves/parênteses nos 2 arquivos C# (bateram) + revisão manual das assinaturas/DI (`IFrontendUrlProvider` já é singleton registrado globalmente no Host, usado por outro módulo — `ReissueCourseAccessLinkCommandHandler` — então a injeção nova nos dois serviços resolve sem precisar de nenhum registro adicional).

Frontend: `npx tsc --noEmit` (modo strict) e `npx next lint --dir src` limpos na página nova, no sandbox já montado em rodadas anteriores.

Os 3 arquivos entregues foram confirmados **byte-idênticos via SHA-256** direto no disco do seu computador.

## Conclusão

A partir de agora, quem paga por Pix ou cartão é redirecionado automaticamente de volta pro site com um aviso claro ("verifique seu e-mail para acessar o curso") em vez de ficar parado na tela genérica da Asaas — assim que você cadastrar o domínio nas configurações da conta principal da Asaas (passo manual, não é algo que eu consigo fazer por código). Quem paga por boleto continua recebendo o aviso normalmente pela aba original do site e pelo e-mail, como já era.

## Fontes consultadas
- [Redirecionamento após o pagamento — Asaas](https://docs.asaas.com/docs/redirecionamento-apos-o-pagamento)
- [Asaas Checkout](https://docs.asaas.com/docs/checkout-asaas)
