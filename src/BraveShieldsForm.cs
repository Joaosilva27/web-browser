using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SimpleBrowser
{
    public class BraveShieldsForm : Form
    {
        public BraveShieldsForm(string currentSite)
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            Size = new Size(320, 360);
            BackColor = Color.FromArgb(24, 25, 36);
            ForeColor = Color.White;
            ShowInTaskbar = false;
            TopMost = true;

            // Header panel
            var header = new Panel();
            header.Dock = DockStyle.Top;
            header.Height = 54;
            header.BackColor = Color.FromArgb(32, 34, 48);
            header.Padding = new Padding(16, 12, 16, 12);

            var lionIcon = new Label();
            lionIcon.Text = "🦁";
            lionIcon.Font = new Font("Segoe UI", 16F);
            lionIcon.AutoSize = true;
            lionIcon.Dock = DockStyle.Left;

            var title = new Label();
            title.Text = "Brave Shields";
            title.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            title.ForeColor = Color.FromArgb(251, 84, 43); // Brave Orange
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
            closeBtn.ForeColor = Color.LightGray;
            closeBtn.Cursor = Cursors.Hand;
            closeBtn.Click += delegate { Close(); };

            header.Controls.Add(closeBtn);
            header.Controls.Add(title);
            header.Controls.Add(lionIcon);

            // Body
            var body = new Panel();
            body.Dock = DockStyle.Fill;
            body.Padding = new Padding(18);

            var siteLabel = new Label();
            siteLabel.Text = string.IsNullOrEmpty(currentSite) ? "Current Site" : currentSite;
            siteLabel.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
            siteLabel.ForeColor = Color.FromArgb(160, 165, 180);
            siteLabel.Dock = DockStyle.Top;
            siteLabel.Height = 24;
            siteLabel.AutoEllipsis = true;

            var statusPanel = new Panel();
            statusPanel.Dock = DockStyle.Top;
            statusPanel.Height = 50;
            statusPanel.BackColor = Color.FromArgb(35, 37, 52);
            statusPanel.Padding = new Padding(12);

            var statusText = new Label();
            statusText.Text = "SHIELDS ARE UP";
            statusText.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
            statusText.ForeColor = Color.FromArgb(251, 84, 43);
            statusText.Dock = DockStyle.Left;
            statusText.AutoSize = true;

            var badge = new Label();
            badge.Text = "ACTIVE";
            badge.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            badge.ForeColor = Color.FromArgb(46, 204, 113);
            badge.Dock = DockStyle.Right;
            badge.AutoSize = true;

            statusPanel.Controls.Add(badge);
            statusPanel.Controls.Add(statusText);

            var statsBox = new GroupBox();
            statsBox.Text = "Protection Summary";
            statsBox.ForeColor = Color.FromArgb(160, 165, 180);
            statsBox.Dock = DockStyle.Top;
            statsBox.Height = 150;
            statsBox.Font = new Font("Segoe UI", 8.5F);

            var lbl1 = CreateStatRow("🚫 Trackers & ads blocked", "12", 28);
            var lbl2 = CreateStatRow("🔒 HTTPS Upgrade", "Active", 58);
            var lbl3 = CreateStatRow("🛡️ Fingerprinting protection", "Strict", 88);
            var lbl4 = CreateStatRow("⚡ Scripts blocked", "Safe Mode", 118);

            statsBox.Controls.Add(lbl1);
            statsBox.Controls.Add(lbl2);
            statsBox.Controls.Add(lbl3);
            statsBox.Controls.Add(lbl4);

            var footerBtn = new Button();
            footerBtn.Text = "Done";
            footerBtn.Dock = DockStyle.Bottom;
            footerBtn.Height = 36;
            footerBtn.FlatStyle = FlatStyle.Flat;
            footerBtn.FlatAppearance.BorderColor = Color.FromArgb(251, 84, 43);
            footerBtn.BackColor = Color.FromArgb(251, 84, 43);
            footerBtn.ForeColor = Color.White;
            footerBtn.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            footerBtn.Cursor = Cursors.Hand;
            footerBtn.Click += delegate { Close(); };

            body.Controls.Add(footerBtn);
            body.Controls.Add(statsBox);
            body.Controls.Add(statusPanel);
            body.Controls.Add(siteLabel);

            Controls.Add(body);
            Controls.Add(header);

            // Border painting
            Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(251, 84, 43), 2))
                {
                    e.Graphics.DrawRectangle(pen, 1, 1, Width - 2, Height - 2);
                }
            };
        }

        private Panel CreateStatRow(string label, string val, int top)
        {
            var p = new Panel();
            p.Location = new Point(12, top);
            p.Size = new Size(260, 24);

            var l = new Label();
            l.Text = label;
            l.ForeColor = Color.White;
            l.Dock = DockStyle.Left;
            l.AutoSize = true;

            var v = new Label();
            v.Text = val;
            v.ForeColor = Color.FromArgb(251, 84, 43);
            v.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold);
            v.Dock = DockStyle.Right;
            v.AutoSize = true;

            p.Controls.Add(v);
            p.Controls.Add(l);
            return p;
        }

        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
            Close();
        }
    }
}
