# Security

## Reporting

Send a private report via GitHub Security Advisories
(**Security → Report a vulnerability**) instead of a public issue.

You can expect an acknowledgement within 72 hours and a fix or a mitigation plan within 14 days.

## Scope

- The application binary (`Naghma.exe`) and its update mechanism.
- The license key verification (offline HMAC).
- Anything that could execute code from untrusted input (URLs, playlist files, M3U export).

## Not in scope

- Content downloaded by the engine — Naghma does not host or distribute media.
- Third-party components: report to [yt-dlp](https://github.com/yt-dlp/yt-dlp/security) or
  [FFmpeg](https://ffmpeg.org/security.html) directly.
