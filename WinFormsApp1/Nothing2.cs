using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;

namespace WinFormsApp1
{
    public partial class Nothing2 : Form
    {
        private string serverIp = "192.168.100.4";
        private string selectedFilePath = "";

        public Nothing2()
        {
            InitializeComponent();
        }
        

        private void btnSelectFile_DragDrop(object sender, DragEventArgs e)
        {
            string[] selectedFilePath = (string[])e.Data.GetData(DataFormats.FileDrop);

            foreach (string file in selectedFilePath)
            {
                lblFilename.Text = Path.GetFileName(file);
                MessageBox.Show($"Dropped file: {file}");
            }
        }

        private void btnSelectFile_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void btnSelectFile_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    string selectedFile = ofd.FileName;
                    lblFilename.Text = Path.GetFileName(selectedFilePath);
                }
            }
        }

        private void SaveInfoDatabase()
        {
            string connStr = "Server=localhost;Port=3306;Database=student_activities;Uid=root;Pwd=;";

            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    string query = "INSERT INTO useractivities (Assessment, Score, DueData) VALUES (@Asses, @Score, @DueDate)";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Asses", txtTitle.Text);
                        cmd.Parameters.AddWithValue("@Score", txtScore.Text);
                        cmd.Parameters.AddWithValue("@DueDate", dtpDueDate.Value);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }

        private async void btnSubmit_Click(object sender, EventArgs e)
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

                SaveInfoDatabase();
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
