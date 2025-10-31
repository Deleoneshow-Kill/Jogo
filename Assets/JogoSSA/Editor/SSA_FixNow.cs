#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class SSAFixNow
{
    private const string MaterialsFolder = "Assets/JogoSSA/Materials";
    private const string SkinMatPath = MaterialsFolder + "/M_Char_Skin_Toon.mat";
    private const string ArmorMatPath = MaterialsFolder + "/M_Armor_Gold_Toon.mat";
    private const string OutlineMatPath = MaterialsFolder + "/M_Outline_Black.mat";
    private const string ProfileFolder = "Assets/JogoSSA/Profiles";
    private const string ProfilePath = ProfileFolder + "/SSA_GlobalProfile.asset";

    [MenuItem("Jogo/SSA: Corrigir Cena Agora", priority = 40)]
    public static void FixScene()
    {
        var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (urp == null)
        {
            EditorUtility.DisplayDialog(
                "URP não ativo",
                "O Universal Render Pipeline está INSTALADO, mas não está ATIVO.\nUse o menu: Jogo > Ativar URP (Assign).",
                "OK");
            return;
        }

        if (PlayerSettings.colorSpace != ColorSpace.Linear)
        {
            PlayerSettings.colorSpace = ColorSpace.Linear;
        }

        FixRampImport("Assets/Shaders/Toon/Ramps");

        Directory.CreateDirectory(MaterialsFolder);
        var skinMat = GetOrCreateMaterial(SkinMatPath, "Toon/CharacterURP");
        var armorMat = GetOrCreateMaterial(ArmorMatPath, "Toon/CharacterURP");
        var outlineMat = GetOrCreateMaterial(OutlineMatPath, "Toon/Outline");

        var rampSkin = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Shaders/Toon/Ramps/ramp_skin.png");
        var rampArmor = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Shaders/Toon/Ramps/ramp_armor.png");
        var matcap = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Shaders/Toon/MatCaps/matcap_gold.png");

        if (skinMat && rampSkin)
        {
            skinMat.SetTexture("_RampTex", rampSkin);
            skinMat.SetFloat("_RampBias", 0.05f);
            skinMat.SetFloat("_SpecIntensity", 0.6f);
            skinMat.SetFloat("_RimIntensity", 0.30f);
            skinMat.SetFloat("_RimPower", 2.2f);
            if (matcap && skinMat.HasProperty("_MatcapTex"))
            {
                skinMat.SetTexture("_MatcapTex", matcap);
            }
        }

        if (armorMat && rampArmor)
        {
            armorMat.SetTexture("_RampTex", rampArmor);
            armorMat.SetFloat("_RampBias", 0.02f);
            armorMat.SetFloat("_SpecIntensity", 0.90f);
            armorMat.SetFloat("_RimIntensity", 0.35f);
            armorMat.SetFloat("_RimPower", 2.0f);
            if (matcap && armorMat.HasProperty("_MatcapTex"))
            {
                armorMat.SetTexture("_MatcapTex", matcap);
            }
        }

        if (outlineMat && outlineMat.HasProperty("_Thickness"))
        {
            outlineMat.SetFloat("_Thickness", 0.004f);
        }

        Directory.CreateDirectory(ProfileFolder);

        var volumeGO = GameObject.Find("SSA Global Volume");
        if (!volumeGO)
        {
            volumeGO = new GameObject("SSA Global Volume");
        }

        var volume = volumeGO.GetComponent<Volume>();
        if (!volume)
        {
            volume = volumeGO.AddComponent<Volume>();
        }
        volume.isGlobal = true;

        var profile = volume.sharedProfile;
        if (!profile)
        {
            profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            if (!profile)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, ProfilePath);
            }
            volume.sharedProfile = profile;
        }

        AddOrGet(profile, out Tonemapping tonemap);
        tonemap.mode.value = TonemappingMode.ACES;

        AddOrGet(profile, out Bloom bloom);
        bloom.threshold.value = 1.20f;
        bloom.intensity.value = 0.50f;

        AddOrGet(profile, out ColorAdjustments ca);
        ca.postExposure.value = 0.20f;
        ca.contrast.value = 10f;
        ca.saturation.value = 8f;

        var light = Object.FindObjectOfType<Light>();
        if (light == null || light.type != LightType.Directional)
        {
            var go = new GameObject("SSA Sun Light");
            light = go.AddComponent<Light>();
            light.type = LightType.Directional;
        }
        light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        light.intensity = 1.2f;
        light.color = Color.white;
        light.shadows = LightShadows.Soft;
        light.shadowBias = 0.05f;
        light.shadowNormalBias = 0.4f;

        var cam = Camera.main;
        if (cam)
        {
            cam.allowHDR = true;
            var ucam = cam.GetUniversalAdditionalCameraData();
            if (ucam != null)
            {
                ucam.renderPostProcessing = true;
                ucam.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
            }
            cam.fieldOfView = 42f;
        }

        var targets = Selection.gameObjects != null && Selection.gameObjects.Length > 0
            ? Selection.gameObjects
            : GameObject.FindObjectsOfType<Transform>()
                .Where(t => NameLooksLikeCharacter(t.name))
                .Select(t => t.gameObject)
                .ToArray();

        if (targets.Length == 0)
        {
            targets = new[]
            {
                GameObject.Find("Player"),
                GameObject.Find("Hero"),
                GameObject.Find("Personagem"),
                GameObject.Find("Character"),
            }
            .Where(g => g != null)
            .Distinct()
            .ToArray();
        }

        foreach (var target in targets)
        {
            ApplyToonToRenderers(target, skinMat, armorMat, outlineMat);
        }

        Debug.Log("SSA Fix aplicado. Verifique se a cena agora exibe o toon (faixas, rim, outline). Se algo permanecer liso, confirme URP ativo e ramps corretas.");
        Selection.objects = new Object[] { skinMat, armorMat, outlineMat, profile };
    }

    private static bool NameLooksLikeCharacter(string name)
    {
        name = name.ToLower();
        return name.Contains("hero") || name.Contains("player") || name.Contains("char") || name.Contains("knight") || name.Contains("enemy") || name.Contains("virgo") || name.Contains("saga");
    }

    private static void ApplyToonToRenderers(GameObject root, Material skinMat, Material armorMat, Material outlineMat)
    {
        if (!root)
        {
            return;
        }

        var renderers = root.GetComponentsInChildren<Renderer>(true);
        foreach (var renderer in renderers)
        {
            if (renderer is LineRenderer || renderer is TrailRenderer)
            {
                continue;
            }

            if (renderer.gameObject.layer == LayerMask.NameToLayer("UI"))
            {
                continue;
            }

            bool useArmor = renderer.name.ToLower().Contains("armor") || renderer.name.ToLower().Contains("armadura") || renderer.name.ToLower().Contains("metal") || renderer.name.ToLower().Contains("ouro") || renderer.name.ToLower().Contains("gold");
            var baseMaterial = useArmor ? armorMat : skinMat;

            var materials = renderer.sharedMaterials;
            if (materials == null || materials.Length == 0)
            {
                materials = new Material[1];
            }
            materials[0] = baseMaterial;

            var list = materials.ToList();
            if (outlineMat != null)
            {
                if (list.Count == 0 || list[list.Count - 1] == null || list[list.Count - 1].shader == null || list[list.Count - 1].shader.name != "Toon/Outline")
                {
                    list.Add(outlineMat);
                }
            }
            renderer.sharedMaterials = list.ToArray();
        }
    }

    private static Material GetOrCreateMaterial(string path, string shaderName)
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material)
        {
            return material;
        }

        var shader = Shader.Find(shaderName);
        if (shader == null)
        {
            Debug.LogError($"Shader não encontrado: {shaderName}");
            return null;
        }

        material = new Material(shader);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static void FixRampImport(string folder)
    {
        if (!AssetDatabase.IsValidFolder(folder))
        {
            return;
        }

        var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!(AssetImporter.GetAtPath(path) is TextureImporter importer))
            {
                continue;
            }

            bool changed = false;
            if (importer.sRGBTexture)
            {
                importer.sRGBTexture = false;
                changed = true;
            }
            if (importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                changed = true;
            }
            if (importer.filterMode != FilterMode.Point)
            {
                importer.filterMode = FilterMode.Point;
                changed = true;
            }
            if (importer.wrapMode != TextureWrapMode.Clamp)
            {
                importer.wrapMode = TextureWrapMode.Clamp;
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
                Debug.Log($"Ramp ajustada: {path}");
            }
        }
    }

    private static bool AddOrGet<T>(VolumeProfile profile, out T component) where T : VolumeComponent
    {
        if (!profile.TryGet(out component))
        {
            component = profile.Add<T>(true);
            return true;
        }
        return false;
    }
}
#endif
