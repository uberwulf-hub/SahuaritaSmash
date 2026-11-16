// Classic Games - Sahuarita Smash's checks (part of -Test, group "brawl"): the fighters' data, standing, walking off and
// dropping through, knockback that grows with damage and falls off with weight, strong attacks that win a life at
// sensible damage, every fighter coming back to the stage from off it, ledges, shields, grabs, the same buttons giving
// the same match, whole matches between computer players on every stage (someone wins, nobody walks off), every stage
// surprise warning first, sudden death, the ladder, the sounds and the pictures. Nothing here plays sound.
using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Media;

namespace ClassicGames
{
    public static partial class Tests
    {
        static readonly string[] BrawlMoveIds = { "jab", "ftilt", "utilt", "dtilt", "dash", "fstrong", "ustrong", "dstrong", "nair", "fair", "bair", "uair", "dair", "grab", "fthrow", "bthrow", "uthrow", "dthrow", "nspec", "sspec", "uspec", "dspec" };

        static BrawlSim BrawlMatch(int stage, uint seed, params int[] fighters)
        {
            var defs = fighters.Select(i => BrawlData.Fighters[i]).ToList();
            return new BrawlSim(BrawlData.Stages[stage], defs, 3, 0, seed);
        }

        static void BrawlRun(BrawlSim sim, BrawlBot[] bots, int frames)
        {
            var inp = new BrInput[sim.F.Length];
            for (int i = 0; i < frames && !sim.Over; i++)
            {
                for (int s = 0; s < sim.F.Length; s++) inp[s] = bots != null && bots[s] != null ? bots[s].Think(sim) : new BrInput();
                sim.Step(inp);
            }
        }

        static void BrawlChecks()
        {
            BrawlDataChecks();
            BrawlPhysicsChecks();
            BrawlKnockbackChecks();
            BrawlRecoveryChecks();
            BrawlMechanicsChecks();
            BrawlMatchChecks();
            BrawlHazardChecks();
            BrawlGameChecks();
            BrawlSoundChecks();
            BrawlMusicChecks();
            BrawlKeyChecks();
            BrawlBufferChecks();
            BrawlMotionChecks();
            BrawlStageChecks();
            BrawlDrawChecks();
        }

        static void BrawlDataChecks()
        {
            Check("brawl: eleven fighters from five groups and six stages", BrawlData.Fighters.Count == 11 && BrawlData.Fighters.Select(f => f.Group).Distinct().Count() == 5 && BrawlData.Stages.Count == 6, BrawlData.Fighters.Count + " " + BrawlData.Stages.Count);
            var missing = new List<string>(); var bad = new List<string>();
            foreach (BFighterDef f in BrawlData.Fighters)
            {
                foreach (string id in BrawlMoveIds) if (f.M(id) == null) missing.Add(f.Id + "." + id);
                if (f.SpecialNames.Any(string.IsNullOrEmpty)) missing.Add(f.Id + " special names");
                foreach (BMove m in f.Moves.Values)
                {
                    foreach (BHit h in m.Hits) if (h.From < 1 || h.To < h.From || h.To > m.Len + 60 || h.R <= 0) bad.Add(f.Id + "." + m.Id + " hit " + h.From + "-" + h.To);
                    // every attack shows itself before it lands (a wind-up of at least 3 frames)
                    if ((m.Hits.Count > 0 || m.Shot != null) && m.FirstActive < 3) bad.Add(f.Id + "." + m.Id + " lands on frame " + m.FirstActive);
                    if (m.Shot != null && m.ShotAt > m.Len) bad.Add(f.Id + "." + m.Id + " shot after the move");
                }
                BMove up = f.M("uspec");
                if (up == null || up.Kind != "rise" || !up.Helpless) bad.Add(f.Id + " up special isn't a rise");
                if (f.Weight < 70 || f.Weight > 140 || f.Jumps < 2) bad.Add(f.Id + " stats");
            }
            Check("brawl: every fighter has every move and four named specials", missing.Count == 0, string.Join(", ", missing.Take(6)));
            Check("brawl: every move's hits are in its frames, and every attack winds up first (3+ frames)", bad.Count == 0, string.Join(", ", bad.Take(6)));
            Check("brawl: fighter and stage ids are unique", BrawlData.Fighters.Select(f => f.Id).Distinct().Count() == 11 && BrawlData.Stages.Select(s => s.Id).Distinct().Count() == 6, "");
            // hits out at the same time land once between them (groups)
            BMove lash = BrawlData.Fighter("specimen").M("nspec");
            BMove cop = BrawlData.Fighter("bloom").M("uspec");
            Check("brawl: overlapping hits share a group, a multi-hit's last blow has its own",
                lash.Hits[0].Group == lash.Hits[1].Group && cop.Hits[0].Group != cop.Hits[1].Group, lash.Hits[0].Group + " " + lash.Hits[1].Group + " / " + cop.Hits[0].Group + " " + cop.Hits[1].Group);
        }

