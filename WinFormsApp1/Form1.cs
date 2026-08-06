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
        private string ipAddress = "192.168.100.112";


        public Form1()
        {
            InitializeComponent();
            ConnectToServer();
        }


        // To Connect it to Server or Form2 //
        private async void ConnectToServer()
        {
            try
            {
                client = new TcpClient();
                await client.ConnectAsync("192.168.100.1", 5000); // use the SERVER's actual IP here And Should be Empty and configure it to setting

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
                    await client.ConnectAsync("192.168.100.1", 5001); // Same to other one it Should be Empty and configure it to setting
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

        private System.Windows.Forms.Timer screenShareTimer;
        private TcpClient screenClient;
        private bool isSharingScreen = false;

        private void StartScreenSharing(string serverIp)
        {
            screenClient = new TcpClient();
            screenClient.Connect(serverIp, 5002);

            isSharingScreen = true;

            screenShareTimer = new System.Windows.Forms.Timer();
            screenShareTimer.Interval = 500;
            screenShareTimer.Tick += ScreenShareTimer_Tick;
            screenShareTimer.Start();
        }

        private void ScreenShareTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                Bitmap screenshot = CaptureScreen();

                using (MemoryStream ms = new MemoryStream())
                {
                    screenshot.Save(ms, ImageFormat.Jpeg); // JPEG = smaller size, faster to send
                    byte[] imageBytes = ms.ToArray();

                    NetworkStream stream = screenClient.GetStream();
                    using (BinaryWriter writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
                    {
                        writer.Write(imageBytes.Length);
                        writer.Write(imageBytes);
                    }
                }

                screenshot.Dispose();
            }
            catch (Exception ex)
            {
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
