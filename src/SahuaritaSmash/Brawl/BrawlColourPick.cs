// Sahuarita Smash! (Sauce Edition): choosing colours and unlocking them. Climbing the ladder with a fighter unlocks its
// second colour (on Hard, its third too), saved per player in brawl-unlocks.txt next to the scores (BrawlUnlocks.cs);
// the ladder's end says "New colour unlocked for X!". The fight screen, the ladder and practice show a row of colour
// chips under the chosen fighter (80 game units square: 44+ pixels on a phone; a locked one says how to get it), and C
// changes the colour from the keyboard. Two seats with the same fighter get different colours when they can. Colours
// are pictures only (BrawlView.Colours, BrawlColours.cs). Online matches use the usual colours.
using System;
using System.Collections.Generic;
using System.IO;
using Avalonia;
using Avalonia.Input;

namespace ClassicGames
{
    public sealed partial class BrawlGame
    {
        /// <summary>Each seat's colour on the fight screen (0 = the usual one).</summary>
        public readonly int[] Colour = new int[4];
        public int LadderColour;
        /// <summary>For tests: where the unlocked colours are saved when there is no scorebook.</summary>
        public string UnlocksFileOverride;
        BrawlUnlocks unlocks;
        string UnlocksFile
        {
            get { return UnlocksFileOverride ?? (Scores != null && Scores.Path != null ? Path.Combine(Path.GetDirectoryName(Scores.Path), "brawl-unlocks.txt") : null); }
        }
        /// <summary>The unlocked colours (opened the first time they are needed).</summary>
        public BrawlUnlocks Unlocks
        {
            get
            {
                string f = UnlocksFile;
                if (unlocks == null || !unlocksInMemory && unlocks.Path != f) unlocks = new BrawlUnlocks(f);
                return unlocks;
            }
        }
        /// <summary>What the ladder's end says about colours just unlocked (null: nothing new).</summary>
        public string UnlockNote;
        int unlockFighter = -1, unlockFrom;
        int colourSeat;

        bool HasColour(int fighter, int c)
        {
            return fighter >= 0 && fighter < BrawlData.Fighters.Count && c >= 0 && c < BrawlUnlocks.Colours && Unlocks.Has(Player, BrawlData.Fighters[fighter].Id, c);
        }
        /// <summary>The colour if it is unlocked, else the usual one.</summary>
        int UsableColour(int fighter, int c) { return HasColour(fighter, c) ? c : 0; }
        /// <summary>The next unlocked colour after c (d = +1 or -1), round and round.</summary>
        int NextColour(int fighter, int c, int d)
        {
            for (int k = 1; k <= BrawlUnlocks.Colours; k++)
            {
                int n = ((c + d * k) % BrawlUnlocks.Colours + BrawlUnlocks.Colours) % BrawlUnlocks.Colours;
                if (HasColour(fighter, n)) return n;
            }
            return 0;
        }

        /// <summary>The colours of a match's fighters, in order: each the one wanted if it is unlocked; a fighter already
        /// in the match in that colour gets another unlocked one if there is one.</summary>
        public int[] MatchColours(IList<int> fighters, IList<int> wanted)
        {
            var got = new int[fighters.Count];
            for (int i = 0; i < fighters.Count; i++)
            {
                int f = fighters[i], c = UsableColour(f, i < wanted.Count ? wanted[i] : 0);
                Func<int, bool> taken = cc => { for (int j = 0; j < i; j++) if (fighters[j] == f && got[j] == cc) return true; return false; };
                if (taken(c)) for (int k = 0; k < BrawlUnlocks.Colours; k++) if (HasColour(f, k) && !taken(k)) { c = k; break; }
                got[i] = c;
            }
            return got;
        }

        /// <summary>Puts the colours of a match just set up (Reset) on the view.</summary>
        void SetMatchColours(IList<BFighterDef> defs, IList<int> seats, IList<int> who)
        {
            Array.Clear(View.Colours, 0, View.Colours.Length);
            var fighters = new List<int>(); var wanted = new List<int>();
            for (int i = 0; i < defs.Count; i++)
            {
                fighters.Add(BrawlData.FighterIndex(defs[i].Id));
                wanted.Add(Ladder ? (i == 0 ? LadderColour : 0) : seats[i] >= 0 && seats[i] < 4 ? Colour[seats[i]] : 0);
            }
            int[] got = MatchColours(fighters, wanted);
            for (int i = 0; i < got.Length && i < View.Colours.Length; i++) View.Colours[i] = got[i];
        }

