using System;
using System.IO;

namespace SimpleBrowser
{
    /// <summary>
    /// Carries the profile over from the name the browser shipped under
    /// before it became Toge, so bookmarks, history, sessions, cookies and
    /// saved logins are not lost by the rename. Runs once: if the new folder
    /// already exists, nothing is touched.
    /// </summary>
    internal static class AppMigration
    {
        private const string LegacyFolder = "AuraBrowser";

        public static void Migrate()
        {
            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string legacy = Path.Combine(appData, LegacyFolder);
                string current = Path.Combine(appData, "Toge");

                if (!Directory.Exists(current) && Directory.Exists(legacy))
                {
                    Directory.Move(legacy, current);
                }
            }
            catch
            {
                // A locked file must never keep the browser from opening; the
                // next launch will try again.
            }
        }
    }
}
