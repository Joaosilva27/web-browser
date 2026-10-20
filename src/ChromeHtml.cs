using System;

namespace SimpleBrowser
{
    public static class ChromeHtml
    {
        /// <summary>Total height of the browser chrome, in device-independent pixels.</summary>
        public const int ChromeHeight = 84;

        /// <summary>Height of the tab strip row.</summary>
        public const int TabStripHeight = 40;

        /// <summary>Height of the toolbar row.</summary>
        public const int ToolbarHeight = 44;

        /// <summary>Height of the bookmarks bar, which sits under the toolbar.</summary>
        public const int BookmarksBarHeight = 30;

        /// <summary>Total chrome height with the bookmarks bar showing.</summary>
        public const int ChromeHeightWithBookmarks = ChromeHeight + BookmarksBarHeight;

        public static string GetHtml(bool dark)
        {
            string html = @"<!DOCTYPE html>
<html lang='en'>
<head>
<meta charset='utf-8' />
<meta name='viewport' content='width=device-width, initial-scale=1.0' />
<title>Aura</title>
<style>
    * { box-sizing: border-box; margin: 0; padding: 0; }

    html, body { height: 100%; overflow: hidden; }

    body {
        font-family: 'SF Pro Text', 'SF Pro Display', -apple-system, BlinkMacSystemFont,
                     'Segoe UI Variable Text', 'Segoe UI', system-ui, sans-serif;
        font-size: 13px;
        color: var(--text);
        background: var(--chrome);
        display: flex;
        flex-direction: column;
        user-select: none;
        cursor: default;
        -webkit-font-smoothing: antialiased;
        -moz-osx-font-smoothing: grayscale;
        position: relative;
        box-shadow: inset 0 -1px 0 var(--hairline);
    }

    body {
        --chrome: linear-gradient(180deg, #fbfbfc 0%, #e9e9ec 100%);
        --text: #1d1d1f;
        --text2: #5f5f65;
        --text3: #96969c;
        --tab-active: #ffffff;
        --tab-hover: rgba(0, 0, 0, 0.055);
        --hairline: rgba(0, 0, 0, 0.12);
        --field: rgba(0, 0, 0, 0.065);
        --field-hover: rgba(0, 0, 0, 0.09);
        --field-active: #ffffff;
        --field-border: rgba(0, 0, 0, 0.05);
        --ring: rgba(0, 122, 255, 0.28);
        --accent: #007aff;
        --orange: #f25a29;
        --orange-wash: rgba(242, 90, 41, 0.10);
        --orange-line: rgba(242, 90, 41, 0.22);
        --orange-wash-hover: rgba(242, 90, 41, 0.17);
        --btn-hover: rgba(0, 0, 0, 0.065);
        --tab-shadow: 0 1px 2px rgba(0, 0, 0, 0.13), 0 0 0 0.5px rgba(0, 0, 0, 0.05);
        --tile-shadow: 0 1px 2px rgba(0, 0, 0, 0.10);
        --top-gloss: rgba(255, 255, 255, 0.72);
    }

    body.dark {
        --chrome: linear-gradient(180deg, #363639 0%, #29292c 100%);
        --text: #f5f5f7;
        --text2: #a1a1a6;
        --text3: #7c7c82;
        --tab-active: #454548;
        --tab-hover: rgba(255, 255, 255, 0.07);
        --hairline: rgba(255, 255, 255, 0.09);
        --field: rgba(255, 255, 255, 0.075);
        --field-hover: rgba(255, 255, 255, 0.11);
        --field-active: #3a3a3d;
        --field-border: rgba(255, 255, 255, 0.07);
        --ring: rgba(10, 132, 255, 0.42);
        --accent: #0a84ff;
        --orange: #ff6b3d;
        --orange-wash: rgba(255, 107, 61, 0.13);
        --orange-line: rgba(255, 107, 61, 0.25);
        --orange-wash-hover: rgba(255, 107, 61, 0.20);
        --btn-hover: rgba(255, 255, 255, 0.10);
        --tab-shadow: 0 1px 3px rgba(0, 0, 0, 0.38), 0 0 0 0.5px rgba(0, 0, 0, 0.35);
        --tile-shadow: 0 1px 2px rgba(0, 0, 0, 0.30);
        --top-gloss: rgba(255, 255, 255, 0.06);
    }

    body::before {
        content: '';
        position: absolute;
        top: 0;
        left: 0;
        right: 0;
        height: 1px;
        background: var(--top-gloss);
        pointer-events: none;
        z-index: 6;
    }

    /* ── Tab strip ────────────────────────────────────────────────────── */

    .tabstrip {
        height: 40px;
        flex: 0 0 40px;
        display: flex;
        align-items: center;
        gap: 9px;
        padding: 0 0 0 13px;
        -webkit-app-region: drag;
    }

    /* ── Window buttons (top right, Windows order) ────────────────────── */

    .wctl {
        display: flex;
        align-items: stretch;
        flex: none;
        align-self: stretch;
        margin-left: auto;
        -webkit-app-region: no-drag;
    }

    .wbtn {
        width: 46px;
        display: grid;
        place-items: center;
        border-radius: 0;
        transition: background 0.1s ease, color 0.1s ease;
    }
    .wbtn svg {
        width: 10px;
        height: 10px;
        fill: none;
        stroke: currentColor;
        stroke-width: 1.4;
        stroke-linecap: round;
        stroke-linejoin: round;
    }

    /* The traffic-light colours, now carried by the glyphs themselves. */
    .wbtn.min { color: #febc2e; }
    .wbtn.max { color: #28c840; }
    .wbtn.close { color: #ff5f57; }

    .wbtn.min:hover, .wbtn.max:hover, .wbtn.close:hover { color: #ffffff; }
    .wbtn.min:hover { background: #febc2e; }
    .wbtn.max:hover { background: #28c840; }
    .wbtn.close:hover { background: #ff5f57; }

    .wbtn.min:active { background: #f0ad1d; }
    .wbtn.max:active { background: #1fb236; }
    .wbtn.close:active { background: #e8504a; }

    .tabs {
        /* Shrink-wrapped: the New Tab button sits right after the last tab.
           When the tabs no longer fit, this shrinks (down to min-width: 0)
           and scrolls, which keeps the button pinned on screen. */
        flex: 0 1 auto;
        min-width: 0;
        height: 100%;
        display: flex;
        align-items: center;
        gap: 2px;
        overflow-x: auto;
        overflow-y: hidden;
        scrollbar-width: none;
        -webkit-app-region: drag;
    }
    .tabs::-webkit-scrollbar { display: none; }

    .tab {
        display: flex;
        align-items: center;
        gap: 7px;
        height: 30px;
        padding: 0 7px 0 9px;
        border-radius: 7px;
        flex: 0 1 auto;
        width: 232px;
        min-width: 58px;
        max-width: 232px;
        color: var(--text2);
        font-size: 12.5px;
        font-weight: 500;
        letter-spacing: -0.012em;
        position: relative;
        overflow: hidden;
        white-space: nowrap;
        transition: background 0.13s ease, color 0.13s ease;
        -webkit-app-region: no-drag;
    }
    .tab:hover { background: var(--tab-hover); color: var(--text); }
    .tab.active {
        background: var(--tab-active);
        color: var(--text);
        box-shadow: var(--tab-shadow), inset 0 1px 0 rgba(255, 255, 255, 0.65);
    }
    body.dark .tab.active {
        box-shadow: var(--tab-shadow), inset 0 1px 0 rgba(255, 255, 255, 0.06);
    }

    .tab-fav {
        width: 15px;
        height: 15px;
        border-radius: 4px;
        flex: none;
        display: grid;
        place-items: center;
        font-size: 8.5px;
        font-weight: 700;
        letter-spacing: 0;
        color: #fff;
        box-shadow: var(--tile-shadow);
    }

    .tab-fav img {
        width: 100%;
        height: 100%;
        object-fit: contain;
        display: block;
        border-radius: 3px;
    }

    .tab-fav.plain {
        background: transparent;
        box-shadow: none;
    }

    .tab-fav.home {
        background: transparent;
        box-shadow: none;
    }

    .tab-fav.home svg {
        width: 13px;
        height: 13px;
        fill: none;
        stroke: var(--text3);
        stroke-width: 1.6;
    }

    .tab-title {
        flex: 1 1 auto;
        min-width: 0;
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
    }

    .tab-x {
        width: 18px;
        height: 18px;
        border-radius: 50%;
        flex: none;
        display: grid;
        place-items: center;
        opacity: 0;
        transition: opacity 0.12s ease, background 0.12s ease;
    }
    .tab:hover .tab-x, .tab.active .tab-x { opacity: 1; }
    .tab-x:hover { background: var(--btn-hover); }
    .tab-x svg {
        width: 13px;
        height: 13px;
        fill: none;
        stroke: var(--text2);
        stroke-width: 1.6;
        stroke-linecap: round;
    }
    .tab-x:hover svg { stroke: var(--text); }

    .newtab {
        width: 28px;
        height: 28px;
        flex: none;
        margin: 0 4px;
        border-radius: 7px;
        display: grid;
        place-items: center;
        color: var(--text2);
        transition: background 0.12s ease, color 0.12s ease;
        -webkit-app-region: no-drag;
    }
    .newtab:hover { background: var(--btn-hover); color: var(--text); }
    .newtab svg {
        width: 15px;
        height: 15px;
        fill: none;
        stroke: currentColor;
        stroke-width: 1.7;
        stroke-linecap: round;
    }

    /* A pill that marks a private window, so the two are never confused. */
    .priv {
        flex: none;
        display: none;
        align-items: center;
        gap: 5px;
        height: 22px;
        margin: 0 4px 0 0;
        padding: 0 9px 0 8px;
        border-radius: 999px;
        background: rgba(255, 255, 255, 0.6);
        border: 1px solid var(--hairline);
        color: var(--text2);
        font-size: 11px;
        font-weight: 600;
        letter-spacing: 0.005em;
        -webkit-app-region: no-drag;
    }
    body.dark .priv { background: rgba(255, 255, 255, 0.09); }
    .priv.on { display: flex; }
    .priv svg {
        width: 13px;
        height: 13px;
        fill: none;
        stroke: currentColor;
        stroke-width: 1.6;
        stroke-linecap: round;
        stroke-linejoin: round;
    }

    /* ── Toolbar ──────────────────────────────────────────────────────── */

    .toolbar {
        height: 44px;
        flex: 0 0 44px;
        display: flex;
        align-items: center;
        gap: 8px;
        padding: 0 10px 0 8px;
        -webkit-app-region: drag;
    }

    .nav, .fieldwrap, .actions { -webkit-app-region: no-drag; }

    .nav { display: flex; align-items: center; gap: 1px; flex: none; }

    button { font: inherit; color: inherit; background: none; border: none; }

    .iconbtn {
        width: 28px;
        height: 28px;
        border-radius: 6px;
        display: grid;
        place-items: center;
        color: var(--text2);
        transition: background 0.12s ease, color 0.12s ease;
    }
    .iconbtn:hover:not(:disabled) { background: var(--btn-hover); color: var(--text); }
    .iconbtn:disabled { opacity: 0.28; }
    .iconbtn svg {
        width: 17px;
        height: 17px;
        fill: none;
        stroke: currentColor;
        stroke-width: 1.75;
        stroke-linecap: round;
        stroke-linejoin: round;
    }
    .iconbtn svg.dots { fill: currentColor; stroke: none; }

    /* ── Address field ────────────────────────────────────────────────── */

    .fieldwrap {
        flex: 1 1 auto;
        min-width: 0;
        display: flex;
        justify-content: center;
        padding: 0 2px;
    }

    .field {
        display: flex;
        align-items: center;
        gap: 3px;
        width: 100%;
        max-width: 620px;
        height: 30px;
        padding: 0 4px 0 3px;
        border-radius: 8px;
        background: var(--field);
        border: 1px solid transparent;
        transition: background 0.15s ease, box-shadow 0.15s ease, border-color 0.15s ease;
    }
    .field:hover { background: var(--field-hover); }
    .field.focused {
        background: var(--field-active);
        border-color: var(--field-border);
        box-shadow: 0 0 0 3px var(--ring), 0 1px 2px rgba(0, 0, 0, 0.07);
    }

    .aa {
        width: 26px;
        height: 24px;
        flex: none;
        border-radius: 5px;
        display: flex;
        align-items: baseline;
        justify-content: center;
        gap: 0.5px;
        font-weight: 600;
        color: var(--text2);
        transition: background 0.12s ease, color 0.12s ease, opacity 0.12s ease;
    }
    .aa .a1 { font-size: 10px; }
    .aa .a2 { font-size: 13.5px; }
    .aa:hover { background: var(--btn-hover); color: var(--text); }
    .field.focused .aa { opacity: 0; pointer-events: none; }
    .field.nopage .aa { display: none; }

    .url-display {
        flex: 1 1 auto;
        min-width: 0;
        display: flex;
        align-items: center;
        justify-content: center;
        overflow: hidden;
        white-space: nowrap;
        font-size: 12.8px;
        letter-spacing: -0.008em;
    }
    .u-host { overflow: hidden; text-overflow: ellipsis; color: var(--text); }
    .u-rest { overflow: hidden; text-overflow: ellipsis; color: var(--text3); }
    .u-placeholder { color: var(--text3); }

    .url {
        display: none;
        flex: 1 1 auto;
        min-width: 0;
        height: 100%;
        border: none;
        outline: none;
        background: transparent;
        color: var(--text);
        font: inherit;
        font-size: 12.8px;
        letter-spacing: -0.008em;
        padding: 0 2px;
        user-select: text;
        cursor: text;
    }
    .url::placeholder { color: var(--text3); }
    .field.focused .url { display: block; }
    .field.focused .url-display { display: none; }

    .star {
        width: 22px;
        height: 22px;
        flex: none;
        border-radius: 5px;
        display: grid;
        place-items: center;
        color: var(--text3);
        transition: background 0.12s ease, color 0.12s ease;
    }
    .star:hover { background: var(--btn-hover); color: var(--text); }
    .star.on { color: var(--orange); }
    .star svg {
        width: 13px;
        height: 13px;
        fill: none;
        stroke: currentColor;
        stroke-width: 1.5;
        stroke-linejoin: round;
    }
    .star.on svg { fill: currentColor; }

    .reload {
        width: 22px;
        height: 22px;
        flex: none;
        border-radius: 5px;
        display: grid;
        place-items: center;
        color: var(--text3);
        transition: background 0.12s ease, color 0.12s ease;
    }
    .reload:hover { background: var(--btn-hover); color: var(--text); }
    .reload svg {
        width: 14px;
        height: 14px;
        fill: none;
        stroke: currentColor;
        stroke-width: 1.7;
        stroke-linecap: round;
        stroke-linejoin: round;
    }
    .reload .ico-stop { display: none; }
    .field.loading .reload .ico-reload { display: none; }
    .field.loading .reload .ico-stop { display: block; }

    /* ── Right side ───────────────────────────────────────────────────── */

    .actions { display: flex; align-items: center; gap: 3px; flex: none; }

    /* The download button only appears once there is something to show. */
    .dl {
        position: relative;
        display: none;
    }
    .dl.on { display: grid; }
    .dl .dl-count {
        position: absolute;
        top: -2px;
        right: -2px;
        min-width: 15px;
        height: 15px;
        padding: 0 4px;
        border-radius: 8px;
        background: var(--orange);
        color: #fff;
        font-size: 9.5px;
        font-weight: 700;
        line-height: 15px;
        text-align: center;
        font-variant-numeric: tabular-nums;
    }
    .dl .dl-count.none { display: none; }

    .shields {
        height: 26px;
        display: flex;
        align-items: center;
        gap: 5px;
        padding: 0 9px 0 7px;
        margin-right: 3px;
        border-radius: 7px;
        background: var(--orange-wash);
        border: 1px solid var(--orange-line);
        transition: background 0.13s ease;
    }
    .shields:hover { background: var(--orange-wash-hover); }
    .shields svg {
        width: 14px;
        height: 14px;
        fill: none;
        stroke: var(--orange);
        stroke-width: 1.7;
        stroke-linejoin: round;
    }
    .shield-count {
        font-size: 11.5px;
        font-weight: 650;
        color: var(--orange);
        letter-spacing: -0.01em;
        font-variant-numeric: tabular-nums;
    }

    /* ── Bookmarks bar ────────────────────────────────────────────────── */

    .bbar {
        height: 30px;
        flex: 0 0 30px;
        display: none;
        align-items: center;
        gap: 2px;
        padding: 0 6px 0 8px;
        border-top: 1px solid var(--hairline);
        -webkit-app-region: drag;
        overflow: hidden;
    }
    .bbar.show { display: flex; }

    .bbar-items {
        position: relative;
        flex: 1 1 auto;
        min-width: 0;
        height: 100%;
        display: flex;
        align-items: center;
        gap: 2px;
        overflow: hidden;
    }

    .bitem {
        display: flex;
        align-items: center;
        gap: 6px;
        height: 22px;
        flex: none;
        max-width: 170px;
        padding: 0 7px;
        border-radius: 5px;
        color: var(--text2);
        font-size: 12px;
        font-weight: 400;
        letter-spacing: -0.005em;
        white-space: nowrap;
        overflow: hidden;
        transition: background 0.12s ease, color 0.12s ease;
        -webkit-app-region: no-drag;
    }
    .bitem:hover { background: var(--btn-hover); color: var(--text); }
    .bitem:active { background: var(--field-hover); }

    .bitem .bname {
        min-width: 0;
        overflow: hidden;
        text-overflow: ellipsis;
    }

    .bfav {
        width: 15px;
        height: 15px;
        flex: none;
        border-radius: 3px;
        display: grid;
        place-items: center;
        font-size: 8.5px;
        font-weight: 700;
        color: #fff;
        box-shadow: var(--tile-shadow);
    }
    .bfav img {
        width: 100%;
        height: 100%;
        object-fit: contain;
        display: block;
        border-radius: 3px;
    }
    .bfav.plain { background: transparent; box-shadow: none; }
    .bfav.folder {
        background: transparent;
        box-shadow: none;
    }
    .bfav.folder svg {
        width: 14px;
        height: 14px;
        fill: none;
        stroke: var(--text3);
        stroke-width: 1.65;
        stroke-linejoin: round;
    }
    .bitem:hover .bfav.folder svg { stroke: var(--text); }

    .bbar-more {
        width: 20px;
        height: 22px;
        flex: none;
        border-radius: 5px;
        display: none;
        place-items: center;
        color: var(--text2);
        -webkit-app-region: no-drag;
    }
    .bbar-more.on { display: grid; }
    .bbar-more:hover { background: var(--btn-hover); color: var(--text); }
    .bbar-more svg {
        width: 13px;
        height: 13px;
        fill: none;
        stroke: currentColor;
        stroke-width: 2;
        stroke-linecap: round;
        stroke-linejoin: round;
    }

    /* ── Loading line ─────────────────────────────────────────────────── */

    .progress {
        position: absolute;
        left: 0;
        right: 0;
        bottom: 0;
        height: 2px;
        overflow: hidden;
        opacity: 0;
        transition: opacity 0.22s ease;
        pointer-events: none;
    }
    .progress.on { opacity: 1; }
    .progress i {
        display: block;
        width: 35%;
        height: 100%;
        border-radius: 2px;
        background: var(--accent);
        animation: slide 1.15s cubic-bezier(0.45, 0, 0.55, 1) infinite;
    }
    @keyframes slide {
        0% { transform: translateX(-110%); }
        100% { transform: translateX(320%); }
    }

    .toast {
        position: absolute;
        left: 50%;
        bottom: 9px;
        transform: translate(-50%, 6px);
        padding: 5px 12px;
        border-radius: 7px;
        background: rgba(28, 28, 30, 0.94);
        color: #fff;
        font-size: 11.5px;
        font-weight: 500;
        letter-spacing: -0.01em;
        white-space: nowrap;
        opacity: 0;
        pointer-events: none;
        z-index: 10;
        transition: opacity 0.2s ease, transform 0.2s ease;
    }
    .toast.show { opacity: 1; transform: translate(-50%, 0); }
</style>
</head>
<body class='__THEME__'>

    <div class='tabstrip'>
        <div class='tabs' id='tabs'></div>

        <div class='newtab' id='btn-newtab' title='New tab (Ctrl+T)'>
            <svg viewBox='0 0 24 24'><path d='M12 5.5v13M5.5 12h13'/></svg>
        </div>

        <div class='priv' id='priv' title='Private browsing'>
            <svg viewBox='0 0 24 24'><circle cx='7.4' cy='14' r='3.2'/><circle cx='16.6' cy='14' r='3.2'/><path d='M10.6 14h2.8'/><path d='M4.5 13.4 6.1 8.5h3.1'/><path d='M19.5 13.4 17.9 8.5h-3.1'/></svg>
            <span>Private</span>
        </div>

        <div class='wctl'>
            <div class='wbtn min' id='wctl-min' title='Minimize'>
                <svg viewBox='0 0 10 10'><path d='M2 5h6'/></svg>
            </div>
            <div class='wbtn max' id='wctl-max' title='Maximize'>
                <svg viewBox='0 0 10 10'><rect x='2.2' y='2.2' width='5.6' height='5.6' rx='1'/></svg>
            </div>
            <div class='wbtn close' id='wctl-close' title='Close'>
                <svg viewBox='0 0 10 10'><path d='M2.6 2.6l4.8 4.8M7.4 2.6L2.6 7.4'/></svg>
            </div>
        </div>
    </div>

    <div class='toolbar'>
        <div class='nav'>
            <button class='iconbtn' id='btn-back' title='Back (Alt+Left)' disabled>
                <svg viewBox='0 0 24 24'><path d='M14.6 5.6 8.2 12l6.4 6.4'/></svg>
            </button>
            <button class='iconbtn' id='btn-fwd' title='Forward (Alt+Right)' disabled>
                <svg viewBox='0 0 24 24'><path d='M9.4 5.6 15.8 12l-6.4 6.4'/></svg>
            </button>
        </div>

        <div class='fieldwrap'>
            <div class='field nopage' id='field'>
                <div class='aa' id='btn-aa' title='Page Settings'>
                    <span class='a1'>A</span><span class='a2'>A</span>
                </div>

                <div class='url-display' id='url-display'></div>
                <input class='url' id='url-input' type='text' spellcheck='false'
                       autocomplete='off' autocapitalize='off' />

                <div class='star' id='btn-star' title='Bookmark this page (Ctrl+D)'>
                    <svg viewBox='0 0 24 24'><path d='M12 3.6l2.5 5.1 5.6.8-4.05 3.95.96 5.58L12 16.4l-5.01 2.63.96-5.58L3.9 9.5l5.6-.8z'/></svg>
                </div>

                <button class='reload' id='btn-reload' title='Reload (Ctrl+R)'>
                    <svg class='ico-reload' viewBox='0 0 24 24'><path d='M19.5 12a7.5 7.5 0 1 1-2.2-5.3'/><path d='M19.5 4.6V9h-4.4'/></svg>
                    <svg class='ico-stop' viewBox='0 0 24 24'><path d='M7 7l10 10M17 7L7 17'/></svg>
                </button>
            </div>
        </div>

        <div class='actions'>
            <button class='iconbtn dl' id='btn-dl' title='Downloads (Ctrl+J)'>
                <svg viewBox='0 0 24 24'><path d='M12 4.2v9.4'/><path d='M7.8 9.6 12 13.8l4.2-4.2'/><path d='M5.2 17.4h13.6'/></svg>
                <span class='dl-count none' id='dl-count'>0</span>
            </button>

            <div class='shields' id='btn-shields' title='Aura Shields'>
                <svg viewBox='0 0 24 24'><path d='M12 3.2 5.2 6v5.3c0 4.35 2.9 7.6 6.8 9.5 3.9-1.9 6.8-5.15 6.8-9.5V6L12 3.2z'/></svg>
                <span class='shield-count' id='shields-count'>0</span>
            </div>

            <button class='iconbtn' id='btn-share' title='Share this page'>
                <svg viewBox='0 0 24 24'><path d='M12 15.2V3.8'/><path d='M8.3 7.4 12 3.7l3.7 3.7'/><path d='M5.6 11.5v7.2c0 .6.5 1.1 1.1 1.1h10.6c.6 0 1.1-.5 1.1-1.1v-7.2'/></svg>
            </button>

            <button class='iconbtn' id='btn-menu' title='Menu'>
                <svg class='dots' viewBox='0 0 24 24'><circle cx='5.4' cy='12' r='1.7'/><circle cx='12' cy='12' r='1.7'/><circle cx='18.6' cy='12' r='1.7'/></svg>
            </button>
        </div>
    </div>

    <div class='bbar' id='bbar'>
        <div class='bbar-items' id='bbar-items'></div>
        <!-- No title here: Chromium's popup tooltip would sit over the first
             item of the menu this button opens. -->
        <div class='bbar-more' id='bbar-more'>
            <svg viewBox='0 0 24 24'><path d='M9.5 6l6 6-6 6'/></svg>
        </div>
    </div>

    <div class='progress' id='progress'><i></i></div>
    <div class='toast' id='toast'></div>

<script>
    function post(action, data) {
        if (window.chrome && window.chrome.webview) {
            var msg = data || {};
            msg.action = action;
            window.chrome.webview.postMessage(msg);
        }
    }

    var field = document.getElementById('field');
    var urlInput = document.getElementById('url-input');
    var urlDisplay = document.getElementById('url-display');
    var currentUrl = '';

    // Host -> page messages. Used as a second, independent channel for
    // keyboard shortcuts that move focus into the chrome.
    if (window.chrome && window.chrome.webview) {
        window.chrome.webview.addEventListener('message', function (e) {
            var m = e.data;
            if (m && m.action === 'focus_url') { focusUrl(); }
        });
    }

    var PALETTE = [
        '#007aff', '#34c759', '#ff9500', '#ff3b30', '#af52de',
        '#5856d6', '#5ac8fa', '#ff2d55', '#30b0c7', '#8e8e93'
    ];

    function colorFor(text) {
        var h = 0;
        for (var i = 0; i < text.length; i++) {
            h = (h * 31 + text.charCodeAt(i)) & 0xffff;
        }
        return PALETTE[h % PALETTE.length];
    }

    function hostOf(url) {
        if (!url) { return ''; }
        var m = /^[a-z][a-z0-9+.-]*:\/\/([^\/?#]+)/i.exec(url);
        if (m) { return m[1].replace(/^www\./i, ''); }
        return '';
    }

    function pretty(url) {
        if (!url || url === 'about:home') {
            return { host: '', rest: '', empty: true };
        }
        var m = /^([a-z][a-z0-9+.-]*:\/\/)([^\/?#]*)([\s\S]*)$/i.exec(url);
        if (!m) { return { host: url, rest: '', empty: false }; }
        var host = m[2].replace(/^www\./i, '');
        var rest = m[3] || '';
        if (rest === '/') { rest = ''; }
        return { host: host, rest: rest, empty: false };
    }

    function hasPage() {
        return !!currentUrl
            && currentUrl.indexOf('about:') !== 0
            && currentUrl.indexOf('data:') !== 0;
    }

    function renderUrl() {
        var p = pretty(currentUrl);
        urlDisplay.innerHTML = '';

        if (p.empty) {
            var placeholder = document.createElement('span');
            placeholder.className = 'u-placeholder';
            placeholder.textContent = 'Search or enter website';
            urlDisplay.appendChild(placeholder);
            return;
        }

        var host = document.createElement('span');
        host.className = 'u-host';
        host.textContent = p.host;
        urlDisplay.appendChild(host);

        if (p.rest) {
            var rest = document.createElement('span');
            rest.className = 'u-rest';
            rest.textContent = p.rest;
            urlDisplay.appendChild(rest);
        }
    }

    function focusUrl() {
        field.classList.add('focused');
        urlInput.value = currentUrl === 'about:home' ? '' : currentUrl;
        urlInput.focus();
        urlInput.select();
    }

    function fieldBox() {
        var r = field.getBoundingClientRect();
        return {
            x: Math.round(r.left),
            y: Math.round(r.bottom),
            w: Math.round(r.width)
        };
    }

    field.addEventListener('mousedown', function (e) {
        if (e.target === field || e.target === urlDisplay || urlDisplay.contains(e.target)) {
            e.preventDefault();
            focusUrl();
        }
    });

    urlInput.addEventListener('focus', function () {
        field.classList.add('focused');
    });

    urlInput.addEventListener('blur', function () {
        field.classList.remove('focused');
        urlInput.value = '';
        renderUrl();
        post('blur_url');
    });

    // Every keystroke asks the host for suggestions; the list itself is a
    // native window under the field, which is the only way out of this strip.
    urlInput.addEventListener('input', function () {
        var at = fieldBox();
        post('url_input', { text: urlInput.value, x: at.x, y: at.y, w: at.w });
    });

    urlInput.addEventListener('keydown', function (e) {
        if (e.key === 'Enter') {
            var value = urlInput.value.trim();
            urlInput.blur();
            if (value) { post('navigate', { url: value }); }
        } else if (e.key === 'Escape') {
            urlInput.value = '';
            urlInput.blur();
            post('blur_url');
        } else if (e.key === 'ArrowDown') {
            e.preventDefault();
            post('url_key', { key: 'down' });
        } else if (e.key === 'ArrowUp') {
            e.preventDefault();
            post('url_key', { key: 'up' });
        }
    });

    document.getElementById('btn-back').addEventListener('click', function () { post('back'); });
    document.getElementById('btn-fwd').addEventListener('click', function () { post('forward'); });
    document.getElementById('btn-newtab').addEventListener('click', function () { post('new_tab'); });
    document.getElementById('btn-star').addEventListener('click', function (e) {
        e.stopPropagation();
        post('bookmark');
    });
    document.getElementById('btn-aa').addEventListener('click', function (e) {
        e.stopPropagation();
        var at = barAnchor(this);
        post('page_settings', { x: at.x, y: at.y });
    });
    document.getElementById('btn-share').addEventListener('click', function () { post('share'); });
    document.getElementById('btn-menu').addEventListener('click', function () { post('menu'); });
    document.getElementById('btn-shields').addEventListener('click', function () { post('shield'); });
    document.getElementById('btn-dl').addEventListener('click', function () { post('downloads'); });
    document.getElementById('btn-reload').addEventListener('click', function () {
        post(field.classList.contains('loading') ? 'stop' : 'reload');
    });

    document.getElementById('wctl-close').addEventListener('click', function () { post('window_close'); });
    document.getElementById('wctl-min').addEventListener('click', function () { post('window_min'); });
    document.getElementById('wctl-max').addEventListener('click', function () { post('window_max'); });

    document.querySelector('.tabstrip').addEventListener('dblclick', function (e) {
        if (e.target === field || field.contains(e.target)) { return; }
        post('window_max');
    });

    // ── Bookmarks bar ────────────────────────────────────────────────────
    // The host decides whether the bar is on and what it holds. Folders open a
    // native menu owned by the host, which is the only way to escape this
    // WebView's own rectangle.

    var bbar = document.getElementById('bbar');
    var bbarItems = document.getElementById('bbar-items');
    var bbarMore = document.getElementById('bbar-more');
    var barOverflowFrom = -1;

    function folderGlyph() {
        var ns = 'http://www.w3.org/2000/svg';
        var svg = document.createElementNS(ns, 'svg');
        svg.setAttribute('viewBox', '0 0 24 24');
        var path = document.createElementNS(ns, 'path');
        path.setAttribute('d', 'M3.5 7.6c0-.9.7-1.6 1.6-1.6h3.6l1.7 2h8.5c.9 0 1.6.7 1.6 1.6v7.2c0 .9-.7 1.6-1.6 1.6H5.1c-.9 0-1.6-.7-1.6-1.6z');
        svg.appendChild(path);
        return svg;
    }

    function barIcon(item) {
        var fav = document.createElement('div');
        fav.className = 'bfav';

        if (item.kind === 'folder') {
            fav.classList.add('folder');
            fav.appendChild(folderGlyph());
            return fav;
        }

        if (item.icon) {
            var img = document.createElement('img');
            img.alt = '';
            img.addEventListener('error', function () {
                // Nothing came back — the network is down or the service has no
                // icon for the host. Draw the letter tile instead of an empty box.
                letterTile(fav, item);
            });
            img.src = item.icon;
            fav.classList.add('plain');
            fav.appendChild(img);
            return fav;
        }

        letterTile(fav, item);
        return fav;
    }

    function letterTile(fav, item) {
        var label = hostOf(item.url) || item.title || 'A';
        fav.classList.remove('plain');
        fav.innerHTML = '';
        fav.style.background = colorFor(label);
        fav.textContent = (label.charAt(0) || 'A').toUpperCase();
    }

    function barAnchor(el) {
        var r = el.getBoundingClientRect();
        return { x: Math.round(r.left), y: Math.round(r.bottom) };
    }

    function buildBarItem(item) {
        var el = document.createElement('div');
        el.className = 'bitem';
        el.dataset.tip = item.kind === 'folder'
            ? (item.title || 'Folder') + ' \u2014 folder'
            : (item.title || item.url || '');

        el.appendChild(barIcon(item));

        var name = document.createElement('span');
        name.className = 'bname';
        name.textContent = item.title || item.url || '';
        el.appendChild(name);

        el.addEventListener('click', function (e) {
            var at = barAnchor(el);
            if (item.kind === 'folder') {
                post('bm_folder', { path: item.path, x: at.x, y: at.y });
            } else {
                post('bm_open', { url: item.url, newTab: e.metaKey || e.ctrlKey, x: at.x, y: at.y });
            }
        });

        el.addEventListener('auxclick', function (e) {
            if (e.button !== 1) { return; }
            e.preventDefault();
            if (item.kind === 'folder') {
                post('bm_folder_all', { path: item.path });
            } else {
                post('bm_open', { url: item.url, newTab: true, x: 0, y: 0 });
            }
        });

        el.addEventListener('contextmenu', function (e) {
            e.preventDefault();
            e.stopPropagation();
            var at = barAnchor(el);
            post('bm_menu', {
                kind: item.kind,
                url: item.url,
                path: item.path,
                title: item.title,
                x: at.x,
                y: at.y
            });
        });

        return el;
    }

    // Keeps as many items on the bar as fit, and moves the rest behind the
    // chevron. Runs again on every resize.
    function layoutBar() {
        var nodes = bbarItems.children;
        var i;

        for (i = 0; i < nodes.length; i++) { nodes[i].style.display = ''; }
        bbarMore.classList.remove('on');
        barOverflowFrom = -1;

        if (nodes.length === 0) { return; }

        var limit = bbarItems.clientWidth;
        for (i = 0; i < nodes.length; i++) {
            if (nodes[i].offsetLeft + nodes[i].offsetWidth > limit + 1) {
                barOverflowFrom = i;
                break;
            }
        }
        if (barOverflowFrom < 0) {
            markTruncated(nodes);
            return;
        }

        bbarMore.classList.add('on');
        limit = bbarItems.clientWidth;

        for (i = barOverflowFrom; i < nodes.length; i++) { nodes[i].style.display = 'none'; }

        // The chevron itself took room, so check the items that looked fine.
        for (i = 0; i < barOverflowFrom; i++) {
            if (nodes[i].offsetLeft + nodes[i].offsetWidth > limit + 1) {
                barOverflowFrom = i;
                for (var j = i; j < nodes.length; j++) { nodes[j].style.display = 'none'; }
                break;
            }
        }

        markTruncated(nodes);
    }

    // Chromium draws its own popup tooltip, which would sit over a menu that
    // opens directly under the item. Only ask for one when the label is
    // actually cut off.
    function markTruncated(nodes) {
        for (var i = 0; i < nodes.length; i++) {
            var el = nodes[i];
            if (el.style.display === 'none') { el.removeAttribute('title'); continue; }

            var name = el.querySelector('.bname');
            if (name && name.scrollWidth > name.clientWidth + 1) { el.title = el.dataset.tip; }
            else { el.removeAttribute('title'); }
        }
    }

    bbarMore.addEventListener('click', function () {
        if (barOverflowFrom < 0) { return; }
        var at = barAnchor(bbarMore);
        post('bm_more', { index: barOverflowFrom, x: at.x, y: at.y });
    });

    bbar.addEventListener('contextmenu', function (e) {
        e.preventDefault();
        var r = bbar.getBoundingClientRect();
        post('bm_bar_menu', { x: Math.round(e.clientX), y: Math.round(r.bottom) });
    });

    window.addEventListener('resize', function () { layoutBar(); });

    window.BraveAPI = {
        renderTabs: function (tabs, activeIdx) {
            var container = document.getElementById('tabs');
            container.innerHTML = '';

            for (var i = 0; i < tabs.length; i++) {
                (function (idx) {
                    var info = tabs[idx] || {};
                    var title = info.title || 'New Tab';
                    var url = info.url || '';
                    var host = hostOf(url) || title;

                    var tab = document.createElement('div');
                    tab.className = 'tab' + (idx === activeIdx ? ' active' : '');
                    tab.title = title;

                    var fav = document.createElement('div');
                    fav.className = 'tab-fav';
                    if (info.icon) {
                        var icon = document.createElement('img');
                        icon.src = info.icon;
                        icon.alt = '';
                        fav.classList.add('plain');
                        fav.appendChild(icon);
                    } else if (!url) {
                        fav.classList.add('home');
                        fav.innerHTML = '<svg viewBox=""0 0 24 24""><circle cx=""12"" cy=""12"" r=""8""/><path d=""M4 12h16""/><path d=""M12 4c2.2 2.2 3.3 5 3.3 8s-1.1 5.8-3.3 8c-2.2-2.2-3.3-5-3.3-8s1.1-5.8 3.3-8z""/></svg>';
                    } else {
                        fav.style.background = colorFor(host);
                        fav.textContent = (host.charAt(0) || 'A').toUpperCase();
                    }

                    var label = document.createElement('span');
                    label.className = 'tab-title';
                    label.textContent = title;

                    var close = document.createElement('div');
                    close.className = 'tab-x';
                    close.title = 'Close tab';
                    close.innerHTML = '<svg viewBox=""0 0 20 20""><path d=""M5 5l10 10M15 5l-10 10""/></svg>';

                    tab.appendChild(fav);
                    tab.appendChild(label);
                    tab.appendChild(close);

                    tab.addEventListener('click', function () {
                        post('switch_tab', { index: idx });
                    });
                    tab.addEventListener('auxclick', function (e) {
                        if (e.button === 1) {
                            e.preventDefault();
                            post('close_tab', { index: idx });
                        }
                    });
                    close.addEventListener('click', function (e) {
                        e.stopPropagation();
                        post('close_tab', { index: idx });
                    });

                    container.appendChild(tab);
                })(i);
            }
        },

        setUrl: function (url) {
            currentUrl = url || '';
            field.classList.toggle('nopage', !hasPage());
            if (!field.classList.contains('focused')) {
                renderUrl();
            }
        },

        setNavState: function (canBack, canForward, loading) {
            document.getElementById('btn-back').disabled = !canBack;
            document.getElementById('btn-fwd').disabled = !canForward;
            field.classList.toggle('loading', !!loading);
            document.getElementById('progress').classList.toggle('on', !!loading);
        },

        setBookmarked: function (yes) {
            var star = document.getElementById('btn-star');
            star.classList.toggle('on', !!yes);
            star.title = yes ? 'Remove bookmark (Ctrl+D)' : 'Bookmark this page (Ctrl+D)';
        },

        renderBookmarks: function (show, items) {
            bbar.classList.toggle('show', !!show);
            bbarItems.innerHTML = '';
            barOverflowFrom = -1;

            var list = items || [];
            for (var i = 0; i < list.length; i++) {
                bbarItems.appendChild(buildBarItem(list[i]));
            }

            layoutBar();
        },

        focusUrl: function () {
            focusUrl();
        },

        setInput: function (text) {
            urlInput.value = text || '';
        },

        setPrivate: function (on) {
            var el = document.getElementById('priv');
            if (el) { el.classList.toggle('on', !!on); }
        },

        setDownloads: function (total, active) {
            var btn = document.getElementById('btn-dl');
            var badge = document.getElementById('dl-count');
            if (!btn) { return; }

            btn.classList.toggle('on', total > 0);

            if (badge) {
                badge.classList.toggle('none', !(active > 0));
                badge.textContent = active > 99 ? '99+' : String(active);
            }

            btn.title = active > 0
                ? 'Downloads - ' + active + ' in progress (Ctrl+J)'
                : 'Downloads (Ctrl+J)';
        },

        setMaximized: function (yes) {
            var btn = document.getElementById('wctl-max');
            if (!btn) { return; }
            btn.title = yes ? 'Restore' : 'Maximize';
            // Windows draws the restore glyph as two overlapping squares.
            btn.innerHTML = yes
                ? '<svg viewBox=""0 0 10 10""><rect x=""2"" y=""3.4"" width=""4.8"" height=""4.8"" rx=""1""/><path d=""M3.6 3.4V2.2a.7.7 0 0 1 .7-.7h3.5a.7.7 0 0 1 .7.7v3.5a.7.7 0 0 1-.7.7H7.8""/></svg>'
                : '<svg viewBox=""0 0 10 10""><rect x=""2.2"" y=""2.2"" width=""5.6"" height=""5.6"" rx=""1""/></svg>';
        },

        toast: function (text) {
            var el = document.getElementById('toast');
            el.textContent = text;
            el.classList.add('show');
            if (window.__toastTimer) { clearTimeout(window.__toastTimer); }
            window.__toastTimer = setTimeout(function () {
                el.classList.remove('show');
            }, 1600);
        }
    };

    renderUrl();
</script>
</body>
</html>";

            return html.Replace("__THEME__", dark ? "dark" : "light");
        }
    }
}
