using Naghma.Core;

namespace Naghma;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        using var mutex = new Mutex(true, "Naghma.SingleInstance.v1", out var isNew);
        if (!isNew)
        {
            MessageBox.Show("Naghma is already running.", "Naghma", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();
        Paths.EnsureDirs();

        var settings = AppSettings.Load();
        Loc.Load(settings.Language);
        License.Init(settings.LicenseKey);

        var form = new MainForm(settings);

        Application.Run(form);
    }
}
