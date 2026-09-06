using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;

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
        }
        [DllImport("user32.dll")]
        private static extern bool BlockInput(bool fBlockIt);

        private void LockInput()
        {
            BlockInput(true);
        }

        private void UnlockInput()
        {
            BlockInput(false);
        }

        private void btnHome_Click(object sender, EventArgs e)
        {
            pnlHome.BringToFront();
        }

        private void btnActivities_Click(object sender, EventArgs e)
        {
            pnlActivity.BringToFront();
        }

        private void btnSubject_Click(object sender, EventArgs e)
        {
            pnlSubject.BringToFront();
        }

        private void btnGrades_Click(object sender, EventArgs e)
        {
            pnlGrades.BringToFront();
        }

        private void btnFile_Click(object sender, EventArgs e)
        {
            pnlFile.BringToFront();
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
        private async void ConnectBroadcastReceiver(string serverIp)
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
            catch { }
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

    }
}
