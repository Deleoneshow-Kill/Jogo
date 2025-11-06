using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

#if UNITY_RENDER_PIPELINE_URP
using UnityEngine.Rendering.Universal;
#endif

namespace JogoSSA.Editor
{
    public static class SSAFullAutomation
    {
        private const string StarterKitMenuPath = "Jogo/SSA/0) Starter Kit (Auto)";
        private const string RunAllMenuPath = "Jogo/SSA/1) Rodar Fluxo Completo";
        private const string ApplyMenuPath = "Jogo/SSA/2) Aplicar Toon (Seleção ou Pasta)";
        private const string ComponentsMenuPath = "Jogo/SSA/3) Garantir Componentes SSA";
        private const string HudMenuPath = "Jogo/SSA/4) Criar HUD SSA";
        private const string PreviewMenuPath = "Jogo/SSA/5) Criar Palco de Preview";

        [MenuItem(StarterKitMenuPath, priority = 4)]
        private static void RunStarterKitAutomation()
        {
            if (!EnsureUrpReady(interactive: true))
            {
                return;
            }

#if UNITY_RENDER_PIPELINE_URP
            if (!EnsureUrpPipelineActive())
            {
                return;
            }
#endif

            using var progress = new ProgressScope("SSA Starter Kit");

            progress.Report(0.05f, "Validando Starter Kit");
            if (!EnsureStarterKitAssets())
            {
                return;
            }

            StarterKitStage stage = null;

#if UNITY_RENDER_PIPELINE_URP
            progress.Report(0.15f, "Convertendo assets para URP");
            ConvertProjectToUrp();
            progress.Report(0.22f, "Executando correção de materiais magenta");
            RunSsaFixMagenta();
#endif

            progress.Report(0.3f, "Montando palco SSA Starter Kit");
            stage = EnsureStarterKitStage();

            progress.Report(0.45f, "Localizando SSAQuickApply");
            var applier = FindOrCreateQuickApply();
            if (!applier)
            {
                Debug.LogError("[SSA] Não foi possível preparar o componente SSAQuickApply.");
                return;
            }

            progress.Report(0.55f, "Atribuindo referências");
            var missing = AssignQuickApplyReferences(applier, stage);

            progress.Report(0.65f, "Aplicando visual SSA");
            applier.ApplyNow();

#if UNITY_RENDER_PIPELINE_URP
            progress.Report(0.72f, "Configurando materiais do piso e colunas");
            EnsureStarterKitMaterials(stage);
#endif

            progress.Report(0.8f, "Reforçando emissive do personagem ativo");
            EnsureActiveActorEmission(applier.activeActor);

            progress.Report(0.86f, "Ocultando placeholders");
            DisableStarterKitPlaceholders(stage);

            progress.Report(0.92f, "Rodando verificação visual");
            SSAVisualVerifier.Verify();

            if (missing.Count > 0)
            {
                Debug.LogWarning("[SSA] Starter Kit auto: não encontrei automaticamente: " + string.Join(", ", missing) + ". Preencha manualmente no componente SSAQuickApply se necessário.");
            }

            Debug.Log("[SSA] Starter Kit aplicado. Confira o log do verificador para PASS/FAIL detalhado.");
        }

        [MenuItem(RunAllMenuPath, priority = 5)]
        private static void RunEverythingMenu()
        {
            RunFullWorkflow(interactive: true);
        }

        [MenuItem(ApplyMenuPath, priority = 6)]
        private static void ApplyToonStep()
        {
            var targets = ResolveTargets(promptForFolder: true);
            if (targets.Length == 0)
            {
                return;
            }

            SSAApplyToonToSelection.ApplyToon(targets, interactive: false);
            EnsureUnitStats(targets);
            EnsureTurnManagerUnits();
            Debug.Log("[SSA] Materiais toon aplicados. Componentes SSA garantidos para os alvos.");
        }

        [MenuItem(ComponentsMenuPath, priority = 7)]
        private static void EnsureComponentsStep()
        {
            var targets = ResolveTargets(promptForFolder: Selection.gameObjects == null || Selection.gameObjects.Length == 0);
            if (targets.Length == 0)
            {
                return;
            }

            EnsureUnitStats(targets);
            EnsureTurnManagerUnits();
            Debug.Log("[SSA] Componentes SSA adicionados/atualizados.");
        }

        [MenuItem(HudMenuPath, priority = 8)]
        private static void CreateHudStep()
        {
            SSA_CreateHUD.CreateHUD();
            EnsureTurnManagerUnits();
            Debug.Log("[SSA] HUD SSA pronto na cena.");
        }

        [MenuItem(PreviewMenuPath, priority = 9)]
        private static void CreatePreviewStep()
        {
            CreatePreviewStage(interactive: true);
            EnsureTargetSelectorOnCamera();
            Debug.Log("[SSA] Palco de preview configurado.");
        }

