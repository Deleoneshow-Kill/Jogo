using System.IO;
using UnityEditor;
using UnityEngine;

namespace JogoSSA.Editor
{
    /// <summary>
    /// Gera pastas, materiais base e um prefab placeholder para o personagem Gemini Arc.
    /// Assim os artistas têm um destino pronto para o FBX final enquanto o time visualiza um substituto dentro da cena.
    /// </summary>
    public static class SSAGeminiArcTemplate
    {
        private const string MenuPath = "Jogo/Characters/Create Gemini Arc Template";

        private const string BaseSkinMatPath = "Assets/JogoSSA/Materials/M_Char_Skin_Toon.mat";
        private const string BaseArmorMatPath = "Assets/JogoSSA/Materials/M_Armor_Gold_Toon.mat";
        private const string BaseOutlineMatPath = "Assets/JogoSSA/Materials/M_Outline_Black.mat";

        private const string CharactersRoot = "Assets/Characters";
        private const string CharacterFolder = CharactersRoot + "/GeminiArc";
        private const string FbxFolder = CharacterFolder + "/FBX";
        private const string TexturesFolder = CharacterFolder + "/Textures";
        private const string MaterialsFolder = CharacterFolder + "/Materials";
        private const string PrefabsFolder = CharacterFolder + "/Prefabs";

        private const string ArmorMaterialPath = MaterialsFolder + "/Mat_GeminiArc.mat";
        private const string CapeMaterialPath = MaterialsFolder + "/Mat_GeminiArc_Cape.mat";
        private const string PrefabPath = PrefabsFolder + "/GeminiArcPlaceholder.prefab";

        [MenuItem(MenuPath, priority = 70)]
        private static void CreateTemplate()
        {
            var skinMat = AssetDatabase.LoadAssetAtPath<Material>(BaseSkinMatPath);
            var armorMat = AssetDatabase.LoadAssetAtPath<Material>(BaseArmorMatPath);
            var outlineMat = AssetDatabase.LoadAssetAtPath<Material>(BaseOutlineMatPath);

            if (skinMat == null || armorMat == null || outlineMat == null)
            {
                EditorUtility.DisplayDialog(
                    "Assets SSA ausentes",
                    "Execute primeiro 'Jogo -> Setup SSA (Toon)' para gerar os materiais base (pele, armadura e outline).",
                    "OK");
                return;
            }

            EnsureFolder("Assets", "Characters");
            EnsureFolder(CharactersRoot, "GeminiArc");
            EnsureFolder(CharacterFolder, "FBX");
            EnsureFolder(CharacterFolder, "Textures");
            EnsureFolder(CharacterFolder, "Materials");
            EnsureFolder(CharacterFolder, "Prefabs");

            var gemArmorMat = CreateOrUpdateGeminiArmorMaterial(armorMat);
            var capeMat = CreateOrUpdateGeminiCapeMaterial(skinMat);

            CreateOrUpdatePrefab(gemArmorMat, skinMat, capeMat, outlineMat);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Gemini Arc pronto",
                "Estrutura criada em Assets/Characters/GeminiArc com materiais e prefab placeholder.",
                "Beleza");
        }

