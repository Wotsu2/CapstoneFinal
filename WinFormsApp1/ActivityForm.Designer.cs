namespace WinFormsApp1
{
    partial class ActivityForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges1 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges2 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges3 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges4 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges5 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges6 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            lblActivityTitle = new Label();
            lblActivityDueDate = new Label();
            guna2Panel1 = new Guna.UI2.WinForms.Guna2Panel();
            lblActivityStatus = new Label();
            label4 = new Label();
            lblActivityDescription = new Label();
            btnUploadActivity = new Guna.UI2.WinForms.Guna2Button();
            btnPostActivity = new Guna.UI2.WinForms.Guna2Button();
            guna2Panel1.SuspendLayout();
            SuspendLayout();
            // 
            // lblActivityTitle
            // 
            lblActivityTitle.AutoSize = true;
            lblActivityTitle.Font = new Font("Segoe UI Semibold", 14.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblActivityTitle.Location = new Point(25, 34);
            lblActivityTitle.Name = "lblActivityTitle";
            lblActivityTitle.Size = new Size(232, 25);
            lblActivityTitle.TabIndex = 0;
            lblActivityTitle.Text = "Hotel Reservation System";
            // 
            // lblActivityDueDate
            // 
            lblActivityDueDate.AutoSize = true;
            lblActivityDueDate.Location = new Point(359, 42);
            lblActivityDueDate.Name = "lblActivityDueDate";
            lblActivityDueDate.Size = new Size(140, 15);
            lblActivityDueDate.TabIndex = 1;
            lblActivityDueDate.Text = "Due: 09/02/2026 11:59PM";
            // 
            // guna2Panel1
            // 
            guna2Panel1.BackColor = Color.Transparent;
            guna2Panel1.BorderRadius = 20;
            guna2Panel1.Controls.Add(lblActivityStatus);
            guna2Panel1.CustomizableEdges = customizableEdges1;
            guna2Panel1.FillColor = Color.FromArgb(184, 186, 66);
            guna2Panel1.Location = new Point(623, 34);
            guna2Panel1.Name = "guna2Panel1";
            guna2Panel1.ShadowDecoration.CustomizableEdges = customizableEdges2;
            guna2Panel1.Size = new Size(105, 38);
            guna2Panel1.TabIndex = 2;
            // 
            // lblActivityStatus
            // 
            lblActivityStatus.AutoSize = true;
            lblActivityStatus.Font = new Font("Bahnschrift SemiBold", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblActivityStatus.ForeColor = Color.White;
            lblActivityStatus.Location = new Point(28, 11);
            lblActivityStatus.Name = "lblActivityStatus";
            lblActivityStatus.Size = new Size(52, 16);
            lblActivityStatus.TabIndex = 3;
            lblActivityStatus.Text = "Pending";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Font = new Font("Segoe UI Semibold", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label4.Image = Properties.Resources.Activity_Description;
            label4.ImageAlign = ContentAlignment.MiddleLeft;
            label4.Location = new Point(35, 132);
            label4.Name = "label4";
            label4.Size = new Size(153, 17);
            label4.TabIndex = 3;
            label4.Text = "       Activity Description";
            label4.TextAlign = ContentAlignment.MiddleRight;
            // 
            // lblActivityDescription
            // 
            lblActivityDescription.AutoSize = true;
            lblActivityDescription.Font = new Font("Segoe UI Semibold", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblActivityDescription.ImageAlign = ContentAlignment.MiddleLeft;
            lblActivityDescription.Location = new Point(58, 165);
            lblActivityDescription.Name = "lblActivityDescription";
            lblActivityDescription.Size = new Size(141, 17);
            lblActivityDescription.TabIndex = 4;
            lblActivityDescription.Text = "Description of Activity";
            lblActivityDescription.TextAlign = ContentAlignment.MiddleRight;
            // 
            // btnUploadActivity
            // 
            btnUploadActivity.BorderRadius = 20;
            btnUploadActivity.BorderThickness = 1;
            btnUploadActivity.CustomizableEdges = customizableEdges3;
            btnUploadActivity.DisabledState.BorderColor = Color.DarkGray;
            btnUploadActivity.DisabledState.CustomBorderColor = Color.DarkGray;
            btnUploadActivity.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
            btnUploadActivity.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
            btnUploadActivity.FillColor = Color.FromArgb(217, 217, 217);
            btnUploadActivity.Font = new Font("Segoe UI", 9F);
            btnUploadActivity.ForeColor = Color.Black;
            btnUploadActivity.Image = Properties.Resources.Upload;
            btnUploadActivity.ImageOffset = new Point(45, -30);
            btnUploadActivity.ImageSize = new Size(25, 23);
            btnUploadActivity.Location = new Point(12, 810);
            btnUploadActivity.Name = "btnUploadActivity";
            btnUploadActivity.ShadowDecoration.CustomizableEdges = customizableEdges4;
            btnUploadActivity.Size = new Size(800, 170);
            btnUploadActivity.TabIndex = 5;
            btnUploadActivity.Text = "Click to upload your file or \r\ndrag and drop your file here";
            btnUploadActivity.TextOffset = new Point(0, 10);
            btnUploadActivity.Click += btnUploadActivity_Click;
            // 
            // btnPostActivity
            // 
            btnPostActivity.BorderRadius = 15;
            btnPostActivity.BorderThickness = 1;
            btnPostActivity.CustomizableEdges = customizableEdges5;
            btnPostActivity.DisabledState.BorderColor = Color.DarkGray;
            btnPostActivity.DisabledState.CustomBorderColor = Color.DarkGray;
            btnPostActivity.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
            btnPostActivity.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
            btnPostActivity.FillColor = Color.FromArgb(217, 217, 217);
            btnPostActivity.Font = new Font("Segoe UI", 9F);
            btnPostActivity.ForeColor = Color.Black;
            btnPostActivity.ImageSize = new Size(25, 23);
            btnPostActivity.Location = new Point(735, 1016);
            btnPostActivity.Name = "btnPostActivity";
            btnPostActivity.ShadowDecoration.CustomizableEdges = customizableEdges6;
            btnPostActivity.Size = new Size(77, 33);
            btnPostActivity.TabIndex = 6;
            btnPostActivity.Text = "Submit";
            btnPostActivity.Click += btnPostActivity_Click;
            // 
            // ActivityForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            AutoScroll = true;
            BackColor = Color.White;
            ClientSize = new Size(824, 1061);
            Controls.Add(btnPostActivity);
            Controls.Add(btnUploadActivity);
            Controls.Add(lblActivityDescription);
            Controls.Add(label4);
            Controls.Add(guna2Panel1);
            Controls.Add(lblActivityDueDate);
            Controls.Add(lblActivityTitle);
            Name = "ActivityForm";
            Text = "ActivityForm";
            guna2Panel1.ResumeLayout(false);
            guna2Panel1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lblActivityTitle;
        private Label lblActivityDueDate;
        private Guna.UI2.WinForms.Guna2Panel guna2Panel1;
        private Label lblActivityStatus;
        private Label label4;
        private Label lblActivityDescription;
        private Guna.UI2.WinForms.Guna2Button btnUploadActivity;
        private Guna.UI2.WinForms.Guna2Button btnPostActivity;
    }
}