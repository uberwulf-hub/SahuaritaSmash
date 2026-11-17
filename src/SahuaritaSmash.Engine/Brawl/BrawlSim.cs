// Classic Games - Sahuarita Smash!, part 2: the match. BrawlSim.Step is the only place a match changes: 60 steps a second,
// from each fighter's buttons (BrInput), with its own seeded randomness (the stage surprises), so the same buttons
// always give the same match (tests, and network play later). Fighters: standing, walking, running, crouching, jumping
// (three-frame squat; let go early for a short hop), air jumps, attacks, the four specials, shield (it shrinks as it
// takes hits and breaks into a dizzy spell), rolls and dodges, grabs and throws, ledges (hang, climb, jump, drop),
// knockback that grows with damage and falls off with weight, hitstun, tumbling, being knocked down, and losing a life
// past the stage's edges. The pictures are BrawlArt.cs, the menus and the screen Brawl.cs, the computer players
// BrawlBot.cs.
using System;
using System.Collections.Generic;

namespace ClassicGames
{
    public struct BrInput
    {
        /// <summary>Stick or keys: -1..1, Y up.</summary>
        public double X, Y;
        public bool Jump, Attack, Strong, Special, Shield, Grab;
        public bool Any { get { return Jump || Attack || Strong || Special || Shield || Grab || Math.Abs(X) > 0.5 || Math.Abs(Y) > 0.5; } }
    }

    public enum BS { Idle, Walk, Run, Crouch, Squat, Air, Land, Attack, Shield, ShieldStun, ShieldDrop, Roll, Dodge, AirDodge, Hurt, Helpless, Ledge, LedgeUp, Grabbing, Grabbed, Dizzy, Down, Out, Enter }

    public sealed class BFighter
    {
        public int Slot;
        public BFighterDef D;
        public double X, Y, Vx, Vy, Lx, Ly;
        public int Face = 1;
        public BS St = BS.Air;
        public int T, Lag;
        public BMove Mv; public int MF, Charge; public bool AirMove;
        public double Dmg; public int Stocks;
        public int Jumps; public bool Ground; public BPlat On;
        public int Hitlag, Hitstun, Invuln; public bool Tumble;
        public double ShieldHp = 50;
        public bool UpUsed, SideUsed, AirDodged, FastFall;
        public int DropT, LedgeCool, RunT;
        public BPlat LedgePlat; public int LedgeSide; public string LedgeNext = "";
        public int HoldT, Grabber = -1, Victim = -1, PullT;
        public int Last = -1, LastT = 9999;
        public int KOs, Falls, SDs; public double Done, Taken; public int Place;
        public BrInput In, Prev;
        /// <summary>Buttons pressed in the last few steps that haven't been used yet (BrawlSim.Buffer): attack, strong,
        /// special, grab, shield, jump.</summary>
        public readonly int[] Buf = new int[6];
        /// <summary>This attack has landed on someone (combos may cancel it), and how many cancels in a row so far.</summary>
        public bool Connected; public int Chain;
        public readonly int[] HitAt = new int[32];   // [target * 4 + hit group]: the move frame (MF) it last landed: counted in the move's own frames, so a hit's pause can't stop a flurry from moving on
        /// <summary>The ladder's giant: bigger and heavier.</summary>
        public double Size = 1;
        public double H { get { return D.Height * Size; } }
        public double HalfW { get { return D.Width * Size / 2; } }
        public double Weight { get { return D.Weight * (Size > 1 ? 1 + (Size - 1) * 0.8 : 1); } }
        public bool Playing { get { return St != BS.Out; } }
        public bool Out { get { return St == BS.Out; } }
        /// <summary>The percentage shown, as a whole number.</summary>
        public int Percent { get { return (int)Math.Floor(Dmg); } }
        public int Score { get { return KOs - Falls - SDs; } }
    }

    public sealed class BShot
    {
        public bool On; public int Owner; public BShotDef D;
        public double X, Y, Vx, Vy; public int Age, Face;
        public bool Back, Landed;
        public readonly int[] HitAt = new int[8];
    }

    public struct BrEvent
    {
        public string K, Name;
        public double X, Y, A;
        public int S;
    }

    public sealed class BrawlSim
    {
        public const double KbSpeed = 0.112, LaunchDecay = 0.3;
        public const int MaxShots = 64;
        /// <summary>How far past a platform's end you still stand on it.</summary>
        const double Edge = 4;
        public BStageDef Stage;
        public readonly List<BPlat> Plats = new List<BPlat>();
        public BFighter[] F;
        public int Frame;
        public bool TimeMode, Surprises = true, Sudden;
        /// <summary>The practice room (BrawlPractice.cs): nobody loses a life, falling off brings you straight back, the
        /// match never ends. Off in every other match, which it leaves exactly as before.</summary>
        public bool Practice;
        public int TimeLeft, StartLives = 3;
        public bool Over; public int Winner = -1; public int OverT;
        /// <summary>"GAME!" is showing: the match has been decided, a moment before the results.</summary>
        public bool Decided;
        public readonly BShot[] Shots = new BShot[MaxShots];
        public readonly List<BrEvent> Events = new List<BrEvent>();
        uint seed;

        // the stage surprise (Hazard): state for the picture too
        public string Hazard = "";
        public int HazT, HazPhase;   // phase 0 = quiet, 1 = warning, 2 = happening
        public double HazX, HazY; public int HazDir;
        public readonly int[] HazHit = new int[8];
        /// <summary>The orchard's pecans: where each one lands (picked when the shaker starts, so their shadows show
        /// through the whole warning), one every NutGap frames once they fall, NutDrop frames from the branches.</summary>
        public readonly double[] Nuts = new double[5];
        public const int NutGap = 9, NutDrop = 24;
        readonly string[] hazards;
        /// <summary>The javelina herd, front to back: where each runs behind the leader (HazX) and whether it is a grown-up
        /// (two) or a baby (three).</summary>
        public static readonly double[] HerdBack = { 0, 50, 90, 150, 196 };
        public static readonly bool[] HerdAdult = { true, false, false, true, false };
        public const double HerdSpeed = 9;

        public BrawlSim(BStageDef stage, IList<BFighterDef> fighters, int lives, int minutes, uint rngSeed)
        {
            Stage = stage;
            seed = rngSeed == 0 ? 0x9E3779B9u : rngSeed;
            foreach (BPlat p in stage.Plats) Plats.Add(new BPlat { X1 = p.X1, X2 = p.X2, Y = p.Y, Solid = p.Solid, Thick = p.Thick, MoveX = p.MoveX, MoveY = p.MoveY, Period = p.Period, Look = p.Look });
            for (int i = 0; i < MaxShots; i++) Shots[i] = new BShot();
            F = new BFighter[fighters.Count];
            StartLives = Math.Max(1, lives);
            TimeMode = minutes > 0;
            TimeLeft = minutes * 3600;
            hazards = stage.Hazard.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            Hazard = hazards.Length > 0 ? hazards[0] : "";
            HazT = 600 + Rand(300);
            for (int i = 0; i < F.Length; i++)
            {
                var f = new BFighter { Slot = i, D = fighters[i], Stocks = TimeMode ? 1 : StartLives };
                F[i] = f;
                Spawn(f, false);
                f.St = BS.Idle; f.Ground = true; f.On = Plats[0]; f.Y = Plats[0].Top; f.Invuln = 0;
                for (int k = 0; k < 32; k++) f.HitAt[k] = -9999;
            }
            if (F.Length > 0) for (int i = 0; i < F.Length; i++) F[i].Face = F[i].X < 0 ? 1 : -1;
        }

        public int Rand(int n) { seed ^= seed << 13; seed ^= seed >> 17; seed ^= seed << 5; return n <= 0 ? 0 : (int)(seed % (uint)n); }
        public double RandD() { return Rand(1 << 20) / (double)(1 << 20); }

        public BPlat Main { get { return Plats[0]; } }
        public double SpawnX(int slot)
        {
            double half = Math.Min(Main.X2 - 60, 300);
            int n = Math.Max(2, F.Length);
            return -half + 2 * half * slot / (n - 1);
        }

        void Spawn(BFighter f, bool respawn)
        {
            f.X = SpawnX(f.Slot); f.Y = respawn ? Main.Top + 320 : Main.Top;
            f.Vx = f.Vy = f.Lx = f.Ly = 0;
            f.Dmg = Sudden ? 300 : 0; f.ShieldHp = 50;
            f.St = respawn ? BS.Enter : BS.Idle; f.T = 0; f.Mv = null; f.Ground = !respawn; f.On = respawn ? null : Main;
            f.Jumps = f.D.Jumps - 1; f.UpUsed = f.SideUsed = f.AirDodged = f.FastFall = false;
            f.Invuln = respawn ? 150 : 0; f.Hitlag = 0; f.Hitstun = 0; f.Tumble = false;
            f.Grabber = f.Victim = -1; f.Last = -1; f.LastT = 9999;
        }

        void Ev(string k, string name, double x, double y, double a, int s) { Events.Add(new BrEvent { K = k, Name = name, X = x, Y = y, A = a, S = s }); }
        void Sfx(string name, double x, double vol) { Ev("sfx", name, x, 0, vol, -1); }

