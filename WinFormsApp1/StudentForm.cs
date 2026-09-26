using Guna.UI2.WinForms;
using Microsoft.VisualBasic;
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
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

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

        private bool _assessmentsInitialized = false;
        private bool _reminderShown = false;

        // Banner slideshow
        private System.Windows.Forms.Timer slideshowTimer;
        private List<Image> slideshowImages = new List<Image>();
        private int slideshowIndex = 0;
        private FileSystemWatcher bannersWatcher;

        public StudentForm(int UserId, string Section, string Username)
        {
            InitializeComponent();
            StudentSection = Section;
            userId = UserId.ToString();
            StudentUsername = Username;

            initializeShowReminderForm();

            this.Shown += StudentForm_Shown;
        }

        private void StudentForm_Load(object sender, EventArgs e)
        {
            try
            {
                isSharingScreen = true;
                lblProfUsername.Text = StudentUsername;

                Task.Run(() => ConnectToServer());
                Task.Run(() => StartScreenShare());
                Task.Run(() => ConnectBroadcastReceiver());
                Task.Run(() => StartListening());

                NameGet();
                InitializeSaveDirectory();
                InitializeChangingPicture();

                try { InitializeCreateButtonActivity(); }
                catch (Exception ex) { Console.WriteLine("InitCreateButton: " + ex.Message); }

                try { InitializeDataGridViewActivities(); }
                catch (Exception ex) { Console.WriteLine("InitDgvActivities: " + ex.Message); }

                try { InitializeDataGridViewGrades(); }
                catch (Exception ex) { Console.WriteLine("InitDgvGrades: " + ex.Message); }

                try { LoadJoinedClasses(); }
                catch (Exception ex) { Console.WriteLine("LoadJoinedClasses: " + ex.Message); }

                try { InitializeQuizExam(); }
                catch (Exception ex) { Console.WriteLine("InitQuizExam: " + ex.Message); }

                lblStudentName.Text = studentname;

                pnlHome.Visible = true;
                pnlHome.BringToFront();

                flpPendingActivities.AutoScroll = true;
                flpPendingActivities.WrapContents = true;
                flpPendingActivities.FlowDirection = FlowDirection.LeftToRight;
                flpPendingActivities.PerformLayout();

                // ✅ Start the banner slideshow
                StartSlideshow();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "StudentForm failed to load:\n\n" + ex.Message + "\n\n" + ex.StackTrace,
                    "Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void StudentForm_Shown(object sender, EventArgs e)
        {
            if (!_reminderShown)
            {
                _reminderShown = true;
                if (string.IsNullOrEmpty(Isauthentication_photoEmpty))
                {
                    try
                    {
                        var reminder = new FacialRecognitionReminderForm(int.Parse(userId), StudentUsername);
                        reminder.ShowDialog(this);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Reminder form error: " + ex.Message);
                    }
                }
            }

            if (!_assessmentsInitialized)
            {
                _assessmentsInitialized = true;
                try { InitializeAssessmentsCard(); }
                catch (Exception ex) { Console.WriteLine("InitializeAssessmentsCard error: " + ex.Message); }
            }
        }

        private void initializeShowReminderForm()
        {
            string connStr = SettingsManager.Current.GetConnectionString();
            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand("SELECT authentication_photo FROM user_credential WHERE username = @username", conn))
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

        // =========================================================
        // BANNER SLIDESHOW
        // =========================================================

        private void StartSlideshow()
        {
            try
            {
                slideshowImages = BannerHelper.LoadBannerImages();

                if (slideshowImages.Count == 0)
                {
                    Console.WriteLine("[Slideshow] No banners found yet.");
                    return;
                }

                slideshowIndex = 0;
                pictureboxBanner.Image = slideshowImages[0];
                pictureboxBanner.SizeMode = PictureBoxSizeMode.StretchImage;

                slideshowTimer = new System.Windows.Forms.Timer();
                slideshowTimer.Interval = 3000;
                slideshowTimer.Tick += SlideshowTimer_Tick;
                slideshowTimer.Start();

                StartBannersWatcher();

                Console.WriteLine($"[Slideshow] Started with {slideshowImages.Count} image(s).");
            }
            catch (Exception ex)
            {
                Console.WriteLine("StartSlideshow error: " + ex.Message);
            }
        }

        private void SlideshowTimer_Tick(object sender, EventArgs e)
        {
            if (slideshowImages.Count == 0) return;

            slideshowIndex++;
            if (slideshowIndex >= slideshowImages.Count)
                slideshowIndex = 0;

            pictureboxBanner.Image = slideshowImages[slideshowIndex];
        }

        private void StartBannersWatcher()
        {
            try
            {
                string folder = BannerHelper.GetBannersFolder(false);
                if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder)) return;

                bannersWatcher = new FileSystemWatcher(folder);
                bannersWatcher.Filter = "*.*";
                bannersWatcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite;
                bannersWatcher.EnableRaisingEvents = true;

                FileSystemEventHandler reload = (s, e) =>
                {
                    System.Threading.Thread.Sleep(500);
                    if (this.IsHandleCreated && !this.IsDisposed)
                        this.BeginInvoke(new Action(ReloadBanners));
                };

                bannersWatcher.Created += reload;
                bannersWatcher.Deleted += reload;
                bannersWatcher.Renamed += (s, e) => reload(s, e);
                bannersWatcher.Changed += reload;
            }
            catch (Exception ex)
            {
                Console.WriteLine("StartBannersWatcher error: " + ex.Message);
            }
        }

        private void ReloadBanners()
        {
            try
            {
                foreach (var img in slideshowImages) try { img.Dispose(); } catch { }
                slideshowImages = BannerHelper.LoadBannerImages();

                if (slideshowImages.Count == 0)
                {
                    pictureboxBanner.Image = null;
                    return;
                }

                if (slideshowIndex >= slideshowImages.Count) slideshowIndex = 0;

                pictureboxBanner.Image = slideshowImages[slideshowIndex];
            }
            catch (Exception ex)
            {
                Console.WriteLine("ReloadBanners error: " + ex.Message);
            }
        }

        // =========================================================
        // NAVIGATION
        // =========================================================

        private void btnHome_Click(object sender, EventArgs e)
        {
            pnlHome.Visible = true;
            pnlActivity.Visible = false;
            pnlSubject.Visible = false;
            pnlGrades.Visible = false;
            pnlSetting.Visible = false;
            pnlQuizExam.Visible = false;
            lblhometitle.Text = "Home";

            try { LoadAssessments(); } catch { }
        }

        private void btnActivities_Click(object sender, EventArgs e)
        {
            pnlHome.Visible = false;
            pnlActivity.Visible = true;
            pnlSubject.Visible = false;
            pnlGrades.Visible = false;
            pnlSetting.Visible = false;
            pnlQuizExam.Visible = false;
            lblhometitle.Text = "Activity";
        }

        private void btnSubject_Click(object sender, EventArgs e)
        {
            pnlHome.Visible = false;
            pnlActivity.Visible = false;
            pnlSubject.Visible = true;
            pnlGrades.Visible = false;
            pnlSetting.Visible = false;
            pnlQuizExam.Visible = false;
            lblhometitle.Text = "Subject";
        }

        private void btnGrades_Click(object sender, EventArgs e)
        {
            pnlHome.Visible = false;
            pnlActivity.Visible = false;
            pnlSubject.Visible = false;
            pnlGrades.Visible = true;
            pnlSetting.Visible = false;
            pnlQuizExam.Visible = false;
            lblhometitle.Text = "Grade";
        }

        private void btnAccount_Click(object sender, EventArgs e)
        {
            pnlHome.Visible = false;
            pnlActivity.Visible = false;
            pnlSubject.Visible = false;
            pnlGrades.Visible = false;
            pnlSetting.Visible = true;
            lblhometitle.Text = "Settings";
            pnlQuizExam.Visible = false;
        }

        private void btnQuizExam_Click(object sender, EventArgs e)
        {
            pnlHome.Visible = false;
            pnlActivity.Visible = false;
            pnlSubject.Visible = false;
            pnlGrades.Visible = false;
            pnlQuizExam.Visible = true;
            InitializeQuizExam();
        }

        // =========================================================
        // TCP CLIENT HELPERS
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
                    NetworkStream stream = c.GetStream();

                    while (!isSignedOut)
                    {
                        try
                        {
                            await Task.Delay(2000);

                            if (c.Client.Poll(0, SelectMode.SelectRead) && c.Client.Available == 0)
                            {
                                Console.WriteLine("[Workstation] server closed — reconnecting");
                                break;
                            }

                            try { stream.Write(new byte[0], 0, 0); }
                            catch { break; }
                        }
                        catch
                        {
                            break;
                        }
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
                g.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);

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
                    catch { }
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

        private void StartListening()
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
            while (isSharingScreen && !isSignedOut)
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

        // =========================================================
        // HOME — Activities
        // =========================================================

        private void InitializeCreateButtonActivity()
        {
            string connStr = SettingsManager.Current.GetConnectionString();

            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    Console.WriteLine("[Activities] Loading for section = '" + StudentSection + "'");

                    string query;
                    if (string.IsNullOrEmpty(StudentSection))
                    {
                        query = @"SELECT activity_id, title, start_time, due_date, activity_subject, activity_status 
                          FROM professor_activity";
                    }
                    else
                    {
                        query = @"SELECT activity_id, title, start_time, due_date, activity_subject, activity_status 
                          FROM professor_activity WHERE section = @section";
                    }

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        if (!string.IsNullOrEmpty(StudentSection))
                            cmd.Parameters.AddWithValue("@section", StudentSection);

                        int count = 0;

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                count++;

                                int activityId = reader.GetInt32("activity_id");
                                string title = reader.GetString("title");
                                string start_time = reader.GetString("start_time");
                                string due_date = reader.GetString("due_date");
                                string className = reader.IsDBNull(reader.GetOrdinal("activity_subject"))
                                    ? "" : reader.GetString("activity_subject");
                                string activity_status = reader.IsDBNull(reader.GetOrdinal("activity_status"))
                                    ? "" : reader.GetString("activity_status");

                                Guna.UI2.WinForms.Guna2Panel card = new Guna.UI2.WinForms.Guna2Panel();
                                card.Width = 260;
                                card.Height = 250;
                                card.Margin = new Padding(10);
                                card.FillColor = Color.White;
                                card.BorderColor = Color.FromArgb(66, 133, 244);
                                card.BorderThickness = 2;
                                card.BorderRadius = 14;
                                card.Cursor = Cursors.Hand;

                                int capturedId = activityId;
                                card.Click += (s, e) => InitializeHomeActivityButton(capturedId);

                                Guna.UI2.WinForms.Guna2Panel badge = new Guna.UI2.WinForms.Guna2Panel();
                                badge.Size = new Size(150, 34);
                                badge.Location = new Point(card.Width - 150, 0);
                                badge.FillColor = Color.FromArgb(199, 125, 226);
                                badge.BorderRadius = 0;
                                badge.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                                badge.Cursor = Cursors.Hand;

                                Label lblDue = new Label();
                                lblDue.Text = $"Due: {due_date}";
                                lblDue.ForeColor = Color.Black;
                                lblDue.BackColor = Color.Transparent;
                                lblDue.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
                                lblDue.AutoSize = false;
                                lblDue.TextAlign = ContentAlignment.MiddleCenter;
                                lblDue.Dock = DockStyle.Fill;
                                lblDue.Cursor = Cursors.Hand;
                                badge.Controls.Add(lblDue);
                                card.Controls.Add(badge);

                                Label lblSubject = new Label();
                                lblSubject.Text = className.ToUpper();
                                lblSubject.ForeColor = Color.Black;
                                lblSubject.BackColor = Color.Transparent;
                                lblSubject.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
                                lblSubject.AutoSize = false;
                                lblSubject.Size = new Size(card.Width - 30, 30);
                                lblSubject.Location = new Point(20, 70);
                                lblSubject.Cursor = Cursors.Hand;
                                card.Controls.Add(lblSubject);

                                Label lblTitle = new Label();
                                lblTitle.Text = title;
                                lblTitle.ForeColor = Color.FromArgb(30, 30, 30);
                                lblTitle.BackColor = Color.Transparent;
                                lblTitle.Font = new Font("Segoe UI", 12F, FontStyle.Regular);
                                lblTitle.AutoSize = false;
                                lblTitle.Size = new Size(card.Width - 30, 30);
                                lblTitle.Location = new Point(20, 115);
                                lblTitle.Cursor = Cursors.Hand;
                                card.Controls.Add(lblTitle);

                                Label lblStatus = new Label();
                                lblStatus.Text = activity_status;
                                lblStatus.ForeColor = Color.Gray;
                                lblStatus.BackColor = Color.Transparent;
                                lblStatus.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
                                lblStatus.AutoSize = true;
                                lblStatus.Location = new Point(20, 155);
                                lblStatus.Cursor = Cursors.Hand;
                                card.Controls.Add(lblStatus);

                                Label lblView = new Label();
                                lblView.Text = "View Activity   ›";
                                lblView.ForeColor = Color.FromArgb(139, 0, 0);
                                lblView.BackColor = Color.Transparent;
                                lblView.Font = new Font("Segoe UI", 11F, FontStyle.Regular);
                                lblView.AutoSize = true;
                                lblView.Location = new Point(card.Width - 145, card.Height - 45);
                                lblView.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
                                lblView.Cursor = Cursors.Hand;
                                card.Controls.Add(lblView);

                                EventHandler openActivity = (s, e) => InitializeHomeActivityButton(capturedId);
                                badge.Click += openActivity;
                                lblDue.Click += openActivity;
                                lblSubject.Click += openActivity;
                                lblTitle.Click += openActivity;
                                lblStatus.Click += openActivity;
                                lblView.Click += openActivity;

                                flpPendingActivities.Controls.Add(card);
                            }
                        }

                        Console.WriteLine($"[Activities] Loaded {count} activities.");
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

        // =========================================================
        // Activities
        // =========================================================

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
                        query += " AND activity_status = @activity_status";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@section", StudentSection);

                        if (!string.IsNullOrEmpty(selectedActivitiesCategory))
                            cmd.Parameters.AddWithValue("@activity_status", selectedActivitiesCategory);

                        MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);

                        dgvStudentActivities.DataSource = dt;

                        if (dgvStudentActivities.Columns.Contains("description"))
                            dgvStudentActivities.Columns["description"].Visible = false;

                        if (dgvStudentActivities.Columns.Contains("professor_id"))
                            dgvStudentActivities.Columns["professor_id"].Visible = false;

                        if (dt.Rows.Count > 0)
                            activityId = dt.Rows[0]["activity_id"].ToString();
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
                        query = @"SELECT activity_file, activity_filename FROM professor_activity WHERE activity_id = @activity_id";
                    else
                        query = @"SELECT activity_file, activity_filename FROM professor_activity 
                                  WHERE professor_id = @professor_id AND title = @title 
                                    AND section = @section AND activity_subject = @activity_subject LIMIT 1";

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
                            if (reader.IsDBNull(reader.GetOrdinal("activity_file"))) return null;

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
                    using (var cmd = new MySqlCommand("SELECT lastname, firstname, middlename FROM user_information WHERE user_id = @user_id", conn))
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

        // =========================================================
        // Grades
        // =========================================================

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
                        query += " AND activity_status = @activity_status";

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

        // =========================================================
        // Subject
        // =========================================================

        private void btnJoinClass_Click(object sender, EventArgs e) => pnlCreateClass.Visible = true;
        private void btnCloseJointClassPanel_Click(object sender, EventArgs e) => pnlCreateClass.Visible = false;

        private void btnEnterClass_Click(object sender, EventArgs e)
        {
            string txtcode = txtEnterCode.Text;
            string connStr = SettingsManager.Current.GetConnectionString();

            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand("SELECT professor_id, class_name, class_section, class_time, class_date FROM professor_class WHERE class_code = @class_code", conn))
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

                    using (var checkCmd = new MySqlCommand(@"SELECT COUNT(*) FROM student_class
                        WHERE professor_id = @professor_id AND user_id = @user_id
                        AND class_name = @class_name AND section = @section
                        AND class_time = @class_time AND class_date = @class_date", conn))
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

                    using (var cmd = new MySqlCommand(@"INSERT INTO student_class
                        (professor_id, user_id, class_name, section, class_time, class_date)
                        VALUES (@professor_id, @user_id, @class_name, @section, @class_time, @class_date)", conn))
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

        private void InitializeCreadeClass(string classname = "", string classSection = "",
                                   string classTime = "", string classDate = "")
        {
            string connStr = SettingsManager.Current.GetConnectionString();

            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    string query = @"
                SELECT sc.class_id, 
                       sc.class_name, 
                       sc.class_date, 
                       sc.class_time, 
                       sc.section,
                       ui.lastname, 
                       ui.firstname, 
                       ui.middlename
                FROM student_class sc
                LEFT JOIN user_information ui ON ui.user_id = sc.professor_id
                WHERE sc.user_id = @user_id";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@user_id", userId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string classId = reader["class_id"] != DBNull.Value ? reader["class_id"].ToString() : "";
                                string rClassName = reader["class_name"] != DBNull.Value ? reader["class_name"].ToString() : "";
                                string rClassDate = reader["class_date"] != DBNull.Value ? reader["class_date"].ToString() : "";
                                string rClassTime = reader["class_time"] != DBNull.Value ? reader["class_time"].ToString() : "";
                                string rClassSection = reader["section"] != DBNull.Value ? reader["section"].ToString() : "";

                                string profLast = reader["lastname"] != DBNull.Value ? reader["lastname"].ToString() : "";
                                string profFirst = reader["firstname"] != DBNull.Value ? reader["firstname"].ToString() : "";
                                string profMiddle = reader["middlename"] != DBNull.Value ? reader["middlename"].ToString() : "";

                                string profFullName = string.Join(" ",
                                    new[] { profLast, profFirst, profMiddle }
                                        .Where(s => !string.IsNullOrWhiteSpace(s)))
                                    .Trim();

                                if (string.IsNullOrEmpty(profFullName))
                                    profFullName = "Unknown Professor";

                                Panel cardPanel = new Panel
                                {
                                    Size = new Size(350, 250),
                                    BackColor = Color.White,
                                    BorderStyle = BorderStyle.FixedSingle,
                                    Margin = new Padding(10),
                                    Tag = classId
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
                                    Text = rClassName,
                                    Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                                    Location = new Point(20, 50),
                                    AutoSize = true
                                };
                                cardPanel.Controls.Add(lblTitle);

                                Label lblProfName = new Label
                                {
                                    Text = "Prof. " + profFullName,
                                    Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                                    Location = new Point(20, 100),
                                    AutoSize = true,
                                    ForeColor = Color.FromArgb(123, 15, 23)
                                };
                                cardPanel.Controls.Add(lblProfName);

                                Label lblDay = new Label
                                {
                                    Text = rClassDate,
                                    Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                                    Location = new Point(50, 150),
                                    AutoSize = true
                                };
                                cardPanel.Controls.Add(lblDay);

                                Label lblTime = new Label
                                {
                                    Text = rClassTime,
                                    Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                                    Location = new Point(50, 180),
                                    AutoSize = true
                                };
                                cardPanel.Controls.Add(lblTime);

                                Label lblSection = new Label
                                {
                                    Text = rClassSection,
                                    Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                                    Location = new Point(200, 210),
                                    AutoSize = true,
                                    TextAlign = ContentAlignment.MiddleRight
                                };
                                cardPanel.Controls.Add(lblSection);

                                ContextMenuStrip menu = new ContextMenuStrip();
                                lblMenu.Click += (s, e) =>
                                {
                                    DialogResult result = MessageBox.Show(
                                        $"Are you sure you want to unjoin '{rClassName}'?",
                                        "Confirm Unjoin",
                                        MessageBoxButtons.YesNo,
                                        MessageBoxIcon.Warning);

                                    if (result != DialogResult.Yes) return;

                                    UnjoinClass(rClassName, rClassSection, rClassTime, rClassDate);

                                    flpSubjectClass.Controls.Remove(cardPanel);
                                    cardPanel.Dispose();
                                };

                                cardPanel.ContextMenuStrip = menu;
                                lblMenu.ContextMenuStrip = menu;
                                lblTitle.ContextMenuStrip = menu;
                                lblProfName.ContextMenuStrip = menu;
                                lblSection.ContextMenuStrip = menu;
                                lblTime.ContextMenuStrip = menu;
                                lblDay.ContextMenuStrip = menu;

                                flpSubjectClass.Controls.Add(cardPanel);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load classes: " + ex.Message,
                                "Load Error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
                Console.WriteLine("InitializeCreadeClass error: " + ex);
            }
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
                    using (var cmd = new MySqlCommand(@"SELECT class_name, section, class_time, class_date FROM student_class WHERE user_id = @user_id", conn))
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
                    using (var cmd = new MySqlCommand(@"DELETE FROM student_class
                        WHERE user_id = @user_id AND class_name = @class_name
                        AND section = @section AND class_time = @class_time AND class_date = @class_date", conn))
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

        // =========================================================
        // Settings
        // =========================================================

        private void btnSettingProfileExpand_Click(object sender, EventArgs e)
        {
            if (pnlSettingProfile.Height <= 350)
                pnlSettingProfile.Height = 668;
            else if (pnlSettingProfile.Height >= 668)
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

            try { assessmentsRefreshTimer?.Stop(); } catch { }
            try { assessmentsRefreshTimer?.Dispose(); } catch { }
            assessmentsRefreshTimer = null;

            MessageBox.Show("Signed out successfully.");
        }

        private void Logout()
        {
            DialogResult result = MessageBox.Show("Are you sure you want to logout?",
                "Logout Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result != DialogResult.Yes) return;

            try { StopServer(); } catch { }
            try { ClearAllFormData(); } catch { }

            Login loginForm = null;
            foreach (Form f in Application.OpenForms)
            {
                if (f is Login && !f.IsDisposed)
                {
                    loginForm = (Login)f;
                    break;
                }
            }

            if (loginForm != null)
            {
                loginForm.Show();
                loginForm.BringToFront();
                loginForm.Activate();
                loginForm.WindowState = FormWindowState.Normal;
            }
            else
            {
                loginForm = new Login();
                loginForm.Show();
            }

            this.Hide();
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
                    using (var cmd = new MySqlCommand("UPDATE user_credential SET username = @new_username WHERE username = @current_username", conn))
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
                    using (var cmd = new MySqlCommand("UPDATE user_credential SET p_word = @new_password WHERE username = @current_username", conn))
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
                    using (var cmd = new MySqlCommand("UPDATE user_credential SET profile_picture = @profile_picture WHERE username = @username", conn))
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
                    using (var cmd = new MySqlCommand("SELECT profile_picture FROM user_credential WHERE username = @username", conn))
                    {
                        cmd.Parameters.AddWithValue("@username", StudentUsername);
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
        private void btnOpenUploadAuthenticationPhoto_Click(object sender, EventArgs e) => pnlSettingAuthenticationPhoto.Visible = true;
        private void btnCloseUploadAuthenticationPhoto_Click(object sender, EventArgs e) => pnlSettingAuthenticationPhoto.Visible = false;

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

        // =========================================================
        // AUTH PHOTO — SEND TO ADMIN SHARED FOLDER
        // =========================================================
        private async Task<bool> SendAuthenticationPhotoToAdmin(byte[] imageBytes, string fileName)
        {
            try
            {
                // Use the SAME ServerIp and FileTransferPort
                string adminIp = SettingsManager.Current.ServerIp;
                int adminPort = SettingsManager.Current.FileTransferPort;

                using (TcpClient client = new TcpClient())
                {
                    var connectTask = client.ConnectAsync(adminIp, adminPort);
                    var timeoutTask = Task.Delay(5000);
                    var completed = await Task.WhenAny(connectTask, timeoutTask);

                    if (completed == timeoutTask)
                    {
                        MessageBox.Show($"Admin ({adminIp}:{adminPort}) not reachable (timeout).");
                        return false;
                    }

                    await connectTask;

                    if (!client.Connected)
                    {
                        MessageBox.Show($"Admin ({adminIp}:{adminPort}) refused the connection.");
                        return false;
                    }

                    using (NetworkStream stream = client.GetStream())
                    using (BinaryWriter writer = new BinaryWriter(stream))
                    {
                        writer.Write(fileName);
                        writer.Write(imageBytes.Length);
                        writer.Write(imageBytes);
                        writer.Flush();
                    }
                }
                return true;
            }
            catch (SocketException sex)
            {
                MessageBox.Show($"Network error: {sex.SocketErrorCode}\n{sex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine("SendAuthenticationPhotoToAdmin error: " + ex.Message);
                MessageBox.Show("Failed to send photo to admin: " + ex.Message);
                return false;
            }
        }

        private async void btnSubmitAuthenticationPhoto_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(AuthenticationPhoto))
            {
                MessageBox.Show("Upload an image first!");
                return;
            }

            try
            {
                byte[] imageBytes = await File.ReadAllBytesAsync(AuthenticationPhoto);

                string ext = Path.GetExtension(AuthenticationPhoto);
                string fileName = $"{userId}_{StudentUsername}_{DateTime.Now:yyyyMMddHHmmss}{ext}";

                bool sent = await SendAuthenticationPhotoToAdmin(imageBytes, fileName);
                if (!sent) return;

                // Store the network path in DB using ServerIp + FileTransferPort isn't needed — 
                // just build the UNC/network path from ServerIp + SaveFolder + AuthPhotoSubfolder.
                // Since we only have the shared root name (SaveFolder's last segment), store the
                // logical path: \\ServerIp\<SharedFolderName>\AuthenticationPhotos\fileName
                string sharedFolderName = new DirectoryInfo(SettingsManager.Current.SaveFolder).Name;
                string uncPath = $@"\\{SettingsManager.Current.ServerIp}\{sharedFolderName}\{SettingsManager.Current.AuthPhotoSubfolder}\{fileName}";

                string connStr = SettingsManager.Current.GetConnectionString();
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand(
                        "UPDATE user_credential SET authentication_photo = @path WHERE username = @u", conn))
                    {
                        cmd.Parameters.AddWithValue("@path", uncPath);
                        cmd.Parameters.AddWithValue("@u", StudentUsername);
                        cmd.ExecuteNonQuery();
                    }
                }

                Isauthentication_photoEmpty = uncPath;

                MessageBox.Show("Authentication photo sent to admin successfully.");
                pnlSettingAuthenticationPhoto.Visible = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine("btnSubmitAuthenticationPhoto_Click error: " + ex.Message);
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        // =========================================================
        // Assessment quiz exam — THREE STATES
        // =========================================================

        private void InitializeAssessmentsCard()
        {
            if (assessmentsPanel != null) return;

            assessmentsPanel = new Guna.UI2.WinForms.Guna2Panel
            {
                Size = new Size(234, 150),
                Location = new Point(954, 315),
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
            if (assessmentsList == null) return;

            assessmentsList.Controls.Clear();
            assessmentsList.PerformLayout();

            string connStr = SettingsManager.Current.GetConnectionString();

            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    string query = @"SELECT q.quiz_id, q.quiz_title, q.subject, q.assessment_type,
                                            q.exam_period, q.created_at
                                     FROM quizzes q ORDER BY q.created_at DESC";

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

            string state = GetAssessmentState(quizId, submitted);

            Color iconColor;
            Color titleColor;
            Color badgeColor;
            string badgeText;
            bool clickable;

            if (state == "SUBMITTED")
            {
                iconColor = Color.FromArgb(46, 204, 113);
                titleColor = Color.FromArgb(46, 204, 113);
                badgeColor = Color.FromArgb(46, 204, 113);
                badgeText = "✔";
                clickable = false;
            }
            else if (state == "IN_PROGRESS")
            {
                iconColor = Color.FromArgb(243, 156, 18);
                titleColor = Color.FromArgb(243, 156, 18);
                badgeColor = Color.FromArgb(243, 156, 18);
                badgeText = "▶";
                clickable = true;
            }
            else
            {
                iconColor = Color.FromArgb(231, 76, 60);
                titleColor = Color.FromArgb(231, 76, 60);
                badgeColor = Color.FromArgb(231, 76, 60);
                badgeText = "✖";
                clickable = true;
            }

            var row = new Panel
            {
                Width = rowWidth,
                Height = 32,
                Margin = new Padding(0, 4, 0, 4),
                BackColor = Color.Transparent,
                Cursor = clickable ? Cursors.Hand : Cursors.Default
            };

            var rowIcon = new Label
            {
                Text = type.Equals("exam", StringComparison.OrdinalIgnoreCase) ? "📝" : "📄",
                Font = new Font("Segoe UI Emoji", 11F),
                ForeColor = iconColor,
                Location = new Point(0, 4),
                AutoSize = true,
                Cursor = row.Cursor
            };

            var rowTitle = new Label
            {
                Text = string.IsNullOrEmpty(subject) ? title : $"{subject} {title}",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = titleColor,
                Location = new Point(28, 5),
                AutoSize = true,
                Cursor = row.Cursor
            };

            var badge = new Label
            {
                Text = badgeText,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = badgeColor,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(24, 24),
                Location = new Point(rowWidth - 30, 4),
                Cursor = row.Cursor
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

            if (clickable)
            {
                EventHandler onClick = (s, e) => OpenAssessment(quizId, title, type);

                row.Click += onClick;
                rowIcon.Click += onClick;
                rowTitle.Click += onClick;
                badge.Click += onClick;
            }

            row.Controls.Add(rowIcon);
            row.Controls.Add(rowTitle);
            row.Controls.Add(badge);

            assessmentsList.Controls.Add(row);
        }

        private string GetAssessmentState(int quizId, bool submitted)
        {
            if (submitted) return "SUBMITTED";

            string connStr = SettingsManager.Current.GetConnectionString();

            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    using (var cmd = new MySqlCommand(
                        @"SELECT status
                          FROM quiz_attempts
                          WHERE quiz_id = @quiz_id
                            AND user_id = @user_id
                          ORDER BY attempt_id DESC
                          LIMIT 1",
                        conn))
                    {
                        cmd.Parameters.AddWithValue("@quiz_id", quizId);
                        cmd.Parameters.AddWithValue("@user_id", userId);

                        object result = cmd.ExecuteScalar();

                        if (result == null || result == DBNull.Value)
                            return "NOT_STARTED";

                        string status = result.ToString().ToUpper();

                        if (status == "SUBMITTED") return "SUBMITTED";
                        if (status == "TAKING") return "IN_PROGRESS";

                        return "NOT_STARTED";
                    }
                }
            }
            catch
            {
                return submitted ? "SUBMITTED" : "NOT_STARTED";
            }
        }

        private void OpenAssessment(int quizId, string title, string type)
        {
            try
            {
                Console.WriteLine($"[OpenAssessment] userId={userId}, quizId={quizId}");
                StudentQuizForm quizForm = new StudentQuizForm(int.Parse(userId), quizId);
                quizForm.ShowDialog();

                LoadAssessments();
            }
            catch (Exception ex)
            {
                Console.WriteLine("OpenAssessment error: " + ex.Message);
                MessageBox.Show("Unable to open the assessment:\n\n" + ex.Message,
                    "Open Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    using (var cmd = new MySqlCommand(
                        @"SELECT COUNT(*) FROM quiz_attempts
                          WHERE quiz_id = @quiz_id
                            AND user_id = @user_id
                            AND status = 'SUBMITTED'",
                        conn))
                    {
                        cmd.Parameters.AddWithValue("@quiz_id", quizId);
                        cmd.Parameters.AddWithValue("@user_id", userId);

                        object result = cmd.ExecuteScalar();
                        if (result == null || result == DBNull.Value) return false;
                        return Convert.ToInt32(result) > 0;
                    }
                }
            }
            catch { return false; }
        }

        // =========================================================
        // Q&A Security
        // =========================================================

        private void btnQandA_Click(object sender, EventArgs e)
        {
            pnlSettingQandA.Visible = true;
        }

        private void btnQandAClosePanel_Click(object sender, EventArgs e)
        {
            pnlSettingQandA.Visible = false;
        }

        private void btnSettingQandASave_Click(object sender, EventArgs e)
        {
            string firstAnswer = txtFirstAnswer.Text.Trim();
            string secondAnswer = txtSecondAnswer.Text.Trim();
            string thirdAnswer = txtThirdAnswer.Text.Trim();

            if (string.IsNullOrWhiteSpace(cmbFirstQuestion.Text) ||
                string.IsNullOrWhiteSpace(cmbSecondQuestion.Text) ||
                string.IsNullOrWhiteSpace(cmbThirdQuestion.Text) ||
                string.IsNullOrWhiteSpace(firstAnswer) ||
                string.IsNullOrWhiteSpace(secondAnswer) ||
                string.IsNullOrWhiteSpace(thirdAnswer))
            {
                MessageBox.Show("Please answer all three security questions.",
                    "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (cmbFirstQuestion.Text == cmbSecondQuestion.Text ||
                cmbFirstQuestion.Text == cmbThirdQuestion.Text ||
                cmbSecondQuestion.Text == cmbThirdQuestion.Text)
            {
                MessageBox.Show("Please choose three different questions.",
                    "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string connStr = SettingsManager.Current.GetConnectionString();

            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    using (var tx = conn.BeginTransaction())
                    {
                        const string query =
                            "INSERT INTO question_answer_security (username, question, answer) " +
                            "VALUES (@username, @question, @answer)";

                        using (var cmd = new MySqlCommand(query, conn, tx))
                        {
                            cmd.Parameters.Add("@username", MySqlDbType.VarChar);
                            cmd.Parameters.Add("@question", MySqlDbType.VarChar);
                            cmd.Parameters.Add("@answer", MySqlDbType.VarChar);

                            cmd.Parameters["@username"].Value = StudentUsername;
                            cmd.Parameters["@question"].Value = cmbFirstQuestion.Text;
                            cmd.Parameters["@answer"].Value = firstAnswer;
                            cmd.ExecuteNonQuery();

                            cmd.Parameters["@question"].Value = cmbSecondQuestion.Text;
                            cmd.Parameters["@answer"].Value = secondAnswer;
                            cmd.ExecuteNonQuery();

                            cmd.Parameters["@question"].Value = cmbThirdQuestion.Text;
                            cmd.Parameters["@answer"].Value = thirdAnswer;
                            cmd.ExecuteNonQuery();
                        }

                        tx.Commit();
                    }
                }

                MessageBox.Show("Security questions saved successfully.",
                    "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (MySqlException ex)
            {
                if (ex.Number == 1062)
                {
                    MessageBox.Show("You already saved these security questions.",
                        "Duplicate", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    MessageBox.Show("Database error: " + ex.Message,
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unexpected error: " + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InitializeQuizExam()
        {
            string connStr = SettingsManager.Current.GetConnectionString();

            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    string query = "SELECT score, total_questions, percentage, submitted_at " +
                                   "FROM quiz_attempts WHERE user_id = @user_id AND status = 'SUBMITTED'";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@user_id", userId);

                        MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);

                        QuizExamScore.DataSource = dt;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("InitializeQuizExam error: " + ex.Message);
            }
        }

        // =========================================================
        // FORM CLOSING
        // =========================================================

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try { assessmentsRefreshTimer?.Stop(); } catch { }

            try { slideshowTimer?.Stop(); } catch { }
            try { slideshowTimer?.Dispose(); } catch { }
            slideshowTimer = null;

            try { bannersWatcher?.Dispose(); } catch { }
            bannersWatcher = null;

            foreach (var img in slideshowImages) try { img.Dispose(); } catch { }
            slideshowImages.Clear();

            base.OnFormClosing(e);
        }
    }
}