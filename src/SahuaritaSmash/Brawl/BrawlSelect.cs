// Sahuarita Smash! (Sauce Edition) - the fighter select screen (Creative Collaboration Test 003, round 2: art direction
// ChatGPT, game director Andrew, 2026-11-21). Four player cards on the left (each: the seat's colour edge with P1-P4,
// a fighter control and a "played by" control), a stage / rules / surprises strip, the colour chips and Fight!; on the
// right the fighter grid (5 across, room for 15) and the chosen fighter's card. Every tappable thing is 78+ units.
// The choices, the keys and what each does are exactly as before: the same 12 logical rows (Brawl.cs SelectRows, Change,
// SetupKey): Up / Down choose a row, Left / Right change it, C the colour, Enter starts (or jumps to Fight!).
// States: selected = gold, focused (the keyboard / controller row) = a cyan ring, both = gold fill + cyan ring,
// pressed = orange and 3 units down for a moment, locked = darkened with a padlock.
using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Input;
using Avalonia.Media;

namespace ClassicGames
{
    public sealed partial class BrawlGame
    {
        // the spec's seat colours (used on this screen; the fight's own player colours are unchanged)
        static readonly uint[] SeatColour = { 0xFFC94D50, 0xFF438BCB, 0xFFE5B83F, 0xFF459A69 };
        const uint SelGold = 0xFFFDC93A, FocusCyan = 0xFF22D3EE, PressOrange = 0xFFF97316, CardFill = 0xFF1F2738, CardFillOff = 0xFF171C27,
            InfoCream = 0xFFE8DCC0;
        public const double SeatCardW = 286, SeatCardH = 170, SelectControlH = 78;
        string pressedId = ""; double pressedAt = -10;
        protected override double BackButtonX { get { return Menu == Screen.Select ? 24 : 12; } }

        /// <summary>The seat that a tapped fighter goes to and whose colours are shown: the seat of the row the keys are on,
        /// else the last one chosen (it was always seat 1 before; now it's the one with the gold outline).</summary>
        int ActiveSeat { get { if (Sel < 8) colourSeat = Sel / 2; return colourSeat; } }

        void Pressed(string id) { pressedId = id; pressedAt = T; }
        bool IsPressed(string id) { return pressedId == id && T - pressedAt < 0.15; }

        void DrawSelect(DrawingContext dc)
        {
            Gfx.Text(dc, "Choose your fighters", 175, 28, 42, 0xFFFDE047, 0, Gfx.UiBold);
            int active = ActiveSeat;
            // ---- the four player cards (two by two)
            for (int s = 0; s < 4; s++) DrawSeatCard(dc, s, 32 + (s % 2) * (SeatCardW + 12), 105 + (s / 2) * (SeatCardH + 12), s == active);
            // ---- stage, rules, surprises
            double sy = 466;
            StripControl(dc, new Rect(32, sy, 250, SelectControlH), "Stage", StagePick >= 0 ? BrawlData.Stages[StagePick].Name : "Any stage", 8, true);
            StripControl(dc, new Rect(294, sy, 160, SelectControlH), "Rules", Minutes > 0 ? Minutes + " minutes, most bonks wins" : Lives + (Lives == 1 ? " life each" : " lives each"), 9, true);
            SurprisesToggle(dc, new Rect(466, sy, 150, SelectControlH));
            // ---- colour and Fight!
            DrawSelectColours(dc);   // BrawlColourPick.cs
            FightButton(dc, new Rect(356, 560, 260, 80));
            // ---- the fighters
            DrawFighterGrid(dc, active);
            DrawFighterInfo(dc, active);
            Gfx.Text(dc, "Up / Down choose a row, Left / Right change it, C the colour, Enter starts. Tap a fighter to give it to the player with the gold outline.", Width / 2, 696, 14, 0xFF8B95A7, 0.5, Gfx.Ui);
        }

