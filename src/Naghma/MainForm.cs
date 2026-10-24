using System.Diagnostics;
using System.Drawing;
using System.Text.Json;
using Naghma.Core;
using Naghma.Engine;

namespace Naghma;

public sealed class MainForm : Form
{
    private readonly AppSettings _settings;
    private readonly Engine.Engine _engine = new();
    private CancellationTokenSource? _cts;

    private readonly Queue<string> _queue = new();
    private int _total;
    private int _doneCount;
    private int _okCount;
    private int _failCount;
    private int _currentType;
    private bool _stopRequested;

    // download tab
    private readonly TextBox _input = new();
    private readonly ComboBox _format = new();
    private readonly TextBox _output = new();
    private readonly Button _browse = new();
    private readonly Button _download = new();
    private readonly Button _stop = new();
    private readonly Button _open = new();
    private readonly Button _update = new();
    private readonly CheckBox _playlist = new();
    private readonly Label _resultLabel = new();
    private readonly NumericUpDown _resultIndex = new();
    private readonly ProgressBar _progress = new();
    private readonly RichTextBox _log = new();

    // sites tab
    private readonly TextBox _filter = new();
    private readonly Label _countLabel = new();
    private readonly ListBox _sites = new();
    private readonly ListBox _popular = new();
    private readonly Label _popularLabel = new();
    private string[] _allSites = Array.Empty<string>();
    private bool _sitesLoaded;

    // settings tab
    private readonly ComboBox _language = new();
    private readonly CheckBox _organize = new();
    private readonly CheckBox _dedupe = new();
    private readonly CheckBox _dark = new();
    private readonly TextBox _license = new();
    private readonly Button _activate = new();
    private readonly Label _about = new();
    private readonly Label _licenseState = new();

    private readonly StatusStrip _status = new();
    private readonly ToolStripStatusLabel _statusLabel = new();

    private readonly TabControl _tabs = new();
    private readonly TabPage _tabDownload = new();
    private readonly TabPage _tabSites = new();
    private readonly TabPage _tabSettings = new();

    private readonly Label _inputLabel = new();
    private readonly Label _formatLabel = new();
    private readonly Label _outputLabel = new();
    private readonly Label _filterLabel = new();
    private readonly Label _languageLabel = new();
    private readonly Label _licenseLabel = new();

    public MainForm(AppSettings settings)
    {
        _settings = settings;

        BuildWindow();
        BuildDownloadTab();
        BuildSitesTab();
        BuildSettingsTab();
        Theme.Apply(this, _settings.DarkTheme);

        ApplyLocale();
        WireEvents();
        LayoutAll();

        Shown += async (_, _) =>
        {
            LayoutAll();
            Log(Loc.T("log.hello"), Theme.Muted);
            Log($"{Loc.T("log.engine")}: {ToolManager.YtDlp}", Theme.Muted);
            Log($"{Loc.T("log.output")}: {_settings.OutputDir}", Theme.Muted);

            if (!ToolManager.Ready)
            {
                SetStatus(Loc.T("status.preparing"));
                _download.Enabled = false;
                await EnsureEngineAsync();
                _download.Enabled = true;
            }
            SetStatus(Loc.T("status.ready"));
        };
    }

    // ---------------------------------------------------------------- window
    private void BuildWindow()
    {
        Text = "Naghma";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(820, 600);
        Size = new Size(_settings.WindowWidth, _settings.WindowHeight);
        Font = new Font("Segoe UI", 9f);
        KeyPreview = true;
        AllowDrop = true;

        _tabs.Dock = DockStyle.Fill;
        _tabs.Font = Font;
        Controls.Add(_tabs);

        _statusLabel.Text = "…";
        _status.Items.Add(_statusLabel);
        _status.SizingGrip = false;
        Controls.Add(_status);

        _tabDownload.Text = "Download";
        _tabSites.Text = "Sites";
        _tabSettings.Text = "Settings";
        _tabs.TabPages.AddRange(new[] { _tabDownload, _tabSites, _tabSettings });
    }

