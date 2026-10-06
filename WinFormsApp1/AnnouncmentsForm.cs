using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinFormsApp1
{
    public partial class AnnouncementsForm : Form
    {
        private Panel leftCard;
        private Panel rightCard;
        private TextBox txtAnnTitle;
        private TextBox txtAnnBody;
        private ComboBox cmbAnnPriority;
        private ComboBox cmbAnnTarget;
        private TextBox txtAnnSection;
        private Label lblAnnSectionLabel;
        private FlowLayoutPanel flpAnnouncements;
        private Label lblEmailStatus;

        public AnnouncementsForm()
        {
            InitializeComponent();
            BuildUI();
            EnsureAnnouncementsTable();
            LoadAnnouncements();
        }

        private void BuildUI()
        {
            this.Text = "Announcements";
            this.Size = new Size(1350, 620);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.White;
            this.MinimumSize = new Size(1000, 500);

            // ---------- LEFT: Compose ----------
            leftCard = new Guna.UI2.WinForms.Guna2Panel
            {
                Location = new Point(20, 20),
                Size = new Size(500, 545),
                BorderRadius = 12,
                FillColor = Color.FromArgb(252, 248, 248),
                BorderColor = Color.FromArgb(230, 220, 220),
                BorderThickness = 1,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom
            };
            this.Controls.Add(leftCard);

            var lblCompose = new Label
            {
                Text = "📢  Compose Announcement",
                Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold),
                ForeColor = Color.Maroon,
                AutoSize = true,
                Location = new Point(18, 14)
            };
            leftCard.Controls.Add(lblCompose);

            leftCard.Controls.Add(new Label
            {
                Text = "Title:",
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(20, 55)
            });
            txtAnnTitle = new TextBox
            {
                Location = new Point(20, 78),
                Width = 460,
                Font = new Font("Segoe UI", 10F),
                BorderStyle = BorderStyle.FixedSingle,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            leftCard.Controls.Add(txtAnnTitle);

            leftCard.Controls.Add(new Label
            {
                Text = "Message:",
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(20, 112)
            });
            txtAnnBody = new TextBox
            {
                Location = new Point(20, 135),
                Size = new Size(460, 200),
                Font = new Font("Segoe UI", 10F),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                BorderStyle = BorderStyle.FixedSingle,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            leftCard.Controls.Add(txtAnnBody);

            leftCard.Controls.Add(new Label
            {
                Text = "Priority:",
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(20, 348)
            });
            cmbAnnPriority = new ComboBox
            {
                Location = new Point(20, 371),
                Width = 220,
                Font = new Font("Segoe UI", 10F),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbAnnPriority.Items.AddRange(new object[] { "Normal", "Important", "Urgent" });
            cmbAnnPriority.SelectedIndex = 0;
            leftCard.Controls.Add(cmbAnnPriority);

            leftCard.Controls.Add(new Label
            {
                Text = "Target audience:",
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(260, 348)
            });
            cmbAnnTarget = new ComboBox
            {
                Location = new Point(260, 371),
                Width = 220,
                Font = new Font("Segoe UI", 10F),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbAnnTarget.Items.AddRange(new object[] { "All Users", "Students Only", "Professors Only", "Specific Section" });
            cmbAnnTarget.SelectedIndex = 0;
            cmbAnnTarget.SelectedIndexChanged += (s, e) =>
            {
                bool showSection = cmbAnnTarget.Text == "Specific Section";
                txtAnnSection.Enabled = showSection;
                lblAnnSectionLabel.Enabled = showSection;
            };
            leftCard.Controls.Add(cmbAnnTarget);

            lblAnnSectionLabel = new Label
            {
                Text = "Section (e.g. 4-1):",
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(20, 410),
                Enabled = false
            };
            leftCard.Controls.Add(lblAnnSectionLabel);

            txtAnnSection = new TextBox
            {
                Location = new Point(20, 433),
                Width = 220,
                Font = new Font("Segoe UI", 10F),
                BorderStyle = BorderStyle.FixedSingle,
                Enabled = false
            };
            leftCard.Controls.Add(txtAnnSection);

            var btnPost = new Guna.UI2.WinForms.Guna2Button
            {
                Text = "Post Announcement",
                Size = new Size(220, 44),
                Location = new Point(260, 425),
                BorderRadius = 10,
                FillColor = Color.Maroon,
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            btnPost.HoverState.FillColor = Color.FromArgb(100, 0, 0);
            btnPost.Click += (s, e) => PostAnnouncement();
            leftCard.Controls.Add(btnPost);

            // ---------- RIGHT: Recent ----------
            rightCard = new Guna.UI2.WinForms.Guna2Panel
            {
                Location = new Point(540, 20),
                Size = new Size(785, 545),
                BorderRadius = 12,
                FillColor = Color.FromArgb(252, 248, 248),
                BorderColor = Color.FromArgb(230, 220, 220),
                BorderThickness = 1,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };
            this.Controls.Add(rightCard);

            var lblRecent = new Label
            {
                Text = "📋  Recent Announcements",
                Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold),
                ForeColor = Color.Maroon,
                AutoSize = true,
                Location = new Point(18, 14)
            };
            rightCard.Controls.Add(lblRecent);

            // Refresh button
            var btnRefresh = new Guna.UI2.WinForms.Guna2Button
            {
                Text = "🔄 Refresh",
                Size = new Size(100, 32),
                Location = new Point(555, 12),
                BorderRadius = 8,
                FillColor = Color.FromArgb(234, 234, 234),
                ForeColor = Color.FromArgb(50, 50, 50),
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnRefresh.Click += (s, e) => LoadAnnouncements();
            rightCard.Controls.Add(btnRefresh);

            // NEW: History button
            var btnHistory = new Guna.UI2.WinForms.Guna2Button
            {
                Text = "📜 History",
                Size = new Size(100, 32),
                Location = new Point(665, 12),
                BorderRadius = 8,
                FillColor = Color.FromArgb(90, 90, 100),
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnHistory.HoverState.FillColor = Color.FromArgb(70, 70, 80);
            btnHistory.Click += (s, e) => OpenDeletionHistory();
            rightCard.Controls.Add(btnHistory);

            flpAnnouncements = new FlowLayoutPanel
            {
                Location = new Point(18, 55),
                Size = new Size(750, 475),
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };
            rightCard.Controls.Add(flpAnnouncements);
        }

        // =========================================================
        //  TABLE SETUP — with soft-delete columns
        // =========================================================
        private void EnsureAnnouncementsTable()
        {
            try
            {
                string connStr = SettingsManager.Current.GetConnectionString();
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    // Base table
                    string q = @"
                        CREATE TABLE IF NOT EXISTS announcements (
                            announcement_id INT AUTO_INCREMENT PRIMARY KEY,
                            title VARCHAR(255) NOT NULL,
                            body TEXT NOT NULL,
                            priority VARCHAR(20) DEFAULT 'Normal',
                            target VARCHAR(50) DEFAULT 'All Users',
                            section_filter VARCHAR(50) NULL,
                            posted_by INT NOT NULL,
                            posted_at DATETIME DEFAULT CURRENT_TIMESTAMP
                        );";
                    using (var cmd = new MySqlCommand(q, conn))
                        cmd.ExecuteNonQuery();

                    // Add soft-delete columns if they don't exist
                    AddColumnIfMissing(conn, "announcements", "is_deleted",
                        "TINYINT(1) NOT NULL DEFAULT 0");
                    AddColumnIfMissing(conn, "announcements", "deleted_at",
                        "DATETIME NULL");
                    AddColumnIfMissing(conn, "announcements", "deleted_by",
                        "INT NULL");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("EnsureAnnouncementsTable error: " + ex.Message);
            }
        }

        private void AddColumnIfMissing(MySqlConnection conn, string table, string column, string definition)
        {
            try
            {
                string check = @"
                    SELECT COUNT(*) 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_SCHEMA = DATABASE()
                      AND TABLE_NAME = @t
                      AND COLUMN_NAME = @c";
                using (var cmd = new MySqlCommand(check, conn))
                {
                    cmd.Parameters.AddWithValue("@t", table);
                    cmd.Parameters.AddWithValue("@c", column);
                    long count = Convert.ToInt64(cmd.ExecuteScalar());
                    if (count > 0) return;
                }

                using (var cmd = new MySqlCommand(
                    $"ALTER TABLE `{table}` ADD COLUMN `{column}` {definition}", conn))
                {
                    cmd.ExecuteNonQuery();
                    Console.WriteLine($"[Announcements] Added column {column} to {table}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AddColumnIfMissing({column}) error: " + ex.Message);
            }
        }

        // =========================================================
        //  POST + EMAIL
        // =========================================================
        private async void PostAnnouncement()
        {
            string title = txtAnnTitle.Text.Trim();
            string body = txtAnnBody.Text.Trim();
            string priority = cmbAnnPriority.Text;
            string target = cmbAnnTarget.Text;
            string sectionFilter = txtAnnSection.Text.Trim();

            if (string.IsNullOrEmpty(title))
            {
                CustomMessageBox.Show("Please enter a title.", "Validation",
                    CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrEmpty(body))
            {
                CustomMessageBox.Show("Please enter a message.", "Validation",
                    CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Warning);
                return;
            }

            if (target == "Specific Section" && string.IsNullOrEmpty(sectionFilter))
            {
                CustomMessageBox.Show("Please enter a section.", "Validation",
                    CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Warning);
                return;
            }

            if (target != "Specific Section")
                sectionFilter = null;

            // ---------- 1. Save to database ----------
            try
            {
                string connStr = SettingsManager.Current.GetConnectionString();
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string q = @"
                        INSERT INTO announcements 
                            (title, body, priority, target, section_filter, posted_by, posted_at)
                        VALUES 
                            (@title, @body, @priority, @target, @section_filter, @posted_by, NOW())";
                    using (var cmd = new MySqlCommand(q, conn))
                    {
                        cmd.Parameters.AddWithValue("@title", title);
                        cmd.Parameters.AddWithValue("@body", body);
                        cmd.Parameters.AddWithValue("@priority", priority);
                        cmd.Parameters.AddWithValue("@target", target);
                        cmd.Parameters.AddWithValue("@section_filter",
                            (object)sectionFilter ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@posted_by", GetAdminUserId());
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show("Failed to save announcement:\n" + ex.Message,
                    "Error", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Error);
                return;
            }

            // ---------- 2. Clear form ----------
            txtAnnTitle.Clear();
            txtAnnBody.Clear();
            cmbAnnPriority.SelectedIndex = 0;
            cmbAnnTarget.SelectedIndex = 0;
            txtAnnSection.Clear();

            LoadAnnouncements();

            // ---------- 3. Confirm + ask to email ----------
            var sendEmail = CustomMessageBox.Show(
                "Announcement posted!\n\nDo you also want to email this announcement to the target audience?",
                "Send Email?",
                CustomMessageBoxButtons.YesNo,
                CustomMessageBoxIcon.Question);

            if (sendEmail != CustomMessageBoxResult.Yes)
            {
                CustomMessageBox.Show("Announcement posted (not emailed).", "Done",
                    CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Information);
                return;
            }

            List<string> recipients = GetRecipientEmails(target, sectionFilter);

            if (recipients.Count == 0)
            {
                CustomMessageBox.Show("No recipients found for the selected target.",
                    "No Recipients", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Warning);
                return;
            }

            var go = CustomMessageBox.Show(
                $"Send email to {recipients.Count} recipient(s)?",
                "Confirm Send",
                CustomMessageBoxButtons.YesNo,
                CustomMessageBoxIcon.Question);

            if (go != CustomMessageBoxResult.Yes) return;

            ShowEmailStatus($"Sending to {recipients.Count} recipient(s)...", Color.FromArgb(52, 120, 200));

            int sent = 0, failed = 0;

            await Task.Run(() =>
            {
                foreach (string email in recipients)
                {
                    if (TrySendAnnouncementEmail(email, title, body, priority, target, sectionFilter))
                        sent++;
                    else
                        failed++;

                    int s = sent, f = failed;
                    if (this.IsHandleCreated && !this.IsDisposed)
                    {
                        this.BeginInvoke(new Action(() =>
                        {
                            ShowEmailStatus($"Sending... {s + f}/{recipients.Count}  (✔ {s}   ✖ {f})",
                                Color.FromArgb(52, 120, 200));
                        }));
                    }
                }
            });

            if (failed == 0)
            {
                ShowEmailStatus($"✔ Emailed {sent} recipient(s).", Color.FromArgb(30, 130, 70));
                CustomMessageBox.Show($"Announcement emailed to {sent} recipient(s).", "Email Sent",
                    CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Information);
            }
            else
            {
                ShowEmailStatus($"✔ {sent} sent, ✖ {failed} failed.", Color.FromArgb(180, 40, 40));
                CustomMessageBox.Show(
                    $"Email broadcast finished.\n\n✔ Sent: {sent}\n✖ Failed: {failed}",
                    "Email Broadcast", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Warning);
            }
        }

        private void ShowEmailStatus(string text, Color color)
        {
            if (lblEmailStatus == null || lblEmailStatus.IsDisposed)
            {
                lblEmailStatus = new Label
                {
                    AutoSize = false,
                    Height = 22,
                    Width = 460,
                    Location = new Point(20, 490),
                    Font = new Font("Segoe UI", 9F, FontStyle.Italic),
                    BackColor = Color.Transparent,
                    Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
                };
                leftCard.Controls.Add(lblEmailStatus);
                lblEmailStatus.BringToFront();
            }

            lblEmailStatus.Text = text;
            lblEmailStatus.ForeColor = color;
            lblEmailStatus.Visible = true;
        }

        // =========================================================
        //  RECIPIENT LOOKUP
        // =========================================================
        private List<string> GetRecipientEmails(string target, string sectionFilter)
        {
            var list = new List<string>();

            try
            {
                string connStr = SettingsManager.Current.GetConnectionString();
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    string q;
                    switch (target)
                    {
                        case "Students Only":
                            q = @"
                                SELECT i.email
                                FROM user_credential u
                                INNER JOIN user_information i ON i.user_id = u.user_id
                                WHERE u.roles = 'Student'
                                  AND u.user_status = 'Active'
                                  AND i.email IS NOT NULL
                                  AND i.email <> ''";
                            break;

                        case "Professors Only":
                            q = @"
                                SELECT i.email
                                FROM user_credential u
                                INNER JOIN user_information i ON i.user_id = u.user_id
                                WHERE u.roles = 'Professor'
                                  AND u.user_status = 'Active'
                                  AND i.email IS NOT NULL
                                  AND i.email <> ''";
                            break;

                        case "Specific Section":
                            q = @"
                                SELECT i.email
                                FROM user_credential u
                                INNER JOIN user_information i ON i.user_id = u.user_id
                                WHERE u.user_status = 'Active'
                                  AND i.school_section = @sec
                                  AND i.email IS NOT NULL
                                  AND i.email <> ''";
                            break;

                        case "All Users":
                        default:
                            q = @"
                                SELECT i.email
                                FROM user_credential u
                                INNER JOIN user_information i ON i.user_id = u.user_id
                                WHERE u.user_status = 'Active'
                                  AND i.email IS NOT NULL
                                  AND i.email <> ''";
                            break;
                    }

                    using (var cmd = new MySqlCommand(q, conn))
                    {
                        if (target == "Specific Section")
                            cmd.Parameters.AddWithValue("@sec", sectionFilter);

                        using (var r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                string email = r["email"].ToString().Trim();
                                if (!string.IsNullOrEmpty(email) && !list.Contains(email))
                                    list.Add(email);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetRecipientEmails error: " + ex.Message);
            }

            return list;
        }

        // =========================================================
        //  EMAIL SENDER
        // =========================================================
        private bool TrySendAnnouncementEmail(string toEmail, string title, string body,
            string priority, string target, string section)
        {
            try
            {
                using (var mail = new MailMessage())
                {
                    mail.From = new MailAddress(
                        SettingsManager.Current.SmtpFrom,
                        SettingsManager.Current.SmtpFromName);
                    mail.To.Add(toEmail);
                    mail.Subject = $"[{priority}] {title}";
                    mail.IsBodyHtml = true;

                    Color accent = priority == "Urgent" ? Color.FromArgb(200, 40, 40)
                                 : priority == "Important" ? Color.FromArgb(220, 150, 30)
                                 : Color.FromArgb(52, 120, 200);

                    string accentHex = $"#{accent.R:X2}{accent.G:X2}{accent.B:X2}";

                    string safeTitle = System.Security.SecurityElement.Escape(title);
                    string safeBody = System.Security.SecurityElement.Escape(body)
                        .Replace("\r\n", "<br/>").Replace("\n", "<br/>");
                    string safeTarget = System.Security.SecurityElement.Escape(target);
                    string safeSection = System.Security.SecurityElement.Escape(section ?? "");

                    string audienceLine = target + (string.IsNullOrEmpty(section) ? "" : $" ({safeSection})");

                    mail.Body = $@"
<!DOCTYPE html>
<html>
<head>
<meta charset='utf-8'>
</head>
<body style='margin:0;padding:0;background:#f5f5f7;font-family:Segoe UI,Arial,sans-serif;'>
  <div style='max-width:640px;margin:24px auto;background:#ffffff;border-radius:12px;overflow:hidden;box-shadow:0 2px 12px rgba(0,0,0,0.08);'>
    <div style='background:{accentHex};padding:20px 24px;color:#fff;'>
      <div style='font-size:13px;letter-spacing:1px;text-transform:uppercase;opacity:0.9;'>CDSGA Hub Announcement</div>
      <div style='font-size:22px;font-weight:600;margin-top:6px;'>{safeTitle}</div>
    </div>

    <div style='padding:24px;color:#222;font-size:14px;line-height:1.6;'>
      {safeBody}
    </div>

    <div style='padding:16px 24px;background:#fafafa;border-top:1px solid #eee;font-size:12px;color:#777;'>
      <div><b>Priority:</b> {priority}</div>
      <div><b>Audience:</b> {audienceLine}</div>
      <div style='margin-top:6px;'>This is an automated message from the CDSGA Hub Administrator.</div>
    </div>
  </div>
</body>
</html>";

                    using (var smtp = new SmtpClient(
                        SettingsManager.Current.SmtpHost,
                        SettingsManager.Current.SmtpPort))
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
                Console.WriteLine($"[Announcement] Failed to send to {toEmail}: {ex.Message}");
                return false;
            }
        }

        private int GetAdminUserId()
        {
            try
            {
                string connStr = SettingsManager.Current.GetConnectionString();
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand(
                        "SELECT user_id FROM user_credential WHERE roles='Admin' LIMIT 1", conn))
                    {
                        object r = cmd.ExecuteScalar();
                        if (r != null && r != DBNull.Value)
                            return Convert.ToInt32(r);
                    }
                }
            }
            catch { }
            return 1;
        }

        // =========================================================
        //  LOAD ACTIVE ANNOUNCEMENTS (is_deleted = 0)
        // =========================================================
        private void LoadAnnouncements()
        {
            if (flpAnnouncements == null) return;

            flpAnnouncements.Controls.Clear();

            try
            {
                string connStr = SettingsManager.Current.GetConnectionString();
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string q = @"
                        SELECT announcement_id, title, body, priority, target,
                               section_filter, posted_at
                        FROM announcements
                        WHERE is_deleted = 0
                        ORDER BY posted_at DESC
                        LIMIT 50";
                    using (var cmd = new MySqlCommand(q, conn))
                    using (var r = cmd.ExecuteReader())
                    {
                        bool any = false;
                        while (r.Read())
                        {
                            any = true;

                            int id = Convert.ToInt32(r["announcement_id"]);
                            string title = r["title"].ToString();
                            string body = r["body"].ToString();
                            string priority = r["priority"].ToString();
                            string target = r["target"].ToString();
                            string sectionFilter = r["section_filter"] == DBNull.Value
                                ? ""
                                : r["section_filter"].ToString();
                            DateTime postedAt = Convert.ToDateTime(r["posted_at"]);

                            flpAnnouncements.Controls.Add(BuildAnnouncementCard(
                                id, title, body, priority, target, sectionFilter, postedAt));
                        }

                        if (!any)
                        {
                            var empty = new Label
                            {
                                Text = "No announcements yet.",
                                Font = new Font("Segoe UI", 10F, FontStyle.Italic),
                                ForeColor = Color.Gray,
                                AutoSize = true,
                                Margin = new Padding(10)
                            };
                            flpAnnouncements.Controls.Add(empty);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("LoadAnnouncements error: " + ex.Message);
            }
        }

        // =========================================================
        //  ANNOUNCEMENT CARD (Active)
        // =========================================================
        private Panel BuildAnnouncementCard(int id, string title, string body,
            string priority, string target, string section, DateTime postedAt)
        {
            Color accent = priority == "Urgent" ? Color.FromArgb(200, 40, 40)
                         : priority == "Important" ? Color.FromArgb(220, 150, 30)
                         : Color.FromArgb(52, 120, 200);

            var card = new Panel
            {
                Width = 720,
                Height = 130,
                Margin = new Padding(0, 0, 0, 10),
                BackColor = Color.White,
                Tag = id
            };

            card.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                using (var path = new GraphicsPath())
                {
                    int r = 10;
                    var rect = new Rectangle(0, 0, card.Width - 1, card.Height - 1);
                    path.AddArc(rect.X, rect.Y, r * 2, r * 2, 180, 90);
                    path.AddArc(rect.Right - r * 2, rect.Y, r * 2, r * 2, 270, 90);
                    path.AddArc(rect.Right - r * 2, rect.Bottom - r * 2, r * 2, r * 2, 0, 90);
                    path.AddArc(rect.X, rect.Bottom - r * 2, r * 2, r * 2, 90, 90);
                    path.CloseFigure();
                    card.Region = new Region(path);
                }

                using (var border = new Pen(Color.FromArgb(230, 225, 225), 1))
                    g.DrawRectangle(border, 0, 0, card.Width - 1, card.Height - 1);

                using (var brush = new SolidBrush(accent))
                    g.FillRectangle(brush, 0, 0, 6, card.Height);
            };

            var lblTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 30, 30),
                AutoSize = false,
                Size = new Size(600, 24),
                Location = new Point(20, 10)
            };
            card.Controls.Add(lblTitle);

            var lblMeta = new Label
            {
                Text = $"Priority: {priority}   •   Target: {target}" +
                       (string.IsNullOrEmpty(section) ? "" : $" ({section})") +
                       $"   •   Posted: {postedAt:MMM dd, yyyy hh:mm tt}",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(120, 120, 120),
                AutoSize = false,
                Size = new Size(600, 18),
                Location = new Point(20, 36)
            };
            card.Controls.Add(lblMeta);

            var lblBody = new Label
            {
                Text = body,
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = Color.FromArgb(70, 70, 70),
                AutoSize = false,
                Size = new Size(600, 60),
                Location = new Point(20, 58),
                AutoEllipsis = true
            };
            card.Controls.Add(lblBody);

            var btnDeleteAnn = new Guna.UI2.WinForms.Guna2Button
            {
                Text = "🗑",
                Size = new Size(36, 36),
                Location = new Point(670, 12),
                BorderRadius = 18,
                FillColor = Color.Transparent,
                ForeColor = Color.FromArgb(180, 40, 40),
                Font = new Font("Segoe UI Emoji", 12F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnDeleteAnn.HoverState.FillColor = Color.FromArgb(255, 240, 240);
            btnDeleteAnn.Click += (s, e) =>
            {
                var confirm = CustomMessageBox.Show(
                    $"Move \"{title}\" to history?\n\nYou can still view it in History and restore it later.",
                    "Confirm Delete",
                    CustomMessageBoxButtons.YesNo,
                    CustomMessageBoxIcon.Warning);
                if (confirm != CustomMessageBoxResult.Yes) return;

                SoftDeleteAnnouncement(id);
            };
            card.Controls.Add(btnDeleteAnn);

            return card;
        }

        // =========================================================
        //  SOFT DELETE + RESTORE + PURGE
        // =========================================================
        private void SoftDeleteAnnouncement(int id)
        {
            try
            {
                string connStr = SettingsManager.Current.GetConnectionString();
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand(@"
                        UPDATE announcements 
                        SET is_deleted = 1,
                            deleted_at = NOW(),
                            deleted_by = @by
                        WHERE announcement_id = @id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.Parameters.AddWithValue("@by", GetAdminUserId());
                        cmd.ExecuteNonQuery();
                    }
                }
                LoadAnnouncements();
            }
            catch (Exception ex)
            {
                Console.WriteLine("SoftDeleteAnnouncement error: " + ex.Message);
            }
        }

        private void RestoreAnnouncement(int id)
        {
            try
            {
                string connStr = SettingsManager.Current.GetConnectionString();
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand(@"
                        UPDATE announcements 
                        SET is_deleted = 0,
                            deleted_at = NULL,
                            deleted_by = NULL
                        WHERE announcement_id = @id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("RestoreAnnouncement error: " + ex.Message);
            }
        }

        private void PurgeAnnouncement(int id)
        {
            try
            {
                string connStr = SettingsManager.Current.GetConnectionString();
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand(
                        "DELETE FROM announcements WHERE announcement_id = @id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("PurgeAnnouncement error: " + ex.Message);
            }
        }

        private void PurgeAllDeleted()
        {
            try
            {
                string connStr = SettingsManager.Current.GetConnectionString();
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand(
                        "DELETE FROM announcements WHERE is_deleted = 1", conn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("PurgeAllDeleted error: " + ex.Message);
            }
        }

        // =========================================================
        //  DELETION HISTORY DIALOG
        // =========================================================
        private void OpenDeletionHistory()
        {
            using (var dlg = new Form())
            {
                dlg.Text = "📜  Deletion History";
                dlg.Size = new Size(900, 640);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.MinimumSize = new Size(700, 500);
                dlg.BackColor = Color.FromArgb(245, 245, 248);

                var topPanel = new Panel
                {
                    Dock = DockStyle.Top,
                    Height = 60,
                    BackColor = Color.White,
                    Padding = new Padding(16, 10, 16, 10)
                };
                dlg.Controls.Add(topPanel);

                var lblTitle = new Label
                {
                    Text = "Deleted Announcements",
                    Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold),
                    ForeColor = Color.Maroon,
                    AutoSize = true,
                    Location = new Point(16, 14)
                };
                topPanel.Controls.Add(lblTitle);

                var lblCount = new Label
                {
                    Text = "",
                    Font = new Font("Segoe UI", 9F, FontStyle.Italic),
                    ForeColor = Color.FromArgb(120, 120, 120),
                    AutoSize = true,
                    Location = new Point(230, 22)
                };
                topPanel.Controls.Add(lblCount);

                var btnPurgeAll = new Guna.UI2.WinForms.Guna2Button
                {
                    Text = "🗑 Purge All",
                    Size = new Size(120, 34),
                    Location = new Point(dlg.ClientSize.Width - 140, 14),
                    Anchor = AnchorStyles.Top | AnchorStyles.Right,
                    BorderRadius = 8,
                    FillColor = Color.FromArgb(180, 40, 40),
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold)
                };
                topPanel.Controls.Add(btnPurgeAll);

                var flpHistory = new FlowLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    FlowDirection = FlowDirection.TopDown,
                    WrapContents = false,
                    AutoScroll = true,
                    Padding = new Padding(20),
                    BackColor = Color.FromArgb(245, 245, 248)
                };
                dlg.Controls.Add(flpHistory);
                flpHistory.BringToFront();

                Action reload = null;
                reload = () =>
                {
                    flpHistory.Controls.Clear();

                    try
                    {
                        string connStr = SettingsManager.Current.GetConnectionString();
                        using (var conn = new MySqlConnection(connStr))
                        {
                            conn.Open();
                            string q = @"
                                SELECT a.announcement_id, a.title, a.body, a.priority, a.target,
                                       a.section_filter, a.posted_at, a.deleted_at, a.deleted_by,
                                       u.username AS deleted_by_name
                                FROM announcements a
                                LEFT JOIN user_credential u ON u.user_id = a.deleted_by
                                WHERE a.is_deleted = 1
                                ORDER BY a.deleted_at DESC
                                LIMIT 200";

                            int count = 0;
                            using (var cmd = new MySqlCommand(q, conn))
                            using (var r = cmd.ExecuteReader())
                            {
                                while (r.Read())
                                {
                                    count++;

                                    int id = Convert.ToInt32(r["announcement_id"]);
                                    string title = r["title"].ToString();
                                    string body = r["body"].ToString();
                                    string priority = r["priority"].ToString();
                                    string target = r["target"].ToString();
                                    string section = r["section_filter"] == DBNull.Value
                                        ? ""
                                        : r["section_filter"].ToString();
                                    DateTime postedAt = Convert.ToDateTime(r["posted_at"]);
                                    DateTime? deletedAt = r["deleted_at"] == DBNull.Value
                                        ? (DateTime?)null
                                        : Convert.ToDateTime(r["deleted_at"]);
                                    string deletedBy = r["deleted_by_name"] == DBNull.Value
                                        ? "Unknown"
                                        : r["deleted_by_name"].ToString();

                                    flpHistory.Controls.Add(BuildHistoryCard(
                                        id, title, body, priority, target, section,
                                        postedAt, deletedAt, deletedBy,
                                        () => { reload(); LoadAnnouncements(); }));
                                }
                            }

                            lblCount.Text = $"{count} deleted item(s)";

                            if (count == 0)
                            {
                                var empty = new Label
                                {
                                    Text = "No deleted announcements. 🎉",
                                    Font = new Font("Segoe UI", 11F, FontStyle.Italic),
                                    ForeColor = Color.Gray,
                                    AutoSize = true,
                                    Margin = new Padding(10, 20, 0, 0)
                                };
                                flpHistory.Controls.Add(empty);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("OpenDeletionHistory reload error: " + ex.Message);
                    }
                };

                btnPurgeAll.Click += (s, e) =>
                {
                    var c = CustomMessageBox.Show(
                        "Permanently delete ALL items in history?\n\nThis CANNOT be undone.",
                        "Confirm Purge All",
                        CustomMessageBoxButtons.YesNo,
                        CustomMessageBoxIcon.Warning);
                    if (c != CustomMessageBoxResult.Yes) return;

                    PurgeAllDeleted();
                    reload();
                    LoadAnnouncements();
                };

                dlg.Load += (s, e) => reload();
                dlg.ShowDialog(this);
            }
        }

        private Panel BuildHistoryCard(int id, string title, string body,
            string priority, string target, string section,
            DateTime postedAt, DateTime? deletedAt, string deletedBy,
            Action onChanged)
        {
            Color accent = priority == "Urgent" ? Color.FromArgb(200, 40, 40)
                         : priority == "Important" ? Color.FromArgb(220, 150, 30)
                         : Color.FromArgb(52, 120, 200);

            var card = new Panel
            {
                Width = 800,
                Height = 150,
                Margin = new Padding(0, 0, 0, 12),
                BackColor = Color.White
            };

            card.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                using (var path = new GraphicsPath())
                {
                    int r = 10;
                    var rect = new Rectangle(0, 0, card.Width - 1, card.Height - 1);
                    path.AddArc(rect.X, rect.Y, r * 2, r * 2, 180, 90);
                    path.AddArc(rect.Right - r * 2, rect.Y, r * 2, r * 2, 270, 90);
                    path.AddArc(rect.Right - r * 2, rect.Bottom - r * 2, r * 2, r * 2, 0, 90);
                    path.AddArc(rect.X, rect.Bottom - r * 2, r * 2, r * 2, 90, 90);
                    path.CloseFigure();
                    card.Region = new Region(path);
                }

                using (var border = new Pen(Color.FromArgb(230, 225, 225), 1))
                    g.DrawRectangle(border, 0, 0, card.Width - 1, card.Height - 1);

                // Grayed-out accent because it's deleted
                using (var brush = new SolidBrush(Color.FromArgb(150, accent)))
                    g.FillRectangle(brush, 0, 0, 6, card.Height);
            };

            var lblTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(90, 90, 90),
                AutoSize = false,
                Size = new Size(660, 24),
                Location = new Point(20, 10)
            };
            card.Controls.Add(lblTitle);

            var lblMeta = new Label
            {
                Text = $"Priority: {priority}   •   Target: {target}" +
                       (string.IsNullOrEmpty(section) ? "" : $" ({section})") +
                       $"   •   Posted: {postedAt:MMM dd, yyyy hh:mm tt}",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(120, 120, 120),
                AutoSize = false,
                Size = new Size(660, 18),
                Location = new Point(20, 36)
            };
            card.Controls.Add(lblMeta);

            string deletedLine = deletedAt.HasValue
                ? $"🗑  Deleted: {deletedAt.Value:MMM dd, yyyy hh:mm tt}   •   by {deletedBy}"
                : "🗑  Deleted";

            var lblDeleted = new Label
            {
                Text = deletedLine,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Italic),
                ForeColor = Color.FromArgb(180, 40, 40),
                AutoSize = false,
                Size = new Size(660, 18),
                Location = new Point(20, 56)
            };
            card.Controls.Add(lblDeleted);

            var lblBody = new Label
            {
                Text = body,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(100, 100, 100),
                AutoSize = false,
                Size = new Size(660, 60),
                Location = new Point(20, 78),
                AutoEllipsis = true
            };
            card.Controls.Add(lblBody);

            // Restore button
            var btnRestore = new Guna.UI2.WinForms.Guna2Button
            {
                Text = "↩ Restore",
                Size = new Size(100, 32),
                Location = new Point(690, 12),
                BorderRadius = 8,
                FillColor = Color.FromArgb(46, 160, 90),
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold)
            };
            btnRestore.HoverState.FillColor = Color.FromArgb(30, 130, 70);
            btnRestore.Click += (s, e) =>
            {
                var c = CustomMessageBox.Show(
                    $"Restore \"{title}\"?\n\nIt will appear in the active list again.",
                    "Confirm Restore",
                    CustomMessageBoxButtons.YesNo,
                    CustomMessageBoxIcon.Question);
                if (c != CustomMessageBoxResult.Yes) return;

                RestoreAnnouncement(id);
                onChanged?.Invoke();
            };
            card.Controls.Add(btnRestore);

            // Permanent delete button
            var btnPurge = new Guna.UI2.WinForms.Guna2Button
            {
                Text = "🗑 Purge",
                Size = new Size(100, 32),
                Location = new Point(690, 50),
                BorderRadius = 8,
                FillColor = Color.FromArgb(180, 40, 40),
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold)
            };
            btnPurge.HoverState.FillColor = Color.FromArgb(140, 20, 20);
            btnPurge.Click += (s, e) =>
            {
                var c = CustomMessageBox.Show(
                    $"Permanently delete \"{title}\"?\n\nThis CANNOT be undone.",
                    "Confirm Purge",
                    CustomMessageBoxButtons.YesNo,
                    CustomMessageBoxIcon.Warning);
                if (c != CustomMessageBoxResult.Yes) return;

                PurgeAnnouncement(id);
                onChanged?.Invoke();
            };
            card.Controls.Add(btnPurge);

            return card;
        }
    }
}