        void DrawSeatCard(DrawingContext dc, int s, double x, double y, bool active)
        {
            var card = new Rect(x, y, SeatCardW, SeatCardH);
            bool off = Who[s] == 0;
            int fRow = s * 2, wRow = s * 2 + 1;
            // the whole card: tapping it makes it the active seat
            AddHit(card, "Player " + (s + 1) + " card", Sel == fRow || Sel == wRow, () => { }, () => { Sel = fRow; Audio.Ui("move"); });
            dc.DrawRoundedRectangle(Gfx.B(FighterInk.Ink), null, new Rect(x, y + 4, card.Width, card.Height), 16, 16);
            dc.DrawRoundedRectangle(Gfx.B(off ? CardFillOff : CardFill), Gfx.P(FighterInk.Ink, 3), card, 16, 16);
            // the seat's colour edge with its number
            var edge = new StreamGeometry();
            using (var g = edge.Open())
            {
                g.BeginFigure(new Point(x + 16, y), true, true);
                g.LineTo(new Point(x + 34, y), true, true);
                g.LineTo(new Point(x + 34, y + SeatCardH), true, true);
                g.LineTo(new Point(x + 16, y + SeatCardH), true, true);
                g.QuadraticBezierTo(new Point(x, y + SeatCardH), new Point(x, y + SeatCardH - 16), true, true);
                g.LineTo(new Point(x, y + 16), true, true);
                g.QuadraticBezierTo(new Point(x, y), new Point(x + 16, y), true, true);
            }
            edge.Freeze();
            uint seatCol = off ? Gfx.Mix(SeatColour[s], 0xFF1F2738, 0.55) : SeatColour[s];
            dc.DrawGeometry(Gfx.B(seatCol), Gfx.P(FighterInk.Ink, 3), edge);
            dc.PushTransform(new RotateTransform(-90, x + 17, y + SeatCardH / 2));
            Gfx.Text(dc, "P" + (s + 1), x + 17, y + SeatCardH / 2 - 13, 21, s == 2 ? FighterInk.Ink : 0xFFFFFFFF, 0.5, Gfx.UiBold);
            dc.Pop();
            if (active)
            {
                // the active seat: a bright gold outline and a small pointer
                dc.DrawRoundedRectangle(null, Gfx.P(SelGold, 4), new Rect(x - 2, y - 2, card.Width + 4, card.Height + 4), 18, 18);
                var ptr = new StreamGeometry();
                using (var g = ptr.Open()) { g.BeginFigure(new Point(x - 14, y + SeatCardH / 2 - 11), true, true); g.LineTo(new Point(x - 1, y + SeatCardH / 2), true, true); g.LineTo(new Point(x - 14, y + SeatCardH / 2 + 11), true, true); }
                ptr.Freeze();
                dc.DrawGeometry(Gfx.B(SelGold), Gfx.P(FighterInk.Ink, 2), ptr);
            }
            double cx0 = x + 42, cw = SeatCardW - 50;
            // the fighter control: < and > change it, the middle makes this seat active for the grid
            var fr = new Rect(cx0, y + 6, cw, SelectControlH);
            string fName = Pick[s] >= 0 ? BrawlData.Fighters[Pick[s]].Name : "Any fighter";
            bool fPress = IsPressed("f" + s);
            KitControl(dc, fr, Sel == fRow, false, fPress, true);
            double fdy = fPress ? 3 : 0;
            if (Pick[s] >= 0) View.Portrait(dc, BrawlData.Fighters[Pick[s]], fr.X + 60, fr.Y + fr.Height / 2 + fdy, 54, T + s * 0.4, true, BS.Idle);
            else Gfx.Text(dc, "?", fr.X + 60, fr.Y + 16 + fdy, 36, 0xFF93C5FD, 0.5, Gfx.UiBold);
            FitText(dc, fName, fr.X + 90, fr.Y + fdy, fr.Width - 90 - 34, fr.Height, 25, 16, off ? 0xFF8B95A7 : 0xFFFFFFFF, Gfx.UiBold, 0);
            int fi = fRow;
            AddHit(new Rect(fr.X + 78, fr.Y, fr.Width - 156, fr.Height), "Player " + (s + 1) + " " + fName, Sel == fRow, () => Sel = fi, () => { Sel = fi; Pressed("f" + (fi / 2)); Audio.Ui("move"); }).Item = fRow;
            ArrowZones(dc, fr, "Player " + (s + 1), fRow, "f" + s);
            // the played-by control: < and > (or tap the left / right half) go through off, a person, the computer's levels
            var wr = new Rect(cx0, y + 88, cw, SelectControlH);
            bool wPress = IsPressed("w" + s);
            KitControl(dc, wr, Sel == wRow, false, wPress, true);
            string main, sub;
            PlayedBy(s, out main, out sub);
            Gfx.Text(dc, main, wr.X + wr.Width / 2, wr.Y + 12 + (wPress ? 3 : 0), 22, off ? 0xFF8B95A7 : 0xFFF1F5F9, 0.5, Gfx.UiBold);
            FitText(dc, sub, wr.X + wr.Width / 2, wr.Y + 40 + (wPress ? 3 : 0), wr.Width - 70, 30, 18, 14, 0xFFA9B4C8, Gfx.Ui, 0.5);
            ArrowZones(dc, wr, "played by", wRow, "w" + s, true);
        }

