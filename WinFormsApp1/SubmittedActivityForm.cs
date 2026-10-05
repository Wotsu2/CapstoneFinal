using DevExpress.XtraPdfViewer;
using Guna.UI2.WinForms;
using MySql.Data.MySqlClient;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace WinFormsApp1
{
    public partial class SubmittedActivityForm : Form
    {
        private int _userId;
        private string _title;
        private string _name;
        private string _section;
        private string _className;
        private string _status;
        private string _filePath;
        private int _professorId;

        // NEW: max score set by the professor when creating the activity
        private decimal _maxScore = -1m;

        // Controls
        private Guna2Panel header;
        private Guna2Panel infoStrip;
        private Guna2Panel pdfCard;
        private Guna2Panel footer;
        private Guna2CircleButton btnClose;
        private PdfViewer pdfViewer;
        private Guna2TextBox txtScore;
        private Guna2Button btnSaveScore;
        private Label lblMaxScore;   // NEW: shows "Max: X"

        /// <summary>
        /// Displays a student's submitted activity with PDF preview and score editing.
        /// </summary>
        public SubmittedActivityForm(
            int professorId,
            int userId,
            string title,
            string name,
            string section,
            string className,
            string status,
            string filePath)
        {
            _professorId = professorId;
            _userId = userId;
            _title = title ?? "";
            _name = name ?? "";
            _section = section ?? "";
            _className = className ?? "";
            _status = status ?? "";
            _filePath = filePath ?? "";

            // Load the max score BEFORE building the UI so we can show it
            _maxScore = LoadMaxScore();

            BuildUi();
            LoadPdf();
        }

        // =========================================================
        // LOAD MAX SCORE (from professor_activity.score)
        // =========================================================
        private decimal LoadMaxScore()
        {
            string connStr = SettingsManager.Current.GetConnectionString();
            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    // Find the activity that matches this submission.
                    // We filter by prof_id + title + section because that is
                    // exactly what uniquely identifies the original post.
                    string query = @"
                        SELECT score
                        FROM professor_activity
                        WHERE professor_id = @prof_id
                          AND title        = @title
                          AND section      = @section
                        LIMIT 1";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@prof_id", _professorId);
                        cmd.Parameters.AddWithValue("@title", _title);
                        cmd.Parameters.AddWithValue("@section", _section);

                        object result = cmd.ExecuteScalar();

                        if (result == null || result == DBNull.Value)
                            return -1m;

                        string raw = result.ToString().Trim();

                        if (string.IsNullOrEmpty(raw))
                            return -1m;

                        // The score column might store a plain number
                        // ("20") or a number with extra text ("20 pts").
                        // Extract the leading number if possible.
                        string numeric = "";

                        foreach (char ch in raw)
                        {
                            if (char.IsDigit(ch) || ch == '.')
                                numeric += ch;
                            else if (numeric.Length > 0)
                                break;
                        }

                        if (decimal.TryParse(numeric, out decimal value))
                            return value;

                        return -1m;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("LoadMaxScore error: " + ex.Message);
                return -1m;
            }
        }

        // =========================================================
        // UI CONSTRUCTION
        // =========================================================
        private void BuildUi()
        {
            // ---- FORM ----
            this.Text = "Submitted Activity - " + _name;
            this.Size = new Size(1050, 950);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(245, 245, 248);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowIcon = false;

            // ---- MAIN CARD ----
            Guna2Panel mainCard = new Guna2Panel();
            mainCard.Size = new Size(1010, 900);
            mainCard.Location = new Point(20, 15);
            mainCard.BorderRadius = 16;
            mainCard.FillColor = Color.White;
            mainCard.BorderColor = Color.FromArgb(225, 225, 225);
            mainCard.BorderThickness = 1;
            mainCard.ShadowDecoration.Enabled = true;
            mainCard.ShadowDecoration.Depth = 15;
            mainCard.ShadowDecoration.Color = Color.FromArgb(40, 0, 0, 0);
            this.Controls.Add(mainCard);

            // ---- HEADER ----
            header = new Guna2Panel
            {
                Dock = DockStyle.Top,
                Height = 90,
                FillColor = Color.Maroon,
                BorderRadius = 0
            };
            mainCard.Controls.Add(header);

            Label lblTitle = new Label
            {
                Text = _title,
                Font = new Font("Segoe UI Semibold", 16F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                AutoSize = false,
                AutoEllipsis = true,
                Size = new Size(760, 34),
                Location = new Point(24, 18)
            };
            header.Controls.Add(lblTitle);

            Label lblSubtitle = new Label
            {
                Text = _className + "  •  " + _section,
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(255, 220, 220),
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(26, 55)
            };
            header.Controls.Add(lblSubtitle);

            btnClose = new Guna2CircleButton
            {
                Size = new Size(40, 40),
                Location = new Point(mainCard.Width - 60, 25),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FillColor = Color.FromArgb(60, 0, 0, 0),
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.White,
                Text = "✕"
            };
            btnClose.HoverState.FillColor = Color.FromArgb(120, 0, 0, 0);
            btnClose.Click += (s, e) => this.Close();
            header.Controls.Add(btnClose);
            btnClose.BringToFront();

            // ---- INFO STRIP ----
            infoStrip = new Guna2Panel
            {
                Size = new Size(mainCard.Width - 40, 70),
                Location = new Point(20, 110),
                BorderRadius = 12,
                FillColor = Color.FromArgb(250, 245, 245),
                BorderColor = Color.FromArgb(240, 230, 230),
                BorderThickness = 1,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            mainCard.Controls.Add(infoStrip);

            string initials = "";
            if (!string.IsNullOrWhiteSpace(_name))
            {
                var parts = _name.Split(new[] { ' ', '_' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var p in parts) initials += char.ToUpper(p[0]);
                if (initials.Length > 2) initials = initials.Substring(0, 2);
            }
            if (string.IsNullOrEmpty(initials)) initials = "?";

            Guna2CircleButton avatar = new Guna2CircleButton
            {
                Size = new Size(46, 46),
                Location = new Point(14, 12),
                FillColor = Color.Maroon,
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold),
                Text = initials,
                Enabled = false
            };
            infoStrip.Controls.Add(avatar);

            infoStrip.Controls.Add(new Label
            {
                Text = _name,
                Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 30, 30),
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(72, 14)
            });

            infoStrip.Controls.Add(new Label
            {
                Text = "Section: " + _section + "     Subject: " + _className + "     Status: " + _status,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(110, 110, 110),
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(72, 38)
            });

            // ---- PDF CARD ----
            pdfCard = new Guna2Panel
            {
                Size = new Size(mainCard.Width - 40, 620),
                Location = new Point(20, 195),
                BorderRadius = 12,
                FillColor = Color.FromArgb(245, 245, 248),
                BorderColor = Color.FromArgb(225, 225, 225),
                BorderThickness = 1,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            mainCard.Controls.Add(pdfCard);

            pdfViewer = new PdfViewer
            {
                Dock = DockStyle.Fill,
                Location = new Point(1, 1)
            };
            pdfCard.Controls.Add(pdfViewer);

            // ---- FOOTER ----
            footer = new Guna2Panel
            {
                Size = new Size(mainCard.Width - 40, 80),
                Location = new Point(20, 830),
                BorderRadius = 12,
                FillColor = Color.FromArgb(250, 245, 245),
                BorderColor = Color.FromArgb(240, 230, 230),
                BorderThickness = 1,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            mainCard.Controls.Add(footer);

            footer.Controls.Add(new Label
            {
                Text = "Score:",
                Font = new Font("Segoe UI Semibold", 11F, FontStyle.Bold),
                ForeColor = Color.FromArgb(50, 50, 50),
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(24, 28)
            });

            txtScore = new Guna2TextBox
            {
                Width = 110,
                Height = 40,
                Location = new Point(90, 20),
                BorderRadius = 8,
                Font = new Font("Segoe UI", 11F),
                PlaceholderText = "0",
                TextAlign = HorizontalAlignment.Center
            };
            footer.Controls.Add(txtScore);

            // ---- SHOW MAX SCORE ----
            lblMaxScore = new Label
            {
                Text = _maxScore > 0m
                    ? $"Max: {_maxScore:0.##}"
                    : "Max: (not set)",
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                ForeColor = Color.Maroon,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(210, 29)
            };
            footer.Controls.Add(lblMaxScore);

            // ---- SAVE BUTTON ----
            btnSaveScore = new Guna2Button
            {
                Text = "Save Score",
                Size = new Size(150, 40),
                Location = new Point(footer.Width - 170, 20),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BorderRadius = 8,
                FillColor = Color.Maroon,
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                ForeColor = Color.White
            };
            btnSaveScore.HoverState.FillColor = Color.FromArgb(100, 0, 0);
            btnSaveScore.Click += BtnSaveScore_Click;
            footer.Controls.Add(btnSaveScore);

            // ---- LIVE INPUT GUARD: only allow numbers ----
            txtScore.KeyPress += TxtScore_KeyPress;

            // If a max score exists, cap the length too
            if (_maxScore > 0m)
                txtScore.MaxLength = _maxScore.ToString("0.##").Length + 1;
        }

        // =========================================================
        // PREVENT NON-NUMERIC INPUT
        // =========================================================
        private void TxtScore_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Allow digits, one decimal point, and backspace
            if (char.IsControl(e.KeyChar))
                return;

            if (char.IsDigit(e.KeyChar))
                return;

            if (e.KeyChar == '.' && !txtScore.Text.Contains("."))
                return;

            e.Handled = true;
        }

        // =========================================================
        // PDF LOAD
        // =========================================================
        private void LoadPdf()
        {
            try
            {
                string trimmedPath = _filePath?.Trim() ?? "";
                if (File.Exists(trimmedPath))
                {
                    pdfViewer.LoadDocument(trimmedPath);
                }
                else
                {
                    Label lblNoFile = new Label
                    {
                        Text = "📄   No PDF preview available",
                        Font = new Font("Segoe UI", 11F, FontStyle.Italic),
                        ForeColor = Color.Gray,
                        BackColor = Color.Transparent,
                        TextAlign = ContentAlignment.MiddleCenter,
                        Dock = DockStyle.Fill
                    };
                    pdfCard.Controls.Add(lblNoFile);
                    lblNoFile.BringToFront();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("LoadPdf error: " + ex.Message);
            }
        }

        // =========================================================
        // SAVE SCORE  (with max-score validation)
        // =========================================================
        private void BtnSaveScore_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtScore.Text))
            {
                CustomMessageBox.Show("Please enter a score.", "Validation",
                    CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Warning);
                return;
            }

            // Parse the entered score
            if (!decimal.TryParse(txtScore.Text.Trim(), out decimal enteredScore))
            {
                CustomMessageBox.Show("Please enter a valid number for the score.",
                    "Validation", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Warning);
                txtScore.Focus();
                return;
            }

            if (enteredScore < 0m)
            {
                CustomMessageBox.Show("Score cannot be negative.",
                    "Validation", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Warning);
                txtScore.Focus();
                return;
            }

            // =========================================================
            // MAX-SCORE VALIDATION
            // =========================================================
            if (_maxScore > 0m && enteredScore > _maxScore)
            {
                CustomMessageBox.Show(
                    $"Invalid score.\n\n" +
                    $"The maximum score for this activity is {_maxScore:0.##}.\n" +
                    $"You entered {enteredScore:0.##}.\n\n" +
                    $"Please enter a value between 0 and {_maxScore:0.##}.",
                    "Score Exceeds Maximum",
                    CustomMessageBoxButtons.OK,
                    CustomMessageBoxIcon.Warning);

                txtScore.Focus();
                txtScore.SelectAll();
                return;
            }

            string connStr = SettingsManager.Current.GetConnectionString();
            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    // Filter by all fields so we only update the specific submission
                    string query = @"UPDATE submitted_activity 
                                     SET score = @score 
                                     WHERE user_id = @user_id
                                       AND title   = @title
                                       AND section = @section
                                       AND prof_id = @prof_id";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@score", enteredScore);
                        cmd.Parameters.AddWithValue("@user_id", _userId);
                        cmd.Parameters.AddWithValue("@title", _title);
                        cmd.Parameters.AddWithValue("@section", _section);
                        cmd.Parameters.AddWithValue("@prof_id", _professorId);

                        int rows = cmd.ExecuteNonQuery();
                        if (rows > 0)
                        {
                            CustomMessageBox.Show(
                                $"Score saved successfully.\n\n" +
                                $"Score: {enteredScore:0.##}" +
                                (_maxScore > 0m ? $" / {_maxScore:0.##}" : ""),
                                "Saved",
                                CustomMessageBoxButtons.OK,
                                CustomMessageBoxIcon.Information);

                            this.Close();
                        }
                        else
                        {
                            CustomMessageBox.Show("No matching submission found to update.",
                                "Not Found", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Warning);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("SaveScore error: " + ex.Message);
                CustomMessageBox.Show("Error saving score: " + ex.Message,
                    "Error", CustomMessageBoxButtons.OK, CustomMessageBoxIcon.Error);
            }
        }
    }
}