using AForge.Video.DirectShow;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

using Path = System.IO.Path;

namespace WinFormsApp1
{
    public partial class Login : Form
    {
        // =========================================================
        // BUILT-IN ADMIN FALLBACK
        // =========================================================
        private const string BUILTIN_ADMIN_USER = "superadmin";
        private const string BUILTIN_ADMIN_PASS = "superadmin123";

        private string StudentSection;
        private int UserId;
        private string question;
        private string answer;

        private string selectedRootFolder = "";

        private Panel pnlPresetContainer;
        private ComboBox cmbPreset;
        private Button btnApplyPreset;
        private Button btnSavePreset;
        private Button btnDeletePreset;

        // =========================================================
        // SINGLE ACCOUNT / SINGLE SESSION
        // =========================================================
        private string activeSessionToken = "";
        private int activeSessionUserId = 0;
        private System.Windows.Forms.Timer sessionHeartbeatTimer;
        private bool sessionReleased = false;

        private const int SESSION_TIMEOUT_SECONDS = 90;

        public Login()
        {
            InitializeComponent();

            this.Shown += Login_Shown;

            sessionHeartbeatTimer = new System.Windows.Forms.Timer();
            sessionHeartbeatTimer.Interval = 15000;
            sessionHeartbeatTimer.Tick += SessionHeartbeatTimer_Tick;

            this.FormClosing += Login_FormClosing;
        }

        private void Login_Shown(object sender, EventArgs e)
        {
            txtUsername.Clear();
            txtPassword.Clear();
            txtUsername.Focus();
        }

        private void Login_Load(object sender, EventArgs e)
        {
            BuildPresetUi();
            RefreshPresetDropdown();

            LoadCurrentSettings();
            IsAnyCameraDetected();
            BuildWindowButtons();
        }

        // =========================================================
        // PRESET UI
        // =========================================================
        private void BuildWindowButtons()
        {
            if (this.Controls.Find("btnExit", true).Length > 0) return;

            int btnW = 46;
            int btnH = 32;
            int top = 0;
            int right = 0;

            Button btnExit = new Button
            {
                Name = "btnExit",
                Text = "✕",
                Size = new Size(btnW, btnH),
                Location = new Point(this.ClientSize.Width - btnW - right, top),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(180, 30, 30),
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                TabStop = false
            };
            btnExit.FlatAppearance.BorderSize = 0;
            btnExit.FlatAppearance.MouseOverBackColor = Color.FromArgb(232, 17, 35);
            btnExit.FlatAppearance.MouseDownBackColor = Color.FromArgb(140, 10, 20);
            btnExit.Click += (s, e) =>
            {
                var confirm = MessageBox.Show(
                    "Are you sure you want to exit?",
                    "Exit Application",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (confirm == DialogResult.Yes)
                {
                    ReleaseCurrentSession();
                    Application.Exit();
                }
            };
            this.Controls.Add(btnExit);
            btnExit.BringToFront();

            Button btnMinimize = new Button
            {
                Name = "btnMinimize",
                Text = "—",
                Size = new Size(btnW, btnH),
                Location = new Point(this.ClientSize.Width - (btnW * 2) - right, top),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                TabStop = false
            };
            btnMinimize.FlatAppearance.BorderSize = 0;
            btnMinimize.FlatAppearance.MouseOverBackColor = Color.FromArgb(90, 90, 90);
            btnMinimize.FlatAppearance.MouseDownBackColor = Color.FromArgb(40, 40, 40);
            btnMinimize.Click += (s, e) =>
            {
                this.WindowState = FormWindowState.Minimized;
            };
            this.Controls.Add(btnMinimize);
            btnMinimize.BringToFront();
        }

        private void BuildPresetUi()
        {
            if (pnlConfiguration == null) return;

            var existing = pnlConfiguration.Controls.Find("pnlPresetContainer", true);
            foreach (var c in existing)
                pnlConfiguration.Controls.Remove(c);

            pnlPresetContainer = new Panel
            {
                Name = "pnlPresetContainer",
                Left = 12,
                Top = 42,
                Width = 700,
                Height = 58,
                BackColor = Color.Transparent
            };

            var lblPreset = new Label
            {
                Text = "Preset Server Profile:",
                Left = 0,
                Top = 0,
                Width = 200,
                Font = new Font("Segoe UI Semibold", 9.5F)
            };

            cmbPreset = new ComboBox
            {
                Left = 0,
                Top = 22,
                Width = 250,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5F)
            };

            btnApplyPreset = new Button
            {
                Text = "Apply",
                Left = 258,
                Top = 21,
                Width = 80,
                Height = 25
            };
            btnApplyPreset.Click += BtnApplyPreset_Click;

            btnSavePreset = new Button
            {
                Text = "Save Current as New",
                Left = 344,
                Top = 21,
                Width = 160,
                Height = 25
            };
            btnSavePreset.Click += BtnSavePreset_Click;

            btnDeletePreset = new Button
            {
                Text = "Delete",
                Left = 510,
                Top = 21,
                Width = 80,
                Height = 25
            };
            btnDeletePreset.Click += BtnDeletePreset_Click;

            pnlPresetContainer.Controls.Add(lblPreset);
            pnlPresetContainer.Controls.Add(cmbPreset);
            pnlPresetContainer.Controls.Add(btnApplyPreset);
            pnlPresetContainer.Controls.Add(btnSavePreset);
            pnlPresetContainer.Controls.Add(btnDeletePreset);

            pnlConfiguration.Controls.Add(pnlPresetContainer);
            pnlPresetContainer.BringToFront();
        }

