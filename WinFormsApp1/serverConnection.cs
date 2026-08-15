using MySqlX.XDevAPI;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;
namespace WinFormsApp1

{
    internal class serverConnection
    {
        private AdminForm parentForm; // reference to the Form that owns the UI

        public serverConnection(AdminForm form)
        {
            parentForm = form;
        }

    }
}
