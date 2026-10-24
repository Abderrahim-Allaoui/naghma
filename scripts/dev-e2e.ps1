# Dev helper: end-to-end test through the real UI (UIAutomation).
#   .\dev-e2e.ps1 -Target "Daft Punk - Instant Crush"
param(
    [string]$Target = "Daft Punk - Instant Crush",
    [string]$Exe = "$PSScriptRoot\..\src\Naghma\bin\Release\net8.0-windows\Naghma.exe",
    [string]$Shot = "$PSScriptRoot\..\assets\screenshot-download.png",
    [int]$TimeoutSec = 300
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

Add-Type @'
using System;
using System.Runtime.InteropServices;
public class Win32E2E {
    [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, uint nFlags);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
}
'@

function Capture($hWnd, $path) {
    $rect = New-Object Win32E2E+RECT
    [Win32E2E]::GetWindowRect($hWnd, [ref]$rect) | Out-Null
    $w = $rect.Right - $rect.Left; $h = $rect.Bottom - $rect.Top
    $bmp = New-Object System.Drawing.Bitmap($w, $h)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $hdc = $g.GetHdc()
    [Win32E2E]::PrintWindow($hWnd, $hdc, 0) | Out-Null
    $g.ReleaseHdc($hdc); $g.Dispose()
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $path) | Out-Null
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
}

Get-Process Naghma -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 500

$AE = [System.Windows.Automation.AutomationElement]
$proc = Start-Process -FilePath (Resolve-Path $Exe) -PassThru
$deadline = (Get-Date).AddSeconds($TimeoutSec)

while ($proc.MainWindowHandle -eq [IntPtr]::Zero -and (Get-Date) -lt $deadline) { Start-Sleep -Milliseconds 300; $proc.Refresh() }
if ($proc.MainWindowHandle -eq [IntPtr]::Zero) { throw "window never appeared" }

[Win32E2E]::SetForegroundWindow($proc.MainWindowHandle) | Out-Null
Start-Sleep -Milliseconds 800

$root = $AE::FromHandle($proc.MainWindowHandle)

function Find-All($type) {
    $cond = New-Object System.Windows.Automation.PropertyCondition($AE::ControlTypeProperty, $type)
    return $root.FindAll([System.Windows.Automation.TreeScope]::Descendants, $cond)
}

$edits = $null
for ($try = 0; $try -lt 25; $try++) {
    $edits = Find-All ([System.Windows.Automation.ControlType]::Edit)
    if ($edits.Count -gt 0) { break }
    Start-Sleep -Milliseconds 400
}
if (-not $edits -or $edits.Count -eq 0) { throw "no edit controls appeared" }
Write-Output "edit controls: $($edits.Count)"

# the multi-line input is the tallest edit box
$input = $edits[0]
$best = 0
foreach ($e in $edits) {
    $r = $e.Current.BoundingRectangle
    if ($r.Height -gt $best) { $best = $r.Height; $input = $e }
}
$input.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue($Target)
Write-Output "target set: $Target"

Start-Sleep -Milliseconds 300

$buttons = Find-All ([System.Windows.Automation.ControlType]::Button)
$download = $null
foreach ($b in $buttons) {
    $n = $b.Current.Name
    if ($n -match 'Download|T.l.charger|تحميل') { $download = $b; break }
}
if (-not $download) {
    Write-Output ("buttons seen: " + (($buttons | ForEach-Object { $_.Current.Name }) -join ' | '))
    throw "download button not found"
}

$download.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
Write-Output "clicked: $($download.Current.Name)"

Start-Sleep -Seconds 3

# wait until the button is enabled again (batch finished) or timeout
while ((Get-Date) -lt $deadline) {
    Start-Sleep -Seconds 2
    if ($proc.HasExited) { throw "app exited unexpectedly" }
    if ($download.Current.IsEnabled) { break }
}

Capture $proc.MainWindowHandle $Shot
Write-Output "captured -> $Shot"

$proc.CloseMainWindow() | Out-Null
Start-Sleep -Milliseconds 500
if (!$proc.HasExited) { $proc.Kill() }
