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

            Application.Run(new ProfessorQuizForm());


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

            
            Application.Run(new ProfessorQuizForm());


        }
    }
}

