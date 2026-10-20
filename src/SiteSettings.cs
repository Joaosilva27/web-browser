using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace SimpleBrowser
{
    /// <summary>
    /// Choices that belong to one website rather than to Toge as a whole.
    ///
    /// Page zoom lives here, and only when the user has moved it for that site:
    /// everything else follows the Settings page, so a site nobody has touched
    /// keeps using the general page zoom and forgetting an override is simply a
    /// matter of removing the entry.
    /// </summary>
    public class SiteSettings
    {
        private static SiteSettings _current;

        public static SiteSettings Current
        {
            get
            {
                if (_current == null) _current = new SiteSettings();
                return _current;
            }
        }

        /// <summary>
        /// The zoom steps the page-settings popover walks through. A short list
        /// reads better than every value between 50 and 200.
        /// </summary>
        public static readonly int[] ZoomSteps = new int[]
        {
            50, 67, 75, 80, 90, 100, 110, 125, 150, 175, 200
        };

        private const int MinZoom = 50;
        private const int MaxZoom = 200;

        private readonly Dictionary<string, int> _zoom;
        private readonly string _filePath;

        private SiteSettings()
        {
            _zoom = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            string appData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Toge");
            _filePath = Path.Combine(appData, "sites.json");

            Load();
        }

        /// <summary>
        /// The site a URL belongs to, or an empty string for Toge's own pages.
        /// www is dropped so a site is one entry whichever way it is reached.
        /// </summary>
        public static string HostOf(string url)
        {
            if (string.IsNullOrEmpty(url)) return string.Empty;
            if (url.StartsWith("about:", StringComparison.OrdinalIgnoreCase)) return string.Empty;
            if (url.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) return string.Empty;

            string host = FavoritesStore.HostOf(url);
            if (string.IsNullOrEmpty(host)) return string.Empty;

            host = host.ToLowerInvariant();
            if (host.StartsWith("www.")) host = host.Substring(4);
            return host;
        }

        /// <summary>True when this site has been given a zoom of its own.</summary>
        public bool HasZoom(string url)
        {
            string host = HostOf(url);
            return host.Length > 0 && _zoom.ContainsKey(host);
        }

        /// <summary>The zoom for a URL: the site's own, or the general one.</summary>
        public int ZoomFor(string url)
        {
            string host = HostOf(url);

            int stored;
            if (host.Length > 0 && _zoom.TryGetValue(host, out stored)) return stored;

            return Settings.Current.Zoom;
        }

        /// <summary>
        /// Gives a site its own zoom. Setting it back to the general page zoom
        /// drops the entry instead of storing a value that says nothing.
        /// </summary>
        public void SetZoom(string url, int percent)
        {
            string host = HostOf(url);
            if (host.Length == 0) return;

            percent = Clamp(percent);
            if (percent == Settings.Current.Zoom)
            {
                ClearZoom(url);
                return;
            }

            _zoom[host] = percent;
            Save();
        }

        /// <summary>Hands the site back to the general page zoom.</summary>
        public void ClearZoom(string url)
        {
            string host = HostOf(url);
            if (host.Length == 0) return;
            if (_zoom.Remove(host)) Save();
        }

        /// <summary>The next step up or down from a zoom value.</summary>
        public static int Step(int percent, int direction)
        {
            int i;

            if (direction > 0)
            {
                for (i = 0; i < ZoomSteps.Length; i++)
                {
                    if (ZoomSteps[i] > percent) return ZoomSteps[i];
                }
                return MaxZoom;
            }

            for (i = ZoomSteps.Length - 1; i >= 0; i--)
            {
                if (ZoomSteps[i] < percent) return ZoomSteps[i];
            }
            return MinZoom;
        }

        private static int Clamp(int percent)
        {
            if (percent < MinZoom) return MinZoom;
            if (percent > MaxZoom) return MaxZoom;
            return percent;
        }

        private void Load()
        {
            try
            {
                if (!File.Exists(_filePath)) return;

                var root = Json.AsObject(Json.Parse(File.ReadAllText(_filePath)));
                var sites = Json.AsObject(root == null ? null : Get(root, "sites"));
                if (sites == null) return;

                foreach (var pair in sites)
                {
                    var entry = Json.AsObject(pair.Value);
                    if (entry == null) continue;

                    int percent = Clamp(Json.GetInt(entry, "zoom", Settings.Current.Zoom));
                    _zoom[pair.Key] = percent;
                }
            }
            catch
            {
                // A damaged file simply means no site overrides.
            }
        }

        private void Save()
        {
            try
            {
                var sites = new Dictionary<string, object>();
                foreach (var pair in _zoom)
                {
                    sites[pair.Key] = Json.Obj("zoom", pair.Value);
                }

                string json = Json.Write(Json.Obj("version", 1, "sites", sites));
                if (json != null) File.WriteAllText(_filePath, json);
            }
            catch
            {
                // Ignore persistence errors.
            }
        }

        private static object Get(Dictionary<string, object> obj, string key)
        {
            if (obj == null) return null;

            object value;
            return obj.TryGetValue(key, out value) ? value : null;
        }

        /// <summary>Used by the settings page and the popover to show a percentage.</summary>
        public static string PercentText(int percent)
        {
            return percent.ToString(CultureInfo.InvariantCulture) + "%";
        }
    }
}
