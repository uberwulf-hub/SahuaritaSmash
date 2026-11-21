// Classic Games - Sahuarita Smash!, the fighters' own body shapes (Andrew, 2026-10-31: they "all look too much alike",
// "the shapes that are too perfect"). Each fighter has a BodyStyle: proportions (head, legs, arms, body), limbs that
// taper and swell like arms and legs do (or armour plates, or a puffy suit), a body shaped like a barrel, a pear or a
// V, a head with a jaw, a chin, a nose and an ear instead of a perfect circle, and its own hands and feet (fists,
// gloves, gauntlets, boots, buckled shoes...). Pictures only: nothing here is read by the match (BrawlSim).
using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace ClassicGames
{
    /// <summary>How one fighter is built. Lengths are shares of the fighter's height, thicknesses of its width.</summary>
    public sealed class BodyStyle
    {
        public double Head = 0.165, Leg = 0.36, Arm = 0.31, Torso = 0.30;
        public double ArmW = 0.2, LegW = 0.24, BodyW = 0.62;
        /// <summary>Thickness at the wrist / ankle end compared with the shoulder / hip end.</summary>
        public double ArmTaper = 0.8, LegTaper = 0.8;
        /// <summary>How much the middle of a limb swells (muscle, padding).</summary>
        public double Bulge = 0.08;
        /// <summary>The body: width at the shoulders and at the hips (shares of BodyW), and a belly or chest bulge.</summary>
        public double Shoulders = 1, Hips = 1, Belly = 0;
        /// <summary>The head: width and height (shares of the radius), the jaw's width (1 = square) and how far the chin drops.</summary>
        public double HeadW = 1, HeadH = 1, Jaw = 0.82, Chin = 0;
        public bool Nose = true, Ear = true;
        /// <summary>Hands: ball, glove, fist, gauntlet, mitt. Feet: shoe, round, boot, tallboot, bigboot, sabaton, buckle.</summary>
        public string Hand = "ball", Foot = "shoe";
        /// <summary>Armour plates on the limbs (the Firewall Knight), ribs on a suit (Juniper).</summary>
        public bool Plates;
        public int Rings;
        /// <summary>Drawn more like a real person (life-like proportions, thinner outlines, eyes with whites and brows)
        /// rather than as a cartoon (Andrew, 2026-10-31: "some ... more cartoony while others look more realistic").</summary>
        public bool Real;
        /// <summary>The shared fighter style (Creative Collaboration Test 003, round 1: art direction ChatGPT, Andrew, 2026-11-20):
        /// one look for the whole cast instead of the two modes above. Fighters move onto it one at a time (round 1: Teddy);
        /// it takes over from Real for the outline, the shading and the face. Its rules are in FighterInk.</summary>
        public bool Toon;

        static readonly Dictionary<string, BodyStyle> all = new Dictionary<string, BodyStyle>();
        static readonly BodyStyle plain = new BodyStyle();
        public static BodyStyle For(string id) { BodyStyle s; return id != null && all.TryGetValue(id, out s) ? s : plain; }

        static BodyStyle()
        {
            // Bloom: small and round, a big head, short limbs, a little flared tunic
            all["bloom"] = new BodyStyle { Head = 0.23, Leg = 0.29, Arm = 0.26, Torso = 0.24, ArmW = 0.17, LegW = 0.2, BodyW = 0.56, ArmTaper = 0.86, LegTaper = 0.9, Bulge = 0.04,
                Shoulders = 0.82, Hips = 1.12, HeadW = 1.06, HeadH = 0.96, Jaw = 0.72, Chin = 0.04, Nose = false, Hand = "mitt", Foot = "round" };
            // the Trooper: square and solid, a vest, thick boots
            all["trooper"] = new BodyStyle { Real = true, Head = 0.145, Leg = 0.38, Arm = 0.32, Torso = 0.32, ArmW = 0.23, LegW = 0.27, BodyW = 0.7, ArmTaper = 0.76, LegTaper = 0.78, Bulge = 0.12,
                Shoulders = 1.16, Hips = 0.94, HeadW = 0.96, HeadH = 1.02, Jaw = 0.98, Chin = 0.02, Hand = "glove", Foot = "boot" };
            // Juniper: a puffy space suit with ribs, chunky gloves and moon boots
            all["juniper"] = new BodyStyle { Head = 0.19, Leg = 0.32, Arm = 0.28, Torso = 0.29, ArmW = 0.27, LegW = 0.31, BodyW = 0.76, ArmTaper = 0.9, LegTaper = 0.95, Bulge = 0.2,
                Shoulders = 1.04, Hips = 1.06, Belly = 0.06, HeadW = 0.92, HeadH = 1.05, Jaw = 0.7, Chin = 0.05, Rings = 2, Hand = "glove", Foot = "bigboot" };
            // Teddy Roosevelt: stocky, a barrel chest, a square jaw, riding boots
            all["teddy"] = new BodyStyle { Toon = true, Real = true, Head = 0.165, Leg = 0.355, Arm = 0.32, Torso = 0.315, ArmW = 0.25, LegW = 0.29, BodyW = 0.84, ArmTaper = 0.72, LegTaper = 0.8, Bulge = 0.22,
                Shoulders = 1.12, Hips = 0.92, Belly = 0.18, HeadW = 1.04, HeadH = 0.96, Jaw = 1.0, Chin = 0.03, Hand = "fist", Foot = "tallboot" };
            // Ben Franklin: older and round in the middle, narrow shoulders, a soft double chin, buckled shoes
            all["ben"] = new BodyStyle { Real = true, Head = 0.145, Leg = 0.35, Arm = 0.3, Torso = 0.35, ArmW = 0.16, LegW = 0.2, BodyW = 0.72, ArmTaper = 0.82, LegTaper = 0.72, Bulge = 0.05,
                Shoulders = 0.76, Hips = 1.12, Belly = 0.22, HeadW = 1.08, HeadH = 1.0, Jaw = 0.95, Chin = 0.12, Hand = "ball", Foot = "buckle" };
            // the Firewall Knight: armour plates, broad pauldrons, gauntlets and pointed sabatons
            all["knight"] = new BodyStyle { Real = true, Head = 0.15, Leg = 0.37, Arm = 0.32, Torso = 0.32, ArmW = 0.24, LegW = 0.28, BodyW = 0.74, ArmTaper = 0.92, LegTaper = 0.88, Bulge = 0,
                Shoulders = 1.3, Hips = 0.9, Plates = true, Hand = "gauntlet", Foot = "sabaton", Nose = false, Ear = false };
            // Paul Bunyan: a giant V, huge arms tapering to big fists, a beard, big boots
            all["paul"] = new BodyStyle { Real = true, Head = 0.125, Leg = 0.4, Arm = 0.37, Torso = 0.34, ArmW = 0.27, LegW = 0.29, BodyW = 0.8, ArmTaper = 0.66, LegTaper = 0.78, Bulge = 0.2,
                Shoulders = 1.36, Hips = 0.88, HeadW = 1.0, HeadH = 1.08, Jaw = 1.0, Chin = 0.06, Hand = "fist", Foot = "bigboot" };
            // Pecos Bill: tall and lanky, long legs a little bowed, narrow hips, a long face, cowboy boots
            all["pecos"] = new BodyStyle { Real = true, Head = 0.13, Leg = 0.43, Arm = 0.35, Torso = 0.3, ArmW = 0.17, LegW = 0.2, BodyW = 0.6, ArmTaper = 0.78, LegTaper = 0.8, Bulge = 0.06,
                Shoulders = 1.08, Hips = 0.82, HeadW = 0.92, HeadH = 1.08, Jaw = 0.9, Chin = 0.06, Hand = "glove", Foot = "cowboy" };
            // the animals and the specimen keep their own drawings; these only set their proportions
            all["specimen"] = new BodyStyle { Head = 0.14, Leg = 0.26, Arm = 0.36, Torso = 0.38, Bulge = 0.12, ArmTaper = 0.6, LegTaper = 0.6 };
            all["dusty"] = new BodyStyle { Head = 0.12, Leg = 0.44, Arm = 0.24, Torso = 0.22, ArmTaper = 0.55, LegTaper = 0.55, Bulge = 0.02 };
            all["gila"] = new BodyStyle { Head = 0.2, Leg = 0.24, Arm = 0.22, Torso = 0.36, ArmW = 0.2, LegW = 0.24, BodyW = 0.8, ArmTaper = 0.7, LegTaper = 0.7, Bulge = 0.16 };
            all["sam"] = new BodyStyle { Head = 0.16, Leg = 0.22, Arm = 0.3, Torso = 0.46, ArmTaper = 0.75, LegTaper = 0.85, Bulge = 0.16 };
        }
    }

    /// <summary>The shared fighter style's rules (Test 003, round 1). Sizes are in fighter units (a fighter is about 90 tall),
    /// face sizes are shares of the head's radius R, so everything scales with the fighter.
    /// - Ink: one near-black #161616 for every outline. Body and limbs 4.2, the head 2.8, hands / feet / hat / props 2.6,
    ///   details inside a shape (buttons, seams, glasses) 1.8. (Before: cartoon 5 / 2.5, "real" 3.4 / 1.7.)
    /// - Faces: eyes with whites, 0.17 R wide and 0.2 R tall (the far eye 80%), an iris 0.11 R, a pupil 0.06 R and a
    ///   catch-light; brows 0.09 R thick; a nose 0.2 R; mouths drawn with ink, at least 0.4 R wide. Big enough to read at
    ///   phone size (a head is about 25 phone pixels across in a match).
    /// - Volume: each limb part gets a shadow along its lower side (30% darker, half its width), a highlight along its
    ///   upper side (35% lighter) and a soft shine at the swell, so limbs read as rounded rather than as flat tubes.
    /// - Colour: one signature colour covers most of a fighter's area; trims and props are darker or contrasting.
    /// Pictures only: nothing here changes a fighter's size, moves or hit areas.</summary>
    public static class FighterInk
    {
        public const uint Ink = 0xFF161616;
        public const double Limb = 4.2, Head = 2.8, Part = 2.6, Detail = 1.8;

        public static Pen Pen(double w) { return Gfx.P(Ink, w); }

        /// <summary>Rounded shading for one limb part from a (width wa) to b (width wb).</summary>
        public static void Sculpt(DrawingContext dc, Point a, Point b, double wa, double wb, uint c)
        {
            double dx = b.X - a.X, dy = b.Y - a.Y, len = Math.Sqrt(dx * dx + dy * dy);
            if (len < 0.5 || Math.Max(wa, wb) < 3) return;
            double nx = -dy / len, ny = dx / len;
            if (ny < 0) { nx = -nx; ny = -ny; }   // n points up (local y is up)
            // the shadow side: a band along the lower half
            dc.DrawGeometry(Gfx.B(Gfx.Mix(c, 0xFF000000, 0.3)), null, SegOf(new Point(a.X - nx * wa * 0.22, a.Y - ny * wa * 0.22), new Point(b.X - nx * wb * 0.22, b.Y - ny * wb * 0.22), wa * 0.5, wb * 0.5));
            // the lit side and a soft shine where the limb swells
            dc.DrawLine(RoundPen(Gfx.Mix(c, 0xFFFFFFFF, 0.35), (wa + wb) * 0.11), new Point(a.X + nx * wa * 0.24, a.Y + ny * wa * 0.24), new Point(b.X + nx * wb * 0.24, b.Y + ny * wb * 0.24));
            var m = new Point((a.X + b.X) / 2 + nx * (wa + wb) * 0.14, (a.Y + b.Y) / 2 + ny * (wa + wb) * 0.14);
            dc.DrawEllipse(Gfx.B(Gfx.Alpha(Gfx.Mix(c, 0xFFFFFFFF, 0.6), 0.55)), null, m, (wa + wb) * 0.07, (wa + wb) * 0.07);
        }

        static Pen RoundPen(uint c, double w) { var p = new Pen(Gfx.B(c), w, null, PenLineCap.Round, PenLineJoin.Round); p.Freeze(); return p; }

        static StreamGeometry SegOf(Point a, Point b, double wa, double wb)
        {
            double dx = b.X - a.X, dy = b.Y - a.Y, len = Math.Sqrt(dx * dx + dy * dy);
            double ux = dx / len, uy = dy / len, nx = -uy, ny = ux, ha = wa / 2, hb = wb / 2;
            var g = new StreamGeometry();
            using (var s = g.Open())
            {
                s.BeginFigure(new Point(a.X + nx * ha, a.Y + ny * ha), true, true);
                s.LineTo(new Point(b.X + nx * hb, b.Y + ny * hb), true, true);
                s.QuadraticBezierTo(new Point(b.X + ux * hb, b.Y + uy * hb), new Point(b.X - nx * hb, b.Y - ny * hb), true, true);
                s.LineTo(new Point(a.X - nx * ha, a.Y - ny * ha), true, true);
                s.QuadraticBezierTo(new Point(a.X - ux * ha, a.Y - uy * ha), new Point(a.X + nx * ha, a.Y + ny * ha), true, true);
            }
            g.Freeze();
            return g;
        }

        /// <summary>Eyes looking forward (+x): whites, iris, pupil, a catch-light and a brow. Local y is up.</summary>
        public static void Eyes(DrawingContext dc, Point h, double R)
        {
            foreach (double ex in new[] { 0.16, 0.58 })
            {
                double k = ex < 0.5 ? 0.8 : 1;   // the far eye a little smaller: the head is turned
                var e = new Point(h.X + R * ex, h.Y + R * 0.14);
                double w = R * 0.17 * k, hh = R * 0.2 * k;
                dc.DrawEllipse(Gfx.B(0xFFFAFAF7), Pen(Detail), e, w, hh);
                var ir = new Point(e.X + w * 0.32, e.Y - hh * 0.08);
                dc.DrawEllipse(Gfx.B(0xFF4A3220), null, ir, R * 0.11 * k, R * 0.12 * k);
                dc.DrawEllipse(Gfx.B(0xFF0C0A09), null, ir, R * 0.06 * k, R * 0.065 * k);
                dc.DrawEllipse(Gfx.B(0xFFFFFFFF), null, new Point(ir.X + R * 0.04, ir.Y + R * 0.05), R * 0.028, R * 0.028);
            }
        }

        /// <summary>A rounded nose at the front of the face, with a little shine.</summary>
        public static void Nose(DrawingContext dc, Point h, double R, double W, double Hh, uint skin)
        {
            var c = new Point(h.X + W * 0.62, h.Y - Hh * 0.12);
            dc.DrawEllipse(Gfx.B(Gfx.Mix(skin, 0xFF9A3412, 0.2)), Pen(Detail), c, R * 0.2, R * 0.16);
            dc.DrawEllipse(Gfx.B(Gfx.Alpha(0xFFFFFFFF, 0.45)), null, new Point(c.X + R * 0.05, c.Y + R * 0.06), R * 0.06, R * 0.04);
        }
    }

    public sealed partial class BrawlView
    {
        // ---------------------------------------------------------------- limbs
        /// <summary>One limb segment from a (width wa) to b (width wb), its sides swelling by bulge, both ends rounded.</summary>
        static StreamGeometry Seg(Point a, Point b, double wa, double wb, double bulge)
        {
            double dx = b.X - a.X, dy = b.Y - a.Y, len = Math.Sqrt(dx * dx + dy * dy);
            if (len < 0.01) { dx = 0; dy = 1; len = 1; }
            double ux = dx / len, uy = dy / len, nx = -uy, ny = ux;
            double ha = wa / 2, hb = wb / 2, hm = (ha + hb) / 2 * (1 + 2 * bulge);
            var mid = new Point((a.X + b.X) / 2, (a.Y + b.Y) / 2);
            var g = new StreamGeometry();
            using (var s = g.Open())
            {
                s.BeginFigure(new Point(a.X + nx * ha, a.Y + ny * ha), true, true);
                s.QuadraticBezierTo(new Point(mid.X + nx * hm, mid.Y + ny * hm), new Point(b.X + nx * hb, b.Y + ny * hb), true, true);
                s.QuadraticBezierTo(new Point(b.X + ux * wb + nx * hb * 0.05, b.Y + uy * wb + ny * hb * 0.05), new Point(b.X - nx * hb, b.Y - ny * hb), true, true);
                s.QuadraticBezierTo(new Point(mid.X - nx * hm, mid.Y - ny * hm), new Point(a.X - nx * ha, a.Y - ny * ha), true, true);
                s.QuadraticBezierTo(new Point(a.X - ux * wa, a.Y - uy * wa), new Point(a.X + nx * ha, a.Y + ny * ha), true, true);
            }
            g.Freeze();
            return g;
        }

        /// <summary>A two-part limb (shoulder or hip, elbow or knee, hand or foot) shaped by the fighter's style: one outline
        /// around both parts, then the colour, a shadow along the lower side, a highlight, and plates or ribs.</summary>
        static void Limb(DrawingContext dc, Point a, Point b, Point c, uint upper, uint lower, double w, double taper, double bulge, BodyStyle st)
        {
            double wb = w * (1 + taper) / 2 * 0.98, wc = w * taper;
            StreamGeometry g1 = Seg(a, b, w, wb, bulge), g2 = Seg(b, c, wb, wc, bulge * 0.8);
            bool toon = st != null && st.Toon;
            Pen o = Outline(toon ? FighterInk.Limb : st != null && st.Real ? 3.4 : 5);
            dc.DrawGeometry(null, o, g1); dc.DrawGeometry(null, o, g2);
            dc.DrawGeometry(Gfx.B(upper), null, g1); dc.DrawGeometry(Gfx.B(lower), null, g2);
            if (toon) { FighterInk.Sculpt(dc, a, b, w, wb, upper); FighterInk.Sculpt(dc, b, c, wb, wc, lower); }
            else { Shade(dc, a, b, w, wb, upper); Shade(dc, b, c, wb, wc, lower); }
            if (st != null && st.Plates)
            {
                // armour: bands near each joint and a round plate over the elbow / knee
                Band(dc, a, b, 0.82, w, wb, 0xFF1E293B); Band(dc, b, c, 0.2, wb, wc, 0xFF1E293B); Band(dc, b, c, 0.86, wb, wc, 0xFF1E293B);
                dc.DrawEllipse(BrawlView.Sphere(Gfx.Mix(upper, 0xFFFFFFFF, 0.15)), Outline(2), b, wb * 0.62, wb * 0.62);
            }
            if (st != null && st.Rings > 0)
                for (int i = 1; i <= st.Rings; i++) { Band(dc, a, b, i / (st.Rings + 1.0), w, wb, Gfx.Mix(upper, 0xFF000000, 0.3)); Band(dc, b, c, i / (st.Rings + 1.0), wb, wc, Gfx.Mix(lower, 0xFF000000, 0.3)); }
        }

        /// <summary>The usual limb for the animals and the specimen: a gentle taper and swell.</summary>
        static void Limb(DrawingContext dc, Point a, Point b, Point c, uint upper, uint lower, double w)
        {
            Limb(dc, a, b, c, upper, lower, w, 0.8, 0.08, null);
        }

        static void Shade(DrawingContext dc, Point a, Point b, double wa, double wb, uint c)
        {
            double dx = b.X - a.X, dy = b.Y - a.Y, len = Math.Sqrt(dx * dx + dy * dy);
            if (len < 0.5 || Math.Max(wa, wb) < 3) return;
            double nx = -dy / len, ny = dx / len;
            if (ny < 0) { nx = -nx; ny = -ny; }   // n points up (local y is up)
            dc.DrawGeometry(Gfx.B(Gfx.Mix(c, 0xFF000000, 0.3)), null, Seg(new Point(a.X - nx * wa * 0.24, a.Y - ny * wa * 0.24), new Point(b.X - nx * wb * 0.24, b.Y - ny * wb * 0.24), wa * 0.42, wb * 0.42, 0));
            double h = 0.26;
            dc.DrawLine(Gfx.P(Gfx.Mix(c, 0xFFFFFFFF, 0.3), (wa + wb) * 0.08), new Point(a.X + nx * wa * h, a.Y + ny * wa * h), new Point(b.X + nx * wb * h, b.Y + ny * wb * h));
        }

        /// <summary>A line across a limb part at share k of its length (armour edges, suit ribs).</summary>
        static void Band(DrawingContext dc, Point a, Point b, double k, double wa, double wb, uint c)
        {
            double dx = b.X - a.X, dy = b.Y - a.Y, len = Math.Sqrt(dx * dx + dy * dy);
            if (len < 0.5) return;
            double nx = -dy / len, ny = dx / len, w = (wa + (wb - wa) * k) * 0.5;
            var p = new Point(a.X + dx * k, a.Y + dy * k);
            dc.DrawLine(Gfx.P(c, Math.Max(1.5, w * 0.16)), new Point(p.X + nx * w, p.Y + ny * w), new Point(p.X - nx * w, p.Y - ny * w));
        }

        // ---------------------------------------------------------------- hands and feet
        /// <summary>A hand at p at the end of a forearm pointing ang degrees, w = the arm's thickness.</summary>
        static void Hand(DrawingContext dc, Point p, double ang, double w, uint c, string kind)
        {
            switch (kind)
            {
                case "glove":
                case "mitt":
                    {
                        dc.PushTransform(new RotateTransform(ang, p.X, p.Y));
                        double r = w * (kind == "glove" ? 0.62 : 0.52);
                        dc.DrawRoundedRectangle(BrawlView.Sphere(c), Outline(2.2), new Rect(p.X - r * 0.6, p.Y - r * 0.85, r * 1.9, r * 1.7), r * 0.7, r * 0.7);
                        dc.DrawEllipse(Gfx.B(c), Outline(1.8), new Point(p.X + r * 0.2, p.Y + r * 0.85), r * 0.38, r * 0.3);   // the thumb
                        if (kind == "glove") dc.DrawRectangle(Gfx.B(Gfx.Mix(c, 0xFF000000, 0.25)), Outline(1.5), new Rect(p.X - r * 0.95, p.Y - r * 0.9, r * 0.45, r * 1.8));   // the cuff
                        dc.Pop();
                        return;
                    }
                case "fist":
                    {
                        dc.PushTransform(new RotateTransform(ang, p.X, p.Y));
                        double r = w * 0.66;
                        dc.DrawRoundedRectangle(BrawlView.Sphere(c), Outline(2.4), new Rect(p.X - r * 0.7, p.Y - r * 0.8, r * 1.75, r * 1.6), r * 0.55, r * 0.55);
                        for (int i = 0; i < 3; i++) dc.DrawLine(Gfx.P(Gfx.Mix(c, 0xFF000000, 0.35), 1.4), new Point(p.X + r * 0.9, p.Y - r * 0.5 + i * r * 0.5), new Point(p.X + r * 0.6, p.Y - r * 0.5 + i * r * 0.5));
                        dc.Pop();
                        return;
                    }
                case "gauntlet":
                    {
                        dc.PushTransform(new RotateTransform(ang, p.X, p.Y));
                        double r = w * 0.62;
                        Poly(dc, BrawlView.Sphere(c), Outline(2.2), new Point(p.X - r * 1.0, p.Y - r * 0.95), new Point(p.X + r * 0.7, p.Y - r * 0.8), new Point(p.X + r * 1.15, p.Y - r * 0.2),
                            new Point(p.X + r * 1.0, p.Y + r * 0.7), new Point(p.X - r * 1.0, p.Y + r * 0.95));
                        dc.DrawLine(Gfx.P(0xFF1E293B, 1.6), new Point(p.X - r * 0.35, p.Y - r * 0.85), new Point(p.X - r * 0.35, p.Y + r * 0.85));
                        dc.Pop();
                        return;
                    }
                default:
                    Ball(dc, p, w * 0.62, c);
                    return;
            }
        }

        /// <summary>A foot at p (the ankle), toes forward (local +x), w = the leg's thickness.</summary>
        static void FootOf(DrawingContext dc, Point p, double w, uint c, string kind)
        {
            Pen o = Outline(2.4);
            switch (kind)
            {
                case "round":
                    dc.DrawEllipse(BrawlView.Sphere(c), o, new Point(p.X + w * 0.3, p.Y + w * 0.12), w * 0.72, w * 0.58);
                    return;
                case "boot":
                case "tallboot":
                    {
                        double shaft = kind == "tallboot" ? w * 1.5 : w * 0.8;
                        dc.DrawRoundedRectangle(Gfx.B(c), o, new Rect(p.X - w * 0.55, p.Y - w * 0.1, w * 1.05, shaft), w * 0.2, w * 0.2);
                        Poly(dc, Gfx.B(c), o, new Point(p.X - w * 0.6, p.Y - w * 0.35), new Point(p.X + w * 0.75, p.Y - w * 0.35), new Point(p.X + w * 1.15, p.Y - w * 0.05),
                            new Point(p.X + w * 0.95, p.Y + w * 0.35), new Point(p.X - w * 0.55, p.Y + w * 0.3));
                        dc.DrawLine(Gfx.P(0xFF0C0A09, w * 0.16), new Point(p.X - w * 0.6, p.Y - w * 0.33), new Point(p.X + w * 0.85, p.Y - w * 0.33));   // the sole
                        if (kind == "tallboot") dc.DrawLine(Gfx.P(Gfx.Mix(c, 0xFFFFFFFF, 0.25), w * 0.12), new Point(p.X - w * 0.5, p.Y + shaft - w * 0.25), new Point(p.X + w * 0.45, p.Y + shaft - w * 0.25));
                        return;
                    }
                case "cowboy":
                    {
                        // a cowboy boot: a tall stitched shaft, a pointed toe, a block heel and a little spur behind
                        double shaft = w * 1.6;
                        dc.DrawRoundedRectangle(Gfx.B(c), o, new Rect(p.X - w * 0.5, p.Y - w * 0.1, w * 0.95, shaft), w * 0.15, w * 0.15);
                        dc.DrawLine(Gfx.P(Gfx.Mix(c, 0xFFFDE68A, 0.45), w * 0.08), new Point(p.X - w * 0.2, p.Y + shaft * 0.25), new Point(p.X + w * 0.05, p.Y + shaft * 0.8));
                        Poly(dc, Gfx.B(c), o, new Point(p.X - w * 0.55, p.Y - w * 0.25), new Point(p.X + w * 0.6, p.Y - w * 0.3), new Point(p.X + w * 1.45, p.Y - w * 0.22),
                            new Point(p.X + w * 0.75, p.Y + w * 0.3), new Point(p.X - w * 0.5, p.Y + w * 0.32));
                        dc.DrawRectangle(Gfx.B(0xFF1C1209), null, new Rect(p.X - w * 0.55, p.Y - w * 0.42, w * 0.38, w * 0.22));   // the heel
                        dc.DrawLine(Gfx.P(0xFF9CA3AF, w * 0.1), new Point(p.X - w * 0.5, p.Y - w * 0.05), new Point(p.X - w * 0.85, p.Y - w * 0.05));
                        dc.DrawEllipse(Gfx.B(0xFFD1D5DB), Gfx.P(0xFF4B5563, 1), new Point(p.X - w * 0.92, p.Y - w * 0.05), w * 0.13, w * 0.13);   // the spur's wheel
                        return;
                    }
                case "bigboot":
                    {
                        Poly(dc, BrawlView.Sphere(c), o, new Point(p.X - w * 0.75, p.Y - w * 0.42), new Point(p.X + w * 1.0, p.Y - w * 0.42), new Point(p.X + w * 1.45, p.Y - w * 0.05),
                            new Point(p.X + w * 1.2, p.Y + w * 0.45), new Point(p.X + w * 0.2, p.Y + w * 0.6), new Point(p.X - w * 0.7, p.Y + w * 0.55));
                        dc.DrawLine(Gfx.P(0xFF0C0A09, w * 0.18), new Point(p.X - w * 0.75, p.Y - w * 0.4), new Point(p.X + w * 1.1, p.Y - w * 0.4));
                        return;
                    }
                case "sabaton":
                    {
                        Poly(dc, BrawlView.Sphere(c), o, new Point(p.X - w * 0.6, p.Y - w * 0.32), new Point(p.X + w * 0.6, p.Y - w * 0.32), new Point(p.X + w * 1.4, p.Y - w * 0.2),
                            new Point(p.X + w * 0.55, p.Y + w * 0.3), new Point(p.X - w * 0.55, p.Y + w * 0.45));
                        for (int i = 0; i < 2; i++) dc.DrawLine(Gfx.P(0xFF1E293B, 1.4), new Point(p.X + w * (0.1 + i * 0.35), p.Y - w * 0.3), new Point(p.X + w * (0.0 + i * 0.35), p.Y + w * 0.35));
                        return;
                    }
                case "buckle":
                    {
                        dc.DrawEllipse(Gfx.B(c), o, new Point(p.X + w * 0.4, p.Y + w * 0.05), w * 0.85, w * 0.42);
                        dc.DrawRectangle(Gfx.B(0xFFFACC15), Gfx.P(0xFF854D0E, 1.2), new Rect(p.X + w * 0.3, p.Y - w * 0.05, w * 0.4, w * 0.32));
                        return;
                    }
                default:
                    dc.DrawEllipse(Gfx.B(c), o, new Point(p.X + w * 0.35, p.Y + w * 0.1), w * 0.85, w * 0.5);
                    return;
            }
        }

        // ---------------------------------------------------------------- bodies and heads
        /// <summary>The body in its own upright frame (bottom at y0, top at y1, centre x cx, width w): wider at the
        /// shoulders or the hips, a belly or chest swelling at the front, rounded shoulders and a softer bottom.</summary>
        static Geometry BodyShape(double cx, double y0, double y1, double w, BodyStyle st)
        {
            double wt = w * st.Shoulders / 2, wb = w * st.Hips / 2, belly = w * st.Belly, midY = y0 + (y1 - y0) * 0.45;
            var g = new StreamGeometry();
            using (var s = g.Open())
            {
                s.BeginFigure(new Point(cx - wb, y0), true, true);
                // the back: nearly straight
                s.QuadraticBezierTo(new Point(cx - Math.Max(wb, wt) * 1.02, midY), new Point(cx - wt, y1 - w * 0.12), true, true);
                // rounded shoulders
                s.QuadraticBezierTo(new Point(cx - wt * 0.9, y1 + w * 0.06), new Point(cx, y1 + w * 0.06), true, true);
                s.QuadraticBezierTo(new Point(cx + wt * 0.9, y1 + w * 0.06), new Point(cx + wt, y1 - w * 0.12), true, true);
                // the front: chest or belly
                s.QuadraticBezierTo(new Point(cx + Math.Max(wb, wt) * 1.02 + belly * 2, midY), new Point(cx + wb, y0), true, true);
                // a soft bottom
                s.QuadraticBezierTo(new Point(cx, y0 - w * 0.16), new Point(cx - wb, y0), true, true);
            }
            g.Freeze();
            return g;
        }

        /// <summary>A person's head: a skull wider or taller than round, a jaw (square or narrow) with a chin, an ear at
        /// the back and a nose at the front (facing +x), shaded like a ball with the stage's light on its edge.</summary>
        static void Skull(DrawingContext dc, Point h, double R, uint skin, BodyStyle st)
        {
            double W = R * st.HeadW, Hh = R * st.HeadH, jaw = st.Jaw, chin = st.Chin;
            if (st.Ear) { dc.DrawEllipse(Gfx.B(Gfx.Mix(skin, 0xFF000000, 0.12)), Outline(2), new Point(h.X - W * 0.22, h.Y - Hh * 0.08), W * 0.2, Hh * 0.26); }
            var g = new StreamGeometry();
            using (var s = g.Open())
            {
                Point l = new Point(h.X - W, h.Y), r = new Point(h.X + W, h.Y);
                s.BeginFigure(l, true, true);
                s.BezierTo(new Point(h.X - W, h.Y + Hh * 1.33), new Point(h.X + W, h.Y + Hh * 1.33), r, true, true);
                Point j1 = new Point(h.X + W * jaw, h.Y - Hh * 0.72), c = new Point(h.X + W * 0.2, h.Y - Hh * (1 + chin)), j2 = new Point(h.X - W * jaw * 0.92, h.Y - Hh * 0.7);
                s.QuadraticBezierTo(new Point(h.X + W * 1.03, h.Y - Hh * 0.38), j1, true, true);
                s.QuadraticBezierTo(new Point(h.X + W * jaw * 0.9, h.Y - Hh * (1 + chin)), c, true, true);
                s.QuadraticBezierTo(new Point(h.X - W * jaw * 0.55, h.Y - Hh * (1 + chin) * 0.98), j2, true, true);
                s.QuadraticBezierTo(new Point(h.X - W * 1.03, h.Y - Hh * 0.36), l, true, true);
            }
            g.Freeze();
            dc.DrawGeometry(BrawlView.Sphere(skin), Outline(st.Toon ? FighterInk.Head : st.Real ? 1.7 : 2.5), g);
            if (R > 6) dc.DrawGeometry(null, Gfx.P(Gfx.Alpha(Rim, 0.55), Math.Max(1.2, R * 0.12)), Cap(h, R * 0.86, 20, 80));
            if (st.Nose && st.Toon) FighterInk.Nose(dc, h, R, W, Hh, skin);
            else if (st.Nose) dc.DrawEllipse(Gfx.B(Gfx.Mix(skin, 0xFF7C2D12, 0.22)), Gfx.P(Gfx.Mix(skin, 0xFF000000, 0.45), 1.2), new Point(h.X + W * 0.55, h.Y - Hh * 0.2), R * 0.15, R * 0.12);
            if (chin > 0.08)
            {
                // a soft double chin
                var dbl = new StreamGeometry();
                using (var s = dbl.Open()) { s.BeginFigure(new Point(h.X - W * 0.05, h.Y - Hh * 0.82), false, false); s.QuadraticBezierTo(new Point(h.X + W * 0.3, h.Y - Hh * 1.02), new Point(h.X + W * 0.62, h.Y - Hh * 0.78), true, true); }
                dbl.Freeze();
                dc.DrawGeometry(null, Gfx.P(Gfx.Mix(skin, 0xFF000000, 0.3), 1.4), dbl);
            }
        }

        /// <summary>Eyes for the fighter's style: cartoon dots, or (Real) small almond eyes with whites, an iris and a
        /// brow. Hurt is the same X for everyone.</summary>
        static void FaceEyes(DrawingContext dc, Point h, double R, bool hurt, BodyStyle st)
        {
            if (st.Toon && !hurt) { FighterInk.Eyes(dc, h, R); return; }
            if (!st.Real || hurt) { Eyes(dc, h, R, hurt, 0xFF1F2937); return; }
            foreach (double ex in new[] { 0.3, 0.72 })
            {
                var e = new Point(h.X + R * ex, h.Y + R * 0.1);
                double w = R * (ex < 0.5 ? 0.13 : 0.16);   // the far eye a little narrower: the head is turned
                dc.DrawEllipse(Gfx.B(0xFFF8FAFC), Gfx.P(0xFF3F3F46, 1), e, w, R * 0.085);
                dc.DrawEllipse(Gfx.B(0xFF3B2A1A), null, new Point(e.X + w * 0.3, e.Y), R * 0.065, R * 0.07);
                dc.DrawEllipse(Gfx.B(0xFF0C0A09), null, new Point(e.X + w * 0.3, e.Y), R * 0.03, R * 0.035);
                dc.DrawLine(Gfx.P(0xFF3F2A1A, Math.Max(1.4, R * 0.07)), new Point(e.X - w, e.Y + R * 0.16), new Point(e.X + w * 1.05, e.Y + R * 0.2));
            }
        }
    }
}
