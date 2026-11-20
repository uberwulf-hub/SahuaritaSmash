// Sahuarita Smash! (Sauce Edition) - Monsoon Wash's pictures: a dry sandy wash at the edge of town in monsoon season
// (mesquite and palo verde on the banks, the Santa Rita Mountains under a storm, lightning far off), its platforms (a
// fallen cottonwood log on rocks, the old wooden footbridge, the concrete culvert) and its surprise, the flash flood (a
// sign and a trickle while it is warned, then a wave of muddy water running along the ground). Pictures only: the
// match never reads any of this (the flood itself is BrawlSim.StepFlood).
using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;

namespace ClassicGames
{
    public sealed partial class BrawlView
    {
        /// <summary>The sky behind the wash: a storm building over the mountains, its dark clouds heaped up along the top
        /// with grey curtains of rain under some, and the late sun breaking through low down.</summary>
        void WashSky(DrawingContext dc, double ox, double span)
        {
            var sun = new Point(ox + W * 0.18, H * 0.5);
            dc.DrawEllipse(Glow(0x90FFD9A0), null, sun, 320, 240);
            dc.DrawEllipse(Sphere(0xFFFFE8C2), null, sun, 34, 34);
            var rng = new Random(77);
            for (double x = -100; x < span + 200; x += 150 + rng.Next(90))
            {
                bool heavy = rng.Next(3) > 0;
                double y = 40 + rng.Next(70), k = 0.8 + rng.NextDouble() * 0.6;
                uint dark = heavy ? 0xFF2F3A4A : 0xFF4B5567, lit = heavy ? 0xFF4B5567 : 0xFF6B7487;
                // rain falling from the heavy ones, slanting a little
                if (heavy && rng.Next(2) == 0)
                    Poly(dc, Linear(0x50778395, 0x00778395, 90), null, new Point(x - 50 * k, y + 40), new Point(x + 70 * k, y + 40), new Point(x + 40 * k + 60, y + 330), new Point(x - 80 * k + 60, y + 330));
                for (int i = 0; i < 6; i++) dc.DrawEllipse(Gfx.B(dark), null, new Point(x + (i - 2.5) * 34 * k, y + (i % 2) * 12 - (i == 2 || i == 3 ? 22 : 0) * k), 48 * k, 34 * k);
                for (int i = 1; i < 5; i++) dc.DrawEllipse(Gfx.B(lit), null, new Point(x + (i - 2.5) * 34 * k - 6, y - 14 * k - (i == 2 || i == 3 ? 18 : 0) * k), 30 * k, 18 * k);
            }
            dc.DrawRectangle(Linear(0xC0232B38, 0x00232B38, 90), null, new Rect(0, -200, span, 260));
        }

        /// <summary>The wash's three background layers: the mountains under the storm, the far bank with mesquite and palo
        /// verde, and the near bank with big trees, rocks and the flood sign.</summary>
        void WashLayer(DrawingContext dc, int layer, double w, double h, double horizon, Random rng)
        {
            if (layer == 0)
            {
                // the Santa Rita Mountains: jagged blue-grey peaks with the storm's shadow on them
                var g = new StreamGeometry();
                using (var c = g.Open())
                {
                    c.BeginFigure(new Point(0, horizon + 10), true, true);
                    for (double x = 0; x <= w; x += 26) c.LineTo(new Point(x, horizon - 50 - 90 * Math.Abs(Math.Sin(x * 0.0017 + 0.4)) - 26 * Math.Abs(Math.Sin(x * 0.013)) - rng.Next(10)), true, true);
                    c.LineTo(new Point(w, horizon + 10), true, true);
                }
                g.Freeze();
                dc.DrawGeometry(Linear(0xFF55607A, 0xFF7B8197, 90), null, g);
                dc.DrawRectangle(Linear(0xFFB49A78, 0xFF9C8462, 90), null, new Rect(0, horizon + 8, w, h));
                // far: the desert floor below the mountains, a few saguaros and small trees, hazy and blue-green
                double fy = horizon + 12;
                for (double x = 40; x < w; x += 300 + rng.Next(160))
                {
                    if (rng.Next(3) == 0) SaguaroShaded(dc, x, fy, (90 + rng.Next(55)) / 110.0 * 0.8, Haze(0xFF4F7A45, FarHaze));
                    else if (rng.Next(2) == 0) PaloVerde(dc, x, fy, 80 + rng.Next(35), FarHaze, 0, rng);
                    else Mesquite(dc, x, fy, 80 + rng.Next(35), FarHaze, 0, rng);
                    Shrub(dc, x + 70 + rng.Next(60), fy, 12 + rng.Next(8), FarHaze, 0, rng);
                }
            }
            else if (layer == 1)
            {
                // the far bank of the wash: a low sandy cut with scrub and small trees along its top
                var g = new StreamGeometry();
                using (var c = g.Open())
                {
                    c.BeginFigure(new Point(0, h), true, true);
                    c.LineTo(new Point(0, horizon + 24), true, true);
                    for (double x = 0; x <= w; x += 60) c.LineTo(new Point(x, horizon + 20 + 8 * Math.Sin(x * 0.01) + rng.Next(6)), true, true);
                    c.LineTo(new Point(w, h), true, true);
                }
                g.Freeze();
                dc.DrawGeometry(Linear(0xFFA88A62, 0xFF7E6446, 90), null, g);
                for (int i = 0; i < 10; i++) { double y = horizon + 40 + i * 6; dc.DrawLine(Gfx.P(0x18000000, 2), new Point(0, y), new Point(w, y + 4)); }
                // middle: the plants that say "Sonoran desert": palo verde and mesquite along the bank, shrubs between
                double my = horizon + 28;
                for (double x = 60; x < w; x += 230 + rng.Next(140))
                {
                    if (rng.Next(2) == 0) PaloVerde(dc, x, my, 120 + rng.Next(45), MidHaze, 1.4, rng);
                    else Mesquite(dc, x, my, 120 + rng.Next(45), MidHaze, 1.4, rng);
                    if (rng.Next(3) > 0) Shrub(dc, x + 95 + rng.Next(50), my + 2, 25 + rng.Next(20), MidHaze, 1.4, rng);
                }
            }
            else
            {
                // foreground: the near bank: big mesquite and palo verde with stronger colour, shrubs and rocks (the flood
                // sign now stands in the stage itself, by the log, where the footbridge can't cover it: RoadSign)
                for (double x = -40; x < w + 100; x += 430 + rng.Next(140))
                {
                    if (rng.Next(2) == 0) Mesquite(dc, x, h - 30, 155 + rng.Next(50), 0, 2.2, rng);
                    else PaloVerde(dc, x, h - 30, 155 + rng.Next(50), 0, 2.2, rng);
                    Shrub(dc, x + 150 + rng.Next(80), h - 26, 38 + rng.Next(17), 0, 2.2, rng);
                }
                for (int i = 0; i < 26; i++) { double x = rng.NextDouble() * w, y = h - 10 - rng.Next(70); dc.DrawEllipse(Sphere(i % 3 == 0 ? 0xFF8A7F74 : 0xFF6E5E4E), null, new Point(x, y), 10 + rng.Next(14), 7 + rng.Next(8)); }
            }
        }

