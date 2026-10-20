using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace SimpleBrowser
{
    /// <summary>
    /// The Toge mark. It is embedded in the executable, so the window, the
    /// taskbar and the start page all draw the same picture without shipping
    /// loose files next to the app.
    /// </summary>
    internal static class AppIcon
    {
        private const string ResourceName = "toge.ico";
        private static Icon _windowIcon;
        private static string _markDataUrl;

        /// <summary>The icon every window uses. Never null.</summary>
        public static Icon Value
        {
            get
            {
                if (_windowIcon == null) _windowIcon = Load(64);
                return _windowIcon;
            }
        }

        /// <summary>One size out of the embedded icon, or the stock icon.</summary>
        public static Icon Load(int size)
        {
            try
            {
                var assembly = typeof(AppIcon).Assembly;
                using (var stream = assembly.GetManifestResourceStream(ResourceName))
                {
                    if (stream != null)
                    {
                        // The Icon keeps reading from its stream, so it gets a
                        // private copy that stays alive for the process.
                        var buffer = new MemoryStream();
                        stream.CopyTo(buffer);
                        buffer.Position = 0;
                        return new Icon(buffer, size, size);
                    }
                }
            }
            catch
            {
                // A missing resource must not stop the browser opening.
            }

            return SystemIcons.Application;
        }

        /// <summary>The mark as a data URL, for the start page and its tab.</summary>
        public static string MarkDataUrl
        {
            get
            {
                if (_markDataUrl == null)
                {
                    try
                    {
                        using (var icon = Load(64))
                        using (var bitmap = icon.ToBitmap())
                        using (var stream = new MemoryStream())
                        {
                            bitmap.Save(stream, ImageFormat.Png);
                            _markDataUrl = "data:image/png;base64," + Convert.ToBase64String(stream.ToArray());
                        }
                    }
                    catch
                    {
                        _markDataUrl = string.Empty;
                    }
                }

                return _markDataUrl;
            }
        }

        /// <summary>Gives a window the mark, including in the taskbar.</summary>
        public static void Apply(Form form)
        {
            if (form != null) form.Icon = Value;
        }
    }
}
