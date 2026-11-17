// Touch controls for a fight (Android, and a Windows touch screen once it has been touched): a stick that appears where
// the left thumb lands, five buttons for the right thumb and a pause button. They pretend to be controller 1
// (KartPads.Fake[0]), which Brawl.cs already reads like a person's keys, so the game itself is unchanged: the stick
// moves (up jumps, down drops, as the keys do), Attack = A, Special = B, Strong = left trigger, Shield = RB, Grab = LB,
// pause = Start. (0.1.1, Andrew: no Jump button, the stick already jumps. Shield stays on the right, big and next to
// Attack: Shield + the stick rolls and dodges, which needs both thumbs.) Sizes are in the screen's own units (at least 56 across, so a thumb can't miss), drawn
// over the game, never under a finger's way to the menus: menus get every tap as before.
using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using ClassicGames;

namespace SahuaritaSmash.Views
{
    public sealed class TouchControls
    {
        public sealed class Button
        {
            public string Label; public Point Center; public double R; public uint Color; public bool Down;
        }

        public readonly KartPad Pad = new KartPad { On = true };
        public readonly List<Button> Buttons = new List<Button>();
        public readonly Button Pause = new Button { Label = "II", Color = 0xFF94A3B8 };
        /// <summary>The stick: where the thumb landed (its centre) and where it is now; null centre = no thumb.</summary>
        public Point? StickHome; public Point StickAt;
        public double StickReach = 70;
        readonly Dictionary<long, Button> held = new Dictionary<long, Button>();
        long stickPointer = -1;
        Size size;
        /// <summary>The button size (Touch controls page: Small 0.85, Normal 1, Large 1.25; Services\TouchSettings.cs).</summary>
        public double Scale = 1;
        /// <summary>Left-handed: the stick on the right half, the buttons mirrored to the left.</summary>
        public bool LeftHanded;
        double laidScale = -1; bool laidLefty;

        public TouchControls()
        {
            foreach (var b in new[] {
                new Button { Label = "Attack", Color = 0xFFEF4444 },
                new Button { Label = "Shield", Color = 0xFF8B5CF6 },
                new Button { Label = "Special", Color = 0xFF3B82F6 },
                new Button { Label = "Strong", Color = 0xFFF59E0B },
                new Button { Label = "Grab", Color = 0xFF14B8A6 } }) Buttons.Add(b);
        }

        /// <summary>Places everything for a screen of this size (landscape): the buttons in an arc by the right edge.</summary>
        public void Layout(Size s)
        {
            if (s == size && Scale == laidScale && LeftHanded == laidLefty) return;
            size = s; laidScale = Scale; laidLefty = LeftHanded;
            double u = Math.Max(1, Math.Min(s.Width, s.Height) / 400.0);   // 1 on a 400-tall phone screen
            double big = Math.Max(36, 40 * u) * Scale, small = Math.Max(28, 31 * u) * Scale, pad = 14 * u;
            double rx = s.Width - pad, by = s.Height - pad;
            // a large size on a narrow screen: as big as still fits on its half; never under 44 across
            double room = s.Width * 0.5 - pad - 4, need = big * 5.45 + small;
            if (need > room) { big *= room / need; small *= room / need; }
            big = Math.Max(22, big); small = Math.Max(22, small);
            // Attack and Shield big along the bottom, where the thumb rests; Special, Strong and Grab above them
            double mid = (big + small) / 2;
            Place(Buttons[0], rx - big * 1.3, by - big * 1.3, big);           // Attack
            Place(Buttons[1], rx - big * 3.75, by - big * 1.1, mid);          // Shield
            Place(Buttons[2], rx - big * 1.2, by - big * 3.65, small);        // Special
            Place(Buttons[3], rx - big * 3.25, by - big * 3.35, small);       // Strong
            Place(Buttons[4], rx - big * 5.45, by - big * 2.6, small);        // Grab
            if (LeftHanded) foreach (Button b in Buttons) b.Center = new Point(s.Width - b.Center.X, b.Center.Y);
            double pr = Math.Max(22, small * 0.8);
            Place(Pause, s.Width / 2, pad + pr, pr);
            StickReach = 70 * u * Math.Max(1, Scale);
        }

        static void Place(Button b, double x, double y, double r) { b.Center = new Point(x, y); b.R = r; }

        Button ButtonAt(Point p)
        {
            Button best = null; double bd = double.MaxValue;
            foreach (Button b in Buttons)
            {
                double d = Distance(p, b.Center);
                if (d <= b.R * 1.25 && d < bd) { best = b; bd = d; }   // a little forgiving: thumbs are wide
            }
            if (best == null && Distance(p, Pause.Center) <= Pause.R * 1.3) best = Pause;
            return best;
        }

