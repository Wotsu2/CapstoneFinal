using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinFormsApp1
{
    public partial class DashboardControl : UserControl
    {
        // ===== Stat cards =====
        private Label lblUsers;
        private Label lblStudents;
        private Label lblProfessors;
        private Label lblFiles;

        private Label lblActivityFeed;

        public DashboardControl()
        {
            BuildUi();
        }

        private void BuildUi()
        {
            this.BackColor = Color.FromArgb(245, 245, 248);
            this.Dock = DockStyle.Fill;

            // ---------- TOP: greeting ----------
            var topBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = Color.FromArgb(245, 245, 248),
                Padding = new Padding(20, 12, 20, 12)
            };
            this.Controls.Add(topBar);

            topBar.Controls.Add(new Label
            {
                Text = "📊  Dashboard",
                Font = new Font("Segoe UI Semibold", 20F, FontStyle.Bold),
                ForeColor = Color.Maroon,
                AutoSize = true,
                Location = new Point(20, 12)
            });

            var lblClock = new Label
            {
                Name = "lblClock",
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(120, 120, 120),
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            topBar.Controls.Add(lblClock);

            var clockTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            clockTimer.Tick += (s, e) =>
            {
                lblClock.Text = DateTime.Now.ToString("dddd, MMMM d, yyyy  •  hh:mm:ss tt");
                lblClock.Location = new Point(
                    Math.Max(20, topBar.ClientSize.Width - lblClock.Width - 20), 20);
            };
            clockTimer.Start();

            // ---------- STAT CARDS row ----------
            var statsPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 130,
                BackColor = Color.FromArgb(245, 245, 248),
                Padding = new Padding(20, 10, 20, 10)
            };
            this.Controls.Add(statsPanel);

            lblUsers = BuildStatCard(statsPanel, "👥  Users", Color.FromArgb(123, 15, 23), 20);
            lblStudents = BuildStatCard(statsPanel, "🎓  Students", Color.FromArgb(52, 120, 200), 210);
            lblProfessors = BuildStatCard(statsPanel, "👨‍🏫  Professors", Color.FromArgb(46, 160, 90), 400);
            lblFiles = BuildStatCard(statsPanel, "📁  Files", Color.FromArgb(220, 150, 30), 590);

            // ---------- Bottom: activity feed ----------
            var feedHolder = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(245, 245, 248),
                Padding = new Padding(20, 10, 20, 20)
            };
            this.Controls.Add(feedHolder);
            feedHolder.BringToFront();

            var feedCard = new Guna.UI2.WinForms.Guna2Panel
            {
                Dock = DockStyle.Fill,
                BorderRadius = 12,
                FillColor = Color.White,
                BorderColor = Color.FromArgb(230, 225, 225),
                BorderThickness = 1,
                Padding = new Padding(20)
            };
            feedHolder.Controls.Add(feedCard);

            feedCard.Controls.Add(new Label
            {
                Text = "🕒  Recent Activity",
                Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold),
                ForeColor = Color.Maroon,
                AutoSize = true,
                Location = new Point(20, 14)
            });

            var btnRefresh = new Guna.UI2.WinForms.Guna2Button
            {
                Text = "🔄 Refresh",
                Size = new Size(100, 32),
                BorderRadius = 8,
                FillColor = Color.FromArgb(234, 234, 234),
                ForeColor = Color.FromArgb(50, 50, 50),
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnRefresh.Click += (s, e) => RefreshAll();
            feedCard.Controls.Add(btnRefresh);
            feedCard.Resize += (s, e) =>
                btnRefresh.Location = new Point(feedCard.ClientSize.Width - btnRefresh.Width - 20, 12);
            btnRefresh.Location = new Point(feedCard.Width - btnRefresh.Width - 20, 12);

            lblActivityFeed = new Label
            {
                AutoSize = false,
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(70, 70, 70),
                BackColor = Color.Transparent,
                Location = new Point(20, 55),
                Size = new Size(feedCard.Width - 40, feedCard.Height - 70),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Text = "Loading…"
            };
            feedCard.Controls.Add(lblActivityFeed);

            // Auto-load once visible
            this.VisibleChanged += (s, e) =>
            {
                if (this.Visible) RefreshAll();
            };
        }

        private Label BuildStatCard(Panel parent, string title, Color accent, int x)
        {
            var card = new Guna.UI2.WinForms.Guna2Panel
            {
                Location = new Point(x, 10),
                Size = new Size(180, 100),
                BorderRadius = 12,
                FillColor = Color.White,
                BorderColor = Color.FromArgb(230, 225, 225),
                BorderThickness = 1
            };
            parent.Controls.Add(card);

            // colored left strip
            var strip = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(6, card.Height),
                BackColor = accent
            };
            card.Controls.Add(strip);

            card.Controls.Add(new Label
            {
                Text = title,
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(80, 80, 80),
                AutoSize = true,
                Location = new Point(16, 12)
            });

            var lblValue = new Label
            {
                Text = "—",
                Font = new Font("Segoe UI Semibold", 22F, FontStyle.Bold),
                ForeColor = accent,
                AutoSize = true,
                Location = new Point(16, 42)
            };
            card.Controls.Add(lblValue);

            return lblValue;
        }

        // =========================================================
        //  REFRESH
        // =========================================================
        public void RefreshAll()
        {
            LoadCountsAsync();
            LoadRecentActivityAsync();
        }

        private void LoadCountsAsync()
        {
            Task.Run(() =>
            {
                int users = 0, students = 0, professors = 0, files = 0;

                try
                {
                    string connStr = SettingsManager.Current.GetConnectionString();
                    using (var conn = new MySqlConnection(connStr))
                    {
                        conn.Open();

                        users = ScalarInt(conn, "SELECT COUNT(*) FROM user_credential");
                        students = ScalarInt(conn, "SELECT COUNT(*) FROM user_credential WHERE roles='Student'");
                        professors = ScalarInt(conn, "SELECT COUNT(*) FROM user_credential WHERE roles='Professor'");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("DashboardControl.LoadCountsAsync db: " + ex.Message);
                }

                try
                {
                    string root = SettingsManager.Current.SaveFolder;
                    if (!string.IsNullOrEmpty(root) && Directory.Exists(root))
                        files = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories).Count();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("DashboardControl.LoadCountsAsync files: " + ex.Message);
                }

                if (this.IsHandleCreated && !this.IsDisposed)
                {
                    this.BeginInvoke(new Action(() =>
                    {
                        lblUsers.Text = users.ToString();
                        lblStudents.Text = students.ToString();
                        lblProfessors.Text = professors.ToString();
                        lblFiles.Text = files.ToString();
                    }));
                }
            });
        }

        private int ScalarInt(MySqlConnection conn, string sql)
        {
            using (var cmd = new MySqlCommand(sql, conn))
            {
                object r = cmd.ExecuteScalar();
                return (r == null || r == DBNull.Value) ? 0 : Convert.ToInt32(r);
            }
        }

        private void LoadRecentActivityAsync()
        {
            Task.Run(() =>
            {
                var lines = new List<string>();

                try
                {
                    string connStr = SettingsManager.Current.GetConnectionString();
                    using (var conn = new MySqlConnection(connStr))
                    {
                        conn.Open();

                        // Newest users
                        using (var cmd = new MySqlCommand(@"
                            SELECT CONCAT(i.firstname, ' ', i.lastname) AS name,
                                   u.roles,
                                   u.user_id
                            FROM user_credential u
                            LEFT JOIN user_information i ON i.user_id = u.user_id
                            ORDER BY u.user_id DESC
                            LIMIT 5", conn))
                        using (var r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                string name = r["name"] == DBNull.Value ? "Unknown" : r["name"].ToString();
                                string role = r["roles"] == DBNull.Value ? "" : r["roles"].ToString();
                                lines.Add($"🟢  New {role}: {name}");
                            }
                        }

                        // Newest announcements
                        try
                        {
                            using (var cmd = new MySqlCommand(@"
                                SELECT title, posted_at
                                FROM announcements
                                WHERE is_deleted = 0
                                ORDER BY posted_at DESC
                                LIMIT 3", conn))
                            using (var r = cmd.ExecuteReader())
                            {
                                while (r.Read())
                                {
                                    string title = r["title"].ToString();
                                    DateTime at = Convert.ToDateTime(r["posted_at"]);
                                    lines.Add($"📢  Announcement: \"{title}\"  ({at:MMM dd hh:mm tt})");
                                }
                            }
                        }
                        catch { /* announcements table may not exist */ }

                        // Recent submissions
                        try
                        {
                            using (var cmd = new MySqlCommand(@"
                                SELECT student_name, title
                                FROM submitted_activity
                                ORDER BY user_id DESC
                                LIMIT 3", conn))
                            using (var r = cmd.ExecuteReader())
                            {
                                while (r.Read())
                                {
                                    string student = r["student_name"].ToString();
                                    string activity = r["title"].ToString();
                                    lines.Add($"📤  {student} submitted \"{activity}\"");
                                }
                            }
                        }
                        catch { }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("DashboardControl.LoadRecentActivityAsync: " + ex.Message);
                    lines.Add("⚠ Unable to load activity: " + ex.Message);
                }

                if (lines.Count == 0)
                    lines.Add("No recent activity yet.");

                string text = string.Join(Environment.NewLine + Environment.NewLine, lines);

                if (this.IsHandleCreated && !this.IsDisposed)
                {
                    this.BeginInvoke(new Action(() =>
                    {
                        lblActivityFeed.Text = text;
                    }));
                }
            });
        }
    }
}