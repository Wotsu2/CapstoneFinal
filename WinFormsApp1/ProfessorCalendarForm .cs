using Guna.UI2.WinForms;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Windows.Forms;

namespace WinFormsApp1
{
    public partial class ProfessorCalendarForm : Form
    {
        // =========================================================
        // FIELDS
        // =========================================================
        private readonly int professorId;

        private DateTime currentMonth = DateTime.Today;

        private Panel headerBar;
        private Label lblMonthYear;
        private Label lblYear;
        private Guna2CircleButton btnPrev;
        private Guna2CircleButton btnNext;
        private Guna2Button btnToday;
        private TableLayoutPanel gridPanel;

        private readonly List<Panel> dayCells = new List<Panel>();

        // Color scheme (matching Student Calendar)
        private readonly Color AssessmentColor = Color.FromArgb(196, 106, 74); // terracotta (Activity)
        private readonly Color QuizColor = Color.FromArgb(52, 120, 200);      // blue (Quiz/Exam)
        private readonly Color GridLine = Color.FromArgb(230, 230, 230);
        private readonly Color ChipBack = Color.FromArgb(240, 242, 245);

        // =========================================================
        // CTOR
        // =========================================================
        public ProfessorCalendarForm(int profId)
        {
            professorId = profId;

            BuildUi();
            RenderMonth(currentMonth);
        }

        // =========================================================
        // UI
        // =========================================================
        private void BuildUi()
        {
            this.Text = "My Calendar - Activities & Quizzes";
            this.Size = new Size(1400, 900);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.White;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MinimizeBox = true;
            this.MaximizeBox = true;
            this.ShowIcon = false;

            // ---------------- HEADER ----------------
            headerBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = Color.White
            };
            this.Controls.Add(headerBar);

            // Prev
            btnPrev = new Guna2CircleButton
            {
                Text = "‹",
                Size = new Size(38, 38),
                Location = new Point(20, 16),
                FillColor = Color.Transparent,
                ForeColor = Color.FromArgb(60, 60, 60),
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                BorderColor = Color.FromArgb(220, 220, 220),
                BorderThickness = 1
            };
            btnPrev.HoverState.FillColor = Color.FromArgb(245, 245, 245);
            btnPrev.Click += (s, e) => { currentMonth = currentMonth.AddMonths(-1); RenderMonth(currentMonth); };
            headerBar.Controls.Add(btnPrev);

            // Next
            btnNext = new Guna2CircleButton
            {
                Text = "›",
                Size = new Size(38, 38),
                Location = new Point(66, 16),
                FillColor = Color.Transparent,
                ForeColor = Color.FromArgb(60, 60, 60),
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                BorderColor = Color.FromArgb(220, 220, 220),
                BorderThickness = 1
            };
            btnNext.HoverState.FillColor = Color.FromArgb(245, 245, 245);
            btnNext.Click += (s, e) => { currentMonth = currentMonth.AddMonths(1); RenderMonth(currentMonth); };
            headerBar.Controls.Add(btnNext);

            // Month name
            lblMonthYear = new Label
            {
                Text = "March",
                Font = new Font("Segoe UI", 22F, FontStyle.Regular),
                ForeColor = Color.FromArgb(60, 60, 60),
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(125, 20)
            };
            headerBar.Controls.Add(lblMonthYear);

            // Year
            lblYear = new Label
            {
                Text = "2026",
                Font = new Font("Segoe UI", 11F),
                ForeColor = Color.FromArgb(120, 120, 120),
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(240, 48)
            };
            headerBar.Controls.Add(lblYear);

            // Today button
            btnToday = new Guna2Button
            {
                Text = "Today",
                Size = new Size(90, 38),
                Location = new Point(headerBar.Width - 130, 16),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BorderRadius = 8,
                FillColor = Color.Transparent,
                ForeColor = Color.FromArgb(60, 60, 60),
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                BorderColor = Color.FromArgb(220, 220, 220),
                BorderThickness = 1
            };
            btnToday.HoverState.FillColor = Color.FromArgb(245, 245, 245);
            btnToday.Click += (s, e) =>
            {
                currentMonth = DateTime.Today;
                RenderMonth(currentMonth);
            };
            headerBar.Controls.Add(btnToday);

