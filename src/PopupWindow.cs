using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace SimpleBrowser
{
    /// <summary>
    /// A small secondary window used for scripted popups — most importantly the
    /// OAuth / "Sign in with Google" windows that communicate back to their
    /// opener. WebView2 only supports that handshake when the popup is a real
    /// WebView2 from the same environment handed back through
    /// <c>NewWindowRequested.NewWindow</c>, which is what the host does here.
    /// </summary>
    public class PopupWindow : Form
    {
        private readonly WebView2 _webView;

        public WebView2 View { get { return _webView; } }

        public PopupWindow()
        {
            AutoScaleMode = AutoScaleMode.None;
            FormBorderStyle = FormBorderStyle.Sizable;
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            MinimizeBox = false;
            MaximizeBox = false;
            Size = new Size(Dpi.Px(520), Dpi.Px(660));
            MinimumSize = new Size(Dpi.Px(320), Dpi.Px(280));
            BackColor = Theme.WindowBackground;
            Text = "Aura";

            _webView = new WebView2();
            _webView.Dock = DockStyle.Fill;
            _webView.DefaultBackgroundColor = Theme.ContentBackground;
            Controls.Add(_webView);

            Theme.Changed += ApplyTheme;
        }

        /// <summary>
        /// Called once the WebView2 underneath has finished initializing. The
        /// caller has already assigned this window to the opener's request.
        /// </summary>
        public void Attach()
        {
            var core = _webView.CoreWebView2;
            if (core == null) return;

            core.Settings.IsStatusBarEnabled = false;
            core.Settings.AreDefaultContextMenusEnabled = true;
            core.Settings.IsPasswordAutosaveEnabled = Settings.Current.SavePasswords;
            core.Settings.IsGeneralAutofillEnabled = Settings.Current.Autofill;

            _webView.ZoomFactor = Settings.Current.ZoomFactor;

            core.Profile.PreferredColorScheme = Theme.IsDark
                ? CoreWebView2PreferredColorScheme.Dark
                : CoreWebView2PreferredColorScheme.Light;

            // window.close() from the popup's own script.
            core.WindowCloseRequested += delegate
            {
                try { Close(); }
                catch { }
            };

            // Keep the title bar in sync with the page.
            core.DocumentTitleChanged += delegate
            {
                try
                {
                    string title = core.DocumentTitle;
                    Text = string.IsNullOrEmpty(title) ? "Aura" : title;
                }
                catch { }
            };
        }

        /// <summary>
        /// WebView2 applies the page's requested window geometry itself and can
        /// leave the window at the default cascade spot; put it over Aura instead.
        /// </summary>
        public void CenterOnOwner()
        {
            var owner = Owner;
            if (owner == null) return;

            Rectangle bounds = owner.Bounds;
            int x = bounds.Left + Math.Max(0, (bounds.Width - Width) / 2);
            int y = bounds.Top + Math.Max(0, (bounds.Height - Height) / 2);
            Location = new Point(x, y);
        }

        private void ApplyTheme()
        {
            BackColor = Theme.WindowBackground;
            if (_webView != null)
            {
                _webView.DefaultBackgroundColor = Theme.ContentBackground;
                if (_webView.CoreWebView2 != null)
                {
                    _webView.CoreWebView2.Profile.PreferredColorScheme = Theme.IsDark
                        ? CoreWebView2PreferredColorScheme.Dark
                        : CoreWebView2PreferredColorScheme.Light;
                }
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            Theme.Changed -= ApplyTheme;
            base.OnFormClosed(e);
        }
    }
}
