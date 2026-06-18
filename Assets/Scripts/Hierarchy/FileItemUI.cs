using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Diagnostics;
using System.IO;
using SFB;

using Debug = UnityEngine.Debug;

public class FileItemUI : MonoBehaviour
{
    [SerializeField] private TMP_Text fileNameText;
    [SerializeField] private Button openButton;
    [SerializeField] private Button openPathButton;
    [SerializeField] private Button deleteButton;

    private NodeFile file;

    public event Action<NodeFile> OnDelete;

    public void Setup(NodeFile f)
    {
        file = f;

        fileNameText.text = f.FileName;

        openButton.onClick.RemoveAllListeners();
        openButton.onClick.AddListener(OpenFile);

        openPathButton.onClick.RemoveAllListeners();
        openPathButton.onClick.AddListener(SaveFileAs);

        deleteButton.onClick.RemoveAllListeners();
        deleteButton.onClick.AddListener(() => OnDelete?.Invoke(file));
    }

    void OpenFile()
    {
        string path = GetAvailableFilePath();

        if (string.IsNullOrEmpty(path))
        {
            Debug.LogError("Файл не задан");
            return;
        }

        Application.OpenURL("file:///" + path);
    }

    void SaveFileAs()
    {
        if (file == null)
        {
            Debug.LogError("Файл не задан");
            return;
        }

        string savePath = StandaloneFileBrowser.SaveFilePanel(
            "Сохранить файл",
            "",
            Path.GetFileNameWithoutExtension(file.FileName),
            Path.GetExtension(file.FileName).TrimStart('.')
        );

        if (string.IsNullOrEmpty(savePath))
            return;

        if (file.FileData != null && file.FileData.Length > 0)
        {
            File.WriteAllBytes(savePath, file.FileData);
            ShowFileInExplorer(savePath);
            return;
        }

        string sourcePath = GetAvailableFilePath();

        if (string.IsNullOrEmpty(sourcePath))
        {
            Debug.LogError("Файл не найден");
            return;
        }

        File.Copy(sourcePath, savePath, true);
        ShowFileInExplorer(savePath);
    }

    string GetAvailableFilePath()
    {
        if (file == null)
            return null;

        if (file.FileData != null && file.FileData.Length > 0)
            return RestoreFileFromDatabase();

        if (string.IsNullOrEmpty(file.FilePath))
            return null;

        string path = Path.GetFullPath(file.FilePath);
        return File.Exists(path) ? path : null;
    }

    string RestoreFileFromDatabase()
    {
        string folder = Path.Combine(Application.persistentDataPath, "DatabaseFiles");

        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        string safeName = string.Join("_", file.FileName.Split(Path.GetInvalidFileNameChars()));
        string path = Path.Combine(folder, $"{file.Id}_{safeName}");

        File.WriteAllBytes(path, file.FileData);
        return path;
    }

    void ShowFileInExplorer(string path)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{Path.GetFullPath(path)}\"",
                UseShellExecute = true
            });
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }
}
