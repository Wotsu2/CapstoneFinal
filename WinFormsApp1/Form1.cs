using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Data.SqlClient; // For SQL Server. Use MySql.Data.MySqlClient for MySQL, etc.
using System.Windows.Forms;
using TheArtOfDevHtmlRenderer.Adapters;

using System.Net.Sockets;
namespace WinFormsApp1
{
    public partial class Form1 : Form
    {
        private TcpClient client;

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
                await client.ConnectAsync("192.168.1.10", 5000); // use the SERVER's actual IP here

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

    }
}
