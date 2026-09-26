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
        private string saveFolder = @"C:\ReceivedFileFolder";

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

        // AUTH PHOTO LISTENER
        private TcpListener authPhotoListener;
        private volatile bool adminIsRunning = true;

        // BANNER MANAGEMENT
        private Guna.UI2.WinForms.Guna2Button btnUploadBanner;
        private Guna.UI2.WinForms.Guna2Button btnOpenBannersFolder;

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
            isRunning = true;
            adminIsRunning = true;

            lblTotalUsers.Text = TotalUsers().ToString();

            LoadUserData();

            StartServer();
            StartScreenListener();
            _ = StartAuthPhotoListener();

            InitializeBannerButtons();
        }

        // =========================================================
        //  AUTH PHOTO RECEIVER
        // =========================================================
        private async Task StartAuthPhotoListener()
        {
            int port = SettingsManager.Current.AdminPhotoPort;

            try
            {
                authPhotoListener = new TcpListener(IPAddress.Any, port);
                authPhotoListener.Server.SetSocketOption(
                    SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                authPhotoListener.Start();
                Console.WriteLine($"[Admin] AuthPhoto listener started on {port}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Admin] AuthPhoto bind FAILED: " + ex.Message);
                MessageBox.Show("Failed to start auth photo listener: " + ex.Message);
                return;
            }

            while (adminIsRunning)
            {
                try
                {
                    TcpClient client = await authPhotoListener.AcceptTcpClientAsync();
                    _ = HandleAuthPhotoReceive(client);
                }
                catch (ObjectDisposedException) { break; }
                catch (Exception ex)
                {
                    if (!adminIsRunning) break;
                    Console.WriteLine("[Admin] AuthPhoto accept error: " + ex.Message);
                }
            }
        }

        private async Task HandleAuthPhotoReceive(TcpClient client)
        {
            try
            {
                using (client)
                using (NetworkStream stream = client.GetStream())
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    string subfolder = reader.ReadString();
                    string fileName = reader.ReadString();
                    int length = reader.ReadInt32();

                    if (length <= 0 || length > 20 * 1024 * 1024)
                    {
                        Console.WriteLine("[Admin] Invalid auth photo length: " + length);
                        return;
                    }

                    byte[] bytes = reader.ReadBytes(length);

                    foreach (char c in Path.GetInvalidFileNameChars())
                        fileName = fileName.Replace(c, '_');

                    subfolder = subfolder
                        .Replace("..", "")
                        .Replace("/", "")
                        .Replace("\\", "");

                    string root = SettingsManager.Current.AdminSharedRoot;
                    string folder = Path.Combine(root, subfolder);
                    if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                    string savePath = Path.Combine(folder, fileName);
                    await File.WriteAllBytesAsync(savePath, bytes);

                    Console.WriteLine("[Admin] Auth photo saved → " + savePath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("HandleAuthPhotoReceive error: " + ex.Message);
            }
        }

        // =========================================================
        //  BANNER MANAGEMENT
        // =========================================================
        private void InitializeBannerButtons()
        {
            int btnHeight = 45;
            int btnWidth = 180;

            btnUploadBanner = new Guna.UI2.WinForms.Guna2Button();
            btnUploadBanner.Text = "Upload Banner";
            btnUploadBanner.Size = new Size(btnWidth, btnHeight);
            btnUploadBanner.BorderRadius = 10;
            btnUploadBanner.FillColor = Color.Maroon;
            btnUploadBanner.ForeColor = Color.White;
            btnUploadBanner.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            btnUploadBanner.Cursor = Cursors.Hand;
            btnUploadBanner.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

            btnUploadBanner.Location = new Point(this.ClientSize.Width, 150);

            btnUploadBanner.Click += BtnUploadBanner_Click;
            this.Controls.Add(btnUploadBanner);
            btnUploadBanner.BringToFront();

            btnOpenBannersFolder = new Guna.UI2.WinForms.Guna2Button();
            btnOpenBannersFolder.Text = "Open Folder";
            btnOpenBannersFolder.Size = new Size(140, btnHeight);
            btnOpenBannersFolder.BorderRadius = 10;
            btnOpenBannersFolder.FillColor = Color.Gray;
            btnOpenBannersFolder.ForeColor = Color.White;
            btnOpenBannersFolder.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            btnOpenBannersFolder.Cursor = Cursors.Hand;
            btnOpenBannersFolder.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

            btnOpenBannersFolder.Location = new Point(
                btnUploadBanner.Left - btnOpenBannersFolder.Width - 10,
                btnUploadBanner.Top);

            btnOpenBannersFolder.Click += BtnOpenBannersFolder_Click;
            this.Controls.Add(btnOpenBannersFolder);
            btnOpenBannersFolder.BringToFront();
        }

        private void BtnUploadBanner_Click(object sender, EventArgs e)
        {
            string folder = BannerHelper.GetBannersFolder();

            if (string.IsNullOrEmpty(folder))
            {
                MessageBox.Show(
                    "The Banners folder could not be located.\n\n" +
                    "Please set a Root Folder in Login → Configuration → File Storage first.",
                    "Root Folder Not Set",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif";
                ofd.Multiselect = true;
                ofd.Title = "Select banner image(s) to upload";

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

                        Console.WriteLine("[Admin] Uploaded: " + dest);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"Failed to upload {Path.GetFileName(source)}:\n\n{ex.Message}",
                            "Upload Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                }

                if (copied > 0)
                {
                    MessageBox.Show(
                        $"{copied} banner(s) uploaded successfully.\n\n" +
                        $"Location:\n{folder}",
                        "Upload Complete",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
        }

        private void BtnOpenBannersFolder_Click(object sender, EventArgs e)
        {
            string folder = BannerHelper.GetBannersFolder();

            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
            {
                MessageBox.Show(
                    "The Banners folder does not exist yet.\n\n" +
                    "Please set a Root Folder in Login → Configuration → File Storage first.",
                    "Folder Not Found",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            try
            {
                System.Diagnostics.Process.Start("explorer.exe", "\"" + folder + "\"");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not open folder:\n" + ex.Message);
            }
        }

        // =========================================================
        //  NAVIGATION
        // =========================================================
        private void btnDashboard_Click_1(object sender, EventArgs e)
        {
            panelDashoard.BringToFront();
            navbarStyle.RemoveIndicator(PanelIndicator);
            PanelIndicator = navbarStyle.CreateIndicator(btnDashboard);
        }

        private void btnUserManagement_Click(object sender, EventArgs e)
        {
            pnlUserManagement.BringToFront();
            navbarStyle.RemoveIndicator(PanelIndicator);
            PanelIndicator = navbarStyle.CreateIndicator(btnUserManagement);
            LoadUserData();
        }

        private void btnFileManagement_Click(object sender, EventArgs e)
        {
            pnlFileManagement.BringToFront();
            navbarStyle.RemoveIndicator(PanelIndicator);
            PanelIndicator = navbarStyle.CreateIndicator(btnFileManagement);

            string root = SettingsManager.Current.SaveFolder;
            if (string.IsNullOrEmpty(root))
                root = saveFolder;

            if (!Directory.Exists(root))
            {
                MessageBox.Show(
                    "Root save folder is not configured or does not exist:\n" + root +
                    "\n\nPlease set it in Login → Configuration → File Storage.",
                    "Folder Missing",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            lsServerFolderSetup();
            LoadServerFolder(root, addToHistory: false);
        }

        private void btnWorkstation_Click(object sender, EventArgs e)
        {
            pnlWorkstation.BringToFront();
            navbarStyle.RemoveIndicator(PanelIndicator);
            PanelIndicator = navbarStyle.CreateIndicator(btnWorkstation);
        }

        private void CreateButton_Click(object sender, EventArgs e)
        {
            CreateUser();
        }

        private void ContextRoleText_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ContextRoleText.Text == "Professor")
            {
                ContextYearText.Enabled = false;
                ContextSectionText.Enabled = false;
                ContextCourseText.Enabled = false;
            }
            else if (ContextRoleText.Text == "Student")
            {
                ContextYearText.Enabled = true;
                ContextSectionText.Enabled = true;
                ContextCourseText.Enabled = true;
            }
            else if (ContextRoleText.Text == "Admin")
            {
                ContextYearText.Enabled = false;
                ContextSectionText.Enabled = false;
                ContextCourseText.Enabled = false;
            }
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

                default:
                    break;
            }
        }

        private void SearchButton_TextChanged(object sender, EventArgs e)
        {
            LoadUserData(SearchButton.Text);
        }

        private void lvServerFolder_DoubleClick(object sender, EventArgs e)
        {
            doubleClick();
        }

        private void BtnBack_Click(object sender, EventArgs e)
        {
            btnBack();
        }

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
                SizeMode = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.None
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
                ForeColor = Color.Black,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Size = new Size(80, 32),
                Location = new Point(270, 175),
                Cursor = Cursors.Hand
            };
            detailsInfoTab.FlatAppearance.BorderColor = Color.LightGray;

            detailsHistoryTab = new Button
            {
                Text = "History",
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = Color.Black,
                Font = new Font("Segoe UI", 9F),
                Size = new Size(100, 32),
                Location = new Point(352, 175),
                Cursor = Cursors.Hand
            };
            detailsHistoryTab.FlatAppearance.BorderColor = Color.LightGray;

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
            if (UserDataList.Columns.Contains("profile_picture"))
            {
                object raw = row.Cells["profile_picture"].Value;

                byte[] bytes = raw as byte[];
                if (bytes != null && bytes.Length > 0)
                {
                    try
                    {
                        using (var ms = new MemoryStream(bytes))
                            return Image.FromStream(ms);
                    }
                    catch { }
                }

                string path = raw as string;
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                        return Image.FromStream(fs);
                }
            }

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

            var editItem = new ToolStripMenuItem("Edit Info") { Name = "cmEdit" };
            var deleteItem = new ToolStripMenuItem("Delete Info") { Name = "cmDelete" };
            var resetItem = new ToolStripMenuItem("Reset Password") { Name = "cmReset" };
            var bulkSecItem = new ToolStripMenuItem("Update Section (Bulk)") { Name = "cmBulkSection" };
            var bulkYrItem = new ToolStripMenuItem("Update Year (Bulk)") { Name = "cmBulkYear" };
            var bulkSemItem = new ToolStripMenuItem("Update Semester (Bulk)") { Name = "cmBulkSemester" };

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

            if (UserDataList.Columns.Contains("user_id") &&
                row.Cells["user_id"].Value != null &&
                row.Cells["user_id"].Value != DBNull.Value)
            {
                contextUserId = Convert.ToInt32(row.Cells["user_id"].Value);
            }
            else
            {
                contextUserId = -1;
            }
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
            catch (Exception ex)
            {
                MessageBox.Show("Error loading user: " + ex.Message);
                return;
            }

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
                catch (Exception ex)
                {
                    MessageBox.Show("Error updating user: " + ex.Message);
                }
            }
        }

        private void ContextDelete_Click(object sender, EventArgs e)
        {
            if (contextUserId < 0) { MessageBox.Show("No user selected."); return; }

            var confirm = MessageBox.Show(
                "Are you sure you want to delete this user?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

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
                            try { cmd.ExecuteNonQuery(); }
                            catch { }
                        }
                    }
                }
                MessageBox.Show("User deleted.");
                LoadUserData();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error deleting user: " + ex.Message);
            }
        }

        private void ContextResetPassword_Click(object sender, EventArgs e)
        {
            if (contextUserId < 0) { MessageBox.Show("No user selected."); return; }

            var confirm = MessageBox.Show(
                "Reset this user's password to the default '12345678'?",
                "Confirm Reset",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            string connStr = SettingsManager.Current.GetConnectionString();
            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string q = @"UPDATE user_credential SET p_word = @pw WHERE user_id = @id";
                    using (var cmd = new MySqlCommand(q, conn))
                    {
                        cmd.Parameters.AddWithValue("@pw", "12345678");
                        cmd.Parameters.AddWithValue("@id", contextUserId);
                        cmd.ExecuteNonQuery();
                    }
                }
                MessageBox.Show("Password reset to default: 12345678");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error resetting password: " + ex.Message);
            }
        }

        // =========================================================
        //  BULK UPDATE
        // =========================================================
        private void ContextBulkSection_Click(object sender, EventArgs e)
        {
            BulkUpdateField("school_section", "Section", "e.g. 4-1");
        }

        private void ContextBulkYear_Click(object sender, EventArgs e)
        {
            BulkUpdateField("school_year", "Year", "e.g. 4TH YEAR");
        }

        private void ContextBulkSemester_Click(object sender, EventArgs e)
        {
            BulkUpdateField("school_semester", "Semester", "e.g. 1ST SEMESTER / 2ND SEMESTER");
        }

        private void BulkUpdateField(string columnName, string displayName, string hint)
        {
            var ids = GetSelectedUserIds();
            if (ids.Count == 0)
            {
                MessageBox.Show("No rows selected. Hold Ctrl or Shift and click multiple rows first.");
                return;
            }

            string newValue = Prompt(
                $"Enter the new {displayName} for {ids.Count} selected user(s).\n({hint})",
                "");

            if (string.IsNullOrWhiteSpace(newValue)) return;

            newValue = newValue.Trim().ToUpper();

            var confirm = MessageBox.Show(
                $"Update {displayName} of {ids.Count} user(s) to \"{newValue}\"?",
                "Confirm Bulk Update",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            string connStr = SettingsManager.Current.GetConnectionString();
            try
            {
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
                        MessageBox.Show($"{rows} user(s) updated to {displayName} = \"{newValue}\".");
                    }
                }
                LoadUserData();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error during bulk update: " + ex.Message);
            }
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
                frm.Text = "Bulk Update";
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
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private static int TotalUsers()
        {
            string connStr = SettingsManager.Current.GetConnectionString();

            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = "SELECT COUNT(*) FROM user_credential";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        return Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return 0;
            }
        }

        // =========================================================
        //  CREATE ACCOUNT
        // =========================================================
        string semester;

        private void CreateUser()
        {
            string connStr = SettingsManager.Current.GetConnectionString();

            if (string.IsNullOrEmpty(IdNumberText.Text) && string.IsNullOrEmpty(ContextRoleText.Text) && string.IsNullOrEmpty(LastnameText.Text) && string.IsNullOrEmpty(FirstnameText.Text) && string.IsNullOrEmpty(MiddlenameText.Text)
                && string.IsNullOrEmpty(EmailText.Text) && string.IsNullOrEmpty(ContextYearText.Text) && string.IsNullOrEmpty(ContextSectionText.Text) && string.IsNullOrEmpty(ContextCourseText.Text))
            {
                MessageBox.Show("Please Fill up the Blank");
                return;
            }

            if (ContextRoleText.Text == "Professor")
                semester = "Null";
            else if (ContextRoleText.Text == "Student")
                semester = "1st Semester";

            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    string Insertquery2 = @"INSERT INTO user_credential (username, p_word, roles, user_status, authentication_condition)
                                            VALUES (@Uname, @Password, @UserRole, @Status, @authentication_condition); SELECT LAST_INSERT_ID();";

                    long userId;
                    using (MySqlCommand cmd2 = new MySqlCommand(Insertquery2, conn))
                    {
                        cmd2.Parameters.AddWithValue("@Uname", IdNumberText.Text.Trim());
                        cmd2.Parameters.AddWithValue("@Password", "12345678");
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
                        cmd3.Parameters.AddWithValue("@student_name", $"{LastnameText.Text.ToUpper()} {FirstnameText.Text.ToUpper()} {MiddlenameText.Text.ToUpper()}");
                        cmd3.ExecuteNonQuery();
                    }

                    string rootPath = SettingsManager.Current.SaveFolder;

                    if (string.IsNullOrEmpty(rootPath))
                    {
                        MessageBox.Show(
                            "Root folder is not configured.\n\n" +
                            "Please log out and set it in Login → Configuration → File Storage first.",
                            "Root Folder Missing",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    if (!Directory.Exists(rootPath))
                    {
                        try { Directory.CreateDirectory(rootPath); }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Could not create root folder:\n" + rootPath + "\n\n" + ex.Message);
                            return;
                        }
                    }

                    string folderName = SanitizeFolderName(
                        $"{LastnameText.Text.ToUpper()}_{FirstnameText.Text.ToUpper()}_{MiddlenameText.Text.ToUpper()}");

                    string userFolderPath = Path.Combine(rootPath, folderName);

                    try
                    {
                        if (!Directory.Exists(userFolderPath))
                            Directory.CreateDirectory(userFolderPath);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Could not create user folder:\n" + userFolderPath + "\n\n" + ex.Message);
                        return;
                    }

                    string FolderPathQuery = "INSERT INTO mainfolderpath (user_id, FolderPath) VALUES (@user_id, @FolderPath)";
                    using (var cmd4 = new MySqlCommand(FolderPathQuery, conn))
                    {
                        cmd4.Parameters.AddWithValue("@user_id", userId);
                        cmd4.Parameters.AddWithValue("@FolderPath", userFolderPath);
                        cmd4.ExecuteNonQuery();
                    }

                    MessageBox.Show("Account Successfully Created!");
                    ClearText();
                    LoadUserData();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private string SanitizeFolderName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "Unknown";

            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');

            name = name.Trim().TrimEnd('.');
            if (string.IsNullOrWhiteSpace(name)) return "Unknown";

            return name;
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
            catch (Exception ex)
            {
                Console.WriteLine("Workstation bind failed: " + ex.Message);
                return;
            }

            lblTotalWorkstations.Text = "0";

            while (isRunning)
            {
                try
                {
                    TcpClient client = await listener.AcceptTcpClientAsync();
                    string clientIp = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();

                    Console.WriteLine("🟢 New TCP connection accepted from: " + clientIp);

                    Button wsButton = null;

                    if (this.InvokeRequired)
                        this.Invoke(new Action(() => wsButton = OnWorkStationConnected(clientIp)));
                    else
                        wsButton = OnWorkStationConnected(clientIp);

                    _ = MonitorDisconnected(client, wsButton, clientIp);
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (SocketException)
                {
                    break;
                }
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
            catch (Exception ex)
            {
                Console.WriteLine("MonitorDisconnected exception for " + clientIp + ": " + ex.Message);
            }
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
            int connectedCount = workstationButtons.Values
                .Count(btn => btn.BackColor == Color.LightGreen);

            int disconnectedCount = workstationButtons.Count - connectedCount;

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
            catch (Exception ex)
            {
                Console.WriteLine("Screen listener bind failed: " + ex.Message);
                return;
            }

            while (isRunning)
            {
                try
                {
                    TcpClient client = await screenListener.AcceptTcpClientAsync();
                    _ = ReceiveScreenStream(client);
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (SocketException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    if (!isRunning) break;
                    Console.WriteLine("Screen accept error: " + ex.Message);
                }
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

                    Console.WriteLine("Receiving frame: " + imageLength + " bytes from " + clientIp);
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
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
                return;

            if (addToHistory && !string.IsNullOrEmpty(currentFolder) && currentFolder != path)
                folderHistory.Push(currentFolder);

            currentFolder = path;
            lvServerFolder.Items.Clear();
            imageListIcon.Images.Clear();
            int imageIndex = 0;

            foreach (string dir in Directory.GetDirectories(path))
            {
                imageListIcon.Images.Add(Properties.Resources.Folder);
                ListViewItem item = new ListViewItem(Path.GetFileName(dir), imageIndex);
                item.Tag = dir;
                lvServerFolder.Items.Add(item);
                imageIndex++;
            }

            foreach (string file in Directory.GetFiles(path))
            {
                Icon fileIcon = Icon.ExtractAssociatedIcon(file);
                imageListIcon.Images.Add(Properties.Resources.Item);

                ListViewItem item = new ListViewItem(Path.GetFileName(file), imageIndex);
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
            var result = MessageBox.Show(
                "Are you sure you want to log out?",
                "Logout Confirmation",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

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

            Login loginForm = new Login();
            loginForm.Show();

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
    }
}