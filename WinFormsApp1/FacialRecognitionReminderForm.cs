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
                            CustomMessageBox.Show("Remaining limit updated successfully.",
                                "Success", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Information);
                        }
                        else
                        {
                            CustomMessageBox.Show("No rows were updated. Please check the user ID.",
                                "Update Failed", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Warning);
                        }
                    }
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show("An error occurred while updating the remaining limit.\n\n" + ex.Message,
                    "Error", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Error);
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
                            CustomMessageBox.Show("Remaining limit updated successfully.",
                                "Success", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Information);
                        }
                        else
                        {
                            CustomMessageBox.Show("No rows were updated. Please check the user ID.",
                                "Update Failed", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Warning);
                        }
                    }
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show("An error occurred while updating the remaining limit.\n\n" + ex.Message,
                    "Error", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Error);
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
                CustomMessageBox.Show("Upload an image first!",
                    "Validation", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Warning);
                return;
            }

            try
            {
                byte[] imageBytes = await File.ReadAllBytesAsync(AuthenticationPhoto);

                string ext = Path.GetExtension(AuthenticationPhoto);
                string fileName = $"{StudentId}_{StudentUsername}_{DateTime.Now:yyyyMMddHHmmss}{ext}";

                bool sent = await SendAuthenticationPhotoToAdmin(imageBytes, fileName);
                if (!sent) return;

                // Build the UNC path that the ADMIN saved to
                string saveRoot = SettingsManager.Current.SaveFolder;
                string authSub = SettingsManager.Current.AuthPhotoSubfolder ?? "";
                string uncPath;

                if (!string.IsNullOrEmpty(saveRoot) && saveRoot.StartsWith(@"\\"))
                {
                    // SaveFolder is already UNC — just append
                    uncPath = Path.Combine(saveRoot, authSub, fileName);
                }
                else
                {
                    // SaveFolder is local — build UNC from sanitized ServerIp
                    string ip = CleanIp(SettingsManager.Current.ServerIp);
                    string shared = !string.IsNullOrEmpty(saveRoot)
                                    ? new DirectoryInfo(saveRoot).Name
                                    : "SharedFolder";
                    uncPath = $@"\\{ip}\{shared}\{authSub}\{fileName}";
                }

                Console.WriteLine("authPhoto uncPath = " + uncPath);

                string connStr = SettingsManager.Current.GetConnectionString();
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = @"UPDATE user_credential 
                             SET authentication_photo = @path 
                             WHERE username = @username";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@path", uncPath);
                        cmd.Parameters.AddWithValue("@username", StudentUsername);
                        cmd.ExecuteNonQuery();
                    }
                }

                CustomMessageBox.Show("Authentication photo sent to admin successfully.",
                    "Success", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Information);
                this.Close();
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show("Error sending authentication photo:\n\n" + ex.Message,
                    "Error", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Error);
            }
        }

        // Strip "\\", trailing \, and junk like "(null)" from the ServerIp string
        private string CleanIp(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "localhost";

            string s = raw.Trim()
                          .Replace("(null)", "")
                          .Replace(" ", "")
                          .TrimStart('\\')
                          .TrimEnd('\\');

            // If it's like "192.168.100.4\SharedFolder" — take only the IP part
            int slash = s.IndexOf('\\');
            if (slash > 0) s = s.Substring(0, slash);

            return s;
        }

        private async Task<bool> SendAuthenticationPhotoToAdmin(byte[] imageBytes, string fileName)
        {
            try
            {
                string adminIp = CleanIp(SettingsManager.Current.ServerIp);
                int adminPort = SettingsManager.Current.FileTransferPort;

                Console.WriteLine($"[AUTH SEND] {adminIp}:{adminPort}, file={fileName}, bytes={imageBytes.Length}");

                using (TcpClient client = new TcpClient())
                {
                    var connectTask = client.ConnectAsync(adminIp, adminPort);
                    var timeoutTask = Task.Delay(5000);
                    var completed = await Task.WhenAny(connectTask, timeoutTask);

                    if (completed == timeoutTask)
                    {
                        Console.WriteLine($"Admin ({adminIp}:{adminPort}) not reachable (timeout).");
                        return false;
                    }

                    await connectTask;

                    if (!client.Connected)
                    {
                        Console.WriteLine($"Admin ({adminIp}:{adminPort}) refused the connection.");
                        return false;
                    }

                    using (NetworkStream stream = client.GetStream())
                    using (BinaryWriter writer = new BinaryWriter(stream))
                    {
                        writer.Write(fileName);          // <- admin treats 1st string as filename
                        writer.Write(imageBytes.Length);
                        writer.Write(imageBytes);
                        writer.Flush();
                    }
                }
                return true;
            }
            catch (SocketException sex)
            {
                Console.WriteLine($"Network error: {sex.SocketErrorCode} — {sex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine("SendAuthenticationPhotoToAdmin error: " + ex.Message);
                return false;
            }
        }
    }
}