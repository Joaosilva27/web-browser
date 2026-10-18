using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace SimpleBrowser
{
    /// <summary>
    /// Read-only SQLite access through the SQLite copy that ships with Windows
    /// (winsqlite3.dll). This is what lets Aura read another Chromium browser's
    /// history and top sites without shipping a database engine of its own.
    ///
    /// The source file is copied to a scratch file first, because browser
    /// profiles keep it open and may have a journal alongside it. One row is
    /// returned per string array, one entry per column; anything that is not
    /// text comes back in its SQLite text form.
    /// </summary>
    public static class Sqlite
    {
        private const int SQLITE_OK = 0;
        private const int SQLITE_ROW = 100;
        private const int SQLITE_OPEN_READONLY = 0x00000001;

        [DllImport("winsqlite3.dll", EntryPoint = "sqlite3_open_v2", CallingConvention = CallingConvention.Cdecl)]
        private static extern int Open(byte[] filename, out IntPtr db, int flags, IntPtr vfs);

        [DllImport("winsqlite3.dll", EntryPoint = "sqlite3_prepare_v2", CallingConvention = CallingConvention.Cdecl)]
        private static extern int Prepare(IntPtr db, byte[] sql, int byteCount, out IntPtr statement, IntPtr tail);

        [DllImport("winsqlite3.dll", EntryPoint = "sqlite3_step", CallingConvention = CallingConvention.Cdecl)]
        private static extern int Step(IntPtr statement);

        [DllImport("winsqlite3.dll", EntryPoint = "sqlite3_column_count", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ColumnCount(IntPtr statement);

        [DllImport("winsqlite3.dll", EntryPoint = "sqlite3_column_text", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr ColumnText(IntPtr statement, int index);

        [DllImport("winsqlite3.dll", EntryPoint = "sqlite3_column_bytes", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ColumnBytes(IntPtr statement, int index);

        [DllImport("winsqlite3.dll", EntryPoint = "sqlite3_finalize", CallingConvention = CallingConvention.Cdecl)]
        private static extern int Finalize(IntPtr statement);

        [DllImport("winsqlite3.dll", EntryPoint = "sqlite3_close", CallingConvention = CallingConvention.Cdecl)]
        private static extern int Close(IntPtr db);

        [DllImport("winsqlite3.dll", EntryPoint = "sqlite3_errmsg", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr ErrorMessage(IntPtr db);

        /// <summary>
        /// Runs a query and returns every row. Never throws: a database that
        /// cannot be read simply yields no rows, and the caller reports "nothing
        /// found" rather than an error the user cannot act on.
        /// </summary>
        public static List<string[]> Query(string databasePath, string sql, int maxRows)
        {
            var rows = new List<string[]>();
            if (string.IsNullOrEmpty(databasePath) || !File.Exists(databasePath)) return rows;

            string scratch = Path.Combine(
                Path.GetTempPath(),
                "aura-read-" + Guid.NewGuid().ToString("N") + ".db");

            IntPtr db = IntPtr.Zero;
            IntPtr statement = IntPtr.Zero;

            try
            {
                if (!CopyShared(databasePath, scratch)) return rows;

                if (Open(NullTerminated(scratch), out db, SQLITE_OPEN_READONLY, IntPtr.Zero) != SQLITE_OK)
                {
                    return rows;
                }

                byte[] query = NullTerminated(sql);
                if (Prepare(db, query, query.Length, out statement, IntPtr.Zero) != SQLITE_OK)
                {
                    return rows;
                }

                int columns = ColumnCount(statement);
                while (Step(statement) == SQLITE_ROW)
                {
                    var row = new string[columns];
                    for (int i = 0; i < columns; i++)
                    {
                        row[i] = ReadText(statement, i);
                    }
                    rows.Add(row);

                    if (maxRows > 0 && rows.Count >= maxRows) break;
                }
            }
            catch (DllNotFoundException)
            {
                // winsqlite3.dll is missing on this Windows build; import what we can.
            }
            catch (Exception)
            {
                // Locked or malformed database: report nothing rather than failing the import.
            }
            finally
            {
                if (statement != IntPtr.Zero)
                {
                    try { Finalize(statement); } catch { }
                }
                if (db != IntPtr.Zero)
                {
                    try { Close(db); } catch { }
                }
                try { if (File.Exists(scratch)) File.Delete(scratch); } catch { }
            }

            return rows;
        }

        /// <summary>
        /// True when the database can be opened at all, so the UI can avoid
        /// offering an import that would silently produce nothing.
        /// </summary>
        public static bool CanRead(string databasePath)
        {
            if (string.IsNullOrEmpty(databasePath) || !File.Exists(databasePath)) return false;
            return Query(databasePath, "SELECT 1", 1).Count == 1;
        }

        private static bool CopyShared(string source, string destination)
        {
            try
            {
                using (var input = new FileStream(source, FileMode.Open, FileAccess.Read,
                           FileShare.ReadWrite | FileShare.Delete))
                using (var output = new FileStream(destination, FileMode.Create, FileAccess.Write,
                           FileShare.None))
                {
                    input.CopyTo(output);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static byte[] NullTerminated(string text)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            var result = new byte[bytes.Length + 1];
            Array.Copy(bytes, result, bytes.Length);
            return result;
        }

        private static string ReadText(IntPtr statement, int index)
        {
            IntPtr pointer = ColumnText(statement, index);
            if (pointer == IntPtr.Zero) return string.Empty;

            int length = ColumnBytes(statement, index);
            if (length <= 0) return string.Empty;

            var buffer = new byte[length];
            Marshal.Copy(pointer, buffer, 0, length);
            return Encoding.UTF8.GetString(buffer);
        }
    }
}
