using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace SimpleBrowser
{
    /// <summary>
    /// The Ctrl+Shift+A palette: type a few letters, press Enter, and the tab
    /// with that title or address comes forward. Useful once the strip is too
    /// narrow to read.
    /// </summary>
    public class TabSearchForm : Form
    {
        /// <summary>One tab, as the palette needs to see it.</summary>
        public class Entry
        {
            public int Index;
            public string Title;
            public string Url;
        }

        private readonly List<Entry> _all = new List<Entry>();
        private readonly List<Entry> _shown = new List<Entry>();

        private TextBox _filter;
        private ListBox _list;
        private Label _hint;
        private int _selected = -1;

        public TabSearchForm(List<Entry> entries)
        {
            if (entries != null) _all.AddRange(entries);

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            AutoScaleMode = AutoScaleMode.None;
            Size = new Size(Dpi.Px(472), Dpi.Px(392));
            BackColor = Theme.CardBackground;
            ForeColor = Theme.TextPrimary;
            ShowInTaskbar = false;
            DoubleBuffered = true;

            var title = new Label();
            title.Text = "Switch to tab";
            title.Font = new Font("Segoe UI Semibold", 10.5F);
            title.ForeColor = Theme.TextTertiary;
            title.AutoSize = true;
            title.Location = new Point(Dpi.Px(18), Dpi.Px(14));

            _filter = new TextBox();
            _filter.BorderStyle = BorderStyle.None;
            _filter.Font = new Font("Segoe UI", 13F);
            _filter.BackColor = Theme.CardBackground;
            _filter.ForeColor = Theme.TextPrimary;
            _filter.Location = new Point(Dpi.Px(18), Dpi.Px(40));
            _filter.Size = new Size(Dpi.Px(436), Dpi.Px(30));
            _filter.TextChanged += delegate { Rebuild(); };

            var divider = new Panel();
            divider.BackColor = Theme.CardBorder;
            divider.Location = new Point(Dpi.Px(1), Dpi.Px(82));
            divider.Size = new Size(Dpi.Px(470), 1);

            _list = new ListBox();
            _list.BorderStyle = BorderStyle.None;
            _list.Font = new Font("Segoe UI", 9.75F);
            _list.BackColor = Theme.CardBackground;
            _list.ForeColor = Theme.TextPrimary;
            _list.IntegralHeight = false;
            _list.Location = new Point(Dpi.Px(8), Dpi.Px(90));
            _list.Size = new Size(Dpi.Px(456), Dpi.Px(252));
            _list.DoubleClick += delegate { Accept(); };
            _list.KeyDown += OnListKey;

            _hint = new Label();
            _hint.Text = "Enter to switch \u00B7 Esc to close";
            _hint.Font = new Font("Segoe UI", 8.25F);
            _hint.ForeColor = Theme.TextTertiary;
            _hint.AutoSize = true;
            _hint.Location = new Point(Dpi.Px(18), Dpi.Px(354));

            Controls.Add(title);
            Controls.Add(_filter);
            Controls.Add(divider);
            Controls.Add(_list);
            Controls.Add(_hint);

            _filter.KeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Down && _shown.Count > 0)
                {
                    _list.Focus();
                    _list.SelectedIndex = 0;
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.Enter)
                {
                    Accept();
                    e.Handled = true;
                }
            };

            Rebuild();
        }

        /// <summary>The tab the user picked, or -1 when the palette was closed.</summary>
        public int SelectedIndex
        {
            get { return _selected; }
        }

        private void OnListKey(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                Accept();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Up && _list.SelectedIndex <= 0)
            {
                _filter.Focus();
                e.Handled = true;
            }
        }

        private void Rebuild()
        {
            string query = _filter.Text.Trim();

            _shown.Clear();
            _list.Items.Clear();

            for (int i = 0; i < _all.Count; i++)
            {
                Entry entry = _all[i];
                string title = string.IsNullOrEmpty(entry.Title) ? entry.Url : entry.Title;
                string url = entry.Url ?? "";

                if (query.Length == 0
                    || title.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0
                    || url.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _shown.Add(entry);
                    _list.Items.Add(title + "   \u2014   " + SiteSettings.HostOf(url));
                }
            }

            if (_shown.Count > 0)
            {
                _list.SelectedIndex = 0;
                _hint.Text = _shown.Count == 1
                    ? "1 tab \u00B7 Enter to switch \u00B7 Esc to close"
                    : _shown.Count + " tabs \u00B7 Enter to switch \u00B7 Esc to close";
            }
            else
            {
                _hint.Text = "No tab matches \u201C" + query + "\u201D";
            }
        }

        private void Accept()
        {
            if (_shown.Count == 0) return;

            int index = _list.SelectedIndex;
            if (index < 0 || index >= _shown.Count) index = 0;

            _selected = _shown[index].Index;
            DialogResult = DialogResult.OK;
            Close();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            Region = Theme.RoundedRegion(ClientRectangle, Dpi.Px(14));
            _filter.Focus();
        }

        protected override bool ProcessDialogKey(Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
                Close();
                return true;
            }
            return base.ProcessDialogKey(keyData);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= 0x20000; // CS_DROPSHADOW
                return cp;
            }
        }

        /// <summary>Draws the hairline around the card and under the filter.</summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            using (var pen = new Pen(Theme.CardBorder, 1))
            {
                e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
            }
        }
    }
}
