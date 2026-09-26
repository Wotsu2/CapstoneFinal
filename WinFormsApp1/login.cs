using AForge.Video.DirectShow;
using DocumentFormat.OpenXml.Drawing;
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

using Path = System.IO.Path;

namespace WinFormsApp1
{
    public partial class Login : Form
    {
        private string StudentSection;
        private int UserId;
        private string question;
        private string answer;

        // Root folder picked in Configuration → File Storage
        private string selectedRootFolder = "";

        public Login()
        {
            InitializeComponent();
        }

        private void Login_Load(object sender, EventArgs e)
        {
            LoadCurrentSettings();
            IsAnyCameraDetected();
        }

        private void LoadCurrentSettings()
        {
            txtServerIP.Text = SettingsManager.Current.ServerIp;
            txtWorkStationPort.Text = SettingsManager.Current.WorkstationPort.ToString();
            txtScreenSharingPort.Text = SettingsManager.Current.ScreenSharePort.ToString();
            txtBroadcastPort.Text = SettingsManager.Current.BroadcastPort.ToString();
            txtFileTransferPort.Text = SettingsManager.Current.FileTransferPort.ToString();
            txtCommandPort.Text = SettingsManager.Current.CommandPort.ToString();

            txtDatabaseHost.Text = SettingsManager.Current.DatabaseHost.ToString();
            txtDatabasePort.Text = SettingsManager.Current.DatabasePort.ToString();
            txtDatabaseName.Text = SettingsManager.Current.DatabaseName.ToString();
            txtDatabaseUser.Text = SettingsManager.Current.DatabaseUser.ToString();
            txtDatabasePassword.Text = SettingsManager.Current.DatabasePassword.ToString();

            if (!string.IsNullOrEmpty(SettingsManager.Current.SaveFolder))
                selectedRootFolder = SettingsManager.Current.SaveFolder;
        }

        // =========================================================
        // CONFIGURATION — SAVE
        // =========================================================
        private void btnSaveSetting_Click_1(object sender, EventArgs e)
        {
            try
            {
                SettingsManager.Current.ServerIp = txtServerIP.Text.Trim();
                SettingsManager.Current.WorkstationPort = int.Parse(txtWorkStationPort.Text.Trim());
                SettingsManager.Current.ScreenSharePort = int.Parse(txtScreenSharingPort.Text.Trim());
                SettingsManager.Current.BroadcastPort = int.Parse(txtBroadcastPort.Text.Trim());
                SettingsManager.Current.FileTransferPort = int.Parse(txtFileTransferPort.Text.Trim());
                SettingsManager.Current.CommandPort = int.Parse(txtCommandPort.Text.Trim());

                SettingsManager.Current.DatabaseHost = txtDatabaseHost.Text.Trim();
                SettingsManager.Current.DatabasePort = int.Parse(txtDatabasePort.Text.Trim());
                SettingsManager.Current.DatabaseName = txtDatabaseName.Text.Trim();
                SettingsManager.Current.DatabaseUser = txtDatabaseUser.Text.Trim();
                SettingsManager.Current.DatabasePassword = txtDatabasePassword.Text;

                if (!string.IsNullOrEmpty(selectedRootFolder))
                    SettingsManager.Current.SaveFolder = selectedRootFolder;

                SettingsManager.Save();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Invalid settings: " + ex.Message);
                return;
            }

            MessageBox.Show("Settings saved successfully!\n\nRoot folder: " + SettingsManager.Current.SaveFolder);
        }

        // =========================================================
        // CONFIGURATION — SELECT FOLDER
        // =========================================================
        private void btnSelectFolder_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Choose the ROOT folder where all user folders will be created";
                fbd.ShowNewFolderButton = true;

                if (!string.IsNullOrEmpty(SettingsManager.Current.SaveFolder) &&
                    Directory.Exists(SettingsManager.Current.SaveFolder))
                    fbd.SelectedPath = SettingsManager.Current.SaveFolder;

                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    selectedRootFolder = fbd.SelectedPath;
                    SettingsManager.Current.SaveFolder = selectedRootFolder;