        private static void CreateOrUpdatePrefab(Material armorMat, Material skinMat, Material capeMat, Material outlineMat)
        {
            var tempRoot = new GameObject("GeminiArcPlaceholder");
            tempRoot.transform.position = Vector3.zero;
            tempRoot.transform.rotation = Quaternion.identity;
            tempRoot.transform.localScale = Vector3.one;

            tempRoot.AddComponent<SSAOutlineAutoScale>();

            CreatePart(tempRoot.transform, "Body", PrimitiveType.Capsule, new Vector3(0f, 1.1f, 0f), new Vector3(0.55f, 1.5f, 0.55f), skinMat, outlineMat, new Color(1.0f, 0.86f, 0.78f));
            CreatePart(tempRoot.transform, "TorsoArmor", PrimitiveType.Cylinder, new Vector3(0f, 1.4f, 0f), new Vector3(0.75f, 0.55f, 0.55f), armorMat, outlineMat, new Color(0.95f, 0.78f, 0.22f), Quaternion.Euler(90f, 0f, 0f));
            CreatePart(tempRoot.transform, "ChestPlate", PrimitiveType.Cube, new Vector3(0f, 1.65f, 0.12f), new Vector3(0.9f, 0.45f, 0.3f), armorMat, outlineMat, new Color(0.92f, 0.74f, 0.18f));
            CreatePart(tempRoot.transform, "BeltGem", PrimitiveType.Cylinder, new Vector3(0f, 1.0f, 0.2f), new Vector3(0.3f, 0.12f, 0.3f), armorMat, outlineMat, new Color(0.7f, 0.45f, 1.0f), Quaternion.Euler(90f, 0f, 0f));

            CreatePart(tempRoot.transform, "Head", PrimitiveType.Sphere, new Vector3(0f, 2.15f, 0.08f), new Vector3(0.52f, 0.52f, 0.52f), skinMat, outlineMat, new Color(1.0f, 0.86f, 0.78f));
            CreatePart(tempRoot.transform, "Helm_Crown", PrimitiveType.Sphere, new Vector3(0f, 2.3f, 0f), new Vector3(0.72f, 0.35f, 0.72f), armorMat, outlineMat, new Color(0.98f, 0.82f, 0.22f));
            CreatePart(tempRoot.transform, "Helm_FaceGuard", PrimitiveType.Cube, new Vector3(0f, 2.15f, 0.32f), new Vector3(0.8f, 0.42f, 0.12f), armorMat, outlineMat, new Color(0.88f, 0.68f, 0.18f));
            CreatePart(tempRoot.transform, "Gem_Front", PrimitiveType.Sphere, new Vector3(0f, 2.24f, 0.44f), new Vector3(0.16f, 0.16f, 0.16f), armorMat, outlineMat, new Color(0.55f, 0.35f, 1.0f));

            CreateArm(tempRoot.transform, armorMat, skinMat, outlineMat, true);
            CreateArm(tempRoot.transform, armorMat, skinMat, outlineMat, false);

            CreateLeg(tempRoot.transform, armorMat, outlineMat, true);
            CreateLeg(tempRoot.transform, armorMat, outlineMat, false);

            CreateCape(tempRoot.transform, capeMat, outlineMat);

            PrefabUtility.SaveAsPrefabAssetAndConnect(tempRoot, PrefabPath, InteractionMode.AutomatedAction);

            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);

