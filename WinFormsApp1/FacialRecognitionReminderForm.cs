using DevExpress.Pdf.Native;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinFormsApp1
{
    public partial class FacialRecognitionReminderForm : Form
    {
        private int StudentId;
        private string AuthenticationPhoto;
        private string StudentUsername;

        public FacialRecognitionReminderForm(int StudentID, string Username)
        {
            InitializeComponent();
            StudentId = StudentID;
            StudentUsername = Username;
            InitializeGetRemainingLimit();
            initializeCloseExitButton();
        }

        private void InitializeGetRemainingLimit()
        {
            string connStr = SettingsManager.Current.GetConnectionString();
            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = "SELECT remaining_limit FROM user_credential WHERE username = @username";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@username", StudentUsername);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int remainingLimit = 0;
                                if (!reader.IsDBNull(reader.GetOrdinal("remaining_limit")))
                                    remainingLimit = reader.GetInt32("remaining_limit");

                                lblRemainingLimit.Text = $"*{remainingLimit} remaining";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("InitializeGetRemainingLimit error: " + ex.Message);
            }
        }

        private void initializeCloseExitButton()
        {
            string connStr = SettingsManager.Current.GetConnectionString();
            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = "SELECT remaining_limit FROM user_credential WHERE username = @username";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@username", StudentUsername);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int remainingLimit = 0;
                                if (!reader.IsDBNull(reader.GetOrdinal("remaining_limit")))
                                    remainingLimit = reader.GetInt32("remaining_limit");

                                if (remainingLimit <= 0)
                                {
                                    btnCloseForm.Enabled = false;
                                    btnSkipforNow.Enabled = false;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("initializeCloseExitButton error: " + ex.Message);
            }
        }

        private void btnCloseForm_Click(object sender, EventArgs e)
        {
            string connStr = SettingsManager.Current.GetConnectionString();
            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = "UPDATE user_credential SET remaining_limit = remaining_limit - 1 WHERE username = @username";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@username", StudentUsername);
                        int rowsAffected = cmd.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("Remaining limit updated successfully.");
                        }
                        else
                        {
                            MessageBox.Show("No rows were updated. Please check the user ID.");
                        }
                    }
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while updating the remaining limit." + ex.Message);
            }
        }

        private void btnSkipforNow_Click(object sender, EventArgs e)
        {
            string connStr = SettingsManager.Current.GetConnectionString();
            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = "UPDATE user_credential SET remaining_limit = remaining_limit - 1 WHERE username = @username";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@username", StudentUsername);
                        int rowsAffected = cmd.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("Remaining limit updated successfully.");
                        }
                        else
                        {
                            MessageBox.Show("No rows were updated. Please check the user ID.");
                        }
                    }
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while updating the remaining limit." + ex.Message);
            }
        }

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
                string fileName = $"{StudentId}_{StudentUsername}_{DateTime.Now:yyyyMMddHHmmss}{ext}";

                bool sent = await SendAuthenticationPhotoToAdmin(imageBytes, fileName);
                if (!sent) return;

                string adminUnc = SettingsManager.Current.AdminSharedUnc;
                string subfolder = SettingsManager.Current.AdminPhotoSubfolder;
                string fullUncPath = Path.Combine(adminUnc, subfolder, fileName);

                string connStr = SettingsManager.Current.GetConnectionString();
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = @"UPDATE user_credential 
                                     SET authentication_photo = @path 
                                     WHERE username = @username";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@path", fullUncPath);
                        cmd.Parameters.AddWithValue("@username", StudentUsername);
                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Authentication photo sent to admin successfully.");
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error sending authentication photo: " + ex.Message);
            }
        }

        private async Task<bool> SendAuthenticationPhotoToAdmin(byte[] imageBytes, string fileName)
        {
            try
            {
                string adminIp = SettingsManager.Current.AdminIp;
                int adminPort = SettingsManager.Current.AdminPhotoPort;
                string subfolder = SettingsManager.Current.AdminPhotoSubfolder;

                using (TcpClient client = new TcpClient())
                {
                    var connectTask = client.ConnectAsync(adminIp, adminPort);
                    var timeout = Task.Delay(5000);
                    if (await Task.WhenAny(connectTask, timeout) == timeout)
                    {
                        MessageBox.Show($"Admin ({adminIp}:{adminPort}) not reachable (timeout).");
                        return false;
                    }

                    using (NetworkStream stream = client.GetStream())
                    using (BinaryWriter writer = new BinaryWriter(stream))
                    {
                        writer.Write(subfolder);
                        writer.Write(fileName);
                        writer.Write(imageBytes.Length);
                        writer.Write(imageBytes);
                        writer.Flush();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("SendAuthenticationPhotoToAdmin error: " + ex.Message);
                MessageBox.Show("Failed to send photo to admin: " + ex.Message);
                return false;
            }
        }
    }
}