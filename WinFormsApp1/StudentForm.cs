using Guna.UI2.WinForms;
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
        public StudentForm()
        {
            InitializeComponent();
        }
        private void StudentForm_Load(object sender, EventArgs e)
        {
            ConnectToServer(serverIp);
            StartScreenShare(serverIp);
            ConnectBroadcastReceiver(serverIp);
            StartListening();
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

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void label8_Click(object sender, EventArgs e)
        {

        }
    }
}
