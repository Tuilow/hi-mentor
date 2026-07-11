<#
.SYNOPSIS
    Remove o backend antigo do Tuilow (src/Tuilow.Domain, Tuilow.Application, Tuilow.Infrastructure,
    Tuilow.API) e o teste que aponta pra ele, agora que todos os mÃ³dulos de negÃ³cio com cÃ³digo real
    (IdentidadeAcesso, Catalog, Learning, Journey, Sales, Streaming) foram migrados e verificados
    rota a rota contra Modules/ + Host/.

.DESCRIPTION
    1. Confere que a nova estrutura (SharedKernel/, Modules/, Host/, Tuilow.Modular.sln) existe antes
       de apagar qualquer coisa â€” se nÃ£o existir, aborta sem tocar em nada.
    2. Pede confirmaÃ§Ã£o explÃ­cita (digitar "SIM") antes de deletar.
    3. Remove:
         - src\Tuilow.Domain
         - src\Tuilow.Application
         - src\Tuilow.Infrastructure
         - src\Tuilow.API
         - tests\Tuilow.Domain.Tests   (testava o Domain antigo â€” ver nota nas instruÃ§Ãµes abaixo)
         - Tuilow.sln                 (solution antiga, referenciava os 4 projetos acima)
         - Tuilow.slnLaunch.user      (perfil de launch da IDE pra solution antiga)
    4. Renomeia Tuilow.Modular.sln -> Tuilow.sln (deixa de ser "a solution de transiÃ§Ã£o" e vira A
       solution do projeto).
    5. NÃƒO toca em: src\Tuilow.Web (frontend Next.js), docker-compose.yml (jÃ¡ atualizado pro Host),
       scripts\, README.md, RELATORIO_REBRANDING.md.

.NOTES
    Rode a partir da raiz do projeto (C:\Tuilow\TuilowCurso), com PowerShell:
        .\remover_backend_antigo.ps1
    Feche o Visual Studio antes de rodar â€” arquivos abertos/travados pela IDE podem impedir a remoÃ§Ã£o.
#>

$ErrorActionPreference = 'Stop'

