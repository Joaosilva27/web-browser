using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SimpleBrowser
{
    /// <summary>
    /// Browse, filter and open bookmarks. Folders come from whatever the browser
    /// that supplied them used, so an imported structure stays recognisable.
    /// </summary>
    public class BookmarksForm : Form
    {
        private const int EM_SETCUEBANNER = 0x1501;

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);

        private readonly BookmarkManager _bookmarks;
        private ListBox _folderList;
        private ListView _listView;
        private TextBox _searchBox;
        private Label _countLabel;

        public string SelectedUrl { get; private set; }

        /// <summary>True when the set changed, so the host can refresh its star.</summary>
        public bool Changed { get; private set; }

        public BookmarksForm(BookmarkManager bookmarks)
        {
            _bookmarks = bookmarks;

            Text = "Bookmarks";
            AutoScaleMode = AutoScaleMode.None;
            Size = new Size(Dpi.Px(900), Dpi.Px(600));
            MinimumSize = new Size(Dpi.Px(680), Dpi.Px(440));
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Theme.ContentBackground;
            ForeColor = Theme.TextPrimary;
            Font = new Font("Segoe UI", 9F);

            var topPanel = new Panel();
            topPanel.Dock = DockStyle.Top;
            topPanel.Height = Dpi.Px(68);
            topPanel.BackColor = Theme.CardBackground;
            topPanel.Paint += delegate (object s, PaintEventArgs e)
            {
                using (var pen = new Pen(Theme.CardBorder, 1))
                {
                    e.Graphics.DrawLine(pen, 0, topPanel.Height - 1, topPanel.Width, topPanel.Height - 1);
                }
            };

            var title = new Label();
            title.Text = "Bookmarks";
            title.Font = new Font("Segoe UI Semibold", 13F);
            title.ForeColor = Theme.TextPrimary;
            title.AutoSize = true;
            title.Location = new Point(Dpi.Px(20), Dpi.Px(16));

            var searchWrap = new Panel();
            searchWrap.Location = new Point(Dpi.Px(140), Dpi.Px(17));
            searchWrap.Size = new Size(Dpi.Px(320), Dpi.Px(32));
            searchWrap.BackColor = Theme.IsDark
                ? Color.FromArgb(56, 56, 60)
                : Color.FromArgb(244, 244, 246);
            searchWrap.Padding = new Padding(Dpi.Px(10), Dpi.Px(7), Dpi.Px(10), Dpi.Px(7));

            _searchBox = new TextBox();
            _searchBox.BorderStyle = BorderStyle.None;
            _searchBox.Dock = DockStyle.Fill;
            _searchBox.BackColor = searchWrap.BackColor;
            _searchBox.ForeColor = Theme.TextPrimary;
            _searchBox.Font = new Font("Segoe UI", 9.5F);
            _searchBox.TextChanged += delegate { PopulateList(); };
            searchWrap.Controls.Add(_searchBox);

            _countLabel = new Label();
            _countLabel.Text = "";
            _countLabel.Font = new Font("Segoe UI", 8.25F);
            _countLabel.ForeColor = Theme.TextTertiary;
            _countLabel.AutoSize = true;
            _countLabel.Location = new Point(Dpi.Px(142), Dpi.Px(52));

            var openButton = new AuraButton();
            openButton.Text = "Open";
            openButton.Size = new Size(Dpi.Px(96), Dpi.Px(32));
            openButton.Radius = Dpi.Px(8);
            openButton.BackColor = Theme.AccentBlue;
            openButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            openButton.Location = new Point(topPanel.Width - Dpi.Px(136), Dpi.Px(17));
            openButton.Click += delegate { OpenSelected(); };

            var deleteButton = new AuraButton();
            deleteButton.Text = "Delete";
            deleteButton.Size = new Size(Dpi.Px(88), Dpi.Px(32));
            deleteButton.Radius = Dpi.Px(8);
            deleteButton.BackColor = Theme.IsDark
                ? Color.FromArgb(72, 72, 78)
                : Color.FromArgb(228, 228, 232);
            deleteButton.ForeColor = Theme.TextPrimary;
            deleteButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            deleteButton.Location = new Point(topPanel.Width - Dpi.Px(234), Dpi.Px(17));
            deleteButton.Click += delegate { DeleteSelected(); };

            topPanel.Controls.Add(title);
            topPanel.Controls.Add(searchWrap);
            topPanel.Controls.Add(_countLabel);
            topPanel.Controls.Add(openButton);
            topPanel.Controls.Add(deleteButton);

            _folderList = new ListBox();
            _folderList.Dock = DockStyle.Left;
            _folderList.Width = Dpi.Px(210);
            _folderList.IntegralHeight = false;
            _folderList.BorderStyle = BorderStyle.None;
            _folderList.BackColor = Theme.IsDark
                ? Color.FromArgb(32, 32, 35)
                : Color.FromArgb(248, 248, 250);
            _folderList.ForeColor = Theme.TextPrimary;
            _folderList.Font = new Font("Segoe UI", 9.5F);
            _folderList.ItemHeight = Dpi.Px(24);
            _folderList.SelectedIndexChanged += delegate { PopulateList(); };

            _listView = new ListView();
            _listView.Dock = DockStyle.Fill;
            _listView.View = View.Details;
            _listView.FullRowSelect = true;
            _listView.GridLines = false;
            _listView.HideSelection = false;
            _listView.BorderStyle = BorderStyle.None;
            _listView.BackColor = Theme.ContentBackground;
            _listView.ForeColor = Theme.TextPrimary;
            _listView.Font = new Font("Segoe UI", 9F);
            _listView.Columns.Add("Name", Dpi.Px(280));
            _listView.Columns.Add("Folder", Dpi.Px(180));
            _listView.Columns.Add("Address", Dpi.Px(380));
            _listView.DoubleClick += delegate { OpenSelected(); };
            _listView.KeyDown += delegate (object s, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Delete) DeleteSelected();
            };

            Controls.Add(_listView);
            Controls.Add(_folderList);
            Controls.Add(topPanel);

            PopulateFolders();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            SendMessage(_searchBox.Handle, EM_SETCUEBANNER, (IntPtr)1, "Search bookmarks");
        }

        private void PopulateFolders()
        {
            _folderList.Items.Clear();
            _folderList.Items.Add("All Bookmarks");

            foreach (string folder in _bookmarks.GetFolders())
            {
                _folderList.Items.Add(string.IsNullOrEmpty(folder) ? BookmarkManager.RootFolder : folder);
            }

            _folderList.SelectedIndex = 0;
        }

        private void PopulateList()
        {
            string selectedFolder = _folderList.SelectedIndex <= 0
                ? null
                : _folderList.SelectedItem as string;
            string filter = _searchBox.Text.Trim().ToLowerInvariant();

            _listView.Items.Clear();

            foreach (var bookmark in _bookmarks.GetBookmarks())
            {
                string folder = string.IsNullOrEmpty(bookmark.Folder)
                    ? BookmarkManager.RootFolder
                    : bookmark.Folder;

                if (selectedFolder != null && folder != selectedFolder) continue;

                if (filter.Length > 0)
                {
                    bool matchTitle = bookmark.Title != null &&
                                      bookmark.Title.ToLowerInvariant().Contains(filter);
                    bool matchUrl = bookmark.Url != null &&
                                    bookmark.Url.ToLowerInvariant().Contains(filter);
                    if (!matchTitle && !matchUrl) continue;
                }

                var row = new ListViewItem(bookmark.Title ?? string.Empty);
                row.SubItems.Add(folder);
                row.SubItems.Add(bookmark.Url ?? string.Empty);
                row.Tag = bookmark.Url;
                _listView.Items.Add(row);
            }

            _countLabel.Text = _listView.Items.Count == 1
                ? "1 bookmark"
                : _listView.Items.Count + " bookmarks";
        }

        private void OpenSelected()
        {
            if (_listView.SelectedItems.Count == 0) return;

            SelectedUrl = _listView.SelectedItems[0].Tag as string;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void DeleteSelected()
        {
            if (_listView.SelectedItems.Count == 0) return;

            string url = _listView.SelectedItems[0].Tag as string;
            if (string.IsNullOrEmpty(url)) return;

            _bookmarks.RemoveBookmark(url);
            Changed = true;
            PopulateFolders();
            _listView.Items.Clear();
            _countLabel.Text = "";
        }
    }
}
