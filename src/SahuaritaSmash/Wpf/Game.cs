// Classic Games' game base (App Hub's Engine\Core.cs: Phase, Game, HitArea, FrameStats), on Avalonia: pause, game
// over, high scores, music, keys (held and tapped), and the mouse in menus, which is also how touch works here. The
// window side (App Hub's GameSurface) is Views\GameView.cs. Network play isn't here yet: Net stays null (NetLink.cs).
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Input;

namespace ClassicGames
{
    public enum Phase { Setup, Play, Ending, Over }

    public abstract class Game
    {
        public abstract string Id { get; }
        public abstract string Title { get; }
        public virtual double Width { get { return 960; } }
        public virtual double Height { get { return 720; } }
        public virtual uint Background { get { return 0xFF0B0D12; } }
        public virtual bool HideCursor { get { return false; } }

        public string Player = "Player";
        public ScoreBook Scores;
        /// <summary>Shows a file in Explorer ("Show the file" for a save that couldn't be opened). Set by the window;
        /// tests and snapshots leave it null, and the game just names the file.</summary>
        public static Action<string> RevealFile;
        protected void ShowFile(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            if (RevealFile != null) { try { RevealFile(path); return; } catch (Exception) { } }
            Toast(System.IO.Path.GetFileName(path));
        }
        public GameTunes Tunes = new GameTunes();
        public event EventHandler ExitRequested;
        public Phase State { get; protected set; }
        public bool Paused { get; private set; }
        /// <summary>A built-in player (for -Demo snapshots, card pictures and tests).</summary>
        public bool Autopilot;
        public Random Rng = new Random();
        /// <summary>Seconds since the game was made (for animations).</summary>
        public double T;
        /// <summary>Set when the game is played with other computers (Engine\NetPlay.cs): it pumps the network every frame,
        /// draws the other players' names, the ping and a lost-connection panel, and the game no longer pauses.</summary>
        public NetLink Net;
        /// <summary>For races on the network: the score the other players see, and a few words about how it's going.</summary>
        public virtual long NetScore { get { return 0; } }
        public virtual string NetStatus { get { return ""; } }

        protected readonly HashSet<Key> held = new HashSet<Key>();
        double endTimer, endLength, toastTime;
        string toast;
        protected bool NewBest;
        protected long LastScore, PrevBest;
        protected string OverTitle = "Game over", OverDetail = "", OverKey = "";
        protected IList<ScoreEntry> OverTop = new List<ScoreEntry>();

        protected Game() { State = Phase.Play; }

        // ---------------------------------------------------------------- what a game fills in
        protected abstract void Reset();
        protected abstract void Tick(double dt);
        protected abstract void Draw(DrawingContext dc);
        protected virtual void OnKey(Key k) { }
        protected virtual void OnKeyUp(Key k) { }
        protected virtual bool HasSetup { get { return false; } }
        protected virtual void SetupKey(Key k) { if (k == Key.Enter || k == Key.Space) { Audio.Ui("select"); NewGame(); } }
        /// <summary>Esc on a setup screen: a game with several setup screens goes back one (true); false leaves the game.</summary>
        protected virtual bool SetupBack() { return false; }
        protected virtual void DrawSetup(DrawingContext dc) { }
        protected virtual void TickSetup(double dt) { }
        protected virtual void TickEnding(double dt) { }
        protected virtual void StartMusic() { if (Tunes.Theme != null) Audio.Play(Tunes.Theme); }
        protected virtual void SetupMusic() { Audio.StopMusic(); }
        protected virtual void OverConfirm() { NewGame(); }
        protected virtual string OverConfirmText { get { return "Enter: play again"; } }
        public virtual void MouseMove(Point p) { }
        public virtual void MouseDown(Point p) { }
        /// <summary>The right mouse button (games that don't use it treat it like the left one).</summary>
        public virtual void MouseRightDown(Point p) { MouseDown(p); }
        /// <summary>A mouse button let go during play.</summary>
        public virtual void MouseUp(Point p) { }
        /// <summary>The game wants the mouse captured for looking round (the pointer is hidden and kept in the middle; only
        /// real windows do this, never snapshots or tests: GameSurface.AllowMouseCapture).</summary>
        public virtual bool WantsMouseCapture { get { return false; } }
        /// <summary>The game aims with a hidden pointer and wants it kept inside the picture while playing (a click
        /// can't land on the desktop and pause the game; the pointer is read fresh every frame). Only real windows do
        /// this (GameSurface.AllowMouseCapture), only while the window is active, never with a menu or pause open.</summary>
        public virtual bool ConfinesPointer { get { return false; } }
        /// <summary>A few words about where the game is (level, how busy), for the error log.</summary>
        public virtual string CrashDetail { get { return null; } }
        /// <summary>How far a captured mouse moved (window units).</summary>
        public virtual void MouseLook(double dx, double dy) { }
        /// <summary>The mouse wheel while playing (no menu open).</summary>
        protected virtual void PlayWheel(int delta) { }
        /// <summary>Keys the game uses itself before the shared ones (M music, N sounds, Esc / P pause, Backspace back):
        /// true sends the key to SetupKey (setup screens, repeats too, for typing) or OnKey (play).</summary>
        protected virtual bool TakesKey(Key k) { return false; }
        /// <summary>A key on the pause panel the game handles itself (true = used).</summary>
        protected virtual bool PausedKey(Key k) { return false; }
        /// <summary>The window is closing (a game with something to save saves it now).</summary>
        public virtual void Closing() { }
        /// <summary>The window is closing for an App Hub update (nobody may be at the screen): save where the player is,
        /// quickly and without asking anything. Returns what the game needs to carry on afterwards (UpdateResume keeps it
        /// and ResumeAfterUpdate gets it back), or null when there is nothing to carry on from (scores are kept as they
        /// are made). Throws when the game couldn't be saved. The default saves as on closing.</summary>
        public virtual IDictionary<string, string> SaveForUpdate() { Closing(); return null; }
        /// <summary>After an update: carry on from what SaveForUpdate returned (the game has just been begun). True when it
        /// did; false leaves the game on its own start screen.</summary>
        public virtual bool ResumeAfterUpdate(IDictionary<string, string> saved) { return false; }

