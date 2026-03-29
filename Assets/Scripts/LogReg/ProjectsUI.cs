// ProjectsUI.cs - ИСПРАВЛЕННЫЙ
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Threading.Tasks;

public class ProjectsUI : MonoBehaviour
{
    [Header("Контейнер для кнопок проектов")]
    public Transform projectsContainer;      // Куда будут добавляться кнопки проектов
    public GameObject projectButtonPrefab;   // Префаб кнопки проекта

    [Header("Кнопка создания проекта (вне контейнера)")]
    public Button addProjectButton;          // Это отдельная кнопка, не в контейнере!

    [Header("Панель создания проекта")]
    public GameObject createProjectPanel;
    public TMP_InputField projectNameInput;
    public TMP_InputField projectDescriptionInput;
    public Button confirmCreateButton;
    public Button cancelCreateButton;
    public TextMeshProUGUI createProjectMessage;

    [Header("Кнопки управления")]
    public Button logoutButton;

    [Header("Информация о пользователе")]
    public TextMeshProUGUI welcomeText;

    [Header("Сообщение когда нет проектов")]
    public TextMeshProUGUI emptyMessageText;  // Текст "Нет проектов"

    private DatabaseManager db;
    private List<Project> projects = new List<Project>();

    void Start()
    {
        db = DatabaseManager.Instance;

        if (db == null)
        {
            Debug.LogError("DatabaseManager не найден!");
            return;
        }

        // Проверяем, что пользователь авторизован
        if (db.CurrentUser == null)
        {
            Debug.LogError("ProjectsUI: CurrentUser = null! Пользователь не авторизован");
            return;
        }

        // Обновляем приветствие
        welcomeText.text = $"Привет, {db.CurrentUser.Username}!\n{(db.IsAdmin ? "Администратор" : "Пользователь")}";

        // Назначаем кнопки
        if (addProjectButton != null)
        {
            addProjectButton.onClick.AddListener(ShowCreateProjectPanel);
        }

        logoutButton.onClick.AddListener(OnLogout);
        confirmCreateButton.onClick.AddListener(OnCreateProject);
        cancelCreateButton.onClick.AddListener(HideCreateProjectPanel);

        // Скрываем панель создания
        createProjectPanel.SetActive(false);

        // Загружаем проекты
        LoadProjects();
    }
    void OnEnable()
    {
        // Назначаем кнопки каждый раз при активации панели
        if (addProjectButton != null)
        {
            addProjectButton.onClick.RemoveAllListeners();
            addProjectButton.onClick.AddListener(ShowCreateProjectPanel);
        }

        if (logoutButton != null)
        {
            logoutButton.onClick.RemoveAllListeners();
            logoutButton.onClick.AddListener(OnLogout);
        }

        if (confirmCreateButton != null)
        {
            confirmCreateButton.onClick.RemoveAllListeners();
            confirmCreateButton.onClick.AddListener(OnCreateProject);
        }

        if (cancelCreateButton != null)
        {
            cancelCreateButton.onClick.RemoveAllListeners();
            cancelCreateButton.onClick.AddListener(HideCreateProjectPanel);
        }
    }

    void OnDisable()
    {
        // Опционально: отписываемся при деактивации
        if (addProjectButton != null)
            addProjectButton.onClick.RemoveAllListeners();

        if (logoutButton != null)
            logoutButton.onClick.RemoveAllListeners();

        if (confirmCreateButton != null)
            confirmCreateButton.onClick.RemoveAllListeners();

        if (cancelCreateButton != null)
            cancelCreateButton.onClick.RemoveAllListeners();
    }

