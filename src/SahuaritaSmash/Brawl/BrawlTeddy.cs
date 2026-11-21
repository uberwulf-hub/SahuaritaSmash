// Sahuarita Smash! (Sauce Edition) - Teddy's pictures on the shared fighter style (Creative Collaboration Test 003,
// round 1: art direction ChatGPT, game director Andrew, 2026-11-20). A respectful caricature of Theodore Roosevelt
// (1858-1919): a green safari jacket as his signature colour, a campaign hat, round spectacles, the big moustache and
// grin, and his big stick, which rests over his back shoulder unless he's swinging it. Sizes in the face are shares of
// the head's radius R (FighterInk's rules); everything is drawn in the fighter's own frame (local y is up, facing +x).
// Pictures only: the match never reads any of this.
using System;
using Avalonia;
using Avalonia.Media;

namespace ClassicGames
{
    public sealed partial class BrawlView
    {
        const uint TeddyHat = 0xFF8B5A2B, TeddyHatCrown = 0xFFA0703A, TeddyLeather = 0xFF4A2E14, TeddyBrass = 0xFFD4A84A,
            TeddyHair = 0xFF6B4423, TeddyWood = 0xFF8A5A2B;

        /// <summary>The jacket's details (drawn by Torso in the body's turned frame: hip at ay, the neck at ay + len):
        /// lapels, a red neckerchief, a button line with brass buttons, a chest pocket and a lower pocket, a belt.</summary>
        static void TeddyJacket(DrawingContext dc, BFighterDef d, double cx, double ay, double len, double w)
        {
            double top = ay + len;
            uint seam = Gfx.Mix(d.Main, 0xFF000000, 0.35), lit = Gfx.Mix(d.Main, 0xFFFFFFFF, 0.18);
            Pen detail = FighterInk.Pen(FighterInk.Detail);
            // the belt and its buckle
            dc.DrawRectangle(Gfx.B(TeddyLeather), detail, new Rect(cx - w * 0.47, ay + len * 0.06, w * 0.94, len * 0.13));
            dc.DrawRoundedRectangle(Gfx.B(TeddyBrass), detail, new Rect(cx + w * 0.1, ay + len * 0.04, w * 0.2, len * 0.17), 2, 2);
            // pockets: a chest pocket with a flap, a bigger one low on the jacket
            dc.DrawRoundedRectangle(Gfx.B(lit), detail, new Rect(cx - w * 0.06, top - len * 0.52, w * 0.3, len * 0.2), 3, 3);
            dc.DrawRectangle(Gfx.B(seam), null, new Rect(cx - w * 0.06, top - len * 0.36, w * 0.3, len * 0.05));
            dc.DrawRoundedRectangle(Gfx.B(lit), detail, new Rect(cx - w * 0.36, ay + len * 0.24, w * 0.34, len * 0.22), 3, 3);
            dc.DrawRectangle(Gfx.B(seam), null, new Rect(cx - w * 0.36, ay + len * 0.4, w * 0.34, len * 0.05));
            // the button line and brass buttons
            dc.DrawLine(Gfx.P(seam, Math.Max(1.5, w * 0.03)), new Point(cx + w * 0.34, ay + len * 0.2), new Point(cx + w * 0.34, top - len * 0.22));
            for (int i = 0; i < 3; i++)
                dc.DrawEllipse(Gfx.B(TeddyBrass), FighterInk.Pen(1.2), new Point(cx + w * 0.4, ay + len * (0.32 + i * 0.17)), w * 0.045, w * 0.045);
            // lapels and the red neckerchief
            Poly(dc, Gfx.B(lit), detail, new Point(cx - w * 0.18, top - len * 0.02), new Point(cx + w * 0.12, top - len * 0.02), new Point(cx + w * 0.02, top - len * 0.3));
            Poly(dc, Gfx.B(lit), detail, new Point(cx + w * 0.12, top - len * 0.02), new Point(cx + w * 0.44, top - len * 0.04), new Point(cx + w * 0.26, top - len * 0.32));
            Poly(dc, Gfx.B(0xFFDC2626), detail, new Point(cx + w * 0.06, top - len * 0.04), new Point(cx + w * 0.34, top - len * 0.05), new Point(cx + w * 0.2, top - len * 0.26));
            dc.DrawEllipse(Gfx.B(0xFFB91C1C), FighterInk.Pen(1.2), new Point(cx + w * 0.2, top - len * 0.07), w * 0.06, w * 0.05);
        }

