using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace JogoSSA.Editor
{
    /// <summary>
    /// Creates a stylised sample hero built from primitives and configured with the SSA toon materials.
    /// Gives the team a tangible reference for lighting and shading before final models arrive.
    /// </summary>
    public static class SSASampleHeroCreator
    {
        private const string MenuPath = "Jogo/Create SSA Sample Hero";
        private const string SkinMatPath = "Assets/JogoSSA/Materials/M_Char_Skin_Toon.mat";
        private const string ArmorMatPath = "Assets/JogoSSA/Materials/M_Armor_Gold_Toon.mat";
        private const string OutlineMatPath = "Assets/JogoSSA/Materials/M_Outline_Black.mat";

        [MenuItem(MenuPath, priority = 60)]
        private static void CreateSampleHero()
        {
            var skinMat = AssetDatabase.LoadAssetAtPath<Material>(SkinMatPath);
            var armorMat = AssetDatabase.LoadAssetAtPath<Material>(ArmorMatPath);
            var outlineMat = AssetDatabase.LoadAssetAtPath<Material>(OutlineMatPath);

            if (skinMat == null || armorMat == null || outlineMat == null)
            {
                EditorUtility.DisplayDialog(
                    "Materiais SSA não encontrados",
                    "Execute primeiro o menu 'Jogo -> Setup SSA (Toon)' para gerar os materiais base.",
                    "OK");
                return;
            }

            var root = new GameObject("SSA_HeroSample");
            Undo.RegisterCreatedObjectUndo(root, "Create SSA Sample Hero");
            root.transform.position = Vector3.zero;

            // Skin tones
            var skinColor = new Color(1.0f, 0.86f, 0.78f);
            var faceShadow = new Color(0.95f, 0.54f, 0.45f);

            // Armor palette
            var armorMain = new Color(0.16f, 0.32f, 0.85f);
            var armorSecondary = new Color(0.12f, 0.22f, 0.58f);
            var accentGold = new Color(0.95f, 0.78f, 0.22f);
            var hairColor = new Color(0.22f, 0.07f, 0.36f);

            // Body core
            CreatePart("Body", PrimitiveType.Capsule, new Vector3(0f, 1.1f, 0f), new Vector3(0.55f, 1.4f, 0.4f), skinMat, outlineMat, skinColor, root.transform);
            CreatePart("TorsoArmor", PrimitiveType.Cylinder, new Vector3(0f, 1.35f, 0f), new Vector3(0.65f, 0.5f, 0.45f), armorMat, outlineMat, armorMain, root.transform, rotation: Quaternion.Euler(90f, 0f, 0f));
            CreatePart("ChestPlate", PrimitiveType.Cube, new Vector3(0f, 1.55f, 0.08f), new Vector3(0.7f, 0.4f, 0.25f), armorMat, outlineMat, armorSecondary, root.transform);
            CreatePart("BeltGem", PrimitiveType.Cylinder, new Vector3(0f, 0.9f, 0.18f), new Vector3(0.25f, 0.1f, 0.25f), armorMat, outlineMat, accentGold, root.transform, rotation: Quaternion.Euler(90f, 0f, 0f));

            // Head and hair
            CreatePart("Head", PrimitiveType.Sphere, new Vector3(0f, 2.0f, 0.05f), new Vector3(0.5f, 0.5f, 0.5f), skinMat, outlineMat, skinColor, root.transform);
            var faceMask = CreatePart("FaceMask", PrimitiveType.Sphere, new Vector3(0f, 2.0f, 0.25f), new Vector3(0.3f, 0.3f, 0.2f), armorMat, outlineMat, armorMain, root.transform);
            faceMask.transform.localRotation = Quaternion.Euler(12f, 0f, 0f);
            CreatePart("HairTop", PrimitiveType.Capsule, new Vector3(0f, 2.25f, -0.05f), new Vector3(0.75f, 0.35f, 0.75f), armorMat, outlineMat, hairColor, root.transform, rotation: Quaternion.Euler(90f, 0f, 0f));
            CreatePart("HairBack", PrimitiveType.Capsule, new Vector3(0f, 1.85f, -0.35f), new Vector3(0.55f, 0.8f, 0.55f), armorMat, outlineMat, hairColor, root.transform, rotation: Quaternion.Euler(0f, 0f, 90f));
            CreatePart("HairSideL", PrimitiveType.Capsule, new Vector3(-0.35f, 2.0f, -0.05f), new Vector3(0.18f, 0.55f, 0.18f), armorMat, outlineMat, hairColor, root.transform, rotation: Quaternion.Euler(0f, 0f, 75f));
            CreatePart("HairSideR", PrimitiveType.Capsule, new Vector3(0.35f, 2.0f, -0.05f), new Vector3(0.18f, 0.55f, 0.18f), armorMat, outlineMat, hairColor, root.transform, rotation: Quaternion.Euler(0f, 0f, -75f));

            // Arms
            CreateLimb(root.transform, "ArmL", new Vector3(-0.55f, 1.4f, 0f), new Vector3(-0.75f, 0.85f, 0f), skinMat, armorMat, outlineMat, armorMain, armorSecondary, skinColor, accentGold);
            CreateLimb(root.transform, "ArmR", new Vector3(0.55f, 1.4f, 0f), new Vector3(0.75f, 0.9f, 0.1f), skinMat, armorMat, outlineMat, armorMain, armorSecondary, skinColor, accentGold, true);

            // Legs
            CreateLeg(root.transform, "LegL", new Vector3(-0.22f, 0.4f, 0f), skinMat, armorMat, outlineMat, armorMain, accentGold, skinColor);
            CreateLeg(root.transform, "LegR", new Vector3(0.22f, 0.4f, 0f), skinMat, armorMat, outlineMat, armorMain, accentGold, skinColor);

            // Cape
            var cape = CreatePart("Cape", PrimitiveType.Quad, new Vector3(0f, 1.35f, -0.35f), new Vector3(1.6f, 2.2f, 1f), armorMat, outlineMat, new Color(0.08f, 0.12f, 0.35f), root.transform);
            cape.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);

            // Energy orb in hand
            var orb = CreatePart("CosmoOrb", PrimitiveType.Sphere, new Vector3(0.9f, 0.95f, 0.2f), new Vector3(0.25f, 0.25f, 0.25f), armorMat, outlineMat, new Color(0.22f, 0.65f, 1.0f), root.transform);
            var orbMat = orb.GetComponent<Renderer>().sharedMaterials[0];
            if (orbMat.HasProperty("_EmissionColor"))
            {
                orbMat.SetColor("_EmissionColor", new Color(0.2f, 0.7f, 1.0f) * 1.5f);
            }

            Selection.activeGameObject = root;
        }

        private static void CreateLimb(Transform parent, string prefix, Vector3 shoulderPos, Vector3 handPos, Material skinMat, Material armorMat, Material outlineMat, Color armorMain, Color armorSecondary, Color skinColor, Color accentGold, bool mirror = false)
        {
            float sideSign = mirror ? 1f : -1f;
            float shoulderOffset = mirror ? 1f : -1f;

            var shoulder = CreatePart(prefix + "_Shoulder", PrimitiveType.Sphere, shoulderPos, new Vector3(0.32f, 0.32f, 0.32f), armorMat, outlineMat, armorMain, parent);
            CreatePart(prefix + "_UpperArmor", PrimitiveType.Capsule, shoulderPos + new Vector3(0f, -0.35f, 0.05f), new Vector3(0.22f, 0.55f, 0.22f), armorMat, outlineMat, armorSecondary, parent, rotation: Quaternion.Euler(0f, 0f, 15f * sideSign));
            CreatePart(prefix + "_ForearmGuard", PrimitiveType.Capsule, shoulderPos + new Vector3(0f, -0.85f, 0.06f), new Vector3(0.20f, 0.5f, 0.20f), armorMat, outlineMat, armorMain, parent, rotation: Quaternion.Euler(0f, 0f, -10f * sideSign));
            CreatePart(prefix + "_Glove", PrimitiveType.Sphere, handPos, new Vector3(0.23f, 0.23f, 0.23f), armorMat, outlineMat, accentGold, parent);
            CreatePart(prefix + "_Palm", PrimitiveType.Cube, handPos + new Vector3(0.0f, -0.1f, 0.0f), new Vector3(0.18f, 0.1f, 0.25f), armorMat, outlineMat, armorSecondary, parent);
            CreatePart(prefix + "_Fingers", PrimitiveType.Cube, handPos + new Vector3(0.0f, -0.1f, 0.18f), new Vector3(0.16f, 0.08f, 0.28f), skinMat, outlineMat, skinColor, parent);
        }

        private static void CreateLeg(Transform parent, string prefix, Vector3 hipPos, Material skinMat, Material armorMat, Material outlineMat, Color armorMain, Color accentGold, Color skinColor)
        {
            CreatePart(prefix + "_Thigh", PrimitiveType.Capsule, hipPos, new Vector3(0.28f, 0.75f, 0.28f), armorMat, outlineMat, armorMain, parent, rotation: Quaternion.Euler(0f, 0f, 4f));
            CreatePart(prefix + "_KneeGuard", PrimitiveType.Sphere, hipPos + new Vector3(0f, -0.65f, 0.05f), new Vector3(0.23f, 0.23f, 0.23f), armorMat, outlineMat, accentGold, parent);
            CreatePart(prefix + "_Calf", PrimitiveType.Capsule, hipPos + new Vector3(0f, -1.1f, 0.05f), new Vector3(0.24f, 0.65f, 0.24f), armorMat, outlineMat, armorMain, parent, rotation: Quaternion.Euler(3f, 0f, 0f));
            CreatePart(prefix + "_Boot", PrimitiveType.Cube, hipPos + new Vector3(0f, -1.55f, 0.18f), new Vector3(0.3f, 0.18f, 0.5f), armorMat, outlineMat, accentGold, parent);
            CreatePart(prefix + "_Toe", PrimitiveType.Cube, hipPos + new Vector3(0f, -1.58f, 0.45f), new Vector3(0.26f, 0.14f, 0.35f), armorMat, outlineMat, armorMain, parent);
        }

        private static Renderer CreatePart(
            string partName,
            PrimitiveType primitiveType,
            Vector3 localPosition,
            Vector3 localScale,
            Material baseMaterial,
            Material outlineMaterial,
            Color baseColor,
            Transform parent,
            Quaternion? rotation = null)
        {
            var go = GameObject.CreatePrimitive(primitiveType);
            Undo.RegisterCreatedObjectUndo(go, "Create SSA Sample Hero Part");
            go.name = partName;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;
            go.transform.localRotation = rotation ?? Quaternion.identity;

            var collider = go.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }

            var renderer = go.GetComponent<Renderer>();
            var mats = new List<Material>();
            var toonMat = new Material(baseMaterial)
            {
                name = partName + "_Toon"
            };
            if (toonMat.HasProperty("_BaseColor"))
            {
                toonMat.SetColor("_BaseColor", baseColor);
            }
            mats.Add(toonMat);
            if (outlineMaterial != null)
            {
                mats.Add(outlineMaterial);
            }
            renderer.sharedMaterials = mats.ToArray();
            return renderer;
        }
    }
}
