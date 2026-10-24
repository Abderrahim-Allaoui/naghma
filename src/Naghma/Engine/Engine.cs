using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Naghma.Engine;

public enum EngineKind { Log, Progress, Finished }

public sealed record EngineEvent(EngineKind Kind, string Text = "", int Percent = -1);

public sealed record DownloadOptions(
    string Target,
    int TypeIndex,
    string OutputDir,
    bool Playlist,
    bool Organize,
    bool Dedupe,
    int ResultIndex)
{
    public bool IsUrl => Regex.IsMatch(Target, @"^https?://", RegexOptions.IgnoreCase);
}

/// <summary>Turns high level options into a yt-dlp argument line.</summary>
public static class Args
{
    public static string[] Build(DownloadOptions o)
    {
        var a = new List<string>
        {
            "--newline", "--no-mtime", "--windows-filenames",
            "--trim-filenames", "180", "--retries", "5", "--concurrent-fragments", "4",
        };

        a.Add(o.Playlist ? "--yes-playlist" : "--no-playlist");

        // JS challenge solver (YouTube). Bundled deno, when present.
        if (File.Exists(ToolManager.Deno))
        {
            a.Add("--js-runtimes");
            a.Add($"deno:{ToolManager.Deno}");
        }
        a.Add("--remote-components");
        a.Add("ejs:github");

        // "Artist - Title" -> proper tags, and strips the usual YouTube noise.
        a.Add("--parse-metadata");
        a.Add("title:(?P<artist>[^-]+?) - (?P<title>.+)");
        a.Add("--replace-in-metadata");
        a.Add("title");
        a.Add(@"(?i)\s*[\(\[](official\s*(music\s*)?(video|audio)|lyric[s]?|hd|hq|4k|visualizer|audio)[\)\]]");
        a.Add("");
        a.Add("--replace-in-metadata");
        a.Add("title");
        a.Add(@"(?i)\s*-\s*topic$");
        a.Add("");

        if (o.Dedupe)
        {
            a.Add("--download-archive");
            a.Add(Path.Combine(o.OutputDir, ".archive.txt"));
        }

        a.Add("--paths");
        a.Add("home:" + o.OutputDir);

        if (Directory.Exists(ToolManager.FfmpegLocation))
        {
            a.Add("--ffmpeg-location");
            a.Add(ToolManager.FfmpegLocation);
        }

        var audioTemplate = o.Organize
            ? Path.Combine("%(artist,creator,uploader,channel)s", "%(album,playlist_title|Singles)s", "%(title)s.%(ext)s")
            : "%(artist,creator,uploader,channel)s - %(title)s.%(ext)s";

        var videoTemplate = o.Organize
            ? Path.Combine("%(uploader)s", "%(title)s.%(ext)s")
            : "%(title)s.%(ext)s";

        var meta = new[] { "--embed-metadata", "--embed-thumbnail", "--convert-thumbnails", "jpg" };

        switch (o.TypeIndex)
        {
            case 0:
                a.AddRange(new[] { "-x", "--audio-format", "mp3", "--audio-quality", "0" });
                a.AddRange(meta);
                a.AddRange(new[] { "--output", audioTemplate });
                break;
            case 1:
                a.AddRange(new[] { "-x", "--audio-format", "m4a", "--audio-quality", "0" });
                a.AddRange(meta);
                a.AddRange(new[] { "--output", audioTemplate });
                break;
            case 2:
                a.AddRange(new[] { "-x", "--audio-format", "opus", "--audio-quality", "0" });
                a.AddRange(meta);
                a.AddRange(new[] { "--output", audioTemplate });
                break;
            case 3:
                a.AddRange(new[] { "-x", "--audio-format", "flac", "--audio-quality", "0" });
                a.AddRange(meta);
                a.AddRange(new[] { "--output", audioTemplate });
                break;
            case 4:
                a.AddRange(new[] { "-f", "bv*+ba/b", "--merge-output-format", "mp4" });
                a.AddRange(meta);
                a.AddRange(new[] { "--output", videoTemplate });
                break;
            case 5:
                a.AddRange(new[] { "-f", "bv*+ba/b", "-S", "res:1080", "--merge-output-format", "mp4" });
                a.AddRange(meta);
                a.AddRange(new[] { "--output", videoTemplate });
                break;
            case 6:
                a.AddRange(new[] { "-f", "bv*+ba/b", "-S", "res:720", "--merge-output-format", "mp4" });
                a.AddRange(meta);
                a.AddRange(new[] { "--output", videoTemplate });
                break;
            case 7:
                a.AddRange(new[] { "-f", "bv*+ba/b", "-S", "res:2160", "--merge-output-format", "mp4" });
                a.AddRange(meta);
                a.AddRange(new[] { "--output", videoTemplate });
                break;
        }

        var source = o.IsUrl
            ? o.Target
            : $"ytsearch{Math.Max(1, o.ResultIndex)}:{o.Target}";

        a.Add(source);
        return a.ToArray();
    }

