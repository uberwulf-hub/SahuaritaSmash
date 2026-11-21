// Sahuarita Smash! (Sauce Edition) - Ben Franklin's and Paul Bunyan's pictures on the shared fighter style
// (Creative Collaboration Test 003, round 2: art direction ChatGPT, game director Andrew, 2026-11-21).
// Ben: a kind, clever, round inventor (a respectful caricature of Benjamin Franklin, 1706-1790): a deep teal coat over a
// big cream waistcoat, white hair at the sides of a bald crown, square spectacles, a gentle smile, and his cane upright
// beside him with a big gold key at the top. Paul: the giant lumberjack of the public-domain tall tales: a red-and-white
// bobble hat, a big brown beard in a few round masses, a red check shirt in large blocks, blue jeans, big boots, and his
// axe handle (never a blade) upright behind his far shoulder until he swings it. The spec's colours (base / shadow /
// highlight) are the constants below and FighterInk.Tones. Sizes in faces are shares of the head radius R; everything
// is in the fighter's own frame (local y is up, facing +x). Pictures only: the match never reads any of this.
using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;

namespace ClassicGames
{
    public sealed partial class BrawlView
    {
        // Ben
        const uint BenTrousers = 0xFF38404A, BenShirt = 0xFFF3EBDD, BenShirtShade = 0xFFC9BDA6, BenHair = 0xFFE8E3D8, BenHairShade = 0xFFB9B5AC,
            BenHairLight = 0xFFFFF9EC, BenBrow = 0xFFD0C8B9, BenBrown = 0xFF49352D, BenBrownLight = 0xFF806047,
            BenKey = 0xFFE4B33D, BenKeyShade = 0xFFA8751C, BenKeyLight = 0xFFFFE078, BenCoatShade = 0xFF193E49, BenCoatLight = 0xFF43818C,
            BenVestShade = 0xFFA99570, BenVestLight = 0xFFF3E6C6;
        // Paul
        const uint PaulRed = 0xFFB91C1C, PaulRedShade = 0xFF751B20, PaulCream = 0xFFF3EBDD, PaulCreamShade = 0xFFC8C1B5,
            PaulBeard = 0xFF70462F, PaulBeardShade = 0xFF422B23, PaulBeardLight = 0xFF9A6948, PaulBelt = 0xFF49352D,
            PaulWood = 0xFF93613B, PaulWoodShade = 0xFF5D3928, PaulWoodLight = 0xFFC18B55;

        // ---------------------------------------------------------------- Ben
        /// <summary>Ben's coat details (Torso's turned frame: hip at ay, the neck at ay + len, facing +x): a big cream
        /// waistcoat from the chest to the belt line, a cream shirt collar and jabot, simple teal lapels.</summary>
        static void BenCoat(DrawingContext dc, BFighterDef d, double cx, double ay, double len, double w)
        {
            double top = ay + len;
            Pen detail = FighterInk.Pen(FighterInk.Detail);
            // the waistcoat: the whole front of his round middle, clipped to the body so it follows the belly; its shadow
            // along the bottom
            Geometry shape = BodyShape(cx, ay - w * 0.15, ay + len + w * 0.05, w, BodyStyle.For(d.Id));
            dc.PushClip(shape);
            var vest = new Rect(cx + w * 0.02, ay - w * 0.2, w * 0.9, len * 0.92 + w * 0.2);
            dc.DrawRoundedRectangle(Gfx.B(d.Second), detail, vest, w * 0.16, w * 0.16);
            dc.DrawRectangle(Gfx.B(Gfx.Alpha(BenVestShade, 0.9)), null, new Rect(vest.X, ay - w * 0.2, vest.Width, len * 0.16 + w * 0.2));
            dc.DrawLine(Gfx.P(BenVestLight, Math.Max(1.5, w * 0.05)), new Point(vest.X + w * 0.12, ay + len * 0.25), new Point(vest.X + w * 0.12, ay + len * 0.7));
            // three big buttons down its middle
            for (int i = 0; i < 3; i++)
                dc.DrawEllipse(Gfx.B(BenVestShade), FighterInk.Pen(1.2), new Point(cx + w * 0.3, ay + len * (0.28 + i * 0.18)), w * 0.055, w * 0.055);
            dc.Pop();
            dc.DrawGeometry(null, FighterInk.Pen(FighterInk.Limb), shape);
            // the teal lapels at the waistcoat's back edge, and the shirt's collar and jabot at the neck
            Poly(dc, Gfx.B(BenCoatLight), detail, new Point(cx - w * 0.12, top - len * 0.02), new Point(cx + w * 0.02, top - len * 0.02), new Point(cx - w * 0.06, ay + len * 0.45));
            Poly(dc, Gfx.B(BenShirt), detail, new Point(cx + w * 0.02, top - len * 0.0), new Point(cx + w * 0.4, top - len * 0.02), new Point(cx + w * 0.3, top - len * 0.26), new Point(cx + w * 0.12, top - len * 0.24));
            dc.DrawLine(Gfx.P(BenShirtShade, Math.Max(1, w * 0.03)), new Point(cx + w * 0.16, top - len * 0.06), new Point(cx + w * 0.24, top - len * 0.2));
        }

