# Changelog

All notable changes to this project are documented here.
Format: [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) · [SemVer](https://semver.org/).

## [Unreleased]

### Added
- Repository structure, CI, i18n scaffolding.

## [0.1.2] - 2026-11-10

### Added
- Ad banner for free users on the Download tab; it disappears the moment a Pro key is
  activated. Links to the store page, localised in EN/AR/FR.
- Android app (`android/`): Java + Material 3, yt-dlp/ffmpeg engine, banner + rewarded ads
  (24 h of Pro), same offline ECDSA key check as the desktop build, EN/AR/FR.
- `verify-key.ps1` / `LicenseTool verify`: re-run the app's exact licence check from the
  command line, so a customer's key can be diagnosed without launching the GUI.

### Changed
- Buy button on the landing page and `BUY_URL` in the Android app point at the live product
  `https://gumroad.com/l/naghma-pro` ($6, published). The old link was a 404.
- Demo video is 19 s (the Explorer payoff shot was dropped; the destination log line is the
  payoff), and the README/site copy follow.
- Light theme: the log box now uses a dedicated palette (`Theme.ForLog`) so entries are
  readable on the dark background.
- Progress percentage is shown in the status bar while a download runs.
- Social cards use absolute `og:image` / `twitter:image` URLs (1200×630).

## [0.1.1] - 2026-10-24

### Added
- Landing page (GitHub Pages) and a 23-second demo video.
- Dark title bar on Windows 10/11 when the dark theme is on.
- `issue-key.ps1`: one command to issue a Pro key, log the sale, and print the delivery email.

### Changed
- Pro price is $6 everywhere (app dialog, site, docs).
- The Pro upsell now opens the product site instead of a GitHub section.
- Status bar and log box are readable in the dark theme.
- Fresh screenshots in both themes.

## [0.1.0] — 2026-10-24

### Added
- Windows GUI: download from any yt-dlp supported site.
- Audio: MP3, M4A, OPUS, FLAC. Video: MP4 (720p free / 1080p-4K Pro).
- Automatic tags and embedded cover art.
- Sites browser (1,750+ extractors) with filter and popular list.
- English, Arabic (RTL) and French interface.
- Portable build, engine auto-download, no installer.
