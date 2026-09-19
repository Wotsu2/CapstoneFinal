using Guna.UI2.WinForms;
using MySql.Data.MySqlClient;
using Org.BouncyCastle.Asn1.Cmp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

namespace WinFormsApp1
{
    public partial class StudentForm : Form
    {
        private TcpClient client;
        private System.Windows.Forms.Timer screenShareTimer;
        private TcpClient screenClient;
        private bool isSharingScreen = false;
        private TcpClient broadcastClient;
        private TcpListener Shutdownlistener;
        private BroadcastViewerForm broadcastViewer;
        private Guna2Button activeMenuButton;

        private volatile bool isSignedOut = false;
        private string StudentSection;
        private string userId;
        private string activityId;
        //Grades//
        private string selectedGradeCategory = "";
        private string selectedActivitiesCategory = "";
        private string file_path;
        private string studentname;
        private string activitySubject;
        private string StudentUsername;
        private string CurrentProfilePath;
        private string SaveCurrentProfilePath;
        private string AuthenticationPhoto;
        private string SaveAuthenticationPhoto;
        private string Isauthentication_photoEmpty;

        public StudentForm(int UserId, string Section, string Username)
        {
            InitializeComponent();
            StudentSection = Section;
            userId = UserId.ToString();

            StudentUsername = Username;
            initializeShowReminderForm();

            if (string.IsNullOrEmpty(Isauthentication_photoEmpty))
            {
                FacialRecognitionReminderForm reminderForm = new FacialRecognitionReminderForm(int.Parse(userId), StudentUsername);
                reminderForm.ShowDialog();
            }
        }
        private void StudentForm_Load(object sender, EventArgs e)
        {
            isSharingScreen = true;
            lblProfUsername.Text = StudentUsername;
            ConnectToServer();
            StartScreenShare();
            ConnectBroadcastReceiver();
            StartListening();

            //Home Caller//
            InitializeCreateButtonActivity();

            //Activitiy Caller//
            InitializeDataGridViewActivities();
            NameGet();

            //Grades Caller//
            InitializeDataGridViewGrades();

            //Subject Caller//
            LoadJoinedClasses();

            //Settings//
            InitializeSaveDirectory();
            InitializeChangingPicture();
            InitializeAuthenticationSaveDirectory();

            lblStudentName.Text = studentname;
        }

