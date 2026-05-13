using UnityEngine;
using UnityEngine.UI;
using System;

public class SchemeItemUI : MonoBehaviour
{
    [SerializeField] private Image image;
    [SerializeField] private Button deleteButton;

    public event Action<SchemeItemUI> OnDelete;

    public string FilePath { get; private set; }

    public void Setup(Sprite sprite, string path)
    {
        image.sprite = sprite;
        FilePath = path;

        deleteButton.onClick.RemoveAllListeners();
        deleteButton.onClick.AddListener(() =>
        {
            OnDelete?.Invoke(this);
        });
    }
}