using System.IO;
using ToonSetup;
using UnityEditor;
using UnityEngine;

#if UNITY_RENDER_PIPELINE_URP
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#endif

namespace JogoSSA.Editor
{
    public static class SetupSSA
    {
        private const string MenuPath = "Jogo/Setup SSA (Toon)";

#if UNITY_RENDER_PIPELINE_URP
        private const string PipelineFolder = "Assets/Rendering/URP";
        private const string PipelineAssetPath = PipelineFolder + "/URP-High.asset";
        private const string RendererAssetName = "URP-Renderer";
        private const string MaterialsFolder = "Assets/JogoSSA/Materials";
        private const string SkinMatPath = MaterialsFolder + "/M_Char_Skin_Toon.mat";
        private const string ArmorMatPath = MaterialsFolder + "/M_Armor_Gold_Toon.mat";
        private const string OutlineMatPath = MaterialsFolder + "/M_Outline_Black.mat";
        private const string GlobalVolumeName = "SSA_GlobalVolume";
        private const string DirectionalLightName = "SSA Directional Light";
        private const string VolumeProfilePath = "Assets/JogoSSA/Post/SSA_GlobalVolumeProfile.asset";
#endif

        [MenuItem(MenuPath, priority = 1)]
        public static void Execute()
        {
#if !UNITY_RENDER_PIPELINE_URP
            EditorUtility.DisplayDialog(
                "Universal RP não encontrado",
                "Instale o pacote Universal Render Pipeline via Package Manager antes de rodar o setup.",
                "OK");
            return;
#else
            EnsurePipelineAsset();
            EnsureColorSpace();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EnsureDefaultTextures();
            EnsureMaterials();
            EnsureSceneObjects();

            Debug.Log("SSA toon setup complete. Assign the generated materials to your characters.");
#endif
        }

#if UNITY_RENDER_PIPELINE_URP
        private static void EnsurePipelineAsset()
        {
            Directory.CreateDirectory(PipelineFolder);
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelineAssetPath);
            if (pipeline == null)
            {
                pipeline = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
                AssetDatabase.CreateAsset(pipeline, PipelineAssetPath);

                var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                renderer.name = RendererAssetName;
                AssetDatabase.AddObjectToAsset(renderer, pipeline);

                var so = new SerializedObject(pipeline);
                var rendererList = so.FindProperty("m_RendererDataList");
                rendererList.arraySize = 1;
                rendererList.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
                so.FindProperty("m_DefaultRendererIndex").intValue = 0;
                so.ApplyModifiedProperties();

            }

