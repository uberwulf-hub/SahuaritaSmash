// Classic Games - Sahuarita Smash!, part 1: the fighters, their moves and the stages, all as data. A platform fighter in the
// spirit of the 1999 classic (Super Smash Bros.) with its own cast (Andrew, 2026-10-29: "a mix of all of them"): stars
// of the other Classic Games, the Arizona desert, long-gone historical figures, the server room and a folk legend.
// Nothing here is Nintendo's: no characters, stages, items, music or names. Damage is a percentage; the higher it is,
// the farther a hit sends you; leave the screen past a stage's edges and you lose a life. Nobody is hurt: fighters are
// bonked and fly off with a twinkle.
//
// Units: world pixels, y up, a fighter's X / Y is the middle of its feet; a move's frames count from 1 at 60 a second;
// a hit's X is forward of the fighter (it flips with the facing), its angle 0 = forward, 90 = up, 180 = backward,
// 270 = down, and 361 = "forward and a little up" (low on the ground for weak hits). Knockback (see BrawlSim.Launch) =
// ((p/10 + p*d/20) * 200/(weight+100) * 1.4 + 18) * grow/100 + base, where p is the damage after the hit and d the hit's
// damage, the same shape the old fighting games use, so damage, weight and the hit all matter.
using System;
using System.Collections.Generic;

namespace ClassicGames
{
    public sealed class BHit
    {
        public int From, To;
        public double X, Y, R, Dmg, Ang = 361, Base, Grow;
        public bool Grab;
        /// <summary>Hits again every this many frames while active (multi-hit moves); 0 = once per move.</summary>
        public int Rehit;
        public bool Electric;
        /// <summary>Hits that are out at the same time share a group and land once between them; a later hit (a
        /// multi-hit move's last blow) is its own group (set by BrawlData once the moves are made).</summary>
        public int Group;
    }

    public sealed class BShotDef
    {
        public double Vx, Vy, Grav, R = 12, Dmg, Ang = 361, Base, Grow;
        public int Life = 60;
        /// <summary>Frames it hangs harmless where it appears first (a lightning strike's warning cloud).</summary>
        public int Delay;
        /// <summary>Picture: star, cloud, bolt, book, packet, wall, needle, pancake, log, rock, grenade, pellet.</summary>
        public string Look = "star";
        public bool Boomerang, Explode, Wall, Rolls;
        public double ExplodeR;
        /// <summary>Offset where it starts (forward, up).</summary>
        public double SX = 30, SY = 45;
        public int Rehit;
        public bool Electric;
    }

    public sealed class BMove
    {
        public string Id = "", Anim = "jab", Name = "";
        public int Len = 20;
        public readonly List<BHit> Hits = new List<BHit>();
        /// <summary>Kind: "" plain, "rise" (up special: helpless after in the air), "lunge" (side special: once in the air),
        /// "dive" (falls fast, a shockwave on landing), "counter", "reflect", "cmdgrab" (a hit that grabs and throws),
        /// "throw" (a grab's throw), "grab".</summary>
        public string Kind = "";
        public double Vx, Vy, Drift;
        public int VFrom, VTo;
        public bool Float;   // no gravity while VFrom..VTo
        public BShotDef Shot; public int ShotAt; public double[] ShotAngles;
        public int LandLag = 8;
        public string Sound = "brawl-swing"; public int SoundAt = 1;
        /// <summary>Strong attacks: held at this frame while the button is held (up to a second), harder the longer.</summary>
        public int ChargeAt;
        public bool Helpless;
        public int CounterFrom, CounterTo;
        public int ReflectFrom, ReflectTo;
        /// <summary>A shockwave when a dive lands.</summary>
        public BHit LandHit;
        /// <summary>A command grab's throw.</summary>
        public BHit ThrowHit;
        /// <summary>Something extra the picture shows: moose, kite, ox, drill, jet, bubble, copter, tendril, spark.</summary>
        public string Extra = "";
        /// <summary>The first frame a hit can land (for the computer players and the tests).</summary>
        public int LastActive { get { int f = 0; foreach (BHit h in Hits) f = Math.Max(f, h.To); if (Shot != null) f = Math.Max(f, ShotAt); return f; } }
        public int FirstActive { get { int f = 999; foreach (BHit h in Hits) f = Math.Min(f, h.From); if (Shot != null) f = Math.Min(f, ShotAt); return f == 999 ? 0 : f; } }
        public double Reach { get { double r = 0; foreach (BHit h in Hits) r = Math.Max(r, Math.Abs(h.X) + h.R); return r; } }

        public BMove H(int from, int to, double x, double y, double r, double dmg, double ang, double bas, double grow)
        {
            Hits.Add(new BHit { From = from, To = to, X = x, Y = y, R = r, Dmg = dmg, Ang = ang, Base = bas, Grow = grow });
            return this;
        }
        public BMove Multi(int rehit) { Hits[Hits.Count - 1].Rehit = rehit; return this; }
        public BMove Zap() { Hits[Hits.Count - 1].Electric = true; return this; }
        public BMove V(int from, int to, double vx, double vy) { VFrom = from; VTo = to; Vx = vx; Vy = vy; return this; }
        public BMove S(string sound, int at) { Sound = sound; SoundAt = at; return this; }
    }

    public sealed class BFighterDef
    {
        public string Id, Name, Group, Blurb, From;
        public double Weight = 100, Height = 90, Width = 46;
        public double Walk = 3.4, Run = 7.2, Air = 5.2, AirAcc = 0.45, Grav = 0.65, Fall = 11, Jump = 14.5, DJump = 13.5;
        public int Jumps = 2;
        public uint Main, Second, Skin, Trim, Dark;
        public readonly Dictionary<string, BMove> Moves = new Dictionary<string, BMove>();
        public string[] SpecialNames = new string[4];
        public BMove M(string id) { BMove m; return Moves.TryGetValue(id, out m) ? m : null; }
        public double Scale { get { return Height / 90.0; } }
    }

