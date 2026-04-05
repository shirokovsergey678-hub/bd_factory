using UnityEngine;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Data.SqlClient;

public class DatabaseManager : MonoBehaviour
{
    private string connectionString = "Server=localhost;Database=factory;Integrated Security=True;";

    public event Action OnLoginSuccess;

    private static DatabaseManager instance;
    public static DatabaseManager Instance => instance;

    public User CurrentUser { get; private set; }
    public bool IsAdmin => CurrentUser != null && CurrentUser.Role == "Admin";

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    public async Task<(bool success, string message)> Register(string username, string email, string password)
    {
        try
        {
            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            var checkCmd = new SqlCommand("SELECT COUNT(*) FROM Users WHERE Username=@u OR Email=@e", conn);
            checkCmd.Parameters.AddWithValue("@u", username);
            checkCmd.Parameters.AddWithValue("@e", email);

            int count = (int)await checkCmd.ExecuteScalarAsync();
            if (count > 0)
                return (false, "Пользователь уже существует");

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

    public async Task<(bool success, User user, string message)> Login(string username, string password)
    {
        try
        {
            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            var cmd = new SqlCommand(@"
                                    SELECT Id, Username, Email, Role
                                    FROM Users
                                    WHERE Username=@u AND PasswordHash = HASHBYTES('SHA2_256', CAST(@p AS NVARCHAR(4000)))", conn);

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

    public async Task<List<Project>> GetProjectsAsync()
    {
        var projects = new List<Project>();

        if (CurrentUser == null)
            return projects;

        using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();

        string query = IsAdmin
            ? "SELECT Id, Name, Description, CreatedBy, CreatedAt, IsArchived FROM Projects WHERE IsArchived=0 ORDER BY CreatedAt DESC"
            : "SELECT Id, Name, Description, CreatedBy, CreatedAt, IsArchived FROM Projects WHERE CreatedBy=@id AND IsArchived=0 ORDER BY CreatedAt DESC";

        var cmd = new SqlCommand(query, conn);

        if (!IsAdmin)
            cmd.Parameters.AddWithValue("@id", CurrentUser.Id);

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
                IsArchived = reader.GetBoolean(5)
            });
        }

        return projects;
    }

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

    public async Task<(bool success, string message)> ArchiveProjectAsync(int id)
    {
        try
        {
            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            var cmd = new SqlCommand("UPDATE Projects SET IsArchived=1 WHERE Id=@id", conn);
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