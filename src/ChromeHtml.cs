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
    <title>Brave Chrome</title>
    <style>
        * { box-sizing: border-box; margin: 0; padding: 0; user-select: none; }

        :root {
            --bg-tabstrip: #1e2029;
            --bg-toolbar: #282b36;
            --bg-omnibox: #1e2029;
            --border-omnibox: #393c4a;
            --brave-orange: #fb542b;
            --brave-orange-hover: #ff6a45;
            --text-primary: #ffffff;
            --text-muted: #9fa4b2;
            --tab-inactive: #1e2029;
            --tab-hover: #242732;
        }

        body {
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif;
            background: var(--bg-tabstrip);
            color: var(--text-primary);
            height: 100vh;
            display: flex;
            flex-direction: column;
            overflow: hidden;
        }

        /* Drag region */
        .drag-region {
            -webkit-app-region: drag;
        }
        .no-drag {
            -webkit-app-region: no-drag;
        }

        /* ROW 1: Tabs + Windows Caption Controls */
        .top-row {
            height: 38px;
            display: flex;
            align-items: flex-end;
            background: var(--bg-tabstrip);
            padding-left: 8px;
            position: relative;
        }

        /* Brave Tabs */
        .tabs-container {
            display: flex;
            align-items: flex-end;
            gap: 2px;
            flex: 1;
            height: 100%;
            overflow-x: auto;
            scrollbar-width: none;
        }
        .tabs-container::-webkit-scrollbar { display: none; }

        .tab {
            display: flex;
            align-items: center;
            gap: 8px;
            padding: 0 12px;
            height: 34px;
            background: var(--tab-inactive);
            border-top-left-radius: 8px;
            border-top-right-radius: 8px;
            font-size: 12px;
            color: var(--text-muted);
            cursor: pointer;
            max-width: 220px;
            min-width: 140px;
            position: relative;
            border-top: 2px solid transparent;
            transition: background 0.15s, color 0.15s;
        }
        .tab:hover {
            background: var(--tab-hover);
            color: var(--text-primary);
        }
        .tab.active {
            background: var(--bg-toolbar);
            color: #ffffff;
            border-top: 2px solid var(--brave-orange);
            font-weight: 500;
        }
        .tab-icon {
            font-size: 12px;
            color: var(--brave-orange);
        }
        .tab-title {
            white-space: nowrap;
            overflow: hidden;
            text-overflow: ellipsis;
            flex: 1;
        }
        .tab-close {
            width: 16px;
            height: 16px;
            border-radius: 50%;
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 10px;
            color: var(--text-muted);
            transition: background 0.15s, color 0.15s;
        }
        .tab-close:hover {
            background: rgba(255, 255, 255, 0.2);
            color: #ffffff;
        }

        .new-tab-btn {
            width: 28px;
            height: 28px;
            border-radius: 50%;
            color: var(--text-muted);
            display: flex;
            align-items: center;
            justify-content: center;
            cursor: pointer;
            font-size: 16px;
            margin-bottom: 3px;
            margin-left: 4px;
            transition: background 0.15s, color 0.15s;
        }
        .new-tab-btn:hover {
            background: rgba(255, 255, 255, 0.1);
            color: #ffffff;
        }

        /* Windows 10/11 Caption Controls */
        .window-controls {
            display: flex;
            height: 100%;
            margin-left: auto;
        }
        .caption-btn {
            width: 46px;
            height: 38px;
            display: flex;
            align-items: center;
            justify-content: center;
            background: transparent;
            border: none;
            color: var(--text-muted);
            font-size: 11px;
            cursor: pointer;
            transition: background 0.15s, color 0.15s;
        }
        .caption-btn:hover {
            background: rgba(255, 255, 255, 0.1);
            color: #ffffff;
        }
        .caption-btn.btn-close:hover {
            background: #e81123;
            color: #ffffff;
        }

        /* ROW 2: Navigation Toolbar & Omnibox */
        .toolbar-row {
            height: 44px;
            background: var(--bg-toolbar);
            display: flex;
            align-items: center;
            padding: 0 10px;
            gap: 8px;
            box-shadow: 0 2px 4px rgba(0,0,0,0.2);
        }

        .nav-buttons {
            display: flex;
            align-items: center;
            gap: 2px;
        }
        .nav-btn {
            width: 30px;
            height: 30px;
            border-radius: 50%;
            background: transparent;
            border: none;
            color: #d1d5db;
            display: flex;
            align-items: center;
            justify-content: center;
            cursor: pointer;
            font-size: 14px;
            transition: background 0.15s;
        }
        .nav-btn:hover {
            background: rgba(255, 255, 255, 0.12);
        }
        .nav-btn:disabled {
            opacity: 0.3;
            cursor: default;
        }

        /* Exact Brave Omnibox */
        .omnibox-container {
            flex: 1;
            position: relative;
        }
        .omnibox {
            display: flex;
            align-items: center;
            background: var(--bg-omnibox);
            border: 1px solid var(--border-omnibox);
            border-radius: 18px;
            height: 32px;
            padding: 0 12px;
            gap: 8px;
            transition: border-color 0.2s, box-shadow 0.2s;
        }
        .omnibox:focus-within {
            border-color: var(--brave-orange);
            box-shadow: 0 0 0 1px var(--brave-orange);
        }
        .lock-icon {
            font-size: 11px;
            color: #9fa4b2;
        }
        .url-input {
            flex: 1;
            background: transparent;
            border: none;
            outline: none;
            color: #ffffff;
            font-size: 13px;
            font-family: inherit;
        }
        .url-input::placeholder {
            color: #6b7280;
        }
        .bookmark-btn {
            font-size: 13px;
            color: var(--text-muted);
            cursor: pointer;
            transition: color 0.15s;
        }
        .bookmark-btn.active {
            color: var(--brave-orange);
        }

        /* Exact Brave Action Buttons (Shields Lion, Rewards, History, Menu) */
        .actions-group {
            display: flex;
            align-items: center;
            gap: 4px;
        }
        .shields-btn {
            display: flex;
            align-items: center;
            gap: 4px;
            background: rgba(251, 84, 43, 0.15);
            border: 1px solid rgba(251, 84, 43, 0.4);
            border-radius: 14px;
            padding: 3px 8px;
            cursor: pointer;
            transition: background 0.15s;
        }
        .shields-btn:hover {
            background: rgba(251, 84, 43, 0.28);
        }
        .shields-lion {
            font-size: 14px;
        }
        .shields-count {
            font-size: 11px;
            font-weight: 700;
            color: var(--brave-orange);
        }

        .rewards-btn {
            width: 30px;
            height: 30px;
            border-radius: 50%;
            background: transparent;
            border: none;
            color: #ff542b;
            font-size: 13px;
            display: flex;
            align-items: center;
            justify-content: center;
            cursor: pointer;
            transition: background 0.15s;
        }
        .rewards-btn:hover {
            background: rgba(255, 255, 255, 0.12);
        }

        /* ROW 3: Bookmarks Bar */
        .bookmarks-row {
            height: 28px;
            background: #20222c;
            display: flex;
            align-items: center;
            padding: 0 12px;
            gap: 12px;
            border-bottom: 1px solid rgba(0, 0, 0, 0.3);
        }
        .bookmark-item {
            display: flex;
            align-items: center;
            gap: 5px;
            font-size: 11.5px;
            color: #d1d5db;
            text-decoration: none;
            cursor: pointer;
            padding: 2px 6px;
            border-radius: 4px;
            transition: background 0.15s, color 0.15s;
        }
        .bookmark-item:hover {
            background: rgba(255, 255, 255, 0.08);
            color: #ffffff;
        }
        .bm-icon {
            font-size: 11px;
        }
    </style>