    // ---------------------------------------------------------------- tab 1
    private void BuildDownloadTab()
    {
        _input.Multiline = true;
        _input.AcceptsReturn = true;
        _input.ScrollBars = ScrollBars.Vertical;
        _input.Font = new Font("Consolas", 9.5f);
        _input.BorderStyle = BorderStyle.FixedSingle;

        _format.DropDownStyle = ComboBoxStyle.DropDownList;
        _format.Items.AddRange(new object[]
        {
            Loc.T("fmt.mp3"), Loc.T("fmt.m4a"), Loc.T("fmt.opus"), Loc.T("fmt.flac"),
            Loc.T("fmt.video"), Loc.T("fmt.1080"), Loc.T("fmt.720"), Loc.T("fmt.2160"),
        });
        _currentType = Math.Clamp(_settings.TypeIndex, 0, 7);
        if (!License.IsPro && (_currentType == 5 || _currentType == 7)) _currentType = 0;
        _format.SelectedIndex = _currentType;

        _output.ReadOnly = true;
        _output.Text = _settings.OutputDir;

        _browse.Text = "…";
        _download.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
        _download.Height = 36;
        _stop.Enabled = false;

        _playlist.Text = Loc.T("chk.playlist");
        _playlist.Enabled = true; // Pro gate is enforced on click (better upsell than greyed out)

        _resultLabel.Text = Loc.T("lbl.result");
        _resultIndex.Minimum = 1;
        _resultIndex.Maximum = 10;
        _resultIndex.Value = Math.Clamp(_settings.ResultIndex, 1, 10);

        _progress.Minimum = 0;
        _progress.Maximum = 100;

        _log.ReadOnly = true;
        _log.WordWrap = true;
        _log.BorderStyle = BorderStyle.None;
        _log.Font = new Font("Consolas", 9f);
        _log.ScrollBars = RichTextBoxScrollBars.Vertical;

        _tabDownload.Controls.AddRange(new Control[]
        {
            _inputLabel, _input, _formatLabel, _format, _outputLabel, _output, _browse,
            _playlist, _resultLabel, _resultIndex, _download, _stop, _open, _update, _progress, _log,
        });

        _inputLabel.Text = Loc.T("lbl.input");
        _formatLabel.Text = Loc.T("lbl.format");
        _outputLabel.Text = Loc.T("lbl.output");
        _download.Text = Loc.T("btn.download");
        _stop.Text = Loc.T("btn.stop");
        _open.Text = Loc.T("btn.open");
        _update.Text = Loc.T("btn.update");
    }

    // ---------------------------------------------------------------- tab 2
    private void BuildSitesTab()
    {
        _sites.Font = new Font("Consolas", 9f);
        _sites.IntegralHeight = false;
        _popular.Font = new Font("Consolas", 9f);
        _popular.IntegralHeight = false;
        _popularLabel.Text = Loc.T("sites.popular");
        _filterLabel.Text = Loc.T("sites.filter");
        _countLabel.Text = Loc.T("sites.loading");

        _popular.Items.AddRange(new object[]
        {
            "youtube", "youtube:tab", "youtube:music", "soundcloud", "Bandcamp", "vimeo",
            "dailymotion", "TikTok", "Instagram", "facebook", "twitter", "Reddit", "twitch",
            "mixcloud", "audiomack", "Jamendo", "archive.org", "BiliBili", "nicovideo",
            "Rumble", "kick", "vk", "9GAG", "Tumblr", "Pinterest", "Streamable", "Snapchat",
        });

        _tabSites.Controls.AddRange(new Control[]
        {
            _filterLabel, _filter, _countLabel, _popularLabel, _popular, _sites,
        });
    }

