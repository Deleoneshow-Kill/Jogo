using UnityEngine;
using UnityEditor;
using System.IO;

namespace CleanRPG.Setup
{
    public static class ProjectSetup
    {
        [MenuItem("Tools/Setup Project")]
        public static void SetupProject()
        {
            Debug.Log("Configurando projeto...");
            
            // 1. Importar TextMeshPro Essentials
            ImportTMPEssentials();
            
            // 2. Verificar pastas necessárias
            EnsureDirectories();
            
            // 3. Atualizar assets
            AssetDatabase.Refresh();
            
            Debug.Log("Projeto configurado com sucesso!");
        }
        
        [InitializeOnLoadMethod]
        static void AutoSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (!HasTMPEssentials())
                {
                    SetupProject();
                }
            };
        }
        
        static void ImportTMPEssentials()
        {
            string packagePath = "Packages/com.unity.textmeshpro/Package Resources/TMP Essential Resources.unitypackage";
            if (File.Exists(packagePath))
            {
                AssetDatabase.ImportPackage(packagePath, false);
                Debug.Log("TextMeshPro Essential Resources importados.");
            }
        }
        
        static bool HasTMPEssentials()
        {
            return AssetDatabase.IsValidFolder("Assets/TextMeshPro");
        }
        
        static void EnsureDirectories()
        {
            string[] dirs = {
                "Assets/TextMeshPro",
                "Assets/TextMeshPro/Resources",
                "Assets/TextMeshPro/Fonts",
                "Assets/TextMeshPro/Sprites"
            };
            
            foreach (string dir in dirs)
            {
                if (!AssetDatabase.IsValidFolder(dir))
                {
                    string parentDir = Path.GetDirectoryName(dir).Replace('\\', '/');
                    string folderName = Path.GetFileName(dir);
                    AssetDatabase.CreateFolder(parentDir, folderName);
                }
            }
        }
    }
}