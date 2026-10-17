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

    public class HistoryManager
    {
        private readonly string _filePath;
        private readonly List<HistoryItem> _history;
        private const int MaxHistoryCount = 500;

        public HistoryManager()
        {
            string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AuraBrowser");
            if (!Directory.Exists(appData))
            {
                Directory.CreateDirectory(appData);
            }
            _filePath = Path.Combine(appData, "history.txt");
            _history = new List<HistoryItem>();
            LoadHistory();
        }

        public List<HistoryItem> GetHistory()
        {
            return new List<HistoryItem>(_history);
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

        public void ClearHistory()
        {
            _history.Clear();
            SaveHistory();
        }

        private void LoadHistory()
        {
            _history.Clear();
            if (File.Exists(_filePath))
            {
                var lines = File.ReadAllLines(_filePath);
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var parts = line.Split(new char[] { '|' }, 3);
                    if (parts.Length == 3)
                    {
                        DateTime dt;
                        if (DateTime.TryParse(parts[0], out dt))
                        {
                            _history.Add(new HistoryItem(dt, parts[1], parts[2]));
                        }
                    }
                }
            }
        }

        private void SaveHistory()
        {
            try
            {
                var lines = new List<string>();
                foreach (var h in _history)
                {
                    lines.Add(h.VisitedAt.ToString("o") + "|" + h.Title + "|" + h.Url);
                }
                File.WriteAllLines(_filePath, lines.ToArray());
            }
            catch
            {
                // Ignore persistence errors
            }
        }
    }
}
