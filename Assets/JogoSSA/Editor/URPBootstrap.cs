#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

/// <summary>
/// Garante automaticamente a presença do pacote Universal Render Pipeline.
/// Se o pacote não estiver listado no manifest, dispara a instalação via Package Manager.
/// </summary>
[InitializeOnLoad]
public static class URPBootstrap
{
    private const string PackageName = "com.unity.render-pipelines.universal";
    private const string PackageVersion = "14.0.11"; // Compatível com Unity 2022.3 LTS

    private static AddRequest _installRequest;

    static URPBootstrap()
    {
        if (IsUrpDeclared())
        {
            return;
        }

        Debug.Log("URP não encontrado no manifest. Iniciando instalação automática (" + PackageName + "@" + PackageVersion + ").");
        _installRequest = Client.Add(PackageName + "@" + PackageVersion);
        EditorApplication.update += OnEditorUpdate;
    }

    private static void OnEditorUpdate()
    {
        if (_installRequest == null)
        {
            EditorApplication.update -= OnEditorUpdate;
            return;
        }

        if (!_installRequest.IsCompleted)
        {
            return;
        }

        if (_installRequest.Status == StatusCode.Success)
        {
            Debug.Log("URP instalado com sucesso: " + _installRequest.Result.packageId);
        }
        else if (_installRequest.Status >= StatusCode.Failure)
        {
            Debug.LogError("Falha ao instalar URP: " + _installRequest.Error?.message);
        }

        _installRequest = null;
        EditorApplication.update -= OnEditorUpdate;
    }

    private static bool IsUrpDeclared()
    {
        try
        {
            string manifestPath = Path.Combine(Directory.GetCurrentDirectory(), "Packages", "manifest.json");
            if (!File.Exists(manifestPath))
            {
                return false;
            }

            string manifestText = File.ReadAllText(manifestPath);
            return manifestText.IndexOf(PackageName, StringComparison.OrdinalIgnoreCase) >= 0;
        }
        catch (Exception ex)
        {
            Debug.LogError("Não foi possível ler o manifest do Package Manager: " + ex.Message);
            return false;
        }
    }
}
#endif
