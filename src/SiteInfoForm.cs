using System;
using System.Drawing;
using System.Windows.Forms;

namespace SimpleBrowser
{
    /// <summary>
    /// The card behind the lock in the address bar. It says who the page says
    /// it is, whether the connection is private, and what can be cleared for
    /// this one site without touching anything else.
    /// </summary>
    public class SiteInfoForm : Form
    {
        /// <summary>Raised when the user asks to drop this site's cookies.</summary>
        public event EventHandler ClearCookiesRequested;

        /// <summary>Raised when the user wants the full site-permission list.</summary>
        public event EventHandler SettingsRequested;

        private readonly Label _cookieCount;
        private readonly Label _clearHint;

        public SiteInfoForm(string host, bool secure)
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            AutoScaleMode = AutoScaleMode.None;
            Size = new Size(Dpi.Px(372), Dpi.Px(218));
            BackColor = Theme.CardBackground;
            ForeColor = Theme.TextPrimary;
            ShowInTaskbar = false;
            DoubleBuffered = true;

            var title = new Label();
            title.Text = string.IsNullOrEmpty(host) ? "This page" : host;
            title.Font = new Font("Segoe UI Semibold", 12F);
            title.ForeColor = Theme.TextPrimary;
            title.AutoEllipsis = true;
            title.AutoSize = false;
            title.Size = new Size(Dpi.Px(330), Dpi.Px(24));
            title.Location = new Point(Dpi.Px(18), Dpi.Px(14));

            var status = new Label();
            status.Text = secure
                ? "\u2713  Connection is secure"
                : "\u26A0  Connection is not secure";
            status.Font = new Font("Segoe UI", 9F);
            status.ForeColor = secure ? Color.FromArgb(28, 150, 82) : Color.FromArgb(196, 127, 0);
            status.AutoSize = true;
            status.Location = new Point(Dpi.Px(18), Dpi.Px(44));

            var note = new Label();
            note.Text = secure
                ? "Information sent to this site is private."
                : "Anyone on the network can see or change what you send here.";
            note.Font = new Font("Segoe UI", 8.5F);
            note.ForeColor = Theme.TextTertiary;
            note.AutoEllipsis = true;
            note.AutoSize = false;
            note.Size = new Size(Dpi.Px(334), Dpi.Px(18));
            note.Location = new Point(Dpi.Px(18), Dpi.Px(66));

            var divider = new Panel();
            divider.BackColor = Theme.CardBorder;
            divider.Location = new Point(Dpi.Px(1), Dpi.Px(96));
            divider.Size = new Size(Dpi.Px(370), 1);

            _cookieCount = new Label();
            _cookieCount.Text = "Cookies for this site: \u2026";
            _cookieCount.Font = new Font("Segoe UI", 9F);
            _cookieCount.ForeColor = Theme.TextSecondary;
            _cookieCount.AutoSize = true;
            _cookieCount.Location = new Point(Dpi.Px(18), Dpi.Px(108));

            var clear = new AuraButton();
            clear.Text = "Clear cookies";
            clear.Font = new Font("Segoe UI", 9F);
            clear.Radius = Dpi.Px(8);
            clear.Size = new Size(Dpi.Px(128), Dpi.Px(32));
            clear.Location = new Point(Dpi.Px(18), Dpi.Px(136));
            clear.BackColor = Theme.IsDark ? Color.FromArgb(64, 64, 68) : Color.FromArgb(234, 234, 238);
            clear.ForeColor = Theme.TextPrimary;
            clear.Click += delegate
            {
                if (ClearCookiesRequested != null) ClearCookiesRequested(this, EventArgs.Empty);
            };

            var settings = new AuraButton();
            settings.Text = "Site settings";
            settings.Font = new Font("Segoe UI", 9F);
            settings.Radius = Dpi.Px(8);
            settings.Size = new Size(Dpi.Px(120), Dpi.Px(32));
            settings.Location = new Point(Dpi.Px(156), Dpi.Px(136));
            settings.BackColor = Theme.IsDark ? Color.FromArgb(64, 64, 68) : Color.FromArgb(234, 234, 238);
            settings.ForeColor = Theme.TextPrimary;
            settings.Click += delegate
            {
                if (SettingsRequested != null) SettingsRequested(this, EventArgs.Empty);
                Close();
            };

            _clearHint = new Label();
            _clearHint.Text = "Only this site's cookies are removed.";
            _clearHint.Font = new Font("Segoe UI", 8F);
            _clearHint.ForeColor = Theme.TextTertiary;
            _clearHint.AutoEllipsis = true;
            _clearHint.AutoSize = false;
            _clearHint.Size = new Size(Dpi.Px(334), Dpi.Px(16));
            _clearHint.Location = new Point(Dpi.Px(18), Dpi.Px(182));

            Controls.Add(title);
            Controls.Add(status);
            Controls.Add(note);
            Controls.Add(divider);
            Controls.Add(_cookieCount);
            Controls.Add(clear);
            Controls.Add(settings);
            Controls.Add(_clearHint);
        }

        /// <summary>Shows how many cookies the site currently keeps here.</summary>
        public void SetCookieCount(int count)
        {
            _cookieCount.Text = count == 1
                ? "1 cookie for this site"
                : count + " cookies for this site";
            _clearHint.Text = count == 0
                ? "Nothing is stored for this site."
                : "Only this site's cookies are removed.";
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Region = Theme.RoundedRegion(ClientRectangle, Dpi.Px(14));
        }

        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
            Close();
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= 0x20000; // CS_DROPSHADOW
                return cp;
            }
        }

        /// <summary>Draws the hairline around the card.</summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            using (var pen = new Pen(Theme.CardBorder, 1))
            {
                e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
            }
        }
    }
}