    // ---------------------------------------------------------------- tab 3
    private void BuildSettingsTab()
    {
        _language.DropDownStyle = ComboBoxStyle.DropDownList;
        _language.Items.AddRange(new object[] { "English", "العربية", "Français" });
        _language.SelectedIndex = Loc.Code switch { "ar" => 1, "fr" => 2, _ => 0 };

        _organize.Text = Loc.T("set.organize");
        _organize.Checked = _settings.Organize;
        _organize.Enabled = true; // Pro gate is enforced on click

        _dedupe.Text = Loc.T("set.dedupe");
        _dedupe.Checked = _settings.Dedupe;

        _dark.Text = Loc.T("set.theme");
        _dark.Checked = _settings.DarkTheme;

        _licenseLabel.Text = Loc.T("set.license");
        _license.Text = _settings.LicenseKey;
        _activate.Text = Loc.T("set.activate");

        _licenseState.Text = License.IsPro
            ? Loc.T("set.activated", License.Email)
            : "Free";
        _licenseState.ForeColor = License.IsPro ? Color.SeaGreen : Theme.Muted;

        _about.Text = Loc.T("set.about.body", Application.ProductVersion);

        _tabSettings.Controls.AddRange(new Control[]
        {
            _languageLabel, _language, _dark, _organize, _dedupe,
            _licenseLabel, _license, _activate, _licenseState, _about,
        });

        _languageLabel.Text = Loc.T("set.language");
    }

    // ---------------------------------------------------------------- i18n
    private void ApplyLocale()
    {
        RightToLeft = Loc.IsRtl ? RightToLeft.Yes : RightToLeft.No;
        RightToLeftLayout = Loc.IsRtl;

        Text = Loc.T("app.title");
        _tabDownload.Text = "  " + Loc.T("tab.download") + "  ";
        _tabSites.Text = "  " + Loc.T("tab.sites") + "  ";
        _tabSettings.Text = "  " + Loc.T("tab.settings") + "  ";

        _inputLabel.Text = Loc.T("lbl.input");
        _formatLabel.Text = Loc.T("lbl.format");
        _outputLabel.Text = Loc.T("lbl.output");
        _resultLabel.Text = Loc.T("lbl.result");
        _download.Text = Loc.T("btn.download");
        _stop.Text = Loc.T("btn.stop");
        _open.Text = Loc.T("btn.open");
        _update.Text = Loc.T("btn.update");
        _playlist.Text = Loc.T("chk.playlist");

        _filterLabel.Text = Loc.T("sites.filter");
        _popularLabel.Text = Loc.T("sites.popular");

        _languageLabel.Text = Loc.T("set.language");
        _organize.Text = Loc.T("set.organize");
        _dedupe.Text = Loc.T("set.dedupe");
        _dark.Text = Loc.T("set.theme");
        _licenseLabel.Text = Loc.T("set.license");
        _activate.Text = Loc.T("set.activate");
        _about.Text = Loc.T("set.about.body", Application.ProductVersion);
    }

