param([string]$Root = ".")
$manifest = Join-Path $Root "INTEGRITY\content_manifest.sha256"
if (!(Test-Path $manifest)) { Write-Host "Manifesto não encontrado: $manifest" -ForegroundColor Red; exit 1 }
$lines = Get-Content $manifest
$errors = 0
foreach ($line in $lines) {
  if ($line.Trim() -eq "" -or $line.Trim().StartsWith("#")) { continue }
  $parts = $line.Split("  ")
  if ($parts.Count -lt 2) { continue }
  $expected = $parts[0].Trim()
  $rel = $parts[1].Trim()
  $path = Join-Path $Root $rel
  if (!(Test-Path $path)) { Write-Host "FALTANDO: $rel" -ForegroundColor Red; $errors++ ; continue }
  $actual = (Get-FileHash -Path $path -Algorithm SHA256).Hash.ToLower()
  if ($actual -ne $expected.ToLower()) {
    Write-Host ("DIFERENTE: {0}" -f $rel) -ForegroundColor Yellow
    Write-Host ("  esperado: {0}" -f $expected)
    Write-Host ("  obtido  : {0}" -f $actual)
    $errors++
  } else {
    Write-Host ("OK: {0}" -f $rel) -ForegroundColor Green
  }
}
if ($errors -gt 0) { Write-Host "Falhas: $errors" -ForegroundColor Red; exit 2 } else { Write-Host "Tudo OK." -ForegroundColor Green; exit 0 }