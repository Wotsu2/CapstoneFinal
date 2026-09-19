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

        private List<QuizQuestion> questions =
            new List<QuizQuestion>();

        private Dictionary<int, string> studentAnswers =
            new Dictionary<int, string>();

        private int selectedQuizId = 0;

        // Student Demo user_id
        private int studentUserId = 2;

        private int score = 0;

        // =========================================================
        // COLORS
        // =========================================================

        private readonly Color BackgroundColor =
            Color.FromArgb(241, 245, 249);

        private readonly Color CardColor =
            Color.White;

        private readonly Color DarkColor =
            Color.FromArgb(15, 23, 42);

        private readonly Color TextColor =
            Color.FromArgb(51, 65, 85);

        private readonly Color MutedColor =
            Color.FromArgb(100, 116, 139);

        private readonly Color GreenColor =
            Color.FromArgb(22, 163, 74);

        private readonly Color BorderColor =
            Color.FromArgb(226, 232, 240);

        private readonly Color MaroonColor =
            Color.FromArgb(128, 45, 58);

        private readonly Color MaroonSoft =
            Color.FromArgb(250, 235, 238);

        private readonly Color OptionBackColor =
            Color.FromArgb(248, 250, 252);

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

        // NEW: EXAMINATION PERIOD
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

        private List<Panel> questionCards =
            new List<Panel>();

        private List<Panel> sectionHeaders =
            new List<Panel>();

        private List<RadioButton[]> multipleChoiceControls =
            new List<RadioButton[]>();

        private List<RadioButton[]> trueFalseControls =
            new List<RadioButton[]>();

        private List<TextBox> identificationControls =
            new List<TextBox>();

        private List<TextBox> essayControls =
            new List<TextBox>();

        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public StudentQuizForm()
        {
            InitializeUI();
            LoadLatestQuiz();
        }

        // =========================================================
        // INITIALIZE UI
        // =========================================================

        private void InitializeUI()
        {
            this.Text =
                "Student Examination System";

            this.StartPosition =
                FormStartPosition.CenterScreen;

            this.Size =
                new Size(1100, 800);

            this.MinimumSize =
                new Size(850, 650);

            this.BackColor =
                BackgroundColor;

            this.FormBorderStyle =
                FormBorderStyle.Sizable;

            this.KeyPreview = true;

            // =====================================================
            // HEADER
            // =====================================================

            headerPanel =
                new SmoothPanel();

            headerPanel.Dock =
                DockStyle.Top;

            headerPanel.Height =
                145;

            headerPanel.BackColor =
                DarkColor;

            this.Controls.Add(
                headerPanel);

            Panel headerAccent =
                new Panel();

            headerAccent.Dock =
                DockStyle.Bottom;

            headerAccent.Height =
                4;

            headerAccent.BackColor =
                MaroonColor;

            headerPanel.Controls.Add(
                headerAccent);

            // =====================================================
            // EXAM PERIOD
            // =====================================================

            lblExamPeriod =
                new Label();

            lblExamPeriod.Text =
                "EXAMINATION";

            lblExamPeriod.Font =
                new Font(
                    "Segoe UI",
                    11,
                    FontStyle.Bold);

            lblExamPeriod.ForeColor =
                Color.FromArgb(
                    248,
                    113,
                    113);

            lblExamPeriod.AutoSize =
                true;

            lblExamPeriod.Location =
                new Point(
                    38,
                    10);

            headerPanel.Controls.Add(
                lblExamPeriod);

            // =====================================================
            // QUIZ TITLE
            // =====================================================

            lblQuizTitle =
                new Label();

            lblQuizTitle.Text =
                "Loading Quiz...";

            lblQuizTitle.Font =
                new Font(
                    "Segoe UI",
                    25,
                    FontStyle.Bold);

            lblQuizTitle.ForeColor =
                Color.White;

            lblQuizTitle.AutoSize =
                true;

            lblQuizTitle.Location =
                new Point(
                    35,
                    30);

            headerPanel.Controls.Add(
                lblQuizTitle);

            // =====================================================
            // SUBJECT
            // =====================================================

            lblSubject =
                new Label();

            lblSubject.Text =
                "Preparing examination...";

            lblSubject.Font =
                new Font(
                    "Segoe UI",
                    12);

            lblSubject.ForeColor =
                Color.FromArgb(
                    203,
                    213,
                    225);

            lblSubject.AutoSize =
                true;

            lblSubject.Location =
                new Point(
                    38,
                    72);

            headerPanel.Controls.Add(
                lblSubject);

            // =====================================================
            // INSTRUCTION
            // =====================================================

            lblInstruction =
                new Label();

            lblInstruction.Text =
                "Answer all questions carefully. Scroll down to continue.";

            lblInstruction.Font =
                new Font(
                    "Segoe UI",
                    10);

            lblInstruction.ForeColor =
                Color.FromArgb(
                    148,
                    163,
                    184);

            lblInstruction.AutoSize =
                true;

            lblInstruction.Location =
                new Point(
                    38,
                    101);

            headerPanel.Controls.Add(
                lblInstruction);

            // =====================================================
            // SCROLL PANEL
            // =====================================================

            scrollPanel =
                new SmoothPanel();

            scrollPanel.Anchor =
                AnchorStyles.Top |
                AnchorStyles.Bottom |
                AnchorStyles.Left |
                AnchorStyles.Right;

            scrollPanel.Location =
                new Point(
                    0,
                    headerPanel.Height);

            scrollPanel.Size =
                new Size(
                    this.ClientSize.Width,
                    Math.Max(
                        0,
                        this.ClientSize.Height -
                        headerPanel.Height));

            scrollPanel.AutoScroll =
                true;

            scrollPanel.BackColor =
                BackgroundColor;

            scrollPanel.Padding =
                new Padding(
                    0,
                    25,
                    0,
                    40);

            this.Controls.Add(
                scrollPanel);

            headerPanel.BringToFront();

            // =====================================================
            // CONTENT PANEL
            // =====================================================

            contentPanel =
                new SmoothPanel();

            contentPanel.BackColor =
                BackgroundColor;

            contentPanel.Size =
                new Size(
                    900,
                    500);

            contentPanel.Location =
                new Point(
                    50,
                    25);

            scrollPanel.Controls.Add(
                contentPanel);

            // =====================================================
            // QUESTION COUNT
            // =====================================================

            lblQuestionCount =
                new Label();

            lblQuestionCount.Text =
                "0 Questions";

            lblQuestionCount.Font =
                new Font(
                    "Segoe UI",
                    14,
                    FontStyle.Bold);

            lblQuestionCount.ForeColor =
                DarkColor;

            lblQuestionCount.AutoSize =
                true;

            lblQuestionCount.Location =
                new Point(
                    5,
                    0);

            contentPanel.Controls.Add(
                lblQuestionCount);

            // =====================================================
            // PROGRESS BAR
            // =====================================================

            progressBar =
                new ProgressBar();

            progressBar.Minimum =
                0;

            progressBar.Maximum =
                100;

            progressBar.Value =
                0;

            progressBar.Location =
                new Point(
                    5,
                    34);

            progressBar.Size =
                new Size(
                    890,
                    12);

            contentPanel.Controls.Add(
                progressBar);

            // =====================================================
            // SUBMIT BUTTON
            // =====================================================

            btnSubmit =
                new Button();

            btnSubmit.Text =
                "✓  SUBMIT";

            btnSubmit.Size =
                new Size(
                    240,
                    55);

            btnSubmit.Font =
                new Font(
                    "Segoe UI",
                    13,
                    FontStyle.Bold);

            btnSubmit.BackColor =
                GreenColor;

            btnSubmit.ForeColor =
                Color.White;

            btnSubmit.FlatStyle =
                FlatStyle.Flat;

            btnSubmit.FlatAppearance.BorderSize =
                0;

            btnSubmit.Cursor =
                Cursors.Hand;

            btnSubmit.Visible =
                false;

            btnSubmit.Click +=
                BtnSubmit_Click;

            contentPanel.Controls.Add(
                btnSubmit);

            // =====================================================
            // RESIZE
            // =====================================================

            this.Resize +=
                StudentQuizForm_Resize;

            StudentQuizForm_Resize(
                null,
                EventArgs.Empty);

            // =====================================================
            // ANTI COPY / PASTE
            // =====================================================

            EnableAntiCheat();
        }

        // =========================================================
        // RESIZE
        // =========================================================

        private void StudentQuizForm_Resize(
            object sender,
            EventArgs e)
        {
            if (scrollPanel == null ||
                contentPanel == null)
                return;

            if (headerPanel != null)
            {
                scrollPanel.Location =
                    new Point(
                        0,
                        headerPanel.Height);

                scrollPanel.Size =
                    new Size(
                        this.ClientSize.Width,
                        Math.Max(
                            0,
                            this.ClientSize.Height -
                            headerPanel.Height));
            }

            int availableWidth =
                scrollPanel.ClientSize.Width;

            int contentWidth =
                Math.Max(
                    780,
                    Math.Min(
                        900,
                        availableWidth - 70));

            contentPanel.Width =
                contentWidth;

            contentPanel.Left =
                Math.Max(
                    25,
                    (availableWidth -
                     contentPanel.Width) / 2);

            progressBar.Width =
                contentPanel.Width - 10;

            foreach (Panel header
                     in sectionHeaders)
            {
                if (header == null)
                    continue;

                header.Width =
                    contentPanel.Width - 10;

                ResizeSectionHeader(
                    header);

                header.Invalidate();
            }

            foreach (Panel card
                     in questionCards)
            {
                if (card == null)
                    continue;

                card.Width =
                    contentPanel.Width - 10;

                ResizeQuestionCard(
                    card);

                card.Invalidate();
            }

            if (btnSubmit != null)
            {
                btnSubmit.Left =
                    (contentPanel.Width -
                     btnSubmit.Width) / 2;
            }
        }

        // =========================================================
        // LOAD LATEST QUIZ
        // =========================================================

        private void LoadLatestQuiz()
        {
            using (MySqlConnection connection =
                   DatabaseConnection.GetConnection())
            {
                try
                {
                    connection.Open();

                    string query = @"
                        SELECT
                            quiz_id,
                            quiz_title,
                            subject,
                            exam_period
                        FROM quizzes
                        ORDER BY quiz_id DESC
                        LIMIT 1;";

                    using (MySqlCommand command =
                           new MySqlCommand(
                               query,
                               connection))
                    {
                        using (MySqlDataReader reader =
                               command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                selectedQuizId =
                                    Convert.ToInt32(
                                        reader["quiz_id"]);

                                // =================================================
                                // GET EXAM PERIOD
                                // =================================================

                                string examPeriod =
                                    "PRELIM";

                                if (reader["exam_period"] !=
                                    DBNull.Value)
                                {
                                    examPeriod =
                                        reader[
                                            "exam_period"]
                                        .ToString()
                                        .Trim()
                                        .ToUpper();
                                }

                                lblExamPeriod.Text =
                                    GetExamPeriodDisplay(
                                        examPeriod);

                                // =================================================
                                // QUIZ TITLE
                                // =================================================

                                lblQuizTitle.Text =
                                    reader[
                                        "quiz_title"]
                                    .ToString();

                                // =================================================
                                // SUBJECT
                                // =================================================

                                lblSubject.Text =
                                    "Subject: " +
                                    reader[
                                        "subject"]
                                    .ToString();

                                // =================================================
                                // INSTRUCTION
                                // =================================================

                                lblInstruction.Text =
                                    "Answer all questions carefully. Scroll down to continue.";
                            }
                            else
                            {
                                MessageBox.Show(
                                    "There are no quizzes available yet.",
                                    "No Quiz Available",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Information);

                                DisableQuiz();

                                return;
                            }
                        }
                    }

                    LoadQuestions();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "Unable to load the quiz.\n\n" +
                        ex.Message,
                        "Database Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);

                    DisableQuiz();
                }
            }
        }

        // =========================================================
        // EXAM PERIOD DISPLAY
        // =========================================================

        private string GetExamPeriodDisplay(
            string examPeriod)
        {
            switch (examPeriod)
            {
                case "PRELIM":
                    return "PRELIM EXAMINATION";

                case "MIDTERM":
                    return "MIDTERM EXAMINATION";

                case "SEMIFINALS":
                    return "SEMIFINALS EXAMINATION";

                case "FINALS":
                    return "FINAL EXAMINATION";

                default:
                    if (string.IsNullOrWhiteSpace(
                        examPeriod))
                    {
                        return "EXAMINATION";
                    }

                    return examPeriod.ToUpper() +
                           " EXAMINATION";
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

            contentPanel.Controls.Add(
                lblQuestionCount);

            contentPanel.Controls.Add(
                progressBar);

            using (MySqlConnection connection =
                   DatabaseConnection.GetConnection())
            {
                try
                {
                    connection.Open();

                    string query = @"
                        SELECT
                            question_id,
                            question_text,
                            question_type,
                            choice_a,
                            choice_b,
                            choice_c,
                            choice_d,
                            correct_answer
                        FROM questions
                        WHERE quiz_id = @quiz_id
                        ORDER BY question_id ASC;";

                    using (MySqlCommand command =
                           new MySqlCommand(
                               query,
                               connection))
                    {
                        command.Parameters.AddWithValue(
                            "@quiz_id",
                            selectedQuizId);

                        using (MySqlDataReader reader =
                               command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                QuizQuestion question =
                                    new QuizQuestion();

                                question.Question =
                                    reader[
                                        "question_text"]
                                    .ToString();

                                question.QuestionType =
                                    reader[
                                        "question_type"]
                                    .ToString();

                                question.ChoiceA =
                                    reader["choice_a"] ==
                                    DBNull.Value
                                    ? ""
                                    : reader[
                                        "choice_a"]
                                    .ToString();

                                question.ChoiceB =
                                    reader["choice_b"] ==
                                    DBNull.Value
                                    ? ""
                                    : reader[
                                        "choice_b"]
                                    .ToString();

                                question.ChoiceC =
                                    reader["choice_c"] ==
                                    DBNull.Value
                                    ? ""
                                    : reader[
                                        "choice_c"]
                                    .ToString();

                                question.ChoiceD =
                                    reader["choice_d"] ==
                                    DBNull.Value
                                    ? ""
                                    : reader[
                                        "choice_d"]
                                    .ToString();

                                question.CorrectAnswer =
                                    reader[
                                        "correct_answer"] ==
                                    DBNull.Value
                                    ? ""
                                    : reader[
                                        "correct_answer"]
                                    .ToString();

                                // =================================================
                                // AUTO-DETECT IDENTIFICATION
                                // =================================================

                                string loadedType =
                                    NormalizeQuestionType(
                                        question.QuestionType);

                                bool noChoices =
                                    string.IsNullOrWhiteSpace(
                                        question.ChoiceA) &&
                                    string.IsNullOrWhiteSpace(
                                        question.ChoiceB) &&
                                    string.IsNullOrWhiteSpace(
                                        question.ChoiceC) &&
                                    string.IsNullOrWhiteSpace(
                                        question.ChoiceD);

                                bool hasCorrectAnswer =
                                    !string.IsNullOrWhiteSpace(
                                        question.CorrectAnswer);

                                if (loadedType ==
                                        "multiple_choice" &&
                                    noChoices &&
                                    hasCorrectAnswer)
                                {
                                    question.QuestionType =
                                        "identification";
                                }
                                else
                                {
                                    question.QuestionType =
                                        loadedType;
                                }

                                questions.Add(
                                    question);
                            }
                        }
                    }

                    if (questions.Count == 0)
                    {
                        MessageBox.Show(
                            "This quiz does not contain any questions.",
                            "No Questions",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);

                        DisableQuiz();

                        return;
                    }

                    lblQuestionCount.Text =
                        questions.Count +
                        " Questions";

                    progressBar.Value =
                        0;

                    BuildAllQuestions();

                    btnSubmit.Visible =
                        true;

                    StudentQuizForm_Resize(
                        null,
                        EventArgs.Empty);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "Unable to load questions.\n\n" +
                        ex.Message,
                        "Database Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);

                    DisableQuiz();
                }
            }
        }

        // =========================================================
        // NORMALIZE QUESTION TYPE
        // =========================================================

        private string NormalizeQuestionType(
            string type)
        {
            if (string.IsNullOrWhiteSpace(type))
                return "multiple_choice";

            string value =
                type.Trim()
                .ToLower()
                .Replace("-", "_")
                .Replace(" ", "_");

            if (value == "multiple_choice" ||
                value == "multiplechoice" ||
                value == "mc")
            {
                return "multiple_choice";
            }

            if (value == "true_false" ||
                value == "truefalse" ||
                value == "true_or_false" ||
                value == "tf")
            {
                return "true_false";
            }

            if (value == "identification" ||
                value == "identification_question" ||
                value == "identification_questions" ||
                value == "identify" ||
                value == "id" ||
                value == "fill_in_the_blank" ||
                value == "fillintheblank")
            {
                return "identification";
            }

            if (value == "essay" ||
                value == "essay_question" ||
                value == "essay_questions")
            {
                return "essay";
            }

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

            // Prepare control lists based on original question index
            for (int i = 0;
                 i < questions.Count;
                 i++)
            {
                questionCards.Add(null);
                multipleChoiceControls.Add(null);
                trueFalseControls.Add(null);
                identificationControls.Add(null);
                essayControls.Add(null);
            }

            // =====================================================
            // SECTION ORDER WILL NOT CHANGE
            // =====================================================

            string[] typeOrder =
            {
        "multiple_choice",
        "true_false",
        "identification",
        "essay"
    };

            string[] sectionTitles =
            {
        "MULTIPLE CHOICE",
        "TRUE OR FALSE",
        "IDENTIFICATION",
        "ESSAY"
    };

            string[] sectionDirections =
            {
        "Direction: Choose the letter of the correct answer for each item.",
        "Direction: Write True if the statement is correct, or False if it is incorrect.",
        "Direction: Write the correct answer in the space provided.",
        "Direction: Answer the following item(s) in complete and well-organized sentences."
    };

            string[] romanNumerals =
            {
        "I",
        "II",
        "III",
        "IV"
    };

            int sectionCounter = 0;
            int displayNumber = 0;

            // =====================================================
            // RANDOM GENERATOR
            // =====================================================

            Random random = new Random();

            // =====================================================
            // BUILD EACH SECTION
            // =====================================================

            for (int t = 0;
                 t < typeOrder.Length;
                 t++)
            {
                List<int> indices =
                    new List<int>();

                // -------------------------------------------------
                // GET QUESTIONS OF THIS TYPE ONLY
                // -------------------------------------------------

                for (int i = 0;
                     i < questions.Count;
                     i++)
                {
                    string qType =
                        NormalizeQuestionType(
                            questions[i].QuestionType);

                    if (qType ==
                        typeOrder[t])
                    {
                        indices.Add(i);
                    }
                }

                // No questions in this section
                if (indices.Count == 0)
                    continue;

                // =================================================
                // SCRAMBLE ONLY THIS SECTION
                // =================================================

                ShuffleQuestionIndices(
                    indices,
                    random);

                sectionCounter++;

                // =================================================
                // CREATE SECTION HEADER
                // =================================================

                Panel header =
                    CreateSectionHeader(
                        romanNumerals[
                            sectionCounter - 1],
                        sectionTitles[t],
                        sectionDirections[t]);

                header.Location =
                    new Point(
                        5,
                        y);

                contentPanel.Controls.Add(
                    header);

                sectionHeaders.Add(
                    header);

                y +=
                    header.Height + 16;

                // =================================================
                // CREATE SCRAMBLED QUESTIONS
                // =================================================

                foreach (int originalIndex
                         in indices)
                {
                    // Display number is sequential
                    // but question itself came from shuffled list
                    displayNumber++;

                    Panel card =
                        CreateQuestionCard(
                            questions[originalIndex],
                            originalIndex,
                            displayNumber);

                    card.Location =
                        new Point(
                            5,
                            y);

                    contentPanel.Controls.Add(
                        card);

                    // IMPORTANT:
                    // Keep originalIndex so checking still
                    // uses the correct question.
                    questionCards[originalIndex] =
                        card;

                    y +=
                        card.Height + 22;
                }
            }

            // =====================================================
            // SUBMIT AREA
            // =====================================================

            Panel submitPanel =
                new RoundedPanel();

            submitPanel.BackColor =
                CardColor;

            submitPanel.BorderStyle =
                BorderStyle.None;

            submitPanel.Size =
                new Size(
                    contentPanel.Width - 10,
                    118);

            submitPanel.Location =
                new Point(
                    5,
                    y);

            StyleRoundedPanel(
                submitPanel,
                false);

            contentPanel.Controls.Add(
                submitPanel);

            Label submitLabel =
                new Label();

            submitLabel.Text =
                "You have reached the end of the examination.";

            submitLabel.Font =
                new Font(
                    "Segoe UI",
                    11,
                    FontStyle.Regular);

            submitLabel.ForeColor =
                MutedColor;

            submitLabel.AutoSize =
                true;

            submitLabel.Location =
                new Point(
                    20,
                    20);

            submitPanel.Controls.Add(
                submitLabel);

            btnSubmit.Parent =
                submitPanel;

            btnSubmit.Left =
                (submitPanel.Width -
                 btnSubmit.Width) / 2;

            btnSubmit.Top =
                52;

            contentPanel.Height =
                y +
                submitPanel.Height +
                30;

            UpdateProgress();
        }
        private void ShuffleQuestionIndices(
    List<int> indices,
    Random random)
        {
            // Fisher-Yates Shuffle
            for (int i = indices.Count - 1;
                 i > 0;
                 i--)
            {
                int j =
                    random.Next(
                        0,
                        i + 1);

                int temp =
                    indices[i];

                indices[i] =
                    indices[j];

                indices[j] =
                    temp;
            }
        }

        // =========================================================
        // ROUNDED PANEL HELPERS
        // =========================================================

        private GraphicsPath GetRoundedRectPath(
            Rectangle rect,
            int radius)
        {
            GraphicsPath path =
                new GraphicsPath();

            int d =
                radius * 2;

            path.StartFigure();

            path.AddArc(
                rect.X,
                rect.Y,
                d,
                d,
                180,
                90);

            path.AddArc(
                rect.Right - d,
                rect.Y,
                d,
                d,
                270,
                90);

            path.AddArc(
                rect.Right - d,
                rect.Bottom - d,
                d,
                d,
                0,
                90);

            path.AddArc(
                rect.X,
                rect.Bottom - d,
                d,
                d,
                90,
                90);

            path.CloseFigure();

            return path;
        }

        private void ApplyRoundedRegion(
            Panel panel)
        {
            if (panel.Width <= 0 ||
                panel.Height <= 0)
                return;

            Rectangle rect =
                new Rectangle(
                    0,
                    0,
                    panel.Width - 1,
                    panel.Height - 1);

            using (GraphicsPath path =
                   GetRoundedRectPath(
                       rect,
                       16))
            {
                panel.Region =
                    new Region(path);
            }
        }

        private void StyleRoundedPanel(
            Panel panel,
            bool showAccent,
            Color? fillColor = null,
            int accentHeight = 44)
        {
            panel.BorderStyle =
                BorderStyle.None;

            Color actualFill =
                fillColor.HasValue
                ? fillColor.Value
                : CardColor;

            ApplyRoundedRegion(
                panel);

            panel.Resize +=
                (s, e) =>
                    ApplyRoundedRegion(
                        panel);

            panel.Paint +=
                (s, e) =>
                {
                    Graphics g =
                        e.Graphics;

                    g.SmoothingMode =
                        SmoothingMode.AntiAlias;

                    Rectangle rect =
                        new Rectangle(
                            0,
                            0,
                            panel.Width - 1,
                            panel.Height - 1);

                    using (GraphicsPath path =
                           GetRoundedRectPath(
                               rect,
                               16))
                    {
                        using (SolidBrush backBrush =
                               new SolidBrush(
                                   actualFill))
                        {
                            g.FillPath(
                                backBrush,
                                path);
                        }

                        using (Pen borderPen =
                               new Pen(
                                   BorderColor,
                                   1))
                        {
                            g.DrawPath(
                                borderPen,
                                path);
                        }
                    }

                    if (showAccent)
                    {
                        using (SolidBrush accentBrush =
                               new SolidBrush(
                                   MaroonColor))
                        {
                            Rectangle accentRect =
                                new Rectangle(
                                    0,
                                    0,
                                    5,
                                    Math.Min(
                                        accentHeight,
                                        panel.Height));

                            g.FillRectangle(
                                accentBrush,
                                accentRect);
                        }
                    }
                };
        }

        // =========================================================
        // SECTION HEADER
        // =========================================================

        private Panel CreateSectionHeader(
            string romanNumeral,
            string sectionTitle,
            string direction)
        {
            Panel header =
                new RoundedPanel();

            header.Width =
                contentPanel.Width - 10;

            header.Height =
                92;

            StyleRoundedPanel(
                header,
                true,
                MaroonSoft,
                header.Height);

            Label lblTitle =
                new Label();

            lblTitle.Text =
                romanNumeral +
                ".  " +
                sectionTitle;

            lblTitle.Font =
                new Font(
                    "Segoe UI",
                    16,
                    FontStyle.Bold);

            lblTitle.ForeColor =
                MaroonColor;

            lblTitle.AutoSize =
                true;

            lblTitle.Location =
                new Point(
                    28,
                    16);

            header.Controls.Add(
                lblTitle);

            Label lblDirection =
                new Label();

            lblDirection.Text =
                direction;

            lblDirection.Font =
                new Font(
                    "Segoe UI",
                    10.5F,
                    FontStyle.Italic);

            lblDirection.ForeColor =
                TextColor;

            lblDirection.AutoSize =
                false;

            lblDirection.Location =
                new Point(
                    28,
                    52);

            lblDirection.Size =
                new Size(
                    header.Width - 56,
                    32);

            header.Controls.Add(
                lblDirection);

            return header;
        }

        // =========================================================
        // RESIZE SECTION HEADER
        // =========================================================

        private void ResizeSectionHeader(
            Panel header)
        {
            foreach (Control control
                     in header.Controls)
            {
                if (control is Label &&
                    control.Location.Y >= 40)
                {
                    control.Width =
                        header.Width - 56;
                }
            }
        }

        // =========================================================
        // CREATE QUESTION CARD
        // =========================================================

        private Panel CreateQuestionCard(
            QuizQuestion q,
            int originalIndex,
            int displayNumber)
        {
            Panel card =
                new RoundedPanel();

            card.BackColor =
                CardColor;

            card.Width =
                contentPanel.Width - 10;

            string questionType =
                NormalizeQuestionType(
                    q.QuestionType);

            int height =
                250;

            if (questionType ==
                "multiple_choice")
            {
                height =
                    510;
            }
            else if (questionType ==
                     "true_false")
            {
                height =
                    350;
            }
            else if (questionType ==
                     "identification")
            {
                height =
                    330;
            }
            else if (questionType ==
                     "essay")
            {
                height =
                    420;
            }

            card.Height =
                height;

            StyleRoundedPanel(
                card,
                true);

            // =====================================================
            // QUESTION NUMBER
            // =====================================================

            Label numberLabel =
                new Label();

            numberLabel.Text =
                "QUESTION " +
                displayNumber;

            numberLabel.Font =
                new Font(
                    "Segoe UI",
                    10,
                    FontStyle.Bold);

            numberLabel.ForeColor =
                MaroonColor;

            numberLabel.AutoSize =
                true;

            numberLabel.Location =
                new Point(
                    28,
                    22);

            card.Controls.Add(
                numberLabel);

            // =====================================================
            // QUESTION TYPE
            // =====================================================

            Label typeLabel =
                new Label();

            if (questionType ==
                "multiple_choice")
            {
                typeLabel.Text =
                    "MULTIPLE CHOICE";
            }
            else if (questionType ==
                     "true_false")
            {
                typeLabel.Text =
                    "TRUE OR FALSE";
            }
            else if (questionType ==
                     "identification")
            {
                typeLabel.Text =
                    "IDENTIFICATION";
            }
            else if (questionType ==
                     "essay")
            {
                typeLabel.Text =
                    "ESSAY";
            }
            else
            {
                typeLabel.Text =
                    questionType.ToUpper();
            }

            typeLabel.Font =
                new Font(
                    "Segoe UI",
                    9,
                    FontStyle.Bold);

            typeLabel.ForeColor =
                MutedColor;

            typeLabel.AutoSize =
                true;

            typeLabel.Location =
                new Point(
                    28,
                    48);

            card.Controls.Add(
                typeLabel);

            // =====================================================
            // QUESTION TEXT
            // =====================================================

            Label questionLabel =
                new Label();

            questionLabel.Text =
                q.Question;

            questionLabel.Font =
                new Font(
                    "Segoe UI",
                    17,
                    FontStyle.Bold);

            questionLabel.ForeColor =
                DarkColor;

            questionLabel.Location =
                new Point(
                    28,
                    78);

            questionLabel.Size =
                new Size(
                    card.Width - 56,
                    82);

            questionLabel.AutoEllipsis =
                false;

            card.Controls.Add(
                questionLabel);

            // =====================================================
            // MULTIPLE CHOICE
            // =====================================================

            if (questionType ==
                "multiple_choice")
            {
                RadioButton[] radios =
                    new RadioButton[4];

                radios[0] =
                    CreateOption(
                        "A. " + q.ChoiceA,
                        28,
                        170);

                radios[1] =
                    CreateOption(
                        "B. " + q.ChoiceB,
                        28,
                        240);

                radios[2] =
                    CreateOption(
                        "C. " + q.ChoiceC,
                        28,
                        310);

                radios[3] =
                    CreateOption(
                        "D. " + q.ChoiceD,
                        28,
                        380);

                foreach (RadioButton rb
                         in radios)
                {
                    card.Controls.Add(
                        rb);
                }

                multipleChoiceControls[
                    originalIndex] =
                    radios;
            }

            // =====================================================
            // TRUE / FALSE
            // =====================================================

            else if (questionType ==
                     "true_false")
            {
                RadioButton[] radios =
                    new RadioButton[2];

                radios[0] =
                    CreateOption(
                        "True",
                        28,
                        175);

                radios[1] =
                    CreateOption(
                        "False",
                        28,
                        245);

                card.Controls.Add(
                    radios[0]);

                card.Controls.Add(
                    radios[1]);

                trueFalseControls[
                    originalIndex] =
                    radios;
            }

            // =====================================================
            // IDENTIFICATION
            // =====================================================

            else if (questionType ==
                     "identification")
            {
                Label answerLabel =
                    new Label();

                answerLabel.Text =
                    "Your Answer:";

                answerLabel.Font =
                    new Font(
                        "Segoe UI",
                        10,
                        FontStyle.Bold);

                answerLabel.ForeColor =
                    TextColor;

                answerLabel.AutoSize =
                    true;

                answerLabel.Location =
                    new Point(
                        28,
                        170);

                card.Controls.Add(
                    answerLabel);

                TextBox identification =
                    new TextBox();

                identification.Font =
                    new Font(
                        "Segoe UI",
                        14);

                identification.ForeColor =
                    TextColor;

                identification.BackColor =
                    OptionBackColor;

                identification.BorderStyle =
                    BorderStyle.FixedSingle;

                identification.Location =
                    new Point(
                        28,
                        198);

                identification.Size =
                    new Size(
                        card.Width - 56,
                        45);

                identification.MaxLength =
                    500;

                identification.Multiline =
                    false;

                card.Controls.Add(
                    identification);

                identificationControls[
                    originalIndex] =
                    identification;

                BlockTextEditingShortcuts(
                    identification);

                identification.TextChanged +=
                    Identification_TextChanged;
            }

            // =====================================================
            // ESSAY
            // =====================================================

            else if (questionType ==
                     "essay")
            {
                TextBox essay =
                    new TextBox();

                essay.Multiline =
                    true;

                essay.ScrollBars =
                    ScrollBars.Vertical;

                essay.Font =
                    new Font(
                        "Segoe UI",
                        13);

                essay.ForeColor =
                    TextColor;

                essay.BackColor =
                    OptionBackColor;

                essay.BorderStyle =
                    BorderStyle.FixedSingle;

                essay.Location =
                    new Point(
                        28,
                        175);

                essay.Size =
                    new Size(
                        card.Width - 56,
                        195);

                essay.MaxLength =
                    5000;

                card.Controls.Add(
                    essay);

                essayControls[
                    originalIndex] =
                    essay;

                BlockTextEditingShortcuts(
                    essay);
            }

            return card;
        }

        // =========================================================
        // CREATE OPTION
        // =========================================================

        private RadioButton CreateOption(
            string text,
            int x,
            int y)
        {
            RadioButton rb =
                new RadioButton();

            rb.Text =
                text;

            rb.Font =
                new Font(
                    "Segoe UI",
                    13);

            rb.ForeColor =
                TextColor;

            rb.BackColor =
                OptionBackColor;

            rb.Location =
                new Point(
                    x,
                    y);

            rb.Size =
                new Size(
                    contentPanel.Width - 66,
                    55);

            rb.AutoSize =
                false;

            rb.Padding =
                new Padding(
                    14,
                    0,
                    8,
                    0);

            rb.Cursor =
                Cursors.Hand;

            rb.FlatStyle =
                FlatStyle.Standard;

            rb.CheckedChanged +=
                Option_CheckedChanged;

            BlockTextEditingShortcuts(
                rb);

            return rb;
        }

        // =========================================================
        // OPTION SELECTED
        // =========================================================

        private void Option_CheckedChanged(
            object sender,
            EventArgs e)
        {
            RadioButton rb =
                sender as RadioButton;

            if (rb == null)
                return;

            if (rb.Checked)
            {
                rb.BackColor =
                    MaroonSoft;

                rb.ForeColor =
                    MaroonColor;

                rb.Font =
                    new Font(
                        rb.Font,
                        FontStyle.Bold);
            }
            else
            {
                rb.BackColor =
                    OptionBackColor;

                rb.ForeColor =
                    TextColor;

                rb.Font =
                    new Font(
                        rb.Font.FontFamily,
                        rb.Font.Size,
                        FontStyle.Regular);
            }

            UpdateProgress();
        }

        // =========================================================
        // IDENTIFICATION TEXT CHANGED
        // =========================================================

        private void Identification_TextChanged(
            object sender,
            EventArgs e)
        {
            UpdateProgress();
        }

        // =========================================================
        // RESIZE QUESTION CARD
        // =========================================================

        private void ResizeQuestionCard(
            Panel card)
        {
            foreach (Control control
                     in card.Controls)
            {
                if (control is RadioButton)
                {
                    control.Width =
                        card.Width - 66;
                }
                else if (control is TextBox)
                {
                    control.Width =
                        card.Width - 56;
                }
                else if (control is Label)
                {
                    Label label =
                        control as Label;

                    if (label.Location.Y >= 70)
                    {
                        label.Width =
                            card.Width - 56;
                    }
                }
            }
        }

        // =========================================================
        // CHECK ALL ANSWERS
        // =========================================================

        private bool ValidateAllAnswers()
        {
            for (int i = 0;
                 i < questions.Count;
                 i++)
            {
                QuizQuestion q =
                    questions[i];

                string type =
                    NormalizeQuestionType(
                        q.QuestionType);

                bool answered =
                    false;

                if (type ==
                    "multiple_choice")
                {
                    RadioButton[] radios =
                        multipleChoiceControls[i];

                    if (radios != null)
                    {
                        foreach (RadioButton rb
                                 in radios)
                        {
                            if (rb.Checked)
                            {
                                answered =
                                    true;

                                break;
                            }
                        }
                    }
                }
                else if (type ==
                         "true_false")
                {
                    RadioButton[] radios =
                        trueFalseControls[i];

                    if (radios != null)
                    {
                        answered =
                            radios[0].Checked ||
                            radios[1].Checked;
                    }
                }
                else if (type ==
                         "identification")
                {
                    TextBox identification =
                        identificationControls[i];

                    if (identification != null)
                    {
                        answered =
                            !string.IsNullOrWhiteSpace(
                                identification.Text);
                    }
                }
                else if (type ==
                         "essay")
                {
                    TextBox essay =
                        essayControls[i];

                    if (essay != null)
                    {
                        answered =
                            !string.IsNullOrWhiteSpace(
                                essay.Text);
                    }
                }

                if (!answered)
                {
                    MessageBox.Show(
                        "Please answer Question " +
                        (i + 1) +
                        " before submitting.",
                        "Unanswered Question",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    ScrollToQuestion(i);

                    return false;
                }
            }

            return true;
        }

        // =========================================================
        // SAVE ALL ANSWERS
        // =========================================================

        private void SaveAllAnswers()
        {
            studentAnswers.Clear();

            for (int i = 0;
                 i < questions.Count;
                 i++)
            {
                QuizQuestion q =
                    questions[i];

                string type =
                    NormalizeQuestionType(
                        q.QuestionType);

                string answer =
                    "";

                if (type ==
                    "multiple_choice")
                {
                    RadioButton[] radios =
                        multipleChoiceControls[i];

                    if (radios != null)
                    {
                        if (radios[0].Checked)
                            answer = "A";

                        else if (radios[1].Checked)
                            answer = "B";

                        else if (radios[2].Checked)
                            answer = "C";

                        else if (radios[3].Checked)
                            answer = "D";
                    }
                }
                else if (type ==
                         "true_false")
                {
                    RadioButton[] radios =
                        trueFalseControls[i];

                    if (radios != null)
                    {
                        if (radios[0].Checked)
                            answer = "TRUE";

                        else if (radios[1].Checked)
                            answer = "FALSE";
                    }
                }
                else if (type ==
                         "identification")
                {
                    TextBox identification =
                        identificationControls[i];

                    if (identification != null)
                    {
                        answer =
                            identification.Text.Trim();
                    }
                }
                else if (type ==
                         "essay")
                {
                    TextBox essay =
                        essayControls[i];

                    if (essay != null)
                    {
                        answer =
                            essay.Text.Trim();
                    }
                }

                studentAnswers[i] =
                    answer;
            }
        }

        // =========================================================
        // SUBMIT
        // =========================================================

        private void BtnSubmit_Click(
            object sender,
            EventArgs e)
        {
            if (!ValidateAllAnswers())
                return;

            SaveAllAnswers();

            DialogResult confirm =
                MessageBox.Show(
                    "Are you sure you want to submit the examination?\n\n" +
                    "You will not be able to change your answers after submission.",
                    "Submit Examination",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

            if (confirm !=
                DialogResult.Yes)
                return;

            SaveQuizResult();
        }

        // =========================================================
        // CALCULATE SCORE
        // =========================================================

        private void SaveQuizResult()
        {
            score = 0;

            for (int i = 0;
                 i < questions.Count;
                 i++)
            {
                QuizQuestion q =
                    questions[i];

                string studentAnswer =
                    "";

                if (studentAnswers.ContainsKey(
                    i))
                {
                    studentAnswer =
                        studentAnswers[i];
                }

                string type =
                    NormalizeQuestionType(
                        q.QuestionType);

                // Essay requires manual checking
                if (type ==
                    "essay")
                    continue;

                if (!string.IsNullOrWhiteSpace(
                    studentAnswer) &&
                    !string.IsNullOrWhiteSpace(
                        q.CorrectAnswer))
                {
                    string studentValue =
                        studentAnswer.Trim()
                        .ToUpper();

                    string correctValue =
                        q.CorrectAnswer.Trim()
                        .ToUpper();

                    if (studentValue ==
                        correctValue)
                    {
                        score++;
                    }
                }
            }

            SaveAttemptToDatabase(
                score,
                questions.Count);
        }

        // =========================================================
        // SAVE RESULT TO DATABASE
        // =========================================================

        private void SaveAttemptToDatabase(
            int finalScore,
            int totalQuestions)
        {
            decimal percentage =
                0;

            if (totalQuestions > 0)
            {
                percentage =
                    ((decimal)finalScore /
                    totalQuestions) *
                    100;
            }

            using (MySqlConnection connection =
                   DatabaseConnection.GetConnection())
            {
                MySqlTransaction transaction =
                    null;

                try
                {
                    connection.Open();

                    transaction =
                        connection.BeginTransaction();

                    string attemptQuery = @"
                        INSERT INTO quiz_attempts
                        (
                            quiz_id,
                            user_id,
                            score,
                            total_questions,
                            percentage
                        )
                        VALUES
                        (
                            @quiz_id,
                            @user_id,
                            @score,
                            @total_questions,
                            @percentage
                        );";

                    int attemptId;

                    using (MySqlCommand command =
                           new MySqlCommand(
                               attemptQuery,
                               connection,
                               transaction))
                    {
                        command.Parameters.AddWithValue(
                            "@quiz_id",
                            selectedQuizId);

                        command.Parameters.AddWithValue(
                            "@user_id",
                            studentUserId);

                        command.Parameters.AddWithValue(
                            "@score",
                            finalScore);

                        command.Parameters.AddWithValue(
                            "@total_questions",
                            totalQuestions);

                        command.Parameters.AddWithValue(
                            "@percentage",
                            percentage);

                        command.ExecuteNonQuery();

                        attemptId =
                            Convert.ToInt32(
                                command.LastInsertedId);
                    }

                    for (int i = 0;
                         i < questions.Count;
                         i++)
                    {
                        QuizQuestion q =
                            questions[i];

                        string answer =
                            "";

                        if (studentAnswers.ContainsKey(
                            i))
                        {
                            answer =
                                studentAnswers[i];
                        }

                        string type =
                            NormalizeQuestionType(
                                q.QuestionType);

                        bool isCorrect =
                            false;

                        if (type !=
                            "essay" &&
                            !string.IsNullOrWhiteSpace(
                                answer) &&
                            !string.IsNullOrWhiteSpace(
                                q.CorrectAnswer))
                        {
                            string studentValue =
                                answer.Trim()
                                .ToUpper();

                            string correctValue =
                                q.CorrectAnswer.Trim()
                                .ToUpper();

                            if (studentValue ==
                                correctValue)
                            {
                                isCorrect =
                                    true;
                            }
                        }

                        int questionId =
                            GetQuestionId(
                                connection,
                                transaction,
                                i);

                        string answerQuery = @"
                            INSERT INTO student_answers
                            (
                                attempt_id,
                                question_id,
                                student_answer,
                                is_correct
                            )
                            VALUES
                            (
                                @attempt_id,
                                @question_id,
                                @student_answer,
                                @is_correct
                            );";

                        using (MySqlCommand answerCommand =
                               new MySqlCommand(
                                   answerQuery,
                                   connection,
                                   transaction))
                        {
                            answerCommand.Parameters.AddWithValue(
                                "@attempt_id",
                                attemptId);

                            answerCommand.Parameters.AddWithValue(
                                "@question_id",
                                questionId);

                            if (string.IsNullOrWhiteSpace(
                                answer))
                            {
                                answerCommand.Parameters.AddWithValue(
                                    "@student_answer",
                                    DBNull.Value);
                            }
                            else
                            {
                                answerCommand.Parameters.AddWithValue(
                                    "@student_answer",
                                    answer);
                            }

                            answerCommand.Parameters.AddWithValue(
                                "@is_correct",
                                isCorrect);

                            answerCommand.ExecuteNonQuery();
                        }
                    }

                    transaction.Commit();

                    MessageBox.Show(
                        "Examination submitted successfully!\n\n" +
                        "Score: " +
                        finalScore +
                        " / " +
                        totalQuestions +
                        "\nPercentage: " +
                        percentage.ToString("0.00") +
                        "%",
                        "Examination Submitted",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);

                    DisableQuiz();
                }
                catch (Exception ex)
                {
                    try
                    {
                        if (transaction != null)
                            transaction.Rollback();
                    }
                    catch
                    {
                    }

                    MessageBox.Show(
                        "Your examination was completed, but the result could not be saved.\n\n" +
                        "Error:\n" +
                        ex.Message,
                        "Database Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }

        // =========================================================
        // GET QUESTION ID
        // =========================================================

        private int GetQuestionId(
            MySqlConnection connection,
            MySqlTransaction transaction,
            int index)
        {
            string query = @"
                SELECT question_id
                FROM questions
                WHERE quiz_id = @quiz_id
                ORDER BY question_id ASC
                LIMIT @index, 1;";

            using (MySqlCommand command =
                   new MySqlCommand(
                       query,
                       connection,
                       transaction))
            {
                command.Parameters.AddWithValue(
                    "@quiz_id",
                    selectedQuizId);

                command.Parameters.AddWithValue(
                    "@index",
                    index);

                object result =
                    command.ExecuteScalar();

                return Convert.ToInt32(
                    result);
            }
        }

        // =========================================================
        // SCROLL TO QUESTION
        // =========================================================

        private void ScrollToQuestion(
            int index)
        {
            if (index < 0 ||
                index >= questionCards.Count)
                return;

            Panel card =
                questionCards[index];

            if (card != null)
            {
                scrollPanel.ScrollControlIntoView(
                    card);
            }
        }

        // =========================================================
        // UPDATE PROGRESS
        // =========================================================

        private void UpdateProgress()
        {
            if (questions.Count == 0)
            {
                progressBar.Value =
                    0;

                return;
            }

            int answered =
                0;

            for (int i = 0;
                 i < questions.Count;
                 i++)
            {
                if (IsQuestionAnswered(i))
                    answered++;
            }

            int percentage =
                (int)(
                    ((double)answered /
                    questions.Count) *
                    100);

            if (percentage < 0)
                percentage = 0;

            if (percentage > 100)
                percentage = 100;

            progressBar.Value =
                percentage;
        }

        // =========================================================
        // CHECK QUESTION ANSWER
        // =========================================================

        private bool IsQuestionAnswered(
            int index)
        {
            if (index < 0 ||
                index >= questions.Count)
                return false;

            QuizQuestion q =
                questions[index];

            string type =
                NormalizeQuestionType(
                    q.QuestionType);

            if (type ==
                "multiple_choice")
            {
                RadioButton[] radios =
                    multipleChoiceControls[index];

                if (radios == null)
                    return false;

                foreach (RadioButton rb
                         in radios)
                {
                    if (rb.Checked)
                        return true;
                }

                return false;
            }

            if (type ==
                "true_false")
            {
                RadioButton[] radios =
                    trueFalseControls[index];

                if (radios == null)
                    return false;

                return
                    radios[0].Checked ||
                    radios[1].Checked;
            }

            if (type ==
                "identification")
            {
                TextBox identification =
                    identificationControls[index];

                if (identification == null)
                    return false;

                return
                    !string.IsNullOrWhiteSpace(
                        identification.Text);
            }

            if (type ==
                "essay")
            {
                TextBox essay =
                    essayControls[index];

                if (essay == null)
                    return false;

                return
                    !string.IsNullOrWhiteSpace(
                        essay.Text);
            }

            return false;
        }

        // =========================================================
        // ANTI COPY / PASTE
        // =========================================================

        private void EnableAntiCheat()
        {
            this.KeyDown +=
                StudentQuizForm_KeyDown;

            this.MouseDown +=
                StudentQuizForm_MouseDown;

            ApplyAntiCheatToControlTree(
                this);
        }

        // =========================================================
        // GLOBAL KEYBOARD BLOCK
        // =========================================================

        private void StudentQuizForm_KeyDown(
            object sender,
            KeyEventArgs e)
        {
            if (e.Control &&
                (
                    e.KeyCode == Keys.C ||
                    e.KeyCode == Keys.X ||
                    e.KeyCode == Keys.V ||
                    e.KeyCode == Keys.A ||
                    e.KeyCode == Keys.Z
                ))
            {
                e.SuppressKeyPress =
                    true;

                e.Handled =
                    true;

                return;
            }

            if (e.Shift &&
                e.KeyCode ==
                Keys.Insert)
            {
                e.SuppressKeyPress =
                    true;

                e.Handled =
                    true;
            }
        }

        // =========================================================
        // BLOCK RIGHT CLICK
        // =========================================================

        private void StudentQuizForm_MouseDown(
            object sender,
            MouseEventArgs e)
        {
            if (e.Button ==
                MouseButtons.Right)
            {
                // Right-click disabled.
            }
        }

        // =========================================================
        // APPLY ANTI CHEAT
        // =========================================================

        private void ApplyAntiCheatToControlTree(
            Control parent)
        {
            foreach (Control control
                     in parent.Controls)
            {
                control.ContextMenuStrip =
                    null;

                control.MouseDown +=
                    AntiCheat_MouseDown;

                if (control is Label)
                {
                    Label label =
                        control as Label;

                    label.Cursor =
                        Cursors.Default;
                }

                if (control.HasChildren)
                {
                    ApplyAntiCheatToControlTree(
                        control);
                }
            }
        }

        // =========================================================
        // BLOCK RIGHT CLICK
        // =========================================================

        private void AntiCheat_MouseDown(
            object sender,
            MouseEventArgs e)
        {
            if (e.Button ==
                MouseButtons.Right)
            {
                Control control =
                    sender as Control;

                if (control != null)
                {
                    control.ContextMenuStrip =
                        null;
                }
            }
        }

        // =========================================================
        // BLOCK TEXT EDITING SHORTCUTS
        // =========================================================

        private void BlockTextEditingShortcuts(
            Control control)
        {
            control.KeyDown +=
                TextControl_KeyDown;

            control.MouseDown +=
                TextControl_MouseDown;

            control.ContextMenuStrip =
                null;
        }

        // =========================================================
        // TEXT CONTROL KEY BLOCK
        // =========================================================

        private void TextControl_KeyDown(
            object sender,
            KeyEventArgs e)
        {
            if (e.Control &&
                (
                    e.KeyCode == Keys.C ||
                    e.KeyCode == Keys.X ||
                    e.KeyCode == Keys.V ||
                    e.KeyCode == Keys.A
                ))
            {
                e.SuppressKeyPress =
                    true;

                e.Handled =
                    true;

                return;
            }

            if (e.Shift &&
                e.KeyCode ==
                Keys.Insert)
            {
                e.SuppressKeyPress =
                    true;

                e.Handled =
                    true;
            }
        }

        // =========================================================
        // TEXT CONTROL MOUSE
        // =========================================================

        private void TextControl_MouseDown(
            object sender,
            MouseEventArgs e)
        {
            if (e.Button ==
                MouseButtons.Right)
            {
                TextBox textBox =
                    sender as TextBox;

                if (textBox != null)
                {
                    textBox.ContextMenuStrip =
                        null;
                }
            }
        }

        // =========================================================
        // DISABLE QUIZ
        // =========================================================

        private void DisableQuiz()
        {
            if (btnSubmit != null)
                btnSubmit.Enabled =
                    false;

            if (scrollPanel != null)
                scrollPanel.Enabled =
                    false;

            if (lblInstruction != null)
            {
                lblInstruction.Text =
                    "This examination is no longer available.";
            }
        }
    }
}