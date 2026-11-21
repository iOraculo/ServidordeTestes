#Requires -Version 5.1
<#
.SYNOPSIS
    Restaura um arquivo de backup SQL criado por backup-db.ps1 no container aureumro-db.
#>
param(
    [Parameter(Mandatory = $true)]
    [string]$BackupFile
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $BackupFile)) {
    Write-Host "[ERRO] Arquivo de backup nao encontrado: $BackupFile" -ForegroundColor Red
    exit 1
}

Write-Host "[Restore] Restaurando '$BackupFile' no banco 'ragnarok'..." -ForegroundColor Yellow
Get-Content -Path $BackupFile -Raw | docker exec -i aureumro-db mariadb -uragnarok -paureumro_db_pass ragnarok
if ($LASTEXITCODE -ne 0) {
    Write-Host "[ERRO] Falha ao restaurar backup." -ForegroundColor Red
    exit 1
}

Write-Host "[OK] Banco restaurado com sucesso! Reiniciando container rathena..." -ForegroundColor Green
docker compose -f (Join-Path $PSScriptRoot "..\docker-compose.yml") restart rathena
