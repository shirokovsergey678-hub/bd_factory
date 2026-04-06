using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;
using Unity.VisualScripting;

// Логин/регистрация
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
    public Toggle LoginShowPasswordToggle;

    [Header("Регистрация")]
    public TMP_InputField regUsername;
    public TMP_InputField regEmail;
    public TMP_InputField regPassword;
    public TMP_InputField regConfirmPassword;
    public Button registerButton;
    public Button backToLoginButton;
    public TextMeshProUGUI regMessage;
    public Toggle RegistrationShowPasswordToggle;

    private DatabaseManager db;

    void Start()
    {
        db = DatabaseManager.Instance;
        if (db == null) return;

        loginButton.onClick.AddListener(OnLogin);
        goToRegisterButton.onClick.AddListener(() => ShowPanel(registerPanel));
        registerButton.onClick.AddListener(OnRegister);
        backToLoginButton.onClick.AddListener(() => ShowPanel(loginPanel));

        ShowPanel(loginPanel);

        ShowPassword();
    }
    
    public void ShowPassword()
    {
        if (RegistrationShowPasswordToggle.isOn)
        {
            regPassword.contentType = TMP_InputField.ContentType.Standard;
            regConfirmPassword.contentType = TMP_InputField.ContentType.Standard;
        }
        else
        {
            regPassword.contentType = TMP_InputField.ContentType.Password;
            regConfirmPassword.contentType = TMP_InputField.ContentType.Password;
        }        
        if (LoginShowPasswordToggle.isOn)
        {
            loginPassword.contentType = TMP_InputField.ContentType.Standard;
        }
        else
        {
            loginPassword.contentType = TMP_InputField.ContentType.Password;
        }

        // обновить поле
        regPassword.ForceLabelUpdate();
        regConfirmPassword.ForceLabelUpdate();
        loginPassword.ForceLabelUpdate();
    }

    Color colorOK = new Color(255, 255, 255);
    Color colorERR = new Color(255, 0, 0);

    void CheckInputFieldColor(TMP_InputField inputField)
    {
        if (string.IsNullOrEmpty(inputField.text))
        {
            inputField.GetComponent<Image>().color = colorERR;
        }
        else inputField.GetComponent<Image>().color = colorOK;
    }
    public void ResetColorInputField()
    {
        loginUsername.GetComponent<Image>().color = colorOK;
        loginPassword.GetComponent<Image>().color = colorOK;
        regUsername.GetComponent<Image>().color = colorOK;
        regEmail.GetComponent<Image>().color = colorOK;
        regPassword.GetComponent<Image>().color = colorOK;
        regConfirmPassword.GetComponent<Image>().color = colorOK;
    }

    async void OnLogin()
    {
        string username = loginUsername.text;
        string password = loginPassword.text;

        CheckInputFieldColor(loginUsername);
        CheckInputFieldColor(loginPassword);

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        { 
            loginMessage.text = "Заполните все поля!";
            loginMessage.color = Color.red;
            return;
        }

        loginButton.interactable = false;
        loginMessage.text = "Вход...";
        loginMessage.color = Color.yellow;

        var result = await db.Login(username, password);

        loginButton.interactable = true;

        if (result.success)
        {
            loginMessage.text = result.message;
            loginMessage.color = Color.green;
            ShowMainPanel();
        }
        else
        {
            loginMessage.text = result.message;
            loginMessage.color = Color.red;
        }
    }

    void ShowMainPanel()
    {
        ShowPanel(mainPanel);
    }

    async void OnRegister()
    {
        string username = regUsername.text;
        string email = regEmail.text;
        string password = regPassword.text;
        string confirm = regConfirmPassword.text;

        CheckInputFieldColor(regUsername);
        CheckInputFieldColor(regEmail);
        CheckInputFieldColor(regPassword);
        CheckInputFieldColor(regConfirmPassword);

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirm))
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

            await Task.Delay(1000);
            ShowPanel(loginPanel);
        }
        else
        {
            regMessage.text = result.message;
            regMessage.color = Color.red;
        }
    }

    void ShowPanel(GameObject panel)
    {
        loginPanel.SetActive(false);
        registerPanel.SetActive(false);
        mainPanel.SetActive(false);
        panel.SetActive(true);
    }
}