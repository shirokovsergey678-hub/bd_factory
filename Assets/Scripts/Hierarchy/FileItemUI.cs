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
    [SerializeField] private Button openButton;       // открыть файл
    [SerializeField] private Button openPathButton;   // открыть папку
    [SerializeField] private Button deleteButton;

    private NodeFile file;

    public event Action<NodeFile> OnDelete;

    public void Setup(NodeFile f)
    {
        file = f;

        fileNameText.text = f.FileName;

        // открыть сам файл
        openButton.onClick.RemoveAllListeners();
        openButton.onClick.AddListener(OpenFile);

        // открыть папку и выделить файл
        openPathButton.onClick.RemoveAllListeners();
        openPathButton.onClick.AddListener(OpenInFolder);

        // удалить
        deleteButton.onClick.RemoveAllListeners();
        deleteButton.onClick.AddListener(() => OnDelete?.Invoke(file));
    }

    // --------------------------
    // ОТКРЫТЬ ФАЙЛ
    // --------------------------
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

    // --------------------------
    // ОТКРЫТЬ ПАПКУ
    // --------------------------
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

        path = Path.GetFullPath(path); // 🔥 ВАЖНО

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