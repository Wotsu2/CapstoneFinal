using ClosedXML.Excel;
using MySql.Data.MySqlClient;
using MySqlX.XDevAPI;
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

        int ProfessorID;
        public ProfessorForm(int UserId)
        {
            InitializeComponent();
            ProfessorID = UserId;
        }

        private void ProfessorForm_Load(object sender, EventArgs e)
        {
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
        }
        private void btnGrades_Click(object sender, EventArgs e)
        {
            pnlGrades.BringToFront();
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
            FlowLayoutPanel currentColumn = (FlowLayoutPanel)flpAttendance.Controls[0];
            string DateToday = DateTime.Today.ToString("MMMdd");

            string connStr = "Server=localhost;Port=3306;Database=cdsga_hub;Uid=root;Pwd=;";
            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
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
                    AutoCreateClassBtn();
                    MessageBox.Show("Created Succesfuly");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
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
    }
}
