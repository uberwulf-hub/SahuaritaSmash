// The touch controls' settings, per device (Android, a Windows touch screen): how big the buttons are, which hand holds
// the stick, whether the phone buzzes on hits, and whether the first-time guide has been seen. Kept in touch.txt in the
// app's folder (Data.Dir) through the safe-save helper (backups, never written over when it couldn't be read).
//
// The format (version 1), one key=value a line after the first line:
//   Sahuarita Smash! touch controls
//   version=1
//   size=small|normal|large
//   hand=right|left          (left = the stick on the right, the buttons on the left)
//   vibration=on|off
//   guide=new|done
// Unknown lines are kept and written back (a newer version's settings survive an older app); unknown values fall back
// to the default.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SahuaritaSmash.Services
{
    public sealed class TouchSettings
    {
        public const string FirstLine = "Sahuarita Smash! touch controls";
        public const int Version = 1;
        public static readonly string[] SizeNames = { "Small", "Normal", "Large" };
        /// <summary>The button size's scale for TouchControls.Layout (every size keeps every button 44+ across: TouchChecks).</summary>
        public static readonly double[] SizeScales = { 0.85, 1.0, 1.25 };

        /// <summary>0 Small, 1 Normal, 2 Large.</summary>
        public int Size = 1;
        /// <summary>The stick on the right and the buttons on the left.</summary>
        public bool LeftHanded;
        public bool Vibration = true;
        public bool GuideDone;
        /// <summary>Lines this version doesn't know (kept and written back).</summary>
        public readonly List<string> Extra = new List<string>();
        /// <summary>The file couldn't be read (nor a backup): changes are kept for this run but never written over it.</summary>
        public bool Unreadable;
        /// <summary>A sentence for the person when a backup was used or the file couldn't be read (else null).</summary>
        public string Note;
        public string Path;

        public double Scale { get { return SizeScales[Math.Max(0, Math.Min(2, Size))]; } }

        static TouchSettings current;
        /// <summary>This device's settings (read from Data.Dir the first time).</summary>
        public static TouchSettings Current
        {
            get { if (current == null) current = Load(System.IO.Path.Combine(Data.Dir, "touch.txt")); return current; }
            set { current = value; }
        }

        public static TouchSettings Load(string path)
        {
            var t = new TouchSettings { Path = path };
            if (path == null || (!File.Exists(path) && AppHubSaves.SafeFile.Backups(path).Count == 0)) return t;
            try
            {
                AppHubSaves.SaveLoad info;
                string text = AppHubSaves.SafeFile.LoadText(path, s => s.StartsWith(FirstLine, StringComparison.Ordinal) || s.Contains("version="), out info);
                t.Read(text);
                if (!string.IsNullOrEmpty(info.Message)) t.Note = info.Message;
            }
            catch (FileNotFoundException) { }
            catch (Exception ex)
            {
                t.Unreadable = true;
                t.Note = "Your touch settings couldn't be read, so the usual ones are used this time (the file is kept).";
                ClassicGames.CrashLog.Write("reading the touch settings", ex, false);
            }
            return t;
        }

        /// <summary>Reads the text of a settings file (any version).</summary>
        public void Read(string text)
        {
            foreach (string raw in text.Replace("\r\n", "\n").Split('\n'))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line == FirstLine) continue;
                int eq = line.IndexOf('=');
                string key = eq > 0 ? line.Substring(0, eq).Trim().ToLowerInvariant() : "";
                string v = eq > 0 ? line.Substring(eq + 1).Trim().ToLowerInvariant() : "";
                switch (key)
                {
                    case "version": break;
                    case "size": Size = v == "small" ? 0 : v == "large" ? 2 : 1; break;
                    case "hand": LeftHanded = v == "left"; break;
                    case "vibration": Vibration = v != "off"; break;
                    case "guide": GuideDone = v == "done"; break;
                    default: Extra.Add(line); break;
                }
            }
        }

        public string Text()
        {
            var sb = new StringBuilder();
            sb.Append(FirstLine).Append("\r\n");
            sb.Append("version=").Append(Version).Append("\r\n");
            sb.Append("size=").Append(SizeNames[Math.Max(0, Math.Min(2, Size))].ToLowerInvariant()).Append("\r\n");
            sb.Append("hand=").Append(LeftHanded ? "left" : "right").Append("\r\n");
            sb.Append("vibration=").Append(Vibration ? "on" : "off").Append("\r\n");
            sb.Append("guide=").Append(GuideDone ? "done" : "new").Append("\r\n");
            foreach (string l in Extra) sb.Append(l).Append("\r\n");
            return sb.ToString();
        }

        /// <summary>Writes the settings (never over a file that couldn't be read). False when not saved.</summary>
        public bool Save()
        {
            if (Path == null || Unreadable) return false;
            try { AppHubSaves.SafeFile.WriteText(Path, Text()); return true; }
            catch (Exception ex) { ClassicGames.CrashLog.Write("saving the touch settings", ex, false); return false; }
        }
    }
}
