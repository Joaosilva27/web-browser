using System;
using System.Collections.Generic;
using System.IO;

namespace SimpleBrowser
{
    public class HistoryItem
    {
        public DateTime VisitedAt { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }

        public HistoryItem(DateTime visitedAt, string title, string url)
        {
            VisitedAt = visitedAt;
            Title = title;
            Url = url;
        }
    }

    public class SearchEntry
    {
        public DateTime SearchedAt { get; set; }
        public string Term { get; set; }
        public string Url { get; set; }

        public SearchEntry(DateTime searchedAt, string term, string url)
        {
            SearchedAt = searchedAt;
            Term = term;
            Url = url;
        }
    }

    public class HistoryManager
    {
        private readonly string _filePath;
        private readonly string _searchPath;
        private readonly List<HistoryItem> _history;
        private readonly List<SearchEntry> _searches;

        /// <summary>
        /// A generous bound rather than the old 500, which silently truncated
        /// imported history, but still finite so the file cannot grow forever.
        /// </summary>
        private const int MaxHistoryCount = 50000;
        private const int MaxSearchCount = 20000;

        public HistoryManager()
        {
            string appData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Toge");
            if (!Directory.Exists(appData))
            {
                Directory.CreateDirectory(appData);
            }

            _filePath = Path.Combine(appData, "history.txt");
            _searchPath = Path.Combine(appData, "searches.txt");
            _history = new List<HistoryItem>();
            _searches = new List<SearchEntry>();
            LoadHistory();
            LoadSearches();
        }

        public List<HistoryItem> GetHistory()
        {
            return new List<HistoryItem>(_history);
        }

        public List<SearchEntry> GetSearches()
        {
            return new List<SearchEntry>(_searches);
        }

        public void AddEntry(string title, string url)
        {
            if (string.IsNullOrEmpty(url) || url.StartsWith("about:") || url.StartsWith("javascript:")) return;
            if (string.IsNullOrEmpty(title)) title = url;

            // Remove adjacent duplicate if already at top
            if (_history.Count > 0 && string.Equals(_history[0].Url, url, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _history.Insert(0, new HistoryItem(DateTime.Now, title, url));
            if (_history.Count > MaxHistoryCount)
            {
                _history.RemoveAt(_history.Count - 1);
            }
            SaveHistory();
        }

        /// <summary>
        /// Merges imported entries, newest first. When an address is already
        /// known the newer visit wins, so re-importing is harmless.
        /// </summary>
        public int ImportHistory(IEnumerable<HistoryItem> items)
        {
            if (items == null) return 0;

            // Keep every local visit: the imported set must not erase what Toge saw.
            var byUrl = new Dictionary<string, HistoryItem>(StringComparer.OrdinalIgnoreCase);
            var merged = new List<HistoryItem>(_history);

            foreach (var existing in _history)
            {
                if (existing.Url == null) continue;
                byUrl[existing.Url] = existing;
            }

            int added = 0;
            foreach (var item in items)
            {
                if (item == null || string.IsNullOrEmpty(item.Url)) continue;
                if (item.Url.StartsWith("about:") || item.Url.StartsWith("javascript:")) continue;

                HistoryItem existing;
                if (byUrl.TryGetValue(item.Url, out existing))
                {
                    if (item.VisitedAt > existing.VisitedAt) existing.VisitedAt = item.VisitedAt;
                    continue;
                }

                byUrl[item.Url] = item;
                merged.Add(item);
                added++;
            }

            merged.Sort((a, b) => b.VisitedAt.CompareTo(a.VisitedAt));
            _history.Clear();
            for (int i = 0; i < merged.Count && i < MaxHistoryCount; i++)
            {
                _history.Add(merged[i]);
            }

            SaveHistory();
            return added;
        }

        public int ImportSearches(IEnumerable<SearchEntry> entries)
        {
            if (entries == null) return 0;

            var known = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in _searches) known.Add(SearchKey(entry));

            int added = 0;
            foreach (var entry in entries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.Term)) continue;
                if (!known.Add(SearchKey(entry))) continue;

                _searches.Add(entry);
                added++;
            }

            if (added > 0)
            {
                _searches.Sort((a, b) => b.SearchedAt.CompareTo(a.SearchedAt));
                while (_searches.Count > MaxSearchCount) _searches.RemoveAt(_searches.Count - 1);
                SaveSearches();
            }

            return added;
        }

        public void ClearHistory()
        {
            _history.Clear();
            SaveHistory();
        }

        public void ClearSearches()
        {
            _searches.Clear();
            SaveSearches();
        }

        private static string SearchKey(SearchEntry entry)
        {
            return (entry.Term ?? string.Empty) + "\u0001" + (entry.Url ?? string.Empty);
        }

        private void LoadHistory()
        {
            _history.Clear();
            if (!File.Exists(_filePath)) return;

            try
            {
                foreach (string line in File.ReadAllLines(_filePath))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    string[] parts = line.Split(new[] { '|' }, 3);
                    if (parts.Length != 3) continue;

                    DateTime when;
                    if (DateTime.TryParse(parts[0], out when))
                    {
                        _history.Add(new HistoryItem(when, parts[1], parts[2]));
                    }
                }
            }
            catch
            {
                // Ignore unreadable history.
            }
        }

        private void LoadSearches()
        {
            _searches.Clear();
            if (!File.Exists(_searchPath)) return;

            try
            {
                foreach (string line in File.ReadAllLines(_searchPath))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    string[] parts = line.Split(new[] { '|' }, 3);
                    if (parts.Length != 3) continue;

                    DateTime when;
                    if (DateTime.TryParse(parts[0], out when))
                    {
                        _searches.Add(new SearchEntry(when, parts[1], parts[2]));
                    }
                }
            }
            catch
            {
                // Ignore unreadable searches.
            }
        }

        private void SaveHistory()
        {
            try
            {
                var lines = new List<string>();
                foreach (var h in _history)
                {
                    lines.Add(h.VisitedAt.ToString("o") + "|" + Clean(h.Title) + "|" + Clean(h.Url));
                }
                File.WriteAllLines(_filePath, lines.ToArray());
            }
            catch
            {
                // Ignore persistence errors.
            }
        }

        private void SaveSearches()
        {
            try
            {
                var lines = new List<string>();
                foreach (var s in _searches)
                {
                    lines.Add(s.SearchedAt.ToString("o") + "|" + Clean(s.Term) + "|" + Clean(s.Url));
                }
                File.WriteAllLines(_searchPath, lines.ToArray());
            }
            catch
            {
                // Ignore persistence errors.
            }
        }

        /// <summary>The file format is pipe-delimited, so keep the separator out of fields.</summary>
        private static string Clean(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return text.Replace("|", "/").Replace("\r", " ").Replace("\n", " ");
        }
    }
}
