using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using System.Windows.Forms;
using System.Linq;

using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace WinFormsApp1
{
    public partial class AdminForm : Form
    {

        private string Date = DateTime.Now.ToString("MM/dd/yyyy");        // 08/06/2026
        private string Time = DateTime.Now.ToString("hh:mm:ss tt");       // 12:14:32 PM
        int pendingNewUser = 0;

        private string saveFolder = @"C:\ReceivedFileFolder"; // Should be Empty for setting to configure
        private int fileCount;

        private string currentFolder;
        private Stack<string> folderHistory = new Stack<string>();

        public AdminForm()
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

            if (!Directory.Exists(saveFolder))
                Directory.CreateDirectory(saveFolder); // if the folder is not existed then it will create it self 

            StartFileServer();
            fileCount = Directory.GetFiles(saveFolder).Length;
            lblFilesToday.Text = fileCount.ToString();

            //File Management Panel
            lsServerFolderSetup();
            LoadServerFolder(saveFolder, addToHistory: false);

            //WorkStation Management panel
        }

        // Button For Panel to Show Up //
        private void DashboardButton_Click(object sender, EventArgs e)
        {
            DashboardPanel.Visible = true;
            UserManagementPanel.Visible = false;
            panelFileManagement.Visible = false;
            panelWorkStation.Visible = false;
            LoadStorageProgress();
        }
        private void UserManagementButton_Click(object sender, EventArgs e)
        {
            UserManagementPanel.Visible = true;
            DashboardPanel.Visible = false;
            panelFileManagement.Visible = false;
            panelWorkStation.Visible = false;
        }
        private void fileManagementBtn_Click(object sender, EventArgs e)
        {
            panelFileManagement.Visible = true;
            DashboardPanel.Visible = false;
            UserManagementPanel.Visible = false;
            panelWorkStation.Visible = false;

        }
        private void WorkStationButton_Click(object sender, EventArgs e)
        {
            panelWorkStation.Visible = true;
            panelFileManagement.Visible = false;
            DashboardPanel.Visible = false;
            UserManagementPanel.Visible = false;
        }



        // for User Management Panel //
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



        // Searching a user in datagrid //
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
        private void SearchButton_TextChanged(object sender, EventArgs e)
        {
            LoadUser(SearchButton.Text);
        }



        // Creation of Account //
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



        // For Role if the admin pick the professor or Admin it will ignore the other information //
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



        // Context Menu on DataGridView //
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



        // Update or Edit the User Information only //
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



        // Just Style of Activation and Deactivation Noting More //
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



        // Deactivate the User you want to Deactivate //
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



        // Delete the Account of User you want to delete //
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



        // Reset the password of the user who you want to reset //
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



        //Dashboard Panel Notifier Button //
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



        // Return the Total User even the Professor //
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



        // For Load the Storage //
        private void LoadStorageProgress()
        {
            DriveInfo drive = new DriveInfo("C:\\"); // Put the Storage you want to show Exampke: "C:\\"

            long totalSpace = drive.TotalSize;
            long freeSpace = drive.AvailableFreeSpace;
            long usedSpace = totalSpace - freeSpace;

            double totalGb = Math.Round((double)totalSpace / (1024.0 * 1024.0 * 1024.0), 2);
            double usedGb = Math.Round((double)usedSpace / (1024.0 * 1024.0 * 1024.0), 2);
            double freeGb = Math.Round((double)freeSpace / (1024.0 * 1024.0 * 1024.0), 2);

            int percentage = (int)((double)usedSpace / totalSpace * 100);
            storageProgressBar.Minimum = 0;
            storageProgressBar.Maximum = 100;
            storageProgressBar.Value = percentage;

            lblUsedStorage.Text = $"{usedGb} GB";
            lblFreeStorage.Text = $"{freeGb} GB";
            lblTotalStorage.Text = $"{totalGb} Total GB";

            StorageAutoLoader(usedGb, freeGb, totalGb, percentage);
        }

        // For File Management Panel //

        private void lsServerFolderSetup()
        {
            lvServerFolder.View = View.LargeIcon;
            lvServerFolder.LargeImageList = imgListIcon;
            lvServerFolder.MultiSelect = false;
        }

        private void LoadServerFolder(string path, bool addToHistory = true)
        {
            if (addToHistory && !string.IsNullOrEmpty(currentFolder))
            {
                folderHistory.Push(currentFolder);
            }

            currentFolder = path;
            lvServerFolder.Items.Clear();
            imgListIcon.Images.Clear();
            int imageIndex = 0;

            //To Show Folder
            foreach (string dir in Directory.GetDirectories(path))
            {
                imgListIcon.Images.Add(Properties.Resources.Vector_Folder);
                ListViewItem item = new ListViewItem(Path.GetFileName(dir), imageIndex);
                item.Tag = dir;
                lvServerFolder.Items.Add(item);
                imageIndex++;
            }

            //to Show File

            foreach (string file in Directory.GetFiles(path))
            {
                Icon fileIcon = Icon.ExtractAssociatedIcon(file);
                imgListIcon.Images.Add(Properties.Resources.Vector_Item);

                ListViewItem item = new ListViewItem(Path.GetFileName(file), imageIndex);
                item.Tag = file;
                lvServerFolder.Items.Add(item);
                imageIndex++;

            }

            BtnBack.Enabled = folderHistory.Count > 0;
        }

        private void lvServerFolder_DoubleClick(object sender, EventArgs e)
        {
            if (lvServerFolder.SelectedItems.Count == 0) return;

            string path = lvServerFolder.SelectedItems[0].Tag.ToString();

            if (Directory.Exists(path))
                LoadServerFolder(path);
            else if (File.Exists(path))
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
        }

        private void BtnBack_Click(object sender, EventArgs e)
        {
            if (folderHistory.Count > 0)
            {
                string previousFolder = folderHistory.Pop();
                LoadServerFolder(previousFolder, addToHistory: false);
            }
        }
        private void OnFileReceived(string fileName)
        {
            fileSubmittedCount++;
            lblFileSubmittedCount.Text = fileSubmittedCount.ToString();
            lblFilesToday.Text = fileSubmittedCount.ToString();
        }

        private void StorageAutoLoader(double usedGb, double freeGb, double totalGb, int percentage)
        {
            string usedGbStr = usedGb.ToString();
            string freeGbStr = freeGb.ToString();
            string totalGbStr = totalGb.ToString();

            progressStorageBar.Minimum = 0;
            progressStorageBar.Maximum = 100;
            progressStorageBar.Value = percentage;

            lblTotalGb2.Text = $"{totalGb} GB";
            lblStorageUsed.Text = $"{usedGb} GB";
            lblStorageFree.Text = $"{freeGb} GB";
            fileCount = Directory.GetFiles(saveFolder).Length;
            lblTotalFiles2.Text = $"{fileCount.ToString()} Total Files";
            lblFileToday2.Text = $"{fileSubmittedCount.ToString()} Files Today";
        }



        // For WorkStation Connection Server //

        private TcpListener listener;
        private TcpListener fileListener;
        private int fileSubmittedCount = 0;
        private int WorkStationNum = 0;
        private Dictionary<string, (Button mainBtn, Button miniBtn)> workstationButtons
        = new Dictionary<string, (Button, Button)>();

        private TcpListener screenListener;
        private PictureBox pictureBoxScreen;

        private async void StartServer()
        {
            listener = new TcpListener(IPAddress.Any, 5000);
            listener.Start();

            lblWorkstation.Text = "0";
            int registeredCount = workstationButtons.Count;

            while (true)
            {
                TcpClient client = await listener.AcceptTcpClientAsync();

                string pcId = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();

                Button MainPcButton = null;
                Button MiniPcButton = null;

                if (workstationButtons.ContainsKey(pcId))
                {
                    (MainPcButton, MiniPcButton) = workstationButtons[pcId];

                    if (this.InvokeRequired)
                    {
                        this.Invoke(new Action(() =>
                        {
                            MainPcButton.BackColor = Color.LightGreen;
                            MiniPcButton.BackColor = Color.LightGreen;
                            UpdateConnectedCount();
                        }));
                    }
                    else
                    {
                        MainPcButton.BackColor = Color.LightGreen;
                        MiniPcButton.BackColor = Color.LightGreen;
                        UpdateConnectedCount();
                    }
                }
                else
                {
                    // First time seeing this PC — create buttons
                    if (this.InvokeRequired)
                    {
                        this.Invoke(new Action(() =>
                        {
                            var (mainBtn, miniBtn) = OnWorkStationConnected();
                            MainPcButton = mainBtn;
                            MiniPcButton = miniBtn;
                        }));
                    }
                    else
                    {
                        var (mainBtn, miniBtn) = OnWorkStationConnected();
                        MainPcButton = mainBtn;
                        MiniPcButton = miniBtn;
                    }

                    workstationButtons[pcId] = (MainPcButton, MiniPcButton);

                    if (this.InvokeRequired)
                    {
                        this.Invoke(new Action(() =>
                        {
                            lblTotalComputer.Text = workstationButtons.Count.ToString();
                            UpdateConnectedCount();
                        }));
                    }
                    else
                    {
                        lblTotalComputer.Text = workstationButtons.Count.ToString();
                        UpdateConnectedCount();
                    }


                }


                _ = MonitorDisconnected(client, MainPcButton, MiniPcButton);

            }
        }

        private (Button mainBtn, Button miniBtn) OnWorkStationConnected()
        {
            int currentCount = int.Parse(lblWorkstation.Text);
            currentCount++;
            lblWorkstation.Text = currentCount.ToString();

            WorkStationNum++;

            Button MiniPcButton = new Button();
            MiniPcButton.Text = "PC " + WorkStationNum.ToString();
            MiniPcButton.Image = Properties.Resources.material_symbols_light_computer_outline_rounded;
            MiniPcButton.Height = 100;
            MiniPcButton.Width = 80;
            MiniPcButton.Margin = new Padding(5);
            MiniPcButton.BackColor = Color.LightGreen;
            MiniPcButton.Tag = WorkStationNum;

            Button MainPcButton = new Button();
            MainPcButton.Text = "PC " + WorkStationNum.ToString();
            MainPcButton.Image = Properties.Resources.material_symbols_light_computer_outline_rounded;
            MainPcButton.Height = 180;
            MainPcButton.Width = 131;
            MainPcButton.Margin = new Padding(5);
            MainPcButton.BackColor = Color.LightGreen;
            MainPcButton.Tag = WorkStationNum;

            Label wsLabelName = new Label();
            wsLabelName.Text = "Meriales";
            wsLabelName.Location = new Point(14, 115);
            wsLabelName.AutoSize = false;
            wsLabelName.BackColor = Color.Transparent;
            wsLabelName.ForeColor = Color.Black;
            wsLabelName.TextAlign = ContentAlignment.MiddleCenter;
            wsLabelName.Font = MainPcButton.Font;

            Label wsLabelDate = new Label();
            wsLabelDate.Text = Date;
            wsLabelDate.Location = new Point(14, 135);
            wsLabelDate.AutoSize = false;
            wsLabelDate.BackColor = Color.Transparent;
            wsLabelDate.ForeColor = Color.Black;
            wsLabelDate.TextAlign = ContentAlignment.MiddleCenter;
            wsLabelDate.Font = MainPcButton.Font;

            Label wsLabelTime = new Label();
            wsLabelTime.Text = Time;
            wsLabelTime.Location = new Point(14, 155);
            wsLabelTime.AutoSize = false;
            wsLabelTime.BackColor = Color.Transparent;
            wsLabelTime.ForeColor = Color.Black;
            wsLabelTime.TextAlign = ContentAlignment.MiddleCenter;
            wsLabelTime.Font = MainPcButton.Font;


            MainPcButton.Controls.Add(wsLabelName);
            MainPcButton.Controls.Add(wsLabelDate);
            MainPcButton.Controls.Add(wsLabelTime);

            MiniWorkstationFLP.Controls.Add(MiniPcButton);
            MainWorkstationFLP.Controls.Add(MainPcButton);

            return (MainPcButton, MiniPcButton);

        }

        private async Task MonitorDisconnected(TcpClient client, Button MainPcButton, Button MiniPcButton)
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
                {
                    this.Invoke(new Action(() =>
                    {
                        MainPcButton.BackColor = Color.Red;
                        MiniPcButton.BackColor = Color.Red;
                        UpdateConnectedCount();

                        int count = int.Parse(lblWorkstation.Text);
                        if (count > 0) count--;
                        lblWorkstation.Text = count.ToString();
                    }));
                }
                else
                {
                    MainPcButton.BackColor = Color.Red;
                    MiniPcButton.BackColor = Color.Red;
                    UpdateConnectedCount();

                    int count = int.Parse(lblWorkstation.Text);
                    if (count > 0) count--;
                    lblWorkstation.Text = count.ToString();

                }
                client.Close();
            }
        }
        private void UpdateConnectedCount()
        {
            int connectedCount = workstationButtons.Values
            .Count(pair => pair.mainBtn.BackColor == Color.LightGreen);

            int disconnectedCount = workstationButtons.Count - connectedCount;

            lblOnline.Text = connectedCount.ToString();
            lblOffline.Text = disconnectedCount.ToString();
        }




        // For Receiving File Connection to Server //
        private async void StartFileServer()
        {
            fileListener = new TcpListener(IPAddress.Any, 5001);
            fileListener.Start();

            while (true)
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
                    byte[] fileBytes = reader.ReadBytes(fileLength);

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

        private async void StartScreenListerner()
        {
            screenListener = new TcpListener(IPAddress.Any, 5002);
            screenListener.Start();
            while (true)
            {
                TcpClient client = await screenListener.AcceptTcpClientAsync();
                _ = ReceiveScreenStream(client);
            }
        }

        private async Task ReceiveScreenStream(TcpClient client)
        {
            NetworkStream stream = client.GetStream();

            try
            {
                while (client.Connected)
                {
                    byte[] lengthBuffer = new byte[4];
                    int read = await stream.ReadAsync(lengthBuffer, 0, 4);
                    if (read == 0) break;

                    int imgLength = BitConverter.ToInt32(lengthBuffer, 0);
                    byte[] imageBuffer = new byte[imgLength];

                    int totalRead = 0;
                    while (totalRead < imgLength)
                    {
                        int bytesRead = await stream.ReadAsync(imageBuffer, totalRead, imgLength - totalRead);
                        if (bytesRead == 0) break;
                        totalRead += bytesRead;
                    }
                    using (MemoryStream ms = new MemoryStream(imageBuffer))
                    {
                        Image frame = Image.FromStream(ms);

                        if (this.InvokeRequired)
                            this.Invoke(new Action(() => pictureBoxScreen.Image = frame));
                        else
                            pictureBoxScreen.Image = frame;
                    }
                }

            }
            catch { }
            finally
            {
                client.Close();
            }
        }

        private void ViewScreenbtn_Click(object sender, EventArgs e)
        {
            Button btn = (Button)sender;
            string workstationId = btn.Tag.ToString(); // or however you identify which PC

            // Open a viewer form/panel showing that student's screen
            ScreenViewerForm viewer = new ScreenViewerForm(workstationId);
            viewer.Show();
        }
    }
}
