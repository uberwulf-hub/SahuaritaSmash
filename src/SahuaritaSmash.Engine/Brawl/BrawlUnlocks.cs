// Sahuarita Smash! (Sauce Edition): the colours a player has unlocked, saved next to the scores in brawl-unlocks.txt.
// Climbing the ladder with a fighter (any difficulty) unlocks its second colour; climbing it on Hard unlocks the
// third too. Colours are pictures only (BrawlColours.cs in the app): the match never sees them.
//
// The file (version 1): a version line, then one line per player and fighter, fields split by tabs:
//   version  1
//   Kim      gila   2        (the player, the fighter's id, how many extra colours: 1 = the second, 2 = the third too)
// Lines this version doesn't understand (and fields after the known ones) are kept and written back as they were. A
// file that can't be opened (nor any backup) is never written over: unlocking goes on in memory for this time only.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace ClassicGames
{
    public sealed class BrawlUnlocks
    {
        public const int Version = 1;
        /// <summary>Colours per fighter: the usual one and two to unlock.</summary>
        public const int Colours = 3;

        sealed class Row { public string Player = "", Fighter = ""; public int Level; public string Extra = ""; }

        public readonly string Path;
        readonly List<Row> rows = new List<Row>();
        readonly List<string> unknown = new List<string>();
        int fileVersion = Version;
        /// <summary>The file couldn't be opened (nor a backup): it is kept as it is and never written over.</summary>
        public bool Unreadable { get; private set; }
        /// <summary>What to tell the person about opening the file ("" when it opened fine).</summary>
        public string Message { get; private set; }

        public BrawlUnlocks(string path)
        {
            Path = path; Message = "";
            if (path == null || (!File.Exists(path) && AppHubSaves.SafeFile.Backups(path).Count == 0)) return;
            try
            {
                AppHubSaves.SaveLoad info;
                string text = AppHubSaves.SafeFile.LoadText(path, LooksRight, out info);
                Parse(text);
                Message = info.Message ?? "";
            }
            catch (Exception ex)
            {
                Unreadable = true; rows.Clear(); unknown.Clear();
                var u = ex as AppHubSaves.SaveUnreadableException;
                Message = "The unlocked colours couldn't be opened (" + (u != null ? u.Reason : ex.Message) + "). They are kept, and nothing was written over them.";
            }
        }

        /// <summary>A file of ours: empty, or its first line that isn't a comment is the version line.</summary>
        static bool LooksRight(string text)
        {
            foreach (string raw in text.Split('\n'))
            {
                string l = raw.TrimEnd('\r');
                if (l.Length == 0 || l.StartsWith("#")) continue;
                string[] p = l.Split('\t');
                int v;
                return p.Length >= 2 && p[0] == "version" && int.TryParse(p[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out v) && v >= 1;
            }
            return true;
        }

        void Parse(string text)
        {
            foreach (string raw in text.Split('\n'))
            {
                string l = raw.TrimEnd('\r');
                if (l.Length == 0) continue;
                if (l.StartsWith("#")) continue;   // our own comment line is written again
                string[] p = l.Split('\t');
                int n;
                if (p[0] == "version" && p.Length >= 2 && int.TryParse(p[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out n)) { fileVersion = Math.Max(Version, n); continue; }
                if (p.Length >= 3 && p[0].Length > 0 && p[1].Length > 0 && int.TryParse(p[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out n) && n >= 0)
                {
                    rows.Add(new Row { Player = p[0], Fighter = p[1], Level = Math.Min(Colours - 1, n), Extra = p.Length > 3 ? "\t" + string.Join("\t", p.Skip(3).ToArray()) : "" });
                    continue;
                }
                unknown.Add(l);
            }
        }

        Row Find(string player, string fighter)
        {
            return rows.FirstOrDefault(r => string.Equals(r.Player, player, StringComparison.OrdinalIgnoreCase) && r.Fighter == fighter);
        }

        /// <summary>How many extra colours this player has for this fighter (0, 1 or 2).</summary>
        public int Level(string player, string fighter)
        {
            Row r = Find(player ?? "", fighter ?? "");
            return r == null ? 0 : r.Level;
        }

        /// <summary>Whether colour c (0 = the usual one) can be picked.</summary>
        public bool Has(string player, string fighter, int c) { return c <= 0 || c < Colours && c <= Level(player, fighter); }

        /// <summary>The ladder was climbed with this fighter: level 1 (the second colour) or 2 (on Hard: the third too).
        /// Returns the colours newly unlocked (0 = nothing new) and saves.</summary>
        public int Unlock(string player, string fighter, int level)
        {
            if (string.IsNullOrEmpty(fighter)) return 0;
            player = (player ?? "Player").Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');
            level = Math.Max(0, Math.Min(Colours - 1, level));
            Row r = Find(player, fighter);
            int before = r == null ? 0 : r.Level;
            if (level <= before) return 0;
            if (r == null) { r = new Row { Player = player, Fighter = fighter }; rows.Add(r); }
            r.Level = level;
            Save();
            return level - before;
        }

        public string Text()
        {
            var lines = new List<string> { "# Sahuarita Smash! colours unlocked: player, fighter, extra colours (1 = the second, 2 = the third too)", "version\t" + fileVersion.ToString(CultureInfo.InvariantCulture) };
            foreach (Row r in rows) lines.Add(r.Player + "\t" + r.Fighter + "\t" + r.Level.ToString(CultureInfo.InvariantCulture) + r.Extra);
            lines.AddRange(unknown);
            return string.Join("\r\n", lines.ToArray()) + "\r\n";
        }

        public void Save()
        {
            if (Path == null || Unreadable) return;
            try { AppHubSaves.SafeFile.WriteText(Path, Text()); } catch (Exception) { }
        }
    }
}
