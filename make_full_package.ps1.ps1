# make_full_package.ps1
param(
  [Parameter(Mandatory=$true)]
  [string]$SourceZip,           # Caminho do último ZIP COMPLETO que você tem
  [string]$UnityExe="C:\Program Files\Unity\Hub\Editor\6000.2.9f1\Editor\Unity.exe"
)

$ErrorActionPreference = "Stop"
function Banner($t){ Write-Host "==== $t ====" -ForegroundColor Cyan }

# 0) Ambiente
Set-ExecutionPolicy -Scope Process Bypass -Force
$stamp = (Get-Date).ToString("yyyy-MM-dd_HH-mm-ss")
$work = Join-Path $PSScriptRoot ("_work_"+$stamp)
$dst  = Join-Path $PSScriptRoot ("Jogo_"+$stamp+"_FULL.zip")

New-Item -ItemType Directory -Force -Path $work | Out-Null
Banner "Descompactando base completa: $SourceZip"
Expand-Archive -Path $SourceZip -DestinationPath $work -Force

# Descobre a raiz do projeto (pasta que contém Assets/Packages/ProjectSettings)
$proj = Get-ChildItem $work -Recurse -Directory | Where-Object {
  Test-Path (Join-Path $_.FullName "Assets") -and
  Test-Path (Join-Path $_.FullName "Packages") -and
  Test-Path (Join-Path $_.FullName "ProjectSettings")
} | Select-Object -First 1

if(-not $proj){ throw "Não encontrei a raiz do projeto (Assets/Packages/ProjectSettings)." }
$projPath = $proj.FullName
Banner "Projeto detectado: $projPath"

# 1) Blindagens fixas de estrutura
Banner "Blindagem de TMP / Packages / .meta"
# Garante TMP Essentials presente (não importa o conteúdo, só garante estrutura para evitar Importer)
$tmpRes = Join-Path $projPath "Assets\TextMesh Pro\Resources"
New-Item -ItemType Directory -Force -Path $tmpRes | Out-Null
# Não tocamos em manifest nem packages-lock conforme regra.

# 2) Remoção de duplicatas e namespaces incorretos
Banner "Correções de duplicatas e namespaces"
# PalaceArenaAutoBuilder duplicado em Battle -> remover (manter o de Scene)
$dupPA = Join-Path $projPath "Assets\Scripts\Battle\PalaceArenaAutoBuilder.cs"
if(Test-Path $dupPA){ Remove-Item $dupPA -Force -ErrorAction SilentlyContinue
  $dupPAm = $dupPA + ".meta"; if(Test-Path $dupPAm){ Remove-Item $dupPAm -Force }
}

# BattleBootstrap3D_ComboHooks no namespace errado -> corrigir
$bbHooks = Join-Path $projPath "Assets\Scripts\Battle\BattleBootstrap3D_ComboHooks.cs"
if(Test-Path $bbHooks){
  (Get-Content $bbHooks) -replace 'namespace\s+CleanRPG\.Systems','namespace CleanRPG.Battle' |
    Set-Content $bbHooks -Encoding UTF8
}

# 3) Scripts fundamentais (substituições seguras, mantendo tua API)
Banner "Injetando/atualizando scripts blindados"

$sceneBuilderPath = Join-Path $projPath "Assets\Scripts\Scene\SceneBuilder.cs"
New-Item -ItemType Directory -Force -Path (Split-Path $sceneBuilderPath) | Out-Null
@'
using UnityEngine;

namespace CleanRPG.Systems {
  public static class SceneBuilder {
    public static void BuildPalaceArena() {
      // CÂMERA
      if (Camera.main==null){
        var cam=new GameObject("Main Camera").AddComponent<Camera>();
        cam.tag="MainCamera"; cam.clearFlags=CameraClearFlags.Skybox; cam.transform.position=new Vector3(0,6,-12);
        cam.transform.rotation=Quaternion.Euler(15,0,0);
      }
      // LUZ
      if (GameObject.Find("Directional Light")==null){
        var l=new GameObject("Directional Light").AddComponent<Light>();
        l.type=LightType.Directional; l.intensity=1.2f; l.color=new Color(1f,0.95f,0.85f);
        l.transform.rotation=Quaternion.Euler(50,30,0);
      }
      // CHÃO circular simples
      var floor=GameObject.Find("Arena_Floor");
      if (floor==null){
        floor=GameObject.CreatePrimitive(PrimitiveType.Cylinder); floor.name="Arena_Floor";
        floor.transform.localScale=new Vector3(10f,0.2f,10f);
        var m=new Material(Shader.Find("Unlit/Color")); m.color=new Color(0.15f,0.25f,0.55f,1f);
        floor.GetComponent<Renderer>().sharedMaterial=m;
      }
      // COLUNAS laterais
      for(int i=0;i<6;i++){
        var name="Pillar_"+i;
        if(!GameObject.Find(name)){
          var p=GameObject.CreatePrimitive(PrimitiveType.Cylinder); p.name=name;
          float ang=i*(360f/6f)*Mathf.Deg2Rad; float r=9f;
          p.transform.position=new Vector3(Mathf.Cos(ang)*r,2.5f,Mathf.Sin(ang)*r);
          p.transform.localScale=new Vector3(0.6f,2.5f,0.6f);
          var pm=new Material(Shader.Find("Unlit/Color")); pm.color=new Color(0.9f,0.78f,0.4f);
          p.GetComponent<Renderer>().sharedMaterial=pm;
        }
      }
    }
  }

