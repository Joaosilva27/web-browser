using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SimpleBrowser
{
    public class MainForm : Form
    {
        // Apple-inspired Futuristic Palette
        private static readonly Color ColorBase = Color.FromArgb(10, 11, 16);            // Obsidian #0A0B10
        private static readonly Color ColorHeader = Color.FromArgb(13, 14, 21);          // #0D0E15
        private static readonly Color ColorToolbar = Color.FromArgb(17, 19, 28);         // #11131C
        private static readonly Color ColorOmnibarBg = Color.FromArgb(11, 12, 18);       // #0B0C12
        private static readonly Color ColorOmnibarBorder = Color.FromArgb(36, 40, 56);   // #242838
        private static readonly Color ColorAccentCyan = Color.FromArgb(0, 242, 254);     // #00F2FE (Cyan Glow)
        private static readonly Color ColorAccentPurple = Color.FromArgb(157, 78, 221);  // #9D4EDD
        private static readonly Color ColorTextWhite = Color.FromArgb(245, 245, 247);    // SF Pro White
        private static readonly Color ColorTextMuted = Color.FromArgb(134, 134, 139);    // Apple Gray #86868B
        private static readonly Color ColorTabActive = Color.FromArgb(26, 30, 44);       // Frosted Glass Pill
        private static readonly Color ColorTabInactive = Color.FromArgb(14, 16, 23);     // Deep Translucent
        private static readonly Color ColorButtonHover = Color.FromArgb(32, 36, 52);

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
        private Panel _omnibarPanel;
        private Label _lockIcon;
        private TextBox _txtUrl;
        private Button _btnStar;
        private Button _btnGo;
        private Button _btnShield;
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
            Text = "Aether";
            Size = new Size(1260, 820);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9F);
            BackColor = ColorBase;
            KeyPreview = true;
            KeyDown += OnFormKeyDown;

            // 1. TOP TAB CONTROL (OwnerDraw for Apple VisionOS floating pill look)
            _tabControl = new TabControl();
            _tabControl.Dock = DockStyle.Fill;
            _tabControl.DrawMode = TabDrawMode.OwnerDrawFixed;
            _tabControl.SizeMode = TabSizeMode.Fixed;
            _tabControl.ItemSize = new Size(196, 38);
            _tabControl.Padding = new Point(16, 4);
            _tabControl.DrawItem += OnTabControlDrawItem;
            _tabControl.MouseDown += OnTabControlMouseDown;
            _tabControl.SelectedIndexChanged += delegate { UpdateControls(); };

            // Floating + Button beside tabs
            _btnNewTabHeader = new Button();
            _btnNewTabHeader.Text = "+";
            _btnNewTabHeader.Font = new Font("Segoe UI", 12F, FontStyle.Regular);
            _btnNewTabHeader.FlatStyle = FlatStyle.Flat;
            _btnNewTabHeader.FlatAppearance.BorderSize = 0;
            _btnNewTabHeader.BackColor = Color.FromArgb(20, 23, 33);
            _btnNewTabHeader.ForeColor = ColorTextMuted;
            _btnNewTabHeader.Size = new Size(30, 30);
            _btnNewTabHeader.Cursor = Cursors.Hand;
            _btnNewTabHeader.Location = new Point(210, 4);
            _btnNewTabHeader.MouseEnter += delegate { _btnNewTabHeader.ForeColor = ColorAccentCyan; _btnNewTabHeader.BackColor = Color.FromArgb(30, 35, 50); };
            _btnNewTabHeader.MouseLeave += delegate { _btnNewTabHeader.ForeColor = ColorTextMuted; _btnNewTabHeader.BackColor = Color.FromArgb(20, 23, 33); };
            _btnNewTabHeader.Click += delegate { AddNewTab(); };

            // 2. UNIFIED FLOATING TOOLBAR
            _navPanel = new Panel();
            _navPanel.Dock = DockStyle.Top;
            _navPanel.Height = 52;
            _navPanel.BackColor = ColorToolbar;
            _navPanel.Padding = new Padding(12, 8, 12, 8);

            _btnBack = CreatePillButton("‹", "Back (Alt+Left)", 36);
            _btnBack.Font = new Font("Segoe UI", 15F, FontStyle.Regular);
            _btnBack.Click += delegate { if (ActiveTab != null) ActiveTab.GoBack(); };

            _btnForward = CreatePillButton("›", "Forward (Alt+Right)", 36);
            _btnForward.Font = new Font("Segoe UI", 15F, FontStyle.Regular);
            _btnForward.Click += delegate { if (ActiveTab != null) ActiveTab.GoForward(); };

            _btnRefresh = CreatePillButton("⟳", "Reload (Ctrl+R)", 36);
            _btnRefresh.Font = new Font("Segoe UI", 12F, FontStyle.Regular);
            _btnRefresh.Click += delegate { if (ActiveTab != null) ActiveTab.Reload(); };

            _btnHome = CreatePillButton("⌂", "Start Page", 36);
            _btnHome.Font = new Font("Segoe UI", 12F, FontStyle.Regular);
            _btnHome.Click += delegate { if (ActiveTab != null) ActiveTab.LoadHomePage(); };

            // Omnibar (Floating Frosted Capsule)
            _omnibarPanel = new Panel();
            _omnibarPanel.BackColor = ColorOmnibarBg;
            _omnibarPanel.Height = 36;
            _omnibarPanel.Padding = new Padding(12, 4, 10, 4);

            _lockIcon = new Label();
            _lockIcon.Text = "✦";
            _lockIcon.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            _lockIcon.ForeColor = ColorAccentCyan;
            _lockIcon.AutoSize = true;
            _lockIcon.Dock = DockStyle.Left;
            _lockIcon.Cursor = Cursors.Hand;
            var lockTip = new ToolTip();
            lockTip.SetToolTip(_lockIcon, "Aether Quantum Security Active");

            _txtUrl = new TextBox();
            _txtUrl.BorderStyle = BorderStyle.None;
            _txtUrl.BackColor = ColorOmnibarBg;
            _txtUrl.ForeColor = ColorTextWhite;
            _txtUrl.Font = new Font("Segoe UI", 9.5F);
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
            _btnStar.Text = "★";
            _btnStar.Font = new Font("Segoe UI", 11F);
            _btnStar.FlatStyle = FlatStyle.Flat;
            _btnStar.FlatAppearance.BorderSize = 0;
            _btnStar.ForeColor = ColorTextMuted;
            _btnStar.Dock = DockStyle.Right;
            _btnStar.Width = 28;
            _btnStar.Cursor = Cursors.Hand;
            _btnStar.Click += delegate { ToggleBookmarkCurrent(); };

            _btnGo = new Button();
            _btnGo.Text = "→";
            _btnGo.Font = new Font("Segoe UI", 11F);
            _btnGo.FlatStyle = FlatStyle.Flat;
            _btnGo.FlatAppearance.BorderSize = 0;
            _btnGo.ForeColor = ColorAccentCyan;
            _btnGo.Dock = DockStyle.Right;
            _btnGo.Width = 28;
            _btnGo.Cursor = Cursors.Hand;
            _btnGo.Click += delegate { NavigateToInput(); };

            _omnibarPanel.Controls.Add(_txtUrl);
            _omnibarPanel.Controls.Add(_lockIcon);
            _omnibarPanel.Controls.Add(_btnStar);
            _omnibarPanel.Controls.Add(_btnGo);

            // Specular border on Omnibar
            _omnibarPanel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var path = CreateRoundedRectanglePath(new Rectangle(0, 0, _omnibarPanel.Width - 1, _omnibarPanel.Height - 1), 16))
                using (var pen = new Pen(ColorOmnibarBorder, 1.2f))
                {
                    e.Graphics.DrawPath(pen, path);
                }
            };

            // Aether Shield Capsule
            _btnShield = new Button();
            _btnShield.Text = "✦ Shield";
            _btnShield.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _btnShield.FlatStyle = FlatStyle.Flat;
            _btnShield.FlatAppearance.BorderSize = 0;
            _btnShield.BackColor = Color.FromArgb(18, 26, 38);
            _btnShield.ForeColor = ColorAccentCyan;
            _btnShield.Size = new Size(84, 34);
            _btnShield.Cursor = Cursors.Hand;
            _btnShield.MouseEnter += delegate { _btnShield.BackColor = Color.FromArgb(24, 38, 56); };
            _btnShield.MouseLeave += delegate { _btnShield.BackColor = Color.FromArgb(18, 26, 38); };
            var shieldTip = new ToolTip();
            shieldTip.SetToolTip(_btnShield, "Aether Quantum Shield & Privacy Telemetry");
            _btnShield.Click += delegate { ShowAetherShield(); };

            _btnHistory = CreatePillButton("🕒", "History (Ctrl+H)", 36);
            _btnHistory.Click += delegate { ShowHistory(); };

            _btnSource = CreatePillButton("⌥", "Source Inspector (Ctrl+U)", 36);
            _btnSource.Click += delegate { ShowPageSource(); };

            _btnMenu = CreatePillButton("•••", "Aether Menu", 38);
            _btnMenu.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            _btnMenu.Click += delegate { ShowAetherMenu(); };

            // Assemble Nav Panel
            _navPanel.Controls.Add(_btnMenu);
            _navPanel.Controls.Add(_btnSource);
            _navPanel.Controls.Add(_btnHistory);
            _navPanel.Controls.Add(_btnShield);
            _navPanel.Controls.Add(_omnibarPanel);
            _navPanel.Controls.Add(_btnHome);
            _navPanel.Controls.Add(_btnRefresh);
            _navPanel.Controls.Add(_btnForward);
            _navPanel.Controls.Add(_btnBack);

            // 3. BOOKMARKS BAR (Frosted glass pills)
            _bookmarksBar = new FlowLayoutPanel();
            _bookmarksBar.Dock = DockStyle.Top;
            _bookmarksBar.Height = 32;
            _bookmarksBar.BackColor = Color.FromArgb(14, 15, 23);
            _bookmarksBar.Padding = new Padding(12, 2, 12, 2);
            _bookmarksBar.WrapContents = false;
            _bookmarksBar.AutoScroll = false;

            // 4. STATUS BAR
            _statusBar = new StatusStrip();
            _statusBar.BackColor = Color.FromArgb(8, 9, 14);
            _statusLabel = new ToolStripStatusLabel("✦ Aether Quantum Engine Ready");
            _statusLabel.ForeColor = ColorTextMuted;
            _statusLabel.Spring = true;
            _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
            _statusBar.Items.Add(_statusLabel);

            // 5. Hierarchy
            Controls.Add(_tabControl);
            Controls.Add(_bookmarksBar);
            Controls.Add(_navPanel);
            Controls.Add(_statusBar);

            Resize += OnFormResized;
            LayoutNavControls();
        }

        private Button CreatePillButton(string text, string tooltip, int width)
        {
            var btn = new Button();
            btn.Text = text;
            btn.Font = new Font("Segoe UI", 10.5F);
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = ColorToolbar;
            btn.ForeColor = ColorTextMuted;
            btn.Size = new Size(width, 34);
            btn.Cursor = Cursors.Hand;
            btn.MouseEnter += delegate { btn.BackColor = ColorButtonHover; btn.ForeColor = ColorTextWhite; };
            btn.MouseLeave += delegate { btn.BackColor = ColorToolbar; btn.ForeColor = ColorTextMuted; };
            var tip = new ToolTip();
            tip.SetToolTip(btn, tooltip);
            return btn;
        }

        private void LayoutNavControls()
        {
            int left = 12;
            int gap = 4;

            _btnBack.Location = new Point(left, 9);
            left += _btnBack.Width + gap;
            _btnForward.Location = new Point(left, 9);
            left += _btnForward.Width + gap;
            _btnRefresh.Location = new Point(left, 9);
            left += _btnRefresh.Width + gap;
            _btnHome.Location = new Point(left, 9);
            left += _btnHome.Width + gap + 8;

            int right = _navPanel.Width - 12;
            _btnMenu.Location = new Point(right - _btnMenu.Width, 9);
            right -= (_btnMenu.Width + gap);
            _btnSource.Location = new Point(right - _btnSource.Width, 9);
            right -= (_btnSource.Width + gap);
            _btnHistory.Location = new Point(right - _btnHistory.Width, 9);
            right -= (_btnHistory.Width + gap);
            _btnShield.Location = new Point(right - _btnShield.Width, 9);
            right -= (_btnShield.Width + gap + 8);

            int omnibarWidth = Math.Max(220, right - left);
            _omnibarPanel.Location = new Point(left, 8);
            _omnibarPanel.Size = new Size(omnibarWidth, 36);

            _txtUrl.Location = new Point(32, 9);
            _txtUrl.Width = omnibarWidth - 92;

            int tabCount = _tabControl.TabCount;
            int tabWidth = _tabControl.ItemSize.Width;
            int plusX = Math.Min(_tabControl.Width - 45, tabCount * tabWidth + 12);
            _btnNewTabHeader.Location = new Point(plusX, 4);
        }

        private void OnFormResized(object sender, EventArgs e)
        {
            LayoutNavControls();
        }

        // Apple-style Pill Tab Rendering
        private void OnTabControlDrawItem(object sender, DrawItemEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle tabRect = _tabControl.GetTabRect(e.Index);
            bool isSelected = (e.Index == _tabControl.SelectedIndex);

            // Tab pill bounding box
            Rectangle pillRect = new Rectangle(tabRect.Left + 4, tabRect.Top + 4, tabRect.Width - 8, tabRect.Height - 8);

            using (var path = CreateRoundedRectanglePath(pillRect, 10))
            {
                if (isSelected)
                {
                    using (var brush = new LinearGradientBrush(pillRect, Color.FromArgb(28, 32, 48), Color.FromArgb(20, 23, 34), LinearGradientMode.Vertical))
                    {
                        g.FillPath(brush, path);
                    }
                    using (var pen = new Pen(Color.FromArgb(60, 68, 96), 1.2f))
                    {
                        g.DrawPath(pen, path);
                    }
                }
                else
                {
                    using (var brush = new SolidBrush(ColorTabInactive))
                    {
                        g.FillPath(brush, path);
                    }
                    using (var pen = new Pen(Color.FromArgb(26, 29, 40), 1f))
                    {
                        g.DrawPath(pen, path);
                    }
                }
            }

            // Glowing Icon
            string icon = isSelected ? "✦" : "○";
            using (var font = new Font("Segoe UI", 9F, FontStyle.Bold))
            using (var brush = new SolidBrush(isSelected ? ColorAccentCyan : ColorTextMuted))
            {
                g.DrawString(icon, font, brush, pillRect.Left + 10, pillRect.Top + 7);
            }

            // Tab Title
            string title = _tabControl.TabPages[e.Index].Text;
            Rectangle textRect = new Rectangle(pillRect.Left + 28, pillRect.Top + 6, pillRect.Width - 54, pillRect.Height - 12);
            Color textColor = isSelected ? ColorTextWhite : ColorTextMuted;
            TextRenderer.DrawText(g, title, Font, textRect, textColor, TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter);

            // Close Button '✕'
            Rectangle closeRect = new Rectangle(pillRect.Right - 20, pillRect.Top + 7, 14, 14);
            using (var brush = new SolidBrush(isSelected ? Color.FromArgb(180, 185, 200) : Color.FromArgb(80, 85, 100)))
            using (var font = new Font("Segoe UI", 8F, FontStyle.Bold))
            {
                g.DrawString("✕", font, brush, closeRect.X, closeRect.Y);
            }
        }

        private static GraphicsPath CreateRoundedRectanglePath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int diameter = radius * 2;
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void OnTabControlMouseDown(object sender, MouseEventArgs e)
        {
            for (int i = 0; i < _tabControl.TabPages.Count; i++)
            {
                Rectangle tabRect = _tabControl.GetTabRect(i);
                Rectangle pillRect = new Rectangle(tabRect.Left + 4, tabRect.Top + 4, tabRect.Width - 8, tabRect.Height - 8);
                Rectangle closeRect = new Rectangle(pillRect.Right - 24, pillRect.Top + 4, 22, 22);

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
                _btnStar.ForeColor = ColorTextMuted;
                _statusLabel.Text = "Removed bookmark: " + url;
            }
            else
            {
                _bookmarkManager.AddBookmark(title, url);
                _btnStar.ForeColor = ColorAccentCyan;
                _statusLabel.Text = "✦ Saved to Aether Bookmarks: " + title;
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
                btn.BackColor = Color.FromArgb(20, 22, 33);
                btn.ForeColor = Color.FromArgb(210, 215, 230);
                btn.Font = new Font("Segoe UI", 8.5F);
                btn.Cursor = Cursors.Hand;
                btn.Margin = new Padding(3, 2, 4, 2);
                btn.Height = 24;
                btn.MouseEnter += delegate { btn.BackColor = Color.FromArgb(32, 36, 54); btn.ForeColor = ColorAccentCyan; };
                btn.MouseLeave += delegate { btn.BackColor = Color.FromArgb(20, 22, 33); btn.ForeColor = Color.FromArgb(210, 215, 230); };

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
                _btnStar.ForeColor = isBookmarked ? ColorAccentCyan : ColorTextMuted;

                Text = (string.IsNullOrEmpty(tab.Text) ? "Aether" : tab.Text + " — Aether");
            }
            else
            {
                _btnBack.Enabled = false;
                _btnForward.Enabled = false;
                _txtUrl.Text = "";
                _btnStar.ForeColor = ColorTextMuted;
                Text = "Aether";
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
                _btnStar.ForeColor = bookmarked ? ColorAccentCyan : ColorTextMuted;
            }
        }

        private void OnTabTitleChanged(BrowserTab tab, string title)
        {
            if (tab == ActiveTab)
            {
                Text = title + " — Aether";
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
                _statusLabel.Text = string.IsNullOrEmpty(status) ? "✦ Aether Quantum Engine Ready" : status;
            }
        }

        private void ShowAetherShield()
        {
            string url = ActiveTab != null ? ActiveTab.CurrentUrl : "";
            var shield = new AetherShieldForm(url);
            Point pt = _btnShield.PointToScreen(new Point(0, _btnShield.Height + 6));
            shield.Location = new Point(Math.Max(10, pt.X - 250), pt.Y);
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

        private void ShowAetherMenu()
        {
            var menu = new ContextMenuStrip();
            menu.BackColor = ColorOmnibarBg;
            menu.ForeColor = ColorTextWhite;

            var newTabItem = new ToolStripMenuItem("New Tab\t⌘T", null, delegate { AddNewTab(); });
            var bookmarksItem = new ToolStripMenuItem("Bookmark Page\t⌘D", null, delegate { ToggleBookmarkCurrent(); });
            var historyItem = new ToolStripMenuItem("History\t⌘H", null, delegate { ShowHistory(); });
            var sourceItem = new ToolStripMenuItem("Inspect Source\t⌘U", null, delegate { ShowPageSource(); });
            var shieldItem = new ToolStripMenuItem("✦ Aether Shield...", null, delegate { ShowAetherShield(); });
            var aboutItem = new ToolStripMenuItem("About Aether Browser...", null, delegate
            {
                MessageBox.Show(
                    "Aether Browser 2.0\n\n" +
                    "A quantum-minimalist web browser designed with Cupertino simplicity & precision.\n\n" +
                    "Highlights:\n" +
                    "• Floating Frosted Glass UI with VisionOS styling\n" +
                    "• Ambient Cosmic Start Page with live clock\n" +
                    "• Apple-style Rounded Pill Tabs with quick close\n" +
                    "• Aether Quantum Shield with privacy telemetry\n" +
                    "• Edge Chromium 155 engine underneath\n\n" +
                    "Crafted for speed, elegance, and pure focus.",
                    "About Aether",
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

            Point pt = _btnMenu.PointToScreen(new Point(0, _btnMenu.Height + 4));
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
