using System.Text.Json;

namespace Naghma.Core;

public sealed class AppSettings
{
    public string OutputDir { get; set; } = Paths.DefaultOutput;
    public string Language { get; set; } = "";
    public int TypeIndex { get; set; } = 0;
    public int ResultIndex { get; set; } = 1;
    public bool Organize { get; set; }
    public bool Dedupe { get; set; } = true;
    public bool DarkTheme { get; set; }
    public bool Verbose { get; set; }
    public string ProUrl { get; set; } = "https://github.com/Abderrahim-Allaoui/naghma#free-vs-pro";
    public string LicenseKey { get; set; } = "";
    public int WindowWidth { get; set; } = 940;
    public int WindowHeight { get; set; } = 700;

    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(Paths.SettingsFile)) return new AppSettings();
            return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(Paths.SettingsFile)) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save()
    {
        try
        {
            Paths.EnsureDirs();
            File.WriteAllText(Paths.SettingsFile, JsonSerializer.Serialize(this, Json));
        }
        catch
        {
            // settings are best-effort only
        }
    }
}
