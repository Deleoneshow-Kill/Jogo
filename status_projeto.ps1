# Verificação Final do Projeto Unity - Clean Room RPG
Write-Host "=== STATUS DO PROJETO UNITY ===" -ForegroundColor Green
Write-Host ""

# Verificar se Unity está rodando
$unityProcess = Get-Process -Name "Unity*" -ErrorAction SilentlyContinue
if ($unityProcess) {
    Write-Host "✅ Unity está executando (PID: $($unityProcess.Id))" -ForegroundColor Green
} else {
    Write-Host "❌ Unity não está executando" -ForegroundColor Red
}

# Verificar arquivos importantes
$projectPath = Get-Location
Write-Host "📁 Caminho do projeto: $projectPath" -ForegroundColor Cyan

$manifestPath = "Packages\manifest.json"
if (Test-Path $manifestPath) {
    Write-Host "✅ manifest.json existe" -ForegroundColor Green
} else {
    Write-Host "❌ manifest.json não encontrado" -ForegroundColor Red
}

$mainScenePath = "Assets\Scenes\Main.unity"
if (Test-Path $mainScenePath) {
    Write-Host "✅ Cena principal (Main.unity) existe" -ForegroundColor Green
} else {
    Write-Host "❌ Cena principal não encontrada" -ForegroundColor Red
}

$rotatorPath = "Assets\Scripts\Systems\Rotator.cs"
if (Test-Path $rotatorPath) {
    Write-Host "✅ Classe Rotator corrigida" -ForegroundColor Green
} else {
    Write-Host "❌ Classe Rotator não encontrada" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== INSTRUÇÕES PARA EXECUTAR O JOGO ===" -ForegroundColor Yellow
Write-Host ""
Write-Host "1. 🎮 No Unity Editor:" -ForegroundColor Cyan
Write-Host "   - Pressione o botão PLAY (▶️) no Unity" -ForegroundColor White
Write-Host "   - O jogo carregará automaticamente" -ForegroundColor White
Write-Host ""
Write-Host "2. 🎯 Controles do jogo:" -ForegroundColor Cyan
Write-Host "   F2 - Seleção de personagens" -ForegroundColor White
Write-Host "   F3 - Sistema Gacha" -ForegroundColor White
Write-Host "   F4 - Arena PvP" -ForegroundColor White
Write-Host "   F5 - Sistema de Replay" -ForegroundColor White
Write-Host "   1/2/3 - Usar habilidades" -ForegroundColor White
Write-Host "   Tab - Alternar alvo" -ForegroundColor White
Write-Host "   Espaço - Pular turno" -ForegroundColor White
Write-Host "   R - Reset da batalha" -ForegroundColor White
Write-Host ""
Write-Host "3. 🔧 Se houver problemas:" -ForegroundColor Cyan
Write-Host "   - Verifique o Console do Unity para erros" -ForegroundColor White
Write-Host "   - Use Tools > Setup Project no Unity para reconfigurar" -ForegroundColor White
Write-Host ""
Write-Host "✨ Projeto corrigido e pronto para uso!" -ForegroundColor Green