        // ---------------------------------------------------------------- the mouse in menus
        // Every menu, button and panel drawn by a game registers where it is while it draws (AddHit, DrawMenu, MenuButtons,
        // MenuRows): a rectangle in game coordinates, what makes it the selected item (the menu's own selection, so the
        // keys and the mouse always agree) and what a click does (nearly always the key that does it, so a click runs
        // exactly the keyboard's code). The list is rebuilt on every frame. Pointing at an item selects it (the "move"
        // sound), a click selects and confirms, a right click is Esc, the wheel is Up / Down on setup screens. During play
        // (no panel open) the mouse goes to the game as before.
        readonly List<HitArea> hits = new List<HitArea>();
        Point pointer = new Point(-1, -1);
        bool pointerIn;
        Rect hoverRect = default(Rect);

        /// <summary>The items the last frame drew that can be clicked (topmost last).</summary>
        public IList<HitArea> MouseHits { get { return hits; } }
        /// <summary>The mouse is over something that can be clicked (the window shows a hand).</summary>
        public bool PointerOnHit { get { return pointerIn && HitAt(pointer) != null; } }
        /// <summary>A panel or menu has the mouse (not the game being played).</summary>
        public bool MenuOpenForMouse { get { return State != Phase.Play || Paused || Net != null && (Net.Lost || Net.Round != null); } }

        HitArea HitAt(Point p)
        {
            for (int i = hits.Count - 1; i >= 0; i--) if (hits[i].Area.Contains(p)) return hits[i];
            return null;
        }

        /// <summary>Registers a clickable area for this frame. select may be null (a plain button).</summary>
        public HitArea AddHit(Rect r, string label, bool selected, Action select, Action click)
        {
            var h = new HitArea { Area = r, Label = label ?? "", Selected = selected, Select = select, Click = click };
            hits.Add(h);
            return h;
        }
        /// <summary>A clickable area that presses a key (and lets go of it).</summary>
        public HitArea AddKeyHit(Rect r, string label, Key k) { return AddHit(r, label, false, null, () => TapKey(k)); }
        /// <summary>A menu item: pointing at it selects it, a click selects it and presses the key (usually Enter).</summary>
        public HitArea AddItemHit(Rect r, string label, bool selected, Action select, Key k) { return AddHit(r, label, selected, select, () => TapKey(k)); }

        /// <summary>The mouse is over this rectangle (for drawing a button lit up).</summary>
        public bool Hovered(Rect r) { return pointerIn && r.Contains(pointer); }

        /// <summary>A key pressed and let go at once (what a click does).</summary>
        public void TapKey(Key k) { KeyDown(k, false); KeyUp(k); }

        /// <summary>The middle of the first clickable item whose label contains this text (tests and snapshots).</summary>
        public Point? HitPoint(string label)
        {
            foreach (HitArea h in hits)
                if (string.Equals(h.Label, label, StringComparison.OrdinalIgnoreCase)) return new Point(h.Area.X + h.Area.Width / 2, h.Area.Y + h.Area.Height / 2);
            foreach (HitArea h in hits)
                if (h.Label.IndexOf(label ?? "", StringComparison.OrdinalIgnoreCase) >= 0) return new Point(h.Area.X + h.Area.Width / 2, h.Area.Y + h.Area.Height / 2);
            return null;
        }

        /// <summary>The mouse moved (game coordinates).</summary>
        public void PointerMove(Point p)
        {
            pointer = p; pointerIn = true;
            MouseMove(p);
            if (hits.Count == 0) return;   // nothing drawn since the last key: wait for the next frame
            HitArea h = HitAt(p);
            Rect r = h != null ? h.Area : default(Rect);
            if (h != null && h.Select != null && !h.Selected)
            {
                h.Select();
                foreach (HitArea o in hits) if (o.Select != null) o.Selected = h.Item >= 0 && o.Item == h.Item;
                h.Selected = true;
                Audio.Ui("move");
            }
            else if (h != null && h.Select == null && r != hoverRect) Audio.Ui("move");
            hoverRect = r;
        }

        /// <summary>The mouse left the game.</summary>
        public void PointerLeave() { pointerIn = false; hoverRect = default(Rect); }

