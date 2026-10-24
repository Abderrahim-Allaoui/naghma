# Dev helper: record assets/demo.mp4 - a real download captured with gdigrab,
# then encoded with intro/outro cards. The download itself is played 8x faster
# so the clip stays around 30 seconds.
#
#   powershell -ExecutionPolicy Bypass -File scripts\dev-demo.ps1

param(
    [ValidateSet('dark', 'light')][string]$Theme = 'light',
    [string]$Target = 'Daft Punk - Instant Crush',
    [string]$Exe = "$PSScriptRoot\..\src\Naghma\bin\Release\net8.0-windows\Naghma.exe",
    [int]$Fps = 12,
    [int]$Speed = 8,
    [int]$TimeoutSec = 300
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

Add-Type @'
using System;
using System.Runtime.InteropServices;
public class Win32Demo {
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr hWnd, IntPtr after, int x, int y, int cx, int cy, uint flags);
    [DllImport("dwmapi.dll")] public static extern int DwmGetWindowAttribute(IntPtr hWnd, int attr, out RECT value, int size);
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
}
'@

$root = Split-Path -Parent $PSScriptRoot
$assets = Join-Path $root 'assets'
$work = Join-Path $env:TEMP 'naghma-demo'
if (Test-Path $work) { Remove-Item $work -Recurse -Force }
New-Item -ItemType Directory -Force -Path $work | Out-Null
$raw = Join-Path $work 'raw.mp4'

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
$settings | ConvertTo-Json -Depth 6 | Set-Content -Path $settingsFile -Encoding UTF8

Get-Process Naghma -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 400
if (Test-Path 'E:\Music\.archive.txt') { Remove-Item 'E:\Music\.archive.txt' -Force }

$AE = [System.Windows.Automation.AutomationElement]
$proc = Start-Process -FilePath (Resolve-Path $Exe) -PassThru
$deadline = (Get-Date).AddSeconds($TimeoutSec)
while ($proc.MainWindowHandle -eq [IntPtr]::Zero -and (Get-Date) -lt $deadline) { Start-Sleep -Milliseconds 250; $proc.Refresh() }
if ($proc.MainWindowHandle -eq [IntPtr]::Zero) { throw 'window never appeared' }

$hwnd = $proc.MainWindowHandle
[Win32Demo]::SetWindowPos($hwnd, [IntPtr](-1), 320, 140, 0, 0, 0x0045) | Out-Null   # TOPMOST, NOSIZE|SHOWWINDOW|NOACTIVATE? keep simple
[Win32Demo]::SetForegroundWindow($hwnd) | Out-Null
Start-Sleep -Milliseconds 800

$rect = New-Object Win32Demo+RECT
if ([Win32Demo]::DwmGetWindowAttribute($hwnd, 9, [ref]$rect, 16) -ne 0) {
    [Win32Demo]::GetWindowRect($hwnd, [ref]$rect) | Out-Null
}
$w = $rect.Right - $rect.Left
$h = $rect.Bottom - $rect.Top
# libx264 needs even dimensions
if ($w % 2 -ne 0) { $w-- }
if ($h % 2 -ne 0) { $h-- }
Write-Output "window ${w}x${h} at $($rect.Left),$($rect.Top)"

# --- start the recorder -------------------------------------------------------
$ffmpeg = Join-Path $env:LOCALAPPDATA 'Naghma\bin\ffmpeg\ffmpeg.exe'
if (!(Test-Path $ffmpeg)) { $ffmpeg = (Get-Command ffmpeg -ErrorAction Stop).Source }

