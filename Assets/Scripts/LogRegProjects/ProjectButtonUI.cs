using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class ProjectButtonUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI dateText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI userText;

    [SerializeField] private Button mainButton;
    [SerializeField] private Button archiveButton;
    [SerializeField] private Button unarchiveButton;
    [SerializeField] private Button deleteButton;

    private Project currentProject;

    public event Action<Project> OnClick;
    public event Action<Project> OnDelete;
    public event Action<Project> OnArchive;
    public event Action<Project> OnUnarchive;

    void Start()
    {
        if (mainButton != null)
            mainButton.onClick.AddListener(() => OnClick?.Invoke(currentProject));

        if (deleteButton != null)
            deleteButton.onClick.AddListener(() => OnDelete?.Invoke(currentProject));

        if (archiveButton != null)
            archiveButton.onClick.AddListener(() => OnArchive?.Invoke(currentProject));

        if (unarchiveButton != null)
            unarchiveButton.onClick.AddListener(() => OnUnarchive?.Invoke(currentProject));
    }

    public void SetData(Project project, bool isAdmin, bool canDelete)
    {
        currentProject = project;

        if (nameText != null)
            nameText.text = project.Name;

        if (dateText != null)
            dateText.text = project.CreatedAtFormatted;

        if (descriptionText != null)
            descriptionText.text = string.IsNullOrEmpty(project.Description)
                ? "Нет описания"
                : project.Description;

        if (userText != null)
        {
            if (isAdmin)
            {
                userText.gameObject.SetActive(true);
                userText.text = $"U: {project.Username}";
            }
            else
            {
                userText.gameObject.SetActive(false);
            }
        }

        if (archiveButton != null && unarchiveButton != null)
        {
            if (project.IsArchived)
            {
                archiveButton.gameObject.SetActive(false);
                unarchiveButton.gameObject.SetActive(true);
            }
            else
            {
                archiveButton.gameObject.SetActive(true);
                unarchiveButton.gameObject.SetActive(false);
            }
        }

        if (deleteButton != null)
            deleteButton.gameObject.SetActive(canDelete);
    }
}
