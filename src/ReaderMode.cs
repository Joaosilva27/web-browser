namespace SimpleBrowser
{
    /// <summary>
    /// Toge's reader view.
    ///
    /// One script, sent to the page when the reader is asked for. It finds the
    /// part of the page that reads like an article and lays a clean sheet over
    /// the top of it, inside a shadow root so the site's own styling cannot reach
    /// it. Running it again removes the sheet; the page itself is never changed,
    /// so putting it back is exact.
    ///
    /// The script returns 'shown', 'hidden' or 'none'.
    /// </summary>
    public static class ReaderMode
    {
        public static string Script(bool dark)
        {
            return "window.TogeReaderDark = " + (dark ? "true" : "false") + ";\n"
                 + "window.TogeReaderCss = \"" + Json.Escape(Css) + "\";\n"
                 + Js;
        }

        private const string Css = @"
:host {
    position: fixed !important;
    inset: 0 !important;
    z-index: 2147483646 !important;
    display: block !important;
    margin: 0 !important;
    padding: 0 !important;
    overflow-y: auto !important;
    overscroll-behavior: contain;
    outline: none !important;
    font-size: var(--r-size, 17px);
    line-height: 1.62;
    text-align: left;
    -webkit-font-smoothing: antialiased;
    --r-bg: #ffffff;
    --r-fg: #1d1d1f;
    --r-dim: #6e6e73;
    --r-line: #e6e6e9;
    --r-link: #0a6cff;
    background: var(--r-bg) !important;
    color: var(--r-fg) !important;
    font-family: Georgia, 'Iowan Old Style', 'Times New Roman', serif;
}
:host([data-theme='sepia']) { --r-bg: #f7f0dd; --r-fg: #3a3125; --r-dim: #7b6a51; --r-line: #e6dcc2; --r-link: #9a5a12; }
:host([data-theme='dark']) { --r-bg: #1b1b1d; --r-fg: #e9e9ec; --r-dim: #9b9ba3; --r-line: #34343a; --r-link: #5aa9ff; }

*, *::before, *::after { box-sizing: border-box; }

.r-bar {
    position: sticky;
    top: 0;
    z-index: 3;
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 12px;
    padding: 9px 14px;
    background: var(--r-bg);
    border-bottom: 1px solid var(--r-line);
    font-family: -apple-system, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif;
    font-size: 13px;
    line-height: 1.2;
}
.r-side { display: flex; align-items: center; gap: 6px; min-width: 0; }
.r-tag { font-size: 12.5px; font-weight: 600; letter-spacing: .01em; }
.r-site { font-size: 11.5px; color: var(--r-dim); margin-left: 3px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.r-btn {
    font: 500 12.5px/1 -apple-system, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif;
    color: var(--r-fg);
    background: transparent;
    border: 1px solid var(--r-line);
    border-radius: 7px;
    padding: 7px 10px;
    cursor: pointer;
    transition: background .12s ease, opacity .12s ease;
    white-space: nowrap;
}
.r-btn:hover { background: rgba(128, 128, 128, .16); }
.r-btn.r-done { background: var(--r-fg); color: var(--r-bg); border-color: transparent; }

.r-body { max-width: 43em; margin: 0 auto; padding: 42px 26px 120px; }
.r-title { font-size: 1.95em; line-height: 1.22; font-weight: 700; letter-spacing: -.015em; margin: 0 0 .45em; }
.r-meta {
    font-family: -apple-system, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif;
    font-size: .72em;
    letter-spacing: .09em;
    text-transform: uppercase;
    color: var(--r-dim);
    padding-bottom: 1.6em;
    margin-bottom: 1.7em;
    border-bottom: 1px solid var(--r-line);
}

.r-content p { margin: 0 0 1.15em; }
.r-content h1, .r-content h2, .r-content h3, .r-content h4, .r-content h5 {
    line-height: 1.28;
    letter-spacing: -.01em;
    margin: 1.7em 0 .55em;
}
.r-content h1 { font-size: 1.5em; }
.r-content h2 { font-size: 1.28em; }
.r-content h3 { font-size: 1.12em; }
.r-content a { color: var(--r-link); text-decoration: none; }
.r-content a:hover { text-decoration: underline; }
.r-content img, .r-content video, .r-content svg { max-width: 100% !important; height: auto !important; }
.r-content img { display: block; margin: 1.5em auto; border-radius: 6px; }
.r-content figure { margin: 1.6em 0; }
.r-content figcaption { font-size: .82em; color: var(--r-dim); text-align: center; margin-top: .55em; }
.r-content blockquote {
    margin: 1.4em 0;
    padding: 0 0 0 1.1em;
    border-left: 3px solid var(--r-line);
    color: var(--r-dim);
}
.r-content ul, .r-content ol { margin: 0 0 1.2em; padding-left: 1.5em; }
.r-content li { margin: .35em 0; }
.r-content pre {
    margin: 1.3em 0;
    padding: 13px 15px;
    overflow-x: auto;
    border-radius: 8px;
    background: rgba(128, 128, 128, .14);
    line-height: 1.5;
}
.r-content code {
    font-family: ui-monospace, SFMono-Regular, Consolas, 'Courier New', monospace;
    font-size: .87em;
    background: rgba(128, 128, 128, .14);
    padding: .12em .34em;
    border-radius: 4px;
}
.r-content pre code { background: none; padding: 0; font-size: 1em; }
.r-content table { max-width: 100%; border-collapse: collapse; margin: 1.3em 0; font-size: .93em; }
.r-content th, .r-content td { border: 1px solid var(--r-line); padding: 7px 10px; text-align: left; }
.r-content hr { border: 0; border-top: 1px solid var(--r-line); margin: 2em 0; }
";

        private const string Js = @"(function () {
    var ID = 'toge-reader';
    var H = window.TogeReader || (window.TogeReader = {});

    if (!H.ready) {
        H.ready = true;
        H.id = ID;
        H.size = 17;
        H.theme = null;
        H.byId = function (id) { return document.getElementById(id); };

        H.close = function () {
            var root = H.byId(ID);
            if (root && root.parentNode) root.parentNode.removeChild(root);

            var lock = H.byId(ID + '-lock');
            if (lock && lock.parentNode) lock.parentNode.removeChild(lock);

            if (document.documentElement) document.documentElement.classList.remove('toge-reader-open');

            document.removeEventListener('click', H.block, true);
            document.removeEventListener('keydown', H.key, true);
        };

        H.act = function (what) {
            var root = H.byId(ID);
            if (!root) return;

            if (what === 'done') { H.close(); return; }

            if (what === 'smaller' || what === 'bigger') {
                H.size += (what === 'bigger' ? 1 : -1);
                if (H.size < 13) H.size = 13;
                if (H.size > 26) H.size = 26;
                root.style.setProperty('--r-size', H.size + 'px');
                return;
            }

            if (what === 'theme') {
                var order = ['light', 'sepia', 'dark'];
                var now = root.getAttribute('data-theme') || 'light';
                var at = order.indexOf(now);
                H.theme = order[(at + 1) % order.length];
                root.setAttribute('data-theme', H.theme);
            }
        };

        H.block = function (e) {
            var root = H.byId(ID);
            if (!root) return;

            var path = e.composedPath ? e.composedPath() : [e.target];
            for (var i = 0; i < path.length; i++) {
                if (path[i] === root) return;
            }

            e.stopPropagation();
            e.preventDefault();
        };

        H.key = function (e) {
            if (e.key === 'Escape' || e.keyCode === 27) H.close();
        };

        H.toast = function (text) {
            var el = H.byId(ID + '-toast');
            if (!el) {
                el = document.createElement('div');
                el.id = ID + '-toast';
                el.style.cssText = 'position:fixed;left:50%;bottom:30px;transform:translateX(-50%);z-index:2147483647;'
                    + 'background:rgba(28,28,30,.94);color:#fff;padding:10px 16px;border-radius:10px;'
                    + 'font:500 13px/1.35 -apple-system,Segoe UI,Roboto,sans-serif;letter-spacing:.01em;'
                    + 'box-shadow:0 10px 30px rgba(0,0,0,.3);pointer-events:none;opacity:0;'
                    + 'transition:opacity .18s ease;';
                document.body.appendChild(el);
            }

            el.textContent = text;
            el.style.opacity = '1';
            if (H.toastTimer) clearTimeout(H.toastTimer);
            H.toastTimer = setTimeout(function () { el.style.opacity = '0'; }, 1900);
        };
    }

    if (H.byId(ID)) { H.close(); return 'hidden'; }

    if (!H.theme) H.theme = window.TogeReaderDark ? 'dark' : 'light';

    function readerButton(act, label, title) {
        var button = document.createElement('button');
        button.className = 'r-btn' + (act === 'done' ? ' r-done' : '');
        button.setAttribute('data-act', act);
        button.setAttribute('title', title);
        button.setAttribute('type', 'button');
        button.textContent = label;
        return button;
    }

    /*
     * The page's own title, minus the site's name: a tab reading
     * Safari - Wikipedia becomes a reader heading reading just Safari.
     */
    function pageTitle() {
        var raw = (document.title || '').replace(/\s+/g, ' ').trim();
        if (!raw) return '';

        var host = (location.hostname || '').replace(/^www\./i, '');
        var parts = host.split('.');
        var brand = (parts.length > 1 ? parts[parts.length - 2] : parts[0] || '').toLowerCase();

        var seps = [' | ', ' - ', ' \u2014 ', ' \u2013 ', ' \u00B7 ', ' :: ', ' > '];
        for (var i = 0; i < seps.length; i++) {
            var at = raw.lastIndexOf(seps[i]);
            if (at < 4) continue;

            var tail = raw.substring(at + seps[i].length).toLowerCase();
            if ((brand && tail.indexOf(brand) >= 0) || tail.length < 16) {
                return raw.substring(0, at).trim();
            }
        }

        return raw;
    }

    /* ---------- finding the article ---------- */

    var JUNK = 'script, style, noscript, iframe, form, input, textarea, select, button, nav, aside, footer, '
        + '[role=navigation], [role=banner], [role=contentinfo], [aria-hidden=true], [hidden], '
        + '.nav, .navbar, .menu, .sidebar, .comments, .comment, .share, .sharing, .social, '
        + '.ad, .ads, .advert, .advertisement, .newsletter, .subscribe, .promo, '
        + '.related, .recommend, .breadcrumb, .cookie, .popup, .modal, .toolbar, .widget, '
        + '.navbox, .infobox, .metadata, .toc, #toc, .table-of-contents, .mw-editsection, '
        + '.mw-references-wrap, .reflist, .mw-empty-elt, sup.reference, .mw-cite-backlink, '
        + '.noprint, .skip-link, .screen-reader-text, .visually-hidden';

    function isJunk(el) {
        if (!el || !el.matches) return false;
        try { return el.matches(JUNK); } catch (e) { return false; }
    }

    function textLength(node) {
        return (node.textContent || '').replace(/\s+/g, ' ').trim().length;
    }

    function linkDensity(node) {
        var length = textLength(node);
        if (length < 1) return 1;

        var links = node.getElementsByTagName('a');
        var linkText = 0;
        for (var i = 0; i < links.length; i++) {
            linkText += (links[i].textContent || '').length;
        }

        return Math.min(linkText / length, 1);
    }

    function score(node) {
        if (!node) return -1;

        var length = textLength(node);
        if (length < 200) return -1;

        var paragraphs = node.getElementsByTagName('p').length;
        return length * (1 - linkDensity(node) * 1.4) + paragraphs * 25;
    }

    /*
     * Keeps walking down while a single child holds nearly all of the text and
     * carries fewer links than its parent. A page shell that opens with a
     * navigation column gives way to the column that actually holds the writing.
     */
    function descend(node) {
        for (var hop = 0; hop < 8; hop++) {
            var total = textLength(node);
            if (total < 600) break;

            var parentDensity = linkDensity(node);
            var kids = node.children;
            var winner = null;
            var winnerShare = 0;

            for (var i = 0; i < kids.length; i++) {
                var kid = kids[i];
                if (isJunk(kid)) continue;

                var share = textLength(kid) / total;
                if (share < 0.6 || share <= winnerShare) continue;
                if (linkDensity(kid) > parentDensity) continue;

                winner = kid;
                winnerShare = share;
            }

            if (!winner) break;
            node = winner;
        }

        return node;
    }

    var best = null;
    var bestScore = 0;

    var candidates = document.querySelectorAll(
        'article, main, [role=main], [itemtype*=""Article""], .post, .entry, .entry-content, '
        + '.post-content, .article-body, .story, .story-body, #content, #main, .content');

    for (var c = 0; c < candidates.length; c++) {
        var s = score(candidates[c]);
        if (s > bestScore) { bestScore = s; best = candidates[c]; }
    }

    if (!best && document.body) {
        var blocks = document.body.children;
        for (var b = 0; b < blocks.length; b++) {
            var s2 = score(blocks[b]);
            if (s2 > bestScore) { bestScore = s2; best = blocks[b]; }
        }
    }

    if (!best) best = document.body;
    if (!best) { H.toast('Reader is not available on this page.'); return 'none'; }

    best = descend(best);

    /* ---------- a cleaned copy of it ---------- */

    var clone = best.cloneNode(true);

    var junk = clone.querySelectorAll(JUNK);

    for (var d = junk.length - 1; d >= 0; d--) {
        if (junk[d].parentNode) junk[d].parentNode.removeChild(junk[d]);
    }

    var all = clone.getElementsByTagName('*');
    for (var a = all.length - 1; a >= 0; a--) {
        var el = all[a];
        var attrs = el.attributes;
        for (var t = attrs.length - 1; t >= 0; t--) {
            var name = attrs[t].name || '';
            if (name.length > 2 && name.substring(0, 2).toLowerCase() === 'on') {
                el.removeAttribute(attrs[t].name);
            }
        }

        var inline = (el.getAttribute('style') || '').toLowerCase().replace(/\s/g, '');
        if (!inline) continue;

        if (inline.indexOf('display:none') >= 0 || inline.indexOf('visibility:hidden') >= 0) {
            if (el.parentNode) el.parentNode.removeChild(el);
            continue;
        }

        if (inline.indexOf('position:fixed') >= 0 || inline.indexOf('position:absolute') >= 0
            || inline.indexOf('position:sticky') >= 0 || inline.indexOf('z-index') >= 0) {
            el.removeAttribute('style');
        }
    }

    var text = (clone.textContent || '').replace(/\s+/g, ' ').trim();
    if (text.length < 400) { H.toast('Reader is not available on this page.'); return 'none'; }

    /* ---------- the title ---------- */

    var title = '';
    var heading = clone.querySelector('h1');
    if (heading) {
        title = (heading.textContent || '').replace(/\s+/g, ' ').trim();
        if (heading.parentNode) heading.parentNode.removeChild(heading);
    }

    if (!title) title = pageTitle();
    if (!title) title = location.hostname || 'Reader';

    /* ---------- the sheet ---------- */

    var host = document.createElement('div');
    host.id = ID;
    host.setAttribute('data-theme', H.theme);
    host.setAttribute('tabindex', '-1');
    host.style.setProperty('--r-size', H.size + 'px');

    var shadow = host.attachShadow({ mode: 'open' });

    var style = document.createElement('style');
    style.textContent = window.TogeReaderCss || '';

    var head = document.createElement('div');
    head.className = 'r-bar';

    var left = document.createElement('div');
    left.className = 'r-side';

    var tag = document.createElement('span');
    tag.className = 'r-tag';
    tag.textContent = 'Reader';
    left.appendChild(tag);

    var site = document.createElement('span');
    site.className = 'r-site';
    site.textContent = location.hostname || 'Toge';
    left.appendChild(site);

    var right = document.createElement('div');
    right.className = 'r-side';

    right.appendChild(readerButton('smaller', 'A\u2212', 'Smaller text'));
    right.appendChild(readerButton('bigger', 'A+', 'Larger text'));
    right.appendChild(readerButton('theme', 'Theme', 'Change the reader theme'));
    right.appendChild(readerButton('done', 'Done', 'Close the reader'));

    head.appendChild(left);
    head.appendChild(right);

    var body = document.createElement('div');
    body.className = 'r-body';

    var titleEl = document.createElement('h1');
    titleEl.className = 'r-title';
    titleEl.textContent = title;

    var meta = document.createElement('div');
    meta.className = 'r-meta';
    meta.textContent = (location.hostname || 'Reader') + '  \u00B7  Reader View';

    var content = document.createElement('div');
    content.className = 'r-content';
    content.appendChild(clone);

    body.appendChild(titleEl);
    body.appendChild(meta);
    body.appendChild(content);

    shadow.appendChild(style);
    shadow.appendChild(head);
    shadow.appendChild(body);

    document.body.appendChild(host);

    /* The sheet scrolls on its own, so the page behind it should not keep a
       scrollbar of its own next to it. */
    var lock = document.createElement('style');
    lock.id = ID + '-lock';
    lock.textContent = 'html.toge-reader-open, html.toge-reader-open body { overflow: hidden !important; }';
    document.head.appendChild(lock);
    document.documentElement.classList.add('toge-reader-open');

    shadow.addEventListener('click', function (e) {
        var button = e.target && e.target.closest ? e.target.closest('[data-act]') : null;
        if (!button) return;
        e.preventDefault();
        e.stopPropagation();
        H.act(button.getAttribute('data-act'));
    });

    document.addEventListener('click', H.block, true);
    document.addEventListener('keydown', H.key, true);

    try { host.focus({ preventScroll: true }); } catch (ignored) { }

    return 'shown';
})();";
    }
}
