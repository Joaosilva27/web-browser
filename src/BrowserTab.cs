using System;
using System.Drawing;
using System.Windows.Forms;

namespace SimpleBrowser
{
    public class BrowserTab : TabPage
    {
        private WebBrowser _browser;
        private ProgressBar _progressBar;

        public event Action<BrowserTab, string> UrlChanged;
        public event Action<BrowserTab, string> TitleChanged;
        public event Action<BrowserTab> StateChanged;
        public event Action<BrowserTab, string> StatusTextChanged;

        public WebBrowser Browser { get { return _browser; } }
        public string CurrentUrl { get; private set; }
        public bool CanGoBack { get { return _browser != null && _browser.CanGoBack; } }
        public bool CanGoForward { get { return _browser != null && _browser.CanGoForward; } }

        public BrowserTab(string initialUrl = null)
        {
            Text = "New Tab";
            BackColor = Color.FromArgb(32, 34, 37);

            _progressBar = new ProgressBar();
            _progressBar.Dock = DockStyle.Top;
            _progressBar.Height = 3;
            _progressBar.Visible = false;
            _progressBar.Style = ProgressBarStyle.Continuous;

            _browser = new WebBrowser();
            _browser.Dock = DockStyle.Fill;
            _browser.ScriptErrorsSuppressed = true;
            _browser.IsWebBrowserContextMenuEnabled = true;

            _browser.Navigating += OnNavigating;
            _browser.Navigated += OnNavigated;
            _browser.DocumentCompleted += OnDocumentCompleted;
            _browser.ProgressChanged += OnProgressChanged;
            _browser.StatusTextChanged += OnStatusTextChanged;

            Controls.Add(_browser);
            Controls.Add(_progressBar);

            if (string.IsNullOrEmpty(initialUrl) || initialUrl == "about:blank")
            {
                LoadHomePage();
            }
            else
            {
                Navigate(initialUrl);
            }
        }

        public void LoadHomePage()
        {
            CurrentUrl = "about:home";
            _browser.DocumentText = HomePage.GetHtml();
            Text = "New Tab";
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
                    // Search Google
                    destination = "https://www.google.com/search?q=" + Uri.EscapeDataString(destination);
                }
                else
                {
                    // Prepend https://
                    destination = "https://" + destination;
                }
            }

            try
            {
                CurrentUrl = destination;
                _progressBar.Value = 10;
                _progressBar.Visible = true;
                _browser.Navigate(destination);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to load page: " + ex.Message, "Navigation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public string GetSourceHtml()
        {
            try
            {
                if (_browser.Document != null)
                {
                    var doc = _browser.Document.DomDocument as dynamic;
                    if (doc != null && doc.documentElement != null)
                    {
                        return doc.documentElement.outerHTML;
                    }
                }
            }
            catch
            {
                // Fallback to DocumentText
            }
            return _browser.DocumentText;
        }

        private void OnNavigating(object sender, WebBrowserNavigatingEventArgs e)
        {
            _progressBar.Value = 20;
            _progressBar.Visible = true;
            if (e.Url != null)
            {
                string target = e.Url.ToString();
                if (!target.StartsWith("about:blank"))
                {
                    CurrentUrl = target;
                    if (UrlChanged != null) UrlChanged(this, CurrentUrl);
                }
            }
            if (StateChanged != null) StateChanged(this);
        }

        private void OnNavigated(object sender, WebBrowserNavigatedEventArgs e)
        {
            if (e.Url != null)
            {
                string url = e.Url.ToString();
                if (!url.StartsWith("about:blank"))
                {
                    CurrentUrl = url;
                    if (UrlChanged != null) UrlChanged(this, CurrentUrl);
                }
            }
            if (StateChanged != null) StateChanged(this);
        }

        private void OnDocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            _progressBar.Visible = false;
            _progressBar.Value = 0;

            string title = _browser.DocumentTitle;
            if (string.IsNullOrWhiteSpace(title))
            {
                title = CurrentUrl == "about:home" ? "New Tab" : CurrentUrl;
            }

            // Truncate long tab titles
            string shortTitle = title;
            if (shortTitle.Length > 20)
            {
                shortTitle = shortTitle.Substring(0, 18) + "..";
            }
            Text = shortTitle;

            if (TitleChanged != null) TitleChanged(this, title);
            if (StateChanged != null) StateChanged(this);
        }

        private void OnProgressChanged(object sender, WebBrowserProgressChangedEventArgs e)
        {
            if (e.MaximumProgress > 0 && e.CurrentProgress >= 0)
            {
                int val = (int)((e.CurrentProgress * 100) / e.MaximumProgress);
                if (val >= 0 && val <= 100)
                {
                    _progressBar.Value = Math.Min(100, val);
                }
            }
        }

        private void OnStatusTextChanged(object sender, EventArgs e)
        {
            if (StatusTextChanged != null)
            {
                StatusTextChanged(this, _browser.StatusText);
            }
        }
    }
}
