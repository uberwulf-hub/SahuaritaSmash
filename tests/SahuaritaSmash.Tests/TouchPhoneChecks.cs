// Playing on a phone (Brawl\BrawlTouchGuide.cs, Services\TouchSettings.cs): the touch layout at every size and with
// either hand, the settings file (fixture, unknown lines kept, never written over when unreadable), the Touch controls
// page by taps, the phone guide in the pause panel, the first-time guide, and the buzz on hits. Phone-sized pictures
// (915 x 412) with -Snapshot: TouchPause-phone, TouchPage-phone, TouchPageLarge-phone, Guide-1..4-phone,
// TouchLefty-phone, TouchLarge-phone.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Input;
using SahuaritaSmash.Services;
using SahuaritaSmash.Views;

namespace ClassicGames
{
    public static partial class Tests
    {
        static readonly Size Phone = new Size(915, 412);

        public static void TouchPhoneChecks()
        {
            TouchLayoutAllSizes();
            TouchSettingsFile();
            string dir = SaveTemp("touch-phone");
            try
            {
                TouchSettings.Current = TouchSettings.Load(Path.Combine(dir, "touch.txt"));
                TouchPagesAndGuide(dir);
                TouchHaptics();
            }
            finally
            {
                TouchSettings.Current = new TouchSettings();
                BrawlGame.GuideInTests = false;
                SahuaritaSmash.App.Haptic = null;
                Game.TouchMode = false;
                KartPads.Fake[0] = null;
                Audio.StopMusic(); Audio.StopEffects();
                try { Directory.Delete(dir, true); } catch (Exception) { }
            }
        }

        static void TouchLayoutAllSizes()
        {
            foreach (var screen in new[] { new Size(640, 360), new Size(780, 360), new Size(915, 412), new Size(1280, 720), new Size(2340, 1080) })
                for (int size = 0; size < 3; size++)
                    foreach (bool lefty in new[] { false, true })
                    {
                        var t = new TouchControls { Scale = TouchSettings.SizeScales[size], LeftHanded = lefty };
                        t.Layout(screen);
                        var all = t.Buttons.Concat(new[] { t.Pause }).ToList();
                        bool big = all.All(b => b.R * 2 >= 44);
                        bool apart = true, inside = true;
                        bool side = t.Buttons.All(b => lefty ? b.Center.X + b.R < screen.Width * 0.5 : b.Center.X - b.R > screen.Width * 0.5);
                        for (int i = 0; i < all.Count; i++)
                        {
                            var a = all[i];
                            if (a.Center.X - a.R < 0 || a.Center.Y - a.R < 0 || a.Center.X + a.R > screen.Width || a.Center.Y + a.R > screen.Height) inside = false;
                            for (int j = i + 1; j < all.Count; j++) { var b = all[j]; double dx = a.Center.X - b.Center.X, dy = a.Center.Y - b.Center.Y; if (Math.Sqrt(dx * dx + dy * dy) < a.R + b.R + 4) apart = false; }
                        }
                        // the stick takes a thumb on its own half only
                        t.Clear();
                        bool stickSide = t.Down(1, new Point(lefty ? screen.Width * 0.8 : screen.Width * 0.2, screen.Height * 0.4)) && t.StickHome.HasValue;
                        t.Clear();
                        bool notOther = !t.Down(2, new Point(lefty ? screen.Width * 0.2 : screen.Width * 0.8, screen.Height * 0.4)) || !t.StickHome.HasValue;
                        t.Clear();
                        Check("touch-phone: " + TouchSettings.SizeNames[size] + (lefty ? ", left-handed," : "") + " on a " + screen.Width + " x " + screen.Height + " screen: every button 44+ across, on screen, apart, on its side, and the stick on the other",
                            big && apart && inside && side && stickSide && notOther, "big " + big + ", apart " + apart + ", inside " + inside + ", side " + side + ", stick " + stickSide + " " + notOther);
                    }
        }

