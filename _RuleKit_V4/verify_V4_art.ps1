param([string]$ProjectRoot = ".")
$pr = (Resolve-Path $ProjectRoot).Path
function Ok($m){ Write-Host "OK   - $m" -ForegroundColor Green }
function Bad($m){ Write-Host "FAIL - $m" -ForegroundColor Red; $script:fail = $true }
$assets = @(
 "Assets\Resources\Art\Matcaps\matcap_purple.png",
 "Assets\Resources\Art\FX\cape_gradient.png",
 "Assets\Resources\Art\FX\cosmic_swirl.png",
 "Assets\Resources\Art\FX\orb_glow_purple.png",
 "Assets\Resources\Art\FX\halo_rings.png",
 "Assets\Resources\Art\FX\lotus_petal.png",
 "Assets\Resources\Art\FX\bead.png",
 "Assets\Resources\Characters\chr_orion_gemini.json",
 "Assets\Resources\Characters\chr_solaris_virgo.json"
)
foreach($a in $assets){
  if (Test-Path (Join-Path $pr $a)) { Ok "$a" } else { Bad "Faltando $a" }
}
if ($fail){ exit 1 } else { Write-Host "Arte mínima garantida (V4)" -ForegroundColor Green; exit 0 }
