using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace SimpleBrowser
{
    static class Program
    {
        [DllImport("user32.dll")]
        private static extern bool SetProcessDpiAwarenessContext(IntPtr value);

        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        [STAThread]
        static void Main(string[] args)
        {
            EnableHighDpi();
            Dpi.Init();

            // Without a targetFramework attribute the engine offers the old
            // ciphers only; modern suggestion endpoints want TLS 1.2.
            try
            {
                System.Net.ServicePointManager.SecurityProtocol =
                    System.Net.SecurityProtocolType.Tls12
                    | System.Net.SecurityProtocolType.Tls11
                    | System.Net.SecurityProtocolType.Tls;
            }
            catch
            {
            }

            EnsureModernBrowserEmulation();
            AppMigration.Migrate();
            GoogleAccount.Load();
            FaviconStore.Load();

            var launch = BrowserLaunch.Parse(args);

            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                // One copy per user session: a second launch posts what it was
                // asked to open to the copy already running, and stops here.
                if (!SingleInstance.Claim(launch, OpenFromHandOff)) return;

                var windows = NewWindowsFor(launch);

                // Every window after the first is shown before the message loop
                // starts, with the first one left for Application.Run to own.
                for (int i = 1; i < windows.Count; i++) windows[i].Show();

                Application.Run(windows[0]);
            }
            catch (Exception ex)
            {
                File.WriteAllText("crash.log", ex.ToString());
                MessageBox.Show("Fatal Error: " + ex.ToString(), "Browser Crash", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>The windows this launch should open, already built.</summary>
        private static List<MainForm> NewWindowsFor(BrowserLaunch launch)
        {
            var windows = new List<MainForm>();

            if (launch.Private)
            {
                windows.Add(new MainForm(true));
                return windows;
            }

            if (!string.IsNullOrEmpty(launch.Url))
            {
                windows.Add(new MainForm(false, new List<string> { launch.Url }, 0));
                return windows;
            }

            // No page was named, so bring back the last session when there is
            // one to bring back.
            var restored = SessionStore.Restore();
            if (restored != null)
            {
                foreach (var session in restored)
                {
                    windows.Add(new MainForm(false, session.Tabs, session.Active));
                }
            }

            if (windows.Count == 0) windows.Add(new MainForm(false));
            return windows;
        }

        /// <summary>
        /// Opens a page a second copy of Toge handed over, in a window that is
        /// already open where that makes sense.
        /// </summary>
        private static void OpenFromHandOff(BrowserLaunch launch)
        {
            if (launch == null) return;

            if (launch.Private)
            {
                MainForm.NewWindow(true);
                return;
            }

            if (string.IsNullOrEmpty(launch.Url))
            {
                MainForm.NewWindow(false);
                return;
            }

            var target = MainForm.Target;
            if (!launch.NewWindow && target != null)
            {
                target.OpenUrl(launch.Url);
                return;
            }

            MainForm.NewWindowWith(launch.Url);
        }

        /// <summary>
        /// Opt into native DPI rendering. Without this Windows bitmap-stretches
        /// the whole window on scaled displays, which makes WebView2 and the
        /// chrome look soft no matter how high the monitor resolution is.
        /// </summary>
        private static void EnableHighDpi()
        {
            try
            {
                // DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2
                if (SetProcessDpiAwarenessContext(new IntPtr(-4))) return;
            }
            catch
            {
                // Older Windows: fall through to system DPI awareness.
            }

            try
            {
                SetProcessDPIAware();
            }
            catch
            {
                // Leave the process DPI-unaware if neither API is available.
            }
        }

        private static void EnsureModernBrowserEmulation()
        {
            try
            {
                string processName = Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName);
                using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION"))
                {
                    if (key != null)
                    {
                        // 11001 (0x2AF9): Internet Explorer 11 mode
                        key.SetValue(processName, 11001, RegistryValueKind.DWord);
                    }
                }
            }
            catch
            {
                // Ignore registry permission limitations if any
            }
        }
    }
}
