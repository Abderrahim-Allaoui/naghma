using System.Text.Json;

namespace Naghma.Core;

/// <summary>Very small JSON-backed localisation layer (en / ar / fr).</summary>
public static class Loc
{
    private static Dictionary<string, string> _map = new(StringComparer.OrdinalIgnoreCase);

    public static string Code { get; private set; } = "en";
    public static bool IsRtl { get; private set; }

    public static readonly string[] Available = { "en", "ar", "fr" };

    public static void Load(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            var c = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLowerInvariant();
            code = Available.Contains(c) ? c : "en";
        }

        Code = code!;
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var candidate in new[] { Code, "en" })
        {
            var path = Path.Combine(Paths.LocalesDir, candidate + ".json");
            if (!File.Exists(path)) continue;

            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(path));
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    if (prop.Name == "_rtl")
                    {
                        if (candidate == Code) IsRtl = prop.Value.ValueKind == JsonValueKind.True;
                        continue;
                    }
                    if (!map.ContainsKey(prop.Name)) map[prop.Name] = prop.Value.GetString() ?? prop.Name;
                }
            }
            catch
            {
                // ignore a broken locale file and keep the fallback chain
            }
        }

        if (map.Count == 0) IsRtl = Code == "ar";
        _map = map;
    }

    public static string T(string key) =>
        _map.TryGetValue(key, out var value) ? value : key;

    public static string T(string key, params object[] args)
    {
        var text = T(key);
        try { return string.Format(text, args); } catch { return text; }
    }
}
