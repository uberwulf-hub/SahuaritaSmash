// Sahuarita Smash! (Sauce Edition) - Pecos Bill's pictures: the cowboy of the tall tales, drawn our own way from the
// public-domain folk legend only (a lanky cowboy with a wide hat, a bandana, a leather vest, chaps and boots, a lasso).
// His head and shirt are drawn from BrawlView.HeadOf / Torso, his lasso, his twister and his howl from ExtraFront and
// SigFront. Pictures only: the match never reads any of this.
using System;
using Avalonia;
using Avalonia.Media;

namespace ClassicGames
{
    public sealed partial class BrawlView
    {
        const uint PecosRope = 0xFFD6B98C, PecosRopeDark = 0xFF7C5A2E, PecosHat = 0xFFB98A52, PecosVest = 0xFF6B4423;

        /// <summary>His shirt (drawn by Torso, in the body's turned frame: hip at a, the neck at the top, facing +x):
        /// a leather vest open over the denim shirt, a belt with a buckle, the red bandana at the neck and a coil of
        /// spare rope on his hip.</summary>
        void PecosTorso(DrawingContext dc, double cx, double ay, double len, double w, double t)
        {
            double top = ay + len;
            // the vest: the back panel and a narrow front panel, the shirt and its buttons showing between
            dc.DrawRoundedRectangle(Gfx.B(PecosVest), null, new Rect(cx - w / 2 + 2, ay + len * 0.12, w * 0.52, len * 0.8), 4, 4);
            dc.DrawRoundedRectangle(Gfx.B(PecosVest), null, new Rect(cx + w * 0.28, ay + len * 0.12, w * 0.2, len * 0.72), 3, 3);
            for (int i = 0; i < 3; i++) dc.DrawEllipse(Gfx.B(0xFFE5E7EB), null, new Point(cx + w * 0.17, ay + len * (0.35 + i * 0.18)), 1.8, 1.8);
            dc.DrawLine(Gfx.P(Gfx.Mix(PecosVest, 0xFFFDE68A, 0.35), 1.2), new Point(cx - w / 2 + 5, ay + len * 0.2), new Point(cx - w / 2 + 5, ay + len * 0.85));
            // the belt and its big buckle
            dc.DrawRectangle(Gfx.B(0xFF2A1A0E), null, new Rect(cx - w / 2 + 1, ay + len * 0.02, w - 2, len * 0.12));
            dc.DrawRoundedRectangle(Gfx.B(0xFFD4AF37), Gfx.P(0xFF7C5A12, 1), new Rect(cx + w * 0.12, ay + len * 0.0, w * 0.28, len * 0.16), 2, 2);
            // the bandana: a red triangle tied at the neck, white dots
            Poly(dc, Gfx.B(0xFFDC2626), Outline(1.4), new Point(cx - w * 0.42, top - len * 0.02), new Point(cx + w * 0.5, top - len * 0.02), new Point(cx + w * 0.16, top - len * 0.34));
            for (int i = 0; i < 3; i++) dc.DrawEllipse(Gfx.B(0xC0FFFFFF), null, new Point(cx + w * (-0.1 + i * 0.17), top - len * (0.09 + (i % 2) * 0.07)), 1.4, 1.4);
            // the spare coil of rope on his hip
            Point coil = new Point(cx - w * 0.42, ay + len * 0.08);
            for (int i = 0; i < 3; i++) dc.DrawEllipse(null, Gfx.P(i == 1 ? PecosRopeDark : PecosRope, 1.8), new Point(coil.X + i * 1.2, coil.Y - i * 1.0), w * 0.18, w * 0.24);
        }