        private void RefreshPresetDropdown()
        {
            if (cmbPreset == null) return;

            cmbPreset.Items.Clear();

            if (SettingsManager.Current.ServerPresets == null ||
                SettingsManager.Current.ServerPresets.Count == 0)
            {
                cmbPreset.Items.Add("(no presets yet)");
                cmbPreset.Enabled = false;
                btnApplyPreset.Enabled = false;
                btnDeletePreset.Enabled = false;
                cmbPreset.SelectedIndex = 0;
                return;
            }

            cmbPreset.Enabled = true;
            btnApplyPreset.Enabled = true;
            btnDeletePreset.Enabled = true;

            foreach (var preset in SettingsManager.Current.ServerPresets)
                cmbPreset.Items.Add(preset.Name);

            cmbPreset.SelectedIndex = 0;
        }

        private void BtnApplyPreset_Click(object sender, EventArgs e)
        {
            if (cmbPreset.SelectedIndex < 0) return;

            int index = cmbPreset.SelectedIndex;
            if (index < 0 || index >= SettingsManager.Current.ServerPresets.Count) return;

            var preset = SettingsManager.Current.ServerPresets[index];

            txtServerIP.Text = preset.ServerIp;
            txtWorkStationPort.Text = preset.WorkstationPort.ToString();
            txtScreenSharingPort.Text = preset.ScreenSharePort.ToString();
            txtBroadcastPort.Text = preset.BroadcastPort.ToString();
            txtFileTransferPort.Text = preset.FileTransferPort.ToString();
            txtCommandPort.Text = preset.CommandPort.ToString();

            txtDatabaseHost.Text = preset.DatabaseHost;
            txtDatabasePort.Text = preset.DatabasePort.ToString();
            txtDatabaseName.Text = preset.DatabaseName;
            txtDatabaseUser.Text = preset.DatabaseUser;
            txtDatabasePassword.Text = preset.DatabasePassword;

            SettingsManager.Current.ServerIp = preset.ServerIp;
            SettingsManager.Current.WorkstationPort = preset.WorkstationPort;
            SettingsManager.Current.ScreenSharePort = preset.ScreenSharePort;
            SettingsManager.Current.BroadcastPort = preset.BroadcastPort;
            SettingsManager.Current.FileTransferPort = preset.FileTransferPort;
            SettingsManager.Current.CommandPort = preset.CommandPort;

            SettingsManager.Current.DatabaseHost = preset.DatabaseHost;
            SettingsManager.Current.DatabasePort = preset.DatabasePort;
            SettingsManager.Current.DatabaseName = preset.DatabaseName;
            SettingsManager.Current.DatabaseUser = preset.DatabaseUser;
            SettingsManager.Current.DatabasePassword = preset.DatabasePassword;

            SettingsManager.Save();

            MessageBox.Show(
                $"Applied preset \"{preset.Name}\".\n\n" +
                $"Server IP: {preset.ServerIp}\n" +
                $"Database: {preset.DatabaseHost}:{preset.DatabasePort}/{preset.DatabaseName}",
                "Preset Applied",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void BtnSavePreset_Click(object sender, EventArgs e)
        {
            string name = Microsoft.VisualBasic.Interaction.InputBox(
                "Enter a name for this preset (e.g. your name):",
                "Save Preset",
                "");

            if (string.IsNullOrWhiteSpace(name)) return;
            name = name.Trim();

            if (name.Equals("(no presets yet)", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("That name is reserved. Pick another.");
                return;
            }

            if (!int.TryParse(txtWorkStationPort.Text.Trim(), out int wsPort) ||
                !int.TryParse(txtScreenSharingPort.Text.Trim(), out int ssPort) ||
                !int.TryParse(txtBroadcastPort.Text.Trim(), out int bcPort) ||
                !int.TryParse(txtFileTransferPort.Text.Trim(), out int ftPort) ||
                !int.TryParse(txtCommandPort.Text.Trim(), out int cmdPort))
            {
                MessageBox.Show("Please make sure all network port fields contain valid numbers.");
                return;
            }

            if (!int.TryParse(txtDatabasePort.Text.Trim(), out int dbPort))
            {
                MessageBox.Show("Please enter a valid database port.");
                return;
            }

            var newPreset = new ServerPreset
            {
                Name = name,

                ServerIp = txtServerIP.Text.Trim(),
                WorkstationPort = wsPort,
                ScreenSharePort = ssPort,
                BroadcastPort = bcPort,
                FileTransferPort = ftPort,
                CommandPort = cmdPort,

                DatabaseHost = txtDatabaseHost.Text.Trim(),
                DatabasePort = dbPort,
                DatabaseName = txtDatabaseName.Text.Trim(),
                DatabaseUser = txtDatabaseUser.Text.Trim(),
                DatabasePassword = txtDatabasePassword.Text
            };

            if (SettingsManager.Current.ServerPresets == null)
                SettingsManager.Current.ServerPresets = new List<ServerPreset>();

            int existing = SettingsManager.Current.ServerPresets.FindIndex(
                p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));

            if (existing >= 0)
            {
                var confirm = MessageBox.Show(
                    $"A preset named \"{name}\" already exists. Overwrite it?",
                    "Overwrite Preset",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (confirm != DialogResult.Yes) return;

                SettingsManager.Current.ServerPresets[existing] = newPreset;
            }
            else
            {
                SettingsManager.Current.ServerPresets.Add(newPreset);
            }

            SettingsManager.Save();
            RefreshPresetDropdown();

            int newIdx = SettingsManager.Current.ServerPresets.FindIndex(p => p.Name == name);
            if (newIdx >= 0) cmbPreset.SelectedIndex = newIdx;

            MessageBox.Show($"Preset \"{name}\" saved.");
        }

        private void BtnDeletePreset_Click(object sender, EventArgs e)
        {
            if (cmbPreset.SelectedIndex < 0) return;

            int index = cmbPreset.SelectedIndex;
            if (index < 0 || index >= SettingsManager.Current.ServerPresets.Count) return;

            var preset = SettingsManager.Current.ServerPresets[index];

            var confirm = MessageBox.Show(
                $"Delete preset \"{preset.Name}\"?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes) return;

            SettingsManager.Current.ServerPresets.RemoveAt(index);
            SettingsManager.Save();
            RefreshPresetDropdown();
        }

        // =========================================================
        // LOAD CURRENT SETTINGS
        // =========================================================
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

            txtSmtpUser.Text = SettingsManager.Current.SmtpUser;
            txtSmtpPass.Text = SettingsManager.Current.SmtpPass;
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
                {
                    SettingsManager.Current.SaveFolder = selectedRootFolder;
                    SaveRootFolderToDatabase(selectedRootFolder);
                }

                SettingsManager.Current.SmtpUser = txtSmtpUser.Text.Trim();
                SettingsManager.Current.SmtpPass = txtSmtpPass.Text;

                SettingsManager.Save();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Invalid settings: " + ex.Message);
                return;
            }

            MessageBox.Show("Settings saved successfully!\n\nRoot folder: " + SettingsManager.Current.SaveFolder);
        }

        private void SaveRootFolderToDatabase(string path)
        {
            string connStr = SettingsManager.Current.GetConnectionString();

            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    string createTable = @"CREATE TABLE IF NOT EXISTS mainfolderpath (
                                        user_id INT PRIMARY KEY,
                                        FolderPath VARCHAR(255)
                                   )";
                    using (var createCmd = new MySqlCommand(createTable, conn))
                        createCmd.ExecuteNonQuery();

                    string query = @"INSERT INTO mainfolderpath (user_id, FolderPath) 
                              VALUES (0, @path)
                              ON DUPLICATE KEY UPDATE FolderPath = @path";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@path", path);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Warning: Root folder saved locally, but failed to sync to shared database:\n" + ex.Message);
            }
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
                                ReleaseCurrentSession();

