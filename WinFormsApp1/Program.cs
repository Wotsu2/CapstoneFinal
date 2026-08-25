namespace WinFormsApp1
{
    internal static class Program
    {
        [System.STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

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

            // After SplashForm:
            // open your existing login form.
            Application.Run(new ProfessorForm());
        }
    }
}