  public class PalaceArenaAutoBuilder : MonoBehaviour {
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void OnAfterSceneLoad(){ SceneBuilder.BuildPalaceArena(); }
  }
}
'@ | Set-Content $sceneBuilderPath -Encoding UTF8

$rotatorPath = Join-Path $projPath "Assets\Scripts\FX\Rotator.cs"
New-Item -ItemType Directory -Force -Path (Split-Path $rotatorPath) | Out-Null
@'
using UnityEngine;
namespace CleanRPG.FX {
  public class Rotator : MonoBehaviour {
    public Vector3 speed = new Vector3(0,60,0);
    void Update(){ transform.Rotate(speed * Time.deltaTime, Space.Self); }
  }
}
'@ | Set-Content $rotatorPath -Encoding UTF8

$charVisPath = Join-Path $projPath "Assets\Scripts\Systems\CharacterVisuals.cs"
New-Item -ItemType Directory -Force -Path (Split-Path $charVisPath) | Out-Null
@'
using UnityEngine;
using CleanRPG.FX;

namespace CleanRPG.Systems {
  public static class CharacterVisuals {
    public static void Build(GameObject host, object def, bool isEnemy=false){
      if(host==null) return;
      // limpa filhos
      for(int i=host.transform.childCount-1;i>=0;i--) Object.Destroy(host.transform.GetChild(i).gameObject);
      // desliga renderer base se existir
      var r = host.GetComponent<Renderer>(); if(r) r.enabled=false;

      string id = (def!=null)? def.ToString().ToLowerInvariant() : "orion";
      if(id.Contains("solaris") || id.Contains("virgo")) BuildSolaris(host);
      else BuildOrion(host, isEnemy);
    }

    static Material M(Color c){ var m=new Material(Shader.Find("Unlit/Color")); m.color=c; return m; }
    static GameObject Q(string n, Transform p, Vector3 s, Color c){
      var g=new GameObject(n); g.transform.SetParent(p,false);
      var q=GameObject.CreatePrimitive(PrimitiveType.Quad); q.name="Quad"; q.transform.SetParent(g.transform,false);
      q.transform.localScale=s; q.GetComponent<Renderer>().sharedMaterial=M(c);
      return g;
    }

    static void BuildOrion(GameObject host, bool enemy){
      var root=new GameObject("Orion").transform; root.SetParent(host.transform,false);
      // tronco
      var body=GameObject.CreatePrimitive(PrimitiveType.Capsule); body.name="Body"; body.transform.SetParent(root,false);
      body.transform.localScale=new Vector3(0.8f,1.2f,0.8f);
      body.GetComponent<Renderer>().sharedMaterial=M(new Color(0.6f,0.45f,0.95f));
      // ombreiras
      for(int i=0;i<2;i++){
        var s=GameObject.CreatePrimitive(PrimitiveType.Sphere); s.name="Shoulder_"+i; s.transform.SetParent(root,false);
        s.transform.localScale=new Vector3(0.6f,0.35f,0.6f);
        s.transform.localPosition=new Vector3(i==0?-0.5f:0.5f,0.9f,0);
        s.GetComponent<Renderer>().sharedMaterial=M(new Color(0.5f,0.35f,0.85f));
      }
      // capa
      var cape=Q("Cape",root,new Vector3(1.6f,2.8f,1f),new Color(0.35f,0.15f,0.6f,0.7f));
      cape.transform.localPosition=new Vector3(0,0.2f,-0.15f);
      cape.AddComponent<Rotator>().speed=new Vector3(0,10,0);

      // aura solo
      var aura=GameObject.CreatePrimitive(PrimitiveType.Cylinder); aura.name="Aura"; aura.transform.SetParent(root,false);
      aura.transform.localScale=new Vector3(1.6f,0.01f,1.6f);
      aura.transform.localPosition=new Vector3(0,-1.0f,0);
      aura.GetComponent<Renderer>().sharedMaterial=M(enemy? new Color(1f,0.2f,0.2f,0.5f) : new Color(0.4f,0.6f,1f,0.5f));

      // orbe
      var orb=GameObject.CreatePrimitive(PrimitiveType.Sphere); orb.name="Orb"; orb.transform.SetParent(root,false);
      orb.transform.localScale=new Vector3(0.35f,0.35f,0.35f);
      orb.transform.localPosition=new Vector3(0.55f,0.6f,0.35f);
      orb.GetComponent<Renderer>().sharedMaterial=M(new Color(0.85f,0.2f,1f,0.9f));
      var ring=Q("CosmicRing",orb.transform,new Vector3(1.6f,1.6f,1f),new Color(0.7f,0.5f,1f,0.35f));
      ring.AddComponent<Rotator>().speed=new Vector3(0,120,0);
    }

