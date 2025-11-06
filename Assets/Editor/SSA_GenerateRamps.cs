using UnityEditor;
using UnityEngine;
using System.IO;

public static class SSA_GenerateRamps
{
    [MenuItem("SSA/Setup/1) Gerar Ramp Textures")]
    public static void Generate()
    {
        string dir = "Assets/Resources/ToonRamps";
        if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder("Assets/Resources", "ToonRamps");

        CreateRamp(Path.Combine(dir, "skin_ramp.png"), new float[] { 0.0f, 0.55f, 0.82f, 1.0f });
        CreateRamp(Path.Combine(dir, "metal_ramp.png"), new float[] { 0.0f, 0.4f, 0.9f, 1.0f });
        CreateRamp(Path.Combine(dir, "cloth_light_ramp.png"), new float[] { 0.0f, 0.5f, 0.8f, 1.0f });
        CreateRamp(Path.Combine(dir, "cloth_dark_ramp.png"), new float[] { 0.0f, 0.35f, 0.7f, 1.0f });

        AssetDatabase.Refresh();
        Debug.Log("[SSA] Toon ramps geradas em " + dir);
    }

    static void CreateRamp(string path, float[] bands)
    {
        int w = 256, h = 1;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false, true);
        for (int x = 0; x < w; x++)
        {
            float u = x / (w - 1f);
            float v = StepBands(u, bands);
            tex.SetPixel(x, 0, new Color(v, v, v, 1));
        }
        tex.Apply();

        var bytes = tex.EncodeToPNG();
        File.WriteAllBytes(path, bytes);
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path);
        var ti = (TextureImporter)AssetImporter.GetAtPath(path);
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.mipmapEnabled = false;
        ti.wrapMode = TextureWrapMode.Clamp;
        ti.SaveAndReimport();
    }

    static float StepBands(float u, float[] b)
    {
        for (int i = 1; i < b.Length; i++)
            if (u < b[i]) return (i - 1) / (float)(b.Length - 1);
        return 1f;
    }
}
