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
        private ComboBox cmbSubject;

        // Time limit
        private NumericUpDown numHours;
        private NumericUpDown numMinutes;

        // Deployment (when students can see the quiz / exam)
        private ComboBox cmbDeployMode;
        private DateTimePicker dtpDeployDate;
        private DateTimePicker dtpDeployTime;

        private RoundedButton btnImportDocx;
        private RoundedButton btnViewExample;
        private RoundedButton btnSaveQuiz;
        private RoundedButton btnClear;
        private RoundedButton btnMonitor;
        private RoundedButton btnEditQuestion;

        private Label lblFileName;
        private Label lblQuestionCount;
        private ListBox lstQuestions;

        // Last quiz created in this window (preselected in Monitoring)
        private int monitoredQuizId = 0;

        // Imported questions
        private List<QuizQuestion> importedQuestions = new List<QuizQuestion>();
        private Dictionary<int, QuizQuestion> questionItemMap = new Dictionary<int, QuizQuestion>();

        // ---- Colors ----
        private static readonly Color ClrMaroon = Color.FromArgb(94, 14, 33);
        private static readonly Color ClrMaroonDark = Color.FromArgb(70, 10, 24);
        private static readonly Color ClrBlack = Color.FromArgb(20, 20, 20);
        private static readonly Color ClrBlackHover = Color.FromArgb(50, 50, 50);
        private static readonly Color ClrLabelGray = Color.FromArgb(50, 50, 50);
        private static readonly Color ClrGold = Color.FromArgb(198, 156, 53);
        private static readonly Color ClrGoldDark = Color.FromArgb(163, 126, 36);
        private static readonly Color ClrPageBg = Color.FromArgb(250, 247, 239);

        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public ProfessorQuizForm(int professorID)
        {
            professorUserId = professorID;

            BuildProfessorInterface();

            this.Load += (s, e) => LoadProfessorSubjects();
        }

        // =========================================================
        // LOAD PROFESSOR'S SUBJECTS INTO THE DROPDOWN
        // =========================================================

        private void LoadProfessorSubjects()
        {
            if (cmbSubject == null) return;

            try
            {
                cmbSubject.Items.Clear();

                string connStr = SettingsManager.Current.GetConnectionString();

                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    string query = @"
                        SELECT DISTINCT class_name
                        FROM professor_class
                        WHERE professor_id = @prof_id
                          AND class_name IS NOT NULL
                          AND class_name <> ''
                        ORDER BY class_name";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@prof_id", professorUserId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string name = reader["class_name"]?.ToString()?.Trim();
                                if (!string.IsNullOrEmpty(name) && !cmbSubject.Items.Contains(name))
                                    cmbSubject.Items.Add(name);
                            }
                        }
                    }
                }

                if (cmbSubject.Items.Count > 0)
                    cmbSubject.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("LoadProfessorSubjects error: " + ex.Message);

                CustomMessageBox.Show(
                    "Unable to load your subjects.\n\n" + ex.Message,
                    "Subjects",
                    CustomMessageBoxButtons.OK,
                    CustomMessageBoxIcon.Warning);
            }
        }

        // =========================================================
        // SMALL UI HELPERS
        // =========================================================

        private Label AddLabel(string text, int x, int y)
        {
            Label label = new Label();
            label.Text = text;
            label.Font = new Font("Segoe UI Semibold", 10, FontStyle.Bold);
            label.ForeColor = ClrLabelGray;
            label.AutoSize = true;
            label.Location = new Point(x, y);
            Controls.Add(label);
            return label;
        }

        private RoundedButton MakeButton(string text, int x, int y, int w, int h,
                                         Color back, Color hover, Color fore, float fontSize = 10F)
        {
            RoundedButton b = new RoundedButton();
            b.Text = text;
            b.Font = new Font("Segoe UI Semibold", fontSize, FontStyle.Bold);
            b.Size = new Size(w, h);
            b.Location = new Point(x, y);
            b.BackColor = back;
            b.HoverColor = hover;
            b.ForeColor = fore;
            Controls.Add(b);
            return b;
        }

        // =========================================================
        // BUILD INTERFACE
        // =========================================================

        private void BuildProfessorInterface()
        {
            Text = "Professor - Create Quiz / Exam";
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(780, 800);
            MinimumSize = new Size(780, 800);
            BackColor = ClrPageBg;
            FormBorderStyle = FormBorderStyle.None;
            Font = new Font("Segoe UI", 9.5F);

            this.Paint += (s, e) =>
            {
                using (var pen = new Pen(ClrGold, 1.5f))
                    e.Graphics.DrawRectangle(pen, 0, 0, this.Width - 1, this.Height - 1);
            };

            int pad = 30;
            int leftWidth = 690;
            int bannerHeight = 100;
            int yShift = 40;
            int dep = 70;                 // extra space used by the Deploy row
            int halfWidth = (leftWidth - 24) / 2;
            int rightX = pad + halfWidth + 24;

            // ---------------- HEADER BANNER ----------------
            Panel bannerPanel = new Panel();
            bannerPanel.Location = new Point(0, 0);
            bannerPanel.Size = new Size(this.Width, bannerHeight);
            bannerPanel.BackColor = ClrMaroon;
            bannerPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            bannerPanel.Paint += (s, e) =>
            {
                using (var goldLine = new Pen(ClrGold, 4f))
                    e.Graphics.DrawLine(goldLine, 0, bannerPanel.Height - 2, bannerPanel.Width, bannerPanel.Height - 2);
            };
            Controls.Add(bannerPanel);

            Label lblBrand = new Label();
            lblBrand.Text = "CDSGA";
            lblBrand.Font = new Font("Segoe UI Semibold", 13, FontStyle.Bold);
            lblBrand.ForeColor = ClrGold;
            lblBrand.AutoSize = true;
            lblBrand.Location = new Point(pad, 16);
            bannerPanel.Controls.Add(lblBrand);

            Label lblHeader = new Label();
            lblHeader.Text = "Create Quiz / Exam";
            lblHeader.Font = new Font("Segoe UI Semibold", 20, FontStyle.Bold);
            lblHeader.ForeColor = Color.White;
            lblHeader.AutoSize = true;
            lblHeader.Location = new Point(pad, 40);
            bannerPanel.Controls.Add(lblHeader);

            Label lblClose = new Label();
            lblClose.Text = "✕";
            lblClose.Font = new Font("Segoe UI", 14);
            lblClose.ForeColor = Color.White;
            lblClose.AutoSize = true;
            lblClose.Cursor = Cursors.Hand;
            lblClose.Location = new Point(this.ClientSize.Width - pad - 16, 36);
            lblClose.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblClose.Click += (s, e) => this.Close();
            bannerPanel.Controls.Add(lblClose);

            // ---------------- LEFT CARD ----------------
            Panel leftCard = CreateCardPanel(pad - 15, bannerHeight + 15, leftWidth + 30, 665);
            Controls.Add(leftCard);

            // ---------------- TITLE ----------------
            AddLabel("Title", pad, 85 + yShift);

            txtQuizTitle = new TextBox();
            txtQuizTitle.Font = new Font("Segoe UI", 10);
            txtQuizTitle.BorderStyle = BorderStyle.FixedSingle;
            txtQuizTitle.Location = new Point(pad, 108 + yShift);
            txtQuizTitle.Size = new Size(leftWidth, 30);
            Controls.Add(txtQuizTitle);

            // ---------------- ASSESSMENT TYPE / EXAM PERIOD ----------------
            AddLabel("Assessment Type", pad, 155 + yShift);
            AddLabel("Exam Period", rightX, 155 + yShift);

            cmbAssessmentType = new ComboBox();
            cmbAssessmentType.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbAssessmentType.Items.Add("Quiz");
            cmbAssessmentType.Items.Add("Exam");
            cmbAssessmentType.SelectedIndex = 0;
            cmbAssessmentType.Font = new Font("Segoe UI", 10);
            cmbAssessmentType.Location = new Point(pad, 178 + yShift);
            cmbAssessmentType.Size = new Size(halfWidth, 30);
            Controls.Add(cmbAssessmentType);

            cmbExamPeriod = new ComboBox();
            cmbExamPeriod.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbExamPeriod.Items.Add("PRELIM");
            cmbExamPeriod.Items.Add("MIDTERM");
            cmbExamPeriod.Items.Add("SEMIFINALS");
            cmbExamPeriod.Items.Add("FINALS");
            cmbExamPeriod.SelectedIndex = 0;
            cmbExamPeriod.Font = new Font("Segoe UI", 10);
            cmbExamPeriod.Location = new Point(rightX, 178 + yShift);
            cmbExamPeriod.Size = new Size(halfWidth, 30);
            Controls.Add(cmbExamPeriod);

            // ---------------- SUBJECT / TIME LIMIT ----------------
            AddLabel("Subject", pad, 225 + yShift);
            AddLabel("Time Limit", rightX, 225 + yShift);

            cmbSubject = new ComboBox();
            cmbSubject.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbSubject.Font = new Font("Segoe UI", 10);
            cmbSubject.Location = new Point(pad, 248 + yShift);
            cmbSubject.Size = new Size(halfWidth, 30);
            Controls.Add(cmbSubject);

            int durY = 248 + yShift;

            numHours = new NumericUpDown();
            numHours.Font = new Font("Segoe UI", 10);
            numHours.BorderStyle = BorderStyle.FixedSingle;
            numHours.Location = new Point(rightX, durY);
            numHours.Size = new Size(70, 30);
            numHours.Minimum = 0;
            numHours.Maximum = 24;
            numHours.Value = 1;
            numHours.TextAlign = HorizontalAlignment.Center;
            Controls.Add(numHours);

            Label lblHoursUnit = new Label();
            lblHoursUnit.Text = "hr";
            lblHoursUnit.Font = new Font("Segoe UI", 9.5F);
            lblHoursUnit.ForeColor = ClrLabelGray;
            lblHoursUnit.AutoSize = true;
            lblHoursUnit.Location = new Point(rightX + 74, durY + 7);
            Controls.Add(lblHoursUnit);

            numMinutes = new NumericUpDown();
            numMinutes.Font = new Font("Segoe UI", 10);
            numMinutes.BorderStyle = BorderStyle.FixedSingle;
            numMinutes.Location = new Point(rightX + 104, durY);
            numMinutes.Size = new Size(70, 30);
            numMinutes.Minimum = 0;
            numMinutes.Maximum = 59;
            numMinutes.Increment = 5;
            numMinutes.Value = 0;
            numMinutes.TextAlign = HorizontalAlignment.Center;
            Controls.Add(numMinutes);

            Label lblMinutesUnit = new Label();
            lblMinutesUnit.Text = "min";
            lblMinutesUnit.Font = new Font("Segoe UI", 9.5F);
            lblMinutesUnit.ForeColor = ClrLabelGray;
            lblMinutesUnit.AutoSize = true;
            lblMinutesUnit.Location = new Point(rightX + 178, durY + 7);
            Controls.Add(lblMinutesUnit);

            // ---------------- DEPLOY TO STUDENTS ----------------
            AddLabel("Deploy to Students", pad, 295 + yShift);
            AddLabel("Deploy Date & Time", rightX, 295 + yShift);

            int depY = 318 + yShift;

            cmbDeployMode = new ComboBox();
            cmbDeployMode.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbDeployMode.Items.Add("Deploy immediately");
            cmbDeployMode.Items.Add("Schedule for later");
            cmbDeployMode.SelectedIndex = 0;
            cmbDeployMode.Font = new Font("Segoe UI", 10);
            cmbDeployMode.Location = new Point(pad, depY);
            cmbDeployMode.Size = new Size(halfWidth, 30);
            cmbDeployMode.SelectedIndexChanged += (s, e) => UpdateDeployControls();
            Controls.Add(cmbDeployMode);

            DateTime defaultDeploy = DateTime.Today.AddDays(1).AddHours(8);

            dtpDeployDate = new DateTimePicker();
            dtpDeployDate.Format = DateTimePickerFormat.Custom;
            dtpDeployDate.CustomFormat = "MMM dd, yyyy";
            dtpDeployDate.Font = new Font("Segoe UI", 10);
            dtpDeployDate.MinDate = DateTime.Today;
            dtpDeployDate.Value = defaultDeploy;
            dtpDeployDate.Location = new Point(rightX, depY);
            dtpDeployDate.Size = new Size(165, 30);
            Controls.Add(dtpDeployDate);

            dtpDeployTime = new DateTimePicker();
            dtpDeployTime.Format = DateTimePickerFormat.Custom;
            dtpDeployTime.CustomFormat = "hh:mm tt";
            dtpDeployTime.ShowUpDown = true;
            dtpDeployTime.Font = new Font("Segoe UI", 10);
            dtpDeployTime.Value = defaultDeploy;
            dtpDeployTime.Location = new Point(rightX + 175, depY);
            dtpDeployTime.Size = new Size(120, 30);
            Controls.Add(dtpDeployTime);

            ToolTip tipDeploy = new ToolTip();
            tipDeploy.SetToolTip(cmbDeployMode,
                "Students will NOT see this quiz/exam until the deploy date and time.");

            UpdateDeployControls();

            // ---------------- IMPORT DOCX ----------------
            btnImportDocx = MakeButton("⬆  IMPORT DOCX", pad, 300 + yShift + dep, 190, 40,
                ClrMaroon, ClrMaroonDark, Color.White, 9.5F);
            btnImportDocx.Click += BtnImportDocx_Click;

            btnViewExample = MakeButton("?", pad + 190 + 8, 298 + yShift + dep, 40, 40,
                ClrGold, ClrGoldDark, ClrMaroonDark, 10F);
            new ToolTip().SetToolTip(btnViewExample,
                "See an example DOCX showing how to format your quiz");
            btnViewExample.Click += BtnViewExample_Click;

            lblFileName = new Label();
            lblFileName.Text = "No DOCX file selected.";
            lblFileName.Font = new Font("Segoe UI Italic", 9F, FontStyle.Italic);
            lblFileName.ForeColor = Color.FromArgb(120, 115, 110);
            lblFileName.AutoSize = true;
            lblFileName.Location = new Point(pad + 250, 312 + yShift + dep);
            Controls.Add(lblFileName);

            lblQuestionCount = new Label();
            lblQuestionCount.Text = "Questions: 0";
            lblQuestionCount.Font = new Font("Segoe UI Semibold", 10, FontStyle.Bold);
            lblQuestionCount.ForeColor = ClrMaroon;
            lblQuestionCount.AutoSize = true;
            lblQuestionCount.Location = new Point(pad + leftWidth - 110, 312 + yShift + dep);
            Controls.Add(lblQuestionCount);

            // ---------------- PREVIEW ----------------
            Label lblPreview = AddLabel("Imported Questions / Exam Structure", pad, 358 + yShift + dep);
            lblPreview.Font = new Font("Segoe UI Semibold", 10.5F, FontStyle.Bold);

            lstQuestions = new ListBox();
            lstQuestions.Font = new Font("Segoe UI", 9.5F);
            lstQuestions.HorizontalScrollbar = true;
            lstQuestions.BorderStyle = BorderStyle.FixedSingle;
            lstQuestions.Location = new Point(pad, 385 + yShift + dep);
            lstQuestions.Size = new Size(leftWidth, 170);
            lstQuestions.DoubleClick += LstQuestions_DoubleClick;
            Controls.Add(lstQuestions);

            // ---------------- EDIT / SUBMIT / MONITOR / CLEAR ----------------
            btnEditQuestion = MakeButton("✎  EDIT SELECTED", pad, 675, 160, 38,
                ClrGold, ClrGoldDark, ClrMaroonDark, 9.5F);
            btnEditQuestion.Click += BtnEditQuestion_Click;

            btnSaveQuiz = MakeButton("✓  SUBMIT", pad + leftWidth - 480, 725, 150, 42,
                ClrMaroon, ClrMaroonDark, Color.White);
            btnSaveQuiz.Click += BtnSaveQuiz_Click;

            btnMonitor = MakeButton("◉  MONITOR", pad + leftWidth - 315, 725, 150, 42,
                ClrGold, ClrGoldDark, ClrMaroonDark);
            btnMonitor.Click += BtnMonitor_Click;

            btnClear = MakeButton("CLEAR", pad + leftWidth - 150, 725, 130, 42,
                ClrBlack, ClrBlackHover, Color.White);
            btnClear.Click += BtnClear_Click;

            leftCard.SendToBack();
        }

        private void UpdateDeployControls()
        {
            bool scheduled = cmbDeployMode != null && cmbDeployMode.SelectedIndex == 1;

            if (dtpDeployDate != null) dtpDeployDate.Enabled = scheduled;
            if (dtpDeployTime != null) dtpDeployTime.Enabled = scheduled;
        }

        private bool IsScheduled()
        {
            return cmbDeployMode != null && cmbDeployMode.SelectedIndex == 1;
        }

        private DateTime GetDeployDateTime()
        {
            return dtpDeployDate.Value.Date + dtpDeployTime.Value.TimeOfDay;
        }

        // =========================================================
        // CARD PANEL HELPER
        // =========================================================

        private Panel CreateCardPanel(int x, int y, int width, int height, int radius = 14)
        {
            Panel panel = new Panel();
            panel.Location = new Point(x, y);
            panel.Size = new Size(width, height);
            panel.BackColor = Color.White;

            panel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                Rectangle bounds = new Rectangle(0, 0, panel.Width - 1, panel.Height - 1);

                using (GraphicsPath path = BuildRoundedRectPath(bounds, radius))
                using (var borderPen = new Pen(ClrGold, 1.6f))
                {
                    e.Graphics.DrawPath(borderPen, path);
                }
            };

            return panel;
        }

        private GraphicsPath BuildRoundedRectPath(Rectangle bounds, int radius)
        {
            int d = radius * 2;
            GraphicsPath path = new GraphicsPath();

            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();

            return path;
        }

        // =========================================================
        // FORMAT DURATION
        // =========================================================

        private string FormatDuration(int totalMinutes)
        {
            int hours = totalMinutes / 60;
            int minutes = totalMinutes % 60;

            if (hours > 0 && minutes > 0) return hours + "h " + minutes + "m";
            if (hours > 0) return hours + "h";

            return minutes + " minute" + (minutes == 1 ? "" : "s");
        }

        // =========================================================
        // IMPORT DOCX
        // =========================================================

        private async void BtnImportDocx_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Title = "Select Quiz / Exam DOCX";
                dialog.Filter = "Word Document (*.docx)|*.docx";
                dialog.Multiselect = false;

                if (dialog.ShowDialog() != DialogResult.OK) return;

                try
                {
                    string selectedDocx = dialog.FileName;
                    QuizImportResult result = null;

                    using (new LoadingOverlay(this, "Importing DOCX"))
                    {
                        result = await Task.Run(() => DocxQuizImporter.Import(selectedDocx));
                    }

                    if (result == null)
                    {
                        CustomMessageBox.Show("Unable to import the DOCX.", "Import Error",
                            CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Error);
                        return;
                    }

                    if (result.Questions == null || result.Questions.Count == 0)
                    {
                        CustomMessageBox.Show("No questions were found in the DOCX.", "No Questions",
                            CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Warning);
                        return;
                    }

                    if (!string.IsNullOrWhiteSpace(result.Title))
                        txtQuizTitle.Text = result.Title;

                    // If the DOCX had a subject, auto-select it when it exists in the dropdown
                    if (!string.IsNullOrWhiteSpace(result.Subject) && cmbSubject != null)
                    {
                        string match = null;

                        foreach (var item in cmbSubject.Items)
                        {
                            if (string.Equals(item?.ToString(), result.Subject.Trim(),
                                StringComparison.OrdinalIgnoreCase))
                            {
                                match = item.ToString();
                                break;
                            }
                        }

                        if (match != null) cmbSubject.SelectedItem = match;
                    }

                    importedQuestions = result.Questions;

                    lblFileName.Text = "File: " + Path.GetFileName(dialog.FileName);
                    lblQuestionCount.Text = "Questions: " + importedQuestions.Count;

                    RefreshQuestionList();

                    CustomMessageBox.Show(
                        "DOCX imported successfully!\n\n" +
                        "Questions found: " + importedQuestions.Count + "\n\n" +
                        "You can now edit any imported question without uploading another DOCX.\n\n" +
                        "Double-click a question or select it and click EDIT SELECTED.",
                        "Import Successful",
                        CustomMessageBoxButtons.OK,
                        CustomMessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show("Error importing DOCX:\n\n" + ex.Message, "Import Error",
                        CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Error);
                }
            }
        }

        // =========================================================
        // VIEW EXAMPLE DOCX
        // =========================================================

        private async void BtnViewExample_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Title = "Save Example Quiz Template";
                dialog.Filter = "Word Document (*.docx)|*.docx";
                dialog.FileName = "Exam_Import_Template_Example.docx";

                if (dialog.ShowDialog() != DialogResult.OK) return;

                try
                {
                    string examplePath = dialog.FileName;

                    using (new LoadingOverlay(this, "Creating template"))
                    {
                        await Task.Run(() => ExampleTemplateGenerator.Generate(examplePath));
                    }

                    var open = CustomMessageBox.Show(
                        "Example template saved!\n\nOpen it now?",
                        "Saved",
                        CustomMessageBoxButtons.YesNo,
                        CustomMessageBoxIcon.Question);

                    if (open == CustomMessageBoxResult.Yes)
                    {
                        System.Diagnostics.Process.Start(
                            new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = dialog.FileName,
                                UseShellExecute = true
                            });
                    }
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show("Could not create the example file:\n\n" + ex.Message, "Error",
                        CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Error);
                }
            }
        }

        // =========================================================
        // QUESTION LIST
        // =========================================================

        private void RefreshQuestionList()
        {
            lstQuestions.Items.Clear();
            questionItemMap.Clear();

            if (importedQuestions == null || importedQuestions.Count == 0) return;

            int displayNumber = 1;

            AddSectionToPreview("multiple_choice", "I. MULTIPLE CHOICE", ref displayNumber);
            AddSectionToPreview("true_false", "II. TRUE / FALSE", ref displayNumber);
            AddSectionToPreview("identification", "III. IDENTIFICATION", ref displayNumber);
            AddSectionToPreview("essay", "IV. ESSAY", ref displayNumber);
        }

        private void AddSectionToPreview(string sectionType, string sectionTitle, ref int displayNumber)
        {
            List<QuizQuestion> sectionQuestions = new List<QuizQuestion>();

            for (int i = 0; i < importedQuestions.Count; i++)
            {
                QuizQuestion q = importedQuestions[i];
                if (q == null) continue;

                if (NormalizeQuestionType(q.QuestionType, false) == sectionType)
                    sectionQuestions.Add(q);
            }

            if (sectionQuestions.Count == 0) return;

            lstQuestions.Items.Add("────────────────────────────────────────");
            lstQuestions.Items.Add(sectionTitle + " (" + sectionQuestions.Count +
                " question" + (sectionQuestions.Count == 1 ? "" : "s") + ")");
            lstQuestions.Items.Add("────────────────────────────────────────");

            for (int i = 0; i < sectionQuestions.Count; i++)
            {
                QuizQuestion q = sectionQuestions[i];
                string questionText = q.Question == null ? "" : q.Question.Trim();

                int itemIndex = lstQuestions.Items.Add(displayNumber + ". " + questionText);
                questionItemMap[itemIndex] = q;
                displayNumber++;
            }

            lstQuestions.Items.Add("");
        }

        private void LstQuestions_DoubleClick(object sender, EventArgs e)
        {
            EditSelectedQuestion();
        }

        private void BtnEditQuestion_Click(object sender, EventArgs e)
        {
            EditSelectedQuestion();
        }

        private void EditSelectedQuestion()
        {
            if (lstQuestions.SelectedIndex < 0)
            {
                CustomMessageBox.Show(
                    "Please select a question first.\n\nYou can also double-click a question to edit it.",
                    "Edit Question",
                    CustomMessageBoxButtons.OK,
                    CustomMessageBoxIcon.Information);
                return;
            }

            int selectedIndex = lstQuestions.SelectedIndex;

            if (!questionItemMap.ContainsKey(selectedIndex))
            {
                CustomMessageBox.Show(
                    "Please select an actual question.\n\nSection headers cannot be edited.",
                    "Edit Question",
                    CustomMessageBoxButtons.OK,
                    CustomMessageBoxIcon.Information);
                return;
            }

            QuizQuestion selectedQuestion = questionItemMap[selectedIndex];
            if (selectedQuestion == null) return;

            using (QuestionEditorForm editor = new QuestionEditorForm(selectedQuestion))
            {
                if (editor.ShowDialog(this) == DialogResult.OK)
                {
                    RefreshQuestionList();
                    lblQuestionCount.Text = "Questions: " + importedQuestions.Count;

                    CustomMessageBox.Show(
                        "Question updated successfully.\n\n" +
                        "You do not need to upload the DOCX again.\n\n" +
                        "The updated question will be used when you submit the Quiz / Exam.",
                        "Question Updated",
                        CustomMessageBoxButtons.OK,
                        CustomMessageBoxIcon.Information);
                }
            }
        }

        // =========================================================
        // SAVE QUIZ / EXAM
        // =========================================================

        private async void BtnSaveQuiz_Click(object sender, EventArgs e)
        {
            if (cmbAssessmentType.SelectedItem == null)
            {
                CustomMessageBox.Show("Please select Quiz or Exam.", "Missing Type",
                    CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Warning);
                return;
            }

            if (cmbExamPeriod.SelectedItem == null)
            {
                CustomMessageBox.Show(
                    "Please select the Exam Period.\n\nChoose PRELIM, MIDTERM, SEMIFINALS, or FINALS.",
                    "Missing Exam Period",
                    CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Warning);
                cmbExamPeriod.Focus();
                return;
            }

            string title = txtQuizTitle.Text.Trim();

            if (string.IsNullOrWhiteSpace(title))
            {
                CustomMessageBox.Show("Please enter the title.", "Missing Title",
                    CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Warning);
                txtQuizTitle.Focus();
                return;
            }

            string subject = cmbSubject.SelectedItem == null
                ? "" : cmbSubject.SelectedItem.ToString().Trim();

            if (string.IsNullOrWhiteSpace(subject))
            {
                CustomMessageBox.Show(
                    "Please select a subject.\n\n" +
                    "If the dropdown is empty, you have no classes assigned yet. Create a class first.",
                    "Missing Subject",
                    CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Warning);
                cmbSubject.Focus();
                return;
            }

            int durationMinutes =
                (Convert.ToInt32(numHours.Value) * 60) + Convert.ToInt32(numMinutes.Value);

            if (durationMinutes < 1)
            {
                CustomMessageBox.Show("Time limit must be at least 1 minute.", "Invalid Time Limit",
                    CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Warning);
                numHours.Focus();
                return;
            }

            if (durationMinutes > 1440)
            {
                CustomMessageBox.Show("Time limit cannot exceed 24 hours (1440 minutes).", "Invalid Time Limit",
                    CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Warning);
                numHours.Focus();
                return;
            }

            // ---- deployment ----
            DateTime? deployAt = null;

            if (IsScheduled())
            {
                DateTime picked = GetDeployDateTime();

                if (picked <= DateTime.Now.AddMinutes(1))
                {
                    CustomMessageBox.Show(
                        "The deploy date and time must be in the future.\n\n" +
                        "Choose a later time, or select \"Deploy immediately\".",
                        "Invalid Deploy Time",
                        CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Warning);
                    dtpDeployDate.Focus();
                    return;
                }

                deployAt = picked;
            }

            if (importedQuestions == null || importedQuestions.Count == 0)
            {
                CustomMessageBox.Show("Please import a DOCX containing questions first.", "No Questions",
                    CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Warning);
                return;
            }

            string validationError = ValidateQuestions();

            if (!string.IsNullOrWhiteSpace(validationError))
            {
                CustomMessageBox.Show(validationError, "Question Validation Error",
                    CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Warning);
                return;
            }

            string assessmentType = cmbAssessmentType.SelectedItem.ToString().ToLower();
            string examPeriod = cmbExamPeriod.SelectedItem.ToString().ToUpper();

            int mcCount = CountQuestionsByType("multiple_choice");
            int tfCount = CountQuestionsByType("true_false");
            int identificationCount = CountQuestionsByType("identification");
            int essayCount = CountQuestionsByType("essay");

            string deployText = deployAt.HasValue
                ? "Scheduled for " + deployAt.Value.ToString("MMM dd, yyyy  hh:mm tt")
                : "Immediately";

            var confirm = CustomMessageBox.Show(
                "Submit this " + assessmentType.ToUpper() + "?\n\n" +
                "Exam Period: " + examPeriod + "\n" +
                "Title: " + title + "\n" +
                "Subject: " + subject + "\n" +
                "Time Limit: " + FormatDuration(durationMinutes) + "\n" +
                "Deploy: " + deployText + "\n\n" +
                "QUESTION STRUCTURE\n" +
                "Multiple Choice: " + mcCount + "\n" +
                "True / False: " + tfCount + "\n" +
                "Identification: " + identificationCount + "\n" +
                "Essay: " + essayCount + "\n\n" +
                "Total Questions: " + importedQuestions.Count,
                "Confirm Submit",
                CustomMessageBoxButtons.YesNo,
                CustomMessageBoxIcon.Question);

            if (confirm != CustomMessageBoxResult.Yes) return;

            string connStr = SettingsManager.Current.GetConnectionString();
            int quizId = 0;
            Exception saveError = null;

            using (new LoadingOverlay(this, "Submitting " + assessmentType))
            {
                try
                {
                    quizId = await Task.Run(() =>
                        SaveQuizToDatabase(connStr, title, assessmentType, subject,
                                           examPeriod, durationMinutes, deployAt));
                }
                catch (Exception ex)
                {
                    saveError = ex;
                }
            }

            if (saveError != null)
            {
                CustomMessageBox.Show(
                    "The " + assessmentType.ToUpper() + " was not submitted.\n\n" + saveError.Message,
                    "Database Error",
                    CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Error);
                return;
            }

            monitoredQuizId = quizId;

            string visibility = deployAt.HasValue
                ? "Students will only see it starting " + deployAt.Value.ToString("MMM dd, yyyy  hh:mm tt") + "."
                : "Students can see it now.";

            CustomMessageBox.Show(
                assessmentType.ToUpper() + " submitted successfully!\n\n" +
                "Quiz ID: " + quizId + "\n" +
                "Exam Period: " + examPeriod + "\n" +
                "Time Limit: " + FormatDuration(durationMinutes) + "\n" +
                "Deploy: " + deployText + "\n\n" +
                "Question Structure:\n" +
                "Multiple Choice: " + mcCount + "\n" +
                "True / False: " + tfCount + "\n" +
                "Identification: " + identificationCount + "\n" +
                "Essay: " + essayCount + "\n\n" +
                "Total Questions: " + importedQuestions.Count + "\n\n" +
                visibility + "\n" +
                "Click MONITOR to watch the students.",
                "Submit Successful",
                CustomMessageBoxButtons.OK,
                CustomMessageBoxIcon.Information);

            ClearForm();
        }

        // =========================================================
        // SAVE QUIZ TO DATABASE
        // =========================================================

        private int SaveQuizToDatabase(string connStr, string title, string assessmentType,
                                       string subject, string examPeriod, int durationMinutes,
                                       DateTime? deployAt)
        {
            using (var connection = new MySqlConnection(connStr))
            {
                connection.Open();

                // Adds quizzes.deploy_at when missing (must run before the transaction)
                bool hasDeploy = QuizSchema.EnsureDeployColumn(connection);

                if (deployAt.HasValue && !hasDeploy)
                {
                    throw new Exception(
                        "Scheduling needs a 'deploy_at' column in the quizzes table, " +
                        "and it could not be created automatically.\n\n" +
                        "Please run this in MySQL:\n" +
                        "ALTER TABLE quizzes ADD COLUMN deploy_at DATETIME NULL;");
                }

                MySqlTransaction transaction = null;

                try
                {
                    transaction = connection.BeginTransaction();

                    // ---------------- INSERT QUIZ ----------------
                    string quizSql = hasDeploy
                        ? @"INSERT INTO quizzes
                            (quiz_title, assessment_type, subject, exam_period,
                             created_by, duration_minutes, deploy_at)
                            VALUES
                            (@quiz_title, @assessment_type, @subject, @exam_period,
                             @created_by, @duration_minutes, @deploy_at);"
                        : @"INSERT INTO quizzes
                            (quiz_title, assessment_type, subject, exam_period,
                             created_by, duration_minutes)
                            VALUES
                            (@quiz_title, @assessment_type, @subject, @exam_period,
                             @created_by, @duration_minutes);";

                    int quizId;

                    using (var command = new MySqlCommand(quizSql, connection, transaction))
                    {
                        command.Parameters.AddWithValue("@quiz_title", title);
                        command.Parameters.AddWithValue("@assessment_type", assessmentType);
                        command.Parameters.AddWithValue("@subject", subject);
                        command.Parameters.AddWithValue("@exam_period", examPeriod);
                        command.Parameters.AddWithValue("@created_by", professorUserId);
                        command.Parameters.AddWithValue("@duration_minutes", durationMinutes);

                        if (hasDeploy)
                            command.Parameters.AddWithValue("@deploy_at", (object)deployAt ?? DBNull.Value);

                        command.ExecuteNonQuery();
                        quizId = Convert.ToInt32(command.LastInsertedId);
                    }

                    // ---------------- INSERT QUESTIONS ----------------
                    string questionSql = @"
                        INSERT INTO questions
                        (quiz_id, question_text, question_type,
                         choice_a, choice_b, choice_c, choice_d, correct_answer)
                        VALUES
                        (@quiz_id, @question_text, @question_type,
                         @choice_a, @choice_b, @choice_c, @choice_d, @correct_answer);";

                    for (int i = 0; i < importedQuestions.Count; i++)
                    {
                        QuizQuestion q = importedQuestions[i];
                        if (q == null) continue;

                        string questionType = NormalizeQuestionType(q.QuestionType, true);

                        string choiceA = null, choiceB = null, choiceC = null, choiceD = null;
                        string correctAnswer = null;

                        if (questionType == "multiple_choice")
                        {
                            choiceA = EmptyToNull(q.ChoiceA);
                            choiceB = EmptyToNull(q.ChoiceB);
                            choiceC = EmptyToNull(q.ChoiceC);
                            choiceD = EmptyToNull(q.ChoiceD);
                            correctAnswer = EmptyToNull(q.CorrectAnswer);

                            if (choiceA == null || choiceB == null || choiceC == null || choiceD == null)
                            {
                                throw new Exception("Question " + (i + 1) +
                                    " is marked as Multiple Choice but one or more choices are missing.\n\n" +
                                    q.Question);
                            }

                            if (correctAnswer == null)
                            {
                                throw new Exception("Question " + (i + 1) +
                                    " is Multiple Choice but has no Correct Answer.\n\n" + q.Question);
                            }

                            correctAnswer = correctAnswer.Trim().ToUpper();

                            if (!IsValidMultipleChoiceAnswer(correctAnswer))
                            {
                                throw new Exception("Question " + (i + 1) +
                                    " has an invalid Multiple Choice answer.\n\n" +
                                    "Correct Answer must be A, B, C, or D.\n\n" + q.Question);
                            }
                        }
                        else if (questionType == "true_false")
                        {
                            choiceA = "TRUE";
                            choiceB = "FALSE";
                            correctAnswer = NormalizeTrueFalseAnswer(q.CorrectAnswer);

                            if (correctAnswer == null)
                            {
                                throw new Exception("Question " + (i + 1) +
                                    " is True/False but the Correct Answer is missing or invalid.\n\n" +
                                    q.Question);
                            }
                        }
                        else if (questionType == "identification")
                        {
                            correctAnswer = EmptyToNull(q.CorrectAnswer);

                            if (correctAnswer == null)
                            {
                                throw new Exception("Question " + (i + 1) +
                                    " is Identification but has no Correct Answer.\n\n" +
                                    "Please provide the expected answer.\n\n" + q.Question);
                            }
                        }
                        // essay: no correct answer

                        using (var command = new MySqlCommand(questionSql, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@quiz_id", quizId);
                            command.Parameters.AddWithValue("@question_text", q.Question.Trim());
                            command.Parameters.AddWithValue("@question_type", questionType);
                            command.Parameters.AddWithValue("@choice_a", (object)choiceA ?? DBNull.Value);
                            command.Parameters.AddWithValue("@choice_b", (object)choiceB ?? DBNull.Value);
                            command.Parameters.AddWithValue("@choice_c", (object)choiceC ?? DBNull.Value);
                            command.Parameters.AddWithValue("@choice_d", (object)choiceD ?? DBNull.Value);
                            command.Parameters.AddWithValue("@correct_answer", (object)correctAnswer ?? DBNull.Value);
                            command.ExecuteNonQuery();
                        }
                    }

                    transaction.Commit();

                    return quizId;
                }
                catch
                {
                    try { if (transaction != null) transaction.Rollback(); } catch { }
                    throw;
                }
            }
        }

        // =========================================================
        // MONITOR BUTTON  (opens the Monitoring window)
        // =========================================================

        private void BtnMonitor_Click(object sender, EventArgs e)
        {
            using (MonitoringForm monitor = new MonitoringForm(professorUserId, monitoredQuizId))
            {
                monitor.ShowDialog(this);
            }
        }

        // =========================================================
        // COUNT / VALIDATE
        // =========================================================

        private int CountQuestionsByType(string targetType)
        {
            int count = 0;

            if (importedQuestions == null) return 0;

            for (int i = 0; i < importedQuestions.Count; i++)
            {
                QuizQuestion q = importedQuestions[i];
                if (q == null) continue;

                if (NormalizeQuestionType(q.QuestionType, false) == targetType)
                    count++;
            }

            return count;
        }

        private string ValidateQuestions()
        {
            for (int i = 0; i < importedQuestions.Count; i++)
            {
                QuizQuestion q = importedQuestions[i];
                string no = "Question " + (i + 1);

                if (q == null) return no + " is empty.";

                if (string.IsNullOrWhiteSpace(q.Question))
                    return no + " has no question text.";

                string questionType;

                try
                {
                    questionType = NormalizeQuestionType(q.QuestionType, true);
                }
                catch (Exception ex)
                {
                    return no + " has an unsupported question type.\n\n" + ex.Message;
                }

                if (questionType == "multiple_choice")
                {
                    if (string.IsNullOrWhiteSpace(q.ChoiceA))
                        return no + " is Multiple Choice but Choice A is missing.\n\n" + q.Question;
                    if (string.IsNullOrWhiteSpace(q.ChoiceB))
                        return no + " is Multiple Choice but Choice B is missing.\n\n" + q.Question;
                    if (string.IsNullOrWhiteSpace(q.ChoiceC))
                        return no + " is Multiple Choice but Choice C is missing.\n\n" + q.Question;
                    if (string.IsNullOrWhiteSpace(q.ChoiceD))
                        return no + " is Multiple Choice but Choice D is missing.\n\n" + q.Question;

                    if (string.IsNullOrWhiteSpace(q.CorrectAnswer))
                        return no + " is Multiple Choice but the Correct Answer is missing.\n\n" + q.Question;

                    if (!IsValidMultipleChoiceAnswer(q.CorrectAnswer))
                        return no + " has an invalid Multiple Choice Correct Answer.\n\nUse A, B, C, or D.\n\n" + q.Question;
                }
                else if (questionType == "true_false")
                {
                    if (string.IsNullOrWhiteSpace(q.CorrectAnswer))
                        return no + " is True/False but has no Correct Answer.\n\n" + q.Question;

                    if (NormalizeTrueFalseAnswer(q.CorrectAnswer) == null)
                        return no + " has an invalid True/False answer.\n\nUse TRUE or FALSE.\n\n" + q.Question;
                }
                else if (questionType == "identification")
                {
                    if (string.IsNullOrWhiteSpace(q.CorrectAnswer))
                    {
                        return no + " is Identification but has no Correct Answer.\n\n" +
                               "Identification questions need an expected answer for automatic checking.\n\n" +
                               q.Question;
                    }
                }
            }

            return null;
        }

        private string NormalizeQuestionType(string type, bool throwOnUnknown)
        {
            if (string.IsNullOrWhiteSpace(type))
            {
                if (throwOnUnknown) throw new Exception("Question type is missing.");
                return "unknown";
            }

            string normalized = type.Trim().ToLower().Replace("-", "_").Replace("/", "_");

            if (normalized == "multiple_choice" || normalized == "multiplechoice" ||
                normalized == "multiple choice" || normalized == "mc")
                return "multiple_choice";

            if (normalized == "true_false" || normalized == "truefalse" ||
                normalized == "true or false" || normalized == "true_false_question" ||
                normalized == "true/false" || normalized == "tf")
                return "true_false";

            if (normalized == "identification" || normalized == "identification_question" ||
                normalized == "identification question" || normalized == "identify" ||
                normalized == "id" || normalized == "fill_in_the_blank" ||
                normalized == "fill in the blank" || normalized == "fillintheblank")
                return "identification";

            if (normalized == "essay" || normalized == "essay_question" ||
                normalized == "essay question")
                return "essay";

            if (throwOnUnknown)
            {
                throw new Exception(
                    "Unsupported question type: \"" + type + "\".\n\n" +
                    "Supported types are:\n" +
                    "• Multiple Choice\n• True / False\n• Identification\n• Essay");
            }

            return "unknown";
        }

        private string NormalizeTrueFalseAnswer(string answer)
        {
            if (string.IsNullOrWhiteSpace(answer)) return null;

            string value = answer.Trim().ToUpper();

            if (value == "TRUE" || value == "T") return "TRUE";
            if (value == "FALSE" || value == "F") return "FALSE";

            return null;
        }

        private bool IsValidMultipleChoiceAnswer(string answer)
        {
            if (string.IsNullOrWhiteSpace(answer)) return false;

            string value = answer.Trim().ToUpper();

            return value == "A" || value == "B" || value == "C" || value == "D";
        }

        private string EmptyToNull(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        // =========================================================
        // CLEAR
        // =========================================================

        private void BtnClear_Click(object sender, EventArgs e)
        {
            var result = CustomMessageBox.Show(
                "Clear the current quiz/exam?",
                "Clear",
                CustomMessageBoxButtons.YesNo,
                CustomMessageBoxIcon.Question);

            if (result == CustomMessageBoxResult.Yes) ClearForm();
        }

        private void ClearForm()
        {
            txtQuizTitle.Clear();

            if (cmbSubject != null && cmbSubject.Items.Count > 0)
                cmbSubject.SelectedIndex = 0;

            cmbAssessmentType.SelectedIndex = 0;
            cmbExamPeriod.SelectedIndex = 0;

            if (numHours != null) numHours.Value = 1;
            if (numMinutes != null) numMinutes.Value = 0;

            if (cmbDeployMode != null) cmbDeployMode.SelectedIndex = 0;

            DateTime defaultDeploy = DateTime.Today.AddDays(1).AddHours(8);
            if (dtpDeployDate != null) dtpDeployDate.Value = defaultDeploy;
            if (dtpDeployTime != null) dtpDeployTime.Value = defaultDeploy;

            importedQuestions = new List<QuizQuestion>();
            questionItemMap.Clear();
            lstQuestions.Items.Clear();

            lblFileName.Text = "No DOCX file selected.";
            lblQuestionCount.Text = "Questions: 0";
        }

        private void InitializeComponent()
        {
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
        private TextBox txtChoiceA, txtChoiceB, txtChoiceC, txtChoiceD;
        private TextBox txtCorrectAnswer;
        private Label lblChoiceA, lblChoiceB, lblChoiceC, lblChoiceD, lblCorrectAnswer;
        private Button btnSave;
        private Button btnCancel;
        private Panel choicesPanel;

        private static readonly Color ClrMaroon = Color.FromArgb(94, 14, 33);
        private static readonly Color ClrPageBg = Color.FromArgb(250, 247, 239);

        public QuestionEditorForm(QuizQuestion questionToEdit)
        {
            question = questionToEdit;

            BuildInterface();
            LoadQuestion();
        }

        private void BuildInterface()
        {
            Text = "Edit Question";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(700, 650);
            MinimumSize = new Size(650, 600);
            BackColor = ClrPageBg;
            Font = new Font("Segoe UI", 9.5F);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            Panel header = new Panel();
            header.Location = new Point(0, 0);
            header.Size = new Size(ClientSize.Width, 80);
            header.BackColor = ClrMaroon;
            Controls.Add(header);

            Label title = new Label();
            title.Text = "Edit Imported Question";
            title.Font = new Font("Segoe UI Semibold", 18, FontStyle.Bold);
            title.ForeColor = Color.White;
            title.AutoSize = true;
            title.Location = new Point(25, 18);
            header.Controls.Add(title);

            Label lblType = new Label();
            lblType.Text = "Question Type";
            lblType.Font = new Font("Segoe UI Semibold", 10, FontStyle.Bold);
            lblType.AutoSize = true;
            lblType.Location = new Point(30, 105);
            Controls.Add(lblType);

            cmbQuestionType = new ComboBox();
            cmbQuestionType.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbQuestionType.Font = new Font("Segoe UI", 10);
            cmbQuestionType.Items.Add("Multiple Choice");
            cmbQuestionType.Items.Add("True / False");
            cmbQuestionType.Items.Add("Identification");
            cmbQuestionType.Items.Add("Essay");
            cmbQuestionType.Location = new Point(30, 130);
            cmbQuestionType.Size = new Size(250, 32);
            cmbQuestionType.SelectedIndexChanged += (s, e) => UpdateQuestionTypeControls();
            Controls.Add(cmbQuestionType);

            Label lblQuestion = new Label();
            lblQuestion.Text = "Question";
            lblQuestion.Font = new Font("Segoe UI Semibold", 10, FontStyle.Bold);
            lblQuestion.AutoSize = true;
            lblQuestion.Location = new Point(30, 180);
            Controls.Add(lblQuestion);

            txtQuestion = new TextBox();
            txtQuestion.Multiline = true;
            txtQuestion.ScrollBars = ScrollBars.Vertical;
            txtQuestion.Font = new Font("Segoe UI", 10);
            txtQuestion.Location = new Point(30, 205);
            txtQuestion.Size = new Size(620, 80);
            Controls.Add(txtQuestion);

            choicesPanel = new Panel();
            choicesPanel.Location = new Point(30, 305);
            choicesPanel.Size = new Size(620, 230);
            choicesPanel.BackColor = Color.White;
            choicesPanel.BorderStyle = BorderStyle.FixedSingle;
            Controls.Add(choicesPanel);

            lblChoiceA = CreateLabel("Choice A", 15, 15);
            choicesPanel.Controls.Add(lblChoiceA);
            txtChoiceA = CreateTextBox(100, 12, 480, 30);
            choicesPanel.Controls.Add(txtChoiceA);

            lblChoiceB = CreateLabel("Choice B", 15, 55);
            choicesPanel.Controls.Add(lblChoiceB);
            txtChoiceB = CreateTextBox(100, 52, 480, 30);
            choicesPanel.Controls.Add(txtChoiceB);

            lblChoiceC = CreateLabel("Choice C", 15, 95);
            choicesPanel.Controls.Add(lblChoiceC);
            txtChoiceC = CreateTextBox(100, 92, 480, 30);
            choicesPanel.Controls.Add(txtChoiceC);

            lblChoiceD = CreateLabel("Choice D", 15, 135);
            choicesPanel.Controls.Add(lblChoiceD);
            txtChoiceD = CreateTextBox(100, 132, 480, 30);
            choicesPanel.Controls.Add(txtChoiceD);

            lblCorrectAnswer = CreateLabel("Correct Answer", 15, 175);
            choicesPanel.Controls.Add(lblCorrectAnswer);
            txtCorrectAnswer = CreateTextBox(140, 172, 440, 30);
            choicesPanel.Controls.Add(txtCorrectAnswer);

            btnSave = new Button();
            btnSave.Text = "✓  SAVE CHANGES";
            btnSave.Font = new Font("Segoe UI Semibold", 10, FontStyle.Bold);
            btnSave.BackColor = ClrMaroon;
            btnSave.ForeColor = Color.White;
            btnSave.FlatStyle = FlatStyle.Flat;
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Size = new Size(170, 42);
            btnSave.Location = new Point(300, 555);
            btnSave.Click += BtnSave_Click;
            Controls.Add(btnSave);

            btnCancel = new Button();
            btnCancel.Text = "CANCEL";
            btnCancel.Font = new Font("Segoe UI Semibold", 10, FontStyle.Bold);
            btnCancel.BackColor = Color.FromArgb(40, 40, 40);
            btnCancel.ForeColor = Color.White;
            btnCancel.FlatStyle = FlatStyle.Flat;
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Size = new Size(130, 42);
            btnCancel.Location = new Point(480, 555);
            btnCancel.Click += (s, e) =>
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };
            Controls.Add(btnCancel);
        }

        private Label CreateLabel(string text, int x, int y)
        {
            Label label = new Label();
            label.Text = text;
            label.Font = new Font("Segoe UI Semibold", 9, FontStyle.Bold);
            label.ForeColor = Color.FromArgb(50, 50, 50);
            label.AutoSize = true;
            label.Location = new Point(x, y + 6);
            return label;
        }

        private TextBox CreateTextBox(int x, int y, int width, int height)
        {
            TextBox textbox = new TextBox();
            textbox.Font = new Font("Segoe UI", 9.5F);
            textbox.BorderStyle = BorderStyle.FixedSingle;
            textbox.Location = new Point(x, y);
            textbox.Size = new Size(width, height);
            return textbox;
        }

        private void LoadQuestion()
        {
            if (question == null) return;

            string type = question.QuestionType == null
                ? "" : question.QuestionType.Trim().ToLower();

            if (type == "multiple_choice" || type == "multiplechoice" ||
                type == "multiple choice" || type == "mc")
                cmbQuestionType.SelectedIndex = 0;
            else if (type == "true_false" || type == "truefalse" ||
                     type == "true or false" || type == "tf")
                cmbQuestionType.SelectedIndex = 1;
            else if (type == "identification" || type == "identification_question" ||
                     type == "identification question" || type == "identify" || type == "id")
                cmbQuestionType.SelectedIndex = 2;
            else
                cmbQuestionType.SelectedIndex = 3;

            txtQuestion.Text = question.Question ?? "";
            txtChoiceA.Text = question.ChoiceA ?? "";
            txtChoiceB.Text = question.ChoiceB ?? "";
            txtChoiceC.Text = question.ChoiceC ?? "";
            txtChoiceD.Text = question.ChoiceD ?? "";
            txtCorrectAnswer.Text = question.CorrectAnswer ?? "";

            UpdateQuestionTypeControls();
        }

        private void UpdateQuestionTypeControls()
        {
            if (cmbQuestionType == null) return;

            int index = cmbQuestionType.SelectedIndex;

            bool multipleChoice = index == 0;
            bool identification = index == 2;
            bool essay = index == 3;

            lblChoiceA.Visible = multipleChoice;
            lblChoiceB.Visible = multipleChoice;
            lblChoiceC.Visible = multipleChoice;
            lblChoiceD.Visible = multipleChoice;
            txtChoiceA.Visible = multipleChoice;
            txtChoiceB.Visible = multipleChoice;
            txtChoiceC.Visible = multipleChoice;
            txtChoiceD.Visible = multipleChoice;

            lblCorrectAnswer.Visible = !essay;
            txtCorrectAnswer.Visible = !essay;

            lblCorrectAnswer.Text = identification ? "Expected Answer" : "Correct Answer";

            // Existing choices are NOT erased here, to avoid accidental data loss.
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (question == null) return;

            string questionText = txtQuestion.Text.Trim();

            if (string.IsNullOrWhiteSpace(questionText))
            {
                MessageBox.Show("Question text is required.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtQuestion.Focus();
                return;
            }

            if (cmbQuestionType.SelectedIndex < 0)
            {
                MessageBox.Show("Please select a question type.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string newType;

            if (cmbQuestionType.SelectedIndex == 0) newType = "multiple_choice";
            else if (cmbQuestionType.SelectedIndex == 1) newType = "true_false";
            else if (cmbQuestionType.SelectedIndex == 2) newType = "identification";
            else newType = "essay";

            if (newType == "multiple_choice")
            {
                if (string.IsNullOrWhiteSpace(txtChoiceA.Text) ||
                    string.IsNullOrWhiteSpace(txtChoiceB.Text) ||
                    string.IsNullOrWhiteSpace(txtChoiceC.Text) ||
                    string.IsNullOrWhiteSpace(txtChoiceD.Text))
                {
                    MessageBox.Show("Multiple Choice requires choices A, B, C, and D.", "Validation",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string answer = txtCorrectAnswer.Text.Trim().ToUpper();

                if (answer != "A" && answer != "B" && answer != "C" && answer != "D")
                {
                    MessageBox.Show("Correct Answer must be A, B, C, or D.", "Validation",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtCorrectAnswer.Focus();
                    return;
                }

                question.ChoiceA = txtChoiceA.Text.Trim();
                question.ChoiceB = txtChoiceB.Text.Trim();
                question.ChoiceC = txtChoiceC.Text.Trim();
                question.ChoiceD = txtChoiceD.Text.Trim();
                question.CorrectAnswer = answer;
            }
            else if (newType == "true_false")
            {
                string answer = txtCorrectAnswer.Text.Trim().ToUpper();

                if (answer == "T") answer = "TRUE";
                if (answer == "F") answer = "FALSE";

                if (answer != "TRUE" && answer != "FALSE")
                {
                    MessageBox.Show("True / False Correct Answer must be TRUE or FALSE.", "Validation",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtCorrectAnswer.Focus();
                    return;
                }

                question.ChoiceA = "TRUE";
                question.ChoiceB = "FALSE";
                question.ChoiceC = null;
                question.ChoiceD = null;
                question.CorrectAnswer = answer;
            }
            else if (newType == "identification")
            {
                string answer = txtCorrectAnswer.Text.Trim();

                if (string.IsNullOrWhiteSpace(answer))
                {
                    MessageBox.Show("Identification requires an expected answer.", "Validation",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtCorrectAnswer.Focus();
                    return;
                }

                question.ChoiceA = null;
                question.ChoiceB = null;
                question.ChoiceC = null;
                question.ChoiceD = null;
                question.CorrectAnswer = answer;
            }
            else
            {
                question.ChoiceA = null;
                question.ChoiceB = null;
                question.ChoiceC = null;
                question.ChoiceD = null;
                question.CorrectAnswer = null;
            }

            question.Question = questionText;
            question.QuestionType = newType;

            DialogResult = DialogResult.OK;
            Close();
        }
    }

    // NOTE:
    // RoundedButton class ay nasa LivenessCheckForm.cs mo.
    // Huwag magdagdag ng panibagong RoundedButton class dito
    // para maiwasan ang duplicate/ambiguous class error.
}