        // ---------------------------------------------------------------- Sonoran plants (Creative Collaboration Test 001)
        // Art direction: ChatGPT (creative director), approved by Andrew, 2026-11-20. Colours are the spec's; the far
        // layer is hazed toward a cool blue-grey, the middle layer a little, the foreground not at all. Heights are in
        // layer pixels (the layers are drawn at screen scale): far 80-115, middle 120-165, foreground 155-205.
        const double FarHaze = 0.45, MidHaze = 0.14;
        const uint HazeTone = 0xFF8C98A8, Ink = 0xFF161616;

        /// <summary>A colour pushed back into the distance (0 = as specified, 1 = the haze itself).</summary>
        static uint Haze(uint c, double k) { return k <= 0 ? c : Gfx.Mix(c, HazeTone, k); }

        /// <summary>The soft dark edge every plant gets (the game's #161616 ink), lighter and thinner further back.</summary>
        static uint InkFor(double haze) { return Gfx.Alpha(Ink, 0.55 * (1 - haze)); }

        /// <summary>A branch drawn as a tapering line: an ink edge (ow wide) under the wood, and a thin lit stroke on its
        /// upper-left side.</summary>
        static void Branch(DrawingContext dc, Point a, Point b, double wa, double wb, uint wood, uint lit, double haze, double ow)
        {
            int n = 3;
            for (int i = 0; i < n; i++)
            {
                double k0 = i / (double)n, k1 = (i + 1) / (double)n, wd = wa + (wb - wa) * (k0 + k1) / 2;
                Point p0 = new Point(a.X + (b.X - a.X) * k0, a.Y + (b.Y - a.Y) * k0), p1 = new Point(a.X + (b.X - a.X) * k1, a.Y + (b.Y - a.Y) * k1);
                if (ow > 0) dc.DrawLine(RoundPen(InkFor(haze), wd + ow * 2), p0, p1);
            }
            for (int i = 0; i < n; i++)
            {
                double k0 = i / (double)n, k1 = (i + 1) / (double)n, wd = wa + (wb - wa) * (k0 + k1) / 2;
                Point p0 = new Point(a.X + (b.X - a.X) * k0, a.Y + (b.Y - a.Y) * k0), p1 = new Point(a.X + (b.X - a.X) * k1, a.Y + (b.Y - a.Y) * k1);
                dc.DrawLine(RoundPen(Haze(wood, haze), wd), p0, p1);
            }
            double wm = (wa + wb) / 2;
            if (wm > 3)
            {
                // one continuous lit stroke along the upper-left side, stopping short of both ends
                Point s0 = new Point(a.X + (b.X - a.X) * 0.08 - wm * 0.22, a.Y + (b.Y - a.Y) * 0.08 - wm * 0.1), s1 = new Point(a.X + (b.X - a.X) * 0.9 - wm * 0.2, a.Y + (b.Y - a.Y) * 0.9 - wm * 0.1);
                dc.DrawLine(RoundPen(Haze(lit, haze), Math.Max(1, wm * 0.26)), s0, s1);
            }
        }

        /// <summary>A crown of leaf clumps: an ink outline under all of them (so the crown reads as one shape), the
        /// darker clumps behind, the leaf colour over them and a highlight on the upper left of the front ones.</summary>
        static void Crown(DrawingContext dc, List<Rect> blobs, uint leaf, uint lit, double haze, double ow, bool backOnly, bool frontOnly)
        {
            if (!frontOnly)
            {
                if (ow > 0) foreach (Rect b in blobs) dc.DrawEllipse(Gfx.B(InkFor(haze)), null, b.Center, b.Width / 2 + ow, b.Height / 2 + ow);
                foreach (Rect b in blobs) dc.DrawEllipse(Gfx.B(Haze(Gfx.Mix(leaf, 0xFF000000, 0.22), haze)), null, b.Center, b.Width / 2, b.Height / 2);
            }
            if (backOnly) return;
            for (int i = 0; i < blobs.Count; i++)
            {
                Rect b = blobs[i];
                if (i % 3 == 2) continue;   // gaps: the airy crown lets the dark back clumps (and branches) show through
                dc.DrawEllipse(Gfx.B(Haze(leaf, haze)), null, new Point(b.Center.X - b.Width * 0.06, b.Center.Y - b.Height * 0.1), b.Width * 0.42, b.Height * 0.4);
                if (i % 2 == 0) dc.DrawEllipse(Gfx.B(Haze(lit, haze)), null, new Point(b.Center.X - b.Width * 0.18, b.Center.Y - b.Height * 0.24), b.Width * 0.2, b.Height * 0.17);
            }
        }

        static Rect Blob(Point c, double rx, double ry) { return new Rect(c.X - rx, c.Y - ry, rx * 2, ry * 2); }

