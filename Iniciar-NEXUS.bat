@echo off
setlocal
cd /d "%~dp0"

where dotnet >nul 2>&1
if errorlevel 1 goto no_dotnet

dotnet --list-sdks | findstr /r /c:"^10\." >nul
if errorlevel 1 goto no_sdk

echo Restaurando dependencias de NEXUS...
dotnet restore "NEXUS 2.0.csproj"
if errorlevel 1 goto failed

echo Iniciando NEXUS...
dotnet run --no-restore --project "NEXUS 2.0.csproj"
if errorlevel 1 goto failed
exit /b 0

:no_dotnet
echo No se encontro .NET. Instala .NET 10 SDK desde https://dotnet.microsoft.com/download/dotnet/10.0
pause
exit /b 1

:no_sdk
echo NEXUS requiere .NET 10 SDK. Instala desde https://dotnet.microsoft.com/download/dotnet/10.0
pause
exit /b 1

:failed
echo NEXUS no pudo iniciarse. Revisa los mensajes anteriores y vuelve a intentarlo.
pause
exit /b 1

