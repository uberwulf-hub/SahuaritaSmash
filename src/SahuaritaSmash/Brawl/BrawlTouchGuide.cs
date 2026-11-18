// Playing on a phone (Sauce Edition, 2026-11-17, Andrew plays on an Android phone):
//  - the pause panel's right side as a phone guide when the person plays by touch (Game.TouchMode): a small drawing of
//    the touch layout and this fighter's moves the touch way, in text big enough for a phone (24+ game units);
//  - the Touch controls page (from the title's "Touch controls" while playing by touch): button size, left-handed,
//    vibration, the guide again, all saved per device in touch.txt (Services\TouchSettings.cs);
//  - the first-time guide: four big cards over the paused fight, the first time a touch player starts a fight;
//  - haptics: a short buzz when the person's own fighter lands a hit or gets hit (App.Haptic, Android only), read from
//    the match's events after each step, never from inside BrawlSim.
using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Input;
using SahuaritaSmash.Services;
using SahuaritaSmash.Views;

namespace ClassicGames
{
    public sealed partial class BrawlGame
    {
        // ================================================================ whose fighter the touch controls drive
        /// <summary>The slot the touch controls (controller 1) play: online this device's person, else the first person
        /// (-1: the computer plays everyone).</summary>
        public int TouchSlot()
        {
            if (Sim == null || Autopilot) return -1;
            if (Online != null && Online.Match != null && Net != null)
            {
                foreach (int s in Online.Match.MySeats()) if (s < Sim.F.Length) return s;
                return -1;
            }
            if (bots == null || seatOf == null) return -1;
            for (int i = 0; i < Sim.F.Length && i < bots.Length; i++)
                if (bots[i] == null && (Ladder || HumanIndex(seatOf[i]) == 0)) return i;
            return -1;
        }

        // ================================================================ haptics
        double lastBuzz = -1;
        /// <summary>The milliseconds of the last buzz asked for (the checks).</summary>
        public int LastBuzzMs;
        /// <summary>After each step: a short buzz when the person's fighter lands a hit (longer for a big hit or a bonk off
        /// the stage) or is hit, with Vibration on, at most one every 120 ms.</summary>
        public void Feel()
        {
            Action<int> buzz = SahuaritaSmash.App.Haptic;
            if (buzz == null || !TouchMode || Sim == null || !TouchSettings.Current.Vibration) return;
            int me = TouchSlot();
            if (me < 0) return;
            int ms = 0;
            foreach (BrEvent e in Sim.Events)
            {
                if (e.S < 0 || e.S >= Sim.F.Length) continue;
                BFighter v = Sim.F[e.S];
                bool mine = e.S != me && v.Last == me;
                if (e.K == "hit")
                {
                    bool strong = e.A > 150 || mine && Sim.F[me].Mv != null && Sim.F[me].Mv.Id.EndsWith("strong", StringComparison.Ordinal);
                    if (e.S == me) ms = Math.Max(ms, e.A > 150 ? 60 : 35);
                    else if (mine) ms = Math.Max(ms, strong ? 50 : e.A > 70 ? 30 : 20);
                }
                else if (e.K == "ko")
                {
                    if (e.S == me) ms = Math.Max(ms, 120);
                    else if (mine && v.LastT < 480) ms = Math.Max(ms, 80);
                }
            }
            if (ms == 0) return;
            if (lastBuzz >= 0 && T - lastBuzz < 0.12 && T >= lastBuzz) return;
            lastBuzz = T; LastBuzzMs = ms;
            try { buzz(ms); } catch (Exception ex) { CrashLog.Write("the phone's buzz", ex, false); }
        }