        /// <summary>The white hair behind his head: a big round mass at the back.</summary>
        static void BenHairBack(DrawingContext dc, Point h, double R, BFighterDef d)
        {
            dc.DrawEllipse(Gfx.B(BenHairShade), FighterInk.Pen(FighterInk.Head), new Point(h.X - R * 0.5, h.Y - R * 0.12), R * 0.62, R * 0.9);
        }

        /// <summary>His face over the shared eyes: the white side hair over the ear, brows, square spectacles, a gentle
        /// smile.</summary>
        static void BenFace(DrawingContext dc, Point h, double R, BFighterDef d, bool hurt)
        {
            Pen detail = FighterInk.Pen(FighterInk.Detail);
            // the side hair: one long soft shape from the temple down past the ear to the collar, a wavy bottom edge; the
            // crown stays bald
            var hair = new StreamGeometry();
            using (var s = hair.Open())
            {
                s.BeginFigure(new Point(h.X - R * 0.1, h.Y + R * 0.42), true, true);
                s.QuadraticBezierTo(new Point(h.X - R * 0.7, h.Y + R * 0.55), new Point(h.X - R * 0.92, h.Y + R * 0.1), true, true);
                s.QuadraticBezierTo(new Point(h.X - R * 1.08, h.Y - R * 0.35), new Point(h.X - R * 0.86, h.Y - R * 0.72), true, true);
                s.QuadraticBezierTo(new Point(h.X - R * 0.7, h.Y - R * 0.6), new Point(h.X - R * 0.58, h.Y - R * 0.74), true, true);
                s.QuadraticBezierTo(new Point(h.X - R * 0.44, h.Y - R * 0.6), new Point(h.X - R * 0.3, h.Y - R * 0.66), true, true);
                s.QuadraticBezierTo(new Point(h.X - R * 0.12, h.Y - R * 0.3), new Point(h.X - R * 0.2, h.Y + R * 0.05), true, true);
                s.QuadraticBezierTo(new Point(h.X - R * 0.02, h.Y + R * 0.25), new Point(h.X - R * 0.1, h.Y + R * 0.42), true, true);
            }
            hair.Freeze();
            dc.DrawGeometry(Gfx.B(d.Trim), detail, hair);
            var strand = new Pen(Gfx.B(BenHairShade), Math.Max(1, R * 0.06), null, PenLineCap.Round, PenLineJoin.Round); strand.Freeze();
            dc.DrawLine(strand, new Point(h.X - R * 0.62, h.Y + R * 0.05), new Point(h.X - R * 0.6, h.Y - R * 0.45));
            dc.DrawLine(strand, new Point(h.X - R * 0.38, h.Y - R * 0.05), new Point(h.X - R * 0.4, h.Y - R * 0.45));
            dc.DrawLine(Gfx.P(BenHairLight, Math.Max(1.2, R * 0.08)), new Point(h.X - R * 0.7, h.Y + R * 0.3), new Point(h.X - R * 0.36, h.Y + R * 0.34));
            // brows: soft grey-white, about 2.7 units thick
            var brow = new Pen(Gfx.B(BenBrow), Math.Max(2, R * 0.17), null, PenLineCap.Round, PenLineJoin.Round); brow.Freeze();
            dc.DrawLine(brow, new Point(h.X + R * 0.02, h.Y + R * 0.46), new Point(h.X + R * 0.28, h.Y + R * 0.5));
            dc.DrawLine(brow, new Point(h.X + R * 0.44, h.Y + R * 0.52), new Point(h.X + R * 0.78, h.Y + R * 0.48));
            // square spectacles: thick dark-brown frames, broad lenses, a glint
            var frame = new Pen(Gfx.B(BenBrown), 2.6, null, PenLineCap.Round, PenLineJoin.Round); frame.Freeze();
            foreach (double ex in new[] { 0.16, 0.58 })
            {
                double k = ex < 0.5 ? 0.8 : 1, s = R * 0.23 * k;
                var e = new Point(h.X + R * ex, h.Y + R * 0.13);
                var lens = new Rect(e.X - s, e.Y - s * 0.85, s * 2, s * 1.7);
                dc.DrawRoundedRectangle(Gfx.B(Gfx.Alpha(0xFFE0F2FE, 0.16)), FighterInk.Pen(2.6 + 1.6), lens, s * 0.25, s * 0.25);
                dc.DrawRoundedRectangle(null, frame, lens, s * 0.25, s * 0.25);
                dc.DrawLine(Gfx.P(Gfx.Alpha(0xFFFFFFFF, 0.75), Math.Max(1, R * 0.05)), new Point(lens.X + s * 0.35, lens.Bottom - s * 0.3), new Point(lens.X + s * 0.8, lens.Bottom - s * 0.3));
            }
            dc.DrawLine(frame, new Point(h.X + R * 0.3, h.Y + R * 0.2), new Point(h.X + R * 0.31, h.Y + R * 0.2));
            // a gentle smile, about 8 units wide
            if (hurt) dc.DrawEllipse(Gfx.B(0xFF7F1D1D), detail, new Point(h.X + R * 0.48, h.Y - R * 0.5), R * 0.11, R * 0.13);
            else
            {
                var m = new Pen(Gfx.B(0xFF5B2A1E), Math.Max(1.6, R * 0.1), null, PenLineCap.Round, PenLineJoin.Round); m.Freeze();
                dc.DrawGeometry(null, m, Cap(new Point(h.X + R * 0.46, h.Y - R * 0.3), R * 0.26, 215, 325));
            }
        }

