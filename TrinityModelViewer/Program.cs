namespace TrinityModelViewer
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                Directory.SetCurrentDirectory(AppContext.BaseDirectory);
            }
            catch
            {
                // Ignore; relative shader paths may fail but app can still run.
            }

            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Application.Run(new ModelViewerForm(args));
        }
    }
}
