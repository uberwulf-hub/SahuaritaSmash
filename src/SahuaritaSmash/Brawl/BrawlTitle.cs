// Sahuarita Smash! (Sauce Edition) - the title screen (Creative Collaboration Test 003, round 1: art direction ChatGPT,
// game director Andrew, 2026-11-20): an inked, tilted logo, the menu kit (chunky rounded buttons with an ink outline
// and a drop shadow; the chosen one turns sunny yellow), the seven items in two columns on the left, and the cast in
// two staggered rows of six on the right. Same items, same words, same order (Up / Down go through them as before;
// Left / Right now jump between the columns). Every button is 78 picture units tall (about 44 phone pixels).
// The kit's numbers are constants here so a later spec can change them in one place.
using System;
using Avalonia;
using Avalonia.Input;
using Avalonia.Media;

namespace ClassicGames
{
    public sealed partial class BrawlGame
    {
        // the menu kit
        public const double KitButtonH = 78, KitButtonW = 292, KitGap = 12, KitRadius = 20, KitInk = 3, KitShadow = 5;
        const uint KitFillTop = 0xFF2A3550, KitFillBottom = 0xFF1A2133, KitHoverTop = 0xFF34426A, KitHoverBottom = 0xFF222B44,
            KitPickTop = 0xFFFFD45C, KitPickBottom = 0xFFF59E0B, KitText = 0xFFF1F5F9, KitSub = 0xFFA9B4C8,
            KitPickText = 0xFF161616, KitPickSub = 0xFF5A3A06, KitInkColour = 0xFF161616;
        // the logo
        const uint LogoTown = 0xFFFB923C, LogoSmash = 0xFFFDE047, LogoShadow = 0xFF3B1260;

        /// <summary>The title's menu items, in order (Up / Down go through them in this order).</summary>
        string[] TitleItems { get { return new[] { "Fight (2 to 4 players)", "The ladder (on your own)", "Practice (try your moves)", "How to play", ControlsItem, "Play online (with others)", "Quit" }; } }
        /// <summary>Items in the left column; the rest go in the right one, from the top.</summary>
        public const int TitleLeftColumn = 4;

        void DrawTitle(DrawingContext dc)
        {
            // the cast on the right: two staggered rows of six, the back row a little smaller
            int n = BrawlData.Fighters.Count;
            for (int i = 0; i < n; i++)
            {
                bool back = i < 6;
                int c = back ? i : i - 6;
                BFighterDef d = BrawlData.Fighters[i];
                double x = back ? 730 + c * 98 : 770 + c * 90, y = back ? 452 : 690;
                dc.DrawEllipse(Gfx.B(0x50000000), null, new Point(x, y - 2), 36, 8);
                View.Portrait(dc, d, x, y, d.Height * (back ? 1.3 : 1.55), T + i * 0.37, false, BS.Idle);
            }
            InkedLogo(dc, 350, 58);
            Gfx.Text(dc, "Knock your friends off the stage.", 350, 252, 19, 0xFFE5E7EB, 0.5, Gfx.Ui);
            Gfx.Text(dc, "Nobody gets hurt: they just fly off with a twinkle.", 350, 276, 19, 0xFFE5E7EB, 0.5, Gfx.Ui);
            string[] items = TitleItems;
            for (int i = 0; i < items.Length; i++)
            {
                int col = i < TitleLeftColumn ? 0 : 1, row = col == 0 ? i : i - TitleLeftColumn;
                var r = new Rect(48 + col * (KitButtonW + 20), 322 + row * (KitButtonH + KitGap), KitButtonW, KitButtonH);
                int item = i;
                AddItemHit(r, items[i], i == Sel, () => Sel = item, Key.Enter).Item = i;
                KitButton(dc, r, items[i], i == Sel, Hovered(r));
            }
        }

        /// <summary>Left / Right on the title: to the other column, the same row (or its last item).</summary>
        int TitleColumnMove(int sel)
        {
            int rightCount = TitleItems.Length - TitleLeftColumn;
            if (sel < TitleLeftColumn) return TitleLeftColumn + Math.Min(sel, rightCount - 1);
            return sel - TitleLeftColumn;
        }

