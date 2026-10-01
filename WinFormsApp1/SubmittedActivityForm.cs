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

        // Controls
        private Guna2Panel header;
        private Guna2Panel infoStrip;
        private Guna2Panel pdfCard;
        private Guna2Panel footer;
        private Guna2CircleButton btnClose;
        private PdfViewer pdfViewer;
        private Guna2TextBox txtScore;
        private Guna2Button btnSaveScore;

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

            BuildUi();
            LoadPdf();
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

            btnSaveScore = new Guna2Button
            {
                Text = "Save Score",
                Size = new Size(150, 40),
                Location = new Point(220, 20),
                BorderRadius = 8,
                FillColor = Color.Maroon,
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                ForeColor = Color.White
            };
            btnSaveScore.HoverState.FillColor = Color.FromArgb(100, 0, 0);
            btnSaveScore.Click += BtnSaveScore_Click;
            footer.Controls.Add(btnSaveScore);
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
        // SAVE SCORE
        // =========================================================
        private void BtnSaveScore_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtScore.Text))
            {
                MessageBox.Show("Please enter a score.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string connStr = SettingsManager.Current.GetConnectionString();
            try
            {
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    // Filter by all three so we only update the specific submission
                    string query = @"UPDATE submitted_activity 
                                     SET score = @score 
                                     WHERE user_id = @user_id
                                       AND title   = @title
                                       AND section = @section
                                       AND prof_id = @prof_id";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@score", txtScore.Text.Trim());
                        cmd.Parameters.AddWithValue("@user_id", _userId);
                        cmd.Parameters.AddWithValue("@title", _title);
                        cmd.Parameters.AddWithValue("@section", _section);
                        cmd.Parameters.AddWithValue("@prof_id", _professorId);

                        int rows = cmd.ExecuteNonQuery();
                        if (rows > 0)
                        {
                            MessageBox.Show("Score saved successfully.", "Saved",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                            this.Close();
                        }
                        else
                        {
                            MessageBox.Show("No matching submission found to update.",
                                "Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("SaveScore error: " + ex.Message);
                MessageBox.Show("Error saving score: " + ex.Message);
            }
        }
    }
}