        // ================================================================ the step
        public void Step(BrInput[] inputs)
        {
            if (Over) return;
            Frame++;
            Events.Clear();
            foreach (BPlat p in Plats)
            {
                if (p.Period <= 0) { p.DX = p.DY = 0; continue; }
                double a = 2 * Math.PI * Frame / p.Period;
                double nx = p.MoveX * DMath.Sin(a), ny = p.MoveY * DMath.Sin(a);
                p.DX = nx - p.OX; p.DY = ny - p.OY; p.OX = nx; p.OY = ny;
            }
            for (int i = 0; i < F.Length; i++) { F[i].Prev = F[i].In; F[i].In = inputs != null && i < inputs.Length ? inputs[i] : new BrInput(); }
            if (Decided) { OverT++; if (OverT >= 150) Over = true; return; }
            for (int i = 0; i < F.Length; i++) { Buffer(F[i]); StepFighter(F[i]); }
            StepShots();
            for (int i = 0; i < F.Length; i++) MeleeHits(F[i]);
            if (Surprises) StepHazard();
            for (int i = 0; i < F.Length; i++) CheckBlast(F[i]);
            Push();
            CheckEnd();
        }

        // ================================================================ one fighter
        static bool Pressed(bool now, bool before) { return now && !before; }

        // the button buffer (Andrew, 2026-10-29: "my character might be slow to react"): a button pressed while the fighter
        // is still busy (an attack finishing, landing, a hit's stun) is kept for BufferFrames steps and used on the first
        // step it can be, instead of being lost. Grabbed, at the ledge, out or coming in, nothing is kept.
        public const int BufferFrames = 9;
        const int BAttack = 0, BStrong = 1, BSpecial = 2, BGrab = 3, BShield = 4, BJump = 5;
        void Buffer(BFighter f)
        {
            BrInput i = f.In, p = f.Prev;
            if (f.St == BS.Grabbed || f.St == BS.Ledge || f.St == BS.LedgeUp || f.St == BS.Out || f.St == BS.Enter) { for (int b = 0; b < 6; b++) f.Buf[b] = 0; return; }
            for (int b = 0; b < 6; b++) if (f.Buf[b] > 0) f.Buf[b]--;
            if (Pressed(i.Attack, p.Attack)) f.Buf[BAttack] = BufferFrames;
            if (Pressed(i.Strong, p.Strong)) f.Buf[BStrong] = BufferFrames;
            if (Pressed(i.Special, p.Special)) f.Buf[BSpecial] = BufferFrames;
            if (Pressed(i.Grab, p.Grab)) f.Buf[BGrab] = BufferFrames;
            if (Pressed(i.Shield, p.Shield)) f.Buf[BShield] = BufferFrames;
            if (Pressed(i.Jump, p.Jump) || (i.Y > 0.5 && p.Y <= 0.5)) f.Buf[BJump] = BufferFrames;
        }
        /// <summary>Was the button pressed just now or in the last few busy steps? Using it empties it.</summary>
        public const int MaxChain = 4;
        /// <summary>Moves a combo can cancel: the ordinary attacks and the dash / lunge ones, not grabs, throws, recoveries or dives.</summary>
        static bool Chains(BMove m) { return (m.Kind == "" || m.Kind == "dashatk" || m.Kind == "lunge") && m.Hits.Count > 0; }
        static bool Tap(BFighter f, int b) { if (f.Buf[b] <= 0) return false; f.Buf[b] = 0; return true; }

        void StepFighter(BFighter f)
        {
            if (f.St == BS.Out)
            {
                f.T++;
                if (f.T >= (Practice ? 30 : 100) && (f.Stocks > 0 || TimeMode) && !Decided) { Spawn(f, true); Ev("respawn", "", f.X, f.Y, 0, f.Slot); }
                return;
            }
            if (f.Hitlag > 0) { f.Hitlag--; return; }
            if (f.Invuln > 0) f.Invuln--;
            if (f.LedgeCool > 0) f.LedgeCool--;
            if (f.DropT > 0) f.DropT--;
            if (f.LastT < 9999) f.LastT++;
            if (f.St != BS.Shield && f.St != BS.ShieldStun) f.ShieldHp = Math.Min(50, f.ShieldHp + 0.1);
            f.T++;
            BrInput i = f.In, p = f.Prev;
            bool jP = Pressed(i.Jump, p.Jump) || (i.Y > 0.5 && p.Y <= 0.5);
            switch (f.St)
            {
                case BS.Idle: case BS.Walk: case BS.Run: case BS.Crouch: GroundActions(f, jP); break;
                case BS.Squat:
                    // a quick up + attack (or special, or strong) from the squat is the up attack instead of a jump
                    if (Tap(f, BAttack)) { StartMove(f, "utilt"); break; }
                    if (Tap(f, BStrong)) { StartMove(f, "ustrong"); break; }
                    if (Tap(f, BSpecial)) { StartMove(f, "uspec"); break; }
                    if (f.T >= 4)
                    {
                        bool full = i.Jump || i.Y > 0.5;
                        f.Vy = f.D.Jump * (full ? 1 : 0.68); f.Ground = false; f.On = null; f.Y += 1;
                        f.Vx = Math.Max(-f.D.Air, Math.Min(f.D.Air, f.Vx * 0.8 + i.X * 1.2));
                        Set(f, BS.Air); Sfx("brawl-jump", f.X, 0.5);
                    }
                    break;
                case BS.Land: case BS.ShieldDrop: case BS.Dizzy: case BS.Down: case BS.ShieldStun:
                    f.Vx *= 0.8;
                    if (f.St == BS.ShieldStun && f.T >= f.Lag) { Set(f, i.Shield ? BS.Shield : BS.ShieldDrop); f.Lag = 6; break; }
                    if (f.T >= f.Lag) { Set(f, BS.Idle); if (f.St == BS.Idle && f.Lag >= 20) f.Invuln = Math.Max(f.Invuln, 10); }
                    break;
                case BS.Air: AirActions(f, jP); break;
                case BS.Attack: StepMove(f); break;
                case BS.Shield: ShieldActions(f, jP); break;
                case BS.Roll:
                    f.Vx = f.T < 22 ? -f.Face * 7.2 * (1 - f.T / 26.0) : 0;
                    if (f.T == 4) f.Invuln = Math.Max(f.Invuln, 13);
                    if (f.T >= 26) Set(f, BS.Idle);
                    break;
                case BS.Dodge:
                    f.Vx = 0;
                    if (f.T == 3) f.Invuln = Math.Max(f.Invuln, 13);
                    if (f.T >= 22) Set(f, BS.Idle);
                    break;
                case BS.AirDodge:
                    f.Vx *= 0.9; f.Vy *= 0.9;
                    if (f.T == 3) f.Invuln = Math.Max(f.Invuln, 20);
                    if (f.T >= 30) Set(f, BS.Air);
                    break;
                case BS.Hurt:
                    if (f.Hitstun > 0) f.Hitstun--;
                    f.Vx += i.X * 0.06;
                    if (f.Hitstun <= 0) Set(f, f.Ground ? BS.Idle : BS.Air);
                    break;
                case BS.Helpless:
                    f.Vx += i.X * f.D.AirAcc * 0.6;
                    f.Vx = Math.Max(-f.D.Air * 0.7, Math.Min(f.D.Air * 0.7, f.Vx));
                    break;
                case BS.Ledge: LedgeActions(f, jP); break;
                case BS.LedgeUp:
                    if (f.T >= 14) ClimbDone(f);
                    break;
                case BS.Grabbing: GrabbingActions(f); break;
                case BS.Grabbed:
                    if (Pressed(i.Attack, p.Attack) || Pressed(i.Jump, p.Jump) || Pressed(i.Special, p.Special) || (Math.Abs(i.X) > 0.5 && Math.Abs(p.X) <= 0.5))
                        if (f.Grabber >= 0 && F[f.Grabber].St == BS.Grabbing) F[f.Grabber].HoldT -= 4;
                    if (f.Grabber < 0 || (F[f.Grabber].St != BS.Grabbing && !(F[f.Grabber].St == BS.Attack && F[f.Grabber].Victim == f.Slot))) { f.Grabber = -1; Set(f, f.Ground ? BS.Idle : BS.Air); }
                    return;
                case BS.Enter:
                    f.Vx = f.Vy = 0;
                    if (f.T > 180 || (f.T > 20 && f.In.Any && !f.Prev.Any)) { Set(f, BS.Air); f.Y -= 1; }
                    return;
            }
            if (f.St != BS.Ledge && f.St != BS.LedgeUp && f.St != BS.Grabbed) Move(f);
        }

        void Set(BFighter f, BS st) { f.St = st; f.T = 0; if (st != BS.Attack) { f.Mv = null; } }

