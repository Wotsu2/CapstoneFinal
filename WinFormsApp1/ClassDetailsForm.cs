using Guna.UI2.WinForms;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace WinFormsApp1
{
    public class ClassDetailsForm : Form
    {
        private readonly int ProfessorID;
        private readonly string ClassId;
        private readonly string ClassName;
        private readonly string Section;
        private readonly string ClassDay;
        private readonly string ClassTime;

        private Guna2Panel headerPanel;
        private Guna2Panel bodyPanel;
        private FlowLayoutPanel studentsList;
        private FlowLayoutPanel activitiesList;
        private Label lblStudentCount;
        private Label lblActivityCount;
        private Guna2Button btnClose;

        public ClassDetailsForm(int professorId, string classId, string className,
                                string section, string day, string time)
        {
            ProfessorID = professorId;
            ClassId = classId ?? "";
            ClassName = className ?? "";
            Section = section ?? "";
            ClassDay = day ?? "";
            ClassTime = time ?? "";

            BuildUi();

            Load += (s, e) =>
            {
                LoadStudents();
                LoadActivities();
            };
        }

        private void BuildUi()
        {
            Text = "Class - " + ClassName;
            Size = new Size(1100, 750);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(245, 245, 248);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowIcon = false;
            Font = new Font("Segoe UI", 9.5F);

            headerPanel = new Guna2Panel
            {
                Dock = DockStyle.Top,
                Height = 110,
                FillColor = Color.Maroon,
                BorderRadius = 0
            };
            Controls.Add(headerPanel);

            Label lblTitle = new Label
            {
                Text = ClassName,
                Font = new Font("Segoe UI Semibold", 22F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                AutoSize = false,
                AutoEllipsis = true,
                Size = new Size(700, 40),
                Location = new Point(30, 18)
            };
            headerPanel.Controls.Add(lblTitle);

            Label lblSub = new Label
            {
                Text = $"Section {Section}   •   {ClassDay}   •   {ClassTime}",
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(255, 220, 220),
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(32, 66)
            };
            headerPanel.Controls.Add(lblSub);

            bodyPanel = new Guna2Panel
            {
                Location = new Point(15, 125),
                Size = new Size(ClientSize.Width - 30, ClientSize.Height - 140),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                FillColor = Color.White,
                BorderColor = Color.FromArgb(230, 225, 225),
                BorderThickness = 1,
                BorderRadius = 14
            };
            Controls.Add(bodyPanel);

            // Students header
            Guna2Panel studentsHeader = new Guna2Panel
            {
                Location = new Point(20, 20),
                Size = new Size(430, 55),
                FillColor = Color.FromArgb(250, 240, 240),
                BorderRadius = 10
            };
            bodyPanel.Controls.Add(studentsHeader);

            Label lblStudentsTitle = new Label
            {
                Text = "👨‍🎓  Enrolled Students",
                Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold),
                ForeColor = Color.Maroon,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(16, 8)
            };
            studentsHeader.Controls.Add(lblStudentsTitle);

            lblStudentCount = new Label
            {
                Text = "0 students",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(120, 120, 120),
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(18, 30)
            };
            studentsHeader.Controls.Add(lblStudentCount);

            studentsList = new FlowLayoutPanel
            {
                Location = new Point(20, 85),
                Size = new Size(430, bodyPanel.Height - 105),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                BackColor = Color.White,
                Padding = new Padding(0)
            };
            bodyPanel.Controls.Add(studentsList);

            // Activities header
            Guna2Panel activitiesHeader = new Guna2Panel
            {
                Location = new Point(470, 20),
                Size = new Size(bodyPanel.Width - 490, 55),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                FillColor = Color.FromArgb(240, 245, 255),
                BorderRadius = 10
            };
            bodyPanel.Controls.Add(activitiesHeader);

            Label lblActivitiesTitle = new Label
            {
                Text = "📋  Activities / Performance Tasks / Quizzes / Exams",
                Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 70, 140),
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(16, 8)
            };
            activitiesHeader.Controls.Add(lblActivitiesTitle);

            lblActivityCount = new Label
            {
                Text = "0 items posted",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(120, 120, 120),
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(18, 30)
            };
            activitiesHeader.Controls.Add(lblActivityCount);

            activitiesList = new FlowLayoutPanel
            {
                Location = new Point(470, 85),
                Size = new Size(bodyPanel.Width - 490, bodyPanel.Height - 105),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                BackColor = Color.White,
                Padding = new Padding(0)
            };
            bodyPanel.Controls.Add(activitiesList);

            bodyPanel.SizeChanged += (s, e) =>
            {
                studentsList.Height = Math.Max(100, bodyPanel.Height - 105);
                activitiesList.Size = new Size(
                    Math.Max(200, bodyPanel.Width - 490),
                    Math.Max(100, bodyPanel.Height - 105));
            };
        }

        private void LoadStudents()
        {
            studentsList.Controls.Clear();
            int count = 0;
            string connStr = SettingsManager.Current.GetConnectionString();

            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = @"
                        SELECT DISTINCT
                            sc.user_id,
                            ui.lastname,
                            ui.firstname,
                            ui.middlename,
                            ui.school_year,
                            ui.school_course
                        FROM student_class sc
                        LEFT JOIN user_information ui ON ui.user_id = sc.user_id
                        WHERE sc.professor_id = @prof_id
                          AND sc.class_name = @class_name
                          AND sc.section = @section
                        ORDER BY ui.lastname, ui.firstname";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@prof_id", ProfessorID);
                        cmd.Parameters.AddWithValue("@class_name", ClassName);
                        cmd.Parameters.AddWithValue("@section", Section);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                count++;

                                int studentId = reader["user_id"] == DBNull.Value ? 0 : Convert.ToInt32(reader["user_id"]);
                                string lastname = reader["lastname"]?.ToString()?.Trim() ?? "";
                                string firstname = reader["firstname"]?.ToString()?.Trim() ?? "";
                                string middlename = reader["middlename"]?.ToString()?.Trim() ?? "";
                                string year = reader["school_year"]?.ToString()?.Trim() ?? "";
                                string course = reader["school_course"]?.ToString()?.Trim() ?? "";

                                string fullName = string.Join(" ",
                                    new[] { lastname, firstname, middlename }
                                        .Where(x => !string.IsNullOrWhiteSpace(x))).Trim();

                                if (string.IsNullOrEmpty(fullName))
                                    fullName = "Unknown Student";

                                AddStudentCard(studentId, fullName, year, course);
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine("LoadStudents error: " + ex.Message); }

            lblStudentCount.Text = count + (count == 1 ? " student" : " students");

            if (count == 0)
            {
                Label lblEmpty = new Label
                {
                    Text = "No students have joined this class yet.",
                    Font = new Font("Segoe UI", 10F, FontStyle.Italic),
                    ForeColor = Color.Gray,
                    AutoSize = false,
                    Size = new Size(studentsList.Width - 20, 60),
                    TextAlign = ContentAlignment.MiddleCenter
                };
                studentsList.Controls.Add(lblEmpty);
            }
        }

        private void AddStudentCard(int studentId, string fullName, string year, string course)
        {
            int cardWidth = Math.Max(300, studentsList.ClientSize.Width - 25);

            Guna2Panel card = new Guna2Panel
            {
                Size = new Size(cardWidth, 68),
                FillColor = Color.FromArgb(252, 252, 254),
                BorderColor = Color.FromArgb(235, 235, 240),
                BorderThickness = 1,
                BorderRadius = 10,
                Margin = new Padding(0, 0, 0, 8)
            };

            Guna2CircleButton avatar = new Guna2CircleButton
            {
                Size = new Size(44, 44),
                Location = new Point(12, 12),
                FillColor = Color.FromArgb(250, 235, 235),
                ForeColor = Color.Maroon,
                Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
                Text = GetInitials(fullName),
                Enabled = false
            };
            card.Controls.Add(avatar);

            Label lblName = new Label
            {
                Text = fullName,
                Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 40, 40),
                BackColor = Color.Transparent,
                AutoSize = false,
                AutoEllipsis = true,
                Size = new Size(cardWidth - 80, 22),
                Location = new Point(68, 12)
            };
            card.Controls.Add(lblName);

            string meta = string.Join("   •   ",
                new[] { year, course }.Where(x => !string.IsNullOrWhiteSpace(x)));
            if (string.IsNullOrEmpty(meta)) meta = "No year / course info";

            Label lblMeta = new Label
            {
                Text = meta,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Italic),
                ForeColor = Color.FromArgb(130, 130, 130),
                BackColor = Color.Transparent,
                AutoSize = false,
                AutoEllipsis = true,
                Size = new Size(cardWidth - 80, 18),
                Location = new Point(68, 36)
            };
            card.Controls.Add(lblMeta);

            studentsList.Controls.Add(card);
        }

        private string GetInitials(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "?";
            var parts = name.Split(new[] { ' ', ',', '_', '.' }, StringSplitOptions.RemoveEmptyEntries);
            string initials = "";
            foreach (var p in parts)
            {
                if (initials.Length >= 2) break;
                initials += char.ToUpper(p[0]);
            }
            return string.IsNullOrEmpty(initials) ? "?" : initials;
        }

        private void LoadActivities()
        {
            activitiesList.Controls.Clear();
            int count = 0;
            string connStr = SettingsManager.Current.GetConnectionString();

            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = @"
                        SELECT title, description, activity_subject,
                               start_time, due_date, activity_status,
                               score, activity_filename
                        FROM professor_activity
                        WHERE professor_id = @prof_id
                          AND section = @section
                          AND activity_subject = @class_name
                        ORDER BY start_time DESC";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@prof_id", ProfessorID);
                        cmd.Parameters.AddWithValue("@section", Section);
                        cmd.Parameters.AddWithValue("@class_name", ClassName);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                count++;
                                AddActivityCard(
                                    type: "Activity",
                                    title: reader["title"]?.ToString()?.Trim() ?? "",
                                    description: reader["description"]?.ToString()?.Trim() ?? "",
                                    subject: reader["activity_subject"]?.ToString()?.Trim() ?? "",
                                    startTime: reader["start_time"]?.ToString()?.Trim() ?? "",
                                    dueDate: reader["due_date"]?.ToString()?.Trim() ?? "",
                                    status: reader["activity_status"]?.ToString()?.Trim() ?? "",
                                    score: reader["score"]?.ToString()?.Trim() ?? "",
                                    fileName: reader["activity_filename"]?.ToString()?.Trim() ?? "");
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine("LoadActivities (activities) error: " + ex.Message); }

            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = @"
                        SELECT quiz_id, quiz_title, subject, assessment_type,
                               exam_period, duration_minutes, created_at
                        FROM quizzes
                        WHERE created_by = @prof_id
                          AND subject = @class_name
                        ORDER BY created_at DESC";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@prof_id", ProfessorID);
                        cmd.Parameters.AddWithValue("@class_name", ClassName);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                count++;
                                int duration = reader["duration_minutes"] == DBNull.Value ? 0 : Convert.ToInt32(reader["duration_minutes"]);
                                string type = reader["assessment_type"]?.ToString()?.Trim() ?? "quiz";

                                AddActivityCard(
                                    type: type.Equals("exam", StringComparison.OrdinalIgnoreCase) ? "Exam" : "Quiz",
                                    title: reader["quiz_title"]?.ToString()?.Trim() ?? "",
                                    description: "Duration: " + duration + " min   •   " + (reader["exam_period"]?.ToString()?.Trim() ?? ""),
                                    subject: reader["subject"]?.ToString()?.Trim() ?? "",
                                    startTime: reader["created_at"]?.ToString()?.Trim() ?? "",
                                    dueDate: "",
                                    status: "",
                                    score: "",
                                    fileName: "");
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine("LoadActivities (quizzes) error: " + ex.Message); }

            lblActivityCount.Text = count + (count == 1 ? " item posted" : " items posted");

            if (count == 0)
            {
                Label lblEmpty = new Label
                {
                    Text = "No activities, quizzes, or exams have been posted for this class yet.",
                    Font = new Font("Segoe UI", 10F, FontStyle.Italic),
                    ForeColor = Color.Gray,
                    AutoSize = false,
                    Size = new Size(activitiesList.Width - 20, 60),
                    TextAlign = ContentAlignment.MiddleCenter
                };
                activitiesList.Controls.Add(lblEmpty);
            }
        }

        private void AddActivityCard(string type, string title, string description,
                                     string subject, string startTime, string dueDate,
                                     string status, string score, string fileName)
        {
            int cardWidth = Math.Max(400, activitiesList.ClientSize.Width - 25);

            Guna2Panel card = new Guna2Panel
            {
                Size = new Size(cardWidth, 100),
                FillColor = Color.White,
                BorderColor = Color.FromArgb(235, 235, 240),
                BorderThickness = 1,
                BorderRadius = 12,
                Margin = new Padding(0, 0, 0, 10)
            };

            Color accent;
            string icon;
            switch (type.ToLower())
            {
                case "exam": accent = Color.FromArgb(200, 40, 40); icon = "📝"; break;
                case "quiz": accent = Color.FromArgb(220, 150, 40); icon = "📋"; break;
                default: accent = Color.FromArgb(60, 130, 200); icon = "📄"; break;
            }

            card.Controls.Add(new Panel { Width = 5, Dock = DockStyle.Left, BackColor = accent });

            card.Controls.Add(new Label
            {
                Text = icon,
                Font = new Font("Segoe UI Emoji", 16F),
                ForeColor = accent,
                BackColor = Color.Transparent,
                AutoSize = false,
                Size = new Size(40, 40),
                Location = new Point(16, 12),
                TextAlign = ContentAlignment.MiddleCenter
            });

            card.Controls.Add(new Label
            {
                Text = title,
                Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 30, 30),
                BackColor = Color.Transparent,
                AutoSize = false,
                AutoEllipsis = true,
                Size = new Size(cardWidth - 130, 22),
                Location = new Point(60, 12)
            });

            card.Controls.Add(new Label
            {
                Text = type.ToUpper() + (string.IsNullOrEmpty(subject) ? "" : "  •  " + subject),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = accent,
                BackColor = Color.Transparent,
                AutoSize = false,
                AutoEllipsis = true,
                Size = new Size(cardWidth - 130, 18),
                Location = new Point(60, 34)
            });

            card.Controls.Add(new Label
            {
                Text = string.IsNullOrEmpty(description) ? "(No description)" : description,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(90, 90, 90),
                BackColor = Color.Transparent,
                AutoSize = false,
                AutoEllipsis = true,
                Size = new Size(cardWidth - 40, 18),
                Location = new Point(20, 58)
            });

            string metaLine = "";
            if (!string.IsNullOrEmpty(dueDate)) metaLine = "Due: " + dueDate;
            else if (!string.IsNullOrEmpty(startTime)) metaLine = "Posted: " + startTime;

            if (!string.IsNullOrEmpty(score)) metaLine += (metaLine.Length > 0 ? "   •   " : "") + "Score: " + score;
            if (!string.IsNullOrEmpty(status)) metaLine += (metaLine.Length > 0 ? "   •   " : "") + "Status: " + status;
            if (!string.IsNullOrEmpty(fileName)) metaLine += (metaLine.Length > 0 ? "   •   " : "") + "📎 " + fileName;

            card.Controls.Add(new Label
            {
                Text = metaLine,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Italic),
                ForeColor = Color.FromArgb(140, 140, 140),
                BackColor = Color.Transparent,
                AutoSize = false,
                AutoEllipsis = true,
                Size = new Size(cardWidth - 40, 18),
                Location = new Point(20, 78)
            });

            activitiesList.Controls.Add(card);
        }
    }
}