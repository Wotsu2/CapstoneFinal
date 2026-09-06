using ClosedXML.Excel;
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
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using UMapx.Distribution;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;
using DevExpress.XtraPdfViewer;
namespace WinFormsApp1
{
    public partial class ProfessorForm : Form
    {

        // WorkStation //
        private TcpListener listener;
        private int WorkStationNum = 0;
        private Dictionary<string, Button> workstationButtons = new Dictionary<string, Button>();
        private Dictionary<string, Button> miniWorkstationButtons = new Dictionary<string, Button>();
        private const int MAX_MINI_BUTTONS = 5;
        private int OnlineCount = 0;
        private int OfflineCount = 0;
        private string selectedWorkstationId = "";

        //Attendance//

        //Activity//
        private string selectedFilePath = "";

        //File Management//
        private string currentFolder;
        private string FolderName;
        private Stack<string> folderHistory = new Stack<string>();
        private string saveFolder;

        int ProfessorID;
        public ProfessorForm(int UserId)
        {
            InitializeComponent();
            ProfessorID = UserId;

        }

        private void ProfessorForm_Load(object sender, EventArgs e)
        {
            saveFolder = GetFolderPath(ProfessorID);

            //DataGridView Desgin//

            // WORKSTATION ATTRIBUTES //
            StartServer();

            // MYSTUDENT Load All Student IN DataGridView//
            LoadAllStudent();

            //Attendance Caller//
            dgvAttendance();

            //Class Subject Caller//
            AutoCreateClassBtn();

            //Activity Caller//
            ActivitySectionSubject();
            RecentActivity();

            //File Management Caller//

            lsServerFolderSetup();
            LoadServerFolder(saveFolder, addToHistory: false);

            //Grade Caller//
            ActivityStatus();
            lblGradesSubmitted.Text = CountTotalSubmitted(ProfessorID).ToString();
            lblGradesGraded.Text = CountTotalGraded(ProfessorID).ToString();
            lblGradesNotSubmitted.Text = CountTotalNotSubmitted(ProfessorID).ToString();

        }

        private void btnHome_Click(object sender, EventArgs e)
        {
            pnlHome.BringToFront();
        }
        private void btnWorkstation_Click(object sender, EventArgs e)
        {
            pnlWorkstation.BringToFront();
        }
        private void btnStudent_Click(object sender, EventArgs e)
        {
            pnlStudent.BringToFront();
        }
        private void btnActivities_Click(object sender, EventArgs e)
        {
            pnlActivity.BringToFront();
            ActivitySectionSubject();
            RecentActivity();
        }
        private void btnGrades_Click(object sender, EventArgs e)
        {
            pnlGrades.BringToFront();
            ActivityStatus();
        }
        private void btnAttendance_Click(object sender, EventArgs e)
        {
            pnlAttendance.BringToFront();
            dgvAttendance();
        }

        private void btnSubject_Click(object sender, EventArgs e)
        {
            pnlSubject.BringToFront();
        }

        private void btnFile_Click(object sender, EventArgs e)
        {
            pnlFile.BringToFront();
            lsServerFolderSetup();
            LoadServerFolder(saveFolder, addToHistory: false);
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

        // WorkStation Page //
        public void WorkstationButton_Click(object sender, EventArgs e)
        {
            Button clickedButton = (Button)sender;
            string workstationId = clickedButton.Tag.ToString();
            selectedWorkstationId = workstationId;
            //AddScreenViewer(workstationId);
        }

        private async void StartServer()
        {
            listener = new TcpListener(IPAddress.Any, 5000);
            listener.Start();

            lblTotalWorkstations.Text = "0";
            lblComputerOnline.Text = "0";
            lblComputerOffline.Text = "0";

            while (true)
            {
                TcpClient client = await listener.AcceptTcpClientAsync();
                string clientIp = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();

                Console.WriteLine("🟢 New TCP connection accepted from: " + clientIp);

                Button wsButton = null;

                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() => wsButton = OnWorkStationConnected(clientIp)));
                }
                else
                {
                    wsButton = OnWorkStationConnected(clientIp);
                }

