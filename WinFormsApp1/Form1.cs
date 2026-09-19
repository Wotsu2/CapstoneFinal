using Microsoft.VisualBasic;
using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;
using TheArtOfDevHtmlRenderer.Adapters;
namespace WinFormsApp1
{
    public partial class Form1 : Form
    {


        public Form1()
        {
            InitializeComponent();
            CreateButton();
        }

        private void CreateButton()
        {
            Guna.UI2.WinForms.Guna2Button ActivityButton = new Guna.UI2.WinForms.Guna2Button();
            ActivityButton.Height = 180;
            ActivityButton.Width = 180;
            ActivityButton.Margin = new Padding(5);
            ActivityButton.BorderColor = Color.Black;
            ActivityButton.BorderRadius = 10;

            Label DueDate = new Label();
            DueDate.Text = "Due: August 9 2026";
            DueDate.ForeColor = Color.Black;
            DueDate.BackColor = Color.DarkViolet;
            DueDate.Width = 150;
            DueDate.Location = new Point(55, 0);
            ActivityButton.Controls.Add(DueDate);

            Label Title = new Label();
            Title.Text = "Class Name";
            Title.ForeColor = Color.Black;
            Title.BackColor = Color.Transparent;
            Title.Location = new Point(20, 50);
            ActivityButton.Controls.Add(Title);

            Label Status = new Label();
            Status.Text = "Laboratoryt Activity";
            Status.ForeColor = Color.Black;
            Status.BackColor = Color.Transparent;
            Status.Width = 150;
            Status.Location = new Point(40, 90);
            ActivityButton.Controls.Add(Status);

            Label ViewActivity = new Label();
            ViewActivity.Text = "View Activity >";
            ViewActivity.ForeColor = Color.Maroon;
            ViewActivity.BackColor = Color.Transparent;
            ViewActivity.Location = new Point(80, 150);
            ActivityButton.Controls.Add(ViewActivity);

            this.Controls.Add(ActivityButton);
        }

    }
}
