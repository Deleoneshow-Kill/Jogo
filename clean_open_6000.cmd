@echo off
setlocal
set PROJ=%~dp0
rmdir /s /q "%PROJ%\Library\ScriptAssemblies" 2>nul
rmdir /s /q "%PROJ%\Library\Bee" 2>nul
rmdir /s /q "%PROJ%\Temp" 2>nul
set UEXE=C:\Program Files\Unity\Hub\Editor\6000.2.9f1\Editor\Unity.exe
if exist "%UEXE%" (
  start "" "%UEXE%" -projectPath "%PROJ%"
) else (
  echo [Aviso] Abra pelo Unity Hub na versao 6000.2.9f1.
)
endlocal
