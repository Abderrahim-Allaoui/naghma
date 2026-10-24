using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Naghma.Core;

/// <summary>
/// Offline licence check, ECDSA P-256.
///
///   NAGHMA-&lt;base64url(payload)&gt;.&lt;base64url(signature)&gt;
///   payload = {"e":"buyer@mail","t":1735689600,"p":"pro"}
///
/// Only the public key ships inside the app, so keys cannot be forged even if the
/// binary is reversed. No server, no expiry, no phone-home.
/// </summary>
public static class License
{
    private const string PublicKeySpki =
        "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAE56lm9ss3NgQ2MMUNgmvQ5xVhQaGxNRcOGyIHHSv5mcnQ1/QOMxiQc47uudHkfHWXnlrFyLEd5Tq4zu01FSfyRQ==";

    public static bool IsPro { get; private set; }
    public static string Email { get; private set; } = "";

    public static void Init(string? key)
    {
        IsPro = false;
        Email = "";

        if (string.IsNullOrWhiteSpace(key)) return;

        var text = key.Trim().Replace("\r", "").Replace("\n", "").Replace(" ", "");

        if (!text.StartsWith("NAGHMA-", StringComparison.OrdinalIgnoreCase)) return;

        var rest = text[7..];
        var dot = rest.IndexOf('.');
        if (dot <= 0 || dot == rest.Length - 1) return;

        try
        {
            var payload = FromBase64Url(rest[..dot]);
            var signature = FromBase64Url(rest[(dot + 1)..]);

            using var ecdsa = ECDsa.Create();
            ecdsa.ImportSubjectPublicKeyInfo(Convert.FromBase64String(PublicKeySpki), out _);

            if (!ecdsa.VerifyData(payload, signature, HashAlgorithmName.SHA256)) return;

            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;

            var plan = root.TryGetProperty("p", out var p) ? p.GetString() : null;
            if (!string.Equals(plan, "pro", StringComparison.OrdinalIgnoreCase)) return;

            IsPro = true;
            Email = root.TryGetProperty("e", out var e) ? e.GetString() ?? "" : "";
        }
        catch
        {
            IsPro = false;
            Email = "";
        }
    }

    private static byte[] FromBase64Url(string s)
    {
        s = s.Replace('-', '+').Replace('_', '/');
        s = (s.Length % 4) switch
        {
            2 => s + "==",
            3 => s + "=",
            _ => s,
        };
        return Convert.FromBase64String(s);
    }
}
