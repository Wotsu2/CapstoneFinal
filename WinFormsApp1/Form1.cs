using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Data.SqlClient; // For SQL Server. Use MySql.Data.MySqlClient for MySQL, etc.
using System.Windows.Forms;
using TheArtOfDevHtmlRenderer.Adapters;
namespace WinFormsApp1
{
    public partial class Form1 : Form
    {

        public Form1()
        {
            InitializeComponent();
            // LoadUsers();
        }

        private void UserButton_Click(object sender, EventArgs e)
        {
            Form2 form2 = new Form2();
            form2.Show();
            this.Hide();
        }
    }
}