        private static void RunFullWorkflow(bool interactive)
        {
            if (!EnsureUrpReady(interactive))
            {
                return;
            }

            using var progress = new ProgressScope("SSA Fluxo Completo");

            progress.Report(0.05f, "Executando Setup SSA (Toon)");
            SetupSSA.Execute();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            progress.Report(0.25f, "Localizando personagens");
            var targets = ResolveTargets(promptForFolder: interactive);

            if (targets.Length > 0)
            {
                progress.Report(0.45f, "Aplicando materiais toon");
                SSAApplyToonToSelection.ApplyToon(targets, interactive: false);

                progress.Report(0.6f, "Configurando componentes SSA");
                EnsureUnitStats(targets);
            }
            else
            {
                Debug.LogWarning("[SSA] Nenhum personagem encontrado para aplicar o setup toon.");
            }

            progress.Report(0.75f, "Construindo HUD SSA");
            SSA_CreateHUD.CreateHUD();
            EnsureTurnManagerUnits();

            progress.Report(0.85f, "Garantindo Target Selector na câmera");
            EnsureTargetSelectorOnCamera();

            progress.Report(0.92f, "Criando palco de preview");
            CreatePreviewStage(interactive: false);

            progress.Report(0.97f, "Centralizando visão");
            FramePreview(targets);

            Debug.Log("[SSA] Fluxo completo finalizado.");
        }

        private static bool EnsureUrpReady(bool interactive)
        {
#if UNITY_RENDER_PIPELINE_URP
            return true;
#else
            var urpType = System.Type.GetType("UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset, Unity.RenderPipelines.Universal.Runtime");
            if (urpType == null)
            {
                const string title = "Universal RP necessário";
                const string message = "Instale o pacote 'Universal Render Pipeline' via Package Manager antes de rodar o setup.";

                if (!interactive)
                {
                    Debug.LogWarning("[SSA] URP não está instalado. Rode o menu 'Jogo/Instalar URP (auto)' e tente novamente.");
                    return false;
                }

                bool install = EditorUtility.DisplayDialog(title, message + "\n\nDeseja que eu acione a instalação automática agora?", "Instalar automaticamente", "Cancelar");
                if (install)
                {
                    URPInstaller.InstallURP();
                    EditorUtility.DisplayDialog("Instalação iniciada", "O pacote URP está sendo instalado pelo Package Manager. Aguarde a recompilação e execute o fluxo novamente.", "OK");
                }

                return false;
            }

            if (!EnsureUrpDefineEnabled())
            {
                EditorUtility.DisplayDialog(
                    "Recompilar com URP",
                    "Adicionei o define UNITY_RENDER_PIPELINE_URP. Aguarde a recompilação automática e execute o menu novamente.",
                    "OK");
            }

            return false;
#endif
        }

#if !UNITY_RENDER_PIPELINE_URP
        private static bool EnsureUrpDefineEnabled()
        {
            var activeTarget = EditorUserBuildSettings.activeBuildTarget;
            var group = BuildPipeline.GetBuildTargetGroup(activeTarget);
            if (group == BuildTargetGroup.Unknown)
            {
                group = EditorUserBuildSettings.selectedBuildTargetGroup;
            }
            if (group == BuildTargetGroup.Unknown)
            {
                group = BuildTargetGroup.Standalone;
            }

            var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
            var entries = defines.Split(';').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();
            if (entries.Contains("UNITY_RENDER_PIPELINE_URP"))
            {
                return true;
            }

            entries.Add("UNITY_RENDER_PIPELINE_URP");
            var updated = string.Join(";", entries);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(group, updated);
            Debug.Log($"[SSA] UNITY_RENDER_PIPELINE_URP adicionado para {group}. Defines atuais: {updated}");
            return false;
        }
#endif

        private static GameObject[] ResolveTargets(bool promptForFolder)
        {
            var resolved = new List<GameObject>();
            var selection = Selection.gameObjects;
            if (selection != null && selection.Length > 0)
            {
                foreach (var go in selection.Distinct())
                {
                    var instance = EnsureSceneInstance(go);
                    if (instance && !ShouldSkipTarget(instance))
                    {
                        resolved.Add(instance);
                    }
                }
            }

            if (resolved.Count > 0 || !promptForFolder)
            {
                return resolved.ToArray();
            }

            var folderPath = EditorUtility.OpenFolderPanel("Selecione a pasta com prefabs SSA", Application.dataPath, string.Empty);
            if (string.IsNullOrEmpty(folderPath))
            {
                return System.Array.Empty<GameObject>();
            }

            var assetRelative = ToAssetRelativePath(folderPath);
            if (string.IsNullOrEmpty(assetRelative))
            {
                EditorUtility.DisplayDialog("Pasta inválida", "A pasta precisa estar dentro de Assets.", "OK");
                return System.Array.Empty<GameObject>();
            }

            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { assetRelative });
            if (guids.Length == 0)
            {
                EditorUtility.DisplayDialog("Nada encontrado", "Nenhum prefab foi localizado na pasta escolhida.", "OK");
                return System.Array.Empty<GameObject>();
            }