    public static string Quote(string s)
    {
        var sb = new StringBuilder("\"");
        var backslashes = 0;

        foreach (var c in s)
        {
            if (c == '\\') { backslashes++; continue; }

            if (c == '"')
            {
                sb.Append('\\', backslashes * 2 + 1);
                sb.Append('"');
            }
            else
            {
                sb.Append('\\', backslashes);
                sb.Append(c);
            }

            backslashes = 0;
        }

        sb.Append('\\', backslashes * 2);
        sb.Append('"');
        return sb.ToString();
    }

    public static string ToCommandLine(IEnumerable<string> args) =>
        string.Join(" ", args.Select(Quote));
}

/// <summary>Runs yt-dlp and streams its output as <see cref="EngineEvent"/>.</summary>
public sealed class Engine
{
    private static readonly Regex PercentRx = new(@"^\[download\]\s+([\d.]+)%", RegexOptions.Compiled);

    private Process? _process;

    public bool Running => _process is { HasExited: false };

    public async Task<int> RunAsync(string exe, string[] args, string workingDir,
        IProgress<EngineEvent> progress, CancellationToken ct = default)
    {
        var psi = new ProcessStartInfo(exe)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            WorkingDirectory = workingDir,
        };

        foreach (var a in args) psi.ArgumentList.Add(a);

        _process = new Process { StartInfo = psi, EnableRaisingEvents = true };

        void Handler(object? _, DataReceivedEventArgs e)
        {
            if (e.Data is null) return;

            // yt-dlp uses \r to rewrite progress lines; keep only the final segment.
            foreach (var raw in e.Data.Split('\r'))
            {
                var line = raw.TrimEnd();
                if (line.Length == 0) continue;

                var m = PercentRx.Match(line);
                if (m.Success &&
                    double.TryParse(m.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var pct))
                {
                    progress.Report(new EngineEvent(EngineKind.Progress, "", (int)pct));
                    continue;
                }

                // noise: progress detail and bookkeeping lines
                if (line.StartsWith("[download] ", StringComparison.Ordinal)) continue;
                if (line.StartsWith("[info] ", StringComparison.Ordinal)) continue;
                if (line.StartsWith("Deleting original file", StringComparison.Ordinal)) continue;

                progress.Report(new EngineEvent(EngineKind.Log, line));
            }
        }

        _process.OutputDataReceived += Handler;
        _process.ErrorDataReceived += Handler;

        _process.Start();
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();

        try
        {
            await _process.WaitForExitAsync(ct);
        }
        catch (OperationCanceledException)
        {
            Kill();
            throw;
        }

        return _process.ExitCode;
    }

    public void Kill()
    {
        try
        {
            if (_process is { HasExited: false }) _process.Kill(true);
        }
        catch
        {
            // process already gone
        }
    }
}
