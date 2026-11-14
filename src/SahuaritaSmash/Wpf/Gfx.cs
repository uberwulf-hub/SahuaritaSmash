// Classic Games' drawing helpers (App Hub's Engine\Core.cs, class Gfx), on Avalonia: cached brushes and pens, colour
// mixing, text. Fonts: Inter (bundled; Segoe UI and Consolas aren't on Android).
using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Media;

namespace ClassicGames
{
    public static class Gfx
    {
        static readonly Dictionary<uint, SolidColorBrush> brushes = new Dictionary<uint, SolidColorBrush>();
        static readonly Dictionary<string, FormattedText> texts = new Dictionary<string, FormattedText>();
        public static readonly Typeface Ui = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.SemiBold, FontStretch.Normal);
        public static readonly Typeface UiBold = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Bold, FontStretch.Normal);
        public static readonly Typeface Mono = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Bold, FontStretch.Normal);

        public static SolidColorBrush B(uint argb)
        {
            SolidColorBrush b;
            if (brushes.TryGetValue(argb, out b)) return b;
            b = new SolidColorBrush(WpfBrush.C(argb));
            brushes[argb] = b;
            return b;
        }
        static readonly Dictionary<long, Pen> pens = new Dictionary<long, Pen>();
        public static Pen P(uint argb, double width)
        {
            long key = ((long)argb << 16) ^ (long)(width * 64);
            Pen p;
            if (pens.TryGetValue(key, out p)) return p;
            p = new Pen(B(argb), width, null, PenLineCap.Round, PenLineJoin.Round);
            pens[key] = p;
            return p;
        }
        public static uint Alpha(uint argb, double a) { return ((uint)(Math.Max(0, Math.Min(1, a)) * 255) << 24) | (argb & 0xFFFFFF); }
        public static uint Mix(uint a, uint b, double k)
        {
            k = Math.Max(0, Math.Min(1, k));
            Func<int, uint> ch = s => (uint)Math.Round(((a >> s) & 255) * (1 - k) + ((b >> s) & 255) * k);
            return (ch(24) << 24) | (ch(16) << 16) | (ch(8) << 8) | ch(0);
        }

        public static FormattedText Fmt(string s, double size, uint color, Typeface tf)
        {
            string key = s + "\u0001" + size.ToString(CultureInfo.InvariantCulture) + "\u0001" + color.ToString(CultureInfo.InvariantCulture) + "\u0001" + tf.GetHashCode();
            FormattedText ft;
            if (texts.TryGetValue(key, out ft)) return ft;
            if (texts.Count > 3000) texts.Clear();
            ft = new FormattedText(s, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, tf, size, B(color));
            texts[key] = ft;
            return ft;
        }
        /// <summary>Draws text; align 0 = left, 0.5 = centred, 1 = right. Returns the width.</summary>
        public static double Text(DrawingContext dc, string s, double x, double y, double size, uint color, double align, Typeface tf)
        {
            if (string.IsNullOrEmpty(s)) return 0;
            FormattedText ft = Fmt(s, size, color, tf);
            dc.DrawText(ft, new Point(x - ft.WidthIncludingTrailingWhitespace * align, y));
            return ft.WidthIncludingTrailingWhitespace;
        }
        public static double Text(DrawingContext dc, string s, double x, double y, double size, uint color, double align) { return Text(dc, s, x, y, size, color, align, Ui); }
        public static double Shadowed(DrawingContext dc, string s, double x, double y, double size, uint color, double align, Typeface tf)
        {
            Text(dc, s, x + size * 0.07, y + size * 0.07, size, (uint)((color >> 24) * 3 / 4) << 24, align, tf);
            return Text(dc, s, x, y, size, color, align, tf);
        }
        public static double Width(string s, double size, Typeface tf) { return string.IsNullOrEmpty(s) ? 0 : Fmt(s, size, 0xFFFFFFFF, tf).WidthIncludingTrailingWhitespace; }

        public static void Panel(DrawingContext dc, Rect r) { Panel(dc, r, 0xF0131820, 0xFF3B82F6); }
        public static void Panel(DrawingContext dc, Rect r, uint fill, uint border)
        {
            dc.DrawRoundedRectangle(B(fill), P(border, 2), r, 14, 14);
        }

        public static string N(long v) { return v.ToString("N0", CultureInfo.InvariantCulture); }

        /// <summary>Key hints: pairs of (keys, what they do).</summary>
        public static double Keys(DrawingContext dc, double x, double y, double size, params string[] pairs)
        {
            double w = 0;
            for (int i = 0; i + 1 < pairs.Length; i += 2) w = Math.Max(w, Width(pairs[i], size, UiBold));
            for (int i = 0; i + 1 < pairs.Length; i += 2)
            {
                Text(dc, pairs[i], x, y, size, 0xFF93C5FD, 0, UiBold);
                Text(dc, pairs[i + 1], x + w + size * 0.8, y, size, 0xFFB8BFCC, 0, Ui);
                y += size * 1.45;
            }
            return y;
        }
    }
}
