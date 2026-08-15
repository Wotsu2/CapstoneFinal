using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace WinFormsApp1
{
    public partial class admindash : Form
    {
        // DASHBOARD ATTRIBUTES //
        private Panel PanelIndicator;

        // USER MANAGEMENT ATTRIBUTES //
        private LoadUser loadUser;
        // FILE MANAGEMENT ATTRIBUTES //
        private ServerFolder serverFolder;
        private string saveFolder = @"C:\ReceivedFileFolder";

        // WORKSTATION ATTRIBUTES //
        private WorkStationFunctions workStationFunctions;
        private ScreenSharing screenSharing;
        public string selectedWorkstationId = "";
        public admindash()
        {
            InitializeComponent();
        }

        private void admindash_Load(object sender, EventArgs e)
        {
            lblTotalUsers.Text = DashboardTotalUsers.TotalUsers().ToString();

            // USER MANAGEMENT //
            loadUser = new LoadUser(this);
            loadUser.LoadUserData();

            // FILE MANAGEMENT //
            serverFolder = new ServerFolder(this);
            serverFolder.lsServerFolderSetup();
            serverFolder.LoadServerFolder(saveFolder, addToHistory: false);

            // WORKSTATION ATTRIBUTES //
            workStationFunctions = new WorkStationFunctions(this);
            workStationFunctions.StartServer();

            screenSharing = new ScreenSharing(this);
            screenSharing.StartScreenListener();
        }

        private void btnDashboard_Click_1(object sender, EventArgs e)
        {
            panelDashoard.Visible = true;
            pnlUserManagement.Visible = false;
            pnlFileManagement.Visible = false;
            pnlWorkstation.Visible = false;

            navbarStyle.RemoveIndicator(PanelIndicator);
            PanelIndicator = navbarStyle.CreateIndicator(btnDashboard);
        }

        private void btnUserManagement_Click(object sender, EventArgs e)
        {
            pnlUserManagement.Visible = true;
            panelDashoard.Visible = false;
            pnlFileManagement.Visible = false;
            pnlWorkstation.Visible = false;

            navbarStyle.RemoveIndicator(PanelIndicator);
            PanelIndicator = navbarStyle.CreateIndicator(btnUserManagement);

            loadUser.LoadUserData();
        }

        private void btnFileManagement_Click(object sender, EventArgs e)
        {
            pnlFileManagement.Visible = true;
            panelDashoard.Visible = false;
            pnlUserManagement.Visible = false;
            pnlWorkstation.Visible = false;

            navbarStyle.RemoveIndicator(PanelIndicator);
            PanelIndicator = navbarStyle.CreateIndicator(btnFileManagement);

            serverFolder.lsServerFolderSetup();
            serverFolder.LoadServerFolder(saveFolder, addToHistory: false);
        }

        private void btnWorkstation_Click(object sender, EventArgs e)
        {
            pnlWorkstation.Visible = true;
            panelDashoard.Visible = false;
            pnlUserManagement.Visible = false;
            pnlFileManagement.Visible = false;

            navbarStyle.RemoveIndicator(PanelIndicator);
            PanelIndicator = navbarStyle.CreateIndicator(btnWorkstation);
        }

        private void CreateButton_Click(object sender, EventArgs e)
        {
            CreateAccount createAccount = new CreateAccount(this);
            createAccount.CreateUser();
        }

        private void ContextRoleText_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (ContextRoleText.Text == "Professor")
            {
                ContextYearText.Enabled = false;
                ContextSectionText.Enabled = false;
                ContextCourseText.Enabled = false;
            }
            else if (ContextRoleText.Text == "Student")
            {
                ContextYearText.Enabled = true;
                ContextSectionText.Enabled = true;
                ContextCourseText.Enabled = true;
            }
            else if (ContextCourseText.Text == "Admin")
            {
                ContextYearText.Enabled = false;
                ContextSectionText.Enabled = false;
                ContextCourseText.Enabled = false;
            }
        }

        private void AccountCreateButton_Click(object sender, EventArgs e)
        {
            pnlCreateAccount.Visible = true;
            pnlUserList.Visible = false;
        }

        private void UserListButton_Click(object sender, EventArgs e)
        {
            pnlUserList.Visible = true;
            pnlCreateAccount.Visible = false;
        }

        private void SearchButton_TextChanged(object sender, EventArgs e)
        {
            loadUser.LoadUserData(SearchButton.Text);
        }

        private void lvServerFolder_DoubleClick(object sender, EventArgs e)
        {
            serverFolder.doubleClick();
        }

        private void BtnBack_Click(object sender, EventArgs e)
        {
            serverFolder.btnBack();
        }

        // WORKSTATION FUNCTIONS //
        public void WorkstationButton_Click(object sender, EventArgs e)
        {
            Button MainPcButton = (Button)sender;
            string workstationId = MainPcButton.Tag.ToString();

            selectedWorkstationId = workstationId;
        }
    }
}
