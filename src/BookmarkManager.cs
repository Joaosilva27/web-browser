using System;
using System.Collections.Generic;
using System.IO;

namespace SimpleBrowser
{
    public class BookmarkItem
    {
        public string Title { get; set; }
        public string Url { get; set; }

        public BookmarkItem(string title, string url)
        {
            Title = title;
            Url = url;
        }
    }

    public class BookmarkManager
    {
        private readonly string _filePath;
        private readonly List<BookmarkItem> _bookmarks;

        public BookmarkManager()
        {
            string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AuraBrowser");
            if (!Directory.Exists(appData))
            {
                Directory.CreateDirectory(appData);
            }
            _filePath = Path.Combine(appData, "bookmarks.txt");
            _bookmarks = new List<BookmarkItem>();
            LoadBookmarks();
        }

        public List<BookmarkItem> GetBookmarks()
        {
            return new List<BookmarkItem>(_bookmarks);
        }

        public bool IsBookmarked(string url)
        {
            if (string.IsNullOrEmpty(url)) return false;
            foreach (var b in _bookmarks)
            {
                if (string.Equals(b.Url.TrimEnd('/'), url.TrimEnd('/'), StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        public void AddBookmark(string title, string url)
        {
            if (string.IsNullOrEmpty(url) || IsBookmarked(url)) return;
            if (string.IsNullOrEmpty(title)) title = url;
            _bookmarks.Add(new BookmarkItem(title, url));
            SaveBookmarks();
        }

        public void RemoveBookmark(string url)
        {
            _bookmarks.RemoveAll(b => string.Equals(b.Url.TrimEnd('/'), url.TrimEnd('/'), StringComparison.OrdinalIgnoreCase));
            SaveBookmarks();
        }

        private void LoadBookmarks()
        {
            _bookmarks.Clear();
            if (File.Exists(_filePath))
            {
                var lines = File.ReadAllLines(_filePath);
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var parts = line.Split(new char[] { '|' }, 2);
                    if (parts.Length == 2)
                    {
                        _bookmarks.Add(new BookmarkItem(parts[0], parts[1]));
                    }
                }
            }
            else
            {
                // Default starter bookmarks
                _bookmarks.Add(new BookmarkItem("✦ Apple", "https://apple.com"));
                _bookmarks.Add(new BookmarkItem("GitHub", "https://github.com"));
                _bookmarks.Add(new BookmarkItem("YouTube", "https://www.youtube.com"));
                _bookmarks.Add(new BookmarkItem("Wikipedia", "https://www.wikipedia.org"));
                _bookmarks.Add(new BookmarkItem("Reddit", "https://www.reddit.com"));
                _bookmarks.Add(new BookmarkItem("Hacker News", "https://news.ycombinator.com"));
                SaveBookmarks();
            }
        }

        private void SaveBookmarks()
        {
            try
            {
                var lines = new List<string>();
                foreach (var b in _bookmarks)
                {
                    lines.Add(b.Title + "|" + b.Url);
                }
                File.WriteAllLines(_filePath, lines.ToArray());
            }
            catch
            {
                // Ignore persistence errors silently
            }
        }
    }
}
