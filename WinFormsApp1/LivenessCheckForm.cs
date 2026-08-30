using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using AForge.Video;
using AForge.Video.DirectShow;
using FaceONNX;

namespace WinFormsApp1
{
    public partial class LivenessCheckForm : Form
    {
        private FilterInfoCollection videoDevices;
        private VideoCaptureDevice videoSource;

        private Panel pnlBrand;
        private Panel pnlInstructions;
        private Panel pnlCamera;
        private ScannerView scannerView;
        private Label lblCamTitle;
        private Button btnStart;
        private System.Windows.Forms.Timer countdownTimer;
        private int countdown = 3;

        // ---- Face recognition ----
        private FaceRecognitionHelper faceHelper;
        private Bitmap referencePhoto;

        // ---- School colors: Colegio De San Gabriel Arcangel Inc. (Maroon & Gold) ----
        private static readonly Color ClrMaroon = Color.FromArgb(94, 14, 33);
        private static readonly Color ClrMaroonDark = Color.FromArgb(58, 8, 20);
        private static readonly Color ClrMaroonLight = Color.FromArgb(140, 26, 50);
        private static readonly Color ClrGold = Color.FromArgb(212, 175, 55);
        private static readonly Color ClrGoldSoft = Color.FromArgb(230, 205, 130);
        private static readonly Color ClrCream = Color.FromArgb(250, 247, 240);

        private static readonly Color ClrAmberBg = Color.FromArgb(255, 247, 224);
        private static readonly Color ClrAmberText = Color.FromArgb(146, 100, 6);
        private static readonly Color ClrGrayText = Color.FromArgb(90, 96, 105);

        private static readonly Color ClrSuccessGlow = Color.FromArgb(0, 200, 100);
        private static readonly Color ClrErrorGlow = Color.FromArgb(230, 55, 75);

        public bool VerificationPassed { get; private set; } = false;

