// Classic Games - Sahuarita Smash!, part 3: the computer players. A bot only presses buttons (a BrInput each step), so it
// plays by exactly the same rules as a person. Level 1-9: how quickly it reacts, how often it shields, how well it
// spaces its attacks and how wisely it comes back to the stage. It always tries to come home: jumps, then its
// up special toward the ledge, and never walks off on purpose. Its choices use the match's own randomness (BrawlSim's),
// so a match with bots is the same every time from the same seed.
using System;
using System.Collections.Generic;

namespace ClassicGames
{
    public sealed class BrawlBot
    {
        public readonly int Slot;
        public int Level;
        int wait, holdT, target = -1, retarget;
        BrInput plan;
        string planMove = "";
        /// <summary>Tests: a bot that only comes home (no fighting).</summary>
        public bool RecoverOnly;

        public BrawlBot(int slot, int level) { Slot = slot; Level = Math.Max(1, Math.Min(9, level)); }

        int React { get { return Math.Max(2, 26 - Level * 3); } }
        /// <summary>How often (in 100) it shields an attack it sees coming: Easy (2) 6, Normal (4) 16, Hard (7) 38, Champion (9) 52.</summary>
        public int ShieldChance { get { return Level * Level / 2 + Level * 2 - (Level >= 9 ? 6 : 0); } }
        int seenSlot = -1; BMove seenMove; long seenStart; bool seenShield;

        public BrInput Think(BrawlSim sim)
        {
            BFighter me = sim.F[Slot];
            var i = new BrInput();
            if (!me.Playing) return i;
            if (me.St == BS.Enter) { if (me.T > 40 + sim.Rand(40)) i.X = me.X < 0 ? 1 : -1; return i; }
            BPlat main = sim.Main;
            bool off = me.X < main.L - 6 || me.X > main.R + 6 || me.Y < main.Top - 10;
            if (me.St == BS.Ledge) return Ledge(sim, me);
            if (me.St == BS.Grabbed) { if (sim.Frame % Math.Max(2, 10 - Level) == 0) i.Attack = true; i.X = sim.Frame % 8 < 4 ? 1 : -1; return i; }
            if (me.St == BS.Grabbing) return Throw(sim, me);
            if (off && !me.Ground) return Recover(sim, me);
            if (RecoverOnly) { if (me.Ground) i.X = Math.Abs(me.X) > 40 ? (me.X > 0 ? -1 : 1) : 0; return i; }
            // a javelina herd coming: jump it (better players see it sooner)
            if (me.Ground && HerdComing(sim, me)) { i.Jump = true; Hold(i, 12, "jump"); return i; }
            // keep doing what was planned for a few steps (a held direction, the button on the right frame)
            if (holdT > 0)
            {
                // buttons were pressed on the first step; keep the direction (and a jump or a charging strong attack held)
                holdT--; BrInput h = plan;
                h.Attack = h.Special = h.Grab = false;
                if (planMove != "jump") h.Jump = false;
                if (!planMove.EndsWith("strong")) h.Strong = false;
                return Safe(sim, me, h);
            }
            if (wait > 0) { wait--; return Safe(sim, me, Walk(sim, me)); }
            if (--retarget <= 0 || target < 0 || !sim.F[target].Playing) { target = Pick(sim, me); retarget = 90 + sim.Rand(90); }
            if (target < 0) return i;
            BFighter t = sim.F[target];
            double dx = t.X - me.X, dy = t.Y - me.Y, adx = Math.Abs(dx);
            int face = dx >= 0 ? 1 : -1;

            // defend: an attack coming my way (better players see it more often). Decided once per attack: rolling the dice
            // every step of the windup meant Normal shielded nearly everything (Andrew, 2026-10-29: "normal mode is pretty
            // tough")
            bool seesIt = false;
            if (t.St == BS.Attack && t.Mv != null)
            {
                long start = sim.Frame - t.MF;
                if (t.Slot != seenSlot || t.Mv != seenMove || start != seenStart) { seenSlot = t.Slot; seenMove = t.Mv; seenStart = start; seenShield = sim.Rand(100) < ShieldChance; }
                seesIt = seenShield;
            }
            if (me.Ground && seesIt && t.MF < t.Mv.FirstActive && adx < t.Mv.Reach + 60)
            {
                i.Shield = true; wait = 6 + sim.Rand(10);
                if (Level >= 6 && sim.Rand(3) == 0) { i.X = -face; }
                return i;
            }
            if (me.St == BS.Shield)
            {
                if (t.St == BS.Attack && t.Mv != null && t.MF < t.Mv.Len) { i.Shield = true; if (adx < 70 && t.MF > t.Mv.FirstActive + 2 && Level >= 4) { i.Attack = true; } return i; }
                return i;
            }
            // a shielding target gets grabbed
            if (me.Ground && t.St == BS.Shield && adx < 70 * me.D.Scale && sim.Rand(100) < 20 + Level * 6) { i.Grab = true; Hold(i, 6, "grab"); return i; }

            string move = Choose(sim, me, t, dx, dy);
            if (move != null) return Safe(sim, me, Press(sim, me, move, face));
            return Safe(sim, me, Approach(sim, me, t, dx, dy));
        }