        /// <summary>A blue palo verde, about h tall standing at (x, y): a short green trunk splitting low into three or
        /// four green limbs that fork again, and an airy, irregular rounded crown of small clumps with the branches
        /// showing through. ow = the ink edge's width (0 for the far layer).</summary>
        static void PaloVerde(DrawingContext dc, double x, double y, double h, double haze, double ow, Random r)
        {
            const uint trunk = 0xFF657F43, trunkLit = 0xFF9DAF62, leaf = 0xFF789C45, leafLit = 0xFFB6C86A;
            double tw = h * 0.075, split = y - h * 0.24;
            Point top = new Point(x + (r.NextDouble() - 0.5) * h * 0.05, split);
            var limbs = new List<Point[]>();
            int n = 3 + r.Next(2);
            for (int i = 0; i < n; i++)
            {
                double ang = -132 + 84 * (i + 0.5) / n + (r.NextDouble() - 0.5) * 12;   // an upright vase, not a flat fan
                double len = h * (0.5 + r.NextDouble() * 0.14);
                Point mid = Off(top, ang + (r.NextDouble() - 0.5) * 20, len * 0.55), tip = Off(mid, ang + (r.NextDouble() - 0.5) * 30, len * 0.5);
                limbs.Add(new[] { top, mid, tip, Off(mid, ang - 28, len * 0.38), Off(mid, ang + 26, len * 0.34) });
            }
            // the crown: clumps round each limb's outer half and tips, more on top, never a perfect dome
            var blobs = new List<Rect>();
            foreach (Point[] l in limbs)
                for (int k = 1; k < 5; k++)
                    for (int j = 0; j < (k == 1 ? 1 : 3); j++)
                    {
                        double rr = h * (0.045 + r.NextDouble() * 0.035);
                        blobs.Add(Blob(new Point(l[k].X + (r.NextDouble() - 0.5) * h * 0.14, l[k].Y + (r.NextDouble() - 0.5) * h * 0.12), rr, rr * 0.8));
                    }
            // fill the top of the crown so its outline is rounded (but lumpy), never flat
            for (int j = 0; j < 5; j++)
            {
                double rr = h * (0.05 + r.NextDouble() * 0.03);
                blobs.Add(Blob(new Point(x + (r.NextDouble() - 0.5) * h * 0.4, y - h * (0.8 + r.NextDouble() * 0.12)), rr, rr * 0.8));
            }
            Crown(dc, blobs, leaf, leafLit, haze, ow, true, false);
            Branch(dc, new Point(x, y), top, tw, tw * 0.8, trunk, trunkLit, haze, ow);
            foreach (Point[] l in limbs)
            {
                Branch(dc, l[0], l[1], tw * 0.7, tw * 0.45, trunk, trunkLit, haze, ow);
                Branch(dc, l[1], l[2], tw * 0.45, tw * 0.22, trunk, trunkLit, haze, ow * 0.7);
                Branch(dc, l[1], l[3], tw * 0.32, tw * 0.15, trunk, trunkLit, haze, ow * 0.6);
                Branch(dc, l[1], l[4], tw * 0.3, tw * 0.14, trunk, trunkLit, haze, ow * 0.6);
            }
            Crown(dc, blobs, leaf, leafLit, haze, ow, false, true);
        }

        /// <summary>A velvet mesquite, about h tall standing at (x, y): two or three dark, twisting stems leaning apart,
        /// branches spreading wide and lopsided, and a fine, feathery crown in uneven tiers with a ragged lower edge.</summary>
        static void Mesquite(DrawingContext dc, double x, double y, double h, double haze, double ow, Random r)
        {
            const uint trunk = 0xFF59402D, trunkLit = 0xFF896344, leaf = 0xFF657D3D, leafLit = 0xFF9BAE5C;
            double tw = h * 0.07;
            int lean = r.Next(2) == 0 ? -1 : 1;   // which way the whole tree leans (never symmetrical)
            int stems = 2 + r.Next(2);
            var tips = new List<Point>();
            var wood = new List<Point[]>();
            for (int sI = 0; sI < stems; sI++)
            {
                double side = (sI - (stems - 1) / 2.0) * 0.7 + lean * 0.35;
                Point p = new Point(x + side * tw * 0.6, y);
                double ang = -90 + side * 18;
                double segLen = h * 0.19;
                // a twisting stem: a few segments, each bending a little the other way
                for (int k = 0; k < 3; k++)
                {
                    ang += (k % 2 == 0 ? 1 : -1) * (10 + r.NextDouble() * 12) + side * 4;
                    Point q = Off(p, ang, segLen * (1 - k * 0.12));
                    wood.Add(new[] { p, q, new Point(tw * (1 - k * 0.25), tw * (1 - (k + 1) * 0.25)) });
                    p = q;
                }
                // spreading branches from the stem's top, wide and drooping at the ends
                for (int bI = 0; bI < 2; bI++)
                {
                    double ba = ang + (bI == 0 ? -1 : 1) * (30 + r.NextDouble() * 18) + lean * 8;
                    Point m = Off(p, ba, h * (0.2 + r.NextDouble() * 0.1));
                    Point e = Off(m, ba + (ba < -90 ? -1 : 1) * 30, h * (0.16 + r.NextDouble() * 0.08));
                    e = new Point(e.X, e.Y + h * 0.04);   // the ends droop
                    wood.Add(new[] { p, m, new Point(tw * 0.38, tw * 0.22) });
                    wood.Add(new[] { m, e, new Point(tw * 0.22, tw * 0.1) });
                    tips.Add(m); tips.Add(e);
                }
                tips.Add(p);
            }
            // the crown: wide, flattened clumps in tiers over the tips, a fringe of small ones under each tier
            var blobs = new List<Rect>();
            // an irregular dome, not an umbrella: clumps over every tip, a second tier stacked above the inner ones, and a
            // fringe of small drooping clumps under each (the "feathery" lower edge)
            foreach (Point t in tips)
            {
                double rx = h * (0.075 + r.NextDouble() * 0.045);
                blobs.Add(Blob(new Point(t.X + lean * h * 0.03, t.Y - h * 0.05), rx, rx * 0.6));
                blobs.Add(Blob(new Point(t.X - lean * rx * 0.8, t.Y - h * 0.02), rx * 0.7, rx * 0.42));
                if (Math.Abs(t.X - x) < h * 0.3) blobs.Add(Blob(new Point(t.X + (r.NextDouble() - 0.5) * h * 0.08, t.Y - h * 0.15 - r.NextDouble() * h * 0.06), rx * 0.85, rx * 0.55));
                for (int f = 0; f < 3; f++) blobs.Add(Blob(new Point(t.X + (f - 1) * rx * 0.75, t.Y + rx * 0.3 + r.NextDouble() * h * 0.03), rx * 0.3, rx * 0.22));
            }
            Crown(dc, blobs, leaf, leafLit, haze, ow, true, false);
            foreach (Point[] wd in wood) Branch(dc, wd[0], wd[1], wd[2].X, wd[2].Y, trunk, trunkLit, haze, ow);
            Crown(dc, blobs, leaf, leafLit, haze, ow, false, true);
        }

