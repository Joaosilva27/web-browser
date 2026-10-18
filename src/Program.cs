using System;
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
        static void Main()
        {
            try
            {
                EnableHighDpi();
                Dpi.Init();
                EnsureModernBrowserEmulation();

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                File.WriteAllText("crash.log", ex.ToString());
                MessageBox.Show("Fatal Error: " + ex.ToString(), "Browser Crash", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