        /// <summary>A mouse button (game coordinates). Menus and panels take it; otherwise the game gets it.</summary>
        public void PointerDown(Point p, bool right)
        {
            InputCount++;
            pointer = p; pointerIn = true;
            bool inside = p.X >= 0 && p.Y >= 0 && p.X < Width && p.Y < Height;
            if (!inside) return;   // the bars beside the game: nothing
            HitArea h = right ? null : HitAt(p);
            if (h != null)
            {
                if (h.Select != null) h.Select();
                hits.Clear(); hoverRect = default(Rect);
                if (h.Click != null) h.Click();
                return;
            }
            if (MenuOpenForMouse) { if (right) { hits.Clear(); TapKey(Key.Escape); } return; }
            if (right) MouseRightDown(p); else MouseDown(p);
        }

        /// <summary>A mouse button let go (game coordinates): only the game itself hears it, during play.</summary>
        public void PointerUp(Point p, bool right) { if (!right && !MenuOpenForMouse) MouseUp(p); }

        /// <summary>The mouse wheel: Up / Down (or what the setup screen uses) on setup screens.</summary>
        public void PointerWheel(int delta)
        {
            if (delta != 0 && State == Phase.Play && !Paused && !MenuOpenForMouse) { PlayWheel(delta); return; }
            if (delta == 0 || State != Phase.Setup || Paused) return;
            hits.Clear();
            TapKey(WheelKey(delta > 0));
        }
        /// <summary>The key one notch of the mouse wheel presses on a setup screen.</summary>
        protected virtual Key WheelKey(bool up) { return up ? Key.Up : Key.Down; }

        /// <summary>A row of buttons centred at cx (each label already says its key, like "Enter: play again").</summary>
        public void MenuButtons(DrawingContext dc, double cx, double y, double size, string[] labels, Key[] keys)
        {
            double gap = size * 0.9, pad = size * 0.9, h = size * 2.0, total = 0;
            var ws = new double[labels.Length];
            for (int i = 0; i < labels.Length; i++) { ws[i] = Gfx.Width(labels[i], size, Gfx.Ui) + pad * 2; total += ws[i] + (i > 0 ? gap : 0); }
            double x = cx - total / 2;
            for (int i = 0; i < labels.Length; i++)
            {
                var r = new Rect(x, y - size * 0.45, ws[i], h);
                bool lit = Hovered(r);
                dc.DrawRoundedRectangle(Gfx.B(lit ? 0xFF1E3A8A : 0xC0131A2A), Gfx.P(lit ? 0xFF60A5FA : 0xFF2C3A55, lit ? 2 : 1.2), r, h / 2, h / 2);
                Gfx.Text(dc, labels[i], x + ws[i] / 2, y, size, lit ? 0xFFFFFFFF : 0xFF93C5FD, 0.5, lit ? Gfx.UiBold : Gfx.Ui);
                AddKeyHit(r, labels[i], keys[i]);
                x += ws[i] + gap;
            }
        }

        /// <summary>The person is playing by touch (Android, or a touched Windows screen): key names are left off buttons.</summary>
        public static bool TouchMode;
        /// <summary>The height of a big menu button in game units: 44+ screen pixels even on a small phone (the game's
        /// 720 rows shown about 400 high).</summary>
        public const double BigButton = 80;

        /// <summary>Big buttons in a column, each pressing its key (the pause menu, made for thumbs: 0.3.1, Andrew: "from
        /// the pause menu on the phone there's no restart, main menu etc"). The key is shown small at the right unless
        /// the person plays by touch. Returns the y below them.</summary>
        public double BigButtons(DrawingContext dc, double x, double y, double width, string[] labels, string[] keyNames, Key[] keys)
        {
            for (int i = 0; i < labels.Length; i++)
            {
                var r = new Rect(x, y, width, BigButton);
                bool lit = Hovered(r);
                dc.DrawRoundedRectangle(Gfx.B(lit ? 0xFF1E3A8A : 0xE0172036), Gfx.P(lit ? 0xFF60A5FA : 0xFF2C3A55, lit ? 2.5 : 1.5), r, 16, 16);
                Gfx.Text(dc, labels[i], x + 26, y + BigButton / 2 - 15, 24, 0xFFFFFFFF, 0, Gfx.UiBold);
                if (!TouchMode && keyNames != null && i < keyNames.Length && keyNames[i] != null)
                    Gfx.Text(dc, keyNames[i], x + width - 22, y + BigButton / 2 - 10, 16, 0xFF93C5FD, 1, Gfx.Ui);
                AddKeyHit(r, labels[i], keys[i]);
                y += BigButton + 10;
            }
            return y;
        }

