using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SimpleBrowser
{
    /// <summary>One row in the address bar's suggestion list.</summary>
    public class SuggestEntry
    {
        public string Title;
        public string Detail;
        public string Url;
        public bool IsSearch;

        /// <summary>The words of a search row, as opposed to its Url — the query the row completes.</summary>
        public string Term;

        /// <summary>The site icon as a data URL; null draws the letter tile.</summary>
        public string IconDataUrl;
    }

    /// <summary>
    /// The address bar's suggestion list.
    ///
    /// It is a separate, never-activated window because the chrome WebView is
    /// only as tall as the toolbar: a list drawn in HTML would be clipped the
    /// moment it left that strip. Keys stay with the chrome — it reports them,
    /// the host moves the highlight and writes the address back into the field,
    /// so Enter and typing keep working without ever moving focus.
    /// </summary>
    public class UrlSuggestForm : Form
    {
        /// <summary>Raised when the user clicks a row.</summary>
        public event Action<SuggestEntry> Chosen;

        private readonly List<SuggestEntry> _entries = new List<SuggestEntry>();
        private readonly Dictionary<string, Image> _icons =
            new Dictionary<string, Image>(StringComparer.Ordinal);
        private int _highlight = -1;
        private bool _shown;

        public UrlSuggestForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            AutoScaleMode = AutoScaleMode.None;
            BackColor = Theme.CardBackground;
            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        }

        /// <summary>Never take focus from the address field behind it.</summary>
        protected override bool ShowWithoutActivation
        {
            get { return true; }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE
                cp.ExStyle |= 0x00000080; // WS_EX_TOOLWINDOW
                cp.ClassStyle |= 0x20000; // CS_DROPSHADOW
                return cp;
            }
        }

        private static int RowHeight
        {
            get { return Dpi.Px(48); }
        }

        public void SetEntries(List<SuggestEntry> entries)
        {
            _entries.Clear();
            if (entries != null) _entries.AddRange(entries);
            _highlight = -1;

            int rows = Math.Max(1, _entries.Count);
            Height = Dpi.Px(8) + rows * RowHeight;
            Invalidate();
        }

        /// <summary>-1 means "nothing chosen"; the field keeps what was typed.</summary>
        public void Highlight(int index)
        {
            if (index < -1 || index >= _entries.Count) index = -1;
            if (index == _highlight) return;

            _highlight = index;
            Invalidate();
        }

        public void ShowAt(Point location, int width)
        {
            if (width < Dpi.Px(260)) width = Dpi.Px(260);
            if (width > Dpi.Px(820)) width = Dpi.Px(820);

            Width = width;
            Location = location;

            // Showing a Form normally takes the focus, which would end the
            // address field's editing session. Showing the window directly with
            // SWP_NOACTIVATE leaves the chrome exactly as it was.
            IntPtr handle = Handle;
            SetWindowPos(handle, HwndTopmost, location.X, location.Y, width, Height,
                SwpNoActivate | SwpShowWindow);

            _shown = true;
            Invalidate();
        }

        /// <summary>Puts the list away without disturbing anything else.</summary>
        public void HideQuietly()
        {
            if (!_shown) return;
            _shown = false;

            if (IsHandleCreated)
            {
                SetWindowPos(Handle, IntPtr.Zero, 0, 0, 0, 0,
                    SwpNoActivate | SwpNoMove | SwpNoSize | SwpHideWindow);
            }
        }

        /// <summary>True while the list is on screen.</summary>
        public bool IsShown { get { return _shown; } }

        protected override void Dispose(bool disposing)
        {
            foreach (var pair in _icons)
            {
                if (pair.Value != null) pair.Value.Dispose();
            }
            _icons.Clear();

            base.Dispose(disposing);
        }

        private static readonly IntPtr HwndTopmost = new IntPtr(-1);
        private const int SwpNoSize = 0x0001;
        private const int SwpNoMove = 0x0002;
        private const int SwpNoActivate = 0x0010;
        private const int SwpShowWindow = 0x0040;
        private const int SwpHideWindow = 0x0080;

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr window, IntPtr insertAfter,
            int x, int y, int width, int height, int flags);

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (var back = new SolidBrush(Theme.CardBackground))
            {
                g.FillRectangle(back, ClientRectangle);
            }

            int pad = Dpi.Px(14);
            int iconSize = Dpi.Px(24);
            int textLeft = pad + iconSize + Dpi.Px(9);
            int textWidth = Width - textLeft - pad;

            using (var titleFont = new Font("Segoe UI", 9.25F))
            using (var boldFont = new Font("Segoe UI", 9.25F, FontStyle.Bold))
            using (var detailFont = new Font("Segoe UI", 8.25F))
            {
                for (int i = 0; i < _entries.Count; i++)
                {
                    int top = Dpi.Px(4) + i * RowHeight;

                    if (i == _highlight)
                    {
                        var bounds = new Rectangle(Dpi.Px(4), top + Dpi.Px(1),
                            Width - Dpi.Px(8), RowHeight - Dpi.Px(2));

                        using (var path = RoundedPath(bounds, Dpi.Px(7)))
                        using (var fill = new SolidBrush(Theme.IsDark
                            ? Color.FromArgb(64, 64, 68)
                            : Color.FromArgb(238, 238, 241)))
                        {
                            g.FillPath(fill, path);
                        }
                    }

                    int iconY = top + (RowHeight - iconSize) / 2;
                    SuggestEntry entry = _entries[i];

                    if (entry.IsSearch)
                    {
                        DrawSearchGlyph(g, pad, iconY, iconSize);
                        TextRenderer.DrawText(g, entry.Title, boldFont,
                            new Rectangle(textLeft, top, textWidth, RowHeight),
                            Theme.TextPrimary,
                            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                        continue;
                    }

                    DrawRowIcon(g, entry, pad, iconY, iconSize, boldFont);

                    TextRenderer.DrawText(g, entry.Title, titleFont,
                        new Rectangle(textLeft, top + Dpi.Px(5), textWidth, Dpi.Px(20)),
                        Theme.TextPrimary,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

                    TextRenderer.DrawText(g, entry.Detail, detailFont,
                        new Rectangle(textLeft, top + Dpi.Px(24), textWidth, Dpi.Px(17)),
                        Theme.TextTertiary,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                }
            }

            using (var pen = new Pen(Theme.CardBorder, 1))
            {
                g.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
            }
        }

        private int IndexAt(int y)
        {
            int index = (y - Dpi.Px(4)) / RowHeight;
            if (index < 0 || index >= _entries.Count) return -1;
            return index;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            int index = IndexAt(e.Y);
            if (index != _highlight)
            {
                _highlight = index;
                Invalidate();
            }

            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            if (_highlight != -1)
            {
                _highlight = -1;
                Invalidate();
            }

            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                int index = IndexAt(e.Y);
                if (index >= 0 && index < _entries.Count)
                {
                    SuggestEntry entry = _entries[index];
                    HideQuietly();

                    if (Chosen != null) Chosen(entry);
                }
            }

            base.OnMouseDown(e);
        }

        /// <summary>
        /// The round face each row wears: the site's own icon when one was
        /// seen, otherwise the first letter on a coloured tile, matching what
        /// the tab strip shows when a page has no picture.
        /// </summary>
        private void DrawRowIcon(Graphics g, SuggestEntry entry, int x, int y, int size, Font letterFont)
        {
            Image icon = LookupIcon(entry);
            if (icon != null)
            {
                InterpolationMode saved = g.InterpolationMode;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(icon, x + Dpi.Px(2), y + Dpi.Px(2), size - Dpi.Px(4), size - Dpi.Px(4));
                g.InterpolationMode = saved;
                return;
            }

            string host = HostOf(entry.Url);
            if (string.IsNullOrEmpty(host) && !string.IsNullOrEmpty(entry.Title))
            {
                host = entry.Title;
            }
            if (string.IsNullOrEmpty(host)) host = "?";
            string letter = host.Substring(0, 1).ToUpperInvariant();

            using (var path = RoundedPath(new Rectangle(x, y, size, size), size / 2))
            using (var brush = new SolidBrush(ChipColor(host)))
            {
                g.FillPath(brush, path);
            }

            TextRenderer.DrawText(g, letter, letterFont, new Rectangle(x, y, size, size),
                Color.White,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        /// <summary>Decodes an entry's data URL once, then serves it from cache.</summary>
        private Image LookupIcon(SuggestEntry entry)
        {
            if (entry == null || string.IsNullOrEmpty(entry.IconDataUrl)) return null;

            Image cached;
            if (_icons.TryGetValue(entry.IconDataUrl, out cached)) return cached;

            Image decoded = null;
            int comma = entry.IconDataUrl.IndexOf(',');
            if (comma >= 0)
            {
                try
                {
                    byte[] bytes = Convert.FromBase64String(entry.IconDataUrl.Substring(comma + 1));
                    // The stream has to stay open while GDI+ reads, so the
                    // decoded image is copied straight into a bitmap instead.
                    using (var memory = new MemoryStream(bytes))
                    using (var source = Image.FromStream(memory))
                    {
                        decoded = new Bitmap(source);
                    }
                }
                catch
                {
                    decoded = null;
                }
            }

            _icons[entry.IconDataUrl] = decoded;
            return decoded;
        }

        /// <summary>A magnifier for the search row, drawn in the same slot the
        /// site icons use, so every entry starts with a picture.</summary>
        private void DrawSearchGlyph(Graphics g, int x, int y, int size)
        {
            int box = Dpi.Px(16);
            int cx = x + (size - box) / 2;
            int cy = y + (size - box) / 2;
            int glass = box * 2 / 3;

            using (var pen = new Pen(Theme.TextTertiary, Math.Max(1f, Dpi.Px(1.4f))))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;

                g.DrawEllipse(pen, cx, cy, glass, glass);
                g.DrawLine(pen,
                    cx + glass - box / 10, cy + glass - box / 10,
                    cx + box, cy + box);
            }
        }

        private static string HostOf(string url)
        {
            if (string.IsNullOrEmpty(url)) return null;

            Uri parsed;
            if (!Uri.TryCreate(url, UriKind.Absolute, out parsed)) return null;

            string host = parsed.Host;
            if (host.StartsWith("www.", StringComparison.OrdinalIgnoreCase)) host = host.Substring(4);
            return host.Length > 0 ? host : null;
        }

        /// <summary>The same colour roll the start page tiles use.</summary>
        private static readonly string[] Palette =
        {
            "#007aff", "#34c759", "#ff9500", "#ff3b30", "#af52de",
            "#5856d6", "#5ac8fa", "#ff2d55", "#30b0c7", "#8e8e93"
        };

        private static Color ChipColor(string text)
        {
            if (string.IsNullOrEmpty(text)) text = "a";

            int hash = 0;
            for (int i = 0; i < text.Length; i++)
            {
                hash = (hash * 31 + text[i]) & 0xffff;
            }

            return ColorTranslator.FromHtml(Palette[hash % Palette.Length]);
        }

        private static GraphicsPath RoundedPath(Rectangle bounds, int radius)
        {
            var path = new GraphicsPath();

            if (radius <= 0)
            {
                path.AddRectangle(bounds);
                return path;
            }

            int diameter = radius * 2;
            if (diameter > bounds.Width) diameter = bounds.Width;
            if (diameter > bounds.Height) diameter = bounds.Height;

            var arc = new Rectangle(bounds.Location, new Size(diameter, diameter));

            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();

            return path;
        }
    }
}