        static void BrawlPhysicsChecks()
        {
            // standing still for ten seconds: never falls through the ground
            BrawlSim sim = BrawlMatch(0, 3, 0, 9);
            BrawlRun(sim, null, 600);
            Check("brawl: fighters standing still stay on the ground", sim.F.All(f => f.Ground && f.St == BS.Idle && Math.Abs(f.Y) < 0.01), string.Join(", ", sim.F.Select(f => f.St + " " + f.Y.ToString("0.0"))));
            // walking right walks off the edge and falls
            sim = BrawlMatch(0, 3, 4, 9);
            BFighter d = sim.F[0];
            var inp = new BrInput[2];
            bool fell = false;
            for (int i = 0; i < 400 && !fell; i++) { inp[0] = new BrInput { X = 1 }; sim.Step(inp); if (!d.Ground && d.X > sim.Main.R) fell = true; }
            Check("brawl: walking past the edge falls off", fell, d.X.ToString("0"));
            // jumping comes back down onto the ground
            sim = BrawlMatch(0, 3, 0, 9);
            BFighter b = sim.F[0]; b.X = 0;
            double top = 0; bool left = false;
            for (int i = 0; i < 200; i++) { inp[0] = new BrInput { Jump = i < 12 }; sim.Step(inp); top = Math.Max(top, b.Y); if (!b.Ground) left = true; }
            Check("brawl: a jump goes up and lands again", left && top > 100 && b.Ground, top.ToString("0"));
            // a tap of jump is a short hop, lower than a full one
            sim = BrawlMatch(0, 3, 0, 9); b = sim.F[0]; double shortTop = 0;
            for (int i = 0; i < 200; i++) { inp[0] = new BrInput { Jump = i < 2 }; sim.Step(inp); shortTop = Math.Max(shortTop, b.Y); }
            Check("brawl: a short tap is a short hop", shortTop > 30 && shortTop < top * 0.75, shortTop.ToString("0") + " vs " + top.ToString("0"));
            // standing on a thin platform, down drops through it
            sim = BrawlMatch(0, 3, 0, 9); b = sim.F[0];
            BPlat soft = sim.Plats.First(p => !p.Solid);
            b.X = (soft.L + soft.R) / 2; b.Y = soft.Top + 30; b.Ground = false; b.On = null; b.St = BS.Air;
            for (int i = 0; i < 60; i++) { inp[0] = new BrInput(); sim.Step(inp); }
            bool stood = b.Ground && b.On == soft;
            for (int i = 0; i < 40; i++) { inp[0] = new BrInput { Y = i < 3 ? -1 : 0 }; sim.Step(inp); }
            Check("brawl: you can stand on a thin platform, and down drops through it", stood && b.On == sim.Main, stood + " " + (b.On == sim.Main));
            // jumping up through a thin platform from below lands on it
            sim = BrawlMatch(0, 3, 0, 9); b = sim.F[0]; b.X = (soft.L + soft.R) / 2;
            bool onSoft = false;
            for (int i = 0; i < 120; i++) { inp[0] = new BrInput { Jump = i < 14, X = 0 }; sim.Step(inp); if (b.Ground && b.On == sim.Plats.First(p => !p.Solid)) onSoft = true; }
            Check("brawl: jumping up through a thin platform lands on top of it", onSoft, b.Y.ToString("0"));
        }