        /// <summary>Like Gfx.Keys (pairs of keys and what they do), but each row is a button that presses its key.</summary>
        public double MenuRows(DrawingContext dc, double x, double y, double size, double width, string[] pairs, Key[] keys)
        {
            double w = 0;
            for (int i = 0; i + 1 < pairs.Length; i += 2) w = Math.Max(w, Gfx.Width(pairs[i], size, Gfx.UiBold));
            for (int i = 0; i + 1 < pairs.Length; i += 2)
            {
                var r = new Rect(x - size * 0.6, y - size * 0.2, width, size * 1.4);
                bool lit = Hovered(r);
                if (lit) dc.DrawRoundedRectangle(Gfx.B(0xFF1E3A8A), Gfx.P(0xFF60A5FA, 1.5), r, 8, 8);
                Gfx.Text(dc, pairs[i], x, y, size, lit ? 0xFFFFFFFF : 0xFF93C5FD, 0, Gfx.UiBold);
                Gfx.Text(dc, pairs[i + 1], x + w + size * 0.8, y, size, lit ? 0xFFFFFFFF : 0xFFB8BFCC, 0, Gfx.Ui);
                if (i / 2 < keys.Length) AddKeyHit(r, pairs[i] + " " + pairs[i + 1], keys[i / 2]);
                y += size * 1.45;
            }
            return y;
        }

        /// <summary>Clickable arrows for a value picker row ("Turn time  &lt; 45 s &gt;"): Left and Right at these x.</summary>
        public void AddArrowHits(Rect row, double leftX, double rightX, string label, int item, bool selected, Action select)
        {
            double aw = 34;
            AddHit(new Rect(leftX - aw / 2, row.Y, aw, row.Height), label + " <", selected, select, () => TapKey(Key.Left)).Item = item;
            AddHit(new Rect(rightX - aw / 2, row.Y, aw, row.Height), label + " >", selected, select, () => TapKey(Key.Right)).Item = item;
        }

        /// <summary>Setup screens get a "Back" button in the top-left corner (Esc).</summary>
        protected virtual bool SetupBackButton { get { return true; } }
        void DrawBackButton(DrawingContext dc)
        {
            var r = new Rect(14, 14, 110, 36);
            bool lit = Hovered(r);
            dc.DrawRoundedRectangle(Gfx.B(lit ? 0xFF1E3A8A : 0xB0131A2A), Gfx.P(lit ? 0xFF60A5FA : 0xFF2C3A55, lit ? 2 : 1.2), r, 18, 18);
            Gfx.Text(dc, "\u25C0  Back", r.X + r.Width / 2, r.Y + 7, 16, lit ? 0xFFFFFFFF : 0xFF93C5FD, 0.5, Gfx.UiBold);
            AddKeyHit(r, "Back", Key.Escape);
        }

        /// <summary>How this game is played together on the network ("Co-op", "Race"...; empty = not on the network). Set by
        /// the games list; while it's set, setup and game-over screens show a "Play together" button in the top-right
        /// corner that opens Classic Games' network page with this game chosen. (2026-10-24, Andrew couldn't find Hive
        /// Breach's co-op: the only way in was the start screen's button.)</summary>
        public string PlayTogether = "";
        /// <summary>The person asked to play this game with others on the network.</summary>
        public event EventHandler PlayTogetherRequested;
        public void RequestPlayTogether()
        {
            Audio.StopMusic(); Audio.StopEffects();
            EventHandler h = PlayTogetherRequested;
            if (h != null) h(this, EventArgs.Empty);
        }
        protected virtual bool PlayTogetherButton { get { return !string.IsNullOrEmpty(PlayTogether) && Net == null && PlayTogetherRequested != null; } }
        void DrawPlayTogetherButton(DrawingContext dc)
        {
            var r = new Rect(Width - 14 - 196, 14, 196, 36);
            bool lit = Hovered(r);
            dc.DrawRoundedRectangle(Gfx.B(lit ? 0xFF14532D : 0xB0131A2A), Gfx.P(lit ? 0xFF4ADE80 : 0xFF2C553A, lit ? 2 : 1.2), r, 18, 18);
            Gfx.Text(dc, "Play together  ▶", r.X + r.Width / 2, r.Y + 7, 16, lit ? 0xFFFFFFFF : 0xFF86EFAC, 0.5, Gfx.UiBold);
            AddHit(r, "Play together", false, null, RequestPlayTogether);
        }

        // ---------------------------------------------------------------- flow
        public void Begin()
        {
            if (HasSetup) { State = Phase.Setup; Paused = false; SetupMusic(); }
            else NewGame();
        }

        public void NewGame()
        {
            Paused = false;
            State = Phase.Play;
            Audio.StopEffects();
            Reset();
            StartMusic();
        }

        public void Update(double dt)
        {
            if (dt > 0.1) dt = 0.1;
            if (Net != null) Net.BeforeUpdate(dt);
            if (Paused && Net == null) return;   // a network game keeps going behind its menu
            T += dt;
            if (toast != null) { toastTime -= dt; if (toastTime <= 0) toast = null; }
            switch (State)
            {
                case Phase.Setup: TickSetup(dt); break;
                case Phase.Play: Tick(dt); break;
                case Phase.Ending:
                    endTimer -= dt;
                    TickEnding(dt);
                    if (endTimer <= 0) State = Phase.Over;
                    break;
                case Phase.Over: TickEnding(dt); break;
            }
            if (Net != null) Net.AfterUpdate(dt);
            if (!ClearsTapsItself) tapped.Clear();
        }

        /// <summary>Runs the game for some seconds at 60 steps a second (tests and -Demo snapshots).</summary>
        public void Simulate(double seconds)
        {
            int n = (int)Math.Round(seconds * 60);
            for (int i = 0; i < n; i++) Update(1.0 / 60);
        }