        // ================================================================ the touch layout, drawn small
        static readonly Size GuidePhone = new Size(915, 412);
        /// <summary>The touch controls as the person has them (size, hand), drawn into area: the phone, the stick's place
        /// and the five buttons. light = the buttons to light up (by label), null = all.</summary>
        public static void DrawTouchLayout(DrawingContext dc, Rect area, bool labels, ICollection<string> light, bool stickLit)
        {
            var ts = TouchSettings.Current;
            var t = new TouchControls { Scale = ts.Scale, LeftHanded = ts.LeftHanded };
            t.Layout(GuidePhone);
            double k = area.Width / GuidePhone.Width;
            dc.DrawRoundedRectangle(Gfx.B(0xFF1A2333), Gfx.P(0xFF64748B, 3), new Rect(area.X - 10, area.Y - 10, area.Width + 20, area.Height + 20), 22, 22);
            dc.DrawRoundedRectangle(Gfx.B(0xFF0E1A2C), null, area, 10, 10);
            // a hint of the fight: the ground and two fighters' heads
            dc.DrawRectangle(Gfx.B(0xFF3F3A2E), null, new Rect(area.X + area.Width * 0.3, area.Y + area.Height * 0.42, area.Width * 0.4, area.Height * 0.04));
            Func<double, double, Point> at = (x, y) => new Point(area.X + x * k, area.Y + y * k);
            Point stick = at(GuidePhone.Width * (ts.LeftHanded ? 0.86 : 0.14), GuidePhone.Height * 0.72);
            double reach = t.StickReach * k;
            uint stickC = stickLit ? 0xFFFDE047 : 0xB0FFFFFF;
            dc.DrawEllipse(Gfx.B(stickLit ? 0x40FDE047u : 0x30FFFFFFu), Gfx.P(stickC, stickLit ? 3 : 2), stick, reach, reach);
            dc.DrawEllipse(Gfx.B(stickLit ? 0xC0FDE047 : 0x90FFFFFF), null, stick, reach * 0.42, reach * 0.42);
            foreach (TouchControls.Button b in t.Buttons)
            {
                bool lit = light == null || light.Contains(b.Label);
                Point c = at(b.Center.X, b.Center.Y);
                double r = b.R * k;
                dc.DrawEllipse(Gfx.B(lit ? b.Color : Gfx.Alpha(b.Color, 0.3)), Gfx.P(lit ? 0xFFFFFFFF : 0x60FFFFFF, lit && light != null ? 3 : 1.5), c, r, r);
                if (labels) Gfx.Text(dc, b.Label, c.X, c.Y - r * 0.32, Math.Max(10, r * (b.Label.Length > 5 ? 0.56 : 0.64)), lit ? 0xFFFFFFFF : 0x80FFFFFF, 0.5, Gfx.UiBold);
            }
            Point pause = at(t.Pause.Center.X, t.Pause.Center.Y);
            dc.DrawEllipse(Gfx.B(0x8094A3B8), null, pause, t.Pause.R * k, t.Pause.R * k);
        }

        /// <summary>A line of text made smaller (down to 16) until it fits in maxW. Returns its width.</summary>
        static double FitText(DrawingContext dc, string s, double x, double y, double size, double maxW, uint color, Avalonia.Media.Typeface tf)
        {
            while (size > 16 && Gfx.Width(s, size, tf) > maxW) size -= 1;
            return Gfx.Text(dc, s, x, y + 0, size, color, 0, tf);
        }

        /// <summary>Text broken into lines of at most maxW, each drawn centred at cx. Returns the y below it.</summary>
        static double Wrapped(DrawingContext dc, string s, double cx, double y, double size, double maxW, uint color, double lineH)
        {
            var lines = new List<string>();
            string line = "";
            foreach (string w in s.Split(' '))
            {
                string next = line.Length == 0 ? w : line + " " + w;
                if (line.Length > 0 && Gfx.Width(next, size, Gfx.Ui) > maxW) { lines.Add(line); line = w; }
                else line = next;
            }
            if (line.Length > 0) lines.Add(line);
            foreach (string l in lines) { Gfx.Text(dc, l, cx, y, size, color, 0.5, Gfx.Ui); y += lineH; }
            return y;
        }

        static uint ButtonColor(string label)
        {
            switch (label)
            {
                case "Attack": return 0xFFF87171;
                case "Shield": return 0xFFA78BFA;
                case "Special": return 0xFF60A5FA;
                case "Strong": return 0xFFFBBF24;
                case "Grab": return 0xFF2DD4BF;
            }
            return 0xFFFDE047;
        }