        /// <summary>Who plays a seat, in two lines: "Computer" / "Hard", "Person" / "left keys", "Nobody" / "(off)".</summary>
        void PlayedBy(int s, out string main, out string sub)
        {
            if (Who[s] == 0) { main = "Nobody"; sub = "off"; return; }
            if (Who[s] == 2) { main = "Computer"; sub = LevelNames[Level[s]]; return; }
            string t = WhoText(s);   // "a person (the left keys)"
            int p = t.IndexOf('(');
            main = "Person";
            sub = p >= 0 ? t.Substring(p + 1).TrimEnd(')').Replace("the ", "") : "";
        }

        /// <summary>A control's left and right halves (or 78-wide ends) as the old arrows: Left / Right on that row.</summary>
        void ArrowZones(DrawingContext dc, Rect r, string label, int row, string id, bool halves = false)
        {
            double aw = halves ? r.Width / 2 : SelectControlH;
            bool lit = Sel == row;
            uint arrow = lit ? 0xFFFFFFFF : 0xFF93C5FD;
            double dy = IsPressed(id) ? 3 : 0;
            Gfx.Text(dc, "\u25C0", r.X + 18, r.Y + r.Height / 2 - 12 + dy, 20, arrow, 0.5, Gfx.UiBold);
            Gfx.Text(dc, "\u25B6", r.Right - 18, r.Y + r.Height / 2 - 12 + dy, 20, arrow, 0.5, Gfx.UiBold);
            int rr = row;
            AddHit(new Rect(r.X, r.Y, aw, r.Height), label + " <", lit, () => Sel = rr, () => { Sel = rr; Pressed(id); TapKey(Key.Left); }).Item = row;
            AddHit(new Rect(r.Right - aw, r.Y, aw, r.Height), label + " >", lit, () => Sel = rr, () => { Sel = rr; Pressed(id); TapKey(Key.Right); }).Item = row;
        }

        /// <summary>One strip setting: a label over its value, left half / right half change it.</summary>
        void StripControl(DrawingContext dc, Rect r, string label, string value, int row, bool arrows)
        {
            string id = "row" + row;
            bool press = IsPressed(id);
            KitControl(dc, r, Sel == row, false, press, true);
            double dy = press ? 3 : 0;
            Gfx.Text(dc, label, r.X + r.Width / 2, r.Y + 6 + dy, 19, 0xFF93C5FD, 0.5, Gfx.UiBold);
            FitText(dc, value, r.X + r.Width / 2, r.Y + 30 + dy, r.Width - 52, 44, 23, 15, 0xFFFFFFFF, Gfx.UiBold, 0.5);
            ArrowZones(dc, r, label, row, id, true);
        }

        /// <summary>Stage surprises as a toggle: a tick and teal when on, a cross and grey when off; a tap flips it.</summary>
        void SurprisesToggle(DrawingContext dc, Rect r)
        {
            const int row = 10;
            bool press = IsPressed("row10");
            KitControl(dc, r, Sel == row, false, press, true);
            double dy = press ? 3 : 0;
            FitText(dc, "Stage surprises", r.X + r.Width / 2, r.Y + 6 + dy, r.Width - 16, 24, 18, 14, 0xFF93C5FD, Gfx.UiBold, 0.5);
            var pill = new Rect(r.X + r.Width / 2 - 44, r.Y + 34 + dy, 88, 34);
            uint fill = Surprises ? 0xFF14B8A6 : 0xFF475569;
            dc.DrawRoundedRectangle(Gfx.B(fill), Gfx.P(FighterInk.Ink, 2.5), pill, 17, 17);
            var knob = new Point(Surprises ? pill.Right - 17 : pill.X + 17, pill.Y + 17);
            dc.DrawEllipse(Gfx.B(0xFFF8FAFC), Gfx.P(FighterInk.Ink, 2), knob, 13, 13);
            Gfx.Text(dc, Surprises ? "\u2713" : "\u2715", knob.X, knob.Y - 11, 17, Surprises ? 0xFF0F766E : 0xFF475569, 0.5, Gfx.UiBold);
            Gfx.Text(dc, Surprises ? "On" : "Off", Surprises ? pill.X + 26 : pill.Right - 26, pill.Y + 6, 17, 0xFFFFFFFF, 0.5, Gfx.UiBold);
            AddHit(r, "Stage surprises " + (Surprises ? "On" : "Off"), Sel == row, () => Sel = row, () => { Sel = row; Pressed("row10"); TapKey(Key.Right); }).Item = row;
        }

