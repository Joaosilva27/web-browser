using System;

namespace SimpleBrowser
{
    public static class HomePage
    {
        public static string GetHtml()
        {
            return @"<!DOCTYPE html>
<html lang='en'>
<head>
    <meta http-equiv='X-UA-Compatible' content='IE=edge' />
    <meta charset='utf-8' />
    <title>New Tab - Brave</title>
    <style>
        * { box-sizing: border-box; margin: 0; padding: 0; }
        body {
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif;
            background: radial-gradient(circle at 50% 20%, #1e1b38 0%, #111019 70%, #0a0910 100%);
            color: #ffffff;
            min-height: 100vh;
            display: flex;
            flex-direction: column;
            justify-content: space-between;
            padding: 30px 40px;
            user-select: none;
            overflow-x: hidden;
        }

        /* Top Bar / Stats */
        .top-row {
            display: flex;
            justify-content: space-between;
            align-items: flex-start;
            width: 100%;
        }
        .stats-container {
            display: flex;
            gap: 28px;
        }
        .stat-item {
            display: flex;
            flex-direction: column;
        }
        .stat-value {
            font-size: 26px;
            font-weight: 700;
            color: #fb542b;
            letter-spacing: -0.5px;
        }
        .stat-label {
            font-size: 11px;
            color: #9fa4b2;
            text-transform: uppercase;
            font-weight: 600;
            letter-spacing: 0.5px;
            margin-top: 2px;
        }
        .brave-badge {
            display: flex;
            align-items: center;
            gap: 8px;
            background: rgba(251, 84, 43, 0.15);
            border: 1px solid rgba(251, 84, 43, 0.4);
            padding: 6px 14px;
            border-radius: 20px;
            font-size: 13px;
            font-weight: 600;
            color: #ff7654;
        }

        /* Center Content */
        .center-content {
            display: flex;
            flex-direction: column;
            align-items: center;
            text-align: center;
            margin: auto 0;
            width: 100%;
        }
        .clock {
            font-size: 82px;
            font-weight: 300;
            letter-spacing: -2px;
            margin-bottom: 4px;
            text-shadow: 0 4px 18px rgba(0,0,0,0.5);
        }
        .greeting {
            font-size: 22px;
            font-weight: 400;
            color: #c7cbd6;
            margin-bottom: 30px;
        }

        /* Brave Search Bar */
        .search-form {
            width: 100%;
            max-width: 650px;
            position: relative;
            margin-bottom: 40px;
        }
        .search-wrapper {
            display: flex;
            align-items: center;
            background: #1e202d;
            border: 2px solid #2d3042;
            border-radius: 30px;
            padding: 4px 18px;
            box-shadow: 0 8px 30px rgba(0, 0, 0, 0.4);
            transition: all 0.2s ease;
        }
        .search-wrapper:hover {
            border-color: #3f435c;
        }
        .search-wrapper:focus-within {
            border-color: #fb542b;
            box-shadow: 0 8px 32px rgba(251, 84, 43, 0.25);
            background: #232536;
        }
        .search-icon {
            font-size: 18px;
            color: #fb542b;
            margin-right: 12px;
        }
        .search-input {
            width: 100%;
            background: transparent;
            border: none;
            outline: none;
            color: #ffffff;
            font-size: 16px;
            padding: 12px 0;
        }
        .search-input::placeholder {
            color: #7b8194;
        }
        .search-btn {
            background: #fb542b;
            border: none;
            color: #fff;
            padding: 8px 16px;
            border-radius: 20px;
            font-weight: 600;
            font-size: 13px;
            cursor: pointer;
            transition: background 0.2s;
        }
        .search-btn:hover {
            background: #ff6a45;
        }

        /* Shortcuts Grid */
        .shortcuts-grid {
            display: grid;
            grid-template-columns: repeat(6, 1fr);
            gap: 16px;
            max-width: 720px;
            width: 100%;
        }
        .shortcut-card {
            display: flex;
            flex-direction: column;
            align-items: center;
            background: rgba(30, 32, 45, 0.7);
            border: 1px solid rgba(255, 255, 255, 0.06);
            border-radius: 16px;
            padding: 16px 10px;
            text-decoration: none;
            color: #e2e5ec;
            transition: all 0.2s cubic-bezier(0.4, 0, 0.2, 1);
            backdrop-filter: blur(8px);
        }
        .shortcut-card:hover {
            transform: translateY(-4px);
            background: rgba(45, 48, 68, 0.85);
            border-color: #fb542b;
            box-shadow: 0 6px 20px rgba(0,0,0,0.3);
            color: #fff;
        }
        .shortcut-icon {
            font-size: 26px;
            margin-bottom: 8px;
        }
        .shortcut-title {
            font-size: 12px;
            font-weight: 500;
        }

        /* Footer */
        .footer {
            display: flex;
            justify-content: space-between;
            align-items: center;
            font-size: 12px;
            color: #6c7283;
            width: 100%;
        }
        .footer a {
            color: #8b92a5;
            text-decoration: none;
        }
        .footer a:hover {
            color: #fb542b;
        }
    </style>
</head>
<body>
    <div class='top-row'>
        <div class='stats-container'>
            <div class='stat-item'>
                <div class='stat-value' id='stat-trackers'>1,842</div>
                <div class='stat-label'>Trackers &amp; Ads Blocked</div>
            </div>
            <div class='stat-item'>
                <div class='stat-value' id='stat-bandwidth'>24.6 MB</div>
                <div class='stat-label'>Bandwidth Saved</div>
            </div>
            <div class='stat-item'>
                <div class='stat-value' id='stat-time'>1.8 m</div>
                <div class='stat-label'>Time Saved</div>
            </div>
        </div>
        <div class='brave-badge'>
            <span>🦁</span> Brave Shields Active
        </div>
    </div>

    <div class='center-content'>
        <div class='clock' id='clock-display'>12:00</div>
        <div class='greeting' id='greeting-display'>Welcome</div>

        <form class='search-form' action='https://search.brave.com/search' method='GET'>
            <div class='search-wrapper'>
                <div class='search-icon'>🦁</div>
                <input class='search-input' type='text' name='q' placeholder='Search with Brave or enter a site address...' autofocus />
                <button class='search-btn' type='submit'>Search</button>
            </div>
        </form>

        <div class='shortcuts-grid'>
            <a class='shortcut-card' href='https://search.brave.com'>
                <div class='shortcut-icon'>🦁</div>
                <div class='shortcut-title'>Brave Search</div>
            </a>
            <a class='shortcut-card' href='https://github.com'>
                <div class='shortcut-icon'>🐙</div>
                <div class='shortcut-title'>GitHub</div>
            </a>
            <a class='shortcut-card' href='https://youtube.com'>
                <div class='shortcut-icon'>▶️</div>
                <div class='shortcut-title'>YouTube</div>
            </a>
            <a class='shortcut-card' href='https://reddit.com'>
                <div class='shortcut-icon'>💬</div>
                <div class='shortcut-title'>Reddit</div>
            </a>
            <a class='shortcut-card' href='https://wikipedia.org'>
                <div class='shortcut-icon'>📚</div>
                <div class='shortcut-title'>Wikipedia</div>
            </a>
            <a class='shortcut-card' href='https://news.ycombinator.com'>
                <div class='shortcut-icon'>⚡</div>
                <div class='shortcut-title'>Hacker News</div>
            </a>
        </div>
    </div>

    <div class='footer'>
        <div>Press <kbd>Ctrl+T</kbd> for New Tab &bull; <kbd>Ctrl+H</kbd> for History &bull; <kbd>Ctrl+D</kbd> to Bookmark</div>
        <div>Brave UI Edition &bull; Fast, Private &amp; Secure</div>
    </div>

    <script>
        function updateClock() {
            var now = new Date();
            var hours = now.getHours();
            var minutes = now.getMinutes();
            var formattedHours = hours < 10 ? '0' + hours : hours;
            var formattedMinutes = minutes < 10 ? '0' + minutes : minutes;
            document.getElementById('clock-display').innerText = formattedHours + ':' + formattedMinutes;

            var greeting = 'Good evening';
            if (hours >= 5 && hours < 12) {
                greeting = 'Good morning';
            } else if (hours >= 12 && hours < 18) {
                greeting = 'Good afternoon';
            }
            document.getElementById('greeting-display').innerText = greeting;
        }
        updateClock();
        setInterval(updateClock, 1000);
    </script>
</body>
</html>";
        }
    }
}