        /// <summary>A desert shrub (creosote, desert broom), about h tall at (x, y): a low, lumpy mound of a few uneven
        /// clumps, darker underneath, lit on top. Never a circle.</summary>
        static void Shrub(DrawingContext dc, double x, double y, double h, double haze, double ow, Random r)
        {
            const uint baseC = 0xFF89934C, lit = 0xFFB5A65D;
            int n = 4 + r.Next(3);
            var blobs = new List<Rect>();
            for (int i = 0; i < n; i++)
            {
                double fx = (i - (n - 1) / 2.0) / n;
                double rx = h * (0.45 + r.NextDouble() * 0.35), ry = h * (0.35 + r.NextDouble() * 0.3) * (1 - Math.Abs(fx) * 0.6);
                blobs.Add(Blob(new Point(x + fx * h * 1.9, y - ry * 0.9), rx, ry));
            }
            if (ow > 0) foreach (Rect b in blobs) dc.DrawEllipse(Gfx.B(InkFor(haze)), null, b.Center, b.Width / 2 + ow, b.Height / 2 + ow);
            foreach (Rect b in blobs) dc.DrawEllipse(Gfx.B(Haze(Gfx.Mix(baseC, 0xFF000000, 0.25), haze)), null, b.Center, b.Width / 2, b.Height / 2);
            foreach (Rect b in blobs) dc.DrawEllipse(Gfx.B(Haze(baseC, haze)), null, new Point(b.Center.X - b.Width * 0.05, b.Center.Y - b.Height * 0.12), b.Width * 0.42, b.Height * 0.36);
            for (int i = 0; i < blobs.Count; i += 2) { Rect b = blobs[i]; dc.DrawEllipse(Gfx.B(Haze(lit, haze)), null, new Point(b.Center.X - b.Width * 0.14, b.Center.Y - b.Height * 0.26), b.Width * 0.18, b.Height * 0.12); }
        }

        /// <summary>The flood warning sign in the stage itself, by the wash's entrance (left of the log), in front of the
        /// background and clear of the footbridge at every camera: a yellow diamond with a dark border on a sturdy
        /// post, "DO NOT ENTER WHEN FLOODED", and a depth gauge (1-4 ft) on the post. World units x z (the camera);
        /// g = the ground's top on screen.</summary>
        void RoadSign(DrawingContext dc, double g, double z)
        {
            const uint yellow = 0xFFF4C542, border = 0xFF29251E, post = 0xFF514438;
            double x = SX(SignX), half = 60 * z, cy = g - 246 * z;
            // the post, planted in the sand, with the depth gauge on its lower part
            double pw = 9 * z;
            dc.DrawRectangle(Gfx.B(Ink), null, new Rect(x - pw / 2 - 2 * z, cy, pw + 4 * z, g - cy + 2 * z));
            dc.DrawRectangle(Linear(Gfx.Mix(post, 0xFFFFFFFF, 0.18), post, 0), null, new Rect(x - pw / 2, cy, pw, g - cy));
            double gw = 16 * z, gTop = g - 128 * z;
            dc.DrawRectangle(Gfx.B(0xFFF5F5F4), Gfx.P(Ink, 1.5 * z), new Rect(x - gw / 2, gTop, gw, g - gTop - 4 * z));
            for (int ft = 1; ft <= 4; ft++)
            {
                double ty = g - 4 * z - ft * 30 * z;
                dc.DrawLine(Gfx.P(border, 2 * z), new Point(x - gw / 2, ty), new Point(x + gw / 2 - 5 * z, ty));
                Gfx.Text(dc, ft.ToString(), x + gw / 2 - 4 * z, ty - 1 * z, 9 * z, border, 1, Gfx.UiBold);
            }
            // the diamond: a dark border, the yellow face, a thin dark inner line, the words in three lines
            var outer = new StreamGeometry();
            using (var c = outer.Open())
            {
                c.BeginFigure(new Point(x, cy - half), true, true);
                c.LineTo(new Point(x + half, cy), true, true); c.LineTo(new Point(x, cy + half), true, true); c.LineTo(new Point(x - half, cy), true, true);
            }
            outer.Freeze();
            dc.DrawGeometry(Gfx.B(border), Gfx.P(Ink, 2.5 * z), outer);
            double inner = half - 7 * z;
            var face = new StreamGeometry();
            using (var c = face.Open())
            {
                c.BeginFigure(new Point(x, cy - inner), true, true);
                c.LineTo(new Point(x + inner, cy), true, true); c.LineTo(new Point(x, cy + inner), true, true); c.LineTo(new Point(x - inner, cy), true, true);
            }
            face.Freeze();
            dc.DrawGeometry(Linear(Gfx.Mix(yellow, 0xFFFFFFFF, 0.2), yellow, 90), Gfx.P(border, 1.5 * z), face);
            double fs = 14 * z;
            Gfx.Text(dc, "DO NOT", x, cy - fs * 1.62, fs, border, 0.5, Gfx.UiBold);
            Gfx.Text(dc, "ENTER WHEN", x, cy - fs * 0.56, fs * 0.94, border, 0.5, Gfx.UiBold);
            Gfx.Text(dc, "FLOODED", x, cy + fs * 0.5, fs, border, 0.5, Gfx.UiBold);
        }
        /// <summary>Where the sign stands (world units): just left of the log, at the wash's entrance.</summary>
        const double SignX = -392;

        /// <summary>What moves behind the wash: lightning flashing far off now and then, and the odd big raindrop.</summary>
        void WashLive(DrawingContext dc, double t)
        {
            // a bolt every few seconds somewhere over the mountains, with a faint flash of the whole sky
            double cyc = t % 5.3;
            if (cyc < 0.22)
            {
                int n = (int)(t / 5.3);
                double bx = ((n * 397.7) % (W * 0.8)) + W * 0.1, by = 70;
                dc.DrawRectangle(Gfx.B(Gfx.Alpha(0xFFE0E7FF, 0.12 * (1 - cyc / 0.22))), null, new Rect(0, 0, W, H));
                var pts = new Point[7];
                pts[0] = new Point(bx, by);
                for (int i = 1; i < pts.Length; i++) pts[i] = new Point(pts[i - 1].X + ((n * 13 + i * 7) % 5 - 2) * 9, pts[i - 1].Y + 30);
                for (int i = 0; i < pts.Length - 1; i++)
                {
                    dc.DrawLine(Gfx.P(Gfx.Alpha(0xFFC7D2FE, 0.45), 6), pts[i], pts[i + 1]);
                    dc.DrawLine(Gfx.P(0xF0FFFFFF, 2), pts[i], pts[i + 1]);
                }
            }
            for (int i = 0; i < 18; i++)
            {
                double x = ((i * 211.3 + t * 40) % (W + 60)) - 30, y = (i * 97.1 + t * (420 + i % 4 * 60)) % (H - 120);
                dc.DrawLine(Gfx.P(0x50CBD5E1, 1.5), new Point(x, y), new Point(x - 4, y + 14));
            }
        }

