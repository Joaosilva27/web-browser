using System;
using System.Collections.Generic;
using System.IO;

namespace SimpleBrowser
{
    public class ImportSelection
    {
        public bool Bookmarks;
        public bool History;
        public bool Searches;
        public bool TopSites;
    }

    public class ImportResult
    {
        public int Bookmarks;
        public int History;
        public int Searches;
        public int Favorites;
        public readonly List<string> Notes = new List<string>();

        public int Total
        {
            get { return Bookmarks + History + Searches; }
        }
    }

    /// <summary>
    /// Reads bookmarks, history, search terms and top sites out of another
    /// browser's profile.
    ///
    /// Cookies and saved passwords are deliberately out of scope: Chrome, Edge
    /// and Brave have encrypted them with App-Bound Encryption since version 127,
    /// and the key only unwraps for the browser that wrote it. Nothing another
    /// application does can read those values, so Aura does not pretend to.
    /// </summary>
    public static class BrowserImporter
    {
        public static ImportResult Run(
            BrowserProfile profile,
            ImportSelection selection,
            BookmarkManager bookmarks,
            HistoryManager history,
            FavoritesStore favorites)
        {
            var result = new ImportResult();
            if (profile == null || selection == null) return result;

            if (selection.Bookmarks)
            {
                var items = ReadBookmarks(profile);
                if (items.Count == 0)
                {
                    result.Notes.Add("No bookmarks were found.");
                }
                else
                {
                    result.Bookmarks = bookmarks.ImportBookmarks(items);
                }
            }

            if (selection.History)
            {
                var items = ReadHistory(profile, 20000);
                if (items.Count == 0)
                {
                    result.Notes.Add("No history was found.");
                }
                else
                {
                    result.History = history.ImportHistory(items);
                }
            }

            if (selection.Searches)
            {
                var items = ReadSearches(profile, 5000);
                if (items.Count == 0)
                {
                    result.Notes.Add(profile.IsChromium
                        ? "No search terms were found."
                        : "Firefox does not keep a separate search history.");
                }
                else
                {
                    result.Searches = history.ImportSearches(items);
                }
            }

            if (selection.TopSites)
            {
                var items = ReadTopSites(profile, 12);
                result.Favorites = favorites.AddIfNew(ToFavorites(items), 12);
            }

            return result;
        }

        // ── Bookmarks ────────────────────────────────────────────────────────

        public static List<BookmarkItem> ReadBookmarks(BrowserProfile profile)
        {
            return profile.IsChromium ? ReadChromiumBookmarks(profile) : ReadFirefoxBookmarks(profile);
        }

        private static List<BookmarkItem> ReadChromiumBookmarks(BrowserProfile profile)
        {
            var items = new List<BookmarkItem>();

            try
            {
                if (!File.Exists(profile.BookmarksFile)) return items;

                var root = Json.AsObject(Json.Parse(File.ReadAllText(profile.BookmarksFile)));
                var roots = root == null ? null : Json.AsObject(Get(root, "roots"));
                if (roots == null) return items;

                foreach (var pair in roots)
                {
                    WalkBookmarkNode(pair.Value, FolderNameFor(pair.Key), items);
                }
            }
            catch
            {
                // A damaged bookmarks file yields nothing.
            }

            return items;
        }

        private static void WalkBookmarkNode(object node, string folder, List<BookmarkItem> items)
        {
            var obj = Json.AsObject(node);
            if (obj == null) return;

            string type = Json.GetString(obj, "type");
            if (string.Equals(type, "url", StringComparison.OrdinalIgnoreCase))
            {
                string url = Json.GetString(obj, "url");
                if (!IsImportableUrl(url)) return;

                items.Add(new BookmarkItem(
                    CleanTitle(Json.GetString(obj, "name"), url), url, folder));
                return;
            }

            var children = Json.AsList(Get(obj, "children"));
            if (children == null) return;

            string name = Json.GetString(obj, "name");
            string childFolder = string.IsNullOrEmpty(name) ? folder : Combine(folder, name);

            foreach (var child in children)
            {
                WalkBookmarkNode(child, childFolder, items);
            }
        }

        private static string FolderNameFor(string rootKey)
        {
            if (string.Equals(rootKey, "bookmark_bar", StringComparison.OrdinalIgnoreCase)) return "Favorites";
            if (string.Equals(rootKey, "other", StringComparison.OrdinalIgnoreCase)) return "Other bookmarks";
            if (string.Equals(rootKey, "synced", StringComparison.OrdinalIgnoreCase)) return "Mobile bookmarks";
            return rootKey;
        }

