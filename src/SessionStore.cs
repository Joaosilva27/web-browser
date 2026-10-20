using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace SimpleBrowser
{
    /// <summary>One window's worth of tabs, as remembered between runs.</summary>
    public class WindowSession
    {
        public readonly List<string> Tabs = new List<string>();
        public int Active;

        public WindowSession()
        {
        }

        public WindowSession(List<string> tabs, int active)
        {
            if (tabs != null) Tabs.AddRange(tabs);
            Active = active;
        }
    }

    /// <summary>
    /// Remembers the tabs that were open, so Toge can offer to bring them back
    /// after a crash and continue where the user left off if that is what they
    /// asked for on the settings page.
    ///
    /// The file (%APPDATA%\Toge\session.json) also carries a "clean"
    /// flag: it is written false the moment a session starts and true only when
    /// every window has closed in an orderly way. A file that is still false at
    /// startup is what a crash looks like.
    /// </summary>
    public static class SessionStore
    {
        private static readonly object Gate = new object();
        private static bool _restored;
        private static Timer _timer;

        private static string FilePath
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Toge",
                    "session.json");
            }
        }

        /// <summary>
        /// The tabs to open this run, or null when the start page should be
        /// shown instead. Only the first window of the run ever gets them.
        /// </summary>
        public static List<WindowSession> Restore()
        {
            lock (Gate)
            {
                if (_restored) return null;
                _restored = true;

                try
                {
                    if (!File.Exists(FilePath)) return null;

                    var root = Json.AsObject(Json.Parse(File.ReadAllText(FilePath)));
                    if (root == null) return null;

                    bool clean = Json.GetBool(root, "clean", true);
                    bool always = Settings.Current.StartupContinue;

                    // A session left behind by a crash is offered back even when
                    // the user asked for the start page: losing work is worse
                    // than an unexpected restore.
                    if (clean && !always) return null;

                    var windows = new List<WindowSession>();
                    object rawWindows;
                    if (root.TryGetValue("windows", out rawWindows))
                    {
                        var list = rawWindows as System.Collections.IEnumerable;
                        if (list != null)
                        {
                            foreach (object entry in list)
                            {
                                var map = entry as Dictionary<string, object>;
                                if (map == null) continue;

                                var session = new WindowSession();
                                session.Active = Json.GetInt(map, "active", 0);

                                object rawTabs;
                                if (map.TryGetValue("tabs", out rawTabs))
                                {
                                    var tabs = rawTabs as System.Collections.IEnumerable;
                                    if (tabs != null)
                                    {
                                        foreach (object tab in tabs)
                                        {
                                            string url = tab as string;
                                            if (!string.IsNullOrEmpty(url)) session.Tabs.Add(url);
                                        }
                                    }
                                }

                                if (session.Tabs.Count > 0) windows.Add(session);
                            }
                        }
                    }

                    return windows.Count > 0 ? windows : null;
                }
                catch
                {
                    // A damaged session file just means a fresh start.
                    return null;
                }
            }
        }

        /// <summary>
        /// Queues a save of the tabs every window is showing. Saves are
        /// coalesced, because they happen on every tab change.
        /// </summary>
        public static void Touch()
        {
            lock (Gate)
            {
                if (_timer == null)
                {
                    _timer = new Timer();
                    _timer.Interval = 1200;
                    _timer.Tick += delegate
                    {
                        _timer.Stop();
                        Save();
                    };
                }

                _timer.Stop();
                _timer.Start();
            }
        }

        /// <summary>Writes the state of every normal window right now.</summary>
        public static void Save()
        {
            try
            {
                var windows = new List<WindowSession>();

                foreach (var window in MainForm.Windows)
                {
                    if (window == null || window.IsPrivate) continue;

                    var session = window.CaptureTabs();
                    if (session != null && session.Tabs.Count > 0) windows.Add(session);
                }

                var root = Json.Obj("windows", ToJsonList(windows), "clean", false, "version", 1);

                string folder = Path.GetDirectoryName(FilePath);
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                string json = Json.Write(root);
                if (json != null) File.WriteAllText(FilePath, json);
            }
            catch
            {
                // Remembering the session is best-effort.
            }
        }

        /// <summary>Marks the session as closed in an orderly way.</summary>
        public static void MarkClean()
        {
            lock (Gate)
            {
                if (_timer != null) _timer.Stop();
            }

            Save();
            SetCleanFlag(true);
        }

        /// <summary>Forgets the remembered session entirely.</summary>
        public static void Clear()
        {
            lock (Gate)
            {
                if (_timer != null) _timer.Stop();
            }

            try
            {
                if (File.Exists(FilePath)) File.Delete(FilePath);
            }
            catch
            {
                // Nothing to forget.
            }
        }

        private static void SetCleanFlag(bool clean)
        {
            try
            {
                if (!File.Exists(FilePath)) return;

                var root = Json.AsObject(Json.Parse(File.ReadAllText(FilePath)));
                if (root == null) return;

                root["clean"] = clean;
                string json = Json.Write(root);
                if (json != null) File.WriteAllText(FilePath, json);
            }
            catch
            {
                // The flag is a hint, never a hard requirement.
            }
        }

        private static object ToJsonList(List<WindowSession> windows)
        {
            var list = new List<object>();
            foreach (var window in windows)
            {
                var tabs = new List<object>();
                foreach (string url in window.Tabs) tabs.Add(url);

                list.Add(Json.Obj("tabs", tabs, "active", window.Active));
            }
            return list;
        }
    }
}
