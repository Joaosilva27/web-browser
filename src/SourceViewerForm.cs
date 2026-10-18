using System;
using System.Drawing;
using System.Windows.Forms;

namespace SimpleBrowser
{
    public class SourceViewerForm : Form
    {
        private TextBox _textBox;
        private AuraButton _copyButton;
        private Label _infoLabel;

        public SourceViewerForm(string url, string htmlSource)
        {
            Text = "Page Source";
            Size = new Size(900, 660);
            MinimumSize = new Size(600, 400);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Theme.ContentBackground;
            ForeColor = Theme.TextPrimary;
            Font = new Font("Segoe UI", 9F);

            var topPanel = new Panel();
            topPanel.Dock = DockStyle.Top;
            topPanel.Height = 60;
            topPanel.BackColor = Theme.CardBackground;
            topPanel.Paint += delegate (object s, PaintEventArgs e)
            {
                using (var pen = new Pen(Theme.CardBorder, 1))
                {
                    e.Graphics.DrawLine(pen, 0, topPanel.Height - 1, topPanel.Width, topPanel.Height - 1);
                }
            };

            _infoLabel = new Label();
            _infoLabel.Text = url;
            _infoLabel.Font = new Font("Segoe UI Semibold", 10.5F);
            _infoLabel.ForeColor = Theme.TextPrimary;
            _infoLabel.AutoEllipsis = true;
            _infoLabel.AutoSize = false;
            _infoLabel.Size = new Size(Width - 180, 22);
            _infoLabel.Location = new Point(20, 11);
            _infoLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            var subtitle = new Label();
            subtitle.Text = "Document source";
            subtitle.Font = new Font("Segoe UI", 8.25F);
            subtitle.ForeColor = Theme.TextTertiary;
            subtitle.AutoSize = true;
            subtitle.Location = new Point(21, 34);

            _copyButton = new AuraButton();
            _copyButton.Text = "Copy";
            _copyButton.Size = new Size(88, 32);
            _copyButton.Radius = 8;
            _copyButton.BackColor = Theme.AccentBlue;
            _copyButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _copyButton.Location = new Point(Width - 108, 14);
            _copyButton.Click += delegate
            {
                if (!string.IsNullOrEmpty(_textBox.Text))
                {
                    try
                    {
                        Clipboard.SetText(_textBox.Text);
                        _copyButton.Text = "Copied";
                        var reset = new Timer();
                        reset.Interval = 1400;
                        reset.Tick += delegate
                        {
                            reset.Stop();
                            reset.Dispose();
                            _copyButton.Text = "Copy";
                        };
                        reset.Start();
                    }
                    catch
                    {
                        // Clipboard briefly unavailable.
                    }
                }
            };

            topPanel.Controls.Add(_infoLabel);
            topPanel.Controls.Add(subtitle);
            topPanel.Controls.Add(_copyButton);

            _textBox = new TextBox();
            _textBox.Dock = DockStyle.Fill;
            _textBox.Multiline = true;
            _textBox.ScrollBars = ScrollBars.Both;
            _textBox.ReadOnly = true;
            _textBox.WordWrap = false;
            _textBox.BorderStyle = BorderStyle.None;
            _textBox.Font = new Font("Consolas", 10F);
            _textBox.BackColor = Theme.IsDark
                ? Color.FromArgb(22, 22, 24)
                : Color.FromArgb(251, 251, 253);
            _textBox.ForeColor = Theme.IsDark
                ? Color.FromArgb(220, 222, 228)
                : Color.FromArgb(40, 40, 44);
            _textBox.Text = htmlSource ?? string.Empty;

            Controls.Add(_textBox);
            Controls.Add(topPanel);
        }
    }
}