            var parent = new GameObject("SSA_PreviewCharacters");
            Undo.RegisterCreatedObjectUndo(parent, "Criar Grupo SSA Preview");

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (!prefab)
                {
                    continue;
                }

                if (PrefabUtility.InstantiatePrefab(prefab) is GameObject instance)
                {
                    Undo.RegisterCreatedObjectUndo(instance, "Instanciar Personagem SSA");
                    instance.transform.SetParent(parent.transform, false);
                    if (ShouldSkipTarget(instance))
                    {
                        Undo.DestroyObjectImmediate(instance);
                        continue;
                    }
                    resolved.Add(instance);
                }
            }

            if (resolved.Count == 0)
            {
                Object.DestroyImmediate(parent);
                return System.Array.Empty<GameObject>();
            }

            Selection.activeGameObject = parent;
            return resolved.ToArray();
        }

        private static GameObject EnsureSceneInstance(GameObject go)
        {
            if (!go)
            {
                return null;
            }

            if (go.scene.IsValid() && go.scene.isLoaded)
            {
                return ShouldSkipTarget(go) ? null : go;
            }

            if (PrefabUtility.InstantiatePrefab(go) is GameObject instance)
            {
                Undo.RegisterCreatedObjectUndo(instance, "Instanciar Prefab SSA");
                if (ShouldSkipTarget(instance))
                {
                    Undo.DestroyObjectImmediate(instance);
                    return null;
                }
                return instance;
            }

            return null;
        }

        private static bool ShouldSkipTarget(GameObject go)
        {
            return go && go.name.StartsWith("SSA_Stage", System.StringComparison.OrdinalIgnoreCase);
        }

        private static void EnsureUnitStats(IEnumerable<GameObject> targets)
        {
            foreach (var go in targets)
            {
                if (!go)
                {
                    continue;
                }

                var stats = go.GetComponent<SSA_UnitStats>();
                if (!stats)
                {
                    stats = Undo.AddComponent<SSA_UnitStats>(go);
                }

                Undo.RecordObject(stats, "Atualizar SSA Unit Stats");

                if (string.IsNullOrEmpty(stats.UnitName) || stats.UnitName == "Unit")
                {
                    stats.UnitName = go.name;
                }

                if (!stats.SelectionAnchor)
                {
                    var anchor = go.transform.Find("SSA_SelectionAnchor");
                    if (!anchor)
                    {
                        var anchorGO = new GameObject("SSA_SelectionAnchor");
                        Undo.RegisterCreatedObjectUndo(anchorGO, "Criar SSA Selection Anchor");
                        anchorGO.transform.SetParent(go.transform, false);
                        anchor = anchorGO.transform;
                    }
                    stats.SelectionAnchor = anchor;
                }

                EditorUtility.SetDirty(stats);
            }
        }

        private static void EnsureTurnManagerUnits()
        {
            var manager = Object.FindObjectOfType<SSA_TurnManager>();
            if (!manager)
            {
                return;
            }

            Undo.RecordObject(manager, "Atualizar SSA Turn Manager");
            manager.Units = Object.FindObjectsOfType<SSA_UnitStats>().Where(u => u.IsAlive).ToList();
            EditorUtility.SetDirty(manager);
        }

        private static void EnsureTargetSelectorOnCamera()
        {
            var camera = Camera.main ?? Object.FindObjectOfType<Camera>();
            if (!camera)
            {
                Debug.LogWarning("[SSA] Nenhuma câmera encontrada para adicionar SSA_TargetSelector.");
                return;
            }

            var selector = camera.GetComponent<SSA_TargetSelector>();
            if (!selector)
            {
                selector = Undo.AddComponent<SSA_TargetSelector>(camera.gameObject);
            }

            Undo.RecordObject(selector, "Atualizar SSA Target Selector");

            if (selector.hitMask == 0)
            {
                selector.hitMask = LayerMask.GetMask("Default");
            }

            EditorUtility.SetDirty(selector);
        }

        private static void CreatePreviewStage(bool interactive)
        {
            if (GameObject.Find("SSA_StageSample"))
            {
                return;
            }

#if UNITY_RENDER_PIPELINE_URP
            var stage = SSASampleStageCreator.CreateStage(interactive);
            if (!interactive && stage)
            {
                Selection.activeGameObject = stage;
            }
#else
            Debug.LogWarning("[SSA] URP não está ativo; palco de preview não foi criado.");
#endif
        }

        private static void FramePreview(GameObject[] targets)
        {
            GameObject focus = null;
            if (targets != null && targets.Length > 0)
            {
                focus = targets.FirstOrDefault(go => go);
            }

            if (!focus)
            {
                focus = GameObject.Find("SSA_StageSample");
            }

            if (!focus)
            {
                return;
            }

            Selection.activeGameObject = focus;
            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView)
            {
                sceneView.FrameSelected();
            }
        }

        private static string ToAssetRelativePath(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath))
            {
                return string.Empty;
            }

            absolutePath = absolutePath.Replace('\\', '/');
            var dataPath = Application.dataPath.Replace('\\', '/');
            if (!absolutePath.StartsWith(dataPath))
            {
                return string.Empty;
            }

            return "Assets" + absolutePath.Substring(dataPath.Length);
        }

        private static bool EnsureStarterKitAssets()
        {
            const string kitRoot = "Assets/SSA_Kit";
            if (!AssetDatabase.IsValidFolder(kitRoot))
            {
                EditorUtility.DisplayDialog(
                    "SSA Starter Kit ausente",
                    "Não encontrei a pasta 'Assets/SSA_Kit'. Extraia o pacote SSA Starter Kit na raiz do projeto (Assets/) e tente novamente.",
                    "OK");
                return false;
            }

            var expected = new (string path, string label)[]
            {
                ($"{kitRoot}/SSAQuickApply.cs", "SSAQuickApply.cs"),
                ($"{kitRoot}/Ramp_Toon_Warm_256.png", "Ramp toon"),
                ($"{kitRoot}/Matcap_Silver_512.png", "Matcap Silver"),
                ($"{kitRoot}/Matcap_Gold_512.png", "Matcap Gold"),
                ($"{kitRoot}/Floor_Tiles_Warm_Albedo_1024.png", "Floor Albedo"),
                ($"{kitRoot}/Floor_Tiles_Normal_1024.png", "Floor Normal"),
                ($"{kitRoot}/Selection_Ring_Additive.png", "Selection ring"),
                ($"{kitRoot}/HUD_Circle_BG_256.png", "HUD circle"),
                ($"{kitRoot}/README_SSA_Starter_Kit.txt", "README")
            };

            var missing = new List<string>();
            foreach (var (path, label) in expected)
            {
                if (!AssetExists(path))
                {
                    missing.Add(label);
                }
            }

            if (missing.Count > 0)
            {
                Debug.LogWarning("[SSA] Alguns arquivos do Starter Kit não foram encontrados: " + string.Join(", ", missing) + ". O fluxo continua, mas verifique a extração do ZIP.");
            }

            return true;
        }

        private static bool AssetExists(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return false;
            }

            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
            return asset != null;
        }

        private static SSAQuickApply FindOrCreateQuickApply()
        {
            var applier = Object.FindObjectOfType<SSAQuickApply>();
            if (applier)
            {
                Undo.RecordObject(applier, "Atualizar SSAQuickApply");
                Selection.activeGameObject = applier.gameObject;
                return applier;
            }

            var go = new GameObject("SSAQuickApply");
            Undo.RegisterCreatedObjectUndo(go, "Criar SSAQuickApply");
            applier = go.AddComponent<SSAQuickApply>();
            Selection.activeGameObject = go;
            return applier;
        }

        private static List<string> AssignQuickApplyReferences(SSAQuickApply applier, StarterKitStage stage)
        {
            var issues = new List<string>();
            Undo.RecordObject(applier, "Configurar SSAQuickApply");

            if (!applier.mainCam)
            {
                applier.mainCam = Camera.main ?? stage?.Camera ?? Object.FindObjectOfType<Camera>();
            }
            if (!applier.mainCam)
            {
                issues.Add("Camera principal");
            }

            if (!applier.sun)
            {
                applier.sun = RenderSettings.sun ?? Object.FindObjectsOfType<Light>().FirstOrDefault(l => l && l.type == LightType.Directional) ?? Object.FindObjectOfType<Light>();
            }
            if (!applier.sun && stage?.Sun)
            {
                applier.sun = stage.Sun;
                RenderSettings.sun = stage.Sun;
            }
            if (!applier.sun)
            {
                issues.Add("Luz direcional");
            }

            if (!applier.arenaFloor)
            {
                applier.arenaFloor = FindRenderer(new[] { "ArenaFloor", "Floor", "SSA_Floor" });
            }
            if (!applier.arenaFloor && stage?.Floor)
            {
                applier.arenaFloor = stage.Floor;
            }
            if (!applier.arenaFloor)
            {
                issues.Add("Floor renderer");
            }

            var resolvedColumns = new List<Renderer>();
            if (applier.columns != null)
            {
                resolvedColumns.AddRange(applier.columns.Where(r => r));
            }
            if (stage?.Columns != null && stage.Columns.Length > 0)
            {
                foreach (var column in stage.Columns)
                {
                    if (column && !resolvedColumns.Contains(column))
                    {
                        resolvedColumns.Add(column);
                    }
                }
            }
            if (resolvedColumns.Count == 0)
            {
                resolvedColumns.AddRange(FindColumns());
            }
            applier.columns = resolvedColumns.Where(r => r).ToArray();
            if (applier.columns == null || applier.columns.Length == 0)
            {
                issues.Add("Colunas");
            }

            if (!applier.activeActor)
            {
                var unit = Object.FindObjectsOfType<SSA_UnitStats>().FirstOrDefault(u => u && u.IsAlive) ?? Object.FindObjectsOfType<SSA_UnitStats>().FirstOrDefault();
                applier.activeActor = unit ? unit.transform : null;
            }
            if (!applier.activeActor && stage?.Actor)
            {
                applier.activeActor = stage.Actor;
            }
            if (!applier.activeActor)
            {
                issues.Add("Personagem ativo");
            }

            if (applier.ringScale <= 0f)
            {
                applier.ringScale = 1.4f;
            }

            EditorUtility.SetDirty(applier);
            return issues;
        }

        private static Renderer FindRenderer(IEnumerable<string> candidateNames)
        {
            foreach (var name in candidateNames)
            {
                var go = GameObject.Find(name);
                if (go && go.TryGetComponent(out Renderer r))
                {
                    return r;
                }
            }

            var lowered = candidateNames.Select(n => n.ToLowerInvariant()).ToArray();
            return Object.FindObjectsOfType<Renderer>()
                .FirstOrDefault(r => r && lowered.Any(n => r.name.ToLowerInvariant().Contains(n)));
        }

        private static Renderer[] FindColumns()
        {
            var columns = new List<Renderer>();
            var namedColumns = new[] { "Column_L", "Column_R", "Column_M", "SSA_Column" };
            foreach (var name in namedColumns)
            {
                var go = GameObject.Find(name);
                if (go && go.TryGetComponent(out Renderer renderer) && !columns.Contains(renderer))
                {
                    columns.Add(renderer);
                }
            }

            if (columns.Count < 2)
            {
                columns.AddRange(Object.FindObjectsOfType<Renderer>()
                    .Where(r => r && r.name.IndexOf("column", System.StringComparison.OrdinalIgnoreCase) >= 0));
            }

            return columns.Distinct().Where(r => r).ToArray();
        }

        private static StarterKitStage EnsureStarterKitStage()
        {
            var stageRoot = GameObject.Find("SSA_StarterKitStage");
            var floorGO = GameObject.Find("ArenaFloor");
            var columnLeft = GameObject.Find("Column_L");
            var columnRight = GameObject.Find("Column_R");
            var stageCreated = false;

            if (!stageRoot)
            {
                stageRoot = new GameObject("SSA_StarterKitStage");
                Undo.RegisterCreatedObjectUndo(stageRoot, "Criar SSA Starter Kit Stage");
                stageCreated = true;
            }

            if (!floorGO)
            {
                floorGO = GameObject.CreatePrimitive(PrimitiveType.Plane);
                Undo.RegisterCreatedObjectUndo(floorGO, "Criar ArenaFloor");
                floorGO.name = "ArenaFloor";
                floorGO.transform.SetParent(stageRoot.transform, false);
                floorGO.transform.localScale = new Vector3(0.8f, 1f, 0.8f);
                floorGO.transform.position = Vector3.zero;
            }
            else if (stageCreated && floorGO.transform.parent == null)
            {
                floorGO.transform.SetParent(stageRoot.transform, true);
            }

            var columns = new List<Renderer>();

            columnLeft = EnsureColumn(columnLeft, stageRoot.transform, new Vector3(-1.8f, 0f, 2.2f), "Column_L", columns);
            columnRight = EnsureColumn(columnRight, stageRoot.transform, new Vector3(1.8f, 0f, 2.2f), "Column_R", columns);

            var camera = Camera.main ?? Object.FindObjectOfType<Camera>();
            if (!camera)
            {
                var cameraGO = new GameObject("SSA_StarterKit_Camera");
                Undo.RegisterCreatedObjectUndo(cameraGO, "Criar SSA Starter Kit Camera");
                camera = cameraGO.AddComponent<Camera>();
                cameraGO.tag = "MainCamera";
                cameraGO.transform.SetParent(stageRoot.transform, false);
                cameraGO.transform.position = new Vector3(0f, 6.5f, -8f);
                cameraGO.transform.LookAt(Vector3.zero);
#if UNITY_RENDER_PIPELINE_URP
                Undo.AddComponent<UniversalAdditionalCameraData>(cameraGO);
#endif
            }

            var sun = RenderSettings.sun ?? Object.FindObjectsOfType<Light>().FirstOrDefault(l => l && l.type == LightType.Directional);
            if (!sun)
            {
                var sunGO = new GameObject("SSA_StarterKit_Sun");
                Undo.RegisterCreatedObjectUndo(sunGO, "Criar SSA Starter Kit Sun");
                sun = sunGO.AddComponent<Light>();
                sun.type = LightType.Directional;
                sunGO.transform.SetParent(stageRoot.transform, false);
                sunGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                RenderSettings.sun = sun;
            }

            var actor = Object.FindObjectsOfType<SSA_UnitStats>().FirstOrDefault(u => u && u.IsAlive)?.transform;
            if (!actor)
            {
                var preview = GameObject.Find("SSA_ActorPreview");
                if (!preview)
                {
                    preview = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    Undo.RegisterCreatedObjectUndo(preview, "Criar SSA Actor Preview");
                    preview.name = "SSA_ActorPreview";
                    preview.transform.SetParent(stageRoot.transform, false);
                    preview.transform.position = new Vector3(0f, 1f, 0f);
                    preview.transform.localScale = new Vector3(0.85f, 1.7f, 0.85f);
                    if (preview.TryGetComponent<Collider>(out var collider))
                    {
                        Object.DestroyImmediate(collider);
                    }
                }
                var previewStats = preview.GetComponent<SSA_UnitStats>();
                if (!previewStats)
                {
                    previewStats = Undo.AddComponent<SSA_UnitStats>(preview);
                    previewStats.UnitName = "StarterKit Hero";
                }
                previewStats.IsAlive = false;
                actor = preview.transform;
            }

            if (stageCreated)
            {
                Selection.activeGameObject = stageRoot;
            }

            return new StarterKitStage(
                stageRoot,
                floorGO.TryGetComponent(out Renderer floorRenderer) ? floorRenderer : null,
                columns.ToArray(),
                actor,
                camera,
                sun);
        }

        private static GameObject EnsureColumn(GameObject existing, Transform parent, Vector3 position, string name, ICollection<Renderer> collector)
        {
            var columnGO = existing;
            if (!columnGO)
            {
                columnGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Undo.RegisterCreatedObjectUndo(columnGO, $"Criar {name}");
                columnGO.name = name;
                columnGO.transform.localScale = new Vector3(0.6f, 2.8f, 0.6f);
                columnGO.transform.position = position;
                columnGO.transform.SetParent(parent, true);
            }
            else if (columnGO.transform.parent == null)
            {
                columnGO.transform.SetParent(parent, true);
            }

            if (columnGO.TryGetComponent<Renderer>(out var renderer))
            {
                if (!collector.Contains(renderer))
                {
                    collector.Add(renderer);
                }
            }

            if (columnGO.TryGetComponent<Collider>(out var collider))
            {
                Object.DestroyImmediate(collider);
            }

            return columnGO;
        }

