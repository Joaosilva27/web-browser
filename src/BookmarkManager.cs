using System;
using System.Collections.Generic;
using System.IO;

namespace SimpleBrowser
{
    /// <summary>One saved page.</summary>
    public class BookmarkItem
    {
        public string Title { get; set; }
        public string Url { get; set; }

        /// <summary>Slash-separated folder path, empty for the top level.</summary>
        public string Folder { get; set; }

        /// <summary>
        /// The page's favicon, kept as a data URL so the bookmarks bar can draw
        /// the real icon. Null when the site never offered one, in which case the
        /// bar falls back to a coloured initial.
        /// </summary>
        public string Icon { get; set; }

        public BookmarkItem(string title, string url)
            : this(title, url, null, null)
        {
        }

        public BookmarkItem(string title, string url, string folder)
            : this(title, url, folder, null)
        {
        }

        public BookmarkItem(string title, string url, string folder, string icon)
        {
            Title = title;
            Url = url;
            Folder = BookmarkManager.Normalize(folder);
            Icon = string.IsNullOrEmpty(icon) ? null : icon;
        }
    }

    /// <summary>
    /// Aura's bookmarks, stored as JSON so titles and addresses may contain any
    /// character and so the folder structure survives.
    ///
    /// Folders are paths ("Reading", "Reading/News") held in their own list as
    /// well as on every bookmark, so a folder that is empty still exists after a
    /// restart. Older installs have a pipe-delimited bookmarks.txt, which is
    /// migrated once on first load.
    /// </summary>
    public class BookmarkManager
    {
        /// <summary>Display name for the top level, which has no path.</summary>
        public const string RootFolder = "Bookmarks";

        private readonly string _filePath;
        private readonly string _legacyPath;
        private readonly List<BookmarkItem> _bookmarks;
        private readonly List<string> _folders;

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
            _folders = new List<string>();
            LoadBookmarks();
        }

        /// <summary>Raised after anything is added, moved or removed.</summary>
        public event Action Changed;

        // ── Reading ──────────────────────────────────────────────────────────

        public List<BookmarkItem> GetBookmarks()
        {
            return new List<BookmarkItem>(_bookmarks);
        }

        /// <summary>Bookmarks directly inside one folder, or in it and below it.</summary>
        public List<BookmarkItem> GetBookmarks(string folder, bool includeSubfolders)
        {
            string path = Normalize(folder);
            var result = new List<BookmarkItem>();

            foreach (var bookmark in _bookmarks)
            {
                string at = Normalize(bookmark.Folder);
                if (includeSubfolders)
                {
                    if (IsSameOrInside(at, path)) result.Add(bookmark);
                }
                else if (string.Equals(at, path, StringComparison.OrdinalIgnoreCase))
                {
                    result.Add(bookmark);
                }
            }

            return result;
        }

        /// <summary>Bookmarks sitting at the top level, in insertion order.</summary>
        public List<BookmarkItem> GetRootBookmarks()
        {
            return GetBookmarks(null, false);
        }

        /// <summary>Every folder, top level first, ordered so parents come before children.</summary>
        public List<string> GetFolders()
        {
            return GetAllFolders();
        }

        /// <summary>Every folder, deepest first — the order to delete them safely in.</summary>
        public List<string> GetFoldersDeepestFirst()
        {
            var folders = GetAllFolders();
            folders.Reverse();
            return folders;
        }

        /// <summary>Direct subfolders of one folder.</summary>
        public List<string> GetChildFolders(string parent)
        {
            string path = Normalize(parent);
            var result = new List<string>();

            foreach (string folder in GetAllFolders())
            {
                if (folder.Length == 0) continue;
                if (!string.Equals(ParentOf(folder), path, StringComparison.OrdinalIgnoreCase)) continue;
                result.Add(folder);
            }

            return result;
        }

        /// <summary>How many bookmarks a folder holds, optionally counting nested ones.</summary>
        public int CountBookmarks(string folder, bool includeSubfolders)
        {
            return GetBookmarks(folder, includeSubfolders).Count;
        }