    public async void LoadProjects()
    {
        Debug.Log("LoadProjects: Начинаем загрузку проектов...");

        // Очищаем контейнер
        ClearProjectsContainer();

        // Скрываем сообщение о пустоте
        if (emptyMessageText != null)
        {
            emptyMessageText.gameObject.SetActive(false);
        }

        // Загружаем проекты из БД
        projects = await db.GetProjectsAsync();

        Debug.Log($"LoadProjects: Получено проектов из БД: {projects.Count}");

        if (projects.Count == 0)
        {
            Debug.Log("LoadProjects: Нет проектов, показываем сообщение");
            // Показываем сообщение "Нет проектов"
            if (emptyMessageText != null)
            {
                emptyMessageText.text = "Нет проектов. Нажмите + чтобы создать первый проект";
                emptyMessageText.gameObject.SetActive(true);
            }
        }
        else
        {
            Debug.Log($"LoadProjects: Создаем {projects.Count} кнопок проектов");
            // Создаем кнопки для каждого проекта
            foreach (var project in projects)
            {
                Debug.Log($"Создаем кнопку для проекта: {project.Name} (Id={project.Id})");
                CreateProjectButton(project);
            }
        }
    }

    void CreateProjectButton(Project project)
    {
        if (projectButtonPrefab == null)
        {
            Debug.LogError("projectButtonPrefab не назначен в инспекторе!");
            return;
        }

        GameObject buttonObj = Instantiate(projectButtonPrefab, projectsContainer);
        ProjectButtonUI buttonUI = buttonObj.GetComponent<ProjectButtonUI>();

        if (buttonUI != null)
        {
            buttonUI.SetData(project);
            buttonUI.OnClick += (p) => OnProjectSelected(p);

            //Для админа показываем кнопку удаления
            if (db.IsAdmin)
            {
                buttonUI.ShowDeleteButton(true);
                buttonUI.OnDelete += OnDeleteProject;
            }
        }
        else
        {
            Debug.LogError("На префабе нет компонента ProjectButtonUI!");
        }
    }

    void ClearProjectsContainer()
    {
        // Удаляем только дочерние объекты в контейнере
        foreach (Transform child in projectsContainer)
        {
            Destroy(child.gameObject);
        }
    }

    void OnProjectSelected(Project project)
    {
        Debug.Log($"Выбран проект: {project.Name}");
        // TODO: Открыть CatalogUI с этим проектом
        // Здесь будет переход к каталогу
    }

    async void OnDeleteProject(Project project)
    {
        // TODO: Сделать нормальное окно подтверждения
        bool confirm = true; // Временная заглушка

        if (confirm)
        {
            var result = await db.ArchiveProjectAsync(project.Id);

            if (result.success)
            {
                Debug.Log($"Проект {project.Name} удален");
                // Перезагружаем список проектов
                LoadProjects();
            }
            else
            {
                Debug.LogError($"Ошибка удаления: {result.message}");
            }
        }
    }

    public void ShowCreateProjectPanel()
    {
        createProjectPanel.SetActive(true);
        projectNameInput.text = "";
        projectDescriptionInput.text = "";
        createProjectMessage.text = "";
    }

    void HideCreateProjectPanel()
    {
        createProjectPanel.SetActive(false);
    }

    async void OnCreateProject()
    {
        string name = projectNameInput.text.Trim();
        string description = projectDescriptionInput.text.Trim();

        if (string.IsNullOrEmpty(name))
        {
            createProjectMessage.text = "Введите название проекта!";
            createProjectMessage.color = Color.red;
            return;
        }

        confirmCreateButton.interactable = false;
        createProjectMessage.text = "Создание...";
        createProjectMessage.color = Color.yellow;

        var result = await db.CreateProjectAsync(name, description);

        confirmCreateButton.interactable = true;

        if (result.success)
        {
            createProjectMessage.text = result.message;
            createProjectMessage.color = Color.green;

            // Закрываем панель через 1 секунду и обновляем список
            await Task.Delay(1000);
            HideCreateProjectPanel();
            LoadProjects(); // Перезагружаем список проектов
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
        // Возвращаемся на панель авторизации
        gameObject.SetActive(false);

        // Находим AuthUI и показываем его
        AuthUI authUI = FindObjectOfType<AuthUI>(true);
        if (authUI != null)
        {
            authUI.gameObject.SetActive(true);
        }
    }
}