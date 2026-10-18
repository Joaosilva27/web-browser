using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace SimpleBrowser
{
    public class BraveShieldsForm : Form
    {
        public BraveShieldsForm(string currentSite)
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            AutoScaleMode = AutoScaleMode.None;
            Size = new Size(Dpi.Px(344), Dpi.Px(468));
            BackColor = Theme.CardBackground;
            ForeColor = Theme.TextPrimary;
            ShowInTaskbar = false;
            TopMost = true;
            DoubleBuffered = true;

            var title = new Label();
            title.Text = "Aura Shields";
            title.Font = new Font("Segoe UI Semibold", 12.5F);
            title.ForeColor = Theme.TextPrimary;
            title.AutoSize = true;
            title.Location = new Point(Dpi.Px(22), Dpi.Px(17));

            var host = new Label();
            host.Text = SiteLabel(currentSite);
            host.Font = new Font("Segoe UI", 8.75F);
            host.ForeColor = Theme.TextSecondary;
            host.AutoEllipsis = true;
            host.AutoSize = false;
            host.Size = new Size(Dpi.Px(268), Dpi.Px(18));
            host.Location = new Point(Dpi.Px(23), Dpi.Px(42));

            var close = new Label();
            close.Text = "\u2715";
            close.Font = new Font("Segoe UI", 9.5F);
            close.ForeColor = Theme.TextTertiary;
            close.TextAlign = ContentAlignment.MiddleCenter;
            close.Size = new Size(Dpi.Px(26), Dpi.Px(26));
            close.Location = new Point(Dpi.Px(302), Dpi.Px(15));
            close.Cursor = Cursors.Hand;
            close.Click += delegate { Close(); };

            var count = new Label();
            count.Text = AdBlocker.BlockedCount.ToString("N0");
            count.Font = new Font("Segoe UI", 30F, FontStyle.Regular);
            count.ForeColor = Theme.AccentOrange;
            count.AutoSize = true;
            count.Location = new Point(Dpi.Px(19), Dpi.Px(68));

            var caption = new Label();
            caption.Text = "trackers & ads blocked this session";
            caption.UseMnemonic = false;
            caption.Font = new Font("Segoe UI", 8.75F);
            caption.ForeColor = Theme.TextSecondary;
            caption.AutoSize = true;
            caption.Location = new Point(Dpi.Px(23), Dpi.Px(120));

            var card = new Panel();
            card.Location = new Point(Dpi.Px(16), Dpi.Px(150));
            card.Size = new Size(Dpi.Px(312), Dpi.Px(118));
            card.BackColor = Theme.IsDark
                ? Color.FromArgb(52, 52, 56)
                : Color.FromArgb(245, 245, 247);

            card.Controls.Add(CreateRow("Filter rules loaded", AdBlocker.RuleCount.ToString("N0"), 11));
            card.Controls.Add(CreateRow("HTTPS upgrade", "On", 44));
            card.Controls.Add(CreateRow("Fingerprinting protection", "Strict", 77));

            var listTitle = new Label();
            listTitle.Text = "MOST BLOCKED";
            listTitle.Font = new Font("Segoe UI", 7.75F, FontStyle.Bold);
            listTitle.ForeColor = Theme.TextTertiary;
            listTitle.AutoSize = true;
            listTitle.Location = new Point(Dpi.Px(22), Dpi.Px(287));

            var list = new Label();
            list.Text = BuildBlockedList();
            list.Font = new Font("Segoe UI", 8.75F);
            list.ForeColor = Theme.TextSecondary;
            list.AutoSize = false;
            list.Size = new Size(Dpi.Px(300), Dpi.Px(82));
            list.Location = new Point(Dpi.Px(22), Dpi.Px(308));

            var done = new AuraButton();
            done.Text = "Done";
            done.Size = new Size(Dpi.Px(312), Dpi.Px(38));
            done.Location = new Point(Dpi.Px(16), Dpi.Px(410));
            done.Radius = Dpi.Px(9);
            done.BackColor = Theme.AccentOrange;
            done.Click += delegate { Close(); };

            Controls.Add(title);
            Controls.Add(host);
            Controls.Add(close);
            Controls.Add(count);
            Controls.Add(caption);
            Controls.Add(card);
            Controls.Add(listTitle);
            Controls.Add(list);
            Controls.Add(done);

            Paint += OnCardPaint;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Region = Theme.RoundedRegion(ClientRectangle, Dpi.Px(14));
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= 0x20000; // CS_DROPSHADOW
                return cp;
            }
        }

        private static string SiteLabel(string site)
        {
            if (string.IsNullOrEmpty(site) || site == "about:home") return "New Tab";
            try
            {
                Uri uri = new Uri(site);
                return uri.Host.Replace("www.", "");
            }
            catch
            {
                return site;
            }
        }

        private Panel CreateRow(string label, string value, int top)
        {
            var row = new Panel();
            row.Location = new Point(Dpi.Px(14), Dpi.Px(top));
            row.Size = new Size(Dpi.Px(284), Dpi.Px(24));

            var name = new Label();
            name.Text = label;
            name.Dock = DockStyle.Fill;
            name.TextAlign = ContentAlignment.MiddleLeft;
            name.ForeColor = Theme.TextSecondary;
            name.Font = new Font("Segoe UI", 8.75F);

            var val = new Label();
            val.Text = value;
            val.Dock = DockStyle.Right;
            val.Width = Dpi.Px(96);
            val.TextAlign = ContentAlignment.MiddleRight;
            val.ForeColor = Theme.TextPrimary;
            val.Font = new Font("Segoe UI", 8.75F, FontStyle.Bold);

            row.Controls.Add(name);
            row.Controls.Add(val);
            return row;
        }

        private static string BuildBlockedList()
        {
            List<KeyValuePair<string, int>> ranked =
                new List<KeyValuePair<string, int>>(AdBlocker.GetBlockedByHost());
            ranked.Sort(delegate (KeyValuePair<string, int> a, KeyValuePair<string, int> b)
            {
                return b.Value.CompareTo(a.Value);
            });

            if (ranked.Count == 0)
            {
                return "Nothing blocked yet on this page.";
            }

            StringBuilder sb = new StringBuilder();
            int shown = ranked.Count < 4 ? ranked.Count : 4;
            for (int i = 0; i < shown; i++)
            {
                string hostName = ranked[i].Key;
                if (hostName.Length > 34) hostName = hostName.Substring(0, 32) + "..";
                if (i > 0) sb.Append(Environment.NewLine);
                sb.Append(hostName).Append("  ").Append(ranked[i].Value);
            }
            return sb.ToString();
        }

        private void OnCardPaint(object sender, PaintEventArgs e)
        {
            using (var pen = new Pen(Theme.CardBorder, 1))
            {
                e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
            }
        }

        protected override void OnDeactivate(EventArgs e)
        {
            base.OnDeactivate(e);
            Close();
        }
    }
}