    public sealed class BPlat
    {
        public double X1, X2, Y;
        /// <summary>Solid: the main ground, with sides and an underneath (Thick down) and ledges at both ends.</summary>
        public bool Solid; public double Thick = 140;
        /// <summary>Moving platforms: offset = (MoveX, MoveY) * sin(2 pi t / Period).</summary>
        public double MoveX, MoveY, Period;
        public double OX, OY, DX, DY;   // current offset and its change this frame (BrawlSim)
        /// <summary>How it is drawn (pictures only): ledge, arch, saguaro, terrace, lift, bucket, rack, tray, rock, crystal, tank,
        /// catwalk, gantry; "" = a plain slab.</summary>
        public string Look = "";
        public double L { get { return X1 + OX; } }
        public double R { get { return X2 + OX; } }
        public double Top { get { return Y + OY; } }
    }

    public sealed class BStageDef
    {
        public string Id, Name, Blurb, From;
        public readonly List<BPlat> Plats = new List<BPlat>();
        public double BlastL = -940, BlastR = 940, BlastT = 860, BlastB = -500;
        public double Gravity = 1;
        /// <summary>The stage's surprise (when Surprises is on): wind (monsoon gusts), cart (a mine cart along the
        /// ground), meteor (pebbles from the sky), laser (the lab's sweeping beam), javelina (a herd stampeding across the
        /// ground), pecans (the orchard's tree shaker: nuts fall where their shadows show) or "" (none). Several, split by spaces, take turns at random.</summary>
        public string Hazard = "";
        public int Song;
        public uint Sky1, Sky2, Ground, GroundTop, Accent;
    }

    public static class BrawlData
    {
        public static readonly List<BFighterDef> Fighters = new List<BFighterDef>();
        public static readonly List<BStageDef> Stages = new List<BStageDef>();
        public static readonly string[] Groups = { "Classic Games All-Stars", "Arizona", "History", "The server room", "Legends" };

        static BrawlData()
        {
            MakeFighters(); BrawlSignatures.Apply(Fighters); MakeStages();
            foreach (BFighterDef f in Fighters)
                foreach (BMove m in f.Moves.Values)
                {
                    int next = 0;
                    for (int i = 0; i < m.Hits.Count; i++)
                    {
                        BHit h = m.Hits[i]; h.Group = -1;
                        for (int j = 0; j < i; j++) if (m.Hits[j].From <= h.To && h.From <= m.Hits[j].To) { h.Group = m.Hits[j].Group; break; }
                        if (h.Group < 0) h.Group = Math.Min(3, next++);
                    }
                }
        }

        public static BFighterDef Fighter(string id) { foreach (BFighterDef f in Fighters) if (f.Id == id) return f; return null; }
        public static int FighterIndex(string id) { for (int i = 0; i < Fighters.Count; i++) if (Fighters[i].Id == id) return i; return -1; }
        public static int StageIndex(string id) { for (int i = 0; i < Stages.Count; i++) if (Stages[i].Id == id) return i; return -1; }

        // ---------------------------------------------------------------- the ordinary moves, shaped by size and style
        /// <summary>The moves every fighter has. k = size, reach = how far its arms (or stick) go, pow = how hard it
        /// hits, style picks the pictures: "fist", "kick" (birds), "tendril", "stick" (anything held).</summary>
        static void Normals(BFighterDef f, double reach, double pow, string style)
        {
            double k = f.Scale, r = reach * k;
            bool held = style == "stick";
            string sw = held ? "swing" : "jab";
            Func<string, string, int, BMove> add = (id, anim, len) => { var m = new BMove { Id = id, Anim = anim, Len = len }; f.Moves[id] = m; return m; };
            add("jab", held ? "poke" : (style == "kick" ? "peck" : "jab"), 16).H(4, 6, 30 * r, 50 * k, 15 * k, 3 * pow, 361, 12, 25).S("brawl-swish", 3);
            add("ftilt", held ? "swing" : (style == "kick" ? "kick" : "punch"), 26).H(7, 10, 42 * r, 46 * k, 17 * k, 8 * pow, 361, 22, 80).S("brawl-swing", 5);
            add("utilt", "upswing", 26).H(6, 11, 12 * k, 95 * k, 21 * k, 7 * pow, 86, 26, 92).S("brawl-swing", 5);
            add("dtilt", "lowkick", 20).H(5, 8, 40 * r, 10 * k, 16 * k, 6 * pow, 74, 30, 52).S("brawl-swish", 4);
            add("dash", "dash", 34).H(6, 14, 30 * k, 40 * k, 21 * k, 9 * pow, 50, 40, 70).V(1, 18, 0, 0).S("brawl-swing", 5);
            f.Moves["dash"].Kind = "dashatk";
            var fs = add("fstrong", held ? "bigswing" : "bigpunch", 48).H(16, 19, 50 * r, 45 * k, 23 * k, 16 * pow, 361, 30, 100).S("brawl-strong", 15);
            fs.ChargeAt = 9;
            var us = add("ustrong", "uppercut", 46).H(13, 18, 8 * k, 104 * k, 26 * k, 15 * pow, 88, 32, 98).S("brawl-strong", 12);
            us.ChargeAt = 8;
            var ds = add("dstrong", "splits", 44).H(12, 14, 42 * r, 10 * k, 19 * k, 14 * pow, 30, 30, 95).H(12, 14, -42 * r, 10 * k, 19 * k, 14 * pow, 150, 30, 95).S("brawl-strong", 11);
            ds.ChargeAt = 7;
            var na = add("nair", "spin", 32).H(5, 20, 0, 45 * k, 32 * k, 8 * pow, 361, 15, 80).S("brawl-swish", 4); na.LandLag = 6;
            var fa = add("fair", held ? "overhead" : "airpunch", 36).H(10, 14, 42 * r, 46 * k, 21 * k, 12 * pow, 361, 25, 95).S("brawl-swing", 8); fa.LandLag = 12;
            var ba = add("bair", "backkick", 30).H(7, 11, -42 * k, 40 * k, 19 * k, 11 * pow, 145, 22, 95).S("brawl-swing", 6); ba.LandLag = 10;
            var ua = add("uair", "flip", 30).H(6, 12, 0, 100 * k, 23 * k, 9 * pow, 86, 20, 90).S("brawl-swish", 5); ua.LandLag = 8;
            var da = add("dair", "stomp", 38).H(12, 17, 0, -2 * k, 21 * k, 12 * pow, 285, 20, 80).S("brawl-swing", 10); da.LandLag = 15;
            var g = add("grab", "grab", 30); g.Kind = "grab"; g.H(6, 8, 32 * r, 42 * k, 19 * k, 0, 0, 0, 0); g.Hits[0].Grab = true; g.Sound = "brawl-swish"; g.SoundAt = 5;
            Throw(f, "fthrow", "throwf", 8 * pow, 40, 60, 62);
            Throw(f, "bthrow", "throwb", 10 * pow, 140, 60, 70);
            Throw(f, "uthrow", "throwu", 7 * pow, 90, 70, 60);
            Throw(f, "dthrow", "throwd", 6 * pow, 70, 50, 40);
            if (style == "tendril") foreach (BMove m in f.Moves.Values) m.Extra = m.Extra == "" ? "tendril" : m.Extra;
        }

