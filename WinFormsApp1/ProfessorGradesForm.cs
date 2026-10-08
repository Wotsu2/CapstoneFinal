using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Guna.UI2.WinForms;
using MySql.Data.MySqlClient;

namespace WinFormsApp1
{
    public class ProfessorGradesForm : Form
    {
        // Palette (same as the rest of the app)
        private static readonly Color ClrMaroon = Color.FromArgb(94, 14, 33);
        private static readonly Color ClrMaroonDark = Color.FromArgb(70, 10, 24);
        private static readonly Color ClrPageBg = Color.FromArgb(243, 244, 246);
        private static readonly Color ClrCardBg = Color.White;
        private static readonly Color ClrBorder = Color.FromArgb(218, 222, 228);
        private static readonly Color ClrGridLine = Color.FromArgb(238, 240, 243);
        private static readonly Color ClrText = Color.FromArgb(31, 41, 55);
        private static readonly Color ClrMuted = Color.FromArgb(107, 114, 128);
        private static readonly Color ClrAmber = Color.FromArgb(160, 80, 0);
        private static readonly Color ClrHoverLight = Color.FromArgb(243, 244, 246);
        private static readonly Color ClrRowSelect = Color.FromArgb(252, 242, 226);
        private static readonly Color ClrGold = Color.FromArgb(198, 156, 53);
        private static readonly Color ClrGoldSoft = Color.FromArgb(230, 210, 180);

        private Guna2Panel filterCard;
        private Guna2ComboBox cmbAssessment;
        private Guna2TextBox txtSearch;
        private Guna2Button btnRefresh;
        private Guna2Button btnViewAnswers;

        private Guna2Panel tableCard;
        private DataGridView dgvGrades;
        private Label plusLink;

        private DataTable gradesTable = new DataTable();

        private class AssessmentItem
        {
            public int QuizID;
            public string Title;

            public override string ToString()
            {
                return Title;
            }
        }

        public ProfessorGradesForm()
        {
            BuildInterface();
            LoadAssessments();
            LoadGrades();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            PositionPlusLink();
        }

        // =========================================================
        // BUILD INTERFACE
        // =========================================================

