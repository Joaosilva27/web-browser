using System;
using System.Collections.Generic;
using System.IO;

namespace SimpleBrowser
{
    /// <summary>A search provider the address bar and the start page can use.</summary>
    public class SearchEngine
    {
        public string Id;
        public string Name;
        /// <summary>Short form used in sentences, so the start page does not say "Search Brave Search".</summary>
        public string ShortName;
        public string BaseUrl;
        public string QueryParam;

        public string Build(string query)
        {
            return BaseUrl + "?" + QueryParam + "=" + Uri.EscapeDataString(query);
        }
    }

    /// <summary>
    /// Everything the user can tune, in one file
    /// (%APPDATA%\AuraBrowser\settings.json).
    ///
    /// Values are kept as a loose dictionary rather than properties so that
    /// reading the file can never fail on a missing or mistyped entry: an
    /// unreadable value simply falls back to the default. The one exception is
    /// the theme, which is mirrored into Theme so the window chrome and the
    /// pages stay in step.
    /// </summary>
    public class Settings
    {
        private static Settings _current;

        public static Settings Current
        {
            get
            {
                if (_current == null) _current = new Settings();
                return _current;
            }
        }

        /// <summary>Raised after a value changes, so the app can apply it.</summary>
        public static event Action Changed;

        public static readonly SearchEngine[] Engines = new SearchEngine[]
        {
            new SearchEngine { Id = "brave", Name = "Brave Search", ShortName = "Brave", BaseUrl = "https://search.brave.com/search", QueryParam = "q" },
            new SearchEngine { Id = "google", Name = "Google", ShortName = "Google", BaseUrl = "https://www.google.com/search", QueryParam = "q" },
            new SearchEngine { Id = "duckduckgo", Name = "DuckDuckGo", ShortName = "DuckDuckGo", BaseUrl = "https://duckduckgo.com/", QueryParam = "q" },
            new SearchEngine { Id = "bing", Name = "Bing", ShortName = "Bing", BaseUrl = "https://www.bing.com/search", QueryParam = "q" },
            new SearchEngine { Id = "startpage", Name = "Startpage", ShortName = "Startpage", BaseUrl = "https://www.startpage.com/sp/search", QueryParam = "query" },
            new SearchEngine { Id = "ecosia", Name = "Ecosia", ShortName = "Ecosia", BaseUrl = "https://www.ecosia.org/search", QueryParam = "q" },
            new SearchEngine { Id = "youtube", Name = "YouTube", ShortName = "YouTube", BaseUrl = "https://www.youtube.com/results", QueryParam = "search_query" }
        };

        private readonly string _filePath;
        private readonly Dictionary<string, object> _values =
            new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        public Settings()
        {
            string appData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AuraBrowser");
            if (!Directory.Exists(appData))
            {
                Directory.CreateDirectory(appData);
            }

            _filePath = Path.Combine(appData, "settings.json");
            Load();
        }

        public string FilePath
        {
            get { return _filePath; }
        }

        // ── Values ───────────────────────────────────────────────────────────

        /// <summary>"light", "dark" or "system".</summary>
        public string Theme
        {
            get { return Json.GetString(_values, "theme") ?? "system"; }
        }

        public string SearchId
        {
            get { return Json.GetString(_values, "search") ?? "brave"; }
        }

        public SearchEngine Engine
        {
            get
            {
                string id = SearchId;
                foreach (var engine in Engines)
                {
                    if (string.Equals(engine.Id, id, StringComparison.OrdinalIgnoreCase)) return engine;
                }
                return Engines[0];
            }
        }

        /// <summary>Ads and trackers are blocked unless the user turns this off.</summary>
        public bool Shields
        {
            get { return Json.GetBool(_values, "shields", true); }
        }

        public bool SavePasswords
        {
            get { return Json.GetBool(_values, "savePasswords", true); }
        }

        public bool Autofill
        {
            get { return Json.GetBool(_values, "autofill", true); }
        }

        public bool ShowClock
        {
            get { return Json.GetBool(_values, "showClock", true); }
        }

        public bool ShowTiles
        {
            get { return Json.GetBool(_values, "showTiles", true); }
        }

        /// <summary>Page zoom as a percentage: 100 is one hundred percent.</summary>
        public int Zoom
        {
            get { return Math.Max(50, Math.Min(200, Json.GetInt(_values, "zoom", 100))); }
        }

        public float ZoomFactor
        {
            get { return Zoom / 100f; }
        }

        // ── Changing ─────────────────────────────────────────────────────────