        static double KillPercent(BFighterDef att, string move, BFighterDef target, BStageDef stage)
        {
            BHit h = att.M(move).Hits.OrderByDescending(x => x.Dmg).First();
            for (int pct = 0; pct <= 400; pct += 5)
            {
                var sim = new BrawlSim(stage, new List<BFighterDef> { att, target }, 3, 0, 9);
                BFighter a = sim.F[0], t = sim.F[1];
                a.X = 0; t.X = 40; a.Face = 1; t.Dmg = pct;
                sim.Hit(a, t, h, 1, 1, false);
                var inp = new BrInput[2];
                for (int i = 0; i < 400 && t.Stocks == 3; i++) sim.Step(inp);
                if (t.Stocks < 3) return pct;
            }
            return 999;
        }

        static void BrawlKnockbackChecks()
        {
            var hit = new BHit { Dmg = 10, Ang = 45, Base = 30, Grow = 90 };
            double k0 = BrawlSim.Knockback(10, 10, 100, hit), k1 = BrawlSim.Knockback(100, 10, 100, hit), heavy = BrawlSim.Knockback(100, 10, 130, hit);
            Check("brawl: knockback grows with damage and falls off with weight", k1 > k0 * 2 && heavy < k1, k0.ToString("0") + " " + k1.ToString("0") + " " + heavy.ToString("0"));
            BStageDef mesa = BrawlData.Stages[0];
            BFighterDef mid = BrawlData.Fighter("trooper");
            var kills = new List<string>(); bool ok = true;
            foreach (BFighterDef f in BrawlData.Fighters)
            {
                double kp = KillPercent(f, "fstrong", mid, mesa);
                kills.Add(f.Id + " " + kp);
                if (kp < 60 || kp > 200) ok = false;
            }
            Check("brawl: every fighter's strong side attack wins a life from the middle of the mesa between 60% and 200%", ok, string.Join(", ", kills));
            double light = KillPercent(BrawlData.Fighter("trooper"), "fstrong", BrawlData.Fighter("dusty"), mesa), hv = KillPercent(BrawlData.Fighter("trooper"), "fstrong", BrawlData.Fighter("paul"), mesa);
            Check("brawl: a light fighter flies off sooner than a heavy one", light < hv, light + " vs " + hv);
            double jab = KillPercent(mid, "jab", mid, mesa);
            Check("brawl: a jab never wins a life at ordinary damage", jab >= 300, jab.ToString());
            double moon = KillPercent(mid, "ustrong", mid, BrawlData.Stages[3]), earth = KillPercent(mid, "ustrong", mid, mesa);
            Check("brawl: on the low-gravity moon an up strong attack sends you off the top sooner", moon <= earth, moon + " vs " + earth);
        }

        static void BrawlRecoveryChecks()
        {
            // every fighter comes back from off the stage on each side, from below and above the ledge
            var fails = new List<string>();
            foreach (BFighterDef f in BrawlData.Fighters)
                foreach (int side in new[] { -1, 1 })
                    foreach (double dx in new[] { 140.0, 240.0 })
                        foreach (double dy in new[] { -120.0, 0.0, 120.0 })
                            foreach (int stage in new[] { 0, 4 })
                            {
                                var sim = new BrawlSim(BrawlData.Stages[stage], new List<BFighterDef> { f, BrawlData.Fighters[0] }, 3, 0, 5);
                                sim.Surprises = false;
                                BFighter me = sim.F[0];
                                me.X = side * (sim.Main.R + dx); me.Y = dy; me.Ground = false; me.On = null; me.St = BS.Air; me.Vy = -2;
                                me.Jumps = f.Jumps - 1;
                                sim.F[1].X = -side * 200;
                                var bots = new[] { new BrawlBot(0, 9) { RecoverOnly = true }, null };
                                bool home = false;
                                var inp = new BrInput[2];
                                for (int i = 0; i < 600 && !home; i++)
                                {
                                    inp[0] = bots[0].Think(sim); sim.Step(inp);
                                    if (me.St == BS.Ledge || me.Ground) home = true;
                                    if (me.Stocks < 3) break;
                                }
                                if (!home) fails.Add(f.Id + " " + sim.Stage.Id + " side " + side + " dx " + dx + " dy " + dy);
                            }
            Check("brawl: every fighter gets back to the stage from off either side (two stages, six spots each)", fails.Count == 0, fails.Count + ": " + string.Join("; ", fails.Take(5)));
        }

