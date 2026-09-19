using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace WinFormsApp1
{
    public class ProfessorStudentAnswersForm : Form
    {
        private int attemptId;

        private Label lblStudent;
        private Label lblAssessment;
        private Label lblScore;
        private DataGridView dgvAnswers;
        private Button btnClose;

        public ProfessorStudentAnswersForm(int attemptId)
        {
            this.attemptId = attemptId;

            BuildInterface();
            LoadStudentInformation();
            LoadAnswers();
        }

        private void BuildInterface()
        {
            this.Text = "Student Answers";
            this.StartPosition = FormStartPosition.CenterParent;
            this.Size = new Size(1100, 700);
            this.MinimumSize = new Size(900, 550);
            this.BackColor = Color.White;

            // =========================
            // HEADER
            // =========================

            Panel header = new Panel();
            header.Dock = DockStyle.Top;
            header.Height = 125;
            header.BackColor = Color.FromArgb(245, 247, 250);

            lblStudent = new Label();
            lblStudent.Text = "Student: Loading...";
            lblStudent.AutoSize = true;
            lblStudent.Font =
                new Font("Segoe UI", 12, FontStyle.Bold);
            lblStudent.Location =
                new System.Drawing.Point(25, 18);

            lblAssessment = new Label();
            lblAssessment.Text = "Assessment: Loading...";
            lblAssessment.AutoSize = true;
            lblAssessment.Font =
                new Font("Segoe UI", 10);
            lblAssessment.Location =
                new System.Drawing.Point(25, 50);

            lblScore = new Label();
            lblScore.Text = "Score: Loading...";
            lblScore.AutoSize = true;
            lblScore.Font =
                new Font("Segoe UI", 10, FontStyle.Bold);
            lblScore.Location =
                new System.Drawing.Point(25, 80);

            header.Controls.Add(lblStudent);
            header.Controls.Add(lblAssessment);
            header.Controls.Add(lblScore);

            // =========================
            // ANSWERS GRID
            // =========================

            dgvAnswers = new DataGridView();

            dgvAnswers.Dock = DockStyle.Fill;
            dgvAnswers.BackgroundColor = Color.White;
            dgvAnswers.BorderStyle = BorderStyle.None;

            dgvAnswers.AllowUserToAddRows = false;
            dgvAnswers.AllowUserToDeleteRows = false;
            dgvAnswers.AllowUserToResizeRows = false;

            dgvAnswers.ReadOnly = true;
            dgvAnswers.MultiSelect = false;

            dgvAnswers.SelectionMode =
                DataGridViewSelectionMode.FullRowSelect;

            dgvAnswers.RowHeadersVisible = false;
            dgvAnswers.AutoGenerateColumns = false;

            dgvAnswers.AutoSizeRowsMode =
                DataGridViewAutoSizeRowsMode.AllCells;

            dgvAnswers.ColumnHeadersDefaultCellStyle.Font =
                new Font("Segoe UI", 9, FontStyle.Bold);

            dgvAnswers.DefaultCellStyle.Font =
                new Font("Segoe UI", 9);

            // NUMBER
            dgvAnswers.Columns.Add(
                new DataGridViewTextBoxColumn
                {
                    Name = "Number",
                    HeaderText = "#",
                    DataPropertyName = "Number",
                    Width = 45
                });

            // TYPE
            dgvAnswers.Columns.Add(
                new DataGridViewTextBoxColumn
                {
                    Name = "QuestionType",
                    HeaderText = "Type",
                    DataPropertyName = "QuestionType",
                    Width = 120
                });

            // QUESTION
            DataGridViewTextBoxColumn questionColumn =
                new DataGridViewTextBoxColumn();

            questionColumn.Name = "Question";
            questionColumn.HeaderText = "Question";
            questionColumn.DataPropertyName = "Question";

            questionColumn.AutoSizeMode =
                DataGridViewAutoSizeColumnMode.Fill;

            dgvAnswers.Columns.Add(questionColumn);

            // STUDENT ANSWER
            dgvAnswers.Columns.Add(
                new DataGridViewTextBoxColumn
                {
                    Name = "StudentAnswer",
                    HeaderText = "Student Answer",
                    DataPropertyName = "StudentAnswer",
                    Width = 200
                });

            // CORRECT ANSWER
            dgvAnswers.Columns.Add(
                new DataGridViewTextBoxColumn
                {
                    Name = "CorrectAnswer",
                    HeaderText = "Correct Answer",
                    DataPropertyName = "CorrectAnswer",
                    Width = 160
                });

            // RESULT
            dgvAnswers.Columns.Add(
                new DataGridViewTextBoxColumn
                {
                    Name = "Result",
                    HeaderText = "Result",
                    DataPropertyName = "Result",
                    Width = 100
                });

            // =========================
            // BOTTOM
            // =========================

            Panel bottom = new Panel();

            bottom.Dock = DockStyle.Bottom;
            bottom.Height = 65;
            bottom.BackColor =
                Color.FromArgb(245, 247, 250);

            btnClose = new Button();

            btnClose.Text = "Close";
            btnClose.Font =
                new Font("Segoe UI", 9, FontStyle.Bold);

            btnClose.Width = 100;
            btnClose.Height = 35;

            btnClose.Location =
                new System.Drawing.Point(970, 15);

            btnClose.Anchor =
                AnchorStyles.Top |
                AnchorStyles.Right;

            btnClose.Click +=
                (s, e) =>
                {
                    this.Close();
                };

            bottom.Controls.Add(btnClose);

            // =========================
            // ADD CONTROLS
            // =========================

            this.Controls.Add(dgvAnswers);
            this.Controls.Add(bottom);
            this.Controls.Add(header);
        }

        // =========================================================
        // STUDENT INFORMATION
        // =========================================================

        private void LoadStudentInformation()
        {
            string connStr = SettingsManager.Current.GetConnectionString();
            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = @"
                        SELECT
                            u.full_name,
                            u.student_id,
                            q.quiz_title,
                            q.assessment_type,
                            qa.score,
                            qa.total_questions,
                            qa.percentage
                        FROM quiz_attempts qa
                        INNER JOIN user_credential u
                            ON qa.user_id = u.user_id
                        INNER JOIN quizzes q
                            ON qa.quiz_id = q.quiz_id
                        WHERE qa.attempt_id = @attemptId
                        LIMIT 1";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue(
                            "@attemptId",
                            attemptId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (!reader.Read())
                                return;

                            string studentName =
                                reader["full_name"]
                                ?.ToString() ?? "";

                            string studentId =
                                reader["student_id"]
                                ?.ToString() ?? "";

                            string title =
                                reader["quiz_title"]
                                ?.ToString() ?? "";

                            string type =
                                reader["assessment_type"]
                                ?.ToString() ?? "";

                            string score =
                                reader["score"]
                                ?.ToString() ?? "0";

                            string total =
                                reader["total_questions"]
                                ?.ToString() ?? "0";

                            string percentage =
                                reader["percentage"]
                                ?.ToString() ?? "0";

                            lblStudent.Text =
                                "Student: " +
                                studentName;

                            if (!string.IsNullOrWhiteSpace(studentId))
                            {
                                lblStudent.Text +=
                                    " (" +
                                    studentId +
                                    ")";
                            }

                            lblAssessment.Text =
                                "Assessment: " +
                                title +
                                " [" +
                                type.ToUpper() +
                                "]";

                            lblScore.Text =
                                "Score: " +
                                score +
                                " / " +
                                total +
                                "    |    Grade: " +
                                percentage +
                                "%";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Unable to load student information.\n\n" +
                    ex.Message,
                    "Database Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        // =========================================================
        // LOAD ANSWERS
        // =========================================================

        private void LoadAnswers()
        {
            string connStr = SettingsManager.Current.GetConnectionString();
            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();
                    string query = @"
                        SELECT
                            q.question_id,
                            q.question_type,
                            q.question_text,
                            q.correct_answer,
                            sa.student_answer,
                            sa.is_correct
                        FROM student_answers sa
                        INNER JOIN questions q
                            ON sa.question_id = q.question_id
                        WHERE sa.attempt_id = @attemptId
                        ORDER BY q.question_id";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue(
                            "@attemptId",
                            attemptId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            DataTable table =
                                new DataTable();

                            table.Columns.Add("Number");
                            table.Columns.Add("QuestionType");
                            table.Columns.Add("Question");
                            table.Columns.Add("StudentAnswer");
                            table.Columns.Add("CorrectAnswer");
                            table.Columns.Add("Result");

                            int number = 1;

                            while (reader.Read())
                            {
                                string type =
                                    reader["question_type"]
                                    ?.ToString() ?? "";

                                string question =
                                    reader["question_text"]
                                    ?.ToString() ?? "";

                                string studentAnswer =
                                    reader["student_answer"]
                                    ?.ToString() ?? "";

                                string correctAnswer =
                                    reader["correct_answer"]
                                    ?.ToString() ?? "";

                                bool correct =
                                    Convert.ToBoolean(
                                        reader["is_correct"]);

                                string result;

                                if (type == "essay")
                                {
                                    result = "Essay";
                                }
                                else if (correct)
                                {
                                    result = "Correct";
                                }
                                else
                                {
                                    result = "Incorrect";
                                }

                                table.Rows.Add(
                                    number.ToString(),
                                    FormatQuestionType(type),
                                    question,
                                    studentAnswer,
                                    correctAnswer,
                                    result);

                                number++;
                            }

                            dgvAnswers.DataSource = table;
                        }
                    }
                }

                FormatResults();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Unable to load student answers.\n\n" +
                    ex.Message,
                    "Database Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        // =========================================================
        // QUESTION TYPE
        // =========================================================

        private string FormatQuestionType(string type)
        {
            if (type == "multiple_choice")
                return "Multiple Choice";

            if (type == "true_false")
                return "True / False";

            if (type == "essay")
                return "Essay";

            return type;
        }

        // =========================================================
        // RESULT FORMATTING
        // =========================================================

        private void FormatResults()
        {
            foreach (DataGridViewRow row
                in dgvAnswers.Rows)
            {
                object value =
                    row.Cells["Result"].Value;

                if (value == null)
                    continue;

                string result =
                    value.ToString();

                if (result == "Correct" ||
                    result == "Incorrect" ||
                    result == "Essay")
                {
                    row.Cells["Result"].Style.Font =
                        new Font(
                            dgvAnswers.Font,
                            FontStyle.Bold);
                }
            }
        }
    }
}