</head>
<body class='drag-region'>

    <!-- ROW 1: Tabs + Windows Caption Controls -->
    <div class='top-row'>
        <div class='tabs-container no-drag' id='tabs-container'>
            <!-- Injected dynamically -->
        </div>

        <div class='new-tab-btn no-drag' title='New Tab (Ctrl+T)' onclick='sendMsg(""new_tab"")'>+</div>

        <div class='window-controls no-drag'>
            <button class='caption-btn' title='Minimize' onclick='sendMsg(""window_min"")'>&#x2212;</button>
            <button class='caption-btn' title='Maximize' onclick='sendMsg(""window_max"")'>&#x25A2;</button>
            <button class='caption-btn btn-close' title='Close' onclick='sendMsg(""window_close"")'>&#x2715;</button>
        </div>
    </div>

    <!-- ROW 2: Navigation Toolbar & Omnibox -->
    <div class='toolbar-row no-drag'>
        <div class='nav-buttons'>
            <button class='nav-btn' id='btn-back' title='Click to go back (Alt+Left)' onclick='sendMsg(""back"")'>&larr;</button>
            <button class='nav-btn' id='btn-forward' title='Click to go forward (Alt+Right)' onclick='sendMsg(""forward"")'>&rarr;</button>
            <button class='nav-btn' id='btn-reload' title='Reload this page (Ctrl+R)' onclick='sendMsg(""reload"")'>&#x21BB;</button>
            <button class='nav-btn' id='btn-home' title='Open homepage' onclick='sendMsg(""home"")'>&#x2302;</button>
        </div>

        <div class='omnibox-container'>
            <div class='omnibox'>
                <span class='lock-icon'>🔒</span>
                <input class='url-input' id='url-input' type='text' placeholder='Search with Brave or enter address' spellcheck='false' />
                <span class='bookmark-btn' id='btn-star' title='Bookmark this tab (Ctrl+D)' onclick='sendMsg(""bookmark"")'>★</span>
            </div>
        </div>

        <div class='actions-group'>
            <div class='shields-btn' title='Brave Shields' onclick='sendMsg(""shield"")'>
                <span class='shields-lion'>🦁</span>
                <span class='shields-count'>14</span>
            </div>
            <button class='rewards-btn' title='Brave Rewards' onclick='sendMsg(""shield"")'>▲</button>
            <button class='nav-btn' title='History (Ctrl+H)' onclick='sendMsg(""history"")'>🕒</button>
            <button class='nav-btn' title='Source Code (Ctrl+U)' onclick='sendMsg(""source"")'>&lt;/&gt;</button>
            <button class='nav-btn' title='Customize and control Brave' onclick='sendMsg(""menu"")'>&#x2630;</button>
        </div>
    </div>

    <!-- ROW 3: Bookmarks Bar -->
    <div class='bookmarks-row no-drag'>
        <div class='bookmark-item' onclick='sendMsg(""navigate"", { url: ""https://search.brave.com"" })'>
            <span class='bm-icon'>🦁</span>
            <span>Brave Search</span>
        </div>
        <div class='bookmark-item' onclick='sendMsg(""navigate"", { url: ""https://github.com"" })'>
            <span class='bm-icon'>🐙</span>
            <span>GitHub</span>
        </div>
        <div class='bookmark-item' onclick='sendMsg(""navigate"", { url: ""https://youtube.com"" })'>
            <span class='bm-icon'>▶️</span>
            <span>YouTube</span>
        </div>
        <div class='bookmark-item' onclick='sendMsg(""navigate"", { url: ""https://reddit.com"" })'>
            <span class='bm-icon'>💬</span>
            <span>Reddit</span>
        </div>
        <div class='bookmark-item' onclick='sendMsg(""navigate"", { url: ""https://wikipedia.org"" })'>
            <span class='bm-icon'>📚</span>
            <span>Wikipedia</span>
        </div>
        <div class='bookmark-item' onclick='sendMsg(""navigate"", { url: ""https://news.ycombinator.com"" })'>
            <span class='bm-icon'>⚡</span>
            <span>Hacker News</span>
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

        // Global API called by C#
        window.BraveAPI = {
            renderTabs: function(tabs, activeIndex) {
                var container = document.getElementById('tabs-container');
                container.innerHTML = '';
                tabs.forEach(function(tab, index) {
                    var tabEl = document.createElement('div');
                    tabEl.className = 'tab' + (index === activeIndex ? ' active' : '');
                    tabEl.onclick = function() { sendMsg('switch_tab', { index: index }); };

                    var icon = document.createElement('span');
                    icon.className = 'tab-icon';
                    icon.innerText = '🦁';

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

                    tabEl.appendChild(icon);
                    tabEl.appendChild(title);
                    tabEl.appendChild(close);
                    container.appendChild(tabEl);
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
