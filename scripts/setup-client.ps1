#Requires -Version 5.1
<#
.SYNOPSIS
    Automatiza a montagem da pasta do cliente de testes (C:\AureumRO\ClienteTestes)
    para conectar ao AureumRO Servidor de Testes (rAthena Pré-Renewal 2022-04-06).

.DESCRIPTION
    1. Copia arquivos base (.grf, DATA.INI, BGM, System e DLLs padrão) de uma pasta
       Ragnarok existente sem alterar a instalação original e removendo DLLs de anti-cheat.
    2. Baixa e instala automaticamente data\clientinfo.xml a partir do Painel Web local.
    3. Cria o atalho/lançador "Jogar AureumRO Testes.bat".
    4. Verifica se o Login Server (127.0.0.1:6900) e o Painel Web (8080) estão online.

.EXAMPLE
    .\scripts\setup-client.ps1
.EXAMPLE
    .\scripts\setup-client.ps1 -SourceClientDir "C:\AureumRO\AureumRO" -TargetClientDir "C:\AureumRO\ClienteTestes"
#>
param(
    [string]$SourceClientDir = "",
    [string]$TargetClientDir = "C:\AureumRO\ClienteTestes",
    [string]$WebPanelUrl = "http://localhost:8080",
    [string]$RagexePath = ""
)

$ErrorActionPreference = "Stop"

function Write-Step([string]$msg) {
    Write-Host "`n[AureumRO-Setup] $msg" -ForegroundColor Yellow
}

function Write-Ok([string]$msg) {
    Write-Host "  [OK] $msg" -ForegroundColor Green
}

function Write-Warn([string]$msg) {
    Write-Host "  [AVISO] $msg" -ForegroundColor DarkYellow
}

Write-Host "============================================================" -ForegroundColor Yellow
Write-Host "  ✦ AureumRO — Assistente de Cliente de Testes (2022-04-06)" -ForegroundColor Yellow
Write-Host "============================================================" -ForegroundColor Yellow

# 1) Verifica se o servidor local está online
Write-Step "Verificando servidor local (127.0.0.1:6900)..."
$tcpTest = Test-NetConnection -ComputerName "127.0.0.1" -Port 6900 -WarningAction SilentlyContinue
if ($tcpTest.TcpTestSucceeded) {
    Write-Ok "Login Server respondendo em 127.0.0.1:6900."
} else {
    Write-Warn "Login Server não respondeu na porta 6900. Certifique-se de rodar 'docker compose up -d'."
}

# 2) Descobre ou solicita pasta de cliente RO existente (com .grf)
if ([string]::IsNullOrWhiteSpace($SourceClientDir)) {
    $candidates = @(
        "C:\AureumRO",
        "C:\Program Files (x86)\AureumRO",
        "C:\Program Files\AureumRO",
        "C:\Gravity\RO",
        "C:\Ragnarok",
        "$env:USERPROFILE\Desktop\AureumRO",
        "$env:USERPROFILE\Downloads\AureumRO"
    )
    foreach ($cand in $candidates) {
        if ((Test-Path $cand) -and ((Get-ChildItem -Path $cand -Filter "*.grf" -ErrorAction SilentlyContinue).Count -gt 0)) {
            $SourceClientDir = $cand
            Write-Ok "Pasta de cliente RO detectada automaticamente: $SourceClientDir"
            break
        }
    }
}

while ([string]::IsNullOrWhiteSpace($SourceClientDir) -or -not (Test-Path $SourceClientDir)) {
    Write-Host ""
    Write-Host "Informe o caminho da pasta onde você já tem um Ragnarok instalado (ex.: pasta do AureumRO com data.grf):" -ForegroundColor Cyan
    $SourceClientDir = (Read-Host "Caminho da pasta").Trim('"').Trim()
    if (-not (Test-Path $SourceClientDir)) {
        Write-Warn "Pasta não encontrada: $SourceClientDir"
    }
}

$grfFiles = Get-ChildItem -Path $SourceClientDir -Filter "*.grf" -File -ErrorAction SilentlyContinue
if ($grfFiles.Count -eq 0) {
    Write-Warn "Nenhum arquivo .grf encontrado em '$SourceClientDir'. Verifique se essa é mesmo a pasta do cliente."
}

# 3) Cria pasta de destino sem mexer na pasta original
Write-Step "Preparando pasta isolada em '$TargetClientDir' (sua pasta original NÃO será alterada)..."
New-Item -ItemType Directory -Path $TargetClientDir -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $TargetClientDir "data") -Force | Out-Null

$blockedPatterns = @(
    "*gepard*",
    "*odin*",
    "*anticheat*",
    "*aureumro*.exe",
    "*patcher*.exe",
    "*launcher*.exe"
)

function Test-BlockedFile([string]$fileName) {
    foreach ($pat in $blockedPatterns) {
        if ($fileName -like $pat) { return $true }
    }
    return $false
}

