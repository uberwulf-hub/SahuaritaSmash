// Errors written down instead of ending the game (App Hub's Classic Games has a fuller one, Engine\CrashLog.cs: this
// keeps its Write so the moved code stays the same). Lines go to errors.log in Data's folder when one is set.
using System;
using System.IO;

namespace ClassicGames
{
    public static class CrashLog
    {
        /// <summary>The file errors are added to (null: only the debug output, as in the checks).</summary>
        public static string Path;
        static readonly object sync = new object();

        /// <summary>Writes an error with where it happened; fatal = the app is about to close.</summary>
        public static void Write(string where, Exception ex, bool fatal)
        {
            string line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  " + (fatal ? "CRASH " : "") + where + ": " + (ex == null ? "" : ex.GetType().Name + ": " + ex.Message + Environment.NewLine + ex.StackTrace) + Environment.NewLine;
            System.Diagnostics.Debug.WriteLine(line);
            if (Path == null) return;
            try { lock (sync) File.AppendAllText(Path, line); } catch (Exception) { }
        }
    }
}
