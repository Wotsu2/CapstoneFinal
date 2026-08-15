namespace WinFormsApp1
{
    partial class ScreenViewerForm
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
            pictureBoxScreen = new PictureBox();
            panelPcStatus = new Guna.UI2.WinForms.Guna2ShadowPanel();
            lblComputerName = new Label();
            lblIpAddress = new Label();
            ViewScreenbtn = new Guna.UI2.WinForms.Guna2Button();
            label18 = new Label();
            ((System.ComponentModel.ISupportInitialize)pictureBoxScreen).BeginInit();
            panelPcStatus.SuspendLayout();
            SuspendLayout();
            // 
            // pictureBoxScreen
            // 
            pictureBoxScreen.Location = new Point(0, 0);
            pictureBoxScreen.Name = "pictureBoxScreen";
            pictureBoxScreen.Size = new Size(798, 678);
            pictureBoxScreen.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBoxScreen.TabIndex = 0;
            pictureBoxScreen.TabStop = false;
            // 
            // panelPcStatus
            // 
            panelPcStatus.AllowDrop = true;
            panelPcStatus.BackColor = Color.Transparent;
            panelPcStatus.Controls.Add(lblComputerName);
            panelPcStatus.Controls.Add(lblIpAddress);
            panelPcStatus.Controls.Add(ViewScreenbtn);
            panelPcStatus.Controls.Add(label18);
            panelPcStatus.FillColor = Color.Black;
            panelPcStatus.Location = new Point(804, 0);
            panelPcStatus.Margin = new Padding(3, 2, 3, 2);
            panelPcStatus.Name = "panelPcStatus";
            panelPcStatus.Radius = 15;
            panelPcStatus.ShadowColor = Color.Black;
            panelPcStatus.ShadowStyle = Guna.UI2.WinForms.Guna2ShadowPanel.ShadowMode.Dropped;
            panelPcStatus.Size = new Size(315, 262);
            panelPcStatus.TabIndex = 37;
            // 
            // lblComputerName
            // 
            lblComputerName.AutoSize = true;
            lblComputerName.Font = new Font("Segoe UI", 11F);
            lblComputerName.ForeColor = Color.White;
            lblComputerName.Location = new Point(51, 144);
            lblComputerName.Name = "lblComputerName";
            lblComputerName.Size = new Size(0, 20);
            lblComputerName.TabIndex = 15;
            // 
            // lblIpAddress
            // 
            lblIpAddress.AutoSize = true;
            lblIpAddress.Font = new Font("Segoe UI", 11F);
            lblIpAddress.ForeColor = Color.White;
            lblIpAddress.Location = new Point(51, 120);
            lblIpAddress.Name = "lblIpAddress";
            lblIpAddress.Size = new Size(0, 20);
            lblIpAddress.TabIndex = 14;
            // 
            // ViewScreenbtn
            // 
            ViewScreenbtn.CustomizableEdges = customizableEdges1;
            ViewScreenbtn.DisabledState.BorderColor = Color.DarkGray;
            ViewScreenbtn.DisabledState.CustomBorderColor = Color.DarkGray;
            ViewScreenbtn.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
            ViewScreenbtn.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
            ViewScreenbtn.Font = new Font("Segoe UI", 9F);
            ViewScreenbtn.ForeColor = Color.White;
            ViewScreenbtn.Location = new Point(74, 199);
            ViewScreenbtn.Name = "ViewScreenbtn";
            ViewScreenbtn.ShadowDecoration.CustomizableEdges = customizableEdges2;
            ViewScreenbtn.Size = new Size(180, 31);
            ViewScreenbtn.TabIndex = 13;
            ViewScreenbtn.Text = "View Screen";
            // 
            // label18
            // 
            label18.AutoSize = true;
            label18.BackColor = Color.Transparent;
            label18.FlatStyle = FlatStyle.Flat;
            label18.Font = new Font("Segoe UI", 10F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label18.ForeColor = Color.White;
            label18.Location = new Point(125, 21);
            label18.Name = "label18";
            label18.Size = new Size(69, 19);
            label18.TabIndex = 12;
            label18.Text = "Pc Status";
            // 
            // ScreenViewerForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1131, 678);
            Controls.Add(panelPcStatus);
            Controls.Add(pictureBoxScreen);
            Name = "ScreenViewerForm";
            Text = "ScreenViewerForm";
            ((System.ComponentModel.ISupportInitialize)pictureBoxScreen).EndInit();
            panelPcStatus.ResumeLayout(false);
            panelPcStatus.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private PictureBox pictureBoxScreen;
        private Guna.UI2.WinForms.Guna2ShadowPanel panelPcStatus;
        private Label lblComputerName;
        private Label lblIpAddress;
        private Guna.UI2.WinForms.Guna2Button ViewScreenbtn;
        private Label label18;
    }
}