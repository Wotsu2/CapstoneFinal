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
            Application.SetCompatibleTextRenderingDefault(false);

            SettingsManager.Load();

            // 1) Splash — blocking hanggang matapos ang 5s animation + fade out
            using (SplashForm splash = new SplashForm())
            {
                splash.ShowDialog();
            }

            // 2) Login — main form ng app
            Application.Run(new Login());
        }
    }
}