            GraphicsSettings.defaultRenderPipeline = pipeline;
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, applyExpensiveChanges: false);
                QualitySettings.renderPipeline = pipeline;
            }
            QualitySettings.SetQualityLevel(QualitySettings.GetQualityLevel(), true);
        }

        private static void EnsureColorSpace()
        {
            if (PlayerSettings.colorSpace != ColorSpace.Linear)
            {
                PlayerSettings.colorSpace = ColorSpace.Linear;
            }
        }

        private static void EnsureDefaultTextures()
        {
            CreateToonAssets.CreateDefaults();
        }

        private static void EnsureMaterials()
        {
            Directory.CreateDirectory(MaterialsFolder);

            var skinMat = AssetDatabase.LoadAssetAtPath<Material>(SkinMatPath);
            if (skinMat == null)
            {
                skinMat = new Material(Shader.Find("Toon/CharacterURP"));
                skinMat.name = Path.GetFileNameWithoutExtension(SkinMatPath);
                skinMat.SetTexture("_RampTex", LoadRamp("ramp_skin.png"));
                skinMat.SetTexture("_MatcapTex", LoadMatcap("matcap_gold.png"));
                skinMat.SetFloat("_RampBias", 0.05f);
                skinMat.SetFloat("_SpecIntensity", 0.6f);
                skinMat.SetFloat("_RimIntensity", 0.3f);
                skinMat.SetFloat("_RimPower", 2.2f);
                AssetDatabase.CreateAsset(skinMat, SkinMatPath);
            }

            var armorMat = AssetDatabase.LoadAssetAtPath<Material>(ArmorMatPath);
            if (armorMat == null)
            {
                armorMat = new Material(Shader.Find("Toon/CharacterURP"));
                armorMat.name = Path.GetFileNameWithoutExtension(ArmorMatPath);
                armorMat.SetTexture("_RampTex", LoadRamp("ramp_armor.png"));
                armorMat.SetTexture("_MatcapTex", LoadMatcap("matcap_gold.png"));
                armorMat.SetFloat("_RampBias", 0.02f);
                armorMat.SetFloat("_SpecIntensity", 0.9f);
                armorMat.SetFloat("_RimIntensity", 0.35f);
                armorMat.SetFloat("_RimPower", 2.0f);
                AssetDatabase.CreateAsset(armorMat, ArmorMatPath);
            }

            var outlineMat = AssetDatabase.LoadAssetAtPath<Material>(OutlineMatPath);
            if (outlineMat == null)
            {
                outlineMat = new Material(Shader.Find("Toon/Outline"));
                outlineMat.name = Path.GetFileNameWithoutExtension(OutlineMatPath);
                outlineMat.SetColor("_Color", new Color(0.02f, 0.02f, 0.08f));
                outlineMat.SetFloat("_Thickness", 0.0045f);
                AssetDatabase.CreateAsset(outlineMat, OutlineMatPath);
            }
        }

        private static Texture2D LoadRamp(string fileName)
        {
            return AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/Shaders/Toon/Ramps/{fileName}");
        }

        private static Texture2D LoadMatcap(string fileName)
        {
            return AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/Shaders/Toon/MatCaps/{fileName}");
        }

        private static void EnsureSceneObjects()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            if (!activeScene.IsValid())
            {
                return;
            }

            EnsureGlobalVolume();
            EnsureDirectionalLight();
        }

        private static void EnsureGlobalVolume()
        {
            var volumeGO = GameObject.Find(GlobalVolumeName);
            if (volumeGO == null)
            {
                volumeGO = new GameObject(GlobalVolumeName);
            }

            var volume = volumeGO.GetComponent<Volume>();
            if (volume == null)
            {
                volume = volumeGO.AddComponent<Volume>();
            }

            volume.isGlobal = true;
            volume.priority = 0f;
            if (volume.sharedProfile == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(VolumeProfilePath));
                var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
                if (profile == null)
                {
                    profile = ScriptableObject.CreateInstance<VolumeProfile>();
                    AssetDatabase.CreateAsset(profile, VolumeProfilePath);
                }
                volume.sharedProfile = profile;
            }

            ConfigureVolumeProfile(volume.sharedProfile);
        }

        private static void ConfigureVolumeProfile(VolumeProfile profile)
        {
            EnsureOverride<Tonemapping>(profile, overrideData =>
            {
                overrideData.active = true;
                overrideData.mode.Override(TonemappingMode.ACES);
            });

            EnsureOverride<Bloom>(profile, overrideData =>
            {
                overrideData.active = true;
                overrideData.threshold.Override(1.2f);
                overrideData.intensity.Override(0.5f);
            });

            EnsureOverride<ColorAdjustments>(profile, overrideData =>
            {
                overrideData.active = true;
                overrideData.saturation.Override(10f);
            });
        }

        private static void EnsureDirectionalLight()
        {
            var lightGO = GameObject.Find(DirectionalLightName);
            if (lightGO == null)
            {
                lightGO = new GameObject(DirectionalLightName);
                lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }

            var light = lightGO.GetComponent<Light>();
            if (light == null)
            {
                light = lightGO.AddComponent<Light>();
            }

            light.type = LightType.Directional;
            light.color = new Color(1.0f, 0.96f, 0.9f);
            light.intensity = 1.2f;
            light.shadows = LightShadows.Soft;
            light.shadowBias = 0.05f;
            light.shadowNormalBias = 0.4f;
        }

        private static void EnsureOverride<T>(VolumeProfile profile, System.Action<T> configure) where T : VolumeComponent
        {
            if (!profile.TryGet(out T component))
            {
                component = profile.Add<T>(true);
            }
            configure(component);
        }
#endif
    }
}
