// Sahuarita Smash! (Sauce Edition) - Monsoon Wash's pictures: a dry sandy wash at the edge of town in monsoon season
// (mesquite and palo verde on the banks, the Santa Rita Mountains under a storm, lightning far off), its platforms (a
// fallen cottonwood log on rocks, the old wooden footbridge, the concrete culvert) and its surprise, the flash flood (a
// sign and a trickle while it is warned, then a wave of muddy water running along the ground). Pictures only: the
// match never reads any of this (the flood itself is BrawlSim.StepFlood).
using System;
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
                for (double x = 30; x < w; x += 120 + rng.Next(80))
                {
                    if (rng.Next(3) == 0) PaloVerde(dc, x, horizon + 26, 0.5 + rng.NextDouble() * 0.1);
                    else Mesquite(dc, x, horizon + 26, 0.5 + rng.NextDouble() * 0.12);
                }
                for (int i = 0; i < 30; i++) dc.DrawEllipse(Gfx.B(0xFF6B7A45), null, new Point(rng.NextDouble() * w, horizon + 24 + rng.Next(6)), 8 + rng.Next(8), 5);
            }
            else
            {
                // the near bank: big mesquite and palo verde, rocks, and the yellow flood sign by the crossing
                for (double x = -60; x < w + 100; x += 300 + rng.Next(120))
                {
                    if (rng.Next(2) == 0) Mesquite(dc, x, h - 30, 1.15 + rng.NextDouble() * 0.25);
                    else PaloVerde(dc, x, h - 30, 1.1 + rng.NextDouble() * 0.2);
                }
                for (int i = 0; i < 26; i++) { double x = rng.NextDouble() * w, y = h - 10 - rng.Next(70); dc.DrawEllipse(Sphere(i % 3 == 0 ? 0xFF8A7F74 : 0xFF6E5E4E), null, new Point(x, y), 10 + rng.Next(14), 7 + rng.Next(8)); }
                for (double x = 400; x < w; x += 1100) FloodSign(dc, x, h - 230);
            }
        }

        /// <summary>A velvet mesquite: a dark twisting trunk in two or three leaning stems, a wide flat crown of fine
        /// grey-green leaves.</summary>
        static void Mesquite(DrawingContext dc, double x, double y, double k)
        {
            Pen trunk = RoundPen(0xFF3B2A1E, 10 * k), limb = RoundPen(0xFF3B2A1E, 6 * k);
            dc.DrawLine(trunk, new Point(x, y), new Point(x - 14 * k, y - 60 * k));
            dc.DrawLine(trunk, new Point(x, y), new Point(x + 18 * k, y - 54 * k));
            dc.DrawLine(limb, new Point(x - 14 * k, y - 60 * k), new Point(x - 60 * k, y - 96 * k));
            dc.DrawLine(limb, new Point(x + 18 * k, y - 54 * k), new Point(x + 64 * k, y - 92 * k));
            dc.DrawLine(limb, new Point(x - 10 * k, y - 62 * k), new Point(x + 6 * k, y - 110 * k));
            double[] cx = { -80, -40, 0, 42, 82, -58, -12, 30, 62 }, cy = { -96, -112, -118, -110, -94, -128, -138, -132, -116 }, cr = { 30, 36, 38, 36, 28, 26, 30, 28, 22 };
            for (int i = 0; i < cx.Length; i++) dc.DrawEllipse(Gfx.B(0xFF4E5A34), null, new Point(x + cx[i] * k, y + cy[i] * k), cr[i] * k, cr[i] * 0.55 * k);
            for (int i = 0; i < cx.Length; i++) dc.DrawEllipse(Gfx.B(0xFF6F7D47), null, new Point(x + cx[i] * k - 4 * k, y + cy[i] * k - 5 * k), cr[i] * 0.72 * k, cr[i] * 0.36 * k);
        }

        /// <summary>A palo verde: smooth green trunk and branches fanning up, and a thin airy crown of yellow-green.</summary>
        static void PaloVerde(DrawingContext dc, double x, double y, double k)
        {
            Pen trunk = RoundPen(0xFF6E8B3D, 9 * k), limb = RoundPen(0xFF7FA048, 5 * k);
            dc.DrawLine(trunk, new Point(x, y), new Point(x, y - 40 * k));
            foreach (double a in new[] { -140.0, -115, -90, -65, -40 })
            {
                Point s = new Point(x, y - 40 * k), e = Off(s, a, 70 * k);
                dc.DrawLine(limb, s, e);
                for (int i = 0; i < 3; i++) dc.DrawEllipse(Gfx.B(i == 1 ? 0xB0C2CF5A : 0xB09CB54A), null, Off(e, a + (i - 1) * 40, 12 * k), 22 * k, 13 * k);
            }
        }

        /// <summary>The yellow road sign by the crossing: "Do not enter when flooded".</summary>
        static void FloodSign(DrawingContext dc, double x, double y)
        {
            dc.DrawLine(RoundPen(0xFF6B7280, 5), new Point(x, y), new Point(x, y - 110));
            dc.DrawRoundedRectangle(Gfx.B(0xFFFACC15), Gfx.P(0xFF1C1917, 3), new Rect(x - 52, y - 160, 104, 56), 6, 6);
            Gfx.Text(dc, "DO NOT ENTER", x, y - 156, 14, 0xFF1C1917, 0.5, Gfx.UiBold);
            Gfx.Text(dc, "WHEN FLOODED", x, y - 136, 14, 0xFF1C1917, 0.5, Gfx.UiBold);
        }

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
                    dc.DrawLine(RoundPen(Gfx.Alpha(i == 0 ? 0xFF6B4A2A : 0xFF9C774E, 0.95), (6 + 8 * k - i * 2) * Zoom), new Point(a, yy), new Point(b, yy + Math.Sin(t * 8 + i) * Zoom));
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
            if (behind) { dc.DrawGeometry(Linear(0xE0A07A4E, 0xF0866039, 90), null, surf); return; }
            var face = new StreamGeometry();
            using (var c = face.Open())
            {
                c.BeginFigure(new Point(a0, gy + 4 * Zoom), true, true);
                for (int i = 0; i <= steps; i++) c.LineTo(top[i], true, true);
                c.LineTo(new Point(a1, gy + 4 * Zoom), true, true);
            }
            face.Freeze();
            dc.DrawGeometry(Linear(0xF08A6236, 0xF05A3E22, 90), null, face);
            // ripples and glints moving with the water
            for (int i = 0; i < steps; i += 2)
            {
                double ph = (i * 0.37 + t * 1.6) % 1.0;
                Point q = top[i];
                dc.DrawLine(Gfx.P(Gfx.Alpha(0xFFE7D2B0, 0.55 * (1 - ph)), 2 * Zoom), new Point(q.X - 8 * Zoom, q.Y + 4 * Zoom), new Point(q.X + 8 * Zoom, q.Y + 3 * Zoom));
            }
            dc.DrawGeometry(null, Gfx.P(0xA0E7D2B0, 2 * Zoom), StripLine(top));
            // the foamy front: white bubbles tumbling over the top of the wave
            if (front > lx - 30 && front < rx + 30)
            {
                double fy = gy - depth(front - dir * 2);
                for (int i = 0; i < 9; i++)
                {
                    double a = t * 12 + i * 0.8;
                    Point c = new Point(front - dir * (i * 6 + 4) * Zoom + Math.Cos(a) * 4 * Zoom, fy + 2 * Zoom + (i % 3) * 7 * Zoom + Math.Sin(a) * 3 * Zoom);
                    dc.DrawEllipse(Gfx.B(0xE8F5EBDD), null, c, (8 - i * 0.5) * Zoom, (6 - i * 0.4) * Zoom);
                }
                for (int i = 0; i < 5; i++) dc.DrawEllipse(Gfx.B(0xA0F5EBDD), null, new Point(front + dir * (6 + i * 7) * Zoom * ((t * 3 + i * 0.3) % 1.0), gy - (10 + i * 6) * Zoom * Math.Sin(((t * 3 + i * 0.3) % 1.0) * Math.PI)), 2.5 * Zoom, 2.5 * Zoom);
            }
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
                dc.DrawRectangle(Linear(0xD08A6236, 0x008A6236, 90), null, new Rect(lip - (dir > 0 ? 0 : 18 * Zoom), gy - 4 * Zoom, 18 * Zoom, 140 * Zoom));
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
