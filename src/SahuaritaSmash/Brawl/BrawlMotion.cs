// Classic Games - Sahuarita Smash!, how each fighter moves when it hits (Andrew, 2026-10-31: "make each character's
// hitting method unique ... rather their hitting movements"). Every attack's picture (Anim) gives a wind-up and a
// strike pose (BrawlView.AnimPoses); a fighter's MotionStyle reshapes both and the timing between them: Teddy boxes with
// his guard up and weaves, Ben lunges like a fencer, Paul and the Knight swing with both hands, Specimen whips and
// writhes, Dusty pecks with his whole head and scurries, Saguaro Sam is stiff and wobbles after, Bloom bounces, Juniper
// dances, the Trooper is crisp and economical. Pictures only: the match never reads any of this, so the hits, their
// timing and the balance are unchanged.
using System;
using System.Collections.Generic;

namespace ClassicGames
{
    public sealed class MotionStyle
    {
        /// <summary>How far each pose reaches from standing (cartoony fighters above 1, careful ones below).</summary>
        public double Amp = 1;
        /// <summary>Frames to snap from the wind-up to the strike (smaller is snappier; the hit's timing is unchanged).</summary>
        public double Snap = 2.2;
        /// <summary>The follow-through past the strike (1.18 is the plain one).</summary>
        public double Over = 1.18;
        /// <summary>Feet apart while attacking on the ground (degrees), and how much lower.</summary>
        public double Wide, Low;
        /// <summary>Extra lean into the strike (degrees).</summary>
        public double Lean;
        /// <summary>What the arm that isn't striking does: "" as drawn, "guard" (a boxer's fist by the chin), "two" (both
        /// hands on the weapon), "fencer" (raised behind, like a fencer's), "dancer" (out and up, graceful), "tucked"
        /// (held in close to the chest), "flung" (thrown out behind, cartoon style).</summary>
        public string Arms = "";
        /// <summary>Elbows: below 1 straighter (stiff, military), above 1 bendier.</summary>
        public double Elbow = 1;
        /// <summary>Elbows bent upward like a cactus's arms (degrees).</summary>
        public double Cactus;
        /// <summary>The front leg steps out on the strike and the back leg straightens (a fencer's lunge, degrees).</summary>
        public double Lunge;
        /// <summary>Weaving (lean side to side) during the wind-up, in degrees.</summary>
        public double Weave;
        /// <summary>The head thrusts forward with the strike (a peck, degrees).</summary>
        public double Peck;
        /// <summary>Feet scurrying during the attack (degrees).</summary>
        public double Scurry;
        /// <summary>Every limb writhing (degrees), and the elbows lagging like a whip in the snap (degrees).</summary>
        public double Writhe, Whip;
        /// <summary>A bounce in the knees while attacking (share of a crouch).</summary>
        public double Bounce;
        /// <summary>A wobble after the strike that dies away (degrees of lean), like a plant springing back.</summary>
        public double Wobble;
        /// <summary>A small turn of the whole body with the strike (degrees), a flourish.</summary>
        public double Flourish;
    }

