// Sahuarita Smash! (Sauce Edition): the practice room (the title's "Practice"). Pick your fighter, its colour, someone to
// practise on and a stage; then a match nobody loses: falling off brings you straight back (BrawlSim.Practice), no
// timer, no scores. The dummy stands still unless you ask it to walk, jump, shield or fight back (at Easy); the pause
// menu changes that, puts everyone's damage back to 0%, turns stage surprises on or off and shows or hides your moves
// list, which is drawn at the top right while you play (your specials by name, your attacks and how to do them).
// Leaving goes back to the title. The dummy's buttons are worked out from the match (no clock, no random numbers), and
// nothing here is ever saved: the ladder's scores are untouched.
using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Input;

namespace ClassicGames
{
    public sealed partial class BrawlGame
    {
        /// <summary>The match on screen is a practice one.</summary>
        public bool Practice;
        public int PracticePick, PracticeFoe = 1, PracticeStage, PracticeColour;
        /// <summary>What the dummy does: 0 stands, 1 walks, 2 jumps, 3 shields, 4 fights back (Easy).</summary>
        public int DummyMode;
        public bool PracticeSurprises, ShowMoves = true;
        public static readonly string[] DummyModes = { "Stands still", "Walks", "Jumps", "Shields", "Fights back (Easy)" };
        int dummyDir = 1;
        BrawlBot dummyBot;
        const int PracticeRows = 7;   // fighter, colour, dummy, what it does, stage, surprises, start

        // ---------------------------------------------------------------- the setup screen
        void OpenPractice()
        {
            Menu = Screen.Practice; Sel = PracticeRows - 1;
            if (PracticeFoe == PracticePick) PracticeFoe = (PracticePick + 1) % BrawlData.Fighters.Count;
        }

        void PracticeKey(Key k)
        {
            if (IsUp(k) || IsDown(k)) { Sel = MenuMove(k, Sel, PracticeRows); return; }
            int d = IsLeft(k) ? -1 : IsRight(k) ? 1 : 0;
            if (d != 0) { PracticeChange(Sel, d); Audio.Ui("move"); return; }
            if (k == Key.C) { PracticeChange(1, 1); Audio.Ui("move"); return; }
            if (k != Key.Enter && k != Key.Space) return;
            if (Sel == PracticeRows - 1) { Audio.Ui("select"); StartPractice(); }
            else { PracticeChange(Sel, 1); Audio.Ui("move"); }
        }

        void PracticeChange(int row, int d)
        {
            int n = BrawlData.Fighters.Count, ns = BrawlData.Stages.Count;
            switch (row)
            {
                case 0: PracticePick = (PracticePick + d + n) % n; PracticeColour = UsableColour(PracticePick, PracticeColour); break;
                case 1: PracticeColour = NextColour(PracticePick, PracticeColour, d); break;
                case 2: PracticeFoe = (PracticeFoe + d + n) % n; break;
                case 3: DummyMode = (DummyMode + d + DummyModes.Length) % DummyModes.Length; break;
                case 4: PracticeStage = (PracticeStage + d + ns) % ns; break;
                case 5: PracticeSurprises = !PracticeSurprises; break;
            }
        }

        void StartPractice()
        {
            Ladder = false; Practice = true;
            NewGame();
        }

        /// <summary>Reset for a practice match: you (a person, any keys or controller 1) and the dummy.</summary>
        void ResetPractice()
        {
            Ladder = false;
            var defs = new List<BFighterDef> { BrawlData.Fighters[PracticePick], BrawlData.Fighters[PracticeFoe] };
            StageIndex = Math.Max(0, Math.Min(BrawlData.Stages.Count - 1, PracticeStage));
            Sim = new BrawlSim(BrawlData.Stages[StageIndex], defs, 1, 0, (uint)Rng.Next(1, int.MaxValue));
            Sim.Practice = true;
            Sim.Surprises = PracticeSurprises;
            bots = new BrawlBot[2];
            if (Autopilot) bots[0] = new BrawlBot(0, 5);
            seatOf = new[] { 0, 1 };
            Names = new[] { Player, "Dummy" };
            View.Tags[0] = "P1"; View.Tags[1] = "DUMMY";
            Array.Clear(View.Colours, 0, View.Colours.Length);
            View.Colours[0] = UsableColour(PracticePick, PracticeColour);
            if (PracticeFoe == PracticePick && View.Colours[0] == 0) View.Colours[1] = UsableColour(PracticeFoe, 1) == 1 ? 1 : 0;
            dummyBot = new BrawlBot(1, BotLevels[0]);
            dummyDir = 1;
            View.Fx.Clear(); View.Follow(Sim, true);
            Step = Stage.Countdown;
            stageT = 0; acc = 0; resultsDone = false;
        }