        void GroundActions(BFighter f, bool jP)
        {
            BrInput i = f.In, p = f.Prev;
            if (!f.Ground) { Set(f, BS.Air); return; }
            if (i.Shield) { if (Tap(f, BAttack) | Tap(f, BGrab)) { StartMove(f, "grab"); return; } Set(f, BS.Shield); f.Vx *= 0.5; return; }
            if (Tap(f, BGrab)) { StartMove(f, "grab"); return; }
            if (Tap(f, BSpecial)) { StartMove(f, SpecialFor(f)); return; }
            if (Tap(f, BStrong))
            {
                if (i.Y > 0.5) StartMove(f, "ustrong");
                else if (i.Y < -0.5) StartMove(f, "dstrong");
                else { if (Math.Abs(i.X) > 0.3) f.Face = i.X > 0 ? 1 : -1; StartMove(f, "fstrong"); }
                return;
            }
            if (Tap(f, BAttack))
            {
                if (f.St == BS.Run && Math.Abs(i.X) > 0.3) StartMove(f, "dash");
                else if (i.Y > 0.5) StartMove(f, "utilt");
                else if (i.Y < -0.5) StartMove(f, "dtilt");
                else if (Math.Abs(i.X) > 0.3) { f.Face = i.X > 0 ? 1 : -1; StartMove(f, "ftilt"); }
                else StartMove(f, "jab");
                return;
            }
            if (Tap(f, BJump)) { Set(f, BS.Squat); return; }
            if (i.Y < -0.5)
            {
                // down on a thin platform drops through it (pressed, not held from before)
                if (f.On != null && !f.On.Solid && p.Y >= -0.5) { f.DropT = 14; f.Ground = false; f.On = null; f.Y -= 2; Set(f, BS.Air); return; }
                if (f.St != BS.Crouch) Set(f, BS.Crouch);
                f.Vx *= 0.75; f.RunT = 0;
                return;
            }
            if (Math.Abs(i.X) > 0.3)
            {
                int d = i.X > 0 ? 1 : -1;
                if (d != f.Face) { f.Face = d; if (f.St == BS.Run) f.Vx *= 0.3; }
                f.RunT++;
                bool run = f.RunT > 9 || Math.Abs(i.X) > 0.9 && f.St == BS.Run;
                BS want = run ? BS.Run : BS.Walk;
                if (f.St != want) { f.St = want; }
                double target = d * (run ? f.D.Run : f.D.Walk * Math.Min(1, Math.Abs(i.X) / 0.8)) * Math.Min(1.2, 0.9 + f.Size * 0.1);
                double acc = run ? 1.1 : 0.8;
                f.Vx += Math.Max(-acc, Math.Min(acc, target - f.Vx));
            }
            else
            {
                f.RunT = 0;
                if (f.St != BS.Idle) f.St = BS.Idle;
                f.Vx += Math.Max(-0.7, Math.Min(0.7, -f.Vx));
            }
        }

        string SpecialFor(BFighter f)
        {
            BrInput i = f.In;
            if (i.Y > 0.5) return "uspec";
            if (i.Y < -0.5) return "dspec";
            if (Math.Abs(i.X) > 0.3) { f.Face = i.X > 0 ? 1 : -1; return "sspec"; }
            return "nspec";
        }

        void AirActions(BFighter f, bool jP)
        {
            BrInput i = f.In, p = f.Prev;
            if (f.Buf[BSpecial] > 0 && StartMove(f, SpecialFor(f))) { f.Buf[BSpecial] = 0; return; }
            if (Tap(f, BAttack) | Tap(f, BStrong))
            {
                string id;
                if (i.Y > 0.5) id = "uair";
                else if (i.Y < -0.5) id = "dair";
                else if (Math.Abs(i.X) > 0.3) id = (i.X > 0 ? 1 : -1) == f.Face ? "fair" : "bair";
                else id = "nair";
                StartMove(f, id);
                return;
            }
            if (!f.AirDodged && Tap(f, BShield))
            {
                double dx = i.X, dy = i.Y, len = Math.Sqrt(dx * dx + dy * dy);
                if (len > 0.3) { f.Vx = dx / len * 9; f.Vy = dy / len * 9; } else { f.Vx *= 0.3; f.Vy = 0; }
                f.AirDodged = true; f.Lx = f.Ly = 0;
                Set(f, BS.AirDodge); Sfx("brawl-dodge", f.X, 0.4);
                return;
            }
            if (f.Jumps > 0 && Tap(f, BJump))
            {
                f.Jumps--; f.Vy = f.D.DJump; f.Vx = i.X * f.D.Air; f.FastFall = false; f.Lx = f.Ly = 0;
                Set(f, BS.Air); Ev("djump", "", f.X, f.Y, 0, f.Slot); Sfx("brawl-djump", f.X, 0.45);
                return;
            }
            if (i.Y < -0.5 && p.Y >= -0.5 && f.Vy <= 1) f.FastFall = true;
            Drift(f, 1);
        }

        void Drift(BFighter f, double k)
        {
            double ax = f.In.X * f.D.AirAcc * k;
            if (Math.Abs(f.In.X) > 0.1)
            {
                double max = f.D.Air;
                if (f.Vx * Math.Sign(ax) < max) f.Vx = Math.Min(max, Math.Max(-max, f.Vx + ax));
            }
            else f.Vx *= 0.96;
        }

        void ShieldActions(BFighter f, bool jP)
        {
            BrInput i = f.In, p = f.Prev;
            f.Vx *= 0.7;
            if (!i.Shield) { Set(f, BS.ShieldDrop); f.Lag = 7; return; }
            f.ShieldHp -= 0.14;
            if (f.ShieldHp <= 0) { BreakShield(f); return; }
            if (Tap(f, BAttack) | Tap(f, BGrab)) { StartMove(f, "grab"); return; }
            if (Tap(f, BJump)) { Set(f, BS.Squat); return; }
            if (Math.Abs(i.X) > 0.5 && Math.Abs(p.X) <= 0.5)
            {
                // roll: behind you the way you pressed (you end up facing back)
                f.Face = i.X > 0 ? -1 : 1;
                Set(f, BS.Roll); Sfx("brawl-dodge", f.X, 0.4); return;
            }
            if (i.Y < -0.5 && p.Y >= -0.5) { Set(f, BS.Dodge); Sfx("brawl-dodge", f.X, 0.4); }
        }

        void BreakShield(BFighter f)
        {
            Set(f, BS.Dizzy); f.Lag = 200; f.ShieldHp = 30;
            f.Ground = false; f.On = null; f.Vy = 9; f.Y += 1;
            Ev("break", "", f.X, f.Y + f.H, 0, f.Slot); Sfx("brawl-break", f.X, 0.9);
        }

        // ---------------------------------------------------------------- moves
        public bool StartMove(BFighter f, string id)
        {
            BMove m = f.D.M(id);
            if (m == null) return false;
            if (m.Kind == "rise") { if (f.UpUsed) return false; f.UpUsed = true; if (Math.Abs(f.In.X) > 0.3) f.Face = f.In.X > 0 ? 1 : -1; }
            if (m.Kind == "lunge" && !f.Ground) { if (f.SideUsed) return false; f.SideUsed = true; }
            f.St = BS.Attack; f.T = 0; f.Mv = m; f.MF = 0; f.Charge = 0; f.AirMove = !f.Ground;
            f.Connected = false;
            for (int k = 0; k < 32; k++) f.HitAt[k] = -9999;
            if (m.Kind == "grab" && !f.Ground) { f.Mv = f.D.M("nair"); f.MF = 0; }
            return true;
        }

