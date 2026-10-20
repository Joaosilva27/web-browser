using System;
using System.Collections.Generic;
using System.IO;

namespace SimpleBrowser
{
    /// <summary>
    /// A small memory of the site pictures the browser has already seen: one
    /// page icon per website host, handed over by the tabs themselves. The
    /// address bar uses it so history suggestions wear the same face the tab
    /// strip shows. Nothing is fetched here, ever.
    /// </summary>
    internal static class FaviconStore
    {
        /// <summary>Enough hosts to cover months of browsing, few enough to stay small.</summary>
        private const int Limit = 256;

        /// <summary>Icons this large are flukes (favicongrabber pages and the like); skip them.</summary>
        private const int MaxIconSize = 60000;

        private static readonly Dictionary<string, string> ByHost =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Seen order, so the oldest hosts retire first when the cap is hit.</summary>
        private static readonly List<string> Order = new List<string>();

        private static bool _loaded;

        private static string FilePath
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Toge", "favicon.json");
            }
        }

        public static void Load()
        {
            if (_loaded) return;
            _loaded = true;

            try
            {
                string path = FilePath;
                if (!File.Exists(path)) return;

                var stored = Json.AsObject(Json.Parse(File.ReadAllText(path)));
                if (stored == null) return;

                foreach (var pair in stored)
                {
                    string icon = pair.Value as string;
                    if (string.IsNullOrEmpty(icon) || icon.Length > MaxIconSize) continue;
                    RememberQuiet(pair.Key, icon);
                }
            }
            catch
            {
                // A damaged cache is not worth a message; it rebuilds itself.
            }
        }

        /// <summary>
        /// Remembers a page icon for a URL's host. Called as tabs resolve
        /// their own favicons, so no extra work is done that a tab was not
        /// already doing.
        /// </summary>
        public static void Remember(string url, string dataUrl)
        {
            if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(dataUrl)) return;
            if (dataUrl.Length > MaxIconSize) return;

            string host = HostOf(url);
            if (string.IsNullOrEmpty(host)) return;

            string current;
            if (ByHost.TryGetValue(host, out current))
            {
                if (string.Equals(current, dataUrl, StringComparison.Ordinal)) return;
                Order.Remove(host);
            }
            else if (Order.Count >= Limit)
            {
                while (Order.Count >= Limit)
                {
                    ByHost.Remove(Order[0]);
                    Order.RemoveAt(0);
                }
            }

            ByHost[host] = dataUrl;
            Order.Add(host);
            Save();
        }

        /// <summary>The stored icon for a URL's host, or null.</summary>
        public static string For(string url)
        {
            if (string.IsNullOrEmpty(url)) return null;

            string host = HostOf(url);
            if (string.IsNullOrEmpty(host)) return null;

            string icon;
            return ByHost.TryGetValue(host, out icon) ? icon : null;
        }

        private static void RememberQuiet(string host, string dataUrl)
        {
            ByHost[host] = dataUrl;
            if (Order.Contains(host)) Order.Remove(host);
            Order.Add(host);

            while (Order.Count > Limit)
            {
                ByHost.Remove(Order[0]);
                Order.RemoveAt(0);
            }
        }

        private static string HostOf(string url)
        {
            Uri parsed;
            if (!Uri.TryCreate(url, UriKind.Absolute, out parsed)) return null;

            string host = parsed.Host;
            if (host.StartsWith("www.", StringComparison.OrdinalIgnoreCase)) host = host.Substring(4);

            return host.Length > 0 ? host : null;
        }

        private static void Save()
        {
            try
            {
                string directory = Path.GetDirectoryName(FilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var payload = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                foreach (var pair in ByHost)
                {
                    payload[pair.Key] = pair.Value;
                }

                File.WriteAllText(FilePath, Json.Write(payload));
            }
            catch
            {
                // Persistence is best effort; the memory cache keeps working.
            }
        }
    }
}