        static void BrawlMechanicsChecks()
        {
            var inp = new BrInput[2];
            // ledge: falling past the edge grabs it, up climbs onto the stage
            BrawlSim sim = BrawlMatch(0, 3, 6, 0);
            BFighter t = sim.F[0];
            t.X = sim.Main.R + 12; t.Y = sim.Main.Top - t.H * 0.6; t.Ground = false; t.On = null; t.St = BS.Air; t.Vy = -1;
            bool hung = false;
            for (int i = 0; i < 30 && !hung; i++) { sim.Step(inp); hung = t.St == BS.Ledge; }
            for (int i = 0; i < 40; i++) { inp[0] = new BrInput { Y = i > 10 && i < 14 ? 1 : 0 }; sim.Step(inp); }
            Check("brawl: falling past the edge grabs the ledge, and up climbs back on", hung && t.Ground && t.On == sim.Main, hung + " " + t.St);
            // shield: hits shrink it until it breaks into a dizzy spell
            sim = BrawlMatch(0, 3, 9, 2); inp = new BrInput[2];
            BFighter a = sim.F[0], s = sim.F[1];
            a.X = -30; s.X = 30;
            bool broke = false;
            for (int i = 0; i < 40 && !broke; i++)
            {
                for (int k = 0; k < 3; k++) { inp[1] = new BrInput { Shield = true }; sim.Step(inp); }
                if (s.St == BS.Shield) sim.Hit(a, s, BrawlData.Fighter("paul").M("fstrong").Hits[0], 1, 1, false);
                broke = s.St == BS.Dizzy;
            }
            Check("brawl: a shield takes hits but breaks into a dizzy spell", broke && s.Dmg == 0, s.St + " " + s.ShieldHp.ToString("0"));
            // a grab goes through a shield and a throw adds damage
            sim = BrawlMatch(0, 3, 2, 9); inp = new BrInput[2];
            a = sim.F[0]; s = sim.F[1]; a.X = 0; s.X = 40; a.Face = 1;
            bool grabbed = false;
            for (int i = 0; i < 120; i++)
            {
                inp[1] = new BrInput { Shield = true };
                inp[0] = new BrInput { Grab = i == 5, X = i > 30 && i < 40 ? 1 : 0 };
                sim.Step(inp);
                if (s.St == BS.Grabbed) grabbed = true;
            }
            Check("brawl: a grab gets through a shield, and the throw adds damage", grabbed && s.Dmg > 5, grabbed + " " + s.Dmg.ToString("0"));
            // a counter sends a hit back
            sim = BrawlMatch(0, 3, 6, 2); inp = new BrInput[2];
            BFighter teddy = sim.F[0], tr = sim.F[1]; teddy.X = 0; tr.X = 50; teddy.Face = 1; tr.Face = -1;
            sim.StartMove(teddy, "dspec");
            for (int i = 0; i < 10; i++) sim.Step(inp);
            sim.Hit(tr, teddy, tr.D.M("ftilt").Hits[0], -1, 1, false);
            Check("brawl: Teddy's counter catches a hit and sends it back", teddy.Dmg == 0 && tr.Dmg >= 8, teddy.Dmg + " / " + tr.Dmg.ToString("0"));
            // Bloom's bubble bounces a shot back to whoever threw it
            sim = BrawlMatch(0, 3, 0, 2); inp = new BrInput[2];
            BFighter bl = sim.F[0], so = sim.F[1]; bl.X = 0; so.X = 300; so.Face = -1; bl.Face = 1;
            sim.StartMove(so, "nspec");
            bool reflected = false;
            for (int i = 0; i < 80; i++)
            {
                BShot shot = sim.Shots.FirstOrDefault(x => x.On);
                if (shot != null && Math.Abs(shot.X - bl.X) < 170 && bl.St != BS.Attack) sim.StartMove(bl, "dspec");
                sim.Step(inp);
                if (sim.Shots.Any(x => x.On && x.Owner == 0)) reflected = true;
            }
            Check("brawl: Bloom's bubble sends a shot back the other way", reflected && bl.Dmg == 0, reflected + " " + bl.Dmg);
            // a firewall stops the other side's shots
            sim = BrawlMatch(0, 3, 8, 2); inp = new BrInput[2];
            BFighter kn = sim.F[0], gun = sim.F[1]; kn.X = 0; kn.Face = 1; gun.X = 320; gun.Face = -1;
            sim.StartMove(kn, "sspec");
            for (int i = 0; i < 20; i++) sim.Step(inp);
            sim.StartMove(gun, "nspec");
            for (int i = 0; i < 60; i++) sim.Step(inp);
            Check("brawl: the Firewall Knight's wall stops shots", kn.Dmg == 0, kn.Dmg.ToString());
            // flying past the side loses a life and comes back after a moment, safe for a while
            sim = BrawlMatch(0, 3, 0, 9); inp = new BrInput[2];
            BFighter v = sim.F[0]; v.X = sim.Stage.BlastR + 5; v.Ground = false; v.St = BS.Hurt;
            sim.Step(inp);
            bool lost = v.Stocks == 2 && v.Out;
            for (int i = 0; i < 110; i++) sim.Step(inp);
            Check("brawl: past the edge you lose a life, then come back safe", lost && v.St == BS.Enter && v.Invuln > 0 && v.Dmg == 0, v.St + " " + v.Stocks);
        }