#if UNITY_RENDER_PIPELINE_URP
        private static void ConvertProjectToUrp()
        {
            var materialGuids = AssetDatabase.FindAssets("t:Material");
            if (materialGuids == null || materialGuids.Length == 0)
            {
                return;
            }

            var updated = 0;
            foreach (var guid in materialGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (!material)
                {
                    continue;
                }

                if (ConvertMaterialToUrp(material))
                {
                    updated++;
                }
            }

            if (updated > 0)
            {
                AssetDatabase.SaveAssets();
                Debug.Log($"[SSA] {updated} materiais convertidos para URP.");
            }
        }

        private static bool ConvertMaterialToUrp(Material material)
        {
            if (!material)
            {
                return false;
            }

            var shaderName = material.shader ? material.shader.name : string.Empty;
            var changed = false;

            if (string.IsNullOrEmpty(shaderName) || shaderName == "Standard" || shaderName.StartsWith("Legacy Shaders"))
            {
                var target = Shader.Find("Universal Render Pipeline/Lit");
                if (target)
                {
                    SetupMaterialFromStandard(material, target);
                    changed = true;
                }
            }
            else if (!shaderName.Contains("Universal Render Pipeline") && (shaderName.Contains("Particles") || shaderName.Contains("Unlit")))
            {
                var target = Shader.Find("Universal Render Pipeline/Unlit");
                if (target)
                {
                    SetupMaterialUnlit(material, target);
                    changed = true;
                }
            }

            return changed;
        }

        private static void SetupMaterialFromStandard(Material material, Shader target)
        {
            var mainTex = material.HasProperty("_MainTex") ? material.GetTexture("_MainTex") : null;
            var mainScale = material.HasProperty("_MainTex") ? material.GetTextureScale("_MainTex") : Vector2.one;
            var color = material.HasProperty("_Color") ? material.GetColor("_Color") : Color.white;
            var metallic = material.HasProperty("_Metallic") ? material.GetFloat("_Metallic") : 0f;
            var smoothness = 0.5f;
            if (material.HasProperty("_Glossiness"))
            {
                smoothness = material.GetFloat("_Glossiness");
            }
            else if (material.HasProperty("_Smoothness"))
            {
                smoothness = material.GetFloat("_Smoothness");
            }

            var emissionColor = Color.black;
            if (material.HasProperty("_EmissionColor") && material.IsKeywordEnabled("_EMISSION"))
            {
                emissionColor = material.GetColor("_EmissionColor");
            }

            var emissionMap = material.HasProperty("_EmissionMap") ? material.GetTexture("_EmissionMap") : null;
            var bump = material.HasProperty("_BumpMap") ? material.GetTexture("_BumpMap") : null;
            var bumpScale = material.HasProperty("_BumpScale") ? material.GetFloat("_BumpScale") : 1f;

            material.shader = target;
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", smoothness);

            if (mainTex)
            {
                material.SetTexture("_BaseMap", mainTex);
                material.SetTextureScale("_BaseMap", mainScale);
            }

            if (bump)
            {
                material.EnableKeyword("_NORMALMAP");
                material.SetTexture("_BumpMap", bump);
                material.SetFloat("_BumpScale", bumpScale);
            }

            if (emissionMap || emissionColor.maxColorComponent > 0.001f)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emissionColor);
                if (emissionMap)
                {
                    material.SetTexture("_EmissionMap", emissionMap);
                }
            }

            EditorUtility.SetDirty(material);
        }

        private static void SetupMaterialUnlit(Material material, Shader target)
        {
            var mainTex = material.HasProperty("_MainTex") ? material.GetTexture("_MainTex") : null;
            var mainScale = material.HasProperty("_MainTex") ? material.GetTextureScale("_MainTex") : Vector2.one;
            var color = material.HasProperty("_Color") ? material.GetColor("_Color") : Color.white;

            material.shader = target;
            material.SetColor("_BaseColor", color);
            if (mainTex)
            {
                material.SetTexture("_BaseMap", mainTex);
                material.SetTextureScale("_BaseMap", mainScale);
            }

            EditorUtility.SetDirty(material);
        }

        private static void EnsureStarterKitMaterials(StarterKitStage stage)
        {
            if (stage == null)
            {
                return;
            }

            const string materialsFolder = "Assets/SSA_Kit/Materials";
            Directory.CreateDirectory(materialsFolder);

            var floorMatPath = Path.Combine(materialsFolder, "SSA_Floor.mat").Replace('\\', '/');
            var columnMatPath = Path.Combine(materialsFolder, "SSA_Column.mat").Replace('\\', '/');

            var floorMat = AssetDatabase.LoadAssetAtPath<Material>(floorMatPath);
            if (!floorMat)
            {
                floorMat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "SSA_Floor" };
                AssetDatabase.CreateAsset(floorMat, floorMatPath);
            }
            else if (floorMat.shader == null || floorMat.shader.name != "Universal Render Pipeline/Lit")
            {
                floorMat.shader = Shader.Find("Universal Render Pipeline/Lit");
            }

            var floorAlbedo = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/SSA_Kit/Floor_Tiles_Warm_Albedo_1024.png");
            var floorNormalPath = "Assets/SSA_Kit/Floor_Tiles_Normal_1024.png";
            EnsureNormalMap(floorNormalPath);
            var floorNormal = AssetDatabase.LoadAssetAtPath<Texture2D>(floorNormalPath);

            if (floorAlbedo)
            {
                floorMat.SetTexture("_BaseMap", floorAlbedo);
                floorMat.SetTextureScale("_BaseMap", new Vector2(2f, 2f));
            }
            floorMat.SetFloat("_Metallic", 0f);
            floorMat.SetFloat("_Smoothness", 0.25f);

            if (floorNormal)
            {
                floorMat.EnableKeyword("_NORMALMAP");
                floorMat.SetTexture("_BumpMap", floorNormal);
                floorMat.SetFloat("_BumpScale", 1f);
            }

            EditorUtility.SetDirty(floorMat);

            if (stage.Floor)
            {
                Undo.RecordObject(stage.Floor, "Aplicar SSA Floor Material");
                stage.Floor.sharedMaterial = floorMat;
            }

            var columnMat = AssetDatabase.LoadAssetAtPath<Material>(columnMatPath);
            if (!columnMat)
            {
                columnMat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "SSA_Column" };
                AssetDatabase.CreateAsset(columnMat, columnMatPath);
            }
            else if (columnMat.shader == null || columnMat.shader.name != "Universal Render Pipeline/Lit")
            {
                columnMat.shader = Shader.Find("Universal Render Pipeline/Lit");
            }

            columnMat.SetFloat("_Metallic", 0f);
            columnMat.SetFloat("_Smoothness", 0.3f);
            columnMat.SetColor("_BaseColor", HexColor("#D6DCE5"));
            EditorUtility.SetDirty(columnMat);

            if (stage.Columns != null)
            {
                foreach (var column in stage.Columns)
                {
                    if (!column)
                    {
                        continue;
                    }

                    Undo.RecordObject(column.transform, "Ajustar coluna SSA");
                    var scale = column.transform.localScale;
                    if (scale.x < 1.8f)
                    {
                        column.transform.localScale = new Vector3(1.8f, scale.y, scale.z);
                    }

                    Undo.RecordObject(column, "Aplicar SSA Column Material");
                    column.sharedMaterial = columnMat;
                }
            }

            AssetDatabase.SaveAssets();
        }

        private static void EnsureNormalMap(string texturePath)
        {
            if (string.IsNullOrEmpty(texturePath))
            {
                return;
            }

            var importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (importer && importer.textureType != TextureImporterType.NormalMap)
            {
                importer.textureType = TextureImporterType.NormalMap;
                importer.sRGBTexture = false;
                importer.SaveAndReimport();
            }
        }
