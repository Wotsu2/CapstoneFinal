namespace WinFormsApp1
{
    partial class BroadcastViewerForm
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
            pictureBoxBroadcast = new PictureBox();
            ((System.ComponentModel.ISupportInitialize)pictureBoxBroadcast).BeginInit();
            SuspendLayout();
            // 
            // pictureBoxBroadcast
            // 
            pictureBoxBroadcast.Dock = DockStyle.Fill;
            pictureBoxBroadcast.Location = new Point(0, 0);
            pictureBoxBroadcast.Name = "pictureBoxBroadcast";
            pictureBoxBroadcast.Size = new Size(1385, 792);
            pictureBoxBroadcast.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBoxBroadcast.TabIndex = 0;
            pictureBoxBroadcast.TabStop = false;
            // 
            // BroadcastViewerForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1385, 792);
            Controls.Add(pictureBoxBroadcast);
            Name = "BroadcastViewerForm";
            Text = "BroadcastViewerForm";
            ((System.ComponentModel.ISupportInitialize)pictureBoxBroadcast).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private PictureBox pictureBoxBroadcast;
    }
}