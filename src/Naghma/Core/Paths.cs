namespace Naghma.Core;

/// <summary>Well-known folders used by the app. Everything lives under %LOCALAPPDATA%\Naghma.</summary>
public static class Paths
{
    public static string AppDir => AppContext.BaseDirectory;

    public static string DataDir { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Naghma");

    public static string BinDir => Path.Combine(DataDir, "bin");

    public static string SettingsFile => Path.Combine(DataDir, "settings.json");

    public static string CacheFile => Path.Combine(DataDir, "sites.json");

    public static string LocalesDir => Path.Combine(AppDir, "locales");

    public static string DefaultOutput
    {
        get
        {
            // Prefer a dedicated music drive when one is present (keeps C: free).
            const string alt = @"E:\Music";
            if (Directory.Exists(@"E:\")) return alt;
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), "Naghma");
        }
    }

    public static void EnsureDirs()
    {
        Directory.CreateDirectory(DataDir);
        Directory.CreateDirectory(BinDir);
    }
}