        static void BrawlMatchChecks()
        {
            // the same buttons (and seed) always give the same match
            Func<uint, long> run = seed =>
            {
                BrawlSim m = BrawlMatch(1, seed, 0, 3, 6, 9);
                var bots = Enumerable.Range(0, 4).Select(i => new BrawlBot(i, 4 + i)).ToArray();
                BrawlRun(m, bots, 3000);
                return m.Checksum();
            };
            long c1 = run(11), c2 = run(11), c3 = run(12);
            Check("brawl: the same seed gives the same match, another seed another", c1 == c2 && c1 != c3, c1 + " " + c2 + " " + c3);
            // whole matches between computer players on every stage: someone wins, and nobody walks off by themselves
            var notes = new List<string>(); bool ok = true; int sds = 0;
            var sw = System.Diagnostics.Stopwatch.StartNew(); long frames = 0;
            for (int st = 0; st < BrawlData.Stages.Count; st++)
            {
                int[] who = { st % 10, (st + 3) % 10, (st + 6) % 10, (st + 8) % 10 };
                BrawlSim m = BrawlMatch(st, (uint)(100 + st), who);
                var bots = Enumerable.Range(0, 4).Select(i => new BrawlBot(i, 3 + 2 * i)).ToArray();
                BrawlRun(m, bots, 60 * 60 * 8);
                frames += m.Frame;
                int sd = m.F.Sum(f => f.SDs); sds += sd;
                notes.Add(m.Stage.Id + " " + (m.Frame / 3600.0).ToString("0.0") + " min, winner " + m.Winner + ", SD " + sd);
                if (!m.Over || m.Winner < 0) ok = false;
            }
            Check("brawl: a four-way match between computer players ends with a winner on every stage", ok, string.Join("; ", notes));
            Check("brawl: computer players hardly ever fall off by themselves (at most 1 in 5 matches)", sds <= 1, sds.ToString());
            double us = sw.Elapsed.TotalMilliseconds * 1000 / Math.Max(1, frames);
            Check("brawl: a step with four fighters and the computer players takes well under a millisecond", us < 300, us.ToString("0") + " us");
            // time: the most bonks wins; a tie goes to sudden death at 300%
            BrawlSim tm = new BrawlSim(BrawlData.Stages[0], new List<BFighterDef> { BrawlData.Fighters[0], BrawlData.Fighters[1] }, 3, 2, 7);
            tm.TimeLeft = 3;
            BrawlRun(tm, null, 5);
            Check("brawl: a timed match tied at the end goes to sudden death at 300%", tm.Sudden && !tm.TimeMode && tm.F.All(f => f.Stocks == 1 && f.Dmg == 300), tm.Sudden + " " + tm.F[0].Dmg);
            tm = new BrawlSim(BrawlData.Stages[0], new List<BFighterDef> { BrawlData.Fighters[0], BrawlData.Fighters[1] }, 3, 2, 7);
            tm.F[1].KOs = 1; tm.TimeLeft = 3;
            BrawlRun(tm, null, 200);
            Check("brawl: a timed match goes to the most bonks", tm.Over && tm.Winner == 1, tm.Winner.ToString());
        }

