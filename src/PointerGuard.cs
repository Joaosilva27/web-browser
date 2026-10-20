using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SimpleBrowser
{
    /// <summary>
    /// Chromium blanks the pointer while it is putting up a dialog of its own,
    /// and a native card drawn in its place never gets the pointer back. This
    /// puts it back whenever it goes missing, and only then, so the hand over a
    /// button still works.
    /// </summary>
    internal static class PointerGuard
    {
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

        public static void KeepVisible()
        {
            var info = new CursorInfo();
            info.Size = Marshal.SizeOf(typeof(CursorInfo));

            if (!GetCursorInfo(ref info)) return;
            if ((info.Flags & CursorShowing) != 0) return;

            SetCursor(LoadCursor(IntPtr.Zero, ArrowCursor));
        }

        /// <summary>
        /// Watches a form for as long as it is up. Chromium sometimes hides the
        /// pointer again a moment after the dialog is shown, hence the timer.
        /// </summary>
        public static void Attach(Form form)
        {
            form.Shown += delegate
            {
                KeepVisible();

                var timer = new Timer();
                timer.Interval = 200;
                timer.Tick += delegate { KeepVisible(); };

                form.FormClosed += delegate
                {
                    timer.Stop();
                    timer.Dispose();
                    KeepVisible();
                };

                timer.Start();
            };
        }
    }
}
