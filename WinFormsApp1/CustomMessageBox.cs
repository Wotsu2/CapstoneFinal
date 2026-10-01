using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace WinFormsApp1
{
    // 1. Enums for Buttons and Icons
    public enum CustomMessageBoxButtons
    {
        OK,
        YesNo
    }

    public enum CustomMessageBoxIcon
    {
        None,
        Information,
        Warning,
        Error,
        Question
    }

    public enum CustomMessageBoxResult
    {
        OK,
        Yes,
        No,
        Cancel
    }

    // 2. Static Helper Class (Use this to call the message box)
    public static class CustomMessageBox
    {
        public static CustomMessageBoxResult Show(string message, string title = "Message",
            CustomMessageBoxButtons buttons = CustomMessageBoxButtons.OK,
            CustomMessageBoxIcon icon = CustomMessageBoxIcon.None)
        {
            using (var form = new CustomMessageBoxForm(message, title, buttons, icon))
            {
                form.ShowDialog();
                return form.Result;
            }
        }
    }

    // 3. UI Form Class (This is the actual visual popup)
    public partial class CustomMessageBoxForm : Form
    {
        public CustomMessageBoxResult Result { get; private set; } = CustomMessageBoxResult.Cancel;

        private readonly Color HeaderColor = ColorTranslator.FromHtml("#5A1015"); // Dark Mahogany
        private readonly Color PrimaryButtonColor = ColorTranslator.FromHtml("#941B1B"); // Deep Crimson
        private readonly Color BackgroundColor = ColorTranslator.FromHtml("#F7F4F5"); // Light Gray/Pinkish

        public CustomMessageBoxForm(string message, string title, CustomMessageBoxButtons buttons, CustomMessageBoxIcon icon)
        {
            InitializeComponent(message, title, buttons, icon);
        }

        private void InitializeComponent(string message, string title, CustomMessageBoxButtons buttons, CustomMessageBoxIcon icon)
        {
            // 1. Form Settings
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = BackgroundColor;
            this.Size = new Size(460, 210);
            this.Padding = new Padding(2);
            this.ShowInTaskbar = false;

            // Subtle border
            this.Paint += (s, e) =>
            {
                e.Graphics.DrawRectangle(new Pen(ColorTranslator.FromHtml("#CCCCCC"), 2), this.ClientRectangle);
            };

            // 2. Header Panel
            Panel headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 45,
                BackColor = HeaderColor
            };
            this.Controls.Add(headerPanel);

            // 3. Title Label
            Label lblTitle = new Label
            {
                Text = title,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Location = new Point(15, 12),
                AutoSize = true
            };
            headerPanel.Controls.Add(lblTitle);

            // 4. Close Button
            Label lblClose = new Label
            {
                Text = "✕",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Location = new Point(this.Width - 35, 12),
                Cursor = Cursors.Hand
            };
            lblClose.Click += (s, e) => { this.Result = CustomMessageBoxResult.Cancel; this.Close(); };
            headerPanel.Controls.Add(lblClose);

            // 5. Icon
            if (icon != CustomMessageBoxIcon.None)
            {
                PictureBox iconBox = new PictureBox
                {
                    SizeMode = PictureBoxSizeMode.StretchImage,
                    Size = new Size(32, 32),
                    Location = new Point(25, 75)
                };

                switch (icon)
                {
                    case CustomMessageBoxIcon.Information: iconBox.Image = SystemIcons.Information.ToBitmap(); break;
                    case CustomMessageBoxIcon.Warning: iconBox.Image = SystemIcons.Warning.ToBitmap(); break;
                    case CustomMessageBoxIcon.Error: iconBox.Image = SystemIcons.Error.ToBitmap(); break;
                    case CustomMessageBoxIcon.Question: iconBox.Image = SystemIcons.Question.ToBitmap(); break;
                }
                this.Controls.Add(iconBox);
            }

            // 6. Message Label
            int textLeft = (icon == CustomMessageBoxIcon.None) ? 25 : 70;
            Label lblMessage = new Label
            {
                Text = message,
                ForeColor = ColorTranslator.FromHtml("#333333"),
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                Location = new Point(textLeft, 75),
                Size = new Size(this.Width - textLeft - 25, 60)
            };
            this.Controls.Add(lblMessage);

            // 7. Buttons
            int btnY = 150;
            int btnWidth = 100;
            int btnHeight = 35;
            int rightMargin = 25;

            if (buttons == CustomMessageBoxButtons.OK)
            {
                Button btnOk = CreateStyledButton("OK", this.Width - btnWidth - rightMargin, btnY, btnWidth, btnHeight);
                btnOk.Click += (s, e) => { this.Result = CustomMessageBoxResult.OK; this.Close(); };
                this.Controls.Add(btnOk);
                this.AcceptButton = btnOk;
            }
            else if (buttons == CustomMessageBoxButtons.YesNo)
            {
                Button btnNo = CreateStyledButton("No", this.Width - btnWidth - rightMargin, btnY, btnWidth, btnHeight);
                btnNo.BackColor = Color.FromArgb(200, 200, 200); // Gray for No
                btnNo.ForeColor = Color.Black;
                btnNo.Click += (s, e) => { this.Result = CustomMessageBoxResult.No; this.Close(); };
                this.Controls.Add(btnNo);

                Button btnYes = CreateStyledButton("Yes", this.Width - (btnWidth * 2) - rightMargin - 10, btnY, btnWidth, btnHeight);
                btnYes.Click += (s, e) => { this.Result = CustomMessageBoxResult.Yes; this.Close(); };
                this.Controls.Add(btnYes);
                this.AcceptButton = btnYes;
            }
        }

        private Button CreateStyledButton(string text, int x, int y, int width, int height)
        {
            Button btn = new Button
            {
                Text = text,
                BackColor = PrimaryButtonColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Size = new Size(width, height),
                Location = new Point(x, y),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;

            // Rounded corners
            btn.Paint += (s, e) =>
            {
                GraphicsPath path = new GraphicsPath();
                int radius = 10;
                path.AddArc(0, 0, radius, radius, 180, 90);
                path.AddArc(btn.Width - radius, 0, radius, radius, 270, 90);
                path.AddArc(btn.Width - radius, btn.Height - radius, radius, radius, 0, 90);
                path.AddArc(0, btn.Height - radius, radius, radius, 90, 90);
                btn.Region = new Region(path);
            };

            return btn;
        }
    }
}