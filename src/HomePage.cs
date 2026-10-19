using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleBrowser
{
    public static class HomePage
    {
        public static string GetHtml(bool dark)
        {
            string html = @"<!DOCTYPE html>
<html lang='en'>
<head>
<meta charset='utf-8' />
<meta name='viewport' content='width=device-width, initial-scale=1.0' />
<title>New Tab</title>
<style>
    * { box-sizing: border-box; margin: 0; padding: 0; }

    html { height: 100%; }

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
        --sheet-w: 340px;

        min-height: 100vh;
        font-family: 'SF Pro Text', 'SF Pro Display', -apple-system, BlinkMacSystemFont,
                     'Segoe UI Variable Text', 'Segoe UI', system-ui, sans-serif;
        color: var(--text);
        background:
            radial-gradient(1000px 520px at 50% -10%, #dbe8ff 0%, rgba(219, 232, 255, 0) 60%),
            radial-gradient(720px 420px at 86% 112%, rgba(255, 226, 214, 0.55) 0%, rgba(255, 226, 214, 0) 62%),
            linear-gradient(180deg, #f8f9fc 0%, #eef0f5 100%);
        background-attachment: fixed;
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

    /* Text standing on a wallpaper picture is drawn light, with the search
       field and hover states flipped to match. */
    body.on-wall {
        --text: #ffffff;
        --text2: rgba(255, 255, 255, 0.88);
        --text3: rgba(255, 255, 255, 0.66);
        --field: rgba(20, 20, 22, 0.58);
        --field-line: rgba(255, 255, 255, 0.16);
        --field-shadow: 0 1px 2px rgba(0, 0, 0, 0.28), 0 10px 34px rgba(0, 0, 0, 0.30);
        --hover: rgba(255, 255, 255, 0.16);
        --tile-line: rgba(255, 255, 255, 0.22);
    }
    body.on-wall .search-input::placeholder { color: rgba(255, 255, 255, 0.62); }
    body.on-wall .search:hover { border-color: rgba(255, 255, 255, 0.22); }

    /* ── Wallpaper ────────────────────────────────────────────────────── */

    .wall {
        position: fixed;
        inset: -24px;
        z-index: -2;
        background-repeat: no-repeat;
        background-position: center center;
        background-size: cover;
        transition: opacity 0.25s ease;
    }

    .wall-tint {
        position: fixed;
        inset: 0;
        z-index: -1;
        background: transparent;
        pointer-events: none;
    }

    /* ── Stage ────────────────────────────────────────────────────────── */

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

    /* ── Shortcuts ────────────────────────────────────────────────────── */

    .favorites {
        display: flex;
        flex-wrap: wrap;
        justify-content: center;
        gap: 4px 2px;
        max-width: 620px;
        width: 100%;
        min-height: 74px;
    }

    .fav {
        position: relative;
        width: 74px;
        padding: 9px 4px 8px;
        border-radius: 12px;
        display: flex;
        flex-direction: column;
        align-items: center;
        text-decoration: none;
        color: inherit;
        transition: background 0.14s ease, transform 0.14s ease;
        -webkit-user-select: none;
        user-select: none;
        /* Shortcuts are reordered with pointer events, so the browser's own
           link dragging has to stay out of the way. */
        -webkit-user-drag: none;
    }
    .fav:hover { background: var(--hover); transform: translateY(-2px); }
    .fav.dragging { opacity: 0.4; }
    body.reordering, body.reordering * { cursor: grabbing !important; }
    .fav.drop-before::before,
    .fav.drop-after::after {
        content: '';
        position: absolute;
        top: 4px;
        bottom: 4px;
        width: 2px;
        border-radius: 2px;
        background: var(--accent);
    }
    .fav.drop-before::before { left: -3px; }
    .fav.drop-after::after { right: -3px; }

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

    .fav-del {
        position: absolute;
        top: 4px;
        right: 6px;
        width: 19px;
        height: 19px;
        display: grid;
        place-items: center;
        padding: 0;
        border: none;
        border-radius: 50%;
        background: rgba(28, 28, 30, 0.72);
        color: #fff;
        cursor: pointer;
        opacity: 0;
        transform: scale(0.7);
        transition: opacity 0.13s ease, transform 0.13s ease, background 0.13s ease;
        box-shadow: 0 1px 4px rgba(0, 0, 0, 0.35);
        z-index: 3;
    }
    .fav:hover .fav-del { opacity: 1; transform: scale(1); }
    .fav-del:hover { background: #ff453a; }
    .fav-del svg {
        width: 11px;
        height: 11px;
        fill: none;
        stroke: #fff;
        stroke-width: 2.2;
        stroke-linecap: round;
    }

    .fav.add .tile {
        background: transparent;
        border: 1.5px dashed var(--text3);
        box-shadow: none;
        color: var(--text3);
    }
    .fav.add:hover .tile { color: var(--text); border-color: var(--text2); }
    .fav.add .tile svg {
        width: 18px;
        height: 18px;
        fill: none;
        stroke: currentColor;
        stroke-width: 1.8;
        stroke-linecap: round;
    }
    .fav.add .label { color: var(--text3); }

    /* ── Customize button ─────────────────────────────────────────────── */

    .fab {
        position: fixed;
        right: 20px;
        bottom: 66px;
        z-index: 30;
        display: flex;
        align-items: center;
        gap: 7px;
        height: 34px;
        padding: 0 14px 0 11px;
        border: 1px solid var(--field-line);
        border-radius: 17px;
        background: var(--field);
        color: var(--text2);
        font: inherit;
        font-size: 12.5px;
        font-weight: 500;
        letter-spacing: -0.01em;
        cursor: pointer;
        opacity: 0.72;
        box-shadow: var(--field-shadow);
        transition: opacity 0.15s ease, color 0.15s ease, transform 0.15s ease,
                    right 0.24s cubic-bezier(0.32, 0.72, 0, 1);
    }
    .fab:hover { opacity: 1; color: var(--text); transform: translateY(-1px); }
    .fab svg {
        width: 14px;
        height: 14px;
        fill: none;
        stroke: currentColor;
        stroke-width: 1.7;
        stroke-linecap: round;
        stroke-linejoin: round;
    }

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

    /* ── Surfaces: sheet, modals, menus ───────────────────────────────── */

    .sheet, .modal, .tile-menu {
        --s-text: #1d1d1f;
        --s-text2: #6e6e73;
        --s-text3: #9a9aa0;
        --s-bg: #ffffff;
        --s-line: rgba(0, 0, 0, 0.08);
        --s-hover: rgba(0, 0, 0, 0.05);
        --s-field: #f2f2f4;
        --s-shadow: 0 18px 50px rgba(0, 0, 0, 0.20), 0 2px 8px rgba(0, 0, 0, 0.08);
        color: var(--s-text);
    }
    body.dark .sheet, body.dark .modal, body.dark .tile-menu {
        --s-text: #f5f5f7;
        --s-text2: #a1a1a6;
        --s-text3: #8a8a90;
        --s-bg: #2a2a2d;
        --s-line: rgba(255, 255, 255, 0.09);
        --s-hover: rgba(255, 255, 255, 0.07);
        --s-field: #3a3a3d;
        --s-shadow: 0 18px 50px rgba(0, 0, 0, 0.55), 0 2px 8px rgba(0, 0, 0, 0.35);
    }

    .modal-scrim {
        position: fixed;
        inset: 0;
        z-index: 60;
        background: rgba(0, 0, 0, 0.38);
        opacity: 0;
        pointer-events: none;
        transition: opacity 0.2s ease;
    }
    body.modal-open .modal-scrim { opacity: 1; pointer-events: auto; }

    .sheet {
        position: fixed;
        top: 0;
        right: 0;
        bottom: 0;
        z-index: 50;
        width: var(--sheet-w);
        max-width: 92vw;
        background: var(--s-bg);
        box-shadow: var(--s-shadow);
        display: flex;
        flex-direction: column;
        transform: translateX(102%);
        transition: transform 0.24s cubic-bezier(0.32, 0.72, 0, 1);
    }
    body.sheet-open .sheet { transform: translateX(0); }

    .sheet-head {
        flex: none;
        display: flex;
        align-items: center;
        justify-content: space-between;
        padding: 16px 14px 12px 20px;
        border-bottom: 1px solid var(--s-line);
    }
    .sheet-title { font-size: 15px; font-weight: 600; letter-spacing: -0.01em; }

    .icon-btn {
        width: 28px;
        height: 28px;
        display: grid;
        place-items: center;
        padding: 0;
        border: none;
        border-radius: 8px;
        background: transparent;
        color: var(--s-text2);
        cursor: pointer;
    }
    .icon-btn:hover { background: var(--s-hover); color: var(--s-text); }
    .icon-btn svg {
        width: 13px;
        height: 13px;
        fill: none;
        stroke: currentColor;
        stroke-width: 2;
        stroke-linecap: round;
    }

    .sheet-body { flex: 1 1 auto; overflow-y: auto; padding: 4px 20px 30px; }

    .group { padding: 16px 0 18px; border-bottom: 1px solid var(--s-line); }
    .group:last-child { border-bottom: none; }
    .group h2 {
        font-size: 11px;
        font-weight: 600;
        letter-spacing: 1.2px;
        text-transform: uppercase;
        color: var(--s-text3);
        margin-bottom: 12px;
    }
    .hint { font-size: 11.5px; line-height: 1.45; color: var(--s-text3); margin-top: 9px; }

    .switch {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 12px;
        padding: 7px 0;
        font-size: 13px;
        cursor: pointer;
    }
    .switch input { position: absolute; opacity: 0; width: 0; height: 0; }
    .switch i {
        position: relative;
        flex: none;
        width: 40px;
        height: 24px;
        border-radius: 12px;
        background: var(--s-field);
        border: 1px solid var(--s-line);
        transition: background 0.16s ease;
    }
    .switch i::after {
        content: '';
        position: absolute;
        top: 2px;
        left: 2px;
        width: 18px;
        height: 18px;
        border-radius: 50%;
        background: #fff;
        box-shadow: 0 1px 3px rgba(0, 0, 0, 0.28);
        transition: transform 0.16s ease;
    }
    .switch input:checked + i { background: #34c759; border-color: transparent; }
    .switch input:checked + i::after { transform: translateX(16px); }

    .swatches {
        display: grid;
        grid-template-columns: repeat(6, 1fr);
        gap: 8px;
        margin-bottom: 14px;
    }
    .swatch {
        position: relative;
        aspect-ratio: 1 / 1;
        border: 1px solid var(--s-line);
        border-radius: 9px;
        padding: 0;
        cursor: pointer;
        overflow: hidden;
        transition: transform 0.12s ease;
    }
    .swatch:hover { transform: scale(1.06); }
    .swatch.on { box-shadow: 0 0 0 2px var(--s-bg), 0 0 0 4px #007aff; }
    .swatch.default-swatch {
        display: grid;
        place-items: center;
        background: linear-gradient(180deg, #f8f9fc 0%, #eef0f5 100%);
        color: #6e6e73;
        font-size: 10px;
        font-weight: 600;
        letter-spacing: -0.01em;
    }
    .swatch.image-swatch { background: var(--s-field); }
    .swatch.image-swatch svg {
        width: 18px;
        height: 18px;
        fill: none;
        stroke: #6e6e73;
        stroke-width: 1.7;
        stroke-linecap: round;
        stroke-linejoin: round;
    }

    .row { display: flex; align-items: center; gap: 8px; margin-bottom: 10px; }

    .btn {
        height: 30px;
        padding: 0 12px;
        border: 1px solid var(--s-line);
        border-radius: 8px;
        background: var(--s-field);
        color: var(--s-text);
        font: inherit;
        font-size: 12.5px;
        font-weight: 500;
        letter-spacing: -0.01em;
        cursor: pointer;
        white-space: nowrap;
        transition: background 0.13s ease, border-color 0.13s ease, opacity 0.13s ease;
    }
    .btn:hover { background: var(--s-hover); }
    .btn.wide { width: 100%; }
    .btn.ghost { background: transparent; }
    .btn.ghost:hover { background: var(--s-hover); }
    .btn.primary { background: #007aff; border-color: transparent; color: #fff; }
    .btn.primary:hover { background: #006ae0; }
    .btn.danger { color: #ff453a; }
    .btn.danger-solid { background: #ff453a; border-color: transparent; color: #fff; }
    .btn.danger-solid:hover { background: #e8362b; }

    .colorwrap {
        position: relative;
        flex: none;
        width: 30px;
        height: 30px;
        border-radius: 8px;
        overflow: hidden;
        border: 1px solid var(--s-line);
        cursor: pointer;
    }
    .colorwrap input {
        position: absolute;
        inset: -4px;
        width: 200%;
        height: 200%;
        border: none;
        padding: 0;
        background: transparent;
        cursor: pointer;
    }

    .text-input {
        flex: 1 1 auto;
        min-width: 0;
        height: 30px;
        padding: 0 10px;
        border: 1px solid var(--s-line);
        border-radius: 8px;
        background: var(--s-field);
        color: var(--s-text);
        font: inherit;
        font-size: 12.5px;
        outline: none;
    }
    .text-input:focus { border-color: var(--accent); }

    .slider {
        display: flex;
        align-items: center;
        gap: 10px;
        padding: 7px 0;
        font-size: 13px;
    }
    .slider > span:first-child { flex: none; width: 42px; color: var(--s-text2); }
    .slider input[type=range] {
        flex: 1 1 auto;
        min-width: 0;
        -webkit-appearance: none;
        height: 4px;
        border-radius: 2px;
        background: var(--s-field);
        outline: none;
    }
    .slider input[type=range]::-webkit-slider-thumb {
        -webkit-appearance: none;
        width: 15px;
        height: 15px;
        border-radius: 50%;
        background: #fff;
        border: 1px solid var(--s-line);
        box-shadow: 0 1px 3px rgba(0, 0, 0, 0.30);
        cursor: pointer;
    }
    .slider b {
        flex: none;
        width: 38px;
        text-align: right;
        font-size: 11.5px;
        font-weight: 500;
        color: var(--s-text3);
        font-variant-numeric: tabular-nums;
    }

    .seg {
        flex: 1 1 auto;
        display: flex;
        padding: 2px;
        border-radius: 8px;
        background: var(--s-field);
        border: 1px solid var(--s-line);
    }
    .seg button {
        flex: 1 1 0;
        height: 24px;
        border: none;
        border-radius: 6px;
        background: transparent;
        color: var(--s-text2);
        font: inherit;
        font-size: 11.5px;
        font-weight: 500;
        cursor: pointer;
    }
    .seg button.on {
        background: var(--s-bg);
        color: var(--s-text);
        box-shadow: 0 1px 3px rgba(0, 0, 0, 0.14);
    }

    /* ── Modals ───────────────────────────────────────────────────────── */

    .modal {
        position: fixed;
        top: 50%;
        left: 50%;
        z-index: 70;
        width: 340px;
        max-width: 92vw;
        padding: 22px;
        border-radius: 16px;
        background: var(--s-bg);
        box-shadow: var(--s-shadow);
        opacity: 0;
        pointer-events: none;
        transform: translate(-50%, -46%) scale(0.97);
        transition: opacity 0.16s ease, transform 0.16s ease;
    }
    body.modal-open .modal.shown {
        opacity: 1;
        pointer-events: auto;
        transform: translate(-50%, -50%) scale(1);
    }
    .modal h2 { font-size: 15.5px; font-weight: 600; letter-spacing: -0.01em; margin-bottom: 16px; }
    .modal.small { width: 320px; }
    .confirm-text { font-size: 13px; color: var(--s-text2); line-height: 1.45; margin-bottom: 18px; }

    .field { display: block; margin-bottom: 12px; }
    .field span {
        display: block;
        font-size: 11.5px;
        font-weight: 600;
        color: var(--s-text3);
        margin-bottom: 5px;
        letter-spacing: 0.01em;
    }
    .field input {
        width: 100%;
        height: 34px;
        padding: 0 11px;
        border: 1px solid var(--s-line);
        border-radius: 9px;
        background: var(--s-field);
        color: var(--s-text);
        font: inherit;
        font-size: 13px;
        outline: none;
    }
    .field input:focus { border-color: #007aff; box-shadow: 0 0 0 3px rgba(0, 122, 255, 0.16); }
    .field-error { font-size: 11.5px; color: #ff453a; min-height: 15px; margin-bottom: 4px; }

    .modal-actions { display: flex; justify-content: flex-end; gap: 8px; margin-top: 6px; }
    .modal-actions .btn { height: 32px; padding: 0 16px; }

    /* ── Tile context menu ────────────────────────────────────────────── */

    .tile-menu {
        position: fixed;
        z-index: 80;
        min-width: 190px;
        padding: 5px;
        border-radius: 12px;
        background: var(--s-bg);
        box-shadow: var(--s-shadow);
        display: none;
    }
    .tile-menu.open { display: block; }
    .tile-menu button {
        display: block;
        width: 100%;
        text-align: left;
        padding: 7px 10px;
        border: none;
        border-radius: 7px;
        background: transparent;
        color: var(--s-text);
        font: inherit;
        font-size: 12.5px;
        cursor: pointer;
    }
    .tile-menu button:hover { background: var(--s-hover); }
    .tile-menu button.danger { color: #ff453a; }
    .tile-menu hr { border: none; border-top: 1px solid var(--s-line); margin: 4px 6px; }

    .toast {
        position: fixed;
        left: 50%;
        bottom: 78px;
        z-index: 90;
        padding: 9px 16px;
        border-radius: 10px;
        background: rgba(28, 28, 30, 0.9);
        color: #fff;
        font-size: 12.5px;
        font-weight: 500;
        opacity: 0;
        pointer-events: none;
        transform: translate(-50%, 8px);
        transition: opacity 0.18s ease, transform 0.18s ease;
    }
    .toast.show { opacity: 1; transform: translate(-50%, 0); }

    /* The customize sheet docks beside the page rather than covering it, so
       shortcuts stay hoverable and draggable while it is open. */
    body { transition: padding-right 0.24s cubic-bezier(0.32, 0.72, 0, 1); }
    body.sheet-open { padding-right: var(--sheet-w); overflow: hidden; }
    body.modal-open { overflow: hidden; }
    body.sheet-open .fab { right: calc(var(--sheet-w) + 20px); }
    body.sheet-open .footer { padding-right: calc(var(--sheet-w) + 24px); }

    @media (max-width: 560px) {
        .clock { font-size: 46px; }
        .footer { display: none; }
        .fab { bottom: 18px; }
    }
</style>
</head>
<body class='__THEME__'>

<div class='wall' id='wall'></div>
<div class='wall-tint' id='wall-tint'></div>

<div class='stage'>
    <div class='date' id='date'></div>
    <div class='clock' id='clock'>--:--</div>

    <form class='search' id='search' method='GET'>
        <svg class='search-icon' viewBox='0 0 24 24'><circle cx='11' cy='11' r='6.5'/><path d='M16 16l4.5 4.5'/></svg>
        <input class='search-input' id='search-input' type='text' autocomplete='off'
               autocapitalize='off' spellcheck='false' />
        <button class='search-go' type='submit'>Search</button>
    </form>

    <div class='favorites' id='favorites'></div>
</div>

<div class='footer'>
    <div class='brand'>
        <svg viewBox='0 0 24 24'><path d='M12 3.2 5.2 6v5.3c0 4.35 2.9 7.6 6.8 9.5 3.9-1.9 6.8-5.15 6.8-9.5V6L12 3.2z'/></svg>
        Aura
    </div>
    <div class='shields-note'>
        <span class='dot'></span><span id='shields'>Aura Shields are active</span>
    </div>
</div>

<button class='fab' id='btn-customize' type='button' title='Customize this page'>
    <svg viewBox='0 0 24 24'><path d='M4 20h4l10-10a2.8 2.8 0 0 0-4-4L4 16v4z'/><path d='M13.5 6.5 17.5 10.5'/></svg>
    Customize
</button>

<aside class='sheet' id='sheet' role='dialog' aria-modal='true' aria-label='Customize this page'>
    <div class='sheet-head'>
        <div class='sheet-title'>Customize</div>
        <button class='icon-btn' id='sheet-close' type='button' title='Done'>
            <svg viewBox='0 0 24 24'><path d='M6 6l12 12M18 6L6 18'/></svg>
        </button>
    </div>
    <div class='sheet-body'>

        <section class='group'>
            <h2>Shortcuts</h2>
            <label class='switch'>
                <span>Show shortcuts</span>
                <input type='checkbox' id='opt-tiles' /><i></i>
            </label>
            <p class='hint'>Hover a shortcut and press its &times; to remove it, or drag shortcuts to put them in the order you like. Right-click one for more.</p>
            <button class='btn wide' id='btn-add-shortcut' type='button' style='margin-top:12px'>Add a shortcut</button>
        </section>

        <section class='group'>
            <h2>Wallpaper</h2>
            <div class='swatches' id='wall-swatches'></div>
            <div class='row'>
                <label class='colorwrap' title='Pick a colour'>
                    <input type='color' id='wall-color' value='#dbe8ff' />
                </label>
                <button class='btn' id='wall-pick' type='button'>Picture&hellip;</button>
                <button class='btn ghost' id='wall-none' type='button'>Default</button>
            </div>
            <div class='row'>
                <input class='text-input' id='wall-url' type='text' spellcheck='false' placeholder='Paste a picture address' />
                <button class='btn' id='wall-url-go' type='button'>Use</button>
            </div>
            <div class='slider'>
                <span>Dim</span>
                <input type='range' id='wall-dim' min='0' max='85' step='5' />
                <b id='wall-dim-v'>0%</b>
            </div>
            <div class='slider'>
                <span>Blur</span>
                <input type='range' id='wall-blur' min='0' max='40' step='2' />
                <b id='wall-blur-v'>0px</b>
            </div>
            <div class='slider'>
                <span>Text</span>
                <div class='seg' id='wall-text'>
                    <button type='button' data-value='auto'>Auto</button>
                    <button type='button' data-value='light'>Light</button>
                    <button type='button' data-value='dark'>Dark</button>
                </div>
            </div>
            <p class='hint' id='wall-name'></p>
        </section>

        <section class='group'>
            <h2>Layout</h2>
            <label class='switch'>
                <span>Show clock</span>
                <input type='checkbox' id='opt-clock' /><i></i>
            </label>
            <label class='switch'>
                <span>Show search box</span>
                <input type='checkbox' id='opt-search' /><i></i>
            </label>
        </section>

        <section class='group'>
            <button class='btn ghost wide danger' id='btn-reset' type='button'>Reset this page</button>
            <p class='hint'>Puts the wallpaper and layout back to how they started. Shortcuts are left alone.</p>
        </section>

    </div>
</aside>

<div class='modal-scrim' id='modal-scrim'></div>

<div class='modal' id='modal' role='dialog' aria-modal='true' aria-label='Shortcut'>
    <h2 id='modal-title'>Add shortcut</h2>
    <label class='field'>
        <span>Name</span>
        <input type='text' id='m-name' spellcheck='false' placeholder='Example' />
    </label>
    <label class='field'>
        <span>Address</span>
        <input type='text' id='m-url' spellcheck='false' placeholder='example.com' />
    </label>
    <p class='field-error' id='m-error'></p>
    <div class='modal-actions'>
        <button class='btn ghost' id='m-cancel' type='button'>Cancel</button>
        <button class='btn primary' id='m-save' type='button'>Save</button>
    </div>
</div>

<div class='modal small' id='confirm' role='dialog' aria-modal='true' aria-label='Please confirm'>
    <h2 id='confirm-title'>Remove</h2>
    <p class='confirm-text' id='confirm-text'></p>
    <div class='modal-actions'>
        <button class='btn ghost' id='c-cancel' type='button'>Cancel</button>
        <button class='btn danger-solid' id='c-ok' type='button'>Remove</button>
    </div>
</div>

<div class='tile-menu' id='tile-menu'>
    <button type='button' data-do='open'>Open</button>
    <button type='button' data-do='newtab'>Open in a new tab</button>
    <hr />
    <button type='button' data-do='edit'>Edit shortcut&hellip;</button>
    <button type='button' data-do='left'>Move left</button>
    <button type='button' data-do='right'>Move right</button>
    <hr />
    <button type='button' data-do='remove' class='danger'>Remove</button>
</div>

<div class='toast' id='toast'></div>

<script>
var AuraHome = (function () {
    'use strict';

    var state = null;
    var editIndex = -1;
    var dragFrom = -1;
    var dragLink = null;
    var dragMoved = false;
    var dragTarget = -1;
    var dragStartX = 0;
    var dragStartY = 0;
    var suppressClick = false;
    var menuIndex = -1;
    var confirmAction = null;
    var swatchesBuilt = false;

    function byId(id) { return document.getElementById(id); }

    function post(message) {
        try {
            if (window.chrome && window.chrome.webview) window.chrome.webview.postMessage(message);
        } catch (e) { }
    }

    function cssUrl(url) { return 'url(' + JSON.stringify(url) + ')'; }

    function pad(n) { return n < 10 ? '0' + n : '' + n; }

    function toast(text) {
        var el = byId('toast');
        el.textContent = text;
        el.classList.add('show');
        clearTimeout(toast.timer);
        toast.timer = setTimeout(function () { el.classList.remove('show'); }, 1800);
    }

    /* ── Clock ─────────────────────────────────────────────────────────── */

    function tick() {
        var now = new Date();
        byId('clock').textContent = pad(now.getHours()) + ':' + pad(now.getMinutes());
        byId('date').textContent = now.toLocaleDateString(undefined, {
            weekday: 'long', month: 'long', day: 'numeric'
        });
    }

    /* ── Wallpaper ─────────────────────────────────────────────────────── */

    function paintWallpaper() {
        var wall = byId('wall');
        var tint = byId('wall-tint');
        var w = state.wallpaper || {};

        wall.style.backgroundImage = 'none';
        wall.style.backgroundColor = 'transparent';
        wall.style.filter = 'none';
        wall.style.transform = 'none';

        if (w.type === 'image' && w.url) {
            wall.style.backgroundImage = cssUrl(w.url);
        } else if (w.type === 'gradient' && w.css) {
            wall.style.backgroundImage = w.css;
        } else if (w.type === 'color' && w.value) {
            wall.style.backgroundColor = w.value;
        }

        var blur = w.blur || 0;
        if (blur > 0 && w.type !== 'aura') {
            wall.style.filter = 'blur(' + blur + 'px)';
            wall.style.transform = 'scale(1.06)';
        }

        tint.style.background = (w.dim || 0) > 0
            ? 'rgba(0, 0, 0, ' + ((w.dim || 0) / 100) + ')'
            : 'transparent';

        document.body.classList.toggle('on-wall', w.text === 'light');
    }

    /* ── Shortcuts ─────────────────────────────────────────────────────── */

    function tileElement(favorite) {
        var tile = document.createElement('span');
        tile.className = 'tile';

        var letter = document.createElement('span');
        letter.className = 'tile-letter';
        letter.style.background = favorite.color || '#6e6e73';
        letter.textContent = favorite.letter || '?';
        tile.appendChild(letter);

        if (favorite.icon) {
            var img = document.createElement('img');
            img.className = 'tile-icon';
            img.alt = '';
            img.src = favorite.icon;
            img.addEventListener('load', function () { tile.classList.add('has-icon'); });
            tile.appendChild(img);
        }

        return tile;
    }

    function shortcutElement(favorite, index) {
        var link = document.createElement('a');
        link.className = 'fav';
        link.href = favorite.url;
        link.title = favorite.title + '\n' + favorite.url;
        link.setAttribute('data-index', index);

        link.appendChild(tileElement(favorite));

        var label = document.createElement('span');
        label.className = 'label';
        label.textContent = favorite.title;
        link.appendChild(label);

        var remove = document.createElement('button');
        remove.className = 'fav-del';
        remove.type = 'button';
        remove.title = 'Remove shortcut';
        remove.innerHTML = '<svg viewBox=""0 0 24 24""><path d=""M7 7l10 10M17 7L7 17""/></svg>';
        remove.addEventListener('click', function (e) {
            e.preventDefault();
            e.stopPropagation();
            askRemoveShortcut(index);
        });
        link.appendChild(remove);

        link.addEventListener('contextmenu', function (e) {
            e.preventDefault();
            openTileMenu(index, e.clientX, e.clientY);
        });

        link.addEventListener('mousedown', function (e) { beginDrag(e, link, index); });

        return link;
    }

    function addTileElement() {
        var link = document.createElement('a');
        link.className = 'fav add';
        link.href = '#';
        link.title = 'Add a shortcut';

        var tile = document.createElement('span');
        tile.className = 'tile';
        tile.innerHTML = '<svg viewBox=""0 0 24 24""><path d=""M12 6v12M6 12h12""/></svg>';
        link.appendChild(tile);

        var label = document.createElement('span');
        label.className = 'label';
        label.textContent = 'Add';
        link.appendChild(label);

        link.addEventListener('click', function (e) {
            e.preventDefault();
            openShortcutForm(-1);
        });

        return link;
    }

    function renderShortcuts() {
        var box = byId('favorites');
        var favorites = state.favorites || [];

        box.innerHTML = '';
        for (var i = 0; i < favorites.length; i++) {
            box.appendChild(shortcutElement(favorites[i], i));
        }
        box.appendChild(addTileElement());
    }

    /* ── Reordering shortcuts ──────────────────────────────────────────── */

    function clearDropMarks() {
        var marked = document.querySelectorAll('.drop-before, .drop-after');
        for (var i = 0; i < marked.length; i++) {
            marked[i].classList.remove('drop-before');
            marked[i].classList.remove('drop-after');
        }
    }

    function tileUnder(x, y) {
        var node = document.elementFromPoint(x, y);
        while (node && node !== document.body) {
            if (node.classList && node.classList.contains('fav')) return node;
            node = node.parentNode;
        }
        return null;
    }

    /**
     * Shortcuts are moved with plain pointer events rather than the browser's
     * own drag-and-drop, so a dropped tile lands exactly where the pointer
     * leaves it and a half-tile hover picks the right side.
     */
    function beginDrag(e, link, index) {
        if (e.button !== 0) return;
        if (e.target && e.target.closest && e.target.closest('.fav-del')) return;

        dragFrom = index;
        dragLink = link;
        dragMoved = false;
        dragTarget = -1;
        dragStartX = e.clientX;
        dragStartY = e.clientY;

        document.addEventListener('mousemove', onDragMove, true);
        document.addEventListener('mouseup', onDragEnd, true);
    }

    function onDragMove(e) {
        if (dragFrom < 0) return;

        if (!dragMoved) {
            var dx = e.clientX - dragStartX;
            var dy = e.clientY - dragStartY;
            if (dx * dx + dy * dy < 36) return;

            dragMoved = true;
            if (dragLink) dragLink.classList.add('dragging');
            document.body.classList.add('reordering');
        }

        e.preventDefault();
        clearDropMarks();
        dragTarget = -1;

        var over = tileUnder(e.clientX, e.clientY);
        if (!over || over === dragLink) return;

        var index = parseInt(over.getAttribute('data-index'), 10);
        // The Add tile stands for the end of the list.
        if (isNaN(index)) index = (state.favorites || []).length;

        var rect = over.getBoundingClientRect();
        var after = e.clientX > rect.left + rect.width / 2;
        over.classList.add(after ? 'drop-after' : 'drop-before');
        dragTarget = after ? index + 1 : index;
    }

    function onDragEnd() {
        document.removeEventListener('mousemove', onDragMove, true);
        document.removeEventListener('mouseup', onDragEnd, true);

        clearDropMarks();
        if (dragLink) dragLink.classList.remove('dragging');
        document.body.classList.remove('reordering');

        if (dragMoved) {
            // Swallow the click the browser is about to send the link.
            suppressClick = true;
            moveShortcut(dragFrom, dragTarget);
        }

        dragFrom = -1;
        dragLink = null;
        dragMoved = false;
        dragTarget = -1;
    }

    function moveShortcut(from, to) {
        if (from < 0 || to < 0) return;
        // Anywhere but the slot it already occupies is worth sending.
        if (to === from || to === from + 1) return;

        // The slot refers to the current order; the app works out the rest.
        post({ action: 'home_fav_move', from: from, to: to });
    }

    /* ── Tile menu ─────────────────────────────────────────────────────── */

    function openTileMenu(index, x, y) {
        var menu = byId('tile-menu');
        menuIndex = index;
        menu.classList.add('open');

        var width = menu.offsetWidth || 190;
        var height = menu.offsetHeight || 220;
        if (x + width > window.innerWidth - 8) x = window.innerWidth - width - 8;
        if (y + height > window.innerHeight - 8) y = Math.max(8, y - height);
        menu.style.left = Math.max(8, x) + 'px';
        menu.style.top = Math.max(8, y) + 'px';
    }

    function closeTileMenu() {
        byId('tile-menu').classList.remove('open');
        menuIndex = -1;
    }

    function runTileCommand(command) {
        var index = menuIndex;
        closeTileMenu();
        if (index < 0 || !state.favorites || !state.favorites[index]) return;

        var favorite = state.favorites[index];

        if (command === 'open') {
            window.location.href = favorite.url;
        } else if (command === 'newtab') {
            post({ action: 'home_open', url: favorite.url, newTab: true });
        } else if (command === 'edit') {
            openShortcutForm(index);
        } else if (command === 'left') {
            if (index > 0) post({ action: 'home_fav_move', from: index, to: index - 1 });
        } else if (command === 'right') {
            if (index < state.favorites.length - 1) post({ action: 'home_fav_move', from: index, to: index + 2 });
        } else if (command === 'remove') {
            askRemoveShortcut(index);
        }
    }

    /* ── Confirmation ──────────────────────────────────────────────────── */

    function openConfirm(title, text, okLabel, action) {
        byId('confirm-title').textContent = title;
        byId('confirm-text').textContent = text;
        byId('c-ok').textContent = okLabel;
        confirmAction = action;
        closeShortcutForm();
        byId('confirm').classList.add('shown');
        document.body.classList.add('modal-open');
    }

    function closeConfirm() {
        byId('confirm').classList.remove('shown');
        confirmAction = null;
        if (!byId('modal').classList.contains('shown')) document.body.classList.remove('modal-open');
    }

    function askRemoveShortcut(index) {
        var favorite = state.favorites[index];
        if (!favorite) return;

        openConfirm('Remove shortcut', 'Remove \u201c' + favorite.title + '\u201d from this page?',
            'Remove', function () {
                post({ action: 'home_fav_remove', url: favorite.url });
                toast('Shortcut removed');
            });
    }

    /* ── Add / edit shortcut ───────────────────────────────────────────── */

    function openShortcutForm(index) {
        editIndex = index;
        var favorite = index >= 0 ? state.favorites[index] : null;

        byId('modal-title').textContent = favorite ? 'Edit shortcut' : 'Add shortcut';
        byId('m-name').value = favorite ? favorite.title : '';
        byId('m-url').value = favorite ? favorite.url : '';
        byId('m-error').textContent = '';

        closeConfirm();
        byId('modal').classList.add('shown');
        document.body.classList.add('modal-open');
        setTimeout(function () { byId('m-name').focus(); }, 40);
    }

    function closeShortcutForm() {
        byId('modal').classList.remove('shown');
        editIndex = -1;
        if (!byId('confirm').classList.contains('shown')) document.body.classList.remove('modal-open');
    }

    function looksLikeAddress(text) {
        if (!text) return false;
        if (text.indexOf(' ') >= 0) return false;
        if (text.indexOf('.') < 0) return false;
        return true;
    }

    function saveShortcut() {
        var name = byId('m-name').value.trim();
        var url = byId('m-url').value.trim();

        if (!looksLikeAddress(url)) {
            byId('m-error').textContent = 'That does not look like a web address.';
            byId('m-url').focus();
            return;
        }

        if (editIndex >= 0 && state.favorites[editIndex]) {
            post({
                action: 'home_fav_edit',
                url: state.favorites[editIndex].url,
                title: name,
                newUrl: url
            });
            toast('Shortcut updated');
        } else {
            post({ action: 'home_fav_add', title: name, url: url });
            toast('Shortcut added');
        }

        closeShortcutForm();
    }

    /* ── Customize sheet ───────────────────────────────────────────────── */

    function openSheet() {
        document.body.classList.add('sheet-open');
    }

    function closeSheet() {
        document.body.classList.remove('sheet-open');
    }

    function buildSwatches() {
        var box = byId('wall-swatches');
        var presets = state.presets || {};

        box.innerHTML = '';

        var plain = document.createElement('button');
        plain.className = 'swatch default-swatch';
        plain.type = 'button';
        plain.title = 'Aura';
        plain.textContent = 'Aura';
        plain.setAttribute('data-kind', 'aura');
        plain.setAttribute('data-value', '');
        box.appendChild(plain);

        var picture = document.createElement('button');
        picture.className = 'swatch image-swatch';
        picture.type = 'button';
        picture.title = 'Your picture';
        picture.setAttribute('data-kind', 'image');
        picture.setAttribute('data-value', '');
        picture.innerHTML = '<svg viewBox=""0 0 24 24""><rect x=""3.5"" y=""5.5"" width=""17"" height=""13"" rx=""2.5""/>' +
            '<circle cx=""9"" cy=""10.5"" r=""1.6""/><path d=""M4.5 17l5-4.5 3.5 3 2.5-2 4 3.5""/></svg>';
        box.appendChild(picture);

        var gradients = presets.gradients || [];
        for (var i = 0; i < gradients.length; i++) {
            var g = document.createElement('button');
            g.className = 'swatch';
            g.type = 'button';
            g.title = gradients[i].name;
            g.style.background = gradients[i].css;
            g.setAttribute('data-kind', 'gradient');
            g.setAttribute('data-value', gradients[i].id);
            box.appendChild(g);
        }

        var colors = presets.colors || [];
        for (var c = 0; c < colors.length; c++) {
            var s = document.createElement('button');
            s.className = 'swatch';
            s.type = 'button';
            s.title = colors[c];
            s.style.background = colors[c];
            s.setAttribute('data-kind', 'color');
            s.setAttribute('data-value', colors[c]);
            box.appendChild(s);
        }

        swatchesBuilt = true;
    }

    function markSwatch() {
        var w = state.wallpaper || {};
        var all = byId('wall-swatches').querySelectorAll('.swatch');
        for (var i = 0; i < all.length; i++) {
            var kind = all[i].getAttribute('data-kind');
            var value = all[i].getAttribute('data-value');
            var on = kind === w.type && (kind === 'aura' || kind === 'image' || value === (w.value || '').toLowerCase());
            all[i].classList.toggle('on', !!on);
        }
    }

    function syncControls() {
        var w = state.wallpaper || {};

        byId('opt-tiles').checked = !!state.showTiles;
        byId('opt-clock').checked = !!state.showClock;
        byId('opt-search').checked = !!state.showSearch;

        byId('wall-dim').value = w.dim || 0;
        byId('wall-dim-v').textContent = (w.dim || 0) + '%';
        byId('wall-blur').value = w.blur || 0;
        byId('wall-blur-v').textContent = (w.blur || 0) + 'px';

        if (w.type === 'color' && w.value) byId('wall-color').value = w.value;

        var options = byId('wall-text').querySelectorAll('button');
        for (var i = 0; i < options.length; i++) {
            options[i].classList.toggle('on', options[i].getAttribute('data-value') === (state.wallpaperText || 'auto'));
        }

        byId('wall-name').textContent = 'Showing: ' + (w.name || 'Aura');
        markSwatch();
    }

    function setWallpaper(kind, value) {
        post({ action: 'set', key: 'wallpaperType', value: kind });
        if (value !== undefined && value !== null) {
            post({ action: 'set', key: 'wallpaperValue', value: value });
        }
    }

    /* ── Putting it together ───────────────────────────────────────────── */

    function applyLayout() {
        byId('favorites').style.display = state.showTiles ? '' : 'none';
        byId('clock').style.display = state.showClock ? '' : 'none';
        byId('date').style.display = state.showClock ? '' : 'none';
        byId('search').style.display = state.showSearch ? '' : 'none';

        var form = byId('search');
        form.action = state.searchAction || '';
        var input = byId('search-input');
        input.name = state.searchParam || 'q';
        input.placeholder = 'Search ' + (state.searchName || 'the web') + ' or enter a website';
    }

    function apply(next) {
        if (!next) return;
        state = next;

        document.body.classList.toggle('dark', !!state.dark);
        document.body.classList.toggle('light', !state.dark);

        byId('shields').textContent = state.shields || 'Aura Shields are active';

        if (!swatchesBuilt) buildSwatches();

        paintWallpaper();
        renderShortcuts();
        applyLayout();
        syncControls();

        if (state.notice) toast(state.notice);
    }

    /* ── Wiring ────────────────────────────────────────────────────────── */

    // Shortcuts are reordered by hand below, so the browser's own link drag
    // has to be turned off. Cancelling dragstart is the one place that always
    // sticks; without it the drag loop eats the mouse-up.
    document.addEventListener('dragstart', function (e) {
        var node = e.target;
        while (node && node !== document.body) {
            if (node.classList && node.classList.contains('fav')) {
                e.preventDefault();
                return;
            }
            node = node.parentNode;
        }
    }, true);

    byId('btn-customize').addEventListener('click', openSheet);    byId('sheet-close').addEventListener('click', closeSheet);

    byId('opt-tiles').addEventListener('change', function () {
        post({ action: 'set', key: 'showTiles', value: this.checked });
    });
    byId('opt-clock').addEventListener('change', function () {
        post({ action: 'set', key: 'showClock', value: this.checked });
    });
    byId('opt-search').addEventListener('change', function () {
        post({ action: 'set', key: 'showSearch', value: this.checked });
    });

    byId('btn-add-shortcut').addEventListener('click', function () {
        if (!state.showTiles) post({ action: 'set', key: 'showTiles', value: true });
        openShortcutForm(-1);
    });

    byId('wall-swatches').addEventListener('click', function (e) {
        var swatch = e.target.closest('.swatch');
        if (!swatch) return;

        var kind = swatch.getAttribute('data-kind');
        var value = swatch.getAttribute('data-value');

        if (kind === 'image') {
            post({ action: 'home_wallpaper_pick' });
            return;
        }
        if (kind === 'aura') {
            setWallpaper('aura', '');
            return;
        }
        setWallpaper(kind, value);
    });

    byId('wall-pick').addEventListener('click', function () {
        post({ action: 'home_wallpaper_pick' });
    });

    byId('wall-none').addEventListener('click', function () {
        setWallpaper('aura', '');
    });

    byId('wall-url-go').addEventListener('click', function () {
        var url = byId('wall-url').value.trim();
        if (!url) return;
        if (url.indexOf('http://') !== 0 && url.indexOf('https://') !== 0) {
            url = 'https://' + url;
        }
        byId('wall-url').value = '';
        setWallpaper('image', url);
    });

    byId('wall-color').addEventListener('change', function () {
        setWallpaper('color', this.value);
    });

    byId('wall-dim').addEventListener('input', function () {
        state.wallpaper.dim = parseInt(this.value, 10) || 0;
        byId('wall-dim-v').textContent = state.wallpaper.dim + '%';
        paintWallpaper();
    });
    byId('wall-dim').addEventListener('change', function () {
        post({ action: 'set', key: 'wallpaperDim', value: parseInt(this.value, 10) || 0 });
    });

    byId('wall-blur').addEventListener('input', function () {
        state.wallpaper.blur = parseInt(this.value, 10) || 0;
        byId('wall-blur-v').textContent = state.wallpaper.blur + 'px';
        paintWallpaper();
    });
    byId('wall-blur').addEventListener('change', function () {
        post({ action: 'set', key: 'wallpaperBlur', value: parseInt(this.value, 10) || 0 });
    });

    byId('wall-text').addEventListener('click', function (e) {
        var button = e.target.closest('button');
        if (!button) return;

        var mode = button.getAttribute('data-value');
        state.wallpaperText = mode;

        // Auto has to be resolved by the app, which knows the wallpaper.
        state.wallpaper.text = mode === 'auto'
            ? (state.wallpaperAuto || 'dark')
            : mode;

        post({ action: 'set', key: 'wallpaperText', value: mode });
        syncControls();
        paintWallpaper();
    });

    byId('btn-reset').addEventListener('click', function () {
        openConfirm('Reset this page', 'Put the wallpaper and layout back to how they started?', 'Reset',
            function () {
                post({ action: 'home_reset' });
                toast('Home page reset');
            });
    });

    byId('m-cancel').addEventListener('click', closeShortcutForm);
    byId('m-save').addEventListener('click', saveShortcut);
    byId('m-url').addEventListener('keydown', function (e) { if (e.key === 'Enter') saveShortcut(); });
    byId('m-name').addEventListener('keydown', function (e) { if (e.key === 'Enter') byId('m-url').focus(); });

    byId('c-cancel').addEventListener('click', closeConfirm);
    byId('c-ok').addEventListener('click', function () {
        var action = confirmAction;
        closeConfirm();
        if (action) action();
    });
    byId('modal-scrim').addEventListener('click', function () {
        if (byId('modal').classList.contains('shown')) closeShortcutForm();
        else closeConfirm();
    });

    byId('tile-menu').addEventListener('click', function (e) {
        var button = e.target.closest('button');
        if (button) runTileCommand(button.getAttribute('data-do'));
    });

    document.addEventListener('click', function (e) {
        // The click that follows a drag must not open the shortcut.
        if (suppressClick) {
            suppressClick = false;
            e.preventDefault();
            e.stopPropagation();
            return;
        }
        if (!e.target.closest('#tile-menu')) closeTileMenu();
    }, true);
    window.addEventListener('resize', closeTileMenu);
    window.addEventListener('blur', closeTileMenu);

    document.addEventListener('keydown', function (e) {
        if (e.key !== 'Escape') return;
        if (byId('tile-menu').classList.contains('open')) { closeTileMenu(); return; }
        if (byId('modal').classList.contains('shown')) { closeShortcutForm(); return; }
        if (byId('confirm').classList.contains('shown')) { closeConfirm(); return; }
        if (document.body.classList.contains('sheet-open')) { closeSheet(); return; }
    });

    /* ── Search box ────────────────────────────────────────────────────── */

    var form = byId('search');
    var input = byId('search-input');

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

    tick();
    setInterval(tick, 1000);

    return {
        apply: apply,

        /** Opens the customize sheet, used by the browser's own menu. */
        openCustomize: function () { openSheet(); },

        /** Short message from the app, for example after a shortcut is added. */
        toast: toast,

        setShields: function (text) { byId('shields').textContent = text; }
    };
})();

AuraHome.apply(__STATE__);
</script>
</body>
</html>";

            return html
                .Replace("__THEME__", dark ? "dark" : "light")
                .Replace("__STATE__", StateJson());
        }

        /// <summary>
        /// Everything the start page draws itself from, as one JSON object. The
        /// app pushes a fresh copy whenever a shortcut or an appearance setting
        /// changes, so the page never has to be reloaded to keep up.
        /// </summary>
        public static string StateJson()
        {
            var settings = Settings.Current;
            var favorites = new List<object>();

            foreach (var favorite in FavoritesStore.Current.GetFavorites())
            {
                if (favorite == null || string.IsNullOrEmpty(favorite.Url)) continue;

                favorites.Add(Json.Obj(
                    "title", string.IsNullOrEmpty(favorite.Title)
                        ? FavoritesStore.DisplayHost(favorite.Url) : favorite.Title,
                    "url", favorite.Url,
                    "letter", string.IsNullOrEmpty(favorite.Letter)
                        ? FavoritesStore.LetterFor(favorite.Title, favorite.Url) : favorite.Letter,
                    "color", string.IsNullOrEmpty(favorite.Color)
                        ? FavoritesStore.ColorFor(favorite.Url) : favorite.Color,
                    "icon", FavoritesStore.IconFor(favorite) ?? string.Empty));
            }

            var gradients = new List<object>();
            foreach (var preset in HomeWallpaper.Gradients)
            {
                gradients.Add(Json.Obj("id", preset.Id, "name", preset.Name, "css", preset.Css));
            }

            var colors = new List<object>();
            foreach (string color in HomeWallpaper.Colors) colors.Add(color);

            string type = settings.WallpaperType;
            string value = settings.WallpaperValue;

            var wallpaper = Json.Obj(
                "type", type,
                "value", value,
                "dim", settings.WallpaperDim,
                "blur", settings.WallpaperBlur,
                "url", type == "image" ? HomeWallpaper.ResolveImageUrl(value) : string.Empty,
                "css", HomeWallpaper.CssFor(type, value) ?? string.Empty,
                "name", HomeWallpaper.NameFor(type, value),
                "text", HomeWallpaper.TextMode());

            var engine = settings.Engine;

            var state = Json.Obj(
                "dark", Theme.IsDark,
                "showClock", settings.ShowClock,
                "showTiles", settings.ShowTiles,
                "showSearch", settings.ShowSearch,
                "wallpaperText", settings.WallpaperText,
                "wallpaperAuto", AutoTextMode(),
                "searchAction", engine.BaseUrl,
                "searchParam", engine.QueryParam,
                "searchName", engine.ShortName,
                "shields", ShieldsNote(),
                "favorites", favorites,
                "wallpaper", wallpaper,
                "presets", Json.Obj("gradients", gradients, "colors", colors));

            string json = Json.Write(state);
            if (json == null) return "{}";

            // Keep a "</script>" hiding in a page title from ending our script block.
            return json.Replace("<", "\\u003c");
        }

        /// <summary>What "Auto" text would pick right now, so the page can preview it.</summary>
        private static string AutoTextMode()
        {
            string forced = Settings.Current.WallpaperText;
            if (forced == "light" || forced == "dark") return forced;
            return HomeWallpaper.TextMode();
        }

        private static string ShieldsNote()
        {
            int blocked = AdBlocker.BlockedCount;
            return blocked > 0
                ? blocked.ToString("N0") + " trackers & ads blocked this session"
                : "Aura Shields are active";
        }
    }
}
