using System;

namespace SimpleBrowser
{
    /// <summary>
    /// Find on page, as Aura's own bar rather than Chromium's.
    ///
    /// The bar lives in a shadow root pinned to the top-right of the page, so
    /// the page's styles cannot reach it and it cannot leak styles back.
    /// Matches are found by walking the page's text nodes and shown by selecting
    /// the range, which highlights the text without touching the page.
    ///
    /// Running the script again closes the bar, so the host only has to toggle.
    /// </summary>
    public static class FindBar
    {
        public static string Script()
        {
            return Js;
        }

        private const string Js = @"(function () {
    if (window.AuraFind && window.AuraFind.host) { window.AuraFind.close(); return 'closed'; }

    var host = document.createElement('div');
    host.setAttribute('data-aura-find', '1');
    host.style.cssText = 'all: initial; position: fixed; top: 12px; right: 16px; z-index: 2147483646;';

    var shadow = host.attachShadow({ mode: 'open' });

    var style = document.createElement('style');
    style.textContent = [
        '.bar { display: flex; align-items: center; gap: 2px; padding: 6px 6px 6px 11px;',
        '  background: #2c2c2e; color: #f5f5f7; border-radius: 10px;',
        '  box-shadow: 0 6px 24px rgba(0,0,0,.38), 0 0 0 .5px rgba(255,255,255,.10);',
        '  font-family: -apple-system, Segoe UI, system-ui, sans-serif; font-size: 13px; }',
        'input.f { width: 190px; border: none; outline: none; background: transparent; color: inherit;',
        '  font: inherit; padding: 3px 2px; }',
        'input.f::placeholder { color: rgba(245,245,247,.45); }',
        '.count { min-width: 62px; text-align: right; padding: 0 6px; font-size: 11.5px;',
        '  font-variant-numeric: tabular-nums; color: rgba(245,245,247,.65); }',
        '.btn { width: 26px; height: 26px; flex: none; border: none; border-radius: 6px; background: transparent;',
        '  color: inherit; cursor: pointer; font-size: 14px; line-height: 1; display: grid; place-items: center; }',
        '.btn:hover:not(:disabled) { background: rgba(255,255,255,.14); }',
        '.btn:disabled { opacity: .3; cursor: default; }'
    ].join('\n');

    var bar = document.createElement('div');
    bar.className = 'bar';

    var input = document.createElement('input');
    input.className = 'f';
    input.type = 'text';
    input.placeholder = 'Find on page';
    input.setAttribute('spellcheck', 'false');
    input.setAttribute('autocomplete', 'off');

    var count = document.createElement('span');
    count.className = 'count';

    var up = document.createElement('button');
    up.className = 'btn';
    up.title = 'Previous match';
    up.textContent = '\u2191';

    var down = document.createElement('button');
    down.className = 'btn';
    down.title = 'Next match';
    down.textContent = '\u2193';

    // Named closeBtn rather than close: close() is the function that tears the
    // bar down, and a second `close` would replace it with this button, leaving
    // the bar with no way out.
    var closeBtn = document.createElement('button');
    closeBtn.className = 'btn';
    closeBtn.title = 'Close';
    closeBtn.textContent = '\u2715';
    closeBtn.style.marginLeft = '4px';

    bar.appendChild(input);
    bar.appendChild(count);
    bar.appendChild(down);
    bar.appendChild(up);
    bar.appendChild(closeBtn);
    shadow.appendChild(style);
    shadow.appendChild(bar);

    var matches = [];
    var index = -1;
    var query = '';

    function collect(text) {
        matches = [];
        index = -1;
        if (!text) { return; }

        var needle = text.toLowerCase();
        var walker = document.createTreeWalker(document.body, NodeFilter.SHOW_TEXT, null, false);
        var node;
        while ((node = walker.nextNode())) {
            if (matches.length >= 2000) { return; }

            var value = node.nodeValue;
            if (!value) { continue; }

            var parent = node.parentNode;
            if (!parent) { continue; }

            var tag = parent.nodeName;
            if (tag === 'SCRIPT' || tag === 'STYLE' || tag === 'NOSCRIPT' || tag === 'TEXTAREA') { continue; }

            var lower = value.toLowerCase();
            var at = lower.indexOf(needle);
            while (at >= 0) {
                if (matches.length >= 2000) { return; }
                try {
                    var range = document.createRange();
                    range.setStart(node, at);
                    range.setEnd(node, at + needle.length);
                    matches.push(range);
                } catch (e) { }
                at = lower.indexOf(needle, at + needle.length);
            }
        }
    }