        /// <summary>The cane while he swings it: along his arm from start, the key at its tip.</summary>
        static void BenCane(DrawingContext dc, Point start, double ang, double len)
        {
            Point e = Off(start, ang, len);
            dc.DrawLine(FighterInk.Pen(5.5 + 2 * FighterInk.Part), start, e);
            dc.DrawLine(Gfx.P(BenBrown, 5.5), start, e);
            dc.DrawLine(Gfx.P(BenBrownLight, 1.6), Off(Off(start, ang, len * 0.1), ang + 90, 1.2), Off(Off(start, ang, len * 0.9), ang + 90, 1.2));
            BenKeyAt(dc, e, ang);
        }

        /// <summary>Standing: the cane upright beside his front leg, his front hand on it at the waist, the gold key above.</summary>
        void BenCaneIdle(DrawingContext dc, Rig r)
        {
            double groundY = Math.Min(r.FFoot.Y, r.BFoot.Y) - r.LegW * 0.45;
            var topP = new Point(r.FHand.X + 2, r.FHand.Y + 6);
            var bottom = new Point(topP.X + 3, groundY);
            dc.DrawLine(FighterInk.Pen(5.5 + 2 * FighterInk.Part), bottom, topP);
            dc.DrawLine(Gfx.P(BenBrown, 5.5), bottom, topP);
            dc.DrawLine(Gfx.P(BenBrownLight, 1.6), new Point(bottom.X + 1.2, bottom.Y + 4), new Point(topP.X + 1.2, topP.Y - 4));
            dc.DrawEllipse(Gfx.B(BenBrown), FighterInk.Pen(FighterInk.Detail), new Point(bottom.X, bottom.Y), 3.4, 2.2);   // the ferrule
            BenKeyAt(dc, topP, 90);
        }

