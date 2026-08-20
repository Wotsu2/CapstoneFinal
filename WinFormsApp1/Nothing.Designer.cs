namespace WinFormsApp1
{
    partial class Nothing
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
            flpActivities = new FlowLayoutPanel();
            SuspendLayout();
            // 
            // flpActivities
            // 
            flpActivities.Location = new Point(334, 242);
            flpActivities.Name = "flpActivities";
            flpActivities.Size = new Size(493, 249);
            flpActivities.TabIndex = 0;
            // 
            // Nothing
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1241, 724);
            Controls.Add(flpActivities);
            Name = "Nothing";
            Text = "Nothing";
            ResumeLayout(false);
        }

        #endregion

        public FlowLayoutPanel flpActivities;
    }
}