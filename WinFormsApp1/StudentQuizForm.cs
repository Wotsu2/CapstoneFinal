using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace WinFormsApp1
{
    public class StudentQuizForm : Form
    {
        // =========================================================
        // DATA
        // =========================================================

        private List<QuizQuestion> questions = new List<QuizQuestion>();
        private Dictionary<int, string> studentAnswers = new Dictionary<int, string>();

        private int selectedQuizId = 0;
        private int studentUserId = 0;
        private int score = 0;

        // =========================================================
        // QUIZ ATTEMPT
        // =========================================================

        private int currentAttemptId = 0;

        // =========================================================
        // EXAM COUNTDOWN
        // =========================================================

        private const int ExamDurationMinutes = 60;
        private const int ExamDurationSeconds = ExamDurationMinutes * 60;

        private int remainingSeconds = ExamDurationSeconds;

        private Label lblTimer;
        private System.Windows.Forms.Timer examTimer;
        private bool isSubmitting = false;

        private const int PersistEveryNSeconds = 10;

        // =========================================================
        // HEARTBEAT
        // =========================================================

        private System.Windows.Forms.Timer heartbeatTimer;

        // =========================================================
        // AUTO-SAVE
        // =========================================================

        private System.Windows.Forms.Timer autoSaveTimer;
        private bool isAutoSaving = false;

        // =========================================================
        // COLORS (CDSGA maroon + gold theme)
        // =========================================================

        private readonly Color BackgroundColor = Color.FromArgb(250, 247, 239);
        private readonly Color CardColor = Color.White;
        private readonly Color DarkColor = Color.FromArgb(15, 23, 42);
        private readonly Color TextColor = Color.FromArgb(51, 65, 85);
        private readonly Color MutedColor = Color.FromArgb(100, 116, 139);
        private readonly Color GreenColor = Color.FromArgb(22, 163, 74);
        private readonly Color BorderColor = Color.FromArgb(198, 156, 53);
        private readonly Color MaroonColor = Color.FromArgb(128, 45, 58);
        private readonly Color MaroonSoft = Color.FromArgb(250, 242, 216);
        private readonly Color OptionBackColor = Color.FromArgb(248, 250, 252);
        private readonly Color GoldColor = Color.FromArgb(198, 156, 53);
        private readonly Color GoldDark = Color.FromArgb(163, 126, 36);

        // =========================================================
        // DOUBLE BUFFERED PANELS
        // =========================================================

        private class SmoothPanel : Panel
        {
            public SmoothPanel()
            {
                this.SetStyle(
                    ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.UserPaint |
                    ControlStyles.OptimizedDoubleBuffer |
                    ControlStyles.ResizeRedraw,
                    true);
                this.DoubleBuffered = true;
            }
        }

        private class RoundedPanel : Panel
        {
            public RoundedPanel()
            {
                this.SetStyle(
                    ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.UserPaint |
                    ControlStyles.OptimizedDoubleBuffer |
                    ControlStyles.ResizeRedraw,
                    true);
                this.DoubleBuffered = true;
            }
        }

        // =========================================================
        // MAIN UI
        // =========================================================

        private Panel headerPanel;
        private Panel scrollPanel;
        private Panel contentPanel;

        private Label lblExamPeriod;
        private Label lblQuizTitle;
        private Label lblSubject;
        private Label lblInstruction;
        private Label lblQuestionCount;

        private ProgressBar progressBar;
        private Button btnSubmit;

        // =========================================================
        // QUESTION CONTROLS
        // =========================================================

        private List<Panel> questionCards = new List<Panel>();
        private List<Panel> sectionHeaders = new List<Panel>();
        private List<RadioButton[]> multipleChoiceControls = new List<RadioButton[]>();
        private List<RadioButton[]> trueFalseControls = new List<RadioButton[]>();
        private List<TextBox> identificationControls = new List<TextBox>();
        private List<TextBox> essayControls = new List<TextBox>();

        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public StudentQuizForm(int studentId, int quizId)
        {
            studentUserId = studentId;
            selectedQuizId = quizId;

            InitializeUI();
            LoadQuiz(quizId);
        }

        // =========================================================
        // INITIALIZE UI
        // =========================================================

        private void InitializeUI()
        {
            this.Text = "Student Examination System";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(1100, 800);
            this.MinimumSize = new Size(850, 650);
            this.BackColor = BackgroundColor;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.KeyPreview = true;

            headerPanel = new SmoothPanel();
            headerPanel.Dock = DockStyle.Top;
            headerPanel.Height = 145;
            headerPanel.BackColor = MaroonColor;
            this.Controls.Add(headerPanel);

            Panel headerAccent = new Panel();
            headerAccent.Dock = DockStyle.Bottom;
            headerAccent.Height = 4;
            headerAccent.BackColor = GoldColor;
            headerPanel.Controls.Add(headerAccent);

            lblExamPeriod = new Label();
            lblExamPeriod.Text = "CDSGA  •  EXAMINATION";
            lblExamPeriod.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            lblExamPeriod.ForeColor = GoldColor;
            lblExamPeriod.AutoSize = true;
            lblExamPeriod.Location = new Point(38, 10);
            headerPanel.Controls.Add(lblExamPeriod);

            lblQuizTitle = new Label();
            lblQuizTitle.Text = "Loading Quiz...";
            lblQuizTitle.Font = new Font("Segoe UI", 25, FontStyle.Bold);
            lblQuizTitle.ForeColor = Color.White;
            lblQuizTitle.AutoSize = true;
            lblQuizTitle.Location = new Point(35, 30);
            headerPanel.Controls.Add(lblQuizTitle);

            lblSubject = new Label();
            lblSubject.Text = "Preparing examination...";
            lblSubject.Font = new Font("Segoe UI", 12);
            lblSubject.ForeColor = Color.FromArgb(230, 210, 180);
            lblSubject.AutoSize = true;
            lblSubject.Location = new Point(38, 72);
            headerPanel.Controls.Add(lblSubject);

            lblInstruction = new Label();
            lblInstruction.Text = "Answer all questions carefully. Scroll down to continue.";
            lblInstruction.Font = new Font("Segoe UI", 10);
            lblInstruction.ForeColor = Color.FromArgb(210, 190, 165);
            lblInstruction.AutoSize = true;
            lblInstruction.Location = new Point(38, 101);
            headerPanel.Controls.Add(lblInstruction);

            lblTimer = new Label();
            lblTimer.Text = $"TIME: {ExamDurationMinutes:00}:00";
            lblTimer.Font = new Font("Segoe UI", 18, FontStyle.Bold);
            lblTimer.ForeColor = Color.White;
            lblTimer.BackColor = DarkColor;
            lblTimer.TextAlign = ContentAlignment.MiddleCenter;
            lblTimer.Size = new Size(180, 48);
            lblTimer.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblTimer.Location = new Point(this.ClientSize.Width - 215, 38);
            lblTimer.Paint += LblTimer_Paint;
            headerPanel.Controls.Add(lblTimer);
            lblTimer.BringToFront();

            scrollPanel = new SmoothPanel();
            scrollPanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom |
                                  AnchorStyles.Left | AnchorStyles.Right;
            scrollPanel.Location = new Point(0, headerPanel.Height);
            scrollPanel.Size = new Size(this.ClientSize.Width,
                Math.Max(0, this.ClientSize.Height - headerPanel.Height));
            scrollPanel.AutoScroll = true;
            scrollPanel.BackColor = BackgroundColor;
            scrollPanel.Padding = new Padding(0, 25, 0, 40);
            this.Controls.Add(scrollPanel);
            headerPanel.BringToFront();

            contentPanel = new SmoothPanel();
            contentPanel.BackColor = BackgroundColor;
            contentPanel.Size = new Size(900, 500);
            contentPanel.Location = new Point(50, 25);
            scrollPanel.Controls.Add(contentPanel);

            lblQuestionCount = new Label();
            lblQuestionCount.Text = "0 Questions";
            lblQuestionCount.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            lblQuestionCount.ForeColor = DarkColor;
            lblQuestionCount.AutoSize = true;
            lblQuestionCount.Location = new Point(5, 0);
            contentPanel.Controls.Add(lblQuestionCount);

            progressBar = new ProgressBar();
            progressBar.Minimum = 0;
            progressBar.Maximum = 100;
            progressBar.Value = 0;
            progressBar.Location = new Point(5, 34);
            progressBar.Size = new Size(890, 12);
            contentPanel.Controls.Add(progressBar);

            btnSubmit = new Button();
            btnSubmit.Text = "✓  SUBMIT";
            btnSubmit.Size = new Size(240, 55);
            btnSubmit.Font = new Font("Segoe UI", 13, FontStyle.Bold);
            btnSubmit.BackColor = GreenColor;
            btnSubmit.ForeColor = Color.White;
            btnSubmit.FlatStyle = FlatStyle.Flat;
            btnSubmit.FlatAppearance.BorderSize = 2;
            btnSubmit.FlatAppearance.BorderColor = GoldColor;
            btnSubmit.Cursor = Cursors.Hand;
            btnSubmit.Visible = false;
            btnSubmit.Click += BtnSubmit_Click;
            contentPanel.Controls.Add(btnSubmit);

            this.Resize += StudentQuizForm_Resize;
            this.FormClosed += StudentQuizForm_FormClosed;

            StudentQuizForm_Resize(null, EventArgs.Empty);

            EnableAntiCheat();
        }

        // =========================================================
<<<<<<< Updated upstream
        // TIMER BADGE - GOLD OUTLINE
        // =========================================================

        private void LblTimer_Paint(object sender, PaintEventArgs e)
        {
            Label label = sender as Label;
            if (label == null) return;

            using (Pen goldPen = new Pen(GoldColor, 2f))
            {
                e.Graphics.DrawRectangle(
                    goldPen,
                    1,
                    1,
                    label.Width - 3,
                    label.Height - 3);
            }
        }

        // =========================================================
        // FORM CLOSED
=======
        // FORM CLOSED — final save + mark DISCONNECTED
>>>>>>> Stashed changes
        // =========================================================

        private void StudentQuizForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            try { StopExamTimer(); } catch { }
            try { StopAutoSave(); } catch { }
            try { StopHeartbeat(); } catch { }

            if (currentAttemptId <= 0) return;

            if (!isSubmitting)
            {
                try
                {
                    SaveAllAnswers();
                    SaveAnswersToDatabase();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("FormClosed save error: " + ex.Message);
                }

                try
                {
                    SaveRemainingSecondsToDb(remainingSeconds);
                    MarkAttemptAsDisconnected();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("FormClosed disconnect error: " + ex.Message);
                }
            }
        }

        private void MarkAttemptAsDisconnected()
        {
            if (currentAttemptId <= 0) return;

            try
            {
                string connStr = SettingsManager.Current.GetConnectionString();

                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    using (var cmd = new MySqlCommand(
                        @"UPDATE quiz_attempts
                          SET status = 'DISCONNECTED',
                              last_seen = NOW()
                          WHERE attempt_id = @id
                            AND status = 'TAKING'", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", currentAttemptId);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch { }
        }

        // =========================================================
        // RESIZE
        // =========================================================

        private void StudentQuizForm_Resize(object sender, EventArgs e)
        {
            if (scrollPanel == null || contentPanel == null) return;

            if (headerPanel != null)
            {
                scrollPanel.Location = new Point(0, headerPanel.Height);
                scrollPanel.Size = new Size(this.ClientSize.Width,
                    Math.Max(0, this.ClientSize.Height - headerPanel.Height));
            }

            if (lblTimer != null)
            {
                lblTimer.Location = new Point(
                    Math.Max(10, headerPanel.ClientSize.Width - lblTimer.Width - 30), 38);
            }

            int availableWidth = scrollPanel.ClientSize.Width;
            int contentWidth = Math.Max(780, Math.Min(900, availableWidth - 70));

            contentPanel.Width = contentWidth;
            contentPanel.Left = Math.Max(25, (availableWidth - contentPanel.Width) / 2);

            progressBar.Width = contentPanel.Width - 10;

            foreach (Panel header in sectionHeaders)
            {
                if (header == null) continue;
                header.Width = contentPanel.Width - 10;
                ResizeSectionHeader(header);
                header.Invalidate();
            }

            foreach (Panel card in questionCards)
            {
                if (card == null) continue;
                card.Width = contentPanel.Width - 10;
                ResizeQuestionCard(card);
                card.Invalidate();
            }

            if (btnSubmit != null)
            {
                btnSubmit.Left = (contentPanel.Width - btnSubmit.Width) / 2;
            }
        }

        // =========================================================
        // LOAD QUIZ
        // =========================================================

        private void LoadQuiz(int quizId)
        {
            string connStr = SettingsManager.Current.GetConnectionString();

            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    string query = @"
                        SELECT quiz_id, quiz_title, subject, exam_period, assessment_type
                        FROM quizzes
                        WHERE quiz_id = @quiz_id
                        LIMIT 1";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@quiz_id", quizId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                MessageBox.Show("This quiz/exam no longer exists.",
                                    "Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                DisableQuiz();
                                return;
                            }

                            selectedQuizId = Convert.ToInt32(reader["quiz_id"]);

                            string examPeriod = reader["exam_period"] == DBNull.Value
                                ? "PRELIM"
                                : reader["exam_period"].ToString().Trim().ToUpper();

                            lblExamPeriod.Text = GetExamPeriodDisplay(examPeriod);
                            lblQuizTitle.Text = reader["quiz_title"].ToString();
                            lblSubject.Text = "Subject: " + reader["subject"].ToString();
                            lblInstruction.Text = "Answer all questions carefully. Scroll down to continue.";
                        }
                    }

                    LoadQuestions();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to load the quiz.\n\n" + ex.Message,
                    "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                DisableQuiz();
            }
        }

        private string GetExamPeriodDisplay(string examPeriod)
        {
            switch (examPeriod)
            {
                case "PRELIM": return "CDSGA  •  PRELIM EXAMINATION";
                case "MIDTERM": return "CDSGA  •  MIDTERM EXAMINATION";
                case "SEMIFINALS": return "CDSGA  •  SEMIFINALS EXAMINATION";
                case "FINALS": return "CDSGA  •  FINAL EXAMINATION";
                default:
                    if (string.IsNullOrWhiteSpace(examPeriod)) return "CDSGA  •  EXAMINATION";
                    return "CDSGA  •  " + examPeriod.ToUpper() + " EXAMINATION";
            }
        }

        // =========================================================
        // LOAD QUESTIONS
        // =========================================================

        private void LoadQuestions()
        {
            questions.Clear();
            studentAnswers.Clear();
            questionCards.Clear();
            sectionHeaders.Clear();
            multipleChoiceControls.Clear();
            trueFalseControls.Clear();
            identificationControls.Clear();
            essayControls.Clear();
            contentPanel.Controls.Clear();

            contentPanel.Controls.Add(lblQuestionCount);
            contentPanel.Controls.Add(progressBar);

            string connStr = SettingsManager.Current.GetConnectionString();

            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    string query = @"
                        SELECT question_id, question_text, question_type,
                               choice_a, choice_b, choice_c, choice_d, correct_answer
                        FROM questions
                        WHERE quiz_id = @quiz_id
                        ORDER BY question_id ASC";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@quiz_id", selectedQuizId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                QuizQuestion question = new QuizQuestion();

                                question.QuestionId = Convert.ToInt32(reader["question_id"]);
                                question.Question = reader["question_text"].ToString();
                                question.QuestionType = reader["question_type"].ToString();
                                question.ChoiceA = reader["choice_a"] == DBNull.Value ? "" : reader["choice_a"].ToString();
                                question.ChoiceB = reader["choice_b"] == DBNull.Value ? "" : reader["choice_b"].ToString();
                                question.ChoiceC = reader["choice_c"] == DBNull.Value ? "" : reader["choice_c"].ToString();
                                question.ChoiceD = reader["choice_d"] == DBNull.Value ? "" : reader["choice_d"].ToString();
                                question.CorrectAnswer = reader["correct_answer"] == DBNull.Value ? "" : reader["correct_answer"].ToString();

                                string loadedType = NormalizeQuestionType(question.QuestionType);

                                bool noChoices =
                                    string.IsNullOrWhiteSpace(question.ChoiceA) &&
                                    string.IsNullOrWhiteSpace(question.ChoiceB) &&
                                    string.IsNullOrWhiteSpace(question.ChoiceC) &&
                                    string.IsNullOrWhiteSpace(question.ChoiceD);

                                bool hasCorrectAnswer = !string.IsNullOrWhiteSpace(question.CorrectAnswer);

                                if (loadedType == "multiple_choice" && noChoices && hasCorrectAnswer)
                                    question.QuestionType = "identification";
                                else
                                    question.QuestionType = loadedType;

                                questions.Add(question);
                            }
                        }
                    }
                }

                if (questions.Count == 0)
                {
                    MessageBox.Show("This quiz does not contain any questions.",
                        "No Questions", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    DisableQuiz();
                    return;
                }

                lblQuestionCount.Text = questions.Count + " Questions";
                progressBar.Value = 0;

                BuildAllQuestions();
                btnSubmit.Visible = true;
                StudentQuizForm_Resize(null, EventArgs.Empty);

                // Build UI first, then resume/create the attempt
                if (!CreateOrResumeAttempt())
                {
                    DisableQuiz();
                    return;
                }

                // Restore answers from previous session (if any)
                LoadSavedAnswers();

                if (!StartExamTimer())
                {
                    DisableQuiz();
                    return;
                }

                StartHeartbeat();
                StartAutoSave();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to load questions.\n\n" + ex.Message,
                    "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                DisableQuiz();
            }
        }

        // =========================================================
        // CREATE OR RESUME QUIZ ATTEMPT
        // =========================================================
        // If there's an existing TAKING or DISCONNECTED attempt for
        // this user+quiz, REUSE it so saved answers are preserved.
        // Otherwise create a fresh one.
        // =========================================================

        private bool CreateOrResumeAttempt()
        {
            string connStr = SettingsManager.Current.GetConnectionString();

            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    // 1. Look for a resumable attempt — newest first
                    string findQuery = @"
                        SELECT attempt_id, remaining_seconds
                        FROM quiz_attempts
                        WHERE quiz_id = @quiz_id
                          AND user_id = @user_id
                          AND status IN ('TAKING', 'DISCONNECTED')
                        ORDER BY attempt_id DESC
                        LIMIT 1";

                    using (var findCmd = new MySqlCommand(findQuery, conn))
                    {
                        findCmd.Parameters.AddWithValue("@quiz_id", selectedQuizId);
                        findCmd.Parameters.AddWithValue("@user_id", studentUserId);

                        using (var reader = findCmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                currentAttemptId = Convert.ToInt32(reader["attempt_id"]);

                                int savedSeconds = ExamDurationSeconds;

                                if (reader["remaining_seconds"] != DBNull.Value)
                                {
                                    savedSeconds = Convert.ToInt32(reader["remaining_seconds"]);
                                    if (savedSeconds <= 0) savedSeconds = ExamDurationSeconds;
                                }

                                if (savedSeconds > ExamDurationSeconds)
                                    savedSeconds = ExamDurationSeconds;

                                remainingSeconds = savedSeconds;

                                Console.WriteLine($"[QuizAttempt] RESUMING attempt {currentAttemptId} with {remainingSeconds} sec remaining");
                            }
                        }
                    }

                    // 2. Reactivate if found
                    if (currentAttemptId > 0)
                    {
                        using (var reactivateCmd = new MySqlCommand(
                            @"UPDATE quiz_attempts
                              SET status = 'TAKING', last_seen = NOW()
                              WHERE attempt_id = @id", conn))
                        {
                            reactivateCmd.Parameters.AddWithValue("@id", currentAttemptId);
                            reactivateCmd.ExecuteNonQuery();
                        }

                        return true;
                    }

                    // 3. Otherwise create a fresh attempt
                    string insertQuery = @"
                        INSERT INTO quiz_attempts
                        (quiz_id, user_id, score, total_questions, percentage,
                         status, started_at, last_seen, remaining_seconds)
                        VALUES
                        (@quiz_id, @user_id, 0, @total_questions, 0,
                         'TAKING', NOW(), NOW(), @remaining_seconds)";

                    using (var insertCmd = new MySqlCommand(insertQuery, conn))
                    {
                        insertCmd.Parameters.AddWithValue("@quiz_id", selectedQuizId);
                        insertCmd.Parameters.AddWithValue("@user_id", studentUserId);
                        insertCmd.Parameters.AddWithValue("@total_questions", questions.Count);
                        insertCmd.Parameters.AddWithValue("@remaining_seconds", ExamDurationSeconds);

                        insertCmd.ExecuteNonQuery();
                        currentAttemptId = Convert.ToInt32(insertCmd.LastInsertedId);
                    }

                    remainingSeconds = ExamDurationSeconds;

                    Console.WriteLine($"[QuizAttempt] CREATED new attempt {currentAttemptId}");

                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Unable to start or resume the examination.\n\n" + ex.Message,
                    "Unable to Start Examination",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);

                currentAttemptId = 0;
                return false;
            }
        }

        // =========================================================
        // LOAD SAVED ANSWERS
        // =========================================================

        private void LoadSavedAnswers()
        {
            if (currentAttemptId <= 0) return;

            string connStr = SettingsManager.Current.GetConnectionString();

            try
            {
                int restored = 0;

                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    string query = @"
                        SELECT question_id, student_answer
                        FROM student_answers
                        WHERE attempt_id = @attempt_id";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@attempt_id", currentAttemptId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                restored++;

                                int questionId = Convert.ToInt32(reader["question_id"]);
                                string answer = reader["student_answer"] == DBNull.Value
                                    ? ""
                                    : reader["student_answer"].ToString();

                                studentAnswers[questionId] = answer;
                            }
                        }
                    }
                }

                Console.WriteLine($"[LoadSavedAnswers] attempt {currentAttemptId} → {restored} answers");

                // Apply restored answers to UI
                for (int i = 0; i < questions.Count; i++)
                {
                    QuizQuestion question = questions[i];
                    int questionId = question.QuestionId;

                    if (!studentAnswers.ContainsKey(questionId)) continue;

                    string savedAnswer = studentAnswers[questionId];
                    if (string.IsNullOrWhiteSpace(savedAnswer)) continue;

                    string type = NormalizeQuestionType(question.QuestionType);

                    if (type == "multiple_choice")
                    {
                        if (i < multipleChoiceControls.Count && multipleChoiceControls[i] != null)
                        {
                            RadioButton[] radios = multipleChoiceControls[i];
                            for (int r = 0; r < radios.Length; r++)
                            {
                                if (radios[r] == null) continue;
                                string tagValue = radios[r].Tag == null ? "" : radios[r].Tag.ToString();
                                if (tagValue.Equals(savedAnswer.Trim(), StringComparison.OrdinalIgnoreCase))
                                {
                                    radios[r].Checked = true;
                                    break;
                                }
                            }
                        }
                    }
                    else if (type == "true_false")
                    {
                        if (i < trueFalseControls.Count && trueFalseControls[i] != null)
                        {
                            RadioButton[] radios = trueFalseControls[i];
                            for (int r = 0; r < radios.Length; r++)
                            {
                                if (radios[r] == null) continue;
                                string tagValue = radios[r].Tag == null ? "" : radios[r].Tag.ToString();
                                if (tagValue.Equals(savedAnswer.Trim(), StringComparison.OrdinalIgnoreCase))
                                {
                                    radios[r].Checked = true;
                                    break;
                                }
                            }
                        }
                    }
                    else if (type == "identification")
                    {
                        if (i < identificationControls.Count && identificationControls[i] != null)
                            identificationControls[i].Text = savedAnswer;
                    }
                    else if (type == "essay")
                    {
                        if (i < essayControls.Count && essayControls[i] != null)
                            essayControls[i].Text = savedAnswer;
                    }
                }

                UpdateProgress();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Saved answers could not be restored.\n\n" + ex.Message,
                    "Resume Examination",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // =========================================================
        // EXAM TIMER
        // =========================================================

        private bool StartExamTimer()
        {
            StopExamTimer();

            if (currentAttemptId <= 0) return false;

            if (remainingSeconds <= 0)
            {
                remainingSeconds = ExamDurationSeconds;
                SaveRemainingSecondsToDb(remainingSeconds);
            }

            if (remainingSeconds > ExamDurationSeconds)
                remainingSeconds = ExamDurationSeconds;

            UpdateTimerLabel();

            examTimer = new System.Windows.Forms.Timer();
            examTimer.Interval = 1000;
            examTimer.Tick += ExamTimer_Tick;
            examTimer.Start();

            return true;
        }

        private void ExamTimer_Tick(object sender, EventArgs e)
        {
            if (currentAttemptId <= 0) return;

            remainingSeconds--;
            if (remainingSeconds < 0) remainingSeconds = 0;

            UpdateTimerLabel();

            if (remainingSeconds % PersistEveryNSeconds == 0)
                SaveRemainingSecondsToDb(remainingSeconds);

            if (remainingSeconds <= 0)
            {
                SaveRemainingSecondsToDb(0);
                StopExamTimer();
                AutoSubmitWhenTimeExpires();
            }
        }

        private void UpdateTimerLabel()
        {
            if (lblTimer == null) return;

            int minutes = remainingSeconds / 60;
            int seconds = remainingSeconds % 60;

            lblTimer.Text = $"TIME: {minutes:00}:{seconds:00}";

            lblTimer.BackColor = remainingSeconds <= 300
<<<<<<< Updated upstream
                ? Color.FromArgb(185, 28, 28)   // red warning in last 5 minutes
                : DarkColor;
=======
                ? Color.FromArgb(185, 28, 28)
                : MaroonColor;
>>>>>>> Stashed changes
        }

        private void SaveRemainingSecondsToDb(int seconds)
        {
            if (currentAttemptId <= 0) return;

            try
            {
                string connStr = SettingsManager.Current.GetConnectionString();

                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    using (var cmd = new MySqlCommand(
                        @"UPDATE quiz_attempts
                          SET remaining_seconds = @s, last_seen = NOW()
                          WHERE attempt_id = @id AND status = 'TAKING'", conn))
                    {
                        cmd.Parameters.AddWithValue("@s", seconds);
                        cmd.Parameters.AddWithValue("@id", currentAttemptId);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch { }
        }

        private void StopExamTimer()
        {
            if (examTimer == null) return;

            try
            {
                examTimer.Stop();
                examTimer.Tick -= ExamTimer_Tick;
                examTimer.Dispose();
            }
            catch { }

            examTimer = null;
        }

        private void AutoSubmitWhenTimeExpires()
        {
            if (isSubmitting) return;
            if (currentAttemptId <= 0) return;

            isSubmitting = true;

            try
            {
                SaveAllAnswers();
                SaveQuizResult(true);
            }
            finally
            {
                isSubmitting = false;
            }
        }

        // =========================================================
        // HEARTBEAT
        // =========================================================

        private void StartHeartbeat()
        {
            StopHeartbeat();
            if (currentAttemptId <= 0) return;

            heartbeatTimer = new System.Windows.Forms.Timer();
            heartbeatTimer.Interval = 5000;
            heartbeatTimer.Tick += HeartbeatTimer_Tick;
            heartbeatTimer.Start();
        }

        private void HeartbeatTimer_Tick(object sender, EventArgs e)
        {
            if (currentAttemptId <= 0) return;

            string connStr = SettingsManager.Current.GetConnectionString();

            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    using (var cmd = new MySqlCommand(
                        @"UPDATE quiz_attempts
                          SET last_seen = NOW(), status = 'TAKING'
                          WHERE attempt_id = @attempt_id AND status = 'TAKING'", conn))
                    {
                        cmd.Parameters.AddWithValue("@attempt_id", currentAttemptId);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch { }
        }

        private void StopHeartbeat()
        {
            if (heartbeatTimer == null) return;

            try
            {
                heartbeatTimer.Stop();
                heartbeatTimer.Tick -= HeartbeatTimer_Tick;
                heartbeatTimer.Dispose();
            }
            catch { }

            heartbeatTimer = null;
        }

        // =========================================================
        // AUTO-SAVE
        // =========================================================

        private void StartAutoSave()
        {
            StopAutoSave();
            if (currentAttemptId <= 0) return;

            autoSaveTimer = new System.Windows.Forms.Timer();
            autoSaveTimer.Interval = 5000;
            autoSaveTimer.Tick += AutoSaveTimer_Tick;
            autoSaveTimer.Start();
        }

        private void AutoSaveTimer_Tick(object sender, EventArgs e)
        {
            if (currentAttemptId <= 0) return;
            if (isAutoSaving) return;
            if (isSubmitting) return;

            try
            {
                isAutoSaving = true;
                SaveAllAnswers();
                SaveAnswersToDatabase();
            }
            catch (Exception ex)
            {
                Console.WriteLine("AutoSave error: " + ex.Message);
            }
            finally
            {
                isAutoSaving = false;
            }
        }

        private void StopAutoSave()
        {
            if (autoSaveTimer == null) return;

            try
            {
                autoSaveTimer.Stop();
                autoSaveTimer.Tick -= AutoSaveTimer_Tick;
                autoSaveTimer.Dispose();
            }
            catch { }

            autoSaveTimer = null;
        }

        // =========================================================
        // NORMALIZE QUESTION TYPE
        // =========================================================

        private string NormalizeQuestionType(string type)
        {
            if (string.IsNullOrWhiteSpace(type)) return "multiple_choice";

            string value = type.Trim().ToLower().Replace("-", "_").Replace(" ", "_");

            if (value == "multiple_choice" || value == "multiplechoice" || value == "mc")
                return "multiple_choice";

            if (value == "true_false" || value == "truefalse" ||
                value == "true_or_false" || value == "tf")
                return "true_false";

            if (value == "identification" || value == "identification_question" ||
                value == "identification_questions" || value == "identify" ||
                value == "id" || value == "fill_in_the_blank" || value == "fillintheblank")
                return "identification";

            if (value == "essay" || value == "essay_question" || value == "essay_questions")
                return "essay";

            return value;
        }

        // =========================================================
        // BUILD ALL QUESTIONS
        // =========================================================

        private void BuildAllQuestions()
        {
            int y = 65;

            questionCards.Clear();
            sectionHeaders.Clear();
            multipleChoiceControls.Clear();
            trueFalseControls.Clear();
            identificationControls.Clear();
            essayControls.Clear();

            for (int i = 0; i < questions.Count; i++)
            {
                questionCards.Add(null);
                multipleChoiceControls.Add(null);
                trueFalseControls.Add(null);
                identificationControls.Add(null);
                essayControls.Add(null);
            }

            string[] typeOrder = { "multiple_choice", "true_false", "identification", "essay" };
            string[] sectionTitles = { "MULTIPLE CHOICE", "TRUE OR FALSE", "IDENTIFICATION", "ESSAY" };
            string[] sectionDirections =
            {
                "Direction: Choose the letter of the correct answer for each item.",
                "Direction: Write True if the statement is correct, or False if it is incorrect.",
                "Direction: Write the correct answer in the space provided.",
                "Direction: Answer the following item(s) in complete and well-organized sentences."
            };
            string[] romanNumerals = { "I", "II", "III", "IV" };

            int sectionCounter = 0;
            int displayNumber = 0;
            Random random = new Random();

            for (int t = 0; t < typeOrder.Length; t++)
            {
                List<int> indices = new List<int>();

                for (int i = 0; i < questions.Count; i++)
                {
                    string qType = NormalizeQuestionType(questions[i].QuestionType);
                    if (qType == typeOrder[t]) indices.Add(i);
                }

                if (indices.Count == 0) continue;

                ShuffleQuestionIndices(indices, random);

                sectionCounter++;

                Panel header = CreateSectionHeader(
                    romanNumerals[sectionCounter - 1],
                    sectionTitles[t],
                    sectionDirections[t]);

                header.Location = new Point(5, y);
                contentPanel.Controls.Add(header);
                sectionHeaders.Add(header);
                y += header.Height + 16;

                foreach (int originalIndex in indices)
                {
                    displayNumber++;

                    Panel card = CreateQuestionCard(
                        questions[originalIndex],
                        originalIndex,
                        displayNumber);

                    card.Location = new Point(5, y);
                    contentPanel.Controls.Add(card);
                    questionCards[originalIndex] = card;
                    y += card.Height + 22;
                }
            }

            Panel submitPanel = new RoundedPanel();
            submitPanel.BackColor = CardColor;
            submitPanel.BorderStyle = BorderStyle.None;
            submitPanel.Size = new Size(contentPanel.Width - 10, 118);
            submitPanel.Location = new Point(5, y);
            StyleRoundedPanel(submitPanel, false);
            contentPanel.Controls.Add(submitPanel);

            Label submitLabel = new Label();
            submitLabel.Text = "You have reached the end of the examination.";
            submitLabel.Font = new Font("Segoe UI", 11, FontStyle.Regular);
            submitLabel.ForeColor = MutedColor;
            submitLabel.AutoSize = true;
            submitLabel.Location = new Point(20, 20);
            submitPanel.Controls.Add(submitLabel);

            btnSubmit.Parent = submitPanel;
            btnSubmit.Left = (submitPanel.Width - btnSubmit.Width) / 2;
            btnSubmit.Top = 52;

            contentPanel.Height = y + submitPanel.Height + 30;

            UpdateProgress();
        }

        private void ShuffleQuestionIndices(List<int> indices, Random random)
        {
            for (int i = indices.Count - 1; i > 0; i--)
            {
                int j = random.Next(0, i + 1);
                int temp = indices[i];
                indices[i] = indices[j];
                indices[j] = temp;
            }
        }

        // =========================================================
        // ROUNDED PANEL HELPERS
        // =========================================================

        private GraphicsPath GetRoundedRectPath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int d = radius * 2;

            path.StartFigure();
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();

            return path;
        }

        private void ApplyRoundedRegion(Panel panel)
        {
            if (panel.Width <= 0 || panel.Height <= 0) return;

            Rectangle rect = new Rectangle(0, 0, panel.Width - 1, panel.Height - 1);

            using (GraphicsPath path = GetRoundedRectPath(rect, 16))
            {
                panel.Region = new Region(path);
            }
        }

        private void StyleRoundedPanel(Panel panel, bool showAccent,
                                       Color? fillColor = null, int accentHeight = 44)
        {
            panel.BorderStyle = BorderStyle.None;

            Color actualFill = fillColor.HasValue ? fillColor.Value : CardColor;

            ApplyRoundedRegion(panel);
            panel.Resize += (s, e) => ApplyRoundedRegion(panel);

            panel.Paint += (s, e) =>
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                Rectangle rect = new Rectangle(0, 0, panel.Width - 1, panel.Height - 1);

                using (GraphicsPath path = GetRoundedRectPath(rect, 16))
                {
                    using (SolidBrush backBrush = new SolidBrush(actualFill))
                    {
                        g.FillPath(backBrush, path);
                    }

                    using (Pen borderPen = new Pen(BorderColor, 1.4f))
                    {
                        g.DrawPath(borderPen, path);
                    }
                }

                if (showAccent)
                {
                    using (SolidBrush accentBrush = new SolidBrush(MaroonColor))
                    {
                        Rectangle accentRect = new Rectangle(0, 0, 5,
                            Math.Min(accentHeight, panel.Height));
                        g.FillRectangle(accentBrush, accentRect);
                    }
                }
            };
        }

        // =========================================================
        // SECTION HEADER
        // =========================================================

        private Panel CreateSectionHeader(string romanNumeral, string sectionTitle, string direction)
        {
            Panel header = new RoundedPanel();
            header.Width = contentPanel.Width - 10;
            header.Height = 92;
            StyleRoundedPanel(header, true, MaroonSoft, header.Height);

            Label lblTitle = new Label();
            lblTitle.Text = romanNumeral + ".  " + sectionTitle;
            lblTitle.Font = new Font("Segoe UI", 16, FontStyle.Bold);
            lblTitle.ForeColor = MaroonColor;
            lblTitle.AutoSize = true;
            lblTitle.Location = new Point(28, 16);
            header.Controls.Add(lblTitle);

            Label lblDirection = new Label();
            lblDirection.Text = direction;
            lblDirection.Font = new Font("Segoe UI", 10.5F, FontStyle.Italic);
            lblDirection.ForeColor = TextColor;
            lblDirection.AutoSize = false;
            lblDirection.Location = new Point(28, 52);
            lblDirection.Size = new Size(header.Width - 56, 32);
            header.Controls.Add(lblDirection);

            return header;
        }

        private void ResizeSectionHeader(Panel header)
        {
            foreach (Control control in header.Controls)
            {
                if (control is Label && control.Location.Y >= 40)
                {
                    control.Width = header.Width - 56;
                }
            }
        }

        // =========================================================
        // QUESTION CARD
        // =========================================================

        private Panel CreateQuestionCard(QuizQuestion q, int originalIndex, int displayNumber)
        {
            Panel card = new RoundedPanel();
            card.BackColor = CardColor;
            card.Width = contentPanel.Width - 10;

            string questionType = NormalizeQuestionType(q.QuestionType);

            int height = 250;
            if (questionType == "multiple_choice") height = 510;
            else if (questionType == "true_false") height = 350;
            else if (questionType == "identification") height = 330;
            else if (questionType == "essay") height = 420;

            card.Height = height;
            StyleRoundedPanel(card, true);

            Label numberLabel = new Label();
            numberLabel.Text = "QUESTION " + displayNumber;
            numberLabel.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            numberLabel.ForeColor = MaroonColor;
            numberLabel.AutoSize = true;
            numberLabel.Location = new Point(28, 22);
            card.Controls.Add(numberLabel);

            Label typeLabel = new Label();
            if (questionType == "multiple_choice") typeLabel.Text = "MULTIPLE CHOICE";
            else if (questionType == "true_false") typeLabel.Text = "TRUE OR FALSE";
            else if (questionType == "identification") typeLabel.Text = "IDENTIFICATION";
            else if (questionType == "essay") typeLabel.Text = "ESSAY";
            else typeLabel.Text = questionType.ToUpper();

            typeLabel.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            typeLabel.ForeColor = MutedColor;
            typeLabel.AutoSize = true;
            typeLabel.Location = new Point(28, 48);
            card.Controls.Add(typeLabel);

            Label questionLabel = new Label();
            questionLabel.Text = q.Question;
            questionLabel.Font = new Font("Segoe UI", 17, FontStyle.Bold);
            questionLabel.ForeColor = DarkColor;
            questionLabel.Location = new Point(28, 78);
            questionLabel.Size = new Size(card.Width - 56, 82);
            questionLabel.AutoEllipsis = false;
            card.Controls.Add(questionLabel);

            if (questionType == "multiple_choice")
            {
                RadioButton[] radios = new RadioButton[4];
                radios[0] = CreateOption("A. " + q.ChoiceA, 28, 170);
                radios[1] = CreateOption("B. " + q.ChoiceB, 28, 240);
                radios[2] = CreateOption("C. " + q.ChoiceC, 28, 310);
                radios[3] = CreateOption("D. " + q.ChoiceD, 28, 380);

                radios[0].Tag = "A";
                radios[1].Tag = "B";
                radios[2].Tag = "C";
                radios[3].Tag = "D";

                foreach (RadioButton rb in radios) card.Controls.Add(rb);
                multipleChoiceControls[originalIndex] = radios;
            }
            else if (questionType == "true_false")
            {
                RadioButton[] radios = new RadioButton[2];
                radios[0] = CreateOption("True", 28, 175);
                radios[1] = CreateOption("False", 28, 245);

                radios[0].Tag = "TRUE";
                radios[1].Tag = "FALSE";

                card.Controls.Add(radios[0]);
                card.Controls.Add(radios[1]);
                trueFalseControls[originalIndex] = radios;
            }
            else if (questionType == "identification")
            {
                Label answerLabel = new Label();
                answerLabel.Text = "Your Answer:";
                answerLabel.Font = new Font("Segoe UI", 10, FontStyle.Bold);
                answerLabel.ForeColor = TextColor;
                answerLabel.AutoSize = true;
                answerLabel.Location = new Point(28, 170);
                card.Controls.Add(answerLabel);

                TextBox identification = new TextBox();
                identification.Font = new Font("Segoe UI", 14);
                identification.ForeColor = TextColor;
                identification.BackColor = OptionBackColor;
                identification.BorderStyle = BorderStyle.FixedSingle;
                identification.Location = new Point(28, 198);
                identification.Size = new Size(card.Width - 56, 45);
                identification.MaxLength = 500;
                identification.Multiline = false;
                card.Controls.Add(identification);

                identificationControls[originalIndex] = identification;

                BlockTextEditingShortcuts(identification);
                identification.TextChanged += Identification_TextChanged;
            }
            else if (questionType == "essay")
            {
                TextBox essay = new TextBox();
                essay.Multiline = true;
                essay.ScrollBars = ScrollBars.Vertical;
                essay.Font = new Font("Segoe UI", 13);
                essay.ForeColor = TextColor;
                essay.BackColor = OptionBackColor;
                essay.BorderStyle = BorderStyle.FixedSingle;
                essay.Location = new Point(28, 175);
                essay.Size = new Size(card.Width - 56, 195);
                essay.MaxLength = 5000;
                card.Controls.Add(essay);

                essayControls[originalIndex] = essay;

                BlockTextEditingShortcuts(essay);
                essay.TextChanged += Identification_TextChanged;
            }

            return card;
        }

        private RadioButton CreateOption(string text, int x, int y)
        {
            RadioButton rb = new RadioButton();
            rb.Text = text;
            rb.Font = new Font("Segoe UI", 13);
            rb.ForeColor = TextColor;
            rb.BackColor = OptionBackColor;
            rb.Location = new Point(x, y);
            rb.Size = new Size(contentPanel.Width - 66, 55);
            rb.AutoSize = false;
            rb.Padding = new Padding(14, 0, 8, 0);
            rb.Cursor = Cursors.Hand;
            rb.FlatStyle = FlatStyle.Standard;
            rb.CheckedChanged += Option_CheckedChanged;

            BlockTextEditingShortcuts(rb);
            return rb;
        }

        private void Option_CheckedChanged(object sender, EventArgs e)
        {
            RadioButton rb = sender as RadioButton;
            if (rb == null) return;

            if (rb.Checked)
            {
                rb.BackColor = MaroonSoft;
                rb.ForeColor = MaroonColor;
                rb.Font = new Font(rb.Font, FontStyle.Bold);
            }
            else
            {
                rb.BackColor = OptionBackColor;
                rb.ForeColor = TextColor;
                rb.Font = new Font(rb.Font.FontFamily, rb.Font.Size, FontStyle.Regular);
            }

            UpdateProgress();
        }

        private void Identification_TextChanged(object sender, EventArgs e)
        {
            UpdateProgress();
        }

        private void ResizeQuestionCard(Panel card)
        {
            foreach (Control control in card.Controls)
            {
                if (control is RadioButton) control.Width = card.Width - 66;
                else if (control is TextBox) control.Width = card.Width - 56;
                else if (control is Label)
                {
                    Label label = control as Label;
                    if (label.Location.Y >= 70) label.Width = card.Width - 56;
                }
            }
        }

        // =========================================================
        // VALIDATE ANSWERS
        // =========================================================

        private bool ValidateAllAnswers()
        {
            for (int i = 0; i < questions.Count; i++)
            {
                QuizQuestion q = questions[i];
                string type = NormalizeQuestionType(q.QuestionType);
                bool answered = false;

                if (type == "multiple_choice")
                {
                    RadioButton[] radios = multipleChoiceControls[i];
                    if (radios != null)
                    {
                        foreach (RadioButton rb in radios)
                        {
                            if (rb.Checked) { answered = true; break; }
                        }
                    }
                }
                else if (type == "true_false")
                {
                    RadioButton[] radios = trueFalseControls[i];
                    if (radios != null)
                        answered = radios[0].Checked || radios[1].Checked;
                }
                else if (type == "identification")
                {
                    TextBox identification = identificationControls[i];
                    if (identification != null)
                        answered = !string.IsNullOrWhiteSpace(identification.Text);
                }
                else if (type == "essay")
                {
                    TextBox essay = essayControls[i];
                    if (essay != null)
                        answered = !string.IsNullOrWhiteSpace(essay.Text);
                }

                if (!answered)
                {
                    MessageBox.Show("Please answer Question " + (i + 1) + " before submitting.",
                        "Unanswered Question", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                    ScrollToQuestion(i);
                    return false;
                }
            }

            return true;
        }

        // =========================================================
        // COLLECT CURRENT ANSWERS
        // =========================================================

        private void SaveAllAnswers()
        {
            studentAnswers.Clear();

            for (int i = 0; i < questions.Count; i++)
            {
                QuizQuestion q = questions[i];
                string type = NormalizeQuestionType(q.QuestionType);
                string answer = "";

                if (type == "multiple_choice")
                {
                    RadioButton[] radios = multipleChoiceControls[i];
                    if (radios != null)
                    {
                        if (radios[0].Checked) answer = "A";
                        else if (radios[1].Checked) answer = "B";
                        else if (radios[2].Checked) answer = "C";
                        else if (radios[3].Checked) answer = "D";
                    }
                }
                else if (type == "true_false")
                {
                    RadioButton[] radios = trueFalseControls[i];
                    if (radios != null)
                    {
                        if (radios[0].Checked) answer = "TRUE";
                        else if (radios[1].Checked) answer = "FALSE";
                    }
                }
                else if (type == "identification")
                {
                    TextBox identification = identificationControls[i];
                    if (identification != null) answer = identification.Text.Trim();
                }
                else if (type == "essay")
                {
                    TextBox essay = essayControls[i];
                    if (essay != null) answer = essay.Text.Trim();
                }

                studentAnswers[i] = answer;
            }
        }

        // =========================================================
        // SAVE ANSWERS TO DATABASE
        // =========================================================

        private bool SaveAnswersToDatabase()
        {
            if (currentAttemptId <= 0) return false;

            SaveAllAnswers();

            string connStr = SettingsManager.Current.GetConnectionString();

            using (var conn = new MySqlConnection(connStr))
            {
                MySqlTransaction tx = null;

                try
                {
                    conn.Open();
                    tx = conn.BeginTransaction();

                    for (int i = 0; i < questions.Count; i++)
                    {
                        QuizQuestion q = questions[i];
                        int questionId = q.QuestionId;

                        string answer = studentAnswers.ContainsKey(i) ? studentAnswers[i] : "";
                        string type = NormalizeQuestionType(q.QuestionType);

                        bool isCorrect = false;

                        if (type != "essay" &&
                            !string.IsNullOrWhiteSpace(answer) &&
                            !string.IsNullOrWhiteSpace(q.CorrectAnswer))
                        {
                            isCorrect = answer.Trim().ToUpper() == q.CorrectAnswer.Trim().ToUpper();
                        }

                        if (string.IsNullOrWhiteSpace(answer))
                        {
                            using (var deleteCmd = new MySqlCommand(
                                @"DELETE FROM student_answers
                                  WHERE attempt_id = @attempt_id AND question_id = @question_id",
                                conn, tx))
                            {
                                deleteCmd.Parameters.AddWithValue("@attempt_id", currentAttemptId);
                                deleteCmd.Parameters.AddWithValue("@question_id", questionId);
                                deleteCmd.ExecuteNonQuery();
                            }
                            continue;
                        }

                        bool exists = false;
                        using (var checkCmd = new MySqlCommand(
                            @"SELECT COUNT(*) FROM student_answers
                              WHERE attempt_id = @attempt_id AND question_id = @question_id",
                            conn, tx))
                        {
                            checkCmd.Parameters.AddWithValue("@attempt_id", currentAttemptId);
                            checkCmd.Parameters.AddWithValue("@question_id", questionId);
                            exists = Convert.ToInt32(checkCmd.ExecuteScalar()) > 0;
                        }

                        if (exists)
                        {
                            using (var updateCmd = new MySqlCommand(
                                @"UPDATE student_answers
                                  SET student_answer = @student_answer, is_correct = @is_correct
                                  WHERE attempt_id = @attempt_id AND question_id = @question_id",
                                conn, tx))
                            {
                                updateCmd.Parameters.AddWithValue("@student_answer", answer);
                                updateCmd.Parameters.AddWithValue("@is_correct", isCorrect ? 1 : 0);
                                updateCmd.Parameters.AddWithValue("@attempt_id", currentAttemptId);
                                updateCmd.Parameters.AddWithValue("@question_id", questionId);
                                updateCmd.ExecuteNonQuery();
                            }
                        }
                        else
                        {
                            using (var insertCmd = new MySqlCommand(
                                @"INSERT INTO student_answers
                                  (attempt_id, question_id, student_answer, is_correct)
                                  VALUES (@attempt_id, @question_id, @student_answer, @is_correct)",
                                conn, tx))
                            {
                                insertCmd.Parameters.AddWithValue("@attempt_id", currentAttemptId);
                                insertCmd.Parameters.AddWithValue("@question_id", questionId);
                                insertCmd.Parameters.AddWithValue("@student_answer", answer);
                                insertCmd.Parameters.AddWithValue("@is_correct", isCorrect ? 1 : 0);
                                insertCmd.ExecuteNonQuery();
                            }
                        }
                    }

                    tx.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    try { tx?.Rollback(); } catch { }
                    Console.WriteLine("SaveAnswersToDatabase error: " + ex.Message);
                    return false;
                }
            }
        }

        // =========================================================
        // MANUAL SUBMIT
        // =========================================================

        private void BtnSubmit_Click(object sender, EventArgs e)
        {
            if (isSubmitting) return;

            if (currentAttemptId <= 0)
            {
                MessageBox.Show(
                    "The examination attempt could not be identified.\n\n" +
                    "Please restart the examination.",
                    "Attempt Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!ValidateAllAnswers()) return;

            SaveAllAnswers();

            DialogResult confirm = MessageBox.Show(
                "Are you sure you want to submit the examination?\n\n" +
                "You will not be able to change your answers after submission.",
                "Submit Examination", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            isSubmitting = true;

            try
            {
                StopExamTimer();
                SaveQuizResult(false);
            }
            finally
            {
                isSubmitting = false;
            }
        }

        // =========================================================
        // SAVE QUIZ RESULT
        // =========================================================

        private void SaveQuizResult(bool automaticSubmit)
        {
            score = 0;

            for (int i = 0; i < questions.Count; i++)
            {
                QuizQuestion q = questions[i];
                string studentAnswer = studentAnswers.ContainsKey(i) ? studentAnswers[i] : "";
                string type = NormalizeQuestionType(q.QuestionType);

                if (type == "essay") continue;

                if (!string.IsNullOrWhiteSpace(studentAnswer) &&
                    !string.IsNullOrWhiteSpace(q.CorrectAnswer))
                {
                    if (studentAnswer.Trim().ToUpper() == q.CorrectAnswer.Trim().ToUpper())
                        score++;
                }
            }

            if (!SaveAnswersToDatabase())
            {
                MessageBox.Show(
                    "Your answers could not be saved.\n\n" +
                    "Please check the database connection and try submitting again.",
                    "Unable to Save Answers",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            SaveAttemptToDatabase(score, questions.Count, automaticSubmit);
        }

        // =========================================================
        // SAVE ATTEMPT TO DATABASE
        // =========================================================

        private void SaveAttemptToDatabase(int finalScore, int totalQuestions, bool automaticSubmit)
        {
            decimal percentage = 0;
            if (totalQuestions > 0)
                percentage = ((decimal)finalScore / totalQuestions) * 100;

            string connStr = SettingsManager.Current.GetConnectionString();

            using (var conn = new MySqlConnection(connStr))
            {
                MySqlTransaction tx = null;

                try
                {
                    conn.Open();
                    tx = conn.BeginTransaction();

                    string attemptQuery = @"
                        UPDATE quiz_attempts
                        SET score = @score,
                            total_questions = @total_questions,
                            percentage = @percentage,
                            status = 'SUBMITTED',
                            last_seen = NOW(),
                            remaining_seconds = @remaining_seconds
                        WHERE attempt_id = @attempt_id";

                    using (var cmd = new MySqlCommand(attemptQuery, conn, tx))
                    {
                        cmd.Parameters.AddWithValue("@score", finalScore);
                        cmd.Parameters.AddWithValue("@total_questions", totalQuestions);
                        cmd.Parameters.AddWithValue("@percentage", percentage);
                        cmd.Parameters.AddWithValue("@remaining_seconds", Math.Max(0, remainingSeconds));
                        cmd.Parameters.AddWithValue("@attempt_id", currentAttemptId);

                        int affectedRows = cmd.ExecuteNonQuery();

                        if (affectedRows != 1)
                            throw new Exception("The quiz attempt could not be updated.");
                    }

                    tx.Commit();

                    StopExamTimer();
                    StopAutoSave();
                    StopHeartbeat();

                    if (automaticSubmit)
                    {
                        MessageBox.Show(
                            "Time is up.\n\n" +
                            "Your examination has been submitted automatically.\n\n" +
                            "Score: " + finalScore + " / " + totalQuestions +
                            "\nPercentage: " + percentage.ToString("0.00") + "%",
                            "Time Expired", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show(
                            "Examination submitted successfully!\n\n" +
                            "Score: " + finalScore + " / " + totalQuestions +
                            "\nPercentage: " + percentage.ToString("0.00") + "%",
                            "Examination Submitted", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }

                    DisableQuiz();
                }
                catch (Exception ex)
                {
                    try { tx?.Rollback(); } catch { }

                    MessageBox.Show(
                        "Your examination could not be submitted.\n\n" +
                        "Error:\n" + ex.Message +
                        "\n\nAttempt ID: " + currentAttemptId,
                        "Database Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        // =========================================================
        // SCROLL / PROGRESS
        // =========================================================

        private void ScrollToQuestion(int index)
        {
            if (index < 0 || index >= questionCards.Count) return;

            Panel card = questionCards[index];
            if (card != null) scrollPanel.ScrollControlIntoView(card);
        }

        private void UpdateProgress()
        {
            if (questions.Count == 0)
            {
                progressBar.Value = 0;
                return;
            }

            int answered = 0;
            for (int i = 0; i < questions.Count; i++)
            {
                if (IsQuestionAnswered(i)) answered++;
            }

            int percentage = (int)(((double)answered / questions.Count) * 100);

            if (percentage < 0) percentage = 0;
            if (percentage > 100) percentage = 100;

            progressBar.Value = percentage;
        }

        private bool IsQuestionAnswered(int index)
        {
            if (index < 0 || index >= questions.Count) return false;

            QuizQuestion q = questions[index];
            string type = NormalizeQuestionType(q.QuestionType);

            if (type == "multiple_choice")
            {
                RadioButton[] radios = multipleChoiceControls[index];
                if (radios == null) return false;
                foreach (RadioButton rb in radios)
                    if (rb.Checked) return true;
                return false;
            }

            if (type == "true_false")
            {
                RadioButton[] radios = trueFalseControls[index];
                if (radios == null) return false;
                return radios[0].Checked || radios[1].Checked;
            }

            if (type == "identification")
            {
                TextBox identification = identificationControls[index];
                if (identification == null) return false;
                return !string.IsNullOrWhiteSpace(identification.Text);
            }

            if (type == "essay")
            {
                TextBox essay = essayControls[index];
                if (essay == null) return false;
                return !string.IsNullOrWhiteSpace(essay.Text);
            }

            return false;
        }

        // =========================================================
        // ANTI CHEAT
        // =========================================================

        private void EnableAntiCheat()
        {
            this.KeyDown += StudentQuizForm_KeyDown;
            this.MouseDown += StudentQuizForm_MouseDown;
            ApplyAntiCheatToControlTree(this);
        }

        private void StudentQuizForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && (e.KeyCode == Keys.C || e.KeyCode == Keys.X ||
                              e.KeyCode == Keys.V || e.KeyCode == Keys.A ||
                              e.KeyCode == Keys.Z))
            {
                e.SuppressKeyPress = true;
                e.Handled = true;
                return;
            }

            if (e.Shift && e.KeyCode == Keys.Insert)
            {
                e.SuppressKeyPress = true;
                e.Handled = true;
            }
        }

        private void StudentQuizForm_MouseDown(object sender, MouseEventArgs e)
        {
            // Right-click disabled
        }

        private void ApplyAntiCheatToControlTree(Control parent)
        {
            foreach (Control control in parent.Controls)
            {
                control.ContextMenuStrip = null;
                control.MouseDown += AntiCheat_MouseDown;

                if (control is Label)
                    ((Label)control).Cursor = Cursors.Default;

                if (control.HasChildren)
                    ApplyAntiCheatToControlTree(control);
            }
        }

        private void AntiCheat_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                Control control = sender as Control;
                if (control != null) control.ContextMenuStrip = null;
            }
        }

        private void BlockTextEditingShortcuts(Control control)
        {
            control.KeyDown += TextControl_KeyDown;
            control.MouseDown += TextControl_MouseDown;
            control.ContextMenuStrip = null;
        }

        private void TextControl_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && (e.KeyCode == Keys.C || e.KeyCode == Keys.X ||
                              e.KeyCode == Keys.V || e.KeyCode == Keys.A))
            {
                e.SuppressKeyPress = true;
                e.Handled = true;
                return;
            }

            if (e.Shift && e.KeyCode == Keys.Insert)
            {
                e.SuppressKeyPress = true;
                e.Handled = true;
            }
        }

        private void TextControl_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                TextBox textBox = sender as TextBox;
                if (textBox != null) textBox.ContextMenuStrip = null;
            }
        }

        // =========================================================
        // DISABLE QUIZ
        // =========================================================

        private void DisableQuiz()
        {
            StopExamTimer();
            StopAutoSave();
            StopHeartbeat();

            if (btnSubmit != null) btnSubmit.Enabled = false;
            if (scrollPanel != null) scrollPanel.Enabled = false;
            if (lblTimer != null) lblTimer.Text = "TIME: 00:00";

            if (lblInstruction != null)
                lblInstruction.Text = "This examination is no longer available.";
        }
    }
}