        private void BuildInterface()
        {
            Text = "Student Grades";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(1100, 700);
            MinimumSize = new Size(900, 560);
            BackColor = ClrPageBg;
            Font = new Font("Segoe UI", 9.5F);

            // ---------------- TITLE ----------------
            Label lblTitle = new Label();
            lblTitle.Text = "STUDENT GRADES";
            lblTitle.Font = new Font("Segoe UI", 22F, FontStyle.Bold);
            lblTitle.ForeColor = ClrText;
            lblTitle.BackColor = Color.Transparent;
            lblTitle.AutoSize = true;
            lblTitle.Location = new Point(40, 26);
            Controls.Add(lblTitle);

            Label lblSub = new Label();
            lblSub.Text = "View the results of the students who completed quizzes and exams.";
            lblSub.Font = new Font("Segoe UI", 10F);
            lblSub.ForeColor = ClrMuted;
            lblSub.BackColor = Color.Transparent;
            lblSub.AutoSize = true;
            lblSub.Location = new Point(42, 78);
            Controls.Add(lblSub);

            // ---------------- FILTER CARD ----------------
            filterCard = MakeCard(40, 118, 1020, 80);
            filterCard.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            Controls.Add(filterCard);

            filterCard.Controls.Add(SmallCaption("ASSESSMENT", 22, 14));

            cmbAssessment = new Guna2ComboBox();
            cmbAssessment.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbAssessment.Location = new Point(22, 36);
            cmbAssessment.Size = new Size(300, 34);
            cmbAssessment.FillColor = Color.White;
            cmbAssessment.BorderColor = ClrBorder;
            cmbAssessment.BorderThickness = 1;
            cmbAssessment.BorderRadius = 17;
            cmbAssessment.ForeColor = ClrText;
            cmbAssessment.Font = new Font("Segoe UI", 10F);
            cmbAssessment.SelectedIndexChanged += (s, e) => ApplyFilter();
            filterCard.Controls.Add(cmbAssessment);

            filterCard.Controls.Add(SmallCaption("SEARCH", 344, 14));

            txtSearch = new Guna2TextBox();
            txtSearch.PlaceholderText = "Name, student ID, assessment, or permit...";
            txtSearch.Location = new Point(344, 36);
            txtSearch.Size = new Size(340, 34);
            txtSearch.FillColor = Color.White;
            txtSearch.BorderColor = ClrBorder;
            txtSearch.BorderThickness = 1;
            txtSearch.BorderRadius = 17;
            txtSearch.ForeColor = ClrText;
            txtSearch.Font = new Font("Segoe UI", 10F);
            txtSearch.FocusedState.BorderColor = ClrMaroon;
            txtSearch.TextChanged += (s, e) => ApplyFilter();
            filterCard.Controls.Add(txtSearch);

            btnRefresh = MakeButton("REFRESH", 706, 34, 130, 38,
                Color.White, ClrHoverLight, ClrText, true);
            btnRefresh.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnRefresh.Click += (s, e) =>
            {
                LoadAssessments();
                LoadGrades();
            };
            filterCard.Controls.Add(btnRefresh);

            btnViewAnswers = MakeButton("VIEW ANSWERS", 848, 34, 150, 38,
                ClrMaroon, ClrMaroonDark, Color.White, false);
            btnViewAnswers.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnViewAnswers.Click += (s, e) => OpenSelectedAnswers();
            filterCard.Controls.Add(btnViewAnswers);

            // ---------------- TABLE CARD ----------------
            tableCard = MakeCard(40, 218, 1020, 440);
            tableCard.Anchor = AnchorStyles.Top | AnchorStyles.Bottom |
                               AnchorStyles.Left | AnchorStyles.Right;
            tableCard.Padding = new Padding(16);
            Controls.Add(tableCard);

            dgvGrades = new DataGridView();
            dgvGrades.Location = new Point(16, 16);
            dgvGrades.Size = new Size(tableCard.Width - 32, tableCard.Height - 32);
            dgvGrades.Anchor = AnchorStyles.Top | AnchorStyles.Bottom |
                               AnchorStyles.Left | AnchorStyles.Right;
            dgvGrades.ReadOnly = true;
            dgvGrades.AllowUserToAddRows = false;
            dgvGrades.AllowUserToDeleteRows = false;
            dgvGrades.AllowUserToResizeRows = false;
            dgvGrades.AllowUserToResizeColumns = false;
            dgvGrades.RowHeadersVisible = false;
            dgvGrades.MultiSelect = false;
            dgvGrades.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvGrades.AutoGenerateColumns = false;
            dgvGrades.BackgroundColor = ClrCardBg;
            dgvGrades.BorderStyle = BorderStyle.None;
            dgvGrades.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgvGrades.GridColor = ClrGridLine;
            dgvGrades.EnableHeadersVisualStyles = false;
            dgvGrades.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dgvGrades.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgvGrades.ColumnHeadersHeight = 46;
            dgvGrades.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            dgvGrades.RowTemplate.Height = 44;

            dgvGrades.ColumnHeadersDefaultCellStyle.BackColor = ClrCardBg;
            dgvGrades.ColumnHeadersDefaultCellStyle.ForeColor = ClrMuted;
            dgvGrades.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            dgvGrades.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvGrades.ColumnHeadersDefaultCellStyle.SelectionBackColor = ClrCardBg;
            dgvGrades.ColumnHeadersDefaultCellStyle.SelectionForeColor = ClrMuted;

            dgvGrades.DefaultCellStyle.Font = new Font("Segoe UI", 9.5F);
            dgvGrades.DefaultCellStyle.ForeColor = ClrText;
            dgvGrades.DefaultCellStyle.BackColor = ClrCardBg;
            dgvGrades.DefaultCellStyle.SelectionBackColor = ClrRowSelect;
            dgvGrades.DefaultCellStyle.SelectionForeColor = ClrText;
            dgvGrades.DefaultCellStyle.Padding = new Padding(12, 0, 8, 0);
            dgvGrades.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            dgvGrades.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "AttemptId",
                DataPropertyName = "attempt_id",
                Visible = false
            });