        /// <summary>One button of the menu kit. A label "Main (more)" is drawn as Main, big, with "(more)" under it.</summary>
        void KitButton(DrawingContext dc, Rect r, string label, bool picked, bool hover)
        {
            dc.DrawRoundedRectangle(Gfx.B(KitInkColour), null, new Rect(r.X, r.Y + KitShadow, r.Width, r.Height), KitRadius, KitRadius);
            uint top = picked ? KitPickTop : hover ? KitHoverTop : KitFillTop, bottom = picked ? KitPickBottom : hover ? KitHoverBottom : KitFillBottom;
            var fill = new LinearGradientBrush { StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative), EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative) };
            fill.GradientStops.Add(new GradientStop(Color.FromUInt32(top), 0));
            fill.GradientStops.Add(new GradientStop(Color.FromUInt32(bottom), 1));
            dc.DrawRoundedRectangle(fill, Gfx.P(KitInkColour, KitInk), r, KitRadius, KitRadius);
            // a soft shine along the top
            dc.DrawLine(Gfx.P(Gfx.Alpha(0xFFFFFFFF, picked ? 0.55 : 0.14), 2.5), new Point(r.X + KitRadius, r.Y + 6), new Point(r.Right - KitRadius, r.Y + 6));
            int p = label.IndexOf(" (", StringComparison.Ordinal);
            string main = p > 0 ? label.Substring(0, p) : label, sub = p > 0 ? label.Substring(p + 1) : null;
            double cx = r.X + r.Width / 2;
            if (sub == null) Gfx.Text(dc, main, cx, r.Y + r.Height / 2 - 16, 26, picked ? KitPickText : KitText, 0.5, Gfx.UiBold);
            else
            {
                Gfx.Text(dc, main, cx, r.Y + 11, 26, picked ? KitPickText : KitText, 0.5, Gfx.UiBold);
                Gfx.Text(dc, sub, cx, r.Y + 46, 17, picked ? KitPickSub : KitSub, 0.5, Gfx.Ui);
            }
        }

        /// <summary>The logo: SAHUARITA over a big SMASH!, both inked with a thick round outline, tilted a little, on a
        /// drop shadow. cx is its middle, y its top.</summary>
        static void InkedLogo(DrawingContext dc, double cx, double y)
        {
            dc.PushTransform(new RotateTransform(-4, cx, y + 90));
            InkedWord(dc, "SAHUARITA", cx, y, 54, LogoTown, 9);
            InkedWord(dc, "SMASH!", cx, y + 52, 124, LogoSmash, 14);
            dc.Pop();
        }

        static void InkedWord(DrawingContext dc, string word, double cx, double y, double size, uint colour, double ink)
        {
            FormattedText ft = Gfx.Fmt(word, size, colour, Gfx.UiBold);
            double x = cx - ft.WidthIncludingTrailingWhitespace / 2;
            Geometry shadow = ft.BuildGeometry(new Point(x + 4, y + 9)), g = ft.BuildGeometry(new Point(x, y));
            var inkPen = new Pen(Gfx.B(KitInkColour), ink, null, PenLineCap.Round, PenLineJoin.Round);
            dc.DrawGeometry(Gfx.B(LogoShadow), new Pen(Gfx.B(LogoShadow), ink, null, PenLineCap.Round, PenLineJoin.Round), shadow);
            dc.DrawGeometry(null, inkPen, g);
            var fill = new LinearGradientBrush { StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative), EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative) };
            fill.GradientStops.Add(new GradientStop(Color.FromUInt32(Gfx.Mix(colour, 0xFFFFFFFF, 0.35)), 0));
            fill.GradientStops.Add(new GradientStop(Color.FromUInt32(colour), 0.55));
            fill.GradientStops.Add(new GradientStop(Color.FromUInt32(Gfx.Mix(colour, 0xFFB45309, 0.35)), 1));
            dc.DrawGeometry(fill, null, g);
        }
    }
}
