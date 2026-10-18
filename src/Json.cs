using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Web.Script.Serialization;

namespace SimpleBrowser
{
    /// <summary>
    /// Thin wrapper over the framework's JavaScriptSerializer. Used for Aura's
    /// own small data files (bookmarks, favourites) and for reading another
    /// browser's bookmark JSON. Serialization produces whatever the framework
    /// serializer produces, so it is always safe to read back with Parse.
    /// </summary>
    public static class Json
    {
        private static JavaScriptSerializer NewSerializer()
        {
            var serializer = new JavaScriptSerializer();
            serializer.MaxJsonLength = int.MaxValue;
            serializer.RecursionLimit = 512;
            return serializer;
        }

        public static object Parse(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;
            try
            {
                return NewSerializer().DeserializeObject(text);
            }
            catch
            {
                return null;
            }
        }

        public static string Write(object value)
        {
            try
            {
                return NewSerializer().Serialize(value);
            }
            catch
            {
                return null;
            }
        }

        public static Dictionary<string, object> AsObject(object value)
        {
            return value as Dictionary<string, object>;
        }

        public static List<object> AsList(object value)
        {
            var array = value as object[];
            if (array != null) return new List<object>(array);

            var list = value as System.Collections.IEnumerable;
            if (list != null && !(value is string))
            {
                var result = new List<object>();
                foreach (var item in list) result.Add(item);
                return result;
            }

            return null;
        }

        public static string Str(object value)
        {
            if (value == null) return null;
            return value as string;
        }

        /// <summary>Reads a string member, tolerating missing keys.</summary>
        public static string GetString(Dictionary<string, object> obj, string key)
        {
            if (obj == null) return null;
            object value;
            if (!obj.TryGetValue(key, out value)) return null;
            if (value == null) return null;

            var text = value as string;
            if (text != null) return text;

            var convertible = value as IConvertible;
            if (convertible != null) return convertible.ToString(CultureInfo.InvariantCulture);
            return null;
        }

        public static bool GetBool(Dictionary<string, object> obj, string key, bool fallback)
        {
            if (obj == null) return fallback;

            object value;
            if (!obj.TryGetValue(key, out value) || value == null) return fallback;

            if (value is bool) return (bool)value;

            var text = value as string;
            if (text != null)
            {
                bool parsed;
                if (bool.TryParse(text, out parsed)) return parsed;
                return fallback;
            }

            var convertible = value as IConvertible;
            if (convertible != null)
            {
                try { return convertible.ToBoolean(CultureInfo.InvariantCulture); }
                catch { }
            }
            return fallback;
        }

        public static int GetInt(Dictionary<string, object> obj, string key, int fallback)
        {
            if (obj == null) return fallback;

            object value;
            if (!obj.TryGetValue(key, out value) || value == null) return fallback;

            var text = value as string;
            if (text != null)
            {
                int parsed;
                if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed)) return parsed;
                double asDouble;
                if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out asDouble))
                {
                    return (int)Math.Round(asDouble);
                }
                return fallback;
            }

            var convertible = value as IConvertible;
            if (convertible != null)
            {
                try { return (int)Math.Round(convertible.ToDouble(CultureInfo.InvariantCulture)); }
                catch { }
            }
            return fallback;
        }

        public static Dictionary<string, object> Obj(params object[] pairs)
        {
            var result = new Dictionary<string, object>();
            for (int i = 0; i + 1 < pairs.Length; i += 2)
            {
                result[(string)pairs[i]] = pairs[i + 1];
            }
            return result;
        }

        public static string Escape(string text)
        {
            if (text == null) return string.Empty;

            var builder = new StringBuilder(text.Length + 8);
            foreach (char c in text)
            {
                switch (c)
                {
                    case '"': builder.Append("\\\""); break;
                    case '\\': builder.Append("\\\\"); break;
                    case '\n': builder.Append("\\n"); break;
                    case '\r': builder.Append("\\r"); break;
                    case '\t': builder.Append("\\t"); break;
                    default:
                        if (c < ' ') builder.Append("\\u").Append(((int)c).ToString("x4"));
                        else builder.Append(c);
                        break;
                }
            }
            return builder.ToString();
        }
    }
}