        static void TouchSettingsFile()
        {
            string d = SaveTemp("touch-settings");
            try
            {
                string f = Path.Combine(d, "touch.txt");
                var none = TouchSettings.Load(f);
                Check("touch-phone: no settings file = the usual ones (normal size, right hand, vibration on, guide not seen)", none.Size == 1 && !none.LeftHanded && none.Vibration && !none.GuideDone && !none.Unreadable, none.Text());
                File.Copy(Path.Combine(FixtureDir, "touch.txt"), f);
                var t = TouchSettings.Load(f);
                Check("touch-phone fixture: the version 1 settings file (2026-11-17) opens", t.Size == 2 && t.LeftHanded && !t.Vibration && t.GuideDone && !t.Unreadable && Math.Abs(t.Scale - 1.25) < 1e-9, t.Text());
                t.Vibration = true;
                bool saved = t.Save();
                string back = File.ReadAllText(f);
                var again = TouchSettings.Load(f);
                Check("touch-phone fixture: saving keeps the line a newer version added, writes the change and a backup, and reads back the same",
                    saved && back.Contains("stick=floating") && back.Contains("vibration=on") && back.StartsWith(TouchSettings.FirstLine) && back.Contains("version=1") && File.Exists(f + ".bak1") && again.Vibration && again.Size == 2 && again.LeftHanded,
                    back.Replace("\r\n", " | "));
                // a file that can't be read (and no backup): the usual settings, never written over
                string d2 = SaveTemp("touch-bad");
                string bad = Path.Combine(d2, "touch.txt");
                File.WriteAllBytes(bad, new byte[] { 0x00, 0xFF, 0x13, 0x37 });
                var u = TouchSettings.Load(bad);
                u.Size = 0;
                bool wrote = u.Save();
                byte[] still = File.ReadAllBytes(bad);
                Check("touch-phone: an unreadable settings file gives the usual settings and a note, and is never written over",
                    u.Unreadable && !wrote && u.Note != null && still.Length == 4 && still[1] == 0xFF, "unreadable " + u.Unreadable + ", wrote " + wrote);
                try { Directory.Delete(d2, true); } catch (Exception) { }
            }
            finally { try { Directory.Delete(d, true); } catch (Exception) { } }
        }

        /// <summary>The game's picture at phone size (and the touch controls on top when given).</summary>
        static void PhonePicture(Game g, TouchControls touch, string name)
        {
            var dv = new DrawingVisual((int)Phone.Width, (int)Phone.Height);
            using (DrawingContext dc = dv.RenderOpen())
            {
                double k = Math.Min(Phone.Width / g.Width, Phone.Height / g.Height), ox = (Phone.Width - g.Width * k) / 2, oy = (Phone.Height - g.Height * k) / 2;
                dc.Ctx.FillRectangle(Avalonia.Media.Brushes.Black, new Rect(Phone));
                using (dc.Ctx.PushTransform(Matrix.CreateScale(k, k) * Matrix.CreateTranslation(ox, oy))) GameView.RenderGame(g, new DrawingContext(dc.Ctx));
                if (touch != null) touch.Draw(dc.Ctx);
            }
            Save(dv.Bitmap, name);
            dv.Dispose();
        }

        static bool Tap(Game g, string label)
        {
            var h = g.MouseHits.LastOrDefault(x => x.Label == label);
            if (h == null) return false;
            var c = new Point(h.Area.X + h.Area.Width / 2, h.Area.Y + h.Area.Height / 2);
            g.PointerMove(c); g.PointerDown(c, false); g.PointerUp(c, false);
            return true;
        }

        static BrawlGame TouchGame(string dir, out GameView view)
        {
            Audio.MusicOn = false;
            var g = new BrawlGame { Scores = new ScoreBook(Path.Combine(dir, "scores.txt")), Player = "Kim", Rng = new Random(5) };
            g.KeysFileOverride = Path.Combine(dir, "keys.txt");
            view = new GameView { Game = g, TouchOn = true };
            view.Touch.Layout(Phone);
            g.Begin();
            Frame(view, g);
            return g;
        }

