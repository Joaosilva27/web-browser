using System;
using System.Drawing;
using System.Windows.Forms;

namespace SimpleBrowser
{
    public class SourceViewerForm : Form
    {
        private TextBox _textBox;
        private Button _copyButton;
        private Label _infoLabel;

        public SourceViewerForm(string url, string htmlSource)
        {
            Text = "Page Source: " + url + " - Brave Inspector";
            Size = new Size(840, 620);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(24, 25, 36);
            ForeColor = Color.White;

            var topPanel = new Panel();
            topPanel.Dock = DockStyle.Top;
            topPanel.Height = 48;
            topPanel.BackColor = Color.FromArgb(34, 36, 50);
            topPanel.Padding = new Padding(12, 10, 12, 10);

            _infoLabel = new Label();
            _infoLabel.Text = "🦁 " + url;
            _infoLabel.ForeColor = Color.FromArgb(160, 165, 180);
            _infoLabel.AutoEllipsis = true;
            _infoLabel.Dock = DockStyle.Fill;
            _infoLabel.TextAlign = ContentAlignment.MiddleLeft;

            _copyButton = new Button();
            _copyButton.Text = "Copy Source";
            _copyButton.Dock = DockStyle.Right;
            _copyButton.Width = 120;
            _copyButton.FlatStyle = FlatStyle.Flat;
            _copyButton.FlatAppearance.BorderSize = 0;
            _copyButton.BackColor = Color.FromArgb(251, 84, 43); // Brave Orange
            _copyButton.ForeColor = Color.White;
            _copyButton.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _copyButton.Cursor = Cursors.Hand;
            _copyButton.Click += delegate
            {
                if (!string.IsNullOrEmpty(_textBox.Text))
                {
                    Clipboard.SetText(_textBox.Text);
                    MessageBox.Show("Page source copied to clipboard!", "Brave Inspector", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };

            topPanel.Controls.Add(_infoLabel);
            topPanel.Controls.Add(_copyButton);

            _textBox = new TextBox();
            _textBox.Dock = DockStyle.Fill;
            _textBox.Multiline = true;
            _textBox.ScrollBars = ScrollBars.Both;
            _textBox.ReadOnly = true;
            _textBox.Font = new Font("Consolas", 10F);
            _textBox.BackColor = Color.FromArgb(18, 19, 28);
            _textBox.ForeColor = Color.FromArgb(220, 225, 235);
            _textBox.BorderStyle = BorderStyle.None;
            _textBox.Text = htmlSource ?? string.Empty;

            Controls.Add(_textBox);
            Controls.Add(topPanel);
        }
    }
}
