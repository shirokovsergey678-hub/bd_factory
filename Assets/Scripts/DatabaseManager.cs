using UnityEngine;
using System.Data.Odbc;
using System;
using System.Threading.Tasks;

public class DatabaseManager : MonoBehaviour
{
    // ODBC строка подключения
    private string connectionString = "Driver={SQL Server};Server=DESKTOP-CNBOKTJ\\SQLEXPRESS;Database=factory;Trusted_Connection=yes;";

    private static DatabaseManager instance;
    public static DatabaseManager Instance => instance;

    public User CurrentUser { get; private set; }

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Регистрация (с async, тут все ок)
    public async Task<(bool success, string message)> Register(string username, string email, string password)
    {
        try
        {
            using (OdbcConnection conn = new OdbcConnection(connectionString))
            {
                await conn.OpenAsync();

                // Проверяем, нет ли такого пользователя
                string checkQuery = "SELECT COUNT(*) FROM Users WHERE Username = ? OR Email = ?";
                using (OdbcCommand checkCmd = new OdbcCommand(checkQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("@p1", username);
                    checkCmd.Parameters.AddWithValue("@p2", email);

                    int count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
                    if (count > 0)
                    {
                        return (false, "Пользователь с таким именем или email уже существует");
                    }
                }

                // Экранируем кавычки в пароле
                string escapedPassword = password.Replace("'", "''");

                // Формируем запрос с экранированным паролем
                string insertQuery = $@"
                    INSERT INTO Users (Username, Email, PasswordHash, Role) 
                    VALUES (?, ?, HASHBYTES('SHA2_256', '{escapedPassword}'), 'User')";

                Debug.Log($"Регистрация: {username}");

                using (OdbcCommand insertCmd = new OdbcCommand(insertQuery, conn))
                {
                    insertCmd.Parameters.AddWithValue("@p1", username);
                    insertCmd.Parameters.AddWithValue("@p2", email);

                    int rows = await insertCmd.ExecuteNonQueryAsync();
                    Debug.Log($"Добавлено строк: {rows}");

                    if (rows > 0)
                    {
                        return (true, "Регистрация успешна! Теперь можно войти.");
                    }
                    else
                    {
                        return (false, "Ошибка при создании пользователя");
                    }
                }
            }
        }
        catch (OdbcException ex)
        {
            Debug.LogError($"Ошибка ODBC: {ex.Message}");
            return (false, $"Ошибка базы данных: {ex.Message}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Ошибка: {ex.Message}");
            return (false, $"Ошибка: {ex.Message}");
        }
    }

    // Вход (синхронная версия, без async)
    public (bool success, User user, string message) Login(string username, string password)
    {
        try
        {
            using (OdbcConnection conn = new OdbcConnection(connectionString))
            {
                conn.Open();
                Debug.Log("Подключение к БД установлено");

                // Экранируем кавычки в пароле
                string escapedPassword = password.Replace("'", "''");

                string query = $@"
                    SELECT Id, Username, Email, Role 
                    FROM Users 
                    WHERE Username = ? 
                    AND PasswordHash = HASHBYTES('SHA2_256', '{escapedPassword}')";

                using (OdbcCommand cmd = new OdbcCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@username", username);

                    using (OdbcDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            CurrentUser = new User
                            {
                                Id = reader.GetInt32(0),
                                Username = reader.GetString(1),
                                Email = reader.GetString(2),
                                Role = reader.GetString(3)
                            };

                            Debug.Log($"Успешный вход: {CurrentUser.Username}");
                            return (true, CurrentUser, "Добро пожаловать!");
                        }
                    }
                }
            }
            return (false, null, "Неверный логин или пароль");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Ошибка: {ex.Message}");
            return (false, null, ex.Message);
        }
    }

    public void Logout()
    {
        CurrentUser = null;
        Debug.Log("Выход выполнен");
    }

    public bool IsAdmin => CurrentUser != null && CurrentUser.Role == "Admin";
}