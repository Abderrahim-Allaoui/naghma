# Dev helper: record assets/demo.mp4 - a real download captured with gdigrab,
# with a caption band, an Explorer payoff and intro/outro cards.
#
#   powershell -ExecutionPolicy Bypass -File scripts\dev-demo.ps1

param(
    [ValidateSet('dark', 'light')][string]$Theme = 'light',
    [string]$Target = 'Daft Punk - Instant Crush',
    [string]$DemoFolder = "$env:USERPROFILE\Music\Naghma Demo",
    [string]$Exe = "$PSScriptRoot\..\src\Naghma\bin\Release\net8.0-windows\Naghma.exe",
    [int]$Fps = 12,
    [int]$TimeoutSec = 300
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

Add-Type @'
using System;
using System.Text;
using System.Runtime.InteropServices;
public class Win32Demo {
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr hWnd, IntPtr after, int x, int y, int cx, int cy, uint flags);
    [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr w, IntPtr l);
    [DllImport("dwmapi.dll")] public static extern int DwmGetWindowAttribute(IntPtr hWnd, int attr, out RECT value, int size);
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }

    delegate bool EnumProc(IntPtr hWnd, IntPtr lParam);
    [DllImport("user32.dll")] static extern bool EnumWindows(EnumProc cb, IntPtr l);
    [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr h);
    [DllImport("user32.dll")] static extern int GetWindowTextLength(IntPtr h);
    [DllImport("user32.dll")] static extern int GetWindowText(IntPtr h, StringBuilder s, int n);

    public static IntPtr FindByTitle(string part) {
        IntPtr found = IntPtr.Zero;
        EnumWindows((h, l) => {
            if (!IsWindowVisible(h)) return true;
            int len = GetWindowTextLength(h);
            if (len == 0) return true;
            var sb = new StringBuilder(len + 1);
            GetWindowText(h, sb, sb.Capacity);
            if (sb.ToString().IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0) { found = h; return false; }
            return true;
        }, IntPtr.Zero);
        return found;
    }
}
'@

$root = Split-Path -Parent $PSScriptRoot
$assets = Join-Path $root 'assets'
$work = Join-Path $env:TEMP 'naghma-demo'
if (Test-Path $work) { Remove-Item $work -Recurse -Force }
New-Item -ItemType Directory -Force -Path $work | Out-Null
Copy-Item 'C:\Windows\Fonts\segoeuib.ttf' (Join-Path $work 'segoeuib.ttf') -Force
$raw = Join-Path $work 'raw.mp4'

# --- a clean output folder so the payoff shot shows exactly one file -----------
if (Test-Path $DemoFolder) { Remove-Item $DemoFolder -Recurse -Force }
New-Item -ItemType Directory -Force -Path $DemoFolder | Out-Null

# --- theme + a 16:9-ish window ------------------------------------------------
$settingsFile = Join-Path $env:LOCALAPPDATA 'Naghma\settings.json'
$settings = @{}
if (Test-Path $settingsFile) {
    $rawJson = Get-Content $settingsFile -Raw
    if ($rawJson.Trim()) { $settings = $rawJson | ConvertFrom-Json }
}
$settings | Add-Member -NotePropertyName DarkTheme -NotePropertyValue ($Theme -eq 'dark') -Force
$settings | Add-Member -NotePropertyName WindowWidth -NotePropertyValue 1200 -Force
$settings | Add-Member -NotePropertyName WindowHeight -NotePropertyValue 720 -Force
$originalOutput = $settings.OutputDir
$settings | Add-Member -NotePropertyName OutputDir -NotePropertyValue $DemoFolder -Force
$settings | ConvertTo-Json -Depth 6 | Set-Content -Path $settingsFile -Encoding UTF8

Get-Process Naghma -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 400

$AE = [System.Windows.Automation.AutomationElement]
$proc = Start-Process -FilePath (Resolve-Path $Exe) -PassThru
$deadline = (Get-Date).AddSeconds($TimeoutSec)
while ($proc.MainWindowHandle -eq [IntPtr]::Zero -and (Get-Date) -lt $deadline) { Start-Sleep -Milliseconds 250; $proc.Refresh() }
if ($proc.MainWindowHandle -eq [IntPtr]::Zero) { throw 'window never appeared' }

