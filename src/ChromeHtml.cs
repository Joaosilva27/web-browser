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
    <title>Browser Chrome</title>
    <link rel='preconnect' href='https://fonts.googleapis.com' />
    <link rel='preconnect' href='https://fonts.gstatic.com' crossorigin />
    <link href='https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap' rel='stylesheet' />
    <style>
        * { box-sizing: border-box; margin: 0; padding: 0; user-select: none; }

        :root {
            --frame: #121212;
            --toolbar: #202124;
            --tab-strip: #181818;
            --tab-active: #202124;
            --tab-inactive-hover: #1f2023;
            --omnibox-bg: #2a2a2d;
            --omnibox-border: #3c3c42;
            --omnibox-focus: #fb542b;
            --brave-orange: #fb542b;
            --brave-orange-light: #ff7654;
            --text-primary: #e8eaed;
            --text-secondary: #9aa0a6;
            --text-tab-inactive: #969696;
            --separator: rgba(255,255,255,0.08);
            --btn-hover: rgba(255,255,255,0.10);
        }

        body {
            font-family: 'Inter', -apple-system, BlinkMacSystemFont, 'Helvetica Neue', 'Segoe UI', Roboto, sans-serif;
            background: var(--frame);
            color: var(--text-primary);
            height: 100vh;
            display: flex;
            flex-direction: column;
            overflow: hidden;
            -webkit-font-smoothing: antialiased;
            -moz-osx-font-smoothing: grayscale;
        }

        .drag-region { -webkit-app-region: drag; }
        .no-drag { -webkit-app-region: no-drag; }

        /* ── ROW 1: TAB STRIP ── */
        .tab-strip {
            height: 36px;
            background: var(--tab-strip);
            display: flex;
            align-items: flex-end;
            padding: 0 0 0 8px;
        }

        .tabs-scroll {
            display: flex;
            align-items: flex-end;
            gap: 1px;
            flex: 1;
            height: 100%;
            overflow-x: auto;
            scrollbar-width: none;
        }
        .tabs-scroll::-webkit-scrollbar { display: none; }

        .tab {
            display: flex;
            align-items: center;
            gap: 6px;
            padding: 0 14px 0 10px;
            height: 32px;
            background: transparent;
            border-radius: 8px 8px 0 0;
            font-size: 12px;
            font-weight: 500;
            color: var(--text-tab-inactive);
            cursor: pointer;
            max-width: 240px;
            min-width: 100px;
            transition: background 0.12s, color 0.12s;
            position: relative;
            letter-spacing: -0.01em;
        }
        .tab:hover {
            background: var(--tab-inactive-hover);
            color: var(--text-primary);
        }
        .tab.active {
            background: var(--tab-active);
            color: #fff;
        }
        /* Separator lines between inactive tabs */
        .tab:not(.active) + .tab:not(.active)::before {
            content: '';
            position: absolute;
            left: -1px;
            top: 8px;
            bottom: 8px;
            width: 1px;
            background: var(--separator);
        }
        .tab-favicon {
            width: 16px;
            height: 16px;
            border-radius: 3px;
            background: var(--brave-orange);
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 9px;
            color: #fff;
            font-weight: 700;
            flex-shrink: 0;
        }
        .tab-title {
            white-space: nowrap;
            overflow: hidden;
            text-overflow: ellipsis;
            flex: 1;
        }
        .tab-close {
            width: 18px;
            height: 18px;
            border-radius: 50%;
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 11px;
            color: var(--text-secondary);
            transition: background 0.12s, color 0.12s;
            flex-shrink: 0;
            opacity: 0;
        }
        .tab:hover .tab-close,
        .tab.active .tab-close { opacity: 1; }
        .tab-close:hover {
            background: rgba(255,255,255,0.18);
            color: #fff;
        }

        .new-tab-btn {
            width: 28px;
            height: 28px;
            border-radius: 50%;
            color: var(--text-secondary);
            display: flex;
            align-items: center;
            justify-content: center;
            cursor: pointer;
            font-size: 18px;
            font-weight: 300;
            margin: 0 4px 2px;
            transition: background 0.12s, color 0.12s;
            flex-shrink: 0;
        }
        .new-tab-btn:hover {
            background: var(--btn-hover);
            color: #fff;
        }

        /* Spacer pushes caption buttons right */
        .tab-strip-spacer { flex: 0; min-width: 60px; }

        /* Windows caption buttons */
        .window-controls {
            display: flex;
            height: 36px;
            margin-left: auto;
        }
        .caption-btn {
            width: 46px;
            height: 36px;
            display: flex;
            align-items: center;
            justify-content: center;
            background: transparent;
            border: none;
            color: var(--text-secondary);
            font-size: 10px;
            cursor: pointer;
            transition: background 0.12s, color 0.12s;
            font-family: 'Segoe MDL2 Assets', 'Segoe Fluent Icons', sans-serif;
        }
        .caption-btn:hover {
            background: var(--btn-hover);
            color: #fff;
        }
        .caption-btn.close-btn:hover {
            background: #c42b1c;
            color: #fff;
        }
        .caption-btn svg {
            width: 10px;
            height: 10px;
            fill: currentColor;
        }

        /* ── ROW 2: TOOLBAR ── */
        .toolbar {
            height: 40px;
            background: var(--toolbar);
            display: flex;
            align-items: center;
            padding: 0 8px;
            gap: 4px;
        }

        .nav-group {
            display: flex;
            align-items: center;
            gap: 2px;
        }
        .icon-btn {
            width: 28px;
            height: 28px;
            border-radius: 50%;
            background: transparent;
            border: none;
            color: var(--text-secondary);
            display: flex;
            align-items: center;
            justify-content: center;
            cursor: pointer;
            font-size: 16px;
            transition: background 0.12s, color 0.12s;
        }
        .icon-btn:hover {
            background: var(--btn-hover);
            color: #fff;
        }
        .icon-btn:disabled {
            opacity: 0.30;
            cursor: default;
            pointer-events: none;
        }
        .icon-btn svg {
            width: 16px;
            height: 16px;
            fill: currentColor;
        }

        /* Omnibox */
        .omnibox-wrap {
            flex: 1;
            margin: 0 6px;
        }
        .omnibox {
            display: flex;
            align-items: center;
            background: var(--omnibox-bg);
            border: 1.5px solid transparent;
            border-radius: 24px;
            height: 30px;
            padding: 0 12px;
            gap: 8px;
            transition: border-color 0.2s, background 0.2s, box-shadow 0.2s;
        }
        .omnibox:hover {
            background: #313136;
        }
        .omnibox:focus-within {
            border-color: var(--omnibox-focus);
            background: #313136;
            box-shadow: 0 0 0 1px var(--omnibox-focus);
        }
        .lock-icon {
            font-size: 12px;
            color: var(--text-secondary);
            flex-shrink: 0;
        }
        .url-input {
            flex: 1;
            background: transparent;
            border: none;
            outline: none;
            color: var(--text-primary);
            font-size: 13px;
            font-family: 'Inter', -apple-system, BlinkMacSystemFont, 'Helvetica Neue', 'Segoe UI', sans-serif;
            font-weight: 400;
            letter-spacing: -0.01em;
        }
        .url-input::placeholder {
            color: #6b7280;
        }
        .star-btn {
            font-size: 14px;
            color: var(--text-secondary);
            cursor: pointer;
            transition: color 0.15s;
            flex-shrink: 0;
        }
        .star-btn.bookmarked {
            color: var(--brave-orange);
        }

        /* Actions right side */
        .actions-group {
            display: flex;
            align-items: center;
            gap: 2px;
        }

        /* Shields lion pill */
        .shields-pill {
            display: flex;
            align-items: center;
            gap: 4px;
            background: rgba(251, 84, 43, 0.12);
            border: 1px solid rgba(251, 84, 43, 0.35);
            border-radius: 12px;
            padding: 3px 8px 3px 6px;
            cursor: pointer;
            transition: background 0.15s;
            margin-right: 2px;
        }
        .shields-pill:hover {
            background: rgba(251, 84, 43, 0.22);
        }
        .shields-lion {
            font-size: 13px;
        }
        .shields-count {
            font-size: 11px;
            font-weight: 700;
            color: var(--brave-orange);
            font-family: 'Inter', sans-serif;
        }

        /* ── ROW 3: BOOKMARKS BAR ── */
        .bookmarks-bar {
            height: 26px;
            background: var(--toolbar);
            display: flex;
            align-items: center;
            padding: 0 12px;
            gap: 2px;
            border-top: 1px solid var(--separator);
        }
        .bm-item {
            display: flex;
            align-items: center;
            gap: 6px;
            font-size: 12px;
            font-weight: 500;
            color: var(--text-secondary);
            cursor: pointer;
            padding: 3px 8px;
            border-radius: 4px;
            transition: background 0.12s, color 0.12s;
            white-space: nowrap;
            letter-spacing: -0.01em;
        }
        .bm-item:hover {
            background: var(--btn-hover);
            color: var(--text-primary);
        }
        .bm-favicon {
            font-size: 12px;
            flex-shrink: 0;
        }
    </style>
