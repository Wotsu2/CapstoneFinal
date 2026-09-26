using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace WinFormsApp1
{
    public class ProfessorQuizForm : Form
    {
        private int professorUserId;

        private ComboBox cmbAssessmentType;
        private ComboBox cmbExamPeriod;

        private TextBox txtQuizTitle;
        private TextBox txtSubject;

        // =========================================================
        // TIME LIMIT
        // =========================================================

        private NumericUpDown numDurationMinutes;

        private RoundedButton btnImportDocx;
        private RoundedButton btnSaveQuiz;
        private RoundedButton btnClear;
        private RoundedButton btnMonitor;

        private Label lblFileName;
        private Label lblQuestionCount;
        private Label lblMonitoringTitle;
        private Label lblMonitoringQuiz;

        private ListBox lstQuestions;

        // =========================================================
        // PROFESSOR MONITORING
        // =========================================================

        private DataGridView dgvAttempts;

        private System.Windows.Forms.Timer monitoringTimer;

        private int monitoredQuizId = 0;

        private List<QuizQuestion> importedQuestions =
            new List<QuizQuestion>();

        // ---- Colors ----
        private static readonly Color ClrMaroon =
            Color.FromArgb(94, 14, 33);

        private static readonly Color ClrMaroonDark =
            Color.FromArgb(70, 10, 24);

        private static readonly Color ClrBlack =
            Color.FromArgb(20, 20, 20);

        private static readonly Color ClrBlackHover =
            Color.FromArgb(50, 50, 50);

        private static readonly Color ClrLabelGray =
            Color.FromArgb(50, 50, 50);

        private static readonly Color ClrGreen =
            Color.FromArgb(22, 163, 74);

        private static readonly Color ClrYellow =
            Color.FromArgb(202, 138, 4);

        private static readonly Color ClrRed =
            Color.FromArgb(220, 38, 38);

        public ProfessorQuizForm(int professorID)
        {
            professorUserId = professorID;

            BuildProfessorInterface();

            this.FormClosed += ProfessorQuizForm_FormClosed;
        }

        // =========================================================
        // BUILD INTERFACE
        // =========================================================

        private void BuildProfessorInterface()
        {
            Text = "Professor - Create Quiz / Exam";
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(1200, 760);
            MinimumSize = new Size(1100, 700);
            BackColor = Color.White;
            FormBorderStyle = FormBorderStyle.None;
            Font = new Font("Segoe UI", 9.5F);

            this.Paint += (s, e) =>
            {
                using (var pen =
                       new Pen(
                           Color.FromArgb(120, 170, 230),
                           1.5f))
                {
                    e.Graphics.DrawRectangle(
                        pen,
                        0,
                        0,
                        this.Width - 1,
                        this.Height - 1);
                }
            };

            // =====================================================
            // MAIN LAYOUT
            // =====================================================

            int pad = 30;

            int leftWidth = 690;
            int rightWidth = 410;

            int rightX =
                pad +
                leftWidth +
                25;

            // =====================================================
            // HEADER
            // =====================================================

            Label lblHeader = new Label();

            lblHeader.Text =
                "Create Quiz / Exam";

            lblHeader.Font =
                new Font(
                    "Segoe UI Semibold",
                    20,
                    FontStyle.Bold);

            lblHeader.ForeColor =
                ClrBlack;

            lblHeader.AutoSize = true;

            lblHeader.Location =
                new Point(
                    pad,
                    25);

            Controls.Add(lblHeader);

            Label lblClose = new Label();

            lblClose.Text = "✕";

            lblClose.Font =
                new Font(
                    "Segoe UI",
                    14);

            lblClose.ForeColor =
                ClrBlack;

            lblClose.AutoSize = true;

            lblClose.Cursor =
                Cursors.Hand;

            lblClose.Location =
                new Point(
                    this.ClientSize.Width - pad - 16,
                    28);

            lblClose.Click +=
                (s, e) => this.Close();

            Controls.Add(lblClose);

            // =====================================================
            // LEFT SIDE - QUIZ CREATION
            // =====================================================

            Label lblTitle = new Label();

            lblTitle.Text = "Title";

            lblTitle.Font =
                new Font(
                    "Segoe UI Semibold",
                    10,
                    FontStyle.Bold);

            lblTitle.ForeColor =
                ClrLabelGray;

            lblTitle.AutoSize = true;

            lblTitle.Location =
                new Point(
                    pad,
                    85);

            Controls.Add(lblTitle);

            txtQuizTitle = new TextBox();

            txtQuizTitle.Font =
                new Font(
                    "Segoe UI",
                    10);

            txtQuizTitle.BorderStyle =
                BorderStyle.FixedSingle;

            txtQuizTitle.Location =
                new Point(
                    pad,
                    108);

            txtQuizTitle.Size =
                new Size(
                    leftWidth,
                    30);

            Controls.Add(txtQuizTitle);

            // =====================================================
            // ASSESSMENT TYPE / EXAM PERIOD
            // =====================================================

            int halfWidth =
                (leftWidth - 24) / 2;

            Label lblType = new Label();

            lblType.Text =
                "Assessment Type";

            lblType.Font =
                new Font(
                    "Segoe UI Semibold",
                    10,
                    FontStyle.Bold);

            lblType.ForeColor =
                ClrLabelGray;

            lblType.AutoSize = true;

            lblType.Location =
                new Point(
                    pad,
                    155);

            Controls.Add(lblType);

            Label lblExamPeriod = new Label();

            lblExamPeriod.Text =
                "Exam Period";

            lblExamPeriod.Font =
                new Font(
                    "Segoe UI Semibold",
                    10,
                    FontStyle.Bold);

            lblExamPeriod.ForeColor =
                ClrLabelGray;

            lblExamPeriod.AutoSize = true;

            lblExamPeriod.Location =
                new Point(
                    pad +
                    halfWidth +
                    24,
                    155);

            Controls.Add(lblExamPeriod);

            cmbAssessmentType =
                new ComboBox();

            cmbAssessmentType.DropDownStyle =
                ComboBoxStyle.DropDownList;

            cmbAssessmentType.Items.Add("Quiz");
            cmbAssessmentType.Items.Add("Exam");

            cmbAssessmentType.SelectedIndex =
                0;

            cmbAssessmentType.Font =
                new Font(
                    "Segoe UI",
                    10);

            cmbAssessmentType.Location =
                new Point(
                    pad,
                    178);

            cmbAssessmentType.Size =
                new Size(
                    halfWidth,
                    30);

            Controls.Add(cmbAssessmentType);

            cmbExamPeriod =
                new ComboBox();

            cmbExamPeriod.DropDownStyle =
                ComboBoxStyle.DropDownList;

            cmbExamPeriod.Items.Add("PRELIM");
            cmbExamPeriod.Items.Add("MIDTERM");
            cmbExamPeriod.Items.Add("SEMIFINALS");
            cmbExamPeriod.Items.Add("FINALS");

            cmbExamPeriod.SelectedIndex =
                0;

            cmbExamPeriod.Font =
                new Font(
                    "Segoe UI",
                    10);

            cmbExamPeriod.Location =
                new Point(
                    pad +
                    halfWidth +
                    24,
                    178);

            cmbExamPeriod.Size =
                new Size(
                    halfWidth,
                    30);

            Controls.Add(cmbExamPeriod);

            // =====================================================
            // SUBJECT / TIME LIMIT
            // =====================================================

            Label lblSubject = new Label();

            lblSubject.Text =
                "Subject";

            lblSubject.Font =
                new Font(
                    "Segoe UI Semibold",
                    10,
                    FontStyle.Bold);

            lblSubject.ForeColor =
                ClrLabelGray;

            lblSubject.AutoSize = true;

            lblSubject.Location =
                new Point(
                    pad,
                    225);

            Controls.Add(lblSubject);

            Label lblDuration = new Label();

            lblDuration.Text =
                "Time Limit (minutes)";

            lblDuration.Font =
                new Font(
                    "Segoe UI Semibold",
                    10,
                    FontStyle.Bold);

            lblDuration.ForeColor =
                ClrLabelGray;

            lblDuration.AutoSize = true;

            lblDuration.Location =
                new Point(
                    pad +
                    halfWidth +
                    24,
                    225);

            Controls.Add(lblDuration);

            txtSubject =
                new TextBox();

            txtSubject.Font =
                new Font(
                    "Segoe UI",
                    10);

            txtSubject.BorderStyle =
                BorderStyle.FixedSingle;

            txtSubject.Location =
                new Point(
                    pad,
                    248);

            txtSubject.Size =
                new Size(
                    halfWidth,
                    30);

            Controls.Add(txtSubject);

            // =====================================================
            // TIME LIMIT INPUT
            // =====================================================

            numDurationMinutes =
                new NumericUpDown();

            numDurationMinutes.Font =
                new Font(
                    "Segoe UI",
                    10);

            numDurationMinutes.Location =
                new Point(
                    pad +
                    halfWidth +
                    24,
                    248);

            numDurationMinutes.Size =
                new Size(
                    halfWidth,
                    30);

            // Minimum: 1 minute
            numDurationMinutes.Minimum = 1;

            // Maximum: 24 hours
            numDurationMinutes.Maximum = 1440;

            // Default: 60 minutes
            numDurationMinutes.Value = 60;

            numDurationMinutes.Increment = 5;

            numDurationMinutes.TextAlign =
                HorizontalAlignment.Left;

            Controls.Add(numDurationMinutes);

            // =====================================================
            // IMPORT DOCX
            // =====================================================

            btnImportDocx =
                new RoundedButton();

            btnImportDocx.Text =
                "⬆  IMPORT DOCX";

            btnImportDocx.Font =
                new Font(
                    "Segoe UI Semibold",
                    9.5F,
                    FontStyle.Bold);

            btnImportDocx.Size =
                new Size(
                    190,
                    40);

            btnImportDocx.Location =
                new Point(
                    pad,
                    300);

            btnImportDocx.BackColor =
                ClrMaroon;

            btnImportDocx.HoverColor =
                ClrMaroonDark;

            btnImportDocx.ForeColor =
                Color.White;

            btnImportDocx.Click +=
                BtnImportDocx_Click;

            Controls.Add(btnImportDocx);

            lblFileName =
                new Label();

            lblFileName.Text =
                "No DOCX file selected.";

            lblFileName.Font =
                new Font(
                    "Segoe UI Italic",
                    9F,
                    FontStyle.Italic);

            lblFileName.ForeColor =
                Color.FromArgb(
                    120,
                    115,
                    110);

            lblFileName.AutoSize = true;

            lblFileName.Location =
                new Point(
                    pad + 210,
                    312);

            Controls.Add(lblFileName);

            lblQuestionCount =
                new Label();

            lblQuestionCount.Text =
                "Questions: 0";

            lblQuestionCount.Font =
                new Font(
                    "Segoe UI Semibold",
                    10,
                    FontStyle.Bold);

            lblQuestionCount.ForeColor =
                ClrMaroon;

            lblQuestionCount.AutoSize = true;

            lblQuestionCount.Location =
                new Point(
                    pad +
                    leftWidth -
                    110,
                    312);

            Controls.Add(lblQuestionCount);

            // =====================================================
            // PREVIEW TITLE
            // =====================================================

            Label lblPreview =
                new Label();

            lblPreview.Text =
                "Imported Questions / Exam Structure";

            lblPreview.Font =
                new Font(
                    "Segoe UI Semibold",
                    10.5F,
                    FontStyle.Bold);

            lblPreview.ForeColor =
                ClrLabelGray;

            lblPreview.AutoSize = true;

            lblPreview.Location =
                new Point(
                    pad,
                    358);

            Controls.Add(lblPreview);

            // =====================================================
            // QUESTION LIST
            // =====================================================

            lstQuestions =
                new ListBox();

            lstQuestions.Font =
                new Font(
                    "Segoe UI",
                    9.5F);

            lstQuestions.HorizontalScrollbar =
                true;

            lstQuestions.BorderStyle =
                BorderStyle.FixedSingle;

            lstQuestions.Location =
                new Point(
                    pad,
                    385);

            lstQuestions.Size =
                new Size(
                    leftWidth,
                    270);

            Controls.Add(lstQuestions);

            // =====================================================
            // SAVE / CLEAR
            // =====================================================

            btnSaveQuiz =
                new RoundedButton();

            btnSaveQuiz.Text =
                "✓  SUBMIT";

            btnSaveQuiz.Font =
                new Font(
                    "Segoe UI Semibold",
                    10,
                    FontStyle.Bold);

            btnSaveQuiz.Size =
                new Size(
                    150,
                    42);

            btnSaveQuiz.Location =
                new Point(
                    pad +
                    leftWidth -
                    480,
                    675);

            btnSaveQuiz.BackColor =
                ClrMaroon;

            btnSaveQuiz.HoverColor =
                ClrMaroonDark;

            btnSaveQuiz.ForeColor =
                Color.White;

            btnSaveQuiz.Click +=
                BtnSaveQuiz_Click;

            Controls.Add(btnSaveQuiz);

            btnMonitor =
                new RoundedButton();

            btnMonitor.Text =
                "◉  MONITOR";

            btnMonitor.Font =
                new Font(
                    "Segoe UI Semibold",
                    10,
                    FontStyle.Bold);

            btnMonitor.Size =
                new Size(
                    150,
                    42);

            btnMonitor.Location =
                new Point(
                    pad +
                    leftWidth -
                    315,
                    675);

            btnMonitor.BackColor =
                Color.FromArgb(
                    37,
                    99,
                    235);

            btnMonitor.HoverColor =
                Color.FromArgb(
                    29,
                    78,
                    216);

            btnMonitor.ForeColor =
                Color.White;

            btnMonitor.Enabled =
                false;

            btnMonitor.Click +=
                BtnMonitor_Click;

            Controls.Add(btnMonitor);

            btnClear =
                new RoundedButton();

            btnClear.Text =
                "CLEAR";

            btnClear.Font =
                new Font(
                    "Segoe UI Semibold",
                    10,
                    FontStyle.Bold);

            btnClear.Size =
                new Size(
                    130,
                    42);

            btnClear.Location =
                new Point(
                    pad +
                    leftWidth -
                    150,
                    675);

            btnClear.BackColor =
                ClrBlack;

            btnClear.HoverColor =
                ClrBlackHover;

            btnClear.ForeColor =
                Color.White;

            btnClear.Click +=
                BtnClear_Click;

            Controls.Add(btnClear);

            // =====================================================
            // RIGHT SIDE - MONITORING DASHBOARD
            // =====================================================

            Panel monitoringPanel =
                new Panel();

            monitoringPanel.Location =
                new Point(
                    rightX,
                    85);

            monitoringPanel.Size =
                new Size(
                    rightWidth,
                    632);

            monitoringPanel.BorderStyle =
                BorderStyle.FixedSingle;

            monitoringPanel.BackColor =
                Color.White;

            Controls.Add(monitoringPanel);

            lblMonitoringTitle =
                new Label();

            lblMonitoringTitle.Text =
                "Student Monitoring";

            lblMonitoringTitle.Font =
                new Font(
                    "Segoe UI Semibold",
                    15,
                    FontStyle.Bold);

            lblMonitoringTitle.ForeColor =
                ClrBlack;

            lblMonitoringTitle.AutoSize = true;

            lblMonitoringTitle.Location =
                new Point(
                    20,
                    18);

            monitoringPanel.Controls.Add(
                lblMonitoringTitle);

            lblMonitoringQuiz =
                new Label();

            lblMonitoringQuiz.Text =
                "No quiz selected.";

            lblMonitoringQuiz.Font =
                new Font(
                    "Segoe UI",
                    9.5F);

            lblMonitoringQuiz.ForeColor =
                Color.FromArgb(
                    100,
                    116,
                    139);

            lblMonitoringQuiz.AutoSize =
                false;

            lblMonitoringQuiz.Size =
                new Size(
                    rightWidth - 40,
                    42);

            lblMonitoringQuiz.Location =
                new Point(
                    20,
                    52);

            monitoringPanel.Controls.Add(
                lblMonitoringQuiz);

            // =====================================================
            // STATUS LEGEND
            // =====================================================

            Label lblLegend =
                new Label();

            lblLegend.Text =
                "🟢 Taking Quiz    🟡 Disconnected    ✓ Submitted";

            lblLegend.Font =
                new Font(
                    "Segoe UI",
                    8.5F);

            lblLegend.ForeColor =
                ClrLabelGray;

            lblLegend.AutoSize =
                false;

            lblLegend.Size =
                new Size(
                    rightWidth - 40,
                    30);

            lblLegend.Location =
                new Point(
                    20,
                    94);

            monitoringPanel.Controls.Add(
                lblLegend);

            // =====================================================
            // DATAGRIDVIEW
            // =====================================================

            dgvAttempts =
                new DataGridView();

            dgvAttempts.Location =
                new Point(
                    20,
                    130);

            dgvAttempts.Size =
                new Size(
                    rightWidth - 40,
                    475);

            dgvAttempts.AllowUserToAddRows =
                false;

            dgvAttempts.AllowUserToDeleteRows =
                false;

            dgvAttempts.AllowUserToResizeRows =
                false;

            dgvAttempts.ReadOnly =
                true;

            dgvAttempts.MultiSelect =
                false;

            dgvAttempts.SelectionMode =
                DataGridViewSelectionMode.FullRowSelect;

            dgvAttempts.AutoGenerateColumns =
                false;

            dgvAttempts.RowHeadersVisible =
                false;

            dgvAttempts.BackgroundColor =
                Color.White;

            dgvAttempts.BorderStyle =
                BorderStyle.FixedSingle;

            dgvAttempts.AutoSizeRowsMode =
                DataGridViewAutoSizeRowsMode.None;

            dgvAttempts.ColumnHeadersHeight =
                35;

            dgvAttempts.RowTemplate.Height =
                38;

            dgvAttempts.EnableHeadersVisualStyles =
                false;

            dgvAttempts.ColumnHeadersDefaultCellStyle =
                new DataGridViewCellStyle
                {
                    Font =
                        new Font(
                            "Segoe UI Semibold",
                            9,
                            FontStyle.Bold),

                    BackColor =
                        Color.FromArgb(
                            248,
                            250,
                            252),

                    ForeColor =
                        ClrBlack,

                    Alignment =
                        DataGridViewContentAlignment.MiddleLeft
                };

            dgvAttempts.DefaultCellStyle =
                new DataGridViewCellStyle
                {
                    Font =
                        new Font(
                            "Segoe UI",
                            9),

                    ForeColor =
                        ClrBlack,

                    BackColor =
                        Color.White,

                    SelectionBackColor =
                        Color.FromArgb(
                            241,
                            245,
                            249),

                    SelectionForeColor =
                        ClrBlack,

                    WrapMode =
                        DataGridViewTriState.False
                };

            DataGridViewTextBoxColumn colStudent =
                new DataGridViewTextBoxColumn();

            colStudent.Name =
                "Student";

            colStudent.HeaderText =
                "Student";

            colStudent.DataPropertyName =
                "Student";

            colStudent.AutoSizeMode =
                DataGridViewAutoSizeColumnMode.Fill;

            colStudent.FillWeight =
                52;

            dgvAttempts.Columns.Add(
                colStudent);

            DataGridViewTextBoxColumn colStatus =
                new DataGridViewTextBoxColumn();

            colStatus.Name =
                "Status";

            colStatus.HeaderText =
                "Status";

            colStatus.DataPropertyName =
                "Status";

            colStatus.AutoSizeMode =
                DataGridViewAutoSizeColumnMode.Fill;

            colStatus.FillWeight =
                48;

            dgvAttempts.Columns.Add(
                colStatus);

            monitoringPanel.Controls.Add(
                dgvAttempts);

            Label lblRefresh =
                new Label();

            lblRefresh.Text =
                "Automatically refreshes every 5 seconds";

            lblRefresh.Font =
                new Font(
                    "Segoe UI Italic",
                    8.5F,
                    FontStyle.Italic);

            lblRefresh.ForeColor =
                Color.FromArgb(
                    100,
                    116,
                    139);

            lblRefresh.AutoSize = true;

            lblRefresh.Location =
                new Point(
                    20,
                    610);

            monitoringPanel.Controls.Add(
                lblRefresh);

            // =====================================================
            // MONITORING TIMER
            // =====================================================

            monitoringTimer =
                new System.Windows.Forms.Timer();

            monitoringTimer.Interval =
                5000;

            monitoringTimer.Tick +=
                MonitoringTimer_Tick;
        }

        // =========================================================
        // IMPORT DOCX
        // =========================================================

        private void BtnImportDocx_Click(
            object sender,
            EventArgs e)
        {
            using (OpenFileDialog dialog =
                   new OpenFileDialog())
            {
                dialog.Title =
                    "Select Quiz / Exam DOCX";

                dialog.Filter =
                    "Word Document (*.docx)|*.docx";

                dialog.Multiselect =
                    false;

                if (dialog.ShowDialog() !=
                    DialogResult.OK)
                {
                    return;
                }

                try
                {
                    QuizImportResult result =
                        DocxQuizImporter.Import(
                            dialog.FileName);

                    if (result == null)
                    {
                        MessageBox.Show(
                            "Unable to import the DOCX.",
                            "Import Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);

                        return;
                    }

                    if (result.Questions == null ||
                        result.Questions.Count == 0)
                    {
                        MessageBox.Show(
                            "No questions were found in the DOCX.",
                            "No Questions",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);

                        return;
                    }

                    if (!string.IsNullOrWhiteSpace(
                        result.Title))
                    {
                        txtQuizTitle.Text =
                            result.Title;
                    }

                    if (!string.IsNullOrWhiteSpace(
                        result.Subject))
                    {
                        txtSubject.Text =
                            result.Subject;
                    }

                    importedQuestions =
                        result.Questions;

                    lblFileName.Text =
                        "File: " +
                        Path.GetFileName(
                            dialog.FileName);

                    lblQuestionCount.Text =
                        "Questions: " +
                        importedQuestions.Count;

                    RefreshQuestionList();

                    MessageBox.Show(
                        "DOCX imported successfully!\n\n" +
                        "Questions found: " +
                        importedQuestions.Count +
                        "\n\n" +
                        "The original DOCX order will be preserved when saved.\n" +
                        "Question scrambling will happen on the student side by section.",
                        "Import Successful",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "Error importing DOCX:\n\n" +
                        ex.Message,
                        "Import Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }

        // =========================================================
        // REFRESH QUESTION LIST
        // =========================================================

        private void RefreshQuestionList()
        {
            lstQuestions.Items.Clear();

            if (importedQuestions == null ||
                importedQuestions.Count == 0)
            {
                return;
            }

            int displayNumber = 1;

            AddSectionToPreview(
                "multiple_choice",
                "I. MULTIPLE CHOICE",
                ref displayNumber);

            AddSectionToPreview(
                "true_false",
                "II. TRUE / FALSE",
                ref displayNumber);

            AddSectionToPreview(
                "identification",
                "III. IDENTIFICATION",
                ref displayNumber);

            AddSectionToPreview(
                "essay",
                "IV. ESSAY",
                ref displayNumber);
        }

        // =========================================================
        // ADD SECTION TO PREVIEW
        // =========================================================

        private void AddSectionToPreview(
            string sectionType,
            string sectionTitle,
            ref int displayNumber)
        {
            List<QuizQuestion> sectionQuestions =
                new List<QuizQuestion>();

            for (int i = 0;
                 i < importedQuestions.Count;
                 i++)
            {
                QuizQuestion q =
                    importedQuestions[i];

                if (q == null)
                {
                    continue;
                }

                string type =
                    NormalizeQuestionType(
                        q.QuestionType,
                        false);

                if (type == sectionType)
                {
                    sectionQuestions.Add(q);
                }
            }

            if (sectionQuestions.Count == 0)
            {
                return;
            }

            lstQuestions.Items.Add(
                "────────────────────────────────────────");

            lstQuestions.Items.Add(
                sectionTitle +
                " (" +
                sectionQuestions.Count +
                " question" +
                (sectionQuestions.Count == 1
                    ? ""
                    : "s") +
                ")");

            lstQuestions.Items.Add(
                "────────────────────────────────────────");

            for (int i = 0;
                 i < sectionQuestions.Count;
                 i++)
            {
                QuizQuestion q =
                    sectionQuestions[i];

                string questionText =
                    q.Question == null
                        ? ""
                        : q.Question.Trim();

                lstQuestions.Items.Add(
                    displayNumber +
                    ". " +
                    questionText);

                displayNumber++;
            }

            lstQuestions.Items.Add("");
        }

        // =========================================================
        // READABLE QUESTION TYPE
        // =========================================================

        private string GetReadableQuestionType(
            string type)
        {
            string normalized =
                NormalizeQuestionType(
                    type,
                    false);

            if (normalized ==
                "multiple_choice")
            {
                return "MULTIPLE CHOICE";
            }

            if (normalized ==
                "true_false")
            {
                return "TRUE / FALSE";
            }

            if (normalized ==
                "identification")
            {
                return "IDENTIFICATION";
            }

            if (normalized ==
                "essay")
            {
                return "ESSAY";
            }

            return "UNKNOWN";
        }

        // =========================================================
        // SAVE QUIZ / EXAM
        // =========================================================

        private void BtnSaveQuiz_Click(
            object sender,
            EventArgs e)
        {
            if (cmbAssessmentType.SelectedItem == null)
            {
                MessageBox.Show(
                    "Please select Quiz or Exam.",
                    "Missing Type",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            if (cmbExamPeriod.SelectedItem == null)
            {
                MessageBox.Show(
                    "Please select the Exam Period.\n\n" +
                    "Choose PRELIM, MIDTERM, SEMIFINALS, or FINALS.",
                    "Missing Exam Period",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                cmbExamPeriod.Focus();

                return;
            }

            string title =
                txtQuizTitle.Text.Trim();

            if (string.IsNullOrWhiteSpace(title))
            {
                MessageBox.Show(
                    "Please enter the title.",
                    "Missing Title",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                txtQuizTitle.Focus();

                return;
            }

            string subject =
                txtSubject.Text.Trim();

            if (string.IsNullOrWhiteSpace(subject))
            {
                MessageBox.Show(
                    "Please enter the subject.",
                    "Missing Subject",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                txtSubject.Focus();

                return;
            }

            // =====================================================
            // VALIDATE TIME LIMIT
            // =====================================================

            int durationMinutes =
                Convert.ToInt32(
                    numDurationMinutes.Value);

            if (durationMinutes < 1)
            {
                MessageBox.Show(
                    "Time limit must be at least 1 minute.",
                    "Invalid Time Limit",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                numDurationMinutes.Focus();

                return;
            }

            if (durationMinutes > 1440)
            {
                MessageBox.Show(
                    "Time limit cannot exceed 1440 minutes (24 hours).",
                    "Invalid Time Limit",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                numDurationMinutes.Focus();

                return;
            }

            if (importedQuestions == null ||
                importedQuestions.Count == 0)
            {
                MessageBox.Show(
                    "Please import a DOCX containing questions first.",
                    "No Questions",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            // =====================================================
            // VALIDATE
            // =====================================================

            string validationError =
                ValidateQuestions();

            if (!string.IsNullOrWhiteSpace(
                validationError))
            {
                MessageBox.Show(
                    validationError,
                    "Question Validation Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            string assessmentType =
                cmbAssessmentType.SelectedItem
                .ToString()
                .ToLower();

            string examPeriod =
                cmbExamPeriod.SelectedItem
                .ToString()
                .ToUpper();

            int mcCount =
                CountQuestionsByType(
                    "multiple_choice");

            int tfCount =
                CountQuestionsByType(
                    "true_false");

            int identificationCount =
                CountQuestionsByType(
                    "identification");

            int essayCount =
                CountQuestionsByType(
                    "essay");

            DialogResult confirm =
                MessageBox.Show(
                    "Submit this " +
                    assessmentType.ToUpper() +
                    "?\n\n" +
                    "Exam Period: " +
                    examPeriod +
                    "\n" +
                    "Title: " +
                    title +
                    "\n" +
                    "Subject: " +
                    subject +
                    "\n" +
                    "Time Limit: " +
                    durationMinutes +
                    " minute" +
                    (durationMinutes == 1 ? "" : "s") +
                    "\n\n" +
                    "QUESTION STRUCTURE\n" +
                    "Multiple Choice: " +
                    mcCount +
                    "\n" +
                    "True / False: " +
                    tfCount +
                    "\n" +
                    "Identification: " +
                    identificationCount +
                    "\n" +
                    "Essay: " +
                    essayCount +
                    "\n\n" +
                    "Total Questions: " +
                    importedQuestions.Count,
                    "Confirm Submit",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

            if (confirm !=
                DialogResult.Yes)
            {
                return;
            }

            string connStr =
                SettingsManager.Current
                .GetConnectionString();

            using (var connection =
                   new MySqlConnection(connStr))
            {
                MySqlTransaction transaction =
                    null;

                try
                {
                    connection.Open();

                    transaction =
                        connection.BeginTransaction();

                    // =================================================
                    // INSERT QUIZ
                    // =================================================

                    string quizSql = @"
                        INSERT INTO quizzes
                        (
                            quiz_title,
                            assessment_type,
                            subject,
                            exam_period,
                            created_by,
                            duration_minutes
                        )
                        VALUES
                        (
                            @quiz_title,
                            @assessment_type,
                            @subject,
                            @exam_period,
                            @created_by,
                            @duration_minutes
                        );";

                    int quizId;

                    using (var command =
                           new MySqlCommand(
                               quizSql,
                               connection,
                               transaction))
                    {
                        command.Parameters.AddWithValue(
                            "@quiz_title",
                            title);

                        command.Parameters.AddWithValue(
                            "@assessment_type",
                            assessmentType);

                        command.Parameters.AddWithValue(
                            "@subject",
                            subject);

                        command.Parameters.AddWithValue(
                            "@exam_period",
                            examPeriod);

                        command.Parameters.AddWithValue(
                            "@created_by",
                            professorUserId);

                        command.Parameters.AddWithValue(
                            "@duration_minutes",
                            durationMinutes);

                        command.ExecuteNonQuery();

                        quizId =
                            Convert.ToInt32(
                                command.LastInsertedId);
                    }

                    // =================================================
                    // INSERT QUESTIONS
                    // =================================================

                    string questionSql = @"
                        INSERT INTO questions
                        (
                            quiz_id,
                            question_text,
                            question_type,
                            choice_a,
                            choice_b,
                            choice_c,
                            choice_d,
                            correct_answer
                        )
                        VALUES
                        (
                            @quiz_id,
                            @question_text,
                            @question_type,
                            @choice_a,
                            @choice_b,
                            @choice_c,
                            @choice_d,
                            @correct_answer
                        );";

                    for (int i = 0;
                         i < importedQuestions.Count;
                         i++)
                    {
                        QuizQuestion q =
                            importedQuestions[i];

                        string questionType =
                            NormalizeQuestionType(
                                q.QuestionType,
                                true);

                        string choiceA = null;
                        string choiceB = null;
                        string choiceC = null;
                        string choiceD = null;
                        string correctAnswer = null;

                        // =================================================
                        // MULTIPLE CHOICE
                        // =================================================

                        if (questionType ==
                            "multiple_choice")
                        {
                            choiceA =
                                EmptyToNull(
                                    q.ChoiceA);

                            choiceB =
                                EmptyToNull(
                                    q.ChoiceB);

                            choiceC =
                                EmptyToNull(
                                    q.ChoiceC);

                            choiceD =
                                EmptyToNull(
                                    q.ChoiceD);

                            correctAnswer =
                                EmptyToNull(
                                    q.CorrectAnswer);

                            if (choiceA == null ||
                                choiceB == null ||
                                choiceC == null ||
                                choiceD == null)
                            {
                                throw new Exception(
                                    "Question " +
                                    (i + 1) +
                                    " is marked as Multiple Choice but one or more choices are missing.\n\n" +
                                    q.Question);
                            }

                            if (correctAnswer == null)
                            {
                                throw new Exception(
                                    "Question " +
                                    (i + 1) +
                                    " is Multiple Choice but has no Correct Answer.\n\n" +
                                    q.Question);
                            }

                            correctAnswer =
                                correctAnswer
                                .Trim()
                                .ToUpper();

                            if (!IsValidMultipleChoiceAnswer(
                                correctAnswer))
                            {
                                throw new Exception(
                                    "Question " +
                                    (i + 1) +
                                    " has an invalid Multiple Choice answer.\n\n" +
                                    "Correct Answer must be A, B, C, or D.\n\n" +
                                    q.Question);
                            }
                        }

                        // =================================================
                        // TRUE / FALSE
                        // =================================================

                        else if (questionType ==
                                 "true_false")
                        {
                            choiceA =
                                "TRUE";

                            choiceB =
                                "FALSE";

                            correctAnswer =
                                NormalizeTrueFalseAnswer(
                                    q.CorrectAnswer);

                            if (correctAnswer == null)
                            {
                                throw new Exception(
                                    "Question " +
                                    (i + 1) +
                                    " is True/False but the Correct Answer is missing or invalid.\n\n" +
                                    q.Question);
                            }
                        }

                        // =================================================
                        // IDENTIFICATION
                        // =================================================

                        else if (questionType ==
                                 "identification")
                        {
                            correctAnswer =
                                EmptyToNull(
                                    q.CorrectAnswer);

                            if (correctAnswer == null)
                            {
                                throw new Exception(
                                    "Question " +
                                    (i + 1) +
                                    " is Identification but has no Correct Answer.\n\n" +
                                    "Please provide the expected answer.\n\n" +
                                    q.Question);
                            }
                        }

                        // =================================================
                        // ESSAY
                        // =================================================

                        else if (questionType ==
                                 "essay")
                        {
                            correctAnswer = null;
                        }

                        // =================================================
                        // INSERT
                        // =================================================

                        using (var command =
                               new MySqlCommand(
                                   questionSql,
                                   connection,
                                   transaction))
                        {
                            command.Parameters.AddWithValue(
                                "@quiz_id",
                                quizId);

                            command.Parameters.AddWithValue(
                                "@question_text",
                                q.Question.Trim());

                            command.Parameters.AddWithValue(
                                "@question_type",
                                questionType);

                            command.Parameters.AddWithValue(
                                "@choice_a",
                                (object)choiceA ??
                                DBNull.Value);

                            command.Parameters.AddWithValue(
                                "@choice_b",
                                (object)choiceB ??
                                DBNull.Value);

                            command.Parameters.AddWithValue(
                                "@choice_c",
                                (object)choiceC ??
                                DBNull.Value);

                            command.Parameters.AddWithValue(
                                "@choice_d",
                                (object)choiceD ??
                                DBNull.Value);

                            command.Parameters.AddWithValue(
                                "@correct_answer",
                                (object)correctAnswer ??
                                DBNull.Value);

                            command.ExecuteNonQuery();
                        }
                    }

                    // =================================================
                    // COMMIT
                    // =================================================

                    transaction.Commit();

                    // =================================================
                    // START MONITORING THIS QUIZ
                    // =================================================

                    monitoredQuizId =
                        quizId;

                    btnMonitor.Enabled =
                        true;

                    StartMonitoring(
                        monitoredQuizId);

                    MessageBox.Show(
                        assessmentType.ToUpper() +
                        " submitted successfully!\n\n" +
                        "Quiz ID: " +
                        quizId +
                        "\n" +
                        "Exam Period: " +
                        examPeriod +
                        "\n" +
                        "Time Limit: " +
                        durationMinutes +
                        " minute" +
                        (durationMinutes == 1 ? "" : "s") +
                        "\n\n" +
                        "Question Structure:\n" +
                        "Multiple Choice: " +
                        mcCount +
                        "\n" +
                        "True / False: " +
                        tfCount +
                        "\n" +
                        "Identification: " +
                        identificationCount +
                        "\n" +
                        "Essay: " +
                        essayCount +
                        "\n\n" +
                        "Total Questions: " +
                        importedQuestions.Count +
                        "\n\n" +
                        "Student monitoring is now active.",
                        "Submit Successful",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);

                    ClearForm(
                        false);
                }
                catch (Exception ex)
                {
                    try
                    {
                        if (transaction != null)
                        {
                            transaction.Rollback();
                        }
                    }
                    catch
                    {
                    }

                    MessageBox.Show(
                        "The " +
                        assessmentType.ToUpper() +
                        " was not submitted.\n\n" +
                        ex.Message,
                        "Database Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }

        // =========================================================
        // START MONITORING
        // =========================================================

        private void StartMonitoring(
            int quizId)
        {
            monitoredQuizId =
                quizId;

            lblMonitoringQuiz.Text =
                "Monitoring Quiz ID: " +
                quizId;

            dgvAttempts.Rows.Clear();

            LoadStudentAttempts();

            if (monitoringTimer != null)
            {
                monitoringTimer.Stop();
                monitoringTimer.Start();
            }
        }

        // =========================================================
        // MONITOR BUTTON
        // =========================================================

        private void BtnMonitor_Click(
            object sender,
            EventArgs e)
        {
            if (monitoredQuizId <= 0)
            {
                MessageBox.Show(
                    "No quiz is currently selected for monitoring.",
                    "Monitoring",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                return;
            }

            LoadStudentAttempts();

            if (monitoringTimer != null)
            {
                monitoringTimer.Start();
            }
        }

        // =========================================================
        // MONITORING TIMER
        // =========================================================

        private void MonitoringTimer_Tick(
            object sender,
            EventArgs e)
        {
            if (monitoredQuizId <= 0)
            {
                return;
            }

            LoadStudentAttempts();
        }

        // =========================================================
        // LOAD STUDENT ATTEMPTS
        // =========================================================

        private void LoadStudentAttempts()
        {
            if (monitoredQuizId <= 0)
            {
                return;
            }

            try
            {
                string connStr =
                    SettingsManager.Current
                    .GetConnectionString();

                using (var connection =
                       new MySqlConnection(connStr))
                {
                    connection.Open();

                    string query = @"
                        SELECT
                            qa.attempt_id,
                            qa.user_id,
                            uc.full_name,
                            qa.status,
                            qa.last_seen,
                            qa.submitted_at
                        FROM quiz_attempts qa
                        INNER JOIN user_credential uc
                            ON uc.user_id = qa.user_id
                        WHERE qa.quiz_id = @quiz_id
                        ORDER BY uc.full_name;";

                    using (var command =
                           new MySqlCommand(
                               query,
                               connection))
                    {
                        command.Parameters.AddWithValue(
                            "@quiz_id",
                            monitoredQuizId);

                        using (var reader =
                               command.ExecuteReader())
                        {
                            dgvAttempts.Rows.Clear();

                            while (reader.Read())
                            {
                                string fullName =
                                    reader["full_name"] == DBNull.Value
                                        ? "Unknown Student"
                                        : reader["full_name"].ToString();

                                string dbStatus =
                                    reader["status"] == DBNull.Value
                                        ? ""
                                        : reader["status"].ToString();

                                DateTime? lastSeen =
                                    null;

                                if (reader["last_seen"] !=
                                    DBNull.Value)
                                {
                                    lastSeen =
                                        Convert.ToDateTime(
                                            reader["last_seen"]);
                                }

                                string displayStatus =
                                    GetDisplayStatus(
                                        dbStatus,
                                        lastSeen);

                                int rowIndex =
                                    dgvAttempts.Rows.Add(
                                        fullName,
                                        displayStatus);

                                DataGridViewRow row =
                                    dgvAttempts.Rows[
                                        rowIndex];

                                ApplyStatusStyle(
                                    row,
                                    displayStatus);
                            }
                        }
                    }
                }
            }
            catch
            {
                // Do not continuously show database/LAN errors
                // to the professor while the timer is refreshing.
                //
                // The next refresh will try again.
            }
        }

        // =========================================================
        // GET DISPLAY STATUS
        // =========================================================

        private string GetDisplayStatus(
            string dbStatus,
            DateTime? lastSeen)
        {
            string status =
                dbStatus == null
                    ? ""
                    : dbStatus.Trim().ToUpper();

            // Submitted always remains Submitted.
            if (status ==
                "SUBMITTED")
            {
                return "✓ Submitted";
            }

            // Taking + recent heartbeat.
            if (status ==
                    "TAKING" &&
                lastSeen.HasValue)
            {
                TimeSpan elapsed =
                    DateTime.Now -
                    lastSeen.Value;

                if (elapsed.TotalSeconds <=
                    15)
                {
                    return "🟢 Taking Quiz";
                }

                return "🟡 Disconnected";
            }

            // If TAKING but last_seen is missing,
            // treat it as disconnected rather than
            // incorrectly showing the student as active.
            if (status ==
                "TAKING")
            {
                return "🟡 Disconnected";
            }

            return status;
        }

        // =========================================================
        // APPLY STATUS STYLE
        // =========================================================

        private void ApplyStatusStyle(
            DataGridViewRow row,
            string displayStatus)
        {
            if (row == null)
            {
                return;
            }

            if (displayStatus ==
                "🟢 Taking Quiz")
            {
                row.Cells["Status"]
                    .Style.ForeColor =
                    ClrGreen;

                row.Cells["Status"]
                    .Style.Font =
                    new Font(
                        "Segoe UI Semibold",
                        9,
                        FontStyle.Bold);
            }
            else if (displayStatus ==
                     "🟡 Disconnected")
            {
                row.Cells["Status"]
                    .Style.ForeColor =
                    ClrYellow;

                row.Cells["Status"]
                    .Style.Font =
                    new Font(
                        "Segoe UI Semibold",
                        9,
                        FontStyle.Bold);
            }
            else if (displayStatus ==
                     "✓ Submitted")
            {
                row.Cells["Status"]
                    .Style.ForeColor =
                    ClrGreen;

                row.Cells["Status"]
                    .Style.Font =
                    new Font(
                        "Segoe UI Semibold",
                        9,
                        FontStyle.Bold);
            }
            else
            {
                row.Cells["Status"]
                    .Style.ForeColor =
                    ClrRed;
            }
        }

        // =========================================================
        // COUNT QUESTIONS BY TYPE
        // =========================================================

        private int CountQuestionsByType(
            string targetType)
        {
            int count = 0;

            if (importedQuestions == null)
            {
                return 0;
            }

            for (int i = 0;
                 i < importedQuestions.Count;
                 i++)
            {
                QuizQuestion q =
                    importedQuestions[i];

                if (q == null)
                {
                    continue;
                }

                string type =
                    NormalizeQuestionType(
                        q.QuestionType,
                        false);

                if (type == targetType)
                {
                    count++;
                }
            }

            return count;
        }

        // =========================================================
        // VALIDATE QUESTIONS
        // =========================================================

        private string ValidateQuestions()
        {
            for (int i = 0;
                 i < importedQuestions.Count;
                 i++)
            {
                QuizQuestion q =
                    importedQuestions[i];

                if (q == null)
                {
                    return
                        "Question " +
                        (i + 1) +
                        " is empty.";
                }

                if (string.IsNullOrWhiteSpace(
                    q.Question))
                {
                    return
                        "Question " +
                        (i + 1) +
                        " has no question text.";
                }

                string questionType;

                try
                {
                    questionType =
                        NormalizeQuestionType(
                            q.QuestionType,
                            true);
                }
                catch (Exception ex)
                {
                    return
                        "Question " +
                        (i + 1) +
                        " has an unsupported question type.\n\n" +
                        ex.Message;
                }

                // =====================================================
                // MULTIPLE CHOICE
                // =====================================================

                if (questionType ==
                    "multiple_choice")
                {
                    if (string.IsNullOrWhiteSpace(
                        q.ChoiceA))
                    {
                        return
                            "Question " +
                            (i + 1) +
                            " is Multiple Choice but Choice A is missing.\n\n" +
                            q.Question;
                    }

                    if (string.IsNullOrWhiteSpace(
                        q.ChoiceB))
                    {
                        return
                            "Question " +
                            (i + 1) +
                            " is Multiple Choice but Choice B is missing.\n\n" +
                            q.Question;
                    }

                    if (string.IsNullOrWhiteSpace(
                        q.ChoiceC))
                    {
                        return
                            "Question " +
                            (i + 1) +
                            " is Multiple Choice but Choice C is missing.\n\n" +
                            q.Question;
                    }

                    if (string.IsNullOrWhiteSpace(
                        q.ChoiceD))
                    {
                        return
                            "Question " +
                            (i + 1) +
                            " is Multiple Choice but Choice D is missing.\n\n" +
                            q.Question;
                    }

                    if (string.IsNullOrWhiteSpace(
                        q.CorrectAnswer))
                    {
                        return
                            "Question " +
                            (i + 1) +
                            " is Multiple Choice but the Correct Answer is missing.\n\n" +
                            q.Question;
                    }

                    if (!IsValidMultipleChoiceAnswer(
                        q.CorrectAnswer))
                    {
                        return
                            "Question " +
                            (i + 1) +
                            " has an invalid Multiple Choice Correct Answer.\n\n" +
                            "Use A, B, C, or D.\n\n" +
                            q.Question;
                    }
                }

                // =====================================================
                // TRUE / FALSE
                // =====================================================

                else if (questionType ==
                         "true_false")
                {
                    if (string.IsNullOrWhiteSpace(
                        q.CorrectAnswer))
                    {
                        return
                            "Question " +
                            (i + 1) +
                            " is True/False but has no Correct Answer.\n\n" +
                            q.Question;
                    }

                    if (NormalizeTrueFalseAnswer(
                        q.CorrectAnswer) == null)
                    {
                        return
                            "Question " +
                            (i + 1) +
                            " has an invalid True/False answer.\n\n" +
                            "Use TRUE or FALSE.\n\n" +
                            q.Question;
                    }
                }

                // =====================================================
                // IDENTIFICATION
                // =====================================================

                else if (questionType ==
                         "identification")
                {
                    if (string.IsNullOrWhiteSpace(
                        q.CorrectAnswer))
                    {
                        return
                            "Question " +
                            (i + 1) +
                            " is Identification but has no Correct Answer.\n\n" +
                            "Identification questions need an expected answer for automatic checking.\n\n" +
                            q.Question;
                    }
                }

                // =====================================================
                // ESSAY
                // =====================================================

                else if (questionType ==
                         "essay")
                {
                    // Essay does not require a correct answer.
                }
            }

            return null;
        }

        // =========================================================
        // NORMALIZE QUESTION TYPE
        // =========================================================

        private string NormalizeQuestionType(
            string type,
            bool throwOnUnknown)
        {
            if (string.IsNullOrWhiteSpace(type))
            {
                if (throwOnUnknown)
                {
                    throw new Exception(
                        "Question type is missing.");
                }

                return "unknown";
            }

            string normalized =
                type.Trim()
                .ToLower()
                .Replace("-", "_")
                .Replace("/", "_");

            // =====================================================
            // MULTIPLE CHOICE
            // =====================================================

            if (normalized ==
                    "multiple_choice" ||
                normalized ==
                    "multiplechoice" ||
                normalized ==
                    "multiple choice" ||
                normalized ==
                    "mc")
            {
                return "multiple_choice";
            }

            // =====================================================
            // TRUE / FALSE
            // =====================================================

            if (normalized ==
                    "true_false" ||
                normalized ==
                    "truefalse" ||
                normalized ==
                    "true or false" ||
                normalized ==
                    "true_false_question" ||
                normalized ==
                    "true/false" ||
                normalized ==
                    "tf")
            {
                return "true_false";
            }

            // =====================================================
            // IDENTIFICATION
            // =====================================================

            if (normalized ==
                    "identification" ||
                normalized ==
                    "identification_question" ||
                normalized ==
                    "identification question" ||
                normalized ==
                    "identify" ||
                normalized ==
                    "id" ||
                normalized ==
                    "fill_in_the_blank" ||
                normalized ==
                    "fill in the blank" ||
                normalized ==
                    "fillintheblank")
            {
                return "identification";
            }

            // =====================================================
            // ESSAY
            // =====================================================

            if (normalized ==
                    "essay" ||
                normalized ==
                    "essay_question" ||
                normalized ==
                    "essay question")
            {
                return "essay";
            }

            // =====================================================
            // UNKNOWN
            // =====================================================

            if (throwOnUnknown)
            {
                throw new Exception(
                    "Unsupported question type: \"" +
                    type +
                    "\".\n\n" +
                    "Supported types are:\n" +
                    "• Multiple Choice\n" +
                    "• True / False\n" +
                    "• Identification\n" +
                    "• Essay");
            }

            return "unknown";
        }

        // =========================================================
        // TRUE / FALSE ANSWER
        // =========================================================

        private string NormalizeTrueFalseAnswer(
            string answer)
        {
            if (string.IsNullOrWhiteSpace(
                answer))
            {
                return null;
            }

            string value =
                answer.Trim()
                .ToUpper();

            if (value ==
                    "TRUE" ||
                value ==
                    "T")
            {
                return "TRUE";
            }

            if (value ==
                    "FALSE" ||
                value ==
                    "F")
            {
                return "FALSE";
            }

            return null;
        }

        // =========================================================
        // MULTIPLE CHOICE ANSWER
        // =========================================================

        private bool IsValidMultipleChoiceAnswer(
            string answer)
        {
            if (string.IsNullOrWhiteSpace(
                answer))
            {
                return false;
            }

            string value =
                answer.Trim()
                .ToUpper();

            return
                value == "A" ||
                value == "B" ||
                value == "C" ||
                value == "D";
        }

        // =========================================================
        // EMPTY TO NULL
        // =========================================================

        private string EmptyToNull(
            string value)
        {
            if (string.IsNullOrWhiteSpace(
                value))
            {
                return null;
            }

            return value.Trim();
        }

        // =========================================================
        // CLEAR BUTTON
        // =========================================================

        private void BtnClear_Click(
            object sender,
            EventArgs e)
        {
            DialogResult result =
                MessageBox.Show(
                    "Clear the current quiz/exam?",
                    "Clear",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

            if (result ==
                DialogResult.Yes)
            {
                ClearForm();
            }
        }

        // =========================================================
        // CLEAR FORM
        // =========================================================

        private void ClearForm(
            bool clearMonitoring = true)
        {
            txtQuizTitle.Clear();

            txtSubject.Clear();

            cmbAssessmentType.SelectedIndex =
                0;

            cmbExamPeriod.SelectedIndex =
                0;

            // Reset time limit to default 60 minutes.
            if (numDurationMinutes != null)
            {
                numDurationMinutes.Value = 60;
            }

            importedQuestions =
                new List<QuizQuestion>();

            lstQuestions.Items.Clear();

            lblFileName.Text =
                "No DOCX file selected.";

            lblQuestionCount.Text =
                "Questions: 0";

            if (clearMonitoring)
            {
                StopMonitoring();

                monitoredQuizId =
                    0;

                btnMonitor.Enabled =
                    false;

                lblMonitoringQuiz.Text =
                    "No quiz selected.";

                dgvAttempts.Rows.Clear();
            }
        }

        // =========================================================
        // STOP MONITORING
        // =========================================================

        private void StopMonitoring()
        {
            if (monitoringTimer != null)
            {
                monitoringTimer.Stop();
            }
        }

        // =========================================================
        // FORM CLOSED
        // =========================================================

        private void ProfessorQuizForm_FormClosed(
            object sender,
            FormClosedEventArgs e)
        {
            StopMonitoring();

            if (monitoringTimer != null)
            {
                monitoringTimer.Tick -=
                    MonitoringTimer_Tick;

                monitoringTimer.Dispose();

                monitoringTimer =
                    null;
            }
        }
    }

    // NOTE:
    // RoundedButton class ay nasa LivenessCheckForm.cs na —
    // inalis dito para maiwasan ang duplicate/ambiguous class error.
}