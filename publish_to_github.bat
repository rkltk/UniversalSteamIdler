@echo off
setlocal
echo ========================================================
echo       Universal Steam Idler - Push to GitHub
echo ========================================================
echo.
echo 1. Create an empty repository on https://github.com/new
echo    (Do NOT check "Add a README file" or .gitignore)
echo.
set /p REPO_URL="Enter your GitHub Repository URL (e.g. https://github.com/username/UniversalSteamIdler.git): "

if "%REPO_URL%"=="" (
    echo [ERROR] URL cannot be empty.
    pause
    exit /b 1
)

git remote remove origin 2>nul
git remote add origin %REPO_URL%
git branch -M main
echo.
echo Pushing to GitHub...
git push -u origin main

if %ERRORLEVEL% equ 0 (
    echo.
    echo ========================================================
    echo [SUCCESS] Repository successfully pushed to GitHub!
    echo ========================================================
) else (
    echo.
    echo [ERROR] Git push failed. Make sure you are authenticated with Git.
)
echo.
pause