        void StepMove(BFighter f)
        {
            BMove m = f.Mv;
            if (m == null) { Set(f, f.Ground ? BS.Idle : BS.Air); return; }
            // a strong attack held at its charge frame: up to a second, harder the longer
            if (m.ChargeAt > 0 && f.MF == m.ChargeAt && f.In.Strong && f.Charge < 60)
            {
                f.Charge++;
                if (f.Charge == 1) Ev("charge", "", f.X, f.Y + f.H * 0.5, 0, f.Slot);
                f.Vx *= 0.8;
                return;
            }
            f.MF++;
            if (f.MF == m.SoundAt && m.Sound != "") Sfx(m.Sound, f.X, 0.6);
            // combos (Andrew, 2026-10-31: "very responsive with the feel of being able to smash through combos"): once this
            // attack has landed and its hits are over, a button pressed (or kept by the buffer) cancels the rest of it into the
            // next move. At most MaxChain cancels in a row, so nobody is locked in place.
            if (f.Connected && f.Chain < MaxChain && f.MF > m.LastActive && Chains(m) && f.Victim < 0 &&
                (f.Buf[BAttack] > 0 || f.Buf[BStrong] > 0 || f.Buf[BSpecial] > 0 || f.Buf[BJump] > 0 || f.Buf[BGrab] > 0))
            {
                int chain = f.Chain + 1;
                Set(f, f.Ground ? BS.Idle : BS.Air);
                if (f.Ground) GroundActions(f, false); else AirActions(f, false);
                f.Chain = f.St == BS.Attack || f.St == BS.Squat ? chain : 0;
                if (f.St == BS.Attack) Ev("combo", "", f.X, f.Y + f.H * 0.5, chain, f.Slot);
                return;
            }
            bool inV = f.MF >= m.VFrom && f.MF <= m.VTo && m.VTo > 0;
            switch (m.Kind)
            {
                case "rise":
                    if (inV)
                    {
                        if (f.Ground) { f.Ground = false; f.On = null; f.Y += 1; }
                        f.Vy = m.Vy * Math.Sqrt(Stage.Gravity);
                        f.Vx = f.Face * m.Vx + f.In.X * m.Drift;
                        f.Lx = f.Ly = 0;
                    }
                    break;
                case "lunge":
                    if (inV) { f.Vx = f.Face * m.Vx; if (!f.Ground) f.Vy = Math.Max(f.Vy, 0.5); }
                    else f.Vx *= 0.85;
                    break;
                case "dive":
                    if (f.MF >= m.VFrom)
                    {
                        if (f.Ground) { DiveLanded(f); return; }
                        f.Vy = m.Vy; f.Vx = f.Face * m.Vx + f.In.X * 1.2;
                    }
                    else if (!f.Ground) { f.Vy = Math.Max(f.Vy, 0); f.Vx *= 0.9; }
                    break;
                case "dashatk":
                    if (f.Ground) f.Vx = f.Face * f.D.Run * Math.Max(0, 1 - f.MF / 22.0);
                    break;
                case "cmdgrab":
                    if (f.Victim >= 0)
                    {
                        BFighter v = F[f.Victim];
                        double tx = f.X + f.Face * (f.HalfW + v.HalfW + 4);
                        v.X += (tx - v.X) * 0.35; v.Y += (f.Y - v.Y) * 0.35;
                        f.PullT--;
                        if (f.PullT <= 0) { ApplyThrow(f, v, m.ThrowHit); return; }
                        return;
                    }
                    break;
                case "throw":
                    if (f.Victim >= 0)
                    {
                        BFighter v = F[f.Victim];
                        HoldVictim(f, v, f.MF);
                        if (f.MF >= m.ThrowHit.From) { ApplyThrow(f, v, m.ThrowHit); }
                    }
                    break;
                default:
                    if (inV && m.Vx == 0 && m.Vy == 0 && !f.Ground) { f.Vy = Math.Max(f.Vy, -1.6); Drift(f, 0.5); }
                    else if (f.AirMove && !f.Ground) Drift(f, 1);
                    else if (f.Ground) f.Vx *= 0.82;
                    break;
            }
            if (m.Kind == "rise" && !inV && !f.Ground) Drift(f, 0.6);
            if (m.Shot != null && f.MF == m.ShotAt)
            {
                if (m.ShotAngles == null) Fire(f, m.Shot, 0);
                else foreach (double a in m.ShotAngles) Fire(f, m.Shot, a);
            }
            if (f.MF >= m.Len)
            {
                if (f.Victim >= 0 && f.St == BS.Attack && m.Kind != "grab") ReleaseVictim(f);
                if (f.Ground) Set(f, BS.Idle);
                else if (m.Helpless) Set(f, BS.Helpless);
                else Set(f, BS.Air);
            }
        }

        void DiveLanded(BFighter f)
        {
            BMove m = f.Mv;
            if (m.LandHit != null)
            {
                Ev("quake", "", f.X, f.Y, m.LandHit.R, f.Slot);
                Sfx("brawl-thud", f.X, 0.9);
                foreach (BFighter t in F)
                {
                    if (t == f || !t.Playing || t.Invuln > 0) continue;
                    double dx = t.X - f.X, dy = (t.Y + t.H * 0.3) - (f.Y + m.LandHit.Y);
                    if (dx * dx + dy * dy <= (m.LandHit.R + t.HalfW) * (m.LandHit.R + t.HalfW))
                        Hit(f, t, m.LandHit, dx >= 0 ? 1 : -1, 1, false);
                }
            }
            Set(f, BS.Land); f.Lag = 14;
        }

        void Fire(BFighter f, BShotDef d, double angle)
        {
            if (d.Wall) foreach (BShot o in Shots) if (o.On && o.Owner == f.Slot && o.D.Wall) o.On = false;
            BShot s = null;
            foreach (BShot o in Shots) if (!o.On) { s = o; break; }
            if (s == null) return;
            double k = f.D.Scale * f.Size;
            s.On = true; s.Owner = f.Slot; s.D = d; s.Face = f.Face; s.Age = 0; s.Back = false; s.Landed = false;
            s.X = f.X + f.Face * d.SX * (d.Look == "bolt" ? 1 : k); s.Y = f.Y + d.SY * k;
            double a = angle * Math.PI / 180;
            s.Vx = f.Face * (d.Vx * DMath.Cos(a)) ; s.Vy = d.Vy + d.Vx * DMath.Sin(a);
            if (d.Look == "bolt")
            {
                // the strike lands on whatever floor is under that spot
                double top = FloorUnder(s.X, f.Y + 200);
                s.Y = top > -9000 ? top : f.Y;
            }
            for (int i = 0; i < 8; i++) s.HitAt[i] = -9999;
            if (d.Look == "grenade" || d.Look == "rock" || d.Look == "book" || d.Look == "pancake" || d.Look == "log") s.Vx += f.Vx * 0.3;
            Ev("shot", d.Look, s.X, s.Y, 0, f.Slot);
        }

        /// <summary>The top of the highest platform under (x, below y), or -9999.</summary>
        public double FloorUnder(double x, double y)
        {
            double best = -9999;
            foreach (BPlat p in Plats) if (x >= p.L && x <= p.R && p.Top <= y && p.Top > best) best = p.Top;
            return best;
        }

        // ---------------------------------------------------------------- grabs
        void HoldVictim(BFighter f, BFighter v, int frame)
        {
            v.X = f.X + f.Face * (f.HalfW + v.HalfW - 4); v.Y = f.Y; v.Vx = v.Vy = v.Lx = v.Ly = 0;
            v.Face = -f.Face;
        }

        void GrabbingActions(BFighter f)
        {
            BrInput i = f.In, p = f.Prev;
            f.Vx = 0;
            if (f.Victim < 0 || F[f.Victim].St != BS.Grabbed) { f.Victim = -1; Set(f, BS.Idle); return; }
            BFighter v = F[f.Victim];
            HoldVictim(f, v, 0);
            f.HoldT--;
            if (Pressed(i.Attack, p.Attack) && f.T > 6)
            {
                v.Dmg = Math.Min(999, v.Dmg + 1.5); f.Done += 1.5; v.Taken += 1.5; f.Hitlag = 4; v.Hitlag = 4; f.T = 0;
                Ev("hit", "", v.X, v.Y + v.H * 0.6, 10, v.Slot); Sfx("brawl-pummel", v.X, 0.5);
                return;
            }
            string th = null;
            if (i.Y > 0.5) th = "uthrow"; else if (i.Y < -0.5) th = "dthrow";
            else if (Math.Abs(i.X) > 0.5) th = (i.X > 0 ? 1 : -1) == f.Face ? "fthrow" : "bthrow";
            if (th != null)
            {
                int victim = f.Victim;
                StartMove(f, th); f.Victim = victim;
                return;
            }
            if (f.HoldT <= 0) { ReleaseVictim(f); Set(f, BS.Idle); }
        }

        void ReleaseVictim(BFighter f)
        {
            if (f.Victim < 0) return;
            BFighter v = F[f.Victim];
            f.Victim = -1;
            if (v.St == BS.Grabbed)
            {
                v.Grabber = -1; Set(v, v.Ground ? BS.Idle : BS.Air);
                v.Vx = f.Face * 4; f.Vx = -f.Face * 3;
            }
        }

        void ApplyThrow(BFighter f, BFighter v, BHit h)
        {
            f.Victim = -1; v.Grabber = -1;
            v.St = BS.Air; v.Invuln = 0;
            int dir = f.Face;
            Hit(f, v, h, dir, 1, true);
            if (f.Mv != null && f.Mv.Kind == "cmdgrab") { f.MF = Math.Max(f.MF, f.Mv.Len - 12); }
        }

        void Grabbed(BFighter f, BFighter v)
        {
            if (v.St == BS.Grabbed || v.St == BS.Ledge || v.St == BS.LedgeUp) return;
            if (v.Victim >= 0) ReleaseVictim(v);
            v.Mv = null; v.St = BS.Grabbed; v.T = 0; v.Grabber = f.Slot; v.Lx = v.Ly = 0;
            f.Victim = v.Slot;
            Sfx("brawl-grab", v.X, 0.6);
            if (f.Mv != null && f.Mv.Kind == "cmdgrab") { f.PullT = 10; return; }
            f.St = BS.Grabbing; f.T = 0; f.Mv = null;
            f.HoldT = (int)(70 + v.Dmg * 0.6);
        }

        // ---------------------------------------------------------------- ledges
        void LedgeActions(BFighter f, bool jP)
        {
            BrInput i = f.In, p = f.Prev;
            f.Vx = f.Vy = 0;
            int side = f.LedgeSide;
            bool towardStage = i.X * -side > 0.5, away = i.X * side > 0.5;
            if (f.T < 6) return;
            if (jP && i.Y <= 0.5 || Pressed(i.Jump, p.Jump))
            {
                Set(f, BS.Air); f.Vy = f.D.Jump * 1.0; f.Vx = -side * 2.5; f.Y += 4;
                f.LedgeCool = 20; Sfx("brawl-jump", f.X, 0.5);
                return;
            }
            if (i.Y > 0.5 && p.Y <= 0.5 || towardStage && p.X * -side <= 0.5) { f.LedgeNext = ""; Set(f, BS.LedgeUp); f.Invuln = Math.Max(f.Invuln, 14); return; }
            if (Pressed(i.Attack, p.Attack) || Pressed(i.Strong, p.Strong)) { f.LedgeNext = "ftilt"; Set(f, BS.LedgeUp); f.Invuln = Math.Max(f.Invuln, 14); return; }
            if (Pressed(i.Shield, p.Shield)) { f.LedgeNext = "roll"; Set(f, BS.LedgeUp); f.Invuln = Math.Max(f.Invuln, 20); return; }
            if (i.Y < -0.5 && p.Y >= -0.5 || away && p.X * side <= 0.5 || f.T > 300)
            {
                Set(f, BS.Air); f.LedgeCool = 30; f.X += side * 8;
                return;
            }
        }

