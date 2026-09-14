using System.Runtime.Serialization;

namespace WinFormsApp1
{
    internal static class Program
    {
        [System.STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            AppContext.SetSwitch("System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization", false);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            //using (SplashForm splash =
            //       new SplashForm())
            //{
            //    splash.ShowDialog();

            //    if (splash.DialogResult !=
            //        DialogResult.OK)
            //    {
            //        return;
            //    }
            //}


            // Simulan sa Login form — ito ang tamang starting point ng app

            Bitmap studentreferencesPhoto = new Bitmap("StudentPhotos/student01.jpg");

            string photoPath = Path.Combine(
    Application.StartupPath,
    "StudentPhotos",
    "student01.jpg"
);
            Application.Run(new LivenessCheckForm(studentreferencesPhoto));

        }
    }
}