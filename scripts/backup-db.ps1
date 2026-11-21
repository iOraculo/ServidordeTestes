#Requires -Version 5.1
<#
.SYNOPSIS
    Cria um snapshot SQL completo do banco MariaDB do AureumRO Servidor de Testes em backups/.
#>
param(
    [string]$BackupDir = ""
)

$ErrorActionPreference = "Stop"
if ([string]::IsNullOrWhiteSpace($BackupDir)) {
    $BackupDir = Join-Path $PSScriptRoot "..\backups"
}
New-Item -ItemType Directory -Path $BackupDir -Force | Out-Null

$stamp = Get-Date -Format "yyyyMMdd_HHmmss"
$outFile = Join-Path $BackupDir "aureumro_$stamp.sql"

Write-Host "[Backup] Exportando banco 'ragnarok' do container aureumro-db..." -ForegroundColor Yellow
$sql = docker exec aureumro-db mariadb-dump -uragnarok -paureumro_db_pass --single-transaction --routines --triggers ragnarok
if ($LASTEXITCODE -ne 0) {
    Write-Host "[ERRO] Falha ao executar mariadb-dump. Verifique se aureumro-db esta online." -ForegroundColor Red
    exit 1
}

$sql | Set-Content -Path $outFile -Encoding UTF8
Write-Host "[OK] Backup salvo em: $((Resolve-Path $outFile).Path)" -ForegroundColor Green
