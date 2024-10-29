#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.IO;
using System.Linq;

[InitializeOnLoad]
public static class CopiadorDeRecursos
{
    static CopiadorDeRecursos()
    {
        // Automatically called when the Unity editor loads or detects changes
        CopySpritesToResources();
    }

    private static void CopySpritesToResources()
    {
        // Find the Assets folder in a case-insensitive manner
        string assetsFolderPath = Directory.EnumerateDirectories(Directory.GetCurrentDirectory(), "*", SearchOption.TopDirectoryOnly)
            .FirstOrDefault(d => string.Equals(Path.GetFileName(d), "assets", System.StringComparison.OrdinalIgnoreCase));

        if (assetsFolderPath == null)
        {
            Debug.LogError("Assets folder not found.");
            return;
        }

        string sourcePath = Path.Combine(assetsFolderPath, "pacoteMovimento/sprites/tutorial");
        string destinationPath = Path.Combine(assetsFolderPath, "Resources/pacoteMovimento/tutorial");

        if (!Directory.Exists(sourcePath))
        {
            Debug.LogWarning($"Source path '{sourcePath}' does not exist.");
            return;
        }

        // Ensure the destination directory exists
        if (!Directory.Exists(destinationPath))
        {
            Directory.CreateDirectory(destinationPath);
        }

        // Copy files recursively
        CopyDirectory(sourcePath, destinationPath);
        AssetDatabase.Refresh(); // Refresh asset database to reflect changes
        Debug.Log("Sprites copiados para Resources/pacoteMovimento/tutorial pasta.");
    }

    private static void CopyDirectory(string sourceDir, string destinationDir)
    {
        string projectPath = Application.dataPath; // Project folder path
        sourceDir = MakeRelativePath(projectPath, sourceDir); 
        destinationDir = MakeRelativePath(projectPath, destinationDir);

        foreach (var filePath in Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories))
        {
            string relativePath = Path.GetRelativePath(sourceDir, filePath);
            string destinationFilePath = Path.Combine(destinationDir, relativePath);

            Directory.CreateDirectory(Path.GetDirectoryName(destinationFilePath));
            AssetDatabase.CopyAsset(filePath, destinationFilePath);
        }
    }
    private static string MakeRelativePath(string fromPath, string toPath)
    {
        Uri fromUri = new Uri(fromPath);
        Uri toUri = new Uri(toPath);

        if (fromUri.Scheme != toUri.Scheme) { return toPath; } // Paths must have the same scheme

        Uri relativeUri = fromUri.MakeRelativeUri(toUri);
        return Uri.UnescapeDataString(relativeUri.ToString()).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
    }
}
#endif
