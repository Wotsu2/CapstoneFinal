using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace WinFormsApp1
{
    public partial class SplashForm : Form
    {
        private readonly Stopwatch stopwatch = new Stopwatch();
        private readonly Random random = new Random();

        private readonly List<Particle> particles = new List<Particle>();

        private Image logoImage;

        private float elapsed;
        private float finalTransition;
        private float deltaSeconds;
        private float lastElapsed;

        private bool finalState;
        private bool loginOpened;

        // ============================================================
        // TIMELINE
        // ============================================================

        private const float INITIALIZING_END = 3.0f;

        private const float CORE_START = 3.0f;
        private const float CORE_END = 4.25f;

        private const float LOGO_START = 3.0f;
        private const float LOGO_END = 4.25f;

        private const float TITLE_START = 4.0f;

        private const float LOADING_START = 0.0f;
        private const float LOADING_DURATION = 8.0f;
        private const float READY_START = 8.0f;

        private const float FINAL_TRANSITION_START = 8.0f;
        private const float FINAL_TRANSITION_DURATION = 0.65f;
        private const float ACTIVE_DURATION = 5.0f;

        // ============================================================
        // CONSTRUCTOR
        // ============================================================

        public SplashForm()
        {
            InitializeComponent();

            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true
            );

            DoubleBuffered = true;

            try
            {
                logoImage = Properties.Resources.CCSLogo;
            }
            catch
            {
                logoImage = null;
            }

            CreateParticles();

            enterButton.Visible = false;
            enterButton.Enabled = false;

            stopwatch.Start();
            lastElapsed = 0f;
        }

        // ============================================================
        // FORM
        // ============================================================

        private void SplashForm_Load(object sender, EventArgs e)
        {
            if (enterButton != null)
                enterButton.Visible = false;

            animationTimer.Interval = 16;
            animationTimer.Start();
        }

        private void SplashForm_Resize(object sender, EventArgs e)
        {
            // Enter System button removed; no button positioning needed.
        }

        private void SplashForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            animationTimer.Stop();
            stopwatch.Stop();

            if (logoImage != null)
            {
                // Do not dispose resource image.
            }
        }

        // ============================================================
        // BUTTON
        // ============================================================

        private void CenterEnterButton()
        {
            if (enterButton == null)
                return;

            enterButton.Left =
                (ClientSize.Width - enterButton.Width) / 2;

            enterButton.Top =
                (int)(ClientSize.Height * 0.79f);
        }

        private void animationTimer_Tick(object sender, EventArgs e)
        {
            float now = (float)stopwatch.Elapsed.TotalSeconds;

            // Real frame time keeps movement consistent if a frame is delayed.
            deltaSeconds = Clamp(now - lastElapsed, 0f, 0.05f);
            lastElapsed = now;
            elapsed = now;

            if (elapsed >= FINAL_TRANSITION_START)
            {
                finalState = true;

                finalTransition =
                    Clamp(
                        (elapsed - FINAL_TRANSITION_START) /
                        FINAL_TRANSITION_DURATION,
                        0f,
                        1f
                    );

                // After the 8-second loading sequence, show SYSTEM ACTIVE
                // for 5 seconds, then automatically open Login.cs.
                if (!loginOpened &&
                    elapsed >= FINAL_TRANSITION_START + ACTIVE_DURATION)
                {
                    OpenLoginForm();
                }
            }

            UpdateParticles(deltaSeconds);
            Invalidate();
        }

        private void AnimateEnterButton()
        {
            if (!enterButton.Visible)
                return;

            float t =
                EaseOutCubic(
                    Clamp(
                        (finalTransition - 0.05f) / 0.95f,
                        0f,
                        1f
                    )
                );

            int centerX = ClientSize.Width / 2;

            int startY =
                (int)(ClientSize.Height * 0.87f);

            int finalY =
                (int)(ClientSize.Height * 0.79f);

            enterButton.Left =
                centerX - enterButton.Width / 2;

            enterButton.Top =
                (int)(
                    startY +
                    (finalY - startY) * t
                );

            int r =
                (int)(35 + 105 * t);

            int g =
                (int)(4 + 8 * t);

            int b =
                (int)(4 + 8 * t);

            enterButton.BackColor =
                Color.FromArgb(
                    r,
                    g,
                    b
                );

            int borderRed =
                (int)(120 + 115 * t);

            enterButton.FlatAppearance.BorderColor =
                Color.FromArgb(
                    borderRed,
                    35,
                    35
                );
        }

        private void enterButton_Click(object sender, EventArgs e)
        {
            animationTimer.Stop();
            stopwatch.Stop();

            DialogResult = DialogResult.OK;
            Close();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.KeyCode == Keys.Enter &&
                enterButton.Visible &&
                enterButton.Enabled)
            {
                enterButton.PerformClick();
            }
        }

        // ============================================================
        // PARTICLES
        // ============================================================

        private void CreateParticles()
        {
            particles.Clear();

            for (int i = 0; i < 75; i++)
            {
                particles.Add(
                    new Particle
                    {
                        X = random.Next(0, Math.Max(1, ClientSize.Width)),
                        Y = random.Next(0, Math.Max(1, ClientSize.Height)),
                        Speed = 4f + (float)random.NextDouble() * 12f,
                        Size = 1f + (float)random.NextDouble() * 2.2f,
                        Phase = (float)random.NextDouble() * 6.28f,
                        Alpha = 30 + random.Next(70)
                    }
                );
            }
        }

        private void UpdateParticles(float dt)
        {
            if (particles.Count == 0)
                return;

            foreach (Particle p in particles)
            {
                p.Y -= p.Speed * dt;

                if (p.Y < -10)
                {
                    p.Y = ClientSize.Height + 10;
                    p.X = random.Next(
                        0,
                        Math.Max(1, ClientSize.Width)
                    );
                }
            }
        }

        private void DrawParticles(Graphics g)
        {
            if (elapsed < 0.4f)
                return;

            float appear =
                Clamp(
                    (elapsed - 0.25f) / 1.5f,
                    0f,
                    1f
                );

            foreach (Particle p in particles)
            {
                float pulse =
                    0.55f +
                    0.45f *
                    (float)Math.Sin(
                        elapsed * 1.8f + p.Phase
                    );

                int alpha =
                    (int)(
                        p.Alpha *
                        pulse *
                        appear
                    );

                using SolidBrush brush =
                    new SolidBrush(
                        Color.FromArgb(
                            alpha,
                            255,
                            35,
                            35
                        )
                    );

                g.FillEllipse(
                    brush,
                    p.X,
                    p.Y,
                    p.Size,
                    p.Size
                );
            }
        }

        // ============================================================
        // PAINT
        // ============================================================

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g = e.Graphics;

            g.SmoothingMode =
                SmoothingMode.AntiAlias;

            g.InterpolationMode =
                InterpolationMode.HighQualityBicubic;

            g.PixelOffsetMode =
                PixelOffsetMode.HighQuality;

            g.CompositingQuality =
                CompositingQuality.HighQuality;

            DrawBackground(g);
            DrawParticles(g);

            if (!finalState)
            {
                DrawInitializing(g);
                DrawCore(g);
                DrawLogo(g);
                DrawTitle(g);
                DrawLoading(g);

                return;
            }

            DrawTransitionLayer(g);
            DrawFinalScreen(g);
        }

        // ============================================================
        // BACKGROUND
        // ============================================================

        private void DrawBackground(Graphics g)
        {
            using LinearGradientBrush background =
                new LinearGradientBrush(
                    ClientRectangle,
                    Color.Black,
                    Color.FromArgb(11, 0, 0),
                    90f
                );

            g.FillRectangle(
                background,
                ClientRectangle
            );

            int cx =
                ClientSize.Width / 2;

            int cy =
                ClientSize.Height / 2;

            // Main red atmospheric glow
            int glowWidth =
                (int)(
                    ClientSize.Width * 0.72f
                );

            int glowHeight =
                (int)(
                    ClientSize.Height * 0.95f
                );

            using GraphicsPath glowPath =
                new GraphicsPath();

            glowPath.AddEllipse(
                cx - glowWidth / 2,
                cy - glowHeight / 2,
                glowWidth,
                glowHeight
            );

            using PathGradientBrush glow =
                new PathGradientBrush(glowPath);

            glow.CenterColor =
                Color.FromArgb(
                    38,
                    120,
                    0,
                    0
                );

            glow.SurroundColors =
                new[]
                {
                    Color.FromArgb(
                        0,
                        0,
                        0,
                        0
                    )
                };

            g.FillPath(
                glow,
                glowPath
            );

            // Subtle top red light
            using LinearGradientBrush topGlow =
                new LinearGradientBrush(
                    new Rectangle(
                        0,
                        0,
                        ClientSize.Width,
                        260
                    ),
                    Color.FromArgb(
                        22,
                        150,
                        0,
                        0
                    ),
                    Color.FromArgb(
                        0,
                        150,
                        0,
                        0
                    ),
                    90f
                );

            g.FillRectangle(
                topGlow,
                0,
                0,
                ClientSize.Width,
                260
            );

            // Subtle bottom shadow
            using LinearGradientBrush bottom =
                new LinearGradientBrush(
                    new Rectangle(
                        0,
                        ClientSize.Height - 220,
                        ClientSize.Width,
                        220
                    ),
                    Color.FromArgb(
                        0,
                        0,
                        0,
                        0
                    ),
                    Color.FromArgb(
                        90,
                        0,
                        0,
                        0
                    ),
                    90f
                );

            g.FillRectangle(
                bottom,
                0,
                ClientSize.Height - 220,
                ClientSize.Width,
                220
            );
        }

        // ============================================================
        // INITIALIZING
        // ============================================================

        private void DrawInitializing(Graphics g)
        {
            if (elapsed >= INITIALIZING_END)
                return;

            float fadeIn =
                Clamp(
                    elapsed / 0.45f,
                    0f,
                    1f
                );

            float fadeOut =
                Clamp(
                    (INITIALIZING_END - elapsed) / 0.45f,
                    0f,
                    1f
                );

            float alpha =
                Math.Min(
                    fadeIn,
                    fadeOut
                );

            alpha =
                EaseInOutCubic(alpha);

            using Font font =
                new Font(
                    "Segoe UI",
                    19,
                    FontStyle.Bold,
                    GraphicsUnit.Pixel
                );

            using SolidBrush brush =
                new SolidBrush(
                    Color.FromArgb(
                        (int)(255 * alpha),
                        255,
                        65,
                        70
                    )
                );

            DrawCenteredString(
                g,
                "ANALYZING CDSGA HUB",
                font,
                brush,
                ClientSize.Width / 2,
                ClientSize.Height / 2 - 15
            );

            using Font small =
                new Font(
                    "Segoe UI",
                    10,
                    FontStyle.Regular,
                    GraphicsUnit.Pixel
                );

            using SolidBrush smallBrush =
                new SolidBrush(
                    Color.FromArgb(
                        (int)(130 * alpha),
                        220,
                        220,
                        220
                    )
                );

            DrawCenteredString(
                g,
                "Analyzing laboratory management environment",
                small,
                smallBrush,
                ClientSize.Width / 2,
                ClientSize.Height / 2 + 20
            );
        }

        // ============================================================
        // CENTRAL CORE
        // ============================================================

        private void DrawCore(Graphics g)
        {
            if (elapsed < CORE_START)
                return;

            float progress =
                EaseOutCubic(
                    Clamp(
                        (elapsed - CORE_START) /
                        (CORE_END - CORE_START),
                        0f,
                        1f
                    )
                );

            int cx = ClientSize.Width / 2;
            int cy = (int)(ClientSize.Height * 0.33f);

            float pulse =
                0.5f +
                0.5f * (float)Math.Sin(elapsed * 2.2f);

            // Soft red atmospheric glow behind the ring.
            int glowSize = (int)((205 + pulse * 18) * progress);
            if (glowSize > 2)
            {
                using GraphicsPath glowPath = new GraphicsPath();
                glowPath.AddEllipse(
                    cx - glowSize / 2,
                    cy - glowSize / 2,
                    glowSize,
                    glowSize
                );

                using PathGradientBrush glowBrush =
                    new PathGradientBrush(glowPath);

                glowBrush.CenterColor =
                    Color.FromArgb(
                        (int)(38 * progress),
                        255,
                        35,
                        25
                    );

                glowBrush.SurroundColors = new[]
                {
                    Color.FromArgb(0, 0, 0, 0)
                };

                g.FillPath(glowBrush, glowPath);
            }

            // Main circular HUD ring.
            float rotation = elapsed * 24f;

            int ringSize = (int)(158 * progress);
            int ringLeft = cx - ringSize / 2;
            int ringTop = cy - ringSize / 2;

            using Pen outerRing = new Pen(
                Color.FromArgb((int)(215 * progress), 255, 176, 45),
                2.2f
            );

            using Pen innerRing = new Pen(
                Color.FromArgb((int)(150 * progress), 214, 104, 25),
                1.2f
            );

            using Pen softRing = new Pen(
                Color.FromArgb((int)(90 * progress), 255, 75, 35),
                1f
            );

            g.DrawEllipse(outerRing, ringLeft, ringTop, ringSize, ringSize);
            g.DrawEllipse(
                innerRing,
                ringLeft + 10,
                ringTop + 10,
                ringSize - 20,
                ringSize - 20
            );
            g.DrawEllipse(
                softRing,
                ringLeft + 18,
                ringTop + 18,
                ringSize - 36,
                ringSize - 36
            );

            // Rotating HUD arcs.
            Rectangle arcRect = new Rectangle(
                ringLeft - 4,
                ringTop - 4,
                ringSize + 8,
                ringSize + 8
            );

            using Pen arcPen = new Pen(
                Color.FromArgb((int)(230 * progress), 255, 190, 55),
                3f
            );

            g.DrawArc(arcPen, arcRect, rotation, 55f);
            g.DrawArc(arcPen, arcRect, rotation + 180f, 55f);

            using Pen tickPen = new Pen(
                Color.FromArgb((int)(180 * progress), 245, 160, 40),
                1f
            );

            // Small technical ticks around the circle.
            for (int i = 0; i < 24; i++)
            {
                double a = (Math.PI * 2.0 * i / 24.0) + elapsed * 0.18;
                float outer = ringSize / 2f - 1f;
                float inner = outer - (i % 3 == 0 ? 9f : 5f);

                float x1 = cx + (float)Math.Cos(a) * inner;
                float y1 = cy + (float)Math.Sin(a) * inner;
                float x2 = cx + (float)Math.Cos(a) * outer;
                float y2 = cy + (float)Math.Sin(a) * outer;

                g.DrawLine(tickPen, x1, y1, x2, y2);
            }

            // Four bright locator points.
            using SolidBrush pointBrush = new SolidBrush(
                Color.FromArgb((int)(235 * progress), 255, 190, 60)
            );

            float pointRadius = ringSize / 2f;
            for (int i = 0; i < 4; i++)
            {
                double a = i * Math.PI / 2.0;
                float px = cx + (float)Math.Cos(a) * pointRadius;
                float py = cy + (float)Math.Sin(a) * pointRadius;

                g.FillEllipse(pointBrush, px - 3.5f, py - 3.5f, 7f, 7f);
            }
        }

        // ============================================================
        // LOGO
        // ============================================================

        private void DrawLogo(Graphics g)
        {
            if (logoImage == null || elapsed < LOGO_START)
                return;

            float progress =
                EaseOutBack(
                    Clamp(
                        (elapsed - LOGO_START) / 1.15f,
                        0f,
                        1f
                    )
                );

            float fade =
                EaseOutCubic(
                    Clamp(
                        (elapsed - LOGO_START) / 0.7f,
                        0f,
                        1f
                    )
                );

            int cx = ClientSize.Width / 2;
            int cy = (int)(ClientSize.Height * 0.33f);

            int maxSize =
                (int)(Math.Min(ClientSize.Width, ClientSize.Height) * 0.155f);

            int size = Math.Max(1, (int)(maxSize * progress));

            // Keep the logo safely inside the circular HUD.
            DrawLogoGlow(g, cx, cy, size, fade);

            Rectangle destination = new Rectangle(
                cx - size / 2,
                cy - size / 2,
                size,
                size
            );

            DrawImageWithOpacity(
                g,
                logoImage,
                destination,
                fade
            );
        }

        private void DrawLogoGlow(
            Graphics g,
            int cx,
            int cy,
            int size,
            float intensity)
        {
            int glowSize =
                (int)(
                    size * 1.15f
                );

            for (int i = 5; i >= 1; i--)
            {
                int current =
                    glowSize +
                    i * 9;

                int alpha =
                    (int)(
                        intensity *
                        18
                    );

                using SolidBrush brush =
                    new SolidBrush(
                        Color.FromArgb(
                            alpha,
                            255,
                            30,
                            20
                        )
                    );

                g.FillEllipse(
                    brush,
                    cx - current / 2,
                    cy - current / 2,
                    current,
                    current
                );
            }
        }

        private void DrawImageWithOpacity(
            Graphics g,
            Image image,
            Rectangle destination,
            float opacity)
        {
            using ImageAttributes attributes =
                new ImageAttributes();

            ColorMatrix matrix =
                new ColorMatrix();

            matrix.Matrix33 =
                Clamp(
                    opacity,
                    0f,
                    1f
                );

            attributes.SetColorMatrix(
                matrix,
                ColorMatrixFlag.Default,
                ColorAdjustType.Bitmap
            );

            g.DrawImage(
                image,
                destination,
                0,
                0,
                image.Width,
                image.Height,
                GraphicsUnit.Pixel,
                attributes
            );
        }

        // ============================================================
        // TITLE
        // ============================================================

        private void DrawTitle(Graphics g)
        {
            if (elapsed < TITLE_START)
                return;

            float fade =
                EaseOutCubic(
                    Clamp(
                        (elapsed - TITLE_START) /
                        0.8f,
                        0f,
                        1f
                    )
                );

            float slide =
                18f *
                (1f - fade);

            int cx =
                ClientSize.Width / 2;

            int y =
                (int)(
                    ClientSize.Height * 0.61f +
                    slide
                );

            using Font titleFont =
                new Font(
                    "Segoe UI Semibold",
                    38,
                    FontStyle.Bold,
                    GraphicsUnit.Pixel
                );

            using Font subtitleFont =
                new Font(
                    "Segoe UI",
                    13,
                    FontStyle.Regular,
                    GraphicsUnit.Pixel
                );

            using SolidBrush titleBrush =
                new SolidBrush(
                    Color.FromArgb(
                        (int)(255 * fade),
                        248,
                        248,
                        248
                    )
                );

            using SolidBrush subtitleBrush =
                new SolidBrush(
                    Color.FromArgb(
                        (int)(220 * fade),
                        220,
                        220,
                        220
                    )
                );

            DrawCenteredString(
                g,
                "CDSGA Hub",
                titleFont,
                titleBrush,
                cx,
                y
            );

            DrawCenteredString(
                g,
                "An Integrated Laboratory Management System",
                subtitleFont,
                subtitleBrush,
                cx,
                y + 55
            );

            DrawCenteredString(
                g,
                "for the College of Computer Studies (CCS)",
                subtitleFont,
                subtitleBrush,
                cx,
                y + 77
            );
        }

        // ============================================================
        // LOADING
        // ============================================================

        private void DrawLoading(Graphics g)
        {
            if (elapsed < LOADING_START)
                return;

            float progress =
                Clamp(
                    (elapsed - LOADING_START) /
                    LOADING_DURATION,
                    0f,
                    1f
                );

            progress =
                EaseOutCubic(progress);

            int width = 300;
            int height = 7;

            int x =
                (ClientSize.Width - width) / 2;

            int y =
                (int)(
                    ClientSize.Height * 0.76f
                );

            // glow
            if (progress > 0.05f)
            {
                using SolidBrush glow =
                    new SolidBrush(
                        Color.FromArgb(
                            35,
                            255,
                            25,
                            25
                        )
                    );

                g.FillRectangle(
                    glow,
                    x - 8,
                    y - 8,
                    width + 16,
                    height + 16
                );
            }

            Rectangle bar =
                new Rectangle(
                    x,
                    y,
                    width,
                    height
                );

            using SolidBrush background =
                new SolidBrush(
                    Color.FromArgb(
                        75,
                        80,
                        10,
                        10
                    )
                );

            g.FillRoundedRectangle(
                background,
                bar,
                4
            );

            int fillWidth =
                (int)(
                    width *
                    progress
                );

            if (fillWidth > 0)
            {
                Rectangle fill =
                    new Rectangle(
                        x,
                        y,
                        fillWidth,
                        height
                    );

                using LinearGradientBrush fillBrush =
                    new LinearGradientBrush(
                        fill,
                        Color.FromArgb(
                            190,
                            20,
                            20
                        ),
                        Color.FromArgb(
                            255,
                            60,
                            45
                        ),
                        0f
                    );

                g.FillRoundedRectangle(
                    fillBrush,
                    fill,
                    4
                );
            }

            using Font font =
                new Font(
                    "Segoe UI",
                    10,
                    FontStyle.Regular,
                    GraphicsUnit.Pixel
                );

            using SolidBrush textBrush =
                new SolidBrush(
                    Color.FromArgb(
                        180,
                        220,
                        220,
                        220
                    )
                );

            DrawCenteredString(
                g,
                progress >= 1f
                    ? "SYSTEM READY"
                    : "INITIALIZING SYSTEM",
                font,
                textBrush,
                ClientSize.Width / 2,
                y + 24
            );
        }

        // ============================================================
        // TRANSITION
        // ============================================================

        private void DrawTransitionLayer(Graphics g)
        {
            // No off-screen Bitmap is created per frame. This avoids GC spikes
            // and makes the splash-to-main transition visibly smoother.
            float fadeOut =
                1f -
                EaseInOutCubic(
                    Clamp(
                        finalTransition / 0.78f,
                        0f,
                        1f
                    )
                );

            if (fadeOut <= 0.01f)
                return;

            int alpha = (int)(255f * (1f - fadeOut));

            using SolidBrush fadeBrush =
                new SolidBrush(
                    Color.FromArgb(
                        alpha,
                        0,
                        0,
                        0
                    )
                );

            g.FillRectangle(fadeBrush, ClientRectangle);
        }

        // ============================================================
        // FINAL SCREEN
        // ============================================================

        private void DrawFinalScreen(Graphics g)
        {
            float transitionT =
                EaseOutCubic(
                    Clamp(finalTransition, 0f, 1f)
                );

            int cx = ClientSize.Width / 2;
            int cy = (int)(ClientSize.Height * 0.31f);

            // Background red glow.
            float pulse =
                0.5f +
                0.5f * (float)Math.Sin(elapsed * 2.5f);

            int glowSize = (int)(260 + pulse * 18);

            using GraphicsPath glowPath = new GraphicsPath();
            glowPath.AddEllipse(
                cx - glowSize / 2,
                cy - glowSize / 2,
                glowSize,
                glowSize
            );

            using PathGradientBrush glowBrush =
                new PathGradientBrush(glowPath);

            glowBrush.CenterColor = Color.FromArgb(
                (int)(42 * transitionT),
                240,
                35,
                25
            );

            glowBrush.SurroundColors = new[]
            {
                Color.FromArgb(0, 0, 0, 0)
            };

            g.FillPath(glowBrush, glowPath);

            // Circular HUD around the real logo.
            int ringSize = 168;
            int left = cx - ringSize / 2;
            int top = cy - ringSize / 2;

            using Pen ring = new Pen(
                Color.FromArgb((int)(230 * transitionT), 255, 177, 45),
                2.4f
            );

            using Pen ring2 = new Pen(
                Color.FromArgb((int)(135 * transitionT), 220, 104, 25),
                1.2f
            );

            g.DrawEllipse(ring, left, top, ringSize, ringSize);
            g.DrawEllipse(ring2, left + 11, top + 11, ringSize - 22, ringSize - 22);

            Rectangle arcRect = new Rectangle(left - 5, top - 5, ringSize + 10, ringSize + 10);
            using Pen arcPen = new Pen(
                Color.FromArgb((int)(225 * transitionT), 255, 185, 50),
                3f
            );

            float rotation = elapsed * 26f;
            g.DrawArc(arcPen, arcRect, rotation, 52f);
            g.DrawArc(arcPen, arcRect, rotation + 180f, 52f);

            using Pen tickPen = new Pen(
                Color.FromArgb((int)(155 * transitionT), 245, 160, 40),
                1f
            );

            for (int i = 0; i < 24; i++)
            {
                double a = Math.PI * 2.0 * i / 24.0;
                float outer = ringSize / 2f - 1f;
                float inner = outer - (i % 3 == 0 ? 9f : 5f);

                g.DrawLine(
                    tickPen,
                    cx + (float)Math.Cos(a) * inner,
                    cy + (float)Math.Sin(a) * inner,
                    cx + (float)Math.Cos(a) * outer,
                    cy + (float)Math.Sin(a) * outer
                );
            }

            // Real CCS logo centered inside the ring.
            if (logoImage != null)
            {
                int logoSize = 104;
                DrawLogoGlow(g, cx, cy, logoSize, transitionT);

                Rectangle logoRect = new Rectangle(
                    cx - logoSize / 2,
                    cy - logoSize / 2,
                    logoSize,
                    logoSize
                );

                DrawImageWithOpacity(
                    g,
                    logoImage,
                    logoRect,
                    transitionT
                );
            }

            // Main title.
            using Font titleFont = new Font(
                "Segoe UI Semibold",
                42,
                FontStyle.Bold,
                GraphicsUnit.Pixel
            );

            using SolidBrush titleBrush = new SolidBrush(
                Color.FromArgb(
                    (int)(255 * transitionT),
                    248,
                    248,
                    248
                )
            );

            DrawCenteredString(
                g,
                "CDSGA Hub",
                titleFont,
                titleBrush,
                cx,
                (int)(ClientSize.Height * 0.60f)
            );

            using Font subtitleFont = new Font(
                "Segoe UI",
                14,
                FontStyle.Regular,
                GraphicsUnit.Pixel
            );

            using SolidBrush subtitleBrush = new SolidBrush(
                Color.FromArgb(
                    (int)(205 * transitionT),
                    220,
                    220,
                    220
                )
            );

            DrawCenteredString(
                g,
                "COLLEGE OF COMPUTER STUDIES",
                subtitleFont,
                subtitleBrush,
                cx,
                (int)(ClientSize.Height * 0.655f)
            );

            // Green active status pill.
            float activeElapsed = Math.Max(0f, elapsed - FINAL_TRANSITION_START);

            int secondsLeft = Math.Max(
                0,
                (int)Math.Ceiling(ACTIVE_DURATION - activeElapsed)
            );

            int pillWidth = 190;
            int pillHeight = 38;
            int pillX = cx - pillWidth / 2;
            int pillY = (int)(ClientSize.Height * 0.735f);

            using GraphicsPath pillPath = RoundedRect(
                new Rectangle(pillX, pillY, pillWidth, pillHeight),
                19
            );

            using SolidBrush pillBrush = new SolidBrush(
                Color.FromArgb(
                    (int)(120 * transitionT),
                    10,
                    18,
                    13
                )
            );

            using Pen pillPen = new Pen(
                Color.FromArgb(
                    (int)(190 * transitionT),
                    65,
                    165,
                    75
                ),
                1.3f
            );

            g.FillPath(pillBrush, pillPath);
            g.DrawPath(pillPen, pillPath);

            int dotSize = 9;
            using SolidBrush dotBrush = new SolidBrush(
                Color.FromArgb(
                    (int)(245 * transitionT),
                    45,
                    220,
                    105
                )
            );

            g.FillEllipse(
                dotBrush,
                pillX + 20,
                pillY + pillHeight / 2 - dotSize / 2,
                dotSize,
                dotSize
            );

            using Font statusFont = new Font(
                "Segoe UI Semibold",
                12,
                FontStyle.Bold,
                GraphicsUnit.Pixel
            );

            using SolidBrush statusBrush = new SolidBrush(
                Color.FromArgb(
                    (int)(240 * transitionT),
                    205,
                    245,
                    215
                )
            );

            DrawCenteredString(
                g,
                "SYSTEM ACTIVE",
                statusFont,
                statusBrush,
                cx + 10,
                pillY + pillHeight / 2f
            );

            using Font countdownFont = new Font(
                "Segoe UI",
                12,
                FontStyle.Regular,
                GraphicsUnit.Pixel
            );

            using SolidBrush countdownBrush = new SolidBrush(
                Color.FromArgb(
                    (int)(190 * transitionT),
                    190,
                    220,
                    198
                )
            );

            DrawCenteredString(
                g,
                secondsLeft > 0
                    ? "Opening Login in " + secondsLeft
                    : "OPENING LOGIN...",
                countdownFont,
                countdownBrush,
                cx,
                pillY + 57
            );
        }

        private static GraphicsPath RoundedRect(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int d = radius * 2;

            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();

            return path;
        }

        // ============================================================
        // AUTOMATIC LOGIN
        // ============================================================

        private void OpenLoginForm()
        {
            if (loginOpened)
                return;

            loginOpened = true;
            animationTimer.Stop();
            stopwatch.Stop();

            try
            {
                Login loginForm = new Login();

                loginForm.FormClosed += (s, e) =>
                {
                    if (!IsDisposed)
                        Close();
                };

                loginForm.Show();
                Hide();
            }
            catch
            {
                // If Login.cs is not available or cannot be created,
                // keep the splash closed cleanly instead of crashing.
                Close();
            }
        }

        // ============================================================
        // HELPERS
        // ============================================================

        private static void DrawCenteredString(
            Graphics g,
            string text,
            Font font,
            Brush brush,
            float centerX,
            float centerY)
        {
            SizeF size =
                g.MeasureString(
                    text,
                    font
                );

            g.DrawString(
                text,
                font,
                brush,
                centerX - size.Width / 2f,
                centerY - size.Height / 2f
            );
        }

        private static float Clamp(
            float value,
            float min,
            float max)
        {
            return Math.Max(
                min,
                Math.Min(
                    max,
                    value
                )
            );
        }

        private static float EaseOutCubic(float t)
        {
            t =
                Clamp(
                    t,
                    0f,
                    1f
                );

            return
                1f -
                (float)Math.Pow(
                    1f - t,
                    3
                );
        }

        private static float EaseOutBack(float t)
        {
            t =
                Clamp(
                    t,
                    0f,
                    1f
                );

            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;

            return
                1f +
                c3 *
                (float)Math.Pow(
                    t - 1f,
                    3
                ) +
                c1 *
                (float)Math.Pow(
                    t - 1f,
                    2
                );
        }

        private static float EaseInOutCubic(float t)
        {
            t =
                Clamp(
                    t,
                    0f,
                    1f
                );

            return
                t < 0.5f
                    ? 4f * t * t * t
                    : 1f -
                      (float)Math.Pow(
                          -2f * t + 2f,
                          3
                      ) / 2f;
        }

        // ============================================================
        // PARTICLE CLASS
        // ============================================================

        private class Particle
        {
            public float X;
            public float Y;
            public float Speed;
            public float Size;
            public float Phase;
            public int Alpha;
        }
    }

    // ================================================================
    // GRAPHICS EXTENSIONS
    // ================================================================

    public static class GraphicsExtensions
    {
        public static void DrawRoundedRectangle(
            this Graphics graphics,
            Pen pen,
            Rectangle rectangle,
            int radius)
        {
            using GraphicsPath path =
                CreateRoundedPath(
                    rectangle,
                    radius
                );

            graphics.DrawPath(
                pen,
                path
            );
        }

        public static void FillRoundedRectangle(
            this Graphics graphics,
            Brush brush,
            Rectangle rectangle,
            int radius)
        {
            using GraphicsPath path =
                CreateRoundedPath(
                    rectangle,
                    radius
                );

            graphics.FillPath(
                brush,
                path
            );
        }

        private static GraphicsPath CreateRoundedPath(
            Rectangle rectangle,
            int radius)
        {
            GraphicsPath path =
                new GraphicsPath();

            int diameter =
                radius * 2;

            if (diameter > rectangle.Width)
                diameter = rectangle.Width;

            if (diameter > rectangle.Height)
                diameter = rectangle.Height;

            Rectangle arc =
                new Rectangle(
                    rectangle.X,
                    rectangle.Y,
                    diameter,
                    diameter
                );

            path.AddArc(
                arc,
                180,
                90
            );

            arc.X =
                rectangle.Right -
                diameter;

            path.AddArc(
                arc,
                270,
                90
            );

            arc.Y =
                rectangle.Bottom -
                diameter;

            path.AddArc(
                arc,
                0,
                90
            );

            arc.X =
                rectangle.Left;

            path.AddArc(
                arc,
                90,
                90
            );

            path.CloseFigure();

            return path;
        }
    }
}