using System;
using System.Drawing;
using System.Windows.Forms;

namespace SimpleBrowser
{
    /// <summary>
    /// A small dialog that asks for one line of text: a new folder's name, or a
    /// new title for a bookmark. Windows' own InputBox is not in the framework,
    /// and this matches the rest of Toge's dialogs.
    /// </summary>
    public class TextPromptForm : Form
    {
        private readonly TextBox _box;

        /// <summary>The text the user accepted; empty when cancelled.</summary>
        public string Value { get; private set; }

        public TextPromptForm(string title, string caption, string initial, string action)
        {
            Value = string.Empty;

            Text = title;
            AutoScaleMode = AutoScaleMode.None;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(Dpi.Px(428), Dpi.Px(150));
            BackColor = Theme.ContentBackground;
            ForeColor = Theme.TextPrimary;
            Font = new Font("Segoe UI", 9F);

            var label = new Label();
            label.Text = caption;
            label.AutoSize = true;
            label.Location = new Point(Dpi.Px(22), Dpi.Px(20));
            label.ForeColor = Theme.TextSecondary;

            _box = new TextBox();
            _box.Text = initial ?? string.Empty;
            _box.Location = new Point(Dpi.Px(22), Dpi.Px(46));
            _box.Width = ClientSize.Width - Dpi.Px(44);
            _box.Font = new Font("Segoe UI", 10F);
            _box.BorderStyle = BorderStyle.FixedSingle;
            _box.BackColor = Theme.IsDark ? Color.FromArgb(56, 56, 60) : Color.White;
            _box.ForeColor = Theme.TextPrimary;

            var cancel = new TogeButton();
            cancel.Text = "Cancel";
            cancel.Size = new Size(Dpi.Px(92), Dpi.Px(32));
            cancel.Radius = Dpi.Px(8);
            cancel.BackColor = Theme.IsDark ? Color.FromArgb(72, 72, 78) : Color.FromArgb(228, 228, 232);
            cancel.ForeColor = Theme.TextPrimary;
            cancel.Location = new Point(ClientSize.Width - Dpi.Px(114), Dpi.Px(96));
            cancel.Click += delegate
            {
                Value = string.Empty;
                DialogResult = DialogResult.Cancel;
                Close();
            };

            var accept = new TogeButton();
            accept.Text = string.IsNullOrEmpty(action) ? "Save" : action;
            accept.Size = new Size(Dpi.Px(104), Dpi.Px(32));
            accept.Radius = Dpi.Px(8);
            accept.BackColor = Theme.AccentBlue;
            accept.Location = new Point(ClientSize.Width - Dpi.Px(226), Dpi.Px(96));
            accept.Click += delegate { Accept(); };

            Controls.Add(label);
            Controls.Add(_box);
            Controls.Add(cancel);
            Controls.Add(accept);

            AcceptButton = null;
            CancelButton = null;

            // Enter accepts and Escape cancels, as in any other dialog.
            _box.KeyDown += delegate (object s, KeyEventArgs e)
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
                _box.Focus();
                _box.SelectAll();
            };
        }

        private void Accept()
        {
            string text = (_box.Text ?? string.Empty).Trim();
            if (text.Length == 0) return;

            Value = text;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
