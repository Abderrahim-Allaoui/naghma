# Gumroad — paste-ready

**Product exists:** `Naghma Pro` on the **`allaoui67`** account, slug **`eydroa`** →
<https://gumroad.com/l/eydroa> (price $6). The account with the **Naghma Font** product
(`arendxstd`) is a *different* account — do not look for the app there.

Open <https://gumroad.com/products/eydroa/edit> to edit it. The remaining sections are
the field-by-field reference for what should be on that page.

> The slug is the auto-generated `eydroa`. If you want a readable one, change it in the
> edit page's **URL** field (`naghma-pro` is free on *this* account) — then update the
> three places that link to it: `index.html`, `MainActivity.java` (`BUY_URL`), `docs/`.

## 1. Basics

| Field | Value |
|---|---|
| Name | `Naghma Pro` |
| Type | Digital product |
| Price | `6` USD, one-time (uncheck "let customers pay what they want") |
| URL | `eydroa` → `https://gumroad.com/l/eydroa` |
| Quantity | unlimited |
| Duration | not applicable |

## 2. Product content (the "Content" tab)

Upload one file, any small file works as the placeholder; the key itself is emailed:

```
naghma-pro-key.txt
---
Your Naghma Pro key is sent to this email address by the seller within 24 hours.

If it has not arrived, reply to your Gumroad receipt with your order number.
Keep the key: it is verified offline and works forever, on every Windows PC you own.
```

Then set **"Send your customers a unique license key"** → *off* (keys are issued manually
for now, see §5).

## 3. Description (Rich text / "Details" tab)

Paste this, it is already plain-text friendly:

```
Paste a link from YouTube, SoundCloud, Bandcamp, TikTok or 1,750+ other sites.
Naghma downloads the audio, writes the tags, embeds the cover, and files everything
under Artist / Album / Title. Batch a whole playlist, skip what you already own,
export an M3U to your phone.

Free forever for single downloads. Pro is $6 once - batch queue, playlists,
1080p/4K, auto-organising, duplicate detection. No subscription, no account,
works offline, one key for life.

Windows 10/11 - portable - no installer - English / Arabic / French
```

Summary (short line, 160 chars max):

```
Batch downloads, playlists, 4K and auto-organising for Naghma. One key, offline, lifetime.
```

## 4. Cover and metadata

| Field | Value |
|---|---|
| Cover | `assets/cover.png` (1280×720) |
| Thumbnail | `assets/card-intro.png` |
| Tags | `music`, `youtube`, `downloader`, `mp3`, `windows`, `productivity` |
| Category | Software / Productivity |
| Call-to-action | `Get the key` |
| Refund policy | `14 days, no questions` |
| Receipt note | `Your key arrives by email within 24 h. Keep it: it works offline, forever, on every PC you own.` |

## 5. After publishing

1. Buy it yourself with a 100% discount code (`Settings → Discounts → New discount → 100%`).
2. Run the key tool on the order:

   ```powershell
   powershell -ExecutionPolicy Bypass -File private\issue-key.ps1 -Email you@example.com -Order <gumroad-order-id>
   ```

   It appends to `private\sales.csv` and prints the delivery email body.
3. Paste the key in the app: **Settings → Pro → paste key** → expect `Pro activated for ...`.
4. Email the key to the buyer (or set up the webhook later, see `docs/SELLING.md` §2).
5. Re-check the landing page buy button resolves:
   `https://gumroad.com/l/eydroa` → 200, price $6.

## 6. Do not do this

- Do not reuse the slug `naghma` (that is the font).
- Do not enable "pay what you want" - it undercuts the $6 anchor.
- Do not post the launch links before step 5 passes.
