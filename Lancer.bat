@echo off
chcp 65001 >nul
title Focus Heure
cd /d "%~dp0"

echo.
echo  Focus Heure - demarrage...
echo.

REM Version autonome (si presente)
if exist "dist\FocusHeure.exe" (
    start "" "dist\FocusHeure.exe"
    goto :ok
)

REM Version compilee classique
if exist "bin\Release\net9.0-windows\FocusHeure.exe" (
    start "" "bin\Release\net9.0-windows\FocusHeure.exe"
    goto :ok
)

echo  Compilation en cours (premiere fois)...
where dotnet >nul 2>&1
if errorlevel 1 (
    echo.
    echo  ERREUR : .NET SDK introuvable.
    echo  Installez .NET 9 : https://dotnet.microsoft.com/download/dotnet/9.0
    echo  Ou lancez : Creer-version-portable.bat
    pause
    exit /b 1
)

dotnet build -c Release
if errorlevel 1 (
    echo.
    echo  ERREUR de compilation.
    pause
    exit /b 1
)

start "" "bin\Release\net9.0-windows\FocusHeure.exe"

:ok
echo.
echo  OK - Regardez l'icone F en bas a droite (zone de notification).
echo  Si vous ne la voyez pas : fleche ^^ puis epinglez Focus Heure.
echo.
timeout /t 5 >nul
