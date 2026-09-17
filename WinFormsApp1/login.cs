using AForge.Video.DirectShow;
using DocumentFormat.OpenXml.Office.SpreadSheetML.Y2023.MsForms;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace WinFormsApp1
{
    public partial class Login : Form
    {
        private string StudentSection;
        private string DatabaseIP = "localhost";
        private int UserId;
        private string question;
        private string answer;
        // Temporary hardcoded accounts (for testing lang, wala pang database)
        private readonly Dictionary<string, string> tempAccounts = new Dictionary<string, string>
        {
            { "admin", "admin123" },
            { "student01", "pass123" },{ "student02", "pass123" },
            { "prof01", "prof123" }
        };

        public Login()
        {
            InitializeComponent();
            IsAnyCameraDetected();
        }
        private bool IsAnyCameraDetected()
        {
            try
            {
                var devices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                return devices != null && devices.Count > 0;
            }
            catch
            {
                return false;
            }
        }

        private void SelectSection(int userid)
        {
            string connStr = $"Server={DatabaseIP};Port=3306;Database=cdsga_hub;Uid=root;Pwd=;";
            try
            {
                
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = "SELECT school_section FROM user_information WHERE user_id = @user_id";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@user_id", userid);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read()) // <-- this was missing
                            {
                                StudentSection = reader.GetString("school_section");
                            }
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void FaceAuthentication(string username)
        {
            string saveAuthenticationPhoto = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory,
        "StudentAuthenticationPhoto");

            string connStr = $"Server={DatabaseIP};Port=3306;Database=cdsga_hub;Uid=root;Pwd=;";

            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    string query = "SELECT authentication_photo FROM user_credential WHERE username = @username";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@username", username);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                MessageBox.Show("User not found.", "Login Error",
                                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                return;
                            }

                            if (reader.IsDBNull(reader.GetOrdinal("authentication_photo")))
                            {
                                MessageBox.Show("No face photo stored for this user.", "Login Error",
                                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                return;
                            }

                            string faceFileName = reader.GetString("authentication_photo");

                            // ⭐ Use the local variable
                            string faceFullPath = Path.Combine(saveAuthenticationPhoto, faceFileName);

                            if (!File.Exists(faceFullPath))
                            {
                                MessageBox.Show("Reference photo not found: " + faceFullPath,
                                    "Login Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                return;
                            }

                            Bitmap studentreferencesPhoto;
                            using (var fs = new FileStream(faceFullPath, FileMode.Open, FileAccess.Read))
                            {
                                studentreferencesPhoto = new Bitmap(fs);
                            }

                            LivenessCheckForm livenessForm = new LivenessCheckForm(studentreferencesPhoto, UserId, StudentSection, username);
                            livenessForm.Show();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Face authentication error: " + ex.Message);
            }
        }

        private void InitializeGetQandA(string username)
        {
            string connStr = $"Server={DatabaseIP};Port=3306;Database=cdsga_hub;Uid=root;Pwd=;";

            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = "SELECT question, answer FROM question_and_answer WHERE username = @username";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@username", username);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                question = reader.GetString("question");
                                answer = reader.GetString("answer");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred: " + ex.Message);
            }
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text;
            string password = txtPassword.Text;
            InitializeGetQandA(username);
            string connStr = $"Server={DatabaseIP};Port=3306;Database=cdsga_hub;Uid=root;Pwd=;";
            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = "SELECT user_id, username, p_word, roles, authentication_photo FROM user_credential WHERE username = @username";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@username", username);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                UserId = reader.GetInt32("user_id");
                                string storedPassword = reader.GetString("p_word");
                                string role = reader.GetString("roles");
                                string authentication_photo = reader.IsDBNull(reader.GetOrdinal("authentication_photo")) ? "" : reader.GetString("authentication_photo");


                                if (storedPassword == password)
                                {
                                    SelectSection(UserId);
                                    OpenAppropriateForm(role, username, UserId, authentication_photo, question, answer);
                                    this.Hide();
                                }
                                else
                                {
                                    MessageBox.Show("Incorrect password.", "Login Error",
                                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred: " + ex.Message);
            }
        }
        private void OpenAppropriateForm(string role, string username, int UserId, string authenticationPhoto, string question, string answer)
        {
            if (role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                AdminForm adminForm = new AdminForm();
                adminForm.Show();
            }
            else if (role.Equals("Professor", StringComparison.OrdinalIgnoreCase))
            {
                ProfessorForm profForm = new ProfessorForm(UserId, username); // pass ID and username if the form needs them
                profForm.Show();
            }
            else if (role.Equals("Student", StringComparison.OrdinalIgnoreCase))
            {
                bool cameraDetected = IsAnyCameraDetected();

                if (cameraDetected)
                {
                    if (string.IsNullOrEmpty(authenticationPhoto))
                    {
                        if (string.IsNullOrEmpty(question) && string.IsNullOrEmpty(answer))
                        {
                            StudentForm studentform = new StudentForm(UserId, StudentSection, username);
                            studentform.Show();
                        }
                        else if (!string.IsNullOrEmpty(question) && !string.IsNullOrEmpty(answer))
                        {
                            QandAForm QandAform = new QandAForm(UserId, StudentSection, username); // pass ID and username if the form needs them
                            QandAform.Show();
                        }
                        
                    }
                    else if (!string.IsNullOrEmpty(authenticationPhoto))
                    {
                        FaceAuthentication(username);
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(question) && string.IsNullOrEmpty(answer))
                    {
                        StudentForm studentform = new StudentForm(UserId, StudentSection, username);
                        studentform.Show();
                    }
                    else if (!string.IsNullOrEmpty(question) && !string.IsNullOrEmpty(answer))
                    {
                        QandAForm QandAform = new QandAForm(UserId, StudentSection, username); // pass ID and username if the form needs them
                        QandAform.Show();
                    }
                }
                
            }
            else
            {
                MessageBox.Show("Unknown role: " + role);
            }
        }
    }
}