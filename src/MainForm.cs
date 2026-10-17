using System;
using System.Drawing;
using System.Windows.Forms;

namespace SimpleBrowser
{
    public class MainForm : Form
    {
        private TabControl _tabControl;
        private ToolStrip _navBar;
        private FlowLayoutPanel _bookmarksBar;
        private StatusStrip _statusBar;
        private ToolStripStatusLabel _statusLabel;

        private ToolStripButton _btnBack;
        private ToolStripButton _btnForward;
        private ToolStripButton _btnRefresh;
        private ToolStripButton _btnHome;
        private ToolStripTextBox _txtUrl;
        private ToolStripButton _btnGo;
        private ToolStripButton _btnStar;
        private ToolStripButton _btnNewTab;
        private ToolStripButton _btnCloseTab;
        private ToolStripButton _btnHistory;
        private ToolStripButton _btnSource;
        private ToolStripButton _btnAbout;

        private readonly BookmarkManager _bookmarkManager;
        private readonly HistoryManager _historyManager;

        public MainForm()
        {
            _bookmarkManager = new BookmarkManager();
            _historyManager = new HistoryManager();

            InitializeUi();
            AddNewTab();
            RebuildBookmarksBar();
        }

        private void InitializeUi()
        {
            Text = "Aura Web Browser";
            Size = new Size(1180, 780);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9F);
            KeyPreview = true;
            KeyDown += OnFormKeyDown;

            // Navigation Bar
            _navBar = new ToolStrip();
            _navBar.GripStyle = ToolStripGripStyle.Hidden;
            _navBar.Padding = new Padding(6, 4, 6, 4);
            _navBar.ImageScalingSize = new Size(18, 18);
            _navBar.BackColor = Color.FromArgb(240, 243, 246);

            _btnBack = new ToolStripButton("◀");
            _btnBack.ToolTipText = "Back (Alt+Left)";
            _btnBack.Click += delegate { if (ActiveTab != null && ActiveTab.Browser != null) ActiveTab.Browser.GoBack(); };

            _btnForward = new ToolStripButton("▶");
            _btnForward.ToolTipText = "Forward (Alt+Right)";
            _btnForward.Click += delegate { if (ActiveTab != null && ActiveTab.Browser != null) ActiveTab.Browser.GoForward(); };

            _btnRefresh = new ToolStripButton("🔄");
            _btnRefresh.ToolTipText = "Reload (Ctrl+R or F5)";
            _btnRefresh.Click += delegate { if (ActiveTab != null && ActiveTab.Browser != null) ActiveTab.Browser.Refresh(); };

            _btnHome = new ToolStripButton("🏠");
            _btnHome.ToolTipText = "Home Page";
            _btnHome.Click += delegate { if (ActiveTab != null) ActiveTab.LoadHomePage(); };