</head>
<body class='drag-region'>

    <!-- TAB STRIP -->
    <div class='tab-strip'>
        <div class='tabs-scroll no-drag' id='tabs-container'></div>
        <div class='new-tab-btn no-drag' title='New tab (Ctrl+T)' onclick='post(""new_tab"")'>+</div>
        <div class='tab-strip-spacer'></div>
        <div class='window-controls no-drag'>
            <button class='caption-btn' title='Minimize' onclick='post(""window_min"")'>
                <svg viewBox='0 0 10 1'><rect width='10' height='1'/></svg>
            </button>
            <button class='caption-btn' title='Maximize' onclick='post(""window_max"")'>
                <svg viewBox='0 0 10 10'><rect x='0.5' y='0.5' width='9' height='9' fill='none' stroke='currentColor' stroke-width='1'/></svg>
            </button>
            <button class='caption-btn close-btn' title='Close' onclick='post(""window_close"")'>
                <svg viewBox='0 0 10 10'><line x1='1' y1='1' x2='9' y2='9' stroke='currentColor' stroke-width='1.2'/><line x1='9' y1='1' x2='1' y2='9' stroke='currentColor' stroke-width='1.2'/></svg>
            </button>
        </div>
    </div>

    <!-- TOOLBAR -->
    <div class='toolbar no-drag'>
        <div class='nav-group'>
            <button class='icon-btn' id='btn-back' title='Back (Alt+Left)' onclick='post(""back"")'>
                <svg viewBox='0 0 24 24'><path d='M15.41 7.41 14 6l-6 6 6 6 1.41-1.41L10.83 12z'/></svg>
            </button>
            <button class='icon-btn' id='btn-fwd' title='Forward (Alt+Right)' onclick='post(""forward"")'>
                <svg viewBox='0 0 24 24'><path d='M8.59 16.59 10 18l6-6-6-6-1.41 1.41L13.17 12z'/></svg>
            </button>
            <button class='icon-btn' id='btn-reload' title='Reload (Ctrl+R)' onclick='post(""reload"")'>
                <svg viewBox='0 0 24 24'><path d='M17.65 6.35A7.958 7.958 0 0 0 12 4c-4.42 0-7.99 3.58-7.99 8s3.57 8 7.99 8c3.73 0 6.84-2.55 7.73-6h-2.08A5.99 5.99 0 0 1 12 18c-3.31 0-6-2.69-6-6s2.69-6 6-6c1.66 0 3.14.69 4.22 1.78L13 11h7V4l-2.35 2.35z'/></svg>
            </button>
        </div>

        <div class='omnibox-wrap'>
            <div class='omnibox'>
                <span class='lock-icon'>
                    <svg width='12' height='12' viewBox='0 0 24 24' fill='currentColor'><path d='M18 8h-1V6c0-2.76-2.24-5-5-5S7 3.24 7 6v2H6c-1.1 0-2 .9-2 2v10c0 1.1.9 2 2 2h12c1.1 0 2-.9 2-2V10c0-1.1-.9-2-2-2zM12 17c-1.1 0-2-.9-2-2s.9-2 2-2 2 .9 2 2-.9 2-2 2zM9 8V6c0-1.66 1.34-3 3-3s3 1.34 3 3v2H9z'/></svg>
                </span>
                <input class='url-input' id='url-input' type='text' placeholder='Search Brave or type a URL' spellcheck='false' />
                <span class='star-btn' id='btn-star' title='Bookmark this tab (Ctrl+D)' onclick='post(""bookmark"")'>☆</span>
            </div>
        </div>

        <div class='actions-group'>
            <div class='shields-pill' title='Brave Shields: Trackers blocked' onclick='post(""shield"")'>
                <span class='shields-lion'>🦁</span>
                <span class='shields-count' id='shields-count'>0</span>
            </div>
            <button class='icon-btn' title='Brave Rewards'>
                <svg viewBox='0 0 24 24' fill='var(--brave-orange)'><path d='M12 2 L22 8.5 L22 15.5 L12 22 L2 15.5 L2 8.5 Z' fill='none' stroke='var(--brave-orange)' stroke-width='1.5'/><path d='M12 6 L16 9 L14 14 L10 14 L8 9 Z' fill='var(--brave-orange)' opacity='0.7'/></svg>
            </button>
            <button class='icon-btn' title='History (Ctrl+H)' onclick='post(""history"")'>
                <svg viewBox='0 0 24 24'><path d='M13 3a9 9 0 0 0-9 9H1l3.89 3.89.07.14L9 12H6c0-3.87 3.13-7 7-7s7 3.13 7 7-3.13 7-7 7c-1.93 0-3.68-.79-4.94-2.06l-1.42 1.42A8.954 8.954 0 0 0 13 21a9 9 0 0 0 0-18zm-1 5v5l4.28 2.54.72-1.21-3.5-2.08V8H12z'/></svg>
            </button>
            <button class='icon-btn' title='View page source (Ctrl+U)' onclick='post(""source"")'>
                <svg viewBox='0 0 24 24'><path d='M9.4 16.6 4.8 12l4.6-4.6L8 6l-6 6 6 6 1.4-1.4zm5.2 0 4.6-4.6-4.6-4.6L16 6l6 6-6 6-1.4-1.4z'/></svg>
            </button>
            <button class='icon-btn' title='Customize and control Brave' onclick='post(""menu"")'>
                <svg viewBox='0 0 24 24'><circle cx='12' cy='5' r='2'/><circle cx='12' cy='12' r='2'/><circle cx='12' cy='19' r='2'/></svg>
            </button>
        </div>
    </div>

    <!-- BOOKMARKS BAR -->
    <div class='bookmarks-bar no-drag' id='bookmarks-bar'>
        <div class='bm-item' onclick='post(""navigate"",{url:""https://search.brave.com""})'>
            <span class='bm-favicon'>🦁</span><span>Brave Search</span>
        </div>
        <div class='bm-item' onclick='post(""navigate"",{url:""https://github.com""})'>
            <span class='bm-favicon'>🐙</span><span>GitHub</span>
        </div>
        <div class='bm-item' onclick='post(""navigate"",{url:""https://youtube.com""})'>
            <span class='bm-favicon'>▶️</span><span>YouTube</span>
        </div>
        <div class='bm-item' onclick='post(""navigate"",{url:""https://reddit.com""})'>
            <span class='bm-favicon'>💬</span><span>Reddit</span>
        </div>
        <div class='bm-item' onclick='post(""navigate"",{url:""https://wikipedia.org""})'>
            <span class='bm-favicon'>📚</span><span>Wikipedia</span>
        </div>
        <div class='bm-item' onclick='post(""navigate"",{url:""https://news.ycombinator.com""})'>
            <span class='bm-favicon'>⚡</span><span>Hacker News</span>
        </div>
    </div>

    <script>
        function post(action, data) {
            if (window.chrome && window.chrome.webview) {
                window.chrome.webview.postMessage(Object.assign({action:action}, data||{}));
            }
        }

        var urlInput = document.getElementById('url-input');
        urlInput.addEventListener('keydown', function(e) {
            if (e.key === 'Enter') post('navigate', {url: urlInput.value.trim()});
        });
        urlInput.addEventListener('focus', function() { urlInput.select(); });

        window.BraveAPI = {
            renderTabs: function(tabs, activeIdx) {
                var c = document.getElementById('tabs-container');
                c.innerHTML = '';
                for (var i = 0; i < tabs.length; i++) {
                    (function(idx) {
                        var t = document.createElement('div');
                        t.className = 'tab' + (idx === activeIdx ? ' active' : '');
                        t.onclick = function(){ post('switch_tab', {index:idx}); };

                        var fav = document.createElement('div');
                        fav.className = 'tab-favicon';
                        fav.innerText = (tabs[idx].title || 'N')[0].toUpperCase();

                        var title = document.createElement('span');
                        title.className = 'tab-title';
                        title.innerText = tabs[idx].title || 'New Tab';

                        var close = document.createElement('span');
                        close.className = 'tab-close';
                        close.innerText = '\u00d7';
                        close.onclick = function(e){ e.stopPropagation(); post('close_tab',{index:idx}); };

                        t.appendChild(fav);
                        t.appendChild(title);
                        t.appendChild(close);
                        c.appendChild(t);
                    })(i);
                }
            },
            setUrl: function(url) {
                urlInput.value = (url === 'about:home' ? '' : url);
            },
            setNavState: function(canBack, canFwd) {
                document.getElementById('btn-back').disabled = !canBack;
                document.getElementById('btn-fwd').disabled = !canFwd;
            },
            setBookmarked: function(yes) {
                var s = document.getElementById('btn-star');
                s.innerText = yes ? '\u2605' : '\u2606';
                s.className = 'star-btn' + (yes ? ' bookmarked' : '');
            }
        };
    </script>
</body>
</html>";
        }
    }
}
