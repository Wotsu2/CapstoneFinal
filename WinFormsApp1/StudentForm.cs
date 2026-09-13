using Guna.UI2.WinForms;
using MySql.Data.MySqlClient;
using Org.BouncyCastle.Asn1.Cmp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Windows.Forms;

namespace WinFormsApp1
{
    public partial class StudentForm : Form
    {
        private TcpClient client;
        private System.Windows.Forms.Timer screenShareTimer;
        private TcpClient screenClient;
        private bool isSharingScreen = false;
        private string serverIp = "192.168.100.4"; //Should be Empty and configure it to setting

        private TcpClient broadcastClient;
        private TcpListener Shutdownlistener;
        private BroadcastViewerForm broadcastViewer;
        private string StudentSection;
        private string userId;


        //Grades//
        private string selectedGradeCategory = "";
        private string selectedActivitiesCategory = "";
        private string file_path;
        private string studentname;
        private string activitySubject;

        public StudentForm(int UserId, string Section)
        {
            InitializeComponent();
            StudentSection = Section;
            userId = UserId.ToString();
        }
        private void StudentForm_Load(object sender, EventArgs e)
        {
            ConnectToServer(serverIp);
            StartScreenShare(serverIp);
            ConnectBroadcastReceiver(serverIp);
            StartListening();

            //Home Caller//
            InitializeCreateButtonActivity();

            //Activitiy Caller//
            InitializeDataGridViewActivities();
            getActivityPath();
            NameGet();

            //Grades Caller//
            InitializeDataGridViewGrades();
        }

        private Guna2Button activeMenuButton;

