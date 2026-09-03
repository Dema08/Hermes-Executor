@echo off
title Hermes-Executor Launcher
color 0a
echo ===================================================
echo       LAUNCHING LATEST HERMES-EXECUTOR
echo ===================================================

cd /d "%~dp0"

set "UI_EXE=%~dp0HermesUI\bin\Release\net9.0-windows\Hermes-Executor.exe"

if not exist "%UI_EXE%" (
    echo [WARNING] Release executable not found! Running build.bat first...
    call "%~dp0build.bat"
)

:: Check if already running
tasklist /FI "IMAGENAME eq Hermes-Executor.exe" 2>NUL | find /I /N "Hermes-Executor.exe">NUL
if "%ERRORLEVEL%"=="0" (
    echo [INFO] Hermes-Executor is already running. Bringing to front...
    powershell -Command "(New-Object -ComObject Shell.Application).MinimizeAll()"
    powershell -Command "$wshell = New-Object -ComObject WScript.Shell; $wshell.AppActivate('Hermes-Executor')"
    timeout /t 2 /nobreak >nul
    exit
)

if exist "%UI_EXE%" (
    echo [INFO] Starting Hermes-Executor UI...
    start "" "%UI_EXE%"
    timeout /t 2 /nobreak >nul
) else (
    echo [ERROR] Could not find executable to run!
    pause
)
