using System;
using System.Windows.Forms;

namespace WinFormsApp1
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.EnableVisualStyles();

            SettingsManager.Load();

            Application.SetCompatibleTextRenderingDefault(false);

            var login = new Login();

            // Hold a reference so the app stays alive while Login is hidden
            var context = new ApplicationContext(login);
            Application.Run(context);
        }
    }
}