        void ClimbDone(BFighter f)
        {
            BPlat p = f.LedgePlat;
            double ex = f.LedgeSide < 0 ? p.L : p.R;
            f.X = ex - f.LedgeSide * (f.HalfW + 6); f.Y = p.Top; f.Ground = true; f.On = p; f.Face = -f.LedgeSide;
            Land(f);
            Set(f, BS.Idle);
            if (f.LedgeNext == "ftilt") StartMove(f, "ftilt");
            else if (f.LedgeNext == "roll") { f.Face = f.LedgeSide; Set(f, BS.Roll); }
            f.LedgeNext = "";
        }

        bool CanGrabLedge(BFighter f)
        {
            if (f.LedgeCool > 0 || f.Vy + f.Ly > 0.5) return false;
            if (f.St == BS.Air || f.St == BS.Helpless || f.St == BS.AirDodge) return true;
            if (f.St == BS.Attack && f.Mv != null && (f.Mv.Kind == "rise" && f.MF > f.Mv.VTo || f.Mv.Kind == "rise" && f.Vy <= 0)) return true;
            return false;
        }

        void TryLedge(BFighter f)
        {
            if (!CanGrabLedge(f)) return;
            foreach (BPlat p in Plats)
            {
                if (!p.Solid) continue;
                for (int side = -1; side <= 1; side += 2)
                {
                    double ex = side < 0 ? p.L : p.R;
                    double hx = ex + side * f.HalfW * 0.6;
                    double hy = p.Top - f.H * 0.85;
                    if (Math.Abs(f.X - hx) > 34 || f.Y < hy - 40 || f.Y > hy + 34) continue;
                    if ((f.X - ex) * side < -f.HalfW * 0.5) continue;   // must be outside the edge
                    bool taken = false;
                    foreach (BFighter o in F) if (o != f && (o.St == BS.Ledge || o.St == BS.LedgeUp) && o.LedgePlat == p && o.LedgeSide == side) taken = true;
                    if (taken) continue;
                    f.St = BS.Ledge; f.T = 0; f.Mv = null; f.LedgePlat = p; f.LedgeSide = side; f.Face = -side;
                    f.X = hx; f.Y = hy; f.Vx = f.Vy = f.Lx = f.Ly = 0;
                    f.Jumps = f.D.Jumps - 1; f.UpUsed = f.SideUsed = f.AirDodged = f.FastFall = false;
                    f.Invuln = Math.Max(f.Invuln, 36);
                    Ev("ledge", "", ex, p.Top, 0, f.Slot); Sfx("brawl-ledge", f.X, 0.5);
                    return;
                }
            }
        }

        // ---------------------------------------------------------------- moving and landing
        void Land(BFighter f)
        {
            f.Jumps = f.D.Jumps - 1; f.UpUsed = f.SideUsed = f.AirDodged = f.FastFall = false;
        }

        void Move(BFighter f)
        {
            bool float_ = f.St == BS.Attack && f.Mv != null && f.Mv.Float && f.MF >= f.Mv.VFrom && f.MF <= f.Mv.VTo;
            if (f.St == BS.Attack && f.Mv != null && f.Mv.Kind == "cmdgrab" && f.Victim >= 0) float_ = true;
            double g = f.D.Grav * Stage.Gravity;
            if (f.Ground && f.On != null)
            {
                f.X += f.On.DX; f.Y = f.On.Top;
                f.Vy = 0;
            }
            else if (!float_ && f.St != BS.Enter && f.St != BS.AirDodge)
            {
                f.Vy -= g;
                double max = f.D.Fall * (f.FastFall && f.St != BS.Hurt ? 1.55 : 1) * Math.Sqrt(Stage.Gravity);
                if (f.Vy < -max) f.Vy = -max;
            }
            // launch speed decays along its direction
            double lm = Math.Sqrt(f.Lx * f.Lx + f.Ly * f.Ly);
            if (lm > 0)
            {
                double nm = Math.Max(0, lm - LaunchDecay);
                f.Lx *= nm / lm; f.Ly *= nm / lm;
            }
            double ox = f.X, oy = f.Y;
            double nx = f.X + f.Vx + f.Lx, ny = f.Y + f.Vy + f.Ly;
            if (f.Ground && f.On != null)
            {
                ny = f.On.Top;
                if (nx < f.On.L - Edge || nx > f.On.R + Edge)
                {
                    // off the edge: walking, running, an attack carrying on into the air
                    f.Ground = false; f.On = null;
                    if (f.St == BS.Idle || f.St == BS.Walk || f.St == BS.Run || f.St == BS.Crouch || f.St == BS.Land || f.St == BS.ShieldDrop) Set(f, BS.Air);
                    else if (f.St == BS.Shield || f.St == BS.Roll || f.St == BS.Dodge || f.St == BS.Squat) Set(f, BS.Air);
                    if (f.St == BS.Attack) f.AirMove = false;
                }
            }
            else
            {
                double down = f.Vy + f.Ly;
                foreach (BPlat p in Plats)
                {
                    if (down > 0) break;
                    double prevTop = p.Top - p.DY;
                    if (oy >= prevTop - 0.5 && ny <= p.Top && nx >= p.L - Edge && nx <= p.R + Edge)
                    {
                        if (!p.Solid && (f.DropT > 0 || f.In.Y < -0.5 && f.St != BS.Hurt && f.St != BS.Helpless)) continue;
                        ny = p.Top; f.Ground = true; f.On = p;
                        Landed(f, down);
                        break;
                    }
                }
            }
            // the main ground's sides and underneath
            foreach (BPlat p in Plats)
            {
                if (!p.Solid) continue;
                // the middle of the body can't go into the ground's block (it may hang over its sides a little)
                double hw = Edge;
                if (nx > p.L - hw && nx < p.R + hw && ny < p.Top - 1 && ny + f.H > p.Top - p.Thick)
                {
                    if (ox <= p.L - hw + 0.5) { nx = p.L - hw; if (f.St == BS.Hurt && f.Lx > 3) { f.Lx = -f.Lx * 0.5; Sfx("brawl-thud", nx, 0.5); } f.Vx = Math.Min(0, f.Vx); }
                    else if (ox >= p.R + hw - 0.5) { nx = p.R + hw; if (f.St == BS.Hurt && f.Lx < -3) { f.Lx = -f.Lx * 0.5; Sfx("brawl-thud", nx, 0.5); } f.Vx = Math.Max(0, f.Vx); }
                    else if (oy < p.Top - 1)
                    {
                        ny = p.Top - p.Thick - f.H; f.Vy = Math.Min(0, f.Vy); if (f.Ly > 0) f.Ly = -f.Ly * 0.4;
                    }
                    else if (!f.Ground) { ny = p.Top; f.Ground = true; f.On = p; Landed(f, f.Vy + f.Ly); }
                }
            }
            f.X = nx; f.Y = ny;
            if (!f.Ground) TryLedge(f);
        }

        void Landed(BFighter f, double speed)
        {
            bool hard = f.St == BS.Hurt && f.Tumble;
            Land(f);
            double lx = f.Lx; f.Lx = f.Ly = 0; f.Vy = 0;
            f.Vx = Math.Max(-8, Math.Min(8, f.Vx + lx * 0.5));
            switch (f.St)
            {
                case BS.Air: Set(f, BS.Land); f.Lag = 4; Ev("land", "", f.X, f.Y, 0, f.Slot); break;
                case BS.AirDodge: Set(f, BS.Land); f.Lag = 10; break;
                case BS.Helpless: Set(f, BS.Land); f.Lag = 14; Ev("land", "", f.X, f.Y, 1, f.Slot); break;
                case BS.Hurt:
                    if (hard && f.Hitstun > 0) { Set(f, BS.Down); f.Lag = 30; f.Hitstun = 0; Ev("land", "", f.X, f.Y, 2, f.Slot); Sfx("brawl-thud", f.X, 0.6); }
                    else if (f.Hitstun > 0) { f.Hitstun = Math.Max(0, f.Hitstun - 6); }
                    else { Set(f, BS.Land); f.Lag = 4; }
                    break;
                case BS.Attack:
                    if (f.Mv != null && f.Mv.Kind == "dive" && f.MF >= f.Mv.VFrom) { DiveLanded(f); break; }
                    if (f.Mv != null && f.AirMove) { int lag = f.Mv.LandLag; Set(f, BS.Land); f.Lag = lag; Ev("land", "", f.X, f.Y, 0, f.Slot); }
                    else if (f.Mv != null && f.Mv.Kind == "rise" && f.MF > f.Mv.VTo) { Set(f, BS.Land); f.Lag = 12; }
                    break;
                case BS.Dizzy: break;
            }
        }

