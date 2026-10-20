using System;
using System.Drawing;
using System.Windows.Forms;

namespace SimpleBrowser
{
    /// <summary>
    /// The card Toge puts up when a page is behind a login the engine cannot
    /// answer on its own. Nothing is remembered: the credentials go straight to
    /// the page's own challenge and are never written to disk.
    /// </summary>
    public class LoginForm : Form
    {
        private TextBox _user;
        private TextBox _password;

        public string UserName { get; private set; }
        public string Password { get; private set; }

        public LoginForm(string host, string challenge)
        {
            PointerGuard.Attach(this);

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            AutoScaleMode = AutoScaleMode.None;
            Size = new Size(Dpi.Px(396), Dpi.Px(280));
            BackColor = Theme.CardBackground;
            ForeColor = Theme.TextPrimary;
            ShowInTaskbar = false;
            DoubleBuffered = true;

            var title = new Label();
            title.Text = "Sign in";
            title.Font = new Font("Segoe UI Semibold", 13F);
            title.ForeColor = Theme.TextPrimary;
            title.AutoSize = true;
            title.Location = new Point(Dpi.Px(24), Dpi.Px(20));

            var site = new Label();
            site.Text = string.IsNullOrEmpty(host) ? "This site" : host;
            site.Font = new Font("Segoe UI", 8.75F);
            site.ForeColor = Theme.TextSecondary;
            site.AutoEllipsis = true;
            site.AutoSize = false;
            site.Size = new Size(Dpi.Px(340), Dpi.Px(18));
            site.Location = new Point(Dpi.Px(25), Dpi.Px(48));

            var hint = new Label();
            hint.Text = string.IsNullOrEmpty(challenge)
                ? "Enter the name and password this site asked for."
                : challenge;
            hint.Font = new Font("Segoe UI", 8.75F);
            hint.ForeColor = Theme.TextTertiary;
            hint.AutoEllipsis = true;
            hint.AutoSize = false;
            hint.Size = new Size(Dpi.Px(348), Dpi.Px(18));
            hint.Location = new Point(Dpi.Px(25), Dpi.Px(70));

            _user = Field("Name", Dpi.Px(118));
            _password = Field("Password", Dpi.Px(174));
            _password.UseSystemPasswordChar = true;

            var userLabel = FieldLabel("Name", Dpi.Px(100));
            var passwordLabel = FieldLabel("Password", Dpi.Px(156));

            var cancel = new TogeButton();
            cancel.Text = "Cancel";
            cancel.Font = new Font("Segoe UI", 9.25F);
            cancel.Radius = Dpi.Px(8);
            cancel.Size = new Size(Dpi.Px(96), Dpi.Px(34));
            cancel.Location = new Point(Dpi.Px(172), Dpi.Px(218));
            cancel.BackColor = Theme.IsDark ? Color.FromArgb(64, 64, 68) : Color.FromArgb(232, 232, 235);
            cancel.ForeColor = Theme.TextPrimary;
            cancel.Click += delegate
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };

            var ok = new TogeButton();
            ok.Text = "Sign in";
            ok.Font = new Font("Segoe UI", 9.25F);
            ok.Radius = Dpi.Px(8);
            ok.Size = new Size(Dpi.Px(104), Dpi.Px(34));
            ok.Location = new Point(Dpi.Px(276), Dpi.Px(218));
            ok.BackColor = Theme.AccentBlue;
            ok.ForeColor = Color.White;
            ok.Click += delegate { Accept(); };

            Controls.Add(title);
            Controls.Add(site);
            Controls.Add(hint);
            Controls.Add(userLabel);
            Controls.Add(_user);
            Controls.Add(passwordLabel);
            Controls.Add(_password);
            Controls.Add(cancel);
            Controls.Add(ok);

            AcceptButton = null;
            _user.KeyDown += OnFieldKey;
            _password.KeyDown += OnFieldKey;
        }

        private TextBox Field(string placeholder, int top)
        {
            var box = new TextBox();
            box.BorderStyle = BorderStyle.FixedSingle;
            box.Font = new Font("Segoe UI", 9.75F);
            box.Size = new Size(Dpi.Px(348), Dpi.Px(28));
            box.Location = new Point(Dpi.Px(24), top);
            box.BackColor = Theme.IsDark ? Color.FromArgb(52, 52, 56) : Color.White;
            box.ForeColor = Theme.TextPrimary;
            return box;
        }

        private Label FieldLabel(string text, int top)
        {
            var label = new Label();
            label.Text = text;
            label.Font = new Font("Segoe UI", 8.25F);
            label.ForeColor = Theme.TextSecondary;
            label.AutoSize = true;
            label.Location = new Point(Dpi.Px(25), top);
            return label;
        }

        private void OnFieldKey(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
                Close();
            }
        }

        private void Accept()
        {
            UserName = _user.Text;
            Password = _password.Text;
            DialogResult = DialogResult.OK;
            Close();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Region = Theme.RoundedRegion(ClientRectangle, Dpi.Px(14));
            _user.Focus();
        }

        protected override bool ProcessDialogKey(Keys keyData)
        {
            if (keyData == Keys.Enter)
            {
                Accept();
                return true;
            }
            return base.ProcessDialogKey(keyData);
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
    }
}
