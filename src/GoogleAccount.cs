using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;

namespace SimpleBrowser
{
    /// <summary>
    /// The Google account this profile is signed in with.
    ///
    /// The session itself is nothing more than cookies in the WebView2 profile,
    /// so any site offering "Sign in with Google" already sees the account —
    /// nothing here is needed for that. This class only remembers *who* that
    /// account is, so the start page can show it and offer a way in.
    ///
    /// The name, address and picture are read from the account chip Google
    /// already draws on its own pages; no extra request is ever made, and
    /// nothing leaves the machine (a small cache lives in the profile folder).
    /// </summary>
    internal static class GoogleAccount
    {
        private const string FileName = "google.json";

        /// <summary>Raised when the signed-in account or its details change.</summary>
        public static event Action Changed;

        private static bool _loaded;
        private static bool _signedIn;
        private static string _name = "";
        private static string _email = "";
        private static string _avatar = "";

        public static bool SignedIn { get { return _signedIn; } }
        public static string Name { get { return _name; } }
        public static string Email { get { return _email; } }
        public static string Avatar { get { return _avatar; } }

        /// <summary>
        /// Runs on a Google page and reports the account that page is showing:
        /// the account button's label, which carries the name and the address,
        /// and the profile picture next to it.
        /// </summary>
        public const string CaptureScript =
            @"(function(){try{var nodes=document.querySelectorAll('[aria-label]');" +
            @"for(var i=0;i<nodes.length;i++){var label=nodes[i].getAttribute('aria-label')||'';" +
            @"if(label.indexOf('@')>0&&label.indexOf(':')>0){" +
            @"var pic=document.querySelector('img[src*=""googleusercontent""]');" +
            @"return JSON.stringify({label:label,avatar:pic?pic.src:''});}}" +
            @"var only=document.querySelector('img[src*=""googleusercontent""]');" +
            @"if(only){return JSON.stringify({label:'',avatar:only.src});}" +
            @"}catch(err){}return '';})()";

        private static string FilePath
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Toge", FileName);
            }
        }

        /// <summary>Reads the cached account once, at startup.</summary>
        public static void Load()
        {
            if (_loaded) return;
            _loaded = true;

            try
            {
                if (!File.Exists(FilePath)) return;

                var stored = Json.AsObject(Json.Parse(File.ReadAllText(FilePath)));
                if (stored == null) return;

                _signedIn = Json.GetBool(stored, "signedIn", false);
                _name = Json.GetString(stored, "name");
                _email = Json.GetString(stored, "email");
                _avatar = Json.GetString(stored, "avatar");
            }
            catch
            {
                // A cache that cannot be read is simply an empty one.
            }
        }

        /// <summary>
        /// Checks the profile cookies, which is the only truth about whether
        /// Google still considers this browser signed in.
        /// </summary>
        public static async Task RefreshAsync(CoreWebView2 webView)
        {
            if (webView == null) return;

            bool signedIn = false;

            try
            {
                var cookies = await webView.CookieManager.GetCookiesAsync("https://accounts.google.com/");
                foreach (var cookie in cookies)
                {
                    if (cookie.Name == "SID" || cookie.Name == "__Secure-1PSID" || cookie.Name == "HSID")
                    {
                        signedIn = true;
                        break;
                    }
                }
            }
            catch
            {
                // No answer from the profile; keep what is already known.
                return;
            }

            if (signedIn == _signedIn) return;

            _signedIn = signedIn;
            if (!signedIn) ClearDetails();

            Save();
            Raise();
        }

        /// <summary>Takes the raw result of <see cref="CaptureScript"/>.</summary>
        public static void Capture(string scriptResult)
        {
            if (string.IsNullOrEmpty(scriptResult)) return;

            try
            {
                string json = Regex.Unescape(scriptResult.Trim('"'));
                if (json.Length == 0 || json == "null") return;

                var found = Json.AsObject(Json.Parse(json));
                if (found == null) return;

                string label = Json.GetString(found, "label");
                string avatar = Json.GetString(found, "avatar");
                string name = "";
                string email = "";

                if (!string.IsNullOrEmpty(label))
                {
                    // Google words the label in the page's own language, but the
                    // address in brackets and the name before it always travel
                    // together: "...: João Silva (joaosilva7875@gmail.com)".
                    var match = Regex.Match(label, @"\(([^()\s]+@[^()\s]+)\)");
                    if (match.Success)
                    {
                        email = match.Groups[1].Value;

                        int colon = label.IndexOf(':');
                        if (colon >= 0 && match.Index > colon + 1)
                        {
                            name = label.Substring(colon + 1, match.Index - colon - 1).Trim();
                        }
                    }
                }

                bool changed = false;

                if (email.Length > 0 && email != _email) { _email = email; changed = true; }
                if (name.Length > 0 && name != _name) { _name = name; changed = true; }
                if (avatar.Length > 0 && avatar != _avatar) { _avatar = avatar; changed = true; }
                if (!_signedIn) { _signedIn = true; changed = true; }

                if (changed)
                {
                    Save();
                    Raise();
                }
            }
            catch
            {
                // Anything unexpected on the page is ignored; the chip keeps
                // showing whatever it already knew.
            }
        }

        /// <summary>The account, as the start page's state object.</summary>
        public static object State()
        {
            return Json.Obj(
                "signedIn", _signedIn,
                "name", _name,
                "email", _email,
                "avatar", _avatar);
        }

        private static void ClearDetails()
        {
            _name = "";
            _email = "";
            _avatar = "";
        }

        private static void Save()
        {
            try
            {
                string folder = Path.GetDirectoryName(FilePath);
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                string json = Json.Write(Json.Obj(
                    "signedIn", _signedIn,
                    "name", _name,
                    "email", _email,
                    "avatar", _avatar));

                if (json != null) File.WriteAllText(FilePath, json);
            }
            catch
            {
                // Losing the cache only means asking Google again later.
            }
        }

        private static void Raise()
        {
            if (Changed != null) Changed();
        }
    }
}