    // ---------------------------------------------------------------- events
    private void WireEvents()
    {
        Resize += (_, _) => LayoutAll();
        FormClosing += (_, _) => SaveSettings();

        KeyDown += (_, e) =>
        {
            if (e.Control && e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                _ = StartDownloadsAsync();
            }
        };

        DragEnter += (_, e) =>
        {
            if (e.Data?.GetDataPresent(DataFormats.Text) == true) e.Effect = DragDropEffects.Copy;
        };
        DragDrop += (_, e) =>
        {
            if (e.Data?.GetData(DataFormats.Text) is string text && text.Trim().Length > 0)
            {
                _input.Text = text.Trim();
                _input.Focus();
            }
        };

        _download.Click += async (_, _) => await StartDownloadsAsync();
        _stop.Click += (_, _) => StopDownloads();
        _open.Click += (_, _) => OpenOutput();
        _browse.Click += (_, _) => BrowseOutput();
        _update.Click += async (_, _) => await UpdateEngineAsync();

        _playlist.CheckedChanged += (_, _) =>
        {
            if (_playlist.Checked && !License.IsPro)
            {
                _playlist.Checked = false;
                ShowPro();
            }
        };

        _format.SelectedIndexChanged += (_, _) =>
        {
            var idx = _format.SelectedIndex;
            if (!License.IsPro && (idx == 5 || idx == 7))
            {
                _format.SelectedIndex = Math.Max(0, _currentType);
                ShowPro();
                return;
            }
            _currentType = idx;
        };

        _filter.TextChanged += (_, _) => ApplyFilter();
        _popular.DoubleClick += (_, _) =>
        {
            if (_popular.SelectedItem is string s)
            {
                _filter.Text = s;
                _filter.Focus();
                _filter.SelectAll();
            }
        };

        _tabs.SelectedIndexChanged += async (_, _) =>
        {
            if (_tabs.SelectedIndex == 1 && !_sitesLoaded) await LoadSitesAsync();
        };

        _language.SelectedIndexChanged += (_, _) =>
        {
            var code = _language.SelectedIndex switch { 1 => "ar", 2 => "fr", _ => "en" };
            if (code == Loc.Code) return;

            _settings.Language = code;
            _settings.Save();
            Loc.Load(code);
            ApplyLocale();
            LayoutAll();

            // format combo items are localised too
            var keep = _format.SelectedIndex;
            _format.BeginUpdate();
            _format.Items.Clear();
            _format.Items.AddRange(new object[]
            {
                Loc.T("fmt.mp3"), Loc.T("fmt.m4a"), Loc.T("fmt.opus"), Loc.T("fmt.flac"),
                Loc.T("fmt.video"), Loc.T("fmt.1080"), Loc.T("fmt.720"), Loc.T("fmt.2160"),
            });
            _format.SelectedIndex = Math.Max(0, keep);
            _format.EndUpdate();

            RightToLeft = Loc.IsRtl ? RightToLeft.Yes : RightToLeft.No;
            RightToLeftLayout = Loc.IsRtl;
        };

        _dark.CheckedChanged += (_, _) =>
        {
            _settings.DarkTheme = _dark.Checked;
            Theme.Apply(this, _dark.Checked);
            Invalidate(true);
        };

        _organize.CheckedChanged += (_, _) =>
        {
            if (_organize.Checked && !License.IsPro)
            {
                _organize.Checked = false;
                ShowPro();
                return;
            }
            _settings.Organize = _organize.Checked;
        };

        _dedupe.CheckedChanged += (_, _) => _settings.Dedupe = _dedupe.Checked;

        _activate.Click += (_, _) => ActivateLicense();
    }

    // ---------------------------------------------------------------- layout
    private void LayoutAll()
    {
        var w = _tabDownload.ClientSize.Width;
        var h = _tabDownload.ClientSize.Height;
        var pad = 14;

        _inputLabel.SetBounds(pad, 10, w - 2 * pad, 18);
        _input.SetBounds(pad, 30, w - 2 * pad, 84);

        _formatLabel.SetBounds(pad, 122, 120, 18);
        _format.SetBounds(pad, 142, 250, 26);

        _outputLabel.SetBounds(pad + 268, 122, 200, 18);
        _output.SetBounds(pad + 268, 142, Math.Max(120, w - pad - 268 - 74), 26);
        _browse.SetBounds(w - pad - 58, 141, 58, 28);

        _playlist.SetBounds(pad, 176, 150, 24);
        _resultLabel.SetBounds(pad + 162, 178, 84, 20);
        _resultIndex.SetBounds(pad + 250, 176, 52, 24);

        _download.SetBounds(pad + 316, 172, 160, 36);
        _stop.SetBounds(pad + 484, 176, 84, 30);
        _open.SetBounds(pad + 576, 176, 140, 30);
        _update.SetBounds(w - pad - 150, 176, 150, 30);

        _progress.SetBounds(pad, 218, Math.Max(200, w - 2 * pad), 12);
        _log.SetBounds(pad, 240, Math.Max(200, w - 2 * pad), Math.Max(80, h - 252));

        // sites
        var sw = _tabSites.ClientSize.Width;
        var sh = _tabSites.ClientSize.Height;
        var popLeft = Math.Max(320, sw - 316);

        _filterLabel.SetBounds(pad, 16, 50, 20);
        _filter.SetBounds(pad + 44, 12, Math.Max(120, popLeft - pad - 44 - 120), 26);
        _countLabel.SetBounds(popLeft - 118, 16, 110, 20);

        _popularLabel.SetBounds(popLeft, 16, 200, 20);
        _popular.SetBounds(popLeft, 44, Math.Max(160, sw - pad - popLeft), Math.Max(80, sh - 58));
        _sites.SetBounds(pad, 44, Math.Max(200, popLeft - pad - 12), Math.Max(80, sh - 58));

        // settings
        var gw = _tabSettings.ClientSize.Width;
        _languageLabel.SetBounds(pad, 16, 100, 20);
        _language.SetBounds(pad + 90, 14, 200, 26);
        _dark.SetBounds(pad, 52, 220, 24);
        _organize.SetBounds(pad, 80, gw - 2 * pad, 24);
        _dedupe.SetBounds(pad, 106, gw - 2 * pad, 24);
        _licenseLabel.SetBounds(pad, 144, 120, 20);
        _license.SetBounds(pad, 164, Math.Max(160, gw - 2 * pad - 110), 26);
        _activate.SetBounds(gw - pad - 100, 163, 100, 28);
        _licenseState.SetBounds(pad, 196, gw - 2 * pad, 20);
        _about.SetBounds(pad, 224, gw - 2 * pad, Math.Max(80, _tabSettings.ClientSize.Height - 236));
    }

