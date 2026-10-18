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

        public event Action<BrowserTab, string> UrlChanged;
        public event Action<BrowserTab, string> TitleChanged;
        public event Action<BrowserTab> StateChanged;
        public event Action<BrowserTab, string> StatusTextChanged;

        public WebView2 WebView { get { return _webView; } }
        public string CurrentUrl { get; private set; }
        public string DisplayTitle { get; private set; }
        public bool IsLoading { get { return _isLoading; } }
        public bool CanGoBack { get { return _webView != null && _isInitialized && _webView.CanGoBack; } }
        public bool CanGoForward { get { return _webView != null && _isInitialized && _webView.CanGoForward; } }

        public BrowserTab(string initialUrl = null)
        {
            Text = "New Tab";
            DisplayTitle = "New Tab";
            BackColor = Theme.ContentBackground;

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
                string userDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AuraBrowser", "WebView2Profile");
                if (!Directory.Exists(userDataFolder))
                {
                    Directory.CreateDirectory(userDataFolder);
                }

                var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
                await _webView.EnsureCoreWebView2Async(env);

                _isInitialized = true;

                // Pages should follow Aura's light/dark choice, not the OS.
                _webView.CoreWebView2.Profile.PreferredColorScheme = Theme.IsDark
                    ? CoreWebView2PreferredColorScheme.Dark
                    : CoreWebView2PreferredColorScheme.Light;

                // Hook up AdBlocker & Brave Shields
                AdBlocker.Initialize();
                await _webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(AdBlocker.GetAdBlockInjectionScript());
                _webView.CoreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
                _webView.CoreWebView2.WebResourceRequested += OnWebResourceRequested;

                _webView.CoreWebView2.NavigationStarting += OnNavigationStarting;
                _webView.CoreWebView2.NavigationCompleted += OnNavigationCompleted;
                _webView.CoreWebView2.SourceChanged += OnSourceChanged;
                _webView.CoreWebView2.DocumentTitleChanged += OnDocumentTitleChanged;

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
            CurrentUrl = "about:home";
            _topLevelUri = null;
            Text = "New Tab";

            if (_isInitialized && _webView.CoreWebView2 != null)
            {
                _webView.NavigateToString(HomePage.GetHtml(Theme.IsDark));
            }
            else
            {
                _pendingUrl = "about:home";
            }

            if (TitleChanged != null) TitleChanged(this, "New Tab");
            if (UrlChanged != null) UrlChanged(this, "");
            if (StateChanged != null) StateChanged(this);
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

            if (_isInitialized && _webView.CoreWebView2 != null && _isHome)
            {
                _webView.CoreWebView2.ExecuteScriptAsync(
                    "document.body.className='" + (Theme.IsDark ? "dark" : "light") + "';");
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

            // Check if input looks like a search query
            if (!destination.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !destination.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
                !destination.StartsWith("file://", StringComparison.OrdinalIgnoreCase) &&
                !destination.StartsWith("about:", StringComparison.OrdinalIgnoreCase))
            {
                if (destination.Contains(" ") || !destination.Contains("."))
                {
                    destination = "https://search.brave.com/search?q=" + Uri.EscapeDataString(destination);
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
    }
}
