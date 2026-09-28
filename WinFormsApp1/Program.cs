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

            // Give the user ~2.5 seconds to see the splash
            DateTime start = DateTime.Now;
            while ((DateTime.Now - start).TotalMilliseconds < 2500)
            {
                Application.DoEvents();      // keep the splash responsive
                System.Threading.Thread.Sleep(20);
            }

            // Close splash
            splash.Close();
            splash.Dispose();

            // ---- Then show Login ----
            var login = new Login();
            var context = new ApplicationContext(login);
            Application.Run(context);
        }
    }
}