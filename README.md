# Brave Browser UI Edition 🦁

A lightweight, fast desktop web browser built from scratch in C# with a sleek UI inspired by **Brave Browser**.

Zero external dependencies: runs and compiles immediately on Windows with no need to install SDKs, Node, or Python.

---

## 🎨 Brave-Inspired UI & Features

- **🦁 Brave Dark Theme Palette**: Deep dark purple/slate (`#1B1C26` and `#282A3A`) with Brave Lion Orange (`#FB542B`) accent highlights.
- **📑 Custom-Drawn Tabs**: Tabs sit at the top with orange active tab indicator, tab close buttons (`✕`), and a new tab button (`+`).
- **🛡️ Brave Shields Popup**: Distinctive lion button (`🦁`) next to the Omnibox. Clicking it opens a Brave Shields popup displaying protection status, blocked trackers count, HTTPS upgrade, and safe script execution.
- **🔍 Omnibox (Brave Search default)**: Dark rounded address bar with security lock status (`🔒`), one-click bookmark star (`★`), and automatic fallback to **Brave Search** (`search.brave.com`).
- **🕒 Brave Start Page (New Tab)**:
  - Big live digital clock and greeting ("Good evening").
  - Live tracker & ad blocking statistics widgets.
  - Centered Brave Search input box.
  - Shortcut tiles with hover animations.
- **⭐ Bookmarks Toolbar**: Persistent dark bookmarks bar with right-click delete support.
- **📜 Browsing History**: Searchable and filterable history viewer (`Ctrl+H`).
- **💻 Page Source Inspector**: Built-in HTML source viewer (`Ctrl+U`) with copy button.

---

## ⌨️ Keyboard Shortcuts

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
| `Enter` in Omnibox | Search Brave or navigate to address |

---

## 🚀 How to Run

### 1-Click Launch
Double-click `run.bat` or run:
```cmd
run.bat
```
This automatically compiles the browser (if not already built) and launches it.

### Build Manually
Run:
```cmd
build.bat
```
The compiled executable will be output to `bin\AuraBrowser.exe`.

---

## 📁 Repository Structure

```
simple-web-browser/
├── src/
│   ├── Program.cs             # Entry point & registry modern browser emulation
│   ├── MainForm.cs            # Brave UI main window, custom tabs, Omnibox, navigation
│   ├── BraveShieldsForm.cs    # Brave Shields tracker protection popup
│   ├── BrowserTab.cs          # Web rendering tab, lifecycle & Brave Search fallback
│   ├── BookmarkManager.cs     # Bookmarks manager & persistence
│   ├── HistoryManager.cs      # Browsing history persistence
│   ├── HistoryForm.cs         # Brave-themed history dialog with search & filter
│   ├── SourceViewerForm.cs    # Brave-themed HTML source code inspector
│   └── HomePage.cs            # Brave-style New Tab Start Page
├── bin/                       # Executable output directory
├── build.bat                  # Instant compilation script
├── run.bat                    # 1-click launch script
├── .gitignore
└── README.md
```