        int Pick(BrawlSim sim, BFighter me)
        {
            int best = -1; double bd = double.MaxValue;
            foreach (BFighter f in sim.F)
            {
                if (f == me || !f.Playing || f.St == BS.Enter) continue;
                double d = Math.Abs(f.X - me.X) + Math.Abs(f.Y - me.Y) * 0.6 - f.Dmg * 0.4 + sim.Rand(120);
                if (d < bd) { bd = d; best = f.Slot; }
            }
            return best;
        }

        void Hold(BrInput i, int frames, string move) { plan = i; holdT = frames; planMove = move; }

        bool HerdComing(BrawlSim sim, BFighter me)
        {
            if (sim.Hazard != "javelina" || sim.HazPhase != 2 || me.On != sim.Main) return false;
            double look = 40 + Level * 12;
            for (int k = 0; k < BrawlSim.HerdBack.Length; k++)
            {
                double ahead = (me.X - (sim.HazX - sim.HazDir * BrawlSim.HerdBack[k])) * sim.HazDir;
                if (ahead > 20 && ahead < look + 40) return true;
            }
            return false;
        }

        BrInput Walk(BrawlSim sim, BFighter me)
        {
            var i = new BrInput();
            if (target >= 0 && sim.F[target].Playing)
            {
                double dx = sim.F[target].X - me.X;
                if (Math.Abs(dx) > 120) i.X = dx > 0 ? 0.6 : -0.6;
            }
            return i;
        }

