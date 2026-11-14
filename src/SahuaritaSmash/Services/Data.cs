// Where Sahuarita Smash! keeps its things: the ladder scores (scores.txt, App Hub's scorebook format), the keys each
// player chose (brawl-keys.txt), a match kept when the app was closed (update-resume.txt) and errors.log. Windows:
// %APPDATA%\SahuaritaSmashSE; Android: the app's own files folder. The checks use a folder in the temp folder.
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace SahuaritaSmash.Services
{
    public static class Data
    {
        static string dir;

        /// <summary>The folder (made when first asked for).</summary>
        public static string Dir
        {
            get
            {
                if (dir == null) dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SahuaritaSmashSE");
                Directory.CreateDirectory(dir);
                return dir;
            }
        }

        public static string Scores { get { return Path.Combine(Dir, "scores.txt"); } }
        public static string Errors { get { return Path.Combine(Dir, "errors.log"); } }

        /// <summary>The checks: a new empty folder in the temp folder, never the person's real one.</summary>
        public static string UseTestFolder()
        {
            dir = Path.Combine(Path.GetTempPath(), "SahuaritaSmash-test", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            return dir;
        }

        /// <summary>Points at a folder (the checks, with fixtures).</summary>
        public static void UseFolder(string folder) { dir = folder; }

        /// <summary>Once, on Windows: brings App Hub's Classic Games saves for this game across (%APPDATA%\ClassicGames:
        /// the scorebook's Brawl lines and brawl-keys.txt), only when this app has none of its own yet. App Hub's files
        /// are only read, never changed. Returns what was brought (for a note), or null.</summary>
        public static string BringAcrossFromAppHub(string appHubDir)
        {
            try
            {
                if (appHubDir == null || !Directory.Exists(appHubDir)) return null;
                string mark = Path.Combine(Dir, "brought-from-app-hub.txt");
                if (File.Exists(mark) || File.Exists(Scores) || File.Exists(Path.Combine(Dir, "brawl-keys.txt"))) return null;
                string brought = "";
                string oldScores = Path.Combine(appHubDir, "scores.txt");
                if (File.Exists(oldScores))
                {
                    var book = new ClassicGames.ScoreBook(oldScores);
                    if (!book.LoadFailed)
                    {
                        var mine = new ClassicGames.ScoreBook(Scores);
                        int n = 0;
                        foreach (string key in new[] { "ladder-0", "ladder-1", "ladder-2", "ladder-3" })
                            foreach (var e in book.Top("Brawl", key, 50).Reverse()) { mine.AddKept(e); n++; }
                        if (n > 0) { mine.Save(); brought += n + " ladder score" + (n == 1 ? "" : "s"); }
                    }
                }
                string oldKeys = Path.Combine(appHubDir, "brawl-keys.txt");
                if (File.Exists(oldKeys))
                {
                    File.Copy(oldKeys, Path.Combine(Dir, "brawl-keys.txt"), false);
                    brought += (brought.Length > 0 ? " and " : "") + "your keys";
                }
                File.WriteAllText(mark, "Brought from App Hub's Classic Games on " + DateTime.Now.ToString("yyyy-MM-dd HH:mm") + ": " + (brought.Length > 0 ? brought : "nothing") + "\r\n", Encoding.UTF8);
                return brought.Length > 0 ? brought : null;
            }
            catch (Exception ex) { ClassicGames.CrashLog.Write("bringing App Hub's saves across", ex, false); return null; }
        }
    }
}
