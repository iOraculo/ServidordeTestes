param(
    [string]$ClientDir = "",
    [string]$ZipPath = "",
    [switch]$Uninstall
)

$ErrorActionPreference = "Stop"

$RepoRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($ClientDir)) {
    $unified = Join-Path $RepoRoot "client\Froggo2022"
    if (Test-Path $unified) { $ClientDir = $unified } else { $ClientDir = "C:\AureumRO\Froggo2022" }
}
$ConfSrc = Join-Path $RepoRoot "client\visuals\dgVoodoo.conf"
$FogSrc = Join-Path $RepoRoot "client\visuals\fogParameterTable.txt"

Write-Host "=== AureumRO dgVoodoo2 HD Graphics (DX7 -> DX11 | 16x Anisotropic + 4x MSAA) ===" -ForegroundColor Yellow

if (-not (Test-Path $ClientDir)) {
    throw "Pasta do cliente nao encontrada: $ClientDir"
}

$Targets = @("DDraw.dll", "D3DImm.dll", "dgVoodoo.conf")

if ($Uninstall) {
    foreach ($f in $Targets) {
        $p = Join-Path $ClientDir $f
        if (Test-Path $p) {
            Remove-Item -Path $p -Force
            Write-Host "[REMOVIDO] $p" -ForegroundColor Yellow
        }
    }
    Write-Host "[OK] dgVoodoo2 removido. O cliente voltara a usar DirectX 7 nativo." -ForegroundColor Green
    exit 0
}

[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$TempDir = Join-Path $env:TEMP ("aureumro_dgvoodoo_" + [Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $TempDir -Force | Out-Null

try {
    $LocalZip = $ZipPath
    if ([string]::IsNullOrWhiteSpace($LocalZip)) {
        Write-Host "[1/3] Buscando ultima versao oficial do dgVoodoo2 no GitHub..." -ForegroundColor Cyan
        $DownloadUrl = ""
        try {
            $release = Invoke-RestMethod -Uri "https://api.github.com/repos/dege-diosg/dgVoodoo2/releases/latest" -Headers @{ "User-Agent" = "AureumRO-Installer" } -TimeoutSec 20
            foreach ($asset in $release.assets) {
                if ($asset.name -match "^dgVoodoo2_[0-9_]+\.zip$") {
                    $DownloadUrl = $asset.browser_download_url
                    break
                }
            }
        } catch {
            Write-Host "[WARN] Nao foi possivel consultar a API do GitHub; tentando URL direta de fallback..." -ForegroundColor DarkYellow
        }

        if ([string]::IsNullOrWhiteSpace($DownloadUrl)) {
            $DownloadUrl = "https://github.com/dege-diosg/dgVoodoo2/releases/download/2.87.6/dgVoodoo2_87_6.zip"
        }

        $LocalZip = Join-Path $TempDir "dgVoodoo2.zip"
        Write-Host "      Baixando: $DownloadUrl" -ForegroundColor Gray
        Invoke-WebRequest -Uri $DownloadUrl -OutFile $LocalZip -UseBasicParsing
    }

    if (-not (Test-Path $LocalZip)) {
        throw "Arquivo ZIP do dgVoodoo2 nao encontrado: $LocalZip"
    }

    Write-Host "[2/3] Extraindo MS\x86\DDraw.dll e D3DImm.dll (32-bit para Ragexe)..." -ForegroundColor Cyan
    $ExtractDir = Join-Path $TempDir "extracted"
    Expand-Archive -Path $LocalZip -DestinationPath $ExtractDir -Force

    $DDraw = Get-ChildItem -Path $ExtractDir -Recurse -Filter "DDraw.dll" | Where-Object { $_.FullName -match "\\MS\\x86\\" } | Select-Object -First 1
    $D3DImm = Get-ChildItem -Path $ExtractDir -Recurse -Filter "D3DImm.dll" | Where-Object { $_.FullName -match "\\MS\\x86\\" } | Select-Object -First 1

    if (-not $DDraw -or -not $D3DImm) {
        throw "Nao foi possivel localizar MS\x86\DDraw.dll e D3DImm.dll dentro do ZIP do dgVoodoo2."
    }

    Copy-Item -Path $DDraw.FullName -Destination (Join-Path $ClientDir "DDraw.dll") -Force
    Copy-Item -Path $D3DImm.FullName -Destination (Join-Path $ClientDir "D3DImm.dll") -Force

    Write-Host "[3/3] Instalando dgVoodoo.conf (16x Anisotropico, MSAA 4x, sem watermark) e fog limpa..." -ForegroundColor Cyan
    Copy-Item -Path $ConfSrc -Destination (Join-Path $ClientDir "dgVoodoo.conf") -Force

    $DataDir = Join-Path $ClientDir "data"
    if (-not (Test-Path $DataDir)) {
        New-Item -ItemType Directory -Path $DataDir -Force | Out-Null
    }
    if (Test-Path $FogSrc) {
        Copy-Item -Path $FogSrc -Destination (Join-Path $DataDir "fogParameterTable.txt") -Force
    }

    Write-Host ""
    Write-Host "CONCLUIDO! dgVoodoo2 instalado com sucesso em $ClientDir" -ForegroundColor Green
    Write-Host "O que mudou:" -ForegroundColor Yellow
    Write-Host "  - TODOS os mapas do jogo agora usam DirectX 11 com Filtro Anisotropico 16x + MSAA 4x" -ForegroundColor Gray
    Write-Host "  - Marca d'agua desativada (dgVoodooWatermark = false)" -ForegroundColor Gray
    Write-Host "  - Neblina lavada removida em Prontera, Arena e cidades principais" -ForegroundColor Gray
    Write-Host "  - No opensetup.exe, selecione 'dgVoodoo Virtual 3D Accelerated Card' se quiser confirmar." -ForegroundColor Gray
    Write-Host "  - Para remover depois: powershell -ExecutionPolicy Bypass -File .\scripts\install-dgvoodoo-hd.ps1 -Uninstall" -ForegroundColor Gray
}
finally {
    if (Test-Path $TempDir) {
        Remove-Item -Path $TempDir -Recurse -Force -ErrorAction SilentlyContinue
    }
}
