@echo off
title AureumRO - Central de Controle do Servidor de Testes
cd /d "%~dp0"

:MENU
cls
echo ========================================================================
echo            AUREUMRO - CENTRAL DE CONTROLE (SERVIDOR DE TESTES)
echo ========================================================================
echo.
echo   [1] Jogar (Abrir FroggoClient + AureumTools)
echo   [2] Setup de Video / Audio (opensetup.exe)
echo   [3] Iniciar Servidor (Docker: MariaDB + rAthena + Painel Web)
echo   [4] Reiniciar rAthena (Aplicar mudancas de NPCs/Configs)
echo   [5] Parar Servidor (Docker down)
echo   [6] Abrir Painel Web (http://localhost:8080)
echo   [7] Atualizar do GitHub (git pull + rebuild WebPanel + restart)
echo   [8] Chao HD 512x512 - Modo Marmore Real / Arena Obsidiana
echo   [9] Chao HD 512x512 - Modo Remaster Classico (4x nitido)
echo   [O] Restaurar Chao Original do kRO
echo   [D] Graficos HD Globais (Instalar dgVoodoo2 DX11 + 16x AF + MSAA 4x)
echo   [V] Reinstalar Tela de Login, Loading e Skin AureumRO_Obsidian
echo   [B] Fazer Backup do Banco de Dados (backups\)
echo   [0] Sair
echo.
set /p "OPT=Escolha uma opcao e pressione Enter: "

if /i "%OPT%"=="1" (
    call "%~dp0Jogar-AureumRO.bat"
    goto MENU
)
if /i "%OPT%"=="2" (
    call "%~dp0Setup-AureumRO.bat"
    goto MENU
)
if /i "%OPT%"=="3" (
    docker compose up -d
    pause
    goto MENU
)
if /i "%OPT%"=="4" (
    docker compose restart rathena
    pause
    goto MENU
)
if /i "%OPT%"=="5" (
    docker compose down
    pause
    goto MENU
)
if /i "%OPT%"=="6" (
    start "" "http://localhost:8080"
    goto MENU
)
if /i "%OPT%"=="7" (
    git pull
    docker compose up -d --build webpanel
    docker compose restart rathena
    pause
    goto MENU
)
if /i "%OPT%"=="8" (
    powershell -ExecutionPolicy Bypass -File "%~dp0scripts\install-hd-ground.ps1" -Mode marmore
    pause
    goto MENU
)
if /i "%OPT%"=="9" (
    powershell -ExecutionPolicy Bypass -File "%~dp0scripts\install-hd-ground.ps1" -Mode remaster
    pause
    goto MENU
)
if /i "%OPT%"=="O" (
    powershell -ExecutionPolicy Bypass -File "%~dp0scripts\install-hd-ground.ps1" -Mode original
    pause
    goto MENU
)
if /i "%OPT%"=="D" (
    powershell -ExecutionPolicy Bypass -File "%~dp0scripts\install-dgvoodoo-hd.ps1"
    pause
    goto MENU
)
if /i "%OPT%"=="V" (
    powershell -ExecutionPolicy Bypass -File "%~dp0scripts\install-client-visuals.ps1"
    pause
    goto MENU
)
if /i "%OPT%"=="B" (
    powershell -ExecutionPolicy Bypass -File "%~dp0scripts\backup-db.ps1"
    pause
    goto MENU
)
if /i "%OPT%"=="0" exit /b 0

goto MENU
