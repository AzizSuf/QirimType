namespace QirimType;

static class Program
{
    private const string AppMutexName = @"Global\QirimType_SingleInstance_Mutex";

    [STAThread]
    static void Main()
    {
        // Ensure single instance to prevent duplicate keyboard hooks
        using var mutex = new Mutex(true, AppMutexName, out bool isOnlyInstance);
        if (!isOnlyInstance)
        {
            MessageBox.Show(
                "QırımType уже запущен и находится в системном трее (возле часов).",
                "QırımType",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        // Configure application DPI awareness and visual styles
        ApplicationConfiguration.Initialize();

        // Run as background tray application context
        Application.Run(new TrayApplicationContext());
    }
}