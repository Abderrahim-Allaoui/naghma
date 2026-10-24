# Build & release

## Requirements

- .NET 8 SDK (x64)
- Windows 10/11 for the WinForms target
- Optional: `Inno Setup` if you also want an installer (the portable ZIP is the primary artifact)

## Local build

```powershell
dotnet build src/Naghma/Naghma.csproj -c Release
```

Binary: `src/Naghma/bin/Release/net8.0-windows/Naghma.exe`

## Portable single file

```powershell
dotnet publish src/Naghma/Naghma.csproj -c Release -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:EnableCompressionInSingleFile=true `
  -p:DebugType=none `
  -o publish
```

Expected: `publish/Naghma.exe` (~40-60 MB, self-contained, no runtime required).

## Engine binaries

`yt-dlp.exe` and `ffmpeg.exe` are **not** committed. At first launch `ToolManager` downloads them
into `%LOCALAPPDATA%\Naghma\bin`:

| Tool | Source | Size |
|---|---|---|
| yt-dlp | `github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe` | ~17 MB |
| ffmpeg | `gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip` | ~109 MB |
| deno | `github.com/denoland/deno/releases/latest/download/deno-x86_64-pc-windows-msvc.zip` | ~41 MB |

If `ffmpeg.exe` is already on the user's `PATH`, Naghma uses it and skips the 109 MB download.

`scripts/fetch-tools.ps1` does the same thing offline for testing a fresh install.

## Versioning

`Directory.Build.props` holds `<Version>`. Bump it, update `CHANGELOG.md`, tag `vX.Y.Z`.
The GitHub Actions workflow builds the ZIP and attaches it to the release.

## License keys (Pro)

Pro keys are offline **ECDSA P-256** signatures. Only the public key ships inside the app,
so a key cannot be forged even if the binary is decompiled.

```
NAGHMA-<base64url(payload)>.<base64url(signature)>
payload = {"e":"buyer@mail.com","t":1735689600,"p":"pro"}
```

Issue a key with the private tool (never committed, `private/` is git-ignored):

```powershell
dotnet run --project private/LicenseTool -- keygen            # once, prints the public key
dotnet run --project private/LicenseTool -- issue buyer@mail  # one key per sale
```

Paste the printed public key into `src/Naghma/Core/License.cs` (`PublicKeySpki`). Verification
is fully offline: no server call, no expiry, works forever.
