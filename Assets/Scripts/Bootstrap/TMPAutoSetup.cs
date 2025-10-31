using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

namespace CleanRPG.Bootstrap
{
    public static class TMPAutoSetup
    {
        [MenuItem("Tools/Setup TMP Automatically")]
        public static void SetupTMP()
        {
            // Create basic TMP folder structure if it doesn't exist
            string tmpPath = "Assets/TextMeshPro";
            string resourcesPath = "Assets/TextMeshPro/Resources";
            
            if (!AssetDatabase.IsValidFolder(tmpPath))
            {
                AssetDatabase.CreateFolder("Assets", "TextMeshPro");
            }
            
            if (!AssetDatabase.IsValidFolder(resourcesPath))
            {
                AssetDatabase.CreateFolder("Assets/TextMeshPro", "Resources");
            }
            
            AssetDatabase.Refresh();
            Debug.Log("TMP folders created successfully!");
        }
        
        [InitializeOnLoadMethod]
        static void AutoSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (!AssetDatabase.IsValidFolder("Assets/TextMeshPro"))
                {
                    SetupTMP();
                }
            };
        }
    }
}
#endif