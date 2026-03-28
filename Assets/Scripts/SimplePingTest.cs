using UnityEngine;
using System.Data.SqlClient;
using System;
using System.Data.Odbc;

public class SimplePingTest : MonoBehaviour
{
    void Start()
    {
        TestLogin();
    }

    void TestLogin()
    {
        string connStr = "Driver={SQL Server};Server=DESKTOP-CNBOKTJ\\SQLEXPRESS;Database=factory;Trusted_Connection=yes;";

        string username = "admin";
        string password = "admin123";

        try
        {
            using (OdbcConnection conn = new OdbcConnection(connStr))
            {
                conn.Open();

                // Прямой запрос без параметров для проверки
                string query = $"SELECT COUNT(*) FROM Users WHERE Username = '{username}' AND PasswordHash = HASHBYTES('SHA2_256', '{password}')";
                using (OdbcCommand cmd = new OdbcCommand(query, conn))
                {
                    int count = (int)cmd.ExecuteScalar();
                    Debug.Log($"Результат: {count} (1 - должно работать, 0 - нет)");
                }

                conn.Close();
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Ошибка: {ex.Message}");
        }
    }
}