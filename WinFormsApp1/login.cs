using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace WinFormsApp1
{
    public partial class Login : Form
    {
        // Temporary hardcoded accounts (for testing lang, wala pang database)
        private readonly Dictionary<string, string> tempAccounts = new Dictionary<string, string>
        {
            { "admin", "admin123" },
            { "student01", "pass123" },{ "student02", "pass123" },
            { "prof01", "prof123" }
        };

        public Login()
        {
            InitializeComponent();
        }

        private void Users_Click(object sender, EventArgs e)
        {

        }

        private void guna2TextBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void label7_Click(object sender, EventArgs e)
        {

        }

        private void guna2PictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void Login_Load(object sender, EventArgs e)
        {

        }

        private void txtIdNumber_TextChanged(object sender, EventArgs e)
        {

        }

        private void txtPassword_TextChanged(object sender, EventArgs e)
        {

        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            string idNumber = txtIdNumber.Text.Trim();
            string password = txtPassword.Text.Trim();

            // Check kung may laman yung fields
            if (string.IsNullOrEmpty(idNumber) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Paki-fill up po ang ID Number at Password.", "Login Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Check kung tama yung ID at password sa temporary accounts
            if (tempAccounts.ContainsKey(idNumber) && tempAccounts[idNumber] == password)
            {
                // Hanapin yung reference photo ng account na ito
                string photoPath = Path.Combine(Application.StartupPath, "StudentPhotos", idNumber + ".jpg");

                if (!File.Exists(photoPath))
                {
                    MessageBox.Show("Walang reference photo para sa account na '" + idNumber +
                        "'. Ilagay ang larawan sa: StudentPhotos\\" + idNumber + ".jpg",
                        "Missing Photo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                Bitmap referencePhoto = new Bitmap(photoPath);
                LivenessCheckForm liveness = new LivenessCheckForm(referencePhoto);
                DialogResult result = liveness.ShowDialog();

                if (result == DialogResult.OK && liveness.VerificationPassed)
                {
                    MessageBox.Show("Login successful! Welcome, " + idNumber);

                    StudentForm studentForm = new StudentForm();
                    studentForm.Show();
                    this.Hide();
                }
                else
                {
                    MessageBox.Show("Face verification failed o hindi na-complete. Subukan ulit.",
                        "Verification Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else
            {
                MessageBox.Show("Mali ang ID Number o Password. Pakisubukan ulit.", "Login Failed",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}