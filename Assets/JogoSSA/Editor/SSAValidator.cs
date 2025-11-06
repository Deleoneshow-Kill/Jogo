using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_RENDER_PIPELINE_URP
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#endif

namespace JogoSSA.Editor
{
    public static class SSAValidator
    {
        private const string MenuPath = "Jogo/Validate SSA Setup";
        private const string SkinMaterialPath = "Assets/JogoSSA/Materials/M_Char_Skin_Toon.mat";
        private const string ArmorMaterialPath = "Assets/JogoSSA/Materials/M_Armor_Gold_Toon.mat";
        private const string OutlineMaterialPath = "Assets/JogoSSA/Materials/M_Outline_Black.mat";
        private const string GlobalVolumeName = "SSA_GlobalVolume";

        [MenuItem(MenuPath, priority = 20)]
        public static void Validate()
        {
            var report = new StringBuilder();
            bool hasIssues = false;

#if UNITY_RENDER_PIPELINE_URP
            // Render Pipeline asset
            if (UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset urpAsset)
            {
                report.AppendLine("✔ URP asset atribuído: " + urpAsset.name);
            }
            else
            {
                report.AppendLine("✖ URP não está ativo em GraphicsSettings. Atribua o URP Asset.");
                hasIssues = true;
            }
#else
            report.AppendLine("✖ Pacote Universal RP ainda não está instalado. Instale pelo Package Manager.");
            hasIssues = true;
#endif

            // Color space
            if (PlayerSettings.colorSpace == ColorSpace.Linear)
            {
                report.AppendLine("✔ Color Space = Linear");
            }
            else
            {
                report.AppendLine("✖ Color Space está em Gamma. Ajuste em Project Settings > Player > Other Settings.");
                hasIssues = true;
            }

            // Materials exist
            var skinMat = AssetDatabase.LoadAssetAtPath<Material>(SkinMaterialPath);
            var armorMat = AssetDatabase.LoadAssetAtPath<Material>(ArmorMaterialPath);
            var outlineMat = AssetDatabase.LoadAssetAtPath<Material>(OutlineMaterialPath);
            if (skinMat != null && armorMat != null && outlineMat != null)
            {
                report.AppendLine("✔ Materiais SSA encontrados");
            }
            else
            {
                report.AppendLine("✖ Nem todos os materiais SSA estão presentes. Rode 'Jogo -> Setup SSA (Toon)'.");
                hasIssues = true;
            }

            // Scene checks
            var activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid())
            {
                report.AppendLine("✖ Cena ativa inválida. Abra a cena desejada e rode novamente.");
                Debug.LogWarning(report.ToString());
                return;
            }

            var missingToon = new List<string>();
            var missingOutline = new List<string>();
            var renderers = GetAllRenderers(activeScene);
            foreach (var renderer in renderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                bool hasToonMaterial = false;
                bool hasOutlineMaterial = false;
                foreach (var mat in renderer.sharedMaterials)
                {
                    if (mat == null)
                    {
                        continue;
                    }

                    if (mat.shader != null && mat.shader.name == "Toon/CharacterURP")
                    {
                        hasToonMaterial = true;
                    }
                    if (mat.shader != null && mat.shader.name == "Toon/Outline")
                    {
                        hasOutlineMaterial = true;
                    }
                }

                if (!hasToonMaterial)
                {
                    missingToon.Add(renderer.gameObject.name);
                }
                if (!hasOutlineMaterial)
                {
                    missingOutline.Add(renderer.gameObject.name);
                }
            }

            if (missingToon.Count == 0)
            {
                report.AppendLine("✔ Todos os personagens encontrados estão usando Toon/CharacterURP.");
            }
            else
            {
                hasIssues = true;
                report.AppendLine("✖ Objetos sem material toon:");
                foreach (var entry in missingToon)
                {
                    report.AppendLine("  - " + entry);
                }
            }

            if (missingOutline.Count == 0)
            {
                report.AppendLine("✔ Todos os personagens possuem material de contorno.");
            }
            else
            {
                hasIssues = true;
                report.AppendLine("✖ Objetos sem outline aplicado:");
                foreach (var entry in missingOutline)
                {
                    report.AppendLine("  - " + entry);
                }
            }

#if UNITY_RENDER_PIPELINE_URP
            // Global volume check
            var globalVolume = GameObject.Find(GlobalVolumeName);
            if (globalVolume != null && globalVolume.TryGetComponent(out Volume volume) && volume.sharedProfile != null)
            {
                var profile = volume.sharedProfile;
                bool hasTonemapping = profile.TryGet(out Tonemapping _);
                bool hasBloom = profile.TryGet(out Bloom _);
                if (hasTonemapping && hasBloom)
                {
                    report.AppendLine("✔ Global Volume possui Tonemapping + Bloom.");
                }
                else
                {
                    hasIssues = true;
                    report.AppendLine("✖ Global Volume encontrado, mas faltam efeitos (Tonemapping ou Bloom).");
                }
            }
            else
            {
                hasIssues = true;
                report.AppendLine("✖ Global Volume SSA_GlobalVolume não está presente/ativo.");
            }
#endif

            if (hasIssues)
            {
                Debug.LogWarning(report.ToString());
            }
            else
            {
                Debug.Log(report.ToString());
            }
        }

        private static IEnumerable<SkinnedMeshRenderer> GetAllRenderers(Scene scene)
        {
            var list = new List<SkinnedMeshRenderer>();
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root == null)
                {
                    continue;
                }
                root.GetComponentsInChildren(true, list);
            }
            return list;
        }
    }
}