        private void SetActiveMenuButton(Guna2Button clickedBtn, Panel panelToShow)
        {
            // i-reset lahat ng buttons pabalik sa maroon (transparent) background
            foreach (var b in new[] { btnHome, btnActivities, btnSubject, btnGrades, btnFile, btnApps })
            {
                b.FillColor = Color.Transparent;
                b.ForeColor = Color.Firebrick;
            }

            // gawing "active" yung kaka-click lang
            clickedBtn.FillColor = Color.Firebrick;
            clickedBtn.ForeColor = Color.FromArgb(80, 12, 24); // maroon text sa puting background
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

        private void btnFile_Click(object sender, EventArgs e)
        {
            SetActiveMenuButton(btnFile, pnlFile);
            lblhometitle.Text = "File";
        }

        private void btnApps_Click(object sender, EventArgs e)
        {

        }

        //Connect the Client to the Server//
        private async void ConnectToServer(string serverIp)
        {
            try
            {
                client = new TcpClient();
                await client.ConnectAsync(serverIp, 5000); // use the SERVER's actual IP here And Should be Empty and configure it to setting

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
                while (client.Connected)
                {
                    await Task.Delay(2000); // just idle — connection itself signals "online"
                }
            }
            catch { }
        }

        //Share the Screen of the Client to the Server//

        private void StartScreenShare(string serverIp)
        {
            try
            {
                screenClient = new TcpClient();
                screenClient.Connect(serverIp, 5002); // dedicated screen-share port

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
        private async Task ConnectBroadcastReceiver(string serverIp)
        {
            try
            {
                broadcastClient = new TcpClient();
                await broadcastClient.ConnectAsync(serverIp, 5005);

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
                while (true)
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
            Shutdownlistener = new TcpListener(IPAddress.Any, 8888);
            Shutdownlistener.Start();

            System.Threading.Thread t = new System.Threading.Thread(ListenForCommands);
            t.IsBackground = true;
            t.Start();
        }

        private void ListenForCommands()
        {
            while (true)
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
        private static int CountTotalActivities(string StudentSection)
        {
            
            string connStr = $"Server=192.168.100.4;Port=3306;Database=cdsga_hub;Uid=root;Pwd=;";
            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = "SELECT COUNT(*) FROM professor_activity WHERE section = @section";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@section", StudentSection);
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
        private void InitializeCreateButtonActivity()
        {
            int totalClasses = CountTotalActivities(StudentSection);
            MessageBox.Show($"{totalClasses}");
            string connStr = "Server=192.168.100.4;Port=3306;Database=cdsga_hub;Uid=root;Pwd=;";
            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = "SELECT title, start_time, due_date, activity_status FROM professor_activity WHERE section = @section";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@section", StudentSection);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string title = reader.GetString("title");
                                string start_time = reader.GetString("start_time");
                                string due_date = reader.GetString("due_date");
                                string activity_status = reader.GetString("activity_status");

                                for (int i = 0; i < totalClasses; i++)
                                {
                                    Guna.UI2.WinForms.Guna2Button ActivityButton = new Guna.UI2.WinForms.Guna2Button();
                                    ActivityButton.Height = 180;
                                    ActivityButton.Width = 180;
                                    ActivityButton.Margin = new Padding(5);
                                    ActivityButton.BorderColor = Color.Black;
                                    ActivityButton.BorderThickness = 1;

                                    Label Title = new Label();
                                    Title.Text = title;
                                    Title.ForeColor = Color.Black;
                                    Title.BackColor = Color.Green;
                                    Title.Location = new Point(60, 50);
                                    ActivityButton.Controls.Add(Title);

                                    Label StartTime = new Label();
                                    StartTime.Text = start_time;
                                    StartTime.ForeColor = Color.Black;
                                    StartTime.BackColor = Color.Transparent;
                                    StartTime.Location = new Point(180, 0);
                                    //ActivityButton.Controls.Add(StartTime);

                                    Label DueDate = new Label();
                                    DueDate.Text = due_date;
                                    DueDate.ForeColor = Color.Black;
                                    DueDate.BackColor = Color.Violet;
                                    DueDate.Location = new Point(60, 0);
                                    ActivityButton.Controls.Add(DueDate);

                                    Label Status = new Label();
                                    Status.Text = activity_status;
                                    Status.ForeColor = Color.Black;
                                    Status.BackColor = Color.Transparent;
                                    Status.Location = new Point(0, 120);
                                    ActivityButton.Controls.Add(Status);


                                    flpPendingActivities.Controls.Add(ActivityButton);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        //Activities//
        private void InitializeDataGridViewActivities()
        {
            string connStr = "Server=192.168.100.4;Port=3306;Database=cdsga_hub;Uid=root;Pwd=;";

            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = "SELECT title, start_time, due_date, activity_status FROM professor_activity WHERE section = @section";
                    if (!string.IsNullOrEmpty(selectedActivitiesCategory))
                    {
                        query += " AND activity_status = @activity_status";
                    }

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@section", StudentSection);

                        if (!string.IsNullOrEmpty(selectedActivitiesCategory))
                            cmd.Parameters.AddWithValue("@activity_status", selectedActivitiesCategory);

                        MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);

                        dgvStudentActivities.DataSource = dt;

                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading activities: " + ex.Message);
            }
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
        private void getActivityPath()
        {
            string connStr = "Server=192.168.100.4;Port=3306;Database=cdsga_hub;Uid=root;Pwd=;";

            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = "SELECT activity_subject, file_path FROM professor_activity WHERE section = @section";


                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@section", StudentSection);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                file_path = reader.GetString("file_path");
                                activitySubject = reader.GetString("activity_subject");

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
        private void NameGet()
        {
            string connStr = "Server=192.168.100.4;Port=3306;Database=cdsga_hub;Uid=root;Pwd=;";
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
        private void dgvStudentActivities_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {

            MessageBox.Show($"Row {e.RowIndex} double-clicked.");
            if (e.RowIndex >= 0)
            {
                DataGridViewRow row = dgvStudentActivities.Rows[e.RowIndex];
                string Title = GetSafeValue(row, "title");
                string StartTime = GetSafeValue(row, "start_time");
                string DueDate = GetSafeValue(row, "due_date");
                string ActivityStatus = GetSafeValue(row, "activity_status");
                string Description = GetSafeValue(row, "description");
                string profId = GetSafeValue(row, "prof_id");

                ActivityForm activityForm = new ActivityForm(profId, userId, studentname, Title, DueDate, Description, StudentSection, activitySubject, ActivityStatus, file_path);

                activityForm.Show();
            }
        }

        private string GetSafeValue(DataGridViewRow row, string columnName)
        {
            object raw = row.Cells[columnName].Value;
            return (raw == null || raw == DBNull.Value) ? "" : raw.ToString();
        }

        //Grades//

        private void InitializeDataGridViewGrades()
        {
            string connStr = "Server=192.168.100.4;Port=3306;Database=cdsga_hub;Uid=root;Pwd=;";

            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = "SELECT prof_id title, start_time, due_date, activity_status, score FROM submitted_activity WHERE user_id = @user_id";

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

        
    }
}