        public bool FolderExists(string folder)
        {
            string path = Normalize(folder);
            if (path.Length == 0) return true;

            foreach (string folder2 in GetAllFolders())
            {
                if (string.Equals(folder2, path, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
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

        /// <summary>The bookmark for an address, or null.</summary>
        public BookmarkItem Find(string url)
        {
            if (string.IsNullOrEmpty(url)) return null;
            foreach (var b in _bookmarks)
            {
                if (SameUrl(b.Url, url)) return b;
            }
            return null;
        }

        // ── Folder paths ─────────────────────────────────────────────────────

        /// <summary>Cleans a folder path: no stray separators, no empty segments.</summary>
        public static string Normalize(string folder)
        {
            if (string.IsNullOrEmpty(folder)) return string.Empty;

            var parts = folder.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            return string.Join("/", parts);
        }

        /// <summary>The folder's own name, without its parents.</summary>
        public static string NameOf(string folder)
        {
            string path = Normalize(folder);
            int slash = path.LastIndexOf('/');
            return slash < 0 ? path : path.Substring(slash + 1);
        }

        /// <summary>The folder that contains this one; empty for a top-level folder.</summary>
        public static string ParentOf(string folder)
        {
            string path = Normalize(folder);
            int slash = path.LastIndexOf('/');
            return slash < 0 ? string.Empty : path.Substring(0, slash);
        }

        /// <summary>Joins a parent folder and a child name into one path.</summary>
        public static string Combine(string parent, string child)
        {
            string head = Normalize(parent);
            string leaf = Normalize(child);
            if (head.Length == 0) return leaf;
            if (leaf.Length == 0) return head;
            return head + "/" + leaf;
        }

        /// <summary>True when <paramref name="folder"/> is the ancestor or a descendant of it.</summary>
        public static bool IsSameOrInside(string folder, string ancestor)
        {
            string path = Normalize(folder);
            string root = Normalize(ancestor);

            if (root.Length == 0) return true;   // everything lives in the top level
            if (string.Equals(path, root, StringComparison.OrdinalIgnoreCase)) return true;
            return path.StartsWith(root + "/", StringComparison.OrdinalIgnoreCase);
        }

        // ── Changing ─────────────────────────────────────────────────────────

        public void AddBookmark(string title, string url)
        {
            AddBookmark(title, url, null, null);
        }

        public void AddBookmark(string title, string url, string folder)
        {
            AddBookmark(title, url, folder, null);
        }

        public void AddBookmark(string title, string url, string folder, string icon)
        {
            if (string.IsNullOrEmpty(url) || IsBookmarked(url)) return;
            if (string.IsNullOrEmpty(title)) title = url;

            string path = Normalize(folder);
            EnsureFolder(path);
            _bookmarks.Add(new BookmarkItem(title, url, path, icon));
            SaveBookmarks();
            Raise();
        }

        /// <summary>Adds a bookmark, replacing the stored one for the same address.</summary>
        public void SetBookmark(string title, string url, string folder, string icon)
        {
            if (string.IsNullOrEmpty(url)) return;

            var existing = Find(url);
            if (existing != null) _bookmarks.Remove(existing);

            if (string.IsNullOrEmpty(title)) title = url;
            string path = Normalize(folder);
            EnsureFolder(path);
            _bookmarks.Add(new BookmarkItem(title, url, path, icon));

            SaveBookmarks();
            Raise();
        }

        /// <summary>
        /// Adds many bookmarks at once. Duplicates (by address) are skipped, the
        /// folders they came in are kept, and the file is written once. Returns
        /// how many were actually added.
        /// </summary>
        public int ImportBookmarks(IEnumerable<BookmarkItem> items)
        {
            if (items == null) return 0;

            var known = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var b in _bookmarks) known.Add(NormalizeUrl(b.Url));

            int added = 0;
            foreach (var item in items)
            {
                if (item == null || string.IsNullOrEmpty(item.Url)) continue;
                if (!known.Add(NormalizeUrl(item.Url))) continue;

                string path = Normalize(item.Folder);
                EnsureFolder(path);
                _bookmarks.Add(new BookmarkItem(
                    string.IsNullOrEmpty(item.Title) ? item.Url : item.Title,
                    item.Url,
                    path,
                    item.Icon));
                added++;
            }

            if (added > 0)
            {
                SaveBookmarks();
                Raise();
            }
            return added;
        }

        public void RemoveBookmark(string url)
        {
            int removed = _bookmarks.RemoveAll(b => SameUrl(b.Url, url));
            if (removed == 0) return;

            SaveBookmarks();
            Raise();
        }

        /// <summary>Replaces a bookmark's title, keeping it where it is.</summary>
        public bool RenameBookmark(string url, string title)
        {
            var bookmark = Find(url);
            if (bookmark == null) return false;
            if (string.IsNullOrEmpty(title) || title == bookmark.Title) return false;

            bookmark.Title = title;
            SaveBookmarks();
            Raise();
            return true;
        }

        /// <summary>Moves a bookmark into another folder, creating it if needed.</summary>
        public bool MoveBookmark(string url, string folder)
        {
            var bookmark = Find(url);
            if (bookmark == null) return false;

            string path = Normalize(folder);
            if (string.Equals(Normalize(bookmark.Folder), path, StringComparison.OrdinalIgnoreCase)) return false;

            EnsureFolder(path);
            bookmark.Folder = path;
            SaveBookmarks();
            Raise();
            return true;
        }

        /// <summary>
        /// Creates a folder inside another one. An existing folder of the same
        /// name is returned rather than duplicated, so adding a folder is safe to
        /// repeat.
        /// </summary>
        public string CreateFolder(string parent, string name)
        {
            string leaf = CleanName(name);
            if (leaf.Length == 0) return string.Empty;

            string parentPath = Normalize(parent);
            if (parentPath.Length > 0) EnsureFolder(parentPath);

            string path = Combine(parentPath, leaf);
            EnsureFolder(path);
            SaveBookmarks();
            Raise();
            return path;
        }

        /// <summary>Adds a child folder name that is not taken yet: "News", "News 2", "News 3".</summary>
        public string UniqueChildName(string parent, string name)
        {
            string leaf = CleanName(name);
            if (leaf.Length == 0) leaf = "New Folder";

            string path = Combine(Normalize(parent), leaf);
            int suffix = 2;
            while (FolderExists(path))
            {
                path = Combine(Normalize(parent), leaf + " " + suffix);
                suffix++;
            }
            return NameOf(path);
        }

        /// <summary>Renames a folder and everything inside it. False when the name is taken.</summary>
        public bool RenameFolder(string folder, string newName)
        {
            string path = Normalize(folder);
            if (path.Length == 0) return false;

            string leaf = CleanName(newName);
            if (leaf.Length == 0) return false;

            string target = Combine(ParentOf(path), leaf);
            if (string.Equals(target, path, StringComparison.OrdinalIgnoreCase)) return false;
            if (FolderExists(target)) return false;

            for (int i = 0; i < _folders.Count; i++)
            {
                _folders[i] = Rewrite(_folders[i], path, target);
            }

            foreach (var bookmark in _bookmarks)
            {
                bookmark.Folder = Rewrite(Normalize(bookmark.Folder), path, target);
            }

            SaveBookmarks();
            Raise();
            return true;
        }

        /// <summary>Deletes a folder, its subfolders and every bookmark inside them.</summary>
        public void DeleteFolder(string folder)
        {
            string path = Normalize(folder);
            if (path.Length == 0) return;

            _bookmarks.RemoveAll(b => IsSameOrInside(Normalize(b.Folder), path));
            _folders.RemoveAll(f => IsSameOrInside(Normalize(f), path));

            SaveBookmarks();
            Raise();
        }

        /// <summary>Moves every bookmark of a folder into another one.</summary>
        public void MoveFolder(string folder, string destination)
        {
            string path = Normalize(folder);
            if (path.Length == 0) return;

            string target = Normalize(destination);
            if (IsSameOrInside(target, path)) return;   // would swallow itself

            for (int i = 0; i < _folders.Count; i++)
            {
                _folders[i] = Rewrite(_folders[i], path, Combine(target, NameOf(path)));
            }

            foreach (var bookmark in _bookmarks)
            {
                bookmark.Folder = Rewrite(Normalize(bookmark.Folder), path, Combine(target, NameOf(path)));
            }

            SaveBookmarks();
            Raise();
        }

        // ── Internals ────────────────────────────────────────────────────────

        /// <summary>Every known folder, path-ordered so a parent precedes its children.</summary>
        private List<string> GetAllFolders()
        {
            var seen = new List<string>();
            var known = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string folder in _folders)
            {
                string path = Normalize(folder);
                if (path.Length > 0 && known.Add(path)) seen.Add(path);
            }

            foreach (var bookmark in _bookmarks)
            {
                string path = Normalize(bookmark.Folder);
                if (path.Length > 0 && known.Add(path)) seen.Add(path);
            }

            // A path implies its parents exist, even if nothing else put them there.
            int count = seen.Count;
            for (int i = 0; i < count; i++)
            {
                string parent = ParentOf(seen[i]);
                while (parent.Length > 0)
                {
                    if (known.Add(parent)) seen.Add(parent);
                    parent = ParentOf(parent);
                }
            }

            seen.Sort(delegate (string a, string b)
            {
                return string.Compare(a, b, StringComparison.CurrentCultureIgnoreCase);
            });

            return seen;
        }

        /// <summary>Registers a folder and every folder above it.</summary>
        private void EnsureFolder(string path)
        {
            string current = Normalize(path);
            while (current.Length > 0)
            {
                if (!ContainsFolder(current)) _folders.Add(current);
                current = ParentOf(current);
            }
        }

        private bool ContainsFolder(string path)
        {
            foreach (string folder in _folders)
            {
                if (string.Equals(folder, path, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        /// <summary>Replaces a folder prefix in a path, leaving anything else alone.</summary>
        private static string Rewrite(string value, string from, string to)
        {
            string path = Normalize(value);
            if (!IsSameOrInside(path, from)) return path;
            return to + path.Substring(Normalize(from).Length);
        }

        /// <summary>A folder name without separators or surrounding space.</summary>
        private static string CleanName(string name)
        {
            if (string.IsNullOrEmpty(name)) return string.Empty;
            return name.Trim().Trim('/').Trim();
        }

        private void Raise()
        {
            if (Changed != null) Changed();
        }

        private static bool SameUrl(string a, string b)
        {
            return string.Equals((a ?? string.Empty).TrimEnd('/'), (b ?? string.Empty).TrimEnd('/'),
                StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeUrl(string url)
        {
            return (url ?? string.Empty).TrimEnd('/');
        }

        // ── Storage ──────────────────────────────────────────────────────────

        private void LoadBookmarks()
        {
            _bookmarks.Clear();
            _folders.Clear();

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

                var storedFolders = Json.AsList(Get(root, "folders"));
                if (storedFolders != null)
                {
                    foreach (var entry in storedFolders)
                    {
                        string path = Normalize(Json.Str(entry));
                        if (path.Length > 0) EnsureFolder(path);
                    }
                }

                foreach (var entry in list)
                {
                    var obj = Json.AsObject(entry);
                    string url = Json.GetString(obj, "url");
                    if (string.IsNullOrEmpty(url)) continue;

                    string title = Json.GetString(obj, "title");
                    string folder = Json.GetString(obj, "folder");
                    _bookmarks.Add(new BookmarkItem(
                        string.IsNullOrEmpty(title) ? url : title, url, folder,
                        Json.GetString(obj, "icon")));

                    EnsureFolder(folder);
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
                    var entry = Json.Obj(
                        "title", b.Title ?? string.Empty,
                        "url", b.Url ?? string.Empty,
                        "folder", b.Folder ?? string.Empty);

                    if (!string.IsNullOrEmpty(b.Icon)) entry["icon"] = b.Icon;
                    list.Add(entry);
                }

                var folders = new List<object>();
                foreach (string folder in GetAllFolders())
                {
                    folders.Add(folder);
                }

                string json = Json.Write(Json.Obj(
                    "version", 2,
                    "folders", folders,
                    "bookmarks", list));
                if (json != null) File.WriteAllText(_filePath, json);
            }
            catch
            {
                // Ignore persistence errors silently.
            }
        }
    }
}
