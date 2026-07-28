using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Data.SqlClient; // For SQL Server. Use MySql.Data.MySqlClient for MySQL, etc.
using System.Windows.Forms;
using TheArtOfDevHtmlRenderer.Adapters;

using System.IO;
using System.Net.Sockets;
namespace WinFormsApp1
{
    public partial class Form1 : Form
    {
        private TcpClient client;
        private string selectedFilePath = "";


        public Form1()
        {
            InitializeComponent();
            ConnectToServer();
        }

        private async void ConnectToServer()
        {
            try
            {
                client = new TcpClient();
                await client.ConnectAsync("192.168.100.124", 5000); // use the SERVER's actual IP here

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
                    await client.ConnectAsync("192.168.100.124", 5001);
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
    }
}
