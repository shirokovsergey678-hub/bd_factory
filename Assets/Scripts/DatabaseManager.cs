using UnityEngine;
using System.Data.Odbc;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

public class DatabaseManager : MonoBehaviour
{
    // ODBC строка подключения
    private string connectionString = "Driver={SQL Server};Server=DESKTOP-CNBOKTJ\\SQLEXPRESS;Database=factory;Trusted_Connection=yes;";

    private static DatabaseManager instance;
    public static DatabaseManager Instance => instance;

    //private set - инкапсулируем от неправильного использования, тобишь можем изменять только в данном классе.
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
                //асинхронно выполняем соединение, дабы не ждать пока оно откроется а выполнять код дальше
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

    // ==================== РАБОТА С ПРОЕКТАМИ ====================

    // Получить все проекты (для обычного пользователя — только свои, для админа — все)
    // В DatabaseManager.cs, исправленный метод GetProjectsAsync
    public async Task<List<Project>> GetProjectsAsync()
    {
        var projects = new List<Project>();

        try
        {
            // ВАЖНО: Проверяем, есть ли текущий пользователь
            if (CurrentUser == null)
            {
                Debug.LogError("GetProjectsAsync: CurrentUser = null! Пользователь не авторизован");
                return projects;
            }

            using (OdbcConnection conn = new OdbcConnection(connectionString))
            {
                await conn.OpenAsync();
                Debug.Log($"GetProjectsAsync: Подключение открыто. IsAdmin = {IsAdmin}, CurrentUser.Id = {CurrentUser.Id}");

                string query;
                if (IsAdmin)
                {
                    query = "SELECT Id, Name, Description, CreatedBy, CreatedAt, IsArchived FROM Projects WHERE IsArchived = 0 ORDER BY CreatedAt DESC";
                    Debug.Log("GetProjectsAsync: Запрос для админа (все проекты)");
                }
                else
                {
                    query = "SELECT Id, Name, Description, CreatedBy, CreatedAt, IsArchived FROM Projects WHERE CreatedBy = ? AND IsArchived = 0 ORDER BY CreatedAt DESC";
                    Debug.Log($"GetProjectsAsync: Запрос для обычного пользователя (UserId = {CurrentUser.Id})");
                }

                using (OdbcCommand cmd = new OdbcCommand(query, conn))
                {
                    if (!IsAdmin)
                    {
                        cmd.Parameters.AddWithValue("@userId", CurrentUser.Id);
                    }

                    using (OdbcDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var project = new Project
                            {
                                Id = reader.GetInt32(0),
                                Name = reader.GetString(1),
                                Description = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                CreatedBy = reader.GetInt32(3),
                                CreatedAt = reader.GetDateTime(4),
                                IsArchived = reader.GetBoolean(5)
                            };
                            projects.Add(project);
                            Debug.Log($"Найден проект: Id={project.Id}, Name={project.Name}");
                        }
                        Debug.Log($"GetProjectsAsync: Всего найдено проектов: {projects.Count}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Ошибка при загрузке проектов: {ex.Message}");
            Debug.LogError($"Stack trace: {ex.StackTrace}");
        }

        return projects;
    }

    // Создать новый проект
    public async Task<(bool success, string message, Project project)> CreateProjectAsync(string name, string description)
    {
        try
        {
            using (OdbcConnection conn = new OdbcConnection(connectionString))
            {
                await conn.OpenAsync();

                // Проверяем, нет ли проекта с таким именем у пользователя
                string checkQuery = "SELECT COUNT(*) FROM Projects WHERE Name = ? AND CreatedBy = ?";
                using (OdbcCommand checkCmd = new OdbcCommand(checkQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("@name", name);
                    checkCmd.Parameters.AddWithValue("@userId", CurrentUser.Id);

                    int count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
                    if (count > 0)
                    {
                        return (false, "У вас уже есть проект с таким названием", null);
                    }
                }

                // Создаем проект
                string insertQuery = @"
                    INSERT INTO Projects (Name, Description, CreatedBy) 
                    VALUES (?, ?, ?);
                    SELECT SCOPE_IDENTITY();";

                using (OdbcCommand insertCmd = new OdbcCommand(insertQuery, conn))
                {
                    insertCmd.Parameters.AddWithValue("@name", name);
                    insertCmd.Parameters.AddWithValue("@description", description ?? "");
                    insertCmd.Parameters.AddWithValue("@userId", CurrentUser.Id);

                    int newId = Convert.ToInt32(await insertCmd.ExecuteScalarAsync());

                    var newProject = new Project
                    {
                        Id = newId,
                        Name = name,
                        Description = description ?? "",
                        CreatedBy = CurrentUser.Id,
                        CreatedAt = DateTime.Now,
                        IsArchived = false
                    };

                    return (true, "Проект успешно создан!", newProject);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Ошибка при создании проекта: {ex.Message}");
            return (false, $"Ошибка: {ex.Message}", null);
        }
    }

    // Удалить проект (архивировать)
    public async Task<(bool success, string message)> ArchiveProjectAsync(int projectId)
    {
        try
        {
            using (OdbcConnection conn = new OdbcConnection(connectionString))
            {
                await conn.OpenAsync();

                string query = "UPDATE Projects SET IsArchived = 1 WHERE Id = ?";
                using (OdbcCommand cmd = new OdbcCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@projectId", projectId);
                    int rows = await cmd.ExecuteNonQueryAsync();

                    if (rows > 0)
                    {
                        return (true, "Проект удален");
                    }
                    else
                    {
                        return (false, "Проект не найден");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Ошибка при удалении проекта: {ex.Message}");
            return (false, ex.Message);
        }
    }
}