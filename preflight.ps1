Write-Host '== Preflight (blindado) ==' -ForegroundColor Cyan
$folders = @('Library\ScriptAssemblies','Library\Bee','Temp','obj','Logs')
foreach ($f in $folders) { if (Test-Path $f) { Remove-Item $f -Recurse -Force -ErrorAction SilentlyContinue } }
# Corrige namespace do ComboHooks se existir
$combo = 'Assets\Scripts\Battle\BattleBootstrap3D_ComboHooks.cs'
if (Test-Path $combo) {
  $txt = Get-Content $combo -Raw
  if ($txt -match 'namespace\s+CleanRPG\.Systems') {
    $txt = $txt -replace 'namespace\s+CleanRPG\.Systems','namespace CleanRPG.Battle'
    Set-Content $combo $txt -Encoding UTF8
    Write-Host 'Corrigido namespace ComboHooks -> CleanRPG.Battle' -ForegroundColor Yellow
  }
}
Write-Host 'Preflight concluído.' -ForegroundColor Green