#endif

        private static void EnsureActiveActorEmission(Transform actor)
        {
            if (!actor)
            {
                Debug.LogWarning("[SSA] Starter Kit auto: nenhum personagem ativo detectado para reforçar emissão.");
                return;
            }

            var teal = HexColor("#6FE5FF");
            foreach (var renderer in actor.GetComponentsInChildren<Renderer>(includeInactive: true))
            {
                if (!renderer)
                {
                    continue;
                }

                var materials = renderer.sharedMaterials;
                var modified = false;

                for (int i = 0; i < materials.Length; i++)
                {
                    var mat = materials[i];
                    if (!mat)
                    {
                        continue;
                    }

#if UNITY_RENDER_PIPELINE_URP
                    if (mat.shader == null || !mat.shader.name.Contains("Universal Render Pipeline"))
                    {
                        var target = Shader.Find("Universal Render Pipeline/Lit");
                        if (target)
                        {
                            mat.shader = target;
                            modified = true;
                        }
                    }
#endif
                    mat.EnableKeyword("_EMISSION");
                    mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                    mat.SetColor("_EmissionColor", teal);
                    EditorUtility.SetDirty(mat);
                    modified = true;
                }

                if (modified)
                {
                    Undo.RecordObject(renderer, "Atualizar emission SSA");
                }
            }
        }

        private static void DisableStarterKitPlaceholders(StarterKitStage stage)
        {
            if (stage?.Actor && stage.Actor.name == "SSA_ActorPreview")
            {
                stage.Actor.gameObject.SetActive(false);
            }
        }

        private static Color HexColor(string hex)
        {
            return ColorUtility.TryParseHtmlString(hex, out var color) ? color : Color.white;
        }