        /// <summary>His head: a long face with a droopy moustache and a grin, a stalk of grass in the corner of his mouth,
        /// brown hair under a wide hat with a pinched crown and a band.</summary>
        void PecosHead(DrawingContext dc, BFighter f, Point h, double R, bool hurt, BodyStyle st, double t)
        {
            // hair at the back, under the hat
            dc.DrawEllipse(Gfx.B(0xFF5B3A1E), Outline(1.4), new Point(h.X - R * 0.62, h.Y + R * 0.28), R * 0.38, R * 0.42);
            Skull(dc, h, R, f.D.Skin, st);
            dc.DrawEllipse(Gfx.B(0x50F87171), null, new Point(h.X + R * 0.62, h.Y - R * 0.18), R * 0.16, R * 0.1);   // sun on his cheek
            FaceEyes(dc, h, R, hurt, st);
            // the moustache, drooping at the ends, and the grin under it
            Poly(dc, Gfx.B(0xFF5B3A1E), Outline(1.1), new Point(h.X + R * 0.22, h.Y - R * 0.48), new Point(h.X + R * 0.55, h.Y - R * 0.3), new Point(h.X + R * 0.95, h.Y - R * 0.44),
                new Point(h.X + R * 0.9, h.Y - R * 0.62), new Point(h.X + R * 0.6, h.Y - R * 0.44), new Point(h.X + R * 0.3, h.Y - R * 0.64));
            if (hurt) dc.DrawEllipse(Gfx.B(0xFF7F1D1D), null, new Point(h.X + R * 0.6, h.Y - R * 0.66), R * 0.12, R * 0.12);
            else
            {
                dc.DrawGeometry(null, Gfx.P(0xFF7F1D1D, 1.6), Cap(new Point(h.X + R * 0.6, h.Y - R * 0.5), R * 0.22, 210, 330));
                // a stalk of grass, bobbing a little
                double bob = Math.Sin(t * 3) * R * 0.06;
                dc.DrawLine(Gfx.P(0xFFD9C46A, Math.Max(1.4, R * 0.08)), new Point(h.X + R * 0.78, h.Y - R * 0.62), new Point(h.X + R * 1.55, h.Y - R * 0.42 + bob));
            }
            // the hat: a wide brim curling up at both ends, a tall crown pinched at the top, a dark band
            double by = h.Y + R * 0.52;
            var brim = new StreamGeometry();
            using (var s = brim.Open())
            {
                s.BeginFigure(new Point(h.X - R * 1.75, by + R * 0.3), true, true);
                s.QuadraticBezierTo(new Point(h.X - R * 1.3, by - R * 0.12), new Point(h.X + R * 0.1, by - R * 0.12), true, true);
                s.QuadraticBezierTo(new Point(h.X + R * 1.5, by - R * 0.12), new Point(h.X + R * 1.95, by + R * 0.32), true, true);
                s.QuadraticBezierTo(new Point(h.X + R * 1.5, by + R * 0.12), new Point(h.X + R * 0.1, by + R * 0.16), true, true);
                s.QuadraticBezierTo(new Point(h.X - R * 1.3, by + R * 0.12), new Point(h.X - R * 1.75, by + R * 0.3), true, true);
            }
            brim.Freeze();
            var crown = new StreamGeometry();
            using (var s = crown.Open())
            {
                s.BeginFigure(new Point(h.X - R * 0.88, by), true, true);
                s.BezierTo(new Point(h.X - R * 0.95, by + R * 0.6), new Point(h.X - R * 0.75, by + R * 1.05), new Point(h.X - R * 0.35, by + R * 1.08), true, true);
                s.QuadraticBezierTo(new Point(h.X - R * 0.05, by + R * 0.88), new Point(h.X + R * 0.3, by + R * 1.06), true, true);
                s.BezierTo(new Point(h.X + R * 0.75, by + R * 1.04), new Point(h.X + R * 1.0, by + R * 0.6), new Point(h.X + R * 0.98, by), true, true);
            }
            crown.Freeze();
            dc.DrawGeometry(BrawlView.Cylinder(PecosHat), Outline(st.Real ? 1.8 : 2.4), crown);
            dc.DrawRectangle(Gfx.B(0xFF3F2614), null, new Rect(h.X - R * 0.9, by + R * 0.04, R * 1.88, R * 0.2));
            dc.DrawGeometry(Gfx.B(Gfx.Mix(PecosHat, 0xFFFFFFFF, 0.08)), Outline(st.Real ? 1.8 : 2.4), brim);
            dc.DrawLine(Gfx.P(Gfx.Alpha(Rim, 0.6), Math.Max(1.2, R * 0.08)), new Point(h.X - R * 0.6, by + R * 0.9), new Point(h.X - R * 0.2, by + R * 1.0));
        }