        /// <summary>The big gold key: its shaft along ang from p, a round bow at the end and a toothed bit near p.</summary>
        static void BenKeyAt(DrawingContext dc, Point p, double ang)
        {
            Point bowC = Off(p, ang, 12);
            dc.DrawLine(FighterInk.Pen(3.2 + 2 * FighterInk.Detail), p, Off(p, ang, 7));
            dc.DrawLine(Gfx.P(BenKey, 3.2), p, Off(p, ang, 7));
            Point bit = Off(p, ang, 2.5);
            Poly(dc, Gfx.B(BenKey), FighterInk.Pen(FighterInk.Detail), Off(bit, ang + 90, 1.6), Off(Off(bit, ang, 3.2), ang + 90, 1.6), Off(Off(bit, ang, 3.2), ang + 90, 6), Off(bit, ang + 90, 6));
            dc.DrawEllipse(Gfx.B(BenKey), FighterInk.Pen(FighterInk.Detail), bowC, 5.6, 5.6);
            dc.DrawEllipse(Gfx.B(BenKeyShade), null, bowC, 2.3, 2.3);
            dc.DrawGeometry(null, Gfx.P(BenKeyLight, 1.4), Cap(bowC, 4.2, 100, 170));
        }

        // ---------------------------------------------------------------- Paul
        /// <summary>Paul's shirt (Torso's turned frame): big red-and-cream check blocks (about 9 units) clipped to his
        /// body, mostly red; a wide leather belt with a buckle.</summary>
        static void PaulShirt(DrawingContext dc, BFighterDef d, double cx, double ay, double len, double w)
        {
            BodyStyle st = BodyStyle.For(d.Id);
            Geometry shape = BodyShape(cx, ay - w * 0.15, ay + len + w * 0.05, w, st);
            double cell = 9, x0 = cx - w, x1 = cx + w, y0 = ay - w * 0.2, y1 = ay + len + w * 0.1;
            uint stripe = Gfx.Mix(d.Main, PaulCream, 0.28);
            dc.PushClip(shape);
            int j = 0;
            for (double y = y0; y < y1; y += cell, j++)
            {
                int i = 0;
                for (double x = x0; x < x1; x += cell, i++)
                {
                    bool ci = i % 2 == 0, cj = j % 2 == 0;
                    if (ci && cj) dc.DrawRectangle(Gfx.B(Gfx.Alpha(PaulCream, 0.9)), null, new Rect(x, y, cell, cell));
                    else if (ci || cj) dc.DrawRectangle(Gfx.B(Gfx.Alpha(stripe, 0.55)), null, new Rect(x, y, cell, cell));
                }
            }
            // the round body's shading over the checks: darker at the back, a soft light at the front
            dc.DrawRectangle(Gfx.B(Gfx.Alpha(PaulRedShade, 0.45)), null, new Rect(cx - w, y0, w * 0.55, y1 - y0));
            dc.DrawRectangle(Gfx.B(Gfx.Alpha(0xFFFFFFFF, 0.08)), null, new Rect(cx + w * 0.05, y0, w * 0.3, y1 - y0));
            dc.Pop();
            dc.DrawGeometry(null, FighterInk.Pen(FighterInk.Limb), shape);
            // the belt and its buckle
            dc.DrawRectangle(Gfx.B(PaulBelt), FighterInk.Pen(FighterInk.Detail), new Rect(cx - w * 0.5, ay + len * 0.03, w, len * 0.11));
            dc.DrawRoundedRectangle(Gfx.B(0xFFB8A27A), FighterInk.Pen(1.4), new Rect(cx + w * 0.14, ay + len * 0.015, w * 0.18, len * 0.14), 2, 2);
        }