        /// <summary>What holds the wash's platforms up, drawn behind them (screen coordinates; g = the ground's top).</summary>
        void WashUnder(DrawingContext dc, BPlat p, double x1, double x2, double y, double yb, double g, double z, Pen edge, double t)
        {
            double w = x2 - x1;
            switch (p.Look)
            {
                case "log":
                    RoadSign(dc, g, z);   // the flood sign by the entrance, before the log so its post sits behind it
                    // the fallen cottonwood rests on two piles of rocks
                    foreach (double fx in new[] { 0.2, 0.8 })
                    {
                        double rx = x1 + w * fx;
                        double hgt = g - yb;
                        for (int i = 0; i < 3; i++)
                        {
                            double ry = g - hgt * (i + 0.5) / 3, rr = (26 - i * 4) * z;
                            dc.DrawEllipse(Sphere(i % 2 == 0 ? 0xFF8A7F74 : 0xFF746A60), edge, new Point(rx + ((i % 2) * 2 - 1) * 6 * z, ry), rr, hgt / 6 + 4 * z);
                        }
                    }
                    break;
                case "bridge":
                    {
                        // the old footbridge: heavy wooden posts down to the sand, braced with crossed boards
                        double pw = 14 * z, a = x1 + 10 * z, b = x2 - 10 * z - pw;
                        Brush post = Linear(0xFF7A5230, 0xFF4E3420, 0);
                        dc.DrawRectangle(post, edge, new Rect(a, yb, pw, g - yb));
                        dc.DrawRectangle(post, edge, new Rect(b, yb, pw, g - yb));
                        dc.DrawRectangle(post, edge, new Rect(x1 + w / 2 - pw / 2, yb, pw, g - yb));
                        Pen brace = Gfx.P(0xFF6B4628, 6 * z);
                        double mid = x1 + w / 2;
                        dc.DrawLine(brace, new Point(a + pw, yb + 8 * z), new Point(mid - pw / 2, g - 30 * z));
                        dc.DrawLine(brace, new Point(a + pw, g - 30 * z), new Point(mid - pw / 2, yb + 8 * z));
                        dc.DrawLine(brace, new Point(mid + pw / 2, yb + 8 * z), new Point(b, g - 30 * z));
                        dc.DrawLine(brace, new Point(mid + pw / 2, g - 30 * z), new Point(b, yb + 8 * z));
                        break;
                    }
                case "culvert":
                    {
                        // the concrete box culvert: a headwall down to the sand with two round pipes through it
                        dc.DrawRectangle(Linear(0xFFB1AAA0, 0xFF8A847B, 90), edge, new Rect(x1 + 6 * z, yb, w - 12 * z, g - yb));
                        double pr = Math.Min((g - yb) * 0.36, w * 0.19);
                        foreach (double fx in new[] { 0.3, 0.7 })
                        {
                            Point c = new Point(x1 + w * fx, g - pr - 4 * z);
                            dc.DrawEllipse(Gfx.B(0xFF57534E), edge, c, pr + 5 * z, pr + 5 * z);
                            dc.DrawEllipse(Linear(0xFF0C0A09, 0xFF292524, 90), null, c, pr, pr);
                            dc.DrawEllipse(Gfx.B(0x405B8BA0), null, new Point(c.X, c.Y + pr * 0.75), pr * 0.7, pr * 0.15);
                        }
                        dc.DrawLine(Gfx.P(0x30000000, 3 * z), new Point(x1 + 8 * z, g - (g - yb) * 0.25), new Point(x2 - 8 * z, g - (g - yb) * 0.25));
                        break;
                    }
            }
        }

        /// <summary>Over the wash's platforms: the log's cut end with its rings and a broken branch, the footbridge's
        /// railings, a marker post on the culvert.</summary>
        void WashOver(DrawingContext dc, BPlat p, Point fl, Point fr, Point bl, Point br, double yb, double z, double t)
        {
            switch (p.Look)
            {
                case "log":
                    {
                        double ry = (yb - fl.Y) / 2 + 2 * z;
                        Point end = new Point(fl.X, fl.Y + ry - 2 * z);
                        dc.DrawEllipse(Gfx.B(0xFFD6C3A0), Gfx.P(0xFF4A4038, 2 * z), end, ry * 0.55, ry);
                        for (int i = 1; i < 4; i++) dc.DrawEllipse(null, Gfx.P(0x605B4632, 1.2 * z), end, ry * 0.55 * i / 4, ry * i / 4);
                        // a broken branch sticking up at the far end
                        Point bs = new Point(fr.X - 24 * z, (fr.Y + br.Y) / 2);
                        dc.DrawLine(RoundPen(0xFF4A4038, 10 * z), bs, new Point(bs.X + 18 * z, bs.Y - 34 * z));
                        dc.DrawLine(RoundPen(0xFFA39A8E, 6 * z), bs, new Point(bs.X + 18 * z, bs.Y - 34 * z));
                        break;
                    }
                case "bridge":
                    {
                        Pen rail = RoundPen(0xFF8B5E34, 4 * z), post = RoundPen(0xFF5C3D22, 4 * z);
                        double h = 30 * z;
                        foreach (Point[] e in new[] { new[] { bl, br }, new[] { fl, fr } })
                        {
                            for (int i = 0; i <= 4; i++) { double k = i / 4.0; double x = e[0].X + (e[1].X - e[0].X) * k, yy = e[0].Y + (e[1].Y - e[0].Y) * k; dc.DrawLine(post, new Point(x, yy), new Point(x, yy - h)); }
                            dc.DrawLine(rail, new Point(e[0].X, e[0].Y - h), new Point(e[1].X, e[1].Y - h));
                            dc.DrawLine(RoundPen(0xFF7A5230, 2.5 * z), new Point(e[0].X, e[0].Y - h * 0.5), new Point(e[1].X, e[1].Y - h * 0.5));
                        }
                        // the planks' gaps across the deck
                        for (int i = 1; i < 10; i++) { double k = i / 10.0; dc.DrawLine(Gfx.P(0x50000000, 1.5 * z), new Point(fl.X + (fr.X - fl.X) * k, fl.Y), new Point(bl.X + (br.X - bl.X) * k, bl.Y)); }
                        break;
                    }
                case "culvert":
                    {
                        // a striped marker post at the back corner, so drivers see the crossing
                        Point b = new Point(br.X - 10 * z, br.Y);
                        dc.DrawRectangle(Gfx.B(0xFFF5F5F4), Gfx.P(0x80000000, 1), new Rect(b.X - 4 * z, b.Y - 46 * z, 8 * z, 46 * z));
                        for (int i = 0; i < 3; i++) dc.DrawRectangle(Gfx.B(0xFFDC2626), null, new Rect(b.X - 4 * z, b.Y - 44 * z + i * 14 * z, 8 * z, 6 * z));
                        break;
                    }
            }
        }

