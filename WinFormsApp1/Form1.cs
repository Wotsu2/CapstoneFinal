using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Sockets;
using System.Windows.Forms;
using TheArtOfDevHtmlRenderer.Adapters;
namespace WinFormsApp1
{
    public partial class Form1 : Form
    {
        private TcpClient client;
        private string selectedFilePath = "";


        private System.Windows.Forms.Timer screenShareTimer;
        private TcpClient screenClient;
        private bool isSharingScreen = false;
        private string serverIp = "192.168.100.4";


        public Form1()
        {
            InitializeComponent();
            ConnectToServer(serverIp);
            StartScreenShare(serverIp);
        }


        // To Connect it to Server or Form2 //
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



        // To Select A File //
        private void SelectFileBtn_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    selectedFilePath = ofd.FileName;
                    lblFileName.Text = Path.GetFileName(selectedFilePath);

                }
            }
        }

        private TcpListener screenListener;
        private PictureBox pictureBoxScreen;

        // To Submit the File you selected to Server //
        private async void SubmitBtn_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(selectedFilePath))
            {
                MessageBox.Show("Please select a file");
                return;

            }

            try
            {
                using (TcpClient client = new TcpClient())
                {
                    await client.ConnectAsync(serverIp, 5001); // Same to other one it Should be Empty and configure it to setting
                    using (NetworkStream stream = client.GetStream())
                    using (BinaryWriter writer = new BinaryWriter(stream))
                    {
                        string fileName = Path.GetFileName(selectedFilePath);
                        byte[] fileBytes = File.ReadAllBytes(selectedFilePath);

                        writer.Write(fileName);
                        writer.Write(fileBytes.Length);
                        writer.Write(fileBytes);
                    }
                }

                MessageBox.Show("File Submitted Successfuly");
                selectedFilePath = "";
                lblFileName.Text = "Filename";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error" + ex.Message);
            }

        }

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

    }
}
