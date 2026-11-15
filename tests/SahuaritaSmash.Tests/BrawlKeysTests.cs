// Classic Games - Mesa Mayhem's key checks (part of -Test, group "brawl"): changing a key swaps with whichever action had
// it, keys the game needs can't be taken, a saved line reads back (and an odd one falls back to the usual keys), the
// extra keys stop when a set uses them, and a match reads the person's own keys. Saving next to the scores is checked
// with the fixture in SaveTests.cs.
using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Input;

namespace ClassicGames
{
    public static partial class Tests
    {
        static void BrawlKeyChecks()
        {
            var k = new BrawlKeys();
            Check("brawl keys: the usual keys to start with", k.IsUsual && k.Set[0][BrawlKeys.Attack] == Key.F && k.Set[1][BrawlKeys.Jump] == Key.Up, k.Fields());

            k.Assign(0, BrawlKeys.Attack, Key.G);   // G was Strong: the two swap
            Check("brawl keys: giving a key another action has swaps them", k.Set[0][BrawlKeys.Attack] == Key.G && k.Set[0][BrawlKeys.Strong] == Key.F && !k.IsUsual, k.Fields());
            k.Assign(0, BrawlKeys.Grab, Key.K);     // K is the right set's attack
            Check("brawl keys: a key the other set uses swaps across the sets too", k.Set[0][BrawlKeys.Grab] == Key.K && k.Set[1][BrawlKeys.Attack] == Key.T, k.Fields());
            Key before = k.Set[0][BrawlKeys.Shield];
            foreach (Key bad in new[] { Key.Escape, Key.P, Key.M, Key.N, Key.Enter, Key.F3, Key.Tab, Key.LWin })
                k.Assign(0, BrawlKeys.Shield, bad);
            Check("brawl keys: pause, music, sounds, Enter and the like can't be taken", k.Set[0][BrawlKeys.Shield] == before, k.Fields());
            bool unique = k.Set[0].Concat(k.Set[1]).Distinct().Count() == BrawlKeys.Count * 2;
            Check("brawl keys: every action keeps its own key", unique, k.Fields());

            // a saved line reads back; odd names, keys the game needs and repeats fall back to the usual keys
            string[] f = k.Fields().Split('\t');
            var back = new BrawlKeys(); back.Read(f[0], f[1]);
            Check("brawl keys: the saved keys read back the same", back.Fields() == k.Fields(), back.Fields());
            back.Read("A,Nope,Escape,S,F,F,H", "");
            Check("brawl keys: an odd saved line falls back to the usual keys where it doesn't make sense", back.Set[0][1] == Key.D && back.Set[0][2] == Key.W && back.Set[0][BrawlKeys.Strong] == Key.G && back.Set[0][BrawlKeys.Grab] == Key.T && back.Set[1].SequenceEqual(BrawlKeys.RightUsual), back.Fields());
            Check("brawl keys: names a person reads", BrawlKeys.Name(Key.OemSemicolon) == ";" && BrawlKeys.Name(Key.D7) == "7" && BrawlKeys.Name(Key.NumPad3) == "num 3" && BrawlKeys.Name(Key.Q) == "Q", "");

            // the extras: Space jumps until a set uses it
            var e = new BrawlKeys();
            bool spaceBefore = e.Extra(Key.Space);
            e.Assign(1, BrawlKeys.Attack, Key.Space);
            Check("brawl keys: Space and the number pad are extras only while no set uses them", spaceBefore && !e.Extra(Key.Space) && e.Extra(Key.NumPad1), "");

            // a match reads the person's own keys (and an old key that is now free does nothing)
            var g = new BrawlGame { Player = "Kim", Rng = new Random(4) };
            g.Begin();
            g.Keys.Assign(0, BrawlKeys.Attack, Key.J);
            for (int s = 0; s < 4; s++) g.Who[s] = s == 0 ? 1 : s == 1 ? 2 : 0;
            g.Ladder = false; g.Autopilot = false; g.NewGame(); g.Step = BrawlGame.Stage.Fight;
            g.Press(Key.J);
            bool newKey = g.ReadForTest(0).Attack;
            g.KeyUp(Key.J); g.Update(1.0 / 60);
            g.Press(Key.F);
            BrInput old = g.ReadForTest(0);
            Check("brawl keys: in a match the changed key attacks and the old one doesn't", newKey && !old.Attack, newKey + " " + old.Attack);

            // the Controls screen: Enter waits for a key, the next key is taken (M too, even though M is the music key elsewhere)
            var c = new BrawlGame { Player = "Kim", Rng = new Random(4) };
            c.Begin();
            c.Menu = BrawlGame.Screen.Title; c.Sel = 3; c.Press(Key.Enter);
            bool onKeys = c.Menu == BrawlGame.Screen.Keys;
            c.Press(Key.Down); c.Press(Key.Enter);   // the left set's "Move right"
            bool waiting = c.KeyListening;
            c.Press(Key.M);                          // not allowed: still waiting, music not toggled
            bool stillWaiting = c.KeyListening && c.Keys.Set[0][BrawlKeys.MoveRight] == Key.D;
            c.Press(Key.L);
            Check("brawl keys: the Controls screen waits for a key, refuses the game's own keys and takes the next one", onKeys && waiting && stillWaiting && !c.KeyListening && c.Keys.Set[0][BrawlKeys.MoveRight] == Key.L && c.Keys.Set[1][BrawlKeys.Strong] == Key.D,
                onKeys + " " + waiting + " " + stillWaiting + " " + c.Keys.Fields());
            c.Press(Key.Escape);
            Check("brawl keys: Esc goes back to the menu on Controls", c.Menu == BrawlGame.Screen.Title && c.Sel == 3, c.Menu.ToString());
        }

