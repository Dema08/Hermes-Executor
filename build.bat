@echo off
title Hermes-Executor Auto Builder
color 0b
echo ===================================================
echo       HERMES-EXECUTOR AUTOMATED BUILD SCRIPT
echo ===================================================

cd /d "%~dp0"

:: Kill running Hermes-Executor to avoid file locks
echo [PREP] Closing running Hermes-Executor instances...
taskkill /F /IM Hermes-Executor.exe >nul 2>&1
timeout /t 1 /nobreak >nul

set "UI_BIN=%~dp0HermesUI\bin\Release\net9.0-windows"

echo [1/4] Building HermesPayload (Payload DLL)...
pushd HermesPayload
if not exist build mkdir build
pushd build
cmake .. -G "Visual Studio 17 2022" -A x64 >nul 2>&1
if %errorlevel% neq 0 (
    echo [ERROR] HermesPayload CMake configuration failed!
    pause
    exit /b %errorlevel%
)
cmake --build . --config Release 2>&1
if %errorlevel% neq 0 (
    echo [ERROR] HermesPayload build failed!
    pause
    exit /b %errorlevel%
)
popd
popd

echo [2/4] Copying HermesPayload.dll to UI output...
if exist "%UI_BIN%\HermesPayload.dll" (
    echo [OK] HermesPayload.dll present at %UI_BIN%\HermesPayload.dll
) else (
    echo [WARNING] HermesPayload.dll not found at expected path!
    if exist "%~dp0HermesPayload\build\Release\HermesPayload.dll" (
        copy /Y "%~dp0HermesPayload\build\Release\HermesPayload.dll" "%UI_BIN%\HermesPayload.dll" >nul
        echo [OK] Copied from build\Release subfolder.
    )
)

echo [3/4] Building HermesCore (C++ DLL via CMake/MSBuild)...
pushd HermesCore
if not exist build mkdir build
pushd build
cmake .. -G "Visual Studio 17 2022" -A x64 >nul 2>&1
if %errorlevel% neq 0 (
    echo [ERROR] HermesCore CMake configuration failed!
    pause
    exit /b %errorlevel%
)
cmake --build . --config Release 2>&1
if %errorlevel% neq 0 (
    echo [ERROR] HermesCore build failed!
    pause
    exit /b %errorlevel%
)
popd
popd

echo [4/4] Copying HermesCore.dll to UI output...
if exist "%UI_BIN%\HermesCore.dll" (
    echo [OK] HermesCore.dll present at %UI_BIN%\HermesCore.dll
) else (
    echo [WARNING] HermesCore.dll not found at expected path!
    if exist "%~dp0HermesCore\build\Release\HermesCore.dll" (
        copy /Y "%~dp0HermesCore\build\Release\HermesCore.dll" "%UI_BIN%\HermesCore.dll" >nul
        echo [OK] Copied from build\Release subfolder.
    )
)

echo [5/5] Building HermesUI (.NET WPF Release)...
pushd HermesUI
dotnet build -c Release 2>&1
if %errorlevel% neq 0 (
    echo [ERROR] HermesUI build failed!
    pause
    exit /b %errorlevel%
)
popd

echo ===================================================
echo [SUCCESS] Build completed successfully!
echo Binaries updated to latest version.
echo ===================================================
pause
