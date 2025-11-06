using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace JogoSSA.Editor
{
    /// <summary>
    /// Utility to assign SSA toon materials to renderers inside the current selection.
    /// Ideal for FBXs or prefabs already in the project.
    /// </summary>
    public static class SSAApplyToonToSelection
    {
        private const string MenuPath = "Jogo/Apply SSA Toon To Selection";
        private const string SkinMatPath = "Assets/JogoSSA/Materials/M_Char_Skin_Toon.mat";
        private const string ArmorMatPath = "Assets/JogoSSA/Materials/M_Armor_Gold_Toon.mat";
        private const string OutlineMatPath = "Assets/JogoSSA/Materials/M_Outline_Black.mat";

        [MenuItem(MenuPath, priority = 80)]
        private static void ApplyToonMenu()
        {
            var success = ApplyToon(Selection.gameObjects, interactive: true);
            if (!success)
            {
                return;
            }
        }

        public static bool ApplyToon(GameObject[] targets, bool interactive = false)
        {
            var skinMat = AssetDatabase.LoadAssetAtPath<Material>(SkinMatPath);
            var armorMat = AssetDatabase.LoadAssetAtPath<Material>(ArmorMatPath);
            var outlineMat = AssetDatabase.LoadAssetAtPath<Material>(OutlineMatPath);

            if (skinMat == null || armorMat == null || outlineMat == null)
            {
                if (interactive)
                {
                    EditorUtility.DisplayDialog(
                        "Materiais SSA ausentes",
                        "Execute 'Jogo -> Setup SSA (Toon)' para gerar os materiais padrão antes de aplicar o toon.",
                        "OK");
                }
                else
                {
                    Debug.LogWarning("[SSA] Materiais padrão não encontrados. Rode 'Jogo -> Setup SSA (Toon)'.");
                }
                return false;
            }

            if (targets == null || targets.Length == 0)
            {
                if (interactive)
                {
                    EditorUtility.DisplayDialog("Nada selecionado", "Selecione um personagem ou prefab antes de aplicar o shader.", "OK");
                }
                return false;
            }

            int rendererCount = 0;
            foreach (var go in targets)
            {
                if (go == null)
                {
                    continue;
                }

                var renderers = go.GetComponentsInChildren<Renderer>(true);
                foreach (var renderer in renderers)
                {
                    if (!renderer)
                    {
                        continue;
                    }

                    rendererCount++;
                    var materials = new List<Material>();

                    bool isSkin = renderer.name.ToLower().Contains("skin") || renderer.name.ToLower().Contains("face");
                    bool isHair = renderer.name.ToLower().Contains("hair");

                    Material toonMaterial = isSkin ? new Material(skinMat) : new Material(armorMat);
                    toonMaterial.name = renderer.name + "_Toon";

                    var center = renderer.bounds.center;
                    float hueOffset = Mathf.Abs(Mathf.Sin(center.x + center.z));
                    var color = Color.Lerp(new Color(0.15f, 0.25f, 0.8f), new Color(0.7f, 0.2f, 0.3f), hueOffset);
                    if (toonMaterial.HasProperty("_BaseColor") && !isSkin)
                    {
                        toonMaterial.SetColor("_BaseColor", color);
                    }

                    if (isHair && toonMaterial.HasProperty("_BaseColor"))
                    {
                        toonMaterial.SetColor("_BaseColor", new Color(0.2f, 0.05f, 0.35f));
                    }

                    materials.Add(toonMaterial);
                    materials.Add(outlineMat);

                    renderer.sharedMaterials = materials.ToArray();
                }
            }

            if (rendererCount == 0)
            {
                if (interactive)
                {
                    EditorUtility.DisplayDialog("Nenhum Renderer encontrado", "Certifique-se de selecionar um objeto com MeshRenderer ou SkinnedMeshRenderer.", "OK");
                }
                return false;
            }

            Debug.Log($"SSA Toon aplicado em {rendererCount} renderers. Ajuste manualmente as cores conforme necessário.");
            return true;
        }
    }
}
