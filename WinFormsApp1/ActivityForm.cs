using DevExpress.XtraPdfViewer;
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
                if (!string.IsNullOrEmpty(AcitvitypdfPath) && File.Exists(AcitvitypdfPath))
                {
                    pdfViewer.LoadDocument(AcitvitypdfPath);
                }
                else
                {
                    Label lblNoFile = new Label();
                    lblNoFile.Text = "No attachment for this activity.";
                    lblNoFile.Location = new Point(200, 130);
                    lblNoFile.Size = new Size(400, 25);
                    lblNoFile.ForeColor = Color.Gray;
                    pdfContainer.Controls.Add(lblNoFile);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading PDF: " + ex.Message);
            }
        }

        private void btnUploadActivity_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    PathAnswer = ofd.FileName;
                }
            }
        }

        private async void btnPostActivity_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(PathAnswer))
            {
                MessageBox.Show("Please select a file");
                return;
            }

            try
            {
                using (TcpClient client = new TcpClient())
                {
                    await client.ConnectAsync(SettingsManager.Current.ServerIp, SettingsManager.Current.FileTransferPort); // Same to other one it Should be Empty and configure it to setting
                    using (NetworkStream stream = client.GetStream())
                    using (BinaryWriter writer = new BinaryWriter(stream))
                    {
                        string fileName = Path.GetFileName(PathAnswer);
                        byte[] fileBytes = File.ReadAllBytes(PathAnswer);

                        writer.Write(studentSection);
                        writer.Write(profId);
                        writer.Write(userId);
                        writer.Write(fileName);
                        writer.Write(fileBytes.Length);
                        writer.Write(fileBytes);
                    }
                }
                UpdateSubmittedFile();
                MessageBox.Show("File Submitted Successfuly");
                PathAnswer = "";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error" + ex.Message);
            }
        }

        private void UpdateSubmittedFile()
        {
            string connStr = SettingsManager.Current.GetConnectionString();
            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = @"INSERT INTO submitted_activity (prof_id, user_id, title, section, student_name, class_name, activity_status) 
                                VALUES (@prof_id, @user_id, @title, @seciton, @student_name, @classname, @activity_status)";


                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@prof_id", profId);
                        cmd.Parameters.AddWithValue("@user_id", userId);
                        cmd.Parameters.AddWithValue("@title", title);
                        cmd.Parameters.AddWithValue("@seciton", studentSection);
                        cmd.Parameters.AddWithValue("@student_name", studentname);
                        cmd.Parameters.AddWithValue("@classname", activitySubject);
                        cmd.Parameters.AddWithValue("@activity_status", "Submitted");

                        cmd.ExecuteNonQuery();
                    }

                    MessageBox.Show("Activity submitted successfully!");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading activities: " + ex.Message);
            }

        }
    }
}