        // ---------------------------------------------------------------- the flash flood
        /// <summary>The flash flood: while it is warned, a blinking sign and a thin trickle of brown water feeling its
        /// way along the ground from the side it will come from; then the wave itself, a foamy muddy front with the
        /// water behind it, sticks and a tumbleweed riding along, thinning out at its tail (BrawlSim.StepFlood).</summary>
        void DrawFlood(DrawingContext dc, BrawlSim sim, double t, bool behind)
        {
            double gy = SY(sim.Main.Top), lx = SX(sim.Main.L), rx = SX(sim.Main.R);
            int dir = sim.HazDir;
            if (sim.HazPhase == 1 && !behind)
            {
                double k = 1 - sim.HazT / (double)BrawlSim.HazWarn;
                double edge = dir > 0 ? lx : rx, len = (rx - lx) * 0.55 * k;
                for (int i = 0; i < 3; i++)
                {
                    double a = edge, b = edge + dir * len * (1 - i * 0.2);
                    double yy = gy - 1 - i * 2;
                    dc.DrawLine(RoundPen(Gfx.Alpha(i == 0 ? WaterFace : WaterTop, 0.95), (6 + 8 * k - i * 2) * Zoom), new Point(a, yy), new Point(b, yy + Math.Sin(t * 8 + i) * Zoom));
                }
                for (int i = 0; i < 4; i++) { double gx = edge + dir * len * ((t * 0.8 + i * 0.25) % 1.0); dc.DrawEllipse(Gfx.B(0xB0FFFFFF), null, new Point(gx, gy - 3), 2.5, 1.2); }
                double blink = Math.Sin(t * 18) > 0 ? 1 : 0.4;
                double x = W / 2, y = 70;
                dc.DrawRoundedRectangle(Gfx.B(Gfx.Alpha(0xFFFACC15, blink)), Gfx.P(0xFF000000, 2), new Rect(x - 170, y - 18, 340, 36), 10, 10);
                Gfx.Text(dc, "Flash flood! Get up high", x, y - 12, 18, 0xFF000000, 0.5, Gfx.UiBold);
                double sx = dir > 0 ? 40 : W - 40;
                dc.DrawEllipse(Gfx.B(Gfx.Alpha(0xFFFACC15, blink)), Gfx.P(0xFF000000, 2), new Point(sx, gy - 50), 22, 22);
                Gfx.Text(dc, "!", sx, gy - 66, 26, 0xFF000000, 0.5, Gfx.UiBold);
                return;
            }
            if (sim.HazPhase != 2) return;
            double front = SX(sim.HazX), tail = SX(sim.HazX - dir * BrawlSim.FloodLen);
            double a0 = Math.Max(lx, Math.Min(front, tail)), a1 = Math.Min(rx, Math.Max(front, tail));
            if (a1 <= a0) return;
            Point vp = VP;
            Func<double, double> depth = sx =>
            {
                double behind = (front - sx) * dir / Zoom;   // world px behind the front
                // as deep as it reaches, the front standing up higher
                double hgt = BrawlSim.FloodDepth + 4 * Math.Sin(sx * 0.03 + t * 7);
                if (behind < 70) hgt = BrawlSim.FloodDepth + 26 * Math.Sin(Math.Max(0, behind) / 70.0 * Math.PI * 0.5 + Math.PI * 0.5) + 4;   // the wave's front stands up
                double fromTail = BrawlSim.FloodLen - behind;
                if (fromTail < 160) hgt *= Math.Max(0.1, fromTail / 160.0);
                return Math.Max(2, hgt) * Zoom;
            };
            const int steps = 40;
            var top = new Point[steps + 1];
            for (int i = 0; i <= steps; i++) { double sx = a0 + (a1 - a0) * i / steps; top[i] = new Point(sx, gy - depth(sx)); }
            // the surface seen from above (it runs back into the picture), then the front face of the water
            var surf = new StreamGeometry();
            using (var c = surf.Open())
            {
                c.BeginFigure(top[0], true, true);
                for (int i = 1; i <= steps; i++) c.LineTo(top[i], true, true);
                for (int i = steps; i >= 0; i--) { Point q = top[i]; c.LineTo(new Point(q.X + (vp.X - q.X) * 0.1, q.Y + (vp.Y - gy) * 0.1 + depth(q.X) * 0.5), true, true); }
            }
            surf.Freeze();
            // the surface goes behind the fighters (they stand in it), the front of the water in front of their feet
            if (behind)
            {
                dc.DrawGeometry(Linear(WaterTop, WaterTopShade, 90), null, surf);
                FloodSurfaceDetail(dc, front, dir, a0, a1, gy, vp, depth, t);
                return;
            }
            var face = new StreamGeometry();
            using (var c = face.Open())
            {
                c.BeginFigure(new Point(a0, gy + 4 * Zoom), true, true);
                for (int i = 0; i <= steps; i++) c.LineTo(top[i], true, true);
                c.LineTo(new Point(a1, gy + 4 * Zoom), true, true);
            }
            face.Freeze();
            dc.DrawGeometry(Linear(WaterFace, WaterDeep, 90), Gfx.P(Gfx.Alpha(Ink, 0.7), 2 * Zoom), face);
            FloodFaceDetail(dc, front, dir, a0, a1, gy, depth, t);
            FloodCrest(dc, front, dir, a0, a1, lx, rx, gy, depth, t);
            // sticks and a tumbleweed riding along
            for (int i = 0; i < 4; i++)
            {
                double sx = front - dir * (90 + i * 120) * Zoom;
                if (sx < a0 || sx > a1) continue;
                double sy = gy - depth(sx) + 2 * Zoom + Math.Sin(t * 4 + i) * 2 * Zoom;
                if (i == 1)
                {
                    double rr = 14 * Zoom;
                    dc.DrawEllipse(Gfx.B(0xC0A3824E), Gfx.P(0xFF6B4F2A, 1.2), new Point(sx, sy - rr * 0.6), rr, rr);
                    for (int k = 0; k < 6; k++) dc.DrawGeometry(null, Gfx.P(0xFF8A6A3A, 1.4), Cap(new Point(sx, sy - rr * 0.6), rr * (0.5 + 0.1 * k), t * 90 * dir + k * 55, t * 90 * dir + k * 55 + 140));
                }
                else
                {
                    double ang = Math.Sin(t * 3 + i) * 12 + i * 30;
                    dc.PushTransform(new RotateTransform(ang, sx, sy));
                    dc.DrawLine(RoundPen(0xFF4A3524, 4 * Zoom), new Point(sx - 16 * Zoom, sy), new Point(sx + 16 * Zoom, sy));
                    dc.Pop();
                }
            }
            // water pouring off the end of the ground once it gets there
            double lip = dir > 0 ? rx : lx;
            if (dir > 0 ? front > rx && tail < rx : front < lx && tail > lx)
                dc.DrawRectangle(Linear(Gfx.Alpha(WaterFace, 0.82), Gfx.Alpha(WaterFace, 0), 90), null, new Rect(lip - (dir > 0 ? 0 : 18 * Zoom), gy - 4 * Zoom, 18 * Zoom, 140 * Zoom));
        }

