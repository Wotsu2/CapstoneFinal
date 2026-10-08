using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Guna.UI2.WinForms;
using MySql.Data.MySqlClient;

namespace WinFormsApp1
{
    public class ProfessorQuizForm : Form
    {
        private int professorUserId;

        // ---- Palette (same as the rest of the app) ----
        private static readonly Color ClrMaroon = Color.FromArgb(94, 14, 33);
        private static readonly Color ClrMaroonDark = Color.FromArgb(70, 10, 24);
        private static readonly Color ClrGold = Color.FromArgb(198, 156, 53);
        private static readonly Color ClrGoldDark = Color.FromArgb(163, 126, 36);
        private static readonly Color ClrGoldSoft = Color.FromArgb(230, 210, 180);
        private static readonly Color ClrPageBg = Color.FromArgb(243, 244, 246);
        private static readonly Color ClrCardBg = Color.White;
        private static readonly Color ClrBorder = Color.FromArgb(218, 222, 228);
        private static readonly Color ClrText = Color.FromArgb(31, 41, 55);
        private static readonly Color ClrMuted = Color.FromArgb(107, 114, 128);
        private static readonly Color ClrHoverLight = Color.FromArgb(243, 244, 246);
        private static readonly Color ClrDark = Color.FromArgb(31, 31, 31);
        private static readonly Color ClrDarkHover = Color.FromArgb(60, 60, 60);

        // ---- Controls ----
        private Guna2Panel mainCard;
        private Guna2TextBox txtQuizTitle;
        private Guna2ComboBox cmbAssessmentType;
        private Guna2ComboBox cmbExamPeriod;
        private Guna2ComboBox cmbSubject;
        private Guna2TextBox txtHours;
        private Guna2TextBox txtMinutes;
        private Guna2ComboBox cmbDeployMode;
        private DateTimePicker dtpDeployDate;
        private Guna2TextBox txtDeployTime;

        private Guna2Button btnImportDocx;
        private Guna2Button btnViewExample;
        private Guna2Button btnSaveQuiz;
        private Guna2Button btnClear;
        private Guna2Button btnMonitor;
        private Guna2Button btnEditQuestion;
        private Guna2Button btnApplyPoints;

        private Guna2TextBox txtSelPoints;
        private Label lblFileName;
        private Label lblQuestionCount;
        private Guna2Panel listCard;
        private ListBox lstQuestions;

        // Last quiz created in this window (preselected in Monitoring)
        private int monitoredQuizId = 0;

