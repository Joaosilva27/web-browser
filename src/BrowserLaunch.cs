using System;
using System.Collections.Generic;

namespace SimpleBrowser
{
    /// <summary>
    /// What Toge was asked to do when it was started: open a particular page,
    /// open a private window, or just carry on from the last session. It is the
    /// shape of a command line and also the shape handed to a running Toge when
    /// the user clicks a link while the browser is already open.
    /// </summary>
    public class BrowserLaunch
    {
        public string Url;
        public bool Private;
        public bool NewWindow;

        public static BrowserLaunch Parse(string[] args)
        {
            var launch = new BrowserLaunch();
            if (args == null) return launch;

            foreach (string raw in args)
            {
                if (string.IsNullOrEmpty(raw)) continue;

                string arg = raw.Trim();
                if (arg.Length == 0) continue;

                if (IsSwitch(arg, "private", "incognito"))
                {
                    launch.Private = true;
                    continue;
                }

                if (IsSwitch(arg, "new-window", "newwindow", "new"))
                {
                    launch.NewWindow = true;
                    continue;
                }

                // Anything else that starts with a dash is a switch Toge does
                // not know; ignore it rather than treating it as a page.
                if (arg.StartsWith("-")) continue;

                if (launch.Url == null) launch.Url = arg;
            }

            return launch;
        }

        private static bool IsSwitch(string arg, params string[] names)
        {
            string trimmed = arg.TrimStart('-', '/');
            foreach (string name in names)
            {
                if (string.Equals(trimmed, name, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        public string ToJson()
        {
            var map = Json.Obj(
                "url", Url == null ? string.Empty : Url,
                "private", Private,
                "newWindow", NewWindow);
            return Json.Write(map) ?? "{}";
        }

        public static BrowserLaunch FromJson(string json)
        {
            var launch = new BrowserLaunch();
            if (string.IsNullOrEmpty(json)) return launch;

            try
            {
                var map = Json.AsObject(Json.Parse(json));
                if (map == null) return launch;

                launch.Url = Json.GetString(map, "url");
                launch.Private = Json.GetBool(map, "private", false);
                launch.NewWindow = Json.GetBool(map, "newWindow", false);
            }
            catch
            {
                // A malformed hand-off is treated as a plain new window.
            }

            return launch;
        }
    }
}
