@echo off
setlocal EnableExtensions
set PROJ=%~dp0
echo Limpando pastas transitórias em: %PROJ%
for %%D in (Library Temp obj Logs) do (
  if exist "%PROJ%%%D" (
    echo Removendo "%PROJ%%%D"...
    rmdir /S /Q "%PROJ%%%D"
  )
)
echo Concluído. Agora abra com o Unity Hub (6000.2.9f1).
pause
