using System;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace WinFormsApp1
{
    public class ProfessorStudentAnswersForm : Form
    {
        private readonly int attemptId;
        private int quizId = 0;
        private int pendingEssayCount = 0;

        // Palette (same as the rest of the app)
        private static readonly Color ClrMaroon = Color.FromArgb(94, 14, 33);
        private static readonly Color ClrMaroonDark = Color.FromArgb(70, 10, 24);
        private static readonly Color ClrGold = Color.FromArgb(198, 156, 53);
        private static readonly Color ClrGoldSoft = Color.FromArgb(230, 210, 180);
        private static readonly Color ClrPageBg = Color.FromArgb(245, 247, 250);
        private static readonly Color ClrCardBorder = Color.FromArgb(225, 228, 235);
        private static readonly Color ClrMuted = Color.FromArgb(120, 125, 135);
        private static readonly Color ClrText = Color.FromArgb(35, 35, 35);
        private static readonly Color ClrDark = Color.FromArgb(40, 40, 40);
        private static readonly Color ClrHeaderBg = Color.FromArgb(248, 249, 251);
        private static readonly Color ClrGridLine = Color.FromArgb(234, 236, 241);

        private Panel header;
        private Label lblStudent;
        private Label lblAssessment;
        private Label lblScore;
        private Label lblGrade;

        private Panel gridCard;
        private Panel summaryRow;
        private DataGridView dgv;

        private Panel footer;
        private Panel detailCard;
        private Label lblDetailCaption;
        private TextBox txtDetail;
        private Button btnGradeEssay;
        private Button btnClose;

        private Font boldFont;

        private DataTable table = new DataTable();

        public ProfessorStudentAnswersForm(int attemptId)
        {
            this.attemptId = attemptId;

            BuildInterface();
            LoadHeader();
            LoadAnswers();
        }

        // =========================================================
        // BUILD INTERFACE
        // =========================================================

        private void BuildInterface()
        {
            Text = "Student Answers";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(1100, 720);
            MinimumSize = new Size(900, 600);
            BackColor = ClrPageBg;
            Font = new Font("Segoe UI", 9.5F);

            boldFont = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);

            // ---------------- GRID AREA (Fill, added first) ----------------
            Panel gridHost = new Panel();
            gridHost.Dock = DockStyle.Fill;
            gridHost.BackColor = ClrPageBg;
            gridHost.Padding = new Padding(24, 14, 24, 14);

            gridCard = new Panel();
            gridCard.Dock = DockStyle.Fill;
            gridCard.BackColor = ClrPageBg;
            gridCard.Padding = new Padding(14);
            gridCard.Paint += Card_Paint;
            gridCard.Resize += (s, e) => gridCard.Invalidate();

            dgv = new DataGridView();
            dgv.Dock = DockStyle.Fill;
            dgv.ReadOnly = true;
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.AllowUserToResizeRows = false;
            dgv.AllowUserToResizeColumns = false;
            dgv.RowHeadersVisible = false;
            dgv.MultiSelect = false;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.AutoGenerateColumns = false;
            dgv.BackgroundColor = Color.White;
            dgv.BorderStyle = BorderStyle.None;
            dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgv.GridColor = ClrGridLine;
            dgv.EnableHeadersVisualStyles = false;
            dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgv.ColumnHeadersHeight = 40;
            dgv.RowTemplate.Height = 38;
            dgv.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;

            dgv.ColumnHeadersDefaultCellStyle.BackColor = ClrHeaderBg;
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = ClrDark;
            dgv.ColumnHeadersDefaultCellStyle.Font = boldFont;
            dgv.ColumnHeadersDefaultCellStyle.Padding = new Padding(10, 0, 0, 0);
            dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = ClrHeaderBg;
            dgv.ColumnHeadersDefaultCellStyle.SelectionForeColor = ClrDark;

            dgv.DefaultCellStyle.Font = new Font("Segoe UI", 9.5F);
            dgv.DefaultCellStyle.ForeColor = ClrText;
            dgv.DefaultCellStyle.BackColor = Color.White;
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(252, 242, 226);
            dgv.DefaultCellStyle.SelectionForeColor = ClrText;
            dgv.DefaultCellStyle.Padding = new Padding(10, 0, 8, 0);

            dgv.CellFormatting += Dgv_CellFormatting;
            dgv.SelectionChanged += (s, e) => ShowSelectedDetail();

            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "AnswerId", DataPropertyName = "AnswerId", Visible = false });
            dgv.Columns.Add(new DataGridViewTextBoxColumn { Name = "Pending", DataPropertyName = "Pending", Visible = false });
            dgv.Columns.Add(MakeColumn("No", "#", "No", 50));
            dgv.Columns.Add(MakeColumn("Type", "Type", "Type", 140));
            dgv.Columns.Add(MakeColumn("Question", "Question", "Question", 380));
            dgv.Columns.Add(MakeColumn("StudentAnswer", "Student Answer", "StudentAnswer", 210));
            dgv.Columns.Add(MakeColumn("CorrectAnswer", "Correct Answer", "CorrectAnswer", 160));
            dgv.Columns.Add(MakeColumn("Points", "Points", "Points", 90));
            dgv.Columns.Add(MakeColumn("Result", "Result", "Result", 110));

            dgv.Columns["Question"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dgv.Columns["No"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgv.Columns["Points"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgv.Columns["Result"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            gridCard.Controls.Add(dgv);

            // Summary chips row (Top, added second)
            summaryRow = new Panel();
            summaryRow.Dock = DockStyle.Top;
            summaryRow.Height = 46;
            summaryRow.BackColor = ClrPageBg;

            gridHost.Controls.Add(gridCard);
            gridHost.Controls.Add(summaryRow);

            // ---------------- FOOTER ----------------
            footer = new Panel();
            footer.Dock = DockStyle.Bottom;
            footer.Height = 170;
            footer.BackColor = ClrPageBg;
            footer.Padding = new Padding(24, 14, 24, 22);

            detailCard = new Panel();
            detailCard.Dock = DockStyle.Fill;
            detailCard.BackColor = ClrPageBg;
            detailCard.Padding = new Padding(16, 12, 16, 14);
            detailCard.Paint += Card_Paint;
            detailCard.Resize += (s, e) => detailCard.Invalidate();

            txtDetail = new TextBox();
            txtDetail.Multiline = true;
            txtDetail.ReadOnly = true;
            txtDetail.ScrollBars = ScrollBars.Vertical;
            txtDetail.BorderStyle = BorderStyle.None;
            txtDetail.BackColor = Color.White;
            txtDetail.ForeColor = ClrText;
            txtDetail.Font = new Font("Segoe UI", 10F);
            txtDetail.Dock = DockStyle.Fill;

            lblDetailCaption = new Label();
            lblDetailCaption.Text = "SELECTED ANSWER";
            lblDetailCaption.Font = new Font("Segoe UI Semibold", 8F, FontStyle.Bold);
            lblDetailCaption.ForeColor = ClrMuted;
            lblDetailCaption.BackColor = Color.Transparent;
            lblDetailCaption.Dock = DockStyle.Top;
            lblDetailCaption.Height = 22;

            detailCard.Controls.Add(txtDetail);
            detailCard.Controls.Add(lblDetailCaption);

            Panel right = new Panel();
            right.Dock = DockStyle.Right;
            right.Width = 200;
            right.BackColor = ClrPageBg;

            btnGradeEssay = new Button();
            btnGradeEssay.Text = "GRADE ESSAY";
            btnGradeEssay.Size = new Size(180, 46);
            btnGradeEssay.Location = new Point(16, 18);
            StyleButton(btnGradeEssay, ClrMaroon, ClrMaroonDark, Color.White);
            btnGradeEssay.Click += BtnGradeEssay_Click;

            btnClose = new Button();
            btnClose.Text = "CLOSE";
            btnClose.Size = new Size(180, 46);
            btnClose.Location = new Point(16, 74);
            StyleButton(btnClose, ClrDark, Color.FromArgb(70, 70, 70), Color.White);
            btnClose.Click += (s, e) => Close();

            right.Controls.Add(btnGradeEssay);
            right.Controls.Add(btnClose);

            footer.Controls.Add(detailCard);
            footer.Controls.Add(right);

            // ---------------- HEADER ----------------
            header = new Panel();
            header.Dock = DockStyle.Top;
            header.Height = 122;
            header.BackColor = ClrMaroon;

            Panel accent = new Panel();
            accent.Dock = DockStyle.Bottom;
            accent.Height = 3;
            accent.BackColor = ClrGold;
            header.Controls.Add(accent);

            lblStudent = new Label();
            lblStudent.Text = "Loading...";
            lblStudent.Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold);
            lblStudent.ForeColor = Color.White;
            lblStudent.BackColor = Color.Transparent;
            lblStudent.AutoSize = true;
            lblStudent.Location = new Point(28, 16);
            header.Controls.Add(lblStudent);

            lblAssessment = new Label();
            lblAssessment.Text = "";
            lblAssessment.Font = new Font("Segoe UI", 10F);
            lblAssessment.ForeColor = ClrGoldSoft;
            lblAssessment.BackColor = Color.Transparent;
            lblAssessment.AutoSize = true;
            lblAssessment.Location = new Point(28, 54);
            header.Controls.Add(lblAssessment);

            lblScore = new Label();
            lblScore.Font = boldFont;
            lblScore.ForeColor = Color.White;
            lblScore.BackColor = ClrMaroonDark;
            lblScore.TextAlign = ContentAlignment.MiddleCenter;
            lblScore.AutoSize = false;
            lblScore.Location = new Point(28, 84);
            lblScore.Size = new Size(120, 28);
            header.Controls.Add(lblScore);

            lblGrade = new Label();
            lblGrade.Font = boldFont;
            lblGrade.ForeColor = ClrMaroonDark;
            lblGrade.BackColor = ClrGold;
            lblGrade.TextAlign = ContentAlignment.MiddleCenter;
            lblGrade.AutoSize = false;
            lblGrade.Location = new Point(158, 84);
            lblGrade.Size = new Size(120, 28);
            header.Controls.Add(lblGrade);

            // Added last = docked first (Top), then Bottom, then Fill
            Controls.Add(gridHost);
            Controls.Add(footer);
            Controls.Add(header);
        }

        // =========================================================
        // UI HELPERS
        // =========================================================

        private DataGridViewTextBoxColumn MakeColumn(string name, string headerText, string prop, int width)
        {
            return new DataGridViewTextBoxColumn
            {
                Name = name,
                HeaderText = headerText,
                DataPropertyName = prop,
                Width = width
            };
        }

        private void Card_Paint(object sender, PaintEventArgs e)
        {
            Panel p = sender as Panel;
            if (p == null) return;

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle rect = new Rectangle(0, 0, p.Width - 1, p.Height - 1);

            using (GraphicsPath path = RoundedPath(rect, 14))
            using (SolidBrush fill = new SolidBrush(Color.White))
            using (Pen pen = new Pen(ClrCardBorder, 1f))
            {
                e.Graphics.FillPath(fill, path);
                e.Graphics.DrawPath(pen, path);
            }
        }

        private static GraphicsPath RoundedPath(Rectangle r, int radius)
        {
            int d = radius * 2;
            GraphicsPath path = new GraphicsPath();

            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();

            return path;
        }

        private void StyleButton(Button b, Color back, Color hover, Color fore)
        {
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = hover;
            b.BackColor = back;
            b.ForeColor = fore;
            b.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            b.Cursor = Cursors.Hand;
        }

        private void SizePill(Label l)
        {
            Size sz = TextRenderer.MeasureText(l.Text, l.Font);
            l.Size = new Size(sz.Width + 28, 28);
        }

        private int AddChip(int x, string text, Color back, Color fore)
        {
            Label chip = new Label();
            chip.Text = text;
            chip.Font = boldFont;
            chip.BackColor = back;
            chip.ForeColor = fore;
            chip.TextAlign = ContentAlignment.MiddleCenter;
            chip.AutoSize = false;

            Size sz = TextRenderer.MeasureText(text, chip.Font);
            chip.Size = new Size(sz.Width + 28, 28);
            chip.Location = new Point(x, 9);

            summaryRow.Controls.Add(chip);
            return chip.Right + 10;
        }

        private void RenderSummary(int correct, int incorrect, int gradedEssay, int pendingEssay, int noAnswer)
        {
            summaryRow.Controls.Clear();

            int x = 0;
            x = AddChip(x, "Correct  " + correct,
                Color.FromArgb(220, 243, 228), Color.FromArgb(22, 110, 55));
            x = AddChip(x, "Incorrect  " + incorrect,
                Color.FromArgb(252, 228, 228), Color.FromArgb(170, 30, 30));

            if (gradedEssay > 0)
                x = AddChip(x, "Essay graded  " + gradedEssay,
                    Color.FromArgb(226, 236, 252), Color.FromArgb(30, 80, 160));

            if (pendingEssay > 0)
                x = AddChip(x, "Essay pending  " + pendingEssay,
                    Color.FromArgb(255, 236, 205), Color.FromArgb(160, 80, 0));

            if (noAnswer > 0)
                x = AddChip(x, "No answer  " + noAnswer,
                    Color.FromArgb(236, 236, 240), ClrMuted);
        }

        private static void ResultColors(string result, out Color back, out Color fore)
        {
            switch (result)
            {
                case "Correct":
                    back = Color.FromArgb(240, 250, 243);
                    fore = Color.FromArgb(22, 110, 55);
                    return;

                case "Incorrect":
                    back = Color.FromArgb(253, 242, 242);
                    fore = Color.FromArgb(170, 30, 30);
                    return;

                case "Pending":
                    back = Color.FromArgb(255, 248, 234);
                    fore = Color.FromArgb(160, 80, 0);
                    return;

                case "Graded":
                    back = Color.FromArgb(242, 246, 254);
                    fore = Color.FromArgb(30, 80, 160);
                    return;

                default:
                    back = Color.White;
                    fore = Color.FromArgb(120, 125, 135);
                    return;
            }
        }

        private void Dgv_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;

            object v = dgv.Rows[e.RowIndex].Cells["Result"].Value;
            string result = v == null ? "" : v.ToString();

            Color back, fore;
            ResultColors(result, out back, out fore);

            e.CellStyle.BackColor = back;

            if (dgv.Columns[e.ColumnIndex].Name == "Result")
            {
                e.CellStyle.ForeColor = fore;
                e.CellStyle.Font = boldFont;
            }
        }

        // =========================================================
        // HEADER: STUDENT, ASSESSMENT, SCORE, GRADE
        // =========================================================

        private void LoadHeader()
        {
            try
            {
                string connStr = SettingsManager.Current.GetConnectionString();

                using (var conn = new MySqlConnection(connStr))
                using (var cmd = new MySqlCommand(@"
                    SELECT qa.quiz_id, qa.student_name, qa.score, qa.total_questions,
                           qa.percentage, qz.quiz_title, qz.assessment_type
                    FROM quiz_attempts qa
                    INNER JOIN quizzes qz ON qz.quiz_id = qa.quiz_id
                    WHERE qa.attempt_id = @id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", attemptId);
                    conn.Open();

                    using (var r = cmd.ExecuteReader())
                    {
                        if (!r.Read())
                        {
                            lblStudent.Text = "Attempt not found.";
                            return;
                        }

                        quizId = Convert.ToInt32(r["quiz_id"]);

                        string student = r["student_name"] == DBNull.Value
                            ? "Unknown" : r["student_name"].ToString().Replace("_", " ");

                        string title = r["quiz_title"] == DBNull.Value ? "" : r["quiz_title"].ToString();
                        string type = r["assessment_type"] == DBNull.Value ? "quiz" : r["assessment_type"].ToString();

                        int score = Convert.ToInt32(r["score"]);
                        int total = Convert.ToInt32(r["total_questions"]);
                        decimal pct = Convert.ToDecimal(r["percentage"]);

                        lblStudent.Text = student;
                        lblAssessment.Text = title + "   [" + type.ToUpper() + "]";

                        lblScore.Text = "Score  " + score + " / " + total;
                        lblGrade.Text = "Grade  " + pct.ToString("0.00") + "%";

                        // Auto-size the pills to their text
                        Size s1 = TextRenderer.MeasureText(lblScore.Text, boldFont);
                        lblScore.Size = new Size(s1.Width + 28, 28);

                        Size s2 = TextRenderer.MeasureText(lblGrade.Text, boldFont);
                        lblGrade.Size = new Size(s2.Width + 28, 28);
                        lblGrade.Location = new Point(lblScore.Right + 10, lblScore.Top);
                    }
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show("Unable to load the attempt.\n\n" + ex.Message,
                    "Database Error", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Error);
            }
        }

        // =========================================================
        // ANSWERS GRID
        // =========================================================

        private void LoadAnswers()
        {
            table = new DataTable();
            table.Columns.Add("AnswerId", typeof(int));
            table.Columns.Add("No", typeof(int));
            table.Columns.Add("Type", typeof(string));
            table.Columns.Add("Question", typeof(string));
            table.Columns.Add("StudentAnswer", typeof(string));
            table.Columns.Add("CorrectAnswer", typeof(string));
            table.Columns.Add("Points", typeof(string));
            table.Columns.Add("Result", typeof(string));
            table.Columns.Add("Pending", typeof(int));

            try
            {
                string connStr = SettingsManager.Current.GetConnectionString();

                using (var conn = new MySqlConnection(connStr))
                using (var cmd = new MySqlCommand(@"
                    SELECT q.question_id, q.question_text, q.question_type,
                           q.correct_answer, q.points,
                           sa.answer_id, sa.student_answer, sa.is_correct,
                           sa.needs_grading, sa.points_earned
                    FROM questions q
                    LEFT JOIN student_answers sa
                           ON sa.question_id = q.question_id
                          AND sa.attempt_id = @id
                    WHERE q.quiz_id = @quiz
                    ORDER BY FIELD(q.question_type,
                                   'multiple_choice', 'true_false', 'identification', 'essay'),
                             q.question_id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", attemptId);
                    cmd.Parameters.AddWithValue("@quiz", quizId);
                    conn.Open();

                    int no = 0;
                    int correctCount = 0, incorrectCount = 0, gradedEssay = 0, pendingEssay = 0, noAnswer = 0;

                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            no++;

                            string type = r["question_type"] == DBNull.Value
                                ? "" : r["question_type"].ToString();

                            int maxPts = r["points"] == DBNull.Value ? 1 : Convert.ToInt32(r["points"]);
                            if (maxPts < 1) maxPts = 1;

                            bool answered = r["answer_id"] != DBNull.Value;
                            string studentAnswer = answered && r["student_answer"] != DBNull.Value
                                ? r["student_answer"].ToString() : "";
                            bool hasAnswer = !string.IsNullOrWhiteSpace(studentAnswer);

                            bool isEssay = type == "essay";
                            bool isCorrect = answered && r["is_correct"] != DBNull.Value &&
                                             Convert.ToInt32(r["is_correct"]) == 1;
                            bool needsGrading = isEssay && answered && hasAnswer &&
                                                r["needs_grading"] != DBNull.Value &&
                                                Convert.ToInt32(r["needs_grading"]) == 1;

                            string pointsText;
                            string result;

                            if (!hasAnswer)
                            {
                                pointsText = "0 / " + maxPts;
                                result = "No answer";
                                noAnswer++;
                            }
                            else if (isEssay)
                            {
                                if (needsGrading)
                                {
                                    pointsText = "- / " + maxPts;
                                    result = "Pending";
                                    pendingEssay++;
                                }
                                else
                                {
                                    decimal earned = r["points_earned"] == DBNull.Value
                                        ? 0 : Convert.ToDecimal(r["points_earned"]);
                                    pointsText = earned.ToString("0.##") + " / " + maxPts;
                                    result = "Graded";
                                    gradedEssay++;
                                }
                            }
                            else
                            {
                                int earned = isCorrect ? maxPts : 0;
                                pointsText = earned + " / " + maxPts;
                                result = isCorrect ? "Correct" : "Incorrect";

                                if (isCorrect) correctCount++;
                                else incorrectCount++;
                            }

                            table.Rows.Add(
                                answered ? Convert.ToInt32(r["answer_id"]) : 0,
                                no,
                                TypeLabel(type),
                                r["question_text"] == DBNull.Value ? "" : r["question_text"].ToString(),
                                hasAnswer ? studentAnswer : "",
                                r["correct_answer"] == DBNull.Value ? "" : r["correct_answer"].ToString(),
                                pointsText,
                                result,
                                needsGrading ? 1 : 0);
                        }
                    }

                    pendingEssayCount = pendingEssay;

                    RenderSummary(correctCount, incorrectCount, gradedEssay, pendingEssay, noAnswer);

                    dgv.DataSource = table;

                    if (dgv.Rows.Count > 0)
                        dgv.Rows[0].Selected = true;
                    else
                        txtDetail.Text = "";
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show("Unable to load the answers.\n\n" + ex.Message,
                    "Database Error", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Error);
            }
        }

        private string TypeLabel(string type)
        {
            switch (type)
            {
                case "multiple_choice": return "Multiple Choice";
                case "true_false": return "True / False";
                case "identification": return "Identification";
                case "essay": return "Essay";
                default: return type;
            }
        }

        // =========================================================
        // SELECTED ANSWER DETAIL
        // =========================================================

        private void ShowSelectedDetail()
        {
            if (txtDetail == null || lblDetailCaption == null || table == null) return;

            if (dgv.CurrentRow == null ||
                dgv.CurrentRow.Index < 0 ||
                dgv.CurrentRow.Index >= table.Rows.Count)
            {
                txtDetail.Text = "";
                lblDetailCaption.Text = "SELECTED ANSWER";
                return;
            }

            DataRow dr = table.Rows[dgv.CurrentRow.Index];
            string answer = dr["StudentAnswer"].ToString();

            lblDetailCaption.Text = "SELECTED ANSWER   -   QUESTION " + dr["No"];
            txtDetail.Text = string.IsNullOrWhiteSpace(answer) ? "(walang sagot)" : answer;
        }

        // =========================================================
        // GRADE ESSAY
        // =========================================================

        private void BtnGradeEssay_Click(object sender, EventArgs e)
        {
            if (pendingEssayCount == 0)
            {
                CustomMessageBox.Show(
                    "Walang essay na kailangang i-grade sa attempt na ito.\n\n" +
                    "Kung 'Graded' na ang lahat ng essay, tapos na ang grading.",
                    "Grade Essay",
                    CustomMessageBoxButtons.OK,
                    CustomMessageBoxIcon.Information);
                return;
            }

            using (var dlg = new GradeEssayForm(attemptId))
            {
                dlg.ShowDialog(this);
            }

            LoadHeader();
            LoadAnswers();
        }
    }
}