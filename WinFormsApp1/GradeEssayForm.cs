using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace WinFormsApp1
{
    public class GradeEssayForm : Form
    {
        private readonly int attemptId;
        private bool noPendingEssays = false;

        // Palette (same as the rest of the app)
        private static readonly Color ClrMaroon = Color.FromArgb(94, 14, 33);
        private static readonly Color ClrMaroonDark = Color.FromArgb(70, 10, 24);
        private static readonly Color ClrGold = Color.FromArgb(198, 156, 53);
        private static readonly Color ClrGoldSoft = Color.FromArgb(230, 210, 180);
        private static readonly Color ClrPageBg = Color.FromArgb(245, 247, 250);
        private static readonly Color ClrCardBorder = Color.FromArgb(225, 228, 235);
        private static readonly Color ClrMuted = Color.FromArgb(120, 125, 135);
        private static readonly Color ClrWarn = Color.FromArgb(180, 90, 0);
        private static readonly Color ClrDark = Color.FromArgb(40, 40, 40);

        private ComboBox cmbEssay;
        private TextBox txtQuestion;
        private TextBox txtAnswer;
        private Label lblMax;
        private NumericUpDown numPoints;
        private CheckBox chkRelease;
        private Label lblRemaining;
        private Button btnSave;
        private Button btnClose;

        private class EssayItem
        {
            public int AnswerId;
            public int MaxPoints;
            public string Text;

            public override string ToString()
            {
                return Text;
            }
        }

        public GradeEssayForm(int attemptId)
        {
            this.attemptId = attemptId;

            BuildInterface();
            LoadPendingEssays();
        }

        // =========================================================
        // BUILD INTERFACE
        // =========================================================

        private void BuildInterface()
        {
            Text = "Grade Essay";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(660, 664);
            BackColor = ClrPageBg;
            Font = new Font("Segoe UI", 9.5F);

            // ---------------- HEADER ----------------
            Panel header = new Panel();
            header.Location = new Point(0, 0);
            header.Size = new Size(660, 90);
            header.BackColor = ClrMaroon;

            Panel accent = new Panel();
            accent.Dock = DockStyle.Bottom;
            accent.Height = 3;
            accent.BackColor = ClrGold;
            header.Controls.Add(accent);

            Label lblTitle = new Label();
            lblTitle.Text = "Grade Essay";
            lblTitle.Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold);
            lblTitle.ForeColor = Color.White;
            lblTitle.BackColor = Color.Transparent;
            lblTitle.AutoSize = true;
            lblTitle.Location = new Point(24, 16);
            header.Controls.Add(lblTitle);

            Label lblSub = new Label();
            lblSub.Text = "Review the student's answer, then assign points.";
            lblSub.Font = new Font("Segoe UI", 9.5F);
            lblSub.ForeColor = ClrGoldSoft;
            lblSub.BackColor = Color.Transparent;
            lblSub.AutoSize = true;
            lblSub.Location = new Point(26, 54);
            header.Controls.Add(lblSub);

            Controls.Add(header);

            // ---------------- CARD: PENDING ESSAY ----------------
            Panel cardSelect = CreateCard(24, 108, 612, 70);
            AddCaption(cardSelect, "PENDING ESSAY", 16, 10);

            cmbEssay = new ComboBox();
            cmbEssay.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbEssay.FlatStyle = FlatStyle.Flat;
            cmbEssay.Font = new Font("Segoe UI", 10F);
            cmbEssay.Location = new Point(16, 32);
            cmbEssay.Size = new Size(580, 28);
            cmbEssay.SelectedIndexChanged += (s, e) => ShowSelectedEssay();
            cardSelect.Controls.Add(cmbEssay);

            Controls.Add(cardSelect);

            // ---------------- CARD: QUESTION ----------------
            Panel cardQuestion = CreateCard(24, 192, 612, 122);
            AddCaption(cardQuestion, "QUESTION", 16, 10);

            txtQuestion = new TextBox();
            txtQuestion.Multiline = true;
            txtQuestion.ReadOnly = true;
            txtQuestion.ScrollBars = ScrollBars.Vertical;
            txtQuestion.BorderStyle = BorderStyle.None;
            txtQuestion.BackColor = Color.White;
            txtQuestion.ForeColor = ClrText();
            txtQuestion.Font = new Font("Segoe UI", 10F);
            txtQuestion.Location = new Point(16, 32);
            txtQuestion.Size = new Size(580, 80);
            cardQuestion.Controls.Add(txtQuestion);

            Controls.Add(cardQuestion);

            // ---------------- CARD: STUDENT ANSWER ----------------
            Panel cardAnswer = CreateCard(24, 326, 612, 170);
            AddCaption(cardAnswer, "STUDENT ANSWER", 16, 10);

            txtAnswer = new TextBox();
            txtAnswer.Multiline = true;
            txtAnswer.ReadOnly = true;
            txtAnswer.ScrollBars = ScrollBars.Vertical;
            txtAnswer.BorderStyle = BorderStyle.None;
            txtAnswer.BackColor = Color.White;
            txtAnswer.ForeColor = ClrText();
            txtAnswer.Font = new Font("Segoe UI", 10.5F);
            txtAnswer.Location = new Point(16, 32);
            txtAnswer.Size = new Size(580, 124);
            cardAnswer.Controls.Add(txtAnswer);

            Controls.Add(cardAnswer);

            // ---------------- CARD: POINTS + RELEASE ----------------
            Panel cardGrade = CreateCard(24, 508, 612, 70);
            AddCaption(cardGrade, "POINTS", 16, 10);

            numPoints = new NumericUpDown();
            numPoints.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            numPoints.TextAlign = HorizontalAlignment.Center;
            numPoints.Minimum = 0;
            numPoints.Maximum = 1;
            numPoints.Value = 0;
            numPoints.DecimalPlaces = 0;
            numPoints.Location = new Point(16, 32);
            numPoints.Size = new Size(90, 28);
            cardGrade.Controls.Add(numPoints);



            lblMax = new Label();
            lblMax.Text = "out of 1";
            lblMax.Font = new Font("Segoe UI", 9.5F);
            lblMax.ForeColor = ClrMuted;
            lblMax.BackColor = Color.Transparent;
            lblMax.AutoSize = true;
            lblMax.Location = new Point(116, 36);
            cardGrade.Controls.Add(lblMax);

            chkRelease = new CheckBox();
            chkRelease.Text = "Release to student after all essays are graded";
            chkRelease.Font = new Font("Segoe UI", 9.5F);
            chkRelease.ForeColor = ClrDark;
            chkRelease.AutoSize = true;
            chkRelease.Checked = true;
            chkRelease.Location = new Point(250, 33);
            cardGrade.Controls.Add(chkRelease);

            Controls.Add(cardGrade);

            // ---------------- FOOTER ----------------
            lblRemaining = new Label();
            lblRemaining.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            lblRemaining.ForeColor = ClrWarn;
            lblRemaining.BackColor = Color.Transparent;
            lblRemaining.AutoSize = true;
            lblRemaining.Location = new Point(24, 606);
            Controls.Add(lblRemaining);

            btnClose = new Button();
            btnClose.Text = "CANCEL";
            btnClose.Size = new Size(112, 38);
            btnClose.Location = new Point(380, 596);
            StyleButton(btnClose, ClrDark, Color.FromArgb(70, 70, 70), Color.White);
            btnClose.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            Controls.Add(btnClose);

            btnSave = new Button();
            btnSave.Text = "SAVE GRADE";
            btnSave.Size = new Size(136, 38);
            btnSave.Location = new Point(500, 596);
            StyleButton(btnSave, ClrMaroon, ClrMaroonDark, Color.White);
            btnSave.Click += BtnSave_Click;
            Controls.Add(btnSave);

            AcceptButton = btnSave;
            CancelButton = btnClose;
        }

        // =========================================================
        // UI HELPERS
        // =========================================================

        private static Color ClrText()
        {
            return Color.FromArgb(35, 35, 35);
        }

        private Panel CreateCard(int x, int y, int width, int height)
        {
            Panel card = new Panel();
            card.Location = new Point(x, y);
            card.Size = new Size(width, height);
            card.BackColor = Color.White;

            card.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                Rectangle rect = new Rectangle(0, 0, card.Width - 1, card.Height - 1);

                using (GraphicsPath path = RoundedPath(rect, 12))
                using (Pen pen = new Pen(ClrCardBorder, 1f))
                {
                    e.Graphics.FillPath(Brushes.White, path);
                    e.Graphics.DrawPath(pen, path);
                }
            };

            return card;
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

        private void AddCaption(Control parent, string text, int x, int y)
        {
            Label lbl = new Label();
            lbl.Text = text;
            lbl.Font = new Font("Segoe UI Semibold", 8F, FontStyle.Bold);
            lbl.ForeColor = ClrMuted;
            lbl.BackColor = Color.White;
            lbl.AutoSize = true;
            lbl.Location = new Point(x, y);
            parent.Controls.Add(lbl);
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

        // =========================================================
        // LOAD PENDING ESSAYS (needs_grading = 1)
        // =========================================================

        private void LoadPendingEssays()
        {
            cmbEssay.Items.Clear();

            try
            {
                string connStr = SettingsManager.Current.GetConnectionString();

                using (var conn = new MySqlConnection(connStr))
                using (var cmd = new MySqlCommand(@"
                    SELECT sa.answer_id, sa.student_answer, q.question_text, q.points
                    FROM student_answers sa
                    INNER JOIN questions q ON q.question_id = sa.question_id
                    WHERE sa.attempt_id = @id
                      AND sa.needs_grading = 1
                      AND q.question_type = 'essay'
                    ORDER BY sa.answer_id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", attemptId);
                    conn.Open();

                    int n = 0;

                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            n++;

                            string question = r["question_text"] == DBNull.Value
                                ? "" : r["question_text"].ToString();

                            if (question.Length > 60)
                                question = question.Substring(0, 60) + "...";

                            cmbEssay.Items.Add(new EssayItem
                            {
                                AnswerId = Convert.ToInt32(r["answer_id"]),
                                MaxPoints = r["points"] == DBNull.Value ? 1 : Math.Max(1, Convert.ToInt32(r["points"])),
                                Text = "Essay " + n + ": " + question
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show("Unable to load essays.\n\n" + ex.Message,
                    "Database Error", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Error);
            }

            UpdateRemainingLabel();

            if (cmbEssay.Items.Count == 0)
            {
                noPendingEssays = true;
                btnSave.Enabled = false;
                return;
            }

            cmbEssay.SelectedIndex = 0;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            if (noPendingEssays)
            {
                CustomMessageBox.Show("Walang pending na essay sa attempt na ito.", "Grade Essay",
                    CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Information);
                DialogResult = DialogResult.Cancel;
                Close();
            }
        }

        private void ShowSelectedEssay()
        {
            EssayItem item = cmbEssay.SelectedItem as EssayItem;
            if (item == null) return;

            try
            {
                string connStr = SettingsManager.Current.GetConnectionString();

                using (var conn = new MySqlConnection(connStr))
                using (var cmd = new MySqlCommand(@"
                    SELECT q.question_text, sa.student_answer
                    FROM student_answers sa
                    INNER JOIN questions q ON q.question_id = sa.question_id
                    WHERE sa.answer_id = @aid", conn))
                {
                    cmd.Parameters.AddWithValue("@aid", item.AnswerId);
                    conn.Open();

                    using (var r = cmd.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            txtQuestion.Text = r["question_text"] == DBNull.Value
                                ? "" : r["question_text"].ToString();

                            txtAnswer.Text = r["student_answer"] == DBNull.Value
                                ? "(walang sagot)" : r["student_answer"].ToString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show("Unable to load the essay.\n\n" + ex.Message,
                    "Database Error", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Error);
            }

            numPoints.Maximum = item.MaxPoints;
            numPoints.Value = 0;
            lblMax.Text = "out of " + item.MaxPoints;
        }

        private void UpdateRemainingLabel()
        {
            int remaining = cmbEssay.Items.Count;

            lblRemaining.Text = remaining > 0
                ? remaining + " essay pa ang kailangang i-grade"
                : "";
        }

        // =========================================================
        // SAVE GRADE
        // =========================================================

        private void BtnSave_Click(object sender, EventArgs e)
        {
            EssayItem item = cmbEssay.SelectedItem as EssayItem;
            if (item == null) return;

            int earned = (int)numPoints.Value;

            // Release is only applied by EssayGrading when no essay is left pending
            bool willBeLast = cmbEssay.Items.Count == 1;

            try
            {
                EssayGrading.SaveEssayGrade(item.AnswerId, earned, chkRelease.Checked);
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show("Unable to save the grade.\n\n" + ex.Message,
                    "Database Error", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Error);
                return;
            }

            if (willBeLast)
            {
                CustomMessageBox.Show(
                    chkRelease.Checked
                        ? "Tapos na ang lahat ng essay. Na-release na ang grade ng student."
                        : "Tapos na ang lahat ng essay. Hindi pa na-release ang grade.",
                    "Saved", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Information);

                DialogResult = DialogResult.OK;
                Close();
                return;
            }

            // May natitira pang essay: i-reload ang listahan
            int selectedIndex = cmbEssay.SelectedIndex;
            LoadPendingEssays();

            if (cmbEssay.Items.Count > 0)
            {
                cmbEssay.SelectedIndex = Math.Min(selectedIndex, cmbEssay.Items.Count - 1);

                CustomMessageBox.Show(
                    "Na-save ang grade.\n\nMay natitira pang essay na kailangang i-grade.\n" +
                    "Hindi pa ma-release ang grade hangga't hindi tapos ang lahat.",
                    "Saved", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Information);
            }
            else
            {
                DialogResult = DialogResult.OK;
                Close();
            }
        }
    }
}