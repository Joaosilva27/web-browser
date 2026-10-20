<p align="center"><img src="assets/toge.png" width="144" alt="Toge"></p>

# Toge 🦊

A fast desktop browser with a Safari-inspired interface and Brave-grade Shields, built from
scratch in C# and powered by Microsoft Edge WebView2 (Chromium).

---

## Features

- **Tab strip** — Safari-like tabs with a favicon, a mute badge, drag-to-reorder and a full
  right-click menu (duplicate, close others, close to the right). The `+` button sits beside
  the tabs; Windows caption buttons stay top-right.
- **Toolbar** — back, forward, reload, a rounded omnibox with a security lock, bookmark star,
  page settings (zoom and reader view), share and overflow menus.
- **Start page** — clock, search box, shortcut tiles and a customizable wallpaper, with the
  Toge mark as its icon.
- **Toge Shields** — an orange shield badge whose counter updates live as ads and trackers are
  blocked, with a panel for the details.
- **Everyday browsing** — find on page with highlighting, reader view, history, bookmarks,
  downloads with pause/resume, private windows, session restore after a crash, a site-info
  panel with per-site cookies, per-site zoom, save page, print, notifications and a tab search
  palette.
- **Keyboard first** — every shortcut below, plus F6 to move between the page and the chrome.

---

## 🚫 Ad & Tracker Blocking

Shields does real network-level filtering, not just cosmetic hiding.

- **Filter lists**: **EasyList** and **EasyPrivacy** are downloaded on first run and cached in
  `%APPDATA%\Toge\filters` (refreshed every 5 days), combined with the bundled
  `scripts\d3host.adblock` list and a built-in fallback set.
  Current total: **~113,000 rules** (≈93,000 host rules + ≈20,500 URL patterns).
- **Syntax support**: `||domain^`, `/regex/`, wildcards, `@@` exception rules, and the
  `$domain=`, `third-party`, `first-party` and resource-type options. Rules using options the
  engine cannot honour (`redirect`, `csp`, `removeparam`, …) are skipped rather than misapplied.
- **Cosmetic filtering**: a stylesheet plus a `MutationObserver` removes ad slots, sponsored
  cards and empty ad iframes, including YouTube's in-feed and player ads.
- **Safety**: the top-level page is never blocked, and popups are only blocked when they target
  known ad networks — so ordinary sites, downloads and legitimate popups keep working.
- **Performance**: pattern rules are matched through a 3-character index; a typical request costs
  roughly `0.04 ms` on the UI thread.
- **Offline**: if the lists have never been downloaded, the bundled and built-in rules still apply.

---

## ⌨️ Shortcuts

| Shortcut | Action |
| :--- | :--- |
| `Ctrl + T` | Open a new tab |
| `Ctrl + W` | Close the active tab |
| `Ctrl + Shift + T` | Reopen the last closed tab, then window |
| `Ctrl + N` / `Ctrl + Shift + N` | New window / new private window |
| `Ctrl + R` or `F5` | Reload / refresh page |
| `Ctrl + L` or `Alt + D` | Focus and select the address bar |
| `F6` / `Shift + F6` | Focus the chrome / the address bar |
| `Ctrl + D` | Bookmark or unbookmark the current page |
| `Ctrl + H` | Open browsing history |
| `Ctrl + J` | Open downloads |
| `Ctrl + F` | Find on page |
| `Ctrl + S` | Save the page |
| `Ctrl + P` | Print the page |
| `Ctrl + Shift + A` | Search open tabs |
| `Ctrl + +` / `Ctrl + -` / `Ctrl + 0` | Zoom in / out / reset for this site |
| `Ctrl + U` | View page source |
| `Ctrl + ,` | Open settings |
| `Alt + Left` / `Alt + Right` | Back / forward |
| `Enter` in the omnibox | Search with the chosen engine or open the address |

---

## 🚀 How to Launch

Double-click `run.bat` or run:
```cmd
run.bat
```
To compile manually:
```cmd
build.bat
```
Executable output: `bin\Toge.exe`.

The icon in `assets/toge.ico` is compiled into the executable and every window; `assets/toge.png`
is the source artwork used for the start page and for regenerating the icon.
