using System;

namespace SimpleBrowser
{
    public static class HomePage
    {
        public static string GetHtml()
        {
            return @"<!DOCTYPE html>
<html>
<head>
    <meta http-equiv='X-UA-Compatible' content='IE=edge' />
    <meta charset='utf-8' />
    <title>New Tab</title>
    <style>
        * { box-sizing: border-box; margin: 0; padding: 0; }
        body {
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif;
            background: #181a1b;
            color: #e8e6e3;
            display: flex;
            flex-direction: column;
            align-items: center;
            justify-content: center;
            min-height: 100vh;
            padding: 20px;
        }
        .container {
            text-align: center;
            max-width: 680px;
            width: 100%;
        }
        .logo {
            font-size: 52px;
            font-weight: 700;
            background: linear-gradient(135deg, #00d2ff 0%, #3a7bd5 100%);
            -webkit-background-clip: text;
            -webkit-text-fill-color: transparent;
            margin-bottom: 8px;
            letter-spacing: -1px;
        }
        .subtitle {
            font-size: 15px;
            color: #9ba3af;
            margin-bottom: 32px;
        }
        .search-box {
            position: relative;
            margin-bottom: 40px;
        }
        .search-box input {
            width: 100%;
            padding: 16px 24px;
            font-size: 16px;
            border-radius: 28px;
            border: 1px solid #33383e;
            background: #23272a;
            color: #fff;
            outline: none;
            box-shadow: 0 4px 14px rgba(0,0,0,0.3);
            transition: border-color 0.2s, box-shadow 0.2s;
        }
        .search-box input:focus {
            border-color: #3a7bd5;
            box-shadow: 0 4px 20px rgba(58, 123, 213, 0.4);
        }
        .shortcuts {
            display: grid;
            grid-template-columns: repeat(4, 1fr);
            gap: 16px;
        }
        .card {
            background: #23272a;
            border: 1px solid #2f343b;
            border-radius: 12px;
            padding: 18px 12px;
            text-decoration: none;
            color: #e8e6e3;
            display: flex;
            flex-direction: column;
            align-items: center;
            transition: transform 0.2s, background 0.2s, border-color 0.2s;
        }
        .card:hover {
            transform: translateY(-3px);
            background: #2c3238;
            border-color: #3a7bd5;
            color: #fff;
        }
        .card-icon {
            font-size: 26px;
            margin-bottom: 10px;
        }
        .card-title {
            font-size: 13px;
            font-weight: 500;
        }
        .footer {
            margin-top: 50px;
            font-size: 12px;
            color: #6c757d;
        }
    </style>
</head>
<body>
    <div class='container'>
        <div class='logo'>Aura Browser</div>
        <p class='subtitle'>Lightweight, Fast &amp; Simple Web Browser</p>
        
        <form class='search-box' action='https://www.google.com/search' method='GET'>
            <input type='text' name='q' placeholder='Search Google or enter address...' autofocus />
        </form>

        <div class='shortcuts'>
            <a class='card' href='https://www.google.com'>
                <div class='card-icon'>🔍</div>
                <div class='card-title'>Google</div>
            </a>
            <a class='card' href='https://github.com'>
                <div class='card-icon'>🐙</div>
                <div class='card-title'>GitHub</div>
            </a>
            <a class='card' href='https://www.wikipedia.org'>
                <div class='card-icon'>📚</div>
                <div class='card-title'>Wikipedia</div>
            </a>
            <a class='card' href='https://news.ycombinator.com'>
                <div class='card-icon'>⚡</div>
                <div class='card-title'>Hacker News</div>
            </a>
            <a class='card' href='https://www.reddit.com'>
                <div class='card-icon'>💬</div>
                <div class='card-title'>Reddit</div>
            </a>
            <a class='card' href='https://www.youtube.com'>
                <div class='card-icon'>▶️</div>
                <div class='card-title'>YouTube</div>
            </a>
            <a class='card' href='https://developer.mozilla.org'>
                <div class='card-icon'>📖</div>
                <div class='card-title'>MDN Docs</div>
            </a>
            <a class='card' href='https://duckduckgo.com'>
                <div class='card-icon'>🦆</div>
                <div class='card-title'>DuckDuckGo</div>
            </a>
        </div>

        <div class='footer'>
            Aura Web Browser &bull; Press Ctrl+T for New Tab &bull; Ctrl+H for History &bull; Ctrl+D to Bookmark
        </div>
    </div>
</body>
</html>";
        }
    }
}
