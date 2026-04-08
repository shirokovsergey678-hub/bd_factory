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
    public TMP_InputField emptyMessageText;

    public GameObject createProjectPanel;
    public TMP_InputField projectNameInput;
    public TMP_InputField projectDescriptionInput;
    public Button confirmCreateButton;
    public Button cancelCreateButton;
    public TMP_InputField createProjectMessage;

    [Header("Фильтры")]
    public TMP_InputField searchInput;
    public TMP_Dropdown sortDropdown;
    public Toggle showArchivedToggle;
    public TMP_Dropdown userDropdown; // только для админа

    private DatabaseManager db;
    private List<Project> allProjects = new List<Project>();
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

        searchInput.onValueChanged.AddListener(_ => ApplyFilters());
        sortDropdown.onValueChanged.AddListener(_ => ApplyFilters());
        showArchivedToggle.onValueChanged.AddListener(_ => ApplyFilters());

        if (userDropdown != null)
            userDropdown.onValueChanged.AddListener(_ => ApplyFilters());
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

        allProjects = await db.GetProjectsAsync();
        SetupUserDropdown();
        ApplyFilters();

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
    void ApplyFilters()
    {
        List<Project> filtered = new List<Project>(allProjects);

        // 🔍 Поиск
        if (!string.IsNullOrEmpty(searchInput.text))
        {
            string search = searchInput.text.ToLower();
            filtered = filtered.FindAll(p => p.Name.ToLower().Contains(search));
        }

        // 📦 Архив
        if (!showArchivedToggle.isOn)
        {
            filtered = filtered.FindAll(p => !p.IsArchived);
        }

        // 👤 Пользователь (только админ)
        if (db.IsAdmin && userDropdown != null && userDropdown.value > 0)
        {
            int selectedUserId = int.Parse(userDropdown.options[userDropdown.value].text.Split(':')[0]);
            filtered = filtered.FindAll(p => p.CreatedBy == selectedUserId);
        }

        switch (sortDropdown.value)
        {
            case 0://сначала новые
                filtered.Sort((a, b) => b.CreatedAt.CompareTo(a.CreatedAt));
                break;

            case 1://сначала старые
                filtered.Sort((a, b) => a.CreatedAt.CompareTo(b.CreatedAt));
                break;

            case 2://нащвание A-Z
                filtered.Sort((a, b) => a.Name.CompareTo(b.Name));
                break;

            case 3://название Z-A
                filtered.Sort((a, b) => b.Name.CompareTo(a.Name));
                break;
        }

        // UI
        ClearProjectsContainer();

        if (filtered.Count == 0)
        {
            emptyMessageText.text = "Нет проектов";
            emptyMessageText.gameObject.SetActive(true);
        }
        else
        {
            emptyMessageText.gameObject.SetActive(false);

            foreach (var project in filtered)
                CreateProjectButton(project);
        }
    }

    void SetupUserDropdown()
    {
        if (userDropdown == null)
            return;

        if (!db.IsAdmin)
        {
            userDropdown.gameObject.SetActive(false);
            return;
        }

        // ВАЖНО!
        userDropdown.gameObject.SetActive(true);

        userDropdown.ClearOptions();

        List<string> options = new List<string>();
        options.Add("Все");

        HashSet<string> users = new HashSet<string>();

        foreach (var p in allProjects)
        {
            users.Add($"{p.CreatedBy}:{p.Username}");
        }

        options.AddRange(users);

        userDropdown.AddOptions(options);
    }

    async void UnarchiveProject(Project project)
    {
        var result = await DatabaseManager.Instance.UnarchiveProjectAsync(project.Id);

        if (result.success)
            LoadProjects();
    }

    void CreateProjectButton(Project project)
    {
        GameObject obj = Instantiate(projectButtonPrefab, projectsContainer);
        ProjectButtonUI ui = obj.GetComponent<ProjectButtonUI>();

        if (ui != null)
        {
            ui.SetData(project, db.IsAdmin);

            ui.OnArchive += ArchiveProject;
            ui.OnUnarchive += UnarchiveProject;

            if (db.IsAdmin)
            {
                ui.OnDelete += DeleteProject;
            }
        }

    }

    void ClearProjectsContainer()
    {
        foreach (Transform child in projectsContainer)
            Destroy(child.gameObject);
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
        }
    }

    async void ArchiveProject(Project project)
    {
        var result = await DatabaseManager.Instance.ArchiveProjectAsync(project.Id);

        if (result.success)
            LoadProjects();
    }

    async void DeleteProject(Project project)
    {
        var result = await DatabaseManager.Instance.DeleteProjectAsync(project.Id);

        if (result.success)
            LoadProjects();
    }

    public void OnLogout()
    {
        db.Logout();

        AuthUI authUI = FindObjectOfType<AuthUI>(true);
        authUI.loginPanel.SetActive(true);
        authUI.mainPanel.SetActive(false);
        authUI.loginUsername.text = "";
        authUI.loginPassword.text = "";
        authUI.loginMessage.text = "";
        authUI.ResetColorInputField();
    }
}