        void FightButton(DrawingContext dc, Rect r)
        {
            int row = SelectRows - 1;
            bool focus = Sel == row, press = IsPressed("fight");
            var rr = press ? new Rect(r.X, r.Y + 3, r.Width, r.Height) : r;
            dc.DrawRoundedRectangle(Gfx.B(FighterInk.Ink), null, new Rect(r.X, r.Y + 5, r.Width, r.Height), 20, 20);
            uint top = press ? PressOrange : focus ? KitPickTop : 0xFF2A3550, bottom = press ? 0xFFEA580C : focus ? KitPickBottom : 0xFF1A2133;
            dc.DrawRoundedRectangle(Vertical(top, bottom), Gfx.P(FighterInk.Ink, 3), rr, 20, 20);
            if (focus) dc.DrawRoundedRectangle(null, Gfx.P(FocusCyan, 3.5), new Rect(rr.X - 5, rr.Y - 5, rr.Width + 10, rr.Height + 10), 24, 24);
            Gfx.Text(dc, "FIGHT!  \u25B6", rr.X + rr.Width / 2, rr.Y + rr.Height / 2 - 20, 31, focus || press ? FighterInk.Ink : 0xFFFFFFFF, 0.5, Gfx.UiBold);
            AddHit(r, "Fight", focus, () => Sel = row, () => { Sel = row; Pressed("fight"); TryStart(); }).Item = row;
        }

        void DrawFighterGrid(DrawingContext dc, int active)
        {
            int n = BrawlData.Fighters.Count;
            const double gx = 656, gy = 82, tw = 112, th = 116;
            for (int i = 0; i < 15; i++)
            {
                double x = gx + (i % 5) * (tw + 8), y = gy + (i / 5) * (th + 12);
                var r = new Rect(x, y, tw, th);
                if (i >= n)
                {
                    // an empty place in the roster: drawn faintly, not tappable
                    dc.DrawRoundedRectangle(Gfx.B(0x30131A2A), Gfx.P(0x40FFFFFF, 1.5), r, 14, 14);
                    continue;
                }
                BFighterDef d = BrawlData.Fighters[i];
                bool chosen = Pick[active] == i, focus = chosen && Sel == active * 2, press = IsPressed("tile" + i), lit = Hovered(r);
                var rr = press ? new Rect(x, y + 3, tw, th) : r;
                dc.DrawRoundedRectangle(Gfx.B(press ? Gfx.Mix(PressOrange, CardFill, 0.4) : lit ? 0xFF2A3550 : CardFill), Gfx.P(chosen ? SelGold : FighterInk.Ink, chosen ? 4 : 3), rr, 14, 14);
                if (focus) dc.DrawRoundedRectangle(null, Gfx.P(FocusCyan, 3.5), new Rect(rr.X - 5, rr.Y - 5, rr.Width + 10, rr.Height + 10), 18, 18);
                // a name that doesn't fit on one line at 20 goes on two lines, never under TileNameMin (ChatGPT's review:
                // "Ben Franklin" shrank too far to read on a phone)
                bool twoLines = Gfx.Width(d.Name, 20, Gfx.UiBold) > tw - 8;
                View.Portrait(dc, d, rr.X + tw / 2, rr.Y + (twoLines ? 68 : 80), Math.Min(twoLines ? 64 : 76, d.Height * 0.8), T + i * 0.3, false, BS.Idle);
                if (!twoLines) FitText(dc, d.Name, rr.X + tw / 2, rr.Y + 82, tw - 8, 32, 20, TileNameMin, 0xFFFFFFFF, Gfx.UiBold, 0.5);
                else TileTwoLines(dc, d.Name, rr.X + tw / 2, rr.Y + 70, tw - 6);
                if (chosen)
                {
                    dc.DrawEllipse(Gfx.B(SelGold), Gfx.P(FighterInk.Ink, 2), new Point(rr.Right - 15, rr.Y + 15), 11, 11);
                    Gfx.Text(dc, "\u2713", rr.Right - 15, rr.Y + 4, 15, FighterInk.Ink, 0.5, Gfx.UiBold);
                }
                // who has picked it
                int k = 0;
                for (int s = 0; s < 4; s++)
                    if (Who[s] != 0 && Pick[s] == i)
                    {
                        dc.DrawRoundedRectangle(Gfx.B(SeatColour[s]), Gfx.P(FighterInk.Ink, 1.5), new Rect(rr.X + 6 + k * 27, rr.Y + 6, 25, 20), 6, 6);
                        Gfx.Text(dc, "P" + (s + 1), rr.X + 18.5 + k * 27, rr.Y + 7, 13, s == 2 ? FighterInk.Ink : 0xFFFFFFFF, 0.5, Gfx.UiBold);
                        k++;
                    }
                int fi = i;
                AddHit(r, d.Name, false, null, () => { int seat = ActiveSeat; Pick[seat] = fi; if (Who[seat] == 0) Who[seat] = 2; Pressed("tile" + fi); Audio.Ui("select"); });
            }
        }

