using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SimpleBrowser
{
    public class MainForm : Form
    {
        // Brave Theme Colors
        private static readonly Color ColorTabHeader = Color.FromArgb(27, 28, 38);       // #1B1C26
        private static readonly Color ColorActiveTab = Color.FromArgb(40, 42, 58);       // #282A3A
        private static readonly Color ColorInactiveTab = Color.FromArgb(27, 28, 38);     // #1B1C26
        private static readonly Color ColorNavBar = Color.FromArgb(40, 42, 58);          // #282A3A
        private static readonly Color ColorOmnibox = Color.FromArgb(24, 25, 36);         // #181924
        private static readonly Color ColorBookmarksBar = Color.FromArgb(32, 33, 46);    // #20212E
        private static readonly Color ColorBraveOrange = Color.FromArgb(251, 84, 43);    // #FB542B
        private static readonly Color ColorTextMuted = Color.FromArgb(159, 164, 178);    // #9FA4B2
        private static readonly Color ColorButtonHover = Color.FromArgb(56, 59, 77);     // #383B4D

        private TabControl _tabControl;
        private Button _btnNewTabHeader;
        private Panel _navPanel;
        private FlowLayoutPanel _bookmarksBar;
        private StatusStrip _statusBar;
        private ToolStripStatusLabel _statusLabel;

        // Nav controls
        private Button _btnBack;
        private Button _btnForward;
        private Button _btnRefresh;
        private Button _btnHome;
        private Panel _omniboxPanel;
        private Label _lockIcon;
        private TextBox _txtUrl;
        private Button _btnStar;
        private Button _btnGo;
        private Button _btnShields;
        private Button _btnHistory;
        private Button _btnSource;
        private Button _btnMenu;

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
            Text = "Brave Browser";
            Size = new Size(1220, 800);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9F);
            BackColor = ColorTabHeader;
            KeyPreview = true;
            KeyDown += OnFormKeyDown;

            // 1. TOP TAB CONTROL (OwnerDraw for Brave look)
            _tabControl = new TabControl();
            _tabControl.Dock = DockStyle.Fill;
            _tabControl.DrawMode = TabDrawMode.OwnerDrawFixed;
            _tabControl.SizeMode = TabSizeMode.Fixed;
            _tabControl.ItemSize = new Size(190, 36);
            _tabControl.Padding = new Point(14, 4);
            _tabControl.DrawItem += OnTabControlDrawItem;
            _tabControl.MouseDown += OnTabControlMouseDown;
            _tabControl.SelectedIndexChanged += delegate { UpdateControls(); };

            _btnNewTabHeader = new Button();
            _btnNewTabHeader.Text = "+";
            _btnNewTabHeader.Font = new Font("Segoe UI", 12F, FontStyle.Regular);
            _btnNewTabHeader.FlatStyle = FlatStyle.Flat;
            _btnNewTabHeader.FlatAppearance.BorderSize = 0;
            _btnNewTabHeader.BackColor = ColorTabHeader;
            _btnNewTabHeader.ForeColor = Color.White;
            _btnNewTabHeader.Size = new Size(32, 32);
            _btnNewTabHeader.Cursor = Cursors.Hand;
            _btnNewTabHeader.Location = new Point(200, 2);
            _btnNewTabHeader.Click += delegate { AddNewTab(); };

            // 2. NAVIGATION BAR
            _navPanel = new Panel();
            _navPanel.Dock = DockStyle.Top;
            _navPanel.Height = 48;
            _navPanel.BackColor = ColorNavBar;
            _navPanel.Padding = new Padding(8, 6, 8, 6);

            _btnBack = CreateNavButton("←", "Back (Alt+Left)");
            _btnBack.Click += delegate { if (ActiveTab != null) ActiveTab.GoBack(); };

            _btnForward = CreateNavButton("→", "Forward (Alt+Right)");
            _btnForward.Click += delegate { if (ActiveTab != null) ActiveTab.GoForward(); };

            _btnRefresh = CreateNavButton("⟳", "Reload (Ctrl+R or F5)");
            _btnRefresh.Click += delegate { if (ActiveTab != null) ActiveTab.Reload(); };

            _btnHome = CreateNavButton("⌂", "Home Page");
            _btnHome.Click += delegate { if (ActiveTab != null) ActiveTab.LoadHomePage(); };

            // Omnibox / Address Bar Pill
            _omniboxPanel = new Panel();
            _omniboxPanel.BackColor = ColorOmnibox;
            _omniboxPanel.Height = 34;
            _omniboxPanel.Padding = new Padding(10, 4, 8, 4);

            _lockIcon = new Label();
            _lockIcon.Text = "🔒";
            _lockIcon.Font = new Font("Segoe UI", 10F);
            _lockIcon.ForeColor = Color.FromArgb(46, 204, 113);
            _lockIcon.AutoSize = true;
            _lockIcon.Dock = DockStyle.Left;
            _lockIcon.Cursor = Cursors.Hand;

            _txtUrl = new TextBox();
            _txtUrl.BorderStyle = BorderStyle.None;
            _txtUrl.BackColor = ColorOmnibox;
            _txtUrl.ForeColor = Color.White;
            _txtUrl.Font = new Font("Segoe UI", 10F);
            _txtUrl.Dock = DockStyle.Fill;
            _txtUrl.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    NavigateToInput();
                }
            };
            _txtUrl.Enter += delegate { _txtUrl.SelectAll(); };

            _btnStar = new Button();
            _btnStar.Text = "☆";
            _btnStar.Font = new Font("Segoe UI", 11F);
            _btnStar.FlatStyle = FlatStyle.Flat;
            _btnStar.FlatAppearance.BorderSize = 0;
            _btnStar.ForeColor = ColorTextMuted;
            _btnStar.Dock = DockStyle.Right;
            _btnStar.Width = 28;
            _btnStar.Cursor = Cursors.Hand;
            _btnStar.Click += delegate { ToggleBookmarkCurrent(); };

            _btnGo = new Button();
            _btnGo.Text = "➔";
            _btnGo.Font = new Font("Segoe UI", 10F);
            _btnGo.FlatStyle = FlatStyle.Flat;
            _btnGo.FlatAppearance.BorderSize = 0;
            _btnGo.ForeColor = ColorBraveOrange;
            _btnGo.Dock = DockStyle.Right;
            _btnGo.Width = 28;
            _btnGo.Cursor = Cursors.Hand;
            _btnGo.Click += delegate { NavigateToInput(); };

            _omniboxPanel.Controls.Add(_txtUrl);
            _omniboxPanel.Controls.Add(_lockIcon);
            _omniboxPanel.Controls.Add(_btnStar);
            _omniboxPanel.Controls.Add(_btnGo);

            // Paint orange border on Omnibox
            _omniboxPanel.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(60, 64, 84), 1))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, _omniboxPanel.Width - 1, _omniboxPanel.Height - 1);
                }
            };

            // Brave Shields Lion Button
            _btnShields = new Button();
            _btnShields.Text = "🦁";
            _btnShields.Font = new Font("Segoe UI", 13F);
            _btnShields.FlatStyle = FlatStyle.Flat;
            _btnShields.FlatAppearance.BorderSize = 0;
            _btnShields.BackColor = Color.FromArgb(251, 84, 43);
            _btnShields.ForeColor = Color.White;
            _btnShields.Size = new Size(34, 34);
            _btnShields.Cursor = Cursors.Hand;
            var tip = new ToolTip();
            tip.SetToolTip(_btnShields, "Brave Shields - Trackers & Ads Blocked");
            _btnShields.Click += delegate { ShowBraveShields(); };

            _btnHistory = CreateNavButton("🕒", "Browsing History (Ctrl+H)");
            _btnHistory.Click += delegate { ShowHistory(); };

            _btnSource = CreateNavButton("</>", "Page Source (Ctrl+U)");
            _btnSource.Click += delegate { ShowPageSource(); };

            _btnMenu = CreateNavButton("☰", "Brave Menu");
            _btnMenu.Click += delegate { ShowBraveMenu(); };

            // Assemble Nav Panel
            _navPanel.Controls.Add(_btnMenu);
            _navPanel.Controls.Add(_btnSource);
            _navPanel.Controls.Add(_btnHistory);
            _navPanel.Controls.Add(_btnShields);
            _navPanel.Controls.Add(_omniboxPanel);
            _navPanel.Controls.Add(_btnHome);
            _navPanel.Controls.Add(_btnRefresh);
            _navPanel.Controls.Add(_btnForward);
            _navPanel.Controls.Add(_btnBack);

            // 3. BOOKMARKS BAR
            _bookmarksBar = new FlowLayoutPanel();
            _bookmarksBar.Dock = DockStyle.Top;
            _bookmarksBar.Height = 32;
            _bookmarksBar.BackColor = ColorBookmarksBar;
            _bookmarksBar.Padding = new Padding(8, 3, 8, 3);
            _bookmarksBar.WrapContents = false;
            _bookmarksBar.AutoScroll = false;

            // 4. STATUS BAR
            _statusBar = new StatusStrip();
            _statusBar.BackColor = ColorOmnibox;
            _statusLabel = new ToolStripStatusLabel("🦁 Brave Ready");
            _statusLabel.ForeColor = ColorTextMuted;
            _statusLabel.Spring = true;
            _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
            _statusBar.Items.Add(_statusLabel);

            // 5. Layout Setup
            Controls.Add(_tabControl);
            Controls.Add(_bookmarksBar);
            Controls.Add(_navPanel);
            Controls.Add(_statusBar);

            Resize += OnFormResized;
            LayoutNavControls();
        }

        private Button CreateNavButton(string text, string tooltip)
        {
            var btn = new Button();
            btn.Text = text;
            btn.Font = new Font("Segoe UI", 11F);
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = ColorNavBar;
            btn.ForeColor = Color.White;
            btn.Size = new Size(34, 34);
            btn.Cursor = Cursors.Hand;
            btn.MouseEnter += delegate { btn.BackColor = ColorButtonHover; };
            btn.MouseLeave += delegate { btn.BackColor = ColorNavBar; };
            var tip = new ToolTip();
            tip.SetToolTip(btn, tooltip);
            return btn;
        }

        private void LayoutNavControls()
        {
            int left = 8;
            int btnSize = 34;
            int gap = 4;

            _btnBack.Location = new Point(left, 7);
            left += btnSize + gap;
            _btnForward.Location = new Point(left, 7);
            left += btnSize + gap;
            _btnRefresh.Location = new Point(left, 7);
            left += btnSize + gap;
            _btnHome.Location = new Point(left, 7);
            left += btnSize + gap + 4;

            int right = _navPanel.Width - 8;
            _btnMenu.Location = new Point(right - btnSize, 7);
            right -= (btnSize + gap);
            _btnSource.Location = new Point(right - btnSize, 7);
            right -= (btnSize + gap);
            _btnHistory.Location = new Point(right - btnSize, 7);
            right -= (btnSize + gap);
            _btnShields.Location = new Point(right - btnSize, 7);
            right -= (btnSize + gap + 4);

            int omniboxWidth = Math.Max(200, right - left);
            _omniboxPanel.Location = new Point(left, 7);
            _omniboxPanel.Size = new Size(omniboxWidth, 34);

            _txtUrl.Location = new Point(28, 7);
            _txtUrl.Width = omniboxWidth - 85;

            // Reposition New Tab button next to last tab
            int tabCount = _tabControl.TabCount;
            int tabWidth = _tabControl.ItemSize.Width;
            int plusX = Math.Min(_tabControl.Width - 40, tabCount * tabWidth + 10);
            _btnNewTabHeader.Location = new Point(plusX, 2);
        }

        private void OnFormResized(object sender, EventArgs e)
        {
            LayoutNavControls();
        }

        // Custom Brave Tab Rendering
        private void OnTabControlDrawItem(object sender, DrawItemEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle tabRect = _tabControl.GetTabRect(e.Index);
            bool isSelected = (e.Index == _tabControl.SelectedIndex);

            Color bgColor = isSelected ? ColorActiveTab : ColorInactiveTab;
            using (var brush = new SolidBrush(bgColor))
            {
                g.FillRectangle(brush, tabRect);
            }

            if (isSelected)
            {
                // Brave Orange Accent top border on active tab
                using (var pen = new Pen(ColorBraveOrange, 2.5f))
                {
                    g.DrawLine(pen, tabRect.Left, tabRect.Top + 1, tabRect.Right, tabRect.Top + 1);
                }
            }
            else
            {
                // Subtle divider between inactive tabs
                using (var pen = new Pen(Color.FromArgb(45, 48, 65), 1))
                {
                    g.DrawLine(pen, tabRect.Right - 1, tabRect.Top + 8, tabRect.Right - 1, tabRect.Bottom - 8);
                }
            }

            // Draw Favicon / Globe icon
            string icon = "🦁";
            using (var font = new Font("Segoe UI", 9F))
            using (var brush = new SolidBrush(Color.White))
            {
                g.DrawString(icon, font, brush, tabRect.Left + 8, tabRect.Top + 9);
            }

            // Draw Tab Title
            string title = _tabControl.TabPages[e.Index].Text;
            Rectangle textRect = new Rectangle(tabRect.Left + 30, tabRect.Top + 9, tabRect.Width - 60, tabRect.Height - 12);
            Color textColor = isSelected ? Color.White : ColorTextMuted;
            TextRenderer.DrawText(g, title, Font, textRect, textColor, TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter);

            // Draw Close Button '✕'
            Rectangle closeRect = new Rectangle(tabRect.Right - 22, tabRect.Top + 10, 16, 16);
            using (var brush = new SolidBrush(isSelected ? Color.White : ColorTextMuted))
            using (var font = new Font("Segoe UI", 8.5F, FontStyle.Bold))
            {
                g.DrawString("✕", font, brush, closeRect.X, closeRect.Y);
            }
        }

        private void OnTabControlMouseDown(object sender, MouseEventArgs e)
        {
            for (int i = 0; i < _tabControl.TabPages.Count; i++)
            {
                Rectangle tabRect = _tabControl.GetTabRect(i);
                Rectangle closeRect = new Rectangle(tabRect.Right - 24, tabRect.Top + 8, 20, 20);

                if (closeRect.Contains(e.Location))
                {
                    CloseTabAt(i);
                    return;
                }
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
            tab.StatusTextChanged += OnTabStatusTextChanged;

            _tabControl.TabPages.Add(tab);
            _tabControl.SelectedTab = tab;
            UpdateControls();
            LayoutNavControls();
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
            LayoutNavControls();
        }

        public void CloseActiveTab()
        {
            CloseTabAt(_tabControl.SelectedIndex);
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
                _btnStar.ForeColor = ColorTextMuted;
                _statusLabel.Text = "Removed bookmark: " + url;
            }
            else
            {
                _bookmarkManager.AddBookmark(title, url);
                _btnStar.Text = "★";
                _btnStar.ForeColor = ColorBraveOrange;
                _statusLabel.Text = "🦁 Bookmarked: " + title;
            }
            RebuildBookmarksBar();
        }

        private void RebuildBookmarksBar()
        {
            _bookmarksBar.Controls.Clear();

            foreach (var b in _bookmarkManager.GetBookmarks())
            {
                var btn = new Button();
                btn.Text = b.Title;
                btn.Tag = b.Url;
                btn.AutoSize = true;
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderSize = 0;
                btn.BackColor = Color.FromArgb(43, 46, 64);
                btn.ForeColor = Color.White;
                btn.Font = new Font("Segoe UI", 8.5F);
                btn.Cursor = Cursors.Hand;
                btn.Margin = new Padding(2, 2, 4, 2);
                btn.Height = 24;
                btn.MouseEnter += delegate { btn.BackColor = Color.FromArgb(58, 62, 86); };
                btn.MouseLeave += delegate { btn.BackColor = Color.FromArgb(43, 46, 64); };

                var currentUrl = b.Url;
                btn.Click += delegate
                {
                    if (ActiveTab != null)
                    {
                        ActiveTab.Navigate(currentUrl);
                    }
                };

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
                _btnStar.ForeColor = isBookmarked ? ColorBraveOrange : ColorTextMuted;

                Text = (string.IsNullOrEmpty(tab.Text) ? "Brave Browser" : tab.Text + " - Brave");
            }
            else
            {
                _btnBack.Enabled = false;
                _btnForward.Enabled = false;
                _txtUrl.Text = "";
                _btnStar.Text = "☆";
                Text = "Brave Browser";
            }
            _tabControl.Invalidate();
        }

        private void OnTabUrlChanged(BrowserTab tab, string url)
        {
            if (tab == ActiveTab)
            {
                string displayUrl = url == "about:home" ? "" : url;
                _txtUrl.Text = displayUrl;
                bool bookmarked = _bookmarkManager.IsBookmarked(url);
                _btnStar.Text = bookmarked ? "★" : "☆";
                _btnStar.ForeColor = bookmarked ? ColorBraveOrange : ColorTextMuted;
            }
        }

        private void OnTabTitleChanged(BrowserTab tab, string title)
        {
            if (tab == ActiveTab)
            {
                Text = title + " - Brave";
            }
            if (!string.IsNullOrEmpty(tab.CurrentUrl) && tab.CurrentUrl != "about:home")
            {
                _historyManager.AddEntry(title, tab.CurrentUrl);
            }
            _tabControl.Invalidate();
        }

        private void OnTabStateChanged(BrowserTab tab)
        {
            if (tab == ActiveTab)
            {
                _btnBack.Enabled = tab.CanGoBack;
                _btnForward.Enabled = tab.CanGoForward;
            }
            _tabControl.Invalidate();
        }

        private void OnTabStatusTextChanged(BrowserTab tab, string status)
        {
            if (tab == ActiveTab)
            {
                _statusLabel.Text = string.IsNullOrEmpty(status) ? "🦁 Brave Ready" : status;
            }
        }

        private void ShowBraveShields()
        {
            string url = ActiveTab != null ? ActiveTab.CurrentUrl : "";
            var shields = new BraveShieldsForm(url);
            Point pt = _btnShields.PointToScreen(new Point(0, _btnShields.Height + 4));
            shields.Location = new Point(Math.Max(10, pt.X - 280), pt.Y);
            shields.Show(this);
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
            menu.BackColor = ColorOmnibox;
            menu.ForeColor = Color.White;

            var newTabItem = new ToolStripMenuItem("New Tab\tCtrl+T", null, delegate { AddNewTab(); });
            var bookmarksItem = new ToolStripMenuItem("Bookmark Page\tCtrl+D", null, delegate { ToggleBookmarkCurrent(); });
            var historyItem = new ToolStripMenuItem("History\tCtrl+H", null, delegate { ShowHistory(); });
            var sourceItem = new ToolStripMenuItem("Page Source\tCtrl+U", null, delegate { ShowPageSource(); });
            var shieldsItem = new ToolStripMenuItem("🦁 Brave Shields", null, delegate { ShowBraveShields(); });
            var aboutItem = new ToolStripMenuItem("About Brave Browser Edition", null, delegate
            {
                MessageBox.Show(
                    "Brave Browser UI Edition v1.5\n\n" +
                    "A fast, private, and lightweight browser built from scratch.\n\n" +
                    "Features:\n" +
                    "• Brave Dark Theme & Brave Orange styling\n" +
                    "• Multi-tab browsing with quick close\n" +
                    "• Brave Shields popup & protection panel\n" +
                    "• Smart Omnibox with Brave Search default\n" +
                    "• Bookmarks bar with instant access\n" +
                    "• Browsing history manager (Ctrl+H)\n" +
                    "• HTML Source inspector (Ctrl+U)\n" +
                    "• Brave-style Start Page with live clock & statistics\n",
                    "About Brave Edition",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            });

            menu.Items.Add(newTabItem);
            menu.Items.Add(bookmarksItem);
            menu.Items.Add(historyItem);
            menu.Items.Add(sourceItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(shieldsItem);
            menu.Items.Add(aboutItem);

            Point pt = _btnMenu.PointToScreen(new Point(0, _btnMenu.Height));
            menu.Show(pt);
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
                if (ActiveTab != null) ActiveTab.Reload();
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