        /// <summary>His face over the shared eyes: thick brows, the beard in big round masses, a wide grin above it, and
        /// the red-and-white bobble hat (its brim above the brows, so the eyes stay clear).</summary>
        static void PaulFace(DrawingContext dc, Point h, double R, BFighterDef d, bool hurt)
        {
            Pen detail = FighterInk.Pen(FighterInk.Detail);
            var brow = new Pen(Gfx.B(PaulBeardShade), Math.Max(2.5, R * 0.17), null, PenLineCap.Round, PenLineJoin.Round); brow.Freeze();
            dc.DrawLine(brow, new Point(h.X + R * 0.0, h.Y + R * 0.42), new Point(h.X + R * 0.3, h.Y + R * 0.47));
            dc.DrawLine(brow, new Point(h.X + R * 0.44, h.Y + R * 0.48), new Point(h.X + R * 0.8, h.Y + R * 0.43));
            // the beard: a sideburn and three big round masses, one ink outline round them all, then the fill
            var masses = new[] { new[] { -0.46, -0.3, 0.26, 0.36 }, new[] { -0.05, -0.58, 0.55, 0.5 }, new[] { 0.45, -0.66, 0.58, 0.46 }, new[] { 0.88, -0.48, 0.3, 0.34 } };
            foreach (var m in masses) dc.DrawEllipse(Gfx.B(FighterInk.Ink), null, new Point(h.X + R * m[0], h.Y + R * m[1]), R * m[2] + FighterInk.Head, R * m[3] + FighterInk.Head);
            foreach (var m in masses) dc.DrawEllipse(Gfx.B(PaulBeard), null, new Point(h.X + R * m[0], h.Y + R * m[1]), R * m[2], R * m[3]);
            dc.DrawGeometry(null, Gfx.P(PaulBeardLight, Math.Max(1.4, R * 0.07)), Cap(new Point(h.X + R * 0.4, h.Y - R * 0.62), R * 0.34, 120, 170));
            dc.DrawGeometry(null, Gfx.P(PaulBeardShade, Math.Max(1.4, R * 0.07)), Cap(new Point(h.X + R * 0.2, h.Y - R * 0.58), R * 0.42, 230, 300));
            // the grin, about 11 units wide, sitting on top of the beard
            if (hurt) dc.DrawEllipse(Gfx.B(0xFF7F1D1D), detail, new Point(h.X + R * 0.52, h.Y - R * 0.36), R * 0.12, R * 0.14);
            else
            {
                var g = new StreamGeometry();
                using (var s = g.Open())
                {
                    s.BeginFigure(new Point(h.X + R * 0.26, h.Y - R * 0.28), true, true);
                    s.LineTo(new Point(h.X + R * 0.8, h.Y - R * 0.26), true, true);
                    s.QuadraticBezierTo(new Point(h.X + R * 0.74, h.Y - R * 0.56), new Point(h.X + R * 0.52, h.Y - R * 0.54), true, true);
                    s.QuadraticBezierTo(new Point(h.X + R * 0.3, h.Y - R * 0.52), new Point(h.X + R * 0.26, h.Y - R * 0.28), true, true);
                }
                g.Freeze();
                dc.DrawGeometry(Gfx.B(0xFFFFFFFF), detail, g);
                dc.DrawLine(Gfx.P(0xFFD6D3D1, Math.Max(1, R * 0.035)), new Point(h.X + R * 0.3, h.Y - R * 0.38), new Point(h.X + R * 0.76, h.Y - R * 0.37));
            }
            // the hat: a round red body a little oversized, a white band at the brow, a big white bobble
            dc.DrawGeometry(BrawlView.Sphere(PaulRed), FighterInk.Pen(FighterInk.Part), Cap(new Point(h.X - R * 0.02, h.Y + R * 0.62), R * 1.06, 0, 180));
            dc.DrawGeometry(null, Gfx.P(PaulRedShade, Math.Max(1.4, R * 0.08)), Cap(new Point(h.X - R * 0.02, h.Y + R * 0.62), R * 0.7, 120, 160));
            var band = new Rect(h.X - R * 1.12, h.Y + R * 0.52, R * 2.2, R * 0.3);
            dc.DrawRoundedRectangle(Gfx.B(PaulCream), FighterInk.Pen(FighterInk.Part), band, R * 0.12, R * 0.12);
            dc.DrawLine(Gfx.P(PaulCreamShade, Math.Max(1, R * 0.06)), new Point(band.X + R * 0.2, band.Y + R * 0.08), new Point(band.Right - R * 0.2, band.Y + R * 0.08));
            var bob = new Point(h.X - R * 0.12, h.Y + R * 1.78);
            dc.DrawEllipse(Gfx.B(PaulCream), FighterInk.Pen(FighterInk.Part), bob, R * 0.34, R * 0.34);
            dc.DrawEllipse(Gfx.B(PaulCreamShade), null, new Point(bob.X - R * 0.08, bob.Y - R * 0.1), R * 0.16, R * 0.12);
            dc.DrawEllipse(Gfx.B(0xFFFFF9EF), null, new Point(bob.X + R * 0.08, bob.Y + R * 0.1), R * 0.1, R * 0.08);
        }