        /// <summary>Which move would land from here (or null to keep moving).</summary>
        string Choose(BrawlSim sim, BFighter me, BFighter t, double dx, double dy)
        {
            double adx = Math.Abs(dx);
            double k = me.D.Scale * me.Size;
            bool tAir = !t.Ground;
            bool canAct = me.St == BS.Idle || me.St == BS.Walk || me.St == BS.Run || me.St == BS.Crouch || me.St == BS.Air;
            if (!canAct) return null;
            if (sim.Rand(100) > 30 + Level * 7) return null;   // not every chance is taken
            if (me.Ground)
            {
                // a shot from afar
                if (adx > 200 && adx < 520 && Math.Abs(dy) < 120 && me.D.M("nspec").Shot != null && sim.Rand(100) < 18) return "nspec";
                if (adx > 160 && adx < 420 && me.D.M("sspec").Shot != null && sim.Rand(100) < 10) return "sspec";
                if (adx < 50 * k + 30 && dy > 40 && dy < 160) return sim.Rand(3) == 0 ? "ustrong" : "utilt";
                if (adx < 60 * k + 40 && Math.Abs(dy) < 50)
                {
                    int r = sim.Rand(100);
                    bool kill = t.Dmg > 85 + me.D.Weight * 0.2;
                    if (kill && r < 55) return sim.Rand(3) == 0 ? "dstrong" : "fstrong";
                    if (r < 25) return "jab";
                    if (r < 50) return "ftilt";
                    if (r < 62) return "dtilt";
                    if (r < 72) return "grab";
                    if (r < 82) return "dspec";
                    return "fstrong";
                }
                if (me.St == BS.Run && adx < 110 * k && Math.Abs(dy) < 40 && sim.Rand(4) == 0) return "dash";
                if (tAir && adx < 90 && dy > 60 && dy < 260 && sim.Rand(3) == 0) return "jump";
                return null;
            }
            // in the air
            if (adx < 70 * k && Math.Abs(dy) < 70) return (dx >= 0 ? 1 : -1) == me.Face ? (sim.Rand(2) == 0 ? "fair" : "nair") : "bair";
            if (adx < 60 && dy > 40 && dy < 150) return "uair";
            if (adx < 60 && dy < -30 && dy > -150) return "dair";
            return null;
        }

        BrInput Press(BrawlSim sim, BFighter me, string move, int face)
        {
            var i = new BrInput();
            switch (move)
            {
                case "jab": i.Attack = true; break;
                case "ftilt": i.Attack = true; i.X = face; break;
                case "utilt": i.Attack = true; i.Y = 1; break;
                case "dtilt": i.Attack = true; i.Y = -1; break;
                case "dash": i.Attack = true; i.X = face; break;
                case "fstrong": i.Strong = true; i.X = face; break;
                case "ustrong": i.Strong = true; i.Y = 1; break;
                case "dstrong": i.Strong = true; i.Y = -1; break;
                case "grab": i.Grab = true; break;
                case "nspec": if (me.Face != face) { i.X = face; Hold(i, 1, "turn"); return i; } i.Special = true; break;
                case "sspec": i.Special = true; i.X = face; break;
                case "dspec": i.Special = true; i.Y = -1; break;
                case "jump": i.Jump = true; i.X = face * 0.5; Hold(i, 8, "jump"); return i;
                case "fair": i.Attack = true; i.X = me.Face; break;
                case "bair": i.Attack = true; i.X = -me.Face; break;
                case "nair": i.Attack = true; break;
                case "uair": i.Attack = true; i.Y = 1; break;
                case "dair": i.Attack = true; i.Y = -1; break;
            }
            wait = Math.Max(2, React / 2 + sim.Rand(React));
            if (move.EndsWith("strong")) { Hold(i, 4 + (Level >= 5 ? sim.Rand(30) : 0), move); }
            return i;
        }

        BrInput Approach(BrawlSim sim, BFighter me, BFighter t, double dx, double dy)
        {
            var i = new BrInput();
            double adx = Math.Abs(dx);
            if (adx > 40) i.X = dx > 0 ? 1 : -1;
            if (me.Ground)
            {
                // the target is up on a platform: jump to it; below on a thin one: drop down to it
                if (dy > 70 && adx < 220 && sim.Rand(100) < 12) { i.Jump = true; Hold(i, 8, "jump"); }
                else if (dy < -60 && adx < 160 && me.On != null && !me.On.Solid && sim.Rand(100) < 8) { i.X = 0; i.Y = -1; }
            }
            else
            {
                if (dy > 60 && me.Vy < 0 && me.Jumps > 0 && sim.Rand(100) < 15) i.Jump = true;
                if (dy < -80 && me.Vy < 2 && sim.Rand(100) < 10) i.Y = -1;   // fast fall onto them
            }
            return i;
        }

