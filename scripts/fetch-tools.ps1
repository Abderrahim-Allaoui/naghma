# Pre-downloads the engine binaries into %LOCALAPPDATA%\Naghma\bin
# (same layout the app creates itself on first launch - handy for dev and for a
# fully offline "portable" build).
param(
    [string]$Dest = "$env:LOCALAPPDATA\Naghma\bin"
)

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

New-Item -ItemType Directory -Force -Path $Dest | Out-Null

function Get-File($url, $out) {
    if (Test-Path $out) { Write-Host "  exists: $(Split-Path -Leaf $out)"; return }
    Write-Host "  downloading $(Split-Path -Leaf $out)..."
    $tmp = "$out.part"
    curl.exe -L --fail --noproxy "*" -o $tmp $url
    if ($LASTEXITCODE -ne 0) { throw "download failed: $url" }
    Move-Item -Force $tmp $out
}

Get-File "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe" (Join-Path $Dest 'yt-dlp.exe')

$ffmpegExe = Join-Path $Dest 'ffmpeg\ffmpeg.exe'
if (!(Test-Path $ffmpegExe)) {
    $zip = Join-Path $Dest 'ffmpeg.zip'
    Get-File "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip" $zip
    $tmpDir = Join-Path $Dest 'ffmpeg.tmp'
    if (Test-Path $tmpDir) { Remove-Item -Recurse -Force $tmpDir }
    Expand-Archive -Path $zip -DestinationPath $tmpDir -Force
    $found = Get-ChildItem -Recurse -Filter ffmpeg.exe $tmpDir | Select-Object -First 1
    New-Item -ItemType Directory -Force -Path (Split-Path $ffmpegExe) | Out-Null
    Copy-Item $found.FullName $ffmpegExe -Force
    $probe = Join-Path $found.DirectoryName 'ffprobe.exe'
    if (Test-Path $probe) { Copy-Item $probe (Join-Path $Dest 'ffmpeg\ffprobe.exe') -Force }
    Remove-Item -Recurse -Force $tmpDir
    Remove-Item -Force $zip
}

$deno = Join-Path $Dest 'deno.exe'
if (!(Test-Path $deno)) {
    $zip = Join-Path $Dest 'deno.zip'
    Get-File "https://github.com/denoland/deno/releases/latest/download/deno-x86_64-pc-windows-msvc.zip" $zip
    Expand-Archive -Path $zip -DestinationPath $Dest -Force
    Remove-Item -Force $zip
}

Write-Host ""
Write-Host "engine ready in $Dest"
& (Join-Path $Dest 'yt-dlp.exe') --version
