using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

namespace CleanRPG.Bootstrap
{
    [InitializeOnLoad]
    public static class AutoImportTMP
    {
        const string EssentialsPrefsKey = "CleanRPG_TMP_Essentials_Imported";
        const string ExamplesPrefsKey = "CleanRPG_TMP_Examples_Imported";
        const string EssentialsCheckFolder = "Assets/TextMesh Pro/Resources";
        const string ExamplesCheckFolder = "Assets/TextMesh Pro/Examples & Extras";
        const string EssentialsMenu = "Window/TextMeshPro/Import TMP Essential Resources";
        const string ExamplesMenu = "Window/TextMeshPro/Import TMP Examples & Extras";

        static AutoImportTMP()
        {
            EditorApplication.delayCall += ImportTMPResources;
        }

        static void ImportTMPResources()
        {
            EditorApplication.delayCall -= ImportTMPResources;

            TryImportResources();
            TryImportExamples();
        }

        static void TryImportResources()
        {
            if (EditorPrefs.GetBool(EssentialsPrefsKey, false) && AssetDatabase.IsValidFolder(EssentialsCheckFolder))
                return;

            if (EditorApplication.ExecuteMenuItem(EssentialsMenu))
            {
                EditorPrefs.SetBool(EssentialsPrefsKey, true);
            }
        }

        static void TryImportExamples()
        {
            if (EditorPrefs.GetBool(ExamplesPrefsKey, false) && AssetDatabase.IsValidFolder(ExamplesCheckFolder))
                return;

            if (EditorApplication.ExecuteMenuItem(ExamplesMenu))
            {
                EditorPrefs.SetBool(ExamplesPrefsKey, true);
            }
        }
    }
}
#endif