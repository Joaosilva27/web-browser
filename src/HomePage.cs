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
    <title>Aether &bull; New Tab</title>
    <style>
        * { box-sizing: border-box; margin: 0; padding: 0; }

        :root {
            --bg-base: #07080b;
            --accent-cyan: #00f2fe;
            --accent-purple: #9d4edd;
            --accent-blue: #4facfe;
            --glass-bg: rgba(255, 255, 255, 0.04);
            --glass-border: rgba(255, 255, 255, 0.09);
            --glass-hover: rgba(255, 255, 255, 0.08);
            --text-primary: #f5f5f7;
            --text-secondary: #86868b;
        }

        body {
            font-family: -apple-system, BlinkMacSystemFont, 'SF Pro Display', 'Segoe UI Variable', Roboto, sans-serif;
            background-color: var(--bg-base);
            color: var(--text-primary);
            min-height: 100vh;
            display: flex;
            flex-direction: column;
            justify-content: space-between;
            padding: 40px 60px;
            overflow-x: hidden;
            position: relative;
            user-select: none;
        }

        /* Ambient Lighting / Cosmic Gradient Orbs */
        .ambient-glow-1 {
            position: fixed;
            top: -20%;
            left: 20%;
            width: 600px;
            height: 600px;
            background: radial-gradient(circle, rgba(0, 242, 254, 0.09) 0%, transparent 70%);
            filter: blur(80px);
            pointer-events: none;
            z-index: 0;
            animation: drift 18s ease-in-out infinite alternate;
        }
        .ambient-glow-2 {
            position: fixed;
            bottom: -15%;
            right: 15%;
            width: 700px;
            height: 700px;
            background: radial-gradient(circle, rgba(157, 78, 221, 0.08) 0%, transparent 70%);
            filter: blur(90px);
            pointer-events: none;
            z-index: 0;
            animation: drift 22s ease-in-out infinite alternate-reverse;
        }

        @keyframes drift {
            0% { transform: translateY(0px) scale(1); }
            100% { transform: translateY(60px) scale(1.15); }
        }

        /* Header / Telemetry */
        .top-bar {
            display: flex;
            justify-content: space-between;
            align-items: center;
            z-index: 1;
        }
        .brand-capsule {
            display: inline-flex;
            align-items: center;
            gap: 10px;
            background: var(--glass-bg);
            border: 1px solid var(--glass-border);
            padding: 8px 18px;
            border-radius: 40px;
            backdrop-filter: blur(24px);
            font-size: 13px;
            letter-spacing: 0.5px;
            font-weight: 500;
            color: #d1d5db;
        }
        .brand-icon {
            color: var(--accent-cyan);
            font-size: 14px;
            animation: pulse 3s infinite;
        }
        @keyframes pulse {
            0%, 100% { opacity: 0.7; transform: scale(1); }
            50% { opacity: 1; transform: scale(1.2); }
        }

        .telemetry-row {
            display: flex;
            gap: 20px;
        }
        .telemetry-pill {
            display: inline-flex;
            align-items: center;
            gap: 8px;
            background: var(--glass-bg);
            border: 1px solid var(--glass-border);
            padding: 6px 14px;
            border-radius: 30px;
            font-size: 12px;
            color: var(--text-secondary);
        }
        .telemetry-val {
            color: var(--accent-cyan);
            font-weight: 600;
        }

        /* Center Content */
        .hero {
            display: flex;
            flex-direction: column;
            align-items: center;
            text-align: center;
            margin: auto 0;
            z-index: 1;
            width: 100%;
        }

        .clock {
            font-size: 88px;
            font-weight: 200;
            letter-spacing: -3px;
            color: #ffffff;
            margin-bottom: 6px;
            line-height: 1;
            background: linear-gradient(180deg, #ffffff 40%, rgba(255, 255, 255, 0.6) 100%);
            -webkit-background-clip: text;
            -webkit-text-fill-color: transparent;
        }
        .greeting {
            font-size: 19px;
            font-weight: 400;
            color: var(--text-secondary);
            margin-bottom: 38px;
            letter-spacing: -0.2px;
        }

        /* Futuristic Floating Omnibar Search */
        .search-container {
            width: 100%;
            max-width: 640px;
            position: relative;
            margin-bottom: 44px;
        }
        .search-pill {
            display: flex;
            align-items: center;
            background: rgba(18, 20, 29, 0.75);
            border: 1px solid rgba(255, 255, 255, 0.12);
            border-radius: 50px;
            padding: 6px 20px;
            box-shadow: 0 16px 40px rgba(0, 0, 0, 0.5), inset 0 1px 1px rgba(255, 255, 255, 0.1);
            backdrop-filter: blur(32px);
            transition: all 0.3s cubic-bezier(0.16, 1, 0.3, 1);
        }
        .search-pill:hover {
            border-color: rgba(0, 242, 254, 0.4);
            box-shadow: 0 20px 50px rgba(0, 0, 0, 0.6), 0 0 24px rgba(0, 242, 254, 0.15);
        }
        .search-pill:focus-within {
            border-color: var(--accent-cyan);
            background: rgba(22, 25, 38, 0.95);
            box-shadow: 0 20px 60px rgba(0, 0, 0, 0.7), 0 0 35px rgba(0, 242, 254, 0.25);
            transform: scale(1.015);
        }
        .search-icon {
            font-size: 16px;
            color: var(--accent-cyan);
            margin-right: 14px;
        }
        .search-input {
            width: 100%;
            background: transparent;
            border: none;
            outline: none;
            color: #ffffff;
            font-size: 16px;
            padding: 12px 0;
            font-weight: 400;
            font-family: inherit;
        }
        .search-input::placeholder {
            color: #636778;
        }
        .search-action-btn {
            background: linear-gradient(135deg, #00f2fe 0%, #4facfe 100%);
            border: none;
            color: #07080b;
            padding: 8px 18px;
            border-radius: 30px;
            font-weight: 600;
            font-size: 13px;
            cursor: pointer;
            transition: opacity 0.2s, transform 0.2s;
            font-family: inherit;
        }
        .search-action-btn:hover {
            opacity: 0.92;
            transform: scale(1.04);
        }

        /* Apple VisionOS Frosted Glass Shortcut Cards */
        .dock-grid {
            display: grid;
            grid-template-columns: repeat(6, 1fr);
            gap: 16px;
            max-width: 720px;
            width: 100%;
        }
        .dock-card {
            display: flex;
            flex-direction: column;
            align-items: center;
            background: var(--glass-bg);
            border: 1px solid var(--glass-border);
            border-radius: 20px;
            padding: 18px 12px;
            text-decoration: none;
            color: var(--text-primary);
            backdrop-filter: blur(28px);
            transition: all 0.25s cubic-bezier(0.16, 1, 0.3, 1);
            position: relative;
            overflow: hidden;
        }
        .dock-card::before {
            content: '';
            position: absolute;
            top: 0;
            left: 0;
            right: 0;
            height: 1px;
            background: linear-gradient(90deg, transparent, rgba(255, 255, 255, 0.2), transparent);
        }
        .dock-card:hover {
            transform: translateY(-5px);
            background: var(--glass-hover);
            border-color: rgba(0, 242, 254, 0.4);
            box-shadow: 0 12px 30px rgba(0, 0, 0, 0.4), 0 0 20px rgba(0, 242, 254, 0.12);
            color: #fff;
        }
        .dock-icon {
            font-size: 26px;
            margin-bottom: 8px;
        }
        .dock-title {
            font-size: 12px;
            font-weight: 500;
            color: #c0c4d2;
        }

        /* Minimal Footer */
        .footer {
            display: flex;
            justify-content: space-between;
            align-items: center;
            font-size: 12px;
            color: var(--text-secondary);
            z-index: 1;
            padding-top: 20px;
        }
        .footer-shortcuts {
            display: flex;
            gap: 14px;
        }
        kbd {
            background: rgba(255, 255, 255, 0.08);
            border: 1px solid rgba(255, 255, 255, 0.12);
            border-radius: 6px;
            padding: 2px 7px;
            font-size: 11px;
            font-family: inherit;
            color: #e5e7eb;
        }
    </style>
</head>
<body>
    <div class='ambient-glow-1'></div>
    <div class='ambient-glow-2'></div>

    <div class='top-bar'>
        <div class='brand-capsule'>
            <span class='brand-icon'>✦</span>
            <span>AETHER 2.0</span>
        </div>
        <div class='telemetry-row'>
            <div class='telemetry-pill'>
                <span>Shield:</span>
                <span class='telemetry-val'>Protected</span>
            </div>
            <div class='telemetry-pill'>
                <span>Telemetry:</span>
                <span class='telemetry-val'>Zero-Footprint</span>
            </div>
            <div class='telemetry-pill'>
                <span>Engine:</span>
                <span class='telemetry-val'>Chromium 155</span>
            </div>
        </div>
    </div>

    <div class='hero'>
        <div class='clock' id='clock-display'>12:00</div>
        <div class='greeting' id='greeting-display'>Good evening, Explorer</div>

        <form class='search-container' action='https://www.google.com/search' method='GET'>
            <div class='search-pill'>
                <div class='search-icon'>✦</div>
                <input class='search-input' type='text' name='q' placeholder='Explore the web or enter address...' autofocus />
                <button class='search-action-btn' type='submit'>Explore</button>
            </div>
        </form>

        <div class='dock-grid'>
            <a class='dock-card' href='https://github.com'>
                <div class='dock-icon'>🐙</div>
                <div class='dock-title'>GitHub</div>
            </a>
            <a class='dock-card' href='https://youtube.com'>
                <div class='dock-icon'>▶️</div>
                <div class='dock-title'>YouTube</div>
            </a>
            <a class='dock-card' href='https://reddit.com'>
                <div class='dock-icon'>💬</div>
                <div class='dock-title'>Reddit</div>
            </a>
            <a class='dock-card' href='https://wikipedia.org'>
                <div class='dock-icon'>📚</div>
                <div class='dock-title'>Wikipedia</div>
            </a>
            <a class='dock-card' href='https://news.ycombinator.com'>
                <div class='dock-icon'>⚡</div>
                <div class='dock-title'>Hacker News</div>
            </a>
            <a class='dock-card' href='https://apple.com'>
                <div class='dock-icon'>🍎</div>
                <div class='dock-title'>Apple</div>
            </a>
        </div>
    </div>

    <div class='footer'>
        <div class='footer-shortcuts'>
            <span><kbd>⌘ T</kbd> New Tab</span>
            <span><kbd>⌘ L</kbd> Focus Omnibar</span>
            <span><kbd>⌘ D</kbd> Bookmark</span>
            <span><kbd>⌘ H</kbd> History</span>
        </div>
        <div>Aether &bull; Designed with Cupertino Simplicity &bull; Fast, Private &amp; Modern</div>
    </div>

    <script>
        function updateClock() {
            var now = new Date();
            var hours = now.getHours();
            var minutes = now.getMinutes();
            var formattedHours = hours < 10 ? '0' + hours : hours;
            var formattedMinutes = minutes < 10 ? '0' + minutes : minutes;
            document.getElementById('clock-display').innerText = formattedHours + ':' + formattedMinutes;

            var greeting = 'Good evening, Explorer';
            if (hours >= 5 && hours < 12) {
                greeting = 'Good morning, Explorer';
            } else if (hours >= 12 && hours < 18) {
                greeting = 'Good afternoon, Explorer';
            }
            document.getElementById('greeting-display').innerText = greeting;
        }
        updateClock();
        setInterval(updateClock, 1000);

        var form = document.querySelector('.search-container');
        if (form) {
            form.addEventListener('submit', function(e) {
                var input = document.querySelector('.search-input');
                if (!input) return;
                var val = input.value.trim();
                if (!val) {
                    e.preventDefault();
                    return;
                }
                if (val.indexOf('http://') === 0 || val.indexOf('https://') === 0) {
                    window.location.href = val;
                    e.preventDefault();
                } else if (val.indexOf('.') !== -1 && val.indexOf(' ') === -1) {
                    window.location.href = 'https://' + val;
                    e.preventDefault();
                }
            });
        }
    </script>
</body>
</html>";
        }
    }
}