        /// <summary>Everyone's buttons for one practice step: yours as usual, the dummy's from what it was asked to do.</summary>
        void PracticeInputs(BrInput[] inputs)
        {
            inputs[0] = bots[0] != null ? bots[0].Think(Sim) : Autopilot ? new BrInput() : Read(0);
            inputs[1] = DummyInput();
        }

        /// <summary>The dummy's buttons (the checks call it too): worked out from the match only.</summary>
        public BrInput DummyInput()
        {
            BFighter f = Sim.F[1];
            var i = new BrInput();
            switch (DummyMode)
            {
                case 1:
                    // walks back and forth along the main stage, turning near the edges
                    if (f.X > Sim.Main.R - 140) dummyDir = -1; else if (f.X < Sim.Main.L + 140) dummyDir = 1;
                    i.X = 0.45 * dummyDir;
                    break;
                case 2:
                    // a jump now and then (a short press: the next one is a new press)
                    i.Jump = Sim.Frame % 70 < 4;
                    break;
                case 3:
                    i.Shield = true;
                    break;
                case 4:
                    i = dummyBot.Think(Sim);
                    break;
            }
            // anything but standing: off the stage it comes back the way a computer player would
            if (DummyMode != 0 && DummyMode != 4 && (!f.Ground || f.St == BS.Ledge) && Math.Abs(f.X) > Sim.Main.R - 20) i = dummyBot.Think(Sim);
            return i;
        }

        /// <summary>Everyone's damage back to 0% (the pause menu's "Damage back to 0%").</summary>
        public void ResetPracticeDamage()
        {
            if (Sim == null || !Practice) return;
            foreach (BFighter f in Sim.F) f.Dmg = 0;
            Toast("Damage back to 0%");
        }

        void SetPracticeSurprises(bool on)
        {
            PracticeSurprises = on;
            if (Sim == null || !Practice) return;
            Sim.Surprises = on;
            if (!on) Sim.HazPhase = 0;   // a surprise under way stops at once
        }

        // ---------------------------------------------------------------- the pause menu
        // keys: Esc / P keep playing, D what the dummy does, Z damage back to 0%, S surprises, H the moves list, Q leave
        protected override bool PausedKey(Key k)
        {
            if (!Practice || Net != null) return false;
            switch (k)
            {
                case Key.D: DummyMode = (DummyMode + 1) % DummyModes.Length; Audio.Ui("move"); return true;
                case Key.Z: ResetPracticeDamage(); Audio.Ui("select"); return true;
                case Key.S: SetPracticeSurprises(!PracticeSurprises); Audio.Ui("move"); return true;
                case Key.H: ShowMoves = !ShowMoves; Audio.Ui("move"); return true;
                case Key.Q: case Key.Back: Menu = Screen.Title; Sel = 2; Practice = false; return false;   // the game base leaves (Quit)
            }
            return false;
        }

        void DrawPracticePause(DrawingContext dc)
        {
            double w = 1180, h = 680, x = (Width - w) / 2, y = (Height - h) / 2, menuW = 420;
            dc.DrawRectangle(Gfx.B(0xA0000000), null, new Rect(0, 0, Width, Height));
            Gfx.Panel(dc, new Rect(x, y, w, h), 0xF0101522, 0xFF34D399);
            Gfx.Text(dc, "Practice", x + 30 + menuW / 2, y + 16, 34, 0xFF6EE7B7, 0.5, Gfx.UiBold);
            string[] labels = { "Keep practising", "Dummy: " + DummyModes[DummyMode], "Damage back to 0%", "Stage surprises: " + (PracticeSurprises ? "On" : "Off"), ShowMoves ? "Hide the moves list" : "Show the moves list", "Leave practice" };
            BigButtons(dc, x + 30, y + 70, menuW, labels, new[] { "P / Esc", "D", "Z", "S", "H", "Q" }, new[] { Key.Escape, Key.D, Key.Z, Key.S, Key.H, Key.Q });
            dc.DrawRectangle(Gfx.B(0x6034D399), null, new Rect(x + menuW + 52, y + 20, 1.5, h - 40));
            double below = DrawMovesList(dc, BrawlData.Fighters[PracticePick], x + menuW + 80, y + 22, x + w - 30, 17, 22, true);
            string L = ((char)0x2190).ToString(), R = ((char)0x2192).ToString(), D = ((char)0x2193).ToString();
            Gfx.Text(dc, "Hold Strong to charge it.  Shield + " + L + " / " + R + " rolls, Shield + " + D + " dodges.  Grabs go through shields.", x + menuW + 80, below + 16, 15, 0xFF8C93A5, 0, Gfx.Ui);
        }

