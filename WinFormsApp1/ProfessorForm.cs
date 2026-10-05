using ClosedXML.Excel;
using DevExpress.XtraPdfViewer;
using DocumentFormat.OpenXml.VariantTypes;
using Guna.UI2.WinForms;
using MySql.Data.MySqlClient;
using MySqlX.XDevAPI;
using Org.BouncyCastle.Asn1.Cmp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using UMapx.Distribution;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace WinFormsApp1
{
    public partial class ProfessorForm : Form
    {
        private TcpListener listener;
        private TcpListener broadcastListener;
        private TcpListener screenListener;
        private TcpListener activityFileListener;

        private Dictionary<string, Button> workstationButtons = new Dictionary<string, Button>();
        private Dictionary<string, Button> miniWorkstationButtons = new Dictionary<string, Button>();
        private Dictionary<string, PictureBox> screenViewers = new Dictionary<string, PictureBox>();
        private Dictionary<string, TcpClient> broadcastClients = new Dictionary<string, TcpClient>();
        private Dictionary<string, DateTime> lastThumbnailUpdate = new Dictionary<string, DateTime>();
        private ToolTip navToolTip;

        private readonly object screenViewersLock = new object();

        private readonly TimeSpan thumbnailInterval = TimeSpan.FromSeconds(5);
        private System.Windows.Forms.Timer broadcastTimer;

        // Home dashboard counters
        private System.Windows.Forms.Timer homeCountersTimer;

        private const int MAX_MINI_BUTTONS = 5;
        private int OnlineCount = 0;
        private int OfflineCount = 0;
        private int WorkStationNum = 0;
        private string selectedWorkstationId = "";
        private string SelectedIP = "";
        private volatile bool isRunning = false;
        private string selectedFilePath = "";
        private string currentFolder;
        private string FolderName;
        private Stack<string> folderHistory = new Stack<string>();
        private string saveFolder;

        private string CurrentProfilePath;
        private string SaveCurrentProfilePath;
        private string AuthenticationPhoto;
        private string SaveAuthenticationPhoto;
        private string ProfessorName;

        private Image cachedProfileImage;
        private bool profileImageLoaded = false;

        int ProfessorID;
        string ProfessorUsername;

        private static readonly Color[] BannerColors = new Color[]
        {
            Color.FromArgb(46, 125, 90),
            Color.FromArgb(55, 65, 79),
            Color.FromArgb(90, 90, 100),
            Color.FromArgb(196, 106, 74)
        };
        private int _colorIndex = 0;

        // Notifications
        private Guna2Button btnNotifications;
        private Label lblNotificationBadge;
        private Guna2Panel notificationPanel;
        private FlowLayoutPanel notificationList;
        private System.Windows.Forms.Timer notificationsRefreshTimer;
        private HashSet<string> readNotificationKeys = new HashSet<string>();
        private bool notificationPanelOpen = false;

        public ProfessorForm(int UserId, string Username)
        {
            InitializeComponent();

            ProfessorID = UserId;
            ProfessorUsername = Username;

            InitializeSaveDirectory();
        }

        private async void ProfessorForm_Load(object sender, EventArgs e)
        {
            saveFolder = GetFolderPath(ProfessorID);
            isRunning = true;

            RefreshHomeCounters();

            homeCountersTimer = new System.Windows.Forms.Timer { Interval = 30000 };
            homeCountersTimer.Tick += (s, ev) =>
            {
                try { if (pnlHome.Visible) RefreshHomeCounters(); }
                catch { }
            };
            homeCountersTimer.Start();

            _ = StartServer();
            _ = StartBroadcastListener();
            _ = StartScreenListener();
            _ = StartActivityFileServer();

            LoadAllStudent();
            AutoCreateClassBtn();
            ActivitySectionSubject();
            RecentActivity();

            ActivityStatus();
            lblGradesSubmitted.Text = CountTotalSubmitted(ProfessorID).ToString();
            lblGradesGraded.Text = CountTotalGraded(ProfessorID).ToString();
            lblGradesNotSubmitted.Text = CountTotalNotSubmitted(ProfessorID).ToString();

            lblProfUsername.Text = ProfessorUsername;
            InitializeChangingPicture();
            lsServerFolderSetup();

            if (!string.IsNullOrEmpty(saveFolder) && saveFolder != "Null" && Directory.Exists(saveFolder))
                LoadServerFolder(saveFolder);

            NameGet();
            InitializeComboBoxes();
            InitializeNavTooltips();

            try { BuildAttendanceUi(); }
            catch (Exception ex) { Console.WriteLine("BuildAttendanceUi error: " + ex.Message); }

            try { BuildNotificationsUi(); }
            catch (Exception ex) { Console.WriteLine("BuildNotificationsUi: " + ex.Message); }

            try { LoadNotifications(); }
            catch (Exception ex) { Console.WriteLine("LoadNotifications: " + ex.Message); }

            notificationsRefreshTimer = new System.Windows.Forms.Timer { Interval = 60000 };
            notificationsRefreshTimer.Tick += (s, ev) => { try { LoadNotifications(); } catch { } };
            notificationsRefreshTimer.Start();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try { notificationsRefreshTimer?.Stop(); } catch { }
            try { notificationsRefreshTimer?.Dispose(); } catch { }
            notificationsRefreshTimer = null;

            try { homeCountersTimer?.Stop(); } catch { }
            try { homeCountersTimer?.Dispose(); } catch { }
            homeCountersTimer = null;

            StopServer();
            base.OnFormClosing(e);
        }

        // =========================================================
        // HOME DASHBOARD COUNTERS
        // =========================================================
        private int CountProfessorSubjects(int professorId)
        {
            string connStr = SettingsManager.Current.GetConnectionString();
            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = @"SELECT COUNT(DISTINCT class_name) FROM professor_class WHERE professor_id = @prof_id";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@prof_id", professorId);
                        object result = cmd.ExecuteScalar();
                        if (result == null || result == DBNull.Value) return 0;
                        return Convert.ToInt32(result);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("CountProfessorSubjects error: " + ex.Message);
                return 0;
            }
        }

        private int CountProfessorStudents(int professorId)
        {
            string connStr = SettingsManager.Current.GetConnectionString();
            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = @"SELECT COUNT(DISTINCT sc.user_id) FROM student_class sc WHERE sc.professor_id = @prof_id";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@prof_id", professorId);
                        object result = cmd.ExecuteScalar();
                        if (result == null || result == DBNull.Value) return 0;
                        return Convert.ToInt32(result);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("CountProfessorStudents error: " + ex.Message);
                return 0;
            }
        }

        private void RefreshHomeCounters()
        {
            try
            {
                int subjectCount = CountProfessorSubjects(ProfessorID);
                int studentCount = CountProfessorStudents(ProfessorID);

                try { lblMySubjects.Text = subjectCount.ToString(); } catch { }
                try { lblMyStudents.Text = studentCount.ToString(); } catch { }

                Console.WriteLine($"[Dashboard] Subjects: {subjectCount}, Students: {studentCount}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("RefreshHomeCounters error: " + ex.Message);
            }
        }

        private void ShowPage(Panel page, string title, Guna.UI2.WinForms.Guna2Button activeBtn)
        {
            pnlHome.Visible = false;
            pnlWorkstation.Visible = false;
            pnlStudent.Visible = false;
            pnlActivity.Visible = false;
            pnlGrades.Visible = false;
            pnlAttendance.Visible = false;
            pnlSubject.Visible = false;
            pnlFile.Visible = false;
            pnlSetting.Visible = false;

            page.Visible = true;
            page.BringToFront();
            lblPanelName.Text = title;

            btnHome.Checked = false;
            btnWorkstation.Checked = false;
            btnStudent.Checked = false;
            btnActivities.Checked = false;
            btnGrades.Checked = false;
            btnAttendance.Checked = false;
            btnSubject.Checked = false;
            btnFile.Checked = false;
            btnAccount.Checked = false;

            if (activeBtn != null) activeBtn.Checked = true;
        }

        private void btnHome_Click(object sender, EventArgs e) { ShowPage(pnlHome, "Home", btnHome); RefreshHomeCounters(); }
        private void btnWorkstation_Click(object sender, EventArgs e) => ShowPage(pnlWorkstation, "Workstations", btnWorkstation);
        private void btnStudent_Click(object sender, EventArgs e) => ShowPage(pnlStudent, "My Students", btnStudent);

        private void btnActivities_Click(object sender, EventArgs e)
        {
            ShowPage(pnlActivity, "Activities", btnActivities);
            ActivitySectionSubject();
            RecentActivity();
        }

        private void btnGrades_Click(object sender, EventArgs e) { ShowPage(pnlGrades, "Grades", btnGrades); ActivityStatus(); }
        private void btnAttendance_Click(object sender, EventArgs e) { ShowPage(pnlAttendance, "Attendance", btnAttendance); RefreshAttendanceSections(); }
        private void btnSubject_Click(object sender, EventArgs e) => ShowPage(pnlSubject, "Subjects", btnSubject);

        private void btnFile_Click(object sender, EventArgs e)
        {
            ShowPage(pnlFile, "Files", btnFile);
            if (!string.IsNullOrEmpty(saveFolder) && saveFolder != "Null" && Directory.Exists(saveFolder))
                LoadServerFolder(saveFolder);
            else
                CustomMessageBox.Show("Save folder is not configured for this account.", "Notice", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Information);
        }

        private void btnAccount_Click(object sender, EventArgs e) => ShowPage(pnlSetting, "Settings", btnAccount);
        private void linkLblWorkstations_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) => ShowPage(pnlWorkstation, "Workstations", btnWorkstation);
        private void btnHomeCreateSubject_Click(object sender, EventArgs e) => ShowPage(pnlSubject, "Subjects", btnSubject);

        private void btnHomeCreateActivity_Click(object sender, EventArgs e)
        {
            ShowPage(pnlActivity, "Activities", btnActivities);
            ActivitySectionSubject();
            RecentActivity();
        }

        // =========================================================
        // LOADING OVERLAY HELPERS
        // =========================================================
        private async Task RunWithLoadingAsync(string message, Func<Task> operation)
        {
            LoadingOverlay overlay = null;
            try { overlay = new LoadingOverlay(this, message); await operation(); }
            finally { overlay?.Dispose(); this.Focus(); }
        }

        // =========================================================
        // START SERVER — Reads student name after TCP connect
        // =========================================================
        private async Task StartServer()
        {
            try
            {
                listener = new TcpListener(IPAddress.Any, SettingsManager.Current.WorkstationPort);
                listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                listener.Start();
            }
            catch (Exception ex) { Console.WriteLine("[Professor] Workstation bind FAILED: " + ex.Message); return; }

            SafeInvoke(() => { lblComputerOnline.Text = "0"; lblComputerOffline.Text = "0"; });

            while (isRunning)
            {
                try
                {
                    TcpClient client = await listener.AcceptTcpClientAsync();
                    string clientIp = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();

                    // Read [4-byte length][N-byte UTF-8 name]
                    string studentName = "";
                    try
                    {
                        NetworkStream ns = client.GetStream();
                        ns.ReadTimeout = 3000;

                        byte[] lenBuf = new byte[4];
                        int got = 0;
                        while (got < 4) { int n = ns.Read(lenBuf, got, 4 - got); if (n == 0) break; got += n; }

                        if (got == 4)
                        {
                            int nameLen = BitConverter.ToInt32(lenBuf, 0);
                            if (nameLen > 0 && nameLen <= 512)
                            {
                                byte[] nameBuf = new byte[nameLen];
                                int nameGot = 0;
                                while (nameGot < nameLen) { int n = ns.Read(nameBuf, nameGot, nameLen - nameGot); if (n == 0) break; nameGot += n; }
                                if (nameGot == nameLen) studentName = Encoding.UTF8.GetString(nameBuf).Trim();
                            }
                        }
                    }
                    catch (Exception exName) { Console.WriteLine("[Professor] Could not read name from " + clientIp + ": " + exName.Message); }

                    if (string.IsNullOrEmpty(studentName)) studentName = "Unknown Student";

                    Console.WriteLine($"[DEBUG] Connected → IP: {clientIp} | Name: {studentName}");

                    Button wsButton = null;
                    SafeInvoke(() => wsButton = OnWorkStationConnected(clientIp, studentName));
                    _ = MonitorDisconnected(client, wsButton, clientIp);
                }
                catch (ObjectDisposedException) { break; }
                catch (Exception ex) { if (!isRunning) break; Console.WriteLine("[Professor] Workstation accept error: " + ex.Message); }
            }
        }

        private void SafeInvoke(Action action)
        {
            try
            {
                if (IsDisposed || !IsHandleCreated) return;
                if (InvokeRequired) Invoke(action); else action();
            }
            catch (ObjectDisposedException) { }
            catch (InvalidOperationException) { }
        }

        // =========================================================
        // WORKSTATION CONNECTED — Big + mini, image fits perfectly
        // =========================================================
        private Button OnWorkStationConnected(string clientIp, string studentName)
        {
            string label = studentName + Environment.NewLine + "(" + clientIp + ")";

            if (workstationButtons.ContainsKey(clientIp))
            {
                Button existingBtn = workstationButtons[clientIp];
                existingBtn.BackColor = Color.LightGreen;
                existingBtn.Text = label;

                if (existingBtn.Parent is Panel wrapPanel)
                {
                    foreach (Control c in wrapPanel.Controls)
                    {
                        if (c is Label lblName && (lblName.Tag as string) == "name-overlay")
                            lblName.Text = label;
                    }
                }

                if (miniWorkstationButtons.ContainsKey(clientIp))
                {
                    miniWorkstationButtons[clientIp].BackColor = Color.LightGreen;
                    miniWorkstationButtons[clientIp].Text = label;
                }

                UpdateConnectedCount();
                return existingBtn;
            }

            WorkStationNum++;

            // =========================================================
            // WRAPPER PANEL
            // =========================================================
            Panel wrapper = new Panel();
            wrapper.Width = 280;
            wrapper.Height = 200;
            wrapper.Margin = new Padding(8);
            wrapper.BackColor = Color.White;
            wrapper.BorderStyle = BorderStyle.FixedSingle;
            wrapper.Tag = clientIp;

            Button MainPcButton = new Button();
            MainPcButton.Dock = DockStyle.Top;
            MainPcButton.Height = wrapper.Height - 55;
            MainPcButton.BackColor = Color.LightGreen;
            MainPcButton.Tag = clientIp;
            MainPcButton.Text = "";
            MainPcButton.FlatStyle = FlatStyle.Flat;
            MainPcButton.FlatAppearance.BorderSize = 0;
            MainPcButton.BackgroundImageLayout = ImageLayout.Zoom;
            MainPcButton.Cursor = Cursors.Hand;
            MainPcButton.Click += (s, args) => { SelectedIP = clientIp; };
            wrapper.Controls.Add(MainPcButton);
            MainPcButton.BringToFront();

            Label lblNameBar = new Label();
            lblNameBar.Tag = "name-overlay";
            lblNameBar.Text = label;
            lblNameBar.Dock = DockStyle.Bottom;
            lblNameBar.Height = 55;
            lblNameBar.TextAlign = ContentAlignment.MiddleCenter;
            lblNameBar.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            lblNameBar.ForeColor = Color.White;
            lblNameBar.BackColor = Color.FromArgb(30, 100, 60);
            lblNameBar.Cursor = Cursors.Hand;
            lblNameBar.Click += (s, args) => { SelectedIP = clientIp; };
            wrapper.Controls.Add(lblNameBar);
            lblNameBar.BringToFront();

            flpMainWorkstations.Controls.Add(wrapper);
            workstationButtons[clientIp] = MainPcButton;

            // Mini button
            Button miniButton = new Button();
            miniButton.Text = label;
            miniButton.Height = 150;
            miniButton.Width = 120;
            miniButton.Margin = new Padding(3);
            miniButton.BackColor = Color.LightGreen;
            miniButton.Tag = clientIp;
            miniButton.Font = new Font("Segoe UI", 8F, FontStyle.Regular);
            miniButton.TextAlign = ContentAlignment.MiddleCenter;
            miniButton.FlatStyle = FlatStyle.Flat;
            miniButton.FlatAppearance.BorderSize = 1;
            miniButton.FlatAppearance.BorderColor = Color.DarkGreen;

            if (flpMiniWorkStations.Controls.Count < MAX_MINI_BUTTONS)
            {
                flpMiniWorkStations.Controls.Add(miniButton);
                miniWorkstationButtons[clientIp] = miniButton;
            }
            else
            {
                miniButton.Visible = false;
                miniWorkstationButtons[clientIp] = miniButton;
            }

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
            catch (Exception ex) { Console.WriteLine("MonitorDisconnected: " + clientIp + " — " + ex.Message); }
            finally
            {
                SafeInvoke(() =>
                {
                    if (workstationButtons.ContainsKey(clientIp))
                    {
                        ApplyNoSignal(clientIp);
                        lastThumbnailUpdate.Remove(clientIp);
                    }

                    if (miniWorkstationButtons.ContainsKey(clientIp))
                        miniWorkstationButtons[clientIp].BackColor = Color.Red;

                    UpdateConnectedCount();
                });

                try { client.Close(); } catch { }
                try { client.Dispose(); } catch { }
            }
        }

        private void UpdateConnectedCount()
        {
            int connectedCount = workstationButtons.Values.Count(btn => btn.BackColor == Color.LightGreen);
            int disconnectedCount = workstationButtons.Count - connectedCount;

            lblStudentOnline.Text = $"{connectedCount} Online";
            lblComputerOnline.Text = connectedCount.ToString();
            lblComputerOffline.Text = disconnectedCount.ToString();
        }

        private void LoadAllStudent(string filter = "")
        {
            string connStr = SettingsManager.Current.GetConnectionString();
            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    // =========================================================
                    // FIX: Only students who joined one of THIS professor's
                    // classes (via student_class) should appear.
                    // =========================================================
                    string query = @"
                SELECT DISTINCT
                       u.username,
                       i.lastname,
                       i.firstname,
                       i.middlename,
                       i.school_year,
                       i.school_section,
                       i.school_course
                FROM student_class sc
                INNER JOIN user_credential u  ON u.user_id = sc.user_id
                INNER JOIN user_information i ON i.user_id = sc.user_id
                WHERE sc.professor_id = @professor_id
                  AND u.roles = 'Student'";

                    if (!string.IsNullOrEmpty(filter))
                        query += " AND (u.username LIKE @f1 OR i.lastname LIKE @f2 OR i.firstname LIKE @f3)";

                    if (!string.IsNullOrEmpty(cmbSemester.Text) && cmbSemester.Text != "Select Semester")
                        query += " AND i.school_semester = @semester";

                    if (!string.IsNullOrEmpty(cmbSection.Text) && cmbSection.Text != "Select Section")
                        query += " AND i.school_section = @section";

                    if (!string.IsNullOrEmpty(cmbYear.Text) && cmbYear.Text != "Select Year")
                        query += " AND i.school_year = @year";

                    query += " ORDER BY i.lastname, i.firstname";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@professor_id", ProfessorID);

                        if (!string.IsNullOrEmpty(cmbYear.Text) && cmbYear.Text != "Select Year")
                            cmd.Parameters.AddWithValue("@year", cmbYear.Text.Trim());

                        if (!string.IsNullOrEmpty(cmbSection.Text) && cmbSection.Text != "Select Section")
                            cmd.Parameters.AddWithValue("@section", cmbSection.Text.Trim());

                        if (!string.IsNullOrEmpty(cmbSemester.Text) && cmbSemester.Text != "Select Semester")
                            cmd.Parameters.AddWithValue("@semester", cmbSemester.Text.Trim());

                        if (!string.IsNullOrEmpty(filter))
                        {
                            string f = "%" + filter + "%";
                            cmd.Parameters.AddWithValue("@f1", f);
                            cmd.Parameters.AddWithValue("@f2", f);
                            cmd.Parameters.AddWithValue("@f3", f);
                        }

                        MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        dgvStudents.DataSource = dt;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("LoadAllStudent error: " + ex.Message);
            }
        }

        private void txtBoxSearch_TextChanged(object sender, EventArgs e) => LoadAllStudent(txtBoxSearch.Text);
        private void cmbSemester_SelectedIndexChanged(object sender, EventArgs e) => LoadAllStudent(txtBoxSearch.Text);
        private void cmbYear_SelectedIndexChanged(object sender, EventArgs e) => LoadAllStudent(txtBoxSearch.Text);
        private void cmbSection_SelectedIndexChanged(object sender, EventArgs e) => LoadAllStudent(txtBoxSearch.Text);
        private void btnSearch_Click(object sender, EventArgs e) => LoadAllStudent(txtBoxSearch.Text);

        private void btnCleanFilter_Click(object sender, EventArgs e)
        {
            cmbYear.SelectedIndex = -1;
            cmbSection.SelectedIndex = -1;
            cmbSemester.SelectedIndex = -1;
            txtBoxSearch.Text = "";
            LoadAllStudent();
        }

        // =========================================================
        // ATTENDANCE
        // =========================================================
        private Guna.UI2.WinForms.Guna2ComboBox attSectionCombo;
        private FlowLayoutPanel attListPanel;
        private DataGridView attGrid;
        private Guna.UI2.WinForms.Guna2Button attAddBtn;
        private Guna.UI2.WinForms.Guna2Button attUpdateBtn;
        private Guna.UI2.WinForms.Guna2Button attExportBtn;
        private Label attHeaderDate;
        private Label attHeaderSection;
        private bool _attendanceUiBuilt = false;

        private void BuildAttendanceUi()
        {
            if (_attendanceUiBuilt) return;
            _attendanceUiBuilt = true;

            pnlAttendance.Controls.Clear();
            pnlAttendance.BackColor = Color.FromArgb(248, 248, 250);
            pnlAttendance.BackgroundImage = null;
            pnlAttendance.Padding = new Padding(0);
            pnlAttendance.AutoScroll = false;

            Guna2Panel topBar = new Guna2Panel { Dock = DockStyle.Top, Height = 160, FillColor = Color.White, BorderRadius = 0 };
            pnlAttendance.Controls.Add(topBar);

            Label lblTitle = new Label
            {
                Text = "Attendance",
                Font = new Font("Segoe UI Semibold", 20F, FontStyle.Bold),
                ForeColor = Color.Maroon,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(24, 18)
            };
            topBar.Controls.Add(lblTitle);

            Label lblSubtitle = new Label
            {
                Text = "Track daily student attendance per section",
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = Color.FromArgb(120, 120, 120),
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(26, 52)
            };
            topBar.Controls.Add(lblSubtitle);

            Label lblSection = new Label
            {
                Text = "Section",
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(80, 80, 80),
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(26, 82)
            };
            topBar.Controls.Add(lblSection);

            attSectionCombo = new Guna.UI2.WinForms.Guna2ComboBox
            {
                Location = new Point(26, 104),
                Size = new Size(220, 38),
                BorderRadius = 8,
                Font = new Font("Segoe UI", 9.5F),
                FillColor = Color.FromArgb(250, 250, 252),
                BorderColor = Color.FromArgb(220, 215, 215),
                ForeColor = Color.FromArgb(50, 50, 50),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            attSectionCombo.SelectedIndexChanged += (s, e) => OnAttendanceSectionChanged();
            topBar.Controls.Add(attSectionCombo);

            attExportBtn = new Guna2Button
            {
                Text = "📊  Export",
                Size = new Size(120, 42),
                Location = new Point(topBar.Width - 150, 30),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BorderRadius = 10,
                FillColor = Color.White,
                ForeColor = Color.Maroon,
                BorderColor = Color.FromArgb(220, 200, 200),
                BorderThickness = 1,
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold)
            };
            attExportBtn.HoverState.FillColor = Color.FromArgb(250, 240, 240);
            attExportBtn.Click += (s, e) => ExportAttendanceToExcel();
            topBar.Controls.Add(attExportBtn);

            attAddBtn = new Guna2Button
            {
                Text = "➕  Add Attendance",
                Size = new Size(180, 42),
                Location = new Point(topBar.Width - 340, 30),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BorderRadius = 10,
                FillColor = Color.Maroon,
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold)
            };
            attAddBtn.HoverState.FillColor = Color.FromArgb(100, 0, 0);
            attAddBtn.Click += (s, e) => StartAttendanceSession();
            topBar.Controls.Add(attAddBtn);

            attUpdateBtn = new Guna2Button
            {
                Text = "💾  Update",
                Size = new Size(130, 42),
                Location = new Point(topBar.Width - 480, 30),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BorderRadius = 10,
                FillColor = Color.White,
                ForeColor = Color.FromArgb(46, 160, 90),
                BorderColor = Color.FromArgb(46, 160, 90),
                BorderThickness = 1,
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold)
            };
            attUpdateBtn.HoverState.FillColor = Color.FromArgb(240, 250, 240);
            attUpdateBtn.Click += (s, e) => SaveAttendanceSession();
            topBar.Controls.Add(attUpdateBtn);

            Panel bodyPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(248, 248, 250) };
            pnlAttendance.Controls.Add(bodyPanel);
            bodyPanel.BringToFront();

            Guna2Panel listHeader = new Guna2Panel
            {
                Location = new Point(20, 12),
                Size = new Size(700, 70),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BorderRadius = 12,
                FillColor = Color.White,
                BorderColor = Color.FromArgb(235, 230, 230),
                BorderThickness = 1
            };
            bodyPanel.Controls.Add(listHeader);

            attHeaderDate = new Label
            {
                Text = DateTime.Today.ToString("dddd, MMMM dd, yyyy"),
                Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold),
                ForeColor = Color.Maroon,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(20, 12)
            };
            listHeader.Controls.Add(attHeaderDate);

            attHeaderSection = new Label
            {
                Text = "No section selected",
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = Color.FromArgb(120, 120, 120),
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(22, 40)
            };
            listHeader.Controls.Add(attHeaderSection);

            attListPanel = new FlowLayoutPanel
            {
                Location = new Point(20, 94),
                Size = new Size(700, 300),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                BackColor = Color.FromArgb(248, 248, 250),
                Padding = new Padding(0)
            };
            bodyPanel.Controls.Add(attListPanel);

            Guna2Panel gridHolder = new Guna2Panel
            {
                Location = new Point(740, 12),
                Size = new Size(420, 300),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right,
                BorderRadius = 12,
                FillColor = Color.White,
                BorderColor = Color.FromArgb(235, 230, 230),
                BorderThickness = 1
            };
            bodyPanel.Controls.Add(gridHolder);

            Label lblGridTitle = new Label
            {
                Text = "📈  Attendance Summary",
                Font = new Font("Segoe UI Semibold", 11.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 60, 60),
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(18, 14)
            };
            gridHolder.Controls.Add(lblGridTitle);

            attGrid = new DataGridView
            {
                Location = new Point(16, 50),
                Size = new Size(388, 234),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToResizeRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                EnableHeadersVisualStyles = false,
                ColumnHeadersHeight = 38,
                Font = new Font("Segoe UI", 10F)
            };
            attGrid.ColumnHeadersDefaultCellStyle.BackColor = Color.Maroon;
            attGrid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            attGrid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            attGrid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.Maroon;
            attGrid.DefaultCellStyle.Font = new Font("Segoe UI", 9.5F);
            attGrid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(250, 235, 235);
            attGrid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(40, 40, 40);
            attGrid.RowTemplate.Height = 34;
            attGrid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(252, 250, 250);
            gridHolder.Controls.Add(attGrid);

            Action relayout = () =>
            {
                int bodyW = bodyPanel.ClientSize.Width;
                int bodyH = bodyPanel.ClientSize.Height;
                if (bodyW <= 0 || bodyH <= 0) return;

                int rightW = 420;
                int gap = 20;
                int rightX = bodyW - rightW - gap;
                int leftW = rightX - gap - 20;
                if (leftW < 200) leftW = Math.Max(200, bodyW / 2);

                listHeader.Location = new Point(20, 12);
                listHeader.Size = new Size(leftW, 70);

                attListPanel.Location = new Point(20, 94);
                attListPanel.Size = new Size(leftW, Math.Max(50, bodyH - 114));

                gridHolder.Location = new Point(rightX, 12);
                gridHolder.Size = new Size(rightW, Math.Max(50, bodyH - 24));

                attGrid.Location = new Point(16, 50);
                attGrid.Size = new Size(Math.Max(50, gridHolder.Width - 32), Math.Max(50, gridHolder.Height - 66));
            };

            bodyPanel.SizeChanged += (s, e) => relayout();
            pnlAttendance.SizeChanged += (s, e) => relayout();

            pnlAttendance.PerformLayout();
            bodyPanel.PerformLayout();
            relayout();

            ShowAttendancePlaceholder();
            RefreshAttendanceSections();
        }

        private void RefreshAttendanceSections()
        {
            if (attSectionCombo == null) return;
            string connStr = SettingsManager.Current.GetConnectionString();
            try
            {
                string previous = attSectionCombo.Text;
                attSectionCombo.Items.Clear();

                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = @"SELECT DISTINCT class_section FROM professor_class WHERE professor_id = @professor_id AND class_section IS NOT NULL AND class_section <> '' ORDER BY class_section";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@professor_id", ProfessorID);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string section = reader["class_section"]?.ToString()?.Trim();
                                if (!string.IsNullOrEmpty(section) && !attSectionCombo.Items.Contains(section))
                                    attSectionCombo.Items.Add(section);
                            }
                        }
                    }
                }

                if (!string.IsNullOrEmpty(previous) && attSectionCombo.Items.Contains(previous))
                    attSectionCombo.SelectedItem = previous;
                else if (attSectionCombo.Items.Count > 0)
                    attSectionCombo.SelectedIndex = 0;
            }
            catch (Exception ex) { Console.WriteLine("RefreshAttendanceSections error: " + ex.Message); }
        }

        private void OnAttendanceSectionChanged()
        {
            if (attSectionCombo == null) return;
            string section = attSectionCombo.Text;
            if (string.IsNullOrEmpty(section)) return;
            attHeaderSection.Text = $"Section {section}";
            attHeaderDate.Text = DateTime.Today.ToString("dddd, MMMM dd, yyyy");
            RefreshAttendanceGrid(section);
            ShowAttendancePlaceholder();
        }

        private void RefreshAttendanceGrid(string sectionFilter)
        {
            if (attGrid == null) return;
            string connStr = SettingsManager.Current.GetConnectionString();
            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = @"SELECT pa.student_name AS Name, COALESCE(pa.present, 0) AS Present, COALESCE(pa.absent, 0) AS Absent, COALESCE(pa.late, 0) AS Late FROM professor_attendance pa INNER JOIN student_class sc ON sc.user_id = pa.student_id WHERE sc.professor_id = @professor_id AND LOWER(TRIM(sc.section)) = LOWER(TRIM(@section)) ORDER BY pa.student_name";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@professor_id", ProfessorID);
                        cmd.Parameters.AddWithValue("@section", sectionFilter);
                        MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        attGrid.DataSource = dt;
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine("RefreshAttendanceGrid error: " + ex.Message); }
        }

        private List<(int StudentId, string StudentName)> GetStudentsInSection(string sectionFilter)
        {
            List<(int, string)> list = new List<(int, string)>();
            string connStr = SettingsManager.Current.GetConnectionString();
            using (var conn = new MySqlConnection(connStr))
            {
                conn.Open();
                string query = @"SELECT DISTINCT ui.user_id AS student_id, CONCAT(ui.lastname, ' ', ui.firstname, ' ', ui.middlename) AS student_name FROM student_class sc INNER JOIN user_information ui ON ui.user_id = sc.user_id WHERE sc.professor_id = @professor_id AND LOWER(TRIM(sc.section)) = LOWER(TRIM(@section)) ORDER BY student_name";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@professor_id", ProfessorID);
                    cmd.Parameters.AddWithValue("@section", sectionFilter);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int studentId = Convert.ToInt32(reader["student_id"]);
                            string studentName = reader["student_name"]?.ToString()?.Trim() ?? "";
                            if (!string.IsNullOrEmpty(studentName)) list.Add((studentId, studentName));
                        }
                    }
                }
            }
            return list;
        }

        private async void StartAttendanceSession()
        {
            if (attSectionCombo == null || string.IsNullOrEmpty(attSectionCombo.Text))
            {
                CustomMessageBox.Show("Please select a section first.", "No Section", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Warning);
                return;
            }

            string section = attSectionCombo.Text;
            List<(int StudentId, string StudentName)> students = null;
            Dictionary<int, string> existingAttendance = null;

            await RunWithLoadingAsync("Loading students", async () =>
            {
                students = await Task.Run(() => GetStudentsInSection(section));
                existingAttendance = await Task.Run(() => GetTodayAttendanceForSection(section));
            });

            if (students == null || students.Count == 0)
            {
                CustomMessageBox.Show($"No enrolled students found in section {section}.", "No Students", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Information);
                return;
            }

            EnsureTodayAttendanceColumn();

            attListPanel.SuspendLayout();
            for (int i = attListPanel.Controls.Count - 1; i >= 0; i--)
            {
                var c = attListPanel.Controls[i];
                attListPanel.Controls.RemoveAt(i);
                c.Dispose();
            }

            foreach (var student in students)
            {
                string currentStatus = existingAttendance != null && existingAttendance.ContainsKey(student.StudentId) ? existingAttendance[student.StudentId] : null;
                var card = BuildStudentAttendanceCard(student.StudentId, student.StudentName, currentStatus);
                attListPanel.Controls.Add(card);
            }

            attListPanel.ResumeLayout(true);
            attListPanel.PerformLayout();
            attListPanel.Refresh();

            attHeaderSection.Text = $"Section {section}   •   {students.Count} student(s)";
            attHeaderDate.Text = DateTime.Today.ToString("dddd, MMMM dd, yyyy");
        }

        private Dictionary<int, string> GetTodayAttendanceForSection(string sectionFilter)
        {
            var result = new Dictionary<int, string>();
            string dateCol = DateTime.Today.ToString("MMMdd", CultureInfo.InvariantCulture);
            string connStr = SettingsManager.Current.GetConnectionString();
            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string checkQuery = @"SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'professor_attendance' AND COLUMN_NAME = @col";
                    using (var checkCmd = new MySqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@col", dateCol);
                        if (Convert.ToInt32(checkCmd.ExecuteScalar()) == 0) return result;
                    }
                    string query = $@"SELECT pa.student_name AS Name, COALESCE(pa.`{dateCol}`, '—') AS Status FROM professor_attendance_new pa INNER JOIN student_class sc ON sc.user_id = pa.student_id AND sc.professor_id = pa.professor_id WHERE pa.professor_id = @professor_id AND LOWER(TRIM(sc.section)) = LOWER(TRIM(@section)) ORDER BY pa.student_name";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@professor_id", ProfessorID);
                        cmd.Parameters.AddWithValue("@section", sectionFilter);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int studentId = Convert.ToInt32(reader["student_id"]);
                                string status = reader["status"]?.ToString()?.Trim();
                                if (!string.IsNullOrEmpty(status)) result[studentId] = status;
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine("GetTodayAttendanceForSection error: " + ex.Message); }
            return result;
        }

        private async void SaveAttendanceSession()
        {
            if (attListPanel == null || attListPanel.Controls.Count == 0)
            {
                CustomMessageBox.Show("Nothing to update. Load the attendance list first.", "Nothing to Update", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Information);
                return;
            }

            string dateToday = DateTime.Today.ToString("MMMdd", CultureInfo.InvariantCulture);
            string connStr = SettingsManager.Current.GetConnectionString();
            var selections = new List<(int StudentId, string Status, string Name)>();

            foreach (Control ctrl in attListPanel.Controls)
            {
                if (!(ctrl is Guna2Panel card)) continue;
                if (!(card.Tag is int studentId)) continue;

                string selected = null;
                string name = "";

                foreach (Control inner in card.Controls)
                {
                    if (inner is Label lbl && lbl.Font.Bold && string.IsNullOrEmpty(name) && !lbl.Text.StartsWith("Tap a status")) name = lbl.Text;
                    if (inner is FlowLayoutPanel row)
                    {
                        foreach (Control b in row.Controls)
                        {
                            if (b is Guna2Button btn && btn.ForeColor == Color.White && btn.Text.Contains("✓")) selected = btn.Text.Replace("✓", "").Trim();
                        }
                    }
                }

                if (!string.IsNullOrEmpty(selected)) selections.Add((studentId, selected, name));
            }

            if (selections.Count == 0)
            {
                CustomMessageBox.Show("No statuses selected yet.", "Nothing to Update", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Information);
                return;
            }

            try
            {
                await RunWithLoadingAsync("Saving attendance", async () =>
                {
                    await Task.Run(() =>
                    {
                        using (var conn = new MySqlConnection(connStr))
                        {
                            conn.Open();
                            foreach (var s in selections)
                            {
                                string col = s.Status == "Present" ? "present" : s.Status == "Absent" ? "absent" : "late";
                                string insertIfMissing = @"INSERT INTO professor_attendance (student_id, student_name, present, absent, late) SELECT @student_id, @name, 0, 0, 0 FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM professor_attendance WHERE student_id = @student_id)";
                                using (var cmd = new MySqlCommand(insertIfMissing, conn))
                                {
                                    cmd.Parameters.AddWithValue("@student_id", s.StudentId);
                                    cmd.Parameters.AddWithValue("@name", s.Name);
                                    cmd.ExecuteNonQuery();
                                }
                                string query = $@"UPDATE professor_attendance SET {col} = CASE WHEN `{dateToday}` IS NULL OR `{dateToday}` <> @status THEN COALESCE({col}, 0) + 1 ELSE COALESCE({col}, 0) END, `{dateToday}` = @status WHERE student_id = @student_id";
                                using (var cmd = new MySqlCommand(query, conn))
                                {
                                    cmd.Parameters.AddWithValue("@status", s.Status);
                                    cmd.Parameters.AddWithValue("@student_id", s.StudentId);
                                    cmd.ExecuteNonQuery();
                                }
                            }
                        }
                    });
                });

                CustomMessageBox.Show($"{selections.Count} attendance record(s) saved.", "Success", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Information);
                RefreshAttendanceGrid(attSectionCombo.Text);
            }
            catch (Exception ex)
            {
                Console.WriteLine("SaveAttendanceSession error: " + ex.Message);
                CustomMessageBox.Show("Error saving attendance: " + ex.Message, "Error", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Error);
            }
        }

        private Guna2Panel BuildStudentAttendanceCard(int studentId, string studentName, string currentStatus = null)
        {
            int scrollbarWidth = SystemInformation.VerticalScrollBarWidth;
            int cardWidth = Math.Max(400, attListPanel.ClientSize.Width - scrollbarWidth - 20);

            Guna2Panel card = new Guna2Panel
            {
                Size = new Size(cardWidth, 84),
                FillColor = Color.White,
                BorderRadius = 12,
                BorderColor = Color.FromArgb(235, 230, 230),
                BorderThickness = 1,
                Margin = new Padding(0, 0, 0, 10),
                Tag = studentId
            };

            Guna2CircleButton avatar = new Guna2CircleButton
            {
                Size = new Size(52, 52),
                Location = new Point(16, 16),
                FillColor = Color.FromArgb(250, 235, 235),
                ForeColor = Color.Maroon,
                Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold),
                Text = GetInitialsFromName(studentName),
                Enabled = false
            };
            card.Controls.Add(avatar);

            Label lblName = new Label
            {
                Text = studentName,
                Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 40, 40),
                BackColor = Color.Transparent,
                AutoSize = false,
                AutoEllipsis = true,
                Size = new Size(card.Width - 520, 24),
                Location = new Point(82, 18)
            };
            card.Controls.Add(lblName);

            string hintText = string.IsNullOrEmpty(currentStatus) ? "Tap a status to mark attendance" : $"✓ Already marked as {currentStatus} today";
            Label lblHint = new Label
            {
                Text = hintText,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Italic),
                ForeColor = string.IsNullOrEmpty(currentStatus) ? Color.FromArgb(150, 150, 150) : GetStatusColor(currentStatus),
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(84, 46)
            };
            card.Controls.Add(lblHint);

            FlowLayoutPanel row = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = false,
                Size = new Size(390, 44),
                Location = new Point(card.Width - 410, 20),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BackColor = Color.Transparent
            };

            var btnPresent = MakeAttendanceButton("Present", Color.FromArgb(46, 160, 90));
            var btnLate = MakeAttendanceButton("Late", Color.FromArgb(230, 160, 30));
            var btnAbsent = MakeAttendanceButton("Absent", Color.FromArgb(200, 60, 60));

            Action<Guna2Button> reset = b =>
            {
                b.FillColor = Color.White;
                b.ForeColor = (Color)b.Tag;
                b.Font = new Font("Segoe UI", 9.5F);
                b.Text = b.Text.Replace("✓", "").Trim();
            };

            Action<Guna2Button> select = b =>
            {
                b.FillColor = (Color)b.Tag;
                b.ForeColor = Color.White;
                b.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
                b.Text = "✓  " + b.Text.Replace("✓", "").Trim();
            };

            EventHandler onClick = (s, ev) =>
            {
                Guna2Button clicked = (Guna2Button)s;
                reset(btnPresent); reset(btnLate); reset(btnAbsent);
                select(clicked);
                card.BorderColor = (Color)clicked.Tag;
                card.BorderThickness = 2;
                string newStatus = clicked.Text.Replace("✓", "").Trim();
                lblHint.Text = $"✓ Already marked as {newStatus} today";
                lblHint.ForeColor = (Color)clicked.Tag;
            };

            btnPresent.Click += onClick;
            btnLate.Click += onClick;
            btnAbsent.Click += onClick;

            row.Controls.Add(btnPresent);
            row.Controls.Add(btnLate);
            row.Controls.Add(btnAbsent);
            card.Controls.Add(row);

            if (!string.IsNullOrEmpty(currentStatus))
            {
                Guna2Button target = null;
                switch (currentStatus.ToLower())
                {
                    case "present": target = btnPresent; break;
                    case "late": target = btnLate; break;
                    case "absent": target = btnAbsent; break;
                }

                if (target != null)
                {
                    select(target);
                    card.BorderColor = (Color)target.Tag;
                    card.BorderThickness = 2;
                }
            }

            return card;
        }

        private Color GetStatusColor(string status)
        {
            switch (status?.ToLower())
            {
                case "present": return Color.FromArgb(46, 160, 90);
                case "late": return Color.FromArgb(230, 160, 30);
                case "absent": return Color.FromArgb(200, 60, 60);
                default: return Color.FromArgb(150, 150, 150);
            }
        }

        private Guna2Button MakeAttendanceButton(string text, Color accent)
        {
            Guna2Button btn = new Guna2Button
            {
                Text = text,
                Size = new Size(120, 40),
                Margin = new Padding(5, 0, 5, 0),
                BorderRadius = 10,
                FillColor = Color.White,
                ForeColor = accent,
                Font = new Font("Segoe UI", 9.5F),
                BorderColor = accent,
                BorderThickness = 1,
                Tag = accent,
                Cursor = Cursors.Hand
            };
            btn.HoverState.FillColor = Color.FromArgb(30, accent.R, accent.G, accent.B);
            btn.HoverState.ForeColor = accent;
            return btn;
        }

        private string GetInitialsFromName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "?";
            var parts = name.Split(new[] { ' ', ',', '_', '.' }, StringSplitOptions.RemoveEmptyEntries);
            string initials = "";
            foreach (var p in parts)
            {
                if (initials.Length >= 2) break;
                initials += char.ToUpper(p[0]);
            }
            return string.IsNullOrEmpty(initials) ? "?" : initials;
        }

        private void ShowAttendancePlaceholder()
        {
            if (attListPanel == null) return;

            bool hasCards = false;
            foreach (Control c in attListPanel.Controls) { if (c is Guna2Panel p && p.Tag is int) { hasCards = true; break; } }
            if (hasCards) return;

            attListPanel.Controls.Clear();

            Guna2Panel placeholder = new Guna2Panel
            {
                Size = new Size(Math.Max(400, attListPanel.ClientSize.Width - 24), 260),
                FillColor = Color.White,
                BorderRadius = 16,
                BorderColor = Color.FromArgb(230, 225, 225),
                BorderThickness = 1,
                Margin = new Padding(0, 20, 0, 0),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            Label lblIcon = new Label
            {
                Text = "📋",
                Font = new Font("Segoe UI Emoji", 36F),
                ForeColor = Color.FromArgb(210, 200, 200),
                BackColor = Color.Transparent,
                AutoSize = false,
                Size = new Size(placeholder.Width, 60),
                Location = new Point(0, 50),
                TextAlign = ContentAlignment.MiddleCenter,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            placeholder.Controls.Add(lblIcon);

            Label lblMsg = new Label
            {
                Text = "No attendance list loaded",
                Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold),
                ForeColor = Color.FromArgb(80, 80, 80),
                BackColor = Color.Transparent,
                AutoSize = false,
                Size = new Size(placeholder.Width, 26),
                Location = new Point(0, 120),
                TextAlign = ContentAlignment.MiddleCenter,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            placeholder.Controls.Add(lblMsg);

            Label lblSub = new Label
            {
                Text = "Click “Add Attendance” above to load today's students.",
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = Color.FromArgb(140, 140, 140),
                BackColor = Color.Transparent,
                AutoSize = false,
                Size = new Size(placeholder.Width, 22),
                Location = new Point(0, 152),
                TextAlign = ContentAlignment.MiddleCenter,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            placeholder.Controls.Add(lblSub);

            attListPanel.Controls.Add(placeholder);
        }

        private async void ExportAttendanceToExcel()
        {
            if (attSectionCombo == null || string.IsNullOrEmpty(attSectionCombo.Text))
            {
                CustomMessageBox.Show("Please select a section first.", "No Section", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Warning);
                return;
            }

            string section = attSectionCombo.Text;
            string connStr = SettingsManager.Current.GetConnectionString();

            try
            {
                DataTable dt = null;
                await RunWithLoadingAsync("Loading attendance data", async () =>
                {
                    dt = await Task.Run(() =>
                    {
                        var table = new DataTable();
                        using (var conn = new MySqlConnection(connStr))
                        {
                            conn.Open();
                            string query = @"SELECT pa.student_id, pa.student_name, pa.present, pa.absent, pa.late, pa.Sep02, pa.Sep04, pa.Sep24, pa.Sep28, pa.Oct01, pa.Oct04 FROM professor_attendance pa INNER JOIN student_class sc ON sc.user_id = pa.student_id WHERE sc.professor_id = @professor_id AND LOWER(TRIM(sc.section)) = LOWER(TRIM(@section)) ORDER BY pa.student_name";
                            using (var cmd = new MySqlCommand(query, conn))
                            {
                                cmd.Parameters.AddWithValue("@professor_id", ProfessorID);
                                cmd.Parameters.AddWithValue("@section", section);
                                using (var adapter = new MySqlDataAdapter(cmd)) adapter.Fill(table);
                            }
                        }
                        return table;
                    });
                });

                if (dt.Rows.Count == 0)
                {
                    CustomMessageBox.Show($"No attendance data found for section {section}.", "Empty", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Information);
                    return;
                }

                using (SaveFileDialog sfd = new SaveFileDialog())
                {
                    sfd.Filter = "Excel Files|*.xlsx";
                    sfd.FileName = $"Attendance_{SanitizeFolderName(section)}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        await RunWithLoadingAsync("Exporting to Excel", async () =>
                        {
                            await Task.Run(() =>
                            {
                                using (var workbook = new XLWorkbook())
                                {
                                    var worksheet = workbook.Worksheets.Add(dt, "Attendance");
                                    var headerRow = worksheet.Row(1);
                                    headerRow.Style.Font.Bold = true;
                                    headerRow.Style.Fill.BackgroundColor = XLColor.Maroon;
                                    headerRow.Style.Font.FontColor = XLColor.White;
                                    headerRow.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                                    headerRow.Height = 22;
                                    ColorDateColumns(worksheet, dt);
                                    worksheet.Columns().AdjustToContents();
                                    worksheet.SheetView.FreezeRows(1);
                                    workbook.SaveAs(sfd.FileName);
                                }
                            });
                        });

                        CustomMessageBox.Show($"Exported successfully!\n\nFile: {sfd.FileName}", "Exported", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ExportAttendanceToExcel error: " + ex.Message);
                CustomMessageBox.Show("Export failed: " + ex.Message, "Error", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Error);
            }
        }

        private void ColorDateColumns(IXLWorksheet worksheet, DataTable dt)
        {
            var dateColumnIndices = new List<int>();

            for (int col = 0; col < dt.Columns.Count; col++)
            {
                string colName = dt.Columns[col].ColumnName;
                if (System.Text.RegularExpressions.Regex.IsMatch(colName, @"^[A-Z][a-z]{2}\d{2}$")) dateColumnIndices.Add(col + 1);
            }

            foreach (int colIdx in dateColumnIndices)
            {
                for (int row = 2; row <= dt.Rows.Count + 1; row++)
                {
                    var cell = worksheet.Cell(row, colIdx);
                    string value = cell.GetString()?.Trim() ?? "";

                    switch (value.ToLower())
                    {
                        case "present": cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#D4EDDA"); cell.Style.Font.FontColor = XLColor.FromHtml("#155724"); break;
                        case "late": cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#FFF3CD"); cell.Style.Font.FontColor = XLColor.FromHtml("#856404"); break;
                        case "absent": cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F8D7DA"); cell.Style.Font.FontColor = XLColor.FromHtml("#721C24"); break;
                    }

                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }
            }

            string[] counterCols = { "present", "absent", "late" };
            for (int col = 0; col < dt.Columns.Count; col++)
            {
                if (counterCols.Contains(dt.Columns[col].ColumnName.ToLower()))
                {
                    worksheet.Column(col + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    worksheet.Column(col + 1).Style.Font.Bold = true;
                }
            }
        }

        private void EnsureTodayAttendanceColumn()
        {
            string dateCol = DateTime.Today.ToString("MMMdd", CultureInfo.InvariantCulture);
            string connStr = SettingsManager.Current.GetConnectionString();
            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    var builder = new MySqlConnectionStringBuilder(connStr);
                    string dbName = builder.Database;
                    string checkQuery = @"SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = @dbName AND TABLE_NAME = 'professor_attendance' AND COLUMN_NAME = @columnName";
                    bool columnExists;
                    using (var checkCmd = new MySqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@dbName", dbName);
                        checkCmd.Parameters.AddWithValue("@columnName", dateCol);
                        columnExists = Convert.ToInt32(checkCmd.ExecuteScalar()) > 0;
                    }
                    if (!columnExists)
                    {
                        string addColumnQuery = $"ALTER TABLE professor_attendance ADD `{dateCol}` VARCHAR(20)";
                        using (var cmd = new MySqlCommand(addColumnQuery, conn)) cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine("EnsureTodayAttendanceColumn error: " + ex.Message); }
        }

        // =========================================================
        // CLASS
        // =========================================================
        private void btnShowPnlCreateClass_Click(object sender, EventArgs e) { pnlCreateClass.Visible = true; pnlCreateClass.BringToFront(); }
        private void btnClosePanel_Click(object sender, EventArgs e) { pnlCreateClass.Visible = false; pnlCreateClass.SendToBack(); }

        private async void btnCreateClass_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtClassCode.Text) || string.IsNullOrWhiteSpace(txtClassName.Text) || string.IsNullOrWhiteSpace(txtClassSection.Text) || string.IsNullOrWhiteSpace(txtClassTime.Text) || string.IsNullOrWhiteSpace(cmbClassDate.Text))
            {
                CustomMessageBox.Show("Please fill in all fields.", "Validation", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Warning);
                return;
            }

            string classCode = txtClassCode.Text.Trim();
            string className = txtClassName.Text.Trim();
            string classSection = txtClassSection.Text.Trim();
            string classTime = txtClassTime.Text.Trim();
            string classDate = cmbClassDate.Text.Trim();

            string connStr = SettingsManager.Current.GetConnectionString();
            try
            {
                await RunWithLoadingAsync("Creating class", async () =>
                {
                    await Task.Run(() =>
                    {
                        using (var conn = new MySqlConnection(connStr))
                        {
                            conn.Open();
                            string query = @"INSERT INTO professor_class (professor_id, class_code, class_name, class_section, class_time, class_date) VALUES (@professor_id, @class_code, @class_name, @class_section, @class_time, @class_date)";
                            using (var cmd = new MySqlCommand(query, conn))
                            {
                                cmd.Parameters.AddWithValue("@professor_id", ProfessorID);
                                cmd.Parameters.AddWithValue("@class_code", classCode);
                                cmd.Parameters.AddWithValue("@class_name", className);
                                cmd.Parameters.AddWithValue("@class_section", classSection);
                                cmd.Parameters.AddWithValue("@class_time", classTime);
                                cmd.Parameters.AddWithValue("@class_date", classDate);
                                cmd.ExecuteNonQuery();
                            }
                        }
                    });
                });

                string folderName = classSection;
                AutoCreateClassBtn();
                CreateFolderForSection(folderName);
                RefreshHomeCounters();

                CustomMessageBox.Show("Class created successfully!", "Success", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Information);

                txtClassCode.Clear();
                txtClassName.Clear();
                txtClassSection.Clear();
                txtClassTime.Clear();
                cmbClassDate.SelectedIndex = -1;
                pnlCreateClass.Visible = false;
                pnlCreateClass.SendToBack();

                RefreshAttendanceSections();
            }
            catch (Exception ex)
            {
                Console.WriteLine("btnCreateClass_Click error: " + ex.Message);
                CustomMessageBox.Show("Error: " + ex.Message, "Error", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Error);
            }
        }

        private async void CreateFolderForSection(string folderName)
        {
            if (string.IsNullOrWhiteSpace(folderName) || string.IsNullOrEmpty(saveFolder) || saveFolder == "Null")
            {
                CustomMessageBox.Show("Save folder is not set up correctly.", "Notice", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Warning);
                return;
            }

            string newFolderPath = Path.Combine(saveFolder, SanitizeFolderName(folderName));

            if (Directory.Exists(newFolderPath))
            {
                CustomMessageBox.Show("Folder already exists.", "Notice", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Information);
                return;
            }

            await RunWithLoadingAsync("Creating section folder", async () => { await Task.Run(() => Directory.CreateDirectory(newFolderPath)); });
            LoadServerFolder(saveFolder);
        }

        private void AutoCreateClassBtn()
        {
            flpSubjectClass.Controls.Clear();
            _colorIndex = 0;
            flpSubjectClass.FlowDirection = FlowDirection.LeftToRight;
            flpSubjectClass.WrapContents = true;
            flpSubjectClass.AutoScroll = true;

            if (!_classResizeHooked)
            {
                _classResizeHooked = true;
                flpSubjectClass.SizeChanged += (s, e) => ResizeClassCards();
                if (flpSubjectClass.Parent != null) flpSubjectClass.Parent.SizeChanged += (s, e) => ResizeClassCards();
            }

            string connStr = SettingsManager.Current.GetConnectionString();
            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = @"SELECT class_id, class_name, class_date, class_time, class_section FROM professor_class WHERE professor_id = @professor_id";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@professor_id", ProfessorID);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                ClassCardPanel card = CreateClassCard(
                                    reader["class_id"].ToString(),
                                    reader["class_name"].ToString(),
                                    reader["class_section"].ToString(),
                                    reader["class_date"].ToString(),
                                    reader["class_time"].ToString(),
                                    "Prof. " + ProfessorID);
                                flpSubjectClass.Controls.Add(card);
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine("AutoCreateClassBtn error: " + ex.Message); }

            RefreshHomeCounters();
        }

        private bool _classResizeHooked = false;

        private int GetClassCardWidth()
        {
            int panelWidth = flpSubjectClass.ClientSize.Width;
            if (flpSubjectClass.Parent != null)
            {
                int parentSpace = flpSubjectClass.Parent.ClientSize.Width - flpSubjectClass.Left;
                panelWidth = Math.Min(panelWidth, parentSpace);
            }
            int available = panelWidth - flpSubjectClass.Padding.Horizontal - SystemInformation.VerticalScrollBarWidth - 20;
            int width = (available - 40) / 2;
            return Math.Max(280, Math.Min(width, 420));
        }

        private void ResizeClassCards()
        {
            int w = GetClassCardWidth();
            flpSubjectClass.SuspendLayout();
            foreach (Control c in flpSubjectClass.Controls) { if (c is ClassCardPanel card) card.Width = w; }
            flpSubjectClass.ResumeLayout();
        }

        // =========================================================
        // CREATE CLASS CARD — now clickable to open Class Details
        // =========================================================
        private ClassCardPanel CreateClassCard(string classId, string title, string section,
            string day, string time, string professorName)
        {
            var card = new ClassCardPanel
            {
                Size = new Size(GetClassCardWidth(), 210),
                Margin = new Padding(10),
                Cursor = Cursors.Hand,
                BannerColor = BannerColors[_colorIndex % BannerColors.Length],
                Title = title,
                Subtitle = section,
                Section = professorName,
                Day = day,
                Time = time,
                Tag = classId
            };
            _colorIndex++;

            // NEW: Open Class Details when the card is clicked
            card.Click += (s, e) =>
            {
                if (e is MouseEventArgs me && me.Button != MouseButtons.Left) return;
                OpenClassDetails(classId, title, section, day, time);
            };

            // Optional: also support double-click
            card.DoubleClick += (s, e) => OpenClassDetails(classId, title, section, day, time);

            ContextMenuStrip rightClickMenu = new ContextMenuStrip();
            ToolStripMenuItem deleteMenuItem = new ToolStripMenuItem("Delete Class");
            deleteMenuItem.Click += DeleteClass_Click;
            rightClickMenu.Items.Add(deleteMenuItem);
            card.ContextMenuStrip = rightClickMenu;

            return card;
        }

        // =========================================================
        // OPEN CLASS DETAILS FORM
        // =========================================================
        private void OpenClassDetails(string classId, string className, string section,
                                      string day, string time)
        {
            try
            {
                var detailsForm = new ClassDetailsForm(
                    ProfessorID,
                    classId,
                    className,
                    section,
                    day,
                    time);

                detailsForm.ShowDialog(this);
            }
            catch (Exception ex)
            {
                Console.WriteLine("OpenClassDetails error: " + ex.Message);
                CustomMessageBox.Show("Unable to open class details:\n\n" + ex.Message,
                    "Open Error", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Error);
            }
        }

        private async void DeleteClass_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem menuItem = sender as ToolStripMenuItem;
            if (menuItem == null) return;
            ContextMenuStrip menu = menuItem.Owner as ContextMenuStrip;
            if (menu == null) return;
            Panel clickedCard = menu.SourceControl as Panel;
            if (clickedCard == null) return;

            string classId = clickedCard.Tag?.ToString();
            if (string.IsNullOrEmpty(classId)) return;

            var result = CustomMessageBox.Show("Are you sure you want to delete this class?", "Confirm Delete", CustomMessageBoxButtons.YesNo, CustomMessageBoxIcon.Warning);

            if (result == CustomMessageBoxResult.Yes)
            {
                string connStr = SettingsManager.Current.GetConnectionString();
                try
                {
                    await RunWithLoadingAsync("Deleting class", async () =>
                    {
                        await Task.Run(() =>
                        {
                            using (var conn = new MySqlConnection(connStr))
                            {
                                conn.Open();
                                using (var cmd = new MySqlCommand("DELETE FROM professor_class WHERE class_id = @id", conn))
                                {
                                    cmd.Parameters.AddWithValue("@id", classId);
                                    cmd.ExecuteNonQuery();
                                }
                            }
                        });
                    });

                    flpSubjectClass.Controls.Remove(clickedCard);
                    clickedCard.Dispose();
                    menu.Dispose();

                    RefreshAttendanceSections();
                    RefreshHomeCounters();
                }
                catch (Exception ex) { Console.WriteLine("DeleteClass_Click error: " + ex.Message); }
            }
        }

        // =========================================================
        // ACTIVITY
        // =========================================================
        private void btnActivityUploadFile_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    selectedFilePath = ofd.FileName;
                    btnActivityUploadFile.Text = Path.GetFileName(selectedFilePath);
                }
                if (btnActivityUploadFile != null)
                {
                    btnActivityUploadFile.AllowDrop = true;
                    btnActivityUploadFile.DragEnter += ActivityUpload_DragEnter;
                    btnActivityUploadFile.DragDrop += ActivityUpload_DragDrop;
                }
                if (pnlActivity != null)
                {
                    pnlActivity.AllowDrop = true;
                    pnlActivity.DragEnter += ActivityUpload_DragEnter;
                    pnlActivity.DragDrop += ActivityUpload_DragDrop;
                }
            }
        }

        private void ActivityUpload_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0) { e.Effect = DragDropEffects.Copy; return; }
            }
            e.Effect = DragDropEffects.None;
        }

        private void ActivityUpload_DragDrop(object sender, DragEventArgs e)
        {
            try
            {
                if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files == null || files.Length == 0) return;

                string droppedFile = files[0];
                if (!File.Exists(droppedFile))
                {
                    CustomMessageBox.Show("The dropped item is not a valid file.", "Invalid File", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Warning);
                    return;
                }

                FileInfo fi = new FileInfo(droppedFile);
                if (fi.Length > 200 * 1024 * 1024)
                {
                    CustomMessageBox.Show("File is too large. Maximum allowed size is 200 MB.", "File Too Large", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Warning);
                    return;
                }

                selectedFilePath = droppedFile;
                btnActivityUploadFile.Text = Path.GetFileName(selectedFilePath);

                btnActivityUploadFile.FillColor = Color.FromArgb(46, 160, 90);
                var resetTimer = new System.Windows.Forms.Timer { Interval = 800 };
                resetTimer.Tick += (s, ev) => { btnActivityUploadFile.FillColor = Color.Maroon; resetTimer.Stop(); resetTimer.Dispose(); };
                resetTimer.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine("ActivityUpload_DragDrop error: " + ex.Message);
                CustomMessageBox.Show("Error handling dropped file: " + ex.Message, "Error", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Error);
            }
        }

        private async void btnPostActivity_Click(object sender, EventArgs e)
        {
            string connStr = SettingsManager.Current.GetConnectionString();
            DateTime now = DateTime.Now;
            string FullDateTime = now.ToString("MMM-dd HH:mm:ss");

            string title = cmbActivityTitle.Text.Trim();
            string description = txtActivityPostDetails.Text.Trim();
            string sectionRaw = cmbActivitySection.Text.Trim();
            string subject = cmbActivitySubject.Text.Trim();
            string score = txtActivityScore.Text.Trim();
            DateTime dueDate = dtpActivityDeadline.Value;

            string pdfName = null;
            byte[] fileBytes = null;
            string uncPath = null;
            string section = SanitizeFolderName(sectionRaw);

            if (string.IsNullOrEmpty(ProfessorName)) NameGet();
            string professorFolder = SanitizeFolderName(ProfessorName);

            if (!string.IsNullOrEmpty(selectedFilePath) && File.Exists(selectedFilePath))
            {
                try
                {
                    await RunWithLoadingAsync("Reading file", async () =>
                    {
                        pdfName = SanitizeFolderName(Path.GetFileName(selectedFilePath));
                        fileBytes = await File.ReadAllBytesAsync(selectedFilePath);
                    });
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show("Could not read the selected file: " + ex.Message, "File Error", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Error);
                    return;
                }
            }

            if (fileBytes != null && !string.IsNullOrEmpty(pdfName))
            {
                bool sent = false;
                await RunWithLoadingAsync("Uploading file to server", async () => { sent = await SendActivityFileToServer(professorFolder, section, pdfName, fileBytes); });

                if (!sent)
                {
                    CustomMessageBox.Show("Failed to send activity file to server. Activity not posted.", "Send Failed", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Error);
                    return;
                }

                string saveRoot = SettingsManager.Current.SaveFolder;
                if (saveRoot.StartsWith(@"\\"))
                {
                    uncPath = Path.Combine(saveRoot, professorFolder, section, "ActivityFiles", pdfName);
                }
                else
                {
                    string shared = new DirectoryInfo(saveRoot).Name;
                    string ip = SettingsManager.Current.ServerIp.TrimStart('\\').TrimEnd('\\');
                    uncPath = $@"\\{ip}\{shared}\{professorFolder}\{section}\ActivityFiles\{pdfName}";
                }
            }

            try
            {
                string finalUncPath = uncPath;
                string finalPdfName = pdfName;

                await RunWithLoadingAsync("Posting activity", async () =>
                {
                    await Task.Run(() =>
                    {
                        using (var conn = new MySqlConnection(connStr))
                        {
                            conn.Open();
                            string query = @"INSERT INTO professor_activity (professor_id, title, description, section, activity_subject, start_time, due_date, activity_status, score, activity_filename, activity_file) VALUES (@professor_id, @title, @description, @section, @activity_subject, @start_time, @due_date, @activity_status, @score, @activity_filename, @activity_file)";
                            using (var cmd = new MySqlCommand(query, conn))
                            {
                                cmd.Parameters.AddWithValue("@professor_id", ProfessorID);
                                cmd.Parameters.AddWithValue("@title", title);
                                cmd.Parameters.AddWithValue("@description", description);
                                cmd.Parameters.AddWithValue("@section", sectionRaw);
                                cmd.Parameters.AddWithValue("@activity_subject", subject);
                                cmd.Parameters.AddWithValue("@start_time", FullDateTime);
                                cmd.Parameters.AddWithValue("@due_date", dueDate);
                                cmd.Parameters.AddWithValue("@activity_status", "Pending");
                                cmd.Parameters.AddWithValue("@score", score);
                                cmd.Parameters.AddWithValue("@activity_file", (object)finalUncPath ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@activity_filename", (object)finalPdfName ?? DBNull.Value);
                                cmd.ExecuteNonQuery();
                            }
                        }
                    });
                });

                CustomMessageBox.Show("Activity Posted Successfully", "Success", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Information);
                selectedFilePath = "";
                btnActivityUploadFile.Text = "Upload File";
                RecentActivity();

                if (!string.IsNullOrEmpty(currentFolder) && Directory.Exists(currentFolder))
                    LoadServerFolder(currentFolder, addToHistory: false);
            }
            catch (Exception ex)
            {
                Console.WriteLine("btnPostActivity_Click error: " + ex.Message);
                CustomMessageBox.Show("Error posting activity: " + ex.Message, "Error", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Error);
            }
        }

        private async Task<bool> SendActivityFileToServer(string professorFolder, string section, string fileName, byte[] fileBytes)
        {
            try
            {
                string serverIp = SettingsManager.Current.ServerIp.TrimStart('\\').TrimEnd('\\');
                int serverPort = SettingsManager.Current.FileTransferPort;

                using (TcpClient client = new TcpClient())
                {
                    var connectTask = client.ConnectAsync(serverIp, serverPort);
                    var timeoutTask = Task.Delay(5000);
                    var completed = await Task.WhenAny(connectTask, timeoutTask);

                    if (completed == timeoutTask)
                    {
                        CustomMessageBox.Show("Connection timeout.\n\nServer: " + serverIp + ":" + serverPort, "Connection Timeout", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Error);
                        return false;
                    }

                    await connectTask;
                    if (!client.Connected)
                    {
                        CustomMessageBox.Show("Connection refused by server.\n\nServer: " + serverIp + ":" + serverPort, "Connection Refused", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Error);
                        return false;
                    }

                    using (NetworkStream stream = client.GetStream())
                    using (BinaryWriter writer = new BinaryWriter(stream))
                    {
                        writer.Write("ACTIVITY_FILE");
                        writer.Write(professorFolder);
                        writer.Write(section);
                        writer.Write(fileName);
                        writer.Write(fileBytes.Length);
                        writer.Write(fileBytes);
                        writer.Flush();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("SendActivityFileToServer EXCEPTION: " + ex.Message);
                CustomMessageBox.Show("Error sending file:\n\n" + ex.Message, "Send Error", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Error);
                return false;
            }
        }

        private void ActivitySectionSubject()
        {
            cmbActivitySection.Items.Clear();
            cmbActivitySubject.Items.Clear();
            string connStr = SettingsManager.Current.GetConnectionString();
            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand("SELECT class_name, class_section FROM professor_class WHERE professor_id = @professor_id", conn))
                    {
                        cmd.Parameters.AddWithValue("@professor_id", ProfessorID);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string section = reader.GetString("class_section");
                                string className = reader.GetString("class_name");
                                if (!cmbActivitySection.Items.Contains(section)) cmbActivitySection.Items.Add(section);
                                if (!cmbActivitySubject.Items.Contains(className)) cmbActivitySubject.Items.Add(className);
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine("ActivitySectionSubject error: " + ex.Message); }
        }

        private void RecentActivity()
        {
            string connStr = SettingsManager.Current.GetConnectionString();
            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = @"SELECT title, section, activity_subject, due_date FROM professor_activity WHERE professor_id = @professor_id ORDER BY start_time DESC LIMIT 5";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@professor_id", ProfessorID);
                        MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        dgvRecentActivity.DataSource = dt;
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine("RecentActivity error: " + ex.Message); }
        }

        private void lsServerFolderSetup()
        {
            FolderListView.View = View.LargeIcon;
            FolderListView.LargeImageList = imageList1;
            FolderListView.MultiSelect = false;
        }

        private void LoadServerFolder(string path, bool addToHistory = true)
        {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path)) return;
            if (addToHistory && !string.IsNullOrEmpty(currentFolder) && currentFolder != path) folderHistory.Push(currentFolder);
            currentFolder = path;

            FolderListView.BeginUpdate();

            try
            {
                FolderListView.Items.Clear();
                imageList1.Images.Clear();
                imageList1.ColorDepth = ColorDepth.Depth32Bit;
                imageList1.ImageSize = new Size(64, 64);
                imageList1.TransparentColor = Color.Transparent;

                int imageIndex = 0;

                foreach (string dir in Directory.GetDirectories(path).OrderBy(d => d))
                {
                    Image folderIcon = GetRealFolderIcon();
                    imageList1.Images.Add(folderIcon);
                    ListViewItem item = new ListViewItem(Path.GetFileName(dir), imageIndex);
                    item.Tag = dir;
                    item.ToolTipText = dir;
                    FolderListView.Items.Add(item);
                    imageIndex++;
                }

                foreach (string file in Directory.GetFiles(path).OrderBy(f => f))
                {
                    Image fileIcon = GetRealFileIcon(file);
                    imageList1.Images.Add(fileIcon);
                    ListViewItem item = new ListViewItem(Path.GetFileName(file), imageIndex);
                    item.Tag = file;
                    item.ToolTipText = file;
                    FolderListView.Items.Add(item);
                    imageIndex++;
                }

                BtnBack.Enabled = folderHistory.Count > 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to load files:\n\n" + ex.Message, "File Browser", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                FolderListView.EndUpdate();
            }
        }

        private Image GetRealFolderIcon() => CreateProgrammaticFolderIcon();
        private Image GetRealFileIcon(string filePath) => CreateProgrammaticFileIcon(filePath);

        private Image CreateProgrammaticFolderIcon()
        {
            Bitmap bmp = new Bitmap(64, 64);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.Clear(Color.Transparent);

                using (SolidBrush shadow = new SolidBrush(Color.FromArgb(35, 0, 0, 0)))
                using (GraphicsPath shadowPath = CreateRoundedRectanglePath(new Rectangle(7, 20, 51, 35), 6))
                    g.FillPath(shadow, shadowPath);

                using (SolidBrush tabBrush = new SolidBrush(Color.FromArgb(255, 211, 79)))
                using (GraphicsPath tabPath = CreateRoundedRectanglePath(new Rectangle(8, 11, 27, 16), 4))
                    g.FillPath(tabBrush, tabPath);

                using (LinearGradientBrush folderBrush = new LinearGradientBrush(new Rectangle(6, 19, 52, 36), Color.FromArgb(255, 235, 176), Color.FromArgb(255, 184, 134), LinearGradientMode.Vertical))
                using (GraphicsPath folderPath = CreateRoundedRectanglePath(new Rectangle(6, 18, 52, 37), 6))
                    g.FillPath(folderBrush, folderPath);

                using (Pen highlight = new Pen(Color.FromArgb(150, 255, 255, 255), 1.5f))
                    g.DrawLine(highlight, 10, 23, 52, 23);

                using (Pen border = new Pen(Color.FromArgb(120, 155, 105, 40), 1.2f))
                using (GraphicsPath borderPath = CreateRoundedRectanglePath(new Rectangle(6, 18, 52, 37), 6))
                    g.DrawPath(border, borderPath);
            }
            return bmp;
        }

        private Image CreateProgrammaticFileIcon(string filePath)
        {
            Bitmap bmp = new Bitmap(64, 64);
            string extension = Path.GetExtension(filePath ?? string.Empty).ToLowerInvariant();

            string badgeText = "FILE";
            Color badgeColor = Color.FromArgb(88, 101, 116);

            switch (extension)
            {
                case ".doc": case ".docx": badgeText = "W"; badgeColor = Color.FromArgb(43, 87, 154); break;
                case ".pdf": badgeText = "PDF"; badgeColor = Color.FromArgb(190, 45, 45); break;
                case ".xls": case ".xlsx": case ".csv": badgeText = "X"; badgeColor = Color.FromArgb(33, 115, 70); break;
                case ".ppt": case ".pptx": badgeText = "P"; badgeColor = Color.FromArgb(194, 91, 40); break;
                case ".txt": case ".log": badgeText = "TXT"; badgeColor = Color.FromArgb(90, 99, 109); break;
                case ".jpg": case ".jpeg": case ".png": case ".gif": case ".bmp": case ".webp": badgeText = "IMG"; badgeColor = Color.FromArgb(92, 92, 180); break;
                case ".zip": case ".rar": case ".7z": badgeText = "ZIP"; badgeColor = Color.FromArgb(133, 91, 52); break;
                case ".cs": badgeText = "C#"; badgeColor = Color.FromArgb(104, 62, 143); break;
                case ".html": case ".htm": badgeText = "< >"; badgeColor = Color.FromArgb(220, 92, 42); break;
                case ".sql": badgeText = "SQL"; badgeColor = Color.FromArgb(45, 102, 150); break;
                case ".exe": badgeText = "APP"; badgeColor = Color.FromArgb(70, 80, 92); break;
            }

            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.Clear(Color.Transparent);

                using (SolidBrush shadow = new SolidBrush(Color.FromArgb(30, 0, 0, 0)))
                using (GraphicsPath shadowPath = CreateRoundedRectanglePath(new Rectangle(11, 7, 43, 51), 5))
                    g.FillPath(shadow, shadowPath);

                Point[] document = { new Point(13, 5), new Point(39, 5), new Point(52, 18), new Point(52, 57), new Point(13, 57) };
                using (SolidBrush paper = new SolidBrush(Color.FromArgb(248, 250, 252)))
                    g.FillPolygon(paper, document);

                using (Pen paperBorder = new Pen(Color.FromArgb(145, 155, 165), 1.2f))
                    g.DrawPolygon(paperBorder, document);

                Point[] fold = { new Point(39, 5), new Point(39, 18), new Point(52, 18) };
                using (SolidBrush foldBrush = new SolidBrush(Color.FromArgb(220, 228, 236)))
                    g.FillPolygon(foldBrush, fold);

                using (Pen foldPen = new Pen(Color.FromArgb(150, 160, 170), 1f))
                {
                    g.DrawLine(foldPen, 39, 5, 39, 18);
                    g.DrawLine(foldPen, 39, 18, 52, 18);
                }

                using (Pen linePen = new Pen(Color.FromArgb(175, 185, 195), 1.4f))
                {
                    g.DrawLine(linePen, 19, 25, 45, 25);
                    g.DrawLine(linePen, 19, 31, 45, 31);
                    g.DrawLine(linePen, 19, 37, 40, 37);
                }

                Rectangle badge = new Rectangle(17, 40, 31, 14);
                using (GraphicsPath badgePath = CreateRoundedRectanglePath(badge, 4))
                using (SolidBrush badgeBrush = new SolidBrush(badgeColor))
                    g.FillPath(badgeBrush, badgePath);

                using (Font badgeFont = new Font("Segoe UI", badgeText.Length > 3 ? 5.5f : 8f, FontStyle.Bold))
                using (SolidBrush badgeTextBrush = new SolidBrush(Color.White))
                using (StringFormat format = new StringFormat())
                {
                    format.Alignment = StringAlignment.Center;
                    format.LineAlignment = StringAlignment.Center;
                    g.DrawString(badgeText, badgeFont, badgeTextBrush, badge, format);
                }
            }
            return bmp;
        }

        private GraphicsPath CreateRoundedRectanglePath(Rectangle rectangle, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = radius * 2;
            if (diameter > rectangle.Width) diameter = rectangle.Width;
            if (diameter > rectangle.Height) diameter = rectangle.Height;

            Rectangle arc = new Rectangle(rectangle.X, rectangle.Y, diameter, diameter);
            path.AddArc(arc, 180, 90);
            arc.X = rectangle.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = rectangle.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = rectangle.X;
            path.AddArc(arc, 90, 90);
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
            if (FolderListView.SelectedItems.Count == 0) return;
            string path = FolderListView.SelectedItems[0].Tag?.ToString();
            if (string.IsNullOrEmpty(path)) return;

            if (Directory.Exists(path)) LoadServerFolder(path);
            else if (File.Exists(path)) System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
        }

        private void BtnBack_Click(object sender, EventArgs e) => btnBack();
        private void FolderListView_DoubleClick(object sender, EventArgs e) => doubleClick();

        private void btnAddFolder_Click(object sender, EventArgs e)
        {
            if (pnlFile.Controls.OfType<Guna.UI2.WinForms.Guna2Panel>().Any(p => p.Name == "pnlNewFolderPrompt")) return;

            Guna.UI2.WinForms.Guna2Panel overlay = new Guna.UI2.WinForms.Guna2Panel();
            overlay.Name = "pnlNewFolderPrompt";
            overlay.Dock = DockStyle.Fill;
            overlay.FillColor = Color.FromArgb(150, 0, 0, 0);
            overlay.BorderRadius = 10;
            pnlFile.Controls.Add(overlay);
            overlay.BringToFront();

            Guna.UI2.WinForms.Guna2Panel card = new Guna.UI2.WinForms.Guna2Panel();
            card.Size = new Size(420, 210);
            card.BorderRadius = 15;
            card.FillColor = Color.White;
            card.ShadowDecoration.Enabled = true;
            card.ShadowDecoration.Depth = 20;
            card.Location = new Point(Math.Max(0, (overlay.Width - card.Width) / 2), Math.Max(0, (overlay.Height - card.Height) / 2));
            overlay.Controls.Add(card);

            overlay.Resize += (s, args) => { card.Location = new Point(Math.Max(0, (overlay.Width - card.Width) / 2), Math.Max(0, (overlay.Height - card.Height) / 2)); };

            Label lblTitle = new Label();
            lblTitle.Text = "Create New Folder";
            lblTitle.Font = new Font("Segoe UI Semibold", 14F, FontStyle.Bold);
            lblTitle.ForeColor = Color.Maroon;
            lblTitle.AutoSize = true;
            lblTitle.Location = new Point(24, 20);
            card.Controls.Add(lblTitle);

            Label lblSub = new Label();
            lblSub.Text = "Enter a folder name:";
            lblSub.Font = new Font("Segoe UI", 10F);
            lblSub.ForeColor = Color.DimGray;
            lblSub.AutoSize = true;
            lblSub.Location = new Point(24, 60);
            card.Controls.Add(lblSub);

            Guna.UI2.WinForms.Guna2TextBox txtFolderName = new Guna.UI2.WinForms.Guna2TextBox();
            txtFolderName.Width = 372;
            txtFolderName.Height = 42;
            txtFolderName.Location = new Point(24, 90);
            txtFolderName.BorderRadius = 8;
            txtFolderName.Font = new Font("Segoe UI", 10F);
            txtFolderName.PlaceholderText = "Folder name";
            card.Controls.Add(txtFolderName);

            Guna.UI2.WinForms.Guna2Button btnCancel = new Guna.UI2.WinForms.Guna2Button();
            btnCancel.Text = "Cancel";
            btnCancel.Size = new Size(120, 42);
            btnCancel.Location = new Point(24, 148);
            btnCancel.BorderRadius = 8;
            btnCancel.FillColor = Color.LightGray;
            btnCancel.ForeColor = Color.Black;
            btnCancel.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            btnCancel.Click += (s, args) => { pnlFile.Controls.Remove(overlay); overlay.Dispose(); };
            card.Controls.Add(btnCancel);

            Guna.UI2.WinForms.Guna2Button btnCreate = new Guna.UI2.WinForms.Guna2Button();
            btnCreate.Text = "Create";
            btnCreate.Size = new Size(120, 42);
            btnCreate.Location = new Point(276, 148);
            btnCreate.BorderRadius = 8;
            btnCreate.FillColor = Color.Maroon;
            btnCreate.ForeColor = Color.White;
            btnCreate.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            btnCreate.Click += (s, args) =>
            {
                string folderName = txtFolderName.Text.Trim();
                if (string.IsNullOrWhiteSpace(folderName)) { CustomMessageBox.Show("Please enter a folder name.", "Validation", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Warning); return; }
                if (folderName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) { CustomMessageBox.Show("Folder name contains invalid characters.", "Validation", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Warning); return; }
                if (NewCreateFolder(folderName)) { pnlFile.Controls.Remove(overlay); overlay.Dispose(); }
            };
            card.Controls.Add(btnCreate);

            txtFolderName.KeyDown += (s, args) =>
            {
                if (args.KeyCode == Keys.Enter) { args.SuppressKeyPress = true; btnCreate.PerformClick(); }
                else if (args.KeyCode == Keys.Escape) { args.SuppressKeyPress = true; btnCancel.PerformClick(); }
            };

            overlay.Click += (s, args) => { pnlFile.Controls.Remove(overlay); overlay.Dispose(); };
            card.Click += (s, args) => { };

            txtFolderName.Focus();
        }

        private bool NewCreateFolder(string folderName)
        {
            if (string.IsNullOrEmpty(saveFolder) || saveFolder == "Null") { CustomMessageBox.Show("Save folder is not set up correctly.", "Notice", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Warning); return false; }

            string basePath = !string.IsNullOrEmpty(currentFolder) && Directory.Exists(currentFolder) ? currentFolder : saveFolder;
            string newFolderPath = Path.Combine(basePath, SanitizeFolderName(folderName));

            if (Directory.Exists(newFolderPath)) { CustomMessageBox.Show("Folder already exists.", "Notice", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Information); return false; }

            try
            {
                using (new LoadingOverlay(this, "Creating folder"))
                {
                    Directory.CreateDirectory(newFolderPath);
                    System.Windows.Forms.Application.DoEvents();
                }

                CustomMessageBox.Show("Folder created!", "Success", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Information);
                LoadServerFolder(basePath, addToHistory: false);
                return true;
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show("Error creating folder: " + ex.Message, "Error", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Error);
                return false;
            }
        }

        private void btnDeleteFile_Click(object sender, EventArgs e) { }

        private string GetFolderPath(int professorId)
        {
            string connStr = SettingsManager.Current.GetConnectionString();
            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand("SELECT FolderPath FROM mainfolderpath WHERE user_id = @id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", professorId);
                        object result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            string path = result.ToString();
                            if (!Directory.Exists(path)) { try { Directory.CreateDirectory(path); } catch { return "Null"; } }
                            return path;
                        }
                    }

                    string rootPath = null;
                    using (var cmd = new MySqlCommand("SELECT FolderPath FROM mainfolderpath WHERE user_id = 0", conn))
                    {
                        object result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value) rootPath = result.ToString();
                    }

                    if (string.IsNullOrEmpty(rootPath))
                    {
                        CustomMessageBox.Show("Root folder has not been configured.\nPlease ask your administrator to set it in Configuration → File Storage.", "Not Configured", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Warning);
                        return "Null";
                    }

                    string folderName = SanitizeFolderName(ProfessorUsername);
                    string newPath = Path.Combine(rootPath, folderName);
                    if (!Directory.Exists(newPath)) Directory.CreateDirectory(newPath);

                    using (var cmd = new MySqlCommand(@"INSERT INTO mainfolderpath (user_id, FolderPath) VALUES (@id, @path) ON DUPLICATE KEY UPDATE FolderPath = @path", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", professorId);
                        cmd.Parameters.AddWithValue("@path", newPath);
                        cmd.ExecuteNonQuery();
                    }

                    return newPath;
                }
            }
            catch (Exception ex) { Console.WriteLine("GetFolderPath error: " + ex.Message); return "Null"; }
        }

        private void ActivityStatus(string filter = "")
        {
            string connStr = SettingsManager.Current.GetConnectionString();
            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = @"SELECT user_id, title, section, student_name, class_name, activity_status, score, file_path FROM submitted_activity WHERE prof_id = @prof_id";

                    if (!string.IsNullOrEmpty(filter)) query += " AND student_name LIKE @f1";
                    if (!string.IsNullOrEmpty(cmbActivityGrades.Text)) query += " AND title = @title";
                    if (!string.IsNullOrEmpty(cmbSectionGrades.Text)) query += " AND section = @section";
                    if (!string.IsNullOrEmpty(cmbSubjectGrades.Text)) query += " AND class_name = @class_name";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@prof_id", ProfessorID);
                        if (!string.IsNullOrEmpty(cmbActivityGrades.Text)) cmd.Parameters.AddWithValue("@title", cmbActivityGrades.Text);
                        if (!string.IsNullOrEmpty(cmbSectionGrades.Text)) cmd.Parameters.AddWithValue("@section", cmbSectionGrades.Text);
                        if (!string.IsNullOrEmpty(cmbSubjectGrades.Text)) cmd.Parameters.AddWithValue("@class_name", cmbSubjectGrades.Text);

                        if (!string.IsNullOrEmpty(filter)) { string f = "%" + filter + "%"; cmd.Parameters.AddWithValue("@f1", f); }

                        MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        dgvStudentActivitySubmitted.DataSource = dt;
                    }
                    GetSection();
                }
            }
            catch (Exception ex) { Console.WriteLine("ActivityStatus error: " + ex.Message); }
        }

        private void txtSearchGrades_TextChanged(object sender, EventArgs e) => ActivityStatus(txtSearchGrades.Text.Trim());
        private void cmbActivityGrades_SelectedIndexChanged(object sender, EventArgs e) { lblGradesActivity.Text = ""; ActivityStatus(); }
        private void cmbSectionGrades_SelectedIndexChanged(object sender, EventArgs e) { lblGradesSection.Text = ""; ActivityStatus(); }
        private void cmbSubjectGrades_SelectedIndexChanged(object sender, EventArgs e) { lblGradesSubject.Text = ""; ActivityStatus(); }

        private void btnGradesClearFilter_Click(object sender, EventArgs e)
        {
            txtSearchGrades.Text = "";
            cmbActivityGrades.SelectedIndex = -1;
            cmbSectionGrades.SelectedIndex = -1;
            cmbSubjectGrades.SelectedIndex = -1;
            lblGradesActivity.Text = "Activity";
            lblGradesSection.Text = "Section";
            lblGradesSubject.Text = "Subject";
        }

        private static int CountTotalSubmitted(int ProfessorID)
        {
            string connStr = SettingsManager.Current.GetConnectionString();
            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM submitted_activity WHERE prof_id = @prof_id AND activity_status = 'Submitted'", conn))
                    {
                        cmd.Parameters.AddWithValue("@prof_id", ProfessorID);
                        return Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }
            }
            catch { return 0; }
        }

        private static int CountTotalGraded(int ProfessorID)
        {
            string connStr = SettingsManager.Current.GetConnectionString();
            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM submitted_activity WHERE prof_id = @prof_id AND score IS NOT NULL", conn))
                    {
                        cmd.Parameters.AddWithValue("@prof_id", ProfessorID);
                        return Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }
            }
            catch { return 0; }
        }

        private static int CountTotalNotSubmitted(int ProfessorID)
        {
            string connStr = SettingsManager.Current.GetConnectionString();
            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM submitted_activity WHERE prof_id = @prof_id AND activity_status = 'Incomplete'", conn))
                    {
                        cmd.Parameters.AddWithValue("@prof_id", ProfessorID);
                        return Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }
            }
            catch { return 0; }
        }

        private void CreatePanelForSubmittedFiles(int User_id, string Title, string Name, string Section, string ClassName, string Status, string filePath)
        {
            try
            {
                SubmittedActivityForm viewer = new SubmittedActivityForm(ProfessorID, User_id, Title, Name, Section, ClassName, Status, filePath);
                viewer.ShowDialog(this);
                ActivityStatus();
            }
            catch (Exception ex)
            {
                Console.WriteLine("CreatePanelForSubmittedFiles error: " + ex.Message);
                CustomMessageBox.Show("Unable to open submission viewer:\n\n" + ex.Message, "Open Error", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Error);
            }
        }

        private void dgvStudentActivitySubmitted_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            DataGridView.HitTestInfo hit = dgvStudentActivitySubmitted.HitTest(e.X, e.Y);
            if (hit.RowIndex < 0) return;
            DataGridViewRow selectedRow = dgvStudentActivitySubmitted.Rows[hit.RowIndex];
            if (selectedRow.Cells["user_id"].Value == null || selectedRow.Cells["user_id"].Value == DBNull.Value) return;

            try
            {
                int user_id = Convert.ToInt32(selectedRow.Cells["user_id"].Value);
                string title = selectedRow.Cells["title"].Value?.ToString()?.Trim() ?? "";
                string name = selectedRow.Cells["student_name"].Value?.ToString()?.Trim() ?? "";
                string sectionGrades = selectedRow.Cells["section"].Value?.ToString()?.Trim() ?? "";
                string classNameGrades = selectedRow.Cells["class_name"].Value?.ToString()?.Trim() ?? "";
                string statusGrades = selectedRow.Cells["activity_status"].Value?.ToString()?.Trim() ?? "";
                string filePath = selectedRow.Cells["file_path"].Value?.ToString() ?? "";
                CreatePanelForSubmittedFiles(user_id, title, name, sectionGrades, classNameGrades, statusGrades, filePath);
            }
            catch (Exception ex) { Console.WriteLine("dgvStudentActivitySubmitted_MouseDoubleClick error: " + ex.Message); }
        }

        private void GetSection()
        {
            string connStr = SettingsManager.Current.GetConnectionString();
            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand("SELECT class_name, class_section FROM professor_class WHERE professor_id = @professor_id", conn))
                    {
                        cmd.Parameters.AddWithValue("@professor_id", ProfessorID);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string className = reader.GetString("class_name");
                                string classSection = reader.GetString("class_section");
                                if (!cmbSubjectGrades.Items.Contains(className)) cmbSubjectGrades.Items.Add(className);
                                if (!cmbSectionGrades.Items.Contains(classSection)) cmbSectionGrades.Items.Add(classSection);
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine("GetSection error: " + ex.Message); }
        }

        private void btnWorkStationMonitoring_Click(object sender, EventArgs e) => pnlWorkStationMonitoring.BringToFront();

        private async Task StartBroadcastListener()
        {
            try
            {
                broadcastListener = new TcpListener(IPAddress.Any, SettingsManager.Current.BroadcastPort);
                broadcastListener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                broadcastListener.Start();
            }
            catch (Exception ex) { Console.WriteLine("[Professor] Broadcast bind FAILED: " + ex.Message); return; }

            while (isRunning)
            {
                try
                {
                    TcpClient client = await broadcastListener.AcceptTcpClientAsync();
                    string clientIp = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
                    broadcastClients[clientIp] = client;
                }
                catch (ObjectDisposedException) { break; }
                catch { if (!isRunning) break; }
            }
        }

        private void btnShareScreen_Click(object sender, EventArgs e)
        {
            if (broadcastTimer != null && broadcastTimer.Enabled) return;
            broadcastTimer = new System.Windows.Forms.Timer();
            broadcastTimer.Interval = 300;
            broadcastTimer.Tick += BroadcastTimer_Tick;
            broadcastTimer.Start();
        }

        private void BroadcastTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                Bitmap screenshot = CaptureScreen();
                using (MemoryStream ms = new MemoryStream())
                {
                    screenshot.Save(ms, ImageFormat.Jpeg);
                    byte[] imageBytes = ms.ToArray();
                    byte[] lengthPrefix = BitConverter.GetBytes(imageBytes.Length);

                    foreach (var kvp in broadcastClients.ToList())
                    {
                        try
                        {
                            NetworkStream stream = kvp.Value.GetStream();
                            stream.Write(lengthPrefix, 0, lengthPrefix.Length);
                            stream.Write(imageBytes, 0, imageBytes.Length);
                        }
                        catch
                        {
                            try { kvp.Value.Close(); } catch { }
                            try { kvp.Value.Dispose(); } catch { }
                            broadcastClients.Remove(kvp.Key);
                        }
                    }
                }
                screenshot.Dispose();
            }
            catch (Exception ex) { Console.WriteLine("Broadcast error: " + ex.Message); }
        }

        private Bitmap CaptureScreen()
        {
            Rectangle bounds = Screen.PrimaryScreen.Bounds;
            Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height);
            using (Graphics g = Graphics.FromImage(bitmap)) g.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);
            return bitmap;
        }

        private void btnStopSharing_Click(object sender, EventArgs e) { broadcastTimer?.Stop(); }

        private void ShutdownStartListener(string clientIp)
        {
            try
            {
                TcpClient client = new TcpClient(clientIp, SettingsManager.Current.CommandPort);
                NetworkStream stream = client.GetStream();
                byte[] data = Encoding.UTF8.GetBytes("SHUTDOWN");
                stream.Write(data, 0, data.Length);
                client.Close();
                CustomMessageBox.Show("Shutdown command sent!", "Success", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Information);
            }
            catch { CustomMessageBox.Show("Error: Client not reachable", "Error", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Error); }
        }

        private void btnShutdown_Click(object sender, EventArgs e) => ShutdownStartListener(SelectedIP);

        private void RestartStartListener(string clientIp)
        {
            try
            {
                TcpClient client = new TcpClient(clientIp, SettingsManager.Current.CommandPort);
                NetworkStream stream = client.GetStream();
                byte[] data = Encoding.UTF8.GetBytes("RESTART");
                stream.Write(data, 0, data.Length);
                client.Close();
                CustomMessageBox.Show("Restart command sent!", "Success", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Information);
            }
            catch { CustomMessageBox.Show("Error: Client not reachable", "Error", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Error); }
        }

        private void btnReboot_Click(object sender, EventArgs e) => RestartStartListener(SelectedIP);

        private async Task StartScreenListener()
        {
            try
            {
                screenListener = new TcpListener(IPAddress.Any, SettingsManager.Current.ScreenSharePort);
                screenListener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                screenListener.Start();
            }
            catch (Exception ex) { Console.WriteLine("[Professor] Screen bind FAILED: " + ex.Message); return; }

            while (isRunning)
            {
                try
                {
                    TcpClient client = await screenListener.AcceptTcpClientAsync();
                    _ = ReceiveScreenStream(client);
                }
                catch (ObjectDisposedException) { break; }
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
                        SafeInvoke(() => UpdateScreenViewer(clientIp, frame));
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
            bool viewerIsOpen;
            lock (screenViewersLock)
            {
                viewerIsOpen = screenViewers.ContainsKey(clientIp) && screenViewers[clientIp] != null;
                if (viewerIsOpen)
                {
                    PictureBox pb = screenViewers[clientIp];
                    try { Image oldImage = pb.Image; pb.Image = (Image)frame.Clone(); oldImage?.Dispose(); }
                    catch { viewerIsOpen = false; }
                }
            }

            bool shouldUpdateThumbnail = !lastThumbnailUpdate.ContainsKey(clientIp)
                || (DateTime.Now - lastThumbnailUpdate[clientIp]) >= thumbnailInterval;

            if (shouldUpdateThumbnail && workstationButtons.ContainsKey(clientIp))
            {
                Button btn = workstationButtons[clientIp];

                int targetW = btn.ClientSize.Width;
                int targetH = btn.ClientSize.Height;

                if (targetW > 0 && targetH > 0)
                {
                    Image thumbnail = ResizeImageToFit(frame, targetW, targetH);
                    Image oldThumb = btn.BackgroundImage;

                    btn.BackgroundImage = thumbnail;
                    btn.BackgroundImageLayout = ImageLayout.Zoom;

                    oldThumb?.Dispose();
                }

                lastThumbnailUpdate[clientIp] = DateTime.Now;
            }

            if (!viewerIsOpen)
                frame.Dispose();
        }

        private Image ResizeImageToFit(Image original, int targetWidth, int targetHeight)
        {
            Bitmap bmp = new Bitmap(targetWidth, targetHeight);

            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Black);
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

                float srcAspect = (float)original.Width / original.Height;
                float dstAspect = (float)targetWidth / targetHeight;

                int drawW, drawH;
                if (srcAspect > dstAspect)
                {
                    drawW = targetWidth;
                    drawH = (int)(targetWidth / srcAspect);
                }
                else
                {
                    drawH = targetHeight;
                    drawW = (int)(targetHeight * srcAspect);
                }

                int x = (targetWidth - drawW) / 2;
                int y = (targetHeight - drawH) / 2;

                g.DrawImage(original, x, y, drawW, drawH);
            }

            return bmp;
        }

        private void ApplyNoSignal(string clientIp)
        {
            if (workstationButtons.ContainsKey(clientIp))
            {
                Button btn = workstationButtons[clientIp];
                btn.BackColor = Color.Gray;
                Image oldThumb = btn.BackgroundImage;
                btn.BackgroundImage = CreateNoSignalImage(btn.Width, btn.Height);
                btn.BackgroundImageLayout = ImageLayout.Stretch;
                oldThumb?.Dispose();
            }

            if (miniWorkstationButtons.ContainsKey(clientIp))
            {
                Button miniBtn = miniWorkstationButtons[clientIp];
                miniBtn.BackColor = Color.Gray;
                Image oldMiniThumb = miniBtn.BackgroundImage;
                miniBtn.BackgroundImage = CreateNoSignalImage(miniBtn.Width, miniBtn.Height);
                miniBtn.BackgroundImageLayout = ImageLayout.Stretch;
                oldMiniThumb?.Dispose();
            }
        }

        private Image CreateNoSignalImage(int width, int height)
        {
            Bitmap bmp = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.FromArgb(45, 45, 45));
                using (Pen stripePen = new Pen(Color.FromArgb(60, 60, 60), 8))
                {
                    for (int i = -height; i < width; i += 20) g.DrawLine(stripePen, i, height, i + height, 0);
                }
                using (Font font = new Font("Segoe UI", 9, FontStyle.Bold))
                using (Brush brush = new SolidBrush(Color.LightGray))
                {
                    string text = "NO SIGNAL";
                    SizeF textSize = g.MeasureString(text, font);
                    float x = (width - textSize.Width) / 2;
                    float y = (height - textSize.Height) / 2;
                    g.DrawString(text, font, brush, x, y);
                }
            }
            return bmp;
        }

        private void AddScreenViewer(string workstationId)
        {
            ScreenViewerForm viewer = new ScreenViewerForm(workstationId);
            lock (screenViewersLock) { screenViewers[workstationId] = viewer.GetPictureBox(); }
            viewer.FormClosed += (s, args) => { lock (screenViewersLock) { screenViewers.Remove(workstationId); } };
            viewer.Show();
        }

        private void btnRemoteView_Click(object sender, EventArgs e) => AddScreenViewer(SelectedIP);

        private void btnSettingProfileExpand_Click(object sender, EventArgs e)
        {
            if (pnlSettingProfile.Height <= 350) pnlSettingProfile.Height = 733;
            else if (pnlSettingProfile.Height >= 733) pnlSettingProfile.Height = 350;
        }

        private void ClearAllFormData() => ClearAllFormData(this);

        private void ClearAllFormData(Control parent)
        {
            foreach (Control ctrl in parent.Controls)
            {
                if (ctrl is TextBox tb) tb.Text = "";
                else if (ctrl is ComboBox cb) cb.SelectedIndex = -1;
                else if (ctrl is DataGridView dgv) dgv.DataSource = null;
                else if (ctrl is ListBox lb) lb.Items.Clear();
                else if (ctrl.HasChildren) ClearAllFormData(ctrl);
            }
        }

        private void StopServer()
        {
            if (!isRunning) return;
            isRunning = false;

            try { listener?.Stop(); } catch { }
            try { broadcastListener?.Stop(); } catch { }
            try { screenListener?.Stop(); } catch { }
            try { activityFileListener?.Stop(); } catch { }

            try { listener?.Server?.Close(); } catch { }
            try { broadcastListener?.Server?.Close(); } catch { }
            try { screenListener?.Server?.Close(); } catch { }
            try { activityFileListener?.Server?.Close(); } catch { }

            try { listener?.Server?.Dispose(); } catch { }
            try { broadcastListener?.Server?.Dispose(); } catch { }
            try { screenListener?.Server?.Dispose(); } catch { }
            try { activityFileListener?.Server?.Dispose(); } catch { }

            listener = null;
            broadcastListener = null;
            screenListener = null;
            activityFileListener = null;

            try { broadcastTimer?.Stop(); } catch { }
            try { broadcastTimer?.Dispose(); } catch { }
            broadcastTimer = null;

            foreach (var kvp in broadcastClients) { try { kvp.Value.Close(); } catch { } try { kvp.Value.Dispose(); } catch { } }
            broadcastClients.Clear();

            cachedProfileImage?.Dispose();
            cachedProfileImage = null;
        }

        private void Logout()
        {
            var result = CustomMessageBox.Show("Are you sure you want to logout?", "Logout Confirmation", CustomMessageBoxButtons.YesNo, CustomMessageBoxIcon.Question);

            if (result == CustomMessageBoxResult.Yes)
            {
                StopServer();
                ClearAllFormData();

                Login login = new Login();
                login.Show();

                this.Hide();
                this.Close();
            }
        }

        private void btnSignOut_Click(object sender, EventArgs e) => Logout();
        private void btnSignOut2_Click(object sender, EventArgs e) => Logout();

        private void btnSettingChangeUsername_Click(object sender, EventArgs e) { pnlChangeUsername.Visible = true; pnlChangePassword.Visible = false; pnlChangePhoto.Visible = false; }
        private void btnExitChangeUsernamePanel_Click(object sender, EventArgs e) => pnlChangeUsername.Visible = false;

        private async void btnSubmitChangeUsername_Click(object sender, EventArgs e)
        {
            string connStr = SettingsManager.Current.GetConnectionString();
            if (string.IsNullOrEmpty(txtCurrentUsername.Text) || string.IsNullOrEmpty(txtNewUsername.Text)) { CustomMessageBox.Show("Please enter both the current and new usernames.", "Validation", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Warning); return; }
            if (txtCurrentUsername.Text != ProfessorUsername) { CustomMessageBox.Show("Please enter the Correct usernames.", "Validation", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Warning); return; }

            string newUsername = txtNewUsername.Text.Trim();
            string currentUsername = txtCurrentUsername.Text.Trim();

            try
            {
                await RunWithLoadingAsync("Updating username", async () =>
                {
                    await Task.Run(() =>
                    {
                        using (var conn = new MySqlConnection(connStr))
                        {
                            conn.Open();
                            string query = @"UPDATE user_credential SET username = @new_username WHERE username = @current_username";
                            using (var cmd = new MySqlCommand(query, conn))
                            {
                                cmd.Parameters.AddWithValue("@new_username", newUsername);
                                cmd.Parameters.AddWithValue("@current_username", currentUsername);
                                cmd.ExecuteNonQuery();
                            }
                        }
                    });
                });

                ProfessorUsername = newUsername;
                ClearTextSettings();
                CustomMessageBox.Show("Username updated successfully.", "Success", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Information);
            }
            catch (Exception ex) { Console.WriteLine("btnSubmitChangeUsername_Click error: " + ex.Message); }
        }

        private void btnSettingChangePassword_Click(object sender, EventArgs e) { pnlChangePassword.Visible = true; pnlChangeUsername.Visible = false; pnlChangePhoto.Visible = false; }
        private void btnExitChangePasswordPanel_Click(object sender, EventArgs e) => pnlChangePassword.Visible = false;

        private async void btnSubmitChangePassword_Click(object sender, EventArgs e)
        {
            string connStr = SettingsManager.Current.GetConnectionString();
            if (string.IsNullOrEmpty(txtCurrentPassword.Text) || string.IsNullOrEmpty(txtNewPassword.Text) || string.IsNullOrEmpty(txtConfirmPassword.Text)) { CustomMessageBox.Show("Please fill in all fields.", "Validation", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Warning); return; }
            if (txtNewPassword.Text != txtConfirmPassword.Text) { CustomMessageBox.Show("New password and confirm password do not match.", "Validation", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Warning); return; }

            string newPassword = txtNewPassword.Text.Trim();
            string username = ProfessorUsername;

            try
            {
                await RunWithLoadingAsync("Updating password", async () =>
                {
                    await Task.Run(() =>
                    {
                        using (var conn = new MySqlConnection(connStr))
                        {
                            conn.Open();
                            string query = @"UPDATE user_credential SET p_word = @new_password WHERE username = @current_username";
                            using (var cmd = new MySqlCommand(query, conn))
                            {
                                cmd.Parameters.AddWithValue("@new_password", newPassword);
                                cmd.Parameters.AddWithValue("@current_username", username);
                                cmd.ExecuteNonQuery();
                            }
                        }
                    });
                });

                ClearTextSettings();
                CustomMessageBox.Show("Password updated successfully.", "Success", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Information);
            }
            catch (Exception ex) { Console.WriteLine("btnSubmitChangePassword_Click error: " + ex.Message); }
        }

        private void ClearTextSettings()
        {
            txtCurrentUsername.Text = "";
            txtNewUsername.Text = "";
            txtCurrentPassword.Text = "";
            txtNewPassword.Text = "";
            txtConfirmPassword.Text = "";
        }

        private void InitializeSaveDirectory()
        {
            string solutionDirectory = AppDomain.CurrentDomain.BaseDirectory;
            SaveCurrentProfilePath = Path.Combine(solutionDirectory, "StudentProfilePicture");
            if (!Directory.Exists(SaveCurrentProfilePath)) Directory.CreateDirectory(SaveCurrentProfilePath);
        }

        private void btnUploadPhoto_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    CurrentProfilePath = ofd.FileName;
                    picboxNewPicture.Image?.Dispose();
                    using (var fs = new FileStream(CurrentProfilePath, FileMode.Open, FileAccess.Read)) picboxNewPicture.Image = Image.FromStream(fs);
                    picboxNewPicture.SizeMode = PictureBoxSizeMode.Zoom;
                    btnSubmitChangePhoto.Enabled = true;
                }
            }
        }

        private async void btnSubmitChangePhoto_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(CurrentProfilePath)) { CustomMessageBox.Show("Upload an image first!", "Validation", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Warning); return; }
            if (!File.Exists(CurrentProfilePath)) { CustomMessageBox.Show("The selected file no longer exists.", "File Error", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Warning); return; }

            try
            {
                byte[] imageBytes = null;
                string uncPath = null;

                await RunWithLoadingAsync("Uploading profile photo", async () =>
                {
                    imageBytes = await File.ReadAllBytesAsync(CurrentProfilePath);
                    string ext = Path.GetExtension(CurrentProfilePath);
                    string fileName = SanitizeFolderName(ProfessorUsername) + "_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ext;
                    uncPath = await SendProfilePhotoToAdmin(imageBytes, fileName);
                });

                if (string.IsNullOrEmpty(uncPath)) { CustomMessageBox.Show("Failed to send profile picture to server.", "Server Error", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Warning); return; }

                await RunWithLoadingAsync("Saving profile photo", async () =>
                {
                    await Task.Run(() =>
                    {
                        string connStr = SettingsManager.Current.GetConnectionString();
                        using (var conn = new MySqlConnection(connStr))
                        {
                            conn.Open();
                            string query = @"UPDATE user_credential SET profile_picture = @profile_picture WHERE username = @username";
                            using (var cmd = new MySqlCommand(query, conn))
                            {
                                cmd.Parameters.AddWithValue("@profile_picture", uncPath);
                                cmd.Parameters.AddWithValue("@username", ProfessorUsername);
                                int rows = cmd.ExecuteNonQuery();
                                if (rows == 0) throw new Exception("No user row was updated. Check the username.");
                            }
                        }
                    });
                });

                profileImageLoaded = false;
                cachedProfileImage?.Dispose();
                cachedProfileImage = null;

                InitializeChangingPicture();
                CustomMessageBox.Show("Profile picture updated successfully.", "Success", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Information);
            }
            catch (Exception ex) { Console.WriteLine("btnSubmitChangePhoto_Click error: " + ex); CustomMessageBox.Show("Error: " + ex.Message, "Error", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Error); }
        }

        private async Task<string> SendProfilePhotoToAdmin(byte[] imageBytes, string fileName)
        {
            try
            {
                string adminIp = SettingsManager.Current.ServerIp.TrimStart('\\').TrimEnd('\\');
                int adminPort = SettingsManager.Current.FileTransferPort;

                using (var client = new TcpClient())
                {
                    var connectTask = client.ConnectAsync(adminIp, adminPort);
                    var timeoutTask = Task.Delay(5000);
                    var completed = await Task.WhenAny(connectTask, timeoutTask);

                    if (completed == timeoutTask) { Console.WriteLine($"Server ({adminIp}:{adminPort}) not reachable (timeout)."); return null; }
                    await connectTask;
                    if (!client.Connected) { Console.WriteLine($"Server ({adminIp}:{adminPort}) refused the connection."); return null; }

                    string returnedUnc = null;
                    using (var stream = client.GetStream())
                    using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
                    {
                        writer.Write("PROFILE_PHOTO");
                        writer.Write(ProfessorUsername);
                        writer.Write(fileName);
                        writer.Write(imageBytes.Length);
                        writer.Write(imageBytes);
                        writer.Flush();

                        try
                        {
                            var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
                            returnedUnc = reader.ReadString();
                        }
                        catch (Exception ex) { Console.WriteLine("Read reply error: " + ex.Message); }
                    }

                    return returnedUnc;
                }
            }
            catch (Exception ex) { Console.WriteLine("SendProfilePhotoToAdmin error: " + ex.Message); return null; }
        }

        private void InitializeChangingPicture()
        {
            if (profileImageLoaded) return;
            string connStr = SettingsManager.Current.GetConnectionString();
            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand("SELECT profile_picture FROM user_credential WHERE username = @username", conn))
                    {
                        cmd.Parameters.AddWithValue("@username", ProfessorUsername);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                if (reader.IsDBNull(reader.GetOrdinal("profile_picture"))) return;
                                string path = reader.GetString("profile_picture");
                                if (!File.Exists(path)) return;

                                byte[] bytes = File.ReadAllBytes(path);
                                using (var ms = new MemoryStream(bytes))
                                {
                                    var temp = Image.FromStream(ms);
                                    cachedProfileImage = new Bitmap(temp);
                                    temp.Dispose();
                                }

                                picboxSettingProfilePicture.Image?.Dispose();
                                picboxSettingProfilePicture.Image = cachedProfileImage;
                                picboxSettingProfilePicture.SizeMode = PictureBoxSizeMode.Zoom;

                                btnAccount.Image?.Dispose();
                                btnAccount.Image = cachedProfileImage;

                                profileImageLoaded = true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine("InitializeChangingPicture error: " + ex.Message); }
        }

        private void btnSettingChangePhoto_Click(object sender, EventArgs e) { pnlChangePhoto.Visible = true; pnlChangeUsername.Visible = false; pnlChangePassword.Visible = false; }
        private void btnExitChangePhotoPanel_Click(object sender, EventArgs e) => pnlChangePhoto.Visible = false;

        private void NameGet()
        {
            string connStr = SettingsManager.Current.GetConnectionString();
            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand("SELECT lastname, firstname, middlename FROM user_information WHERE user_id = @user_id", conn))
                    {
                        cmd.Parameters.AddWithValue("@user_id", ProfessorID);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string Lastname = reader.GetString("lastname");
                                string Firstname = reader.GetString("firstname");
                                string Middlename = reader.GetString("middlename");
                                ProfessorName = $"{Lastname}_{Firstname}_{Middlename}";
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine("NameGet error: " + ex.Message); }
        }

        private async Task StartActivityFileServer()
        {
            try
            {
                activityFileListener = new TcpListener(IPAddress.Any, SettingsManager.Current.FileTransferPort);
                activityFileListener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                activityFileListener.Start();
            }
            catch (Exception ex) { Console.WriteLine("[Professor] FileTransfer bind FAILED: " + ex.Message); return; }

            while (isRunning)
            {
                try
                {
                    TcpClient client = await activityFileListener.AcceptTcpClientAsync();
                    _ = HandleActivityFileReceive(client);
                }
                catch (ObjectDisposedException) { break; }
                catch (Exception ex) { if (!isRunning) break; Console.WriteLine("[Professor] FileTransfer accept error: " + ex.Message); }
            }
        }

        // =========================================================
        // *** FIXED HandleActivityFileReceive ***
        // Reads the command first, then dispatches to the correct protocol
        // =========================================================

        private async Task HandleActivityFileReceive(TcpClient client)
        {
            try
            {
                using (client)
                using (NetworkStream stream = client.GetStream())
                using (BinaryReader reader = new BinaryReader(stream, Encoding.UTF8))
                {
                    // =========================================================
                    // Read the command header FIRST
                    // =========================================================
                    string command = reader.ReadString();
                    Console.WriteLine("[Professor] FileTransfer command: " + command);

                    if (command == "STUDENT_SUBMISSION")
                    {
                        // Matches ActivityForm.SendSubmissionToServer
                        string professorFolder = reader.ReadString();
                        string section = reader.ReadString();
                        string studentName = reader.ReadString();
                        string activityTitle = reader.ReadString();
                        string fileName = reader.ReadString();
                        int fileLength = reader.ReadInt32();

                        if (fileLength <= 0 || fileLength > 200 * 1024 * 1024)
                        {
                            Console.WriteLine("[Professor] Invalid file length: " + fileLength);
                            return;
                        }

                        byte[] fileBytes = reader.ReadBytes(fileLength);

                        if (string.IsNullOrEmpty(saveFolder) || saveFolder == "Null")
                        {
                            Console.WriteLine("[Professor] Save folder not configured.");
                            return;
                        }

                        // Save under the section folder directly (student submission)
                        string sectionFolder = Path.Combine(saveFolder, SanitizeFolderName(section));
                        if (!Directory.Exists(sectionFolder))
                            Directory.CreateDirectory(sectionFolder);

                        string safeName = SanitizeFolderName(fileName);
                        string savePath = Path.Combine(sectionFolder, safeName);

                        await File.WriteAllBytesAsync(savePath, fileBytes);
                        Console.WriteLine("[Professor] Student submission saved: " + savePath);

                        // Look up student ID by name
                        int studentId = GetStudentIdByName(studentName);

                        // Update the database with the correct save path
                        UpdateSubmissionFilePath(studentId, activityTitle, section, savePath);

                        // =========================================================
                        // *** IMPORTANT: REPLY WITH THE SAVED PATH ***
                        // Without this, the Student's ReadString() will fail/hang
                        // and the student will see "Failed to submit. Please try again."
                        // =========================================================
                        try
                        {
                            using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
                            {
                                writer.Write(savePath);
                                writer.Flush();
                            }
                            Console.WriteLine("[Professor] Replied with path: " + savePath);
                        }
                        catch (Exception exReply)
                        {
                            Console.WriteLine("[Professor] Failed to send reply: " + exReply.Message);
                        }

                        // Refresh UI
                        SafeInvoke(() =>
                        {
                            try { ActivityStatus(); } catch { }
                        });
                    }
                    else if (command == "ACTIVITY_FILE")
                    {
                        // Matches ProfessorForm.SendActivityFileToServer
                        string professorFolder = reader.ReadString();
                        string section = reader.ReadString();
                        string fileName = reader.ReadString();
                        int fileLength = reader.ReadInt32();

                        if (fileLength <= 0 || fileLength > 200 * 1024 * 1024)
                            return;

                        byte[] fileBytes = reader.ReadBytes(fileLength);

                        if (string.IsNullOrEmpty(saveFolder) || saveFolder == "Null") return;
                        if (string.IsNullOrEmpty(ProfessorName)) NameGet();

                        string professorFolderSafe = string.IsNullOrEmpty(ProfessorName)
                            ? "Unknown"
                            : SanitizeFolderName(ProfessorName);

                        string targetFolder = Path.Combine(
                            saveFolder,
                            professorFolderSafe,
                            SanitizeFolderName(section),
                            "ActivityFiles");

                        if (!Directory.Exists(targetFolder))
                            Directory.CreateDirectory(targetFolder);

                        string savePath = Path.Combine(targetFolder, SanitizeFolderName(fileName));
                        await File.WriteAllBytesAsync(savePath, fileBytes);
                        Console.WriteLine("[Professor] Activity file saved: " + savePath);
                    }
                    else if (command == "PROFILE_PHOTO")
                    {
                        // Matches StudentForm.SendProfilePhotoToAdmin
                        string username = reader.ReadString();
                        string fileName = reader.ReadString();
                        int fileLength = reader.ReadInt32();

                        if (fileLength <= 0 || fileLength > 200 * 1024 * 1024)
                            return;

                        byte[] fileBytes = reader.ReadBytes(fileLength);

                        string profileFolder = Path.Combine(saveFolder ?? "", "ProfilePictures");
                        if (!Directory.Exists(profileFolder))
                            Directory.CreateDirectory(profileFolder);

                        string savePath = Path.Combine(profileFolder, SanitizeFolderName(fileName));
                        await File.WriteAllBytesAsync(savePath, fileBytes);

                        // Reply back with the UNC path
                        try
                        {
                            using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
                            {
                                writer.Write(savePath);
                                writer.Flush();
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("[Professor] Failed to send reply: " + ex.Message);
                        }

                        Console.WriteLine("[Professor] Profile photo saved: " + savePath);
                    }
                    else
                    {
                        Console.WriteLine("[Professor] Unknown FileTransfer command: " + command);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("HandleActivityFileReceive error: " + ex.Message);
            }
        }
        // =========================================================
        // NEW: Get student_id from "Lastname_Firstname_Middlename"
        // =========================================================
        private int GetStudentIdByName(string studentName)
        {
            if (string.IsNullOrWhiteSpace(studentName)) return 0;

            string[] parts = studentName.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
            string lastname = parts.Length > 0 ? parts[0].Trim() : "";
            string firstname = parts.Length > 1 ? parts[1].Trim() : "";
            string middlename = parts.Length > 2 ? parts[2].Trim() : "";

            string connStr = SettingsManager.Current.GetConnectionString();
            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = @"SELECT user_id FROM user_information
                                     WHERE LOWER(TRIM(lastname))   = LOWER(TRIM(@ln))
                                       AND LOWER(TRIM(firstname))  = LOWER(TRIM(@fn))
                                       AND LOWER(TRIM(middlename)) = LOWER(TRIM(@mn))
                                     LIMIT 1";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@ln", lastname);
                        cmd.Parameters.AddWithValue("@fn", firstname);
                        cmd.Parameters.AddWithValue("@mn", middlename);

                        object result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                            return Convert.ToInt32(result);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetStudentIdByName error: " + ex.Message);
            }
            return 0;
        }

        // =========================================================
        // NEW: Update submitted_activity file path + status
        // =========================================================
        private void UpdateSubmissionFilePath(int studentId, string title, string section, string savedPath)
        {
            if (studentId <= 0 || string.IsNullOrEmpty(title)) return;

            string connStr = SettingsManager.Current.GetConnectionString();
            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = @"UPDATE submitted_activity
                                     SET file_path = @path,
                                         activity_status = 'Submitted'
                                     WHERE prof_id = @prof_id
                                       AND user_id = @user_id
                                       AND title   = @title
                                       AND section = @section";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@path", savedPath);
                        cmd.Parameters.AddWithValue("@prof_id", ProfessorID);
                        cmd.Parameters.AddWithValue("@user_id", studentId);
                        cmd.Parameters.AddWithValue("@title", title);
                        cmd.Parameters.AddWithValue("@section", section);

                        int rows = cmd.ExecuteNonQuery();
                        Console.WriteLine($"[Professor] Updated {rows} submitted_activity row(s) for student {studentId}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("UpdateSubmissionFilePath error: " + ex.Message);
            }
        }

        private string SanitizeFolderName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "Unknown";
            foreach (char c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
            name = name.Trim().TrimEnd('.');
            if (string.IsNullOrWhiteSpace(name)) return "Unknown";
            return name;
        }

        private void btnCreateAssessment_Click(object sender, EventArgs e) { ProfessorQuizForm profQuiz = new ProfessorQuizForm(ProfessorID); profQuiz.ShowDialog(); }
        private void btnQuizExam_Click(object sender, EventArgs e) { ProfessorGradesForm QuizGradeform = new ProfessorGradesForm(); QuizGradeform.ShowDialog(); }

        private async void btnDeleteFile_Click_1(object sender, EventArgs e)
        {
            if (FolderListView.SelectedItems.Count == 0) { CustomMessageBox.Show("Please select a file or folder first.", "Nothing Selected", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Information); return; }
            string path = FolderListView.SelectedItems[0].Tag?.ToString();
            if (string.IsNullOrEmpty(path)) { CustomMessageBox.Show("Invalid selection.", "Notice", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Warning); return; }

            bool isFolder = Directory.Exists(path);
            bool isFile = File.Exists(path);

            if (!isFolder && !isFile) { CustomMessageBox.Show("The selected item no longer exists.", "Notice", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Warning); return; }

            if (isFolder && !string.IsNullOrEmpty(saveFolder) && string.Equals(Path.GetFullPath(path).TrimEnd('\\'), Path.GetFullPath(saveFolder).TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
            {
                CustomMessageBox.Show("You cannot delete your root folder.", "Not Allowed", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Warning);
                return;
            }

            string itemName = Path.GetFileName(path);
            string message = isFolder ? $"Delete folder '{itemName}' and ALL of its contents?\n\nThis cannot be undone." : $"Delete file '{itemName}'?\n\nThis cannot be undone.";
            var confirm = CustomMessageBox.Show(message, "Confirm Delete", CustomMessageBoxButtons.YesNo, CustomMessageBoxIcon.Warning);

            if (confirm != CustomMessageBoxResult.Yes) return;

            try
            {
                await RunWithLoadingAsync("Deleting " + (isFolder ? "folder" : "file"), async () =>
                {
                    await Task.Run(() =>
                    {
                        if (isFile) { var attrs = File.GetAttributes(path); if ((attrs & FileAttributes.ReadOnly) == FileAttributes.ReadOnly) File.SetAttributes(path, attrs & ~FileAttributes.ReadOnly); }
                        if (isFolder) Directory.Delete(path, recursive: true);
                        else File.Delete(path);
                    });
                });

                CustomMessageBox.Show("Deleted successfully.", "Deleted", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Information);

                if (!string.IsNullOrEmpty(currentFolder) && Directory.Exists(currentFolder)) LoadServerFolder(currentFolder, addToHistory: false);
                else if (!string.IsNullOrEmpty(saveFolder) && Directory.Exists(saveFolder)) LoadServerFolder(saveFolder, addToHistory: false);
            }
            catch (UnauthorizedAccessException) { CustomMessageBox.Show("Access denied.\n\nThe file/folder may be open in another program or you don't have permission.", "Delete Failed", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Error); }
            catch (IOException ioEx) { CustomMessageBox.Show("The file is in use or locked.\n\nDetails: " + ioEx.Message, "Delete Failed", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Error); }
            catch (Exception ex) { CustomMessageBox.Show("Error deleting: " + ex.Message, "Delete Failed", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Error); }
        }

        private void InitializeComboBoxes()
        {
            ConfigureCombo(cmbSection, "Select Section", "Section");
            ConfigureCombo(cmbYear, "Select Year", "Year");
            ConfigureCombo(cmbSemester, "Select Semester", "Semester");
            ConfigureCombo(cmbClassDate, "Select Day", "Day");
            ConfigureCombo(cmbActivityTitle, "Select Title", "Title");
            ConfigureCombo(cmbActivitySection, "Select Section", "Section");
            ConfigureCombo(cmbActivitySubject, "Select Subject", "Subject");
        }

        private void ConfigureCombo(Guna.UI2.WinForms.Guna2ComboBox cmb, string placeholder, string fieldName)
        {
            if (cmb == null) return;
            cmb.DropDownStyle = ComboBoxStyle.DropDownList;
            cmb.Tag = fieldName;
            if (!cmb.Items.Contains(placeholder)) cmb.Items.Insert(0, placeholder);
            cmb.SelectedIndex = 0;
        }

        private void InitializeNavTooltips()
        {
            navToolTip = new ToolTip();
            navToolTip.AutoPopDelay = 5000;
            navToolTip.InitialDelay = 350;
            navToolTip.ReshowDelay = 100;
            navToolTip.ShowAlways = true;
            navToolTip.IsBalloon = false;
            navToolTip.ToolTipTitle = "";
            navToolTip.UseFading = true;
            navToolTip.UseAnimation = true;

            navToolTip.SetToolTip(btnHome, "Home");
            navToolTip.SetToolTip(btnWorkstation, "Workstations");
            navToolTip.SetToolTip(btnStudent, "My Students");
            navToolTip.SetToolTip(btnActivities, "Activities");
            navToolTip.SetToolTip(btnGrades, "Activities Grades");
            navToolTip.SetToolTip(btnAttendance, "Attendance");
            navToolTip.SetToolTip(btnSubject, "Subjects");
            navToolTip.SetToolTip(btnFile, "Files");
            navToolTip.SetToolTip(btnAccount, "Account");
            navToolTip.SetToolTip(btnQuizExam, "Quiz Exam Grades");
        }

        private void btnCalendarExpand_Click(object sender, EventArgs e) { var cal = new ProfessorCalendarForm(ProfessorID); cal.ShowDialog(this); }
        private void lblPanelName_Click(object sender, EventArgs e) { }

        // =========================================================
        // NOTIFICATIONS — UI
        // =========================================================
        private void BuildNotificationsUi()
        {
            if (btnNotifications != null) return;

            btnNotifications = new Guna2Button
            {
                Size = new Size(46, 46),
                Location = new Point(this.ClientSize.Width - 180, 22),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BorderRadius = 23,
                BackColor = Color.White,
                FillColor = Color.White,
                ForeColor = Color.Maroon,
                Font = new Font("Segoe UI Emoji", 14F, FontStyle.Bold),
                Text = "🔔",
                Cursor = Cursors.Hand
            };
            btnNotifications.HoverState.FillColor = Color.FromArgb(250, 235, 235);
            btnNotifications.Click += (s, e) => ToggleNotificationPanel();

            guna2Panel2.Controls.Add(btnNotifications);
            btnNotifications.BringToFront();

            lblNotificationBadge = new Label
            {
                Size = new Size(22, 22),
                Location = new Point(btnNotifications.Right - 26, btnNotifications.Top - 4),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BackColor = Color.FromArgb(220, 40, 40),
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Text = "0",
                Visible = false
            };
            lblNotificationBadge.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var path = new GraphicsPath())
                {
                    path.AddEllipse(0, 0, lblNotificationBadge.Width - 1, lblNotificationBadge.Height - 1);
                    lblNotificationBadge.Region = new Region(path);
                }
            };
            this.Controls.Add(lblNotificationBadge);
            lblNotificationBadge.BringToFront();

            notificationPanel = new Guna2Panel
            {
                Size = new Size(420, 520),
                Location = new Point(this.ClientSize.Width - 440, 78),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BorderRadius = 14,
                FillColor = Color.White,
                BorderColor = Color.FromArgb(230, 225, 225),
                BorderThickness = 1,
                Visible = false,
                AutoScroll = false
            };

            notificationPanel.ShadowDecoration.Enabled = true;
            notificationPanel.ShadowDecoration.Depth = 16;
            notificationPanel.ShadowDecoration.Color = Color.FromArgb(60, 0, 0, 0);

            Guna2Panel panelHeader = new Guna2Panel { Dock = DockStyle.Top, Height = 55, FillColor = Color.Maroon, BorderRadius = 0 };
            notificationPanel.Controls.Add(panelHeader);

            Label lblPanelTitle = new Label
            {
                Text = "🔔  Notifications",
                Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(18, 0, 0, 0)
            };
            panelHeader.Controls.Add(lblPanelTitle);

            Guna2Button btnMarkAllRead = new Guna2Button
            {
                Text = "Mark all read",
                Size = new Size(120, 32),
                Location = new Point(panelHeader.Width - 130, 12),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BorderRadius = 8,
                FillColor = Color.FromArgb(60, 255, 255, 255),
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold)
            };
            btnMarkAllRead.HoverState.FillColor = Color.FromArgb(120, 255, 255, 255);
            btnMarkAllRead.Click += (s, e) =>
            {
                foreach (Control c in notificationList.Controls) { if (c is Panel card && card.Tag is string key) readNotificationKeys.Add(key); }
                LoadNotifications();
            };
            panelHeader.Controls.Add(btnMarkAllRead);
            btnMarkAllRead.BringToFront();

            notificationList = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(10),
                BackColor = Color.White
            };
            notificationPanel.Controls.Add(notificationList);
            notificationList.BringToFront();

            this.Controls.Add(notificationPanel);
            notificationPanel.BringToFront();

            this.Click += (s, e) => CloseNotificationPanel();
            foreach (Control ctrl in this.Controls)
            {
                if (ctrl != notificationPanel && ctrl != btnNotifications && ctrl != lblNotificationBadge)
                    ctrl.Click += (s, e) => CloseNotificationPanel();
            }
        }

        private void ToggleNotificationPanel()
        {
            if (notificationPanel == null) return;
            if (notificationPanelOpen) { CloseNotificationPanel(); }
            else { notificationPanelOpen = true; notificationPanel.Visible = true; notificationPanel.BringToFront(); LoadNotifications(); }
        }

        private void CloseNotificationPanel()
        {
            if (notificationPanel == null) return;
            notificationPanel.Visible = false;
            notificationPanelOpen = false;
        }

        // =========================================================
        // NOTIFICATIONS — DATA
        // =========================================================
        private void LoadNotifications()
        {
            if (notificationList == null) return;
            notificationList.Controls.Clear();

            int unreadCount = 0;
            var items = new List<ProfessorNotificationItem>();

            foreach (var s in LoadNotificationSubmissions())
            {
                string key = "S|" + s.Id;
                bool unread = !readNotificationKeys.Contains(key);
                if (unread) unreadCount++;
                items.Add(new ProfessorNotificationItem { Key = key, Icon = "📄", Title = s.Title, Subtitle = $"{s.StudentName}  •  {s.Section}", Time = s.PostedAt, Type = "submission", Id = s.Id, Unread = unread });
            }

            foreach (var q in LoadNotificationQuizAttempts())
            {
                string key = "Q|" + q.Id;
                bool unread = !readNotificationKeys.Contains(key);
                if (unread) unreadCount++;
                items.Add(new ProfessorNotificationItem { Key = key, Icon = q.Type == "exam" ? "📝" : "📋", Title = q.Title, Subtitle = $"{q.StudentName}  •  {q.Subject}", Time = q.PostedAt, Type = "quiz", Id = q.Id, Unread = unread });
            }

            foreach (var j in LoadNotificationJoinedClasses())
            {
                string key = "J|" + j.Id;
                bool unread = !readNotificationKeys.Contains(key);
                if (unread) unreadCount++;
                items.Add(new ProfessorNotificationItem { Key = key, Icon = "🎓", Title = "New student joined", Subtitle = $"{j.StudentName}  •  {j.Subject}", Time = j.PostedAt, Type = "join", Id = j.Id, Unread = unread });
            }

            items.Sort((a, b) =>
            {
                if (a.Time.HasValue && b.Time.HasValue) return b.Time.Value.CompareTo(a.Time.Value);
                if (a.Time.HasValue) return -1;
                if (b.Time.HasValue) return 1;
                return 0;
            });

            if (items.Count == 0)
            {
                Label lblEmpty = new Label
                {
                    Text = "🔕  No new notifications",
                    Font = new Font("Segoe UI", 10F, FontStyle.Italic),
                    ForeColor = Color.FromArgb(140, 140, 140),
                    AutoSize = false,
                    Size = new Size(notificationList.Width - 30, 80),
                    TextAlign = ContentAlignment.MiddleCenter
                };
                notificationList.Controls.Add(lblEmpty);
            }
            else
            {
                foreach (var item in items) notificationList.Controls.Add(BuildNotificationCard(item));
            }

            if (unreadCount > 0) { lblNotificationBadge.Text = unreadCount > 99 ? "99+" : unreadCount.ToString(); lblNotificationBadge.Visible = true; lblNotificationBadge.BringToFront(); }
            else { lblNotificationBadge.Visible = false; }
        }

        private class ProfessorNotificationItem
        {
            public string Key;
            public string Icon;
            public string Title;
            public string Subtitle;
            public DateTime? Time;
            public string Type;
            public int Id;
            public bool Unread;
        }

        private class SimpleProfNotification
        {
            public int Id;
            public string Title;
            public string Subject;
            public string StudentName;
            public string Section;
            public string Type;
            public DateTime? PostedAt;
        }

        private List<SimpleProfNotification> LoadNotificationSubmissions()
        {
            var list = new List<SimpleProfNotification>();
            string connStr = SettingsManager.Current.GetConnectionString();
            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = @"SELECT sa.submitted_id, sa.title, sa.student_name, sa.section, sa.class_name, sa.activity_status, sa.submitted_at FROM submitted_activity sa WHERE sa.prof_id = @prof_id AND sa.activity_status IN ('Submitted', 'Incomplete') ORDER BY sa.submitted_at DESC LIMIT 30";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@prof_id", ProfessorID);
                        using (var r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                DateTime? posted = null;
                                try { object raw = r["submitted_at"]; if (raw != null && raw != DBNull.Value && DateTime.TryParse(raw.ToString(), out DateTime dt)) posted = dt; } catch { }

                                list.Add(new SimpleProfNotification
                                {
                                    Id = Convert.ToInt32(r["submitted_id"]),
                                    Title = r["title"]?.ToString() ?? "",
                                    StudentName = r["student_name"]?.ToString() ?? "",
                                    Section = r["section"]?.ToString() ?? "",
                                    Subject = r["class_name"]?.ToString() ?? "",
                                    Type = "submission",
                                    PostedAt = posted
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine("LoadNotificationSubmissions error: " + ex.Message); }
            return list;
        }

        private List<SimpleProfNotification> LoadNotificationQuizAttempts()
        {
            var list = new List<SimpleProfNotification>();
            string connStr = SettingsManager.Current.GetConnectionString();
            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = @"SELECT qa.attempt_id, q.quiz_title, q.subject, q.assessment_type, CONCAT(ui.lastname, ', ', ui.firstname) AS student_name, qa.started_at FROM quiz_attempts qa INNER JOIN quizzes q ON q.quiz_id = qa.quiz_id LEFT JOIN user_information ui ON ui.user_id = qa.user_id WHERE q.created_by = @prof_id AND qa.status = 'SUBMITTED' ORDER BY qa.started_at DESC LIMIT 30";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@prof_id", ProfessorID);
                        using (var r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                DateTime? posted = null;
                                try { object raw = r["started_at"]; if (raw != null && raw != DBNull.Value && DateTime.TryParse(raw.ToString(), out DateTime dt)) posted = dt; } catch { }

                                list.Add(new SimpleProfNotification
                                {
                                    Id = Convert.ToInt32(r["attempt_id"]),
                                    Title = r["quiz_title"]?.ToString() ?? "",
                                    Subject = r["subject"]?.ToString() ?? "",
                                    StudentName = r["student_name"]?.ToString() ?? "",
                                    Type = r["assessment_type"]?.ToString() ?? "quiz",
                                    PostedAt = posted
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine("LoadNotificationQuizAttempts error: " + ex.Message); }
            return list;
        }

        private List<SimpleProfNotification> LoadNotificationJoinedClasses()
        {
            var list = new List<SimpleProfNotification>();
            string connStr = SettingsManager.Current.GetConnectionString();
            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = @"SELECT sc.class_id, sc.class_name, sc.section, CONCAT(ui.lastname, ', ', ui.firstname) AS student_name FROM student_class sc LEFT JOIN user_information ui ON ui.user_id = sc.user_id WHERE sc.professor_id = @prof_id ORDER BY sc.class_id DESC LIMIT 20";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@prof_id", ProfessorID);
                        using (var r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                list.Add(new SimpleProfNotification
                                {
                                    Id = Convert.ToInt32(r["class_id"]),
                                    Title = "Class joined",
                                    Subject = r["class_name"]?.ToString() ?? "",
                                    Section = r["section"]?.ToString() ?? "",
                                    StudentName = r["student_name"]?.ToString() ?? "",
                                    Type = "join",
                                    PostedAt = null
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine("LoadNotificationJoinedClasses error: " + ex.Message); }
            return list;
        }

        private Panel BuildNotificationCard(ProfessorNotificationItem item)
        {
            var card = new Panel
            {
                Width = notificationList.ClientSize.Width - 28,
                Height = 74,
                Margin = new Padding(0, 0, 0, 8),
                BackColor = item.Unread ? Color.FromArgb(255, 248, 248) : Color.White,
                Cursor = Cursors.Hand,
                Tag = item.Key
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

                using (var border = new Pen(Color.FromArgb(235, 230, 230), 1))
                    e.Graphics.DrawRectangle(border, 0, 0, card.Width - 1, card.Height - 1);

                if (item.Unread)
                {
                    using (var dot = new SolidBrush(Color.FromArgb(220, 40, 40)))
                        e.Graphics.FillEllipse(dot, 8, 10, 8, 8);
                }
            };

            var icon = new Label
            {
                Text = item.Icon,
                Font = new Font("Segoe UI Emoji", 18F),
                ForeColor = Color.FromArgb(60, 60, 60),
                BackColor = Color.Transparent,
                AutoSize = false,
                Size = new Size(42, 42),
                Location = new Point(24, 14),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            card.Controls.Add(icon);

            var lblTitle = new Label
            {
                Text = item.Title,
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(35, 35, 35),
                BackColor = Color.Transparent,
                AutoSize = false,
                AutoEllipsis = true,
                Size = new Size(card.Width - 100, 20),
                Location = new Point(74, 12),
                Cursor = Cursors.Hand
            };
            card.Controls.Add(lblTitle);

            var lblSub = new Label
            {
                Text = item.Subtitle,
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.FromArgb(120, 120, 120),
                BackColor = Color.Transparent,
                AutoSize = false,
                AutoEllipsis = true,
                Size = new Size(card.Width - 100, 18),
                Location = new Point(74, 34),
                Cursor = Cursors.Hand
            };
            card.Controls.Add(lblSub);

            if (item.Time.HasValue)
            {
                var lblTime = new Label
                {
                    Text = GetRelativeTime(item.Time.Value),
                    Font = new Font("Segoe UI", 8F, FontStyle.Italic),
                    ForeColor = Color.FromArgb(160, 160, 160),
                    BackColor = Color.Transparent,
                    AutoSize = false,
                    Size = new Size(card.Width - 100, 16),
                    Location = new Point(74, 52),
                    Cursor = Cursors.Hand
                };
                card.Controls.Add(lblTime);
            }

            Action openItem = () =>
            {
                readNotificationKeys.Add(item.Key);
                CloseNotificationPanel();

                if (item.Type == "submission") { try { ActivityStatus(); ShowPage(pnlGrades, "Grades", btnGrades); } catch { } }
                else if (item.Type == "quiz") { try { var form = new ProfessorGradesForm(); form.ShowDialog(this); } catch { } }
                else if (item.Type == "join") { try { ShowPage(pnlStudent, "My Students", btnStudent); LoadAllStudent(); } catch { } }
            };

            card.Click += (s, e) => openItem();
            icon.Click += (s, e) => openItem();
            lblTitle.Click += (s, e) => openItem();
            lblSub.Click += (s, e) => openItem();

            foreach (Control c in card.Controls) c.Click += (s, e) => openItem();

            return card;
        }

        private string GetRelativeTime(DateTime dt)
        {
            var span = DateTime.Now - dt;
            if (span.TotalMinutes < 1) return "just now";
            if (span.TotalMinutes < 60) return (int)span.TotalMinutes + "m ago";
            if (span.TotalHours < 24) return (int)span.TotalHours + "h ago";
            if (span.TotalDays < 7) return (int)span.TotalDays + "d ago";
            return dt.ToString("MMM dd, yyyy");
        }
    }

    public class ClassCardPanel : Panel
    {
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color BannerColor { get; set; } = Color.SeaGreen;

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Title { get; set; } = "";

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Subtitle { get; set; } = "";

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Section { get; set; } = "";

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Day { get; set; } = "";

        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Time { get; set; } = "";

        private int _bannerHeight = 100;
        private const int CornerRadius = 16;
        private const int MinBannerHeight = 90;
        private const int MaxBannerHeight = 130;

        public ClassCardPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Color.White;
        }

        private GraphicsPath GetRoundedRect(RectangleF rect, int radius)
        {
            var path = new GraphicsPath();
            float d = radius * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            if (Width > 0 && Height > 0)
            {
                using (var path = GetRoundedRect(new RectangleF(0, 0, Width, Height), CornerRadius))
                {
                    Region?.Dispose();
                    Region = new Region(path);
                }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            float titleAreaWidth = Width - 16 - 30;
            Font titleFont = new Font("Segoe UI", 16F, FontStyle.Bold);
            SizeF measured = g.MeasureString(Title, titleFont, (int)titleAreaWidth);

            float size = 16F;
            while (measured.Height > 46 && size > 10F)
            {
                titleFont.Dispose();
                size -= 1F;
                titleFont = new Font("Segoe UI", size, FontStyle.Bold);
                measured = g.MeasureString(Title, titleFont, (int)titleAreaWidth);
            }

            int neededBannerHeight = (int)(14 + measured.Height + 6 + 18 + 14);
            _bannerHeight = Math.Max(MinBannerHeight, Math.Min(MaxBannerHeight, neededBannerHeight));

            var fullRect = new RectangleF(0, 0, Width - 1, Height - 1);
            using (var cardPath = GetRoundedRect(fullRect, CornerRadius))
            {
                using (var bg = new SolidBrush(Color.White)) g.FillPath(bg, cardPath);

                var bannerRect = new RectangleF(0, 0, Width, _bannerHeight);
                g.SetClip(cardPath);
                using (var gradBrush = new LinearGradientBrush(bannerRect, ControlPaint.Light(BannerColor, 0.15f), ControlPaint.Dark(BannerColor, 0.05f), LinearGradientMode.ForwardDiagonal)) g.FillRectangle(gradBrush, bannerRect);
                g.ResetClip();

                using (var pen = new Pen(Color.FromArgb(230, 230, 230), 1)) g.DrawPath(pen, cardPath);
            }

            var titleRect = new RectangleF(16, 14, titleAreaWidth, measured.Height + 2);
            using (var whiteBrush = new SolidBrush(Color.White)) g.DrawString(Title, titleFont, whiteBrush, titleRect);
            titleFont.Dispose();

            float subtitleY = 14 + measured.Height + 6;
            using (var subFont = new Font("Segoe UI", 10F, FontStyle.Regular))
            using (var whiteBrush = new SolidBrush(Color.White)) g.DrawString(Subtitle, subFont, whiteBrush, new PointF(16, subtitleY));

            using (var dotsFont = new Font("Segoe UI", 12F, FontStyle.Bold))
            using (var whiteBrush = new SolidBrush(Color.White))
            {
                var sz = g.MeasureString("⋮", dotsFont);
                g.DrawString("⋮", dotsFont, whiteBrush, new PointF(Width - sz.Width - 12, 10));
            }

            using (var profFont = new Font("Segoe UI", 9F, FontStyle.Regular))
            using (var grayBrush = new SolidBrush(Color.FromArgb(90, 90, 90))) g.DrawString($"{Day}   {Time}", profFont, grayBrush, new PointF(16, _bannerHeight + 14));

            using (var sectionFont = new Font("Segoe UI", 10F, FontStyle.Bold))
            using (var sectionBrush = new SolidBrush(Color.FromArgb(60, 60, 60)))
            {
                var sz = g.MeasureString(Section, sectionFont);
                g.DrawString(Section, sectionFont, sectionBrush, new PointF(Width - sz.Width - 16, Height - sz.Height - 12));
            }
        }
    }

    // =========================================================
    // LOADING OVERLAY
    // =========================================================
    public class LoadingOverlay : IDisposable
    {
        private readonly Form _parentForm;
        private LoadingCard _card;
        private LoadingInputBlocker _blocker;
        private bool _disposed = false;

        public LoadingOverlay(Form parentForm, string message)
        {
            _parentForm = parentForm;
            string msg = string.IsNullOrEmpty(message) ? "Loading" : message;
            if (_parentForm == null || _parentForm.IsDisposed) return;

            _card = new LoadingCard(msg);
            _parentForm.Controls.Add(_card);
            _card.BringToFront();
            CenterCard();
            _parentForm.Resize += ParentForm_Resize;
            _card.StartAnimation();

            _blocker = new LoadingInputBlocker(_parentForm);
            Application.AddMessageFilter(_blocker);
        }

        private void ParentForm_Resize(object sender, EventArgs e) { CenterCard(); }

        private void CenterCard()
        {
            if (_card == null || _card.IsDisposed || _parentForm == null || _parentForm.IsDisposed) return;
            Size area = _parentForm.ClientSize;
            _card.Location = new Point(Math.Max(0, (area.Width - _card.Width) / 2), Math.Max(0, (area.Height - _card.Height) / 2));
        }

        public void UpdateMessage(string message)
        {
            if (_card == null || _card.IsDisposed) return;
            if (_card.InvokeRequired) _card.BeginInvoke(new Action(() => _card.SetMessage(message)));
            else _card.SetMessage(message);
        }

        public void Dispose() { Dispose(true); GC.SuppressFinalize(this); }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            _disposed = true;
            if (!disposing || _card == null) return;

            Action cleanup = () =>
            {
                try
                {
                    if (_blocker != null) Application.RemoveMessageFilter(_blocker);
                    _blocker = null;
                    _parentForm.Resize -= ParentForm_Resize;
                    _card.StopAnimation();
                    if (!_parentForm.IsDisposed) _parentForm.Controls.Remove(_card);
                    _card.Dispose();
                }
                catch { }
                _card = null;
            };

            if (_parentForm != null && !_parentForm.IsDisposed && _parentForm.InvokeRequired) _parentForm.Invoke(cleanup);
            else cleanup();
        }
    }

    internal class LoadingInputBlocker : IMessageFilter
    {
        private readonly Form _form;
        public LoadingInputBlocker(Form form) { _form = form; }

        public bool PreFilterMessage(ref Message m)
        {
            int msg = m.Msg;
            bool isInput = (msg >= 0x0201 && msg <= 0x0209) || msg == 0x020A || (msg >= 0x0100 && msg <= 0x0109);
            if (!isInput) return false;

            try { Control c = Control.FromHandle(m.HWnd); if (c == null) return false; return c.FindForm() == _form; }
            catch { return false; }
        }
    }

    internal class LoadingCard : Control
    {
        private static readonly Color Accent = Color.FromArgb(128, 0, 0);
        private static readonly Color AccentSoft = Color.FromArgb(244, 232, 232);
        private static readonly Color TextMain = Color.FromArgb(45, 45, 55);
        private static readonly Color TextSub = Color.FromArgb(135, 135, 145);
        private static readonly Color BorderColor = Color.FromArgb(225, 220, 220);
        private const int SpinnerSize = 46;

        private readonly System.Windows.Forms.Timer _timer;
        private readonly Stopwatch _clock = new Stopwatch();
        private readonly Font _fontMain;
        private readonly Font _fontSub;
        private readonly Pen _trackPen;
        private readonly Pen _arcPen;
        private readonly StringFormat _centerFormat;
        private string _message;
        private int _lastDots = -1;
        private Rectangle _spinnerRect;

        public LoadingCard(string message)
        {
            _message = message;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Size = new Size(280, 150);
            BackColor = Color.White;

            _fontMain = new Font("Segoe UI Semibold", 11.5F, FontStyle.Bold);
            _fontSub = new Font("Segoe UI", 9F, FontStyle.Regular);

            _trackPen = new Pen(AccentSoft, 4.5F);
            _arcPen = new Pen(Accent, 4.5F);
            _arcPen.StartCap = LineCap.Round;
            _arcPen.EndCap = LineCap.Round;

            _centerFormat = new StringFormat { Alignment = StringAlignment.Center };
            _spinnerRect = new Rectangle((Width - SpinnerSize) / 2 + 3, 22 + 3, SpinnerSize - 6, SpinnerSize - 6);

            _timer = new System.Windows.Forms.Timer();
            _timer.Interval = 33;
            _timer.Tick += (s, e) => OnFrame();
        }

        public void StartAnimation() { _clock.Restart(); _timer.Start(); }
        public void StopAnimation() { _timer.Stop(); _clock.Stop(); }

        public void SetMessage(string message) { _message = string.IsNullOrEmpty(message) ? "Loading" : message; Invalidate(); }

        private void OnFrame()
        {
            int dots = (int)(_clock.ElapsedMilliseconds / 380) % 4;
            if (dots != _lastDots) { _lastDots = dots; Invalidate(); }
            else { Rectangle r = _spinnerRect; r.Inflate(6, 6); Invalidate(r); }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            using (GraphicsPath path = RoundedRect(new Rectangle(0, 0, Width, Height), 14)) Region = new Region(path);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(Color.White);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            double t = _clock.ElapsedMilliseconds;

            using (GraphicsPath bp = RoundedRect(new Rectangle(0, 0, Width - 1, Height - 1), 14))
            using (Pen border = new Pen(BorderColor, 1)) g.DrawPath(border, bp);

            g.DrawEllipse(_trackPen, _spinnerRect);

            float rotation = (float)((t / 1100.0 * 360.0) % 360.0);
            float sweep = 50f + 200f * (float)(0.5 - 0.5 * Math.Cos(2 * Math.PI * t / 1600.0));
            g.DrawArc(_arcPen, _spinnerRect, rotation - 90f, sweep);

            int dots = (int)(t / 380.0) % 4;
            string full = _message + "...";
            string shown = _message + new string('.', dots);

            SizeF fullSize = g.MeasureString(full, _fontMain, Width * 2, StringFormat.GenericTypographic);
            float x = (Width - fullSize.Width) / 2f;
            using (SolidBrush tb = new SolidBrush(TextMain)) g.DrawString(shown, _fontMain, tb, x, 84, StringFormat.GenericTypographic);

            using (SolidBrush sb = new SolidBrush(TextSub)) g.DrawString("Please wait a moment", _fontSub, sb, new RectangleF(0, 112, Width, 20), _centerFormat);
        }

        private static GraphicsPath RoundedRect(Rectangle rect, int radius)
        {
            int d = radius * 2;
            GraphicsPath path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timer.Dispose();
                _fontMain.Dispose();
                _fontSub.Dispose();
                _trackPen.Dispose();
                _arcPen.Dispose();
                _centerFormat.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}