        /// <summary>The ladder is climbed: unlock this fighter's colours (the second; on Hard the third too).</summary>
        void UnlockForLadder()
        {
            BFighterDef d = BrawlData.Fighters[LadderPick];
            int before = Unlocks.Level(Player, d.Id);
            int n = Unlocks.Unlock(Player, d.Id, LadderDiff >= 2 ? 2 : 1);
            UnlockNote = null;
            if (n <= 0) return;
            unlockFighter = LadderPick; unlockFrom = before + 1;
            UnlockNote = n == 1 ? "New colour unlocked for " + d.Name + "!" : "Two new colours unlocked for " + d.Name + "!";
        }

        /// <summary>The ladder's end: the usual panel, and above it the colours just unlocked.</summary>
        protected override void DrawOver(DrawingContext dc)
        {
            base.DrawOver(dc);
            if (UnlockNote == null || !Ladder || unlockFighter < 0) return;
            BFighterDef d = BrawlData.Fighters[unlockFighter];
            int level = Unlocks.Level(Player, d.Id), count = Math.Max(1, level - unlockFrom + 1);
            var names = new List<string>();
            for (int c = unlockFrom; c <= level; c++) names.Add(BrawlColours.Name(d, c));
            string line2 = string.Join(" and ", names.ToArray()) + ": pick " + (count > 1 ? "them" : "it") + " under " + d.Name + " when you choose fighters.";
            string line3 = level < 2 ? "Climb the ladder on Hard for one more." : "";
            double textW = Math.Max(Gfx.Width(UnlockNote, 26, Gfx.UiBold), Math.Max(Gfx.Width(line2, 15, Gfx.Ui), Gfx.Width(line3, 15, Gfx.Ui)));
            double w = Math.Min(Width - 20, 40 + count * 100 + textW + 30);
            var r = new Rect((Width - w) / 2, 8, w, 146);
            dc.DrawRoundedRectangle(Gfx.B(0xF0161226), Gfx.P(0xFFFDE047, 3), r, 18, 18);
            for (int c = unlockFrom, i = 0; c <= level; c++, i++)
            {
                double x = r.X + 70 + i * 100;
                dc.DrawEllipse(Gfx.B(Gfx.Alpha(BrawlColours.MainOf(d, c), 0.35)), null, new Point(x, r.Y + 74), 46, 46);
                View.Portrait(dc, BrawlColours.Def(d, c), x, r.Y + 74, 74, T + c, true, BS.Idle);
            }
            double tx = r.X + 40 + count * 100;
            Gfx.Text(dc, UnlockNote, tx, r.Y + 30, 26, 0xFFFDE047, 0, Gfx.UiBold);
            Gfx.Text(dc, line2, tx, r.Y + 76, 15, 0xFFE5E7EB, 0, Gfx.Ui);
            if (line3.Length > 0) Gfx.Text(dc, line3, tx, r.Y + 100, 15, 0xFFB8BFCC, 0, Gfx.Ui);
        }

        /// <summary>How to unlock colour c of a fighter, in words.</summary>
        static string HowToUnlock(BFighterDef d, int c)
        {
            return c == 1 ? "Climb the ladder with " + d.Name + " to unlock this colour." : "Climb the ladder on Hard with " + d.Name + " to unlock this colour.";
        }

        /// <summary>A row of colour chips (the fighter's head in each colour): tap one to choose it. Returns the y below.</summary>
        double DrawColourChips(DrawingContext dc, double x, double y, int fighter, int current, Action<int> set, bool labels = true, double gap = 12)
        {
            if (fighter < 0 || fighter >= BrawlData.Fighters.Count) return y;
            BFighterDef d = BrawlData.Fighters[fighter];
            if (labels) Gfx.Text(dc, "Colour", x, y - 28, 17, 0xFF93C5FD, 0, Gfx.UiBold);
            double s = BigButton;
            for (int c = 0; c < BrawlUnlocks.Colours; c++)
            {
                var r = new Rect(x + c * (s + gap), y, s, s);
                bool has = HasColour(fighter, c), sel = has && c == current;
                bool lit = Hovered(r);
                // the menu kit's chip (Test 003 round 2): an ink outline; chosen = a thick gold ring outside and a tick;
                // locked = darkened, with the padlock
                dc.DrawRoundedRectangle(Gfx.B(sel ? 0xFF243B6B : has ? (lit ? 0xFF22304F : 0xE0172036) : 0xE00A0C12), Gfx.P(0xFF161616, 3), r, 14, 14);
                if (sel) dc.DrawRoundedRectangle(null, Gfx.P(0xFFFDC93A, 4.5), new Rect(r.X - 5, r.Y - 5, s + 10, s + 10), 18, 18);
                if (has)
                {
                    dc.DrawEllipse(Gfx.B(Gfx.Alpha(BrawlColours.MainOf(d, c), 0.3)), null, new Point(r.X + s / 2, r.Y + s / 2), s * 0.4, s * 0.4);
                    View.Portrait(dc, BrawlColours.Def(d, c), r.X + s / 2, r.Y + s / 2, s * 0.72, T + c * 0.4, true, BS.Idle);
                }
                else DrawLock(dc, r.X + s / 2, r.Y + s / 2, BrawlColours.MainOf(d, c));
                if (sel)
                {
                    dc.DrawEllipse(Gfx.B(0xFFFDC93A), Gfx.P(0xFF161616, 2), new Point(r.Right - 8, r.Y + 8), 12, 12);
                    Gfx.Text(dc, "\u2713", r.Right - 8, r.Y - 3, 15, 0xFF161616, 0.5, Gfx.UiBold);
                }
                int cc = c;
                AddHit(r, "Colour " + (c + 1) + (has ? "" : " (locked)"), false, null, () =>
                {
                    if (HasColour(fighter, cc)) { set(cc); Audio.Ui("select"); }
                    else { Toast(HowToUnlock(d, cc)); Audio.Ui("back"); }
                });
            }
            if (labels) Gfx.Text(dc, ChipHint(fighter, current), x, y + s + 8, 14, 0xFFB8BFCC, 0, Gfx.Ui);
            return y + s + 30;
        }