    public sealed partial class BrawlView
    {
        static readonly MotionStyle plainMotion = new MotionStyle();
        static readonly Dictionary<string, MotionStyle> motions = new Dictionary<string, MotionStyle>
        {
            // a fairy-tale hero: big, bouncy, every move flung out and overshooting
            { "bloom", new MotionStyle { Amp = 1.22, Snap = 2.4, Over = 1.36, Bounce = 0.12, Flourish = 14, Arms = "flung", Elbow = 1.2, Lean = -4 } },
            // a tangle of ropes: low, writhing, whips that crack and lag behind
            { "specimen", new MotionStyle { Amp = 1.15, Snap = 1.5, Over = 1.42, Low = 0.18, Lean = 10, Writhe = 9, Whip = 55, Elbow = 1.5 } },
            // a soldier: crisp, short and economical, straight arms, a firm wide stance
            { "trooper", new MotionStyle { Amp = 0.92, Snap = 1.4, Over = 1.04, Wide = 16, Low = 0.06, Lean = 4, Elbow = 0.45, Arms = "tucked" } },
            // a dancer in a space suit: graceful arcs, the free arm up and out, a turn with every strike
            { "juniper", new MotionStyle { Amp = 1.12, Snap = 2.7, Over = 1.26, Arms = "dancer", Flourish = 22, Bounce = 0.06 } },
            // a roadrunner: lightning pecks with the whole head, leaning far in, feet never still
            { "dusty", new MotionStyle { Amp = 1.05, Snap = 1.1, Over = 1.12, Lean = 16, Low = 0.1, Peck = 28, Scurry = 26, Arms = "tucked" } },
            // a cactus: stiff arms bent up, slow to swing, and a springy wobble afterwards
            { "sam", new MotionStyle { Amp = 0.85, Snap = 3.2, Over = 1.1, Elbow = 0.3, Cactus = 70, Wobble = 9, Wide = 6 } },
            // a Gila monster: low and heavy, slow to wind up, then the head snaps forward in a bite and the body rocks
            { "gila", new MotionStyle { Amp = 0.95, Snap = 2.6, Over = 1.2, Low = 0.22, Lean = 14, Peck = 18, Bounce = 0.05, Wobble = 4, Arms = "guard", Elbow = 0.6 } },
            // a boxer: the other fist up by his chin, bobbing and weaving, short punchy snaps
            { "teddy", new MotionStyle { Amp = 1.0, Snap = 1.7, Over = 1.1, Arms = "guard", Weave = 8, Wide = 12, Low = 0.08, Lean = 8 } },
            // a gentleman fencer: the free arm raised behind, a lunge on every strike
            { "ben", new MotionStyle { Amp = 0.9, Snap = 2.0, Over = 1.1, Arms = "fencer", Lunge = 34, Lean = 6, Elbow = 0.8 } },
            // a knight: both hands on the sword, planted wide and low, big heavy arcs
            { "knight", new MotionStyle { Amp = 1.1, Snap = 2.7, Over = 1.32, Arms = "two", Wide = 18, Low = 0.1, Lean = 4 } },
            // a lumberjack: both hands on the axe, huge wind-ups and chops that bury themselves
            { "paul", new MotionStyle { Amp = 1.22, Snap = 3.1, Over = 1.38, Arms = "two", Wide = 20, Low = 0.14, Lean = 10 } },
        };

        public static MotionStyle MotionOf(string id) { MotionStyle s; return id != null && motions.TryGetValue(id, out s) ? s : plainMotion; }

        /// <summary>Pictures where the front hand leads the strike (the other arm is free for the fighter's style).</summary>
        static bool FrontArmAnim(string anim)
        {
            switch (anim)
            {
                case "jab": case "poke": case "punch": case "swing": case "upswing": case "bigswing": case "bigpunch":
                case "uppercut": case "airpunch": case "overhead": case "lash": case "aim": case "throw": case "drill":
                case "dash": case "peck": case "kick": case "lowkick": case "sweep": case "headbutt": case "stomp": case "backkick":
                    return true;
            }
            return false;
        }

        /// <summary>Pictures where a leg does the work (left alone by stance and lunge).</summary>
        static bool LegAnim(string anim)
        {
            switch (anim)
            {
                case "kick": case "lowkick": case "splits": case "stomp": case "flip": case "sweep": case "backkick": case "ride":
                    return true;
            }
            return false;
        }

        static double Toward(double stand, double v, double k)
        {
            double d = v - stand;
            return Math.Abs(d) > 175 ? v : stand + d * k;
        }

        static BPose Scale(BPose p, double k)
        {
            if (k == 1) return p;
            p.Lean *= k; p.Crouch = Math.Max(-0.05, Stand.Crouch + (p.Crouch - Stand.Crouch) * k);
            p.FA1 = Toward(Stand.FA1, p.FA1, k); p.BA1 = Toward(Stand.BA1, p.BA1, k);
            p.FA2 = Toward(Stand.FA2, p.FA2, k); p.BA2 = Toward(Stand.BA2, p.BA2, k);
            p.FL1 = Toward(Stand.FL1, p.FL1, k); p.BL1 = Toward(Stand.BL1, p.BL1, k);
            p.FL2 = Toward(Stand.FL2, p.FL2, k); p.BL2 = Toward(Stand.BL2, p.BL2, k);
            return p;
        }

        /// <summary>The fighter's own wind-up and strike for an attack picture.</summary>
        public static void StyledPoses(string fighterId, string anim, bool air, out BPose wind, out BPose strike)
        {
            AnimPoses(anim, out wind, out strike);
            MotionStyle s = MotionOf(fighterId);
            wind = Scale(wind, s.Amp); strike = Scale(strike, s.Amp);
            wind = Shape(s, anim, air, wind, false); strike = Shape(s, anim, air, strike, true);
        }

