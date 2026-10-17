using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SimpleBrowser
{
    public class AetherShieldForm : Form
    {
        public AetherShieldForm(string currentSite)
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            Size = new Size(340, 390);
            BackColor = Color.FromArgb(14, 16, 23); // Obsidian Glass
            ForeColor = Color.White;
            ShowInTaskbar = false;
            TopMost = true;

            // Header Panel
            var header = new Panel();
            header.Dock = DockStyle.Top;
            header.Height = 58;
            header.BackColor = Color.FromArgb(19, 22, 32);
            header.Padding = new Padding(18, 14, 18, 14);

            var starIcon = new Label();
            starIcon.Text = "✦";
            starIcon.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            starIcon.ForeColor = Color.FromArgb(0, 242, 254); // Cyan Glow
            starIcon.AutoSize = true;
            starIcon.Dock = DockStyle.Left;

            var title = new Label();
            title.Text = "Aether Shield";
            title.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            title.ForeColor = Color.White;
            title.AutoSize = true;
            title.Dock = DockStyle.Left;
            title.TextAlign = ContentAlignment.MiddleLeft;
            title.Padding = new Padding(8, 2, 0, 0);

            var closeBtn = new Button();
            closeBtn.Text = "✕";
            closeBtn.Dock = DockStyle.Right;
            closeBtn.Width = 30;
            closeBtn.FlatStyle = FlatStyle.Flat;
            closeBtn.FlatAppearance.BorderSize = 0;
            closeBtn.ForeColor = Color.FromArgb(160, 165, 180);
            closeBtn.Cursor = Cursors.Hand;
            closeBtn.Click += delegate { Close(); };

            header.Controls.Add(closeBtn);
            header.Controls.Add(title);
            header.Controls.Add(starIcon);

            // Body
            var body = new Panel();
            body.Dock = DockStyle.Fill;
            body.Padding = new Padding(20);

            var siteLabel = new Label();
            siteLabel.Text = string.IsNullOrEmpty(currentSite) ? "Current Session" : currentSite;
            siteLabel.Font = new Font("Segoe UI", 9F);
            siteLabel.ForeColor = Color.FromArgb(140, 145, 165);
            siteLabel.Dock = DockStyle.Top;
            siteLabel.Height = 24;
            siteLabel.AutoEllipsis = true;

            // Status Card
            var statusPanel = new Panel();
            statusPanel.Dock = DockStyle.Top;
            statusPanel.Height = 56;
            statusPanel.BackColor = Color.FromArgb(22, 26, 38);
            statusPanel.Padding = new Padding(14);

            var statusText = new Label();
            statusText.Text = "QUANTUM SHIELD ON";
            statusText.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            statusText.ForeColor = Color.FromArgb(0, 242, 254);
            statusText.Dock = DockStyle.Left;
            statusText.AutoSize = true;

            var statusDot = new Label();
            statusDot.Text = "● ACTIVE";
            statusDot.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            statusDot.ForeColor = Color.FromArgb(52, 211, 153); // Emerald
            statusDot.Dock = DockStyle.Right;
            statusDot.AutoSize = true;

            statusPanel.Controls.Add(statusDot);
            statusPanel.Controls.Add(statusText);

            // Metrics Group
            var metricsBox = new Panel();
            metricsBox.Dock = DockStyle.Top;
            metricsBox.Height = 150;
            metricsBox.Padding = new Padding(4, 12, 4, 12);

            metricsBox.Controls.Add(CreateRow("Trackers Neutralized", "24", 8));
            metricsBox.Controls.Add(CreateRow("Fingerprint Cloak", "Quantum Strict", 40));
            metricsBox.Controls.Add(CreateRow("HTTPS Encryption", "Enforced", 72));
            metricsBox.Controls.Add(CreateRow("Telemetry Isolation", "100% Private", 104));

            var doneBtn = new Button();
            doneBtn.Text = "Done";
            doneBtn.Dock = DockStyle.Bottom;
            doneBtn.Height = 38;
            doneBtn.FlatStyle = FlatStyle.Flat;
            doneBtn.FlatAppearance.BorderSize = 0;
            doneBtn.BackColor = Color.FromArgb(0, 242, 254);
            doneBtn.ForeColor = Color.FromArgb(10, 12, 18);
            doneBtn.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            doneBtn.Cursor = Cursors.Hand;
            doneBtn.Click += delegate { Close(); };

            body.Controls.Add(doneBtn);
            body.Controls.Add(metricsBox);
            body.Controls.Add(statusPanel);
            body.Controls.Add(siteLabel);

            Controls.Add(body);
            Controls.Add(header);

            // Specular border
            Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var pen = new Pen(Color.FromArgb(0, 242, 254), 1.5f))
                {
                    e.Graphics.DrawRectangle(pen, 1, 1, Width - 2, Height - 2);
                }
            };
        }

        private Panel CreateRow(string title, string value, int top)
        {
            var p = new Panel();
            p.Location = new Point(0, top);
            p.Size = new Size(300, 26);

            var lbl = new Label();
            lbl.Text = title;
            lbl.ForeColor = Color.FromArgb(190, 195, 210);
            lbl.Font = new Font("Segoe UI", 9F);
            lbl.Dock = DockStyle.Left;
            lbl.AutoSize = true;

            var val = new Label();
            val.Text = value;
            val.ForeColor = Color.FromArgb(0, 242, 254);
            val.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            val.Dock = DockStyle.Right;
            val.AutoSize = true;

            p.Controls.Add(val);
            p.Controls.Add(lbl);
            return p;
        }

        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
            Close();
        }
    }
}
