// Classic Games - Sahuarita Smash!, part 7: the scenery in depth (2026-10-29, Andrew: "some 3d aspects, more texture and
// more polished graphics"). Platforms are boxes seen in one-point perspective: a front face with a tiled texture that
// moves with the camera, a top face receding to the vanishing point, and the side face that shows on the far side of
// the screen's middle, with ambient occlusion and a lit edge. The backgrounds are three layers drawn once into
// pictures (far, middle, near) that slide at different speeds (parallax) with haze between them, plus a few live
// things (blinking lights, bubbles, drifting dust) and light shafts. Textures are made in code (value noise), tiled.
// Fighters get their shading from BrawlArt (Shaded*); the whole picture ends with the stage's colour grade and a
// vignette.
using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace ClassicGames
{
    /// <summary>Tileable textures made in code, as brushes in world units (so they scale with the camera).</summary>
    public static class BrawlTex
    {
        const int N = 128;
        static readonly Dictionary<string, ImageBrush> cache = new Dictionary<string, ImageBrush>();

        static double Hash(int x, int y, int seed)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + seed * 2246822519);
                h = (h ^ (h >> 13)) * 1274126177u;
                return ((h ^ (h >> 16)) & 0xFFFFFF) / (double)0x1000000;
            }
        }
        /// <summary>Smooth value noise that tiles every N pixels; cells = how many bumps across.</summary>
        static double Noise(double x, double y, int cells, int seed)
        {
            double fx = x / N * cells, fy = y / N * cells;
            int x0 = (int)Math.Floor(fx), y0 = (int)Math.Floor(fy);
            double tx = fx - x0, ty = fy - y0;
            tx = tx * tx * (3 - 2 * tx); ty = ty * ty * (3 - 2 * ty);
            Func<int, int, double> v = (a, b) => Hash(((a % cells) + cells) % cells, ((b % cells) + cells) % cells, seed);
            double a0 = v(x0, y0) + (v(x0 + 1, y0) - v(x0, y0)) * tx, a1 = v(x0, y0 + 1) + (v(x0 + 1, y0 + 1) - v(x0, y0 + 1)) * tx;
            return a0 + (a1 - a0) * ty;
        }
        static double Fbm(double x, double y, int seed) { return Noise(x, y, 4, seed) * 0.5 + Noise(x, y, 8, seed + 1) * 0.3 + Noise(x, y, 16, seed + 2) * 0.2; }

        static uint Shade(uint c, double k)
        {
            k = Math.Max(0, k);
            Func<int, uint> ch = s => (uint)Math.Max(0, Math.Min(255, ((c >> s) & 255) * k));
            return 0xFF000000 | (ch(16) << 16) | (ch(8) << 8) | ch(0);
        }

        public static ImageBrush Tile(string kind, double worldSize)
        {
            string key = kind + "|" + worldSize;
            ImageBrush b;
            if (cache.TryGetValue(key, out b)) return b;
            b = new ImageBrush(Make(kind)) { TileMode = TileMode.Tile, DestinationRect = new RelativeRect(0, 0, worldSize, worldSize, RelativeUnit.Absolute), Stretch = Stretch.Fill };
            b.Freeze();
            cache[key] = b;
            return b;
        }

        public static Bitmap Make(string kind)
        {
            var px = new uint[N * N];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    double n = Fbm(x, y, kind.Length * 7), d = Noise(x, y, 32, 9);
                    uint c;
                    switch (kind)
                    {
                        case "sandstone":
                            {
                                // horizontal strata, wavy, with grit and the odd crack
                                double band = Math.Sin((y + Noise(x, y, 4, 3) * 18) * 0.21) * 0.5 + 0.5;
                                uint baseC = band > 0.66 ? 0xFFB4532A : band > 0.33 ? 0xFF9A4A2A : 0xFFC2693A;
                                c = Shade(baseC, 0.8 + n * 0.35 + (d - 0.5) * 0.12);
                                if (Noise(x, y, 16, 21) > 0.78 && (x + y) % 3 == 0) c = Shade(baseC, 0.55);
                                break;
                            }
                        case "sand": c = Shade(0xFFD9824B, 0.85 + n * 0.25 + (d - 0.5) * 0.18); if (Hash(x, y, 4) > 0.985) c = 0xFF7C2D12; break;
                        case "copperrock":
                            {
                                c = Shade(0xFF7C4A2D, 0.7 + n * 0.45 + (d - 0.5) * 0.1);
                                double vein = Math.Abs(Noise(x, y, 4, 11) - 0.5) + (1 - Noise(x, y, 8, 12)) * 0.05;
                                if (vein < 0.012) c = Shade(0xFF2DD4BF, 0.7 + n * 0.4);   // oxidised copper, green-blue
                                else if (vein < 0.022) c = Shade(0xFFB45309, 0.75 + n * 0.3);
                                break;
                            }
                        case "gravel": { double g = Noise(x, y, 24, 5); c = Shade(g > 0.6 ? 0xFFA8A29E : 0xFF8B6B4A, 0.7 + n * 0.4 + g * 0.2); break; }
                        case "metalpanel":
                            {
                                bool seam = x % 64 < 2 || y % 64 < 2;
                                bool rivet = (x % 64 - 6) * (x % 64 - 6) + (y % 64 - 6) * (y % 64 - 6) < 5 || (x % 64 - 58) * (x % 64 - 58) + (y % 64 - 6) * (y % 64 - 6) < 5;
                                bool vent = y % 64 > 40 && y % 64 < 54 && x % 64 > 14 && x % 64 < 50 && y % 4 < 2;
                                c = Shade(0xFF475569, 0.85 + (n - 0.5) * 0.2 + (x % 64) / 640.0);
                                if (seam) c = 0xFF1E293B; if (rivet) c = 0xFF94A3B8; if (vent) c = 0xFF0F172A;
                                break;
                            }
                        case "grate": { bool bar = x % 16 < 3 || y % 16 < 3; c = bar ? Shade(0xFF94A3B8, 0.85 + n * 0.2) : Shade(0xFF0F172A, 0.8 + n * 0.3); break; }
                        case "regolith":
                            {
                                c = Shade(0xFF71717A, 0.75 + n * 0.4 + (d - 0.5) * 0.15);
                                // small craters: a dark bowl with a lit rim
                                for (int k = 0; k < 5; k++)
                                {
                                    double cx = Hash(k, 1, 33) * N, cy = Hash(k, 2, 33) * N, r = 6 + Hash(k, 3, 33) * 12;
                                    double dx = Math.Min(Math.Abs(x - cx), N - Math.Abs(x - cx)), dy = Math.Min(Math.Abs(y - cy), N - Math.Abs(y - cy));
                                    double dist = Math.Sqrt(dx * dx + dy * dy);
                                    if (dist < r) c = Shade(0xFF52525B, 0.75 + (dist / r) * 0.3 + (y < cy ? 0.15 : -0.1));
                                    else if (dist < r + 2) c = Shade(0xFFA1A1AA, 0.95);
                                }
                                break;
                            }
                        case "dust": c = Shade(0xFFA1A1AA, 0.85 + n * 0.25 + (d - 0.5) * 0.1); break;
                        case "labwall":
                            {
                                bool seam = x % 32 < 1 || y % 64 < 1;
                                bool stripe = y % 64 > 52 && ((x + y) / 8) % 2 == 0;
                                c = Shade(0xFF52525B, 0.85 + (n - 0.5) * 0.25);
                                if (seam) c = 0xFF27272A; if (stripe) c = Shade(0xFFEAB308, 0.85 + n * 0.2);
                                if (y % 64 > 50 && y % 64 <= 52) c = 0xFF18181B;
                                break;
                            }
                        case "labfloor": { bool seam = x % 32 < 1 || y % 32 < 1; c = seam ? 0xFF18181B : Shade(0xFF3F3F46, 0.85 + n * 0.25); break; }
                        case "wood":
                            {
                                double grain = Math.Sin(x * 0.15 + Noise(x, y, 4, 8) * 12) * 0.5 + 0.5;
                                c = Shade(0xFF92400E, 0.75 + grain * 0.3 + n * 0.15);
                                if (x % 32 < 1) c = 0xFF451A03;
                                if ((x % 32 == 4 || x % 32 == 27) && (y % 32 == 6 || y % 32 == 26)) c = 0xFF57534E;   // nails
                                break;
                            }
                        case "bark":
                            {
                                // pecan bark: grey-brown, in long rough ridges
                                double ridge = Math.Abs(Math.Sin(x * 0.2 + Noise(x, y, 4, 17) * 9));
                                c = Shade(0xFF6B5B4B, 0.6 + ridge * 0.35 + n * 0.2);
                                if (ridge < 0.12) c = Shade(0xFF2E2620, 0.8 + n * 0.3);
                                break;
                            }
                        case "soil":
                            {
                                // the orchard's floor: dark flood-watered earth with fallen leaves and the odd pecan shell
                                c = Shade(0xFF5B3A24, 0.7 + n * 0.4 + (d - 0.5) * 0.15);
                                double leaf = Noise(x, y, 16, 23);
                                if (leaf > 0.74) c = Shade(Hash(x / 6, y / 6, 24) > 0.5 ? 0xFF9A7B2E : 0xFF6B7A2E, 0.8 + n * 0.3);
                                if (Hash(x, y, 25) > 0.993) c = 0xFF3B2414;
                                break;
                            }
                        case "washsand":
                            {
                                // the wash's floor: pale packed sand in rippled bands left by the last flood, with pebbles
                                double rip = Math.Sin((y + Noise(x, y, 4, 31) * 22) * 0.35);
                                c = Shade(0xFFC9A877, 0.82 + n * 0.3 + rip * 0.06);
                                if (rip > 0.92) c = Shade(0xFF9C7E55, 0.9 + n * 0.2);
                                double peb = Noise(x, y, 32, 33);
                                if (peb > 0.83) c = Shade(Hash(x / 5, y / 5, 34) > 0.5 ? 0xFF8A8178 : 0xFF6B5A48, 0.8 + n * 0.4);
                                break;
                            }
                        case "concrete":
                            {
                                // poured concrete: grey with flecks of gravel, the lines of the forms, a water stain low down
                                c = Shade(0xFFA8A29E, 0.82 + n * 0.25 + (d - 0.5) * 0.1);
                                if (Hash(x, y, 35) > 0.97) c = Shade(0xFF78716C, 0.9);
                                if (y % 48 < 2) c = Shade(0xFF78716C, 0.85);
                                if (y % 128 > 100) c = Shade(c, 0.85);
                                break;
                            }
                        case "cottonwood":
                            {
                                // an old cottonwood's bark: pale grey, cut deep with long criss-crossing furrows
                                double fur = Math.Abs(Math.Sin(y * 0.16 + Noise(x, y, 4, 36) * 7 + Math.Sin(x * 0.05) * 2));
                                c = Shade(0xFFA39A8E, 0.62 + fur * 0.38 + n * 0.18);
                                if (fur < 0.16) c = Shade(0xFF4A4038, 0.85 + n * 0.3);
                                break;
                            }
                        case "crystal": { double g = Noise(x, y, 8, 15); c = Shade(g > 0.55 ? 0xFFA5B4FC : 0xFF6366F1, 0.75 + n * 0.45); break; }
                        default: c = Shade(0xFF808080, 0.8 + n * 0.3); break;
                    }
                    px[y * N + x] = c;
                }
            var bmp = Pics.FromBgra(px, N, N);
            bmp.Freeze();
            return bmp;
        }
    }

    public sealed partial class BrawlView
    {
        public static Color C(uint c) { return Color.FromArgb((byte)(c >> 24), (byte)(c >> 16), (byte)(c >> 8), (byte)c); }

        static void Poly(DrawingContext dc, Brush b, Pen p, params Point[] pts)
        {
            var g = new StreamGeometry();
            using (var c = g.Open())
            {
                c.BeginFigure(pts[0], b != null, true);
                for (int i = 1; i < pts.Length; i++) c.LineTo(pts[i], true, true);
            }
            g.Freeze();
            dc.DrawGeometry(b, p, g);
        }

        // ---------------------------------------------------------------- brushes kept (frozen)
        static readonly Dictionary<string, Brush> brushes = new Dictionary<string, Brush>();
        static Brush Linear(uint a, uint b, double angle)
        {
            string k = "L" + a + "|" + b + "|" + angle;
            Brush br;
            if (brushes.TryGetValue(k, out br)) return br;
            var g = WpfBrush.Linear(C(a), C(b), angle); g.Freeze(); brushes[k] = br = g; return br;
        }
        static Brush Linear3(uint a, uint m, uint b, double angle)   // angle 90: top to bottom, 0: left to right
        {
            string k = "L3" + a + "|" + m + "|" + b + "|" + angle;
            Brush br;
            if (brushes.TryGetValue(k, out br)) return br;
            var g = new LinearGradientBrush { StartPoint = WpfBrush.Rel(0, 0), EndPoint = angle == 90 ? WpfBrush.Rel(0, 1) : WpfBrush.Rel(1, 0) };
            g.GradientStops.Add(new GradientStop(C(a), 0)); g.GradientStops.Add(new GradientStop(C(m), 0.5)); g.GradientStops.Add(new GradientStop(C(b), 1));
            g.Freeze(); brushes[k] = br = g; return br;
        }
        /// <summary>A ball lit from the top left: light, the colour, a darker edge.</summary>
        public static Brush Sphere(uint c)
        {
            string k = "S" + c;
            Brush br;
            if (brushes.TryGetValue(k, out br)) return br;
            var g = new RadialGradientBrush { GradientOrigin = WpfBrush.Rel(0.32, 0.68), Center = WpfBrush.Rel(0.42, 0.6), RadiusX = WpfBrush.Rel(0.7), RadiusY = WpfBrush.Rel(0.7) };
            g.GradientStops.Add(new GradientStop(C(Gfx.Mix(c, 0xFFFFFFFF, 0.35)), 0));
            g.GradientStops.Add(new GradientStop(C(c), 0.45));
            g.GradientStops.Add(new GradientStop(C(Gfx.Mix(c, 0xFF000000, 0.38)), 1));
            g.Freeze(); brushes[k] = br = g; return br;
        }
        /// <summary>A soft round glow, bright in the middle and gone at the edge.</summary>
        public static Brush Glow(uint c)
        {
            string k = "G" + c;
            Brush br;
            if (brushes.TryGetValue(k, out br)) return br;
            var g = new RadialGradientBrush();
            g.GradientStops.Add(new GradientStop(C(c), 0));
            g.GradientStops.Add(new GradientStop(C(Gfx.Alpha(c, ((c >> 24) / 255.0) * 0.35)), 0.45));
            g.GradientStops.Add(new GradientStop(C(c & 0x00FFFFFF), 1));
            g.Freeze(); brushes[k] = br = g; return br;
        }
        /// <summary>A body or a column lit from one side: dark edges, a soft highlight just off the middle.</summary>
        public static Brush Cylinder(uint c)
        {
            string k = "C" + c;
            Brush br;
            if (brushes.TryGetValue(k, out br)) return br;
            var g = new LinearGradientBrush { StartPoint = WpfBrush.Rel(0, 0), EndPoint = WpfBrush.Rel(1, 0) };
            g.GradientStops.Add(new GradientStop(C(Gfx.Mix(c, 0xFF000000, 0.35)), 0));
            g.GradientStops.Add(new GradientStop(C(Gfx.Mix(c, 0xFFFFFFFF, 0.14)), 0.38));
            g.GradientStops.Add(new GradientStop(C(c), 0.7));
            g.GradientStops.Add(new GradientStop(C(Gfx.Mix(c, 0xFF000000, 0.42)), 1));
            g.Freeze(); brushes[k] = br = g; return br;
        }
        static Brush vignette;
        static Brush Vignette()
        {
            if (vignette != null) return vignette;
            var g = new RadialGradientBrush { RadiusX = WpfBrush.Rel(0.75), RadiusY = WpfBrush.Rel(0.85) };
            g.GradientStops.Add(new GradientStop(Color.FromArgb(0, 0, 0, 0), 0.55));
            g.GradientStops.Add(new GradientStop(Color.FromArgb(120, 0, 0, 0), 1));
            g.Freeze(); return vignette = g;
        }

        // ---------------------------------------------------------------- the stage's light
        /// <summary>The light on the fighters' edges (rim) and over the whole picture (grade), per stage.</summary>
        public static uint RimFor(string stage)
        {
            switch (stage) { case "mesa": return 0xFFFFB25C; case "copper": return 0xFFFFE7A3; case "server": return 0xFF67E8F9; case "moon": return 0xFFC7D2FE; case "lab": return 0xFFFCA5A5; case "orchard": return 0xFFFFF1C2; case "wash": return 0xFFFFD9A8; }
            return 0xFFFFFFFF;
        }
        static uint GradeFor(string stage)
        {
            switch (stage) { case "mesa": return 0x18FF7A1A; case "copper": return 0x10FFD27A; case "server": return 0x1A0EA5E9; case "moon": return 0x184338CA; case "lab": return 0x16DC2626; case "orchard": return 0x10FDE68A; case "wash": return 0x14475569; }
            return 0;
        }

        // ---------------------------------------------------------------- the vanishing point
        /// <summary>Where every depth line meets: the middle of the screen, high up (the camera looks down a little).</summary>
        Point VP { get { return new Point(W / 2, (H - 120) * 0.12); } }
        /// <summary>A screen point pushed back into the picture by depth k (0 = at the front, 1 = at the vanishing point).</summary>
        Point Back(Point p, double k) { Point v = VP; return new Point(p.X + (v.X - p.X) * k, p.Y + (v.Y - p.Y) * k); }

        // ---------------------------------------------------------------- backgrounds: three layers drawn once
        string layerStage; int layerW, layerH;
        readonly IImage[] layers = new IImage[3];
        static readonly double[] Parallax = { 0.08, 0.22, 0.45 };
        const int LayerWidth = 2600;
        /// <summary>Extra picture above and below each layer, so moving the camera never shows its edge.</summary>
        const int LayerPad = 160;
        /// <summary>The ground continuing under a layer (the colour its bottom ends in).</summary>
        static Brush LayerFoot(BStageDef s, int layer)
        {
            switch (s.Id) { case "mesa": return Gfx.B(layer == 1 ? 0xFF7C2D12 : 0xFF6B2A0E); case "copper": return Gfx.B(0xFF1C1209); case "server": return Gfx.B(0xFF0F172A); case "moon": return Gfx.B(0xFF27272A); case "orchard": return Gfx.B(0xFF3B2A1A); case "wash": return Gfx.B(0xFF5A4630); default: return Gfx.B(0xFF18181B); }
        }

        void EnsureLayers(BStageDef s)
        {
            if (layerStage == s.Id && layerW == (int)W && layerH == (int)H) return;
            layerStage = s.Id; layerW = (int)W; layerH = (int)H;
            for (int i = 0; i < 3; i++)
            {
                var rtb = new RenderTargetBitmap(new PixelSize(LayerWidth, (int)H + LayerPad * 2));
                using (DrawingContext dc = new DrawingContext(rtb.CreateDrawingContext(), true))
                {
                    dc.PushTransform(new TranslateTransform(0, LayerPad));
                    // the far layer carries the sky (and the sun, the planet, the stars); the far two the haze in front of them
                    if (i == 0) { dc.DrawRectangle(Linear3(s.Sky2, Gfx.Mix(s.Sky1, s.Sky2, 0.5), s.Sky1, 90), null, new Rect(0, -LayerPad, LayerWidth, H + LayerPad * 2)); DrawSky(dc, s, 0, (LayerWidth - W) / 2, LayerWidth); }
                    DrawLayer(dc, s, i, LayerWidth, H);
                    if (i < 2) dc.DrawRectangle(Linear(Gfx.Alpha(s.Sky1, 0), Gfx.Alpha(s.Sky1, 0.22 - i * 0.07), 90), null, new Rect(0, H * 0.35, LayerWidth, H * 0.65));
                    dc.Pop(); if (i > 0) dc.DrawRectangle(LayerFoot(s, i), null, new Rect(0, H + LayerPad - 2, LayerWidth, LayerPad + 2)); }
                var old = layers[i] as IDisposable;   // the last stage's layer: its memory goes now, not whenever (Sauce Edition)
                layers[i] = rtb;
                if (old != null) old.Dispose();
                partPic[i * 2] = partPic[i * 2 + 1] = null;
            }
        }

        /// <summary>Windows draws without a graphics card here (RenderCapability tier below 2): leave out the middle
        /// background layer and the vignette, which cost the most then.</summary>
        public static bool Lite = (RenderCapability.Tier >> 16) < 2;
        readonly IImage[] partPic = new IImage[6];
        readonly long[] partKey = new long[6];
        /// <summary>A piece of a layer (kept while the camera holds still).</summary>
        IImage LayerPart(int layer, int x, int y, int w)
        {
            y = Math.Max(0, Math.Min(LayerPad * 2, y));
            w = Math.Max(1, Math.Min(LayerWidth - x, w));
            int slot = layer * 2 + (x == 0 ? 1 : 0);
            long key = (long)x * 100000000L + (long)y * 10000L + w;
            if (partPic[slot] != null && partKey[slot] == key) return partPic[slot];
            var c = new CroppedBitmap(layers[layer], new Int32Rect(x, y, w, (int)H));
            c.Freeze();
            partPic[slot] = c; partKey[slot] = key;
            return c;
        }

        public void DrawBackground(DrawingContext dc, BStageDef s, double t)
        {
            EnsureLayers(s);
            for (int i = 0; i < 3; i++)
            {
                if (Lite && i == 1) continue;
                double par = Parallax[i];
                double x = -((CamX * Zoom * par) % LayerWidth + LayerWidth) % LayerWidth - (LayerWidth - W) / 2;
                double y = (CamY - 150) * Zoom * par * 0.6;
                while (x > 0) x -= LayerWidth;
                // only the part on screen is drawn (a whole layer is twice the screen: drawing it all was most of a frame
                // when Windows draws without the graphics card); whole pixels, so nothing has to be resampled
                double srcY = Math.Round(LayerPad - y);
                for (double xx = Math.Round(x); xx < W; xx += LayerWidth)
                {
                    double from = Math.Max(0, -xx), to = Math.Min(LayerWidth, W - xx);
                    if (to <= from) continue;
                    dc.DrawImage(LayerPart(i, (int)from, (int)srcY, (int)(to - from)), new Rect(xx + from, 0, to - from, H));
                }
            }
            DrawLiveScenery(dc, s, t);
        }

        void DrawSky(DrawingContext dc, BStageDef s, double t, double ox, double span)
        {
            switch (s.Id)
            {
                case "mesa":
                    {
                        var sun = new Point(ox + W * 0.7, H * 0.3);
                        dc.DrawEllipse(Glow(0x90FDBA74), null, sun, 260, 260);
                        dc.DrawEllipse(Sphere(0xFFFDE68A), null, sun, 52, 52);
                        // light shafts from the sun, slowly turning
                        for (int i = 0; i < 6; i++)
                        {
                            double a = -2.6 + i * 0.32 + Math.Sin(t * 0.2 + i) * 0.03;
                            Poly(dc, Gfx.B(0x10FFF7ED), null, sun, new Point(sun.X + Math.Cos(a - 0.05) * 1500, sun.Y + Math.Sin(a - 0.05) * 1500 + 600), new Point(sun.X + Math.Cos(a + 0.05) * 1500, sun.Y + Math.Sin(a + 0.05) * 1500 + 600));
                        }
                        break;
                    }
                case "copper":
                    dc.DrawEllipse(Glow(0x70FFFFFF), null, new Point(ox + W * 0.25, H * 0.18), 200, 200);
                    for (int i = 0; i < span / 380; i++) { double x = i * 380 + 120; dc.DrawEllipse(Glow(0x80FFFFFF), null, new Point(x, 70 + (i % 4) * 22), 120, 30); }
                    break;
                case "server":
                    dc.DrawRectangle(Linear(0x00000000, 0x3022D3EE, 90), null, new Rect(0, 0, span, H * 0.5));
                    break;
                case "moon":
                    {
                        for (int i = 0; i < 320; i++)
                        {
                            double x = (i * 977.3) % span, y = (i * 571) % 560;
                            double tw = 0.5 + 0.5 * Math.Sin(t * (1 + i % 5) + i);
                            dc.DrawEllipse(Gfx.B(Gfx.Alpha(0xFFFFFFFF, 0.3 + 0.6 * tw * ((i % 4) / 3.0))), null, new Point(x, y), (i % 3) * 0.5 + 0.7, (i % 3) * 0.5 + 0.7);
                        }
                        dc.DrawEllipse(Glow(0x506D28D9), null, new Point(ox + W * 0.75, H * 0.2), 380, 160);
                        var pl = new Point(ox + W * 0.2, H * 0.24);
                        // the ring behind, the planet lit from the right, the ring in front
                        dc.DrawGeometry(null, Gfx.P(0x80C7D2FE, 7), RingHalf(pl, 160, 30, true));
                        dc.DrawEllipse(Glow(0x604F46E5), null, pl, 150, 150);
                        var pg = new RadialGradientBrush { GradientOrigin = WpfBrush.Rel(0.7, 0.35), Center = WpfBrush.Rel(0.6, 0.4), RadiusX = WpfBrush.Rel(0.75), RadiusY = WpfBrush.Rel(0.75) };
                        pg.GradientStops.Add(new GradientStop(C(0xFFA5B4FC), 0)); pg.GradientStops.Add(new GradientStop(C(0xFF4F46E5), 0.5)); pg.GradientStops.Add(new GradientStop(C(0xFF1E1B4B), 1));
                        dc.DrawEllipse(pg, null, pl, 92, 92);
                        for (int i = 0; i < 3; i++) dc.DrawEllipse(null, Gfx.P(0x30E0E7FF, 6), pl, 92 - i * 2, 18 + i * 12);
                        dc.DrawGeometry(null, Gfx.P(0xB0C7D2FE, 7), RingHalf(pl, 160, 30, false));
                        break;
                    }
                case "lab":
                    dc.DrawRectangle(Linear(0x00000000, 0x40450A0A, 90), null, new Rect(0, 0, span, H));
                    break;
                case "wash": WashSky(dc, ox, span); break;
                case "orchard":
                    {
                        // a clear morning: a soft sun up on the left, a few fair-weather clouds
                        var sun = new Point(ox + W * 0.22, H * 0.16);
                        dc.DrawEllipse(Glow(0x80FFF7D6), null, sun, 220, 220);
                        dc.DrawEllipse(Sphere(0xFFFFF7D6), null, sun, 40, 40);
                        for (int i = 0; i < span / 330; i++)
                        {
                            double x = i * 330 + 90 + (i % 3) * 40, y = 60 + (i % 4) * 26;
                            for (int k = 0; k < 4; k++) dc.DrawEllipse(Gfx.B(0xC0FFFFFF), null, new Point(x + k * 34, y - (k == 1 || k == 2 ? 14 : 0)), 34, 20);
                        }
                        break;
                    }
            }
        }

        static Geometry RingHalf(Point c, double rx, double ry, bool back)
        {
            var g = new StreamGeometry();
            using (var s = g.Open())
            {
                s.BeginFigure(new Point(c.X - rx, c.Y), false, false);
                s.ArcTo(new Point(c.X + rx, c.Y), new Size(rx, ry), 0, false, back ? SweepDirection.Clockwise : SweepDirection.CounterClockwise, true, true);
            }
            g.Freeze();
            return g;
        }

        /// <summary>One background layer (0 far, 1 middle, 2 near), drawn once into a picture LayerWidth wide.</summary>
        void DrawLayer(DrawingContext dc, BStageDef s, int layer, double w, double h)
        {
            var rng = new Random(s.Id.Length * 31 + layer * 7);
            double horizon = h * 0.62;
            switch (s.Id)
            {
                case "mesa":
                    if (layer == 0)
                    {
                        // far mesas in the haze, purple
                        for (double x = -100; x < w + 200; x += 260 + rng.Next(220))
                        {
                            double top = horizon - 60 - rng.Next(60), wd = 160 + rng.Next(200);
                            Mesa(dc, x, top, wd, horizon + 10, 0xFF6D3A6B, 0xFF4C2A55);
                        }
                        dc.DrawRectangle(Linear(0xFF5B2A4A, 0xFF7A3B3B, 90), null, new Rect(0, horizon + 8, w, h));
                    }
                    else if (layer == 1)
                    {
                        for (double x = -80; x < w + 200; x += 420 + rng.Next(300))
                        {
                            double top = horizon - 120 - rng.Next(80), wd = 220 + rng.Next(220);
                            Mesa(dc, x, top, wd, horizon + 40, 0xFFC2410C, 0xFF7C2D12);
                        }
                        dc.DrawRectangle(Linear(0xFF9A3412, 0xFF7C2D12, 90), null, new Rect(0, horizon + 38, w, h));
                        for (int i = 0; i < 26; i++) SaguaroShaded(dc, rng.NextDouble() * w, horizon + 46 + rng.Next(20), 0.45 + rng.NextDouble() * 0.25, 0xFF2F5D2B);
                    }
                    else
                    {
                        // near dunes and scrub
                        for (double x = -200; x < w + 200; x += 300)
                        {
                            var g = new StreamGeometry();
                            using (var c = g.Open())
                            {
                                c.BeginFigure(new Point(x, h), true, true);
                                c.BezierTo(new Point(x + 80, horizon + 70 - rng.Next(40)), new Point(x + 220, horizon + 60 - rng.Next(40)), new Point(x + 400, h), true, true);
                            }
                            g.Freeze();
                            dc.DrawGeometry(Linear(0xFFB45309, 0xFF6B2A0E, 90), null, g);
                        }
                        for (int i = 0; i < 18; i++) SaguaroShaded(dc, rng.NextDouble() * w, h - 40 - rng.Next(90), 0.9 + rng.NextDouble() * 0.5, 0xFF2F6B35);
                        for (int i = 0; i < 40; i++) { double x = rng.NextDouble() * w, y = h - 20 - rng.Next(120); dc.DrawEllipse(Sphere(0xFF57534E), null, new Point(x, y), 6 + rng.Next(10), 4 + rng.Next(6)); }
                    }
                    break;
                case "copper":
                    if (layer == 0)
                    {
                        for (double x = -100; x < w + 200; x += 300)
                            Poly(dc, Linear(0xFF94A3B8, 0xFF64748B, 90), null, new Point(x, horizon), new Point(x + 150 + rng.Next(60), horizon - 120 - rng.Next(80)), new Point(x + 320, horizon));
                        dc.DrawRectangle(Gfx.B(0xFF8B6B4A), null, new Rect(0, horizon - 2, w, h));
                    }
                    else if (layer == 1)
                    {
                        // the pit's terraces: each step a lit top and a shaded wall, stepping down toward us
                        for (int i = 0; i < 7; i++)
                        {
                            double y = horizon - 80 + i * 30;
                            uint lit = Gfx.Mix(0xFFD6A06A, 0xFFB87333, i / 7.0), wall = Gfx.Mix(0xFF8B5A2B, 0xFF5B3418, i / 7.0);
                            dc.DrawRectangle(Gfx.B(lit), null, new Rect(0, y, w, 8));
                            dc.DrawRectangle(Linear(wall, Gfx.Mix(wall, 0xFF000000, 0.35), 90), null, new Rect(0, y + 8, w, 22));
                            for (int k = 0; k < 40; k++) dc.DrawEllipse(Gfx.B(Gfx.Alpha(0xFF2DD4BF, 0.5)), null, new Point(rng.NextDouble() * w, y + 12 + rng.Next(14)), 2, 1.5);
                        }
                        dc.DrawRectangle(Linear(0xFF3F2410, 0xFF1C1209, 90), null, new Rect(0, horizon + 130, w, h));
                        // headframes (the towers over the shafts)
                        for (double x = 200; x < w; x += 700 + rng.Next(300)) Headframe(dc, x, horizon - 80);
                    }
                    else
                    {
                        // rails, ore heaps and a big haul truck near the camera
                        dc.DrawLine(Gfx.P(0xFF57534E, 4), new Point(0, h - 60), new Point(w, h - 60));
                        dc.DrawLine(Gfx.P(0xFF57534E, 4), new Point(0, h - 44), new Point(w, h - 44));
                        for (double x = 0; x < w; x += 30) dc.DrawRectangle(Gfx.B(0xFF44281A), null, new Rect(x, h - 62, 10, 22));
                        for (int i = 0; i < 8; i++)
                        {
                            double x = rng.NextDouble() * w, r = 70 + rng.Next(60);
                            dc.DrawGeometry(Linear(0xFFB87333, 0xFF5B3418, 90), null, Mound(x, h - 40, r, r * 0.55));
                        }
                        for (double x = 400; x < w; x += 1100) Truck(dc, x, h - 70);
                    }
                    break;
                case "server":
                    {
                        // rows of racks down a corridor toward the vanishing point
                        double vx = w / 2, vy = h * 0.3;
                        if (layer == 0)
                        {
                            dc.DrawRectangle(Gfx.B(0xFF0B1120), null, new Rect(0, 0, w, h));
                            for (int i = 0; i < 40; i++)
                            {
                                double x = i * (w / 40.0);
                                dc.DrawLine(Gfx.P(0x301E293B, 1), new Point(x, h), new Point(vx + (x - vx) * 0.1, vy));
                            }
                            for (int r = 0; r < 18; r++)
                            {
                                double x = r * 150 + 20;
                                dc.DrawRectangle(Linear(0xFF111827, 0xFF0B1120, 0), null, new Rect(x, horizon - 200, 100, 260));
                                for (int k = 0; k < 14; k++) dc.DrawEllipse(Gfx.B(rng.Next(3) == 0 ? 0x8022D3EE : 0x6034D399), null, new Point(x + 12 + rng.Next(70), horizon - 190 + k * 18), 1.5, 1.5);
                            }
                        }
                        else if (layer == 1)
                        {
                            for (int r = 0; r < 14; r++)
                            {
                                double x = r * 190 + 40;
                                Rack(dc, x, horizon - 300, 130, 360, rng);
                            }
                            dc.DrawRectangle(Linear(0xFF1E293B, 0xFF0F172A, 90), null, new Rect(0, horizon + 60, w, h));
                            for (int i = 0; i < 40; i++) dc.DrawLine(Gfx.P(0x40334155, 1.5), new Point(i * 70, h), new Point(vx + (i * 70 - vx) * 0.5, horizon + 60));
                        }
                        else
                        {
                            // cable trays and pipes close by
                            for (double x = 0; x < w; x += 640)
                            {
                                dc.DrawRectangle(Linear(0xFF334155, 0xFF0F172A, 90), Gfx.P(0xFF0B1120, 2), new Rect(x, 30, 520, 26));
                                for (int k = 0; k < 6; k++) dc.DrawLine(Gfx.P(new[] { 0xFF2563EB, 0xFFF59E0B, 0xFF16A34A }[k % 3], 4), new Point(x + 20, 40 + k * 2.5), new Point(x + 500, 42 + k * 2.5));
                            }
                            dc.DrawLine(Gfx.P(0xFF64748B, 22), new Point(0, h - 30), new Point(w, h - 30));
                            dc.DrawLine(Gfx.P(0x6094A3B8, 6), new Point(0, h - 37), new Point(w, h - 37));
                        }
                        break;
                    }
                case "moon":
                    if (layer == 0)
                    {
                        for (double x = -100; x < w + 200; x += 240) Poly(dc, Linear(0xFF3F3F46, 0xFF27272A, 90), null, new Point(x, horizon + 10), new Point(x + 100 + rng.Next(80), horizon - 70 - rng.Next(70)), new Point(x + 280, horizon + 10));
                        dc.DrawRectangle(Gfx.B(0xFF27272A), null, new Rect(0, horizon + 8, w, h));
                    }
                    else if (layer == 1)
                    {
                        var g = new StreamGeometry();
                        using (var c = g.Open())
                        {
                            c.BeginFigure(new Point(0, h), true, true);
                            for (double x = 0; x <= w; x += 160) c.LineTo(new Point(x, horizon + 40 + Math.Sin(x * 0.004) * 30 + rng.Next(20)), true, true);
                            c.LineTo(new Point(w, h), true, true);
                        }
                        g.Freeze();
                        dc.DrawGeometry(Linear(0xFF52525B, 0xFF27272A, 90), null, g);
                        for (int i = 0; i < 24; i++) Crater(dc, rng.NextDouble() * w, horizon + 80 + rng.Next(160), 20 + rng.Next(40));
                    }
                    else
                    {
                        for (int i = 0; i < 26; i++) { double x = rng.NextDouble() * w, y = h - 30 - rng.Next(100), r = 10 + rng.Next(24); dc.DrawEllipse(Sphere(0xFF71717A), null, new Point(x, y), r, r * 0.7); }
                        for (int i = 0; i < 6; i++) { double x = rng.NextDouble() * w; Crystal(dc, x, h - 40 - rng.Next(60), 0.8 + rng.NextDouble()); }
                    }
                    break;
                case "wash": WashLayer(dc, layer, w, h, horizon, rng); break;
                case "orchard":
                    if (layer == 0)
                    {
                        // the Santa Rita Mountains across the valley, blue in the distance
                        var g = new StreamGeometry();
                        using (var c = g.Open())
                        {
                            c.BeginFigure(new Point(0, horizon + 10), true, true);
                            for (double x = 0; x <= w; x += 40) c.LineTo(new Point(x, horizon - 70 - 60 * Math.Abs(Math.Sin(x * 0.0021)) - 30 * Math.Sin(x * 0.011 + 1) - rng.Next(14)), true, true);
                            c.LineTo(new Point(w, horizon + 10), true, true);
                        }
                        g.Freeze();
                        dc.DrawGeometry(Linear(0xFF7C8DB5, 0xFF8FA0BF, 90), null, g);
                        dc.DrawRectangle(Linear(0xFF9AA67A, 0xFF7E8A5A, 90), null, new Rect(0, horizon + 8, w, h));
                    }
                    else if (layer == 1)
                    {
                        // rows of pecan trees going back, small
                        dc.DrawRectangle(Linear(0xFF6B5134, 0xFF4E3A26, 90), null, new Rect(0, horizon + 30, w, h));
                        for (double x = 20; x < w; x += 110 + rng.Next(20)) PecanTree(dc, x, horizon + 36, 0.55 + rng.NextDouble() * 0.08, 0xFF4F7A3A);
                        for (double x = 70; x < w; x += 130 + rng.Next(20)) PecanTree(dc, x, horizon + 70, 0.7 + rng.NextDouble() * 0.08, 0xFF46703A);
                    }
                    else
                    {
                        // the near row, big, and the berms that hold the irrigation water
                        for (double x = -40; x < w + 100; x += 340 + rng.Next(80)) PecanTree(dc, x, h - 20, 1.25 + rng.NextDouble() * 0.2, 0xFF3F6B33);
                        for (int i = 0; i < 4; i++) dc.DrawLine(Gfx.P(0x40000000, 6), new Point(0, h - 120 + i * 34), new Point(w, h - 116 + i * 34));
                    }
                    break;
                case "lab":
                    if (layer == 0)
                    {
                        dc.DrawRectangle(BrawlTex.Tile("labwall", 96), null, new Rect(0, 0, w, h));
                        dc.DrawRectangle(Gfx.B(0xA0110E14), null, new Rect(0, 0, w, h));
                        for (double x = 0; x < w; x += 320) { dc.DrawRectangle(Linear(0xFF27272A, 0xFF18181B, 0), null, new Rect(x, 0, 26, h)); }
                    }
                    else if (layer == 1)
                    {
                        for (double x = 60; x < w; x += 260) Tank(dc, x, horizon - 230, 96, 250, rng);
                        dc.DrawRectangle(Linear(0xFF3F3F46, 0xFF18181B, 90), null, new Rect(0, horizon + 20, w, h));
                        for (int i = 0; i < 40; i++) dc.DrawLine(Gfx.P(0x3052525B, 1.5), new Point(i * 70, h), new Point(w / 2 + (i * 70 - w / 2) * 0.4, horizon + 20));
                    }
                    else
                    {
                        for (double x = 0; x < w; x += 900)
                        {
                            dc.DrawRectangle(Linear(0xFF52525B, 0xFF27272A, 90), Gfx.P(0xFF18181B, 2), new Rect(x + 100, h - 150, 220, 110));
                            for (int k = 0; k < 4; k++) dc.DrawRectangle(Gfx.B(new[] { 0xFF22C55E, 0xFFEF4444, 0xFF38BDF8, 0xFFFACC15 }[k]), null, new Rect(x + 120 + k * 46, h - 130, 30, 18));
                            dc.DrawRectangle(Linear(0xFF0C4A6E, 0xFF082F49, 90), Gfx.P(0xFF18181B, 2), new Rect(x + 130, h - 100, 160, 44));
                        }
                        dc.DrawLine(Gfx.P(0xFF71717A, 6), new Point(0, h - 170), new Point(w, h - 170));
                        for (double x = 0; x < w; x += 90) dc.DrawLine(Gfx.P(0xFF71717A, 4), new Point(x, h - 170), new Point(x, h));
                    }
                    break;
            }
        }

        /// <summary>A pecan tree standing on (x, y), size k: a tall grey trunk that forks, a wide rounded crown of
        /// clumps (lit from the upper left).</summary>
        static void PecanTree(DrawingContext dc, double x, double y, double k, uint leaf)
        {
            Pen trunk = Gfx.P(0xFF5B4B3B, 14 * k), limb = Gfx.P(0xFF5B4B3B, 8 * k);
            dc.DrawLine(trunk, new Point(x, y), new Point(x, y - 80 * k));
            dc.DrawLine(limb, new Point(x, y - 70 * k), new Point(x - 40 * k, y - 120 * k));
            dc.DrawLine(limb, new Point(x, y - 72 * k), new Point(x + 44 * k, y - 116 * k));
            uint dark = Gfx.Mix(leaf, 0xFF000000, 0.3), lite = Gfx.Mix(leaf, 0xFFFFFFFF, 0.18);
            double[] cx = { -60, -25, 15, 55, -40, 5, 40, -8 }, cy = { -120, -150, -155, -122, -175, -190, -170, -205 }, cr = { 38, 42, 44, 38, 36, 40, 34, 30 };
            for (int i = 0; i < cx.Length; i++) dc.DrawEllipse(Gfx.B(dark), null, new Point(x + cx[i] * k, y + cy[i] * k), cr[i] * k, cr[i] * 0.8 * k);
            for (int i = 0; i < cx.Length; i++) dc.DrawEllipse(Gfx.B(leaf), null, new Point(x + cx[i] * k - 4 * k, y + cy[i] * k - 4 * k), cr[i] * 0.82 * k, cr[i] * 0.64 * k);
            for (int i = 4; i < cx.Length; i++) dc.DrawEllipse(Gfx.B(lite), null, new Point(x + cx[i] * k - 10 * k, y + cy[i] * k - 10 * k), cr[i] * 0.4 * k, cr[i] * 0.3 * k);
        }

        static void Mesa(DrawingContext dc, double x, double top, double wd, double bottom, uint lit, uint shade)
        {
            // a flat-topped hill lit from the right: the lit face, the shadowed face, strata and a pale cap
            double slope = (bottom - top) * 0.35;
            Point a = new Point(x, bottom), b = new Point(x + slope, top), c = new Point(x + wd - slope, top), d = new Point(x + wd, bottom);
            Point mid = new Point(x + wd * 0.55, bottom);
            Poly(dc, Gfx.B(shade), null, a, b, new Point(x + wd * 0.45, top), mid);
            Poly(dc, Gfx.B(lit), null, new Point(x + wd * 0.45, top), c, d, mid);
            for (int i = 1; i < 4; i++)
            {
                double y = top + (bottom - top) * i / 4.0, inset = slope * (1 - i / 4.0);
                dc.DrawLine(Gfx.P(Gfx.Alpha(0xFF000000, 0.15), 2), new Point(x + inset + 4, y), new Point(x + wd - inset - 4, y));
            }
            dc.DrawRectangle(Gfx.B(Gfx.Mix(lit, 0xFFFFFFFF, 0.25)), null, new Rect(b.X, top - 4, c.X - b.X, 6));
        }

        static void SaguaroShaded(DrawingContext dc, double x, double y, double k, uint c)
        {
            uint dark = Gfx.Mix(c, 0xFF000000, 0.35), light = Gfx.Mix(c, 0xFFFFFFFF, 0.2);
            Action<Point, Point, double> arm = (p, q, w) => { dc.DrawLine(Gfx.P(dark, w), p, q); dc.DrawLine(Gfx.P(c, w * 0.7), new Point(p.X - w * 0.12, p.Y), new Point(q.X - w * 0.12, q.Y)); dc.DrawLine(Gfx.P(light, w * 0.2), new Point(p.X - w * 0.25, p.Y), new Point(q.X - w * 0.25, q.Y)); };
            arm(new Point(x, y), new Point(x, y - 110 * k), 18 * k);
            arm(new Point(x, y - 46 * k), new Point(x - 24 * k, y - 46 * k), 12 * k); arm(new Point(x - 24 * k, y - 46 * k), new Point(x - 24 * k, y - 80 * k), 12 * k);
            arm(new Point(x, y - 62 * k), new Point(x + 20 * k, y - 62 * k), 12 * k); arm(new Point(x + 20 * k, y - 62 * k), new Point(x + 20 * k, y - 92 * k), 12 * k);
        }

        static Geometry Mound(double x, double y, double rx, double ry)
        {
            var g = new StreamGeometry();
            using (var c = g.Open()) { c.BeginFigure(new Point(x - rx, y), true, true); c.BezierTo(new Point(x - rx * 0.5, y - ry * 1.3), new Point(x + rx * 0.5, y - ry * 1.3), new Point(x + rx, y), true, true); }
            g.Freeze();
            return g;
        }

        static void Headframe(DrawingContext dc, double x, double base_)
        {
            Pen p = Gfx.P(0xFF44403C, 6), q = Gfx.P(0xFF57534E, 3);
            dc.DrawLine(p, new Point(x, base_), new Point(x + 30, base_ - 170)); dc.DrawLine(p, new Point(x + 80, base_), new Point(x + 50, base_ - 170));
            for (int i = 1; i < 6; i++) dc.DrawLine(q, new Point(x + 5 * i, base_ - 30 * i), new Point(x + 80 - 5 * i, base_ - 30 * i));
            dc.DrawLine(p, new Point(x + 40, base_ - 170), new Point(x + 160, base_ - 40));
            dc.DrawEllipse(Gfx.B(0xFF1C1917), Gfx.P(0xFF78716C, 3), new Point(x + 40, base_ - 176), 16, 16);
        }

        static void Truck(DrawingContext dc, double x, double y)
        {
            dc.DrawRoundedRectangle(Linear(0xFFFACC15, 0xFFB45309, 90), Gfx.P(0xFF1C1917, 3), new Rect(x, y - 120, 260, 80), 8, 8);
            dc.DrawRectangle(Linear(0xFFFDE047, 0xFFCA8A04, 90), Gfx.P(0xFF1C1917, 3), new Rect(x + 200, y - 170, 80, 60));
            dc.DrawRectangle(Gfx.B(0xFF7DD3FC), null, new Rect(x + 215, y - 160, 50, 26));
            foreach (double wx in new[] { x + 50, x + 210 }) { dc.DrawEllipse(Sphere(0xFF27272A), null, new Point(wx, y - 30), 42, 42); dc.DrawEllipse(Gfx.B(0xFF78716C), null, new Point(wx, y - 30), 14, 14); }
        }

        static void Rack(DrawingContext dc, double x, double y, double w, double h, Random rng)
        {
            dc.DrawRectangle(Linear(0xFF1F2937, 0xFF111827, 0), Gfx.P(0xFF0B1120, 2), new Rect(x, y, w, h));
            dc.DrawRectangle(Linear(0xFF334155, 0xFF1F2937, 0), null, new Rect(x + w, y + 10, 18, h - 10));   // the side, in depth
            for (int r = 0; r < 14; r++)
            {
                double yy = y + 10 + r * 24;
                dc.DrawRectangle(Linear(0xFF334155, 0xFF1E293B, 90), null, new Rect(x + 8, yy, w - 16, 18));
                for (int k = 0; k < 4; k++) dc.DrawEllipse(Gfx.B(rng.Next(4) == 0 ? 0xFFF97316 : rng.Next(2) == 0 ? 0xFF22D3EE : 0xFF4ADE80), null, new Point(x + 16 + k * 8, yy + 9), 2, 2);
                dc.DrawRectangle(Gfx.B(0xFF0F172A), null, new Rect(x + w - 50, yy + 5, 36, 8));
            }
        }

        static void Crater(DrawingContext dc, double x, double y, double r)
        {
            dc.DrawEllipse(Gfx.B(0xFF3F3F46), null, new Point(x, y), r, r * 0.35);
            dc.DrawEllipse(Linear(0xFF18181B, 0xFF52525B, 90), null, new Point(x, y + r * 0.05), r * 0.8, r * 0.26);
            dc.DrawGeometry(null, Gfx.P(0xFFA1A1AA, 2), RingHalf(new Point(x, y), r, r * 0.35, false));
        }

        static void Crystal(DrawingContext dc, double x, double y, double k)
        {
            dc.DrawEllipse(Glow(0x70818CF8), null, new Point(x, y - 20 * k), 60 * k, 60 * k);
            for (int i = -1; i <= 1; i++)
            {
                double h = (50 - Math.Abs(i) * 18) * k, cx = x + i * 14 * k;
                Poly(dc, Linear(0xFFC7D2FE, 0xFF6366F1, 0), Gfx.P(0xFF312E81, 1.5), new Point(cx - 8 * k, y), new Point(cx - 6 * k, y - h), new Point(cx, y - h - 10 * k), new Point(cx + 6 * k, y - h), new Point(cx + 8 * k, y));
            }
        }

        static void Tank(DrawingContext dc, double x, double y, double w, double h, Random rng)
        {
            dc.DrawRectangle(Linear(0xFF71717A, 0xFF3F3F46, 0), Gfx.P(0xFF18181B, 2), new Rect(x - 8, y - 20, w + 16, 24));
            dc.DrawRectangle(Linear(0xFF71717A, 0xFF3F3F46, 0), Gfx.P(0xFF18181B, 2), new Rect(x - 8, y + h - 4, w + 16, 28));
            // glass: the liquid glows, the glass has a bright streak
            dc.DrawRoundedRectangle(Linear3(0xA0155E75, 0xC022D3EE, 0xA0155E75, 0), Gfx.P(0xFF52525B, 3), new Rect(x, y, w, h), 10, 10);
            dc.DrawEllipse(Glow(0x6067E8F9), null, new Point(x + w / 2, y + h * 0.55), w, h * 0.5);
            dc.DrawRectangle(Gfx.B(0x50FFFFFF), null, new Rect(x + w * 0.15, y + 10, w * 0.08, h - 20));
            dc.DrawRectangle(Gfx.B(0x25FFFFFF), null, new Rect(x + w * 0.28, y + 10, w * 0.04, h - 20));
            dc.DrawLine(Gfx.P(0xFF52525B, 8), new Point(x + w / 2, y - 20), new Point(x + w / 2, 0));
        }

        /// <summary>Things that move in the background (drawn every frame, small and cheap).</summary>
        void DrawLiveScenery(DrawingContext dc, BStageDef s, double t)
        {
            switch (s.Id)
            {
                case "mesa":
                    for (int i = 0; i < 24; i++)
                    {
                        // dust motes drifting in the sunlight
                        double x = ((i * 157.3 + t * (12 + i % 5 * 4)) % (W + 40)) - 20, y = (i * 83.7 + Math.Sin(t * 0.7 + i) * 20) % (H - 140);
                        dc.DrawEllipse(Gfx.B(0x50FFF7ED), null, new Point(x, y), 1.5, 1.5);
                    }
                    break;
                case "wash": WashLive(dc, t); break;
                case "orchard":
                    for (int i = 0; i < 16; i++)
                    {
                        // leaves drifting down and across, turning as they fall
                        double x = ((i * 173.1 + t * (20 + i % 4 * 6)) % (W + 40)) - 20, y = (i * 61.3 + t * (26 + i % 3 * 8)) % (H - 120);
                        double a = t * (1.5 + i % 3) + i;
                        dc.DrawEllipse(Gfx.B(i % 3 == 0 ? 0xC0A3842E : 0xC06B8E3A), null, new Point(x + Math.Sin(a) * 8, y), 5 * Math.Abs(Math.Cos(a)) + 1.5, 2.5);
                    }
                    break;
                case "server":
                    for (int i = 0; i < 30; i++)
                    {
                        double par = Parallax[1];
                        double x = ((i * 211.0 - CamX * Zoom * par) % W + W) % W, y = H * 0.25 + (i * 53) % 260 + (CamY - 150) * Zoom * par * 0.6;
                        if (Math.Sin(t * (3 + i % 4) + i * 1.7) > 0.3) dc.DrawEllipse(Glow(i % 3 == 0 ? 0xFFF97316 : 0xFF22D3EE), null, new Point(x, y), 6, 6);
                    }
                    break;
                case "lab":
                    for (int i = 0; i < 18; i++)
                    {
                        // bubbles rising in the tanks
                        double par = Parallax[1];
                        double tankX = 60 + (i % 10) * 260 + 48 - (LayerWidth - W) / 2 - CamX * Zoom * par;
                        tankX = ((tankX % LayerWidth) + LayerWidth) % LayerWidth - (LayerWidth - W) / 2;
                        double y = H * 0.62 + 10 - ((t * (30 + i * 3) + i * 37) % 230) + (CamY - 150) * Zoom * par * 0.6;
                        dc.DrawEllipse(Gfx.B(0x80E0FFFF), Gfx.P(0x40FFFFFF, 1), new Point(tankX + Math.Sin(t * 2 + i) * 8, y), 3 + i % 3, 3 + i % 3);
                    }
                    break;
                case "moon":
                    {
                        for (int i = 0; i < 14; i++) { double tw = Math.Sin(t * (1.3 + i % 4) + i * 2.1); if (tw > 0.4) Star(dc, (i * 197.3) % W, (i * 83.1) % (H * 0.5), 4 * tw, 1.2, 0, 0xE0FFFFFF, 0x00FFFFFF); }
                        double k = (t * 0.07) % 1;
                        double x = W * (1.2 - k * 1.6), y = 80 + k * 120;
                        if (k < 0.6) { dc.DrawLine(Gfx.P(0x70FFFFFF, 2), new Point(x, y), new Point(x + 90, y - 30)); dc.DrawEllipse(Glow(0xC0FFFFFF), null, new Point(x, y), 5, 5); }
                        break;
                    }
            }
        }

        // ---------------------------------------------------------------- platforms as boxes in perspective
        static string FrontTex(string stage) { switch (stage) { case "mesa": return "sandstone"; case "copper": return "copperrock"; case "server": return "metalpanel"; case "moon": return "regolith"; case "orchard": return "soil"; case "wash": return "washsand"; default: return "labwall"; } }

        void DrawPlatforms(DrawingContext dc, BrawlSim sim, double t)
        {
            BStageDef s = sim.Stage;
            // the world as the camera sees it, for textures that move and scale with it
            var world = new Matrix(Zoom, 0, 0, -Zoom, W / 2 - CamX * Zoom, (H - 120) * 0.56 + CamY * Zoom);
            // the far ones first
            var order = new List<BPlat>(sim.Plats);
            order.Sort((a, b) => a.Solid != b.Solid ? (a.Solid ? 1 : -1) : b.Top.CompareTo(a.Top));
            foreach (BPlat p in order)
            {
                double x1 = SX(p.L), x2 = SX(p.R), y = SY(p.Top);
                bool solid = p.Solid;
                double depth = solid ? 0.1 : 0.06;
                double thick = solid ? p.Thick : SoftThick(p.Look);
                double yb = SY(p.Top - thick);
                Point fl = new Point(x1, y), fr = new Point(x2, y), bl = Back(fl, depth), br = Back(fr, depth);
                Point flb = new Point(solid ? x1 + 30 * Zoom : x1, yb), frb = new Point(solid ? x2 - 30 * Zoom : x2, yb);
                uint softTop = LookTop(p.Look, s.Id);
                uint top = solid ? s.GroundTop : softTop, side = Gfx.Mix(solid ? s.Ground : softTop, 0xFF000000, 0.45);
                if (!solid) DrawUnder(dc, p, sim, x1, x2, y, yb, t);
                if (p.Look == "saguaro") continue;
                // the side that shows: the left side when the box is right of the middle, the right side when left
                if (fl.X > VP.X) Poly(dc, Linear(side, Gfx.Mix(side, 0xFF000000, 0.4), 0), null, fl, bl, Back(flb, depth), flb);
                if (fr.X < VP.X) Poly(dc, Linear(Gfx.Mix(side, 0xFF000000, 0.4), side, 0), null, fr, br, Back(frb, depth), frb);
                // the top, receding: lighter at the front edge, with lines going back
                Poly(dc, Linear(Gfx.Mix(top, 0xFF000000, 0.25), Gfx.Mix(top, 0xFFFFFFFF, 0.15), 90), null, bl, br, fr, fl);
                int lines = solid ? 14 : 5;
                for (int i = 1; i < lines; i++)
                {
                    double k = i / (double)lines;
                    Point a = new Point(fl.X + (fr.X - fl.X) * k, fl.Y), b = new Point(bl.X + (br.X - bl.X) * k, bl.Y);
                    dc.DrawLine(Gfx.P(Gfx.Alpha(0xFF000000, 0.12), 1), a, b);
                }
                if (s.Id == "mesa" && solid) for (int i = 0; i < 8; i++) { double k = (i * 0.37 + 0.1) % 1; Point a = new Point(fl.X + (fr.X - fl.X) * k, fl.Y), b = new Point(bl.X + (br.X - bl.X) * k, bl.Y); dc.DrawEllipse(Gfx.B(0xFF7C2D12), null, new Point((a.X + b.X) / 2, (a.Y + b.Y) / 2), 3 * Zoom, 1.6 * Zoom); }
                // the front face: the stage's texture, darker toward the bottom (ambient occlusion), a lit edge on top
                var front = new StreamGeometry();
                using (var c = front.Open()) { c.BeginFigure(fl, true, true); c.LineTo(fr, true, true); c.LineTo(frb, true, true); c.LineTo(flb, true, true); }
                front.Freeze();
                dc.PushClip(front);
                dc.PushTransform(new MatrixTransform(world));
                Brush tex = solid ? BrawlTex.Tile(FrontTex(s.Id), 110) : BrawlTex.Tile(LookTex(p.Look, s.Id), 64);
                dc.DrawRectangle(tex, null, new Rect(p.L - 40, p.Top - thick - 10, p.R - p.L + 80, thick + 12));
                dc.Pop();
                dc.DrawRectangle(Linear(0x00000000, 0xA0000000, 90), null, new Rect(x1, y, x2 - x1, yb - y));
                dc.Pop();
                dc.DrawGeometry(null, Gfx.P(Gfx.Alpha(0xFF000000, 0.55), 2), front);
                dc.DrawLine(Gfx.P(Gfx.Mix(top, 0xFFFFFFFF, 0.45), Math.Max(1.5, 3 * Zoom)), fl, fr);
                if (s.Id == "server" && !solid) { double glow = 0.5 + 0.5 * Math.Sin(t * 2); dc.DrawLine(Gfx.P(Gfx.Alpha(0xFF22D3EE, 0.4 + 0.4 * glow), 3), new Point(fl.X, yb), new Point(fr.X, yb)); }
                if (p.Look == "crystal") dc.DrawRectangle(Glow(0x60818CF8), null, new Rect(x1 - 20, yb - 10, x2 - x1 + 40, 40));
                if (!solid) DrawOver(dc, p, fl, fr, bl, br, yb, t);
                // a strip of light along the front edge of the lab's and the server room's floors
                if (solid && (s.Id == "lab" || s.Id == "server")) dc.DrawLine(Gfx.P(Gfx.Alpha(s.Id == "lab" ? 0xFFEF4444 : 0xFF22D3EE, 0.5 + 0.3 * Math.Sin(t * 1.5)), 3 * Zoom), new Point(x1, y + 14 * Zoom), new Point(x2, y + 14 * Zoom));
            }
        }

        // ---------------------------------------------------------------- each stage's own platforms (BPlat.Look)
        static string LookTex(string look, string stage)
        {
            switch (look)
            {
                case "ledge": case "arch": case "saguaro": return "sandstone";
                case "terrace": return "copperrock";
                case "lift": return "wood";
                case "bucket": case "rack": case "tank": case "gantry": return "metalpanel";
                case "tray": case "catwalk": return "grate";
                case "rock": return "regolith";
                case "crystal": return "crystal";
                case "branch": return "bark";
                case "bins": return "wood";
                case "picker": return "metalpanel";
                case "log": return "cottonwood";
                case "bridge": return "wood";
                case "culvert": return "concrete";
                default: return stage == "moon" ? "crystal" : stage == "mesa" || stage == "copper" ? "wood" : "grate";
            }
        }

        static uint LookTop(string look, string stage)
        {
            switch (look)
            {
                case "ledge": case "arch": return 0xFFD9824B;
                case "terrace": return 0xFFB87333;
                case "lift": return 0xFFC08040;
                case "bucket": return 0xFF8B5E3C;
                case "rack": return 0xFF475569;
                case "tray": case "tank": return 0xFF94A3B8;
                case "rock": return 0xFFA1A1AA;
                case "crystal": return 0xFFA5B4FC;
                case "catwalk": return 0xFF71717A;
                case "gantry": return 0xFFFACC15;
                case "branch": return 0xFF8B7355;
                case "bins": return 0xFFB7834A;
                case "picker": return 0xFFEAB308;
                case "log": return 0xFFB5ADA2;
                case "bridge": return 0xFF9C6B3E;
                case "culvert": return 0xFFBDB6AA;
                default: switch (stage) { case "mesa": return 0xFFC08040; case "copper": return 0xFFB87333; case "server": return 0xFF94A3B8; case "moon": return 0xFFA5B4FC; default: return 0xFF71717A; }
            }
        }

        static double SoftThick(string look)
        {
            switch (look) { case "ledge": case "arch": return 24; case "terrace": return 20; case "rock": return 18; case "rack": return 10; case "tank": return 12; case "branch": return 18; case "bins": return 22; case "log": return 26; case "bridge": return 12; case "culvert": return 20; default: return 16; }
        }

        /// <summary>What holds a platform up (or hangs it down), drawn behind it: hoodoo columns and a stone arch, a saguaro,
        /// a terrace's rock face, the lift's and the bucket's cables, a server rack, the tray's rods, a drifting rock's
        /// jagged underside, crystals, a glass specimen tank, the catwalk's rods, the crane's rail and trolley.</summary>
        void DrawUnder(DrawingContext dc, BPlat p, BrawlSim sim, double x1, double x2, double y, double yb, double t)
        {
            double z = Zoom, g = SY(sim.Main.Top), w = x2 - x1, cx = (x1 + x2) / 2;
            Pen edge = Gfx.P(Gfx.Alpha(0xFF000000, 0.5), Math.Max(1, 1.5 * z));
            switch (p.Look)
            {
                case "log": case "bridge": case "culvert": WashUnder(dc, p, x1, x2, y, yb, g, z, edge, t); break;
                case "ledge":
                    {
                        // a hoodoo: a rock column, pinched in the middle, a little darker than the ledge on top
                        double topW = w * 0.34, midW = w * 0.2, botW = w * 0.3, mid = (yb + g) / 2;
                        var geo = new StreamGeometry();
                        using (var c = geo.Open())
                        {
                            c.BeginFigure(new Point(cx - topW, yb), true, true);
                            c.BezierTo(new Point(cx - midW * 0.7, mid - 20 * z), new Point(cx - midW * 1.2, mid + 20 * z), new Point(cx - botW, g), true, true);
                            c.LineTo(new Point(cx + botW, g), true, true);
                            c.BezierTo(new Point(cx + midW * 1.2, mid + 20 * z), new Point(cx + midW * 0.7, mid - 20 * z), new Point(cx + topW, yb), true, true);
                        }
                        geo.Freeze();
                        dc.DrawGeometry(Linear(0xFF9A4A2A, 0xFF5C2A17, 0), edge, geo);
                        for (int i = 1; i < 5; i++) { double yy = yb + (g - yb) * i / 5.0; dc.DrawLine(Gfx.P(Gfx.Alpha(0xFF3B1A0E, 0.35), 2 * z), new Point(cx - midW * 1.1, yy), new Point(cx + midW * 1.1, yy + 3 * z)); }
                        break;
                    }
                case "arch":
                    {
                        // a natural stone arch: two legs down to the mesa with a rounded opening between them
                        double leg = w * 0.2, foot = w * 0.12;
                        var geo = new StreamGeometry();
                        using (var c = geo.Open())
                        {
                            c.BeginFigure(new Point(x1 + 4 * z, yb), true, true);
                            c.LineTo(new Point(x2 - 4 * z, yb), true, true);
                            c.LineTo(new Point(x2 + foot, g), true, true);
                            c.LineTo(new Point(x2 - leg, g), true, true);
                            c.ArcTo(new Point(x1 + leg, g), new Size(w / 2 - leg, (g - yb) * 0.78), 0, false, SweepDirection.CounterClockwise, true, true);
                            c.LineTo(new Point(x1 - foot, g), true, true);
                        }
                        geo.Freeze();
                        dc.DrawGeometry(Linear3(0xFF7C3A1E, 0xFFA8522E, 0xFF6B3018, 0), edge, geo);
                        break;
                    }
                case "branch":
                    {
                        // a pecan tree: the crown spreading behind and above, the trunk down to the orchard floor
                        double tx = cx + w * 0.18 * (cx < W / 2 ? 1 : -1);
                        for (int i = 0; i < 7; i++)
                        {
                            double ex = cx + (i - 3) * w * 0.26, ey = y - (40 + (i % 3) * 34 + (i == 3 ? 40 : 0)) * z, er = (52 + (i % 2) * 14) * z;
                            dc.DrawEllipse(Gfx.B(0xFF2F5228), null, new Point(ex, ey), er, er * 0.78);
                            dc.DrawEllipse(Gfx.B(0xFF3F6B33), null, new Point(ex - 6 * z, ey - 6 * z), er * 0.8, er * 0.6);
                        }
                        var tg = new StreamGeometry();
                        using (var c = tg.Open())
                        {
                            c.BeginFigure(new Point(tx - 16 * z, g), true, true);
                            c.BezierTo(new Point(tx - 12 * z, (g + yb) / 2), new Point(tx - 10 * z, yb + 10 * z), new Point(tx - 14 * z, yb), true, true);
                            c.LineTo(new Point(tx + 14 * z, yb), true, true);
                            c.BezierTo(new Point(tx + 10 * z, yb + 10 * z), new Point(tx + 12 * z, (g + yb) / 2), new Point(tx + 18 * z, g), true, true);
                        }
                        tg.Freeze();
                        dc.DrawGeometry(Linear(0xFF6B5B4B, 0xFF3F342A, 0), edge, tg);
                        for (int i = 1; i < 5; i++) { double yy = yb + (g - yb) * i / 5.0; dc.DrawLine(Gfx.P(0x40000000, 1.5 * z), new Point(tx - 8 * z, yy), new Point(tx - 4 * z, yy + 14 * z)); }
                        break;
                    }
                case "bins":
                    {
                        // wooden harvest bins stacked up to the top one, full of pecans
                        int n = Math.Max(1, (int)Math.Round((g - yb) / (46 * z)));
                        double bh = (g - yb) / n;
                        for (int i = 0; i < n; i++)
                        {
                            double by = yb + i * bh;
                            dc.DrawRectangle(Linear(0xFFB7834A, 0xFF7C5228, 90), edge, new Rect(x1 + 4 * z, by, w - 8 * z, bh - 2 * z));
                            for (int k = 1; k < 4; k++) dc.DrawLine(Gfx.P(0x40000000, 1.5 * z), new Point(x1 + 4 * z, by + bh * k / 4), new Point(x2 - 4 * z, by + bh * k / 4));
                        }
                        for (int i = 0; i < 14; i++) dc.DrawEllipse(Gfx.B(i % 2 == 0 ? 0xFF7C4A1E : 0xFF92582A), Gfx.P(0x80000000, 1), new Point(x1 + 12 * z + (i * 37 % (int)Math.Max(1, w - 24 * z)), y - 3 * z - (i % 3) * 2 * z), 6 * z, 4 * z);
                        break;
                    }
                case "picker":
                    {
                        // the picker: a yellow mast from a little cart on the ground up to the basket
                        dc.DrawRectangle(Linear(0xFFCA8A04, 0xFF854D0E, 0), edge, new Rect(cx - 8 * z, yb, 16 * z, Math.Max(0, g - yb - 20 * z)));
                        dc.DrawRoundedRectangle(Gfx.B(0xFFEAB308), edge, new Rect(cx - 40 * z, g - 30 * z, 80 * z, 22 * z), 5 * z, 5 * z);
                        foreach (int side in new[] { -1, 1 }) dc.DrawEllipse(Gfx.B(0xFF1C1917), null, new Point(cx + side * 26 * z, g - 6 * z), 9 * z, 9 * z);
                        break;
                    }
                case "saguaro":
                    {
                        // a tall saguaro: stand on its two arms (the platform is where the arms bend up)
                        double tw = 13 * z, aw = 9 * z, ay = y + aw * 0.6;
                        Brush skin = Cylinder(0xFF2F6B35);
                        Pen rib = Gfx.P(Gfx.Alpha(0xFF14361A, 0.55), 1.2 * z), outline = Gfx.P(0xFF14361A, Math.Max(1, 2 * z));
                        foreach (int side in new[] { -1, 1 })
                        {
                            double ex = side < 0 ? x1 + aw : x2 - aw;
                            var arm = new StreamGeometry();
                            using (var c = arm.Open())
                            {
                                c.BeginFigure(new Point(cx, ay + 16 * z), false, false);
                                c.LineTo(new Point(ex - side * 8 * z, ay + 16 * z), true, true);
                                c.QuadraticBezierTo(new Point(ex, ay + 16 * z), new Point(ex, ay), true, true);
                                c.LineTo(new Point(ex, y - 34 * z), true, true);
                            }
                            arm.Freeze();
                            dc.DrawGeometry(null, RoundPen(0xFF14361A, aw * 2 + 3 * z), arm);
                            dc.DrawGeometry(null, RoundPen(0xFF2F6B35, aw * 2), arm);
                            dc.DrawLine(rib, new Point(ex, ay - 2 * z), new Point(ex, y - 32 * z));
                        }
                        dc.DrawRoundedRectangle(skin, outline, new Rect(cx - tw, y - 70 * z, tw * 2, g - y + 70 * z), tw, tw);
                        for (int i = -1; i <= 1; i++) dc.DrawLine(rib, new Point(cx + i * tw * 0.5, y - 62 * z), new Point(cx + i * tw * 0.5, g - 4 * z));
                        // the flat of the arms you stand on, a little lighter
                        dc.DrawLine(Gfx.P(Gfx.Alpha(0xFFA3E635, 0.45), 2 * z), new Point(x1 + 2 * z, y), new Point(x2 - 2 * z, y));
                        dc.DrawEllipse(Gfx.B(0xFFFDE68A), null, new Point(cx, y - 70 * z), 5 * z, 3.5 * z);
                        break;
                    }
                case "terrace":
                    {
                        // a bench cut into the pit: its rock face goes down to the floor
                        dc.DrawRectangle(Linear(0xFF6B4226, 0xFF3F2716, 90), edge, new Rect(x1 + 6 * z, yb, w - 12 * z, Math.Max(0, g - yb)));
                        for (int i = 0; i < 6; i++) { double yy = yb + (g - yb) * (i + 0.5) / 6; dc.DrawLine(Gfx.P(Gfx.Alpha(0xFFB87333, 0.25), 2 * z), new Point(x1 + 8 * z, yy), new Point(x2 - 8 * z, yy + (i % 2 == 0 ? 3 : -3) * z)); }
                        break;
                    }
                case "lift":
                    {
                        Pen cable = Gfx.P(0xFF57534E, 2.5);
                        dc.DrawLine(cable, new Point(x1 + 12, y), new Point(x1 + 12, 0)); dc.DrawLine(cable, new Point(x2 - 12, y), new Point(x2 - 12, 0));
                        break;
                    }
                case "bucket":
                    {
                        // an ore bucket hanging from a trolley on a cable across the pit
                        double cy = SY(p.Y + 200);
                        dc.DrawLine(Gfx.P(0xFF292524, 3 * z), new Point(SX(sim.Stage.BlastL), cy + (cx - SX(0)) * 0.02), new Point(SX(sim.Stage.BlastR), cy - (cx - SX(0)) * 0.02));
                        Pen hang = Gfx.P(0xFF44403C, 2.5 * z);
                        dc.DrawLine(hang, new Point(x1 + 8 * z, y), new Point(cx, cy + 12 * z)); dc.DrawLine(hang, new Point(x2 - 8 * z, y), new Point(cx, cy + 12 * z));
                        dc.DrawEllipse(Sphere(0xFF78716C), Gfx.P(0xFF1C1917, 2), new Point(cx, cy + 4 * z), 9 * z, 9 * z);
                        var hull = new StreamGeometry();
                        using (var c = hull.Open()) { c.BeginFigure(new Point(x1, yb - 2 * z), true, true); c.LineTo(new Point(x2, yb - 2 * z), true, true); c.LineTo(new Point(x2 - w * 0.16, yb + 38 * z), true, true); c.LineTo(new Point(x1 + w * 0.16, yb + 38 * z), true, true); }
                        hull.Freeze();
                        dc.DrawGeometry(Linear(0xFF8B5E3C, 0xFF4A2F1C, 90), Gfx.P(0xFF1C1917, 2), hull);
                        for (int i = 1; i < 4; i++) dc.DrawEllipse(Gfx.B(0xFF292524), null, new Point(x1 + w * i / 4.0, yb + 6 * z), 1.8 * z, 1.8 * z);
                        break;
                    }
                case "rack":
                    {
                        // a server rack standing on the floor, its lights blinking
                        dc.DrawRectangle(Linear(0xFF334155, 0xFF111827, 0), Gfx.P(Gfx.Alpha(0xFF22D3EE, 0.55), 2 * z), new Rect(x1 + 4 * z, yb, w - 8 * z, Math.Max(0, g - yb)));
                        int rows = Math.Max(1, (int)((g - yb) / (14 * z)));
                        for (int r = 0; r < rows; r++)
                        {
                            double yy = yb + 7 * z + r * 14 * z;
                            dc.DrawRectangle(Gfx.B(0xFF0F172A), null, new Rect(x1 + 10 * z, yy - 4 * z, w - 20 * z, 9 * z));
                            for (int k = 0; k < 4; k++)
                            {
                                bool on = Math.Sin(t * (2.1 + k * 0.7) + r * 1.9 + k + p.X1) > 0.1;
                                uint led = k == 3 ? 0xFFF59E0B : 0xFF22D3EE;
                                if (on) dc.DrawEllipse(Gfx.B(led), null, new Point(x1 + (14 + k * 6) * z, yy), 1.6 * z, 1.6 * z);
                            }
                            dc.DrawLine(Gfx.P(Gfx.Alpha(0xFF94A3B8, 0.25), 1), new Point(x2 - 30 * z, yy), new Point(x2 - 12 * z, yy));
                        }
                        break;
                    }
                case "tray":
                    {
                        // hung from the ceiling on rods, with cables drooping over the edge
                        Pen rod = Gfx.P(0xFF64748B, 2.5 * z);
                        dc.DrawLine(rod, new Point(x1 + 10 * z, y), new Point(x1 + 10 * z, 0)); dc.DrawLine(rod, new Point(x2 - 10 * z, y), new Point(x2 - 10 * z, 0));
                        uint[] cables = { 0xFF22D3EE, 0xFFF59E0B, 0xFF4ADE80, 0xFFF472B6 };
                        for (int i = 0; i < cables.Length; i++)
                        {
                            double ax = x1 + w * (0.15 + i * 0.22);
                            var droop = new StreamGeometry();
                            using (var c = droop.Open()) { c.BeginFigure(new Point(ax, yb - 2 * z), false, false); c.QuadraticBezierTo(new Point(ax + 14 * z, yb + (16 + i * 5) * z), new Point(ax + 28 * z, yb - 2 * z), true, true); }
                            droop.Freeze();
                            dc.DrawGeometry(null, Gfx.P(cables[i], 2.5 * z), droop);
                        }
                        break;
                    }
                case "rock":
                case "crystal":
                    {
                        // a drifting rock: a jagged underside (crystals hang under the crystal one)
                        bool cr = p.Look == "crystal";
                        var geo = new StreamGeometry();
                        double deep = (cr ? 0.55 : 0.4) * w;
                        using (var c = geo.Open())
                        {
                            c.BeginFigure(new Point(x1, yb - 2 * z), true, true);
                            c.LineTo(new Point(x2, yb - 2 * z), true, true);
                            int n = 7;
                            for (int i = n - 1; i >= 1; i--)
                            {
                                double k = i / (double)n, dip = Math.Sin(k * Math.PI) * deep * (0.7 + 0.3 * ((i * 37 + (int)p.X1) % 5) / 4.0);
                                c.LineTo(new Point(x1 + w * k, yb + dip), true, true);
                            }
                        }
                        geo.Freeze();
                        dc.DrawGeometry(cr ? Linear(0xFF6366F1, 0xFF312E81, 90) : Linear(0xFF71717A, 0xFF27272A, 90), edge, geo);
                        if (!cr) for (int i = 0; i < 3; i++) dc.DrawEllipse(Gfx.B(Gfx.Alpha(0xFF18181B, 0.35)), null, new Point(x1 + w * (0.25 + i * 0.25), yb + deep * (0.25 + (i % 2) * 0.15)), 5 * z, 3 * z);
                        else for (int i = 0; i < 4; i++)
                        {
                            double ax = x1 + w * (0.2 + i * 0.2), len = deep * (0.6 + (i % 2) * 0.5);
                            Poly(dc, Linear(0xFFC7D2FE, 0xFF818CF8, 90), Gfx.P(0xFF4338CA, 1), new Point(ax - 6 * z, yb + deep * 0.3), new Point(ax + 6 * z, yb + deep * 0.3), new Point(ax, yb + len + 14 * z));
                        }
                        break;
                    }
                case "tank":
                    {
                        // a glass specimen tank: green liquid, rising bubbles, going down into the dark
                        double bottom = Math.Max(g + 40 * z, H);
                        dc.DrawRectangle(Linear3(0x5534D399, 0x8822C55E, 0x5534D399, 0), Gfx.P(Gfx.Alpha(0xFFBBF7D0, 0.5), 2 * z), new Rect(x1 + 8 * z, yb, w - 16 * z, bottom - yb));
                        for (int i = 0; i < 7; i++)
                        {
                            double a = (t * 0.35 + i / 7.0) % 1;
                            dc.DrawEllipse(Gfx.B(Gfx.Alpha(0xFFDCFCE7, 0.6 * (1 - a))), null, new Point(x1 + w * (0.2 + (i * 0.37) % 0.6) + Math.Sin(t * 3 + i) * 3 * z, bottom - a * (bottom - yb - 10 * z)), (2 + i % 3) * z, (2 + i % 3) * z);
                        }
                        dc.DrawLine(Gfx.P(Gfx.Alpha(0xFFFFFFFF, 0.3), 3 * z), new Point(x1 + 14 * z, yb + 6 * z), new Point(x1 + 14 * z, bottom));
                        break;
                    }
                case "catwalk":
                    {
                        Pen rod = Gfx.P(0xFF52525B, 2 * z);
                        dc.DrawLine(rod, new Point(x1 + 12 * z, y), new Point(x1 + 12 * z, 0)); dc.DrawLine(rod, new Point(x2 - 12 * z, y), new Point(x2 - 12 * z, 0));
                        break;
                    }
                case "gantry":
                    {
                        // the crane's rail across the top, its trolley and the cables down to the beam
                        double ry = SY(p.Y + 105);
                        dc.DrawRectangle(Linear(0xFF52525B, 0xFF27272A, 90), null, new Rect(0, ry - 8 * z, W, 16 * z));
                        dc.DrawLine(Gfx.P(Gfx.Alpha(0xFFFACC15, 0.5), 2 * z), new Point(0, ry + 8 * z), new Point(W, ry + 8 * z));
                        dc.DrawRoundedRectangle(Linear(0xFFFACC15, 0xFFB45309, 90), Gfx.P(0xFF1C1917, 2), new Rect(cx - 26 * z, ry - 12 * z, 52 * z, 24 * z), 4, 4);
                        Pen cable = Gfx.P(0xFF27272A, 2.5 * z);
                        dc.DrawLine(cable, new Point(cx - 14 * z, ry + 12 * z), new Point(x1 + 10 * z, y)); dc.DrawLine(cable, new Point(cx + 14 * z, ry + 12 * z), new Point(x2 - 10 * z, y));
                        break;
                    }
            }
        }

        /// <summary>Touches in front of a platform: the catwalk's railing, the crane beam's warning stripes, the tank's rim.</summary>
        void DrawOver(DrawingContext dc, BPlat p, Point fl, Point fr, Point bl, Point br, double yb, double t)
        {
            double z = Zoom;
            switch (p.Look)
            {
                case "log": case "bridge": case "culvert": WashOver(dc, p, fl, fr, bl, br, yb, z, t); break;
                case "catwalk":
                    {
                        Pen post = Gfx.P(0xFFA1A1AA, 2 * z), rail = Gfx.P(0xFFD4D4D8, 2.5 * z);
                        double h = 24 * z;
                        foreach (Point[] edgePts in new[] { new[] { bl, br }, new[] { fl, fr } })
                        {
                            Point a = edgePts[0], b = edgePts[1];
                            for (int i = 0; i <= 5; i++) { double k = i / 5.0; double x = a.X + (b.X - a.X) * k, y = a.Y + (b.Y - a.Y) * k; dc.DrawLine(post, new Point(x, y), new Point(x, y - h)); }
                            dc.DrawLine(rail, new Point(a.X, a.Y - h), new Point(b.X, b.Y - h));
                            dc.DrawLine(Gfx.P(0xFFA1A1AA, 1.2 * z), new Point(a.X, a.Y - h / 2), new Point(b.X, b.Y - h / 2));
                        }
                        break;
                    }
                case "gantry":
                    {
                        // yellow and black stripes along the beam's face
                        var face = new RectangleGeometry(new Rect(fl.X, fl.Y, fr.X - fl.X, yb - fl.Y)); face.Freeze();
                        dc.PushClip(face);
                        for (double x = fl.X - 40 * z; x < fr.X + 20 * z; x += 20 * z)
                            Poly(dc, Gfx.B(0xFF1C1917), null, new Point(x, yb), new Point(x + 10 * z, yb), new Point(x + 10 * z + (yb - fl.Y), fl.Y), new Point(x + (yb - fl.Y), fl.Y));
                        dc.Pop();
                        break;
                    }
                case "picker":
                    {
                        // the basket's railing
                        Pen rail = Gfx.P(0xFFFACC15, 3 * z), post = Gfx.P(0xFFCA8A04, 2.5 * z);
                        double h = 26 * z;
                        foreach (Point[] e in new[] { new[] { bl, br }, new[] { fl, fr } })
                        {
                            for (int i = 0; i <= 3; i++) { double k = i / 3.0; double x = e[0].X + (e[1].X - e[0].X) * k, yy = e[0].Y + (e[1].Y - e[0].Y) * k; dc.DrawLine(post, new Point(x, yy), new Point(x, yy - h)); }
                            dc.DrawLine(rail, new Point(e[0].X, e[0].Y - h), new Point(e[1].X, e[1].Y - h));
                        }
                        break;
                    }
                case "tank":
                    dc.DrawLine(Gfx.P(Gfx.Alpha(0xFF4ADE80, 0.5 + 0.3 * Math.Sin(t * 2)), 2 * z), new Point(fl.X + 8 * z, yb + 2 * z), new Point(fr.X - 8 * z, yb + 2 * z));
                    break;
            }
        }

        static Pen RoundPen(uint c, double w) { var p = new Pen(Gfx.B(c), w, null, PenLineCap.Round, PenLineJoin.Round); p.Freeze(); return p; }

        /// <summary>The last touch over everything: the stage's colour grade and a soft vignette.</summary>
        void DrawGrade(DrawingContext dc, BStageDef s)
        {
            uint g = GradeFor(s.Id);
            if (g != 0) dc.DrawRectangle(Gfx.B(g), null, new Rect(0, 0, W, H));
            if (!Lite) dc.DrawRectangle(Vignette(), null, new Rect(0, 0, W, H));
        }
    }
}
