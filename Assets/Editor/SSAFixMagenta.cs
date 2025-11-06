#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class SSAFixMagenta
{
    private const string MenuPath = "SSA/Consertar Magenta (Converter para URP)";

    [MenuItem(MenuPath)]
    public static void Run()
    {
        int converted = 0;
        int errors = 0;

        var renderers = Object.FindObjectsOfType<Renderer>(true);
        foreach (var renderer in renderers)
        {
            var materials = renderer.sharedMaterials;
            var changed = false;

            for (int i = 0; i < materials.Length; i++)
            {
                var material = materials[i];
                if (!material)
                {
                    materials[i] = CreateMaterial("Universal Render Pipeline/Lit");
                    converted++;
                    changed = true;
                    continue;
                }

                var shaderName = material.shader ? material.shader.name : "Hidden/InternalErrorShader";
                if (!NeedsConversion(shaderName))
                {
                    continue;
                }

                try
                {
                    var targetShader = ResolveTargetShader(shaderName);
                    var convertedMat = CreateMaterial(targetShader);
                    convertedMat.CopyPropertiesFromMaterial(material);
                    convertedMat.name = material.name + "_URP";
                    materials[i] = convertedMat;
                    converted++;
                    changed = true;
                }
                catch
                {
                    errors++;
                }
            }

            if (changed)
            {
                renderer.sharedMaterials = materials;
            }
        }

        Debug.Log($"[SSA Fix] Materiais convertidos: {converted} | erros: {errors}");
    }

    private static bool NeedsConversion(string shaderName)
    {
        if (string.IsNullOrEmpty(shaderName))
        {
            return true;
        }

        return shaderName == "Hidden/InternalErrorShader"
               || shaderName == "Standard"
               || shaderName.StartsWith("Legacy Shaders/")
               || (shaderName.Contains("Particles") && !shaderName.Contains("Universal Render Pipeline"))
               || (shaderName.StartsWith("Unlit/") && !shaderName.StartsWith("Universal Render Pipeline"));
    }

    private static string ResolveTargetShader(string sourceShader)
    {
        if (sourceShader.Contains("Particles"))
        {
            return "Universal Render Pipeline/Particles/Unlit";
        }

        if (sourceShader.StartsWith("Unlit/"))
        {
            return "Universal Render Pipeline/Unlit";
        }

        return "Universal Render Pipeline/Lit";
    }

    private static Material CreateMaterial(string shaderPath)
    {
        var shader = Shader.Find(shaderPath);
        return shader ? new Material(shader) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
    }
}
#endif
