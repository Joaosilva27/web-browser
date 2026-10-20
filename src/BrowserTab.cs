using System;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace SimpleBrowser
{
    public class BrowserTab : TabPage
    {
        private WebView2 _webView;
        private string _pendingUrl;
        private string _topLevelUri;
        private bool _isInitialized;
        private bool _isLoading;
        private bool _isHome;
        private bool _openHomeCustomizerPending;
        private bool _isSettings;
        private bool _privateMode;

        public event Action<BrowserTab, string> UrlChanged;
        public event Action<BrowserTab, string> TitleChanged;
        public event Action<BrowserTab> StateChanged;
        public event Action<BrowserTab, string> StatusTextChanged;
        public event Action<BrowserTab> IconChanged;
        public event EventHandler<CoreWebView2NewWindowRequestedEventArgs> NewWindowRequested;
        public event Action<BrowserTab> WindowCloseRequested;

        /// <summary>Raised by the about:settings page with a JSON message.</summary>
        public event Action<BrowserTab, string> SettingsMessage;

        /// <summary>Raised when the page enters or leaves its own fullscreen (video, slides).</summary>
        public event Action<BrowserTab, bool> FullScreenChanged;

        /// <summary>Raised when the page starts a download, so the host can run it.</summary>
        public event Action<BrowserTab, CoreWebView2DownloadStartingEventArgs> DownloadStarting;

        /// <summary>Raised when a page asks for a permission such as the camera.</summary>
        public event Action<BrowserTab, CoreWebView2PermissionRequestedEventArgs> PermissionRequested;

        /// <summary>Raised when the page asks for a context menu.</summary>
        public event Action<BrowserTab, CoreWebView2ContextMenuRequestedEventArgs> ContextMenuRequested;

        /// <summary>
        /// A browser key was pressed in the page. The host answers it, so the
        /// engine's own handling is switched off in ApplyAppSettings.
        /// </summary>
        public event Action<BrowserTab, int, CoreWebView2AcceleratorKeyPressedEventArgs> AcceleratorPressed;

        public WebView2 WebView { get { return _webView; } }
        public string CurrentUrl { get; private set; }
        public string DisplayTitle { get; private set; }
        public string FaviconDataUrl { get; private set; }
        public bool IsLoading { get { return _isLoading; } }
        /// <summary>True when this tab lives in a private window.</summary>
        public bool IsPrivate { get { return _privateMode; } }
        public bool CanGoBack { get { return _webView != null && _isInitialized && _webView.CanGoBack; } }
        public bool CanGoForward { get { return _webView != null && _isInitialized && _webView.CanGoForward; } }

        public BrowserTab(string initialUrl = null, bool privateMode = false)
        {
            Text = "New Tab";
            DisplayTitle = "New Tab";
            BackColor = Theme.ContentBackground;
            _privateMode = privateMode;

            _webView = new WebView2();
            _webView.Dock = DockStyle.Fill;
            _webView.DefaultBackgroundColor = Theme.ContentBackground;

            _pendingUrl = string.IsNullOrEmpty(initialUrl) ? "about:home" : initialUrl;

            Controls.Add(_webView);

            InitEngine();
        }

        private async void InitEngine()
        {
            try
            {
                await BrowserEnvironment.InitializeAsync(_webView, _privateMode);

                _isInitialized = true;

                // Lets the start page show a wallpaper the user picked off disk.
                HomeWallpaper.ApplyMapping(_webView.CoreWebView2);

                // Pages should follow Aura's light/dark choice, not the OS.
                _webView.CoreWebView2.Profile.PreferredColorScheme = Theme.IsDark
                    ? CoreWebView2PreferredColorScheme.Dark
                    : CoreWebView2PreferredColorScheme.Light;

                // Let WebView2 offer to save logins and autofill data. Both live
                // in the persistent profile, so they survive restarts, and both
                // follow whatever the settings page says.
                ApplyAppSettings();

                // Hook up AdBlocker & Brave Shields
                AdBlocker.Initialize();
                if (Settings.Current.Shields)
                {
                    await _webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(AdBlocker.GetAdBlockInjectionScript());
                }
                _webView.CoreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
                _webView.CoreWebView2.WebResourceRequested += OnWebResourceRequested;

                _webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;

                _webView.CoreWebView2.NavigationStarting += OnNavigationStarting;
                _webView.CoreWebView2.NavigationCompleted += OnNavigationCompleted;
                _webView.CoreWebView2.SourceChanged += OnSourceChanged;
                _webView.CoreWebView2.DocumentTitleChanged += OnDocumentTitleChanged;
                _webView.CoreWebView2.FaviconChanged += OnFaviconChanged;
                _webView.CoreWebView2.NewWindowRequested += OnNewWindowRequested;
                _webView.CoreWebView2.WindowCloseRequested += OnWindowCloseRequested;
                _webView.CoreWebView2.ContainsFullScreenElementChanged += OnContainsFullScreenElementChanged;
                _webView.CoreWebView2.DownloadStarting += OnDownloadStarting;
                _webView.CoreWebView2.PermissionRequested += OnPermissionRequested;
                _webView.CoreWebView2.ContextMenuRequested += OnContextMenuRequested;

                var controller = BrowserEnvironment.ControllerOf(_webView);
                if (controller != null) controller.AcceleratorKeyPressed += OnAcceleratorKeyPressed;

                if (!string.IsNullOrEmpty(_pendingUrl))
                {
                    Navigate(_pendingUrl);
                }
                else
                {
                    LoadHomePage();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to initialize Chromium engine: " + ex.Message, "Browser Engine Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void LoadHomePage()
        {
            _isHome = true;
            _isSettings = false;
            CurrentUrl = "about:home";
            _topLevelUri = null;
            Text = "New Tab";
            FaviconDataUrl = null;

            if (_isInitialized && _webView.CoreWebView2 != null)
            {
                _webView.NavigateToString(HomePage.GetHtml(Theme.IsDark));
            }
            else
            {
                _pendingUrl = "about:home";
            }

            if (IconChanged != null) IconChanged(this);
            if (TitleChanged != null) TitleChanged(this, "New Tab");
            if (UrlChanged != null) UrlChanged(this, "");
            if (StateChanged != null) StateChanged(this);
        }

        /// <summary>
        /// Redraws the start page from the current settings and favourites, without
        /// reloading it. Called after anything on the customize sheet changes, so
        /// the sheet stays open while the page updates underneath it.
        /// </summary>
        public void RefreshHome()
        {
            if (!_isHome || !_isInitialized || _webView.CoreWebView2 == null) return;

            try
            {
                _webView.CoreWebView2.ExecuteScriptAsync(
                    "window.AuraHome && window.AuraHome.apply(" + HomePage.StateJson() + ");");
            }
            catch
            {
                // A tab that is going away can refuse; nothing to redraw.
            }
        }

        /// <summary>
        /// Shows the start page and then opens its customize sheet. The sheet is
        /// asked for once the page has loaded, so the request is never lost to a
        /// page that has not been built yet.
        /// </summary>
        public void LoadHomePageWithCustomizer()
        {
            LoadHomePage();
            _openHomeCustomizerPending = true;
        }

        /// <summary>Opens the start page's customize sheet, from the Aura menu.</summary>
        public void OpenHomeCustomizer()
        {
            if (!_isHome || !_isInitialized || _webView.CoreWebView2 == null) return;

            try
            {
                _webView.CoreWebView2.ExecuteScriptAsync(
                    "window.AuraHome && window.AuraHome.openCustomize();");
            }
            catch
            {
                // Nothing to open.
            }
        }

        /// <summary>Shows a short message on the start page.</summary>
        public void NotifyHome(string message)
        {
            if (!_isHome || !_isInitialized || _webView.CoreWebView2 == null) return;

            // Serialising it turns the text into a safe JavaScript literal.
            string literal = Json.Write(message ?? string.Empty);
            if (literal == null) return;

            try
            {
                _webView.CoreWebView2.ExecuteScriptAsync(
                    "window.AuraHome && window.AuraHome.toast(" + literal + ");");
            }
            catch
            {
                // A tab on its way out cannot show anything.
            }
        }

        /// <summary>Shows Aura's own settings page in this tab.</summary>
        public void LoadSettingsPage()
        {
            _isHome = false;
            _isSettings = true;
            CurrentUrl = SettingsPage.Address;
            _topLevelUri = null;
            Text = "Settings";
            DisplayTitle = "Settings";
            FaviconDataUrl = null;

            if (_isInitialized && _webView.CoreWebView2 != null)
            {
                _webView.NavigateToString(SettingsPage.GetHtml());
            }
            else
            {
                _pendingUrl = SettingsPage.Address;
            }

            if (IconChanged != null) IconChanged(this);
            if (TitleChanged != null) TitleChanged(this, "Settings");
            if (UrlChanged != null) UrlChanged(this, SettingsPage.Address);
            if (StateChanged != null) StateChanged(this);
        }

        public bool IsSettingsPage
        {
            get { return _isSettings; }
        }

        /// <summary>
        /// Applies the settings that reach into a live WebView. Called when the
        /// tab starts and again whenever something changes on the settings page.
        /// </summary>
        public void ApplyAppSettings()
        {
            if (!_isInitialized || _webView.CoreWebView2 == null) return;

            try
            {
                _webView.CoreWebView2.Settings.IsPasswordAutosaveEnabled = Settings.Current.SavePasswords;
                _webView.CoreWebView2.Settings.IsGeneralAutofillEnabled = Settings.Current.Autofill;
            }
            catch
            {
                // A tab that is mid-teardown can refuse; the next one picks it up.
            }

            ApplySiteZoom();
        }

        /// <summary>
        /// Puts the page on show at the zoom kept for its site. A site the user
        /// has never zoomed follows the page zoom in Settings.
        /// </summary>
        public void ApplySiteZoom()
        {
            if (!_isInitialized || _webView == null || _webView.CoreWebView2 == null) return;

            try
            {
                float factor = SiteSettings.Current.ZoomFor(CurrentUrl) / 100f;
                if (Math.Abs(_webView.ZoomFactor - factor) > 0.001f) _webView.ZoomFactor = factor;
            }
            catch
            {
                // A tab that is mid-teardown can refuse; the next one picks it up.
            }
        }

        /// <summary>
        /// Turns reader view on or off for the page on show. Deciding what counts
        /// as an article is left to the script, which also says so on screen when
        /// the page has nothing to read.
        /// </summary>
        public void ToggleReader()
        {
            if (!_isInitialized || _webView == null || _webView.CoreWebView2 == null) return;
            if (_isHome || _isSettings) return;

            try
            {
                _webView.CoreWebView2.ExecuteScriptAsync(ReaderMode.Script(Theme.IsDark));
            }
            catch
            {
                // The page may be gone; nothing to do.
            }
        }

        /// <summary>
        /// Shows or hides Aura's own find bar on the page. Finding is left to
        /// the script, which owns the bar and its match counts.
        /// </summary>
        public void ToggleFind()
        {
            if (!_isInitialized || _webView == null || _webView.CoreWebView2 == null) return;
            if (_isHome || _isSettings) return;

            try
            {
                _webView.CoreWebView2.ExecuteScriptAsync(FindBar.Script());
            }
            catch
            {
                // The page may be gone; nothing to search.
            }
        }

        /// <summary>
        /// Runs a small script in the page on the user's behalf, used for the
        /// clipboard actions on the page's own context menu.
        /// </summary>
        public void RunScript(string script)
        {
            if (!_isInitialized || _webView == null || _webView.CoreWebView2 == null) return;
            if (string.IsNullOrEmpty(script)) return;

            try
            {
                _webView.CoreWebView2.ExecuteScriptAsync(script);
            }
            catch
            {
                // The page may be gone.
            }
        }

        /// <summary>Redraws the settings page, used after a reset.</summary>
        public void ReloadSettingsPage()
        {
            if (!_isSettings || !_isInitialized || _webView.CoreWebView2 == null) return;
            _webView.NavigateToString(SettingsPage.GetHtml());
        }

        /// <summary>Feeds the live Shields numbers back to the settings page.</summary>
        public void RefreshSettingsStats()
        {
            if (!_isSettings || !_isInitialized || _webView.CoreWebView2 == null) return;

            try
            {
                _webView.CoreWebView2.ExecuteScriptAsync(
                    SettingsPage.StatsScript(AdBlocker.RuleCount, AdBlocker.BlockedCount));
            }
            catch
            {
                // The page may be gone; nothing to report.
            }
        }

        /// <summary>Shows a one-line result on the settings page.</summary>
        public void NotifySettingsPage(string message)
        {
            if (!_isSettings || !_isInitialized || _webView.CoreWebView2 == null) return;

            try
            {
                _webView.CoreWebView2.ExecuteScriptAsync(SettingsPage.NoticeScript(message));
            }
            catch
            {
                // The page may be gone; nothing to report.
            }
        }

        /// <summary>Re-applies the current light/dark theme to this tab.</summary>
        public void ApplyTheme()
        {
            BackColor = Theme.ContentBackground;
            if (_webView == null) return;

            _webView.DefaultBackgroundColor = Theme.ContentBackground;

            if (_isInitialized && _webView.CoreWebView2 != null)
            {
                _webView.CoreWebView2.Profile.PreferredColorScheme = Theme.IsDark
                    ? CoreWebView2PreferredColorScheme.Dark
                    : CoreWebView2PreferredColorScheme.Light;
            }

            if (_isInitialized && _webView.CoreWebView2 != null && (_isHome || _isSettings))
            {
                // Toggled rather than assigned, because the start page also keeps
                // state of its own on the body (a wallpaper needs light text, the
                // customize sheet marks itself open).
                _webView.CoreWebView2.ExecuteScriptAsync(
                    "document.body.classList.toggle('dark'," + (Theme.IsDark ? "true" : "false") + ");" +
                    "document.body.classList.toggle('light'," + (Theme.IsDark ? "false" : "true") + ");");
            }
        }

        public void Navigate(string destination)
        {
            if (string.IsNullOrWhiteSpace(destination))
            {
                LoadHomePage();
                return;
            }

            destination = destination.Trim();

            if (destination.Equals("about:home", StringComparison.OrdinalIgnoreCase) ||
                destination.Equals("home", StringComparison.OrdinalIgnoreCase))
            {
                LoadHomePage();
                return;
            }

            if (destination.Equals(SettingsPage.Address, StringComparison.OrdinalIgnoreCase) ||
                destination.Equals("settings", StringComparison.OrdinalIgnoreCase))
            {
                LoadSettingsPage();
                return;
            }

            // Check if input looks like a search query
            if (!destination.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !destination.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
                !destination.StartsWith("file://", StringComparison.OrdinalIgnoreCase) &&
                !destination.StartsWith("about:", StringComparison.OrdinalIgnoreCase))
            {
                if (destination.Contains(" ") || !destination.Contains("."))
                {
                    destination = Settings.Current.Engine.Build(destination);
                }
                else
                {
                    // Prepend https://
                    destination = "https://" + destination;
                }
            }

            CurrentUrl = destination;
            _topLevelUri = destination;
            _isHome = false;
            _isSettings = false;

            if (!_isInitialized || _webView.CoreWebView2 == null)
            {
                _pendingUrl = destination;
                return;
            }

            try
            {
                _webView.CoreWebView2.Navigate(destination);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to load page: " + ex.Message, "Navigation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void GoBack()
        {
            if (_isInitialized && _webView.CanGoBack)
            {
                _webView.GoBack();
            }
        }

        public void GoForward()
        {
            if (_isInitialized && _webView.CanGoForward)
            {
                _webView.GoForward();
            }
        }

        public void Reload()
        {
            if (_isInitialized && _webView.CoreWebView2 != null)
            {
                _webView.Reload();
            }
        }

        public void Stop()
        {
            if (_isInitialized && _webView.CoreWebView2 != null)
            {
                _webView.CoreWebView2.Stop();
            }
        }

        public string GetSourceHtml()
        {
            if (_isInitialized && _webView.CoreWebView2 != null)
            {
                try
                {
                    var task = _webView.CoreWebView2.ExecuteScriptAsync("document.documentElement.outerHTML");
                    if (task.Wait(2000))
                    {
                        string raw = task.Result;
                        if (!string.IsNullOrEmpty(raw))
                        {
                            return Regex.Unescape(raw.Trim('"'));
                        }
                    }
                }
                catch
                {
                    // Fallback
                }
            }
            return "";
        }

        private void OnWebResourceRequested(object sender, CoreWebView2WebResourceRequestedEventArgs args)
        {
            try
            {
                string uri = args.Request.Uri;

                // Never cancel the page the user asked for, or same-site frames: blocking the
                // main document would leave an error page instead of the site.
                if (!string.IsNullOrEmpty(_topLevelUri))
                {
                    if (string.Equals(uri, _topLevelUri, StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }

                    if (args.ResourceContext == CoreWebView2WebResourceContext.Document)
                    {
                        Uri top;
                        Uri request;
                        if (Uri.TryCreate(_topLevelUri, UriKind.Absolute, out top) &&
                            Uri.TryCreate(uri, UriKind.Absolute, out request) &&
                            string.Equals(request.Host, top.Host, StringComparison.OrdinalIgnoreCase))
                        {
                            return;
                        }
                    }
                }

                if (AdBlocker.ShouldBlock(uri, _topLevelUri, MapResourceType(args.ResourceContext)))
                {
                    args.Response = _webView.CoreWebView2.Environment.CreateWebResourceResponse(
                        null, 403, "Blocked by Aura Shields", "");
                }
            }
            catch
            {
                // A filter failure must never take down the page load.
            }
        }

        private static string MapResourceType(CoreWebView2WebResourceContext context)
        {
            switch (context)
            {
                case CoreWebView2WebResourceContext.Document: return "document";
                case CoreWebView2WebResourceContext.Script: return "script";
                case CoreWebView2WebResourceContext.Stylesheet: return "stylesheet";
                case CoreWebView2WebResourceContext.Image: return "image";
                case CoreWebView2WebResourceContext.Media: return "media";
                case CoreWebView2WebResourceContext.Font: return "font";
                case CoreWebView2WebResourceContext.XmlHttpRequest: return "xmlhttprequest";
                case CoreWebView2WebResourceContext.Fetch: return "xmlhttprequest";
                case CoreWebView2WebResourceContext.Websocket: return "websocket";
                case CoreWebView2WebResourceContext.Ping: return "ping";
                case CoreWebView2WebResourceContext.Manifest: return "other";
                default: return "other";
            }
        }

        private void OnNavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            _isLoading = true;

            if (!string.IsNullOrEmpty(e.Uri))
            {
                if (!e.Uri.StartsWith("data:text/html") && !e.Uri.StartsWith("about:"))
                {
                    _isHome = false;
                    _isSettings = false;
                }

                if (!e.Uri.StartsWith("data:text/html"))
                {
                    _topLevelUri = e.Uri;
                    CurrentUrl = e.Uri;
                    if (UrlChanged != null) UrlChanged(this, CurrentUrl);
                }
            }
            if (StateChanged != null) StateChanged(this);
        }

        private void OnNavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            _isLoading = false;

            // The site is known by now, so the page can be shown at its own zoom.
            ApplySiteZoom();

            // A sheet that was asked for before the page existed can open now.
            if (_openHomeCustomizerPending)
            {
                _openHomeCustomizerPending = false;
                if (_isHome) OpenHomeCustomizer();
            }

            if (!e.IsSuccess && StatusTextChanged != null)
            {
                StatusTextChanged(this, "Status: " + e.WebErrorStatus.ToString());
            }
            if (StateChanged != null) StateChanged(this);
        }

        private void OnSourceChanged(object sender, CoreWebView2SourceChangedEventArgs e)
        {
            if (_webView.CoreWebView2 != null)
            {
                string src = _webView.CoreWebView2.Source;
                if (!string.IsNullOrEmpty(src) && !src.StartsWith("data:text/html"))
                {
                    CurrentUrl = src;
                    if (UrlChanged != null) UrlChanged(this, CurrentUrl);
                }
            }
            if (StateChanged != null) StateChanged(this);
        }

        private void OnNewWindowRequested(object sender, CoreWebView2NewWindowRequestedEventArgs e)
        {
            var handler = NewWindowRequested;
            if (handler != null)
            {
                handler(this, e);
            }
            else
            {
                // Nobody is listening, so keep the click alive ourselves.
                e.Handled = true;
                if (!string.IsNullOrEmpty(e.Uri)) Navigate(e.Uri);
            }
        }

        private void OnWindowCloseRequested(object sender, object e)
        {
            if (WindowCloseRequested != null) WindowCloseRequested(this);
        }

        private void OnDownloadStarting(object sender, CoreWebView2DownloadStartingEventArgs e)
        {
            if (DownloadStarting != null) DownloadStarting(this, e);
        }

        private void OnPermissionRequested(object sender, CoreWebView2PermissionRequestedEventArgs e)
        {
            if (PermissionRequested != null)
            {
                PermissionRequested(this, e);
                return;
            }

            // Nobody is listening, so follow the browser's usual rule: a page
            // that was opened by the user may use what it asked for.
            e.State = CoreWebView2PermissionState.Allow;
            e.Handled = true;
        }

        private void OnContextMenuRequested(object sender, CoreWebView2ContextMenuRequestedEventArgs e)
        {
            var handler = ContextMenuRequested;
            if (handler != null) handler(this, e);
        }

        private void OnAcceleratorKeyPressed(object sender, CoreWebView2AcceleratorKeyPressedEventArgs e)
        {
            var handler = AcceleratorPressed;
            if (handler == null) return;

            // Key-up carries no decision; answering only key-down keeps the
            // host from acting twice on one press.
            if (e.KeyEventKind != CoreWebView2KeyEventKind.KeyDown
                && e.KeyEventKind != CoreWebView2KeyEventKind.SystemKeyDown) return;

            handler(this, (int)e.VirtualKey, e);
        }

        /// <summary>
        /// A page (usually a video) asked for its own fullscreen. Aura answers by
        /// filling the working area instead of the whole monitor, so the taskbar
        /// stays where it is.
        /// </summary>
        private void OnContainsFullScreenElementChanged(object sender, object e)
        {
            if (FullScreenChanged != null) FullScreenChanged(this, IsFullScreen);
        }

        /// <summary>True while the page inside this tab holds the fullscreen.</summary>
        public bool IsFullScreen
        {
            get
            {
                if (!_isInitialized || _webView.CoreWebView2 == null) return false;

                try
                {
                    return _webView.CoreWebView2.ContainsFullScreenElement;
                }
                catch
                {
                    return false;
                }
            }
        }

        private async void OnFaviconChanged(object sender, object e)
        {
            try
            {
                using (var stream = await _webView.CoreWebView2.GetFaviconAsync(CoreWebView2FaviconImageFormat.Png))
                {
                    if (stream == null) return;

                    using (var memory = new MemoryStream())
                    {
                        stream.CopyTo(memory);
                        byte[] bytes = memory.ToArray();
                        if (bytes.Length == 0) return;

                        FaviconDataUrl = "data:image/png;base64," + Convert.ToBase64String(bytes);
                        if (IconChanged != null) IconChanged(this);
                    }
                }
            }
            catch
            {
                // Sites without a usable icon keep the generated letter badge.
            }
        }

        private void OnDocumentTitleChanged(object sender, object e)
        {
            string title = _webView.CoreWebView2 != null ? _webView.CoreWebView2.DocumentTitle : null;
            if (string.IsNullOrWhiteSpace(title))
            {
                title = CurrentUrl == "about:home" ? "New Tab" : CurrentUrl;
            }

            string shortTitle = title;
            Text = shortTitle;
            DisplayTitle = shortTitle;

            if (TitleChanged != null) TitleChanged(this, title);
            if (StateChanged != null) StateChanged(this);
        }

        /// <summary>
        /// The settings page and the start page talk to the app through this
        /// channel. Only Aura's own pages may use it: the tab has to be showing
        /// about:settings or about:home, and the message has to come from a
        /// document Aura itself wrote, so a website loaded in this tab can never
        /// change a setting or a shortcut.
        /// </summary>
        private void OnWebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            if (!_isSettings && !_isHome) return;
            if (!IsInternalSource(e.Source)) return;

            var handler = SettingsMessage;
            if (handler != null) handler(this, e.WebMessageAsJson);
        }

        private static bool IsInternalSource(string source)
        {
            if (string.IsNullOrEmpty(source)) return true;

            return source.StartsWith("about:", StringComparison.OrdinalIgnoreCase)
                || source.StartsWith("data:", StringComparison.OrdinalIgnoreCase);
        }
    }
}
