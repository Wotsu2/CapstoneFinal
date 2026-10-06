using MySql.Data.MySqlClient;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Windows.Forms;

namespace WinFormsApp1
{
    public partial class CreateAccountForm : Form
    {
        private Guna.UI2.WinForms.Guna2ComboBox cmbRole;
        private Guna.UI2.WinForms.Guna2TextBox txtLastName;
        private Guna.UI2.WinForms.Guna2TextBox txtFirstName;
        private Guna.UI2.WinForms.Guna2TextBox txtMiddleName;
        private Guna.UI2.WinForms.Guna2TextBox txtEmail;

        private Label lblInlineError;

        private string _defaultPassword;

        public CreateAccountForm()
        {
            // Load current default password from app settings
            _defaultPassword = SettingsManager.Current.DefaultPassword;
            if (string.IsNullOrEmpty(_defaultPassword))
                _defaultPassword = "12345678";

            BuildUi();
        }

        // =========================================================
        //  UI
        // =========================================================
        private void BuildUi()
        {
            this.Text = "Create Account";
            this.Size = new Size(700, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.White;
            this.Padding = new Padding(1);
            this.DoubleBuffered = true;

            // Outer rounded container
            var container = new Guna.UI2.WinForms.Guna2Panel
            {
                Dock = DockStyle.Fill,
                BorderRadius = 0,
                FillColor = Color.White,
                BorderColor = Color.FromArgb(220, 220, 220),
                BorderThickness = 1
            };
            this.Controls.Add(container);

            // ============ HEADER ============
            var header = new Guna.UI2.WinForms.Guna2Panel
            {
                Dock = DockStyle.Top,
                Height = 72,
                FillColor = Color.Maroon,
                BorderRadius = 0
            };
            container.Controls.Add(header);

            var lblTitle = new Label
            {
                Text = "👤  Create Account",
                Font = new Font("Segoe UI Semibold", 15F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(24, 22)
            };
            header.Controls.Add(lblTitle);

            // ---------- Close button (standard Button) ----------
            var btnClose = new Button
            {
                Text = "✕",
                Size = new Size(36, 36),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Maroon,
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                TabStop = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(160, 20, 20);
            btnClose.FlatAppearance.MouseDownBackColor = Color.FromArgb(120, 0, 0);
            btnClose.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
            header.Controls.Add(btnClose);

            header.Resize += (s, e) =>
                btnClose.Location = new Point(header.ClientSize.Width - btnClose.Width - 16, 18);
            btnClose.Location = new Point(header.ClientSize.Width - btnClose.Width - 16, 18);

            // ============ BODY ============
            var body = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24, 16, 24, 16),
                BackColor = Color.White,
                AutoScroll = true
            };
            container.Controls.Add(body);
            body.BringToFront();

            int top = 8;
            int rowGap = 78;

            // ---------- Role ----------
            body.Controls.Add(MakeLabel("Role *", 24, top));
            top += 24;

            cmbRole = new Guna.UI2.WinForms.Guna2ComboBox
            {
                Location = new Point(24, top),
                Size = new Size(body.Width - 48, 42),
                BorderRadius = 8,
                FillColor = Color.White,
                BorderColor = Color.FromArgb(213, 218, 223),
                BorderThickness = 1,
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(80, 80, 80),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            cmbRole.Items.AddRange(new object[] { "Student", "Professor", "Admin" });
            cmbRole.SelectedIndex = -1;
            body.Controls.Add(cmbRole);
            top += rowGap;

            // ---------- Name row ----------
            int nameRowTop = top;
            int colGap = 14;
            int totalWidth = body.Width - 48;

            int lastW = (int)(totalWidth * 0.40);
            int firstW = (int)(totalWidth * 0.40);
            int midW = totalWidth - lastW - firstW - colGap * 2;

            body.Controls.Add(MakeLabel("Last Name *", 24, nameRowTop));
            body.Controls.Add(MakeLabel("First Name *", 24 + lastW + colGap, nameRowTop));
            body.Controls.Add(MakeLabel("M.I.", 24 + lastW + colGap + firstW + colGap, nameRowTop));
            nameRowTop += 24;

            txtLastName = MakeTextBox("e.g., Cabañero", 24, nameRowTop, lastW);
            txtFirstName = MakeTextBox("e.g., Vince", 24 + lastW + colGap, nameRowTop, firstW);
            txtMiddleName = MakeTextBox("e.g., A", 24 + lastW + colGap + firstW + colGap, nameRowTop, midW);
            body.Controls.Add(txtLastName);
            body.Controls.Add(txtFirstName);
            body.Controls.Add(txtMiddleName);
            top = nameRowTop + rowGap;

            // ---------- Email ----------
            body.Controls.Add(MakeLabel("Email Address *", 24, top));
            top += 24;

            txtEmail = new Guna.UI2.WinForms.Guna2TextBox
            {
                Location = new Point(24, top),
                Size = new Size(body.Width - 48, 42),
                BorderRadius = 8,
                FillColor = Color.White,
                BorderColor = Color.FromArgb(213, 218, 223),
                BorderThickness = 1,
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(80, 80, 80),
                PlaceholderText = "e.g., vince@email.com",
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            body.Controls.Add(txtEmail);
            top += rowGap;

            // ---------- Notice banner ----------
            var notice = new Guna.UI2.WinForms.Guna2Panel
            {
                Location = new Point(24, top),
                Size = new Size(body.Width - 48, 60),
                BorderRadius = 10,
                FillColor = Color.FromArgb(255, 247, 224),
                BorderColor = Color.FromArgb(240, 220, 150),
                BorderThickness = 1,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            body.Controls.Add(notice);

            var lblNotice = new Label
            {
                Name = "lblNotice",
                Text = $"⚠   *Notice: Default Password is set to \"{_defaultPassword}\" upon account creation.",
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(120, 90, 20),
                BackColor = Color.Transparent,
                AutoSize = false,
                Location = new Point(46, 12),
                Size = new Size(notice.Width - 200, 36),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            notice.Controls.Add(lblNotice);

            var lblCustom = new Label
            {
                Text = "Set custom password?",
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold | FontStyle.Underline),
                ForeColor = Color.Maroon,
                BackColor = Color.Transparent,
                AutoSize = true,
                Cursor = Cursors.Hand
            };
            lblCustom.Location = new Point(notice.Width - lblCustom.PreferredWidth - 14, 20);
            lblCustom.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblCustom.Click += (s, e) => PromptCustomPassword();
            notice.Controls.Add(lblCustom);

            top += notice.Height + 16;

            // ---------- Inline error ----------
            lblInlineError = new Label
            {
                AutoSize = false,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(220, 53, 69),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft,
                Height = 22,
                Visible = false,
                Location = new Point(24, top),
                Size = new Size(body.Width - 48, 22),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            body.Controls.Add(lblInlineError);

            // ---------- Footer (Create button) ----------
            var footer = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 72,
                BackColor = Color.White
            };
            container.Controls.Add(footer);

            var sep = new Panel
            {
                Dock = DockStyle.Top,
                Height = 1,
                BackColor = Color.FromArgb(230, 225, 225)
            };
            footer.Controls.Add(sep);

            var btnCreate = new Guna.UI2.WinForms.Guna2Button
            {
                Text = "＋  Create Account",
                Size = new Size(220, 46),
                BorderRadius = 10,
                FillColor = Color.Maroon,
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Right | AnchorStyles.Top
            };
            btnCreate.HoverState.FillColor = Color.FromArgb(100, 0, 0);
            btnCreate.Click += (s, e) => CreateUser();
            footer.Controls.Add(btnCreate);

            footer.Resize += (s, e) =>
            {
                btnCreate.Location = new Point(footer.ClientSize.Width - btnCreate.Width - 24, 14);
            };
            btnCreate.Location = new Point(footer.ClientSize.Width - btnCreate.Width - 24, 14);
        }

        private Label MakeLabel(string text, int left, int top)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 60, 60),
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(left, top)
            };
        }

        private Guna.UI2.WinForms.Guna2TextBox MakeTextBox(string placeholder, int left, int top, int width)
        {
            var tb = new Guna.UI2.WinForms.Guna2TextBox
            {
                Location = new Point(left, top),
                Size = new Size(width, 42),
                BorderRadius = 8,
                FillColor = Color.White,
                BorderColor = Color.FromArgb(213, 218, 223),
                BorderThickness = 1,
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(80, 80, 80),
                PlaceholderText = placeholder
            };
            tb.TextChanged += (s, e) => HideError();
            return tb;
        }

        // =========================================================
        //  CUSTOM PASSWORD PROMPT
        // =========================================================
        private void PromptCustomPassword()
        {
            using (var frm = new Form())
            {
                frm.Text = "Custom Password";
                frm.Size = new Size(420, 200);
                frm.StartPosition = FormStartPosition.CenterParent;
                frm.FormBorderStyle = FormBorderStyle.FixedDialog;
                frm.MaximizeBox = false;
                frm.MinimizeBox = false;
                frm.BackColor = Color.White;

                var lbl = new Label
                {
                    Text = "Enter a custom password for this account:",
                    Left = 16,
                    Top = 16,
                    Width = 380,
                    Font = new Font("Segoe UI", 9.5F)
                };
                var txt = new TextBox
                {
                    Left = 16,
                    Top = 46,
                    Width = 380,
                    Font = new Font("Consolas", 11F),
                    PasswordChar = '●',
                    Text = _defaultPassword
                };
                var chkShow = new CheckBox
                {
                    Text = "Show password",
                    Left = 16,
                    Top = 78,
                    Width = 200,
                    Font = new Font("Segoe UI", 8.5F),
                    Checked = false
                };
                chkShow.CheckedChanged += (s, e) =>
                {
                    txt.PasswordChar = chkShow.Checked ? '\0' : '●';
                };

                var ok = new Button { Text = "OK", Left = 235, Top = 118, Width = 75, DialogResult = DialogResult.OK };
                var cancel = new Button { Text = "Cancel", Left = 320, Top = 118, Width = 75, DialogResult = DialogResult.Cancel };
                var reset = new Button { Text = "Reset", Left = 20, Top = 118, Width = 75, DialogResult = DialogResult.Abort };

                frm.Controls.Add(lbl);
                frm.Controls.Add(txt);
                frm.Controls.Add(chkShow);
                frm.Controls.Add(ok);
                frm.Controls.Add(cancel);
                frm.Controls.Add(reset);
                frm.AcceptButton = ok;
                frm.CancelButton = cancel;

                var result = frm.ShowDialog(this);

                if (result == DialogResult.Abort)
                {
                    _defaultPassword = "12345678";
                    SettingsManager.Current.DefaultPassword = _defaultPassword;
                    SettingsManager.Save();

                    UpdateNoticeLabel();

                    CustomMessageBox.Show("Password reset to factory default.", "Reset",
                        CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Information);
                    return;
                }

                if (result != DialogResult.OK) return;

                string pwd = txt.Text.Trim();
                if (string.IsNullOrEmpty(pwd))
                {
                    CustomMessageBox.Show("Password cannot be empty.", "Notice",
                        CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Warning);
                    return;
                }
                if (pwd.Length < 8)
                {
                    CustomMessageBox.Show("Password must be at least 8 characters.", "Notice",
                        CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Warning);
                    return;
                }

                _defaultPassword = pwd;

                // Persist to app settings
                SettingsManager.Current.DefaultPassword = pwd;
                SettingsManager.Save();

                UpdateNoticeLabel();

                CustomMessageBox.Show(
                    $"Custom password saved to app settings.\n\nNext accounts will use: {pwd}",
                    "Saved",
                    CustomMessageBoxButtons.OK,
                    CustomMessageBoxIcon.Information);
            }
        }

        private void UpdateNoticeLabel()
        {
            var notice = this.Controls.Find("lblNotice", true).FirstOrDefault() as Label;
            if (notice == null) return;

            if (_defaultPassword == "12345678")
            {
                notice.Text = "⚠   *Notice: Default Password is set to \"12345678\" upon account creation.";
                notice.ForeColor = Color.FromArgb(120, 90, 20);
            }
            else
            {
                notice.Text = $"⚠   *Notice: Custom password \"{_defaultPassword}\" will be used upon account creation.";
                notice.ForeColor = Color.FromArgb(30, 130, 70);
            }
        }

        // =========================================================
        //  ERROR HELPERS
        // =========================================================
        private void SetFieldError(Control ctrl, bool hasError)
        {
            if (ctrl == null) return;
            Color errorColor = Color.FromArgb(255, 228, 230);
            Color normalColor = Color.White;

            if (ctrl is Guna.UI2.WinForms.Guna2TextBox gunaTb)
            {
                gunaTb.FillColor = hasError ? errorColor : normalColor;
                gunaTb.BorderColor = hasError ? Color.FromArgb(220, 53, 69) : Color.FromArgb(213, 218, 223);
                gunaTb.BorderThickness = hasError ? 2 : 1;
            }
            else if (ctrl is Guna.UI2.WinForms.Guna2ComboBox gunaCb)
            {
                gunaCb.FillColor = hasError ? errorColor : normalColor;
                gunaCb.BorderColor = hasError ? Color.FromArgb(220, 53, 69) : Color.FromArgb(213, 218, 223);
                gunaCb.BorderThickness = hasError ? 2 : 1;
            }
        }

        private void ShowError(string msg)
        {
            lblInlineError.Text = "⚠  " + msg;
            lblInlineError.Visible = true;
        }

        private void HideError()
        {
            if (lblInlineError != null)
                lblInlineError.Visible = false;
        }

        private void ClearAllErrors()
        {
            SetFieldError(txtLastName, false);
            SetFieldError(txtFirstName, false);
            SetFieldError(txtMiddleName, false);
            SetFieldError(txtEmail, false);
            SetFieldError(cmbRole, false);
            HideError();
        }

        // =========================================================
        //  CREATE USER
        // =========================================================
        private void CreateUser()
        {
            string connStr = SettingsManager.Current.GetConnectionString();

            ClearAllErrors();

            bool hasError = false;

            if (string.IsNullOrWhiteSpace(cmbRole.Text))
            { SetFieldError(cmbRole, true); hasError = true; }

            if (string.IsNullOrWhiteSpace(txtLastName.Text))
            { SetFieldError(txtLastName, true); hasError = true; }

            if (string.IsNullOrWhiteSpace(txtFirstName.Text))
            { SetFieldError(txtFirstName, true); hasError = true; }

            if (string.IsNullOrWhiteSpace(txtEmail.Text))
            { SetFieldError(txtEmail, true); hasError = true; }

            if (hasError)
            {
                ShowError("Please fill in all required fields.");
                return;
            }

            hasError = false;
            if (txtLastName.Text.Any(char.IsDigit)) { SetFieldError(txtLastName, true); hasError = true; }
            if (txtFirstName.Text.Any(char.IsDigit)) { SetFieldError(txtFirstName, true); hasError = true; }
            if (txtMiddleName.Text.Any(char.IsDigit)) { SetFieldError(txtMiddleName, true); hasError = true; }

            if (hasError)
            {
                ShowError("Names cannot contain numbers.");
                return;
            }

            if (!txtEmail.Text.Trim().EndsWith("@gmail.com", StringComparison.OrdinalIgnoreCase))
            {
                SetFieldError(txtEmail, true);
                ShowError("Email must end with @gmail.com");
                return;
            }

            string semester = cmbRole.Text == "Student" ? "1st Semester" : "Null";
            string username = GenerateUsername(txtLastName.Text, txtFirstName.Text, txtMiddleName.Text, connStr);

            try
            {
                long userId;

                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    string Insertquery2 = @"
                        INSERT INTO user_credential (username, p_word, roles, user_status, authentication_condition, remaining_limit)
                        VALUES (@Uname, MD5(@Password), @UserRole, @Status, @authentication_condition, @remaining_limit);
                        SELECT LAST_INSERT_ID();";

                    using (MySqlCommand cmd2 = new MySqlCommand(Insertquery2, conn))
                    {
                        cmd2.Parameters.AddWithValue("@Uname", username);
                        cmd2.Parameters.AddWithValue("@Password", _defaultPassword);
                        cmd2.Parameters.AddWithValue("@UserRole", cmbRole.Text.Trim());
                        cmd2.Parameters.AddWithValue("@Status", "Active");
                        cmd2.Parameters.AddWithValue("@authentication_condition", "Disabled");
                        cmd2.Parameters.AddWithValue("@remaining_limit", 20);
                        userId = Convert.ToInt64(cmd2.ExecuteScalar());
                    }

                    string Insertquery = @"
                        INSERT INTO user_information 
                            (user_id, lastname, firstname, middlename, email, school_semester) 
                        VALUES 
                            (@user_id, @lastname, @firstname, @middlename, @email, @school_semester)";

                    using (MySqlCommand cmd = new MySqlCommand(Insertquery, conn))
                    {
                        cmd.Parameters.AddWithValue("@user_id", userId);
                        cmd.Parameters.AddWithValue("@lastname", txtLastName.Text.ToUpper());
                        cmd.Parameters.AddWithValue("@firstname", txtFirstName.Text.ToUpper());
                        cmd.Parameters.AddWithValue("@middlename", txtMiddleName.Text.ToUpper());
                        cmd.Parameters.AddWithValue("@email", txtEmail.Text.Trim());
                        cmd.Parameters.AddWithValue("@school_semester", semester);
                        cmd.ExecuteNonQuery();
                    }

                    string AttendanceQuery = "INSERT INTO professor_attendance (student_id, student_name) VALUES (@student_id, @student_name)";
                    using (MySqlCommand cmd3 = new MySqlCommand(AttendanceQuery, conn))
                    {
                        cmd3.Parameters.AddWithValue("@student_id", userId);
                        cmd3.Parameters.AddWithValue("@student_name",
                            $"{txtLastName.Text.ToUpper()} {txtFirstName.Text.ToUpper()} {txtMiddleName.Text.ToUpper()}");
                        cmd3.ExecuteNonQuery();
                    }

                    string rootPath = SettingsManager.Current.SaveFolder;
                    if (string.IsNullOrEmpty(rootPath))
                    {
                        ShowError("Root folder is not configured.");
                        return;
                    }

                    if (!Directory.Exists(rootPath))
                    {
                        try { Directory.CreateDirectory(rootPath); }
                        catch (Exception ex) { ShowError("Could not create root folder: " + ex.Message); return; }
                    }

                    string folderName = SanitizeFolderName(
                        $"{txtLastName.Text.ToUpper()}_{txtFirstName.Text.ToUpper()}_{txtMiddleName.Text.ToUpper()}");
                    string userFolderPath = Path.Combine(rootPath, folderName);

                    try
                    {
                        if (!Directory.Exists(userFolderPath))
                            Directory.CreateDirectory(userFolderPath);
                    }
                    catch (Exception ex) { ShowError("Could not create user folder: " + ex.Message); return; }

                    using (var cmd4 = new MySqlCommand("INSERT INTO mainfolderpath (user_id, FolderPath) VALUES (@user_id, @FolderPath)", conn))
                    {
                        cmd4.Parameters.AddWithValue("@user_id", userId);
                        cmd4.Parameters.AddWithValue("@FolderPath", userFolderPath);
                        cmd4.ExecuteNonQuery();
                    }
                }

                string email = txtEmail.Text.Trim();
                string fullName = $"{txtFirstName.Text.Trim()} {txtMiddleName.Text.Trim()} {txtLastName.Text.Trim()}".Trim();
                string role = cmbRole.Text.Trim();

                bool emailed = TrySendCredentialsEmail(email, fullName, username, _defaultPassword, role);

                if (emailed)
                    CustomMessageBox.Show($"Account created!\n\nUsername: {username}\nPassword: {_defaultPassword}\n\nEmailed to {email}",
                        "Account Created", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Information);
                else
                    CustomMessageBox.Show($"Account created but email failed.\n\nUsername: {username}\nPassword: {_defaultPassword}",
                        "Account Created (Email Failed)", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Warning);

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                ShowError("CreateUser error: " + ex.Message);
                Console.WriteLine("CreateUser error: " + ex.Message);
            }
        }