        /// <summary>A fighter's moves: the specials by their names, then the attacks (BrawlSignatures.HowTo) by name.
        /// Returns the y below them.</summary>
        double DrawMovesList(DrawingContext dc, BFighterDef d, double x, double y, double right, double size, double row, bool full)
        {
            string L = ((char)0x2190).ToString(), R = ((char)0x2192).ToString(), U = ((char)0x2191).ToString(), D = ((char)0x2193).ToString();
            Gfx.Text(dc, d.Name + "'s moves", x, y, size + 5, 0xFFFDE047, 0, Gfx.UiBold);
            y += row * 1.5;
            string[] how = { "Special", L + " / " + R + " + Special", U + " + Special" + (full ? " (back to the stage)" : ""), D + " + Special" };
            string[] ids = full ? BrawlSignatures.AttackIds : new[] { "ftilt", "fstrong", "ustrong", "dstrong" };
            double col2 = x;
            foreach (string hh in how) col2 = Math.Max(col2, x + Gfx.Width(hh, size - 1, Gfx.Ui) + size);
            foreach (string id in ids) col2 = Math.Max(col2, x + Gfx.Width(BrawlSignatures.HowTo(id), size - 1, Gfx.Ui) + size);
            Gfx.Text(dc, "Specials", x, y, size, 0xFF93C5FD, 0, Gfx.UiBold);
            y += row;
            for (int k = 0; k < 4; k++)
            {
                Gfx.Text(dc, how[k], x, y, size - 1, 0xFF93C5FD, 0, Gfx.Ui);
                Gfx.Text(dc, d.SpecialNames[k] ?? "", col2, y, size - 1, 0xFFFFFFFF, 0, Gfx.UiBold);
                y += row;
            }
            y += row * 0.4;
            Gfx.Text(dc, full ? "Attacks" : "Attacks (all of them: in the menu)", x, y, size, 0xFF93C5FD, 0, Gfx.UiBold);
            y += row;
            foreach (string id in ids)
            {
                BMove m = d.M(id);
                if (m == null) continue;
                Gfx.Text(dc, BrawlSignatures.HowTo(id), x, y, size - 1, 0xFF93C5FD, 0, Gfx.Ui);
                Gfx.Text(dc, m.Name ?? "", col2, y, size - 1, 0xFFFFFFFF, 0, Gfx.UiBold);
                y += row;
            }
            return y;
        }

        /// <summary>While practising: a small note at the top left, and the moves list at the top right.</summary>
        void DrawPracticeHud(DrawingContext dc)
        {
            if (Paused) return;
            string note = "Practice  \u00B7  Dummy: " + DummyModes[DummyMode].ToLowerInvariant() + "  \u00B7  " + (TouchMode ? "Back" : "Esc") + ": options";
            double w = Gfx.Width(note, 16, Gfx.UiBold) + 32;
            dc.DrawRoundedRectangle(Gfx.B(0xB0101522), Gfx.P(0x8034D399, 1.2), new Rect(14, 14, w, 32), 16, 16);
            Gfx.Text(dc, note, 30, 20, 16, 0xFFA7F3D0, 0, Gfx.UiBold);
            if (!ShowMoves) return;
            var r = new Rect(Width - 434, 14, 420, 312);
            dc.DrawRoundedRectangle(Gfx.B(0x800B0F1A), Gfx.P(0x4034D399, 1), r, 14, 14);
            DrawMovesList(dc, Sim.F[0].D, r.X + 14, r.Y + 8, r.Right - 10, 18, 25, false);
        }