                                MessageBox.Show("User not found.", "Login Error",
                                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                return;
                            }

                            if (reader.IsDBNull(reader.GetOrdinal("authentication_photo")))
                            {
                                ReleaseCurrentSession();

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
                                ReleaseCurrentSession();

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

                            using (LivenessCheckForm livenessForm = new LivenessCheckForm(
                                studentreferencesPhoto, UserId, StudentSection, username))
                            {
                                livenessForm.ShowDialog(this);
                            }

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
                                this.Hide();
                            }
                            else
                            {
                                ReleaseCurrentSession();
                                this.Show();
                                this.BringToFront();
                                this.Activate();
                                txtUsername.Clear();
                                txtPassword.Clear();
                                txtUsername.Focus();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ReleaseCurrentSession();
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
        // SINGLE SESSION FUNCTIONS
        // =========================================================
        private bool TryAcquireSession(int userId, string username)
        {
            string connStr = SettingsManager.Current.GetConnectionString();

            string newToken = Guid.NewGuid().ToString("N");

            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    string query = @"
                        UPDATE user_credential
                        SET
                            is_logged_in = 1,
                            session_token = @newToken,
                            last_seen = NOW()
                        WHERE
                            user_id = @userId
                            AND
                            (
                                is_logged_in = 0
                                OR last_seen IS NULL
                                OR last_seen < DATE_SUB(NOW(), INTERVAL @timeout SECOND)
                            )";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@newToken", newToken);
                        cmd.Parameters.AddWithValue("@userId", userId);
                        cmd.Parameters.AddWithValue("@timeout", SESSION_TIMEOUT_SECONDS);

                        int affected = cmd.ExecuteNonQuery();

                        if (affected != 1)
                        {
                            MessageBox.Show(
                                "This account is already logged in on another computer.\n\n" +
                                "You cannot open the same account at the same time.\n\n" +
                                "If the previous computer was unexpectedly closed, " +
                                "please wait up to 90 seconds before trying again.",
                                "Account Already In Use",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);

                            return false;
                        }
                    }
                }

