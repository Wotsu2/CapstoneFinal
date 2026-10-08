using Microsoft.VisualBasic.ApplicationServices;
using MySql.Data.MySqlClient;
using Org.BouncyCastle.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
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
        private ContextMenuStrip userContextMenu;
        private int contextUserId = -1;

        private Panel PanelIndicator;

        private string currentFolder;
        private Stack<string> folderHistory = new Stack<string>();
        private List<ListViewItem> allServerFolderItems = new List<ListViewItem>();

        private TcpListener authPhotoListener;
        private volatile bool adminIsRunning = true;

        private Guna.UI2.WinForms.Guna2Button btnAppMenu;
        private ContextMenuStrip appMenu;

        private Guna.UI2.WinForms.Guna2Button btnDeleteFile;

        // Embedded panels
        private Panel pnlDatabase;
        private DatabaseManagerForm databaseManager;

        // Dashboard user control host
        private Panel pnlDashboardHost;
        private DashboardControl dashboardControl;

        // ★★★ HARDCODED PROFESSOR FOLDER — change this if you rename the folder ★★★
        private const string PROFESSOR_ROOT = @"\\192.168.100.4\SharedFolder\CPA10072026";

        public AdminForm()
        {
            InitializeComponent();

            InitializeUserContextMenu();

            UserDataList.CellDoubleClick += UserDataList_CellDoubleClick;
            UserDataList.CellMouseDown += UserDataList_CellMouseDown;

            LoadUserData();
        }

        private void admindash_Load(object sender, EventArgs e)
        {
            adminIsRunning = true;

            StyleUserDataGrid();
            LoadUserData();

            _ = StartAuthPhotoListener();

            InitializeAppMenu();
            InitializeFileManagementButtons();
            BuildEmbeddedDatabasePanel();
            BuildEmbeddedDashboardPanel();

            if (pnlDashboardHost != null)
            {
                pnlDashboardHost.Visible = true;
                pnlDashboardHost.BringToFront();
                LayoutDashboardPanel();
                dashboardControl?.RefreshAll();
            }

            pnlUserManagement.SendToBack();
            pnlFileManagement.SendToBack();
            if (pnlDatabase != null) pnlDatabase.SendToBack();
        }

        // =========================================================
        //  EMBEDDED DASHBOARD PANEL
        // =========================================================
        private void BuildEmbeddedDashboardPanel()
        {
            pnlDashboardHost = new Panel
            {
                Name = "pnlDashboardHost",
                BackColor = Color.FromArgb(245, 245, 248),
                Visible = false,
                AutoScroll = false
            };

            dashboardControl = new DashboardControl
            {
                Dock = DockStyle.Fill
            };

            pnlDashboardHost.Controls.Add(dashboardControl);
            this.Controls.Add(pnlDashboardHost);

            LayoutDashboardPanel();
            pnlDashboardHost.BringToFront();
        }

        private void LayoutDashboardPanel()
        {
            if (pnlDashboardHost == null || pnlDashboardHost.IsDisposed) return;

            int left = (pnlUserManagement != null) ? pnlUserManagement.Left : 40;
            int top = (pnlUserManagement != null) ? pnlUserManagement.Top : 293;

            int w = Math.Max(400, this.ClientSize.Width - left * 2);
            int h = Math.Max(250, this.ClientSize.Height - top + 250);

            pnlDashboardHost.SetBounds(left, top, w, h);
        }

        // =========================================================
        //  EMBEDDED DATABASE PANEL
        // =========================================================
        private void BuildEmbeddedDatabasePanel()
        {
            pnlDatabase = new Panel
            {
                Name = "pnlDatabase",
                BackColor = Color.White,
                Visible = false,
                AutoScroll = false
            };

            databaseManager = new DatabaseManagerForm
            {
                Dock = DockStyle.Fill
            };

            pnlDatabase.Controls.Add(databaseManager);
            this.Controls.Add(pnlDatabase);

            LayoutDatabasePanel();
            pnlDatabase.BringToFront();
        }

        private void LayoutDatabasePanel()
        {
            if (pnlDatabase == null || pnlDatabase.IsDisposed) return;

            int left = (pnlUserManagement != null) ? pnlUserManagement.Left : 40;
            int top = (pnlUserManagement != null) ? pnlUserManagement.Top : 293;

            int w = Math.Max(400, this.ClientSize.Width - left * 2);
            int h = Math.Max(250, this.ClientSize.Height - top);

            pnlDatabase.SetBounds(left, top, w, h);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutDatabasePanel();
            LayoutDashboardPanel();
        }

        // =========================================================
        //  DATABASE BUTTON CLICK
        // =========================================================
        private void btnDatabase_Click(object sender, EventArgs e)
        {
            if (pnlDatabase == null) return;

            pnlUserManagement.Visible = false;
            pnlFileManagement.Visible = false;
            if (pnlDashboardHost != null) pnlDashboardHost.Visible = false;

            pnlDatabase.Visible = true;
            pnlDatabase.BringToFront();
            LayoutDatabasePanel();

            navbarStyle.RemoveIndicator(PanelIndicator);
            PanelIndicator = navbarStyle.CreateIndicator(btnDatabase);
        }

        // =========================================================
        //  POPUP CREATE ACCOUNT
        // =========================================================
        private void OpenCreateAccountForm()
        {
            using (var dlg = new CreateAccountForm())
            {
                var result = dlg.ShowDialog(this);
                if (result == DialogResult.OK)
                {
                    LoadUserData();
                    dashboardControl?.RefreshAll();
                }
            }
        }

        // =========================================================
        //  COUNT FILES
        // =========================================================
        private int CountFilesInServerFolder()
        {
            try
            {
                string root = SettingsManager.Current.SaveFolder;
                if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
                    return 0;

                return Directory.EnumerateFiles(
                    root,
                    "*",
                    SearchOption.AllDirectories).Count();
            }
            catch (Exception ex)
            {
                Console.WriteLine("CountFilesInServerFolder error: " + ex.Message);
                return 0;
            }
        }

        // =========================================================
        //  APP MENU (☰)
        // =========================================================
        private void InitializeAppMenu()
        {
            btnAppMenu = new Guna.UI2.WinForms.Guna2Button
            {
                Text = "☰",
                Size = new Size(46, 20),
                Location = new Point(0, 0),
                BorderRadius = 0,
                FillColor = Color.White,
                ForeColor = Color.FromArgb(123, 15, 23),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Animated = true,
                Name = "btnAppMenu"
            };
            btnAppMenu.HoverState.FillColor = Color.FromArgb(250, 235, 235);

            appMenu = new ContextMenuStrip
            {
                Font = new Font("Segoe UI", 9F),
                ShowImageMargin = false,
                BackColor = Color.White,
                Renderer = new ToolStripProfessionalRenderer(new AppMenuColorTable())
            };

            var miUpload = new ToolStripMenuItem("Upload Banner");
            miUpload.Click += BtnUploadBanner_Click;

            var miOpenFolder = new ToolStripMenuItem("Open Banners Folder");
            miOpenFolder.Click += BtnOpenBannersFolder_Click;

            var sep = new ToolStripSeparator();

            var miAnnouncements = new ToolStripMenuItem("Announcements");
            miAnnouncements.Click += (s, e) =>
            {
                using (var annForm = new AnnouncementsForm())
                {
                    annForm.ShowDialog(this);
                }
            };

            appMenu.Items.Add(miUpload);
            appMenu.Items.Add(miOpenFolder);
            appMenu.Items.Add(sep);
            appMenu.Items.Add(miAnnouncements);

            btnAppMenu.Click += (s, e) =>
            {
                appMenu.Show(btnAppMenu, new Point(0, btnAppMenu.Height));
            };

            this.Controls.Add(btnAppMenu);
            btnAppMenu.BringToFront();
        }

        private class AppMenuColorTable : ProfessionalColorTable
        {
            public override Color MenuItemSelected => Color.FromArgb(250, 235, 235);
            public override Color MenuItemSelectedGradientBegin => Color.FromArgb(250, 235, 235);
            public override Color MenuItemSelectedGradientEnd => Color.FromArgb(250, 235, 235);
            public override Color MenuItemBorder => Color.FromArgb(200, 180, 180);
            public override Color MenuBorder => Color.FromArgb(210, 200, 200);
        }

        // =========================================================
        //  DATAGRIDVIEW STYLING
        // =========================================================
        private void StyleUserDataGrid()
        {
            UserDataList.BorderStyle = BorderStyle.None;
            UserDataList.BackgroundColor = Color.White;
            UserDataList.GridColor = Color.FromArgb(240, 235, 235);
            UserDataList.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            UserDataList.EnableHeadersVisualStyles = false;
            UserDataList.RowHeadersVisible = false;
            UserDataList.AllowUserToResizeRows = false;
            UserDataList.AllowUserToAddRows = false;
            UserDataList.AllowUserToDeleteRows = false;
            UserDataList.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            UserDataList.MultiSelect = true;
            UserDataList.ReadOnly = true;
            UserDataList.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            UserDataList.ScrollBars = ScrollBars.Both;
            UserDataList.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

            UserDataList.ColumnHeadersHeight = 44;
            UserDataList.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.Maroon,
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0),
                SelectionBackColor = Color.Maroon,
                SelectionForeColor = Color.White,
                WrapMode = DataGridViewTriState.False
            };

            UserDataList.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.White,
                ForeColor = Color.FromArgb(50, 50, 50),
                Font = new Font("Segoe UI", 9.5F),
                SelectionBackColor = Color.FromArgb(255, 235, 235),
                SelectionForeColor = Color.FromArgb(80, 0, 0),
                Padding = new Padding(8, 0, 0, 0),
                Alignment = DataGridViewContentAlignment.MiddleLeft
            };

            UserDataList.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(252, 248, 248),
                ForeColor = Color.FromArgb(50, 50, 50),
                SelectionBackColor = Color.FromArgb(255, 235, 235),
                SelectionForeColor = Color.FromArgb(80, 0, 0),
                Font = new Font("Segoe UI", 9.5F),
                Padding = new Padding(8, 0, 0, 0)
            };

            UserDataList.RowTemplate.Height = 40;

            UserDataList.DataBindingComplete -= UserDataList_DataBindingComplete;
            UserDataList.DataBindingComplete += UserDataList_DataBindingComplete;
        }

        private void UserDataList_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            if (UserDataList.Columns.Count == 0) return;

            if (UserDataList.Columns.Contains("user_id"))
                UserDataList.Columns["user_id"].Visible = false;
            if (UserDataList.Columns.Contains("profile_picture"))
                UserDataList.Columns["profile_picture"].Visible = false;

            SetCol("username", "Username", 120);
            SetCol("roles", "Role", 100);
            SetCol("lastname", "Last Name", 130);
            SetCol("firstname", "First Name", 130);
            SetCol("middlename", "Middle Name", 120);
            SetCol("email", "Email", 200);
            SetCol("school_year", "Year", 90);
            SetCol("school_section", "Section", 90);
            SetCol("school_semester", "Semester", 120);
            SetCol("school_course", "Course", 140);
            SetCol("user_status", "Status", 90);

            CenterCol("roles");
            CenterCol("school_year");
            CenterCol("school_section");
            CenterCol("user_status");

            if (UserDataList.Columns.Contains("user_status"))
            {
                UserDataList.CellFormatting -= UserDataList_CellFormatting;
                UserDataList.CellFormatting += UserDataList_CellFormatting;
            }
        }

        private void SetCol(string name, string header, int minWidth)
        {
            if (!UserDataList.Columns.Contains(name)) return;
            var c = UserDataList.Columns[name];
            c.HeaderText = header;
            c.MinimumWidth = minWidth;
            c.FillWeight = minWidth;
        }

        private void CenterCol(string name)
        {
            if (!UserDataList.Columns.Contains(name)) return;
            UserDataList.Columns[name].DefaultCellStyle.Alignment =
                DataGridViewContentAlignment.MiddleCenter;
        }

        private void UserDataList_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;

            if (UserDataList.Columns[e.ColumnIndex].Name == "user_status" && e.Value != null)
            {
                string status = e.Value.ToString();
                if (string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase))
                {
                    e.CellStyle.ForeColor = Color.FromArgb(30, 130, 70);
                    e.CellStyle.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
                }
                else if (string.Equals(status, "Inactive", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(status, "Disabled", StringComparison.OrdinalIgnoreCase))
                {
                    e.CellStyle.ForeColor = Color.FromArgb(180, 40, 40);
                    e.CellStyle.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
                }
            }
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
                using (BinaryReader reader = new BinaryReader(stream, Encoding.UTF8))
                {
                    string firstToken = reader.ReadString();
                    Console.WriteLine("[Admin] FileTransfer command: " + firstToken);

                    // =========================================================
                    // ACTIVITY FILE → <PROFESSOR_ROOT>/<Section>/ActivityFiles/
                    // =========================================================
                    if (firstToken == "ACTIVITY_FILE")
                    {
                        string professorFolder = reader.ReadString();
                        string section = reader.ReadString();
                        string fileName = reader.ReadString();
                        int length = reader.ReadInt32();

                        if (length <= 0 || length > 200 * 1024 * 1024) return;

                        byte[] bytes = reader.ReadBytes(length);
                        section = SanitizeFolderName(section);
                        fileName = SanitizeFolderName(fileName);

                        // ★ HARDCODED — always saves into the correct professor folder
                        string profFolderPath = PROFESSOR_ROOT;

                        string folder = Path.Combine(profFolderPath, section, "ActivityFiles");
                        if (!Directory.Exists(folder))
                            Directory.CreateDirectory(folder);

                        string savePath = Path.Combine(folder, fileName);
                        await File.WriteAllBytesAsync(savePath, bytes);

                        Console.WriteLine("[Admin] Activity file saved: " + savePath);
                        RefreshFileCountAsync();
                        return;
                    }

                    // =========================================================
                    // STUDENT SUBMISSION → <PROFESSOR_ROOT>/<Section>/<Student>/
                    // =========================================================
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
                        section = SanitizeFolderName(section);
                        studentName = SanitizeFolderName(studentName);
                        title = SanitizeFolderName(title);
                        fileName = SanitizeFolderName(fileName);

                        // ★ HARDCODED — always saves into the correct professor folder
                        string profFolderPath = PROFESSOR_ROOT;

                        // Route into <PROFESSOR_ROOT>/<section>/<student>/
                        string studentFolder = Path.Combine(profFolderPath, section, studentName);
                        if (!Directory.Exists(studentFolder))
                            Directory.CreateDirectory(studentFolder);

                        string finalName = fileName.StartsWith(studentName, StringComparison.OrdinalIgnoreCase)
                            ? fileName
                            : $"{studentName}_{title}_{fileName}";

                        string savePath = Path.Combine(studentFolder, finalName);
                        await File.WriteAllBytesAsync(savePath, bytes);

                        Console.WriteLine("[Admin] Student submission saved: " + savePath);
                        RefreshFileCountAsync();

                        // Reply with UNC path
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

                    // =========================================================
                    // PROFILE PHOTO
                    // =========================================================
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

                        RefreshFileCountAsync();

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

                    // =========================================================
                    // AUTH PHOTO
                    // =========================================================
                    if (firstToken == "AUTH_PHOTO")
                    {
                        string fileName = reader.ReadString();
                        int length = reader.ReadInt32();

                        if (length <= 0 || length > 20 * 1024 * 1024) return;

                        byte[] bytes = reader.ReadBytes(length);
                        fileName = SanitizeFolderName(fileName);

                        string root = SettingsManager.Current.SaveFolder;
                        if (string.IsNullOrEmpty(root)) return;

                        string authSub = SettingsManager.Current.AuthPhotoSubfolder;
                        if (string.IsNullOrEmpty(authSub)) authSub = "AuthPhotos";

                        string folder = Path.Combine(root, authSub);
                        Directory.CreateDirectory(folder);

                        string savePath = Path.Combine(folder, fileName);
                        await File.WriteAllBytesAsync(savePath, bytes);
                        Console.WriteLine("[Admin] Auth photo saved: " + savePath);

                        RefreshFileCountAsync();

                        try
                        {
                            string uncPath = ToUnc(savePath);
                            using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
                            {
                                writer.Write(uncPath);
                                writer.Flush();
                            }
                        }
                        catch { }
                        return;
                    }

                    Console.WriteLine("[Admin] Unknown FileTransfer command: " + firstToken);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("HandleIncomingFile error: " + ex.Message);
            }
        }

        private void RefreshFileCountAsync()
        {
            Task.Run(() => CountFilesInServerFolder())
                .ContinueWith(t =>
                {
                    if (this.IsDisposed) return;
                    int count = t.Result;

                    if (this.IsHandleCreated && !this.IsDisposed)
                    {
                        this.BeginInvoke(new Action(() =>
                        {
                            try
                            {
                                dashboardControl?.RefreshAll();
                            }
                            catch { }
                        }));
                    }
                });
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
        private void BtnUploadBanner_Click(object sender, EventArgs e)
        {
            string folder = BannerHelper.GetBannersFolder();
            if (string.IsNullOrEmpty(folder))
            {
                CustomMessageBox.Show("The Banners folder could not be located.", "Root Folder Not Set",
                    CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Warning);
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
                        Console.WriteLine("Banner copy error: " + ex.Message);
                    }
                }

                if (copied > 0)
                {
                    CustomMessageBox.Show($"{copied} banner(s) uploaded.", "Upload Complete",
                        CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Information);
                    RefreshFileCountAsync();
                }
            }
        }

        private void BtnOpenBannersFolder_Click(object sender, EventArgs e)
        {
            string folder = BannerHelper.GetBannersFolder();
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
            {
                CustomMessageBox.Show("The Banners folder does not exist yet.", "Folder Not Found",
                    CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Warning);
                return;
            }

            try { System.Diagnostics.Process.Start("explorer.exe", "\"" + folder + "\""); }
            catch (Exception ex) { Console.WriteLine("Open banners folder error: " + ex.Message); }
        }

        // =========================================================
        //  NAVIGATION
        // =========================================================
        private void btnDashboard_Click_1(object sender, EventArgs e)
        {
            pnlUserManagement.Visible = false;
            pnlFileManagement.Visible = false;
            if (pnlDatabase != null) pnlDatabase.Visible = false;

            if (pnlDashboardHost != null)
            {
                pnlDashboardHost.Visible = true;
                pnlDashboardHost.BringToFront();
                LayoutDashboardPanel();
            }

            dashboardControl?.RefreshAll();

            navbarStyle.RemoveIndicator(PanelIndicator);
            PanelIndicator = navbarStyle.CreateIndicator(btnDashboard);
        }

        private void btnUserManagement_Click(object sender, EventArgs e)
        {
            pnlUserManagement.Visible = true;
            pnlFileManagement.Visible = false;
            if (pnlDatabase != null) pnlDatabase.Visible = false;
            if (pnlDashboardHost != null) pnlDashboardHost.Visible = false;

            navbarStyle.RemoveIndicator(PanelIndicator);
            PanelIndicator = navbarStyle.CreateIndicator(btnUserManagement);
            LoadUserData();
        }

        private void btnFileManagement_Click(object sender, EventArgs e)
        {
            pnlFileManagement.Visible = true;
            pnlUserManagement.Visible = false;
            if (pnlDatabase != null) pnlDatabase.Visible = false;
            if (pnlDashboardHost != null) pnlDashboardHost.Visible = false;

            navbarStyle.RemoveIndicator(PanelIndicator);
            PanelIndicator = navbarStyle.CreateIndicator(btnFileManagement);

            string root = SettingsManager.Current.SaveFolder;
            if (!Directory.Exists(root))
            {
                CustomMessageBox.Show("Root save folder is not configured or does not exist:\n" + root,
                    "Folder Missing", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Warning);
                return;
            }

            lsServerFolderSetup();
            LoadServerFolder(root, addToHistory: false);
        }

        private void CreateButton_Click(object sender, EventArgs e) => OpenCreateAccountForm();

        private void cmbSelection_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (cmbSelection.Text)
            {
                case "Users":
                    LoadUserData();
                    pnlUserList.BringToFront();
                    break;

                case "Create Account":
                    OpenCreateAccountForm();

                    cmbSelection.SelectedIndexChanged -= cmbSelection_SelectedIndexChanged;
                    cmbSelection.SelectedItem = "Users";
                    cmbSelection.SelectedIndexChanged += cmbSelection_SelectedIndexChanged;
                    break;
            }
        }

        private void SearchButton_TextChanged(object sender, EventArgs e) => LoadUserData(SearchButton.Text);
        private void lvServerFolder_DoubleClick(object sender, EventArgs e) => doubleClick();
        private void BtnBack_Click(object sender, EventArgs e) => btnBack();

        // =========================================================
        //  USER ROW DOUBLE-CLICK -> OPEN DETAILS FORM
        // =========================================================
        private void UserDataList_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var row = UserDataList.Rows[e.RowIndex];
            using (var dlg = new UserDetailsForm())
            {
                dlg.LoadUser(row, UserDataList);
                dlg.ShowDialog(this);
            }
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
            if (contextUserId < 0) { CustomMessageBox.Show("No user selected.", "Notice", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Warning); return; }

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
            catch (Exception ex) { Console.WriteLine("Error loading user: " + ex.Message); return; }

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
                    CustomMessageBox.Show("User info updated.", "Success", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Information);
                    LoadUserData();
                }
                catch (Exception ex) { Console.WriteLine("Error updating user: " + ex.Message); }
            }
        }

        private void ContextDelete_Click(object sender, EventArgs e)
        {
            if (contextUserId < 0) { CustomMessageBox.Show("No user selected.", "Notice", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Warning); return; }

            var confirm = CustomMessageBox.Show("Are you sure you want to delete this user?",
                "Confirm Delete", CustomMessageBoxButtons.YesNo, CustomMessageBoxIcon.Warning);
            if (confirm != CustomMessageBoxResult.Yes) return;

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
                CustomMessageBox.Show("User deleted.", "Success", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Information);
                LoadUserData();
                dashboardControl?.RefreshAll();
            }
            catch (Exception ex) { Console.WriteLine("Error deleting user: " + ex.Message); }
        }

        private void ContextResetPassword_Click(object sender, EventArgs e)
        {
            if (contextUserId < 0) { CustomMessageBox.Show("No user selected.", "Notice", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Warning); return; }

            var confirm = CustomMessageBox.Show("Reset password to '12345678'?",
                "Confirm Reset", CustomMessageBoxButtons.YesNo, CustomMessageBoxIcon.Question);
            if (confirm != CustomMessageBoxResult.Yes) return;

            try
            {
                string connStr = SettingsManager.Current.GetConnectionString();
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand(
                        "UPDATE user_credential SET p_word = MD5(@pw) WHERE user_id = @id", conn))
                    {
                        cmd.Parameters.AddWithValue("@pw", "12345678");
                        cmd.Parameters.AddWithValue("@id", contextUserId);
                        cmd.ExecuteNonQuery();
                    }
                }
                CustomMessageBox.Show("Password reset to default: 12345678", "Success", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Information);
            }
            catch (Exception ex) { Console.WriteLine("Error resetting password: " + ex.Message); }
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
                CustomMessageBox.Show("No rows selected. Hold Ctrl or Shift and click multiple rows first.",
                    "Notice", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Information);
                return;
            }

            string newValue = Prompt($"Enter the new {displayName} for {ids.Count} selected user(s).\n({hint})", "");
            if (string.IsNullOrWhiteSpace(newValue)) return;

            newValue = newValue.Trim().ToUpper();

            var confirm = CustomMessageBox.Show(
                $"Update {displayName} of {ids.Count} user(s) to \"{newValue}\"?",
                "Confirm Bulk Update", CustomMessageBoxButtons.YesNo, CustomMessageBoxIcon.Question);
            if (confirm != CustomMessageBoxResult.Yes) return;

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
                        CustomMessageBox.Show($"{rows} user(s) updated.", "Success", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Information);
                    }
                }
                LoadUserData();
            }
            catch (Exception ex) { Console.WriteLine("Error during bulk update: " + ex.Message); }
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
                        }
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine("LoadUserData error: " + ex.Message); }
        }

        // =========================================================
        //  SERVER FOLDER MANAGEMENT
        // =========================================================
        private void lsServerFolderSetup()
        {
            lvServerFolder.View = View.LargeIcon;
            lvServerFolder.MultiSelect = false;

            imageListIcon.ImageSize = new Size(48, 48);
            imageListIcon.ColorDepth = ColorDepth.Depth32Bit;
            imageListIcon.Images.Clear();

            lvServerFolder.LargeImageList = imageListIcon;
        }

        private void LoadServerFolder(string path, bool addToHistory = true)
        {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path)) return;

            if (addToHistory && !string.IsNullOrEmpty(currentFolder) && currentFolder != path)
                folderHistory.Push(currentFolder);

            currentFolder = path;
            lvServerFolder.Items.Clear();
            imageListIcon.Images.Clear();

            if (imageListIcon.ImageSize.Width != 48 || imageListIcon.ImageSize.Height != 48)
                imageListIcon.ImageSize = new Size(48, 48);

            int imageIndex = 0;

            foreach (string dir in Directory.GetDirectories(path))
            {
                Image folderImg = CreateFolderIcon(48, 48);
                imageListIcon.Images.Add(folderImg);

                var item = new ListViewItem(Path.GetFileName(dir), imageIndex);
                item.Tag = dir;
                lvServerFolder.Items.Add(item);
                imageIndex++;
            }

            foreach (string file in Directory.GetFiles(path))
            {
                string ext = Path.GetExtension(file);
                Image fileImg = CreateFileIcon(ext, 48, 48);
                imageListIcon.Images.Add(fileImg);

                var item = new ListViewItem(Path.GetFileName(file), imageIndex);
                item.Tag = file;
                lvServerFolder.Items.Add(item);
                imageIndex++;
            }
            allServerFolderItems = lvServerFolder.Items.Cast<ListViewItem>().ToList();
            BtnBack.Enabled = folderHistory.Count > 0;
        }

        // =========================================================
        //  CUSTOM ICON DRAWING
        // =========================================================
        private Image CreateFolderIcon(int width, int height)
        {
            var bmp = new Bitmap(width, height);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);

                int pad = 3;
                int tabW = (int)(width * 0.42);
                int tabH = (int)(height * 0.15);
                int bodyY = (int)(height * 0.30);
                int bodyH = (int)(height * 0.58);
                int bodyW = width - pad * 2;

                var tabRect = new Rectangle(pad, bodyY - tabH, tabW, tabH + 6);
                using (var tabBrush = new LinearGradientBrush(
                    tabRect,
                    Color.FromArgb(255, 220, 140),
                    Color.FromArgb(255, 190, 80),
                    LinearGradientMode.Vertical))
                {
                    using (var tabPath = GetRoundedRect(tabRect, 3))
                        g.FillPath(tabBrush, tabPath);
                }

                var bodyRect = new Rectangle(pad, bodyY, bodyW, bodyH);
                using (var bodyPath = GetRoundedRect(bodyRect, 4))
                {
                    using (var shBrush = new SolidBrush(Color.FromArgb(50, 0, 0, 0)))
                    {
                        var shRect = new Rectangle(bodyRect.X + 2, bodyRect.Y + 2, bodyRect.Width, bodyRect.Height);
                        using (var shPath = GetRoundedRect(shRect, 4))
                            g.FillPath(shBrush, shPath);
                    }

                    using (var bodyBrush = new LinearGradientBrush(
                        bodyRect,
                        Color.FromArgb(255, 232, 160),
                        Color.FromArgb(240, 175, 55),
                        LinearGradientMode.Vertical))
                    {
                        g.FillPath(bodyBrush, bodyPath);
                    }

                    using (var pen = new Pen(Color.FromArgb(200, 130, 30), 1))
                        g.DrawPath(pen, bodyPath);
                }

                using (var hl = new SolidBrush(Color.FromArgb(110, 255, 255, 255)))
                {
                    var hlRect = new Rectangle(bodyRect.X + 3, bodyRect.Y + 2, bodyRect.Width - 6, 3);
                    g.FillRectangle(hl, hlRect);
                }

                using (var sh = new SolidBrush(Color.FromArgb(35, 0, 0, 0)))
                {
                    g.FillRectangle(sh, bodyRect.X + 4, bodyRect.Bottom - 3, bodyRect.Width - 8, 2);
                }
            }
            return bmp;
        }

        private Image CreateFileIcon(string extension, int width, int height)
        {
            string ext = (extension ?? "").ToLowerInvariant().TrimStart('.');

            string badge;
            Color badgeColor;

            switch (ext)
            {
                case "doc":
                case "docx":
                    badge = "W"; badgeColor = Color.FromArgb(43, 87, 154); break;
                case "pdf":
                    badge = "PDF"; badgeColor = Color.FromArgb(200, 40, 40); break;
                case "xls":
                case "xlsx":
                case "csv":
                    badge = "X"; badgeColor = Color.FromArgb(33, 115, 70); break;
                case "ppt":
                case "pptx":
                    badge = "P"; badgeColor = Color.FromArgb(208, 82, 30); break;
                case "jpg":
                case "jpeg":
                case "png":
                case "gif":
                case "bmp":
                case "webp":
                    badge = "IMG"; badgeColor = Color.FromArgb(150, 60, 150); break;
                case "zip":
                case "rar":
                case "7z":
                    badge = "ZIP"; badgeColor = Color.FromArgb(120, 90, 40); break;
                case "cs":
                    badge = "C#"; badgeColor = Color.FromArgb(80, 40, 130); break;
                case "sql":
                    badge = "SQL"; badgeColor = Color.FromArgb(200, 100, 40); break;
                case "html":
                case "htm":
                    badge = "<>"; badgeColor = Color.FromArgb(200, 80, 40); break;
                case "exe":
                    badge = "APP"; badgeColor = Color.FromArgb(60, 60, 60); break;
                default:
                    badge = "FILE"; badgeColor = Color.FromArgb(100, 100, 110); break;
            }

            var bmp = new Bitmap(width, height);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                g.Clear(Color.Transparent);

                int padX = (int)(width * 0.20);
                int padY = (int)(height * 0.08);
                int docW = width - padX * 2;
                int docH = height - padY * 2;
                int fold = (int)(docW * 0.34);

                using (var shBrush = new SolidBrush(Color.FromArgb(50, 0, 0, 0)))
                    g.FillRectangle(shBrush, padX + 2, padY + 2, docW, docH);

                using (var docPath = new GraphicsPath())
                {
                    docPath.AddLine(padX, padY, padX + docW - fold, padY);
                    docPath.AddLine(padX + docW - fold, padY, padX + docW, padY + fold);
                    docPath.AddLine(padX + docW, padY + fold, padX + docW, padY + docH);
                    docPath.AddLine(padX + docW, padY + docH, padX, padY + docH);
                    docPath.CloseFigure();

                    using (var docBrush = new LinearGradientBrush(
                        new Rectangle(padX, padY, docW, docH),
                        Color.White,
                        Color.FromArgb(238, 240, 245),
                        LinearGradientMode.Vertical))
                    {
                        g.FillPath(docBrush, docPath);
                    }
                    using (var pen = new Pen(Color.FromArgb(170, 175, 185), 1))
                    {
                        g.DrawPath(pen, docPath);
                    }
                }

                using (var foldPath = new GraphicsPath())
                {
                    foldPath.AddLine(padX + docW - fold, padY,
                                     padX + docW - fold, padY + fold);
                    foldPath.AddLine(padX + docW - fold, padY + fold,
                                     padX + docW, padY + fold);
                    foldPath.CloseFigure();

                    using (var foldBrush = new SolidBrush(Color.FromArgb(214, 218, 226)))
                        g.FillPath(foldBrush, foldPath);
                    using (var foldPen = new Pen(Color.FromArgb(170, 175, 185), 1))
                        g.DrawPath(foldPen, foldPath);
                }

                using (var linePen = new Pen(Color.FromArgb(195, 200, 210), 1))
                {
                    int lineY = padY + fold + 5;
                    for (int i = 0; i < 3; i++)
                    {
                        int lineW = (int)(docW * (0.72 - i * 0.12));
                        if (lineW < 4) lineW = 4;
                        g.DrawLine(linePen, padX + 4, lineY, padX + 4 + lineW, lineY);
                        lineY += 4;
                    }
                }

                int badgeH = (int)(docH * 0.34);
                int badgeY = padY + docH - badgeH;
                var badgeRect = new Rectangle(padX, badgeY, docW, badgeH);

                using (var badgeBrush = new SolidBrush(badgeColor))
                    g.FillRectangle(badgeBrush, badgeRect);

                using (var edgePen = new Pen(Color.FromArgb(80, 0, 0, 0), 1))
                    g.DrawLine(edgePen, badgeRect.Left, badgeRect.Top, badgeRect.Right, badgeRect.Top);

                float fontSize = Math.Max(7f, badgeH * 0.58f);
                using (var font = new Font("Segoe UI", fontSize, FontStyle.Bold, GraphicsUnit.Pixel))
                using (var sf = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                })
                using (var textBrush = new SolidBrush(Color.White))
                {
                    g.DrawString(badge, font, textBrush, badgeRect, sf);
                }
            }
            return bmp;
        }

        private GraphicsPath GetRoundedRect(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int d = radius * 2;

            if (d > rect.Width) d = rect.Width;
            if (d > rect.Height) d = rect.Height;

            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
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
            var result = CustomMessageBox.Show("Log out?", "Logout Confirmation",
                CustomMessageBoxButtons.YesNo, CustomMessageBoxIcon.Question);
            if (result != CustomMessageBoxResult.Yes) return;

            adminIsRunning = false;

            try { authPhotoListener?.Stop(); } catch { }
            try { authPhotoListener?.Server?.Dispose(); } catch { }
            authPhotoListener = null;

            System.Threading.Thread.Sleep(150);

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
                btnDeleteFile = new Guna.UI2.WinForms.Guna2Button
                {
                    Text = "🗑",
                    Size = new Size(50, 42),
                    Location = new Point(1140, 65),
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
                CustomMessageBox.Show("Please select a file or folder first.",
                    "Nothing Selected", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Information);
                return;
            }

            string path = lvServerFolder.SelectedItems[0].Tag?.ToString();
            if (string.IsNullOrEmpty(path))
            {
                CustomMessageBox.Show("Invalid selection.", "Notice", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Warning);
                return;
            }

            bool isFolder = Directory.Exists(path);
            bool isFile = File.Exists(path);

            if (!isFolder && !isFile)
            {
                CustomMessageBox.Show("The selected item no longer exists.", "Notice", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Warning);
                return;
            }

            if (isFolder && !string.IsNullOrEmpty(currentFolder) &&
                string.Equals(Path.GetFullPath(path).TrimEnd('\\'),
                              Path.GetFullPath(SettingsManager.Current.SaveFolder).TrimEnd('\\'),
                              StringComparison.OrdinalIgnoreCase))
            {
                CustomMessageBox.Show("You cannot delete the root folder.",
                    "Not Allowed", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Warning);
                return;
            }

            string itemName = Path.GetFileName(path);

            string message = isFolder
                ? $"Delete folder '{itemName}' and ALL of its contents?\n\nThis cannot be undone."
                : $"Delete file '{itemName}'?\n\nThis cannot be undone.";

            var confirm = CustomMessageBox.Show(message, "Confirm Delete",
                CustomMessageBoxButtons.YesNo, CustomMessageBoxIcon.Warning);

            if (confirm != CustomMessageBoxResult.Yes) return;

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

                CustomMessageBox.Show("Deleted successfully.",
                    "Deleted", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Information);

                if (!string.IsNullOrEmpty(currentFolder) && Directory.Exists(currentFolder))
                    LoadServerFolder(currentFolder, addToHistory: false);

                RefreshFileCountAsync();
            }
            catch (UnauthorizedAccessException)
            {
                CustomMessageBox.Show(
                    "Access denied.\n\nThe file/folder may be open in another program, " +
                    "or you don't have permission.",
                    "Delete Failed", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Error);
            }
            catch (IOException ioEx)
            {
                CustomMessageBox.Show(
                    "The file is in use or locked.\n\nDetails: " + ioEx.Message,
                    "Delete Failed", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show("Error deleting: " + ex.Message,
                    "Delete Failed", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Error);
            }
        }

        private void ApplyFileSearchFilter(string query)
        {
            if (lvServerFolder == null || lvServerFolder.IsDisposed) return;

            lvServerFolder.BeginUpdate();
            try
            {
                lvServerFolder.Items.Clear();

                if (string.IsNullOrWhiteSpace(query))
                {
                    lvServerFolder.Items.AddRange(allServerFolderItems.ToArray());
                    return;
                }

                string q = query.Trim();

                var matches = allServerFolderItems.Where(item =>
                {
                    if (item.Text != null &&
                        item.Text.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;

                    string path = item.Tag as string;
                    if (!string.IsNullOrEmpty(path))
                    {
                        string ext = Path.GetExtension(path)?.TrimStart('.');
                        if (!string.IsNullOrEmpty(ext) &&
                            ext.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                            return true;
                    }

                    return false;
                });

                lvServerFolder.Items.AddRange(matches.ToArray());
            }
            finally
            {
                lvServerFolder.EndUpdate();
            }
        }

        private void txtFileSearchBar_TextChanged(object sender, EventArgs e)
        {
            ApplyFileSearchFilter(txtFileSearchBar.Text);
        }
    }
}