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
        private static void ApplyToon()
        {
            var skinMat = AssetDatabase.LoadAssetAtPath<Material>(SkinMatPath);
            var armorMat = AssetDatabase.LoadAssetAtPath<Material>(ArmorMatPath);
            var outlineMat = AssetDatabase.LoadAssetAtPath<Material>(OutlineMatPath);

            if (skinMat == null || armorMat == null || outlineMat == null)
            {
                EditorUtility.DisplayDialog(
                    "Materiais SSA ausentes",
                    "Execute 'Jogo -> Setup SSA (Toon)' para gerar os materiais padrão antes de aplicar o toon.",
                    "OK");
                return;
            }

            var targets = Selection.gameObjects;
            if (targets == null || targets.Length == 0)
            {
                EditorUtility.DisplayDialog("Nada selecionado", "Selecione um personagem ou prefab antes de aplicar o shader.", "OK");
                return;
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
                    rendererCount++;
                    var materials = new List<Material>();

                    // heurística simples: se o nome do renderer contém "hair" ou "mesh" universal, usa armor
                    bool isSkin = renderer.name.ToLower().Contains("skin") || renderer.name.ToLower().Contains("face");
                    bool isHair = renderer.name.ToLower().Contains("hair");

                    Material toonMaterial = isSkin ? new Material(skinMat) : new Material(armorMat);
                    toonMaterial.name = renderer.name + "_Toon";

                    // variação rápida de cor baseada em bounds para dar diferenciação
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
                EditorUtility.DisplayDialog("Nenhum Renderer encontrado", "Certifique-se de selecionar um objeto com MeshRenderer ou SkinnedMeshRenderer.", "OK");
            }
            else
            {
                Debug.Log($"SSA Toon aplicado em {rendererCount} renderers. Ajuste manualmente as cores conforme necessário.");
            }
        }
    }
}
