using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace WinFormsApp1
{
    public partial class AdminForm : Form
    {
        // DASHBOARD ATTRIBUTES //
        private Panel PanelIndicator;

        // USER MANAGEMENT ATTRIBUTES //

        // FILE MANAGEMENT ATTRIBUTES //
        private string currentFolder;
        private Stack<string> folderHistory = new Stack<string>();
        private string saveFolder = @"C:\ReceivedFileFolder";

        // WORKSTATION ATTRIBUTES //
        private TcpListener listener;
        private TcpListener fileListener;
        private int fileSubmittedCount = 0;
        private int WorkStationNum = 0;
        private Dictionary<string, Button> workstationButtons = new Dictionary<string, Button>();
        private Dictionary<string, PictureBox> screenViewers = new Dictionary<string, PictureBox>();
        private TcpListener screenListener;
        private PictureBox pictureBoxScreen;
        private string selectedWorkstationId = "";
        private bool isRunning;


        public AdminForm()
        {
            InitializeComponent();
        }

        private void admindash_Load(object sender, EventArgs e)
        {
            lblTotalUsers.Text = TotalUsers().ToString();

            // USER MANAGEMENT //
            LoadUserData();

            // FILE MANAGEMENT //

            // WORKSTATION ATTRIBUTES //
            StartServer();
            StartScreenListener();
            StartReceivingFileServer(5001);
        }

        private void btnDashboard_Click_1(object sender, EventArgs e)
        {
            panelDashoard.BringToFront();

            navbarStyle.RemoveIndicator(PanelIndicator);
            PanelIndicator = navbarStyle.CreateIndicator(btnDashboard);
        }

        private void btnUserManagement_Click(object sender, EventArgs e)
        {
            pnlUserManagement.BringToFront();

            navbarStyle.RemoveIndicator(PanelIndicator);
            PanelIndicator = navbarStyle.CreateIndicator(btnUserManagement);

            LoadUserData();
        }

        private void btnFileManagement_Click(object sender, EventArgs e)
        {
            pnlFileManagement.BringToFront() ;

            navbarStyle.RemoveIndicator(PanelIndicator);
            PanelIndicator = navbarStyle.CreateIndicator(btnFileManagement);

            lsServerFolderSetup();
            LoadServerFolder(saveFolder, addToHistory: false);
        }

        private void btnWorkstation_Click(object sender, EventArgs e)
        {
            pnlWorkstation.BringToFront();

            navbarStyle.RemoveIndicator(PanelIndicator);
            PanelIndicator = navbarStyle.CreateIndicator(btnWorkstation);
        }

        private void CreateButton_Click(object sender, EventArgs e)
        {
            CreateUser();
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

        private void AccountCreateButton_Click(object sender, EventArgs e)
        {
            pnlCreateAccount.Visible = true;
            pnlUserList.Visible = false;
        }

        private void UserListButton_Click(object sender, EventArgs e)
        {
            LoadUserData();
            pnlUserList.Visible = true;
            pnlCreateAccount.Visible = false;
        }

        private void SearchButton_TextChanged(object sender, EventArgs e)
        {
            LoadUserData(SearchButton.Text);
        }

        private void lvServerFolder_DoubleClick(object sender, EventArgs e)
        {
            doubleClick();
        }

        private void BtnBack_Click(object sender, EventArgs e)
        {
            btnBack();
        }

        //Client to Server Connection//
        private void StartReceivingFileServer(int port = 5001)
        {
            listener = new TcpListener(IPAddress.Any, port);
            Directory.CreateDirectory(saveFolder); // ensure folder exists

            listener.Start();
            isRunning = true;
            Console.WriteLine("Server started, waiting for files...");

            Task.Run(() => ListenForClients());
        }
        private async Task ListenForClients()
        {
            while (isRunning)
            {
                try
                {
                    TcpClient client = await listener.AcceptTcpClientAsync();
                    _ = HandleClientAsync(client); // handle each client without blocking others
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Listener error: " + ex.Message);
                }
            }
        }
        private async Task HandleClientAsync(TcpClient client)
        {
            try
            {
                using (client)
                using (NetworkStream stream = client.GetStream())
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    // Read in the SAME order the client wrote them
                    string fileName = reader.ReadString();
                    int fileLength = reader.ReadInt32();
                    byte[] fileBytes = reader.ReadBytes(fileLength);

                    string savePath = Path.Combine(saveFolder, fileName);
                    await File.WriteAllBytesAsync(savePath, fileBytes);

                    Console.WriteLine($"Received file: {fileName} ({fileLength} bytes)");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error receiving file: " + ex.Message);
            }
        }
        private void StopServer()
        {
            isRunning = false;
            listener?.Stop();
        }

        //User Management Total User//
        private static int TotalUsers()
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
                        int total = Convert.ToInt32(cmd.ExecuteScalar());
                        return total;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return 0;
            }
        }

        //User Management Load User//
        private void LoadUserData(string filter = "")
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
                    //ActivationStyle();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        //User Management Create Account//
        private void CreateUser()
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
                    LoadUserData();

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

        // WORKSTATION FUNCTIONS //
        public void WorkstationButton_Click(object sender, EventArgs e)
        {
            Button clickedButton = (Button)sender;
            string workstationId = clickedButton.Tag.ToString();
            selectedWorkstationId = workstationId;
            AddScreenViewer(workstationId);
        }

        private async void StartServer()
        {
            listener = new TcpListener(IPAddress.Any, 5000);
            listener.Start();

            lblTotalWorkstations.Text = "0";
            int registeredCount = workstationButtons.Count;

            while (true)
            {
                TcpClient client = await listener.AcceptTcpClientAsync();

                string pcId = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();

                Button MainPcButton = null;

                if (workstationButtons.ContainsKey(pcId))
                {
                    (MainPcButton) = workstationButtons[pcId];

                    if (this.InvokeRequired)
                    {
                        this.Invoke(new Action(() =>
                        {
                            MainPcButton.BackColor = Color.LightGreen;
                            UpdateConnectedCount();
                        }));
                    }
                    else
                    {
                        MainPcButton.BackColor = Color.LightGreen;
                        UpdateConnectedCount();
                    }
                }
                else
                {
                    if (this.InvokeRequired)
                    {
                        this.Invoke(new Action(() =>
                        {
                            var mainBtn = OnWorkStationConnected(pcId);
                            MainPcButton = mainBtn;
                        }));
                    }
                    else
                    {
                        var mainBtn = OnWorkStationConnected(pcId);
                        MainPcButton = mainBtn;
                    }

                    workstationButtons[pcId] = (MainPcButton);

                    if (this.InvokeRequired)
                    {
                        this.Invoke(new Action(() =>
                        {
                            this.lblTotalWorkstations.Text = workstationButtons.Count.ToString();
                            UpdateConnectedCount();
                        }));
                    }
                    else
                    {
                        this.lblTotalWorkstations.Text = workstationButtons.Count.ToString();
                        UpdateConnectedCount();
                    }


                }
                _ = MonitorDisconnected(client, MainPcButton);

            }
        }

        private Button OnWorkStationConnected(string clientIp)
        {
            int currentCount = int.Parse(lblTotalWorkstations.Text);
            currentCount++;
            lblTotalWorkstations.Text = currentCount.ToString();

            WorkStationNum++;

            Button MainPcButton = new Button();
            MainPcButton.Text = "PC " + WorkStationNum.ToString();
            MainPcButton.Image = Properties.Resources.material_symbols_light_computer_outline_rounded;
            MainPcButton.Height = 180;
            MainPcButton.Width = 131;
            MainPcButton.Margin = new Padding(5);
            MainPcButton.BackColor = Color.Green;
            MainPcButton.Tag = clientIp;
            MainPcButton.Click += (s, e) => WorkstationButton_Click(s, e);

            MainWorkstationFLP.Controls.Add(MainPcButton);

            return MainPcButton;

        }

        private async Task MonitorDisconnected(TcpClient client, Button MainPcButton)
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
                        UpdateConnectedCount();

                        int count = int.Parse(lblTotalWorkstations.Text);
                        if (count > 0) count--;
                        this.lblTotalWorkstations.Text = count.ToString();
                    }));
                }
                else
                {
                    MainPcButton.BackColor = Color.Red;
                    UpdateConnectedCount();

                    int count = int.Parse(lblTotalWorkstations.Text);
                    if (count > 0) count--;
                    lblTotalWorkstations.Text = count.ToString();

                }
                client.Close();
            }
        }

        private void UpdateConnectedCount()
        {
            int connectedCount = workstationButtons.Values
            .Count(btn => btn.BackColor == Color.LightGreen);

            int disconnectedCount = workstationButtons.Count - connectedCount;

        }

        //WorkStation Screen Sharing//
        private async void StartScreenListener()
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
            string clientIp = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();

            try
            {
                while (client.Connected)
                {

                    byte[] lengthBuffer = new byte[4];
                    int read = await ReadExactAsync(stream, lengthBuffer, 4);
                    if (read == 0) break;

                    int imageLength = BitConverter.ToInt32(lengthBuffer, 0);
                    Console.WriteLine("Receiving frame: " + imageLength + " bytes from " + clientIp);
                    byte[] imageBuffer = new byte[imageLength];

                    int totalRead = await ReadExactAsync(stream, imageBuffer, imageLength);
                    if (totalRead == 0) break;

                    using (MemoryStream ms = new MemoryStream(imageBuffer))
                    {
                        Image frame = Image.FromStream(ms);

                        if (this.InvokeRequired)
                            this.Invoke(new Action(() => UpdateScreenViewer(clientIp, frame)));
                        else
                            UpdateScreenViewer(clientIp, frame);
                    }
                }
            }
            catch { }
            finally
            {
                client.Close();
            }
        }

        private async Task<int> ReadExactAsync(NetworkStream stream, byte[] buffer, int count)
        {
            int totalRead = 0;
            while (totalRead < count)
            {
                int bytesRead = await stream.ReadAsync(buffer, totalRead, count - totalRead);
                if (bytesRead == 0) return 0;
                totalRead += bytesRead;
            }
            return totalRead;
        }

        private void UpdateScreenViewer(string clientIp, Image frame)
        {
            if (screenViewers.ContainsKey(clientIp) && screenViewers[clientIp] != null)
            {
                PictureBox pb = screenViewers[clientIp];
                Image oldImage = pb.Image;
                pb.Image = frame;
                oldImage?.Dispose();
            }
            else
            {
                frame.Dispose();
            }
        }

        private void AddScreenViewer(string workstationId)
        {
            ScreenViewerForm viewer = new ScreenViewerForm(workstationId);

            screenViewers[workstationId] = viewer.GetPictureBox();

            viewer.FormClosed += (s, args) => screenViewers.Remove(workstationId);

            viewer.Show();
        }



        //Server Folder Management//


        private void lsServerFolderSetup()
        {
            lvServerFolder.View = View.LargeIcon;
            lvServerFolder.LargeImageList = imageListIcon;
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
            imageListIcon.Images.Clear();
            int imageIndex = 0;

            //To Show Folder
            foreach (string dir in Directory.GetDirectories(path))
            {
                imageListIcon.Images.Add(Properties.Resources.Folder);
                ListViewItem item = new ListViewItem(Path.GetFileName(dir), imageIndex);
                item.Tag = dir;
                lvServerFolder.Items.Add(item);
                imageIndex++;
            }

            //to Show File

            foreach (string file in Directory.GetFiles(path))
            {
                Icon fileIcon = Icon.ExtractAssociatedIcon(file);
                imageListIcon.Images.Add(Properties.Resources.Item);

                ListViewItem item = new ListViewItem(Path.GetFileName(file), imageIndex);
                item.Tag = file;
                lvServerFolder.Items.Add(item);
                imageIndex++;

            }

            BtnBack.Enabled = folderHistory.Count > 0;
        }
        private void btnBack()
        {
            if (folderHistory.Count > 0)
            {
                string previousFolder = folderHistory.Pop();
                LoadServerFolder(previousFolder, addToHistory: false);
            }
        }
        private void doubleClick()
        {
            if (lvServerFolder.SelectedItems.Count == 0) return;

            string path = lvServerFolder.SelectedItems[0].Tag.ToString();

            if (Directory.Exists(path))
                LoadServerFolder(path);
            else if (File.Exists(path))
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
        }
    }
}
