# Brave Browser Desktop Edition 🦁

An exact desktop replica of the **Brave Browser** UI, built from scratch in C# and powered by Edge Chromium 155.

---

## 🦁 Exact Brave Browser UI Features

- **Exact Brave Tab Strip & Colors**:
  - Dark bluish-slate theme palette (`#1E2029` tab strip, `#282B36` toolbar).
  - Brave trapezoid-style tabs with the signature **Brave Orange** (`#FB542B`) accent line on the active tab.
  - New tab button (`+`) placed directly next to the tabs.
  - Windows caption buttons (`–`, `□`, `✕`) on the top-right.

- **Exact Brave Navigation Toolbar & Omnibox**:
  - Navigation buttons: `←`, `→`, `⟳`, `⌂`.
  - Rounded pill-shaped Omnibox (`#1E2029`) with security lock indicator (`🔒`), one-click bookmark star (`★`), and focus border in Brave Orange (`#FB542B`).
  - Search queries route directly to **Brave Search** (`https://search.brave.com`).
  - **Brave Shields Lion Button**: Distinctive orange lion badge (`🦁 0`) whose counter updates live as ads and trackers are blocked.
  - **Brave Rewards icon**: `▲`.
  - **Brave Menu**: `☰` (customization and controls).

- **Exact Brave Bookmarks Bar**:
  - Fixed dark bar (`#20222C`) with default shortcuts: Brave Search, GitHub, YouTube, Reddit, Wikipedia, Hacker News.

- **Exact Brave Start Page (New Tab)**:
  - Statistics row:
    - **1,842** Trackers & ads blocked
    - **24.6 MB** Bandwidth saved
    - **1.8 m** Time saved
  - Digital clock and greeting (*"Good evening"*).
  - Brave Search input box with `🦁` branding.
  - Top site shortcut cards with favicons.

- **🛡️ Brave Shields Modal**:
  - Displays protection status (*"SHIELDS ARE UP"*), blocked trackers count, HTTPS upgrades, fingerprinting protection, and script blocking.

---

## 🚫 Ad & Tracker Blocking

Shields does real network-level filtering, not just cosmetic hiding.

- **Filter lists**: **EasyList** and **EasyPrivacy** are downloaded on first run and cached in
  `%APPDATA%\AuraBrowser\filters` (refreshed every 5 days), combined with the bundled
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
| `Ctrl + R` or `F5` | Reload / refresh page |
| `Ctrl + L` | Focus and select URL address bar |
| `Ctrl + D` | Bookmark or unbookmark current page |
| `Ctrl + H` | Open Browsing History |
| `Ctrl + U` | View page source code |
| `Alt + Left` | Navigate back |
| `Alt + Right` | Navigate forward |
| `Enter` in Omnibox | Search with Brave or navigate to address |

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
Executable output: `bin\AuraBrowser.exe`.