        public void Render(DrawingContext dc)
        {
            hits.Clear();
            if (State == Phase.Setup) { DrawSetup(dc); if (SetupBackButton && !Paused) DrawBackButton(dc); if (PlayTogetherButton && !Paused) DrawPlayTogetherButton(dc); }
            else { Draw(dc); if (State == Phase.Over && PlayTogetherButton) DrawPlayTogetherButton(dc); }
            if (Net != null && State != Phase.Setup) Net.Draw(dc);
            // a panel on top takes the mouse from everything under it
            // on the network, the host's results of a round cover the game's own panel (Engine\NetRound.cs)
            bool shared = Net != null && Net.SharesRoundPanel;
            if (State == Phase.Over && !shared) { hits.Clear(); if (Net != null) Net.DrawOver(dc); else DrawOver(dc); }
            if (Net != null && Net.Round != null && !Net.Lost) { if (shared) hits.Clear(); Net.DrawRound(dc); }
            if (Paused) { hits.Clear(); DrawPause(dc); }
            if (Net != null && Net.Lost) { hits.Clear(); Net.DrawLost(dc); }
            if (ShowStats) DrawStats(dc);
            if (toast != null)
            {
                double a = Math.Min(1, toastTime * 3);
                double w = Gfx.Width(toast, 20, Gfx.UiBold) + 40;
                dc.DrawRoundedRectangle(Gfx.B(Gfx.Alpha(0xFF1B2D50, 0.92 * a)), null, new Rect((Width - w) / 2, 14, w, 40), 20, 20);
                Gfx.Text(dc, toast, Width / 2, 21, 20, Gfx.Alpha(0xFFE6E9F0, a), 0.5, Gfx.UiBold);
            }
        }

        public void Toast(string text) { toast = text; toastTime = 1.6; }

        public void KeyDown(Key k, bool repeat)
        {
            hits.Clear();   // what the last frame drew may be gone now; the next frame registers it again
            if (k == Key.F3 && !repeat) { ShowStats = !ShowStats; return; }
            if (!repeat) { InputCount++; if (State == Phase.Play && !Paused) tapped.Add(k); }
            if (Net != null && !Paused && Net.RoundKey(k, repeat)) return;   // the results of a round on the network
            if (!Paused && (State == Phase.Setup || State == Phase.Play) && !(Net != null && Net.Lost) && TakesKey(k))
            {
                // the game's own use of a shared key (typing a name, a map on M, Esc closing a panel)
                if (State == Phase.Setup) SetupKey(k);
                else if (!repeat) { held.Add(k); OnKey(k); }
                return;
            }
            if (k == Key.M && !repeat) { Toast(Audio.ToggleMusic() ? "Music on" : "Music off"); return; }
            if (k == Key.N && !repeat) { Audio.SoundsOn = !Audio.SoundsOn; if (Audio.SoundsOn) Audio.Ui("select"); Toast(Audio.SoundsOn ? "Sounds on" : "Sounds off"); return; }
            if (Net != null && Net.Key(k, repeat)) return;
            if (Paused)
            {
                if (repeat) return;
                if (PausedKey(k)) return;
                if (k == Key.P || k == Key.Escape || k == Key.Enter || k == Key.Space) Resume();
                else if (k == Key.R && Net == null) { Audio.Ui("select"); Paused = false; Audio.PauseMusic(false); NewGame(); }
                else if (k == Key.Q || k == Key.Back) { Audio.Ui("back"); Paused = false; Audio.PauseMusic(false); Quit(); }
                return;
            }
            switch (State)
            {
                case Phase.Setup:
                    if (repeat && k != Key.Up && k != Key.Down && k != Key.Left && k != Key.Right) return;
                    if (k == Key.Escape || k == Key.Back) { Audio.Ui("back"); if (!SetupBack()) RequestExit(); return; }
                    SetupKey(k);
                    break;
                case Phase.Play:
                    if ((k == Key.Escape || k == Key.P) && !repeat) { Pause(); return; }
                    if (!repeat) { held.Add(k); OnKey(k); }
                    break;
                case Phase.Ending:
                    break;
                case Phase.Over:
                    if (repeat) return;
                    if (Net != null && Net.HostWaits && k != Key.Q) { if (k == Key.Enter || k == Key.Space || k == Key.Escape || k == Key.Back) Net.Banner("The others are still playing: the results come next (Q: back to the lobby anyway)."); }
                    else if (Net != null) { if (k == Key.Enter || k == Key.Space || k == Key.Escape || k == Key.Q || k == Key.Back) { Audio.Ui("back"); Quit(); } }
                    else if (k == Key.Enter || k == Key.Space) { Audio.Ui("select"); OverConfirm(); }
                    else if (k == Key.R) { Audio.Ui("select"); NewGame(); }
                    else if (k == Key.Escape || k == Key.Q || k == Key.Back) { Audio.Ui("back"); Quit(); }
                    break;
            }
        }

        public void KeyUp(Key k)
        {
            held.Remove(k);   // (a key let go before the game looked still counts once: see tapped)
            if (State == Phase.Play && !Paused) OnKeyUp(k);
        }

