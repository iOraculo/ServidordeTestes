@echo off
setlocal
set "CLIENT_DIR=%~dp0client\Froggo2022"
if not exist "%CLIENT_DIR%\opensetup.exe" (
    set "CLIENT_DIR=C:\AureumRO\Froggo2022"
)

if not exist "%CLIENT_DIR%\opensetup.exe" (
    echo [ERRO] opensetup.exe nao encontrado em client\Froggo2022 nem em C:\AureumRO\Froggo2022.
    pause
    exit /b 1
)

start "" /D "%CLIENT_DIR%" "opensetup.exe"
exit /b 0
