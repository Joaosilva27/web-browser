using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Microsoft.Web.WebView2.Core;

namespace SimpleBrowser
{
    /// <summary>One of Aura's built-in wallpaper gradients.</summary>
    public class WallpaperPreset
    {
        public string Id;
        public string Name;
        public string Css;

        /// <summary>True when the preset is dark enough to want light text on it.</summary>
        public bool Dark;
    }

    /// <summary>
    /// Wallpapers for the start page.
    ///
    /// Pictures are copied into %APPDATA%\AuraBrowser\wallpapers and served back
    /// to the page through a WebView2 virtual host, so the start page can just
    /// point a CSS background at them. That keeps multi-megabyte base64 out of
    /// the generated HTML, which is what stuffing a photo into a data: URL would
    /// have cost.
    /// </summary>
    public static class HomeWallpaper
    {
        /// <summary>The host name the wallpapers folder is published under.</summary>
        public const string VirtualHost = "aura.wall";

        public static readonly WallpaperPreset[] Gradients = new WallpaperPreset[]
        {
            new WallpaperPreset { Id = "dusk",     Name = "Dusk",     Dark = true,  Css = "linear-gradient(160deg, #1b2140 0%, #3a2a5c 48%, #6b3f6e 100%)" },
            new WallpaperPreset { Id = "ember",    Name = "Ember",    Dark = true,  Css = "linear-gradient(160deg, #1f1c2c 0%, #4a2c4f 46%, #c33764 100%)" },
            new WallpaperPreset { Id = "ocean",    Name = "Ocean",    Dark = true,  Css = "linear-gradient(160deg, #0f2027 0%, #203a43 46%, #2c5364 100%)" },
            new WallpaperPreset { Id = "forest",   Name = "Forest",   Dark = true,  Css = "linear-gradient(160deg, #10312b 0%, #1f6f5c 52%, #57a773 100%)" },
            new WallpaperPreset { Id = "graphite", Name = "Graphite", Dark = true,  Css = "linear-gradient(160deg, #1c1c1e 0%, #3a3a3c 100%)" },
            new WallpaperPreset { Id = "sunset",   Name = "Sunset",   Dark = false, Css = "linear-gradient(160deg, #ff9966 0%, #ff5e62 55%, #b8336a 100%)" },
            new WallpaperPreset { Id = "sand",     Name = "Sand",     Dark = false, Css = "linear-gradient(160deg, #f6d365 0%, #fda085 100%)" },
            new WallpaperPreset { Id = "sky",      Name = "Sky",      Dark = false, Css = "linear-gradient(160deg, #a1c4fd 0%, #c2e9fb 100%)" },
            new WallpaperPreset { Id = "lilac",    Name = "Lilac",    Dark = false, Css = "linear-gradient(160deg, #e0c3fc 0%, #8ec5fc 100%)" },
            new WallpaperPreset { Id = "blossom",  Name = "Blossom",  Dark = false, Css = "linear-gradient(160deg, #ffdde1 0%, #ee9ca7 100%)" }
        };

        /// <summary>Soft solids, a mix of light surfaces and deep ones.</summary>
        public static readonly string[] Colors = new string[]
        {
            "#f5f5f7", "#e8eaf0", "#dbe8ff", "#e6f4ea", "#fdecef", "#fff4e6",
            "#ece6ff", "#1c1c1e", "#2c2c2e", "#11324d", "#1f3b2c", "#3a2a3f"
        };

        /// <summary>The wallpapers folder; created on first use.</summary>
        public static string Folder
        {
            get
            {
                string appData = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AuraBrowser");
                string folder = Path.Combine(appData, "wallpapers");

                try
                {
                    if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                }
                catch
                {
                    // If it cannot be made, the page simply shows no picture.
                }

                return folder;
            }
        }

        public static WallpaperPreset Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            foreach (var preset in Gradients)
            {
                if (string.Equals(preset.Id, id, StringComparison.OrdinalIgnoreCase)) return preset;
            }
            return null;
        }

        /// <summary>Points the virtual host at the wallpapers folder for one WebView.</summary>
        public static void ApplyMapping(CoreWebView2 core)
        {
            if (core == null) return;

            try
            {
                core.SetVirtualHostNameToFolderMapping(
                    VirtualHost, Folder, CoreWebView2HostResourceAccessKind.Allow);
            }
            catch
            {
                // Without the mapping a custom picture stays blank; nothing else breaks.
            }
        }

        /// <summary>Copies a picture the user picked into the wallpapers folder.</summary>
        public static string Import(string sourcePath)
        {
            try
            {
                if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath)) return null;

