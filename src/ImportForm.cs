using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace SimpleBrowser
{
    /// <summary>
    /// Lets the user pull bookmarks, history, search terms and top sites out of
    /// another browser installed on this PC.
    /// </summary>
    public class ImportForm : Form
    {
        private readonly BookmarkManager _bookmarks;
        private readonly HistoryManager _history;

        private List<BrowserProfile> _profiles;
        private ListBox _profileList;
        private CheckBox _bookmarksCheck;
        private CheckBox _historyCheck;
        private CheckBox _searchesCheck;
        private CheckBox _topSitesCheck;
        private Label _availability;
        private Label _status;
        private Label _privacy;
        private AuraButton _importButton;
        private AuraButton _cancelButton;
        private bool _laidOut;

        /// <summary>True when the start page gained tiles, so the host can refresh it.</summary>
        public bool FavoritesChanged { get; private set; }

        public ImportForm(BookmarkManager bookmarks, HistoryManager history)
        {
            _bookmarks = bookmarks;
            _history = history;

            Text = "Import Browser Data";
            AutoScaleMode = AutoScaleMode.None;
            Size = new Size(Dpi.Px(600), Dpi.Px(512));
            MinimumSize = new Size(Dpi.Px(560), Dpi.Px(470));
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Theme.ContentBackground;
            ForeColor = Theme.TextPrimary;
            Font = new Font("Segoe UI", 9F);
            MaximizeBox = false;
            MinimizeBox = false;

            var heading = new Label();
            heading.Text = "Import Browser Data";
            heading.Font = new Font("Segoe UI Semibold", 14F);
            heading.ForeColor = Theme.TextPrimary;
            heading.AutoSize = true;
            heading.Location = new Point(Dpi.Px(24), Dpi.Px(20));

            var subheading = new Label();
            subheading.Text = "Bring your bookmarks and browsing history over from another browser on this PC.";
            subheading.Font = new Font("Segoe UI", 9F);
            subheading.ForeColor = Theme.TextSecondary;
            subheading.AutoSize = true;
            subheading.Location = new Point(Dpi.Px(26), Dpi.Px(51));

            var pick = new Label();
            pick.Text = "Import from";
            pick.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            pick.ForeColor = Theme.TextSecondary;
            pick.AutoSize = true;
            pick.Location = new Point(Dpi.Px(26), Dpi.Px(88));

            _profileList = new ListBox();
            _profileList.Location = new Point(Dpi.Px(26), Dpi.Px(110));
            _profileList.Size = new Size(Dpi.Px(548), Dpi.Px(112));
            _profileList.IntegralHeight = false;
            _profileList.BorderStyle = BorderStyle.FixedSingle;
            _profileList.BackColor = Theme.IsDark ? Color.FromArgb(38, 38, 41) : Color.White;
            _profileList.ForeColor = Theme.TextPrimary;
            _profileList.Font = new Font("Segoe UI", 9.5F);
            _profileList.DisplayMember = "Display";
            _profileList.SelectedIndexChanged += delegate { SyncAvailability(); };

            _availability = new Label();
            _availability.Text = "";
            _availability.Font = new Font("Segoe UI", 8.25F);
            _availability.ForeColor = Theme.TextTertiary;
            _availability.AutoSize = true;
            _availability.Location = new Point(Dpi.Px(28), Dpi.Px(228));

            var categories = new Label();
            categories.Text = "What to bring over";
            categories.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            categories.ForeColor = Theme.TextSecondary;
            categories.AutoSize = true;
            categories.Location = new Point(Dpi.Px(26), Dpi.Px(256));

            _bookmarksCheck = MakeCheckBox("Bookmarks", Dpi.Px(26), Dpi.Px(280));
            _historyCheck = MakeCheckBox("Browsing history", Dpi.Px(26), Dpi.Px(308));
            _searchesCheck = MakeCheckBox("Search history", Dpi.Px(250), Dpi.Px(280));
            _topSitesCheck = MakeCheckBox("Start page tiles", Dpi.Px(250), Dpi.Px(308));

            _status = new Label();
            _status.Text = "";
            _status.Font = new Font("Segoe UI", 8.25F);
            _status.ForeColor = Theme.TextSecondary;
            _status.AutoSize = false;
            _status.Location = new Point(Dpi.Px(26), Dpi.Px(334));
            _status.Size = new Size(Dpi.Px(548), Dpi.Px(40));

            _privacy = new Label();
            _privacy.Text = "Cookies and saved passwords cannot be imported: Chrome, Edge and Brave " +
                            "encrypt them with App-Bound Encryption, which only the browser that wrote " +
                            "them can unlock. Sign in once in Aura and the session is kept.";
            _privacy.Font = new Font("Segoe UI", 8.25F);
            _privacy.ForeColor = Theme.TextTertiary;
            _privacy.AutoSize = false;
            _privacy.Location = new Point(Dpi.Px(26), Dpi.Px(386));
            _privacy.Size = new Size(Dpi.Px(548), Dpi.Px(46));

            _cancelButton = new AuraButton();
            _cancelButton.Text = "Cancel";
            _cancelButton.Size = new Size(Dpi.Px(100), Dpi.Px(34));
            _cancelButton.Radius = Dpi.Px(9);
            _cancelButton.BackColor = Theme.IsDark ? Color.FromArgb(72, 72, 78) : Color.FromArgb(228, 228, 232);
            _cancelButton.ForeColor = Theme.TextPrimary;
            _cancelButton.Click += delegate { Close(); };

            _importButton = new AuraButton();
            _importButton.Text = "Import";
            _importButton.Size = new Size(Dpi.Px(124), Dpi.Px(34));
            _importButton.Radius = Dpi.Px(9);
            _importButton.BackColor = Theme.AccentBlue;
            _importButton.Click += delegate { RunImport(); };

            CancelButton = _cancelButton;

            Controls.Add(heading);
            Controls.Add(subheading);
            Controls.Add(pick);
            Controls.Add(_profileList);
            Controls.Add(_availability);
            Controls.Add(categories);
            Controls.Add(_bookmarksCheck);
            Controls.Add(_historyCheck);
            Controls.Add(_searchesCheck);
            Controls.Add(_topSitesCheck);
            Controls.Add(_status);
            Controls.Add(_privacy);
            Controls.Add(_importButton);
            Controls.Add(_cancelButton);

            LoadProfiles();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // The real client area only exists once the window is created, so
            // anything tied to its size is placed here rather than in the
            // constructor.
            _laidOut = true;
            LayoutContent();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (_laidOut) LayoutContent();
        }

        /// <summary>
        /// Sizes and positions everything that follows the client area. Anchors
        /// are avoided on purpose: their reference size is captured while the
        /// dialog is still at the WinForms default size, which put the buttons
        /// off the bottom edge.
        /// </summary>
        private void LayoutContent()
        {
            int width = ClientSize.Width;
            int height = ClientSize.Height;
            int contentWidth = Math.Max(Dpi.Px(200), width - Dpi.Px(52));

            _profileList.Size = new Size(contentWidth, Dpi.Px(112));
            _status.Size = new Size(contentWidth, Dpi.Px(40));
            _privacy.Size = new Size(contentWidth, Dpi.Px(46));

            int buttonTop = height - Dpi.Px(48);
            _importButton.Location = new Point(width - Dpi.Px(262), buttonTop);
            _cancelButton.Location = new Point(width - Dpi.Px(126), buttonTop);
        }

        private CheckBox MakeCheckBox(string text, int x, int y)
        {
            var check = new CheckBox();
            check.Text = text;
            check.AutoSize = true;
            check.Location = new Point(x, y);
            check.ForeColor = Theme.TextPrimary;
            check.BackColor = Color.Transparent;
            check.Font = new Font("Segoe UI", 9F);
            check.Checked = true;
            return check;
        }

        private void LoadProfiles()
        {
            _profiles = BrowserProfiles.Detect();

            foreach (var profile in _profiles)
            {
                _profileList.Items.Add(profile);
            }

            if (_profiles.Count == 0)
            {
                _importButton.Enabled = false;
                _status.Text = "No other browser was found on this PC.";
                SyncAvailability();
                return;
            }

            _profileList.SelectedIndex = 0;
            SyncAvailability();
        }

        /// <summary>Enables only what the selected profile actually has.</summary>
        private void SyncAvailability()
        {
            if (_profiles == null || _profiles.Count == 0) return;

            int index = _profileList.SelectedIndex;
            if (index < 0 || index >= _profiles.Count) return;

            var profile = _profiles[index];
            bool hasBookmarks = profile.HasBookmarks;
            bool hasHistory = profile.HasHistory;
            bool hasTopSites = profile.HasTopSites;

            _bookmarksCheck.Enabled = hasBookmarks;
            _historyCheck.Enabled = hasHistory;
            _searchesCheck.Enabled = hasHistory && profile.IsChromium;
            _topSitesCheck.Enabled = hasTopSites;

            if (!hasBookmarks) _bookmarksCheck.Checked = false;
            if (!hasHistory) _historyCheck.Checked = false;
            if (!_searchesCheck.Enabled) _searchesCheck.Checked = false;
            if (!_topSitesCheck.Enabled) _topSitesCheck.Checked = false;

            var parts = new List<string>();
            if (hasBookmarks) parts.Add("bookmarks");
            if (hasHistory) parts.Add("history");
            if (hasHistory && profile.IsChromium) parts.Add("search terms");
            if (hasTopSites) parts.Add("top sites");

            _availability.Text = parts.Count == 0
                ? "Nothing to import from this profile."
                : "Available: " + string.Join(", ", parts.ToArray());
        }

        private void RunImport()
        {
            if (_profiles == null || _profiles.Count == 0) return;

            int index = _profileList.SelectedIndex;
            if (index < 0 || index >= _profiles.Count) return;
            var profile = _profiles[index];

            var selection = new ImportSelection();
            selection.Bookmarks = _bookmarksCheck.Checked && _bookmarksCheck.Enabled;
            selection.History = _historyCheck.Checked && _historyCheck.Enabled;
            selection.Searches = _searchesCheck.Checked && _searchesCheck.Enabled;
            selection.TopSites = _topSitesCheck.Checked && _topSitesCheck.Enabled;

            if (!selection.Bookmarks && !selection.History && !selection.Searches && !selection.TopSites)
            {
                _status.Text = "Choose at least one thing to import.";
                return;
            }

            _importButton.Enabled = false;
            _cancelButton.Enabled = false;
            Cursor = Cursors.WaitCursor;
            _status.Text = "Importing from " + profile.Display + "\u2026";
            Application.DoEvents();

            ImportResult result;
            try
            {
                result = BrowserImporter.Run(profile, selection, _bookmarks, _history, FavoritesStore.Current);
            }
            catch (Exception error)
            {
                Cursor = Cursors.Default;
                _importButton.Enabled = true;
                _cancelButton.Enabled = true;
                _status.Text = "";
                MessageBox.Show(this,
                    "The import could not be completed.\r\n\r\n" + error.Message,
                    "Import Browser Data", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Cursor = Cursors.Default;
            _importButton.Enabled = true;
            _cancelButton.Enabled = true;
            if (result.Favorites > 0) FavoritesChanged = true;

            MessageBox.Show(this, Describe(result, profile), "Import complete",
                MessageBoxButtons.OK, MessageBoxIcon.Information);

            DialogResult = DialogResult.OK;
            Close();
        }

        private static string Describe(ImportResult result, BrowserProfile profile)
        {
            var summary = new StringBuilder();
            summary.Append("Imported from ").Append(profile.Display).Append(":\r\n\r\n");
            summary.Append("\u2022  ").Append(result.Bookmarks).Append(" new bookmarks\r\n");
            summary.Append("\u2022  ").Append(result.History).Append(" new history entries\r\n");
            summary.Append("\u2022  ").Append(result.Searches).Append(" new search terms\r\n");
            summary.Append("\u2022  ").Append(result.Favorites).Append(" new start page tiles\r\n");

            if (result.Bookmarks == 0 && result.History == 0 && result.Searches == 0)
            {
                summary.Append("\r\nNothing new was found \u2014 everything was already here.");
            }

            if (result.Notes.Count > 0)
            {
                summary.Append("\r\n");
                foreach (string note in result.Notes) summary.Append(note).Append("\r\n");
            }

            return summary.ToString();
        }
    }
}
