# Builds the store artwork for Naghma:
#   assets/cover.png     1280x720  (Gumroad / Lemon Squeezy product cover)
#   assets/og.png        1200x630  (social preview)
#
# Pure GDI+, no external dependencies. Run from anywhere:
#   powershell -ExecutionPolicy Bypass -File scripts\make-cover.ps1

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$root = Split-Path -Parent $PSScriptRoot
$assets = Join-Path $root 'assets'

function New-RoundedPath([float]$x, [float]$y, [float]$w, [float]$h, [float]$r) {
    $p = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = $r * 2
    $p.AddArc($x, $y, $d, $d, 180, 90)
    $p.AddArc($x + $w - $d, $y, $d, $d, 270, 90)
    $p.AddArc($x + $w - $d, $y + $h - $d, $d, $d, 0, 90)
    $p.AddArc($x, $y + $h - $d, $d, $d, 90, 90)
    $p.CloseFigure()
    return $p
}

function New-Font([string]$name, [float]$size, [System.Drawing.FontStyle]$style = [System.Drawing.FontStyle]::Regular) {
    try { return New-Object System.Drawing.Font($name, $size, $style, [System.Drawing.GraphicsUnit]::Pixel) }
    catch { return New-Object System.Drawing.Font('Arial', $size, $style, [System.Drawing.GraphicsUnit]::Pixel) }
}

function Draw-Chip($g, [string]$text, [float]$x, [float]$y, $font) {
    $size = $g.MeasureString($text, $font)
    $w = $size.Width + 26
    $h = 34
    $path = New-RoundedPath $x $y $w $h 17
    $fill = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 22, 22, 30))
    $pen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255, 46, 46, 58), 1)
    $g.FillPath($fill, $path)
    $g.DrawPath($pen, $path)
    $brush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 156, 156, 172))
    $g.DrawString($text, $font, $brush, $x + 13, $y + 9)
    $fill.Dispose(); $pen.Dispose(); $brush.Dispose(); $path.Dispose()
    return $w
}

function Draw-Screenshot($g, [string]$file, [float]$x, [float]$y, [float]$w) {
    $img = [System.Drawing.Image]::FromFile($file)
    $h = $w * $img.Height / $img.Width

    # soft shadow
    for ($i = 6; $i -ge 1; $i--) {
        $a = 12
        $sp = New-RoundedPath ($x - $i) ($y - $i + 10) ($w + $i * 2) ($h + $i * 2) 18
        $sb = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb($a, 0, 0, 0))
        $g.FillPath($sb, $sp)
        $sb.Dispose(); $sp.Dispose()
    }

    $clip = New-RoundedPath $x $y $w $h 14
    $g.SetClip($clip)
    $g.DrawImage($img, [float]$x, [float]$y, [float]$w, [float]$h)
    $g.ResetClip()

    $pen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255, 52, 52, 66), 1.5)
    $g.DrawPath($pen, $clip)
    $pen.Dispose(); $clip.Dispose(); $img.Dispose()
    return $h
}

function New-Canvas([int]$w, [int]$h) {
    $bmp = New-Object System.Drawing.Bitmap($w, $h, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit
    $g.Clear([System.Drawing.Color]::FromArgb(255, 10, 10, 13))

    # diagonal violet wash
    $rect = New-Object System.Drawing.Rectangle(0, 0, $w, $h)
    $lg = New-Object System.Drawing.Drawing2D.LinearGradientBrush($rect,
        [System.Drawing.Color]::FromArgb(70, 124, 58, 237),
        [System.Drawing.Color]::FromArgb(0, 10, 10, 13), 35.0)
    $g.FillRectangle($lg, $rect)
    $lg.Dispose()

    # glow behind the mark
    $glow = New-Object System.Drawing.Drawing2D.GraphicsPath
    $glow.AddEllipse(-260, -300, 900, 900)
    $pgb = New-Object System.Drawing.Drawing2D.PathGradientBrush($glow)
    $pgb.CenterColor = [System.Drawing.Color]::FromArgb(90, 124, 58, 237)
    $pgb.SurroundColors = @([System.Drawing.Color]::FromArgb(0, 124, 58, 237))
    $g.FillPath($pgb, $glow)
    $pgb.Dispose(); $glow.Dispose()

    return @{ Bitmap = $bmp; Graphics = $g }
}

function Draw-Mark($g, [float]$x, [float]$y, [float]$size) {
    $icon = [System.Drawing.Image]::FromFile((Join-Path $assets 'icon.png'))
    $clip = New-RoundedPath $x $y $size $size ($size * 0.24)
    $g.SetClip($clip)
    $g.DrawImage($icon, [float]$x, [float]$y, [float]$size, [float]$size)
    $g.ResetClip()
    $clip.Dispose(); $icon.Dispose()
}

# ---------------------------------------------------------------- 1280 x 720
$c = New-Canvas 1280 720
$g = $c.Graphics

Draw-Mark $g 80 72 64
$fWord = New-Font 'Segoe UI' 30 ([System.Drawing.FontStyle]::Bold)
$bWhite = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 237, 237, 242))
$g.DrawString('Naghma', $fWord, $bWhite, 158, 86)

