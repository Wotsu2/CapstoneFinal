using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using static WinFormsApp1.QuizSchema;

namespace WinFormsApp1
{
    // =================================================================
    // SCHEMA HELPER  (adds quizzes.deploy_at if it does not exist yet)
    // =================================================================

    internal static class QuizSchema
    {
        public static bool HasDeployColumn(MySqlConnection conn)
        {
            using (var cmd = new MySqlCommand(
                @"SELECT COUNT(*) FROM information_schema.COLUMNS
                  WHERE TABLE_SCHEMA = DATABASE()
                    AND TABLE_NAME = 'quizzes'
                    AND COLUMN_NAME = 'deploy_at'", conn))
            {
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        internal static class Gfx
        {
            public static GraphicsPath RoundPath(Rectangle r, int radius)
            {
                int d = Math.Max(1, radius * 2);
                var p = new GraphicsPath();
                p.AddArc(r.X, r.Y, d, d, 180, 90);
                p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
                p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
                p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
                p.CloseFigure();
                return p;
            }
        }

        internal class CardPanel : Panel
        {
            public int Radius = 12;
            public Color Fill = Color.White;
            public Color BorderColor = Color.FromArgb(232, 224, 200);
            public Color? Accent;

            public CardPanel()
            {
                DoubleBuffered = true;
                ResizeRedraw = true;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                var rect = new Rectangle(0, 0, Width - 1, Height - 1);
                using (var path = Gfx.RoundPath(rect, Radius))
                using (var fill = new SolidBrush(Fill))
                using (var pen = new Pen(BorderColor, 1f))
                {
                    e.Graphics.FillPath(fill, path);
                    e.Graphics.DrawPath(pen, path);
                }

                if (Accent.HasValue)
                {
                    var bar = new Rectangle(14, 20, 5, Height - 40);
                    using (var path = Gfx.RoundPath(bar, 2))
                    using (var br = new SolidBrush(Accent.Value))
                        e.Graphics.FillPath(br, path);
                }
            }
        }

        // Call BEFORE starting a transaction (ALTER TABLE commits implicitly).
        public static bool EnsureDeployColumn(MySqlConnection conn)
        {
            try
            {
                if (HasDeployColumn(conn)) return true;

                using (var alter = new MySqlCommand(
                    "ALTER TABLE quizzes ADD COLUMN deploy_at DATETIME NULL", conn))
                {
                    alter.ExecuteNonQuery();
                }

                return HasDeployColumn(conn);
            }
            catch
            {
                return false;
            }
        }
    }

    // =================================================================
    // MONITORING FORM
    // =================================================================

    public class MonitoringForm : Form
    {
        // ---- data ----

        private class QuizItem
        {
            public int Id;
            public string Title, Subject, Period, Type;
            public int Duration;
            public DateTime? DeployAt;
            public bool IsOpen = true;

            public override string ToString()
            {
                return Title + "  •  " + Subject + "  •  " + Period + " " + Type;
            }
        }

        private class AttemptRow
        {
            public string Name;
            public string Status;
            public int Answered;
            public int Total;
            public int RemainingSeconds;
            public bool HasRemaining;
            public int Score;
            public decimal Percentage;
            public DateTime? LastSeen;
        }

        private readonly int professorUserId;
        private readonly int initialQuizId;

        private QuizItem currentQuiz;
        private List<AttemptRow> allRows = new List<AttemptRow>();
        private bool hasDeployColumn = false;
        private bool isRefreshing = false;

        // ---- controls ----

        private ComboBox cmbQuiz;
        private Label lblDeploy;
        private RoundedButton btnDeployNow;
        private Panel quizPanel;

        private Label valTotal, valTaking, valDisconnected, valSubmitted, valAverage;

        private TextBox txtSearch;
        private ComboBox cmbStatus;
        private CheckBox chkAuto;
        private RoundedButton btnRefresh;
        private RoundedButton btnExport;
        private Panel filterPanel;

        private DataGridView dgv;
        private Label lblUpdated;
        private System.Windows.Forms.Timer refreshTimer;

        // ---- colors ----

        private static readonly Color ClrMaroon = Color.FromArgb(94, 14, 33);
        private static readonly Color ClrMaroonDark = Color.FromArgb(70, 10, 24);
        private static readonly Color ClrBlack = Color.FromArgb(20, 20, 20);
        private static readonly Color ClrLabelGray = Color.FromArgb(50, 50, 50);
        private static readonly Color ClrGreen = Color.FromArgb(22, 163, 74);
        private static readonly Color ClrYellow = Color.FromArgb(202, 138, 4);
        private static readonly Color ClrBlue = Color.FromArgb(37, 99, 235);
        private static readonly Color ClrRed = Color.FromArgb(220, 38, 38);
        private static readonly Color ClrGold = Color.FromArgb(198, 156, 53);
        private static readonly Color ClrGoldDark = Color.FromArgb(163, 126, 36);
        private static readonly Color ClrGoldLight = Color.FromArgb(240, 224, 180);
        private static readonly Color ClrPageBg = Color.FromArgb(250, 247, 239);

        private const string AllStatuses = "All Statuses";

        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public MonitoringForm(int professorId, int quizId = 0)
        {
            professorUserId = professorId;
            initialQuizId = quizId;

            BuildInterface();

            this.Load += (s, e) => LoadQuizList();
            this.FormClosed += (s, e) =>
            {
                if (refreshTimer != null)
                {
                    refreshTimer.Stop();
                    refreshTimer.Dispose();
                    refreshTimer = null;
                }
            };
        }

        // =========================================================
        // BUILD INTERFACE
        // =========================================================

        private void BuildInterface()
        {
            Text = "Student Monitoring";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(1120, 760);
            MinimumSize = new Size(980, 640);
            BackColor = ClrPageBg;
            Font = new Font("Segoe UI", 9.5F);
            DoubleBuffered = true;

            TableLayoutPanel table = new TableLayoutPanel();
            table.Dock = DockStyle.Fill;
            table.ColumnCount = 1;
            table.RowCount = 6;
            table.BackColor = ClrPageBg;
            table.Margin = new Padding(0);
            table.Padding = new Padding(0);
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 88));   // banner
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));   // quiz picker
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));  // stat cards
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));   // filters
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // grid
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));   // footer
            Controls.Add(table);

            // ---------- banner ----------
            Panel banner = new Panel();
            banner.Dock = DockStyle.Fill;
            banner.Margin = new Padding(0);
            banner.BackColor = ClrMaroon;
            banner.Paint += (s, e) =>
            {
                using (var pen = new Pen(ClrGold, 3f))
                    e.Graphics.DrawLine(pen, 0, banner.Height - 2, banner.Width, banner.Height - 2);
            };
            table.Controls.Add(banner, 0, 0);

            Label lblBrand = new Label();
            lblBrand.Text = "CDSGA";
            lblBrand.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            lblBrand.ForeColor = ClrGold;
            lblBrand.BackColor = Color.Transparent;
            lblBrand.AutoSize = true;
            lblBrand.Location = new Point(28, 12);
            banner.Controls.Add(lblBrand);

            Label lblTitle = new Label();
            lblTitle.Text = "Student Monitoring";
            lblTitle.Font = new Font("Segoe UI Semibold", 20F, FontStyle.Bold);
            lblTitle.ForeColor = Color.White;
            lblTitle.BackColor = Color.Transparent;
            lblTitle.AutoSize = true;
            lblTitle.Location = new Point(26, 30);
            banner.Controls.Add(lblTitle);

            // ---------- quiz picker row ----------
            TableLayoutPanel quizRow = new TableLayoutPanel();
            quizRow.Dock = DockStyle.Fill;
            quizRow.Margin = new Padding(24, 0, 24, 0);
            quizRow.BackColor = ClrPageBg;
            quizRow.ColumnCount = 4;
            quizRow.RowCount = 1;
            quizRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            quizRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            quizRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            quizRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            quizRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            table.Controls.Add(quizRow, 0, 1);
            quizPanel = quizRow;

            Label lblQuiz = new Label();
            lblQuiz.Text = "Quiz / Exam";
            lblQuiz.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            lblQuiz.ForeColor = ClrLabelGray;
            lblQuiz.BackColor = Color.Transparent;
            lblQuiz.AutoSize = true;
            lblQuiz.Anchor = AnchorStyles.Left;
            lblQuiz.Margin = new Padding(0, 0, 12, 0);
            quizRow.Controls.Add(lblQuiz, 0, 0);

            cmbQuiz = new ComboBox();
            cmbQuiz.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbQuiz.Font = new Font("Segoe UI", 10F);
            cmbQuiz.DropDownWidth = 640;
            cmbQuiz.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            cmbQuiz.Margin = new Padding(0, 0, 18, 0);
            cmbQuiz.SelectedIndexChanged += (s, e) => OnQuizChanged();
            quizRow.Controls.Add(cmbQuiz, 1, 0);

            lblDeploy = new Label();
            lblDeploy.Text = "";
            lblDeploy.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            lblDeploy.ForeColor = ClrLabelGray;
            lblDeploy.BackColor = Color.Transparent;
            lblDeploy.AutoSize = true;
            lblDeploy.Anchor = AnchorStyles.Left;
            lblDeploy.Margin = new Padding(0, 0, 16, 0);
            quizRow.Controls.Add(lblDeploy, 2, 0);

            btnDeployNow = new RoundedButton();
            btnDeployNow.Text = "DEPLOY NOW";
            btnDeployNow.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            btnDeployNow.Size = new Size(150, 38);
            btnDeployNow.BackColor = ClrMaroon;
            btnDeployNow.HoverColor = ClrMaroonDark;
            btnDeployNow.ForeColor = Color.White;
            btnDeployNow.Anchor = AnchorStyles.Right;
            btnDeployNow.Margin = new Padding(0);
            btnDeployNow.Visible = false;
            btnDeployNow.Click += BtnDeployNow_Click;
            quizRow.Controls.Add(btnDeployNow, 3, 0);

            // ---------- stat cards ----------
            TableLayoutPanel statsRow = new TableLayoutPanel();
            statsRow.Dock = DockStyle.Fill;
            statsRow.Margin = new Padding(24, 4, 24, 8);
            statsRow.BackColor = ClrPageBg;
            statsRow.ColumnCount = 5;
            statsRow.RowCount = 1;
            for (int i = 0; i < 5; i++)
                statsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
            statsRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            table.Controls.Add(statsRow, 0, 2);

            valTotal = AddStatCard(statsRow, 0, "Total Students", ClrMaroon);
            valTaking = AddStatCard(statsRow, 1, "Taking Now", ClrGreen);
            valDisconnected = AddStatCard(statsRow, 2, "Disconnected", ClrYellow);
            valSubmitted = AddStatCard(statsRow, 3, "Submitted", ClrBlue);
            valAverage = AddStatCard(statsRow, 4, "Average Score", ClrGoldDark);

            // ---------- filter row ----------
            TableLayoutPanel filterRow = new TableLayoutPanel();
            filterRow.Dock = DockStyle.Fill;
            filterRow.Margin = new Padding(24, 0, 24, 0);
            filterRow.BackColor = ClrPageBg;
            filterRow.ColumnCount = 7;
            filterRow.RowCount = 1;
            filterRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 260)); // search
            filterRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));      // "Status"
            filterRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170)); // combo
            filterRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));      // auto-refresh
            filterRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));  // spacer
            filterRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));      // refresh
            filterRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));      // export
            filterRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            table.Controls.Add(filterRow, 0, 3);
            filterPanel = filterRow;

            // search box (rounded)
            CardPanel searchHost = new CardPanel();
            searchHost.Radius = 8;
            searchHost.BackColor = ClrPageBg;
            searchHost.Size = new Size(250, 38);
            searchHost.Anchor = AnchorStyles.Left;
            searchHost.Margin = new Padding(0, 0, 10, 0);
            filterRow.Controls.Add(searchHost, 0, 0);

            Label lblSearchIcon = new Label();
            lblSearchIcon.Text = "\uE721"; // Segoe MDL2 Assets: search
            lblSearchIcon.Font = new Font("Segoe MDL2 Assets", 11F);
            lblSearchIcon.ForeColor = Color.FromArgb(130, 130, 130);
            lblSearchIcon.BackColor = Color.White;
            lblSearchIcon.AutoSize = true;
            lblSearchIcon.Location = new Point(12, 10);
            searchHost.Controls.Add(lblSearchIcon);

            txtSearch = new TextBox();
            txtSearch.Font = new Font("Segoe UI", 10F);
            txtSearch.BorderStyle = BorderStyle.None;
            txtSearch.BackColor = Color.White;
            txtSearch.Location = new Point(38, 10);
            txtSearch.Width = 198;
            txtSearch.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            txtSearch.TextChanged += (s, e) => ApplyFilter();
            searchHost.Controls.Add(txtSearch);

            Label lblStatusFilter = new Label();
            lblStatusFilter.Text = "Status";
            lblStatusFilter.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            lblStatusFilter.ForeColor = ClrLabelGray;
            lblStatusFilter.BackColor = Color.Transparent;
            lblStatusFilter.AutoSize = true;
            lblStatusFilter.Anchor = AnchorStyles.Left;
            lblStatusFilter.Margin = new Padding(8, 0, 10, 0);
            filterRow.Controls.Add(lblStatusFilter, 1, 0);

            cmbStatus = new ComboBox();
            cmbStatus.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbStatus.Font = new Font("Segoe UI", 10F);
            cmbStatus.Items.AddRange(new object[] { AllStatuses, "Taking Quiz", "Disconnected", "Submitted" });
            cmbStatus.SelectedIndex = 0;
            cmbStatus.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            cmbStatus.Margin = new Padding(0, 0, 18, 0);
            cmbStatus.SelectedIndexChanged += (s, e) => ApplyFilter();
            filterRow.Controls.Add(cmbStatus, 2, 0);

            chkAuto = new CheckBox();
            chkAuto.Text = "Auto-refresh (5 sec)";
            chkAuto.Font = new Font("Segoe UI", 9.5F);
            chkAuto.ForeColor = ClrLabelGray;
            chkAuto.BackColor = Color.Transparent;
            chkAuto.Checked = true;
            chkAuto.AutoSize = true;
            chkAuto.Anchor = AnchorStyles.Left;
            chkAuto.Margin = new Padding(0);
            chkAuto.CheckedChanged += (s, e) =>
            {
                if (refreshTimer != null) refreshTimer.Enabled = chkAuto.Checked;
            };
            filterRow.Controls.Add(chkAuto, 3, 0);

            btnRefresh = new RoundedButton();
            btnRefresh.Text = "⟳  REFRESH";
            btnRefresh.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            btnRefresh.Size = new Size(130, 38);
            btnRefresh.BackColor = ClrMaroon;
            btnRefresh.HoverColor = ClrMaroonDark;
            btnRefresh.ForeColor = Color.White;
            btnRefresh.Anchor = AnchorStyles.Right;
            btnRefresh.Margin = new Padding(0, 0, 10, 0);
            btnRefresh.Click += (s, e) => RefreshNow();
            filterRow.Controls.Add(btnRefresh, 5, 0);

            btnExport = new RoundedButton();
            btnExport.Text = "⬇  EXPORT CSV";
            btnExport.Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold);
            btnExport.Size = new Size(150, 38);
            btnExport.BackColor = ClrGold;
            btnExport.HoverColor = ClrGoldDark;
            btnExport.ForeColor = ClrMaroonDark;
            btnExport.Anchor = AnchorStyles.Right;
            btnExport.Margin = new Padding(0);
            btnExport.Click += BtnExport_Click;
            filterRow.Controls.Add(btnExport, 6, 0);

            // ---------- grid (inside a rounded card) ----------
            CardPanel gridHost = new CardPanel();
            gridHost.Dock = DockStyle.Fill;
            gridHost.Radius = 8;
            gridHost.BackColor = ClrPageBg;
            gridHost.Margin = new Padding(24, 0, 24, 8);
            gridHost.Padding = new Padding(3);
            table.Controls.Add(gridHost, 0, 4);

            dgv = new DataGridView();
            dgv.Dock = DockStyle.Fill;
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.AllowUserToResizeRows = false;
            dgv.AllowUserToResizeColumns = false;
            dgv.ReadOnly = true;
            dgv.MultiSelect = false;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.AutoGenerateColumns = false;
            dgv.RowHeadersVisible = false;
            dgv.BackgroundColor = Color.White;
            dgv.BorderStyle = BorderStyle.None;
            dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dgv.GridColor = Color.FromArgb(240, 236, 224);
            dgv.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgv.ColumnHeadersHeight = 42;
            dgv.RowTemplate.Height = 48;
            dgv.EnableHeadersVisualStyles = false;

            // less flicker on the 5-second refresh
            typeof(DataGridView).GetProperty("DoubleBuffered",
                BindingFlags.Instance | BindingFlags.NonPublic).SetValue(dgv, true, null);

            dgv.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Bold),
                BackColor = Color.FromArgb(252, 247, 232),
                ForeColor = ClrMaroonDark,
                SelectionBackColor = Color.FromArgb(252, 247, 232),
                SelectionForeColor = ClrMaroonDark,
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0)
            };

            dgv.DefaultCellStyle = new DataGridViewCellStyle
            {
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = ClrBlack,
                BackColor = Color.White,
                SelectionBackColor = Color.FromArgb(250, 243, 222),
                SelectionForeColor = ClrBlack,
                Padding = new Padding(10, 0, 0, 0),
                WrapMode = DataGridViewTriState.False
            };

            dgv.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(253, 251, 246),
                SelectionBackColor = Color.FromArgb(250, 243, 222),
                SelectionForeColor = ClrBlack
            };

            AddColumn("Student", "Student", 28);
            AddColumn("Status", "Status", 17);
            AddColumn("Progress", "Progress", 20);
            AddColumn("TimeLeft", "Time Left", 11);
            AddColumn("Score", "Score", 15);
            AddColumn("LastSeen", "Last Seen", 11);

            dgv.CellPainting += Dgv_CellPainting;

            // empty state
            dgv.Paint += (s, e) =>
            {
                if (dgv.Rows.Count > 0) return;
                var area = new Rectangle(0, dgv.ColumnHeadersHeight, dgv.Width, dgv.Height - dgv.ColumnHeadersHeight);
                using (var f = new Font("Segoe UI", 10.5F))
                    TextRenderer.DrawText(e.Graphics, "No students to show yet", f, area,
                        Color.FromArgb(140, 140, 140),
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };

            gridHost.Controls.Add(dgv);

            // ---------- footer ----------
            Panel footer = new Panel();
            footer.Dock = DockStyle.Fill;
            footer.Margin = new Padding(0);
            footer.BackColor = ClrPageBg;
            footer.Paint += Legend_Paint;
            table.Controls.Add(footer, 0, 5);

            lblUpdated = new Label();
            lblUpdated.Text = "";
            lblUpdated.Font = new Font("Segoe UI", 8.5F, FontStyle.Italic);
            lblUpdated.ForeColor = Color.FromArgb(100, 116, 139);
            lblUpdated.BackColor = Color.Transparent;
            lblUpdated.AutoSize = false;
            lblUpdated.Dock = DockStyle.Right;
            lblUpdated.Width = 300;
            lblUpdated.TextAlign = ContentAlignment.MiddleRight;
            lblUpdated.Padding = new Padding(0, 0, 24, 0);
            footer.Controls.Add(lblUpdated);

            // ---------- refresh timer ----------
            refreshTimer = new System.Windows.Forms.Timer();
            refreshTimer.Interval = 5000;
            refreshTimer.Tick += (s, e) => RefreshNow();
            refreshTimer.Enabled = true;
        }

        private void AddColumn(string name, string header, float weight)
        {
            DataGridViewTextBoxColumn col = new DataGridViewTextBoxColumn();
            col.Name = name;
            col.HeaderText = header;
            col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            col.FillWeight = weight;
            col.SortMode = DataGridViewColumnSortMode.NotSortable;
            dgv.Columns.Add(col);
        }
        private Label AddStatCard(TableLayoutPanel host, int column, string caption, Color accent)
        {
            CardPanel card = new CardPanel();
            card.Dock = DockStyle.Fill;
            card.Radius = 12;
            card.BackColor = ClrPageBg;
            card.Accent = accent;
            card.Margin = new Padding(0, 0, column == 4 ? 0 : 12, 0);

            Label value = new Label();
            value.Text = "0";
            value.Font = new Font("Segoe UI Semibold", 22F, FontStyle.Bold);
            value.ForeColor = accent;
            value.BackColor = Color.Transparent;
            value.AutoSize = true;
            value.Location = new Point(30, 12);
            card.Controls.Add(value);

            Label cap = new Label();
            cap.Text = caption;
            cap.Font = new Font("Segoe UI", 9F);
            cap.ForeColor = Color.FromArgb(110, 110, 110);
            cap.BackColor = Color.Transparent;
            cap.AutoSize = true;
            cap.Location = new Point(32, 58);
            card.Controls.Add(cap);

            host.Controls.Add(card, column, 0);
            return value;
        }

        private Label AddStatCard(FlowLayoutPanel host, string caption, Color accent)
        {
            Panel card = new Panel();
            card.Size = new Size(176, 74);
            card.BackColor = Color.White;
            card.Margin = new Padding(0, 0, 12, 0);
            card.Paint += (s, e) =>
            {
                using (var pen = new Pen(ClrGold, 1.2f))
                    e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
                using (var br = new SolidBrush(accent))
                    e.Graphics.FillRectangle(br, 0, 0, 5, card.Height);
            };

            Label value = new Label();
            value.Text = "0";
            value.Font = new Font("Segoe UI Semibold", 20F, FontStyle.Bold);
            value.ForeColor = accent;
            value.AutoSize = true;
            value.Location = new Point(16, 6);
            card.Controls.Add(value);

            Label cap = new Label();
            cap.Text = caption;
            cap.Font = new Font("Segoe UI", 9F);
            cap.ForeColor = ClrLabelGray;
            cap.AutoSize = true;
            cap.Location = new Point(18, 49);
            card.Controls.Add(cap);

            host.Controls.Add(card);
            return value;
        }

        // =========================================================
        // LOAD QUIZ LIST
        // =========================================================

        private void LoadQuizList()
        {
            cmbQuiz.Items.Clear();

            try
            {
                string connStr = SettingsManager.Current.GetConnectionString();

                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    hasDeployColumn = QuizSchema.EnsureDeployColumn(conn);

                    string deployCols = hasDeployColumn
                        ? "deploy_at, (deploy_at IS NULL OR deploy_at <= NOW()) AS is_open"
                        : "NULL AS deploy_at, 1 AS is_open";

                    string sql =
                        @"SELECT quiz_id, quiz_title, subject, exam_period,
                                 assessment_type, duration_minutes, " + deployCols + @"
                          FROM quizzes
                          WHERE created_by = @prof
                          ORDER BY quiz_id DESC";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@prof", professorUserId);

                        using (var r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                QuizItem item = new QuizItem();
                                item.Id = Convert.ToInt32(r["quiz_id"]);
                                item.Title = Str(r["quiz_title"]);
                                item.Subject = Str(r["subject"]);
                                item.Period = Str(r["exam_period"]).ToUpper();
                                item.Type = Str(r["assessment_type"]).ToUpper();
                                item.Duration = r["duration_minutes"] == DBNull.Value
                                    ? 0 : Convert.ToInt32(r["duration_minutes"]);
                                item.DeployAt = r["deploy_at"] == DBNull.Value
                                    ? (DateTime?)null : Convert.ToDateTime(r["deploy_at"]);
                                item.IsOpen = Convert.ToInt32(r["is_open"]) == 1;
                                cmbQuiz.Items.Add(item);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show(
                    "Unable to load your quizzes / exams.\n\n" + ex.Message,
                    "Monitoring",
                    CustomMessageBoxButtons.OK,
                    CustomMessageBoxIcon.Warning);
            }

            if (cmbQuiz.Items.Count == 0)
            {
                lblDeploy.Text = "You have not created any quiz or exam yet.";
                lblDeploy.ForeColor = ClrLabelGray;
                btnDeployNow.Visible = false;
                ApplyFilter();
                return;
            }

            int selectIndex = 0;
            for (int i = 0; i < cmbQuiz.Items.Count; i++)
            {
                if (((QuizItem)cmbQuiz.Items[i]).Id == initialQuizId)
                {
                    selectIndex = i;
                    break;
                }
            }

            cmbQuiz.SelectedIndex = selectIndex;
        }

        private static string Str(object o)
        {
            return o == null || o == DBNull.Value ? "" : o.ToString().Trim();
        }

        // =========================================================
        // QUIZ CHANGED / DEPLOY STATE
        // =========================================================

        private void OnQuizChanged()
        {
            currentQuiz = cmbQuiz.SelectedItem as QuizItem;
            UpdateDeployLabel();
            RefreshNow();
        }

        private void UpdateDeployLabel()
        {
            if (currentQuiz == null)
            {
                lblDeploy.Text = "";
                btnDeployNow.Visible = false;
                return;
            }

            if (currentQuiz.IsOpen)
            {
                lblDeploy.Text = "●  Deployed — students can see this now";
                lblDeploy.ForeColor = ClrGreen;
                btnDeployNow.Visible = false;
            }
            else
            {
                string when = currentQuiz.DeployAt.HasValue
                    ? currentQuiz.DeployAt.Value.ToString("MMM dd, yyyy  hh:mm tt")
                    : "";

                lblDeploy.Text = "◷  Scheduled: " + when + " — hidden from students";
                lblDeploy.ForeColor = ClrYellow;
                btnDeployNow.Visible = true;
            }
        }

        private void BtnDeployNow_Click(object sender, EventArgs e)
        {
            if (currentQuiz == null || currentQuiz.IsOpen) return;

            var confirm = CustomMessageBox.Show(
                "Deploy \"" + currentQuiz.Title + "\" right now?\n\n" +
                "Students will be able to see and take it immediately.",
                "Deploy Now",
                CustomMessageBoxButtons.YesNo,
                CustomMessageBoxIcon.Question);

            if (confirm != CustomMessageBoxResult.Yes) return;

            try
            {
                string connStr = SettingsManager.Current.GetConnectionString();

                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    using (var cmd = new MySqlCommand(
                        @"UPDATE quizzes SET deploy_at = NOW()
                          WHERE quiz_id = @id AND created_by = @prof", conn))
                    {
                        cmd.Parameters.AddWithValue("@id", currentQuiz.Id);
                        cmd.Parameters.AddWithValue("@prof", professorUserId);
                        cmd.ExecuteNonQuery();
                    }
                }

                currentQuiz.IsOpen = true;
                currentQuiz.DeployAt = DateTime.Now;
                UpdateDeployLabel();
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show(
                    "Could not deploy the quiz / exam.\n\n" + ex.Message,
                    "Deploy Now",
                    CustomMessageBoxButtons.OK,
                    CustomMessageBoxIcon.Error);
            }
        }

        // =========================================================
        // REFRESH
        // =========================================================

        private void RefreshNow()
        {
            if (isRefreshing) return;

            if (currentQuiz == null)
            {
                allRows.Clear();
                ApplyFilter();
                return;
            }

            isRefreshing = true;

            try
            {
                LoadAttempts();
                ApplyFilter();
                lblUpdated.Text = "Last updated: " + DateTime.Now.ToString("hh:mm:ss tt");
                
            }
            finally
            {
                isRefreshing = false;
            }
        }

        private void LoadAttempts()
        {
            List<AttemptRow> rows = new List<AttemptRow>();

            try
            {
                string connStr = SettingsManager.Current.GetConnectionString();

                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    // Keep the deploy state fresh (a scheduled quiz may have just opened)
                    if (hasDeployColumn)
                    {
                        using (var dc = new MySqlCommand(
                            @"SELECT deploy_at, (deploy_at IS NULL OR deploy_at <= NOW()) AS is_open
                              FROM quizzes WHERE quiz_id = @id", conn))
                        {
                            dc.Parameters.AddWithValue("@id", currentQuiz.Id);

                            using (var r = dc.ExecuteReader())
                            {
                                if (r.Read())
                                {
                                    currentQuiz.DeployAt = r["deploy_at"] == DBNull.Value
                                        ? (DateTime?)null : Convert.ToDateTime(r["deploy_at"]);
                                    currentQuiz.IsOpen = Convert.ToInt32(r["is_open"]) == 1;
                                }
                            }
                        }

                        UpdateDeployLabel();
                    }

                    string sql = @"
                        SELECT qa.attempt_id, qa.user_id, qa.status, qa.last_seen,
                               qa.remaining_seconds, qa.score, qa.total_questions,
                               qa.percentage, qa.student_name,
                               ui.lastname, ui.firstname, ui.middlename,
                               uc.full_name,
                               (SELECT COUNT(*) FROM student_answers sa
                                 WHERE sa.attempt_id = qa.attempt_id) AS answered
                        FROM quiz_attempts qa
                        LEFT JOIN user_information ui ON ui.user_id = qa.user_id
                        LEFT JOIN user_credential uc ON uc.user_id = qa.user_id
                        WHERE qa.quiz_id = @quiz_id";

                    using (var cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@quiz_id", currentQuiz.Id);

                        using (var r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                AttemptRow row = new AttemptRow();

                                row.Name = BuildName(
                                    Convert.ToInt32(r["user_id"]),
                                    r["lastname"], r["firstname"], r["middlename"],
                                    r["student_name"], r["full_name"]);

                                DateTime? lastSeen = r["last_seen"] == DBNull.Value
                                    ? (DateTime?)null : Convert.ToDateTime(r["last_seen"]);

                                row.LastSeen = lastSeen;
                                row.Status = GetDisplayStatus(Str(r["status"]), lastSeen);
                                row.Answered = Convert.ToInt32(r["answered"]);
                                row.Total = r["total_questions"] == DBNull.Value
                                    ? 0 : Convert.ToInt32(r["total_questions"]);

                                if (r["remaining_seconds"] != DBNull.Value)
                                {
                                    row.HasRemaining = true;
                                    row.RemainingSeconds = Math.Max(0, Convert.ToInt32(r["remaining_seconds"]));
                                }

                                row.Score = r["score"] == DBNull.Value ? 0 : Convert.ToInt32(r["score"]);
                                row.Percentage = r["percentage"] == DBNull.Value
                                    ? 0m : Convert.ToDecimal(r["percentage"]);

                                rows.Add(row);
                            }
                        }
                    }
                }

                allRows = rows.OrderBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Monitoring LoadAttempts error: " + ex.Message);
                lblUpdated.Text = "Refresh failed — retrying...";
            }
        }

        // "Last, First Middle" from user_information; falls back to the name saved on the attempt
        private static string BuildName(int userId, object ln, object fn, object mn,
                                        object studentName, object fullName)
        {
            string last = Str(ln);
            string first = (Str(fn) + " " + Str(mn)).Trim();

            if (last != "" || first != "")
            {
                if (last != "" && first != "") return last + ", " + first;
                return last + first;
            }

            string saved = Str(studentName).Replace("_", " ").Trim();
            if (saved != "" && !saved.Equals("Unknown", StringComparison.OrdinalIgnoreCase))
                return saved;

            string full = Str(fullName);
            if (full != "" && !full.Equals("Unknown", StringComparison.OrdinalIgnoreCase))
                return full;

            return "Student #" + userId;
        }

        private static string GetDisplayStatus(string dbStatus, DateTime? lastSeen)
        {
            string status = (dbStatus ?? "").Trim().ToUpper();

            if (status == "SUBMITTED") return "Submitted";
            if (status == "DISCONNECTED") return "Disconnected";

            if (status == "TAKING")
            {
                if (lastSeen.HasValue && (DateTime.Now - lastSeen.Value).TotalSeconds <= 15)
                    return "Taking Quiz";

                return "Disconnected";
            }

            return string.IsNullOrEmpty(status) ? "Unknown" : status;
        }

        // =========================================================
        // FILTER + FILL GRID + SUMMARY
        // =========================================================

        private void ApplyFilter()
        {
            // ---- summary (always from all students) ----
            int taking = allRows.Count(x => x.Status == "Taking Quiz");
            int disconnected = allRows.Count(x => x.Status == "Disconnected");
            var submittedRows = allRows.Where(x => x.Status == "Submitted").ToList();

            valTotal.Text = allRows.Count.ToString();
            valTaking.Text = taking.ToString();
            valDisconnected.Text = disconnected.ToString();
            valSubmitted.Text = submittedRows.Count.ToString();
            valAverage.Text = submittedRows.Count == 0
                ? "—"
                : submittedRows.Average(x => (double)x.Percentage).ToString("0.#") + "%";

            // ---- filtered rows ----
            string search = txtSearch.Text.Trim();
            string statusFilter = cmbStatus.SelectedItem == null
                ? AllStatuses : cmbStatus.SelectedItem.ToString();

            IEnumerable<AttemptRow> view = allRows;

            if (search.Length > 0)
                view = view.Where(x => x.Name.IndexOf(search, StringComparison.CurrentCultureIgnoreCase) >= 0);

            if (statusFilter != AllStatuses)
                view = view.Where(x => x.Status == statusFilter);

            string selectedName = dgv.CurrentRow != null
                ? Convert.ToString(dgv.CurrentRow.Cells[0].Value) : null;
            int firstVisible = dgv.FirstDisplayedScrollingRowIndex;

            dgv.SuspendLayout();
            dgv.Rows.Clear();
            dgv.Invalidate();

            foreach (AttemptRow row in view)
            {
                int idx = dgv.Rows.Add(
                    row.Name,
                    row.Status,
                    ProgressText(row),
                    TimeLeftText(row),
                    ScoreText(row),
                    LastSeenText(row.LastSeen));

                if (selectedName != null && row.Name == selectedName)
                {
                    dgv.ClearSelection();
                    dgv.Rows[idx].Selected = true;
                    dgv.Rows[idx].Tag = row;
                }
            }

            if (firstVisible >= 0 && firstVisible < dgv.Rows.Count)
            {
                try { dgv.FirstDisplayedScrollingRowIndex = firstVisible; } catch { }
            }

            dgv.ResumeLayout();
            dgv.Invalidate();
        }

        private static string ProgressText(AttemptRow r)
        {
            if (r.Total <= 0) return r.Answered + " answered";

            int pct = (int)Math.Round(100.0 * r.Answered / r.Total);
            return r.Answered + " / " + r.Total + "  (" + pct + "%)";
        }

        private static string TimeLeftText(AttemptRow r)
        {
            if (r.Status == "Submitted" || !r.HasRemaining) return "—";

            int h = r.RemainingSeconds / 3600;
            int m = (r.RemainingSeconds % 3600) / 60;
            int s = r.RemainingSeconds % 60;

            return h > 0
                ? h + ":" + m.ToString("00") + ":" + s.ToString("00")
                : m.ToString("00") + ":" + s.ToString("00");
        }

        private static string ScoreText(AttemptRow r)
        {
            if (r.Status != "Submitted") return "—";
            return r.Score + " / " + r.Total + "  (" + r.Percentage.ToString("0.#") + "%)";
        }

        private static string LastSeenText(DateTime? seen)
        {
            if (!seen.HasValue) return "—";

            double sec = (DateTime.Now - seen.Value).TotalSeconds;
            if (sec < 0) sec = 0;

            if (sec < 60) return (int)sec + "s ago";
            if (sec < 3600) return (int)(sec / 60) + "m ago";
            return seen.Value.ToString("MMM dd hh:mm tt");
        }

        // =========================================================
        // EXPORT CSV
        // =========================================================

        private void BtnExport_Click(object sender, EventArgs e)
        {
            if (currentQuiz == null || allRows.Count == 0)
            {
                CustomMessageBox.Show(
                    "There is nothing to export yet.",
                    "Export",
                    CustomMessageBoxButtons.OK,
                    CustomMessageBoxIcon.Information);
                return;
            }

            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Title = "Export Monitoring Report";
                dialog.Filter = "CSV file (*.csv)|*.csv";

                string safeTitle = currentQuiz.Title;
                foreach (char c in Path.GetInvalidFileNameChars())
                    safeTitle = safeTitle.Replace(c, '_');

                dialog.FileName = "Monitoring_" + safeTitle + "_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".csv";

                if (dialog.ShowDialog() != DialogResult.OK) return;

                try
                {
                    StringBuilder sb = new StringBuilder();

                    sb.AppendLine(Csv("Quiz / Exam") + "," + Csv(currentQuiz.Title));
                    sb.AppendLine(Csv("Subject") + "," + Csv(currentQuiz.Subject));
                    sb.AppendLine(Csv("Exported") + "," + Csv(DateTime.Now.ToString("MMM dd, yyyy hh:mm tt")));
                    sb.AppendLine();

                    sb.AppendLine(string.Join(",", new[]
                    {
                        Csv("Student"), Csv("Status"), Csv("Answered"), Csv("Total Questions"),
                        Csv("Progress %"), Csv("Time Left"), Csv("Score"), Csv("Percentage"), Csv("Last Seen")
                    }));

                    foreach (AttemptRow r in allRows)
                    {
                        int pct = r.Total > 0 ? (int)Math.Round(100.0 * r.Answered / r.Total) : 0;
                        bool done = r.Status == "Submitted";

                        sb.AppendLine(string.Join(",", new[]
                        {
                            Csv(r.Name),
                            Csv(r.Status),
                            Csv(r.Answered.ToString()),
                            Csv(r.Total.ToString()),
                            Csv(pct.ToString()),
                            Csv(TimeLeftText(r)),
                            Csv(done ? r.Score.ToString() : ""),
                            Csv(done ? r.Percentage.ToString("0.##") : ""),
                            Csv(r.LastSeen.HasValue ? r.LastSeen.Value.ToString("yyyy-MM-dd HH:mm:ss") : "")
                        }));
                    }

                    File.WriteAllText(dialog.FileName, sb.ToString(), new UTF8Encoding(true));

                    CustomMessageBox.Show(
                        "Report exported successfully!",
                        "Export",
                        CustomMessageBoxButtons.OK,
                        CustomMessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    CustomMessageBox.Show(
                        "Could not export the report.\n\n" + ex.Message,
                        "Export",
                        CustomMessageBoxButtons.OK,
                        CustomMessageBoxIcon.Error);
                }
            }
        }

        private static string Csv(string value)
        {
            if (value == null) value = "";
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        // =========================================================
        // STATUS DOT + LEGEND
        // =========================================================

        private static Color StatusColor(string status)
        {
            if (status == "Taking Quiz") return ClrGreen;
            if (status == "Disconnected") return ClrYellow;
            if (status == "Submitted") return ClrBlue;
            return ClrRed;
        }

        private void Dgv_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            string colName = dgv.Columns[e.ColumnIndex].Name;
            if (colName != "Status" && colName != "Progress") return;

            e.PaintBackground(e.ClipBounds, true);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            if (colName == "Status")
            {
                string status = e.Value == null ? "" : e.Value.ToString();
                Color c = StatusColor(status);

                using (var font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold))
                {
                    Size ts = TextRenderer.MeasureText(status, font,
                        new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding);

                    int h = 26;
                    var pill = new Rectangle(e.CellBounds.Left + 10,
                        e.CellBounds.Top + (e.CellBounds.Height - h) / 2, ts.Width + 36, h);

                    using (var path = Gfx.RoundPath(pill, h / 2))
                    using (var br = new SolidBrush(Color.FromArgb(30, c)))
                        e.Graphics.FillPath(br, path);

                    using (var br = new SolidBrush(c))
                        e.Graphics.FillEllipse(br, pill.Left + 12, pill.Top + (h - 8) / 2, 8, 8);

                    TextRenderer.DrawText(e.Graphics, status, font,
                        new Point(pill.Left + 26, pill.Top + (h - ts.Height) / 2),
                        c, TextFormatFlags.NoPadding);
                }
            }
            else // Progress
            {
                AttemptRow row = dgv.Rows[e.RowIndex].Tag as AttemptRow;
                string text = e.Value == null ? "" : e.Value.ToString();
                double frac = (row != null && row.Total > 0)
                    ? Math.Min(1.0, (double)row.Answered / row.Total) : 0;

                TextRenderer.DrawText(e.Graphics, text, dgv.DefaultCellStyle.Font,
                    new Point(e.CellBounds.Left + 10, e.CellBounds.Top + 7),
                    ClrBlack, TextFormatFlags.NoPadding);

                int barW = Math.Min(150, e.CellBounds.Width - 24);
                if (barW > 12)
                {
                    var track = new Rectangle(e.CellBounds.Left + 10, e.CellBounds.Top + 30, barW, 6);

                    using (var path = Gfx.RoundPath(track, 3))
                    using (var br = new SolidBrush(Color.FromArgb(234, 230, 220)))
                        e.Graphics.FillPath(br, path);

                    if (frac > 0)
                    {
                        var fill = new Rectangle(track.Left, track.Top,
                            Math.Max(6, (int)(barW * frac)), track.Height);
                        Color fc = row != null ? StatusColor(row.Status) : ClrGold;

                        using (var path = Gfx.RoundPath(fill, 3))
                        using (var br = new SolidBrush(fc))
                            e.Graphics.FillPath(br, path);
                    }
                }
            }

            e.Handled = true;
        }

        private void Legend_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            Panel p = (Panel)sender;
            int dot = 10;
            int y = (p.Height - dot) / 2;
            int x = 24;

            using (var font = new Font("Segoe UI", 8.5F))
            {
                DrawLegendItem(e.Graphics, ref x, y, dot, ClrGreen, "Taking Quiz", font);
                x += 20;
                DrawLegendItem(e.Graphics, ref x, y, dot, ClrYellow, "Disconnected", font);
                x += 20;
                DrawLegendItem(e.Graphics, ref x, y, dot, ClrBlue, "Submitted", font);
            }
        }

        private void DrawLegendItem(Graphics g, ref int x, int y, int dot, Color color, string text, Font font)
        {
            using (var br = new SolidBrush(color))
                g.FillEllipse(br, x, y, dot, dot);

            x += dot + 6;

            SizeF size = g.MeasureString(text, font);

            using (var tb = new SolidBrush(ClrLabelGray))
                g.DrawString(text, font, tb, x, y - 3);

            x += (int)size.Width;
        }
    }
}