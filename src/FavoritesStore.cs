using System;
using System.Collections.Generic;
using System.IO;

namespace SimpleBrowser
{
    public class Favorite
    {
        public string Title { get; set; }
        public string Url { get; set; }
        public string Letter { get; set; }
        public string Color { get; set; }

        /// <summary>Explicit icon address; when empty one is derived from the host.</summary>
        public string Icon { get; set; }
    }

    /// <summary>
    /// The tiles on the start page. Seeded with Aura's own picks and extended by
    /// imported top sites, so the new tab page reflects where the user actually
    /// goes rather than a fixed list.
    /// </summary>
    public class FavoritesStore
    {
        private static FavoritesStore _current;

        /// <summary>The start page reads this directly, like Theme.</summary>
        public static FavoritesStore Current
        {
            get
            {
                if (_current == null) _current = new FavoritesStore();
                return _current;
            }
        }

        private readonly string _filePath;
        private readonly List<Favorite> _favorites;

        public FavoritesStore()
        {
            string appData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AuraBrowser");
            if (!Directory.Exists(appData))
            {
                Directory.CreateDirectory(appData);
            }

            _filePath = Path.Combine(appData, "favorites.json");
            _favorites = new List<Favorite>();
            Load();
        }

        public List<Favorite> GetFavorites()
        {
            return new List<Favorite>(_favorites);
        }