        // ================================================================ the pause panel's phone guide
        /// <summary>The right side of the pause panel while playing by touch: the layout and this fighter's moves the
        /// touch way, in big text (the 720-tall picture is about 400 pixels tall on a phone).</summary>
        void DrawTouchPauseGuide(DrawingContext dc, double x, double y, double right, double bottom)
        {
            dc.DrawRectangle(Gfx.B(0x60FB923C), null, new Rect(x - 28, y + 6, 1.5, bottom - y - 12));
            string L = ((char)0x2190).ToString(), R = ((char)0x2192).ToString(), U = ((char)0x2191).ToString(), D = ((char)0x2193).ToString();
            int me = TouchSlot();
            BFighterDef d = me >= 0 ? Sim.F[me].D : null;
            double w = Gfx.Text(dc, "Playing by touch", x, y, 28, PauseHead, 0, Gfx.UiBold);
            if (d != null) FitText(dc, "as " + d.Name, x + w + 16, y + 3, 24, right - x - w - 16, PauseText, Gfx.Ui);
            y += 48;
            // the layout, small, on the left; the specials beside it
            var pic = new Rect(x + 10, y + 10, 250, 250 * GuidePhone.Height / GuidePhone.Width);
            DrawTouchLayout(dc, pic, false, null, false);
            double sx = pic.Right + 34, sy = y;
            Gfx.Text(dc, d != null ? "Specials" : "The computer is playing", sx, sy, 24, PauseHead, 0, Gfx.UiBold);
            sy += 36;
            if (d != null)
            {
                string[] how = { "Special", L + R + " + Special", U + " + Special", D + " + Special" };
                for (int k = 0; k < 4; k++)
                {
                    Gfx.Text(dc, how[k], sx, sy, 24, ButtonColor("Special"), 0, Gfx.UiBold);
                    FitText(dc, d.SpecialNames[k] ?? "", sx + 172, sy, 24, right - sx - 172, PauseText, Gfx.Ui);
                    sy += 34;
                }
            }
            y = Math.Max(pic.Bottom + 26, sy + 8);
            // the moves the touch way: what to press, then what it does
            string[,] rows = {
                { "Stick " + U, "jump, and again in the air" },
                { "Stick " + D, "drop through a platform" },
                { "Attack", "+ stick: side, up, down attacks" },
                { "Strong", "your big hit: hold to charge" },
                { "Shield", "block; + stick rolls away" },
                { "Grab", "grab and throw (beats shields)" },
                { U + " + Special", "fall off? it flies you back" } };
            for (int r = 0; r < rows.GetLength(0) && y + 30 < bottom; r++)
            {
                string key = rows[r, 0];
                Gfx.Text(dc, key, x, y, 26, key.StartsWith("Stick", StringComparison.Ordinal) ? 0xFFE5E7EB : ButtonColor(key.EndsWith("Special", StringComparison.Ordinal) ? "Special" : key), 0, Gfx.UiBold);
                FitText(dc, rows[r, 1], x + 180, y, 26, right - x - 180, PauseText, Gfx.Ui);
                y += 41;
            }
        }

        // ================================================================ the Touch controls page
        public int TouchRow;
        int TouchRows { get { return OperatingSystem.IsAndroid() ? 5 : 6; } }   // size, hand, vibration, guide, (keys), back
        const int RowGuide = 3;

        /// <summary>The title's Controls item: the Touch controls page while playing by touch, else the keys.</summary>
        void OpenControls()
        {
            if (TouchMode) { Menu = Screen.Touch; TouchRow = 0; guideCard = -1; }
            else { Menu = Screen.Keys; KeyRow = 0; KeyCol = 0; KeyListening = false; }
        }
        string ControlsItem { get { return TouchMode ? "Touch controls" : "Controls (change the keys)"; } }

        void ChangeTouch(int row, int d)
        {
            var ts = TouchSettings.Current;
            if (row == 0) ts.Size = Math.Max(0, Math.Min(2, ts.Size + d));
            else if (row == 1) ts.LeftHanded = !ts.LeftHanded;
            else if (row == 2) { ts.Vibration = !ts.Vibration; if (ts.Vibration && SahuaritaSmash.App.Haptic != null) SahuaritaSmash.App.Haptic(40); }
            else return;
            Audio.Ui("move");
            if (!ts.Save() && ts.Unreadable) Toast("Not saved: the old settings file couldn't be read");
        }

        void TouchRowEnter(int row)
        {
            if (row <= 2) { ChangeTouch(row, row == 0 ? (TouchSettings.Current.Size == 2 ? -2 : 1) : 1); return; }
            Audio.Ui("select");
            if (row == RowGuide) { guideCard = 0; guideFromFight = false; return; }
            if (row == 4 && TouchRows == 6) { Menu = Screen.Keys; KeyRow = 0; KeyCol = 0; KeyListening = false; return; }
            Menu = Screen.Title; Sel = 4;
        }

        void TouchPageKey(Key k)
        {
            if (GuideShowing) { GuideKey(k); return; }
            if (IsUp(k) || IsDown(k)) { TouchRow = MenuMove(k, TouchRow, TouchRows); Audio.Ui("move"); return; }
            if ((IsLeft(k) || IsRight(k)) && TouchRow <= 2) { ChangeTouch(TouchRow, IsLeft(k) ? -1 : 1); return; }
            if (k == Key.Enter || k == Key.Space) TouchRowEnter(TouchRow);
        }

        /// <summary>Esc / Back on the page: closes the guide if it is open, else back to the title.</summary>
        bool TouchPageBack()
        {
            if (GuideShowing) { guideCard = -1; return true; }
            Menu = Screen.Title; Sel = 4; return true;
        }