        static void TouchPagesAndGuide(string dir)
        {
            GameView view;
            BrawlGame g = TouchGame(dir, out view);
            double scale = Math.Min(Phone.Width / g.Width, Phone.Height / g.Height);
            Check("touch-phone: playing by touch, the title offers Touch controls", g.MouseHits.Any(h => h.Label == "Touch controls") && !g.MouseHits.Any(h => h.Label.StartsWith("Controls (")), string.Join(", ", g.MouseHits.Select(h => h.Label)));

            // the Touch controls page, by taps
            Tap(g, "Touch controls"); Frame(view, g);
            Check("touch-phone: tapping Touch controls opens its page", g.Menu == BrawlGame.Screen.Touch, g.Menu.ToString());
            var rows = g.MouseHits.Where(h => h.Label != "Back" || h.Area.Height > 40).ToList();
            bool tall = rows.All(h => h.Area.Height * scale >= 44 && h.Area.Width * scale >= 44);
            Check("touch-phone: every row and arrow on the Touch controls page is 44+ pixels on a phone", tall, string.Join(", ", rows.Where(h => h.Area.Height * scale < 44 || h.Area.Width * scale < 44).Select(h => h.Label + " " + (h.Area.Width * scale).ToString("0") + "x" + (h.Area.Height * scale).ToString("0"))));
            PhonePicture(g, null, "TouchPage-phone");
            Tap(g, "Button size >"); Frame(view, g);
            Tap(g, "Left-handed >"); Frame(view, g);
            Tap(g, "Vibration <"); Frame(view, g);
            var ts = TouchSettings.Current;
            string file = File.Exists(ts.Path) ? File.ReadAllText(ts.Path) : "";
            Check("touch-phone: the arrows change the size, the hand and vibration, and they are saved",
                ts.Size == 2 && ts.LeftHanded && !ts.Vibration && file.Contains("size=large") && file.Contains("hand=left") && file.Contains("vibration=off"), file.Replace("\r\n", " | "));
            Tap(g, "Button size >"); Frame(view, g);
            Check("touch-phone: the size stops at Large", ts.Size == 2, ts.Size.ToString());
            PhonePicture(g, null, "TouchPageLarge-phone");
            // the layout in a fight follows the settings
            view.SyncTouch();
            Check("touch-phone: the touch controls take the size and hand from the settings", Math.Abs(view.Touch.Scale - 1.25) < 1e-9 && view.Touch.LeftHanded, view.Touch.Scale + " " + view.Touch.LeftHanded);
            Tap(g, "Show the guide again"); Frame(view, g);
            bool pageGuide = g.GuideShowing && g.MouseHits.Any(h => h.Label == "Next");
            g.TapKey(Key.Escape); Frame(view, g);
            Check("touch-phone: Show the guide again shows it on the page, and Back closes it (staying on the page)", pageGuide && !g.GuideShowing && g.Menu == BrawlGame.Screen.Touch, pageGuide + " " + g.GuideShowing + " " + g.Menu);
            g.TapKey(Key.Escape); Frame(view, g);
            Check("touch-phone: Back leaves the page for the title", g.Menu == BrawlGame.Screen.Title, g.Menu.ToString());

            // a fight, left-handed and large: the picture
            g.Who[0] = 1; g.Who[1] = 2; g.Who[2] = 0; g.Who[3] = 0; g.Pick[0] = 4; g.Pick[1] = 1; g.StagePick = 0; g.Level[1] = 0;
            ts.GuideDone = true;
            g.NewGame();
            for (int i = 0; i < 60 * 3; i++) Frame(view, g);
            view.Touch.Layout(Phone);
            view.Touch.Down(4, new Point(Phone.Width * 0.84, Phone.Height * 0.7)); view.Touch.Move(4, new Point(Phone.Width * 0.84 - 40, Phone.Height * 0.7 - 20));
            view.Touch.Down(5, view.Touch.Buttons[0].Center);
            PhonePicture(g, view.Touch, "TouchLefty-phone");
            Check("touch-phone: left-handed, the stick is on the right and Attack on the left", view.Touch.StickHome.HasValue && view.Touch.StickHome.Value.X > Phone.Width / 2 && view.Touch.Buttons[0].Center.X < Phone.Width / 2 && view.Touch.Pad.A, "");
            view.Touch.Clear();
            ts.LeftHanded = false; view.SyncTouch(); view.Touch.Layout(Phone);
            view.Touch.Down(4, new Point(160, 290)); view.Touch.Move(4, new Point(200, 270));
            PhonePicture(g, view.Touch, "TouchLarge-phone");
            view.Touch.Clear();

            // the pause panel by touch: the phone guide instead of the keyboard table
            g.Pause(); Frame(view, g);
            PhonePicture(g, null, "TouchPause-phone");
            Check("touch-phone: the pause panel by touch still has its big buttons", new[] { "Keep playing", "Restart", "Main menu" }.All(w => g.MouseHits.Any(h => h.Label == w)), "");
            g.Resume(); Frame(view, g);

            // the first-time guide: on the first fight by touch, over the paused fight, card by card
            ts.GuideDone = false; ts.Save();
            BrawlGame.GuideInTests = true;
            GameView view2;
            BrawlGame g2 = TouchGame(dir, out view2);
            g2.Who[0] = 1; g2.Who[1] = 2; g2.Who[2] = 0; g2.Who[3] = 0; g2.Pick[0] = 6; g2.Pick[1] = 1; g2.StagePick = 1; g2.Level[1] = 0;
            g2.NewGame();
            Frame(view2, g2); Frame(view2, g2);
            Check("touch-phone: the first fight by touch shows the guide over the paused fight", g2.Paused && g2.GuideShowing && g2.GuideCard == 0, "paused " + g2.Paused + ", card " + g2.GuideCard);
            bool everyCard = true, bigButtons = true;
            for (int c = 0; c < BrawlGame.GuideCards; c++)
            {
                Frame(view2, g2);
                PhonePicture(g2, null, "Guide-" + (c + 1) + "-phone");
                everyCard &= g2.GuideCard == c;
                string button = c == BrawlGame.GuideCards - 1 ? "Got it" : "Next";
                var hb = g2.MouseHits.FirstOrDefault(h => h.Label == button);
                bigButtons &= hb != null && hb.Area.Height * scale >= 44 && (c == BrawlGame.GuideCards - 1 || g2.MouseHits.Any(h => h.Label == "Skip"));
                Tap(g2, button);
            }
            Frame(view2, g2);
            string saved = File.ReadAllText(ts.Path);
            Check("touch-phone: the guide's four cards go by with big Next / Skip buttons, Got it carries on with the fight and it is remembered",
                everyCard && bigButtons && !g2.GuideShowing && !g2.Paused && ts.GuideDone && saved.Contains("guide=done"), "cards " + everyCard + ", buttons " + bigButtons + ", paused " + g2.Paused + ", " + saved.Replace("\r\n", " | "));
            g2.NewGame(); Frame(view2, g2); Frame(view2, g2);
            Check("touch-phone: the guide doesn't come again once seen", !g2.GuideShowing && !g2.Paused, "");
            // Skip also counts as seen
            ts.GuideDone = false;
            GameView view3;
            BrawlGame g3 = TouchGame(dir, out view3);
            g3.Who[0] = 1; g3.Who[1] = 2; g3.Who[2] = 0; g3.Who[3] = 0;
            g3.NewGame(); Frame(view3, g3); Frame(view3, g3); Frame(view3, g3);
            bool skipped = Tap(g3, "Skip"); Frame(view3, g3);
            Check("touch-phone: Skip closes the guide, carries on and remembers it", skipped && !g3.GuideShowing && !g3.Paused && ts.GuideDone, "");
            // not by keyboard
            ts.GuideDone = false;
            GameView view4;
            BrawlGame g4 = TouchGame(dir, out view4);
            view4.TouchOn = false;
            g4.Who[0] = 1; g4.Who[1] = 2; g4.Who[2] = 0; g4.Who[3] = 0;
            g4.NewGame(); Frame(view4, g4); Frame(view4, g4);
            Check("touch-phone: playing with keys, there is no guide", !g4.GuideShowing && !g4.Paused, "");
            BrawlGame.GuideInTests = false;
            ts.GuideDone = true;
        }