        // Imported questions
        private List<QuizQuestion> importedQuestions = new List<QuizQuestion>();
        private Dictionary<int, QuizQuestion> questionItemMap = new Dictionary<int, QuizQuestion>();

        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public ProfessorQuizForm(int professorID)
        {
            professorUserId = professorID;

            AutoScaleMode = AutoScaleMode.None;
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
        // BUILD INTERFACE
        // =========================================================

        private void BuildProfessorInterface()
        {
            Text = "Professor - Create Quiz / Exam";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(780, 800);
            BackColor = ClrPageBg;
            FormBorderStyle = FormBorderStyle.None;
            Font = new Font("Segoe UI", 9.5F);

            Paint += (s, e) =>
            {
                using (var pen = new Pen(ClrGold, 1.5f))
                    e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
            };

            // ---------------- MAIN CARD (added first so it stays behind everything) ----------------
            mainCard = MakeCard(20, 116, 740, 668);
            Controls.Add(mainCard);

            // ---------------- HEADER ----------------
            Panel header = new Panel();
            header.Location = new Point(0, 0);
            header.Size = new Size(780, 100);
            header.BackColor = ClrMaroon;

            Panel accent = new Panel();
            accent.Dock = DockStyle.Bottom;
            accent.Height = 3;
            accent.BackColor = ClrGold;
            header.Controls.Add(accent);

            Label lblBrand = new Label();
            lblBrand.Text = "CDSGA";
            lblBrand.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            lblBrand.ForeColor = ClrGold;
            lblBrand.BackColor = Color.Transparent;
            lblBrand.AutoSize = true;
            lblBrand.Location = new Point(30, 16);
            header.Controls.Add(lblBrand);

            Label lblHeader = new Label();
            lblHeader.Text = "Create Quiz / Exam";
            lblHeader.Font = new Font("Segoe UI Semibold", 20F, FontStyle.Bold);
            lblHeader.ForeColor = Color.White;
            lblHeader.BackColor = Color.Transparent;
            lblHeader.AutoSize = true;
            lblHeader.Location = new Point(28, 36);
            header.Controls.Add(lblHeader);

            Label lblClose = new Label();
            lblClose.Text = "✕";
            lblClose.Font = new Font("Segoe UI", 14F);
            lblClose.ForeColor = Color.White;
            lblClose.BackColor = Color.Transparent;
            lblClose.AutoSize = true;
            lblClose.Cursor = Cursors.Hand;
            lblClose.Location = new Point(722, 34);
            lblClose.Click += (s, e) => Close();
            header.Controls.Add(lblClose);

            Controls.Add(header);

            // ---------------- TITLE ----------------
            AddCaption("TITLE", 44, 134);

            txtQuizTitle = MakeTextBox(44, 154, 692, 36);
            txtQuizTitle.PlaceholderText = "Halimbawa: Midterm Exam - Chapter 1";

            // ---------------- ASSESSMENT TYPE / EXAM PERIOD ----------------
            AddCaption("ASSESSMENT TYPE", 44, 204);
            AddCaption("EXAM PERIOD", 404, 204);

            cmbAssessmentType = MakeCombo(44, 224, 332, 36);
            cmbAssessmentType.Items.Add("Quiz");
            cmbAssessmentType.Items.Add("Exam");
            cmbAssessmentType.SelectedIndex = 0;

            cmbExamPeriod = MakeCombo(404, 224, 332, 36);
            cmbExamPeriod.Items.Add("PRELIM");
            cmbExamPeriod.Items.Add("MIDTERM");
            cmbExamPeriod.Items.Add("SEMIFINALS");
            cmbExamPeriod.Items.Add("FINALS");
            cmbExamPeriod.SelectedIndex = 0;

            // ---------------- SUBJECT / TIME LIMIT ----------------
            AddCaption("SUBJECT", 44, 274);
            AddCaption("TIME LIMIT", 404, 274);

            cmbSubject = MakeCombo(44, 294, 332, 36);

            txtHours = MakeTextBox(404, 294, 70, 36);
            txtHours.Text = "1";
            txtHours.TextAlign = HorizontalAlignment.Center;
            txtHours.MaxLength = 2;
            txtHours.KeyPress += DigitsOnly;

            AddCaption("hr", 480, 304, false);

            txtMinutes = MakeTextBox(510, 294, 70, 36);
            txtMinutes.Text = "0";
            txtMinutes.TextAlign = HorizontalAlignment.Center;
            txtMinutes.MaxLength = 2;
            txtMinutes.KeyPress += DigitsOnly;

            AddCaption("min", 586, 304, false);

            // ---------------- DEPLOY ----------------
            AddCaption("DEPLOY TO STUDENTS", 44, 344);
            AddCaption("DEPLOY DATE & TIME", 404, 344);

            cmbDeployMode = MakeCombo(44, 364, 332, 36);
            cmbDeployMode.Items.Add("Deploy immediately");
            cmbDeployMode.Items.Add("Schedule for later");
            cmbDeployMode.SelectedIndex = 0;
            cmbDeployMode.SelectedIndexChanged += (s, e) => UpdateDeployControls();

            DateTime defaultDeploy = DateTime.Today.AddDays(1).AddHours(8);

            dtpDeployDate = new DateTimePicker();
            dtpDeployDate.Format = DateTimePickerFormat.Custom;
            dtpDeployDate.CustomFormat = "MMM dd, yyyy";
            dtpDeployDate.Font = new Font("Segoe UI", 10F);
            dtpDeployDate.Location = new Point(404, 364);
            dtpDeployDate.Size = new Size(190, 36);
            dtpDeployDate.MinDate = DateTime.Today;
            dtpDeployDate.Value = defaultDeploy.Date;
            Controls.Add(dtpDeployDate);

            txtDeployTime = MakeTextBox(606, 364, 130, 36);
            txtDeployTime.Text = defaultDeploy.ToString("hh:mm tt");
            txtDeployTime.PlaceholderText = "08:00 AM";
            txtDeployTime.TextAlign = HorizontalAlignment.Center;
            txtDeployTime.MaxLength = 8;

            UpdateDeployControls();

            // ---------------- IMPORT DOCX ----------------
            btnImportDocx = MakeButton("⬆  IMPORT DOCX", 44, 420, 200, 42,
                ClrMaroon, ClrMaroonDark, Color.White);
            btnImportDocx.Click += BtnImportDocx_Click;

            btnViewExample = MakeButton("?", 254, 420, 42, 42,
                ClrGold, ClrGoldDark, ClrMaroonDark);
            btnViewExample.Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold);
            new ToolTip().SetToolTip(btnViewExample,
                "See an example DOCX showing how to format your quiz");
            btnViewExample.Click += BtnViewExample_Click;

            lblFileName = new Label();
            lblFileName.Text = "No DOCX file selected.";
            lblFileName.Font = new Font("Segoe UI", 9F, FontStyle.Italic);
            lblFileName.ForeColor = Color.FromArgb(120, 115, 110);
            lblFileName.BackColor = Color.White;
            lblFileName.AutoSize = true;
            lblFileName.Location = new Point(308, 432);
            Controls.Add(lblFileName);

            lblQuestionCount = new Label();
            lblQuestionCount.Text = "Questions: 0";
            lblQuestionCount.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            lblQuestionCount.ForeColor = ClrMaroon;
            lblQuestionCount.BackColor = Color.White;
            lblQuestionCount.AutoSize = true;
            lblQuestionCount.Location = new Point(600, 432);
            Controls.Add(lblQuestionCount);

            // ---------------- PREVIEW + POINTS ----------------
            AddCaption("IMPORTED QUESTIONS / EXAM STRUCTURE", 44, 484);

            AddCaption("POINTS", 430, 484);

            txtSelPoints = MakeTextBox(490, 476, 70, 36);
            txtSelPoints.Text = "1";
            txtSelPoints.TextAlign = HorizontalAlignment.Center;
            txtSelPoints.MaxLength = 3;
            txtSelPoints.Enabled = false;
            txtSelPoints.KeyPress += DigitsOnly;

            btnApplyPoints = MakeButton("SET POINTS", 572, 476, 164, 36,
                ClrGold, ClrGoldDark, ClrMaroonDark);
            btnApplyPoints.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            btnApplyPoints.Click += BtnApplyPoints_Click;

            // List inside a rounded card
            listCard = new Guna2Panel();
            listCard.Location = new Point(44, 522);
            listCard.Size = new Size(692, 150);
            listCard.FillColor = Color.White;
            listCard.BorderColor = ClrBorder;
            listCard.BorderThickness = 1;
            listCard.BorderRadius = 12;
            listCard.Padding = new Padding(8);
            Controls.Add(listCard);

            lstQuestions = new ListBox();
            lstQuestions.Dock = DockStyle.Fill;
            lstQuestions.BorderStyle = BorderStyle.None;
            lstQuestions.Font = new Font("Segoe UI", 9.5F);
            lstQuestions.ForeColor = ClrText;
            lstQuestions.BackColor = Color.White;
            lstQuestions.HorizontalScrollbar = true;
            lstQuestions.DoubleClick += LstQuestions_DoubleClick;
            lstQuestions.SelectedIndexChanged += LstQuestions_SelectedIndexChanged;
            listCard.Controls.Add(lstQuestions);

            // ---------------- ACTIONS ----------------
            btnEditQuestion = MakeButton("✎  EDIT SELECTED", 44, 688, 170, 42,
                ClrGold, ClrGoldDark, ClrMaroonDark);
            btnEditQuestion.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold);
            btnEditQuestion.Click += BtnEditQuestion_Click;

