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

        [DllImport("user32.dll")]
        private static extern short GetKeyState(int nVirtKey);

        private WebView2 _chromeWebView;
        private Panel _contentPanel;
        private TabControl _tabControl;

        private readonly BookmarkManager _bookmarkManager;
        private readonly HistoryManager _historyManager;
        private bool _chromeReady;
        private Timer _shieldsTimer;

        public MainForm()
        {
            Theme.LoadPreference();
            _bookmarkManager = new BookmarkManager();
            _historyManager = new HistoryManager();

            Application.AddMessageFilter(new ShortcutFilter(this));

            InitializeWindow();
            InitializeChrome();
            AddNewTab();
        }

        private void InitializeWindow()
        {
            Text = "Aura";
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(1280, 820);
            MinimumSize = new Size(620, 420);
            BackColor = Theme.WindowBackground;
            DoubleBuffered = true;
            SetStyle(ControlStyles.ResizeRedraw, true);

            // Top chrome WebView2: tab strip + toolbar, rendered as one HTML surface.
            _chromeWebView = new WebView2();
            _chromeWebView.Dock = DockStyle.Top;
            _chromeWebView.Height = ChromeHtml.ChromeHeight;
            _chromeWebView.DefaultBackgroundColor = Theme.ChromeTop;

            // Main Content Area
            _contentPanel = new Panel();
            _contentPanel.Dock = DockStyle.Fill;
            _contentPanel.BackColor = Theme.ContentBackground;

            _tabControl = new TabControl();
            _tabControl.ItemSize = new Size(0, 1);
            _tabControl.SizeMode = TabSizeMode.Fixed;
            _tabControl.Appearance = TabAppearance.Buttons;
            _tabControl.Padding = new Point(0, 0);
            _tabControl.BackColor = Theme.ContentBackground;
            _tabControl.SelectedIndexChanged += delegate
            {
                SyncAllToChrome();
            };

            _contentPanel.Controls.Add(_tabControl);
            _contentPanel.Resize += delegate { LayoutTabs(); };

            Controls.Add(_contentPanel);
            Controls.Add(_chromeWebView);

            LayoutTabs();
            // Subtle border
            Paint += (s, e) =>
            {
                using (var pen = new Pen(Theme.Border, 1))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
                }
            };

            // Keep the Shields pill counter live as requests are blocked in the background.
            _shieldsTimer = new Timer();
            _shieldsTimer.Interval = 2000;
            _shieldsTimer.Tick += delegate { SyncShields(); };
            _shieldsTimer.Start();

            Theme.Changed += ApplyTheme;
        }

        private void ApplyTheme()
        {
            BackColor = Theme.WindowBackground;
            _chromeWebView.DefaultBackgroundColor = Theme.ChromeTop;
            _contentPanel.BackColor = Theme.ContentBackground;
            _tabControl.BackColor = Theme.ContentBackground;

            if (_chromeWebView.CoreWebView2 != null)
            {
                _chromeWebView.CoreWebView2.ExecuteScriptAsync(
                    "document.body.className='" + (Theme.IsDark ? "dark" : "light") + "';");
            }

            foreach (TabPage page in _tabControl.TabPages)
            {
                var tab = page as BrowserTab;
                if (tab != null) tab.ApplyTheme();
            }

            Invalidate(true);
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

                _chromeWebView.NavigateToString(ChromeHtml.GetHtml(Theme.IsDark));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error initializing Aura chrome: " + ex.Message, "Aura Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                            FocusContent();
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
                    case "stop":
                        if (ActiveTab != null) ActiveTab.Stop();
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
                    case "share":
                        ShareCurrentPage();
                        break;
                    case "about":
                        ShowAbout();
                        break;
                    case "focus_url":
                        FocusUrlBar();
                        break;
                    case "blur_url":
                        FocusContent();
                        break;
                    case "close_active_tab":
                        CloseTabAt(_tabControl.SelectedIndex);
                        break;
                    case "next_tab":
                        CycleTab(1);
                        break;
                    case "prev_tab":
                        CycleTab(-1);
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

                string url = "";
                var browserTab = _tabControl.TabPages[i] as BrowserTab;
                if (browserTab != null)
                {
                    if (!string.IsNullOrEmpty(browserTab.DisplayTitle)) title = browserTab.DisplayTitle;
                    url = browserTab.CurrentUrl ?? "";
                    if (url == "about:home") url = "";
                }

                tabItems.Add(string.Format("{{\"title\":\"{0}\",\"url\":\"{1}\"}}",
                    EscapeJs(title), EscapeJs(url)));
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
            bool loading = ActiveTab != null && ActiveTab.IsLoading;
            _chromeWebView.CoreWebView2.ExecuteScriptAsync(
                string.Format("window.BraveAPI && window.BraveAPI.setNavState({0}, {1}, {2});",
                    canBack ? "true" : "false",
                    canForward ? "true" : "false",
                    loading ? "true" : "false"));
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
            shield.Location = new Point(Math.Max(10, Right - 356), Top + ChromeHtml.ChromeHeight + 6);
            shield.Show(this);
        }

        private void FocusContent()
        {
            BeginInvoke(new Action(delegate
            {
                if (ActiveTab != null && ActiveTab.WebView != null)
                {
                    ActiveTab.WebView.Focus();
                }
            }));
        }

        private void FocusUrlBar()
        {
            // Defer: moving focus during PreFilterMessage can be undone by the
            // control that still owns the key message.
            BeginInvoke(new Action(delegate
            {
                if (_chromeWebView.CoreWebView2 == null) return;

                _chromeWebView.Focus();
                _chromeWebView.CoreWebView2.PostWebMessageAsJson("{\"action\":\"focus_url\"}");
                _chromeWebView.CoreWebView2.ExecuteScriptAsync(
                    "window.BraveAPI && window.BraveAPI.focusUrl();");
            }));
        }

        private void LayoutTabs()
        {
            if (_tabControl == null || _contentPanel == null) return;

            int w = _contentPanel.ClientSize.Width;
            int h = _contentPanel.ClientSize.Height;
            if (w <= 0 || h <= 0) return;

            // A TabControl insets its pages by the system-drawn border and tab
            // header (4px left, 5px top, 4px right/bottom with our 1px header).
            // Oversize and lift it so that inset lands outside the panel and the
            // page meets the chrome with no system-coloured frame.
            _tabControl.Bounds = new Rectangle(-4, -5, w + 8, h + 9);
        }

        private void CycleTab(int delta)
        {
            int count = _tabControl.TabPages.Count;
            if (count < 2) return;
            _tabControl.SelectedIndex = (_tabControl.SelectedIndex + delta + count) % count;
        }

        private void ShareCurrentPage()
        {
            if (ActiveTab == null) return;
            string url = ActiveTab.CurrentUrl;
            if (string.IsNullOrEmpty(url) || url == "about:home") return;

            try
            {
                Clipboard.SetText(url);
                if (_chromeWebView.CoreWebView2 != null)
                {
                    _chromeWebView.CoreWebView2.ExecuteScriptAsync(
                        "window.BraveAPI && window.BraveAPI.toast('Link copied');");
                }
            }
            catch
            {
                // The clipboard can be briefly locked by another process.
            }
        }

        private void ShowAbout()
        {
            MessageBox.Show(
                "Aura\n\n" +
                "A lightweight browser with a Safari-inspired interface and Brave-grade Shields.\n\n" +
                "Chromium engine powered by Microsoft Edge WebView2.\n" +
                "Version 2.0",
                "About Aura",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        internal bool HandleShortcutKey(int vk)
        {
            if (ActiveForm != this) return false;

            bool ctrl = (GetKeyState(0x11) & 0x8000) != 0;
            bool alt = (GetKeyState(0x12) & 0x8000) != 0;
            bool shift = (GetKeyState(0x10) & 0x8000) != 0;

            string action = null;

            if (ctrl && !alt)
            {
                switch (vk)
                {
                    case 0x54: action = "new_tab"; break;                          // Ctrl+T
                    case 0x57: action = "close_tab"; break;                        // Ctrl+W
                    case 0x4C: action = "focus_url"; break;                        // Ctrl+L
                    case 0x52: action = "reload"; break;                           // Ctrl+R
                    case 0x44: action = "bookmark"; break;                         // Ctrl+D
                    case 0x48: action = "history"; break;                          // Ctrl+H
                    case 0x55: action = "source"; break;                           // Ctrl+U
                    case 0x09: action = shift ? "prev_tab" : "next_tab"; break;    // Ctrl+Tab
                }
            }
            else if (alt && !ctrl)
            {
                if (vk == 0x25) action = "back";        // Alt+Left
                else if (vk == 0x27) action = "forward"; // Alt+Right
            }
            else if (!ctrl && !alt && vk == 0x74)       // F5
            {
                action = "reload";
            }

            if (action == null) return false;

            HandleShortcutAction(action);
            return true;
        }

        private void HandleShortcutAction(string action)
        {
            switch (action)
            {
                case "new_tab": AddNewTab(); break;
                case "close_tab": CloseTabAt(_tabControl.SelectedIndex); break;
                case "focus_url": FocusUrlBar(); break;
                case "reload": if (ActiveTab != null) ActiveTab.Reload(); break;
                case "bookmark": ToggleBookmarkCurrent(); break;
                case "history": ShowHistory(); break;
                case "source": ShowPageSource(); break;
                case "back": if (ActiveTab != null) ActiveTab.GoBack(); break;
                case "forward": if (ActiveTab != null) ActiveTab.GoForward(); break;
                case "next_tab": CycleTab(1); break;
                case "prev_tab": CycleTab(-1); break;
            }
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
            menu.BackColor = Theme.CardBackground;
            menu.ForeColor = Theme.TextPrimary;
            menu.Font = new Font("Segoe UI", 9.5F);
            menu.ShowImageMargin = false;
            menu.ShowCheckMargin = false;
            menu.Padding = new Padding(5, 6, 5, 6);
            menu.Renderer = new ToolStripProfessionalRenderer(new AuraMenuColors());

            var newTabItem = new ToolStripMenuItem("New Tab", null, delegate { AddNewTab(); });
            newTabItem.ShortcutKeyDisplayString = "Ctrl+T";

            var bookmarksItem = new ToolStripMenuItem("Bookmark This Page", null, delegate { ToggleBookmarkCurrent(); });
            bookmarksItem.ShortcutKeyDisplayString = "Ctrl+D";

            var historyItem = new ToolStripMenuItem("History", null, delegate { ShowHistory(); });
            historyItem.ShortcutKeyDisplayString = "Ctrl+H";

            var sourceItem = new ToolStripMenuItem("View Page Source", null, delegate { ShowPageSource(); });
            sourceItem.ShortcutKeyDisplayString = "Ctrl+U";

            var shieldItem = new ToolStripMenuItem("Aura Shields", null, delegate { ShowBraveShields(); });

            var appearanceItem = new ToolStripMenuItem("Appearance");
            var lightItem = new ToolStripMenuItem("Light", null, delegate { Theme.SetPreference("light"); });
            var darkItem = new ToolStripMenuItem("Dark", null, delegate { Theme.SetPreference("dark"); });
            var systemItem = new ToolStripMenuItem("Use System Theme", null, delegate { Theme.SetPreference("system"); });
            appearanceItem.DropDownItems.Add(lightItem);
            appearanceItem.DropDownItems.Add(darkItem);
            appearanceItem.DropDownItems.Add(new ToolStripSeparator());
            appearanceItem.DropDownItems.Add(systemItem);

            var appearanceMenu = appearanceItem.DropDown as ToolStripDropDownMenu;
            if (appearanceMenu != null)
            {
                appearanceMenu.ShowCheckMargin = true;
                appearanceMenu.ShowImageMargin = false;
            }

            menu.Opening += delegate
            {
                lightItem.Checked = Theme.Preference == "light";
                darkItem.Checked = Theme.Preference == "dark";
                systemItem.Checked = Theme.Preference == "system";
            };

            var aboutItem = new ToolStripMenuItem("About Aura", null, delegate { ShowAbout(); });
            var quitItem = new ToolStripMenuItem("Quit Aura", null, delegate { Close(); });

            menu.Items.Add(newTabItem);
            menu.Items.Add(bookmarksItem);
            menu.Items.Add(historyItem);
            menu.Items.Add(sourceItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(shieldItem);
            menu.Items.Add(appearanceItem);
            menu.Items.Add(aboutItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(quitItem);

            menu.Show(PointToScreen(new Point(Width - 208, ChromeHtml.ChromeHeight + 4)));
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

    /// <summary>
    /// Keyboard shortcuts have to be caught before the WebView2 child window
    /// swallows them, so they are filtered at the application message loop.
    /// </summary>
    internal class ShortcutFilter : IMessageFilter
    {
        private readonly MainForm _form;

        public ShortcutFilter(MainForm form)
        {
            _form = form;
        }

        public bool PreFilterMessage(ref Message m)
        {
            // WM_KEYDOWN / WM_SYSKEYDOWN
            if (m.Msg != 0x0100 && m.Msg != 0x0104) return false;
            return _form.HandleShortcutKey(m.WParam.ToInt32());
        }
    }

    internal class AuraMenuColors : ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground { get { return Theme.CardBackground; } }
        public override Color ImageMarginGradientBegin { get { return Theme.CardBackground; } }
        public override Color ImageMarginGradientMiddle { get { return Theme.CardBackground; } }
        public override Color ImageMarginGradientEnd { get { return Theme.CardBackground; } }
        public override Color MenuItemSelected { get { return Theme.Tint(Theme.AccentBlue, 34); } }
        public override Color MenuItemSelectedGradientBegin { get { return Theme.Tint(Theme.AccentBlue, 34); } }
        public override Color MenuItemSelectedGradientEnd { get { return Theme.Tint(Theme.AccentBlue, 34); } }
        public override Color MenuItemBorder { get { return Color.Transparent; } }
        public override Color MenuBorder { get { return Theme.CardBorder; } }
        public override Color SeparatorDark { get { return Theme.CardBorder; } }
        public override Color SeparatorLight { get { return Theme.CardBorder; } }
    }
}