        /// <summary>
        /// Sets one value from its settings-page key, persists it and lets the
        /// app apply it. Unknown keys are ignored rather than stored, so a page
        /// cannot fill the file with junk.
        /// </summary>
        public bool Set(string key, object value)
        {
            if (string.IsNullOrEmpty(key)) return false;

            switch (key.ToLowerInvariant())
            {
                case "theme":
                    {
                        string mode = value as string;
                        if (mode != "light" && mode != "dark" && mode != "system") return false;
                        _values["theme"] = mode;
                        Save();
                        SimpleBrowser.Theme.SetPreference(mode);
                        Raise();
                        return true;
                    }
                case "search":
                    {
                        string id = value as string;
                        if (Find(id) == null) return false;
                        _values["search"] = Find(id).Id;
                        break;
                    }
                case "shields":
                    _values["shields"] = AsBool(value, Shields);
                    break;
                case "savepasswords":
                    _values["savePasswords"] = AsBool(value, SavePasswords);
                    break;
                case "autofill":
                    _values["autofill"] = AsBool(value, Autofill);
                    break;
                case "showclock":
                    _values["showClock"] = AsBool(value, ShowClock);
                    break;
                case "showtiles":
                    _values["showTiles"] = AsBool(value, ShowTiles);
                    break;
                case "zoom":
                    {
                        int percent = AsInt(value, Zoom);
                        _values["zoom"] = Math.Max(50, Math.Min(200, RoundToStep(percent)));
                        break;
                    }
                default:
                    return false;
            }

            Save();
            Raise();
            return true;
        }

        /// <summary>Pushes the stored theme into Theme, used once at startup.</summary>
        public void ApplyThemePreference()
        {
            SimpleBrowser.Theme.SetPreference(Theme);
        }

        public void ResetToDefaults()
        {
            _values.Clear();
            ApplyDefaults();
            Save();

            SimpleBrowser.Theme.SetPreference(Theme);
            Raise();
        }

        public static SearchEngine Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var engine in Engines)
            {
                if (string.Equals(engine.Id, id, StringComparison.OrdinalIgnoreCase)) return engine;
            }
            return null;
        }

        private static void Raise()
        {
            if (Changed != null) Changed();
        }

        private static bool AsBool(object value, bool fallback)
        {
            if (value == null) return fallback;
            if (value is bool) return (bool)value;

            string text = value as string;
            if (text != null)
            {
                bool parsed;
                if (bool.TryParse(text, out parsed)) return parsed;
                return fallback;
            }

            var convertible = value as IConvertible;
            if (convertible != null)
            {
                try { return convertible.ToBoolean(System.Globalization.CultureInfo.InvariantCulture); }
                catch { }
            }
            return fallback;
        }

        private static int AsInt(object value, int fallback)
        {
            if (value == null) return fallback;

            string text = value as string;
            if (text != null)
            {
                int parsed;
                if (int.TryParse(text, out parsed)) return parsed;
                return fallback;
            }

            var convertible = value as IConvertible;
            if (convertible != null)
            {
                try { return (int)Math.Round(convertible.ToDouble(System.Globalization.CultureInfo.InvariantCulture)); }
                catch { }
            }
            return fallback;
        }

        /// <summary>Zoom reads better in steps of ten.</summary>
        private static int RoundToStep(int percent)
        {
            return (int)(Math.Round(percent / 10.0) * 10);
        }

        // ── Storage ──────────────────────────────────────────────────────────

        private void ApplyDefaults()
        {
            _values["theme"] = "system";
            _values["search"] = "brave";
            _values["shields"] = true;
            _values["savePasswords"] = true;
            _values["autofill"] = true;
            _values["showClock"] = true;
            _values["showTiles"] = true;
            _values["zoom"] = 100;
        }

        private void Load()
        {
            ApplyDefaults();

            try
            {
                if (File.Exists(_filePath))
                {
                    var stored = Json.AsObject(Json.Parse(File.ReadAllText(_filePath)));
                    if (stored != null)
                    {
                        foreach (var pair in stored)
                        {
                            _values[pair.Key] = pair.Value;
                        }
                    }
                }
                else
                {
                    // First run on this version: carry over the theme that used to
                    // live in the registry, then leave the registry mirror alone.
                    string legacy = SimpleBrowser.Theme.ReadStoredPreference();
                    if (legacy != null) _values["theme"] = legacy;
                    Save();
                }
            }
            catch
            {
                // A damaged settings file just means the defaults are used.
            }
        }

        private void Save()
        {
            try
            {
                var output = new Dictionary<string, object>(_values, StringComparer.OrdinalIgnoreCase);
                output["version"] = 1;

                string json = Json.Write(output);
                if (json != null) File.WriteAllText(_filePath, json);
            }
            catch
            {
                // Persisting is best-effort; the running app keeps the value.
            }
        }
    }
}