        /// <summary>The window lost focus: let go of every key and pause.</summary>
        public void FocusLost()
        {
            foreach (Key k in held.ToList()) { held.Remove(k); if (State == Phase.Play) OnKeyUp(k); }
            if (State == Phase.Play && !Paused && !Autopilot && Net == null) Pause();
        }

        // test hooks: press and release keys as if typed
        public void Press(Key k) { KeyDown(k, false); }
        public void Release(Key k) { KeyUp(k); }
        public void Tap(Key k) { KeyDown(k, false); KeyUp(k); }

        // A key counts as down while it is held, and also once if it was pressed and let go again before the game
        // looked (a quick tap between two frames, or during a slow one, is never lost).
        protected bool Down(Key a) { return held.Contains(a) || tapped.Contains(a); }
        protected bool Down(Key a, Key b) { return Down(a) || Down(b); }
        protected bool Down(Key a, Key b, Key c) { return Down(a) || Down(b) || Down(c); }
        /// <summary>Keys pressed since the game last looked (see Down).</summary>
        protected readonly HashSet<Key> tapped = new HashSet<Key>();
        /// <summary>A game that moves in fixed steps calls this after each step has read the keys; other games have
        /// it done after every Update.</summary>
        protected void ClearTaps() { tapped.Clear(); }
        protected virtual bool ClearsTapsItself { get { return false; } }

        // ---------------------------------------------------------------- timing (F3 shows it)
        /// <summary>F3: frames a second, the time each frame takes and how long a key takes to show on screen.</summary>
        public bool ShowStats;
        /// <summary>Key presses and clicks so far (GameSurface times each from the event to the picture).</summary>
        public long InputCount;
        /// <summary>Steps of the game's world so far, for games that move in fixed steps (0: not counted).</summary>
        public long WorldSteps;
        public readonly FrameStats Stats = new FrameStats();
        /// <summary>A line of the game's own timing for the F3 panel.</summary>
        protected virtual string StatsDetail { get { return null; } }

        void DrawStats(DrawingContext dc)
        {
            FrameStats s = Stats;
            var lines = new List<string>();
            lines.Add(s.Fps.ToString("0", CultureInfo.InvariantCulture) + " fps   frame every " + s.IntervalMs.ToString("0.0", CultureInfo.InvariantCulture) + " ms (screen " + s.ScreenHz.ToString("0", CultureInfo.InvariantCulture) + " Hz)");
            lines.Add("update " + s.UpdateMs.ToString("0.0", CultureInfo.InvariantCulture) + " ms   draw " + s.DrawMs.ToString("0.0", CultureInfo.InvariantCulture) + " ms   slowest " + s.WorstMs.ToString("0.0", CultureInfo.InvariantCulture) + " ms");
            lines.Add("key to picture " + (s.InputMs < 0 ? "-" : s.InputMs.ToString("0.0", CultureInfo.InvariantCulture)) + " ms (worst of last 20: " + s.InputWorstMs.ToString("0.0", CultureInfo.InvariantCulture) + " ms)");
            string extra = StatsDetail;
            if (!string.IsNullOrEmpty(extra)) lines.AddRange(extra.Split('\n'));
            double h = lines.Count * 18 + 12;
            // near the top in the middle, under where toasts go, clear of most games' scores and messages
            double x0 = Width / 2 - 270, y0 = 62;
            dc.DrawRectangle(Gfx.B(0xC0000000), null, new Rect(x0, y0, 540, h));
            for (int i = 0; i < lines.Count; i++) Gfx.Text(dc, lines[i], x0 + 8, y0 + 6 + i * 18, 13, 0xFF9AE6B4, 0, Gfx.Ui);
        }

        public void Pause() { if (Paused || State != Phase.Play) return; Paused = true; Audio.Ui("pause"); if (Net == null) { Audio.PauseMusic(true); OnPauseChanged(true); } }
        protected virtual void OnPauseChanged(bool paused) { }
        public void Resume() { if (!Paused) return; Paused = false; Audio.Ui("unpause"); if (Net == null) { Audio.PauseMusic(false); OnPauseChanged(false); } }

        /// <summary>Back out of a game: to its setup screen if it has one, else to the game picker.</summary>
        protected void Quit()
        {
            // online: leave the game (back to its own menu), never out of the app (Sahuarita Smash! SE)
            if (Net != null && Net.Leave != null) { Net.Leave(); return; }
            if (HasSetup && Net == null) { State = Phase.Setup; Audio.StopEffects(); SetupMusic(); }
            else RequestExit();
        }
        public void RequestExit()
        {
            Audio.StopMusic(); Audio.StopEffects();
            EventHandler h = ExitRequested;
            if (h != null) h(this, EventArgs.Empty);
        }

