# Script para corrigir problemas do projeto Unity
Write-Host "Diagnosticando e corrigindo problemas do projeto Unity..." -ForegroundColor Green

# 1. Verificar se TextMeshPro está configurado corretamente
$tmpPath = "Assets\TextMeshPro"
$tmpResourcesPath = "Assets\TextMeshPro\Resources"

if (!(Test-Path $tmpPath)) {
    Write-Host "Criando pasta TextMeshPro..." -ForegroundColor Yellow
    New-Item -ItemType Directory -Path $tmpPath -Force
}

if (!(Test-Path $tmpResourcesPath)) {
    Write-Host "Criando pasta TextMeshPro/Resources..." -ForegroundColor Yellow
    New-Item -ItemType Directory -Path $tmpResourcesPath -Force
}

# 2. Limpar cache do Unity
Write-Host "Limpando cache do Unity..." -ForegroundColor Yellow
if (Test-Path "Library") {
    Remove-Item -Path "Library" -Recurse -Force
}
if (Test-Path "Temp") {
    Remove-Item -Path "Temp" -Recurse -Force
}
if (Test-Path "Logs") {
    Remove-Item -Path "Logs\*" -Recurse -Force
}

# 3. Recrear manifest.json com versões compatíveis
Write-Host "Atualizando manifest.json para Unity 6..." -ForegroundColor Yellow
$manifestContent = @"
{
  "dependencies": {
    "com.unity.textmeshpro": "3.2.0-pre.11",
    "com.unity.ugui": "2.0.0",
    "com.unity.modules.accessibility": "1.0.0",
    "com.unity.modules.animation": "1.0.0",
    "com.unity.modules.imgui": "1.0.0",
    "com.unity.modules.physics": "1.0.0",
    "com.unity.modules.uielements": "1.0.0",
    "com.unity.modules.unitywebrequest": "1.0.0"
  }
}
"@

$manifestContent | Out-File -FilePath "Packages\manifest.json" -Encoding UTF8

Write-Host "Projeto corrigido! Tente abrir no Unity novamente." -ForegroundColor Green
Write-Host "Se ainda houver problemas, execute: Unity.exe -projectPath . -createProject" -ForegroundColor Cyan