        static void TouchHaptics()
        {
            Game.TouchMode = true;
            var buzzes = new List<int>();
            SahuaritaSmash.App.Haptic = ms => buzzes.Add(ms);
            var ts = TouchSettings.Current;
            ts.Vibration = true;
            var g = new BrawlGame { Player = "Kim", Rng = new Random(7) };
            g.Who[0] = 1; g.Who[1] = 2; g.Who[2] = 2; g.Who[3] = 0; g.StagePick = 0;
            g.NewGame();
            BrawlSim sim = g.Sim;
            int me = g.TouchSlot();
            Check("touch-phone: the touch controls play the first person's fighter", me == 0, me.ToString());
            Func<string, int, double, int, int> feel = (kind, victim, a, by) =>
            {
                g.Simulate(0.13);   // past the 120 ms between buzzes (still in the countdown: no real hits)
                buzzes.Clear();
                sim.Events.Clear();
                sim.F[victim].Last = by; sim.F[victim].LastT = 0;
                sim.Events.Add(new BrEvent { K = kind, S = victim, A = a });
                g.Feel();
                return buzzes.Count == 0 ? 0 : buzzes[0];
            };
            int landed = feel("hit", 1, 40, 0), big = feel("hit", 1, 200, 0), gotHit = feel("hit", 0, 40, 1), bonk = feel("ko", 1, 0, 0), bonked = feel("ko", 0, 0, 1);
            int cpu = feel("hit", 2, 200, 1), cpuKo = feel("ko", 2, 0, 1);
            Check("touch-phone: a buzz when your fighter lands a hit (longer for a big one and a bonk) or is hit, none for the computers' hits on each other",
                landed > 0 && big > landed && gotHit > 0 && bonk > big && bonked > 0 && cpu == 0 && cpuKo == 0,
                "landed " + landed + ", big " + big + ", hit " + gotHit + ", bonk " + bonk + ", bonked " + bonked + ", cpu " + cpu + " " + cpuKo);
            // at most one every 120 ms
            g.Simulate(0.13);
            buzzes.Clear();
            for (int i = 0; i < 3; i++) { sim.Events.Clear(); sim.F[1].Last = 0; sim.Events.Add(new BrEvent { K = "hit", S = 1, A = 40 }); g.Feel(); }
            Check("touch-phone: hits close together buzz once", buzzes.Count == 1, buzzes.Count.ToString());
            ts.Vibration = false;
            int off = feel("hit", 0, 200, 1);
            ts.Vibration = true;
            Game.TouchMode = false;
            int keys = feel("hit", 0, 200, 1);
            Game.TouchMode = true;
            SahuaritaSmash.App.Haptic = null;
            bool noHook = true;
            try { sim.Events.Clear(); sim.Events.Add(new BrEvent { K = "hit", S = 0, A = 200 }); g.Simulate(0.13); g.Feel(); } catch (Exception) { noHook = false; }
            Check("touch-phone: no buzz with Vibration off, when playing with keys, or without a phone to buzz", off == 0 && keys == 0 && noHook, off + " " + keys + " " + noHook);
            // a real fight: only hits that involve the person's fighter buzz
            SahuaritaSmash.App.Haptic = ms => buzzes.Add(ms);
            int total = 0, ours = 0;
            var g2 = new BrawlGame { Player = "Kim", Rng = new Random(9) };
            g2.Who[0] = 1; g2.Who[1] = 2; g2.Who[2] = 2; g2.Who[3] = 0; g2.StagePick = 0; g2.Level[1] = 3; g2.Level[2] = 3;
            g2.NewGame();
            buzzes.Clear();
            for (int i = 0; i < 60 * 20 && g2.State == Phase.Play; i++)
            {
                g2.Update(1.0 / 60);
                foreach (BrEvent e in g2.Sim.Events) if (e.K == "hit" || e.K == "ko") { total++; if (e.S == 0 || g2.Sim.F[e.S].Last == 0) ours++; }
            }
            Check("touch-phone: in a real fight against two computers the phone buzzes only around the person's own hits", (ours == 0) == (buzzes.Count == 0) && buzzes.Count <= ours + 1,
                "events " + total + ", ours " + ours + ", buzzes " + buzzes.Count);
            SahuaritaSmash.App.Haptic = null;
            Game.TouchMode = false;
        }
    }
}
