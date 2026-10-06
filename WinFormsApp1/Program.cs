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
            // =========================================================
            // 1) CHECK FOR ADMIN RIGHTS — if not, relaunch elevated
            // =========================================================
            if (!IsRunAsAdmin())
            {
                try
                {
                    ProcessStartInfo proc = new ProcessStartInfo
                    {
                        UseShellExecute = true,
                        WorkingDirectory = Environment.CurrentDirectory,
                        FileName = Application.ExecutablePath,
                        Verb = "runas" // Triggers UAC prompt
                    };

                    Process.Start(proc);
                }
                catch (Exception)
                {
                    // User clicked "No" on the UAC prompt
                    MessageBox.Show(
                        "This application requires Administrator privileges to run correctly.\n\n" +
                        "Please restart the application and click 'Yes' on the UAC prompt.",
                        "Administrator Required",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }

                // Exit the non-elevated instance
                return;
            }

            // =========================================================
            // 2) NORMAL STARTUP — running as Admin now
            // =========================================================
            ApplicationConfiguration.Initialize();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            SettingsManager.Load();

            // Splash — blocking until 5s animation + fade out finishes
            using (SplashForm splash = new SplashForm())
            {
                splash.ShowDialog();
            }

            // Login — main form of the app
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