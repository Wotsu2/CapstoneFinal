namespace WinFormsApp1
{
    partial class SplashForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Timer animationTimer;
        private System.Windows.Forms.Button enterButton;

        protected override void Dispose(bool disposing)
        {
            if (disposing &&
                components != null)
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            components =
                new System.ComponentModel.Container();

            animationTimer =
                new System.Windows.Forms.Timer(
                    components
                );

            enterButton =
                new System.Windows.Forms.Button();

            SuspendLayout();

            // =========================================================
            // animationTimer
            // =========================================================

            animationTimer.Interval = 16;

            animationTimer.Tick +=
                animationTimer_Tick;

            // =========================================================
            // enterButton
            // =========================================================

            enterButton.BackColor =
                System.Drawing.Color.FromArgb(
                    42,
                    3,
                    3
                );

            enterButton.FlatAppearance.BorderColor =
                System.Drawing.Color.FromArgb(
                    205,
                    35,
                    35
                );

            enterButton.FlatAppearance.BorderSize =
                1;

            enterButton.FlatAppearance.MouseDownBackColor =
                System.Drawing.Color.FromArgb(
                    105,
                    15,
                    15
                );

            enterButton.FlatAppearance.MouseOverBackColor =
                System.Drawing.Color.FromArgb(
                    72,
                    10,
                    10
                );

            enterButton.FlatStyle =
                System.Windows.Forms.FlatStyle.Flat;

            enterButton.Font =
                new System.Drawing.Font(
                    "Segoe UI Semibold",
                    12F,
                    System.Drawing.FontStyle.Regular,
                    System.Drawing.GraphicsUnit.Point
                );

            enterButton.ForeColor =
                System.Drawing.Color.White;

            enterButton.Name =
                "enterButton";

            enterButton.Size =
                new System.Drawing.Size(
                    190,
                    50
                );

            enterButton.TabIndex =
                0;

            enterButton.Text =
                "ENTER SYSTEM";

            enterButton.UseVisualStyleBackColor =
                false;

            enterButton.Visible =
                false;

            enterButton.Enabled =
                false;

            enterButton.Cursor =
                System.Windows.Forms.Cursors.Hand;

            enterButton.Click +=
                enterButton_Click;

            // =========================================================
            // SplashForm
            // =========================================================

            AutoScaleDimensions =
                new System.Drawing.SizeF(
                    7F,
                    15F
                );

            AutoScaleMode =
                System.Windows.Forms.AutoScaleMode.Font;

            BackColor =
                System.Drawing.Color.Black;

            ClientSize =
                new System.Drawing.Size(
                    1280,
                    720
                );

            Controls.Add(
                enterButton
            );

            FormBorderStyle =
                System.Windows.Forms.FormBorderStyle.None;

            KeyPreview =
                true;

            MaximizeBox =
                false;

            MinimizeBox =
                false;

            Name =
                "SplashForm";

            ShowIcon =
                false;

            ShowInTaskbar =
                false;

            StartPosition =
                System.Windows.Forms.FormStartPosition.CenterScreen;

            Text =
                "CDSGA Hub";

            Load +=
                SplashForm_Load;

            Resize +=
                SplashForm_Resize;

            FormClosed +=
                SplashForm_FormClosed;

            ResumeLayout(false);
        }

        #endregion
    }
}