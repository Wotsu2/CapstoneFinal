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
            SetupCpuChartControl();

        }
        private void SetupCpuChartControl()
        {
            chartCpu = new Chart();
            chartCpu.Name = "chartCpu";
            chartCpu.Location = new Point(30, 50); // adjust position to fit your layout
            chartCpu.Size = new Size(250, 50);     // adjust size as needed
            chartCpu.BackColor = Color.Gray;       // optional, match your dark theme
            panelPcStatus.Controls.Add(chartCpu);
        }
        
        
        public PictureBox GetPictureBox() => pictureBoxScreen;

    }
}
