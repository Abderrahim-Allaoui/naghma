# Naghma on Android

The phone app lives in `android/`. Same engine as the desktop build (yt-dlp + ffmpeg),
same licence key, different packaging.

## What it does today

| | |
|---|---|
| Input | one link or one song name per line (Pro: many) |
| Formats | MP3, M4A, OPUS, FLAC, MP4 |
| Output | `Music/Naghma/` in the public library (MediaStore), so any player sees it |
| Pro | batch queue, FLAC, 4K video, no ads |
| Ads | one rewarded ad = 24 h of Pro |
| Key | the **same** `NAGHMA-…` key as the Windows app, verified offline (ECDSA P-256) |
| Languages | English, العربية (RTL), Français |

## Play Store: not for this app

Google Play's Developer Program Policy, *Encouraging Infringement of Copyright*:

> "Streaming apps that allow users to download a local copy of copyrighted content
> without authorization."

A downloader cannot be listed, whatever the store description says. Two honest routes:

1. **Sideload / GitHub (what this build is for).** Ship `Naghma-<version>.apk` on the
   GitHub release next to the Windows zip, plus a "Get it for Android" button on the site.
   F-Droid is also an option later.
2. **A separate Play-safe app.** An *organizer* — reads the MP3s already on the phone,
   fixes tags, fetches cover art, renames to `Artist/Album/01 - Title`, dedupes, exports
   M3U. No downloading, so Play accepts it. Needs a Play account ($25), ID verification,
   and a closed test with 12 testers for 14 days before production.

## Ads

AdMob, two units:

1. **Banner** at the bottom of the screen — shows only in Free, removed when Pro is active (bought key or 24h rewarded ad).
2. **Rewarded** ("Watch an ad — 24h Pro") — grants 24 hours of Pro.

The build uses Google's **test** ad units so it works without an AdMob account:

| Unit | Test ID |
|---|---|
| App ID | `ca-app-pub-3940256099942544~3347511713` |
| Banner | `ca-app-pub-3940256099942544/6300978111` |
| Rewarded | `ca-app-pub-3940256099942544/5224354917` |

To go live, create an AdMob account, add `app.naghma.mobile`, create a Banner + a Rewarded unit, then swap the IDs in:

- `AndroidManifest.xml` → `com.google.android.gms.ads.APPLICATION_ID`
- `activity_main.xml` → `ads:adUnitId` on the `AdView`
- `MainActivity.java` → `AD_UNIT` constant

AdMob pays out from $100/month to a bank account or Payoneer. Rewarded ads on a sideloaded APK are allowed; just keep ads off the download flow itself (banner only, no forced clicks).


## Build

```powershell
cd android
$env:JAVA_HOME = "C:\Program Files\Android\Android Studio\jbr"
.\gradlew.bat :app:assembleDebug
# -> app\build\outputs\apk\debug\app-debug.apk
```

Install on a connected phone or the `Pixel_7` emulator:

```powershell
& "$env:LOCALAPPDATA\Android\Sdk\platform-tools\adb.exe" install -r app\build\outputs\apk\debug\app-debug.apk
```

Release build (needs a keystore, keep it next to the desktop signing key in `E:\Backups\Naghma`):

```powershell
keytool -genkeypair -v -keystore naghma-android.jks -alias naghma -keyalg RSA -keysize 4096 -validity 10000
.\gradlew.bat :app:assembleRelease
```

## Layout

```
android/app/src/main/java/app/naghma/mobile/
  App.java           engine bootstrap (yt-dlp + ffmpeg extract on first run)
  Engine.java        download loop, options, MediaStore publish
  Pro.java           key verification + rewarded-ad state
  MainActivity.java  UI, ads, gating
```
