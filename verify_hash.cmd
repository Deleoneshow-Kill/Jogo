@echo off
set FILE=%~1
if "%FILE%"=="" (echo Uso: verify_hash.cmd caminho\arquivo.zip & exit /b 1)
certutil -hashfile "%FILE%" SHA256
