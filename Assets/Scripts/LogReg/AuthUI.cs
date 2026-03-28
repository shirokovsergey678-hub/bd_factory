using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;

public class AuthUI : MonoBehaviour
{
    [Header("Панели")]
    public GameObject loginPanel;
    public GameObject registerPanel;
    public GameObject mainPanel;

    [Header("Логин")]
    public TMP_InputField loginUsername;
    public TMP_InputField loginPassword;
    public Button loginButton;
    public Button goToRegisterButton;
    public TextMeshProUGUI loginMessage;

    [Header("Регистрация")]
    public TMP_InputField regUsername;
    public TMP_InputField regEmail;
    public TMP_InputField regPassword;
    public TMP_InputField regConfirmPassword;
    public Button registerButton;
    public Button backToLoginButton;
    public TextMeshProUGUI regMessage;

    [Header("Главное меню")]
    public TextMeshProUGUI welcomeText;
    public Button logoutButton;

    private DatabaseManager db;

    void Start()
    {
        db = DatabaseManager.Instance;

        if (db == null)
        {
            Debug.LogError("DatabaseManager не найден!");
            return;
        }

        loginButton.onClick.AddListener(OnLogin);
        goToRegisterButton.onClick.AddListener(() => ShowPanel(registerPanel));
        registerButton.onClick.AddListener(OnRegister);
        backToLoginButton.onClick.AddListener(() => ShowPanel(loginPanel));
        logoutButton.onClick.AddListener(OnLogout);

        ShowPanel(loginPanel);
    }

    // Убрали async, теперь синхронно
    void OnLogin()
    {
        string username = loginUsername.text;
        string password = loginPassword.text;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            loginMessage.text = "Заполните все поля!";
            loginMessage.color = Color.red;
            return;
        }

        loginButton.interactable = false;
        loginMessage.text = "Вход...";
        loginMessage.color = Color.yellow;

        // Синхронный вызов
        var result = db.Login(username, password);

        loginButton.interactable = true;

        if (result.success)
        {
            loginMessage.text = result.message;
            loginMessage.color = Color.green;
            welcomeText.text = $"Привет, {result.user.Username}! Роль: {result.user.Role}";
            ShowPanel(mainPanel);
        }
        else
        {
            loginMessage.text = result.message;
            loginMessage.color = Color.red;
        }
    }

    async void OnRegister()
    {
        string username = regUsername.text;
        string email = regEmail.text;
        string password = regPassword.text;
        string confirm = regConfirmPassword.text;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            regMessage.text = "Заполните все поля!";
            regMessage.color = Color.red;
            return;
        }

        if (password != confirm)
        {
            regMessage.text = "Пароли не совпадают!";
            regMessage.color = Color.red;
            return;
        }

        if (password.Length < 4)
        {
            regMessage.text = "Пароль минимум 4 символа!";
            regMessage.color = Color.red;
            return;
        }

        if (!email.Contains("@") || !email.Contains("."))
        {
            regMessage.text = "Введите корректный email!";
            regMessage.color = Color.red;
            return;
        }

        registerButton.interactable = false;
        regMessage.text = "Регистрация...";
        regMessage.color = Color.yellow;

        var result = await db.Register(username, email, password);

        registerButton.interactable = true;

        if (result.success)
        {
            regMessage.text = result.message;
            regMessage.color = Color.green;

            regUsername.text = "";
            regEmail.text = "";
            regPassword.text = "";
            regConfirmPassword.text = "";

            await Task.Delay(2000);
            ShowPanel(loginPanel);
        }
        else
        {
            regMessage.text = result.message;
            regMessage.color = Color.red;
        }
    }

    void OnLogout()
    {
        db.Logout();
        loginUsername.text = "";
        loginPassword.text = "";
        loginMessage.text = "";
        ShowPanel(loginPanel);
    }

    void ShowPanel(GameObject panel)
    {
        loginPanel.SetActive(false);
        registerPanel.SetActive(false);
        mainPanel.SetActive(false);

        panel.SetActive(true);
    }
}