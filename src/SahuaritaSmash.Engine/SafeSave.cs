// App Hub - keeping people's saved data safe (shared by the apps: the original is Core\SafeSave.cs, and
// tools\Sync-SharedUi.ps1 copies it into every app's Engine folder that has a copy; keep the copies identical).
//
// The rules (CLAUDE.md, "Saved data"):
//   - a save is written to a temporary file next to it, flushed to the disk, and only then put in place, so a crash
//     or a full disk never leaves half a file;
//   - the versions before it are kept as <file>.bak1 (the newest) .. <file>.bak5; older ones are pruned, and a backup
//     older than 30 days goes only while newer ones exist (an older App Hub's single <file>.bak is left alone);
//   - a file that can't be read is never overwritten or deleted: it is first copied to
//     <file>.unreadable-yyyyMMdd-HHmmss (Quarantine), then the backups are tried, newest first, and the person is told.
// The class is internal, so every app's engine has its own copy without clashing with another app's.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace AppHubSaves
{
    /// <summary>What happened when a save was opened: which file the data came from and anything the person should be told.</summary>
    internal sealed class SaveLoad
    {
        /// <summary>The file the data was read from (the save itself or one of its backups).</summary>
        public string From = "";
        /// <summary>True when the save itself couldn't be read and a backup was used.</summary>
        public bool UsedBackup;
        /// <summary>Where the unreadable save was copied (empty when it read fine).</summary>
        public string Quarantined = "";
        /// <summary>Why the save itself couldn't be read (empty when it read fine).</summary>
        public string Error = "";
        /// <summary>A sentence for the person, empty when there is nothing to say.</summary>
        public string Message = "";
    }

    /// <summary>A save that couldn't be read, nor any of its backups. The file itself is kept (and copied to Quarantined).</summary>
    internal sealed class SaveUnreadableException : IOException
    {
        public readonly string File, Quarantined, Reason;
        public SaveUnreadableException(string file, string quarantined, string reason)
            : base("Couldn't open " + Path.GetFileName(file) + " (it may be from a different version): " + reason)
        { File = file; Quarantined = quarantined; Reason = reason; }
    }

    internal static class SafeFile
    {
        /// <summary>How many earlier versions are kept (.bak1 .. .bak5).</summary>
        public const int Keep = 5;
        /// <summary>Backups older than this go, as long as a newer backup is kept.</summary>
        public static readonly TimeSpan MaxAge = TimeSpan.FromDays(30);
        public const string QuarantineMark = ".unreadable-";

        static readonly object gate = new object();
        static readonly HashSet<string> unreadable = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public static string BackupName(string file, int n) { return file + ".bak" + n.ToString(CultureInfo.InvariantCulture); }

        // ------------------------------------------------------------------ writing
        public static void WriteText(string file, string text) { Write(file, new UTF8Encoding(false).GetBytes(text ?? ""), true); }
        public static void WriteText(string file, string text, bool backups) { Write(file, new UTF8Encoding(false).GetBytes(text ?? ""), backups); }
        public static void Write(string file, byte[] data) { Write(file, data, true); }

        /// <summary>Writes the file safely: a temporary file flushed to the disk, the current version moved to .bak1 (the
        /// older ones shifted along to .bak5), then the new one put in place. backups=false (caches, window positions)
        /// keeps no backups but is still written safely.</summary>
        public static void Write(string file, byte[] data, bool backups)
        {
            if (string.IsNullOrEmpty(file)) throw new ArgumentException("No file name");
            file = Path.GetFullPath(file);
            string dir = Path.GetDirectoryName(file);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            lock (gate)
            {
                // a file that failed to load in this run: copy it aside first (Quarantine never copies the same bytes twice)
                if (unreadable.Contains(file) && File.Exists(file)) { Quarantine(file); unreadable.Remove(file); }
                string tmp = file + ".tmp";
                using (var fs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None, 65536, FileOptions.WriteThrough))
                {
                    fs.Write(data, 0, data.Length);
                    fs.Flush(true);
                }
                if (!File.Exists(file)) { File.Move(tmp, file); return; }
                if (!backups) { ReplaceInto(tmp, file, null); return; }
                Shift(file);
                ReplaceInto(tmp, file, BackupName(file, 1));
                Prune(file);
            }
        }

        static void ReplaceInto(string tmp, string file, string backup)
        {
            try { File.Replace(tmp, file, backup, true); return; }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            catch (PlatformNotSupportedException) { }
            // File.Replace can't work on some drives (FAT, network shares): copy instead, the old version first
            if (backup != null) File.Copy(file, backup, true);
            File.Copy(tmp, file, true);
            try { File.Delete(tmp); } catch (Exception) { }
        }

        /// <summary>.bak4 -> .bak5, ..., .bak1 -> .bak2 (the oldest beyond Keep goes).</summary>
        static void Shift(string file)
        {
            string last = BackupName(file, Keep);
            if (File.Exists(last)) File.Delete(last);
            for (int n = Keep - 1; n >= 1; n--)
            {
                string a = BackupName(file, n);
                if (File.Exists(a)) File.Move(a, BackupName(file, n + 1));
            }
        }

        /// <summary>Backups older than MaxAge go, oldest first, but .bak1 (the newest) always stays.</summary>
        static void Prune(string file)
        {
            DateTime limit = DateTime.Now - MaxAge;
            for (int n = Keep; n >= 2; n--)
            {
                string b = BackupName(file, n);
                try { if (File.Exists(b) && File.GetLastWriteTime(b) < limit) File.Delete(b); } catch (Exception) { }
            }
        }

        // ------------------------------------------------------------------ backups and quarantine
        /// <summary>The backups of a file, newest first: .bak1 .. .bak5, then an older App Hub's single .bak.</summary>
        public static List<string> Backups(string file)
        {
            var list = new List<string>();
            for (int n = 1; n <= Keep; n++) { string b = BackupName(file, n); if (File.Exists(b)) list.Add(b); }
            if (File.Exists(file + ".bak")) list.Add(file + ".bak");
            return list;
        }

        /// <summary>Copies a file that couldn't be read to file.unreadable-yyyyMMdd-HHmmss (once for the same bytes) and
        /// returns that name. The file itself is left where it is.</summary>
        public static string Quarantine(string file)
        {
            if (!File.Exists(file)) return "";
            lock (gate)
            {
                byte[] data;
                try { data = File.ReadAllBytes(file); } catch (Exception) { data = null; }
                string dir = Path.GetDirectoryName(Path.GetFullPath(file)), name = Path.GetFileName(file);
                if (data != null)
                    foreach (string q in Directory.GetFiles(dir, name + QuarantineMark + "*"))
                    {
                        try { if (new FileInfo(q).Length == data.Length && File.ReadAllBytes(q).SequenceEqual(data)) return q; } catch (Exception) { }
                    }
                string target = file + QuarantineMark + DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
                for (int k = 2; File.Exists(target); k++) target = file + QuarantineMark + DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + "-" + k;
                File.Copy(file, target, false);
                return target;
            }
        }

        /// <summary>The copies Quarantine made of this file, newest first.</summary>
        public static List<string> Quarantined(string file)
        {
            string dir = Path.GetDirectoryName(Path.GetFullPath(file));
            if (!Directory.Exists(dir)) return new List<string>();
            return Directory.GetFiles(dir, Path.GetFileName(file) + QuarantineMark + "*").OrderByDescending(f => f, StringComparer.Ordinal).ToList();
        }

        /// <summary>Remembers that this file couldn't be read in this run: before anything is written over it, it is
        /// copied aside (Quarantine). Load does this itself.</summary>
        public static void MarkUnreadable(string file) { lock (gate) unreadable.Add(Path.GetFullPath(file)); }
        public static bool IsMarkedUnreadable(string file) { lock (gate) return unreadable.Contains(Path.GetFullPath(file)); }

        /// <summary>A plain description of a backup for a "Restore a backup" list: "Saved 14 Oct 2026, 18:02".</summary>
        public static string Describe(string backup)
        {
            DateTime t = File.GetLastWriteTime(backup);
            return "Saved " + t.ToString("d MMM yyyy, HH:mm", CultureInfo.InvariantCulture);
        }

        /// <summary>Puts a backup back as the save: the current file is kept as a backup first (so restoring can be undone).</summary>
        public static void Restore(string file, string backup)
        {
            byte[] data = File.ReadAllBytes(backup);
            Write(file, data, true);
        }

        // ------------------------------------------------------------------ reading
        /// <summary>Opens a save with parse. If the save can't be read it is copied aside (Quarantine) and the backups
        /// are tried, newest first; if none can be read either, SaveUnreadableException (the file stays where it is).
        /// A missing file throws FileNotFoundException as usual.</summary>
        public static T Load<T>(string file, Func<byte[], T> parse, out SaveLoad info)
        {
            info = new SaveLoad { From = file };
            if (!File.Exists(file))
            {
                // the save itself is gone (a crash between two steps of an older App Hub): the newest backup
                foreach (string b in Backups(file))
                {
                    try { T v = parse(File.ReadAllBytes(b)); info.From = b; info.UsedBackup = true; info.Message = "Your save was missing, so the copy from " + When(b) + " was used."; return v; }
                    catch (Exception) { }
                }
                throw new FileNotFoundException("No save", file);
            }
            try { return parse(File.ReadAllBytes(file)); }
            catch (Exception ex)
            {
                info.Error = ex.Message;
                try { info.Quarantined = Quarantine(file); } catch (Exception) { }
                MarkUnreadable(file);
            }
            foreach (string b in Backups(file))
            {
                try
                {
                    T v = parse(File.ReadAllBytes(b));
                    info.From = b; info.UsedBackup = true;
                    info.Message = "Your save couldn't be opened, so the copy from " + When(b) + " was used. The unreadable file is kept as " + Path.GetFileName(info.Quarantined) + ".";
                    return v;
                }
                catch (Exception) { }
            }
            throw new SaveUnreadableException(file, info.Quarantined, info.Error);
        }

        public static string LoadText(string file, Func<string, bool> check, out SaveLoad info)
        {
            return Load(file, b => { string s = new UTF8Encoding(false).GetString(b); if (s.Length > 0 && s[0] == (char)0xFEFF) s = s.Substring(1); if (check != null && !check(s)) throw new InvalidDataException("Not a save of this kind"); return s; }, out info);
        }

        static string When(string file)
        {
            return File.GetLastWriteTime(file).ToString("d MMM yyyy, HH:mm", CultureInfo.InvariantCulture);
        }
    }
}
