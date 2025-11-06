#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class SSAFixMagenta
{
    [MenuItem("SSA/Consertar Magenta (Converter para URP)")]
    static void Run()
    {
        int trocados = 0, erros = 0;
        var rends = Object.FindObjectsOfType<Renderer>(true);
        foreach (var r in rends)
        {
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                var m = mats[i];
                if (!m)
                {
                    mats[i] = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    trocados++; continue;
                }

                var sh = m.shader ? m.shader.name : "Hidden/InternalErrorShader";
                bool precisaTrocar =
                    sh == "Hidden/InternalErrorShader" ||
                    sh == "Standard" ||
                    sh.StartsWith("Legacy Shaders/") ||
                    (sh.Contains("Particles") && !sh.Contains("Universal Render Pipeline")) ||
                    (sh.StartsWith("Unlit/") && !sh.StartsWith("Universal Render Pipeline"));

                if (!precisaTrocar) continue;

                try
                {
                    Material novo;
                    if (sh.Contains("Particles"))
                        novo = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                    else if (sh.StartsWith("Unlit/"))
                        novo = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                    else
                        novo = new Material(Shader.Find("Universal Render Pipeline/Lit"));

                    novo.CopyPropertiesFromMaterial(m);
                    novo.name = m.name + "_URP";
                    mats[i] = novo;
                    trocados++;
                }
                catch { erros++; }
            }
            r.sharedMaterials = mats;
        }
        Debug.Log($"[SSA Fix] Materiais convertidos: {trocados} | erros: {erros}");
    }
}
#endif