        void DrawFighterInfo(DrawingContext dc, int active)
        {
            var r = new Rect(656, 470, 592, 196);
            dc.DrawRoundedRectangle(Gfx.B(0xE0161C2A), Gfx.P(FighterInk.Ink, 3), r, 16, 16);
            double x = r.X + 18, y = r.Y;
            int show = Pick[active];
            if (show >= 0)
            {
                BFighterDef d = BrawlData.Fighters[show];
                Gfx.Text(dc, d.Name, x, y + 8, 27, 0xFFFDE047, 0, Gfx.UiBold);
                FitText(dc, d.Group + "  \u00B7  " + d.From, x, y + 42, r.Width - 36, 24, 20, 15, InfoCream, Gfx.Ui, 0);
                double by = y + 70;
                List<string> lines = LinesFor(d.Blurb, 20, r.Width - 36);
                double bs = 20;
                if (lines.Count > 2) { bs = 17; lines = LinesFor(d.Blurb, bs, r.Width - 36); }
                foreach (string l in lines) { Gfx.Text(dc, l, x, by, bs, 0xFFE5E7EB, 0, Gfx.Ui); by += bs * 1.25; }
                double ly = Math.Max(y + 124, by + 4);
                // the four specials in two columns, the stats on the right
                for (int i = 0; i < d.SpecialNames.Length && i < 4; i++)
                    FitText(dc, "\u2022 " + d.SpecialNames[i], x + (i % 2) * 172, ly + (i / 2) * 23, 166, 23, 18, 13, 0xFFB8BFCC, Gfx.Ui, 0);
                double sx = r.Right - 200;
                Gfx.Text(dc, "Weight  " + Stars(d.Weight, 75, 135), sx, ly, 18, 0xFFB8BFCC, 0, Gfx.Ui);
                Gfx.Text(dc, "Speed   " + Stars(d.Run, 5.5, 11), sx, ly + 23, 18, 0xFFB8BFCC, 0, Gfx.Ui);
                Gfx.Text(dc, "Jumps   " + (d.Jumps + 0), sx, ly + 46, 18, 0xFFB8BFCC, 0, Gfx.Ui);
            }
            else
            {
                BStageDef s = StagePick >= 0 ? BrawlData.Stages[StagePick] : null;
                Gfx.Text(dc, s != null ? s.Name : "Any stage", x, y + 8, 27, 0xFFFDE047, 0, Gfx.UiBold);
                if (s != null) { FitText(dc, s.From, x, y + 42, r.Width - 36, 24, 20, 15, InfoCream, Gfx.Ui, 0); Wrap(dc, s.Blurb, x, y + 72, r.Width - 36, 19, 0xFFE5E7EB); }
                else Wrap(dc, "A stage picked at random when the fight starts.", x, y + 48, r.Width - 36, 20, 0xFFE5E7EB);
            }
        }

        /// <summary>The smallest size a fighter's name is drawn at on a tile (about 10 phone pixels).</summary>
        public const double TileNameMin = 18;

