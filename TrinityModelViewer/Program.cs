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
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (_, e) => ShowFatalError(e.Exception);
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                if (e.ExceptionObject is Exception ex)
                {
                    ShowFatalError(ex);
                }
            };

            try
            {
                Directory.SetCurrentDirectory(AppContext.BaseDirectory);
            }
            catch
            {
                // Ignore; relative shader paths may fail but app can still run.
            }

            GFTool.Renderer.Core.DiagnosticLog.Reset("Application startup");
            GFTool.Renderer.Core.DiagnosticLog.WriteCapabilities();

            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            try
            {
                Application.Run(new ModelViewerForm(args));
            }
            catch (Exception ex)
            {
                ShowFatalError(ex);
            }
        }

        private static void ShowFatalError(Exception ex)
        {
            try
            {
                GFTool.Renderer.Core.DiagnosticLog.WriteException("Fatal error", ex);
                File.WriteAllText(GetCrashLogPath(), ex.ToString());
            }
            catch
            {
                // Ignore logging failures so the user still gets the message box.
            }

            try
            {
                MessageBox.Show(
                    $"Trinity Model Viewer hit an unexpected error:\n\n{ex.Message}\n\nA crash log was written to:\n{GetCrashLogPath()}",
                    "Trinity Model Viewer",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            catch
            {
                // Nothing else to do if WinForms cannot show the dialog.
            }
        }

        private static string GetCrashLogPath()
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "TrinityModelViewer");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "TrinityModelViewer-crash.log");
        }
    }
}