        // the button buffer and the computer's shield (Andrew, 2026-10-29: Normal felt tough, "slow to react")
        static void BrawlBufferChecks()
        {
            // a tap while still landing is used as soon as the fighter can act, not lost
            BrawlSim sim = BrawlMatch(0, 3, 0, 9);
            BrawlRun(sim, null, 120);
            BFighter f = sim.F[0];
            f.St = BS.Land; f.T = 0; f.Lag = 6;
            var inp = new BrInput[2];
            inp[0] = new BrInput { Attack = true }; sim.Step(inp);
            inp[0] = new BrInput(); sim.Step(inp);
            bool busy = f.St == BS.Land;
            for (int i = 0; i < 6 && f.St != BS.Attack; i++) sim.Step(inp);
            Check("brawl buffer: an attack pressed while landing comes out when the landing ends", busy && f.St == BS.Attack && f.Mv != null && f.Mv.Id == "jab", f.St + " " + (f.Mv != null ? f.Mv.Id : ""));
            // ...but one pressed long before is forgotten
            sim = BrawlMatch(0, 3, 0, 9); BrawlRun(sim, null, 120); f = sim.F[0];
            f.St = BS.Land; f.T = 0; f.Lag = 30;
            inp[0] = new BrInput { Attack = true }; sim.Step(inp);
            inp[0] = new BrInput(); for (int i = 0; i < 40; i++) sim.Step(inp);
            Check("brawl buffer: a press from long ago is forgotten", f.St == BS.Idle, f.St.ToString());
            // a jump pressed during a jab comes out after it
            sim = BrawlMatch(0, 3, 0, 9); BrawlRun(sim, null, 120); f = sim.F[0];
            inp[0] = new BrInput { Attack = true }; sim.Step(inp);
            int len = f.Mv != null ? f.Mv.Len : 0;
            inp[0] = new BrInput(); for (int i = 0; i < Math.Max(0, len - 5); i++) sim.Step(inp);
            inp[0] = new BrInput { Jump = true }; sim.Step(inp);
            inp[0] = new BrInput(); bool jumped = false;
            for (int i = 0; i < 20 && !jumped; i++) { sim.Step(inp); jumped = f.St == BS.Squat || !f.Ground; }
            Check("brawl buffer: a jump pressed near the end of an attack still happens", len > 0 && jumped, len + " " + f.St);

            // the computer decides once per attack whether to shield it; Normal shields few
            var easy = new BrawlBot(0, BrawlGame.BotLevels[0]); var normal = new BrawlBot(0, BrawlGame.BotLevels[1]); var champ = new BrawlBot(0, BrawlGame.BotLevels[3]);
            Check("brawl bots: shield chances rise with the level and Normal shields under one attack in five", easy.ShieldChance < normal.ShieldChance && normal.ShieldChance < champ.ShieldChance && normal.ShieldChance <= 20 && champ.ShieldChance <= 60,
                easy.ShieldChance + " " + normal.ShieldChance + " " + champ.ShieldChance);
            // a person poking a Normal bot with jabs: it seldom shields them
            int shielded = 0, thrown = 0;
            for (uint seed = 1; seed <= 6; seed++)
            {
                sim = BrawlMatch(0, seed, 0, 9); var bots = new BrawlBot[] { null, new BrawlBot(1, BrawlGame.BotLevels[1]) };
                BrawlRun(sim, bots, 60);
                for (int round = 0; round < 12; round++)
                {
                    BFighter me = sim.F[0], bot = sim.F[1];
                    me.X = bot.X - 40; me.Face = 1; me.St = BS.Idle; me.Vx = 0; bot.St = BS.Idle; bot.Vx = 0; bot.Dmg = 0;
                    thrown++;
                    inp = new BrInput[2];
                    bool sh = false;
                    for (int i = 0; i < 40; i++) { inp[0] = new BrInput { Attack = i == 0 }; inp[1] = bots[1].Think(sim); sim.Step(inp); sh |= bot.St == BS.Shield; }
                    if (sh) shielded++;
                }
            }
            Check("brawl bots: on Normal, the computer shields only a few of the jabs thrown at it", shielded * 100 <= thrown * 30, shielded + " of " + thrown);

            // combos: a jab that lands cancels into the next attack as soon as its hit is over; a jab that misses doesn't
            foreach (bool near in new[] { true, false })
            {
                sim = BrawlMatch(0, 3, 2, 9); BrawlRun(sim, null, 120);
                BFighter a = sim.F[0], t = sim.F[1];
                a.X = 0; t.X = near ? 34 : 400; a.Face = 1; t.Face = -1;
                inp = new BrInput[2];
                inp[0] = new BrInput { Attack = true }; sim.Step(inp);
                BMove jab = a.Mv;
                string at = "";
                for (int i = 0; i < 40 && a.Mv == jab; i++)
                {
                    inp[0] = a.MF == jab.LastActive - 1 ? new BrInput { Attack = true, X = 1 } : new BrInput { X = a.MF >= jab.LastActive - 1 ? 1 : 0 };
                    sim.Step(inp);
                    if (a.Mv != jab) at = (a.Mv != null ? a.Mv.Id : a.St.ToString()) + " at frame " + i;
                }
                if (near) Check("brawl combos: a jab that lands goes straight into the next attack once its hit is over", jab != null && a.St == BS.Attack && a.Mv != null && a.Mv.Id == "ftilt" && a.MF <= 2 && t.Dmg > 0, at + ", jab " + (jab != null ? jab.LastActive + "/" + jab.Len : "?"));
                else Check("brawl combos: a jab that misses finishes first (the next press waits in the buffer)", a.Mv == null || a.Mv != jab, at);
            }
            // never more than MaxChain cancels in a row
            sim = BrawlMatch(0, 5, 2, 9); BrawlRun(sim, null, 120);
            {
                BFighter a = sim.F[0], t = sim.F[1];
                a.X = 0; t.X = 34; a.Face = 1;
                int most = 0;
                inp = new BrInput[2];
                for (int i = 0; i < 240; i++) { inp[0] = new BrInput { Attack = i % 3 == 0 }; inp[1] = new BrInput(); sim.Step(inp); most = Math.Max(most, a.Chain); t.X = Math.Min(t.X, a.X + 40); }
                Check("brawl combos: at most " + BrawlSim.MaxChain + " cancels in a row (nobody is locked in place)", most > 0 && most <= BrawlSim.MaxChain, most.ToString());
            }

            // signature attacks: every fighter's 13 have names, no two fighters share a set, names are unique within a fighter
            var missingNames = new List<string>(); var sets = new HashSet<string>();
            foreach (BFighterDef fd in BrawlData.Fighters)
            {
                var names = new List<string>();
                foreach (string id in BrawlSignatures.AttackIds) { BMove m = fd.M(id); if (m == null || string.IsNullOrEmpty(m.Name)) missingNames.Add(fd.Id + "." + id); else names.Add(m.Name); }
                if (names.Distinct().Count() != names.Count) missingNames.Add(fd.Id + " repeats a name");
                sets.Add(string.Join("|", names.ToArray()) + "/" + string.Join("|", BrawlSignatures.AttackIds.Select(id => fd.M(id).Anim + fd.M(id).Extra).ToArray()));
            }
            Check("brawl attacks: every fighter's 13 attacks have their own names, and every fighter's set is different", missingNames.Count == 0 && sets.Count == BrawlData.Fighters.Count, string.Join(", ", missingNames.Take(6).ToArray()));
            // a flurry (Bloom's Petal pat) lands more than once and still ends
            sim = BrawlMatch(0, 3, 0, 9); BrawlRun(sim, null, 120);
            {
                BFighter a = sim.F[0], t = sim.F[1];
                a.X = 0; t.X = 34; a.Face = 1;
                inp = new BrInput[2];
                inp[0] = new BrInput { Attack = true }; sim.Step(inp);
                int hits = 0, steps = 0; double last = t.Dmg;
                inp[0] = new BrInput();
                while (a.St == BS.Attack && steps < 120) { sim.Step(inp); steps++; if (t.Dmg > last) { hits++; last = t.Dmg; } }
                Check("brawl attacks: a flurry lands several times and then ends", hits >= 2 && steps < 60, hits + " hits, " + steps + " steps");
            }

            // the music in between: choosing fighters plays its own quiet tune; the results get a calm loop after the fanfare
            bool was = BrawlMusic.Enabled;
            try
            {
                BrawlMusic.Reset(); BrawlMusic.Enabled = false;
                var mg = new BrawlGame { Player = "Kim", Rng = new Random(2) };
                mg.Begin();
                for (int i = 0; i < 3; i++) mg.Update(1.0 / 60);
                bool title = Audio.Current == mg.Tunes.Theme;
                mg.Menu = BrawlGame.Screen.Select;
                for (int i = 0; i < 3; i++) mg.Update(1.0 / 60);
                bool select = Audio.Current == Music.BrawlFill(0);
                Check("brawl music: the title plays the menu song and choosing fighters its own quieter tune", title && select, title + " " + select);
                // pausing a fight plays the hold music; carrying on brings the stage's song back
                for (int s = 0; s < 4; s++) mg.Who[s] = s == 0 ? 1 : s == 1 ? 2 : 0;
                mg.StagePick = 0; mg.Ladder = false; mg.NewGame();
                for (int i = 0; i < 3; i++) mg.Update(1.0 / 60);
                Song stage = Audio.Current;
                mg.Pause();
                bool hold = Audio.Current == Music.BrawlFill(3);
                mg.Resume();
                Check("brawl music: pausing plays the hold music, carrying on brings the stage's song back", stage == Music.BrawlSong(0) && hold && Audio.Current == stage, (stage != null ? stage.Name : "none") + " " + hold + " " + (Audio.Current != null ? Audio.Current.Name : "none"));
                Check("brawl music: the in-between tunes are original and quieter than the menu song", Music.BrawlFill(0).Gain < mg.Tunes.Theme.Gain && Music.BrawlFill(1).Gain < mg.Tunes.Theme.Gain && Music.BrawlFill(1).Loop && !Music.BrawlFill(2).Loop, "");
            }
            finally { BrawlMusic.Reset(); BrawlMusic.Enabled = was; Audio.StopMusic(); }
        }