        public bool ContainsHost(string url)
        {
            string host = HostOf(url);
            if (string.IsNullOrEmpty(host)) return false;

            foreach (var favorite in _favorites)
            {
                if (string.Equals(HostOf(favorite.Url), host, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        /// <summary>
        /// Appends favourites that are not already on the page, up to a total of
        /// <paramref name="maxTotal"/> tiles. Returns how many were added.
        /// </summary>
        public int AddIfNew(IEnumerable<Favorite> candidates, int maxTotal)
        {
            if (candidates == null) return 0;

            int added = 0;
            foreach (var candidate in candidates)
            {
                if (_favorites.Count >= maxTotal) break;
                if (candidate == null || string.IsNullOrEmpty(candidate.Url)) continue;
                if (ContainsHost(candidate.Url)) continue;

                if (string.IsNullOrEmpty(candidate.Letter)) candidate.Letter = LetterFor(candidate.Title, candidate.Url);
                if (string.IsNullOrEmpty(candidate.Color)) candidate.Color = ColorFor(candidate.Url);
                _favorites.Add(candidate);
                added++;
            }

            if (added > 0) Save();
            return added;
        }

        public void Remove(string url)
        {
            _favorites.RemoveAll(f => string.Equals(f.Url, url, StringComparison.OrdinalIgnoreCase));
            Save();
        }

        /// <summary>Adds a tile for any address, used by the start page itself.</summary>
        public bool Add(string title, string url)
        {
            if (string.IsNullOrEmpty(url) || ContainsHost(url)) return false;
            _favorites.Add(new Favorite
            {
                Title = string.IsNullOrEmpty(title) ? DisplayHost(url) : title,
                Url = url,
                Letter = LetterFor(title, url),
                Color = ColorFor(url),
                Icon = null
            });
            Save();
            return true;
        }

        /// <summary>Local-file icon choices that beat what the favicon service returns.</summary>
        public static string IconFor(Favorite favorite)
        {
            if (favorite == null) return null;
            if (!string.IsNullOrEmpty(favorite.Icon)) return favorite.Icon;

            return ServiceIconUrl(favorite.Url);
        }

        /// <summary>
        /// The favicon service address for a page. The start-page tiles and the
        /// bookmarks bar both use it so a site shows its real icon even when it
        /// has never been opened here.
        /// </summary>
        public static string ServiceIconUrl(string url)
        {
            string host = HostOf(url);
            if (string.IsNullOrEmpty(host)) return null;

            if (host.IndexOf("mail.google.com", StringComparison.OrdinalIgnoreCase) >= 0 ||
                host.IndexOf("gmail.com", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "https://www.gstatic.com/images/branding/product/2x/gmail_2020q4_48dp.png";
            }

            return "https://icons.duckduckgo.com/ip3/" + host + ".ico";
        }

        public static string HostOf(string url)
        {
            if (string.IsNullOrEmpty(url)) return null;
            try
            {
                var uri = new Uri(url, UriKind.Absolute);
                return uri.Host;
            }
            catch
            {
                return null;
            }
        }

        public static string DisplayHost(string url)
        {
            string host = HostOf(url);
            if (string.IsNullOrEmpty(host)) return url;
            if (host.StartsWith("www.", StringComparison.OrdinalIgnoreCase)) host = host.Substring(4);
            return host;
        }

        public static string LetterFor(string title, string url)
        {
            string source = string.IsNullOrEmpty(title) ? DisplayHost(url) : title;
            if (string.IsNullOrEmpty(source)) return "?";
            return char.ToUpperInvariant(source[0]).ToString();
        }

        /// <summary>Stable brand-ish colour derived from the host.</summary>
        public static string ColorFor(string url)
        {
            string host = HostOf(url) ?? string.Empty;
            int hash = 0;
            foreach (char c in host) hash = (hash * 31 + c) & 0x7FFFFFFF;

            string[] palette =
            {
                "#0a84ff", "#ff375f", "#30d158", "#ff9f0a", "#bf5af2",
                "#5e5ce6", "#ff6482", "#64d2ff", "#ffd60a", "#ac8e68"
            };

            return palette[hash % palette.Length];
        }

        private void Load()
        {
            if (TryLoad()) return;

            // Aura's own starting set.
            AddDefault("Brave Search", "https://search.brave.com", "B", "#f25a29", null);
            AddDefault("GitHub", "https://github.com", "G", "#24292f", null);
            AddDefault("YouTube", "https://youtube.com", "Y", "#ff0033", null);
            AddDefault("Reddit", "https://reddit.com", "R", "#ff4500", null);
            AddDefault("Wikipedia", "https://wikipedia.org", "W", "#54595d", null);
            AddDefault("Hacker News", "https://news.ycombinator.com", "H", "#ff6600", null);
            AddDefault("Gmail", "https://mail.google.com", "M", "#ea4335",
                "https://www.gstatic.com/images/branding/product/2x/gmail_2020q4_48dp.png");
            AddDefault("X", "https://x.com", "X", "#1d1d1f", null);
            Save();
        }

        private void AddDefault(string title, string url, string letter, string color, string icon)
        {
            _favorites.Add(new Favorite
            {
                Title = title,
                Url = url,
                Letter = letter,
                Color = color,
                Icon = icon
            });
        }

        private bool TryLoad()
        {
            if (!File.Exists(_filePath)) return false;

            try
            {
                var root = Json.AsObject(Json.Parse(File.ReadAllText(_filePath)));
                var list = root == null ? null : Json.AsList(Value(root, "favorites"));
                if (list == null) return false;

                foreach (var entry in list)
                {
                    var obj = Json.AsObject(entry);
                    string url = Json.GetString(obj, "url");
                    if (string.IsNullOrEmpty(url)) continue;

                    _favorites.Add(new Favorite
                    {
                        Title = Json.GetString(obj, "title"),
                        Url = url,
                        Letter = Json.GetString(obj, "letter"),
                        Color = Json.GetString(obj, "color"),
                        Icon = Json.GetString(obj, "icon")
                    });
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static object Value(Dictionary<string, object> obj, string key)
        {
            if (obj == null) return null;
            object value;
            return obj.TryGetValue(key, out value) ? value : null;
        }

        private void Save()
        {
            try
            {
                var list = new List<object>();
                foreach (var favorite in _favorites)
                {
                    list.Add(Json.Obj(
                        "title", favorite.Title ?? string.Empty,
                        "url", favorite.Url ?? string.Empty,
                        "letter", favorite.Letter ?? string.Empty,
                        "color", favorite.Color ?? string.Empty,
                        "icon", favorite.Icon ?? string.Empty));
                }

                string json = Json.Write(Json.Obj("version", 1, "favorites", list));
                if (json != null) File.WriteAllText(_filePath, json);
            }
            catch
            {
                // Ignore persistence errors.
            }
        }
    }
}
