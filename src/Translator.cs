using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;

namespace SimpleBrowser
{
    /// <summary>
    /// The machine behind Toge's Translate option: Google's dictionary
    /// endpoint, the one Chrome's translator extensions use. It takes several
    /// strings per request (one "q" each) and answers in the same order, with
    /// the language it detected — no key, no markers, nothing to misalign.
    ///
    /// A page is sent in small batches so the address never grows past what
    /// the service likes, and a throttled batch is simply given up on: half a
    /// translated page beats a stalled one.
    /// </summary>
    public static class Translator
    {
        /// <summary>Roughly how much text one request should carry.</summary>
        public const int MaxBatchChars = 800;

        private const string Endpoint =
            "https://clients5.google.com/translate_a/t?client=dict-chrome-ex&sl=auto&tl={0}&dt=t";

        /// <summary>English names, for the prompt and the toast.</summary>
        public static string LanguageName(string code)
        {
            if (string.IsNullOrEmpty(code)) return "this language";

            switch (code.Trim().ToLowerInvariant())
            {
                case "en": return "English";
                case "pt": return "Portuguese";
                case "es": return "Spanish";
                case "fr": return "French";
                case "de": return "German";
                case "it": return "Italian";
                case "nl": return "Dutch";
                case "pl": return "Polish";
                case "ru": return "Russian";
                case "uk": return "Ukrainian";
                case "ja": return "Japanese";
                case "ko": return "Korean";
                case "zh": return "Chinese";
                case "ar": return "Arabic";
                case "tr": return "Turkish";
                case "sv": return "Swedish";
                case "da": return "Danish";
                case "no": return "Norwegian";
                case "fi": return "Finnish";
                case "cs": return "Czech";
                case "el": return "Greek";
                case "he": return "Hebrew";
                case "hu": return "Hungarian";
                case "ro": return "Romanian";
                case "id": return "Indonesian";
                case "vi": return "Vietnamese";
                case "th": return "Thai";
                case "hi": return "Hindi";
                default: return code.ToUpperInvariant();
            }
        }

        /// <summary>Translates one string, or null when the service cannot.</summary>
        public static string Translate(string text, string target)
        {
            if (string.IsNullOrEmpty(text)) return null;

            var one = new List<string>();
            one.Add(text);
            var translated = TranslateMany(one, target);
            return translated == null ? null : translated[0];
        }

        /// <summary>
        /// Translates a group of strings in one request. Returns one entry per
        /// input, in the same order, or null when the batch did not come back
        /// whole.
        /// </summary>
        public static List<string> TranslateMany(IList<string> texts, string target)
        {
            if (texts == null || texts.Count == 0) return null;

            var url = new StringBuilder();
            url.Append(string.Format(
                CultureInfo.InvariantCulture,
                Endpoint,
                Uri.EscapeDataString(string.IsNullOrEmpty(target) ? "en" : target)));

            for (int i = 0; i < texts.Count; i++)
            {
                url.Append("&q=").Append(Uri.EscapeDataString(texts[i] ?? string.Empty));
            }

            string raw = Request(url.ToString());
            var parsed = Parse(raw, texts.Count);
            if (parsed != null) return parsed;

            // One more try, slowly, in case the service was merely busy.
            System.Threading.Thread.Sleep(650);
            raw = Request(url.ToString());
            return Parse(raw, texts.Count);
        }

        /// <summary>
        /// Reads the answer: an array with one entry per "q", each entry a pair
        /// of translated text and detected language.
        /// </summary>
        private static List<string> Parse(string raw, int expected)
        {
            if (string.IsNullOrEmpty(raw)) return null;

            var root = Json.AsList(Json.Parse(raw));
            if (root == null) return null;

            var results = new List<string>();
            foreach (var item in root)
            {
                string piece = Json.Str(item);
                if (piece == null)
                {
                    var pair = Json.AsList(item);
                    piece = pair != null && pair.Count > 0 ? Json.Str(pair[0]) : null;
                }
                results.Add(piece);
            }

            if (results.Count != expected) return null;

            // Any null fragment means the service reshaped the answer; the
            // caller would paste the wrong words into the page, so refuse.
            for (int i = 0; i < results.Count; i++)
            {
                if (results[i] == null) return null;
            }
            return results;
        }

        private static string Request(string url)
        {
            try
            {
                var request = (HttpWebRequest)WebRequest.Create(url);
                request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
                    "(KHTML, like Gecko) Chrome/120.0 Safari/537.36";
                request.Timeout = 15000;
                request.ReadWriteTimeout = 15000;
                request.Proxy = WebRequest.DefaultWebProxy;

                using (var response = (HttpWebResponse)request.GetResponse())
                using (var reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                {
                    return reader.ReadToEnd();
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
