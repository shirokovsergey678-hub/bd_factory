using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class FileItemUI : MonoBehaviour
{
    [SerializeField] private TMP_Text fileNameText;
    [SerializeField] private Button openButton;
    [SerializeField] private Button deleteButton;

    private NodeFile file;

    public event Action<NodeFile> OnDelete;

    public void Setup(NodeFile f)
    {
        file = f;

        fileNameText.text = f.FileName;

        openButton.onClick.AddListener(() =>
        {
            FileManager.OpenFile(f.FilePath);
        });

        deleteButton.onClick.AddListener(() =>
        {
            OnDelete?.Invoke(f);
        });
    }
}