#if UNITY_RENDER_PIPELINE_URP
        private static void RunSsaFixMagenta()
        {
            if (!EditorApplication.ExecuteMenuItem("SSA/Consertar Magenta (Converter para URP)"))
            {
                var type = System.Type.GetType("SSAFixMagenta, Assembly-CSharp");
                var method = type?.GetMethod("Run", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                method?.Invoke(null, null);
            }
        }

        private static bool EnsureUrpPipelineActive()
        {
            if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset existing && existing)
            {
                return true;
            }

            UniversalRenderPipelineAsset pipeline = null;
            var guid = AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset").FirstOrDefault();
            if (!string.IsNullOrEmpty(guid))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
            }

            if (!pipeline)
            {
                const string folder = "Assets/SSA_Kit/Generated";
                Directory.CreateDirectory(folder);
                const string assetPath = folder + "/SSA_StarterKit_URP.asset";

                pipeline = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
                pipeline.name = "SSA_StarterKit_URP";

                var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                renderer.name = "SSA_StarterKit_URP_Renderer";

                AssetDatabase.CreateAsset(pipeline, assetPath);
                AssetDatabase.AddObjectToAsset(renderer, pipeline);

                ConfigurePipelineRendererList(pipeline, renderer);
                EditorUtility.SetDirty(pipeline);
                AssetDatabase.SaveAssets();
            }

            if (!pipeline)
            {
                Debug.LogError("[SSA] Não foi possível criar ou localizar um UniversalRenderPipelineAsset.");
                return false;
            }

            GraphicsSettings.defaultRenderPipeline = pipeline;
            ApplyPipelineToQualityLevels(pipeline);
            QualitySettings.renderPipeline = pipeline;

            Debug.Log("[SSA] Universal Render Pipeline ativo para o Starter Kit.");
            return true;
        }

        private static void ConfigurePipelineRendererList(UniversalRenderPipelineAsset pipeline, UniversalRendererData renderer)
        {
            var so = new SerializedObject(pipeline);
            var rendererList = so.FindProperty("m_RendererDataList");
            if (rendererList != null)
            {
                rendererList.arraySize = 1;
                rendererList.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
            }

            var defaultIndex = so.FindProperty("m_DefaultRendererIndex");
            if (defaultIndex != null)
            {
                defaultIndex.intValue = 0;
            }

            so.ApplyModifiedProperties();
        }

        private static void ApplyPipelineToQualityLevels(RenderPipelineAsset pipeline)
        {
            var currentLevel = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.count; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = pipeline;
            }
            QualitySettings.SetQualityLevel(currentLevel, true);
        }
#endif

        private sealed class StarterKitStage
        {
            public StarterKitStage(GameObject root, Renderer floor, Renderer[] columns, Transform actor, Camera camera, Light sun)
            {
                Root = root;
                Floor = floor;
                Columns = columns ?? System.Array.Empty<Renderer>();
                Actor = actor;
                Camera = camera;
                Sun = sun;
            }

            public GameObject Root { get; }
            public Renderer Floor { get; }
            public Renderer[] Columns { get; }
            public Transform Actor { get; }
            public Camera Camera { get; }
            public Light Sun { get; }
        }

        private readonly struct ProgressScope : System.IDisposable
        {
            private readonly string _title;

            public ProgressScope(string title)
            {
                _title = title;
                EditorUtility.DisplayProgressBar(_title, string.Empty, 0f);
            }

            public void Report(float progress, string info)
            {
                EditorUtility.DisplayProgressBar(_title, info, Mathf.Clamp01(progress));
            }

            public void Dispose()
            {
                EditorUtility.ClearProgressBar();
            }
        }
    }
}