        // ---------------------------------------------------------------- drawing the setup screen
        void DrawPracticeSetup(DrawingContext dc)
        {
            Gfx.Text(dc, "Practice", 150, 18, 34, 0xFF6EE7B7, 0, Gfx.UiBold);
            Gfx.Text(dc, "Try your moves on a dummy. Nobody loses: fall off and you're straight back. No timer, no scores.", 40, 62, 17, 0xFFE5E7EB, 0, Gfx.Ui);
            BFighterDef me = BrawlData.Fighters[PracticePick], foe = BrawlData.Fighters[PracticeFoe];
            int col = UsableColour(PracticePick, PracticeColour);
            View.Portrait(dc, BrawlColours.Def(me, col), 830, 470, 230 * Math.Min(1.25, 90 / me.Height * 1.1), T, false, BS.Idle);
            View.Portrait(dc, foe, 1110, 470, 170 * Math.Min(1.25, 90 / foe.Height * 1.1), T + 1.3, false, BS.Idle);
            Gfx.Text(dc, "You", 830, 486, 20, 0xFFFFFFFF, 0.5, Gfx.UiBold);
            Gfx.Text(dc, "Dummy", 1110, 486, 20, 0xFFB8BFCC, 0.5, Gfx.UiBold);
            string[] labels = { "Your fighter", "Colour", "Practise on", "The dummy", "Stage", "Stage surprises", "" };
            string[] values = { me.Name, BrawlColours.Name(me, col), foe.Name, DummyModes[DummyMode], BrawlData.Stages[PracticeStage].Name, PracticeSurprises ? "On" : "Off", "" };
            for (int row = 0; row < PracticeRows; row++)
            {
                double y = 96 + row * 82;
                int rowI = row;
                if (row == PracticeRows - 1)
                {
                    var br = new Rect(30, y + 6, 340, BigButton);
                    bool lit = Sel == row || Hovered(br);
                    dc.DrawRoundedRectangle(Gfx.B(lit ? 0xFF059669 : 0xC0131A2A), Gfx.P(lit ? 0xFF6EE7B7 : 0xFF2C3A55, 2), br, 26, 26);
                    Gfx.Text(dc, "Practise!  \u25B6", br.X + br.Width / 2, br.Y + BigButton / 2 - 15, 26, 0xFFFFFFFF, 0.5, Gfx.UiBold);
                    AddHit(br, "Practise", Sel == row, () => Sel = rowI, () => { Audio.Ui("select"); StartPractice(); });
                    continue;
                }
                var rr = new Rect(30, y, 600, 78);
                dc.DrawRoundedRectangle(Gfx.B(Sel == row ? 0xFF1E3A8A : 0xB0131A2A), Gfx.P(Sel == row ? 0xFF60A5FA : 0xFF2C3A55, 1.5), rr, 12, 12);
                Gfx.Text(dc, labels[row], 46, y + 26, 20, 0xFF93C5FD, 0, Gfx.UiBold);
                Gfx.Text(dc, "\u25C0", 250, y + 26, 22, 0xFF93C5FD, 0.5, Gfx.UiBold);
                Gfx.Text(dc, values[row], 410, y + 26, 20, 0xFFFFFFFF, 0.5, Gfx.Ui);
                Gfx.Text(dc, "\u25B6", 590, y + 26, 22, 0xFF93C5FD, 0.5, Gfx.UiBold);
                AddHit(rr, labels[row] + " " + values[row], Sel == row, () => Sel = rowI, () => { Sel = rowI; TapKey(Key.Right); });
                AddWideArrowHits(rr, 250, 590, labels[row], row, Sel == row, () => Sel = rowI);
            }
            DrawColourChips(dc, 680, 560, PracticePick, col, c => PracticeColour = c);
        }

        /// <summary>Arrows wide enough for a thumb (80 game units: 44+ pixels on a small phone).</summary>
        void AddWideArrowHits(Rect row, double leftX, double rightX, string label, int item, bool selected, Action select)
        {
            double aw = 80;
            AddHit(new Rect(leftX - aw / 2, row.Y, aw, row.Height), label + " <", selected, select, () => TapKey(Key.Left)).Item = item;
            AddHit(new Rect(rightX - aw / 2, row.Y, aw, row.Height), label + " >", selected, select, () => TapKey(Key.Right)).Item = item;
        }
    }
}
