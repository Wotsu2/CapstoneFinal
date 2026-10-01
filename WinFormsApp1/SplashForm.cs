// ============================================================================
//  SplashForm.cs — Maroon Theme Edition
//  Professional modern splash screen — pure GDI+, no external dependencies.
// ============================================================================

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinFormsApp1
{
    public partial class SplashForm : Form
    {
        // ====================================================================
        //  MAROON PALETTE
        // ====================================================================
        private static readonly Color BG_TOP = Color.FromArgb(0x1A, 0x05, 0x05); // #1A0505
        private static readonly Color BG_BOTTOM = Color.FromArgb(0x2D, 0x0A, 0x0A); // #2D0A0A
        private static readonly Color GLOW_CENTER = Color.FromArgb(38, 180, 40, 40);
        private static readonly Color GLOW_OUTER = Color.FromArgb(0, 180, 40, 40);
        private static readonly Color BORDER_COLOR = Color.FromArgb(70, 122, 30, 30);
        private static readonly Color PARTICLE_CLR = Color.FromArgb(255, 155, 130);   // warm coral

        // Logo glow
        private static readonly Color LOGO_GLOW_IN = Color.FromArgb(120, 200, 60, 60);
        private static readonly Color LOGO_GLOW_OUT = Color.FromArgb(0, 200, 60, 60);

        // Progress bar gradient stops (maroon → crimson → bright red)
        private static readonly Color PROG_A1 = Color.FromArgb(90, 15, 15); // deep maroon
        private static readonly Color PROG_A2 = Color.FromArgb(140, 30, 30);
        private static readonly Color PROG_B1 = Color.FromArgb(165, 42, 42); // brown red
        private static readonly Color PROG_B2 = Color.FromArgb(200, 60, 50);
        private static readonly Color PROG_C1 = Color.FromArgb(192, 57, 43); // crimson
        private static readonly Color PROG_C2 = Color.FromArgb(231, 76, 60); // bright red

        // Text colors
        private static readonly Color TEXT_PRIMARY = Color.FromArgb(245, 230, 230);
        private static readonly Color TEXT_SECONDARY = Color.FromArgb(210, 165, 165);
        private static readonly Color TEXT_MUTED = Color.FromArgb(160, 110, 110);

        // ====================================================================
        //  CONSTANTS
        // ====================================================================
        private const int FORM_WIDTH = 520;
        private const int FORM_HEIGHT = 340;
        private const int CORNER_RADIUS = 22;
        private const int TIMER_INTERVAL_MS = 15;
        private const float TARGET_OPACITY = 0.95f;

        private const float FADE_IN_DURATION = 0.40f;
        private const float FADE_OUT_DURATION = 0.90f;
        private const float LOGO_SCALE_DURATION = 0.60f;
        private const float TEXT_SLIDE_DURATION = 0.50f;
        private const float PROGRESS_DURATION = 5.00f;
        private const float MESSAGE_INTERVAL = 0.50f;
        private const float POST_PROGRESS_DELAY = 0.60f;

        // ====================================================================
        //  STATE
        // ====================================================================
        private readonly System.Windows.Forms.Timer animationTimer;
        private readonly Stopwatch stopwatch;
        private readonly Random random;
        private readonly List<Particle> particles;

        private Image logoImage;

        private float elapsed;
        private float lastElapsed;
        private float deltaTime;

        private float fadeInProgress;
        private float exitProgress;
        private float exitStartTime;
        private float logoScaleProgress;
        private float textSlideProgress;
        private float currentProgress;
        private float shimmerPhase;
        private float nextMessageTime;

        private bool isExiting;
        private bool loginLaunched;

        private int animationFrame;
        private string loadingMessage;
        private int lastMessageIndex = -1;

        private readonly string[] loadingMessages = new[]
        {
            "Initializing...",
            "Loading modules...",
            "Connecting...",
            "Preparing UI...",
            "Almost there...",
            "Finalizing setup..."
        };

        // ====================================================================
        //  CONSTRUCTOR
        // ====================================================================
        public SplashForm()
        {
            InitializeComponent();

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(FORM_WIDTH, FORM_HEIGHT);
            BackColor = BG_TOP;
            ShowInTaskbar = false;
            KeyPreview = true;

            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);
            DoubleBuffered = true;

            UpdateFormRegion();

            try { logoImage = Properties.Resources.CCSLogo; }
            catch { logoImage = null; }

            random = new Random();
            stopwatch = new Stopwatch();
            animationTimer = new System.Windows.Forms.Timer { Interval = TIMER_INTERVAL_MS };
            animationTimer.Tick += AnimationTimer_Tick;
            particles = new List<Particle>();

            Opacity = 0.0;
            loadingMessage = loadingMessages[0];
        }

        // ====================================================================
        //  NATIVE DROP SHADOW
        // ====================================================================
        protected override CreateParams CreateParams
        {
            get
            {
                const int CS_DROPSHADOW = 0x00020000;
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= CS_DROPSHADOW;
                return cp;
            }
        }

        // ====================================================================
        //  ROUNDED REGION
        // ====================================================================
        private void UpdateFormRegion()
        {
            using (GraphicsPath path = DrawRoundedRect(ClientRectangle, CORNER_RADIUS))
            {
                Region old = Region;
                Region = new Region(path);
                old?.Dispose();
            }
        }

        // ====================================================================
        //  LIFECYCLE
        // ====================================================================
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            CreateParticles();
            StartAnimations();
            _ = SimulateAsyncLoadAsync();
        }

        private async Task SimulateAsyncLoadAsync()
        {
            await Task.Delay((int)(PROGRESS_DURATION * 1000));
        }

        // ====================================================================
        //  START ANIMATIONS
        // ====================================================================
        public void StartAnimations()
        {
            stopwatch.Restart();
            lastElapsed = 0f;
            elapsed = 0f;
            nextMessageTime = MESSAGE_INTERVAL;
            animationTimer.Start();
        }

        // ====================================================================
        //  MAIN TICK
        // ====================================================================
        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            float now = (float)stopwatch.Elapsed.TotalSeconds;
            deltaTime = Math.Max(0f, Math.Min(0.05f, now - lastElapsed));
            lastElapsed = now;
            elapsed = now;
            animationFrame++;

            AnimateProgress(deltaTime);
            UpdateParticles(deltaTime);
            Invalidate();
        }

        // ====================================================================
        //  ANIMATE PROGRESS
        // ====================================================================
        private void AnimateProgress(float dt)
        {
            if (fadeInProgress < 1f)
                fadeInProgress = Clamp(elapsed / FADE_IN_DURATION, 0f, 1f);

            if (logoScaleProgress < 1f)
                logoScaleProgress = Clamp(elapsed / LOGO_SCALE_DURATION, 0f, 1f);

            if (textSlideProgress < 1f)
                textSlideProgress = Clamp(elapsed / TEXT_SLIDE_DURATION, 0f, 1f);

            if (!isExiting)
            {
                float rawTarget = Clamp(elapsed / PROGRESS_DURATION, 0f, 1f);
                float eased = EaseOutCubic(rawTarget);

                currentProgress += (eased - currentProgress) * Math.Min(1f, dt * 12f);
                if (currentProgress > 0.999f) currentProgress = 1f;
            }

            shimmerPhase += dt * 0.8f;
            if (shimmerPhase > 1.6f) shimmerPhase = -0.3f;

            if (elapsed >= nextMessageTime && currentProgress < 0.97f)
            {
                nextMessageTime = elapsed + MESSAGE_INTERVAL;
                PickRandomMessage();
            }

            if (!isExiting &&
                currentProgress >= 1f &&
                elapsed > PROGRESS_DURATION + POST_PROGRESS_DELAY)
            {
                FadeOut();
            }

            if (isExiting)
            {
                exitProgress = Clamp((elapsed - exitStartTime) / FADE_OUT_DURATION, 0f, 1f);

                double targetOpacity = TARGET_OPACITY * (1.0 - exitProgress);
                Opacity = Math.Max(0.0, Math.Min(1.0, targetOpacity));

                if (exitProgress >= 1f && !loginLaunched)
                {
                    loginLaunched = true;
                    animationTimer.Stop();
                    stopwatch.Stop();
                    LaunchLoginForm();
                }
            }
            else
            {
                Opacity = Math.Max(0.0, Math.Min(1.0,
                    TARGET_OPACITY * EaseOutCubic(fadeInProgress)));
            }
        }

        // ====================================================================
        //  FADE OUT
        // ====================================================================
        public void FadeOut()
        {
            if (isExiting) return;
            isExiting = true;
            exitStartTime = elapsed;
        }

        // ====================================================================
        //  RANDOM MESSAGE
        // ====================================================================
        private void PickRandomMessage()
        {
            int idx;
            do { idx = random.Next(loadingMessages.Length); }
            while (idx == lastMessageIndex && loadingMessages.Length > 1);
            lastMessageIndex = idx;
            loadingMessage = loadingMessages[idx];
        }

        // ====================================================================
        //  LAUNCH LOGIN FORM — direct reference
        // ====================================================================
        private void LaunchLoginForm()
        {
            // Huwag mag-Show ng Login dito — ang Program.cs na ang bahala.
            Close();
        }

        // ====================================================================
        //  PARTICLES
        // ====================================================================
        private void CreateParticles()
        {
            particles.Clear();
            for (int i = 0; i < 42; i++)
            {
                particles.Add(new Particle
                {
                    X = (float)(random.NextDouble() * ClientSize.Width),
                    Y = (float)(random.NextDouble() * ClientSize.Height),
                    Radius = (float)(random.NextDouble() * 1.6 + 0.5),
                    SpeedX = (float)((random.NextDouble() - 0.5) * 6),
                    SpeedY = (float)(-6 - random.NextDouble() * 16),
                    Alpha = random.Next(20, 70),
                    Phase = (float)(random.NextDouble() * Math.PI * 2),
                    PhaseSpeed = (float)(random.NextDouble() * 1.4 + 0.4)
                });
            }
        }

        private void UpdateParticles(float dt)
        {
            for (int i = 0; i < particles.Count; i++)
            {
                Particle p = particles[i];
                p.X += p.SpeedX * dt;
                p.Y += p.SpeedY * dt;
                p.Phase += p.PhaseSpeed * dt;

                if (p.Y < -12f)
                {
                    p.Y = ClientSize.Height + 12f;
                    p.X = (float)(random.NextDouble() * ClientSize.Width);
                }
                if (p.X < -12f) p.X = ClientSize.Width + 12f;
                if (p.X > ClientSize.Width + 12f) p.X = -12f;
            }
        }

        private void DrawParticles(Graphics g)
        {
            foreach (Particle p in particles)
            {
                float pulse = 0.55f + 0.45f * (float)Math.Sin(p.Phase);
                int alpha = (int)(p.Alpha * pulse);
                if (alpha <= 0) continue;

                using (SolidBrush brush = new SolidBrush(
                    Color.FromArgb(alpha, PARTICLE_CLR)))
                {
                    g.FillEllipse(brush,
                        p.X - p.Radius, p.Y - p.Radius,
                        p.Radius * 2f, p.Radius * 2f);
                }
            }
        }

        // ====================================================================
        //  ONPAINT
        // ====================================================================
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            DrawGradientBackground(g);
            DrawParticles(g);

            Matrix saved = g.Transform.Clone();
            if (isExiting)
            {
                float scale = 1f - 0.05f * EaseOutCubic(exitProgress);
                g.TranslateTransform(ClientSize.Width / 2f, ClientSize.Height / 2f);
                g.ScaleTransform(scale, scale);
                g.TranslateTransform(-ClientSize.Width / 2f, -ClientSize.Height / 2f);
            }

            DrawLogo(g);
            DrawAppName(g);
            DrawVersion(g);
            DrawLoadingMessage(g);
            DrawGradientBar(g);
            DrawCopyright(g);

            g.Transform = saved;
            saved.Dispose();
        }

        // ====================================================================
        //  BACKGROUND — maroon gradient + atmospheric glow
        // ====================================================================
        private void DrawGradientBackground(Graphics g)
        {
            Rectangle rect = ClientRectangle;

            using (LinearGradientBrush bg = new LinearGradientBrush(
                rect, BG_TOP, BG_BOTTOM, 135f))
            {
                g.FillRectangle(bg, rect);
            }

            // Radial maroon highlight behind logo area
            using (GraphicsPath glow = new GraphicsPath())
            {
                int w = (int)(ClientSize.Width * 1.30f);
                int h = (int)(ClientSize.Height * 1.50f);
                glow.AddEllipse(
                    ClientSize.Width / 2 - w / 2,
                    (int)(ClientSize.Height * 0.38f) - h / 2,
                    w, h);

                using (PathGradientBrush pg = new PathGradientBrush(glow))
                {
                    pg.CenterColor = GLOW_CENTER;
                    pg.SurroundColors = new[] { GLOW_OUTER };
                    g.FillPath(pg, glow);
                }
            }

            // Subtle rounded border
            using (Pen border = new Pen(BORDER_COLOR, 1f))
            using (GraphicsPath path = DrawRoundedRect(
                new Rectangle(0, 0, ClientSize.Width - 1, ClientSize.Height - 1),
                CORNER_RADIUS))
            {
                g.DrawPath(border, path);
            }
        }

        // ====================================================================
        //  LOGO
        // ====================================================================
        private void DrawLogo(Graphics g)
        {
            float scale = 0.80f + 0.20f * EaseOutCubic(logoScaleProgress);
            const int baseSize = 96;
            int size = (int)(baseSize * scale);

            int cx = ClientSize.Width / 2;
            int cy = (int)(ClientSize.Height * 0.32f);

            Rectangle dest = new Rectangle(cx - size / 2, cy - size / 2, size, size);

            DrawGlow(g, cx, cy, size,
                0.85f + 0.15f * (float)Math.Sin(elapsed * 2.4f));

            if (logoImage != null)
            {
                float alpha = EaseOutCubic(fadeInProgress);
                using (ImageAttributes ia = new ImageAttributes())
                {
                    ColorMatrix cm = new ColorMatrix();
                    cm.Matrix33 = alpha;
                    ia.SetColorMatrix(cm);
                    g.DrawImage(logoImage, dest, 0, 0,
                        logoImage.Width, logoImage.Height,
                        GraphicsUnit.Pixel, ia);
                }
            }
            else
            {
                DrawFallbackLogo(g, dest, fadeInProgress);
            }
        }

        private void DrawGlow(Graphics g, int cx, int cy, int size, float intensity)
        {
            int glowSize = (int)(size * 1.9f);
            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddEllipse(cx - glowSize / 2, cy - glowSize / 2, glowSize, glowSize);
                using (PathGradientBrush pg = new PathGradientBrush(path))
                {
                    int a = (int)(120 * intensity);
                    pg.CenterColor = Color.FromArgb(a, LOGO_GLOW_IN.R, LOGO_GLOW_IN.G, LOGO_GLOW_IN.B);
                    pg.SurroundColors = new[] { LOGO_GLOW_OUT };
                    g.FillPath(pg, path);
                }
            }
        }

        private void DrawFallbackLogo(Graphics g, Rectangle rect, float alpha)
        {
            int a = (int)(255 * EaseOutCubic(alpha));

            using (GraphicsPath path = DrawRoundedRect(rect, rect.Width / 4))
            using (LinearGradientBrush brush = new LinearGradientBrush(
                rect,
                Color.FromArgb(a, 140, 30, 30),
                Color.FromArgb(a, 220, 80, 60),
                45f))
            {
                g.FillPath(brush, path);
            }

            using (Font f = new Font("Segoe UI", rect.Width * 0.55f,
                FontStyle.Bold, GraphicsUnit.Pixel))
            using (SolidBrush b = new SolidBrush(Color.FromArgb(a, 255, 240, 235)))
            using (StringFormat sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            })
            {
                g.DrawString("S", f, b, rect, sf);
            }
        }

        // ====================================================================
        //  APP NAME
        // ====================================================================
        private void DrawAppName(Graphics g)
        {
            float t = EaseOutCubic(textSlideProgress);
            float slide = (1f - t) * 20f;
            float alpha = t;

            using (Font f = new Font("Segoe UI Semibold", 24f,
                FontStyle.Bold, GraphicsUnit.Pixel))
            using (SolidBrush b = new SolidBrush(
                Color.FromArgb((int)(255 * alpha), TEXT_PRIMARY)))
            using (StringFormat sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            })
            {
                RectangleF rect = new RectangleF(
                    0,
                    ClientSize.Height * 0.55f + slide,
                    ClientSize.Width,
                    30);
                g.DrawString("CCS LABORATORY STYTEM", f, b, rect, sf);
            }
        }

        // ====================================================================
        //  VERSION
        // ====================================================================
        private void DrawVersion(Graphics g)
        {
            float t = EaseOutCubic(Clamp((elapsed - 0.10f) / TEXT_SLIDE_DURATION, 0f, 1f));
            float slide = (1f - t) * 20f;
            float alpha = t;

            using (Font f = new Font("Segoe UI", 11f, FontStyle.Regular, GraphicsUnit.Pixel))
            using (SolidBrush b = new SolidBrush(
                Color.FromArgb((int)(180 * alpha), TEXT_SECONDARY)))
            using (StringFormat sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            })
            {
                RectangleF rect = new RectangleF(
                    0,
                    ClientSize.Height * 0.55f + 32 + slide,
                    ClientSize.Width,
                    18);
                g.DrawString("Version 1.0.0", f, b, rect, sf);
            }
        }

        // ====================================================================
        //  LOADING MESSAGE
        // ====================================================================
        private void DrawLoadingMessage(Graphics g)
        {
            if (loadingMessage == null) return;

            float alpha = EaseOutCubic(fadeInProgress);

            using (Font f = new Font("Segoe UI", 11f, FontStyle.Regular, GraphicsUnit.Pixel))
            using (SolidBrush b = new SolidBrush(
                Color.FromArgb((int)(230 * alpha), TEXT_SECONDARY)))
            using (StringFormat sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            })
            {
                RectangleF rect = new RectangleF(
                    0,
                    ClientSize.Height * 0.74f,
                    ClientSize.Width,
                    18);
                string msg = currentProgress >= 1f ? "Ready!" : loadingMessage;
                g.DrawString(msg, f, b, rect, sf);
            }
        }

        // ====================================================================
        //  PROGRESS BAR — maroon → crimson → bright red
        // ====================================================================
        private void DrawGradientBar(Graphics g)
        {
            int barWidth = (int)(ClientSize.Width * 0.72f);
            int barHeight = 8;
            int x = (ClientSize.Width - barWidth) / 2;
            int y = (int)(ClientSize.Height * 0.82f);

            Rectangle trackRect = new Rectangle(x, y, barWidth, barHeight);

            using (GraphicsPath trackPath = DrawRoundedRect(trackRect, barHeight / 2))
            using (SolidBrush trackBrush = new SolidBrush(Color.FromArgb(70, 90, 30, 30)))
            {
                g.FillPath(trackBrush, trackPath);
            }

            int fillWidth = (int)(barWidth * currentProgress);
            if (fillWidth > 1)
            {
                Rectangle fillRect = new Rectangle(x, y, fillWidth, barHeight);
                using (GraphicsPath fillPath = DrawRoundedRect(fillRect, barHeight / 2))
                using (LinearGradientBrush fillBrush = CreateProgressGradient(fillRect))
                {
                    g.SetClip(fillPath);
                    g.FillRectangle(fillBrush, fillRect);
                    DrawShimmerBand(g, fillRect);
                    g.ResetClip();
                }
            }

            string pct = ((int)Math.Round(currentProgress * 100)).ToString() + "%";
            using (Font f = new Font("Segoe UI Semibold", 11f,
                FontStyle.Bold, GraphicsUnit.Pixel))
            using (SolidBrush b = new SolidBrush(
                Color.FromArgb(235, TEXT_PRIMARY)))
            {
                SizeF size = g.MeasureString(pct, f);
                g.DrawString(pct, f, b,
                    ClientSize.Width / 2f - size.Width / 2f,
                    y + barHeight + 7);
            }
        }

        // Maroon → crimson → bright red, driven by current progress.
        private LinearGradientBrush CreateProgressGradient(Rectangle rect)
        {
            float p = currentProgress;
            Color c1, c2;

            if (p < 0.5f)
            {
                float k = p / 0.5f;
                c1 = Lerp(PROG_A1, PROG_B1, k);
                c2 = Lerp(PROG_A2, PROG_B2, k);
            }
            else
            {
                float k = (p - 0.5f) / 0.5f;
                c1 = Lerp(PROG_B1, PROG_C1, k);
                c2 = Lerp(PROG_B2, PROG_C2, k);
            }

            return new LinearGradientBrush(rect, c1, c2, 0f);
        }

        private void DrawShimmerBand(Graphics g, Rectangle fillRect)
        {
            float centerX = fillRect.X + shimmerPhase * fillRect.Width * 1.4f
                          - fillRect.Width * 0.2f;
            const int shimmerWidth = 90;

            Rectangle band = new Rectangle(
                (int)(centerX - shimmerWidth / 2f),
                fillRect.Y - 2,
                shimmerWidth,
                fillRect.Height + 4);

            using (LinearGradientBrush bandBrush =
                new LinearGradientBrush(band,
                    Color.FromArgb(0, 255, 230, 220),
                    Color.FromArgb(0, 255, 230, 220),
                    0f))
            {
                ColorBlend blend = new ColorBlend(3)
                {
                    Colors = new[]
                    {
                        Color.FromArgb(  0, 255, 230, 220),
                        Color.FromArgb(180, 255, 230, 220),
                        Color.FromArgb(  0, 255, 230, 220)
                    },
                    Positions = new[] { 0f, 0.5f, 1f }
                };
                bandBrush.InterpolationColors = blend;
                g.FillRectangle(bandBrush, band);
            }
        }

        // ====================================================================
        //  COPYRIGHT
        // ====================================================================
        private void DrawCopyright(Graphics g)
        {
            float alpha = EaseOutCubic(fadeInProgress);
            const string text = "© 2026 CDSGA";

            using (Font f = new Font("Segoe UI", 9f, FontStyle.Regular, GraphicsUnit.Pixel))
            using (SolidBrush b = new SolidBrush(
                Color.FromArgb((int)(160 * alpha), TEXT_MUTED)))
            {
                SizeF size = g.MeasureString(text, f);
                g.DrawString(text, f, b,
                    20,
                    ClientSize.Height - size.Height - 14);
            }
        }

        // ====================================================================
        //  HELPERS
        // ====================================================================
        public static GraphicsPath DrawRoundedRect(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int d = radius * 2;
            if (d > rect.Width) d = rect.Width;
            if (d > rect.Height) d = rect.Height;

            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static float EaseOutCubic(float t)
        {
            t = Clamp(t, 0f, 1f);
            return 1f - (float)Math.Pow(1f - t, 3);
        }

        private static float Clamp(float v, float min, float max)
            => Math.Max(min, Math.Min(max, v));

        private static Color Lerp(Color a, Color b, float t)
        {
            t = Clamp(t, 0f, 1f);
            return Color.FromArgb(
                (int)(a.A + (b.A - a.A) * t),
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t));
        }

        // ====================================================================
        //  ESC TO SKIP
        // ====================================================================
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Escape)
            {
                animationTimer.Stop();
                stopwatch.Stop();
                Close();
            }
        }

        // ====================================================================
        //  CLEANUP
        // ====================================================================
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            animationTimer.Stop();
            animationTimer.Dispose();
            stopwatch.Stop();
        }

        // ====================================================================
        //  PARTICLE
        // ====================================================================
        private class Particle
        {
            public float X;
            public float Y;
            public float Radius;
            public float SpeedX;
            public float SpeedY;
            public int Alpha;
            public float Phase;
            public float PhaseSpeed;
        }
    }
}