    static void BuildSolaris(GameObject host){
      var root=new GameObject("Solaris").transform; root.SetParent(host.transform,false);
      var body=GameObject.CreatePrimitive(PrimitiveType.Capsule); body.name="Body"; body.transform.SetParent(root,false);
      body.transform.localScale=new Vector3(0.8f,1.2f,0.8f);
      body.GetComponent<Renderer>().sharedMaterial=M(new Color(1.0f,0.85f,0.35f));
      var halo=Q("Halo",root,new Vector3(2.2f,2.2f,1f),new Color(1f,0.9f,0.4f,0.35f));
      halo.transform.localPosition=new Vector3(0,1.4f,0);
      halo.AddComponent<Rotator>().speed=new Vector3(0,80,0);
      var lotus=Q("Lotus",root,new Vector3(1.6f,1.6f,1f),new Color(1f,0.8f,0.25f,0.45f));
      lotus.transform.localPosition=new Vector3(0,-0.9f,0);
      lotus.AddComponent<Rotator>().speed=new Vector3(0,60,0);
    }
  }
}
'@ | Set-Content $charVisPath -Encoding UTF8

$turnEnginePath = Join-Path $projPath "Assets\Scripts\Battle\TurnEngine.cs"
New-Item -ItemType Directory -Force -Path (Split-Path $turnEnginePath) | Out-Null
@'
using UnityEngine;

namespace CleanRPG.Battle {
  public class TurnEngine : MonoBehaviour {
    public int round=1;
    public int energy=3;
    public int maxEnergy=7;

    public bool Spend(int cost){
      if(energy<cost) return false;
      energy-=cost; return true;
    }
    public void NextTurn(){
      round++; energy=Mathf.Min(maxEnergy, energy+2);
    }
  }
}
'@ | Set-Content $turnEnginePath -Encoding UTF8

$hudPath = Join-Path $projPath "Assets\Scripts\UI\HUD_MobileStyle.cs"
New-Item -ItemType Directory -Force -Path (Split-Path $hudPath) | Out-Null
@'
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CleanRPG.Battle;

