#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

public static class URPInstaller
{
    private static AddRequest _add;

    [MenuItem("Jogo/Instalar URP (auto)", priority = 5)]
    public static void InstallURP()
    {
        _add = Client.Add("com.unity.render-pipelines.universal@14.0.11");
        EditorApplication.update += Progress;
        Debug.Log("Instalando URP 14.0.11 via Package Manager...");
    }

    private static void Progress()
    {
        if (_add != null && _add.IsCompleted)
        {
            EditorApplication.update -= Progress;
            if (_add.Status == StatusCode.Success)
            {
                Debug.Log("URP instalado: " + _add.Result.version);
            }
            else
            {
                Debug.LogError("Falhou ao instalar URP: " + _add.Error.message);
            }
        }
    }
}
#endif
