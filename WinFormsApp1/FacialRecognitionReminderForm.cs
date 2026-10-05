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

        // =========================================================
        // SUBMIT AUTHENTICATION PHOTO
        // Sends "AUTH_PHOTO" command header so the server can route it.
        // Waits for the server's reply to get the actual saved path.
        // =========================================================
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

                string savedPath = await SendAuthenticationPhotoToAdmin(imageBytes, fileName);
                if (string.IsNullOrEmpty(savedPath))
                {
                    CustomMessageBox.Show("Failed to send authentication photo to server.",
                        "Send Error", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Warning);
                    return;
                }

                Console.WriteLine("authPhoto savedPath = " + savedPath);

                string connStr = SettingsManager.Current.GetConnectionString();
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = @"UPDATE user_credential 
                                     SET authentication_photo = @path 
                                     WHERE username = @username";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@path", savedPath);
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

            int slash = s.IndexOf('\\');
            if (slash > 0) s = s.Substring(0, slash);

            return s;
        }

        // =========================================================
        // Send auth photo and RETURN the server's saved path.
        // Sends "AUTH_PHOTO" as the first string (command header).
        // =========================================================
        private async Task<string> SendAuthenticationPhotoToAdmin(byte[] imageBytes, string fileName)
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
                        return null;
                    }

                    await connectTask;

                    if (!client.Connected)
                    {
                        Console.WriteLine($"Admin ({adminIp}:{adminPort}) refused the connection.");
                        return null;
                    }

                    string returnedPath = null;

                    using (NetworkStream stream = client.GetStream())
                    using (BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
                    {
                        // =========================================================
                        // Command header FIRST so the server knows what this is
                        // =========================================================
                        writer.Write("AUTH_PHOTO");
                        writer.Write(fileName);
                        writer.Write(imageBytes.Length);
                        writer.Write(imageBytes);
                        writer.Flush();

                        // =========================================================
                        // Wait for the server to reply with the saved path
                        // =========================================================
                        try
                        {
                            using (var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true))
                            {
                                returnedPath = reader.ReadString();
                                Console.WriteLine("[AUTH SEND] Server replied: " + returnedPath);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("[AUTH SEND] No reply from server: " + ex.Message);
                        }
                    }

                    return returnedPath;
                }
            }
            catch (SocketException sex)
            {
                Console.WriteLine($"Network error: {sex.SocketErrorCode} — {sex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine("SendAuthenticationPhotoToAdmin error: " + ex.Message);
                return null;
            }
        }
    }
}