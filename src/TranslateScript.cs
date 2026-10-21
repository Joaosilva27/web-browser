namespace SimpleBrowser
{
    /// <summary>
    /// The page-side half of Translate. It reports the document's language so
    /// the host can ask, walks the text when the host says go, draws the small
    /// ask / translated bubble, and puts the translations back where they came
    /// from. Injected once per document; all of it sleeps until the host calls.
    ///
    /// The CSS lives in one namespaced style block, so a page's own rules are
    /// never touched.
    /// </summary>
    internal static class TranslateScript
    {
        public const string Source = @"(function () {
    if (window.top !== window) { return; }
    if (window.__togeTranslate) { return; }

    var nodes = null;
    var bubble = null;
    var reported = '';

    function post(text) {
        try { chrome.webview.postMessage(text); } catch (e) { }
    }

    function pageLanguage() {
        var el = document.documentElement;
        var code = el ? el.getAttribute('lang') : null;
        if (!code) {
            var metas = document.querySelectorAll('meta');
            for (var i = 0; i < metas.length; i++) {
                var http = metas[i].getAttribute('http-equiv');
                if (http && http.toLowerCase() === 'content-language') {
                    code = metas[i].getAttribute('content');
                }
            }
        }
        if (!code) { return ''; }

        code = code.toLowerCase().replace(/_/g, '-');
        var dash = code.indexOf('-');
        if (dash > 0) { code = code.substring(0, dash); }
        return code;
    }

    /* One report per language per document: the host asks at most once. */
    function reportLanguage() {
        var code = pageLanguage();
        if (!code || code === reported) { return; }
        reported = code;
        post('toge-lang:' + code);
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', reportLanguage);
    } else {
        reportLanguage();
    }
    window.addEventListener('load', reportLanguage);

    function ignored(tag) {
        return tag === 'SCRIPT' || tag === 'STYLE' || tag === 'NOSCRIPT'
            || tag === 'TEXTAREA' || tag === 'INPUT' || tag === 'SELECT'
            || tag === 'OPTION' || tag === 'CODE' || tag === 'PRE'
            || tag === 'SVG' || tag === 'IFRAME' || tag === 'CANVAS'
            || tag === 'VIDEO' || tag === 'AUDIO';
    }

    function hiddenByAncestor(el) {
        while (el && el !== document.body) {
            if (ignored(el.tagName) || el.isContentEditable) { return true; }
            el = el.parentNode;
        }
        return false;
    }

    /* Walks every visible run of text and sends the words to the host. The
       separators are the same character Toge uses to split messages back. */
    function collect() {
        if (!document.body) { return; }

        var found = [];
        var walker = document.createTreeWalker(document.body, NodeFilter.SHOW_TEXT, null, false);
        var node;
        while ((node = walker.nextNode()) && found.length < 400) {
            var raw = node.nodeValue;
            if (!raw || raw.length < 2 || !raw.replace(/\s/g, '')) { continue; }

            var parent = node.parentNode;
            if (!parent || hiddenByAncestor(parent)) { continue; }
            found.push(node);
        }

        if (!found.length) { return; }

        nodes = [];
        var payload = [];
        for (var i = 0; i < found.length; i++) {
            var text = found[i].nodeValue;
            var lead = text.match(/^\s*/)[0];
            var tail = text.match(/\s*$/)[0];
            var core = text.substring(lead.length, text.length - tail.length);
            if (!core) { continue; }

            nodes.push({ node: found[i], raw: text, lead: lead, tail: tail });
            payload.push(core);
        }

        if (!payload.length) { nodes = null; return; }
        post('toge-tx:' + payload.join('\u0001'));
    }

    function apply(indexes, texts) {
        if (!nodes) { return; }
        for (var k = 0; k < indexes.length; k++) {
            var item = nodes[indexes[k]];
            if (!item || !item.node) { continue; }
            var text = texts[k];
            if (text === null || text === undefined || text === '') { continue; }
            item.node.nodeValue = item.lead + text + item.tail;
        }
    }

    function restore() {
        if (nodes) {
            for (var i = 0; i < nodes.length; i++) {
                if (nodes[i].node) { nodes[i].node.nodeValue = nodes[i].raw; }
            }
        }
        nodes = null;
        closeBubble();
    }

    /* ── The bubble ─────────────────────────────────────────────────────── */

    function ensureStyle() {
        if (document.getElementById('toge-tx-style')) { return; }
        var style = document.createElement('style');
        style.id = 'toge-tx-style';
        style.textContent = '#toge-tx { position: fixed; top: 14px; right: 14px; z-index: 2147483647; width: 330px; box-sizing: border-box; padding: 16px 18px 14px; background: #ffffff; color: #1f2328; border: 1px solid rgba(0, 0, 0, 0.12); border-radius: 14px; box-shadow: 0 18px 46px rgba(0, 0, 0, 0.26); font-family: Segoe UI, Arial, sans-serif; font-size: 13px; line-height: 1.45; text-align: left; }'
            + '#toge-tx .tg-title { font-size: 14px; font-weight: 600; margin: 0 18px 2px 0; }'
            + '#toge-tx .tg-sub { color: #57606a; margin: 0 0 12px; }'
            + '#toge-tx .tg-go { display: inline-block; border: 0; border-radius: 8px; background: #fb542b; color: #ffffff; font-family: inherit; font-size: 13px; font-weight: 600; padding: 8px 16px; cursor: pointer; }'
            + '#toge-tx .tg-go:hover { background: #e2491f; }'
            + '#toge-tx .tg-act { display: block; margin-top: 9px; color: #57606a; font-size: 12.5px; text-decoration: none; cursor: pointer; }'
            + '#toge-tx .tg-act:hover { color: #1f2328; text-decoration: underline; }'
            + '#toge-tx .tg-x { position: absolute; top: 7px; right: 9px; border: 0; background: none; color: #7c8590; font-size: 18px; line-height: 1; padding: 4px; cursor: pointer; }'
            + '#toge-tx .tg-x:hover { color: #1f2328; }';

        var head = document.head || document.documentElement;
        if (head) { head.appendChild(style); }
    }

    function closeBubble() {
        if (bubble && bubble.parentNode) { bubble.parentNode.removeChild(bubble); }
        bubble = null;
    }

    function el(tag, cls, text) {
        var node = document.createElement(tag);
        if (cls) { node.className = cls; }
        if (text) { node.textContent = text; }
        return node;
    }

    function action(label, name, close) {
        var link = el('a', 'tg-act', label);
        link.href = '#';
        link.addEventListener('click', function (e) {
            e.preventDefault();
            e.stopPropagation();
            if (close) { closeBubble(); }
            post('toge-bubble:' + name);
        });
        return link;
    }

    function primary(label, name) {
        var button = el('button', 'tg-go', label);
        button.type = 'button';
        button.addEventListener('click', function (e) {
            e.preventDefault();
            e.stopPropagation();
            closeBubble();
            post('toge-bubble:' + name);
        });
        return button;
    }

    function open() {
        closeBubble();
        ensureStyle();
        bubble = document.createElement('div');
        bubble.id = 'toge-tx';

        var x = el('button', 'tg-x', '\u00d7');
        x.type = 'button';
        x.title = 'Close';
        x.addEventListener('click', function (e) {
            e.preventDefault();
            closeBubble();
            post('toge-bubble:close');
        });
        bubble.appendChild(x);

        (document.body || document.documentElement).appendChild(bubble);
    }

    function ask(language, target) {
        open();
        bubble.appendChild(el('div', 'tg-title', 'Translate this page?'));
        bubble.appendChild(el('div', 'tg-sub',
            'This page is in ' + language + '. Translate it to ' + target + '?'));
        bubble.appendChild(primary('Translate', 'translate'));
        bubble.appendChild(action('Always translate non-English pages', 'always', false));
        bubble.appendChild(action('Never ask again', 'never', false));
    }

    function done(target) {
        open();
        bubble.appendChild(el('div', 'tg-title', 'Translated to ' + target));
        bubble.appendChild(el('div', 'tg-sub', 'The text on this page is now translated.'));
        bubble.appendChild(primary('Show original page', 'original'));
    }

    window.__togeTranslate = {
        collect: collect,
        apply: apply,
        restore: restore,
        ask: ask,
        done: done,
        close: closeBubble
    };
})();";
    }
}
