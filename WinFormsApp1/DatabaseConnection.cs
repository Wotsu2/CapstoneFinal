using MySql.Data.MySqlClient;

namespace WinFormsApp1
{
    public static class DatabaseConnection
    {
        private static readonly string connectionString =
            "Server=localhost;Database=school_portal;Uid=root;Pwd=;";

        public static MySqlConnection GetConnection()
        {
            return new MySqlConnection(connectionString);
        }
    }
}