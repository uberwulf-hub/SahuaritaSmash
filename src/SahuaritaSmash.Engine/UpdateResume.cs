// Classic Games - "carry on where you left off" after an App Hub update. When App Hub closes Classic Games for an
// update (UI\UpdateClose.ps1, after a 30-second notice), the game being played saves where the player is
// (Game.SaveForUpdate) and ClassicGames.ps1 keeps what it returned here: update-resume.txt in the data folder
// (%APPDATA%\ClassicGames; tests and snapshots use %TEMP%). The next start offers "Carry on where you left off",
// which starts that game and hands the values back (Game.ResumeAfterUpdate). A resume is used once (then marked used,
// never deleted), and offered for two weeks.
//
// The file (version 1), UTF-8 text, one value a line:
//   classic-games-resume 1
//   game=Hive            the game's Id
//   player=Kim           who was playing
//   saved=<ISO time>     when
//   used=0               1 once carried on from
//   v.<key>=<value>      what the game returned (values on one line; line breaks become spaces)
// Lines it doesn't know are kept and written back. Written through AppHubSaves.SafeFile (backups, a damaged file is
// copied aside and never written over).
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace ClassicGames
{
    public sealed class ResumeEntry
    {
        public string Game = "", Player = "";
        public DateTime Saved;
        public bool Used;
        public readonly Dictionary<string, string> Values = new Dictionary<string, string>();
        internal readonly List<string> Unknown = new List<string>();
        /// <summary>Not used yet and saved in the last two weeks: offer to carry on.</summary>
        public bool Offer { get { return !Used && Game.Length > 0 && (DateTime.Now - Saved).TotalDays <= UpdateResume.OfferDays; } }
    }

    public static class UpdateResume
    {
        public const string FileName = "update-resume.txt";
        public const string Header = "classic-games-resume 1";
        public const double OfferDays = 14;

        public static string FileIn(string dataDir) { return string.IsNullOrEmpty(dataDir) ? null : Path.Combine(dataDir, FileName); }

        static string OneLine(string s) { return (s ?? "").Replace("\r", " ").Replace("\n", " "); }

        public static string ToText(ResumeEntry e)
        {
            var sb = new StringBuilder();
            sb.Append(Header).Append("\r\n");
            sb.Append("game=").Append(OneLine(e.Game)).Append("\r\n");
            sb.Append("player=").Append(OneLine(e.Player)).Append("\r\n");
            sb.Append("saved=").Append(e.Saved.ToString("o", CultureInfo.InvariantCulture)).Append("\r\n");
            sb.Append("used=").Append(e.Used ? "1" : "0").Append("\r\n");
            foreach (KeyValuePair<string, string> kv in e.Values)
            {
                string k = OneLine(kv.Key).Replace("=", "_");
                if (k.Length == 0) continue;
                sb.Append("v.").Append(k).Append('=').Append(OneLine(kv.Value)).Append("\r\n");
            }
            foreach (string u in e.Unknown) sb.Append(u).Append("\r\n");
            return sb.ToString();
        }

        /// <summary>Reads the text of a resume file; throws InvalidDataException when it isn't one.</summary>
        public static ResumeEntry Parse(string text)
        {
            if (text != null && text.Length > 0 && text[0] == (char)0xFEFF) text = text.Substring(1);
            string[] lines = (text ?? "").Replace("\r\n", "\n").Split('\n');
            if (lines.Length == 0 || !lines[0].Trim().StartsWith("classic-games-resume ", StringComparison.Ordinal)) throw new InvalidDataException("not a resume file");
            var e = new ResumeEntry();
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i];
                if (line.Trim().Length == 0) continue;
                int eq = line.IndexOf('=');
                string k = eq > 0 ? line.Substring(0, eq) : "", v = eq > 0 ? line.Substring(eq + 1) : "";
                switch (k)
                {
                    case "game": e.Game = v.Trim(); break;
                    case "player": e.Player = v; break;
                    case "saved": { DateTime d; if (DateTime.TryParse(v, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out d)) e.Saved = d.ToLocalTime(); break; }
                    case "used": e.Used = v.Trim() == "1"; break;
                    default:
                        if (k.StartsWith("v.", StringComparison.Ordinal) && k.Length > 2) e.Values[k.Substring(2)] = v;
                        else e.Unknown.Add(line);
                        break;
                }
            }
            return e;
        }

        /// <summary>Keeps what the game returned (replacing an older resume, whose earlier versions stay as backups).</summary>
        public static void Write(string dataDir, string game, string player, IDictionary<string, string> values)
        {
            string f = FileIn(dataDir);
            if (f == null) throw new IOException("Classic Games has no folder to save in");
            var e = new ResumeEntry { Game = game ?? "", Player = player ?? "", Saved = DateTime.Now };
            if (values != null) foreach (KeyValuePair<string, string> kv in values) e.Values[kv.Key] = kv.Value;
            Directory.CreateDirectory(dataDir);
            AppHubSaves.SafeFile.WriteText(f, ToText(e));
        }

        /// <summary>The saved resume (used or not), or null when there is none or it couldn't be read (then it is copied
        /// aside by SafeFile and never written over; note says so).</summary>
        public static ResumeEntry Read(string dataDir, out string note)
        {
            note = "";
            string f = FileIn(dataDir);
            try
            {
                if (f == null || (!File.Exists(f) && AppHubSaves.SafeFile.Backups(f).Count == 0)) return null;
                AppHubSaves.SaveLoad info;
                ResumeEntry e = AppHubSaves.SafeFile.Load(f, b => Parse(new UTF8Encoding(false).GetString(b)), out info);
                note = info.Message ?? "";
                return e;
            }
            catch (AppHubSaves.SaveUnreadableException) { note = "The game saved before the update couldn't be opened. It is kept, and nothing was written over it."; return null; }
            catch (Exception) { return null; }
        }
        public static ResumeEntry Read(string dataDir) { string note; return Read(dataDir, out note); }

        /// <summary>The resume has been carried on from: marked used (kept, never deleted).</summary>
        public static void MarkUsed(string dataDir)
        {
            string f = FileIn(dataDir);
            if (f == null) return;
            ResumeEntry e = Read(dataDir);
            if (e == null || e.Used) return;
            e.Used = true;
            try { AppHubSaves.SafeFile.WriteText(f, ToText(e)); } catch (Exception) { }
        }

        // ------------------------------------------------------------------ helpers for the games' values
        public static string Num(double v) { return v.ToString("R", CultureInfo.InvariantCulture); }
        public static string Get(IDictionary<string, string> d, string k) { string v; return d != null && d.TryGetValue(k, out v) ? v ?? "" : ""; }
        public static int Int(IDictionary<string, string> d, string k, int def)
        {
            int v; return int.TryParse(Get(d, k).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out v) ? v : def;
        }
        public static long Long(IDictionary<string, string> d, string k, long def)
        {
            long v; return long.TryParse(Get(d, k).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out v) ? v : def;
        }
        public static double Dbl(IDictionary<string, string> d, string k, double def)
        {
            double v; return double.TryParse(Get(d, k).Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out v) ? v : def;
        }
        /// <summary>Numbers separated by commas (missing or bad ones are def).</summary>
        public static int[] Ints(string s, int count, int def)
        {
            var r = new int[count];
            string[] p = (s ?? "").Split(',');
            for (int i = 0; i < count; i++) { int v; r[i] = i < p.Length && int.TryParse(p[i].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out v) ? v : def; }
            return r;
        }
        public static string Join(int[] a)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < a.Length; i++) { if (i > 0) sb.Append(','); sb.Append(a[i].ToString(CultureInfo.InvariantCulture)); }
            return sb.ToString();
        }
    }
}
