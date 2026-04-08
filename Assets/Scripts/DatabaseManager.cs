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

    //CurrentUser - кто сейчас залогинен, и делаем проверку прав
    public User CurrentUser { get; private set; }
    public bool IsAdmin => CurrentUser != null && CurrentUser.Role == "Admin";

    void Awake()
    {
        //В игре только 1 DatabaseManager И он не уничтожается при смене сцен, тобишь трансформ с данным скриптом всегда один, в независимости от переходов между сценами.
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
            // using = авто-закрытие соединения
            using var conn = new SqlConnection(connectionString);
            // открываем асинхронное соединение . async и await не ждут пока операция закончится, следоватеьлно не блочат поток.
            await conn.OpenAsync();

            var checkCmd = new SqlCommand("SELECT COUNT(*) FROM Users WHERE Username=@u OR Email=@e", conn);
            //параметру @u присваиваем значение username
            checkCmd.Parameters.AddWithValue("@u", username);
            checkCmd.Parameters.AddWithValue("@e", email);

            //проверка чтобы не было дублей
            int count = (int)await checkCmd.ExecuteScalarAsync();//ExecuteScalarAsync когда нужен 1 результат
            if (count > 0)
                return (false, "Пользователь уже существует");

            //Добавляем нового юзера в таблицу Users с хешированным паролем
            var cmd = new SqlCommand(@"
                                    INSERT INTO Users (Username, Email, PasswordHash, Role)
                                    VALUES (@u, @e, HASHBYTES('SHA2_256', CAST(@p AS NVARCHAR(4000))), 'User')", conn);

            cmd.Parameters.AddWithValue("@u", username);
            cmd.Parameters.AddWithValue("@e", email);
            cmd.Parameters.AddWithValue("@p", password);

            //асинхронное выполнение запроса. await - обязательно, т.к. код НЕ будет ждать и могут быть баги.
            await cmd.ExecuteNonQueryAsync();//ExecuteNonQueryAsync для манипуляции данными

            //возвращаем sucess - true, сообщение - успех.
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
            // using гарантирует закрытие соединения
            using var conn = new SqlConnection(connectionString);
            // await ждет подключения
            await conn.OpenAsync();

            //ищем пользователя по username и passwoed
            //!!! Делаем проверку в запросе! Username = @u && Pass = hash(...)
            var cmd = new SqlCommand(@"
                                    SELECT Id, Username, Email, Role
                                    FROM Users
                                    WHERE Username=@u AND PasswordHash = HASHBYTES('SHA2_256', CAST(@p AS NVARCHAR(4000)))", conn);

            cmd.Parameters.AddWithValue("@u", username);
            cmd.Parameters.AddWithValue("@p", password);

            //получаем результат запроса если username и password совпали.
            using var reader = await cmd.ExecuteReaderAsync(); //ExecuteReaderAsync для чтения данных

            //проверяем, есть ли хотя бы одна строка?
            //if а не while т.к. ожидается 1 зарегестрированный пользователь.
            if (await reader.ReadAsync())
            {
                //если строка есть - читаем данные и значения колонок используются для создания объекта User.
                CurrentUser = new User
                {
                    Id = reader.GetInt32(0),
                    Username = reader.GetString(1),
                    Email = reader.GetString(2),
                    Role = reader.GetString(3)
                };

                //если есть подписчики на данное событие, вызываем их.
                OnLoginSuccess?.Invoke();

                return (true, CurrentUser, "Добро пожаловать");
            }
            //если строки нет - то что-то пошло не так.
            return (false, null, "Неверный логин или пароль");
        }
        catch (Exception ex)
        {
            Debug.LogError(ex);
            return (false, null, ex.Message);
        }
    }

    // обнуляем узера при выходе
    public void Logout()
    {
        CurrentUser = null;
    }

    //Метод получения проектов
    public async Task<List<Project>> GetProjectsAsync()
    {
        var projects = new List<Project>();

        //защита от вызова без логина
        if (CurrentUser == null)
            return projects;

        //открываем соединение
        using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();

        // для админа получаем все проекты, для юзера только свои проекты
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

        // если не админ сортируем по айдишнику плюсом. присваиваем параметру @id значение curusr.id
        if (!IsAdmin)
            cmd.Parameters.AddWithValue("@id", CurrentUser.Id);

        //считываем итоговые строки и принимаем их в значение reader
        using var reader = await cmd.ExecuteReaderAsync();

        //прокручиваем каждую строку
        while (await reader.ReadAsync())
        {
            // переменным задаем значения каждого столбца строки. 
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
        //возвращаем проекты
        return projects;
    }

    public async Task<(bool success, string message)> UnarchiveProjectAsync(int id)
    {
        try
        {
            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            var cmd = new SqlCommand("UPDATE Projects SET IsArchived=0 WHERE Id=@id", conn);
            cmd.Parameters.AddWithValue("@id", id);

            int rows = await cmd.ExecuteNonQueryAsync();
            return (rows > 0, rows > 0 ? "Восстановлено" : "Не найдено");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    //добавление проекта
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

    public async Task<(bool success, string message)> ArchiveProjectAsync(int id)
    {
        try
        {
            using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            var cmd = new SqlCommand("UPDATE Projects SET IsArchived=1 WHERE Id=@id", conn);
            cmd.Parameters.AddWithValue("@id", id);

            //rows - кол-во измененных строк
            int rows = await cmd.ExecuteNonQueryAsync();
            //была удалена хотябы 1 строка - да/нет
            return (rows > 0, rows > 0 ? "Удалено" : "Не найдено");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}