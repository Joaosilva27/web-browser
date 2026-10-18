using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace SimpleBrowser
{
    /// <summary>
    /// The "add bookmark" sheet: a name, and the folder to keep it in. Folders
    /// live on the bookmarks bar and in the bookmarks manager, so this is where a
    /// new bookmark is put where it belongs rather than always at the top level.
    /// </summary>
    public class AddBookmarkForm : Form
    {
        private class Choice
        {
            public string Path;
            public string Text;

            public override string ToString()
            {
                return Text;
            }
        }

        private readonly BookmarkManager _bookmarks;
        private readonly TextBox _titleBox;
        private readonly ComboBox _folderBox;

        /// <summary>The name to save under, once the dialog is accepted.</summary>
        public string BookmarkTitle { get; private set; }

        /// <summary>The folder path to save into; empty means the top level.</summary>
        public string Folder { get; private set; }

        public AddBookmarkForm(BookmarkManager bookmarks, string title, string url, string folder)
        {
            _bookmarks = bookmarks;
            BookmarkTitle = title ?? string.Empty;
            Folder = BookmarkManager.Normalize(folder);

            Text = "Add Bookmark";
            AutoScaleMode = AutoScaleMode.None;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(Dpi.Px(468), Dpi.Px(212));
            BackColor = Theme.ContentBackground;
            ForeColor = Theme.TextPrimary;
            Font = new Font("Segoe UI", 9F);

            var nameLabel = new Label();
            nameLabel.Text = "Name";
            nameLabel.AutoSize = true;
            nameLabel.ForeColor = Theme.TextSecondary;
            nameLabel.Location = new Point(Dpi.Px(22), Dpi.Px(20));

            _titleBox = new TextBox();
            _titleBox.Text = string.IsNullOrEmpty(title) ? url : title;
            _titleBox.Location = new Point(Dpi.Px(22), Dpi.Px(42));
            _titleBox.Width = ClientSize.Width - Dpi.Px(44);
            _titleBox.Font = new Font("Segoe UI", 10F);
            _titleBox.BorderStyle = BorderStyle.FixedSingle;
            _titleBox.BackColor = Theme.IsDark ? Color.FromArgb(56, 56, 60) : Color.White;
            _titleBox.ForeColor = Theme.TextPrimary;

            var urlLabel = new Label();
            urlLabel.Text = url;
            urlLabel.AutoEllipsis = true;
            urlLabel.ForeColor = Theme.TextTertiary;
            urlLabel.Location = new Point(Dpi.Px(24), Dpi.Px(72));
            urlLabel.Width = ClientSize.Width - Dpi.Px(48);
            urlLabel.Height = Dpi.Px(18);

            var folderLabel = new Label();
            folderLabel.Text = "Folder";
            folderLabel.AutoSize = true;
            folderLabel.ForeColor = Theme.TextSecondary;
            folderLabel.Location = new Point(Dpi.Px(22), Dpi.Px(98));

            _folderBox = new ComboBox();
            _folderBox.DropDownStyle = ComboBoxStyle.DropDownList;
            _folderBox.Location = new Point(Dpi.Px(22), Dpi.Px(120));
            _folderBox.Width = ClientSize.Width - Dpi.Px(158);
            _folderBox.Font = new Font("Segoe UI", 9.5F);
            _folderBox.FlatStyle = FlatStyle.System;

            var newFolderButton = new AuraButton();
            newFolderButton.Text = "New Folder";
            newFolderButton.Size = new Size(Dpi.Px(120), Dpi.Px(30));
            newFolderButton.Radius = Dpi.Px(8);
            newFolderButton.BackColor = Theme.IsDark ? Color.FromArgb(72, 72, 78) : Color.FromArgb(228, 228, 232);
            newFolderButton.ForeColor = Theme.TextPrimary;
            newFolderButton.Location = new Point(ClientSize.Width - Dpi.Px(134), Dpi.Px(119));
            newFolderButton.Click += delegate { CreateFolder(); };

            var cancel = new AuraButton();
            cancel.Text = "Cancel";
            cancel.Size = new Size(Dpi.Px(92), Dpi.Px(32));
            cancel.Radius = Dpi.Px(8);
            cancel.BackColor = Theme.IsDark ? Color.FromArgb(72, 72, 78) : Color.FromArgb(228, 228, 232);
            cancel.ForeColor = Theme.TextPrimary;
            cancel.Location = new Point(ClientSize.Width - Dpi.Px(114), Dpi.Px(162));
            cancel.Click += delegate
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };

            var save = new AuraButton();
            save.Text = "Save";
            save.Size = new Size(Dpi.Px(104), Dpi.Px(32));
            save.Radius = Dpi.Px(8);
            save.BackColor = Theme.AccentBlue;
            save.Location = new Point(ClientSize.Width - Dpi.Px(226), Dpi.Px(162));
            save.Click += delegate { Accept(); };

            Controls.Add(nameLabel);
            Controls.Add(_titleBox);
            Controls.Add(urlLabel);
            Controls.Add(folderLabel);
            Controls.Add(_folderBox);
            Controls.Add(newFolderButton);
            Controls.Add(cancel);
            Controls.Add(save);

            FillFolders(Folder);

            _titleBox.KeyDown += delegate (object s, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    Accept();
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    e.SuppressKeyPress = true;
                    DialogResult = DialogResult.Cancel;
                    Close();
                }
            };

            Shown += delegate
            {
                _titleBox.Focus();
                _titleBox.SelectAll();
            };
        }

        private void FillFolders(string select)
        {
            _folderBox.Items.Clear();

            var root = new Choice();
            root.Path = string.Empty;
            root.Text = BookmarkManager.RootFolder;
            _folderBox.Items.Add(root);

            foreach (string folder in _bookmarks.GetFolders())
            {
                var choice = new Choice();
                choice.Path = folder;
                choice.Text = Indent(folder);
                _folderBox.Items.Add(choice);
            }

            string wanted = BookmarkManager.Normalize(select);
            for (int i = 0; i < _folderBox.Items.Count; i++)
            {
                var choice = _folderBox.Items[i] as Choice;
                if (choice != null && string.Equals(choice.Path, wanted, StringComparison.OrdinalIgnoreCase))
                {
                    _folderBox.SelectedIndex = i;
                    return;
                }
            }

            _folderBox.SelectedIndex = 0;
        }

        private static string Indent(string folder)
        {
            int depth = 0;
            foreach (char c in folder)
            {
                if (c == '/') depth++;
            }
            return new string(' ', depth * 4) + BookmarkManager.NameOf(folder);
        }

        private void CreateFolder()
        {
            string parent = string.Empty;
            var selected = _folderBox.SelectedItem as Choice;
            if (selected != null) parent = selected.Path;

            string suggested = _bookmarks.UniqueChildName(parent, "New Folder");
            using (var prompt = new TextPromptForm("New Folder", "Name for the new folder:", suggested, "Create"))
            {
                if (prompt.ShowDialog(this) != DialogResult.OK) return;

                string created = _bookmarks.CreateFolder(parent, _bookmarks.UniqueChildName(parent, prompt.Value));
                if (string.IsNullOrEmpty(created)) return;

                Folder = created;
                FillFolders(created);
            }
        }

        private void Accept()
        {
            if (_titleBox.Text.Trim().Length == 0) return;

            var selected = _folderBox.SelectedItem as Choice;

            BookmarkTitle = _titleBox.Text.Trim();
            Folder = selected == null ? string.Empty : selected.Path;

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
