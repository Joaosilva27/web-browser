using System;
using System.Text;

namespace SimpleBrowser
{
    public static class HomePage
    {
        public static string GetHtml(bool dark)
        {
            int blocked = AdBlocker.BlockedCount;
            string shieldsNote = blocked > 0
                ? blocked.ToString("N0") + " trackers &amp; ads blocked this session"
                : "Aura Shields are active";

            string html = @"<!DOCTYPE html>
<html lang='en'>
<head>
<meta charset='utf-8' />
<meta name='viewport' content='width=device-width, initial-scale=1.0' />
<title>New Tab</title>
<style>
    * { box-sizing: border-box; margin: 0; padding: 0; }

    body {
        --text: #1d1d1f;
        --text2: #6e6e73;
        --text3: #9a9aa0;
        --field: #ffffff;
        --field-line: rgba(0, 0, 0, 0.06);
        --field-shadow: 0 1px 2px rgba(0, 0, 0, 0.05), 0 6px 22px rgba(0, 0, 0, 0.07);
        --tile-shadow: 0 1px 2px rgba(0, 0, 0, 0.14);
        --tile-bg: #ffffff;
        --tile-line: rgba(0, 0, 0, 0.07);
        --hover: rgba(0, 0, 0, 0.045);
        --accent: #007aff;
        --ring: rgba(0, 122, 255, 0.16);
        --orange: #f25a29;
        --green: #34c759;

        min-height: 100vh;
        font-family: 'SF Pro Text', 'SF Pro Display', -apple-system, BlinkMacSystemFont,
                     'Segoe UI Variable Text', 'Segoe UI', system-ui, sans-serif;
        color: var(--text);
        background:
            radial-gradient(1000px 520px at 50% -10%, #dbe8ff 0%, rgba(219, 232, 255, 0) 60%),
            radial-gradient(720px 420px at 86% 112%, rgba(255, 226, 214, 0.55) 0%, rgba(255, 226, 214, 0) 62%),
            linear-gradient(180deg, #f8f9fc 0%, #eef0f5 100%);
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        overflow: auto;
        -webkit-font-smoothing: antialiased;
    }

    body.dark {
        --text: #f5f5f7;
        --text2: #a1a1a6;
        --text3: #8a8a90;
        --field: #2a2a2d;
        --field-line: rgba(255, 255, 255, 0.07);
        --field-shadow: 0 1px 2px rgba(0, 0, 0, 0.35), 0 8px 26px rgba(0, 0, 0, 0.28);
        --tile-shadow: 0 1px 2px rgba(0, 0, 0, 0.40);
        --tile-bg: #ffffff;
        --tile-line: rgba(0, 0, 0, 0.12);
        --hover: rgba(255, 255, 255, 0.06);
        --accent: #0a84ff;
        --ring: rgba(10, 132, 255, 0.22);
        --orange: #ff6b3d;
        --green: #30d158;

        background:
            radial-gradient(1000px 520px at 50% -10%, #26364f 0%, rgba(38, 54, 79, 0) 60%),
            radial-gradient(720px 420px at 86% 112%, rgba(84, 48, 32, 0.38) 0%, rgba(84, 48, 32, 0) 62%),
            linear-gradient(180deg, #17191e 0%, #0f1013 100%);
    }

    .stage {
        width: 100%;
        max-width: 700px;
        padding: 40px 28px 90px;
        display: flex;
        flex-direction: column;
        align-items: center;
    }

    .date {
        font-size: 11.5px;
        font-weight: 600;
        letter-spacing: 1.5px;
        text-transform: uppercase;
        color: var(--text3);
        margin-bottom: 6px;
    }

    .clock {
        font-size: 60px;
        font-weight: 200;
        letter-spacing: -2.6px;
        line-height: 1.06;
        font-variant-numeric: tabular-nums;
        margin-bottom: 30px;
    }

    /* ── Search ───────────────────────────────────────────────────────── */

    .search {
        width: 100%;
        max-width: 600px;
        display: flex;
        align-items: center;
        gap: 11px;
        height: 48px;
        padding: 0 8px 0 16px;
        border-radius: 13px;
        background: var(--field);
        border: 1px solid var(--field-line);
        box-shadow: var(--field-shadow);
        transition: border-color 0.15s ease, box-shadow 0.15s ease;
        margin-bottom: 34px;
    }
    .search:hover { border-color: rgba(0, 0, 0, 0.10); }
    body.dark .search:hover { border-color: rgba(255, 255, 255, 0.12); }
    .search:focus-within {
        border-color: var(--accent);
        box-shadow: 0 0 0 4px var(--ring), var(--field-shadow);
    }

    .search-icon {
        width: 17px;
        height: 17px;
        flex: none;
        fill: none;
        stroke: var(--text3);
        stroke-width: 1.8;
        stroke-linecap: round;
    }

    .search-input {
        flex: 1 1 auto;
        min-width: 0;
        height: 100%;
        border: none;
        outline: none;
        background: transparent;
        color: var(--text);
        font: inherit;
        font-size: 14.5px;
        letter-spacing: -0.01em;
    }
    .search-input::placeholder { color: var(--text3); }

    .search-go {
        flex: none;
        height: 32px;
        padding: 0 14px;
        border: none;
        border-radius: 9px;
        background: var(--accent);
        color: #fff;
        font: inherit;
        font-size: 12.5px;
        font-weight: 600;
        letter-spacing: -0.01em;
        cursor: pointer;
        opacity: 0;
        transform: scale(0.96);
        transition: opacity 0.15s ease, transform 0.15s ease, background 0.15s ease;
    }
    .search:focus-within .search-go,
    .search.has-text .search-go {
        opacity: 1;
        transform: scale(1);
    }
    .search-go:hover { background: #006ae0; }
    body.dark .search-go:hover { background: #3b9bff; }

    /* ── Favorites ────────────────────────────────────────────────────── */

    .favorites {
        display: flex;
        flex-wrap: wrap;
        justify-content: center;
        gap: 4px 2px;
        max-width: 620px;
        width: 100%;
    }

    .fav {
        width: 74px;
        padding: 9px 4px 8px;
        border-radius: 12px;
        display: flex;
        flex-direction: column;
        align-items: center;
        text-decoration: none;
        color: inherit;
        transition: background 0.14s ease, transform 0.14s ease;
    }
    .fav:hover { background: var(--hover); transform: translateY(-2px); }

    .tile {
        position: relative;
        width: 44px;
        height: 44px;
        border-radius: 12px;
        display: grid;
        place-items: center;
        overflow: hidden;
        background: var(--tile-bg);
        border: 1px solid var(--tile-line);
        box-shadow: var(--tile-shadow);
    }

    /* Brand-coloured letter badge, shown until (or unless) the real
       favicon loads. */
    .tile-letter {
        position: absolute;
        inset: 0;
        display: grid;
        place-items: center;
        color: #fff;
        font-size: 18px;
        font-weight: 600;
        letter-spacing: -0.02em;
    }

    .tile-icon {
        position: relative;
        z-index: 1;
        width: 26px;
        height: 26px;
        object-fit: contain;
        opacity: 0;
        transition: opacity 0.15s ease;
    }

    .tile.has-icon .tile-icon { opacity: 1; }
    .tile.has-icon .tile-letter { display: none; }

    .label {
        margin-top: 7px;
        max-width: 100%;
        font-size: 11px;
        font-weight: 500;
        letter-spacing: -0.015em;
        color: var(--text2);
        white-space: nowrap;
        overflow: hidden;
        text-overflow: ellipsis;
    }
    .fav:hover .label { color: var(--text); }

    /* ── Footer ───────────────────────────────────────────────────────── */

    .footer {
        position: fixed;
        left: 0;
        right: 0;
        bottom: 0;
        height: 54px;
        padding: 0 24px;
        display: flex;
        align-items: center;
        justify-content: space-between;
        font-size: 11.5px;
        color: var(--text3);
        pointer-events: none;
    }

    .brand {
        display: flex;
        align-items: center;
        gap: 6px;
        font-weight: 600;
        letter-spacing: -0.01em;
    }
    .brand svg {
        width: 14px;
        height: 14px;
        fill: none;
        stroke: var(--orange);
        stroke-width: 1.7;
        stroke-linejoin: round;
    }

    .shields-note { display: flex; align-items: center; gap: 7px; }
    .dot {
        width: 6px;
        height: 6px;
        border-radius: 50%;
        background: var(--green);
        box-shadow: 0 0 0 3px rgba(52, 199, 89, 0.16);
    }

    @media (max-width: 560px) {
        .clock { font-size: 46px; }
        .footer { display: none; }
    }
</style>
</head>
<body class='__THEME__'>

    <div class='stage'>
        <div class='date' id='date' style='__CLOCK_STYLE__'></div>
        <div class='clock' id='clock' style='__CLOCK_STYLE__'>--:--</div>

        <form class='search' id='search' action='__SEARCH_ACTION__' method='GET'>
            <svg class='search-icon' viewBox='0 0 24 24'><circle cx='11' cy='11' r='6.5'/><path d='M16 16l4.5 4.5'/></svg>
            <input class='search-input' id='search-input' type='text' name='__SEARCH_PARAM__'
                   placeholder='Search __SEARCH_NAME__ or enter a website' autocomplete='off'
                   autocapitalize='off' spellcheck='false' />
            <button class='search-go' type='submit'>Search</button>
        </form>

        <div class='favorites' style='__TILES_STYLE__'>
__FAVORITES__
        </div>
    </div>

    <div class='footer'>
        <div class='brand'>
            <svg viewBox='0 0 24 24'><path d='M12 3.2 5.2 6v5.3c0 4.35 2.9 7.6 6.8 9.5 3.9-1.9 6.8-5.15 6.8-9.5V6L12 3.2z'/></svg>
            Aura
        </div>
        <div class='shields-note'>
            <span class='dot'></span>__SHIELDS__
        </div>
    </div>

<script>
    function pad(n) { return n < 10 ? '0' + n : '' + n; }

    function tick() {
        var now = new Date();
        document.getElementById('clock').textContent = pad(now.getHours()) + ':' + pad(now.getMinutes());
        document.getElementById('date').textContent = now.toLocaleDateString(undefined, {
            weekday: 'long', month: 'long', day: 'numeric'
        });
    }
    tick();
    setInterval(tick, 1000);

    var form = document.getElementById('search');
    var input = document.getElementById('search-input');

    input.addEventListener('input', function () {
        form.classList.toggle('has-text', input.value.trim().length > 0);
    });

    form.addEventListener('submit', function (e) {
        var value = input.value.trim();
        if (!value) { e.preventDefault(); return; }

        if (value.indexOf('http://') === 0 || value.indexOf('https://') === 0) {
            e.preventDefault();
            window.location.href = value;
            return;
        }

        if (value.indexOf(' ') === -1 && value.indexOf('.') !== -1) {
            e.preventDefault();
            window.location.href = 'https://' + value;
            return;
        }

        input.value = value;
    });
</script>
</body>
</html>";

            var engine = Settings.Current.Engine;

            return html
                .Replace("__THEME__", dark ? "dark" : "light")
                .Replace("__FAVORITES__", RenderFavorites())
                .Replace("__SHIELDS__", shieldsNote)
                .Replace("__SEARCH_ACTION__", Attribute(engine.BaseUrl))
                .Replace("__SEARCH_PARAM__", Attribute(engine.QueryParam))
                .Replace("__SEARCH_NAME__", Escape(engine.ShortName))
                .Replace("__CLOCK_STYLE__", Settings.Current.ShowClock ? "" : "display:none")
                .Replace("__TILES_STYLE__", Settings.Current.ShowTiles ? "" : "display:none");
        }

        /// <summary>
        /// Builds the tile strip from the favourites store, so imported top sites
        /// and anything added later show up here without touching this file.
        /// </summary>
        private static string RenderFavorites()
        {
            var builder = new StringBuilder();

            foreach (var favorite in FavoritesStore.Current.GetFavorites())
            {
                if (favorite == null || string.IsNullOrEmpty(favorite.Url)) continue;

                string color = string.IsNullOrEmpty(favorite.Color) ? "#6e6e73" : favorite.Color;
                string letter = string.IsNullOrEmpty(favorite.Letter)
                    ? FavoritesStore.LetterFor(favorite.Title, favorite.Url)
                    : favorite.Letter;
                string icon = FavoritesStore.IconFor(favorite);

                builder.Append("            <a class='fav' href='").Append(Attribute(favorite.Url)).Append("'>\r\n");
                builder.Append("                <span class='tile'>\r\n");
                builder.Append("                    <span class='tile-letter' style='background:")
                       .Append(Attribute(color)).Append("'>").Append(Escape(letter)).Append("</span>\r\n");

                if (!string.IsNullOrEmpty(icon))
                {
                    builder.Append("                    <img class='tile-icon' alt='' src='")
                           .Append(Attribute(icon))
                           .Append("' onload=\"this.parentNode.classList.add('has-icon')\" />\r\n");
                }

                builder.Append("                </span>\r\n");
                builder.Append("                <span class='label'>")
                       .Append(Escape(favorite.Title)).Append("</span>\r\n");
                builder.Append("            </a>\r\n");
            }

            return builder.ToString();
        }

        private static string Escape(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;

            return text
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;");
        }

        private static string Attribute(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return Escape(text).Replace("'", "&#39;");
        }
    }
}
