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
        private ProgressBar _progressBar;
        private string _pendingUrl;
        private bool _isInitialized;

        public event Action<BrowserTab, string> UrlChanged;
        public event Action<BrowserTab, string> TitleChanged;
        public event Action<BrowserTab> StateChanged;
        public event Action<BrowserTab, string> StatusTextChanged;

        public WebView2 WebView { get { return _webView; } }
        public string CurrentUrl { get; private set; }
        public bool CanGoBack { get { return _webView != null && _isInitialized && _webView.CanGoBack; } }
        public bool CanGoForward { get { return _webView != null && _isInitialized && _webView.CanGoForward; } }

        public BrowserTab(string initialUrl = null)
        {
            Text = "New Tab";
            BackColor = Color.FromArgb(10, 11, 16);

            _progressBar = new ProgressBar();
            _progressBar.Dock = DockStyle.Top;
            _progressBar.Height = 2;
            _progressBar.Visible = false;
            _progressBar.Style = ProgressBarStyle.Continuous;

            _webView = new WebView2();
            _webView.Dock = DockStyle.Fill;
            _webView.DefaultBackgroundColor = Color.FromArgb(7, 8, 11);

            _pendingUrl = string.IsNullOrEmpty(initialUrl) ? "about:home" : initialUrl;

            Controls.Add(_webView);
            Controls.Add(_progressBar);

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

                // Hook up AdBlocker & Brave Shields
                await _webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(AdBlocker.GetAdBlockInjectionScript());
                _webView.CoreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
                _webView.CoreWebView2.WebResourceRequested += (s, args) =>
                {
                    if (AdBlocker.ShouldBlock(args.Request.Uri))
                    {
                        var response = _webView.CoreWebView2.Environment.CreateWebResourceResponse(null, 403, "Blocked by Brave Shields", "");
                        args.Response = response;
                    }
                };

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
            CurrentUrl = "about:home";
            Text = "New Tab";

            if (_isInitialized && _webView.CoreWebView2 != null)
            {
                _webView.NavigateToString(HomePage.GetHtml());
            }
            else
            {
                _pendingUrl = "about:home";
            }

            if (TitleChanged != null) TitleChanged(this, "New Tab");
            if (UrlChanged != null) UrlChanged(this, "");
            if (StateChanged != null) StateChanged(this);
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

            if (!_isInitialized || _webView.CoreWebView2 == null)
            {
                _pendingUrl = destination;
                return;
            }

            try
            {
                _progressBar.Value = 30;
                _progressBar.Visible = true;
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

        private void OnNavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            _progressBar.Value = 40;
            _progressBar.Visible = true;

            if (!string.IsNullOrEmpty(e.Uri) && !e.Uri.StartsWith("data:text/html"))
            {
                CurrentUrl = e.Uri;
                if (UrlChanged != null) UrlChanged(this, CurrentUrl);
            }
            if (StateChanged != null) StateChanged(this);
        }

        private void OnNavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            _progressBar.Visible = false;
            _progressBar.Value = 0;

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
            if (shortTitle.Length > 20)
            {
                shortTitle = shortTitle.Substring(0, 18) + "..";
            }
            Text = shortTitle;

            if (TitleChanged != null) TitleChanged(this, title);
            if (StateChanged != null) StateChanged(this);
        }
    }
}