        static void BrawlHazardChecks()
        {
            // every stage surprise is warned at least a second and a half before it happens
            var notes = new List<string>(); bool ok = true;
            for (int st = 0; st < BrawlData.Stages.Count; st++)
            {
                BrawlSim m = BrawlMatch(st, (uint)(7 + st), 0, 9);
                int warn = 0, happen = 0, longestWarn = 0, run1 = 0;
                bool sawWarn = false, badStart = false;
                int prev = 0;
                var inp = new BrInput[2];
                for (int i = 0; i < 60 * 90; i++)
                {
                    m.Step(inp);
                    if (m.HazPhase == 1) { warn++; run1++; sawWarn = true; }
                    if (m.HazPhase == 2) { happen++; if (prev == 0) badStart = true; if (prev == 1) longestWarn = Math.Max(longestWarn, run1); }
                    if (m.HazPhase != 1) run1 = 0;
                    prev = m.HazPhase;
                    foreach (BFighter f in m.F) { f.Dmg = 0; if (f.Out) { } }
                }
                notes.Add(m.Stage.Id + " " + m.Hazard + " warned " + longestWarn + " frames, happened " + happen);
                if (!sawWarn || happen == 0 || badStart || longestWarn < 90) ok = false;
            }
            Check("brawl: every stage's surprise happens, and always after a warning of 1.5 s", ok, string.Join("; ", notes));
            BrawlSim off = BrawlMatch(0, 7, 0, 9); off.Surprises = false;
            BrawlRun(off, null, 60 * 60);
            Check("brawl: with surprises off nothing happens", off.HazPhase == 0, off.HazPhase.ToString());
        }

        static void BrawlGameChecks()
        {
            Audio.MusicOn = false;
            var book = new ScoreBook(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "brawl-test-scores-" + Guid.NewGuid().ToString("N") + ".txt"));
            var g = new BrawlGame { Scores = book, Player = "Kim", Rng = new Random(4) };
            g.Begin();
            Check("brawl: it opens on its title screen", g.State == Phase.Setup && g.Menu == BrawlGame.Screen.Title, g.State.ToString());
            // the ladder: winning a round goes on to the next; round 3 is two at once; the last a giant
            g.LadderPick = 2; g.LadderDiff = 1;
            g.Tap(Avalonia.Input.Key.Down); g.Tap(Avalonia.Input.Key.Enter);
            Check("brawl: Down then Enter opens the ladder", g.Menu == BrawlGame.Screen.Ladder, g.Menu.ToString());
            g.Tap(Avalonia.Input.Key.Enter);
            bool r1 = g.State == Phase.Play && g.Ladder && g.Sim.F.Length == 2 && g.Sim.F[0].D.Id == "trooper";
            var sizes = new List<int>(); bool giant = false;
            for (int round = 0; round < BrawlGame.LadderRounds && g.State == Phase.Play; round++)
            {
                sizes.Add(g.Sim.F.Length);
                if (g.Sim.F.Any(f => f.Size > 1.5)) giant = true;
                for (int i = 1; i < g.Sim.F.Length; i++) { g.Sim.F[i].Stocks = 1; g.Sim.F[i].X = 5000; g.Sim.F[i].St = BS.Hurt; }
                for (int i = 0; i < 60 * 14 && g.LadderRound == round && g.State == Phase.Play; i++) g.Update(1.0 / 60);
            }
            for (int i = 0; i < 120; i++) g.Update(1.0 / 60);
            Check("brawl: the ladder: six rounds, two foes in round 3, a giant at the top, then the score is kept",
                r1 && sizes.SequenceEqual(new[] { 2, 2, 3, 2, 2, 2 }) && giant && g.State != Phase.Play && book.Best("Brawl", "ladder-1", "Kim") > 0, string.Join(",", sizes) + " " + giant + " " + g.State + " " + book.Best("Brawl", "ladder-1", "Kim"));
            // a versus match with two people: the left keys move the first, the right keys the second
            g = new BrawlGame { Scores = book, Player = "Kim", Rng = new Random(4) };
            g.Begin(); g.Menu = BrawlGame.Screen.Select;
            g.Who[0] = 1; g.Who[1] = 1; g.Who[2] = 0; g.Who[3] = 0; g.StagePick = 0;
            g.Sel = 11; g.Tap(Avalonia.Input.Key.Enter);
            for (int i = 0; i < 160; i++) g.Update(1.0 / 60);
            double x0 = g.Sim.F[0].X, x1 = g.Sim.F[1].X;
            g.Press(Avalonia.Input.Key.D); g.Press(Avalonia.Input.Key.Left);
            for (int i = 0; i < 30; i++) g.Update(1.0 / 60);
            g.Release(Avalonia.Input.Key.D); g.Release(Avalonia.Input.Key.Left);
            Check("brawl: two people on one keyboard: the left keys and the right keys move their own fighters", g.Sim.F[0].X > x0 + 40 && g.Sim.F[1].X < x1 - 40, (g.Sim.F[0].X - x0).ToString("0") + " " + (g.Sim.F[1].X - x1).ToString("0"));
            g.Tap(Avalonia.Input.Key.F);
            g.Update(1.0 / 60);
            Check("brawl: F attacks", g.Sim.F[0].St == BS.Attack, g.Sim.F[0].St.ToString());
            var one = new BrawlGame { Scores = book, Player = "Kim" };
            one.Begin(); one.Menu = BrawlGame.Screen.Select; one.Who[0] = 1; one.Who[1] = 0; one.Who[2] = 0; one.Who[3] = 0; one.Sel = 11;
            one.Tap(Avalonia.Input.Key.Enter);
            Check("brawl: with only one fighter chosen, Fight! says to choose another and doesn't start", one.State == Phase.Setup, one.State.ToString());
            Audio.StopMusic(); Audio.StopEffects();
            try { System.IO.File.Delete(book.Path); } catch (Exception) { }
        }

