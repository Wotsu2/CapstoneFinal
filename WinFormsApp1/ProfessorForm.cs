using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace WinFormsApp1
{
    public partial class ProfessorForm : Form
    {

        // WorkStation //
        private ProfMiniWorkStationFunction MiniworkStationFunctions;
        private ProfMainWorkStationFunctions MainworkStationFunctions;
        private string selectedWorkstationId = "";
        public ProfessorForm()
        {
            InitializeComponent();
        }

        private void ProfessorForm_Load(object sender, EventArgs e)
        {
            //DataGridView Desgin//

            // WORKSTATION ATTRIBUTES //
            MiniworkStationFunctions = new ProfMiniWorkStationFunction(this);
            MiniworkStationFunctions.StartServer();

            MainworkStationFunctions = new ProfMainWorkStationFunctions(this);
            MainworkStationFunctions.StartServer();

            //screenSharing = new ScreenSharing(this);
            //screenSharing.StartScreenListener();

        }

        private void btnHome_Click(object sender, EventArgs e)
        {
            pnlHome.BringToFront();
        }
        private void btnWorkstation_Click(object sender, EventArgs e)
        {
            pnlWorkstation.BringToFront();
        }
        private void btnStudent_Click(object sender, EventArgs e)
        {
            pnlStudent.BringToFront();
        }
        private void btnActivities_Click(object sender, EventArgs e)
        {
            pnlActivity.BringToFront();
        }
        private void btnGrades_Click(object sender, EventArgs e)
        {
            pnlGrades.BringToFront();
        }
        private void btnAttendance_Click(object sender, EventArgs e)
        {
            pnlAttendance.BringToFront();
        }

        private void btnSubject_Click(object sender, EventArgs e)
        {
            pnlSubject.BringToFront();
        }

        private void btnFile_Click(object sender, EventArgs e)
        {
            pnlFile.BringToFront();
        }

        public void WorkstationButton_Click(object sender, EventArgs e)
        {
            Button clickedButton = (Button)sender;
            string workstationId = clickedButton.Tag.ToString();
            selectedWorkstationId = workstationId;
        }

        
    }
}