        /// <summary>The axe handle: a thick wooden shaft from start along ang, a round butt end (no head, no blade).</summary>
        static void PaulHandle(DrawingContext dc, Point start, double ang, double len)
        {
            Point e = Off(start, ang, len);
            var g = Seg(start, e, 11, 13, 0.04);
            dc.DrawGeometry(Gfx.B(PaulWood), FighterInk.Pen(FighterInk.Part), g);
            dc.DrawLine(Gfx.P(PaulWoodLight, 2.4), Off(Off(start, ang, len * 0.1), ang + 90, 2.6), Off(Off(start, ang, len * 0.9), ang + 90, 3.2));
            dc.DrawLine(Gfx.P(PaulWoodShade, 2), Off(Off(start, ang, len * 0.2), ang - 90, 2.6), Off(Off(start, ang, len * 0.85), ang - 90, 3.4));
            dc.DrawEllipse(Gfx.B(PaulWoodShade), FighterInk.Pen(FighterInk.Detail), e, 6.8, 6.8);
            dc.DrawEllipse(null, Gfx.P(PaulWood, 1.5), e, 3.4, 3.4);
        }

        /// <summary>Standing: the handle upright behind his far shoulder, its top above the shoulder (behind the body).</summary>
        static void PaulHandleIdle(DrawingContext dc, Rig r)
        {
            var bottom = new Point(r.Neck.X - r.BodyW * 0.42, r.Hip.Y - r.TorsoLen * 0.1);
            var topP = new Point(bottom.X - 6, r.Head.Y + r.HeadR * 1.15);
            double ang = Math.Atan2(topP.Y - bottom.Y, topP.X - bottom.X) * 180 / Math.PI;
            double len = Math.Sqrt((topP.X - bottom.X) * (topP.X - bottom.X) + (topP.Y - bottom.Y) * (topP.Y - bottom.Y));
            PaulHandle(dc, bottom, ang, len);
        }
    }
}
