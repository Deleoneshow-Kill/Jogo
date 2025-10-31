using UnityEngine;

namespace CleanRPG.Systems
{
    public static class CharacterPrefabRegistry
    {
        public static GameObject LoadPrefabForId(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            id = id.ToLowerInvariant();
            if (id == "orion" || id == "gemini" || id == "orion_gemini")
                return Resources.Load<GameObject>("Characters/Prefabs/orion_gemini");
            if (id == "solaris" || id == "virgo" || id == "solaris_virgo")
                return Resources.Load<GameObject>("Characters/Prefabs/solaris_virgo");
            return null;
        }
    }
}
