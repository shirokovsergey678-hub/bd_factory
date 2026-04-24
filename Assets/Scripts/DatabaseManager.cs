using UnityEngine;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Data.SqlClient;

public class DatabaseManager : MonoBehaviour
{
    private string connectionString = "Server=localhost;Database=factory;Integrated Security=True;";

    private static DatabaseManager instance;
    public static DatabaseManager Instance => instance;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    public event Action OnLoginSuccess;

    public User CurrentUser { get; private set; }

    public bool IsAdmin => CurrentUser != null && CurrentUser.Role == "Admin";

    // Регистрация
    public async Task<(bool success, string message)> Register(string username, string email, string password)
    {
        try
        {
            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            // Проверка на существование
            var checkCmd = new SqlCommand(
                "SELECT COUNT(*) FROM Users WHERE Username=@u OR Email=@e", conn);

            checkCmd.Parameters.AddWithValue("@u", username);
            checkCmd.Parameters.AddWithValue("@e", email);

            int count = (int)await checkCmd.ExecuteScalarAsync();

            if (count > 0)
                return (false, "Пользователь уже существует");

            // Добавление пользователя
            var cmd = new SqlCommand(@"
                INSERT INTO Users (Username, Email, PasswordHash, Role)
                VALUES (@u, @e, HASHBYTES('SHA2_256', CAST(@p AS NVARCHAR(4000))), 'User')", conn);

            cmd.Parameters.AddWithValue("@u", username);
            cmd.Parameters.AddWithValue("@e", email);
            cmd.Parameters.AddWithValue("@p", password);

            await cmd.ExecuteNonQueryAsync();

            return (true, "Регистрация успешна");
        }
        catch (Exception ex)
        {
            Debug.LogError(ex);
            return (false, ex.Message);
        }
    }

    // Логин
    public async Task<(bool success, User user, string message)> Login(string username, string password)
    {
        try
        {
            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            var cmd = new SqlCommand(@"
                SELECT Id, Username, Email, Role
                FROM Users
                WHERE Username=@u 
                AND PasswordHash = HASHBYTES('SHA2_256', CAST(@p AS NVARCHAR(4000)))", conn);

            cmd.Parameters.AddWithValue("@u", username);
            cmd.Parameters.AddWithValue("@p", password);

            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                CurrentUser = new User
                {
                    Id = reader.GetInt32(0),
                    Username = reader.GetString(1),
                    Email = reader.GetString(2),
                    Role = reader.GetString(3)
                };

                OnLoginSuccess?.Invoke();

                return (true, CurrentUser, "Добро пожаловать");
            }

            return (false, null, "Неверный логин или пароль");
        }
        catch (Exception ex)
        {
            Debug.LogError(ex);
            return (false, null, ex.Message);
        }
    }

    public void Logout()
    {
        CurrentUser = null;
    }

    // Получение проектов
    public async Task<List<Project>> GetProjectsAsync()
    {
        var projects = new List<Project>();

        if (CurrentUser == null)
            return projects;

        using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();

        string query = IsAdmin
            ? @"SELECT p.Id, p.Name, p.Description, p.CreatedBy, p.CreatedAt, p.IsArchived, u.Username
               FROM Projects p
               JOIN Users u ON p.CreatedBy = u.Id
               ORDER BY p.CreatedAt DESC"
            : @"SELECT p.Id, p.Name, p.Description, p.CreatedBy, p.CreatedAt, p.IsArchived, u.Username
               FROM Projects p
               JOIN Users u ON p.CreatedBy = u.Id
               WHERE p.CreatedBy=@id
               ORDER BY p.CreatedAt DESC";

        var cmd = new SqlCommand(query, conn);

        if (!IsAdmin)
            cmd.Parameters.AddWithValue("@id", CurrentUser.Id);

        //using для автоматического закрытия соединения
        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            projects.Add(new Project
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Description = reader.IsDBNull(2) ? "" : reader.GetString(2),
                CreatedBy = reader.GetInt32(3),
                CreatedAt = reader.GetDateTime(4),
                IsArchived = reader.GetBoolean(5),
                Username = reader.GetString(6)
            });
        }

        return projects;
    }

    // Создание проекта
    public async Task<(bool success, string message)> CreateProjectAsync(string name, string description)
    {
        try
        {
            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            var cmd = new SqlCommand(@"
                INSERT INTO Projects (Name, Description, CreatedBy)
                VALUES (@n, @d, @u)", conn);

            cmd.Parameters.AddWithValue("@n", name);
            cmd.Parameters.AddWithValue("@d", description ?? "");
            cmd.Parameters.AddWithValue("@u", CurrentUser.Id);

            await cmd.ExecuteNonQueryAsync();

            return (true, "Проект создан");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    // Архивация
    public async Task<(bool success, string message)> ArchiveProjectAsync(int id)
    {
        return await UpdateArchiveState(id, true);
    }

    // Разархивация
    public async Task<(bool success, string message)> UnarchiveProjectAsync(int id)
    {
        return await UpdateArchiveState(id, false);
    }

    public async Task<(bool success, string message)> UpdateArchiveState(int id, bool archived)
    {
        try
        {
            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            var cmd = new SqlCommand(
                "UPDATE Projects SET IsArchived=@state WHERE Id=@id", conn);

            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@state", archived);

            int rows = await cmd.ExecuteNonQueryAsync();

            return (rows > 0,
                rows > 0 ? (archived ? "Архивировано" : "Восстановлено") : "Не найдено");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    // Удаление
    public async Task<(bool success, string message)> DeleteProjectAsync(int id)
    {
        try
        {
            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            var cmd = new SqlCommand("DELETE FROM Projects WHERE Id=@id", conn);
            cmd.Parameters.AddWithValue("@id", id);

            int rows = await cmd.ExecuteNonQueryAsync();

            return (rows > 0, rows > 0 ? "Удалено" : "Не найдено");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}