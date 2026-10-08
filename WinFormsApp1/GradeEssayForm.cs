using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace WinFormsApp1
{
    public class GradeEssayForm : Form
    {
        private readonly int attemptId;
        private bool noPendingEssays = false;

        private ComboBox cmbEssay;
        private TextBox txtQuestion;
        private TextBox txtAnswer;
        private Label lblPoints;
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
            ClientSize = new Size(640, 520);
            BackColor = Color.FromArgb(245, 247, 250);
            Font = new Font("Segoe UI", 9.5F);

            Label lblSelect = new Label();
            lblSelect.Text = "Pending essay:";
            lblSelect.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            lblSelect.AutoSize = true;
            lblSelect.Location = new Point(20, 18);
            Controls.Add(lblSelect);

            cmbEssay = new ComboBox();
            cmbEssay.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbEssay.Font = new Font("Segoe UI", 9.5F);
            cmbEssay.Location = new Point(20, 42);
            cmbEssay.Size = new Size(600, 28);
            cmbEssay.SelectedIndexChanged += (s, e) => ShowSelectedEssay();
            Controls.Add(cmbEssay);

            Label lblQ = new Label();
            lblQ.Text = "Question:";
            lblQ.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            lblQ.AutoSize = true;
            lblQ.Location = new Point(20, 84);
            Controls.Add(lblQ);

            txtQuestion = new TextBox();
            txtQuestion.Multiline = true;
            txtQuestion.ReadOnly = true;
            txtQuestion.ScrollBars = ScrollBars.Vertical;
            txtQuestion.BackColor = Color.White;
            txtQuestion.Font = new Font("Segoe UI", 9.5F);
            txtQuestion.Location = new Point(20, 108);
            txtQuestion.Size = new Size(600, 70);
            Controls.Add(txtQuestion);

            Label lblA = new Label();
            lblA.Text = "Student answer:";
            lblA.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            lblA.AutoSize = true;
            lblA.Location = new Point(20, 190);
            Controls.Add(lblA);

            txtAnswer = new TextBox();
            txtAnswer.Multiline = true;
            txtAnswer.ReadOnly = true;
            txtAnswer.ScrollBars = ScrollBars.Vertical;
            txtAnswer.BackColor = Color.White;
            txtAnswer.Font = new Font("Segoe UI", 10F);
            txtAnswer.Location = new Point(20, 214);
            txtAnswer.Size = new Size(600, 170);
            Controls.Add(txtAnswer);

            lblPoints = new Label();
            lblPoints.Text = "Points:";
            lblPoints.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            lblPoints.AutoSize = true;
            lblPoints.Location = new Point(20, 404);
            Controls.Add(lblPoints);

            numPoints = new NumericUpDown();
            numPoints.Font = new Font("Segoe UI", 10F);
            numPoints.Minimum = 0;
            numPoints.Maximum = 1;
            numPoints.Value = 0;
            numPoints.DecimalPlaces = 0;
            numPoints.TextAlign = HorizontalAlignment.Center;
            numPoints.Location = new Point(80, 400);
            numPoints.Size = new Size(90, 28);
            Controls.Add(numPoints);

            lblRemaining = new Label();
            lblRemaining.AutoSize = true;
            lblRemaining.ForeColor = Color.FromArgb(180, 90, 0);
            lblRemaining.Location = new Point(190, 404);
            Controls.Add(lblRemaining);

            chkRelease = new CheckBox();
            chkRelease.Text = "I-release sa student kapag tapos na lahat ng essay";
            chkRelease.Font = new Font("Segoe UI", 9.5F);
            chkRelease.AutoSize = true;
            chkRelease.Checked = true;
            chkRelease.Location = new Point(20, 440);
            Controls.Add(chkRelease);

            btnSave = new Button();
            btnSave.Text = "SAVE GRADE";
            btnSave.Font = new Font("Segoe UI Semibold", 10, FontStyle.Bold);
            btnSave.BackColor = Color.FromArgb(94, 14, 33);
            btnSave.ForeColor = Color.White;
            btnSave.FlatStyle = FlatStyle.Flat;
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Size = new Size(170, 40);
            btnSave.Location = new Point(360, 470);
            btnSave.Click += BtnSave_Click;
            Controls.Add(btnSave);

            btnClose = new Button();
            btnClose.Text = "CLOSE";
            btnClose.Font = new Font("Segoe UI Semibold", 10, FontStyle.Bold);
            btnClose.BackColor = Color.FromArgb(40, 40, 40);
            btnClose.ForeColor = Color.White;
            btnClose.FlatStyle = FlatStyle.Flat;
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Size = new Size(130, 40);
            btnClose.Location = new Point(490, 470);
            btnClose.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            Controls.Add(btnClose);

            AcceptButton = btnSave;
            CancelButton = btnClose;
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
                MessageBox.Show("Unable to load essays.\n\n" + ex.Message,
                    "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                MessageBox.Show("Walang pending na essay sa attempt na ito.", "Grade Essay",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.Cancel;
                Close();
            }
        }

        private void ShowSelectedEssay()
        {
            EssayItem item = cmbEssay.SelectedItem as EssayItem;
            if (item == null) return;

            // Question text and student answer for the selected essay
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
                MessageBox.Show("Unable to load the essay.\n\n" + ex.Message,
                    "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            numPoints.Maximum = item.MaxPoints;
            numPoints.Value = 0;
            lblPoints.Text = "Points (max " + item.MaxPoints + "):";
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
            int pendingBefore = cmbEssay.Items.Count;
            bool willBeLast = pendingBefore == 1;

            try
            {
                EssayGrading.SaveEssayGrade(item.AnswerId, earned, chkRelease.Checked);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to save the grade.\n\n" + ex.Message,
                    "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (willBeLast)
            {
                MessageBox.Show(
                    chkRelease.Checked
                        ? "Tapos na ang lahat ng essay. Na-release na ang grade ng student."
                        : "Tapos na ang lahat ng essay. Hindi pa na-release ang grade.",
                    "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);

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
                MessageBox.Show(
                    "Na-save ang grade.\n\nMay natitira pang essay na kailangang i-grade.\n" +
                    "Hindi pa ma-release ang grade hangga't hindi tapos ang lahat.",
                    "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                DialogResult = DialogResult.OK;
                Close();
            }
        }
    }
}