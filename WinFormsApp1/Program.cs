using System;
using System.Threading.Tasks;
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

            // ---- Show splash non-modally ----
            SplashForm splash = new SplashForm();
            splash.Show();
            splash.Refresh();

            DateTime start = DateTime.Now;
            while ((DateTime.Now - start).TotalMilliseconds < 2500)
            {
                Application.DoEvents();
                System.Threading.Thread.Sleep(20);
            }

            splash.Close();
            splash.Dispose();

            // ---- Then show Login once ----
            var context = new ApplicationContext();
            Application.Run(context);

            var login = new Login();
            login.Show();
        }
    }
}