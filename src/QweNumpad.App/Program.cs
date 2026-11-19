using QweNumpad.App.Ui;

namespace QweNumpad.App;

internal static class Program
{
    private const string SingleInstanceMutexName = "Global\\QweNumpad.SingleInstance";

    [STAThread]
    private static void Main(string[] args)
    {
        using var mutex = new Mutex(initiallyOwned: true, SingleInstanceMutexName, out var isFirstInstance);

        if (!isFirstInstance)
        {
            MessageBox.Show(
                "QweNumpad zaten çalışıyor. Simgesi saat yanındaki bildirim alanında.",
                "QweNumpad",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();

        var openDiagnostics = args.Any(a =>
            a.Equals("--diagnostics", StringComparison.OrdinalIgnoreCase) ||
            a.Equals("-d", StringComparison.OrdinalIgnoreCase));

        try
        {
            Application.Run(new TrayApplicationContext(openDiagnostics));
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"QweNumpad başlatılamadı:\n\n{ex.Message}",
                "QweNumpad",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}
