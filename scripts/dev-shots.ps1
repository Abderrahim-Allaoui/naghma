# Dev helper: capture the three tabs of the app for the site and the store page.
# Runs one real download first so the Download tab shows a finished job.
#
#   powershell -ExecutionPolicy Bypass -File scripts\dev-shots.ps1
#   powershell -ExecutionPolicy Bypass -File scripts\dev-shots.ps1 -Theme light

param(
    [ValidateSet('dark', 'light')][string]$Theme = 'dark',
    [string]$Target = 'Daft Punk - Instant Crush',
    [string]$Exe = "$PSScriptRoot\..\src\Naghma\bin\Release\net8.0-windows\Naghma.exe",
    [int]$TimeoutSec = 300
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

Add-Type @'
using System;
using System.Runtime.InteropServices;
public class Win32Shots {
    [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, uint nFlags);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr hWnd, IntPtr after, int x, int y, int cx, int cy, uint flags);
    [DllImport("user32.dll")] public static extern bool RedrawWindow(IntPtr hWnd, IntPtr rect, IntPtr region, uint flags);
    [DllImport("dwmapi.dll")] public static extern int DwmGetWindowAttribute(IntPtr hWnd, int attr, out RECT value, int size);
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
}
'@

$root = Split-Path -Parent $PSScriptRoot
$settingsFile = Join-Path $env:LOCALAPPDATA 'Naghma\settings.json'

# --- flip the theme without touching the rest of the settings -----------------
$settings = @{}
if (Test-Path $settingsFile) {
    $raw = Get-Content $settingsFile -Raw
    if ($raw.Trim()) { $settings = $raw | ConvertFrom-Json }
}
$settings | Add-Member -NotePropertyName DarkTheme -NotePropertyValue ($Theme -eq 'dark') -Force
$settings | ConvertTo-Json -Depth 6 | Set-Content -Path $settingsFile -Encoding UTF8
Write-Output "theme: $Theme"

# --- fresh download so the log is not empty -----------------------------------
Get-Process Naghma -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 500
$archive = 'E:\Music\.archive.txt'
if (Test-Path $archive) { Remove-Item $archive -Force }

function Capture($hWnd, $path) {
    # The window must be the topmost thing on screen, then we copy the pixels
    # the user actually sees (PrintWindow returns a stale surface after a tab switch).
    [Win32Shots]::SetWindowPos($hWnd, [IntPtr](-1), 0, 0, 0, 0, 0x0043) | Out-Null   # HWND_TOPMOST, NOMOVE|NOSIZE|SHOWWINDOW
    [Win32Shots]::SetForegroundWindow($hWnd) | Out-Null
    [Win32Shots]::RedrawWindow($hWnd, [IntPtr]::Zero, [IntPtr]::Zero, 0x0085) | Out-Null  # INVALIDATE|ERASE|UPDATENOW|ALLCHILDREN
    Start-Sleep -Milliseconds 700

    $rect = New-Object Win32Shots+RECT
    if ([Win32Shots]::DwmGetWindowAttribute($hWnd, 9, [ref]$rect, 16) -ne 0) {   # EXTENDED_FRAME_BOUNDS
        [Win32Shots]::GetWindowRect($hWnd, [ref]$rect) | Out-Null
    }
    $w = $rect.Right - $rect.Left; $h = $rect.Bottom - $rect.Top
    $bmp = New-Object System.Drawing.Bitmap($w, $h)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen($rect.Left, $rect.Top, 0, 0, (New-Object System.Drawing.Size($w, $h)))
    $g.Dispose()
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Write-Output "captured $w x $h -> $path"
}

$AE = [System.Windows.Automation.AutomationElement]
$proc = Start-Process -FilePath (Resolve-Path $Exe) -PassThru
$deadline = (Get-Date).AddSeconds($TimeoutSec)
while ($proc.MainWindowHandle -eq [IntPtr]::Zero -and (Get-Date) -lt $deadline) { Start-Sleep -Milliseconds 300; $proc.Refresh() }
if ($proc.MainWindowHandle -eq [IntPtr]::Zero) { throw 'window never appeared' }

[Win32Shots]::SetForegroundWindow($proc.MainWindowHandle) | Out-Null
Start-Sleep -Milliseconds 900

$window = $AE::FromHandle($proc.MainWindowHandle)
function Find-All($type) {
    $cond = New-Object System.Windows.Automation.PropertyCondition($AE::ControlTypeProperty, $type)
    return $window.FindAll([System.Windows.Automation.TreeScope]::Descendants, $cond)
}

function Select-Tab([string]$name) {
    $tabs = Find-All ([System.Windows.Automation.ControlType]::TabItem)
    foreach ($t in $tabs) {
        if ($t.Current.Name.Trim() -eq $name) {
            $t.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select()
            Start-Sleep -Milliseconds 900
            return $true
        }
    }
    Write-Output ("tabs seen: " + (($tabs | ForEach-Object { "'" + $_.Current.Name + "'" }) -join ' | '))
    return $false
}

# --- run one download ---------------------------------------------------------
$edits = $null
for ($try = 0; $try -lt 25; $try++) {
    $edits = Find-All ([System.Windows.Automation.ControlType]::Edit)
    if ($edits.Count -gt 0) { break }
    Start-Sleep -Milliseconds 400
}
$input = $null; $best = 0
foreach ($e in $edits) {
    $r = $e.Current.BoundingRectangle
    if ($r.Height -gt $best) { $best = $r.Height; $input = $e }
}
$input.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue($Target)
Start-Sleep -Milliseconds 300

$download = $null
foreach ($b in (Find-All ([System.Windows.Automation.ControlType]::Button))) {
    if ($b.Current.Name -match 'Download|T.l.charger') { $download = $b; break }
}
if (-not $download) { throw 'download button not found' }
$download.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
Write-Output "downloading: $Target"

Start-Sleep -Seconds 3
while ((Get-Date) -lt $deadline) {
    Start-Sleep -Seconds 2
    if ($download.Current.IsEnabled) { break }
}
Start-Sleep -Milliseconds 800

# --- capture ------------------------------------------------------------------
Select-Tab 'Download' | Out-Null
Capture $proc.MainWindowHandle (Join-Path $root 'assets\screenshot-download.png')

$ok = Select-Tab 'Sites'
Write-Output "sites tab: $ok"
if ($ok) { Capture $proc.MainWindowHandle (Join-Path $root 'assets\screenshot-sites.png') }

$ok = Select-Tab 'Settings'
Write-Output "settings tab: $ok"
if ($ok) { Capture $proc.MainWindowHandle (Join-Path $root 'assets\screenshot-settings.png') }

[Win32Shots]::SetWindowPos($proc.MainWindowHandle, [IntPtr](-2), 0, 0, 0, 0, 0x0043) | Out-Null   # HWND_NOTOPMOST
$proc.CloseMainWindow() | Out-Null
Start-Sleep -Milliseconds 600
if (!$proc.HasExited) { $proc.Kill() }
Write-Output 'done'
