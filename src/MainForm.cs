using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace SimpleBrowser
{
    public class MainForm : Form
    {
        // Win32 constants for frameless resizing and dragging
        private const int WM_NCHITTEST = 0x84;
        private const int HTCLIENT = 1;
        private const int HTLEFT = 10;
        private const int HTRIGHT = 11;
        private const int HTTOP = 12;
        private const int HTTOPLEFT = 13;
        private const int HTTOPRIGHT = 14;
        private const int HTBOTTOM = 15;
        private const int HTBOTTOMLEFT = 16;
        private const int HTBOTTOMRIGHT = 17;
        private const int BorderResizeMargin = 6;

        private WebView2 _chromeWebView;
        private Panel _contentPanel;
        private TabControl _tabControl;

        private readonly BookmarkManager _bookmarkManager;
        private readonly HistoryManager _historyManager;
        private bool _chromeReady;
        private Timer _shieldsTimer;

        public MainForm()
        {
            _bookmarkManager = new BookmarkManager();
            _historyManager = new HistoryManager();

            InitializeWindow();
            InitializeChrome();
            AddNewTab();
        }

        private void InitializeWindow()
        {
            Text = "Brave";
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(1280, 820);
            MinimumSize = new Size(600, 400);
            BackColor = Color.FromArgb(30, 32, 41);
            DoubleBuffered = true;
            SetStyle(ControlStyles.ResizeRedraw, true);

            // Top Chrome WebView2 Container (Height: 110 for exact Brave tabstrip + toolbar + bookmarks bar)
            _chromeWebView = new WebView2();
            _chromeWebView.Dock = DockStyle.Top;
            _chromeWebView.Height = 110;
            _chromeWebView.DefaultBackgroundColor = Color.FromArgb(30, 32, 41);

            // Main Content Area
            _contentPanel = new Panel();
            _contentPanel.Dock = DockStyle.Fill;
            _contentPanel.BackColor = Color.FromArgb(17, 16, 25);

            _tabControl = new TabControl();
            _tabControl.Dock = DockStyle.Fill;
            _tabControl.ItemSize = new Size(0, 1);
            _tabControl.SizeMode = TabSizeMode.Fixed;
            _tabControl.Appearance = TabAppearance.Buttons;
            _tabControl.Padding = new Point(0, 0);
            _tabControl.SelectedIndexChanged += delegate
            {
                SyncAllToChrome();
            };

            _contentPanel.Controls.Add(_tabControl);

            Controls.Add(_contentPanel);
            Controls.Add(_chromeWebView);

            // Subtle border
            Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(50, 53, 68), 1))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
                }
            };

            // Keep the Shields pill counter live as requests are blocked in the background.
            _shieldsTimer = new Timer();
            _shieldsTimer.Interval = 2000;
            _shieldsTimer.Tick += delegate { SyncShields(); };
            _shieldsTimer.Start();
        }

        private async void InitializeChrome()
        {
            try
            {
                string userDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AuraBrowser", "BraveChromeProfile");
                if (!Directory.Exists(userDataFolder))
                {
                    Directory.CreateDirectory(userDataFolder);
                }

                var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
                await _chromeWebView.EnsureCoreWebView2Async(env);

                _chromeWebView.CoreWebView2.Settings.IsStatusBarEnabled = false;
                _chromeWebView.CoreWebView2.Settings.AreDevToolsEnabled = false;
                _chromeWebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;

                _chromeWebView.CoreWebView2.WebMessageReceived += OnChromeMessage;
                _chromeWebView.CoreWebView2.DOMContentLoaded += delegate
                {
                    _chromeReady = true;
                    SyncAllToChrome();
                };

                _chromeWebView.NavigateToString(ChromeHtml.GetHtml());
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error initializing Brave Chrome: " + ex.Message, "Brave Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnChromeMessage(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                string json = e.WebMessageAsJson;
                if (string.IsNullOrEmpty(json)) return;

                string action = ExtractJsonField(json, "action");
                if (string.IsNullOrEmpty(action)) return;

                switch (action)
                {
                    case "window_close":
                        Close();
                        break;
                    case "window_min":
                        WindowState = FormWindowState.Minimized;
                        break;
                    case "window_max":
                        WindowState = (WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized);
                        break;
                    case "switch_tab":
                        int tabIdx = ExtractJsonInt(json, "index");
                        if (tabIdx >= 0 && tabIdx < _tabControl.TabPages.Count)
                        {
                            _tabControl.SelectedIndex = tabIdx;
                        }
                        break;
                    case "new_tab":
                        AddNewTab();
                        break;
                    case "close_tab":
                        int closeIdx = ExtractJsonInt(json, "index");
                        CloseTabAt(closeIdx);
                        break;
                    case "navigate":
                        string targetUrl = ExtractJsonField(json, "url");
                        if (ActiveTab != null)
                        {
                            ActiveTab.Navigate(targetUrl);
                        }
                        break;
                    case "back":
                        if (ActiveTab != null) ActiveTab.GoBack();
                        break;
                    case "forward":
                        if (ActiveTab != null) ActiveTab.GoForward();
                        break;
                    case "reload":
                        if (ActiveTab != null) ActiveTab.Reload();
                        break;
                    case "home":
                        if (ActiveTab != null) ActiveTab.LoadHomePage();
                        break;
                    case "bookmark":
                        ToggleBookmarkCurrent();
                        break;
                    case "history":
                        ShowHistory();
                        break;
                    case "source":
                        ShowPageSource();
                        break;
                    case "shield":
                        ShowBraveShields();
                        break;
                    case "menu":
                        ShowBraveMenu();
                        break;
                }
            }
            catch
            {
                // Ignore parse errors
            }
        }

        private BrowserTab ActiveTab
        {
            get
            {
                if (_tabControl.SelectedTab is BrowserTab)
                {
                    return (BrowserTab)_tabControl.SelectedTab;
                }
                return null;
            }
        }

        public void AddNewTab(string url = null)
        {
            var tab = new BrowserTab(url);
            tab.UrlChanged += OnTabUrlChanged;
            tab.TitleChanged += OnTabTitleChanged;
            tab.StateChanged += OnTabStateChanged;

            _tabControl.TabPages.Add(tab);
            _tabControl.SelectedTab = tab;

            SyncAllToChrome();
        }

        public void CloseTabAt(int index)
        {
            if (_tabControl.TabPages.Count > 1 && index >= 0 && index < _tabControl.TabPages.Count)
            {
                var tab = _tabControl.TabPages[index];
                _tabControl.TabPages.RemoveAt(index);
                tab.Dispose();
                _tabControl.SelectedIndex = Math.Max(0, index - 1);
            }
            else if (_tabControl.TabPages.Count == 1 && ActiveTab != null)
            {
                ActiveTab.LoadHomePage();
            }
            SyncAllToChrome();
        }

        private void ToggleBookmarkCurrent()
        {
            if (ActiveTab == null) return;
            string url = ActiveTab.CurrentUrl;
            string title = ActiveTab.Text;

            if (string.IsNullOrEmpty(url) || url == "about:home") return;

            if (_bookmarkManager.IsBookmarked(url))
            {
                _bookmarkManager.RemoveBookmark(url);
            }
            else
            {
                _bookmarkManager.AddBookmark(title, url);
            }
            SyncBookmarked();
        }

        private void SyncAllToChrome()
        {
            SyncTabs();
            SyncUrl();
            SyncNavState();
            SyncBookmarked();
            SyncShields();
        }

        private void SyncShields()
        {
            if (!_chromeReady || _chromeWebView.CoreWebView2 == null) return;
            int count = AdBlocker.BlockedCount;
            _chromeWebView.CoreWebView2.ExecuteScriptAsync(
                string.Format("document.getElementById('shields-count') && (document.getElementById('shields-count').innerText = '{0}');", count));
        }

        private void SyncTabs()
        {
            if (!_chromeReady || _chromeWebView.CoreWebView2 == null) return;

            var tabItems = new List<string>();
            for (int i = 0; i < _tabControl.TabPages.Count; i++)
            {
                string title = _tabControl.TabPages[i].Text;
                if (string.IsNullOrEmpty(title)) title = "New Tab";
                tabItems.Add(string.Format("{{\"title\":\"{0}\"}}", EscapeJs(title)));
            }
            string json = "[" + string.Join(",", tabItems.ToArray()) + "]";
            int activeIndex = _tabControl.SelectedIndex;

            _chromeWebView.CoreWebView2.ExecuteScriptAsync(
                string.Format("window.BraveAPI && window.BraveAPI.renderTabs({0}, {1});", json, activeIndex));
        }

        private void SyncUrl()
        {
            if (!_chromeReady || _chromeWebView.CoreWebView2 == null) return;
            string url = ActiveTab != null ? ActiveTab.CurrentUrl : "";
            _chromeWebView.CoreWebView2.ExecuteScriptAsync(
                string.Format("window.BraveAPI && window.BraveAPI.setUrl('{0}');", EscapeJs(url ?? "")));
        }

        private void SyncNavState()
        {
            if (!_chromeReady || _chromeWebView.CoreWebView2 == null) return;
            bool canBack = ActiveTab != null && ActiveTab.CanGoBack;
            bool canForward = ActiveTab != null && ActiveTab.CanGoForward;
            _chromeWebView.CoreWebView2.ExecuteScriptAsync(
                string.Format("window.BraveAPI && window.BraveAPI.setNavState({0}, {1});", canBack ? "true" : "false", canForward ? "true" : "false"));
        }

        private void SyncBookmarked()
        {
            if (!_chromeReady || _chromeWebView.CoreWebView2 == null) return;
            string url = ActiveTab != null ? ActiveTab.CurrentUrl : "";
            bool isBookmarked = !string.IsNullOrEmpty(url) && _bookmarkManager.IsBookmarked(url);
            _chromeWebView.CoreWebView2.ExecuteScriptAsync(
                string.Format("window.BraveAPI && window.BraveAPI.setBookmarked({0});", isBookmarked ? "true" : "false"));
        }

        private void OnTabUrlChanged(BrowserTab tab, string url)
        {
            if (tab == ActiveTab)
            {
                SyncUrl();
                SyncBookmarked();
            }
        }

        private void OnTabTitleChanged(BrowserTab tab, string title)
        {
            SyncTabs();
            if (!string.IsNullOrEmpty(tab.CurrentUrl) && tab.CurrentUrl != "about:home")
            {
                _historyManager.AddEntry(title, tab.CurrentUrl);
            }
        }

        private void OnTabStateChanged(BrowserTab tab)
        {
            if (tab == ActiveTab)
            {
                SyncNavState();
            }
        }

        private void ShowBraveShields()
        {
            string url = ActiveTab != null ? ActiveTab.CurrentUrl : "";
            var shield = new BraveShieldsForm(url);
            shield.Location = new Point(Math.Max(10, Right - 350), Top + 80);
            shield.Show(this);
        }

        private void ShowHistory()
        {
            using (var historyForm = new HistoryForm(_historyManager))
            {
                if (historyForm.ShowDialog(this) == DialogResult.OK && !string.IsNullOrEmpty(historyForm.SelectedUrl))
                {
                    if (ActiveTab != null)
                    {
                        ActiveTab.Navigate(historyForm.SelectedUrl);
                    }
                }
            }
        }

        private void ShowPageSource()
        {
            if (ActiveTab == null) return;
            string html = ActiveTab.GetSourceHtml();
            string url = ActiveTab.CurrentUrl;

            using (var form = new SourceViewerForm(url, html))
            {
                form.ShowDialog(this);
            }
        }

        private void ShowBraveMenu()
        {
            var menu = new ContextMenuStrip();
            menu.BackColor = Color.FromArgb(30, 32, 41);
            menu.ForeColor = Color.White;

            var newTabItem = new ToolStripMenuItem("New Tab\tCtrl+T", null, delegate { AddNewTab(); });
            var bookmarksItem = new ToolStripMenuItem("Bookmark Page\tCtrl+D", null, delegate { ToggleBookmarkCurrent(); });
            var historyItem = new ToolStripMenuItem("History\tCtrl+H", null, delegate { ShowHistory(); });
            var sourceItem = new ToolStripMenuItem("View Page Source\tCtrl+U", null, delegate { ShowPageSource(); });
            var shieldItem = new ToolStripMenuItem("🦁 Brave Shields...", null, delegate { ShowBraveShields(); });
            var aboutItem = new ToolStripMenuItem("About Brave Browser Edition...", null, delegate
            {
                MessageBox.Show(
                    "Brave Browser Desktop Edition\n\n" +
                    "Exact Brave Browser UI, Brave Shields, Brave Search, and Edge Chromium engine.\n\n" +
                    "Version 1.0 (Official UI Replica)",
                    "About Brave",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            });

            menu.Items.Add(newTabItem);
            menu.Items.Add(bookmarksItem);
            menu.Items.Add(historyItem);
            menu.Items.Add(sourceItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(shieldItem);
            menu.Items.Add(aboutItem);

            menu.Show(PointToScreen(new Point(Width - 190, 80)));
        }

        // Native Resizing
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_NCHITTEST)
            {
                base.WndProc(ref m);
                Point pos = PointToClient(new Point(m.LParam.ToInt32()));

                if (pos.X <= BorderResizeMargin && pos.Y <= BorderResizeMargin)
                    m.Result = (IntPtr)HTTOPLEFT;
                else if (pos.X >= ClientSize.Width - BorderResizeMargin && pos.Y <= BorderResizeMargin)
                    m.Result = (IntPtr)HTTOPRIGHT;
                else if (pos.X <= BorderResizeMargin && pos.Y >= ClientSize.Height - BorderResizeMargin)
                    m.Result = (IntPtr)HTBOTTOMLEFT;
                else if (pos.X >= ClientSize.Width - BorderResizeMargin && pos.Y >= ClientSize.Height - BorderResizeMargin)
                    m.Result = (IntPtr)HTBOTTOMRIGHT;
                else if (pos.X <= BorderResizeMargin)
                    m.Result = (IntPtr)HTLEFT;
                else if (pos.X >= ClientSize.Width - BorderResizeMargin)
                    m.Result = (IntPtr)HTRIGHT;
                else if (pos.Y <= BorderResizeMargin)
                    m.Result = (IntPtr)HTTOP;
                else if (pos.Y >= ClientSize.Height - BorderResizeMargin)
                    m.Result = (IntPtr)HTBOTTOM;
                return;
            }

            base.WndProc(ref m);
        }

        private static string EscapeJs(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("'", "\\'").Replace("\"", "\\\"").Replace("\r", "").Replace("\n", " ");
        }

        private static string ExtractJsonField(string json, string field)
        {
            string pattern = "\"" + field + "\"\\s*:\\s*\"([^\"]*)\"";
            var match = System.Text.RegularExpressions.Regex.Match(json, pattern);
            if (match.Success) return match.Groups[1].Value;
            return "";
        }

        private static int ExtractJsonInt(string json, string field)
        {
            string pattern = "\"" + field + "\"\\s*:\\s*([0-9]+)";
            var match = System.Text.RegularExpressions.Regex.Match(json, pattern);
            if (match.Success)
            {
                int val;
                if (int.TryParse(match.Groups[1].Value, out val)) return val;
            }
            return -1;
        }
    }
}
