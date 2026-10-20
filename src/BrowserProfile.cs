using System;
using System.Collections.Generic;
using System.IO;

namespace SimpleBrowser
{
    /// <summary>
    /// One importable profile belonging to another browser installed on this PC.
    /// </summary>
    public class BrowserProfile
    {
        public string Browser { get; set; }
        public string ProfileName { get; set; }
        public string Directory { get; set; }
        public bool IsChromium { get; set; }

        public string BookmarksFile
        {
            get { return Path.Combine(Directory, "Bookmarks"); }
        }

        public string HistoryFile
        {
            get { return Path.Combine(Directory, "History"); }
        }

        public string TopSitesFile
        {
            get { return Path.Combine(Directory, "Top Sites"); }
        }

        public string PlacesFile
        {
            get { return Path.Combine(Directory, "places.sqlite"); }
        }

        public bool HasBookmarks
        {
            get { return IsChromium ? File.Exists(BookmarksFile) : File.Exists(PlacesFile); }
        }

        public bool HasHistory
        {
            get { return IsChromium ? File.Exists(HistoryFile) : File.Exists(PlacesFile); }
        }

        public bool HasTopSites
        {
            get { return IsChromium && File.Exists(TopSitesFile); }
        }

        public string Display
        {
            get
            {
                if (string.IsNullOrEmpty(ProfileName) || ProfileName == "Default")
                {
                    return Browser;
                }
                return Browser + " \u2014 " + ProfileName;
            }
        }
    }

    /// <summary>
    /// Finds browsers that Toge can read data from. Chromium browsers expose
    /// bookmarks as JSON and history as SQLite; Firefox exposes both as SQLite.
    /// </summary>
    public static class BrowserProfiles
    {
        public static List<BrowserProfile> Detect()
        {
            var found = new List<BrowserProfile>();

            string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            AddChromium(found, "Google Chrome", Path.Combine(local, "Google", "Chrome", "User Data"));
            AddChromium(found, "Google Chrome Beta", Path.Combine(local, "Google", "Chrome Beta", "User Data"));
            AddChromium(found, "Microsoft Edge", Path.Combine(local, "Microsoft", "Edge", "User Data"));
            AddChromium(found, "Brave", Path.Combine(local, "BraveSoftware", "Brave-Browser", "User Data"));
            AddChromium(found, "Vivaldi", Path.Combine(local, "Vivaldi", "User Data"));
            AddChromium(found, "Opera", Path.Combine(roaming, "Opera Software", "Opera Stable"));
            AddChromium(found, "Opera GX", Path.Combine(roaming, "Opera Software", "Opera GX Stable"));
            AddFirefox(found, Path.Combine(roaming, "Mozilla", "Firefox", "Profiles"));

            return found;
        }

        private static void AddChromium(List<BrowserProfile> found, string browser, string userDataDirectory)
        {
            try
            {
                if (!Directory.Exists(userDataDirectory)) return;

                // Opera keeps the profile files directly in its user data folder.
                if (LooksLikeProfile(userDataDirectory))
                {
                    found.Add(new BrowserProfile
                    {
                        Browser = browser,
                        ProfileName = "Default",
                        Directory = userDataDirectory,
                        IsChromium = true
                    });
                    return;
                }

                if (!File.Exists(Path.Combine(userDataDirectory, "Local State"))) return;

                var names = ReadProfileNames(userDataDirectory);

                foreach (string directory in Directory.GetDirectories(userDataDirectory))
                {
                    string profileName = Path.GetFileName(directory);
                    if (profileName == "Guest Profile" || profileName == "System Profile") continue;
                    if (profileName != "Default" && !profileName.StartsWith("Profile ", StringComparison.OrdinalIgnoreCase)) continue;
                    if (!LooksLikeProfile(directory)) continue;

                    string displayName;
                    if (!names.TryGetValue(profileName, out displayName) || string.IsNullOrEmpty(displayName))
                    {
                        displayName = profileName;
                    }

                    found.Add(new BrowserProfile
                    {
                        Browser = browser,
                        ProfileName = displayName,
                        Directory = directory,
                        IsChromium = true
                    });
                }
            }
            catch
            {
                // A browser we cannot enumerate is simply not offered.
            }
        }

        /// <summary>Reads the friendly profile names from Local State.</summary>
        private static Dictionary<string, string> ReadProfileNames(string userDataDirectory)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                string path = Path.Combine(userDataDirectory, "Local State");
                var root = Json.AsObject(Json.Parse(File.ReadAllText(path)));
                var profile = Json.AsObject(root == null ? null : GetValue(root, "profile"));
                var cache = Json.AsObject(profile == null ? null : GetValue(profile, "info_cache"));
                if (cache == null) return result;

                foreach (var pair in cache)
                {
                    var entry = Json.AsObject(pair.Value);
                    string name = Json.GetString(entry, "name");
                    if (!string.IsNullOrEmpty(name)) result[pair.Key] = name;
                }
            }
            catch
            {
                // Names are cosmetic; fall back to the directory names.
            }
            return result;
        }

        private static object GetValue(Dictionary<string, object> obj, string key)
        {
            if (obj == null) return null;
            object value;
            return obj.TryGetValue(key, out value) ? value : null;
        }

        private static bool LooksLikeProfile(string directory)
        {
            try
            {
                return File.Exists(Path.Combine(directory, "Bookmarks"))
                    || File.Exists(Path.Combine(directory, "History"))
                    || File.Exists(Path.Combine(directory, "Preferences"));
            }
            catch
            {
                return false;
            }
        }

        private static void AddFirefox(List<BrowserProfile> found, string profilesDirectory)
        {
            try
            {
                if (!Directory.Exists(profilesDirectory)) return;

                foreach (string directory in Directory.GetDirectories(profilesDirectory))
                {
                    if (!File.Exists(Path.Combine(directory, "places.sqlite"))) continue;

                    string name = Path.GetFileName(directory);
                    int dot = name.IndexOf('.');
                    if (dot > 0) name = name.Substring(0, dot);

                    found.Add(new BrowserProfile
                    {
                        Browser = "Firefox",
                        ProfileName = name,
                        Directory = directory,
                        IsChromium = false
                    });
                }
            }
            catch
            {
                // Ignore.
            }
        }
    }
}
