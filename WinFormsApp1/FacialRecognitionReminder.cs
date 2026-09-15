using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace WinFormsApp1
{
    public class FacialRecognitionReminder : Form
    {
        private readonly string studentId;
        private readonly Action registerAction;

        private Button btnRegister;
        private Button btnClose;
        private Button btnSkip;

        // =========================================================
        // COLORS
        // =========================================================

        private readonly Color Maroon =
            Color.FromArgb(145, 0, 0);

        private readonly Color MaroonDark =
            Color.FromArgb(110, 0, 0);

        private readonly Color Gold =
            Color.FromArgb(195, 175, 20);

        private readonly Color GoldDark =
            Color.FromArgb(165, 145, 10);

        private readonly Color Background =
            Color.FromArgb(245, 245, 245);

        private readonly Color TextDark =
            Color.FromArgb(45, 45, 45);

        private readonly Color TextGray =
            Color.FromArgb(90, 90, 90);

        private readonly Color BorderGray =
            Color.FromArgb(210, 210, 210);

        private readonly Color WarningBackground =
            Color.FromArgb(252, 250, 225);


        // =========================================================
        // CONSTRUCTOR
        // =========================================================

        public FacialRecognitionReminder(
            string studentId,
            Action registerAction)
        {
            this.studentId = studentId;
            this.registerAction = registerAction;

            BuildUI();
        }


        // =========================================================
        // BUILD UI
        // =========================================================

        private void BuildUI()
        {
            Text =
                "CDSGA HUB Account Security";

            StartPosition =
                FormStartPosition.CenterScreen;

            ClientSize =
                new Size(1100, 800);

            FormBorderStyle =
                FormBorderStyle.None;

            MaximizeBox = false;
            MinimizeBox = false;

            BackColor =
                Background;

            DoubleBuffered = true;


            // =====================================================
            // HEADER
            // =====================================================

            Panel header =
                new Panel();

            header.Location =
                new Point(0, 0);

            header.Size =
                new Size(1100, 90);

            header.BackColor =
                Maroon;

            Controls.Add(header);


            // TITLE
            Label lblTitle =
                new Label();

            lblTitle.Text =
                "CDSGA HUB Account Security — Facial Profile Setup";

            lblTitle.Font =
                new Font(
                    "Segoe UI",
                    21,
                    FontStyle.Regular);

            lblTitle.ForeColor =
                Color.White;

            lblTitle.AutoSize =
                true;

            lblTitle.Location =
                new Point(45, 22);

            header.Controls.Add(
                lblTitle);


            // SUBTITLE
            Label lblSetup =
                new Label();

            lblSetup.Text =
                "Facial Profile Setup";

            lblSetup.Font =
                new Font(
                    "Segoe UI",
                    10,
                    FontStyle.Bold);

            lblSetup.ForeColor =
                Color.White;

            lblSetup.AutoSize =
                true;

            lblSetup.Location =
                new Point(47, 58);

            header.Controls.Add(
                lblSetup);


            // =====================================================
            // CLOSE BUTTON
            // =====================================================

            btnClose =
                new Button();

            btnClose.Text =
                "×";

            btnClose.Font =
                new Font(
                    "Segoe UI",
                    26,
                    FontStyle.Regular);

            btnClose.ForeColor =
                Color.White;

            btnClose.BackColor =
                Maroon;

            btnClose.FlatStyle =
                FlatStyle.Flat;

            btnClose.FlatAppearance.BorderSize =
                0;

            btnClose.Size =
                new Size(60, 60);

            btnClose.Location =
                new Point(1020, 14);

            btnClose.Cursor =
                Cursors.Hand;

            btnClose.Click +=
                (s, e) =>
                {
                    Close();
                };

            header.Controls.Add(
                btnClose);


            // =====================================================
            // INTRODUCTION
            // =====================================================

            Label lblIntro =
                new Label();

            lblIntro.Text =
                "To help protect your account and strengthen system security, CDSGA HUB requires facial verification\r\n" +
                "as an additional layer of identity authentication.\r\n" +
                "Please complete your facial profile setup to continue using the system securely.";

            lblIntro.Font =
                new Font(
                    "Segoe UI",
                    11,
                    FontStyle.Regular);

            lblIntro.ForeColor =
                TextGray;

            lblIntro.AutoSize =
                true;

            lblIntro.Location =
                new Point(65, 120);

            Controls.Add(
                lblIntro);


            // =====================================================
            // SECURITY REQUIREMENT
            // =====================================================

            Panel securityPanel =
                new Panel();

            securityPanel.Location =
                new Point(65, 205);

            securityPanel.Size =
                new Size(970, 100);

            securityPanel.BackColor =
                Color.FromArgb(
                    250,
                    250,
                    250);

            securityPanel.Paint +=
                (s, e) =>
                {
                    using Pen p =
                        new Pen(
                            Maroon,
                            1);

                    e.Graphics.DrawRectangle(
                        p,
                        0,
                        0,
                        securityPanel.Width - 1,
                        securityPanel.Height - 1);
                };

            Controls.Add(
                securityPanel);


            // MAROON LINE
            Panel securityLine =
                new Panel();

            securityLine.Location =
                new Point(22, 20);

            securityLine.Size =
                new Size(3, 60);

            securityLine.BackColor =
                Maroon;

            securityPanel.Controls.Add(
                securityLine);


            // SECURITY TITLE
            Label lblSecurity =
                new Label();

            lblSecurity.Text =
                "Security Requirement";

            lblSecurity.Font =
                new Font(
                    "Segoe UI",
                    12,
                    FontStyle.Bold);

            lblSecurity.ForeColor =
                Maroon;

            lblSecurity.AutoSize =
                true;

            lblSecurity.Location =
                new Point(45, 17);

            securityPanel.Controls.Add(
                lblSecurity);


            // SECURITY DESCRIPTION
            Label lblSecurityText =
                new Label();

            lblSecurityText.Text =
                "Users without a registered facial profile will be asked to complete a live selfie verification.\r\n" +
                "The verified facial image will be saved as their profile picture and used for future identity verification.";

            lblSecurityText.Font =
                new Font(
                    "Segoe UI",
                    10,
                    FontStyle.Regular);

            lblSecurityText.ForeColor =
                TextGray;

            lblSecurityText.AutoSize =
                true;

            lblSecurityText.Location =
                new Point(45, 48);

            securityPanel.Controls.Add(
                lblSecurityText);


            // =====================================================
            // STUDENT PROFILE CARD
            // =====================================================

            Panel profileCard =
                new Panel();

            profileCard.Location =
                new Point(365, 335);

            profileCard.Size =
                new Size(370, 355);

            profileCard.BackColor =
                Color.White;

            profileCard.Paint +=
                (s, e) =>
                {
                    using Pen p =
                        new Pen(
                            BorderGray,
                            1);

                    e.Graphics.DrawRectangle(
                        p,
                        0,
                        0,
                        profileCard.Width - 1,
                        profileCard.Height - 1);
                };

            Controls.Add(
                profileCard);


            // =====================================================
            // PROFILE TITLE
            // =====================================================

            Label lblProfile =
                new Label();

            lblProfile.Text =
                "STUDENT PROFILE";

            lblProfile.Font =
                new Font(
                    "Segoe UI",
                    12,
                    FontStyle.Bold);

            lblProfile.ForeColor =
                TextDark;

            lblProfile.AutoSize =
                true;

            lblProfile.Location =
                new Point(30, 22);

            profileCard.Controls.Add(
                lblProfile);


            // =====================================================
            // AVATAR
            // =====================================================

            Panel avatarPanel =
                new Panel();

            avatarPanel.Location =
                new Point(120, 58);

            avatarPanel.Size =
                new Size(130, 110);

            avatarPanel.BackColor =
                Color.Transparent;

            avatarPanel.Paint +=
                DrawAvatar;

            profileCard.Controls.Add(
                avatarPanel);


            // =====================================================
            // PHOTO UNAVAILABLE
            // =====================================================

            Panel warningPanel =
                new Panel();

            warningPanel.Location =
                new Point(25, 180);

            warningPanel.Size =
                new Size(320, 85);

            warningPanel.BackColor =
                WarningBackground;

            warningPanel.Paint +=
                (s, e) =>
                {
                    using Pen p =
                        new Pen(
                            Maroon,
                            1);

                    e.Graphics.DrawRectangle(
                        p,
                        0,
                        0,
                        warningPanel.Width - 1,
                        warningPanel.Height - 1);
                };

            profileCard.Controls.Add(
                warningPanel);


            // =====================================================
            // WARNING ICON
            // =====================================================

            Label lblWarningIcon =
                new Label();

            lblWarningIcon.Text =
                "⚠";

            lblWarningIcon.Font =
                new Font(
                    "Segoe UI Symbol",
                    23,
                    FontStyle.Regular);

            lblWarningIcon.ForeColor =
                Maroon;

            lblWarningIcon.AutoSize =
                true;

            lblWarningIcon.Location =
                new Point(14, 24);

            warningPanel.Controls.Add(
                lblWarningIcon);


            // =====================================================
            // WARNING TITLE
            // =====================================================

            Label lblWarning =
                new Label();

            lblWarning.Text =
                "PHOTO UNAVAILABLE";

            lblWarning.Font =
                new Font(
                    "Segoe UI",
                    9.5f,
                    FontStyle.Bold);

            lblWarning.ForeColor =
                Maroon;

            lblWarning.AutoSize =
                true;

            lblWarning.Location =
                new Point(57, 10);

            warningPanel.Controls.Add(
                lblWarning);


            // =====================================================
            // WARNING DESCRIPTION
            // =====================================================

            Label lblWarningText =
                new Label();

            lblWarningText.Text =
                "You haven't set up your facial profile yet.\r\n" +
                "Take a profile picture to continue using\r\n" +
                "the system securely.";

            lblWarningText.Font =
                new Font(
                    "Segoe UI",
                    8.5f,
                    FontStyle.Regular);

            lblWarningText.ForeColor =
                TextGray;

            lblWarningText.AutoSize =
                true;

            lblWarningText.Location =
                new Point(57, 34);

            warningPanel.Controls.Add(
                lblWarningText);


            // =====================================================
            // TAKE PROFILE PICTURE
            // =====================================================

            btnRegister =
                new Button();

            btnRegister.Text =
                "📷   Take Profile Picture";

            btnRegister.Font =
                new Font(
                    "Segoe UI",
                    10.5f,
                    FontStyle.Bold);

            btnRegister.ForeColor =
                Color.White;

            btnRegister.BackColor =
                Gold;

            btnRegister.FlatStyle =
                FlatStyle.Flat;

            btnRegister.FlatAppearance.BorderSize =
                0;

            btnRegister.Size =
                new Size(320, 45);

            btnRegister.Location =
                new Point(25, 285);

            btnRegister.Cursor =
                Cursors.Hand;

            btnRegister.Click +=
                BtnRegister_Click;

            btnRegister.MouseEnter +=
                (s, e) =>
                {
                    btnRegister.BackColor =
                        GoldDark;
                };

            btnRegister.MouseLeave +=
                (s, e) =>
                {
                    btnRegister.BackColor =
                        Gold;
                };

            profileCard.Controls.Add(
                btnRegister);


            // =====================================================
            // SKIP BUTTON
            // =====================================================

            btnSkip =
                new Button();

            btnSkip.Text =
                "Skip for now";

            btnSkip.Font =
                new Font(
                    "Segoe UI",
                    10,
                    FontStyle.Bold);

            btnSkip.ForeColor =
                Color.White;

            btnSkip.BackColor =
                Maroon;

            btnSkip.FlatStyle =
                FlatStyle.Flat;

            btnSkip.FlatAppearance.BorderSize =
                0;

            btnSkip.Size =
                new Size(330, 45);

            btnSkip.Location =
                new Point(385, 710);

            btnSkip.Cursor =
                Cursors.Hand;

            btnSkip.Click +=
                (s, e) =>
                {
                    Close();
                };

            btnSkip.MouseEnter +=
                (s, e) =>
                {
                    btnSkip.BackColor =
                        MaroonDark;
                };

            btnSkip.MouseLeave +=
                (s, e) =>
                {
                    btnSkip.BackColor =
                        Maroon;
                };

            Controls.Add(
                btnSkip);


            // =====================================================
            // IMPORTANT REMINDER
            // =====================================================

            Label lblReminder =
                new Label();

            lblReminder.Text =
                "* Important Reminder — Please make sure your face is clearly visible, properly centered, and well-lit.\r\n" +
                "  Avoid using filters, hats, sunglasses, or anything that may cover your face.";

            lblReminder.Font =
                new Font(
                    "Segoe UI",
                    9.5f,
                    FontStyle.Regular);

            lblReminder.ForeColor =
                TextGray;

            lblReminder.AutoSize =
                true;

            lblReminder.Location =
                new Point(120, 765);

            Controls.Add(
                lblReminder);
        }


        // =========================================================
        // AVATAR DRAWING
        // =========================================================

        private void DrawAvatar(
            object sender,
            PaintEventArgs e)
        {
            Graphics g =
                e.Graphics;

            g.SmoothingMode =
                SmoothingMode.AntiAlias;


            // Background
            using Brush bg =
                new SolidBrush(
                    Color.FromArgb(
                        225,
                        225,
                        225));

            g.FillEllipse(
                bg,
                10,
                0,
                110,
                110);


            // Person
            using Brush gray =
                new SolidBrush(
                    Color.FromArgb(
                        170,
                        170,
                        170));


            // HEAD
            g.FillEllipse(
                gray,
                45,
                15,
                40,
                40);


            // BODY
            g.FillEllipse(
                gray,
                27,
                55,
                76,
                65);
        }


        // =========================================================
        // TAKE PROFILE PICTURE
        // =========================================================

        private void BtnRegister_Click(
            object sender,
            EventArgs e)
        {
            registerAction?.Invoke();
        }


        // =========================================================
        // SHOW REMINDER
        // WITH BLURRED BACKGROUND
        // =========================================================

        public static bool ShowIfRequired(
            Form owner,
            string studentId,
            bool hasFacialRecognition,
            Action openRegistration)
        {
            if (hasFacialRecognition)
                return true;


            // ==============================================
            // CREATE BLURRED BACKGROUND
            // ==============================================

            using (
                BlurredBackground blur =
                    new BlurredBackground(owner))
            {
                blur.Show(owner);

                blur.BringToFront();

                Application.DoEvents();


                // ==========================================
                // SHOW REMINDER ON TOP
                // ==========================================

                using (
                    FacialRecognitionReminder reminder =
                        new FacialRecognitionReminder(
                            studentId,
                            openRegistration))
                {
                    reminder.ShowDialog(owner);
                }
            }


            return false;
        }
    }


    // =============================================================
    // BLURRED BACKGROUND FORM
    // =============================================================

    internal class BlurredBackground : Form
    {
        private Bitmap blurredImage;


        public BlurredBackground(Form owner)
        {
            FormBorderStyle =
                FormBorderStyle.None;

            ShowInTaskbar =
                false;

            StartPosition =
                FormStartPosition.Manual;

            Owner =
                owner;

            Rectangle bounds =
                owner.Bounds;

            Location =
                bounds.Location;

            Size =
                bounds.Size;

            BackColor =
                Color.Black;

            DoubleBuffered =
                true;


            // ==========================================
            // CAPTURE OWNER
            // ==========================================

            Bitmap screenshot =
                new Bitmap(
                    bounds.Width,
                    bounds.Height);

            using (
                Graphics g =
                    Graphics.FromImage(
                        screenshot))
            {
                g.CopyFromScreen(
                    bounds.Location,
                    Point.Empty,
                    bounds.Size);
            }


            // ==========================================
            // CREATE BLUR
            // ==========================================

            blurredImage =
                CreateBlur(
                    screenshot);

            screenshot.Dispose();


            // ==========================================
            // DARKEN
            // ==========================================

            Panel darkLayer =
                new Panel();

            darkLayer.Dock =
                DockStyle.Fill;

            darkLayer.BackColor =
                Color.FromArgb(
                    75,
                    0,
                    0,
                    0);

            Controls.Add(
                darkLayer);


            // ==========================================
            // DRAW BLUR
            // ==========================================

            BackgroundImage =
                blurredImage;

            BackgroundImageLayout =
                ImageLayout.Stretch;
        }


        // =========================================================
        // SIMPLE BLUR
        // =========================================================

        private Bitmap CreateBlur(
            Bitmap source)
        {
            Bitmap result =
                new Bitmap(
                    source.Width,
                    source.Height);

            using (
                Graphics g =
                    Graphics.FromImage(
                        result))
            {
                g.SmoothingMode =
                    SmoothingMode.HighQuality;

                g.InterpolationMode =
                    InterpolationMode.HighQualityBilinear;

                g.PixelOffsetMode =
                    PixelOffsetMode.HighQuality;

                // Base image
                g.DrawImage(
                    source,
                    new Rectangle(
                        0,
                        0,
                        source.Width,
                        source.Height));


                // Blur copies
                int blurAmount = 5;

                for (
                    int x = -blurAmount;
                    x <= blurAmount;
                    x += 2)
                {
                    for (
                        int y = -blurAmount;
                        y <= blurAmount;
                        y += 2)
                    {
                        if (x == 0 && y == 0)
                            continue;

                        using SolidBrush brush =
                            new SolidBrush(
                                Color.FromArgb(
                                    12,
                                    Color.White));

                        g.DrawImage(
                            source,
                            new Rectangle(
                                x,
                                y,
                                source.Width,
                                source.Height));
                    }
                }
            }

            return result;
        }


        // =========================================================
        // CLEANUP
        // =========================================================

        protected override void Dispose(
            bool disposing)
        {
            if (disposing)
            {
                if (blurredImage != null)
                {
                    blurredImage.Dispose();
                    blurredImage = null;
                }
            }

            base.Dispose(disposing);
        }
    }
}