        static void BrawlSoundChecks()
        {
            var bad = new List<string>();
            foreach (string n in BrawlSfx.Names)
            {
                float[] b = BrawlSfx.Make(n);
                double peak = b.Length == 0 ? 0 : b.Max(x => Math.Abs(x));
                if (b.Length < Audio.Rate / 40 || peak < 0.05 || peak > 0.81) bad.Add(n + " " + b.Length + " " + peak.ToString("0.00"));
            }
            Check("brawl: every sound is made, not empty and not too loud", bad.Count == 0, string.Join(", ", bad.Take(5)));
            // every sound a match asks for exists
            var asked = new HashSet<string>();
            foreach (BFighterDef f in BrawlData.Fighters) foreach (BMove m in f.Moves.Values) if (m.Sound != "") asked.Add(m.Sound);
            for (int st = 0; st < BrawlData.Stages.Count; st++)
            {
                BrawlSim m = BrawlMatch(st, (uint)(31 + st), 0, 4, 6, 9);
                var bots = Enumerable.Range(0, 4).Select(i => new BrawlBot(i, 6)).ToArray();
                var inp = new BrInput[4];
                for (int i = 0; i < 60 * 60 && !m.Over; i++) { for (int s = 0; s < 4; s++) inp[s] = bots[s].Think(m); m.Step(inp); foreach (BrEvent e in m.Events) if (e.K == "sfx") asked.Add(e.Name); }
            }
            var unknown = asked.Where(n => !BrawlSfx.Names.Contains(n)).ToList();
            Check("brawl: every sound the fighters and stages use exists", unknown.Count == 0, string.Join(", ", unknown));
            for (int s = 0; s < 6; s++) { Song song = Music.BrawlSong(s); if (song == null) bad.Add("song " + s); }
            GameTunes t = Music.For("Brawl");
            Check("brawl: a tune for every stage and the menus, and the stings", bad.Count == 0 && t.Theme != null && t.LevelClear != null && t.HighScore != null && t.GameOver != null && t.Extra2 != null, "");
        }

        static void BrawlDrawChecks()
        {
            // every fighter in every state draws, on every stage, and the menus draw
            var view = new BrawlView();
            int drawn = 0; string err = null;
            try
            {
                foreach (BStageDef st in BrawlData.Stages)
                {
                    BrawlSim m = new BrawlSim(st, BrawlData.Fighters.Take(4).ToList(), 3, 0, 3);
                    var dv = new DrawingVisual();
                    using (DrawingContext dc = dv.RenderOpen()) { view.Follow(m, true); view.Draw(dc, m, 1.5); view.DrawPanels(dc, m, null, 1.5); }
                    drawn++;
                }
                foreach (BFighterDef f in BrawlData.Fighters)
                    foreach (BS s in Enum.GetValues(typeof(BS)))
                    {
                        var dv = new DrawingVisual();
                        using (DrawingContext dc = dv.RenderOpen()) { view.Portrait(dc, f, 100, 200, 100, 0.5, false, s); view.Portrait(dc, f, 100, 200, 40, 0.5, true, s); }
                        drawn++;
                    }
                foreach (BFighterDef f in BrawlData.Fighters)
                    foreach (BMove mv in f.Moves.Values)
                        for (int fr = 1; fr <= mv.Len; fr += 4)
                        {
                            var bf = new BFighter { D = f, St = BS.Attack, Mv = mv, MF = fr, Face = -1 };
                            var dv = new DrawingVisual();
                            using (DrawingContext dc = dv.RenderOpen()) view.DrawPuppet(dc, bf, 100, 200, 1, 0.5, 0);
                            drawn++;
                        }
                var g = new BrawlGame { Player = "Kim" };
                foreach (string sc in new[] { "Title", "Select", "Help", "Ladder" })
                {
                    g.SnapshotScene(sc, 0, 1);
                    var dv = new DrawingVisual();
                    using (DrawingContext dc = dv.RenderOpen()) g.Render(dc);
                    drawn++;
                }
            }
            catch (Exception ex) { err = ex.GetType().Name + ": " + ex.Message; }
            Check("brawl: every stage, every fighter in every state and frame of every move, and the menus draw", err == null, err ?? drawn + " pictures");
        }