$psi = New-Object System.Diagnostics.ProcessStartInfo
$psi.FileName = $ffmpeg
$psi.Arguments = "-y -hide_banner -loglevel error -f gdigrab -framerate $Fps -draw_mouse 0 -offset_x $($rect.Left) -offset_y $($rect.Top) -video_size ${w}x${h} -i desktop -c:v libx264 -preset ultrafast -crf 16 -pix_fmt yuv420p `"$raw`""
$psi.RedirectStandardInput = $true
$psi.UseShellExecute = $false
$psi.CreateNoWindow = $true
$rec = [System.Diagnostics.Process]::Start($psi)
Start-Sleep -Milliseconds 1200
$clock = [System.Diagnostics.Stopwatch]::StartNew()
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
            Start-Sleep -Milliseconds 800
            return $true
        }
    }
    return $false
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
Start-Sleep -Milliseconds 900                       # a beat on the empty window
$input.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue($Target)
Start-Sleep -Milliseconds 1200

$download = $null
foreach ($b in (Find-All ([System.Windows.Automation.ControlType]::Button))) {
    if ($b.Current.Name -match 'Download|T.l.charger') { $download = $b; break }
}
if (-not $download) { throw 'download button not found' }

$t0 = $clock.Elapsed.TotalSeconds
$download.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
Write-Output 'downloading...'

$sw = [System.Diagnostics.Stopwatch]::StartNew()
while (-not $download.Current.IsEnabled -and $sw.Elapsed.TotalSeconds -lt $TimeoutSec) { Start-Sleep -Milliseconds 250 }
$t1 = $clock.Elapsed.TotalSeconds
Write-Output "download took $([int]$sw.Elapsed.TotalSeconds)s"
Start-Sleep -Milliseconds 1800                      # the finished state

if (Select-Tab 'Sites') { Start-Sleep -Milliseconds 2600 }
if (Select-Tab 'Settings') { Start-Sleep -Milliseconds 2200 }
Select-Tab 'Download' | Out-Null
Start-Sleep -Milliseconds 900

$tEnd = $clock.Elapsed.TotalSeconds

# --- stop the recorder --------------------------------------------------------
try { $rec.StandardInput.WriteLine('q') } catch { }
if (-not $rec.WaitForExit(20000)) { $rec.Kill() }
[Win32Demo]::SetWindowPos($hwnd, [IntPtr](-2), 0, 0, 0, 0, 0x0043) | Out-Null
$proc.CloseMainWindow() | Out-Null
Start-Sleep -Milliseconds 500
if (!$proc.HasExited) { $proc.Kill() }

Write-Output ("timeline: click {0:N1}s, done {1:N1}s, end {2:N1}s" -f $t0, $t1, $tEnd)
if (!(Test-Path $raw)) { throw 'recording missing' }

# A long download is boring; a short one is worth showing in real time.
$seg = $t1 - $t0
$Speed = [Math]::Max(1, [Math]::Round($seg / 4))
Write-Output "download segment $([int]$seg)s, speed ${Speed}x"

# --- encode -------------------------------------------------------------------
$out = Join-Path $assets 'demo.mp4'
$filter = "[0:v]scale=1280:720,setsar=1[a];" +
          "[1:v]scale=-2:720,setsar=1,split=3[p0][p1][p2];" +
          "[p0]trim=start=0:end=${t0},setpts=PTS-STARTPTS[v0];" +
          "[p1]trim=start=${t0}:end=${t1},setpts=(PTS-STARTPTS)/$Speed[v1];" +
          "[p2]trim=start=${t1},setpts=PTS-STARTPTS[v2];" +
          "[v0][v1][v2]concat=n=3:v=1:a=0,pad=1280:720:(ow-iw)/2:(oh-ih)/2:color=0x0A0A0D[body];" +
          "[2:v]scale=1280:720,setsar=1[c];" +
          "[a][body][c]concat=n=3:v=1:a=0,fps=24,format=yuv420p[v]"

& $ffmpeg -y -hide_banner -loglevel error `
    -loop 1 -t 2.6 -i (Join-Path $assets 'card-intro.png') `
    -i $raw `
    -loop 1 -t 3.2 -i (Join-Path $assets 'card-outro.png') `
    -filter_complex $filter -map '[v]' `
    -c:v libx264 -crf 20 -preset slow -movflags +faststart $out

if ($LASTEXITCODE -ne 0) { throw "ffmpeg failed ($LASTEXITCODE)" }
& $ffmpeg -hide_banner -i $out 2>&1 | Select-String 'Duration' | ForEach-Object { $_.Line.Trim() } | Out-Null
Write-Output "demo.mp4 $([math]::Round((Get-Item $out).Length / 1MB, 2)) MB"