        /// <summary>Never walk or drift off the stage on purpose.</summary>
        BrInput Safe(BrawlSim sim, BFighter me, BrInput i)
        {
            BPlat main = sim.Main;
            double margin = 40 + me.HalfW;
            // on the ground, or on a platform reaching out near the edge (the copper pit's terraces, the lab's tanks)
            if (me.Ground && me.On != null && (me.On == main || me.On.L < main.L + margin || me.On.R > main.R - margin))
            {
                if (i.X < 0 && me.X < main.L + margin) i.X = 0;
                if (i.X > 0 && me.X > main.R - margin) i.X = 0;
            }
            if (!me.Ground)
            {
                if (me.X < main.L + 20 && i.X <= 0 && me.Y < main.Top + 200) i.X = 1;
                if (me.X > main.R - 20 && i.X >= 0 && me.Y < main.Top + 200) i.X = -1;
            }
            return i;
        }

        // ---------------------------------------------------------------- coming home
        BrInput Recover(BrawlSim sim, BFighter me)
        {
            var i = new BrInput();
            BPlat main = sim.Main;
            int side = me.X < 0 ? -1 : 1;
            double ledgeX = side < 0 ? main.L : main.R;
            double dx = ledgeX - me.X, adx = Math.Abs(dx);
            double below = main.Top - me.Y;   // how far under the stage top the feet are (positive = under)
            i.X = dx > 0 ? 1 : -1;
            if (me.St == BS.Hurt || me.St == BS.Helpless || me.St == BS.AirDodge) return i;
            if (me.St == BS.Attack) { if (me.Mv != null && me.Mv.Kind == "rise") { i.X = dx > 0 ? 1 : -1; } return i; }
            BMove up = me.D.M("uspec");
            double rise = UpReach(sim, me, up);
            // jump when falling and still far, keeping the up special for last
            if (me.Jumps > 0 && me.Vy < 0.5 && (below > -40 || adx > 160)) { i.Jump = true; return i; }
            // the side special covers distance if it moves (once in the air)
            BMove side_ = me.D.M("sspec");
            if (!me.SideUsed && side_ != null && side_.Kind == "lunge" && adx > 160 && below < 60 && me.Vy < 0 && me.Jumps == 0) { i.Special = true; i.X = dx > 0 ? 1 : -1; return i; }
            // up special when the ledge is in reach (or there is nothing else left)
            if (!me.UpUsed && me.Jumps == 0 && me.Vy < 0.5 && (below > -rise * 0.35 || me.Y < main.Top - 60))
            {
                i.Special = true; i.Y = 1; i.X = dx > 0 ? 0.4 : -0.4; return i;
            }
            if (!me.UpUsed && me.Y < main.Top - rise * 0.75) { i.Special = true; i.Y = 1; return i; }
            return i;
        }

        /// <summary>About how high an up special carries this fighter.</summary>
        static double UpReach(BrawlSim sim, BFighter me, BMove up)
        {
            if (up == null) return 0;
            return Math.Max(0, up.VTo - up.VFrom) * up.Vy * Math.Sqrt(sim.Stage.Gravity);
        }

        BrInput Ledge(BrawlSim sim, BFighter me)
        {
            var i = new BrInput();
            if (me.T < 10 + React) return i;
            int r = sim.Rand(4);
            if (r == 0) i.Jump = true;
            else if (r == 1 && Level >= 3) i.Attack = true;
            else if (r == 2 && Level >= 5) i.Shield = true;
            else i.Y = 1;
            return i;
        }

        BrInput Throw(BrawlSim sim, BFighter me)
        {
            var i = new BrInput();
            if (me.T < 8 + sim.Rand(10)) { if (sim.Rand(3) == 0) i.Attack = true; return i; }
            BPlat main = sim.Main;
            // throw toward the nearer edge, or up when they are hurt enough
            BFighter v = me.Victim >= 0 ? sim.F[me.Victim] : null;
            if (v != null && v.Dmg > 110 && sim.Rand(2) == 0) { i.Y = 1; return i; }
            int toward = me.X < 0 ? -1 : 1;
            i.X = toward;
            return i;
        }
    }
}
