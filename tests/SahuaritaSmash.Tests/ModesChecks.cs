// Practice, colours and the online rematch (groups "practice", "colours", "rematch"): the practice room never ends,
// nobody loses a life and nothing is saved; the dummy does what it is asked; normal matches are exactly as before
// (their checksums from before the practice room existed); colours unlock on the ladder and are saved per player in
// brawl-unlocks.txt (a fixture of version 1, unknown lines kept, an unreadable file never written over); colours are
// pictures only; the colour chips are big enough to tap; a rematch over the pretend network keeps both devices in step
// and a late message from the match before is left out; the results' buttons by touch. Pictures with -Snapshot.
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
        const double PhoneScale = 412.0 / 720;   // a 915 x 412 phone shows the game's 720 rows 412 pixels high

        static BrawlGame ModesGame(string who, string dir)
        {
            var g = new BrawlGame { Player = who, Rng = new Random(who.Length), Scores = new ScoreBook(Path.Combine(dir, "scores.txt")) };
            g.KeysFileOverride = Path.Combine(dir, "keys.txt");
            g.UnlocksFileOverride = Path.Combine(dir, "brawl-unlocks.txt");
            return g;
        }

        static void SnapScene(string scene, int level, string name)
        {
            var g = ModesGame("Kim", Data.Dir);
            g.SnapshotScene(scene, 0, level);
            using (var dv = new DrawingVisual((int)g.Width, (int)g.Height))
            {
                using (DrawingContext dc = dv.RenderOpen()) SahuaritaSmash.Views.GameView.RenderGame(g, dc);
                int colours = Colours(dv.Bitmap);
                Check("snapshot " + name + " is drawn (not blank)", colours > 40, colours + " colours");
                Save(dv.Bitmap, name);
            }
            SnapPhone(g, name + "-phone");
        }

        /// <summary>The game as a 915 x 412 phone shows it (scaled to fit).</summary>
        static void SnapPhone(Game g, string name)
        {
            var phone = new Size(915, 412);
            using (var dv = new DrawingVisual((int)phone.Width, (int)phone.Height))
            {
                using (DrawingContext dc = dv.RenderOpen())
                {
                    double k = Math.Min(phone.Width / g.Width, phone.Height / g.Height), ox = (phone.Width - g.Width * k) / 2, oy = (phone.Height - g.Height * k) / 2;
                    dc.Ctx.FillRectangle(Avalonia.Media.Brushes.Black, new Rect(phone));
                    dc.PushTransform(new MatrixTransform(new Matrix(k, 0, 0, k, ox, oy)));
                    SahuaritaSmash.Views.GameView.RenderGame(g, dc);
                    dc.Pop();
                }
                Save(dv.Bitmap, name);
            }
        }

        static void Frames(BrawlGame g, int n) { for (int i = 0; i < n; i++) { g.Update(1.0 / 60); Draw(g); } }

        // ================================================================ practice
        public static void PracticeChecks()
        {
            Audio.MusicOn = false;
            string dir = SaveTemp("practice");
            try
            {
                // normal matches: exactly as before the practice room (checksums worked out by the code before it existed)
                var before = new[] { Tuple.Create(0, 4000, -1999661409L), Tuple.Create(2, 4000, -3488756101L), Tuple.Create(4, 4000, 11874881441L) };   // computer players at Hard and up (levels 5-9), which 0.4.1 left as they were
                foreach (var b in before)
                {
                    var defs = new List<BFighterDef> { BrawlData.Fighters[0], BrawlData.Fighters[3], BrawlData.Fighters[6], BrawlData.Fighters[9] };
                    var sim = new BrawlSim(BrawlData.Stages[b.Item1], defs, 2, 0, 777u + (uint)b.Item1);
                    var bots = Enumerable.Range(0, 4).Select(i => new BrawlBot(i, new[] { 5, 6, 7, 9 }[i])).ToArray();
                    var ins = new BrInput[4];
                    for (int f = 0; f < 4000 && !sim.Over; f++) { for (int i = 0; i < 4; i++) ins[i] = bots[i].Think(sim); sim.Step(ins); }
                    Check("practice: a normal match on stage " + b.Item1 + " is exactly as before (frame and checksum)", sim.Frame == b.Item2 && sim.Checksum() == b.Item3, sim.Frame + " " + sim.Checksum());
                }

                // the title's Practice opens its screen; Practise! starts a match of two
                var g = ModesGame("Kim", dir);
                g.Begin();
                Frames(g, 2);
                Check("practice: the title has a Practice item", g.HitPoint("Practice (try your moves)") != null, "");
                g.Sel = 2; g.Tap(Key.Enter);
                Check("practice: Practice opens the practice screen", g.Menu == BrawlGame.Screen.Practice, g.Menu.ToString());
                Frames(g, 2);
                double scale = PhoneScale;
                var tall = g.MouseHits.Where(h => h.Label.Length > 0 && h.Label != "Back").Select(h => h.Area.Height * scale).ToList();
                Check("practice: every button on the practice screen is 44+ pixels tall on a phone", tall.Count > 8 && tall.All(t => t >= 44), string.Join(" ", tall.Select(t => t.ToString("0"))));
                g.PracticePick = 0; g.PracticeFoe = 4; g.DummyMode = 0; g.PracticeStage = 0; g.PracticeSurprises = false;
                g.Sel = 6; g.Tap(Key.Enter);
                Check("practice: Practise! starts a practice match of you and the dummy", g.State == Phase.Play && g.Practice && g.Sim != null && g.Sim.Practice && g.Sim.F.Length == 2 && g.Sim.F[1].D == BrawlData.Fighters[4], g.State + " " + (g.Sim == null ? "" : g.Sim.F.Length.ToString()));
                Frames(g, 60 * 3);
                double dx = g.Sim.F[1].X;
                Frames(g, 60 * 2);
                Check("practice: the dummy stands still by default", Math.Abs(g.Sim.F[1].X - dx) < 0.5 && g.Step == BrawlGame.Stage.Fight, dx + " -> " + g.Sim.F[1].X);

                // falling off: straight back, no life lost, never ends
                int stocks = g.Sim.F[0].Stocks;
                g.Sim.F[0].X = g.Sim.Stage.BlastR + 60; g.Sim.F[0].Y = 200;
                Frames(g, 2);
                bool wentOut = g.Sim.F[0].Out;
                int back = 0;
                for (; back < 120 && g.Sim.F[0].Out; back++) Frames(g, 1);
                Check("practice: falling off brings you straight back (about half a second) without losing a life", wentOut && !g.Sim.F[0].Out && back <= 32 && g.Sim.F[0].Stocks == stocks, wentOut + " back in " + back + " frames, lives " + g.Sim.F[0].Stocks);
                for (int k = 0; k < 6; k++) { g.Sim.F[1].X = g.Sim.Stage.BlastL - 60; Frames(g, 40); }
                Check("practice: the dummy falling off again and again: still no winner, no results", !g.Sim.Decided && g.Step == BrawlGame.Stage.Fight && g.Sim.F[1].Stocks == stocks, g.Sim.F[1].Stocks + " " + g.Step);

                // the dummy's other ways
                Func<int, int, Func<BrawlGame, bool>, bool> mode = (m, secs, saw) =>
                {
                    g.DummyMode = m; bool seen = false;
                    for (int i = 0; i < 60 * secs && !seen; i++) { Frames(g, 1); seen = saw(g); }
                    return seen;
                };
                double x0 = g.Sim.F[1].X;
                Check("practice: the dummy walks", mode(1, 4, x => Math.Abs(x.Sim.F[1].X - x0) > 80 && x.Sim.F[1].St == BS.Walk), g.Sim.F[1].X.ToString("0"));
                Check("practice: the dummy jumps", mode(2, 4, x => x.Sim.F[1].Y > x.Sim.Main.Top + 40), g.Sim.F[1].Y.ToString("0"));
                Check("practice: the dummy shields", mode(3, 3, x => x.Sim.F[1].St == BS.Shield), g.Sim.F[1].St.ToString());
                g.Sim.F[0].X = g.Sim.F[1].X - 60;
                Check("practice: the dummy fights back (Easy)", mode(4, 15, x => x.Sim.F[0].Dmg > 0), g.Sim.F[0].Dmg.ToString("0"));

                // the pause menu: damage back to 0%, surprises, what the dummy does, the moves list; its buttons tap
                g.Sim.F[0].Dmg = 55; g.Sim.F[1].Dmg = 80;
                g.Tap(Key.Escape);
                Frames(g, 1);
                var labels = g.MouseHits.Select(h => h.Label).ToList();
                bool big = g.MouseHits.All(h => h.Area.Height * scale >= 44);
                Check("practice: the pause menu has big buttons for the dummy, damage, surprises, the moves list and leaving", g.Paused && big && labels.Any(l => l.StartsWith("Dummy: ")) && labels.Contains("Damage back to 0%") && labels.Any(l => l.StartsWith("Stage surprises")) && labels.Contains("Leave practice"), string.Join(", ", labels));
                TapLabel(g, "Damage back to 0%");
                Check("practice: Damage back to 0% puts everyone's damage back", g.Sim.F[0].Dmg == 0 && g.Sim.F[1].Dmg == 0, g.Sim.F[0].Dmg + " " + g.Sim.F[1].Dmg);
                Frames(g, 1);
                TapLabel(g, "Stage surprises: Off");
                Check("practice: stage surprises can be turned on", g.Sim.Surprises && g.PracticeSurprises, "");
                Frames(g, 1);
                int dm = g.DummyMode;
                TapLabel(g, "Dummy: " + BrawlGame.DummyModes[dm]);
                Check("practice: the dummy button changes what it does", g.DummyMode == (dm + 1) % BrawlGame.DummyModes.Length && g.Paused, g.DummyMode.ToString());
                Frames(g, 1);
                Snap(g, "PracticePause-check");
                TapLabel(g, "Leave practice");
                Check("practice: Leave practice goes back to the title", g.State == Phase.Setup && g.Menu == BrawlGame.Screen.Title && !g.Practice, g.State + " " + g.Menu);
                Check("practice: nothing was saved (no ladder scores, no update file)", g.Scores.Top("Brawl", "ladder-0", 9).Count + g.Scores.Top("Brawl", "ladder-1", 9).Count + g.Scores.Top("Brawl", "ladder-2", 9).Count == 0 && !File.Exists(Path.Combine(dir, "scores.txt")), "");
                var keep = ModesGame("Kim", dir);
                keep.PracticePick = 1; keep.PracticeFoe = 2; keep.Begin(); keep.Menu = BrawlGame.Screen.Practice; keep.Sel = 6; keep.Tap(Key.Enter); Frames(keep, 200);
                Check("practice: a practice match isn't kept for an update", keep.Practice && keep.SaveForUpdate() == null, "");

                // after practice, a normal fight is a normal match again
                var n = ModesGame("Kim", dir);
                n.Begin(); n.Sel = 2; n.Tap(Key.Enter); n.Sel = 6; n.Tap(Key.Enter); Frames(n, 10);
                n.Tap(Key.Escape); n.Tap(Key.Q);
                n.Menu = BrawlGame.Screen.Select; n.Sel = 11; n.Tap(Key.Enter);
                Check("practice: a fight after practice is a normal match (lives count again)", n.State == Phase.Play && !n.Practice && n.Sim != null && !n.Sim.Practice, n.State + " " + n.Practice);
            }
            finally { try { Directory.Delete(dir, true); } catch (Exception) { } }

            foreach (string s in new[] { "Practice", "PracticeFight", "PracticePause" }) SnapScene(s, 1, s);
            DisposeFrames();
        }

        static void TapLabel(Game g, string label)
        {
            HitArea h = g.MouseHits.FirstOrDefault(x => x.Label == label);
            if (h == null) { Check("tap: a button called " + label + " is there", false, string.Join(", ", g.MouseHits.Select(x => x.Label))); return; }
            var c = new Point(h.Area.X + h.Area.Width / 2, h.Area.Y + h.Area.Height / 2);
            g.PointerMove(c); g.PointerDown(c, false); g.PointerUp(c, false);
        }

        static void DisposeFrames()
        {
            foreach (DrawingVisual v in frameDv.Values) v.Dispose();
            frameDv.Clear();
        }

        // ================================================================ colours
        public static void ColourChecks()
        {
            Audio.MusicOn = false;
            string dir = SaveTemp("colours");
            try
            {
                // the file: version 1 (the fixture), unknown lines and fields kept, a backup made
                string f = Path.Combine(dir, "brawl-unlocks.txt");
                File.Copy(Path.Combine(FixtureDir, "brawl-unlocks.txt"), f);
                var u = new BrawlUnlocks(f);
                Check("colours fixture: brawl-unlocks.txt (version 1) opens", !u.Unreadable && u.Level("Kim", "bloom") == 2 && u.Level("kim", "gila") == 1 && u.Level("Ben", "sam") == 1 && u.Level("Ben", "bloom") == 0 && u.Has("Kim", "gila", 1) && !u.Has("Kim", "gila", 2), u.Message);
                int got = u.Unlock("Ben", "bloom", 1), again = u.Unlock("Ben", "bloom", 1), more = u.Unlock("Kim", "gila", 2);
                string text = File.ReadAllText(f);
                Check("colours: unlocking saves; the same again unlocks nothing new; Hard after Normal adds the third",
                    got == 1 && again == 0 && more == 1 && new BrawlUnlocks(f).Level("Ben", "bloom") == 1 && new BrawlUnlocks(f).Level("Kim", "gila") == 2, got + " " + again + " " + more);
                Check("colours: saving keeps the lines and fields a newer version wrote, and a backup", text.Contains("something a newer version wrote") && text.Contains("\tfrom-a-newer-version") && text.Contains("version\t1") && File.Exists(f + ".bak1"), text);

                // a file that can't be read is never written over
                string bad = Path.Combine(dir, "bad", "brawl-unlocks.txt");
                Directory.CreateDirectory(Path.GetDirectoryName(bad));
                File.WriteAllText(bad, "\u0001\u0002 this is not a list of colours\n");
                byte[] was = File.ReadAllBytes(bad);
                var ub = new BrawlUnlocks(bad);
                ub.Unlock("Kim", "bloom", 2);
                Check("colours: an unreadable file is kept as it is and never written over (unlocking goes on in memory)", ub.Unreadable && ub.Message.Length > 0 && was.SequenceEqual(File.ReadAllBytes(bad)) && ub.Level("Kim", "bloom") == 2, ub.Message);
                Check("colours: no file yet is fine (nothing unlocked)", new BrawlUnlocks(Path.Combine(dir, "none.txt")).Level("Kim", "bloom") == 0 && !new BrawlUnlocks(Path.Combine(dir, "none.txt")).Unreadable, "");

                // the palettes: three different looks per fighter, a fighter added later turned round the colour wheel
                bool differ = true; var unlisted = new List<string>();
                foreach (BFighterDef d in BrawlData.Fighters)
                {
                    var mains = Enumerable.Range(0, BrawlUnlocks.Colours).Select(c => BrawlColours.Def(d, c)).ToList();
                    if (mains.Select(x => x.Main).Distinct().Count() != 3 || mains.Select(x => x.Second).Distinct().Count() < 2) differ = false;
                    if (!BrawlColours.Listed(d.Id)) unlisted.Add(d.Id);
                    if (mains[1].Moves.Count != d.Moves.Count || mains[1].Name != d.Name || mains[1].Weight != d.Weight || mains[1].SpecialNames != d.SpecialNames) differ = false;
                }
                var later = new BFighterDef { Id = "someone-new", Name = "Someone new", Main = 0xFF2563EB, Second = 0xFFF59E0B, Trim = 0xFFFFFFFF, Dark = 0xFF1E3A8A };
                Check("colours: every fighter has three different colours (the same fighter otherwise); one added later still gets three", differ && BrawlColours.Def(later, 1).Main != later.Main && BrawlColours.Def(later, 2).Main != BrawlColours.Def(later, 1).Main && BrawlColours.Def(later, 0) == later, "not in the table: " + string.Join(",", unlisted));

                // two seats with the same fighter get different colours when they can
                var g = ModesGame("Kim", dir);
                int bloom = BrawlData.FighterIndex("bloom"), sam = BrawlData.FighterIndex("sam"), gila = BrawlData.FighterIndex("gila");
                int[] two = g.MatchColours(new[] { bloom, bloom, bloom, sam }, new[] { 0, 0, 2, 2 });
                int[] none = g.MatchColours(new[] { sam, sam }, new[] { 0, 2 });
                Check("colours: the same fighter twice gets different colours when it can (Kim has Bloom's three, not Saguaro Sam's)", two[0] == 0 && two[1] == 1 && two[2] == 2 && two[3] == 0 && none[0] == 0 && none[1] == 0, string.Join(",", two) + " / " + string.Join(",", none));

                // colours are pictures only: the same match drawn in other colours is the same match, and the fighters
                // keep their own definitions
                long[] sums = new long[2];
                for (int run = 0; run < 2; run++)
                {
                    var m = ModesGame("Kim", dir);
                    m.Rng = new Random(9); m.Autopilot = true; m.DemoAll = true; m.StagePick = 1;
                    for (int s = 0; s < 4; s++) { m.Who[s] = 2; m.Pick[s] = bloom; m.Colour[s] = run == 0 ? 0 : s % 3; }
                    m.NewGame();
                    for (int i = 0; i < 600; i++) { m.Update(1.0 / 60); Draw(m); }
                    sums[run] = m.Sim.Checksum();
                    if (run == 1) Check("colours: four Blooms in a match wear three different colours, and their fighters stay Bloom's own", m.View.Colours.Take(4).Distinct().Count() == 3 && m.Sim.F.All(x => x.D == BrawlData.Fighters[bloom]), string.Join(",", m.View.Colours.Take(4)));
                }
                Check("colours: colours change nothing in the match (same checksum)", sums[0] == sums[1], sums[0] + " " + sums[1]);

                // the fight screen's chips: big enough to tap; a locked one says how to get it; C changes the colour
                var s2 = ModesGame("Kim", dir);
                s2.Begin(); s2.Menu = BrawlGame.Screen.Select; s2.Who[0] = 1; s2.Pick[0] = gila; s2.Sel = 0;
                Frames(s2, 2);
                var chips = s2.MouseHits.Where(h => h.Label.StartsWith("Colour ")).ToList();
                Check("colours: the fight screen shows three colour chips, each 44+ pixels on a phone", chips.Count == 3 && chips.All(h => h.Area.Height * PhoneScale >= 44 && h.Area.Width * PhoneScale >= 44), string.Join(", ", chips.Select(h => h.Label + " " + (h.Area.Height * PhoneScale).ToString("0"))));
                TapLabel(s2, "Colour 3");
                Check("colours: tapping an open chip chooses that colour (Kim's Gila Gus, third colour)", s2.Colour[0] == 2, s2.Colour[0].ToString());
                var s3 = ModesGame("Ben", dir);   // Ben: only Bloom's second colour
                s3.Begin(); s3.Menu = BrawlGame.Screen.Select; s3.Who[0] = 1; s3.Pick[0] = bloom; s3.Sel = 0;
                Frames(s3, 2);
                TapLabel(s3, "Colour 3 (locked)");
                bool lockedStays = s3.Colour[0] == 0;
                Frames(s3, 1);
                TapLabel(s3, "Colour 2");
                bool picked = s3.Colour[0] == 1;
                s3.Tap(Key.C);
                Check("colours: a locked chip can't be chosen, an open one can, and C goes round the open ones", lockedStays && picked && s3.Colour[0] == 0, s3.Colour[0].ToString());

                // the ladder: climbed on Normal unlocks the second colour, on Hard the third too; the end says so
                foreach (int diff in new[] { 1, 2 })
                {
                    var l = ModesGame("Riley", dir);
                    l.Begin(); l.Menu = BrawlGame.Screen.Ladder; l.LadderPick = sam; l.LadderDiff = diff; l.Sel = 2; l.Tap(Key.Enter);
                    l.LadderRound = BrawlGame.LadderRounds - 1; l.NewGame();
                    l.Step = BrawlGame.Stage.Fight;
                    for (int i = 0; i < 60 * 20 && l.State == Phase.Play; i++)
                    {
                        for (int k = 1; k < l.Sim.F.Length; k++) if (l.Sim.F[k].Playing && l.Sim.F[k].St != BS.Enter) { l.Sim.F[k].Stocks = 1; l.Sim.F[k].X = l.Sim.Stage.BlastR + 80; }
                        l.Update(1.0 / 60); Draw(l);
                    }
                    for (int i = 0; i < 60 * 7 && l.State != Phase.Over; i++) { l.Update(1.0 / 60); Draw(l); }
                    int level = new BrawlUnlocks(Path.Combine(dir, "brawl-unlocks.txt")).Level("Riley", "sam");
                    Check("colours: climbing the ladder on " + BrawlGame.LevelNames[diff] + " unlocks " + (diff == 2 ? "the third colour too" : "the second colour") + ", and the end says so (the score is kept as usual)",
                        l.State == Phase.Over && level == diff && l.UnlockNote != null && l.UnlockNote.Contains("Saguaro Sam") && l.Scores.Best("Brawl", "ladder-" + diff, "Riley") > 0, l.State + " level " + level + " note " + l.UnlockNote);
                    if (diff == 1) Snap(l, "LadderUnlock-check");
                }
            }
            finally { try { Directory.Delete(dir, true); } catch (Exception) { } }

            SnapScene("SelectColours", 1, "SelectColours");
            SnapScene("SelectColours", 2, "SelectSeats");
            SnapScene("LadderColours", 1, "LadderColours");
            SnapScene("LadderUnlock", 1, "LadderUnlock");
            for (int c = 1; c <= 3; c++) SnapScene("Colours", c, "Colours-" + c);
            DisposeFrames();
        }

        // ================================================================ the online rematch
        public static void RematchChecks()
        {
            // the sessions over the pretend network
            var room = new LoopbackRoom(2, 2, 11);
            var host = new NetSession(room.Join(), false, "Kim", 0);
            var ben = new NetSession(room.Join(), false, "Ben", 4);
            Action<int> pump = n => { for (int i = 0; i < n; i++) { room.Pump(); host.Poll(); ben.Poll(); } };
            Action<int> play = target =>
            {
                for (int t = 0; t < target * 4; t++)
                {
                    room.Pump(); host.Poll(); ben.Poll();
                    bool done = true;
                    foreach (var s in new[] { host, ben })
                    {
                        foreach (int seat in s.Match.MySeats()) s.Match.Local(seat, new BrInput { X = t % 90 < 45 ? 1 : -1, Attack = t % 13 == 0, Jump = t % 50 == 0 });
                        for (int k = 0; k < 2 && s.Match.Sim.Frame < target; k++) if (!s.Match.TryStep()) break;
                        if (s.Match.Sim.Frame < target) done = false;
                    }
                    if (done) break;
                }
            };
            pump(10);
            host.Cpus = 1; host.SendLobby(); pump(5);
            host.StartMatch(1001); pump(5);
            play(500);
            Check("rematch: a first match runs the same on both", host.Match.Sim.Frame == 500 && ben.Match.Sim.Frame == 500 && host.Match.Sim.Checksum() == ben.Match.Sim.Checksum() && host.Match.Setup.Round == 1, "");
            var oldSetup = ben.Match.Setup;
            int started = 0; ben.MatchStarted += () => started++;
            Check("rematch: only the host can ask for a rematch", host.CanRematch && !ben.CanRematch && !ben.Rematch(5), "");
            bool ok = host.Rematch(2002);
            pump(6);
            var hs = host.Match.Setup; var bs = ben.Match.Setup;
            Check("rematch: the same fighters and stage with a new seed reach every device, straight into the match",
                ok && started == 1 && bs.Seed == 2002 && bs.Round == 2 && hs.Round == 2 && bs.Stage == oldSetup.Stage && bs.Seats.Count == 3 && bs.Seats.Select(x => x.Fighter).SequenceEqual(oldSetup.Seats.Select(x => x.Fighter)) && ben.Match.Sim.Frame == 0 && ben.State == NetSession.Phase.Playing, bs.Seed + " round " + bs.Round);
            // a message from the first match arriving now is left out
            var stale = new NetWriter(NetProtocol.Inputs).I(1).I(2).I(4).I(200);
            for (int k = 0; k < 200; k++) stale.I(NetProtocol.Pack(new BrInput { X = 1, Attack = true }));
            host.Match.Receive(2, new NetReader(stale.Done()));
            int stepped = 0; for (int k = 0; k < 20; k++) if (host.Match.TryStep()) stepped++;
            Check("rematch: a late message from the match before is left out (the host still waits for Ben's real buttons)", host.Match.Stale >= 1 && host.Match.Sim.Frame == hs.Delay && stepped == hs.Delay, "stale " + host.Match.Stale + ", frame " + host.Match.Sim.Frame);
            play(900);
            Check("rematch: the rematch runs exactly the same on both devices (checksums)", host.Match.Sim.Frame == 900 && ben.Match.Sim.Frame == 900 && host.Match.Sim.Checksum() == ben.Match.Sim.Checksum(), host.Match.Sim.Frame + " " + ben.Match.Sim.Frame);
            int back = 0; ben.BackToLobby += () => back++;
            host.ToLobby(); pump(6);
            Check("rematch: Back to the lobby still works after a rematch", back == 1 && ben.State == NetSession.Phase.Lobby && host.State == NetSession.Phase.Lobby, "");
            host.StartMatch(3003); pump(6);
            Check("rematch: a match started from the lobby afterwards is the room's third", ben.Match != null && ben.Match.Setup.Round == 3 && ben.Match.Setup.Seed == 3003, ben.Match == null ? "none" : ben.Match.Setup.Round.ToString());
            host.Leave(); pump(3);

            // the whole game: the results' buttons by touch on both devices
            Audio.MusicOn = false;
            var room2 = new LoopbackRoom(1, 1, 3);
            string dir = SaveTemp("rematch");
            try
            {
                var kim = ModesGame("Kim", dir); var eli = ModesGame("Eli", dir);
                kim.Begin(); eli.Begin();
                kim.GoOnlineForTest(new NetSession(room2.Join(), false, "Kim", 0));
                eli.GoOnlineForTest(new NetSession(room2.Join(), false, "Eli", 3));
                Action<int> frames = n => { for (int i = 0; i < n; i++) { room2.Pump(); kim.Update(1.0 / 60); eli.Update(1.0 / 60); Draw(kim); Draw(eli); } };
                frames(10);
                kim.Online.Cpus = 0; kim.Online.SendLobby(); frames(5);
                kim.Online.StartMatch(77); frames(5);
                frames(60 * 4);
                Check("rematch game: both play the first match", kim.State == Phase.Play && eli.State == Phase.Play && eli.Sim != null && eli.Sim.Frame > 60, "");
                kim.ShowResultsForTest(); eli.ShowResultsForTest();
                frames(1);
                Snap(kim, "OnlineResultsHost"); SnapPhone(kim, "OnlineResultsHost-phone");
                Snap(eli, "OnlineResultsGuest");
                var hostButtons = kim.MouseHits.Where(h => h.Label == "Rematch" || h.Label == "Back to the lobby").ToList();
                Check("rematch game: the host's results have big Rematch and Back to the lobby buttons (44+ pixels on a phone)", hostButtons.Count == 2 && hostButtons.All(h => h.Area.Height * PhoneScale >= 44), string.Join(", ", kim.MouseHits.Select(h => h.Label)));
                Check("rematch game: the guest sees its own Leave and no host buttons", eli.MouseHits.Any(h => h.Label == "Leave") && !eli.MouseHits.Any(h => h.Label == "Rematch"), string.Join(", ", eli.MouseHits.Select(h => h.Label)));
                uint seed1 = kim.Online.Match.Setup.Seed;
                TapLabel(kim, "Rematch");
                frames(4);
                Check("rematch game: tapping Rematch starts the countdown on both devices with a new seed", kim.Step == BrawlGame.Stage.Countdown && eli.Step == BrawlGame.Stage.Countdown && eli.Online.Match.Setup.Seed != seed1 && eli.Online.Match.Setup.Round == 2 && eli.Sim.Frame <= 4, kim.Step + " " + eli.Step);
                frames(60 * 6);
                for (int i = 0; i < 200 && kim.Sim.Frame != eli.Sim.Frame; i++) { room2.Pump(); if (kim.Sim.Frame < eli.Sim.Frame) kim.Update(1.0 / 60); else eli.Update(1.0 / 60); }
                Check("rematch game: the rematch is the same match on both devices", kim.Sim.Frame == eli.Sim.Frame && kim.Sim.Frame > 100 && kim.Sim.Checksum() == eli.Sim.Checksum(), kim.Sim.Frame + " " + eli.Sim.Frame);
                kim.ShowResultsForTest(); eli.ShowResultsForTest(); frames(1);
                TapLabel(kim, "Back to the lobby");
                frames(5);
                Check("rematch game: Back to the lobby takes both back to the lobby", kim.Menu == BrawlGame.Screen.OnlineLobby && eli.Menu == BrawlGame.Screen.OnlineLobby && eli.State == Phase.Setup, kim.Menu + " " + eli.Menu);
                kim.Online.StartMatch(78); frames(60 * 2);
                kim.ShowResultsForTest(); eli.ShowResultsForTest(); frames(1);
                TapLabel(eli, "Leave");
                frames(5);
                Check("rematch game: the guest's Leave leaves the game (back to Play online)", eli.Online == null && eli.Menu == BrawlGame.Screen.Online && eli.State == Phase.Setup, eli.Menu + " " + eli.State);
                frames(5);
                Check("rematch game: the host, alone now, can't rematch (only the lobby)", kim.Online != null && !kim.Online.CanRematch, "");
                kim.Online?.Leave();
            }
            finally { try { Directory.Delete(dir, true); } catch (Exception) { } }
            DisposeFrames();
            Audio.StopMusic(); Audio.StopEffects();
        }
    }
}
