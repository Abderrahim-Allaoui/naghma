# Dev helper: launch Naghma, wait for the window, capture it to a PNG, close it.
param(
    [string]$Exe = "$PSScriptRoot\..\src\Naghma\bin\Release\net8.0-windows\Naghma.exe",
    [string]$Out = "$PSScriptRoot\..\assets\screenshot-download.png",
    [int]$WaitMs = 2500
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

Add-Type @'
using System;
using System.Runtime.InteropServices;
public class Win32Cap {
    [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, uint nFlags);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
}
'@

$exe = (Resolve-Path $Exe).Path
$proc = Start-Process -FilePath $exe -PassThru
Start-Sleep -Milliseconds $WaitMs
$proc.Refresh()

if ($proc.MainWindowHandle -eq [IntPtr]::Zero) {
    throw "no main window (app crashed?)"
}

[Win32Cap]::SetForegroundWindow($proc.MainWindowHandle) | Out-Null
Start-Sleep -Milliseconds 600

$rect = New-Object Win32Cap+RECT
[Win32Cap]::GetWindowRect($proc.MainWindowHandle, [ref]$rect) | Out-Null
$w = $rect.Right - $rect.Left
$h = $rect.Bottom - $rect.Top

$bmp = New-Object System.Drawing.Bitmap($w, $h)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$hdc = $g.GetHdc()
[Win32Cap]::PrintWindow($proc.MainWindowHandle, $hdc, 2) | Out-Null   # 2 = PW_RENDERFULLCONTENT
$g.ReleaseHdc($hdc)
$g.Dispose()

New-Item -ItemType Directory -Force -Path (Split-Path -Parent $Out) | Out-Null
$bmp.Save($Out, [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()

Write-Output "captured ${w}x${h} -> $Out"
$proc.CloseMainWindow() | Out-Null
Start-Sleep -Milliseconds 400
if (!$proc.HasExited) { $proc.Kill() }
