using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace SimpleBrowser
{
    /// <summary>
    /// The downloads list, opened from the toolbar button, Ctrl+J or the Aura
    /// menu. It is a normal window rather than a dialog, because downloads
    /// finish while the user is doing something else and the list has to keep
    /// up with them.
    /// </summary>
    public class DownloadsForm : Form
    {
        private readonly ListView _list;
        private readonly Label _count;

        public DownloadsForm()
        {
            Text = "Downloads";
            AutoScaleMode = AutoScaleMode.None;
            Size = new Size(Dpi.Px(720), Dpi.Px(480));
            MinimumSize = new Size(Dpi.Px(560), Dpi.Px(320));
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Theme.ContentBackground;
            ForeColor = Theme.TextPrimary;
            Font = new Font("Segoe UI", 9F);
            ShowInTaskbar = true;

            var topPanel = new Panel();
            topPanel.Dock = DockStyle.Top;
            topPanel.Height = Dpi.Px(62);
            topPanel.BackColor = Theme.CardBackground;
            topPanel.Paint += delegate (object s, PaintEventArgs e)
            {
                using (var pen = new Pen(Theme.CardBorder, 1))
                {
                    e.Graphics.DrawLine(pen, 0, topPanel.Height - 1, topPanel.Width, topPanel.Height - 1);
                }
            };

            var title = new Label();
            title.Text = "Downloads";
            title.Font = new Font("Segoe UI Semibold", 13F);
            title.ForeColor = Theme.TextPrimary;
            title.AutoSize = true;
            title.Location = new Point(Dpi.Px(20), Dpi.Px(17));

            _count = new Label();
            _count.Font = new Font("Segoe UI", 8.75F);
            _count.ForeColor = Theme.TextTertiary;
            _count.AutoSize = true;
            _count.Location = new Point(Dpi.Px(126), Dpi.Px(24));

            var clear = new AuraButton();
            clear.Text = "Clear Finished";
            clear.Size = new Size(Dpi.Px(126), Dpi.Px(32));
            clear.Radius = Dpi.Px(8);
            clear.BackColor = Theme.IsDark ? Color.FromArgb(72, 72, 78) : Color.FromArgb(228, 228, 232);
            clear.ForeColor = Theme.TextPrimary;
            clear.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            clear.Location = new Point(topPanel.Width - Dpi.Px(146), Dpi.Px(16));
            clear.Click += delegate
            {
                DownloadsManager.Current.ClearFinished();
            };

            var openFolder = new AuraButton();
            openFolder.Text = "Open Folder";
            openFolder.Size = new Size(Dpi.Px(112), Dpi.Px(32));
            openFolder.Radius = Dpi.Px(8);
            openFolder.BackColor = Theme.IsDark ? Color.FromArgb(72, 72, 78) : Color.FromArgb(228, 228, 232);
            openFolder.ForeColor = Theme.TextPrimary;
            openFolder.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            openFolder.Location = new Point(topPanel.Width - Dpi.Px(272), Dpi.Px(16));
            openFolder.Click += delegate { OpenDownloadsFolder(); };

            topPanel.Controls.Add(title);
            topPanel.Controls.Add(_count);
            topPanel.Controls.Add(clear);
            topPanel.Controls.Add(openFolder);

            _list = new ListView();
            _list.Dock = DockStyle.Fill;
            _list.View = View.Details;
            _list.FullRowSelect = true;
            _list.GridLines = false;
            _list.HideSelection = false;
            _list.BorderStyle = BorderStyle.None;
            _list.BackColor = Theme.ContentBackground;
            _list.ForeColor = Theme.TextPrimary;
            _list.Font = new Font("Segoe UI", 9F);
            _list.Columns.Add("File", Dpi.Px(280));
            _list.Columns.Add("Status", Dpi.Px(150));
            _list.Columns.Add("Size", Dpi.Px(170));
            _list.Columns.Add("Started", Dpi.Px(140));
            _list.MouseDoubleClick += delegate { OpenSelected(); };

            var menu = new ContextMenuStrip();
            menu.Items.Add(new ToolStripMenuItem("Open", null, delegate { OpenSelected(); }));
            menu.Items.Add(new ToolStripMenuItem("Show in Folder", null, delegate { RevealSelected(); }));
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(new ToolStripMenuItem("Cancel", null, delegate { CancelSelected(); }));
            menu.Items.Add(new ToolStripMenuItem("Remove from List", null, delegate { RemoveSelected(); }));
            _list.ContextMenuStrip = menu;

            var bottom = new Panel();
            bottom.Dock = DockStyle.Bottom;
            bottom.Height = Dpi.Px(54);
            bottom.BackColor = Theme.CardBackground;
            bottom.Paint += delegate (object s, PaintEventArgs e)
            {
                using (var pen = new Pen(Theme.CardBorder, 1))
                {
                    e.Graphics.DrawLine(pen, 0, 0, bottom.Width, 0);
                }
            };

            var open = MakeButton("Open", Dpi.Px(20));
            open.BackColor = Theme.AccentBlue;
            open.Click += delegate { OpenSelected(); };

            var reveal = MakeButton("Show in Folder", Dpi.Px(124));
            reveal.Click += delegate { RevealSelected(); };

            var cancel = MakeButton("Cancel", Dpi.Px(248));
            cancel.Click += delegate { CancelSelected(); };

            var remove = MakeButton("Remove", Dpi.Px(344));
            remove.Click += delegate { RemoveSelected(); };

            bottom.Controls.Add(open);
            bottom.Controls.Add(reveal);
            bottom.Controls.Add(cancel);
            bottom.Controls.Add(remove);

            // A ListView docked Fill and a panel docked Bottom need to be added
            // in the right order for the docking to come out as intended.
            Controls.Add(_list);
            Controls.Add(topPanel);
            Controls.Add(bottom);

            DownloadsManager.Current.Changed += OnDownloadsChanged;
            RefreshList();
        }

        private AuraButton MakeButton(string text, int left)
        {
            var button = new AuraButton();
            button.Text = text;
            button.Size = new Size(Dpi.Px(text.Length > 10 ? 116 : 88), Dpi.Px(32));
            button.Radius = Dpi.Px(8);
            button.Location = new Point(left, Dpi.Px(11));
            button.BackColor = Theme.IsDark ? Color.FromArgb(72, 72, 78) : Color.FromArgb(228, 228, 232);
            button.ForeColor = Theme.TextPrimary;
            return button;
        }

        private void OnDownloadsChanged()
        {
            if (IsDisposed || !IsHandleCreated) return;

            try
            {
                BeginInvoke((MethodInvoker)RefreshList);
            }
            catch
            {
                // The window is closing; nothing to refresh.
            }
        }

        private void RefreshList()
        {
            if (IsDisposed) return;

            var selected = SelectedRecord();

            _list.BeginUpdate();
            _list.Items.Clear();

            int active = 0;
            foreach (var record in DownloadsManager.Current.GetItems())
            {
                if (record.IsActive) active++;

                var row = new ListViewItem(record.FileName ?? "download");
                row.SubItems.Add(record.StateText);
                row.SubItems.Add(record.SizeText);
                row.SubItems.Add(record.StartedAt == DateTime.MinValue
                    ? ""
                    : record.StartedAt.ToString("g"));
                row.Tag = record;
                row.ForeColor = Theme.TextPrimary;
                _list.Items.Add(row);

                if (record == selected) row.Selected = true;
            }

            _list.EndUpdate();

            int total = _list.Items.Count;
            _count.Text = total == 0
                ? "Nothing downloaded yet"
                : (active > 0
                    ? total + (total == 1 ? " download, " : " downloads, ") + active + " in progress"
                    : total + (total == 1 ? " download" : " downloads"));
        }

        private DownloadRecord SelectedRecord()
        {
            if (_list.SelectedItems.Count == 0) return null;
            return _list.SelectedItems[0].Tag as DownloadRecord;
        }

        private void OpenSelected()
        {
            var record = SelectedRecord();
            if (record == null) return;

            if (!record.IsComplete)
            {
                MessageBox.Show(this, "That download has not finished yet.", "Downloads",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                if (!File.Exists(record.FilePath))
                {
                    MessageBox.Show(this, "That file is no longer where it was saved.",
                        "Downloads", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                Process.Start(new ProcessStartInfo(record.FilePath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "The file could not be opened: " + ex.Message,
                    "Downloads", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void RevealSelected()
        {
            var record = SelectedRecord();
            if (record == null) return;

            try
            {
                if (!string.IsNullOrEmpty(record.FilePath) && File.Exists(record.FilePath))
                {
                    Process.Start("explorer.exe", "/select,\"" + record.FilePath + "\"");
                }
                else
                {
                    OpenDownloadsFolder();
                }
            }
            catch
            {
                OpenDownloadsFolder();
            }
        }

        private void CancelSelected()
        {
            var record = SelectedRecord();
            if (record == null || !record.IsActive) return;

            try
            {
                if (record.Operation != null) record.Operation.Cancel();
            }
            catch
            {
                // It may have finished a moment ago.
            }
        }

        private void RemoveSelected()
        {
            var record = SelectedRecord();
            if (record == null) return;

            // Removing something that is still running also stops it, which is
            // what "remove" means for a download in progress.
            if (record.IsActive)
            {
                try { if (record.Operation != null) record.Operation.Cancel(); }
                catch { }
            }

            DownloadsManager.Current.Remove(record);
        }

        private void OpenDownloadsFolder()
        {
            try
            {
                string folder = DefaultFolder();
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                Process.Start("explorer.exe", "\"" + folder + "\"");
            }
            catch
            {
                // Explorer is not always willing; nothing else to try.
            }
        }

        private static string DefaultFolder()
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            DownloadsManager.Current.Changed -= OnDownloadsChanged;
            base.OnFormClosed(e);
        }
    }
}