        /// <summary>Each fighter moves its own way when it hits (BrawlMotion.cs): every fighter has a style, the same
        /// attack picture ends in a different strike for every two fighters, and every frame of every attack is a sane pose.</summary>
        static void BrawlMotionChecks()
        {
            var fs = BrawlData.Fighters;
            int styled = fs.Count(d => BrawlView.MotionOf(d.Id) != BrawlView.MotionOf("nobody"));
            Check("brawl motion: every fighter has its own way of moving", styled == fs.Count, styled + " of " + fs.Count);
            string same = "";
            foreach (string anim in new[] { "jab", "punch", "swing", "bigswing", "kick", "uppercut", "lash", "sweep" })
                for (int i = 0; i < fs.Count; i++)
                    for (int j = i + 1; j < fs.Count; j++)
                    {
                        BPose wa, sa, wb, sb;
                        BrawlView.StyledPoses(fs[i].Id, anim, false, out wa, out sa);
                        BrawlView.StyledPoses(fs[j].Id, anim, false, out wb, out sb);
                        double d = Math.Abs(sa.Lean - sb.Lean) + Math.Abs(sa.Crouch - sb.Crouch) * 100 + Math.Abs(sa.FA1 - sb.FA1) + Math.Abs(sa.FA2 - sb.FA2)
                            + Math.Abs(sa.BA1 - sb.BA1) + Math.Abs(sa.BA2 - sb.BA2) + Math.Abs(sa.FL1 - sb.FL1) + Math.Abs(sa.BL1 - sb.BL1) + Math.Abs(sa.Head - sb.Head);
                        if (d < 15 && same.Length < 200) same += anim + ": " + fs[i].Id + "/" + fs[j].Id + " ";
                    }
            Check("brawl motion: the same attack looks different for every two fighters", same == "", same);
            string bad = "";
            foreach (BFighterDef d in fs)
                foreach (BMove m in d.Moves.Values)
                    for (int fr = 0; fr <= m.Len; fr++)
                    {
                        bool air = m.Id.EndsWith("air");
                        var f = new BFighter { D = d, St = BS.Attack, Mv = m, MF = fr, Face = 1, Ground = !air, AirMove = air };
                        BPose p = BrawlView.Pose(f, fr / 60.0);
                        double sum = p.Lean + p.Crouch + p.FA1 + p.FA2 + p.BA1 + p.BA2 + p.FL1 + p.FL2 + p.BL1 + p.BL2 + p.Spin + p.Head;
                        if ((double.IsNaN(sum) || double.IsInfinity(sum) || p.Crouch > 1.2 || p.Crouch < -0.2) && bad.Length < 200) bad += d.Id + " " + m.Id + "@" + fr + " ";
                    }
            Check("brawl motion: every frame of every attack is a sane pose", bad == "", bad);
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

            // the orchard's pecans: the shaker warns for the full 1.5 s with every nut's spot already shown, then a fighter
            // standing on a spot gets a small bonk and one standing clear of every spot is left alone
            int orchard = BrawlData.StageIndex("orchard");
            var pm = new BrawlSim(BrawlData.Stages[orchard], new List<BFighterDef> { BrawlData.Fighter("gila"), BrawlData.Fighter("ben") }, 3, 0, 11);
            var quiet = new BrInput[2];
            int warnFrames = 0; double[] spots = null;
            for (int i = 0; i < 60 * 60 && pm.HazPhase != 2; i++)
            {
                pm.Step(quiet);
                foreach (BFighter f in pm.F) { f.Dmg = 0; f.X = f.Slot == 0 ? -300 : 300; }
                if (pm.HazPhase == 1) { warnFrames++; if (spots == null) spots = (double[])pm.Nuts.Clone(); }
            }
            bool sameSpots = spots != null && spots.SequenceEqual(pm.Nuts);
            double clear = pm.Main.L + 5;
            foreach (double n in pm.Nuts) if (Math.Abs(n - clear) < 120) clear = pm.Main.R - 5;
            pm.F[0].X = pm.Nuts[0]; pm.F[1].X = clear;
            int landed = 0;
            for (int i = 0; i < 200 && pm.HazPhase == 2; i++) { pm.Step(quiet); foreach (BrEvent e in pm.Events) if (e.K == "sfx" && e.Name == "brawl-pecan") landed++; if (i < 20) { pm.F[0].X = pm.Nuts[0]; pm.F[1].X = clear; } }
            Check("brawl stages: the orchard's pecans are warned 1.5 s with their spots shown, all fall, bonk someone under one and miss someone clear",
                warnFrames >= BrawlSim.HazWarn && sameSpots && landed == pm.Nuts.Length && pm.F[0].Dmg > 0 && pm.F[1].Dmg == 0, warnFrames + " frames, " + landed + " landed, under " + pm.F[0].Dmg.ToString("0") + "%, clear " + pm.F[1].Dmg.ToString("0") + "%");
        }
    }
}
