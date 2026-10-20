using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SimpleBrowser
{
    /// <summary>
    /// The card Toge puts up when a page asks for something of the user's — the
    /// camera, the microphone, their location. It replaces Chromium's own
    /// prompt, which would otherwise appear inside the page in another style.
    ///
    /// Allow or Block both count as a decision; dismissing the card is a refusal
    /// for this visit only, which is why the two are told apart.
    /// </summary>
    public class PermissionPromptForm : Form
    {
        /// <summary>True when the user chose Allow.</summary>
        public bool Allowed { get; private set; }

        public PermissionPromptForm(string host, string request)
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            AutoScaleMode = AutoScaleMode.None;
            Size = new Size(Dpi.Px(432), Dpi.Px(206));
            BackColor = Theme.CardBackground;
            ForeColor = Theme.TextPrimary;
            ShowInTaskbar = false;
            DoubleBuffered = true;
            KeyPreview = true;

            var icon = new Label();
            icon.Text = "\u25CF";
            icon.Font = new Font("Segoe UI", 15F);
            icon.ForeColor = Theme.AccentBlue;
            icon.TextAlign = ContentAlignment.MiddleCenter;
            icon.Size = new Size(Dpi.Px(30), Dpi.Px(30));
            icon.Location = new Point(Dpi.Px(24), Dpi.Px(24));

            var title = new Label();
            title.Text = "Allow this site?";
            title.Font = new Font("Segoe UI Semibold", 12.5F);
            title.ForeColor = Theme.TextPrimary;
            title.AutoSize = true;
            title.Location = new Point(Dpi.Px(58), Dpi.Px(25));

            var detail = new Label();
            detail.Text = (string.IsNullOrEmpty(host) ? "This site" : host) + " wants to " + request + ".";
            detail.Font = new Font("Segoe UI", 9.5F);
            detail.ForeColor = Theme.TextSecondary;
            detail.AutoSize = false;
            detail.Size = new Size(Dpi.Px(372), Dpi.Px(52));
            detail.Location = new Point(Dpi.Px(31), Dpi.Px(66));

            var note = new Label();
            note.Text = "Toge will remember this choice. You can change it in Settings.";
            note.Font = new Font("Segoe UI", 8.25F);
            note.ForeColor = Theme.TextTertiary;
            note.AutoSize = false;
            note.Size = new Size(Dpi.Px(372), Dpi.Px(20));
            note.Location = new Point(Dpi.Px(31), Dpi.Px(112));

            var block = new TogeButton();
            block.Text = "Block";
            block.Font = new Font("Segoe UI", 9.25F);
            block.Radius = Dpi.Px(8);
            block.Size = new Size(Dpi.Px(96), Dpi.Px(34));
            block.Location = new Point(Dpi.Px(200), Dpi.Px(154));
            block.BackColor = Theme.IsDark ? Color.FromArgb(72, 72, 78) : Color.FromArgb(228, 228, 232);
            block.ForeColor = Theme.TextPrimary;
            block.Click += delegate
            {
                Allowed = false;
                DialogResult = DialogResult.OK;
                Close();
            };

            var allow = new TogeButton();
            allow.Text = "Allow";
            allow.Font = new Font("Segoe UI", 9.25F);
            allow.Radius = Dpi.Px(8);
            allow.Size = new Size(Dpi.Px(96), Dpi.Px(34));
            allow.Location = new Point(Dpi.Px(308), Dpi.Px(154));
            allow.BackColor = Theme.AccentBlue;
            allow.ForeColor = Color.White;
            allow.Click += delegate
            {
                Allowed = true;
                DialogResult = DialogResult.OK;
                Close();
            };

            Controls.Add(icon);
            Controls.Add(title);
            Controls.Add(detail);
            Controls.Add(note);
            Controls.Add(block);
            Controls.Add(allow);

            KeyDown += delegate (object sender, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Escape)
                {
                    DialogResult = DialogResult.Cancel;
                    Close();
                }
                else if (e.KeyCode == Keys.Enter)
                {
                    Allowed = true;
                    DialogResult = DialogResult.OK;
                    Close();
                }
            };
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Region = Theme.RoundedRegion(ClientRectangle, Dpi.Px(14));
        }

        // Chromium blanks the pointer while the engine's own permission bubble
        // would be on screen, and the card stands in for that bubble. So the
        // pointer is put back as soon as it goes missing while the card is up,
        // and once more on the way out, when the engine lets go of it.
        private const int CursorShowing = 0x00000001;
        private const int ArrowCursor = 32512;

        [StructLayout(LayoutKind.Sequential)]
        private struct CursorPoint
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct CursorInfo
        {
            public int Size;
            public int Flags;
            public IntPtr Handle;
            public CursorPoint Position;
        }

        [DllImport("user32.dll")]
        private static extern bool GetCursorInfo(ref CursorInfo info);

        [DllImport("user32.dll")]
        private static extern IntPtr LoadCursor(IntPtr instance, int name);

        [DllImport("user32.dll")]
        private static extern IntPtr SetCursor(IntPtr cursor);

        private Timer _pointerTimer;

        private static void KeepPointerVisible()
        {
            var info = new CursorInfo();
            info.Size = Marshal.SizeOf(typeof(CursorInfo));

            if (!GetCursorInfo(ref info)) return;
            if ((info.Flags & CursorShowing) != 0) return;

            SetCursor(LoadCursor(IntPtr.Zero, ArrowCursor));
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            KeepPointerVisible();

            _pointerTimer = new Timer();
            _pointerTimer.Interval = 200;
            _pointerTimer.Tick += delegate { KeepPointerVisible(); };
            _pointerTimer.Start();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (_pointerTimer != null)
            {
                _pointerTimer.Stop();
                _pointerTimer.Dispose();
                _pointerTimer = null;
            }

            base.OnFormClosed(e);
            KeepPointerVisible();
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
