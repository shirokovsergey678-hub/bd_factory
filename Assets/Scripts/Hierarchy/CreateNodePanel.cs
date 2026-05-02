using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class CreateNodePanel : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_InputField input;
    [SerializeField] private Button createButton;
    [SerializeField] private Button cancelButton;

    private Action<string> onCreate;

    void Awake()
    {
        panel.SetActive(false);

        createButton.onClick.AddListener(() =>
        {
            if (!string.IsNullOrEmpty(input.text))
            {
                onCreate?.Invoke(input.text);
                Hide();
            }
        });

        cancelButton.onClick.AddListener(Hide);
    }

    public void Show(Action<string> createCallback)
    {
        panel.SetActive(true);
        input.text = "";
        onCreate = createCallback;
    }

    public void Hide()
    {
        panel.SetActive(false);
    }
}