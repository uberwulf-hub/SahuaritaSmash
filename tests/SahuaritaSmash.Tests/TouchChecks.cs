// The touch controls (Views\TouchControls.cs): big enough for a thumb, apart, on screen, out of the menus' way, and
// really driving a fighter through controller 1. Plus a phone-sized picture of a fight with them.
using System;
using System.Linq;
using Avalonia;
using SahuaritaSmash.Views;

namespace ClassicGames
{
    public static partial class Tests
    {
        public static void TouchChecks()
        {
            foreach (var screen in new[] { new Size(780, 360), new Size(915, 412), new Size(1280, 720), new Size(2340, 1080) })
            {
                var t = new TouchControls();
                t.Layout(screen);
                var all = t.Buttons.Concat(new[] { t.Pause }).ToList();
                bool big = all.All(b => b.R * 2 >= 44);
                bool apart = true, inside = true, right = t.Buttons.All(b => b.Center.X - b.R > screen.Width * 0.5);
                for (int i = 0; i < all.Count; i++)
                {
                    var a = all[i];
                    if (a.Center.X - a.R < 0 || a.Center.Y - a.R < 0 || a.Center.X + a.R > screen.Width || a.Center.Y + a.R > screen.Height) inside = false;
                    for (int j = i + 1; j < all.Count; j++) { var b = all[j]; double dx = a.Center.X - b.Center.X, dy = a.Center.Y - b.Center.Y; if (Math.Sqrt(dx * dx + dy * dy) < a.R + b.R + 4) apart = false; }
                }
                Check("touch: on a " + screen.Width + " x " + screen.Height + " screen every button is at least 44 across, on screen, apart from the others, and the fight buttons are on the right half",
                    big && apart && inside && right, "big " + big + ", apart " + apart + ", inside " + inside + ", right " + right);
            }

            // a fight: the person on touch against a second player who stands still (a computer player could knock
            // the fighter down mid-check)
            Audio.MusicOn = false;
            var g = new BrawlGame { Scores = new ScoreBook(System.IO.Path.Combine(SahuaritaSmash.Services.Data.Dir, "touch-scores.txt")), Player = "Kim", Rng = new Random(3) };
            g.KeysFileOverride = System.IO.Path.Combine(SahuaritaSmash.Services.Data.Dir, "touch-keys.txt");
            var view = new GameView { Game = g, TouchOn = true };
            var phone = new Size(915, 412);
            view.Touch.Layout(phone);
            g.Begin();
            Frame(view, g);
            Check("touch: on the title the touch controls stay out of the way (menus take the taps)", !view.TouchActive && KartPads.Fake[0] == null, "");
            g.Who[0] = 1; g.Who[1] = 1; g.Who[2] = 0; g.Who[3] = 0; g.Pick[0] = 0; g.Pick[1] = 1; g.StagePick = 0; g.Level[1] = 0;
            g.NewGame();
            for (int i = 0; i < 60 * 3; i++) Frame(view, g);
            Check("touch: in a fight they are in use, as controller 1", view.TouchActive && KartPads.Fake[0] == view.Touch.Pad, "");
            BFighter me = g.Sim.F[0];
            double x0 = me.X;
            // the stick: a thumb on the left half, slid right
            Point home = new Point(150, 300);
            view.Touch.Down(1, home); view.Touch.Move(1, new Point(home.X + 100, home.Y));
            for (int i = 0; i < 40; i++) Frame(view, g);
            view.Touch.Up(1);
            Check("touch: sliding the stick right walks the fighter right", me.X > x0 + 40, x0.ToString("0") + " -> " + me.X.ToString("0"));
            for (int i = 0; i < 30; i++) Frame(view, g);
            // Attack
            var attack = view.Touch.Buttons.First(b => b.Label == "Attack");
            view.Touch.Down(2, attack.Center);
            bool attacked = false;
            for (int i = 0; i < 12 && !attacked; i++) { Frame(view, g); attacked = me.St == BS.Attack; }
            view.Touch.Up(2);
            Check("touch: tapping Attack makes the fighter attack", attacked, me.St.ToString());
            Check("touch: five buttons (no Jump: the stick jumps), Shield beside Attack along the bottom",
                view.Touch.Buttons.Count == 5 && !view.Touch.Buttons.Any(x => x.Label == "Jump") && Math.Abs(view.Touch.Buttons[1].Center.Y - attack.Center.Y) < attack.R, "");
            // the stick pushed up jumps (there's no Jump button)
            for (int i = 0; i < 30; i++) Frame(view, g);
            double y0 = me.Y;
            view.Touch.Down(6, home); view.Touch.Move(6, new Point(home.X, home.Y - 100));
            double top = y0;
            for (int i = 0; i < 30; i++) { Frame(view, g); top = Math.Max(top, me.Y); }
            view.Touch.Up(6);
            Check("touch: pushing the stick up jumps", top > y0 + 40, y0.ToString("0") + " -> " + top.ToString("0"));
            // Shield: held, the fighter shields
            for (int i = 0; i < 60; i++) Frame(view, g);
            var shieldB = view.Touch.Buttons.First(x => x.Label == "Shield");
            view.Touch.Down(7, shieldB.Center);
            bool shielded = false;
            for (int i = 0; i < 12 && !shielded; i++) { Frame(view, g); shielded = me.St == BS.Shield; }
            view.Touch.Up(7);
            Check("touch: holding Shield shields", shielded, me.St.ToString());
            // pause, then the controls step aside for the pause panel
            for (int i = 0; i < 40; i++) Frame(view, g);
            view.Touch.Down(3, view.Touch.Pause.Center);
            for (int i = 0; i < 3; i++) Frame(view, g);
            view.Touch.Up(3);
            Frame(view, g);
            Check("touch: the pause button pauses, and the controls let the pause panel have the taps", g.Paused && !view.TouchActive && KartPads.Fake[0] == null, "paused " + g.Paused);
            g.Resume();
            for (int i = 0; i < 3; i++) Frame(view, g);
            // a phone-sized picture: the game scaled to fit with the controls on top
            var dv = new DrawingVisual((int)phone.Width, (int)phone.Height);
            view.Touch.Down(4, new Point(160, 290)); view.Touch.Move(4, new Point(200, 270)); view.Touch.Down(5, attack.Center);
            using (DrawingContext dc = dv.RenderOpen())
            {
                double k = Math.Min(phone.Width / g.Width, phone.Height / g.Height), ox = (phone.Width - g.Width * k) / 2, oy = (phone.Height - g.Height * k) / 2;
                dc.Ctx.FillRectangle(Avalonia.Media.Brushes.Black, new Rect(phone));
                using (dc.Ctx.PushTransform(Matrix.CreateScale(k, k) * Matrix.CreateTranslation(ox, oy))) GameView.RenderGame(g, new DrawingContext(dc.Ctx));
                view.Touch.Draw(dc.Ctx);
            }
            Save(dv.Bitmap, "Touch-phone");
            view.Touch.Clear();
            dv.Dispose();

            // the pause menu by touch (0.3.1, Andrew: "from the pause menu on the phone there's no restart, main menu"):
            // big buttons with plain words, 44+ pixels tall on a phone, and tapping them does what they say
            for (int i = 0; i < 20; i++) Frame(view, g);
            Func<string, HitArea> find = label => g.MouseHits.FirstOrDefault(h => h.Label == label);
            Action<string> tap = label => { var h = find(label); if (h != null) { var c = new Point(h.Area.X + h.Area.Width / 2, h.Area.Y + h.Area.Height / 2); g.PointerDown(c, false); g.PointerUp(c, false); } };
            double scale = Math.Min(phone.Width / g.Width, phone.Height / g.Height);
            g.Pause(); Frame(view, g);
            string[] want = { "Keep playing", "Restart", "Main menu", "Music on", "Sounds off" };
            bool every = want.All(w => find(w) != null);
            bool tall = want.All(w => find(w) == null || find(w).Area.Height * scale >= 44);
            var pic = new DrawingVisual((int)phone.Width, (int)phone.Height);
            using (DrawingContext dc = pic.RenderOpen())
            {
                double ox = (phone.Width - g.Width * scale) / 2, oy = (phone.Height - g.Height * scale) / 2;
                dc.Ctx.FillRectangle(Avalonia.Media.Brushes.Black, new Rect(phone));
                using (dc.Ctx.PushTransform(Matrix.CreateScale(scale, scale) * Matrix.CreateTranslation(ox, oy))) GameView.RenderGame(g, new DrawingContext(dc.Ctx));
            }
            Save(pic.Bitmap, "Pause-phone");
            pic.Dispose();
            Check("touch: the pause menu has big buttons with plain words (Keep playing, Restart, Main menu, music, sounds), 44+ pixels tall on a phone",
                every && tall, string.Join(", ", g.MouseHits.Select(h => h.Label + " " + (h.Area.Height * scale).ToString("0")).ToArray()));
            tap("Keep playing"); Frame(view, g);
            bool resumed = !g.Paused && g.State == Phase.Play;
            for (int i = 0; i < 60; i++) Frame(view, g);
            int before = g.Sim.Frame;
            g.Pause(); Frame(view, g); tap("Restart"); Frame(view, g);
            bool restarted = !g.Paused && g.State == Phase.Play && g.Sim.Frame < before;
            g.Pause(); Frame(view, g); tap("Main menu"); Frame(view, g);
            bool menu = !g.Paused && g.State == Phase.Setup && g.Menu == BrawlGame.Screen.Title;
            Check("touch: tapping Keep playing, Restart and Main menu in the pause menu does what they say", resumed && restarted && menu, resumed + " " + restarted + " " + menu);
            Game.TouchMode = false;
            KartPads.Fake[0] = null;
            Audio.StopMusic(); Audio.StopEffects();
        }

        /// <summary>One frame as the app runs it: the touch controls handed over, the game moved on and drawn (drawing
        /// registers what can be clicked, which decides whether the controls are in use).</summary>
        static void Frame(GameView view, Game g)
        {
            view.SyncTouch();
            g.Update(1.0 / 60);
            var dv = new DrawingVisual(64, 36);
            using (DrawingContext dc = dv.RenderOpen()) g.Render(dc);
            dv.Dispose();
        }
    }
}
