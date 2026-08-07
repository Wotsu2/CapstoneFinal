using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace WinFormsApp1
{
    public partial class ScreenViewerForm : Form
    {
        public string WorkstationId { get; private set; }

        public ScreenViewerForm(string workstationId)
        {
            InitializeComponent();
            WorkstationId = workstationId;
            this.Text = "Viewing: " + workstationId;
        }
        public PictureBox GetPictureBox() => pictureBoxScreen;
    }
}
