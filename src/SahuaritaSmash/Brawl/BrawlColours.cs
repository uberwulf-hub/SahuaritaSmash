// Sahuarita Smash! (Sauce Edition): each fighter's second and third colours (unlocked on the ladder: BrawlUnlocks.cs),
// pictures only. A colour is a copy of the fighter's definition with other Main / Second / Trim / Dark (and, for the
// ones whose "skin" is a colour of their own, like the saguaro's or the Specimen's, Skin) colours; the art reads them as
// usual. BrawlSim never sees a copy: the match view swaps it in only while drawing a fighter (DrawFighterIn).
// A fighter that isn't in the table (one added later) gets its colours turned round the colour wheel, so it always has
// three different looks.
using System;
using System.Collections.Generic;
using System.Reflection;
using Avalonia;
using Avalonia.Media;

namespace ClassicGames
{
    public static class BrawlColours
    {
        /// <summary>A colour: Main, Second, Trim, Dark, Skin (0 = keep the fighter's own).</summary>
        sealed class Pal { public uint Main, Second, Trim, Dark, Skin; public string Name; }

        static Pal P(string name, uint main, uint second, uint trim, uint dark, uint skin = 0) { return new Pal { Name = name, Main = main, Second = second, Trim = trim, Dark = dark, Skin = skin }; }

        // the second and third colour of each fighter, by id; people's skin is always kept
        static readonly Dictionary<string, Pal[]> table = new Dictionary<string, Pal[]>
        {
            { "bloom",    new[] { P("Violet",  0xFF8B5CF6, 0xFF34D399, 0xFFFDE68A, 0xFF4C1D95), P("Sunset", 0xFFF97316, 0xFF60A5FA, 0xFFFEF08A, 0xFF7C2D12) } },
            { "specimen", new[] { P("Swamp",   0xFF22A35A, 0xFF0F5E2E, 0xFFFDE047, 0xFF0A3A1E, 0xFF6EE7A0), P("Nebula", 0xFF7C3AED, 0xFF3B0F7A, 0xFF67E8F9, 0xFF2E1065, 0xFFC4B5FD) } },
            { "trooper",  new[] { P("Desert",  0xFFB08D57, 0xFFD6C7A1, 0xFFF97316, 0xFF5A4526), P("Night", 0xFF1E3A5F, 0xFF64748B, 0xFFFDE047, 0xFF0B1A2E) } },
            { "juniper",  new[] { P("Meadow",  0xFF10B981, 0xFFFEF3C7, 0xFFF472B6, 0xFF065F46), P("Berry", 0xFFDB2777, 0xFFE0E7FF, 0xFF22D3EE, 0xFF831843) } },
            { "dusty",    new[] { P("Storm",   0xFF64748B, 0xFFE2E8F0, 0xFFF97316, 0xFF1E293B), P("Canyon", 0xFFB45309, 0xFFFDE68A, 0xFF16A34A, 0xFF5A2A04) } },
            { "sam",      new[] { P("Dusk",    0xFF2F8F83, 0xFF1F6B61, 0xFFF9A8D4, 0xFF103D37, 0xFF5CC2B3), P("Golden", 0xFF9A9A3A, 0xFF6E6E22, 0xFFFFFFFF, 0xFF3F3F12, 0xFFC9C960) } },
            { "teddy",    new[] { P("Evening", 0xFF374151, 0xFF9CA3AF, 0xFFB91C1C, 0xFF111827), P("Ranger", 0xFF166534, 0xFFD6C7A1, 0xFFFBBF24, 0xFF0A3A1C) } },
            { "ben",      new[] { P("Liberty", 0xFF1E3A8A, 0xFFF5F5DC, 0xFFFDE047, 0xFF0F1D4A), P("Printer", 0xFF7F1D1D, 0xFFE5E7EB, 0xFFD4AF37, 0xFF3F0B0B) } },
            { "knight",   new[] { P("Gold",    0xFFCA8A04, 0xFF713F12, 0xFF38BDF8, 0xFF422006, 0xFFFDE68A), P("Shadow", 0xFF1F2937, 0xFF111827, 0xFF4ADE80, 0xFF030712, 0xFF4B5563) } },
            { "paul",     new[] { P("Mustard", 0xFFD97706, 0xFF3F3F46, 0xFF7C4A1E, 0xFF78350F), P("Pine", 0xFF15803D, 0xFF78350F, 0xFFFBBF24, 0xFF052E16) } },
            { "gila",     new[] { P("Blossom", 0xFFF472B6, 0xFF2A1A14, 0xFFFFF1F2, 0xFF1A0F0A, 0xFFFBCFE8), P("Turquoise", 0xFF22D3EE, 0xFF0F172A, 0xFFECFEFF, 0xFF020617, 0xFFA5F3FC) } },
        };

        static readonly Dictionary<string, BFighterDef[]> made = new Dictionary<string, BFighterDef[]>();

