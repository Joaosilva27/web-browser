using System;
using System.Collections.Generic;
using System.IO;

namespace SimpleBrowser
{
    /// <summary>One file the browser has downloaded, done or in progress.</summary>
    public class DownloadRecord
    {
        public string Url { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public long BytesReceived { get; set; }
        public long TotalBytes { get; set; }
        /// <summary>"progress", "done", "failed" or "cancelled".</summary>
        public string State { get; set; }
        /// <summary>Why an interrupted download stopped.</summary>
        public string Detail { get; set; }
        public DateTime StartedAt { get; set; }

        /// <summary>The live download, while it is running. Never persisted.</summary>
        public Microsoft.Web.WebView2.Core.CoreWebView2DownloadOperation Operation;

        public bool IsActive { get { return State == "progress"; } }
        public bool IsComplete { get { return State == "done"; } }

        /// <summary>True when the user stopped it for now, but it can go on.</summary>
        public bool IsPaused { get { return State == "paused"; } }

        /// <summary>How far along, from 0 to 1, or -1 when the size is unknown.</summary>
        public double Fraction
        {
            get
            {
                if (TotalBytes <= 0) return -1;
                double fraction = (double)BytesReceived / TotalBytes;
                if (fraction < 0) return 0;
                if (fraction > 1) return 1;
                return fraction;
            }
        }

        public string SizeText
        {
            get
            {
                string received = Format(BytesReceived);
                if (TotalBytes <= 0) return received;
                return received + " of " + Format(TotalBytes);
            }
        }

        public string StateText
        {
            get
            {
                if (State == "done") return "Completed";
                if (State == "cancelled") return "Cancelled";
                if (State == "paused") return "Paused";
                if (State == "failed") return string.IsNullOrEmpty(Detail) ? "Failed" : Detail;
                return "Downloading";
            }
        }

        public static string Format(long bytes)
        {
            if (bytes < 1024) return bytes + " B";

            double value = bytes / 1024.0;
            if (value < 1024) return value.ToString("0.0") + " KB";

            value = value / 1024.0;
            if (value < 1024) return value.ToString("0.0") + " MB";

            return (value / 1024.0).ToString("0.00") + " GB";
        }
    }

    /// <summary>
    /// The downloads list, shared by every window and remembered between runs
    /// (%APPDATA%\Toge\downloads.json). A download in progress is live in
    /// memory as well as on disk, so the list and the toolbar badge follow it.
    /// </summary>
    public class DownloadsManager
    {
        private static DownloadsManager _current;

        /// <summary>Raised whenever a download starts, advances or finishes.</summary>
        public event Action Changed;

        private readonly List<DownloadRecord> _items = new List<DownloadRecord>();
        private readonly string _filePath;
        private bool _loaded;

        public static DownloadsManager Current
        {
            get
            {
                if (_current == null) _current = new DownloadsManager();
                return _current;
            }
        }

        public DownloadsManager()
        {
            string appData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Toge");
            _filePath = Path.Combine(appData, "downloads.json");
        }

        /// <summary>The downloads, newest first.</summary>
        public List<DownloadRecord> GetItems()
        {
            Load();
            return _items;
        }

        public int ActiveCount
        {
            get
            {
                Load();
                int count = 0;
                foreach (var item in _items) if (item.IsActive) count++;
                return count;
            }
        }

        public DownloadRecord Add(string url, string fileName, string filePath, long totalBytes)
        {
            Load();

            var record = new DownloadRecord();
            record.Url = url;
            record.FileName = fileName;
            record.FilePath = filePath;
            record.TotalBytes = totalBytes;
            record.State = "progress";
            record.StartedAt = DateTime.Now;

            _items.Insert(0, record);
            Trim();
            SaveAndNotify();
            return record;
        }

        public void Remove(DownloadRecord record)
        {
            if (record == null) return;
            _items.Remove(record);
            Save();
            Notify();
        }