        static void Throw(BFighterDef f, string id, string anim, double dmg, double ang, double bas, double grow)
        {
            var m = new BMove { Id = id, Anim = anim, Len = 26, Kind = "throw", Sound = "brawl-throw", SoundAt = 12 };
            m.ThrowHit = new BHit { From = 12, To = 12, Dmg = dmg, Ang = ang, Base = bas, Grow = grow };
            f.Moves[id] = m;
        }

        static BMove Special(BFighterDef f, string id, string name, string anim, int len)
        {
            var m = new BMove { Id = id, Name = name, Anim = anim, Len = len, Sound = "brawl-special", SoundAt = 4 };
            f.Moves[id] = m;
            int slot = id == "nspec" ? 0 : id == "sspec" ? 1 : id == "uspec" ? 2 : 3;
            f.SpecialNames[slot] = name;
            return m;
        }

        static BFighterDef New(string id, string name, string group, string from, string blurb)
        {
            var f = new BFighterDef { Id = id, Name = name, Group = group, From = from, Blurb = blurb };
            Fighters.Add(f);
            return f;
        }

        // ---------------------------------------------------------------- the cast
        static void MakeFighters()
        {
            BFighterDef f; BMove m;

            // ---- Classic Games All-Stars
            f = New("bloom", "Bloom", Groups[0], "from Bloom", "The kid in the flower helmet. Light and quick, with stars to throw and a bubble that bounces shots back.");
            f.Weight = 88; f.Height = 78; f.Width = 40; f.Walk = 3.6; f.Run = 7.6; f.Air = 5.6; f.Grav = 0.62; f.Fall = 10; f.Jump = 14.5; f.DJump = 13.5;
            f.Main = 0xFF2DD4BF; f.Second = 0xFFF472B6; f.Skin = 0xFFF2C29A; f.Trim = 0xFFFDE047; f.Dark = 0xFF0F5E57;
            Normals(f, 1.0, 0.95, "fist");
            m = Special(f, "nspec", "Star shot", "cast", 34); m.Shot = new BShotDef { Vx = 9, Life = 55, R = 13, Dmg = 6, Base = 20, Grow = 40, Look = "star" }; m.ShotAt = 12; m.Sound = "brawl-star"; m.SoundAt = 12;
            m = Special(f, "sspec", "Rainbow dash", "dash", 40); m.Kind = "lunge"; m.V(8, 22, 13, 0).H(8, 22, 25, 40, 24, 10, 40, 40, 60); m.Extra = "rainbow"; m.Sound = "brawl-dash";
            m = Special(f, "uspec", "Petal-copter", "rise", 42); m.Kind = "rise"; m.V(6, 32, 0, 13.5); m.Drift = 3.5; m.Float = true; m.Helpless = true; m.Extra = "copter";
            m.H(6, 28, 0, 72, 30, 2, 85, 20, 10).Multi(5).H(30, 32, 0, 76, 34, 4, 85, 45, 80); m.Sound = "brawl-copter";
            m = Special(f, "dspec", "Bubble shield", "guard", 40); m.Kind = "reflect"; m.ReflectFrom = 5; m.ReflectTo = 30; m.H(5, 7, 0, 40, 46, 3, 45, 50, 20); m.Extra = "bubble"; m.Sound = "brawl-bubble";

            f = New("specimen", "Specimen", Groups[0], "from Specimen Escape", "A hungry tangle of red tendrils, out of its lab for the day. Long reach, a lash that grabs from afar, three jumps.");
            f.Weight = 96; f.Height = 80; f.Width = 54; f.Walk = 3.2; f.Run = 7; f.Air = 5; f.Grav = 0.6; f.Fall = 10.5; f.Jump = 14; f.DJump = 12; f.Jumps = 3;
            f.Main = 0xFFC0262D; f.Second = 0xFF7A1018; f.Skin = 0xFFE24A4F; f.Trim = 0xFFFDE047; f.Dark = 0xFF4A0A10;
            Normals(f, 1.3, 1.0, "tendril");
            m = Special(f, "nspec", "Lash", "lash", 36); m.H(12, 15, 95, 45, 19, 11, 361, 30, 85).H(12, 15, 52, 45, 17, 9, 361, 25, 80); m.Extra = "tendril"; m.Sound = "brawl-lash"; m.SoundAt = 11;
            m = Special(f, "sspec", "Grab-lash", "lash", 46); m.Kind = "cmdgrab"; m.H(14, 17, 105, 45, 21, 0, 0, 0, 0); m.Hits[0].Grab = true;
            m.ThrowHit = new BHit { Dmg = 10, Ang = 40, Base = 60, Grow = 72 }; m.Extra = "tendril"; m.Sound = "brawl-lash"; m.SoundAt = 13;
            m = Special(f, "uspec", "Tendril zip", "zip", 40); m.Kind = "rise"; m.V(10, 28, 9, 16); m.Drift = 2.5; m.Float = true; m.Helpless = true; m.Extra = "tendril";
            m.H(26, 30, 20, 80, 28, 8, 70, 40, 70); m.Sound = "brawl-lash"; m.SoundAt = 9;
            m = Special(f, "dspec", "Burst", "burst", 50); m.H(10, 13, 34, 40, 44, 14, 50, 50, 80).H(10, 13, -34, 40, 44, 14, 130, 50, 80); m.Extra = "burst"; m.Sound = "brawl-burst"; m.SoundAt = 9;

            f = New("trooper", "Trooper", Groups[0], "from Hive Purge", "The base's last soldier standing, on a day off. A quick blaster, grenades that pop, and jump jets.");
            f.Weight = 108; f.Height = 92; f.Width = 46; f.Walk = 3.3; f.Run = 7.4; f.Air = 5; f.Grav = 0.72; f.Fall = 12; f.Jump = 15; f.DJump = 14;
            f.Main = 0xFF4D5B3A; f.Second = 0xFF8A9277; f.Skin = 0xFFC9926A; f.Trim = 0xFF67E8F9; f.Dark = 0xFF262D1C;
            Normals(f, 1.0, 1.05, "fist");
            m = Special(f, "nspec", "Blaster", "aim", 22); m.Shot = new BShotDef { Vx = 16, Life = 40, R = 8, Dmg = 4, Ang = 0, Base = 8, Grow = 20, Look = "pellet", SY = 52 }; m.ShotAt = 8; m.Sound = "brawl-blaster"; m.SoundAt = 8;
            m = Special(f, "sspec", "Grenade", "throw", 34); m.Shot = new BShotDef { Vx = 7, Vy = 9, Grav = 0.45, Life = 90, R = 10, Dmg = 12, Ang = 60, Base = 45, Grow = 75, Look = "grenade", Explode = true, ExplodeR = 58 }; m.ShotAt = 14; m.Sound = "brawl-throw"; m.SoundAt = 13;
            m = Special(f, "uspec", "Jump jets", "rise", 40); m.Kind = "rise"; m.V(8, 32, 0, 15); m.Drift = 4.6; m.Float = true; m.Helpless = true; m.Extra = "jet";
            m.H(8, 14, 0, -6, 24, 6, 80, 40, 50); m.Sound = "brawl-jet"; m.SoundAt = 7;
            m = Special(f, "dspec", "Dive slam", "dive", 60); m.Kind = "dive"; m.V(10, 60, 0, -18); m.H(10, 60, 0, 10, 26, 10, 285, 20, 60);
            m.LandHit = new BHit { X = 0, Y = 10, R = 64, Dmg = 11, Ang = 60, Base = 45, Grow = 70 }; m.Sound = "brawl-swing"; m.SoundAt = 9;

            f = New("juniper", "Juniper", Groups[0], "from Spaceareum", "The travelling trader from Spaceareum, never without her jetpack and drill. Floaty, with the longest flight home.");
            f.Weight = 85; f.Height = 82; f.Width = 42; f.Walk = 3.8; f.Run = 7.2; f.Air = 6; f.Grav = 0.5; f.Fall = 8.5; f.Jump = 13.5; f.DJump = 12.5;
            f.Main = 0xFFF59E0B; f.Second = 0xFFE2E8F0; f.Skin = 0xFF9C6644; f.Trim = 0xFF93C5FD; f.Dark = 0xFF7C4A03;
            Normals(f, 1.05, 0.95, "fist");
            m = Special(f, "nspec", "Drill", "drill", 46); m.H(10, 34, 44, 45, 22, 1.5, 0, 6, 10).Multi(4).H(36, 37, 46, 45, 26, 4, 361, 40, 80); m.Extra = "drill"; m.Sound = "brawl-drill"; m.SoundAt = 9;
            m = Special(f, "sspec", "Ore toss", "throw", 30); m.Shot = new BShotDef { Vx = 9, Vy = 7, Grav = 0.4, Life = 70, R = 12, Dmg = 9, Ang = 45, Base = 30, Grow = 70, Look = "rock" }; m.ShotAt = 12; m.Sound = "brawl-throw"; m.SoundAt = 11;
            m = Special(f, "uspec", "Jetpack", "rise", 66); m.Kind = "rise"; m.V(6, 62, 0, 10); m.Drift = 4.2; m.Float = true; m.Helpless = true; m.Extra = "jet";
            m.H(6, 62, 0, -10, 20, 2, 80, 20, 20).Multi(8); m.Sound = "brawl-jet"; m.SoundAt = 5;
            m = Special(f, "dspec", "Ground pound", "dive", 56); m.Kind = "dive"; m.V(12, 56, 0, -16); m.H(12, 56, 0, 6, 22, 8, 285, 15, 50);
            m.LandHit = new BHit { X = 0, Y = 8, R = 56, Dmg = 9, Ang = 70, Base = 40, Grow = 60 }; m.Sound = "brawl-swing"; m.SoundAt = 11;

            // ---- Arizona
            f = New("dusty", "Dusty", Groups[1], "a Sonoran roadrunner", "The fastest thing on two legs between Tucson and Nogales. Light, with kicks, a zoom across the stage and a dust devil.");
            f.Weight = 76; f.Height = 84; f.Width = 36; f.Walk = 4.5; f.Run = 11; f.Air = 6.5; f.AirAcc = 0.6; f.Grav = 0.75; f.Fall = 13; f.Jump = 16; f.DJump = 14;
            f.Main = 0xFF8B6B47; f.Second = 0xFFE7D3B1; f.Skin = 0xFFF2B33D; f.Trim = 0xFF3B5BDB; f.Dark = 0xFF4A3520;
            Normals(f, 1.0, 0.9, "kick");
            m = Special(f, "nspec", "Dust devil", "cast", 30); m.Shot = new BShotDef { Vx = 5, Life = 64, R = 20, Dmg = 5, Ang = 80, Base = 40, Grow = 30, Look = "cloud", SY = 30, Rehit = 0 }; m.ShotAt = 10; m.Sound = "brawl-whoosh"; m.SoundAt = 9;
            m = Special(f, "sspec", "Zoom", "dash", 34); m.Kind = "lunge"; m.V(6, 18, 20, 0).H(6, 18, 20, 40, 24, 9, 30, 40, 60); m.Extra = "dust"; m.Sound = "brawl-dash";
            m = Special(f, "uspec", "Flutter", "flap", 42); m.Kind = "rise"; m.V(6, 36, 0, 13); m.Drift = 5; m.Float = true; m.Helpless = true;
            m.H(6, 34, 0, 60, 26, 2, 85, 20, 10).Multi(7); m.Sound = "brawl-flap"; m.SoundAt = 5;
            m = Special(f, "dspec", "Peck drill", "dive", 52); m.Kind = "dive"; m.V(8, 52, 6, -19); m.H(8, 52, 10, 6, 22, 9, 290, 20, 60);
            m.LandHit = new BHit { X = 0, Y = 8, R = 46, Dmg = 7, Ang = 70, Base = 40, Grow = 50 }; m.Sound = "brawl-swing"; m.SoundAt = 7;

            f = New("sam", "Saguaro Sam", Groups[1], "a saguaro giant", "Two hundred years old and in no hurry. Heavy and slow, with long arms, a volley of needles and a flower that blooms on the way up.");
            f.Weight = 126; f.Height = 122; f.Width = 56; f.Walk = 2.6; f.Run = 5.6; f.Air = 4.4; f.Grav = 0.7; f.Fall = 11.5; f.Jump = 14; f.DJump = 12.5;
            f.Main = 0xFF3F8A3A; f.Second = 0xFF2B6328; f.Skin = 0xFF5FAE55; f.Trim = 0xFFFFF7E0; f.Dark = 0xFF1B3D19;
            Normals(f, 1.05, 1.15, "fist");
            m = Special(f, "nspec", "Needle volley", "cast", 36); m.Shot = new BShotDef { Vx = 12, Life = 36, R = 7, Dmg = 3, Ang = 361, Base = 12, Grow = 30, Look = "needle", SY = 70 }; m.ShotAt = 14; m.ShotAngles = new double[] { -9, 0, 9 }; m.Sound = "brawl-needles"; m.SoundAt = 14;
            m = Special(f, "sspec", "Arm swing", "bigswing", 50); m.H(15, 19, 78, 62, 30, 15, 361, 35, 92); m.Sound = "brawl-strong"; m.SoundAt = 14;
            m = Special(f, "uspec", "Spring bloom", "rise", 44); m.Kind = "rise"; m.V(8, 30, 0, 17); m.Drift = 2.8; m.Float = true; m.Helpless = true; m.Extra = "flower";
            m.H(26, 32, 0, 132, 36, 9, 86, 45, 70); m.Sound = "brawl-bloom"; m.SoundAt = 25;
            m = Special(f, "dspec", "Root slam", "dive", 54); m.Kind = "dive"; m.V(14, 54, 0, -16); m.H(14, 54, 0, 8, 26, 10, 285, 20, 60);
            m.LandHit = new BHit { X = 0, Y = 10, R = 84, Dmg = 13, Ang = 70, Base = 50, Grow = 70 }; m.Sound = "brawl-strong"; m.SoundAt = 12;

            // ---- History (long-gone figures, drawn as friendly caricatures)
            f = New("teddy", "Teddy", Groups[2], "Theodore Roosevelt, 1858-1919", "The 26th president: \"speak softly and carry a big stick\". Rides a moose, and shouts \"Bully!\" to hit back.");
            f.Weight = 106; f.Height = 92; f.Width = 48; f.Walk = 3.4; f.Run = 7.2; f.Air = 5.2; f.Grav = 0.68; f.Fall = 11; f.Jump = 14.8; f.DJump = 13.5;
            f.Main = 0xFFB59C6B; f.Second = 0xFF6B4F2A; f.Skin = 0xFFF0C8A0; f.Trim = 0xFF8B5A2B; f.Dark = 0xFF4A3A20;
            Normals(f, 1.15, 1.05, "stick");
            m = Special(f, "nspec", "Big stick", "bigswing", 50); m.H(16, 19, 72, 50, 26, 14, 361, 40, 85); m.Sound = "brawl-strong"; m.SoundAt = 15;
            m = Special(f, "sspec", "Moose charge", "ride", 56); m.Kind = "lunge"; m.V(10, 42, 11, 0).H(10, 42, 60, 30, 34, 13, 40, 55, 80); m.Extra = "moose"; m.Sound = "brawl-moose"; m.SoundAt = 9;
            m = Special(f, "uspec", "Rough Rider leap", "leap", 40); m.Kind = "rise"; m.V(8, 26, 4, 16.5); m.Drift = 3; m.Float = true; m.Helpless = true;
            m.H(8, 24, 20, 62, 26, 8, 70, 40, 70); m.Sound = "brawl-whoosh"; m.SoundAt = 7;
            m = Special(f, "dspec", "Bully!", "guard", 44); m.Kind = "counter"; m.CounterFrom = 6; m.CounterTo = 30; m.Sound = "brawl-counter"; m.SoundAt = 5;

            f = New("ben", "Ben Franklin", Groups[2], "Benjamin Franklin, 1706-1790", "Printer, inventor and kite flyer. Calls down lightning, throws his almanac and floats home on his kite.");
            f.Weight = 98; f.Height = 88; f.Width = 48; f.Walk = 3.0; f.Run = 6.6; f.Air = 5.4; f.Grav = 0.55; f.Fall = 9.5; f.Jump = 14; f.DJump = 13;
            f.Main = 0xFF7C4A2D; f.Second = 0xFFD8C7A0; f.Skin = 0xFFF0C8A0; f.Trim = 0xFFE5E7EB; f.Dark = 0xFF3E2414;
            Normals(f, 1.05, 1.0, "stick");
            m = Special(f, "nspec", "Kite lightning", "cast", 40); m.Shot = new BShotDef { Vx = 0, Life = 26, Delay = 16, R = 40, Dmg = 12, Ang = 70, Base = 40, Grow = 75, Look = "bolt", SX = 150, SY = 40, Electric = true }; m.ShotAt = 10; m.Extra = "kite"; m.Sound = "brawl-zap"; m.SoundAt = 26;
            m = Special(f, "sspec", "Almanac", "throw", 30); m.Shot = new BShotDef { Vx = 10, Vy = 2, Grav = 0.15, Life = 50, R = 13, Dmg = 8, Base = 30, Grow = 60, Look = "book" }; m.ShotAt = 12; m.Sound = "brawl-throw"; m.SoundAt = 11;
            m = Special(f, "uspec", "Kite lift", "kite", 76); m.Kind = "rise"; m.V(6, 72, 0, 8.5); m.Drift = 4; m.Float = true; m.Helpless = true; m.Extra = "kite";
            m.H(60, 72, 0, 110, 26, 5, 85, 40, 60).Zap(); m.Sound = "brawl-whoosh"; m.SoundAt = 5;
            m = Special(f, "dspec", "Key spark", "guard", 44); m.H(10, 30, 0, 22, 52, 2, 80, 20, 10).Multi(6).Zap().H(32, 34, 0, 22, 56, 5, 80, 45, 60).Zap(); m.Extra = "spark"; m.Sound = "brawl-zap"; m.SoundAt = 9;

            // ---- the server room
            f = New("knight", "Firewall Knight", Groups[3], "from the server room", "Guards the family network. Heavy armour, a glowing data blade, and a wall of fire that stops anything thrown at it.");
            f.Weight = 115; f.Height = 96; f.Width = 52; f.Walk = 3.0; f.Run = 6.4; f.Air = 4.8; f.Grav = 0.74; f.Fall = 12; f.Jump = 14.5; f.DJump = 12.8;
            f.Main = 0xFF64748B; f.Second = 0xFF334155; f.Skin = 0xFF94A3B8; f.Trim = 0xFFF97316; f.Dark = 0xFF1E293B;
            Normals(f, 1.2, 1.05, "stick");
            m = Special(f, "nspec", "Packet", "cast", 30); m.Shot = new BShotDef { Vx = 11, Life = 45, R = 12, Dmg = 6, Base = 15, Grow = 50, Look = "packet" }; m.ShotAt = 10; m.Sound = "brawl-packet"; m.SoundAt = 10;
            m = Special(f, "sspec", "Firewall", "cast", 40); m.Shot = new BShotDef { Vx = 0, Life = 150, R = 30, Dmg = 6, Ang = 45, Base = 40, Grow = 40, Look = "wall", Wall = true, SX = 72, SY = 46, Rehit = 40 }; m.ShotAt = 12; m.Sound = "brawl-fire"; m.SoundAt = 12;
            m = Special(f, "uspec", "Uplink", "rise", 40); m.Kind = "rise"; m.V(8, 30, 0, 15); m.Drift = 3; m.Float = true; m.Helpless = true; m.Extra = "beam";
            m.H(8, 28, 0, 64, 28, 3, 85, 20, 10).Multi(6).H(30, 32, 0, 70, 32, 6, 85, 45, 70); m.Sound = "brawl-uplink"; m.SoundAt = 7;
            m = Special(f, "dspec", "Reboot spin", "spin", 44); m.H(8, 26, 0, 48, 54, 3, 361, 15, 20).Multi(6).H(28, 30, 0, 48, 58, 6, 50, 45, 70); m.V(8, 30, 0, 0); m.Drift = 2.5; m.Sound = "brawl-spin"; m.SoundAt = 7;

            // ---- legends (folk tales, free for everyone)
            f = New("paul", "Paul Bunyan", Groups[4], "the giant lumberjack of the tall tales", "Big as a barn and twice as strong. Throws flapjacks that come back, rolls logs, and Babe the blue ox gives him a boost.");
            f.Weight = 132; f.Height = 132; f.Width = 62; f.Walk = 2.8; f.Run = 6; f.Air = 4.2; f.Grav = 0.78; f.Fall = 12.5; f.Jump = 15; f.DJump = 12.5;
            f.Main = 0xFFB91C1C; f.Second = 0xFF1E3A8A; f.Skin = 0xFFF0C8A0; f.Trim = 0xFF7C4A1E; f.Dark = 0xFF450A0A;
            Normals(f, 1.15, 1.15, "stick");
            m = Special(f, "nspec", "Flapjack", "throw", 34); m.Shot = new BShotDef { Vx = 11, Life = 80, R = 16, Dmg = 7, Base = 25, Grow = 50, Look = "pancake", Boomerang = true, SY = 70 }; m.ShotAt = 12; m.Sound = "brawl-throw"; m.SoundAt = 11;
            m = Special(f, "sspec", "Log roll", "throw", 36); m.Shot = new BShotDef { Vx = 8, Vy = 0, Grav = 0.6, Life = 90, R = 20, Dmg = 11, Ang = 50, Base = 45, Grow = 70, Look = "log", Rolls = true, SY = 20 }; m.ShotAt = 14; m.Sound = "brawl-throw"; m.SoundAt = 13;
            m = Special(f, "uspec", "Babe's boost", "leap", 42); m.Kind = "rise"; m.V(8, 26, 3, 18.5); m.Drift = 2.6; m.Float = true; m.Helpless = true; m.Extra = "ox";
            m.H(22, 28, 0, 140, 32, 10, 86, 45, 70); m.Sound = "brawl-moose"; m.SoundAt = 6;
            m = Special(f, "dspec", "Timber stomp", "dive", 58); m.Kind = "dive"; m.V(14, 58, 0, -17); m.H(14, 58, 0, 10, 30, 11, 285, 20, 60);
            m.LandHit = new BHit { X = 0, Y = 10, R = 96, Dmg = 15, Ang = 70, Base = 55, Grow = 75 }; m.Sound = "brawl-strong"; m.SoundAt = 13;

            // ---- Arizona, added later (kept last so the fighters' numbers in saved matches stay the same)
            f = New("gila", "Gila Gus", Groups[1], "a Gila monster", "A beaded orange-and-black lizard from the washes near home, slow and heavy and in no hurry. A tongue that flicks, a bite that won't let go, and a tail that sweeps the ground.");
            f.Weight = 122; f.Height = 74; f.Width = 64; f.Walk = 2.7; f.Run = 6.3; f.Air = 4.6; f.Grav = 0.74; f.Fall = 12; f.Jump = 14.6; f.DJump = 13;
            f.Main = 0xFFF08A3C; f.Second = 0xFF2A1A14; f.Skin = 0xFFF6B27A; f.Trim = 0xFFFDE7C8; f.Dark = 0xFF1A0F0A;
            Normals(f, 1.1, 1.12, "fist");
            m = Special(f, "nspec", "Tongue flick", "lash", 26); m.H(8, 11, 70, 32, 15, 7, 361, 26, 70); m.Extra = "tongue"; m.Sound = "brawl-hiss"; m.SoundAt = 7;
            m = Special(f, "sspec", "Hang-on bite", "headbutt", 48); m.Kind = "cmdgrab"; m.V(6, 16, 7, 0); m.H(12, 16, 42, 30, 22, 0, 0, 0, 0); m.Hits[0].Grab = true;
            m.ThrowHit = new BHit { Dmg = 13, Ang = 35, Base = 60, Grow = 74 }; m.Extra = "chomp"; m.Sound = "brawl-chomp"; m.SoundAt = 11;
            m = Special(f, "uspec", "Rock hop", "leap", 42); m.Kind = "rise"; m.V(8, 28, 3, 17); m.Drift = 3; m.Float = true; m.Helpless = true;
            m.H(8, 24, 10, 50, 26, 8, 75, 40, 70); m.Sound = "brawl-whoosh"; m.SoundAt = 7;
            m = Special(f, "dspec", "Tail sweep", "spin", 46); m.H(8, 28, 0, 18, 64, 3, 30, 15, 20).Multi(6).H(30, 32, 0, 18, 70, 7, 30, 45, 72); m.V(8, 32, 0, 0); m.Drift = 2; m.Extra = "tail"; m.Sound = "brawl-spin"; m.SoundAt = 7;
        }

