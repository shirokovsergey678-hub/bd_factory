using UnityEngine;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Data.SqlClient;

public class DatabaseManager : MonoBehaviour
{
    private string connectionString = "Server=127.0.0.1,1433;Database=factory;Integrated Security=True;TrustServerCertificate=True;";

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
            name = name?.Trim();

            if (string.IsNullOrWhiteSpace(name))
                return (false, "Невозможно создать проект: поле названия не заполнено");

            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            var checkCmd = new SqlCommand(@"
                SELECT COUNT(*)
                FROM Projects
                WHERE CreatedBy=@u AND LOWER(Name)=LOWER(@n)", conn);

            checkCmd.Parameters.AddWithValue("@u", CurrentUser.Id);
            checkCmd.Parameters.AddWithValue("@n", name);

            int count = (int)await checkCmd.ExecuteScalarAsync();

            if (count > 0)
                return (false, "Проект с таким именем уже существует");

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

            string query = IsAdmin
                ? "DELETE FROM Projects WHERE Id=@id"
                : "DELETE FROM Projects WHERE Id=@id AND CreatedBy=@userId";

            var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", id);

            if (!IsAdmin)
                cmd.Parameters.AddWithValue("@userId", CurrentUser.Id);

            int rows = await cmd.ExecuteNonQueryAsync();

            return (rows > 0, rows > 0 ? "Удалено" : "Нет прав на удаление проекта");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    //--------------Иерархия-----------------
    //Получение узлов проекта
    public async Task<List<ProjectNode>> GetNodesAsync(int projectId)
    {
        var list = new List<ProjectNode>();

        using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();

        var cmd = new SqlCommand(@"
        SELECT Id, ProjectId, ParentId, Name, Description, Quantity 
        FROM ProjectNodes 
        WHERE ProjectId=@id", conn);

        cmd.Parameters.AddWithValue("@id", projectId);

        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            list.Add(new ProjectNode
            {
                Id = reader.GetInt32(0),
                ProjectId = reader.GetInt32(1),
                ParentId = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                Name = reader.GetString(3),

                Description = reader.IsDBNull(4) ? "" : reader.GetString(4),
                Quantity = reader.IsDBNull(5) ? 1 : reader.GetInt32(5)
            });
        }

        return list;
    }
    //Создание узла в бд
    public async Task<int> CreateNodeAsync(int projectId, int? parentId, string name)
    {
        using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();

        var cmd = new SqlCommand(@"
        INSERT INTO ProjectNodes (ProjectId, ParentId, Name)
        OUTPUT INSERTED.Id
        VALUES (@p, @parent, @name)", conn);

        cmd.Parameters.AddWithValue("@p", projectId);
        cmd.Parameters.AddWithValue("@parent", (object?)parentId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@name", name);

        int newId = (int)await cmd.ExecuteScalarAsync();
        return newId;
    }
    //удаление узла проекта
    public async Task DeleteNodeAsync(int nodeId)
    {
        using var conn = new SqlConnection(connectionString);

        await conn.OpenAsync();

        var cmd = new SqlCommand(@"

    -- собираем все ноды
    WITH ToDelete AS
    (
        SELECT Id
        FROM ProjectNodes
        WHERE Id=@id

        UNION ALL

        SELECT n.Id
        FROM ProjectNodes n
        JOIN ToDelete td
            ON n.ParentId = td.Id
    )

    -- сначала удаляем файлы
    DELETE FROM NodeFiles
    WHERE NodeId IN (SELECT Id FROM ToDelete);

    -- потом ноды
    WITH ToDelete2 AS
    (
        SELECT Id
        FROM ProjectNodes
        WHERE Id=@id

        UNION ALL

        SELECT n.Id
        FROM ProjectNodes n
        JOIN ToDelete2 td
            ON n.ParentId = td.Id
    )

    DELETE FROM ProjectNodes
    WHERE Id IN (SELECT Id FROM ToDelete2);

    ", conn);

        cmd.Parameters.AddWithValue("@id", nodeId);

        await cmd.ExecuteNonQueryAsync();
    }
    public async Task UpdateNodeParentAsync(int nodeId, int? parentId)
    {
        using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();

        var cmd = new SqlCommand(@"
    UPDATE ProjectNodes
    SET ParentId=@p
    WHERE Id=@id", conn);

        cmd.Parameters.AddWithValue(
            "@p",
            (object?)parentId ?? DBNull.Value
        );

        cmd.Parameters.AddWithValue("@id", nodeId);

        await cmd.ExecuteNonQueryAsync();
    }
    public async Task UpdateNodeAsync(ProjectNode node)
    {
        using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();

        var cmd = new SqlCommand(@"
        UPDATE ProjectNodes
        SET Name=@n,
            Description=@d,
            Quantity=@q
        WHERE Id=@id", conn);

        cmd.Parameters.AddWithValue("@n", node.Name);
        cmd.Parameters.AddWithValue("@d", node.Description ?? "");
        cmd.Parameters.AddWithValue("@q", node.Quantity);
        cmd.Parameters.AddWithValue("@id", node.Id);

        await cmd.ExecuteNonQueryAsync();
    }
    public async Task<List<NodeFile>> GetNodeFilesAsync(int nodeId)
    {
        var list = new List<NodeFile>();

        using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();

        var cmd = new SqlCommand(
            "SELECT Id, NodeId, FileName, FilePath FROM NodeFiles WHERE NodeId=@id", conn);

        cmd.Parameters.AddWithValue("@id", nodeId);

        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            list.Add(new NodeFile
            {
                Id = reader.GetInt32(0),
                NodeId = reader.GetInt32(1),
                FileName = reader.GetString(2),
                FilePath = reader.GetString(3)
            });
        }

        return list;
    }
    public async Task AddNodeFileAsync(NodeFile file)
    {
        using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();

        var cmd = new SqlCommand(@"
        INSERT INTO NodeFiles (NodeId, FileName, FilePath)
        VALUES (@n, @name, @path)", conn);

        cmd.Parameters.AddWithValue("@n", file.NodeId);
        cmd.Parameters.AddWithValue("@name", file.FileName);
        cmd.Parameters.AddWithValue("@path", file.FilePath);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteNodeFileAsync(int id)
    {
        using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();

        var cmd = new SqlCommand("DELETE FROM NodeFiles WHERE Id=@id", conn);
        cmd.Parameters.AddWithValue("@id", id);

        await cmd.ExecuteNonQueryAsync();
    }
    public async Task SetParentAsync(int nodeId, int? parentId)
    {
        using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();

        var cmd = new SqlCommand(@"
    UPDATE ProjectNodes
    SET ParentId=@p
    WHERE Id=@id", conn);

        cmd.Parameters.AddWithValue("@id", nodeId);
        cmd.Parameters.AddWithValue("@p", (object?)parentId ?? DBNull.Value);

        await cmd.ExecuteNonQueryAsync();
    }
    public async Task<List<string>> GetNodeSchemesAsync(int nodeId)
    {
        var list = new List<string>();

        using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();

        var cmd = new SqlCommand(
            "SELECT FilePath FROM NodeSchemes WHERE NodeId=@id",
            conn);

        cmd.Parameters.AddWithValue("@id", nodeId);

        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            list.Add(reader.GetString(0));
        }

        return list;
    }
    public async Task AddNodeSchemeAsync(int nodeId, string path)
    {
        using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();

        var cmd = new SqlCommand(
            "INSERT INTO NodeSchemes (NodeId, FilePath) VALUES (@n,@p)",
            conn);

        cmd.Parameters.AddWithValue("@n", nodeId);
        cmd.Parameters.AddWithValue("@p", path);

        await cmd.ExecuteNonQueryAsync();
    }
    public async Task DeleteNodeSchemeAsync(int nodeId, string path)
    {
        using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();

        var cmd = new SqlCommand(
            "DELETE FROM NodeSchemes WHERE NodeId=@n AND FilePath=@p",
            conn);

        cmd.Parameters.AddWithValue("@n", nodeId);
        cmd.Parameters.AddWithValue("@p", path);

        await cmd.ExecuteNonQueryAsync();

        await DeleteSchemeMarkersAsync(path);
    }

    public async Task<List<SchemeMarker>> GetSchemeMarkersAsync(string schemePath)
    {
        var list = new List<SchemeMarker>();

        using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();
        var cmd = new SqlCommand(@"
        SELECT m.Id, m.SchemePath, m.TargetNodeId, n.Name,
               m.ButtonX, m.ButtonY, m.TargetX, m.TargetY
        FROM SchemeMarkers m
        LEFT JOIN ProjectNodes n ON n.Id = m.TargetNodeId
        WHERE m.SchemePath=@path
        ORDER BY m.Id", conn);

        cmd.Parameters.AddWithValue("@path", schemePath);

        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            list.Add(new SchemeMarker
            {
                Id = reader.GetInt32(0),
                SchemePath = reader.GetString(1),
                TargetNodeId = reader.GetInt32(2),
                TargetNodeName = reader.IsDBNull(3) ? "Удаленный узел" : reader.GetString(3),
                ButtonX = Convert.ToSingle(reader.GetDouble(4)),
                ButtonY = Convert.ToSingle(reader.GetDouble(5)),
                TargetX = Convert.ToSingle(reader.GetDouble(6)),
                TargetY = Convert.ToSingle(reader.GetDouble(7))
            });
        }

        return list;
    }

    public async Task<int> SaveSchemeMarkerAsync(SchemeMarker marker)
    {
        using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();

        if (marker.Id > 0)
        {
            var updateCmd = new SqlCommand(@"
            UPDATE SchemeMarkers
            SET SchemePath=@scheme,
                TargetNodeId=@node,
                ButtonX=@buttonX,
                ButtonY=@buttonY,
                TargetX=@targetX,
                TargetY=@targetY
            WHERE Id=@id", conn);

            FillSchemeMarkerParams(updateCmd, marker);
            updateCmd.Parameters.AddWithValue("@id", marker.Id);

            await updateCmd.ExecuteNonQueryAsync();
            return marker.Id;
        }

        var insertCmd = new SqlCommand(@"
        INSERT INTO SchemeMarkers
            (SchemePath, TargetNodeId, ButtonX, ButtonY, TargetX, TargetY)
        OUTPUT INSERTED.Id
        VALUES
            (@scheme, @node, @buttonX, @buttonY, @targetX, @targetY)", conn);

        FillSchemeMarkerParams(insertCmd, marker);

        marker.Id = (int)await insertCmd.ExecuteScalarAsync();
        return marker.Id;
    }

    void FillSchemeMarkerParams(SqlCommand cmd, SchemeMarker marker)
    {
        cmd.Parameters.AddWithValue("@scheme", marker.SchemePath);
        cmd.Parameters.AddWithValue("@node", marker.TargetNodeId);
        cmd.Parameters.AddWithValue("@buttonX", marker.ButtonX);
        cmd.Parameters.AddWithValue("@buttonY", marker.ButtonY);
        cmd.Parameters.AddWithValue("@targetX", marker.TargetX);
        cmd.Parameters.AddWithValue("@targetY", marker.TargetY);
    }

    public async Task DeleteSchemeMarkersAsync(string schemePath)
    {
        using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();

        var cmd = new SqlCommand(
            "DELETE FROM SchemeMarkers WHERE SchemePath=@path",
            conn);

        cmd.Parameters.AddWithValue("@path", schemePath);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteSchemeMarkerAsync(int markerId)
    {
        using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();

        var cmd = new SqlCommand(
            "DELETE FROM SchemeMarkers WHERE Id=@id",
            conn);

        cmd.Parameters.AddWithValue("@id", markerId);

        await cmd.ExecuteNonQueryAsync();
    }
}