        static double Distance(Point a, Point b) { double dx = a.X - b.X, dy = a.Y - b.Y; return Math.Sqrt(dx * dx + dy * dy); }

        /// <summary>A finger came down: a button, or (on the left half) the stick. True when the controls took it.</summary>
        public bool Down(long id, Point p)
        {
            Button b = ButtonAt(p);
            if (b != null) { held[id] = b; b.Down = true; Update(); return true; }
            if ((LeftHanded ? p.X > size.Width * 0.5 : p.X < size.Width * 0.5) && stickPointer < 0) { stickPointer = id; StickHome = p; StickAt = p; Update(); return true; }
            return false;
        }

        /// <summary>A finger moved: the stick follows; a finger sliding off a button onto another presses that one.</summary>
        public bool Move(long id, Point p)
        {
            if (id == stickPointer) { StickAt = p; Update(); return true; }
            Button was;
            if (!held.TryGetValue(id, out was)) return false;
            Button now = ButtonAt(p);
            if (now != null && now != was && now != Pause && was != Pause) { was.Down = StillHeld(was, id); held[id] = now; now.Down = true; Update(); }
            return true;
        }

        /// <summary>A finger lifted.</summary>
        public bool Up(long id)
        {
            if (id == stickPointer) { stickPointer = -1; StickHome = null; Update(); return true; }
            Button b;
            if (!held.TryGetValue(id, out b)) return false;
            held.Remove(id);
            b.Down = StillHeld(b, -1);
            Update();
            return true;
        }

        bool StillHeld(Button b, long except) { foreach (var kv in held) if (kv.Key != except && kv.Value == b) return true; return false; }

        /// <summary>Every finger let go (the fight ended, the app went to the back).</summary>
        public void Clear()
        {
            held.Clear(); stickPointer = -1; StickHome = null;
            foreach (Button b in Buttons) b.Down = false;
            Pause.Down = false;
            Update();
        }

        void Update()
        {
            double x = 0, y = 0;
            if (StickHome.HasValue)
            {
                x = (StickAt.X - StickHome.Value.X) / StickReach; y = -(StickAt.Y - StickHome.Value.Y) / StickReach;   // up is +1, as the game's
                double len = Math.Sqrt(x * x + y * y);
                if (len > 1) { x /= len; y /= len; }
                if (len < 0.25) { x = 0; y = 0; }   // a small dead zone, so resting a thumb doesn't walk
            }
            Pad.X = x; Pad.Y = y;
            Pad.A = Buttons[0].Down; Pad.RB = Buttons[1].Down; Pad.B = Buttons[2].Down;
            Pad.LT = Buttons[3].Down ? 1 : 0; Pad.LB = Buttons[4].Down;
            Pad.Start = Pause.Down;
        }

        /// <summary>Draws the controls over the game (screen units), see-through so the fight shows under them.</summary>
        public void Draw(Avalonia.Media.DrawingContext dc)
        {
            var face = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.Bold);
            foreach (Button b in Buttons) DrawButton(dc, b, face);
            DrawButton(dc, Pause, face);
            if (StickHome.HasValue)
            {
                Point h = StickHome.Value;
                dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(50, 255, 255, 255)), new Pen(new SolidColorBrush(Color.FromArgb(120, 255, 255, 255)), 2), h, StickReach, StickReach);
                Point knob = new Point(h.X + Pad.X * StickReach, h.Y - Pad.Y * StickReach);
                dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(150, 255, 255, 255)), null, knob, StickReach * 0.42, StickReach * 0.42);
            }
            else
            {
                // where the stick will be: a faint ring and a word, so a first-timer knows
                Point hint = new Point(size.Width * (LeftHanded ? 0.86 : 0.14), size.Height * 0.72);
                dc.DrawEllipse(null, new Pen(new SolidColorBrush(Color.FromArgb(70, 255, 255, 255)), 2), hint, StickReach, StickReach);
                var t = new FormattedText("Move", System.Globalization.CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, face, 15, new SolidColorBrush(Color.FromArgb(140, 255, 255, 255)));
                dc.DrawText(t, new Point(hint.X - t.Width / 2, hint.Y - t.Height / 2));
            }
        }

        static void DrawButton(Avalonia.Media.DrawingContext dc, Button b, Typeface face)
        {
            byte a = (byte)(b.Down ? 210 : 120);
            Color c = WpfBrush.C(b.Color);
            dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(a, c.R, c.G, c.B)), new Pen(new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)), b.Down ? 3 : 1.5), b.Center, b.R, b.R);
            double fs = Math.Max(11, b.R * (b.Label.Length > 5 ? 0.36 : 0.42));
            var t = new FormattedText(b.Label, System.Globalization.CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, face, fs, Brushes.White);
            dc.DrawText(t, new Point(b.Center.X - t.Width / 2, b.Center.Y - t.Height / 2));
        }
    }
}