        // ---------------------------------------------------------------- the stages
        static BStageDef Stage(string id, string name, string from, string blurb)
        {
            var s = new BStageDef { Id = id, Name = name, From = from, Blurb = blurb, Song = Stages.Count };
            Stages.Add(s);
            return s;
        }
        static void Ground(BStageDef s, double half) { s.Plats.Add(new BPlat { X1 = -half, X2 = half, Y = 0, Solid = true }); }
        static BPlat Soft(BStageDef s, double x1, double x2, double y, string look)
        {
            var p = new BPlat { X1 = x1, X2 = x2, Y = y, Look = look };
            s.Plats.Add(p);
            return p;
        }
        static void Moving(BStageDef s, double x1, double x2, double y, string look, double mx, double my, double period)
        {
            BPlat p = Soft(s, x1, x2, y, look); p.MoveX = mx; p.MoveY = my; p.Period = period;
        }

        // Each stage has its own shape of platforms, drawn in its own way (BPlat.Look): sandstone ledges, a stone arch
        // and a saguaro's arm on the mesa; mine terraces, the lift and a swinging ore bucket in the pit; server racks
        // and a cable tray in the server room; drifting rocks and a crystal on the moon; specimen tanks, catwalks and a
        // gantry crane in the lab.
        static void MakeStages()
        {
            BStageDef s;
            s = Stage("mesa", "Sonoran Mesa", "Arizona", "A flat-topped mesa at sunset, saguaros all round. In a monsoon the wind gusts from one side (you'll see the dust coming first), and now and then a herd of javelinas stampedes across (listen for the snorting, then jump!).");
            Ground(s, 430); Soft(s, -330, -170, 112, "ledge"); Soft(s, 150, 300, 150, "ledge"); Soft(s, -80, 80, 262, "arch"); Soft(s, 318, 392, 268, "saguaro");
            s.Hazard = "wind javelina"; s.Sky1 = 0xFFFB923C; s.Sky2 = 0xFF7C3AED; s.Ground = 0xFF9A4A2A; s.GroundTop = 0xFFD9824B; s.Accent = 0xFF2F6B35;

            s = Stage("copper", "Copper Pit", "near Sahuarita", "The terraces of an open copper mine. A lift goes up and down in the middle, an ore bucket swings along its cable, and an ore cart rattles across now and then (its bell rings first).");
            Ground(s, 470); Soft(s, -455, -290, 105, "terrace"); Soft(s, -430, -335, 210, "terrace"); Soft(s, 320, 455, 120, "terrace");
            Moving(s, -80, 80, 150, "lift", 0, 90, 360);
            Moving(s, 150, 260, 230, "bucket", 70, 0, 420);
            s.Hazard = "cart"; s.Sky1 = 0xFF7DD3FC; s.Sky2 = 0xFFFDE68A; s.Ground = 0xFF7C4A2D; s.GroundTop = 0xFFB87333; s.Accent = 0xFF57534E;

            s = Stage("server", "Server Room", "the family network", "Racks of blinking servers and cold air from the floor. Fight on top of racks of different heights and the cable tray above, and the cooling fans blow you upward (they hum louder first).");
            Ground(s, 400); Soft(s, -340, -230, 150, "rack"); Soft(s, -170, -60, 96, "rack"); Soft(s, 60, 170, 186, "rack"); Soft(s, 230, 340, 118, "rack"); Soft(s, -100, 100, 290, "tray");
            s.Hazard = "fans"; s.Sky1 = 0xFF0F172A; s.Sky2 = 0xFF1E293B; s.Ground = 0xFF334155; s.GroundTop = 0xFF64748B; s.Accent = 0xFF22D3EE;

            s = Stage("moon", "Pebblemoon", "Spaceareum", "A little moon from Spaceareum. Low gravity: everyone jumps higher and floats down slowly, and the rocks above drift about. Pebbles fall from the sky (their shadows show where).");
            Ground(s, 380); Moving(s, -270, -100, 150, "rock", 0, 24, 300); Moving(s, 70, 230, 215, "rock", 45, 0, 520); Moving(s, -50, 40, 300, "crystal", 0, 24, 380); Moving(s, 250, 330, 105, "rock", 0, 16, 260);
            s.Gravity = 0.72; s.BlastT = 940; s.Hazard = "meteor"; s.Sky1 = 0xFF020617; s.Sky2 = 0xFF1E1B4B; s.Ground = 0xFF52525B; s.GroundTop = 0xFFA1A1AA; s.Accent = 0xFF818CF8;

            s = Stage("lab", "The Lab", "Specimen Escape", "The lab where Specimen grew up, now empty for the weekend. Stand on the specimen tanks at the sides, the catwalks or the gantry crane gliding overhead, and a scanner beam sweeps across (it blinks first).");
            Ground(s, 330); Soft(s, -470, -330, 60, "tank"); Soft(s, 330, 470, 60, "tank"); Soft(s, -255, -105, 165, "catwalk"); Soft(s, 105, 255, 165, "catwalk");
            Moving(s, -60, 60, 262, "gantry", 150, 0, 600);
            s.Hazard = "laser"; s.Sky1 = 0xFF111827; s.Sky2 = 0xFF3F1D24; s.Ground = 0xFF3F3F46; s.GroundTop = 0xFF71717A; s.Accent = 0xFFEF4444;

            s = Stage("orchard", "Pecan Orchard", "Sahuarita", "Rows of pecan trees by the Santa Cruz River, the town's own. Fight on the branches, the stacked harvest bins and the picker's basket going up and down. When the tree shaker rumbles, get out from under: pecans rain down where their shadows show.");
            Ground(s, 420); Soft(s, -380, -230, 120, "branch"); Soft(s, -140, 10, 225, "branch"); Soft(s, 250, 360, 76, "bins");
            Moving(s, 80, 190, 170, "picker", 0, 70, 420);
            s.Hazard = "pecans"; s.Sky1 = 0xFF93C5FD; s.Sky2 = 0xFFFDE68A; s.Ground = 0xFF5B3A24; s.GroundTop = 0xFF8A5A36; s.Accent = 0xFF3F7D3A;
        }
    }
}

namespace ClassicGames
{
    /// <summary>The game's name, for the engine (the music's names); the app's BrawlGame.GameName is this.</summary>
    public static class BrawlInfo
    {
        public const string GameName = "Sahuarita Smash!";
    }
}
