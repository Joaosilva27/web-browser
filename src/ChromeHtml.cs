using System;

namespace SimpleBrowser
{
    public static class ChromeHtml
    {
        public static string GetHtml()
        {
            return @"<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='utf-8' />
    <meta name='viewport' content='width=device-width, initial-scale=1.0' />
    <title>Aether Chrome</title>
    <style>
        * { box-sizing: border-box; margin: 0; padding: 0; user-select: none; }

        :root {
            --bg-glass: rgba(10, 12, 18, 0.85);
            --border-glass: rgba(255, 255, 255, 0.09);
            --accent-cyan: #00f2fe;
            --accent-purple: #9d4edd;
            --text-primary: #f5f5f7;
            --text-muted: #86868b;
            --pill-bg: rgba(255, 255, 255, 0.05);
            --pill-hover: rgba(255, 255, 255, 0.1);
            --pill-active: rgba(255, 255, 255, 0.12);
        }

        body {
            font-family: -apple-system, BlinkMacSystemFont, 'SF Pro Display', 'Segoe UI Variable', Roboto, sans-serif;
            background: linear-gradient(180deg, #090a0f 0%, #0d0f16 100%);
            color: var(--text-primary);
            height: 100vh;
            display: flex;
            flex-direction: column;
            overflow: hidden;
            border-bottom: 1px solid rgba(255, 255, 255, 0.07);
        }

        /* Drag region */
        .drag-region {
            -webkit-app-region: drag;
        }
        .no-drag {
            -webkit-app-region: no-drag;
        }

        /* ROW 1: Window Controls + Floating Tabs */
        .top-row {
            height: 42px;
            display: flex;
            align-items: center;
            padding: 0 12px;
            gap: 12px;
            position: relative;
        }

        /* Apple Traffic Lights */
        .traffic-lights {
            display: flex;
            gap: 8px;
            align-items: center;
            padding-right: 6px;
        }
        .traffic-btn {
            width: 12px;
            height: 12px;
            border-radius: 50%;
            cursor: pointer;
            border: none;
            outline: none;
            position: relative;
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 8px;
            font-weight: bold;
            color: rgba(0, 0, 0, 0.6);
            transition: transform 0.15s, opacity 0.15s;
        }
        .traffic-btn span { opacity: 0; transition: opacity 0.15s; }
        .traffic-lights:hover .traffic-btn span { opacity: 1; }
        .traffic-close { background: #ff5f56; box-shadow: 0 0 6px rgba(255, 95, 86, 0.4); }
        .traffic-min { background: #ffbd2e; box-shadow: 0 0 6px rgba(255, 189, 46, 0.4); }
        .traffic-max { background: #27c93f; box-shadow: 0 0 6px rgba(39, 201, 63, 0.4); }
        .traffic-btn:active { transform: scale(0.9); }

        /* Floating Pill Tabs Strip */
        .tabs-container {
            display: flex;
            align-items: center;
            gap: 6px;
            flex: 1;
            overflow-x: auto;
            scrollbar-width: none;
            padding: 2px 0;
        }
        .tabs-container::-webkit-scrollbar { display: none; }

        .tab-pill {
            display: flex;
            align-items: center;
            gap: 8px;
            padding: 6px 14px;
            border-radius: 18px;
            background: var(--pill-bg);
            border: 1px solid var(--border-glass);
            font-size: 12.5px;
            color: var(--text-muted);
            cursor: pointer;
            max-width: 200px;
            min-width: 110px;
            transition: all 0.2s cubic-bezier(0.16, 1, 0.3, 1);
            position: relative;
        }
        .tab-pill:hover {
            background: var(--pill-hover);
            color: var(--text-primary);
        }
        .tab-pill.active {
            background: var(--pill-active);
            border-color: rgba(0, 242, 254, 0.45);
            color: #ffffff;
            box-shadow: 0 0 16px rgba(0, 242, 254, 0.18), inset 0 1px 0 rgba(255, 255, 255, 0.15);
        }
        .tab-icon {
            font-size: 11px;
            color: var(--accent-cyan);
        }
        .tab-title {
            white-space: nowrap;
            overflow: hidden;
            text-overflow: ellipsis;
            flex: 1;
            font-weight: 500;
        }
        .tab-close {
            width: 16px;
            height: 16px;
            border-radius: 50%;
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 9px;
            color: var(--text-muted);
            opacity: 0.6;
            transition: all 0.15s;
        }
        .tab-close:hover {
            background: rgba(255, 255, 255, 0.15);
            color: #fff;
            opacity: 1;
        }

        .new-tab-btn {
            width: 28px;
            height: 28px;
            border-radius: 50%;
            background: var(--pill-bg);
            border: 1px solid var(--border-glass);
            color: var(--text-muted);
            display: flex;
            align-items: center;
            justify-content: center;
            cursor: pointer;
            font-size: 14px;
            transition: all 0.2s;
        }
        .new-tab-btn:hover {
            background: var(--pill-hover);
            border-color: var(--accent-cyan);
            color: var(--accent-cyan);
            transform: scale(1.08);
        }

        .brand-badge {
            font-size: 11px;
            letter-spacing: 1.5px;
            font-weight: 600;
            color: rgba(255, 255, 255, 0.35);
            padding: 0 8px;
        }

        /* ROW 2: Navigation & Omnibar */
        .bottom-row {
            height: 48px;
            display: flex;
            align-items: center;
            padding: 0 12px;
            gap: 10px;
        }

        .nav-group {
            display: flex;
            align-items: center;
            gap: 4px;
        }
        .icon-btn {
            width: 32px;
            height: 32px;
            border-radius: 50%;
            background: transparent;
            border: none;
            color: var(--text-muted);
            display: flex;
            align-items: center;
            justify-content: center;
            cursor: pointer;
            font-size: 14px;
            transition: all 0.2s;
        }
        .icon-btn:hover {
            background: var(--pill-hover);
            color: #ffffff;
        }
        .icon-btn:disabled {
            opacity: 0.3;
            cursor: default;
        }

        /* Center Floating Omnibar */
        .omnibar-wrapper {
            flex: 1;
            max-width: 720px;
            margin: 0 auto;
            position: relative;
        }
        .omnibar {
            display: flex;
            align-items: center;
            background: rgba(14, 16, 24, 0.85);
            border: 1px solid var(--border-glass);
            border-radius: 22px;
            padding: 4px 14px;
            gap: 8px;
            box-shadow: 0 4px 16px rgba(0, 0, 0, 0.3);
            backdrop-filter: blur(20px);
            transition: all 0.25s cubic-bezier(0.16, 1, 0.3, 1);
        }
        .omnibar:hover {
            border-color: rgba(0, 242, 254, 0.35);
        }
        .omnibar:focus-within {
            border-color: var(--accent-cyan);
            background: rgba(18, 21, 32, 0.95);
            box-shadow: 0 8px 24px rgba(0, 0, 0, 0.5), 0 0 20px rgba(0, 242, 254, 0.2);
            transform: scale(1.008);
        }
        .omnibar-star {
            color: var(--accent-cyan);
            font-size: 13px;
        }
        .url-input {
            flex: 1;
            background: transparent;
            border: none;
            outline: none;
            color: var(--text-primary);
            font-size: 13.5px;
            font-family: inherit;
            font-weight: 400;
        }
        .url-input::placeholder {
            color: #555866;
        }
        .cmd-hint {
            display: flex;
            align-items: center;
            gap: 4px;
            background: rgba(255, 255, 255, 0.08);
            border: 1px solid rgba(255, 255, 255, 0.1);
            padding: 2px 7px;
            border-radius: 6px;
            font-size: 10px;
            color: var(--text-muted);
            cursor: pointer;
            transition: all 0.15s;
        }
        .cmd-hint:hover {
            background: var(--accent-cyan);
            color: #000;
            border-color: var(--accent-cyan);
        }

        .star-btn {
            font-size: 13px;
            color: var(--text-muted);
            cursor: pointer;
            padding: 0 4px;
            transition: color 0.15s;
        }
        .star-btn.active {
            color: #ffd166;
            text-shadow: 0 0 8px rgba(255, 209, 102, 0.5);
        }

        /* Right Actions */
        .right-group {
            display: flex;
            align-items: center;
            gap: 6px;
        }
        .shield-pill {
            display: flex;
            align-items: center;
            gap: 6px;
            padding: 5px 12px;
            border-radius: 16px;
            background: rgba(0, 242, 254, 0.08);
            border: 1px solid rgba(0, 242, 254, 0.25);
            font-size: 11.5px;
            color: var(--accent-cyan);
            font-weight: 600;
            cursor: pointer;
            transition: all 0.2s;
        }
        .shield-pill:hover {
            background: rgba(0, 242, 254, 0.16);
            box-shadow: 0 0 12px rgba(0, 242, 254, 0.25);
        }

        /* COMMAND PALETTE MODAL (Ctrl+K) */
        .palette-backdrop {
            display: none;
            position: fixed;
            top: 0; left: 0; right: 0; bottom: 0;
            background: rgba(0, 0, 0, 0.65);
            backdrop-filter: blur(16px);
            z-index: 100;
            align-items: flex-start;
            justify-content: center;
            padding-top: 12px;
        }
        .palette-backdrop.open { display: flex; }

        .palette-box {
            width: 560px;
            background: #11131c;
            border: 1px solid rgba(0, 242, 254, 0.35);
            border-radius: 16px;
            box-shadow: 0 24px 60px rgba(0, 0, 0, 0.8), 0 0 30px rgba(0, 242, 254, 0.2);
            overflow: hidden;
            animation: slideDown 0.2s cubic-bezier(0.16, 1, 0.3, 1);
        }
        @keyframes slideDown {
            from { transform: translateY(-16px); opacity: 0; }
            to { transform: translateY(0); opacity: 1; }
        }
        .palette-search {
            width: 100%;
            padding: 14px 18px;
            background: transparent;
            border: none;
            border-bottom: 1px solid rgba(255, 255, 255, 0.08);
            color: #fff;
            font-size: 15px;
            outline: none;
            font-family: inherit;
        }
        .palette-items {
            max-height: 280px;
            overflow-y: auto;
            padding: 8px;
        }
        .palette-item {
            display: flex;
            align-items: center;
            gap: 12px;
            padding: 10px 14px;
            border-radius: 10px;
            cursor: pointer;
            font-size: 13.5px;
            color: var(--text-primary);
            transition: all 0.15s;
        }
        .palette-item:hover, .palette-item.selected {
            background: rgba(0, 242, 254, 0.15);
            color: #ffffff;
        }
        .palette-item-icon {
            color: var(--accent-cyan);
            font-size: 14px;
        }
        .palette-item-desc {
            margin-left: auto;
            font-size: 11px;
            color: var(--text-muted);
        }
    </style>
</head>
<body class='drag-region'>

    <!-- ROW 1: Traffic Lights + Tabs -->
    <div class='top-row'>
        <div class='traffic-lights no-drag'>
            <button class='traffic-btn traffic-close' title='Close Window' onclick='sendMsg(""window_close"")'><span>✕</span></button>
            <button class='traffic-btn traffic-min' title='Minimize' onclick='sendMsg(""window_min"")'><span>–</span></button>
            <button class='traffic-btn traffic-max' title='Maximize' onclick='sendMsg(""window_max"")'><span>+</span></button>
        </div>

        <div class='tabs-container no-drag' id='tabs-container'>
            <!-- Dynamic tabs inserted here -->
        </div>

        <div class='new-tab-btn no-drag' title='New Pill Tab (⌘T)' onclick='sendMsg(""new_tab"")'>+</div>

        <div class='brand-badge'>AETHER</div>
    </div>

    <!-- ROW 2: Navigation & Omnibar -->
    <div class='bottom-row no-drag'>
        <div class='nav-group'>
            <button class='icon-btn' id='btn-back' title='Back (Alt+Left)' onclick='sendMsg(""back"")'>‹</button>
            <button class='icon-btn' id='btn-forward' title='Forward (Alt+Right)' onclick='sendMsg(""forward"")'>›</button>
            <button class='icon-btn' id='btn-reload' title='Reload (Ctrl+R)' onclick='sendMsg(""reload"")'>⟳</button>
            <button class='icon-btn' id='btn-home' title='Start Page' onclick='sendMsg(""home"")'>⌂</button>
        </div>

        <div class='omnibar-wrapper'>
            <div class='omnibar'>
                <span class='omnibar-star'>✦</span>
                <input class='url-input' id='url-input' type='text' placeholder='Search with Google or enter address...' spellcheck='false' />
                <span class='star-btn' id='btn-star' title='Bookmark (⌘D)' onclick='sendMsg(""bookmark"")'>★</span>
                <div class='cmd-hint' onclick='openPalette()'>⌘K</div>
            </div>
        </div>

        <div class='right-group'>
            <div class='shield-pill' title='Aether Quantum Shield Telemetry' onclick='sendMsg(""shield"")'>
                <span>✦</span>
                <span>Shield ON</span>
            </div>
            <button class='icon-btn' title='History (⌘H)' onclick='sendMsg(""history"")'>🕒</button>
            <button class='icon-btn' title='Source Inspector (⌘U)' onclick='sendMsg(""source"")'>⌥</button>
            <button class='icon-btn' title='Aether Menu' onclick='sendMsg(""menu"")'>•••</button>
        </div>
    </div>

    <!-- COMMAND PALETTE MODAL -->
    <div class='palette-backdrop no-drag' id='palette' onclick='closePalette(event)'>
        <div class='palette-box' onclick='event.stopPropagation()'>
            <input class='palette-search' id='palette-search' placeholder='Type a command or search query...' />
            <div class='palette-items' id='palette-items'>
                <div class='palette-item selected' onclick='runPaletteAction(""new_tab"")'>
                    <span class='palette-item-icon'>➕</span>
                    <span>Create New Pill Tab</span>
                    <span class='palette-item-desc'>⌘T</span>
                </div>
                <div class='palette-item' onclick='runPaletteAction(""home"")'>
                    <span class='palette-item-icon'>🌌</span>
                    <span>Open VisionOS Cosmic Start Page</span>
                    <span class='palette-item-desc'>Home</span>
                </div>
                <div class='palette-item' onclick='runPaletteAction(""history"")'>
                    <span class='palette-item-icon'>🕒</span>
                    <span>View Browsing History &amp; Cache</span>
                    <span class='palette-item-desc'>⌘H</span>
                </div>
                <div class='palette-item' onclick='runPaletteAction(""bookmark"")'>
                    <span class='palette-item-icon'>⭐</span>
                    <span>Bookmark Current Session</span>
                    <span class='palette-item-desc'>⌘D</span>
                </div>
                <div class='palette-item' onclick='runPaletteAction(""shield"")'>
                    <span class='palette-item-icon'>✦</span>
                    <span>Open Aether Quantum Shield Telemetry</span>
                    <span class='palette-item-desc'>Security</span>
                </div>
                <div class='palette-item' onclick='runPaletteAction(""source"")'>
                    <span class='palette-item-icon'>⌥</span>
                    <span>Inspect Raw Page Source Code</span>
                    <span class='palette-item-desc'>⌘U</span>
                </div>
            </div>
        </div>
    </div>

    <script>
        function sendMsg(action, data) {
            if (window.chrome && window.chrome.webview) {
                window.chrome.webview.postMessage(Object.assign({ action: action }, data || {}));
            }
        }

        var urlInput = document.getElementById('url-input');
        urlInput.addEventListener('keydown', function(e) {
            if (e.key === 'Enter') {
                sendMsg('navigate', { url: urlInput.value.trim() });
            }
        });
        urlInput.addEventListener('click', function() {
            urlInput.select();
        });

        // Command Palette
        var palette = document.getElementById('palette');
        var paletteSearch = document.getElementById('palette-search');
        function openPalette() {
            palette.classList.add('open');
            paletteSearch.value = '';
            paletteSearch.focus();
        }
        function closePalette(e) {
            palette.classList.remove('open');
        }
        function runPaletteAction(action) {
            closePalette();
            sendMsg(action);
        }

        paletteSearch.addEventListener('keydown', function(e) {
            if (e.key === 'Escape') {
                closePalette();
            } else if (e.key === 'Enter') {
                var query = paletteSearch.value.trim();
                if (query) {
                    closePalette();
                    sendMsg('navigate', { url: query });
                }
            }
        });

        window.addEventListener('keydown', function(e) {
            if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
                e.preventDefault();
                openPalette();
            }
        });

        // Global Aether API callable from C#
        window.AetherAPI = {
            renderTabs: function(tabs, activeIndex) {
                var container = document.getElementById('tabs-container');
                container.innerHTML = '';
                tabs.forEach(function(tab, index) {
                    var pill = document.createElement('div');
                    pill.className = 'tab-pill' + (index === activeIndex ? ' active' : '');
                    pill.onclick = function() { sendMsg('switch_tab', { index: index }); };

                    var icon = document.createElement('span');
                    icon.className = 'tab-icon';
                    icon.innerText = (index === activeIndex ? '✦' : '○');

                    var title = document.createElement('span');
                    title.className = 'tab-title';
                    title.innerText = tab.title || 'New Tab';

                    var close = document.createElement('span');
                    close.className = 'tab-close';
                    close.innerText = '✕';
                    close.onclick = function(e) {
                        e.stopPropagation();
                        sendMsg('close_tab', { index: index });
                    };

                    pill.appendChild(icon);
                    pill.appendChild(title);
                    pill.appendChild(close);
                    container.appendChild(pill);
                });
            },
            setUrl: function(url) {
                urlInput.value = (url === 'about:home' ? '' : url);
            },
            setNavState: function(canBack, canForward) {
                document.getElementById('btn-back').disabled = !canBack;
                document.getElementById('btn-forward').disabled = !canForward;
            },
            setBookmarked: function(isBookmarked) {
                var btn = document.getElementById('btn-star');
                if (isBookmarked) {
                    btn.classList.add('active');
                } else {
                    btn.classList.remove('active');
                }
            }
        };
    </script>
</body>
</html>";
        }
    }
}
