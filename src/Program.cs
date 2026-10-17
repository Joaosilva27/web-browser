using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;

namespace SimpleBrowser
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            EnsureModernBrowserEmulation();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
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
