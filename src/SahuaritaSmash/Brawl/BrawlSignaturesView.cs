// Classic Games - Sahuarita Smash!, the fighters' own attacks (Andrew, 2026-10-31: "up, down, left, right combos for each
// of the attacks like in super smash bros ... each unique per character type"). Every fighter starts with the ordinary
// moves (BrawlData.Normals); this gives each of its 13 attacks its own name, picture (Anim), effect (Extra, drawn by
// BrawlView.SigFront) and feel: a flurry, a shock, a launcher, a trip, a spike, a wider or heavier swing. The strong
// side attack keeps its power (the tests check every fighter wins a life with it at 60-200%), and every attack still
// winds up for 3+ frames. Names are our own words.
using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;

namespace ClassicGames
{

    public sealed partial class BrawlView
    {
        /// <summary>The signature attacks' effects, in front of the fighter (local coordinates, y up, facing +x).</summary>
        void SigFront(DrawingContext dc, BFighter f, Rig r, string extra, bool active, double t)
        {
            if (!active) return;
            Point tip = TipOf(f, r);
            int seed = f.Slot * 7 + f.MF;
            switch (extra)
            {
                case "petals":
                    for (int i = 0; i < 6; i++) { Point p = Off(tip, i * 60 + f.MF * 18, 14 + (i % 2) * 8); dc.DrawEllipse(Gfx.B(i % 2 == 0 ? 0xE0F9A8D4 : 0xE0F472B6), Outline(1.2), p, 6, 3.5); }
                    break;
                case "stars":
                    for (int i = 0; i < 3; i++) Star(dc, tip.X + Math.Cos(i * 2.1 + t * 6) * 16, tip.Y + Math.Sin(i * 2.1 + t * 6) * 16, 8, 3.5, t * 6 + i, 0xFFFDE047, 0xFFB45309);
                    break;
                case "chomp":
                    {
                        double open = 10 + 8 * Math.Abs(Math.Sin(f.MF * 0.6));
                        Point c = new Point(r.Head.X + r.HeadR * 1.4, r.Head.Y);
                        for (int i = 0; i < 5; i++)
                        {
                            Poly(dc, Gfx.B(0xFFFEF3C7), Outline(1.2), new Point(c.X + i * 6, c.Y + open), new Point(c.X + i * 6 + 3, c.Y + open - 8), new Point(c.X + i * 6 + 6, c.Y + open));
                            Poly(dc, Gfx.B(0xFFFEF3C7), Outline(1.2), new Point(c.X + i * 6, c.Y - open), new Point(c.X + i * 6 + 3, c.Y - open + 8), new Point(c.X + i * 6 + 6, c.Y - open));
                        }
                        break;
                    }
                case "muzzle":
                    {
                        double s = 12 + 6 * Math.Abs(Math.Sin(f.MF * 1.7));
                        Star(dc, tip.X + 10, tip.Y, s, s * 0.45, f.MF, 0xFFFEF08A, 0xFFF97316);
                        dc.DrawEllipse(Gfx.B(0x80FB923C), null, new Point(tip.X + 10, tip.Y), s * 1.6, s * 1.1);
                        break;
                    }
                case "boom":
                    {
                        double k = (f.MF % 12) / 12.0;
                        dc.DrawEllipse(null, Gfx.P(Gfx.Alpha(0xFFFB923C, 1 - k), 5), new Point(0, 6), 30 + 60 * k, 10 + 18 * k);
                        for (int i = 0; i < 6; i++) dc.DrawEllipse(Gfx.B(0xC0FDBA74), null, Off(new Point(0, 8), 20 + i * 28, 30 + 40 * k), 7 * (1 - k) + 2, 7 * (1 - k) + 2);
                        break;
                    }
                case "drillfx":
                    {
                        double ang = f.St == BS.Attack ? r.FArmAng : 0;
                        Point a = r.FHand, b = Off(r.FHand, ang, 30);
                        Poly(dc, Gfx.B(0xFF94A3B8), Outline(2), Off(a, ang + 90, 8), b, Off(a, ang - 90, 8));
                        for (int i = 0; i < 3; i++) { double k = ((f.MF * 0.3 + i / 3.0) % 1.0); Point p = new Point(a.X + (b.X - a.X) * k, a.Y + (b.Y - a.Y) * k); dc.DrawLine(Gfx.P(0xFF475569, 1.6), Off(p, ang + 90, 8 * (1 - k)), Off(p, ang - 90, 8 * (1 - k))); }
                        for (int i = 0; i < 3; i++) dc.DrawEllipse(Gfx.B(0xFFFDE68A), null, Off(b, f.MF * 40 + i * 120, 8), 2, 2);
                        break;
                    }
                case "ore":
                    {
                        Point p = new Point(tip.X, tip.Y + 10 + f.MF * 2);
                        dc.DrawEllipse(Gfx.B(0xFFB87333), Outline(2), p, 9, 8);
                        dc.DrawEllipse(Gfx.B(0xFF22D3EE), null, new Point(p.X + 3, p.Y + 2), 3, 2.5);
                        break;
                    }
                case "feathers":
                    for (int i = 0; i < 4; i++)
                    {
                        Point p = Off(tip, 100 + i * 50 + f.MF * 6, 12 + i * 5);
                        dc.PushTransform(new RotateTransform(i * 40 + f.MF * 10, p.X, p.Y));
                        dc.DrawEllipse(Gfx.B(i % 2 == 0 ? 0xFF8B6B4A : 0xFF3B5BDB), Outline(1), p, 9, 3);
                        dc.Pop();
                    }
                    break;
                case "needles":
                    for (int i = 0; i < 6; i++)
                    {
                        double a = -40 + i * 16;
                        Point s = Off(tip, a, 6), e = Off(tip, a, 22 + (f.MF % 4) * 3);
                        dc.DrawLine(Gfx.P(0xFFF5F5DC, 2.2), s, e);
                    }
                    break;
                case "bully":
                    {
                        // Teddy's "Bully!": a little word bubble over his head
                        Point c = new Point(r.Head.X + 10, r.Head.Y + r.HeadR * 2.4);
                        dc.PushTransform(new ScaleTransform(f.Face, -1, c.X, c.Y));   // the puppet is drawn mirrored and upside down: undo both for the text
                        dc.DrawRoundedRectangle(Gfx.B(0xF0FFFFFF), Outline(2), new Rect(c.X - 30, c.Y - 12, 60, 24), 10, 10);
                        Gfx.Text(dc, "Bully!", c.X, c.Y - 9, 14, 0xFF7C2D12, 0.5, Gfx.UiBold);
                        dc.Pop();
                        break;
                    }
                case "lasso":
                    {
                        Point c = new Point(r.FHand.X, r.FHand.Y + 30);
                        dc.DrawEllipse(null, Gfx.P(0xFFD6B98C, 3), c, 46, 14);
                        dc.DrawLine(Gfx.P(0xFFD6B98C, 3), r.FHand, Off(c, 200 + f.MF * 30, 46));
                        break;
                    }
                case "pages":
                    for (int i = 0; i < 4; i++)
                    {
                        Point p = Off(new Point(0, 50), f.MF * 12 + i * 90, 40);
                        dc.PushTransform(new RotateTransform(f.MF * 8 + i * 30, p.X, p.Y));
                        dc.DrawRectangle(Gfx.B(0xFFFEF9C3), Outline(1.2), new Rect(p.X - 7, p.Y - 9, 14, 18));
                        dc.DrawLine(Gfx.P(0xFF9CA3AF, 1), new Point(p.X - 4, p.Y + 3), new Point(p.X + 4, p.Y + 3));
                        dc.Pop();
                    }
                    break;
                case "bits":
                    {
                        // little glowing squares of data streaming off the blade
                        for (int i = 0; i < 6; i++)
                        {
                            double k = ((f.MF * 0.12 + i / 6.0) % 1.0);
                            Point p = new Point(tip.X - 40 * k + Math.Sin(i * 3.1) * 8, tip.Y + Math.Cos(i * 2.3) * 14 + 10 * k);
                            dc.DrawRectangle(Gfx.B(Gfx.Alpha(i % 2 == 0 ? 0xFF22D3EE : 0xFF4ADE80, 1 - k)), null, new Rect(p.X - 3, p.Y - 3, 6, 6));
                        }
                        break;
                    }
                case "fire":
                    for (int i = 0; i < 5; i++)
                    {
                        double h = 18 + 12 * Math.Abs(Math.Sin(t * 20 + i * 1.7));
                        Point b = new Point(tip.X - 20 + i * 10, tip.Y - 8);
                        Poly(dc, Gfx.B(i % 2 == 0 ? 0xD0F97316 : 0xD0FDE047), null, new Point(b.X - 6, b.Y), new Point(b.X, b.Y + h), new Point(b.X + 6, b.Y));
                    }
                    break;
                case "crack": case "gust": PecosSig(dc, f, r, extra, tip, t); break;
                case "chips":
                    for (int i = 0; i < 5; i++)
                    {
                        Point p = Off(tip, 60 + i * 30, 8 + ((f.MF + i * 3) % 10) * 3);
                        dc.PushTransform(new RotateTransform(i * 50 + f.MF * 15, p.X, p.Y));
                        dc.DrawRectangle(Gfx.B(i % 2 == 0 ? 0xFFD6A35C : 0xFFA16207), Outline(1), new Rect(p.X - 5, p.Y - 2.5, 10, 5));
                        dc.Pop();
                    }
                    break;
            }
        }
    }
}