        private static string Combine(string folder, string name)
        {
            return string.IsNullOrEmpty(folder) ? name : folder + "/" + name;
        }

        private static List<BookmarkItem> ReadFirefoxBookmarks(BrowserProfile profile)
        {
            var items = new List<BookmarkItem>();
            if (!File.Exists(profile.PlacesFile)) return items;

            var rows = Sqlite.Query(profile.PlacesFile,
                "SELECT b.title, p.url FROM moz_bookmarks b " +
                "JOIN moz_places p ON p.id = b.fk " +
                "WHERE b.type = 1 AND p.url IS NOT NULL", 20000);

            foreach (var row in rows)
            {
                if (row.Length < 2 || !IsImportableUrl(row[1])) continue;
                items.Add(new BookmarkItem(CleanTitle(row[0], row[1]), row[1], "Firefox"));
            }

            return items;
        }

        // ── History ──────────────────────────────────────────────────────────

        public static List<HistoryItem> ReadHistory(BrowserProfile profile, int max)
        {
            return profile.IsChromium ? ReadChromiumHistory(profile, max) : ReadFirefoxHistory(profile, max);
        }

        private static List<HistoryItem> ReadChromiumHistory(BrowserProfile profile, int max)
        {
            var items = new List<HistoryItem>();
            if (!File.Exists(profile.HistoryFile)) return items;

            var rows = Sqlite.Query(profile.HistoryFile,
                "SELECT url, title, last_visit_time FROM urls " +
                "WHERE last_visit_time > 0 ORDER BY last_visit_time DESC", max);

            foreach (var row in rows)
            {
                if (row.Length < 3 || !IsImportableUrl(row[0])) continue;

                long ticks;
                long.TryParse(row[2], out ticks);
                items.Add(new HistoryItem(FromChromeTime(ticks), CleanTitle(row[1], row[0]), row[0]));
            }

            return items;
        }

        private static List<HistoryItem> ReadFirefoxHistory(BrowserProfile profile, int max)
        {
            var items = new List<HistoryItem>();
            if (!File.Exists(profile.PlacesFile)) return items;

            var rows = Sqlite.Query(profile.PlacesFile,
                "SELECT url, title, last_visit_date FROM moz_places " +
                "WHERE last_visit_date > 0 ORDER BY last_visit_date DESC", max);

            foreach (var row in rows)
            {
                if (row.Length < 3 || !IsImportableUrl(row[0])) continue;

                long micros;
                long.TryParse(row[2], out micros);
                items.Add(new HistoryItem(FromFirefoxTime(micros), CleanTitle(row[1], row[0]), row[0]));
            }

            return items;
        }

        // ── Search terms ─────────────────────────────────────────────────────

        public static List<SearchEntry> ReadSearches(BrowserProfile profile, int max)
        {
            var items = new List<SearchEntry>();
            if (!profile.IsChromium || !File.Exists(profile.HistoryFile)) return items;

            // Modern Chromium keeps the query text in keyword_search_terms and the
            // result address in urls, linked by url_id (there is no url column).
            var rows = Sqlite.Query(profile.HistoryFile,
                "SELECT k.term, u.url, u.last_visit_time FROM keyword_search_terms k " +
                "JOIN urls u ON u.id = k.url_id " +
                "WHERE k.term IS NOT NULL AND k.term <> '' " +
                "ORDER BY u.last_visit_time DESC", max);

            foreach (var row in rows)
            {
                if (row.Length < 2 || string.IsNullOrEmpty(row[0])) continue;

                var when = DateTime.Now;
                if (row.Length >= 3)
                {
                    long ticks;
                    if (long.TryParse(row[2], out ticks)) when = FromChromeTime(ticks);
                }

                items.Add(new SearchEntry(when, row[0], row[1]));
            }

            return items;
        }

        // ── Top sites ────────────────────────────────────────────────────────

        public static List<BookmarkItem> ReadTopSites(BrowserProfile profile, int max)
        {
            var items = new List<BookmarkItem>();
            if (!profile.IsChromium || !File.Exists(profile.TopSitesFile)) return items;

            var rows = Sqlite.Query(profile.TopSitesFile,
                "SELECT url, title FROM top_sites ORDER BY url_rank", max * 3);

            foreach (var row in rows)
            {
                if (items.Count >= max) break;
                if (row.Length < 2 || !IsTopSiteWorthy(row[0])) continue;

                items.Add(new BookmarkItem(CleanTitle(row[1], row[0]), row[0], string.Empty));
            }

            return items;
        }