        public LivenessCheckForm(Bitmap studentReferencePhoto)
        {
            referencePhoto = studentReferencePhoto;
            SetupUI();

            try
            {
                faceHelper = new FaceRecognitionHelper();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Hindi ma-load ang face recognition models.\n\n" + ex.Message,
                    "Model Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ================= UI SETUP =================
        private void SetupUI()
        {
            this.Text = "Face Verification — Colegio De San Gabriel Arcangel Inc.";
            this.ClientSize = new Size(1040, 660);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = ClrCream;
            this.Font = new Font("Segoe UI", 9F);

            int sidebarWidth = 320;
            int contentWidth = this.ClientSize.Width - sidebarWidth;

            // ================= LEFT BRAND SIDEBAR (persistent) =================
            pnlBrand = new Panel
            {
                Dock = DockStyle.Left,
                Width = sidebarWidth,
                BackColor = ClrMaroon
            };
            pnlBrand.Paint += PnlBrand_Paint;

            var picLogo = new PictureBox
            {
                Size = new Size(120, 120),
                Location = new Point((sidebarWidth - 120) / 2, 80),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent
            };
            try
            {
                // Ginagamit yung parehong logo na nasa Login form mo
                picLogo.Image = Properties.Resources._519651826_1547683232876514_5721937903657253200_n_removebg_preview;
            }
            catch { /* kung hindi ma-load, tuloy pa rin nang walang logo image */ }

            var lblSchoolName = new Label
            {
                Text = "COLEGIO DE SAN GABRIEL\nARCANGEL INC.",
                ForeColor = ClrGold,
                Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(sidebarWidth - 40, 60),
                Location = new Point(20, 215)
            };

            var lblDivider = new Panel
            {
                BackColor = ClrGold,
                Size = new Size(60, 2),
                Location = new Point((sidebarWidth - 60) / 2, 288)
            };

            var lblTagline = new Label
            {
                Text = "STUDENT IDENTITY\nVERIFICATION PORTAL",
                ForeColor = Color.FromArgb(220, 210, 200),
                Font = new Font("Consolas", 9F, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(sidebarWidth - 40, 40),
                Location = new Point(20, 306)
            };

            var lblFooter = new Label
            {
                Text = "🔒 Secured by AI Facial\nRecognition Technology",
                ForeColor = Color.FromArgb(180, 150, 160),
                Font = new Font("Segoe UI", 8.5F),
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(sidebarWidth - 40, 40),
                Location = new Point(20, this.ClientSize.Height - 70)
            };

            pnlBrand.Controls.Add(picLogo);
            pnlBrand.Controls.Add(lblSchoolName);
            pnlBrand.Controls.Add(lblDivider);
            pnlBrand.Controls.Add(lblTagline);
            pnlBrand.Controls.Add(lblFooter);

            // ================= RIGHT: INSTRUCTIONS PANEL =================
            pnlInstructions = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = ClrCream };

            int pad = 40;
            int innerWidth = contentWidth - pad * 2;
            int y = 30;

            var lblTitle = new Label
            {
                Text = "Take Live Selfie",
                Font = new Font("Segoe UI Semibold", 22, FontStyle.Bold),
                ForeColor = ClrMaroon,
                AutoSize = true,
                Location = new Point(pad, y)
            };
            y += 46;

            var lblDesc = new Label
            {
                Text = "You will go through a face verification process to prove that you are a real person.",
                Font = new Font("Segoe UI", 10.5F),
                ForeColor = ClrGrayText,
                Size = new Size(innerWidth, 30),
                Location = new Point(pad, y)
            };
            y += 46;

            var pnlWarning = new RoundedPanel(14)
            {
                Location = new Point(pad, y),
                Size = new Size(innerWidth, 70),
                BackColor = ClrAmberBg
            };
            var lblWarnIcon = new Label
            {
                Text = "⚠",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(217, 160, 20),
                AutoSize = true,
                Location = new Point(14, 10)
            };
            var lblWarnTitle = new Label
            {
                Text = "Photosensitivity warning",
                Font = new Font("Segoe UI Semibold", 10, FontStyle.Bold),
                ForeColor = ClrAmberText,
                AutoSize = true,
                Location = new Point(48, 8)
            };
            var lblWarnDesc = new Label
            {
                Text = "This check displays colored lights. Use caution if you are photosensitive.",
                Font = new Font("Segoe UI", 9),
                ForeColor = ClrAmberText,
                Size = new Size(innerWidth - 60, 36),
                Location = new Point(48, 28)
            };
            pnlWarning.Controls.Add(lblWarnIcon);
            pnlWarning.Controls.Add(lblWarnTitle);
            pnlWarning.Controls.Add(lblWarnDesc);
            y += 88;

            var lblAlign = new Label
            {
                Text = "Align your face and press Start Liveness to proceed",
                Font = new Font("Segoe UI Semibold", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 20, 20),
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(innerWidth, 36),
                Location = new Point(pad, y)
            };
            y += 48;

            int cardW = (innerWidth - 20) / 2;
            var pnlGood = new FaceGuideCard(true) { Location = new Point(pad, y), Size = new Size(cardW, 140) };
            var pnlBad = new FaceGuideCard(false) { Location = new Point(pad + cardW + 20, y), Size = new Size(cardW, 140) };
            y += 156;

            string[,] items = new string[,]
            {
                { "Hijab-friendly verification", "true" },
                { "Avoid wearing cap", "false" },
                { "Use enough lighting", "true" },
                { "Avoid wearing glasses", "false" }
            };

            for (int i = 0; i < 4; i++)
            {
                int col = i % 2;
                int row = i / 2;
                bool positive = items[i, 1] == "true";
                var item = new ChecklistItem(items[i, 0], positive)
                {
                    Location = new Point(pad + col * (cardW + 20), y + row * 52),
                    Size = new Size(cardW, 46)
                };
                pnlInstructions.Controls.Add(item);
            }
            y += 116;

            var lblConsent = new Label
            {
                Text = "By proceeding, you allow the collection and use of your camera image for identity verification purposes only.",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = Color.Gray,
                Size = new Size(innerWidth, 30),
                Location = new Point(pad, y),
                TextAlign = ContentAlignment.MiddleCenter
            };
            y += 44;

            btnStart = new RoundedButton
            {
                Text = "Start Liveness",
                Size = new Size(innerWidth, 50),
                Location = new Point(pad, y),
                BackColor = ClrMaroon,
                HoverColor = ClrMaroonLight,
                ForeColor = ClrGoldSoft,
                Font = new Font("Segoe UI Semibold", 11.5F, FontStyle.Bold)
            };
            btnStart.Click += BtnStart_Click;

            pnlInstructions.Controls.Add(lblTitle);
            pnlInstructions.Controls.Add(lblDesc);
            pnlInstructions.Controls.Add(pnlWarning);
            pnlInstructions.Controls.Add(lblAlign);
            pnlInstructions.Controls.Add(pnlGood);
            pnlInstructions.Controls.Add(pnlBad);
            pnlInstructions.Controls.Add(lblConsent);
            pnlInstructions.Controls.Add(btnStart);

            // ================= RIGHT: CAMERA / SCANNER PANEL =================
            pnlCamera = new Panel { Dock = DockStyle.Fill, Visible = false, BackColor = Color.FromArgb(12, 8, 10) };

            lblCamTitle = new Label
            {
                Text = "F A C E   V E R I F I C A T I O N",
                ForeColor = ClrGold,
                Font = new Font("Consolas", 13, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(contentWidth - 40, 26),
                Location = new Point(20, 30)
            };

            var lblSubTitle = new Label
            {
                Text = "AI BIOMETRIC SCANNER — CDSGA",
                ForeColor = Color.FromArgb(190, 160, 150),
                Font = new Font("Consolas", 8.5F, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(contentWidth - 40, 18),
                Location = new Point(20, 56)
            };

            int scannerW = Math.Min(560, contentWidth - 80);
            int scannerH = 520;
            scannerView = new ScannerView
            {
                Location = new Point((contentWidth - scannerW) / 2, 90),
                Size = new Size(scannerW, scannerH)
            };

            pnlCamera.Controls.Add(scannerView);
            pnlCamera.Controls.Add(lblCamTitle);
            pnlCamera.Controls.Add(lblSubTitle);

            // Dock order: Fill panels muna, tapos Left sidebar sa huli
            this.Controls.Add(pnlCamera);
            this.Controls.Add(pnlInstructions);
            this.Controls.Add(pnlBrand);

            countdownTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            countdownTimer.Tick += CountdownTimer_Tick;
        }

        private void PnlBrand_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Subtle gold accent stripe sa gilid
            using (var goldPen = new Pen(ClrGold, 3f))
                g.DrawLine(goldPen, pnlBrand.Width - 2, 0, pnlBrand.Width - 2, pnlBrand.Height);

            // Maroon gradient para may depth
            using (var brush = new LinearGradientBrush(
                new Rectangle(0, 0, pnlBrand.Width, pnlBrand.Height),
                ClrMaroonDark, ClrMaroon, 90f))
            {
                g.FillRectangle(brush, 0, 0, pnlBrand.Width - 4, pnlBrand.Height);
            }
        }

        // ================= CAMERA LOGIC =================
        private void BtnStart_Click(object sender, EventArgs e)
        {
            if (faceHelper == null)
            {
                MessageBox.Show("Hindi available ang face recognition. Suriin ang Models folder.",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

            if (videoDevices.Count == 0)
            {
                MessageBox.Show("Walang nakitang camera. Siguraduhing naka-connect yung webcam/phone camera mo.",
                    "Camera Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            videoSource = new VideoCaptureDevice(videoDevices[0].MonikerString);
            videoSource.NewFrame += VideoSource_NewFrame;
            videoSource.Start();

            pnlInstructions.Visible = false;
            pnlCamera.Visible = true;

            countdown = 3;
            scannerView.AccentColor = ClrGold;
            scannerView.ScanningActive = true;
            scannerView.ShowSuccess = false;
            scannerView.ShowError = false;
            scannerView.StatusText = "HOLD STILL • " + countdown;
        }

        private void VideoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            try
            {
                Bitmap frame = (Bitmap)eventArgs.Frame.Clone();
                if (scannerView.InvokeRequired)
                {
                    scannerView.Invoke(new Action(() =>
                    {
                        var old = scannerView.CameraFrame;
                        scannerView.CameraFrame = frame;
                        old?.Dispose();
                    }));
                }
            }
            catch { }
        }

        private void CountdownTimer_Tick(object sender, EventArgs e)
        {
            countdown--;
            if (countdown > 0)
            {
                scannerView.StatusText = "HOLD STILL • " + countdown;
            }
            else
            {
                countdownTimer.Stop();
                RunFaceVerification();
            }
        }

        // ================= FACE VERIFICATION (FaceONNX) =================
        private void RunFaceVerification()
        {
            scannerView.StatusText = "ANALYZING BIOMETRIC DATA...";

            Bitmap currentFrame = scannerView.CameraFrame != null ? (Bitmap)scannerView.CameraFrame.Clone() : null;

            if (currentFrame == null || referencePhoto == null)
            {
                ShowFailure("NO IMAGE CAPTURED");
                return;
            }

            try
            {
                float[] liveEmbedding = faceHelper.GetEmbedding(currentFrame);
                if (liveEmbedding == null)
                {
                    ShowFailure("NO FACE DETECTED");
                    return;
                }

                float[] refEmbedding = faceHelper.GetEmbedding(referencePhoto);
                if (refEmbedding == null)
                {
                    ShowFailure("REFERENCE PHOTO ERROR");
                    return;
                }

                float similarity = faceHelper.CompareFaces(liveEmbedding, refEmbedding);

                if (similarity >= 0.6f)
                {
                    scannerView.ScanningActive = false;
                    scannerView.ShowSuccess = true;
                    scannerView.AccentColor = ClrSuccessGlow;
                    scannerView.StatusText = "IDENTITY CONFIRMED ✔";
                    VerificationPassed = true;

                    var closeTimer = new System.Windows.Forms.Timer { Interval = 1200 };
                    closeTimer.Tick += (s, ev) =>
                    {
                        closeTimer.Stop();
                        StopCamera();
                        this.DialogResult = DialogResult.OK;
                        this.Close();
                    };
                    closeTimer.Start();
                }
                else
                {
                    ShowFailure("FACE MISMATCH");
                }
            }
            catch (Exception ex)
            {
                ShowFailure("SCAN ERROR: " + ex.Message.ToUpper());
            }
        }

        private void ShowFailure(string message)
        {
            scannerView.ScanningActive = false;
            scannerView.ShowError = true;
            scannerView.AccentColor = ClrErrorGlow;
            scannerView.StatusText = message;
            countdown = 3;

            var retryTimer = new System.Windows.Forms.Timer { Interval = 2200 };
            retryTimer.Tick += (s, ev) =>
            {
                retryTimer.Stop();
                scannerView.ShowError = false;
                scannerView.AccentColor = ClrGold;
                scannerView.ScanningActive = true;
                scannerView.StatusText = "HOLD STILL • " + countdown;
                countdownTimer.Start();
            };
            retryTimer.Start();
        }

        private void StopCamera()
        {
            if (videoSource != null && videoSource.IsRunning)
            {
                videoSource.SignalToStop();
                videoSource.WaitForStop();
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            StopCamera();
            faceHelper?.Dispose();
            base.OnFormClosing(e);
        }
    }

    // ================= CUSTOM CONTROLS =================

    public class RoundedPanel : Panel
    {
        private int radius;
        public RoundedPanel(int cornerRadius)
        {
            radius = cornerRadius;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            using (var path = GetRoundedRect(this.ClientRectangle, radius))
                this.Region = new Region(path);
        }

        private GraphicsPath GetRoundedRect(Rectangle r, int rad)
        {
            var path = new GraphicsPath();
            int d = rad * 2;
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    public class RoundedButton : Button
    {
        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public Color HoverColor { get; set; } = Color.FromArgb(29, 78, 216);
        private Color originalColor;
        private int radius = 12;

        public RoundedButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            Cursor = Cursors.Hand;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            originalColor = BackColor;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            BackColor = HoverColor;
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            BackColor = originalColor;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            using (var path = RoundedRect(ClientRectangle, radius))
                Region = new Region(path);
        }

        private GraphicsPath RoundedRect(Rectangle r, int rad)
        {
            var path = new GraphicsPath();
            int d = rad * 2;
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    public class FaceGuideCard : RoundedPanel
    {
        private bool isGood;

        public FaceGuideCard(bool good) : base(16)
        {
            isGood = good;
            BackColor = Color.FromArgb(248, 245, 240);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int cx = Width / 2;
            int ovalW = 70, ovalH = 84;
            int ovalX = cx - ovalW / 2;
            int ovalY = 12;

            using (var bg = new SolidBrush(Color.FromArgb(230, 225, 220)))
                g.FillEllipse(bg, ovalX, ovalY, ovalW, ovalH);

            Color lineColor = isGood ? Color.FromArgb(22, 163, 74) : Color.FromArgb(200, 200, 205);
            using (var pen = new Pen(lineColor, 2.5f))
                g.DrawEllipse(pen, ovalX + 8, ovalY + 6, ovalW - 16, ovalH - 20);

            using (var faceLine = new Pen(Color.FromArgb(160, 160, 165), 2f))
            {
                g.DrawLine(faceLine, cx - 12, ovalY + 32, cx - 6, ovalY + 32);
                g.DrawLine(faceLine, cx + 6, ovalY + 32, cx + 12, ovalY + 32);
                g.DrawArc(faceLine, cx - 10, ovalY + 44, 20, 12, 0, 180);
            }

            int badgeSize = 22;
            int badgeX = cx + ovalW / 2 - badgeSize - 6;
            int badgeY = ovalY + ovalH - badgeSize - 2;
            Color badgeColor = isGood ? Color.FromArgb(22, 163, 74) : Color.FromArgb(220, 38, 38);
            using (var badgeBrush = new SolidBrush(badgeColor))
                g.FillEllipse(badgeBrush, badgeX, badgeY, badgeSize, badgeSize);
            using (var whitePen = new Pen(Color.White, 2.2f))
            {
                if (isGood)
                {
                    g.DrawLine(whitePen, badgeX + 5, badgeY + 11, badgeX + 9, badgeY + 15);
                    g.DrawLine(whitePen, badgeX + 9, badgeY + 15, badgeX + 17, badgeY + 6);
                }
                else
                {
                    g.DrawLine(whitePen, badgeX + 6, badgeY + 6, badgeX + 16, badgeY + 16);
                    g.DrawLine(whitePen, badgeX + 16, badgeY + 6, badgeX + 6, badgeY + 16);
                }
            }

            string caption = isGood ? "Good Fit" : "Too Far";
            Color capColor = isGood ? Color.FromArgb(22, 163, 74) : Color.FromArgb(140, 140, 145);
            using (var font = new Font("Segoe UI Semibold", 10, FontStyle.Bold))
            using (var brush = new SolidBrush(capColor))
            {
                var size = g.MeasureString(caption, font);
                g.DrawString(caption, font, brush, cx - size.Width / 2, ovalY + ovalH + 14);
            }
        }
    }

    public class ChecklistItem : Panel
    {
        private bool positive;
        private string text;

        public ChecklistItem(string labelText, bool isPositive)
        {
            text = labelText;
            positive = isPositive;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            BackColor = Color.Transparent;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int badgeSize = 22;
            int badgeY = (Height - badgeSize) / 2;
            Color color = positive ? Color.FromArgb(22, 163, 74) : Color.FromArgb(220, 38, 38);

            using (var brush = new SolidBrush(color))
                g.FillEllipse(brush, 0, badgeY, badgeSize, badgeSize);

            using (var pen = new Pen(Color.White, 2f))
            {
                if (positive)
                {
                    g.DrawLine(pen, 5, badgeY + 12, 9, badgeY + 16);
                    g.DrawLine(pen, 9, badgeY + 16, 17, badgeY + 6);
                }
                else
                {
                    g.DrawLine(pen, 6, badgeY + 6, 16, badgeY + 16);
                    g.DrawLine(pen, 16, badgeY + 6, 6, badgeY + 16);
                }
            }

            using (var font = new Font("Segoe UI", 9.5F))
            using (var brush = new SolidBrush(Color.FromArgb(50, 45, 45)))
            {
                var rect = new RectangleF(badgeSize + 10, 0, Width - badgeSize - 10, Height);
                var sf = new StringFormat { LineAlignment = StringAlignment.Center };
                g.DrawString(text, font, brush, rect, sf);
            }
        }
    }

    // ================= SCI-FI SCANNER VIEW (Maroon & Gold) =================
    public class ScannerView : Control
    {
        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public Image CameraFrame { get; set; }

        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public string StatusText { get; set; } = "";

        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public Color AccentColor { get; set; } = Color.FromArgb(212, 175, 55);

        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public bool ScanningActive { get; set; } = false;

        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public bool ShowSuccess { get; set; } = false;

        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public bool ShowError { get; set; } = false;

        private float scanOffset = 0f;
        private float rotationAngle = 0f;
        private float pulsePhase = 0f;
        private System.Windows.Forms.Timer animTimer;
        private int radius = 24;

        private static readonly Color ClrMaroonAccent = Color.FromArgb(140, 26, 50);

        public ScannerView()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                      ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Color.Black;

            animTimer = new System.Windows.Forms.Timer { Interval = 30 };
            animTimer.Tick += (s, e) =>
            {
                scanOffset += 5f;
                rotationAngle += 2.5f;
                if (rotationAngle > 360) rotationAngle -= 360;
                pulsePhase += 0.06f;
                if (pulsePhase > Math.PI * 2) pulsePhase -= (float)(Math.PI * 2);
                Invalidate();
            };
            animTimer.Start();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            using (var path = RoundedRectPath(new RectangleF(0, 0, Width, Height), radius))
                this.Region = new Region(path);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.FromArgb(10, 6, 8));

            if (CameraFrame != null)
                DrawImageCover(g, CameraFrame, ClientRectangle);
            else
                DrawGridBackground(g);

            int cx = Width / 2;
            int cy = Height / 2 - 6;
            int ovalW = (int)(Width * 0.62);
            int ovalH = (int)(Height * 0.6);
            var faceRect = new Rectangle(cx - ovalW / 2, cy - ovalH / 2, ovalW, ovalH);

            using (var vignettePath = new GraphicsPath())
            {
                vignettePath.AddEllipse(faceRect);
                var region = new Region(new Rectangle(0, 0, Width, Height));
                region.Exclude(vignettePath);
                using (var overlayBrush = new SolidBrush(Color.FromArgb(155, 10, 5, 8)))
                    g.FillRegion(overlayBrush, region);
                region.Dispose();
            }

            Color accent = ShowSuccess ? Color.FromArgb(0, 200, 100)
                          : ShowError ? Color.FromArgb(230, 55, 75)
                          : AccentColor;

            float pulse = (float)(Math.Sin(pulsePhase) * 0.5 + 0.5);
            int glowExpand = (int)(6 + pulse * 6);
            using (var glowPen = new Pen(Color.FromArgb((int)(60 + pulse * 60), accent), 10f))
                g.DrawEllipse(glowPen, Rectangle.Inflate(faceRect, glowExpand, glowExpand));

            using (var glowPen2 = new Pen(Color.FromArgb(90, accent), 6f))
                g.DrawEllipse(glowPen2, faceRect);
            using (var pen = new Pen(accent, 2.2f))
                g.DrawEllipse(pen, faceRect);

            int bx = faceRect.X - 16, by = faceRect.Y - 16;
            int bw = faceRect.Width + 32, bh = faceRect.Height + 32;
            int cl = 28;
            using (var cPen = new Pen(ClrMaroonAccent, 3f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
            {
                g.DrawLine(cPen, bx, by, bx + cl, by);
                g.DrawLine(cPen, bx, by, bx, by + cl);
                g.DrawLine(cPen, bx + bw, by, bx + bw - cl, by);
                g.DrawLine(cPen, bx + bw, by, bx + bw, by + cl);
                g.DrawLine(cPen, bx, by + bh, bx + cl, by + bh);
                g.DrawLine(cPen, bx, by + bh, bx, by + bh - cl);
                g.DrawLine(cPen, bx + bw, by + bh, bx + bw - cl, by + bh);
                g.DrawLine(cPen, bx + bw, by + bh, bx + bw, by + bh - cl);
            }

            if (ScanningActive)
            {
                using (var arcPen = new Pen(Color.FromArgb(210, accent), 3f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                {
                    g.DrawArc(arcPen, bx - 8, by - 8, bw + 16, bh + 16, rotationAngle, 50);
                    g.DrawArc(arcPen, bx - 8, by - 8, bw + 16, bh + 16, rotationAngle + 180, 50);
                }

                using (var clipPath = new GraphicsPath())
                {
                    clipPath.AddEllipse(faceRect);
                    var oldClip = g.Clip;
                    g.SetClip(clipPath);

                    float lineY = faceRect.Y + (scanOffset % faceRect.Height);

                    using (var band = new LinearGradientBrush(
                        new RectangleF(faceRect.X, lineY - 18, faceRect.Width, 36),
                        Color.FromArgb(0, accent), Color.FromArgb(0, accent), 90f))
                    {
                        var blend = new ColorBlend(3)
                        {
                            Colors = new[] { Color.FromArgb(0, accent), Color.FromArgb(120, accent), Color.FromArgb(0, accent) },
                            Positions = new[] { 0f, 0.5f, 1f }
                        };
                        band.InterpolationColors = blend;
                        g.FillRectangle(band, faceRect.X, lineY - 18, faceRect.Width, 36);
                    }

                    using (var corePen = new Pen(Color.FromArgb(230, accent), 2f))
                        g.DrawLine(corePen, faceRect.X, lineY, faceRect.Right, lineY);

                    g.Clip = oldClip;
                }
            }

            if (ShowSuccess)
            {
                using (var pen = new Pen(accent, 6f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                {
                    g.DrawLine(pen, cx - 24, cy, cx - 6, cy + 18);
                    g.DrawLine(pen, cx - 6, cy + 18, cx + 28, cy - 20);
                }
            }
            if (ShowError)
            {
                using (var pen = new Pen(accent, 6f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                {
                    g.DrawLine(pen, cx - 18, cy - 18, cx + 18, cy + 18);
                    g.DrawLine(pen, cx + 18, cy - 18, cx - 18, cy + 18);
                }
            }

            using (var dotBrush = new SolidBrush(Color.FromArgb(70, accent)))
            {
                for (int i = 6; i < Width - 6; i += 22)
                    g.FillEllipse(dotBrush, i, 6, 2, 2);
            }

            if (!string.IsNullOrEmpty(StatusText))
            {
                using (var font = new Font("Consolas", 10.5f, FontStyle.Bold))
                {
                    var size = g.MeasureString(StatusText, font);
                    var textRect = new RectangleF(cx - size.Width / 2 - 16, faceRect.Bottom + 26, size.Width + 32, size.Height + 12);
                    var bgPath = RoundedRectPath(textRect, 9);

                    using (var bgBrush = new SolidBrush(Color.FromArgb(190, 20, 10, 14)))
                        g.FillPath(bgBrush, bgPath);

                    using (var borderPen = new Pen(Color.FromArgb(120, accent), 1.2f))
                        g.DrawPath(borderPen, bgPath);

                    using (var textBrush = new SolidBrush(accent))
                        g.DrawString(StatusText, font, textBrush, textRect.X + 16, textRect.Y + 6);

                    bgPath.Dispose();
                }
            }
        }

        private void DrawGridBackground(Graphics g)
        {
            using (var pen = new Pen(Color.FromArgb(25, 140, 26, 50), 1f))
            {
                for (int gx = 0; gx < Width; gx += 20)
                    g.DrawLine(pen, gx, 0, gx, Height);
                for (int gy = 0; gy < Height; gy += 20)
                    g.DrawLine(pen, 0, gy, Width, gy);
            }
        }

        private void DrawImageCover(Graphics g, Image img, Rectangle dest)
        {
            float srcRatio = (float)img.Width / img.Height;
            float destRatio = (float)dest.Width / dest.Height;
            Rectangle srcRect;
            if (srcRatio > destRatio)
            {
                int newWidth = (int)(img.Height * destRatio);
                int x = (img.Width - newWidth) / 2;
                srcRect = new Rectangle(x, 0, newWidth, img.Height);
            }
            else
            {
                int newHeight = (int)(img.Width / destRatio);
                int yOff = (img.Height - newHeight) / 2;
                srcRect = new Rectangle(0, yOff, img.Width, newHeight);
            }
            g.DrawImage(img, dest, srcRect, GraphicsUnit.Pixel);
        }

        private GraphicsPath RoundedRectPath(RectangleF r, int rad)
        {
            var path = new GraphicsPath();
            float d = rad * 2;
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                animTimer?.Stop();
            base.Dispose(disposing);
        }
    }
}