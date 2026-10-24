using System.IO.Compression;
using System.Net.Http;

namespace Naghma.Engine;

/// <summary>
/// Makes sure the download engine exists on disk. Everything is portable:
/// binaries live in %LOCALAPPDATA%\Naghma\bin and are fetched once, then reused.
/// </summary>
public static class ToolManager
{
    private const string YtDlpUrl = "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe";
    private const string FfmpegUrl = "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip";
    private const string DenoUrl = "https://github.com/denoland/deno/releases/latest/download/deno-x86_64-pc-windows-msvc.zip";

    public static string YtDlp => Path.Combine(Core.Paths.BinDir, "yt-dlp.exe");
    public static string FfmpegDir => Path.Combine(Core.Paths.BinDir, "ffmpeg");
    public static string Ffmpeg => Path.Combine(FfmpegDir, "ffmpeg.exe");
    public static string Deno => Path.Combine(Core.Paths.BinDir, "deno.exe");

    private static string? _ffmpegOnPath;

    /// <summary>An ffmpeg already installed by the user (PATH) - avoids a ~100 MB download.</summary>
    public static string? FfmpegOnPath
    {
        get
        {
            if (_ffmpegOnPath is not null) return _ffmpegOnPath.Length == 0 ? null : _ffmpegOnPath;

            _ffmpegOnPath = "";
            foreach (var dir in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(';'))
            {
                try
                {
                    if (dir.Trim().Length == 0) continue;
                    var candidate = Path.Combine(dir.Trim(), "ffmpeg.exe");
                    if (File.Exists(candidate)) { _ffmpegOnPath = candidate; break; }
                }
                catch
                {
                    // malformed PATH entry
                }
            }

            return _ffmpegOnPath.Length == 0 ? null : _ffmpegOnPath;
        }
    }

    /// <summary>Folder handed to yt-dlp's --ffmpeg-location.</summary>
    public static string FfmpegLocation =>
        File.Exists(Ffmpeg) ? FfmpegDir : Path.GetDirectoryName(FfmpegOnPath) ?? FfmpegDir;

    public static bool Ready => File.Exists(YtDlp) && (File.Exists(Ffmpeg) || FfmpegOnPath is not null);

    /// <summary>Downloads anything missing. Reports "label|percent" through <paramref name="progress"/>.</summary>
    public static async Task EnsureAsync(IProgress<string>? progress, CancellationToken ct = default)
    {
        Core.Paths.EnsureDirs();

        using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("Naghma/0.1");

        if (!File.Exists(YtDlp))
        {
            progress?.Report("yt-dlp|0");
            await DownloadAsync(http, YtDlpUrl, YtDlp, "yt-dlp", progress, ct);
        }

        if (!File.Exists(Ffmpeg) && FfmpegOnPath is null)
        {
            progress?.Report("ffmpeg|0");
            var zip = Path.Combine(Core.Paths.BinDir, "ffmpeg.zip");
            await DownloadAsync(http, FfmpegUrl, zip, "ffmpeg", progress, ct);

            var extractDir = Path.Combine(Core.Paths.BinDir, "ffmpeg.tmp");
            if (Directory.Exists(extractDir)) Directory.Delete(extractDir, true);
            ZipFile.ExtractToDirectory(zip, extractDir);

            var bin = Directory.GetFiles(extractDir, "ffmpeg.exe", SearchOption.AllDirectories).FirstOrDefault();
            if (bin is null) throw new InvalidOperationException("ffmpeg.exe not found in archive");

            if (Directory.Exists(FfmpegDir)) Directory.Delete(FfmpegDir, true);
            Directory.CreateDirectory(FfmpegDir);
            File.Copy(bin, Ffmpeg, true);

            foreach (var extra in new[] { "ffprobe.exe" })
            {
                var src = Path.Combine(Path.GetDirectoryName(bin)!, extra);
                if (File.Exists(src)) File.Copy(src, Path.Combine(FfmpegDir, extra), true);
            }

            try { Directory.Delete(extractDir, true); } catch { }
            try { File.Delete(zip); } catch { }
        }

        // Deno powers yt-dlp's JavaScript challenge solver (YouTube). Optional but strongly recommended.
        if (!File.Exists(Deno))
        {
            try
            {
                progress?.Report("deno|0");
                var zip = Path.Combine(Core.Paths.BinDir, "deno.zip");
                await DownloadAsync(http, DenoUrl, zip, "deno", progress, ct);
                ZipFile.ExtractToDirectory(zip, Core.Paths.BinDir, true);
                try { File.Delete(zip); } catch { }
            }
            catch
            {
                // the app still works without it; some YouTube formats may be missing
            }
        }

        progress?.Report("ready|100");
    }

    private static async Task DownloadAsync(HttpClient http, string url, string dest, string label,
        IProgress<string>? progress, CancellationToken ct)
    {
        using var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        var total = response.Content.Headers.ContentLength ?? -1;
        var tmp = dest + ".part";

        await using (var input = await response.Content.ReadAsStreamAsync(ct))
        await using (var output = File.Create(tmp))
        {
            var buffer = new byte[81920];
            long read = 0;
            int n;
            int lastPct = -1;

            while ((n = await input.ReadAsync(buffer, ct)) > 0)
            {
                await output.WriteAsync(buffer.AsMemory(0, n), ct);
                read += n;

                if (total > 0)
                {
                    var pct = (int)(read * 100 / total);
                    if (pct != lastPct)
                    {
                        lastPct = pct;
                        progress?.Report($"{label}|{pct}");
                    }
                }
            }
        }

        File.Move(tmp, dest, true);
    }

    /// <summary>Force-updates yt-dlp (engine changes often).</summary>
    public static async Task UpdateAsync(IProgress<string>? progress, CancellationToken ct = default)
    {
        try
        {
            if (File.Exists(YtDlp)) File.Delete(YtDlp);
        }
        catch { }

        await EnsureAsync(progress, ct);
    }
}