        /// <summary>Removes every finished download, keeping the running ones.</summary>
        public void ClearFinished()
        {
            Load();
            _items.RemoveAll(IsFinished);
            Save();
            Notify();
        }

        /// <summary>Tells the open lists and the toolbar badge to refresh.</summary>
        public void Notify()
        {
            if (Changed != null) Changed();
        }

        /// <summary>Refreshes every surface and writes the list to disk.</summary>
        public void SaveAndNotify()
        {
            Notify();
            Save();
        }

        /// <summary>Writes the list to disk, without the in-progress sizes.</summary>
        public void Save()
        {
            try
            {
                var rows = new List<object>();
                foreach (var item in _items)
                {
                    rows.Add(Json.Obj(
                        "url", item.Url,
                        "fileName", item.FileName,
                        "filePath", item.FilePath,
                        "bytesReceived", item.BytesReceived,
                        "totalBytes", item.TotalBytes,
                        "state", item.State,
                        "detail", item.Detail,
                        "startedAt", item.StartedAt.ToString("o")));
                }

                var root = Json.Obj("version", 1, "items", rows);
                string json = Json.Write(root);
                if (json == null) return;

                string folder = Path.GetDirectoryName(_filePath);
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                File.WriteAllText(_filePath, json);
            }
            catch
            {
                // Remembering downloads is best-effort.
            }
        }

        private static bool IsFinished(DownloadRecord record)
        {
            return record != null && !record.IsActive;
        }

        /// <summary>Reads a byte count, which can be larger than an int.</summary>
        private static long ReadNumber(Dictionary<string, object> map, string key)
        {
            string text = Json.GetString(map, key);
            if (string.IsNullOrEmpty(text)) return 0;

            long value;
            if (long.TryParse(text, out value)) return value;

            double asDouble;
            if (double.TryParse(text, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out asDouble))
            {
                return (long)asDouble;
            }

            return 0;
        }

        private static DateTime ReadDate(Dictionary<string, object> map, string key)
        {
            string text = Json.GetString(map, key);
            if (string.IsNullOrEmpty(text)) return DateTime.MinValue;

            DateTime value;
            if (DateTime.TryParse(text, System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.RoundtripKind, out value))
            {
                return value;
            }

            return DateTime.MinValue;
        }

        private void Load()
        {
            if (_loaded) return;
            _loaded = true;

            try
            {
                if (!File.Exists(_filePath)) return;

                var root = Json.AsObject(Json.Parse(File.ReadAllText(_filePath)));
                if (root == null) return;

                object raw;
                if (!root.TryGetValue("items", out raw)) return;

                var rows = Json.AsList(raw);
                if (rows == null) return;

                foreach (object row in rows)
                {
                    var map = row as Dictionary<string, object>;
                    if (map == null) continue;

                    var record = new DownloadRecord();
                    record.Url = Json.GetString(map, "url");
                    record.FileName = Json.GetString(map, "fileName");
                    record.FilePath = Json.GetString(map, "filePath");
                    record.BytesReceived = ReadNumber(map, "bytesReceived");
                    record.TotalBytes = ReadNumber(map, "totalBytes");
                    record.State = Json.GetString(map, "state");
                    record.Detail = Json.GetString(map, "detail");
                    record.StartedAt = ReadDate(map, "startedAt");

                    // A download that was running when Toge closed never
                    // finished, so it is shown as interrupted rather than live.
                    if (record.State == "progress" || record.State == "paused")
                    {
                        record.State = "failed";
                        record.Detail = "Interrupted";
                    }

                    _items.Add(record);
                }
            }
            catch
            {
                // A damaged file just means an empty list.
            }
        }

        private void Trim()
        {
            const int limit = 250;
            while (_items.Count > limit)
            {
                // Never drop something that is still running.
                int last = _items.Count - 1;
                if (_items[last].IsActive) break;
                _items.RemoveAt(last);
            }
        }
    }
}