        void DrawTouchPage(DrawingContext dc)
        {
            var ts = TouchSettings.Current;
            Gfx.Panel(dc, new Rect(60, 30, Width - 120, Height - 60), 0xF0101522, 0xFFFB923C);
            Gfx.Text(dc, "Touch controls", Width / 2, 44, 34, 0xFFFDE047, 0.5, Gfx.UiBold);
            string[] labels = { "Button size", "Left-handed", "Vibration", "Show the guide again", "Keyboard keys", "Back" };
            string[] values = { TouchSettings.SizeNames[ts.Size], ts.LeftHanded ? "On" : "Off", ts.Vibration ? "On" : "Off" };
            double x = 100, y = 104, rw = 620, rh = BigButton;
            for (int i = 0; i < TouchRows; i++)
            {
                int row = i, label = i == TouchRows - 1 ? 5 : i;
                bool sel = TouchRow == row;
                var r = new Rect(x, y, rw, rh);
                dc.DrawRoundedRectangle(Gfx.B(sel ? 0xFF1B2D50 : 0xFF13161D), Gfx.P(sel ? 0xFF3B82F6 : 0xFF252A35, 2), r, 16, 16);
                AddHit(r, labels[label], sel, () => TouchRow = row, () => { TouchRow = row; TouchRowEnter(row); });
                if (row <= 2)
                {
                    Gfx.Text(dc, labels[label], x + 24, y + rh / 2 - 16, 26, 0xFFFFFFFF, 0, Gfx.UiBold);
                    var lb = new Rect(x + 300, y, 90, rh); var rb = new Rect(x + rw - 90, y, 90, rh);
                    foreach (var ab in new[] { lb, rb })
                        dc.DrawRoundedRectangle(Gfx.B(0xFF22304D), Gfx.P(0xFF3B82F6, 1.5), new Rect(ab.X + 8, ab.Y + 8, ab.Width - 16, ab.Height - 16), 12, 12);
                    Gfx.Text(dc, "<", lb.X + lb.Width / 2, y + rh / 2 - 20, 32, 0xFF93C5FD, 0.5, Gfx.UiBold);
                    Gfx.Text(dc, ">", rb.X + rb.Width / 2, y + rh / 2 - 20, 32, 0xFF93C5FD, 0.5, Gfx.UiBold);
                    Gfx.Text(dc, values[row], (lb.Right + rb.X) / 2, y + rh / 2 - 16, 26, 0xFFFDE047, 0.5, Gfx.UiBold);
                    AddHit(lb, labels[label] + " <", sel, () => TouchRow = row, () => { TouchRow = row; ChangeTouch(row, -1); });
                    AddHit(rb, labels[label] + " >", sel, () => TouchRow = row, () => { TouchRow = row; ChangeTouch(row, 1); });
                }
                else Gfx.Text(dc, labels[label], x + rw / 2, y + rh / 2 - 16, 26, sel ? 0xFFFFFFFF : 0xFFB8BFCC, 0.5, Gfx.UiBold);
                y += rh + 10;
            }
            // how it looks, with the person's own choices
            var pic = new Rect(780, 130, 400, 400 * GuidePhone.Height / GuidePhone.Width);
            DrawTouchLayout(dc, pic, true, null, false);
            double ty = pic.Bottom + 28;
            ty = Wrapped(dc, ts.LeftHanded ? "Left-handed: the stick on the right, the buttons on the left." : "The stick on the left, the buttons on the right.", pic.X + pic.Width / 2, ty, 22, 420, 0xFFE5E7EB, 30);
            ty = Wrapped(dc, ts.Vibration ? "Vibration: a short buzz when you land a hit or get hit." : "Vibration is off.", pic.X + pic.Width / 2, ty + 6, 22, 420, 0xFFB8BFCC, 30);
            if (ts.Note != null) Wrapped(dc, ts.Note, pic.X + pic.Width / 2, ty + 6, 18, 420, 0xFFFCA5A5, 24);
            if (GuideShowing) DrawGuide(dc);
        }

        // ================================================================ the first-time guide
        int guideCard = -1;
        bool guideFromFight, guideAsked;
        /// <summary>The checks: let the guide come up (it never does in the checks otherwise).</summary>
        public static bool GuideInTests;
        public bool GuideShowing { get { return guideCard >= 0; } }
        public int GuideCard { get { return guideCard; } }
        public const int GuideCards = 4;

