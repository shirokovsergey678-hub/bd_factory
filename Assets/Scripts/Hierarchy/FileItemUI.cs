using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Diagnostics;
using System.IO;

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
        openPathButton.onClick.AddListener(OpenInFolder);

        deleteButton.onClick.RemoveAllListeners();
        deleteButton.onClick.AddListener(() => OnDelete?.Invoke(file));
    }

    void OpenFile()
    {
        if (file == null || string.IsNullOrEmpty(file.FilePath))
        {
            Debug.LogError("Файл не задан");
            return;
        }

        string path = Path.GetFullPath(file.FilePath);

        if (!File.Exists(path))
        {
            Debug.LogError("Файл не найден: " + path);
            return;
        }

        Application.OpenURL("file:///" + path);
    }

    void OpenInFolder()
    {
        if (file == null || string.IsNullOrEmpty(file.FilePath))
        {
            Debug.LogError("Путь не задан");
            return;
        }

        string path = file.FilePath;

        if (!File.Exists(path))
        {
            Debug.LogError("Файл не найден: " + path);
            return;
        }

        path = Path.GetFullPath(path);

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{path}\"",
                UseShellExecute = true
            });
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
    }
}
