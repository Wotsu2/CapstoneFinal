using MySql.Data.MySqlClient;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
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

            var btnRefresh = new Guna.UI2.WinForms.Guna2Button
            {
                Text = "🔄 Refresh",
                Size = new Size(100, 32),
                Location = new Point(665, 12),
                BorderRadius = 8,
                FillColor = Color.FromArgb(234, 234, 234),
                ForeColor = Color.FromArgb(50, 50, 50),
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnRefresh.Click += (s, e) => LoadAnnouncements();
            rightCard.Controls.Add(btnRefresh);

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

        private void EnsureAnnouncementsTable()
        {
            try
            {
                string connStr = SettingsManager.Current.GetConnectionString();
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
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
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("EnsureAnnouncementsTable error: " + ex.Message);
            }
        }

        private void PostAnnouncement()
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

                CustomMessageBox.Show("Announcement posted!", "Success",
                    CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Information);

                txtAnnTitle.Clear();
                txtAnnBody.Clear();
                cmbAnnPriority.SelectedIndex = 0;
                cmbAnnTarget.SelectedIndex = 0;
                txtAnnSection.Clear();

                LoadAnnouncements();
            }
            catch (Exception ex)
            {
                Console.WriteLine("PostAnnouncement error: " + ex.Message);
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
                    $"Delete announcement \"{title}\"?",
                    "Confirm Delete",
                    CustomMessageBoxButtons.YesNo,
                    CustomMessageBoxIcon.Warning);
                if (confirm != CustomMessageBoxResult.Yes) return;

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
                    LoadAnnouncements();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Delete announcement error: " + ex.Message);
                }
            };
            card.Controls.Add(btnDeleteAnn);

            return card;
        }
    }
}