                activeSessionUserId = userId;
                activeSessionToken = newToken;
                sessionReleased = false;

                sessionHeartbeatTimer.Start();

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Unable to start the account session.\n\n" + ex.Message,
                    "Session Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return false;
            }
        }

        private void SessionHeartbeatTimer_Tick(object sender, EventArgs e)
        {
            if (activeSessionUserId <= 0 || string.IsNullOrEmpty(activeSessionToken))
                return;

            try
            {
                string connStr = SettingsManager.Current.GetConnectionString();

                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    string query = @"
                        UPDATE user_credential
                        SET last_seen = NOW()
                        WHERE user_id = @userId
                          AND session_token = @token
                          AND is_logged_in = 1";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@userId", activeSessionUserId);
                        cmd.Parameters.AddWithValue("@token", activeSessionToken);

                        int affected = cmd.ExecuteNonQuery();

                        if (affected != 1)
                        {
                            sessionHeartbeatTimer.Stop();

                            MessageBox.Show(
                                "Your account session is no longer active.\n\n" +
                                "This application will close.",
                                "Session Ended",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);

                            sessionReleased = true;
                            activeSessionUserId = 0;
                            activeSessionToken = "";

                            Application.Exit();
                        }
                    }
                }
            }
            catch
            {
                // Ignore a single network blip; next tick will retry.
            }
        }

        public void ReleaseCurrentSession()
        {
            if (sessionReleased)
                return;

            if (activeSessionUserId <= 0 || string.IsNullOrEmpty(activeSessionToken))
                return;

            try
            {
                sessionHeartbeatTimer?.Stop();

                string connStr = SettingsManager.Current.GetConnectionString();

                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    string query = @"
                        UPDATE user_credential
                        SET
                            is_logged_in = 0,
                            session_token = NULL,
                            last_seen = NULL
                        WHERE user_id = @userId
                          AND session_token = @token";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@userId", activeSessionUserId);
                        cmd.Parameters.AddWithValue("@token", activeSessionToken);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch
            {
                // Stale-session timeout will eventually unlock the account.
            }
            finally
            {
                sessionReleased = true;
                activeSessionUserId = 0;
                activeSessionToken = "";
            }
        }

        private void Login_FormClosing(object sender, FormClosingEventArgs e)
        {
            ReleaseCurrentSession();
        }

        // =========================================================
        // LOGIN
        // =========================================================
        private void btnLogin_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text;

            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show(
                    "Please enter your username and password.",
                    "Login",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            // =========================================================
            // BUILT-IN ADMIN FALLBACK (no database check)
            // =========================================================
            if (string.Equals(username, BUILTIN_ADMIN_USER, StringComparison.OrdinalIgnoreCase) &&
                password == BUILTIN_ADMIN_PASS)
            {
                this.Hide();

                using (AdminForm adminForm = new AdminForm())
                {
                    adminForm.ShowDialog(this);
                }

                this.Show();
                this.BringToFront();
                this.Activate();
                txtUsername.Clear();
                txtPassword.Clear();
                txtUsername.Focus();
                return;
            }

            InitializeGetQandA(username);

            string connStr = SettingsManager.Current.GetConnectionString();

            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    string query = @"
                        SELECT
                            user_id,
                            username,
                            p_word,
                            roles,
                            authentication_photo
                        FROM user_credential
                        WHERE username = @username
                        LIMIT 1";

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

                                string authentication_photo =
                                    reader.IsDBNull(reader.GetOrdinal("authentication_photo"))
                                    ? ""
                                    : reader.GetString("authentication_photo");

                                // =========================================================
                                // MD5-ONLY COMPARISON
                                // =========================================================
                                string hashedInput = Md5(password);

                                bool passwordOk =
                                    string.Equals(storedPassword, hashedInput, StringComparison.OrdinalIgnoreCase);

                                if (passwordOk)
                                {
                                    if (!TryAcquireSession(UserId, username))
                                        return;

                                    SelectSection(UserId);

                                    OpenAppropriateForm(
                                        role,
                                        username,
                                        UserId,
                                        authentication_photo,
                                        question,
                                        answer);
                                }
                                else
                                {
                                    MessageBox.Show(
                                        "Incorrect password.",
                                        "Login Error",
                                        MessageBoxButtons.OK,
                                        MessageBoxIcon.Warning);
                                }
                            }
                            else
                            {
                                MessageBox.Show(
                                    "Username not found.",
                                    "Login Error",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Warning);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ReleaseCurrentSession();

                MessageBox.Show(
                    "An error occurred: " + ex.Message,
                    "Login Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        // MD5 helper — no extra file needed
        private static string Md5(string input)
        {
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(input);
                byte[] hash = md5.ComputeHash(bytes);

                var sb = new System.Text.StringBuilder();
                foreach (byte b in hash)
                    sb.Append(b.ToString("x2"));

                return sb.ToString();
            }
        }

        private void OpenAppropriateForm(
    string role,
    string username,
    int UserId,
    string authenticationPhoto,
    string question,
    string answer)
        {
            // Hide the login form entirely before opening any role form
            this.Hide();

            if (role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                using (AdminForm adminForm = new AdminForm())
                {
                    adminForm.ShowDialog();
                }
            }
            else if (role.Equals("Professor", StringComparison.OrdinalIgnoreCase))
            {
                using (ProfessorForm profForm = new ProfessorForm(UserId, username))
                {
                    profForm.ShowDialog();
                }
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
                    using (StudentForm studentform = new StudentForm(UserId, StudentSection, username))
                    {
                        studentform.ShowDialog();
                    }
                }
                else if (!string.IsNullOrEmpty(question) && !string.IsNullOrEmpty(answer))
                {
                    using (QandAForm QandAform = new QandAForm(UserId, StudentSection, username))
                    {
                        QandAform.ShowDialog();
                    }
                }
                else
                {
                    ReleaseCurrentSession();

                    MessageBox.Show(
                        "Your account security-question information is incomplete.",
                        "Login Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    this.Show();
                    this.BringToFront();
                    this.Activate();
                    return;
                }
            }
            else
            {
                ReleaseCurrentSession();

                MessageBox.Show(
                    "Unknown role: " + role,
                    "Login Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                this.Show();
                this.BringToFront();
                this.Activate();
                return;
            }

            // After the child form closes → release session → show Login again
            ReleaseCurrentSession();
            this.Show();
            this.BringToFront();
            this.Activate();
            txtUsername.Clear();
            txtPassword.Clear();
            txtUsername.Focus();
        }

        private void guna2PictureBox3_Click(object sender, EventArgs e)
        {

        }

        private void label19_Click(object sender, EventArgs e)
        {

        }
    }
}