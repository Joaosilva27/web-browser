using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace SimpleBrowser
{
    /// <summary>
    /// The card Toge shows when a site sends a notification it was allowed to
    /// send. Chromium has no notification UI of its own inside WebView2, so
    /// without this the permission prompt would be a promise nothing keeps.
    ///
    /// The toast never takes the keyboard: it appears in the corner of the
    /// working area, waits seven seconds, and can be clicked to go to the tab
    /// that sent it.
    /// </summary>
    public class NotificationToast : Form
    {
        private const int ToastWidth = 356;
        private const int ToastHeight = 104;
        private const int Gap = 8;

        private static readonly List<NotificationToast> Live = new List<NotificationToast>();

        private readonly Action _onClick;
        private readonly Action _onClosed;
        private readonly Timer _timer;
        private bool _dismissed;

        public NotificationToast(string origin, string title, string body, Action onClick, Action onClosed)
        {
            _onClick = onClick;
            _onClosed = onClosed;

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = true;
            AutoScaleMode = AutoScaleMode.None;
            BackColor = Theme.CardBackground;
            ForeColor = Theme.TextPrimary;
            DoubleBuffered = true;
            Size = new Size(Dpi.Px(ToastWidth), Dpi.Px(ToastHeight));

            var icon = new Panel();
            icon.Size = new Size(Dpi.Px(28), Dpi.Px(28));
            icon.Location = new Point(Dpi.Px(16), Dpi.Px(16));
            icon.BackColor = Theme.Tint(Theme.AccentBlue, Theme.IsDark ? 70 : 26);

            var glyph = new Label();
            glyph.Text = "\u25CF";
            glyph.Font = new Font("Segoe UI", 9F);
            glyph.ForeColor = Theme.AccentBlue;
            glyph.TextAlign = ContentAlignment.MiddleCenter;
            glyph.Dock = DockStyle.Fill;

            var site = new Label();
            site.Text = string.IsNullOrEmpty(origin) ? "Notification" : SiteSettings.HostOf(origin);
            site.Font = new Font("Segoe UI", 8.25F);
            site.ForeColor = Theme.TextTertiary;
            site.AutoEllipsis = true;
            site.AutoSize = false;
            site.Size = new Size(Dpi.Px(250), Dpi.Px(16));
            site.Location = new Point(Dpi.Px(54), Dpi.Px(12));

            var heading = new Label();
            heading.Text = string.IsNullOrEmpty(title) ? "Notification" : title;
            heading.Font = new Font("Segoe UI Semibold", 9.75F);
            heading.ForeColor = Theme.TextPrimary;
            heading.AutoEllipsis = true;
            heading.AutoSize = false;
            heading.Size = new Size(Dpi.Px(274), Dpi.Px(20));
            heading.Location = new Point(Dpi.Px(54), Dpi.Px(30));

            var text = new Label();
            text.Text = body ?? "";
            text.Font = new Font("Segoe UI", 8.75F);
            text.ForeColor = Theme.TextSecondary;
            text.AutoEllipsis = true;
            text.AutoSize = false;
            text.Size = new Size(Dpi.Px(310), Dpi.Px(34));
            text.Location = new Point(Dpi.Px(24), Dpi.Px(56));

            var close = new Label();
            close.Text = "\u2715";
            close.Font = new Font("Segoe UI", 8F);
            close.ForeColor = Theme.TextTertiary;
            close.TextAlign = ContentAlignment.MiddleCenter;
            close.Size = new Size(Dpi.Px(22), Dpi.Px(22));
            close.Location = new Point(Dpi.Px(ToastWidth - 32), Dpi.Px(10));
            close.Cursor = Cursors.Hand;
            close.Click += delegate { Dismiss(); };

            icon.Controls.Add(glyph);

            Controls.Add(icon);
            Controls.Add(site);
            Controls.Add(heading);
            Controls.Add(text);
            Controls.Add(close);

            // The whole card is the button, except the little close cross.
            Click += delegate { Open(); };
            icon.Click += delegate { Open(); };
            site.Click += delegate { Open(); };
            heading.Click += delegate { Open(); };
            text.Click += delegate { Open(); };

            _timer = new Timer();
            _timer.Interval = 7000;
            _timer.Tick += delegate { Dismiss(); };
        }

        /// <summary>True while this toast is on screen.</summary>
        public bool IsLive { get { return !_dismissed; } }

        /// <summary>
        /// Puts the toast in the corner and starts its clock. Called after the
        /// constructor so the caller can wire up its events first.
        /// </summary>
        public void Pop()
        {
            Live.Add(this);

            Rectangle work = Screen.FromPoint(Cursor.Position).WorkingArea;
            int bottom = work.Bottom - Dpi.Px(12);
            for (int i = 0; i < Live.Count - 1; i++)
            {
                bottom -= Dpi.Px(ToastHeight + Gap);
            }
            bottom -= Dpi.Px(ToastHeight);

            Location = new Point(work.Right - Dpi.Px(ToastWidth + 12), bottom);
            Show();
            _timer.Start();
        }

        private void Open()
        {
            if (_dismissed) return;

            if (_onClick != null) _onClick();
            Dismiss();
        }

        private void Dismiss()
        {
            if (_dismissed) return;

            _dismissed = true;
            _timer.Stop();
            Close();
        }

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
                cp.ClassStyle |= 0x20000; // CS_DROPSHADOW
                return cp;
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Region = Theme.RoundedRegion(ClientRectangle, Dpi.Px(12));
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            Live.Remove(this);
            Reposition();

            base.OnFormClosed(e);

            if (_timer != null)
            {
                _timer.Stop();
                _timer.Dispose();
            }

            if (_onClosed != null) _onClosed();
        }

        /// <summary>Closes every toast, as a window that is going away should.</summary>
        public static void CloseAll()
        {
            for (int i = Live.Count - 1; i >= 0; i--)
            {
                Live[i].Dismiss();
            }
        }

        private static void Reposition()
        {
            Rectangle work = Screen.PrimaryScreen.WorkingArea;
            for (int i = 0; i < Live.Count; i++)
            {
                int bottom = work.Bottom - Dpi.Px(12) - ((i + 1) * Dpi.Px(ToastHeight + Gap));
                Live[i].Location = new Point(work.Right - Dpi.Px(ToastWidth + 12), bottom);
            }
        }
    }
}
