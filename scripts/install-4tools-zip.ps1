param(
    [string]$ZipPath = "",
    [string]$ClientDir = ""
)

$ErrorActionPreference = "Stop"

$RepoRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($ClientDir)) {
    $unified = Join-Path $RepoRoot "client\Froggo2022"
    if (Test-Path $unified) { $ClientDir = $unified } else { $ClientDir = "C:\AureumRO\Froggo2022" }
}

if (-not (Test-Path $ClientDir)) {
    throw "Pasta do cliente nao encontrada: $ClientDir"
}

Write-Host "=== Instalador AureumRO-4Tools (Build Servidor de Testes) ===" -ForegroundColor Yellow

# 1. Fecha qualquer instancia antiga do 4Tools (mesmo minimizada na bandeja)
$running = Get-Process | Where-Object { $_.ProcessName -like "*4Tools*" -or $_.ProcessName -like "*4RTools*" }
foreach ($proc in $running) {
    Write-Host "[INFO] Fechando instancia aberta: $($proc.ProcessName) (PID $($proc.Id))..." -ForegroundColor DarkYellow
    Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
}
Start-Sleep -Milliseconds 600

# 2. Localiza automaticamente o ultimo AureumRO-4Tools-TestBuild*.zip em Downloads
if ([string]::IsNullOrWhiteSpace($ZipPath)) {
    $downloads = Join-Path $env:USERPROFILE "Downloads"
    $latest = Get-ChildItem -Path $downloads -Filter "AureumRO-4Tools-TestBuild*.zip" -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if ($latest) {
        $ZipPath = $latest.FullName
    }
}

if (-not [string]::IsNullOrWhiteSpace($ZipPath) -and (Test-Path $ZipPath)) {
    Write-Host "[1/2] Extraindo: $ZipPath" -ForegroundColor Cyan
    $tmp = Join-Path $env:TEMP ("aureum4tools_" + [Guid]::NewGuid().ToString("N"))
    New-Item -ItemType Directory -Path $tmp -Force | Out-Null
    try {
        Expand-Archive -Path $ZipPath -DestinationPath $tmp -Force
        $exe = Get-ChildItem -Path $tmp -Recurse -Filter "AureumRO 4Tools.exe" | Select-Object -First 1
        if (-not $exe) {
            throw "AureumRO 4Tools.exe nao encontrado dentro do ZIP: $ZipPath"
        }
        Copy-Item -Path $exe.FullName -Destination (Join-Path $ClientDir "AureumRO 4Tools.exe") -Force
        Write-Host "[OK] Novo AureumRO 4Tools.exe instalado em $ClientDir" -ForegroundColor Green
    } finally {
        if (Test-Path $tmp) { Remove-Item -Path $tmp -Recurse -Force -ErrorAction SilentlyContinue }
    }
} else {
    Write-Host "[WARN] Nenhum AureumRO-4Tools-TestBuild*.zip encontrado em Downloads." -ForegroundColor DarkYellow
    Write-Host "       Baixe o artefato do GitHub Actions e rode este script novamente." -ForegroundColor Gray
}

# 3. Sempre sincroniza o supported_servers.json com FroggoClient + Ragexe + AureumRO
$srcJson = Join-Path $RepoRoot "tools\AureumRO-4Tools\supported_servers.json"
if (Test-Path $srcJson) {
    Copy-Item -Path $srcJson -Destination (Join-Path $ClientDir "supported_servers.json") -Force
    Write-Host "[2/2] supported_servers.json sincronizado (AureumRO + FroggoClient + Ragexe)." -ForegroundColor Green
}

Write-Host "Pronto! Agora abra o jogo com Jogar-AureumRO.bat." -ForegroundColor Green
