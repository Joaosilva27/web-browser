using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
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
        private bool _chromeReady;
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

        public MainForm()
        {
            Theme.LoadPreference();
            Settings.Current.ApplyThemePreference();
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

                // The start page draws the search engine, clock and tiles from
                // the settings, so it has to be redrawn to show a change.
                if (tab.CurrentUrl == "about:home")
                {
                    tab.LoadHomePage();
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
                    case "settings":
                        OpenSettings();
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
            tab.IconChanged += OnTabIconChanged;
            tab.NewWindowRequested += OnTabNewWindowRequested;
            tab.WindowCloseRequested += OnTabWindowCloseRequested;
            tab.SettingsMessage += OnTabSettingsMessage;
            tab.FullScreenChanged += OnTabFullScreenChanged;

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
            SyncPageFullScreen();
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

                tabItems.Add(string.Format("{{\"title\":\"{0}\",\"url\":\"{1}\",\"icon\":\"{2}\"}}",
                    EscapeJs(title), EscapeJs(url), EscapeJs(icon)));
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

        private void OnTabIconChanged(BrowserTab tab)
        {
            SyncTabs();
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

                    await popup.View.EnsureCoreWebView2Async(await BrowserEnvironment.GetAsync());
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
                    case 0x4F: if (shift) action = "bookmarks"; break;             // Ctrl+Shift+O
                    case 0x42: if (shift) action = "bookmarks_bar"; break;         // Ctrl+Shift+B
                    case 0xBC: action = "settings"; break;                         // Ctrl+,
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
                case "bookmarks": ShowBookmarks(); break;
                case "bookmarks_bar": Settings.Current.Set("bookmarksBar", !Settings.Current.ShowBookmarksBar); break;
                case "source": ShowPageSource(); break;
                case "settings": OpenSettings(); break;
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

        /// <summary>Re-renders any tab that is sitting on the start page.</summary>
        private void RefreshHomeTabs()
        {
            foreach (object page in _tabControl.TabPages)
            {
                var tab = page as BrowserTab;
                if (tab != null && tab.CurrentUrl == "about:home")
                {
                    tab.LoadHomePage();
                }
            }
        }

        // ── Settings page ─────────────────────────────────────────────────────

        /// <summary>
        /// Opens Aura's settings page, reusing the tab that already holds it so
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
        /// Aura knows about are accepted.
        /// </summary>
        private void OnTabSettingsMessage(BrowserTab tab, string json)
        {
            if (string.IsNullOrEmpty(json)) return;

            string action = ExtractJsonField(json, "action");
            if (string.IsNullOrEmpty(action)) return;

            switch (action)
            {
                case "set":
                    Settings.Current.Set(ExtractJsonField(json, "key"), ExtractJsonValue(json, "value"));
                    break;

                case "stats":
                    tab.RefreshSettingsStats();
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

        private void ShowSettingsTarget(string what)
        {
            switch (what)
            {
                case "import": ShowImport(); break;
                case "bookmarks": ShowBookmarks(); break;
                case "history": ShowHistory(); break;
                case "shields": ShowBraveShields(); break;
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
            if (_chromeWebView != null && _chromeWebView.CoreWebView2 != null)
            {
                return _chromeWebView.CoreWebView2.Profile;
            }

            var tab = ActiveTab;
            if (tab != null && tab.WebView != null && tab.WebView.CoreWebView2 != null)
            {
                return tab.WebView.CoreWebView2.Profile;
            }

            return null;
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
            menu.Renderer = new ToolStripProfessionalRenderer(new AuraMenuColors());

            var newTabItem = new ToolStripMenuItem("New Tab", null, delegate { AddNewTab(); });
            newTabItem.ShortcutKeyDisplayString = "Ctrl+T";

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

            var sourceItem = new ToolStripMenuItem("View Page Source", null, delegate { ShowPageSource(); });
            sourceItem.ShortcutKeyDisplayString = "Ctrl+U";

            var shieldItem = new ToolStripMenuItem("Aura Shields", null, delegate { ShowBraveShields(); });

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
            };

            var aboutItem = new ToolStripMenuItem("About Aura", null, delegate { ShowAbout(); });
            var settingsItem = new ToolStripMenuItem("Settings\u2026", null, delegate { OpenSettings(); });
            settingsItem.ShortcutKeyDisplayString = "Ctrl+,";
            var importItem = new ToolStripMenuItem("Import Browser Data\u2026", null, delegate { ShowImport(); });
            var quitItem = new ToolStripMenuItem("Quit Aura", null, delegate { Close(); });

            menu.Items.Add(newTabItem);
            menu.Items.Add(bookmarksItem);
            menu.Items.Add(allBookmarksItem);
            menu.Items.Add(bookmarksBarItem);
            menu.Items.Add(historyItem);
            menu.Items.Add(sourceItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(shieldItem);
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