namespace CleanRPG.UI {
  public class HUD_MobileStyle : MonoBehaviour {
    TurnEngine te;
    TextMeshProUGUI top;
    void Start(){
      te = FindFirstObjectByType<TurnEngine>();
      var c=new GameObject("HUD").AddComponent<Canvas>(); c.renderMode=RenderMode.ScreenSpaceOverlay;
      var ce=c.gameObject.AddComponent<CanvasScaler>(); ce.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize; ce.referenceResolution=new Vector2(1920,1080);
      c.gameObject.AddComponent<GraphicRaycaster>();
      top = MakeText(c.transform, "Round | Energia", new Vector2(0.5f,1f), new Vector2(0.5f,1f), new Vector2(0,-40));
      MakeBtn(c.transform,"ATK",new Vector2(1f,0f),new Vector2(1f,0f),new Vector2(-260,120), ()=>Try(1));
      MakeBtn(c.transform,"SKILL",new Vector2(1f,0f),new Vector2(1f,0f),new Vector2(-160,120), ()=>Try(2));
      MakeBtn(c.transform,"ULT",new Vector2(1f,0f),new Vector2(1f,0f),new Vector2(-60,120), ()=>Try(4));
      UpdateTop();
    }
    void Update(){ UpdateTop(); }
    void UpdateTop(){ if(te&&top) top.text=$"Round {te.round} | Energia {te.energy}/{te.maxEnergy}"; }
    void Try(int cost){ if(te && te.Spend(cost)) te.NextTurn(); }
    Button MakeBtn(Transform p,string label,Vector2 min,Vector2 max,Vector2 ofs,System.Action cb){
      var go=new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
      go.transform.SetParent(p,false);
      var rt=(RectTransform)go.transform; rt.anchorMin=min; rt.anchorMax=max; rt.anchoredPosition=ofs; rt.sizeDelta=new Vector2(90,90);
      go.GetComponent<Image>().color=new Color(0,0,0,0.35f);
      var t=MakeText(go.transform,label, new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), Vector2.zero);
      var b=go.GetComponent<Button>(); b.onClick.AddListener(()=>cb());
      return b;
    }
    TextMeshProUGUI MakeText(Transform p,string txt,Vector2 min,Vector2 max,Vector2 ofs){
      var go=new GameObject("txt", typeof(RectTransform));
      go.transform.SetParent(p,false);
      var rt=(RectTransform)go.transform; rt.anchorMin=min; rt.anchorMax=max; rt.anchoredPosition=ofs; rt.sizeDelta=new Vector2(0,0);
      var t=go.AddComponent<TextMeshProUGUI>(); t.text=txt; t.fontSize=34; t.alignment=TextAlignmentOptions.Center; t.color=Color.white;
      return t;
    }
  }
}
'@ | Set-Content $hudPath -Encoding UTF8

$demoPath = Join-Path $projPath "Assets\Scripts\Battle\DemoAutoSpawner.cs"
New-Item -ItemType Directory -Force -Path (Split-Path $demoPath) | Out-Null
@'
using UnityEngine;
using CleanRPG.Systems;
using CleanRPG.Battle;

namespace CleanRPG.Battle {
  public class DemoAutoSpawner : MonoBehaviour {
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot(){
      // Cena / Arena
      CleanRPG.Systems.SceneBuilder.BuildPalaceArena();

      // Turn Engine
      if(!FindFirstObjectByType<TurnEngine>()){
        new GameObject("TurnEngine").AddComponent<TurnEngine>();
      }
      // HUD
      if(!FindFirstObjectByType<CleanRPG.UI.HUD_MobileStyle>()){
        new GameObject("HUD").AddComponent<CleanRPG.UI.HUD_MobileStyle>();
      }
      // Spawns
      if(GameObject.Find("OrionHost")==null){
        var a=GameObject.CreatePrimitive(PrimitiveType.Capsule); a.name="OrionHost"; a.transform.position=new Vector3(-2,0,0);
        CharacterVisuals.Build(a, "orion", false);
      }
      if(GameObject.Find("SolarisHost")==null){
        var b=GameObject.CreatePrimitive(PrimitiveType.Capsule); b.name="SolarisHost"; b.transform.position=new Vector3(2,0,0);
        CharacterVisuals.Build(b, "solaris", true);
      }
    }
  }
}
'@ | Set-Content $demoPath -Encoding UTF8

# 4) Limpeza de caches
Banner "Limpando caches (Library/Bee/ScriptAssemblies/Temp)"
@("Library","Bee","Temp","Logs") | ForEach-Object {
  $p=Join-Path $projPath $_
  if(Test-Path $p){ Remove-Item $p -Recurse -Force -ErrorAction SilentlyContinue }
}

# 5) Recompilação headless rápida (abre e fecha para gerar Library sem erro)
Banner "Testando projeto com Unity (headless)"
$log = Join-Path $projPath "Editor_make_full.log"
$arg = "-quit -batchmode -projectPath `"$projPath`" -logFile `"$log`""
Start-Process -FilePath $UnityExe -ArgumentList $arg -Wait

# 6) Verificações mínimas em log
Banner "Verificando erros no log"
$errs = Select-String -Path $log -Pattern "error|Error|ERROR|CompilerError" | Select-Object -First 1
if($errs){ Write-Host $errs -ForegroundColor Yellow }

# 7) Empacotar COMPLETO com nome blindado
Banner "Empacotando FULL"
if(Test-Path $dst){ Remove-Item $dst -Force }
Compress-Archive -Path (Join-Path $projPath "*") -DestinationPath $dst -Force

# 8) Hash e saída
$sha = (Get-FileHash $dst -Algorithm SHA256).Hash
Banner "PRONTO"
Write-Host "ZIP: $dst" -ForegroundColor Green
Write-Host "SHA-256: $sha" -ForegroundColor Green
