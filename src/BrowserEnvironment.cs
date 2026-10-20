using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace SimpleBrowser
{
    /// <summary>
    /// The one WebView2 environment shared by the chrome and every tab.
    ///
    /// The user data folder is persistent, and that is what makes Toge remember
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
                    "Toge",
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

        /// <summary>
        /// Controller options for a tab or a window. A private window is given
        /// an in-private profile, which WebView2 keeps in memory only, so the
        /// sign-in cookies and history it collects are gone with the window.
        /// A normal window needs no options at all.
        /// </summary>
        public static CoreWebView2ControllerOptions CreateOptions(CoreWebView2Environment environment, bool privateMode)
        {
            if (environment == null || !privateMode) return null;

            try
            {
                var options = environment.CreateCoreWebView2ControllerOptions();
                options.IsInPrivateModeEnabled = true;
                return options;
            }
            catch
            {
                // An older runtime without private profiles falls back to the
                // shared one rather than refusing to open the window.
                return null;
            }
        }

        /// <summary>
        /// Makes sure a WebView2 is built on the right profile: an in-private
        /// one for a private window, the shared persistent one otherwise.
        /// </summary>
        public static async Task InitializeAsync(WebView2 view, bool privateMode)
        {
            var environment = await GetAsync();
            var options = CreateOptions(environment, privateMode);

            if (options == null) await view.EnsureCoreWebView2Async(environment);
            else await view.EnsureCoreWebView2Async(environment, options);
        }

        /// <summary>
        /// The controller behind a WinForms WebView2 control.
        ///
        /// The control keeps its controller private, and the controller is the
        /// only place the accelerator-key event exists. Without it Toge could not
        /// answer Ctrl+F or Ctrl+T while a page has focus: the keys never reach
        /// the application's message filter, because the engine consumes them
        /// first. The SDK is fixed at the version in lib, so the field is stable;
        /// if a future runtime renames it, this returns null and the engine's own
        /// shortcut handling simply stays in charge.
        /// </summary>
        public static CoreWebView2Controller ControllerOf(WebView2 view)
        {
            if (view == null) return null;

            try
            {
                FieldInfo field = typeof(WebView2).GetField(
                    "_coreWebView2Controller",
                    BindingFlags.NonPublic | BindingFlags.Instance);

                if (field == null) return null;

                return field.GetValue(view) as CoreWebView2Controller;
            }
            catch
            {
                return null;
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
