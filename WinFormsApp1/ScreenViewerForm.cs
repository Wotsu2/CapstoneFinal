using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace WinFormsApp1
{
    public partial class ScreenViewerForm : Form
    {
        public string WorkstationId { get; private set; }
        private Chart chartCpu;
        public string selectedWorkstationId = "";

        public ScreenViewerForm(string workstationId)
        {
            InitializeComponent();
            WorkstationId = workstationId;
            this.Text = "Viewing: " + workstationId;
        }
        
        public PictureBox GetPictureBox() => pictureBoxScreen;

    }
}
