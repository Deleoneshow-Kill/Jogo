using UnityEditor;
using UnityEngine;
using System;
using System.IO;
using System.IO.Compression;

public static class SSA_PackProject
{
    [MenuItem("SSA/Package/Build Zip (Jogo_YYYY-MM-DD_HH-MM-SS.zip)")]
    public static void BuildZip()
    {
        var projRoot = Directory.GetParent(Application.dataPath).FullName;
        string[] include = { "Assets", "Packages", "ProjectSettings", "UserSettings", "Docs", "INTEGRITY", "_Previews", "_RuleKit_V4", "Logs" };
        string[] excludeDirs = { "Library", "Temp", "obj", ".git", ".vs" };

        var stamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        var zipName = $"Jogo_{stamp}.zip";
        var outDir = Path.Combine(projRoot, "Builds");
        Directory.CreateDirectory(outDir);
        var zipPath = Path.Combine(outDir, zipName);
        if (File.Exists(zipPath)) File.Delete(zipPath);

        using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
        {
            foreach (var top in include)
            {
                var full = Path.Combine(projRoot, top);
                if (!Directory.Exists(full) && !File.Exists(full)) continue;

                if (Directory.Exists(full))
                {
                    foreach (var file in Directory.EnumerateFiles(full, "*", SearchOption.AllDirectories))
                    {
                        if (Array.Exists(excludeDirs, d => file.Contains(Path.DirectorySeparatorChar + d + Path.DirectorySeparatorChar))) continue;
                        if (file.EndsWith(".meta") || !file.Contains(Path.DirectorySeparatorChar + "Library" + Path.DirectorySeparatorChar))
                        {
                            var rel = file.Substring(projRoot.Length + 1).Replace('\\','/');
                            zip.CreateEntryFromFile(file, rel, System.IO.Compression.CompressionLevel.Optimal);
                        }
                    }
                }
                else
                {
                    var rel = top.Replace('\\','/');
                    zip.CreateEntryFromFile(full, rel, System.IO.Compression.CompressionLevel.Optimal);
                }
            }
        }

        Debug.Log($"[SSA] Pacote criado: {zipPath}");
        EditorUtility.RevealInFinder(zipPath);
    }
}
