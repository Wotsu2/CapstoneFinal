using System;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace WinFormsApp1
{
    public partial class UserDetailsForm : Form
    {
        private PictureBox detailsPhoto;
        private Label detailsName;
        private Label detailsRoleBadge;
        private Label detailsUsername, detailsLastName, detailsFirstName,
                      detailsMiddleName, detailsRole, detailsYear,
                      detailsSection, detailsCourse;
        private Button detailsInfoTab, detailsHistoryTab;
        private Panel detailsInfoPage, detailsHistoryPage;

        public UserDetailsForm()
        {
            InitializeComponent();
            BuildUI();
        }

        private void BuildUI()
        {
            this.Text = "User Details";
            this.Size = new Size(680, 700);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.White;
            this.ShowInTaskbar = false;

            int modalWidth = this.ClientSize.Width;

            // ---- Header ----
            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 160,
                BackColor = Color.FromArgb(13, 71, 161)
            };
            this.Controls.Add(header);

            detailsRoleBadge = new Label
            {
                Text = "Student",
                ForeColor = Color.White,
                BackColor = Color.FromArgb(20, 20, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(90, 26),
                Location = new Point(modalWidth - 110, 20),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            detailsName = new Label
            {
                Text = "Full Name",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 15F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(210, 105),
                BackColor = Color.Transparent
            };

            header.Controls.Add(detailsRoleBadge);
            header.Controls.Add(detailsName);

            // ---- Photo ----
            detailsPhoto = new PictureBox
            {
                Size = new Size(120, 120),
                Location = new Point(60, 100),
                BackColor = Color.FromArgb(0, 188, 212),
                SizeMode = PictureBoxSizeMode.Zoom
            };
            detailsPhoto.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var path = new GraphicsPath())
                {
                    path.AddEllipse(0, 0, detailsPhoto.Width - 1, detailsPhoto.Height - 1);
                    detailsPhoto.Region = new Region(path);
                }
            };
            this.Controls.Add(detailsPhoto);
            detailsPhoto.BringToFront();

            // ---- Tabs ----
            detailsInfoTab = new Button
            {
                Text = "Info",
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Size = new Size(80, 32),
                Location = new Point(270, 175),
                Cursor = Cursors.Hand
            };
            detailsHistoryTab = new Button
            {
                Text = "History",
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 9F),
                Size = new Size(100, 32),
                Location = new Point(352, 175),
                Cursor = Cursors.Hand
            };

            // ---- Info page ----
            detailsInfoPage = new Panel
            {
                Location = new Point(0, 217),
                Size = new Size(modalWidth, 278),
                BackColor = Color.White,
                AutoScroll = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };

            int y = 15;
            detailsUsername = MakeInfoRow(detailsInfoPage, "Username:", ref y);
            detailsLastName = MakeInfoRow(detailsInfoPage, "Last Name:", ref y);
            detailsFirstName = MakeInfoRow(detailsInfoPage, "First Name:", ref y);
            detailsMiddleName = MakeInfoRow(detailsInfoPage, "Middle name:", ref y);
            detailsRole = MakeInfoRow(detailsInfoPage, "Role:", ref y);
            detailsYear = MakeInfoRow(detailsInfoPage, "Year:", ref y);
            detailsSection = MakeInfoRow(detailsInfoPage, "Section:", ref y);
            detailsCourse = MakeInfoRow(detailsInfoPage, "Course:", ref y);

            // ---- History page ----
            detailsHistoryPage = new Panel
            {
                Location = new Point(0, 217),
                Size = new Size(modalWidth, 278),
                BackColor = Color.White,
                Visible = false,
                AutoScroll = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };

            var historyCard = new Panel
            {
                Location = new Point(30, 20),
                Size = new Size(400, 70),
                BackColor = Color.FromArgb(245, 245, 245),
                BorderStyle = BorderStyle.FixedSingle
            };
            historyCard.Controls.Add(new Label
            {
                Text = "🕒  Account History",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Location = new Point(10, 10),
                AutoSize = true
            });
            historyCard.Controls.Add(new Label
            {
                Text = "Create account last August 30, 2026",
                Font = new Font("Segoe UI", 8F),
                Location = new Point(10, 36),
                AutoSize = true
            });
            detailsHistoryPage.Controls.Add(historyCard);

            detailsInfoTab.Click += (s, e) =>
            {
                detailsInfoPage.Visible = true;
                detailsHistoryPage.Visible = false;
                detailsInfoTab.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                detailsHistoryTab.Font = new Font("Segoe UI", 9F);
            };
            detailsHistoryTab.Click += (s, e) =>
            {
                detailsInfoPage.Visible = false;
                detailsHistoryPage.Visible = true;
                detailsInfoTab.Font = new Font("Segoe UI", 9F);
                detailsHistoryTab.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            };

            this.Controls.Add(detailsInfoPage);
            this.Controls.Add(detailsHistoryPage);
            this.Controls.Add(detailsInfoTab);
            this.Controls.Add(detailsHistoryTab);
        }

        private Label MakeInfoRow(Panel parent, string label, ref int y)
        {
            var lbl = new Label
            {
                Text = label,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.Black,
                Location = new Point(60, y),
                AutoSize = true
            };
            var val = new Label
            {
                Text = "-",
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = Color.DimGray,
                Location = new Point(220, y),
                AutoSize = false,
                Size = new Size(420, 22)
            };
            parent.Controls.Add(lbl);
            parent.Controls.Add(val);
            y += 32;
            return val;
        }

        // =========================================================
        //  PUBLIC API — fill this form with row data
        // =========================================================
        public void LoadUser(DataGridViewRow row, DataGridView grid)
        {
            string SafeCell(string col)
            {
                if (!grid.Columns.Contains(col)) return "";
                var v = row.Cells[col].Value;
                return (v == null || v == DBNull.Value) ? "" : v.ToString();
            }

            string fullName = $"{SafeCell("firstname")} {SafeCell("middlename")} {SafeCell("lastname")}".Trim();
            detailsName.Text = string.IsNullOrWhiteSpace(fullName) ? "Unknown" : fullName;

            string role = SafeCell("roles");
            detailsRoleBadge.Text = string.IsNullOrEmpty(role) ? "User" : role;

            detailsUsername.Text = SafeCell("username");
            detailsLastName.Text = SafeCell("lastname");
            detailsFirstName.Text = SafeCell("firstname");
            detailsMiddleName.Text = SafeCell("middlename");
            detailsRole.Text = SafeCell("roles");
            detailsYear.Text = SafeCell("school_year");
            detailsSection.Text = SafeCell("school_section");
            detailsCourse.Text = SafeCell("school_course");

            detailsPhoto.Image = LoadUserPhoto(row, grid);

            detailsInfoPage.Visible = true;
            detailsHistoryPage.Visible = false;
            detailsInfoTab.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            detailsHistoryTab.Font = new Font("Segoe UI", 9F);
        }

        private Image LoadUserPhoto(DataGridViewRow row, DataGridView grid)
        {
            try
            {
                if (grid.Columns.Contains("profile_picture"))
                {
                    object raw = row.Cells["profile_picture"].Value;

                    byte[] bytes = raw as byte[];
                    if (bytes != null && bytes.Length > 0)
                    {
                        using (var ms = new MemoryStream(bytes))
                        {
                            var temp = Image.FromStream(ms);
                            return new Bitmap(temp);
                        }
                    }

                    string path = raw as string;
                    if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                    {
                        byte[] fileBytes = File.ReadAllBytes(path);
                        using (var ms = new MemoryStream(fileBytes))
                        {
                            var temp = Image.FromStream(ms);
                            return new Bitmap(temp);
                        }
                    }
                }
            }
            catch { }

            return MakePlaceholderAvatar();
        }

        private Image MakePlaceholderAvatar()
        {
            var bmp = new Bitmap(120, 120);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.FromArgb(0, 188, 212));
                using (var b = new SolidBrush(Color.White))
                {
                    g.FillEllipse(b, 40, 20, 42, 42);
                    g.FillPie(b, 22, 66, 76, 76, 180, 180);
                }
            }
            return bmp;
        }
    }
}