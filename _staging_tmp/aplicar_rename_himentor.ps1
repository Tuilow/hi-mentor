# aplicar_rename_himentor.ps1
# Aplica a renomeação Tuilow -> HiMentor no repositorio local.
# Rode este script a partir da RAIZ do repositorio (C:\Tuilow\TuilowCurso),
# ou passe -RepoRoot apontando pra ela.
#
# O que ele faz:
#   1) Apaga as pastas/arquivos antigos com "Tuilow" no nome (lista abaixo).
#   2) Extrai himentor_renamed.zip (que deve estar na mesma pasta deste script,
#      ou informe -ZipPath) por cima do repositorio, trazendo tudo já renomeado.
#   3) Mostra os proximos passos (dotnet restore, npm install, git add -A).
#
# NAO mexe no seu .git (historico preservado) nem em node_modules/bin/obj
# (o zip nao contem essas pastas).

param(
    [string]$RepoRoot = (Get-Location).Path,
    [string]$ZipPath = (Join-Path $PSScriptRoot "himentor_renamed.zip")
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $ZipPath)) {
    Write-Error "Nao encontrei $ZipPath. Baixe himentor_renamed.zip e coloque ao lado deste script, ou use -ZipPath."
    exit 1
}

Write-Host "Repo root: $RepoRoot"
Write-Host "Zip: $ZipPath"
Write-Host ""

$oldPaths = @(
    "Host\Tuilow.Host.Api",
    "Modules\Catalog\Tuilow.Catalog.Api",
    "Modules\Catalog\Tuilow.Catalog.Application",
    "Modules\Catalog\Tuilow.Catalog.Domain",
    "Modules\Catalog\Tuilow.Catalog.Infrastructure",
    "Modules\Channel\Tuilow.Channel.Api",
    "Modules\Channel\Tuilow.Channel.Application",
    "Modules\Channel\Tuilow.Channel.Domain",
    "Modules\Channel\Tuilow.Channel.Infrastructure",
    "Modules\CreatorStudio\Tuilow.CreatorStudio.Api",
    "Modules\CreatorStudio\Tuilow.CreatorStudio.Application",
    "Modules\CreatorStudio\Tuilow.CreatorStudio.Domain",
    "Modules\CreatorStudio\Tuilow.CreatorStudio.Infrastructure",
    "Modules\Finance\Tuilow.Finance.Api",
    "Modules\Finance\Tuilow.Finance.Application",
    "Modules\Finance\Tuilow.Finance.Domain",
    "Modules\Finance\Tuilow.Finance.Infrastructure",
    "Modules\Growth\Tuilow.Growth.Domain",
    "Modules\IdentidadeAcesso\Tuilow.IdentidadeAcesso.Api",
    "Modules\IdentidadeAcesso\Tuilow.IdentidadeAcesso.Application",
    "Modules\IdentidadeAcesso\Tuilow.IdentidadeAcesso.Domain",
    "Modules\IdentidadeAcesso\Tuilow.IdentidadeAcesso.Infrastructure",
    "Modules\Journey\Tuilow.Journey.Api",
    "Modules\Journey\Tuilow.Journey.Application",
    "Modules\Journey\Tuilow.Journey.Domain",
    "Modules\Journey\Tuilow.Journey.Infrastructure",
    "Modules\Learning\Tuilow.Learning.Api",
    "Modules\Learning\Tuilow.Learning.Application",
    "Modules\Learning\Tuilow.Learning.Domain",
    "Modules\Learning\Tuilow.Learning.Infrastructure",
    "Modules\Payout\Tuilow.Payout.Api",
    "Modules\Payout\Tuilow.Payout.Application",
    "Modules\Payout\Tuilow.Payout.Domain",
    "Modules\Payout\Tuilow.Payout.Infrastructure",
    "Modules\Sales\Tuilow.Sales.Api",
    "Modules\Sales\Tuilow.Sales.Application",
    "Modules\Sales\Tuilow.Sales.Domain",
    "Modules\Sales\Tuilow.Sales.Infrastructure",
    "Modules\Streaming\Tuilow.Streaming.Api",
    "Modules\Streaming\Tuilow.Streaming.Application",
    "Modules\Streaming\Tuilow.Streaming.Domain",
    "Modules\Streaming\Tuilow.Streaming.Infrastructure",
    "SharedKernel\Tuilow.SharedKernel.Application",
    "SharedKernel\Tuilow.SharedKernel.Domain",
    "SharedKernel\Tuilow.SharedKernel.Infrastructure",
    "Tuilow.sln",
    "Tuilow.slnLaunch.user",
    "src\Tuilow.API",
    "src\Tuilow.Application",
    "src\Tuilow.Domain",
    "src\Tuilow.Infrastructure",
    "src\Tuilow.Web",
    "tests\Tuilow.Application.Tests",
    "tests\Tuilow.Domain.Tests",
    "tests\Tuilow.Finance.Tests"
)

Write-Host "Apagando pastas/arquivos antigos (Tuilow.*)..." -ForegroundColor Yellow
foreach ($rel in $oldPaths) {
    $full = Join-Path $RepoRoot $rel
    if (Test-Path $full) {
        Remove-Item -LiteralPath $full -Recurse -Force
        Write-Host "  removido: $rel"
    }
}

Write-Host ""
Write-Host "Extraindo himentor_renamed.zip sobre o repositorio..." -ForegroundColor Yellow
Expand-Archive -LiteralPath $ZipPath -DestinationPath $RepoRoot -Force

Write-Host ""
Write-Host "Pronto. Proximos passos:" -ForegroundColor Green
Write-Host "  1) Abra HiMentor.sln (antigo Tuilow.sln) no Visual Studio e rode Restore NuGet."
Write-Host "  2) cd src\HiMentor.Web; npm install"
Write-Host "  3) git add -A; git status   -> confira que o git detectou como RENAME (nao delete+add)"
Write-Host "  4) Revise o diff, rode a app localmente, e faca commit + push."
Write-Host ""
Write-Host "Nao renomeados de proposito (ver relatorio): appsettings.json (Jwt Issuer/Audience, emails, nome do banco local) e docker-compose.yml (nomes de imagem/container/rede/banco)." -ForegroundColor Cyan
