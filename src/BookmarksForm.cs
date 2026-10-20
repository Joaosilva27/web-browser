using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SimpleBrowser
{
    /// <summary>
    /// Browse, organise and open bookmarks. Folders nest, so the left rail is a
    /// tree: "All Bookmarks" for everything, then the top level and every folder
    /// under it. Rows on the right are the bookmarks in whatever is selected.
    /// </summary>
    public class BookmarksForm : Form
    {
        private const int EM_SETCUEBANNER = 0x1501;

        /// <summary>Tag used by the "All Bookmarks" node, which has no path.</summary>
        private const string Everything = "*";

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);

        private readonly BookmarkManager _bookmarks;
        private TreeView _tree;
        private readonly System.Collections.Generic.HashSet<string> _expanded =
            new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private ListView _listView;
        private TextBox _searchBox;
        private Label _countLabel;

        public string SelectedUrl { get; private set; }

        /// <summary>True when the set changed, so the host can refresh its star.</summary>
        public bool Changed { get; private set; }

        public BookmarksForm(BookmarkManager bookmarks)
        {
            _bookmarks = bookmarks;
            AppIcon.Apply(this);

            Text = "Bookmarks";
            AutoScaleMode = AutoScaleMode.None;
            Size = new Size(Dpi.Px(920), Dpi.Px(620));
            MinimumSize = new Size(Dpi.Px(720), Dpi.Px(460));
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
            searchWrap.Size = new Size(Dpi.Px(300), Dpi.Px(32));
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

            // Buttons sit against the right edge, in the order they are used most.
            int buttonY = Dpi.Px(17);
            var openButton = MakeButton("Open", Dpi.Px(96), Theme.AccentBlue, false);
            openButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            openButton.Location = new Point(topPanel.Width - Dpi.Px(136), buttonY);
            openButton.Click += delegate { OpenSelected(); };

            var deleteButton = MakeButton("Delete", Dpi.Px(88), Neutral(), true);
            deleteButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            deleteButton.Location = new Point(topPanel.Width - Dpi.Px(232), buttonY);
            deleteButton.Click += delegate { DeleteFocused(); };

            var renameButton = MakeButton("Rename", Dpi.Px(96), Neutral(), true);
            renameButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            renameButton.Location = new Point(topPanel.Width - Dpi.Px(336), buttonY);
            renameButton.Click += delegate { RenameFocused(); };

            var newFolderButton = MakeButton("New Folder", Dpi.Px(118), Neutral(), true);
            newFolderButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            newFolderButton.Location = new Point(topPanel.Width - Dpi.Px(462), buttonY);
            newFolderButton.Click += delegate { NewFolder(); };

            topPanel.Controls.Add(title);
            topPanel.Controls.Add(searchWrap);
            topPanel.Controls.Add(_countLabel);
            topPanel.Controls.Add(openButton);
            topPanel.Controls.Add(deleteButton);
            topPanel.Controls.Add(renameButton);
            topPanel.Controls.Add(newFolderButton);

            _tree = new TreeView();
            _tree.Dock = DockStyle.Left;
            _tree.Width = Dpi.Px(238);
            _tree.BorderStyle = BorderStyle.None;
            _tree.HideSelection = false;
            _tree.ShowLines = false;
            _tree.ShowPlusMinus = true;
            _tree.ShowRootLines = false;
            _tree.ItemHeight = Dpi.Px(24);
            _tree.Indent = Dpi.Px(16);
            _tree.BackColor = Theme.IsDark
                ? Color.FromArgb(32, 32, 35)
                : Color.FromArgb(248, 248, 250);
            _tree.ForeColor = Theme.TextPrimary;
            _tree.Font = new Font("Segoe UI", 9.5F);
            _tree.AfterSelect += delegate { PopulateList(); };
            _tree.MouseUp += delegate (object s, MouseEventArgs e) { TreeContextMenu(e); };

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
            _listView.Columns.Add("Name", Dpi.Px(300));
            _listView.Columns.Add("Folder", Dpi.Px(190));
            _listView.Columns.Add("Address", Dpi.Px(380));
            _listView.DoubleClick += delegate { OpenSelected(); };
            _listView.KeyDown += delegate (object s, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Delete) DeleteFocused();
            };
            _listView.MouseUp += delegate (object s, MouseEventArgs e) { ListContextMenu(e); };

            Controls.Add(_listView);
            Controls.Add(_tree);
            Controls.Add(topPanel);

            PopulateTree(null);
        }

        private Button MakeButton(string text, int width, Color back, bool light)
        {
            var button = new TogeButton();
            button.Text = text;
            button.Size = new Size(width, Dpi.Px(32));
            button.Radius = Dpi.Px(8);
            button.BackColor = back;
            if (light) button.ForeColor = Theme.TextPrimary;
            return button;
        }

        private static Color Neutral()
        {
            return Theme.IsDark ? Color.FromArgb(72, 72, 78) : Color.FromArgb(228, 228, 232);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            SendMessage(_searchBox.Handle, EM_SETCUEBANNER, (IntPtr)1, "Search bookmarks");
        }

        /// <summary>The folder path the tree is pointing at, or null for "everything".</summary>
        private string SelectedFolder
        {
            get
            {
                if (_tree.SelectedNode == null) return null;
                string tag = _tree.SelectedNode.Tag as string;
                return tag == Everything ? null : tag;
            }
        }

        private bool FolderSelected
        {
            get
            {
                if (_tree.SelectedNode == null) return false;
                string tag = _tree.SelectedNode.Tag as string;
                return tag != Everything;
            }
        }

        // ── Painting ─────────────────────────────────────────────────────────

        private void PopulateTree(string select)
        {
            if (select == null && _tree.SelectedNode != null)
            {
                string tag = _tree.SelectedNode.Tag as string;
                if (tag != null) select = tag;
            }

            _tree.BeginUpdate();
            RememberExpansion(_tree.Nodes);
            _tree.Nodes.Clear();

            int total = _bookmarks.GetBookmarks().Count;
            var all = new TreeNode("All Bookmarks (" + total + ")");
            all.Tag = Everything;

            // The top level holds the bookmarks that live outside every folder.
            // Its count is the same as "All Bookmarks" once nesting is counted,
            // so it is shown without one.
            var top = new TreeNode(BookmarkManager.RootFolder);
            top.Tag = string.Empty;

            AddChildren(top, string.Empty);

            _tree.Nodes.Add(all);
            _tree.Nodes.Add(top);
            all.Expand();
            top.Expand();

            RestoreExpansion(_tree.Nodes);

            _tree.EndUpdate();

            SelectNode(_tree.Nodes, select == null ? Everything : select);
        }

        private void AddChildren(TreeNode parent, string folder)
        {
            foreach (string child in _bookmarks.GetChildFolders(folder))
            {
                var node = new TreeNode(Label(BookmarkManager.NameOf(child), child));
                node.Tag = child;
                AddChildren(node, child);
                parent.Nodes.Add(node);
            }
        }

        private string Label(string name, string folder)
        {
            int count = _bookmarks.CountBookmarks(folder, true);
            return count > 0 ? name + " (" + count + ")" : name;
        }

        private void SelectNode(TreeNodeCollection nodes, string tag)
        {
            foreach (TreeNode node in nodes)
            {
                if (Matches(node, tag))
                {
                    _tree.SelectedNode = node;
                    return;
                }

                if (node.Nodes.Count > 0 && IsAncestor(tag, node.Tag as string))
                {
                    node.Expand();
                    SelectNode(node.Nodes, tag);
                    if (_tree.SelectedNode != null) return;
                }
            }

            if (nodes.Count > 0) _tree.SelectedNode = nodes[0];
        }

        private static bool Matches(TreeNode node, string tag)
        {
            string value = node.Tag as string;
            if (tag == Everything) return value == Everything;
            return value != null && value != Everything &&
                   string.Equals(value, tag, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsAncestor(string tag, string candidate)
        {
            // "All Bookmarks" holds everything and the top level holds every
            // folder, so both are ancestors of a nested folder.
            if (candidate == Everything) return true;
            if (tag == Everything) return false;
            if (candidate.Length == 0) return tag.Length > 0;

            return tag.StartsWith(candidate + "/", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Remembers the open branches so a rebuild keeps them open.</summary>
        private void RememberExpansion(TreeNodeCollection nodes)
        {
            foreach (TreeNode node in nodes)
            {
                string tag = node.Tag as string;
                if (tag != null && node.IsExpanded) _expanded.Add(tag);
                if (node.Nodes.Count > 0) RememberExpansion(node.Nodes);
            }
        }

        private void RestoreExpansion(TreeNodeCollection nodes)
        {
            foreach (TreeNode node in nodes)
            {
                string tag = node.Tag as string;
                if (tag != null && _expanded.Contains(tag)) node.Expand();
                if (node.Nodes.Count > 0) RestoreExpansion(node.Nodes);
            }
        }

        private void PopulateList()
        {
            string folder = SelectedFolder;
            string filter = _searchBox.Text.Trim().ToLowerInvariant();

            _listView.BeginUpdate();
            _listView.Items.Clear();

            bool all = !FolderSelected;
            List<BookmarkItem> source = all
                ? _bookmarks.GetBookmarks()
                : _bookmarks.GetBookmarks(folder, false);

            foreach (var bookmark in source)
            {
                if (filter.Length > 0)
                {
                    bool matchTitle = bookmark.Title != null &&
                                      bookmark.Title.ToLowerInvariant().Contains(filter);
                    bool matchUrl = bookmark.Url != null &&
                                    bookmark.Url.ToLowerInvariant().Contains(filter);
                    if (!matchTitle && !matchUrl) continue;
                }

                var row = new ListViewItem(bookmark.Title ?? string.Empty);
                row.SubItems.Add(bookmark.Folder == null || bookmark.Folder.Length == 0
                    ? BookmarkManager.RootFolder
                    : bookmark.Folder);
                row.SubItems.Add(bookmark.Url ?? string.Empty);
                row.Tag = bookmark.Url;
                _listView.Items.Add(row);
            }

            _listView.EndUpdate();

            _countLabel.Text = _listView.Items.Count == 1
                ? "1 bookmark"
                : _listView.Items.Count + " bookmarks";
        }

        // ── Acting on the selection ──────────────────────────────────────────

        private void OpenSelected()
        {
            if (_listView.SelectedItems.Count == 0) return;
            OpenUrl(_listView.SelectedItems[0].Tag as string);
        }

        private void OpenUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return;

            SelectedUrl = url;
            DialogResult = DialogResult.OK;
            Close();
        }

        private string SelectedBookmarkUrl()
        {
            if (_listView.SelectedItems.Count == 0) return null;
            return _listView.SelectedItems[0].Tag as string;
        }

        private void DeleteFocused()
        {
            string url = SelectedBookmarkUrl();
            if (!string.IsNullOrEmpty(url))
            {
                _bookmarks.RemoveBookmark(url);
                Changed = true;
                PopulateTree(SelectedFolder);
                return;
            }

            DeleteFolder();
        }

        private void DeleteFolder()
        {
            if (_tree.SelectedNode == null || !FolderSelected) return;

            string folder = _tree.SelectedNode.Tag as string;
            if (folder == null) return;

            string name = folder.Length == 0 ? BookmarkManager.RootFolder : BookmarkManager.NameOf(folder);
            if (folder.Length == 0)
            {
                int loose = _bookmarks.CountBookmarks(string.Empty, false);
                if (loose == 0) return;
                if (MessageBox.Show(this,
                        "Delete the " + loose + " bookmarks at the top level?",
                        "Delete Bookmarks", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) != DialogResult.OK)
                {
                    return;
                }

                foreach (var bookmark in _bookmarks.GetBookmarks(string.Empty, false))
                {
                    _bookmarks.RemoveBookmark(bookmark.Url);
                }

                Changed = true;
                PopulateTree(string.Empty);
                return;
            }

            int count = _bookmarks.CountBookmarks(folder, true);
            string question = count == 0
                ? "Delete the folder \"" + name + "\"?"
                : "Delete the folder \"" + name + "\" and the " + count +
                  (count == 1 ? " bookmark inside it?" : " bookmarks inside it?");

            if (MessageBox.Show(this, question, "Delete Folder",
                    MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) != DialogResult.OK)
            {
                return;
            }

            _bookmarks.DeleteFolder(folder);
            Changed = true;
            PopulateTree(BookmarkManager.ParentOf(folder));
        }

        private void RenameFocused()
        {
            string url = SelectedBookmarkUrl();
            if (!string.IsNullOrEmpty(url))
            {
                var bookmark = _bookmarks.Find(url);
                if (bookmark == null) return;

                using (var prompt = new TextPromptForm("Rename Bookmark", "Name:", bookmark.Title, "Rename"))
                {
                    if (prompt.ShowDialog(this) != DialogResult.OK) return;
                    if (_bookmarks.RenameBookmark(url, prompt.Value))
                    {
                        Changed = true;
                        PopulateTree(SelectedFolder);
                    }
                }
                return;
            }

            RenameFolder();
        }

        private void RenameFolder()
        {
            if (_tree.SelectedNode == null || !FolderSelected) return;

            string folder = _tree.SelectedNode.Tag as string;
            if (string.IsNullOrEmpty(folder)) return;

            using (var prompt = new TextPromptForm("Rename Folder", "Name:", BookmarkManager.NameOf(folder), "Rename"))
            {
                if (prompt.ShowDialog(this) != DialogResult.OK) return;

                if (_bookmarks.RenameFolder(folder, prompt.Value))
                {
                    Changed = true;
                    PopulateTree(BookmarkManager.Combine(BookmarkManager.ParentOf(folder), prompt.Value));
                }
            }
        }

        private void NewFolder()
        {
            if (_tree.SelectedNode == null) return;

            string parent = SelectedFolder;
            if (parent == null) parent = string.Empty;   // "All Bookmarks" means the top level

            string suggested = _bookmarks.UniqueChildName(parent, "New Folder");
            using (var prompt = new TextPromptForm("New Folder", "Name for the new folder:", suggested, "Create"))
            {
                if (prompt.ShowDialog(this) != DialogResult.OK) return;

                string created = _bookmarks.CreateFolder(parent, _bookmarks.UniqueChildName(parent, prompt.Value));
                if (string.IsNullOrEmpty(created)) return;

                Changed = true;
                PopulateTree(created);
            }
        }

        // ── Context menus ────────────────────────────────────────────────────

        private void TreeContextMenu(MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;

            TreeNode node = _tree.GetNodeAt(e.X, e.Y);
            if (node == null) return;
            _tree.SelectedNode = node;

            string folder = node.Tag as string;
            if (folder == Everything) folder = null;

            var menu = new ContextMenuStrip();
            menu.ShowImageMargin = false;
            menu.Font = Font;

            var newFolder = new ToolStripMenuItem("New Folder\u2026", null, delegate { NewFolder(); });
            menu.Items.Add(newFolder);

            if (folder != null)
            {
                var rename = new ToolStripMenuItem("Rename\u2026", null, delegate { RenameFolder(); });
                menu.Items.Add(rename);

                if (folder.Length > 0)
                {
                    menu.Items.Add(new ToolStripSeparator());

                    var moveTo = new ToolStripMenuItem("Move to Folder");
                    BuildFolderChoices(moveTo.DropDownItems, folder, delegate (string destination)
                    {
                        _bookmarks.MoveFolder(folder, destination);
                        Changed = true;
                        PopulateTree(BookmarkManager.Combine(destination, BookmarkManager.NameOf(folder)));
                    });
                    menu.Items.Add(moveTo);

                    var delete = new ToolStripMenuItem("Delete Folder", null, delegate { DeleteFolder(); });
                    menu.Items.Add(delete);
                }
            }

            menu.Show(_tree, new Point(e.X, e.Y));
        }

        private void ListContextMenu(MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;

            ListViewItem item = _listView.GetItemAt(e.X, e.Y);
            if (item == null) return;

            item.Selected = true;
            string url = item.Tag as string;
            if (string.IsNullOrEmpty(url)) return;

            var menu = new ContextMenuStrip();
            menu.ShowImageMargin = false;
            menu.Font = Font;

            menu.Items.Add(new ToolStripMenuItem("Open", null, delegate { OpenUrl(url); }));
            menu.Items.Add(new ToolStripSeparator());

            var moveTo = new ToolStripMenuItem("Move to Folder");
            BuildFolderChoices(moveTo.DropDownItems, null, delegate (string destination)
            {
                if (_bookmarks.MoveBookmark(url, destination))
                {
                    Changed = true;
                    PopulateTree(BookmarkManager.Normalize(destination));
                }
            });
            menu.Items.Add(moveTo);

            menu.Items.Add(new ToolStripMenuItem("Rename\u2026", null, delegate { RenameFocused(); }));
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(new ToolStripMenuItem("Delete", null, delegate
            {
                _bookmarks.RemoveBookmark(url);
                Changed = true;
                PopulateTree(SelectedFolder);
            }));

            menu.Show(_listView, new Point(e.X, e.Y));
        }

        /// <summary>
        /// Fills a "move to" submenu with every folder that can hold the item,
        /// plus a way to make a new one on the spot.
        /// </summary>
        private void BuildFolderChoices(ToolStripItemCollection items, string excludeFolder, Action<string> move)
        {
            var root = new ToolStripMenuItem(BookmarkManager.RootFolder);
            root.Click += delegate { move(string.Empty); };
            items.Add(root);

            foreach (string folder in _bookmarks.GetFolders())
            {
                // A folder cannot be moved inside itself.
                if (excludeFolder != null && BookmarkManager.IsSameOrInside(folder, excludeFolder)) continue;

                var choice = new ToolStripMenuItem(Indent(folder));
                string destination = folder;
                choice.Click += delegate { move(destination); };
                items.Add(choice);
            }

            items.Add(new ToolStripSeparator());

            var create = new ToolStripMenuItem("New Folder\u2026");
            create.Click += delegate
            {
                string parent = string.Empty;
                using (var prompt = new TextPromptForm("New Folder", "Name for the new folder:",
                        _bookmarks.UniqueChildName(parent, "New Folder"), "Create"))
                {
                    if (prompt.ShowDialog(this) != DialogResult.OK) return;

                    string created = _bookmarks.CreateFolder(parent,
                        _bookmarks.UniqueChildName(parent, prompt.Value));
                    if (string.IsNullOrEmpty(created)) return;

                    Changed = true;
                    move(created);
                }
            };
            items.Add(create);
        }

        private static string Indent(string folder)
        {
            int depth = 0;
            foreach (char c in folder)
            {
                if (c == '/') depth++;
            }
            return new string('\u00a0', depth * 3) + BookmarkManager.NameOf(folder);
        }
    }
}