        /// <summary>His face over the shared eyes: round spectacles, bushy brows, the big moustache, a wide grin, and the
        /// campaign hat.</summary>
        static void TeddyFace(DrawingContext dc, Point h, double R, BFighterDef d, bool hurt)
        {
            Pen detail = FighterInk.Pen(Math.Max(1.4, R * 0.07));
            // brows, a little raised: cheerful and determined
            var brow = new Pen(Gfx.B(TeddyHair), Math.Max(2, R * 0.11), null, PenLineCap.Round, PenLineJoin.Round); brow.Freeze();
            dc.DrawLine(brow, new Point(h.X + R * 0, h.Y + R * 0.43), new Point(h.X + R * 0.28, h.Y + R * 0.49));
            dc.DrawLine(brow, new Point(h.X + R * 0.44, h.Y + R * 0.51), new Point(h.X + R * 0.8, h.Y + R * 0.46));
            // the spectacles: two round lenses with a glint, a bridge and a little cord
            foreach (double ex in new[] { 0.16, 0.58 })
            {
                double k = ex < 0.5 ? 0.8 : 1, lr = R * 0.23 * k;
                var e = new Point(h.X + R * ex, h.Y + R * 0.14);
                dc.DrawEllipse(Gfx.B(Gfx.Alpha(0xFFE0F2FE, 0.18)), detail, e, lr, lr);
                dc.DrawGeometry(null, Gfx.P(Gfx.Alpha(0xFFFFFFFF, 0.8), Math.Max(1, R * 0.045)), Cap(e, lr * 0.7, 100, 150));
            }
            dc.DrawGeometry(null, detail, Cap(new Point(h.X + R * 0.27, h.Y + R * 0.12), R * 0.06, 20, 160));
            // the grin first (the moustache hangs over its top)
            if (hurt)
                dc.DrawEllipse(Gfx.B(0xFF7F1D1D), detail, new Point(h.X + R * 0.52, h.Y - R * 0.55), R * 0.13, R * 0.15);
            else
            {
                var g = new StreamGeometry();
                using (var s = g.Open())
                {
                    s.BeginFigure(new Point(h.X + R * 0.2, h.Y - R * 0.38), true, true);
                    s.LineTo(new Point(h.X + R * 0.9, h.Y - R * 0.36), true, true);
                    s.QuadraticBezierTo(new Point(h.X + R * 0.85, h.Y - R * 0.78), new Point(h.X + R * 0.5, h.Y - R * 0.74), true, true);
                    s.QuadraticBezierTo(new Point(h.X + R * 0.24, h.Y - R * 0.7), new Point(h.X + R * 0.2, h.Y - R * 0.38), true, true);
                }
                g.Freeze();
                dc.DrawGeometry(Gfx.B(0xFFFFFFFF), detail, g);
                dc.DrawLine(Gfx.P(0xFFD6D3D1, Math.Max(1, R * 0.04)), new Point(h.X + R * 0.24, h.Y - R * 0.52), new Point(h.X + R * 0.86, h.Y - R * 0.5));
                for (int i = 1; i < 4; i++) dc.DrawLine(Gfx.P(0xFFD6D3D1, Math.Max(1, R * 0.03)), new Point(h.X + R * (0.2 + i * 0.17), h.Y - R * 0.38), new Point(h.X + R * (0.2 + i * 0.17), h.Y - R * 0.62));
            }
            // the moustache: two bushy lobes under the nose
            foreach (var m in new[] { new[] { 0.34, -0.3, 0.32, 0.15, -10 }, new[] { 0.76, -0.29, 0.27, 0.14, 14 } })
            {
                var c = new Point(h.X + R * m[0], h.Y + R * m[1]);
                dc.PushTransform(new RotateTransform(m[4], c.X, c.Y));
                dc.DrawEllipse(Gfx.B(TeddyHair), detail, c, R * m[2], R * m[3]);
                dc.DrawLine(Gfx.P(Gfx.Mix(TeddyHair, 0xFFFFFFFF, 0.25), Math.Max(1, R * 0.04)), new Point(c.X - R * m[2] * 0.5, c.Y + R * 0.03), new Point(c.X + R * m[2] * 0.4, c.Y + R * 0.05));
                dc.Pop();
            }
            // short hair at the back of the head, under the hat
            dc.DrawGeometry(Gfx.B(TeddyHair), detail, Cap(new Point(h.X - R * 0.05, h.Y + R * 0.05), R * 0.95, 120, 200));
            // the campaign hat: a wide brim, a creased crown, a leather band
            var crown = new StreamGeometry();
            using (var s = crown.Open())
            {
                s.BeginFigure(new Point(h.X - R * 0.8, h.Y + R * 0.68), true, true);
                s.QuadraticBezierTo(new Point(h.X - R * 0.82, h.Y + R * 1.25), new Point(h.X - R * 0.32, h.Y + R * 1.42), true, true);
                s.QuadraticBezierTo(new Point(h.X - R * 0.12, h.Y + R * 1.3), new Point(h.X + R * 0.02, h.Y + R * 1.32), true, true);
                s.QuadraticBezierTo(new Point(h.X + R * 0.16, h.Y + R * 1.3), new Point(h.X + R * 0.36, h.Y + R * 1.42), true, true);
                s.QuadraticBezierTo(new Point(h.X + R * 0.86, h.Y + R * 1.25), new Point(h.X + R * 0.8, h.Y + R * 0.68), true, true);
            }
            crown.Freeze();
            dc.DrawGeometry(BrawlView.Sphere(TeddyHatCrown), FighterInk.Pen(FighterInk.Part), crown);
            dc.DrawRectangle(Gfx.B(TeddyLeather), null, new Rect(h.X - R * 0.8, h.Y + R * 0.68, R * 1.6, R * 0.16));
            dc.DrawEllipse(Gfx.B(TeddyHat), FighterInk.Pen(FighterInk.Part), new Point(h.X + R * 0.05, h.Y + R * 0.64), R * 1.45, R * 0.2);
            dc.DrawLine(Gfx.P(Gfx.Mix(TeddyHat, 0xFFFFFFFF, 0.25), Math.Max(1, R * 0.05)), new Point(h.X - R * 1.1, h.Y + R * 0.68), new Point(h.X + R * 1.2, h.Y + R * 0.68));
        }

        /// <summary>The big stick: a tapered wooden club from start, pointing ang degrees, len long, thicker at the end.</summary>
        static void TeddyStick(DrawingContext dc, Point start, double ang, double len)
        {
            Point e = Off(start, ang, len);
            var g = Seg(start, e, 6, 10, 0.05);
            dc.DrawGeometry(Gfx.B(TeddyWood), FighterInk.Pen(FighterInk.Part), g);
            dc.DrawLine(Gfx.P(Gfx.Mix(TeddyWood, 0xFFFFFFFF, 0.3), 1.6), Off(Off(start, ang, len * 0.15), ang + 90, 1.4), Off(Off(start, ang, len * 0.85), ang + 90, 2.4));
            dc.DrawLine(Gfx.P(Gfx.Mix(TeddyWood, 0xFF000000, 0.35), 1.2), Off(Off(start, ang, len * 0.35), ang - 90, 1.2), Off(Off(start, ang, len * 0.6), ang - 90, 1.6));
            dc.DrawEllipse(Gfx.B(Gfx.Mix(TeddyWood, 0xFF000000, 0.2)), FighterInk.Pen(FighterInk.Detail), e, 5.2, 5.2);
        }
    }
}