        /// <summary>A long name on two lines (split at the space nearest the middle), as big as fits from 20 down to
        /// TileNameMin.</summary>
        static void TileTwoLines(DrawingContext dc, string name, double cx, double y, double width)
        {
            string[] words = name.Split(' ');
            string a = name, b = "";
            double best = double.MaxValue;
            for (int k = 1; k < words.Length; k++)
            {
                string l1 = string.Join(" ", words, 0, k), l2 = string.Join(" ", words, k, words.Length - k);
                double w = Math.Max(Gfx.Width(l1, 20, Gfx.UiBold), Gfx.Width(l2, 20, Gfx.UiBold));
                if (w < best) { best = w; a = l1; b = l2; }
            }
            double s = 20;
            while (s > TileNameMin && Math.Max(Gfx.Width(a, s, Gfx.UiBold), Gfx.Width(b, s, Gfx.UiBold)) > width) s -= 1;
            Gfx.Text(dc, a, cx, y, s, 0xFFFFFFFF, 0.5, Gfx.UiBold);
            Gfx.Text(dc, b, cx, y + s * 1.12, s, 0xFFFFFFFF, 0.5, Gfx.UiBold);
        }

        // ---------------------------------------------------------------- the kit's pieces
        /// <summary>A rounded kit control: dark blue-grey with an ink outline and a drop shadow; selected = a gold border,
        /// focused = a cyan ring outside, both = gold fill + cyan ring, pressed = orange and 3 units down.</summary>
        void KitControl(DrawingContext dc, Rect r, bool focus, bool selected, bool pressed, bool shadow)
        {
            var rr = pressed ? new Rect(r.X, r.Y + 3, r.Width, r.Height) : r;
            if (shadow && !pressed) dc.DrawRoundedRectangle(Gfx.B(FighterInk.Ink), null, new Rect(r.X, r.Y + 4, r.Width, r.Height), 16, 16);
            uint top = pressed ? PressOrange : selected && focus ? KitPickTop : focus ? 0xFF34426A : 0xFF2A3550;
            uint bottom = pressed ? 0xFFEA580C : selected && focus ? KitPickBottom : focus ? 0xFF222B44 : 0xFF1A2133;
            dc.DrawRoundedRectangle(Vertical(top, bottom), Gfx.P(selected ? SelGold : FighterInk.Ink, selected ? 3.5 : 3), rr, 16, 16);
            if (focus) dc.DrawRoundedRectangle(null, Gfx.P(FocusCyan, 3.5), new Rect(rr.X - 5, rr.Y - 5, rr.Width + 10, rr.Height + 10), 20, 20);
        }

        static Brush Vertical(uint top, uint bottom)
        {
            var b = new LinearGradientBrush { StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative), EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative) };
            b.GradientStops.Add(new GradientStop(Color.FromUInt32(top), 0));
            b.GradientStops.Add(new GradientStop(Color.FromUInt32(bottom), 1));
            return b;
        }

        static List<string> LinesFor(string text, double size, double width)
        {
            var lines = new List<string>();
            string line = "";
            foreach (string w in text.Split(' '))
            {
                string t = line.Length == 0 ? w : line + " " + w;
                if (Gfx.Width(t, size, Gfx.Ui) > width && line.Length > 0) { lines.Add(line); line = w; }
                else line = t;
            }
            if (line.Length > 0) lines.Add(line);
            return lines;
        }

        /// <summary>Text in a box (x is its left, or its middle with align 0.5; y its top): the biggest size from max down to
        /// min that fits on one line, else two lines at the size that fits, centred in the box's height.</summary>
        static void FitText(DrawingContext dc, string text, double x, double y, double width, double height, double max, double min, uint colour, Typeface tf, double align)
        {
            for (double s = max; s >= min; s -= 1)
                if (Gfx.Width(text, s, tf) <= width) { Gfx.Text(dc, text, x, y + Math.Max(0, (height - s * 1.25) / 2), s, colour, align, tf); return; }
            for (double s = max; s >= min - 3; s -= 1)
            {
                List<string> lines = new List<string>();
                string line = "";
                foreach (string w in text.Split(' '))
                {
                    string t = line.Length == 0 ? w : line + " " + w;
                    if (Gfx.Width(t, s, tf) > width && line.Length > 0) { lines.Add(line); line = w; } else line = t;
                }
                if (line.Length > 0) lines.Add(line);
                bool fits = lines.Count <= 2;
                foreach (string l in lines) if (Gfx.Width(l, s, tf) > width) fits = false;
                if (fits || s <= min - 3)
                {
                    double yy = y + Math.Max(0, (height - lines.Count * s * 1.2) / 2);
                    foreach (string l in lines) { Gfx.Text(dc, l, x, yy, s, colour, align, tf); yy += s * 1.2; }
                    return;
                }
            }
        }
    }
}
