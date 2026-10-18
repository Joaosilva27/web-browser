using System;
using System.Drawing;

namespace SimpleBrowser
{
    /// <summary>
    /// Converts 96-DPI design pixels to the current display scale. The process
    /// opts into per-monitor DPI awareness, so WinForms units are physical
    /// pixels; the HTML chrome and WebView2 surfaces scale themselves from the
    /// same factor.
    /// </summary>
    public static class Dpi
    {
        public static float Scale = 1f;

        public static void Init()
        {
            try
            {
                using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
                {
                    Scale = g.DpiX / 96f;
                }
            }
            catch
            {
                Scale = 1f;
            }

            if (Scale <= 0f) Scale = 1f;
        }

        public static int Px(int value)
        {
            return (int)Math.Round(value * Scale);
        }

        public static float Px(float value)
        {
            return value * Scale;
        }
    }
}
