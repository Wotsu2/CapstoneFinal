using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using static System.Net.WebRequestMethods;

namespace WinFormsApp1
{
    public partial class Nothing : Form
    {
        private serverConnection fileServer;
        public Nothing()
        {
            InitializeComponent();
            fileServer = new serverConnection(5001);
            fileServer.Start();
        }
        
    }
}
