namespace WinFormsApp1
{
    partial class Nothing2
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
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges3 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges4 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            dtpDueDate = new Guna.UI2.WinForms.Guna2DateTimePicker();
            txtScore = new TextBox();
            label3 = new Label();
            label2 = new Label();
            txtDescription = new TextBox();
            label1 = new Label();
            lblFilename = new Label();
            btnSelectFile = new Button();
            txtTitle = new TextBox();
            btnSubmit = new Button();
            SuspendLayout();
            // 
            // dtpDueDate
            // 
            dtpDueDate.Checked = true;
            dtpDueDate.CustomizableEdges = customizableEdges3;
            dtpDueDate.Font = new Font("Segoe UI", 9F);
            dtpDueDate.Format = DateTimePickerFormat.Long;
            dtpDueDate.Location = new Point(206, 415);
            dtpDueDate.MaxDate = new DateTime(9998, 12, 31, 0, 0, 0, 0);
            dtpDueDate.MinDate = new DateTime(1753, 1, 1, 0, 0, 0, 0);
            dtpDueDate.Name = "dtpDueDate";
            dtpDueDate.ShadowDecoration.CustomizableEdges = customizableEdges4;
            dtpDueDate.Size = new Size(227, 36);
            dtpDueDate.TabIndex = 30;
            dtpDueDate.Value = new DateTime(2026, 8, 20, 1, 8, 24, 72);
            // 
            // txtScore
            // 
            txtScore.Location = new Point(206, 216);
            txtScore.Name = "txtScore";
            txtScore.Size = new Size(227, 23);
            txtScore.TabIndex = 29;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(206, 198);
            label3.Name = "label3";
            label3.Size = new Size(36, 15);
            label3.TabIndex = 28;
            label3.Text = "Score";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(206, 104);
            label2.Name = "label2";
            label2.Size = new Size(67, 15);
            label2.TabIndex = 27;
            label2.Text = "Description";
            // 
            // txtDescription
            // 
            txtDescription.Location = new Point(206, 122);
            txtDescription.Multiline = true;
            txtDescription.Name = "txtDescription";
            txtDescription.Size = new Size(227, 61);
            txtDescription.TabIndex = 26;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(206, 46);
            label1.Name = "label1";
            label1.Size = new Size(30, 15);
            label1.TabIndex = 25;
            label1.Text = "Title";
            // 
            // lblFilename
            // 
            lblFilename.AutoSize = true;
            lblFilename.Location = new Point(288, 282);
            lblFilename.Name = "lblFilename";
            lblFilename.Size = new Size(60, 15);
            lblFilename.TabIndex = 24;
            lblFilename.Text = "File Name";
            // 
            // btnSelectFile
            // 
            btnSelectFile.AllowDrop = true;
            btnSelectFile.Location = new Point(206, 334);
            btnSelectFile.Name = "btnSelectFile";
            btnSelectFile.Size = new Size(227, 43);
            btnSelectFile.TabIndex = 23;
            btnSelectFile.Text = "Select File";
            btnSelectFile.UseVisualStyleBackColor = true;
            // 
            // txtTitle
            // 
            txtTitle.Location = new Point(206, 64);
            txtTitle.Name = "txtTitle";
            txtTitle.Size = new Size(227, 23);
            txtTitle.TabIndex = 22;
            // 
            // btnSubmit
            // 
            btnSubmit.Location = new Point(467, 592);
            btnSubmit.Name = "btnSubmit";
            btnSubmit.Size = new Size(131, 45);
            btnSubmit.TabIndex = 21;
            btnSubmit.Text = "Submit";
            btnSubmit.UseVisualStyleBackColor = true;
            // 
            // Nothing2
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(805, 683);
            Controls.Add(dtpDueDate);
            Controls.Add(txtScore);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(txtDescription);
            Controls.Add(label1);
            Controls.Add(lblFilename);
            Controls.Add(btnSelectFile);
            Controls.Add(txtTitle);
            Controls.Add(btnSubmit);
            Name = "Nothing2";
            Text = "Nothing2";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Guna.UI2.WinForms.Guna2DateTimePicker dtpDueDate;
        private TextBox txtScore;
        private Label label3;
        private Label label2;
        private TextBox txtDescription;
        private Label label1;
        private Label lblFilename;
        private Button btnSelectFile;
        private TextBox txtTitle;
        private Button btnSubmit;
    }
}