        // ---------------------------------------------------------------- the water's look (Creative Collaboration Test 002)
        // Art direction: ChatGPT (creative director), approved by Andrew, 2026-11-20: a muted desert teal that reads as fast,
        // muddy water at phone size (0.57x), a rolling foamy crest, and motion in the body. Pictures only: the flood's
        // timing, speed, depth, length and what it does to fighters are BrawlSim's and unchanged. Every movement here
        // comes from the animation clock t (no randomness), and flows the way the flood runs (dir).
        const uint WaterTop = 0xFF638F89, WaterTopShade = 0xFF466E69, WaterFace = 0xFF365C59, WaterDeep = 0xFF294744;
        const uint Foam = 0xFFF5EBDD, FoamLit = 0xFFD5E9D9;
        /// <summary>How fast the streaks and ripples run inside the water, ahead of the water itself (world units a second).</summary>
        const double FlowAhead = 110;

        /// <summary>A repeating spot along the water: u = world units behind the front, moving toward the front over time.</summary>
        static double FlowU(int k, double spacing, double t, double len)
        {
            double u = (k * spacing - t * FlowAhead) % len;
            return u < 0 ? u + len : u;
        }

        /// <summary>On the surface strip (behind the fighters): long light flow streaks, short ripples and foam patches.</summary>
        void FloodSurfaceDetail(DrawingContext dc, double front, int dir, double a0, double a1, double gy, Point vp, Func<double, double> depth, double t)
        {
            double z = Zoom, len = BrawlSim.FloodLen;
            // long flow streaks: 25-70 units, the main motion cue, big enough to read on a phone
            for (int k = 0; k < 9; k++)
            {
                double u = FlowU(k, 61, t, len), L = 25 + (k * 37) % 46;
                double xa = front - dir * u * z, xb = xa - dir * L * z;
                if (Math.Max(xa, xb) < a0 || Math.Min(xa, xb) > a1) continue;
                xa = Math.Max(a0, Math.Min(a1, xa)); xb = Math.Max(a0, Math.Min(a1, xb));
                double f = 0.25 + (k % 3) * 0.22;   // how far back across the strip
                double d = depth((xa + xb) / 2);
                double yy = gy - d + ((vp.Y - gy) * 0.1 + d * 0.5) * f;
                dc.DrawLine(RoundPen(Gfx.Alpha(FoamLit, 0.5 - (k % 3) * 0.08), (4.5 - (k % 3)) * z), new Point(xa, yy), new Point(xb, yy));
            }
            // short ripples: little arcs 10-25 units, slipping along a little slower
            for (int k = 0; k < 7; k++)
            {
                double u = FlowU(k, 83, t * 0.7, len), L = 10 + (k * 29) % 16;
                double xc = front - dir * u * z;
                if (xc < a0 + 6 || xc > a1 - 6) continue;
                double d = depth(xc), yy = gy - d + ((vp.Y - gy) * 0.1 + d * 0.5) * (0.35 + (k % 2) * 0.3);
                dc.DrawGeometry(null, RoundPen(Gfx.Alpha(Ink, 0.35), 2.5 * z), Cap(new Point(xc, yy + 3 * z), L * 0.5 * z, 200, 340));
                dc.DrawGeometry(null, RoundPen(Gfx.Alpha(FoamLit, 0.6), 2 * z), Cap(new Point(xc, yy + 2 * z), L * 0.5 * z, 205, 335));
            }
            // foam patches riding on top: 8-20 units, a few clumped circles each
            for (int k = 0; k < 5; k++)
            {
                double u = FlowU(k, 113, t, len), r = (8 + (k * 7) % 13) * 0.5 * z;
                double xc = front - dir * u * z;
                if (xc < a0 + r || xc > a1 - r) continue;
                double d = depth(xc), yy = gy - d + ((vp.Y - gy) * 0.1 + d * 0.5) * 0.5;
                dc.DrawEllipse(Gfx.B(Gfx.Alpha(Foam, 0.85)), null, new Point(xc, yy), r, r * 0.45);
                dc.DrawEllipse(Gfx.B(Gfx.Alpha(Foam, 0.75)), null, new Point(xc + r * 0.7, yy + r * 0.12), r * 0.55, r * 0.3);
                dc.DrawEllipse(Gfx.B(Gfx.Alpha(FoamLit, 0.9)), null, new Point(xc - r * 0.25, yy - r * 0.1), r * 0.4, r * 0.18);
            }
        }

