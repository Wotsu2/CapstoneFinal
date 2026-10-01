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

        // =========================================================
        // NOTIFICATIONS
        // =========================================================
        private Guna2Button btnNotifications;
        private Label lblNotificationBadge;
        private Guna2Panel notificationPanel;
        private FlowLayoutPanel notificationList;
        private System.Windows.Forms.Timer notificationsRefreshTimer;
        private HashSet<string> readNotificationKeys = new HashSet<string>();
        private bool notificationPanelOpen = false;

        private System.Windows.Forms.Timer assessmentsRefreshTimer;
        private System.Windows.Forms.Timer activitiesRefreshTimer;

        private Guna.UI2.WinForms.Guna2Panel assessmentsPanel;
        private FlowLayoutPanel assessmentsList;

        private bool _assessmentsInitialized = false;
        private bool _reminderShown = false;

        private System.Windows.Forms.Timer slideshowTimer;
        private List<Image> slideshowImages = new List<Image>();
        private int slideshowIndex = 0;
        private FileSystemWatcher bannersWatcher;
        private ToolTip navToolTip;

        public StudentForm(int UserId, string Section, string Username)
        {
            InitializeComponent();
            StudentSection = Section;
            userId = UserId.ToString();
            StudentUsername = Username;

            initializeShowReminderForm();
            InitializeNavTooltips();
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

                lblStudentName.Text = studentname;

                pnlHome.Visible = true;
                pnlHome.BringToFront();

                flpPendingActivities.AutoScroll = true;
                flpPendingActivities.WrapContents = true;
                flpPendingActivities.FlowDirection = FlowDirection.LeftToRight;
                flpPendingActivities.PerformLayout();

                StartSlideshow();

                // ---------- NOTIFICATIONS ----------
                try { BuildNotificationsUi(); }
                catch (Exception ex) { Console.WriteLine("BuildNotificationsUi: " + ex.Message); }

                try { LoadNotifications(); }
                catch (Exception ex) { Console.WriteLine("LoadNotifications: " + ex.Message); }

                notificationsRefreshTimer = new System.Windows.Forms.Timer { Interval = 60000 };
                notificationsRefreshTimer.Tick += (s, ev) =>
                {
                    try { LoadNotifications(); } catch { }
                };
                notificationsRefreshTimer.Start();

                activitiesRefreshTimer = new System.Windows.Forms.Timer { Interval = 10000 };
                activitiesRefreshTimer.Tick += (s, ev) =>
                {
                    try
                    {
                        if (!pnlHome.Visible) return;
                        flpPendingActivities.Controls.Clear();
                        InitializeCreateButtonActivity();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("activitiesRefreshTimer error: " + ex.Message);
                    }
                };
                activitiesRefreshTimer.Start();
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

            btnHome.Checked = true;
            btnQuizExam.Checked = false;
            btnActivities.Checked = false;
            btnAccount.Checked = false;
            btnGrades.Checked = false;
            btnSubject.Checked = false;

            try { LoadAssessments(); } catch { }
            try { RefreshPendingActivities(); } catch { }
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

            btnHome.Checked = false;
            btnQuizExam.Checked = false;
            btnActivities.Checked = true;
            btnAccount.Checked = false;
            btnSubject.Checked = false;
            btnGrades.Checked = false;

            try { InitializeDataGridViewActivities(); } catch { }
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

            btnHome.Checked = false;
            btnQuizExam.Checked = false;
            btnActivities.Checked = false;
            btnAccount.Checked = false;
            btnSubject.Checked = true;
            btnGrades.Checked = false;
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

            btnHome.Checked = false;
            btnQuizExam.Checked = false;
            btnActivities.Checked = false;
            btnAccount.Checked = false;
            btnSubject.Checked = false;
            btnGrades.Checked = true;
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

            btnHome.Checked = false;
            btnQuizExam.Checked = false;
            btnActivities.Checked = false;
            btnAccount.Checked = true;
            btnSubject.Checked = false;
            btnGrades.Checked = false;
        }

        private void btnQuizExam_Click(object sender, EventArgs e)
        {
            pnlHome.Visible = false;
            pnlActivity.Visible = false;
            pnlSubject.Visible = false;
            pnlGrades.Visible = false;
            pnlQuizExam.Visible = true;
            pnlSetting.Visible = false;
            lblhometitle.Text = "Grade";

            btnHome.Checked = false;
            btnQuizExam.Checked = true;
            btnActivities.Checked = false;
            btnAccount.Checked = false;
            btnSubject.Checked = false;
            btnGrades.Checked = false;
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

                    string query = @"
                        SELECT pa.activity_id,
                               pa.title,
                               pa.start_time,
                               pa.due_date,
                               pa.activity_subject,
                               pa.activity_status,
                               pa.section
                        FROM professor_activity pa
                        INNER JOIN student_class sc
                            ON  sc.user_id      = @user_id
                            AND sc.professor_id = pa.professor_id
                            AND LOWER(TRIM(sc.section)) = LOWER(TRIM(pa.section))
                        WHERE NOT EXISTS (
                            SELECT 1 FROM submitted_activity sa
                            WHERE sa.user_id = @user_id
                              AND sa.prof_id = pa.professor_id
                              AND sa.title   = pa.title
                              AND sa.section = pa.section
                        )
                        ORDER BY pa.due_date ASC";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@user_id", userId);

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
                                card.Tag = activityId;

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

                        Console.WriteLine($"[Activities] Loaded {count} pending activities.");
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

                                activityForm.ActivitySubmitted += () => RefreshPendingActivities();

                                activityForm.ShowDialog(this);

                                RefreshPendingActivities();
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

        private void RefreshPendingActivities()
        {
            try
            {
                flpPendingActivities.Controls.Clear();
                InitializeCreateButtonActivity();
                InitializeDataGridViewActivities();
            }
            catch (Exception ex)
            {
                Console.WriteLine("RefreshPendingActivities error: " + ex.Message);
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

                    string query = @"
                        SELECT pa.activity_id, pa.title, pa.start_time, pa.due_date,
                               pa.activity_subject, pa.activity_status,
                               pa.description, pa.professor_id
                        FROM professor_activity pa
                        INNER JOIN student_class sc
                            ON  sc.user_id      = @user_id
                            AND sc.professor_id = pa.professor_id
                            AND LOWER(TRIM(sc.section)) = LOWER(TRIM(pa.section))
                        WHERE 1 = 1";

                    if (string.IsNullOrEmpty(selectedActivitiesCategory) ||
                        selectedActivitiesCategory == "Pending")
                    {
                        query += @"
                            AND NOT EXISTS (
                                SELECT 1 FROM submitted_activity sa
                                WHERE sa.user_id = @user_id
                                  AND sa.prof_id = pa.professor_id
                                  AND sa.title   = pa.title
                                  AND sa.section = pa.section
                            )";
                    }
                    else if (selectedActivitiesCategory == "Submitted")
                    {
                        query += @"
                            AND EXISTS (
                                SELECT 1 FROM submitted_activity sa
                                WHERE sa.user_id = @user_id
                                  AND sa.prof_id = pa.professor_id
                                  AND sa.title   = pa.title
                                  AND sa.section = pa.section
                            )";
                    }
                    else if (selectedActivitiesCategory == "Incomplete")
                    {
                        query += @"
                            AND pa.due_date < NOW()
                            AND NOT EXISTS (
                                SELECT 1 FROM submitted_activity sa
                                WHERE sa.user_id = @user_id
                                  AND sa.prof_id = pa.professor_id
                                  AND sa.title   = pa.title
                                  AND sa.section = pa.section
                            )";
                    }

                    query += " ORDER BY pa.due_date ASC";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@user_id", userId);

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
                StudentSection, className, ActivityStatus, tempPdfPath);

            activityForm.ActivitySubmitted += () => RefreshPendingActivities();

            activityForm.ShowDialog(this);

            RefreshPendingActivities();
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
                        query = @"SELECT activity_file FROM professor_activity WHERE activity_id = @activity_id";
                    else
                        query = @"SELECT activity_file FROM professor_activity 
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

                        object result = cmd.ExecuteScalar();
                        if (result == null || result == DBNull.Value) return null;

                        if (result is string s && !string.IsNullOrWhiteSpace(s))
                            return s.Trim();

                        if (result is byte[] bytes && bytes.Length > 0)
                        {
                            string tempFolder = Path.Combine(Path.GetTempPath(), "cdsga_activities", userId);
                            Directory.CreateDirectory(tempFolder);
                            string tempPath = Path.Combine(tempFolder, $"activity_{activityId}.pdf");
                            File.WriteAllBytes(tempPath, bytes);
                            return tempPath;
                        }

                        return null;
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

                    LoadJoinedClasses();
                    MessageBox.Show("Successfully Joined Class!");

                    try { RefreshPendingActivities(); } catch { }
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
            // Legacy stub — actual UI is built by LoadJoinedClasses()
        }

        private void LoadJoinedClasses()
        {
            string connStr = SettingsManager.Current.GetConnectionString();
            Color maroon = Color.FromArgb(123, 15, 23);
            Color softPink = Color.FromArgb(253, 236, 238);
            Color borderIdle = Color.FromArgb(230, 225, 225);

            try
            {
                flpSubjectClass.Controls.Clear();
                flpSubjectClass.BackColor = Color.FromArgb(255, 245, 240);
                flpSubjectClass.Padding = new Padding(10);

                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    string query = @"
                        SELECT sc.class_id, 
                               sc.class_name, 
                               sc.class_date, 
                               sc.class_time, 
                               sc.section,
                               sc.professor_id,
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

                                int rProfId = reader["professor_id"] != DBNull.Value
                                    ? Convert.ToInt32(reader["professor_id"]) : 0;

                                string profLast = reader["lastname"] != DBNull.Value ? reader["lastname"].ToString() : "";
                                string profFirst = reader["firstname"] != DBNull.Value ? reader["firstname"].ToString() : "";
                                string profMiddle = reader["middlename"] != DBNull.Value ? reader["middlename"].ToString() : "";

                                string profFullName = string.Join(" ",
                                    new[] { profLast, profFirst, profMiddle }
                                        .Where(s => !string.IsNullOrWhiteSpace(s)))
                                    .Trim();

                                if (string.IsNullOrEmpty(profFullName))
                                    profFullName = "Unknown Professor";

                                // ===== CARD =====
                                var cardPanel = new Guna.UI2.WinForms.Guna2Panel
                                {
                                    Size = new Size(350, 250),
                                    FillColor = Color.White,
                                    BackColor = Color.Transparent,
                                    BorderRadius = 16,
                                    BorderColor = borderIdle,
                                    BorderThickness = 1,
                                    Margin = new Padding(12),
                                    Tag = classId,
                                    Cursor = Cursors.Hand,
                                    ShadowDecoration = { Enabled = true, Depth = 8, BorderRadius = 16, Color = Color.FromArgb(60, 0, 0, 0) }
                                };

                                // ===== HEADER =====
                                var header = new Guna.UI2.WinForms.Guna2Panel
                                {
                                    Size = new Size(350, 80),
                                    Location = new Point(0, 0),
                                    FillColor = maroon,
                                    BorderRadius = 16,
                                    CustomizableEdges = { BottomLeft = false, BottomRight = false },
                                    Cursor = Cursors.Hand
                                };
                                cardPanel.Controls.Add(header);

                                var lblTitle = new Label
                                {
                                    Text = rClassName,
                                    Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                                    ForeColor = Color.White,
                                    BackColor = Color.Transparent,
                                    AutoSize = false,
                                    AutoEllipsis = true,
                                    Size = new Size(275, 34),
                                    Location = new Point(20, 22),
                                    TextAlign = ContentAlignment.MiddleLeft,
                                    Cursor = Cursors.Hand
                                };
                                header.Controls.Add(lblTitle);

                                var lblMenu = new Label
                                {
                                    Text = "•••",
                                    Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                                    ForeColor = Color.White,
                                    BackColor = Color.Transparent,
                                    AutoSize = true,
                                    Location = new Point(305, 10),
                                    Cursor = Cursors.Hand
                                };
                                header.Controls.Add(lblMenu);

                                // ===== AVATAR =====
                                string initials = "";
                                if (!string.IsNullOrWhiteSpace(profFirst)) initials += char.ToUpper(profFirst.Trim()[0]);
                                if (!string.IsNullOrWhiteSpace(profLast)) initials += char.ToUpper(profLast.Trim()[0]);
                                if (initials == "") initials = "?";

                                var avatar = new Guna.UI2.WinForms.Guna2Panel
                                {
                                    Size = new Size(44, 44),
                                    Location = new Point(20, 98),
                                    FillColor = softPink,
                                    BorderRadius = 22,
                                    Cursor = Cursors.Hand
                                };
                                avatar.Controls.Add(new Label
                                {
                                    Text = initials,
                                    Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                                    ForeColor = maroon,
                                    BackColor = Color.Transparent,
                                    Dock = DockStyle.Fill,
                                    TextAlign = ContentAlignment.MiddleCenter,
                                    Cursor = Cursors.Hand
                                });
                                cardPanel.Controls.Add(avatar);

                                cardPanel.Controls.Add(new Label
                                {
                                    Text = "PROFESSOR",
                                    Font = new Font("Segoe UI", 7.5F, FontStyle.Bold),
                                    ForeColor = Color.FromArgb(150, 150, 150),
                                    BackColor = Color.Transparent,
                                    AutoSize = true,
                                    Location = new Point(74, 98),
                                    Cursor = Cursors.Hand
                                });

                                cardPanel.Controls.Add(new Label
                                {
                                    Text = profFullName,
                                    Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                                    ForeColor = maroon,
                                    BackColor = Color.Transparent,
                                    AutoSize = false,
                                    AutoEllipsis = true,
                                    Size = new Size(260, 24),
                                    Location = new Point(74, 114),
                                    TextAlign = ContentAlignment.MiddleLeft,
                                    Cursor = Cursors.Hand
                                });

                                // ===== DIVIDER =====
                                cardPanel.Controls.Add(new Panel
                                {
                                    Size = new Size(310, 1),
                                    Location = new Point(20, 158),
                                    BackColor = Color.FromArgb(235, 235, 235)
                                });

                                // ===== SCHEDULE =====
                                cardPanel.Controls.Add(new Label
                                {
                                    Text = "📅  " + rClassDate,
                                    Font = new Font("Segoe UI Emoji", 11F, FontStyle.Bold),
                                    ForeColor = Color.FromArgb(40, 40, 40),
                                    BackColor = Color.Transparent,
                                    AutoSize = true,
                                    Location = new Point(20, 172),
                                    Cursor = Cursors.Hand
                                });

                                cardPanel.Controls.Add(new Label
                                {
                                    Text = "🕒  " + rClassTime,
                                    Font = new Font("Segoe UI Emoji", 10F, FontStyle.Regular),
                                    ForeColor = Color.FromArgb(110, 110, 110),
                                    BackColor = Color.Transparent,
                                    AutoSize = true,
                                    Location = new Point(20, 202),
                                    Cursor = Cursors.Hand
                                });

                                // ===== SECTION PILL =====
                                var sectionPill = new Guna.UI2.WinForms.Guna2Panel
                                {
                                    Size = new Size(80, 32),
                                    Location = new Point(250, 190),
                                    FillColor = softPink,
                                    BorderRadius = 16,
                                    Cursor = Cursors.Hand
                                };
                                sectionPill.Controls.Add(new Label
                                {
                                    Text = rClassSection,
                                    Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                                    ForeColor = maroon,
                                    BackColor = Color.Transparent,
                                    Dock = DockStyle.Fill,
                                    TextAlign = ContentAlignment.MiddleCenter,
                                    Cursor = Cursors.Hand
                                });
                                cardPanel.Controls.Add(sectionPill);

                                // ===== UNJOIN =====
                                string capturedClassName = rClassName;
                                string capturedSection = rClassSection;
                                string capturedTime = rClassTime;
                                string capturedDate = rClassDate;

                                lblMenu.Click += (s, e) =>
                                {
                                    DialogResult result = MessageBox.Show(
                                        $"Are you sure you want to unjoin '{capturedClassName}'?",
                                        "Confirm Unjoin",
                                        MessageBoxButtons.YesNo,
                                        MessageBoxIcon.Warning);

                                    if (result != DialogResult.Yes) return;

                                    UnjoinClass(capturedClassName, capturedSection, capturedTime, capturedDate);

                                    flpSubjectClass.Controls.Remove(cardPanel);
                                    cardPanel.Dispose();

                                    try { RefreshPendingActivities(); } catch { }
                                };

                                // ===== OPEN CLASSROOM =====
                                int capturedProfId = rProfId;
                                Action openClassroom = () =>
                                {
                                    try
                                    {
                                        var form = new ClassRoomForm(
                                            int.Parse(userId),
                                            studentname,
                                            capturedProfId,
                                            rClassName,
                                            rClassSection,
                                            rClassTime,
                                            rClassDate);

                                        form.ShowDialog(this);
                                    }
                                    catch (Exception ex)
                                    {
                                        MessageBox.Show("Unable to open classroom:\n" + ex.Message);
                                    }
                                };

                                cardPanel.Click += (s, e) => openClassroom();
                                header.Click += (s, e) => openClassroom();
                                lblTitle.Click += (s, e) => openClassroom();
                                avatar.Click += (s, e) => openClassroom();
                                sectionPill.Click += (s, e) => openClassroom();

                                // ===== HOVER =====
                                cardPanel.MouseEnter += (s, e) => cardPanel.BorderColor = maroon;
                                cardPanel.MouseLeave += (s, e) => cardPanel.BorderColor = borderIdle;

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
                Console.WriteLine("LoadJoinedClasses error: " + ex);
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
                pnlSettingProfile.Height = 670;
            else if (pnlSettingProfile.Height >= 670)
                pnlSettingProfile.Height = 350;
        }

        private void ClearAllFormData()
        {
            ClearAllFormData(this);
        }

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

            try { activitiesRefreshTimer?.Stop(); } catch { }
            try { activitiesRefreshTimer?.Dispose(); } catch { }
            activitiesRefreshTimer = null;

            MessageBox.Show("Signed out successfully.");
        }

        private void Logout()
        {
            DialogResult result = MessageBox.Show("Are you sure you want to logout?",
                "Logout Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result != DialogResult.Yes) return;

            try { StopServer(); } catch { }
            try { ClearAllFormData(); } catch { }

            foreach (Form f in Application.OpenForms.Cast<Form>().ToList())
            {
                if (f is Login && !f.IsDisposed)
                {
                    f.Hide();
                    f.Dispose();
                }
            }

            var loginForm = new Login();
            loginForm.Show();
            loginForm.BringToFront();
            loginForm.Activate();

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

                    byte[] bytes = File.ReadAllBytes(CurrentProfilePath);
                    using (var ms = new MemoryStream(bytes))
                    {
                        var temp = Image.FromStream(ms);
                        var bmp = new Bitmap(temp);
                        temp.Dispose();

                        picboxNewPicture.Image?.Dispose();
                        picboxNewPicture.Image = bmp;
                    }
                    picboxNewPicture.SizeMode = PictureBoxSizeMode.Zoom;
                    btnSubmitChangePhoto.Enabled = true;
                }
            }
        }

        // =========================================================
        // PROFILE PHOTO
        // =========================================================
        private async void btnSubmitChangePhoto_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(CurrentProfilePath))
            {
                MessageBox.Show("Upload an image first!");
                return;
            }

            if (!File.Exists(CurrentProfilePath))
            {
                MessageBox.Show("The selected file no longer exists.");
                return;
            }

            try
            {
                byte[] imageBytes = await File.ReadAllBytesAsync(CurrentProfilePath);

                string ext = Path.GetExtension(CurrentProfilePath);
                string fileName = SanitizeFolderName(StudentUsername) + "_" +
                                  DateTime.Now.ToString("yyyyMMddHHmmss") + ext;

                string uncPath = await SendProfilePhotoToAdmin(imageBytes, fileName);

                if (string.IsNullOrEmpty(uncPath))
                {
                    MessageBox.Show("Failed to send profile picture to server.");
                    return;
                }

                string connStr = SettingsManager.Current.GetConnectionString();
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand(
                        "UPDATE user_credential SET profile_picture = @path WHERE username = @username", conn))
                    {
                        cmd.Parameters.AddWithValue("@path", uncPath);
                        cmd.Parameters.AddWithValue("@username", StudentUsername);

                        int rows = cmd.ExecuteNonQuery();
                        if (rows == 0)
                        {
                            MessageBox.Show("No user row was updated. Check the username.");
                            return;
                        }
                    }
                }

                picboxSettingProfilePicture.Image?.Dispose();
                picboxSettingProfilePicture.Image = null;

                btnAccount.Image?.Dispose();
                btnAccount.Image = null;

                InitializeChangingPicture();
                MessageBox.Show("Profile picture updated successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("btnSubmitChangePhoto_Click error: " + ex);
                MessageBox.Show("Error: " + ex.Message);
            }
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

                    if (completed == timeoutTask)
                    {
                        MessageBox.Show($"Server ({adminIp}:{adminPort}) not reachable (timeout).");
                        return null;
                    }

                    await connectTask;
                    if (!client.Connected)
                    {
                        MessageBox.Show($"Server ({adminIp}:{adminPort}) refused the connection.");
                        return null;
                    }

                    string returnedUnc = null;

                    using (var stream = client.GetStream())
                    using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
                    {
                        writer.Write("PROFILE_PHOTO");
                        writer.Write(StudentUsername);
                        writer.Write(fileName);
                        writer.Write(imageBytes.Length);
                        writer.Write(imageBytes);
                        writer.Flush();

                        try
                        {
                            var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
                            returnedUnc = reader.ReadString();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Read reply error: " + ex.Message);
                        }
                    }

                    return returnedUnc;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("SendProfilePhotoToAdmin error: " + ex.Message);
                MessageBox.Show("Send error: " + ex.Message);
                return null;
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

                        object raw = cmd.ExecuteScalar();
                        if (raw == null || raw == DBNull.Value) return;

                        string path = raw.ToString();
                        if (!File.Exists(path)) return;

                        byte[] bytes = File.ReadAllBytes(path);
                        using (var ms = new MemoryStream(bytes))
                        {
                            var temp = Image.FromStream(ms);
                            var bmp = new Bitmap(temp);
                            temp.Dispose();

                            picboxSettingProfilePicture.Image?.Dispose();
                            picboxSettingProfilePicture.Image = bmp;
                            picboxSettingProfilePicture.SizeMode = PictureBoxSizeMode.Zoom;

                            btnAccount.Image?.Dispose();
                            btnAccount.Image = bmp;
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

        private async Task<bool> SendAuthenticationPhotoToAdmin(byte[] imageBytes, string fileName)
        {
            try
            {
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
                Size = new Size(307, 390),
                Location = new Point(977, 355),
                FillColor = Color.White,
                BackColor = Color.Transparent,
                BorderRadius = 10,
                BorderColor = Color.FromArgb(220, 220, 220),
                BorderThickness = 1,
                ShadowDecoration = { BorderRadius = 10, Enabled = true, Depth = 30, Color = Color.FromArgb(60, 0, 0, 0) },
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
                Location = new Point(45, 10),
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
                                int quizId = reader.GetInt32("quiz_id");
                                string title = reader["quiz_title"].ToString();
                                string subject = reader["subject"]?.ToString() ?? "";
                                string type = reader["assessment_type"]?.ToString() ?? "quiz";
                                string period = reader["exam_period"]?.ToString() ?? "";

                                bool submitted = HasSubmitted(quizId);

                                if (submitted)
                                    continue;

                                any = true;
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

        // =========================================================
        // FORM CLOSING
        // =========================================================
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try { assessmentsRefreshTimer?.Stop(); } catch { }
            try { assessmentsRefreshTimer?.Dispose(); } catch { }

            try { activitiesRefreshTimer?.Stop(); } catch { }
            try { activitiesRefreshTimer?.Dispose(); } catch { }
            activitiesRefreshTimer = null;

            try { slideshowTimer?.Stop(); } catch { }
            try { slideshowTimer?.Dispose(); } catch { }
            slideshowTimer = null;

            try { bannersWatcher?.Dispose(); } catch { }
            bannersWatcher = null;

            foreach (var img in slideshowImages) try { img.Dispose(); } catch { }
            slideshowImages.Clear();

            base.OnFormClosing(e);
        }

        private void InitializeNavTooltips()
        {
            navToolTip = new ToolTip
            {
                AutoPopDelay = 5000,
                InitialDelay = 350,
                ReshowDelay = 100,
                ShowAlways = true,
                IsBalloon = false,
                UseFading = true,
                UseAnimation = true
            };

            navToolTip.SetToolTip(btnHome, "Home");
            navToolTip.SetToolTip(btnActivities, "Activities");
            navToolTip.SetToolTip(btnSubject, "Subjects");
            navToolTip.SetToolTip(btnGrades, "Grades");
            navToolTip.SetToolTip(btnQuizExam, "Quiz / Exam");
            navToolTip.SetToolTip(btnAccount, "Settings");
        }

        // =========================================================
        // UTILITIES
        // =========================================================


        // =========================================================
        // UTILITIES
        // =========================================================

        private static string SanitizeFolderName(string name)

        {
            if (string.IsNullOrWhiteSpace(name)) return "Untitled";

            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c.ToString(), "");

            return name.Trim().TrimEnd('.');
        }

        // =========================================================
        // EXPANDED CALENDAR
        // =========================================================

        private ExpandedCalendar expandedCal;

        private static bool TryParseCalDate(string s, out DateTime dt)
        {
            dt = default(DateTime);
            if (string.IsNullOrWhiteSpace(s)) return false;

            return DateTime.TryParse(s, System.Globalization.CultureInfo.CurrentCulture,
                                     System.Globalization.DateTimeStyles.None, out dt)
                || DateTime.TryParse(s, System.Globalization.CultureInfo.InvariantCulture,
                                     System.Globalization.DateTimeStyles.None, out dt);
        }

        private List<CalEvent> LoadCalendarEvents()
        {
            var list = new List<CalEvent>();
            string connStr = SettingsManager.Current.GetConnectionString();

            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    // ---- ACTIVITIES (sa mga section na sinalihan ng student) ----
                    const string qAct = @"
                SELECT pa.activity_id, pa.title, pa.activity_subject, pa.due_date,
                       EXISTS (SELECT 1 FROM submitted_activity sa
                               WHERE sa.user_id = @user_id
                                 AND sa.prof_id = pa.professor_id
                                 AND sa.title   = pa.title
                                 AND sa.section = pa.section) AS submitted
                FROM professor_activity pa
                INNER JOIN student_class sc
                    ON  sc.user_id      = @user_id
                    AND sc.professor_id = pa.professor_id
                    AND LOWER(TRIM(sc.section)) = LOWER(TRIM(pa.section))";

                    using (var cmd = new MySqlCommand(qAct, conn))
                    {
                        cmd.Parameters.AddWithValue("@user_id", userId);
                        using (var r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                string dueRaw = r["due_date"].ToString();
                                if (!TryParseCalDate(dueRaw, out DateTime due))
                                {
                                    Console.WriteLine("[Calendar] Can't parse due_date: '" + dueRaw + "'");
                                    continue;
                                }

                                string title = r["title"].ToString();
                                string subject = r["activity_subject"].ToString();

                                list.Add(new CalEvent
                                {
                                    Id = Convert.ToInt32(r["activity_id"]),
                                    Date = due.Date,
                                    Kind = "Activity",
                                    Text = string.IsNullOrEmpty(subject) ? title : title + " (" + subject + ")",
                                    Done = Convert.ToInt32(r["submitted"]) > 0
                                });
                            }
                        }
                    }

                    // ---- QUIZZES / EXAMS ----
                    const string qQuiz = @"
                SELECT q.quiz_id, q.quiz_title, q.subject, q.assessment_type, q.created_at,
                       EXISTS (SELECT 1 FROM quiz_attempts qa
                               WHERE qa.quiz_id = q.quiz_id
                                 AND qa.user_id = @user_id
                                 AND qa.status  = 'SUBMITTED') AS submitted
                FROM quizzes q";

                    using (var cmd = new MySqlCommand(qQuiz, conn))
                    {
                        cmd.Parameters.AddWithValue("@user_id", userId);
                        using (var r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                bool isExam = r["assessment_type"].ToString()
                                               .Equals("exam", StringComparison.OrdinalIgnoreCase);
                                string subject = r["subject"].ToString();
                                string title = r["quiz_title"].ToString();

                                list.Add(new CalEvent
                                {
                                    Id = Convert.ToInt32(r["quiz_id"]),
                                    Date = Convert.ToDateTime(r["created_at"]).Date,
                                    Kind = isExam ? "Exam" : "Quiz",
                                    Text = string.IsNullOrEmpty(subject) ? title : subject + " " + title,
                                    Done = Convert.ToInt32(r["submitted"]) > 0
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("LoadCalendarEvents error: " + ex.Message);
            }

            return list;
        }

        private void btnCalendarExpand_Click(object sender, EventArgs e)
        {
            if (expandedCal == null)
            {
                expandedCal = new ExpandedCalendar();
                expandedCal.CloseRequested += (s, a) => expandedCal.Visible = false;

                // Pag may na-click na event sa listahan
                expandedCal.EventOpened += (s, ev) =>
                {
                    expandedCal.Visible = false;   // isara muna ang flyout

                    BeginInvoke(new Action(() =>
                    {
                        if (ev.Kind == "Activity")
                            InitializeHomeActivityButton(ev.Id);
                        else if (ev.Kind == "Quiz" || ev.Kind == "Exam")
                            OpenAssessment(ev.Id, ev.Text, ev.Kind.ToLower());
                    }));
                };
            }

            expandedCal.SetEvents(LoadCalendarEvents());   // laging bago ang data pag binuksan
            expandedCal.ShowOn(pnlHome);
        }

<<<<<<< HEAD
        private void guna2Panel1_Paint(object sender, PaintEventArgs e)
        {
           
        }
=======
        private void BuildNotificationsUi()
        {
            if (btnNotifications != null) return;

            btnNotifications = new Guna2Button
            {
                Size = new Size(46, 46),
                Location = new Point(this.ClientSize.Width - 160, 22),
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
            btnNotifications.HoverState.FillColor = Color.FromArgb(250, 235, 235);
            btnNotifications.Click += (s, e) => ToggleNotificationPanel();

            this.Controls.Add(btnNotifications);
            btnNotifications.BringToFront();

            lblNotificationBadge = new Label
            {
                Size = new Size(22, 22),
                Location = new Point(
                    btnNotifications.Right - 26,
                    btnNotifications.Top - 4),
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
                Location = new Point(
                    this.ClientSize.Width - 440,
                    78),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BorderRadius = 14,
                FillColor = Color.White,
                BorderColor = Color.FromArgb(230, 225, 225),
                BorderThickness = 1,
                ShadowDecoration = { Enabled = true, Depth = 16, Color = Color.FromArgb(60, 0, 0, 0) },
                Visible = false,
                AutoScroll = false
            };

            Guna2Panel panelHeader = new Guna2Panel
            {
                Dock = DockStyle.Top,
                Height = 55,
                FillColor = Color.Maroon,
                BorderRadius = 0
            };
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
                foreach (Control c in notificationList.Controls)
                {
                    if (c is Panel card && card.Tag is string key)
                        readNotificationKeys.Add(key);
                }
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

            // Close the panel when clicking anywhere outside it
            this.Click += (s, e) => CloseNotificationPanel();
            foreach (Control ctrl in this.Controls)
            {
                if (ctrl != notificationPanel && ctrl != btnNotifications && ctrl != lblNotificationBadge)
                {
                    ctrl.Click += (s, e) => CloseNotificationPanel();
                }
            }
        }

        private void ToggleNotificationPanel()
        {
            if (notificationPanel == null) return;

            if (notificationPanelOpen)
            {
                CloseNotificationPanel();
            }
            else
            {
                notificationPanelOpen = true;
                notificationPanel.Visible = true;
                notificationPanel.BringToFront();
                LoadNotifications();
            }
        }

        private void CloseNotificationPanel()
        {
            if (notificationPanel == null) return;
            notificationPanel.Visible = false;
            notificationPanelOpen = false;
        }

        // =========================================================
        // NOTIFICATIONS — LOAD + BUILD
        // =========================================================
        private void LoadNotifications()
        {
            if (notificationList == null) return;

            notificationList.Controls.Clear();

            int unreadCount = 0;
            var items = new List<NotificationItem>();

            foreach (var a in LoadNotificationActivities())
            {
                string key = "A|" + a.Id;
                bool unread = !readNotificationKeys.Contains(key);
                if (unread) unreadCount++;

                items.Add(new NotificationItem
                {
                    Key = key,
                    Icon = "📄",
                    Title = a.Title,
                    Subtitle = a.Subject + "  •  Due " + a.DueDate,
                    Time = a.PostedAt,
                    Type = "activity",
                    Id = a.Id,
                    Unread = unread
                });
            }

            foreach (var q in LoadNotificationQuizzes())
            {
                string key = "Q|" + q.Id;
                bool unread = !readNotificationKeys.Contains(key);
                if (unread) unreadCount++;

                items.Add(new NotificationItem
                {
                    Key = key,
                    Icon = q.Type == "exam" ? "📝" : "📋",
                    Title = q.Title,
                    Subtitle = q.Subject + "  •  " + q.Type,
                    Time = q.PostedAt,
                    Type = "quiz",
                    Id = q.Id,
                    Unread = unread
                });
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
                foreach (var item in items)
                {
                    notificationList.Controls.Add(BuildNotificationCard(item));
                }
            }

            if (unreadCount > 0)
            {
                lblNotificationBadge.Text = unreadCount > 99 ? "99+" : unreadCount.ToString();
                lblNotificationBadge.Visible = true;
                lblNotificationBadge.BringToFront();
            }
            else
            {
                lblNotificationBadge.Visible = false;
            }
        }

        private class NotificationItem
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

        private Panel BuildNotificationCard(NotificationItem item)
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

            // ---------- CLICK HANDLER ----------
            Action openItem = () =>
            {
                readNotificationKeys.Add(item.Key);
                CloseNotificationPanel();

                if (item.Type == "activity")
                    OpenNotificationActivity(item.Id);
                else if (item.Type == "quiz")
                    OpenNotificationQuiz(item.Id);
            };

            card.Click += (s, e) => openItem();
            icon.Click += (s, e) => openItem();
            lblTitle.Click += (s, e) => openItem();
            lblSub.Click += (s, e) => openItem();

            foreach (Control c in card.Controls)
                c.Click += (s, e) => openItem();

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

        private List<SimpleNotification> LoadNotificationActivities()
        {
            var list = new List<SimpleNotification>();
            string connStr = SettingsManager.Current.GetConnectionString();

            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    string query = @"
                        SELECT pa.activity_id, pa.title, pa.activity_subject,
                               pa.due_date, pa.start_time
                        FROM professor_activity pa
                        INNER JOIN student_class sc
                            ON  sc.user_id      = @user_id
                            AND sc.professor_id = pa.professor_id
                            AND LOWER(TRIM(sc.section)) = LOWER(TRIM(pa.section))
                        WHERE NOT EXISTS (
                            SELECT 1 FROM submitted_activity sa
                            WHERE sa.user_id = @user_id
                              AND sa.prof_id = pa.professor_id
                              AND sa.title   = pa.title
                              AND sa.section = pa.section
                        )
                        ORDER BY pa.start_time DESC
                        LIMIT 20";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@user_id", userId);

                        using (var r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                DateTime? posted = null;
                                try
                                {
                                    object raw = r["start_time"];
                                    if (raw != null && raw != DBNull.Value)
                                    {
                                        if (DateTime.TryParse(raw.ToString(), out DateTime dt))
                                            posted = dt;
                                    }
                                }
                                catch { }

                                list.Add(new SimpleNotification
                                {
                                    Id = Convert.ToInt32(r["activity_id"]),
                                    Title = r["title"]?.ToString() ?? "",
                                    Subject = r["activity_subject"]?.ToString() ?? "",
                                    DueDate = r["due_date"]?.ToString() ?? "",
                                    PostedAt = posted
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("LoadNotificationActivities error: " + ex.Message);
            }

            return list;
        }

        private List<SimpleNotification> LoadNotificationQuizzes()
        {
            var list = new List<SimpleNotification>();
            string connStr = SettingsManager.Current.GetConnectionString();

            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    string query = @"
                        SELECT q.quiz_id, q.quiz_title, q.subject,
                               q.assessment_type, q.created_at
                        FROM quizzes q
                        ORDER BY q.created_at DESC
                        LIMIT 20";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        using (var r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                int quizId = Convert.ToInt32(r["quiz_id"]);

                                if (HasSubmitted(quizId)) continue;

                                DateTime? posted = null;
                                try
                                {
                                    object raw = r["created_at"];
                                    if (raw != null && raw != DBNull.Value)
                                    {
                                        if (DateTime.TryParse(raw.ToString(), out DateTime dt))
                                            posted = dt;
                                    }
                                }
                                catch { }

                                list.Add(new SimpleNotification
                                {
                                    Id = quizId,
                                    Title = r["quiz_title"]?.ToString() ?? "",
                                    Subject = r["subject"]?.ToString() ?? "",
                                    Type = r["assessment_type"]?.ToString() ?? "quiz",
                                    PostedAt = posted
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("LoadNotificationQuizzes error: " + ex.Message);
            }

            return list;
        }

        private class SimpleNotification
        {
            public int Id;
            public string Title;
            public string Subject;
            public string DueDate;
            public string Type;
            public DateTime? PostedAt;
        }

        private void OpenNotificationActivity(int activityId)
        {
            try
            {
                InitializeHomeActivityButton(activityId);
            }
            catch (Exception ex)
            {
                Console.WriteLine("OpenNotificationActivity error: " + ex.Message);
                MessageBox.Show("Unable to open activity:\n" + ex.Message);
            }
        }

        private void OpenNotificationQuiz(int quizId)
        {
            try
            {
                var form = new StudentQuizForm(int.Parse(userId), quizId);
                form.ShowDialog(this);
                LoadNotifications();
                LoadAssessments();
            }
            catch (Exception ex)
            {
                Console.WriteLine("OpenNotificationQuiz error: " + ex.Message);
                MessageBox.Show("Unable to open quiz:\n" + ex.Message);
            }
        }


>>>>>>> 8b05e2974c34386077fec2c9b40ebf33bfc3690b
    }
}