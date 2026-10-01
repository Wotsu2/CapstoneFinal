using Guna.UI2.WinForms;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace WinFormsApp1
{
    public partial class ClassRoomForm : Form
    {
        private readonly int studentId;
        private readonly string studentName;
        private readonly int professorId;
        private readonly string className;
        private readonly string classSection;
        private readonly string classTime;
        private readonly string classDate;

        private Guna2Panel headerPanel;
        private Guna2Panel tabStrip;
        private Panel bodyPanel;

        private Guna2Button btnStream;
        private Guna2Button btnActivities;
        private Guna2Button btnQuizzes;
        private Guna2Button btnGrades;

        private Panel streamPage;
        private Panel activitiesPage;
        private Panel quizzesPage;
        private Panel gradesPage;

        private readonly Color Maroon = Color.FromArgb(123, 15, 23);
        private readonly Color SoftPink = Color.FromArgb(253, 236, 238);
        private readonly Color CardBorder = Color.FromArgb(230, 225, 225);
        private readonly Color MutedText = Color.FromArgb(130, 130, 130);

        public ClassRoomForm(
            int studentId,
            string studentName,
            int professorId,
            string className,
            string classSection,
            string classTime,
            string classDate)
        {
            this.studentId = studentId;
            this.studentName = studentName;
            this.professorId = professorId;
            this.className = className ?? "";
            this.classSection = classSection ?? "";
            this.classTime = classTime ?? "";
            this.classDate = classDate ?? "";

            BuildUi();
            ShowTab("stream");
        }

        // =========================================================
        // UI
        // =========================================================
        private void BuildUi()
        {
            this.Text = className + "  •  " + classSection;
            this.Size = new Size(1200, 800);
            this.MinimumSize = new Size(900, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(250, 247, 244);
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.ShowIcon = false;

            // ---------- HEADER ----------
            headerPanel = new Guna2Panel
            {
                Dock = DockStyle.Top,
                Height = 180,
                FillColor = Maroon,
                BorderRadius = 0
            };
            this.Controls.Add(headerPanel);

            Label lblTitle = new Label
            {
                Text = className,
                Font = new Font("Segoe UI Semibold", 26F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                AutoSize = false,
                AutoEllipsis = true,
                Size = new Size(700, 46),
                Location = new Point(40, 40)
            };
            headerPanel.Controls.Add(lblTitle);

            Label lblSub = new Label
            {
                Text = $"Section {classSection}  •  {classDate}  •  {classTime}",
                Font = new Font("Segoe UI", 11F),
                ForeColor = Color.FromArgb(240, 220, 220),
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(44, 92)
            };
            headerPanel.Controls.Add(lblSub);

            Label lblTeacher = new Label
            {
                Text = "Professor ID: " + professorId,
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = Color.FromArgb(220, 200, 200),
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(44, 118)
            };
            headerPanel.Controls.Add(lblTeacher);

            // ---------- TAB STRIP ----------
            tabStrip = new Guna2Panel
            {
                Dock = DockStyle.Top,
                Height = 62,
                FillColor = Color.White
            };
            this.Controls.Add(tabStrip);
            tabStrip.BringToFront();

            btnStream = MakeTabButton("Stream", 20);
            btnActivities = MakeTabButton("Activities", 180);
            btnQuizzes = MakeTabButton("Quizzes & Exams", 340);
            btnGrades = MakeTabButton("Grades", 560);

            btnStream.Click += (s, e) => ShowTab("stream");
            btnActivities.Click += (s, e) => ShowTab("activities");
            btnQuizzes.Click += (s, e) => ShowTab("quizzes");
            btnGrades.Click += (s, e) => ShowTab("grades");

            tabStrip.Controls.Add(btnStream);
            tabStrip.Controls.Add(btnActivities);
            tabStrip.Controls.Add(btnQuizzes);
            tabStrip.Controls.Add(btnGrades);

            // ---------- BODY ----------
            bodyPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(250, 247, 244),
                Padding = new Padding(30, 25, 30, 25),
                AutoScroll = true
            };
            this.Controls.Add(bodyPanel);
            bodyPanel.BringToFront();

            streamPage = MakePage();
            activitiesPage = MakePage();
            quizzesPage = MakePage();
            gradesPage = MakePage();

            bodyPanel.Controls.Add(streamPage);
            bodyPanel.Controls.Add(activitiesPage);
            bodyPanel.Controls.Add(quizzesPage);
            bodyPanel.Controls.Add(gradesPage);
        }

        private Guna2Button MakeTabButton(string text, int left)
        {
            var btn = new Guna2Button
            {
                Text = text,
                Size = new Size(160, 42),
                Location = new Point(left, 11),
                BorderRadius = 8,
                FillColor = Color.Transparent,
                ForeColor = Color.FromArgb(60, 60, 60),
                Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Tag = text
            };
            btn.HoverState.FillColor = SoftPink;
            btn.HoverState.ForeColor = Maroon;
            return btn;
        }

        private Panel MakePage()
        {
            return new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(250, 247, 244),
                AutoScroll = true,
                Visible = false
            };
        }

        private void ShowTab(string tab)
        {
            streamPage.Visible = false;
            activitiesPage.Visible = false;
            quizzesPage.Visible = false;
            gradesPage.Visible = false;

            HighlightTab(btnStream, tab == "stream");
            HighlightTab(btnActivities, tab == "activities");
            HighlightTab(btnQuizzes, tab == "quizzes");
            HighlightTab(btnGrades, tab == "grades");

            switch (tab)
            {
                case "stream":
                    streamPage.Visible = true;
                    BuildStreamPage();
                    break;
                case "activities":
                    activitiesPage.Visible = true;
                    BuildActivitiesPage();
                    break;
                case "quizzes":
                    quizzesPage.Visible = true;
                    BuildQuizzesPage();
                    break;
                case "grades":
                    gradesPage.Visible = true;
                    BuildGradesPage();
                    break;
            }
        }

        private void HighlightTab(Guna2Button btn, bool active)
        {
            btn.FillColor = active ? Maroon : Color.Transparent;
            btn.ForeColor = active ? Color.White : Color.FromArgb(60, 60, 60);
        }

        // =========================================================
        // STREAM
        // =========================================================
        private void BuildStreamPage()
        {
            streamPage.Controls.Clear();

            streamPage.Controls.Add(MakeSectionHeader("Stream", "Recent posts from this class"));

            var activityItems = LoadActivityItems();
            var quizItems = LoadQuizItems();

            int y = 90;

            foreach (var a in activityItems)
            {
                streamPage.Controls.Add(MakeStreamCard(
                    "📄  New Activity Posted", a.Title, a.Subject + "  •  Due " + a.DueDate, y));
                y += 100;
            }

            foreach (var q in quizItems)
            {
                streamPage.Controls.Add(MakeStreamCard(
                    "📝  New Quiz / Exam Posted", q.Title, q.Subject + "  •  " + q.Type, y));
                y += 100;
            }

            if (activityItems.Count == 0 && quizItems.Count == 0)
            {
                streamPage.Controls.Add(MakeEmptyCard("No posts yet. Everything will appear here when your professor posts something.", 90));
            }
        }

        private Guna2Panel MakeSectionHeader(string title, string subtitle)
        {
            var p = new Guna2Panel
            {
                Location = new Point(0, 0),
                Size = new Size(bodyPanel.ClientSize.Width - 60, 70),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                FillColor = Color.Transparent,
                BackColor = Color.Transparent
            };

            p.Controls.Add(new Label
            {
                Text = title,
                Font = new Font("Segoe UI Semibold", 20F, FontStyle.Bold),
                ForeColor = Maroon,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(0, 0)
            });

            p.Controls.Add(new Label
            {
                Text = subtitle,
                Font = new Font("Segoe UI", 10.5F),
                ForeColor = MutedText,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(2, 38)
            });

            return p;
        }

        private Guna2Panel MakeStreamCard(string tag, string title, string meta, int y)
        {
            var card = new Guna2Panel
            {
                Location = new Point(0, y),
                Size = new Size(bodyPanel.ClientSize.Width - 60, 82),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BorderRadius = 12,
                FillColor = Color.White,
                BorderColor = CardBorder,
                BorderThickness = 1,
                ShadowDecoration = { Enabled = true, Depth = 6, Color = Color.FromArgb(30, 0, 0, 0) }
            };

            card.Controls.Add(new Label
            {
                Text = tag,
                Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
                ForeColor = Maroon,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(20, 14)
            });

            card.Controls.Add(new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 12.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 40, 40),
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(20, 38)
            });

            card.Controls.Add(new Label
            {
                Text = meta,
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = MutedText,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(20, 60)
            });

            return card;
        }

        private Guna2Panel MakeEmptyCard(string text, int y)
        {
            var card = new Guna2Panel
            {
                Location = new Point(0, y),
                Size = new Size(bodyPanel.ClientSize.Width - 60, 120),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BorderRadius = 12,
                FillColor = Color.White,
                BorderColor = CardBorder,
                BorderThickness = 1
            };

            card.Controls.Add(new Label
            {
                Text = "📭  " + text,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Italic),
                ForeColor = MutedText,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            });

            return card;
        }

        // =========================================================
        // ACTIVITIES
        // =========================================================
        private void BuildActivitiesPage()
        {
            activitiesPage.Controls.Clear();
            activitiesPage.Controls.Add(MakeSectionHeader("Activities", "Assignments and tasks for this class"));

            var items = LoadActivityItems();
            int y = 90;

            foreach (var a in items)
            {
                var card = new Guna2Panel
                {
                    Location = new Point(0, y),
                    Size = new Size(bodyPanel.ClientSize.Width - 60, 100),
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                    BorderRadius = 12,
                    FillColor = Color.White,
                    BorderColor = CardBorder,
                    BorderThickness = 1,
                    Cursor = Cursors.Hand,
                    Tag = a.Id
                };

                card.Controls.Add(new Label
                {
                    Text = a.Title,
                    Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(40, 40, 40),
                    BackColor = Color.Transparent,
                    AutoSize = true,
                    Location = new Point(20, 14)
                });

                card.Controls.Add(new Label
                {
                    Text = a.Subject + "  •  Due " + a.DueDate + "  •  " + a.Status,
                    Font = new Font("Segoe UI", 9.5F),
                    ForeColor = MutedText,
                    BackColor = Color.Transparent,
                    AutoSize = true,
                    Location = new Point(20, 44)
                });

                card.Controls.Add(new Label
                {
                    Text = "Open  ›",
                    Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                    ForeColor = Maroon,
                    BackColor = Color.Transparent,
                    AutoSize = true,
                    Anchor = AnchorStyles.Top | AnchorStyles.Right,
                    Location = new Point(card.Width - 90, 40)
                });

                int capturedId = a.Id;
                card.Click += (s, e) => OpenActivity(capturedId);

                activitiesPage.Controls.Add(card);
                y += 115;
            }

            if (items.Count == 0)
                activitiesPage.Controls.Add(MakeEmptyCard("No activities posted for this class yet.", 90));
        }

        private void OpenActivity(int activityId)
        {
            try
            {
                var form = new ActivityForm(
                    professorId, studentId.ToString(), studentName,
                    "", "", "", classSection, className, "Pending", null);

                form.ShowDialog(this);
                BuildActivitiesPage();
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show("Unable to open activity: " + ex.Message,
                    "Open Error", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Error);
            }
        }

        // =========================================================
        // QUIZZES
        // =========================================================
        private void BuildQuizzesPage()
        {
            quizzesPage.Controls.Clear();
            quizzesPage.Controls.Add(MakeSectionHeader("Quizzes & Exams", "Assessments assigned to this class"));

            var items = LoadQuizItems();
            int y = 90;

            foreach (var q in items)
            {
                var card = new Guna2Panel
                {
                    Location = new Point(0, y),
                    Size = new Size(bodyPanel.ClientSize.Width - 60, 100),
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                    BorderRadius = 12,
                    FillColor = Color.White,
                    BorderColor = CardBorder,
                    BorderThickness = 1,
                    Cursor = Cursors.Hand,
                    Tag = q.Id
                };

                card.Controls.Add(new Label
                {
                    Text = q.Title,
                    Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(40, 40, 40),
                    BackColor = Color.Transparent,
                    AutoSize = true,
                    Location = new Point(20, 14)
                });

                card.Controls.Add(new Label
                {
                    Text = q.Subject + "  •  " + q.Type + (string.IsNullOrEmpty(q.Period) ? "" : "  •  " + q.Period),
                    Font = new Font("Segoe UI", 9.5F),
                    ForeColor = MutedText,
                    BackColor = Color.Transparent,
                    AutoSize = true,
                    Location = new Point(20, 44)
                });

                card.Controls.Add(new Label
                {
                    Text = "Start  ›",
                    Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                    ForeColor = Maroon,
                    BackColor = Color.Transparent,
                    AutoSize = true,
                    Anchor = AnchorStyles.Top | AnchorStyles.Right,
                    Location = new Point(card.Width - 90, 40)
                });

                int capturedId = q.Id;
                card.Click += (s, e) => OpenQuiz(capturedId);

                quizzesPage.Controls.Add(card);
                y += 115;
            }

            if (items.Count == 0)
                quizzesPage.Controls.Add(MakeEmptyCard("No quizzes or exams posted for this class yet.", 90));
        }

        private void OpenQuiz(int quizId)
        {
            try
            {
                var form = new StudentQuizForm(studentId, quizId);
                form.ShowDialog(this);
                BuildQuizzesPage();
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show("Unable to open quiz: " + ex.Message,
                    "Open Error", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Error);
            }
        }

        // =========================================================
        // GRADES
        // =========================================================
        private void BuildGradesPage()
        {
            gradesPage.Controls.Clear();
            gradesPage.Controls.Add(MakeSectionHeader("Grades", "Your scores for this class"));

            var grid = new DataGridView
            {
                Location = new Point(0, 90),
                Size = new Size(bodyPanel.ClientSize.Width - 60, bodyPanel.ClientSize.Height - 130),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToResizeRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                EnableHeadersVisualStyles = false,
                ColumnHeadersHeight = 44,
                Font = new Font("Segoe UI", 10F)
            };

            grid.ColumnHeadersDefaultCellStyle.BackColor = Maroon;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold);
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Maroon;
            grid.DefaultCellStyle.Font = new Font("Segoe UI", 9.5F);
            grid.DefaultCellStyle.SelectionBackColor = SoftPink;
            grid.DefaultCellStyle.SelectionForeColor = Maroon;
            grid.RowTemplate.Height = 40;

            LoadGradesIntoGrid(grid);

            gradesPage.Controls.Add(grid);
        }

        private void LoadGradesIntoGrid(DataGridView grid)
        {
            string connStr = SettingsManager.Current.GetConnectionString();
            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    string query = @"
                        SELECT 
                            q.quiz_title  AS 'Quiz / Exam',
                            q.subject     AS 'Subject',
                            qa.score      AS 'Score',
                            qa.total_questions AS 'Total',
                            qa.percentage AS 'Percentage',
                            qa.submitted_at AS 'Submitted'
                        FROM quiz_attempts qa
                        INNER JOIN quizzes q ON q.quiz_id = qa.quiz_id
                        WHERE qa.user_id = @user_id
                          AND qa.status = 'SUBMITTED'
                          AND q.subject = @subject
                        ORDER BY qa.submitted_at DESC";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@user_id", studentId);
                        cmd.Parameters.AddWithValue("@subject", className);

                        MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        grid.DataSource = dt;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("LoadGradesIntoGrid error: " + ex.Message);
            }
        }

        // =========================================================
        // DATA HELPERS
        // =========================================================
        private class SimpleItem
        {
            public int Id;
            public string Title;
            public string Subject;
            public string DueDate;
            public string Status;
            public string Type;
            public string Period;
        }

        private List<SimpleItem> LoadActivityItems()
        {
            var list = new List<SimpleItem>();
            string connStr = SettingsManager.Current.GetConnectionString();

            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    string q = @"
                        SELECT pa.activity_id, pa.title, pa.activity_subject,
                               pa.due_date, pa.activity_status
                        FROM professor_activity pa
                        WHERE pa.professor_id = @prof_id
                          AND LOWER(TRIM(pa.section)) = LOWER(TRIM(@section))
                          AND pa.activity_subject = @subject
                        ORDER BY pa.due_date ASC";

                    using (var cmd = new MySqlCommand(q, conn))
                    {
                        cmd.Parameters.AddWithValue("@prof_id", professorId);
                        cmd.Parameters.AddWithValue("@section", classSection);
                        cmd.Parameters.AddWithValue("@subject", className);

                        using (var r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                list.Add(new SimpleItem
                                {
                                    Id = Convert.ToInt32(r["activity_id"]),
                                    Title = r["title"]?.ToString() ?? "",
                                    Subject = r["activity_subject"]?.ToString() ?? "",
                                    DueDate = r["due_date"]?.ToString() ?? "",
                                    Status = r["activity_status"]?.ToString() ?? ""
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine("LoadActivityItems error: " + ex.Message); }

            return list;
        }

        private List<SimpleItem> LoadQuizItems()
        {
            var list = new List<SimpleItem>();
            string connStr = SettingsManager.Current.GetConnectionString();

            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    string q = @"
                        SELECT quiz_id, quiz_title, subject, assessment_type, exam_period
                        FROM quizzes
                        WHERE subject = @subject
                        ORDER BY created_at DESC";

                    using (var cmd = new MySqlCommand(q, conn))
                    {
                        cmd.Parameters.AddWithValue("@subject", className);

                        using (var r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                list.Add(new SimpleItem
                                {
                                    Id = Convert.ToInt32(r["quiz_id"]),
                                    Title = r["quiz_title"]?.ToString() ?? "",
                                    Subject = r["subject"]?.ToString() ?? "",
                                    Type = r["assessment_type"]?.ToString() ?? "quiz",
                                    Period = r["exam_period"]?.ToString() ?? ""
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine("LoadQuizItems error: " + ex.Message); }

            return list;
        }
    }
}