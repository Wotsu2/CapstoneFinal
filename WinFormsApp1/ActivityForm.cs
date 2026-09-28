using DevExpress.XtraPdfViewer;
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
    public partial class ActivityForm : Form
    {
        private int profId;
        private string userId;
        private string title;
        private string dueDate;
        private string description;
        private string studentSection;
        private string status;
        private string AcitvitypdfPath;
        private string PathAnswer;
        private string studentname;
        private string activitySubject;
        public event Action ActivitySubmitted;

        // Set this when the form opens — the professor's folder name
        private string professorFolder;

        public ActivityForm(int prof_id, string UserId, string Studentname, string Title, string Due_Date, string Description, string StudentSection, string ActivitySubject, string Status, string PDF_Path)
        {
            InitializeComponent();

            title = Title;
            dueDate = Due_Date;
            description = Description;
            status = Status;
            AcitvitypdfPath = PDF_Path;
            userId = UserId;
            studentSection = StudentSection;
            studentname = Studentname;
            activitySubject = ActivitySubject;
            profId = prof_id;

            InitializeActivityDetails();
        }

        private void InitializeActivityDetails()
        {
            lblActivityTitle.Text = $"{title}";
            lblActivityDescription.Text = description;
            lblActivityDueDate.Text = "Due Date: " + dueDate;
            lblActivityStatus.Text = status;
            MessageBox.Show($"{AcitvitypdfPath}");
            Guna.UI2.WinForms.Guna2Panel pdfContainer = new Guna.UI2.WinForms.Guna2Panel();
            pdfContainer.Location = new Point(20, 280);
            pdfContainer.Size = new Size(760, 500);
            pdfContainer.BorderRadius = 5;
            pdfContainer.BorderColor = Color.Gray;
            pdfContainer.BorderThickness = 1;
            pdfContainer.FillColor = Color.White;
            this.Controls.Add(pdfContainer);

            PdfViewer pdfViewer = new PdfViewer();
            pdfViewer.Dock = DockStyle.Fill;
            pdfContainer.Controls.Add(pdfViewer);

            try
            {
                if (string.IsNullOrWhiteSpace(AcitvitypdfPath))
                {
                    ShowNoFileMessage(pdfContainer, "No attachment for this activity.");
                    return;
                }

                // Normalize the UNC / local path
                string normalized = AcitvitypdfPath.Trim().Replace("\\\\", "\\");
                // fix accidental double backslashes after the server name
                normalized = AcitvitypdfPath.Trim();

                if (!IsShareReachable(normalized))
                {
                    ShowNoFileMessage(pdfContainer,
                        "Shared folder is not reachable.\n\n" +
                        "Make sure the file server is online and the share is accessible.\n\n" +
                        "Path:\n" + normalized);
                    return;
                }

                if (!File.Exists(normalized))
                {
                    ShowNoFileMessage(pdfContainer,
                        "File not found on the share.\n\nPath:\n" + normalized);
                    return;
                }

                // Copy to temp so the PDF viewer doesn't lock the network file
                string tempCopy = Path.Combine(Path.GetTempPath(),
                    "act_" + Guid.NewGuid().ToString("N") + Path.GetExtension(normalized));
                File.Copy(normalized, tempCopy, true);
                pdfViewer.LoadDocument(tempCopy);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading PDF: " + ex.Message +
                                "\n\nPath: " + AcitvitypdfPath);
            }
        }

        private void ShowNoFileMessage(Control parent, string text)
        {
            Label lblNoFile = new Label();
            lblNoFile.Text = text;
            lblNoFile.Location = new Point(20, 20);
            lblNoFile.Size = new Size(parent.Width - 40, parent.Height - 40);
            lblNoFile.ForeColor = Color.Gray;
            lblNoFile.TextAlign = ContentAlignment.MiddleCenter;
            parent.Controls.Add(lblNoFile);
        }

        private bool IsShareReachable(string path)
        {
            try
            {
                // UNC path: \\server\share\...  → test the \\server\share part
                if (path.StartsWith(@"\\"))
                {
                    string[] parts = path.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 2) return false;
                    string shareRoot = $@"\\{parts[0]}\{parts[1]}";
                    return Directory.Exists(shareRoot);
                }

                // Local path: test the drive
                string root = Path.GetPathRoot(path);
                return !string.IsNullOrEmpty(root) && Directory.Exists(root);
            }
            catch { return false; }
        }

        private void btnUploadActivity_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    PathAnswer = ofd.FileName;
                    btnUploadActivity.Text = Path.GetFileName(PathAnswer);
                }
            }
        }

        private async void btnPostActivity_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(PathAnswer) || !File.Exists(PathAnswer))
            {
                MessageBox.Show("Please select a file first.");
                return;
            }

            if (string.IsNullOrEmpty(professorFolder))
                professorFolder = GetProfessorFolder(profId);

            if (string.IsNullOrEmpty(professorFolder))
            {
                MessageBox.Show("Could not determine the professor's folder. Ask the admin to check the share setup.");
                return;
            }

            string fileName = Path.GetFileName(PathAnswer);
            byte[] fileBytes = await File.ReadAllBytesAsync(PathAnswer);

            string savedPath = await SendSubmissionToServer(
                professorFolder, studentSection, studentname, title, fileName, fileBytes);

            if (string.IsNullOrEmpty(savedPath))
            {
                MessageBox.Show("Failed to submit. Please try again.");
                return;
            }

            SaveSubmissionPath(savedPath);

            MessageBox.Show("File Submitted Successfully");

            ActivitySubmitted?.Invoke();       // <-- notify parent

            PathAnswer = "";
            btnUploadActivity.Text = "Upload File";
        }

        private async Task<string> SendSubmissionToServer(
            string professorFolder, string section, string studentName, string activityTitle,
            string fileName, byte[] fileBytes)
        {
            try
            {
                string serverIp = SettingsManager.Current.ServerIp.TrimStart('\\').TrimEnd('\\');
                int serverPort = SettingsManager.Current.FileTransferPort;

                using (TcpClient client = new TcpClient())
                {
                    var connectTask = client.ConnectAsync(serverIp, serverPort);
                    var timeoutTask = Task.Delay(5000);
                    var completed = await Task.WhenAny(connectTask, timeoutTask);

                    if (completed == timeoutTask)
                    {
                        MessageBox.Show($"Server ({serverIp}:{serverPort}) not reachable (timeout).");
                        return null;
                    }

                    await connectTask;
                    if (!client.Connected)
                    {
                        MessageBox.Show($"Server ({serverIp}:{serverPort}) refused the connection.");
                        return null;
                    }

                    string returnedPath = null;

                    using (NetworkStream stream = client.GetStream())
                    using (BinaryWriter writer = new BinaryWriter(stream))
                    {
                        writer.Write("STUDENT_SUBMISSION");
                        writer.Write(professorFolder ?? "");
                        writer.Write(section ?? "");
                        writer.Write(studentName ?? "");
                        writer.Write(activityTitle ?? "");
                        writer.Write(fileName);
                        writer.Write(fileBytes.Length);
                        writer.Write(fileBytes);
                        writer.Flush();

                        // Read the UNC path the admin saved to
                        try
                        {
                            using (var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true))
                            {
                                returnedPath = reader.ReadString();
                            }
                        }
                        catch { }
                    }

                    return returnedPath;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("SendSubmissionToServer error: " + ex.Message);
                MessageBox.Show("Send error: " + ex.Message);
                return null;
            }
        }

        private void SaveSubmissionPath(string savedPath)
        {
            string connStr = SettingsManager.Current.GetConnectionString();
            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    // Try to update an existing row first
                    string update = @"UPDATE submitted_activity 
                                      SET file_path = @path, activity_status = 'Submitted'
                                      WHERE prof_id = @prof_id 
                                        AND user_id = @user_id 
                                        AND title   = @title";

                    int rows;
                    using (var cmd = new MySqlCommand(update, conn))
                    {
                        cmd.Parameters.AddWithValue("@path", savedPath);
                        cmd.Parameters.AddWithValue("@prof_id", profId);
                        cmd.Parameters.AddWithValue("@user_id", userId);
                        cmd.Parameters.AddWithValue("@title", title);
                        rows = cmd.ExecuteNonQuery();
                    }

                    // If nothing existed, insert
                    if (rows == 0)
                    {
                        string insert = @"INSERT INTO submitted_activity 
                            (prof_id, user_id, title, section, student_name, class_name, activity_status, file_path) 
                            VALUES (@prof_id, @user_id, @title, @section, @student_name, @class_name, @status, @path)";

                        using (var cmd = new MySqlCommand(insert, conn))
                        {
                            cmd.Parameters.AddWithValue("@prof_id", profId);
                            cmd.Parameters.AddWithValue("@user_id", userId);
                            cmd.Parameters.AddWithValue("@title", title);
                            cmd.Parameters.AddWithValue("@section", studentSection);
                            cmd.Parameters.AddWithValue("@student_name", studentname);
                            cmd.Parameters.AddWithValue("@class_name", activitySubject);
                            cmd.Parameters.AddWithValue("@status", "Submitted");
                            cmd.Parameters.AddWithValue("@path", savedPath);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving submission: " + ex.Message);
            }
        }

        private string GetProfessorFolder(int prof_id)
        {
            string connStr = SettingsManager.Current.GetConnectionString();
            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    // Try to build from user_information: Lastname_Firstname_Middlename
                    string q = @"SELECT i.lastname, i.firstname, i.middlename
                                 FROM user_information i
                                 WHERE i.user_id = @id";
                    using (var cmd = new MySqlCommand(q, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", prof_id);
                        using (var r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                string ln = r["lastname"]?.ToString() ?? "";
                                string fn = r["firstname"]?.ToString() ?? "";
                                string mn = r["middlename"]?.ToString() ?? "";
                                return SanitizeFolderName($"{ln}_{fn}_{mn}");
                            }
                        }
                    }

                    // Fallback: look up mainfolderpath
                    using (var cmd = new MySqlCommand(
                        "SELECT FolderPath FROM mainfolderpath WHERE user_id = @id", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", prof_id);
                        object result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                            return new DirectoryInfo(result.ToString()).Name;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("GetProfessorFolder error: " + ex.Message);
            }
            return null;
        }

        private string SanitizeFolderName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "Unknown";
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name.Trim().TrimEnd('.');
        }
    }
}