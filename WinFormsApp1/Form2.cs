using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using System.Windows.Forms;

using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace WinFormsApp1
{
    public partial class Form2 : Form
    {

        int pendingNewUser = 0;
        private TcpListener listener;

        //For Submitting File
        private TcpListener fileListener;
        private int fileSubmittedCount = 0;
        private string saveFolder = @"";
        public Form2()
        {

            InitializeComponent();
            LoadUser();
            ConfirmUpdateButton.Enabled = false;

            //Dashboard Panel
            TotalUserFunction();
            NotifierBtn.Visible = false;
            TotalUserLabel.Text = TotalUserFunction().ToString();
            LoadStorageProgress();
            StartServer();
        }

        private void DashboardButton_Click(object sender, EventArgs e)
        {
            DashboardPanel.Visible = true;
            UserManagementPanel.Visible = false;
            LoadStorageProgress();
        }
        private void UserManagementButton_Click(object sender, EventArgs e)
        {
            UserManagementPanel.Visible = true;
            DashboardPanel.Visible = false;
        }


        private void UserListButton_Click(object sender, EventArgs e)
        {
            UserListPanel.Visible = true;
            CreateAccountPanel.Visible = false;
            LoadUser();
        }

        private void AccountCreateButton_Click(object sender, EventArgs e)
        {
            CreateAccountPanel.Visible = true;
            UserListPanel.Visible = false;
        }

        private void LoadUser(string filter = "")
        {
            string connStr = "Server=localhost;Port=3306;Database=cdsga_hub;Uid=root;Pwd=;";
            UserDataList.ReadOnly = true;
            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    string query = "SELECT u.user_id, u.user_role, u.status, " +
                       "i.lastname, i.firstname, i.middlename, " +
                       "i.emails, i.school_years, i.sections, i.courses " +
                       "FROM users_credential_role u " +
                       "LEFT JOIN user_informations i ON u.user_id = i.user_id";

                    if (!string.IsNullOrEmpty(filter))
                    {
                        query += " WHERE u.user_id LIKE @f1 OR i.lastname LIKE @f2 OR i.firstname LIKE @f3";
                    }

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        if (!string.IsNullOrEmpty(filter))
                        {
                            string f = "%" + filter + "%";
                            cmd.Parameters.AddWithValue("@f1", f);
                            cmd.Parameters.AddWithValue("@f2", f);
                            cmd.Parameters.AddWithValue("@f3", f);
                        }
                        using (var adapter = new MySqlDataAdapter(cmd))
                        {
                            var dt = new DataTable();
                            adapter.Fill(dt);
                            UserDataList.DataSource = dt;

                            UserDataList.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                        }
                    }
                    ActivationStyle();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private void ClearText()
        {
            IdNumberText.Clear();
            FirstnameText.Clear();
            LastnameText.Clear();
            MiddlenameText.Clear();
            EmailText.Clear();
            ContextRoleText.SelectedIndex = -1;
            ContextYearText.SelectedIndex = -1;
            ContextSectionText.SelectedIndex = -1;
            ContextRoleText.SelectedIndex = -1;
        }

        private void CreateButton_Click(object sender, EventArgs e)
        {
            string connStr = "Server=localhost;Port=3306;Database=cdsga_hub;Uid=root;Pwd=;";

            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    string Insertquery2 = @"INSERT INTO users_credential_role (user_id, pass_word, user_role, status)
                                            VALUES (@User_Id, @Password, @UserRole, @Status)";

                    using (MySqlCommand cmd2 = new MySqlCommand(Insertquery2, conn))
                    {
                        cmd2.Parameters.AddWithValue("@User_Id", IdNumberText.Text.Trim());
                        cmd2.Parameters.AddWithValue("@Password", "12345678");
                        cmd2.Parameters.AddWithValue("@UserRole", ContextRoleText.Text.Trim());
                        cmd2.Parameters.AddWithValue("@Status", "Activated");
                        cmd2.ExecuteNonQuery();
                    }

                    string Insertquery = @"
                                    INSERT INTO user_informations 
                                        (user_id, lastname, firstname, middlename, emails, school_years, sections, courses) 
                                    VALUES 
                                        (@user_id, @lastname, @firstname, @middlename, @emails, @school_yr, @section, @course)";

                    using (MySqlCommand cmd = new MySqlCommand(Insertquery, conn))
                    {
                        cmd.Parameters.AddWithValue("@user_id", IdNumberText.Text.Trim());
                        cmd.Parameters.AddWithValue("@lastname", LastnameText.Text.ToUpper());
                        cmd.Parameters.AddWithValue("@firstname", FirstnameText.Text.ToUpper());
                        cmd.Parameters.AddWithValue("@middlename", MiddlenameText.Text.ToUpper());
                        cmd.Parameters.AddWithValue("@emails", EmailText.Text.Trim());
                        cmd.Parameters.AddWithValue("@school_yr", ContextYearText.Text.ToUpper());
                        cmd.Parameters.AddWithValue("@section", ContextSectionText.Text.ToUpper());
                        cmd.Parameters.AddWithValue("@course", ContextCourseText.Text.ToUpper());
                        cmd.ExecuteNonQuery();
                    }

                    MessageBox.Show("Account Successfuly Created!");
                    ClearText();
                    LoadUser();

                    OnNewUserCreated(1);

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private void SearchButton_TextChanged(object sender, EventArgs e)
        {
            LoadUser(SearchButton.Text);
        }

        private void ContextRoleText_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ContextRoleText.Text == "Professor")
            {
                ContextYearText.Enabled = false;
                ContextSectionText.Enabled = false;
                ContextCourseText.Enabled = false;
            }
            else if (ContextRoleText.Text == "Student")
            {
                ContextYearText.Enabled = true;
                ContextSectionText.Enabled = true;
                ContextCourseText.Enabled = true;
            }
            else if (ContextCourseText.Text == "Admin")
            {
                ContextYearText.Enabled = false;
                ContextSectionText.Enabled = false;
                ContextCourseText.Enabled = false;
            }
        }

        private void UserDataList_MouseDown(object sender, MouseEventArgs e)
        {

            if (e.Button == MouseButtons.Right)
            {
                var rowIndex = UserDataList.HitTest(e.X, e.Y).RowIndex;

                if (rowIndex >= 0)
                {

                    UserDataList.Rows[rowIndex].Selected = true;
                    contextMenuStrip1.Show(UserDataList, e.Location);
                }
            }
        }

        private void UpdateButton_Click(object sender, EventArgs e)
        {
            ConfirmUpdateButton.Enabled = true;
            UserDataList.ReadOnly = false;
        }
        private void ConfirmUpdateButton_Click(object sender, EventArgs e)
        {

            if (UserDataList.SelectedRows.Count == 0) return;

            DataGridViewRow row = UserDataList.SelectedRows[0];

            string id = row.Cells["user_id"].Value.ToString();
            string lastname = row.Cells["lastname"].Value.ToString();
            string firstname = row.Cells["firstname"].Value.ToString();
            string middlename = row.Cells["middlename"].Value.ToString();
            string email = row.Cells["emails"].Value.ToString();
            string school_year = row.Cells["school_years"].Value.ToString();
            string section = row.Cells["sections"].Value.ToString();
            string course = row.Cells["courses"].Value.ToString();

            UpdateDatabase(id, lastname, firstname, middlename, email, school_year, section, course);

        }

        private void UpdateDatabase(string id, string lastname, string firstname, string middlename, string email, string school_yr, string section, string course)
        {
            string connStr = "Server=localhost;Port=3306;Database=cdsga_hub;Uid=root;Pwd=;";

            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    string query = @"UPDATE user_informations 
                                    SET lastname = @ln, firstname = @fn, middlename = @mn, emails = @email,
                                    school_years = @school_yr, sections = @section, courses = @course
                                    WHERE user_id = @user_id";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@user_id", id);
                        cmd.Parameters.AddWithValue("@ln", lastname);
                        cmd.Parameters.AddWithValue("@fn", firstname);
                        cmd.Parameters.AddWithValue("@mn", middlename);
                        cmd.Parameters.AddWithValue("@email", email);
                        cmd.Parameters.AddWithValue("@school_yr", school_yr);
                        cmd.Parameters.AddWithValue("@section", section);
                        cmd.Parameters.AddWithValue("@course", course);
                        cmd.ExecuteNonQuery();
                    }
                    MessageBox.Show("Update is Succesfully");
                    LoadUser();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private void ActivationStyle()
        {
            foreach (DataGridViewRow row in UserDataList.Rows)
            {
                string status = row.Cells["status"].Value?.ToString();

                if (status == "Deactivated")
                {
                    row.Cells["status"].Style.ForeColor = Color.Red;
                    row.Cells["status"].Style.Font = new Font(UserDataList.Font, FontStyle.Bold);
                }
                else if (status == "Activated")
                {
                    row.Cells["status"].Style.ForeColor = Color.Green;
                    row.Cells["status"].Style.Font = new Font(UserDataList.Font, FontStyle.Bold);
                }
            }
        }

        private void DeactivateButton_Click(object sender, EventArgs e)
        {
            string connStr = "Server=localhost;Port=3306;Database=cdsga_hub;Uid=root;Pwd=;";

            if (UserDataList.SelectedRows.Count == 0) return;

            DataGridViewRow row = UserDataList.SelectedRows[0];
            string id = row.Cells["user_id"].Value.ToString();
            string status = row.Cells["status"].Value.ToString();
            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    string query = "UPDATE users_credential_role SET status = @status WHERE user_id = @user_id";

                    if (status == "Activated")
                    {
                        using (var cmd = new MySqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@user_id", id);
                            cmd.Parameters.AddWithValue("@status", "Deactivated");
                            cmd.ExecuteNonQuery();

                            MessageBox.Show("Succesfully Deactivated");
                        }
                    }
                    else if (status == "Deactivated")
                    {
                        using (var cmd = new MySqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@user_id", id);
                            cmd.Parameters.AddWithValue("@status", "Activated");
                            cmd.ExecuteNonQuery();
                            MessageBox.Show("Succesfully Activated");
                        }
                    }

                    LoadUser();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private void DeleteButton_Click(object sender, EventArgs e)
        {
            if (UserDataList.SelectedRows.Count == 0) return; // silently ignore if nothing selected

            DataGridViewRow row = UserDataList.SelectedRows[0];
            string id = row.Cells["user_id"].Value.ToString();

            DialogResult confirm = MessageBox.Show(
                "Do you really want to delete this account?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (confirm != DialogResult.Yes) return;

            string connStr = "Server=localhost;Port=3306;Database=cdsga_hub;Uid=root;Pwd=;";

            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    string query = "DELETE FROM users_credential_role WHERE user_id = @user_id";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@user_id", id);
                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Account deleted successfully.");
                LoadUser();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private void ResetPasswordButton_Click(object sender, EventArgs e)
        {
            string connStr = "Server=localhost;Port=3306;Database=cdsga_hub;Uid=root;Pwd=;";

            if (UserDataList.SelectedRows.Count == 0) return; // silently ignore if nothing selected

            DataGridViewRow row = UserDataList.SelectedRows[0];
            string id = row.Cells["user_id"].Value.ToString();

            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    string query = "UPDATE users_credential_role SET pass_word = @pd WHERE user_id = @user_id";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@user_id", id);
                        cmd.Parameters.AddWithValue("@pd", "12345678");
                        cmd.ExecuteNonQuery();
                    }
                    MessageBox.Show("Account Password has Reset");
                    LoadUser();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        //Dashboard Panel
        private void OnNewUserCreated(int num)
        {
            pendingNewUser += num;
            NotifierBtn.Text = pendingNewUser.ToString() + " New";
            NotifierBtn.Visible = true;
        }

        private void NotifierBtn_Click(object sender, EventArgs e)
        {
            MessageBox.Show(pendingNewUser.ToString() + " New Account Created");
            NotifierBtn.Visible = false;
            NotifierBtn.Text = "";
            pendingNewUser = 0;
        }

        private int TotalUserFunction()
        {
            string connStr = "Server=localhost;Port=3306;Database=cdsga_hub;Uid=root;Pwd=;";

            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = "SELECT COUNT(*) FROM users_credential_role";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        return Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return 0;
            }
        }

        private void LoadStorageProgress()
        {
            DriveInfo drive = new DriveInfo("E:\\");

            long totalSpace = drive.TotalSize;
            long freeSpace = drive.AvailableFreeSpace;
            long usedSpace = totalSpace - freeSpace;

            double totalGb = Math.Round((double)totalSpace / (1024.0 * 1024.0 * 1024.0), 2);
            double usedGb = Math.Round((double)usedSpace / (1024.0 * 1024.0 * 1024.0), 2);
            double freelGb = Math.Round((double)freeSpace / (1024.0 * 1024.0 * 1024.0), 2);

            int percentage = (int)((double)usedSpace / totalSpace * 100);
            storageProgressBar.Minimum = 0;
            storageProgressBar.Maximum = 100;
            storageProgressBar.Value = percentage;

            lblUsedStorage.Text = $"{usedGb} GB";
            lblFreeStorage.Text = $"{freelGb} GB";
            lblTotalStorage.Text = $"{totalGb} Total GB";

        }

        private async void StartServer()
        {
            listener = new TcpListener(IPAddress.Any, 5000);
            listener.Start();

            lblWorkstation.Text = "0";

            while (true)
            {
                TcpClient client = await listener.AcceptTcpClientAsync();

                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() => OnWorkStationConnected()));
                }
                else
                {
                    OnWorkStationConnected();

                    _ = MonitorDisconnected(client);
                }
            }
        }

        private void OnWorkStationConnected()
        {
            int currentCount = int.Parse(lblWorkstation.Text);
            currentCount++;
            lblWorkstation.Text = currentCount.ToString();
        }

        private async Task MonitorDisconnected(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1];

            try
            {
                while (client.Connected)
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, 1);
                    if (bytesRead == 0) break;
                }
            }
            catch { }
            finally
            {
                if (this.InvokeRequired)
                    this.Invoke(new Action(() => lblWorkstation.Text = "0"));
                else
                    lblWorkstation.Text = "0";

                client.Close();
            }
        }

        private async void StartFileServer()
        {
            fileListener = new TcpListener(IPAddress.Any, 5001);
            fileListener.Start();

            while(true)
            {
                TcpClient client = await fileListener.AcceptTcpClientAsync();
                _ = HandleFileReceive(client);
            }
        }

        private async Task HandleFileReceive(TcpClient client)
        {
            try
            {
                using (client)
                using (NetworkStream stream = client.GetStream())
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    string fileName = reader.ReadString();
                    int fileLength = reader.ReadInt32();
                    byte[] fileBytes = new byte[fileLength];

                    string savePath = Path.Combine(saveFolder, fileName);
                    File.WriteAllBytes(savePath, fileBytes);

                    if (this.InvokeRequired)
                    {
                        this.Invoke(new Action(() => OnFileReceived(fileName)));
                    }
                    else
                    {
                        OnFileReceived(fileName);
                    }
                }
            }
            catch (Exception ex)
            {
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() => MessageBox.Show("File receive error: " + ex.Message)));
                }

            }
        }

        private void OnFileReceived(string fileName)
        {
            fileSubmittedCount++;
            lblFileSubmittedCount.Text = fileSubmittedCount.ToString();
        }

    }
}
