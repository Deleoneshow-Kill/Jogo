using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace JogoSSA.Editor
{
    /// <summary>
    /// Monta automaticamente o prefab final do Gemini Arc a partir de um FBX já importado.
    /// Ajusta materiais SSA, cria LODGroup (quando houver LODs nomeados) e adiciona Cloth e coliders básicos para a capa.
    /// </summary>
    public static class SSAGeminiArcAssembler
    {
        private const string MenuPath = "Jogo/Characters/Assemble Gemini Arc Prefab";

        private const string CharactersRoot = "Assets/Characters";
        private const string CharacterFolder = CharactersRoot + "/GeminiArc";
        private const string PrefabFolder = CharacterFolder + "/Prefabs";
        private const string MaterialsFolder = CharacterFolder + "/Materials";
        private const string FinalPrefabPath = PrefabFolder + "/GeminiArc.prefab";

        private const string ArmorMaterialPath = MaterialsFolder + "/Mat_GeminiArc.mat";
        private const string CapeMaterialPath = MaterialsFolder + "/Mat_GeminiArc_Cape.mat";
        private const string SkinMaterialPath = "Assets/JogoSSA/Materials/M_Char_Skin_Toon.mat";
        private const string OutlineMaterialPath = "Assets/JogoSSA/Materials/M_Outline_Black.mat";

        private static readonly string[] ArmorKeywords = { "armor", "armour", "gold", "metal", "helmet", "helm", "shield", "plate", "greave", "gauntlet" };
        private static readonly string[] CapeKeywords = { "cape", "cloak" };
        private static readonly string[] SkinKeywords = { "skin", "body", "face", "hand", "head", "leg", "foot" };
        private static readonly string[] HairKeywords = { "hair" };

        private const string CapeRendererKeyword = "cape";

        [MenuItem(MenuPath, priority = 75)]
        private static void ShowWizard()
        {
            DisplayWizard();
        }

        private static void DisplayWizard()
        {
            var wizard = ScriptableWizard.DisplayWizard<AssemblerWizard>(
                "Montar Gemini Arc",
                "Montar Prefab");
            wizard.minSize = new Vector2(480f, 220f);
        }

        private class AssemblerWizard : ScriptableWizard
        {
            public GameObject sourceModel;
            public bool addCloth = true;
            public bool createColliders = true;
            public bool addAnimator = true;

            private string _status;

            private void OnEnable()
            {
                helpString = "Selecione o FBX (ou prefab importado) do Gemini Arc. O processo gera/atualiza Assets/Characters/GeminiArc/GeminiArc.prefab.";
            }

            private void OnWizardUpdate()
            {
                bool valid = sourceModel != null;
                isValid = valid;
                _status = valid ? string.Empty : "Informe o FBX ou prefab de origem.";
                errorString = _status;
            }

            private void OnWizardCreate()
            {
                try
                {
                    Assemble(sourceModel, addCloth, createColliders, addAnimator);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Gemini Arc Assembler falhou: {ex.Message}\n{ex.StackTrace}");
                    EditorUtility.DisplayDialog("Erro", "Não foi possível montar o prefab. Verifique o Console para detalhes.", "OK");
                }
            }
        }

        private static void Assemble(GameObject sourceModel, bool addCloth, bool createColliders, bool addAnimator)
        {
            if (sourceModel == null)
            {
                throw new ArgumentNullException(nameof(sourceModel));
            }

            var armorMat = AssetDatabase.LoadAssetAtPath<Material>(ArmorMaterialPath);
            var capeMat = AssetDatabase.LoadAssetAtPath<Material>(CapeMaterialPath);
            var skinMat = AssetDatabase.LoadAssetAtPath<Material>(SkinMaterialPath);
            var outlineMat = AssetDatabase.LoadAssetAtPath<Material>(OutlineMaterialPath);

            if (armorMat == null || capeMat == null || skinMat == null || outlineMat == null)
            {
                throw new InvalidOperationException("Materiais SSA necessários não encontrados. Rode 'Jogo -> Setup SSA (Toon)' e 'Jogo/Characters/Create Gemini Arc Template' antes.");
            }

            EnsureFolder("Assets", "Characters");
            EnsureFolder(CharactersRoot, "GeminiArc");
            EnsureFolder(CharacterFolder, "Materials");
            EnsureFolder(CharacterFolder, "Prefabs");

            var tempRoot = new GameObject("GeminiArc");
            tempRoot.transform.position = Vector3.zero;
            tempRoot.transform.rotation = Quaternion.identity;
            tempRoot.transform.localScale = Vector3.one;

            var instanceObj = PrefabUtility.InstantiatePrefab(sourceModel) as GameObject;
            if (instanceObj == null)
            {
                UnityEngine.Object.DestroyImmediate(tempRoot);
                throw new InvalidOperationException("Não foi possível instanciar o modelo selecionado.");
            }

            instanceObj.name = sourceModel.name;
            instanceObj.transform.SetParent(tempRoot.transform, false);
            instanceObj.transform.localPosition = Vector3.zero;
            instanceObj.transform.localRotation = Quaternion.identity;
            instanceObj.transform.localScale = Vector3.one;

            PrefabUtility.UnpackPrefabInstance(instanceObj, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

            tempRoot.AddComponent<SSAOutlineAutoScale>();

            if (addAnimator)
            {
                EnsureAnimator(tempRoot, instanceObj, sourceModel);
            }

            AssignMaterials(tempRoot, armorMat, capeMat, skinMat, outlineMat);

            ConfigureLods(tempRoot);

            if (addCloth)
            {
                SetupCapeCloth(tempRoot, capeMat, outlineMat, createColliders);
            }

            PrefabUtility.SaveAsPrefabAsset(tempRoot, FinalPrefabPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(FinalPrefabPath);

            UnityEngine.Object.DestroyImmediate(tempRoot);

            EditorUtility.DisplayDialog(
                "Gemini Arc montado",
                "Prefab final criado/atualizado em Assets/Characters/GeminiArc/Prefabs/GeminiArc.prefab.",
                "Feito");
        }

        private static void AssignMaterials(GameObject root, Material armorMat, Material capeMat, Material skinMat, Material outlineMat)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                var originalMats = renderer.sharedMaterials;
                if (originalMats == null || originalMats.Length == 0)
                {
                    continue;
                }

                var baseMats = new Material[originalMats.Length];
                for (int i = 0; i < baseMats.Length; i++)
                {
                    baseMats[i] = ResolveMaterial(renderer, originalMats[i], armorMat, capeMat, skinMat);
                }

                Material[] combinedMats;
                if (outlineMat != null)
                {
                    combinedMats = new Material[baseMats.Length + 1];
                    Array.Copy(baseMats, combinedMats, baseMats.Length);
                    combinedMats[combinedMats.Length - 1] = outlineMat;
                }
                else
                {
                    combinedMats = baseMats;
                }

                renderer.sharedMaterials = combinedMats;

                for (int i = 0; i < baseMats.Length; i++)
                {
                    if (baseMats[i] == null || !baseMats[i].HasProperty("_BaseColor"))
                    {
                        continue;
                    }

                    var color = TryGetBaseColor(originalMats[i]) ?? GetDefaultColor(baseMats[i], armorMat, capeMat, skinMat);
                    var block = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(block, i);
                    block.SetColor("_BaseColor", color);
                    renderer.SetPropertyBlock(block, i);
                }
            }
        }

        private static Material ResolveMaterial(Renderer renderer, Material originalMat, Material armorMat, Material capeMat, Material skinMat)
        {
            var candidate = originalMat != null ? originalMat.name : renderer.name;
            var lower = candidate.ToLowerInvariant();
            var rendererLower = renderer.name.ToLowerInvariant();

            if (ContainsKeyword(lower, CapeKeywords) || ContainsKeyword(rendererLower, CapeKeywords))
            {
                return capeMat;
            }

            if (ContainsKeyword(lower, HairKeywords) || rendererLower.Contains("hair"))
            {
                return skinMat;
            }

            if (ContainsKeyword(lower, SkinKeywords) || ContainsKeyword(rendererLower, SkinKeywords))
            {
                return skinMat;
            }

            if (ContainsKeyword(lower, ArmorKeywords) || ContainsKeyword(rendererLower, ArmorKeywords))
            {
                return armorMat;
            }

            return armorMat;
        }

        private static bool ContainsKeyword(string value, IEnumerable<string> keywords)
        {
            foreach (var keyword in keywords)
            {
                if (value.Contains(keyword))
                {
                    return true;
                }
            }

            return false;
        }

        private static Color GetDefaultColor(Material mat, Material armorMat, Material capeMat, Material skinMat)
        {
            if (mat == skinMat)
            {
                return new Color(1.0f, 0.86f, 0.78f);
            }

            if (mat == capeMat)
            {
                return new Color(0.16f, 0.08f, 0.35f);
            }

            return new Color(0.95f, 0.78f, 0.22f);
        }

        private static Color? TryGetBaseColor(Material mat)
        {
            if (mat != null)
            {
                if (mat.HasProperty("_BaseColor"))
                {
                    return mat.GetColor("_BaseColor");
                }

                if (mat.HasProperty("_Color"))
                {
                    return mat.GetColor("_Color");
                }
            }

            return null;
        }

        private static void ConfigureLods(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            var lodMap = new Dictionary<int, List<Renderer>>();

            foreach (var renderer in renderers)
            {
                int index = GetLodIndex(renderer.name);
                if (index < 0)
                {
                    continue;
                }

                if (!lodMap.TryGetValue(index, out var list))
                {
                    list = new List<Renderer>();
                    lodMap[index] = list;
                }

                list.Add(renderer);
            }

            if (!lodMap.ContainsKey(0))
            {
                return;
            }

            var lodGroup = root.GetComponent<LODGroup>();
            if (lodGroup == null)
            {
                lodGroup = root.AddComponent<LODGroup>();
            }

            var lods = new List<LOD>();
            if (lodMap.TryGetValue(0, out var lod0))
            {
                lods.Add(new LOD(0.6f, lod0.ToArray()));
            }
            if (lodMap.TryGetValue(1, out var lod1))
            {
                lods.Add(new LOD(0.35f, lod1.ToArray()));
            }
            if (lodMap.TryGetValue(2, out var lod2))
            {
                lods.Add(new LOD(0.15f, lod2.ToArray()));
            }

            if (lods.Count > 0)
            {
                lodGroup.SetLODs(lods.ToArray());
                lodGroup.RecalculateBounds();
            }
        }

        private static int GetLodIndex(string rendererName)
        {
            if (string.IsNullOrEmpty(rendererName))
            {
                return -1;
            }

            string lower = rendererName.ToLowerInvariant();
            if (lower.Contains("lod0") || lower.EndsWith("_0"))
            {
                return 0;
            }

            if (lower.Contains("lod1") || lower.EndsWith("_1"))
            {
                return 1;
            }

            if (lower.Contains("lod2") || lower.EndsWith("_2"))
            {
                return 2;
            }

            return -1;
        }

        private static void SetupCapeCloth(GameObject root, Material capeMat, Material outlineMat, bool createColliders)
        {
            var capeRenderer = root.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .FirstOrDefault(r => r.name.ToLowerInvariant().Contains(CapeRendererKeyword));

            if (capeRenderer == null)
            {
                Debug.LogWarning("Nenhum SkinnedMeshRenderer contendo 'Cape' encontrado para configurar Cloth.");
                return;
            }

            if (!Array.Exists(capeRenderer.sharedMaterials, m => m == capeMat))
            {
                var mats = capeRenderer.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null)
                    {
                        continue;
                    }

                    if (mats[i].name.ToLowerInvariant().Contains("cape"))
                    {
                        mats[i] = capeMat;
                    }
                }
                capeRenderer.sharedMaterials = mats;
            }

            var cloth = capeRenderer.GetComponent<Cloth>();
            if (cloth == null)
            {
                cloth = capeRenderer.gameObject.AddComponent<Cloth>();
            }

            cloth.enableContinuousCollision = true;
            cloth.friction = 0.3f;
            cloth.damping = 0.25f;
            cloth.stretchingStiffness = 0.6f;
            cloth.bendingStiffness = 0.5f;
            cloth.worldVelocityScale = 0.2f;
            cloth.useGravity = true;

            if (createColliders)
            {
                var colliders = BuildClothColliders(root.transform);
                if (colliders.Count > 0)
                {
                    cloth.capsuleColliders = colliders.ToArray();
                }
            }

            Debug.Log("Cloth configurado na capa. Ajuste os pesos no Paint Cloth para definir os pinos nos ombros.");
        }

        private struct BoneColliderConfig
        {
            public BoneColliderConfig(string name, Vector3 center, float radius, float height, int direction)
            {
                Name = name;
                Center = center;
                Radius = radius;
                Height = height;
                Direction = direction;
            }

            public string Name { get; }
            public Vector3 Center { get; }
            public float Radius { get; }
            public float Height { get; }
            public int Direction { get; }
        }

        private static readonly BoneColliderConfig[] ColliderConfigs =
        {
            new BoneColliderConfig("hips", new Vector3(0f, 0.1f, 0f), 0.18f, 0.45f, 1),
            new BoneColliderConfig("spine", new Vector3(0f, 0.15f, 0f), 0.16f, 0.5f, 1),
            new BoneColliderConfig("spine1", new Vector3(0f, 0.1f, 0f), 0.15f, 0.42f, 1),
            new BoneColliderConfig("spine2", new Vector3(0f, 0.08f, 0f), 0.14f, 0.38f, 1),
            new BoneColliderConfig("leftshoulder", new Vector3(0f, 0f, 0f), 0.12f, 0.35f, 2),
            new BoneColliderConfig("rightshoulder", new Vector3(0f, 0f, 0f), 0.12f, 0.35f, 2),
            new BoneColliderConfig("leftarm", new Vector3(0f, 0f, 0f), 0.1f, 0.4f, 2),
            new BoneColliderConfig("rightarm", new Vector3(0f, 0f, 0f), 0.1f, 0.4f, 2)
        };

        private static List<CapsuleCollider> BuildClothColliders(Transform root)
        {
            var colliders = new List<CapsuleCollider>();
            var existing = new HashSet<Transform>();

            foreach (var config in ColliderConfigs)
            {
                var bone = FindBone(root, config.Name);
                if (bone == null || existing.Contains(bone))
                {
                    continue;
                }

                var collider = bone.GetComponent<CapsuleCollider>();
                if (collider == null)
                {
                    collider = bone.gameObject.AddComponent<CapsuleCollider>();
                }

                collider.center = config.Center;
                collider.radius = config.Radius;
                collider.height = config.Height;
                collider.direction = config.Direction;

                colliders.Add(collider);
                existing.Add(bone);
            }

            return colliders;
        }

        private static Transform FindBone(Transform root, string nameSnippet)
        {
            var lowerSnippet = nameSnippet.ToLowerInvariant();
            var stack = new Stack<Transform>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                var current = stack.Pop();
                var lowerName = current.name.ToLowerInvariant();
                if (lowerName.Contains(lowerSnippet))
                {
                    return current;
                }

                for (int i = 0; i < current.childCount; i++)
                {
                    stack.Push(current.GetChild(i));
                }
            }

            return null;
        }

        private static void EnsureAnimator(GameObject root, GameObject instance, GameObject sourceModel)
        {
            if (instance != null)
            {
                var instanceAnimator = instance.GetComponent<Animator>();
                if (instanceAnimator != null)
                {
                    var rootAnimator = root.GetComponent<Animator>();
                    if (rootAnimator == null)
                    {
                        rootAnimator = root.AddComponent<Animator>();
                    }

                    rootAnimator.runtimeAnimatorController = instanceAnimator.runtimeAnimatorController;
                    rootAnimator.avatar = instanceAnimator.avatar;
                    rootAnimator.applyRootMotion = instanceAnimator.applyRootMotion;
                    rootAnimator.updateMode = instanceAnimator.updateMode;
                    rootAnimator.cullingMode = instanceAnimator.cullingMode;

                    UnityEngine.Object.DestroyImmediate(instanceAnimator);
                    return;
                }
            }

            var animator = root.GetComponent<Animator>();
            if (animator == null)
            {
                animator = root.AddComponent<Animator>();
            }

            var sourceAnimator = sourceModel.GetComponent<Animator>();
            if (sourceAnimator != null && sourceAnimator.avatar != null)
            {
                animator.runtimeAnimatorController = sourceAnimator.runtimeAnimatorController;
                animator.avatar = sourceAnimator.avatar;
                animator.applyRootMotion = sourceAnimator.applyRootMotion;
                animator.updateMode = sourceAnimator.updateMode;
                animator.cullingMode = sourceAnimator.cullingMode;
                return;
            }

            var assetPath = AssetDatabase.GetAssetPath(sourceModel);
            if (string.IsNullOrEmpty(assetPath))
            {
                return;
            }

            var assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            foreach (var asset in assets)
            {
                if (asset is Avatar avatarAsset && avatarAsset.isHuman && avatarAsset.isValid)
                {
                    animator.avatar = avatarAsset;
                    return;
                }
            }
        }

        private static void EnsureFolder(string parent, string child)
        {
            var combined = Path.Combine(parent, child).Replace("\\", "/");
            if (!AssetDatabase.IsValidFolder(combined))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }
    }
}
