using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleBrowser
{
    /// <summary>
    /// The about:settings page. It is rendered inside a tab, reads its values
    /// from Settings and sends every change back with the WebView2 message
    /// channel, so the page never has to know how anything is applied.
    /// </summary>
    public static class SettingsPage
    {
        public const string Address = "about:settings";

        public static string GetHtml()
        {
            string state = Json.Write(BuildState());
            if (state == null) state = "{}";

            // Never let a stored value close the surrounding script tag.
            state = state.Replace("<", "\\u003c");

            return Html
                .Replace("__THEME__", Theme.IsDark ? "dark" : "light")
                .Replace("__ENGINES__", RenderEngines())
                .Replace("__STATE__", state);
        }

        /// <summary>Script the host runs to refresh the live numbers.</summary>
        public static string StatsScript(int rules, int blocked)
        {
            return "togeStats(" + rules + "," + blocked + ");";
        }

        /// <summary>Script the host runs to report the result of a data action.</summary>
        public static string NoticeScript(string message)
        {
            return "togeNotice(" + Json.Write(message ?? string.Empty) + ");";
        }

        private static Dictionary<string, object> BuildState()
        {
            return Json.Obj(
                "theme", Settings.Current.Theme,
                "search", Settings.Current.SearchId,
                "customSearch", Settings.Current.CustomSearchUrl,
                "shields", Settings.Current.Shields,
                "savePasswords", Settings.Current.SavePasswords,
                "autofill", Settings.Current.Autofill,
                "showClock", Settings.Current.ShowClock,
                "showTiles", Settings.Current.ShowTiles,
                "bookmarksBar", Settings.Current.ShowBookmarksBar,
                "zoom", Settings.Current.Zoom,
                "startup", Settings.Current.StartupMode,
                "askDownloadPath", Settings.Current.AskDownloadPath,
                "isDefault", DefaultBrowser.IsDefault(),
                "ruleCount", AdBlocker.RuleCount,
                "blockedCount", AdBlocker.BlockedCount,
                "profilePath", BrowserEnvironment.UserDataFolder,
                "settingsPath", Settings.Current.FilePath);
        }

        private static string RenderEngines()
        {
            var builder = new StringBuilder();
            foreach (var engine in Settings.Engines)
            {
                builder.Append("<option value='").Append(engine.Id).Append("'>")
                       .Append(engine.Name).Append("</option>");
            }

            builder.Append("<option value='custom'>Custom\u2026</option>");
            return builder.ToString();
        }

        private const string Html = @"<!DOCTYPE html>
<html lang='en'>
<head>
<meta charset='utf-8' />
<meta name='viewport' content='width=device-width, initial-scale=1.0' />
<title>Settings</title>
<style>
    * { box-sizing: border-box; margin: 0; padding: 0; }

    body {
        --text: #1d1d1f;
        --text2: #6e6e73;
        --text3: #9a9aa0;
        --page: #ffffff;
        --card: #ffffff;
        --line: rgba(0, 0, 0, 0.08);
        --rail: #f7f7f8;
        --hover: rgba(0, 0, 0, 0.045);
        --seg: rgba(120, 120, 128, 0.12);
        --switch-off: rgba(120, 120, 128, 0.32);
        --accent: #007aff;
        --shield: #f25a29;
        --info: #f5f5f7;
        --shadow: 0 1px 2px rgba(0, 0, 0, 0.04), 0 8px 24px rgba(0, 0, 0, 0.05);

        font-family: -apple-system, 'Segoe UI', system-ui, sans-serif;
        font-size: 13px;
        color: var(--text);
        background: var(--page);
        -webkit-font-smoothing: antialiased;
        scroll-behavior: smooth;
    }

    body.dark {
        --text: #f0f0f2;
        --text2: #a1a1a6;
        --text3: #77777c;
        --page: #18181a;
        --card: #232326;
        --line: rgba(255, 255, 255, 0.09);
        --rail: #1d1d20;
        --hover: rgba(255, 255, 255, 0.06);
        --seg: rgba(120, 120, 128, 0.28);
        --switch-off: rgba(120, 120, 128, 0.42);
        --accent: #0a84ff;
        --info: #1d1d20;
        --shadow: 0 1px 2px rgba(0, 0, 0, 0.3), 0 8px 24px rgba(0, 0, 0, 0.28);
    }

    .shell { display: flex; align-items: flex-start; min-height: 100vh; }

    /* ── Section rail ─────────────────────────────────────────────────── */
    .rail {
        position: sticky;
        top: 0;
        flex: none;
        width: 212px;
        height: 100vh;
        padding: 26px 12px 26px 20px;
        background: var(--rail);
        border-right: 1px solid var(--line);
    }

    .rail .brand {
        font-size: 15px;
        font-weight: 600;
        letter-spacing: -0.01em;
        padding: 0 10px 16px;
    }

    .rail a {
        display: block;
        padding: 7px 10px;
        margin-bottom: 1px;
        border-radius: 7px;
        color: var(--text2);
        text-decoration: none;
        font-size: 12.75px;
    }

    .rail a:hover { background: var(--hover); color: var(--text); }
    .rail a.on { color: var(--text); font-weight: 600; }

    /* ── Content ──────────────────────────────────────────────────────── */
    .content {
        flex: 1;
        max-width: 760px;
        padding: 30px 44px 90px;
    }

    h1 {
        font-size: 22px;
        font-weight: 600;
        letter-spacing: -0.02em;
        margin-bottom: 4px;
    }

    .lede { color: var(--text2); margin-bottom: 26px; }

    h2 {
        font-size: 11.5px;
        font-weight: 600;
        text-transform: uppercase;
        letter-spacing: 0.07em;
        color: var(--text3);
        margin: 30px 0 9px 2px;
    }

    .card {
        background: var(--card);
        border: 1px solid var(--line);
        border-radius: 12px;
        box-shadow: var(--shadow);
        overflow: hidden;
    }

    .row {
        display: flex;
        align-items: center;
        gap: 18px;
        padding: 14px 18px;
        border-top: 1px solid var(--line);
    }

    .row:first-child { border-top: none; }
    .row .label { flex: 1; min-width: 0; }
    .row .label .name { font-weight: 500; }
    .row .label .hint { color: var(--text2); margin-top: 2px; font-size: 12px; }
    .row .value { color: var(--text2); font-size: 12px; text-align: right; }
    .row .value.mono {
        font-family: 'Cascadia Mono', Consolas, monospace;
        font-size: 11.5px;
        word-break: break-all;
        max-width: 320px;
    }

    .row.action { padding: 11px 14px; }

    button.act {
        font: inherit;
        font-weight: 500;
        color: var(--text);
        background: var(--seg);
        border: none;
        border-radius: 8px;
        padding: 7px 14px;
        cursor: pointer;
    }

    button.act:hover { background: var(--hover); }
    button.act.primary { background: var(--accent); color: #fff; }
    button.act.danger { color: #d70015; }
    body.dark button.act.danger { color: #ff6961; }

    /* ── Controls ─────────────────────────────────────────────────────── */
    .switch { position: relative; flex: none; width: 44px; height: 26px; }
    .switch input { position: absolute; opacity: 0; width: 0; height: 0; }
    .switch .track {
        position: absolute;
        inset: 0;
        background: var(--switch-off);
        border-radius: 999px;
        cursor: pointer;
        transition: background 0.18s ease;
    }
    .switch .track::after {
        content: '';
        position: absolute;
        top: 3px;
        left: 3px;
        width: 20px;
        height: 20px;
        border-radius: 50%;
        background: #fff;
        box-shadow: 0 1px 3px rgba(0, 0, 0, 0.28);
        transition: transform 0.18s ease;
    }
    .switch input:checked + .track { background: var(--accent); }
    .switch input:checked + .track::after { transform: translateX(18px); }

    .seg { display: flex; flex: none; background: var(--seg); border-radius: 9px; padding: 2px; }
    .seg button {
        font: inherit;
        font-weight: 500;
        color: var(--text2);
        background: transparent;
        border: none;
        border-radius: 7px;
        padding: 5px 13px;
        cursor: pointer;
    }
    .seg button.on { background: var(--card); color: var(--text); box-shadow: 0 1px 2px rgba(0, 0, 0, 0.14); }

    select {
        font: inherit;
        color: var(--text);
        background: var(--seg);
        border: none;
        border-radius: 8px;
        padding: 6px 10px;
        min-width: 168px;
        cursor: pointer;
    }

    .textbox {
        font: inherit;
        color: var(--text);
        background: var(--seg);
        border: 1px solid transparent;
        border-radius: 8px;
        padding: 7px 10px;
        width: 320px;
        max-width: 46vw;
    }
    .textbox:focus {
        outline: none;
        border-color: var(--accent);
        background: var(--page);
    }

    input[type='range'] {
        flex: none;
        width: 200px;
        accent-color: var(--accent);
        cursor: pointer;
    }

    .zoom-value { flex: none; width: 46px; text-align: right; color: var(--text2); }

    /* ── Chrome bits ──────────────────────────────────────────────────── */
    .saved {
        position: fixed;
        top: 16px;
        right: 18px;
        padding: 6px 12px;
        border-radius: 999px;
        background: var(--accent);
        color: #fff;
        font-size: 12px;
        font-weight: 500;
        opacity: 0;
        transform: translateY(-6px);
        transition: opacity 0.2s ease, transform 0.2s ease;
        pointer-events: none;
    }

    .saved.show { opacity: 1; transform: none; }

    .notice {
        display: none;
        margin-top: 12px;
        padding: 11px 14px;
        border-radius: 10px;
        background: var(--info);
        color: var(--text2);
    }

    .notice.show { display: block; }

    .shield-dot {
        flex: none;
        width: 7px;
        height: 7px;
        border-radius: 50%;
        background: var(--shield);
    }

    @media (max-width: 720px) {
        .rail { display: none; }
        .content { padding: 24px 20px 70px; }
    }
</style>
</head>
<body class='__THEME__'>

<div class='shell'>
    <nav class='rail'>
        <div class='brand'>Settings</div>
        <a href='#appearance'>Appearance</a>
        <a href='#search'>Search</a>
        <a href='#shields'>Shields</a>
        <a href='#passwords'>Passwords</a>
        <a href='#start'>Start page</a>
        <a href='#browsing'>Browsing</a>
        <a href='#permissions'>Site permissions</a>
        <a href='#data'>Data &amp; privacy</a>
        <a href='#about'>About</a>
    </nav>

    <main class='content'>
        <h1>Settings</h1>
        <p class='lede'>Changes are saved as you make them and apply straight away.</p>

        <h2 id='appearance'>Appearance</h2>
        <div class='card'>
            <div class='row'>
                <div class='label'>
                    <div class='name'>Theme</div>
                    <div class='hint'>System follows the Windows light or dark setting.</div>
                </div>
                <div class='seg' id='theme'>
                    <button data-value='light'>Light</button>
                    <button data-value='dark'>Dark</button>
                    <button data-value='system'>System</button>
                </div>
            </div>
            <div class='row'>
                <div class='label'>
                    <div class='name'>Show the clock and date</div>
                    <div class='hint'>On the new tab page.</div>
                </div>
                <label class='switch'>
                    <input type='checkbox' data-key='showClock' />
                    <span class='track'></span>
                </label>
            </div>
            <div class='row'>
                <div class='label'>
                    <div class='name'>Show shortcut tiles</div>
                    <div class='hint'>Sites you visit often, plus anything imported.</div>
                </div>
                <label class='switch'>
                    <input type='checkbox' data-key='showTiles' />
                    <span class='track'></span>
                </label>
            </div>
            <div class='row'>
                <div class='label'>
                    <div class='name'>Show the bookmarks bar</div>
                    <div class='hint'>The row of saved pages under the toolbar. Ctrl+Shift+B toggles it too.</div>
                </div>
                <label class='switch'>
                    <input type='checkbox' data-key='bookmarksBar' />
                    <span class='track'></span>
                </label>
            </div>
        </div>

        <h2 id='search'>Search</h2>
        <div class='card'>
            <div class='row'>
                <div class='label'>
                    <div class='name'>Search engine</div>
                    <div class='hint'>Used by the address bar and the new tab search box.</div>
                </div>
                <select data-key='search'>__ENGINES__</select>
            </div>
            <div class='row'>
                <div class='label'>
                    <div class='name'>Custom search address</div>
                    <div class='hint'>Put {query} where the words go, then pick Custom above.</div>
                </div>
                <input class='textbox' type='url' id='custom-search' spellcheck='false'
                       placeholder='https://example.com/search?q={query}' />
            </div>
        </div>

        <h2 id='shields'>Shields</h2>
        <div class='card'>
            <div class='row'>
                <div class='label'>
                    <div class='name'>Block ads and trackers</div>
                    <div class='hint'>Toge cancels matching requests before they leave the browser.</div>
                </div>
                <label class='switch'>
                    <input type='checkbox' data-key='shields' />
                    <span class='track'></span>
                </label>
            </div>
            <div class='row'>
                <div class='label'>
                    <div class='name'>Filter rules loaded</div>
                    <div class='hint'>EasyList and EasyPrivacy, refreshed every few days.</div>
                </div>
                <div class='value' id='rule-count'>&ndash;</div>
            </div>
            <div class='row'>
                <div class='label'>
                    <div class='name'>Blocked this session</div>
                    <div class='hint'>Shown on the Shields button in the toolbar too.</div>
                </div>
                <div class='value' id='blocked-count'>&ndash;</div>
            </div>
        </div>

        <h2 id='passwords'>Passwords</h2>
        <div class='card'>
            <div class='row'>
                <div class='label'>
                    <div class='name'>Offer to save passwords</div>
                    <div class='hint'>Saved logins stay in Toge's own profile on this PC.</div>
                </div>
                <label class='switch'>
                    <input type='checkbox' data-key='savePasswords' />
                    <span class='track'></span>
                </label>
            </div>
            <div class='row'>
                <div class='label'>
                    <div class='name'>Fill in forms and addresses</div>
                    <div class='hint'>Autofill for names, addresses and payment fields.</div>
                </div>
                <label class='switch'>
                    <input type='checkbox' data-key='autofill' />
                    <span class='track'></span>
                </label>
            </div>
        </div>

        <h2 id='start'>Start page</h2>
        <div class='card'>
            <div class='row'>
                <div class='label'>
                    <div class='name'>On startup</div>
                    <div class='hint'>Toge can reopen the tabs from last time, and always offers them back after a crash.</div>
                </div>
                <div class='seg' id='startup'>
                    <button data-value='home'>Start page</button>
                    <button data-value='continue'>Last session</button>
                </div>
            </div>
            <div class='row'>
                <div class='label'>
                    <div class='name'>Search from the new tab page</div>
                    <div class='hint'>The new tab search box uses the engine chosen above.</div>
                </div>
                <div class='value' id='engine-name'>&ndash;</div>
            </div>
        </div>

        <h2 id='browsing'>Browsing</h2>
        <div class='card'>
            <div class='row'>
                <div class='label'>
                    <div class='name'>Page zoom</div>
                    <div class='hint'>Applies to every tab and to new windows. A single site can have its own zoom from the AA menu.</div>
                </div>
                <input type='range' min='50' max='200' step='10' data-key='zoom' />
                <div class='zoom-value' id='zoom-value'>100%</div>
            </div>
            <div class='row'>
                <div class='label'>
                    <div class='name'>Ask where to save each file</div>
                    <div class='hint'>Off means downloads go straight to your Downloads folder.</div>
                </div>
                <label class='switch'>
                    <input type='checkbox' data-key='askDownloadPath' />
                    <span class='track'></span>
                </label>
            </div>
            <div class='row action'>
                <div class='label'>
                    <div class='name'>Default browser</div>
                    <div class='hint' id='default-hint'>&ndash;</div>
                </div>
                <button class='act primary' data-open='defaultbrowser' id='default-btn'>Set as Default</button>
            </div>
        </div>

        <h2 id='permissions'>Site permissions</h2>
        <div class='card'>
            <div class='row'>
                <div class='label'>
                    <div class='name'>Sites you have answered for</div>
                    <div class='hint'>Toge asks before a site uses your camera, microphone, location and so on. Change an answer here, or put it back to asking.</div>
                </div>
                <button class='act' data-perm-clear='1'>Remove All</button>
            </div>
            <div id='perm-list'></div>
            <div class='row' id='perm-empty'>
                <div class='label'>
                    <div class='name'>Nothing answered yet</div>
                    <div class='hint'>Answers you give to a site's requests will be listed here.</div>
                </div>
            </div>
        </div>

        <h2 id='data'>Data &amp; privacy</h2>
        <div class='card'>
            <div class='row action'>
                <div class='label'><div class='name'>Bring data in from another browser</div></div>
                <button class='act primary' data-open='import'>Import</button>
            </div>
            <div class='row action'>
                <div class='label'><div class='name'>Bookmarks</div></div>
                <button class='act' data-open='bookmarks'>Manage</button>
            </div>
            <div class='row action'>
                <div class='label'><div class='name'>History</div></div>
                <button class='act' data-open='history'>View</button>
            </div>
            <div class='row action'>
                <div class='label'><div class='name'>Clear browsing history</div></div>
                <button class='act danger' data-clear='history'>Clear</button>
            </div>
            <div class='row action'>
                <div class='label'><div class='name'>Clear search history</div></div>
                <button class='act danger' data-clear='searches'>Clear</button>
            </div>
            <div class='row action'>
                <div class='label'>
                    <div class='name'>Clear cookies and site data</div>
                    <div class='hint'>Signs you out of the sites you are signed in to.</div>
                </div>
                <button class='act danger' data-clear='cookies'>Clear</button>
            </div>
            <div class='row action'>
                <div class='label'><div class='name'>Clear the cache</div></div>
                <button class='act danger' data-clear='cache'>Clear</button>
            </div>
            <div class='notice' id='notice'></div>
        </div>

        <h2 id='about'>About</h2>
        <div class='card'>
            <div class='row'>
                <div class='label'><div class='name'>Toge</div></div>
                <div class='value'>Version 2.0</div>
            </div>
            <div class='row'>
                <div class='label'><div class='name'>Profile folder</div></div>
                <div class='value mono' id='profile-path'>&ndash;</div>
            </div>
            <div class='row'>
                <div class='label'><div class='name'>Settings file</div></div>
                <div class='value mono' id='settings-path'>&ndash;</div>
            </div>
            <div class='row action'>
                <div class='label'>
                    <div class='name'>Reset all settings</div>
                    <div class='hint'>Puts every option on this page back to its default.</div>
                </div>
                <button class='act danger' data-reset='1'>Reset</button>
            </div>
        </div>
    </main>
</div>

<div class='saved' id='saved'>Saved</div>

<script>
    var S = __STATE__;

    function post(message) {
        try {
            if (window.chrome && window.chrome.webview) {
                window.chrome.webview.postMessage(message);
            }
        } catch (e) { }
    }

    function flash() {
        var saved = document.getElementById('saved');
        saved.classList.add('show');
        clearTimeout(flash.timer);
        flash.timer = setTimeout(function () { saved.classList.remove('show'); }, 1100);
    }

    function togeNotice(text) {
        var notice = document.getElementById('notice');
        notice.textContent = text;
        notice.classList.add('show');
        clearTimeout(togeNotice.timer);
        togeNotice.timer = setTimeout(function () { notice.classList.remove('show'); }, 5000);
    }

    function togeStats(rules, blocked) {
        document.getElementById('rule-count').textContent = rules.toLocaleString();
        document.getElementById('blocked-count').textContent = blocked.toLocaleString();
    }

    function engineNameFrom(id) {
        var select = document.querySelector('select[data-key=""search""]');
        for (var i = 0; i < select.options.length; i++) {
            if (select.options[i].value === id) return select.options[i].text;
        }
        return id;
    }

    // ── Paint the stored values ────────────────────────────────────────────
    var boxes = document.querySelectorAll('input[type=checkbox][data-key]');
    for (var i = 0; i < boxes.length; i++) {
        boxes[i].checked = S[boxes[i].getAttribute('data-key')] === true;
    }

    var select = document.querySelector('select[data-key=""search""]');
    select.value = S.search;

    var zoom = document.querySelector('input[type=range][data-key=""zoom""]');
    zoom.value = S.zoom;
    document.getElementById('zoom-value').textContent = S.zoom + '%';
    document.getElementById('engine-name').textContent = engineNameFrom(S.search);

    var customSearch = document.getElementById('custom-search');
    if (customSearch) { customSearch.value = S.customSearch || ''; }
    document.getElementById('profile-path').textContent = S.profilePath;
    document.getElementById('settings-path').textContent = S.settingsPath;

    var themeSeg = document.getElementById('theme');
    var themeButtons = themeSeg.querySelectorAll('button');
    for (var b = 0; b < themeButtons.length; b++) {
        if (themeButtons[b].getAttribute('data-value') === S.theme) {
            themeButtons[b].classList.add('on');
        }
    }

    var startupSeg = document.getElementById('startup');
    var startupButtons = startupSeg.querySelectorAll('button');
    for (var u = 0; u < startupButtons.length; u++) {
        if (startupButtons[u].getAttribute('data-value') === S.startup) {
            startupButtons[u].classList.add('on');
        }
    }

    if (S.isDefault) {
        document.getElementById('default-hint').textContent = 'Toge is your default browser.';
        var defaultBtn = document.getElementById('default-btn');
        defaultBtn.textContent = 'Default';
        defaultBtn.classList.remove('primary');
        defaultBtn.disabled = true;
    } else {
        document.getElementById('default-hint').textContent =
            'Opens links from other apps in Toge. Windows asks you to confirm.';
    }

    // ── The list of site permissions ───────────────────────────────────────
    function togePermissions(rows) {
        var list = document.getElementById('perm-list');
        var empty = document.getElementById('perm-empty');
        list.innerHTML = '';

        if (!rows || !rows.length) {
            empty.style.display = '';
            return;
        }

        empty.style.display = 'none';

        for (var i = 0; i < rows.length; i++) {
            (function (row) {
                var line = document.createElement('div');
                line.className = 'row';

                var label = document.createElement('div');
                label.className = 'label';

                var name = document.createElement('div');
                name.className = 'name';
                name.textContent = row.kindLabel || row.kind;

                var hint = document.createElement('div');
                hint.className = 'hint';
                hint.textContent = row.origin;

                label.appendChild(name);
                label.appendChild(hint);

                var seg = document.createElement('div');
                seg.className = 'seg';

                var options = [['ask', 'Ask'], ['allow', 'Allow'], ['block', 'Block']];
                for (var o = 0; o < options.length; o++) {
                    (function (id, text) {
                        var button = document.createElement('button');
                        button.textContent = text;
                        if (id === row.state) { button.classList.add('on'); }

                        button.addEventListener('click', function () {
                            var buttons = seg.querySelectorAll('button');
                            for (var b = 0; b < buttons.length; b++) { buttons[b].classList.remove('on'); }
                            button.classList.add('on');

                            post({
                                action: 'perm_set',
                                origin: row.origin,
                                kind: row.kind,
                                state: id
                            });
                            flash();
                        });

                        seg.appendChild(button);
                    })(options[o][0], options[o][1]);
                }

                line.appendChild(label);
                line.appendChild(seg);
                list.appendChild(line);
            })(rows[i]);
        }
    }

    // ── React to changes ───────────────────────────────────────────────────
    for (var c = 0; c < boxes.length; c++) {
        boxes[c].addEventListener('change', function () {
            var key = this.getAttribute('data-key');
            post({ action: 'set', key: key, value: this.checked });
            flash();
        });
    }

    themeSeg.addEventListener('click', function (e) {
        var button = e.target.closest('button');
        if (!button) return;

        for (var t = 0; t < themeButtons.length; t++) {
            themeButtons[t].classList.remove('on');
        }
        button.classList.add('on');

        post({ action: 'set', key: 'theme', value: button.getAttribute('data-value') });
        flash();

        // The page follows the theme at once, without a round trip.
        var mode = button.getAttribute('data-value');
        var dark = mode === 'dark' ||
            (mode === 'system' && window.matchMedia('(prefers-color-scheme: dark)').matches);
        document.body.className = dark ? 'dark' : 'light';
    });

    select.addEventListener('change', function () {
        post({ action: 'set', key: 'search', value: this.value });
        document.getElementById('engine-name').textContent = engineNameFrom(this.value);
        flash();
    });

    if (customSearch) {
        customSearch.addEventListener('change', function () {
            post({ action: 'set', key: 'customsearch', value: this.value });
            flash();
        });
    }

    startupSeg.addEventListener('click', function (e) {
        var button = e.target.closest('button');
        if (!button) return;

        for (var t = 0; t < startupButtons.length; t++) {
            startupButtons[t].classList.remove('on');
        }
        button.classList.add('on');

        post({ action: 'set', key: 'startup', value: button.getAttribute('data-value') });
        flash();
    });

    var permClear = document.querySelector('button[data-perm-clear]');
    if (permClear) {
        permClear.addEventListener('click', function () {
            if (confirm('Put every site permission back to asking?')) {
                post({ action: 'perm_clear' });
            }
        });
    }

    zoom.addEventListener('input', function () {
        document.getElementById('zoom-value').textContent = this.value + '%';
    });

    zoom.addEventListener('change', function () {
        post({ action: 'set', key: 'zoom', value: parseInt(this.value, 10) });
        flash();
    });

    var openers = document.querySelectorAll('button[data-open]');
    for (var o = 0; o < openers.length; o++) {
        openers[o].addEventListener('click', function () {
            post({ action: 'open', what: this.getAttribute('data-open') });
        });
    }

    var cleaners = document.querySelectorAll('button[data-clear]');
    for (var k = 0; k < cleaners.length; k++) {
        cleaners[k].addEventListener('click', function () {
            var what = this.getAttribute('data-clear');
            var question = what === 'cookies'
                ? 'Clear cookies and site data? You will be signed out of the sites you are signed in to.'
                : 'Clear this? It cannot be undone.';

            if (confirm(question)) {
                post({ action: 'clear', what: what });
            }
        });
    }

    var reset = document.querySelector('button[data-reset]');
    reset.addEventListener('click', function () {
        if (confirm('Reset every setting on this page to its default?')) {
            post({ action: 'reset' });
        }
    });

    // ── Keep the rail in step with what is on screen ───────────────────────
    var links = document.querySelectorAll('.rail a');
    var sections = [];
    for (var s = 0; s < links.length; s++) {
        var id = links[s].getAttribute('href').substring(1);
        var section = document.getElementById(id);
        if (section) sections.push({ link: links[s], section: section });
    }

    function spy() {
        var current = sections[0];
        for (var s = 0; s < sections.length; s++) {
            if (sections[s].section.getBoundingClientRect().top <= 90) current = sections[s];
        }
        for (var t = 0; t < sections.length; t++) {
            sections[t].link.classList.toggle('on', sections[t] === current);
        }
    }

    window.addEventListener('scroll', spy);
    spy();

    post({ action: 'stats' });
    post({ action: 'permissions' });
</script>
</body>
</html>";
            }
}
