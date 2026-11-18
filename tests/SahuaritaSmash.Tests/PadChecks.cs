// Game controllers reported as events (Android's Bluetooth and USB gamepads; Wpf\PadInput.cs): pretend key codes and
// axis values become the right KartPad fields, dead zones, the d-pad hat, triggers, controller numbers for two
// controllers, a disconnect freeing its number, touch and a controller together, and a pretend controller driving a
// fighter and the menus.
using System;
using System.Linq;
using SahuaritaSmash.Views;

namespace ClassicGames
{
    public static partial class Tests
    {
        public static void PadChecks()
        {
            var r = new KartPadInput();
            const int padA = 7, padB = 12;

            // nothing until someone uses the controller: a resting stick doesn't claim a number
            r.Axis(padA, KartPadInput.AxisX, 0.1); r.Axis(padA, KartPadInput.AxisY, -0.05);
            Check("pads: a controller that has only reported resting sticks isn't anyone's yet", r.Read(0) == null && r.SlotOf(padA) == -1, "");
            r.Key(padA, KartPadInput.KeyA, true);
            KartPad p = r.Read(0);
            Check("pads: pressing A makes it controller 1, with A down and the resting stick at 0",
                p != null && p.On && p.A && !p.B && p.X == 0 && p.Y == 0 && r.SlotOf(padA) == 0, p == null ? "null" : p.X + " " + p.Y);
            Check("pads: it says so once (\"Controller 1 connected\")", r.TakeNews() == "Controller 1 connected" && r.TakeNews() == null, "");
            r.Key(padA, KartPadInput.KeyA, false);
            Check("pads: letting go of A lets go", !r.Read(0).A, "");

            // every button
            int[] codes = { KartPadInput.KeyA, KartPadInput.KeyB, KartPadInput.KeyX, KartPadInput.KeyY, KartPadInput.KeyL1, KartPadInput.KeyR1, KartPadInput.KeyStart, KartPadInput.KeySelect, KartPadInput.KeyDpadUp, KartPadInput.KeyDpadDown, KartPadInput.KeyDpadLeft, KartPadInput.KeyDpadRight };
            Func<KartPad, bool>[] fields = { q => q.A, q => q.B, q => q.XB, q => q.YB, q => q.LB, q => q.RB, q => q.Start, q => q.Back, q => q.Up, q => q.Down, q => q.Left, q => q.Right };
            bool each = true; string wrong = "";
            for (int i = 0; i < codes.Length; i++)
            {
                r.Key(padA, codes[i], true);
                KartPad q = r.Read(0);
                for (int j = 0; j < fields.Length; j++) if (fields[j](q) != (i == j)) { each = false; wrong += codes[i] + "->" + j + " "; }
                r.Key(padA, codes[i], false);
            }
            Check("pads: each button and d-pad key sets its own field and no other (A B X Y, L1 R1, Start Select, the d-pad)", each, wrong);
            r.Key(padA, KartPadInput.KeyThumbL, true);
            Check("pads: a stick click is taken (no field) and sets nothing", !fields.Any(f => f(r.Read(0))), "");
            r.Key(padA, KartPadInput.KeyThumbL, false);
            Check("pads: only controller key codes are taken (not Back 4, Enter 66, volume 24, letters 29)",
                !KartPadInput.IsPadKey(4) && !KartPadInput.IsPadKey(66) && !KartPadInput.IsPadKey(24) && !KartPadInput.IsPadKey(29) && codes.All(KartPadInput.IsPadKey), "");

            // sticks: dead zone, Android's down = + turned into XInput's up = +, full push = 1
            r.Axis(padA, KartPadInput.AxisX, 0.2); r.Axis(padA, KartPadInput.AxisY, -0.24);
            p = r.Read(0);
            Check("pads: a stick inside the dead zone reads 0", p.X == 0 && p.Y == 0, p.X + " " + p.Y);
            r.Axis(padA, KartPadInput.AxisX, 1); r.Axis(padA, KartPadInput.AxisY, -1);
            p = r.Read(0);
            Check("pads: the left stick pushed right and up reads X 1, Y 1 (up is +, as on Windows)", p.X == 1 && p.Y == 1, p.X + " " + p.Y);
            r.Axis(padA, KartPadInput.AxisX, -0.5); r.Axis(padA, KartPadInput.AxisY, 0.6);
            p = r.Read(0);
            Check("pads: half way left and down reads about -0.55 and -0.66", Math.Abs(p.X + 0.5 / 0.915) < 1e-9 && Math.Abs(p.Y + 0.6 / 0.915) < 1e-9, p.X + " " + p.Y);
            r.Axis(padA, KartPadInput.AxisX, 0); r.Axis(padA, KartPadInput.AxisY, 0);
            r.Axis(padA, KartPadInput.AxisZ, 0.9); r.Axis(padA, KartPadInput.AxisRz, 0.9);
            p = r.Read(0);
            Check("pads: the right stick is Z / RZ: right and down reads RX +, RY - (strong attacks)", p.RX > 0.9 && p.RY < -0.9 && p.X == 0, p.RX + " " + p.RY);
            r.Axis(padA, KartPadInput.AxisZ, 0); r.Axis(padA, KartPadInput.AxisRz, 0);

            // the d-pad as a hat
            r.Axis(padA, KartPadInput.AxisHatX, -1); r.Axis(padA, KartPadInput.AxisHatY, -1);
            p = r.Read(0);
            Check("pads: a d-pad reported as a hat: left and up", p.Left && p.Up && !p.Right && !p.Down, "");
            r.Axis(padA, KartPadInput.AxisHatX, 1); r.Axis(padA, KartPadInput.AxisHatY, 1);
            p = r.Read(0);
            Check("pads: ... right and down", p.Right && p.Down && !p.Left && !p.Up, "");
            r.Axis(padA, KartPadInput.AxisHatX, 0); r.Axis(padA, KartPadInput.AxisHatY, 0);
            p = r.Read(0);
            Check("pads: ... and back in the middle", !p.Right && !p.Down && !p.Left && !p.Up, "");

            // triggers: either Android name, or the L2 / R2 keys of controllers without trigger axes
            r.Axis(padA, KartPadInput.AxisLTrigger, 0.6); r.Axis(padA, KartPadInput.AxisGas, 0.8);
            p = r.Read(0);
            Check("pads: triggers from LTRIGGER and GAS read 0.6 and 0.8", Math.Abs(p.LT - 0.6) < 1e-9 && Math.Abs(p.RT - 0.8) < 1e-9, p.LT + " " + p.RT);
            r.Axis(padA, KartPadInput.AxisLTrigger, 0); r.Axis(padA, KartPadInput.AxisGas, 0);
            r.Axis(padA, KartPadInput.AxisBrake, 0.4); r.Axis(padA, KartPadInput.AxisRTrigger, 1.3);
            p = r.Read(0);
            Check("pads: ... from BRAKE and RTRIGGER (kept to 0..1)", Math.Abs(p.LT - 0.4) < 1e-9 && p.RT == 1, p.LT + " " + p.RT);
            r.Axis(padA, KartPadInput.AxisBrake, 0); r.Axis(padA, KartPadInput.AxisRTrigger, 0);
            r.Key(padA, KartPadInput.KeyL2, true); r.Key(padA, KartPadInput.KeyR2, true);
            p = r.Read(0);
            Check("pads: ... and the L2 / R2 buttons count as fully pressed", p.LT == 1 && p.RT == 1, "");
            r.Key(padA, KartPadInput.KeyL2, false); r.Key(padA, KartPadInput.KeyR2, false);
            Check("pads: let go, the triggers are 0", r.Read(0).LT == 0 && r.Read(0).RT == 0, "");

            // a second controller: number 2, its own buttons
            r.Axis(padB, KartPadInput.AxisX, 0.8);
            p = r.Read(1);
            Check("pads: a second controller pushed is controller 2, with its own stick", p != null && p.X > 0.8 && r.Read(0).X == 0 && r.SlotOf(padB) == 1, "");
            Check("pads: ... and it says so", r.TakeNews() == "Controller 2 connected", "");
            Check("pads: no third controller yet", r.Read(2) == null && r.Read(3) == null && r.Read(4) == null && r.Read(-1) == null, "");

            // a disconnect frees the number; the next controller picked up takes the lowest free one
            r.Gone(padA);
            Check("pads: controller 1 switched off: slot 1 is empty, controller 2 stays 2", r.Read(0) == null && r.Read(1) != null && r.TakeNews() == "Controller 1 disconnected", "");
            r.Key(33, KartPadInput.KeyStart, true);
            Check("pads: the next controller picked up becomes controller 1", r.SlotOf(33) == 0 && r.Read(0).Start, "");
            // a fifth controller finds no room
            r.Key(40, KartPadInput.KeyA, true); r.Key(41, KartPadInput.KeyA, true); r.Key(42, KartPadInput.KeyA, true);
            Check("pads: four controllers at most (a fifth waits for a free number)", r.SlotOf(40) == 2 && r.SlotOf(41) == 3 && r.SlotOf(42) == -1, "");

            // going away noticed by asking (Android: InputDevice.GetDevice(id) == null), about once a second
            long now = 5000; bool padBThere = true;
            r.Now = () => now;
            r.Exists = id => id != padB || padBThere;
            r.Read(0);
            padBThere = false;
            now += 200; r.Read(0);
            bool stillThere = r.SlotOf(padB) == 1;
            now += 1000; r.Read(0);
            Check("pads: a controller that no longer exists is let go within about a second", stillThere && r.SlotOf(padB) == -1 && r.Read(1) == null, "");
            r.Reset();
            Check("pads: Reset forgets them all", r.Read(0) == null && r.Read(2) == null && r.TakeNews() == null, "");

            // touch and a controller together are one person's controller 1
            var touch = new KartPad { On = true, A = true, X = 0.3 };
            var pad = new KartPad { On = true, B = true, X = -0.9, RT = 0.7 };
            KartPad both = KartPadInput.Merge(touch, pad);
            Check("pads: touch + controller: both one's buttons count and the stick pushed further wins", both.A && both.B && both.X == -0.9 && both.RT == 0.7 && both.On, "");

            // through KartPads, the way the game reads them
            KartPads.Input.Reset();
            KartPads.Fake[0] = null;
            KartPads.Input.Key(3, KartPadInput.KeyB, true);
            Check("pads: KartPads.Read(0) gives the controller (as XInput's controller 1 on Windows)", KartPads.Read(0).On && KartPads.Read(0).B, "");
            var touchPad = new KartPad { On = true, A = true };
            KartPads.Fake[0] = touchPad;
            Check("pads: with touch in use as controller 1 both work together", KartPads.Read(0).A && KartPads.Read(0).B, "");
            KartPads.Input.Reset();
            Check("pads: with no controller, touch is controller 1 exactly as before", ReferenceEquals(KartPads.Read(0), touchPad), "");
            KartPads.Fake[0] = null;

            // a pretend controller in a match: walk, jump, attack; and the menus
            Audio.MusicOn = false;
            var g = new BrawlGame { Scores = new ScoreBook(System.IO.Path.Combine(SahuaritaSmash.Services.Data.Dir, "pads-scores.txt")), Player = "Kim", Rng = new Random(5) };
            g.KeysFileOverride = System.IO.Path.Combine(SahuaritaSmash.Services.Data.Dir, "pads-keys.txt");
            var view = new GameView { Game = g, TouchOn = false };
            g.Begin();
            Frame(view, g);
            const int dev = 21;
            var input = KartPads.Input;
            input.Key(dev, KartPadInput.KeyDpadDown, true);
            Frame(view, g);
            input.Key(dev, KartPadInput.KeyDpadDown, false);
            Frame(view, g);
            string toast = g.ToastText;
            Check("pads: the first press shows \"Controller 1 connected\" in the game", toast == "Controller 1 connected", toast ?? "none");
            g.Who[0] = 1; g.Who[1] = 1; g.Who[2] = 0; g.Who[3] = 0; g.Pick[0] = 0; g.Pick[1] = 1; g.StagePick = 0; g.Level[1] = 0;
            g.NewGame();
            for (int i = 0; i < 60 * 3; i++) Frame(view, g);
            BFighter me = g.Sim.F[0];
            double x0 = me.X;
            input.Axis(dev, KartPadInput.AxisX, 1);
            for (int i = 0; i < 40; i++) Frame(view, g);
            input.Axis(dev, KartPadInput.AxisX, 0);
            Check("pads: the left stick right walks the fighter right", me.X > x0 + 40, x0.ToString("0") + " -> " + me.X.ToString("0"));
            for (int i = 0; i < 30; i++) Frame(view, g);
            double y0 = me.Y, top = y0;
            input.Key(dev, KartPadInput.KeyX, true);
            for (int i = 0; i < 30; i++) { Frame(view, g); top = Math.Max(top, me.Y); }
            input.Key(dev, KartPadInput.KeyX, false);
            Check("pads: X jumps", top > y0 + 40, y0.ToString("0") + " -> " + top.ToString("0"));
            for (int i = 0; i < 60; i++) Frame(view, g);
            input.Key(dev, KartPadInput.KeyA, true);
            bool attacked = false;
            for (int i = 0; i < 12 && !attacked; i++) { Frame(view, g); attacked = me.St == BS.Attack; }
            input.Key(dev, KartPadInput.KeyA, false);
            Check("pads: A attacks", attacked, me.St.ToString());
            for (int i = 0; i < 40; i++) Frame(view, g);
            input.Key(dev, KartPadInput.KeyStart, true);
            Frame(view, g);
            input.Key(dev, KartPadInput.KeyStart, false);
            Frame(view, g);
            Check("pads: Start pauses", g.Paused, "");
            g.Resume();
            input.Gone(dev);
            for (int i = 0; i < 3; i++) Frame(view, g);
            Check("pads: switched off mid-match: the game says so and controller 1 is empty", g.ToastText == "Controller 1 disconnected" && !KartPads.Read(0).On, g.ToastText ?? "none");
            KartPads.Input.Reset();
            KartPads.Fake[0] = null;
            Audio.StopMusic(); Audio.StopEffects();
        }
    }
}