        /// <summary>On the front face (in front of the fighters' feet): flow streaks and rolling eddies, translucent light
        /// on the dark teal, all moving with the flood.</summary>
        void FloodFaceDetail(DrawingContext dc, double front, int dir, double a0, double a1, double gy, Func<double, double> depth, double t)
        {
            double z = Zoom, len = BrawlSim.FloodLen;
            for (int k = 0; k < 8; k++)
            {
                double u = FlowU(k, 71, t * 1.2, len), L = 30 + (k * 41) % 41;
                double xa = front - dir * (u + 20) * z, xb = xa - dir * L * z;
                if (Math.Max(xa, xb) < a0 || Math.Min(xa, xb) > a1) continue;
                xa = Math.Max(a0, Math.Min(a1, xa)); xb = Math.Max(a0, Math.Min(a1, xb));
                double d = depth((xa + xb) / 2);
                if (d < 9 * z) continue;
                double yy = gy - d * (0.3 + (k % 3) * 0.2);
                dc.DrawLine(RoundPen(Gfx.Alpha(FoamLit, 0.32), 3.5 * z), new Point(xa, yy), new Point(xb, yy));
            }
            // eddies: 12-24 units, a turning curl of light water
            for (int k = 0; k < 4; k++)
            {
                double u = FlowU(k, 149, t, len) + 40;
                double xc = front - dir * u * z, r = (12 + (k * 5) % 13) * 0.5 * z;
                if (xc < a0 + r || xc > a1 - r) continue;
                double d = depth(xc);
                if (d < 2.4 * r) continue;
                Point c = new Point(xc, gy - d * 0.5);
                double spin = -dir * t * 220 + k * 70;   // rolls the way the water runs
                dc.DrawGeometry(null, RoundPen(Gfx.Alpha(FoamLit, 0.3), 3 * z), Cap(c, r, spin, spin + 150));
            }
        }

        /// <summary>The rolling crest: an irregular band of foam 5-8 units thick along the top of the water near the front,
        /// rounded lobes 12-22 units wide with the odd gap, a big rounded curl at the leading edge, splash bumps (10-14) that
        /// rise and settle, and a few droplets flying ahead. Soft ink edges, no spikes.</summary>
        void FloodCrest(DrawingContext dc, double front, int dir, double a0, double a1, double lx, double rx, double gy, Func<double, double> depth, double t)
        {
            double z = Zoom;
            if (front < a0 - 30 * z || front > a1 + 30 * z) return;
            Func<double, double> topAt = x => gy - depth(Math.Max(a0, Math.Min(a1, x)));
            var blobs = new List<Point>(); var radii = new List<double>();
            // the foam band riding the top: rounded lobes 12-22 units wide, overlapping, 5-8 thick, with the odd gap,
            // thinning out about 220 units behind the front
            for (int k = 0; k < 24; k++)
            {
                if (k % 7 == 5 || k % 11 == 9) continue;   // gaps
                double back = 6 + k * 9 + Math.Sin(t * 4 + k * 1.7) * 2;
                double xc = front - dir * back * z;
                if (xc < a0 - 4 * z || xc > a1 + 4 * z) continue;
                double r = (6 + (k * 5) % 6) * z * (1 - Math.Min(0.4, k * 0.02));   // radius 6-11: lobes 12-22 wide
                double bob = Math.Sin(t * 6 - k * 0.9) * 1.2 * z;
                blobs.Add(new Point(xc, topAt(xc) + r * 0.25 + bob)); radii.Add(r);
            }
            // splash bumps that pop up out of the crest and settle (10-14 units), each on its own beat
            for (int k = 0; k < 3; k++)
            {
                double ph = (t * 1.3 + k * 0.37) % 1.0, rise = Math.Sin(ph * Math.PI);
                double xc = front - dir * (16 + k * 38) * z;
                if (xc < a0 || xc > a1 || rise < 0.2) continue;
                double r = (5 + k) * z;   // 10-14 across
                blobs.Add(new Point(xc, topAt(xc) - (10 + 2 * k) * z * rise + r)); radii.Add(r);
            }
            // the rolling leading edge: rounded foam tumbling down the front of the water, biggest at the top
            double fx = Math.Max(a0, Math.Min(a1, front)), fd = depth(fx - dir * 2 * z);
            int tumble = 4;
            for (int i = 0; i < tumble; i++)
            {
                double f = i / (double)(tumble - 1);
                double r = (11 - 4 * f) * z, roll = Math.Sin(t * 8 + i * 1.9) * 1.5 * z;
                blobs.Add(new Point(fx - dir * (r * 0.3 - 3 * z * f) + roll, gy - fd + r * 0.6 + f * (fd - r * 0.6 - 4 * z))); radii.Add(r);
            }
            // ink first (one soft outline round the whole crest), then foam, then the lit tops
            for (int i = 0; i < blobs.Count; i++) dc.DrawEllipse(Gfx.B(Gfx.Alpha(Ink, 0.6)), null, blobs[i], radii[i] + 1.8 * z, radii[i] * 0.82 + 1.8 * z);
            for (int i = 0; i < blobs.Count; i++) dc.DrawEllipse(Gfx.B(Foam), null, blobs[i], radii[i], radii[i] * 0.82);
            for (int i = 0; i < blobs.Count; i++) dc.DrawEllipse(Gfx.B(FoamLit), null, new Point(blobs[i].X - radii[i] * 0.2, blobs[i].Y - radii[i] * 0.3), radii[i] * 0.45, radii[i] * 0.25);
            // droplets flung ahead of the crest, now and then
            for (int i = 0; i < 5; i++)
            {
                double ph = (t * 2.2 + i * 0.21) % 1.0;
                if (i % 2 == 1 && ph > 0.7) continue;
                double dx = dir * (10 + i * 9) * z * ph, dy = (14 + i * 5) * z * Math.Sin(ph * Math.PI);
                double rr = (3.5 - i * 0.3) * z;
                Point p = new Point(fx + dx, gy - fd - dy);
                dc.DrawEllipse(Gfx.B(Gfx.Alpha(Ink, 0.45)), null, p, rr + 1.2 * z, rr + 1.2 * z);
                dc.DrawEllipse(Gfx.B(Foam), null, p, rr, rr);
            }
        }

        static Geometry StripLine(Point[] pts)
        {
            var g = new StreamGeometry();
            using (var c = g.Open()) { c.BeginFigure(pts[0], false, false); for (int i = 1; i < pts.Length; i++) c.LineTo(pts[i], true, true); }
            g.Freeze();
            return g;
        }
    }
}
