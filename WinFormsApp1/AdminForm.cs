using Microsoft.VisualBasic.ApplicationServices;
using MySql.Data.MySqlClient;
using Org.BouncyCastle.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace WinFormsApp1
{
    public partial class AdminForm : Form
    {
        // USER DETAILS
        private Panel overlayPanel;
        private Panel userDetailsPanel;
        private PictureBox detailsPhoto;
        private Label detailsName;
        private Label detailsRoleBadge;
        private Label detailsUsername, detailsLastName, detailsFirstName,
                      detailsMiddleName, detailsRole, detailsYear,
                      detailsSection, detailsCourse;
        private Button detailsInfoTab, detailsHistoryTab;
        private Panel detailsInfoPage, detailsHistoryPage;

        // CONTEXT MENU
        private ContextMenuStrip userContextMenu;
        private int contextUserId = -1;

        // DASHBOARD
        private Panel PanelIndicator;

        // FILE MANAGEMENT
        private string currentFolder;
        private Stack<string> folderHistory = new Stack<string>();

        // WORKSTATION
        private TcpListener listener;
        private TcpListener fileListener;
        private int fileSubmittedCount = 0;
        private int WorkStationNum = 0;
        private Dictionary<string, Button> workstationButtons = new Dictionary<string, Button>();
        private Dictionary<string, PictureBox> screenViewers = new Dictionary<string, PictureBox>();
        private TcpListener screenListener;
        private PictureBox pictureBoxScreen;
        private string selectedWorkstationId = "";
        private volatile bool isRunning = false;

        // AUTH PHOTO / FILE TRANSFER LISTENER
        private TcpListener authPhotoListener;
        private volatile bool adminIsRunning = true;

        // BANNER MANAGEMENT
        private Guna.UI2.WinForms.Guna2Button btnUploadBanner;
        private Guna.UI2.WinForms.Guna2Button btnOpenBannersFolder;

        // DATABASE NAV BUTTON
        private Guna.UI2.WinForms.Guna2Button btnDatabaseNav;

        // ANNOUNCEMENTS
        private Guna.UI2.WinForms.Guna2Button btnAnnouncementNav;
        private Panel pnlAnnouncements;
        private TextBox txtAnnTitle;
        private TextBox txtAnnBody;
        private ComboBox cmbAnnPriority;
        private ComboBox cmbAnnTarget;
        private TextBox txtAnnSection;
        private Label lblAnnSectionLabel;
        private FlowLayoutPanel flpAnnouncements;
        private Guna.UI2.WinForms.Guna2Button btnDeleteFile;
        public AdminForm()
        {
            InitializeComponent();

            InitializeUserDetailsPanel();
            InitializeUserContextMenu();

            UserDataList.CellClick += UserDataList_CellClick;
            UserDataList.CellMouseDown += UserDataList_CellMouseDown;

            LoadUserData();
        }

        private void admindash_Load(object sender, EventArgs e)
        {
            panelDashoard.Visible = true;
            isRunning = true;
            adminIsRunning = true;

            lblTotalUsers.Text = TotalUsers().ToString();

            LoadUserData();

            StartServer();
            StartScreenListener();
            _ = StartAuthPhotoListener();

            InitializeBannerButtons();

            EnsureAnnouncementsTable();

            // Add the Database button to the nav strip
            InitializeDatabaseNavButton();

            // Add the Announcements button to the nav strip
            InitializeAnnouncementsNavButton();
            InitializeFileManagementButtons();
        }

        // =========================================================
        // NAV STRIP — 6 BUTTONS
        // =========================================================
        private void InitializeDatabaseNavButton()
        {
            try
            {
                if (guna2Panel2 == null) return;

                // 6 buttons across 1345px → ~224px each
                int newWidth = 224;
                int x = 0;

                if (btnDashboard != null)
                {
                    btnDashboard.Location = new Point(x, 0);
                    btnDashboard.Size = new Size(newWidth, 80);
                    x += newWidth;
                }

                if (btnUserManagement != null)
                {
                    btnUserManagement.Location = new Point(x, 0);
                    btnUserManagement.Size = new Size(newWidth, 80);
                    x += newWidth;
                }

                if (btnFileManagement != null)
                {
                    btnFileManagement.Location = new Point(x, 0);
                    btnFileManagement.Size = new Size(newWidth, 80);
                    x += newWidth;
                }

                if (btnWorkstation != null)
                {
                    btnWorkstation.Location = new Point(x, 0);
                    btnWorkstation.Size = new Size(newWidth, 80);
                    x += newWidth;
                }

                // Announcements button (5th)
                btnAnnouncementNav = new Guna.UI2.WinForms.Guna2Button
                {
                    Text = "Announcements",
                    Size = new Size(newWidth, 80),
                    Location = new Point(x, 0),
                    BorderRadius = 8,
                    FillColor = Color.FromArgb(234, 234, 234),
                    ForeColor = Color.FromArgb(123, 15, 23),
                    Font = new Font("Segoe UI", 13F),
                    Image = Properties.Resources.Announcement,
                    ImageSize = new Size(28, 28),
                    Animated = true,
                    Name = "btnAnnouncementNav",
                    Cursor = Cursors.Hand
                };
                btnAnnouncementNav.HoverState.FillColor = Color.FromArgb(250, 235, 235);
                btnAnnouncementNav.Click += (s, ev) => ShowAnnouncementsPage();
                guna2Panel2.Controls.Add(btnAnnouncementNav);
                btnAnnouncementNav.BringToFront();
                x += newWidth;

                // Database button (6th)
                btnDatabaseNav = new Guna.UI2.WinForms.Guna2Button
                {
                    Text = "Database",
                    Size = new Size(newWidth, 80),
                    Location = new Point(x, 0),
                    BorderRadius = 8,
                    FillColor = Color.FromArgb(234, 234, 234),
                    ForeColor = Color.FromArgb(123, 15, 23),
                    Font = new Font("Segoe UI", 13F),
                    Image = Properties.Resources.database,
                    ImageSize = new Size(28, 28),
                    Animated = true,
                    Name = "btnDatabaseNav",
                    Cursor = Cursors.Hand
                };
                btnDatabaseNav.HoverState.FillColor = Color.FromArgb(250, 235, 235);
                btnDatabaseNav.Click += (s, ev) =>
                {
                    var dbForm = new DatabaseManagerForm();
                    dbForm.ShowDialog(this);
                };
                guna2Panel2.Controls.Add(btnDatabaseNav);
                btnDatabaseNav.BringToFront();
            }
            catch (Exception ex)
            {
                Console.WriteLine("InitializeDatabaseNavButton error: " + ex.Message);
            }
        }

        // =========================================================
        // ANNOUNCEMENTS
        // =========================================================
        private void InitializeAnnouncementsNavButton()
        {
            // Actual button is created in InitializeDatabaseNavButton()
            // This method only builds the Announcements page panel.
            BuildAnnouncementsPanel();
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

        private void BuildAnnouncementsPanel()
        {
            if (pnlAnnouncements != null) return;

            pnlAnnouncements = new Panel
            {
                Location = new Point(138, 338),
                Size = new Size(1345, 590),
                BackColor = Color.White,
                Visible = false
            };
            this.Controls.Add(pnlAnnouncements);

            // ---------- LEFT: Compose ----------
            var leftCard = new Guna.UI2.WinForms.Guna2Panel
            {
                Location = new Point(20, 20),
                Size = new Size(500, 545),
                BorderRadius = 12,
                FillColor = Color.FromArgb(252, 248, 248),
                BorderColor = Color.FromArgb(230, 220, 220),
                BorderThickness = 1
            };
            pnlAnnouncements.Controls.Add(leftCard);

            var lblCompose = new Label
            {
                Text = "📢  Compose Announcement",
                Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold),
                ForeColor = Color.Maroon,
                AutoSize = true,
                Location = new Point(18, 14)
            };
            leftCard.Controls.Add(lblCompose);

            // Title
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
                BorderStyle = BorderStyle.FixedSingle
            };
            leftCard.Controls.Add(txtAnnTitle);

            // Body
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
                BorderStyle = BorderStyle.FixedSingle
            };
            leftCard.Controls.Add(txtAnnBody);

            // Priority
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

            // Target
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

            // Section filter
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

            // Post button
            var btnPost = new Guna.UI2.WinForms.Guna2Button
            {
                Text = "Post Announcement",
                Size = new Size(220, 44),
                Location = new Point(260, 425),
                BorderRadius = 10,
                FillColor = Color.Maroon,
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold)
            };
            btnPost.HoverState.FillColor = Color.FromArgb(100, 0, 0);
            btnPost.Click += (s, e) => PostAnnouncement();
            leftCard.Controls.Add(btnPost);

            // ---------- RIGHT: Recent ----------
            var rightCard = new Guna.UI2.WinForms.Guna2Panel
            {
                Location = new Point(540, 20),
                Size = new Size(785, 545),
                BorderRadius = 12,
                FillColor = Color.FromArgb(252, 248, 248),
                BorderColor = Color.FromArgb(230, 220, 220),
                BorderThickness = 1
            };
            pnlAnnouncements.Controls.Add(rightCard);

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
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold)
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
                BackColor = Color.Transparent
            };
            rightCard.Controls.Add(flpAnnouncements);

            LoadAnnouncements();
        }

        private void ShowAnnouncementsPage()
        {
            panelDashoard.Visible = false;
            pnlUserManagement.Visible = false;
            pnlFileManagement.Visible = false;
            pnlWorkstation.Visible = false;
            pnlAnnouncements.Visible = true;
            pnlAnnouncements.BringToFront();

            navbarStyle.RemoveIndicator(PanelIndicator);
            PanelIndicator = navbarStyle.CreateIndicator(btnAnnouncementNav);

            LoadAnnouncements();
        }

        private void PostAnnouncement()
        {
            string title = txtAnnTitle.Text.Trim();
            string body = txtAnnBody.Text.Trim();
            string priority = cmbAnnPriority.Text;
            string target = cmbAnnTarget.Text;
            string sectionFilter = cmbAnnSection();

            if (string.IsNullOrEmpty(title))
            {
                MessageBox.Show("Please enter a title.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrEmpty(body))
            {
                MessageBox.Show("Please enter a message.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (target == "Specific Section" && string.IsNullOrEmpty(sectionFilter))
            {
                MessageBox.Show("Please enter a section.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
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

                MessageBox.Show("Announcement posted!", "Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                txtAnnTitle.Clear();
                txtAnnBody.Clear();
                cmbAnnPriority.SelectedIndex = 0;
                cmbAnnTarget.SelectedIndex = 0;
                txtAnnSection.Clear();

                LoadAnnouncements();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to post announcement:\n" + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string cmbAnnSection()
        {
            return txtAnnSection.Text.Trim();
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
            return 1; // fallback
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

                // Left accent strip
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

            var btnDelete = new Guna.UI2.WinForms.Guna2Button
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
            btnDelete.HoverState.FillColor = Color.FromArgb(255, 240, 240);
            btnDelete.Click += (s, e) =>
            {
                var confirm = MessageBox.Show(
                    $"Delete announcement \"{title}\"?",
                    "Confirm Delete",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);
                if (confirm != DialogResult.Yes) return;

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
                    MessageBox.Show("Delete failed:\n" + ex.Message);
                }
            };
            card.Controls.Add(btnDelete);

            return card;
        }

        // =========================================================
        //  FILE RECEIVER
        // =========================================================
        private async Task StartAuthPhotoListener()
        {
            int port = SettingsManager.Current.FileTransferPort;

            try
            {
                authPhotoListener = new TcpListener(IPAddress.Any, port);
                authPhotoListener.Server.SetSocketOption(
                    SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                authPhotoListener.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Admin] FileTransfer bind FAILED: " + ex.Message);
                return;
            }

            while (adminIsRunning)
            {
                try
                {
                    TcpClient client = await authPhotoListener.AcceptTcpClientAsync();
                    _ = HandleIncomingFile(client);
                }
                catch (ObjectDisposedException) { break; }
                catch (Exception ex)
                {
                    if (!adminIsRunning) break;
                    Console.WriteLine("[Admin] FileTransfer accept error: " + ex.Message);
                }
            }
        }

        private async Task HandleIncomingFile(TcpClient client)
        {
            try
            {
                using (client)
                using (NetworkStream stream = client.GetStream())
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    string firstToken = reader.ReadString();

                    if (firstToken == "ACTIVITY_FILE")
                    {
                        string professorFolder = reader.ReadString();
                        string section = reader.ReadString();
                        string fileName = reader.ReadString();
                        int length = reader.ReadInt32();

                        if (length <= 0 || length > 200 * 1024 * 1024) return;

                        byte[] bytes = reader.ReadBytes(length);
                        professorFolder = SanitizeFolderName(professorFolder);
                        section = SanitizeFolderName(section);
                        fileName = SanitizeFolderName(fileName);

                        string root = SettingsManager.Current.SaveFolder;
                        string folder = Path.Combine(root, professorFolder, section, "ActivityFiles");
                        Directory.CreateDirectory(folder);

                        await File.WriteAllBytesAsync(Path.Combine(folder, fileName), bytes);
                        return;
                    }

                    if (firstToken == "STUDENT_SUBMISSION")
                    {
                        string professorFolder = reader.ReadString();
                        string section = reader.ReadString();
                        string studentName = reader.ReadString();
                        string title = reader.ReadString();
                        string fileName = reader.ReadString();
                        int length = reader.ReadInt32();

                        if (length <= 0 || length > 200 * 1024 * 1024) return;

                        byte[] bytes = reader.ReadBytes(length);
                        professorFolder = SanitizeFolderName(professorFolder);
                        section = SanitizeFolderName(section);
                        studentName = SanitizeFolderName(studentName);
                        title = SanitizeFolderName(title);
                        fileName = SanitizeFolderName(fileName);

                        string root = SettingsManager.Current.SaveFolder;
                        string folder = Path.Combine(root, professorFolder, section, "Submissions");
                        Directory.CreateDirectory(folder);

                        string finalName = SanitizeFolderName($"{studentName}_{title}_{fileName}");
                        string localPath = Path.Combine(folder, finalName);
                        await File.WriteAllBytesAsync(localPath, bytes);

                        string uncPath = ToUnc(localPath);
                        try
                        {
                            using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
                            {
                                writer.Write(uncPath);
                                writer.Flush();
                            }
                        }
                        catch { }
                        return;
                    }

                    if (firstToken == "PROFILE_PHOTO")
                    {
                        string username = reader.ReadString();
                        string fileName = reader.ReadString();
                        int length = reader.ReadInt32();
                        if (length <= 0 || length > 20 * 1024 * 1024) return;

                        byte[] bytes = reader.ReadBytes(length);
                        username = SanitizeFolderName(username);
                        fileName = SanitizeFolderName(fileName);

                        string root = SettingsManager.Current.SaveFolder;
                        string folder = Path.Combine(root, "ProfilePictures");
                        Directory.CreateDirectory(folder);

                        string finalName = $"{username}_{fileName}";
                        string savePath = Path.Combine(folder, finalName);
                        await File.WriteAllBytesAsync(savePath, bytes);

                        string uncPath = ToUnc(savePath);
                        try
                        {
                            using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
                            {
                                writer.Write(uncPath);
                                writer.Flush();
                            }
                        }
                        catch { }
                        return;
                    }

                    // AUTH PHOTO (default)
                    string authFileName = SanitizeFolderName(firstToken);
                    int authLength = reader.ReadInt32();
                    if (authLength <= 0 || authLength > 20 * 1024 * 1024) return;

                    byte[] authBytes = reader.ReadBytes(authLength);

                    string authRoot = SettingsManager.Current.SaveFolder;
                    string authSub = SettingsManager.Current.AuthPhotoSubfolder ?? "";
                    string authFolder = Path.Combine(authRoot, authSub);
                    Directory.CreateDirectory(authFolder);

                    await File.WriteAllBytesAsync(Path.Combine(authFolder, authFileName), authBytes);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("HandleIncomingFile error: " + ex.Message);
            }
        }

        private string ToUnc(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path)) return path;
                if (path.StartsWith(@"\\")) return path;

                string localRoot = SettingsManager.Current.SaveFolder ?? "";
                string sharedName = new DirectoryInfo(localRoot).Name;
                string ip = CleanIp(SettingsManager.Current.ServerIp);

                string relative = path;
                if (!string.IsNullOrEmpty(localRoot) &&
                    path.StartsWith(localRoot, StringComparison.OrdinalIgnoreCase))
                {
                    relative = path.Substring(localRoot.Length).TrimStart('\\', '/');
                }

                return $@"\\{ip}\{sharedName}\{relative}";
            }
            catch { return path; }
        }

        private string CleanIp(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return GetLocalLanIp();

            string s = raw.Trim().Replace("(null)", "").TrimStart('\\').TrimEnd('\\');
            int slash = s.IndexOf('\\');
            if (slash > 0) s = s.Substring(0, slash);

            return string.IsNullOrWhiteSpace(s) ? GetLocalLanIp() : s;
        }

        private string GetLocalLanIp()
        {
            try
            {
                var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
                foreach (var addr in host.AddressList)
                {
                    if (addr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        return addr.ToString();
                }
            }
            catch { }
            return "127.0.0.1";
        }

        private string SanitizeFolderName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "Unknown";
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            name = name.Trim().TrimEnd('.');
            return string.IsNullOrWhiteSpace(name) ? "Unknown" : name;
        }

        // =========================================================
        //  BANNER MANAGEMENT
        // =========================================================
        private void InitializeBannerButtons()
        {
            btnUploadBanner = new Guna.UI2.WinForms.Guna2Button
            {
                Text = "Upload Banner",
                Size = new Size(180, 45),
                BorderRadius = 10,
                FillColor = Color.Maroon,
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Location = new Point(1250, 150)
            };
            btnUploadBanner.Click += BtnUploadBanner_Click;
            this.Controls.Add(btnUploadBanner);
            btnUploadBanner.BringToFront();

            btnOpenBannersFolder = new Guna.UI2.WinForms.Guna2Button
            {
                Text = "Open Folder",
                Size = new Size(140, 45),
                BorderRadius = 10,
                FillColor = Color.Gray,
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Location = new Point(btnUploadBanner.Left - 150, btnUploadBanner.Top)
            };
            btnOpenBannersFolder.Click += BtnOpenBannersFolder_Click;
            this.Controls.Add(btnOpenBannersFolder);
            btnOpenBannersFolder.BringToFront();
        }

        private void BtnUploadBanner_Click(object sender, EventArgs e)
        {
            string folder = BannerHelper.GetBannersFolder();
            if (string.IsNullOrEmpty(folder))
            {
                MessageBox.Show("The Banners folder could not be located.", "Root Folder Not Set",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif";
                ofd.Multiselect = true;

                if (ofd.ShowDialog() != DialogResult.OK) return;

                int copied = 0;
                foreach (string source in ofd.FileNames)
                {
                    try
                    {
                        string fileName = Path.GetFileName(source);
                        string dest = Path.Combine(folder, fileName);

                        if (File.Exists(dest))
                        {
                            string ext = Path.GetExtension(source);
                            string baseName = Path.GetFileNameWithoutExtension(source);
                            string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                            dest = Path.Combine(folder, $"{baseName}_{stamp}{ext}");
                        }

                        File.Copy(source, dest);
                        copied++;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Failed: " + ex.Message);
                    }
                }

                if (copied > 0)
                    MessageBox.Show($"{copied} banner(s) uploaded.", "Upload Complete",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnOpenBannersFolder_Click(object sender, EventArgs e)
        {
            string folder = BannerHelper.GetBannersFolder();
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
            {
                MessageBox.Show("The Banners folder does not exist yet.", "Folder Not Found",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try { System.Diagnostics.Process.Start("explorer.exe", "\"" + folder + "\""); }
            catch (Exception ex) { MessageBox.Show("Could not open folder:\n" + ex.Message); }
        }

        // =========================================================
        //  NAVIGATION
        // =========================================================
        private void btnDashboard_Click_1(object sender, EventArgs e)
        {
            panelDashoard.Visible = true;
            pnlUserManagement.Visible = false;
            pnlFileManagement.Visible = false;
            pnlWorkstation.Visible = false;
            if (pnlAnnouncements != null) pnlAnnouncements.Visible = false;
            navbarStyle.RemoveIndicator(PanelIndicator);
            PanelIndicator = navbarStyle.CreateIndicator(btnDashboard);
        }

        private void btnUserManagement_Click(object sender, EventArgs e)
        {
            pnlUserManagement.Visible = true;
            panelDashoard.Visible = false;
            pnlFileManagement.Visible = false;
            pnlWorkstation.Visible = false;
            if (pnlAnnouncements != null) pnlAnnouncements.Visible = false;
            navbarStyle.RemoveIndicator(PanelIndicator);
            PanelIndicator = navbarStyle.CreateIndicator(btnUserManagement);
            LoadUserData();
        }

        private void btnFileManagement_Click(object sender, EventArgs e)
        {
            pnlFileManagement.Visible = true;
            panelDashoard.Visible = false;
            pnlUserManagement.Visible = false;
            pnlWorkstation.Visible = false;
            if (pnlAnnouncements != null) pnlAnnouncements.Visible = false;
            navbarStyle.RemoveIndicator(PanelIndicator);
            PanelIndicator = navbarStyle.CreateIndicator(btnFileManagement);

            string root = SettingsManager.Current.SaveFolder;
            if (!Directory.Exists(root))
            {
                MessageBox.Show("Root save folder is not configured or does not exist:\n" + root,
                    "Folder Missing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            lsServerFolderSetup();
            LoadServerFolder(root, addToHistory: false);
        }

        private void btnWorkstation_Click(object sender, EventArgs e)
        {
            pnlWorkstation.Visible = true;
            panelDashoard.Visible = false;
            pnlUserManagement.Visible = false;
            pnlFileManagement.Visible = false;
            if (pnlAnnouncements != null) pnlAnnouncements.Visible = false;
            navbarStyle.RemoveIndicator(PanelIndicator);
            PanelIndicator = navbarStyle.CreateIndicator(btnWorkstation);
        }

        private void CreateButton_Click(object sender, EventArgs e) => CreateUser();

        private void ContextRoleText_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool student = ContextRoleText.Text == "Student";
            ContextYearText.Enabled = student;
            ContextSectionText.Enabled = student;
            ContextCourseText.Enabled = student;
        }

        private void cmbSelection_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (cmbSelection.Text)
            {
                case "Users":
                    LoadUserData();
                    pnlUserList.BringToFront();
                    break;
                case "Create Account":
                    pnlCreateAccount.BringToFront();
                    break;
            }
        }

        private void SearchButton_TextChanged(object sender, EventArgs e) => LoadUserData(SearchButton.Text);
        private void lvServerFolder_DoubleClick(object sender, EventArgs e) => doubleClick();
        private void BtnBack_Click(object sender, EventArgs e) => btnBack();

        // =========================================================
        //  USER DETAILS MODAL
        // =========================================================
        private void InitializeUserDetailsPanel()
        {
            overlayPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(120, 0, 0, 0),
                Visible = false
            };
            overlayPanel.Click += (s, e) => HideUserDetails();
            this.Controls.Add(overlayPanel);
            overlayPanel.BringToFront();

            userDetailsPanel = new Panel
            {
                Size = new Size(680, 500),
                BackColor = Color.White,
                Visible = false,
                BorderStyle = BorderStyle.FixedSingle
            };

            int modalWidth = userDetailsPanel.Width;

            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 160,
                BackColor = Color.FromArgb(13, 71, 161)
            };
            header.Width = modalWidth;

            var btnClose = new Button
            {
                Text = "✕",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(30, 30, 30),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(36, 36),
                Location = new Point(16, 16),
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => HideUserDetails();

            detailsRoleBadge = new Label
            {
                Text = "Student",
                ForeColor = Color.White,
                BackColor = Color.FromArgb(20, 20, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(90, 26),
                Location = new Point(modalWidth - 110, 20),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            detailsName = new Label
            {
                Text = "Full Name",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(210, 105),
                BackColor = Color.Transparent
            };

            header.Controls.Add(btnClose);
            header.Controls.Add(detailsRoleBadge);
            header.Controls.Add(detailsName);

            detailsPhoto = new PictureBox
            {
                Size = new Size(120, 120),
                Location = new Point(60, 100),
                BackColor = Color.FromArgb(0, 188, 212),
                SizeMode = PictureBoxSizeMode.Zoom
            };
            detailsPhoto.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var path = new GraphicsPath())
                {
                    path.AddEllipse(0, 0, detailsPhoto.Width - 1, detailsPhoto.Height - 1);
                    detailsPhoto.Region = new Region(path);
                }
            };

            detailsInfoTab = new Button
            {
                Text = "Info",
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Size = new Size(80, 32),
                Location = new Point(270, 175),
                Cursor = Cursors.Hand
            };
            detailsHistoryTab = new Button
            {
                Text = "History",
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 9F),
                Size = new Size(100, 32),
                Location = new Point(352, 175),
                Cursor = Cursors.Hand
            };

            detailsInfoPage = new Panel
            {
                Location = new Point(0, 217),
                Size = new Size(modalWidth, 278),
                BackColor = Color.White,
                AutoScroll = true
            };

            int y = 15;
            detailsUsername = MakeInfoRow(detailsInfoPage, "Username:", ref y);
            detailsLastName = MakeInfoRow(detailsInfoPage, "Last Name:", ref y);
            detailsFirstName = MakeInfoRow(detailsInfoPage, "First Name:", ref y);
            detailsMiddleName = MakeInfoRow(detailsInfoPage, "Middle name:", ref y);
            detailsRole = MakeInfoRow(detailsInfoPage, "Role:", ref y);
            detailsYear = MakeInfoRow(detailsInfoPage, "Year:", ref y);
            detailsSection = MakeInfoRow(detailsInfoPage, "Section:", ref y);
            detailsCourse = MakeInfoRow(detailsInfoPage, "Course:", ref y);

            detailsHistoryPage = new Panel
            {
                Location = new Point(0, 217),
                Size = new Size(modalWidth, 278),
                BackColor = Color.White,
                Visible = false,
                AutoScroll = true
            };

            var historyCard = new Panel
            {
                Location = new Point(30, 20),
                Size = new Size(400, 70),
                BackColor = Color.FromArgb(245, 245, 245),
                BorderStyle = BorderStyle.FixedSingle
            };
            historyCard.Controls.Add(new Label
            {
                Text = "🕒  Account History",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Location = new Point(10, 10),
                AutoSize = true
            });
            historyCard.Controls.Add(new Label
            {
                Text = "Create account last August 30, 2026",
                Font = new Font("Segoe UI", 8F),
                Location = new Point(10, 36),
                AutoSize = true
            });
            detailsHistoryPage.Controls.Add(historyCard);

            detailsInfoTab.Click += (s, e) =>
            {
                detailsInfoPage.Visible = true;
                detailsHistoryPage.Visible = false;
                detailsInfoTab.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                detailsHistoryTab.Font = new Font("Segoe UI", 9F);
            };
            detailsHistoryTab.Click += (s, e) =>
            {
                detailsInfoPage.Visible = false;
                detailsHistoryPage.Visible = true;
                detailsInfoTab.Font = new Font("Segoe UI", 9F);
                detailsHistoryTab.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            };

            userDetailsPanel.Controls.Add(detailsInfoPage);
            userDetailsPanel.Controls.Add(detailsHistoryPage);
            userDetailsPanel.Controls.Add(detailsInfoTab);
            userDetailsPanel.Controls.Add(detailsHistoryTab);
            userDetailsPanel.Controls.Add(header);
            userDetailsPanel.Controls.Add(detailsPhoto);
            detailsPhoto.BringToFront();

            this.Controls.Add(userDetailsPanel);
            userDetailsPanel.BringToFront();
        }

        private Label MakeInfoRow(Panel parent, string label, ref int y)
        {
            var lbl = new Label
            {
                Text = label,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.Black,
                Location = new Point(60, y),
                AutoSize = true
            };
            var val = new Label
            {
                Text = "-",
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = Color.DimGray,
                Location = new Point(220, y),
                AutoSize = false,
                Size = new Size(420, 22)
            };
            parent.Controls.Add(lbl);
            parent.Controls.Add(val);
            y += 32;
            return val;
        }

        private void ShowUserDetails()
        {
            overlayPanel.Visible = true;
            overlayPanel.BringToFront();

            userDetailsPanel.Visible = true;
            userDetailsPanel.BringToFront();
            CenterUserDetailsPanel();
        }

        private void HideUserDetails()
        {
            userDetailsPanel.Visible = false;
            overlayPanel.Visible = false;
        }

        private void CenterUserDetailsPanel()
        {
            if (userDetailsPanel == null) return;
            Control parent = UserDataList.Parent ?? this;
            Point screenPt = parent.PointToScreen(Point.Empty);
            Point formPt = this.PointToClient(screenPt);
            userDetailsPanel.Left = formPt.X + (parent.ClientSize.Width - userDetailsPanel.Width) / 2;
            userDetailsPanel.Top = formPt.Y + (parent.ClientSize.Height - userDetailsPanel.Height) / 2;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (userDetailsPanel != null && userDetailsPanel.Visible)
                CenterUserDetailsPanel();
        }

        private void UserDataList_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var row = UserDataList.Rows[e.RowIndex];

            string fullName = $"{SafeCell(row, "firstname")} {SafeCell(row, "middlename")} {SafeCell(row, "lastname")}".Trim();
            detailsName.Text = string.IsNullOrWhiteSpace(fullName) ? "Unknown" : fullName;

            string role = SafeCell(row, "roles");
            detailsRoleBadge.Text = string.IsNullOrEmpty(role) ? "User" : role;

            detailsUsername.Text = SafeCell(row, "username");
            detailsLastName.Text = SafeCell(row, "lastname");
            detailsFirstName.Text = SafeCell(row, "firstname");
            detailsMiddleName.Text = SafeCell(row, "middlename");
            detailsRole.Text = SafeCell(row, "roles");
            detailsYear.Text = SafeCell(row, "school_year");
            detailsSection.Text = SafeCell(row, "school_section");
            detailsCourse.Text = SafeCell(row, "school_course");

            detailsPhoto.Image = LoadUserPhoto(row);

            detailsInfoPage.Visible = true;
            detailsHistoryPage.Visible = false;
            detailsInfoTab.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            detailsHistoryTab.Font = new Font("Segoe UI", 9F);

            ShowUserDetails();
        }

        private string SafeCell(DataGridViewRow row, string col)
        {
            if (!UserDataList.Columns.Contains(col)) return "";
            var v = row.Cells[col].Value;
            return (v == null || v == DBNull.Value) ? "" : v.ToString();
        }

        private Image LoadUserPhoto(DataGridViewRow row)
        {
            try
            {
                if (UserDataList.Columns.Contains("profile_picture"))
                {
                    object raw = row.Cells["profile_picture"].Value;

                    byte[] bytes = raw as byte[];
                    if (bytes != null && bytes.Length > 0)
                    {
                        using (var ms = new MemoryStream(bytes))
                        {
                            var temp = Image.FromStream(ms);
                            return new Bitmap(temp);
                        }
                    }

                    string path = raw as string;
                    if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                    {
                        byte[] fileBytes = File.ReadAllBytes(path);
                        using (var ms = new MemoryStream(fileBytes))
                        {
                            var temp = Image.FromStream(ms);
                            return new Bitmap(temp);
                        }
                    }
                }
            }
            catch { }

            return MakePlaceholderAvatar();
        }

        private Image MakePlaceholderAvatar()
        {
            var bmp = new Bitmap(120, 120);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.FromArgb(0, 188, 212));
                using (var b = new SolidBrush(Color.White))
                {
                    g.FillEllipse(b, 40, 20, 42, 42);
                    g.FillPie(b, 22, 66, 76, 76, 180, 180);
                }
            }
            return bmp;
        }

        // =========================================================
        //  CONTEXT MENU
        // =========================================================
        private void InitializeUserContextMenu()
        {
            userContextMenu = new ContextMenuStrip();

            var editItem = new ToolStripMenuItem("Edit Info");
            var deleteItem = new ToolStripMenuItem("Delete Info");
            var resetItem = new ToolStripMenuItem("Reset Password");
            var bulkSecItem = new ToolStripMenuItem("Update Section (Bulk)");
            var bulkYrItem = new ToolStripMenuItem("Update Year (Bulk)");
            var bulkSemItem = new ToolStripMenuItem("Update Semester (Bulk)");

            editItem.Click += ContextEdit_Click;
            deleteItem.Click += ContextDelete_Click;
            resetItem.Click += ContextResetPassword_Click;
            bulkSecItem.Click += ContextBulkSection_Click;
            bulkYrItem.Click += ContextBulkYear_Click;
            bulkSemItem.Click += ContextBulkSemester_Click;

            userContextMenu.Items.Add(editItem);
            userContextMenu.Items.Add(deleteItem);
            userContextMenu.Items.Add(new ToolStripSeparator());
            userContextMenu.Items.Add(resetItem);
            userContextMenu.Items.Add(new ToolStripSeparator());
            userContextMenu.Items.Add(bulkYrItem);
            userContextMenu.Items.Add(bulkSecItem);
            userContextMenu.Items.Add(bulkSemItem);

            UserDataList.ContextMenuStrip = userContextMenu;
        }

        private void UserDataList_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right || e.RowIndex < 0) return;

            if (!UserDataList.Rows[e.RowIndex].Selected)
            {
                UserDataList.ClearSelection();
                UserDataList.Rows[e.RowIndex].Selected = true;
            }

            var row = UserDataList.Rows[e.RowIndex];
            contextUserId = (UserDataList.Columns.Contains("user_id") &&
                             row.Cells["user_id"].Value != null &&
                             row.Cells["user_id"].Value != DBNull.Value)
                ? Convert.ToInt32(row.Cells["user_id"].Value)
                : -1;
        }

        private void ContextEdit_Click(object sender, EventArgs e)
        {
            if (contextUserId < 0) { MessageBox.Show("No user selected."); return; }

            string lastName = "", firstName = "", middleName = "",
                   email = "", year = "", section = "", course = "", username = "";

            string connStr = SettingsManager.Current.GetConnectionString();
            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string q = @"SELECT u.username, i.lastname, i.firstname, i.middlename,
                                        i.email, i.school_year, i.school_section, i.school_course
                                 FROM user_credential u
                                 LEFT JOIN user_information i ON u.user_id = i.user_id
                                 WHERE u.user_id = @id";
                    using (var cmd = new MySqlCommand(q, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", contextUserId);
                        using (var r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                username = r["username"].ToString();
                                lastName = r["lastname"].ToString();
                                firstName = r["firstname"].ToString();
                                middleName = r["middlename"].ToString();
                                email = r["email"].ToString();
                                year = r["school_year"].ToString();
                                section = r["school_section"].ToString();
                                course = r["school_course"].ToString();
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Error loading user: " + ex.Message); return; }

            using (var dlg = new Form())
            {
                dlg.Text = "Edit User Info";
                dlg.Size = new Size(420, 460);
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;

                int top = 20;
                Label MakeLabel(string t) => new Label { Text = t, Left = 20, Top = top, Width = 120 };
                TextBox MakeBox(string val)
                {
                    var tb = new TextBox { Left = 150, Top = top - 3, Width = 230, Text = val };
                    top += 34;
                    return tb;
                }

                var lblUser = MakeLabel("Username:"); var tbUser = MakeBox(username);
                var lblLast = MakeLabel("Last Name:"); var tbLast = MakeBox(lastName);
                var lblFirst = MakeLabel("First Name:"); var tbFirst = MakeBox(firstName);
                var lblMid = MakeLabel("Middle Name:"); var tbMid = MakeBox(middleName);
                var lblMail = MakeLabel("Email:"); var tbMail = MakeBox(email);
                var lblYear = MakeLabel("Year:"); var tbYear = MakeBox(year);
                var lblSec = MakeLabel("Section:"); var tbSec = MakeBox(section);
                var lblCourse = MakeLabel("Course:"); var tbCourse = MakeBox(course);

                tbUser.Enabled = false;

                var btnSave = new Button { Text = "Save", Left = 220, Top = top + 10, Width = 80, DialogResult = DialogResult.OK };
                var btnCancel = new Button { Text = "Cancel", Left = 306, Top = top + 10, Width = 80, DialogResult = DialogResult.Cancel };

                dlg.Controls.AddRange(new Control[]
                {
                    lblUser, tbUser, lblLast, tbLast, lblFirst, tbFirst, lblMid, tbMid,
                    lblMail, tbMail, lblYear, tbYear, lblSec, tbSec, lblCourse, tbCourse,
                    btnSave, btnCancel
                });
                dlg.AcceptButton = btnSave;
                dlg.CancelButton = btnCancel;

                if (dlg.ShowDialog(this) != DialogResult.OK) return;

                try
                {
                    using (var conn = new MySqlConnection(connStr))
                    {
                        conn.Open();
                        string q = @"UPDATE user_information
                                     SET lastname = @ln, firstname = @fn, middlename = @mn,
                                         email = @em, school_year = @yr,
                                         school_section = @sec, school_course = @cr
                                     WHERE user_id = @id";
                        using (var cmd = new MySqlCommand(q, conn))
                        {
                            cmd.Parameters.AddWithValue("@ln", tbLast.Text.Trim().ToUpper());
                            cmd.Parameters.AddWithValue("@fn", tbFirst.Text.Trim().ToUpper());
                            cmd.Parameters.AddWithValue("@mn", tbMid.Text.Trim().ToUpper());
                            cmd.Parameters.AddWithValue("@em", tbMail.Text.Trim());
                            cmd.Parameters.AddWithValue("@yr", tbYear.Text.Trim().ToUpper());
                            cmd.Parameters.AddWithValue("@sec", tbSec.Text.Trim().ToUpper());
                            cmd.Parameters.AddWithValue("@cr", tbCourse.Text.Trim().ToUpper());
                            cmd.Parameters.AddWithValue("@id", contextUserId);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    MessageBox.Show("User info updated.");
                    LoadUserData();
                }
                catch (Exception ex) { MessageBox.Show("Error updating user: " + ex.Message); }
            }
        }

        private void ContextDelete_Click(object sender, EventArgs e)
        {
            if (contextUserId < 0) { MessageBox.Show("No user selected."); return; }

            var confirm = MessageBox.Show("Are you sure you want to delete this user?",
                "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm != DialogResult.Yes) return;

            string connStr = SettingsManager.Current.GetConnectionString();
            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string[] deletes =
                    {
                        "DELETE FROM mainfolderpath      WHERE user_id = @id",
                        "DELETE FROM professor_attendance WHERE student_id = @id",
                        "DELETE FROM user_information    WHERE user_id = @id",
                        "DELETE FROM user_credential     WHERE user_id = @id"
                    };
                    foreach (var q in deletes)
                    {
                        using (var cmd = new MySqlCommand(q, conn))
                        {
                            cmd.Parameters.AddWithValue("@id", contextUserId);
                            try { cmd.ExecuteNonQuery(); } catch { }
                        }
                    }
                }
                MessageBox.Show("User deleted.");
                LoadUserData();
            }
            catch (Exception ex) { MessageBox.Show("Error deleting user: " + ex.Message); }
        }

        private void ContextResetPassword_Click(object sender, EventArgs e)
        {
            if (contextUserId < 0) { MessageBox.Show("No user selected."); return; }

            var confirm = MessageBox.Show("Reset password to '12345678'?",
                "Confirm Reset", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            try
            {
                string connStr = SettingsManager.Current.GetConnectionString();
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand(
                        "UPDATE user_credential SET p_word = @pw WHERE user_id = @id", conn))
                    {
                        cmd.Parameters.AddWithValue("@pw", "12345678");
                        cmd.Parameters.AddWithValue("@id", contextUserId);
                        cmd.ExecuteNonQuery();
                    }
                }
                MessageBox.Show("Password reset to default: 12345678");
            }
            catch (Exception ex) { MessageBox.Show("Error resetting password: " + ex.Message); }
        }

        // =========================================================
        //  BULK UPDATE
        // =========================================================
        private void ContextBulkSection_Click(object sender, EventArgs e)
            => BulkUpdateField("school_section", "Section", "e.g. 4-1");

        private void ContextBulkYear_Click(object sender, EventArgs e)
            => BulkUpdateField("school_year", "Year", "e.g. 4TH YEAR");

        private void ContextBulkSemester_Click(object sender, EventArgs e)
            => BulkUpdateField("school_semester", "Semester", "e.g. 1ST SEMESTER / 2ND SEMESTER");

        private void BulkUpdateField(string columnName, string displayName, string hint)
        {
            var ids = GetSelectedUserIds();
            if (ids.Count == 0)
            {
                MessageBox.Show("No rows selected. Hold Ctrl or Shift and click multiple rows first.");
                return;
            }

            string newValue = Prompt($"Enter the new {displayName} for {ids.Count} selected user(s).\n({hint})", "");
            if (string.IsNullOrWhiteSpace(newValue)) return;

            newValue = newValue.Trim().ToUpper();

            var confirm = MessageBox.Show(
                $"Update {displayName} of {ids.Count} user(s) to \"{newValue}\"?",
                "Confirm Bulk Update", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;

            try
            {
                string connStr = SettingsManager.Current.GetConnectionString();
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    var paramNames = new List<string>();
                    for (int i = 0; i < ids.Count; i++)
                        paramNames.Add("@id" + i);

                    string q = $"UPDATE user_information SET {columnName} = @newVal " +
                               $"WHERE user_id IN ({string.Join(",", paramNames)})";

                    using (var cmd = new MySqlCommand(q, conn))
                    {
                        cmd.Parameters.AddWithValue("@newVal", newValue);
                        for (int i = 0; i < ids.Count; i++)
                            cmd.Parameters.AddWithValue(paramNames[i], ids[i]);

                        int rows = cmd.ExecuteNonQuery();
                        MessageBox.Show($"{rows} user(s) updated.");
                    }
                }
                LoadUserData();
            }
            catch (Exception ex) { MessageBox.Show("Error during bulk update: " + ex.Message); }
        }

        private List<int> GetSelectedUserIds()
        {
            var list = new List<int>();
            if (!UserDataList.Columns.Contains("user_id")) return list;

            foreach (DataGridViewRow row in UserDataList.SelectedRows)
            {
                if (row.Cells["user_id"].Value != null &&
                    row.Cells["user_id"].Value != DBNull.Value)
                {
                    list.Add(Convert.ToInt32(row.Cells["user_id"].Value));
                }
            }
            return list;
        }

        private string Prompt(string label, string defaultValue)
        {
            using (var frm = new Form())
            {
                frm.Text = "Input";
                frm.Width = 420;
                frm.Height = 160;
                frm.StartPosition = FormStartPosition.CenterParent;
                frm.FormBorderStyle = FormBorderStyle.FixedDialog;
                frm.MinimizeBox = false;
                frm.MaximizeBox = false;

                var lbl = new Label { Text = label, Left = 12, Top = 12, Width = 380, Height = 40 };
                var txt = new TextBox { Text = defaultValue ?? "", Left = 12, Top = 56, Width = 380 };
                var ok = new Button { Text = "OK", Left = 230, Top = 90, Width = 75, DialogResult = DialogResult.OK };
                var cancel = new Button { Text = "Cancel", Left = 316, Top = 90, Width = 75, DialogResult = DialogResult.Cancel };

                frm.Controls.Add(lbl);
                frm.Controls.Add(txt);
                frm.Controls.Add(ok);
                frm.Controls.Add(cancel);
                frm.AcceptButton = ok;
                frm.CancelButton = cancel;

                return frm.ShowDialog() == DialogResult.OK ? txt.Text : null;
            }
        }

        // =========================================================
        //  USER MANAGEMENT
        // =========================================================
        private void LoadUserData(string filter = "")
        {
            string connStr = SettingsManager.Current.GetConnectionString();
            UserDataList.ReadOnly = true;
            UserDataList.MultiSelect = true;
            UserDataList.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    string query = @"SELECT u.user_id, u.username, u.roles, u.user_status, u.profile_picture,
                                            i.lastname, i.firstname, i.middlename,
                                            i.email, i.school_year, i.school_section,
                                            i.school_semester, i.school_course
                                     FROM user_credential u
                                     LEFT JOIN user_information i ON u.user_id = i.user_id";

                    if (!string.IsNullOrEmpty(filter))
                        query += " WHERE u.user_id LIKE @f1 OR i.lastname LIKE @f2 OR i.firstname LIKE @f3";

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
                            UserDataList.DataSource = dt;

                            if (UserDataList.Columns.Contains("user_id"))
                                UserDataList.Columns["user_id"].Visible = false;

                            if (UserDataList.Columns.Contains("profile_picture"))
                                UserDataList.Columns["profile_picture"].Visible = false;

                            UserDataList.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                        }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        private static int TotalUsers()
        {
            try
            {
                string connStr = SettingsManager.Current.GetConnectionString();
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM user_credential", conn))
                        return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); return 0; }
        }

        // =========================================================
        //  CREATE ACCOUNT
        // =========================================================
        string semester;

        private void CreateUser()
        {
            string connStr = SettingsManager.Current.GetConnectionString();

            if (string.IsNullOrEmpty(LastnameText.Text) ||
                string.IsNullOrEmpty(FirstnameText.Text) ||
                string.IsNullOrEmpty(ContextRoleText.Text) ||
                string.IsNullOrEmpty(EmailText.Text))
            {
                MessageBox.Show("Please fill in at least: Role, Last Name, First Name, and Email.");
                return;
            }

            semester = ContextRoleText.Text == "Student" ? "1st Semester" : "Null";

            string username = GenerateUsername(LastnameText.Text, FirstnameText.Text, MiddlenameText.Text, connStr);
            const string defaultPassword = "12345678";

            try
            {
                long userId;

                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    string Insertquery2 = @"
                        INSERT INTO user_credential (username, p_word, roles, user_status, authentication_condition)
                        VALUES (@Uname, @Password, @UserRole, @Status, @authentication_condition);
                        SELECT LAST_INSERT_ID();";

                    using (MySqlCommand cmd2 = new MySqlCommand(Insertquery2, conn))
                    {
                        cmd2.Parameters.AddWithValue("@Uname", username);
                        cmd2.Parameters.AddWithValue("@Password", defaultPassword);
                        cmd2.Parameters.AddWithValue("@UserRole", ContextRoleText.Text.Trim());
                        cmd2.Parameters.AddWithValue("@Status", "Active");
                        cmd2.Parameters.AddWithValue("@authentication_condition", "Disabled");
                        userId = Convert.ToInt64(cmd2.ExecuteScalar());
                    }

                    string Insertquery = @"
                        INSERT INTO user_information 
                            (user_id, lastname, firstname, middlename, email, school_year, school_section, school_semester, school_course) 
                        VALUES 
                            (@user_id, @lastname, @firstname, @middlename, @email, @school_year, @school_section, @school_semester, @school_course)";

                    using (MySqlCommand cmd = new MySqlCommand(Insertquery, conn))
                    {
                        cmd.Parameters.AddWithValue("@user_id", userId);
                        cmd.Parameters.AddWithValue("@lastname", LastnameText.Text.ToUpper());
                        cmd.Parameters.AddWithValue("@firstname", FirstnameText.Text.ToUpper());
                        cmd.Parameters.AddWithValue("@middlename", MiddlenameText.Text.ToUpper());
                        cmd.Parameters.AddWithValue("@email", EmailText.Text.Trim());
                        cmd.Parameters.AddWithValue("@school_year", ContextYearText.Text.ToUpper());
                        cmd.Parameters.AddWithValue("@school_section", ContextSectionText.Text.ToUpper());
                        cmd.Parameters.AddWithValue("@school_semester", semester);
                        cmd.Parameters.AddWithValue("@school_course", ContextCourseText.Text.ToUpper());
                        cmd.ExecuteNonQuery();
                    }

                    string AttendanceQuery = "INSERT INTO professor_attendance (student_id, student_name) VALUES (@student_id, @student_name)";
                    using (MySqlCommand cmd3 = new MySqlCommand(AttendanceQuery, conn))
                    {
                        cmd3.Parameters.AddWithValue("@student_id", userId);
                        cmd3.Parameters.AddWithValue("@student_name",
                            $"{LastnameText.Text.ToUpper()} {FirstnameText.Text.ToUpper()} {MiddlenameText.Text.ToUpper()}");
                        cmd3.ExecuteNonQuery();
                    }

                    string rootPath = SettingsManager.Current.SaveFolder;
                    if (string.IsNullOrEmpty(rootPath))
                    {
                        MessageBox.Show("Root folder is not configured.",
                            "Root Folder Missing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    if (!Directory.Exists(rootPath))
                    {
                        try { Directory.CreateDirectory(rootPath); }
                        catch (Exception ex) { MessageBox.Show("Could not create root folder:\n" + ex.Message); return; }
                    }

                    string folderName = SanitizeFolderName(
                        $"{LastnameText.Text.ToUpper()}_{FirstnameText.Text.ToUpper()}_{MiddlenameText.Text.ToUpper()}");
                    string userFolderPath = Path.Combine(rootPath, folderName);

                    try
                    {
                        if (!Directory.Exists(userFolderPath))
                            Directory.CreateDirectory(userFolderPath);
                    }
                    catch (Exception ex) { MessageBox.Show("Could not create user folder:\n" + ex.Message); return; }

                    using (var cmd4 = new MySqlCommand("INSERT INTO mainfolderpath (user_id, FolderPath) VALUES (@user_id, @FolderPath)", conn))
                    {
                        cmd4.Parameters.AddWithValue("@user_id", userId);
                        cmd4.Parameters.AddWithValue("@FolderPath", userFolderPath);
                        cmd4.ExecuteNonQuery();
                    }
                }

                string email = EmailText.Text.Trim();
                string fullName = $"{FirstnameText.Text.Trim()} {MiddlenameText.Text.Trim()} {LastnameText.Text.Trim()}".Trim();
                string role = ContextRoleText.Text.Trim();

                bool emailed = TrySendCredentialsEmail(email, fullName, username, defaultPassword, role);

                if (emailed)
                    MessageBox.Show($"Account created!\n\nUsername: {username}\nPassword: {defaultPassword}\n\nEmailed to {email}",
                        "Account Created", MessageBoxButtons.OK, MessageBoxIcon.Information);
                else
                    MessageBox.Show($"Account created but email failed.\n\nUsername: {username}\nPassword: {defaultPassword}",
                        "Account Created (Email Failed)", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                ClearText();
                LoadUserData();
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
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
                        smtp.Credentials = new System.Net.NetworkCredential(
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
                Console.WriteLine("[CreateUser] Email send failed: " + ex.Message);
                return false;
            }
        }

        private void ClearText()
        {
            IdNumberText.Clear();
            FirstnameText.Clear();
            LastnameText.Clear();
            MiddlenameText.Clear();
            EmailText.Clear();
            ContextRoleText.SelectedIndex = -1;
            ContextYearText.SelectedIndex = -1;
            ContextSectionText.SelectedIndex = -1;
        }

        // =========================================================
        //  WORKSTATION
        // =========================================================
        public void WorkstationButton_Click(object sender, EventArgs e)
        {
            Button clickedButton = (Button)sender;
            string workstationId = clickedButton.Tag.ToString();
            selectedWorkstationId = workstationId;
            AddScreenViewer(workstationId);
        }

        private async void StartServer()
        {
            try
            {
                listener = new TcpListener(IPAddress.Any, SettingsManager.Current.WorkstationPort);
                listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                listener.Start();
            }
            catch (Exception ex) { Console.WriteLine("Workstation bind failed: " + ex.Message); return; }

            lblTotalWorkstations.Text = "0";

            while (isRunning)
            {
                try
                {
                    TcpClient client = await listener.AcceptTcpClientAsync();
                    string clientIp = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();

                    Button wsButton = null;
                    if (this.InvokeRequired)
                        this.Invoke(new Action(() => wsButton = OnWorkStationConnected(clientIp)));
                    else
                        wsButton = OnWorkStationConnected(clientIp);

                    _ = MonitorDisconnected(client, wsButton, clientIp);
                }
                catch (ObjectDisposedException) { break; }
                catch (SocketException) { break; }
                catch (Exception ex)
                {
                    if (!isRunning) break;
                    Console.WriteLine("Workstation accept error: " + ex.Message);
                }
            }
        }

        private Button OnWorkStationConnected(string clientIp)
        {
            if (workstationButtons.ContainsKey(clientIp))
            {
                Button existingBtn = workstationButtons[clientIp];
                existingBtn.BackColor = Color.LightGreen;
                UpdateConnectedCount();
                return existingBtn;
            }

            WorkStationNum++;

            Button MainPcButton = new Button();
            MainPcButton.Text = "PC " + WorkStationNum;
            MainPcButton.Height = 180;
            MainPcButton.Width = 131;
            MainPcButton.Margin = new Padding(5);
            MainPcButton.BackColor = Color.LightGreen;
            MainPcButton.Tag = clientIp;
            MainPcButton.Click += WorkstationButton_Click;

            MainWorkstationFLP.Controls.Add(MainPcButton);
            workstationButtons[clientIp] = MainPcButton;

            UpdateConnectedCount();
            return MainPcButton;
        }

        private async Task MonitorDisconnected(TcpClient client, Button wsButton, string clientIp)
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1];

            try
            {
                while (client.Connected && isRunning)
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, 1);
                    if (bytesRead == 0) break;
                }
            }
            catch { }
            finally
            {
                try
                {
                    if (!this.IsDisposed && this.IsHandleCreated)
                    {
                        if (this.InvokeRequired)
                        {
                            this.BeginInvoke(new Action(() =>
                            {
                                if (wsButton != null && !wsButton.IsDisposed)
                                    wsButton.BackColor = Color.Red;
                                UpdateConnectedCount();
                            }));
                        }
                        else
                        {
                            if (wsButton != null && !wsButton.IsDisposed)
                                wsButton.BackColor = Color.Red;
                            UpdateConnectedCount();
                        }
                    }
                }
                catch { }

                try { client.Close(); } catch { }
                try { client.Dispose(); } catch { }
            }
        }

        private void UpdateConnectedCount()
        {
            lblTotalWorkstations.Text = workstationButtons.Count.ToString();
        }

        // =========================================================
        //  SCREEN SHARING
        // =========================================================
        private async void StartScreenListener()
        {
            try
            {
                screenListener = new TcpListener(IPAddress.Any, SettingsManager.Current.ScreenSharePort);
                screenListener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                screenListener.Start();
            }
            catch { return; }

            while (isRunning)
            {
                try
                {
                    TcpClient client = await screenListener.AcceptTcpClientAsync();
                    _ = ReceiveScreenStream(client);
                }
                catch (ObjectDisposedException) { break; }
                catch (SocketException) { break; }
                catch { if (!isRunning) break; }
            }
        }

        private async Task ReceiveScreenStream(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            string clientIp = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();

            try
            {
                while (client.Connected && isRunning)
                {
                    byte[] lengthBuffer = new byte[4];
                    int read = await ReadExactAsync(stream, lengthBuffer, 4);
                    if (read == 0) break;

                    int imageLength = BitConverter.ToInt32(lengthBuffer, 0);
                    if (imageLength <= 0 || imageLength > 50 * 1024 * 1024) break;

                    byte[] imageBuffer = new byte[imageLength];
                    int totalRead = await ReadExactAsync(stream, imageBuffer, imageLength);
                    if (totalRead == 0) break;

                    using (MemoryStream ms = new MemoryStream(imageBuffer))
                    {
                        Image frame = Image.FromStream(ms);
                        if (!this.IsDisposed && this.IsHandleCreated)
                        {
                            if (this.InvokeRequired)
                                this.BeginInvoke(new Action(() => UpdateScreenViewer(clientIp, frame)));
                            else
                                UpdateScreenViewer(clientIp, frame);
                        }
                    }
                }
            }
            catch { }
            finally
            {
                try { client.Close(); } catch { }
                try { client.Dispose(); } catch { }
            }
        }

        private async Task<int> ReadExactAsync(NetworkStream stream, byte[] buffer, int count)
        {
            int totalRead = 0;
            while (totalRead < count)
            {
                int bytesRead = await stream.ReadAsync(buffer, totalRead, count - totalRead);
                if (bytesRead == 0) return 0;
                totalRead += bytesRead;
            }
            return totalRead;
        }

        private void UpdateScreenViewer(string clientIp, Image frame)
        {
            if (screenViewers.ContainsKey(clientIp) && screenViewers[clientIp] != null)
            {
                PictureBox pb = screenViewers[clientIp];
                Image oldImage = pb.Image;
                pb.Image = frame;
                oldImage?.Dispose();
            }
            else
            {
                frame.Dispose();
            }
        }

        private void AddScreenViewer(string workstationId)
        {
            ScreenViewerForm viewer = new ScreenViewerForm(workstationId);
            screenViewers[workstationId] = viewer.GetPictureBox();

            viewer.FormClosed += (s, args) =>
            {
                if (screenViewers.ContainsKey(workstationId))
                    screenViewers.Remove(workstationId);
            };

            viewer.Show();
        }

        // =========================================================
        //  SERVER FOLDER MANAGEMENT
        // =========================================================
        private void lsServerFolderSetup()
        {
            lvServerFolder.View = View.LargeIcon;
            lvServerFolder.LargeImageList = imageListIcon;
            lvServerFolder.MultiSelect = false;
        }

        private void LoadServerFolder(string path, bool addToHistory = true)
        {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path)) return;

            if (addToHistory && !string.IsNullOrEmpty(currentFolder) && currentFolder != path)
                folderHistory.Push(currentFolder);

            currentFolder = path;
            lvServerFolder.Items.Clear();
            imageListIcon.Images.Clear();
            int imageIndex = 0;

            foreach (string dir in Directory.GetDirectories(path))
            {
                imageListIcon.Images.Add(Properties.Resources.Folder);
                var item = new ListViewItem(Path.GetFileName(dir), imageIndex);
                item.Tag = dir;
                lvServerFolder.Items.Add(item);
                imageIndex++;
            }

            foreach (string file in Directory.GetFiles(path))
            {
                imageListIcon.Images.Add(Properties.Resources.Item);
                var item = new ListViewItem(Path.GetFileName(file), imageIndex);
                item.Tag = file;
                lvServerFolder.Items.Add(item);
                imageIndex++;
            }

            BtnBack.Enabled = folderHistory.Count > 0;
        }

        private void btnBack()
        {
            if (folderHistory.Count > 0)
            {
                string previousFolder = folderHistory.Pop();
                LoadServerFolder(previousFolder, addToHistory: false);
            }
        }

        private void doubleClick()
        {
            if (lvServerFolder.SelectedItems.Count == 0) return;
            string path = lvServerFolder.SelectedItems[0].Tag.ToString();

            if (Directory.Exists(path))
                LoadServerFolder(path);
            else if (File.Exists(path))
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
        }

        // =========================================================
        //  LOGOUT — CLEAN SHUTDOWN
        // =========================================================
        private void btnLogout_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show("Log out?", "Logout Confirmation",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result != DialogResult.Yes) return;

            isRunning = false;
            adminIsRunning = false;

            try { listener?.Stop(); } catch { }
            try { screenListener?.Stop(); } catch { }
            try { authPhotoListener?.Stop(); } catch { }
            try { authPhotoListener?.Server?.Dispose(); } catch { }
            authPhotoListener = null;

            System.Threading.Thread.Sleep(150);

            foreach (var kvp in screenViewers)
            {
                try { kvp.Value?.Image?.Dispose(); } catch { }
            }
            screenViewers.Clear();

            foreach (Form f in Application.OpenForms.Cast<Form>().ToList())
            {
                if (f is Login && !f.IsDisposed)
                {
                    f.Hide();
                    f.Dispose();
                }
            }

            Login loginForm = new Login();
            loginForm.Show();
            loginForm.BringToFront();
            loginForm.Activate();

            this.Hide();
            this.Close();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            adminIsRunning = false;

            try { authPhotoListener?.Stop(); } catch { }
            try { authPhotoListener?.Server?.Dispose(); } catch { }
            authPhotoListener = null;

            base.OnFormClosing(e);
        }

        private void InitializeFileManagementButtons()
        {
            try
            {
                // Add a Delete button to the File Management panel
                btnDeleteFile = new Guna.UI2.WinForms.Guna2Button
                {
                    Text = "🗑",
                    Size = new Size(100, 42),
                    Location = new Point(1150, 85),
                    Anchor = AnchorStyles.Top | AnchorStyles.Right,
                    BorderRadius = 10,
                    FillColor = Color.Maroon,
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                    Cursor = Cursors.Hand
                };
                btnDeleteFile.HoverState.FillColor = Color.FromArgb(160, 40, 40);
                btnDeleteFile.Click += BtnDeleteFile_Click;

                pnlFileManagement.Controls.Add(btnDeleteFile);
                btnDeleteFile.BringToFront();
            }
            catch (Exception ex)
            {
                Console.WriteLine("InitializeFileManagementButtons error: " + ex.Message);
            }
        }

        private void BtnDeleteFile_Click(object sender, EventArgs e)
        {
            if (lvServerFolder.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select a file or folder first.",
                    "Nothing Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string path = lvServerFolder.SelectedItems[0].Tag?.ToString();
            if (string.IsNullOrEmpty(path))
            {
                MessageBox.Show("Invalid selection.");
                return;
            }

            bool isFolder = Directory.Exists(path);
            bool isFile = File.Exists(path);

            if (!isFolder && !isFile)
            {
                MessageBox.Show("The selected item no longer exists.");
                return;
            }

            // Prevent deleting the root folder
            if (isFolder && !string.IsNullOrEmpty(currentFolder) &&
                string.Equals(Path.GetFullPath(path).TrimEnd('\\'),
                              Path.GetFullPath(SettingsManager.Current.SaveFolder).TrimEnd('\\'),
                              StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("You cannot delete the root folder.",
                    "Not Allowed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string itemName = Path.GetFileName(path);

            string message = isFolder
                ? $"Delete folder '{itemName}' and ALL of its contents?\n\nThis cannot be undone."
                : $"Delete file '{itemName}'?\n\nThis cannot be undone.";

            var confirm = MessageBox.Show(message, "Confirm Delete",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes) return;

            try
            {
                if (isFile)
                {
                    var attrs = File.GetAttributes(path);
                    if ((attrs & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                        File.SetAttributes(path, attrs & ~FileAttributes.ReadOnly);
                }

                if (isFolder)
                    Directory.Delete(path, recursive: true);
                else
                    File.Delete(path);

                MessageBox.Show("Deleted successfully.",
                    "Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Refresh the current folder view
                if (!string.IsNullOrEmpty(currentFolder) && Directory.Exists(currentFolder))
                    LoadServerFolder(currentFolder, addToHistory: false);
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show(
                    "Access denied.\n\nThe file/folder may be open in another program, " +
                    "or you don't have permission.",
                    "Delete Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (IOException ioEx)
            {
                MessageBox.Show(
                    "The file is in use or locked.\n\nDetails: " + ioEx.Message,
                    "Delete Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error deleting: " + ex.Message,
                    "Delete Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}