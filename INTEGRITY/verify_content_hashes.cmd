@echo off
setlocal EnableExtensions EnableDelayedExpansion
powershell -ExecutionPolicy Bypass -File "%~dp0verify_content_hashes.ps1" "%~dp0.."
if errorlevel 1 pause
