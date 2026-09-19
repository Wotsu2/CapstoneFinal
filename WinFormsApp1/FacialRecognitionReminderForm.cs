using DevExpress.Pdf.Native;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace WinFormsApp1
{
    public partial class FacialRecognitionReminderForm : Form
    {
        private int StudentId;
        private string AuthenticationPhoto;
        private string SaveAuthenticationPhoto;
        private string StudentUsername;
        public FacialRecognitionReminderForm(int StudentID, string Username)
        {
            InitializeComponent();
            StudentId = StudentID;
            StudentUsername = Username;
            InitializeGetRemainingLimit();
            initializeCloseExitButton();
            InitializeAuthenticationSaveDirectory();
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
                                int remainingLimit = reader.GetInt32("remaining_limit");
                                lblRemainingLimit.Text = $"*{remainingLimit} remaining";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while fetching the remaining limit." + ex.Message);
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
                                int remainingLimit = reader.GetInt32("remaining_limit");
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
                MessageBox.Show("An error occurred while fetching the remaining limit." + ex.Message);
            }
        }

        //string query = $"UPDATE professor_attendance SET `{DateToday}` = @status, present = COALESCE(present, 0) + 1 WHERE student_id = @student_id ";
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

        private void btnSubmitAuthenticationPhoto_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(AuthenticationPhoto))
            {
                MessageBox.Show("Upload an image first!");
                return;
            }

            string connStr = SettingsManager.Current.GetConnectionString();

            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = @"UPDATE user_credential 
                                     SET authentication_photo = @authentication_photo 
                                     WHERE username = @username";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        // Save the image to the designated folder
                        string fileName = Path.GetFileName(AuthenticationPhoto);
                        string destinationPath = Path.Combine(SaveAuthenticationPhoto, fileName);
                        File.Copy(AuthenticationPhoto, destinationPath, true);
                        cmd.Parameters.AddWithValue("@authentication_photo", destinationPath);
                        cmd.Parameters.AddWithValue("@username", StudentUsername);
                        cmd.ExecuteNonQuery();
                    }
                    InitializeAuthenticationSaveDirectory();
                    MessageBox.Show("Profile picture updated successfully.");
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error updating profile picture: " + ex.Message);
            }
        }
        private void InitializeAuthenticationSaveDirectory()
        {
            // Create a "Images" folder inside the solution directory
            string solutionDirectory = AppDomain.CurrentDomain.BaseDirectory;
            SaveAuthenticationPhoto = Path.Combine(solutionDirectory, "StudentAuthenticationPhoto");

            if (!Directory.Exists(SaveAuthenticationPhoto))
            {
                Directory.CreateDirectory(SaveAuthenticationPhoto);
            }
        }
    }
}