        void Push()
        {
            for (int a = 0; a < F.Length; a++)
                for (int b = a + 1; b < F.Length; b++)
                {
                    BFighter p = F[a], q = F[b];
                    if (!p.Ground || !q.Ground || !p.Playing || !q.Playing || p.On != q.On) continue;
                    if (p.St == BS.Grabbed || q.St == BS.Grabbed || p.St == BS.Grabbing || q.St == BS.Grabbing) continue;
                    double dx = q.X - p.X, min = (p.HalfW + q.HalfW) * 0.6;
                    if (Math.Abs(dx) < min)
                    {
                        double push = (min - Math.Abs(dx)) * 0.15 * (dx >= 0 ? 1 : -1);
                        if (dx == 0) push = 0.5;
                        p.X -= push; q.X += push;
                    }
                }
        }

        // ================================================================ hits
        public static bool Overlap(double cx, double cy, double r, BFighter t)
        {
            // the body is a capsule from just above the feet to the top of the head
            double rad = t.HalfW * 0.85;
            double y1 = t.Y + rad, y2 = t.Y + t.H - rad;
            if (t.St == BS.Crouch) y2 = Math.Max(y1, t.Y + t.H * 0.62 - rad);
            double py = Math.Max(y1, Math.Min(y2, cy));
            double dx = cx - t.X, dy = cy - py;
            return dx * dx + dy * dy <= (r + rad) * (r + rad);
        }

        void MeleeHits(BFighter f)
        {
            if (f.St != BS.Attack || f.Mv == null || f.Hitlag > 0) return;
            foreach (BHit h in f.Mv.Hits)
            {
                if (f.MF < h.From || f.MF > h.To) continue;
                double cx = f.X + f.Face * h.X * f.Size, cy = f.Y + h.Y * f.Size, r = h.R * f.Size;
                foreach (BFighter t in F)
                {
                    if (t == f || !t.Playing || t.St == BS.Enter) continue;
                    if (f.Victim == t.Slot) continue;
                    int key = t.Slot * 4 + h.Group;
                    int last = f.HitAt[key];
                    if (h.Rehit > 0 ? f.MF - last < h.Rehit : last > -9999) continue;
                    if (!Overlap(cx, cy, r, t)) continue;
                    if (t.Invuln > 0) continue;
                    if (h.Grab)
                    {
                        if (t.St == BS.Grabbed || t.St == BS.Ledge || t.St == BS.LedgeUp || t.St == BS.Roll || t.St == BS.Dodge || t.St == BS.AirDodge) continue;
                        f.HitAt[key] = f.MF;
                        Grabbed(f, t);
                        return;
                    }
                    f.HitAt[key] = f.MF;
                    // centred hits (spins, bursts) send away from the middle; the rest go the way the fighter faces
                    int dir = h.X == 0 ? (t.X >= f.X ? 1 : -1) : f.Face;
                    double mul = 1 + 0.4 * f.Charge / 60.0;
                    Hit(f, t, h, dir, mul, false);
                    if (f.St != BS.Attack) return;
                }
            }
        }

        /// <summary>The knockback a hit gives (the formula at the top of BrawlData.cs).</summary>
        public static double Knockback(double percentAfter, double dmg, double weight, BHit h)
        {
            double p = percentAfter;
            return ((p / 10 + p * dmg / 20) * (200 / (weight + 100)) * 1.4 + 18) * h.Grow / 100 + h.Base;
        }

        /// <summary>A hit lands. dir = which way "forward" is for the hit's angle.</summary>
        public void Hit(BFighter a, BFighter t, BHit h, int dir, double mul, bool isThrow)
        {
            double d = h.Dmg * mul;
            // shields take it (but not grabs and throws)
            if (t.St == BS.Shield && !isThrow)
            {
                t.ShieldHp -= d * 1.3 + 2;
                t.Vx = (a != null ? (t.X >= a.X ? 1 : -1) : dir) * Math.Min(8, 1 + d * 0.35);
                Set(t, BS.ShieldStun); t.Lag = (int)(d * 0.55 + 2);
                int hl = (int)Math.Min(12, d / 3 + 2);
                t.Hitlag = hl; if (a != null) a.Hitlag = hl;
                Ev("shield", "", t.X, t.Y + t.H * 0.5, d, t.Slot); Sfx("brawl-shieldhit", t.X, 0.6);
                if (t.ShieldHp <= 0) BreakShield(t);
                return;
            }
            // a counter catches the hit and sends it back
            if (a != null && !isThrow && t.St == BS.Attack && t.Mv != null && t.Mv.Kind == "counter" && t.MF >= t.Mv.CounterFrom && t.MF <= t.Mv.CounterTo)
            {
                var back = new BHit { Dmg = Math.Max(8, d * 1.3), Ang = 361, Base = 50, Grow = 90 };
                t.Invuln = 24; t.Face = a.X >= t.X ? 1 : -1;
                t.Mv = t.D.M("ftilt") ?? t.Mv; t.MF = t.Mv.Len - 10;
                Ev("counter", "", t.X, t.Y + t.H * 0.6, 0, t.Slot); Sfx("brawl-counter-hit", t.X, 0.9);
                Hit(t, a, back, t.Face, 1, false);
                return;
            }
            if (t.Victim >= 0) ReleaseVictim(t);
            if (t.St == BS.Grabbed && t.Grabber >= 0 && (a == null || t.Grabber != a.Slot)) { BFighter g = F[t.Grabber]; g.Victim = -1; Set(g, BS.Idle); t.Grabber = -1; }
            t.Dmg = Math.Min(999, t.Dmg + d); t.Taken += d;
            if (a != null && a != t) a.Done += d;
            if (a != null && a != t && a.St == BS.Attack) a.Connected = true;
            t.Chain = 0;
            double kb = Knockback(t.Dmg, d, t.Weight, h);
            double ang = h.Ang;
            if (ang == 361) ang = kb < 32 && t.Ground ? 0 : 40;
            double rad = ang * Math.PI / 180;
            double ux = DMath.Cos(rad) * dir, uy = DMath.Sin(rad);
            // influence: the one hit can tilt the launch a little by holding a direction across it
            double px = -uy, py = ux;
            double di = Math.Max(-1, Math.Min(1, t.In.X * px + t.In.Y * py));
            double turn = di * 15 * Math.PI / 180;
            double cx = ux * DMath.Cos(turn) - uy * DMath.Sin(turn), cy = ux * DMath.Sin(turn) + uy * DMath.Cos(turn);
            ux = cx; uy = cy;
            double speed = kb * KbSpeed;
            if (t.Ground && uy < 0) { uy = -uy * 0.8; }   // a spike on the ground bounces up
            t.Mv = null;
            if (t.Ground && kb < 45 && uy < 0.35)
            {
                t.Vx = ux * speed; t.Lx = t.Ly = 0;
                t.St = BS.Hurt; t.T = 0; t.Tumble = false;
            }
            else
            {
                t.Ground = false; t.On = null; t.Y += 1;
                t.Lx = ux * speed; t.Ly = uy * speed; t.Vx = 0; t.Vy = 0;
                t.St = BS.Hurt; t.T = 0; t.Tumble = kb > 80;
                t.FastFall = false;
            }
            t.Hitstun = (int)Math.Min(140, kb * 0.4);
            int lag = (int)Math.Min(18, (d / 3 + 3) * (h.Electric ? 1.5 : 1));
            t.Hitlag = lag; if (a != null && !isThrow) a.Hitlag = lag;
            if (a != null && a != t) { t.Last = a.Slot; t.LastT = 0; }
            Ev("hit", h.Electric ? "zap" : "", t.X, t.Y + t.H * 0.55, kb, t.Slot);
            Sfx(kb > 150 ? "brawl-smash" : kb > 70 ? "brawl-hit2" : "brawl-hit1", t.X, Math.Min(1, 0.45 + kb / 300));
            if (h.Electric) Sfx("brawl-zap", t.X, 0.5);
        }

