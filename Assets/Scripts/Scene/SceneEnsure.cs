using UnityEngine;

namespace CleanRPG.Systems
{
    public static class SceneEnsure
    {
        public static void EnsureEssentials()
        {
            // Camera
            if (Camera.main == null)
            {
                var cam = new GameObject("Main Camera", typeof(Camera));
                cam.tag = "MainCamera";
                cam.transform.position = new Vector3(0, 5f, -8f);
                cam.transform.rotation = Quaternion.Euler(15f, 0f, 0f);
            }

            // Light
            if (Object.FindFirstObjectByType<Light>() == null)
            {
                var lightGO = new GameObject("Directional Light", typeof(Light));
                var l = lightGO.GetComponent<Light>();
                l.type = LightType.Directional;
                l.color = new Color(1f, 0.956f, 0.839f);
                lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }

            // Ground
            if (GameObject.Find("ArenaGround") == null)
            {
                var g = GameObject.CreatePrimitive(PrimitiveType.Plane);
                g.name = "ArenaGround";
                Object.Destroy(g.GetComponent<Collider>());
                var mr = g.GetComponent<Renderer>();
                var m = new Material(Shader.Find("Standard")); m.color = new Color(0.22f,0.22f,0.26f);
                mr.sharedMaterial = m;
                g.transform.position = Vector3.zero;
            }
        }
    }
}
