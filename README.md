# AETHER ✦

> **The Quantum Minimalist Web Browser**  
> Designed with Cupertino Simplicity, Futuristic Precision, and Zero Footprint.

Aether is a fast, standalone desktop web browser built from scratch in C# with an Apple-inspired futuristic interface (VisionOS-style frosted glass, floating pill tabs, ambient cosmic start page, and Cupertino typography). Powered under the hood by modern Microsoft Edge Chromium 155.

---

## ✦ Design Philosophy & Features

- **🍎 Apple-Inspired Human Interface**:
  - **VisionOS Floating Pill Tabs**: Rounded capsule tabs with active frosted glass illumination, smooth circular close buttons (`✕`), and dynamic title truncation.
  - **Floating Omnibar**: A centered frosted glass pill with specular border highlights, security indicator (`✦`), and ambient cyan glow on focus.
  - **Minimalist Iconography**: Clean Apple-style chevrons (`‹`, `›`), reload (`⟳`), start page (`⌂`), star bookmark (`★`), and triple-dot menu (`•••`).
  - **Deep Obsidian Palette**: Ultra-refined dark tones (`#07080B`, `#0A0B10`, `#11131C`) with ethereal cyan (`#00F2FE`) and ultraviolet (`#9D4EDD`) ambient lighting.

- **🌌 Ambient Cosmic Start Page**:
  - Thin minimalist digital clock with gradient typography.
  - Organic ambient glowing nebulae drifting softly in the background.
  - Floating Omnibar with intelligent URL / search query detection.
  - VisionOS-style frosted glass shortcut cards with specular borders and subtle hover lift (Apple, GitHub, YouTube, Reddit, Wikipedia, Hacker News).
  - Telemetry badge showing security status and engine stats.

- **🛡️ Aether Quantum Shield**:
  - Frosted glass privacy card providing real-time telemetry on tracker neutralization, fingerprint cloaking, HTTPS enforcement, and zero-telemetry isolation.

- **⚡ Modern Chromium 155 Engine**:
  - Full modern web standard support: ES6+, WebSockets, modern CSS grid/flexbox, HTML5 audio/video, and WASM.
  - No blank pages, no rendering bugs.

- **⭐ Frosted Bookmarks & History**:
  - Translucent bookmarks pill bar with right-click removal.
  - Minimalist browsing history dialog (`⌘H`) with live instant filter.
  - Full HTML source inspector (`⌥ Source` / `⌘U`) with 1-click clipboard copy.

---

## ⌨️ Shortcuts

| Shortcut | Action |
| :--- | :--- |
| `Ctrl + T` (⌘T) | Open a new pill tab |
| `Ctrl + W` (⌘W) | Close the active tab |
| `Ctrl + R` (⌘R) or `F5` | Reload / refresh page |
| `Ctrl + L` (⌘L) | Focus and highlight Omnibar |
| `Ctrl + D` (⌘D) | Save / remove bookmark |
| `Ctrl + H` (⌘H) | Open Browsing History |
| `Ctrl + U` (⌘U) | Inspect page HTML source |
| `Alt + Left` | Navigate back |
| `Alt + Right` | Navigate forward |
| `Enter` in Omnibar | Navigate to address or query |

---

## 🚀 Quick Launch

### 1-Click Launch
Double-click `run.bat` or run:
```cmd
run.bat
```
This automatically compiles the browser (if not already built) and launches `AuraBrowser.exe`.

### Manual Build
```cmd
build.bat
```
Compiled standalone binary is placed in `bin\AuraBrowser.exe`.

---

## 📁 Architecture

```
simple-web-browser/
├── src/
│   ├── Program.cs             # Entry point & crash recovery
│   ├── MainForm.cs            # Aether Apple-style UI, floating pill tabs & Omnibar
│   ├── AetherShieldForm.cs    # Aether Quantum Shield frosted glass modal
│   ├── BrowserTab.cs          # Chromium 155 WebView2 rendering lifecycle
│   ├── BookmarkManager.cs     # Bookmarks persistence and management
│   ├── HistoryManager.cs      # Browsing history persistence
│   ├── HistoryForm.cs         # Frosted glass browsing history UI
│   ├── SourceViewerForm.cs    # Obsidian HTML source code inspector
│   └── HomePage.cs            # Ambient Cosmic Start Page (VisionOS style)
├── bin/                       # Output directory for AuraBrowser.exe
├── lib/                       # WebView2 Chromium libraries
├── scripts/
│   └── get_webview2.ps1       # Automated engine dependency installer
├── build.bat                  # Instant 1-click build script
├── run.bat                    # 1-click launch script
├── .gitignore
└── README.md
```