# Copia arquivos da raiz (.grf, .ini, .dll, .dat, etc.) filtrando anti-cheat e executáveis oficiais
$rootFiles = Get-ChildItem -Path $SourceClientDir -File -ErrorAction SilentlyContinue
foreach ($f in $rootFiles) {
    if ($f.Extension -ieq ".exe" -or (Test-BlockedFile $f.Name)) {
        continue
    }
    $dest = Join-Path $TargetClientDir $f.Name
    if (-not (Test-Path $dest)) {
        Write-Host "  Copiando $($f.Name)..." -ForegroundColor DarkGray
        Copy-Item -Path $f.FullName -Destination $dest -Force
    }
}
Write-Ok "Arquivos base (.grf, .dll, DATA.INI) copiados (anti-cheats ignorados)."

# Copia pastas essenciais (System, BGM, AI, savedata)
foreach ($folder in @("System", "BGM", "AI")) {
    $srcFolder = Join-Path $SourceClientDir $folder
    $dstFolder = Join-Path $TargetClientDir $folder
    if ((Test-Path $srcFolder) -and -not (Test-Path $dstFolder)) {
        Write-Host "  Copiando pasta $folder..." -ForegroundColor DarkGray
        Copy-Item -Path $srcFolder -Destination $dstFolder -Recurse -Force
    }
}
Write-Ok "Pastas auxiliares (System/BGM/AI) preparadas."

# 4) Instala data\clientinfo.xml diretamente do Painel Web (ou fallback local do repo)
Write-Step "Instalando data\clientinfo.xml..."
$clientInfoDest = Join-Path $TargetClientDir "data\clientinfo.xml"
$downloaded = $false
try {
    Invoke-WebRequest -Uri "$WebPanelUrl/api/clientinfo.xml" -OutFile $clientInfoDest -UseBasicParsing -TimeoutSec 5
    Write-Ok "clientinfo.xml baixado do Painel Web ($WebPanelUrl/api/clientinfo.xml)."
    $downloaded = $true
} catch {
    $repoClientInfo = Join-Path $PSScriptRoot "..\client\clientinfo.xml"
    if (Test-Path $repoClientInfo) {
        Copy-Item -Path $repoClientInfo -Destination $clientInfoDest -Force
        Write-Ok "clientinfo.xml copiado do repositório local."
        $downloaded = $true
    }
}
if (-not $downloaded) {
    Write-Warn "Não foi possível copiar clientinfo.xml automaticamente. Baixe em $WebPanelUrl."
}

# 5) Copia Ragexe.exe se informado por parâmetro
$targetExe = Join-Path $TargetClientDir "Ragexe.exe"
if (-not [string]::IsNullOrWhiteSpace($RagexePath) -and (Test-Path $RagexePath)) {
    Copy-Item -Path $RagexePath -Destination $targetExe -Force
    Write-Ok "Ragexe.exe copiado para $targetExe"
}

# 6) Cria script lançador com argumento 1rag1
$launcherPath = Join-Path $TargetClientDir "Jogar AureumRO Testes.bat"
@"
@echo off
cd /d "%~dp0"
if not exist "Ragexe.exe" (
    echo [ERRO] Coloque o executavel Ragexe.exe (2022-04-06 patcheado com WARP) nesta pasta!
    echo Consulte client\README_CLIENTE.md no repositorio ServidordeTestes.
    pause
    exit /b 1
)
start "" "Ragexe.exe" 1rag1
"@ | Set-Content -Path $launcherPath -Encoding ASCII
Write-Ok "Lançador criado: $launcherPath"

Write-Host "`n============================================================" -ForegroundColor Green
Write-Host "  Pasta do cliente pronta em: $TargetClientDir" -ForegroundColor Green
Write-Host "============================================================" -ForegroundColor Green

if (-not (Test-Path $targetExe)) {
    Write-Host "`nÚltimo passo manual (1 único arquivo):" -ForegroundColor Yellow
    Write-Host "  1. Coloque seu '2022-04-06_Ragexe.exe' patcheado com WARP nesta pasta com o nome:" -ForegroundColor White
    Write-Host "     $targetExe" -ForegroundColor Cyan
    Write-Host "     - Download 2022-04-06_Ragexe: http://nemo.herc.ws/downloads/2022-04-06_Ragexe_1648707856/" -ForegroundColor DarkGray
    Write-Host "     - Patcher WARP:               https://github.com/Neo-Mind/WARP" -ForegroundColor DarkGray
    Write-Host "     - Patches obrigatórios:       Read Data Folder First, Disable Packet Encryption, Disable HShield" -ForegroundColor DarkGray
    Write-Host "  2. Execute: $launcherPath" -ForegroundColor White
    Write-Host "  3. Faça login com admin/admin123 ou tester/tester123." -ForegroundColor White
    Write-Host "  4. Com o personagem logado, rode .\scripts\find-4tools-addresses.ps1 para configurar o AureumRO-4Tools automaticamente!" -ForegroundColor White
} else {
    Write-Host "`nTudo pronto! Execute '$launcherPath' para entrar no servidor." -ForegroundColor Cyan
}
