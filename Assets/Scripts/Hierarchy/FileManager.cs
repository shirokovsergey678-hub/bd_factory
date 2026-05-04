using System.IO;
using UnityEngine;

public static class FileManager
{
    public static string RootPath =>
        Path.Combine(Application.persistentDataPath, "ProjectFiles");

    public static string SaveFile(string sourcePath)
    {
        if (!Directory.Exists(RootPath))
            Directory.CreateDirectory(RootPath);

        string fileName = Path.GetFileName(sourcePath);
        string destPath = Path.Combine(RootPath, fileName);

        File.Copy(sourcePath, destPath, true);

        return destPath;
    }

    public static void OpenFile(string path)
    {
        if (File.Exists(path))
            Application.OpenURL("file://" + path);
    }

    public static void DeleteFile(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
    }
}