        /// <summary>Pecos Bill's specials (local coordinates, y up, facing +x): the lasso twirling overhead, thrown out
        /// to where it hits and drawn back; a little twister under him; a howl's rings.</summary>
        void PecosExtra(DrawingContext dc, BFighter f, Rig r, string extra, bool active, double t)
        {
            BMove m = f.Mv;
            if (m == null) return;
            switch (extra)
            {
                case "rope":
                    {
                        int from = m.FirstActive, to = m.LastActive;
                        Point hand = r.FHand;
                        Pen rope = RoundPen(PecosRope, 3), dark = RoundPen(PecosRopeDark, 5.5);
                        if (f.MF < from)
                        {
                            // the loop spins over his head while he winds up
                            Point c = new Point(r.Head.X - 4, r.Head.Y + r.HeadR * 2.2);
                            double a = f.MF * 40;
                            dc.DrawEllipse(null, dark, c, 34, 10);
                            dc.DrawEllipse(null, rope, c, 34, 10);
                            dc.DrawLine(dark, hand, Off(c, a, 30)); dc.DrawLine(rope, hand, Off(c, a, 30));
                            break;
                        }
                        // out to the hit, then drawn back in over ten frames (held out while someone is caught)
                        double reach = 0, hy = r.Neck.Y;
                        foreach (BHit h in m.Hits) if (Math.Abs(h.X) + h.R * 0.4 > reach) { reach = Math.Abs(h.X) + h.R * 0.4; hy = h.Y; }
                        double k = f.MF <= to ? 1 : Math.Max(0, 1 - (f.MF - to) / 10.0);
                        if (f.Victim >= 0) k = Math.Max(k, 0.45);
                        if (k <= 0) break;
                        Point end = new Point(hand.X + (reach - hand.X) * k, hand.Y + (hy - hand.Y) * k);
                        Point mid = new Point((hand.X + end.X) / 2, Math.Max(hand.Y, end.Y) + 10 * (1 - k) + 6);
                        var g = new StreamGeometry();
                        using (var s = g.Open()) { s.BeginFigure(hand, false, false); s.QuadraticBezierTo(mid, end, true, true); }
                        g.Freeze();
                        dc.DrawGeometry(null, dark, g); dc.DrawGeometry(null, rope, g);
                        double lr = 10 + 10 * k;
                        dc.DrawEllipse(null, dark, new Point(end.X + lr * 0.6, end.Y), lr * 0.7, lr);
                        dc.DrawEllipse(null, rope, new Point(end.X + lr * 0.6, end.Y), lr * 0.7, lr);
                        break;
                    }
                case "twister":
                    {
                        if (f.MF > m.VTo + 6) break;
                        double fade = f.MF > m.VTo ? 1 - (f.MF - m.VTo) / 6.0 : Math.Min(1, f.MF / 5.0);
                        // a little cyclone: rings narrowing to a point below his boots, swaying, dust caught in it
                        for (int i = 0; i < 8; i++)
                        {
                            double y = -70 + i * 13, rad = 6 + i * 6.5, sway = Math.Sin(t * 14 + i * 0.7) * (8 - i) * 1.4;
                            uint c = Gfx.Alpha(i % 2 == 0 ? 0xFFCBD5E1 : 0xFFA8A29E, (0.55 + 0.25 * (i % 2)) * fade);
                            dc.DrawEllipse(null, Gfx.P(c, 4.5), new Point(sway, y), rad, rad * 0.28);
                        }
                        for (int i = 0; i < 7; i++)
                        {
                            double a = t * 9 + i * 0.9, yy = -60 + (i * 17 + f.MF * 4) % 90;
                            dc.DrawEllipse(Gfx.B(Gfx.Alpha(0xFF8B6B4A, 0.8 * fade)), null, new Point(Math.Cos(a) * (10 + (yy + 70) * 0.45), yy), 2.6, 2.6);
                        }
                        break;
                    }
                case "howl":
                    {
                        // he throws back his head and howls: rings rising from his mouth, then a push all round him
                        Point mouth = new Point(r.Head.X + r.HeadR * 0.7, r.Head.Y + r.HeadR * 0.2);
                        if (f.MF >= 6 && f.MF <= m.Len - 6)
                            for (int i = 0; i < 3; i++)
                            {
                                double k = ((f.MF - 6) * 0.06 + i / 3.0) % 1.0;
                                Point c = Off(mouth, 60, 10 + 50 * k);
                                dc.DrawGeometry(null, Gfx.P(Gfx.Alpha(0xFFFDE68A, 0.9 * (1 - k)), 3), Cap(c, 8 + 16 * k, 10, 110));
                            }
                        if (active)
                        {
                            double k = Math.Min(1, (f.MF - 11) / 5.0);
                            dc.DrawEllipse(null, Gfx.P(Gfx.Alpha(0xFFFDE68A, 0.8 * (1 - k * 0.6)), 4), new Point(0, 50), 30 + 50 * k, 26 + 34 * k);
                            dc.DrawEllipse(null, Gfx.P(Gfx.Alpha(0xFFFFFFFF, 0.5 * (1 - k)), 2), new Point(0, 50), 20 + 46 * k, 18 + 30 * k);
                        }
                        break;
                    }
            }
        }

        /// <summary>His ordinary attacks' effects: a rope cracking out from his hand, a little gust around an uppercut.</summary>
        void PecosSig(DrawingContext dc, BFighter f, Rig r, string extra, Point tip, double t)
        {
            switch (extra)
            {
                case "crack":
                    {
                        Point hand = r.FHand, end = Off(hand, r.FArmAng, 58);
                        Point mid = Off(Off(hand, r.FArmAng, 30), r.FArmAng + 90, Math.Sin(f.MF * 1.3) * 10);
                        var g = new StreamGeometry();
                        using (var s = g.Open()) { s.BeginFigure(hand, false, false); s.QuadraticBezierTo(mid, end, true, true); }
                        g.Freeze();
                        dc.DrawGeometry(null, RoundPen(PecosRopeDark, 5), g); dc.DrawGeometry(null, RoundPen(PecosRope, 2.8), g);
                        Star(dc, end.X, end.Y, 9, 3.5, f.MF * 0.5, 0xFFFEF9C3, 0xFFCA8A04);
                        break;
                    }
                case "gust":
                    for (int i = 0; i < 3; i++)
                    {
                        Point c = new Point(tip.X, tip.Y - 10 - i * 14);
                        dc.DrawGeometry(null, Gfx.P(Gfx.Alpha(0xFFE2E8F0, 0.75 - i * 0.18), 3), Cap(c, 14 + i * 6, f.MF * 30 + i * 60, f.MF * 30 + i * 60 + 200));
                    }
                    break;
            }
        }
    }
}