        /// <summary>Whether a fighter's colours come from the table (false: turned round the colour wheel).</summary>
        public static bool Listed(string id) { return id != null && table.ContainsKey(id); }

        /// <summary>The fighter in colour c (0 = as made; 1, 2 = its second and third colours).</summary>
        public static BFighterDef Def(BFighterDef d, int c)
        {
            if (d == null || c <= 0 || c >= BrawlUnlocks.Colours) return d;
            BFighterDef[] list;
            string key = d.Id + "|" + d.Main.ToString("X8");
            if (!made.TryGetValue(key, out list)) made[key] = list = new BFighterDef[BrawlUnlocks.Colours];
            if (list[c] == null) list[c] = Make(d, PalFor(d, c));
            return list[c];
        }

        /// <summary>The colour's name ("Violet"), or "Usual" for colour 0.</summary>
        public static string Name(BFighterDef d, int c) { return d == null || c <= 0 ? "Usual" : PalFor(d, c).Name; }

        /// <summary>The colour's main colour (for chips).</summary>
        public static uint MainOf(BFighterDef d, int c) { return c <= 0 ? d.Main : PalFor(d, c).Main; }

        static Pal PalFor(BFighterDef d, int c)
        {
            Pal[] p;
            if (table.TryGetValue(d.Id ?? "", out p) && c - 1 < p.Length) return p[c - 1];
            // a fighter added later: its own colours turned a third (or two thirds) round the colour wheel
            double turn = c == 1 ? 130 : 240;
            return new Pal { Name = c == 1 ? "Second" : "Third", Main = Turn(d.Main, turn), Second = Turn(d.Second, turn), Trim = Turn(d.Trim, turn * 0.5), Dark = Turn(d.Dark, turn) };
        }

        static BFighterDef Make(BFighterDef d, Pal p)
        {
            var n = new BFighterDef();
            foreach (FieldInfo fi in typeof(BFighterDef).GetFields(BindingFlags.Public | BindingFlags.Instance))
                if (!fi.IsInitOnly) fi.SetValue(n, fi.GetValue(d));
            foreach (var kv in d.Moves) n.Moves[kv.Key] = kv.Value;
            n.Main = p.Main; n.Second = p.Second; n.Trim = p.Trim; n.Dark = p.Dark;
            if (p.Skin != 0) n.Skin = p.Skin;
            return n;
        }

        /// <summary>A colour turned round the colour wheel by deg degrees (lightness and greyness kept).</summary>
        public static uint Turn(uint argb, double deg)
        {
            double r = ((argb >> 16) & 255) / 255.0, g = ((argb >> 8) & 255) / 255.0, b = (argb & 255) / 255.0;
            double max = Math.Max(r, Math.Max(g, b)), min = Math.Min(r, Math.Min(g, b)), v = max, s = max <= 0 ? 0 : (max - min) / max, h = 0;
            if (max - min > 1e-9)
            {
                if (max == r) h = 60 * (((g - b) / (max - min)) % 6);
                else if (max == g) h = 60 * ((b - r) / (max - min) + 2);
                else h = 60 * ((r - g) / (max - min) + 4);
            }
            h = ((h + deg) % 360 + 360) % 360;
            double cc = v * s, x = cc * (1 - Math.Abs(h / 60 % 2 - 1)), m = v - cc;
            double rr, gg, bb;
            if (h < 60) { rr = cc; gg = x; bb = 0; } else if (h < 120) { rr = x; gg = cc; bb = 0; } else if (h < 180) { rr = 0; gg = cc; bb = x; }
            else if (h < 240) { rr = 0; gg = x; bb = cc; } else if (h < 300) { rr = x; gg = 0; bb = cc; } else { rr = cc; gg = 0; bb = x; }
            Func<double, uint> q = t => (uint)Math.Max(0, Math.Min(255, Math.Round((t + m) * 255)));
            return (argb & 0xFF000000) | (q(rr) << 16) | (q(gg) << 8) | q(bb);
        }
    }

    public sealed partial class BrawlView
    {
        /// <summary>Each fighter's colour in the match on screen, by slot (0 = the usual one). Pictures only.</summary>
        public readonly int[] Colours = new int[8];

        /// <summary>A fighter of the match in its colour.</summary>
        public BFighterDef Painted(BFighter f) { return BrawlColours.Def(f.D, Colours[f.Slot % 8]); }

        /// <summary>Draws a fighter of the match in its colour: its definition is swapped for the coloured copy only while
        /// it is drawn (on this thread, between steps), so the match itself never sees it.</summary>
        void DrawFighterIn(DrawingContext dc, BFighter f, BrawlSim sim, double t)
        {
            BFighterDef keep = f.D, paint = Painted(f);
            if (paint == keep) { DrawFighter(dc, f, sim, t); return; }
            f.D = paint;
            try { DrawFighter(dc, f, sim, t); }
            finally { f.D = keep; }
        }
    }
}
