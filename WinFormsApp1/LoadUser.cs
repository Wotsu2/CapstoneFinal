using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace WinFormsApp1
{
    internal class LoadUser
    {
        private admindash parentForm;

        public LoadUser(admindash form)
        {
            parentForm = form;
        }
        public void LoadUserData(string filter = "")
        {
            string connStr = "Server=localhost;Port=3306;Database=cdsga_hub;Uid=root;Pwd=;";
            parentForm.UserDataList.ReadOnly = true;
            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    string query = "SELECT u.user_id, u.user_role, u.status, " +
                       "i.lastname, i.firstname, i.middlename, " +
                       "i.emails, i.school_years, i.sections, i.courses " +
                       "FROM users_credential_role u " +
                       "LEFT JOIN user_informations i ON u.user_id = i.user_id";

                    if (!string.IsNullOrEmpty(filter))
                    {
                        query += " WHERE u.user_id LIKE @f1 OR i.lastname LIKE @f2 OR i.firstname LIKE @f3";
                    }

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        if (!string.IsNullOrEmpty(filter))
                        {
                            string f = "%" + filter + "%";
                            cmd.Parameters.AddWithValue("@f1", f);
                            cmd.Parameters.AddWithValue("@f2", f);
                            cmd.Parameters.AddWithValue("@f3", f);
                        }
                        using (var adapter = new MySqlDataAdapter(cmd))
                        {
                            var dt = new DataTable();
                            adapter.Fill(dt);
                            parentForm.UserDataList.DataSource = dt;

                            parentForm.UserDataList.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                        }
                    }
                    //ActivationStyle();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }
    }
}
