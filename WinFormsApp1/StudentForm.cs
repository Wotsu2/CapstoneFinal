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

        private System.Windows.Forms.Timer assessmentsRefreshTimer;

        private Guna.UI2.WinForms.Guna2Panel assessmentsPanel;
        private FlowLayoutPanel assessmentsList;

        public StudentForm(int UserId, string Section, string Username)
        {
            InitializeComponent();
            StudentSection = Section;
            userId = UserId.ToString();
            StudentUsername = Username;

            initializeShowReminderForm();
        }

        private void StudentForm_Load(object sender, EventArgs e)
        {
            this.Show();
            this.Refresh();
            Application.DoEvents();

            if (string.IsNullOrEmpty(Isauthentication_photoEmpty))
            {
                FacialRecognitionReminderForm reminderForm =
                    new FacialRecognitionReminderForm(int.Parse(userId), StudentUsername);
                reminderForm.ShowDialog();
            }

            isSharingScreen = true;
            lblProfUsername.Text = StudentUsername;

            // ✅ Kick off all networking on background threads so the UI never blocks
            Task.Run(() => ConnectToServer());
            Task.Run(() => StartScreenShare());
            Task.Run(() => ConnectBroadcastReceiver());
            Task.Run(() => StartListening());

            // DB + I/O calls (these are fast, keep them on the UI thread)
            InitializeCreateButtonActivity();
            InitializeDataGridViewActivities();
            NameGet();
            InitializeDataGridViewGrades();
            LoadJoinedClasses();

            InitializeSaveDirectory();
            InitializeChangingPicture();
            InitializeAuthenticationSaveDirectory();

            InitializeAssessmentsCard();

            lblStudentName.Text = studentname;

            pnlHome.BringToFront();
            pnlHome.Visible = true;
            pnlHome.Refresh();

            this.Refresh();
            Application.DoEvents();
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
                                if (!reader.IsDBNull(reader.GetOrdinal("authentication_photo")))
                                    Isauthentication_photoEmpty = reader.GetString("authentication_photo");
                                else
                                    Isauthentication_photoEmpty = "";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("initializeShowReminderForm error: " + ex.Message);
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

        // =========================================================
        // RECONNECTING TCP CLIENT HELPER
        // =========================================================

        private async Task RunClientForever(
            string name,
            Func<TcpClient> connect,
            Func<TcpClient, Task> onConnected,
            int retryDelayMs = 2000)
        {
            while (!isSignedOut)
            {
                TcpClient c = null;
                try
                {
                    c = connect();

                    if (c != null && c.Connected)
                    {
                        Console.WriteLine($"[{name}] connected");
                        await onConnected(c);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{name}] error: {ex.Message}");
                }
                finally
                {
                    try { c?.Close(); } catch { }
                    try { c?.Dispose(); } catch { }
                }

                if (isSignedOut) break;

                try { await Task.Delay(retryDelayMs); } catch { }
            }
        }

        private void ConnectToServer()
        {
            _ = RunClientForever(
                "Workstation",
                () =>
                {
                    var c = new TcpClient();
                    c.Connect(SettingsManager.Current.ServerIp, SettingsManager.Current.WorkstationPort);
                    return c;
                },
                async c =>
                {
                    while (!isSignedOut && c.Connected)
                    {
                        try { await Task.Delay(2000); }
                        catch { break; }
                    }
                });
        }

        private void StartScreenShare()
        {
            _ = RunClientForever(
                "ScreenShare",
                () =>
                {
                    var c = new TcpClient();
                    c.Connect(SettingsManager.Current.ServerIp, SettingsManager.Current.ScreenSharePort);
                    return c;
                },
                async c =>
                {
                    while (!isSignedOut && c.Connected)
                    {
                        try
                        {
                            Bitmap shot = CaptureScreen();

                            using (MemoryStream ms = new MemoryStream())
                            {
                                shot.Save(ms, ImageFormat.Jpeg);
                                byte[] imageBytes = ms.ToArray();

                                NetworkStream stream = c.GetStream();
                                byte[] lengthPrefix = BitConverter.GetBytes(imageBytes.Length);

                                await stream.WriteAsync(lengthPrefix, 0, lengthPrefix.Length);
                                await stream.WriteAsync(imageBytes, 0, imageBytes.Length);
                            }

                            shot.Dispose();

                            await Task.Delay(500);
                        }
                        catch
                        {
                            break;
                        }
                    }
                });
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

        private void ConnectBroadcastReceiver()
        {
            _ = RunClientForever(
                "Broadcast",
                () =>
                {
                    var c = new TcpClient();
                    c.Connect(SettingsManager.Current.ServerIp, SettingsManager.Current.BroadcastPort);
                    return c;
                },
                async c =>
                {
                    NetworkStream stream = c.GetStream();

                    try
                    {
                        while (!isSignedOut && c.Connected)
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

                                if (this.IsHandleCreated)
                                    this.Invoke(new Action(() => ShowBroadcastFrame(frame)));
                            }
                        }
                    }
                    catch
                    {
                    }
                    finally
                    {
                        try
                        {
                            if (this.IsHandleCreated)
                                this.Invoke(new Action(() =>
                                {
                                    if (broadcastViewer != null && !broadcastViewer.IsDisposed)
                                    {
                                        broadcastViewer.Close();
                                        broadcastViewer = null;
                                    }
                                }));
                        }
                        catch { }
                    }
                });
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

        private async void StartListening()
        {
            try
            {
                Shutdownlistener = new TcpListener(IPAddress.Any, SettingsManager.Current.CommandPort);
                Shutdownlistener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                Shutdownlistener.Start();

                System.Threading.Thread t = new System.Threading.Thread(ListenForCommands);
                t.IsBackground = true;
                t.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine("StartListening error: " + ex.Message);
            }
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
                        client.Close();
                        System.Threading.Thread.Sleep(1000);
                        System.Diagnostics.Process.Start("shutdown", "/s /f /t 0");
                    }
                    else if (command == "RESTART")
                    {
                        client.Close();
                        System.Threading.Thread.Sleep(1000);
                        System.Diagnostics.Process.Start("shutdown", "/r /f /t 0");
                    }

                    client.Close();
                }
                catch
                {
                    break;
                }
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
                            while (reader.Read())
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
                Console.WriteLine("InitializeCreateButtonActivity error: " + ex.Message);
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
                Console.WriteLine("InitializeHomeActivityButton error: " + ex.Message);
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

                        if (dt.Rows.Count > 0)
                        {
                            activityId = dt.Rows[0]["activity_id"].ToString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("InitializeDataGridViewActivities error: " + ex.Message);
            }
        }

        private void dgvStudentActivities_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                if (e.RowIndex < 0) return;

                DataGridViewRow row = dgvStudentActivities.Rows[e.RowIndex];

                string activityId = GetSafeValue(row, "activity_id");
                string Title = GetSafeValue(row, "title");
                string DueDate = GetSafeValue(row, "due_date");
                string ActivityStatus = GetSafeValue(row, "activity_status");
                string Description = GetSafeValue(row, "description");
                string profId = GetSafeValue(row, "professor_id");
                string className = GetSafeValue(row, "activity_subject");

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
                Console.WriteLine("FetchActivityPdf error: " + ex.Message);
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
                Console.WriteLine("NameGet error: " + ex.Message);
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
                Console.WriteLine("InitializeDataGridViewGrades error: " + ex.Message);
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
                Console.WriteLine("btnEnterClass_Click error: " + ex.Message);
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

                    InitializeCreadeClass(className, classSection, classTime, classDate);

                    MessageBox.Show("Successfully Joined Class!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("InitializeJoinClass error: " + ex.Message);
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

                UnjoinClass(classname, classSection, classTime, classDate);

                flpSubjectClass.Controls.Remove(ClassButton);
                ClassButton.Dispose();
            };
            menu.Items.Add(unjoinItem);
            ClassButton.ContextMenuStrip = menu;

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
                Console.WriteLine("LoadJoinedClasses error: " + ex.Message);
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
                Console.WriteLine("UnjoinClass error: " + ex.Message);
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
                    string query = @"SELECT profile_picture FROM user_credential WHERE username = @username";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@username", StudentUsername);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                if (reader.IsDBNull(reader.GetOrdinal("profile_picture")))
                                    return;

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
                Console.WriteLine("InitializeChangingPicture error: " + ex.Message);
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
                Console.WriteLine("btnSubmitAuthenticationPhoto_Click error: " + ex.Message);
            }
        }

        private void InitializeAuthenticationSaveDirectory()
        {
            string solutionDirectory = AppDomain.CurrentDomain.BaseDirectory;
            SaveAuthenticationPhoto = Path.Combine(solutionDirectory, "StudentAuthenticationPhoto");

            if (!Directory.Exists(SaveAuthenticationPhoto))
            {
                Directory.CreateDirectory(SaveAuthenticationPhoto);
            }
        }

        // =========================================================
        // Assessment quiz exam
        // =========================================================

        private void InitializeAssessmentsCard()
        {
            assessmentsPanel = new Guna.UI2.WinForms.Guna2Panel
            {
                Size = new Size(242, 150),
                Location = new Point(1109, 328),
                FillColor = Color.White,
                BackColor = Color.Transparent,
                BorderRadius = 12,
                BorderColor = Color.FromArgb(220, 220, 220),
                BorderThickness = 1,
                ShadowDecoration = { BorderRadius = 10, Enabled = true, Depth = 6, Color = Color.FromArgb(60, 0, 0, 0) },
                AutoScroll = false
            };

            var headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 42,
                BackColor = Color.Transparent
            };

            var icon = new Label
            {
                Text = "📋",
                Font = new Font("Segoe UI Emoji", 14F),
                Location = new Point(12, 8),
                AutoSize = true
            };

            var title = new Label
            {
                Text = "Assessments",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 30, 30),
                Location = new Point(42, 10),
                AutoSize = true
            };

            headerPanel.Controls.Add(icon);
            headerPanel.Controls.Add(title);

            assessmentsList = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(12, 0, 12, 12),
                BackColor = Color.Transparent
            };

            assessmentsPanel.Controls.Add(assessmentsList);
            assessmentsPanel.Controls.Add(headerPanel);

            pnlHome.Controls.Add(assessmentsPanel);
            assessmentsPanel.BringToFront();

            LoadAssessments();

            assessmentsRefreshTimer = new System.Windows.Forms.Timer { Interval = 30000 };
            assessmentsRefreshTimer.Tick += (s, e) => LoadAssessments();
            assessmentsRefreshTimer.Start();
        }

        private void LoadAssessments()
        {
            assessmentsList.Controls.Clear();
            assessmentsList.PerformLayout();

            string connStr = SettingsManager.Current.GetConnectionString();

            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    string query = @"
                SELECT q.quiz_id,
                       q.quiz_title,
                       q.subject,
                       q.assessment_type,
                       q.exam_period,
                       q.created_at
                FROM quizzes q
                ORDER BY q.created_at DESC";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            bool any = false;

                            while (reader.Read())
                            {
                                any = true;

                                int quizId = reader.GetInt32("quiz_id");
                                string title = reader["quiz_title"].ToString();
                                string subject = reader["subject"]?.ToString() ?? "";
                                string type = reader["assessment_type"]?.ToString() ?? "quiz";
                                string period = reader["exam_period"]?.ToString() ?? "";

                                bool submitted = HasSubmitted(quizId);

                                AddAssessmentRow(quizId, title, subject, type, period, submitted);
                            }

                            if (!any)
                            {
                                var empty = new Label
                                {
                                    Text = "No assessments yet.",
                                    Font = new Font("Segoe UI", 9F, FontStyle.Italic),
                                    ForeColor = Color.Gray,
                                    AutoSize = true,
                                    Margin = new Padding(8, 12, 0, 0)
                                };
                                assessmentsList.Controls.Add(empty);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("LoadAssessments error: " + ex.Message);
            }
        }

        private void AddAssessmentRow(int quizId, string title, string subject,
                              string type, string period, bool submitted)
        {
            int rowWidth = Math.Max(180, assessmentsList.ClientSize.Width - 30);

            var row = new Panel
            {
                Width = rowWidth,
                Height = 32,
                Margin = new Padding(0, 4, 0, 4),
                BackColor = Color.Transparent,
                Cursor = Cursors.Default
            };

            var rowIcon = new Label
            {
                Text = type.Equals("exam", StringComparison.OrdinalIgnoreCase) ? "📝" : "📄",
                Font = new Font("Segoe UI Emoji", 11F),
                ForeColor = submitted ? Color.FromArgb(46, 204, 113) : Color.FromArgb(231, 76, 60),
                Location = new Point(0, 4),
                AutoSize = true
            };

            var rowTitle = new Label
            {
                Text = string.IsNullOrEmpty(subject) ? title : $"{subject} {title}",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = submitted ? Color.FromArgb(46, 204, 113) : Color.FromArgb(231, 76, 60),
                Location = new Point(28, 5),
                AutoSize = true
            };

            var badge = new Label
            {
                Text = submitted ? "✔" : "✖",
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = submitted ? Color.FromArgb(46, 204, 113) : Color.FromArgb(231, 76, 60),
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(24, 24),
                Location = new Point(rowWidth - 30, 4)
            };
            badge.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                using (var path = new System.Drawing.Drawing2D.GraphicsPath())
                {
                    path.AddEllipse(0, 0, badge.Width - 1, badge.Height - 1);
                    badge.Region = new Region(path);
                }
            };

            if (!submitted)
            {
                EventHandler onClick = (s, e) => OpenAssessment(quizId, title, type);
                row.Click += onClick;
                rowIcon.Click += onClick;
                rowTitle.Click += onClick;
                badge.Click += onClick;

                row.Cursor = Cursors.Hand;
                rowIcon.Cursor = Cursors.Hand;
                rowTitle.Cursor = Cursors.Hand;
                badge.Cursor = Cursors.Hand;
            }
            else
            {
                row.Cursor = Cursors.Default;
                rowIcon.Cursor = Cursors.Default;
                rowTitle.Cursor = Cursors.Default;
                badge.Cursor = Cursors.Default;
            }

            row.Controls.Add(rowIcon);
            row.Controls.Add(rowTitle);
            row.Controls.Add(badge);

            assessmentsList.Controls.Add(row);
        }

        private void OpenAssessment(int quizId, string title, string type)
        {
            if (HasSubmitted(quizId))
            {
                LoadAssessments();
                return;
            }

            try
            {
                Console.WriteLine($"[OpenAssessment] userId={userId}, quizId={quizId}");

                StudentQuizForm QuizForm = new StudentQuizForm(int.Parse(userId), quizId);
                QuizForm.ShowDialog();

                LoadAssessments();
            }
            catch (Exception ex)
            {
                Console.WriteLine("OpenAssessment error: " + ex.Message);
            }
        }

        private bool HasSubmitted(int quizId)
        {
            string connStr = SettingsManager.Current.GetConnectionString();
            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string q = @"SELECT COUNT(*) FROM quiz_attempts
                         WHERE quiz_id = @quiz_id AND user_id = @user_id";
                    using (var cmd = new MySqlCommand(q, conn))
                    {
                        cmd.Parameters.AddWithValue("@quiz_id", quizId);
                        cmd.Parameters.AddWithValue("@user_id", userId);

                        object result = cmd.ExecuteScalar();
                        if (result == null || result == DBNull.Value)
                            return false;

                        return Convert.ToInt32(result) > 0;
                    }
                }
            }
            catch { return false; }
        }
    }
}