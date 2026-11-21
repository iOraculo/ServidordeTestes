#Requires -Version 5.1
<#
.SYNOPSIS
    Instala no cliente Froggo2022 as artes Obsidiana & Ouro Real do AureumRO:
    - Tela de Login fatiada em 12 BMPs oficiais (1024x768)
    - Telas de Loading (loading00..03.jpg)
    - Skin escura e dourada "AureumRO_Obsidian" em skin\AureumRO_Obsidian\
#>
param(
    [string]$ClientDir = "C:\AureumRO\Froggo2022"
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing

if (-not (Test-Path $ClientDir)) {
    Write-Host "[ERRO] Pasta do cliente nao encontrada: $ClientDir" -ForegroundColor Red
    exit 1
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$loginSrc = Join-Path $repoRoot "client\visuals\aureumro_login.jpg"
$loadingSrc = Join-Path $repoRoot "client\visuals\aureumro_loading.jpg"

# Cria pasta EUC-KR exata usada pelo cliente Ragnarok: data\texture\<userinterface>
$eucKr = [System.Text.Encoding]::GetEncoding(949)
$uiFolderName = $eucKr.GetString([byte[]]@(0xC0,0xAF,0xC0,0xFA,0xC0,0xCE,0xC5,0xCD,0xC6,0xE4,0xC0,0xCC,0xBD,0xBA))
$bgPrefix = $eucKr.GetString([byte[]]@(0x74,0x5F,0xB9,0xE8,0xB0,0xE6))
$uiDir = Join-Path $ClientDir "data\texture\$uiFolderName"
New-Item -ItemType Directory -Path $uiDir -Force | Out-Null

# 1) Fatiar tela de login em 12 blocos 256x256 (grid 4x3 = 1024x768) em formato BMP 24bpp
if (Test-Path $loginSrc) {
    Write-Host "[Visual] Instalando Tela de Login Obsidiana & Ouro..." -ForegroundColor Yellow
    $orig = [System.Drawing.Image]::FromFile($loginSrc)
    $canvas = New-Object System.Drawing.Bitmap(1024, 768, [System.Drawing.Imaging.PixelFormat]::Format24bppRgb)
    $g = [System.Drawing.Graphics]::FromImage($canvas)
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.DrawImage($orig, 0, 0, 1024, 768)
    $g.Dispose()
    $orig.Dispose()

    for ($row = 0; $row -lt 3; $row++) {
        for ($col = 0; $col -lt 4; $col++) {
            $rect = New-Object System.Drawing.Rectangle(($col * 256), ($row * 256), 256, 256)
            $tile = $canvas.Clone($rect, [System.Drawing.Imaging.PixelFormat]::Format24bppRgb)
            $fileName = "{0}{1}-{2}.bmp" -f $bgPrefix, ($row + 1), ($col + 1)
            $tile.Save((Join-Path $uiDir $fileName), [System.Drawing.Imaging.ImageFormat]::Bmp)
            $tile.Dispose()
        }
    }
    $canvas.Dispose()
    Write-Host "  [OK] 12 fatias de tela de login instaladas em data\texture." -ForegroundColor Green
}

# 2) Instalar telas de loading (loading00..03.jpg em 1024x768)
if (Test-Path $loadingSrc) {
    Write-Host "[Visual] Instalando Telas de Loading Obsidiana & Ouro..." -ForegroundColor Yellow
    $orig = [System.Drawing.Image]::FromFile($loadingSrc)
    $canvas = New-Object System.Drawing.Bitmap(1024, 768, [System.Drawing.Imaging.PixelFormat]::Format24bppRgb)
    $g = [System.Drawing.Graphics]::FromImage($canvas)
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.DrawImage($orig, 0, 0, 1024, 768)
    $g.Dispose()
    $orig.Dispose()

    foreach ($idx in 0..3) {
        $outJpg = Join-Path $uiDir ("loading0{0}.jpg" -f $idx)
        $canvas.Save($outJpg, [System.Drawing.Imaging.ImageFormat]::Jpeg)
    }
    $canvas.Dispose()
    Write-Host "  [OK] loading00.jpg..loading03.jpg instalados." -ForegroundColor Green
}

# 3) Gerar Skin Escura & Dourada (skin\AureumRO_Obsidian\basic_interface)
Write-Host "[Visual] Gerando Skin 'AureumRO_Obsidian' (Obsidiana & Ouro Real)..." -ForegroundColor Yellow
$skinDir = Join-Path $ClientDir "skin\AureumRO_Obsidian\basic_interface"
New-Item -ItemType Directory -Path $skinDir -Force | Out-Null

function New-ObsidianPanel([string]$path, [int]$w, [int]$h, [bool]$withHeader = $true) {
    $bmp = New-Object System.Drawing.Bitmap($w, $h, [System.Drawing.Imaging.PixelFormat]::Format24bppRgb)
    $gr = [System.Drawing.Graphics]::FromImage($bmp)
    $bgBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(18, 12, 3))
    $hdrBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(36, 24, 6))
    $goldPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(212, 175, 55), 1)
    $innerPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(95, 72, 22), 1)

    $gr.FillRectangle($bgBrush, 0, 0, $w, $h)
    if ($withHeader -and $h -ge 30) {
        $gr.FillRectangle($hdrBrush, 1, 1, $w - 2, 17)
        $gr.DrawLine($goldPen, 1, 18, $w - 2, 18)
    }
    $gr.DrawRectangle($goldPen, 0, 0, $w - 1, $h - 1)
    if ($w -gt 6 -and $h -gt 6) {
        $gr.DrawRectangle($innerPen, 2, 2, $w - 5, $h - 5)
    }

    $gr.Dispose(); $bgBrush.Dispose(); $hdrBrush.Dispose(); $goldPen.Dispose(); $innerPen.Dispose()
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Bmp)
    $bmp.Dispose()
}

$skinAssets = @(
    @{ Name = "basewin_bg.bmp";   W = 280; H = 120; Hdr = $true  },
    @{ Name = "basewin_bg2.bmp";  W = 280; H = 120; Hdr = $true  },
    @{ Name = "shortitem_bg.bmp"; W = 280; H = 34;  Hdr = $false },
    @{ Name = "win_msgbox.bmp";   W = 280; H = 120; Hdr = $true  },
    @{ Name = "itemwin_mid.bmp";  W = 280; H = 32;  Hdr = $false },
    @{ Name = "chatwin0_bg.bmp";  W = 280; H = 80;  Hdr = $false },
    @{ Name = "chatwin1_bg.bmp";  W = 280; H = 80;  Hdr = $false },
    @{ Name = "sysbox_bg.bmp";    W = 280; H = 120; Hdr = $true  },
    @{ Name = "titlebar_mid.bmp"; W = 256; H = 17;  Hdr = $false },
    @{ Name = "dialog_bg.bmp";    W = 280; H = 120; Hdr = $true  }
)

foreach ($a in $skinAssets) {
    New-ObsidianPanel (Join-Path $skinDir $a.Name) $a.W $a.H $a.Hdr
}

Write-Host "  [OK] Skin criada em $ClientDir\skin\AureumRO_Obsidian" -ForegroundColor Green
Write-Host "`nConcluido! Ao entrar no jogo:" -ForegroundColor Cyan
Write-Host "  - A tela de Login e Loading ja estao com a arte Obsidiana & Ouro!" -ForegroundColor White
Write-Host "  - Para ativar a Skin escura: aperte ESC -> Configuracoes -> Skin -> AureumRO_Obsidian" -ForegroundColor White