        /// <summary>Ends the game: saves the score, plays the game-over (or high score) jingle, then shows the result.</summary>
        protected void EndGame(long score, string detail, string key) { EndGame(score, detail, key, "Game over", null); }
        protected void EndGame(long score, string detail, string key, string title, Song jingle)
        {
            LastScore = score; OverDetail = detail ?? ""; OverKey = key ?? ""; OverTitle = title;
            PrevBest = Scores != null ? Scores.Best(Id, OverKey, Player) : 0;
            if (Scores != null && score > 0) Scores.Add(Id, OverKey, Player, score, OverDetail);
            NewBest = score > 0 && score > PrevBest;
            OverTop = Scores != null ? Scores.Top(Id, OverKey, 5) : new List<ScoreEntry>();
            Audio.StopMusic();
            Song j = NewBest && Tunes.HighScore != null ? Tunes.HighScore : (jingle ?? Tunes.GameOver);
            if (j != null) Audio.Jingle(j);
            endLength = j != null ? Math.Min(5, j.Seconds) + 0.3 : 1.0;
            endTimer = endLength;
            State = Phase.Ending;
        }
        /// <summary>On the network: the host has decided the round (the winner is in and the time to finish is up, or
        /// the last one still playing has won already), so a game still going ends now, keeping its score.</summary>
        public virtual void EndForNet(string title) { if (State == Phase.Play) EndGame(NetScore, "a race on the network", OverKey, title, null); }
        /// <summary>For snapshots: end the game now with this score.</summary>
        public virtual void ForceEnd(long score) { if (State == Phase.Play) EndGame(score, "a test game", OverKey); }
        protected double EndProgress { get { return endLength > 0 ? Math.Max(0, Math.Min(1, 1 - endTimer / endLength)) : 1; } }

        // ---------------------------------------------------------------- shared panels
        protected virtual void DrawOver(DrawingContext dc)
        {
            double w = 460, h = 400, x = (Width - w) / 2, y = (Height - h) / 2;
            dc.DrawRectangle(Gfx.B(0x99000000), null, new Rect(0, 0, Width, Height));
            Gfx.Panel(dc, new Rect(x, y, w, h));
            string title = NewBest ? "New best score!" : OverTitle;
            Gfx.Text(dc, title, Width / 2, y + 18, 34, NewBest ? 0xFFFBBF24 : 0xFFE6E9F0, 0.5, Gfx.UiBold);
            Gfx.Text(dc, Gfx.N(LastScore), Width / 2, y + 66, 40, 0xFF93C5FD, 0.5, Gfx.Mono);
            string sub = OverDetail;
            if (!NewBest && PrevBest > 0) sub = (sub.Length > 0 ? sub + "   " : "") + "Your best: " + Gfx.N(PrevBest);
            Gfx.Text(dc, sub, Width / 2, y + 116, 16, 0xFF8C93A5, 0.5);
            Gfx.Text(dc, "Top scores", x + 40, y + 150, 16, 0xFFE6E9F0, 0, Gfx.UiBold);
            double ry = y + 178;
            int rank = 1;
            foreach (ScoreEntry e in OverTop)
            {
                bool mine = string.Equals(e.Player, Player, StringComparison.OrdinalIgnoreCase) && e.Score == LastScore;
                uint c = mine ? 0xFFFBBF24 : 0xFFB8BFCC;
                Gfx.Text(dc, rank + ".", x + 40, ry, 17, c, 0);
                Gfx.Text(dc, e.Player, x + 70, ry, 17, c, 0);
                Gfx.Text(dc, Gfx.N(e.Score), x + w - 40, ry, 17, c, 1, Gfx.Mono);
                ry += 26; rank++;
            }
            if (OverTop.Count == 0) Gfx.Text(dc, "No scores yet", x + 40, ry, 16, 0xFF5C6375, 0);
            OverButtons(dc, Width / 2, y + h - 40, "R: start again", "Esc: back");
        }
        /// <summary>The buttons under a game-over panel: Enter (play again, or the game's own), R and Esc; on the network one
        /// button back to the lobby.</summary>
        protected void OverButtons(DrawingContext dc, double cx, double y, string again, string back)
        {
            if (Net != null) MenuButtons(dc, cx, y, 15, new[] { "Enter or Esc: back to the lobby" }, new[] { Key.Enter });
            else if (again == null) MenuButtons(dc, cx, y, 15, new[] { OverConfirmText, back }, new[] { Key.Enter, Key.Escape });
            else MenuButtons(dc, cx, y, 15, new[] { OverConfirmText, again, back }, new[] { Key.Enter, Key.R, Key.Escape });
        }
        /// <summary>The game's own game-over panel (network games draw it under their results).</summary>
        public void DrawOverPanel(DrawingContext dc) { DrawOver(dc); }

        protected virtual void DrawPause(DrawingContext dc)
        {
            double w = 480, h = 84 + 5 * (BigButton + 10) + 16, x = (Width - w) / 2, y = (Height - h) / 2;
            dc.DrawRectangle(Gfx.B(0xA0000000), null, new Rect(0, 0, Width, Height));
            if (Net != null)
            {
                // the others keep playing: this is only a menu
                w = 560; h = 110 + 4 * (BigButton + 10) + 16; x = (Width - w) / 2; y = (Height - h) / 2;
                Gfx.Panel(dc, new Rect(x, y, w, h));
                Gfx.Text(dc, "Menu", Width / 2, y + 18, 34, 0xFFE6E9F0, 0.5, Gfx.UiBold);
                Gfx.Text(dc, "The game keeps going for everyone while this is open.", Width / 2, y + 66, 15, 0xFF8C93A5, 0.5);
                BigButtons(dc, x + 30, y + 110, w - 60, PauseMenuOnline(), new[] { "P / Esc", "Q", "M", "N" }, new[] { Key.Escape, Key.Q, Key.M, Key.N });
                return;
            }
            Gfx.Panel(dc, new Rect(x, y, w, h));
            Gfx.Text(dc, "Paused", Width / 2, y + 20, 34, 0xFFE6E9F0, 0.5, Gfx.UiBold);
            BigButtons(dc, x + 30, y + 84, w - 60, PauseMenu(HasSetup ? "Main menu" : "Back to the games"), new[] { "P / Esc", "R", "Q", "M", "N" }, new[] { Key.Escape, Key.R, Key.Q, Key.M, Key.N });
        }

