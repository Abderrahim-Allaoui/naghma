# Selling Naghma Pro

Practical playbook. Everything here is already wired in the code — this is the
business side: how a payment turns into an activated copy.

---

## 1. What you are selling

| | Free | Pro |
|---|---|---|
| Downloads, tags, cover art | ✅ | ✅ |
| MP3/M4A/OPUS/FLAC, MP4 720p | ✅ | ✅ |
| Batch queue | – | ✅ |
| Playlist / album | – | ✅ |
| 1080p / 4K | – | ✅ |
| `Artist/Album/Title` organizing | – | ✅ |
| Duplicate detection | – | ✅ |

**Price: $12 one-time.** One key, offline, no expiry. No subscription — that is the
selling point against every SaaS in this niche.

---

## 2. The payment → key pipeline

The app ships only a **public** ECDSA key. The private key lives in `private/private-key.txt`
(git-ignored — back it up, lose it and you cannot issue keys anymore).

```powershell
# one key per buyer
dotnet run --project private/LicenseTool -- issue buyer@mail.com
# -> NAGHMA-eyJlIjoidGVzdEBuYWdobWEuYXBwIi...  (158 chars)
```

Send that string to the buyer. They paste it in **Settings → License key → Activate**.
Verification is 100 % offline: nothing to host, nothing to maintain, no piracy of your
licence server (there is none).

### Store options

| Store | Fee | Notes |
|---|---|---|
| **Gumroad** | 10 % | Fastest to set up, licence keys can be delivered as a file/text automatically |
| **Lemon Squeezy** | 5 % + 50¢ | Merchant of record, handles EU VAT |
| **Paddle** | 5 % + 50¢ | Merchant of record, stricter review |
| **Ko-fi shop** | 5 % | No automated key delivery |

Read the acceptable-use policy of whichever you pick **before** uploading: several
processors classify download utilities as restricted. Gumroad is the most permissive of
the four in practice.

### Automating key delivery

1. Create the product on Gumroad, price $12.
2. Add a "content" field telling the buyer they will receive a key by email within 24 h.
3. When an order arrives, run `LicenseTool issue <email>` and reply.
4. At >10 sales/day, script it: Gumroad `sale` webhook → run `LicenseTool` → email the key.

---

## 3. Store page copy (ready to paste)

> **Naghma — your music library, finally organised.**
>
> Paste a link from YouTube, SoundCloud, Bandcamp, TikTok or 1,750+ other sites.
> Naghma downloads the audio, writes the tags, embeds the cover, and files everything
> under `Artist / Album / Title`. Batch a whole playlist, skip what you already own,
> export an M3U to your phone.
>
> Free forever for single downloads. **Pro is $12 once** — batch queue, playlists,
> 1080p/4K, auto-organising, duplicate detection. No subscription, no account,
> works offline, one key for life.
>
> Windows 10/11 · portable · no installer · English / العربية / Français

Screenshots: `assets/screenshot-download.png`, `assets/screenshot-sites.png`.

---

## 4. Where the sales actually come from

The GitHub repo is the funnel, not the store. Realistic channels, in order of effort/reward:

1. **A 60-second demo video** (screen recording of paste → download → file lands in
   `E:\Music`), posted to TikTok / Reels / Shorts with a link in bio. This niche converts
   on video, not on text.
2. **Reddit** — r/software, r/opensource, r/DataHoarder, r/musichoarder. Read each sub's
   self-promotion rules first; most require participation before linking.
3. **Product Hunt** launch (free tier). Prepare 3 screenshots + a GIF.
4. **YouTube tutorial** ("how to organize a 5000-song library") with the app on screen.
5. **Arabic content** — nobody covers this niche in Arabic. A darija/عربية video is the
   cheapest differentiation you have.

Expected reality: **10-50 sales/month at $12 = $120-600/month**, and only with consistent
posting. Without marketing the number is zero, regardless of how good the app is.

---

## 5. Support load to expect

- "It says engine not ready" → network blocked, or the 109 MB ffmpeg download failed.
  `Settings → Update engine` retries.
- "YouTube stopped working" → yt-dlp update. Ship a new build weekly; the engine
  self-updates from the Update button, so most users never need a new app version.
- "My key does not work" → they copied a line break. The parser strips spaces and
  newlines, so it is almost always a truncated paste.

Keep an FAQ in the repo and link it from the store page — it removes ~half the tickets.

---

## 6. Before the first sale

- [ ] Back up `private/private-key.txt` somewhere offline.
- [ ] Bump `Version` in `Naghma.csproj`, update `CHANGELOG.md`, tag `vX.Y.Z`.
- [ ] Set `ProUrl` in `AppSettings` to the real store URL (it defaults to the repo).
- [ ] Test one key end-to-end: issue → activate → Pro features unlocked → restart keeps Pro.
- [ ] Put the store link in the README, the repo description and the release notes.
