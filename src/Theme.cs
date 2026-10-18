using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using Microsoft.Win32;

namespace SimpleBrowser
{
    /// <summary>
    /// One place for every colour in the app. The visual language is Safari's:
    /// soft neutral chrome, hairline separators, a single blue accent. Brave's
    /// orange is kept as the Shields accent.
    /// </summary>
    public static class Theme
    {
        private static bool? _isDark;
        private static bool? _override;

        private const string PreferenceKey = @"HKEY_CURRENT_USER\Software\Aura";

        /// <summary>Raised after the light/dark preference changes.</summary>
        public static event Action Changed;

        /// <summary>"light", "dark" or "system".</summary>
        public static string Preference
        {
            get
            {
                if (!_override.HasValue) return "system";
                return _override.Value ? "dark" : "light";
            }
        }

        /// <summary>
        /// Reads the stored preference without changing the live one. Used once
        /// to migrate the old registry setting into the settings file.
        /// </summary>
        public static string ReadStoredPreference()
        {
            try
            {
                string mode = Registry.GetValue(PreferenceKey, "Theme", "system") as string;
                if (mode == "dark" || mode == "light" || mode == "system") return mode;
            }
            catch
            {
                // Registry unavailable.
            }
            return null;
        }

        public static void LoadPreference()
        {
            try
            {
                object value = Registry.GetValue(PreferenceKey, "Theme", "system");
                string mode = value as string;
                if (mode == "dark") _override = true;
                else if (mode == "light") _override = false;
                else _override = null;
            }
            catch
            {
                _override = null;
            }

            _isDark = null;
        }

        public static void SetPreference(string mode)
        {
            if (mode == "dark")
            {
                _override = true;
            }
            else if (mode == "light")
            {
                _override = false;
            }
            else
            {
                _override = null;
                _isDark = null;  // re-read the Windows preference
            }

            try
            {
                Registry.SetValue(PreferenceKey, "Theme", mode);
            }
            catch
            {
                // Persisting the choice is best-effort.
            }

            if (Changed != null) Changed();
        }

        public static bool IsDark
        {
            get
            {
                if (_override.HasValue) return _override.Value;
                if (_isDark.HasValue) return _isDark.Value;

                bool dark = false;
                try
                {
                    object value = Registry.GetValue(
                        @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                        "AppsUseLightTheme", 1);
                    if (value is int) dark = ((int)value) == 0;
                }
                catch
                {
                    // Registry unavailable: fall back to light.
                }

                _isDark = dark;
                return dark;
            }
        }

        // ── Window chrome ──────────────────────────────────────────────────
        public static Color ChromeTop
        {
            get { return IsDark ? Color.FromArgb(51, 51, 54) : Color.FromArgb(247, 247, 248); }
        }

        public static Color WindowBackground
        {
            get { return IsDark ? Color.FromArgb(41, 41, 44) : Color.FromArgb(236, 236, 238); }
        }

        // ── Content ────────────────────────────────────────────────────────
        public static Color ContentBackground
        {
            get { return IsDark ? Color.FromArgb(24, 24, 26) : Color.FromArgb(255, 255, 255); }
        }

        public static Color CardBackground
        {
            get { return IsDark ? Color.FromArgb(44, 44, 47) : Color.FromArgb(255, 255, 255); }
        }

        public static Color CardBorder
        {
            get { return IsDark ? Color.FromArgb(64, 64, 68) : Color.FromArgb(224, 224, 228); }
        }

        public static Color Border
        {
            get { return CardBorder; }
        }

        // ── Text ───────────────────────────────────────────────────────────
        public static Color TextPrimary
        {
            get { return IsDark ? Color.FromArgb(245, 245, 247) : Color.FromArgb(29, 29, 31); }
        }

        public static Color TextSecondary
        {
            get { return IsDark ? Color.FromArgb(161, 161, 166) : Color.FromArgb(110, 110, 115); }
        }

        public static Color TextTertiary
        {
            get { return IsDark ? Color.FromArgb(120, 120, 126) : Color.FromArgb(154, 154, 160); }
        }

        // ── Accents ────────────────────────────────────────────────────────
        public static Color AccentBlue
        {
            get { return IsDark ? Color.FromArgb(10, 132, 255) : Color.FromArgb(0, 122, 255); }
        }

        public static Color AccentOrange
        {
            get { return IsDark ? Color.FromArgb(255, 107, 61) : Color.FromArgb(242, 90, 41); }
        }

        public static Color Success
        {
            get { return IsDark ? Color.FromArgb(48, 209, 88) : Color.FromArgb(52, 199, 89); }
        }

        /// <summary>Soft tinted surface used for cards and selected rows.</summary>
        public static Color Tint(Color color, int alpha)
        {
            return Color.FromArgb(alpha, color.R, color.G, color.B);
        }

        public static Region RoundedRegion(Rectangle bounds, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return new Region(path);
        }
    }
}
