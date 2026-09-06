using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace WinFormsApp1
{
    public partial class BroadcastViewerForm : Form
    {
        public BroadcastViewerForm()
        {
            InitializeComponent();
            this.WindowState = FormWindowState.Maximized;
            this.FormBorderStyle = FormBorderStyle.None; // fullscreen, no close button — prevents students dismissing it
            this.TopMost = true;
        }
        public PictureBox GetPictureBox() => pictureBoxBroadcast;
    }
}
