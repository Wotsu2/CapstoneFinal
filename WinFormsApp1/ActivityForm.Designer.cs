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
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges7 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges8 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges9 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges10 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges11 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges12 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ActivityForm));
            lblActivityTitle = new Label();
            lblActivityDueDate = new Label();
            guna2Panel1 = new Guna.UI2.WinForms.Guna2Panel();
            lblActivityStatus = new Label();
            label4 = new Label();
            lblActivityDescription = new Label();
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
            guna2Panel1.CustomizableEdges = customizableEdges7;
            guna2Panel1.FillColor = Color.FromArgb(184, 186, 66);
            guna2Panel1.Location = new Point(623, 34);
            guna2Panel1.Name = "guna2Panel1";
            guna2Panel1.ShadowDecoration.CustomizableEdges = customizableEdges8;
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
            // btnPostActivity
            // 
            btnPostActivity.BorderRadius = 15;
            btnPostActivity.BorderThickness = 1;
            btnPostActivity.CustomizableEdges = customizableEdges11;
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
            btnPostActivity.ShadowDecoration.CustomizableEdges = customizableEdges12;
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
            Controls.Add(lblActivityDescription);
            Controls.Add(label4);
            Controls.Add(guna2Panel1);
            Controls.Add(lblActivityDueDate);
            Controls.Add(lblActivityTitle);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "ActivityForm";
            Text = "Activities";
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