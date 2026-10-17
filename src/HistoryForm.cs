using System;
using System.Drawing;
using System.Windows.Forms;

namespace SimpleBrowser
{
    public class HistoryForm : Form
    {
        private readonly HistoryManager _historyManager;
        private ListView _listView;
        private TextBox _searchBox;
        private Button _openBtn;
        private Button _clearBtn;

        public string SelectedUrl { get; private set; }

        public HistoryForm(HistoryManager historyManager)
        {
            _historyManager = historyManager;

            Text = "Browsing History — Aether";
            Size = new Size(820, 540);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(10, 11, 16);
            ForeColor = Color.White;

            var topPanel = new Panel();
            topPanel.Dock = DockStyle.Top;
            topPanel.Height = 52;
            topPanel.BackColor = Color.FromArgb(17, 19, 28);
            topPanel.Padding = new Padding(14, 10, 14, 10);

            var iconLabel = new Label();
            iconLabel.Text = "✦";
            iconLabel.ForeColor = Color.FromArgb(0, 242, 254);
            iconLabel.Dock = DockStyle.Left;
            iconLabel.Width = 26;
            iconLabel.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            iconLabel.TextAlign = ContentAlignment.MiddleLeft;

            var searchLabel = new Label();
            searchLabel.Text = "Filter:";
            searchLabel.ForeColor = Color.FromArgb(134, 134, 139);
            searchLabel.Dock = DockStyle.Left;
            searchLabel.Width = 45;
            searchLabel.TextAlign = ContentAlignment.MiddleLeft;

            _searchBox = new TextBox();
            _searchBox.Dock = DockStyle.Left;
            _searchBox.Width = 300;
            _searchBox.BackColor = Color.FromArgb(12, 13, 20);
            _searchBox.ForeColor = Color.White;
            _searchBox.Font = new Font("Segoe UI", 9.5F);
            _searchBox.BorderStyle = BorderStyle.FixedSingle;
            _searchBox.TextChanged += delegate { PopulateList(); };

            _clearBtn = new Button();
            _clearBtn.Text = "Clear History";
            _clearBtn.Dock = DockStyle.Right;
            _clearBtn.Width = 110;
            _clearBtn.FlatStyle = FlatStyle.Flat;
            _clearBtn.FlatAppearance.BorderSize = 0;
            _clearBtn.BackColor = Color.FromArgb(40, 20, 25);
            _clearBtn.ForeColor = Color.FromArgb(255, 100, 100);
            _clearBtn.Cursor = Cursors.Hand;
            _clearBtn.Click += delegate
            {
                if (MessageBox.Show("Are you sure you want to clear your browsing history?", "Clear History", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    _historyManager.ClearHistory();
                    PopulateList();
                }
            };

            _openBtn = new Button();
            _openBtn.Text = "Open in Aether";
            _openBtn.Dock = DockStyle.Right;
            _openBtn.Width = 120;
            _openBtn.FlatStyle = FlatStyle.Flat;
            _openBtn.FlatAppearance.BorderSize = 0;
            _openBtn.BackColor = Color.FromArgb(0, 242, 254);
            _openBtn.ForeColor = Color.FromArgb(10, 12, 18);
            _openBtn.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _openBtn.Cursor = Cursors.Hand;
            _openBtn.Click += delegate { OpenSelected(); };

            topPanel.Controls.Add(_clearBtn);
            topPanel.Controls.Add(_openBtn);
            topPanel.Controls.Add(_searchBox);
            topPanel.Controls.Add(searchLabel);
            topPanel.Controls.Add(iconLabel);

            _listView = new ListView();
            _listView.Dock = DockStyle.Fill;
            _listView.View = View.Details;
            _listView.FullRowSelect = true;
            _listView.GridLines = true;
            _listView.BackColor = Color.FromArgb(12, 13, 20);
            _listView.ForeColor = Color.FromArgb(245, 245, 247);
            _listView.Font = new Font("Segoe UI", 9F);
            _listView.Columns.Add("Timestamp", 150);
            _listView.Columns.Add("Page Title", 310);
            _listView.Columns.Add("Destination URL", 320);

            _listView.DoubleClick += delegate { OpenSelected(); };

            Controls.Add(_listView);
            Controls.Add(topPanel);

            PopulateList();
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

                var lvi = new ListViewItem(item.VisitedAt.ToString("yyyy-MM-dd HH:mm:ss"));
                lvi.SubItems.Add(item.Title ?? string.Empty);
                lvi.SubItems.Add(item.Url ?? string.Empty);
                lvi.Tag = item.Url;
                _listView.Items.Add(lvi);
            }
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
