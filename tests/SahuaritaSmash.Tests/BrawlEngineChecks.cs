// Sahuarita Smash's match checks, copied unchanged from App Hub's Classic Games (Engine\BrawlTests.cs and
// Engine\BrawlKeysTests.cs, the groups that need only the match engine; the buffer checks also need the menus). The checks that need the pictures, menus,
// keys and sounds come with those parts.
using System;
using System.Collections.Generic;
using System.Linq;

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
        static void BrawlDataChecks()
        {
            Check("brawl: ten fighters from five groups and five stages", BrawlData.Fighters.Count == 10 && BrawlData.Fighters.Select(f => f.Group).Distinct().Count() == 5 && BrawlData.Stages.Count == 5, BrawlData.Fighters.Count + " " + BrawlData.Stages.Count);
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
            Check("brawl: fighter and stage ids are unique", BrawlData.Fighters.Select(f => f.Id).Distinct().Count() == 10 && BrawlData.Stages.Select(s => s.Id).Distinct().Count() == 5, "");
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

        static void BrawlStageChecks()
        {
            // every stage has its own platforms: at least three to stand on besides the ground, each drawn its own way,
            // and no two stages laid out alike
            var notes = new List<string>(); bool ok = true;
            var layouts = new HashSet<string>();
            foreach (BStageDef s in BrawlData.Stages)
            {
                var soft = s.Plats.Where(p => !p.Solid).ToList();
                string looks = string.Join(",", soft.Select(p => p.Look).Distinct());
                notes.Add(s.Id + " " + soft.Count + " (" + looks + ")");
                if (soft.Count < 3 || soft.Any(p => p.Look == "")) ok = false;
                if (!layouts.Add(string.Join("|", soft.Select(p => p.X1 + "," + p.X2 + "," + p.Y)))) ok = false;
            }
            var allLooks = BrawlData.Stages.SelectMany(s => s.Plats.Where(p => !p.Solid).Select(p => s.Id + ":" + p.Look)).Select(x => x.Split(':')[1]).Distinct().ToList();
            bool shared = BrawlData.Stages.Any(a => BrawlData.Stages.Any(b => a != b && a.Plats.Any(p => !p.Solid && b.Plats.Any(q => !q.Solid && q.Look == p.Look))));
            Check("brawl stages: each stage has at least three platforms of its own kind (not one big and two small)", ok && !shared && allLooks.Count >= 10, string.Join("; ", notes));

            // the javelina stampede on the mesa: warned, two grown-ups and three babies, bowls over whoever stands in the
            // way, and someone who jumps over it is left alone
            Check("brawl stages: the herd is two grown-ups and three babies", BrawlSim.HerdAdult.Count(a => a) == 2 && BrawlSim.HerdAdult.Count(a => !a) == 3, "");
            BrawlSim m = null; bool seenWind = false, seenHerd = false;
            for (uint seed = 1; seed < 40 && (m == null || !seenWind); seed++)
            {
                var sim = new BrawlSim(BrawlData.Stages[0], new List<BFighterDef> { BrawlData.Fighters[2], BrawlData.Fighters[6] }, 3, 0, seed);
                var none = new BrInput[2];
                for (int i = 0; i < 60 * 60 * 3; i++)
                {
                    sim.Step(none);
                    foreach (BFighter f in sim.F) { f.Dmg = 0; f.X = f.Slot == 0 ? -60 : 60; }
                    if (sim.HazPhase > 0 && sim.Hazard == "wind") seenWind = true;
                    if (sim.HazPhase == 1 && sim.Hazard == "javelina") { seenHerd = true; if (m == null) { m = sim; break; } }
                }
            }
            Check("brawl stages: the mesa's surprises take turns (the monsoon wind and the javelinas)", seenWind && seenHerd, seenWind + " " + seenHerd);
            if (m == null) return;
            BFighter stand = m.F[0], hop = m.F[1];
            int dir = m.HazDir; bool warned = m.HazPhase == 1;
            stand.X = 0; hop.X = dir * 150;
            var inp = new BrInput[2];
            bool hopHit = false; int jumped = 0, air = 0, again = 0;
            for (int i = 0; i < 600 && (m.HazPhase != 0 || i < 5); i++)
            {
                double lead = m.HazX;
                // the second fighter jumps when the herd's leader is close (a full jump, held)
                bool close = m.HazPhase == 2 && (hop.X - lead) * dir > 0 && (hop.X - lead) * dir < 150;
                if (close && hop.Ground) { jumped = 12; again = 26; }
                // and again at the top, as a person would
                if (again > 0 && --again == 0) jumped = 12;
                inp[1] = new BrInput { Jump = jumped > 0 && (again == 0 || again > 14) };
                if (jumped > 0) jumped--;
                inp[0] = new BrInput();
                m.Step(inp);
                if (hop.Dmg > 0) hopHit = true;
                if (!hop.Ground) air++;
            }
            Check("brawl stages: the javelinas are warned first, bowl over someone standing in the way, and miss someone who jumps them",
                warned && stand.Dmg > 0 && !hopHit, warned + " stood " + stand.Dmg.ToString("0") + "%, jumped " + hop.Dmg.ToString("0") + "% (" + air + " frames in the air)");
        }

        /// <summary>The groups in this file, in App Hub's order.</summary>
        public static void BrawlEngineChecks()
        {
            BrawlDataChecks();
            BrawlPhysicsChecks();
            BrawlKnockbackChecks();
            BrawlRecoveryChecks();
            BrawlMechanicsChecks();
            BrawlMatchChecks();
            BrawlHazardChecks();
            BrawlStageChecks();
        }
    }
}
