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

        // ---- NEW: colors for the reference-image style design ----
        private static readonly Color ClrCardBg = Color.FromArgb(228, 228, 230);      // light gray container card
        private static readonly Color ClrReminderBg = Color.FromArgb(214, 208, 122);  // olive "Important Reminder"
        private static readonly Color ClrReminderText = Color.FromArgb(70, 60, 10);
        private static readonly Color ClrTealBg = Color.FromArgb(150, 214, 200);      // teal privacy banner
        private static readonly Color ClrTealText = Color.FromArgb(30, 70, 60);
        private static readonly Color ClrGoodText = Color.FromArgb(20, 140, 70);
        private static readonly Color ClrBadText = Color.FromArgb(200, 30, 40);

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
            this.ClientSize = new Size(1040, 820);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = ClrCream;
            this.Font = new Font("Segoe UI", 9F);

            int contentWidth = this.ClientSize.Width;

            // ================= RIGHT: INSTRUCTIONS PANEL =================
            pnlInstructions = new Panel { Dock = DockStyle.Fill, AutoScroll = false, BackColor = ClrCream };

            int pad = 40;
            int innerWidth = contentWidth - pad * 2;
            int y = 30;

            // ---- Title with camera icon ----
            var lblTitleIcon = new Label
            {
                Text = "📷",
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = ClrMaroon,
                AutoSize = true,
                Location = new Point(pad, y)
            };
            var lblTitle = new Label
            {
                Text = "Take Live Selfie",
                Font = new Font("Segoe UI Semibold", 22, FontStyle.Bold),
                ForeColor = ClrMaroon,
                AutoSize = true,
                Location = new Point(pad + 46, y)
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

            // ---- "Important Reminder" banner (olive style, gaya ng reference) ----
            var pnlWarning = new RoundedPanel(14)
            {
                Location = new Point(pad, y),
                Size = new Size(innerWidth, 78),
                BackColor = ClrReminderBg
            };
            var lblWarnIcon = new Label
            {
                Text = "⚠",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = ClrReminderText,
                AutoSize = true,
                Location = new Point(18, 22)
            };
            var lblWarnTitle = new Label
            {
                Text = "Important Reminder",
                Font = new Font("Segoe UI Semibold", 12, FontStyle.Bold),
                ForeColor = ClrReminderText,
                AutoSize = true,
                Location = new Point(56, 12)
            };
            var lblWarnDesc = new Label
            {
                Text = "Make sure you are in a well-lit area and look directly at the camera.\nAvoid using filters, hats, sunglasses, or anything that may cover your face.",
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = ClrReminderText,
                Size = new Size(innerWidth - 76, 46),
                Location = new Point(56, 34)
            };
            pnlWarning.Controls.Add(lblWarnIcon);
            pnlWarning.Controls.Add(lblWarnTitle);
            pnlWarning.Controls.Add(lblWarnDesc);
            y += 96;

            // ---- Light-gray container card: align title + good/bad avatar cards ----
            int cardContainerHeight = 40 + 210;
            var pnlGuideContainer = new RoundedPanel(16)
            {
                Location = new Point(pad, y),
                Size = new Size(innerWidth, cardContainerHeight),
                BackColor = ClrCardBg
            };

            var lblAlign = new Label
            {
                Text = "Align your face and follow the requirements below",
                Font = new Font("Segoe UI Semibold", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 20, 20),
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(innerWidth, 36),
                Location = new Point(0, 24)
            };

            int cardW = (innerWidth - 20 - 40) / 2;
            var pnlGood = new FaceGuideCard(true)
            {
                Location = new Point(20, 74),
                Size = new Size(cardW, 190)
            };
            var pnlBad = new FaceGuideCard(false)
            {
                Location = new Point(20 + cardW + 20, 74),
                Size = new Size(cardW, 190)
            };

            pnlGuideContainer.Controls.Add(lblAlign);
            pnlGuideContainer.Controls.Add(pnlGood);
            pnlGuideContainer.Controls.Add(pnlBad);

            y += cardContainerHeight + 20;

            // ---- Checklist items (2x2 grid, title + subtitle, gaya ng reference) ----
            string[,] items = new string[,]
            {
                { "Your face is clearly visible", "Make sure your entire face is in the frame.", "true" },
                { "No Cap / Hat / Head Covering", "Your hair and face must be fully visible.", "false" },
                { "Good lighting (not too dark or too bright)", "Your face should be evenly lit.", "true" },
                { "No Glasses (or any item that covers your eyes)", "Remove sunglasses, prescription glasses, or face masks.", "false" }
            };

            int chCardW = (innerWidth - 20) / 2;
            for (int i = 0; i < 4; i++)
            {
                int col = i % 2;
                int row = i / 2;
                bool positive = items[i, 2] == "true";
                var item = new ChecklistItem(items[i, 0], items[i, 1], positive)
                {
                    Location = new Point(pad + col * (chCardW + 20), y + row * 62),
                    Size = new Size(chCardW, 56)
                };
                pnlInstructions.Controls.Add(item);
            }
            y += 62 * 2 + 24;

            // ---- Teal privacy banner ----
            var pnlPrivacy = new RoundedPanel(28)
            {
                Location = new Point(pad, y),
                Size = new Size(innerWidth, 56),
                BackColor = ClrTealBg
            };
            var lblPrivacyIcon = new Label
            {
                Text = "ⓘ",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 90, 80),
                AutoSize = true,
                Location = new Point(20, 16)
            };
            var lblPrivacy = new Label
            {
                Text = "Your selfie will be used for verification purposes only and will be securely stored in accordance with our data privacy policy",
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = ClrTealText,
                Size = new Size(innerWidth - 60, 40),
                Location = new Point(48, 8),
                TextAlign = ContentAlignment.MiddleLeft
            };
            pnlPrivacy.Controls.Add(lblPrivacyIcon);
            pnlPrivacy.Controls.Add(lblPrivacy);
            y += 76;

            // ---- Start button (black, gaya ng reference) ----
            btnStart = new RoundedButton
            {
                Text = "📷   Start Liveness",
                Size = new Size(innerWidth, 56),
                Location = new Point(pad, y),
                BackColor = Color.Black,
                HoverColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold)
            };
            btnStart.Click += BtnStart_Click;

            pnlInstructions.Controls.Add(lblTitleIcon);
            pnlInstructions.Controls.Add(lblTitle);
            pnlInstructions.Controls.Add(lblDesc);
            pnlInstructions.Controls.Add(pnlWarning);
            pnlInstructions.Controls.Add(pnlGuideContainer);
            pnlInstructions.Controls.Add(pnlPrivacy);
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

            // Dock order: Fill panels
            this.Controls.Add(pnlCamera);
            this.Controls.Add(pnlInstructions);

            countdownTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            countdownTimer.Tick += CountdownTimer_Tick;
        }

        private bool IsVirtualCamera(string cameraName)
        {
            string name = (cameraName ?? string.Empty).ToLowerInvariant();

            return name.Contains("iriun") ||
                   name.Contains("ivcam") ||
                   name.Contains("droidcam") ||
                   name.Contains("obs virtual") ||
                   name.Contains("obs camera") ||
                   name.Contains("virtual camera") ||
                   name.Contains("virtual webcam") ||
                   name.Contains("manycam") ||
                   name.Contains("xsplit") ||
                   name.Contains("snap camera") ||
                   name.Contains("splitcam") ||
                   name.Contains("epoccam");
        }

        private bool IsLikelyExternalWebcam(FilterInfo device)
        {
            if (device == null)
                return false;

            string name = (device.Name ?? string.Empty).ToLowerInvariant();
            string moniker = (device.MonikerString ?? string.Empty).ToLowerInvariant();

            if (IsVirtualCamera(name))
                return false;

            // Strong indicators of a separately connected physical webcam.
            bool externalName =
                name.Contains("usb") ||
                name.Contains("uvc") ||
                name.Contains("webcam") ||
                name.Contains("external") ||
                name.Contains("logitech") ||
                name.Contains("brio") ||
                name.Contains("c920") ||
                name.Contains("c922") ||
                name.Contains("c930") ||
                name.Contains("c925") ||
                name.Contains("streamcam") ||
                name.Contains("lifecam") ||
                name.Contains("hd pro webcam") ||
                name.Contains("full hd camera") ||
                name.Contains("1080p camera") ||
                name.Contains("720p camera");

            // DirectShow USB PnP monikers commonly contain usb#vid_...
            bool usbPnP =
                moniker.Contains("\\usb#") ||
                moniker.Contains("usb#vid_") ||
                moniker.Contains("usb\\vid_");

            return externalName || usbPnP;
        }

        private int FindExternalWebcamIndex()
        {
            if (videoDevices == null || videoDevices.Count == 0)
                return -1;

            // First choice: a camera whose name clearly identifies it as external.
            for (int i = 0; i < videoDevices.Count; i++)
            {
                if (IsLikelyExternalWebcam(videoDevices[i]))
                    return i;
            }

            return -1;
        }

        // ================= CAMERA LOGIC =================
        private void BtnStart_Click(object sender, EventArgs e)
        {
            if (faceHelper == null)
            {
                MessageBox.Show(
                    "Hindi available ang face recognition. Suriin ang Models folder.",
                    "Face Recognition Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            try
            {
                // Reload devices para makita ang external webcam na bagong sinaksak.
                videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

                int externalCameraIndex = FindExternalWebcamIndex();

                // IMPORTANT: Walang fallback sa ibang camera.
                // Iriun/virtual cameras at built-in cameras na walang external
                // indicator ay hindi gagamitin.
                if (externalCameraIndex < 0)
                {
                    MessageBox.Show(
                        "Walang compatible EXTERNAL WEBCAM na nakita.\n\n" +
                        "Siguraduhing:\n" +
                        "• Naka-connect ang physical USB webcam\n" +
                        "• Naka-enable ito sa Windows\n" +
                        "• Hindi ito Iriun o ibang virtual camera\n\n" +
                        "Hindi gagamitin ng system ang built-in laptop camera o virtual camera.",
                        "External Webcam Required",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                string selectedCameraName = videoDevices[externalCameraIndex].Name;

                DialogResult result = MessageBox.Show(
                    "EXTERNAL WEBCAM SELECTED\n\n" +
                    "Camera: " + selectedCameraName + "\n\n" +
                    "FACE VERIFICATION REQUIREMENTS\n\n" +
                    "• Remove your glasses\n" +
                    "• Remove your cap or hat\n" +
                    "• Make sure your full face is visible\n" +
                    "• Look directly at the camera\n" +
                    "• Make sure there is enough lighting\n\n" +
                    "Are you ready to continue?",
                    "Before You Continue",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result != DialogResult.Yes)
                    return;

                StopCamera();

                videoSource = new VideoCaptureDevice(
                    videoDevices[externalCameraIndex].MonikerString);

                videoSource.NewFrame += VideoSource_NewFrame;
                videoSource.Start();

                pnlInstructions.Visible = false;
                pnlCamera.Visible = true;

                countdown = 3;

                scannerView.AccentColor = ClrGold;
                scannerView.ScanningActive = true;
                scannerView.ShowSuccess = false;
                scannerView.ShowError = false;
                scannerView.StatusText =
                    "NO GLASSES • NO CAP • HOLD STILL • " + countdown;

                countdownTimer.Start();
            }
            catch (Exception ex)
            {
                StopCamera();

                MessageBox.Show(
                    "Hindi ma-start ang external webcam.\n\n" +
                    ex.Message,
                    "Camera Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
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
                scannerView.StatusText = "NO GLASSES • NO CAP • HOLD STILL • " + countdown;
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
                scannerView.StatusText = "NO GLASSES • NO CAP • HOLD STILL • " + countdown;
                countdownTimer.Start();
            };
            retryTimer.Start();
        }

        private void StopCamera()
        {
            if (videoSource != null)
            {
                try
                {
                    videoSource.NewFrame -= VideoSource_NewFrame;

                    if (videoSource.IsRunning)
                    {
                        videoSource.SignalToStop();
                        videoSource.WaitForStop();
                    }
                }
                catch { }
                finally
                {
                    videoSource = null;
                }
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

    // ---- FaceGuideCard: gumagamit na ng CartoonG / CartoonR resource images ----
    // Circular avatar + badge overlay (green ✓ sa Good, red ✕ sa Bad) + caption sa ilalim,
    // gaya ng "Good Fit" / "Too Far/ Not Centered" sa reference image.
    public class FaceGuideCard : Panel
    {
        private bool isGood;

        public FaceGuideCard(bool good)
        {
            isGood = good;
            BackColor = Color.Transparent;

            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer,
                true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode =
                System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

            // =========================
            // AVATAR IMAGE
            // =========================
            int avatarSize = 120;
            int avatarX = (Width - avatarSize) / 2;
            int avatarY = 6;

            Image avatarImg = null;

            try
            {
                avatarImg = isGood
                    ? Properties.Resources.CartoonG
                    : Properties.Resources.CartoonR;
            }
            catch
            {
                avatarImg = null;
            }

            using (var path = new GraphicsPath())
            {
                path.AddEllipse(
                    avatarX,
                    avatarY,
                    avatarSize,
                    avatarSize);

                GraphicsState state = g.Save();

                g.SetClip(path);

                // Background
                using (var bgBrush = new SolidBrush(
                    isGood
                        ? Color.FromArgb(220, 245, 230)
                        : Color.FromArgb(250, 225, 230)))
                {
                    g.FillEllipse(
                        bgBrush,
                        avatarX,
                        avatarY,
                        avatarSize,
                        avatarSize);
                }

                // Image
                if (avatarImg != null)
                {
                    g.DrawImage(
                        avatarImg,
                        new Rectangle(
                            avatarX,
                            avatarY,
                            avatarSize,
                            avatarSize));
                }

                g.Restore(state);
            }

            // White border around image
            using (var pen = new Pen(Color.White, 3))
            {
                g.DrawEllipse(
                    pen,
                    avatarX,
                    avatarY,
                    avatarSize,
                    avatarSize);
            }

            // =========================
            // CAPTION
            // =========================
            string caption = isGood
                ? "Good Fit"
                : "Too Far / Not Centered";

            Color captionColor = isGood
                ? Color.FromArgb(20, 140, 70)
                : Color.FromArgb(200, 30, 40);

            using (var font = new Font(
                "Segoe UI Semibold",
                12.5F,
                FontStyle.Bold))
            using (var brush = new SolidBrush(captionColor))
            {
                SizeF textSize =
                    g.MeasureString(caption, font);

                float textX =
                    (Width - textSize.Width) / 2F;

                float textY =
                    avatarY + avatarSize + 14;

                g.DrawString(
                    caption,
                    font,
                    brush,
                    textX,
                    textY);
            }
        }
    }

    // ---- ChecklistItem: badge + bold title + gray subtitle (2-line, gaya ng reference) ----
    public class ChecklistItem : Panel
    {
        private bool positive;
        private string title;
        private string subtitle;

        public ChecklistItem(string titleText, string subtitleText, bool isPositive)
        {
            title = titleText;
            subtitle = subtitleText;
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
            int badgeY = 2;
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

            int textX = badgeSize + 12;
            using (var titleFont = new Font("Segoe UI Semibold", 10, FontStyle.Bold))
            using (var titleBrush = new SolidBrush(color))
            {
                g.DrawString(title, titleFont, titleBrush, textX, 0);
            }

            using (var subFont = new Font("Segoe UI", 8.5F))
            using (var subBrush = new SolidBrush(color))
            {
                g.DrawString(subtitle, subFont, subBrush,
                    new RectangleF(textX, 20, Width - textX, Height - 20));
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
            int faceW = (int)(Width * 0.58);
            int faceH = (int)(Height * 0.62);
            var faceRect = new Rectangle(cx - faceW / 2, cy - faceH / 2, faceW, faceH);

            // Face-shaped guide: mas natural kaysa sa perfect oval/circle.
            using (var facePath = CreateFaceShape(faceRect))
            {
                var region = new Region(new Rectangle(0, 0, Width, Height));
                region.Exclude(facePath);
                using (var overlayBrush = new SolidBrush(Color.FromArgb(155, 10, 5, 8)))
                    g.FillRegion(overlayBrush, region);
                region.Dispose();
            }

            Color accent = ShowSuccess ? Color.FromArgb(0, 200, 100)
                          : ShowError ? Color.FromArgb(230, 55, 75)
                          : AccentColor;

            float pulse = (float)(Math.Sin(pulsePhase) * 0.5 + 0.5);
            int glowExpand = (int)(6 + pulse * 6);
            using (var glowPath = CreateFaceShape(Rectangle.Inflate(faceRect, glowExpand, glowExpand)))
            using (var glowPen = new Pen(Color.FromArgb((int)(60 + pulse * 60), accent), 10f))
                g.DrawPath(glowPen, glowPath);

            using (var glowPath2 = CreateFaceShape(faceRect))
            using (var glowPen2 = new Pen(Color.FromArgb(90, accent), 6f))
                g.DrawPath(glowPen2, glowPath2);

            using (var faceOutline = CreateFaceShape(faceRect))
            using (var pen = new Pen(accent, 2.2f))
                g.DrawPath(pen, faceOutline);

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

                using (var clipPath = CreateFaceShape(faceRect))
                {
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

        private GraphicsPath CreateFaceShape(Rectangle r)
        {
            var path = new GraphicsPath();

            float x = r.X;
            float y = r.Y;
            float w = r.Width;
            float h = r.Height;
            float cx = x + w / 2f;

            // Forehead -> temples -> cheeks -> jaw -> chin.
            path.AddBezier(
                cx, y,
                x + w * 0.73f, y,
                x + w * 0.94f, y + h * 0.17f,
                x + w * 0.92f, y + h * 0.36f);
            path.AddBezier(
                x + w * 0.92f, y + h * 0.36f,
                x + w * 0.91f, y + h * 0.58f,
                x + w * 0.80f, y + h * 0.76f,
                x + w * 0.65f, y + h * 0.86f);
            path.AddBezier(
                x + w * 0.65f, y + h * 0.86f,
                x + w * 0.59f, y + h * 0.91f,
                x + w * 0.56f, y + h * 0.98f,
                cx, y + h);
            path.AddBezier(
                cx, y + h,
                x + w * 0.44f, y + h * 0.98f,
                x + w * 0.41f, y + h * 0.91f,
                x + w * 0.35f, y + h * 0.86f);
            path.AddBezier(
                x + w * 0.35f, y + h * 0.86f,
                x + w * 0.20f, y + h * 0.76f,
                x + w * 0.09f, y + h * 0.58f,
                x + w * 0.08f, y + h * 0.36f);
            path.AddBezier(
                x + w * 0.08f, y + h * 0.36f,
                x + w * 0.06f, y + h * 0.17f,
                x + w * 0.27f, y,
                cx, y);
            path.CloseFigure();

            return path;
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