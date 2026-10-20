using System;
using System.Drawing;
using System.Windows.Forms;

namespace SimpleBrowser
{
    /// <summary>
    /// The page-settings popover that belongs to the AA button.
    ///
    /// It only ever describes the site on screen: the zoom that site is shown at
    /// and a way into reader view. Nothing here changes Toge as a whole, which is
    /// what separates it from the Toge menu behind the ⋯ button.
    /// </summary>
    public class PageSettingsForm : Form
    {
        /// <summary>Raised whenever the zoom changes, so the browser can apply it.</summary>
        public event EventHandler ZoomChanged;

        /// <summary>Raised when the user asks to turn reader view on or off.</summary>
        public event EventHandler ReaderRequested;

        private readonly string _url;
        private readonly string _site;

        private int _zoom;
        private Label _zoomValue;
        private Label _hint;
        private Label _reset;
        private TogeButton _smaller;
        private TogeButton _larger;

        /// <summary>The zoom the popover is showing, as a percentage.</summary>
        public int Zoom
        {
            get { return _zoom; }
        }

        public PageSettingsForm(string url, string site)
        {
            _url = url;
            _site = site;
            _zoom = SiteSettings.Current.ZoomFor(url);

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            AutoScaleMode = AutoScaleMode.None;
            Size = new Size(Dpi.Px(300), Dpi.Px(212));
            BackColor = Theme.CardBackground;
            ForeColor = Theme.TextPrimary;
            ShowInTaskbar = false;
            TopMost = true;
            DoubleBuffered = true;

            var title = new Label();
            title.Text = "Page Settings";
            title.Font = new Font("Segoe UI Semibold", 12.5F);
            title.ForeColor = Theme.TextPrimary;
            title.AutoSize = true;
            title.Location = new Point(Dpi.Px(22), Dpi.Px(15));

            var host = new Label();
            host.Text = _site;
            host.Font = new Font("Segoe UI", 8.75F);
            host.ForeColor = Theme.TextSecondary;
            host.AutoEllipsis = true;
            host.AutoSize = false;
            host.Size = new Size(Dpi.Px(222), Dpi.Px(18));
            host.Location = new Point(Dpi.Px(23), Dpi.Px(41));

            var close = new Label();
            close.Text = "\u2715";
            close.Font = new Font("Segoe UI", 9.5F);
            close.ForeColor = Theme.TextTertiary;
            close.TextAlign = ContentAlignment.MiddleCenter;
            close.Size = new Size(Dpi.Px(26), Dpi.Px(26));
            close.Location = new Point(Dpi.Px(258), Dpi.Px(13));
            close.Cursor = Cursors.Hand;
            close.Click += delegate { Close(); };

            var card = new Panel();
            card.Location = new Point(Dpi.Px(16), Dpi.Px(66));
            card.Size = new Size(Dpi.Px(268), Dpi.Px(104));
            card.BackColor = Theme.IsDark
                ? Color.FromArgb(52, 52, 56)
                : Color.FromArgb(245, 245, 247);

            var zoomLabel = new Label();
            zoomLabel.Text = "Zoom";
            zoomLabel.Font = new Font("Segoe UI", 8.75F);
            zoomLabel.ForeColor = Theme.TextSecondary;
            zoomLabel.TextAlign = ContentAlignment.MiddleLeft;
            zoomLabel.Size = new Size(Dpi.Px(80), Dpi.Px(26));
            zoomLabel.Location = new Point(Dpi.Px(14), Dpi.Px(13));

            _smaller = Stepper("\u2212", Dpi.Px(146));
            _smaller.Click += delegate { Step(-1); };

            _zoomValue = new Label();
            _zoomValue.Font = new Font("Segoe UI", 9.25F, FontStyle.Bold);
            _zoomValue.ForeColor = Theme.TextPrimary;
            _zoomValue.TextAlign = ContentAlignment.MiddleCenter;
            _zoomValue.Size = new Size(Dpi.Px(52), Dpi.Px(26));
            _zoomValue.Location = new Point(Dpi.Px(174), Dpi.Px(13));

            _larger = Stepper("+", Dpi.Px(228));
            _larger.Click += delegate { Step(1); };

            var reader = new TogeButton();
            reader.Text = "Reader View";
            reader.Font = new Font("Segoe UI", 9.25F);
            reader.Radius = Dpi.Px(8);
            reader.Size = new Size(Dpi.Px(240), Dpi.Px(32));
            reader.Location = new Point(Dpi.Px(14), Dpi.Px(62));
            reader.BackColor = Theme.IsDark
                ? Color.FromArgb(64, 64, 68)
                : Color.FromArgb(232, 232, 235);
            reader.ForeColor = Theme.TextPrimary;
            reader.Click += delegate
            {
                Close();
                if (ReaderRequested != null) ReaderRequested(this, EventArgs.Empty);
            };

            card.Controls.Add(zoomLabel);
            card.Controls.Add(_smaller);
            card.Controls.Add(_zoomValue);
            card.Controls.Add(_larger);
            card.Controls.Add(reader);
            card.Paint += OnCardPaint;

            _hint = new Label();
            _hint.Font = new Font("Segoe UI", 8F);
            _hint.ForeColor = Theme.TextTertiary;
            _hint.TextAlign = ContentAlignment.MiddleLeft;
            _hint.AutoEllipsis = true;
            _hint.AutoSize = false;
            _hint.Size = new Size(Dpi.Px(200), Dpi.Px(20));
            _hint.Location = new Point(Dpi.Px(22), Dpi.Px(178));

            _reset = new Label();
            _reset.Text = "Reset";
            _reset.Font = new Font("Segoe UI", 8.75F);
            _reset.ForeColor = Theme.AccentBlue;
            _reset.TextAlign = ContentAlignment.MiddleRight;
            _reset.Size = new Size(Dpi.Px(56), Dpi.Px(20));
            _reset.Location = new Point(Dpi.Px(222), Dpi.Px(178));
            _reset.Cursor = Cursors.Hand;
            _reset.Click += delegate { ResetZoom(); };

            Controls.Add(title);
            Controls.Add(host);
            Controls.Add(close);
            Controls.Add(card);
            Controls.Add(_hint);
            Controls.Add(_reset);

            RefreshZoom();
        }

