// Hosts a Game (App Hub's GameSurface, in Avalonia): runs its loop once per frame, draws its fixed-size picture scaled
// to fit with bars beside it, and passes on keys, the mouse and touch (in game coordinates). Errors are written to the
// error log and the game goes back to its title instead of closing the app. Leaving the window lets go of every key
// and pauses (App Hub's FocusLost); Android's Back button is Esc, except on the title, where it leaves the app.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using ClassicGames;

namespace SahuaritaSmash.Views
{
    public sealed class GameView : Control
    {
        readonly Stopwatch clock = Stopwatch.StartNew();
        readonly HashSet<Key> down = new HashSet<Key>();
        Game game;
        double last;
        bool attached, running = true;
        TopLevel top;

        /// <summary>The game wants to leave (Esc on its title): the app decides what that means.</summary>
        public event EventHandler ExitRequested;
        /// <summary>Errors since the start (the checks look at it).</summary>
        public int Errors;
        /// <summary>The on-screen stick and buttons (TouchControls.cs): on Android from the start, on Windows once the
        /// screen is touched.</summary>
        public readonly TouchControls Touch = new TouchControls();
        public bool TouchOn = OperatingSystem.IsAndroid();

        /// <summary>The touch controls are in use now: during play, nothing paused and nothing on screen to click (a
        /// fight; results and menus take taps themselves).</summary>
        public bool TouchActive
        {
            get { Game g = game; return TouchOn && g != null && g.State == Phase.Play && !g.Paused && g.MouseHits.Count == 0; }
        }
        bool touchWas;
        /// <summary>Hands the touch controls to the game as controller 1 while they are in use (each frame).</summary>
        public void SyncTouch()
        {
            bool on = TouchActive;
            if (!on && touchWas) Touch.Clear();
            KartPads.Fake[0] = on ? Touch.Pad : null;
            touchWas = on;
        }

        public GameView()
        {
            Focusable = true;
            ClipToBounds = true;
        }

        public Game Game
        {
            get { return game; }
            set
            {
                if (game != null) game.ExitRequested -= OnGameExit;
                game = value;
                if (game != null) game.ExitRequested += OnGameExit;
                InvalidateVisual();
            }
        }

        void OnGameExit(object sender, EventArgs e) { var h = ExitRequested; if (h != null) h(this, EventArgs.Empty); }

        // ---------------------------------------------------------------- the loop
        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            attached = true;
            top = TopLevel.GetTopLevel(this);
            if (top != null)
            {
                top.BackRequested += OnBack;
                var w = top as Window;
                if (w != null) w.Deactivated += OnDeactivated;
                last = clock.Elapsed.TotalSeconds;
                top.RequestAnimationFrame(Frame);
            }
            Focus();
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            attached = false;
            if (top != null)
            {
                top.BackRequested -= OnBack;
                var w = top as Window;
                if (w != null) w.Deactivated -= OnDeactivated;
            }
            base.OnDetachedFromVisualTree(e);
        }

        void Frame(TimeSpan _)
        {
            double now = clock.Elapsed.TotalSeconds, dt = now - last;
            last = now;
            Game g = game;
            if (g != null && running)
            {
                g.Stats.Tick(dt * 1000);
                SyncTouch();
                Safe(() => g.Update(Math.Max(0, Math.Min(0.1, dt))), "the game");
                InvalidateVisual();
            }
            if (attached && top != null) top.RequestAnimationFrame(Frame);
        }

        /// <summary>Runs something of the game's; a mistake is written down and the game goes back to its title.</summary>
        void Safe(Action a, string what)
        {
            try { a(); }
            catch (Exception ex)
            {
                Errors++;
                CrashLog.Write(what, ex, false);
                try { Audio.StopMusic(); Audio.StopEffects(); if (game != null) game.Begin(); }
                catch (Exception e2) { CrashLog.Write("going back to the title", e2, false); running = false; }
            }
        }

        // ---------------------------------------------------------------- drawing
        /// <summary>Where the game's picture sits in the control: scale and offset.</summary>
        void Fit(out double k, out double ox, out double oy)
        {
            Game g = game;
            double gw = g != null ? g.Width : 1280, gh = g != null ? g.Height : 720;
            k = Math.Min(Bounds.Width / gw, Bounds.Height / gh);
            ox = (Bounds.Width - gw * k) / 2; oy = (Bounds.Height - gh * k) / 2;
        }

        Point ToGame(Point p) { double k, ox, oy; Fit(out k, out ox, out oy); return k > 0 ? new Point((p.X - ox) / k, (p.Y - oy) / k) : new Point(-1, -1); }