$hwnd = $proc.MainWindowHandle
[Win32Demo]::SetWindowPos($hwnd, [IntPtr](-1), 320, 120, 0, 0, 0x0043) | Out-Null
[Win32Demo]::SetForegroundWindow($hwnd) | Out-Null
Start-Sleep -Milliseconds 800

$rect = New-Object Win32Demo+RECT
if ([Win32Demo]::DwmGetWindowAttribute($hwnd, 9, [ref]$rect, 16) -ne 0) {
    [Win32Demo]::GetWindowRect($hwnd, [ref]$rect) | Out-Null
}
$w = $rect.Right - $rect.Left
$h = $rect.Bottom - $rect.Top
if ($w % 2 -ne 0) { $w-- }
if ($h % 2 -ne 0) { $h-- }
Write-Output "window ${w}x${h} at $($rect.Left),$($rect.Top)"

# --- recorder -----------------------------------------------------------------
$ffmpeg = Join-Path $env:LOCALAPPDATA 'Naghma\bin\ffmpeg\ffmpeg.exe'
if (!(Test-Path $ffmpeg)) { $ffmpeg = (Get-Command ffmpeg -ErrorAction Stop).Source }

$psi = New-Object System.Diagnostics.ProcessStartInfo
$psi.FileName = $ffmpeg
$psi.Arguments = "-y -hide_banner -loglevel error -f gdigrab -framerate $Fps -draw_mouse 0 -offset_x $($rect.Left) -offset_y $($rect.Top) -video_size ${w}x${h} -i desktop -c:v libx264 -preset ultrafast -crf 16 -pix_fmt yuv420p `"$raw`""
$psi.RedirectStandardInput = $true
$psi.UseShellExecute = $false
$psi.CreateNoWindow = $true
$rec = [System.Diagnostics.Process]::Start($psi)
$clock = [System.Diagnostics.Stopwatch]::StartNew()
Start-Sleep -Milliseconds 1200
Write-Output 'recording...'

# --- drive the UI -------------------------------------------------------------
$window = $AE::FromHandle($hwnd)
function Find-All($type) {
    $cond = New-Object System.Windows.Automation.PropertyCondition($AE::ControlTypeProperty, $type)
    return $window.FindAll([System.Windows.Automation.TreeScope]::Descendants, $cond)
}
function Select-Tab([string]$name) {
    foreach ($t in (Find-All ([System.Windows.Automation.ControlType]::TabItem))) {
        if ($t.Current.Name.Trim() -eq $name) {
            $t.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select()
            Start-Sleep -Milliseconds 700
            return $true
        }
    }
    return $false
}
function Find-Button([string]$pattern) {
    foreach ($b in (Find-All ([System.Windows.Automation.ControlType]::Button))) {
        if ($b.Current.Name -match $pattern) { return $b }
    }
    return $null
}
function Wait-Explorer([string]$titlePart, [int]$seconds) {
    $stop = (Get-Date).AddSeconds($seconds)
    while ((Get-Date) -lt $stop) {
        $h = [Win32Demo]::FindByTitle($titlePart)
        if ($h -ne [IntPtr]::Zero) { return $h }
        Start-Sleep -Milliseconds 250
    }
    return [IntPtr]::Zero
}

$edits = $null
for ($try = 0; $try -lt 25; $try++) {
    $edits = Find-All ([System.Windows.Automation.ControlType]::Edit)
    if ($edits.Count -gt 0) { break }
    Start-Sleep -Milliseconds 300
}
$input = $null; $best = 0
foreach ($e in $edits) {
    $r = $e.Current.BoundingRectangle
    if ($r.Height -gt $best) { $best = $r.Height; $input = $e }
}
$input.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue($Target)
Start-Sleep -Milliseconds 1500

$download = Find-Button 'Download|T.l.charger'
if (-not $download) { throw 'download button not found' }

$t0 = $clock.Elapsed.TotalSeconds
$download.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
Write-Output 'downloading...'

$sw = [System.Diagnostics.Stopwatch]::StartNew()
while (-not $download.Current.IsEnabled -and $sw.Elapsed.TotalSeconds -lt $TimeoutSec) { Start-Sleep -Milliseconds 250 }
$t1 = $clock.Elapsed.TotalSeconds
Write-Output "download took $([int]$sw.Elapsed.TotalSeconds)s"
Start-Sleep -Milliseconds 1600

# --- the payoff is the destination line in the log ----------------------------
$tOpen = $clock.Elapsed.TotalSeconds

[Win32Demo]::SetForegroundWindow($hwnd) | Out-Null
$tSites = $clock.Elapsed.TotalSeconds
if (Select-Tab 'Sites') { Start-Sleep -Milliseconds 2600 }
$tEnd = $clock.Elapsed.TotalSeconds

# --- stop ---------------------------------------------------------------------
try { $rec.StandardInput.WriteLine('q') } catch { }
if (-not $rec.WaitForExit(20000)) { $rec.Kill() }
[Win32Demo]::SetWindowPos($hwnd, [IntPtr](-2), 0, 0, 0, 0, 0x0043) | Out-Null
$proc.CloseMainWindow() | Out-Null
Start-Sleep -Milliseconds 500
if (!$proc.HasExited) { $proc.Kill() }

# put the user's real settings back
$back = Get-Content $settingsFile -Raw | ConvertFrom-Json
if ($originalOutput) { $back.OutputDir = $originalOutput }
$back | ConvertTo-Json -Depth 6 | Set-Content -Path $settingsFile -Encoding UTF8

# --- encode -------------------------------------------------------------------
# 2.6s intro | real time | download sped up | real time | outro
$seg = $t1 - $t0
$Speed = [Math]::Max(1, [Math]::Round($seg / 5))
Write-Output ("timeline: click {0:N1} done {1:N1} sites {2:N1} end {3:N1} | speed {4}x" -f $t0, $t1, $tSites, $tEnd, $Speed)

$intro = 2.6
$a0 = $intro                                  # first real-time block starts here
$a1 = $a0 + $t0
$b1 = $a1 + ($seg / $Speed)
$c1 = $b1 + ($tEnd - $t1)

function Num([double]$v) { return $v.ToString('0.###', [Globalization.CultureInfo]::InvariantCulture) }

function Caption([string]$text, [double]$from, [double]$to) {
    $s = "drawtext=fontfile=segoeuib.ttf:text='{0}':fontcolor=0xEDEDF2:fontsize=30:x=(w-text_w)/2:y=650:" +
         "enable='between(t,{1},{2})'"
    return ($s -f $text, (Num $from), (Num $to))
}

$captions = @(
    (Caption 'Paste a name - no link needed'        ($a0 + 0.4) ($a1 - 0.15)),
    (Caption 'Downloads, tags and embeds the cover' $a1          $b1),
    (Caption 'Filed as Artist - Title.mp3'          ($b1 + 0.2)  ($b1 + ($tSites - $t1))),
    (Caption '1,750+ sites, not just YouTube'       ($b1 + ($tSites - $t1) + 0.3) ($c1 - 0.2))
) -join ','

$filter = "[0:v]scale=1280:720,setsar=1[a];" +
          "[1:v]scale=-2:620,setsar=1,split=3[p0][p1][p2];" +
          "[p0]trim=start=0:end=$(Num $t0),setpts=PTS-STARTPTS[v0];" +
          "[p1]trim=start=$(Num $t0):end=$(Num $t1),setpts=(PTS-STARTPTS)/$Speed[v1];" +
          "[p2]trim=start=$(Num $t1),setpts=PTS-STARTPTS[v2];" +
          "[v0][v1][v2]concat=n=3:v=1:a=0,pad=1280:720:(ow-iw)/2:0:color=0x0A0A0D[body];" +
          "[2:v]scale=1280:720,setsar=1[c];" +
          "[a][body][c]concat=n=3:v=1:a=0,fps=24,$captions,format=yuv420p[v]"

$out = Join-Path $assets 'demo.mp4'
Push-Location $work
try {
    & $ffmpeg -y -hide_banner -loglevel error `
        -loop 1 -t 2.6 -i (Join-Path $assets 'card-intro.png') `
        -i $raw `
        -loop 1 -t 3.2 -i (Join-Path $assets 'card-outro.png') `
        -filter_complex $filter -map '[v]' `
        -c:v libx264 -crf 20 -preset slow -movflags +faststart $out
    $code = $LASTEXITCODE
}
finally {
    Pop-Location
}
if ($code -ne 0) { throw "ffmpeg failed ($code)" }

Write-Output "demo.mp4 $([math]::Round((Get-Item $out).Length / 1MB, 2)) MB"