        /// <summary>The chosen colour's name, and how to get more if some are still locked.</summary>
        string ChipHint(int fighter, int current)
        {
            BFighterDef d = BrawlData.Fighters[fighter];
            string under = BrawlColours.Name(d, UsableColour(fighter, current));
            int next = -1;
            for (int c = 1; c < BrawlUnlocks.Colours; c++) if (!HasColour(fighter, c)) { next = c; break; }
            return under + (next > 0 ? "  \u00B7  " + (next == 1 ? "climb the ladder for more" : "climb it on Hard for one more") : "");
        }

        static void DrawLock(DrawingContext dc, double cx, double cy, uint tint)
        {
            dc.DrawEllipse(Gfx.B(Gfx.Alpha(tint, 0.18)), null, new Point(cx, cy), 30, 30);
            var body = new Rect(cx - 13, cy - 4, 26, 20);
            dc.DrawRoundedRectangle(null, Gfx.P(0xFF8C93A5, 4), new Rect(cx - 8, cy - 18, 16, 22), 8, 8);
            dc.DrawRoundedRectangle(Gfx.B(0xFF8C93A5), null, body, 4, 4);
            dc.DrawEllipse(Gfx.B(0xFF1A1E27), null, new Point(cx, cy + 5), 3, 3);
        }

        // ---------------------------------------------------------------- hooks into the fight and ladder screens
        /// <summary>The fight screen: C changes the colour of the seat being chosen for.</summary>
        bool SelectColourKey(Key k)
        {
            if (k != Key.C) return false;
            int seat = Sel < 8 ? Sel / 2 : colourSeat;
            if (Pick[seat] < 0) return true;
            Colour[seat] = NextColour(Pick[seat], Colour[seat], 1);
            Audio.Ui("move");
            return true;
        }

        /// <summary>The ladder screen: C changes the colour.</summary>
        bool LadderColourKey(Key k)
        {
            if (k != Key.C) return false;
            LadderColour = NextColour(LadderPick, LadderColour, 1);
            Audio.Ui("move");
            return true;
        }

        /// <summary>The fight screen's chips: for the seat being chosen for (or the last one), under the rows.</summary>
        void DrawSelectColours(DrawingContext dc)
        {
            if (Sel < 8) colourSeat = Sel / 2;
            int seat = colourSeat;
            if (Pick[seat] < 0 || Who[seat] == 0) return;
            // Test 003 round 2: the chips 80 x 80, 20 apart, at (32, 560); the label under them
            DrawColourChips(dc, 32, 560, Pick[seat], UsableColour(Pick[seat], Colour[seat]), c => Colour[seat] = c, false, 20);
            Gfx.Text(dc, "COLOUR \u2014 P" + (seat + 1) + ": " + BrawlData.Fighters[Pick[seat]].Name.ToUpperInvariant(), 32, 646, 20, SeatColour[seat], 0, Gfx.UiBold);
            Gfx.Text(dc, ChipHint(Pick[seat], Colour[seat]), 32, 670, 15, 0xFFB8BFCC, 0, Gfx.Ui);
        }

        void DrawLadderColours(DrawingContext dc)
        {
            DrawColourChips(dc, 40, 552, LadderPick, UsableColour(LadderPick, LadderColour), c => LadderColour = c);
        }
    }
}
