namespace NetScanAnalyzer;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        Application.ThreadException += (_, e) =>
        {
            LogCrash(e.Exception);
        };
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            LogCrash(e.ExceptionObject as Exception);
        };
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

        try
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Application.Run(new UI.MainForm());
        }
        catch (Exception ex)
        {
            LogCrash(ex);
        }
    }

    static void LogCrash(Exception? ex)
    {
        var msg = ex?.ToString() ?? "Unknown error";
        var crashLog = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash.log");
        File.WriteAllText(crashLog, $"{DateTime.Now}\n{msg}\n");
        Console.Error.WriteLine(msg);
        MessageBox.Show(msg, "NetScan Analyzer — Fatal Error",
            MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
