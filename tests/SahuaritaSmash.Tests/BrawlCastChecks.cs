// Sahuarita Smash! (Sauce Edition) - checks for Pecos Bill and Monsoon Wash (group brawl-cast): his four specials do
// what their names say (the lasso reaches far, the pull grabs and throws, the twister lifts him and hits a few times,
// the howl pushes both sides away), and the wash's flash flood is warned 1.5 s first, bonks someone standing on the
// ground once (lightly), carries them along without washing them over the edge, and misses anyone on a platform or in
// the air. The computer players jump it.
using System;
using System.Collections.Generic;
using System.Linq;

namespace ClassicGames
{
    public static partial class Tests
    {
        static void BrawlCastChecks()
        {
            BFighterDef pecos = BrawlData.Fighter("pecos"), trooper = BrawlData.Fighter("trooper");
            Check("brawl cast: Pecos Bill is a legend with four named specials and a long lasso", pecos != null && pecos.Group == BrawlData.Groups[4] && pecos.SpecialNames.All(n => !string.IsNullOrEmpty(n)) && pecos.M("nspec").Reach >= 120,
                pecos == null ? "missing" : pecos.M("nspec").Reach.ToString("0"));
            if (pecos == null) return;
            BStageDef wash = BrawlData.Stages[BrawlData.StageIndex("wash")];

            // runs one special against a trooper standing dx in front (dy up); returns the sim after it ends
            Func<string, BrInput, double, double, BrawlSim> special = (id, press, dx, dy) =>
            {
                var sim = new BrawlSim(wash, new List<BFighterDef> { pecos, trooper }, 3, 0, 7) { Surprises = false };
                BFighter p = sim.F[0], t = sim.F[1];
                var inp = new BrInput[2];
                for (int i = 0; i < 30; i++) sim.Step(inp);
                p.X = 0; p.Face = 1; t.X = dx; t.Face = -1;
                if (dy > 0) { t.Y = dy; t.Ground = false; t.On = null; t.St = BS.Air; t.Vy = 0; }
                for (int i = 0; i < 90; i++)
                {
                    inp[0] = i == 0 ? press : new BrInput();
                    if (dy > 0 && t.St == BS.Air) { t.Y = Math.Max(t.Y, dy); t.Vy = 0; t.X = dx; }
                    sim.Step(inp);
                }
                return sim;
            };
            BrawlSim s1 = special("nspec", new BrInput { Special = true }, 115, 0);
            Check("brawl cast: Pecos Bill's lasso ropes someone from far across the stage", s1.F[1].Dmg > 0, s1.F[1].Dmg.ToString("0"));
            // the pull: someone far away is grabbed, pulled in and thrown forward
            {
                var sim = new BrawlSim(wash, new List<BFighterDef> { pecos, trooper }, 3, 0, 7) { Surprises = false };
                BFighter p = sim.F[0], t = sim.F[1];
                var inp = new BrInput[2];
                for (int i = 0; i < 30; i++) sim.Step(inp);
                p.X = 0; p.Face = 1; t.X = 110;
                bool grabbed = false; double closest = 999;
                for (int i = 0; i < 90; i++) { inp[0] = i == 0 ? new BrInput { Special = true, X = 1 } : new BrInput(); sim.Step(inp); if (t.St == BS.Grabbed) { grabbed = true; closest = Math.Min(closest, Math.Abs(t.X - p.X)); } }
                Check("brawl cast: the lasso pull grabs someone far away, pulls them in and throws them", grabbed && closest < 80 && t.Dmg >= 10, grabbed + " " + closest.ToString("0") + " " + t.Dmg.ToString("0"));
            }
            // the twister: up he goes, and someone just above gets hit a few times
            {
                var sim = new BrawlSim(wash, new List<BFighterDef> { pecos, trooper }, 3, 0, 7) { Surprises = false };
                BFighter p = sim.F[0], t = sim.F[1];
                var inp = new BrInput[2];
                for (int i = 0; i < 30; i++) sim.Step(inp);
                p.X = 0; p.Face = 1; t.X = 20; t.Y = 60; t.Ground = false; t.On = null; t.St = BS.Air;
                double y0 = p.Y, top = p.Y; int hits = 0;
                for (int i = 0; i < 60; i++)
                {
                    inp[0] = i == 0 ? new BrInput { Special = true, Y = 1 } : new BrInput();
                    if (i < 30 && t.St == BS.Air) { t.X = p.X + 15; t.Y = p.Y + 30; t.Vy = 0; t.Vx = 0; }
                    sim.Step(inp); top = Math.Max(top, p.Y);
                    foreach (BrEvent e in sim.Events) if (e.K == "hit" && e.S == 1) hits++;
                }
                Check("brawl cast: riding the twister lifts Pecos Bill high and hits a few times on the way", top - y0 > 150 && hits >= 3, (top - y0).ToString("0") + " up, " + hits + " hits");
            }
            // the howl: pushes someone away on either side
            BrawlSim s4 = special("dspec", new BrInput { Special = true, Y = -1 }, 50, 0);
            BrawlSim s5 = special("dspec", new BrInput { Special = true, Y = -1 }, -50, 0);
            Check("brawl cast: the coyote howl pushes away someone on either side", s4.F[1].Dmg > 0 && s5.F[1].Dmg > 0 && s4.F[1].X > 60 && s5.F[1].X < -60,
                s4.F[1].Dmg.ToString("0") + "% " + s4.F[1].X.ToString("0") + ", " + s5.F[1].Dmg.ToString("0") + "% " + s5.F[1].X.ToString("0"));

            // ---- the flood
            {
                var sim = new BrawlSim(wash, new List<BFighterDef> { trooper, BrawlData.Fighter("ben"), pecos, BrawlData.Fighter("gila") }, 3, 0, 13);
                var quiet = new BrInput[4];
                BPlat bridge = sim.Plats.First(p => p.Look == "bridge");
                int warn = 0, dir = 0;
                for (int i = 0; i < 60 * 60 && sim.HazPhase != 2; i++)
                {
                    if (sim.HazPhase == 1 && warn == 0)
                    {
                        dir = sim.HazDir;
                        BFighter b = sim.F[1]; b.X = (bridge.L + bridge.R) / 2; b.Y = bridge.Top + 4; b.Ground = false; b.On = null; b.St = BS.Air; b.Vy = 0;
                    }
                    if (sim.HazPhase == 1) warn++;
                    sim.Step(quiet);
                    foreach (BFighter f in sim.F) f.Dmg = 0;
                    if (dir != 0) { sim.F[0].X = -dir * 200; sim.F[2].X = dir * 60; sim.F[3].X = dir * (sim.Main.R - 160); }
                }
                BFighter stand = sim.F[0], high = sim.F[1], hop = sim.F[2], carried = sim.F[3];
                bool onBridge = high.Ground && high.On == bridge;
                double carriedFrom = carried.X;
                int splashes = 0, air = 0;
                for (int i = 0; i < 600 && sim.HazPhase == 2; i++)
                {
                    var inp = new BrInput[4];
                    double ahead = (hop.X - sim.HazX) * sim.HazDir;
                    if (hop.Ground && ahead > 0 && ahead < 150) inp[2] = new BrInput { Jump = true };
                    else if (!hop.Ground) inp[2] = new BrInput { Jump = true };
                    carried.Invuln = 200;
                    sim.Step(inp);
                    if (!hop.Ground) air++;
                    foreach (BrEvent e in sim.Events) if (e.K == "sfx" && e.Name == "brawl-splash") splashes++;
                }
                bool carriedOk = (carried.X - carriedFrom) * dir > 60 && carried.X > sim.Main.L + 20 && carried.X < sim.Main.R - 20 && carried.Stocks == 3;
                Check("brawl stages: the wash's flood is warned 1.5 s, bonks someone standing in it, carries them along (never over the edge) and misses someone on the bridge or in the air",
                    warn >= BrawlSim.HazWarn && onBridge && stand.Dmg > 0 && stand.Dmg < 10 && high.Dmg == 0 && hop.Dmg == 0 && carriedOk && sim.HazPhase == 0,
                    warn + " warned, bridge " + onBridge + ", stood " + stand.Dmg.ToString("0") + "%, high " + high.Dmg.ToString("0") + "%, jumped " + hop.Dmg.ToString("0") + "% (" + air + " in the air), carried " + carriedFrom.ToString("0") + " -> " + carried.X.ToString("0") + ", " + splashes + " splashes");
            }
            // the computer players jump the flood: over many floods, bots on the ground are bonked by few of them
            {
                int floods = 0, bonks = 0, seen = 0;
                for (uint seed = 1; seed <= 3; seed++)
                {
                    var sim = new BrawlSim(wash, new List<BFighterDef> { trooper, pecos }, 99, 0, seed);
                    var bots = new[] { new BrawlBot(0, 9), new BrawlBot(1, 9) };
                    var inp = new BrInput[2];
                    int lastPhase = 0;
                    for (int i = 0; i < 60 * 120; i++)
                    {
                        if (sim.HazPhase == 2 && lastPhase != 2) floods++;
                        lastPhase = sim.HazPhase;
                        for (int s = 0; s < 2; s++) inp[s] = bots[s].Think(sim);
                        sim.Step(inp);
                        foreach (BrEvent e in sim.Events) if (e.K == "sfx" && e.Name == "brawl-splash") bonks++;
                        if (sim.HazPhase == 2) seen++;
                    }
                }
                Check("brawl stages: the computer players jump the flood most of the time", floods >= 3 && bonks <= floods, floods + " floods, " + bonks + " bonks");
            }
        }
    }
}
