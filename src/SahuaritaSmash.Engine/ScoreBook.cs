// High scores (App Hub's Classic Games, Engine\Core.cs: ScoreEntry and ScoreBook, unchanged): one tab-separated
// line per score, saved through App Hub's safe-save helper (SafeSave.cs). Sahuarita Smash! keeps its ladder scores here.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace ClassicGames
{
    // ====================================================================== scores
    public sealed class ScoreEntry
    {
        public string Game { get; set; }
        public string Key { get; set; }
        public string Player { get; set; }
        public long Score { get; set; }
        public string Detail { get; set; }
        public DateTime When { get; set; }
    }

    /// <summary>High scores for every game and player, in a tab-separated text file (one score per line).</summary>
    public sealed class ScoreBook
    {
        public string Path { get; private set; }
        readonly List<ScoreEntry> list = new List<ScoreEntry>();

        /// <summary>Lines this version can't read (from a newer version, say): kept and written back as they were.</summary>
        readonly List<string> unknown = new List<string>();
        /// <summary>What to tell the person when the file couldn't be opened ("" when it opened fine).</summary>
        public string LoadMessage { get; private set; }
        /// <summary>The scores couldn't be read at all (nor any backup): the file is kept, copied aside, and new scores
        /// are still saved (the old file is a backup and its copy stays).</summary>
        public bool LoadFailed { get; private set; }

        public ScoreBook(string path)
        {
            Path = path;
            LoadMessage = "";
            if (path == null || (!File.Exists(path) && AppHubSaves.SafeFile.Backups(path).Count == 0)) return;
            try
            {
                AppHubSaves.SaveLoad info;
                var got = AppHubSaves.SafeFile.Load(path, Parse, out info);
                list.AddRange(got.Item1); unknown.AddRange(got.Item2);
                LoadMessage = info.Message;
            }
            catch (Exception ex)
            {
                LoadFailed = true;
                var u = ex as AppHubSaves.SaveUnreadableException;
                LoadMessage = "The high scores couldn't be opened (" + (u != null ? u.Reason : ex.Message) + "). They are kept" + (u != null && u.Quarantined.Length > 0 ? " as " + System.IO.Path.GetFileName(u.Quarantined) : "") + ", and nothing was written over them.";
            }
        }

        /// <summary>One score per line: game, key, player, score, detail, ticks (tab-separated; the same since 2026-10-14).
        /// Lines it can't read are kept; a file with text but no line it can read at all isn't a score book.</summary>
        static Tuple<List<ScoreEntry>, List<string>> Parse(byte[] data)
        {
            string text = new UTF8Encoding(false).GetString(data);
            if (text.Length > 0 && text[0] == (char)0xFEFF) text = text.Substring(1);
            var got = new List<ScoreEntry>(); var other = new List<string>();
            foreach (string raw in text.Split('\n'))
            {
                string line = raw.TrimEnd('\r');
                if (line.Trim().Length == 0) continue;
                string[] p = line.Split('\t');
                long sc, ticks;
                if (p.Length < 6 || !long.TryParse(p[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out sc)) { other.Add(line); continue; }
                long.TryParse(p[5], NumberStyles.Integer, CultureInfo.InvariantCulture, out ticks);
                got.Add(new ScoreEntry { Game = p[0], Key = p[1], Player = p[2], Score = sc, Detail = p[4], When = new DateTime(Math.Max(0, Math.Min(ticks, DateTime.MaxValue.Ticks))) });
            }
            if (got.Count == 0 && other.Count > 0) throw new InvalidDataException("not a score book");
            return Tuple.Create(got, other);
        }

        static string Clean(string s) { return (s ?? "").Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ').Trim(); }

        public IList<ScoreEntry> Top(string game, string key, int n)
        {
            return list.Where(e => e.Game == game && e.Key == (key ?? "")).OrderByDescending(e => e.Score).ThenBy(e => e.When).Take(n).ToList();
        }
        public IList<ScoreEntry> TopFor(string game, string key, string player, int n)
        {
            return list.Where(e => e.Game == game && e.Key == (key ?? "") && string.Equals(e.Player, player, StringComparison.OrdinalIgnoreCase)).OrderByDescending(e => e.Score).Take(n).ToList();
        }
        public long Best(string game, string key, string player)
        {
            long b = 0;
            foreach (ScoreEntry e in list) if (e.Game == game && e.Key == (key ?? "") && (player == null || string.Equals(e.Player, player, StringComparison.OrdinalIgnoreCase))) b = Math.Max(b, e.Score);
            return b;
        }
        public ScoreEntry BestEntry(string game, string key)
        {
            return list.Where(e => e.Game == game && e.Key == (key ?? "")).OrderByDescending(e => e.Score).FirstOrDefault();
        }
        /// <summary>Best score of a player in a game over all its keys (levels, modes).</summary>
        public long BestAnyKey(string game, string player)
        {
            long b = 0;
            foreach (ScoreEntry e in list) if (e.Game == game && (player == null || string.Equals(e.Player, player, StringComparison.OrdinalIgnoreCase))) b = Math.Max(b, e.Score);
            return b;
        }

        public ScoreEntry Add(string game, string key, string player, long score, string detail)
        {
            var e = new ScoreEntry { Game = Clean(game), Key = Clean(key), Player = Clean(player), Score = score, Detail = Clean(detail), When = DateTime.Now };
            if (e.Player.Length == 0) e.Player = "Player";
            list.Add(e);
            // keep the best 50 of each game / key
            var group = list.Where(x => x.Game == e.Game && x.Key == e.Key).OrderByDescending(x => x.Score).ThenBy(x => x.When).Skip(50).ToList();
            foreach (ScoreEntry x in group) list.Remove(x);
            Save();
            return e;
        }

        /// <summary>Adds a score as it was (its date kept; bringing App Hub's scores across). Call Save afterwards.</summary>
        public void AddKept(ScoreEntry e)
        {
            list.Add(new ScoreEntry { Game = Clean(e.Game), Key = Clean(e.Key), Player = Clean(e.Player), Score = e.Score, Detail = Clean(e.Detail), When = e.When });
        }

        public void Save()
        {
            if (Path == null) return;
            try
            {
                var sb = new StringBuilder();
                foreach (ScoreEntry e in list)
                    sb.Append(e.Game).Append('\t').Append(e.Key).Append('\t').Append(e.Player).Append('\t').Append(e.Score.ToString(CultureInfo.InvariantCulture))
                      .Append('\t').Append(e.Detail).Append('\t').Append(e.When.Ticks.ToString(CultureInfo.InvariantCulture)).Append("\r\n");
                foreach (string u in unknown) sb.Append(u).Append("\r\n");
                // safely, with the five versions before kept (a file that couldn't be read is copied aside first)
                AppHubSaves.SafeFile.WriteText(Path, sb.ToString());
            }
            catch (Exception) { }
        }
    }
}
