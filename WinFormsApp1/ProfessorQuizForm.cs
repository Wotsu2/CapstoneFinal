
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Threading.Tasks;
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

        private NumericUpDown numHours;
        private NumericUpDown numMinutes;

        private RoundedButton btnImportDocx;
        private RoundedButton btnViewExample;
        private RoundedButton btnSaveQuiz;
        private RoundedButton btnClear;
        private RoundedButton btnMonitor;
        private RoundedButton btnEditQuestion;

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

        // =========================================================
        // IMPORTED QUESTIONS
        // =========================================================

        private List<QuizQuestion> importedQuestions =
            new List<QuizQuestion>();

        // Maps ListBox item index to the actual QuizQuestion
        // This allows the professor to edit the selected question.
        private Dictionary<int, QuizQuestion> questionItemMap =
            new Dictionary<int, QuizQuestion>();

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

        private static readonly Color ClrGold =
            Color.FromArgb(198, 156, 53);

        private static readonly Color ClrGoldDark =
            Color.FromArgb(163, 126, 36);

        private static readonly Color ClrGoldLight =
            Color.FromArgb(240, 224, 180);

        private static readonly Color ClrPageBg =
            Color.FromArgb(250, 247, 239);

        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public ProfessorQuizForm(int professorID)
        {
            professorUserId = professorID;

            BuildProfessorInterface();

            this.FormClosed +=
                ProfessorQuizForm_FormClosed;
        }

        // =========================================================
        // BUILD INTERFACE
        // =========================================================

        private void BuildProfessorInterface()
        {
            Text =
                "Professor - Create Quiz / Exam";

            StartPosition =
                FormStartPosition.CenterScreen;

            Size =
                new Size(
                    1200,
                    800);

            MinimumSize =
                new Size(
                    1100,
                    740);

            BackColor =
                ClrPageBg;

            FormBorderStyle =
                FormBorderStyle.None;

            Font =
                new Font(
                    "Segoe UI",
                    9.5F);

            this.Paint +=
                (s, e) =>
                {
                    using (var pen =
                           new Pen(
                               ClrGold,
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

            int bannerHeight = 100;

            int yShift = 40;

            // =====================================================
            // HEADER BANNER
            // =====================================================

            Panel bannerPanel =
                new Panel();

            bannerPanel.Location =
                new Point(
                    0,
                    0);

            bannerPanel.Size =
                new Size(
                    this.Width,
                    bannerHeight);

            bannerPanel.BackColor =
                ClrMaroon;

            bannerPanel.Anchor =
                AnchorStyles.Top |
                AnchorStyles.Left |
                AnchorStyles.Right;

            bannerPanel.Paint +=
                (s, e) =>
                {
                    using (var goldLine =
                           new Pen(
                               ClrGold,
                               4f))
                    {
                        e.Graphics.DrawLine(
                            goldLine,
                            0,
                            bannerPanel.Height - 2,
                            bannerPanel.Width,
                            bannerPanel.Height - 2);
                    }
                };

            Controls.Add(
                bannerPanel);

            Label lblBrand =
                new Label();

            lblBrand.Text =
                "CDSGA";

            lblBrand.Font =
                new Font(
                    "Segoe UI Semibold",
                    13,
                    FontStyle.Bold);

            lblBrand.ForeColor =
                ClrGold;

            lblBrand.AutoSize =
                true;

            lblBrand.Location =
                new Point(
                    pad,
                    16);

            bannerPanel.Controls.Add(
                lblBrand);

            Label lblHeader =
                new Label();

            lblHeader.Text =
                "Create Quiz / Exam";

            lblHeader.Font =
                new Font(
                    "Segoe UI Semibold",
                    20,
                    FontStyle.Bold);

            lblHeader.ForeColor =
                Color.White;

            lblHeader.AutoSize =
                true;

            lblHeader.Location =
                new Point(
                    pad,
                    40);

            bannerPanel.Controls.Add(
                lblHeader);

            Label lblClose =
                new Label();

            lblClose.Text =
                "✕";

            lblClose.Font =
                new Font(
                    "Segoe UI",
                    14);

            lblClose.ForeColor =
                Color.White;

            lblClose.AutoSize =
                true;

            lblClose.Cursor =
                Cursors.Hand;

            lblClose.Location =
                new Point(
                    this.ClientSize.Width -
                    pad -
                    16,
                    36);

            lblClose.Anchor =
                AnchorStyles.Top |
                AnchorStyles.Right;

            lblClose.Click +=
                (s, e) =>
                {
                    this.Close();
                };

            bannerPanel.Controls.Add(
                lblClose);

            // =====================================================
            // LEFT CARD
            // =====================================================

            Panel leftCard =
                CreateCardPanel(
                    pad - 15,
                    bannerHeight + 15,
                    leftWidth + 30,
                    660);

            Controls.Add(
                leftCard);

            leftCard.SendToBack();

            // =====================================================
            // TITLE
            // =====================================================

            Label lblTitle =
                new Label();

            lblTitle.Text =
                "Title";

            lblTitle.Font =
                new Font(
                    "Segoe UI Semibold",
                    10,
                    FontStyle.Bold);

            lblTitle.ForeColor =
                ClrLabelGray;

            lblTitle.AutoSize =
                true;

            lblTitle.Location =
                new Point(
                    pad,
                    85 + yShift);

            Controls.Add(
                lblTitle);

            txtQuizTitle =
                new TextBox();

            txtQuizTitle.Font =
                new Font(
                    "Segoe UI",
                    10);

            txtQuizTitle.BorderStyle =
                BorderStyle.FixedSingle;

            txtQuizTitle.Location =
                new Point(
                    pad,
                    108 + yShift);

            txtQuizTitle.Size =
                new Size(
                    leftWidth,
                    30);

            Controls.Add(
                txtQuizTitle);

            // =====================================================
            // ASSESSMENT TYPE / EXAM PERIOD
            // =====================================================

            int halfWidth =
                (leftWidth - 24) / 2;

            Label lblType =
                new Label();

            lblType.Text =
                "Assessment Type";

            lblType.Font =
                new Font(
                    "Segoe UI Semibold",
                    10,
                    FontStyle.Bold);

            lblType.ForeColor =
                ClrLabelGray;

            lblType.AutoSize =
                true;

            lblType.Location =
                new Point(
                    pad,
                    155 + yShift);

            Controls.Add(
                lblType);

            Label lblExamPeriod =
                new Label();

            lblExamPeriod.Text =
                "Exam Period";

            lblExamPeriod.Font =
                new Font(
                    "Segoe UI Semibold",
                    10,
                    FontStyle.Bold);

            lblExamPeriod.ForeColor =
                ClrLabelGray;

            lblExamPeriod.AutoSize =
                true;

            lblExamPeriod.Location =
                new Point(
                    pad +
                    halfWidth +
                    24,
                    155 + yShift);

            Controls.Add(
                lblExamPeriod);

            cmbAssessmentType =
                new ComboBox();

            cmbAssessmentType.DropDownStyle =
                ComboBoxStyle.DropDownList;

            cmbAssessmentType.Items.Add(
                "Quiz");

            cmbAssessmentType.Items.Add(
                "Exam");

            cmbAssessmentType.SelectedIndex =
                0;

            cmbAssessmentType.Font =
                new Font(
                    "Segoe UI",
                    10);

            cmbAssessmentType.Location =
                new Point(
                    pad,
                    178 + yShift);

            cmbAssessmentType.Size =
                new Size(
                    halfWidth,
                    30);

            Controls.Add(
                cmbAssessmentType);

            cmbExamPeriod =
                new ComboBox();

            cmbExamPeriod.DropDownStyle =
                ComboBoxStyle.DropDownList;

            cmbExamPeriod.Items.Add(
                "PRELIM");

            cmbExamPeriod.Items.Add(
                "MIDTERM");

            cmbExamPeriod.Items.Add(
                "SEMIFINALS");

            cmbExamPeriod.Items.Add(
                "FINALS");

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
                    178 + yShift);

            cmbExamPeriod.Size =
                new Size(
                    halfWidth,
                    30);

            Controls.Add(
                cmbExamPeriod);

            // =====================================================
            // SUBJECT / TIME LIMIT
            // =====================================================

            Label lblSubject =
                new Label();

            lblSubject.Text =
                "Subject";

            lblSubject.Font =
                new Font(
                    "Segoe UI Semibold",
                    10,
                    FontStyle.Bold);

            lblSubject.ForeColor =
                ClrLabelGray;

            lblSubject.AutoSize =
                true;

            lblSubject.Location =
                new Point(
                    pad,
                    225 + yShift);

            Controls.Add(
                lblSubject);

            Label lblDuration =
                new Label();

            lblDuration.Text =
                "Time Limit";

            lblDuration.Font =
                new Font(
                    "Segoe UI Semibold",
                    10,
                    FontStyle.Bold);

            lblDuration.ForeColor =
                ClrLabelGray;

            lblDuration.AutoSize =
                true;

            lblDuration.Location =
                new Point(
                    pad +
                    halfWidth +
                    24,
                    225 + yShift);

            Controls.Add(
                lblDuration);

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
                    248 + yShift);

            txtSubject.Size =
                new Size(
                    halfWidth,
                    30);

            Controls.Add(
                txtSubject);

            // =====================================================
            // TIME LIMIT
            // =====================================================

            int durationInputX =
                pad +
                halfWidth +
                24;

            int durationInputY =
                248 + yShift;

            numHours =
                new NumericUpDown();

            numHours.Font =
                new Font(
                    "Segoe UI",
                    10);

            numHours.BorderStyle =
                BorderStyle.FixedSingle;

            numHours.Location =
                new Point(
                    durationInputX,
                    durationInputY);

            numHours.Size =
                new Size(
                    70,
                    30);

            numHours.Minimum =
                0;

            numHours.Maximum =
                24;

            numHours.Value =
                1;

            numHours.TextAlign =
                HorizontalAlignment.Center;

            Controls.Add(
                numHours);

            Label lblHoursUnit =
                new Label();

            lblHoursUnit.Text =
                "hr";

            lblHoursUnit.Font =
                new Font(
                    "Segoe UI",
                    9.5F);

            lblHoursUnit.ForeColor =
                ClrLabelGray;

            lblHoursUnit.AutoSize =
                true;

            lblHoursUnit.Location =
                new Point(
                    durationInputX + 74,
                    durationInputY + 7);

            Controls.Add(
                lblHoursUnit);

            numMinutes =
                new NumericUpDown();

            numMinutes.Font =
                new Font(
                    "Segoe UI",
                    10);

            numMinutes.BorderStyle =
                BorderStyle.FixedSingle;

            numMinutes.Location =
                new Point(
                    durationInputX + 104,
                    durationInputY);

            numMinutes.Size =
                new Size(
                    70,
                    30);

            numMinutes.Minimum =
                0;

            numMinutes.Maximum =
                59;

            numMinutes.Increment =
                5;

            numMinutes.Value =
                0;

            numMinutes.TextAlign =
                HorizontalAlignment.Center;

            Controls.Add(
                numMinutes);

            Label lblMinutesUnit =
                new Label();

            lblMinutesUnit.Text =
                "min";

            lblMinutesUnit.Font =
                new Font(
                    "Segoe UI",
                    9.5F);

            lblMinutesUnit.ForeColor =
                ClrLabelGray;

            lblMinutesUnit.AutoSize =
                true;

            lblMinutesUnit.Location =
                new Point(
                    durationInputX + 178,
                    durationInputY + 7);

            Controls.Add(
                lblMinutesUnit);

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
                    300 + yShift);

            btnImportDocx.BackColor =
                ClrMaroon;

            btnImportDocx.HoverColor =
                ClrMaroonDark;

            btnImportDocx.ForeColor =
                Color.White;

            btnImportDocx.Click +=
                BtnImportDocx_Click;

            Controls.Add(
                btnImportDocx);

            // =====================================================
            // VIEW EXAMPLE
            // =====================================================

            btnViewExample =
                new RoundedButton();

            btnViewExample.Text =
                "?";

            btnViewExample.Font =
                new Font(
                    "Segoe UI Semibold",
                    10,
                    FontStyle.Bold);

            btnViewExample.Size =
                new Size(
                    40,
                    40);

            btnViewExample.Location =
                new Point(
                    pad + 190 + 8,
                    298 + yShift);

            btnViewExample.BackColor =
                ClrGold;

            btnViewExample.HoverColor =
                ClrGoldDark;

            btnViewExample.ForeColor =
                ClrMaroonDark;

            ToolTip tipViewExample =
                new ToolTip();

            tipViewExample.SetToolTip(
                btnViewExample,
                "See an example DOCX showing how to format your quiz");

            btnViewExample.Click +=
                BtnViewExample_Click;

            Controls.Add(
                btnViewExample);

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

            lblFileName.AutoSize =
                true;

            lblFileName.Location =
                new Point(
                    pad + 250,
                    312 + yShift);

            Controls.Add(
                lblFileName);

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

            lblQuestionCount.AutoSize =
                true;

            lblQuestionCount.Location =
                new Point(
                    pad +
                    leftWidth -
                    110,
                    312 + yShift);

            Controls.Add(
                lblQuestionCount);

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

            lblPreview.AutoSize =
                true;

            lblPreview.Location =
                new Point(
                    pad,
                    358 + yShift);

            Controls.Add(
                lblPreview);

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
                    385 + yShift);

            lstQuestions.Size =
                new Size(
                    leftWidth,
                    230);

            Controls.Add(
                lstQuestions);

            // Double click question to edit
            lstQuestions.DoubleClick +=
                LstQuestions_DoubleClick;

            // =====================================================
            // EDIT SELECTED QUESTION
            // =====================================================

            btnEditQuestion =
                new RoundedButton();

            btnEditQuestion.Text =
                "✎  EDIT SELECTED";

            btnEditQuestion.Font =
                new Font(
                    "Segoe UI Semibold",
                    9.5F,
                    FontStyle.Bold);

            btnEditQuestion.Size =
                new Size(
                    160,
                    38);

            btnEditQuestion.Location =
                new Point(
                    pad,
                    625 + yShift);

            btnEditQuestion.BackColor =
                ClrGold;

            btnEditQuestion.HoverColor =
                ClrGoldDark;

            btnEditQuestion.ForeColor =
                ClrMaroonDark;

            btnEditQuestion.Click +=
                BtnEditQuestion_Click;

            Controls.Add(
                btnEditQuestion);

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
                    675 + yShift);

            btnSaveQuiz.BackColor =
                ClrMaroon;

            btnSaveQuiz.HoverColor =
                ClrMaroonDark;

            btnSaveQuiz.ForeColor =
                Color.White;

            btnSaveQuiz.Click +=
                BtnSaveQuiz_Click;

            Controls.Add(
                btnSaveQuiz);

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
                    675 + yShift);

            btnMonitor.BackColor =
                ClrGold;

            btnMonitor.HoverColor =
                ClrGoldDark;

            btnMonitor.ForeColor =
                ClrMaroonDark;

            btnMonitor.Enabled =
                false;

            btnMonitor.Click +=
                BtnMonitor_Click;

            Controls.Add(
                btnMonitor);

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
                    675 + yShift);

            btnClear.BackColor =
                ClrBlack;

            btnClear.HoverColor =
                ClrBlackHover;

            btnClear.ForeColor =
                Color.White;

            btnClear.Click +=
                BtnClear_Click;

            Controls.Add(
                btnClear);

            // =====================================================
            // RIGHT SIDE - MONITORING DASHBOARD
            // =====================================================

            Panel monitoringPanel =
                CreateCardPanel(
                    rightX,
                    bannerHeight + 15,
                    rightWidth,
                    632);

            Controls.Add(
                monitoringPanel);

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
                ClrMaroon;

            lblMonitoringTitle.AutoSize =
                true;

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

            Panel legendPanel =
                new Panel();

            legendPanel.Size =
                new Size(
                    rightWidth - 40,
                    30);

            legendPanel.Location =
                new Point(
                    20,
                    94);

            legendPanel.BackColor =
                Color.White;

            legendPanel.Paint +=
                LegendPanel_Paint;

            monitoringPanel.Controls.Add(
                legendPanel);

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
                        ClrGoldLight,

                    ForeColor =
                        ClrMaroonDark,

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

            dgvAttempts.CellPainting +=
                DgvAttempts_CellPainting;

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

            lblRefresh.AutoSize =
                true;

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

            leftCard.SendToBack();
        }

        // =========================================================
        // CARD PANEL HELPER
        // =========================================================

        private Panel CreateCardPanel(
            int x,
            int y,
            int width,
            int height,
            int radius = 14)
        {
            Panel panel =
                new Panel();

            panel.Location =
                new Point(
                    x,
                    y);

            panel.Size =
                new Size(
                    width,
                    height);

            panel.BackColor =
                Color.White;

            panel.Paint +=
                (s, e) =>
                {
                    e.Graphics.SmoothingMode =
                        SmoothingMode.AntiAlias;

                    Rectangle bounds =
                        new Rectangle(
                            0,
                            0,
                            panel.Width - 1,
                            panel.Height - 1);

                    using (GraphicsPath path =
                           BuildRoundedRectPath(
                               bounds,
                               radius))
                    {
                        using (var borderPen =
                               new Pen(
                                   ClrGold,
                                   1.6f))
                        {
                            e.Graphics.DrawPath(
                                borderPen,
                                path);
                        }
                    }
                };

            return panel;
        }

        private GraphicsPath BuildRoundedRectPath(
            Rectangle bounds,
            int radius)
        {
            int d =
                radius * 2;

            GraphicsPath path =
                new GraphicsPath();

            path.AddArc(
                bounds.X,
                bounds.Y,
                d,
                d,
                180,
                90);

            path.AddArc(
                bounds.Right - d,
                bounds.Y,
                d,
                d,
                270,
                90);

            path.AddArc(
                bounds.Right - d,
                bounds.Bottom - d,
                d,
                d,
                0,
                90);

            path.AddArc(
                bounds.X,
                bounds.Bottom - d,
                d,
                d,
                90,
                90);

            path.CloseFigure();

            return path;
        }

        // =========================================================
        // LEGEND
        // =========================================================

        private void LegendPanel_Paint(
            object sender,
            PaintEventArgs e)
        {
            e.Graphics.SmoothingMode =
                SmoothingMode.AntiAlias;

            int dotSize =
                10;

            int y =
                (((Panel)sender).Height -
                 dotSize) / 2;

            int x =
                0;

            using (var font =
                   new Font(
                       "Segoe UI",
                       8.5F))
            {
                DrawLegendItem(
                    e.Graphics,
                    ref x,
                    y,
                    dotSize,
                    ClrGreen,
                    "Taking Quiz",
                    font);

                x += 22;

                DrawLegendItem(
                    e.Graphics,
                    ref x,
                    y,
                    dotSize,
                    ClrYellow,
                    "Disconnected",
                    font);

                x += 22;

                DrawLegendItem(
                    e.Graphics,
                    ref x,
                    y,
                    dotSize,
                    ClrGreen,
                    "Submitted",
                    font);
            }
        }

        private void DrawLegendItem(
            Graphics g,
            ref int x,
            int y,
            int dotSize,
            Color dotColor,
            string text,
            Font font)
        {
            using (var dotBrush =
                   new SolidBrush(dotColor))
            {
                g.FillEllipse(
                    dotBrush,
                    x,
                    y,
                    dotSize,
                    dotSize);
            }

            x +=
                dotSize + 6;

            SizeF textSize =
                g.MeasureString(
                    text,
                    font);

            using (var textBrush =
                   new SolidBrush(
                       ClrLabelGray))
            {
                g.DrawString(
                    text,
                    font,
                    textBrush,
                    x,
                    y - 3);
            }

            x +=
                (int)textSize.Width;
        }

        // =========================================================
        // STATUS COLUMN
        // =========================================================

        private void DgvAttempts_CellPainting(
            object sender,
            DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 ||
                e.ColumnIndex < 0 ||
                dgvAttempts.Columns[e.ColumnIndex].Name !=
                "Status")
            {
                return;
            }

            e.PaintBackground(
                e.ClipBounds,
                true);

            string status =
                e.Value == null
                    ? ""
                    : e.Value.ToString();

            Color dotColor =
                ClrRed;

            if (status ==
                "Taking Quiz")
            {
                dotColor =
                    ClrGreen;
            }
            else if (status ==
                     "Disconnected")
            {
                dotColor =
                    ClrYellow;
            }
            else if (status ==
                     "Submitted")
            {
                dotColor =
                    ClrGreen;
            }

            e.Graphics.SmoothingMode =
                SmoothingMode.AntiAlias;

            int dotSize =
                10;

            int dotX =
                e.CellBounds.Left + 10;

            int dotY =
                e.CellBounds.Top +
                ((e.CellBounds.Height -
                  dotSize) / 2);

            using (var dotBrush =
                   new SolidBrush(dotColor))
            {
                e.Graphics.FillEllipse(
                    dotBrush,
                    dotX,
                    dotY,
                    dotSize,
                    dotSize);
            }

            using (var font =
                   new Font(
                       "Segoe UI Semibold",
                       9,
                       FontStyle.Bold))
            using (var textBrush =
                   new SolidBrush(
                       ClrBlack))
            {
                e.Graphics.DrawString(
                    status,
                    font,
                    textBrush,
                    dotX +
                    dotSize +
                    8,
                    e.CellBounds.Top +
                    ((e.CellBounds.Height -
                      font.Height) / 2));
            }

            e.Handled =
                true;
        }

        // =========================================================
        // FORMAT DURATION
        // =========================================================

        private string FormatDuration(
            int totalMinutes)
        {
            int hours =
                totalMinutes / 60;

            int minutes =
                totalMinutes % 60;

            if (hours > 0 &&
                minutes > 0)
            {
                return
                    hours +
                    "h " +
                    minutes +
                    "m";
            }

            if (hours > 0)
            {
                return
                    hours +
                    "h";
            }

            return
                minutes +
                " minute" +
                (minutes == 1
                    ? ""
                    : "s");
        }

        // =========================================================
        // IMPORT DOCX
        // =========================================================

        private async void BtnImportDocx_Click(
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
                    string selectedDocx =
                        dialog.FileName;

                    QuizImportResult result =
                        null;

                    using (new LoadingOverlay(
                        this,
                        "Importing DOCX"))
                    {
                        result =
                            await Task.Run(
                                () =>
                                    DocxQuizImporter.Import(
                                        selectedDocx));
                    }

                    if (result == null)
                    {
                        CustomMessageBox.Show(
                            "Unable to import the DOCX.",
                            "Import Error",
                            CustomMessageBoxButtons.OK,
                            CustomMessageBoxIcon.Error);

                        return;
                    }

                    if (result.Questions == null ||
                        result.Questions.Count == 0)
                    {
                        CustomMessageBox.Show(
                            "No questions were found in the DOCX.",
                            "No Questions",
                            CustomMessageBoxButtons.OK,
                            CustomMessageBoxIcon.Warning);

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

                    CustomMessageBox.Show(
                        "DOCX imported successfully!\n\n" +
                        "Questions found: " +
                        importedQuestions.Count +
                        "\n\n" +
                        "You can now edit any imported question " +
                        "without uploading another DOCX.\n\n" +
                        "Double-click a question or select it " +
                        "and click EDIT SELECTED.",
                        "Import Successful",
                        CustomMessageBoxButtons.OK,
                        CustomMessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show(
                        "Error importing DOCX:\n\n" +
                        ex.Message,
                        "Import Error",
                        CustomMessageBoxButtons.OK,
                        CustomMessageBoxIcon.Error);
                }
            }
        }

        // =========================================================
        // VIEW EXAMPLE DOCX
        // =========================================================

        private async void BtnViewExample_Click(
            object sender,
            EventArgs e)
        {
            using (SaveFileDialog dialog =
                   new SaveFileDialog())
            {
                dialog.Title =
                    "Save Example Quiz Template";

                dialog.Filter =
                    "Word Document (*.docx)|*.docx";

                dialog.FileName =
                    "Exam_Import_Template_Example.docx";

                if (dialog.ShowDialog() !=
                    DialogResult.OK)
                {
                    return;
                }

                try
                {
                    string examplePath =
                        dialog.FileName;

                    using (new LoadingOverlay(
                        this,
                        "Creating template"))
                    {
                        await Task.Run(
                            () =>
                                ExampleTemplateGenerator.Generate(
                                    examplePath));
                    }

                    var open =
                        CustomMessageBox.Show(
                            "Example template saved!\n\n" +
                            "Open it now?",
                            "Saved",
                            CustomMessageBoxButtons.YesNo,
                            CustomMessageBoxIcon.Question);

                    if (open ==
                        CustomMessageBoxResult.Yes)
                    {
                        System.Diagnostics.Process.Start(
                            new System.Diagnostics.ProcessStartInfo
                            {
                                FileName =
                                    dialog.FileName,

                                UseShellExecute =
                                    true
                            });
                    }
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show(
                        "Could not create the example file:\n\n" +
                        ex.Message,
                        "Error",
                        CustomMessageBoxButtons.OK,
                        CustomMessageBoxIcon.Error);
                }
            }
        }

        // =========================================================
        // REFRESH QUESTION LIST
        // =========================================================

        private void RefreshQuestionList()
        {
            lstQuestions.Items.Clear();

            questionItemMap.Clear();

            if (importedQuestions == null ||
                importedQuestions.Count == 0)
            {
                return;
            }

            int displayNumber =
                1;

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

                if (type ==
                    sectionType)
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

                int itemIndex =
                    lstQuestions.Items.Add(
                        displayNumber +
                        ". " +
                        questionText);

                questionItemMap[itemIndex] =
                    q;

                displayNumber++;
            }

            lstQuestions.Items.Add("");
        }

        // =========================================================
        // DOUBLE CLICK QUESTION
        // =========================================================

        private void LstQuestions_DoubleClick(
            object sender,
            EventArgs e)
        {
            EditSelectedQuestion();
        }

        // =========================================================
        // EDIT BUTTON
        // =========================================================

        private void BtnEditQuestion_Click(
            object sender,
            EventArgs e)
        {
            EditSelectedQuestion();
        }

        // =========================================================
        // EDIT SELECTED QUESTION
        // =========================================================

        private void EditSelectedQuestion()
        {
            if (lstQuestions.SelectedIndex < 0)
            {
                CustomMessageBox.Show(
                    "Please select a question first.\n\n" +
                    "You can also double-click a question to edit it.",
                    "Edit Question",
                    CustomMessageBoxButtons.OK,
                    CustomMessageBoxIcon.Information);

                return;
            }

            int selectedIndex =
                lstQuestions.SelectedIndex;

            if (!questionItemMap.ContainsKey(
                selectedIndex))
            {
                CustomMessageBox.Show(
                    "Please select an actual question.\n\n" +
                    "Section headers cannot be edited.",
                    "Edit Question",
                    CustomMessageBoxButtons.OK,
                    CustomMessageBoxIcon.Information);

                return;
            }

            QuizQuestion selectedQuestion =
                questionItemMap[selectedIndex];

            if (selectedQuestion == null)
            {
                return;
            }

            using (QuestionEditorForm editor =
                   new QuestionEditorForm(
                       selectedQuestion))
            {
                if (editor.ShowDialog(this) ==
                    DialogResult.OK)
                {
                    RefreshQuestionList();

                    lblQuestionCount.Text =
                        "Questions: " +
                        importedQuestions.Count;

                    CustomMessageBox.Show(
                        "Question updated successfully.\n\n" +
                        "You do not need to upload the DOCX again.\n\n" +
                        "The updated question will be used when " +
                        "you submit the Quiz / Exam.",
                        "Question Updated",
                        CustomMessageBoxButtons.OK,
                        CustomMessageBoxIcon.Information);
                }
            }
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

        private async void BtnSaveQuiz_Click(
            object sender,
            EventArgs e)
        {
            if (cmbAssessmentType.SelectedItem == null)
            {
                CustomMessageBox.Show(
                    "Please select Quiz or Exam.",
                    "Missing Type",
                    CustomMessageBoxButtons.OK,
                    CustomMessageBoxIcon.Warning);

                return;
            }

            if (cmbExamPeriod.SelectedItem == null)
            {
                CustomMessageBox.Show(
                    "Please select the Exam Period.\n\n" +
                    "Choose PRELIM, MIDTERM, SEMIFINALS, or FINALS.",
                    "Missing Exam Period",
                    CustomMessageBoxButtons.OK,
                    CustomMessageBoxIcon.Warning);

                cmbExamPeriod.Focus();

                return;
            }

            string title =
                txtQuizTitle.Text.Trim();

            if (string.IsNullOrWhiteSpace(title))
            {
                CustomMessageBox.Show(
                    "Please enter the title.",
                    "Missing Title",
                    CustomMessageBoxButtons.OK,
                    CustomMessageBoxIcon.Warning);

                txtQuizTitle.Focus();

                return;
            }

            string subject =
                txtSubject.Text.Trim();

            if (string.IsNullOrWhiteSpace(subject))
            {
                CustomMessageBox.Show(
                    "Please enter the subject.",
                    "Missing Subject",
                    CustomMessageBoxButtons.OK,
                    CustomMessageBoxIcon.Warning);

                txtSubject.Focus();

                return;
            }

            // =====================================================
            // VALIDATE TIME LIMIT
            // =====================================================

            int durationMinutes =
                (Convert.ToInt32(numHours.Value) * 60) +
                Convert.ToInt32(numMinutes.Value);

            if (durationMinutes < 1)
            {
                CustomMessageBox.Show(
                    "Time limit must be at least 1 minute.",
                    "Invalid Time Limit",
                    CustomMessageBoxButtons.OK,
                    CustomMessageBoxIcon.Warning);

                numHours.Focus();

                return;
            }

            if (durationMinutes > 1440)
            {
                CustomMessageBox.Show(
                    "Time limit cannot exceed 24 hours (1440 minutes).",
                    "Invalid Time Limit",
                    CustomMessageBoxButtons.OK,
                    CustomMessageBoxIcon.Warning);

                numHours.Focus();

                return;
            }

            if (importedQuestions == null ||
                importedQuestions.Count == 0)
            {
                CustomMessageBox.Show(
                    "Please import a DOCX containing questions first.",
                    "No Questions",
                    CustomMessageBoxButtons.OK,
                    CustomMessageBoxIcon.Warning);

                return;
            }

            // =====================================================
            // VALIDATE EDITED QUESTIONS
            // =====================================================

            string validationError =
                ValidateQuestions();

            if (!string.IsNullOrWhiteSpace(
                validationError))
            {
                CustomMessageBox.Show(
                    validationError,
                    "Question Validation Error",
                    CustomMessageBoxButtons.OK,
                    CustomMessageBoxIcon.Warning);

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

            var confirm =
                CustomMessageBox.Show(
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
                    FormatDuration(
                        durationMinutes) +
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
                    CustomMessageBoxButtons.YesNo,
                    CustomMessageBoxIcon.Question);

            if (confirm !=
                CustomMessageBoxResult.Yes)
            {
                return;
            }

            string connStr =
                SettingsManager.Current
                .GetConnectionString();

            int quizId =
                0;

            Exception saveError =
                null;

            using (new LoadingOverlay(
                this,
                "Submitting " +
                assessmentType))
            {
                try
                {
                    quizId =
                        await Task.Run(
                            () =>
                                SaveQuizToDatabase(
                                    connStr,
                                    title,
                                    assessmentType,
                                    subject,
                                    examPeriod,
                                    durationMinutes));
                }
                catch (Exception ex)
                {
                    saveError =
                        ex;
                }
            }

            if (saveError != null)
            {
                CustomMessageBox.Show(
                    "The " +
                    assessmentType.ToUpper() +
                    " was not submitted.\n\n" +
                    saveError.Message,
                    "Database Error",
                    CustomMessageBoxButtons.OK,
                    CustomMessageBoxIcon.Error);

                return;
            }

            monitoredQuizId =
                quizId;

            btnMonitor.Enabled =
                true;

            StartMonitoring(
                monitoredQuizId);

            CustomMessageBox.Show(
                assessmentType.ToUpper() +
                " submitted successfully!\n\n" +
                "Quiz ID: " +
                quizId +
                "\n" +
                "Exam Period: " +
                examPeriod +
                "\n" +
                "Time Limit: " +
                FormatDuration(
                    durationMinutes) +
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
                CustomMessageBoxButtons.OK,
                CustomMessageBoxIcon.Information);

            ClearForm(
                false);
        }

        // =========================================================
        // SAVE QUIZ TO DATABASE
        // =========================================================

        private int SaveQuizToDatabase(
            string connStr,
            string title,
            string assessmentType,
            string subject,
            string examPeriod,
            int durationMinutes)
        {
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

                        if (q == null)
                        {
                            continue;
                        }

                        string questionType =
                            NormalizeQuestionType(
                                q.QuestionType,
                                true);

                        string choiceA =
                            null;

                        string choiceB =
                            null;

                        string choiceC =
                            null;

                        string choiceD =
                            null;

                        string correctAnswer =
                            null;

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
                            correctAnswer =
                                null;
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

                    transaction.Commit();

                    return quizId;
                }
                catch
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

                    throw;
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
                CustomMessageBox.Show(
                    "No quiz is currently selected for monitoring.",
                    "Monitoring",
                    CustomMessageBoxButtons.OK,
                    CustomMessageBoxIcon.Information);

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
                                    reader["full_name"] ==
                                    DBNull.Value
                                        ? "Unknown Student"
                                        : reader["full_name"]
                                            .ToString();

                                string dbStatus =
                                    reader["status"] ==
                                    DBNull.Value
                                        ? ""
                                        : reader["status"]
                                            .ToString();

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

                                dgvAttempts.Rows.Add(
                                    fullName,
                                    displayStatus);
                            }
                        }
                    }
                }
            }
            catch
            {
                // Silent.
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

            if (status ==
                "SUBMITTED")
            {
                return "Submitted";
            }

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
                    return "Taking Quiz";
                }

                return "Disconnected";
            }

            if (status ==
                "TAKING")
            {
                return "Disconnected";
            }

            return status;
        }

        // =========================================================
        // COUNT QUESTIONS BY TYPE
        // =========================================================

        private int CountQuestionsByType(
            string targetType)
        {
            int count =
                0;

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

                if (type ==
                    targetType)
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

                // =================================================
                // MULTIPLE CHOICE
                // =================================================

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

                // =================================================
                // TRUE / FALSE
                // =================================================

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

                // =================================================
                // IDENTIFICATION
                // =================================================

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

                // =================================================
                // ESSAY
                // =================================================

                else if (questionType ==
                         "essay")
                {
                    // Essay does not require
                    // a correct answer.
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

            if (normalized ==
                    "essay" ||
                normalized ==
                    "essay_question" ||
                normalized ==
                    "essay question")
            {
                return "essay";
            }

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
            var result =
                CustomMessageBox.Show(
                    "Clear the current quiz/exam?",
                    "Clear",
                    CustomMessageBoxButtons.YesNo,
                    CustomMessageBoxIcon.Question);

            if (result ==
                CustomMessageBoxResult.Yes)
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

            if (numHours != null)
            {
                numHours.Value =
                    1;
            }

            if (numMinutes != null)
            {
                numMinutes.Value =
                    0;
            }

            importedQuestions =
                new List<QuizQuestion>();

            questionItemMap.Clear();

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
        // INITIALIZE COMPONENT
        // =========================================================

        private void InitializeComponent()
        {
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

    // =================================================================
    // QUESTION EDITOR FORM
    // =================================================================

    public class QuestionEditorForm : Form
    {
        private QuizQuestion question;

        private ComboBox cmbQuestionType;

        private TextBox txtQuestion;

        private TextBox txtChoiceA;
        private TextBox txtChoiceB;
        private TextBox txtChoiceC;
        private TextBox txtChoiceD;

        private TextBox txtCorrectAnswer;

        private Label lblChoiceA;
        private Label lblChoiceB;
        private Label lblChoiceC;
        private Label lblChoiceD;
        private Label lblCorrectAnswer;

        private Button btnSave;
        private Button btnCancel;

        private Panel choicesPanel;

        private static readonly Color ClrMaroon =
            Color.FromArgb(
                94,
                14,
                33);

        private static readonly Color ClrMaroonDark =
            Color.FromArgb(
                70,
                10,
                24);

        private static readonly Color ClrGold =
            Color.FromArgb(
                198,
                156,
                53);

        private static readonly Color ClrGoldDark =
            Color.FromArgb(
                163,
                126,
                36);

        private static readonly Color ClrPageBg =
            Color.FromArgb(
                250,
                247,
                239);

        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public QuestionEditorForm(
            QuizQuestion questionToEdit)
        {
            question =
                questionToEdit;

            BuildInterface();

            LoadQuestion();
        }

        // =========================================================
        // BUILD EDITOR
        // =========================================================

        private void BuildInterface()
        {
            Text =
                "Edit Question";

            StartPosition =
                FormStartPosition.CenterParent;

            Size =
                new Size(
                    700,
                    650);

            MinimumSize =
                new Size(
                    650,
                    600);

            BackColor =
                ClrPageBg;

            Font =
                new Font(
                    "Segoe UI",
                    9.5F);

            FormBorderStyle =
                FormBorderStyle.FixedDialog;

            MaximizeBox =
                false;

            MinimizeBox =
                false;

            // =====================================================
            // HEADER
            // =====================================================

            Panel header =
                new Panel();

            header.Location =
                new Point(
                    0,
                    0);

            header.Size =
                new Size(
                    ClientSize.Width,
                    80);

            header.BackColor =
                ClrMaroon;

            Controls.Add(
                header);

            Label title =
                new Label();

            title.Text =
                "Edit Imported Question";

            title.Font =
                new Font(
                    "Segoe UI Semibold",
                    18,
                    FontStyle.Bold);

            title.ForeColor =
                Color.White;

            title.AutoSize =
                true;

            title.Location =
                new Point(
                    25,
                    18);

            header.Controls.Add(
                title);

            // =====================================================
            // QUESTION TYPE
            // =====================================================

            Label lblType =
                new Label();

            lblType.Text =
                "Question Type";

            lblType.Font =
                new Font(
                    "Segoe UI Semibold",
                    10,
                    FontStyle.Bold);

            lblType.AutoSize =
                true;

            lblType.Location =
                new Point(
                    30,
                    105);

            Controls.Add(
                lblType);

            cmbQuestionType =
                new ComboBox();

            cmbQuestionType.DropDownStyle =
                ComboBoxStyle.DropDownList;

            cmbQuestionType.Font =
                new Font(
                    "Segoe UI",
                    10);

            cmbQuestionType.Items.Add(
                "Multiple Choice");

            cmbQuestionType.Items.Add(
                "True / False");

            cmbQuestionType.Items.Add(
                "Identification");

            cmbQuestionType.Items.Add(
                "Essay");

            cmbQuestionType.Location =
                new Point(
                    30,
                    130);

            cmbQuestionType.Size =
                new Size(
                    250,
                    32);

            cmbQuestionType.SelectedIndexChanged +=
                CmbQuestionType_SelectedIndexChanged;

            Controls.Add(
                cmbQuestionType);

            // =====================================================
            // QUESTION
            // =====================================================

            Label lblQuestion =
                new Label();

            lblQuestion.Text =
                "Question";

            lblQuestion.Font =
                new Font(
                    "Segoe UI Semibold",
                    10,
                    FontStyle.Bold);

            lblQuestion.AutoSize =
                true;

            lblQuestion.Location =
                new Point(
                    30,
                    180);

            Controls.Add(
                lblQuestion);

            txtQuestion =
                new TextBox();

            txtQuestion.Multiline =
                true;

            txtQuestion.ScrollBars =
                ScrollBars.Vertical;

            txtQuestion.Font =
                new Font(
                    "Segoe UI",
                    10);

            txtQuestion.Location =
                new Point(
                    30,
                    205);

            txtQuestion.Size =
                new Size(
                    620,
                    80);

            Controls.Add(
                txtQuestion);

            // =====================================================
            // CHOICES PANEL
            // =====================================================

            choicesPanel =
                new Panel();

            choicesPanel.Location =
                new Point(
                    30,
                    305);

            choicesPanel.Size =
                new Size(
                    620,
                    230);

            choicesPanel.BackColor =
                Color.White;

            choicesPanel.BorderStyle =
                BorderStyle.FixedSingle;

            Controls.Add(
                choicesPanel);

            // =====================================================
            // CHOICE A
            // =====================================================

            lblChoiceA =
                CreateLabel(
                    "Choice A",
                    15,
                    15);

            choicesPanel.Controls.Add(
                lblChoiceA);

            txtChoiceA =
                CreateTextBox(
                    100,
                    12,
                    480,
                    30);

            choicesPanel.Controls.Add(
                txtChoiceA);

            // =====================================================
            // CHOICE B
            // =====================================================

            lblChoiceB =
                CreateLabel(
                    "Choice B",
                    15,
                    55);

            choicesPanel.Controls.Add(
                lblChoiceB);

            txtChoiceB =
                CreateTextBox(
                    100,
                    52,
                    480,
                    30);

            choicesPanel.Controls.Add(
                txtChoiceB);

            // =====================================================
            // CHOICE C
            // =====================================================

            lblChoiceC =
                CreateLabel(
                    "Choice C",
                    15,
                    95);

            choicesPanel.Controls.Add(
                lblChoiceC);

            txtChoiceC =
                CreateTextBox(
                    100,
                    92,
                    480,
                    30);

            choicesPanel.Controls.Add(
                txtChoiceC);

            // =====================================================
            // CHOICE D
            // =====================================================

            lblChoiceD =
                CreateLabel(
                    "Choice D",
                    15,
                    135);

            choicesPanel.Controls.Add(
                lblChoiceD);

            txtChoiceD =
                CreateTextBox(
                    100,
                    132,
                    480,
                    30);

            choicesPanel.Controls.Add(
                txtChoiceD);

            // =====================================================
            // CORRECT ANSWER
            // =====================================================

            lblCorrectAnswer =
                CreateLabel(
                    "Correct Answer",
                    15,
                    175);

            choicesPanel.Controls.Add(
                lblCorrectAnswer);

            txtCorrectAnswer =
                CreateTextBox(
                    140,
                    172,
                    440,
                    30);

            choicesPanel.Controls.Add(
                txtCorrectAnswer);

            // =====================================================
            // SAVE CHANGES
            // =====================================================

            btnSave =
                new Button();

            btnSave.Text =
                "✓  SAVE CHANGES";

            btnSave.Font =
                new Font(
                    "Segoe UI Semibold",
                    10,
                    FontStyle.Bold);

            btnSave.BackColor =
                ClrMaroon;

            btnSave.ForeColor =
                Color.White;

            btnSave.FlatStyle =
                FlatStyle.Flat;

            btnSave.FlatAppearance.BorderSize =
                0;

            btnSave.Size =
                new Size(
                    170,
                    42);

            btnSave.Location =
                new Point(
                    300,
                    555);

            btnSave.Click +=
                BtnSave_Click;

            Controls.Add(
                btnSave);

            // =====================================================
            // CANCEL
            // =====================================================

            btnCancel =
                new Button();

            btnCancel.Text =
                "CANCEL";

            btnCancel.Font =
                new Font(
                    "Segoe UI Semibold",
                    10,
                    FontStyle.Bold);

            btnCancel.BackColor =
                Color.FromArgb(
                    40,
                    40,
                    40);

            btnCancel.ForeColor =
                Color.White;

            btnCancel.FlatStyle =
                FlatStyle.Flat;

            btnCancel.FlatAppearance.BorderSize =
                0;

            btnCancel.Size =
                new Size(
                    130,
                    42);

            btnCancel.Location =
                new Point(
                    480,
                    555);

            btnCancel.Click +=
                (s, e) =>
                {
                    DialogResult =
                        DialogResult.Cancel;

                    Close();
                };

            Controls.Add(
                btnCancel);
        }

        // =========================================================
        // LABEL HELPER
        // =========================================================

        private Label CreateLabel(
            string text,
            int x,
            int y)
        {
            Label label =
                new Label();

            label.Text =
                text;

            label.Font =
                new Font(
                    "Segoe UI Semibold",
                    9,
                    FontStyle.Bold);

            label.ForeColor =
                Color.FromArgb(
                    50,
                    50,
                    50);

            label.AutoSize =
                true;

            label.Location =
                new Point(
                    x,
                    y + 6);

            return label;
        }

        // =========================================================
        // TEXTBOX HELPER
        // =========================================================

        private TextBox CreateTextBox(
            int x,
            int y,
            int width,
            int height)
        {
            TextBox textbox =
                new TextBox();

            textbox.Font =
                new Font(
                    "Segoe UI",
                    9.5F);

            textbox.BorderStyle =
                BorderStyle.FixedSingle;

            textbox.Location =
                new Point(
                    x,
                    y);

            textbox.Size =
                new Size(
                    width,
                    height);

            return textbox;
        }

        // =========================================================
        // LOAD QUESTION
        // =========================================================

        private void LoadQuestion()
        {
            if (question == null)
            {
                return;
            }

            string type =
                question.QuestionType == null
                    ? ""
                    : question.QuestionType
                        .Trim()
                        .ToLower();

            if (type ==
                    "multiple_choice" ||
                type ==
                    "multiplechoice" ||
                type ==
                    "multiple choice" ||
                type ==
                    "mc")
            {
                cmbQuestionType.SelectedIndex =
                    0;
            }
            else if (type ==
                         "true_false" ||
                     type ==
                         "truefalse" ||
                     type ==
                         "true or false" ||
                     type ==
                         "tf")
            {
                cmbQuestionType.SelectedIndex =
                    1;
            }
            else if (type ==
                         "identification" ||
                     type ==
                         "identification_question" ||
                     type ==
                         "identification question" ||
                     type ==
                         "identify" ||
                     type ==
                         "id")
            {
                cmbQuestionType.SelectedIndex =
                    2;
            }
            else
            {
                cmbQuestionType.SelectedIndex =
                    3;
            }

            txtQuestion.Text =
                question.Question ?? "";

            txtChoiceA.Text =
                question.ChoiceA ?? "";

            txtChoiceB.Text =
                question.ChoiceB ?? "";

            txtChoiceC.Text =
                question.ChoiceC ?? "";

            txtChoiceD.Text =
                question.ChoiceD ?? "";

            txtCorrectAnswer.Text =
                question.CorrectAnswer ?? "";

            UpdateQuestionTypeControls();
        }

        // =========================================================
        // QUESTION TYPE CHANGED
        // =========================================================

        private void CmbQuestionType_SelectedIndexChanged(
            object sender,
            EventArgs e)
        {
            UpdateQuestionTypeControls();
        }

        // =========================================================
        // UPDATE CONTROLS
        // =========================================================

        private void UpdateQuestionTypeControls()
        {
            if (cmbQuestionType == null)
            {
                return;
            }

            int index =
                cmbQuestionType.SelectedIndex;

            bool multipleChoice =
                index == 0;

            bool trueFalse =
                index == 1;

            bool identification =
                index == 2;

            bool essay =
                index == 3;

            lblChoiceA.Visible =
                multipleChoice;

            lblChoiceB.Visible =
                multipleChoice;

            lblChoiceC.Visible =
                multipleChoice;

            lblChoiceD.Visible =
                multipleChoice;

            txtChoiceA.Visible =
                multipleChoice;

            txtChoiceB.Visible =
                multipleChoice;

            txtChoiceC.Visible =
                multipleChoice;

            txtChoiceD.Visible =
                multipleChoice;

            lblCorrectAnswer.Visible =
                !essay;

            txtCorrectAnswer.Visible =
                !essay;

            if (trueFalse)
            {
                lblCorrectAnswer.Text =
                    "Correct Answer";
            }
            else if (identification)
            {
                lblCorrectAnswer.Text =
                    "Expected Answer";
            }
            else
            {
                lblCorrectAnswer.Text =
                    "Correct Answer";
            }

            // Important:
            // We DO NOT erase existing choices here.
            // This prevents accidental data loss when
            // changing controls.
        }

        // =========================================================
        // SAVE EDITED QUESTION
        // =========================================================

        private void BtnSave_Click(
            object sender,
            EventArgs e)
        {
            if (question == null)
            {
                return;
            }

            string questionText =
                txtQuestion.Text.Trim();

            if (string.IsNullOrWhiteSpace(
                questionText))
            {
                MessageBox.Show(
                    "Question text is required.",
                    "Validation",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                txtQuestion.Focus();

                return;
            }

            if (cmbQuestionType.SelectedIndex < 0)
            {
                MessageBox.Show(
                    "Please select a question type.",
                    "Validation",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            string newType;

            if (cmbQuestionType.SelectedIndex == 0)
            {
                newType =
                    "multiple_choice";
            }
            else if (cmbQuestionType.SelectedIndex == 1)
            {
                newType =
                    "true_false";
            }
            else if (cmbQuestionType.SelectedIndex == 2)
            {
                newType =
                    "identification";
            }
            else
            {
                newType =
                    "essay";
            }

            // =====================================================
            // MULTIPLE CHOICE
            // =====================================================

            if (newType ==
                "multiple_choice")
            {
                if (string.IsNullOrWhiteSpace(
                    txtChoiceA.Text) ||
                    string.IsNullOrWhiteSpace(
                    txtChoiceB.Text) ||
                    string.IsNullOrWhiteSpace(
                    txtChoiceC.Text) ||
                    string.IsNullOrWhiteSpace(
                    txtChoiceD.Text))
                {
                    MessageBox.Show(
                        "Multiple Choice requires choices A, B, C, and D.",
                        "Validation",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    return;
                }

                string answer =
                    txtCorrectAnswer.Text
                        .Trim()
                        .ToUpper();

                if (answer != "A" &&
                    answer != "B" &&
                    answer != "C" &&
                    answer != "D")
                {
                    MessageBox.Show(
                        "Correct Answer must be A, B, C, or D.",
                        "Validation",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    txtCorrectAnswer.Focus();

                    return;
                }

                question.ChoiceA =
                    txtChoiceA.Text.Trim();

                question.ChoiceB =
                    txtChoiceB.Text.Trim();

                question.ChoiceC =
                    txtChoiceC.Text.Trim();

                question.ChoiceD =
                    txtChoiceD.Text.Trim();

                question.CorrectAnswer =
                    answer;
            }

            // =====================================================
            // TRUE / FALSE
            // =====================================================

            else if (newType ==
                     "true_false")
            {
                string answer =
                    txtCorrectAnswer.Text
                        .Trim()
                        .ToUpper();

                if (answer == "T")
                {
                    answer =
                        "TRUE";
                }

                if (answer == "F")
                {
                    answer =
                        "FALSE";
                }

                if (answer != "TRUE" &&
                    answer != "FALSE")
                {
                    MessageBox.Show(
                        "True / False Correct Answer must be TRUE or FALSE.",
                        "Validation",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    txtCorrectAnswer.Focus();

                    return;
                }

                question.ChoiceA =
                    "TRUE";

                question.ChoiceB =
                    "FALSE";

                question.ChoiceC =
                    null;

                question.ChoiceD =
                    null;

                question.CorrectAnswer =
                    answer;
            }

            // =====================================================
            // IDENTIFICATION
            // =====================================================

            else if (newType ==
                     "identification")
            {
                string answer =
                    txtCorrectAnswer.Text.Trim();

                if (string.IsNullOrWhiteSpace(
                    answer))
                {
                    MessageBox.Show(
                        "Identification requires an expected answer.",
                        "Validation",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    txtCorrectAnswer.Focus();

                    return;
                }

                question.ChoiceA =
                    null;

                question.ChoiceB =
                    null;

                question.ChoiceC =
                    null;

                question.ChoiceD =
                    null;

                question.CorrectAnswer =
                    answer;
            }

            // =====================================================
            // ESSAY
            // =====================================================

            else if (newType ==
                     "essay")
            {
                question.ChoiceA =
                    null;

                question.ChoiceB =
                    null;

                question.ChoiceC =
                    null;

                question.ChoiceD =
                    null;

                question.CorrectAnswer =
                    null;
            }

            // =====================================================
            // SAVE QUESTION TEXT + TYPE
            // =====================================================

            question.Question =
                questionText;

            question.QuestionType =
                newType;

            DialogResult =
                DialogResult.OK;

            Close();
        }
    }

    // NOTE:
    // RoundedButton class ay nasa LivenessCheckForm.cs mo.
    // Huwag magdagdag ng panibagong RoundedButton class dito
    // para maiwasan ang duplicate/ambiguous class error.
}