    // ---------------------------------------------------------------- engine
    private async Task EnsureEngineAsync()
    {
        var progress = new Progress<string>(p =>
        {
            var parts = p.Split('|');
            if (parts.Length == 2 && int.TryParse(parts[1], out var pct))
            {
                _progress.Value = Math.Clamp(pct, 0, 100);
                SetStatus($"{Loc.T("status.preparing")} {parts[0]} {pct}%");
            }
        });

        try
        {
            await ToolManager.EnsureAsync(progress);
            _progress.Value = 0;
            Log("engine ready", Theme.Ok);
        }
        catch (Exception ex)
        {
            Log(ex.Message, Theme.Error);
            SetStatus(Loc.T("err.engine"));
        }
    }

    private async Task UpdateEngineAsync()
    {
        if (_engine.Running) return;

        _update.Enabled = false;
        SetStatus(Loc.T("status.preparing"));

        try
        {
            await ToolManager.UpdateAsync(new Progress<string>(p =>
            {
                var parts = p.Split('|');
                if (parts.Length == 2 && int.TryParse(parts[1], out var pct))
                {
                    _progress.Value = Math.Clamp(pct, 0, 100);
                    SetStatus($"{Loc.T("status.preparing")} {parts[0]} {pct}%");
                }
            }));
            Log("engine updated", Theme.Ok);
        }
        catch (Exception ex)
        {
            Log(ex.Message, Theme.Error);
        }
        finally
        {
            _progress.Value = 0;
            _update.Enabled = true;
            SetStatus(Loc.T("status.ready"));
        }
    }