            _txtUrl = new ToolStripTextBox();
            _txtUrl.Size = new Size(500, 28);
            _txtUrl.Font = new Font("Segoe UI", 9.5F);
            _txtUrl.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    NavigateToInput();
                }
            };
            _txtUrl.Enter += delegate { _txtUrl.SelectAll(); };

            _btnGo = new ToolStripButton("➜");
            _btnGo.ToolTipText = "Go";
            _btnGo.Click += delegate { NavigateToInput(); };

            _btnStar = new ToolStripButton("☆");
            _btnStar.ToolTipText = "Bookmark this tab (Ctrl+D)";
            _btnStar.Click += delegate { ToggleBookmarkCurrent(); };

            _btnNewTab = new ToolStripButton("➕ New Tab");
            _btnNewTab.ToolTipText = "Open New Tab (Ctrl+T)";
            _btnNewTab.Click += delegate { AddNewTab(); };

            _btnCloseTab = new ToolStripButton("✖ Close");
            _btnCloseTab.ToolTipText = "Close Active Tab (Ctrl+W)";
            _btnCloseTab.Click += delegate { CloseActiveTab(); };

            _btnHistory = new ToolStripButton("🕒 History");
            _btnHistory.ToolTipText = "Browsing History (Ctrl+H)";
            _btnHistory.Click += delegate { ShowHistory(); };

            _btnSource = new ToolStripButton("</> Source");
            _btnSource.ToolTipText = "View Page Source (Ctrl+U)";
            _btnSource.Click += delegate { ShowPageSource(); };

            _btnAbout = new ToolStripButton("ℹ About");
            _btnAbout.ToolTipText = "About Aura Browser";
            _btnAbout.Click += delegate { ShowAbout(); };

            _navBar.Items.Add(_btnBack);
            _navBar.Items.Add(_btnForward);
            _navBar.Items.Add(_btnRefresh);
            _navBar.Items.Add(_btnHome);
            _navBar.Items.Add(new ToolStripSeparator());
            _navBar.Items.Add(_txtUrl);
            _navBar.Items.Add(_btnGo);
            _navBar.Items.Add(_btnStar);
            _navBar.Items.Add(new ToolStripSeparator());
            _navBar.Items.Add(_btnNewTab);
            _navBar.Items.Add(_btnCloseTab);
            _navBar.Items.Add(new ToolStripSeparator());
            _navBar.Items.Add(_btnHistory);
            _navBar.Items.Add(_btnSource);
            _navBar.Items.Add(_btnAbout);

            // Bookmarks Bar
            _bookmarksBar = new FlowLayoutPanel();
            _bookmarksBar.Dock = DockStyle.Top;
            _bookmarksBar.Height = 28;
            _bookmarksBar.BackColor = Color.FromArgb(248, 249, 250);
            _bookmarksBar.Padding = new Padding(6, 2, 6, 2);
            _bookmarksBar.WrapContents = false;
            _bookmarksBar.AutoScroll = false;

            // Tab Control
            _tabControl = new TabControl();
            _tabControl.Dock = DockStyle.Fill;
            _tabControl.SelectedIndexChanged += delegate { UpdateControls(); };

            // Status Bar
            _statusBar = new StatusStrip();
            _statusBar.BackColor = Color.FromArgb(240, 243, 246);
            _statusLabel = new ToolStripStatusLabel("Ready");
            _statusLabel.Spring = true;
            _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
            _statusBar.Items.Add(_statusLabel);

            // Add to form in order: bottom status, fill tabcontrol, top bookmarks, top navbar
            Controls.Add(_tabControl);
            Controls.Add(_bookmarksBar);
            Controls.Add(_navBar);
            Controls.Add(_statusBar);

            Resize += delegate
            {
                // Responsive URL bar width
                int otherWidth = 460;
                int newWidth = Width - otherWidth;
                if (newWidth > 200)
                {
                    _txtUrl.Size = new Size(newWidth, 26);
                }
            };
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
            tab.StatusTextChanged += OnTabStatusTextChanged;

            _tabControl.TabPages.Add(tab);
            _tabControl.SelectedTab = tab;
            UpdateControls();
        }

        public void CloseActiveTab()
        {
            if (_tabControl.TabPages.Count > 1 && ActiveTab != null)
            {
                var tab = ActiveTab;
                int index = _tabControl.SelectedIndex;
                _tabControl.TabPages.Remove(tab);
                tab.Dispose();
                _tabControl.SelectedIndex = Math.Max(0, index - 1);
            }
            else if (_tabControl.TabPages.Count == 1 && ActiveTab != null)
            {
                ActiveTab.LoadHomePage();
            }
        }

        private void NavigateToInput()
        {
            string url = _txtUrl.Text;
            if (ActiveTab != null)
            {
                ActiveTab.Navigate(url);
            }
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
                _btnStar.Text = "☆";
                _statusLabel.Text = "Removed bookmark: " + url;
            }
            else
            {
                _bookmarkManager.AddBookmark(title, url);
                _btnStar.Text = "★";
                _statusLabel.Text = "Bookmarked: " + title;
            }
            RebuildBookmarksBar();
        }

        private void RebuildBookmarksBar()
        {
            _bookmarksBar.Controls.Clear();

            var lbl = new Label();
            lbl.Text = "Bookmarks:";
            lbl.AutoSize = true;
            lbl.ForeColor = Color.DimGray;
            lbl.Margin = new Padding(2, 4, 6, 2);
            _bookmarksBar.Controls.Add(lbl);

            foreach (var b in _bookmarkManager.GetBookmarks())
            {
                var btn = new Button();
                btn.Text = b.Title;
                btn.Tag = b.Url;
                btn.AutoSize = true;
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderSize = 0;
                btn.BackColor = Color.FromArgb(235, 238, 242);
                btn.ForeColor = Color.FromArgb(33, 37, 41);
                btn.Cursor = Cursors.Hand;
                btn.Margin = new Padding(2, 1, 2, 1);
                btn.Height = 22;

                var currentUrl = b.Url;
                btn.Click += delegate
                {
                    if (ActiveTab != null)
                    {
                        ActiveTab.Navigate(currentUrl);
                    }
                };

                // Context menu to delete bookmark
                var contextMenu = new ContextMenuStrip();
                var deleteItem = new ToolStripMenuItem("Delete Bookmark");
                deleteItem.Click += delegate
                {
                    _bookmarkManager.RemoveBookmark(currentUrl);
                    RebuildBookmarksBar();
                };
                contextMenu.Items.Add(deleteItem);
                btn.ContextMenuStrip = contextMenu;

                _bookmarksBar.Controls.Add(btn);
            }
        }

        private void UpdateControls()
        {
            var tab = ActiveTab;
            if (tab != null)
            {
                _btnBack.Enabled = tab.CanGoBack;
                _btnForward.Enabled = tab.CanGoForward;

                string displayUrl = tab.CurrentUrl;
                if (displayUrl == "about:home") displayUrl = "";
                _txtUrl.Text = displayUrl;

                bool isBookmarked = !string.IsNullOrEmpty(displayUrl) && _bookmarkManager.IsBookmarked(displayUrl);
                _btnStar.Text = isBookmarked ? "★" : "☆";

                Text = (string.IsNullOrEmpty(tab.Text) ? "Aura Browser" : tab.Text + " - Aura Browser");
            }
            else
            {
                _btnBack.Enabled = false;
                _btnForward.Enabled = false;
                _txtUrl.Text = "";
                _btnStar.Text = "☆";
                Text = "Aura Web Browser";
            }
        }

        private void OnTabUrlChanged(BrowserTab tab, string url)
        {
            if (tab == ActiveTab)
            {
                string displayUrl = url == "about:home" ? "" : url;
                _txtUrl.Text = displayUrl;
                _btnStar.Text = _bookmarkManager.IsBookmarked(url) ? "★" : "☆";
            }
        }

        private void OnTabTitleChanged(BrowserTab tab, string title)
        {
            if (tab == ActiveTab)
            {
                Text = title + " - Aura Browser";
            }
            if (!string.IsNullOrEmpty(tab.CurrentUrl) && tab.CurrentUrl != "about:home")
            {
                _historyManager.AddEntry(title, tab.CurrentUrl);
            }
        }

        private void OnTabStateChanged(BrowserTab tab)
        {
            if (tab == ActiveTab)
            {
                _btnBack.Enabled = tab.CanGoBack;
                _btnForward.Enabled = tab.CanGoForward;
            }
        }

        private void OnTabStatusTextChanged(BrowserTab tab, string status)
        {
            if (tab == ActiveTab)
            {
                _statusLabel.Text = string.IsNullOrEmpty(status) ? "Ready" : status;
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

        private void ShowAbout()
        {
            MessageBox.Show(
                "Aura Web Browser v1.0\n\n" +
                "Built from scratch in C# with WinForms & .NET.\n\n" +
                "Features:\n" +
                "• Tabbed browsing (Ctrl+T, Ctrl+W)\n" +
                "• Unified Smart Address & Search bar (Google)\n" +
                "• Fast Bookmark Manager & Bookmarks bar\n" +
                "• Browsing History with search and filter\n" +
                "• Page Source code inspector (Ctrl+U)\n" +
                "• Modern Dark Start Page\n\n" +
                "Created for fast, lightweight browsing.",
                "About Aura Browser",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void OnFormKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.T)
            {
                e.SuppressKeyPress = true;
                AddNewTab();
            }
            else if (e.Control && e.KeyCode == Keys.W)
            {
                e.SuppressKeyPress = true;
                CloseActiveTab();
            }
            else if (e.Control && e.KeyCode == Keys.R || e.KeyCode == Keys.F5)
            {
                e.SuppressKeyPress = true;
                if (ActiveTab != null && ActiveTab.Browser != null) ActiveTab.Browser.Refresh();
            }
            else if (e.Control && e.KeyCode == Keys.L)
            {
                e.SuppressKeyPress = true;
                _txtUrl.Focus();
                _txtUrl.SelectAll();
            }
            else if (e.Control && e.KeyCode == Keys.H)
            {
                e.SuppressKeyPress = true;
                ShowHistory();
            }
            else if (e.Control && e.KeyCode == Keys.D)
            {
                e.SuppressKeyPress = true;
                ToggleBookmarkCurrent();
            }
            else if (e.Control && e.KeyCode == Keys.U)
            {
                e.SuppressKeyPress = true;
                ShowPageSource();
            }
        }
    }
}