        private string GenerateUsername(string last, string first, string middle, string connStr)
        {
            string l = string.IsNullOrWhiteSpace(last) ? "X" : last.Trim().Substring(0, 1).ToUpper();
            string f = string.IsNullOrWhiteSpace(first) ? "X" : first.Trim().Substring(0, 1).ToUpper();
            string m = string.IsNullOrWhiteSpace(middle) ? "X" : middle.Trim().Substring(0, 1).ToUpper();

            string baseUser = $"{l}{f}{m}{DateTime.Now:MMddyyyy}";
            string candidate = baseUser;
            int suffix = 1;

            using (var conn = new MySqlConnection(connStr))
            {
                conn.Open();
                while (true)
                {
                    using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM user_credential WHERE username = @u", conn))
                    {
                        cmd.Parameters.AddWithValue("@u", candidate);
                        if (Convert.ToInt32(cmd.ExecuteScalar()) == 0) return candidate;
                    }
                    candidate = $"{baseUser}-{suffix}";
                    suffix++;
                }
            }
        }

        private bool TrySendCredentialsEmail(string toEmail, string fullName, string username, string password, string role)
        {
            try
            {
                using (var mail = new MailMessage())
                {
                    mail.From = new MailAddress(SettingsManager.Current.SmtpFrom, SettingsManager.Current.SmtpFromName);
                    mail.To.Add(toEmail);
                    mail.Subject = "Your CDSGA Hub account credentials";
                    mail.IsBodyHtml = true;

                    string safeName = System.Security.SecurityElement.Escape(fullName);
                    string safeUser = System.Security.SecurityElement.Escape(username);
                    string safePass = System.Security.SecurityElement.Escape(password);
                    string safeRole = System.Security.SecurityElement.Escape(role);

                    mail.Body = $@"
<div style='font-family:Segoe UI,Arial,sans-serif;font-size:14px;color:#222;'>
  <h2 style='color:#8B0000;'>CDSGA Hub</h2>
  <p>Hello <b>{safeName}</b>,</p>
  <p>Your account has been created.</p>
  <table style='border-collapse:collapse;margin:12px 0;'>
    <tr><td style='padding:6px 12px;background:#f5f5f5;'><b>Role</b></td><td style='padding:6px 12px;'>{safeRole}</td></tr>
    <tr><td style='padding:6px 12px;background:#f5f5f5;'><b>Username</b></td><td style='padding:6px 12px;'>{safeUser}</td></tr>
    <tr><td style='padding:6px 12px;background:#f5f5f5;'><b>Password</b></td><td style='padding:6px 12px;'>{safePass}</td></tr>
  </table>
  <p>Please log in and change your password.</p>
</div>";

                    using (var smtp = new SmtpClient(SettingsManager.Current.SmtpHost, SettingsManager.Current.SmtpPort))
                    {
                        smtp.EnableSsl = true;
                        smtp.Credentials = new NetworkCredential(
                            SettingsManager.Current.SmtpUser,
                            SettingsManager.Current.SmtpPass);
                        smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                        smtp.Timeout = 15000;
                        smtp.Send(mail);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("[CreateAccountForm] Email send failed: " + ex.Message);
                return false;
            }
        }

        private string SanitizeFolderName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "Unknown";
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            name = name.Trim().TrimEnd('.');
            return string.IsNullOrWhiteSpace(name) ? "Unknown" : name;
        }
    }
}