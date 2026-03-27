using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Data.SqlClient;
using System.Threading.Tasks;

public class DatabaseManager : MonoBehaviour
{
    // СТРОКА ПОДКЛЮЧЕНИЯ - ИСПРАВЬ ПОД СВОЮ БАЗУ!
    // Вариант 1: Windows аутентификация (если используешь Windows)
    private string connectionString = "Server=localhost;Database=CatalogDB;Integrated Security=True;";

    // Вариант 2: Если с логином/паролем (раскомментируй и исправь)
    // private string connectionString = "Server=localhost;Database=CatalogDB;User Id=sa;Password=your_password;";

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

    // МЕТОД РЕГИСТРАЦИИ
    public async Task<(bool success, string message)> Register(string username, string email, string password)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();

                // Проверяем, нет ли такого пользователя
                string checkQuery = "SELECT COUNT(*) FROM Users WHERE Username = @username OR Email = @email";
                using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("@username", username);
                    checkCmd.Parameters.AddWithValue("@email", email);

                    int count = (int)await checkCmd.ExecuteScalarAsync();
                    if (count > 0)
                    {
                        return (false, "Пользователь с таким именем или email уже существует");
                    }
                }

                // Создаем нового пользователя
                string insertQuery = @"
                    INSERT INTO Users (Username, Email, PasswordHash, Role) 
                    VALUES (@username, @email, HASHBYTES('SHA2_256', @password), 'User')";

                using (SqlCommand insertCmd = new SqlCommand(insertQuery, conn))
                {
                    insertCmd.Parameters.AddWithValue("@username", username);
                    insertCmd.Parameters.AddWithValue("@email", email);
                    insertCmd.Parameters.AddWithValue("@password", password);

                    await insertCmd.ExecuteNonQueryAsync();
                    return (true, "Регистрация успешна! Теперь можно войти.");
                }
            }
        }
        catch (SqlException ex)
        {
            Debug.LogError($"Ошибка базы данных: {ex.Message}");
            return (false, $"Ошибка подключения: {ex.Message}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Ошибка: {ex.Message}");
            return (false, $"Ошибка: {ex.Message}");
        }
    }

    // МЕТОД ВХОДА
    public async Task<(bool success, User user, string message)> Login(string username, string password)
    {
        try
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();
                Debug.Log("Подключение к БД установлено");

                string query = @"
                    SELECT Id, Username, Email, Role 
                    FROM Users 
                    WHERE Username = @username 
                    AND PasswordHash = HASHBYTES('SHA2_256', @password)";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.Parameters.AddWithValue("@password", password);

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            CurrentUser = new User
                            {
                                Id = reader.GetInt32(0),
                                Username = reader.GetString(1),
                                Email = reader.GetString(2),
                                Role = reader.GetString(3)
                            };

                            Debug.Log($"Успешный вход: {CurrentUser.Username} (Роль: {CurrentUser.Role})");
                            return (true, CurrentUser, $"Добро пожаловать, {CurrentUser.Username}!");
                        }
                        else
                        {
                            Debug.Log("Неверный логин или пароль");
                            return (false, null, "Неверный логин или пароль");
                        }
                    }
                }
            }
        }
        catch (SqlException ex)
        {
            Debug.LogError($"Ошибка SQL: {ex.Message}");
            return (false, null, $"Ошибка подключения к базе: {ex.Message}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Ошибка: {ex.Message}");
            return (false, null, $"Ошибка: {ex.Message}");
        }
    }

    // ВЫХОД
    public void Logout()
    {
        CurrentUser = null;
        Debug.Log("Выход выполнен");
    }

    // ПРОВЕРКА ПРАВ (для админа)
    public bool IsAdmin => CurrentUser != null && CurrentUser.Role == "Admin";
}