    function paint() {
        if (!query) {
            count.textContent = '';
        } else if (!matches.length) {
            count.textContent = 'No results';
        } else {
            count.textContent = (index + 1) + ' of ' + matches.length;
        }
        up.disabled = !matches.length;
        down.disabled = !matches.length;
    }

    // Matching is shown with the browser's own highlight registry rather than
    // by taking the document selection. Taking the selection would move the
    // caret out of this bar, and the second letter typed would land in the page.
    var HIGHLIGHTS = 'aura-find';
    var CURRENT = 'aura-find-current';

    function highlightSupported() {
        return !!(window.CSS && CSS.highlights && typeof Highlight === 'function');
    }

    function ensureHighlightStyle() {
        if (document.getElementById('aura-find-style')) { return; }

        var sheet = document.createElement('style');
        sheet.id = 'aura-find-style';
        sheet.textContent =
            '::highlight(aura-find) { background-color: rgba(255, 214, 0, .32); }' +
            '::highlight(aura-find-current) { background-color: #ffb100; color: #14161a; }';

        (document.head || document.documentElement).appendChild(sheet);
    }

    function paintMatches() {
        if (!highlightSupported()) { return false; }

        try {
            ensureHighlightStyle();

            var all = new Highlight();
            for (var i = 0; i < matches.length; i++) { all.add(matches[i]); }
            CSS.highlights.set(HIGHLIGHTS, all);

            if (index >= 0 && index < matches.length) {
                CSS.highlights.set(CURRENT, new Highlight(matches[index]));
            } else {
                CSS.highlights['delete'](CURRENT);
            }

            return true;
        } catch (e) {
            return false;
        }
    }

    function clearMatches() {
        try {
            if (highlightSupported()) {
                CSS.highlights['delete'](HIGHLIGHTS);
                CSS.highlights['delete'](CURRENT);
            }
        } catch (e) { }
    }

    function focusMatch(i) {
        if (!matches.length) { clearMatches(); paint(); return; }

        if (i < 0) { i = matches.length - 1; }
        if (i >= matches.length) { i = 0; }
        index = i;

        var range = matches[index];

        if (!paintMatches()) {
            // An engine without the highlight registry falls back to selecting
            // the match, with the caret handed straight back to the bar.
            try {
                var selection = window.getSelection();
                if (selection) { selection.removeAllRanges(); selection.addRange(range); }
            } catch (e) { }

            try {
                if (shadow.activeElement !== input) { input.focus(); }
            } catch (e) { }
        }

        try {
            var rect = range.getBoundingClientRect();
            if (rect && (rect.top < 70 || rect.bottom > window.innerHeight - 70)) {
                window.scrollBy(0, rect.top - window.innerHeight / 2.4);
            }
        } catch (e) { }

        paint();
    }

    function search(text) {
        query = text;
        collect(text);
        if (matches.length) { focusMatch(0); } else { clearMatches(); paint(); }
    }

    function step(delta) {
        if (!matches.length) { return; }
        focusMatch(index + delta);
    }

    function close() {
        clearMatches();
        if (host.parentNode) { host.parentNode.removeChild(host); }
        if (window.AuraFind) { window.AuraFind.host = null; }
    }

    input.addEventListener('input', function () { search(input.value); });

    input.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') {
            e.preventDefault();
            close();
        } else if (e.key === 'Enter') {
            e.preventDefault();
            step(e.shiftKey ? -1 : 1);
        } else if (e.key === 'ArrowDown' || e.key === 'F3') {
            e.preventDefault();
            step(1);
        } else if (e.key === 'ArrowUp') {
            e.preventDefault();
            step(-1);
        }
        e.stopPropagation();
    }, true);

    up.addEventListener('mousedown', function (e) { e.preventDefault(); });
    down.addEventListener('mousedown', function (e) { e.preventDefault(); });
    up.addEventListener('click', function () { step(-1); input.focus(); });
    down.addEventListener('click', function () { step(1); input.focus(); });
    closeBtn.addEventListener('click', function () { close(); });

    var seed = '';
    try {
        var selection = window.getSelection();
        if (selection) { seed = String(selection.toString()).trim(); }
    } catch (e) { }
    if (seed.length > 0 && seed.length <= 120 && seed.indexOf('\n') < 0) { input.value = seed; }

    document.documentElement.appendChild(host);
    window.AuraFind = { host: host, close: close, input: input };

    input.focus();
    input.select();
    if (input.value) { search(input.value); } else { paint(); }

    return 'shown';
})();";
    }
}