            DataGridViewTextBoxColumn colName = MakeColumn("StudentName", "Student Name", "student_name", 230);
            colName.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            colName.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dgvGrades.Columns.Add(colName);

            DataGridViewTextBoxColumn colAssessment = MakeColumn("Assessment", "Assessment", "quiz_title", 0);
            colAssessment.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            colAssessment.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            colAssessment.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dgvGrades.Columns.Add(colAssessment);

            dgvGrades.Columns.Add(MakeColumn("Score", "Score", "score_text", 100));
            dgvGrades.Columns.Add(MakeColumn("Total", "Total", "total_questions", 90));
            dgvGrades.Columns.Add(MakeColumn("Submitted", "Date Submitted", "submitted_text", 200));
            dgvGrades.Columns.Add(MakeColumn("Permits", "Permits", "permit_text", 150));
            dgvGrades.Columns.Add(MakeColumn("Plus", "Plus Points", "plus_text", 110));

            dgvGrades.CellFormatting += Dgv_CellFormatting;
            dgvGrades.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0) OpenSelectedAnswers();
            };

            // "+ Plus points" link placed on top of the Permits header
            plusLink = new Label();
            plusLink.Text = "+ Plus points";
            plusLink.Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold);
            plusLink.ForeColor = ClrMaroon;
            plusLink.BackColor = Color.Transparent;
            plusLink.AutoSize = true;
            plusLink.Cursor = Cursors.Hand;
            plusLink.Click += (s, e) => OpenPlusPoints();

            dgvGrades.Controls.Add(plusLink);
            dgvGrades.Layout += (s, e) => PositionPlusLink();
            dgvGrades.Scroll += (s, e) => PositionPlusLink();
            dgvGrades.Resize += (s, e) => PositionPlusLink();

            tableCard.Controls.Add(dgvGrades);
        }

        private void PositionPlusLink()
        {
            if (plusLink == null || dgvGrades == null) return;
            if (!dgvGrades.Columns.Contains("Permits")) return;

            Rectangle r = dgvGrades.GetCellDisplayRectangle(
                dgvGrades.Columns["Permits"].Index, -1, true);

            if (r.Width <= 0 || r.Height <= 0)
            {
                plusLink.Visible = false;
                return;
            }

            plusLink.Visible = true;
            plusLink.Location = new Point(r.Right - plusLink.Width - 8, r.Top + 4);
            plusLink.BringToFront();
        }

        // =========================================================
        // UI HELPERS
        // =========================================================

        private Guna2Panel MakeCard(int x, int y, int w, int h)
        {
            Guna2Panel card = new Guna2Panel();
            card.Location = new Point(x, y);
            card.Size = new Size(w, h);
            card.FillColor = ClrCardBg;
            card.BorderColor = ClrBorder;
            card.BorderThickness = 1;
            card.BorderRadius = 16;
            card.ShadowDecoration.Enabled = true;
            card.ShadowDecoration.Depth = 6;
            card.ShadowDecoration.Color = Color.FromArgb(25, 0, 0, 0);
            return card;
        }

        private Label SmallCaption(string text, int x, int y)
        {
            Label lbl = new Label();
            lbl.Text = text;
            lbl.Font = new Font("Segoe UI Semibold", 7.5F, FontStyle.Bold);
            lbl.ForeColor = ClrMuted;
            lbl.BackColor = Color.Transparent;
            lbl.AutoSize = true;
            lbl.Location = new Point(x, y);
            return lbl;
        }

        private Guna2Button MakeButton(string text, int x, int y, int w, int h,
                                       Color fill, Color hover, Color fore, bool outline)
        {
            Guna2Button b = new Guna2Button();
            b.Text = text;
            b.Location = new Point(x, y);
            b.Size = new Size(w, h);
            b.BorderRadius = h / 2;
            b.FillColor = fill;
            b.ForeColor = fore;
            b.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            b.HoverState.FillColor = hover;
            b.Cursor = Cursors.Hand;

            if (outline)
            {
                b.BorderThickness = 1;
                b.BorderColor = ClrBorder;
            }

            Controls.Add(b);
            return b;
        }

        private DataGridViewTextBoxColumn MakeColumn(string name, string headerText,
                                                     string prop, int width)
        {
            DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn();
            col.Name = name;
            col.HeaderText = headerText;
            col.DataPropertyName = prop;
            col.Width = width > 0 ? width : 100;
            return col;
        }

        private void Dgv_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;

            DataRowView drv = dgvGrades.Rows[e.RowIndex].DataBoundItem as DataRowView;
            if (drv == null) return;

            string colName = dgvGrades.Columns[e.ColumnIndex].Name;

            if (colName == "Score")
            {
                if (drv["grading_status"].ToString() == "pending")
                {
                    e.CellStyle.ForeColor = ClrAmber;
                    e.CellStyle.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
                }
            }
            else if (colName == "Permits")
            {
                string permit = drv["permit_text"].ToString();

                if (permit == "-" || permit == "N/A")
                {
                    e.CellStyle.ForeColor = ClrMuted;
                }
                else
                {
                    e.CellStyle.ForeColor = ClrMaroon;
                    e.CellStyle.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
                }
            }
            else if (colName == "Plus")
            {
                string plus = drv["plus_text"].ToString();

                if (plus == "N/A")
                {
                    e.CellStyle.ForeColor = ClrMuted;
                }
                else
                {
                    e.CellStyle.ForeColor = ClrMaroon;
                    e.CellStyle.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
                }
            }
        }

        // =========================================================
        // LOAD ASSESSMENTS INTO THE DROPDOWN
        // =========================================================

        private void LoadAssessments()
        {
            cmbAssessment.Items.Clear();
            cmbAssessment.Items.Add(new AssessmentItem { QuizID = 0, Title = "All Assessments" });

            try
            {
                string connStr = SettingsManager.Current.GetConnectionString();

                using (var conn = new MySqlConnection(connStr))
                using (var cmd = new MySqlCommand(@"
                    SELECT quiz_id, quiz_title, assessment_type
                    FROM quizzes
                    ORDER BY created_at DESC", conn))
                {
                    conn.Open();

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string title = reader["quiz_title"] == DBNull.Value
                                ? "" : reader["quiz_title"].ToString();

                            string type = reader["assessment_type"] == DBNull.Value
                                ? "" : reader["assessment_type"].ToString();

                            cmbAssessment.Items.Add(new AssessmentItem
                            {
                                QuizID = Convert.ToInt32(reader["quiz_id"]),
                                Title = title + " [" + type.ToUpper() + "]"
                            });
                        }
                    }
                }

                cmbAssessment.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show("Unable to load assessments.\n\n" + ex.Message,
                    "Database Error", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Error);
            }
        }

        // =========================================================
        // LOAD GRADES
        // =========================================================

        private void LoadGrades()
        {
            gradesTable = new DataTable();
            gradesTable.Columns.Add("attempt_id", typeof(int));
            gradesTable.Columns.Add("user_id", typeof(string));
            gradesTable.Columns.Add("quiz_id", typeof(int));
            gradesTable.Columns.Add("student_name", typeof(string));
            gradesTable.Columns.Add("quiz_title", typeof(string));
            gradesTable.Columns.Add("score_text", typeof(string));
            gradesTable.Columns.Add("total_questions", typeof(int));
            gradesTable.Columns.Add("submitted_text", typeof(string));
            gradesTable.Columns.Add("permit_text", typeof(string));
            gradesTable.Columns.Add("plus_text", typeof(string));
            gradesTable.Columns.Add("grading_status", typeof(string));
            gradesTable.Columns.Add("eligible", typeof(bool));

            try
            {
                string connStr = SettingsManager.Current.GetConnectionString();

                using (var conn = new MySqlConnection(connStr))
                using (var cmd = new MySqlCommand(@"
                    SELECT qa.attempt_id, qa.user_id, qa.quiz_id, qa.student_name,
                           q.quiz_title, qa.score, qa.total_questions,
                           qa.submitted_at, qa.grading_status, qa.permit_no,
                           qa.plus_points
                    FROM quiz_attempts qa
                    INNER JOIN quizzes q ON q.quiz_id = qa.quiz_id
                    WHERE qa.status = 'SUBMITTED'
                    ORDER BY qa.submitted_at DESC", conn))
                {
                    conn.Open();

                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            string status = r["grading_status"] == DBNull.Value
                                ? "auto" : r["grading_status"].ToString();

                            bool isPending = status == "pending";

                            string studentName = r["student_name"] == DBNull.Value
                                ? "Unknown" : r["student_name"].ToString().Replace("_", " ");

                            string submitted = r["submitted_at"] == DBNull.Value
                                ? "" : Convert.ToDateTime(r["submitted_at"]).ToString("MMM dd, yyyy  h:mm tt");

                            string permitRaw = r["permit_no"] == DBNull.Value
                                ? "" : r["permit_no"].ToString().Trim();

                            // Only students with a real permit can receive plus points
                            bool eligible = permitRaw.Length > 0 &&
                                            !permitRaw.Equals("N/A", StringComparison.OrdinalIgnoreCase);

                            string permitText = permitRaw.Length == 0 ? "N/A" : permitRaw;

                            int plus = r["plus_points"] == DBNull.Value
                                ? 0 : Convert.ToInt32(r["plus_points"]);

                            string plusText = eligible ? "+" + plus : "N/A";

                            string scoreText = isPending
                                ? "Pending"
                                : (r["score"] == DBNull.Value ? "0" : r["score"].ToString());

                            gradesTable.Rows.Add(
                                Convert.ToInt32(r["attempt_id"]),
                                r["user_id"].ToString(),
                                Convert.ToInt32(r["quiz_id"]),
                                studentName,
                                r["quiz_title"] == DBNull.Value ? "" : r["quiz_title"].ToString(),
                                scoreText,
                                r["total_questions"] == DBNull.Value ? 0 : Convert.ToInt32(r["total_questions"]),
                                submitted,
                                permitText,
                                plusText,
                                status,
                                eligible);
                        }
                    }
                }

                dgvGrades.DataSource = gradesTable.DefaultView;
                ApplyFilter();
                PositionPlusLink();
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show("Unable to load student grades.\n\n" + ex.Message,
                    "Database Error", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Error);
            }
        }

        // =========================================================
        // FILTER: ASSESSMENT + SEARCH
        // =========================================================

        private void ApplyFilter()
        {
            if (gradesTable == null) return;

            List<string> parts = new List<string>();

            AssessmentItem selected = cmbAssessment.SelectedItem as AssessmentItem;
            if (selected != null && selected.QuizID > 0)
                parts.Add("quiz_id = " + selected.QuizID);

            string search = txtSearch.Text.Trim();
            if (search.Length > 0)
            {
                string s = EscapeLike(search);
                parts.Add("(user_id LIKE '%" + s + "%' OR student_name LIKE '%" + s +
                          "%' OR quiz_title LIKE '%" + s + "%' OR permit_text LIKE '%" + s + "%')");
            }

            gradesTable.DefaultView.RowFilter = string.Join(" AND ", parts);
        }

        private static string EscapeLike(string input)
        {
            return input
                .Replace("[", "[[]")
                .Replace("*", "[*]")
                .Replace("%", "[%]")
                .Replace("'", "''");
        }

        // =========================================================
        // PLUS POINTS
        // =========================================================

        private void OpenPlusPoints()
        {
            AssessmentItem selected = cmbAssessment.SelectedItem as AssessmentItem;

            if (selected == null || selected.QuizID == 0)
            {
                CustomMessageBox.Show(
                    "Pumili muna ng assessment sa dropdown bago magbigay ng plus points.",
                    "Plus Points", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Information);
                return;
            }

            List<int> eligibleIds = new List<int>();
            List<int> excludedIds = new List<int>();

            foreach (DataRow row in gradesTable.Rows)
            {
                if (Convert.ToInt32(row["quiz_id"]) != selected.QuizID) continue;

                int id = Convert.ToInt32(row["attempt_id"]);

                if (Convert.ToBoolean(row["eligible"]))
                    eligibleIds.Add(id);
                else
                    excludedIds.Add(id);
            }

            if (eligibleIds.Count == 0)
            {
                CustomMessageBox.Show(
                    "Walang student na may permit sa assessment na ito.\n\n" +
                    "Ang mga may N/A ay hindi binibigyan ng plus points.",
                    "Plus Points", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Information);
                return;
            }

            using (var dlg = new PlusPointsDialog(selected.Title, eligibleIds.Count, excludedIds.Count))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;

                SavePlusPoints(eligibleIds, excludedIds, dlg.Points);
            }
        }

        private void SavePlusPoints(List<int> eligibleIds, List<int> excludedIds, int points)
        {
            // eligibleIds is never empty here (checked by the caller)
            string eligibleList = string.Join(",", eligibleIds);

            List<int> all = new List<int>(eligibleIds);
            all.AddRange(excludedIds);
            string allList = string.Join(",", all);

            try
            {
                string connStr = SettingsManager.Current.GetConnectionString();

                using (var conn = new MySqlConnection(connStr))
                using (var cmd = new MySqlCommand(
                    "UPDATE quiz_attempts " +
                    "SET plus_points = CASE WHEN attempt_id IN (" + eligibleList + ") THEN @pts ELSE 0 END " +
                    "WHERE attempt_id IN (" + allList + ")", conn))
                {
                    cmd.Parameters.AddWithValue("@pts", points);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }

                CustomMessageBox.Show(
                    "Na-save ang plus points.\n\n" +
                    "Plus: +" + points + " para sa " + eligibleIds.Count + " student na may permit.\n" +
                    "Hindi kasama: " + excludedIds.Count + " student (N/A).",
                    "Plus Points Saved", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show("Unable to save plus points.\n\n" + ex.Message,
                    "Database Error", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Error);
            }

            LoadGrades();
        }

        // =========================================================
        // VIEW ANSWERS
        // =========================================================

        private void OpenSelectedAnswers()
        {
            if (dgvGrades.CurrentRow == null)
            {
                CustomMessageBox.Show("Select a student result first.", "No Selection",
                    CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Information);
                return;
            }

            int attemptId = Convert.ToInt32(dgvGrades.CurrentRow.Cells["AttemptId"].Value);

            using (var form = new ProfessorStudentAnswersForm(attemptId))
            {
                form.ShowDialog(this);
            }

            LoadGrades();
        }

        // =========================================================
        // PLUS POINTS DIALOG (nested, so no extra file is needed)
        // =========================================================

        private class PlusPointsDialog : Form
        {
            private Guna2NumericUpDown numPoints;

            public int Points
            {
                get { return (int)numPoints.Value; }
            }

            public PlusPointsDialog(string assessmentTitle, int eligibleCount, int excludedCount)
            {
                Text = "Plus Points";
                StartPosition = FormStartPosition.CenterParent;
                FormBorderStyle = FormBorderStyle.FixedDialog;
                MaximizeBox = false;
                MinimizeBox = false;
                ShowInTaskbar = false;
                ClientSize = new Size(460, 360);
                BackColor = ClrPageBg;
                Font = new Font("Segoe UI", 9.5F);


                // ---------------- HEADER ----------------
                Panel header = new Panel();
                header.Location = new Point(0, 0);
                header.Size = new Size(460, 84);
                header.BackColor = ClrMaroon;

                Panel accent = new Panel();
                accent.Dock = DockStyle.Bottom;
                accent.Height = 3;
                accent.BackColor = ClrGold;
                header.Controls.Add(accent);

                Label lblTitle = new Label();
                lblTitle.Text = "Plus Points";
                lblTitle.Font = new Font("Segoe UI Semibold", 15F, FontStyle.Bold);
                lblTitle.ForeColor = Color.White;
                lblTitle.BackColor = Color.Transparent;
                lblTitle.AutoSize = true;
                lblTitle.Location = new Point(24, 16);
                header.Controls.Add(lblTitle);

                Label lblAssess = new Label();
                lblAssess.Text = assessmentTitle;
                lblAssess.Font = new Font("Segoe UI", 9F);
                lblAssess.ForeColor = ClrGoldSoft;
                lblAssess.BackColor = Color.Transparent;
                lblAssess.AutoSize = false;
                lblAssess.Size = new Size(412, 20);
                lblAssess.Location = new Point(26, 50);
                lblAssess.AutoEllipsis = true;
                header.Controls.Add(lblAssess);

                Controls.Add(header);

                // ---------------- BODY ----------------
                Label lblCap = new Label();
                lblCap.Text = "PLUS POINTS PER STUDENT";
                lblCap.Font = new Font("Segoe UI Semibold", 8F, FontStyle.Bold);
                lblCap.ForeColor = ClrMuted;
                lblCap.BackColor = Color.Transparent;
                lblCap.AutoSize = true;
                lblCap.Location = new Point(28, 104);
                Controls.Add(lblCap);

                numPoints = new Guna2NumericUpDown();
                numPoints.Location = new Point(28, 126);
                numPoints.Size = new Size(160, 42);
                numPoints.Minimum = 0;
                numPoints.Maximum = 100;
                numPoints.Value = 1;
                numPoints.FillColor = Color.White;
                numPoints.BorderColor = ClrBorder;
                numPoints.BorderThickness = 1;
                numPoints.BorderRadius = 12;
                numPoints.ForeColor = ClrText;
                numPoints.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold);
                numPoints.UpDownButtonFillColor = Color.White;
                numPoints.UpDownButtonForeColor = ClrMuted;
                Controls.Add(numPoints);

                Label lblEligible = new Label();
                lblEligible.Text = "✔  " + eligibleCount + " student na may permit ang makakatanggap";
                lblEligible.Font = new Font("Segoe UI", 9.5F);
                lblEligible.ForeColor = Color.FromArgb(22, 110, 55);
                lblEligible.BackColor = Color.Transparent;
                lblEligible.AutoSize = true;
                lblEligible.Location = new Point(28, 196);
                Controls.Add(lblEligible);

                Label lblExcluded = new Label();
                lblExcluded.Text = "✖  " + excludedCount + " student na N/A ang hindi kasali";
                lblExcluded.Font = new Font("Segoe UI", 9.5F);
                lblExcluded.ForeColor = ClrMuted;
                lblExcluded.BackColor = Color.Transparent;
                lblExcluded.AutoSize = true;
                lblExcluded.Location = new Point(28, 222);
                Controls.Add(lblExcluded);

                // ---------------- FOOTER ----------------
                Guna2Button btnCancel = new Guna2Button();
                btnCancel.Text = "CANCEL";
                btnCancel.Location = new Point(220, 296);
                btnCancel.Size = new Size(104, 38);
                btnCancel.BorderRadius = 19;
                btnCancel.FillColor = Color.White;
                btnCancel.ForeColor = ClrText;
                btnCancel.BorderThickness = 1;
                btnCancel.BorderColor = ClrBorder;
                btnCancel.HoverState.FillColor = ClrHoverLight;
                btnCancel.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
                btnCancel.Cursor = Cursors.Hand;
                btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
                Controls.Add(btnCancel);

                Guna2Button btnSave = new Guna2Button();
                btnSave.Text = "SAVE";
                btnSave.Location = new Point(332, 296);
                btnSave.Size = new Size(104, 38);
                btnSave.BorderRadius = 19;
                btnSave.FillColor = ClrMaroon;
                btnSave.ForeColor = Color.White;
                btnSave.HoverState.FillColor = ClrMaroonDark;
                btnSave.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
                btnSave.Cursor = Cursors.Hand;
                btnSave.Click += (s, e) => { DialogResult = DialogResult.OK; Close(); };
                Controls.Add(btnSave);

                AcceptButton = btnSave;
                CancelButton = btnCancel;
            }
        }
    }
}