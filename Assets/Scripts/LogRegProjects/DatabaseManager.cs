using UnityEngine;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
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

    public async Task EnsureCatalogSchemaAsync()
    {
        using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();

        var cmd = new SqlCommand(@"
IF OBJECT_ID('dbo.CatalogRootCategories', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CatalogRootCategories
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(200) NOT NULL,
        SortOrder INT NOT NULL DEFAULT 0
    );
END;

IF OBJECT_ID('dbo.CatalogChildCategories', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CatalogChildCategories
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        RootCategoryId INT NOT NULL,
        Name NVARCHAR(200) NOT NULL,
        SortOrder INT NOT NULL DEFAULT 0,
        CONSTRAINT FK_CatalogChildCategories_Root
            FOREIGN KEY (RootCategoryId) REFERENCES dbo.CatalogRootCategories(Id)
            ON DELETE CASCADE
    );
END;

IF OBJECT_ID('dbo.CatalogProducts', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CatalogProducts
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ChildCategoryId INT NOT NULL,
        Name NVARCHAR(200) NOT NULL DEFAULT '',
        Description NVARCHAR(MAX) NOT NULL DEFAULT '',
        ImagePath NVARCHAR(500) NULL,
        SortOrder INT NOT NULL DEFAULT 0,
        CONSTRAINT FK_CatalogProducts_Child
            FOREIGN KEY (ChildCategoryId) REFERENCES dbo.CatalogChildCategories(Id)
            ON DELETE CASCADE
    );
END;

IF OBJECT_ID('dbo.CatalogProducts', 'U') IS NOT NULL
BEGIN
    IF EXISTS
    (
        SELECT 1
        FROM sys.columns
        WHERE object_id = OBJECT_ID('dbo.CatalogProducts')
          AND name = 'Name'
          AND max_length > 0
          AND max_length < 1000
    )
    BEGIN
        ALTER TABLE dbo.CatalogProducts
        ALTER COLUMN Name NVARCHAR(500) NOT NULL;
    END;

    IF EXISTS
    (
        SELECT 1
        FROM sys.columns
        WHERE object_id = OBJECT_ID('dbo.CatalogProducts')
          AND name = 'Description'
          AND max_length <> -1
    )
    BEGIN
        ALTER TABLE dbo.CatalogProducts
        ALTER COLUMN Description NVARCHAR(MAX) NOT NULL;
    END;

    IF EXISTS
    (
        SELECT 1
        FROM sys.columns
        WHERE object_id = OBJECT_ID('dbo.CatalogProducts')
          AND name = 'ImagePath'
          AND max_length > 0
          AND max_length < 1000
    )
    BEGIN
        ALTER TABLE dbo.CatalogProducts
        ALTER COLUMN ImagePath NVARCHAR(500) NULL;
    END;
END;

IF OBJECT_ID('dbo.CatalogProductFiles', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.CatalogProductFiles
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        ProductId INT NOT NULL,
        FileName NVARCHAR(260) NOT NULL,
        FilePath NVARCHAR(500) NOT NULL,
        CONSTRAINT FK_CatalogProductFiles_Product
            FOREIGN KEY (ProductId) REFERENCES dbo.CatalogProducts(Id)
            ON DELETE CASCADE
    );
END;
", conn);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<CatalogRootCategory>> GetCatalogRootsAsync()
    {
        var list = new List<CatalogRootCategory>();

        using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();

        var cmd = new SqlCommand(@"
SELECT Id, Name
FROM dbo.CatalogRootCategories
ORDER BY SortOrder, Id", conn);

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new CatalogRootCategory
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1)
            });
        }

        return list;
    }

    public async Task<List<CatalogChildCategory>> GetCatalogChildrenAsync(int rootCategoryId)
    {
        var list = new List<CatalogChildCategory>();

        using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();

        var cmd = new SqlCommand(@"
SELECT Id, RootCategoryId, Name
FROM dbo.CatalogChildCategories
WHERE RootCategoryId=@id
ORDER BY SortOrder, Id", conn);

        cmd.Parameters.AddWithValue("@id", rootCategoryId);

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new CatalogChildCategory
            {
                Id = reader.GetInt32(0),
                RootCategoryId = reader.GetInt32(1),
                Name = reader.GetString(2)
            });
        }

        return list;
    }

    public async Task<List<CatalogProduct>> GetCatalogProductsAsync(int childCategoryId)
    {
        var list = new List<CatalogProduct>();

        using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();

        var cmd = new SqlCommand(@"
SELECT Id, ChildCategoryId, Name, Description, ImagePath
FROM dbo.CatalogProducts
WHERE ChildCategoryId=@id
ORDER BY SortOrder, Id", conn);

        cmd.Parameters.AddWithValue("@id", childCategoryId);

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new CatalogProduct
            {
                Id = reader.GetInt32(0),
                ChildCategoryId = reader.GetInt32(1),
                Name = reader.IsDBNull(2) ? "" : reader.GetString(2),
                Description = reader.IsDBNull(3) ? "" : reader.GetString(3),
                ImagePath = reader.IsDBNull(4) ? "" : reader.GetString(4)
            });
        }

        return list;
    }

    public async Task<List<CatalogProductFile>> GetCatalogProductFilesAsync(int productId)
    {
        var list = new List<CatalogProductFile>();

        using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();

        var cmd = new SqlCommand(@"
SELECT Id, ProductId, FileName, FilePath
FROM dbo.CatalogProductFiles
WHERE ProductId=@id
ORDER BY Id", conn);

        cmd.Parameters.AddWithValue("@id", productId);

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new CatalogProductFile
            {
                Id = reader.GetInt32(0),
                ProductId = reader.GetInt32(1),
                FileName = reader.GetString(2),
                FilePath = reader.GetString(3)
            });
        }

        return list;
    }

    public async Task<CatalogRootCategory> GetCatalogRootDetailsAsync(int rootCategoryId)
    {
        CatalogRootCategory root = null;

        using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();

        var rootCmd = new SqlCommand(@"
SELECT Id, Name
FROM dbo.CatalogRootCategories
WHERE Id=@id", conn);
        rootCmd.Parameters.AddWithValue("@id", rootCategoryId);

        using (var rootReader = await rootCmd.ExecuteReaderAsync())
        {
            if (await rootReader.ReadAsync())
            {
                root = new CatalogRootCategory
                {
                    Id = rootReader.GetInt32(0),
                    Name = rootReader.GetString(1)
                };
            }
        }

        if (root == null)
            return null;

        root.Children = await GetCatalogChildrenAsync(rootCategoryId);

        foreach (var child in root.Children)
        {
            child.Products = await GetCatalogProductsAsync(child.Id);

            foreach (var product in child.Products)
                product.Files = await GetCatalogProductFilesAsync(product.Id);
        }

        return root;
    }

    public async Task<int> CreateCatalogRootAsync(string name)
    {
        using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();

        var cmd = new SqlCommand(@"
INSERT INTO dbo.CatalogRootCategories (Name, SortOrder)
OUTPUT INSERTED.Id
VALUES (@name, 0)", conn);

        cmd.Parameters.AddWithValue("@name", name);
        return (int)await cmd.ExecuteScalarAsync();
    }

    public async Task<int> CreateCatalogChildAsync(int rootCategoryId, string name)
    {
        using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();

        var cmd = new SqlCommand(@"
INSERT INTO dbo.CatalogChildCategories (RootCategoryId, Name, SortOrder)
OUTPUT INSERTED.Id
VALUES (@rootId, @name, 0)", conn);

        cmd.Parameters.AddWithValue("@rootId", rootCategoryId);
        cmd.Parameters.AddWithValue("@name", name);
        return (int)await cmd.ExecuteScalarAsync();
    }

    public async Task<int> CreateProductAsync(int childCategoryId)
    {
        await EnsureCatalogSchemaAsync();

        using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();

        var cmd = new SqlCommand(@"
INSERT INTO dbo.CatalogProducts (ChildCategoryId, Name, Description, ImagePath, SortOrder)
OUTPUT INSERTED.Id
VALUES (@childId, '', '', NULL, 0)", conn);

        cmd.Parameters.AddWithValue("@childId", childCategoryId);
        return (int)await cmd.ExecuteScalarAsync();
    }

    public async Task DeleteCatalogRootAsync(int rootCategoryId)
    {
        using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();

        var cmd = new SqlCommand("DELETE FROM dbo.CatalogRootCategories WHERE Id=@id", conn);
        cmd.Parameters.AddWithValue("@id", rootCategoryId);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteCatalogChildAsync(int childCategoryId)
    {
        using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();

        var cmd = new SqlCommand("DELETE FROM dbo.CatalogChildCategories WHERE Id=@id", conn);
        cmd.Parameters.AddWithValue("@id", childCategoryId);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteProductAsync(int productId)
    {
        using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();

        var cmd = new SqlCommand("DELETE FROM dbo.CatalogProducts WHERE Id=@id", conn);
        cmd.Parameters.AddWithValue("@id", productId);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task UpdateProductAsync(CatalogProduct product)
    {
        await EnsureCatalogSchemaAsync();

        using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();

        var cmd = new SqlCommand(@"
UPDATE dbo.CatalogProducts
SET Name=@name,
    Description=@description,
    ImagePath=@imagePath
WHERE Id=@id", conn);

        cmd.Parameters.AddWithValue("@id", product.Id);
        cmd.Parameters.AddWithValue("@name", product.Name ?? "");
        cmd.Parameters.AddWithValue("@description", product.Description ?? "");
        cmd.Parameters.AddWithValue("@imagePath", string.IsNullOrWhiteSpace(product.ImagePath)
            ? DBNull.Value
            : (object)product.ImagePath);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task AddProductFileAsync(int productId, string fileName, string filePath)
    {
        using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();

        var cmd = new SqlCommand(@"
INSERT INTO dbo.CatalogProductFiles (ProductId, FileName, FilePath)
VALUES (@productId, @fileName, @filePath)", conn);

        cmd.Parameters.AddWithValue("@productId", productId);
        cmd.Parameters.AddWithValue("@fileName", fileName);
        cmd.Parameters.AddWithValue("@filePath", filePath);

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteProductFileAsync(int fileId)
    {
        using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();

        var cmd = new SqlCommand("DELETE FROM dbo.CatalogProductFiles WHERE Id=@id", conn);
        cmd.Parameters.AddWithValue("@id", fileId);

        await cmd.ExecuteNonQueryAsync();
    }
}