        /// <summary>The pause menu's words: keep playing, start again, the way back, music, sounds.</summary>
        protected static string[] PauseMenu(string back)
        {
            return new[] { "Keep playing", "Restart", back, Audio.MusicOn ? "Music off" : "Music on", Audio.SoundsOn ? "Sounds off" : "Sounds on" };
        }
        protected static string[] PauseMenuOnline()
        {
            return new[] { "Back to the game", "Leave this game", Audio.MusicOn ? "Music off" : "Music on", Audio.SoundsOn ? "Sounds off" : "Sounds on" };
        }

        /// <summary>A vertical menu for setup screens. Returns the y below it.</summary>
        protected double DrawMenu(DrawingContext dc, double cx, double y, string[] items, int index) { return DrawMenu(dc, cx, y, items, index, null); }
        /// <summary>The same, clickable: pointing at an item calls select(i), a click then presses Enter.</summary>
        protected double DrawMenu(DrawingContext dc, double cx, double y, string[] items, int index, Action<int> select)
        {
            for (int i = 0; i < items.Length; i++)
            {
                bool sel = i == index;
                double w = 420, h = 46;
                var r = new Rect(cx - w / 2, y, w, h);
                if (select != null) { int item = i; AddItemHit(r, items[i], sel, () => select(item), Key.Enter).Item = i; }
                dc.DrawRoundedRectangle(Gfx.B(sel ? 0xFF1B2D50 : 0xFF13161D), Gfx.P(sel ? 0xFF3B82F6 : 0xFF252A35, 2), r, 10, 10);
                Gfx.Text(dc, items[i], cx, y + 10, 20, sel ? 0xFFFFFFFF : 0xFFB8BFCC, 0.5, sel ? Gfx.UiBold : Gfx.Ui);
                if (sel)
                {
                    Gfx.Text(dc, "\u25B6", cx - w / 2 + 16, y + 11, 18, 0xFF60A5FA, 0);
                }
                y += h + 10;
            }
            return y;
        }
        protected static bool IsUp(Key k) { return k == Key.Up || k == Key.W; }
        protected static bool IsDown(Key k) { return k == Key.Down || k == Key.S; }
        protected static bool IsLeft(Key k) { return k == Key.Left || k == Key.A; }
        protected static bool IsRight(Key k) { return k == Key.Right || k == Key.D; }
        protected static int MenuMove(Key k, int index, int count)
        {
            int n = index;
            if (IsUp(k)) n = (index + count - 1) % count;
            else if (IsDown(k)) n = (index + 1) % count;
            if (n != index) Audio.Ui("move");
            return n;
        }
    }

    /// <summary>Something a game drew that the mouse can point at and click (see Game.AddHit).</summary>
    public sealed class HitArea
    {
        public Rect Area;
        /// <summary>What it says (tests and snapshots find items by it).</summary>
        public string Label = "";
        /// <summary>Drawn as the menu's selected item.</summary>
        public bool Selected;
        /// <summary>Hits with the same item number (0 or more) belong to one menu item (a row and its arrows).</summary>
        public int Item = -1;
        /// <summary>Makes it the selected item (null: a button with nothing to select).</summary>
        public Action Select;
        /// <summary>What a click does after selecting it.</summary>
        public Action Click;
    }

    public sealed class FrameStats
    {
        public double Fps, IntervalMs, ScreenHz, UpdateMs, DrawMs, WorstMs, InputMs = -1, InputWorstMs;
        double secT, secWorst, minGap = 1000; int secFrames; readonly Queue<double> inputs = new Queue<double>();
        public void Tick(double gapMs)
        {
            if (gapMs <= 0 || gapMs > 500) return;
            IntervalMs += (gapMs - IntervalMs) * 0.1;
            if (gapMs < minGap) minGap = gapMs;
            secT += gapMs;
            if (secT >= 1000) { Fps = secFrames * 1000 / secT; WorstMs = secWorst; ScreenHz = 1000 / Math.Max(1, minGap); secT = 0; secFrames = 0; secWorst = 0; minGap = 1000; }
        }
        public void Done(double update, double draw)
        {
            UpdateMs += (update - UpdateMs) * 0.1;
            if (draw >= 0) { DrawMs += (draw - DrawMs) * 0.1; secFrames++; }
            secWorst = Math.Max(secWorst, update + Math.Max(0, draw));
        }
        public void Input(double ms)
        {
            InputMs = ms;
            inputs.Enqueue(ms); while (inputs.Count > 20) inputs.Dequeue();
            InputWorstMs = inputs.Max();
        }
    }
}