        public override void Render(Avalonia.Media.DrawingContext context)
        {
            context.FillRectangle(Brushes.Black, new Rect(Bounds.Size));
            Game g = game;
            if (g == null) return;
            double k, ox, oy; Fit(out k, out ox, out oy);
            if (k <= 0) return;
            var sw = Stopwatch.StartNew();
            using (context.PushClip(new Rect(ox, oy, g.Width * k, g.Height * k)))
            using (context.PushTransform(Matrix.CreateScale(k, k) * Matrix.CreateTranslation(ox, oy)))
            {
                var dc = new ClassicGames.DrawingContext(context);
                context.FillRectangle(new SolidColorBrush(WpfBrush.C(g.Background)), new Rect(0, 0, g.Width, g.Height));
                Safe(() => g.Render(dc), "drawing the game");
                dc.Dispose();
            }
            if (TouchActive) { Touch.Layout(Bounds.Size); Touch.Draw(context); }
            g.Stats.Done(0, sw.Elapsed.TotalMilliseconds);
        }

        /// <summary>The game's picture at its own size (snapshots and the checks).</summary>
        public static void RenderGame(Game g, ClassicGames.DrawingContext dc)
        {
            dc.DrawRectangle(Gfx.B(g.Background), null, new Rect(0, 0, g.Width, g.Height));
            g.Render(dc);
        }

        // ---------------------------------------------------------------- keys
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            Game g = game;
            if (g == null || e.Key == Key.None) return;
            bool repeat = !down.Add(e.Key);
            Key k = e.Key;
            Safe(() => g.KeyDown(k, repeat), "a key");
            e.Handled = true;
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);
            down.Remove(e.Key);
            Game g = game;
            if (g == null) return;
            Key k = e.Key;
            Safe(() => g.KeyUp(k), "a key");
            e.Handled = true;
        }

        void OnDeactivated(object sender, EventArgs e)
        {
            down.Clear();
            Touch.Clear();
            Game g = game;
            if (g != null) Safe(g.FocusLost, "the window going to the back");
        }

        /// <summary>The app went to the background (Android): the same as leaving the window.</summary>
        public void AppPaused() { OnDeactivated(this, EventArgs.Empty); }

        void OnBack(object sender, RoutedEventArgs e)
        {
            Game g = game;
            var b = g as BrawlGame;
            if (g == null || (b != null && b.State == Phase.Setup && b.Menu == BrawlGame.Screen.Title)) return;   // the system leaves the app
            e.Handled = true;
            Safe(() => g.TapKey(Key.Escape), "the Back button");
        }

        // ---------------------------------------------------------------- the mouse and touch
        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);
            Game g = game;
            if (g == null) return;
            if (e.Pointer.Type == PointerType.Touch && Touch.Move(e.Pointer.Id, e.GetPosition(this))) { e.Handled = true; return; }
            Point p = ToGame(e.GetPosition(this));
            Safe(() => g.PointerMove(p), "the mouse");
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);
            Focus();
            Game g = game;
            if (g == null) return;
            if (e.Pointer.Type == PointerType.Touch)
            {
                if (!TouchOn) { TouchOn = true; SyncTouch(); }
                if (TouchActive) { Touch.Layout(Bounds.Size); if (Touch.Down(e.Pointer.Id, e.GetPosition(this))) { e.Handled = true; return; } }
            }
            Point p = ToGame(e.GetPosition(this));
            bool right = e.GetCurrentPoint(this).Properties.IsRightButtonPressed;
            // a touch first points (selecting a menu item, as the mouse would on its way there), then presses
            if (e.Pointer.Type == PointerType.Touch) Safe(() => g.PointerMove(p), "a touch");
            Safe(() => g.PointerDown(p, right), "the mouse");
            e.Handled = true;
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);
            Game g = game;
            if (g == null) return;
            if (e.Pointer.Type == PointerType.Touch && Touch.Up(e.Pointer.Id)) { e.Handled = true; return; }
            Point p = ToGame(e.GetPosition(this));
            bool right = e.InitialPressMouseButton == MouseButton.Right;
            Safe(() => g.PointerUp(p, right), "the mouse");
            if (e.Pointer.Type == PointerType.Touch) Safe(g.PointerLeave, "a touch");
        }

        protected override void OnPointerExited(PointerEventArgs e) { base.OnPointerExited(e); Game g = game; if (g != null) Safe(g.PointerLeave, "the mouse"); }

        protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
        {
            base.OnPointerWheelChanged(e);
            Game g = game;
            if (g == null || e.Delta.Y == 0) return;
            int d = e.Delta.Y > 0 ? 120 : -120;
            Safe(() => g.PointerWheel(d), "the mouse wheel");
        }
    }
}