            // ---------------- GRID ----------------
            gridPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 7,
                RowCount = 7, // 1 for day-of-week + 6 for weeks
                BackColor = Color.White,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            for (int i = 0; i < 7; i++)
                gridPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / 7));
            gridPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40)); // dow row
            for (int i = 0; i < 6; i++)
                gridPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / 6));
            this.Controls.Add(gridPanel);
            gridPanel.BringToFront();

            // Day-of-week header cells
            string[] dowNames = { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };
            for (int i = 0; i < 7; i++)
            {
                Label lbl = new Label
                {
                    Text = dowNames[i],
                    Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(120, 120, 120),
                    BackColor = Color.White,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Dock = DockStyle.Fill
                };
                gridPanel.Controls.Add(lbl, i, 0);
            }

            // 42 day cells
            for (int i = 0; i < 42; i++)
            {
                var cell = new Panel
                {
                    Dock = DockStyle.Fill,
                    BackColor = Color.White,
                    Margin = new Padding(0),
                    Tag = null
                };

                cell.Paint += (s, e) =>
                {
                    var p = (Panel)s;
                    using (var pen = new Pen(GridLine, 1))
                    {
                        e.Graphics.DrawLine(pen, 0, 0, p.Width, 0);
                        e.Graphics.DrawLine(pen, 0, 0, 0, p.Height);
                    }
                };

                cell.Click += (s, e) =>
                {
                    if (cell.Tag is DateTime dt)
                        Console.WriteLine("Clicked: " + dt.ToShortDateString());
                };

                dayCells.Add(cell);
                gridPanel.Controls.Add(cell, i % 7, (i / 7) + 1);
            }
        }

        // =========================================================
        // RENDER MONTH
        // =========================================================
        private void RenderMonth(DateTime month)
        {
            currentMonth = new DateTime(month.Year, month.Month, 1);
            lblMonthYear.Text = currentMonth.ToString("MMMM", CultureInfo.InvariantCulture);
            lblYear.Text = currentMonth.Year.ToString();

            int offset = (int)currentMonth.DayOfWeek;
            DateTime firstCell = currentMonth.AddDays(-offset);

            // Clear cells
            foreach (var cell in dayCells)
            {
                cell.Controls.Clear();
                cell.Tag = null;
                cell.BackColor = Color.White;
            }

            // Load data
            DateTime rangeStart = firstCell.Date;
            DateTime rangeEnd = firstCell.AddDays(41).Date;

            var activities = LoadActivitiesInRange(rangeStart, rangeEnd);
            var quizzes = LoadQuizzesInRange(rangeStart, rangeEnd);

            for (int i = 0; i < 42; i++)
            {
                DateTime cellDate = firstCell.AddDays(i);
                var cell = dayCells[i];
                cell.Tag = cellDate;

                bool inMonth = cellDate.Month == currentMonth.Month;
                bool isToday = cellDate.Date == DateTime.Today;

                // -------- Day number --------
                if (isToday)
                {
                    var circle = new Label
                    {
                        Text = cellDate.Day.ToString(),
                        Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                        ForeColor = Color.White,
                        BackColor = Color.FromArgb(66, 133, 244),
                        TextAlign = ContentAlignment.MiddleCenter,
                        AutoSize = false,
                        Size = new Size(28, 28),
                        Location = new Point(8, 8)
                    };
                    circle.Paint += (s, e) =>
                    {
                        var g = e.Graphics;
                        g.SmoothingMode = SmoothingMode.AntiAlias;
                        using (var path = new GraphicsPath())
                        {
                            path.AddEllipse(0, 0, circle.Width - 1, circle.Height - 1);
                            circle.Region = new Region(path);
                        }
                    };
                    cell.Controls.Add(circle);
                }
                else
                {
                    var lblDay = new Label
                    {
                        Text = cellDate.Day.ToString(),
                        Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                        ForeColor = inMonth
                            ? Color.FromArgb(70, 70, 70)
                            : Color.FromArgb(200, 200, 200),
                        BackColor = Color.Transparent,
                        AutoSize = false,
                        Size = new Size(40, 24),
                        Location = new Point(12, 10),
                        TextAlign = ContentAlignment.MiddleLeft
                    };
                    cell.Controls.Add(lblDay);
                }

                // -------- Event chips --------
                var dayActivities = activities.FindAll(a => a.Date.Date == cellDate.Date);
                var dayQuizzes = quizzes.FindAll(q => q.Date.Date == cellDate.Date);

                int y = 42;
                int chipHeight = 42;
                int chipSpacing = 6;
                int maxChips = 3;
                int shown = 0;

                foreach (var a in dayActivities)
                {
                    if (shown >= maxChips) break;
                    cell.Controls.Add(MakeEventChip(cell, "Due", a.Title, AssessmentColor, y));
                    y += chipHeight + chipSpacing;
                    shown++;
                }
                foreach (var q in dayQuizzes)
                {
                    if (shown >= maxChips) break;
                    cell.Controls.Add(MakeEventChip(cell, "Quiz", q.Title, QuizColor, y));
                    y += chipHeight + chipSpacing;
                    shown++;
                }

                int total = dayActivities.Count + dayQuizzes.Count;
                if (total > maxChips)
                {
                    var more = new Label
                    {
                        Text = $"+{total - maxChips} more",
                        Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                        ForeColor = Color.FromArgb(120, 120, 120),
                        BackColor = Color.Transparent,
                        AutoSize = false,
                        Size = new Size(cell.Width - 20, 18),
                        Location = new Point(12, y),
                        TextAlign = ContentAlignment.MiddleLeft
                    };
                    cell.Controls.Add(more);
                }
            }
        }

        // =========================================================
        // EVENT CHIP
        // =========================================================
        private Panel MakeEventChip(Panel parent, string prefix, string title, Color accent, int y)
        {
            var chip = new Panel
            {
                BackColor = ChipBack,
                Location = new Point(6, y),
                Size = new Size(Math.Max(60, parent.Width - 12), 42),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            var strip = new Panel
            {
                Dock = DockStyle.Left,
                Width = 4,
                BackColor = accent
            };
            chip.Controls.Add(strip);

            var lbl = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.FromArgb(60, 60, 60),
                BackColor = Color.Transparent,
                AutoSize = false,
                AutoEllipsis = true,
                Location = new Point(10, 2),
                Size = new Size(chip.Width - 12, 38),
                TextAlign = ContentAlignment.MiddleLeft
            };

            lbl.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                using (var boldFont = new Font("Segoe UI Semibold", 8F, FontStyle.Bold))
                using (var normalFont = new Font("Segoe UI", 8F))
                using (var brush = new SolidBrush(Color.FromArgb(60, 60, 60)))
                {
                    SizeF boldSize = g.MeasureString(prefix, boldFont);
                    g.DrawString(prefix, boldFont, brush, 0, 2);

                    using (var wrapBrush = new SolidBrush(Color.FromArgb(40, 40, 40)))
                    {
                        var titleRect = new RectangleF(0, 2 + boldSize.Height, lbl.Width, lbl.Height - boldSize.Height);
                        g.DrawString(title, normalFont, wrapBrush, titleRect);
                    }
                }
            };

            chip.Controls.Add(lbl);

            parent.SizeChanged += (s, e) =>
            {
                chip.Width = Math.Max(60, parent.Width - 12);
            };

            return chip;
        }

        // =========================================================
        // DATA QUERIES
        // =========================================================
        private class ScheduleItem
        {
            public DateTime Date;
            public string Title;
            public string Subject;
            public string Status;
            public string Type;
        }

        private List<ScheduleItem> LoadActivitiesInRange(DateTime start, DateTime end)
        {
            var list = new List<ScheduleItem>();
            string connStr = SettingsManager.Current.GetConnectionString();

            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    // All activities the professor posted, regardless of section
                    string q = @"SELECT title, activity_subject, activity_status, due_date
                                 FROM professor_activity
                                 WHERE professor_id = @prof_id
                                   AND DATE(due_date) BETWEEN @start AND @end
                                 ORDER BY due_date ASC";

                    using (var cmd = new MySqlCommand(q, conn))
                    {
                        cmd.Parameters.AddWithValue("@prof_id", professorId);
                        cmd.Parameters.AddWithValue("@start", start.Date);
                        cmd.Parameters.AddWithValue("@end", end.Date);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                DateTime d;
                                var raw = reader["due_date"];
                                if (raw == null || raw == DBNull.Value) continue;
                                if (!DateTime.TryParse(raw.ToString(), out d)) continue;

                                list.Add(new ScheduleItem
                                {
                                    Date = d.Date,
                                    Title = reader["title"]?.ToString() ?? "",
                                    Subject = reader["activity_subject"]?.ToString() ?? "",
                                    Status = reader["activity_status"]?.ToString() ?? ""
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("LoadActivitiesInRange: " + ex.Message);
            }

            return list;
        }

        private List<ScheduleItem> LoadQuizzesInRange(DateTime start, DateTime end)
        {
            var list = new List<ScheduleItem>();
            string connStr = SettingsManager.Current.GetConnectionString();

            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    // Try filtering by professor_id if the column exists.
                    // If your quizzes table uses a different column, adjust this query.
                    string q = @"SELECT quiz_title, subject, assessment_type, created_at
                                 FROM quizzes
                                 WHERE DATE(created_at) BETWEEN @start AND @end
                                 ORDER BY created_at ASC";

                    using (var cmd = new MySqlCommand(q, conn))
                    {
                        cmd.Parameters.AddWithValue("@start", start.Date);
                        cmd.Parameters.AddWithValue("@end", end.Date);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                DateTime d;
                                var raw = reader["created_at"];
                                if (raw == null || raw == DBNull.Value) continue;
                                if (!DateTime.TryParse(raw.ToString(), out d)) continue;

                                list.Add(new ScheduleItem
                                {
                                    Date = d.Date,
                                    Title = reader["quiz_title"]?.ToString() ?? "",
                                    Subject = reader["subject"]?.ToString() ?? "",
                                    Type = reader["assessment_type"]?.ToString() ?? "Quiz"
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("LoadQuizzesInRange: " + ex.Message);
            }

            return list;
        }
    }
}