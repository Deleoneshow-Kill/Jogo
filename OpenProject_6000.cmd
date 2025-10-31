@echo off
setlocal EnableExtensions
set PROJ=%~dp0
echo Abrindo no Unity Hub: %PROJ%
start "" "unityhub://open-project?path=%PROJ%"
echo No Hub, garanta que a versão 6000.2.9f1 seja usada.