            btnSaveQuiz = MakeButton("✓  SUBMIT", 296, 688, 150, 42,
                ClrMaroon, ClrMaroonDark, Color.White);
            btnSaveQuiz.Click += BtnSaveQuiz_Click;

            btnMonitor = MakeButton("◉  MONITOR", 456, 688, 150, 42,
                ClrGold, ClrGoldDark, ClrMaroonDark);
            btnMonitor.Click += BtnMonitor_Click;

            btnClear = MakeButton("CLEAR", 616, 688, 120, 42,
               ClrDark, ClrDarkHover, Color.White);
            btnClear.Click += BtnClear_Click;

            // Ilagay ang card sa likod ng lahat ng controls
            mainCard.SendToBack();

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

        private void AddCaption(string text, int x, int y, bool bold = true)
        {
            Label lbl = new Label();
            lbl.Text = text;
            lbl.Font = bold
                ? new Font("Segoe UI Semibold", 7.5F, FontStyle.Bold)
                : new Font("Segoe UI", 9F);
            lbl.ForeColor = ClrMuted;
            lbl.BackColor = Color.White;
            lbl.AutoSize = true;
            lbl.Location = new Point(x, y);
            Controls.Add(lbl);
        }

        private Guna2TextBox MakeTextBox(int x, int y, int w, int h)
        {
            Guna2TextBox tb = new Guna2TextBox();
            tb.Location = new Point(x, y);
            tb.Size = new Size(w, h);
            tb.FillColor = Color.White;
            tb.BorderColor = ClrBorder;
            tb.BorderThickness = 1;
            tb.BorderRadius = 10;
            tb.ForeColor = ClrText;
            tb.Font = new Font("Segoe UI", 10F);
            tb.FocusedState.BorderColor = ClrMaroon;
            Controls.Add(tb);
            return tb;
        }