                    MessageBox.Show(
                        "Root folder selected:\n" + selectedRootFolder + "\n\nClick Save to apply.",
                        "Root Folder",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void btnConfigurationSetting_Click_1(object sender, EventArgs e)
        {
            pnlConfiguration.Visible = true;
        }

        private void btnPnlConfigurationClose_Click(object sender, EventArgs e)
        {
            pnlConfiguration.Visible = false;
        }

        // =========================================================
        // CAMERA CHECK
        // =========================================================
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
            string connStr = SettingsManager.Current.GetConnectionString();
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
                            if (reader.Read())
                            {
                                if (!reader.IsDBNull(reader.GetOrdinal("school_section")))
                                    StudentSection = reader.GetString("school_section");
                                else
                                    StudentSection = "";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("SelectSection error: " + ex.Message);
            }
        }

        private void FaceAuthentication(string username)
        {
            string saveAuthenticationPhoto = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "StudentAuthenticationPhoto");

            string connStr = SettingsManager.Current.GetConnectionString();

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

                            string faceFullPath = faceFileName;
                            if (!File.Exists(faceFullPath))
                                faceFullPath = Path.Combine(saveAuthenticationPhoto, Path.GetFileName(faceFileName));

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

                            this.Hide();

                            LivenessCheckForm livenessForm = new LivenessCheckForm(
                                studentreferencesPhoto, UserId, StudentSection, username);

                            livenessForm.FormClosed += (s, args) =>
                            {
                                // Count only OTHER visible forms (skip Login and Liveness)
                                bool anyOtherVisible = false;
                                foreach (Form f in Application.OpenForms)
                                {
                                    if (f == this) continue;
                                    if (f is LivenessCheckForm) continue;
                                    if (f.Visible && !f.IsDisposed)
                                    {
                                        anyOtherVisible = true;
                                        break;
                                    }
                                }

                                if (anyOtherVisible)
                                {
                                    // StudentForm (or other) took over — just hide Login.
                                    // DO NOT close — closing the main form triggers Application.Exit()
                                    // which kills StudentForm too.
                                    this.Hide();
                                }
                                else
                                {
                                    // Nobody opened — bring Login back
                                    this.Show();
                                }
                            };

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
            string connStr = SettingsManager.Current.GetConnectionString();

            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = "SELECT question, answer FROM question_answer_security WHERE username = @username";

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
                Console.WriteLine("InitializeGetQandA error: " + ex.Message);
            }
        }

        // =========================================================
        // LOGIN
        // =========================================================
        private void btnLogin_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text;
            string password = txtPassword.Text;

            InitializeGetQandA(username);

            if (username == "admin123" && password == "123admin")
            {
                AdminForm adminform = new AdminForm();
                adminform.FormClosed += (s, args) => Application.Exit();

                this.Hide();
                adminform.Show();
                return;
            }

            string connStr = SettingsManager.Current.GetConnectionString();
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
                                string authentication_photo = reader.IsDBNull(reader.GetOrdinal("authentication_photo"))
                                    ? ""
                                    : reader.GetString("authentication_photo");

                                if (storedPassword == password)
                                {
                                    SelectSection(UserId);
                                    OpenAppropriateForm(role, username, UserId, authentication_photo, question, answer);
                                }
                                else
                                {
                                    MessageBox.Show("Incorrect password.", "Login Error",
                                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                }
                            }
                            else
                            {
                                MessageBox.Show("Username not found.", "Login Error",
                                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
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

        private void OpenAppropriateForm(string role, string username, int UserId,
                                  string authenticationPhoto, string question, string answer)
        {
            if (role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                AdminForm adminForm = new AdminForm();
                this.Hide();
                adminForm.Show();
            }
            else if (role.Equals("Professor", StringComparison.OrdinalIgnoreCase))
            {
                ProfessorForm profForm = new ProfessorForm(UserId, username);
                this.Hide();
                profForm.Show();
            }
            else if (role.Equals("Student", StringComparison.OrdinalIgnoreCase))
            {
                bool cameraDetected = IsAnyCameraDetected();

                if (cameraDetected && !string.IsNullOrEmpty(authenticationPhoto))
                {
                    FaceAuthentication(username);
                    return;
                }

                if (string.IsNullOrEmpty(question) && string.IsNullOrEmpty(answer))
                {
                    StudentForm studentform = new StudentForm(UserId, StudentSection, username);
                    this.Hide();
                    studentform.Show();
                }
                else if (!string.IsNullOrEmpty(question) && !string.IsNullOrEmpty(answer))
                {
                    QandAForm QandAform = new QandAForm(UserId, StudentSection, username);
                    this.Hide();
                    QandAform.Show();
                }
            }
            else
            {
                MessageBox.Show("Unknown role: " + role);
            }
        }
    }
}