        static BPose Shape(MotionStyle s, string anim, bool air, BPose p, bool strike)
        {
            bool front = FrontArmAnim(anim), legs = LegAnim(anim);
            // elbows: straighter or bendier, and a cactus's bend upward
            p.FA2 *= s.Elbow; if (front) p.BA2 *= s.Elbow;
            if (s.Cactus != 0) { p.FA2 += s.Cactus * (strike ? 0.4 : 1); if (front) p.BA2 += s.Cactus; }
            if (front)
            {
                switch (s.Arms)
                {
                    case "guard": p.BA1 = strike ? -35 : -20; p.BA2 = 125; break;
                    case "two": p.BA1 = p.FA1 + 7; p.BA2 = p.FA2 - 12; break;
                    case "fencer": p.BA1 = strike ? 155 : 140; p.BA2 = -55; break;
                    case "dancer": p.BA1 = strike ? 150 : 125; p.BA2 = -15; break;
                    case "tucked": p.BA1 = -60; p.BA2 = 115; break;
                    case "flung": p.BA1 = strike ? -165 : 175; p.BA2 = strike ? -15 : 20; break;
                }
            }
            p.Lean += strike ? s.Lean : -s.Lean * 0.4;
            if (!air)
            {
                p.Crouch += s.Low;
                if (!legs)
                {
                    p.FL1 += s.Wide * 0.5; p.BL1 -= s.Wide * 0.5;
                    if (strike && s.Lunge != 0) { p.FL1 += s.Lunge; p.FL2 = Math.Min(p.FL2, -s.Lunge * 0.8); p.BL1 -= s.Lunge * 0.6; p.BL2 = 0; p.Crouch += 0.12; }
                }
            }
            if (s.Peck != 0) p.Head += strike ? -s.Peck : s.Peck * 0.6;
            return p;
        }

        /// <summary>What moves on its own during an attack: writhing, weaving, scurrying, bouncing, whips and wobbles.
        /// fr = the move's frame, a / b = the first and last frame of the strike, len = the move's length.</summary>
        static BPose Alive(MotionStyle s, string anim, bool air, BPose p, double fr, int a, int b, int len, double t)
        {
            if (s.Writhe != 0)
            {
                p.FA1 += Math.Sin(t * 9) * s.Writhe; p.BA1 += Math.Sin(t * 9 + 2) * s.Writhe;
                p.FA2 += Math.Sin(t * 11 + 1) * s.Writhe; p.BA2 += Math.Sin(t * 11 + 3) * s.Writhe;
                if (!LegAnim(anim)) { p.FL1 += Math.Sin(t * 7) * s.Writhe * 0.5; p.BL1 += Math.Sin(t * 7 + 2.5) * s.Writhe * 0.5; }
            }
            if (s.Whip != 0 && fr >= a && fr <= b + 4)
            {
                // the elbow trails behind the shoulder, then cracks straight
                double k = (fr - a) / Math.Max(1.0, b - a + 4);
                p.FA2 += Math.Sin(k * Math.PI) * s.Whip * (1 - k);
            }
            if (s.Weave != 0 && fr < a) p.Lean += Math.Sin(fr * 0.9) * s.Weave;
            if (s.Scurry != 0 && !air && !LegAnim(anim)) { double w = Math.Sin(fr * 1.6); p.FL1 += w * s.Scurry; p.BL1 -= w * s.Scurry; }
            if (s.Bounce != 0 && !air) p.Crouch += Math.Abs(Math.Sin(fr * 0.45)) * s.Bounce;
            if (s.Wobble != 0 && fr > b)
            {
                double k = (fr - b) / Math.Max(1.0, len - b);
                p.Lean += Math.Sin(k * Math.PI * 4) * s.Wobble * (1 - k);
            }
            if (s.Flourish != 0 && p.Spin == 0 && fr >= a)
            {
                double k = Math.Min(1, (fr - a) / Math.Max(1.0, len - a));
                p.Spin = -Math.Sin(k * Math.PI) * s.Flourish;
            }
            p.Crouch = Math.Min(0.95, p.Crouch);   // never folded past the knees
            return p;
        }
    }
}
