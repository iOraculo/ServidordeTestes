#Requires -Version 5.1
<#
.SYNOPSIS
    Instala palette.grf no cliente de testes (C:\AureumRO\Froggo2022) e atualiza DATA.INI
    para liberar o Modo Expandido (0..553) do Estilista sem erro CPaletteRes.
#>
param(
    [string]$SourcePaletteGrf = "",
    [string]$ClientDir = ""
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($ClientDir)) {
    $unified = Join-Path $repoRoot "client\Froggo2022"
    if (Test-Path $unified) { $ClientDir = $unified } else { $ClientDir = "C:\AureumRO\Froggo2022" }
}

if ([string]::IsNullOrWhiteSpace($SourcePaletteGrf)) {
    $candidates = @(
        "C:\AureumRO\ClienteTestes\palette.grf",
        "$env:USERPROFILE\Downloads\AureumRO\palette.grf",
        "C:\AureumRO\AureumRO\palette.grf"
    )
    foreach ($c in $candidates) {
        if (Test-Path $c) {
            $SourcePaletteGrf = $c
            break
        }
    }
}

while ([string]::IsNullOrWhiteSpace($SourcePaletteGrf) -or -not (Test-Path $SourcePaletteGrf)) {
    $SourcePaletteGrf = (Read-Host "Informe o caminho completo do arquivo palette.grf").Trim('"').Trim()
}

if (-not (Test-Path $ClientDir)) {
    Write-Host "[ERRO] Pasta do cliente nao encontrada: $ClientDir" -ForegroundColor Red
    exit 1
}

Copy-Item -Path $SourcePaletteGrf -Destination (Join-Path $ClientDir "palette.grf") -Force

@"
[Data]
0=palette.grf
1=ROTP20220406.grf
2=official_data.grf
3=data.grf
"@ | Set-Content -Path (Join-Path $ClientDir "DATA.INI") -Encoding ASCII

Write-Host "[OK] palette.grf instalado em $ClientDir e DATA.INI atualizado!" -ForegroundColor Green
Write-Host "Reinicie o jogo para usar o Modo Expandido (0..553) no Estilista AureumRO." -ForegroundColor Cyan
