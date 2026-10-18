using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;

namespace SimpleBrowser
{
    /// <summary>
    /// The one WebView2 environment shared by the chrome and every tab.
    ///
    /// The user data folder is persistent, and that is what makes Aura remember
    /// things between runs: sign-in cookies, saved logins, localStorage,
    /// IndexedDB, HTTP auth and the HTTP cache all live there. Every WebView2
    /// created for this profile must come from this environment, because
    /// assigning <c>NewWindowRequested.NewWindow</c> (used for OAuth popups)
    /// only works between WebViews of the same environment.
    /// </summary>
    public static class BrowserEnvironment
    {
        private static readonly object Gate = new object();
        private static Task<CoreWebView2Environment> _initialization;

        public static string UserDataFolder
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "AuraBrowser",
                    "WebView2Profile");
            }
        }

        public static Task<CoreWebView2Environment> GetAsync()
        {
            lock (Gate)
            {
                // Retry after a failed start instead of caching the fault forever.
                if (_initialization == null || _initialization.IsFaulted || _initialization.IsCanceled)
                {
                    _initialization = CreateAsync();
                }

                return _initialization;
            }
        }

        private static async Task<CoreWebView2Environment> CreateAsync()
        {
            string folder = UserDataFolder;
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            // No custom user agent on purpose. WebView2 defaults to the Edge
            // agent, which Google accepts for sign-in; a UA that advertises an
            // embedded browser would get the "not secure" block instead.
            return await CoreWebView2Environment.CreateAsync(null, folder);
        }
    }
}