try {

# $PSScriptRoot sÃ³ Ã© preenchido quando o arquivo Ã© executado como script (.\arquivo.ps1).
# Se alguÃ©m colar o conteÃºdo direto no console (em vez de rodar o arquivo), ele fica vazio â€”
# nesse caso caÃ­mos pro diretÃ³rio atual.
$root = if ($PSScriptRoot) { $PSScriptRoot } else { (Get-Location).Path }
Write-Host "Raiz do projeto detectada: $root" -ForegroundColor DarkGray

function Assert-NewStructure {
    $required = @(
        (Join-Path $root 'SharedKernel'),
        (Join-Path $root 'Modules'),
        (Join-Path $root 'Host\Tuilow.Host.Api'),
        (Join-Path $root 'Tuilow.Modular.sln')
    )
    foreach ($path in $required) {
        if (-not (Test-Path $path)) {
            Write-Error "Abortando: '$path' nÃ£o existe. A nova estrutura modular precisa estar completa antes de remover a antiga."
            exit 1
        }
    }
}

function Remove-IfExists {
    param([string]$Path, [string]$Label)
    if (Test-Path $Path) {
        Write-Host "Removendo $Label ($Path)..." -ForegroundColor Yellow
        Remove-Item -LiteralPath $Path -Recurse -Force
    } else {
        Write-Host "$Label jÃ¡ nÃ£o existe ($Path) â€” pulando." -ForegroundColor DarkGray
    }
}

Write-Host "=== RemoÃ§Ã£o do backend antigo do Tuilow ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "Isto vai apagar PERMANENTEMENTE:"
Write-Host "  - src\Tuilow.Domain"
Write-Host "  - src\Tuilow.Application"
Write-Host "  - src\Tuilow.Infrastructure"
Write-Host "  - src\Tuilow.API"
Write-Host "  - tests\Tuilow.Domain.Tests"
Write-Host "  - Tuilow.sln (antiga)"
Write-Host "  - Tuilow.slnLaunch.user"
Write-Host ""
Write-Host "E vai renomear Tuilow.Modular.sln -> Tuilow.sln."
Write-Host ""
Write-Host "src\Tuilow.Web (frontend) NÃƒO Ã© tocado." -ForegroundColor Green
Write-Host ""

Assert-NewStructure

$confirm = Read-Host "Digite SIM para confirmar a remoÃ§Ã£o"
if ($confirm -ne 'SIM') {
    Write-Host "Cancelado. Nada foi removido." -ForegroundColor Red
    exit 0
}

Remove-IfExists -Path (Join-Path $root 'src\Tuilow.Domain')         -Label 'src\Tuilow.Domain'
Remove-IfExists -Path (Join-Path $root 'src\Tuilow.Application')    -Label 'src\Tuilow.Application'
Remove-IfExists -Path (Join-Path $root 'src\Tuilow.Infrastructure') -Label 'src\Tuilow.Infrastructure'
Remove-IfExists -Path (Join-Path $root 'src\Tuilow.API')            -Label 'src\Tuilow.API'
Remove-IfExists -Path (Join-Path $root 'tests\Tuilow.Domain.Tests') -Label 'tests\Tuilow.Domain.Tests'
Remove-IfExists -Path (Join-Path $root 'Tuilow.sln')                -Label 'Tuilow.sln (antiga)'
Remove-IfExists -Path (Join-Path $root 'Tuilow.slnLaunch.user')     -Label 'Tuilow.slnLaunch.user'

$modularSln = Join-Path $root 'Tuilow.Modular.sln'
$finalSln   = Join-Path $root 'Tuilow.sln'
if (Test-Path $modularSln) {
    Write-Host "Renomeando Tuilow.Modular.sln -> Tuilow.sln..." -ForegroundColor Yellow
    Rename-Item -LiteralPath $modularSln -NewName 'Tuilow.sln'
}

# src\ e tests\ podem ficar vazios (sÃ³ continham os projetos removidos) â€” remove se vazios.
foreach ($dir in @('src', 'tests')) {
    $path = Join-Path $root $dir
    if ((Test-Path $path) -and ((Get-ChildItem $path -Force | Measure-Object).Count -eq 0)) {
        Write-Host "Removendo pasta vazia $dir\..." -ForegroundColor DarkGray
        Remove-Item -LiteralPath $path -Force
    }
}

Write-Host ""
Write-Host "=== ConcluÃ­do ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "PrÃ³ximos passos:"
Write-Host "  1. Abra Tuilow.sln (era o Tuilow.Modular.sln) no Visual Studio."
Write-Host "  2. cd Host\Tuilow.Host.Api ; dotnet build"
Write-Host "  3. dotnet run  (aplica migrations e seed automaticamente em Development)"
Write-Host ""
Write-Host "Nota sobre o banco: a connection string do Host aponta pra 'tuilow_modular_dev' (appsettings.json)," -ForegroundColor Yellow
Write-Host "diferente do 'tuilow_dev' que o backend antigo usava. Se quiser reaproveitar os dados existentes," -ForegroundColor Yellow
Write-Host "renomeie o banco no Postgres ou ajuste ConnectionStrings:Default antes de rodar." -ForegroundColor Yellow
Write-Host ""
Write-Host "Nota sobre testes: tests\Tuilow.Domain.Tests foi removido porque testava o Domain antigo." -ForegroundColor Yellow
Write-Host "Os mÃ³dulos novos (Modules\*) ainda nÃ£o tÃªm testes â€” vale criar tests\Tuilow.<Modulo>.Tests" -ForegroundColor Yellow
Write-Host "por mÃ³dulo quando fizer sentido, seguindo o mesmo isolamento por camada." -ForegroundColor Yellow

}
catch {
    Write-Host ""
    Write-Host "=== ERRO ===" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    Write-Host ""
    Write-Host $_.ScriptStackTrace -ForegroundColor DarkGray
}
finally {
    Write-Host ""
    Read-Host "feche"
}

