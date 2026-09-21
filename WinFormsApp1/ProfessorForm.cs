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
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
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

        private readonly TimeSpan thumbnailInterval = TimeSpan.FromSeconds(5);
        private System.Windows.Forms.Timer broadcastTimer;

        private const int MAX_MINI_BUTTONS = 5;
        private int OnlineCount = 0;
        private int OfflineCount = 0;
        private int WorkStationNum = 0;
        private string selectedWorkstationId = "";
        private string SelectedIP = "";
        private bool isRunning = false;
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
        private string DatabaseIP = "localhost";

        int ProfessorID;
        string ProfessorUsername;

        public ProfessorForm(int UserId, string Username)
        {
            InitializeComponent();
            InitializeSaveDirectory();
            InitializeChangingPicture();
            ProfessorID = UserId;
            ProfessorUsername = Username;
        }

        private void ProfessorForm_Load(object sender, EventArgs e)
        {
            saveFolder = GetFolderPath(ProfessorID);
            isRunning = true;

            StartServer();
            StartBroadcastListener();
            StartScreenListener();

            LoadAllStudent();
            dgvAttendance();
            AutoCreateClassBtn();
            ActivitySectionSubject();
            RecentActivity();

            ActivityStatus();
            lblGradesSubmitted.Text = CountTotalSubmitted(ProfessorID).ToString();
            lblGradesGraded.Text = CountTotalGraded(ProfessorID).ToString();
            lblGradesNotSubmitted.Text = CountTotalNotSubmitted(ProfessorID).ToString();

            lblProfUsername.Text = ProfessorUsername;
            InitializeChangingPicture();

            StartActivityFileServer();
            NameGet();
        }

        private void btnHome_Click(object sender, EventArgs e)
        {
            pnlHome.BringToFront();
            lblPanelName.Text = "Home";
            InitializeChangingPicture();
        }
        private void btnWorkstation_Click(object sender, EventArgs e)
        {
            pnlWorkstation.BringToFront();
            lblPanelName.Text = "Workstations";
            InitializeChangingPicture();
        }
        private void btnStudent_Click(object sender, EventArgs e)
        {
            pnlStudent.BringToFront();
            lblPanelName.Text = "My Students";
            InitializeChangingPicture();
        }
        private void btnActivities_Click(object sender, EventArgs e)
        {
            pnlActivity.BringToFront();
            ActivitySectionSubject();
            RecentActivity();
            lblPanelName.Text = "Activities";
            InitializeChangingPicture();
        }
        private void btnGrades_Click(object sender, EventArgs e)
        {
            pnlGrades.BringToFront();
            lblPanelName.Text = "Grades";
            ActivityStatus();
            InitializeChangingPicture();
        }
        private void btnAttendance_Click(object sender, EventArgs e)
        {
            pnlAttendance.BringToFront();
            lblPanelName.Text = "Attendance";
            dgvAttendance();
            InitializeChangingPicture();
        }

        private void btnSubject_Click(object sender, EventArgs e)
        {
            pnlSubject.BringToFront();
            lblPanelName.Text = "Subjects";
            InitializeChangingPicture();
        }

        private void btnFile_Click(object sender, EventArgs e)
        {
            pnlFile.BringToFront();
            lblPanelName.Text = "Files";
            InitializeChangingPicture();
        }
        private void btnAccount_Click(object sender, EventArgs e)
        {
            pnlSetting.BringToFront();
            lblPanelName.Text = "Settings ";
            InitializeChangingPicture();
        }

        //Home Page//
        private void linkLblWorkstations_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            pnlWorkstation.BringToFront();
        }

        private void btnHomeCreateSubject_Click(object sender, EventArgs e)
        {
            pnlSubject.BringToFront();
        }

        private void btnHomeCreateActivity_Click(object sender, EventArgs e)
        {
            pnlActivity.BringToFront();
        }

        // =========================================================
        // LISTENERS - with SO_REUSEADDR
        // =========================================================

        private async void StartServer()
        {
            try
            {
                listener = new TcpListener(IPAddress.Any, SettingsManager.Current.WorkstationPort);
                listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                listener.Start();
                Console.WriteLine("[Professor] Workstation listener started on port " + SettingsManager.Current.WorkstationPort);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Professor] Workstation bind FAILED: " + ex.Message);
                return;
            }

            lblComputerOnline.Text = "0";
            lblComputerOffline.Text = "0";

            while (isRunning)
            {
                try
                {
                    TcpClient client = await listener.AcceptTcpClientAsync();
                    string clientIp = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
                    Console.WriteLine("[Professor] accepted workstation " + clientIp);

                    Button wsButton = null;

                    if (this.InvokeRequired)
                        this.Invoke(new Action(() => wsButton = OnWorkStationConnected(clientIp)));
                    else
                        wsButton = OnWorkStationConnected(clientIp);

                    _ = MonitorDisconnected(client, wsButton, clientIp);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[Professor] Workstation accept error: " + ex.Message);
                    break;
                }
            }
        }

        private Button OnWorkStationConnected(string clientIp)
        {
            if (workstationButtons.ContainsKey(clientIp))
            {
                Button existingBtn = workstationButtons[clientIp];
                existingBtn.BackColor = Color.LightGreen;
                if (miniWorkstationButtons.ContainsKey(clientIp))
                    miniWorkstationButtons[clientIp].BackColor = Color.LightGreen;

                UpdateConnectedCount();
                return existingBtn;
            }

            WorkStationNum++;

            Button MainPcButton = new Button();
            MainPcButton.Height = 180;
            MainPcButton.Width = 250;
            MainPcButton.Margin = new Padding(5);
            MainPcButton.BackColor = Color.LightGreen;
            MainPcButton.Tag = clientIp;
            MainPcButton.Click += (s, args) => { SelectedIP = clientIp; };

            flpMainWorkstations.Controls.Add(MainPcButton);
            workstationButtons[clientIp] = MainPcButton;

            Button miniButton = new Button();
            miniButton.Text = "PC " + WorkStationNum;
            miniButton.Height = 150;
            miniButton.Width = 80;
            miniButton.Margin = new Padding(3);
            miniButton.BackColor = Color.LightGreen;
            miniButton.Tag = clientIp;

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
                while (client.Connected)
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, 1);
                    if (bytesRead == 0) break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("MonitorDisconnected: " + clientIp + " — " + ex.Message);
            }
            finally
            {
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() =>
                    {
                        if (workstationButtons.ContainsKey(clientIp))
                        {
                            ApplyNoSignal(clientIp);
                            lastThumbnailUpdate.Remove(clientIp);
                        }

                        if (miniWorkstationButtons.ContainsKey(clientIp))
                            miniWorkstationButtons[clientIp].BackColor = Color.Red;

                        UpdateConnectedCount();
                    }));
                }
                else
                {
                    if (workstationButtons.ContainsKey(clientIp))
                    {
                        ApplyNoSignal(clientIp);
                        lastThumbnailUpdate.Remove(clientIp);
                    }

                    if (miniWorkstationButtons.ContainsKey(clientIp))
                        miniWorkstationButtons[clientIp].BackColor = Color.Red;

                    UpdateConnectedCount();
                }

                try { client.Close(); } catch { }
            }
        }

        private void UpdateConnectedCount()
        {
            int connectedCount = workstationButtons.Values
                .Count(btn => btn.BackColor == Color.LightGreen);
            int disconnectedCount = workstationButtons.Count - connectedCount;

            lblStudentOnline.Text = $"{connectedCount} Online";
            lblComputerOnline.Text = connectedCount.ToString();
            lblComputerOffline.Text = disconnectedCount.ToString();
        }

        //My Student Page//
        private void LoadAllStudent(string filter = "")
        {
            string connStr = SettingsManager.Current.GetConnectionString();

            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = "SELECT u.username, " +
                                   "i.lastname, i.firstname, i.middlename, " +
                                   "i.school_year, i.school_section, i.school_course " +
                                   "FROM user_credential u " +
                                   "LEFT JOIN user_information i ON u.user_id = i.user_id " +
                                   "WHERE u.roles = 'Student'";

                    if (!string.IsNullOrEmpty(filter))
                        query += " AND (u.user_id LIKE @f1 OR i.lastname LIKE @f2 OR i.firstname LIKE @f3)";
                    if (!string.IsNullOrEmpty(cmbSemester.Text) && cmbSemester.Text != "Select Semester")
                        query += " AND i.school_semester = @semester";
                    if (!string.IsNullOrEmpty(cmbSection.Text) && cmbSection.Text != "Select Section")
                        query += " AND i.school_section = @section";
                    if (!string.IsNullOrEmpty(cmbYear.Text) && cmbYear.Text != "Select Year")
                        query += " AND i.school_year = @year";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
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
        private void cmbSemester_SelectedIndexChanged(object sender, EventArgs e) => LoadAllStudent();
        private void cmbYear_SelectedIndexChanged(object sender, EventArgs e) => LoadAllStudent();
        private void cmbSection_SelectedIndexChanged(object sender, EventArgs e) => LoadAllStudent();

        private void btnCleanFilter_Click(object sender, EventArgs e)
        {
            cmbYear.SelectedIndex = -1;
            cmbSection.SelectedIndex = -1;
            cmbSemester.SelectedIndex = -1;
            txtBoxSearch.Text = "";
            LoadAllStudent();
        }

        // Attendance //
        private void dgvAttendance()
        {
            string connStr = SettingsManager.Current.GetConnectionString();

            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = @"SELECT 
                                uc.roles,
                                pa.student_name, 
                                pa.present, 
                                pa.absent, 
                                pa.late
                            FROM user_credential uc 
                            INNER JOIN professor_attendance pa ON uc.user_id = pa.student_id
                            WHERE uc.roles = 'Student'";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        ViewStudentAttendance.DataSource = dt;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("dgvAttendance error: " + ex.Message);
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
                    using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM professor_attendance", conn))
                        return Convert.ToInt32(cmd.ExecuteScalar());
                }
            }
            catch { return 0; }
        }

        private void btnAddAttendance_Click(object sender, EventArgs e)
        {
            List<(int StudentId, string StudentName)> students = GetAllStudents();

            FlowLayoutPanel column = new FlowLayoutPanel();
            column.FlowDirection = FlowDirection.TopDown;
            column.WrapContents = false;
            column.AutoSize = true;
            column.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            column.Width = 160;
            column.Margin = new Padding(5);

            Label DateToday = new Label();
            DateToday.Text = DateTime.Today.ToString("MMM-dd");
            DateToday.Font = new Font(DateToday.Font.FontFamily, 12);
            DateToday.Margin = new Padding(4);
            column.Controls.Add(DateToday);

            foreach (var student in students)
            {
                Guna.UI2.WinForms.Guna2ComboBox cmb = new Guna.UI2.WinForms.Guna2ComboBox();
                cmb.Width = 150;
                cmb.Margin = new Padding(5);
                cmb.Tag = student.StudentId;

                cmb.Items.Add("Present");
                cmb.Items.Add("Absent");
                cmb.Items.Add("Late");

                cmb.DropDownStyle = ComboBoxStyle.DropDownList;
                cmb.SelectedIndex = -1;

                column.Controls.Add(cmb);
            }

            flpAttendance.Controls.Add(column);
            flpAttendance.Controls.SetChildIndex(column, 0);

            if (flpAttendance.Controls.Count > 4)
            {
                Control oldest = flpAttendance.Controls[flpAttendance.Controls.Count - 1];
                flpAttendance.Controls.Remove(oldest);
                oldest.Dispose();
            }

            string AttendanceDateNow = DateTime.Today.ToString("MMMdd");
            string connStr = SettingsManager.Current.GetConnectionString();

            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string checkQuery = @"SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS 
                           WHERE TABLE_SCHEMA = 'cdsga_hub' 
                           AND TABLE_NAME = 'professor_attendance' 
                           AND COLUMN_NAME = @columnName";

                    bool columnExists = false;
                    using (var checkCmd = new MySqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@columnName", AttendanceDateNow);
                        columnExists = Convert.ToInt32(checkCmd.ExecuteScalar()) > 0;
                    }

                    if (!columnExists)
                    {
                        string AddColumnQuery = $"ALTER TABLE professor_attendance ADD `{AttendanceDateNow}` VARCHAR(20)";
                        using (var cmd = new MySqlCommand(AddColumnQuery, conn))
                            cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("btnAddAttendance_Click error: " + ex.Message);
            }
        }

        private List<(int StudentId, string StudentName)> GetAllStudents()
        {
            List<(int, string)> list = new List<(int, string)>();
            string connStr = SettingsManager.Current.GetConnectionString();

            using (var conn = new MySqlConnection(connStr))
            {
                conn.Open();
                using (var cmd = new MySqlCommand("SELECT student_id, student_name FROM professor_attendance", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        list.Add((reader.GetInt32("student_id"), reader.GetString("student_name")));
                }
            }
            return list;
        }

        private void btnAttendanceUpdate_Click(object sender, EventArgs e)
        {
            string DateToday = DateTime.Today.ToString("MMMdd");
            string connStr = SettingsManager.Current.GetConnectionString();

            if (flpAttendance.Controls.Count == 0)
            {
                MessageBox.Show("No attendance data to update. Please load attendance first.");
                return;
            }

            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    FlowLayoutPanel currentColumn = (FlowLayoutPanel)flpAttendance.Controls[0];
                    foreach (Control ctrl in currentColumn.Controls)
                    {
                        if (ctrl is Guna.UI2.WinForms.Guna2ComboBox cmb && cmb.Tag != null)
                        {
                            int studentId = (int)cmb.Tag;
                            string status = cmb.Text;

                            if (string.IsNullOrEmpty(status)) continue;

                            string col = status == "Present" ? "present" : status == "Absent" ? "absent" : "late";

                            string query = $"UPDATE professor_attendance SET `{DateToday}` = @status, {col} = COALESCE({col}, 0) + 1 WHERE student_id = @student_id ";
                            using (var cmd = new MySqlCommand(query, conn))
                            {
                                cmd.Parameters.AddWithValue("@status", status);
                                cmd.Parameters.AddWithValue("@student_id", studentId);
                                cmd.ExecuteNonQuery();
                            }
                        }
                    }
                    dgvAttendance();
                    MessageBox.Show("Update");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("btnAttendanceUpdate_Click error: " + ex.Message);
            }
        }

        private void btnExportAttendance_Click(object sender, EventArgs e)
        {
            string connStr = SettingsManager.Current.GetConnectionString();

            try
            {
                DataTable dt = new DataTable();

                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    using (var adapter = new MySqlDataAdapter("SELECT * FROM professor_attendance", conn))
                        adapter.Fill(dt);
                }

                if (dt.Rows.Count == 0)
                {
                    MessageBox.Show("No data to export.");
                    return;
                }

                using (SaveFileDialog sfd = new SaveFileDialog())
                {
                    sfd.Filter = "Excel Files|*.xlsx";
                    sfd.FileName = "UserData_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".xlsx";

                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        using (var workbook = new XLWorkbook())
                        {
                            var worksheet = workbook.Worksheets.Add(dt, "Users");
                            worksheet.Columns().AdjustToContents();
                            workbook.SaveAs(sfd.FileName);
                        }
                        MessageBox.Show("Exported successfully!");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("btnExportAttendance_Click error: " + ex.Message);
            }
        }

        //Class//
        private void btnShowPnlCreateClass_Click(object sender, EventArgs e)
        {
            pnlCreateClass.Visible = true;
            pnlCreateClass.BringToFront();
        }

        private void btnClosePanel_Click(object sender, EventArgs e)
        {
            pnlCreateClass.Visible = false;
            pnlCreateClass.SendToBack();
        }

        private void btnCreateClass_Click(object sender, EventArgs e)
        {
            string connStr = SettingsManager.Current.GetConnectionString();
            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = @"INSERT INTO professor_class 
                                    (professor_id, class_code, class_name, class_section, class_time, class_date) 
                                    VALUE (@professor_id, @class_code, @class_name, @class_section, @class_time, @class_date)";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@professor_id", ProfessorID);
                        cmd.Parameters.AddWithValue("@class_code", txtClassCode.Text.Trim());
                        cmd.Parameters.AddWithValue("@class_name", txtClassName.Text.Trim());
                        cmd.Parameters.AddWithValue("@class_section", txtClassSection.Text.Trim());
                        cmd.Parameters.AddWithValue("@class_time", txtClassTime.Text.Trim());
                        cmd.Parameters.AddWithValue("@class_date", cmbClassDate.Text.Trim());
                        cmd.ExecuteNonQuery();
                    }

                    string folderName = txtClassSection.Text.Trim();
                    AutoCreateClassBtn();
                    CreateFolderForSection(folderName);
                    MessageBox.Show("Created Succesfuly");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("btnCreateClass_Click error: " + ex.Message);
            }
        }

        private void CreateFolderForSection(string folderName)
        {
            string newFolderPath = Path.Combine(saveFolder, folderName);
            if (!Directory.Exists(newFolderPath))
            {
                Directory.CreateDirectory(newFolderPath);
                LoadServerFolder(saveFolder);
            }
            else
            {
                MessageBox.Show("Folder already exists.");
            }
        }

        private void AutoCreateClassBtn()
        {
            flpSubjectClass.Controls.Clear();
            string connStr = SettingsManager.Current.GetConnectionString();
            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = @"SELECT class_id, class_name, class_date, class_time, class_section 
                             FROM professor_class WHERE professor_id = @professor_id";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@professor_id", ProfessorID);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Panel cardPanel = new Panel
                                {
                                    Size = new Size(350, 250),
                                    BackColor = Color.White,
                                    BorderStyle = BorderStyle.FixedSingle,
                                    Margin = new Padding(10),
                                    Tag = reader["class_id"].ToString()
                                };

                                Label lblMenu = new Label
                                {
                                    Text = "•••",
                                    Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                                    Location = new Point(300, 10),
                                    AutoSize = true,
                                    Cursor = Cursors.Hand
                                };
                                cardPanel.Controls.Add(lblMenu);

                                Label lblTitle = new Label
                                {
                                    Text = reader["class_name"].ToString(),
                                    Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                                    Location = new Point(20, 50),
                                    AutoSize = true
                                };
                                cardPanel.Controls.Add(lblTitle);

                                Label lblProfName = new Label
                                {
                                    Text = "Prof. " + ProfessorID,
                                    Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                                    Location = new Point(20, 100),
                                    AutoSize = true
                                };
                                cardPanel.Controls.Add(lblProfName);

                                Label lblDay = new Label
                                {
                                    Text = reader["class_date"].ToString(),
                                    Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                                    Location = new Point(50, 150),
                                    AutoSize = true
                                };
                                cardPanel.Controls.Add(lblDay);

                                Label lblTime = new Label
                                {
                                    Text = reader["class_time"].ToString(),
                                    Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                                    Location = new Point(50, 180),
                                    AutoSize = true
                                };
                                cardPanel.Controls.Add(lblTime);

                                Label lblSection = new Label
                                {
                                    Text = reader["class_section"].ToString(),
                                    Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                                    Location = new Point(200, 210),
                                    AutoSize = true,
                                    TextAlign = ContentAlignment.MiddleRight
                                };
                                cardPanel.Controls.Add(lblSection);

                                ContextMenuStrip rightClickMenu = new ContextMenuStrip();
                                ToolStripMenuItem deleteMenuItem = new ToolStripMenuItem("Delete Class");
                                deleteMenuItem.Click += DeleteClass_Click;
                                rightClickMenu.Items.Add(deleteMenuItem);
                                cardPanel.ContextMenuStrip = rightClickMenu;

                                flpSubjectClass.Controls.Add(cardPanel);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("AutoCreateClassBtn error: " + ex.Message);
            }
        }

        private void DeleteClass_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem menuItem = sender as ToolStripMenuItem;
            if (menuItem == null) return;

            ContextMenuStrip menu = menuItem.Owner as ContextMenuStrip;
            if (menu == null) return;

            Panel clickedCard = menu.SourceControl as Panel;
            if (clickedCard == null) return;

            string classId = clickedCard.Tag.ToString();

            DialogResult result = MessageBox.Show("Are you sure you want to delete this class?",
                                                  "Confirm Delete",
                                                  MessageBoxButtons.YesNo,
                                                  MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                string connStr = SettingsManager.Current.GetConnectionString();
                try
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

                    flpSubjectClass.Controls.Remove(clickedCard);
                    clickedCard.Dispose();
                    menu.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("DeleteClass_Click error: " + ex.Message);
                }
            }
        }

        //Activity//
        private void btnActivityUploadFile_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    selectedFilePath = ofd.FileName;
                    btnActivityUploadFile.Text = Path.GetFileName(selectedFilePath);
                }
            }
        }

        private void btnPostActivity_Click(object sender, EventArgs e)
        {
            string connStr = SettingsManager.Current.GetConnectionString();

            DateTime now = DateTime.Now;
            string FullDateTime = now.ToString("MMM-dd HH:mm:ss");

            byte[] pdfBytes = null;
            string pdfName = null;

            if (!string.IsNullOrEmpty(selectedFilePath) && File.Exists(selectedFilePath))
            {
                try
                {
                    pdfBytes = File.ReadAllBytes(selectedFilePath);
                    pdfName = Path.GetFileName(selectedFilePath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Could not read the selected file: " + ex.Message);
                    return;
                }
            }

            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = @"INSERT INTO professor_activity 
                            (professor_id, title, description, section, activity_subject, 
                             start_time, due_date, activity_status, score, 
                             activity_file, activity_filename) 
                            VALUES (@professor_id, @title, @description, @section, @activity_subject, 
                                    @start_time, @due_date, @activity_status, @score, 
                                    @activity_file, @activity_filename)";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@professor_id", ProfessorID);
                        cmd.Parameters.AddWithValue("@title", cmbActivityTitle.Text.Trim());
                        cmd.Parameters.AddWithValue("@description", txtActivityPostDetails.Text.Trim());
                        cmd.Parameters.AddWithValue("@section", cmbActivitySection.Text.Trim());
                        cmd.Parameters.AddWithValue("@activity_subject", cmbActivitySubject.Text.Trim());
                        cmd.Parameters.AddWithValue("@start_time", FullDateTime);
                        cmd.Parameters.AddWithValue("@due_date", dtpActivityDeadline.Value);
                        cmd.Parameters.AddWithValue("@activity_status", "Pending");
                        cmd.Parameters.AddWithValue("@score", txtActivityScore.Text.Trim());
                        cmd.Parameters.AddWithValue("@activity_file", (object)pdfBytes ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@activity_filename", (object)pdfName ?? DBNull.Value);
                        cmd.ExecuteNonQuery();
                    }

                    MessageBox.Show("Activity Posted Successfully");

                    selectedFilePath = "";
                    btnActivityUploadFile.Text = "Upload File";
                    RecentActivity();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("btnPostActivity_Click error: " + ex.Message);
            }
        }

        private void ActivitySectionSubject()
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
                                cmbActivitySection.Items.Add(reader.GetString("class_section"));
                                cmbActivitySubject.Items.Add(reader.GetString("class_name"));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ActivitySectionSubject error: " + ex.Message);
            }
        }

        private void RecentActivity()
        {
            string connStr = SettingsManager.Current.GetConnectionString();
            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = @"SELECT title, section, activity_subject, due_date FROM professor_activity 
                                     WHERE professor_id = @professor_id 
                                     ORDER BY start_time DESC 
                                     LIMIT 5";
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
            catch (Exception ex)
            {
                Console.WriteLine("RecentActivity error: " + ex.Message);
            }
        }

        //FILE MANAGEMENT//
        private void lsServerFolderSetup()
        {
            FolderListView.View = View.LargeIcon;
            FolderListView.LargeImageList = imageList1;
            FolderListView.MultiSelect = false;
        }

        private void LoadServerFolder(string path, bool addToHistory = true)
        {
            if (addToHistory && !string.IsNullOrEmpty(currentFolder))
                folderHistory.Push(currentFolder);

            currentFolder = path;
            FolderListView.Items.Clear();
            imageList1.Images.Clear();
            int imageIndex = 0;

            foreach (string dir in Directory.GetDirectories(path))
            {
                imageList1.Images.Add(Properties.Resources.Folder);
                ListViewItem item = new ListViewItem(Path.GetFileName(dir), imageIndex);
                item.Tag = dir;
                FolderListView.Items.Add(item);
                imageIndex++;
            }

            foreach (string file in Directory.GetFiles(path))
            {
                Icon fileIcon = Icon.ExtractAssociatedIcon(file);
                imageList1.Images.Add(Properties.Resources.Item);

                ListViewItem item = new ListViewItem(Path.GetFileName(file), imageIndex);
                item.Tag = file;
                FolderListView.Items.Add(item);
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
            if (FolderListView.SelectedItems.Count == 0) return;

            string path = FolderListView.SelectedItems[0].Tag.ToString();

            if (Directory.Exists(path))
                LoadServerFolder(path);
            else if (File.Exists(path))
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
        }

        private void BtnBack_Click(object sender, EventArgs e) => btnBack();
        private void FolderListView_DoubleClick(object sender, EventArgs e) => doubleClick();

        private void btnAddFolder_Click(object sender, EventArgs e)
        {
            Guna.UI2.WinForms.Guna2TextBox txtFolderName = new Guna.UI2.WinForms.Guna2TextBox();
            txtFolderName.Width = 150;
            txtFolderName.Height = 20;
            txtFolderName.Margin = new Padding(5);
            txtFolderName.Location = new Point(230, 70);

            Guna.UI2.WinForms.Guna2CircleButton enterFolderName = new Guna.UI2.WinForms.Guna2CircleButton();
            enterFolderName.Width = 20;
            enterFolderName.Height = 20;
            enterFolderName.Text = "✔";
            enterFolderName.Margin = new Padding(5);
            enterFolderName.Location = new Point(230, 150);

            enterFolderName.Click += (s, args) =>
            {
                string folderName = txtFolderName.Text.Trim();

                if (string.IsNullOrEmpty(folderName))
                {
                    MessageBox.Show("Please enter a folder name.");
                    return;
                }

                NewCreateFolder(folderName);

                pnlFile.Controls.Remove(txtFolderName);
                pnlFile.Controls.Remove(enterFolderName);
            };

            pnlFile.Controls.Add(enterFolderName);
            pnlFile.Controls.Add(txtFolderName);
        }

        private void NewCreateFolder(string FolderName)
        {
            string newFolderPath = Path.Combine(saveFolder, FolderName);

            if (!Directory.Exists(newFolderPath))
            {
                Directory.CreateDirectory(newFolderPath);
                MessageBox.Show("Folder created!");
                LoadServerFolder(saveFolder);
            }
            else
            {
                MessageBox.Show("Folder already exists.");
            }
        }

        private static string GetFolderPath(int ProfessorID)
        {
            string connStr = SettingsManager.Current.GetConnectionString();
            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand("SELECT FolderPath FROM mainfolderpath WHERE user_id = @user_id", conn))
                    {
                        cmd.Parameters.AddWithValue("@user_id", ProfessorID);
                        object result = cmd.ExecuteScalar();

                        if (result != null && result != DBNull.Value)
                            return result.ToString();
                        return "Null";
                    }
                }
            }
            catch { return "Null"; }
        }

        //Panel Grades//
        private void ActivityStatus(string filter = "")
        {
            string connStr = SettingsManager.Current.GetConnectionString();

            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = @"SELECT user_id, title, section, student_name, class_name, activity_status, score, file_path FROM submitted_activity WHERE prof_id = @prof_id";

                    if (!string.IsNullOrEmpty(filter))
                        query += " AND student_name LIKE @f1";
                    if (!string.IsNullOrEmpty(cmbActivityGrades.Text))
                        query += " AND title = @title";
                    if (!string.IsNullOrEmpty(cmbSectionGrades.Text))
                        query += " AND section = @section";
                    if (!string.IsNullOrEmpty(cmbSubjectGrades.Text))
                        query += " AND class_name = @class_name";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@prof_id", ProfessorID);
                        if (!string.IsNullOrEmpty(cmbActivityGrades.Text))
                            cmd.Parameters.AddWithValue("@title", cmbActivityGrades.Text);
                        if (!string.IsNullOrEmpty(cmbSectionGrades.Text))
                            cmd.Parameters.AddWithValue("@section", cmbSectionGrades.Text);
                        if (!string.IsNullOrEmpty(cmbSubjectGrades.Text))
                            cmd.Parameters.AddWithValue("@class_name", cmbSubjectGrades.Text);

                        if (!string.IsNullOrEmpty(filter))
                        {
                            string f = "%" + filter + "%";
                            cmd.Parameters.AddWithValue("@f1", f);
                        }

                        MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        dgvStudentActivitySubmitted.DataSource = dt;
                    }
                    GetSection();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ActivityStatus error: " + ex.Message);
            }
        }

        private void txtSearchGrades_TextChanged(object sender, EventArgs e) => ActivityStatus(txtSearchGrades.Text.Trim());

        private void cmbActivityGrades_SelectedIndexChanged(object sender, EventArgs e)
        {
            lblGradesActivity.Text = "";
            ActivityStatus();
        }

        private void cmbSectionGrades_SelectedIndexChanged(object sender, EventArgs e)
        {
            lblGradesSection.Text = "";
            ActivityStatus();
        }

        private void cmbSubjectGrades_SelectedIndexChanged(object sender, EventArgs e)
        {
            lblGradesSubject.Text = "";
            ActivityStatus();
        }

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
            Guna.UI2.WinForms.Guna2Panel panel = new Guna.UI2.WinForms.Guna2Panel();
            panel.Width = 1000;
            panel.Height = 950;
            panel.Margin = new Padding(5);
            panel.Location = new Point(150, 0);
            panel.BorderRadius = 10;
            panel.FillColor = Color.LightGray;

            Label lblTitle = new Label();
            lblTitle.Text = Title;
            lblTitle.Location = new Point(20, 50);
            lblTitle.Size = new Size(200, 25);
            lblTitle.Font = new Font("Arial", 12, FontStyle.Bold);
            panel.Controls.Add(lblTitle);

            Label lblName = new Label();
            lblName.Text = "👤 " + Name;
            lblName.Location = new Point(20, 80);
            lblName.Size = new Size(200, 25);
            lblName.Font = new Font("Arial", 12, FontStyle.Bold);
            panel.Controls.Add(lblName);

            Label lblSection = new Label();
            lblSection.Text = "📝 " + Section;
            lblSection.Location = new Point(20, 110);
            lblSection.Size = new Size(200, 25);
            lblSection.Font = new Font("Arial", 12, FontStyle.Bold);
            panel.Controls.Add(lblSection);

            Label lblClassNameGrades = new Label();
            lblClassNameGrades.Text = "📝 " + ClassName;
            lblClassNameGrades.Location = new Point(20, 140);
            lblClassNameGrades.Size = new Size(200, 25);
            lblClassNameGrades.Font = new Font("Arial", 12, FontStyle.Bold);
            panel.Controls.Add(lblClassNameGrades);

            Label lblStatus = new Label();
            lblStatus.Text = "👤 " + Status;
            lblStatus.Location = new Point(20, 170);
            lblStatus.Size = new Size(350, 25);
            lblStatus.Font = new Font("Arial", 12, FontStyle.Bold);
            panel.Controls.Add(lblStatus);

            Guna.UI2.WinForms.Guna2Panel pdfContainer = new Guna.UI2.WinForms.Guna2Panel();
            pdfContainer.Location = new Point(20, 200);
            pdfContainer.Size = new Size(960, 700);
            pdfContainer.BorderRadius = 5;
            pdfContainer.BorderColor = Color.Gray;
            pdfContainer.BorderThickness = 1;
            pdfContainer.FillColor = Color.White;
            panel.Controls.Add(pdfContainer);

            PdfViewer pdfViewer = new PdfViewer();
            pdfViewer.Dock = DockStyle.Fill;
            pdfContainer.Controls.Add(pdfViewer);

            try
            {
                string file_path = @$"{filePath}";
                file_path = file_path.Trim();
                if (File.Exists(file_path))
                {
                    pdfViewer.LoadDocument(file_path);
                }
                else
                {
                    Label lblNoFile = new Label();
                    lblNoFile.Text = "PDF file not found";
                    lblNoFile.Location = new Point(200, 130);
                    lblNoFile.Size = new Size(200, 25);
                    lblNoFile.ForeColor = Color.Red;
                    pdfContainer.Controls.Add(lblNoFile);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("CreatePanelForSubmittedFiles PDF error: " + ex.Message);
            }

            Guna.UI2.WinForms.Guna2CircleButton btnDispose = new Guna.UI2.WinForms.Guna2CircleButton();
            btnDispose.Width = 50;
            btnDispose.Height = 50;
            btnDispose.Margin = new Padding(5);
            btnDispose.Image = Properties.Resources.Exit;
            btnDispose.FillColor = Color.Transparent;
            btnDispose.Location = new Point(930, 1);
            btnDispose.Click += (s, args) =>
            {
                pnlGrades.Controls.Remove(panel);
                panel.Dispose();
            };

            Guna.UI2.WinForms.Guna2TextBox txtScore = new Guna.UI2.WinForms.Guna2TextBox();
            txtScore.Width = 50;
            txtScore.Height = 30;
            txtScore.Location = new Point(800, 160);
            panel.Controls.Add(txtScore);

            Guna.UI2.WinForms.Guna2CircleButton btnUpdateScore = new Guna.UI2.WinForms.Guna2CircleButton();
            btnUpdateScore.Width = 30;
            btnUpdateScore.Height = 30;
            btnUpdateScore.Text = "✔";
            btnUpdateScore.Font = new Font("Arial", 12, FontStyle.Bold);
            btnUpdateScore.Margin = new Padding(5);
            btnUpdateScore.FillColor = Color.Transparent;
            btnUpdateScore.Location = new Point(870, 160);
            btnUpdateScore.Click += (s, args) =>
            {
                string connStr = SettingsManager.Current.GetConnectionString();

                if (string.IsNullOrEmpty(txtScore.Text))
                {
                    MessageBox.Show("Please enter a score.");
                    return;
                }

                try
                {
                    using (var conn = new MySqlConnection(connStr))
                    {
                        conn.Open();
                        string query = @"UPDATE submitted_activity SET score = @score WHERE user_id = @user_id";
                        using (var cmd = new MySqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@score", txtScore.Text.Trim());
                            cmd.Parameters.AddWithValue("@user_id", User_id);

                            int rowsAffected = cmd.ExecuteNonQuery();
                            if (rowsAffected > 0)
                            {
                                MessageBox.Show("Score updated successfully.");
                                ActivityStatus();
                            }
                            else
                            {
                                MessageBox.Show("Failed to update score. Please check the details.");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Update score error: " + ex.Message);
                }
            };

            panel.Controls.Add(btnDispose);
            panel.Controls.Add(btnUpdateScore);

            pnlGrades.Controls.Add(panel);
            panel.BringToFront();
        }

        private void dgvStudentActivitySubmitted_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            DataGridView.HitTestInfo hit = dgvStudentActivitySubmitted.HitTest(e.X, e.Y);

            if (hit.RowIndex >= 0)
            {
                DataGridViewRow selectedRow = dgvStudentActivitySubmitted.Rows[hit.RowIndex];

                string user_id = $" {selectedRow.Cells["user_id"].Value}";
                string Title = $" {selectedRow.Cells["title"].Value}";
                string Name = $" {selectedRow.Cells["student_name"].Value}";
                string SectionGrades = $" {selectedRow.Cells["section"].Value}";
                string ClassNameGrades = $" {selectedRow.Cells["class_name"].Value}";
                string StatusGrades = $" {selectedRow.Cells["activity_status"].Value}";
                int User_id = int.Parse(user_id);
                string filePath = $" {selectedRow.Cells["file_path"].Value}";
                CreatePanelForSubmittedFiles(User_id, Title, Name, SectionGrades, ClassNameGrades, StatusGrades, filePath);
            }
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

                                if (!cmbSubjectGrades.Items.Contains(className))
                                    cmbSubjectGrades.Items.Add(className);

                                if (!cmbSectionGrades.Items.Contains(classSection))
                                    cmbSectionGrades.Items.Add(classSection);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetSection error: " + ex.Message);
            }
        }

        private void btnWorkStationMonitoring_Click(object sender, EventArgs e)
        {
            pnlWorkStationMonitoring.BringToFront();
        }

        //Broadcast listener//
        private async void StartBroadcastListener()
        {
            try
            {
                broadcastListener = new TcpListener(IPAddress.Any, SettingsManager.Current.BroadcastPort);
                broadcastListener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                broadcastListener.Start();
                Console.WriteLine("[Professor] Broadcast listener started on port " + SettingsManager.Current.BroadcastPort);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Professor] Broadcast bind FAILED: " + ex.Message);
                return;
            }

            while (isRunning)
            {
                try
                {
                    TcpClient client = await broadcastListener.AcceptTcpClientAsync();
                    string clientIp = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
                    Console.WriteLine("[Professor] broadcast client connected " + clientIp);
                    broadcastClients[clientIp] = client;
                }
                catch
                {
                    break;
                }
            }
        }

        private void btnShareScreen_Click(object sender, EventArgs e)
        {
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
                            broadcastClients.Remove(kvp.Key);
                        }
                    }
                }

                screenshot.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Broadcast error: " + ex.Message);
            }
        }

        private Bitmap CaptureScreen()
        {
            Rectangle bounds = Screen.PrimaryScreen.Bounds;
            Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height);

            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);
            }

            return bitmap;
        }

        private void btnStopSharing_Click(object sender, EventArgs e)
        {
            broadcastTimer?.Stop();
        }

        //Shutdown / Restart//
        private void ShutdownStartListener(string clientIp)
        {
            try
            {
                TcpClient client = new TcpClient(clientIp, SettingsManager.Current.CommandPort);
                NetworkStream stream = client.GetStream();
                byte[] data = Encoding.UTF8.GetBytes("SHUTDOWN");
                stream.Write(data, 0, data.Length);
                client.Close();
                MessageBox.Show("Shutdown command sent!");
            }
            catch
            {
                MessageBox.Show("Error: Client not reachable");
            }
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
                MessageBox.Show("Restart command sent!");
            }
            catch
            {
                MessageBox.Show("Error: Client not reachable");
            }
        }

        private void btnReboot_Click(object sender, EventArgs e) => RestartStartListener(SelectedIP);

        //Screen listener//
        private async void StartScreenListener()
        {
            try
            {
                screenListener = new TcpListener(IPAddress.Any, SettingsManager.Current.ScreenSharePort);
                screenListener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                screenListener.Start();
                Console.WriteLine("[Professor] Screen listener started on port " + SettingsManager.Current.ScreenSharePort);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Professor] Screen bind FAILED: " + ex.Message);
                return;
            }

            while (isRunning)
            {
                try
                {
                    TcpClient client = await screenListener.AcceptTcpClientAsync();
                    _ = ReceiveScreenStream(client);
                }
                catch
                {
                    break;
                }
            }
        }

        private async Task ReceiveScreenStream(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            string clientIp = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();

            try
            {
                while (client.Connected)
                {
                    byte[] lengthBuffer = new byte[4];
                    int read = await ReadExactAsync(stream, lengthBuffer, 4);
                    if (read == 0) break;

                    int imageLength = BitConverter.ToInt32(lengthBuffer, 0);
                    byte[] imageBuffer = new byte[imageLength];

                    int totalRead = await ReadExactAsync(stream, imageBuffer, imageLength);
                    if (totalRead == 0) break;

                    using (MemoryStream ms = new MemoryStream(imageBuffer))
                    {
                        Image frame = Image.FromStream(ms);

                        if (this.InvokeRequired)
                            this.Invoke(new Action(() => UpdateScreenViewer(clientIp, frame)));
                        else
                            UpdateScreenViewer(clientIp, frame);
                    }
                }
            }
            catch { }
            finally
            {
                client.Close();
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
            bool viewerIsOpen = screenViewers.ContainsKey(clientIp) && screenViewers[clientIp] != null;

            if (viewerIsOpen)
            {
                PictureBox pb = screenViewers[clientIp];
                Image oldImage = pb.Image;
                pb.Image = (Image)frame.Clone();
                oldImage?.Dispose();
            }

            bool shouldUpdateThumbnail = !lastThumbnailUpdate.ContainsKey(clientIp)
                || (DateTime.Now - lastThumbnailUpdate[clientIp]) >= thumbnailInterval;

            if (shouldUpdateThumbnail && workstationButtons.ContainsKey(clientIp))
            {
                Button btn = workstationButtons[clientIp];

                Image thumbnail = ResizeImage(frame, btn.Width - 10, btn.Height - 30);
                Image oldThumb = btn.BackgroundImage;

                btn.BackgroundImage = thumbnail;
                btn.BackgroundImageLayout = ImageLayout.Zoom;

                oldThumb?.Dispose();
                lastThumbnailUpdate[clientIp] = DateTime.Now;
            }

            if (!viewerIsOpen)
                frame.Dispose();
        }

        private Image ResizeImage(Image original, int width, int height)
        {
            if (width <= 0 || height <= 0) return original;

            Bitmap resized = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(resized))
                g.DrawImage(original, 0, 0, width, height);
            return resized;
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
                    for (int i = -height; i < width; i += 20)
                        g.DrawLine(stripePen, i, height, i + height, 0);
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
            screenViewers[workstationId] = viewer.GetPictureBox();
            viewer.FormClosed += (s, args) => screenViewers.Remove(workstationId);
            viewer.Show();
        }

        private void btnRemoteView_Click(object sender, EventArgs e) => AddScreenViewer(SelectedIP);

        //Settings//
        private void btnSettingProfileExpand_Click(object sender, EventArgs e)
        {
            if (pnlSettingProfile.Height <= 350)
                pnlSettingProfile.Height = 733;
            else if (pnlSettingProfile.Height >= 733)
                pnlSettingProfile.Height = 350;
        }

        private void ClearAllFormData()
        {
            foreach (Control ctrl in this.Controls)
            {
                if (ctrl is TextBox) ((TextBox)ctrl).Text = "";
                else if (ctrl is ComboBox) ((ComboBox)ctrl).SelectedIndex = -1;
                else if (ctrl is DataGridView) ((DataGridView)ctrl).DataSource = null;
                else if (ctrl is ListBox) ((ListBox)ctrl).Items.Clear();
            }
        }

        // =========================================================
        // StopServer - fully releases the ports
        // =========================================================
        private void StopServer()
        {
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

            foreach (var kvp in broadcastClients)
            {
                try { kvp.Value.Close(); } catch { }
                try { kvp.Value.Dispose(); } catch { }
            }
            broadcastClients.Clear();

            // Give the OS a moment to free the ports
            System.Threading.Thread.Sleep(300);
        }

        private void Logout()
        {
            DialogResult result = MessageBox.Show("Are you sure you want to logout?",
                "Logout Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                StopServer();

                ClearAllFormData();

                GC.Collect();
                GC.WaitForPendingFinalizers();

                // ✅ Show Login BEFORE closing this form so the app doesn't exit
                Login login = new Login();
                login.Show();

                this.Hide();
                this.Close();
            }
        }

        private void btnSignOut_Click(object sender, EventArgs e) => Logout();
        private void btnSignOut2_Click(object sender, EventArgs e) => Logout();

        private void btnSettingChangeUsername_Click(object sender, EventArgs e)
        {
            pnlChangeUsername.Visible = true;
            pnlChangePassword.Visible = false;
            pnlChangePhoto.Visible = false;
        }

        private void btnExitChangeUsernamePanel_Click(object sender, EventArgs e) => pnlChangeUsername.Visible = false;

        private void btnSubmitChangeUsername_Click(object sender, EventArgs e)
        {
            string connStr = SettingsManager.Current.GetConnectionString();

            if (string.IsNullOrEmpty(txtCurrentUsername.Text) || string.IsNullOrEmpty(txtNewUsername.Text))
            {
                MessageBox.Show("Please enter both the current and new usernames.");
                return;
            }

            if (txtCurrentUsername.Text != ProfessorUsername)
            {
                MessageBox.Show("Please enter the Correct usernames.");
                return;
            }

            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = @"UPDATE user_credential SET username = @new_username WHERE username = @current_username";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@new_username", txtNewUsername.Text.Trim());
                        cmd.Parameters.AddWithValue("@current_username", txtCurrentUsername.Text.Trim());
                        cmd.ExecuteNonQuery();
                    }
                    ClearTextSettings();
                    MessageBox.Show("Username updated successfully.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("btnSubmitChangeUsername_Click error: " + ex.Message);
            }
        }

        private void btnSettingChangePassword_Click(object sender, EventArgs e)
        {
            pnlChangePassword.Visible = true;
            pnlChangeUsername.Visible = false;
            pnlChangePhoto.Visible = false;
        }

        private void btnExitChangePasswordPanel_Click(object sender, EventArgs e) => pnlChangePassword.Visible = false;

        private void btnSubmitChangePassword_Click(object sender, EventArgs e)
        {
            string connStr = SettingsManager.Current.GetConnectionString();

            if (string.IsNullOrEmpty(txtCurrentPassword.Text) || string.IsNullOrEmpty(txtNewPassword.Text) || string.IsNullOrEmpty(txtConfirmPassword.Text))
            {
                MessageBox.Show("Please fill in all fields.");
                return;
            }
            if (txtNewPassword.Text != txtConfirmPassword.Text)
            {
                MessageBox.Show("New password and confirm password do not match.");
                return;
            }

            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = @"UPDATE user_credential SET p_word = @new_password WHERE username = @current_username";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@new_password", txtNewPassword.Text.Trim());
                        cmd.Parameters.AddWithValue("@current_username", ProfessorUsername);
                        cmd.ExecuteNonQuery();
                    }
                    ClearTextSettings();
                    MessageBox.Show("Password updated successfully.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("btnSubmitChangePassword_Click error: " + ex.Message);
            }
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

            if (!Directory.Exists(SaveCurrentProfilePath))
                Directory.CreateDirectory(SaveCurrentProfilePath);
        }

        private void btnUploadPhoto_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    CurrentProfilePath = ofd.FileName;
                    picboxNewPicture.Image = Image.FromFile(CurrentProfilePath);
                    picboxNewPicture.SizeMode = PictureBoxSizeMode.Zoom;
                    btnSubmitChangePhoto.Enabled = true;
                }
            }
        }

        private void btnSubmitChangePhoto_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(CurrentProfilePath))
            {
                MessageBox.Show("Upload an image first!");
                return;
            }

            string connStr = SettingsManager.Current.GetConnectionString();

            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = @"UPDATE user_credential SET profile_picture = @profile_picture WHERE username = @username";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        string fileName = Path.GetFileName(CurrentProfilePath);
                        string destinationPath = Path.Combine(SaveCurrentProfilePath, fileName);
                        File.Copy(CurrentProfilePath, destinationPath, true);
                        cmd.Parameters.AddWithValue("@profile_picture", destinationPath);
                        cmd.Parameters.AddWithValue("@username", ProfessorUsername);
                        cmd.ExecuteNonQuery();
                    }
                    InitializeChangingPicture();
                    MessageBox.Show("Profile picture updated successfully.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("btnSubmitChangePhoto_Click error: " + ex.Message);
            }
        }

        private void InitializeChangingPicture()
        {
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
                                if (File.Exists(path))
                                {
                                    picboxSettingProfilePicture.Image = Image.FromFile(path);
                                    picboxSettingProfilePicture.SizeMode = PictureBoxSizeMode.Zoom;
                                    btnAccount.Image = Image.FromFile(path);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("InitializeChangingPicture error: " + ex.Message);
            }
        }

        private void btnSettingChangePhoto_Click(object sender, EventArgs e)
        {
            pnlChangePhoto.Visible = true;
            pnlChangeUsername.Visible = false;
            pnlChangePassword.Visible = false;
        }

        private void btnExitChangePhotoPanel_Click(object sender, EventArgs e) => pnlChangePhoto.Visible = false;

        //FileReceiver//
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
            catch (Exception ex)
            {
                Console.WriteLine("NameGet error: " + ex.Message);
            }
        }

        //File transfer listener//
        private async void StartActivityFileServer()
        {
            try
            {
                activityFileListener = new TcpListener(IPAddress.Any, SettingsManager.Current.FileTransferPort);
                activityFileListener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                activityFileListener.Start();
                Console.WriteLine("[Professor] FileTransfer listener started on port " + SettingsManager.Current.FileTransferPort);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Professor] FileTransfer bind FAILED: " + ex.Message);
                return;
            }

            while (isRunning)
            {
                try
                {
                    TcpClient client = await activityFileListener.AcceptTcpClientAsync();
                    _ = HandleActivityFileReceive(client);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[Professor] FileTransfer accept error: " + ex.Message);
                    break;
                }
            }
        }

        private async Task HandleActivityFileReceive(TcpClient client)
        {
            try
            {
                using (client)
                using (NetworkStream stream = client.GetStream())
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    string Section = reader.ReadString();
                    string prof_ID = reader.ReadString();
                    string user_ID = reader.ReadString();
                    string fileName = reader.ReadString();
                    int fileLength = reader.ReadInt32();
                    byte[] fileBytes = reader.ReadBytes(fileLength);

                    string sectionFolder = Path.Combine(saveFolder, SanitizeFolderName(Section));

                    if (!Directory.Exists(sectionFolder))
                        Directory.CreateDirectory(sectionFolder);

                    string savePath = Path.Combine(sectionFolder, fileName);
                    File.WriteAllBytes(savePath, fileBytes);

                    OnActivityFileReceived(prof_ID, user_ID, savePath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("HandleActivityFileReceive error: " + ex.Message);
            }
        }

        private void OnActivityFileReceived(string prof_ID, string user_ID, string savePath)
        {
            string connStr = SettingsManager.Current.GetConnectionString();
            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = @"UPDATE submitted_activity SET file_path = @file_path WHERE prof_id = @prof_id AND user_id = @user_id";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@file_path", savePath);
                        cmd.Parameters.AddWithValue("@prof_id", prof_ID);
                        cmd.Parameters.AddWithValue("@user_id", user_ID);
                        cmd.ExecuteNonQuery();
                    }
                    ActivityStatus();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("OnActivityFileReceived error: " + ex.Message);
            }
        }

        private string SanitizeFolderName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }

        private void btnCreateAssessment_Click(object sender, EventArgs e)
        {
            ProfessorQuizForm profQuiz = new ProfessorQuizForm(ProfessorID);
            profQuiz.ShowDialog();
        }
    }
}