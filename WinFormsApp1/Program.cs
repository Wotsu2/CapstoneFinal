using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Windows.Forms;

namespace WinFormsApp1
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            if (!IsRunAsAdmin())
            {
                try
                {
                    ProcessStartInfo proc = new ProcessStartInfo
                    {
                        UseShellExecute = true,
                        WorkingDirectory = Environment.CurrentDirectory,
                        FileName = Application.ExecutablePath,
                        Verb = "runas"
                    };
                    Process.Start(proc);
                }
                catch
                {
                    MessageBox.Show("This app requires Administrator privileges.");
                }
                return;
            }

            // ✅ DEBUG: Confirm we're admin
            MessageBox.Show(
                "✅ Running as ADMINISTRATOR",
                "Startup Check",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            ApplicationConfiguration.Initialize();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            SettingsManager.Load();

            using (SplashForm splash = new SplashForm())
            {
                splash.ShowDialog();
            }

            Application.Run(new Login());
        }

        // =========================================================
        // HELPER — Checks if current process has admin privileges
        // =========================================================
        private static bool IsRunAsAdmin()
        {
            try
            {
                WindowsIdentity id = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(id);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }
    }
}