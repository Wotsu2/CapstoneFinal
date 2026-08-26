using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace WinFormsApp1
{
    public partial class StudentForm : Form
    {
        public StudentForm()
        {
            InitializeComponent();
        }

        private void btnHome_Click(object sender, EventArgs e)
        {
            pnlHome.BringToFront();
        }

        private void btnActivities_Click(object sender, EventArgs e)
        {
            pnlActivity.BringToFront();
        }

        private void btnSubject_Click(object sender, EventArgs e)
        {
            pnlSubject.BringToFront();
        }

        private void btnGrades_Click(object sender, EventArgs e)
        {
            pnlGrades.BringToFront();
        }

        private void btnFile_Click(object sender, EventArgs e)
        {
            pnlFile.BringToFront();
        }

        private void btnApps_Click(object sender, EventArgs e)
        {

        }
    }
}
