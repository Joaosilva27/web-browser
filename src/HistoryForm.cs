using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SimpleBrowser
{
    public class HistoryForm : Form
    {
        private const int EM_SETCUEBANNER = 0x1501;

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);

        private readonly HistoryManager _historyManager;

        private ListView _listView;
        private ListView _searchList;
        private TextBox _searchBox;
        private Label _countLabel;
        private TogeButton _historyTab;
        private TogeButton _searchTab;

        public string SelectedUrl { get; private set; }

        public HistoryForm(HistoryManager historyManager)
        {
            _historyManager = historyManager;
            AppIcon.Apply(this);

            Text = "History";
            AutoScaleMode = AutoScaleMode.None;
            Size = new Size(Dpi.Px(880), Dpi.Px(580));
            MinimumSize = new Size(Dpi.Px(660), Dpi.Px(420));
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Theme.ContentBackground;
            ForeColor = Theme.TextPrimary;
            Font = new Font("Segoe UI", 9F);

            var topPanel = new Panel();
            topPanel.Dock = DockStyle.Top;
            topPanel.Height = Dpi.Px(68);
            topPanel.Width = ClientSize.Width;
            topPanel.BackColor = Theme.CardBackground;
            topPanel.Paint += delegate (object s, PaintEventArgs e)
            {
                using (var pen = new Pen(Theme.CardBorder, 1))
                {
                    e.Graphics.DrawLine(pen, 0, topPanel.Height - 1, topPanel.Width, topPanel.Height - 1);
                }
            };

            var title = new Label();
            title.Text = "History";
            title.Font = new Font("Segoe UI Semibold", 13F);
            title.ForeColor = Theme.TextPrimary;
            title.AutoSize = true;
            title.Location = new Point(Dpi.Px(20), Dpi.Px(16));

            // Segmented switch between browsing history and search terms.
            _historyTab = new TogeButton();
            _historyTab.Text = "History";
            _historyTab.Size = new Size(Dpi.Px(90), Dpi.Px(32));
            _historyTab.Radius = Dpi.Px(8);
            _historyTab.Location = new Point(Dpi.Px(126), Dpi.Px(17));
            _historyTab.Click += delegate { ShowTab(true); };

            _searchTab = new TogeButton();
            _searchTab.Text = "Search history";
            _searchTab.Size = new Size(Dpi.Px(126), Dpi.Px(32));
            _searchTab.Radius = Dpi.Px(8);
            _searchTab.Location = new Point(Dpi.Px(220), Dpi.Px(17));
            _searchTab.Click += delegate { ShowTab(false); };

            var searchWrap = new Panel();
            searchWrap.Location = new Point(Dpi.Px(362), Dpi.Px(17));
            searchWrap.Size = new Size(Dpi.Px(220), Dpi.Px(32));
            searchWrap.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
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
            _countLabel.Location = new Point(Dpi.Px(364), Dpi.Px(52));

            var openBtn = new TogeButton();
            openBtn.Text = "Open";
            openBtn.Size = new Size(Dpi.Px(96), Dpi.Px(32));
            openBtn.Radius = Dpi.Px(8);
            openBtn.BackColor = Theme.AccentBlue;
            openBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            openBtn.Location = new Point(topPanel.Width - Dpi.Px(136), Dpi.Px(17));
            openBtn.Click += delegate { OpenSelected(); };

            var clearBtn = new TogeButton();
            clearBtn.Text = "Clear";
            clearBtn.Size = new Size(Dpi.Px(88), Dpi.Px(32));
            clearBtn.Radius = Dpi.Px(8);
            clearBtn.BackColor = Theme.IsDark
                ? Color.FromArgb(72, 72, 78)
                : Color.FromArgb(228, 228, 232);
            clearBtn.ForeColor = Theme.TextPrimary;
            clearBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            clearBtn.Location = new Point(topPanel.Width - Dpi.Px(234), Dpi.Px(17));
            clearBtn.Click += delegate { ClearActive(); };

            topPanel.Controls.Add(title);
            topPanel.Controls.Add(_historyTab);
            topPanel.Controls.Add(_searchTab);
            topPanel.Controls.Add(searchWrap);
            topPanel.Controls.Add(_countLabel);
            topPanel.Controls.Add(openBtn);
            topPanel.Controls.Add(clearBtn);

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
            _listView.Columns.Add("Visited", Dpi.Px(160));
            _listView.Columns.Add("Page", Dpi.Px(300));
            _listView.Columns.Add("Address", Dpi.Px(380));
            _listView.DoubleClick += delegate { OpenSelected(); };

            _searchList = new ListView();
            _searchList.Dock = DockStyle.Fill;
            _searchList.View = View.Details;
            _searchList.FullRowSelect = true;
            _searchList.GridLines = false;
            _searchList.HideSelection = false;
            _searchList.BorderStyle = BorderStyle.None;
            _searchList.BackColor = Theme.ContentBackground;
            _searchList.ForeColor = Theme.TextPrimary;
            _searchList.Font = new Font("Segoe UI", 9F);
            _searchList.Columns.Add("Searched", Dpi.Px(160));
            _searchList.Columns.Add("Term", Dpi.Px(300));
            _searchList.Columns.Add("Result", Dpi.Px(380));
            _searchList.DoubleClick += delegate { OpenSelected(); };

            Controls.Add(_searchList);
            Controls.Add(_listView);
            Controls.Add(topPanel);

            ShowTab(true);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            SendMessage(_searchBox.Handle, EM_SETCUEBANNER, (IntPtr)1, "Search");
        }

        private bool Searching
        {
            get { return _searchList.Visible; }
        }

        private void ShowTab(bool history)
        {
            _listView.Visible = history;
            _searchList.Visible = !history;

            if (history)
            {
                _historyTab.BackColor = Theme.AccentBlue;
                _historyTab.ForeColor = Color.White;
                _searchTab.BackColor = Theme.IsDark
                    ? Color.FromArgb(72, 72, 78)
                    : Color.FromArgb(228, 228, 232);
                _searchTab.ForeColor = Theme.TextPrimary;
            }
            else
            {
                _searchTab.BackColor = Theme.AccentBlue;
                _searchTab.ForeColor = Color.White;
                _historyTab.BackColor = Theme.IsDark
                    ? Color.FromArgb(72, 72, 78)
                    : Color.FromArgb(228, 228, 232);
                _historyTab.ForeColor = Theme.TextPrimary;
            }

            _listView.BringToFront();
            _searchList.BringToFront();
            PopulateList();
        }

        private void PopulateList()
        {
            string filter = _searchBox.Text.Trim().ToLowerInvariant();
            int count;

            if (Searching)
            {
                _searchList.Items.Clear();

                foreach (var entry in _historyManager.GetSearches())
                {
                    if (filter.Length > 0)
                    {
                        bool matchTerm = entry.Term != null && entry.Term.ToLowerInvariant().Contains(filter);
                        bool matchUrl = entry.Url != null && entry.Url.ToLowerInvariant().Contains(filter);
                        if (!matchTerm && !matchUrl) continue;
                    }

                    var row = new ListViewItem(entry.SearchedAt.ToString("yyyy-MM-dd  HH:mm"));
                    row.SubItems.Add(entry.Term ?? string.Empty);
                    row.SubItems.Add(entry.Url ?? string.Empty);
                    row.Tag = entry.Url;
                    _searchList.Items.Add(row);
                }

                count = _searchList.Items.Count;
            }
            else
            {
                _listView.Items.Clear();

                foreach (var item in _historyManager.GetHistory())
                {
                    if (filter.Length > 0)
                    {
                        bool matchTitle = item.Title != null && item.Title.ToLowerInvariant().Contains(filter);
                        bool matchUrl = item.Url != null && item.Url.ToLowerInvariant().Contains(filter);
                        if (!matchTitle && !matchUrl) continue;
                    }

                    var row = new ListViewItem(item.VisitedAt.ToString("yyyy-MM-dd  HH:mm"));
                    row.SubItems.Add(item.Title ?? string.Empty);
                    row.SubItems.Add(item.Url ?? string.Empty);
                    row.Tag = item.Url;
                    _listView.Items.Add(row);
                }

                count = _listView.Items.Count;
            }

            _countLabel.Text = count == 1 ? "1 entry" : count + " entries";
        }

        private void OpenSelected()
        {
            var list = Searching ? _searchList : _listView;
            if (list.SelectedItems.Count == 0) return;

            SelectedUrl = list.SelectedItems[0].Tag as string;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void ClearActive()
        {
            bool searching = Searching;

            if (MessageBox.Show(
                    searching ? "Clear your search history?" : "Clear all browsing history?",
                    searching ? "Clear Search History" : "Clear History",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            if (searching) _historyManager.ClearSearches();
            else _historyManager.ClearHistory();

            PopulateList();
        }
    }
}
