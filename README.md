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
  - **Brave Shields Lion Button**: Distinctive orange lion badge (`🦁 14`) displaying real-time tracker protection telemetry.
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
