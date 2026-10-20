using System;
using System.Diagnostics;
using Microsoft.Win32;

namespace SimpleBrowser
{
    /// <summary>
    /// Aura as the system's default browser.
    ///
    /// Windows no longer lets an app quietly take the job over, so Aura writes
    /// its registration under HKCU (which needs no administrator rights) and
    /// then opens the Windows page where the user does the picking. Everything
    /// here is about being honest about that: Aura can say where things stand,
    /// and it can take the user to the place the choice is actually made.
    /// </summary>
    public static class DefaultBrowser
    {
        /// <summary>The key Windows records the user's choice under.</summary>
        private const string ProgId = "AuraBrowser";
        private const string AppName = "Aura";

        /// <summary>True when links currently open in Aura.</summary>
        public static bool IsDefault()
        {
            string http = ProgIdFor("http");
            string https = ProgIdFor("https");
            return http == ProgId && https == ProgId;
        }

        private static string ProgIdFor(string scheme)
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\Shell\Associations\UrlAssociations\"
                    + scheme + @"\UserChoice"))
                {
                    if (key == null) return null;
                    return key.GetValue("ProgId") as string;
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Writes the registration Windows reads to list Aura as a browser.
        /// The user still has to choose it; this only makes it choosable.
        /// </summary>
        public static bool Register()
        {
            try
            {
                string exe = ExecutablePath();
                if (string.IsNullOrEmpty(exe)) return false;

                using (var command = Registry.CurrentUser.CreateSubKey(
                    @"Software\Classes\" + ProgId + @"\shell\open\command"))
                {
                    if (command == null) return false;
                    command.SetValue(null, "\"" + exe + "\" \"%1\"");
                }

                using (var icon = Registry.CurrentUser.CreateSubKey(
                    @"Software\Classes\" + ProgId + @"\DefaultIcon"))
                {
                    if (icon != null) icon.SetValue(null, "\"" + exe + "\",0");
                }

                using (var capabilities = Registry.CurrentUser.CreateSubKey(
                    @"Software\Classes\" + ProgId + @"\Capabilities"))
                {
                    if (capabilities == null) return false;
                    capabilities.SetValue("ApplicationName", AppName);
                    capabilities.SetValue("ApplicationDescription", "Aura, a fast browser");
                }

                using (var urls = Registry.CurrentUser.CreateSubKey(
                    @"Software\Classes\" + ProgId + @"\Capabilities\URLAssociations"))
                {
                    if (urls == null) return false;
                    urls.SetValue("http", ProgId);
                    urls.SetValue("https", ProgId);
                }

                using (var applications = Registry.CurrentUser.CreateSubKey(
                    @"Software\RegisteredApplications"))
                {
                    if (applications == null) return false;
                    applications.SetValue(AppName, @"Software\Classes\" + ProgId + @"\Capabilities");
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>Opens the Windows page where the default browser is chosen.</summary>
        public static void OpenWindowsSettings()
        {
            try
            {
                Process.Start(new ProcessStartInfo("ms-settings:defaultapps") { UseShellExecute = true });
            }
            catch
            {
                try { Process.Start("ms-settings:defaultapps"); }
                catch { }
            }
        }

        private static string ExecutablePath()
        {
            try
            {
                var module = Process.GetCurrentProcess().MainModule;
                if (module != null && !string.IsNullOrEmpty(module.FileName)) return module.FileName;
            }
            catch
            {
                // Fall back to the assembly's own location.
            }

            try
            {
                return System.Reflection.Assembly.GetEntryAssembly().Location;
            }
            catch
            {
                return null;
            }
        }
    }
}
