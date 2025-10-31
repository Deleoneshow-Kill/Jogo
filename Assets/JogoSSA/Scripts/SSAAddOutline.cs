using UnityEngine;

namespace JogoSSA
{
    [RequireComponent(typeof(Renderer))]
    public class SSAAddOutline : MonoBehaviour
    {
        [SerializeField] private Material outlineMaterial;
        [SerializeField] private float thicknessOverride = -1f;

        private Renderer cachedRenderer;

        private void Reset()
        {
            outlineMaterial = LoadDefaultOutline();
            Apply();
        }

        private void OnValidate()
        {
            Apply();
        }

        private void Awake()
        {
            Apply();
        }

        private void Apply()
        {
            if (cachedRenderer == null)
            {
                cachedRenderer = GetComponent<Renderer>();
            }

            if (cachedRenderer == null)
            {
                return;
            }

            var outline = outlineMaterial != null ? outlineMaterial : LoadDefaultOutline();
            if (outline == null)
            {
                return;
            }

            if (thicknessOverride >= 0f)
            {
                outline.SetFloat("_Thickness", thicknessOverride);
            }

            var materials = cachedRenderer.sharedMaterials;
            bool hasOutline = false;
            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] == outline)
                {
                    hasOutline = true;
                    break;
                }
            }

            if (!hasOutline)
            {
                var newMats = new Material[materials.Length + 1];
                materials.CopyTo(newMats, 0);
                newMats[newMats.Length - 1] = outline;
                cachedRenderer.sharedMaterials = newMats;
            }
        }

        private static Material LoadDefaultOutline()
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/JogoSSA/Materials/M_Outline_Black.mat");
#else
            return null;
#endif
        }
    }
}