        private void initializeShowReminderForm()
        {
            string connStr = SettingsManager.Current.GetConnectionString();

            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = "SELECT authentication_photo FROM user_credential WHERE username = @username";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@username", StudentUsername);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                Isauthentication_photoEmpty = reader.GetString("authentication_photo");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred: " + ex.Message);
            }
        }



        private void SetActiveMenuButton(Guna2Button clickedBtn, Panel panelToShow)
        {
            foreach (var b in new[] { btnHome, btnActivities, btnSubject, btnGrades })
            {
                b.FillColor = Color.Transparent;
                b.ForeColor = Color.Firebrick;
            }
            clickedBtn.FillColor = Color.Firebrick;
            clickedBtn.ForeColor = Color.FromArgb(80, 12, 24);
            activeMenuButton = clickedBtn;

            // ipakita yung tamang panel
            panelToShow.BringToFront();
        }

        private void btnHome_Click(object sender, EventArgs e)
        {
            SetActiveMenuButton(btnHome, pnlHome);
            lblhometitle.Text = "Home";
        }

        private void btnActivities_Click(object sender, EventArgs e)
        {
            SetActiveMenuButton(btnActivities, pnlActivity);
            lblhometitle.Text = "Activity";
        }

        private void btnSubject_Click(object sender, EventArgs e)
        {
            SetActiveMenuButton(btnSubject, pnlSubject);
            lblhometitle.Text = "Subject";
        }

        private void btnGrades_Click(object sender, EventArgs e)
        {
            SetActiveMenuButton(btnGrades, pnlGrades);
            lblhometitle.Text = "Grade";
        }
        private void btnAccount_Click(object sender, EventArgs e)
        {
            pnlSetting.BringToFront();
            lblhometitle.Text = "Settings";
        }

        //Connect the Client to the Server//
        private async void ConnectToServer()
        {
            try
            {
                isSignedOut = false;

                client = new TcpClient();
                await client.ConnectAsync(SettingsManager.Current.ServerIp, SettingsManager.Current.WorkstationPort); // use the SERVER's actual IP here And Should be Empty and configure it to setting

                MessageBox.Show("Connected to server!");

                // Keep the connection alive (so the server knows you're still online)
                _ = KeepAlive();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Connection failed: " + ex.Message);
            }
        }
        private async Task KeepAlive()
        {
            try
            {
                while (!isSignedOut && client != null && client.Connected)
                {
                    await Task.Delay(2000); // just idle — connection itself signals "online"
                }
            }
            catch { }
        }

        //Share the Screen of the Client to the Server//

        private void StartScreenShare()
        {
            try
            {
                screenClient = new TcpClient();
                screenClient.Connect(SettingsManager.Current.ServerIp, SettingsManager.Current.ScreenSharePort); // dedicated screen-share port

                isSharingScreen = true;

                screenShareTimer = new System.Windows.Forms.Timer();
                screenShareTimer.Interval = 500; // send a frame every 0.5s
                screenShareTimer.Tick += ScreenShareTimer_Tick;
                screenShareTimer.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not start screen share: " + ex.Message);
            }
        }

        private void ScreenShareTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                Bitmap screenshot = CaptureScreen();

                using (MemoryStream ms = new MemoryStream())
                {
                    screenshot.Save(ms, ImageFormat.Jpeg);
                    byte[] imageBytes = ms.ToArray();

                    NetworkStream stream = screenClient.GetStream();
                    byte[] lengthPrefix = BitConverter.GetBytes(imageBytes.Length);

                    stream.Write(lengthPrefix, 0, lengthPrefix.Length);
                    stream.Write(imageBytes, 0, imageBytes.Length);
                }

                screenshot.Dispose();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Screen share stopped: " + ex.Message);
                screenShareTimer.Stop();
                isSharingScreen = false;
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

        //Professor can Lock the Input of the Client Computer When Sharing Screen//
        private async Task ConnectBroadcastReceiver()
        {
            try
            {
                broadcastClient = new TcpClient();
                await broadcastClient.ConnectAsync(SettingsManager.Current.ServerIp, SettingsManager.Current.BroadcastPort);

                _ = ReceiveBroadcast();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Could not connect to broadcast: " + ex.Message);
            }
        }

        private async Task ReceiveBroadcast()
        {
            NetworkStream stream = broadcastClient.GetStream();

            try
            {
                isSignedOut = false;
                while (!isSignedOut)
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

                        this.Invoke(new Action(() => ShowBroadcastFrame(frame)));
                    }
                }
            }
            catch
            {
            }
            finally
            {
                this.Invoke(new Action(() =>
                {
                    if (broadcastViewer != null && !broadcastViewer.IsDisposed)
                    {
                        broadcastViewer.Close();
                        broadcastViewer = null;
                    }
                }));
            }
        }

        private void ShowBroadcastFrame(Image frame)
        {
            if (broadcastViewer == null || broadcastViewer.IsDisposed)
            {
                broadcastViewer = new BroadcastViewerForm();
                broadcastViewer.Show();
            }

            Image oldImage = broadcastViewer.GetPictureBox().Image;
            broadcastViewer.GetPictureBox().Image = frame;
            oldImage?.Dispose();
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

        //ShutDown//
        private async void StartListening()
        {
            Shutdownlistener = new TcpListener(IPAddress.Any, SettingsManager.Current.CommandPort);
            Shutdownlistener.Start();

            System.Threading.Thread t = new System.Threading.Thread(ListenForCommands);
            t.IsBackground = true;
            t.Start();
        }

        private void ListenForCommands()
        {
            while (isSharingScreen)
            {
                try
                {
                    TcpClient client = Shutdownlistener.AcceptTcpClient();
                    NetworkStream stream = client.GetStream();

                    byte[] buffer = new byte[1024];
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    string command = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

                    if (command == "SHUTDOWN")
                    {
                        this.Invoke((MethodInvoker)delegate
                        {
                        });

                        client.Close();
                        System.Threading.Thread.Sleep(1000);
                        System.Diagnostics.Process.Start("shutdown", "/s /f /t 0");
                    }
                    else if (command == "RESTART")
                    {
                        this.Invoke((MethodInvoker)delegate
                        {
                        });

                        client.Close();
                        System.Threading.Thread.Sleep(1000);
                        System.Diagnostics.Process.Start("shutdown", "/r /f /t 0");
                    }

                    client.Close();
                }
                catch { }
            }
        }


        //Home//
        private void InitializeCreateButtonActivity()
        {
            string connStr = SettingsManager.Current.GetConnectionString();

            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    string query = @"SELECT activity_id, title, start_time, due_date, activity_subject, activity_status 
                         FROM professor_activity 
                         WHERE section = @section";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@section", StudentSection);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())   // ⭐ while, not if
                            {
                                int activityId = reader.GetInt32("activity_id");
                                string title = reader.GetString("title");
                                string start_time = reader.GetString("start_time");
                                string due_date = reader.GetString("due_date");
                                string className = reader.GetString("activity_subject");
                                string activity_status = reader.GetString("activity_status");

                                Guna.UI2.WinForms.Guna2Button ActivityButton = new Guna.UI2.WinForms.Guna2Button();
                                ActivityButton.Height = 180;
                                ActivityButton.Width = 180;
                                ActivityButton.Margin = new Padding(5);
                                ActivityButton.FillColor = Color.Transparent;
                                ActivityButton.BackColor = Color.Transparent;
                                ActivityButton.BorderThickness = 1;
                                ActivityButton.BorderColor = Color.Gray;
                                ActivityButton.BorderRadius = 10;

                                // ⭐ Capture the ID locally so each button uses its OWN id
                                int capturedId = activityId;
                                ActivityButton.Click += (s, e) =>
                                {
                                    InitializeHomeActivityButton(capturedId);
                                };

                                Label Title = new Label();
                                Title.Text = title;
                                Title.ForeColor = Color.Black;
                                Title.BackColor = Color.Transparent;
                                Title.Location = new Point(20, 50);
                                Title.Font = new Font(Title.Font, FontStyle.Bold);
                                ActivityButton.Controls.Add(Title);

                                Label DueDate = new Label();
                                DueDate.Text = $"Due: {due_date}";
                                DueDate.ForeColor = Color.Black;
                                DueDate.BackColor = Color.DarkViolet;
                                DueDate.Width = 150;
                                DueDate.Location = new Point(55, 0);
                                ActivityButton.Controls.Add(DueDate);

                                Label Status = new Label();
                                Status.Text = activity_status;
                                Status.ForeColor = Color.Black;
                                Status.BackColor = Color.Transparent;
                                Status.Width = 150;
                                Status.Location = new Point(40, 90);
                                ActivityButton.Controls.Add(Status);

                                Label ViewActivity = new Label();
                                ViewActivity.Text = "View Activity >";
                                ViewActivity.ForeColor = Color.Maroon;
                                ViewActivity.BackColor = Color.Transparent;
                                ViewActivity.Location = new Point(80, 150);
                                ActivityButton.Controls.Add(ViewActivity);

                                // ⭐ Labels swallow clicks — forward them to the button
                                Title.Click += (s, e) => ActivityButton.PerformClick();
                                DueDate.Click += (s, e) => ActivityButton.PerformClick();
                                Status.Click += (s, e) => ActivityButton.PerformClick();

                                flpPendingActivities.Controls.Add(ActivityButton);
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
        private void InitializeHomeActivityButton(int ActivityId)
        {
            string connStr = SettingsManager.Current.GetConnectionString();
            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = "SELECT activity_id, title, start_time, due_date, activity_subject, activity_status, description, professor_id FROM professor_activity WHERE activity_id = @activity_id";


                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@activity_id", ActivityId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int profId = reader.GetInt32("professor_id");
                                string title = reader.GetString("title");
                                string due_date = reader.GetString("due_date");
                                string activity_subject = reader.GetString("activity_subject");
                                string activity_status = reader.GetString("activity_status");
                                string description = reader.GetString("description");

                                string tempPdfPath = FetchActivityPdf(ActivityId, profId, title, StudentSection, activity_subject);
                                ActivityForm activityForm = new ActivityForm(
                    profId, userId, studentname, title, due_date, description,
                    StudentSection, activity_subject, activity_status, tempPdfPath);

                                activityForm.Show();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading activities: " + ex.Message);
            }
        }
        //Activities//
        private void InitializeDataGridViewActivities()
        {
            string connStr = SettingsManager.Current.GetConnectionString();

            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    string query = "SELECT activity_id, title, start_time, due_date, activity_subject, activity_status, description, professor_id " +
                                   "FROM professor_activity WHERE section = @section";

                    if (!string.IsNullOrEmpty(selectedActivitiesCategory))
                    {
                        query += " AND activity_status = @activity_status";
                    }

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@section", StudentSection);

                        if (!string.IsNullOrEmpty(selectedActivitiesCategory))
                        {
                            cmd.Parameters.AddWithValue("@activity_status", selectedActivitiesCategory);
                        }

                        MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);

                        dgvStudentActivities.DataSource = dt;

                        if (dgvStudentActivities.Columns.Contains("description"))
                            dgvStudentActivities.Columns["description"].Visible = false;

                        if (dgvStudentActivities.Columns.Contains("professor_id"))
                            dgvStudentActivities.Columns["professor_id"].Visible = false;

                        // If you still need activityId for something, grab it from the DataTable
                        if (dt.Rows.Count > 0)
                        {
                            activityId = dt.Rows[0]["activity_id"].ToString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading activities: " + ex.Message);
            }
        }

        private void dgvStudentActivities_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {

            if (e.RowIndex >= 0)
            {
                if (e.RowIndex < 0) return;

                DataGridViewRow row = dgvStudentActivities.Rows[e.RowIndex];

                string activityId = GetSafeValue(row, "activity_id");            // if you added the id column
                string Title = GetSafeValue(row, "title");
                string DueDate = GetSafeValue(row, "due_date");
                string ActivityStatus = GetSafeValue(row, "activity_status");
                string Description = GetSafeValue(row, "description");
                string profId = GetSafeValue(row, "professor_id");
                string className = GetSafeValue(row, "activity_subject");

                // Fetch the PDF bytes from DB and write to a temp file
                string tempPdfPath = FetchActivityPdf(int.Parse(activityId), int.Parse(profId), Title, StudentSection, className);
                ActivityForm activityForm = new ActivityForm(
                    int.Parse(profId), userId, studentname, Title, DueDate, Description,
                    StudentSection, activitySubject, ActivityStatus, tempPdfPath);

                activityForm.Show();
            }
        }
        private string FetchActivityPdf(int activityId, int profId, string title, string section, string className)
        {
            string connStr = SettingsManager.Current.GetConnectionString();
            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    // Prefer id if available; otherwise fall back to composite key
                    string query;
                    if (activityId > 0)
                    {
                        query = @"SELECT activity_file, activity_filename 
                          FROM professor_activity 
                          WHERE activity_id = @activity_id";
                    }
                    else
                    {
                        query = @"SELECT activity_file, activity_filename 
                          FROM professor_activity 
                          WHERE professor_id = @professor_id 
                            AND title = @title 
                            AND section = @section 
                            AND activity_subject = @activity_subject
                          LIMIT 1";
                    }

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        if (activityId > 0)
                            cmd.Parameters.AddWithValue("@activity_id", activityId);
                        else
                        {
                            cmd.Parameters.AddWithValue("@professor_id", profId);
                            cmd.Parameters.AddWithValue("@title", title);
                            cmd.Parameters.AddWithValue("@section", section);
                            cmd.Parameters.AddWithValue("@activity_subject", className);
                        }

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (!reader.Read()) return null;

                            if (reader.IsDBNull(reader.GetOrdinal("activity_file")))
                                return null;

                            byte[] pdfBytes = (byte[])reader["activity_file"];
                            string pdfName = reader["activity_filename"] as string ?? "activity.pdf";

                            // Write to a per-user temp folder so parallel students don't collide
                            string tempFolder = Path.Combine(Path.GetTempPath(), "cdsga_activities", userId);
                            Directory.CreateDirectory(tempFolder);

                            string tempPath = Path.Combine(tempFolder, pdfName);
                            File.WriteAllBytes(tempPath, pdfBytes);

                            return tempPath;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error fetching activity PDF: " + ex.Message);
                return null;
            }
        }
        private void NameGet()
        {
            string connStr = SettingsManager.Current.GetConnectionString();
            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = "SELECT lastname, firstname, middlename FROM user_information WHERE user_id = @user_id";


                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@user_id", userId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string Lastname = reader.GetString("lastname");
                                string Firstname = reader.GetString("firstname");
                                string Middlename = reader.GetString("middlename");

                                studentname = $"{Lastname}_{Firstname}_{Middlename}";

                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading activities: " + ex.Message);
            }
        }
        private string GetSafeValue(DataGridViewRow row, string columnName)
        {
            object raw = row.Cells[columnName].Value;
            return (raw == null || raw == DBNull.Value) ? "" : raw.ToString();
        }
        private void btnActivitiesAll_Click(object sender, EventArgs e)
        {
            selectedActivitiesCategory = "";
            btnActivitiesAll.FillColor = Color.Maroon;
            btnActivitiesPending.FillColor = Color.White;
            btnActivitiesSubmitted.FillColor = Color.White;
            btnActivitiesPassDue.FillColor = Color.White;
            InitializeDataGridViewActivities();
        }

        private void btnActivitiesPending_Click(object sender, EventArgs e)
        {
            selectedActivitiesCategory = "Pending";
            btnActivitiesPending.FillColor = Color.Maroon;
            btnActivitiesAll.FillColor = Color.White;
            btnActivitiesSubmitted.FillColor = Color.White;
            btnActivitiesPassDue.FillColor = Color.White;
            InitializeDataGridViewActivities();
        }

        private void btnActivitiesSubmitted_Click(object sender, EventArgs e)
        {
            selectedActivitiesCategory = "Submitted";
            btnActivitiesSubmitted.FillColor = Color.Maroon;
            btnActivitiesAll.FillColor = Color.White;
            btnActivitiesPending.FillColor = Color.White;
            btnActivitiesPassDue.FillColor = Color.White;
            InitializeDataGridViewActivities();
        }

        private void btnActivitiesPassDue_Click(object sender, EventArgs e)
        {
            selectedActivitiesCategory = "Incomplete";
            btnActivitiesPassDue.FillColor = Color.Maroon;
            btnActivitiesSubmitted.FillColor = Color.White;
            btnActivitiesAll.FillColor = Color.White;
            btnActivitiesPending.FillColor = Color.White;
            InitializeDataGridViewActivities();
        }


        //Grades//

        private void InitializeDataGridViewGrades()
        {
            string connStr = SettingsManager.Current.GetConnectionString();

            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = "SELECT title, section, class_name, activity_status, score FROM submitted_activity WHERE user_id = @user_id";

                    if (!string.IsNullOrEmpty(selectedGradeCategory))
                    {
                        query += " AND activity_status = @activity_status";
                    }

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@user_id", userId);

                        if (!string.IsNullOrEmpty(selectedGradeCategory))
                            cmd.Parameters.AddWithValue("@activity_status", selectedGradeCategory);

                        MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);

                        dgvStudentGrades.DataSource = dt;

                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading activities: " + ex.Message);
            }
        }

        private void btnGradesAll_Click(object sender, EventArgs e)
        {
            selectedGradeCategory = "";
            btnGradesAll.FillColor = Color.Maroon;
            btnGradesDue.FillColor = Color.White;
            btnGradesSubmitted.FillColor = Color.White;
            InitializeDataGridViewGrades();

        }
        private void btnGradesSubmitted_Click(object sender, EventArgs e)
        {
            selectedGradeCategory = "Submitted";
            InitializeDataGridViewGrades();
            btnGradesAll.FillColor = Color.White;
            btnGradesDue.FillColor = Color.White;
            btnGradesSubmitted.FillColor = Color.Maroon;
        }
        private void btnGradesDue_Click(object sender, EventArgs e)
        {
            selectedGradeCategory = "Incomplete";
            InitializeDataGridViewGrades();
            btnGradesAll.FillColor = Color.White;
            btnGradesDue.FillColor = Color.Maroon;
            btnGradesSubmitted.FillColor = Color.White;
        }

        //Subject//
        private void btnJoinClass_Click(object sender, EventArgs e)
        {
            pnlCreateClass.Visible = true;
        }

        private void btnCloseJointClassPanel_Click(object sender, EventArgs e)
        {
            pnlCreateClass.Visible = false;
        }

        private void btnEnterClass_Click(object sender, EventArgs e)
        {
            string txtcode = txtEnterCode.Text;
            string connStr = SettingsManager.Current.GetConnectionString();

            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = "SELECT professor_id, class_name, class_section, class_time, class_date FROM professor_class WHERE class_code = @class_code";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@class_code", txtcode);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int professor_id = reader.GetInt32("professor_id");
                                string class_name = reader.GetString("class_name");
                                string class_section = reader.GetString("class_section");
                                string class_time = reader.GetString("class_time");
                                string class_date = reader.GetString("class_date");


                                InitializeJoinClass(professor_id, class_name, class_section, class_time, class_date);

                            }
                        }
                    }
                    pnlCreateClass.Visible = false;
                }
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message);
            }
        }
        private void InitializeJoinClass(int professorId, string className, string classSection, string classTime, string classDate)
        {
            string connStr = SettingsManager.Current.GetConnectionString();

            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    // Check if the same information already exists
                    string checkQuery = @"
        SELECT COUNT(*)
        FROM student_class
        WHERE professor_id = @professor_id
        AND user_id = @user_id
        AND class_name = @class_name
        AND section = @section
        AND class_time = @class_time
        AND class_date = @class_date";

                    using (var checkCmd = new MySqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@professor_id", professorId);
                        checkCmd.Parameters.AddWithValue("@user_id", userId);
                        checkCmd.Parameters.AddWithValue("@class_name", className);
                        checkCmd.Parameters.AddWithValue("@section", classSection);
                        checkCmd.Parameters.AddWithValue("@class_time", classTime);
                        checkCmd.Parameters.AddWithValue("@class_date", classDate);

                        int count = Convert.ToInt32(checkCmd.ExecuteScalar());

                        if (count > 0)
                        {
                            MessageBox.Show("This class information already exists.");
                            return;
                        }
                    }

                    // Insert if it doesn't exist
                    string query = @"
        INSERT INTO student_class
        (professor_id, user_id, class_name, section, class_time, class_date)
        VALUES
        (@professor_id, @user_id, @class_name, @section, @class_time, @class_date)";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@professor_id", professorId);
                        cmd.Parameters.AddWithValue("@user_id", userId);
                        cmd.Parameters.AddWithValue("@class_name", className);
                        cmd.Parameters.AddWithValue("@section", classSection);
                        cmd.Parameters.AddWithValue("@class_time", classTime);
                        cmd.Parameters.AddWithValue("@class_date", classDate);

                        cmd.ExecuteNonQuery();
                    }

                    InitializeCreadeClass(
                        className,
                        classSection,
                        classTime,
                        classDate);

                    MessageBox.Show("Successfully Joined Class!");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void InitializeCreadeClass(string classname, string classSection, string classTime, string classDate)
        {
            Guna.UI2.WinForms.Guna2Button ClassButton = new Guna.UI2.WinForms.Guna2Button();
            ClassButton.Height = 180;
            ClassButton.Width = 180;
            ClassButton.Margin = new Padding(5);
            ClassButton.BorderColor = Color.Black;
            ClassButton.BorderThickness = 1;

            Label className = new Label();
            className.Text = classname;
            className.ForeColor = Color.Black;
            className.BackColor = Color.Green;
            className.Location = new Point(60, 50);
            ClassButton.Controls.Add(className);

            Label ClassSection = new Label();
            ClassSection.Text = classSection;
            ClassSection.ForeColor = Color.Black;
            ClassSection.BackColor = Color.Transparent;
            ClassSection.Location = new Point(180, 0);
            ClassButton.Controls.Add(ClassSection);

            Label DueDate = new Label();
            DueDate.Text = classTime;
            DueDate.ForeColor = Color.Black;
            DueDate.BackColor = Color.Violet;
            DueDate.Location = new Point(60, 0);
            ClassButton.Controls.Add(DueDate);

            Label Status = new Label();
            Status.Text = classDate;
            Status.ForeColor = Color.Black;
            Status.BackColor = Color.Transparent;
            Status.Location = new Point(0, 120);
            ClassButton.Controls.Add(Status);


            ContextMenuStrip menu = new ContextMenuStrip();
            ToolStripMenuItem unjoinItem = new ToolStripMenuItem("Unjoin");
            unjoinItem.Click += (s, e) =>
            {
                DialogResult result = MessageBox.Show(
                    $"Are you sure you want to unjoin '{classname}'?",
                    "Confirm Unjoin",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result != DialogResult.Yes) return;

                // 1. Remove from database
                UnjoinClass(classname, classSection, classTime, classDate);

                // 2. Remove from the flow panel
                flpSubjectClass.Controls.Remove(ClassButton);
                ClassButton.Dispose();
            };
            menu.Items.Add(unjoinItem);
            ClassButton.ContextMenuStrip = menu;

            // Also allow right-click on the child labels
            className.ContextMenuStrip = menu;
            ClassSection.ContextMenuStrip = menu;
            DueDate.ContextMenuStrip = menu;
            Status.ContextMenuStrip = menu;

            foreach (Control c in ClassButton.Controls)
            {
                c.ContextMenuStrip = menu;
            }

            flpSubjectClass.Controls.Add(ClassButton);
        }

        private void LoadJoinedClasses()
        {
            string connStr = SettingsManager.Current.GetConnectionString();

            try
            {
                // Clear existing buttons first
                flpSubjectClass.Controls.Clear();

                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    string query = @"
SELECT class_name, section, class_time, class_date
FROM student_class
WHERE user_id = @user_id";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@user_id", userId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string className = reader["class_name"].ToString();
                                string classSection = reader["section"].ToString();
                                string classTime = reader["class_time"].ToString();
                                string classDate = reader["class_date"].ToString();

                                InitializeCreadeClass(className, classSection, classTime, classDate);
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
        private void UnjoinClass(string classname, string classSection, string classTime, string classDate)
        {
            string connStr = SettingsManager.Current.GetConnectionString();

            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    string query = @"
DELETE FROM student_class
WHERE user_id = @user_id
AND class_name = @class_name
AND section = @section
AND class_time = @class_time
AND class_date = @class_date";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@user_id", userId);
                        cmd.Parameters.AddWithValue("@class_name", classname);
                        cmd.Parameters.AddWithValue("@section", classSection);
                        cmd.Parameters.AddWithValue("@class_time", classTime);
                        cmd.Parameters.AddWithValue("@class_date", classDate);

                        int rows = cmd.ExecuteNonQuery();

                        if (rows > 0)
                            MessageBox.Show("Successfully unjoined class.");
                        else
                            MessageBox.Show("No matching class found to unjoin.");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error unjoining class: " + ex.Message);
            }
        }


        //Settings//
        private void btnSettingProfileExpand_Click(object sender, EventArgs e)
        {

            if (pnlSettingProfile.Height <= 350)
            {
                pnlSettingProfile.Height = 592;
            }
            else if (pnlSettingProfile.Height >= 592)
            {
                pnlSettingProfile.Height = 350;
            }
        }
        private void ClearAllFormData()
        {
            // Clear all textboxes, combos, grids, etc.
            foreach (Control ctrl in this.Controls)
            {
                if (ctrl is TextBox)
                    ((TextBox)ctrl).Text = "";
                else if (ctrl is ComboBox)
                    ((ComboBox)ctrl).SelectedIndex = -1;
                else if (ctrl is DataGridView)
                    ((DataGridView)ctrl).DataSource = null;
                else if (ctrl is ListBox)
                    ((ListBox)ctrl).Items.Clear();
            }
        }

        private void StopServer()
        {
            isSignedOut = true;

            try
            {
                if (screenShareTimer != null)
                {
                    screenShareTimer.Stop();
                    screenShareTimer.Tick -= ScreenShareTimer_Tick;
                    screenShareTimer.Dispose();
                    screenShareTimer = null;
                }
            }
            catch { }

            isSharingScreen = false;

            try { client?.Close(); } catch { }
            try { client?.Dispose(); } catch { }
            client = null;

            try { screenClient?.Close(); } catch { }
            try { screenClient?.Dispose(); } catch { }
            screenClient = null;

            try { broadcastClient?.Close(); } catch { }
            try { broadcastClient?.Dispose(); } catch { }
            broadcastClient = null;

            try { Shutdownlistener?.Stop(); } catch { }
            try { Shutdownlistener?.Server?.Dispose(); } catch { }
            Shutdownlistener = null;
            try
            {
                if (broadcastViewer != null && !broadcastViewer.IsDisposed)
                {
                    broadcastViewer.Close();
                    broadcastViewer = null;
                }
            }
            catch { }

            MessageBox.Show("Signed out successfully.");

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

                this.Close();

                Login login = new Login();
                login.Show();

            }
        }

        private void btnSignOut_Click(object sender, EventArgs e)
        {
            Logout();
        }

        private void btnSignOut2_Click(object sender, EventArgs e)
        {
            Logout();
        }
        private void btnSettingChangeUsername_Click(object sender, EventArgs e)
        {
            pnlChangeUsername.Visible = true;
            pnlChangePassword.Visible = false;
            pnlChangePhoto.Visible = false;
        }
        private void btnExitChangeUsernamePanel_Click(object sender, EventArgs e)
        {
            pnlChangeUsername.Visible = false;
        }

        private void btnSubmitChangeUsername_Click(object sender, EventArgs e)
        {
            string connStr = SettingsManager.Current.GetConnectionString();

            if (string.IsNullOrEmpty(txtCurrentUsername.Text) || string.IsNullOrEmpty(txtNewUsername.Text))
            {
                MessageBox.Show("Please enter both the current and new usernames.");
                return;
            }

            if (txtCurrentUsername.Text != StudentUsername)
            {
                MessageBox.Show("Please enter the Correct usernames.");
                return;
            }

            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = @"UPDATE user_credential 
                                         SET username = @new_username 
                                         WHERE username = @current_username";
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
            catch
            {

            }
        }
        private void btnSettingChangePassword_Click(object sender, EventArgs e)
        {
            pnlChangePassword.Visible = true;
            pnlChangeUsername.Visible = false;
            pnlChangePhoto.Visible = false;
        }

        private void btnExitChangePasswordPanel_Click(object sender, EventArgs e)
        {
            pnlChangePassword.Visible = false;
        }

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
                    string query = @"UPDATE user_credential 
                                         SET p_word = @new_password 
                                         WHERE username = @current_username";
                    using (var cmd = new MySqlCommand(query, conn))
                    {

                        cmd.Parameters.AddWithValue("@new_password", txtNewPassword.Text.Trim());
                        cmd.Parameters.AddWithValue("@current_username", StudentUsername);

                        cmd.ExecuteNonQuery();
                    }
                    ClearTextSettings();
                    MessageBox.Show("Password updated successfully.");
                }
            }
            catch
            {

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
            // Create a "Images" folder inside the solution directory
            string solutionDirectory = AppDomain.CurrentDomain.BaseDirectory;
            SaveCurrentProfilePath = Path.Combine(solutionDirectory, "StudentProfilePicture");

            if (!Directory.Exists(SaveCurrentProfilePath))
            {
                Directory.CreateDirectory(SaveCurrentProfilePath);
            }
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
                    string query = @"UPDATE user_credential 
                                     SET profile_picture = @profile_picture 
                                     WHERE username = @username";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        // Save the image to the designated folder
                        string fileName = Path.GetFileName(CurrentProfilePath);
                        string destinationPath = Path.Combine(SaveCurrentProfilePath, fileName);
                        File.Copy(CurrentProfilePath, destinationPath, true);
                        cmd.Parameters.AddWithValue("@profile_picture", destinationPath);
                        cmd.Parameters.AddWithValue("@username", StudentUsername);
                        cmd.ExecuteNonQuery();
                    }
                    InitializeChangingPicture();
                    MessageBox.Show("Profile picture updated successfully.");
                }
            }
            catch
            {

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
                    string query = @"SELECT profile_picture FROM user_credential WHERE username = @username";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@username", StudentUsername);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string profilePicturePath = reader.GetString("profile_picture");
                                if (File.Exists(profilePicturePath))
                                {
                                    picboxSettingProfilePicture.Image = Image.FromFile(profilePicturePath);
                                    picboxSettingProfilePicture.SizeMode = PictureBoxSizeMode.Zoom;
                                    btnAccount.Image = Image.FromFile(profilePicturePath);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading profile picture: " + ex.Message);
            }
        }
        private void btnSettingChangePhoto_Click(object sender, EventArgs e)
        {
            pnlChangePhoto.Visible = true;
            pnlChangeUsername.Visible = false;
            pnlChangePassword.Visible = false;
        }

        private void btnExitChangePhotoPanel_Click(object sender, EventArgs e)
        {
            pnlChangePhoto.Visible = false;
        }

        private void btnOpenUploadAuthenticationPhoto_Click(object sender, EventArgs e)
        {
            pnlSettingAuthenticationPhoto.Visible = true;
        }

        private void btnCloseUploadAuthenticationPhoto_Click(object sender, EventArgs e)
        {
            pnlSettingAuthenticationPhoto.Visible = false;
        }

        private void btnUploadAuthenticationPhoto_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    AuthenticationPhoto = ofd.FileName;
                    picboxAuthenticationPhoto.Image = Image.FromFile(AuthenticationPhoto);
                    picboxAuthenticationPhoto.SizeMode = PictureBoxSizeMode.Zoom;
                    btnSubmitAuthenticationPhoto.Enabled = true;
                }
            }
        }

        private void btnSubmitAuthenticationPhoto_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(AuthenticationPhoto))
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
                    string query = @"UPDATE user_credential 
                                     SET authentication_photo = @authentication_photo 
                                     WHERE username = @username";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        // Save the image to the designated folder
                        string fileName = Path.GetFileName(AuthenticationPhoto);
                        string destinationPath = Path.Combine(SaveAuthenticationPhoto, fileName);
                        File.Copy(AuthenticationPhoto, destinationPath, true);
                        cmd.Parameters.AddWithValue("@authentication_photo", destinationPath);
                        cmd.Parameters.AddWithValue("@username", StudentUsername);
                        cmd.ExecuteNonQuery();
                    }
                    InitializeAuthenticationSaveDirectory();
                    MessageBox.Show("Profile picture updated successfully.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error updating profile picture: " + ex.Message);
            }
        }
        private void InitializeAuthenticationSaveDirectory()
        {
            // Create a "Images" folder inside the solution directory
            string solutionDirectory = AppDomain.CurrentDomain.BaseDirectory;
            SaveAuthenticationPhoto = Path.Combine(solutionDirectory, "StudentAuthenticationPhoto");

            if (!Directory.Exists(SaveAuthenticationPhoto))
            {
                Directory.CreateDirectory(SaveAuthenticationPhoto);
            }
        }
    }
}
