using System.IO;
using UnityEditor;
using UnityEngine;

namespace ToonSetup
{
    /// <summary>
    /// Quick helpers to create ramp and matcap textures procedurally so the shader works out of the box.
    /// </summary>
    public static class CreateToonAssets
    {
        private const string RampFolder = "Assets/Shaders/Toon/Ramps";
        private const string MatcapFolder = "Assets/Shaders/Toon/MatCaps";

        [MenuItem("Tools/Toon Setup/Create Default Ramps & MatCap", priority = 10)]
        public static void CreateDefaults()
        {
            Directory.CreateDirectory(RampFolder);
            Directory.CreateDirectory(MatcapFolder);

            CreateRampTexture(Path.Combine(RampFolder, "ramp_skin.png"), new[]
            {
                new Color(0.05f, 0.03f, 0.02f),
                new Color(0.25f, 0.18f, 0.14f),
                new Color(0.7f, 0.55f, 0.45f)
            });

            CreateRampTexture(Path.Combine(RampFolder, "ramp_armor.png"), new[]
            {
                new Color(0.02f, 0.02f, 0.02f),
                new Color(0.2f, 0.2f, 0.25f),
                new Color(0.8f, 0.85f, 0.95f)
            });

            CreateMatcapTexture(Path.Combine(MatcapFolder, "matcap_gold.png"));

            AssetDatabase.Refresh();
            Debug.Log("Toon ramps and matcap generated.");
        }

        private static void CreateRampTexture(string path, Color[] stops)
        {
            const int width = 256;
            const int height = 1;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false, true)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            for (int x = 0; x < width; x++)
            {
                float t = x / (float)(width - 1);
                Color color = EvaluateRamp(t, stops);
                texture.SetPixel(x, 0, color);
            }

            byte[] bytes = texture.EncodeToPNG();
            File.WriteAllBytes(path, bytes);
            Object.DestroyImmediate(texture);
        }

        private static Color EvaluateRamp(float t, Color[] stops)
        {
            if (stops == null || stops.Length == 0)
            {
                return Color.white;
            }

            if (stops.Length == 1)
            {
                return stops[0];
            }

            float scaled = Mathf.Clamp01(t) * (stops.Length - 1);
            int index = Mathf.FloorToInt(scaled);
            int nextIndex = Mathf.Clamp(index + 1, 0, stops.Length - 1);
            float lerpT = scaled - index;
            return Color.Lerp(stops[index], stops[nextIndex], lerpT);
        }

        private static void CreateMatcapTexture(string path)
        {
            const int size = 512;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            Vector2 center = new Vector2(size - 1, size - 1) * 0.5f;
            float radius = center.x;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 uv = new Vector2(x, y);
                    float dist = Vector2.Distance(uv, center) / radius;
                    if (dist > 1.0f)
                    {
                        texture.SetPixel(x, y, new Color(0, 0, 0, 0));
                        continue;
                    }

                    float highlight = Mathf.Pow(1.0f - dist, 3.0f);
                    float warm = Mathf.Clamp01(0.6f + (1.0f - dist) * 0.4f);
                    var color = new Color(0.8f * warm, 0.7f * warm, 0.35f + 0.6f * highlight, 1.0f);
                    texture.SetPixel(x, y, color);
                }
            }

            byte[] bytes = texture.EncodeToPNG();
            File.WriteAllBytes(path, bytes);
            Object.DestroyImmediate(texture);
        }
    }
}
