using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace SimpleBrowser
{
    /// <summary>
    /// Ad / tracker blocking engine.
    ///
    /// Filter sources, in order of preference:
    ///   1. Bundled lists next to the executable  (bin\filters\*.adblock)
    ///   2. Bundled lists in the source tree       (scripts\*.adblock)
    ///   3. Downloaded + cached lists              (%APPDATA%\AuraBrowser\filters\*.txt)
    ///
    /// Supported filter syntax (the useful subset of Adblock Plus / uBlock Origin):
    ///   ! comment, [Adblock Plus 2.0]
    ///   ||example.com^        hostname anchored
    ///   /ads/*.gif            regular expression / wildcard pattern
    ///   @@||example.com^      exception (allow) rule
    ///   $ domain=, third-party / 3p, first-party / 1p, ~third-party, script, image,
    ///     stylesheet, subdocument, document, xmlhttprequest / xhr, media, font, other,
    ///     and ~negations of any of those.
    /// Rules using options we cannot honour (redirect, csp, removeparam, ...) are skipped
    /// rather than misapplied.
    /// </summary>
    public static class AdBlocker
    {
        private const int DownloadTimeoutMs = 45000;
        private const int RemoteRefreshDays = 5;
        private const int MaxPatternRules = 60000;

        private static readonly object _lock = new object();

        private static readonly HashSet<string> _blockHosts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> _allowHosts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, List<PatternRule>> _blockByPrefix = new Dictionary<string, List<PatternRule>>(StringComparer.Ordinal);
        private static readonly List<PatternRule> _blockNoToken = new List<PatternRule>();
        private static readonly Dictionary<string, List<PatternRule>> _allowByPrefix = new Dictionary<string, List<PatternRule>>(StringComparer.Ordinal);
        private static readonly List<PatternRule> _allowNoToken = new List<PatternRule>();
        private static readonly HashSet<string> _loadedSources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, int> _blockedByHost = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        private static int _blockedCount;
        private static int _blockRuleCount;
        private static long _version;
        private static volatile bool _loaded;
        private static int _initStarted;

        private static readonly string[] RemoteLists = new string[]
        {
            "https://easylist.to/easylist/easylist.txt",
            "https://easylist.to/easylist/easyprivacy.txt"
        };

        // Fallback coverage so blocking works even if no list file ships and the network is down.
        private static readonly string[] BuiltInHosts = new string[]
        {
            "doubleclick.net", "googlesyndication.com", "googleadservices.com", "adservice.google.com",
            "google-analytics.com", "googletagservices.com", "googletagmanager.com", "adnxs.com",
            "adsystem.com", "amazon-adsystem.com", "adsafeprotected.com", "moatads.com", "adroll.com",
            "criteo.com", "criteo.net", "taboola.com", "outbrain.com", "revcontent.com", "mgid.com",
            "zergnet.com", "sharethrough.com", "teads.tv", "smartadserver.com", "adform.net",
            "casalemedia.com", "pubmatic.com", "rubiconproject.com", "magnite.com", "openx.net",
            "indexexchange.com", "appnexus.com", "xandr.com", "bidswitch.net", "contextweb.com",
            "exponential.com", "lijit.com", "yieldmo.com", "sovrn.com", "gumgum.com", "33across.com",
            "spotxchange.com", "spotx.tv", "freewheel.tv", "tremorhub.com", "telaria.com",
            "adcolony.com", "unityads.unity3d.com", "applovin.com", "chartboost.com", "inmobi.com",
            "mopub.com", "vungle.com", "ironsrc.com", "supersonicads.com", "tapjoy.com",
            "propellerads.com", "popads.net", "popcash.net", "adcash.com", "exoclick.com",
            "trafficjunky.com", "juicyads.com", "plugrush.com", "trafficstars.com", "hilltopads.net",
            "adsterra.com", "clickadu.com", "mgid.com", "adblade.com", "adskeeper.com", "bidvertiser.com",
            "scorecardresearch.com", "comscore.com", "quantserve.com", "quantcount.com", "chartbeat.com",
            "chartbeat.net", "hotjar.com", "mouseflow.com", "luckyorange.com", "clarity.ms",
            "fullstory.com", "inspectlet.com", "crazyegg.com", "clicktale.net", "smartlook.com",
            "matomo.cloud", "statcounter.com", "histats.com", "newrelic.com", "segment.io",
            "segment.com", "mixpanel.com", "amplitude.com", "heapanalytics.com", "kissmetrics.com",
            "bugsnag.com", "sentry-cdn.com", "sentry.io", "pixel.facebook.com", "connect.facebook.net",
            "ads-twitter.com", "ads-api.twitter.com", "static.ads-twitter.com", "ads.linkedin.com",
            "ads.pinterest.com", "trk.pinterest.com", "analytics.tiktok.com", "ads.tiktok.com",
            "ads.youtube.com", "youtube.com/api/stats/ads", "youtube.com/pagead", "youtube.com/ptracking",
            "samsungads.com", "iadsdk.apple.com", "api-adservices.apple.com", "adtech.yahooinc.com",
            "ads.yahoo.com", "gemini.yahoo.com", "adfox.yandex.ru", "an.yandex.ru", "adsfs.oppomobile.com",
            "ad.xiaomi.com", "tracking.miui.com", "ads.linkedin.com", "tags.tiqcdn.com",
            "bluekai.com", "demdex.net", "omtrdc.net", "2o7.net", "everesttech.net", "adobedtm.com",
            "krxd.net", "rlcdn.com", "crwdcntrl.net", "agkn.com", "mathtag.com", "simpli.fi",
            "tapad.com", "addthis.com", "sharethis.com", "disqusads.com", "po.st", "ampproject.org/v0/amp-ad",
            "adligature.com", "adnium.com", "adsupply.com", "trafficfactory.biz", "zeropark.com",
            "onclickads.net", "onclckds.com", "popmyads.com", "adf.ly", "shorte.st", "adfoc.us"
        };

        // =====================================================================
        //  Public API
        // =====================================================================

        public static int BlockedCount
        {
            get { return _blockedCount; }
        }

        public static bool IsLoaded
        {
            get { return _loaded; }
        }

        public static int RuleCount
        {
            get
            {
                lock (_lock)
                {
                    return _blockHosts.Count + _blockRuleCount;
                }
            }
        }

        public static void ResetCount()
        {
            Interlocked.Exchange(ref _blockedCount, 0);
            lock (_lock)
            {
                _blockedByHost.Clear();
            }
        }

        /// <summary>Snapshot of blocked requests grouped by hostname, for the Shields panel.</summary>
        public static Dictionary<string, int> GetBlockedByHost()
        {
            lock (_lock)
            {
                return new Dictionary<string, int>(_blockedByHost, StringComparer.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Kicks off list loading on a background thread. Safe to call repeatedly; only the
        /// first call does work. Returns immediately so browser startup is never blocked.
        /// </summary>
        public static void Initialize()
        {
            if (Interlocked.CompareExchange(ref _initStarted, 1, 0) != 0)
            {
                return;
            }

            Thread worker = new Thread(LoadEverything);
            worker.IsBackground = true;
            worker.Name = "AuraShields";
            worker.Start();
        }

        public static bool ShouldBlock(string uri)
        {
            return ShouldBlock(uri, null, null);
        }

        /// <summary>
        /// Decides whether a network request should be cancelled.
        /// </summary>
        /// <param name="uri">The request URL.</param>
        /// <param name="topLevelUri">URL of the page the user is on (for domain / party rules). May be null.</param>
        /// <param name="resourceType">script, image, stylesheet, document, subdocument, xhr, media, font, other.</param>
        public static bool ShouldBlock(string uri, string topLevelUri, string resourceType)
        {
            if (string.IsNullOrEmpty(uri))
            {
                return false;
            }

            if (uri.StartsWith("data:", StringComparison.OrdinalIgnoreCase) ||
                uri.StartsWith("blob:", StringComparison.OrdinalIgnoreCase) ||
                uri.StartsWith("about:", StringComparison.OrdinalIgnoreCase) ||
                uri.StartsWith("chrome-extension:", StringComparison.OrdinalIgnoreCase) ||
                uri.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            Uri parsed;
            if (!Uri.TryCreate(uri, UriKind.Absolute, out parsed))
            {
                return false;
            }

            string scheme = parsed.Scheme;
            if (!string.Equals(scheme, "http", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(scheme, "https", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(scheme, "ws", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(scheme, "wss", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string host = parsed.Host;
            if (string.IsNullOrEmpty(host))
            {
                return false;
            }

            // The top-level document is never blocked. Blocking it would replace the page
            // the user asked for with an error screen.
            if (!string.IsNullOrEmpty(topLevelUri) && UrlsEqual(uri, topLevelUri))
            {
                return false;
            }

            string topHost = null;
            if (!string.IsNullOrEmpty(topLevelUri))
            {
                Uri top;
                if (Uri.TryCreate(topLevelUri, UriKind.Absolute, out top))
                {
                    topHost = top.Host;
                }
            }

            bool thirdParty = IsThirdParty(host, topHost);
            string type = NormalizeType(resourceType);

            bool blocked = false;

            lock (_lock)
            {
                if (_allowHosts.Count > 0 && MatchesHostChain(host, _allowHosts))
                {
                    return false;
                }
                if (MatchesPatternIndex(_allowByPrefix, _allowNoToken, uri, host, topHost, thirdParty, type))
                {
                    return false;
                }

                if (MatchesHostChain(host, _blockHosts))
                {
                    blocked = true;
                }
                else if (MatchesPatternIndex(_blockByPrefix, _blockNoToken, uri, host, topHost, thirdParty, type))
                {
                    blocked = true;
                }

                if (blocked)
                {
                    int total = Interlocked.Increment(ref _blockedCount);
                    RecordBlockedHost(host);

                    // Keep the map from growing without bound on long browsing sessions.
                    if (total % 512 == 0 && _blockedByHost.Count > 500)
                    {
                        TrimBlockedHostMap();
                    }
                }
            }

            return blocked;
        }

        /// <summary>
        /// Matches pattern rules through a 3-character prefix index instead of scanning every
        /// rule. A rule's token is always a substring of any URL it can match, so every
        /// candidate is reachable from some 3-gram of the URL. The version stamp stops a rule
        /// that appears in several buckets from being evaluated twice.
        /// </summary>
        private static bool MatchesPatternIndex(
            Dictionary<string, List<PatternRule>> byPrefix,
            List<PatternRule> noToken,
            string uri, string host, string topHost, bool thirdParty, string type)
        {
            long version = ++_version;

            for (int i = 0; i < noToken.Count; i++)
            {
                PatternRule rule = noToken[i];
                if (rule.CheckVersion == version)
                {
                    continue;
                }
                rule.CheckVersion = version;
                if (rule.Matches(uri, host, topHost, thirdParty, type))
                {
                    return true;
                }
            }

            if (byPrefix.Count == 0)
            {
                return false;
            }

            string lower = uri.ToLowerInvariant();
            int length = lower.Length;

            for (int i = 0; i + 2 < length; i++)
            {
                List<PatternRule> bucket;
                if (!byPrefix.TryGetValue(lower.Substring(i, 3), out bucket))
                {
                    continue;
                }

                for (int j = 0; j < bucket.Count; j++)
                {
                    PatternRule rule = bucket[j];
                    if (rule.CheckVersion == version)
                    {
                        continue;
                    }
                    rule.CheckVersion = version;
                    if (rule.Matches(uri, host, topHost, thirdParty, type))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static void RecordBlockedHost(string host)
        {
            int existing;
            if (_blockedByHost.TryGetValue(host, out existing))
            {
                _blockedByHost[host] = existing + 1;
            }
            else
            {
                _blockedByHost[host] = 1;
            }
        }

        private static void TrimBlockedHostMap()
        {
            List<KeyValuePair<string, int>> items = new List<KeyValuePair<string, int>>(_blockedByHost);
            items.Sort(delegate (KeyValuePair<string, int> a, KeyValuePair<string, int> b)
            {
                return b.Value.CompareTo(a.Value);
            });

            _blockedByHost.Clear();
            int keep = Math.Min(250, items.Count);
            for (int i = 0; i < keep; i++)
            {
                _blockedByHost[items[i].Key] = items[i].Value;
            }
        }

        // =====================================================================
        //  List loading
        // =====================================================================

        private static void LoadEverything()
        {
            try
            {
                lock (_lock)
                {
                    foreach (string h in BuiltInHosts)
                    {
                        AddBlockHost(h);
                    }
                }

                // 1 + 2: bundled lists shipped with the app.
                foreach (string dir in GetBundledDirectories())
                {
                    try
                    {
                        if (!Directory.Exists(dir))
                        {
                            continue;
                        }
                        foreach (string file in Directory.GetFiles(dir, "*.adblock"))
                        {
                            LoadFile(file);
                        }
                        foreach (string file in Directory.GetFiles(dir, "*.txt"))
                        {
                            LoadFile(file);
                        }
                    }
                    catch
                    {
                        // A missing or unreadable folder must never break the browser.
                    }
                }

                // 3: cached remote lists, refreshed in the background.
                foreach (string url in RemoteLists)
                {
                    try
                    {
                        string path = EnsureRemoteList(url);
                        if (!string.IsNullOrEmpty(path))
                        {
                            LoadFile(path);
                        }
                    }
                    catch
                    {
                        // Offline is fine - we still have bundled + built-in rules.
                    }
                }

                Log("AdBlocker ready: " + RuleCount + " rules (" +
                    _blockHosts.Count + " hosts, " + _blockRuleCount + " patterns, " +
                    _blockNoToken.Count + " untokenized)");
            }
            catch (Exception ex)
            {
                Log("AdBlocker load failed: " + ex);
            }
            finally
            {
                _loaded = true;
            }
        }

        private static IEnumerable<string> GetBundledDirectories()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            List<string> dirs = new List<string>();

            dirs.Add(Path.Combine(baseDir, "filters"));
            dirs.Add(Path.Combine(baseDir, "scripts"));

            try
            {
                DirectoryInfo parent = Directory.GetParent(baseDir);
                if (parent != null)
                {
                    dirs.Add(Path.Combine(parent.FullName, "scripts"));
                }
            }
            catch
            {
            }

            return dirs;
        }

        private static string GetCacheDirectory()
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "AuraBrowser",
                "filters");

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            return dir;
        }

        private static string EnsureRemoteList(string url)
        {
            string fileName = url.Substring(url.LastIndexOf('/') + 1);
            string path = Path.Combine(GetCacheDirectory(), fileName);

            bool needsDownload = true;
            if (File.Exists(path))
            {
                TimeSpan age = DateTime.UtcNow - File.GetLastWriteTimeUtc(path);
                needsDownload = age.TotalDays > RemoteRefreshDays;
            }

            if (needsDownload)
            {
                try
                {
                    string body = HttpGet(url);
                    if (!string.IsNullOrEmpty(body) && body.Length > 1024)
                    {
                        File.WriteAllText(path, body, Encoding.UTF8);
                        Log("Downloaded " + fileName + " (" + body.Length + " bytes)");
                    }
                }
                catch (Exception ex)
                {
                    Log("Download failed for " + url + ": " + ex.Message);
                }
            }

            return File.Exists(path) ? path : null;
        }

        private static string HttpGet(string url)
        {
            // .NET Framework 4.0 predates SecurityProtocolType.Tls12, so use its numeric value
            // to make sure https filter-list hosts can still be reached.
            try
            {
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)(768 | 3072);
            }
            catch
            {
            }

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.UserAgent = "AuraBrowser/1.0 (+adblock)";
            request.Timeout = DownloadTimeoutMs;
            request.ReadWriteTimeout = DownloadTimeoutMs;
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }

        private static void LoadFile(string path)
        {
            lock (_lock)
            {
                if (!_loadedSources.Add(path))
                {
                    return;
                }
            }

            string text;
            try
            {
                text = File.ReadAllText(path, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Log("Could not read " + path + ": " + ex.Message);
                return;
            }

            int applied = 0;
            string[] lines = text.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0)
                {
                    continue;
                }
                if (ParseRule(line))
                {
                    applied++;
                }
            }

            Log("Loaded " + Path.GetFileName(path) + ": " + applied + " rules");
        }

        // =====================================================================
        //  Filter syntax parsing
        // =====================================================================

        private static bool ParseRule(string line)
        {
            char first = line[0];

            if (first == '!' || first == '[')
            {
                return false;   // comment or [Adblock Plus 2.0] header
            }

            if (line.IndexOf("##") >= 0 || line.IndexOf("#@#") >= 0 ||
                line.IndexOf("#?#") >= 0 || line.IndexOf("#$#") >= 0)
            {
                return false;   // cosmetic rule: handled by the injected stylesheet instead
            }

            bool isException = false;
            if (line.StartsWith("@@", StringComparison.Ordinal))
            {
                isException = true;
                line = line.Substring(2);
            }

            string pattern = line;
            string options = null;

            int dollar = line.IndexOf('$');
            if (dollar >= 0)
            {
                pattern = line.Substring(0, dollar);
                options = line.Substring(dollar + 1);
                if (options.Length == 0)
                {
                    options = null;
                }
            }

            // "/regex/" form, but only when the whole pattern is wrapped in slashes.
            // (A rule like "/pagead/js/x.js" is a literal path, not a regex.)
            if (pattern.Length >= 2 && pattern[0] == '/' && pattern[pattern.Length - 1] == '/')
            {
                return AddRegexRule(pattern.Substring(1, pattern.Length - 2), options, isException);
            }

            if (pattern.Length == 0)
            {
                return false;
            }

            // Fast path: "||example.com^" (and multi-label variants) become plain host lookups.
            if (pattern.StartsWith("||", StringComparison.Ordinal) && options == null)
            {
                string body = pattern.Substring(2);
                if (body.EndsWith("^", StringComparison.Ordinal))
                {
                    body = body.Substring(0, body.Length - 1);
                }

                if (body.Length > 0 && IsPlainHostname(body))
                {
                    lock (_lock)
                    {
                        if (isException)
                        {
                            _allowHosts.Add(body);
                        }
                        else
                        {
                            AddBlockHost(body);
                        }
                    }
                    return true;
                }
            }

            return AddRegexRule(pattern, options, isException);
        }

        private static bool IsPlainHostname(string s)
        {
            if (s.IndexOf('*') >= 0 || s.IndexOf('/') >= 0 || s.IndexOf('^') >= 0 ||
                s.IndexOf('|') >= 0 || s.IndexOf('?') >= 0 || s.IndexOf('=') >= 0)
            {
                return false;
            }
            if (s.Length == 0 || s[0] == '.' || s[s.Length - 1] == '.')
            {
                return false;
            }
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                bool ok = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') ||
                          (c >= '0' && c <= '9') || c == '.' || c == '-' || c == '_';
                if (!ok)
                {
                    return false;
                }
            }
            return s.IndexOf('.') >= 0;
        }

        private static void AddBlockHost(string host)
        {
            if (!string.IsNullOrEmpty(host))
            {
                _blockHosts.Add(host.TrimStart('.'));
            }
        }

        private static bool AddRegexRule(string pattern, string options, bool isException)
        {
            PatternRule rule;
            if (!TryBuildRule(pattern, options, out rule))
            {
                return false;
            }

            lock (_lock)
            {
                Dictionary<string, List<PatternRule>> index = isException ? _allowByPrefix : _blockByPrefix;
                List<PatternRule> noToken = isException ? _allowNoToken : _blockNoToken;

                if (!isException)
                {
                    if (_blockRuleCount >= MaxPatternRules)
                    {
                        return false;
                    }
                    _blockRuleCount++;
                }

                if (string.IsNullOrEmpty(rule.TokenPrefix))
                {
                    noToken.Add(rule);
                }
                else
                {
                    List<PatternRule> bucket;
                    if (!index.TryGetValue(rule.TokenPrefix, out bucket))
                    {
                        bucket = new List<PatternRule>();
                        index[rule.TokenPrefix] = bucket;
                    }
                    bucket.Add(rule);
                }
            }
            return true;
        }

        private static readonly HashSet<string> KnownTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "script", "image", "stylesheet", "css", "object", "xmlhttprequest", "xhr",
            "subdocument", "document", "doc", "media", "font", "other", "popup",
            "ping", "beacon", "websocket", "webrtc", "csp_report"
        };

        private static readonly HashSet<string> UnsupportedOptions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "redirect", "redirect-rule", "csp", "removeparam", "replace", "rewrite",
            "inline-script", "inline-font", "empty", "mp4", "genericblock",
            "generichide", "elemhide", "specifichide", "badfilter", "header"
        };

        private static bool TryBuildRule(string pattern, string options, out PatternRule rule)
        {
            rule = null;

            if (string.IsNullOrEmpty(pattern) || pattern == "*")
            {
                pattern = "*";
            }

            bool thirdPartyOnly = false;
            bool firstPartyOnly = false;
            HashSet<string> includeDomains = null;
            List<string> excludeDomains = null;
            HashSet<string> types = null;
            HashSet<string> excludeTypes = null;

            if (!string.IsNullOrEmpty(options))
            {
                string[] parts = options.Split(',');
                for (int i = 0; i < parts.Length; i++)
                {
                    string opt = parts[i].Trim();
                    if (opt.Length == 0)
                    {
                        continue;
                    }

                    if (opt.StartsWith("domain=", StringComparison.OrdinalIgnoreCase))
                    {
                        string value = opt.Substring(7);
                        string[] domains = value.Split('|');
                        for (int d = 0; d < domains.Length; d++)
                        {
                            string dom = domains[d].Trim().ToLowerInvariant();
                            if (dom.Length == 0)
                            {
                                continue;
                            }
                            if (dom[0] == '~')
                            {
                                if (excludeDomains == null) excludeDomains = new List<string>();
                                excludeDomains.Add(dom.Substring(1).TrimStart('.'));
                            }
                            else
                            {
                                if (includeDomains == null) includeDomains = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                                includeDomains.Add(dom.TrimStart('.'));
                            }
                        }
                        continue;
                    }

                    bool negated = opt[0] == '~';
                    string name = (negated ? opt.Substring(1) : opt).ToLowerInvariant();

                    if (name == "third-party" || name == "3p")
                    {
                        if (negated) firstPartyOnly = true; else thirdPartyOnly = true;
                        continue;
                    }
                    if (name == "first-party" || name == "1p")
                    {
                        if (negated) thirdPartyOnly = true; else firstPartyOnly = true;
                        continue;
                    }
                    if (KnownTypes.Contains(name))
                    {
                        if (negated)
                        {
                            if (excludeTypes == null) excludeTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                            excludeTypes.Add(NormalizeType(name));
                        }
                        else
                        {
                            if (types == null) types = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                            types.Add(NormalizeType(name));
                        }
                        continue;
                    }
                    if (UnsupportedOptions.Contains(name))
                    {
                        return false;   // do not misapply rules we cannot honour
                    }
                    // Unknown option: ignore it and keep the rule.
                }
            }

            string body = pattern;
            string prefix = "";

            if (body.StartsWith("||", StringComparison.Ordinal))
            {
                body = body.Substring(2);
                // Match the hostname itself or any subdomain of it.
                prefix = "^(?:[a-z][a-z0-9+\\-.]*:)?//(?:[^/?#@]*\\.)?";
            }
            else if (body.StartsWith("|", StringComparison.Ordinal))
            {
                body = body.Substring(1);
                prefix = "^";
            }

            bool anchorEnd = false;
            if (body.EndsWith("|", StringComparison.Ordinal))
            {
                body = body.Substring(0, body.Length - 1);
                anchorEnd = true;
            }

            StringBuilder rx = new StringBuilder(prefix);

            for (int i = 0; i < body.Length; i++)
            {
                char c = body[i];
                if (c == '*')
                {
                    rx.Append(".*");
                }
                else if (c == '^')
                {
                    rx.Append("(?:[^a-zA-Z0-9_\\-.%]|$)");
                }
                else
                {
                    rx.Append(Regex.Escape(c.ToString()));
                }
            }

            if (anchorEnd)
            {
                rx.Append("$");
            }

            Regex compiled;
            try
            {
                compiled = new Regex(rx.ToString(), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            }
            catch
            {
                return false;
            }

            rule = new PatternRule();
            rule.Regex = compiled;
            rule.Token = ExtractToken(body);
            rule.TokenPrefix = rule.Token.Length >= 3
                ? rule.Token.Substring(0, 3).ToLowerInvariant()
                : "";
            rule.ThirdPartyOnly = thirdPartyOnly;
            rule.FirstPartyOnly = firstPartyOnly;
            rule.IncludeDomains = includeDomains;
            rule.ExcludeDomains = excludeDomains;
            rule.Types = types;
            rule.ExcludeTypes = excludeTypes;
            return true;
        }

        /// <summary>Longest alphanumeric run in the pattern, used as a cheap substring pre-filter.</summary>
        private static string ExtractToken(string pattern)
        {
            string best = "";
            StringBuilder current = new StringBuilder();

            for (int i = 0; i < pattern.Length; i++)
            {
                char c = pattern[i];
                bool ok = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') ||
                          (c >= '0' && c <= '9') || c == '.' || c == '-' || c == '_';
                if (ok)
                {
                    current.Append(c);
                }
                else
                {
                    if (current.Length > best.Length)
                    {
                        best = current.ToString();
                    }
                    current.Length = 0;
                }
            }
            if (current.Length > best.Length)
            {
                best = current.ToString();
            }

            if (best.Length < 3)
            {
                return "";
            }
            return best.Trim('.');
        }

        // =====================================================================
        //  Matching helpers
        // =====================================================================

        private static bool MatchesHostChain(string host, HashSet<string> set)
        {
            if (set.Count == 0)
            {
                return false;
            }

            string candidate = host;
            while (true)
            {
                if (set.Contains(candidate))
                {
                    return true;
                }

                int dot = candidate.IndexOf('.');
                if (dot < 0)
                {
                    return false;
                }
                candidate = candidate.Substring(dot + 1);
                if (candidate.Length == 0)
                {
                    return false;
                }
            }
        }

        private static string RegistrableDomain(string host)
        {
            if (string.IsNullOrEmpty(host))
            {
                return null;
            }

            string[] parts = host.Split('.');
            if (parts.Length <= 2)
            {
                return host;
            }
            return parts[parts.Length - 2] + "." + parts[parts.Length - 1];
        }

        private static bool IsThirdParty(string host, string topHost)
        {
            if (string.IsNullOrEmpty(topHost))
            {
                return false;
            }
            string a = RegistrableDomain(host);
            string b = RegistrableDomain(topHost);
            if (a == null || b == null)
            {
                return false;
            }
            return !string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Compares two URLs ignoring case and a single trailing slash.</summary>
        private static bool UrlsEqual(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
            {
                return false;
            }

            string x = a.EndsWith("/", StringComparison.Ordinal) ? a.Substring(0, a.Length - 1) : a;
            string y = b.EndsWith("/", StringComparison.Ordinal) ? b.Substring(0, b.Length - 1) : b;
            return string.Equals(x, y, StringComparison.OrdinalIgnoreCase);
        }

        private static bool HostMatchesDomain(string host, string domain)
        {
            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(domain))
            {
                return false;
            }
            if (string.Equals(host, domain, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            return host.EndsWith("." + domain, StringComparison.OrdinalIgnoreCase);
        }

        internal static string NormalizeType(string type)
        {
            if (string.IsNullOrEmpty(type))
            {
                return "other";
            }

            switch (type.ToLowerInvariant())
            {
                case "script": return "script";
                case "image": return "image";
                case "stylesheet":
                case "css": return "stylesheet";
                case "document":
                case "doc": return "document";
                case "subdocument":
                case "sub_frame": return "subdocument";
                case "xmlhttprequest":
                case "xhr": return "xmlhttprequest";
                case "media": return "media";
                case "font": return "font";
                case "popup": return "popup";
                case "ping":
                case "beacon": return "ping";
                case "websocket": return "websocket";
                default: return "other";
            }
        }

        private static void Log(string message)
        {
            try
            {
                string path = Path.Combine(GetCacheDirectory(), "adblock.log");
                File.AppendAllText(path, DateTime.Now.ToString("u", CultureInfo.InvariantCulture) +
                    "  " + message + Environment.NewLine, Encoding.UTF8);
            }
            catch
            {
            }
        }

        private sealed class PatternRule
        {
            public Regex Regex;
            public string Token;
            public string TokenPrefix;
            public long CheckVersion;
            public bool ThirdPartyOnly;
            public bool FirstPartyOnly;
            public HashSet<string> IncludeDomains;
            public List<string> ExcludeDomains;
            public HashSet<string> Types;
            public HashSet<string> ExcludeTypes;

            public bool Matches(string uri, string host, string topHost, bool thirdParty, string type)
            {
                if (ThirdPartyOnly && !thirdParty)
                {
                    return false;
                }
                if (FirstPartyOnly && thirdParty)
                {
                    return false;
                }

                if (IncludeDomains != null && IncludeDomains.Count > 0)
                {
                    if (string.IsNullOrEmpty(topHost))
                    {
                        return false;
                    }

                    bool matched = false;
                    foreach (string domain in IncludeDomains)
                    {
                        if (HostMatchesDomain(topHost, domain))
                        {
                            matched = true;
                            break;
                        }
                    }
                    if (!matched)
                    {
                        return false;
                    }
                }

                if (ExcludeDomains != null && ExcludeDomains.Count > 0 && !string.IsNullOrEmpty(topHost))
                {
                    for (int i = 0; i < ExcludeDomains.Count; i++)
                    {
                        if (HostMatchesDomain(topHost, ExcludeDomains[i]))
                        {
                            return false;
                        }
                    }
                }

                if (Types != null && Types.Count > 0 && !Types.Contains(type))
                {
                    return false;
                }
                if (ExcludeTypes != null && ExcludeTypes.Contains(type))
                {
                    return false;
                }

                if (Token.Length > 0 && uri.IndexOf(Token, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    return false;
                }

                return Regex.IsMatch(uri);
            }
        }

        // =====================================================================
        //  Injected cosmetic / behavioural script
        // =====================================================================

        public static string GetAdBlockInjectionScript()
        {
            return @"
(function () {
    'use strict';

    if (window.__auraShields) { return; }
    window.__auraShields = true;

    // ---- Element hiding rules -------------------------------------------
    var CSS_SELECTORS = [
        'ins.adsbygoogle',
        'iframe[src*=""googlesyndication.com""]',
        'iframe[src*=""doubleclick.net""]',
        'iframe[src*=""adservice.google""]',
        'iframe[src*=""amazon-adsystem""]',
        'iframe[src*=""adsystem""]',
        'iframe[id^=""google_ads_iframe""]',
        'iframe[id^=""aswift_""]',
        'iframe[name^=""google_ads_iframe""]',
        'div[id^=""google_ads_""]',
        'div[id^=""div-gpt-ad""]',
        'div[id^=""AD-""]',
        'div[id*=""-ad-container""]',
        'div[class*=""ad-container""]',
        'div[class*=""ad-wrapper""]',
        'div[class*=""ad-slot""]',
        'div[class*=""ad-banner""]',
        'div[class*=""advertisement""]',
        'div[class*=""advert""]',
        'div[class*=""sponsored""]',
        'div[class*=""adsbygoogle""]',
        'div[id*=""taboola""]',
        'div[class*=""taboola""]',
        'div[id*=""outbrain""]',
        'div[class*=""outbrain""]',
        'div[id^=""rc-widget""]',
        'div[class*=""revcontent""]',
        'div[id*=""mgid""]',
        'div[class*=""adsanity""]',
        'amp-ad',
        'amp-embed[type=""_ping_""]',
        'div[data-ad-slot]',
        'div[data-ad-unit]',
        'div[data-ad-client]',
        'div[data-google-query-id]',
        'div[aria-label=""advertisement""]',
        'div[aria-label=""Advertisement""]',
        'section[class*=""ad-slot""]',
        'aside[class*=""advert""]',
        '.adsbygoogle',
        '.ad-banner',
        '.ad-slot',
        '.ad-unit',
        '.ad-container',
        '#ad_banner',
        '#adBanner',
        '#banner-ad',
        '#advertisement',
        '#carbonads',
        '#taboola-below-article-thumbnails',
        '#outbrain_widget_0',
        '.OUTBRAIN',
        '.trc_related_container',
        '.gpt-ad',
        '.adthrive-ad',
        '.ezoic-ad',
        '.medianet-in-article',
        '[class^=""ad-""]',
        '[class^=""ad_""]',
        '[class$=""-ad""]',
        '[class*=""-ad-""]',
        '[id^=""ad-""]',
        '[id^=""ad_""]',
        '[class*=""-ads-""]',
        '[class$=""-ads""]',
        '[data-testid=""ad-slot""]',
        'div[class*=""promoted""][class*=""sparkles""]',
        'ytd-promoted-sparkles-web-renderer',
        'ytd-promoted-video-renderer',
        'ytd-display-ad-renderer',
        'ytd-banner-promo-renderer',
        'ytd-in-feed-ad-layout-renderer',
        'ytd-ad-slot-renderer',
        'ytd-rich-item-renderer:has(ytd-ad-slot-renderer)',
        'ytd-engagement-panel-section-list-renderer[target-id=""engagement-panel-ads""]',
        '#player-ads',
        '#masthead-ad',
        '.ytp-ad-overlay-container',
        '.ytp-ad-message-container',
        '.ytp-ad-image-overlay',
        '.ytp-ad-text-overlay',
        '.ytp-ad-progress-list'
    ];

    function injectCss() {
        if (!document.documentElement) { return; }
        if (document.getElementById('__aura_shields_css')) { return; }
        var style = document.createElement('style');
        style.id = '__aura_shields_css';
        style.type = 'text/css';
        style.textContent = CSS_SELECTORS.join(',') + '{display:none !important;}';
        (document.head || document.documentElement).appendChild(style);
    }

    function removeMatching(root) {
        if (!root || !root.querySelectorAll) { return; }
        var nodes;
        try {
            nodes = root.querySelectorAll(CSS_SELECTORS.join(','));
        } catch (e) {
            return;
        }
        for (var i = 0; i < nodes.length; i++) {
            var el = nodes[i];
            if (el && el.parentNode && el.tagName !== 'BODY' && el.tagName !== 'HTML') {
                el.parentNode.removeChild(el);
            }
        }
    }

    // ---- Popup / iframe ad URLs -----------------------------------------
    // Only these known ad-network fragments are treated as ads. The previous
    // implementation blocked anything containing 'ad', 'click' or 'track',
    // which broke ordinary sites.
    var AD_URL_FRAGMENTS = [
        'doubleclick.net', 'googlesyndication.com', 'googleadservices.com',
        'adservice.google.', 'adnxs.com', 'adsystem.com', 'adroll.com', 'criteo.',
        'taboola.com', 'outbrain.com', 'revcontent.com', 'mgid.com', 'zergnet.com',
        'popads.net', 'popcash.net', 'propellerads.com', 'exoclick.com', 'trafficjunky.com',
        'adsterra.com', 'clickadu.com', 'onclckds.com', 'onclickads.net', 'zeropark.com',
        'popmyads.com', 'adf.ly', 'shorte.st', 'adfoc.us', 'hilltopads.net', 'plugrush.com',
        'juicyads.com', 'bidvertiser.com', 'adcash.com', 'popunder', 'popunderjs'
    ];

    function isAdUrl(url) {
        if (!url || typeof url !== 'string') { return false; }
        var lower = url.toLowerCase();
        for (var i = 0; i < AD_URL_FRAGMENTS.length; i++) {
            if (lower.indexOf(AD_URL_FRAGMENTS[i]) !== -1) { return true; }
        }
        return false;
    }

    var nativeOpen = window.open;
    window.open = function (url) {
        if (typeof url === 'string' && isAdUrl(url)) {
            try { console.log('[Aura Shields] blocked popup: ' + url); } catch (e) {}
            return null;
        }
        return nativeOpen.apply(window, arguments);
    };

    function killAdFrames() {
        var frames = document.querySelectorAll('iframe[src]');
        for (var i = 0; i < frames.length; i++) {
            var src = frames[i].getAttribute('src');
            if (src && isAdUrl(src) && frames[i].parentNode) {
                frames[i].parentNode.removeChild(frames[i]);
            }
        }
    }

    // ---- YouTube --------------------------------------------------------
    function handleYouTube() {
        if (location.hostname.indexOf('youtube.com') === -1 &&
            location.hostname.indexOf('youtube-nocookie.com') === -1) {
            return;
        }

        try {
            var skip = document.querySelector(
                '.ytp-ad-skip-button, .ytp-ad-skip-button-modern, .ytp-skip-ad-button, .ytp-ad-skip-button-slot button');
            if (skip) { skip.click(); }

            var adShowing = document.querySelector('.ad-showing, .ad-interrupting');
            var video = document.querySelector('video');
            if (adShowing && video) {
                try {
                    if (!isNaN(video.duration) && isFinite(video.duration)) {
                        video.currentTime = video.duration;
                    } else {
                        video.currentTime = 1e5;
                    }
                    video.muted = true;
                } catch (e) {}
                if (skip) { skip.click(); }
            }
        } catch (e) {}
    }

    // ---- Run ------------------------------------------------------------
    function sweep() {
        injectCss();
        killAdFrames();
        handleYouTube();
    }

    function installObserver() {
        if (!document.documentElement) { return; }
        var pending = false;
        var observer = new MutationObserver(function () {
            if (pending) { return; }
            pending = true;
            setTimeout(function () {
                pending = false;
                removeMatching(document);
                handleYouTube();
            }, 250);
        });
        observer.observe(document.documentElement, { childList: true, subtree: true });
    }

    sweep();

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', function () {
            sweep();
            removeMatching(document);
            installObserver();
        });
    } else {
        removeMatching(document);
        installObserver();
    }

    window.addEventListener('load', function () {
        sweep();
        removeMatching(document);
    });
})();
";
        }
    }
}
