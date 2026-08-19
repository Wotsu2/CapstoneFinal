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

        private bool finalState;

        // ============================================================
        // TIMELINE
        // ============================================================

        private const float INITIALIZING_END = 1.8f;

        private const float CORE_START = 1.4f;
        private const float CORE_END = 3.7f;

        private const float LOGO_START = 2.8f;
        private const float LOGO_END = 5.4f;

        private const float TITLE_START = 4.6f;

        private const float LOADING_START = 5.7f;
        private const float READY_START = 7.0f;

        private const float FINAL_TRANSITION_START = 7.5f;
        private const float FINAL_TRANSITION_DURATION = 1.5f;

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
        }

        // ============================================================
        // FORM
        // ============================================================

        private void SplashForm_Load(object sender, EventArgs e)
        {
            CenterEnterButton();
            animationTimer.Start();
        }

        private void SplashForm_Resize(object sender, EventArgs e)
        {
            CenterEnterButton();
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
            elapsed =
                (float)stopwatch.Elapsed.TotalSeconds;

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

                if (finalTransition > 0.12f)
                {
                    enterButton.Visible = true;
                    enterButton.Enabled = true;
                }

                AnimateEnterButton();
            }

            UpdateParticles();

            Invalidate();
        }

        private void AnimateEnterButton()
        {
            if (!enterButton.Visible)
                return;

            float t =
                EaseOutCubic(
                    Clamp(
                        (finalTransition - 0.12f) / 0.88f,
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

        private void UpdateParticles()
        {
            if (particles.Count == 0)
                return;

            foreach (Particle p in particles)
            {
                p.Y -= p.Speed * 0.016f;

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
                    (elapsed - 0.4f) / 1.2f,
                    0f,
                    1f
                );

            foreach (Particle p in particles)
            {
                float pulse =
                    0.55f +
                    0.45f *
                    (float)Math.Sin(
                        elapsed * 2.5f + p.Phase
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
                "INITIALIZING CDSGA HUB",
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
                "Loading laboratory management environment",
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
                Clamp(
                    (elapsed - CORE_START) /
                    (CORE_END - CORE_START),
                    0f,
                    1f
                );

            progress =
                EaseOutCubic(progress);

            int cx =
                ClientSize.Width / 2;

            int cy =
                (int)(
                    ClientSize.Height * 0.39f
                );

            float pulse =
                0.5f +
                0.5f *
                (float)Math.Sin(
                    elapsed * 2.8f
                );

            // ========================================================
            // OUTER AURA
            // ========================================================

            int aura =
                (int)(
                    190 +
                    pulse * 25
                );

            using GraphicsPath auraPath =
                new GraphicsPath();

            auraPath.AddEllipse(
                cx - aura / 2,
                cy - aura / 2,
                aura,
                aura
            );

            using PathGradientBrush auraBrush =
                new PathGradientBrush(auraPath);

            auraBrush.CenterColor =
                Color.FromArgb(
                    (int)(48 * progress),
                    220,
                    20,
                    20
                );

            auraBrush.SurroundColors =
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
                auraBrush,
                auraPath
            );

            // ========================================================
            // ORBIT RINGS
            // ========================================================

            float rotation =
                elapsed * 0.7f;

            using Pen orbitPen =
                new Pen(
                    Color.FromArgb(
                        (int)(95 * progress),
                        230,
                        35,
                        35
                    ),
                    1.2f
                );

            using Pen orbitSoft =
                new Pen(
                    Color.FromArgb(
                        (int)(45 * progress),
                        255,
                        45,
                        45
                    ),
                    2.2f
                );

            Rectangle orbit1 =
                new Rectangle(
                    cx - 96,
                    cy - 34,
                    192,
                    68
                );

            Rectangle orbit2 =
                new Rectangle(
                    cx - 72,
                    cy - 104,
                    144,
                    208
                );

            g.TranslateTransform(
                cx,
                cy
            );

            g.RotateTransform(
                rotation * 25f
            );

            g.DrawEllipse(
                orbitPen,
                -96,
                -34,
                192,
                68
            );

            g.ResetTransform();

            g.TranslateTransform(
                cx,
                cy
            );

            g.RotateTransform(
                -rotation * 18f
            );

            g.DrawEllipse(
                orbitSoft,
                -72,
                -104,
                144,
                208
            );

            g.ResetTransform();

            // ========================================================
            // CORE CIRCLE
            // ========================================================

            int coreSize =
                (int)(
                    86 *
                    progress
                );

            if (coreSize <= 2)
                return;

            using GraphicsPath corePath =
                new GraphicsPath();

            corePath.AddEllipse(
                cx - coreSize / 2,
                cy - coreSize / 2,
                coreSize,
                coreSize
            );

            using PathGradientBrush coreBrush =
                new PathGradientBrush(corePath);

            coreBrush.CenterColor =
                Color.FromArgb(
                    230,
                    255,
                    45,
                    40
                );

            coreBrush.SurroundColors =
                new[]
                {
                    Color.FromArgb(
                        20,
                        100,
                        0,
                        0
                    )
                };

            g.FillPath(
                coreBrush,
                corePath
            );

            using Pen coreBorder =
                new Pen(
                    Color.FromArgb(
                        240,
                        255,
                        50,
                        45
                    ),
                    2
                );

            g.DrawEllipse(
                coreBorder,
                cx - coreSize / 2,
                cy - coreSize / 2,
                coreSize,
                coreSize
            );

            // ========================================================
            // CORE SYMBOL
            // ========================================================

            using Pen symbol =
                new Pen(
                    Color.FromArgb(
                        230,
                        255,
                        230,
                        225
                    ),
                    2
                );

            int s = coreSize / 4;

            g.DrawLine(
                symbol,
                cx - s,
                cy,
                cx + s,
                cy
            );

            g.DrawLine(
                symbol,
                cx,
                cy - s,
                cx,
                cy + s
            );

            // rotating light point
            double angle =
                elapsed * 2.5;

            float px =
                cx +
                (float)Math.Cos(angle) *
                105;

            float py =
                cy +
                (float)Math.Sin(angle) *
                105;

            using SolidBrush point =
                new SolidBrush(
                    Color.FromArgb(
                        230,
                        255,
                        55,
                        50
                    )
                );

            g.FillEllipse(
                point,
                px - 3,
                py - 3,
                6,
                6
            );
        }

        // ============================================================
        // LOGO
        // ============================================================

        private void DrawLogo(Graphics g)
        {
            if (logoImage == null)
                return;

            if (elapsed < LOGO_START)
                return;

            float progress =
                Clamp(
                    (elapsed - LOGO_START) /
                    1.6f,
                    0f,
                    1f
                );

            float scale =
                EaseOutBack(progress);

            float fade =
                EaseOutCubic(
                    Clamp(
                        (elapsed - LOGO_START) /
                        0.65f,
                        0f,
                        1f
                    )
                );

            int maxSize =
                (int)(
                    Math.Min(
                        ClientSize.Width,
                        ClientSize.Height
                    ) * 0.27f
                );

            int size =
                (int)(
                    maxSize *
                    scale
                );

            if (size <= 0)
                return;

            int cx =
                ClientSize.Width / 2;

            int cy =
                (int)(
                    ClientSize.Height * 0.39f
                );

            DrawLogoGlow(
                g,
                cx,
                cy,
                size,
                fade
            );

            Rectangle destination =
                new Rectangle(
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
                    "Segoe UI",
                    40,
                    FontStyle.Bold,
                    GraphicsUnit.Pixel
                );

            using Font subtitleFont =
                new Font(
                    "Segoe UI",
                    14,
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
                    1.2f,
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
            float fadeOut =
                1f -
                EaseInOutCubic(
                    Clamp(
                        finalTransition / 0.7f,
                        0f,
                        1f
                    )
                );

            if (fadeOut <= 0.01f)
                return;

            using Bitmap layer =
                new Bitmap(
                    ClientSize.Width,
                    ClientSize.Height
                );

            using Graphics lg =
                Graphics.FromImage(layer);

            lg.SmoothingMode =
                SmoothingMode.AntiAlias;

            lg.InterpolationMode =
                InterpolationMode.HighQualityBicubic;

            DrawCore(lg);
            DrawLogo(lg);
            DrawTitle(lg);
            DrawLoading(lg);

            using ImageAttributes attributes =
                new ImageAttributes();

            ColorMatrix matrix =
                new ColorMatrix();

            matrix.Matrix33 =
                fadeOut;

            attributes.SetColorMatrix(matrix);

            g.DrawImage(
                layer,
                new Rectangle(
                    0,
                    0,
                    ClientSize.Width,
                    ClientSize.Height
                ),
                0,
                0,
                layer.Width,
                layer.Height,
                GraphicsUnit.Pixel,
                attributes
            );
        }

        // ============================================================
        // FINAL SCREEN
        // ============================================================

        private void DrawFinalScreen(Graphics g)
        {
            float t =
                EaseOutCubic(
                    Clamp(
                        (finalTransition - 0.12f) /
                        0.88f,
                        0f,
                        1f
                    )
                );

            if (t <= 0)
                return;

            int cx =
                ClientSize.Width / 2;

            // ========================================================
            // CENTRAL AURA
            // ========================================================

            float pulse =
                0.5f +
                0.5f *
                (float)Math.Sin(
                    elapsed * 2.3f
                );

            int aura =
                (int)(
                    320 +
                    pulse * 25
                );

            using GraphicsPath auraPath =
                new GraphicsPath();

            auraPath.AddEllipse(
                cx - aura / 2,
                (int)(
                    ClientSize.Height * 0.30f
                ),
                aura,
                aura
            );

            using PathGradientBrush auraBrush =
                new PathGradientBrush(auraPath);

            auraBrush.CenterColor =
                Color.FromArgb(
                    (int)(55 * t),
                    190,
                    15,
                    15
                );

            auraBrush.SurroundColors =
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
                auraBrush,
                auraPath
            );

            // ========================================================
            // LOGO
            // ========================================================

            if (logoImage != null)
            {
                int logoSize =
                    (int)(
                        135 *
                        t
                    );

                int logoY =
                    (int)(
                        ClientSize.Height * 0.25f -
                        25 * (1f - t)
                    );

                Rectangle logoRect =
                    new Rectangle(
                        cx - logoSize / 2,
                        logoY,
                        logoSize,
                        logoSize
                    );

                DrawImageWithOpacity(
                    g,
                    logoImage,
                    logoRect,
                    t
                );
            }

            // ========================================================
            // TEXT
            // ========================================================

            float textT =
                EaseOutCubic(
                    Clamp(
                        (finalTransition - 0.28f) /
                        0.72f,
                        0f,
                        1f
                    )
                );

            int titleY =
                (int)(
                    ClientSize.Height * 0.52f -
                    18 *
                    (1f - textT)
                );

            using Font titleFont =
                new Font(
                    "Segoe UI",
                    36,
                    FontStyle.Bold,
                    GraphicsUnit.Pixel
                );

            using Font subtitleFont =
                new Font(
                    "Segoe UI",
                    15,
                    FontStyle.Regular,
                    GraphicsUnit.Pixel
                );

            using SolidBrush titleBrush =
                new SolidBrush(
                    Color.FromArgb(
                        (int)(255 * textT),
                        255,
                        245,
                        238
                    )
                );

            using SolidBrush subtitleBrush =
                new SolidBrush(
                    Color.FromArgb(
                        (int)(205 * textT),
                        225,
                        225,
                        225
                    )
                );

            DrawCenteredString(
                g,
                "CDSGA Hub",
                titleFont,
                titleBrush,
                cx,
                titleY
            );

            DrawCenteredString(
                g,
                "College of Computer Studies",
                subtitleFont,
                subtitleBrush,
                cx,
                titleY + 48
            );

            // ========================================================
            // ONLINE INDICATOR
            // ========================================================

            float statusT =
                EaseOutCubic(
                    Clamp(
                        (finalTransition - 0.4f) /
                        0.6f,
                        0f,
                        1f
                    )
                );

            int statusY =
                titleY + 86;

            int dotX =
                cx - 74;

            using SolidBrush dot =
                new SolidBrush(
                    Color.FromArgb(
                        (int)(230 * statusT),
                        65,
                        255,
                        145
                    )
                );

            g.FillEllipse(
                dot,
                dotX,
                statusY - 4,
                8,
                8
            );

            using Font statusFont =
                new Font(
                    "Segoe UI",
                    10,
                    FontStyle.Regular,
                    GraphicsUnit.Pixel
                );

            using SolidBrush statusBrush =
                new SolidBrush(
                    Color.FromArgb(
                        (int)(170 * statusT),
                        205,
                        225,
                        215
                    )
                );

            g.DrawString(
                "SYSTEM ONLINE",
                statusFont,
                statusBrush,
                dotX + 16,
                statusY - 9
            );

            // ========================================================
            // BUTTON AURA
            // ========================================================

            if (t > 0.25f)
            {
                float pulseButton =
                    0.5f +
                    0.5f *
                    (float)Math.Sin(
                        elapsed * 3f
                    );

                int buttonWidth =
                    enterButton.Width + 45;

                int buttonHeight =
                    enterButton.Height + 25;

                int buttonX =
                    cx -
                    buttonWidth / 2;

                int buttonY =
                    enterButton.Top -
                    12;

                using GraphicsPath buttonPath =
                    new GraphicsPath();

                buttonPath.AddRectangle(
                    new Rectangle(
                        buttonX,
                        buttonY,
                        buttonWidth,
                        buttonHeight
                    )
                );

                using PathGradientBrush buttonGlow =
                    new PathGradientBrush(
                        buttonPath
                    );

                buttonGlow.CenterColor =
                    Color.FromArgb(
                        (int)(
                            30 +
                            pulseButton * 35
                        ),
                        255,
                        25,
                        25
                    );

                buttonGlow.SurroundColors =
                    new[]
                    {
                        Color.FromArgb(
                            0,
                            255,
                            0,
                            0
                        )
                    };

                g.FillPath(
                    buttonGlow,
                    buttonPath
                );
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