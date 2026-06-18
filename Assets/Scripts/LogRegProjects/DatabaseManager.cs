using UnityEngine;
using System;
using System.Threading.Tasks;
using System.Data.SqlClient;

public class DatabaseManager : MonoBehaviour
{
    private string connectionString = "Server=127.0.0.1,1433;Database=factory;User Id=sa;Password=123456Aa!;TrustServerCertificate=True;";

    private static DatabaseManager instance;
    public static DatabaseManager Instance => instance;

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

    public event Action OnLoginSuccess;

    public User CurrentUser { get; private set; }

    public bool IsAdmin => CurrentUser != null && CurrentUser.Role == "Admin";

    public async Task<(bool success, string message)> Register(string username, string email, string password)
    {
        try
        {
            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            var checkCmd = new SqlCommand(
                "SELECT COUNT(*) FROM Users WHERE Username=@u OR Email=@e", conn);

            checkCmd.Parameters.AddWithValue("@u", username);
            checkCmd.Parameters.AddWithValue("@e", email);

            int count = (int)await checkCmd.ExecuteScalarAsync();

            if (count > 0)
                return (false, "\u041f\u043e\u043b\u044c\u0437\u043e\u0432\u0430\u0442\u0435\u043b\u044c \u0443\u0436\u0435 \u0441\u0443\u0449\u0435\u0441\u0442\u0432\u0443\u0435\u0442");

            var cmd = new SqlCommand(@"
                INSERT INTO Users (Username, Email, PasswordHash, Role)
                VALUES (@u, @e, HASHBYTES('SHA2_256', CAST(@p AS NVARCHAR(4000))), 'User')", conn);

            cmd.Parameters.AddWithValue("@u", username);
            cmd.Parameters.AddWithValue("@e", email);
            cmd.Parameters.AddWithValue("@p", password);

            await cmd.ExecuteNonQueryAsync();

            return (true, "\u0420\u0435\u0433\u0438\u0441\u0442\u0440\u0430\u0446\u0438\u044f \u0443\u0441\u043f\u0435\u0448\u043d\u0430");
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

                return (true, CurrentUser, "\u0414\u043e\u0431\u0440\u043e \u043f\u043e\u0436\u0430\u043b\u043e\u0432\u0430\u0442\u044c");
            }

            return (false, null, "\u041d\u0435\u0432\u0435\u0440\u043d\u044b\u0439 \u043b\u043e\u0433\u0438\u043d \u0438\u043b\u0438 \u043f\u0430\u0440\u043e\u043b\u044c");
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
}
