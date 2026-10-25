@echo off
chcp 65001 >nul
title Focus Heure - version portable
cd /d "%~dp0"

echo.
echo  Creation d'une version portable (ne necessite pas .NET installe)...
echo  Cela peut prendre 1 a 2 minutes...
echo.

where dotnet >nul 2>&1
if errorlevel 1 (
    echo  ERREUR : installez d'abord .NET 9 SDK depuis
    echo  https://dotnet.microsoft.com/download/dotnet/9.0
    pause
    exit /b 1
)

dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o dist

if errorlevel 1 (
    echo  Echec de la publication.
    pause
    exit /b 1
)

echo.
echo  Termine ! Lancez : dist\FocusHeure.exe
echo  Ou double-cliquez sur Lancer.bat
echo.
pause
