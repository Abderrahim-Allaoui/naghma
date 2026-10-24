# Contributing to Naghma

Thanks for taking the time to help. This document is short on purpose.

## Before you start

- **Bugs**: open an issue with the template. Include the app version (`Settings → About`),
  the URL or search term, and the last ~30 lines of the log panel.
- **Features**: open an issue first — a 2-line description is enough. We will tell you if it
  belongs in the free core or in Pro before you write code.

## Setup

```powershell
git clone https://github.com/Abderrahim-Allaoui/naghma.git
cd naghma
dotnet build src/Naghma/Naghma.csproj -c Release
dotnet run --project src/Naghma/Naghma.csproj
```

Requirements: .NET 8 SDK, Windows 10+. `yt-dlp` and `ffmpeg` are downloaded automatically on
first run into `%LOCALAPPDATA%\Naghma\bin`.

## Code guidelines

- One feature per PR. No drive-by refactors.
- C# 12, nullable enabled, `async` for anything that touches the disk or a process.
- UI strings never hardcoded: add a key to `locales/en.json`, then `ar.json` / `fr.json`.
- Keep the engine layer (`src/Naghma/Engine`) free of UI references — it is unit-testable.

## Translations

Copy `locales/en.json` to `locales/<code>.json`, translate the values (not the keys), and open
a PR. Right-to-left languages are detected automatically from the `rtl: true` flag in the file.

## Commit style

```
feat(ui): add queue reordering
fix(engine): parse progress when yt-dlp prints ANSI colors
docs: clarify Pro activation
```

## License

By contributing you agree that your code is licensed under the MIT license, and that you have
the right to submit it.
