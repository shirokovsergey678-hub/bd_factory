using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Threading.Tasks;

// Основная форма с проектами
public class ProjectsUI : MonoBehaviour
{
    [Header("Контейнер проектов")]
    public Transform projectsContainer;
    public GameObject projectButtonPrefab;

    [Header("Главные кнопки")]
    public Button addProjectButton;
    public Button logoutButton;
    public TextMeshProUGUI welcomeText; 
    
    [Header("Фильтры")]
    public TMP_InputField searchInput;
    public TMP_Dropdown sortDropdown;
    public Toggle showArchivedToggle;
    public TMP_Dropdown userDropdown; // только для админа

    [Header("Создание проекта")]
    public GameObject createProjectPanel;
    public TMP_InputField projectNameInput;
    public TMP_InputField projectDescriptionInput;
    public Button confirmCreateButton;
    public Button cancelCreateButton;
    public TextMeshProUGUI createProjectMessage;

    [Header("Удаление")]
    public GameObject deleteConfirmPanel;
    public TextMeshProUGUI confirmDeleteText;
    public Button confirmDeleteButton;
    public Button cancelDeleteButton;

    private DatabaseManager db;

    private List<Project> allProjects = new List<Project>();
    private Project projectToDelete;

    [Header("Панели текущего проекта")]
    [SerializeField] private GameObject projectsPanel;
    [SerializeField] private GameObject projectPanel;

    private Project currentProject;
    [SerializeField] private HierarchyUI hierarchyUI;

    void Awake()
    {
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
        deleteConfirmPanel.SetActive(false);

        // Подписки на фильтры
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
        if (db?.CurrentUser == null)
        {
            Debug.LogError("CurrentUser = null");
            return;
        }

        welcomeText.text =
            $"Привет, {db.CurrentUser.Username}! Роль: " +
            $"{(db.IsAdmin ? "Администратор" : "Пользователь")}";

        LoadProjects();
    }

    async void LoadProjects()
    {
        ClearProjectsContainer();

        allProjects = await db.GetProjectsAsync();

        //обновляем дропдаун пользователей и добавляем имеющихся юзеров в него
        SetupUserDropdown();
        ApplyFilters();
    }

    void ClearProjectsContainer()
    {
        foreach (Transform child in projectsContainer)
            Destroy(child.gameObject);
    }

    void ApplyFilters()
    {
        List<Project> filtered = new List<Project>(allProjects);

        //Поиск
        if (!string.IsNullOrEmpty(searchInput.text))
        {
            string search = searchInput.text.ToLower();
            filtered = filtered.FindAll(p => p.Name.ToLower().Contains(search));
        }

        //Архив
        if (!showArchivedToggle.isOn)
        {
            filtered = filtered.FindAll(p => !p.IsArchived);
        }

        //Пользователь (только админ)
        if (db.IsAdmin && userDropdown != null && userDropdown.value > 0)
        {
            int selectedUserId =
                int.Parse(userDropdown.options[userDropdown.value].text.Split(':')[0]);

            filtered = filtered.FindAll(p => p.CreatedBy == selectedUserId);
        }

        //Сортировка
        switch (sortDropdown.value)
        {
            case 0: // новые
                filtered.Sort((a, b) => b.CreatedAt.CompareTo(a.CreatedAt));
                break;

            case 1: // старые
                filtered.Sort((a, b) => a.CreatedAt.CompareTo(b.CreatedAt));
                break;

            case 2: // A-Z
                filtered.Sort((a, b) => a.Name.CompareTo(b.Name));
                break;

            case 3: // Z-A
                filtered.Sort((a, b) => b.Name.CompareTo(a.Name));
                break;
        }

        UpdateProjectsUI(filtered);
    }

    void UpdateProjectsUI(List<Project> filtered)
    {
        ClearProjectsContainer();

        foreach (var project in filtered)
            CreateProjectButton(project);
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

        userDropdown.gameObject.SetActive(true);
        userDropdown.ClearOptions();

        List<string> options = new List<string> { "Все" };
        HashSet<string> users = new HashSet<string>();

        foreach (var p in allProjects)
            users.Add($"{p.CreatedBy}:{p.Username}");

        options.AddRange(users);
        userDropdown.AddOptions(options);
    }

    void CreateProjectButton(Project project)
    {
        GameObject obj = Instantiate(projectButtonPrefab, projectsContainer);
        ProjectButtonUI ui = obj.GetComponent<ProjectButtonUI>();

        if (ui == null) return;

        ui.SetData(project, db.IsAdmin);

        ui.OnArchive += ArchiveProject;
        ui.OnUnarchive += UnarchiveProject;
        ui.OnClick += OpenProject;

        if (db.IsAdmin)
            ui.OnDelete += ShowDeleteConfirm;
    }

    void OpenProject(Project project)
    {
        //projectsPanel.SetActive(false);
        projectPanel.SetActive(true);

        hierarchyUI.LoadHierarchy(project.Id);
    }
    public void BackToProjects()
    {
        projectPanel.SetActive(false);
        projectsPanel.SetActive(true);

        currentProject = null;
    }

    async void ArchiveProject(Project project)
    {
        var result = await db.ArchiveProjectAsync(project.Id);

        if (result.success)
            LoadProjects();
    }

    async void UnarchiveProject(Project project)
    {
        var result = await db.UnarchiveProjectAsync(project.Id);

        if (result.success)
            LoadProjects();
    }

    void ShowDeleteConfirm(Project project)
    {
        projectToDelete = project;

        deleteConfirmPanel.SetActive(true);
        confirmDeleteText.text =
            $"Удалить проект \"{project.Name}\"?\n" +
            $"После удаления восстановить невозможно!";
    }

    async void ConfirmDelete()
    {
        if (projectToDelete == null) return;

        confirmDeleteButton.interactable = false;

        var result = await db.DeleteProjectAsync(projectToDelete.Id);

        deleteConfirmPanel.SetActive(false);

        if (result.success)
        {
            projectToDelete = null;
            LoadProjects();
        }

        await Task.Delay(200);
        confirmDeleteButton.interactable = true;
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

        confirmDeleteButton.onClick.RemoveAllListeners();
        confirmDeleteButton.onClick.AddListener(ConfirmDelete);

        cancelDeleteButton.onClick.RemoveAllListeners();
        cancelDeleteButton.onClick.AddListener(() => deleteConfirmPanel.SetActive(false));
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
        authUI.regMessage.text = "";
        authUI.ResetColorInputField();
    }

}