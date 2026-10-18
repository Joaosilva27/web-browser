using System;
using System.Collections.Generic;
using System.IO;

namespace SimpleBrowser
{
    public class BookmarkItem
    {
        public string Title { get; set; }
        public string Url { get; set; }

        /// <summary>Slash-separated folder path, empty for the top level.</summary>
        public string Folder { get; set; }

        public BookmarkItem(string title, string url)
            : this(title, url, null)
        {
        }

        public BookmarkItem(string title, string url, string folder)
        {
            Title = title;
            Url = url;
            Folder = string.IsNullOrEmpty(folder) ? string.Empty : folder.Trim('/');
        }
    }

    /// <summary>
    /// Aura's bookmarks, stored as JSON so titles and addresses may contain any
    /// character and so imported folder structure survives. Older installs have
    /// a pipe-delimited bookmarks.txt, which is migrated once on first load.
    /// </summary>
    public class BookmarkManager
    {
        public const string RootFolder = "Bookmarks";

        private readonly string _filePath;
        private readonly string _legacyPath;
        private readonly List<BookmarkItem> _bookmarks;

        public BookmarkManager()
        {
            string appData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AuraBrowser");
            if (!Directory.Exists(appData))
            {
                Directory.CreateDirectory(appData);
            }

            _filePath = Path.Combine(appData, "bookmarks.json");
            _legacyPath = Path.Combine(appData, "bookmarks.txt");
            _bookmarks = new List<BookmarkItem>();
            LoadBookmarks();
        }

        public List<BookmarkItem> GetBookmarks()
        {
            return new List<BookmarkItem>(_bookmarks);
        }

        /// <summary>Distinct folders, top level first, ordered by name.</summary>
        public List<string> GetFolders()
        {
            var seen = new List<string>();
            foreach (var bookmark in _bookmarks)
            {
                string folder = bookmark.Folder ?? string.Empty;
                if (!seen.Contains(folder)) seen.Add(folder);
            }

            seen.Sort(delegate (string a, string b)
            {
                if (a.Length == 0) return b.Length == 0 ? 0 : -1;
                if (b.Length == 0) return 1;
                return string.Compare(a, b, StringComparison.CurrentCultureIgnoreCase);
            });

            return seen;
        }

        public bool IsBookmarked(string url)
        {
            if (string.IsNullOrEmpty(url)) return false;
            foreach (var b in _bookmarks)
            {
                if (SameUrl(b.Url, url)) return true;
            }
            return false;
        }

        public void AddBookmark(string title, string url)
        {
            AddBookmark(title, url, null);
        }

        public void AddBookmark(string title, string url, string folder)
        {
            if (string.IsNullOrEmpty(url) || IsBookmarked(url)) return;
            if (string.IsNullOrEmpty(title)) title = url;
            _bookmarks.Add(new BookmarkItem(title, url, folder));
            SaveBookmarks();
        }

        /// <summary>
        /// Adds many bookmarks at once. Duplicates (by address) are skipped, and
        /// the file is written once. Returns how many were actually added.
        /// </summary>
        public int ImportBookmarks(IEnumerable<BookmarkItem> items)
        {
            if (items == null) return 0;

            var known = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var b in _bookmarks) known.Add(Normalize(b.Url));

            int added = 0;
            foreach (var item in items)
            {
                if (item == null || string.IsNullOrEmpty(item.Url)) continue;
                if (!known.Add(Normalize(item.Url))) continue;

                _bookmarks.Add(new BookmarkItem(
                    string.IsNullOrEmpty(item.Title) ? item.Url : item.Title,
                    item.Url,
                    item.Folder));
                added++;
            }

            if (added > 0) SaveBookmarks();
            return added;
        }

        public void RemoveBookmark(string url)
        {
            _bookmarks.RemoveAll(b => SameUrl(b.Url, url));
            SaveBookmarks();
        }

        private static bool SameUrl(string a, string b)
        {
            return string.Equals((a ?? string.Empty).TrimEnd('/'), (b ?? string.Empty).TrimEnd('/'),
                StringComparison.OrdinalIgnoreCase);
        }

        private static string Normalize(string url)
        {
            return (url ?? string.Empty).TrimEnd('/');
        }

        private void LoadBookmarks()
        {
            _bookmarks.Clear();

            if (TryLoadJson()) return;
            if (TryLoadLegacy()) return;

            _bookmarks.Add(new BookmarkItem("\u2726 Apple", "https://apple.com"));
            _bookmarks.Add(new BookmarkItem("GitHub", "https://github.com"));
            _bookmarks.Add(new BookmarkItem("YouTube", "https://www.youtube.com"));
            _bookmarks.Add(new BookmarkItem("Wikipedia", "https://www.wikipedia.org"));
            _bookmarks.Add(new BookmarkItem("Reddit", "https://www.reddit.com"));
            _bookmarks.Add(new BookmarkItem("Hacker News", "https://news.ycombinator.com"));
            SaveBookmarks();
        }

        private bool TryLoadJson()
        {
            if (!File.Exists(_filePath)) return false;

            try
            {
                var root = Json.AsObject(Json.Parse(File.ReadAllText(_filePath)));
                var list = root == null ? null : Json.AsList(Get(root, "bookmarks"));
                if (list == null) return false;

                foreach (var entry in list)
                {
                    var obj = Json.AsObject(entry);
                    string url = Json.GetString(obj, "url");
                    if (string.IsNullOrEmpty(url)) continue;

                    string title = Json.GetString(obj, "title");
                    _bookmarks.Add(new BookmarkItem(
                        string.IsNullOrEmpty(title) ? url : title, url, Json.GetString(obj, "folder")));
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool TryLoadLegacy()
        {
            if (!File.Exists(_legacyPath)) return false;

            try
            {
                foreach (string line in File.ReadAllLines(_legacyPath))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    string[] parts = line.Split(new[] { '|' }, 2);
                    if (parts.Length == 2) _bookmarks.Add(new BookmarkItem(parts[0], parts[1]));
                }

                // Keep the old file as a safety net; the JSON copy is authoritative now.
                SaveBookmarks();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static object Get(Dictionary<string, object> obj, string key)
        {
            if (obj == null) return null;
            object value;
            return obj.TryGetValue(key, out value) ? value : null;
        }

        private void SaveBookmarks()
        {
            try
            {
                var list = new List<object>();
                foreach (var b in _bookmarks)
                {
                    list.Add(Json.Obj(
                        "title", b.Title ?? string.Empty,
                        "url", b.Url ?? string.Empty,
                        "folder", b.Folder ?? string.Empty));
                }

                string json = Json.Write(Json.Obj("version", 1, "bookmarks", list));
                if (json != null) File.WriteAllText(_filePath, json);
            }
            catch
            {
                // Ignore persistence errors silently.
            }
        }
    }
}
