namespace WinFormsApp1
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges1 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges2 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges3 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges4 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            SelectFileBtn = new Guna.UI2.WinForms.Guna2Button();
            SubmitBtn = new Guna.UI2.WinForms.Guna2Button();
            lblFileName = new Label();
            SuspendLayout();
            // 
            // SelectFileBtn
            // 
            SelectFileBtn.CustomizableEdges = customizableEdges1;
            SelectFileBtn.DisabledState.BorderColor = Color.DarkGray;
            SelectFileBtn.DisabledState.CustomBorderColor = Color.DarkGray;
            SelectFileBtn.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
            SelectFileBtn.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
            SelectFileBtn.Font = new Font("Segoe UI", 9F);
            SelectFileBtn.ForeColor = Color.White;
            SelectFileBtn.Location = new Point(490, 142);
            SelectFileBtn.Name = "SelectFileBtn";
            SelectFileBtn.ShadowDecoration.CustomizableEdges = customizableEdges2;
            SelectFileBtn.Size = new Size(180, 45);
            SelectFileBtn.TabIndex = 0;
            SelectFileBtn.Text = "Select File";
            SelectFileBtn.Click += SelectFileBtn_Click;
            // 
            // SubmitBtn
            // 
            SubmitBtn.CustomizableEdges = customizableEdges3;
            SubmitBtn.DisabledState.BorderColor = Color.DarkGray;
            SubmitBtn.DisabledState.CustomBorderColor = Color.DarkGray;
            SubmitBtn.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
            SubmitBtn.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
            SubmitBtn.Font = new Font("Segoe UI", 9F);
            SubmitBtn.ForeColor = Color.White;
            SubmitBtn.Location = new Point(490, 223);
            SubmitBtn.Name = "SubmitBtn";
            SubmitBtn.ShadowDecoration.CustomizableEdges = customizableEdges4;
            SubmitBtn.Size = new Size(180, 45);
            SubmitBtn.TabIndex = 1;
            SubmitBtn.Text = "Submit File";
            SubmitBtn.Click += SubmitBtn_Click;
            // 
            // lblFileName
            // 
            lblFileName.AutoSize = true;
            lblFileName.Location = new Point(550, 91);
            lblFileName.Name = "lblFileName";                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                             
            lblFileName.Size = new Size(55, 15);
            lblFileName.TabIndex = 2;
            lblFileName.Text = "Filename";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            AutoScroll = true;
            ClientSize = new Size(1370, 749);
            Controls.Add(lblFileName);
            Controls.Add(SubmitBtn);
            Controls.Add(SelectFileBtn);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Form1";
            WindowState = FormWindowState.Maximized;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Guna.UI2.WinForms.Guna2Button SelectFileBtn;
        private Guna.UI2.WinForms.Guna2Button SubmitBtn;
        private Label lblFileName;
    }
}
