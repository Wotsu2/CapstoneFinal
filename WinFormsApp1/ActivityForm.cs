using DevExpress.XtraPdfViewer;
using Guna.UI2.WinForms;
using MySql.Data.MySqlClient;
using System;
using System.Diagnostics;
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
        // =========================================================
        // FIELDS
        // =========================================================
        private readonly int profId;
        private readonly string userId;
        private readonly string title;
        private readonly string dueDate;
        private readonly string description;
        private readonly string studentSection;
        private readonly string status;
        private readonly string AcitvitypdfPath;
        private readonly string studentname;
        private readonly string activitySubject;

        private string PathAnswer;
        private string professorFolder;

        public event Action ActivitySubmitted;

        // UI Controls
        private Guna2Panel mainCard;
        private Guna2Panel headerPanel;
        private Guna2Panel descPanel;
        private Guna2Panel pdfCard;
        private Guna2Panel footerPanel;
        private Guna2Panel uploadZone;
        private Guna2Panel badgePanel;

        private Label lblTitle;
        private Label lblSubject;
        private Label lblDueDate;
        private Label lblStatusBadge;
        private Label lblDescLabel;
        private Label lblDescription;
        private Label lblUploadHint;
        private Label lblFileName;

        private PdfViewer pdfViewer;
        private Guna2Button btnSubmit;

        // =========================================================
        // CONSTRUCTOR
        // =========================================================
        public ActivityForm(int prof_id, string UserId, string Studentname, string Title,
                            string Due_Date, string Description, string StudentSection,
                            string ActivitySubject, string Status, string PDF_Path)
        {
            profId = prof_id;
            userId = UserId;
            studentname = Studentname;
            title = Title;
            dueDate = FormatDueDate(Due_Date);
            description = Description;
            studentSection = StudentSection;
            activitySubject = ActivitySubject;
            status = Status;
            AcitvitypdfPath = PDF_Path;

            BuildUi();
            LoadActivityPdf();
        }

        // =========================================================
        // UI CONSTRUCTION
        // =========================================================
        private void BuildUi()
        {
            // ---------------- FORM ----------------
            this.Text = "Activity - " + title;
            this.Size = new Size(1320, 720);
            this.MinimumSize = new Size(1100, 640);
            this.StartPosition = FormStartPosition.CenterParent;
            this.WindowState = FormWindowState.Maximized;      // fill the screen for best fit
            this.BackColor = Color.FromArgb(245, 245, 248);
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = true;
            this.MinimizeBox = true;
            this.ShowIcon = false;

            // ---------------- MAIN CARD ----------------
            mainCard = new Guna2Panel
            {
                Size = new Size(1280, 680),
                Location = new Point(15, 15),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BorderRadius = 16,
                FillColor = Color.White,
                BorderColor = Color.FromArgb(225, 225, 225),
                BorderThickness = 1,
                ShadowDecoration = { Enabled = true, Depth = 15, Color = Color.FromArgb(40, 0, 0, 0) }
            };
            this.Controls.Add(mainCard);

            // ---------------- HEADER ----------------
            headerPanel = new Guna2Panel
            {
                Dock = DockStyle.Top,
                Height = 95,
                FillColor = Color.Maroon,
                BorderRadius = 0
            };
            mainCard.Controls.Add(headerPanel);

            // Title
            lblTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI Semibold", 15F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                AutoSize = false,
                AutoEllipsis = true,
                Size = new Size(560, 32),
                Location = new Point(24, 16)
            };
            headerPanel.Controls.Add(lblTitle);

            // Subject • Section
            lblSubject = new Label
            {
                Text = activitySubject + "  •  Section " + studentSection,
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = Color.FromArgb(255, 220, 220),
                BackColor = Color.Transparent,
                AutoSize = false,
                AutoEllipsis = true,
                Size = new Size(560, 22),
                Location = new Point(26, 56)
            };
            headerPanel.Controls.Add(lblSubject);

            // ---------------- DUE DATE ----------------
            lblDueDate = new Label
            {
                Text = "Due: " + dueDate,
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                AutoSize = false,
                Size = new Size(240, 30),
                Location = new Point(headerPanel.Width - 400, 34),
                TextAlign = ContentAlignment.MiddleRight,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            headerPanel.Controls.Add(lblDueDate);

            // ---------------- STATUS BADGE ----------------
            Color badgeColor = GetStatusColor(status);

            badgePanel = new Guna2Panel
            {
                Size = new Size(120, 30),
                Location = new Point(headerPanel.Width - 140, 34),
                FillColor = badgeColor,
                BorderRadius = 15,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            headerPanel.Controls.Add(badgePanel);

            lblStatusBadge = new Label
            {
                Text = status,
                Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            badgePanel.Controls.Add(lblStatusBadge);

            // ---------------- DESCRIPTION ----------------
            descPanel = new Guna2Panel
            {
                Size = new Size(mainCard.Width - 40, 75),
                Location = new Point(20, 110),
                BorderRadius = 12,
                FillColor = Color.FromArgb(252, 248, 248),
                BorderColor = Color.FromArgb(240, 230, 230),
                BorderThickness = 1,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            mainCard.Controls.Add(descPanel);

            lblDescLabel = new Label
            {
                Text = "📄   Activity Description",
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                ForeColor = Color.Maroon,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(16, 10)
            };
            descPanel.Controls.Add(lblDescLabel);

            lblDescription = new Label
            {
                Text = string.IsNullOrWhiteSpace(description) ? "(No description provided)" : description,
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = Color.FromArgb(70, 70, 70),
                BackColor = Color.Transparent,
                AutoSize = false,
                Size = new Size(descPanel.Width - 32, 38),
                Location = new Point(18, 32),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            descPanel.Controls.Add(lblDescription);

            // ---------------- PDF CARD ----------------
            pdfCard = new Guna2Panel
            {
                Size = new Size(mainCard.Width - 40, 330),
                Location = new Point(20, 200),
                BorderRadius = 12,
                FillColor = Color.FromArgb(245, 245, 248),
                BorderColor = Color.FromArgb(225, 225, 225),
                BorderThickness = 1,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            mainCard.Controls.Add(pdfCard);

            pdfViewer = new PdfViewer
            {
                Dock = DockStyle.Fill,
                Location = new Point(1, 1)
            };
            pdfCard.Controls.Add(pdfViewer);

            // ---------------- FOOTER / UPLOAD ----------------
            footerPanel = new Guna2Panel
            {
                Size = new Size(mainCard.Width - 40, 120),
                Location = new Point(20, mainCard.Height - 140),
                BorderRadius = 12,
                FillColor = Color.FromArgb(252, 248, 248),
                BorderColor = Color.FromArgb(240, 230, 230),
                BorderThickness = 1,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            mainCard.Controls.Add(footerPanel);

            uploadZone = new Guna2Panel
            {
                Size = new Size(footerPanel.Width - 180, 84),
                Location = new Point(18, 18),
                BorderRadius = 12,
                FillColor = Color.FromArgb(245, 240, 240),
                BorderColor = Color.FromArgb(200, 180, 180),
                BorderThickness = 2,
                Cursor = Cursors.Hand,
                AllowDrop = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            footerPanel.Controls.Add(uploadZone);

            lblUploadHint = new Label
            {
                Text = "☁\nClick to upload your file",
                Font = new Font("Segoe UI", 9F, FontStyle.Italic),
                ForeColor = Color.FromArgb(120, 100, 100),
                BackColor = Color.Transparent,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            uploadZone.Controls.Add(lblUploadHint);

            lblFileName = new Label
            {
                Text = "",
                Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                ForeColor = Color.Maroon,
                BackColor = Color.Transparent,
                AutoSize = false,
                Size = new Size(uploadZone.Width, 18),
                Location = new Point(0, uploadZone.Height - 20),
                TextAlign = ContentAlignment.MiddleCenter,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            uploadZone.Controls.Add(lblFileName);
            lblFileName.BringToFront();

            // Upload click
            EventHandler uploadClick = (s, e) => PickFile();
            uploadZone.Click += uploadClick;
            lblUploadHint.Click += uploadClick;

            // Drag & drop
            uploadZone.DragEnter += (s, e) =>
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                    e.Effect = DragDropEffects.Copy;
            };
            uploadZone.DragDrop += (s, e) =>
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                    SetSelectedFile(files[0]);
            };

            // Submit button
            btnSubmit = new Guna2Button
            {
                Text = "Submit",
                Size = new Size(130, 46),
                Location = new Point(footerPanel.Width - 150, 36),
                Anchor = AnchorStyles.Right,
                BorderRadius = 10,
                FillColor = Color.Maroon,
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold)
            };
            btnSubmit.HoverState.FillColor = Color.FromArgb(100, 0, 0);
            btnSubmit.Click += btnPostActivity_Click;
            footerPanel.Controls.Add(btnSubmit);

            // Layout callback for window resize
            this.Resize += (s, e) =>
            {
                if (mainCard == null || headerPanel == null) return;

                // Header full-width
                headerPanel.Width = mainCard.Width;

                // Due date + badge re-anchor
                if (lblDueDate != null)
                    lblDueDate.Location = new Point(headerPanel.Width - 400, 34);

                if (badgePanel != null)
                    badgePanel.Location = new Point(headerPanel.Width - 140, 34);

                // Description panel
                descPanel.Width = mainCard.Width - 40;

                // PDF card fills the middle
                int headerH = 95;
                int descTop = headerH + 15;
                int descH = 75;
                int gap = 15;
                int footerH = 120;
                int footerBottomGap = 20;

                int pdfTop = descTop + descH + gap;
                int pdfHeight = mainCard.Height - pdfTop - footerH - footerBottomGap - 15;

                pdfCard.Location = new Point(20, pdfTop);
                pdfCard.Size = new Size(mainCard.Width - 40, Math.Max(200, pdfHeight));

                footerPanel.Location = new Point(20, mainCard.Height - footerH - footerBottomGap);
                footerPanel.Width = mainCard.Width - 40;

                btnSubmit.Location = new Point(footerPanel.Width - 150, (footerPanel.Height - btnSubmit.Height) / 2);
            };

            // Initial layout
            this.Resize?.Invoke(this, EventArgs.Empty);
        }

        // =========================================================
        // LOAD PDF / ATTACHMENT
        // =========================================================
        private void LoadActivityPdf()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(AcitvitypdfPath))
                {
                    ShowNoFileMessage(pdfCard, "No attachment for this activity.");
                    return;
                }

                string normalized = AcitvitypdfPath.Trim();

                if (!IsShareReachable(normalized))
                {
                    ShowNoFileMessage(pdfCard,
                        "Shared folder is not reachable.\n\n" +
                        "Make sure the file server is online and the share is accessible.\n\n" +
                        "Path:\n" + normalized);
                    return;
                }

                if (!File.Exists(normalized))
                {
                    ShowNoFileMessage(pdfCard, "File not found on the share.\n\nPath:\n" + normalized);
                    return;
                }

                string ext = Path.GetExtension(normalized).ToLowerInvariant();

                if (ext == ".pdf")
                {
                    try
                    {
                        string tempCopy = Path.Combine(Path.GetTempPath(),
                            "act_" + Guid.NewGuid().ToString("N") + ".pdf");
                        File.Copy(normalized, tempCopy, true);
                        pdfViewer.LoadDocument(tempCopy);
                    }
                    catch (Exception exPdf)
                    {
                        ShowExternalFileMessage(normalized,
                            "This file could not be previewed.\n\n" + exPdf.Message);
                    }
                }
                else
                {
                    ShowExternalFileMessage(normalized,
                        $"This attachment is a {ext.TrimStart('.').ToUpper()} file.\n\n" +
                        "Click the button below to open it with your default application.");
                }
            }
            catch (Exception ex)
            {
                ShowNoFileMessage(pdfCard, "Error loading attachment: " + ex.Message);
            }
        }

        private void ShowNoFileMessage(Control parent, string text)
        {
            Label lblNoFile = new Label
            {
                Text = "📄   " + text,
                Font = new Font("Segoe UI", 11F, FontStyle.Italic),
                ForeColor = Color.Gray,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };
            parent.Controls.Add(lblNoFile);
            lblNoFile.BringToFront();
        }

        private void ShowExternalFileMessage(string filePath, string message)
        {
            Panel overlay = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };
            pdfCard.Controls.Add(overlay);
            overlay.BringToFront();

            Label lbl = new Label
            {
                Text = "📎   " + message,
                Font = new Font("Segoe UI", 10.5F),
                ForeColor = Color.FromArgb(90, 90, 90),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter,
                AutoSize = false,
                Size = new Size(600, 120),
                Location = new Point((pdfCard.Width - 600) / 2, 120)
            };
            overlay.Controls.Add(lbl);

            Guna2Button btnOpen = new Guna2Button
            {
                Text = "Open File",
                Size = new Size(160, 44),
                Location = new Point((pdfCard.Width - 160) / 2, 240),
                BorderRadius = 10,
                FillColor = Color.Maroon,
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold)
            };
            btnOpen.HoverState.FillColor = Color.FromArgb(100, 0, 0);
            btnOpen.Click += (s, e) =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Could not open file: " + ex.Message);
                }
            };
            overlay.Controls.Add(btnOpen);
        }

        private bool IsShareReachable(string path)
        {
            try
            {
                if (path.StartsWith(@"\\"))
                {
                    string[] parts = path.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 2) return false;
                    string shareRoot = $@"\\{parts[0]}\{parts[1]}";
                    return Directory.Exists(shareRoot);
                }
                string root = Path.GetPathRoot(path);
                return !string.IsNullOrEmpty(root) && Directory.Exists(root);
            }
            catch { return false; }
        }

        // =========================================================
        // UPLOAD / SUBMIT
        // =========================================================
        private void PickFile()
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = "Select your file to submit";
                ofd.Filter = "All Files|*.*";
                if (ofd.ShowDialog() == DialogResult.OK)
                    SetSelectedFile(ofd.FileName);
            }
        }

        private void SetSelectedFile(string path)
        {
            PathAnswer = path;
            lblFileName.Text = "✔  " + Path.GetFileName(path);
            lblUploadHint.Text = "☁\nClick to change file";
        }

        private async void btnPostActivity_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(PathAnswer) || !File.Exists(PathAnswer))
            {
                CustomMessageBox.Show("Please select a file first.", "Validation",
                    CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrEmpty(professorFolder))
                professorFolder = GetProfessorFolder(profId);

            if (string.IsNullOrEmpty(professorFolder))
            {
                CustomMessageBox.Show("Could not determine the professor's folder. Ask the admin to check the share setup.",
                    "Missing Folder", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Warning);
                return;
            }

            string fileName = Path.GetFileName(PathAnswer);
            byte[] fileBytes = await File.ReadAllBytesAsync(PathAnswer);

            btnSubmit.Enabled = false;
            btnSubmit.Text = "Submitting...";

            string savedPath = await SendSubmissionToServer(
                professorFolder, studentSection, studentname, title, fileName, fileBytes);

            btnSubmit.Enabled = true;
            btnSubmit.Text = "Submit";

            if (string.IsNullOrEmpty(savedPath))
            {
                CustomMessageBox.Show("Failed to submit. Please try again.",
                    "Submit Failed", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Warning);
                return;
            }

            SaveSubmissionPath(savedPath);

            CustomMessageBox.Show("File submitted successfully!",
                "Success", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Information);

            ActivitySubmitted?.Invoke();

            PathAnswer = "";
            lblFileName.Text = "";
            lblUploadHint.Text = "☁\nClick to upload your file";

            this.Close();
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
                        Console.WriteLine($"Server ({serverIp}:{serverPort}) not reachable (timeout).");
                        return null;
                    }

                    await connectTask;
                    if (!client.Connected)
                    {
                        Console.WriteLine($"Server ({serverIp}:{serverPort}) refused the connection.");
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
                Console.WriteLine("Error saving submission: " + ex.Message);
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

        // =========================================================
        // HELPERS
        // =========================================================
        private Color GetStatusColor(string status)
        {
            if (string.IsNullOrEmpty(status)) return Color.Gray;
            switch (status.Trim().ToLower())
            {
                case "pending": return Color.FromArgb(200, 160, 40);
                case "submitted": return Color.FromArgb(46, 160, 90);
                case "incomplete": return Color.FromArgb(200, 60, 60);
                case "graded": return Color.FromArgb(52, 120, 200);
                default: return Color.Gray;
            }
        }

        /// <summary>
        /// Converts any DateTime-like string into a clean readable format,
        /// e.g. "2026-08-30 12:57:54.9540000" → "Aug 30, 2026 12:57 PM".
        /// </summary>
        private string FormatDueDate(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return raw;

            if (DateTime.TryParse(raw, out DateTime parsed))
                return parsed.ToString("MMM dd, yyyy hh:mm tt");

            try
            {
                int dot = raw.IndexOf('.');
                if (dot > 0)
                {
                    int spaceAfter = raw.IndexOf(' ', dot);
                    string cleaned = spaceAfter > 0
                        ? raw.Substring(0, dot) + raw.Substring(spaceAfter)
                        : raw.Substring(0, dot);
                    if (DateTime.TryParse(cleaned, out DateTime p2))
                        return p2.ToString("MMM dd, yyyy hh:mm tt");
                }
            }
            catch { }

            return raw;
        }
    }
}