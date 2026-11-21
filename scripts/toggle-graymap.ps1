#Requires -Version 5.1
<#
.SYNOPSIS
    Liga ou desliga chaopreto.grf (Graymap / chao limpo para WoE/PvP) no cliente Froggo2022.
.EXAMPLE
    .\scripts\toggle-graymap.ps1 on
.EXAMPLE
    .\scripts\toggle-graymap.ps1 off
#>
param(
    [ValidateSet("on", "off")]
    [string]$Mode = "on",
    [string]$ClientDir = "",
    [string]$SourceGraymapGrf = ""
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($ClientDir)) {
    $unified = Join-Path $repoRoot "client\Froggo2022"
    if (Test-Path $unified) { $ClientDir = $unified } else { $ClientDir = "C:\AureumRO\Froggo2022" }
}

if (-not (Test-Path $ClientDir)) {
    Write-Host "[ERRO] Pasta do cliente nao encontrada: $ClientDir" -ForegroundColor Red
    exit 1
}

$dataIni = Join-Path $ClientDir "DATA.INI"
$dstGrf = Join-Path $ClientDir "chaopreto.grf"

if ($Mode -eq "on") {
    if (-not (Test-Path $dstGrf)) {
        if ([string]::IsNullOrWhiteSpace($SourceGraymapGrf)) {
            $candidates = @(
                "C:\AureumRO\ClienteTestes\chaopreto.grf",
                "$env:USERPROFILE\Downloads\AureumRO\chaopreto.grf",
                "C:\AureumRO\AureumRO\chaopreto.grf"
            )
            foreach ($c in $candidates) {
                if (Test-Path $c) { $SourceGraymapGrf = $c; break }
            }
        }
        while ([string]::IsNullOrWhiteSpace($SourceGraymapGrf) -or -not (Test-Path $SourceGraymapGrf)) {
            $SourceGraymapGrf = (Read-Host "Informe o caminho de chaopreto.grf").Trim('"').Trim()
        }
        Copy-Item -Path $SourceGraymapGrf -Destination $dstGrf -Force
    }
}

$grfs = @()
if ($Mode -eq "on" -and (Test-Path $dstGrf)) { $grfs += "chaopreto.grf" }
if (Test-Path (Join-Path $ClientDir "palette.grf")) { $grfs += "palette.grf" }
if (Test-Path (Join-Path $ClientDir "ROTP20220406.grf")) { $grfs += "ROTP20220406.grf" }
if (Test-Path (Join-Path $ClientDir "official_data.grf")) { $grfs += "official_data.grf" }
$grfs += "data.grf"

$lines = @("[Data]")
for ($i = 0; $i -lt $grfs.Count; $i++) {
    $lines += "$i=$($grfs[$i])"
}
$lines | Set-Content -Path $dataIni -Encoding ASCII

Write-Host "[OK] Graymap (chaopreto.grf): $Mode" -ForegroundColor Green
Write-Host "DATA.INI atualizado:" -ForegroundColor Cyan
$lines | ForEach-Object { Write-Host "  $_" -ForegroundColor DarkGray }