                _ = MonitorDisconnected(client, wsButton, clientIp);
            }
        }

        private Button OnWorkStationConnected(string clientIp)
        {
            Console.WriteLine("🔵 OnWorkStationConnected called for: " + clientIp);
            Console.WriteLine("   Existing keys: [" + string.Join(", ", workstationButtons.Keys) + "]");
            Console.WriteLine("   Contains this IP? " + workstationButtons.ContainsKey(clientIp));

            // If this PC already has a button (reconnecting), just turn it green again
            if (workstationButtons.ContainsKey(clientIp))
            {
                Console.WriteLine("   ✅ Reusing existing button, setting to green");
                Button existingBtn = workstationButtons[clientIp];
                existingBtn.BackColor = Color.LightGreen;
                if (miniWorkstationButtons.ContainsKey(clientIp))
                {
                    miniWorkstationButtons[clientIp].BackColor = Color.LightGreen;
                }
                UpdateConnectedCount();
                return existingBtn;
            }

            // New PC — create a fresh button
            Console.WriteLine("   🆕 Creating new button");
            WorkStationNum++;

            Button MainPcButton = new Button();
            MainPcButton.Text = "PC " + WorkStationNum;
            MainPcButton.Height = 180;
            MainPcButton.Width = 131;
            MainPcButton.Margin = new Padding(5);
            MainPcButton.BackColor = Color.LightGreen;
            MainPcButton.Tag = clientIp;
            MainPcButton.Click += WorkstationButton_Click;

            flpMainWorkstations.Controls.Add(MainPcButton);
            workstationButtons[clientIp] = MainPcButton;

            Button miniButton = new Button();
            miniButton.Text = "PC " + WorkStationNum;
            miniButton.Height = 150;
            miniButton.Width = 80;
            miniButton.Margin = new Padding(3);
            miniButton.BackColor = Color.LightGreen;
            miniButton.Tag = clientIp;
            miniButton.Click += WorkstationButton_Click;
            if (flpMiniWorkStations.Controls.Count < MAX_MINI_BUTTONS)
            {
                flpMiniWorkStations.Controls.Add(miniButton);
                miniWorkstationButtons[clientIp] = miniButton;
            }
            else
            {
                // Mini panel is full - show indicator
                miniButton.Visible = false;
                miniWorkstationButtons[clientIp] = miniButton; // Store but hide
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
                    if (bytesRead == 0) break; // client disconnected
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("⚠️ MonitorDisconnected exception for " + clientIp + ": " + ex.Message);
            }
            finally
            {
                Console.WriteLine("🔴 Marking as disconnected: " + clientIp);

                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() =>
                    {
                        if (workstationButtons.ContainsKey(clientIp))
                        {
                            workstationButtons[clientIp].BackColor = Color.Red;
                        }

                        // Update mini button
                        if (miniWorkstationButtons.ContainsKey(clientIp))
                        {
                            miniWorkstationButtons[clientIp].BackColor = Color.Red;
                        }
                        UpdateConnectedCount();
                    }));
                }
                else
                {
                    if (workstationButtons.ContainsKey(clientIp))
                    {
                        workstationButtons[clientIp].BackColor = Color.Red;
                    }

                    if (miniWorkstationButtons.ContainsKey(clientIp))
                    {
                        miniWorkstationButtons[clientIp].BackColor = Color.Red;
                    }

                    UpdateConnectedCount();
                }

                client.Close();
            }
        }

        private void UpdateConnectedCount()
        {
            int connectedCount = workstationButtons.Values
                .Count(btn => btn.BackColor == Color.LightGreen);

            int disconnectedCount = workstationButtons.Count - connectedCount;

            lblStudentOnline.Text = $"{connectedCount.ToString()} Online";
            lblComputerOnline.Text = connectedCount.ToString();
            lblComputerOffline.Text = disconnectedCount.ToString();
            lblTotalWorkstations.Text = workstationButtons.Count.ToString();
        }


        //My Student Page//
        // Load All Student IN DataGridView//
        private void LoadAllStudent(string filter = "")
        {
            string dbsName = "cdsga_hub";
            string connStr = $"Server=localhost;Port=3306;Database={dbsName};Uid=root;Pwd=;";

            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = "SELECT u.username, " + "i.lastname, i.firstname, i.middlename, " +
                       "i.school_year, i.school_section, i.school_course " +
                       "FROM user_credential u " +
                       "LEFT JOIN user_information i ON u.user_id = i.user_id " +
                       "WHERE u.roles = 'Student'";

                    bool hasFilter = false;
                    if (!string.IsNullOrEmpty(filter))
                    {
                        query += " WHERE u.user_id LIKE @f1 OR i.lastname LIKE @f2 OR i.firstname LIKE @f3";
                        hasFilter = true;
                    }
                    if (!string.IsNullOrEmpty(cmbSemester.Text))
                    {
                        query += " AND i.school_year = @semester";
                        hasFilter = true;
                    }
                    if (!string.IsNullOrEmpty(cmbSection.Text))
                    {
                        query += " AND i.school_section = @section";
                        hasFilter = true;
                    }
                    if (!string.IsNullOrEmpty(cmbYear.Text))
                    {
                        query += " AND i.school_year = @year";
                        hasFilter = true;
                    }
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        if (!string.IsNullOrEmpty(cmbYear.Text) && cmbYear.Text != "Select Year")
                        {
                            cmd.Parameters.AddWithValue("@year", cmbYear.Text.Trim());
                        }

                        if (!string.IsNullOrEmpty(cmbSection.Text) && cmbSection.Text != "Select Section")
                        {
                            cmd.Parameters.AddWithValue("@section", cmbSection.Text.Trim());
                        }

                        if (!string.IsNullOrEmpty(cmbSemester.Text) && cmbSemester.Text != "Select Semester")
                        {
                            cmd.Parameters.AddWithValue("@semester", cmbSemester.Text.Trim());
                        }

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
            }
        }
        private void txtBoxSearch_TextChanged(object sender, EventArgs e)
        {
            LoadAllStudent(txtBoxSearch.Text);
        }
        private void cmbSemester_SelectedIndexChanged(object sender, EventArgs e)
        {
            lblSemester.Text = "";
            LoadAllStudent();
        }

        private void cmbYear_SelectedIndexChanged(object sender, EventArgs e)
        {
            lblYear.Text = "";
            LoadAllStudent();
        }

        private void cmbSection_SelectedIndexChanged(object sender, EventArgs e)
        {
            lblSection.Text = "";
            LoadAllStudent();
        }

        private void btnCleanFilter_Click(object sender, EventArgs e)
        {
            cmbYear.SelectedIndex = -1;
            cmbSection.SelectedIndex = -1;
            cmbSemester.SelectedIndex = -1;
            lblSemester.Text = "Semester";
            lblYear.Text = "Year";
            lblSection.Text = "Section";
            txtBoxSearch.Text = "";
            LoadAllStudent();
        }

        // Attendance Page //

        private void dgvAttendance()
        {
            string connStr = "Server=localhost;Port=3306;Database=cdsga_hub;Uid=root;Pwd=;";

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

            }
        }

        private static int TotalUsers()
        {
            string connStr = "Server=localhost;Port=3306;Database=cdsga_hub;Uid=root;Pwd=;";

            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = "SELECT COUNT(*) FROM professor_attendance";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        int total = Convert.ToInt32(cmd.ExecuteScalar());
                        return total;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return 0;
            }
        }

        private void btnAddAttendance_Click(object sender, EventArgs e)
        {
            List<(int StudentId, string StudentName)> students = GetAllStudents();

            int total = TotalUsers();

            FlowLayoutPanel column = new FlowLayoutPanel();
            column.FlowDirection = FlowDirection.TopDown;
            column.WrapContents = false;        // never wraps mid-batch
            column.AutoSize = true;
            column.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            column.Width = 160;                  // enough for one textbox + margin
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
                oldest.Dispose(); // free up resources since it's gone for good
            }
            string AttendanceDateNow = DateTime.Today.ToString("MMMdd");

            string connStr = "Server=localhost;Port=3306;Database=cdsga_hub;Uid=root;Pwd=;";

            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
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
                            int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                            columnExists = count > 0;
                        }

                        // 2. Only add the column if it doesn't already exist
                        if (!columnExists)
                        {
                            string AddColumnQuery = $"ALTER TABLE professor_attendance ADD `{AttendanceDateNow}` VARCHAR(20)";
                            using (var cmd = new MySqlCommand(AddColumnQuery, conn))
                            {
                                cmd.ExecuteNonQuery();
                            }
                        }


                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private List<(int StudentId, string StudentName)> GetAllStudents()
        {
            List<(int, string)> list = new List<(int, string)>();
            string connStr = "Server=localhost;Port=3306;Database=cdsga_hub;Uid=root;Pwd=;";

            using (var conn = new MySqlConnection(connStr))
            {
                conn.Open();
                string query = "SELECT student_id, student_name FROM professor_attendance";
                using (var cmd = new MySqlCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add((reader.GetInt32("student_id"), reader.GetString("student_name")));
                    }
                }
            }
            return list;
        }

        private void btnAttendanceUpdate_Click(object sender, EventArgs e)
        {

            string DateToday = DateTime.Today.ToString("MMMdd");

            string connStr = "Server=localhost;Port=3306;Database=cdsga_hub;Uid=root;Pwd=;";
            MessageBox.Show($"Total controls in flpAttendance: {flpAttendance.Controls.Count}");

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
                            string status = cmb.Text; // e.g. "present", "absent", "late"
                            if (status == "present")
                            {
                                string query = $"UPDATE professor_attendance SET `{DateToday}` = @status, present = COALESCE(present, 0) + 1 WHERE student_id = @student_id ";
                                using (var cmd = new MySqlCommand(query, conn))
                                {
                                    cmd.Parameters.AddWithValue("@status", status);
                                    cmd.Parameters.AddWithValue("@student_id", studentId);
                                    cmd.ExecuteNonQuery();
                                }
                            }
                            if (status == "absent")
                            {
                                string query = $"UPDATE professor_attendance SET `{DateToday}` = @status, absent = COALESCE(absent, 0) + 1 WHERE student_id = @student_id ";
                                using (var cmd = new MySqlCommand(query, conn))
                                {
                                    cmd.Parameters.AddWithValue("@status", status);
                                    cmd.Parameters.AddWithValue("@student_id", studentId);
                                    cmd.ExecuteNonQuery();
                                }
                            }
                            if (status == "late")
                            {
                                string query = $"UPDATE professor_attendance SET `{DateToday}` = @status, late = COALESCE(late, 0) + 1 WHERE student_id = @student_id ";
                                using (var cmd = new MySqlCommand(query, conn))
                                {
                                    cmd.Parameters.AddWithValue("@status", status);
                                    cmd.Parameters.AddWithValue("@student_id", studentId);
                                    cmd.ExecuteNonQuery();
                                }
                            }
                        }
                    }
                    dgvAttendance();
                    MessageBox.Show("Update");
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnExportAttendance_Click(object sender, EventArgs e)
        {
            string connStr = "Server=localhost;Port=3306;Database=cdsga_hub;Uid=root;Pwd=;";

            try
            {
                DataTable dt = new DataTable();

                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = "SELECT * FROM professor_attendance"; // change to your table/query

                    using (var adapter = new MySqlDataAdapter(query, conn))
                    {
                        adapter.Fill(dt);
                    }
                }

                if (dt.Rows.Count == 0)
                {
                    MessageBox.Show("No data to export.");
                    return;
                }

                // Let the user choose where to save
                using (SaveFileDialog sfd = new SaveFileDialog())
                {
                    sfd.Filter = "Excel Files|*.xlsx";
                    sfd.FileName = "UserData_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".xlsx";

                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        using (var workbook = new XLWorkbook())
                        {
                            var worksheet = workbook.Worksheets.Add(dt, "Users");
                            worksheet.Columns().AdjustToContents(); // auto-fit column widths

                            workbook.SaveAs(sfd.FileName);
                        }

                        MessageBox.Show("Exported successfully!");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Export failed: " + ex.Message);
            }
        }

        private void btnShowPnlCreateClass_Click(object sender, EventArgs e)
        {
            pnlCreateClass.Visible = true;
            pnlCreateClass.BringToFront(); ;
        }

        private void btnClosePanel_Click(object sender, EventArgs e)
        {
            pnlCreateClass.Visible = false;
            pnlCreateClass.SendToBack();
        }

        private void btnCreateClass_Click(object sender, EventArgs e)
        {
            string connStr = "Server=localhost;Port=3306;Database=cdsga_hub;Uid=root;Pwd=;";
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
                MessageBox.Show(ex.Message);
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

            string connStr = "Server=localhost;Port=3306;Database=cdsga_hub;Uid=root;Pwd=;";
        }
        private static int CountTotalClass(int ProfessorID)
        {
            string connStr = "Server=localhost;Port=3306;Database=cdsga_hub;Uid=root;Pwd=;";
            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = "SELECT COUNT(*) FROM professor_class WHERE professor_id = @professor_id";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@professor_id", ProfessorID);
                        int totalClass = Convert.ToInt32(cmd.ExecuteScalar());
                        return totalClass;
                    }
                }
            }
            catch
            {
                return 0;
            }
        }
        private void AutoCreateClassBtn()
        {
            int totalClasses = CountTotalClass(ProfessorID);
            //MessageBox.Show($"Creating {totalClasses} classes"); // Debug line
            for (int i = 0; i < CountTotalClass(ProfessorID); i++)
            {
                Button ClassButton = new Button();
                ClassButton.Text = "Class " + (i + 1);
                ClassButton.Height = 200;
                ClassButton.Width = 300;
                ClassButton.Margin = new Padding(5);
                ClassButton.BackColor = Color.LightGreen;
                ClassButton.Click += WorkstationButton_Click;

                flpSubjectClass.Controls.Add(ClassButton);
            }
            //MessageBox.Show($"Total buttons: {flpSubjectClass.Controls.Count}");
        }

        //Creating Activity Page//

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
            DateTime now = DateTime.Now;
            string FullDateTime = now.ToString("MMM-dd HH:mm:ss");
            string connStr = "Server=localhost;Port=3306;Database=cdsga_hub;Uid=root;Pwd=;";
            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = @"INSERT INTO professor_activity 
                                    (professor_id, title, description, section, activity_subject, start_time, due_date, activity_status, score, file_path) 
                                    VALUE (@professor_id, @title, @description, @section, @activity_subject, @start_time, @due_date, @activity_status, score, @file_path)";
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
                        cmd.Parameters.AddWithValue("@file_path", Path.GetFileName(selectedFilePath));
                        cmd.ExecuteNonQuery();
                    }
                    MessageBox.Show("Activity Posted Succesfuly");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void ActivitySectionSubject()
        {
            string connStr = "Server=localhost;Port=3306;Database=cdsga_hub;Uid=root;Pwd=;";
            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = @"SELECT class_name, class_section FROM professor_class WHERE professor_id = @professor_id";
                    using (var cmd = new MySqlCommand(query, conn))
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
                MessageBox.Show(ex.Message);
            }
        }

        private void RecentActivity()
        {
            string connStr = "Server=localhost;Port=3306;Database=cdsga_hub;Uid=root;Pwd=;";
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
                MessageBox.Show(ex.Message);
            }
        }

        private void cmbActivityTitle_SelectedIndexChanged(object sender, EventArgs e)
        {
            lblActivitytTitle.Visible = false;
        }

        private void cmbActivitySection_SelectedIndexChanged(object sender, EventArgs e)
        {
            lblActivitySection.Visible = false;
        }

        private void cmbActivitySubject_SelectedIndexChanged(object sender, EventArgs e)
        {
            lblActivitySubject.Visible = false;
        }

        //FILE MANAGEMENT PAGE//
        private void lsServerFolderSetup()
        {
            FolderListView.View = View.LargeIcon;
            FolderListView.LargeImageList = imageList1;
            FolderListView.MultiSelect = false;
        }
        private void LoadServerFolder(string path, bool addToHistory = true)
        {
            if (addToHistory && !string.IsNullOrEmpty(currentFolder))
            {
                folderHistory.Push(currentFolder);
            }

            currentFolder = path;
            FolderListView.Items.Clear();
            imageList1.Images.Clear();
            int imageIndex = 0;

            //To Show Folder
            foreach (string dir in Directory.GetDirectories(path))
            {
                imageList1.Images.Add(Properties.Resources.Folder);
                ListViewItem item = new ListViewItem(Path.GetFileName(dir), imageIndex);
                item.Tag = dir;
                FolderListView.Items.Add(item);
                imageIndex++;
            }

            //to Show File

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

        private void BtnBack_Click(object sender, EventArgs e)
        {
            btnBack();
        }
        private void FolderListView_DoubleClick(object sender, EventArgs e)
        {
            doubleClick();
        }

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
                LoadServerFolder(saveFolder); // refresh the view to show the new folder
            }
            else
            {
                MessageBox.Show("Folder already exists.");
            }
        }
        private static string GetFolderPath(int ProfessorID)
        {
            string connStr = "Server=localhost;Port=3306;Database=cdsga_hub;Uid=root;Pwd=;";
            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = "SELECT FolderPath FROM mainfolderpath WHERE user_id = @user_id";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@user_id", ProfessorID);

                        object result = cmd.ExecuteScalar();

                        if (result != null && result != DBNull.Value)
                        {
                            return result.ToString();
                        }
                        else
                        {
                            return "Null"; // no matching row, or FolderPath is NULL in the DB
                        }
                    }
                }
            }
            catch
            {
                return "Null";
            }

        }


        //Panel Grades//
        private void ActivityStatus(string filter = "")
        {

            string connStr = $"Server=localhost;Port=3306;Database=cdsga_hub;Uid=root;Pwd=;";

            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = @"SELECT user_id, title, section, student_name, class_name, activity_status, score FROM submitted_activity WHERE prof_id = @prof_id";

                    if (!string.IsNullOrEmpty(filter))
                    {
                        query += " AND student_name LIKE @f1";
                    }
                    if (!string.IsNullOrEmpty(cmbActivityGrades.Text))
                    {
                        query += " AND title = @title";
                    }
                    if (!string.IsNullOrEmpty(cmbSectionGrades.Text))
                    {
                        query += " AND section = @section";
                    }
                    if (!string.IsNullOrEmpty(cmbSubjectGrades.Text))
                    {
                        query += " AND class_name = @class_name";
                    }
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@prof_id", ProfessorID);
                        if (!string.IsNullOrEmpty(cmbActivityGrades.Text))
                        {
                            cmd.Parameters.AddWithValue("@title", cmbActivityGrades.Text);
                        }

                        if (!string.IsNullOrEmpty(cmbSectionGrades.Text))
                        {
                            cmd.Parameters.AddWithValue("@section", cmbSectionGrades.Text);
                        }

                        if (!string.IsNullOrEmpty(cmbSubjectGrades.Text))
                        {
                            cmd.Parameters.AddWithValue("@class_name", cmbSubjectGrades.Text);
                        }

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
            }
        }


        private void txtSearchGrades_TextChanged(object sender, EventArgs e)
        {

            ActivityStatus(txtSearchGrades.Text.Trim());
        }

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
            string connStr = "Server=localhost;Port=3306;Database=cdsga_hub;Uid=root;Pwd=;";
            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = "SELECT COUNT(*) FROM submitted_activity WHERE prof_id = @prof_id AND activity_status = 'Submitted'";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@prof_id", ProfessorID);
                        int totalSubmitted = Convert.ToInt32(cmd.ExecuteScalar());
                        return totalSubmitted;
                    }
                }
            }
            catch
            {
                return 0;
            }
        }
        private static int CountTotalGraded(int ProfessorID)
        {
            string connStr = "Server=localhost;Port=3306;Database=cdsga_hub;Uid=root;Pwd=;";
            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = "SELECT COUNT(*) FROM submitted_activity WHERE prof_id = @prof_id AND score IS NOT NULL";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@prof_id", ProfessorID);
                        int totalSubmitted = Convert.ToInt32(cmd.ExecuteScalar());
                        return totalSubmitted;
                    }
                }
            }
            catch
            {
                return 0;
            }
        }
        private static int CountTotalNotSubmitted(int ProfessorID)
        {
            string connStr = "Server=localhost;Port=3306;Database=cdsga_hub;Uid=root;Pwd=;";
            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = "SELECT COUNT(*) FROM submitted_activity WHERE prof_id = @prof_id AND activity_status = 'Incomplete'";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@prof_id", ProfessorID);
                        int totalSubmitted = Convert.ToInt32(cmd.ExecuteScalar());
                        return totalSubmitted;
                    }
                }
            }
            catch
            {
                return 0;
            }
        }

        private void CreatePanelForSubmittedFiles(int User_id, string Title, string Name, string Section, string ClassName, string Status)
        {
            Guna.UI2.WinForms.Guna2Panel panel = new Guna.UI2.WinForms.Guna2Panel();
            panel.Width = 1000;
            panel.Height = 950;
            panel.Margin = new Padding(5);
            panel.Location = new Point(150, 0);
            panel.BorderRadius = 10;
            panel.FillColor = Color.LightGray;

            Label lblTitle = new Label();
            lblTitle.Name = "📝 " + "lblTitle";
            lblTitle.Text = Title;
            lblTitle.Location = new Point(20, 50);
            lblTitle.Size = new Size(200, 25);
            lblTitle.Font = new Font("Arial", 12, FontStyle.Bold);
            panel.Controls.Add(lblTitle);

            Label lblName = new Label();
            lblName.Name = "lblName";
            lblName.Text = "👤 " + Name;
            lblName.Location = new Point(20, 80);
            lblName.Size = new Size(200, 25);
            lblName.Font = new Font("Arial", 12, FontStyle.Bold);
            panel.Controls.Add(lblName);

            Label lblSection = new Label();
            lblSection.Name = "lblSection";
            lblSection.Text = "📝 " + Section;
            lblSection.Location = new Point(20, 110);
            lblSection.Size = new Size(200, 25);
            lblSection.Font = new Font("Arial", 12, FontStyle.Bold);
            panel.Controls.Add(lblSection);

            Label lblClassNameGrades = new Label();
            lblClassNameGrades.Name = "lblClassNameGrades";
            lblClassNameGrades.Text = "📝 " + ClassName;
            lblClassNameGrades.Location = new Point(20, 140);
            lblClassNameGrades.Size = new Size(200, 25);
            lblClassNameGrades.Font = new Font("Arial", 12, FontStyle.Bold);
            panel.Controls.Add(lblClassNameGrades);

            Label lblStatus = new Label();
            lblStatus.Name = "lblStatus";
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
                string pdfPath = @"C:\Users\mjm12\OneDrive\Desktop\Sti Activities and Assigment\quiz_1.pdf";  // Change this to your actual path
                if (File.Exists(pdfPath))
                {
                    pdfViewer.LoadDocument(pdfPath);
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
                MessageBox.Show("Error loading PDF: " + ex.Message);
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
                string connStr = "Server=localhost;Port=3306;Database=cdsga_hub;Uid=root;Pwd=;";

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
                        string query = @"UPDATE submitted_activity 
                                         SET score = @score 
                                         WHERE user_id = @user_id";
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
                catch
                {

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
                CreatePanelForSubmittedFiles(User_id, Title, Name, SectionGrades, ClassNameGrades, StatusGrades);
            }
        }
        private void GetSection()
        {
            string connStr = "Server=localhost;Port=3306;Database=cdsga_hub;Uid=root;Pwd=;";

            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = @"SELECT class_name, class_section FROM professor_class WHERE professor_id = @professor_id";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@professor_id", ProfessorID);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string className = reader.GetString("class_name");
                                string classSection = reader.GetString("class_section");

                                if (!cmbSubjectGrades.Items.Contains(className))
                                {
                                    cmbSubjectGrades.Items.Add(className);
                                }

                                if (!cmbSectionGrades.Items.Contains(classSection))
                                {
                                    cmbSectionGrades.Items.Add(classSection);
                                }
                            }
                        }
                    }
                }
            }
            catch
            {

            }
        }
    }
}