        private Guna2ComboBox MakeCombo(int x, int y, int w, int h)
        {
            Guna2ComboBox cb = new Guna2ComboBox();
            cb.DropDownStyle = ComboBoxStyle.DropDownList;
            cb.Location = new Point(x, y);
            cb.Size = new Size(w, h);
            cb.FillColor = Color.White;
            cb.BorderColor = ClrBorder;
            cb.BorderThickness = 1;
            cb.BorderRadius = 10;
            cb.ForeColor = ClrText;
            cb.Font = new Font("Segoe UI", 10F);
            cb.FocusedState.BorderColor = ClrMaroon;
            Controls.Add(cb);
            return cb;
        }

        private Guna2Button MakeButton(string text, int x, int y, int w, int h,
                                       Color fill, Color hover, Color fore)
        {
            Guna2Button b = new Guna2Button();
            b.Text = text;
            b.Location = new Point(x, y);
            b.Size = new Size(w, h);
            b.BorderRadius = h / 2;
            b.FillColor = fill;
            b.ForeColor = fore;
            b.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            b.HoverState.FillColor = hover;
            b.Cursor = Cursors.Hand;
            Controls.Add(b);
            return b;
        }

        private static void DigitsOnly(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                e.Handled = true;
        }

        // =========================================================
        // DEPLOY CONTROLS
        // =========================================================

        private void UpdateDeployControls()
        {
            bool scheduled = IsScheduled();

            if (dtpDeployDate != null) dtpDeployDate.Enabled = scheduled;
            if (txtDeployTime != null) txtDeployTime.Enabled = scheduled;
        }

        private bool IsScheduled()
        {
            return cmbDeployMode != null && cmbDeployMode.SelectedIndex == 1;
        }

        private bool TryGetDeployDateTime(out DateTime result)
        {
            result = DateTime.MinValue;

            DateTime time;
            if (!DateTime.TryParse(txtDeployTime.Text.Trim(), out time))
                return false;

            result = dtpDeployDate.Value.Date + time.TimeOfDay;
            return true;
        }

