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
            Text = "Page Source: " + url;
            Size = new Size(800, 600);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(30, 30, 30);
            ForeColor = Color.White;

            var topPanel = new Panel();
            topPanel.Dock = DockStyle.Top;
            topPanel.Height = 44;
            topPanel.BackColor = Color.FromArgb(40, 40, 40);
            topPanel.Padding = new Padding(10, 8, 10, 8);

            _infoLabel = new Label();
            _infoLabel.Text = url;
            _infoLabel.ForeColor = Color.LightGray;
            _infoLabel.AutoEllipsis = true;
            _infoLabel.Dock = DockStyle.Fill;
            _infoLabel.TextAlign = ContentAlignment.MiddleLeft;

            _copyButton = new Button();
            _copyButton.Text = "Copy Source";
            _copyButton.Dock = DockStyle.Right;
            _copyButton.Width = 110;
            _copyButton.FlatStyle = FlatStyle.Flat;
            _copyButton.BackColor = Color.FromArgb(0, 122, 204);
            _copyButton.ForeColor = Color.White;
            _copyButton.Cursor = Cursors.Hand;
            _copyButton.Click += delegate
            {
                if (!string.IsNullOrEmpty(_textBox.Text))
                {
                    Clipboard.SetText(_textBox.Text);
                    MessageBox.Show("Source code copied to clipboard!", "Copied", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            _textBox.BackColor = Color.FromArgb(24, 24, 24);
            _textBox.ForeColor = Color.FromArgb(220, 220, 220);
            _textBox.Text = htmlSource ?? string.Empty;

            Controls.Add(_textBox);
            Controls.Add(topPanel);
        }
    }
}