        /// <summary>
        /// Top sites are whatever the other browser saw often, which includes
        /// things nobody wants as a tile: OAuth callbacks still carrying a token,
        /// router pages and local servers.
        /// </summary>
        private static bool IsTopSiteWorthy(string url)
        {
            if (!IsImportableUrl(url)) return false;
            if (url.Length > 180) return false;
            if (IsSearchResultUrl(url)) return false;

            string lower = url.ToLowerInvariant();
            if (lower.Contains("/callback") || lower.Contains("/oauth") || lower.Contains("/auth/")) return false;

            string host = FavoritesStore.HostOf(url);
            if (string.IsNullOrEmpty(host)) return false;
            if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase)) return false;
            if (IsPrivateAddress(host)) return false;

            return true;
        }

        private static bool IsPrivateAddress(string host)
        {
            string[] parts = host.Split('.');
            if (parts.Length != 4) return false;

            int a, b, c, d;
            if (!int.TryParse(parts[0], out a)) return false;
            if (!int.TryParse(parts[1], out b)) return false;
            if (!int.TryParse(parts[2], out c)) return false;
            if (!int.TryParse(parts[3], out d)) return false;

            if (a == 10 || a == 127) return true;
            if (a == 192 && b == 168) return true;
            if (a == 172 && b >= 16 && b <= 31) return true;
            return false;
        }

        private static List<Favorite> ToFavorites(List<BookmarkItem> items)
        {
            var favorites = new List<Favorite>();
            foreach (var item in items)
            {
                favorites.Add(new Favorite
                {
                    Title = TileTitle(item),
                    Url = item.Url,
                    Letter = FavoritesStore.LetterFor(item.Title, item.Url),
                    Color = FavoritesStore.ColorFor(item.Url)
                });
            }
            return favorites;
        }

        /// <summary>
        /// A tile label is only ~74px wide, so a short site name reads better
        /// than a hostname; anything long or noisy falls back to the host.
        /// </summary>
        private static string TileTitle(BookmarkItem item)
        {
            string title = (item.Title ?? string.Empty).Trim();
            if (title.Length > 0 && title.Length <= 18) return title;
            return FavoritesStore.DisplayHost(item.Url);
        }

        // ── Time conversions ─────────────────────────────────────────────────

        /// <summary>Chromium stores microseconds since 1601-01-01, the FILETIME epoch.</summary>
        private static DateTime FromChromeTime(long microseconds)
        {
            try
            {
                long ticks = microseconds * 10L;
                if (ticks <= 0 || ticks > DateTime.MaxValue.Ticks) return DateTime.Now;
                return DateTime.FromFileTimeUtc(ticks).ToLocalTime();
            }
            catch
            {
                return DateTime.Now;
            }
        }

        /// <summary>Firefox stores microseconds since 1970-01-01.</summary>
        private static DateTime FromFirefoxTime(long microseconds)
        {
            try
            {
                if (microseconds <= 0) return DateTime.Now;
                var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                long ticks = epoch.Ticks + microseconds * 10L;
                if (ticks > DateTime.MaxValue.Ticks) return DateTime.Now;
                return new DateTime(ticks, DateTimeKind.Utc).ToLocalTime();
            }
            catch
            {
                return DateTime.Now;
            }
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static object Get(Dictionary<string, object> obj, string key)
        {
            if (obj == null) return null;
            object value;
            return obj.TryGetValue(key, out value) ? value : null;
        }

        private static bool IsImportableUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return false;
            return url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsSearchResultUrl(string url)
        {
            return url.IndexOf("google.com/search", StringComparison.OrdinalIgnoreCase) >= 0
                || url.IndexOf("bing.com/search", StringComparison.OrdinalIgnoreCase) >= 0
                || url.IndexOf("duckduckgo.com/?q=", StringComparison.OrdinalIgnoreCase) >= 0
                || url.IndexOf("search.brave.com/search", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string CleanTitle(string title, string url)
        {
            string clean = (title ?? string.Empty).Trim();
            if (clean.Length == 0) return url;
            if (clean.Length > 300) clean = clean.Substring(0, 300);
            return clean;
        }
    }
}