        /// <summary>A small square button for the zoom stepper.</summary>
        private TogeButton Stepper(string text, int left)
        {
            var button = new TogeButton();
            button.Text = text;
            button.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            button.Radius = Dpi.Px(7);
            button.Size = new Size(Dpi.Px(26), Dpi.Px(26));
            button.Location = new Point(left, Dpi.Px(13));
            button.BackColor = Theme.IsDark
                ? Color.FromArgb(78, 78, 84)
                : Color.FromArgb(226, 226, 230);
            button.ForeColor = Theme.TextPrimary;
            button.TabStop = false;
            return button;
        }

        private void Step(int direction)
        {
            _zoom = SiteSettings.Step(_zoom, direction);
            if (ZoomChanged != null) ZoomChanged(this, EventArgs.Empty);
            RefreshZoom();
        }

        private void ResetZoom()
        {
            SiteSettings.Current.ClearZoom(_url);
            _zoom = SiteSettings.Current.ZoomFor(_url);
            if (ZoomChanged != null) ZoomChanged(this, EventArgs.Empty);
            RefreshZoom();
        }

        private void RefreshZoom()
        {
            _zoomValue.Text = SiteSettings.PercentText(_zoom);

            _smaller.Enabled = _zoom > SiteSettings.ZoomSteps[0];
            _larger.Enabled = _zoom < SiteSettings.ZoomSteps[SiteSettings.ZoomSteps.Length - 1];
            _smaller.ForeColor = _smaller.Enabled ? Theme.TextPrimary : Theme.TextTertiary;
            _larger.ForeColor = _larger.Enabled ? Theme.TextPrimary : Theme.TextTertiary;

            bool own = SiteSettings.Current.HasZoom(_url);
            _hint.Text = own
                ? "Zoom is remembered for this site."
                : "Using the page zoom from Settings.";
            _reset.Visible = own;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Region = Theme.RoundedRegion(ClientRectangle, Dpi.Px(14));
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

        /// <summary>Draws the hairline around the card and under the first row.</summary>
        private void OnCardPaint(object sender, PaintEventArgs e)
        {
            using (var pen = new Pen(Theme.CardBorder, 1))
            {
                e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);

                int y = Dpi.Px(52);
                e.Graphics.DrawLine(pen, Dpi.Px(1), y, Width - Dpi.Px(1), y);
            }
        }

        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
            Close();
        }
    }
}
