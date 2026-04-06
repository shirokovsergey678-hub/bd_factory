using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Threading.Tasks;

// Основная форма с проектами
public class ProjectsUI : MonoBehaviour
{
    public Transform projectsContainer;
    public GameObject projectButtonPrefab;
    public Button addProjectButton;
    public Button logoutButton;
    public TextMeshProUGUI welcomeText;
    public TextMeshProUGUI emptyMessageText;

    public GameObject createProjectPanel;
    public TMP_InputField projectNameInput;
    public TMP_InputField projectDescriptionInput;
    public Button confirmCreateButton;
    public Button cancelCreateButton;
    public TextMeshProUGUI createProjectMessage;

    private DatabaseManager db;
    private List<Project> projects = new List<Project>();

    void Awake()
    {
        //инициализация менеджера
        db = DatabaseManager.Instance;

        if (db != null)
            db.OnLoginSuccess += OnLoginSuccess;
    }

    void OnDestroy()
    {
        if (db != null)
            db.OnLoginSuccess -= OnLoginSuccess;
    }

    void Start()
    {
        SubscribeButtons();
        createProjectPanel.SetActive(false);
    }

    void OnLoginSuccess()
    {
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (db == null)
            db = DatabaseManager.Instance;

        if (db?.CurrentUser == null)
        {
            Debug.LogError("CurrentUser = null");
            return;
        }

        welcomeText.text = $"Привет, {db.CurrentUser.Username}!\n{(db.IsAdmin ? "Администратор" : "Пользователь")}";

        LoadProjects();
    }

    async void LoadProjects()
    {
        ClearProjectsContainer();
        emptyMessageText.gameObject.SetActive(false);

        projects = await db.GetProjectsAsync();

        if (projects.Count == 0)
        {
            emptyMessageText.text = "Нет проектов";
            emptyMessageText.gameObject.SetActive(true);
        }
        else
        {
            foreach (var project in projects)
                CreateProjectButton(project);
        }
    }

    void CreateProjectButton(Project project)
    {
        GameObject obj = Instantiate(projectButtonPrefab, projectsContainer);
        ProjectButtonUI ui = obj.GetComponent<ProjectButtonUI>();

        if (ui != null)
        {
            ui.SetData(project);

            if (db.IsAdmin)
            {
                ui.OnDelete += OnDeleteProject;
            }
        }
    }

    void ClearProjectsContainer()
    {
        foreach (Transform child in projectsContainer)
            Destroy(child.gameObject);
    }

    async void OnDeleteProject(Project project)
    {
        var result = await db.ArchiveProjectAsync(project.Id);

        if (result.success)
            LoadProjects();
    }

    void SubscribeButtons()
    {
        addProjectButton.onClick.RemoveAllListeners();
        addProjectButton.onClick.AddListener(() => createProjectPanel.SetActive(true));

        logoutButton.onClick.RemoveAllListeners();
        logoutButton.onClick.AddListener(OnLogout);

        confirmCreateButton.onClick.RemoveAllListeners();
        confirmCreateButton.onClick.AddListener(OnCreateProject);

        cancelCreateButton.onClick.RemoveAllListeners();
        cancelCreateButton.onClick.AddListener(() => createProjectPanel.SetActive(false));
    }

    async void OnCreateProject()
    {
        var result = await db.CreateProjectAsync(
            projectNameInput.text,
            projectDescriptionInput.text
        );

        if (result.success)
        {
            createProjectPanel.SetActive(false);
            projectNameInput.text = "";
            projectDescriptionInput.text = "";
            LoadProjects();
        }
        else
        {
            createProjectMessage.text = result.message;
            createProjectMessage.color = Color.red;
        }
    }

    void OnLogout()
    {
        db.Logout();

        AuthUI authUI = FindObjectOfType<AuthUI>(true);
        authUI.loginPanel.SetActive(true);
        authUI.mainPanel.SetActive(false);
        authUI.loginUsername.text = "";
        authUI.loginPassword.text = "";
        authUI.loginMessage.text = "";
    }
}