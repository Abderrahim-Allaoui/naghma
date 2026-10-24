# Launch copy — ready to post

Everything below is written to be pasted as-is. The asset for every channel is
`assets/demo.mp4` (23 s, 1280×720) plus the three screenshots.

Landing page: https://abderrahim-allaoui.github.io/naghma/
Download: https://github.com/Abderrahim-Allaoui/naghma/releases/latest
Pro ($6): https://gumroad.com/l/naghma  ← replace with your real slug

---

## 1. TikTok / Reels / Shorts

Vertical (1080×1920) converts best, but the 16:9 clip works with bars. Hook must land
in the first 1.5 s — no logo intro.

**Hook (text on screen, 0-2 s):**
> Your MP3 folder is a mess. Watch this.

**Script (voice-over or captions, ~25 s):**

| Time | On screen | Caption |
|---|---|---|
| 0-3 s | the messy folder | "4,000 files, no tags, no covers" |
| 3-8 s | paste a name, hit Download | "paste the song name" |
| 8-14 s | log running | "it finds, downloads, tags, covers" |
| 14-18 s | file in `E:\Music` | "file lands as Artist - Title.mp3" |
| 18-23 s | Sites tab | "1,750 sites, not just YouTube" |
| 23-28 s | pricing card | "free. Pro is $6 if you want batch + 4K" |

**Caption:**
> Naghma — download, tag and organize your music library. Free for Windows, link in bio.
> #music #mp3 #windows #techtok #software #musiclibrary

**Darija version (this is the differentiator — nobody covers it in Arabic):**
> واش عندك 4000 ملف mp3 بلا tags ولا covers؟ هاد الأبليكاسيون كتنقي ليك المكتبة.
> كتلصق سميّة الأغنية، كتحمّل، كتكتب الـtags، كتحط الغلاف، وكتنظم كلشي فـ Artist/Album.
> مجانية، و Pro بـ6 دولار. الرابط فالبيو.

---

## 2. Reddit

Post in r/software, r/DataHoarder, r/musichoarder, r/selfhosted. Read the sub rules first;
most want you to be active before you link anything.

**Title:**
> I built a Windows tool that tags and files downloads automatically, because every other downloader leaves you with 4000 untagged MP3s

**Body:**

> The download part is solved — yt-dlp does it better than anything I could write.
> What was missing for me was everything after: correct tags, embedded cover art, and
> `Artist/Album/01 - Title` instead of `Official Video (HD) (1).mp3`.
>
> So I wrapped yt-dlp in a WinForms front-end:
>
> - paste a link or just the song name (it searches YouTube Music)
> - tags + cover embedded, noise like "(Official Music Video)" stripped
> - duplicate detection via a download archive — re-run a playlist, get only the new tracks
> - 1,750+ sites, five formats, MP4 up to 4K
> - English / العربية (full RTL) / Français
> - single portable .exe, no installer, no admin
>
> It's MIT and free for single downloads. Batch queue, playlists, 4K and auto-organising
> are behind a $6 one-time licence (offline key, no account, no subscription) — that's the
> whole business model, and yes the paywall is client-side and trivially bypassable; I'd
> rather ship honest software than a DRM maze.
>
> Windows only for now. Happy to answer anything about the yt-dlp plumbing.

---

## 3. Product Hunt

**Name:** Naghma
**Tagline (60 chars):** `Download, tag and organize your music library — in one click`
**Topics:** Music, Windows, Productivity, Open Source

**Description:**

> Paste a link — or just a song name — and Naghma downloads the audio, writes the tags,
> embeds the cover art, and files everything under Artist / Album / Title.
>
> Most downloaders stop at the download. You end up with thousands of untagged files and
> no idea what you own. Naghma treats the library as the product: correct metadata, sane
> folder structure, duplicate detection, and a playlist export for your phone.
>
> 1,750+ sites via yt-dlp, five formats (MP3, M4A, OPUS, FLAC, MP4 up to 4K), English,
> Arabic (RTL) and French. One portable .exe — no installer, no admin, no account, no
> telemetry.
>
> Free for single downloads. Pro is $6 once, for batch queues, playlists, 4K and
> auto-organising. Offline licence key, no subscription.

**First comment:**

> Maker here. The part I care about is what happens after the download: the file has to be
> named correctly, tagged, and have a cover, or the library is worthless in five years.
> That's where most of the work went.
>
> The engine is yt-dlp (self-updating), so site changes don't need a new app release.
> Windows 10/11 only for now; Android is the most requested thing I get, iOS is off the
> table because there's no way to run a download engine there.

---

## 4. X / Twitter thread

> 1/ Every music downloader gives you the same thing: 4,000 files called "Official Video (HD).mp3".
>    I got tired of it, so I built Naghma. Free, Windows, MIT. 🧵

> 2/ Paste a link — or just the song name. It downloads, writes the tags, embeds the cover,
>    and files it as Artist/Album/01 - Title.

> 3/ Duplicate detection: re-run a 300-song playlist, it only fetches what's new.
>    1,750+ sites, MP3/M4A/OPUS/FLAC, MP4 up to 4K.

> 4/ English, العربية (full RTL), Français. One portable .exe. No installer, no account, no telemetry.

> 5/ Free for single downloads. Pro is $6 once — batch, playlists, 4K, auto-organising.
>    Offline key, works forever.
>    → github.com/Abderrahim-Allaoui/naghma

---

## 5. Posting order that actually works

1. **TikTok first** — cheapest reach, and the video is already made. Post 1/day for a week;
   the algorithm needs volume before it finds your audience.
2. **Reddit second**, once you have something to point at (a few hundred downloads).
3. **Product Hunt last** — it is a one-shot; use it when the page and the app are stable,
   and line up 10 people to comment in the first hour.
4. **YouTube** in parallel: "How to organize a 5,000-song MP3 library" is a search-driven
   video that keeps converting for years, unlike a TikTok.

Rule of thumb for this niche: **video > text**, **Arabic content > English** (no competition),
and **"organized library" sells better than "downloader"** — one is a tool, the other is a
result.
