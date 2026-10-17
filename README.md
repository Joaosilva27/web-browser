# Aura Web Browser 🌐

A lightweight, fast, and simple desktop web browser built from scratch in C# using Windows Forms and .NET.

Designed to be zero-dependency: no external SDKs, Node, Python, or package managers required to build or run. It compiles in under 1 second using Windows' built-in C# compiler (`csc.exe`).

---

## ✨ Features

- **📑 Multi-Tab Browsing**: Open multiple web pages concurrently (`Ctrl+T` to open a tab, `Ctrl+W` to close).
- **🔍 Smart Address & Search Bar**: Type a full URL or any search term (automatically queries Google if no domain is specified).
- **⭐ Bookmarks Manager**:
  - One-click bookmark toggle button (`☆` / `★` or `Ctrl+D`).
  - Bookmarks bar for quick navigation.
  - Right-click any bookmark to delete it.
  - Automatically saved across sessions in AppData.
- **🕒 Browsing History**:
  - Automatically logs visited sites, timestamps, and page titles.
  - Dedicated history viewer (`Ctrl+H`) with real-time text filter and double-click to navigate.
  - Clear history option.
- **💻 Page Source Inspector**: View raw HTML of any loaded page in a clean dark-themed viewer (`Ctrl+U`) with a one-click "Copy Source" button.
- **⚡ Modern Start Page**: Sleek dark-mode "New Tab" landing page with quick launch tiles (Google, GitHub, Wikipedia, Hacker News, YouTube, Reddit, MDN) and an integrated search box.
- **🛡️ Modern Standards Mode**: Automatically enables IE11 standards emulation in Windows registry to ensure compatibility with modern websites.

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
| `Enter` in URL bar | Navigate to address or search query |

---

## 🚀 How to Run

### Option 1: One-Click Run
Double-click `run.bat` or run:
```cmd
run.bat
```
This automatically compiles the browser (if not already built) and launches `AuraBrowser.exe`.

### Option 2: Build Manually
Run the build script:
```cmd
build.bat
```
The compiled executable will be placed in `bin\AuraBrowser.exe`.

---

## 📁 Project Structure

```
simple-web-browser/
├── src/
│   ├── Program.cs           # Entry point and registry emulation setup
│   ├── MainForm.cs          # Main browser window, toolbars, and layout
│   ├── BrowserTab.cs        # Individual tab hosting the web engine
│   ├── BookmarkManager.cs   # Bookmarks persistence and management
│   ├── HistoryManager.cs    # Browsing history persistence
│   ├── HistoryForm.cs       # Browsing history dialog and filter UI
│   ├── SourceViewerForm.cs  # HTML source code viewer dialog
│   └── HomePage.cs          # Embedded HTML/CSS Start Page
├── bin/                     # Output directory for AuraBrowser.exe
├── build.bat                # 1-click compilation script
├── run.bat                  # 1-click build and launch script
├── .gitignore
└── README.md
```

---

## 🛠️ System Requirements

- **Operating System**: Windows 7, 8, 10, or 11
- **Runtime**: .NET Framework 4.5+ (pre-installed on all modern Windows installations)
- No additional downloads or installations necessary!
