@echo off
setlocal
set "CLIENT_DIR=%~dp0client\Froggo2022"
if not exist "%CLIENT_DIR%\FroggoClient.exe" (
    set "CLIENT_DIR=C:\AureumRO\Froggo2022"
)

if not exist "%CLIENT_DIR%\FroggoClient.exe" (
    echo [ERRO] FroggoClient.exe nao encontrado em client\Froggo2022 nem em C:\AureumRO\Froggo2022.
    pause
    exit /b 1
)

echo Iniciando AureumRO Servidor de Testes (%CLIENT_DIR%)...
start "" /D "%CLIENT_DIR%" "FroggoClient.exe"

if exist "%CLIENT_DIR%\AureumTools.exe" (
    timeout /t 2 /nobreak >nul
    start "" /D "%CLIENT_DIR%" "AureumTools.exe"
) else if exist "%CLIENT_DIR%\AureumRO 4Tools.exe" (
    timeout /t 2 /nobreak >nul
    start "" /D "%CLIENT_DIR%" "AureumRO 4Tools.exe"
)
exit /b 0
