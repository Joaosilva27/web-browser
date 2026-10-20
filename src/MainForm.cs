using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace SimpleBrowser
{
    public class MainForm : Form
    {
        // Win32 constants for frameless resizing and dragging
        private const int WM_NCHITTEST = 0x84;
        private const int WM_DPICHANGED = 0x02E0;
        private const int WM_GETMINMAXINFO = 0x24;
        private const int MONITOR_DEFAULTTONEAREST = 2;
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

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        // A ContextMenuStrip normally closes itself when you click outside it, but
        // the WebView2 (Chromium) child windows swallow those clicks. Watch the
        // mouse globally while the menu is open so it dismisses like a real menu.
        private const int WH_MOUSE_LL = 14;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_RBUTTONDOWN = 0x0204;
        private const int WM_MBUTTONDOWN = 0x0207;

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct NativePoint
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MouseHookInfo
        {
            public NativePoint Pt;
            public uint MouseData;
            public uint Flags;
            public uint Time;
            public IntPtr ExtraInfo;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc callback, IntPtr hMod, uint threadId);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hook);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hook, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr window, int flags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);

        private WebView2 _chromeWebView;
        private Panel _contentPanel;
        private TabControl _tabControl;

        private readonly BookmarkManager _bookmarkManager;
        private readonly HistoryManager _historyManager;
        private readonly bool _privateMode;
        private readonly ShortcutFilter _shortcuts;
        private bool _chromeReady;

        /// <summary>Every window Toge has open, so a new one can be counted and
        /// all of them can be persisted together.</summary>
        internal static readonly List<MainForm> Windows = new List<MainForm>();

        /// <summary>Windows the user closed, newest last: the pages each one
        /// was showing, kept so Ctrl+Shift+T can bring the whole window back.</summary>
        private static readonly List<List<string>> ClosedWindows = new List<List<string>>();
        private bool _windowStateKnown;
        private bool _windowMaximized;
        private bool _windowIsFullScreen;
        private FormWindowState _windowStateBeforeFullScreen = FormWindowState.Normal;
        private Timer _shieldsTimer;

        private ContextMenuStrip _openMenu;
        private IntPtr _menuMouseHook;
        private LowLevelMouseProc _menuMouseHookProc;
        private bool _menuDismissedByButton;
        private DateTime _menuDismissedAtUtc = DateTime.MinValue;

        public MainForm(bool privateMode = false, List<string> startupTabs = null, int activeTab = 0)
        {
            _privateMode = privateMode;
            _startupTabs = startupTabs;
            _startupActive = activeTab;

            AppIcon.Apply(this);

            Theme.LoadPreference();
            Settings.Current.ApplyThemePreference();

            // Bookmarks and history are shared between every window, so two
            // windows cannot overwrite each other's files.
            if (_sharedBookmarks == null) _sharedBookmarks = new BookmarkManager();
            if (_sharedHistory == null) _sharedHistory = new HistoryManager();
            _bookmarkManager = _sharedBookmarks;
            _historyManager = _sharedHistory;

            Windows.Add(this);

            _shortcuts = new ShortcutFilter(this);
            Application.AddMessageFilter(_shortcuts);

            // The downloads list is shared, so every window watches it to keep
            // its own toolbar count in step.
            DownloadsManager.Current.Changed += OnDownloadsChanged;

            // The Google account is shared too: every start page redraws its
            // account chip when the profile signs in or out.
            GoogleAccount.Changed += OnGoogleAccountChanged;

            InitializeWindow();
            InitializeChrome();

            // The suggestion list hangs outside the chrome strip, so it has to
            // go the moment the window it points at moves or changes size.
            Resize += delegate { HideSuggestions(); };
            Move += delegate { HideSuggestions(); };

            OpenStartupTabs();
        }

        private static BookmarkManager _sharedBookmarks;
        private static HistoryManager _sharedHistory;

        private readonly List<string> _startupTabs;
        private readonly int _startupActive;

        /// <summary>True when this window is a private window.</summary>
        public bool IsPrivate { get { return _privateMode; } }

        /// <summary>
        /// The tabs a window opens with. A private window always starts empty;
        /// a normal one either shows the start page or the tabs handed to it by
        /// the session the app restored.
        /// </summary>
        private void OpenStartupTabs()
        {
            if (_startupTabs != null && _startupTabs.Count > 0)
            {
                foreach (string url in _startupTabs) AddNewTab(url);

                if (_startupActive >= 0 && _startupActive < _tabControl.TabPages.Count)
                {
                    _tabControl.SelectedIndex = _startupActive;
                }
                return;
            }

            AddNewTab();
        }

        private void InitializeWindow()
        {
            Text = _privateMode ? "Toge \u2014 Private" : "Toge";
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            // The process is DPI-aware, so all sizes below are physical pixels.
            // Design values are given at 96 DPI and scaled explicitly.
            AutoScaleMode = AutoScaleMode.None;
            Size = new Size(Dpi.Px(1280), Dpi.Px(820));
            MinimumSize = new Size(Dpi.Px(620), Dpi.Px(420));
            BackColor = Theme.WindowBackground;
            DoubleBuffered = true;
            SetStyle(ControlStyles.ResizeRedraw, true);

            // The bookmarks bar shows the saved pages, and follows every change.
            _bookmarkManager.Changed += OnBookmarksChanged;

            // Top chrome WebView2: tab strip + toolbar (+ the bookmarks bar),
            // rendered as one HTML surface.
            _chromeWebView = new WebView2();
            _chromeWebView.Dock = DockStyle.Top;
            _chromeWebView.Height = ChromePx();
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
                SyncPageFullScreen();
                if (!_privateMode) SessionStore.Touch();

                RestoreTabView();
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
            Settings.Changed += OnSettingsChanged;

            // Keep the window buttons in step with maximize/restore.
            Resize += delegate { SyncWindowState(); };
        }

        /// <summary>
        /// A setting changed on the settings page (or from the Appearance menu).
        /// Everything that reaches into a live WebView is re-applied here.
        /// </summary>
        private void OnSettingsChanged()
        {
            foreach (TabPage page in _tabControl.TabPages)
            {
                var tab = page as BrowserTab;
                if (tab == null) continue;

                tab.ApplyAppSettings();

                // The start page draws the search engine, clock, shortcuts and
                // wallpaper from the settings. It is refreshed in place rather
                // than reloaded, so the customize sheet stays open while the
                // user drags a slider or picks a swatch.
                if (tab.CurrentUrl == "about:home")
                {
                    tab.RefreshHome();
                }
                else if (tab.IsSettingsPage)
                {
                    tab.RefreshSettingsStats();
                }
            }

            SyncShields();
            SyncBookmarksBar();
        }

        private void ApplyTheme()
        {
            BackColor = Theme.WindowBackground;
            _chromeWebView.DefaultBackgroundColor = Theme.ChromeTop;
            _contentPanel.BackColor = Theme.ContentBackground;
            _tabControl.BackColor = Theme.ContentBackground;

            if (_chromeWebView.CoreWebView2 != null)
            {
                _chromeWebView.CoreWebView2.Profile.PreferredColorScheme = Theme.IsDark
                    ? CoreWebView2PreferredColorScheme.Dark
                    : CoreWebView2PreferredColorScheme.Light;

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
                await _chromeWebView.EnsureCoreWebView2Async(await BrowserEnvironment.GetAsync());

                _chromeWebView.CoreWebView2.Settings.IsStatusBarEnabled = false;
                _chromeWebView.CoreWebView2.Settings.AreDevToolsEnabled = false;
                _chromeWebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                // Makes -webkit-app-region: drag in the chrome actually move the
                // window, the way a title bar should.
                _chromeWebView.CoreWebView2.Settings.IsNonClientRegionSupportEnabled = true;
                _chromeWebView.CoreWebView2.Profile.PreferredColorScheme = Theme.IsDark
                    ? CoreWebView2PreferredColorScheme.Dark
                    : CoreWebView2PreferredColorScheme.Light;

                _chromeWebView.CoreWebView2.WebMessageReceived += OnChromeMessage;

                var chromeController = BrowserEnvironment.ControllerOf(_chromeWebView);
                if (chromeController != null)
                {
                    chromeController.AcceleratorKeyPressed += OnChromeAcceleratorPressed;
                }

                _chromeWebView.CoreWebView2.DOMContentLoaded += async delegate
                {
                    _chromeReady = true;
                    SyncAllToChrome();

                    // The chrome view shares the profile, so it can answer the
                    // cookie question without touching a page.
                    await GoogleAccount.RefreshAsync(_chromeWebView.CoreWebView2);
                };

                _chromeWebView.NavigateToString(ChromeHtml.GetHtml(Theme.IsDark));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error initializing Toge chrome: " + ex.Message, "Toge Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    case "new_window":
                        NewWindow(false);
                        break;
                    case "new_private_window":
                        NewWindow(true);
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
                    case "downloads":
                        ShowDownloads();
                        break;
                    case "page_settings":
                        ShowPageSettings(ExtractJsonInt(json, "x"), ExtractJsonInt(json, "y"));
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
                    case "settings":
                        OpenSettings();
                        break;
                    case "focus_url":
                        FocusUrlBar(false);
                        break;
                    case "url_input":
                        {
                            var fields = Json.AsObject(Json.Parse(json));
                            ShowSuggestions(
                                Json.GetString(fields, "text"),
                                Json.GetInt(fields, "x", -1),
                                Json.GetInt(fields, "y", -1),
                                Json.GetInt(fields, "w", 0),
                                Json.GetBool(fields, "empty", false));
                            break;
                        }
                    case "url_key":
                        MoveSuggestion(ExtractJsonField(json, "key"));
                        break;
                    case "blur_url":
                        HideSuggestions();
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
                    case "bm_open":
                        {
                            string barUrl = ExtractJsonField(json, "url");
                            if (string.IsNullOrEmpty(barUrl)) break;

                            if (ExtractJsonBool(json, "newTab") && !string.Equals(barUrl, ActiveTabUrl(), StringComparison.OrdinalIgnoreCase))
                            {
                                AddNewTab(barUrl);
                            }
                            else if (ActiveTab != null)
                            {
                                ActiveTab.Navigate(barUrl);
                                FocusContent();
                            }
                            break;
                        }
                    case "bm_folder":
                        ShowBookmarkFolderMenu(ExtractJsonField(json, "path"),
                            ExtractJsonInt(json, "x"), ExtractJsonInt(json, "y"));
                        break;
                    case "bm_folder_all":
                        OpenFolderInTabs(ExtractJsonField(json, "path"));
                        break;
                    case "bm_more":
                        ShowBookmarkOverflowMenu(ExtractJsonInt(json, "index"),
                            ExtractJsonInt(json, "x"), ExtractJsonInt(json, "y"));
                        break;
                    case "bm_menu":
                        ShowBookmarkBarItemMenu(json);
                        break;
                    case "bm_bar_menu":
                        ShowBookmarksBarMenu(ExtractJsonInt(json, "x"), ExtractJsonInt(json, "y"));
                        break;
                    case "tab_menu":
                        ShowTabMenu(ExtractJsonInt(json, "index"),
                            ExtractJsonInt(json, "x"), ExtractJsonInt(json, "y"));
                        break;
                    case "tab_mute":
                        ToggleTabMute(ExtractJsonInt(json, "index"));
                        break;
                    case "tab_move":
                        MoveTab(ExtractJsonInt(json, "from"), ExtractJsonInt(json, "to"));
                        break;
                    case "site_info":
                        ShowSiteInfo(ExtractJsonInt(json, "x"), ExtractJsonInt(json, "y"));
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
            AddNewTabAt(url, _tabControl.TabPages.Count);
        }

        /// <summary>
        /// Adds a tab at a given place in the strip. A duplicate lands next to
        /// the tab it came from rather than at the far end.
        /// </summary>
        public void AddNewTabAt(string url, int index)
        {
            var tab = new BrowserTab(url, _privateMode);
            tab.UrlChanged += OnTabUrlChanged;
            tab.TitleChanged += OnTabTitleChanged;
            tab.StateChanged += OnTabStateChanged;
            tab.IconChanged += OnTabIconChanged;
            tab.AudioChanged += OnTabAudioChanged;
            tab.CrashReported += OnTabCrashReported;
            tab.NotificationArrived += OnTabNotificationArrived;
            tab.SiteZoomChanged += OnTabSiteZoomChanged;
            tab.AuthenticationRequested += OnTabAuthenticationRequested;
            tab.NewWindowRequested += OnTabNewWindowRequested;
            tab.WindowCloseRequested += OnTabWindowCloseRequested;
            tab.SettingsMessage += OnTabSettingsMessage;
            tab.FullScreenChanged += OnTabFullScreenChanged;
            tab.DownloadStarting += OnTabDownloadStarting;
            tab.PermissionRequested += OnTabPermissionRequested;
            tab.ContextMenuRequested += OnTabContextMenuRequested;
            tab.AcceleratorPressed += OnTabAcceleratorPressed;
            tab.FocusChromeRequested += OnTabFocusChromeRequested;

            if (index < 0 || index > _tabControl.TabPages.Count) index = _tabControl.TabPages.Count;
            _tabControl.TabPages.Insert(index, tab);
            _tabControl.SelectedTab = tab;

            // A brand new tab hands the keyboard to the address bar, the way
            // every serious browser does: it is ready to type before anything
            // is typed, with its suggestions already underneath. A tab that
            // opens a page keeps the caret wherever it was.
            if (string.IsNullOrEmpty(url)) tab.RequestChromeFocus();

            if (!_privateMode) SessionStore.Touch();
            SyncAllToChrome();
        }

        public void CloseTabAt(int index)
        {
            if (_tabControl.TabPages.Count > 1 && index >= 0 && index < _tabControl.TabPages.Count)
            {
                var tab = _tabControl.TabPages[index] as BrowserTab;
                RememberClosed(tab, index);

                _tabControl.TabPages.RemoveAt(index);
                tab.Dispose();
                _tabControl.SelectedIndex = Math.Max(0, index - 1);
            }
            else if (_tabControl.TabPages.Count == 1 && ActiveTab != null)
            {
                ActiveTab.LoadHomePage();
            }
            if (!_privateMode) SessionStore.Touch();
            SyncAllToChrome();
            SyncPageFullScreen();
        }

        private BrowserTab TabAt(int index)
        {
            if (index < 0 || index >= _tabControl.TabPages.Count) return null;
            return _tabControl.TabPages[index] as BrowserTab;
        }

        /// <summary>
        /// The menu a right-click on a tab raises. Everything it offers is
        /// about that one tab, so a stray click can never close the wrong page.
        /// </summary>
        private void ShowTabMenu(int index, int cssX, int cssY)
        {
            var tab = TabAt(index);
            if (tab == null) return;

            var menu = NewMenu();

            string url = tab.CurrentUrl;
            bool hasPage = !string.IsNullOrEmpty(url) && url != "about:home";

            menu.Items.Add(MenuAction("New Tab to the Right", delegate { AddNewTabAt(null, index + 1); }));

            var duplicate = MenuAction("Duplicate", delegate { AddNewTabAt(hasPage ? url : null, index + 1); });
            duplicate.Enabled = hasPage;
            menu.Items.Add(duplicate);

            menu.Items.Add(MenuAction(tab.IsMuted ? "Unmute Tab" : "Mute Tab", delegate { ToggleTabMute(index); }));

            var copyLink = MenuAction("Copy Page Address", delegate { CopyText(url); });
            copyLink.Enabled = hasPage;
            menu.Items.Add(copyLink);

            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(MenuAction("Reload", delegate { tab.Reload(); }));

            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(MenuAction("Close Tab", delegate { CloseTabAt(index); }));

            var closeOthers = MenuAction("Close Other Tabs", delegate { CloseOtherTabs(index); });
            closeOthers.Enabled = _tabControl.TabPages.Count > 1;
            menu.Items.Add(closeOthers);

            var closeRight = MenuAction("Close Tabs to the Right", delegate { CloseTabsToRight(index); });
            closeRight.Enabled = index < _tabControl.TabPages.Count - 1;
            menu.Items.Add(closeRight);

            ShowChromeMenu(menu, cssX, cssY);
        }

        private void ToggleTabMute(int index)
        {
            var tab = TabAt(index);
            if (tab == null) return;

            tab.ToggleMute();
            SyncTabs();
        }

        /// <summary>
        /// Moves a tab to another place in the strip. The index arrives counted
        /// in the old order, so a move to the right lands one place earlier.
        /// </summary>
        private void MoveTab(int from, int to)
        {
            if (from < 0 || from >= _tabControl.TabPages.Count) return;

            int target = to > from ? to - 1 : to;
            if (target < 0) target = 0;
            if (target > _tabControl.TabPages.Count - 1) target = _tabControl.TabPages.Count - 1;
            if (target == from) return;

            var page = _tabControl.TabPages[from];
            _tabControl.TabPages.RemoveAt(from);
            _tabControl.TabPages.Insert(target, page);
            _tabControl.SelectedTab = page;

            if (!_privateMode) SessionStore.Touch();
            SyncTabs();
        }

        private void CloseOtherTabs(int keep)
        {
            for (int i = _tabControl.TabPages.Count - 1; i >= 0; i--)
            {
                if (i != keep) CloseTabAt(i);
            }
        }

        private void CloseTabsToRight(int index)
        {
            for (int i = _tabControl.TabPages.Count - 1; i > index; i--) CloseTabAt(i);
        }

        private void OnTabAudioChanged(BrowserTab tab)
        {
            SyncTabs();
        }

        /// <summary>
        /// The engine zoomed a page by itself (Ctrl+wheel). Remember it for the
        /// site, so the next visit and the next run open at the same size.
        /// </summary>
        private void OnTabSiteZoomChanged(BrowserTab tab, int percent)
        {
            if (tab == null) return;

            string url = tab.CurrentUrl;
            if (SiteSettings.HostOf(url).Length == 0) return;

            SiteSettings.Current.SetZoom(url, percent);
        }

        private void OnTabCrashReported(BrowserTab tab)
        {
            tab.ShowCrashCover();
        }

        private void OnTabNotificationArrived(BrowserTab tab, CoreWebView2NotificationReceivedEventArgs e)
        {
            // Toge draws the toast, so the engine must not draw one as well.
            e.Handled = true;

            var note = e.Notification;
            string origin = e.SenderOrigin;
            if (string.IsNullOrEmpty(origin)) origin = tab.CurrentUrl;

            string title = note != null ? note.Title : "";
            string body = note != null ? note.Body : "";

            if (note != null) note.ReportShown();

            var toast = new NotificationToast(origin, title, body,
                delegate
                {
                    if (note != null) note.ReportClicked();
                    ShowTab(tab);
                },
                delegate
                {
                    if (note != null) note.ReportClosed();
                });

            toast.Pop();
        }

        /// <summary>Brings a window forward and shows the tab that rang.</summary>
        private void ShowTab(BrowserTab tab)
        {
            if (tab == null || tab.IsDisposed) return;

            if (WindowState == FormWindowState.Minimized) WindowState = FormWindowState.Normal;
            Activate();

            int index = _tabControl.TabPages.IndexOf(tab);
            if (index >= 0) _tabControl.SelectedIndex = index;
        }

        /// <summary>
        /// A site behind a login asked for a name and password. The dialog is
        /// Toge's own, and nothing it collects is written anywhere.
        /// </summary>
        private void OnTabAuthenticationRequested(BrowserTab tab, CoreWebView2BasicAuthenticationRequestedEventArgs e)
        {
            var deferral = e.GetDeferral();
            try
            {
                string host = SiteSettings.HostOf(e.Uri);
                if (string.IsNullOrEmpty(host)) host = e.Uri;

                using (var prompt = new LoginForm(host, e.Challenge))
                {
                    if (prompt.ShowDialog(this) == DialogResult.OK)
                    {
                        e.Response.UserName = prompt.UserName;
                        e.Response.Password = prompt.Password;
                    }
                    else
                    {
                        e.Cancel = true;
                    }
                }
            }
            catch
            {
                e.Cancel = true;
            }
            finally
            {
                deferral.Complete();
            }
        }

        /// <summary>A tab that was closed, kept so Ctrl+Shift+T can bring it back.</summary>
        private class ClosedTab
        {
            public string Url;
            public string Title;
            public int Index;
        }

        private readonly List<ClosedTab> _closedTabs = new List<ClosedTab>();

        /// <summary>Remembers a closed tab, unless it was only a start page.</summary>
        private void RememberClosed(BrowserTab tab, int index)
        {
            if (tab == null) return;

            string url = tab.CurrentUrl;
            if (string.IsNullOrEmpty(url) || url == "about:home" || url == SettingsPage.Address) return;

            var record = new ClosedTab();
            record.Url = url;
            record.Title = tab.DisplayTitle;
            record.Index = index;

            _closedTabs.Add(record);
            if (_closedTabs.Count > 15) _closedTabs.RemoveAt(0);
        }

        /// <summary>Brings back the most recently closed tab, in its old place.</summary>
        private void ReopenClosedTab()
        {
            int last = _closedTabs.Count - 1;
            if (last < 0)
            {
                // Nothing left in this window; the last closed window may still
                // be waiting, the way Ctrl+Shift+T behaves in other browsers.
                ReopenClosedWindow();
                return;
            }

            var record = _closedTabs[last];
            _closedTabs.RemoveAt(last);

            AddNewTab(record.Url);

            // Put it back where it was, the way a browser does.
            var tab = ActiveTab;
            if (tab == null) return;

            int current = _tabControl.TabPages.IndexOf(tab);
            int target = Math.Max(0, Math.Min(record.Index, _tabControl.TabPages.Count - 1));
            if (current >= 0 && current != target)
            {
                _tabControl.TabPages.RemoveAt(current);
                _tabControl.TabPages.Insert(target, tab);
                _tabControl.SelectedTab = tab;
            }
        }

        /// <summary>
        /// Remembers the pages a window was showing when the user closed it, so
        /// the whole window can come back later. Private windows are not kept.
        /// </summary>
        private void RememberClosedWindow()
        {
            var urls = new List<string>();

            for (int i = 0; i < _tabControl.TabPages.Count; i++)
            {
                var tab = _tabControl.TabPages[i] as BrowserTab;
                if (tab == null) continue;

                string url = tab.CurrentUrl;
                if (string.IsNullOrEmpty(url)) continue;
                if (url == "about:home" || url == SettingsPage.Address) continue;

                urls.Add(url);
            }

            if (urls.Count == 0) return;

            ClosedWindows.Add(urls);
            if (ClosedWindows.Count > 5) ClosedWindows.RemoveAt(0);
        }

        /// <summary>
        /// Brings back the last closed window with the pages it was showing.
        /// Returns false when there is nothing left to reopen.
        /// </summary>
        internal static bool ReopenClosedWindow()
        {
            if (ClosedWindows.Count == 0) return false;

            int last = ClosedWindows.Count - 1;
            var urls = ClosedWindows[last];
            ClosedWindows.RemoveAt(last);

            var window = new MainForm(false, urls, 0);
            window.Show();
            window.Activate();
            return true;
        }

        /// <summary>The tabs this window is showing, for the saved session.</summary>
        public WindowSession CaptureTabs()
        {
            var session = new WindowSession();

            for (int i = 0; i < _tabControl.TabPages.Count; i++)
            {
                var tab = _tabControl.TabPages[i] as BrowserTab;
                if (tab == null) continue;

                string url = tab.CurrentUrl;
                if (string.IsNullOrEmpty(url)) url = "about:home";
                // A paused download or a settings page is not worth reopening.
                if (url == SettingsPage.Address) continue;

                if (session.Active < 0 && i == _tabControl.SelectedIndex) session.Active = session.Tabs.Count;
                session.Tabs.Add(url);
            }

            if (session.Active < 0) session.Active = 0;
            return session;
        }

        private void ToggleBookmarkCurrent()
        {
            if (ActiveTab == null) return;
            string url = ActiveTab.CurrentUrl;

            if (string.IsNullOrEmpty(url) || url == "about:home" || url == SettingsPage.Address) return;

            // Already saved: the star takes it away again, as before.
            if (_bookmarkManager.IsBookmarked(url))
            {
                _bookmarkManager.RemoveBookmark(url);
                SyncBookmarked();
                return;
            }

            AddBookmarkDialog();
        }

        /// <summary>
        /// The "add bookmark" sheet for the page in the active tab, so a new
        /// bookmark can be named and filed instead of always landing at the top.
        /// </summary>
        private void AddBookmarkDialog()
        {
            var tab = ActiveTab;
            if (tab == null) return;

            string url = tab.CurrentUrl;
            if (string.IsNullOrEmpty(url) || url == "about:home" || url == SettingsPage.Address) return;

            string title = tab.Text;
            if (string.IsNullOrEmpty(title)) title = url;

            var existing = _bookmarkManager.Find(url);
            string folder = existing != null ? existing.Folder : string.Empty;
            if (existing != null) title = existing.Title;

            using (var form = new AddBookmarkForm(_bookmarkManager, title, url, folder))
            {
                if (form.ShowDialog(this) != DialogResult.OK) return;

                _bookmarkManager.SetBookmark(form.BookmarkTitle, url, form.Folder, tab.FaviconDataUrl);
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
            SyncBookmarksBar();
            SyncWindowState();
            SyncPrivate();
            SyncDownloads();
        }

        /// <summary>Tells the chrome whether this window is a private one.</summary>
        private void SyncPrivate()
        {
            if (!_chromeReady || _chromeWebView.CoreWebView2 == null) return;

            _chromeWebView.CoreWebView2.ExecuteScriptAsync(
                string.Format("window.BraveAPI && window.BraveAPI.setPrivate({0});",
                    _privateMode ? "true" : "false"));
        }

        /// <summary>
        /// Tells the chrome whether the window is maximized so the middle button
        /// can show "maximize" or "restore", the way the Windows buttons do.
        /// </summary>
        private void SyncWindowState()
        {
            if (!_chromeReady || _chromeWebView.CoreWebView2 == null) return;

            // Resize fires continuously while dragging; only talk to the chrome
            // when the state really changed.
            bool maximized = WindowState == FormWindowState.Maximized;
            if (_windowStateKnown && maximized == _windowMaximized) return;
            _windowStateKnown = true;
            _windowMaximized = maximized;

            _chromeWebView.CoreWebView2.ExecuteScriptAsync(
                string.Format("window.BraveAPI && window.BraveAPI.setMaximized({0});",
                    maximized ? "true" : "false"));
        }

        /// <summary>
        /// Height of the whole chrome, in physical pixels. The bookmarks bar only
        /// takes room when it is switched on and has something to show.
        /// </summary>
        private int ChromePx()
        {
            int design = ChromeHtml.ChromeHeight;
            if (BookmarksBarVisible()) design += ChromeHtml.BookmarksBarHeight;
            return Dpi.Px(design);
        }

        /// <summary>
        /// True when the bar should be on screen: enabled in the settings, and
        /// holding at least one bookmark or folder.
        /// </summary>
        private bool BookmarksBarVisible()
        {
            if (!Settings.Current.ShowBookmarksBar) return false;
            if (_bookmarkManager.GetRootBookmarks().Count > 0) return true;
            return _bookmarkManager.GetChildFolders(null).Count > 0;
        }

        /// <summary>Re-measures the chrome and redraws the bookmarks bar.</summary>
        private void SyncBookmarksBar()
        {
            if (_chromeWebView == null) return;

            bool show = BookmarksBarVisible();

            int height = ChromePx();
            if (_chromeWebView.Height != height) _chromeWebView.Height = height;

            if (!_chromeReady || _chromeWebView.CoreWebView2 == null) return;

            _chromeWebView.CoreWebView2.ExecuteScriptAsync(
                string.Format("window.BraveAPI && window.BraveAPI.renderBookmarks({0}, {1});",
                    show ? "true" : "false", BuildBarJson()));
        }

        /// <summary>
        /// The bar's contents: top-level bookmarks in order, with the top-level
        /// folders first, the way every browser shows them.
        /// </summary>
        private string BuildBarJson()
        {
            var items = new List<string>();

            foreach (var entry in BarEntries())
            {
                if (entry.IsFolder)
                {
                    items.Add(string.Format("{{\"kind\":\"folder\",\"title\":\"{0}\",\"path\":\"{1}\"}}",
                        EscapeJs(entry.Title), EscapeJs(entry.Path)));
                }
                else
                {
                    items.Add(string.Format(
                        "{{\"kind\":\"bookmark\",\"title\":\"{0}\",\"url\":\"{1}\",\"icon\":\"{2}\"}}",
                        EscapeJs(entry.Title), EscapeJs(entry.Url), EscapeJs(entry.Icon)));
                }
            }

            return "[" + string.Join(",", items.ToArray()) + "]";
        }

        private void OnBookmarksChanged()
        {
            SyncBookmarksBar();
            SyncBookmarked();
        }

        /// <summary>One row of the bookmarks bar, mirroring what the chrome draws.</summary>
        private class BarEntry
        {
            public bool IsFolder;
            public string Title;
            public string Url;
            public string Path;
            public string Icon;
        }

        /// <summary>
        /// The bar's contents, in the order it shows them: top-level folders
        /// first, then the top-level bookmarks.
        /// </summary>
        private List<BarEntry> BarEntries()
        {
            var entries = new List<BarEntry>();

            foreach (string folder in _bookmarkManager.GetChildFolders(null))
            {
                var folderEntry = new BarEntry();
                folderEntry.IsFolder = true;
                folderEntry.Title = BookmarkManager.NameOf(folder);
                folderEntry.Path = folder;
                entries.Add(folderEntry);
            }

            foreach (var bookmark in _bookmarkManager.GetRootBookmarks())
            {
                var entry = new BarEntry();
                entry.Title = bookmark.Title;
                entry.Url = bookmark.Url;

                // A bookmark added from a tab keeps the icon that page handed us.
                // Imported ones have none, so ask the favicon service for the
                // site's real icon instead of drawing a letter tile.
                entry.Icon = string.IsNullOrEmpty(bookmark.Icon)
                    ? FavoritesStore.ServiceIconUrl(bookmark.Url)
                    : bookmark.Icon;

                entries.Add(entry);
            }

            return entries;
        }

        private string ActiveTabUrl()
        {
            return ActiveTab != null ? (ActiveTab.CurrentUrl ?? string.Empty) : string.Empty;
        }

        /// <summary>
        /// A menu for the bookmarks bar, tracked in the same way as the ⋯ menu so
        /// that a press outside it closes it. The menu is left to the garbage
        /// collector rather than disposed: the framework still touches a
        /// ToolStripDropDown while it is closing, and disposing it there throws.
        /// </summary>
        private ContextMenuStrip NewMenu()
        {
            if (_openMenu != null) CloseOpenMenu();

            var menu = new ContextMenuStrip();
            menu.BackColor = Theme.CardBackground;
            menu.ForeColor = Theme.TextPrimary;
            menu.Font = new Font("Segoe UI", 9.5F);
            menu.ShowImageMargin = false;

            menu.Closed += delegate
            {
                if (ReferenceEquals(_openMenu, menu)) _openMenu = null;
                RemoveMenuMouseHook();
            };

            _openMenu = menu;
            InstallMenuMouseHook();
            return menu;
        }

        /// <summary>Shows a menu under a point the chrome reported, in CSS pixels.</summary>
        private void ShowChromeMenu(ContextMenuStrip menu, int cssX, int cssY)
        {
            if (_chromeWebView == null) return;

            int x = Math.Max(0, Math.Min(cssX, Width));
            Point at = _chromeWebView.PointToScreen(new Point(Dpi.Px(x), Dpi.Px(Math.Max(0, cssY))));
            menu.Show(at);
        }

        private void OpenBookmark(string url, bool newTab)
        {
            if (string.IsNullOrEmpty(url)) return;

            if (newTab)
            {
                AddNewTab(url);
                return;
            }

            if (ActiveTab != null)
            {
                ActiveTab.Navigate(url);
                FocusContent();
            }
        }

        /// <summary>A menu label that is never empty and never too long.</summary>
        private static string MenuTitle(string title, string url)
        {
            string text = string.IsNullOrEmpty(title) ? url : title;
            if (string.IsNullOrEmpty(text)) text = "Untitled";
            if (text.Length > 64) text = text.Substring(0, 61) + "\u2026";
            return text;
        }

        /// <summary>Fills a menu with a folder's subfolders and bookmarks, to any depth.</summary>
        private void FillFolderItems(ToolStripItemCollection items, string folder)
        {
            foreach (string child in _bookmarkManager.GetChildFolders(folder))
            {
                var folderItem = new ToolStripMenuItem(BookmarkManager.NameOf(child));
                FillFolderItems(folderItem.DropDownItems, child);
                items.Add(folderItem);
            }

            var found = _bookmarkManager.GetBookmarks(folder, false);
            if (found.Count > 0 && items.Count > 0) items.Add(new ToolStripSeparator());

            foreach (var bookmark in found)
            {
                string url = bookmark.Url;
                var item = new ToolStripMenuItem(MenuTitle(bookmark.Title, url));
                item.ToolTipText = url;
                item.Click += delegate { OpenBookmark(url, false); };
                items.Add(item);
            }

            if (items.Count == 0)
            {
                var empty = new ToolStripMenuItem("No bookmarks here");
                empty.Enabled = false;
                items.Add(empty);
            }
        }

        /// <summary>The dropdown under a folder on the bookmarks bar.</summary>
        private void ShowBookmarkFolderMenu(string folder, int cssX, int cssY)
        {
            var menu = NewMenu();
            FillFolderItems(menu.Items, BookmarkManager.Normalize(folder));
            ShowChromeMenu(menu, cssX, cssY);
        }

        /// <summary>Middle-clicking a folder opens everything inside it.</summary>
        private void OpenFolderInTabs(string folder)
        {
            var found = _bookmarkManager.GetBookmarks(BookmarkManager.Normalize(folder), false);
            if (found.Count == 0) return;

            bool first = true;
            foreach (var bookmark in found)
            {
                if (first)
                {
                    OpenBookmark(bookmark.Url, false);
                    first = false;
                }
                else
                {
                    AddNewTab(bookmark.Url);
                }
            }
        }

        /// <summary>The items that did not fit on the bar, behind the chevron.</summary>
        private void ShowBookmarkOverflowMenu(int index, int cssX, int cssY)
        {
            var entries = BarEntries();
            if (index < 0 || index >= entries.Count) return;

            var menu = NewMenu();
            for (int i = index; i < entries.Count; i++)
            {
                var entry = entries[i];

                if (entry.IsFolder)
                {
                    var folderItem = new ToolStripMenuItem(entry.Title);
                    FillFolderItems(folderItem.DropDownItems, entry.Path);
                    menu.Items.Add(folderItem);
                }
                else
                {
                    string url = entry.Url;
                    var item = new ToolStripMenuItem(MenuTitle(entry.Title, url));
                    item.ToolTipText = url;
                    item.Click += delegate { OpenBookmark(url, false); };
                    menu.Items.Add(item);
                }
            }

            ShowChromeMenu(menu, cssX, cssY);
        }

        /// <summary>Right-clicking an item on the bar.</summary>
        private void ShowBookmarkBarItemMenu(string json)
        {
            string kind = ExtractJsonField(json, "kind");
            int cssX = ExtractJsonInt(json, "x");
            int cssY = ExtractJsonInt(json, "y");

            if (kind == "folder")
            {
                ShowBookmarkFolderContextMenu(ExtractJsonField(json, "path"), cssX, cssY);
                return;
            }

            string url = ExtractJsonField(json, "url");
            if (string.IsNullOrEmpty(url)) return;

            var bookmark = _bookmarkManager.Find(url);
            string title = bookmark != null ? bookmark.Title : ExtractJsonField(json, "title");

            var menu = NewMenu();

            var open = new ToolStripMenuItem("Open in This Tab", null, delegate { OpenBookmark(url, false); });
            menu.Items.Add(open);
            menu.Items.Add(new ToolStripMenuItem("Open in New Tab", null, delegate { OpenBookmark(url, true); }));
            menu.Items.Add(new ToolStripSeparator());

            var moveTo = new ToolStripMenuItem("Move to Folder");
            FillMoveTargets(moveTo.DropDownItems, null, delegate (string destination)
            {
                _bookmarkManager.MoveBookmark(url, destination);
            });
            menu.Items.Add(moveTo);

            menu.Items.Add(new ToolStripMenuItem("Rename\u2026", null, delegate { RenameBookmark(url, title); }));

            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(new ToolStripMenuItem("Copy Address", null, delegate { CopyToClipboard(url); }));

            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(new ToolStripMenuItem("Delete Bookmark", null, delegate
            {
                _bookmarkManager.RemoveBookmark(url);
            }));

            ShowChromeMenu(menu, cssX, cssY);
        }

        /// <summary>Right-clicking a folder on the bar.</summary>
        private void ShowBookmarkFolderContextMenu(string folder, int cssX, int cssY)
        {
            string path = BookmarkManager.Normalize(folder);
            var menu = NewMenu();

            menu.Items.Add(new ToolStripMenuItem("Open in New Tabs", null, delegate { OpenFolderInTabs(path); }));
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(new ToolStripMenuItem("New Folder Inside\u2026", null, delegate
            {
                CreateFolderIn(path);
            }));
            menu.Items.Add(new ToolStripMenuItem("Rename\u2026", null, delegate
            {
                using (var prompt = new TextPromptForm("Rename Folder", "Name:", BookmarkManager.NameOf(path), "Rename"))
                {
                    if (prompt.ShowDialog(this) != DialogResult.OK) return;
                    _bookmarkManager.RenameFolder(path, prompt.Value);
                }
            }));

            if (path.Length > 0)
            {
                var moveTo = new ToolStripMenuItem("Move to Folder");
                FillMoveTargets(moveTo.DropDownItems, path, delegate (string destination)
                {
                    _bookmarkManager.MoveFolder(path, destination);
                });
                menu.Items.Add(moveTo);
            }

            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(new ToolStripMenuItem("Delete Folder", null, delegate { DeleteFolderWithConfirm(path); }));

            ShowChromeMenu(menu, cssX, cssY);
        }

        /// <summary>Right-clicking the empty part of the bar.</summary>
        private void ShowBookmarksBarMenu(int cssX, int cssY)
        {
            var menu = NewMenu();

            // No check margin is drawn, so the label carries the state.
            var show = new ToolStripMenuItem(
                Settings.Current.ShowBookmarksBar ? "Hide Bookmarks Bar" : "Show Bookmarks Bar", null,
                delegate { Settings.Current.Set("bookmarksBar", !Settings.Current.ShowBookmarksBar); });
            menu.Items.Add(show);

            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(new ToolStripMenuItem("Bookmark This Page\u2026", null, delegate { AddBookmarkDialog(); }));
            menu.Items.Add(new ToolStripMenuItem("Manage Bookmarks\u2026", null, delegate { ShowBookmarks(); }));

            ShowChromeMenu(menu, cssX, cssY);
        }

        private void RenameBookmark(string url, string currentTitle)
        {
            using (var prompt = new TextPromptForm("Rename Bookmark", "Name:", currentTitle, "Rename"))
            {
                if (prompt.ShowDialog(this) != DialogResult.OK) return;
                _bookmarkManager.RenameBookmark(url, prompt.Value);
            }
        }

        private void CreateFolderIn(string parent)
        {
            string suggested = _bookmarkManager.UniqueChildName(parent, "New Folder");
            using (var prompt = new TextPromptForm("New Folder", "Name for the new folder:", suggested, "Create"))
            {
                if (prompt.ShowDialog(this) != DialogResult.OK) return;
                _bookmarkManager.CreateFolder(parent, _bookmarkManager.UniqueChildName(parent, prompt.Value));
            }
        }

        private void DeleteFolderWithConfirm(string folder)
        {
            string path = BookmarkManager.Normalize(folder);
            int count = _bookmarkManager.CountBookmarks(path, true);

            string name = path.Length == 0 ? BookmarkManager.RootFolder : BookmarkManager.NameOf(path);
            string question = count == 0
                ? "Delete the folder \"" + name + "\"?"
                : "Delete the folder \"" + name + "\" and the " + count +
                  (count == 1 ? " bookmark inside it?" : " bookmarks inside it?");

            if (MessageBox.Show(this, question, "Delete Folder",
                    MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) != DialogResult.OK)
            {
                return;
            }

            _bookmarkManager.DeleteFolder(path);
        }

        /// <summary>
        /// Fills a "move to" submenu with every folder that could hold the item,
        /// plus a way to make a new one on the spot.
        /// </summary>
        private void FillMoveTargets(ToolStripItemCollection items, string excludeFolder, Action<string> move)
        {
            var root = new ToolStripMenuItem(BookmarkManager.RootFolder);
            root.Click += delegate { move(string.Empty); };
            items.Add(root);

            foreach (string folder in _bookmarkManager.GetFolders())
            {
                if (excludeFolder != null && BookmarkManager.IsSameOrInside(folder, excludeFolder)) continue;

                var choice = new ToolStripMenuItem(Indent(folder));
                string destination = folder;
                choice.Click += delegate { move(destination); };
                items.Add(choice);
            }

            items.Add(new ToolStripSeparator());

            var create = new ToolStripMenuItem("New Folder\u2026");
            create.Click += delegate
            {
                string suggested = _bookmarkManager.UniqueChildName(string.Empty, "New Folder");
                using (var prompt = new TextPromptForm("New Folder", "Name for the new folder:", suggested, "Create"))
                {
                    if (prompt.ShowDialog(this) != DialogResult.OK) return;

                    string created = _bookmarkManager.CreateFolder(string.Empty,
                        _bookmarkManager.UniqueChildName(string.Empty, prompt.Value));
                    if (string.IsNullOrEmpty(created)) return;
                    move(created);
                }
            };
            items.Add(create);
        }

        private static string Indent(string folder)
        {
            int depth = 0;
            foreach (char c in folder)
            {
                if (c == '/') depth++;
            }
            return new string('\u00a0', depth * 3) + BookmarkManager.NameOf(folder);
        }

        private void CopyToClipboard(string text)
        {
            try
            {
                Clipboard.SetText(text ?? string.Empty);
            }
            catch
            {
                // The clipboard is occasionally locked by another process.
            }
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
                string icon = "";
                var browserTab = _tabControl.TabPages[i] as BrowserTab;
                if (browserTab != null)
                {
                    if (!string.IsNullOrEmpty(browserTab.DisplayTitle)) title = browserTab.DisplayTitle;
                    url = browserTab.CurrentUrl ?? "";
                    icon = browserTab.FaviconDataUrl ?? "";
                    if (url == "about:home") url = "";
                }

                tabItems.Add(string.Format("{{\"title\":\"{0}\",\"url\":\"{1}\",\"icon\":\"{2}\",\"audio\":{3},\"muted\":{4}}}",
                    EscapeJs(title), EscapeJs(url), EscapeJs(icon),
                    browserTab != null && browserTab.IsPlayingAudio ? "true" : "false",
                    browserTab != null && browserTab.IsMuted ? "true" : "false"));
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

            // The address a tab is showing is part of the saved session.
            if (!_privateMode) SessionStore.Touch();
        }

        private void OnTabTitleChanged(BrowserTab tab, string title)
        {
            SyncTabs();

            // Private windows leave no trace in the history.
            if (_privateMode || tab.IsPrivate) return;

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

        private void OnTabIconChanged(BrowserTab tab)
        {
            SyncTabs();
        }

        /// <summary>
        /// A fresh tab finished loading, so the address bar can finally take
        /// the keyboard for it. Only the tab in front may ask: one still
        /// loading in the background must never steal the caret.
        /// </summary>
        private void OnTabFocusChromeRequested(BrowserTab tab)
        {
            if (tab != null && ReferenceEquals(tab, ActiveTab)) FocusUrlBar(true);
        }

        /// <summary>
        /// Popups and new windows. Requests with explicit window features — the
        /// kind OAuth and "Sign in with Google" flows use — become real popup
        /// windows so they can post back to their opener. Plain target="_blank"
        /// links open as a new tab, like a normal browser.
        /// </summary>
        private async void OnTabNewWindowRequested(object sender, CoreWebView2NewWindowRequestedEventArgs e)
        {
            var deferral = e.GetDeferral();
            try
            {
                var features = e.WindowFeatures;
                bool wantsPopup = (features != null && (features.HasSize || features.HasPosition)) || !e.IsUserInitiated;

                if (wantsPopup)
                {
                    int width = features != null && features.HasSize && features.Width > 0
                        ? Dpi.Px((int)features.Width)
                        : Dpi.Px(520);
                    int height = features != null && features.HasSize && features.Height > 0
                        ? Dpi.Px((int)features.Height)
                        : Dpi.Px(660);

                    var popup = new PopupWindow();
                    popup.Size = new Size(Math.Max(Dpi.Px(320), width), Math.Max(Dpi.Px(280), height));
                    popup.Owner = this;
                    popup.Show();

                    await BrowserEnvironment.InitializeAsync(popup.View, _privateMode);
                    popup.Attach();

                    e.NewWindow = popup.View.CoreWebView2;
                    e.Handled = true;

                    // WebView2 places the window itself, so recentre once it is done.
                    var placed = popup;
                    popup.BeginInvoke((MethodInvoker)delegate { placed.CenterOnOwner(); });
                }
                else
                {
                    e.Handled = true;
                    AddNewTab(string.IsNullOrEmpty(e.Uri) ? null : e.Uri);
                }
            }
            catch
            {
                // Never leave the click dead: fall back to a plain new tab.
                try
                {
                    e.Handled = true;
                    AddNewTab(string.IsNullOrEmpty(e.Uri) ? null : e.Uri);
                }
                catch { }
            }
            finally
            {
                deferral.Complete();
            }
        }

        private void OnTabWindowCloseRequested(BrowserTab tab)
        {
            int index = _tabControl.TabPages.IndexOf(tab);
            if (index >= 0) CloseTabAt(index);
        }

        private void OnTabFullScreenChanged(BrowserTab tab, bool fullScreen)
        {
            SyncPageFullScreen();
        }

        /// <summary>
        /// Matches the window to the fullscreen state of the active tab. A page
        /// that asks for its own fullscreen gets the whole window and the working
        /// area — never the whole monitor, so the taskbar stays visible.
        /// </summary>
        private void SyncPageFullScreen()
        {
            var tab = ActiveTab;
            bool fullScreen = tab != null && tab.IsFullScreen;

            if (fullScreen == _windowIsFullScreen) return;
            _windowIsFullScreen = fullScreen;

            if (fullScreen)
            {
                _windowStateBeforeFullScreen = WindowState;
                WindowState = FormWindowState.Maximized;
                _chromeWebView.Visible = false;
            }
            else
            {
                _chromeWebView.Visible = true;
                WindowState = _windowStateBeforeFullScreen;
            }
        }

        private void ShowBraveShields()
        {
            string url = ActiveTab != null ? ActiveTab.CurrentUrl : "";
            var shield = new BraveShieldsForm(url);
            shield.Location = new Point(Math.Max(Dpi.Px(10), Right - Dpi.Px(356)), Top + ChromePx() + Dpi.Px(6));
            shield.Show(this);
        }

        /// <summary>
        /// The AA popover: page settings for the site on screen. The chrome says
        /// where the AA button is, in page pixels, so the sheet can hang under it.
        /// </summary>
        private void ShowPageSettings(int cssX, int cssY)
        {
            if (_chromeWebView == null || !_chromeReady) return;

            var tab = ActiveTab;
            if (tab == null) return;

            string url = tab.CurrentUrl;
            if (string.IsNullOrEmpty(url)) return;
            if (url.StartsWith("about:", StringComparison.OrdinalIgnoreCase)) return;
            if (url.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) return;

            var popover = new PageSettingsForm(url, SiteSettings.HostOf(url));

            popover.ZoomChanged += delegate
            {
                SiteSettings.Current.SetZoom(url, popover.Zoom);
                if (ActiveTab != null) ActiveTab.ApplySiteZoom();
            };

            popover.ReaderRequested += delegate
            {
                if (ActiveTab != null) ActiveTab.ToggleReader();
            };

            Point at = _chromeWebView.PointToScreen(
                new Point(Dpi.Px(Math.Max(0, cssX)), Dpi.Px(Math.Max(0, cssY))));

            Rectangle work = Screen.FromControl(this).WorkingArea;
            int x = Math.Min(Math.Max(work.Left + Dpi.Px(6), at.X), work.Right - popover.Width - Dpi.Px(6));
            int y = Math.Min(Math.Max(work.Top + Dpi.Px(6), at.Y + Dpi.Px(3)), work.Bottom - popover.Height - Dpi.Px(6));
            popover.Location = new Point(x, y);

            popover.Show(this);
        }

        /// <summary>
        /// The card behind the lock: who the page is, how the connection stands,
        /// and the one piece of site data Toge can clear without wiping the
        /// whole profile.
        /// </summary>
        private void ShowSiteInfo(int cssX, int cssY)
        {
            if (_chromeWebView == null || !_chromeReady) return;

            var tab = ActiveTab;
            if (tab == null) return;

            string url = tab.CurrentUrl;
            if (string.IsNullOrEmpty(url)) return;
            if (url.StartsWith("about:", StringComparison.OrdinalIgnoreCase)) return;
            if (url.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) return;

            bool secure = url.StartsWith("https:", StringComparison.OrdinalIgnoreCase);
            var popover = new SiteInfoForm(SiteSettings.HostOf(url), secure);

            popover.ClearCookiesRequested += delegate { ClearSiteCookies(popover, tab, url); };
            popover.SettingsRequested += delegate { OpenSettings(); };

            Point at = _chromeWebView.PointToScreen(
                new Point(Dpi.Px(Math.Max(0, cssX)), Dpi.Px(Math.Max(0, cssY))));

            Rectangle work = Screen.FromControl(this).WorkingArea;
            int x = Math.Min(Math.Max(work.Left + Dpi.Px(6), at.X), work.Right - popover.Width - Dpi.Px(6));
            int y = Math.Min(Math.Max(work.Top + Dpi.Px(6), at.Y + Dpi.Px(3)), work.Bottom - popover.Height - Dpi.Px(6));
            popover.Location = new Point(x, y);

            popover.Show(this);

            LoadSiteCookies(popover, tab, url);
        }

        private async void LoadSiteCookies(SiteInfoForm popover, BrowserTab tab, string url)
        {
            try
            {
                var cookies = await tab.WebView.CoreWebView2.CookieManager.GetCookiesAsync(url);
                if (popover.IsDisposed) return;
                popover.SetCookieCount(cookies.Count);
            }
            catch
            {
                if (!popover.IsDisposed) popover.SetCookieCount(0);
            }
        }

        private async void ClearSiteCookies(SiteInfoForm popover, BrowserTab tab, string url)
        {
            try
            {
                var manager = tab.WebView.CoreWebView2.CookieManager;
                var cookies = await manager.GetCookiesAsync(url);

                foreach (var cookie in cookies)
                {
                    manager.DeleteCookie(cookie);
                }

                if (!popover.IsDisposed) popover.SetCookieCount(0);
                ChromeToast("Cookies cleared for this site");
            }
            catch
            {
                ChromeToast("Could not clear the cookies");
            }
        }

        /// <summary>Ctrl+Shift+A: find a tab by its title or address.</summary>
        private void ShowTabSearch()
        {
            var entries = new List<TabSearchForm.Entry>();

            for (int i = 0; i < _tabControl.TabPages.Count; i++)
            {
                var tab = _tabControl.TabPages[i] as BrowserTab;

                var entry = new TabSearchForm.Entry();
                entry.Index = i;
                entry.Title = tab != null ? tab.DisplayTitle : "New Tab";
                entry.Url = tab != null ? tab.CurrentUrl : "";

                entries.Add(entry);
            }

            using (var search = new TabSearchForm(entries))
            {
                if (search.ShowDialog(this) == DialogResult.OK && search.SelectedIndex >= 0)
                {
                    _tabControl.SelectedIndex = search.SelectedIndex;
                }
            }
        }

        // ── Page context menu ─────────────────────────────────────────────────

        /// <summary>A menu entry that runs the given action when it is clicked.</summary>
        private static ToolStripMenuItem MenuAction(string text, Action action)
        {
            return new ToolStripMenuItem(text, null, delegate { action(); });
        }

        private void CopyText(string text)
        {
            if (string.IsNullOrEmpty(text)) return;

            try
            {
                Clipboard.SetText(text);
                ChromeToast("Copied");
            }
            catch
            {
                // Another process can hold the clipboard; nothing to do.
            }
        }

        /// <summary>Saves the page on screen through Chromium's own save dialog.</summary>
        private async void SavePage()
        {
            var tab = ActiveTab;
            if (tab == null || tab.WebView == null || tab.WebView.CoreWebView2 == null) return;

            try
            {
                await tab.WebView.CoreWebView2.ShowSaveAsUIAsync();
            }
            catch
            {
                ChromeToast("This page cannot be saved");
            }
        }

        /// <summary>Prints the page on screen through Chromium's print preview.</summary>
        private static void PrintPage(BrowserTab tab)
        {
            if (tab == null || tab.WebView == null || tab.WebView.CoreWebView2 == null) return;

            try
            {
                tab.WebView.CoreWebView2.ShowPrintUI(CoreWebView2PrintDialogKind.Browser);
            }
            catch
            {
                // Printing is the engine's business; a refusal is not fatal.
            }
        }

        /// <summary>A file name for an image the page pointed at.</summary>
        private static string ImageFileName(string url)
        {
            try
            {
                string name = Path.GetFileName(new Uri(url).AbsolutePath);
                if (string.IsNullOrEmpty(name)) name = "image";

                foreach (char bad in Path.GetInvalidFileNameChars())
                {
                    name = name.Replace(bad, '_');
                }
                return name;
            }
            catch
            {
                return "image";
            }
        }

        /// <summary>
        /// Downloads an image the page is showing and writes it where the user
        /// says. The bytes are fetched again because the engine only hands over
        /// the address, not the pixels.
        /// </summary>
        private void SaveImage(string url)
        {
            if (string.IsNullOrEmpty(url)) return;

            string suggested = ImageFileName(url);

            var dialog = new SaveFileDialog();
            dialog.Title = "Save image";
            dialog.FileName = suggested;
            dialog.Filter = "Images|*.png;*.jpg;*.jpeg;*.gif;*.webp;*.bmp;*.svg;*.ico|All files|*.*";
            if (dialog.ShowDialog(this) != DialogResult.OK) return;

            string path = dialog.FileName;

            var client = new System.Net.WebClient();
            client.DownloadDataCompleted += delegate(object sender, System.Net.DownloadDataCompletedEventArgs args)
            {
                try
                {
                    if (args.Error != null || args.Cancelled)
                    {
                        ChromeToast("Could not save the image");
                        return;
                    }

                    File.WriteAllBytes(path, args.Result);
                    ChromeToast("Image saved");
                }
                catch
                {
                    ChromeToast("Could not save the image");
                }
                finally
                {
                    client.Dispose();
                }
            };

            try
            {
                client.DownloadDataAsync(new Uri(url));
            }
            catch
            {
                ChromeToast("Could not save the image");
                client.Dispose();
            }
        }

        /// <summary>Copies the pixels of an image the page is showing.</summary>
        private void CopyImage(string url)
        {
            if (string.IsNullOrEmpty(url)) return;

            var client = new System.Net.WebClient();
            client.DownloadDataCompleted += delegate(object sender, System.Net.DownloadDataCompletedEventArgs args)
            {
                try
                {
                    if (args.Error != null || args.Cancelled)
                    {
                        ChromeToast("Could not copy the image");
                        return;
                    }

                    using (var stream = new MemoryStream(args.Result))
                    using (var image = Image.FromStream(stream))
                    {
                        Clipboard.SetImage(image);
                        ChromeToast("Image copied");
                    }
                }
                catch
                {
                    ChromeToast("Could not copy the image");
                }
                finally
                {
                    client.Dispose();
                }
            };

            try
            {
                client.DownloadDataAsync(new Uri(url));
            }
            catch
            {
                ChromeToast("Could not copy the image");
                client.Dispose();
            }
        }

        /// <summary>Opens a page in a fresh private window, straight away.</summary>
        private static void OpenInPrivateWindow(string url)
        {
            var window = new MainForm(true, new List<string> { url }, 0);
            window.Show();
        }

        /// <summary>
        /// The menu a right-click on the page raises. WebView2 reports what was
        /// under the pointer, so the menu knows a link from an image and offers
        /// only the actions that fit, the way a browser's own menu does.
        /// </summary>
        private void OnTabContextMenuRequested(BrowserTab tab, CoreWebView2ContextMenuRequestedEventArgs e)
        {
            if (tab == null || tab != ActiveTab)
            {
                e.Handled = true;
                return;
            }

            var target = e.ContextMenuTarget;

            // Text fields keep Chromium's own menu. It carries the spelling
            // suggestions and the paste options the engine never hands to us,
            // and a field is exactly where those matter.
            if (target != null && target.IsEditable)
            {
                e.Handled = false;
                return;
            }

            // Everything else is drawn by Toge, so the default menu stays away.
            e.Handled = true;

            var menu = NewMenu();
            var engine = Settings.Current.Engine;

            if (target != null && target.HasLinkUri && !string.IsNullOrEmpty(target.LinkUri))
            {
                string link = target.LinkUri;
                menu.Items.Add(MenuAction("Open Link in New Tab", delegate { AddNewTab(link); }));
                menu.Items.Add(MenuAction("Open Link in New Window", delegate { NewWindowWith(link); }));
                menu.Items.Add(MenuAction("Open Link in Private Window", delegate { OpenInPrivateWindow(link); }));
                menu.Items.Add(new ToolStripSeparator());
                menu.Items.Add(MenuAction("Copy Link Address", delegate { CopyText(link); }));
                menu.Items.Add(new ToolStripSeparator());
            }

            if (target != null && target.Kind == CoreWebView2ContextMenuTargetKind.Image
                && target.HasSourceUri && !string.IsNullOrEmpty(target.SourceUri))
            {
                string image = target.SourceUri;
                menu.Items.Add(MenuAction("Open Image in New Tab", delegate { AddNewTab(image); }));
                menu.Items.Add(MenuAction("Copy Image", delegate { CopyImage(image); }));
                menu.Items.Add(MenuAction("Save Image As\u2026", delegate { SaveImage(image); }));
                menu.Items.Add(MenuAction("Copy Image Address", delegate { CopyText(image); }));
                menu.Items.Add(new ToolStripSeparator());
            }

            if (target != null && target.HasSelection && !string.IsNullOrEmpty(target.SelectionText))
            {
                string selected = target.SelectionText;
                string shown = selected.Length > 32 ? selected.Substring(0, 29) + "\u2026" : selected;

                menu.Items.Add(MenuAction("Copy", delegate { CopyText(selected); }));
                menu.Items.Add(MenuAction("Search " + engine.ShortName + " for \u201c" + shown + "\u201d",
                    delegate { AddNewTab(engine.Build(selected)); }));
                menu.Items.Add(new ToolStripSeparator());
            }

            if (target != null && target.IsEditable)
            {
                var page = tab;
                menu.Items.Add(MenuAction("Undo", delegate { page.RunScript("document.execCommand('undo')"); }));
                menu.Items.Add(MenuAction("Cut", delegate { page.RunScript("document.execCommand('cut')"); }));
                menu.Items.Add(MenuAction("Copy", delegate { page.RunScript("document.execCommand('copy')"); }));
                menu.Items.Add(MenuAction("Paste", delegate { page.RunScript("document.execCommand('paste')"); }));
                menu.Items.Add(MenuAction("Select All", delegate { page.RunScript("document.execCommand('selectAll')"); }));
                menu.Items.Add(new ToolStripSeparator());
            }

            var back = MenuAction("Back", delegate { if (ActiveTab != null) ActiveTab.GoBack(); });
            back.Enabled = tab.CanGoBack;
            var forward = MenuAction("Forward", delegate { if (ActiveTab != null) ActiveTab.GoForward(); });
            forward.Enabled = tab.CanGoForward;

            menu.Items.Add(back);
            menu.Items.Add(forward);
            menu.Items.Add(MenuAction("Reload", delegate { if (ActiveTab != null) ActiveTab.Reload(); }));
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(MenuAction("Find on Page\u2026", delegate { tab.ToggleFind(); }));
            menu.Items.Add(MenuAction("Save Page As\u2026", delegate { SavePage(); }));
            menu.Items.Add(MenuAction("Print\u2026", delegate { PrintPage(tab); }));
            menu.Items.Add(MenuAction("View Page Source", delegate { ShowPageSource(); }));

            ShowContextMenu(menu, tab, e.Location);
        }

        /// <summary>
        /// Shows a menu at a point the page reported. The point is in the page's
        /// own pixels, so it needs the tab's position on screen as well.
        /// </summary>
        private void ShowContextMenu(ContextMenuStrip menu, BrowserTab tab, Point pagePoint)
        {
            if (tab == null || tab.WebView == null) return;

            Point at = tab.WebView.PointToScreen(
                new Point(Dpi.Px(Math.Max(0, pagePoint.X)), Dpi.Px(Math.Max(0, pagePoint.Y))));

            Rectangle work = Screen.FromControl(this).WorkingArea;
            int x = Math.Min(Math.Max(work.Left, at.X), Math.Max(work.Left, work.Right - menu.Width - Dpi.Px(4)));
            int y = Math.Min(Math.Max(work.Top, at.Y), Math.Max(work.Top, work.Bottom - menu.Height - Dpi.Px(4)));

            menu.Show(new Point(x, y));
        }

        // ── Site permissions ─────────────────────────────────────────────────

        /// <summary>The words Toge uses for each permission a page can ask for.</summary>
        public static string PermissionName(CoreWebView2PermissionKind kind)
        {
            switch (kind)
            {
                case CoreWebView2PermissionKind.Microphone: return "use your microphone";
                case CoreWebView2PermissionKind.Camera: return "use your camera";
                case CoreWebView2PermissionKind.Geolocation: return "know your location";
                case CoreWebView2PermissionKind.Notifications: return "send you notifications";
                case CoreWebView2PermissionKind.ClipboardRead: return "read your clipboard";
                case CoreWebView2PermissionKind.OtherSensors: return "use your device's sensors";
                case CoreWebView2PermissionKind.MultipleAutomaticDownloads: return "download multiple files";
                case CoreWebView2PermissionKind.FileReadWrite: return "read and write files you pick";
                case CoreWebView2PermissionKind.Autoplay: return "play sound automatically";
                case CoreWebView2PermissionKind.LocalFonts: return "see the fonts on your device";
                case CoreWebView2PermissionKind.MidiSystemExclusiveMessages: return "use your MIDI devices";
                case CoreWebView2PermissionKind.WindowManagement: return "manage windows";
                default: return "use a feature of your device";
            }
        }

        /// <summary>Short label used in the settings page's permission list.</summary>
        public static string PermissionLabel(CoreWebView2PermissionKind kind)
        {
            switch (kind)
            {
                case CoreWebView2PermissionKind.Microphone: return "Microphone";
                case CoreWebView2PermissionKind.Camera: return "Camera";
                case CoreWebView2PermissionKind.Geolocation: return "Location";
                case CoreWebView2PermissionKind.Notifications: return "Notifications";
                case CoreWebView2PermissionKind.ClipboardRead: return "Clipboard";
                case CoreWebView2PermissionKind.OtherSensors: return "Sensors";
                case CoreWebView2PermissionKind.MultipleAutomaticDownloads: return "Multiple downloads";
                case CoreWebView2PermissionKind.FileReadWrite: return "File editing";
                case CoreWebView2PermissionKind.Autoplay: return "Autoplay";
                case CoreWebView2PermissionKind.LocalFonts: return "Local fonts";
                case CoreWebView2PermissionKind.MidiSystemExclusiveMessages: return "MIDI devices";
                case CoreWebView2PermissionKind.WindowManagement: return "Window management";
                default: return "Other";
            }
        }

        /// <summary>
        /// Asks the user, in Toge's own words, before a page uses the camera,
        /// the microphone, their location and so on, and remembers the answer
        /// for that site the way a browser normally does.
        /// </summary>
        private void OnTabPermissionRequested(BrowserTab tab, CoreWebView2PermissionRequestedEventArgs e)
        {
            var deferral = e.GetDeferral();
            try
            {
                e.Handled = true;

                // Nothing on screen is asking, so there is nothing to ask about.
                if (tab == null || tab != ActiveTab)
                {
                    e.State = CoreWebView2PermissionState.Deny;
                    e.SavesInProfile = false;
                    return;
                }

                string host = SiteSettings.HostOf(e.Uri);
                if (string.IsNullOrEmpty(host)) host = e.Uri;

                using (var prompt = new PermissionPromptForm(host, PermissionName(e.PermissionKind)))
                {
                    bool decided = prompt.ShowDialog(this) == DialogResult.OK;

                    e.State = prompt.Allowed
                        ? CoreWebView2PermissionState.Allow
                        : CoreWebView2PermissionState.Deny;

                    // A dismissed prompt is a refusal for now, not a lasting one.
                    e.SavesInProfile = decided;
                }
            }
            finally
            {
                deferral.Complete();
            }
        }

        /// <summary>The id the settings page uses for a permission kind.</summary>
        internal static string PermissionId(CoreWebView2PermissionKind kind)
        {
            return kind.ToString().ToLowerInvariant();
        }

        /// <summary>The id the settings page uses for an answer.</summary>
        internal static string StateId(CoreWebView2PermissionState state)
        {
            if (state == CoreWebView2PermissionState.Allow) return "allow";
            if (state == CoreWebView2PermissionState.Deny) return "block";
            return "ask";
        }

        private static CoreWebView2PermissionKind KindFromId(string id)
        {
            if (string.IsNullOrEmpty(id)) return CoreWebView2PermissionKind.UnknownPermission;

            foreach (CoreWebView2PermissionKind kind in Enum.GetValues(typeof(CoreWebView2PermissionKind)))
            {
                if (string.Equals(kind.ToString(), id, StringComparison.OrdinalIgnoreCase)) return kind;
            }

            return CoreWebView2PermissionKind.UnknownPermission;
        }

        private static CoreWebView2PermissionState StateFromId(string id)
        {
            if (id == "allow") return CoreWebView2PermissionState.Allow;
            if (id == "block") return CoreWebView2PermissionState.Deny;
            return CoreWebView2PermissionState.Default;
        }

        /// <summary>
        /// Lists the answers the user has given, so the settings page can show
        /// them and offer to change them.
        /// </summary>
        private void SendPermissions(BrowserTab tab)
        {
            if (tab == null) return;

            var profile = ProfileForData();
            if (profile == null)
            {
                tab.RunScript("togePermissions([]);");
                return;
            }

            RefreshPermissionList(tab, profile);
        }

        private async void RefreshPermissionList(BrowserTab tab, CoreWebView2Profile profile)
        {
            try
            {
                var settings = await profile.GetNonDefaultPermissionSettingsAsync();

                var ordered = new List<CoreWebView2PermissionSetting>();
                if (settings != null) ordered.AddRange(settings);

                // Grouped by site, so a page that has answered for the camera and
                // the microphone reads as one thing.
                ordered.Sort(delegate (CoreWebView2PermissionSetting a, CoreWebView2PermissionSetting b)
                {
                    int byOrigin = string.Compare(a.PermissionOrigin, b.PermissionOrigin,
                        StringComparison.OrdinalIgnoreCase);
                    if (byOrigin != 0) return byOrigin;

                    return string.Compare(a.PermissionKind.ToString(), b.PermissionKind.ToString(),
                        StringComparison.Ordinal);
                });

                var rows = new List<object>();
                foreach (var setting in ordered)
                {
                    rows.Add(Json.Obj(
                        "origin", setting.PermissionOrigin,
                        "kind", PermissionId(setting.PermissionKind),
                        "kindLabel", PermissionLabel(setting.PermissionKind),
                        "state", StateId(setting.PermissionState)));
                }

                string json = Json.Write(rows);
                tab.RunScript("togePermissions(" + (json == null ? "[]" : json) + ");");
            }
            catch
            {
                // A profile that is shutting down has nothing to report.
                tab.RunScript("togePermissions([]);");
            }
        }

        private async void SetPermission(BrowserTab tab, string origin, string kindId, string stateId)
        {
            var profile = ProfileForData();
            if (profile == null || string.IsNullOrEmpty(origin)) return;

            var kind = KindFromId(kindId);
            if (kind == CoreWebView2PermissionKind.UnknownPermission) return;

            try
            {
                await profile.SetPermissionStateAsync(kind, origin, StateFromId(stateId));
            }
            catch
            {
                // The profile may be closing.
            }

            SendPermissions(tab);
        }

        /// <summary>Puts every answer back to "ask", leaving the browser tidy.</summary>
        private async void ClearPermissions(BrowserTab tab)
        {
            var profile = ProfileForData();
            if (profile == null) return;

            try
            {
                var settings = await profile.GetNonDefaultPermissionSettingsAsync();

                var items = new List<CoreWebView2PermissionSetting>();
                if (settings != null) items.AddRange(settings);

                foreach (var setting in items)
                {
                    await profile.SetPermissionStateAsync(
                        setting.PermissionKind,
                        setting.PermissionOrigin,
                        CoreWebView2PermissionState.Default);
                }
            }
            catch
            {
                // Nothing left to clear.
            }

            SendPermissions(tab);
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

        private void FocusUrlBar(bool openTopSites)
        {
            // Defer: moving focus during PreFilterMessage can be undone by the
            // control that still owns the key message.
            BeginInvoke(new Action(delegate
            {
                if (_chromeWebView.CoreWebView2 == null) return;

                _chromeWebView.Focus();
                _chromeWebView.CoreWebView2.PostWebMessageAsJson(
                    "{\"action\":\"focus_url\",\"newtab\":" + (openTopSites ? "true" : "false") + "}");
                _chromeWebView.CoreWebView2.ExecuteScriptAsync(
                    "window.BraveAPI && window.BraveAPI.focusUrl(" + (openTopSites ? "true" : "false") + ");");
            }));
        }

        /// <summary>
        /// F6 hands the keyboard to the chrome, starting at the tab strip, so
        /// every button there can be reached with Tab and Enter. Without it the
        /// chrome would be mouse-only.
        /// </summary>
        private void FocusChrome()
        {
            BeginInvoke(new Action(delegate
            {
                if (_chromeWebView.CoreWebView2 == null) return;

                _chromeWebView.Focus();
                _chromeWebView.CoreWebView2.PostWebMessageAsJson("{\"action\":\"focus_chrome\"}");
            }));
        }

        // ── Address bar suggestions ───────────────────────────────────────────

        private UrlSuggestForm _suggest;
        private List<SuggestEntry> _suggestEntries;
        private int _suggestIndex = -1;
        private string _suggestTyped = "";
        private int _suggestX = -1;
        private int _suggestY = -1;
        private int _suggestW;

        /// <summary>Token of the newest network answer wanted; older ones expire.</summary>
        private int _suggestFetchId;

        /// <summary>Completions already fetched, so backspacing re-shows them instantly.</summary>
        private readonly Dictionary<string, List<string>> _fetchedTerms =
            new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Builds the list that hangs under the address bar: the search first,
        /// then the pages the user has visited, saved or pinned that match.
        /// With nothing typed yet it offers the sites the browser would point
        /// at anyway, when the field was opened from a fresh tab.
        /// </summary>
        private void ShowSuggestions(string text, int cssX, int cssY, int cssWidth, bool openOnEmpty)
        {
            _suggestTyped = text ?? "";

            if (cssX >= 0) _suggestX = cssX;
            if (cssY >= 0) _suggestY = cssY;
            if (cssWidth > 0) _suggestW = cssWidth;

            string needle = _suggestTyped.Trim();

            List<SuggestEntry> entries;
            if (needle.Length == 0)
            {
                entries = openOnEmpty ? TopSuggestions() : null;
                if (entries == null || entries.Count == 0)
                {
                    HideSuggestions();
                    return;
                }
            }
            else
            {
                entries = BuildSuggestions(needle, null);
                if (entries.Count == 0)
                {
                    HideSuggestions();
                    return;
                }

                // The engine's own completions — the "how to open the door"
                // lines the whole world searches — trail a moment later, as
                // fast as the network answers.
                FetchEngineSuggestions(needle);
            }

            ShowEntryList(entries);
        }

        /// <summary>A search row: its words show, its hidden Url is the engine's page.</summary>
        private static SuggestEntry QueryEntry(SearchEngine engine, string term)
        {
            var entry = new SuggestEntry();
            entry.IsSearch = true;
            entry.Url = engine.Build(term);
            entry.Term = term;
            entry.Title = term;
            entry.Detail = string.Empty;
            return entry;
        }

        /// <summary>Everything a freshly built list needs: the ghost tail, the
        /// row set, and the geometry of the hanging window.</summary>
        private void ShowEntryList(List<SuggestEntry> entries)
        {

            // The grey type-ahead that follows the caret: the first row the
            // typed text starts with, page or search words alike — but the
            // headline "Search Brave for it" row never completes.
            string ghost = null;
            for (int i = 0; i < entries.Count; i++)
            {
                SuggestEntry entry = entries[i];
                string words = entry.IsSearch ? entry.Term : entry.Url;
                ghost = GhostRemainder(words, _suggestTyped);
                if (!string.IsNullOrEmpty(ghost)) break;
            }
            SendGhost(ghost);

            if (_suggest == null || _suggest.IsDisposed)
            {
                _suggest = new UrlSuggestForm();
                _suggest.Chosen += AcceptSuggestion;
            }

            _suggest.SetEntries(entries);

            if (_suggestX < 0 || _suggestY < 0 || _chromeWebView == null) return;

            Point at = _chromeWebView.PointToScreen(
                new Point(Dpi.Px(_suggestX), Dpi.Px(_suggestY) + Dpi.Px(3)));

            int width = _suggestW > 0 ? Dpi.Px(_suggestW) : Dpi.Px(520);
            Rectangle work = Screen.FromControl(this).WorkingArea;
            int x = Math.Min(Math.Max(work.Left, at.X),
                Math.Max(work.Left, work.Right - width - Dpi.Px(6)));

            _suggest.ShowAt(new Point(x, at.Y), width);
        }

        /// <summary>
        /// Moves the highlight with the arrow keys the chrome forwarded. The
        /// highlighted address is written back into the field, so Enter simply
        /// takes whatever the field holds and needs no special case there.
        /// </summary>
        private void MoveSuggestion(string key)
        {
            if (_suggestEntries == null || _suggestEntries.Count == 0) return;
            if (_suggest == null || _suggest.IsDisposed || !_suggest.IsShown) return;

            if (key == "down") _suggestIndex = Math.Min(_suggestIndex + 1, _suggestEntries.Count - 1);
            else if (key == "up") _suggestIndex = Math.Max(_suggestIndex - 1, -1);
            else return;

            _suggest.Highlight(_suggestIndex);

            string text = _suggestIndex < 0
                ? _suggestTyped
                : _suggestEntries[_suggestIndex].Url;

            SetChromeInput(text);
        }

        private void SetChromeInput(string text)
        {
            if (!_chromeReady || _chromeWebView.CoreWebView2 == null) return;

            string literal = Json.Write(text ?? string.Empty);
            if (literal == null) return;

            _chromeWebView.CoreWebView2.ExecuteScriptAsync(
                "window.BraveAPI && window.BraveAPI.setInput(" + literal + ");");
        }

        private void HideSuggestions()
        {
            if (_suggest != null && !_suggest.IsDisposed) _suggest.HideQuietly();

            _suggestEntries = null;
            _suggestIndex = -1;
            _suggestFetchId++;   // an answer still in flight is out of date now
            SendGhost(null);
        }

        /// <summary>
        /// Tells the chrome what grey tail should follow the caret: the
        /// remainder of the first page address that starts with what was
        /// typed. Nothing to say clears whatever is on screen.
        /// </summary>
        private void SendGhost(string remainder)
        {
            if (!_chromeReady || _chromeWebView.CoreWebView2 == null) return;

            _chromeWebView.CoreWebView2.ExecuteScriptAsync(
                "window.BraveAPI && window.BraveAPI.setGhost(" +
                Json.Write(_suggestTyped ?? string.Empty) + "," +
                Json.Write(remainder ?? string.Empty) + ");");
        }

        /// <summary>
        /// The field shows places without their scheme — and usually without
        /// "www." — so the type-ahead is measured against that shorter
        /// spelling. A paste that carried the scheme still completes against
        /// the raw address.
        /// </summary>
        private static string GhostRemainder(string url, string typed)
        {
            if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(typed)) return null;

            if (url.StartsWith(typed, StringComparison.OrdinalIgnoreCase))
            {
                return url.Substring(typed.Length);
            }

            string display = url;
            if (display.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) display = display.Substring(8);
            else if (display.StartsWith("http://", StringComparison.OrdinalIgnoreCase)) display = display.Substring(7);
            if (display.StartsWith("www.", StringComparison.OrdinalIgnoreCase)) display = display.Substring(4);

            return display.StartsWith(typed, StringComparison.OrdinalIgnoreCase)
                ? display.Substring(typed.Length)
                : null;
        }

        /// <summary>
        /// Asks the search engine for completions of what was typed — the
        /// "how to open the door" lines. The list is already on screen with
        /// the local rows; when the answer lands the list is rebuilt with
        /// them folded in. Private windows never send anything.
        /// </summary>
        private void FetchEngineSuggestions(string needle)
        {
            if (string.IsNullOrWhiteSpace(needle) || _privateMode) return;

            SearchEngine engine = Settings.Current.Engine;
            if (engine == null || string.IsNullOrEmpty(engine.SuggestEndpoint)) return;

            List<string> known;
            if (_fetchedTerms.TryGetValue(needle, out known))
            {
                ApplySearchSuggestions(needle, known);
                return;
            }

            if (_fetchedTerms.Count > 96) _fetchedTerms.Clear();

            int id = ++_suggestFetchId;
            string endpoint = engine.SuggestEndpoint.Replace("{query}", Uri.EscapeDataString(needle));
            SearchWords(id, needle, endpoint);
        }

        private async void SearchWords(int id, string needle, string endpoint)
        {
            List<string> terms = null;

            try
            {
                var request = (HttpWebRequest)WebRequest.Create(endpoint);
                request.Method = "GET";
                request.Timeout = 3500;
                request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Toge";

                using (var response = await request.GetResponseAsync())
                using (var stream = response.GetResponseStream())
                using (var reader = new StreamReader(stream))
                {
                    terms = ParseSuggestions(reader.ReadToEnd());
                }
            }
            catch
            {
                // Offline, slow, or a shape no one promised: the list that is
                // already showing simply stays.
            }

            if (terms == null || _suggestTyped != needle || id != _suggestFetchId) return;

            _fetchedTerms[needle] = terms;
            ApplySearchSuggestions(needle, terms);
        }
        private void ApplySearchSuggestions(string needle, List<string> terms)
        {
            if (needle.Length == 0 || needle != _suggestTyped) return;

            List<SuggestEntry> entries = BuildSuggestions(needle, terms);
            if (entries.Count == 0) return;

            ShowEntryList(entries);
        }

        /// <summary>
        /// Firefox-style suggestion JSON — the typed words first, their
        /// completions second — which Brave, Google, DuckDuckGo and Bing all
        /// share. Anything else yields nothing rather than an error.
        /// </summary>
        private static List<string> ParseSuggestions(string json)
        {
            var terms = new List<string>();
            if (string.IsNullOrEmpty(json)) return terms;

            try
            {
                var answer = Json.AsList(Json.Parse(json));
                if (answer == null || answer.Count < 2) return terms;

                var completions = Json.AsList(answer[1]);
                if (completions == null) return terms;

                foreach (object item in completions)
                {
                    string term = item as string;
                    if (!string.IsNullOrEmpty(term)) terms.Add(term);
                    if (terms.Count >= 8) break;
                }
            }
            catch
            {
                // Not the usual shape; the caller shows its local rows only.
            }

            return terms;
        }

        /// <summary>
        /// What a bare address field offers before anything is typed: the
        /// pages visited most recently, then the saved ones. Eight rows are
        /// plenty to say "this is where you usually go".
        /// </summary>
        private List<SuggestEntry> TopSuggestions()
        {
            var entries = new List<SuggestEntry>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in _historyManager.GetHistory())
            {
                if (entries.Count >= 8) break;
                if (string.IsNullOrEmpty(item.Url)) continue;
                if (item.Url.StartsWith("about:", StringComparison.OrdinalIgnoreCase)) continue;
                if (item.Url.StartsWith("data:", StringComparison.OrdinalIgnoreCase)) continue;
                if (!seen.Add(item.Url)) continue;
                entries.Add(PageEntry(item.Title, item.Url, FaviconStore.For(item.Url)));
            }

            foreach (var bookmark in _bookmarkManager.GetBookmarks())
            {
                if (entries.Count >= 8) break;
                if (string.IsNullOrEmpty(bookmark.Url) || !seen.Add(bookmark.Url)) continue;
                entries.Add(PageEntry(bookmark.Title, bookmark.Url, bookmark.Icon));
            }

            return entries;
        }

        private void AcceptSuggestion(SuggestEntry entry)
        {
            HideSuggestions();
            if (entry == null || string.IsNullOrEmpty(entry.Url)) return;

            if (ActiveTab != null) ActiveTab.Navigate(entry.Url);
            FocusContent();
        }

        private List<SuggestEntry> BuildSuggestions(string needle, List<string> searchTerms)
        {
            var entries = new List<SuggestEntry>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var engine = Settings.Current.Engine;

            var search = QueryEntry(engine, needle);
            search.Title = "Search " + engine.ShortName + " for \u201c" + Shorten(needle, 48) + "\u201d";
            entries.Add(search);

            // Search words first — what the user asked for before, then what
            // the engine says the whole world asks for. Address-like typing
            // ("github.com") leaves most of the room to places to go.
            var usedTerms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            usedTerms.Add(needle);

            int searchCap;
            if (needle.IndexOf(' ') >= 0) searchCap = 5;
            else if (needle.IndexOf('.') >= 0) searchCap = 2;
            else searchCap = 3;

            foreach (var prior in _historyManager.GetSearches())
            {
                if (entries.Count > searchCap) break;
                if (prior.Term == null) continue;
                if (prior.Term.IndexOf(needle, StringComparison.OrdinalIgnoreCase) < 0) continue;
                if (!usedTerms.Add(prior.Term)) continue;
                entries.Add(QueryEntry(engine, prior.Term));
            }

            if (searchTerms != null)
            {
                foreach (string term in searchTerms)
                {
                    if (string.IsNullOrEmpty(term)) continue;
                    if (entries.Count > searchCap) break;
                    if (!usedTerms.Add(term)) continue;
                    entries.Add(QueryEntry(engine, term));
                }
            }

            // Recent pages first: what was just visited is usually what is meant.
            foreach (var item in _historyManager.GetHistory())
            {
                if (entries.Count >= 8) break;
                if (!Matches(needle, item.Title, item.Url)) continue;
                if (!seen.Add(item.Url)) continue;
                entries.Add(PageEntry(item.Title, item.Url, FaviconStore.For(item.Url)));
            }

            foreach (var bookmark in _bookmarkManager.GetBookmarks())
            {
                if (entries.Count >= 8) break;
                if (!Matches(needle, bookmark.Title, bookmark.Url)) continue;
                if (!seen.Add(bookmark.Url)) continue;
                entries.Add(PageEntry(bookmark.Title, bookmark.Url, bookmark.Icon ?? FaviconStore.For(bookmark.Url)));
            }

            foreach (var favorite in FavoritesStore.Current.GetFavorites())
            {
                if (entries.Count >= 8) break;
                if (!Matches(needle, favorite.Title, favorite.Url)) continue;
                if (!seen.Add(favorite.Url)) continue;
                entries.Add(PageEntry(favorite.Title, favorite.Url, favorite.Icon ?? FaviconStore.For(favorite.Url)));
            }

            return entries;
        }

        /// <summary>
        /// A plain "place I have been" row. The icon travels with it — the
        /// saved one for bookmarks, the host cache for the rest — though only
        /// data URLs survive here: anything else would mean a web trip the
        /// suggestion list does not wait for.
        /// </summary>
        private static SuggestEntry PageEntry(string title, string url, string icon)
        {
            var entry = new SuggestEntry();
            entry.Url = url;
            entry.Title = string.IsNullOrEmpty(title) ? url : title;
            entry.Detail = url;
            entry.IconDataUrl = icon != null && icon.StartsWith("data:", StringComparison.OrdinalIgnoreCase)
                ? icon
                : null;
            return entry;
        }

        private static bool Matches(string needle, string title, string url)
        {
            if (string.IsNullOrEmpty(needle)) return false;

            if (!string.IsNullOrEmpty(title)
                && title.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0) return true;

            return !string.IsNullOrEmpty(url)
                && url.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string Shorten(string text, int limit)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            if (text.Length <= limit) return text;
            return text.Substring(0, limit - 1) + "\u2026";
        }

        private void LayoutTabs()
        {
            if (_tabControl == null || _contentPanel == null) return;

            int w = _contentPanel.ClientSize.Width;
            int h = _contentPanel.ClientSize.Height;
            if (w <= 0 || h <= 0) return;

            // A TabControl insets its pages by the system-drawn border and tab
            // header. Measure that inset at the current DPI instead of assuming
            // 96-DPI pixels, then oversize and lift the control so the inset
            // lands outside the panel and the page meets the chrome with no
            // system-coloured frame.
            Rectangle display = _tabControl.DisplayRectangle;
            int insetLeft = display.Left;
            int insetTop = display.Top;
            int insetRight = _tabControl.ClientSize.Width - display.Right;
            int insetBottom = _tabControl.ClientSize.Height - display.Bottom;

            if (insetLeft < 0) insetLeft = 0;
            if (insetTop < 0) insetTop = 0;
            if (insetRight < 0) insetRight = 0;
            if (insetBottom < 0) insetBottom = 0;

            _tabControl.Bounds = new Rectangle(
                -insetLeft,
                -insetTop,
                w + insetLeft + insetRight,
                h + insetTop + insetBottom);
        }

        /// <summary>
        /// Tidy work for the tab that has just come to the front: put the page
        /// back where it was scrolled to, and take the page's "skip to content"
        /// link out of the way. Both are applied again a moment later, because
        /// the engine settles its own view and focus just after the switch.
        /// </summary>
        private void RestoreTabView()
        {
            var tab = ActiveTab;
            if (tab == null) return;

            tab.RestoreScroll();
            tab.ClearSkipLinkFocus();

            var timer = new Timer();
            timer.Interval = 70;
            timer.Tick += delegate
            {
                timer.Stop();
                timer.Dispose();

                if (tab == null || tab.IsDisposed || tab != ActiveTab) return;

                tab.RestoreScroll();
                tab.ClearSkipLinkFocus();
            };
            timer.Start();
        }

        private void CycleTab(int delta)
        {
            int count = _tabControl.TabPages.Count;
            if (count < 2) return;
            _tabControl.SelectedIndex = (_tabControl.SelectedIndex + delta + count) % count;
        }

        /// <summary>
        /// Opens another Toge window. A private one is given its own in-private
        /// profile, so nothing it does is written to the normal profile on disk.
        /// </summary>
        internal static MainForm NewWindow(bool privateMode)
        {
            var window = new MainForm(privateMode);
            window.Show();
            return window;
        }

        /// <summary>Opens a window that already has a page in it.</summary>
        internal static MainForm NewWindowWith(string url)
        {
            var window = new MainForm(false, new List<string> { url }, 0);
            window.Show();
            return window;
        }

        /// <summary>
        /// The window a handed-over page should open in: the one on screen if it
        /// is a normal window, otherwise the newest normal window there is.
        /// </summary>
        internal static MainForm Target
        {
            get
            {
                var active = Form.ActiveForm as MainForm;
                if (active != null && !active.IsPrivate && !active.IsDisposed) return active;

                for (int i = Windows.Count - 1; i >= 0; i--)
                {
                    var window = Windows[i];
                    if (window != null && !window.IsDisposed && !window.IsPrivate) return window;
                }

                return null;
            }
        }

        /// <summary>Brings the window forward and shows a page in a new tab.</summary>
        public void OpenUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return;

            if (WindowState == FormWindowState.Minimized) WindowState = FormWindowState.Normal;

            Activate();
            AddNewTab(url);
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
                "Toge\n\n" +
                "A lightweight browser with a Safari-inspired interface and Brave-grade Shields.\n\n" +
                "Chromium engine powered by Microsoft Edge WebView2.\n" +
                "Version 2.0",
                "About Toge",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        internal bool HandleShortcutKey(int vk)
        {
            // Only the window the user is working in answers, so two windows
            // never both act on one keystroke.
            if (GetForegroundWindow() != Handle) return false;

            bool ctrl = (GetKeyState(0x11) & 0x8000) != 0;
            bool alt = (GetKeyState(0x12) & 0x8000) != 0;
            bool shift = (GetKeyState(0x10) & 0x8000) != 0;

            string action = null;

            if (ctrl && !alt)
            {
                switch (vk)
                {
                    case 0x54: action = shift ? "reopen_tab" : "new_tab"; break;   // Ctrl+T / Ctrl+Shift+T
                    case 0x4E: action = shift ? "new_private_window" : "new_window"; break; // Ctrl+N
                    case 0x57: action = "close_tab"; break;                        // Ctrl+W
                    case 0x4C: action = "focus_url"; break;                        // Ctrl+L
                    case 0x52: action = "reload"; break;                           // Ctrl+R
                    case 0x53: action = "save_page"; break;                        // Ctrl+S
                    case 0x44: action = "bookmark"; break;                         // Ctrl+D
                    case 0x48: action = "history"; break;                          // Ctrl+H
                    case 0x46: action = "find"; break;                             // Ctrl+F
                    case 0x4A: action = "downloads"; break;                        // Ctrl+J
                    case 0x55: action = "source"; break;                           // Ctrl+U
                    case 0x4F: if (shift) action = "bookmarks"; break;             // Ctrl+Shift+O
                    case 0x42: if (shift) action = "bookmarks_bar"; break;         // Ctrl+Shift+B
                    case 0x41: if (shift) action = "tab_search"; break;            // Ctrl+Shift+A
                    case 0x50: action = "print"; break;                            // Ctrl+P
                    case 0xBC: action = "settings"; break;                         // Ctrl+,
                    case 0x09: action = shift ? "prev_tab" : "next_tab"; break;    // Ctrl+Tab
                    case 0xBB: action = "zoom_in"; break;                          // Ctrl++
                    case 0xBD: action = "zoom_out"; break;                         // Ctrl+-
                    case 0x30: action = "zoom_reset"; break;                       // Ctrl+0
                }
            }
            else if (alt && !ctrl)
            {
                if (vk == 0x25) action = "back";        // Alt+Left
                else if (vk == 0x27) action = "forward"; // Alt+Right
                else if (vk == 0x44) action = "focus_url"; // Alt+D
                else if (vk == 0x46) action = "menu";   // Alt+F
            }
            else if (!ctrl && !alt && vk == 0x74)       // F5
            {
                action = "reload";
            }
            else if (!ctrl && !alt && vk == 0x75)       // F6
            {
                action = shift ? "focus_url" : "focus_chrome";
            }

            if (action == null) return false;

            HandleShortcutAction(action);
            return true;
        }

        /// <summary>
        /// Keys pressed while the chrome has focus. The chrome is a WebView too,
        /// so its browser keys arrive here rather than at the message filter.
        /// </summary>
        private void OnChromeAcceleratorPressed(object sender, CoreWebView2AcceleratorKeyPressedEventArgs e)
        {
            if (e.KeyEventKind != CoreWebView2KeyEventKind.KeyDown
                && e.KeyEventKind != CoreWebView2KeyEventKind.SystemKeyDown) return;

            if (HandleShortcutKey((int)e.VirtualKey)) e.Handled = true;
        }

        /// <summary>
        /// Ctrl+wheel over the page zooms the site, the way every browser does.
        /// The engine never hands wheel events to the host, so the message is
        /// caught before it reaches the page.
        /// </summary>
        internal bool HandleWheelZoom(IntPtr wParam, IntPtr lParam)
        {
            if (GetForegroundWindow() != Handle) return false;
            if ((GetKeyState(0x11) & 0x8000) == 0) return false;

            var tab = ActiveTab;
            if (tab == null || tab.WebView == null) return false;

            // Only a wheel over the page counts; the chrome keeps its own job.
            long lp = lParam.ToInt64();
            int screenX = unchecked((short)(lp & 0xFFFF));
            int screenY = unchecked((short)((lp >> 16) & 0xFFFF));

            Point origin = tab.WebView.PointToScreen(Point.Empty);
            if (!new Rectangle(origin, tab.WebView.Size).Contains(screenX, screenY)) return false;

            long wp = wParam.ToInt64();
            int delta = unchecked((short)((wp >> 16) & 0xFFFF));
            if (delta == 0) return false;

            StepSiteZoom(delta > 0 ? 1 : -1);
            return true;
        }

        /// <summary>
        /// Browser keys pressed in the page on show. Answering one marks it
        /// handled, which is what stops Chromium drawing its own find bar or
        /// reloading behind Toge's back; everything Toge does not claim keeps
        /// working the way the engine normally does it.
        /// </summary>
        private void OnTabAcceleratorPressed(BrowserTab tab, int vk, CoreWebView2AcceleratorKeyPressedEventArgs e)
        {
            if (tab != ActiveTab) return;
            if (HandleShortcutKey(vk)) e.Handled = true;
        }

        private void HandleShortcutAction(string action)
        {
            switch (action)
            {
                case "new_tab": AddNewTab(); break;
                case "new_window": NewWindow(false); break;
                case "new_private_window": NewWindow(true); break;
                case "reopen_tab": ReopenClosedTab(); break;
                case "close_tab": CloseTabAt(_tabControl.SelectedIndex); break;
                case "focus_url": FocusUrlBar(false); break;
                case "reload": if (ActiveTab != null) ActiveTab.Reload(); break;
                case "bookmark": ToggleBookmarkCurrent(); break;
                case "history": ShowHistory(); break;
                case "find": if (ActiveTab != null) ActiveTab.ToggleFind(); break;
                case "downloads": ShowDownloads(); break;
                case "bookmarks": ShowBookmarks(); break;
                case "bookmarks_bar": Settings.Current.Set("bookmarksBar", !Settings.Current.ShowBookmarksBar); break;
                case "source": ShowPageSource(); break;
                case "settings": OpenSettings(); break;
                case "back": if (ActiveTab != null) ActiveTab.GoBack(); break;
                case "forward": if (ActiveTab != null) ActiveTab.GoForward(); break;
                case "next_tab": CycleTab(1); break;
                case "prev_tab": CycleTab(-1); break;
                case "zoom_in": StepSiteZoom(1); break;
                case "zoom_out": StepSiteZoom(-1); break;
                case "zoom_reset": ResetSiteZoom(); break;
                case "save_page": SavePage(); break;
                case "print": PrintPage(ActiveTab); break;
                case "focus_chrome": FocusChrome(); break;
                case "tab_search": ShowTabSearch(); break;
                case "menu": ShowBraveMenu(); break;
            }
        }

        /// <summary>
        /// Ctrl+plus and Ctrl+minus walk the same steps as the AA popover, and
        /// belong to the site on screen rather than to Toge as a whole.
        /// </summary>
        private void StepSiteZoom(int direction)
        {
            var tab = ActiveTab;
            if (tab == null) return;

            string url = tab.CurrentUrl;
            if (SiteSettings.HostOf(url).Length == 0) return;

            int next = SiteSettings.Step(SiteSettings.Current.ZoomFor(url), direction);
            SiteSettings.Current.SetZoom(url, next);
            tab.ApplySiteZoom();
            ChromeToast(SiteSettings.PercentText(next));
        }

        private void ResetSiteZoom()
        {
            var tab = ActiveTab;
            if (tab == null) return;
            if (SiteSettings.HostOf(tab.CurrentUrl).Length == 0) return;

            SiteSettings.Current.SetZoom(tab.CurrentUrl, 100);
            tab.ApplySiteZoom();
            ChromeToast("100%");
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

        private void ShowBookmarks()
        {
            using (var bookmarksForm = new BookmarksForm(_bookmarkManager))
            {
                bookmarksForm.ShowDialog(this);

                if (bookmarksForm.Changed) SyncBookmarked();

                if (!string.IsNullOrEmpty(bookmarksForm.SelectedUrl) && ActiveTab != null)
                {
                    ActiveTab.Navigate(bookmarksForm.SelectedUrl);
                }
            }
        }

        private void ShowImport()
        {
            using (var importForm = new ImportForm(_bookmarkManager, _historyManager))
            {
                importForm.ShowDialog(this);

                if (importForm.FavoritesChanged) RefreshHomeTabs();
            }
        }

        // ── Downloads ─────────────────────────────────────────────────────────

        private DownloadsForm _downloadsWindow;

        /// <summary>Opens the downloads list, reusing the window if it is open.</summary>
        private void ShowDownloads()
        {
            if (_downloadsWindow != null && !_downloadsWindow.IsDisposed)
            {
                if (_downloadsWindow.WindowState == FormWindowState.Minimized)
                {
                    _downloadsWindow.WindowState = FormWindowState.Normal;
                }
                _downloadsWindow.Activate();
                return;
            }

            _downloadsWindow = new DownloadsForm();
            _downloadsWindow.Owner = this;
            _downloadsWindow.FormClosed += delegate { _downloadsWindow = null; };
            _downloadsWindow.Show();
        }

        /// <summary>Keeps the toolbar's download count in step with the list.</summary>
        private void SyncDownloads()
        {
            if (!_chromeReady || _chromeWebView.CoreWebView2 == null) return;

            int total = DownloadsManager.Current.GetItems().Count;
            int active = DownloadsManager.Current.ActiveCount;

            _chromeWebView.CoreWebView2.ExecuteScriptAsync(
                string.Format("window.BraveAPI && window.BraveAPI.setDownloads({0}, {1});", total, active));
        }

        private void OnDownloadsChanged()
        {
            SyncDownloads();
        }

        /// <summary>
        /// The Google account changed (signed in, out, or a name arrived), so
        /// every start page redraws its chip. Pages that are not the start page
        /// ignore the call.
        /// </summary>
        private void OnGoogleAccountChanged()
        {
            RefreshHomeTabs();
        }

        /// <summary>
        /// Runs a download Toge's way: the file lands in the Downloads folder
        /// (or wherever the user asks), the list keeps up with it, and WebView2's
        /// own download bar never appears inside the page.
        /// </summary>
        private void OnTabDownloadStarting(BrowserTab tab, CoreWebView2DownloadStartingEventArgs e)
        {
            var deferral = e.GetDeferral();
            try
            {
                var operation = e.DownloadOperation;
                if (operation == null) return;

                string folder = Path.GetDirectoryName(e.ResultFilePath);
                string name = Path.GetFileName(e.ResultFilePath);
                if (string.IsNullOrEmpty(folder)) folder = DefaultDownloadFolder();
                if (string.IsNullOrEmpty(name)) name = "download";

                if (Settings.Current.AskDownloadPath)
                {
                    string chosen = AskForSavePath(folder, name);
                    if (string.IsNullOrEmpty(chosen))
                    {
                        e.Cancel = true;
                        return;
                    }

                    e.ResultFilePath = chosen;
                    name = Path.GetFileName(chosen);
                }
                else
                {
                    string target = UniquePath(Path.Combine(folder, name));
                    e.ResultFilePath = target;
                    name = Path.GetFileName(target);
                }

                e.Handled = true;

                var record = DownloadsManager.Current.Add(
                    operation.Uri, name, e.ResultFilePath, TotalOf(operation));
                record.Operation = operation;

                operation.BytesReceivedChanged += delegate
                {
                    record.BytesReceived = (long)operation.BytesReceived;
                    long total = TotalOf(operation);
                    if (total > 0) record.TotalBytes = total;
                    // Progress is cheap to show and expensive to write, so the
                    // list is told but the file on disk is left alone.
                    DownloadsManager.Current.Notify();
                };

                operation.StateChanged += delegate
                {
                    record.BytesReceived = (long)operation.BytesReceived;
                    long total = TotalOf(operation);
                    if (total > 0) record.TotalBytes = total;

                    if (operation.State == CoreWebView2DownloadState.Completed)
                    {
                        record.State = "done";
                        record.Detail = null;
                        record.Operation = null;
                        ChromeToast("Downloaded " + record.FileName);
                    }
                    else if (operation.State == CoreWebView2DownloadState.Interrupted)
                    {
                        if (operation.InterruptReason == CoreWebView2DownloadInterruptReason.UserPaused)
                        {
                            // Pausing shows up as an interruption in this engine,
                            // but the operation stays alive so Resume can carry on.
                            record.State = "paused";
                            record.Operation = operation;
                        }
                        else
                        {
                            record.Operation = null;

                            if (operation.InterruptReason == CoreWebView2DownloadInterruptReason.UserCanceled)
                            {
                                record.State = "cancelled";
                            }
                            else
                            {
                                record.State = "failed";
                                record.Detail = InterruptText(operation.InterruptReason);
                            }

                            ChromeToast(record.FileName + ": " + record.StateText);
                        }
                    }

                    DownloadsManager.Current.SaveAndNotify();
                };

                ChromeToast("Downloading " + name);
            }
            catch
            {
                // Never leave a download hanging: let WebView2 take it.
                e.Handled = false;
            }
            finally
            {
                deferral.Complete();
            }
        }

        /// <summary>The size a download will be, or 0 while it is unknown.</summary>
        private static long TotalOf(CoreWebView2DownloadOperation operation)
        {
            if (operation == null) return 0;
            if (!operation.TotalBytesToReceive.HasValue) return 0;
            return (long)operation.TotalBytesToReceive.Value;
        }

        private static string InterruptText(CoreWebView2DownloadInterruptReason reason)
        {
            switch (reason)
            {
                case CoreWebView2DownloadInterruptReason.NetworkFailed:
                case CoreWebView2DownloadInterruptReason.NetworkDisconnected:
                case CoreWebView2DownloadInterruptReason.NetworkTimeout:
                case CoreWebView2DownloadInterruptReason.NetworkServerDown:
                    return "Network problem";
                case CoreWebView2DownloadInterruptReason.FileAccessDenied:
                    return "No permission to save there";
                case CoreWebView2DownloadInterruptReason.FileNoSpace:
                    return "Not enough disk space";
                case CoreWebView2DownloadInterruptReason.ServerUnauthorized:
                    return "Sign-in required";
                case CoreWebView2DownloadInterruptReason.ServerForbidden:
                    return "Server refused the download";
                case CoreWebView2DownloadInterruptReason.FileBlockedByPolicy:
                    return "Blocked by policy";
                default:
                    return reason.ToString();
            }
        }

        private string AskForSavePath(string folder, string name)
        {
            using (var dialog = new SaveFileDialog())
            {
                dialog.Title = "Save As";
                dialog.FileName = name;
                if (Directory.Exists(folder)) dialog.InitialDirectory = folder;
                dialog.OverwritePrompt = true;

                if (dialog.ShowDialog(this) != DialogResult.OK) return null;
                return dialog.FileName;
            }
        }

        /// <summary>A path that does not already exist, so nothing is overwritten.</summary>
        private static string UniquePath(string path)
        {
            if (!File.Exists(path)) return path;

            string folder = Path.GetDirectoryName(path);
            string name = Path.GetFileNameWithoutExtension(path);
            string extension = Path.GetExtension(path);

            for (int i = 1; i < 1000; i++)
            {
                string candidate = Path.Combine(folder, name + " (" + i + ")" + extension);
                if (!File.Exists(candidate)) return candidate;
            }

            return path;
        }

        private static string DefaultDownloadFolder()
        {
            try
            {
                string folder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                return folder;
            }
            catch
            {
                return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            }
        }

        /// <summary>Shows a short message in the toolbar's own toast.</summary>
        private void ChromeToast(string message)
        {
            if (!_chromeReady || _chromeWebView.CoreWebView2 == null) return;

            string literal = Json.Write(message ?? string.Empty);
            if (literal == null) return;

            _chromeWebView.CoreWebView2.ExecuteScriptAsync(
                "window.BraveAPI && window.BraveAPI.toast(" + literal + ");");
        }

        /// <summary>
        /// Redraws every open start page. Refreshing in place rather than
        /// reloading keeps the customize sheet open on the tab the user is
        /// working in, and still keeps any other start page in step.
        /// </summary>
        private void RefreshHomeTabs()
        {
            foreach (object page in _tabControl.TabPages)
            {
                var tab = page as BrowserTab;
                if (tab == null) continue;
                if (tab.CurrentUrl != "about:home") continue;

                tab.RefreshHome();
            }
        }

        // ── Settings page ─────────────────────────────────────────────────────

        /// <summary>
        /// Opens Toge's settings page, reusing the tab that already holds it so
        /// Ctrl+, twice does not leave two copies behind.
        /// </summary>
        public void OpenSettings()
        {
            foreach (object page in _tabControl.TabPages)
            {
                var existing = page as BrowserTab;
                if (existing != null && existing.IsSettingsPage)
                {
                    _tabControl.SelectedTab = existing;
                    return;
                }
            }

            AddNewTab(SettingsPage.Address);
        }

        /// <summary>
        /// Handles one message from the settings page. The page only reports a
        /// key and a value; deciding what that means stays here, and only keys
        /// Toge knows about are accepted.
        /// </summary>
        private void OnTabSettingsMessage(BrowserTab tab, string json)
        {
            if (string.IsNullOrEmpty(json)) return;

            string action = ExtractJsonField(json, "action");
            if (string.IsNullOrEmpty(action)) return;

            switch (action)
            {
                case "set":
                    {
                        string key = ExtractJsonField(json, "key");

                        // A wallpaper the user is moving away from may be one of
                        // our own files; tidy it once the change has gone through.
                        bool wallpaper = key == "wallpaperType" || key == "wallpaperValue";
                        string previous = wallpaper ? Settings.Current.WallpaperValue : null;

                        Settings.Current.Set(key, ExtractJsonValue(json, "value"));

                        if (wallpaper) HomeWallpaper.Tidy(previous);
                        break;
                    }

                // ── The start page's own customization ───────────────────────

                // Google is signed in through the profile, so the account page
                // is the only place the password ever has to be typed.
                case "google_account":
                    AddNewTab("https://accounts.google.com/");
                    break;

                case "home_fav_add":
                    {
                        var fields = Json.AsObject(Json.Parse(json));
                        string title = Json.GetString(fields, "title");
                        string newFavorite = Json.GetString(fields, "url");
                        bool added = FavoritesStore.Current.Add(title, newFavorite);
                        tab.NotifyHome(added ? "Shortcut added" : "That site is already pinned");
                        RefreshHomeTabs();
                        break;
                    }

                case "home_fav_edit":
                    {
                        var fields = Json.AsObject(Json.Parse(json));
                        bool saved = FavoritesStore.Current.Update(
                            Json.GetString(fields, "url"),
                            Json.GetString(fields, "title"),
                            Json.GetString(fields, "newUrl"));
                        tab.NotifyHome(saved ? "Shortcut updated" : "Could not save that shortcut");
                        RefreshHomeTabs();
                        break;
                    }

                case "home_fav_remove":
                    {
                        var fields = Json.AsObject(Json.Parse(json));
                        FavoritesStore.Current.Remove(Json.GetString(fields, "url"));
                        tab.NotifyHome("Shortcut removed");
                        RefreshHomeTabs();
                        break;
                    }

                case "home_fav_move":
                    {
                        var fields = Json.AsObject(Json.Parse(json));
                        FavoritesStore.Current.Move(
                            Json.GetInt(fields, "from", -1), Json.GetInt(fields, "to", -1));
                        RefreshHomeTabs();
                        break;
                    }

                case "home_open":
                    {
                        var fields = Json.AsObject(Json.Parse(json));
                        string address = Json.GetString(fields, "url");
                        if (!string.IsNullOrEmpty(address))
                        {
                            if (Json.GetBool(fields, "newTab", false)) AddNewTab(address);
                            else if (ActiveTab != null) ActiveTab.Navigate(address);
                        }
                        break;
                    }

                case "home_wallpaper_pick":
                    ChooseHomeWallpaper();
                    break;

                case "home_reset":
                    ResetHomeCustomization();
                    break;

                case "stats":
                    tab.RefreshSettingsStats();
                    break;

                case "permissions":
                    SendPermissions(tab);
                    break;

                case "perm_set":
                    {
                        var fields = Json.AsObject(Json.Parse(json));
                        SetPermission(
                            tab,
                            Json.GetString(fields, "origin"),
                            Json.GetString(fields, "kind"),
                            Json.GetString(fields, "state"));
                        break;
                    }

                case "perm_clear":
                    ClearPermissions(tab);
                    break;

                case "reset":
                    Settings.Current.ResetToDefaults();
                    tab.ReloadSettingsPage();
                    break;

                case "open":
                    ShowSettingsTarget(ExtractJsonField(json, "what"));
                    break;

                case "clear":
                    ClearData(ExtractJsonField(json, "what"), tab);
                    break;
            }
        }

        /// <summary>
        /// Asks for a picture and makes it the start page wallpaper. The file is
        /// copied into Toge's own wallpapers folder first, so the page keeps
        /// working after the original is moved or deleted.
        /// </summary>
        private void ChooseHomeWallpaper()
        {
            string previous = Settings.Current.WallpaperValue;

            using (var dialog = new OpenFileDialog())
            {
                dialog.Title = "Choose a wallpaper";
                dialog.Filter = "Pictures (*.jpg;*.jpeg;*.png;*.gif;*.webp;*.bmp)|*.jpg;*.jpeg;*.png;*.gif;*.webp;*.bmp" +
                    "|All files (*.*)|*.*";
                if (dialog.ShowDialog(this) != DialogResult.OK) return;

                string name = HomeWallpaper.Import(dialog.FileName);
                if (string.IsNullOrEmpty(name))
                {
                    MessageBox.Show("That picture could not be used as a wallpaper.",
                        "Toge", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                Settings.Current.Set("wallpaperValue", name);
                Settings.Current.Set("wallpaperType", "image");

                // A picture the wallpapers folder no longer needs is tidied away,
                // but a web address obviously stays where it is.
                if (!string.IsNullOrEmpty(previous) && previous != name &&
                    !previous.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    HomeWallpaper.Delete(previous);
                }
            }
        }

        /// <summary>Puts the start page's wallpaper and layout back to the defaults.</summary>
        private void ResetHomeCustomization()
        {
            string previous = Settings.Current.WallpaperValue;

            Settings.Current.Set("wallpaperType", "toge");
            Settings.Current.Set("wallpaperValue", "");
            Settings.Current.Set("wallpaperDim", 0);
            Settings.Current.Set("wallpaperBlur", 0);
            Settings.Current.Set("wallpaperText", "auto");
            Settings.Current.Set("showClock", true);
            Settings.Current.Set("showTiles", true);
            Settings.Current.Set("showSearch", true);

            HomeWallpaper.Tidy(previous);
        }

        /// <summary>Opens the start page and its customize sheet in the active tab.</summary>
        private void CustomizeHomePage()
        {
            var tab = ActiveTab;
            if (tab == null) return;

            if (tab.CurrentUrl == "about:home")
            {
                tab.OpenHomeCustomizer();
                return;
            }

            // The page is only ready to be told once it has been built, so the
            // sheet is opened from the tab when the load finishes.
            tab.LoadHomePageWithCustomizer();
        }

        private void ShowSettingsTarget(string what)
        {
            switch (what)
            {
                case "import": ShowImport(); break;
                case "bookmarks": ShowBookmarks(); break;
                case "history": ShowHistory(); break;
                case "shields": ShowBraveShields(); break;
                case "defaultbrowser": SetAsDefaultBrowser(); break;
            }
        }

        /// <summary>
        /// Registers Toge with Windows and opens the page where the user makes
        /// the choice. Windows will not let an app take the job silently, so the
        /// best Toge can do is put itself on the list and say so.
        /// </summary>
        private void SetAsDefaultBrowser()
        {
            bool registered = DefaultBrowser.Register();
            DefaultBrowser.OpenWindowsSettings();

            if (!registered)
            {
                MessageBox.Show(this,
                    "Toge could not register itself with Windows.\n\n" +
                    "You can still choose a browser from the Windows Settings page that just opened.",
                    "Default browser",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// Runs one of the "Clear" buttons on the settings page. Cookies and site
        /// data are removed through WebView2's own profile API, so the profile on
        /// disk really is emptied rather than just the app's own lists.
        /// </summary>
        private async void ClearData(string what, BrowserTab tab)
        {
            string message;

            switch (what)
            {
                case "history":
                    _historyManager.ClearHistory();
                    message = "Browsing history cleared.";
                    break;

                case "searches":
                    _historyManager.ClearSearches();
                    message = "Search history cleared.";
                    break;

                case "cookies":
                    await ClearProfileDataAsync(
                        CoreWebView2BrowsingDataKinds.Cookies |
                        CoreWebView2BrowsingDataKinds.LocalStorage |
                        CoreWebView2BrowsingDataKinds.IndexedDb |
                        CoreWebView2BrowsingDataKinds.WebSql |
                        CoreWebView2BrowsingDataKinds.FileSystems |
                        CoreWebView2BrowsingDataKinds.ServiceWorkers);
                    message = "Cookies and site data cleared. You are signed out everywhere.";
                    break;

                case "cache":
                    await ClearProfileDataAsync(
                        CoreWebView2BrowsingDataKinds.DiskCache |
                        CoreWebView2BrowsingDataKinds.CacheStorage);
                    message = "Cache cleared.";
                    break;

                default:
                    return;
            }

            if (tab != null) tab.NotifySettingsPage(message);
        }

        private async Task ClearProfileDataAsync(CoreWebView2BrowsingDataKinds kinds)
        {
            CoreWebView2Profile profile = ProfileForData();
            if (profile == null) return;

            try
            {
                await profile.ClearBrowsingDataAsync(kinds);
            }
            catch
            {
                // A profile that cannot be reached leaves the data in place; the
                // page still says what happened.
            }
        }

        private CoreWebView2Profile ProfileForData()
        {
            // Prefer the page the user is looking at: in a private window that
            // is the in-private profile, and clearing it must not touch the
            // normal one.
            var tab = ActiveTab;
            if (tab != null && tab.WebView != null && tab.WebView.CoreWebView2 != null)
            {
                return tab.WebView.CoreWebView2.Profile;
            }

            if (_chromeWebView != null && _chromeWebView.CoreWebView2 != null)
            {
                return _chromeWebView.CoreWebView2.Profile;
            }

            return null;
        }

        private async void ShowPageSource()
        {
            var tab = ActiveTab;
            if (tab == null) return;

            string url = tab.CurrentUrl;
            string html = await tab.GetSourceHtmlAsync();

            using (var form = new SourceViewerForm(url, html))
            {
                form.ShowDialog(this);
            }
        }

        /// <summary>
        /// Screen rectangle of the chrome's ⋯ button, used to tell a normal
        /// outside click apart from a click that toggles the menu shut.
        /// </summary>
        private Rectangle MenuButtonScreenRect()
        {
            Rectangle local = new Rectangle(
                Width - Dpi.Px(44),
                Dpi.Px(44),
                Dpi.Px(40),
                Dpi.Px(36));
            return RectangleToScreen(local);
        }

        private void InstallMenuMouseHook()
        {
            if (_menuMouseHook != IntPtr.Zero) return;
            _menuMouseHookProc = MenuMouseHookProc;
            _menuMouseHook = SetWindowsHookEx(WH_MOUSE_LL, _menuMouseHookProc, IntPtr.Zero, 0);
        }

        private void RemoveMenuMouseHook()
        {
            if (_menuMouseHook == IntPtr.Zero) return;

            IntPtr hook = _menuMouseHook;
            _menuMouseHook = IntPtr.Zero;

            // Unhooking from inside the hook callback can deadlock, so defer it.
            try
            {
                BeginInvoke((MethodInvoker)delegate
                {
                    try { UnhookWindowsHookEx(hook); }
                    catch { }
                });
            }
            catch
            {
                try { UnhookWindowsHookEx(hook); }
                catch { }
            }
        }

        /// <summary>
        /// True when the point is inside the menu or any of its open submenus.
        /// Nested dropdowns are separate windows, so their bounds must be checked
        /// too, otherwise clicking a submenu item would be treated as "outside".
        /// </summary>
        private static bool IsPointInsideMenu(ToolStripDropDown menu, Point screenPoint)
        {
            if (menu == null || !menu.Visible) return false;
            if (menu.Bounds.Contains(screenPoint)) return true;

            foreach (ToolStripItem item in menu.Items)
            {
                var menuItem = item as ToolStripMenuItem;
                if (menuItem != null && menuItem.HasDropDownItems && menuItem.DropDown.Visible &&
                    IsPointInsideMenu(menuItem.DropDown, screenPoint))
                {
                    return true;
                }
            }

            return false;
        }

        private IntPtr MenuMouseHookProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && _openMenu != null)
            {
                int message = wParam.ToInt32();
                if (message == WM_LBUTTONDOWN || message == WM_RBUTTONDOWN || message == WM_MBUTTONDOWN)
                {
                    var info = (MouseHookInfo)Marshal.PtrToStructure(lParam, typeof(MouseHookInfo));
                    var point = new Point(info.Pt.X, info.Pt.Y);

                    if (!IsPointInsideMenu(_openMenu, point))
                    {
                        // If this press was on the ⋯ button, the chrome will also
                        // report a click; remember that so we don't reopen there.
                        _menuDismissedByButton = MenuButtonScreenRect().Contains(point);
                        _menuDismissedAtUtc = DateTime.UtcNow;
                        CloseOpenMenu();
                    }
                }
            }

            return CallNextHookEx(_menuMouseHook, nCode, wParam, lParam);
        }

        private void CloseOpenMenu()
        {
            var menu = _openMenu;
            _openMenu = null;
            RemoveMenuMouseHook();

            if (menu != null)
            {
                try { menu.Close(ToolStripDropDownCloseReason.AppClicked); }
                catch { }
            }
        }

        private void ShowBraveMenu()
        {
            // Clicking ⋯ while its own menu is open closes it.
            if (_openMenu != null)
            {
                _menuDismissedByButton = true;
                _menuDismissedAtUtc = DateTime.UtcNow;
                CloseOpenMenu();
                return;
            }

            // A press on ⋯ that dismissed the menu also counts as a click, which
            // arrives here right after; ignore it so the menu stays closed.
            bool justDismissed = _menuDismissedByButton &&
                (DateTime.UtcNow - _menuDismissedAtUtc).TotalMilliseconds < 500;
            _menuDismissedByButton = false;
            if (justDismissed) return;

            var menu = new ContextMenuStrip();
            menu.BackColor = Theme.CardBackground;
            menu.ForeColor = Theme.TextPrimary;
            menu.Font = new Font("Segoe UI", 9.5F);
            menu.ShowImageMargin = false;
            menu.ShowCheckMargin = false;
            menu.Padding = new Padding(5, 6, 5, 6);
            menu.Renderer = new ToolStripProfessionalRenderer(new TogeMenuColors());

            var newTabItem = new ToolStripMenuItem("New Tab", null, delegate { AddNewTab(); });
            newTabItem.ShortcutKeyDisplayString = "Ctrl+T";

            var newWindowItem = new ToolStripMenuItem("New Window", null, delegate { NewWindow(false); });
            newWindowItem.ShortcutKeyDisplayString = "Ctrl+N";

            var newPrivateItem = new ToolStripMenuItem("New Private Window", null, delegate { NewWindow(true); });
            newPrivateItem.ShortcutKeyDisplayString = "Ctrl+Shift+N";

            var reopenItem = new ToolStripMenuItem("Reopen Closed Tab", null, delegate { ReopenClosedTab(); });
            reopenItem.ShortcutKeyDisplayString = "Ctrl+Shift+T";

            var reopenWindowItem = new ToolStripMenuItem("Reopen Closed Window", null, delegate
            {
                ReopenClosedWindow();
            });
            reopenWindowItem.Enabled = ClosedWindows.Count > 0;

            var findItem = new ToolStripMenuItem("Find on Page\u2026", null, delegate
            {
                if (ActiveTab != null) ActiveTab.ToggleFind();
            });
            findItem.ShortcutKeyDisplayString = "Ctrl+F";

            var downloadsItem = new ToolStripMenuItem("Downloads", null, delegate { ShowDownloads(); });
            downloadsItem.ShortcutKeyDisplayString = "Ctrl+J";

            var bookmarksItem = new ToolStripMenuItem("Bookmark This Page\u2026", null, delegate { ToggleBookmarkCurrent(); });
            bookmarksItem.ShortcutKeyDisplayString = "Ctrl+D";

            var historyItem = new ToolStripMenuItem("History", null, delegate { ShowHistory(); });
            historyItem.ShortcutKeyDisplayString = "Ctrl+H";

            var allBookmarksItem = new ToolStripMenuItem("Show All Bookmarks", null, delegate { ShowBookmarks(); });
            allBookmarksItem.ShortcutKeyDisplayString = "Ctrl+Shift+O";

            var bookmarksBarItem = new ToolStripMenuItem("Show Bookmarks Bar", null, delegate
            {
                Settings.Current.Set("bookmarksBar", !Settings.Current.ShowBookmarksBar);
            });
            bookmarksBarItem.ShortcutKeyDisplayString = "Ctrl+Shift+B";

            var customizeHomeItem = new ToolStripMenuItem("Customize Home Page\u2026", null,
                delegate { CustomizeHomePage(); });

            var sourceItem = new ToolStripMenuItem("View Page Source", null, delegate { ShowPageSource(); });
            sourceItem.ShortcutKeyDisplayString = "Ctrl+U";

            var shieldItem = new ToolStripMenuItem("Toge Shields", null, delegate { ShowBraveShields(); });

            var appearanceItem = new ToolStripMenuItem("Appearance");
            var lightItem = new ToolStripMenuItem("Light", null, delegate { Settings.Current.Set("theme", "light"); });
            var darkItem = new ToolStripMenuItem("Dark", null, delegate { Settings.Current.Set("theme", "dark"); });
            var systemItem = new ToolStripMenuItem("Use System Theme", null, delegate { Settings.Current.Set("theme", "system"); });
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

                // This menu has no check margin, so the label carries the state.
                bool bar = Settings.Current.ShowBookmarksBar;
                bookmarksBarItem.Text = bar ? "Hide Bookmarks Bar" : "Show Bookmarks Bar";

                reopenItem.Enabled = _closedTabs.Count > 0;
            };

            var aboutItem = new ToolStripMenuItem("About Toge", null, delegate { ShowAbout(); });
            var settingsItem = new ToolStripMenuItem("Settings\u2026", null, delegate { OpenSettings(); });
            settingsItem.ShortcutKeyDisplayString = "Ctrl+,";
            var importItem = new ToolStripMenuItem("Import Browser Data\u2026", null, delegate { ShowImport(); });
            var quitItem = new ToolStripMenuItem("Quit Toge", null, delegate { Close(); });

            menu.Items.Add(newTabItem);
            menu.Items.Add(newWindowItem);
            menu.Items.Add(newPrivateItem);
            menu.Items.Add(reopenItem);
            menu.Items.Add(reopenWindowItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(findItem);
            menu.Items.Add(downloadsItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(bookmarksItem);
            menu.Items.Add(allBookmarksItem);
            menu.Items.Add(bookmarksBarItem);
            menu.Items.Add(historyItem);
            menu.Items.Add(sourceItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(shieldItem);
            menu.Items.Add(customizeHomeItem);
            menu.Items.Add(appearanceItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(settingsItem);
            menu.Items.Add(importItem);
            menu.Items.Add(aboutItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(quitItem);

            menu.Closed += delegate
            {
                if (ReferenceEquals(_openMenu, menu)) _openMenu = null;
                RemoveMenuMouseHook();
            };

            _openMenu = menu;
            InstallMenuMouseHook();
            menu.Show(PointToScreen(new Point(Width - Dpi.Px(208), ChromePx() + Dpi.Px(4))));
        }

        /// <summary>
        /// Windows hands a frameless window the whole monitor when it maximizes,
        /// which hides the taskbar. Rewriting MAXSIZE and MAXPOSITION to the
        /// working area of the nearest monitor keeps the taskbar where it was.
        /// </summary>
        private void ClampMaximizedBounds(IntPtr minMaxInfoPointer)
        {
            try
            {
                IntPtr monitor = MonitorFromWindow(Handle, MONITOR_DEFAULTTONEAREST);
                if (monitor == IntPtr.Zero) return;

                var monitorInfo = new MonitorInfo();
                monitorInfo.Size = Marshal.SizeOf(typeof(MonitorInfo));
                if (!GetMonitorInfo(monitor, ref monitorInfo)) return;

                var info = (MinMaxInfo)Marshal.PtrToStructure(minMaxInfoPointer, typeof(MinMaxInfo));
                info.MaxPosition.X = monitorInfo.Work.Left;
                info.MaxPosition.Y = monitorInfo.Work.Top;
                info.MaxSize.X = monitorInfo.Work.Right - monitorInfo.Work.Left;
                info.MaxSize.Y = monitorInfo.Work.Bottom - monitorInfo.Work.Top;
                Marshal.StructureToPtr(info, minMaxInfoPointer, false);
            }
            catch
            {
                // Keeping the default maximize is better than failing to open.
            }
        }

        /// <summary>
        /// A normal window that closes while it is the only one left has ended
        /// the session in an orderly way, so the next run opens the start page
        /// instead of offering to restore what a crash "left behind".
        /// </summary>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            int remaining = 0;
            foreach (var window in Windows)
            {
                if (window == this || window.IsDisposed) continue;
                if (!window.IsPrivate) remaining++;
            }

            if (remaining == 0) SessionStore.MarkClean();

            // A window closed while another one stays open can be brought back
            // with Ctrl+Shift+T, the way browsers do it.
            if (!_privateMode && Windows.Count > 1 && !e.Cancel) RememberClosedWindow();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            Theme.Changed -= ApplyTheme;
            Settings.Changed -= OnSettingsChanged;
            if (_bookmarkManager != null) _bookmarkManager.Changed -= OnBookmarksChanged;
            DownloadsManager.Current.Changed -= OnDownloadsChanged;

            Windows.Remove(this);
            try { Application.RemoveMessageFilter(_shortcuts); }
            catch { }

            base.OnFormClosed(e);
        }

        // Native Resizing
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_GETMINMAXINFO)
            {
                base.WndProc(ref m);

                // A window with no title bar is maximized to the whole monitor,
                // which swallows the taskbar. Clamp it to the working area of
                // the monitor the window is on instead, so the taskbar and any
                // docked toolbars stay visible.
                ClampMaximizedBounds(m.LParam);
                return;
            }

            if (m.Msg == WM_DPICHANGED)
            {
                base.WndProc(ref m);

                // The window moved to a monitor with a different scale factor.
                // Re-derive the pixel scale, adopt the rectangle Windows
                // suggests and re-measure the fixed-size pieces so the chrome
                // stays exactly 84 CSS pixels tall (and therefore crisp).
                try
                {
                    int newDpi = m.WParam.ToInt32() & 0xFFFF;
                    if (newDpi > 0) Dpi.Scale = newDpi / 96f;

                    var suggested = (NativeRect)Marshal.PtrToStructure(m.LParam, typeof(NativeRect));
                    Bounds = new Rectangle(
                        suggested.Left,
                        suggested.Top,
                        suggested.Right - suggested.Left,
                        suggested.Bottom - suggested.Top);

                    if (_chromeWebView != null)
                    {
                        _chromeWebView.Height = ChromePx();
                    }
                    LayoutTabs();
                }
                catch
                {
                    // A failed rescale must not take down the window.
                }
                return;
            }

            if (m.Msg == WM_NCHITTEST)
            {
                base.WndProc(ref m);
                Point pos = PointToClient(new Point(m.LParam.ToInt32()));
                int margin = Dpi.Px(BorderResizeMargin);

                if (pos.X <= margin && pos.Y <= margin)
                    m.Result = (IntPtr)HTTOPLEFT;
                else if (pos.X >= ClientSize.Width - margin && pos.Y <= margin)
                    m.Result = (IntPtr)HTTOPRIGHT;
                else if (pos.X <= margin && pos.Y >= ClientSize.Height - margin)
                    m.Result = (IntPtr)HTBOTTOMLEFT;
                else if (pos.X >= ClientSize.Width - margin && pos.Y >= ClientSize.Height - margin)
                    m.Result = (IntPtr)HTBOTTOMRIGHT;
                else if (pos.X <= margin)
                    m.Result = (IntPtr)HTLEFT;
                else if (pos.X >= ClientSize.Width - margin)
                    m.Result = (IntPtr)HTRIGHT;
                else if (pos.Y <= margin)
                    m.Result = (IntPtr)HTTOP;
                else if (pos.Y >= ClientSize.Height - margin)
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

        private static bool ExtractJsonBool(string json, string field)
        {
            string pattern = "\"" + field + "\"\\s*:\\s*(true|false)";
            var match = System.Text.RegularExpressions.Regex.Match(json, pattern);
            return match.Success && match.Groups[1].Value == "true";
        }

        /// <summary>
        /// Reads a message value of any type, because the settings page sends
        /// text, booleans and numbers through the same field. Returns null when
        /// the field is missing, which the settings store treats as no change.
        /// </summary>
        private static object ExtractJsonValue(string json, string field)
        {
            string pattern = "\"" + field + "\"\\s*:\\s*(\"([^\"]*)\"|true|false|-?[0-9]+(?:\\.[0-9]+)?)";
            var match = System.Text.RegularExpressions.Regex.Match(json, pattern);
            if (!match.Success) return null;

            string token = match.Groups[1].Value;
            if (token == "true") return true;
            if (token == "false") return false;
            if (token.StartsWith("\"")) return match.Groups[2].Value;

            int parsedInt;
            if (int.TryParse(token, out parsedInt)) return parsedInt;

            double parsedDouble;
            if (double.TryParse(token, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out parsedDouble))
            {
                return parsedDouble;
            }

            return null;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeRect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        /// <summary>The WM_GETMINMAXINFO payload Windows fills in before maximizing.</summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct MinMaxInfo
        {
            public NativePoint Reserved;
            public NativePoint MaxSize;
            public NativePoint MaxPosition;
            public NativePoint MinTrackSize;
            public NativePoint MaxTrackSize;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MonitorInfo
        {
            public int Size;
            public NativeRect Monitor;
            public NativeRect Work;
            public int Flags;
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
            // WM_MOUSEWHEEL, caught here because the page never passes it on.
            if (m.Msg == 0x020A) return _form.HandleWheelZoom(m.WParam, m.LParam);

            // WM_KEYDOWN / WM_SYSKEYDOWN
            if (m.Msg != 0x0100 && m.Msg != 0x0104) return false;
            return _form.HandleShortcutKey(m.WParam.ToInt32());
        }
    }

    internal class TogeMenuColors : ProfessionalColorTable
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