    private async Task StartDownloadsAsync()
    {
        if (_engine.Running) return;

        var lines = _input.Lines
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .ToList();

        if (lines.Count == 0)
        {
            MessageBox.Show(this, Loc.T("err.empty"), Loc.T("err.title"),
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            _input.Focus();
            return;
        }

        if (!License.IsPro && lines.Count > 1)
        {
            ShowPro();
            return;
        }

        if (_playlist.Checked && !License.IsPro) { ShowPro(); return; }

        if (!ToolManager.Ready)
        {
            SetStatus(Loc.T("status.preparing"));
            await EnsureEngineAsync();
            if (!ToolManager.Ready) return;
        }

        Directory.CreateDirectory(_settings.OutputDir);

        _queue.Clear();
        foreach (var l in lines) _queue.Enqueue(l);

        _total = _queue.Count;
        _doneCount = _okCount = _failCount = 0;
        _stopRequested = false;

        _download.Enabled = false;
        _stop.Enabled = true;
        _update.Enabled = false;

        _cts = new CancellationTokenSource();

        Log("", Theme.Muted);
        Log($">>> {_total} × → {_settings.OutputDir}", Theme.Ok);

        await RunNextAsync();
    }

    private async Task RunNextAsync()
    {
        if (_stopRequested || _queue.Count == 0)
        {
            FinishBatch();
            return;
        }

        var target = _queue.Dequeue();
        _doneCount++;
        _progress.Value = 0;

        SetStatus(Loc.T("status.item", _doneCount, _total, target));
        Log("", Theme.Muted);
        Log($"### [{_doneCount}/{_total}] {target}", Theme.Accent);

        var options = new DownloadOptions(
            target,
            _format.SelectedIndex,
            _settings.OutputDir,
            _playlist.Checked,
            _organize.Checked,
            _dedupe.Checked,
            (int)_resultIndex.Value);

        var args = Args.Build(options);
        if (_settings.Verbose) Log("  " + Args.ToCommandLine(args), Theme.Muted);

        var progress = new Progress<EngineEvent>(e =>
        {
            if (e.Kind == EngineKind.Progress)
            {
                _progress.Value = Math.Clamp(e.Percent, 0, 100);
                SetStatus($"{Loc.T("status.working")} {e.Percent}%");
                return;
            }

            var line = e.Text;

            if (line.Contains("ERROR", StringComparison.OrdinalIgnoreCase))
            {
                Log("  " + line, Theme.Error);
                return;
            }

            if (line.Contains("WARNING", StringComparison.OrdinalIgnoreCase))
            {
                Log("  " + line, Theme.Warn);
                return;
            }

            // Surface only what a human cares about; the rest is engine noise.
            var marker = line.IndexOf("Destination: ", StringComparison.Ordinal);
            if (marker >= 0)
            {
                Log("  → " + line[(marker + 13)..].Trim(), Theme.Text);
                return;
            }

            if (line.Contains("has already been downloaded", StringComparison.OrdinalIgnoreCase))
                Log("  = " + Loc.T("log.exists"), Theme.Muted);
        });

        int code;
        try
        {
            code = await _engine.RunAsync(ToolManager.YtDlp, args, _settings.OutputDir, progress, _cts!.Token);
        }
        catch (OperationCanceledException)
        {
            code = -1;
        }
        catch (Exception ex)
        {
            Log(ex.Message, Theme.Error);
            code = -1;
        }

        if (_stopRequested)
        {
            Log("  " + Loc.T("log.stopped"), Theme.Warn);
            _failCount++;
            FinishBatch();
            return;
        }

        if (code == 0)
        {
            _okCount++;
            Log("  " + Loc.T("log.ok"), Theme.Ok);
        }
        else
        {
            _failCount++;
            Log($"  {Loc.T("log.fail")} ({code})", Theme.Error);
        }

        _progress.Value = 0;
        await RunNextAsync();
    }

    private void FinishBatch()
    {
        _download.Enabled = true;
        _stop.Enabled = false;
        _update.Enabled = true;
        _progress.Value = 0;
        _stopRequested = false;

        Log("", Theme.Muted);
        Log("=== " + Loc.T("status.done", _okCount, _failCount) + " ===", Theme.Ok);
        SetStatus(Loc.T("status.done", _okCount, _failCount));
    }

    private void StopDownloads()
    {
        if (!_engine.Running && _queue.Count == 0) return;

        _stopRequested = true;
        _queue.Clear();
        Log(">>> stop", Theme.Warn);

        _cts?.Cancel();
        _engine.Kill();
    }

    // ---------------------------------------------------------------- actions
    private void BrowseOutput()
    {
        using var dlg = new FolderBrowserDialog { Description = Loc.T("dlg.folder"), SelectedPath = _settings.OutputDir };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        _settings.OutputDir = dlg.SelectedPath;
        _output.Text = dlg.SelectedPath;
        _settings.Save();
        Log($"{Loc.T("log.output")}: {dlg.SelectedPath}", Theme.Muted);
    }

    private void OpenOutput()
    {
        try
        {
            Directory.CreateDirectory(_settings.OutputDir);
            Process.Start(new ProcessStartInfo("explorer.exe", $"\"{_settings.OutputDir}\"") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Log(ex.Message, Theme.Error);
        }
    }

    private void ActivateLicense()
    {
        var key = _license.Text.Trim();
        License.Init(key);

        if (License.IsPro)
        {
            _settings.LicenseKey = key;
            _settings.Save();
            _licenseState.Text = Loc.T("set.activated", License.Email);
            _licenseState.ForeColor = Color.SeaGreen;
            _playlist.Enabled = true;
            _organize.Enabled = true;
            SetStatus(Loc.T("pro.thanks"));
        }
        else
        {
            _licenseState.Text = Loc.T("set.badkey");
            _licenseState.ForeColor = Theme.Error;
        }
    }

    private void ShowPro()
    {
        var text = Loc.T("pro.body") + Environment.NewLine + Environment.NewLine + Loc.T("pro.button");

        var answer = MessageBox.Show(this, text, Loc.T("pro.title"),
            MessageBoxButtons.YesNo, MessageBoxIcon.Information);

        if (answer != DialogResult.Yes || string.IsNullOrWhiteSpace(_settings.ProUrl)) return;

        try
        {
            Process.Start(new ProcessStartInfo(_settings.ProUrl) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Log(ex.Message, Theme.Error);
        }
    }

    // ---------------------------------------------------------------- sites
    private async Task LoadSitesAsync()
    {
        _countLabel.Text = Loc.T("sites.loading");
        SetStatus(Loc.T("status.sites"));

        try
        {
            if (File.Exists(Paths.CacheFile))
            {
                _allSites = JsonSerializer.Deserialize<string[]>(await File.ReadAllTextAsync(Paths.CacheFile))
                            ?? Array.Empty<string>();
            }

            if (_allSites.Length == 0 && ToolManager.Ready)
            {
                var list = await Task.Run(() =>
                {
                    var psi = new ProcessStartInfo(ToolManager.YtDlp, "--list-extractors")
                    {
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    };
                    using var p = Process.Start(psi)!;
                    var output = p.StandardOutput.ReadToEnd();
                    p.WaitForExit();
                    return output.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .Where(s => s.Length > 0 && !s.StartsWith("["))
                        .Distinct()
                        .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                        .ToArray();
                });

                _allSites = list;
                try { await File.WriteAllTextAsync(Paths.CacheFile, JsonSerializer.Serialize(list)); } catch { }
            }

            ApplyFilter();
        }
        catch (Exception ex)
        {
            Log(ex.Message, Theme.Error);
        }

        _sitesLoaded = true;
        SetStatus(Loc.T("status.ready"));
    }

    private void ApplyFilter()
    {
        var needle = _filter.Text.Trim();
        var items = needle.Length == 0
            ? _allSites
            : _allSites.Where(s => s.Contains(needle, StringComparison.OrdinalIgnoreCase)).ToArray();

        _sites.BeginUpdate();
        _sites.Items.Clear();
        _sites.Items.AddRange(items);
        _sites.EndUpdate();

        _countLabel.Text = Loc.T("sites.count", items.Length);
    }

    // ---------------------------------------------------------------- misc
    private void SetStatus(string text) => _statusLabel.Text = text;

    private void Log(string text, Color? color = null)
    {
        if (_log.TextLength > 400_000) _log.Clear();

        _log.SelectionStart = _log.TextLength;
        _log.SelectionLength = 0;
        _log.SelectionColor = color is null ? Theme.LogText : Theme.ForLog(color.Value);
        _log.AppendText("  " + text + Environment.NewLine);
        _log.SelectionColor = _log.ForeColor;
        _log.ScrollToCaret();
    }

    private void SaveSettings()
    {
        _settings.WindowWidth = Width;
        _settings.WindowHeight = Height;
        _settings.TypeIndex = _format.SelectedIndex;
        _settings.ResultIndex = (int)_resultIndex.Value;
        _settings.Save();
    }
}
