using System;
using System.Text.RegularExpressions;

namespace SimpleBrowser
{
    public static class AdBlocker
    {
        // Known tracking, analytics, and advertising domains / patterns
        private static readonly string[] AdDomainKeywords = new string[]
        {
            // Google Ads / DoubleClick
            "doubleclick.net",
            "googleadservices.com",
            "googlesyndication.com",
            "adservice.google.",
            "/pagead/",
            "/ads?",
            // YouTube ad endpoints and video ads
            "youtube.com/api/stats/ads",
            "youtube.com/pagead/",
            "youtube.com/ptracking",
            "youtube.com/get_midroll_info",
            "play.google.com/log",
            // General ad networks & trackers
            "adnxs.com",
            "advertising.com",
            "adsystem.com",
            "adroll.com",
            "scorecardresearch.com",
            "taboola.com",
            "outbrain.com",
            "criteo.com",
            "rubiconproject.com",
            "pubmatic.com",
            "openx.net",
            "casalemedia.com",
            "amazon-adsystem.com",
            "adservice.",
            "adserver.",
            "adskeeper.com",
            "popads.net",
            "popcash.net",
            "propellerads.com",
            "exoclick.com",
            "trafficjunky.com",
            "adblade.com",
            "revcontent.com",
            "mgid.com",
            "chartbeat.com",
            "hotjar.com",
            "quantserve.com",
            "moatads.com",
            "smartadserver.com",
            "exponential.com",
            "bidswitch.net",
            "contextweb.com",
            "lijit.com",
            "yieldmo.com",
            "zergnet.com"
        };

        public static int BlockedCount { get; private set; }

        public static bool ShouldBlock(string uri)
        {
            if (string.IsNullOrEmpty(uri)) return false;

            string lower = uri.ToLowerInvariant();

            for (int i = 0; i < AdDomainKeywords.Length; i++)
            {
                if (lower.Contains(AdDomainKeywords[i]))
                {
                    BlockedCount++;
                    return true;
                }
            }
            return false;
        }

        // Script injected at DocumentCreated to run before any page script
        public static string GetAdBlockInjectionScript()
        {
            return @"
(function() {
    'use strict';

    // 1. YouTube specific Ad Skipping & Ad Annihilation
    function handleYouTubeAds() {
        if (!location.hostname.includes('youtube.com')) return;

        // Skip video ads automatically and fast-forward
        setInterval(function() {
            try {
                // Click skip ad button if present
                var skipBtn = document.querySelector('.ytp-ad-skip-button, .ytp-ad-skip-button-modern, .ytp-skip-ad-button, .ytp-ad-preview-container-detached');
                if (skipBtn) {
                    skipBtn.click();
                }

                // If ad overlay or video ad is playing, fast-forward to the end
                var adElement = document.querySelector('.ad-showing, .ad-interrupting');
                var video = document.querySelector('video');
                if (adElement && video && !isNaN(video.duration)) {
                    video.currentTime = video.duration || 99999;
                    video.playbackRate = 16.0;
                    if (skipBtn) skipBtn.click();
                }

                // Remove ad banners, companion slots, promo cards
                var badSelectors = [
                    'ytd-promoted-sparkles-web-renderer',
                    'ytd-promoted-video-renderer',
                    'ytd-display-ad-renderer',
                    'ytd-banner-promo-renderer',
                    'ytd-in-feed-ad-layout-renderer',
                    'ytd-ad-slot-renderer',
                    '#player-ads',
                    '.ytp-ad-overlay-container',
                    '.ytp-ad-message-container',
                    'ytd-engagement-panel-section-list-renderer[target-id=""engagement-panel-ads""]',
                    'tp-yt-paper-dialog:has(#feedback)',
                    '#masthead-ad',
                    'ytd-rich-item-renderer:has(ytd-ad-slot-renderer)'
                ];
                for (var i = 0; i < badSelectors.length; i++) {
                    var els = document.querySelectorAll(badSelectors[i]);
                    for (var j = 0; j < els.length; j++) {
                        els[j].style.display = 'none';
                        if (els[j].parentNode) els[j].parentNode.removeChild(els[j]);
                    }
                }
            } catch(e) {}
        }, 150);
    }

    // 2. Generic Element Hiding for Ads
    function hideAdElements() {
        var commonAdSelectors = [
            'ins.adsbygoogle',
            'div[id^=""google_ads_""]',
            'div[class*=""google-ad""]',
            'div[class*=""ad-slot""]',
            'div[class*=""ad-banner""]',
            'div[id*=""taboola-""]',
            'div[id*=""outbrain""]',
            'div[class*=""outbrain""]',
            'div[data-ad-unit]',
            'div[aria-label=""advertisement""]',
            'div[data-ad-slot]'
        ];

        var style = document.createElement('style');
        style.type = 'text/css';
        style.innerHTML = commonAdSelectors.join(', ') + ' { display: none !important; width: 0 !important; height: 0 !important; visibility: hidden !important; }';
        if (document.head) {
            document.head.appendChild(style);
        } else {
            document.addEventListener('DOMContentLoaded', function() {
                if (document.head) document.head.appendChild(style);
            });
        }
    }

    // 3. Block window.open popups from shady ad networks
    var originalOpen = window.open;
    window.open = function(url) {
        if (!url || typeof url !== 'string') return null;
        var lower = url.toLowerCase();
        if (lower.includes('pop') || lower.includes('ad') || lower.includes('click') || lower.includes('track')) {
            console.log('[Brave Shields] Blocked popup: ' + url);
            return null;
        }
        return originalOpen.apply(this, arguments);
    };

    hideAdElements();
    handleYouTubeAds();
})();
";
        }
    }
}
