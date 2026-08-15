using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Text;

namespace WinFormsApp1
{
    internal class CreateAccount
    {
        private admindash parentForm; // reference to the Form that owns the UI

        public CreateAccount(admindash form)
        {
            parentForm = form;
        }

        public void CreateUser()
        {
            string connStr = "Server=localhost;Port=3306;Database=cdsga_hub;Uid=root;Pwd=;";

            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    string Insertquery2 = @"INSERT INTO users_credential_role (user_id, pass_word, user_role, status)
                                            VALUES (@User_Id, @Password, @UserRole, @Status)";

                    using (MySqlCommand cmd2 = new MySqlCommand(Insertquery2, conn))
                    {
                        cmd2.Parameters.AddWithValue("@User_Id", parentForm.IdNumberText.Text.Trim());
                        cmd2.Parameters.AddWithValue("@Password", "12345678");
                        cmd2.Parameters.AddWithValue("@UserRole", parentForm.ContextRoleText.Text.Trim());
                        cmd2.Parameters.AddWithValue("@Status", "Activated");
                        cmd2.ExecuteNonQuery();
                    }

                    string Insertquery = @"
                                    INSERT INTO user_informations 
                                        (user_id, lastname, firstname, middlename, emails, school_years, sections, courses) 
                                    VALUES 
                                        (@user_id, @lastname, @firstname, @middlename, @emails, @school_yr, @section, @course)";

                    using (MySqlCommand cmd = new MySqlCommand(Insertquery, conn))
                    {
                        cmd.Parameters.AddWithValue("@user_id", parentForm.IdNumberText.Text.Trim());
                        cmd.Parameters.AddWithValue("@lastname", parentForm.LastnameText.Text.ToUpper());
                        cmd.Parameters.AddWithValue("@firstname", parentForm.FirstnameText.Text.ToUpper());
                        cmd.Parameters.AddWithValue("@middlename", parentForm.MiddlenameText.Text.ToUpper());
                        cmd.Parameters.AddWithValue("@emails", parentForm.EmailText.Text.Trim());
                        cmd.Parameters.AddWithValue("@school_yr", parentForm.ContextYearText.Text.ToUpper());
                        cmd.Parameters.AddWithValue("@section", parentForm.ContextSectionText.Text.ToUpper());
                        cmd.Parameters.AddWithValue("@course", parentForm.ContextCourseText.Text.ToUpper());
                        cmd.ExecuteNonQuery();
                    }

                    MessageBox.Show("Account Successfuly Created!");
                    ClearText();
                    //LoadUser();

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private void ClearText()
        {
            parentForm.IdNumberText.Clear();
            parentForm.FirstnameText.Clear();
            parentForm.LastnameText.Clear();
            parentForm.MiddlenameText.Clear();
            parentForm.EmailText.Clear();
            parentForm.ContextRoleText.SelectedIndex = -1;
            parentForm.ContextYearText.SelectedIndex = -1;
            parentForm.ContextSectionText.SelectedIndex = -1;
            parentForm.ContextRoleText.SelectedIndex = -1;
        }


    }
}
