using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public class SSAOutlineAutoScale : MonoBehaviour
{
    [Range(0.0005f, 0.02f)] public float BaseThickness = 0.004f;
    public float ReferenceSize = 1.8f;

    private void Update()
    {
        var renderer = GetComponentInChildren<Renderer>();
        if (renderer == null)
        {
            return;
        }

        var materials = renderer.sharedMaterials;
        if (materials == null || materials.Length == 0)
        {
            return;
        }

        float scale = renderer.bounds.size.magnitude / Mathf.Max(ReferenceSize, 0.0001f);
        float thickness = BaseThickness * Mathf.Clamp(scale, 0.5f, 2.0f);

        for (int i = 0; i < materials.Length; i++)
        {
            var mat = materials[i];
            if (mat != null && mat.shader != null && mat.shader.name == "Toon/Outline" && mat.HasProperty("_Thickness"))
            {
                mat.SetFloat("_Thickness", thickness);
            }
        }
    }
}
