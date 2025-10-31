using UnityEngine;

namespace CleanRPG.Systems
{
    public static class CharacterVisuals
    {
        public static void Build(GameObject host, object def) => Build(host, def, false);

        public static void Build(GameObject host, object def, bool isEnemy)
        {
            if (host == null) return;

            // disable base renderer and clear children
            var rend = host.GetComponentInChildren<Renderer>();
            if (rend != null) rend.enabled = false;
            for (int i = host.transform.childCount - 1; i >= 0; i--)
                Object.Destroy(host.transform.GetChild(i).gameObject);

            string id = def?.ToString() ?? "unknown";
            var prefab = CleanRPG.Systems.CharacterPrefabRegistry.LoadPrefabForId(id);
            if (prefab != null)
            {
                var go = Object.Instantiate(prefab, host.transform, false);
                go.name = $"{id}_prefab";
            }
            else
            {
                var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                body.transform.SetParent(host.transform, false);
                var mat = new Material(Shader.Find("Standard"));
                mat.color = isEnemy ? Color.red : new Color(0.75f,0.6f,1f);
                var mr = body.GetComponent<Renderer>(); if (mr) mr.sharedMaterial = mat;
            }

            AuraHelper.AttachGroundAura(host.transform, isEnemy ? Color.red : new Color(0.6f,0.4f,1f,1f));
        }
    }

    public static class AuraHelper
    {
        public static void AttachGroundAura(Transform parent, Color color)
        {
            if (parent == null) return;
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "GroundAura";
            var mr = go.GetComponent<Renderer>();
            mr.sharedMaterial = new Material(Shader.Find("Unlit/Color")){ color = new Color(color.r,color.g,color.b,0.35f)};
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localRotation = Quaternion.Euler(90f,0,0);
            go.transform.localScale = new Vector3(2.2f,2.2f,2.2f);
        }
    }
}
