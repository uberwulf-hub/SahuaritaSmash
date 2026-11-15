// Saves (App Hub's SaveTests.cs and UpdateResumeTests.cs, Sahuarita Smash's parts): the keys fixture from 2026-10-29,
// a match and the ladder kept and carried on, and (new here) bringing App Hub's ladder scores and keys across once.
// Plus the snapshot scenes (App Hub's -Show names), drawn every run and saved with -Snapshot <dir>.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Input;
using SahuaritaSmash.Services;

namespace ClassicGames
{
    public static partial class Tests
    {
        static string FixtureDir { get { return Path.Combine(AppContext.BaseDirectory, "Fixtures"); } }
        static string SaveTemp(string name)
        {
            string d = Path.Combine(Path.GetTempPath(), "SahuaritaSmash-test", name + "-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(d);
            return d;
        }

        public static void BrawlSaveChecks()
        {
            string set = SaveTemp("settings");
            try
            {
                var sb = new ScoreBook(Path.Combine(set, "scores.txt"));
                // Sahuarita Smash's keys (version 1, 2026-10-29), as App Hub's SaveTests.cs
                File.Copy(Path.Combine(FixtureDir, "brawl-keys.txt"), Path.Combine(set, "brawl-keys.txt"));
                var bg = new BrawlGame { Player = "Kim", Scores = sb };
                bg.LoadKeys();
                Check("saves fixture: Sahuarita Smash's keys from 2026-10-29 open", bg.Keys.Set[0][BrawlKeys.Attack] == Key.J && bg.Keys.Set[1][BrawlKeys.Attack] == Key.K, bg.Keys.Fields());
                bg.Player = "Ben"; bg.LoadKeys(); bg.Keys.Assign(0, BrawlKeys.Grab, Key.Y); bg.SaveKeys();
                string[] kl = File.ReadAllLines(Path.Combine(set, "brawl-keys.txt"));
                string ben = kl.FirstOrDefault(l => l.StartsWith("Ben\t")) ?? "";
                Check("saves fixture: saving one player's Sahuarita Smash! keys keeps the others' and the fields a newer version added", kl.Any(l => l.StartsWith("Kim\t") && l.Contains(",J,")) && ben.Contains("NumPad1") && ben.Contains(",Y\t") && ben.EndsWith("\tsomething-newer") && File.Exists(Path.Combine(set, "brawl-keys.txt.bak1")), string.Join(" | ", kl));
            }
            finally { try { Directory.Delete(set, true); } catch (Exception) { } }

            string dir = SaveTemp("update");
            try { BrawlUpdateChecks(dir); }
            finally { try { Directory.Delete(dir, true); } catch (Exception) { } }

            // App Hub's Classic Games folder: its Brawl ladder scores and keys come across once; other games' scores don't
            string hub = SaveTemp("apphub"), mine = SaveTemp("mine");
            try
            {
                var old = new ScoreBook(Path.Combine(hub, "scores.txt"));
                old.Add("Brawl", "ladder-1", "Kim", 5200, "climbed the ladder");
                old.Add("Brawl", "ladder-2", "Ben", 7100, "climbed the ladder");
                old.Add("Hive", "level-1", "Kim", 999, "not this game");
                File.Copy(Path.Combine(FixtureDir, "brawl-keys.txt"), Path.Combine(hub, "brawl-keys.txt"));
                byte[] before = File.ReadAllBytes(Path.Combine(hub, "scores.txt"));
                Data.UseFolder(mine);
                string got = Data.BringAcrossFromAppHub(hub);
                var now = new ScoreBook(Data.Scores);
                Check("saves: App Hub's ladder scores and keys come across (and only this game's scores)",
                    got != null && now.Best("Brawl", "ladder-1", "Kim") == 5200 && now.Best("Brawl", "ladder-2", "Ben") == 7100 && now.Best("Hive", "level-1", null) == 0 && File.Exists(Path.Combine(mine, "brawl-keys.txt")), got ?? "null");
                Check("saves: App Hub's files are only read", before.SequenceEqual(File.ReadAllBytes(Path.Combine(hub, "scores.txt"))), "");
                now.Add("Brawl", "ladder-1", "Kim", 100, "a new score");
                Check("saves: it happens once (a second start brings nothing over our own)", Data.BringAcrossFromAppHub(hub) == null && new ScoreBook(Data.Scores).Top("Brawl", "ladder-1", 50).Count == 2, "");
            }
            finally { Data.UseTestFolder(); try { Directory.Delete(hub, true); Directory.Delete(mine, true); } catch (Exception) { } }
        }

        /// <summary>App Hub's snapshot scenes (-Show), drawn at 1280 x 720: each must not be blank; -Snapshot saves them.</summary>
        public static void BrawlSnapshotChecks()
        {
            var scenes = new List<Tuple<string, int>>();
            foreach (string s in new[] { "Title", "Select", "Help", "Ladder", "Keys", "Lineup", "Countdown", "Pause", "Results", "Ladder1", "Giant" }) scenes.Add(Tuple.Create(s, 1));
            for (int lv = 1; lv <= 5; lv++) scenes.Add(Tuple.Create("Fight", lv));
            scenes.Add(Tuple.Create("Fight2", 3)); scenes.Add(Tuple.Create("Cast", 2));
            scenes.Add(Tuple.Create("Moves", 1)); scenes.Add(Tuple.Create("Swings", 6));
            scenes.Add(Tuple.Create("Stampede", 1)); scenes.Add(Tuple.Create("Stampede", 2));
            foreach (var sc in scenes)
            {
                string name = sc.Item1 + (sc.Item1 == "Fight" || sc.Item1 == "Moves" || sc.Item1 == "Swings" || sc.Item1 == "Stampede" || sc.Item1 == "Fight2" || sc.Item1 == "Cast" ? "-" + sc.Item2 : "");
                string what = null;
                try
                {
                    var g = new BrawlGame { Scores = new ScoreBook(Path.Combine(Data.Dir, "snap-scores.txt")), Player = "Kim" };
                    g.KeysFileOverride = Path.Combine(Data.Dir, "snap-keys.txt");
                    what = g.SnapshotScene(sc.Item1, 0, sc.Item2);
                    using (var dv = new DrawingVisual((int)g.Width, (int)g.Height))
                    {
                        using (DrawingContext dc = dv.RenderOpen()) SahuaritaSmash.Views.GameView.RenderGame(g, dc);
                        int colours = Colours(dv.Bitmap);
                        Check("brawl snapshot " + name + " is drawn (not blank)", colours > 40, colours + " colours; " + what);
                        Save(dv.Bitmap, name);
                    }
                }
                catch (Exception ex) { Check("brawl snapshot " + name + " draws", false, ex.GetType().Name + ": " + ex.Message + " " + ex.StackTrace?.Split('\n').FirstOrDefault()); }
            }
        }

        static int Colours(Avalonia.Media.Imaging.RenderTargetBitmap bmp)
        {
            using (var ms = new MemoryStream())
            {
                bmp.Save(ms);
                ms.Position = 0;
                var wb = Avalonia.Media.Imaging.WriteableBitmap.Decode(ms);
                using (var fb = wb.Lock())
                {
                    var seen = new HashSet<int>();
                    var row = new int[fb.Size.Width];
                    for (int y = 0; y < fb.Size.Height; y += 4)
                    {
                        System.Runtime.InteropServices.Marshal.Copy(fb.Address + y * fb.RowBytes, row, 0, row.Length);
                        for (int x = 0; x < row.Length; x += 4) seen.Add(row[x]);
                    }
                    return seen.Count;
                }
            }
        }

        static void Save(Avalonia.Media.Imaging.RenderTargetBitmap bmp, string name)
        {
            if (SnapshotDir == null) return;
            Directory.CreateDirectory(SnapshotDir);
            bmp.Save(Path.Combine(SnapshotDir, name + ".png"));
        }
    }
}