        /// <summary>At the start of a fight (Tick): the first time a touch player fights, the guide over the paused
        /// fight. True when it just came up.</summary>
        bool GuideCheck()
        {
            if (guideAsked || GuideShowing || !TouchMode || Net != null || Autopilot || Sim == null) return false;
            if (SahuaritaSmash.App.Testing && !GuideInTests) return false;
            if (TouchSettings.Current.GuideDone || TouchSlot() < 0) return false;
            guideAsked = true;
            guideCard = 0; guideFromFight = true;
            Pause();
            return true;
        }

        void GuideNext() { Audio.Ui("select"); if (guideCard + 1 >= GuideCards) GuideFinished(); else guideCard++; }

        void GuideFinished()
        {
            guideCard = -1;
            var ts = TouchSettings.Current;
            if (!ts.GuideDone) { ts.GuideDone = true; ts.Save(); }
            if (guideFromFight) { guideFromFight = false; Resume(); }
        }

        void GuideKey(Key k)
        {
            if (k == Key.Enter || k == Key.Space || IsRight(k)) GuideNext();
            else if (IsLeft(k) && guideCard > 0) { guideCard--; Audio.Ui("move"); }
            else if (k == Key.Escape || k == Key.Back || k == Key.P || k == Key.Q) { Audio.Ui("back"); GuideFinished(); }
        }

        /// <summary>Keys while the guide shows (called first by PausedKey in BrawlPractice.cs).</summary>
        bool GuidePausedKey(Key k)
        {
            if (!GuideShowing) return false;
            GuideKey(k);
            return true;
        }

        void DrawGuide(DrawingContext dc)
        {
            var ts = TouchSettings.Current;
            string thumb = ts.LeftHanded ? "right" : "left", other = ts.LeftHanded ? "left" : "right";
            string[] titles = { "Move and jump", "Attack and Special", "Shield", "Fly back" };
            string[] texts = {
                "Slide your " + thumb + " thumb to move. Push up to jump; push down to drop through a platform.",
                "Attack and Special are on the " + other + ". Tap them with the stick pushed for other moves. Strong is your big hit.",
                "Hold Shield to block. Shield + a direction rolls you away.",
                "Fall off the stage? Push up and tap Special to fly back." };
            string[][] lights = { new string[0], new[] { "Attack", "Special", "Strong" }, new[] { "Shield" }, new[] { "Special" } };
            int c = Math.Max(0, Math.Min(GuideCards - 1, guideCard));
            dc.DrawRectangle(Gfx.B(0xC0000000), null, new Rect(0, 0, Width, Height));
            var card = new Rect(110, 40, Width - 220, Height - 80);
            Gfx.Panel(dc, card, 0xF8101522, 0xFFFB923C);
            Gfx.Text(dc, (c + 1) + " of " + GuideCards, card.Right - 30, card.Y + 22, 22, 0xFF8C93A5, 1, Gfx.Ui);
            Gfx.Text(dc, titles[c], Width / 2, card.Y + 22, 40, 0xFFFDE047, 0.5, Gfx.UiBold);
            var pic = new Rect(Width / 2 - 280, card.Y + 90, 560, 560 * GuidePhone.Height / GuidePhone.Width);
            DrawTouchLayout(dc, pic, true, lights[c], c != 1);
            Wrapped(dc, texts[c], Width / 2, pic.Bottom + 30, 30, card.Width - 120, 0xFFFFFFFF, 40);
            // the buttons, big for thumbs: Skip (not on the last card) and Next / Got it
            double by = card.Bottom - BigButton - 24, bw = 300;
            bool last = c == GuideCards - 1;
            var next = new Rect(card.Right - 40 - bw, by, bw, BigButton);
            dc.DrawRoundedRectangle(Gfx.B(0xFF1D4ED8), Gfx.P(0xFF93C5FD, 2), next, 18, 18);
            Gfx.Text(dc, last ? "Got it" : "Next", next.X + bw / 2, by + BigButton / 2 - 17, 30, 0xFFFFFFFF, 0.5, Gfx.UiBold);
            AddHit(next, last ? "Got it" : "Next", false, null, GuideNext);
            if (!last)
            {
                var skip = new Rect(card.X + 40, by, bw, BigButton);
                dc.DrawRoundedRectangle(Gfx.B(0xFF172036), Gfx.P(0xFF2C3A55, 2), skip, 18, 18);
                Gfx.Text(dc, "Skip", skip.X + bw / 2, by + BigButton / 2 - 17, 30, 0xFFB8BFCC, 0.5, Gfx.UiBold);
                AddHit(skip, "Skip", false, null, () => { Audio.Ui("back"); GuideFinished(); });
            }
        }
    }
}