        static void BrawlUpdateChecks(string dir)
        {
            Audio.MusicOn = false;
            var book = new ScoreBook(System.IO.Path.Combine(dir, "brawl-scores.txt"));
            // a versus match mid-fight: who, the stage, everyone's lives and damage are kept and carried on
            var g = new BrawlGame { Scores = book, Player = "Kim", Rng = new Random(2) };
            g.Begin(); g.Who[0] = 1; g.Who[1] = 2; g.Who[2] = 2; g.Who[3] = 0; g.Pick[0] = 6; g.Pick[1] = 9; g.Pick[2] = 4; g.StagePick = 2; g.Level[1] = 3;
            g.NewGame();
            for (int i = 0; i < 300; i++) g.Update(1.0 / 60);
            g.Sim.F[0].Dmg = 42; g.Sim.F[1].Stocks = 2; g.Sim.F[1].Dmg = 87.5; g.Sim.F[2].KOs = 1;
            IDictionary<string, string> v = g.SaveForUpdate();
            var g2 = new BrawlGame { Scores = book, Player = "Kim", Rng = new Random(9) };
            g2.Begin();
            bool ok = v != null && g2.ResumeAfterUpdate(v);
            Check("update brawl: a match carries on after the update: the same fighters and stage, lives and damage",
                ok && g2.State == Phase.Play && g2.Sim.Stage.Id == "server" && g2.Sim.F.Length == 3 && g2.Sim.F[0].D.Id == "teddy" && g2.Sim.F[0].Dmg == 42 && g2.Sim.F[1].Stocks == 2 && g2.Sim.F[1].Dmg == 87.5 && g2.Sim.F[2].KOs == 1 && g2.Level[1] == 3,
                ok + " " + (g2.Sim != null ? g2.Sim.Stage.Id + " " + string.Join(",", g2.Sim.F.Select(f => f.D.Id + " " + f.Dmg + " x" + f.Stocks)) : ""));
            // the ladder: its round, score and foes
            var l = new BrawlGame { Scores = book, Player = "Kim", Rng = new Random(2) };
            l.Begin(); l.LadderPick = 1; l.LadderDiff = 2; l.Menu = BrawlGame.Screen.Ladder; l.Tap(Avalonia.Input.Key.Enter);
            l.LadderRound = 3; l.LadderScore = 4321; l.NewGame();
            string foe = l.Sim.F[1].D.Id;
            v = l.SaveForUpdate();
            var l2 = new BrawlGame { Scores = book, Player = "Kim" };
            l2.Begin();
            ok = v != null && l2.ResumeAfterUpdate(v);
            Check("update brawl: the ladder carries on at the same round against the same foe, with its score",
                ok && l2.Ladder && l2.LadderRound == 3 && l2.LadderScore == 4321 && l2.LadderDiff == 2 && l2.Sim.F[0].D.Id == "specimen" && l2.Sim.F[1].D.Id == foe, ok + " " + l2.LadderRound + " " + l2.LadderScore);
            // the menus and the results have nothing to keep
            var m = new BrawlGame { Scores = book, Player = "Kim" };
            m.Begin();
            Check("update brawl: the menus have nothing to carry on", m.SaveForUpdate() == null, "");
            Audio.StopMusic(); Audio.StopEffects();
        }
    }
}
