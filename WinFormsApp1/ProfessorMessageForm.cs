using Guna.UI2.WinForms;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace WinFormsApp1
{
    public partial class ProfessorMessageForm : Form
    {
        private readonly string _message;
        private readonly int _durationSeconds;

        private Label lblCountdown;
        private System.Windows.Forms.Timer countdownTimer;
        private System.Windows.Forms.Timer closeTimer;
        private System.Windows.Forms.Timer keepOnTopTimer;

        private int _secondsLeft;
        private bool _allowClose = false;   // ★ set to true when the timer says it's OK to close

        // =========================================================
        // CONSTRUCTOR
        // =========================================================
        public ProfessorMessageForm(string message, int durationSeconds = 8)
        {
            InitializeComponent();

            _message = message ?? "";
            _durationSeconds = durationSeconds < 3 ? 3 : durationSeconds;
            _secondsLeft = _durationSeconds;

            BuildUi();
            WireUpTimers();
        }

        // =========================================================
        // UI
        // =========================================================
        private void BuildUi()
        {
            // ---------- FORM (full-screen overlay, like LockScreenForm) ----------
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.KeyPreview = true;
            this.BackColor = Color.FromArgb(15, 0, 0);
            this.Opacity = 0.97;

            var screen = Screen.PrimaryScreen.Bounds;
            this.Bounds = screen;

            // ---------- CENTERED CARD ----------
            var card = new Guna2Panel
            {
                Size = new Size(860, 500),
                Location = new Point(
                    (screen.Width - 860) / 2,
                    (screen.Height - 500) / 2),
                BorderRadius = 26,
                FillColor = Color.White,
                BorderColor = Color.Maroon,
                BorderThickness = 4,
                ShadowDecoration = { Enabled = true, Depth = 30, Color = Color.FromArgb(140, 0, 0, 0) }
            };
            this.Controls.Add(card);

            // ---------- TOP BAR (maroon) ----------
            var topBar = new Guna2Panel
            {
                Dock = DockStyle.Top,
                Height = 96,
                FillColor = Color.Maroon,
                BorderRadius = 26,
                CustomizableEdges = { BottomLeft = false, BottomRight = false }
            };
            card.Controls.Add(topBar);

            // Warning icon
            var lblIcon = new Label
            {
                Text = "⚠️",
                Font = new Font("Segoe UI Emoji", 40F),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                AutoSize = false,
                Size = new Size(100, 96),
                Location = new Point(20, 0),
                TextAlign = ContentAlignment.MiddleCenter
            };
            topBar.Controls.Add(lblIcon);

            // Header text
            var lblHeader = new Label
            {
                Text = "MESSAGE FROM YOUR PROFESSOR",
                Font = new Font("Segoe UI Semibold", 22F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                AutoSize = false,
                Size = new Size(720, 96),
                Location = new Point(120, 0),
                TextAlign = ContentAlignment.MiddleLeft
            };
            topBar.Controls.Add(lblHeader);

            // ---------- MESSAGE BODY ----------
            var lblMessage = new Label
            {
                Text = _message,
                Font = new Font("Segoe UI", 20F, FontStyle.Regular),
                ForeColor = Color.FromArgb(30, 30, 30),
                BackColor = Color.Transparent,
                AutoSize = false,
                Size = new Size(800, 300),
                Location = new Point(30, 130),
                TextAlign = ContentAlignment.MiddleCenter
            };
            card.Controls.Add(lblMessage);

            // ---------- COUNTDOWN FOOTER ----------
            lblCountdown = new Label
            {
                Text = $"This message will close in {_secondsLeft} seconds",
                Font = new Font("Segoe UI", 12F, FontStyle.Italic),
                ForeColor = Color.FromArgb(140, 140, 140),
                BackColor = Color.Transparent,
                AutoSize = false,
                Size = new Size(800, 40),
                Location = new Point(30, 440),
                TextAlign = ContentAlignment.MiddleCenter
            };
            card.Controls.Add(lblCountdown);
        }

        // =========================================================
        // TIMERS
        // =========================================================
        private void WireUpTimers()
        {
            // Countdown label updater
            countdownTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            countdownTimer.Tick += (s, e) =>
            {
                _secondsLeft--;

                if (_secondsLeft <= 0)
                {
                    if (lblCountdown != null && !lblCountdown.IsDisposed)
                        lblCountdown.Text = "Closing...";
                }
                else
                {
                    if (lblCountdown != null && !lblCountdown.IsDisposed)
                        lblCountdown.Text = $"This message will close in {_secondsLeft} second{(_secondsLeft == 1 ? "" : "s")}";
                }
            };
            countdownTimer.Start();

            // Auto-close
            closeTimer = new System.Windows.Forms.Timer { Interval = _durationSeconds * 1000 };
            closeTimer.Tick += (s, e) =>
            {
                try
                {
                    _allowClose = true;   // ★ permit closing

                    countdownTimer?.Stop();
                    countdownTimer?.Dispose();
                    closeTimer?.Stop();
                    closeTimer?.Dispose();

                    if (!this.IsDisposed)
                        this.Close();
                }
                catch { }
            };
            closeTimer.Start();

            // Force TopMost repeatedly
            keepOnTopTimer = new System.Windows.Forms.Timer { Interval = 250 };
            keepOnTopTimer.Tick += (s, e) =>
            {
                try
                {
                    if (this.IsDisposed)
                    {
                        keepOnTopTimer.Stop();
                        keepOnTopTimer.Dispose();
                        return;
                    }

                    if (!this.Focused)
                    {
                        this.BringToFront();
                        this.Activate();
                    }
                }
                catch
                {
                    keepOnTopTimer.Stop();
                    keepOnTopTimer.Dispose();
                }
            };
            keepOnTopTimer.Start();
        }

        // =========================================================
        // BLOCK EARLY DISMISSAL
        // =========================================================
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // Block ESC and Alt+F4 until the form is allowed to close
            if (!_allowClose)
            {
                if (keyData == Keys.Escape) return true;
                if (keyData == (Keys.Alt | Keys.F4)) return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Block manual closing only while the countdown is still running
            if (!_allowClose && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                return;
            }

            // Cleanup timers
            try { countdownTimer?.Stop(); countdownTimer?.Dispose(); } catch { }
            try { closeTimer?.Stop(); closeTimer?.Dispose(); } catch { }
            try { keepOnTopTimer?.Stop(); keepOnTopTimer?.Dispose(); } catch { }

            base.OnFormClosing(e);
        }

        // =========================================================
        // STATIC HELPER — easy to call from anywhere
        // =========================================================
        public static void ShowMessage(string message, int durationSeconds = 8)
        {
            try
            {
                var frm = new ProfessorMessageForm(message, durationSeconds);
                frm.Show();
            }
            catch (Exception ex)
            {
                Console.WriteLine("ProfessorMessageForm.ShowMessage error: " + ex.Message);
            }
        }
    }
}