                string extension = Path.GetExtension(sourcePath);
                if (string.IsNullOrEmpty(extension)) extension = ".jpg";
                extension = extension.ToLowerInvariant();

                string name = "wall-" + DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) +
                    "-" + Guid.NewGuid().ToString("N").Substring(0, 6) + extension;

                File.Copy(sourcePath, Path.Combine(Folder, name), true);
                return name;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Deletes one of our own wallpaper files, never anything else.</summary>
        public static void Delete(string name)
        {
            if (!IsLocalName(name)) return;

            try
            {
                string path = Path.Combine(Folder, name);
                if (File.Exists(path)) File.Delete(path);
            }
            catch
            {
                // Left behind on failure; harmless.
            }
        }

        /// <summary>
        /// Drops a stored picture once nothing points at it any more. Called after
        /// the wallpaper changes so pictures the user tried and moved on from do
        /// not pile up in the wallpapers folder.
        /// </summary>
        public static void Tidy(string previous)
        {
            if (string.IsNullOrEmpty(previous)) return;
            if (previous.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return;
            if (previous.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) return;

            // Still the picture on show? Then it is wanted.
            if (Settings.Current.WallpaperType == "image" &&
                string.Equals(Settings.Current.WallpaperValue, previous, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            Delete(previous);
        }

        /// <summary>Address the start page should use for the stored picture.</summary>
        public static string ResolveImageUrl(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;

            if (value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                value.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                value.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                return value;
            }

            if (!IsLocalName(value)) return string.Empty;
            return "https://" + VirtualHost + "/" + value;
        }

        /// <summary>The CSS background for the non-picture wallpaper kinds.</summary>
        public static string CssFor(string type, string value)
        {
            if (type == "gradient")
            {
                var preset = Find(value);
                return preset == null ? null : preset.Css;
            }

            if (type == "color" && IsHexColor(value)) return value;
            return null;
        }

        /// <summary>Human name of the current choice, for the customize panel.</summary>
        public static string NameFor(string type, string value)
        {
            if (type == "gradient")
            {
                var preset = Find(value);
                return preset == null ? "Gradient" : preset.Name;
            }
            if (type == "color") return IsHexColor(value) ? value.ToUpperInvariant() : "Colour";
            if (type == "image") return IsLocalName(value) ? "Picture" : "Web picture";
            return "Aura";
        }

        /// <summary>
        /// Whether the page should draw light or dark text: the user's explicit
        /// choice, or the sensible one for what is behind it.
        /// </summary>
        public static string TextMode()
        {
            string mode = Settings.Current.WallpaperText;
            if (mode == "light" || mode == "dark") return mode;

            string type = Settings.Current.WallpaperType;
            string value = Settings.Current.WallpaperValue;

            if (type == "image") return "light";

            if (type == "color" && IsHexColor(value))
            {
                return Luminance(value) > 0.62 ? "dark" : "light";
            }

            if (type == "gradient")
            {
                var preset = Find(value);
                if (preset != null) return preset.Dark ? "light" : "dark";
            }

            return Theme.IsDark ? "light" : "dark";
        }

        public static bool IsHexColor(string value)
        {
            if (string.IsNullOrEmpty(value)) return false;
            if (value.Length != 7 || value[0] != '#') return false;

            for (int i = 1; i < value.Length; i++)
            {
                char c = value[i];
                bool hex = (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
                if (!hex) return false;
            }
            return true;
        }

        /// <summary>Normalises "#abc" and "#AABBCC" into a six-digit lower-case form.</summary>
        public static string NormalizeColor(string value)
        {
            if (string.IsNullOrEmpty(value)) return null;

            string text = value.Trim();
            if (text.StartsWith("#")) text = text.Substring(1);

            if (text.Length == 3)
            {
                text = new string(new char[] { text[0], text[0], text[1], text[1], text[2], text[2] });
            }

            if (text.Length != 6) return null;
            return IsHexColor("#" + text) ? "#" + text.ToLowerInvariant() : null;
        }

        private static bool IsLocalName(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            if (name.IndexOf('/') >= 0 || name.IndexOf('\\') >= 0) return false;
            if (name.IndexOf("..") >= 0) return false;
            return true;
        }

        private static double Luminance(string hex)
        {
            int red = Convert.ToInt32(hex.Substring(1, 2), 16);
            int green = Convert.ToInt32(hex.Substring(3, 2), 16);
            int blue = Convert.ToInt32(hex.Substring(5, 2), 16);

            return (0.2126 * red + 0.7152 * green + 0.0722 * blue) / 255.0;
        }
    }
}