            Object.DestroyImmediate(tempRoot);
        }

        private static void CreateArm(Transform parent, Material armorMat, Material skinMat, Material outlineMat, bool left)
        {
            float side = left ? -1f : 1f;
            Vector3 shoulderPos = new Vector3(0.7f * side, 1.6f, 0f);
            Vector3 elbowPos = new Vector3(0.95f * side, 1.2f, 0.05f);
            Vector3 handPos = new Vector3(1.0f * side, 0.9f, 0.12f);

            CreatePart(parent, $"Shoulder_{(left ? "L" : "R")}", PrimitiveType.Sphere, shoulderPos, new Vector3(0.32f, 0.32f, 0.32f), armorMat, outlineMat, new Color(0.94f, 0.78f, 0.2f));
            CreatePart(parent, $"UpperArm_{(left ? "L" : "R")}", PrimitiveType.Capsule, (shoulderPos + elbowPos) * 0.5f, new Vector3(0.22f, 0.6f, 0.22f), armorMat, outlineMat, new Color(0.9f, 0.72f, 0.16f), Quaternion.Euler(0f, 0f, 15f * -side));
            CreatePart(parent, $"Forearm_{(left ? "L" : "R")}", PrimitiveType.Capsule, (elbowPos + handPos) * 0.5f, new Vector3(0.21f, 0.55f, 0.21f), armorMat, outlineMat, new Color(0.96f, 0.8f, 0.24f), Quaternion.Euler(0f, 0f, -10f * -side));
            CreatePart(parent, $"Hand_{(left ? "L" : "R")}", PrimitiveType.Sphere, handPos, new Vector3(0.24f, 0.24f, 0.24f), skinMat, outlineMat, new Color(1.0f, 0.86f, 0.78f));
        }

        private static void CreateLeg(Transform parent, Material armorMat, Material outlineMat, bool left)
        {
            float side = left ? -1f : 1f;
            Vector3 hipPos = new Vector3(0.25f * side, 0.95f, 0f);
            Vector3 kneePos = hipPos + new Vector3(0f, -0.6f, 0.08f);
            Vector3 footPos = hipPos + new Vector3(0f, -1.3f, 0.35f);

            CreatePart(parent, $"Thigh_{(left ? "L" : "R")}", PrimitiveType.Capsule, (hipPos + kneePos) * 0.5f, new Vector3(0.28f, 0.75f, 0.28f), armorMat, outlineMat, new Color(0.92f, 0.74f, 0.18f), Quaternion.Euler(2f, 0f, 0f));
            CreatePart(parent, $"Knee_{(left ? "L" : "R")}", PrimitiveType.Sphere, kneePos, new Vector3(0.24f, 0.24f, 0.24f), armorMat, outlineMat, new Color(0.98f, 0.82f, 0.24f));
            CreatePart(parent, $"Calf_{(left ? "L" : "R")}", PrimitiveType.Capsule, (kneePos + footPos) * 0.5f, new Vector3(0.24f, 0.65f, 0.24f), armorMat, outlineMat, new Color(0.92f, 0.74f, 0.18f), Quaternion.Euler(-3f, 0f, 0f));
            CreatePart(parent, $"Boot_{(left ? "L" : "R")}", PrimitiveType.Cube, footPos, new Vector3(0.32f, 0.18f, 0.52f), armorMat, outlineMat, new Color(0.95f, 0.78f, 0.22f));
        }

        private static void CreateCape(Transform parent, Material capeMat, Material outlineMat)
        {
            var cape = CreatePart(parent, "Cape", PrimitiveType.Quad, new Vector3(0f, 1.45f, -0.45f), new Vector3(2.0f, 2.6f, 1f), capeMat, outlineMat, new Color(0.2f, 0.1f, 0.4f));
            cape.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
        }

        private static GameObject CreatePart(Transform parent, string name, PrimitiveType type, Vector3 localPos, Vector3 localScale, Material baseMat, Material outlineMat, Color tint, Quaternion? localRot = null)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            go.transform.localRotation = localRot ?? Quaternion.identity;

            var collider = go.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }

            var renderer = go.GetComponent<Renderer>();
            ApplyMaterials(renderer, baseMat, outlineMat, tint);

            return go;
        }

        private static void ApplyMaterials(Renderer renderer, Material baseMat, Material outlineMat, Color tint)
        {
            if (renderer == null || baseMat == null)
            {
                return;
            }

            var mats = outlineMat != null
                ? new[] { baseMat, outlineMat }
                : new[] { baseMat };

            renderer.sharedMaterials = mats;

            if (baseMat.HasProperty("_BaseColor"))
            {
                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block, 0);
                block.SetColor("_BaseColor", tint);
                renderer.SetPropertyBlock(block, 0);
            }
        }

        private static Material CreateOrUpdateGeminiArmorMaterial(Material sourceArmor)
        {
            var gemMat = AssetDatabase.LoadAssetAtPath<Material>(ArmorMaterialPath);
            if (gemMat == null)
            {
                bool copied = AssetDatabase.CopyAsset(BaseArmorMatPath, ArmorMaterialPath);
                if (!copied)
                {
                    gemMat = Object.Instantiate(sourceArmor);
                    AssetDatabase.CreateAsset(gemMat, ArmorMaterialPath);
                }
                gemMat = AssetDatabase.LoadAssetAtPath<Material>(ArmorMaterialPath);
            }

            if (gemMat.HasProperty("_BaseColor"))
            {
                gemMat.SetColor("_BaseColor", new Color(0.95f, 0.78f, 0.22f));
            }
            if (gemMat.HasProperty("_SpecIntensity"))
            {
                gemMat.SetFloat("_SpecIntensity", 0.92f);
            }
            if (gemMat.HasProperty("_RimIntensity"))
            {
                gemMat.SetFloat("_RimIntensity", 0.4f);
            }

            EditorUtility.SetDirty(gemMat);
            return gemMat;
        }

        private static Material CreateOrUpdateGeminiCapeMaterial(Material sourceSkin)
        {
            var capeMat = AssetDatabase.LoadAssetAtPath<Material>(CapeMaterialPath);
            if (capeMat == null)
            {
                bool copied = AssetDatabase.CopyAsset(BaseSkinMatPath, CapeMaterialPath);
                if (!copied)
                {
                    capeMat = Object.Instantiate(sourceSkin);
                    AssetDatabase.CreateAsset(capeMat, CapeMaterialPath);
                }
                capeMat = AssetDatabase.LoadAssetAtPath<Material>(CapeMaterialPath);
            }

            if (capeMat.HasProperty("_BaseColor"))
            {
                capeMat.SetColor("_BaseColor", new Color(0.16f, 0.08f, 0.35f));
            }
            if (capeMat.HasProperty("_SpecIntensity"))
            {
                capeMat.SetFloat("_SpecIntensity", 0.45f);
            }
            if (capeMat.HasProperty("_RimIntensity"))
            {
                capeMat.SetFloat("_RimIntensity", 0.3f);
            }

            EditorUtility.SetDirty(capeMat);
            return capeMat;
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
