using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
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
        private ScreenSharing screenSharing;
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
            pnlHome.Visible = true;
            pnlWorkstation.Visible = false;
            pnlStudent.Visible = false;
        }
        private void btnWorkstation_Click(object sender, EventArgs e)
        {
            pnlWorkstation.Visible = true;
            pnlHome.Visible = false;
            pnlStudent.Visible = false;
        }
        private void btnStudent_Click(object sender, EventArgs e)
        {
            pnlStudent.Visible = true;
            pnlHome.Visible = false;
            pnlWorkstation.Visible = false;
        }
        private void btnActivities_Click(object sender, EventArgs e)
        {

        }
        private void btnGrades_Click(object sender, EventArgs e)
        {

        }

        public void WorkstationButton_Click(object sender, EventArgs e)
        {
            Button clickedButton = (Button)sender;
            string workstationId = clickedButton.Tag.ToString();
            selectedWorkstationId = workstationId;
            screenSharing.AddScreenViewer(workstationId);
        }

        private void guna2ComboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}
