using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Text;

namespace WinFormsApp1
{
    internal class DashboardTotalUsers
    {
        public static int TotalUsers()
        {
            string connStr = "Server=localhost;Port=3306;Database=cdsga_hub;Uid=root;Pwd=;";

            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = "SELECT COUNT(*) FROM users_credential_role";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        int total = Convert.ToInt32(cmd.ExecuteScalar());
                        return total;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return 0;
            }
        }
    }
}
 