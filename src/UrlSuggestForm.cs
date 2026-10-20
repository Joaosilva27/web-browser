using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
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
            int textWidth = Width - pad * 2;

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

                    SuggestEntry entry = _entries[i];

                    if (entry.IsSearch)
                    {
                        TextRenderer.DrawText(g, entry.Title, boldFont,
                            new Rectangle(pad, top, textWidth, RowHeight),
                            Theme.TextPrimary,
                            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
                        continue;
                    }

                    TextRenderer.DrawText(g, entry.Title, titleFont,
                        new Rectangle(pad, top + Dpi.Px(5), textWidth, Dpi.Px(20)),
                        Theme.TextPrimary,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

                    TextRenderer.DrawText(g, entry.Detail, detailFont,
                        new Rectangle(pad, top + Dpi.Px(24), textWidth, Dpi.Px(17)),
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
