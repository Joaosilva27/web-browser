using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace SimpleBrowser
{
    /// <summary>
    /// Keeps Toge to one copy per user session.
    ///
    /// The first copy owns a named mutex and parks an invisible window under a
    /// known title. A second copy — started by clicking a link, or by the shell
    /// when Toge is the default browser — finds that window, posts the request
    /// to it through WM_COPYDATA and exits, so the page opens in the window the
    /// user already has instead of starting a second browser on the same data.
    /// </summary>
    public static class SingleInstance
    {
        private const string MutexName = "Local\\Toge.SingleInstance";
        private const string WindowTitle = "Toge.HandOff.v1";
        private const int WM_COPYDATA = 0x004A;

        private static Mutex _mutex;
        private static HandoffWindow _window;

        /// <summary>
        /// Claims Toge for this process. When another copy already owns it, the
        /// request is handed to that copy and false is returned, meaning this
        /// process should exit without doing anything else.
        /// </summary>
        public static bool Claim(BrowserLaunch launch, Action<BrowserLaunch> onOpen)
        {
            bool createdNew;
            try
            {
                _mutex = new Mutex(true, MutexName, out createdNew);
            }
            catch
            {
                // Without a mutex, run as if there were no other copy.
                createdNew = true;
            }

            if (createdNew)
            {
                _window = new HandoffWindow();
                _window.Opened += delegate (string payload)
                {
                    if (onOpen != null) onOpen(BrowserLaunch.FromJson(payload));
                };

                // Force the handle now, so an early second copy can find it.
                IntPtr handle = _window.Handle;
                GC.KeepAlive(handle);
                return true;
            }

            HandOff(launch);
            return false;
        }

        private static void HandOff(BrowserLaunch launch)
        {
            string payload = launch == null ? "{}" : launch.ToJson();

            // The first copy may still be building its window; give it a moment.
            for (int attempt = 0; attempt < 40; attempt++)
            {
                IntPtr target = FindWindow(null, WindowTitle);
                if (target != IntPtr.Zero && SendPayload(target, payload)) return;

                Thread.Sleep(50);
            }
        }

        private static bool SendPayload(IntPtr target, string payload)
        {
            IntPtr buffer = IntPtr.Zero;
            try
            {
                buffer = Marshal.StringToHGlobalUni(payload);

                var data = new CopyDataStruct();
                data.dwData = IntPtr.Zero;
                data.cbData = (payload.Length + 1) * 2;
                data.lpData = buffer;

                SendMessage(target, WM_COPYDATA, IntPtr.Zero, ref data);
                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                if (buffer != IntPtr.Zero) Marshal.FreeHGlobal(buffer);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct CopyDataStruct
        {
            public IntPtr dwData;
            public int cbData;
            public IntPtr lpData;
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr FindWindow(string className, string windowName);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr window, int message, IntPtr wParam, ref CopyDataStruct lParam);

        /// <summary>
        /// A window that is never shown, exists only to receive the hand-off
        /// from another copy of Toge.
        /// </summary>
        private class HandoffWindow : Form
        {
            public event Action<string> Opened;

            public HandoffWindow()
            {
                Text = WindowTitle;
                FormBorderStyle = FormBorderStyle.None;
                ShowInTaskbar = false;
                StartPosition = FormStartPosition.Manual;
                Location = new Point(-4000, -4000);
                Size = new Size(1, 1);
            }

            protected override void SetVisibleCore(bool value)
            {
                // Created, but never shown: a hidden top-level window is still
                // found by FindWindow.
                base.SetVisibleCore(false);
            }

            protected override void WndProc(ref Message m)
            {
                if (m.Msg == WM_COPYDATA)
                {
                    try
                    {
                        var data = (CopyDataStruct)Marshal.PtrToStructure(m.LParam, typeof(CopyDataStruct));
                        if (data.lpData != IntPtr.Zero)
                        {
                            string payload = Marshal.PtrToStringUni(data.lpData);
                            if (Opened != null && !string.IsNullOrEmpty(payload)) Opened(payload);
                        }
                    }
                    catch
                    {
                        // A bad hand-off must never bring the browser down.
                    }

                    m.Result = (IntPtr)1;
                    return;
                }

                base.WndProc(ref m);
            }
        }
    }
}
