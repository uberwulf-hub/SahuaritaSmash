// Classic Games - Sahuarita Smash!, part 4: the pictures, all drawn in code. Each fighter is a puppet: a body, a head and
// four limbs (two bones each) posed from the state it is in and the move it is doing (BPose, BrawlArt.Pose), then
// dressed by its own costume code (heads, hats, tendrils, wings, a cactus's arms, sticks and props). Also the five
// stages with their backgrounds and surprises, the camera that keeps everyone in view, the effects (sparks, dust,
// shockwaves, the bright burst when someone flies off) and the damage panels along the bottom. Nothing here changes
// the match: it only reads BrawlSim.
using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace ClassicGames
{
    public struct BPose
    {
        public double Lean, Crouch, FA1, FA2, BA1, BA2, FL1, FL2, BL1, BL2, Spin, Head, Prop;
        /// <summary>Like Lerp, but t may go past 1 (an overshoot).</summary>
        public static BPose LerpFree(BPose a, BPose b, double t) { return Mix(a, b, t); }
        public static BPose Lerp(BPose a, BPose b, double t) { return Mix(a, b, Math.Max(0, Math.Min(1, t))); }
        static BPose Mix(BPose a, BPose b, double t)
        {
            Func<double, double, double> l = (x, y) => x + (y - x) * t;
            return new BPose
            {
                Lean = l(a.Lean, b.Lean), Crouch = l(a.Crouch, b.Crouch), FA1 = l(a.FA1, b.FA1), FA2 = l(a.FA2, b.FA2), BA1 = l(a.BA1, b.BA1), BA2 = l(a.BA2, b.BA2),
                FL1 = l(a.FL1, b.FL1), FL2 = l(a.FL2, b.FL2), BL1 = l(a.BL1, b.BL1), BL2 = l(a.BL2, b.BL2), Spin = l(a.Spin, b.Spin), Head = l(a.Head, b.Head), Prop = l(a.Prop, b.Prop)
            };
        }
    }

    public sealed class BFx
    {
        public string K; public double X, Y, Vx, Vy, Life, Age, Size; public uint Color; public double Angle;
    }

    public sealed partial class BrawlView
    {
        public double CamX, CamY = 120, Zoom = 0.8;
        public double W = 1280, H = 720;
        public readonly List<BFx> Fx = new List<BFx>();
        public double Shake;
        public readonly double[] Pop = new double[8];     // the damage number's jolt after a hit
        readonly Random fxRng = new Random(5);
        public static readonly uint[] SlotColors = { 0xFFEF4444, 0xFF3B82F6, 0xFFFACC15, 0xFF22C55E };
        public string[] Tags = { "P1", "P2", "P3", "P4" };

        // ================================================================ camera
        public void Follow(BrawlSim sim, bool snap)
        {
            double minX = double.MaxValue, maxX = double.MinValue, minY = double.MaxValue, maxY = double.MinValue;
            int n = 0;
            foreach (BFighter f in sim.F)
            {
                if (!f.Playing) continue;
                double x = Math.Max(sim.Stage.BlastL + 150, Math.Min(sim.Stage.BlastR - 150, f.X));
                double y = Math.Max(sim.Stage.BlastB + 150, Math.Min(sim.Stage.BlastT - 120, f.Y));
                minX = Math.Min(minX, x); maxX = Math.Max(maxX, x); minY = Math.Min(minY, y); maxY = Math.Max(maxY, y + f.H);
                n++;
            }
            BPlat m = sim.Main;
            if (n == 0) { minX = m.L; maxX = m.R; minY = m.Top - 60; maxY = m.Top + 200; }
            minX = Math.Min(minX, m.L * 0.55); maxX = Math.Max(maxX, m.R * 0.55); minY = Math.Min(minY, m.Top - 40); maxY = Math.Max(maxY, m.Top + 160);
            double cx = (minX + maxX) / 2, cy = (minY + maxY) / 2;
            double z = Math.Min(W / (maxX - minX + 360), (H - 130) / (maxY - minY + 250));
            z = Math.Max(0.48, Math.Min(1.1, z));
            double k = snap ? 1 : 0.08;
            CamX += (cx - CamX) * k; CamY += (cy - CamY) * k; Zoom += (z - Zoom) * k;
        }

        public double SX(double x) { return W / 2 + (x - CamX) * Zoom; }
        public double SY(double y) { return (H - 120) * 0.56 - (y - CamY) * Zoom; }   // the damage panels take the bottom

        // ================================================================ effects (from the match's events)
        public void Take(BrawlSim sim)
        {
            foreach (BrEvent e in sim.Events)
            {
                switch (e.K)
                {
                    case "hit":
                        Burst(e.X, e.Y, e.Name == "zap" ? 0xFF7DD3FC : 0xFFFDE047, 6 + (int)Math.Min(14, e.A / 12), 2 + e.A / 40);
                        Add("ring", e.X, e.Y, 0, 0, 0.22, 20 + e.A * 0.3, e.Name == "zap" ? 0xFF7DD3FC : 0xFFFFFFFF);
                        if (e.S >= 0 && e.S < 8) Pop[e.S] = 1;
                        if (e.A > 120) Shake = Math.Max(Shake, Math.Min(14, e.A / 18));
                        break;
                    case "shield": Add("ring", e.X, e.Y, 0, 0, 0.18, 30, 0xFF93C5FD); break;
                    case "land": for (int i = 0; i < 4 + (int)e.A * 3; i++) Dust(e.X, e.Y, 1 + e.A); break;
                    case "djump": for (int i = 0; i < 5; i++) Add("puff", e.X + (fxRng.NextDouble() - 0.5) * 30, e.Y, (fxRng.NextDouble() - 0.5) * 2, -1 - fxRng.NextDouble(), 0.35, 10, 0xC0FFFFFF); break;
                    case "quake": Add("quake", e.X, e.Y, 0, 0, 0.35, e.A, 0xFFFDE68A); for (int i = 0; i < 10; i++) Dust(e.X + (fxRng.NextDouble() - 0.5) * e.A * 1.6, e.Y, 2); Shake = Math.Max(Shake, 6); break;
                    case "boom": Add("boom", e.X, e.Y, 0, 0, 0.45, e.A, 0xFFFB923C); Burst(e.X, e.Y, 0xFFFDBA74, 12, 4); Shake = Math.Max(Shake, 7); break;
                    case "ko": Add(e.Name == "top" ? "twinkle" : "blast", e.X, e.Y, 0, 0, e.Name == "top" ? 1.2 : 0.9, 1, e.S >= 0 && e.S < 4 ? SlotColors[e.S] : 0xFFFFFFFF); Shake = Math.Max(Shake, e.Name == "top" ? 3 : 16); break;
                    case "break": Burst(e.X, e.Y, 0xFF93C5FD, 16, 5); Add("ring", e.X, e.Y, 0, 0, 0.4, 90, 0xFF93C5FD); break;
                    case "counter": Add("ring", e.X, e.Y, 0, 0, 0.3, 70, 0xFFFDE047); Burst(e.X, e.Y, 0xFFFDE047, 10, 4); break;
                    case "reflect": Add("ring", e.X, e.Y, 0, 0, 0.25, 40, 0xFFA5F3FC); break;
                    case "poof": Add("puff", e.X, e.Y, 0, 0.5, 0.3, 14, 0xC0E5E7EB); break;
                    case "respawn": Add("ring", e.X, e.Y + 40, 0, 0, 0.5, 80, 0xFFFFFFFF); break;
                    case "ledge": Add("ring", e.X, e.Y, 0, 0, 0.2, 18, 0xFFFFFFFF); break;
                    case "combo": Add("combo", e.X, e.Y + 40, 0, 1.4, 0.7, e.A + 1, 0xFFFDE047); break;
                }
            }
        }

        void Add(string k, double x, double y, double vx, double vy, double life, double size, uint color)
        {
            if (Fx.Count > 400) return;
            Fx.Add(new BFx { K = k, X = x, Y = y, Vx = vx, Vy = vy, Life = life, Size = size, Color = color, Angle = fxRng.NextDouble() * 6.28 });
        }
        void Burst(double x, double y, uint c, int n, double speed)
        {
            for (int i = 0; i < n; i++)
            {
                double a = fxRng.NextDouble() * Math.PI * 2, v = speed * (0.5 + fxRng.NextDouble());
                Add("spark", x, y, Math.Cos(a) * v, Math.Sin(a) * v, 0.25 + fxRng.NextDouble() * 0.2, 4 + fxRng.NextDouble() * 4, c);
            }
        }
        void Dust(double x, double y, double k)
        {
            Add("puff", x + (fxRng.NextDouble() - 0.5) * 30, y + 4, (fxRng.NextDouble() - 0.5) * 3 * k, 0.5 + fxRng.NextDouble(), 0.4, 8 + 6 * k, 0xB0D6C7A8);
        }

        public void StepFx(BrawlSim sim, double dt)
        {
            for (int i = Fx.Count - 1; i >= 0; i--)
            {
                BFx p = Fx[i];
                p.Age += dt;
                p.X += p.Vx * dt * 60; p.Y += p.Vy * dt * 60;
                if (p.K == "spark") p.Vy -= 0.15 * dt * 60;
                if (p.Age >= p.Life) Fx.RemoveAt(i);
            }
            // smoke behind anyone flying hard
            foreach (BFighter f in sim.F)
            {
                if (f.St != BS.Hurt || f.Hitlag > 0) continue;
                double v = Math.Sqrt(f.Lx * f.Lx + f.Ly * f.Ly);
                if (v > 9 && fxRng.NextDouble() < 0.7) Add("trail", f.X, f.Y + f.H * 0.5, 0, 0, 0.5, 10 + v * 0.6, v > 20 ? 0xD0FDE68A : 0xB0E5E7EB);
            }
            for (int s = 0; s < Pop.Length; s++) Pop[s] = Math.Max(0, Pop[s] - dt * 4);
            Shake = Math.Max(0, Shake - dt * 40);
        }

        // ================================================================ the whole picture
        public void Draw(DrawingContext dc, BrawlSim sim, double t)
        {
            double sh = Shake > 0 ? Shake : 0;
            double ox = sh > 0 ? (fxRng.NextDouble() - 0.5) * sh : 0, oy = sh > 0 ? (fxRng.NextDouble() - 0.5) * sh : 0;
            Rim = RimFor(sim.Stage.Id);
            DrawBackground(dc, sim.Stage, t);
            dc.PushTransform(new TranslateTransform(ox, oy));
            DrawHazardBack(dc, sim, t);
            DrawPlatforms(dc, sim, t);
            foreach (BShot s in sim.Shots) if (s.On && s.D.Wall) DrawShot(dc, s, sim, t);
            // fighters: the one being thrown above the thrower; those flashing invulnerable a little see-through
            foreach (BFighter f in sim.F) if (f.Playing) DrawFighter(dc, f, sim, t);
            foreach (BShot s in sim.Shots) if (s.On && !s.D.Wall) DrawShot(dc, s, sim, t);
            DrawHazardFront(dc, sim, t);
            DrawFx(dc, t);
            dc.Pop();
            DrawGrade(dc, sim.Stage);
            dc.PushTransform(new TranslateTransform(ox, oy));
            foreach (BFighter f in sim.F) if (f.Playing) DrawTag(dc, f, t);
            dc.Pop();
            DrawOffscreen(dc, sim, t);
        }


        void DrawHazardBack(DrawingContext dc, BrawlSim sim, double t)
        {
            if (sim.HazPhase == 0) return;
            bool warn = sim.HazPhase == 1;
            double blink = Math.Sin(t * 18) > 0 ? 1 : 0.4;
            switch (sim.Hazard)
            {
                case "fans":
                    foreach (int side in new[] { -1, 1 })
                    {
                        double x1 = SX(side < 0 ? -270 : 140), x2 = SX(side < 0 ? -140 : 270), y = SY(sim.Main.Top);
                        dc.DrawRectangle(Gfx.B(warn ? Gfx.Alpha(0xFF22D3EE, 0.35 * blink) : 0x5522D3EE), null, new Rect(x1, warn ? y - 10 : y - 420 * Zoom, x2 - x1, warn ? 10 : 420 * Zoom));
                        if (!warn) for (int i = 0; i < 6; i++) { double yy = y - ((t * 300 + i * 70) % 420) * Zoom; dc.DrawLine(Gfx.P(0x80E0F2FE, 2), new Point(x1 + (i + 0.5) * (x2 - x1) / 6, yy), new Point(x1 + (i + 0.5) * (x2 - x1) / 6, yy - 30 * Zoom)); }
                    }
                    break;
                case "laser":
                    {
                        double y = SY(sim.HazY);
                        if (warn) for (double x = 0; x < W; x += 30) dc.DrawLine(Gfx.P(Gfx.Alpha(0xFFEF4444, 0.7 * blink), 2), new Point(x, y), new Point(x + 14, y));
                        break;
                    }
                case "meteor":
                    {
                        double gx = SX(sim.HazX), gy = SY(sim.Main.Top);
                        double k = warn ? 1 - sim.HazT / (double)BrawlSim.HazWarn : 1;
                        dc.DrawEllipse(Gfx.B(Gfx.Alpha(0xFF000000, 0.25 + 0.35 * k)), null, new Point(gx, gy + 2), (30 + 30 * k) * Zoom, 8 * Zoom);
                        break;
                    }
                case "cart":
                    {
                        double y = SY(sim.Main.Top);
                        if (warn)
                        {
                            double x = sim.HazDir > 0 ? 40 : W - 40;
                            dc.DrawEllipse(Gfx.B(Gfx.Alpha(0xFFFACC15, blink)), Gfx.P(0xFF000000, 2), new Point(x, y - 50), 22, 22);
                            Gfx.Text(dc, "!", x, y - 66, 26, 0xFF000000, 0.5, Gfx.UiBold);
                        }
                        break;
                    }
                case "javelina":
                    if (warn)
                    {
                        // a cloud of dust and a warning sign on the side the herd comes from
                        double edge = sim.HazDir > 0 ? sim.Main.L : sim.Main.R;
                        double k = 1 - sim.HazT / (double)BrawlSim.HazWarn;
                        DrawDust(dc, SX(edge - sim.HazDir * 30), SY(sim.Main.Top), (0.5 + 0.8 * k) * Zoom, t, 0.35 + 0.4 * k);
                        double x = sim.HazDir > 0 ? 40 : W - 40, y = SY(sim.Main.Top);
                        dc.DrawEllipse(Gfx.B(Gfx.Alpha(0xFFFACC15, blink)), Gfx.P(0xFF000000, 2), new Point(x, y - 50), 22, 22);
                        Gfx.Text(dc, "!", x, y - 66, 26, 0xFF000000, 0.5, Gfx.UiBold);
                    }
                    break;
                case "wind":
                    if (warn) for (int i = 0; i < 8; i++)
                    {
                        double x = sim.HazDir > 0 ? (t * 400 + i * 160) % W : W - (t * 400 + i * 160) % W;
                        dc.DrawLine(Gfx.P(Gfx.Alpha(0xFFFDE68A, 0.5), 3), new Point(x, H * 0.2 + i * 50), new Point(x - sim.HazDir * 40, H * 0.2 + i * 50));
                    }
                    break;
            }
        }

        void DrawHazardFront(DrawingContext dc, BrawlSim sim, double t)
        {
            if (sim.HazPhase != 2) return;
            switch (sim.Hazard)
            {
                case "wind":
                    for (int i = 0; i < 18; i++)
                    {
                        double x = sim.HazDir > 0 ? (t * 900 + i * 97) % (W + 100) - 50 : W + 50 - (t * 900 + i * 97) % (W + 100);
                        double y = (i * 53) % (int)H;
                        dc.DrawLine(Gfx.P(0x90FEF3C7, 3), new Point(x, y), new Point(x - sim.HazDir * 80, y + 6));
                    }
                    break;
                case "laser":
                    {
                        double y = SY(sim.HazY), x = SX(sim.HazX);
                        dc.DrawLine(Gfx.P(0x60EF4444, 18 * Zoom), new Point(0, y), new Point(W, y));
                        dc.DrawLine(Gfx.P(0xFFFCA5A5, 4 * Zoom), new Point(0, y), new Point(W, y));
                        dc.DrawEllipse(Gfx.B(0xFFFEE2E2), null, new Point(x, y), 22 * Zoom, 22 * Zoom);
                        break;
                    }
                case "cart":
                    {
                        double x = SX(sim.HazX), y = SY(sim.Main.Top);
                        double z = Zoom;
                        dc.DrawRoundedRectangle(Gfx.B(0xFF57534E), Gfx.P(0xFF1C1917, 3), new Rect(x - 46 * z, y - 52 * z, 92 * z, 40 * z), 6, 6);
                        dc.DrawRectangle(Gfx.B(0xFFB87333), null, new Rect(x - 40 * z, y - 62 * z, 80 * z, 14 * z));
                        foreach (int side in new[] { -1, 1 }) { dc.DrawEllipse(Gfx.B(0xFF1C1917), null, new Point(x + side * 28 * z, y - 10 * z), 11 * z, 11 * z); dc.DrawEllipse(Gfx.B(0xFF78716C), null, new Point(x + side * 28 * z, y - 10 * z), 4 * z, 4 * z); }
                        break;
                    }
                case "javelina":
                    for (int k = BrawlSim.HerdBack.Length - 1; k >= 0; k--)
                    {
                        double mx = sim.HazX - sim.HazDir * BrawlSim.HerdBack[k];
                        // they run up out of the dust on one side and leap off the far side
                        double before = sim.HazDir > 0 ? sim.Main.L - mx : mx - sim.Main.R, after = sim.HazDir > 0 ? mx - sim.Main.R : sim.Main.L - mx;
                        double fade = 1, lift = 0;
                        if (before > 0) fade = Math.Max(0, 1 - before / 110);
                        if (after > 0) { fade = Math.Max(0, 1 - after / 150); lift = after * 0.5 - after * after * 0.006; }
                        if (fade <= 0) continue;
                        double phase = t * 15 + k * 1.7;
                        DrawDust(dc, SX(mx - sim.HazDir * 30), SY(sim.Main.Top), 0.45 * Zoom, t + k, 0.3 * fade);
                        DrawJavelina(dc, SX(mx), SY(sim.Main.Top + lift), sim.HazDir, BrawlSim.HerdAdult[k], phase, fade);
                    }
                    break;
                case "meteor":
                    {
                        double x = SX(sim.HazX), y = SY(sim.HazY);
                        dc.DrawLine(Gfx.P(0x80FDBA74, 16 * Zoom), new Point(x, y), new Point(x - 30 * Zoom, y - 120 * Zoom));
                        dc.DrawEllipse(Gfx.B(0xFF78716C), Gfx.P(0xFF292524, 3), new Point(x, y), 22 * Zoom, 20 * Zoom);
                        break;
                    }
            }
        }

        /// <summary>Puffs of desert dust rising from (x, y), the ground.</summary>
        static void DrawDust(DrawingContext dc, double x, double y, double scale, double t, double alpha)
        {
            for (int i = 0; i < 6; i++)
            {
                double a = (t * 1.3 + i / 6.0) % 1;
                double px = x + (i - 2.5) * 14 * scale + Math.Sin(t * 2 + i) * 6 * scale, py = y - 8 * scale - a * 34 * scale;
                dc.DrawEllipse(Gfx.B(Gfx.Alpha(0xFFD9B38C, alpha * (1 - a))), null, new Point(px, py), (10 + a * 18) * scale, (8 + a * 14) * scale);
            }
        }

        /// <summary>A javelina (collared peccary) galloping, feet at (x, y), facing d: grown-ups grizzled grey with a pale
        /// collar and a bristly mane, babies reddish tan with a dark stripe down the back.</summary>
        void DrawJavelina(DrawingContext dc, double x, double y, int d, bool adult, double phase, double alpha)
        {
            double s = (adult ? 1.0 : 0.6) * Zoom;
            dc.PushOpacity(alpha);
            uint coat = adult ? 0xFF4A4440 : 0xFFA0623A, dark = adult ? 0xFF292524 : 0xFF6B3A1E;
            Pen line = Gfx.P(0xFF1C1917, Math.Max(1, 2 * s));
            double bob = Math.Abs(Math.Sin(phase)) * 3 * s;
            y -= bob;
            // legs: the far pair first, galloping opposite to the near pair
            for (int pass = 0; pass < 2; pass++)
            {
                Pen leg = Gfx.P(pass == 0 ? dark : coat, 4.5 * s);
                foreach (double at in new[] { -18.0, 16.0 })
                {
                    double swing = Math.Sin(phase + (at > 0 ? 0 : Math.PI) + pass * 0.8) * 9 * s;
                    Point hip = new Point(x + d * at * s, y - 24 * s), foot = new Point(x + d * at * s + d * swing, y + bob - 2 * s);
                    dc.DrawLine(leg, hip, foot);
                    dc.DrawEllipse(Gfx.B(0xFF1C1917), null, foot, 3 * s, 2.2 * s);
                }
            }
            // body
            Point mid = new Point(x - d * 3 * s, y - 32 * s);
            dc.DrawEllipse(BrawlView.Sphere(coat), line, mid, 34 * s, 17 * s);
            if (!adult) dc.DrawLine(Gfx.P(dark, 3.5 * s), new Point(mid.X - d * 26 * s, mid.Y - 9 * s), new Point(mid.X + d * 22 * s, mid.Y - 13 * s));
            // the bristly mane along the back
            for (int i = 0; i < 9; i++)
            {
                double bx = mid.X + d * (-22 + i * 5.5) * s, by = mid.Y - (14 + Math.Sin(i * 0.4) * 2) * s;
                dc.DrawLine(Gfx.P(dark, 2 * s), new Point(bx, by), new Point(bx - d * 3 * s, by - (adult ? 8 : 4) * s));
            }
            // the pale collar round the shoulders (grown-ups)
            if (adult) dc.DrawLine(Gfx.P(0xFFD6D3D1, 5 * s), new Point(x + d * 15 * s, y - 46 * s), new Point(x + d * 21 * s, y - 20 * s));
            // the head: a wedge ending in a round snout, a little ear and a shiny eye
            double hy = y - 32 * s + Math.Sin(phase * 2) * 1.5 * s;
            Poly(dc, BrawlView.Sphere(coat), line,
                new Point(x + d * 22 * s, hy - 12 * s), new Point(x + d * 40 * s, hy - 7 * s), new Point(x + d * 52 * s, hy + 1 * s),
                new Point(x + d * 50 * s, hy + 7 * s), new Point(x + d * 22 * s, hy + 12 * s));
            dc.DrawEllipse(Gfx.B(0xFF8A7A72), line, new Point(x + d * 52 * s, hy + 4 * s), 3 * s, 5 * s);
            dc.DrawEllipse(Gfx.B(0xFF1C1917), null, new Point(x + d * 53 * s, hy + 3 * s), 0.9 * s, 1.3 * s);
            Poly(dc, Gfx.B(dark), line, new Point(x + d * 24 * s, hy - 11 * s), new Point(x + d * 21 * s, hy - 21 * s), new Point(x + d * 31 * s, hy - 12 * s));
            dc.DrawEllipse(Gfx.B(0xFF0C0A09), null, new Point(x + d * 36 * s, hy - 3 * s), 2.6 * s, 2.6 * s);
            dc.DrawEllipse(Gfx.B(0xFFFFFFFF), null, new Point(x + d * 36.8 * s, hy - 3.8 * s), 0.9 * s, 0.9 * s);
            // a little tail
            dc.DrawLine(Gfx.P(dark, 2.5 * s), new Point(mid.X - d * 33 * s, mid.Y - 6 * s), new Point(mid.X - d * 39 * s, mid.Y - 2 * s + Math.Sin(phase * 2) * 3 * s));
            dc.Pop();
        }

        // ---------------------------------------------------------------- shots
        void DrawShot(DrawingContext dc, BShot s, BrawlSim sim, double t)
        {
            double x = SX(s.X), y = SY(s.Y), z = Zoom, r = s.D.R * z;
            uint owner = s.Owner >= 0 && s.Owner < 4 ? SlotColors[s.Owner] : 0xFFFFFFFF;
            bool waiting = s.Age <= s.D.Delay;
            switch (s.D.Look)
            {
                case "star": dc.DrawEllipse(Glow(0xA0FDE047), null, new Point(x, y), r * 3.2, r * 3.2); Star(dc, x, y, r * 1.3, r * 0.55, t * 8, 0xFFFDE047, 0xFFB45309); break;
                case "pellet": dc.DrawLine(Gfx.P(0x8067E8F9, r * 1.6), new Point(x - s.Vx * 2 * z, y), new Point(x, y)); dc.DrawEllipse(Gfx.B(0xFFE0FFFF), null, new Point(x, y), r, r * 0.7); break;
                case "grenade": dc.DrawEllipse(Gfx.B(0xFF4D5B3A), Gfx.P(0xFF1C1917, 2), new Point(x, y), r, r); dc.DrawEllipse(Gfx.B(Math.Sin(t * 20) > 0 ? 0xFFEF4444 : 0xFF7F1D1D), null, new Point(x, y - r * 0.8), 3, 3); break;
                case "rock": dc.DrawEllipse(Gfx.B(0xFFB87333), Gfx.P(0xFF5B3418, 2), new Point(x, y), r, r * 0.85); dc.DrawEllipse(Gfx.B(0xFF22D3EE), null, new Point(x + r * 0.3, y - r * 0.2), r * 0.3, r * 0.25); break;
                case "cloud":
                    for (int i = 0; i < 5; i++) { double a = t * 9 + i * 1.26; dc.DrawEllipse(Gfx.B(0x90E7D3B1), null, new Point(x + Math.Cos(a) * r * 0.6, y + Math.Sin(a) * r * 0.3 - i * r * 0.25), r * 0.7, r * 0.45); }
                    break;
                case "needle": dc.DrawLine(Gfx.P(0xFFF5F5DC, 3 * z), new Point(x - Math.Sign(s.Vx) * 14 * z, y + s.Vy * z), new Point(x + Math.Sign(s.Vx) * 10 * z, y - s.Vy * z * 0.5)); break;
                case "book":
                    dc.PushTransform(new RotateTransform(t * 600 % 360, x, y));
                    dc.DrawRectangle(Gfx.B(0xFF7C2D12), Gfx.P(0xFF3E1408, 2), new Rect(x - r, y - r * 0.75, r * 2, r * 1.5));
                    dc.DrawRectangle(Gfx.B(0xFFFEF3C7), null, new Rect(x - r * 0.8, y - r * 0.55, r * 1.6, r * 0.25));
                    dc.Pop(); break;
                case "packet":
                    dc.DrawRectangle(Gfx.B(0xFF0EA5E9), Gfx.P(0xFFE0F2FE, 2), new Rect(x - r, y - r, r * 2, r * 2));
                    Gfx.Text(dc, "01", x, y - r * 0.8, r * 1.1, 0xFFE0F2FE, 0.5, Gfx.Mono);
                    break;
                case "pancake":
                    dc.PushTransform(new ScaleTransform(1, 0.45, x, y));
                    dc.DrawEllipse(Gfx.B(0xFFD97706), Gfx.P(0xFF78350F, 3), new Point(x, y), r, r);
                    dc.DrawEllipse(Gfx.B(0xFFFDE68A), null, new Point(x, y), r * 0.35, r * 0.35);
                    dc.Pop(); break;
                case "log":
                    dc.PushTransform(new RotateTransform(-s.X * 2 % 360, x, y));
                    dc.DrawEllipse(Gfx.B(0xFF92400E), Gfx.P(0xFF451A03, 3), new Point(x, y), r, r);
                    dc.DrawEllipse(null, Gfx.P(0xFFD97706, 2), new Point(x, y), r * 0.6, r * 0.6);
                    dc.DrawEllipse(null, Gfx.P(0xFFD97706, 2), new Point(x, y), r * 0.25, r * 0.25);
                    dc.Pop(); break;
                case "bolt":
                    {
                        double top = y - 320 * z;
                        dc.DrawEllipse(Gfx.B(0xFF64748B), null, new Point(x, top), 50 * z, 22 * z);
                        dc.DrawEllipse(Gfx.B(0xFF94A3B8), null, new Point(x - 24 * z, top + 4), 30 * z, 16 * z);
                        if (waiting) { if (Math.Sin(t * 30) > 0) dc.DrawLine(Gfx.P(0x80FDE047, 2), new Point(x, top + 20 * z), new Point(x, y)); break; }
                        Pen bp = Gfx.P(0xFFFEF9C3, 6 * z), gp = Gfx.P(0x80FDE047, 20 * z);
                        double px = x, py = top + 16 * z;
                        for (int i = 1; i <= 6; i++)
                        {
                            double nx = x + ((i * 37 + (int)(t * 30)) % 40 - 20) * z, ny = top + 16 * z + (y - top - 16 * z) * i / 6;
                            if (i == 6) nx = x;
                            dc.DrawLine(gp, new Point(px, py), new Point(nx, ny)); dc.DrawLine(bp, new Point(px, py), new Point(nx, ny));
                            px = nx; py = ny;
                        }
                        break;
                    }
                case "wall":
                    {
                        double h = 116 * z, w = s.D.R * 1.6 * z, bottom = y + 46 * z;
                        dc.DrawRectangle(Gfx.B(0x40F97316), null, new Rect(x - w / 2, bottom - h, w, h));
                        for (int i = 0; i < 5; i++)
                        {
                            double fx = x - w / 2 + w * (i + 0.5) / 5, fh = h * (0.6 + 0.4 * Math.Abs(Math.Sin(t * 7 + i * 1.7)));
                            Poly(dc, Gfx.B(0xC0F97316), null, new Point(fx - w / 10, bottom), new Point(fx, bottom - fh), new Point(fx + w / 10, bottom));
                            Poly(dc, Gfx.B(0xE0FDE047), null, new Point(fx - w / 22, bottom), new Point(fx, bottom - fh * 0.55), new Point(fx + w / 22, bottom));
                        }
                        // bricks of light: it's a firewall
                        for (int r2 = 0; r2 < 4; r2++) dc.DrawLine(Gfx.P(0x60FFFFFF, 1.5), new Point(x - w / 2, bottom - h * (r2 + 1) / 5), new Point(x + w / 2, bottom - h * (r2 + 1) / 5));
                        break;
                    }
                default: dc.DrawEllipse(Gfx.B(owner), null, new Point(x, y), r, r); break;
            }
        }

        static void Star(DrawingContext dc, double x, double y, double r1, double r2, double a, uint fill, uint line)
        {
            var pts = new Point[10];
            for (int i = 0; i < 10; i++) { double rr = i % 2 == 0 ? r1 : r2, an = a + i * Math.PI / 5 - Math.PI / 2; pts[i] = new Point(x + Math.Cos(an) * rr, y + Math.Sin(an) * rr); }
            Poly(dc, Gfx.B(fill), Gfx.P(line, 2), pts);
        }

        // ---------------------------------------------------------------- effects
        void DrawFx(DrawingContext dc, double t)
        {
            foreach (BFx p in Fx)
            {
                double k = p.Age / p.Life, x = SX(p.X), y = SY(p.Y), z = Zoom;
                double a = 1 - k;
                switch (p.K)
                {
                    case "spark":
                        dc.DrawEllipse(Glow(Gfx.Alpha(p.Color, a * 0.8)), null, new Point(x, y), p.Size * z * 3, p.Size * z * 3);
                        dc.DrawEllipse(Gfx.B(Gfx.Alpha(Gfx.Mix(p.Color, 0xFFFFFFFF, 0.5), a)), null, new Point(x, y), p.Size * z * (1 - k * 0.5), p.Size * z * (1 - k * 0.5));
                        break;
                    case "combo": Gfx.Text(dc, (int)p.Size + " hits!", x, y - 10 * z, Math.Max(12, 20 * z), Gfx.Alpha(p.Color, a), 0.5, Gfx.UiBold); break;
                    case "ring": dc.DrawEllipse(null, Gfx.P(Gfx.Alpha(p.Color, a), 4 * a + 1), new Point(x, y), p.Size * z * (0.4 + k), p.Size * z * (0.4 + k)); break;
                    case "puff": case "trail": dc.DrawEllipse(Gfx.B(Gfx.Alpha(p.Color, a * ((p.Color >> 24) / 255.0))), null, new Point(x, y), p.Size * z * (0.6 + k * 0.6), p.Size * z * (0.6 + k * 0.6)); break;
                    case "quake": dc.DrawEllipse(null, Gfx.P(Gfx.Alpha(p.Color, a), 6 * a + 1), new Point(x, y), p.Size * z * (0.3 + k), p.Size * z * 0.25 * (0.3 + k)); break;
                    case "boom":
                        dc.DrawEllipse(Glow(Gfx.Alpha(0xFFFDBA74, a)), null, new Point(x, y), p.Size * z * 2.4, p.Size * z * 2.4);
                        dc.DrawEllipse(Gfx.B(Gfx.Alpha(0xFFFDE68A, a)), null, new Point(x, y), p.Size * z * (0.4 + k * 0.8), p.Size * z * (0.4 + k * 0.8));
                        dc.DrawEllipse(Gfx.B(Gfx.Alpha(0xFFF97316, a * 0.8)), null, new Point(x, y), p.Size * z * (0.3 + k * 0.6), p.Size * z * (0.3 + k * 0.6));
                        break;
                    case "blast":
                        {
                            // a bright fan of light from the edge someone flew off
                            double cx = Math.Max(0, Math.Min(W, x)), cy = Math.Max(0, Math.Min(H, y));
                            double ang = Math.Atan2(H / 2 - cy, W / 2 - cx);
                            for (int i = -3; i <= 3; i++)
                            {
                                double an = ang + i * 0.16, len = (420 + 120 * Math.Abs(Math.Sin(i * 1.7))) * (0.5 + k);
                                Poly(dc, Gfx.B(Gfx.Alpha(i == 0 ? 0xFFFFFFFF : p.Color, a * 0.85)), null, new Point(cx, cy),
                                    new Point(cx + Math.Cos(an - 0.06) * len, cy + Math.Sin(an - 0.06) * len), new Point(cx + Math.Cos(an + 0.06) * len, cy + Math.Sin(an + 0.06) * len));
                            }
                            dc.DrawEllipse(Gfx.B(Gfx.Alpha(0xFFFFFFFF, a)), null, new Point(cx, cy), 60 * (1 - k * 0.5), 60 * (1 - k * 0.5));
                            break;
                        }
                    case "twinkle":
                        {
                            double cx = Math.Max(30, Math.Min(W - 30, x)), cy = 40;
                            Star(dc, cx, cy, 16 * (1 - Math.Abs(k - 0.5)), 6, t * 6, Gfx.Alpha(0xFFFFFFFF, a + 0.2), Gfx.Alpha(p.Color, a));
                            break;
                        }
                }
            }
        }

        void DrawTag(DrawingContext dc, BFighter f, double t)
        {
            if (f.Slot >= 4) return;
            double x = SX(f.X), y = SY(f.Y + f.H) - 16;
            uint c = SlotColors[f.Slot];
            Poly(dc, Gfx.B(c), null, new Point(x - 7, y - 12), new Point(x + 7, y - 12), new Point(x, y - 2));
            Gfx.Text(dc, Tags[f.Slot], x, y - 34, 15, c, 0.5, Gfx.UiBold);
        }

        void DrawOffscreen(DrawingContext dc, BrawlSim sim, double t)
        {
            foreach (BFighter f in sim.F)
            {
                if (!f.Playing || f.Slot >= 4) continue;
                double x = SX(f.X), y = SY(f.Y + f.H / 2);
                if (x > -10 && x < W + 10 && y > -10 && y < H + 10) continue;
                double bx = Math.Max(46, Math.Min(W - 46, x)), by = Math.Max(46, Math.Min(H - 150, y));
                dc.DrawEllipse(Gfx.B(0xD0111827), Gfx.P(SlotColors[f.Slot], 3), new Point(bx, by), 38, 38);
                double zoom = 0.5 * 76 / f.H * 0.8;
                DrawPuppet(dc, f, bx, by + f.H * zoom * 0.5, zoom, t, 0);
            }
        }

        // ================================================================ the fighters

        /// <summary>What the picture remembers about each fighter between frames, so movement flows: the pose it showed
        /// (blended toward the next one instead of snapping), the run cycle's place, a flip on a double jump, squash and
        /// stretch, and the path of a strike (the swoosh). Pictures only: the match never reads it.</summary>
        sealed class AnimState
        {
            public BPose Pose; public bool Has; public double LastT, Stride, FlipT = -1, Squash = 1;
            public int LastJumps = -1; public bool Ground = true;
            public readonly Point[] Trail = new Point[9]; public int TrailN;
        }
        readonly AnimState[] anim = new AnimState[8];

        static double Wrap180(double a) { a %= 360; if (a > 180) a -= 360; if (a < -180) a += 360; return a; }

        BPose Animate(BFighter f, AnimState s, double t, double dt)
        {
            // the run cycle moves with the real ground speed, so feet don't slide
            if (f.Ground && (f.St == BS.Walk || f.St == BS.Run)) s.Stride += Math.Abs(f.Vx) * dt * 60 * (0.055 * 90 / f.D.Height) / f.Size;
            // a double jump is a front flip
            if (s.LastJumps >= 0 && f.Jumps < s.LastJumps && !f.Ground && f.St == BS.Air) s.FlipT = 0;
            s.LastJumps = f.Jumps;
            BPose target = Pose(f, t, s.Stride);
            if (s.FlipT >= 0)
            {
                s.FlipT += dt * 60;
                if (s.FlipT > 26 || f.St != BS.Air) s.FlipT = -1;
                else
                {
                    double k = s.FlipT / 26;
                    target = BPose.Lerp(target, P(0, 0.35, 60, 110, 120, 110, -20, -120, -60, -110), Math.Sin(k * Math.PI));
                    target.Spin = -Smooth(k) * 360;
                }
            }
            if (!s.Has) { s.Has = true; s.Pose = target; return target; }
            // how quickly it moves to the new pose: strikes and hits snap, everything else glides
            double k60 = 0.32;
            if (f.St == BS.Attack) k60 = 0.62;
            if (f.St == BS.Hurt || f.St == BS.Grabbed) k60 = 0.7;
            if (f.St == BS.Ledge || f.St == BS.Enter) k60 = 0.25;
            double kk = 1 - Math.Pow(1 - k60, Math.Max(0, dt) * 60);
            BPose prev = s.Pose;
            BPose p = BPose.Lerp(prev, target, kk);
            p.Spin = prev.Spin + Wrap180(target.Spin - prev.Spin) * Math.Max(kk, 0.6);
            if (Math.Abs(target.Spin) < 0.01 && Math.Abs(Wrap180(p.Spin)) < 2) p.Spin = 0;
            // the head trails the body a moment (follow-through)
            double headK = 1 - Math.Pow(1 - k60 * 0.5, Math.Max(0, dt) * 60);
            p.Head = prev.Head + (target.Head - prev.Head) * headK - (p.Lean - prev.Lean) * 0.6;
            s.Pose = p;
            return p;
        }

        void DrawFighter(DrawingContext dc, BFighter f, BrawlSim sim, double t)
        {
            AnimState s = anim[f.Slot % 8] ?? (anim[f.Slot % 8] = new AnimState());
            double dt = s.Has ? Math.Max(0, Math.Min(0.1, t - s.LastT)) : 0;
            s.LastT = t;
            double x = SX(f.X), y = SY(f.Y);
            if (f.Hitlag > 0 && f.St == BS.Hurt) x += Math.Sin(f.Hitlag * 2.5) * 4;
            // shadow on the floor below
            double floor = sim.FloorUnder(f.X, f.Y + 1);
            if (floor > -9000 && f.Y - floor < 500) { double k = 1 - (f.Y - floor) / 500; dc.DrawEllipse(Gfx.B(Gfx.Alpha(0xFF000000, 0.3 * k)), null, new Point(x, SY(floor) + 3), f.HalfW * Zoom * (0.6 + 0.4 * k), 6 * Zoom); }
            double flash = f.Invuln > 0 && (f.St == BS.Enter || f.Invuln > 20 && (int)(t * 12) % 2 == 0) ? 0.45 : 0;
            if (f.St == BS.Enter)
            {
                dc.DrawEllipse(Glow(Gfx.Alpha(SlotColors[f.Slot % 4], 0.5)), null, new Point(x, y + 2), 70 * Zoom, 22 * Zoom);
                dc.DrawEllipse(Gfx.B(0x80FFFFFF), Gfx.P(SlotColors[f.Slot % 4], 3), new Point(x, y + 2), 50 * Zoom, 9 * Zoom);
            }
            if (f.St == BS.Shield)
            {
                double r = (f.H * 0.55) * (0.35 + 0.65 * f.ShieldHp / 50) * Zoom;
                dc.DrawEllipse(Glow(Gfx.Alpha(SlotColors[f.Slot % 4], 0.45)), Gfx.P(Gfx.Alpha(0xFFFFFFFF, 0.7), 2), new Point(x, y - f.H * 0.5 * Zoom), r, r);
            }
            BPose pose = Animate(f, s, t, dt);
            // squash and stretch: stretched when moving fast up or down, squashed on landing and before a jump
            double target = 1;
            double vyAll = f.Vy + f.Ly;
            if (!f.Ground && f.St != BS.Hurt) target = 1 + Math.Min(0.12, Math.Abs(vyAll) * 0.009);
            if (f.St == BS.Squat) target = 0.86;
            if (f.Ground && !s.Ground) s.Squash = Math.Min(s.Squash, f.St == BS.Down ? 0.8 : 0.84);
            if (!f.Ground && s.Ground && vyAll > 4) s.Squash = 1.14;
            s.Ground = f.Ground;
            s.Squash += (target - s.Squash) * (1 - Math.Pow(0.72, dt * 60));
            // quick movement leaves two fading copies behind (flying off, a dash, a zoom)
            double vx = f.Vx + f.Lx, vy = vyAll, sp = Math.Sqrt(vx * vx + vy * vy);
            Point tip;
            if (sp > 11 && (f.St == BS.Hurt || f.St == BS.Attack))
                for (int g = 2; g >= 1; g--)
                {
                    dc.PushOpacity(0.16 * (3 - g));
                    DrawPuppet(dc, f, x - vx * g * 2.2 * Zoom, y + vy * g * 2.2 * Zoom, Zoom * f.Size, t, 0, pose, s.Squash, out tip);
                    dc.Pop();
                }
            DrawPuppet(dc, f, x, y, Zoom * f.Size, t, flash, pose, s.Squash, out tip);
            DrawSwoosh(dc, f, s, tip);
            if (f.St == BS.Dizzy) for (int i = 0; i < 3; i++) { double a = t * 5 + i * 2.1; Star(dc, x + Math.Cos(a) * 24 * Zoom, SY(f.Y + f.H + 10) + Math.Sin(a) * 6, 8, 3.5, a, 0xFFFDE047, 0xFFB45309); }
            if (f.St == BS.Attack && f.Charge > 0)
            {
                double c = 0.5 + 0.5 * Math.Sin(t * 30);
                dc.DrawEllipse(Glow(Gfx.Alpha(0xFFFFFFFF, 0.25 + 0.25 * c)), null, new Point(x, y - f.H * 0.5 * Zoom), f.H * 0.8 * Zoom, f.H * 0.8 * Zoom);
            }
        }

        /// <summary>The bright arc a strike leaves along the path of its hand, foot or weapon while it can hit.</summary>
        void DrawSwoosh(DrawingContext dc, BFighter f, AnimState s, Point tip)
        {
            bool active = f.St == BS.Attack && f.Mv != null && f.Mv.Kind != "rise" && f.Mv.Kind != "dive" && f.Mv.Kind != "lunge"
                && f.Mv.Hits.Exists(h => f.MF >= h.From - 2 && f.MF <= h.To + 1);
            if (active)
            {
                if (s.TrailN > 0 && (Math.Abs(s.Trail[0].X - tip.X) + Math.Abs(s.Trail[0].Y - tip.Y)) < 0.5) { }
                else
                {
                    for (int i = Math.Min(s.TrailN, s.Trail.Length - 1); i > 0; i--) s.Trail[i] = s.Trail[i - 1];
                    s.Trail[0] = tip; s.TrailN = Math.Min(s.Trail.Length, s.TrailN + 1);
                }
            }
            else if (s.TrailN > 0) s.TrailN--;
            if (s.TrailN < 2) return;
            uint c = Gfx.Mix(Rim, 0xFFFFFFFF, 0.5);
            for (int i = 0; i < s.TrailN - 1; i++)
            {
                double k = 1 - i / (double)(s.TrailN - 1);
                dc.DrawLine(Gfx.P(Gfx.Alpha(c, 0.25 * k), (14 * k + 2) * Zoom), s.Trail[i], s.Trail[i + 1]);
                dc.DrawLine(Gfx.P(Gfx.Alpha(0xFFFFFFFF, 0.75 * k), (5 * k + 1) * Zoom), s.Trail[i], s.Trail[i + 1]);
            }
        }

        // ---------------------------------------------------------------- poses
        static BPose Stand = new BPose { FA1 = -78, FA2 = 18, BA1 = -102, BA2 = 14, FL1 = -82, BL1 = -98 };

        static BPose P(double lean, double crouch, double fa1, double fa2, double ba1, double ba2, double fl1, double fl2, double bl1, double bl2)
        {
            return new BPose { Lean = lean, Crouch = crouch, FA1 = fa1, FA2 = fa2, BA1 = ba1, BA2 = ba2, FL1 = fl1, FL2 = fl2, BL1 = bl1, BL2 = bl2 };
        }

        /// <summary>The wind-up and the strike of each kind of move.</summary>
        static void AnimPoses(string anim, out BPose wind, out BPose strike)
        {
            switch (anim)
            {
                case "jab": wind = P(0, 0.1, -60, 100, -100, 30, -75, -10, -100, 0); strike = P(12, 0.1, -4, 0, -110, 40, -70, -10, -105, 0); return;
                case "peck": wind = P(-10, 0.1, -60, 40, -100, 30, -80, 0, -100, 0); strike = P(40, 0.15, -30, 20, -120, 30, -70, -20, -110, 0); return;
                case "poke": wind = P(-5, 0.1, -50, 70, -100, 30, -75, -10, -100, 0); strike = P(14, 0.1, -8, 0, -110, 40, -70, -10, -105, 0); return;
                case "punch": wind = P(-6, 0.15, -120, 80, -60, 60, -70, -15, -105, 0); strike = P(18, 0.15, 0, 0, -120, 40, -60, -20, -110, -10); return;
                case "kick": wind = P(-8, 0.1, -60, 40, -120, 30, -40, -90, -95, 0); strike = P(-18, 0.05, -40, 30, -140, 20, 6, 0, -95, 0); return;
                case "swing": wind = P(-10, 0.1, 115, 30, -100, 20, -75, -10, -100, 0); strike = P(16, 0.15, -25, 0, -110, 20, -65, -20, -110, 0); return;
                case "upswing": wind = P(10, 0.2, -110, 20, -80, 20, -70, -20, -105, 0); strike = P(-8, 0.05, 95, 0, -100, 20, -85, 0, -95, 0); return;
                case "lowkick": wind = P(0, 0.5, -60, 40, -110, 30, -50, -60, -110, -40); strike = P(-12, 0.55, -40, 40, -120, 30, -6, 0, -120, -50); return;
                case "dash": wind = P(20, 0.2, -40, 60, -130, 30, -60, -30, -120, -10); strike = P(34, 0.25, -5, 0, -150, 20, -40, -40, -140, -20); return;
                case "bigswing": wind = P(-18, 0.2, 140, 20, -70, 30, -70, -20, -110, 0); strike = P(26, 0.25, -35, 0, -140, 20, -50, -30, -120, -10); return;
                case "bigpunch": wind = P(-14, 0.2, -165, 60, -40, 60, -70, -20, -105, 0); strike = P(26, 0.2, 2, 0, -140, 40, -50, -30, -125, -10); return;
                case "uppercut": wind = P(10, 0.45, -110, 40, -80, 40, -60, -50, -110, -30); strike = P(-6, 0, 92, 0, -110, 30, -88, 0, -92, 0); return;
                case "splits": wind = P(0, 0.4, -60, 40, -120, 40, -60, -40, -120, -40); strike = P(0, 0.75, 20, 0, 160, 0, -4, 0, -176, 0); return;
                case "spin": wind = P(0, 0.2, 10, 0, 170, 0, -70, -20, -110, -20); strike = P(0, 0.2, 0, 0, 180, 0, -60, -30, -120, -30); return;
                case "airpunch": wind = P(-10, 0, -150, 60, -60, 40, -60, -60, -110, -40); strike = P(20, 0, 0, 0, -130, 30, -50, -70, -120, -50); return;
                case "overhead": wind = P(-12, 0, 120, 20, -90, 40, -60, -60, -110, -40); strike = P(18, 0, -50, 0, -120, 30, -50, -70, -120, -50); return;
                case "backkick": wind = P(10, 0, -60, 40, -110, 30, -60, -60, -120, -60); strike = P(32, 0, -40, 30, -130, 20, -70, -40, 178, 0); return;
                case "flip": wind = P(0, 0, 60, 40, 120, 40, -60, -70, -120, -60); strike = P(0, 0, 80, 10, 100, 10, 80, 0, 100, -20); return;
                case "stomp": wind = P(0, 0, 80, 30, 100, 30, -50, -80, -130, -60); strike = P(0, 0, 100, 20, 80, 20, -88, 0, -92, 0); return;
                case "grab": wind = P(6, 0.15, -30, 60, -40, 60, -70, -15, -105, 0); strike = P(14, 0.15, -4, 0, -8, 0, -65, -20, -110, 0); return;
                case "throwf": wind = P(-10, 0.2, -20, 40, -30, 40, -70, -20, -105, 0); strike = P(24, 0.2, 10, 0, -40, 30, -60, -30, -115, 0); return;
                case "throwb": wind = P(10, 0.2, -10, 40, -20, 40, -70, -20, -105, 0); strike = P(-30, 0.2, 170, 0, 160, 0, -80, -10, -100, 0); return;
                case "throwu": wind = P(0, 0.35, -40, 60, -40, 60, -60, -40, -110, -20); strike = P(-6, 0, 90, 0, 90, 0, -85, 0, -95, 0); return;
                case "throwd": wind = P(0, 0, 40, 40, 40, 40, -80, 0, -100, 0); strike = P(30, 0.3, -70, 0, -80, 0, -60, -40, -110, -20); return;
                case "cast": wind = P(-8, 0.1, -150, 60, -150, 60, -75, -10, -100, 0); strike = P(14, 0.1, 0, 0, -6, 0, -70, -10, -105, 0); return;
                case "lash": wind = P(-12, 0.15, 140, 40, -110, 30, -75, -10, -100, 0); strike = P(18, 0.15, 0, 0, -120, 30, -65, -20, -110, 0); return;
                case "aim": wind = P(0, 0.05, -20, 30, -30, 60, -78, 0, -100, 0); strike = P(-4, 0.05, 0, 0, -10, 40, -78, 0, -100, 0); return;
                case "throw": wind = P(-14, 0.15, 150, 30, -60, 30, -75, -10, -100, 0); strike = P(18, 0.15, 10, 0, -120, 30, -65, -20, -110, 0); return;
                case "drill": wind = P(-6, 0.15, -30, 60, -40, 60, -75, -10, -100, 0); strike = P(10, 0.15, 0, 0, -5, 10, -70, -15, -105, 0); return;
                case "rise": wind = P(0, 0.35, -110, 30, -110, 30, -60, -50, -115, -30); strike = P(0, 0, 88, 0, 92, 0, -86, 0, -94, 0); return;
                case "zip": wind = P(0, 0.3, -90, 30, -110, 30, -60, -50, -115, -30); strike = P(10, 0, 60, 0, 70, 0, -95, 0, -100, 0); return;
                case "flap": wind = P(10, 0.2, 40, 0, 140, 0, -70, -30, -110, -30); strike = P(10, 0, -20, 0, 200, 0, -80, -20, -100, -20); return;
                case "kite": wind = P(0, 0.2, -60, 40, -110, 30, -75, -10, -100, 0); strike = P(-5, 0, 80, 0, -110, 20, -90, 0, -95, 0); return;
                case "leap": wind = P(10, 0.4, -110, 30, -120, 30, -55, -60, -115, -40); strike = P(-12, 0, 70, 20, 110, 20, -50, -70, -120, -50); return;
                case "guard": wind = P(0, 0.2, -10, 120, -15, 120, -75, -15, -105, -10); strike = P(0, 0.25, -5, 125, -10, 125, -72, -20, -108, -15); return;
                case "burst": wind = P(0, 0.4, -40, 90, -140, 90, -60, -40, -120, -40); strike = P(0, 0, 40, 0, 140, 0, -60, 0, -120, 0); return;
                case "dive": wind = P(0, 0.2, 90, 40, 90, 40, -60, -60, -120, -60); strike = P(0, 0, 100, 0, 80, 0, -88, 0, -92, 0); return;
                case "headbutt": wind = P(-20, 0.2, -120, 40, -110, 40, -70, -20, -110, 0); strike = P(40, 0.15, -150, 20, -140, 20, -60, -20, -115, 0); return;
                case "shoulder": wind = P(-10, 0.3, -100, 60, -160, 40, -60, -40, -120, -20); strike = P(28, 0.25, -80, 90, -170, 20, -50, -30, -125, -10); return;
                case "clap": wind = P(0, 0.1, 60, 40, 120, -40, -75, -10, -100, 0); strike = P(10, 0.1, -5, 0, 10, -10, -70, -10, -105, 0); return;
                case "slam": wind = P(-15, 0.1, 120, 20, 110, 20, -75, -10, -100, 0); strike = P(25, 0.35, -40, 0, -50, 0, -60, -40, -120, -30); return;
                case "sweep": wind = P(10, 0.6, -40, 40, -140, 40, -30, -80, -120, -50); strike = P(-10, 0.65, -20, 30, -160, 30, 0, 0, -170, 0); return;
                case "twirl": wind = P(0, 0.1, 10, 0, 170, 0, -80, -10, -100, 0); strike = P(0, 0.1, 0, 0, 180, 0, -75, -15, -105, 0); return;
                case "ride": wind = P(0, 0.55, -40, 60, -110, 40, -30, -70, -150, -40); strike = P(14, 0.55, -10, 0, -120, 40, -20, -80, -150, -40); return;
            }
            wind = Stand; strike = Stand;
        }


        static double Smooth(double x) { x = Math.Max(0, Math.Min(1, x)); return x * x * (3 - 2 * x); }
        static double EaseOut(double x) { x = Math.Max(0, Math.Min(1, x)); return 1 - (1 - x) * (1 - x); }

        /// <summary>The pose for a fighter's state (menus: the stride from the clock).</summary>
        public static BPose Pose(BFighter f, double t) { return Pose(f, t, t * (f.St == BS.Run ? 11 : 7)); }

        /// <summary>The pose for a fighter's state and move. stride = how far through the run cycle (radians; the match
        /// moves it with the real speed, so feet don't slide). The match blends from one pose to the next (AnimState).</summary>
        public static BPose Pose(BFighter f, double t, double stride)
        {
            switch (f.St)
            {
                case BS.Idle:
                    {
                        // breathing, and the weight shifting from foot to foot
                        double br = Math.Sin(t * 2.6 + f.Slot), sh = Math.Sin(t * 0.9 + f.Slot * 1.7);
                        BPose p = Stand;
                        p.Crouch = 0.05 + br * 0.02; p.Lean = 2 + sh * 2;
                        p.FA1 += br * 4; p.BA1 -= br * 4; p.FA2 += 10 + br * 4; p.BA2 += 8;
                        p.FL1 += sh * 3; p.BL1 += sh * 3; p.Head = -sh * 3;
                        return p;
                    }
                case BS.Walk:
                case BS.Run:
                    {
                        bool run = f.St == BS.Run;
                        double s = Math.Sin(stride), c = Math.Cos(stride);
                        double a = run ? 48 : 28;
                        // each leg: the thigh swings; the knee folds high while the leg comes forward (cos > 0)
                        double fk = c > 0 ? c : 0, bk = c < 0 ? -c : 0;
                        var p = new BPose
                        {
                            Lean = run ? 17 + Math.Abs(s) * 4 : 5,
                            // the body is lowest at full stride and highest as the legs pass
                            Crouch = (run ? 0.1 : 0.03) + Math.Abs(s) * (run ? 0.08 : 0.03),
                            FL1 = -90 + a * s, FL2 = -(fk * (run ? 100 : 45) + 8),
                            BL1 = -90 - a * s, BL2 = -(bk * (run ? 100 : 45) + 8),
                            // arms pump against the legs, bent at the elbow when running
                            FA1 = -90 - a * 0.9 * s, FA2 = run ? 95 + 10 * s : 18,
                            BA1 = -90 + a * 0.9 * s, BA2 = run ? 95 - 10 * s : 18,
                            Head = run ? -8 : -2
                        };
                        return p;
                    }
                case BS.Crouch: return P(16, 0.48, -55, 60, -115, 45, -40, -85, -125, -45);
                case BS.Squat:
                    {
                        // the wind-up of a jump: down low, arms swung back
                        BPose p = P(10, 0.5, -150, 30, -160, 30, -45, -80, -120, -50);
                        return BPose.Lerp(P(4, 0.25, -110, 30, -130, 30, -60, -60, -115, -30), p, f.T / 3.0);
                    }
                case BS.Land: return BPose.Lerp(P(8, 0.5, -40, 30, -140, 30, -45, -80, -120, -50), Stand, f.Lag > 0 ? f.T / (double)Math.Max(1, f.Lag) : 1);
                case BS.Air:
                case BS.AirDodge:
                    {
                        // going up: knees tucked, arms up; at the top: spread out; coming down: legs reaching for the ground
                        BPose rise = P(4, 0, 40, 60, 150, 40, -30, -95, -110, -70);
                        BPose apex = P(0, 0, 20, 30, 160, 30, -60, -50, -120, -40);
                        BPose fall = P(-6, 0, 70, 20, 120, 20, -95, -12, -80, -25);
                        double vy = f.Vy + f.Ly;
                        BPose p = vy > 2 ? BPose.Lerp(apex, rise, (vy - 2) / 8) : BPose.Lerp(apex, fall, (2 - vy) / 8);
                        // lean into the drift
                        p.Lean += Math.Max(-14, Math.Min(14, f.Vx * f.Face * 2.2));
                        if (f.St == BS.AirDodge) { p = P(0, 0.4, 60, 110, 120, 110, -40, -90, -140, -80); p.Spin = -Smooth(f.T / 22.0) * 360; }
                        return p;
                    }
                case BS.Helpless:
                    {
                        BPose p = P(-10, 0, 140, 10, 150, 10, -70, -20, -110, -20);
                        p.FA1 += Math.Sin(t * 9) * 12; p.BA1 -= Math.Sin(t * 9) * 12; p.Head = Math.Sin(t * 6) * 8;
                        return p;
                    }
                case BS.Shield: case BS.ShieldStun: return P(-4, 0.22, -10, 125, -15, 120, -70, -20, -110, -15);
                case BS.ShieldDrop: return P(0, 0.1, -40, 60, -60, 60, -78, 0, -100, 0);
                case BS.Roll: { BPose p = P(0, 0.55, 60, 100, 120, 100, -30, -100, -150, -80); p.Spin = -Smooth(f.T / 24.0) * 360; return p; }
                case BS.Dodge: { BPose p = P(-14, 0.2, 60, 40, 120, 40, -70, -20, -110, -20); p.Lean -= Math.Sin(Math.Min(1, f.T / 22.0) * Math.PI) * 10; return p; }
                case BS.Hurt:
                    {
                        // knocked back: flung limbs that flail, spinning if it was a big hit
                        double w = Math.Sin(t * 22);
                        BPose p = P(-26, 0, 120 + w * 20, 20, 160 - w * 20, 20, -50 + w * 15, -30, -130 - w * 15, -20);
                        if (f.Tumble) p.Spin = -f.T * 14;
                        return p;
                    }
                case BS.Down: { BPose p = P(0, 0, 160, 0, 170, 0, -60, 0, -80, 0); p.Spin = 90; p.Head = Math.Sin(t * 3) * 6; return p; }
                case BS.Dizzy: { BPose p = P(Math.Sin(t * 4) * 12, 0.15, -100, 40, -80, 40, -80, -10, -100, -10); p.Head = Math.Sin(t * 5) * 15; return p; }
                case BS.Ledge:
                    {
                        double sw = Math.Sin(t * 2.2 + f.Slot);
                        return P(-6, 0, 82, 0, 96, 0, -95 + sw * 6, -10 - sw * 6, -85 - sw * 6, -20);
                    }
                case BS.LedgeUp: return BPose.Lerp(P(-6, 0, 82, 0, 96, 0, -95, -10, -85, -20), P(24, 0.55, -40, 60, -110, 40, -35, -95, -125, -45), Smooth(f.T / 14.0));
                case BS.Grabbing: return P(12, 0.18, -6, 10, -12, 10, -65, -20, -110, -5);
                case BS.Grabbed: { double w = Math.Sin(t * 16); return P(-20, 0, 120 + w * 10, 20, 150 - w * 10, 20, -80 + w * 8, -10, -105 - w * 8, -10); }
                case BS.Enter: return P(0, 0, 60 + Math.Sin(t * 3) * 10, 60, 120 - Math.Sin(t * 3) * 10, 60, -80, 0, -100, 0);
                case BS.Attack:
                    {
                        BMove m = f.Mv;
                        if (m == null) return Stand;
                        BPose wind, strike;
                        // each fighter's own way of moving (BrawlMotion.cs)
                        string fid = f.D != null ? f.D.Id : null;
                        MotionStyle ms = MotionOf(fid);
                        StyledPoses(fid, m.Anim, f.AirMove, out wind, out strike);
                        BPose from = f.AirMove ? P(0, 0, 20, 30, 160, 30, -60, -50, -120, -40) : Stand;
                        int a = Math.Max(1, m.FirstActive), b = a;
                        foreach (BHit h in m.Hits) b = Math.Max(b, h.To);
                        if (m.Shot != null) b = Math.Max(b, m.ShotAt + 4);
                        if (m.Kind == "rise" || m.Kind == "lunge" || m.Kind == "dive") b = Math.Max(b, m.VTo);
                        if (m.Hits.Count == 0 && m.Shot == null) { a = Math.Max(2, m.Len / 4); b = m.Len * 3 / 4; }
                        double fr = f.MF;
                        // past the strike a little (follow-through), then back to rest
                        BPose over = BPose.LerpFree(wind, strike, ms.Over);
                        BPose p;
                        if (fr < a) p = BPose.Lerp(from, wind, EaseOut(fr / a));                       // anticipation
                        else if (fr <= b) p = BPose.Lerp(wind, strike, EaseOut((fr - a + 1) / ms.Snap));     // the snap
                        else
                        {
                            double k = (fr - b) / Math.Max(1, m.Len - b);
                            p = k < 0.3 ? BPose.Lerp(strike, over, Smooth(k / 0.3)) : BPose.Lerp(over, f.Ground ? Stand : from, Smooth((k - 0.3) / 0.7));
                        }
                        // a charging strong attack trembles, held in its wind-up
                        p = Alive(ms, m.Anim, f.AirMove, p, fr, a, b, m.Len, t);
                        if (f.Charge > 0) { p = wind; p.Lean += Math.Sin(t * 50) * 1.5; p.Crouch += 0.05; }
                        if (m.Anim == "spin" || m.Anim == "twirl" || m.Id == "nair") p.Spin = fr >= a && fr <= b ? -Smooth((fr - a) / Math.Max(1, b - a)) * 360 : 0;
                        if (m.Anim == "flip") p.Spin = fr >= a - 3 && fr <= b + 3 ? Smooth((fr - a + 3) / Math.Max(1, b - a + 6)) * 360 : 0;
                        if (m.Anim == "flap") { double w = Math.Sin(fr * 0.9); p.FA1 = 10 + 50 * w; p.BA1 = 170 - 50 * w; }
                        if (m.Anim == "drill" && fr >= a && fr <= b) p.Lean += Math.Sin(fr * 2) * 3;
                        if (m.Kind == "rise" && fr >= m.VFrom && fr <= m.VTo) { p.FL1 += Math.Sin(fr * 0.5) * 6; p.BL1 -= Math.Sin(fr * 0.5) * 6; }
                        return p;
                    }
            }
            return Stand;
        }

        // ---------------------------------------------------------------- the puppet
        struct Rig
        {
            public Point Hip, Neck, Head, FHand, BHand, FFoot, BFoot, FElbow, BElbow, FKnee, BKnee;
            public double HeadR, ArmW, LegW, BodyW, TorsoLen;
            public double FArmAng, BArmAng;
        }

        static Point Off(Point p, double ang, double len) { double a = ang * Math.PI / 180; return new Point(p.X + Math.Cos(a) * len, p.Y + Math.Sin(a) * len); }

        static Rig Build(BFighterDef d, BPose p)
        {
            double H = d.Height, k = H / 90.0;
            var r = new Rig();
            // each fighter's own proportions (BrawlBodies.cs)
            BodyStyle st = BodyStyle.For(d.Id);
            double headR = H * st.Head, legLen = H * st.Leg, armLen = H * st.Arm, torso = H * st.Torso;
            r.HeadR = headR; r.TorsoLen = torso;
            r.ArmW = d.Width * st.ArmW; r.LegW = d.Width * st.LegW; r.BodyW = d.Width * st.BodyW;
            double hipY = legLen * (1 - 0.42 * p.Crouch);
            r.Hip = new Point(0, hipY);
            double lean = p.Lean * Math.PI / 180;
            r.Neck = new Point(r.Hip.X + Math.Sin(lean) * torso, r.Hip.Y + Math.Cos(lean) * torso);
            double hl = (p.Lean + p.Head) * Math.PI / 180;
            r.Head = new Point(r.Neck.X + Math.Sin(hl) * headR * 1.05, r.Neck.Y + Math.Cos(hl) * headR * 1.05);
            Point sh = new Point(r.Neck.X - Math.Sin(lean) * torso * 0.12, r.Neck.Y - Math.Cos(lean) * torso * 0.12);
            r.FElbow = Off(sh, p.FA1, armLen / 2); r.FHand = Off(r.FElbow, p.FA1 + p.FA2, armLen / 2);
            r.BElbow = Off(sh, p.BA1, armLen / 2); r.BHand = Off(r.BElbow, p.BA1 + p.BA2, armLen / 2);
            r.FArmAng = p.FA1 + p.FA2; r.BArmAng = p.BA1 + p.BA2;
            r.FKnee = Off(r.Hip, p.FL1, legLen / 2); r.FFoot = Off(r.FKnee, p.FL1 + p.FL2, legLen / 2);
            r.BKnee = Off(r.Hip, p.BL1, legLen / 2); r.BFoot = Off(r.BKnee, p.BL1 + p.BL2, legLen / 2);
            return r;
        }


        /// <summary>Draws a fighter with its feet at (x, y) on the screen, scale screen pixels per world pixel. Also the
        /// menus' portraits (a fighter that isn't in a match: BrawlView.Portrait).</summary>
        public void DrawPuppet(DrawingContext dc, BFighter f, double x, double y, double scale, double t, double flash)
        {
            Point tip;
            DrawPuppet(dc, f, x, y, scale, t, flash, Pose(f, t), 1, out tip);
        }

        /// <summary>The same with a pose already worked out (the match blends them: AnimState), squashed or stretched
        /// (sq below 1 squashes, above 1 stretches, keeping the volume); tip = where the striking hand, foot or prop
        /// ends on the screen (for the swoosh).</summary>
        public void DrawPuppet(DrawingContext dc, BFighter f, double x, double y, double scale, double t, double flash, BPose p, double sq, out Point tip)
        {
            Rig r = Build(f.D, p);
            var m = new Matrix(scale * f.Face / Math.Sqrt(sq), 0, 0, -scale * sq, x, y);
            dc.PushTransform(new MatrixTransform(m));
            Matrix all = m;
            if (p.Spin != 0)
            {
                var rot = new RotateTransform(p.Spin, 0, f.D.Height * 0.5);
                dc.PushTransform(rot);
                all = rot.Value * m;
            }
            if (flash > 0) dc.PushOpacity(1 - flash * 0.5);
            Dress(dc, f, r, p, t);
            if (flash > 0) dc.Pop();
            if (p.Spin != 0) dc.Pop();
            dc.Pop();
            tip = all.Transform(TipOf(f, r));
        }

        /// <summary>What leads a strike: the foot for kicks, the end of a stick, blade or log, else the hand.</summary>
        static Point TipOf(BFighter f, Rig r)
        {
            string anim = f.Mv != null ? f.Mv.Anim : "";
            switch (anim)
            {
                case "kick": case "lowkick": case "splits": case "stomp": case "flip": case "sweep": return r.FFoot;
                case "headbutt": return new Point(r.Head.X + r.HeadR, r.Head.Y);
                case "shoulder": return r.Neck;
                case "backkick": return r.BFoot;
            }
            switch (f.D.Id)
            {
                case "teddy": return Off(r.FHand, r.FArmAng, f.D.Height * 0.42);
                case "ben": return Off(r.FHand, r.FArmAng, f.D.Height * 0.36);
                case "knight": return Off(r.FHand, r.FArmAng, f.D.Height * 0.5);
                case "paul": return Off(r.FHand, r.FArmAng, f.D.Height * 0.42);
            }
            return r.FHand;
        }

        /// <summary>A fighter standing for the menus (or its head only, for the damage panels).</summary>
        public void Portrait(DrawingContext dc, BFighterDef d, double x, double y, double height, double t, bool headOnly, BS st)
        {
            var f = new BFighter { D = d, St = st, Face = 1, Slot = 0, T = (int)(t * 60) };
            if (st == BS.Attack) { f.Mv = d.M("fstrong"); f.MF = 17; }
            double s = height / d.Height;
            if (!headOnly) { DrawPuppet(dc, f, x, y, s, t, 0); return; }
            BPose p = Stand;
            Rig r = Build(d, p);
            double hs = height / (r.HeadR * 2.6);
            var m = new Matrix(hs, 0, 0, -hs, x - r.Head.X * hs, y + r.Head.Y * hs);
            dc.PushTransform(new MatrixTransform(m));
            HeadOf(dc, f, r, p, t);
            dc.Pop();
        }

        static Pen Outline(double w) { return Gfx.P(0xFF161616, w); }

        /// <summary>The stage's light colour, for the bright edge on heads and bodies (set by Draw).</summary>
        public static uint Rim = 0xFFFFFFFF;

        static void Ball(DrawingContext dc, Point p, double r, uint c)
        {
            dc.DrawEllipse(BrawlView.Sphere(c), Outline(2.5), p, r, r);
            if (r > 6) dc.DrawGeometry(null, Gfx.P(Gfx.Alpha(Rim, 0.55), Math.Max(1.2, r * 0.12)), Cap(p, r * 0.86, 20, 80));
        }

        static void Tendril(DrawingContext dc, Point a, Point c, double w, uint col, uint dark, double t, int seed)
        {
            double dx = c.X - a.X, dy = c.Y - a.Y, len = Math.Sqrt(dx * dx + dy * dy) + 0.01;
            double nx = -dy / len, ny = dx / len;
            double wig = Math.Sin(t * 6 + seed) * len * 0.18;
            var mid = new Point(a.X + dx * 0.5 + nx * wig, a.Y + dy * 0.5 + ny * wig);
            var g = new StreamGeometry();
            using (var s = g.Open()) { s.BeginFigure(a, false, false); s.QuadraticBezierTo(mid, c, true, true); }
            g.Freeze();
            dc.DrawGeometry(null, Gfx.P(dark, w + 4), g);
            dc.DrawGeometry(null, Gfx.P(col, w), g);
            dc.DrawEllipse(Gfx.B(dark), null, c, w * 0.6, w * 0.6);
        }

        void Dress(DrawingContext dc, BFighter f, Rig r, BPose p, double t)
        {
            BFighterDef d = f.D;
            string extra = f.St == BS.Attack && f.Mv != null ? f.Mv.Extra : "";
            bool active = f.St == BS.Attack && f.Mv != null && (f.MF >= f.Mv.VFrom && f.MF <= f.Mv.VTo && f.Mv.VTo > 0 || f.Mv.Hits.Exists(h => f.MF >= h.From && f.MF <= h.To));
            ExtraBehind(dc, f, r, extra, active, t);
            switch (d.Id)
            {
                case "specimen": DressSpecimen(dc, f, r, p, t, active); break;
                case "dusty": DressDusty(dc, f, r, p, t); break;
                case "sam": DressSam(dc, f, r, p, t); break;
                default: DressPerson(dc, f, r, p, t); break;
            }
            ExtraFront(dc, f, r, extra, active, t);
        }

        // ---- people: Bloom, Trooper, Juniper, Teddy, Ben, Firewall Knight, Paul Bunyan
        void DressPerson(DrawingContext dc, BFighter f, Rig r, BPose p, double t)
        {
            BFighterDef d = f.D;
            uint sleeve = d.Main, hand = d.Skin, pants = d.Second, shoe = d.Dark;
            switch (d.Id)
            {
                case "trooper": hand = 0xFF3A3A30; pants = d.Main; shoe = 0xFF1C1917; break;
                case "juniper": hand = 0xFFE2E8F0; pants = d.Main; shoe = 0xFF475569; break;
                case "knight": sleeve = d.Skin; hand = d.Main; pants = d.Main; shoe = d.Second; break;
                case "teddy": pants = d.Main; shoe = 0xFF3F2A14; break;
                case "ben": pants = 0xFF1F2937; shoe = 0xFF111111; break;
                case "paul": pants = 0xFF1E40AF; shoe = 0xFF451A03; break;
                case "bloom": pants = 0xFF0F766E; shoe = 0xFFF472B6; break;
            }
            BodyStyle st = BodyStyle.For(d.Id);
            // back arm and leg
            if (d.Id == "juniper") { dc.DrawRoundedRectangle(Gfx.B(0xFF94A3B8), Outline(2.5), new Rect(r.Neck.X - r.BodyW * 1.15, r.Hip.Y + r.TorsoLen * 0.15, r.BodyW * 0.6, r.TorsoLen * 0.8), 6, 6); }
            Limb(dc, Shoulder(r), r.BElbow, r.BHand, Gfx.Mix(sleeve, 0xFF000000, 0.25), Gfx.Mix(sleeve, 0xFF000000, 0.25), r.ArmW, st.ArmTaper, st.Bulge, st);
            Hand(dc, r.BHand, r.BArmAng, r.ArmW * st.ArmTaper, Gfx.Mix(hand, 0xFF000000, 0.2), st.Hand);
            if (d.Id == "knight") Shield(dc, r.BHand, r.ArmW * 2.6, t);
            Limb(dc, r.Hip, r.BKnee, r.BFoot, Gfx.Mix(pants, 0xFF000000, 0.25), Gfx.Mix(pants, 0xFF000000, 0.25), r.LegW, st.LegTaper, st.Bulge * 0.7, st);
            FootOf(dc, r.BFoot, r.LegW * st.LegTaper, Gfx.Mix(shoe, 0xFF000000, 0.2), st.Foot);
            // body
            Torso(dc, f, r, p, t);
            // front leg
            Limb(dc, r.Hip, r.FKnee, r.FFoot, pants, pants, r.LegW, st.LegTaper, st.Bulge * 0.7, st);
            FootOf(dc, r.FFoot, r.LegW * st.LegTaper, shoe, st.Foot);
            HeadOf(dc, f, r, p, t);
            // front arm and anything held
            Held(dc, f, r, p, t, true);
            Limb(dc, Shoulder(r), r.FElbow, r.FHand, sleeve, sleeve, r.ArmW, st.ArmTaper, st.Bulge, st);
            Hand(dc, r.FHand, r.FArmAng, r.ArmW * st.ArmTaper, hand, st.Hand);
            Held(dc, f, r, p, t, false);
        }

        static Point Shoulder(Rig r) { return new Point(r.Neck.X - (r.Neck.X - r.Hip.X) * 0.12, r.Neck.Y - (r.Neck.Y - r.Hip.Y) * 0.12); }

        static void Foot(DrawingContext dc, Point p, double w, uint c)
        {
            dc.DrawEllipse(Gfx.B(c), Outline(2.5), new Point(p.X + w * 0.35, p.Y + w * 0.1), w * 0.85, w * 0.5);
        }

        static void Shield(DrawingContext dc, Point p, double s, double t)
        {
            Poly(dc, Gfx.B(0xFF334155), Outline(2.5), new Point(p.X - s * 0.2, p.Y + s * 0.6), new Point(p.X + s * 0.6, p.Y + s * 0.6), new Point(p.X + s * 0.6, p.Y - s * 0.2), new Point(p.X + s * 0.2, p.Y - s * 0.7), new Point(p.X - s * 0.2, p.Y - s * 0.2));
            dc.DrawEllipse(Gfx.B(Math.Sin(t * 4) > 0 ? 0xFFF97316 : 0xFFFB923C), null, new Point(p.X + s * 0.2, p.Y), s * 0.16, s * 0.16);
        }

        void Torso(DrawingContext dc, BFighter f, Rig r, BPose p, double t)
        {
            BFighterDef d = f.D;
            double w = r.BodyW;
            Point a = r.Hip, b = r.Neck;
            double ang = Math.Atan2(b.Y - a.Y, b.X - a.X) * 180 / Math.PI - 90;
            double len = Math.Sqrt((b.X - a.X) * (b.X - a.X) + (b.Y - a.Y) * (b.Y - a.Y));
            dc.PushTransform(new RotateTransform(ang, a.X, a.Y));
            uint body = d.Main;
            // the fighter's own shape (BrawlBodies.cs), rounded like a body: darker at both sides, lighter just off the
            // middle, a shadow under the head
            dc.DrawGeometry(BrawlView.Cylinder(body), Outline(2.5), BodyShape(a.X, a.Y - w * 0.15, a.Y + len + w * 0.05, w, BodyStyle.For(d.Id)));
            dc.DrawEllipse(Gfx.B(0x30000000), null, new Point(a.X, a.Y + len - w * 0.02), w * 0.36, w * 0.14);
            double cx = a.X, top = a.Y + len;
            switch (d.Id)
            {
                case "bloom":
                    dc.DrawRectangle(Gfx.B(0xFFFDE047), null, new Rect(cx - w / 2 + 2, a.Y + len * 0.1, w - 4, len * 0.12));
                    Star(dc, cx + w * 0.12, a.Y + len * 0.6, w * 0.18, w * 0.08, 0, 0xFFFDE047, 0xFFB45309);
                    break;
                case "trooper":
                    dc.DrawRoundedRectangle(Gfx.B(d.Second), Outline(1.5), new Rect(cx - w * 0.42, a.Y + len * 0.3, w * 0.84, len * 0.6), 5, 5);
                    dc.DrawRectangle(Gfx.B(0xFF3F4A2E), null, new Rect(cx - w * 0.3, a.Y + len * 0.4, w * 0.22, len * 0.18));
                    dc.DrawRectangle(Gfx.B(0xFF3F4A2E), null, new Rect(cx + w * 0.08, a.Y + len * 0.4, w * 0.22, len * 0.18));
                    break;
                case "juniper":
                    dc.DrawEllipse(Gfx.B(Math.Sin(t * 3) > 0 ? 0xFF93C5FD : 0xFF60A5FA), Outline(1.5), new Point(cx + w * 0.15, a.Y + len * 0.62), w * 0.14, w * 0.14);
                    dc.DrawRectangle(Gfx.B(0xFFE2E8F0), null, new Rect(cx - w / 2 + 2, a.Y + len * 0.08, w - 4, len * 0.1));
                    break;
                case "teddy":
                    for (int i = 0; i < 3; i++) dc.DrawEllipse(Gfx.B(0xFF6B4F2A), null, new Point(cx + w * 0.22, a.Y + len * (0.3 + i * 0.22)), 2.2, 2.2);
                    dc.DrawRectangle(Gfx.B(0xFF6B4F2A), null, new Rect(cx - w / 2 + 2, a.Y + len * 0.08, w - 4, len * 0.1));
                    dc.DrawLine(Gfx.P(0xFFDC2626, 3), new Point(cx + w * 0.2, top), new Point(cx + w * 0.05, top - len * 0.25));
                    break;
                case "ben":
                    dc.DrawRoundedRectangle(Gfx.B(d.Second), null, new Rect(cx - w * 0.05, a.Y + len * 0.15, w * 0.42, len * 0.75), 4, 4);
                    dc.DrawLine(Gfx.P(0xFFFFFFFF, 4), new Point(cx + w * 0.2, top), new Point(cx + w * 0.2, top - len * 0.2));
                    for (int i = 0; i < 4; i++) dc.DrawEllipse(Gfx.B(0xFF7C4A2D), null, new Point(cx + w * 0.16, a.Y + len * (0.3 + i * 0.15)), 1.8, 1.8);
                    break;
                case "knight":
                    dc.DrawRoundedRectangle(Gfx.B(0xFF94A3B8), Outline(1.5), new Rect(cx - w * 0.42, a.Y + len * 0.35, w * 0.84, len * 0.55), 8, 8);
                    double glow = 0.6 + 0.4 * Math.Sin(t * 4);
                    dc.DrawEllipse(Gfx.B(Gfx.Alpha(0xFFF97316, glow)), null, new Point(cx + w * 0.05, a.Y + len * 0.62), w * 0.2, w * 0.2);
                    dc.DrawEllipse(Gfx.B(0xFFFDE68A), null, new Point(cx + w * 0.05, a.Y + len * 0.62), w * 0.08, w * 0.08);
                    break;
                case "paul":
                    for (int i = 1; i < 4; i++) dc.DrawLine(Gfx.P(0x90180000, 3), new Point(cx - w / 2 + 3, a.Y + len * i / 4), new Point(cx + w / 2 - 3, a.Y + len * i / 4));
                    for (int i = 1; i < 3; i++) dc.DrawLine(Gfx.P(0x90180000, 3), new Point(cx - w / 2 + w * i / 3, a.Y + 2), new Point(cx - w / 2 + w * i / 3, top - 2));
                    dc.DrawRectangle(Gfx.B(0xFF451A03), null, new Rect(cx - w / 2 + 2, a.Y + len * 0.05, w - 4, len * 0.1));
                    break;
            }
            dc.Pop();
        }

        void HeadOf(DrawingContext dc, BFighter f, Rig r, BPose p, double t)
        {
            BFighterDef d = f.D;
            Point h = r.Head; double R = r.HeadR;
            BodyStyle st = BodyStyle.For(d.Id);
            bool hurt = f.St == BS.Hurt || f.St == BS.Dizzy || f.St == BS.Helpless || f.St == BS.Grabbed;
            switch (d.Id)
            {
                case "bloom":
                    for (int i = 0; i < 7; i++)
                    {
                        double a = (i / 7.0) * 360 + 10;
                        Point pp = Off(h, a, R * 1.05);
                        dc.PushTransform(new RotateTransform(a, pp.X, pp.Y));
                        dc.DrawEllipse(Gfx.B(i % 2 == 0 ? d.Second : 0xFFF9A8D4), Outline(2), pp, R * 0.55, R * 0.32);
                        dc.Pop();
                    }
                    Skull(dc, h, R, d.Skin, st);
                    dc.DrawGeometry(Gfx.B(d.Main), Outline(2), Cap(h, R * 1.02, 0, 180));
                    dc.DrawEllipse(Gfx.B(0x70F472B6), null, new Point(h.X + R * 0.62, h.Y - R * 0.3), R * 0.2, R * 0.13);   // rosy cheeks
                    FaceEyes(dc, h, R, hurt, st);
                    Smile(dc, h, R, hurt);
                    break;
                case "trooper":
                    Skull(dc, h, R, d.Skin, st);
                    dc.DrawGeometry(Gfx.B(d.Main), Outline(2), Cap(h, R * 1.12, -10, 190));
                    dc.DrawRoundedRectangle(Gfx.B(d.Trim), Outline(1.5), new Rect(h.X - R * 0.1, h.Y - R * 0.05, R * 1.1, R * 0.38), 4, 4);
                    dc.DrawLine(Gfx.P(0x80FFFFFF, 2), new Point(h.X + R * 0.1, h.Y + R * 0.25), new Point(h.X + R * 0.5, h.Y + R * 0.25));
                    if (hurt) dc.DrawLine(Gfx.P(0xFF1F2937, 2), new Point(h.X + R * 0.2, h.Y - R * 0.45), new Point(h.X + R * 0.6, h.Y - R * 0.4));
                    break;
                case "juniper":
                    dc.DrawEllipse(Gfx.B(0xFF1F1309), null, new Point(h.X - R * 0.5, h.Y + R * 0.35), R * 0.42, R * 0.42);
                    Skull(dc, h, R * 0.82, d.Skin, st);
                    dc.DrawGeometry(Gfx.B(0xFF1F1309), null, Cap(h, R * 0.84, 20, 200));
                    Eyes(dc, h, R * 0.82, hurt, 0xFF1F2937);
                    Smile(dc, h, R * 0.82, hurt);
                    dc.DrawEllipse(Gfx.B(0x3093C5FD), Gfx.P(0xC0E0F2FE, 2.5), h, R * 1.18, R * 1.18);
                    dc.DrawLine(Gfx.P(0x90FFFFFF, 2.5), Off(h, 110, R * 0.9), Off(h, 140, R * 0.9));
                    dc.DrawLine(Gfx.P(0xFF94A3B8, 2), Off(h, 100, R * 1.18), Off(h, 100, R * 1.6));
                    dc.DrawEllipse(Gfx.B(Math.Sin(t * 5) > 0 ? 0xFFF97316 : 0xFFFDE68A), null, Off(h, 100, R * 1.65), 3, 3);
                    break;
                case "teddy":
                    Skull(dc, h, R, d.Skin, st);
                    FaceEyes(dc, h, R, hurt, st);
                    dc.DrawEllipse(null, Gfx.P(0xFF1F2937, 1.6), new Point(h.X + R * 0.28, h.Y + R * 0.12), R * 0.2, R * 0.2);
                    dc.DrawEllipse(null, Gfx.P(0xFF1F2937, 1.6), new Point(h.X + R * 0.72, h.Y + R * 0.12), R * 0.2, R * 0.2);
                    // the famous moustache, and a grin
                    Poly(dc, Gfx.B(0xFF5B3A1A), Outline(1.2), new Point(h.X + R * 0.1, h.Y - R * 0.3), new Point(h.X + R * 0.55, h.Y - R * 0.12), new Point(h.X + R * 1.0, h.Y - R * 0.32), new Point(h.X + R * 0.55, h.Y - R * 0.38));
                    if (!hurt) dc.DrawRectangle(Gfx.B(0xFFFFFFFF), Outline(1), new Rect(h.X + R * 0.3, h.Y - R * 0.6, R * 0.5, R * 0.16));
                    dc.DrawRectangle(Gfx.B(0xFF8B5A2B), Outline(2), new Rect(h.X - R * 1.35, h.Y + R * 0.55, R * 2.7, R * 0.22));
                    dc.DrawGeometry(Gfx.B(0xFFA0703A), Outline(2), Cap(new Point(h.X, h.Y + R * 0.66), R * 0.8, 0, 180));
                    break;
                case "ben":
                    dc.DrawEllipse(Gfx.B(0xFFE5E7EB), Outline(2), new Point(h.X - R * 0.45, h.Y - R * 0.1), R * 0.62, R * 0.95);
                    Skull(dc, h, R, d.Skin, st);
                    dc.DrawEllipse(Gfx.B(0xFFE5E7EB), null, new Point(h.X - R * 0.7, h.Y + R * 0.05), R * 0.35, R * 0.7);
                    FaceEyes(dc, h, R, hurt, st);
                    dc.DrawRectangle(null, Gfx.P(0xFF57534E, 1.4), new Rect(h.X + R * 0.12, h.Y + R * 0.0, R * 0.32, R * 0.22));
                    dc.DrawRectangle(null, Gfx.P(0xFF57534E, 1.4), new Rect(h.X + R * 0.56, h.Y + R * 0.0, R * 0.32, R * 0.22));
                    Smile(dc, h, R, hurt);
                    break;
                case "knight":
                    dc.DrawRoundedRectangle(BrawlView.Sphere(d.Main), Outline(2.5), new Rect(h.X - R, h.Y - R * 1.0, R * 2, R * 2.1), R * 0.7, R * 0.7);
                    dc.DrawRectangle(Gfx.B(0xFF0F172A), null, new Rect(h.X - R * 0.05, h.Y - R * 0.05, R * 1.0, R * 0.3));
                    dc.DrawRectangle(Gfx.B(Gfx.Alpha(d.Trim, 0.7 + 0.3 * Math.Sin(t * 6))), null, new Rect(h.X + R * 0.05, h.Y + R * 0.02, R * 0.85, R * 0.16));
                    dc.DrawLine(Gfx.P(0xFF94A3B8, 2.5), new Point(h.X, h.Y + R * 1.05), new Point(h.X - R * 0.2, h.Y + R * 1.6));
                    dc.DrawEllipse(Gfx.B(Math.Sin(t * 3) > 0 ? d.Trim : 0xFF7C2D12), null, new Point(h.X - R * 0.2, h.Y + R * 1.62), 3.5, 3.5);
                    break;
                case "paul":
                    Skull(dc, h, R, d.Skin, st);
                    {
                        // a big scruffy beard: up the cheek, then ragged points along the bottom
                        var beard = new List<Point> { new Point(h.X - R * 0.62, h.Y - R * 0.12), new Point(h.X + R * 0.25, h.Y - R * 0.38), new Point(h.X + R * 1.02, h.Y - R * 0.3) };
                        for (int i = 0; i <= 6; i++) { double k = i / 6.0; beard.Add(new Point(h.X + R * (1.0 - k * 1.7), h.Y - R * (1.05 + (i % 2 == 0 ? 0.32 : 0.08) - k * 0.1))); }
                        Poly(dc, Gfx.B(0xFF5B2E0E), Outline(2), beard.ToArray());
                        dc.DrawLine(Gfx.P(0xFF3B1D08, 1.6), new Point(h.X + R * 0.2, h.Y - R * 0.55), new Point(h.X + R * 0.6, h.Y - R * 0.6));   // the mouth
                    }
                    FaceEyes(dc, h, R, hurt, st);
                    dc.DrawEllipse(Gfx.B(0x60F87171), null, new Point(h.X + R * 0.7, h.Y - R * 0.05), R * 0.18, R * 0.12);
                    dc.DrawGeometry(Gfx.B(0xFFB91C1C), Outline(2), Cap(new Point(h.X, h.Y + R * 0.42), R * 0.98, 0, 180));
                    dc.DrawRectangle(Gfx.B(0xFFFFFFFF), Outline(1.5), new Rect(h.X - R * 1.0, h.Y + R * 0.3, R * 2.0, R * 0.24));
                    dc.DrawEllipse(Gfx.B(0xFFFFFFFF), Outline(1.5), new Point(h.X - R * 0.2, h.Y + R * 1.42), R * 0.22, R * 0.22);
                    break;
                case "specimen":
                    {
                        // the specimen's "head" is its eye, at the top of the tangle
                        dc.DrawEllipse(Gfx.B(0xFFFEF9C3), Outline(2.5), h, R * 1.1, R * 0.95);
                        double look = hurt ? 0 : R * 0.3;
                        dc.DrawEllipse(Gfx.B(d.Trim), null, new Point(h.X + look, h.Y), R * 0.6, R * 0.6);
                        dc.DrawEllipse(Gfx.B(0xFF111111), null, new Point(h.X + look * 1.2, h.Y), R * 0.18, R * 0.42);
                        if (hurt) dc.DrawLine(Gfx.P(0xFF111111, 3), new Point(h.X - R, h.Y), new Point(h.X + R, h.Y));
                        break;
                    }
                case "dusty":
                    {
                        Ball(dc, h, R, d.Main);
                        for (int i = 0; i < 4; i++) Poly(dc, Gfx.B(d.Trim), Outline(1.5), Off(h, 120 + i * 18, R * 0.7), Off(h, 130 + i * 18, R * 2.0), Off(h, 140 + i * 18, R * 0.8));
                        Poly(dc, Gfx.B(0xFFD6A23A), Outline(2), new Point(h.X + R * 0.7, h.Y + R * 0.25), new Point(h.X + R * 2.4, h.Y - R * 0.15), new Point(h.X + R * 0.7, h.Y - R * 0.3));
                        dc.DrawEllipse(Gfx.B(0xFFFFFFFF), Outline(1.5), new Point(h.X + R * 0.35, h.Y + R * 0.25), R * 0.36, R * 0.36);
                        dc.DrawEllipse(Gfx.B(0xFF111111), null, new Point(h.X + R * 0.45, h.Y + R * 0.25), R * (hurt ? 0.08 : 0.16), R * 0.16);
                        dc.DrawLine(Gfx.P(0xFF3B5BDB, 2.5), new Point(h.X + R * 0.0, h.Y - R * 0.05), new Point(h.X - R * 0.6, h.Y - R * 0.3));
                        break;
                    }
                case "sam":
                    {
                        // the top of the column: a face carved in, and a crown of flowers
                        dc.DrawEllipse(BrawlView.Sphere(d.Main), Outline(2.5), h, R * 1.1, R * 1.25);
                        for (int i = -1; i <= 1; i++) dc.DrawLine(Gfx.P(d.Second, 2.5), new Point(h.X + i * R * 0.5, h.Y - R), new Point(h.X + i * R * 0.5, h.Y + R * 0.9));
                        Eyes(dc, h, R, hurt, 0xFF14290F);
                        Smile(dc, h, R, hurt);
                        for (int i = 0; i < 3; i++)
                        {
                            Point fl = new Point(h.X + (i - 1) * R * 0.6, h.Y + R * 1.2 + (i == 1 ? R * 0.25 : 0));
                            for (int k = 0; k < 5; k++) dc.DrawEllipse(Gfx.B(d.Trim), Outline(1), Off(fl, k * 72, R * 0.22), R * 0.2, R * 0.2);
                            dc.DrawEllipse(Gfx.B(0xFFFACC15), null, fl, R * 0.15, R * 0.15);
                        }
                        break;
                    }
                default: Skull(dc, h, R, d.Skin, st); FaceEyes(dc, h, R, hurt, st); break;
            }
        }

        static Geometry Cap(Point c, double r, double from, double to)
        {
            var g = new StreamGeometry();
            using (var s = g.Open())
            {
                Point a = Off(c, from, r), b = Off(c, to, r);
                s.BeginFigure(a, true, true);
                s.ArcTo(b, new Size(r, r), 0, Math.Abs(to - from) > 180, SweepDirection.Clockwise, true, true);
            }
            g.Freeze();
            return g;
        }

        static void Eyes(DrawingContext dc, Point h, double R, bool hurt, uint c)
        {
            if (hurt)
            {
                foreach (double ex in new[] { 0.28, 0.72 })
                {
                    Point e = new Point(h.X + R * ex, h.Y + R * 0.12);
                    dc.DrawLine(Gfx.P(c, 2), new Point(e.X - R * 0.12, e.Y - R * 0.12), new Point(e.X + R * 0.12, e.Y + R * 0.12));
                    dc.DrawLine(Gfx.P(c, 2), new Point(e.X - R * 0.12, e.Y + R * 0.12), new Point(e.X + R * 0.12, e.Y - R * 0.12));
                }
                return;
            }
            dc.DrawEllipse(Gfx.B(c), null, new Point(h.X + R * 0.28, h.Y + R * 0.12), R * 0.1, R * 0.16);
            dc.DrawEllipse(Gfx.B(c), null, new Point(h.X + R * 0.72, h.Y + R * 0.12), R * 0.1, R * 0.16);
        }

        static void Smile(DrawingContext dc, Point h, double R, bool hurt)
        {
            if (hurt) { dc.DrawEllipse(Gfx.B(0xFF7F1D1D), null, new Point(h.X + R * 0.5, h.Y - R * 0.42), R * 0.13, R * 0.17); return; }
            dc.DrawGeometry(null, Gfx.P(0xFF7F1D1D, 2), Cap(new Point(h.X + R * 0.5, h.Y - R * 0.22), R * 0.24, 200, 340));
        }

        // ---- what people hold (sticks, the blade, the log, the cane, the blaster, the drill)
        void Held(DrawingContext dc, BFighter f, Rig r, BPose p, double t, bool behind)
        {
            BFighterDef d = f.D;
            double ang = r.FArmAng, H = d.Height;
            Point hnd = r.FHand;
            bool attacking = f.St == BS.Attack && f.Mv != null;
            switch (d.Id)
            {
                case "teddy":
                    if (!behind) { Point e = Off(hnd, ang, H * 0.42); dc.DrawLine(Outline(10), Off(hnd, ang, -6), e); dc.DrawLine(Gfx.P(0xFF7C4A1E, 6.5), Off(hnd, ang, -6), e); dc.DrawEllipse(Gfx.B(0xFF5B3414), null, e, 4.5, 4.5); }
                    break;
                case "ben":
                    if (!behind) { Point e = Off(hnd, ang, H * 0.36); dc.DrawLine(Outline(6.5), Off(hnd, ang, -4), e); dc.DrawLine(Gfx.P(0xFF3F2A14, 3.5), Off(hnd, ang, -4), e); dc.DrawEllipse(Gfx.B(0xFFD4AF37), null, e, 3.5, 3.5); }
                    break;
                case "knight":
                    if (!behind)
                    {
                        Point e = Off(hnd, ang, H * 0.5);
                        dc.DrawLine(Gfx.P(Gfx.Alpha(d.Trim, 0.35), 14), hnd, e);
                        dc.DrawLine(Outline(8), hnd, e);
                        dc.DrawLine(Gfx.P(0xFFFDBA74, 5), hnd, e);
                        dc.DrawLine(Gfx.P(0xFF475569, 7), Off(hnd, ang + 90, 8), Off(hnd, ang - 90, 8));
                    }
                    break;
                case "paul":
                    if (!behind)
                    {
                        Point e = Off(hnd, ang, H * 0.42);
                        dc.DrawLine(Outline(18), Off(hnd, ang, -10), e);
                        dc.DrawLine(Gfx.P(0xFF92400E, 13), Off(hnd, ang, -10), e);
                        dc.DrawEllipse(Gfx.B(0xFFD97706), Outline(2), e, 7, 7);
                        dc.DrawEllipse(null, Gfx.P(0xFF92400E, 1.5), e, 3.5, 3.5);
                    }
                    break;
                case "trooper":
                    if (!behind && attacking && (f.Mv.Anim == "aim"))
                    {
                        Point e = Off(hnd, ang, 26);
                        dc.DrawLine(Outline(10), hnd, e); dc.DrawLine(Gfx.P(0xFF3F3F46, 6), hnd, e);
                        dc.DrawEllipse(Gfx.B(0xFF67E8F9), null, e, 2.5, 2.5);
                    }
                    break;
                case "juniper":
                    if (!behind && attacking && f.Mv.Extra == "drill")
                    {
                        Point e = Off(hnd, ang, 34);
                        Point s1 = Off(hnd, ang + 90, 9), s2 = Off(hnd, ang - 90, 9);
                        Poly(dc, Gfx.B(0xFF94A3B8), Outline(2), s1, s2, e);
                        for (int i = 1; i < 4; i++) { double k = i / 4.0; dc.DrawLine(Gfx.P(0xFF475569, 1.5), Off(Off(hnd, ang, 34 * k), ang + 90, 9 * (1 - k) * Math.Sin(t * 40 + i)), Off(Off(hnd, ang, 34 * k), ang - 90, 9 * (1 - k))); }
                    }
                    break;
            }
        }

        // ---- the specimen: a tangle of tendrils round a red body
        void DressSpecimen(DrawingContext dc, BFighter f, Rig r, BPose p, double t, bool active)
        {
            BFighterDef d = f.D;
            double w = r.BodyW;
            Point mid = new Point((r.Hip.X + r.Neck.X) / 2, (r.Hip.Y + r.Neck.Y) / 2);
            // the front tendril stretches out to its hit while a lash is out
            Point fh = r.FHand;
            if (f.St == BS.Attack && f.Mv != null && active)
            {
                double reach = 0;
                foreach (BHit h in f.Mv.Hits) if (f.MF >= h.From && f.MF <= h.To) reach = Math.Max(reach, h.X);
                if (reach > 40) fh = new Point(reach, r.Neck.Y * 0.8);
            }
            Tendril(dc, mid, r.BHand, r.ArmW, Gfx.Mix(d.Main, 0xFF000000, 0.25), d.Dark, t, 1);
            Tendril(dc, r.Hip, r.BFoot, r.LegW, Gfx.Mix(d.Main, 0xFF000000, 0.25), d.Dark, t, 2);
            for (int i = 0; i < 3; i++) Tendril(dc, r.Hip, Off(r.Hip, -120 + i * 30 + Math.Sin(t * 3 + i) * 10, r.TorsoLen * 0.7), r.LegW * 0.6, d.Second, d.Dark, t, 5 + i);
            dc.DrawEllipse(BrawlView.Sphere(d.Main), Outline(3), mid, w * 0.95, r.TorsoLen * 0.62);
            for (int i = 0; i < 5; i++) dc.DrawEllipse(Gfx.B(d.Skin), null, Off(mid, i * 72 + t * 20, w * 0.45), w * 0.18, w * 0.12);
            Tendril(dc, r.Hip, r.FFoot, r.LegW, d.Main, d.Dark, t, 3);
            HeadOf(dc, f, r, p, t);
            Tendril(dc, mid, fh, r.ArmW, d.Main, d.Dark, t * (active ? 0.2 : 1), 4);
        }

        // ---- Dusty the roadrunner
        void DressDusty(DrawingContext dc, BFighter f, Rig r, BPose p, double t)
        {
            BFighterDef d = f.D;
            // tail feathers
            Point tail = Off(r.Hip, 200 + p.Lean * 0.5, r.TorsoLen * 1.4);
            Poly(dc, Gfx.B(0xFF5B4630), Outline(2), Off(r.Hip, 90, 5), tail, Off(tail, 260, 10), Off(r.Hip, -90, 5));
            Limb(dc, r.Hip, r.BKnee, r.BFoot, 0xFFD6A23A, 0xFFD6A23A, r.LegW * 0.45);
            Toes(dc, r.BFoot, 0xFFB98A2A);
            // wing behind
            Wing(dc, Shoulder(r), r.BHand, Gfx.Mix(d.Main, 0xFF000000, 0.25));
            // body: a tilted egg with a pale belly
            Point mid = new Point((r.Hip.X + r.Neck.X) / 2, (r.Hip.Y + r.Neck.Y) / 2);
            double ang = Math.Atan2(r.Neck.Y - r.Hip.Y, r.Neck.X - r.Hip.X) * 180 / Math.PI;
            dc.PushTransform(new RotateTransform(ang - 60, mid.X, mid.Y));
            dc.DrawEllipse(BrawlView.Sphere(d.Main), Outline(2.5), mid, r.TorsoLen * 1.05, r.BodyW * 0.6);
            dc.DrawEllipse(Gfx.B(d.Second), null, new Point(mid.X + r.TorsoLen * 0.2, mid.Y - r.BodyW * 0.2), r.TorsoLen * 0.6, r.BodyW * 0.3);
            for (int i = 0; i < 4; i++) dc.DrawLine(Gfx.P(0x80000000, 1.5), new Point(mid.X - r.TorsoLen * 0.5 + i * 8, mid.Y + 4), new Point(mid.X - r.TorsoLen * 0.4 + i * 8, mid.Y + 10));
            dc.Pop();
            Limb(dc, r.Hip, r.FKnee, r.FFoot, 0xFFF2B33D, 0xFFF2B33D, r.LegW * 0.45);
            Toes(dc, r.FFoot, 0xFFD6A23A);
            // neck up to the head
            dc.DrawLine(Outline(r.ArmW + 5), Off(r.Neck, 0, -2), r.Head);
            dc.DrawLine(Gfx.P(d.Main, r.ArmW), Off(r.Neck, 0, -2), r.Head);
            HeadOf(dc, f, r, p, t);
            Wing(dc, Shoulder(r), r.FHand, d.Main);
        }

        static void Toes(DrawingContext dc, Point p, uint c)
        {
            Pen pen = Gfx.P(c, 2.5);
            dc.DrawLine(pen, p, new Point(p.X + 9, p.Y - 1)); dc.DrawLine(pen, p, new Point(p.X + 5, p.Y - 3)); dc.DrawLine(pen, p, new Point(p.X - 7, p.Y - 1));
        }

        static void Wing(DrawingContext dc, Point s, Point tip, uint c)
        {
            double dx = tip.X - s.X, dy = tip.Y - s.Y, l = Math.Sqrt(dx * dx + dy * dy) + 0.01;
            double nx = -dy / l, ny = dx / l;
            Poly(dc, Gfx.B(c), Outline(2), s, new Point(tip.X + nx * 6, tip.Y + ny * 6), new Point(tip.X + dx / l * 6, tip.Y + dy / l * 6), new Point(tip.X - nx * 8, tip.Y - ny * 8), new Point(s.X - nx * 6, s.Y - ny * 6));
        }

        // ---- Saguaro Sam
        void DressSam(DrawingContext dc, BFighter f, Rig r, BPose p, double t)
        {
            BFighterDef d = f.D;
            CactusArm(dc, Shoulder(r), r.BElbow, r.BHand, Gfx.Mix(d.Main, 0xFF000000, 0.2), r.ArmW * 1.3);
            Limb(dc, r.Hip, r.BKnee, r.BFoot, d.Second, d.Second, r.LegW * 1.1);
            // the column
            Point a = r.Hip, b = r.Neck;
            double ang = Math.Atan2(b.Y - a.Y, b.X - a.X) * 180 / Math.PI - 90, len = Math.Sqrt((b.X - a.X) * (b.X - a.X) + (b.Y - a.Y) * (b.Y - a.Y));
            dc.PushTransform(new RotateTransform(ang, a.X, a.Y));
            double w = r.BodyW * 1.1;
            dc.DrawRoundedRectangle(BrawlView.Cylinder(d.Main), Outline(3), new Rect(a.X - w / 2, a.Y - w * 0.2, w, len + w * 0.4), w * 0.45, w * 0.45);
            for (int i = -1; i <= 1; i++) dc.DrawLine(Gfx.P(d.Second, 2.5), new Point(a.X + i * w * 0.25, a.Y), new Point(a.X + i * w * 0.25, a.Y + len));
            for (int i = 0; i < 8; i++) dc.DrawLine(Gfx.P(0xFFFFF7E0, 1.2), new Point(a.X + (i % 2 == 0 ? -w * 0.5 : w * 0.5), a.Y + len * i / 8), new Point(a.X + (i % 2 == 0 ? -w * 0.62 : w * 0.62), a.Y + len * i / 8 + 3));
            dc.Pop();
            Limb(dc, r.Hip, r.FKnee, r.FFoot, d.Main, d.Main, r.LegW * 1.1);
            HeadOf(dc, f, r, p, t);
            CactusArm(dc, Shoulder(r), r.FElbow, r.FHand, d.Main, r.ArmW * 1.3);
        }

        static void CactusArm(DrawingContext dc, Point s, Point e, Point h, uint c, double w)
        {
            Limb(dc, s, e, h, c, c, w);
            dc.DrawEllipse(Gfx.B(c), Outline(2), h, w * 0.6, w * 0.6);
            for (int i = 0; i < 3; i++) dc.DrawLine(Gfx.P(0xFFFFF7E0, 1.2), Off(h, i * 120, w * 0.55), Off(h, i * 120, w * 0.85));
        }

        // ---- extra pictures for some moves
        void ExtraBehind(DrawingContext dc, BFighter f, Rig r, string extra, bool active, double t)
        {
            switch (extra)
            {
                case "moose":
                    {
                        // Teddy rides a moose (drawn under him)
                        double y = 0, x = 0;
                        dc.DrawEllipse(BrawlView.Sphere(0xFF6B4423), Outline(3), new Point(x, y + 52), 58, 28);
                        foreach (double lx in new[] { -38.0, -18.0, 22.0, 40.0 }) { dc.DrawLine(Outline(11), new Point(x + lx, y + 40), new Point(x + lx + Math.Sin(t * 20 + lx) * 8, y)); dc.DrawLine(Gfx.P(0xFF4A2E17, 7), new Point(x + lx, y + 40), new Point(x + lx + Math.Sin(t * 20 + lx) * 8, y)); }
                        dc.DrawEllipse(Gfx.B(0xFF6B4423), Outline(3), new Point(x + 66, y + 66), 20, 16);
                        dc.DrawEllipse(Gfx.B(0xFF4A2E17), null, new Point(x + 82, y + 60), 9, 7);
                        foreach (int s in new[] { -1, 1 }) Poly(dc, Gfx.B(0xFFD6B98C), Outline(2), new Point(x + 60, y + 78), new Point(x + 50 + s * 6, y + 104), new Point(x + 62 + s * 14, y + 98), new Point(x + 72, y + 80));
                        dc.DrawEllipse(Gfx.B(0xFF111111), null, new Point(x + 72, y + 70), 2.5, 2.5);
                        break;
                    }
                case "ox":
                    if (f.MF < 22)
                    {
                        double a = 1 - f.MF / 22.0;
                        dc.PushOpacity(a);
                        dc.DrawEllipse(Gfx.B(0xFF3B82F6), Outline(3), new Point(-10, -20 - f.MF * 3), 60, 30);
                        dc.DrawEllipse(Gfx.B(0xFF3B82F6), Outline(3), new Point(50, -10 - f.MF * 3), 22, 18);
                        dc.Pop();
                    }
                    break;
                case "kite":
                    {
                        Point k = new Point(r.FHand.X + 60, r.FHand.Y + 120 + Math.Sin(t * 3) * 10);
                        dc.DrawLine(Gfx.P(0xFFE5E7EB, 1.2), r.FHand, k);
                        Poly(dc, Gfx.B(0xFFDC2626), Outline(2), new Point(k.X, k.Y + 26), new Point(k.X + 18, k.Y), new Point(k.X, k.Y - 30), new Point(k.X - 18, k.Y));
                        dc.DrawLine(Gfx.P(0xFFFDE047, 1.5), new Point(k.X, k.Y + 26), new Point(k.X, k.Y - 30));
                        dc.DrawEllipse(Gfx.B(0xFFD4AF37), null, new Point(r.FHand.X + 30, r.FHand.Y + 60), 4, 6);
                        break;
                    }
                case "jet":
                    if (active)
                        for (int i = 0; i < 3; i++)
                        {
                            double fl = 26 + 14 * Math.Abs(Math.Sin(t * 30 + i));
                            Poly(dc, Gfx.B(i == 0 ? 0xC0F97316 : 0xC0FDE047), null, new Point(-10 + i * 8, 6), new Point(-4 + i * 8, -fl * (1 - i * 0.25)), new Point(2 + i * 8, 6));
                        }
                    break;
                case "beam":
                    if (active) { dc.DrawRectangle(Gfx.B(0x5022D3EE), null, new Rect(-14, -400, 28, 400)); dc.DrawRectangle(Gfx.B(0xA0A5F3FC), null, new Rect(-5, -400, 10, 400)); }
                    break;
                case "rainbow":
                    if (active) { uint[] cs = { 0xC0EF4444, 0xC0F59E0B, 0xC0FDE047, 0xC022C55E, 0xC03B82F6, 0xC08B5CF6 }; for (int i = 0; i < cs.Length; i++) dc.DrawRectangle(Gfx.B(cs[i]), null, new Rect(-160, 20 + i * 7, 150, 7)); }
                    break;
                case "dust":
                    if (active) for (int i = 0; i < 6; i++) dc.DrawEllipse(Gfx.B(0x80E7D3B1), null, new Point(-30 - i * 22, 8 + (i % 2) * 10), 14 - i, 10 - i * 0.8);
                    break;
            }
        }

        void ExtraFront(DrawingContext dc, BFighter f, Rig r, string extra, bool active, double t)
        {
            switch (extra)
            {
                case "copter":
                    {
                        Point c = new Point(r.Head.X, r.Head.Y + r.HeadR * 1.5);
                        for (int i = 0; i < 4; i++) { double a = t * 1400 + i * 90; Point e = Off(c, a, 40); dc.DrawLine(Gfx.P(0xC0F472B6, 9), c, e); }
                        break;
                    }
                case "bubble":
                    if (active) { dc.DrawEllipse(Gfx.B(0x3093C5FD), Gfx.P(0xC0E0F2FE, 3), new Point(0, f.D.Height * 0.5), f.D.Height * 0.62, f.D.Height * 0.62); dc.DrawLine(Gfx.P(0x90FFFFFF, 3), Off(new Point(0, f.D.Height * 0.5), 120, f.D.Height * 0.48), Off(new Point(0, f.D.Height * 0.5), 150, f.D.Height * 0.48)); }
                    break;
                case "burst":
                    if (active) for (int i = 0; i < 10; i++) { double a = i * 36 + t * 100; Tendril(dc, new Point(0, 40), Off(new Point(0, 40), a, 60), 6, 0xFFC0262D, 0xFF4A0A10, t, i); }
                    break;
                case "spark":
                    if (active) for (int i = 0; i < 8; i++) { double a = i * 45 + t * 400; Point s = Off(new Point(0, 22), a, 20); Point e = Off(new Point(0, 22), a + 15, 52); dc.DrawLine(Gfx.P(0xFFFEF9C3, 2.5), s, e); }
                    break;
                default: SigFront(dc, f, r, extra, active, t); break;
                case "flower":
                    if (f.MF > 20)
                    {
                        Point c = new Point(0, f.D.Height * 1.05);
                        double k = Math.Min(1, (f.MF - 20) / 8.0);
                        for (int i = 0; i < 8; i++) dc.DrawEllipse(Gfx.B(0xFFFBCFE8), Outline(1.5), Off(c, i * 45, 22 * k), 14 * k, 14 * k);
                        dc.DrawEllipse(Gfx.B(0xFFFACC15), Outline(1.5), c, 12 * k, 12 * k);
                    }
                    break;
            }
        }

        // ================================================================ the damage panels
        public void DrawPanels(DrawingContext dc, BrawlSim sim, string[] names, double t)
        {
            int n = sim.F.Length;
            double w = 250, gap = 20, total = n * w + (n - 1) * gap, x0 = (W - total) / 2, y = H - 104;
            for (int i = 0; i < n; i++)
            {
                BFighter f = sim.F[i];
                double x = x0 + i * (w + gap);
                uint c = SlotColors[i % 4];
                bool gone = f.Out && (!sim.TimeMode && f.Stocks <= 0);
                dc.DrawRoundedRectangle(Gfx.B(gone ? 0xB0111111 : 0xD0101522), Gfx.P(Gfx.Alpha(c, gone ? 0.4 : 1), 3), new Rect(x, y, w, 88), 16, 16);
                dc.DrawRoundedRectangle(Gfx.B(Gfx.Alpha(c, 0.25)), null, new Rect(x + 6, y + 6, 76, 76), 12, 12);
                dc.PushOpacity(gone ? 0.4 : 1);
                Portrait(dc, f.D, x + 44, y + 44, 64, t, true, BS.Idle);
                dc.Pop();
                Gfx.Text(dc, names != null && i < names.Length ? names[i] : f.D.Name, x + 92, y + 8, 15, 0xFFE5E7EB, 0, Gfx.UiBold);
                if (gone) { Gfx.Text(dc, "Smashed!", x + 90, y + 36, 28, 0xFFF87171, 0, Gfx.UiBold); continue; }
                double d = f.Dmg;
                uint dc_ = d < 35 ? 0xFFFFFFFF : d < 70 ? Gfx.Mix(0xFFFFFFFF, 0xFFFDE047, (d - 35) / 35) : d < 120 ? Gfx.Mix(0xFFFDE047, 0xFFF97316, (d - 70) / 50) : Gfx.Mix(0xFFF97316, 0xFFDC2626, Math.Min(1, (d - 120) / 80));
                double jolt = Pop[i % 8] * 6;
                string pct = f.Out ? "" : f.Percent + "%";
                Gfx.Shadowed(dc, pct, x + 92 + (jolt > 0 ? Math.Sin(t * 90) * jolt : 0), y + 26 - jolt * 0.5, 40 + jolt, dc_, 0, Gfx.UiBold);
                if (sim.TimeMode)
                {
                    Gfx.Text(dc, (f.Score >= 0 ? "+" : "") + f.Score, x + w - 14, y + 58, 18, f.Score >= 0 ? 0xFF86EFAC : 0xFFFCA5A5, 1, Gfx.UiBold);
                }
                else
                {
                    for (int s = 0; s < f.Stocks && s < 8; s++) dc.DrawEllipse(Gfx.B(c), Gfx.P(0xFF000000, 1.5), new Point(x + w - 18 - s * 18, y + 72), 6.5, 6.5);
                }
            }
        }
    }
}
