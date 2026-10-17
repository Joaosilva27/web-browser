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

            Text = "Browsing History - Brave";
            Size = new Size(800, 520);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(30, 32, 41);
            ForeColor = Color.White;

            var topPanel = new Panel();
            topPanel.Dock = DockStyle.Top;
            topPanel.Height = 50;
            topPanel.BackColor = Color.FromArgb(40, 43, 54);
            topPanel.Padding = new Padding(12, 10, 12, 10);

            var iconLabel = new Label();
            iconLabel.Text = "🦁";
            iconLabel.ForeColor = Color.FromArgb(251, 84, 43);
            iconLabel.Dock = DockStyle.Left;
            iconLabel.Width = 26;
            iconLabel.Font = new Font("Segoe UI", 12F);
            iconLabel.TextAlign = ContentAlignment.MiddleLeft;

            var searchLabel = new Label();
            searchLabel.Text = "Filter:";
            searchLabel.ForeColor = Color.FromArgb(160, 165, 180);
            searchLabel.Dock = DockStyle.Left;
            searchLabel.Width = 45;
            searchLabel.TextAlign = ContentAlignment.MiddleLeft;

            _searchBox = new TextBox();
            _searchBox.Dock = DockStyle.Left;
            _searchBox.Width = 300;
            _searchBox.BackColor = Color.FromArgb(30, 32, 41);
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
            _clearBtn.BackColor = Color.FromArgb(192, 57, 43);
            _clearBtn.ForeColor = Color.White;
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
            _openBtn.Text = "Open in Brave";
            _openBtn.Dock = DockStyle.Right;
            _openBtn.Width = 120;
            _openBtn.FlatStyle = FlatStyle.Flat;
            _openBtn.FlatAppearance.BorderSize = 0;
            _openBtn.BackColor = Color.FromArgb(251, 84, 43);
            _openBtn.ForeColor = Color.White;
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
            _listView.BackColor = Color.FromArgb(30, 32, 41);
            _listView.ForeColor = Color.White;
            _listView.Font = new Font("Segoe UI", 9F);
            _listView.Columns.Add("Timestamp", 140);
            _listView.Columns.Add("Page Title", 300);
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
