using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace WinFormsApp1
{
    public class ProfessorGradesForm : Form
    {
        private ComboBox cmbAssessment;
        private TextBox txtSearch;
        private Button btnRefresh;
        private Button btnViewAnswers;
        private DataGridView dgvGrades;

        private DataTable gradesTable = new DataTable();

        public ProfessorGradesForm()
        {
            BuildInterface();
            LoadAssessments();
            LoadGrades();
        }

        private void BuildInterface()
        {
            this.Text = "Student Grades";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(1150, 700);
            this.MinimumSize = new Size(950, 600);
            this.BackColor = Color.White;

            // =========================
            // TITLE
            // =========================

            Label lblTitle = new Label();

            lblTitle.Text = "STUDENT GRADES";
            lblTitle.Font =
                new Font("Segoe UI", 20, FontStyle.Bold);

            lblTitle.AutoSize = true;
            lblTitle.Location =
                new System.Drawing.Point(25, 20);

            // =========================
            // SUBTITLE
            // =========================

            Label lblSubtitle = new Label();

            lblSubtitle.Text =
                "View the results of students who completed quizzes and exams.";

            lblSubtitle.Font =
                new Font("Segoe UI", 10);

            lblSubtitle.AutoSize = true;
            lblSubtitle.Location =
                new System.Drawing.Point(28, 58);

            // =========================
            // ASSESSMENT LABEL
            // =========================

            Label lblAssessment = new Label();

            lblAssessment.Text = "Assessment:";
            lblAssessment.Font =
                new Font("Segoe UI", 9, FontStyle.Bold);

            lblAssessment.AutoSize = true;
            lblAssessment.Location =
                new System.Drawing.Point(28, 105);

            // =========================
            // ASSESSMENT COMBOBOX
            // =========================

            cmbAssessment = new ComboBox();

            cmbAssessment.DropDownStyle =
                ComboBoxStyle.DropDownList;

            cmbAssessment.Font =
                new Font("Segoe UI", 10);

            cmbAssessment.Width = 300;

            cmbAssessment.Location =
                new System.Drawing.Point(110, 100);

            cmbAssessment.SelectedIndexChanged +=
                (s, e) =>
                {
                    LoadGrades();
                };

            // =========================
            // SEARCH LABEL
            // =========================

            Label lblSearch = new Label();

            lblSearch.Text = "Search:";
            lblSearch.Font =
                new Font("Segoe UI", 9, FontStyle.Bold);

            lblSearch.AutoSize = true;

            lblSearch.Location =
                new System.Drawing.Point(430, 105);

            // =========================
            // SEARCH BOX
            // =========================

            txtSearch = new TextBox();

            txtSearch.Font =
                new Font("Segoe UI", 10);

            txtSearch.Width = 260;

            txtSearch.Location =
                new System.Drawing.Point(490, 100);

            txtSearch.TextChanged +=
                (s, e) =>
                {
                    ApplyFilter();
                };

            // =========================
            // REFRESH BUTTON
            // =========================

            btnRefresh = new Button();

            btnRefresh.Text = "Refresh";
            btnRefresh.Font =
                new Font("Segoe UI", 9, FontStyle.Bold);

            btnRefresh.Width = 100;
            btnRefresh.Height = 34;

            btnRefresh.Location =
                new System.Drawing.Point(770, 98);

            btnRefresh.Click +=
                (s, e) =>
                {
                    LoadAssessments();
                    LoadGrades();
                };

            // =========================
            // VIEW ANSWERS BUTTON
            // =========================

            btnViewAnswers = new Button();

            btnViewAnswers.Text = "View Answers";
            btnViewAnswers.Font =
                new Font("Segoe UI", 9, FontStyle.Bold);

            btnViewAnswers.Width = 130;
            btnViewAnswers.Height = 34;

            btnViewAnswers.Location =
                new System.Drawing.Point(880, 98);

            btnViewAnswers.Click +=
                BtnViewAnswers_Click;

            // =========================
            // DATAGRIDVIEW
            // =========================

            dgvGrades = new DataGridView();

            dgvGrades.Location =
                new System.Drawing.Point(25, 150);

            dgvGrades.Size =
                new Size(1080, 440);

            dgvGrades.Anchor =
                AnchorStyles.Top |
                AnchorStyles.Bottom |
                AnchorStyles.Left |
                AnchorStyles.Right;

            dgvGrades.BackgroundColor =
                Color.White;

            dgvGrades.BorderStyle =
                BorderStyle.None;

            dgvGrades.AllowUserToAddRows = false;
            dgvGrades.AllowUserToDeleteRows = false;
            dgvGrades.AllowUserToResizeRows = false;

            dgvGrades.ReadOnly = true;

            dgvGrades.MultiSelect = false;

            dgvGrades.SelectionMode =
                DataGridViewSelectionMode.FullRowSelect;

            dgvGrades.AutoGenerateColumns = false;

            dgvGrades.RowHeadersVisible = false;

            dgvGrades.AutoSizeRowsMode =
                DataGridViewAutoSizeRowsMode.AllCells;

            dgvGrades.ColumnHeadersDefaultCellStyle.Font =
                new Font("Segoe UI", 9, FontStyle.Bold);

            dgvGrades.DefaultCellStyle.Font =
                new Font("Segoe UI", 9);

            // Attempt ID
            dgvGrades.Columns.Add(
                new DataGridViewTextBoxColumn
                {
                    Name = "AttemptID",
                    HeaderText = "Attempt ID",
                    DataPropertyName = "attempt_id",
                    Visible = false
                });

            // Student ID
            dgvGrades.Columns.Add(
                new DataGridViewTextBoxColumn
                {
                    Name = "StudentID",
                    HeaderText = "Student ID",
                    DataPropertyName = "student_id",
                    Width = 120
                });

            // Student Name
            dgvGrades.Columns.Add(
                new DataGridViewTextBoxColumn
                {
                    Name = "StudentName",
                    HeaderText = "Student Name",
                    DataPropertyName = "full_name",
                    Width = 210
                });

            // Assessment
            dgvGrades.Columns.Add(
                new DataGridViewTextBoxColumn
                {
                    Name = "Assessment",
                    HeaderText = "Assessment",
                    DataPropertyName = "quiz_title",
                    Width = 220
                });

            // Score
            dgvGrades.Columns.Add(
                new DataGridViewTextBoxColumn
                {
                    Name = "Score",
                    HeaderText = "Score",
                    DataPropertyName = "score",
                    Width = 80
                });

            // Total
            dgvGrades.Columns.Add(
                new DataGridViewTextBoxColumn
                {
                    Name = "Total",
                    HeaderText = "Total",
                    DataPropertyName = "total_questions",
                    Width = 70
                });

            // Percentage
            dgvGrades.Columns.Add(
                new DataGridViewTextBoxColumn
                {
                    Name = "Percentage",
                    HeaderText = "Grade",
                    DataPropertyName = "percentage",
                    Width = 90
                });

            // Date
            dgvGrades.Columns.Add(
                new DataGridViewTextBoxColumn
                {
                    Name = "Submitted",
                    HeaderText = "Date Submitted",
                    DataPropertyName = "submitted_at",
                    Width = 160
                });

            // =========================
            // BOTTOM PANEL
            // =========================

            Panel bottomPanel = new Panel();

            bottomPanel.Dock =
                DockStyle.Bottom;

            bottomPanel.Height = 70;

            bottomPanel.BackColor =
                Color.FromArgb(245, 247, 250);

            Label lblInfo = new Label();

            lblInfo.Text =
                "Select a student and click View Answers to see the submitted answers.";

            lblInfo.Font =
                new Font("Segoe UI", 9);

            lblInfo.AutoSize = true;

            lblInfo.Location =
                new System.Drawing.Point(25, 25);

            bottomPanel.Controls.Add(lblInfo);

            // =========================
            // ADD CONTROLS
            // =========================

            this.Controls.Add(lblTitle);
            this.Controls.Add(lblSubtitle);
            this.Controls.Add(lblAssessment);
            this.Controls.Add(cmbAssessment);
            this.Controls.Add(lblSearch);
            this.Controls.Add(txtSearch);
            this.Controls.Add(btnRefresh);
            this.Controls.Add(btnViewAnswers);
            this.Controls.Add(dgvGrades);
            this.Controls.Add(bottomPanel);
        }

        // =========================================================
        // LOAD ASSESSMENTS
        // =========================================================

        private void LoadAssessments()
        {
            string connStr = SettingsManager.Current.GetConnectionString();
            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = @"
                        SELECT
                            quiz_id,
                            quiz_title,
                            assessment_type
                        FROM quizzes
                        ORDER BY created_at DESC";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            cmbAssessment.Items.Clear();

                            cmbAssessment.Items.Add(
                                new AssessmentItem
                                {
                                    QuizID = 0,
                                    Title = "All Assessments"
                                });

                            while (reader.Read())
                            {
                                int quizId =
                                    Convert.ToInt32(
                                        reader["quiz_id"]);

                                string title =
                                    reader["quiz_title"]
                                    ?.ToString() ?? "";

                                string type =
                                    reader["assessment_type"]
                                    ?.ToString() ?? "";

                                cmbAssessment.Items.Add(
                                    new AssessmentItem
                                    {
                                        QuizID = quizId,
                                        Title =
                                            title +
                                            " [" +
                                            type.ToUpper() +
                                            "]"
                                    });
                            }

                            if (cmbAssessment.Items.Count > 0)
                                cmbAssessment.SelectedIndex = 0;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Unable to load assessments.\n\n" +
                    ex.Message,
                    "Database Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        // =========================================================
        // LOAD GRADES
        // =========================================================

        private void LoadGrades()
        {
            string connStr = SettingsManager.Current.GetConnectionString();
            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = @"
                        SELECT
                            qa.attempt_id,
                            u.student_id,
                            u.full_name,
                            q.quiz_title,
                            qa.score,
                            qa.total_questions,
                            qa.percentage,
                            qa.submitted_at

                        FROM quiz_attempts qa

                        INNER JOIN user_credential u
                            ON qa.user_id = u.user_id

                        INNER JOIN quizzes q
                            ON qa.quiz_id = q.quiz_id

                        WHERE 1 = 1";

                    AssessmentItem selected =
                        cmbAssessment.SelectedItem
                        as AssessmentItem;

                    if (selected != null &&
                        selected.QuizID > 0)
                    {
                        query +=
                            " AND qa.quiz_id = @quizId";
                    }

                    query +=
                        " ORDER BY qa.submitted_at DESC";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        if (selected != null &&
                            selected.QuizID > 0)
                        {
                            cmd.Parameters.AddWithValue(
                                "@quizId",
                                selected.QuizID);
                        }

                        using (var adapter = new MySqlDataAdapter(cmd))
                        {
                            gradesTable = new DataTable();

                            adapter.Fill(gradesTable);

                            dgvGrades.DataSource = gradesTable;

                            ApplyFilter();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Unable to load student grades.\n\n" +
                    ex.Message,
                    "Database Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        // =========================================================
        // SEARCH FILTER
        // =========================================================

        private void ApplyFilter()
        {
            if (gradesTable == null)
                return;

            string search =
                txtSearch.Text.Trim()
                .Replace("'", "''");

            if (string.IsNullOrWhiteSpace(search))
            {
                gradesTable.DefaultView.RowFilter = "";
                return;
            }

            gradesTable.DefaultView.RowFilter =
                "student_id LIKE '%" + search + "%' " +
                "OR full_name LIKE '%" + search + "%' " +
                "OR quiz_title LIKE '%" + search + "%'";
        }

        // =========================================================
        // VIEW ANSWERS
        // =========================================================

        private void BtnViewAnswers_Click(
            object sender,
            EventArgs e)
        {
            if (dgvGrades.SelectedRows.Count == 0)
            {
                MessageBox.Show(
                    "Please select a student result first.",
                    "No Selection",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                return;
            }

            object value =
                dgvGrades.SelectedRows[0]
                .Cells["AttemptID"]
                .Value;

            if (value == null)
                return;

            int attemptId =
                Convert.ToInt32(value);

            using (ProfessorStudentAnswersForm form =
                   new ProfessorStudentAnswersForm(attemptId))
            {
                form.ShowDialog(this);
            }
        }

        // =========================================================
        // ASSESSMENT ITEM
        // =========================================================

        private class AssessmentItem
        {
            public int QuizID { get; set; }

            public string Title { get; set; } = "";

            public override string ToString()
            {
                return Title;
            }
        }
    }
}