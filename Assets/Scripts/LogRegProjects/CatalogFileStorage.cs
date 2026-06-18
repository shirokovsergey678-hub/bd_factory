using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;

using Debug = UnityEngine.Debug;

public static class CatalogFileStorage
{
    private static string RootPath => Path.Combine(Application.persistentDataPath, "CatalogStorage");
    private static string ImagesPath => Path.Combine(RootPath, "Images");
    private static string FilesPath => Path.Combine(RootPath, "Files");

    public static string ImportImage(string sourcePath)
    {
        return CopyToFolder(sourcePath, ImagesPath);
    }

    public static string ImportFile(string sourcePath)
    {
        return CopyToFolder(sourcePath, FilesPath);
    }

    static string CopyToFolder(string sourcePath, string folderPath)
    {
        Directory.CreateDirectory(folderPath);

        string extension = Path.GetExtension(sourcePath);
        string fileName = Guid.NewGuid() + extension;
        string destinationPath = Path.Combine(folderPath, fileName);

        File.Copy(sourcePath, destinationPath, true);
        return destinationPath;
    }

    public static void DeleteIfExists(string path)
    {
        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
            File.Delete(path);
    }

    public static void OpenInFolder(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            Debug.LogError("\u0424\u0430\u0439\u043b \u043d\u0435 \u043d\u0430\u0439\u0434\u0435\u043d: " + path);
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{Path.GetFullPath(path)}\"",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Debug.LogError(ex);
        }
    }
}