        // =========================================================
        // POINTS: SELECT A QUESTION, THEN SET ITS POINTS
        // =========================================================

        private void LstQuestions_SelectedIndexChanged(object sender, EventArgs e)
        {
            QuizQuestion q;

            if (lstQuestions.SelectedIndex >= 0 &&
                questionItemMap.TryGetValue(lstQuestions.SelectedIndex, out q) &&
                q != null)
            {
                txtSelPoints.Text = Math.Max(1, Math.Min(100, q.Points)).ToString();
                txtSelPoints.Enabled = true;
            }
            else
            {
                txtSelPoints.Enabled = false;
            }
        }

        private void BtnApplyPoints_Click(object sender, EventArgs e)
        {
            int idx = lstQuestions.SelectedIndex;

            if (idx < 0 || !questionItemMap.ContainsKey(idx))
            {
                CustomMessageBox.Show(
                    "Select a question in the list first.\n\n" +
                    "Section headers cannot be given points.",
                    "Set Points",
                    CustomMessageBoxButtons.OK,
                    CustomMessageBoxIcon.Information);
                return;
            }

            int pts;
            if (!int.TryParse(txtSelPoints.Text.Trim(), out pts) || pts < 1 || pts > 100)
            {
                CustomMessageBox.Show(
                    "Ilagay ang points mula 1 hanggang 100.",
                    "Set Points",
                    CustomMessageBoxButtons.OK,
                    CustomMessageBoxIcon.Warning);
                txtSelPoints.Focus();
                return;
            }

            QuizQuestion q = questionItemMap[idx];
            if (q == null) return;

            q.Points = pts;

            // Rebuild the list (same order, so the same index stays selected)
            RefreshQuestionList();

            if (idx < lstQuestions.Items.Count)
                lstQuestions.SelectedIndex = idx;
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
        // DEFAULT POINTS (MC / TF / IDENTIFICATION = 1, ESSAY = 5)
        // =========================================================

        private void ApplyDefaultPoints()
        {
            if (importedQuestions == null) return;

            foreach (QuizQuestion q in importedQuestions)
            {
                if (q == null) continue;

                string type = NormalizeQuestionType(q.QuestionType, false);
                q.Points = (type == "essay") ? 5 : 1;
            }
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
                    ApplyDefaultPoints();

                    lblFileName.Text = "File: " + Path.GetFileName(dialog.FileName);
                    lblQuestionCount.Text = "Questions: " + importedQuestions.Count;

                    RefreshQuestionList();

                    CustomMessageBox.Show(
                        "DOCX imported successfully!\n\n" +
                        "Questions found: " + importedQuestions.Count + "\n\n" +
                        "Points are set automatically: 1 for MC / TF / Identification, 5 for Essay.\n\n" +
                        "To change points, select a question in the list, set the Points box, " +
                        "and click SET POINTS.",
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

                int pts = q.Points < 1 ? 1 : q.Points;

                int itemIndex = lstQuestions.Items.Add(
                    displayNumber + ". " + questionText + "   [" + pts + (pts == 1 ? " pt]" : " pts]"));

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

            // ---- time limit (typed) ----
            int hours;
            int minutes;

            if (!int.TryParse(txtHours.Text.Trim(), out hours)) hours = 0;
            if (!int.TryParse(txtMinutes.Text.Trim(), out minutes)) minutes = 0;

            if (hours < 0 || hours > 24)
            {
                CustomMessageBox.Show("Hours must be between 0 and 24.", "Invalid Time Limit",
                    CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Warning);
                txtHours.Focus();
                return;
            }

            if (minutes < 0 || minutes > 59)
            {
                CustomMessageBox.Show("Minutes must be between 0 and 59.", "Invalid Time Limit",
                    CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Warning);
                txtMinutes.Focus();
                return;
            }

            int durationMinutes = (hours * 60) + minutes;

            if (durationMinutes < 1)
            {
                CustomMessageBox.Show("Time limit must be at least 1 minute.", "Invalid Time Limit",
                    CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Warning);
                txtHours.Focus();
                return;
            }

            if (durationMinutes > 1440)
            {
                CustomMessageBox.Show("Time limit cannot exceed 24 hours (1440 minutes).", "Invalid Time Limit",
                    CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Warning);
                txtHours.Focus();
                return;
            }

            // ---- deployment ----
            DateTime? deployAt = null;

            if (IsScheduled())
            {
                DateTime picked;

                if (!TryGetDeployDateTime(out picked))
                {
                    CustomMessageBox.Show(
                        "Ilagay ang deploy time sa tamang format, halimbawa: 08:00 AM o 14:30.",
                        "Invalid Deploy Time",
                        CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Warning);
                    txtDeployTime.Focus();
                    return;
                }

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
            int totalPoints = GetTotalPoints();

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
                "Total Questions: " + importedQuestions.Count + "\n" +
                "Total Points: " + totalPoints,
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
                "Total Questions: " + importedQuestions.Count + "\n" +
                "Total Points: " + totalPoints + "\n\n" +
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
                         choice_a, choice_b, choice_c, choice_d, correct_answer, points)
                        VALUES
                        (@quiz_id, @question_text, @question_type,
                         @choice_a, @choice_b, @choice_c, @choice_d, @correct_answer, @points);";

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
                            command.Parameters.AddWithValue("@points", q.Points < 1 ? 1 : q.Points);
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

        private int GetTotalPoints()
        {
            int total = 0;

            if (importedQuestions == null) return 0;

            foreach (QuizQuestion q in importedQuestions)
            {
                if (q == null) continue;
                total += q.Points < 1 ? 1 : q.Points;
            }

            return total;
        }

        private string ValidateQuestions()
        {
            for (int i = 0; i < importedQuestions.Count; i++)
            {
                QuizQuestion q = importedQuestions[i];
                string no = "Question " + (i + 1);

                if (q == null) return no + " is empty.";

                if (q.Points < 1)
                    return no + " must have at least 1 point.";

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

            txtHours.Text = "1";
            txtMinutes.Text = "0";

            cmbDeployMode.SelectedIndex = 0;

            DateTime defaultDeploy = DateTime.Today.AddDays(1).AddHours(8);
            dtpDeployDate.Value = defaultDeploy.Date;
            txtDeployTime.Text = defaultDeploy.ToString("hh:mm tt");

            importedQuestions = new List<QuizQuestion>();
            questionItemMap.Clear();
            lstQuestions.Items.Clear();

            txtSelPoints.Text = "1";
            txtSelPoints.Enabled = false;

            lblFileName.Text = "No DOCX file selected.";
            lblQuestionCount.Text = "Questions: 0";
        }

        private void InitializeComponent()
        {
        }
    }

    // =================================================================
    // QUESTION EDITOR FORM (unchanged)
    // =================================================================

    public class QuestionEditorForm : Form
    {
        private QuizQuestion question;

        private ComboBox cmbQuestionType;
        private NumericUpDown numPoints;
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

            Label lblPoints = new Label();
            lblPoints.Text = "Points";
            lblPoints.Font = new Font("Segoe UI Semibold", 10, FontStyle.Bold);
            lblPoints.AutoSize = true;
            lblPoints.Location = new Point(300, 105);
            Controls.Add(lblPoints);

            numPoints = new NumericUpDown();
            numPoints.Font = new Font("Segoe UI", 10);
            numPoints.Minimum = 1;
            numPoints.Maximum = 100;
            numPoints.Value = 1;
            numPoints.TextAlign = HorizontalAlignment.Center;
            numPoints.Location = new Point(300, 130);
            numPoints.Size = new Size(90, 32);
            Controls.Add(numPoints);

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

            numPoints.Value = Math.Max(1, Math.Min(100, question.Points));

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
            question.Points = (int)numPoints.Value;

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}