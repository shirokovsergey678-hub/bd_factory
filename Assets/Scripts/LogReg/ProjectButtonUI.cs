using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

//Скрипт для префаба кнопки проекта в списке проектов
public class ProjectButtonUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI dateText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Button mainButton;
    [SerializeField] private Button deleteButton;

    private Project currentProject;

    public event Action<Project> OnClick;
    public event Action<Project> OnDelete;

    void Start()
    {
        if (mainButton != null)
            mainButton.onClick.AddListener(() => OnClick?.Invoke(currentProject));

        if (deleteButton != null)
            deleteButton.onClick.AddListener(() => OnDelete?.Invoke(currentProject));
    }

    public void SetData(Project project)
    {
        currentProject = project;

        if (nameText != null)
            nameText.text = project.Name;

        if (dateText != null)
            dateText.text = project.CreatedAtFormatted;

        if (descriptionText != null)
            descriptionText.text = string.IsNullOrEmpty(project.Description) ? "Нет описания" : project.Description;
    }
}