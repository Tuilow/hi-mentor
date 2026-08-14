# Relatório — Renomeação Tuilow → HiMentor

## Contexto
O nome do projeto mudou de **Tuilow** para **HiMentor**. Este relatório documenta a renomeação aplicada à solution e ao código, o que foi deixado de fora de propósito, e como aplicar as mudanças no repositório local.

## Escopo aplicado (identificadores de código)
Substituição de `Tuilow`/`TUILOW`/`tuilow` por `HiMentor`/`HIMENTOR`/`himentor` em:

- **Solution**: `Tuilow.sln` → `HiMentor.sln` (46 projetos, todas as `Project(...)` entries e paths).
- **Pastas/projetos** (55 pastas/arquivos de topo renomeados, ~101 caminhos no total incluindo subpastas):
  - `SharedKernel/Tuilow.SharedKernel.*` → `HiMentor.SharedKernel.*`
  - `Modules/*/Tuilow.<Módulo>.{Domain,Application,Infrastructure,Api}` → `HiMentor.<Módulo>.*` (11 módulos: IdentidadeAcesso, Catalog, Learning, Journey, Sales, Channel, Growth, Streaming, Finance, Payout, CreatorStudio)
  - `Host/Tuilow.Host.Api` → `Host/HiMentor.Host.Api`
  - `tests/Tuilow.Finance.Tests` → `tests/HiMentor.Finance.Tests` (e as pastas reservadas vazias `Tuilow.Application.Tests`/`Tuilow.Domain.Tests`)
  - `src/Tuilow.Web` → `src/HiMentor.Web` (frontend Next.js)
  - `src/Tuilow.API`, `.Application`, `.Domain`, `.Infrastructure` (backend legado, não referenciado no `.sln`) → renomeados também por consistência, embora sejam código morto.
- **Namespaces e `RootNamespace`** em todos os `.csproj` e arquivos `.cs` (verificado: 0 ocorrências de `Tuilow` restantes em declarações `namespace`).
- **`ProjectReference`** em todos os `.csproj` e entradas do `.sln` — todos os 113 `ProjectReference` + 46 entradas do `.sln` foram verificados apontando para arquivos existentes após o rename.
- **`UserSecretsId`** (`tuilow-api-secrets` → `himentor-api-secrets`) — se você usa `dotnet user-secrets` localmente, vai precisar re-configurar os secrets sob o novo Id.
- **Frontend**: `package.json`/`package-lock.json` (`"name": "tuilow-web"` → `"himentor-web"`), textos de UI que mostravam "Tuilow" como marca (headers, footer, `page.tsx`) → "HiMentor", comentários de código.
- **README.md** e **PLANO_CREATOR_HUB.md** — reescritos para refletir os novos caminhos/namespaces.
- **docker-compose.yml** — só os 2 caminhos estruturais que precisavam acompanhar o rename de pasta:
  - `dockerfile: Host/Tuilow.Host.Api/Dockerfile` → `Host/HiMentor.Host.Api/Dockerfile`
  - `context: ./src/Tuilow.Web` → `./src/HiMentor.Web`

## Deixado de propósito sem alterar (fora do escopo "identificadores de código")
- **`appsettings.json`** (`Host/HiMentor.Host.Api/appsettings.json`): `Jwt:Issuer`/`Jwt:Audience` = `"Tuilow"`, nome do banco local `tuilow_modular_dev`, e-mails reais (`rodrigo.leite@tuilow.com.br`, `admin@tuilow.com.br`) e `FromName`/`LastName` = `"Tuilow"`. São valores de configuração/dados, não identificadores de código — e os e-mails dependem do domínio `tuilow.com.br` continuar existindo ou não.
- **`docker-compose.yml`**: nomes de imagem (`tuilow-api`, `tuilow-web`), `container_name`, rede (`tuilow_net`), nome do banco Postgres (`tuilow`) e os valores `Jwt__Issuer=Tuilow`/`Jwt__Audience=Tuilow` — são rótulos de infraestrutura local, não exigidos para o build funcionar.

Se quiser que eu troque isso também (inclusive os e-mails, uma vez que vocês decidam o domínio novo), é só pedir.

## Validação feita
- **Estrutura**: todos os `ProjectReference` (113) e entradas do `.sln` (46) resolvem para arquivos existentes; todos os `.csproj` (46) continuam XML válido; `Project(`/`EndProject` do `.sln` balanceados (61/61); nenhuma declaração `namespace` com `Tuilow` residual.
- **Frontend**: `npx tsc --noEmit` limpo (zero erros) e `next lint` só acusou 2 avisos/erros pré-existentes, sem relação com o rename (uso de `<img>` em vez de `next/image`, aspas não escapadas numa linha que já existia antes — conferido comparando com o arquivo original).
- **Backend**: **não foi possível rodar `dotnet build`/`dotnet restore`** neste ambiente — o sandbox de nuvem não tem acesso ao NuGet (bloqueio de rede conhecido deste ambiente, não específico deste rename). A validação do backend foi manual: XML válido, `RootNamespace` = pasta em todos os 46 `.csproj`, todas as referências de projeto resolvidas, e nenhuma ocorrência de `Tuilow` restante fora dos arquivos citados acima. Recomendo abrir a solution no Visual Studio e rodar um build completo antes de fazer commit.

## Como aplicar no seu repositório
Os arquivos `himentor_renamed.zip` (o código já renomeado) e `aplicar_rename_himentor.ps1` foram enviados no chat e também colocados em `_staging_tmp/` na pasta do projeto. Para aplicar:

1. Baixe/copie os dois arquivos para a raiz do repositório (`C:\Tuilow\TuilowCurso`), ou rode o script direto de `_staging_tmp` passando `-RepoRoot`.
2. Feche o Visual Studio (evita arquivo em uso).
3. Rode no PowerShell, a partir da raiz do repo:
   ```powershell
   .\_staging_tmp\aplicar_rename_himentor.ps1 -RepoRoot "C:\Tuilow\TuilowCurso" -ZipPath "C:\Tuilow\TuilowCurso\_staging_tmp\himentor_renamed.zip"
   ```
   O script apaga as 55 pastas/arquivos antigos com "Tuilow" no nome e extrai o zip renomeado por cima (não mexe em `.git`, `bin`, `obj`, `node_modules`).
4. Abra `HiMentor.sln`, rode **Restore NuGet** e confirme que builda.
5. `cd src\HiMentor.Web && npm install` e confirme que o frontend builda (`npm run build`).
6. `git add -A` e `git status` — confira que o Git detectou como **rename** (não delete+add) na maioria dos arquivos; isso preserva o histórico.
7. Revise o diff, teste localmente, e faça commit + push.

Não consegui aplicar isso direto no seu repositório porque (a) o acesso GitHub via token não está habilitado nesta sessão para este repo, e (b) a ponte com seu computador não tem permissão para apagar arquivos — só copiar/sobrescrever. Por isso o passo de apagar as pastas antigas precisa ser feito por você (ou pelo script acima, que roda direto na sua máquina).