$fHead = New-Font 'Segoe UI' 46 ([System.Drawing.FontStyle]::Bold)
$bMuted = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 156, 156, 172))
$g.DrawString('Your music library,', $fHead, $bWhite, 74, 188)
$g.DrawString('finally organized.', $fHead, $bWhite, 74, 244)

$fSub = New-Font 'Segoe UI' 19
$g.DrawString('Paste a link. Audio, tags, cover art and', $fSub, $bMuted, 78, 332)
$g.DrawString('Artist/Album folders are handled for you.', $fSub, $bMuted, 78, 360)

$fChip = New-Font 'Segoe UI' 14
$x = 78
foreach ($t in @('Windows 10 / 11', '1,750+ sites', 'EN / AR / FR')) {
    $x += (Draw-Chip $g $t $x 414 $fChip) + 10
}

$h = Draw-Screenshot $g (Join-Path $assets 'screenshot-download.png') 528 92 812

$fFoot = New-Font 'Segoe UI' 16
$bDim = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 120, 120, 136))
$g.DrawString('Free core. Pro $6 once.', $fFoot, $bDim, 78, 486)
$g.DrawString('No account, no telemetry.', $fFoot, $bDim, 78, 512)

$fUrl = New-Font 'Segoe UI' 14
$g.DrawString('github.com/Abderrahim-Allaoui/naghma', $fUrl, $bDim, 78, 652)

$out = Join-Path $assets 'cover.png'
$c.Bitmap.Save($out, [System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose(); $c.Bitmap.Dispose()
"cover.png  $((Get-Item $out).Length) bytes"

# ---------------------------------------------------------------- 1200 x 630
$c = New-Canvas 1200 630
$g = $c.Graphics
Draw-Mark $g 72 60 56
$fWord2 = New-Font 'Segoe UI' 27 ([System.Drawing.FontStyle]::Bold)
$g.DrawString('Naghma', $fWord2, $bWhite, 142, 72)

$fHead2 = New-Font 'Segoe UI' 42 ([System.Drawing.FontStyle]::Bold)
$g.DrawString('Your music library,', $fHead2, $bWhite, 68, 176)
$g.DrawString('finally organized.', $fHead2, $bWhite, 68, 226)
$fSub2 = New-Font 'Segoe UI' 18
$g.DrawString('Download, tag and organize.', $fSub2, $bMuted, 72, 302)
$g.DrawString('Portable Windows app.', $fSub2, $bMuted, 72, 328)

$fChip2 = New-Font 'Segoe UI' 14
$x = 72
foreach ($t in @('1,750+ sites', 'Artist/Album', 'EN / AR / FR')) {
    $x += (Draw-Chip $g $t $x 382 $fChip2) + 10
}

Draw-Screenshot $g (Join-Path $assets 'screenshot-download.png') 560 96 590 | Out-Null

$out = Join-Path $assets 'og.png'
$c.Bitmap.Save($out, [System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose(); $c.Bitmap.Dispose()
"og.png     $((Get-Item $out).Length) bytes"

# ---------------------------------------------------------------- demo cards
# Two 1280x720 title cards used as the intro/outro of assets/demo.mp4.
function New-CenteredString($g, [string]$text, $font, $brush, [float]$y, [int]$width) {
    $size = $g.MeasureString($text, $font)
    $g.DrawString($text, $font, $brush, ($width - $size.Width) / 2, $y)
}

$c = New-Canvas 1280 720
$g = $c.Graphics
Draw-Mark $g 576 214 128
$fBig = New-Font 'Segoe UI' 76 ([System.Drawing.FontStyle]::Bold)
New-CenteredString $g 'Naghma' $fBig $bWhite 372 1280
$fTag = New-Font 'Segoe UI' 26
New-CenteredString $g 'Your music library, finally organized.' $fTag $bMuted 480 1280
$out = Join-Path $assets 'card-intro.png'
$c.Bitmap.Save($out, [System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose(); $c.Bitmap.Dispose()
"card-intro.png"

$c = New-Canvas 1280 720
$g = $c.Graphics
$fBig2 = New-Font 'Segoe UI' 54 ([System.Drawing.FontStyle]::Bold)
New-CenteredString $g 'Free for Windows' $fBig2 $bWhite 236 1280
$fLine = New-Font 'Segoe UI' 28
$bAccent = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 196, 181, 253))
New-CenteredString $g 'github.com/Abderrahim-Allaoui/naghma' $fLine $bAccent 330 1280
$fSmall = New-Font 'Segoe UI' 22
New-CenteredString $g 'Pro is $6 once - unlimited batch, playlists, 4K, auto-organizing.' $fSmall $bMuted 400 1280
$out = Join-Path $assets 'card-outro.png'
$c.Bitmap.Save($out, [System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose(); $c.Bitmap.Dispose()
"card-outro.png"