        // ---------------------------------------------------------------- shots
        void StepShots()
        {
            foreach (BShot s in Shots)
            {
                if (!s.On) continue;
                s.Age++;
                BShotDef d = s.D;
                if (s.Age > d.Life + d.Delay) { if (d.Explode) Explode(s); s.On = false; continue; }
                if (s.Age <= d.Delay) continue;
                if (d.Boomerang)
                {
                    if (s.Age > d.Delay + d.Life * 0.42) s.Back = true;
                    if (s.Back)
                    {
                        BFighter o = F[s.Owner];
                        double dx = o.X - s.X, dy = (o.Y + o.H * 0.6) - s.Y, l = Math.Sqrt(dx * dx + dy * dy);
                        if (l < 30 || !o.Playing) { s.On = false; continue; }
                        s.Vx += dx / l * 0.9; s.Vy += dy / l * 0.9;
                        double v = Math.Sqrt(s.Vx * s.Vx + s.Vy * s.Vy);
                        if (v > 12) { s.Vx *= 12 / v; s.Vy *= 12 / v; }
                    }
                }
                s.Vy -= d.Grav * Stage.Gravity;
                double nx = s.X + s.Vx, ny = s.Y + s.Vy;
                bool dead = false;
                foreach (BPlat p in Plats)
                {
                    if (d.Look == "bolt" || d.Wall) break;
                    bool over = nx >= p.L && nx <= p.R;
                    if (over && s.Y >= p.Top - 0.5 && ny - d.R * 0.5 <= p.Top && s.Vy <= 0)
                    {
                        if (d.Rolls) { ny = p.Top + d.R; s.Vy = 0; s.Landed = true; continue; }
                        if (p.Solid || d.Grav > 0) { dead = true; break; }
                    }
                    if (p.Solid && nx > p.L && nx < p.R && ny < p.Top - 2 && ny > p.Top - p.Thick) { dead = true; break; }
                }
                s.X = nx; s.Y = ny;
                if (dead) { if (d.Explode) Explode(s); else Ev("poof", d.Look, s.X, s.Y, 0, s.Owner); s.On = false; continue; }
                if (s.X < Stage.BlastL - 100 || s.X > Stage.BlastR + 100 || s.Y < Stage.BlastB - 100) { s.On = false; continue; }
                // walls stop the other side's shots
                if (d.Wall)
                    foreach (BShot o in Shots)
                        if (o.On && o != s && o.Owner != s.Owner && !o.D.Wall && Math.Abs(o.X - s.X) < d.R + o.D.R && o.Y > s.Y - 60 && o.Y < s.Y + 60)
                        { o.On = false; Ev("poof", o.D.Look, o.X, o.Y, 0, o.Owner); Sfx("brawl-fizzle", o.X, 0.5); }
                ShotHits(s);
            }
        }

        bool ShotTouches(BShot s, BFighter t)
        {
            BShotDef d = s.D;
            if (d.Look == "bolt") return Math.Abs(t.X - s.X) < d.R + t.HalfW * 0.6 && t.Y + t.H > s.Y - 10 && t.Y < s.Y + 320;
            if (d.Wall) return Math.Abs(t.X - s.X) < d.R + t.HalfW * 0.6 && t.Y + t.H > s.Y - 46 && t.Y < s.Y + 70;
            return Overlap(s.X, s.Y, d.R, t);
        }

        void ShotHits(BShot s)
        {
            BShotDef d = s.D;
            foreach (BFighter t in F)
            {
                if (!s.On) return;
                if (t.Slot == s.Owner || !t.Playing || t.St == BS.Enter || t.Invuln > 0) continue;
                int last = s.HitAt[t.Slot];
                if (d.Rehit > 0 ? Frame - last < d.Rehit : last > -9999) continue;
                if (!ShotTouches(s, t)) continue;
                // a reflector sends it back, now theirs
                if (t.St == BS.Attack && t.Mv != null && t.Mv.Kind == "reflect" && t.MF >= t.Mv.ReflectFrom && t.MF <= t.Mv.ReflectTo && !d.Wall && d.Look != "bolt")
                {
                    s.Owner = t.Slot; s.Vx = -s.Vx * 1.2; s.Vy = Math.Abs(s.Vy) * 0.5; s.Face = -s.Face; s.Back = false; s.Age = Math.Min(s.Age, d.Delay + d.Life / 3);
                    for (int i = 0; i < 8; i++) s.HitAt[i] = -9999;
                    Ev("reflect", "", s.X, s.Y, 0, t.Slot); Sfx("brawl-reflect", s.X, 0.7);
                    return;
                }
                if (t.St == BS.Shield && !d.Wall)
                {
                    s.HitAt[t.Slot] = Frame;
                    Hit(F[s.Owner], t, new BHit { Dmg = d.Dmg, Ang = d.Ang, Base = d.Base, Grow = d.Grow }, s.Vx >= 0 ? 1 : -1, 1, false);
                    if (F[s.Owner].Hitlag > 0) F[s.Owner].Hitlag = 0;
                    if (d.Explode) Explode(s);
                    s.On = false; return;
                }
                if (d.Explode) { Explode(s); s.On = false; return; }
                s.HitAt[t.Slot] = Frame;
                int dir = d.Wall || d.Look == "bolt" || d.Look == "cloud" ? (t.X >= s.X ? 1 : -1) : (s.Vx > 0.1 ? 1 : s.Vx < -0.1 ? -1 : s.Face);
                BFighter owner = F[s.Owner];
                var h = new BHit { Dmg = d.Dmg, Ang = d.Ang, Base = d.Base, Grow = d.Grow, Electric = d.Electric };
                int hl = owner.Hitlag;
                Hit(owner, t, h, dir, 1, false);
                owner.Hitlag = hl;   // the thrower doesn't freeze for a shot far away
                if (d.Rehit == 0 && !d.Wall && d.Look != "bolt") { s.On = false; Ev("poof", d.Look, s.X, s.Y, 0, s.Owner); }
            }
        }

        void Explode(BShot s)
        {
            BShotDef d = s.D;
            Ev("boom", "", s.X, s.Y, d.ExplodeR, s.Owner); Sfx("brawl-boom", s.X, 0.9);
            var h = new BHit { Dmg = d.Dmg, Ang = d.Ang, Base = d.Base, Grow = d.Grow };
            foreach (BFighter t in F)
            {
                if (t.Slot == s.Owner || !t.Playing || t.Invuln > 0 || t.St == BS.Enter) continue;
                if (Overlap(s.X, s.Y, d.ExplodeR, t)) { int hl = F[s.Owner].Hitlag; Hit(F[s.Owner], t, h, t.X >= s.X ? 1 : -1, 1, false); F[s.Owner].Hitlag = hl; }
            }
        }

        // ---------------------------------------------------------------- the stage surprise (always warned first)
        public const int HazWarn = 90;
        void StepHazard()
        {
            if (Hazard == "") return;
            HazT--;
            if (HazPhase == 0 && HazT <= 0)
            {
                if (hazards.Length > 1) Hazard = hazards[Rand(hazards.Length)];
                HazPhase = 1; HazT = HazWarn; HazDir = Rand(2) == 0 ? -1 : 1;
                for (int i = 0; i < 8; i++) HazHit[i] = -9999;
                switch (Hazard)
                {
                    case "meteor": HazX = Main.L + 40 + RandD() * (Main.R - Main.L - 80); HazY = Main.Top; break;
                    case "pecans": for (int k = 0; k < Nuts.Length; k++) Nuts[k] = Main.L + 40 + RandD() * (Main.R - Main.L - 80); HazY = 0; Sfx("brawl-shaker", 0, 0.6); return;
                    case "laser": HazY = Main.Top + (Rand(2) == 0 ? 26 : 120); HazX = HazDir < 0 ? Stage.BlastR : Stage.BlastL; break;
                    case "cart": HazX = HazDir > 0 ? Main.L - 120 : Main.R + 120; HazY = Main.Top; break;
                    case "javelina": HazX = HazDir > 0 ? Main.L - 150 : Main.R + 150; HazY = Main.Top; Sfx("brawl-snort", HazX, 0.7); return;
                }
                Sfx(Hazard == "cart" ? "brawl-bell" : Hazard == "laser" ? "brawl-beep" : Hazard == "fans" ? "brawl-hum" : Hazard == "meteor" ? "brawl-whistle" : "brawl-wind", HazDir * 400, 0.6);
                return;
            }
            // the herd snorts and squeals from its side while it gathers
            if (HazPhase == 1 && Hazard == "javelina" && (HazT == 58 || HazT == 26)) Sfx(HazT == 58 ? "brawl-squeal" : "brawl-snort", HazX, 0.6);
            if (HazPhase == 1 && HazT <= 0)
            {
                HazPhase = 2;
                HazT = Hazard == "wind" ? 240 : Hazard == "fans" ? 180 : Hazard == "cart" ? 200 : Hazard == "laser" ? 40 : 30;
                if (Hazard == "meteor") HazY = Main.Top + 700;
                if (Hazard == "pecans") { HazY = 0; HazT = 9999; }   // ends once the last nut is down (below)
                if (Hazard == "javelina") { HazT = 600; Sfx("brawl-hooves", HazX, 0.7); }
                return;
            }
            if (HazPhase == 2)
            {
                switch (Hazard)
                {
                    case "wind":
                        foreach (BFighter f in F) if (f.Playing && f.St != BS.Ledge && f.St != BS.Grabbed && f.St != BS.Enter) f.X += HazDir * (f.Ground ? 0.8 : 2.2);
                        break;
                    case "fans":
                        foreach (BFighter f in F)
                        {
                            if (!f.Playing || f.St == BS.Ledge || f.St == BS.Enter || f.St == BS.Grabbed) continue;
                            double ax = Math.Abs(f.X);
                            if (ax > 140 && ax < 270 && f.Y < Main.Top + 420)
                            {
                                if (f.Ground) { f.Ground = false; f.On = null; f.Y += 2; if (f.St == BS.Idle || f.St == BS.Walk || f.St == BS.Run || f.St == BS.Crouch) Set(f, BS.Air); }
                                f.Vy = Math.Min(7, f.Vy + 1.1);
                            }
                        }
                        break;
                    case "cart":
                        HazX += HazDir * 9;
                        foreach (BFighter f in F)
                            if (f.Playing && f.Invuln == 0 && HazHit[f.Slot] < 0 && f.Y < Main.Top + 50 && Math.Abs(f.X - HazX) < 44 + f.HalfW && f.St != BS.Ledge)
                            { HazHit[f.Slot] = Frame; Hit(null, f, new BHit { Dmg = 12, Ang = 50, Base = 50, Grow = 60 }, HazDir, 1, false); }
                        if (HazX < Stage.BlastL || HazX > Stage.BlastR) HazT = 0;
                        break;
                    case "javelina":
                        StepHerd();
                        break;
                    case "laser":
                        HazX += HazDir * ((Stage.BlastR - Stage.BlastL) / 40.0);
                        foreach (BFighter f in F)
                            if (f.Playing && f.Invuln == 0 && HazHit[f.Slot] < 0 && Math.Abs(f.X - HazX) < 50 && f.Y < HazY && f.Y + f.H > HazY)
                            { HazHit[f.Slot] = Frame; Hit(null, f, new BHit { Dmg = 6, Ang = 60, Base = 40, Grow = 40, Electric = true }, HazDir, 1, false); }
                        break;
                    case "pecans":
                        // HazY counts the frames since the shaking; each nut lands on its own frame with a small bonk
                        HazY++;
                        for (int k = 0; k < Nuts.Length; k++)
                            if (HazY == k * NutGap + NutDrop)
                            {
                                double nx = Nuts[k];
                                Ev("land", "", nx, Main.Top, 0, -1); Sfx("brawl-pecan", nx, 0.6);
                                foreach (BFighter f in F)
                                    if (f.Playing && f.Invuln == 0 && Overlap(nx, Main.Top + 20, 34, f))
                                        Hit(null, f, new BHit { Dmg = 5, Ang = 80, Base = 30, Grow = 45 }, f.X >= nx ? 1 : -1, 1, false);
                            }
                        if (HazY >= (Nuts.Length - 1) * NutGap + NutDrop + 6) HazT = 0;
                        break;
                    case "meteor":
                        HazY -= 24;
                        if (HazY <= Main.Top + 10)
                        {
                            Ev("boom", "", HazX, Main.Top, 60, -1); Sfx("brawl-boom", HazX, 0.7);
                            foreach (BFighter f in F)
                                if (f.Playing && f.Invuln == 0 && Overlap(HazX, Main.Top + 20, 60, f))
                                    Hit(null, f, new BHit { Dmg = 8, Ang = 80, Base = 35, Grow = 50 }, f.X >= HazX ? 1 : -1, 1, false);
                            HazT = 0;
                        }
                        break;
                }
                HazT--;
                if (HazT <= 0) { bool shortWait = Hazard == "meteor" || Hazard == "pecans"; HazPhase = 0; HazT = (shortWait ? 360 : 780) + Rand(shortWait ? 240 : 420); }
            }
        }

