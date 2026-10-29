@echo off
setlocal enabledelayedexpansion

echo ========================================================
echo        Universal Steam Idler - Build Script
echo ========================================================
echo.

set "CSC=C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"

if not exist "%CSC%" (
    echo [ERROR] C# Compiler not found at: %CSC%
    echo Make sure .NET Framework 4.0+ is installed.
    pause
    exit /b 1
)

echo [1/3] Preparing output directories...
if not exist "bin" mkdir "bin"

echo [2/3] Compiling C# source code...
"%CSC%" /target:winexe /platform:x86 /optimize+ /r:System.dll,System.Windows.Forms.dll,System.Drawing.dll,System.Web.Extensions.dll /out:"bin\UniversalSteamIdler.exe" src\Config.cs src\SteamScanner.cs src\WorkerService.cs src\SettingsForm.cs src\TrayApp.cs src\Program.cs

if %ERRORLEVEL% neq 0 (
    echo.
    echo [ERROR] Build failed! Check compiler output above.
    pause
    exit /b %ERRORLEVEL%
)

echo [3/3] Copying dependencies...
if exist "lib\steam_api.dll" (
    copy /Y "lib\steam_api.dll" "bin\steam_api.dll" >nul
)

echo.
echo ========================================================
echo [SUCCESS] Build completed successfully!
echo Executable: bin\UniversalSteamIdler.exe
echo ========================================================
echo.
