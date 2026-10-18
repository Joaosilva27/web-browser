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
        private TextBox _searchBox;
        private Label _countLabel;

        public string SelectedUrl { get; private set; }

        public HistoryForm(HistoryManager historyManager)
        {
            _historyManager = historyManager;

            Text = "History";
            AutoScaleMode = AutoScaleMode.None;
            Size = new Size(Dpi.Px(840), Dpi.Px(560));
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

            var searchWrap = new Panel();
            searchWrap.Location = new Point(Dpi.Px(120), Dpi.Px(17));
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
            _countLabel.Location = new Point(Dpi.Px(122), Dpi.Px(52));

            var openBtn = new AuraButton();
            openBtn.Text = "Open";
            openBtn.Size = new Size(Dpi.Px(96), Dpi.Px(32));
            openBtn.Radius = Dpi.Px(8);
            openBtn.BackColor = Theme.AccentBlue;
            openBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            openBtn.Location = new Point(topPanel.Width - Dpi.Px(136), Dpi.Px(17));
            openBtn.Click += delegate { OpenSelected(); };

            var clearBtn = new AuraButton();
            clearBtn.Text = "Clear";
            clearBtn.Size = new Size(Dpi.Px(88), Dpi.Px(32));
            clearBtn.Radius = Dpi.Px(8);
            clearBtn.BackColor = Theme.IsDark
                ? Color.FromArgb(72, 72, 78)
                : Color.FromArgb(228, 228, 232);
            clearBtn.ForeColor = Theme.TextPrimary;
            clearBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            clearBtn.Location = new Point(topPanel.Width - Dpi.Px(234), Dpi.Px(17));
            clearBtn.Click += delegate
            {
                if (MessageBox.Show(
                        "Clear all browsing history?",
                        "Clear History",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    _historyManager.ClearHistory();
                    PopulateList();
                }
            };

            topPanel.Controls.Add(title);
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
            _listView.Columns.Add("Address", Dpi.Px(360));
            _listView.DoubleClick += delegate { OpenSelected(); };

            Controls.Add(_listView);
            Controls.Add(topPanel);

            PopulateList();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            SendMessage(_searchBox.Handle, EM_SETCUEBANNER, (IntPtr)1, "Search history");
        }

        private void PopulateList()
        {
            _listView.Items.Clear();
            string filter = _searchBox.Text.Trim().ToLowerInvariant();

            foreach (var item in _historyManager.GetHistory())
            {
                if (!string.IsNullOrEmpty(filter))
                {
                    bool matchTitle = (item.Title != null && item.Title.ToLowerInvariant().Contains(filter));
                    bool matchUrl = (item.Url != null && item.Url.ToLowerInvariant().Contains(filter));
                    if (!matchTitle && !matchUrl) continue;
                }

                var lvi = new ListViewItem(item.VisitedAt.ToString("yyyy-MM-dd  HH:mm"));
                lvi.SubItems.Add(item.Title ?? string.Empty);
                lvi.SubItems.Add(item.Url ?? string.Empty);
                lvi.Tag = item.Url;
                _listView.Items.Add(lvi);
            }

            _countLabel.Text = _listView.Items.Count == 1
                ? "1 entry"
                : _listView.Items.Count + " entries";
        }

        private void OpenSelected()
        {
            if (_listView.SelectedItems.Count > 0)
            {
                SelectedUrl = _listView.SelectedItems[0].Tag as string;
                DialogResult = DialogResult.OK;
                Close();
            }
        }
    }
}