        /// <summary>The javelina stampede: two grown-ups and three babies run along the ground from one side to the other.
        /// Anyone standing in their way gets bowled over (a grown-up hits harder than a baby); jumping, a platform or the
        /// ledge keeps you clear. A fighter can be bowled again once they have had a moment to get up.</summary>
        void StepHerd()
        {
            HazX += HazDir * HerdSpeed;
            if ((Frame & 31) == 0) Sfx(Rand(3) == 0 ? "brawl-squeal" : "brawl-snort", HazX, 0.5);
            for (int k = 0; k < HerdBack.Length; k++)
            {
                double mx = HazX - HazDir * HerdBack[k];
                if (mx < Main.L || mx > Main.R) continue;
                bool adult = HerdAdult[k];
                double half = adult ? 34 : 20, tall = adult ? 46 : 28;
                foreach (BFighter f in F)
                {
                    if (!f.Playing || f.Invuln > 0 || f.St == BS.Ledge || f.St == BS.Enter || f.St == BS.Grabbed) continue;
                    if (HazHit[f.Slot] > 0 && Frame - HazHit[f.Slot] < 50) continue;
                    if (f.Y > Main.Top + tall || f.Y < Main.Top - 4 || Math.Abs(f.X - mx) > half + f.HalfW) continue;
                    HazHit[f.Slot] = Frame;
                    Hit(null, f, adult ? new BHit { Dmg = 11, Ang = 55, Base = 48, Grow = 55 } : new BHit { Dmg = 5, Ang = 68, Base = 32, Grow = 32 }, HazDir, 1, false);
                }
            }
            // done once the last one has run off the far side
            double tail = HazX - HazDir * HerdBack[HerdBack.Length - 1];
            if (HazDir > 0 ? tail > Main.R + 160 : tail < Main.L - 160) HazT = 0;
        }

        // ---------------------------------------------------------------- lives, the end
        void CheckBlast(BFighter f)
        {
            if (!f.Playing || f.St == BS.Enter) return;
            bool outSide = f.X < Stage.BlastL || f.X > Stage.BlastR || f.Y < Stage.BlastB;
            bool outTop = f.Y > Stage.BlastT && f.St == BS.Hurt;
            if (!outSide && !outTop) return;
            if (f.Victim >= 0) ReleaseVictim(f);
            if (f.Grabber >= 0) { BFighter g = F[f.Grabber]; g.Victim = -1; f.Grabber = -1; }
            double ex = Math.Max(Stage.BlastL, Math.Min(Stage.BlastR, f.X)), ey = Math.Max(Stage.BlastB, Math.Min(Stage.BlastT, f.Y));
            Ev("ko", outTop ? "top" : "", ex, ey, 0, f.Slot);
            Sfx(outTop ? "brawl-twinkle" : "brawl-ko", ex, 1);
            if (f.Last >= 0 && f.LastT < 480 && f.Last != f.Slot) { F[f.Last].KOs++; f.Falls++; }
            else f.SDs++;
            if (!TimeMode && !Practice) f.Stocks--;
            f.St = BS.Out; f.T = 0; f.Mv = null; f.X = ex; f.Y = ey;
            if (!TimeMode && f.Stocks <= 0)
            {
                int left = 0; foreach (BFighter o in F) if (o.Stocks > 0) left++;
                f.Place = left + 1;
            }
        }

        void CheckEnd()
        {
            if (Decided || Practice) return;
            if (TimeMode)
            {
                TimeLeft--;
                if (TimeLeft > 0) return;
                int best = int.MinValue; foreach (BFighter f in F) best = Math.Max(best, f.Score);
                var tied = new List<BFighter>(); foreach (BFighter f in F) if (f.Score == best) tied.Add(f);
                if (tied.Count == 1) { Decide(tied[0].Slot); return; }
                // sudden death: the tied ones only, one life each at 300%
                StartSudden(tied);
                return;
            }
            int alive = 0, who = -1;
            foreach (BFighter f in F) if (f.Stocks > 0) { alive++; who = f.Slot; }
            if (alive <= 1) Decide(alive == 1 ? who : -1);
        }

        void StartSudden(List<BFighter> tied)
        {
            Sudden = true; TimeMode = false;
            foreach (BFighter f in F)
            {
                bool inIt = tied.Contains(f);
                f.Stocks = inIt ? 1 : 0;
                if (!inIt) { f.St = BS.Out; f.T = 0; f.Place = tied.Count + 1; continue; }
                Spawn(f, false);
                f.Dmg = 300; f.Ground = true; f.On = Main; f.Y = Main.Top; f.St = BS.Idle;
            }
            foreach (BShot s in Shots) s.On = false;
            Ev("sudden", "", 0, 0, 0, -1); Sfx("brawl-sudden", 0, 1);
        }

        void Decide(int winner)
        {
            Decided = true; Winner = winner; OverT = 0;
            if (winner >= 0) F[winner].Place = 1;
            // places for everyone else: by lives, then score, then damage
            var rest = new List<BFighter>();
            foreach (BFighter f in F) if (f.Slot != winner && f.Place == 0) rest.Add(f);
            rest.Sort((x, y) => x.Stocks != y.Stocks ? y.Stocks.CompareTo(x.Stocks) : x.Score != y.Score ? y.Score.CompareTo(x.Score) : x.Dmg.CompareTo(y.Dmg));
            int place = winner >= 0 ? 2 : 1;
            foreach (BFighter f in rest) f.Place = place++;
            Ev("game", "", 0, 0, 0, winner);
            Sfx("brawl-game", 0, 1);
        }

        /// <summary>A number that changes when anything about the match changes (tests: the same buttons give the same match).</summary>
        public long Checksum()
        {
            long h = 17;
            Func<double, long> q = v => (long)Math.Round(v * 64);
            foreach (BFighter f in F) h = h * 31 + q(f.X) * 7 + q(f.Y) * 13 + q(f.Dmg) * 3 + (int)f.St * 101 + f.Stocks * 977 + f.MF + f.Buf[0] + f.Buf[1] * 3 + f.Buf[2] * 5 + f.Buf[5] * 7 + f.Chain * 11;
            foreach (BShot s in Shots) if (s.On) h = h * 31 + q(s.X) + q(s.Y) * 5;
            return h ^ Frame;
        }
    }
}
