// Classic Games - Sahuarita Smash!, part 5: the game. The menus (title, choosing fighters and the stage, the ladder, how to
// play), the keys and the game controllers, the match on screen (a count-down, the fight, "GAME!", the results), the
// ladder of six matches against the computer that gets harder each round (the last against a giant), the music, the
// scenes for snapshots and keeping a match for an App Hub update. The rules are BrawlSim.cs, the fighters and stages
// BrawlData.cs, the pictures BrawlArt.cs, the computer players BrawlBot.cs, the sounds BrawlSfx.cs.
//
// Keys (one person can use either set; two people on one keyboard take one each):
//   left:  A D move, W jump (and up), S down, F attack, G strong, H special, R shield, T grab, Space jumps too
//   right: arrows, K attack, L strong, ; special, O shield, I grab (or the number pad: 1 2 3, 0 shield, 5 grab)
//   controllers: stick, A attack, B special, X / Y jump, right stick or left trigger strong, RB / right trigger shield,
//   LB grab, Start pauses
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Input;
using Avalonia.Media;

namespace ClassicGames
{
    public sealed partial class BrawlGame : Game
    {
        public const string GameName = "Sahuarita Smash!";
        public override string Id { get { return "Brawl"; } }
        public override string Title { get { return GameName; } }
        public override double Width { get { return 1280; } }
        public override double Height { get { return 720; } }
        public override uint Background { get { return 0xFF0B0D12; } }
        protected override bool HasSetup { get { return true; } }
        protected override bool ClearsTapsItself { get { return true; } }

        public enum Screen { Title, Select, Help, Ladder, Keys, Lineup, Moves, Swings, Online, OnlineJoin, OnlineLobby }   // Online*: BrawlOnline.cs (Sauce Edition)   // Lineup, Moves, Swings: snapshots only
        public Screen Menu = Screen.Title;
        public int Sel;
        // who plays: 0 off, 1 a person, 2 the computer; Level: 0 Easy, 1 Normal, 2 Hard, 3 Champion; Pick: a fighter, -1 any
        public readonly int[] Who = { 1, 2, 0, 0 };
        public readonly int[] Level = { 1, 1, 1, 1 };
        public readonly int[] Pick = { 0, 1, 4, 6 };
        public int StagePick = 0;   // -1 any
        public int Lives = 3, Minutes = 0;
        public bool Surprises = true;
        public static readonly string[] LevelNames = { "Easy", "Normal", "Hard", "Champion" };
        public static readonly int[] BotLevels = { 1, 3, 6, 9 };
        static readonly int[] LifeChoices = { 1, 2, 3, 4, 5 };
        static readonly int[] MinuteChoices = { 2, 3, 5 };

        // the ladder
        public bool Ladder;
        public int LadderPick, LadderDiff = 1, LadderRound;
        public long LadderScore;
        public const int LadderRounds = 6;
        int[] ladderFoes;

        // the match
        public BrawlSim Sim;
        BrawlBot[] bots;
        int[] seatOf;
        public string[] Names;
        public readonly BrawlView View = new BrawlView();
        public enum Stage { Countdown, Fight, Results, LadderCard }
        public Stage Step = Stage.Countdown;
        double acc, stageT;
        public int StageIndex;
        bool resultsDone;
        /// <summary>The built-in players' demo (the start screen's card, -Demo): all four seats play.</summary>
        public bool DemoAll = true;

        public BrawlGame() { Tunes = Music.For("Brawl"); BrawlMusic.Want("menu", "select"); }

        // ================================================================ the menus
        // the music: a recorded song when there is one (BrawlMusic.cs), else the built-in tune; when the recorded one
        // finishes decoding while its built-in stand-in plays, it takes over with a crossfade
        string musicRole; Song musicFallback;
        void PlayMusic(string role, Song fallback, double fade)
        {
            musicRole = role; musicFallback = fallback;
            Song s = BrawlMusic.Get(role) ?? fallback;
            if (s != null && Audio.Current != s) Audio.Crossfade(s, fade);
        }
        void MusicCatchUp()
        {
            if (musicRole == null || Audio.Current != musicFallback) return;
            Song s = BrawlMusic.Get(musicRole);
            if (s != null) Audio.Crossfade(s, 1.5);
        }
        protected override void SetupMusic() { MenuMusic(1.2); LoadKeys(); }
        // the title, help and controls play the menu song; choosing fighters and the ladder a quieter tune in the same key
        void MenuMusic(double fade)
        {
            string want = Menu == Screen.Select || Menu == Screen.Ladder ? "select" : "menu";
            if (musicRole != want || Audio.Current == null) PlayMusic(want, want == "select" ? Music.BrawlFill(0) : Tunes.Theme, fade);
        }
        double resultsMusicAt;
        // hold music while paused (Andrew, 2026-10-31: "like elevator music"); back to the stage song on resuming
        protected override void OnPauseChanged(bool paused)
        {
            if (Net != null) return;
            if (paused)
            {
                Audio.PauseMusic(false);
                Song h = BrawlMusic.Get("hold") ?? Music.BrawlFill(3);
                if (h != null) Audio.Crossfade(h, 0.6);
                return;
            }
            Song s = musicRole != null ? BrawlMusic.Get(musicRole) ?? musicFallback : null;
            if (s != null) Audio.Crossfade(s, 0.8);
        }

        // ================================================================ the keys (BrawlKeys.cs), per player next to the scores
        public readonly BrawlKeys Keys = new BrawlKeys();
        bool keysUnreadable;
        string KeysPath { get { return Scores != null && Scores.Path != null ? System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Scores.Path), "brawl-keys.txt") : null; } }
        /// <summary>For tests: where the keys are saved when there is no scorebook.</summary>
        public string KeysFileOverride;
        string KeysFile { get { return KeysFileOverride ?? KeysPath; } }
        public void LoadKeys()
        {
            Keys.Reset(); keysUnreadable = false;
            string f = KeysFile;
            if (f == null || !System.IO.File.Exists(f)) return;
            try
            {
                foreach (string line in System.IO.File.ReadAllLines(f))
                {
                    string[] p = line.Split('\t');
                    if (p.Length < 4 || !string.Equals(p[0], Player, StringComparison.OrdinalIgnoreCase)) continue;
                    Keys.Read(p[2], p[3]);
                }
            }
            catch (Exception) { keysUnreadable = true; }   // never written over: the usual keys this time
        }
        public void SaveKeys()
        {
            string f = KeysFile;
            if (f == null || keysUnreadable) return;
            try
            {
                var lines = new List<string>();
                string extra = "";
                if (System.IO.File.Exists(f))
                    foreach (string l in System.IO.File.ReadAllLines(f))
                    {
                        string[] p = l.Split('\t');
                        // this player's old line goes; anything after the known fields is kept for a newer Classic Games
                        if (string.Equals(p[0], Player, StringComparison.OrdinalIgnoreCase)) { if (p.Length > 4) extra = "\t" + string.Join("\t", p.Skip(4).ToArray()); continue; }
                        if (l.Length > 0) lines.Add(l);
                    }
                lines.Add(Player.Replace('\t', ' ') + "\t1\t" + Keys.Fields() + extra);
                AppHubSaves.SafeFile.WriteText(f, string.Join("\r\n", lines.ToArray()) + "\r\n");
            }
            catch (Exception) { }
        }
        bool ExtraDown(Key k) { return Keys.Extra(k) && Down(k); }

        // the Controls screen: rows = the nine actions, columns = the two sets; then "the usual keys" and "back"
        public int KeyRow, KeyCol;
        public bool KeyListening;
        const int KeyRows = BrawlKeys.Count + 2;
        protected override bool TakesKey(Key k) { return State == Phase.Setup && (Menu == Screen.Keys && KeyListening || OnlineTakesKey(k)); }
        void KeysScreenKey(Key k)
        {
            if (KeyListening)
            {
                if (k == Key.Escape || k == Key.Enter) { KeyListening = false; Audio.Ui("back"); return; }
                if (!BrawlKeys.Allowed(k)) { Toast(BrawlKeys.Name(k) + " is used by the game (pause, music, menus): choose another key"); Audio.Ui("back"); return; }
                Keys.Assign(KeyCol, KeyRow, k); KeyListening = false; Audio.Ui("select"); SaveKeys();
                return;
            }
            if (IsUp(k) || IsDown(k)) { KeyRow = MenuMove(k, KeyRow, KeyRows); Audio.Ui("move"); return; }
            if ((IsLeft(k) || IsRight(k)) && KeyRow < BrawlKeys.Count) { KeyCol = 1 - KeyCol; Audio.Ui("move"); return; }
            if (k != Key.Enter && k != Key.Space) return;
            if (KeyRow < BrawlKeys.Count) { KeyListening = true; Audio.Ui("select"); }
            else if (KeyRow == BrawlKeys.Count) { Keys.Reset(); SaveKeys(); Toast("Back to the usual keys"); Audio.Ui("select"); }
            else { Menu = Screen.Title; Sel = 3; Audio.Ui("back"); }
        }

        void DrawKeys(DrawingContext dc)
        {
            Gfx.Panel(dc, new Rect(160, 40, Width - 320, Height - 80), 0xF0101522, 0xFFFB923C);
            Gfx.Text(dc, "Controls", Width / 2, 56, 34, 0xFFFDE047, 0.5, Gfx.UiBold);
            Gfx.Text(dc, "Choose a key to change it, then press the new key. Two people on one keyboard take a set each; on your own, both work.", Width / 2, 106, 15, 0xFFB8BFCC, 0.5, Gfx.Ui);
            double y = 146, lx = 260, cw = 260;
            double[] cx = { lx + 220, lx + 220 + cw };
            for (int s = 0; s < 2; s++) Gfx.Text(dc, BrawlKeys.SetNames[s], cx[s] + cw / 2 - 20, y, 17, 0xFF93C5FD, 0.5, Gfx.UiBold);
            y += 32;
            for (int a = 0; a < BrawlKeys.Count; a++)
            {
                Gfx.Text(dc, BrawlKeys.Actions[a], lx, y + 7, 17, 0xFFE5E7EB, 0, Gfx.UiBold);
                for (int s = 0; s < 2; s++)
                {
                    bool sel = KeyRow == a && KeyCol == s, wait = sel && KeyListening;
                    var r = new Rect(cx[s], y, cw - 40, 34);
                    int row = a, col = s;
                    AddItemHit(r, BrawlKeys.SetNames[s] + " " + BrawlKeys.Actions[a], sel, () => { KeyRow = row; KeyCol = col; }, Key.Enter);
                    dc.DrawRoundedRectangle(Gfx.B(wait ? 0xFF4A2A10 : sel ? 0xFF1B2D50 : 0xFF13161D), Gfx.P(wait ? 0xFFFB923C : sel ? 0xFF3B82F6 : 0xFF252A35, 2), r, 8, 8);
                    Key k = Keys.Set[s][a];
                    bool changed = k != (s == 0 ? BrawlKeys.LeftUsual[a] : BrawlKeys.RightUsual[a]);
                    Gfx.Text(dc, wait ? "press a key..." : BrawlKeys.Name(k), r.X + r.Width / 2, y + 6, 17, wait ? 0xFFFDBA74 : changed ? 0xFFFDE047 : 0xFFFFFFFF, 0.5, sel ? Gfx.UiBold : Gfx.Ui);
                }
                y += 40;
            }
            y += 8;
            string extras = Keys.IsUsual ? "Also: Space jumps (left keys); the number pad works for the right keys; on your own, Z X C V are attack, special, shield and strong."
                                         : "The extra keys (Space, the number pad, Z X C V) still work unless you gave them to something else.";
            Gfx.Text(dc, extras, Width / 2, y, 14, 0xFF8C93A5, 0.5, Gfx.Ui);
            y += 30;
            string[] items = { "Use the usual keys", "Back" };
            for (int i = 0; i < 2; i++)
            {
                int row = BrawlKeys.Count + i;
                bool sel = KeyRow == row;
                var r = new Rect(Width / 2 - 330 + i * 360, y, 300, 40);
                AddItemHit(r, items[i], sel, () => { KeyRow = row; }, Key.Enter);
                dc.DrawRoundedRectangle(Gfx.B(sel ? 0xFF1B2D50 : 0xFF13161D), Gfx.P(sel ? 0xFF3B82F6 : 0xFF252A35, 2), r, 10, 10);
                Gfx.Text(dc, items[i], r.X + r.Width / 2, y + 8, 18, sel ? 0xFFFFFFFF : 0xFFB8BFCC, 0.5, sel ? Gfx.UiBold : Gfx.Ui);
            }
            Gfx.Text(dc, KeyListening ? "Esc: keep the old key" : "Arrows choose, Enter changes, Esc goes back. Controllers can't be changed here.", Width / 2, Height - 76, 14, 0xFF8C93A5, 0.5, Gfx.Ui);
        }
        protected override void StartMusic() { BStageDef st = BrawlData.Stages[StageIndex]; BrawlMusic.Want("victory", "defeat", "results", "hold"); PlayMusic(BrawlMusic.RoleFor(st), Music.BrawlSong(st.Song), 1.0); }

        protected override bool SetupBack()
        {
            if (Menu == Screen.Title) return false;
            if (OnlineScreen) return OnlineBack();   // online
            Sel = Menu == Screen.Keys ? 3 : 0; Menu = Screen.Title; return true;
        }

        int SelectRows { get { return 4 * 2 + 4; } }   // per seat: fighter, who; then stage, rules, surprises, start

        protected override void SetupKey(Key k)
        {
            switch (Menu)
            {
                case Screen.Online: case Screen.OnlineJoin: case Screen.OnlineLobby: OnlineKey(k); return;   // online
                case Screen.Title:
                    {
                        if (IsUp(k) || IsDown(k)) { Sel = MenuMove(k, Sel, 6); Audio.Ui("move"); return; }
                        if (k != Key.Enter && k != Key.Space) return;
                        Audio.Ui("select");
                        if (Sel == 0) { Menu = Screen.Select; Sel = 0; }
                        else if (Sel == 1) { Menu = Screen.Ladder; Sel = 0; }
                        else if (Sel == 2) { Menu = Screen.Help; }
                        else if (Sel == 3) { Menu = Screen.Keys; KeyRow = 0; KeyCol = 0; KeyListening = false; }
                        else if (Sel == 4) { Menu = Screen.Online; Sel = 0; onlineNote = ""; }   // online (Sauce Edition)
                        else RequestExit();
                        return;
                    }
                case Screen.Keys:
                    KeysScreenKey(k);
                    return;
                case Screen.Help:
                    if (k == Key.Enter || k == Key.Space) { Audio.Ui("back"); Menu = Screen.Title; }
                    return;
                case Screen.Ladder:
                    {
                        if (IsUp(k) || IsDown(k)) { Sel = MenuMove(k, Sel, 3); Audio.Ui("move"); return; }
                        int d = IsLeft(k) ? -1 : IsRight(k) ? 1 : 0;
                        if (d != 0)
                        {
                            if (Sel == 0) LadderPick = (LadderPick + d + BrawlData.Fighters.Count) % BrawlData.Fighters.Count;
                            if (Sel == 1) LadderDiff = Math.Max(0, Math.Min(2, LadderDiff + d));
                            Audio.Ui("move"); return;
                        }
                        if (k == Key.Enter || k == Key.Space) { Audio.Ui("select"); StartLadder(); }
                        return;
                    }
                case Screen.Select:
                    {
                        if (IsUp(k) || IsDown(k)) { Sel = MenuMove(k, Sel, SelectRows); Audio.Ui("move"); return; }
                        int d = IsLeft(k) ? -1 : IsRight(k) ? 1 : 0;
                        if (k == Key.Enter || k == Key.Space)
                        {
                            // Enter on the last row starts; anywhere else it jumps there
                            if (Sel == SelectRows - 1) TryStart(); else { Sel = SelectRows - 1; Audio.Ui("move"); }
                            return;
                        }
                        if (d == 0) return;
                        Change(Sel, d);
                        Audio.Ui("move");
                        return;
                    }
            }
        }

        void Change(int row, int d)
        {
            int n = BrawlData.Fighters.Count;
            if (row < 8)
            {
                int seat = row / 2;
                if (row % 2 == 0) { Pick[seat] = Pick[seat] + d; if (Pick[seat] >= n) Pick[seat] = -1; else if (Pick[seat] < -1) Pick[seat] = n - 1; if (Who[seat] == 0) Who[seat] = 2; }
                else
                {
                    // off, a person, the computer (Easy / Normal / Hard / Champion)
                    int v = Who[seat] == 0 ? 0 : Who[seat] == 1 ? 1 : 2 + Level[seat];
                    v = (v + d + 6) % 6;
                    Who[seat] = v == 0 ? 0 : v == 1 ? 1 : 2; if (v >= 2) Level[seat] = v - 2;
                }
                return;
            }
            switch (row)
            {
                case 8: StagePick += d; if (StagePick >= BrawlData.Stages.Count) StagePick = -1; else if (StagePick < -1) StagePick = BrawlData.Stages.Count - 1; break;
                case 9:
                    {
                        // 1-5 lives, then 2 / 3 / 5 minutes
                        var all = new List<string>();
                        foreach (int l in LifeChoices) all.Add("L" + l);
                        foreach (int m in MinuteChoices) all.Add("M" + m);
                        int at = all.IndexOf(Minutes > 0 ? "M" + Minutes : "L" + Lives);
                        at = (Math.Max(0, at) + d + all.Count) % all.Count;
                        string s = all[at];
                        if (s[0] == 'L') { Lives = s[1] - '0'; Minutes = 0; } else { Minutes = s[1] - '0'; }
                        break;
                    }
                case 10: Surprises = !Surprises; break;
            }
        }

        int Playing { get { int n = 0; for (int i = 0; i < 4; i++) if (Who[i] != 0) n++; return n; } }

        void TryStart()
        {
            if (Playing < 2) { Toast("Choose at least two fighters (a person or the computer)"); Audio.Ui("back"); return; }
            Audio.Ui("select");
            Ladder = false;
            NewGame();
        }

        void StartLadder()
        {
            Ladder = true; LadderRound = 0; LadderScore = 0;
            // the foes: different fighters each round, never the one you picked; the last is a giant
            var pool = new List<int>();
            for (int i = 0; i < BrawlData.Fighters.Count; i++) if (i != LadderPick) pool.Add(i);
            ladderFoes = new int[LadderRounds + 1];
            for (int r = 0; r <= LadderRounds; r++) { int at = Rng.Next(pool.Count); ladderFoes[r] = pool[at]; if (pool.Count > 3) pool.RemoveAt(at); }
            NewGame();
        }

        // ================================================================ a match
        protected override void Reset()
        {
            if (Online != null && Online.Match != null && Net != null) { ResetOnline(); return; }   // online
            var defs = new List<BFighterDef>();
            var who = new List<int>(); var levels = new List<int>(); var seats = new List<int>(); var names = new List<string>();
            int lives = Lives, minutes = Minutes;
            bool giant = false;
            if (Ladder)
            {
                int r = LadderRound;
                defs.Add(BrawlData.Fighters[LadderPick]); who.Add(1); levels.Add(0); seats.Add(0); names.Add(Player);
                int foes = r == 2 ? 2 : 1;
                // Easy 1-3, Normal 3-5, Hard 5-9 by the last round (Normal used to climb to 8: too much, Andrew 2026-10-29)
                int lv = Math.Min(9, new[] { 1, 3, 5 }[LadderDiff] + (LadderDiff == 2 ? r : r / 2));
                for (int i = 0; i < foes; i++) { defs.Add(BrawlData.Fighters[ladderFoes[r + i]]); who.Add(2); levels.Add(lv); seats.Add(1 + i); names.Add(BrawlData.Fighters[ladderFoes[r + i]].Name); }
                lives = r == LadderRounds - 1 ? 3 : 2; minutes = 0;
                giant = r == LadderRounds - 1;
                StageIndex = (LadderPick + r) % BrawlData.Stages.Count;
            }
            else
            {
                bool all = Autopilot;
                for (int s = 0; s < 4; s++)
                {
                    int w = all ? (DemoAll || Who[s] != 0 ? 2 : 0) : Who[s];
                    if (w == 0) continue;
                    int p = Pick[s] >= 0 ? Pick[s] : Rng.Next(BrawlData.Fighters.Count);
                    defs.Add(BrawlData.Fighters[p]); who.Add(w); levels.Add(all ? 4 + s : BotLevels[Level[s]]); seats.Add(s);
                    names.Add(w == 1 ? (HumanIndex(s) == 0 ? Player : "Player " + (s + 1)) : BrawlData.Fighters[p].Name);
                }
                StageIndex = StagePick >= 0 ? StagePick : Rng.Next(BrawlData.Stages.Count);
            }
            Sim = new BrawlSim(BrawlData.Stages[StageIndex], defs, lives, minutes, (uint)Rng.Next(1, int.MaxValue));
            Sim.Surprises = Ladder || Surprises;
            if (giant) { BFighter g = Sim.F[1]; g.Size = 1.6; }
            bots = new BrawlBot[defs.Count];
            seatOf = seats.ToArray();
            for (int i = 0; i < defs.Count; i++) if (who[i] == 2 || Autopilot) bots[i] = new BrawlBot(i, levels[i] > 0 ? levels[i] : 5);
            Names = names.ToArray();
            for (int i = 0; i < defs.Count && i < 4; i++) View.Tags[i] = who[i] == 2 ? "CPU" : "P" + (seats[i] + 1);
            View.Fx.Clear(); View.Follow(Sim, true);
            Step = Ladder ? Stage.LadderCard : Stage.Countdown;
            stageT = 0; acc = 0; resultsDone = false;
        }

        int HumanIndex(int seat) { int h = 0; for (int s = 0; s < seat; s++) if (Who[s] == 1) h++; return h; }
        int Humans { get { if (Ladder) return 1; int h = 0; for (int s = 0; s < 4; s++) if (Who[s] == 1) h++; return h; } }

        // ---------------------------------------------------------------- the buttons
        /// <summary>Tests: the buttons a slot's person is pressing now.</summary>
        public BrInput ReadForTest(int slot) { return Read(slot); }
        BrInput Read(int slot)
        {
            var i = new BrInput();
            bool net = Online != null && Net != null;   // online: this device's one person, with every key set and controller 1
            int seat = net ? 0 : seatOf[slot];
            int h = Ladder || net ? 0 : HumanIndex(seat);
            bool one = Humans == 1 || net;
            bool keysA = h == 0, keysB = h == 1 || one;
            double x = 0, y = 0;
            for (int s = 0; s < 2; s++)
            {
                if (!(s == 0 ? keysA : keysB)) continue;
                Key[] ks = Keys.Set[s];
                if (Down(ks[BrawlKeys.MoveLeft])) x -= 1; if (Down(ks[BrawlKeys.MoveRight])) x += 1;
                if (Down(ks[BrawlKeys.Jump])) y += 1; if (Down(ks[BrawlKeys.Down])) y -= 1;
                i.Attack |= Down(ks[BrawlKeys.Attack]); i.Strong |= Down(ks[BrawlKeys.Strong]); i.Special |= Down(ks[BrawlKeys.Special]);
                i.Shield |= Down(ks[BrawlKeys.Shield]); i.Grab |= Down(ks[BrawlKeys.Grab]);
            }
            // the extra keys, while no set uses them for something else
            if (keysA) i.Jump |= ExtraDown(Key.Space);
            if (keysB)
            {
                if (ExtraDown(Key.NumPad4)) x -= 1; if (ExtraDown(Key.NumPad6)) x += 1; if (ExtraDown(Key.NumPad8)) y += 1;
                i.Attack |= ExtraDown(Key.NumPad1); i.Strong |= ExtraDown(Key.NumPad2); i.Special |= ExtraDown(Key.NumPad3);
                i.Shield |= ExtraDown(Key.NumPad0); i.Grab |= ExtraDown(Key.NumPad5);
                if (one) { i.Attack |= ExtraDown(Key.Z); i.Special |= ExtraDown(Key.X); i.Shield |= ExtraDown(Key.C); i.Strong |= ExtraDown(Key.V); }
            }
            KartPad p = KartPads.Read(Math.Min(3, h));
            if (p.On)
            {
                double px = p.Left ? -1 : p.Right ? 1 : p.X, py = p.Up ? 1 : p.Down ? -1 : p.Y;
                if (Math.Abs(px) > Math.Abs(x)) x = px; if (Math.Abs(py) > Math.Abs(y)) y = py;
                i.Jump |= p.XB || p.YB; i.Attack |= p.A; i.Special |= p.B; i.Shield |= p.RB || p.RT > 0.3; i.Grab |= p.LB; i.Strong |= p.LT > 0.3;
                if (Math.Abs(p.RX) > 0.6 || Math.Abs(p.RY) > 0.6) { i.Strong = true; x = Math.Abs(p.RX) > Math.Abs(p.RY) ? Math.Sign(p.RX) : 0; y = Math.Abs(p.RY) >= Math.Abs(p.RX) ? Math.Sign(p.RY) : 0; }
            }
            i.X = Math.Max(-1, Math.Min(1, x)); i.Y = Math.Max(-1, Math.Min(1, y));
            return i;
        }

        bool PadStart() { for (int k = 0; k < 4; k++) { KartPad p = KartPads.Read(k); if (p.On && p.Start) return true; } return false; }
        bool padStartWas;

        // ---------------------------------------------------------------- the loop
        protected override void TickSetup(double dt)
        {
            OnlineTickSetup(dt);   // online
            MusicCatchUp();
            MenuMusic(1.5);
            // controllers move round the menus too
            KartPad p = KartPads.Read(0);
            if (p.On)
            {
                Key k = Key.None;
                if (p.Up || p.Y > 0.6) k = Key.Up; else if (p.Down || p.Y < -0.6) k = Key.Down; else if (p.Left || p.X < -0.6) k = Key.Left; else if (p.Right || p.X > 0.6) k = Key.Right;
                else if (p.A || p.Start) k = Key.Enter; else if (p.B) k = Key.Escape;
                if (k != padKey && k != Key.None) TapKey(k);
                padKey = k;
            }
        }
        Key padKey = Key.None;

        protected override void Tick(double dt)
        {
            if (Sim == null) return;
            if (Step != Stage.Results) MusicCatchUp();
            stageT += dt;
            bool startNow = PadStart(); if (startNow && !padStartWas && Step == Stage.Fight) Pause(); padStartWas = startNow;
            switch (Step)
            {
                case Stage.LadderCard:
                    View.Follow(Sim, false); View.StepFx(Sim, dt);
                    if (stageT > 3 || Autopilot && stageT > 1) { Step = Stage.Countdown; stageT = 0; }
                    ClearTaps();
                    return;
                case Stage.Countdown:
                    View.Follow(Sim, false); View.StepFx(Sim, dt);
                    if (stageT >= 2.2 || Autopilot && stageT > 0.6) { Step = Stage.Fight; stageT = 0; acc = 0; Audio.Sfx("brawl-go", 0.8); }
                    ClearTaps();
                    return;
                case Stage.Results:
                    View.StepFx(Sim, dt);
                    // after the fanfare, a calm loop under the results
                    if (musicRole == null && stageT >= resultsMusicAt) PlayMusic("results", Music.BrawlFill(1), 2.0);
                    ClearTaps();
                    return;
            }
            acc += dt;
            int steps = 0;
            var inputs = new BrInput[Sim.F.Length];
            while (acc >= 1.0 / 60 && steps < 4)
            {
                acc -= 1.0 / 60; steps++;
                if (Online != null && Online.Match != null && Net != null) { if (!OnlineStep()) { acc = 0; break; } }   // online
                else
                {
                    for (int s = 0; s < Sim.F.Length; s++) inputs[s] = bots[s] != null ? bots[s].Think(Sim) : (Autopilot ? new BrInput() : Read(s));
                    Sim.Step(inputs);
                }
                WorldSteps++;
                ClearTaps();
                Sounds();
                View.Take(Sim);
                if (Sim.Over) break;
            }
            if (steps == 4) acc = 0;
            View.Follow(Sim, false);
            View.StepFx(Sim, dt);
            if (Sim.Over && !resultsDone) MatchOver();
        }

        void Sounds()
        {
            foreach (BrEvent e in Sim.Events)
            {
                if (e.K == "sfx") Audio.Sfx(e.Name, e.A, 1.0);
                if (e.K == "game") Audio.Stinger(Tunes.LevelClear);
            }
        }

        /// <summary>The fanfare when a person (or the computer playing alone) wins, a short kind "so close" when the computer
        /// beats the people; the results loop starts when it ends.</summary>
        Song ResultsJingle()
        {
            bool people = false, personWon = false;
            for (int i = 0; i < Sim.F.Length; i++) if (bots[i] == null && !Autopilot) { people = true; if (Sim.Winner == i) personWon = true; }
            Song j = people && !personWon && Sim.Winner >= 0 ? (BrawlMusic.Get("defeat") ?? Music.BrawlFill(2)) : (BrawlMusic.Get("victory") ?? Tunes.HighScore);
            resultsMusicAt = j.Seconds + 0.5;
            return j;
        }

        void MatchOver()
        {
            resultsDone = true;
            if (!Ladder) { Step = Stage.Results; stageT = 0; musicRole = null; Audio.Jingle(ResultsJingle()); return; }
            BFighter me = Sim.F[0];
            if (Sim.Winner == 0)
            {
                // a round won: points for winning, for being quick and for lives kept
                double secs = Sim.Frame / 60.0;
                long pts = (1000 + (long)Math.Max(0, 180 - secs) * 10 + me.Stocks * 400) * (LadderDiff + 1);
                LadderScore += pts;
                LadderRound++;
                if (LadderRound >= LadderRounds)
                {
                    EndGame(LadderScore, "climbed the ladder with " + me.D.Name + " on " + LevelNames[LadderDiff], "ladder-" + LadderDiff, "The ladder is climbed!", Tunes.Extra2);
                    return;
                }
                Toast("Round won: +" + Gfx.N(pts));
                NewGame();
                return;
            }
            EndGame(LadderScore, "reached round " + (LadderRound + 1) + " of the ladder with " + me.D.Name, "ladder-" + LadderDiff, "Bonked off the ladder", Tunes.GameOver);
        }

        protected override void OnKey(Key k)
        {
            if (Sim == null) return;
            if (Step == Stage.Results && stageT > 0.8 && Online != null && Net != null)   // online: the host takes everyone back
            {
                if ((k == Key.Enter || k == Key.Space) && Online.IsHost) { Audio.Ui("select"); Online.ToLobby(); }
                return;
            }
            if (Step == Stage.Results && stageT > 0.8)
            {
                if (k == Key.Enter || k == Key.Space) { Audio.Ui("select"); BackToSelect(); }
                else if (k == Key.R) { Audio.Ui("select"); NewGame(); }
            }
            if (Step == Stage.LadderCard && (k == Key.Enter || k == Key.Space) && stageT > 0.3) { Step = Stage.Countdown; stageT = 0; }
        }

        void BackToSelect()
        {
            State = Phase.Setup; Menu = Screen.Select; Sel = SelectRows - 1; SetupMusic();
        }

        protected override void OverConfirm() { if (Ladder) { State = Phase.Setup; Menu = Screen.Ladder; Sel = 2; SetupMusic(); } else NewGame(); }
        protected override string OverConfirmText { get { return "Enter: back to the ladder"; } }

        public override string CrashDetail
        {
            get
            {
                if (Sim == null) return "menu " + Menu;
                var parts = new List<string>();
                foreach (BFighter f in Sim.F) parts.Add(f.D.Id + " " + f.St + " " + f.Percent + "%");
                return "stage " + Sim.Stage.Id + ", frame " + Sim.Frame + ", " + string.Join(", ", parts);
            }
        }

        // ================================================================ drawing
        protected override void Draw(DrawingContext dc)
        {
            if (Sim == null) return;
            View.W = Width; View.H = Height;
            View.Draw(dc, Sim, T);
            View.DrawPanels(dc, Sim, Names, T);
            if (Sim.TimeMode && Step != Stage.Results)
            {
                int s = Math.Max(0, (Sim.TimeLeft + 59) / 60);
                string txt = (s / 60) + ":" + (s % 60).ToString("00");
                dc.DrawRoundedRectangle(Gfx.B(0xC0101522), null, new Rect(Width / 2 - 64, 12, 128, 44), 22, 22);
                Gfx.Text(dc, txt, Width / 2, 16, 28, s <= 10 ? 0xFFF87171 : 0xFFFFFFFF, 0.5, Gfx.UiBold);
            }
            if (Sim.Sudden) Gfx.Shadowed(dc, "Sudden death!", Width / 2, 14, 26, 0xFFFCA5A5, 0.5, Gfx.UiBold);
            if (Step == Stage.Countdown)
            {
                int n = 3 - (int)(stageT / 0.7);
                string txt = n >= 1 ? n.ToString(CultureInfo.InvariantCulture) : "GO!";
                double k = (stageT % 0.7) / 0.7;
                Gfx.Shadowed(dc, txt, Width / 2, Height * 0.32 - 40, 110 - k * 20, Gfx.Alpha(0xFFFDE047, 1 - k * 0.5), 0.5, Gfx.UiBold);
            }
            if (Step == Stage.Fight && stageT < 0.6) Gfx.Shadowed(dc, "GO!", Width / 2, Height * 0.32 - 40, 100, Gfx.Alpha(0xFFFDE047, 1 - stageT / 0.6), 0.5, Gfx.UiBold);
            if (Sim.Decided && Step == Stage.Fight) Gfx.Shadowed(dc, "GAME!", Width / 2, Height * 0.3 - 50, 120, 0xFFFFFFFF, 0.5, Gfx.UiBold);
            if (Step == Stage.Results) DrawResults(dc);
            if (Step == Stage.LadderCard) DrawLadderCard(dc);
        }

        void DrawResults(DrawingContext dc)
        {
            dc.DrawRectangle(Gfx.B(0xC0050812), null, new Rect(0, 0, Width, Height));
            BFighter w = Sim.Winner >= 0 ? Sim.F[Sim.Winner] : null;
            string title = w == null ? "No contest!" : Names[w.Slot] + " wins!";
            Gfx.Shadowed(dc, title, Width / 2, 40, 54, w != null ? BrawlView.SlotColors[w.Slot % 4] : 0xFFFFFFFF, 0.5, Gfx.UiBold);
            if (w != null) View.Portrait(dc, w.D, 260, 560, 300 * Math.Min(1.3, 90 / w.D.Height * 1.2), T, false, BS.Attack);
            double x = 470, y = 150;
            Gfx.Text(dc, "Place", x, y, 18, 0xFF93C5FD, 0, Gfx.UiBold);
            Gfx.Text(dc, "Fighter", x + 90, y, 18, 0xFF93C5FD, 0, Gfx.UiBold);
            Gfx.Text(dc, "Bonks", x + 420, y, 18, 0xFF93C5FD, 0.5, Gfx.UiBold);
            Gfx.Text(dc, "Fell", x + 520, y, 18, 0xFF93C5FD, 0.5, Gfx.UiBold);
            Gfx.Text(dc, "Damage dealt", x + 660, y, 18, 0xFF93C5FD, 0.5, Gfx.UiBold);
            y += 40;
            foreach (BFighter f in Sim.F.OrderBy(f => f.Place == 0 ? 9 : f.Place))
            {
                uint c = BrawlView.SlotColors[f.Slot % 4];
                dc.DrawRoundedRectangle(Gfx.B(Gfx.Alpha(c, 0.18)), Gfx.P(Gfx.Alpha(c, 0.7), 2), new Rect(x - 14, y - 8, 760, 62), 12, 12);
                Gfx.Text(dc, Ordinal(f.Place), x, y + 6, 30, 0xFFFFFFFF, 0, Gfx.UiBold);
                View.Portrait(dc, f.D, x + 116, y + 23, 46, T, true, BS.Idle);
                Gfx.Text(dc, Names[f.Slot], x + 150, y + 2, 20, 0xFFFFFFFF, 0, Gfx.UiBold);
                if (Names[f.Slot] != f.D.Name) Gfx.Text(dc, f.D.Name, x + 150, y + 26, 15, 0xFFB8BFCC, 0, Gfx.Ui);
                Gfx.Text(dc, f.KOs.ToString(CultureInfo.InvariantCulture), x + 420, y + 8, 26, 0xFFFFFFFF, 0.5, Gfx.UiBold);
                Gfx.Text(dc, (f.Falls + f.SDs).ToString(CultureInfo.InvariantCulture), x + 520, y + 8, 26, 0xFFFFFFFF, 0.5, Gfx.UiBold);
                Gfx.Text(dc, ((int)f.Done) + "%", x + 660, y + 8, 26, 0xFFFFFFFF, 0.5, Gfx.UiBold);
                y += 74;
            }
            if (stageT > 0.8 && Online != null && Net != null) { if (Online.IsHost) MenuButtons(dc, Width / 2 + 160, Height - 60, 18, new[] { "Enter: back to the lobby" }, new[] { Key.Enter }); else Gfx.Text(dc, "Waiting for the host...", Width / 2 + 160, Height - 66, 18, 0xFF93C5FD, 0.5, Gfx.UiBold); }   // online
            else if (stageT > 0.8) MenuButtons(dc, Width / 2 + 160, Height - 60, 18, new[] { "Enter: choose again", "R: rematch" }, new[] { Key.Enter, Key.R });
        }

        static string Ordinal(int n) { return n == 1 ? "1st" : n == 2 ? "2nd" : n == 3 ? "3rd" : n > 0 ? n + "th" : "-"; }

        void DrawLadderCard(DrawingContext dc)
        {
            dc.DrawRectangle(Gfx.B(0xB0050812), null, new Rect(0, 0, Width, Height));
            string round = LadderRound == LadderRounds - 1 ? "Last round: the giant!" : LadderRound == 2 ? "Round 3: two at once" : "Round " + (LadderRound + 1) + " of " + LadderRounds;
            Gfx.Shadowed(dc, round, Width / 2, 70, 46, 0xFFFDE047, 0.5, Gfx.UiBold);
            View.Portrait(dc, Sim.F[0].D, 330, 470, 260, T, false, BS.Idle);
            Gfx.Shadowed(dc, "vs", Width / 2, 300, 70, 0xFFFFFFFF, 0.5, Gfx.UiBold);
            for (int i = 1; i < Sim.F.Length; i++)
            {
                BFighter f = Sim.F[i];
                double x = Sim.F.Length == 2 ? 950 : 860 + (i - 1) * 200;
                View.Portrait(dc, f.D, x, 470, 260 * f.Size * (Sim.F.Length == 2 ? 1 : 0.8), T, false, BS.Idle);
                Gfx.Text(dc, f.D.Name, x, 490, 22, 0xFFFFFFFF, 0.5, Gfx.UiBold);
            }
            Gfx.Text(dc, Sim.F[0].D.Name, 330, 490, 22, 0xFFFFFFFF, 0.5, Gfx.UiBold);
            Gfx.Text(dc, "on " + Sim.Stage.Name + "  \u00B7  " + LevelNames[LadderDiff] + "  \u00B7  score " + Gfx.N(LadderScore), Width / 2, 560, 20, 0xFFB8BFCC, 0.5, Gfx.Ui);
        }

        protected override void DrawSetup(DrawingContext dc)
        {
            View.W = Width; View.H = Height; View.CamX = 0; View.CamY = 150; View.Zoom = 0.9;
            BStageDef bg = BrawlData.Stages[Menu == Screen.Select && StagePick >= 0 ? StagePick : 0];
            View.DrawBackground(dc, bg, T);
            dc.DrawRectangle(Gfx.B(0x90050812), null, new Rect(0, 0, Width, Height));
            switch (Menu)
            {
                case Screen.Title: DrawTitle(dc); break;
                case Screen.Select: DrawSelect(dc); break;
                case Screen.Help: DrawHelp(dc); break;
                case Screen.Keys: DrawKeys(dc); break;
                case Screen.Lineup: DrawLineup(dc); break;
                case Screen.Swings: DrawSwings(dc); break;
                case Screen.Moves: DrawMoves(dc); break;
                case Screen.Ladder: DrawLadder(dc); break;
                case Screen.Online: DrawOnline(dc); break;      // online
                case Screen.OnlineJoin: DrawJoin(dc); break;
                case Screen.OnlineLobby: DrawLobby(dc); break;
            }
        }

        void Logo(DrawingContext dc, double y, double size)
        {
            // the town's name on top, a big SMASH! under it
            Gfx.Text(dc, "SAHUARITA", Width / 2 + 4, y + 4, size * 0.62, 0xFF7C2D12, 0.5, Gfx.UiBold);
            Gfx.Text(dc, "SAHUARITA", Width / 2, y, size * 0.62, 0xFFFB923C, 0.5, Gfx.UiBold);
            Gfx.Text(dc, "SMASH!", Width / 2 + 5, y + size * 0.62 + 5, size * 1.2, 0xFF4C1D95, 0.5, Gfx.UiBold);
            Gfx.Text(dc, "SMASH!", Width / 2, y + size * 0.62, size * 1.2, 0xFFFDE047, 0.5, Gfx.UiBold);
        }

        void DrawTitle(DrawingContext dc)
        {
            // the cast in a row along the bottom
            int n = BrawlData.Fighters.Count;
            for (int i = 0; i < n; i++)
            {
                BFighterDef d = BrawlData.Fighters[i];
                double x = 80 + i * (Width - 160) / (n - 1);
                View.Portrait(dc, d, x, Height - 40, d.Height * 1.5, T + i * 0.37, false, BS.Idle);
            }
            Logo(dc, 40, 84);
            Gfx.Text(dc, "Knock your friends off the stage. Nobody gets hurt: they just fly off with a twinkle.", Width / 2, 236, 18, 0xFFE5E7EB, 0.5, Gfx.Ui);
            DrawMenu(dc, Width / 2, 280, new[] { "Fight (2 to 4 players)", "The ladder (on your own)", "How to play", "Controls (change the keys)", "Play online (with others)", "Quit" }, Sel, i => Sel = i);
        }

        void DrawHelp(DrawingContext dc)
        {
            Gfx.Panel(dc, new Rect(60, 40, Width - 120, Height - 80), 0xF0101522, 0xFFFB923C);
            Gfx.Text(dc, "How to play", Width / 2, 56, 34, 0xFFFDE047, 0.5, Gfx.UiBold);
            double y = 116;
            string[] lines = {
                "Hit the others to raise their damage %. The higher it is, the farther they fly when hit.",
                "Knock someone past the edges of the screen and they lose a life. Last one standing wins (or the most bonks when time runs out).",
                "Off the stage? Jump again in the air, then use your up special to get back. Grab the edge to hang on.",
                "Strong attacks hit hardest: hold the button to charge them. Specials are each fighter's own tricks.",
                "Shield blocks hits but shrinks; if it breaks you're dizzy. Shield + left / right rolls, shield + down dodges. Grabs go through shields." };
            foreach (string l in lines) { Gfx.Text(dc, "\u2022 " + l, 100, y, 17, 0xFFE5E7EB, 0, Gfx.Ui); y += 30; }
            y += 14;
            double cx = 110;
            string[,] keys = KeyTable("Left keys", "Right keys", false);
            for (int r = 0; r < keys.GetLength(0); r++)
            {
                for (int c = 0; c < 4; c++)
                    Gfx.Text(dc, keys[r, c], cx + new[] { 0, 190, 420, 640 }[c], y, 17, r == 0 || c == 0 ? 0xFF93C5FD : 0xFFFFFFFF, 0, r == 0 || c == 0 ? Gfx.UiBold : Gfx.Ui);
                y += 27;
            }
            Gfx.Text(dc, "Alone at the keyboard? Either set works, and Z X C V are attack, special, shield and strong.  Esc or P pauses.  Change the keys under Controls.", Width / 2, y + 12, 16, 0xFFB8BFCC, 0.5, Gfx.Ui);
            MenuButtons(dc, Width / 2, Height - 92, 18, new[] { "Enter: back" }, new[] { Key.Enter });
        }

        // the pause panel: the menu, and beside it the buttons and each player's specials (Andrew, 2026-10-29: "key
        // mappings here like on specimen zero")
        protected override void DrawPause(DrawingContext dc)
        {
            if (Sim == null || Net != null) { base.DrawPause(dc); return; }
            double w = 1180, h = 620, x = (Width - w) / 2, y = (Height - h) / 2, menuW = 360;
            dc.DrawRectangle(Gfx.B(0xA0000000), null, new Rect(0, 0, Width, Height));
            Gfx.Panel(dc, new Rect(x, y, w, h), 0xF0101522, 0xFFFB923C);
            Gfx.Text(dc, "Paused", x + 40 + menuW / 2, y + 20, 34, 0xFFFDE047, 0.5, Gfx.UiBold);
            MenuRows(dc, x + 40, y + 84, 18, menuW, new[] {
                "P or Esc", "keep playing",
                "R", "start again",
                "Q", "back to the menu",
                "M", "music " + (Audio.MusicOn ? "off" : "on"),
                "N", "sounds " + (Audio.SoundsOn ? "off" : "on") }, new[] { Key.Escape, Key.R, Key.Q, Key.M, Key.N });
            DrawPauseControls(dc, x + 440, y + 26, x + w - 30, y + h - 20);
        }

        /// <summary>The buttons as a table (rows: header + 8 actions; columns: action, left keys, right keys, controller),
        /// with the person's own keys and the extras that still work.</summary>
        string[,] KeyTable(string leftHead, string rightHead, bool alone)
        {
            Func<int, int, string> k = (s, a) => BrawlKeys.Name(Keys.Set[s][a]);
            Func<string, Key, string> or = (t, extra) => Keys.Extra(extra) ? t + " (or " + BrawlKeys.Name(extra) + ")" : t;
            Func<string, Key, string> also = (t, extra) => alone && Keys.Extra(extra) ? t + ", " + BrawlKeys.Name(extra) : t;
            int A = BrawlKeys.Attack, St = BrawlKeys.Strong, Sp = BrawlKeys.Special, Sh = BrawlKeys.Shield, G = BrawlKeys.Grab;
            return new string[,] {
                { "", leftHead, rightHead, "Controller" },
                { "Move", k(0, 0) + "  " + k(0, 1), k(1, 0) + "  " + k(1, 1), "stick" },
                { "Jump", Keys.Extra(Key.Space) ? k(0, 2) + " or Space" : k(0, 2), k(1, 2), "X or Y" },
                { "Down / drop", k(0, 3), k(1, 3), "stick down" },
                { "Attack", k(0, A), also(or(k(1, A), Key.NumPad1), Key.Z), "A" },
                { "Strong", k(0, St), also(or(k(1, St), Key.NumPad2), Key.V), "right stick or left trigger" },
                { "Special", k(0, Sp), also(or(k(1, Sp), Key.NumPad3), Key.X), "B" },
                { "Shield", k(0, Sh), also(or(k(1, Sh), Key.NumPad0), Key.C), "RB or right trigger" },
                { "Grab", k(0, G), or(k(1, G), Key.NumPad5), "LB" } };
        }

        const uint PauseHead =0xFFFDE047, PauseKey = 0xFF93C5FD, PauseText = 0xFFE5E7EB, PauseDim = 0xFF8C93A5;
        void DrawPauseControls(DrawingContext dc, double x, double y, double right, double bottom)
        {
            dc.DrawRectangle(Gfx.B(0x60FB923C), null, new Rect(x - 28, y + 6, 1.5, bottom - y - 12));
            string L = ((char)0x2190).ToString(), R = ((char)0x2192).ToString(), U = ((char)0x2191).ToString(), D = ((char)0x2193).ToString();
            // who plays with which keys (the same rule as Read): the first person the left keys, the second the right
            // keys, a person alone either set; everyone may use a controller
            var people = new List<int>();
            for (int i = 0; i < Sim.F.Length && i < 4; i++) if (bots[i] == null) people.Add(i);
            bool one = people.Count <= 1;
            string leftWho = "Left keys", rightWho = "Right keys";
            foreach (int i in people)
            {
                int hi = Ladder ? 0 : HumanIndex(seatOf[i]);
                if (!one && hi == 0) leftWho = "Left keys: " + Names[i];
                if (!one && hi == 1) rightWho = "Right keys: " + Names[i];
            }
            Gfx.Text(dc, "Controls", x, y, 20, PauseHead, 0, Gfx.UiBold);
            if (one) Gfx.Text(dc, "Either set of keys works.", x + 110, y + 4, 14, PauseDim, 0, Gfx.Ui);
            y += 34;
            double[] col = { 0, 130, 300, 470 };
            string[,] keys = KeyTable(leftWho, rightWho, one);
            for (int r = 0; r < keys.GetLength(0); r++)
            {
                for (int c = 0; c < 4; c++)
                {
                    string s = keys[r, c];
                    if (r == 0 && s.Length > 22) s = s.Substring(0, 21) + "...";
                    Gfx.Text(dc, s, x + col[c], y, 14, r == 0 || c == 0 ? PauseKey : PauseText, 0, r == 0 || c == 0 ? Gfx.UiBold : Gfx.Ui);
                }
                y += 24;
            }
            Gfx.Text(dc, "Hold Strong to charge it.  Shield + " + L + " / " + R + " rolls, Shield + " + D + " dodges.  Start on a controller pauses.", x, y + 4, 14, PauseDim, 0, Gfx.Ui);
            y += 40;

            // each person's specials: the Special button alone or with a direction
            double hw = Gfx.Text(dc, people.Count > 1 ? "Specials" : "Your specials", x, y, 20, PauseHead, 0, Gfx.UiBold);
            if (people.Count == 1) Gfx.Text(dc, U + " + Special gets you back to the stage.", x, y + 34 + 26 + 4 * 22 + 12, 14, PauseDim, 0, Gfx.Ui);
            else Gfx.Text(dc, U + " + Special gets you back to the stage.", x + hw + 16, y + 4, 14, PauseDim, 0, Gfx.Ui);
            y += 34;
            string[] how = { "Special", L + " / " + R + " + Special", U + " + Special", D + " + Special" };
            double colW = (right - x) / 2, top = y;
            for (int p = 0; p < people.Count; p++)
            {
                int i = people[p];
                double cx = x + (p % 2) * colW, cy = top + (p / 2) * 130;
                if (cy + 120 > bottom) break;
                BFighterDef d = Sim.F[i].D;
                Gfx.Text(dc, people.Count > 1 ? Names[i] + " as " + d.Name : d.Name, cx, cy, 15, PauseText, 0, Gfx.UiBold);
                for (int k = 0; k < 4; k++)
                {
                    Gfx.Text(dc, how[k], cx, cy + 26 + k * 22, 14, PauseKey, 0, Gfx.UiBold);
                    string name = d.SpecialNames[k] ?? "";
                    Gfx.Text(dc, name, cx + 130, cy + 26 + k * 22, 14, PauseText, 0, Gfx.Ui);
                }
            }
            if (people.Count == 0) Gfx.Text(dc, "The computer is playing everyone.", x, y, 14, PauseDim, 0, Gfx.Ui);
            // one person: their fighter's own attacks beside the specials (BrawlSignatures.cs)
            if (people.Count == 1)
            {
                BFighterDef d = Sim.F[people[0]].D;
                double ax = x + colW, ay = top - 34;
                Gfx.Text(dc, "Your attacks", ax, ay, 20, PauseHead, 0, Gfx.UiBold);
                ay += 34;
                string[] ahow = { "Attack", L + " " + R + " Attack", U + " Attack", D + " Attack", "Run + Attack", L + " " + R + " Strong", U + " Strong", D + " Strong",
                    "Air Attack", "Air fwd Attack", "Air back Attack", "Air " + U + " Attack", "Air " + D + " Attack" };
                for (int k = 0; k < BrawlSignatures.AttackIds.Length && ay + 17 < bottom; k++)
                {
                    BMove m = d.M(BrawlSignatures.AttackIds[k]);
                    Gfx.Text(dc, ahow[k], ax, ay, 13, PauseKey, 0, Gfx.UiBold);
                    Gfx.Text(dc, m != null && m.Name != "" ? m.Name : "", ax + 122, ay, 13, PauseText, 0, Gfx.Ui);
                    ay += 17.5;
                }
            }
        }

        int movesOf;
        /// <summary>Snapshots only (-Show Moves -Level 1-10): one fighter caught mid-way through each of its 13 attacks.</summary>
        void DrawMoves(DrawingContext dc)
        {
            dc.DrawRectangle(Gfx.B(0xF0101522), null, new Rect(0, 0, Width, Height));
            BFighterDef d = BrawlData.Fighters[movesOf];
            Gfx.Text(dc, d.Name + "'s attacks", Width / 2, 8, 24, 0xFFFDE047, 0.5, Gfx.UiBold);
            for (int i = 0; i < BrawlSignatures.AttackIds.Length; i++)
            {
                BMove m = d.M(BrawlSignatures.AttackIds[i]);
                if (m == null) continue;
                double cx = 128 + (i % 5) * 256, base_ = 50 + (i / 5 + 1) * 205;
                bool air = m.Id.EndsWith("air");
                var f = new BFighter { D = d, St = BS.Attack, Mv = m, MF = (m.FirstActive + m.LastActive) / 2 + 1, Face = 1, Slot = 0, Ground = !air };
                View.DrawPuppet(dc, f, cx - 20, base_ - (air ? 20 : 0), 150 / d.Height, T, 0);
                Gfx.Text(dc, m.Name, cx, base_ + 4, 15, 0xFFE5E7EB, 0.5, Gfx.UiBold);
                Gfx.Text(dc, BrawlSignatures.HowTo(m.Id), cx, base_ + 22, 12, 0xFF93C5FD, 0.5, Gfx.Ui);
            }
        }

        /// <summary>Snapshots only (-Show Swings -Level 1-13): every fighter doing the same attack, its wind-up faint
        /// behind and its strike in front, to compare how each one moves (BrawlMotion.cs).</summary>
        void DrawSwings(DrawingContext dc)
        {
            dc.DrawRectangle(Gfx.B(0xF0101522), null, new Rect(0, 0, Width, Height));
            string id = BrawlSignatures.AttackIds[movesOf];
            Gfx.Text(dc, "Everyone's " + id + " (" + BrawlSignatures.HowTo(id) + ")", Width / 2, 8, 24, 0xFFFDE047, 0.5, Gfx.UiBold);
            bool air = id.EndsWith("air");
            for (int i = 0; i < BrawlData.Fighters.Count; i++)
            {
                BFighterDef d = BrawlData.Fighters[i];
                BMove m = d.M(id);
                if (m == null) continue;
                double cx = 128 + (i % 5) * 256, base_ = i < 5 ? 320 : 670;
                double sc = 140 / d.Height;
                var w = new BFighter { D = d, St = BS.Attack, Mv = m, MF = Math.Max(0, m.FirstActive - 1), Face = 1, Slot = 0, Ground = !air, AirMove = air };
                dc.PushOpacity(0.35);
                View.DrawPuppet(dc, w, cx - 40, base_ - (air ? 20 : 0), sc, T, 0);
                dc.Pop();
                var s = new BFighter { D = d, St = BS.Attack, Mv = m, MF = (m.FirstActive + m.LastActive) / 2 + 1, Face = 1, Slot = 0, Ground = !air, AirMove = air };
                View.DrawPuppet(dc, s, cx, base_ - (air ? 20 : 0), sc, T, 0);
                Gfx.Text(dc, d.Name, cx, base_ + 4, 15, 0xFFE5E7EB, 0.5, Gfx.UiBold);
                Gfx.Text(dc, m.Name, cx, base_ + 22, 12, 0xFF93C5FD, 0.5, Gfx.Ui);
            }
        }

        /// <summary>Snapshots only (-Show Lineup): every fighter large, five to a row, to compare their shapes.</summary>
        void DrawLineup(DrawingContext dc)
        {
            dc.DrawRectangle(Gfx.B(0xF0101522), null, new Rect(0, 0, Width, Height));
            int n = BrawlData.Fighters.Count;
            for (int i = 0; i < n; i++)
            {
                double cx = 128 + (i % 5) * 256, base_ = i < 5 ? 330 : 680;
                View.Portrait(dc, BrawlData.Fighters[i], cx - 10, base_, 270, T, false, BS.Idle);
                Gfx.Text(dc, BrawlData.Fighters[i].Name, cx, base_ + 6, 16, 0xFFE5E7EB, 0.5, Gfx.UiBold);
            }
        }

        void DrawSelect(DrawingContext dc)
        {
            Gfx.Text(dc, "Choose your fighters", 150, 18, 34, 0xFFFDE047, 0, Gfx.UiBold);
            // the cast: click one to give it to the player being chosen for
            int n = BrawlData.Fighters.Count, cols = 5;
            double gx = 640, gy = 70, cw = 120, ch = 168;
            int focusSeat = Sel < 8 ? Sel / 2 : -1;
            for (int i = 0; i < n; i++)
            {
                BFighterDef d = BrawlData.Fighters[i];
                double x = gx + (i % cols) * cw, y = gy + (i / cols) * ch;
                var r = new Rect(x, y, cw - 8, ch - 8);
                bool lit = Hovered(r);
                dc.DrawRoundedRectangle(Gfx.B(lit ? 0xE0233050 : 0xD0131A2A), Gfx.P(lit ? 0xFF60A5FA : 0xFF2C3A55, lit ? 2 : 1), r, 12, 12);
                View.Portrait(dc, d, x + (cw - 8) / 2, y + ch - 44, Math.Min(108, d.Height * 1.05), T + i * 0.3, false, BS.Idle);
                Gfx.Text(dc, d.Name, x + (cw - 8) / 2, y + ch - 38, 14, 0xFFFFFFFF, 0.5, Gfx.UiBold);
                // who has picked it
                int k = 0;
                for (int s = 0; s < 4; s++)
                    if (Who[s] != 0 && Pick[s] == i) { dc.DrawRoundedRectangle(Gfx.B(BrawlView.SlotColors[s]), null, new Rect(x + 6 + k * 27, y + 6, 25, 20), 6, 6); Gfx.Text(dc, "P" + (s + 1), x + 18 + k * 27, y + 7, 13, 0xFF000000, 0.5, Gfx.UiBold); k++; }
                int fi = i;
                AddHit(r, d.Name, false, null, () => { int seat = focusSeat >= 0 ? focusSeat : 0; Pick[seat] = fi; if (Who[seat] == 0) Who[seat] = 2; Audio.Ui("select"); });
            }
            // the rows: each seat's fighter and who plays it, then the stage and rules
            double rx = 40, ry = 78, rh = 30, rw = 560;
            for (int row = 0; row < SelectRows; row++)
            {
                string label, value; uint col = 0xFFFFFFFF;
                if (row < 8)
                {
                    int seat = row / 2;
                    if (row % 2 == 0) { label = "Player " + (seat + 1); value = Pick[seat] >= 0 ? BrawlData.Fighters[Pick[seat]].Name : "Any fighter"; col = Who[seat] == 0 ? 0xFF6B7280 : BrawlView.SlotColors[seat]; }
                    else { label = "    played by"; value = WhoText(seat); col = Who[seat] == 0 ? 0xFF6B7280 : 0xFFE5E7EB; }
                }
                else if (row == 8) { label = "Stage"; value = StagePick >= 0 ? BrawlData.Stages[StagePick].Name : "Any stage"; }
                else if (row == 9) { label = "Rules"; value = Minutes > 0 ? Minutes + " minutes, most bonks wins" : Lives + (Lives == 1 ? " life each" : " lives each"); }
                else if (row == 10) { label = "Stage surprises"; value = Surprises ? "On" : "Off"; }
                else { label = ""; value = ""; }
                double y = ry + row * rh + (row >= 8 ? 14 : 0) + (row == SelectRows - 1 ? 18 : 0);
                var rr = new Rect(rx - 10, y - 3, rw, rh - 2);
                bool sel = Sel == row;
                int rowI = row;
                if (row == SelectRows - 1)
                {
                    var br = new Rect(rx, y, 260, 44);
                    bool lit = sel || Hovered(br);
                    dc.DrawRoundedRectangle(Gfx.B(lit ? 0xFFEA580C : 0xC0131A2A), Gfx.P(lit ? 0xFFFDBA74 : 0xFF2C3A55, 2), br, 22, 22);
                    Gfx.Text(dc, "Fight!  \u25B6", br.X + br.Width / 2, br.Y + 9, 22, 0xFFFFFFFF, 0.5, Gfx.UiBold);
                    AddHit(br, "Fight", sel, () => Sel = rowI, TryStart);
                    continue;
                }
                if (sel) dc.DrawRoundedRectangle(Gfx.B(0xFF1E3A8A), Gfx.P(0xFF60A5FA, 1.5), rr, 8, 8);
                Gfx.Text(dc, label, rx, y, 17, row < 8 && row % 2 == 0 ? col : 0xFF93C5FD, 0, Gfx.UiBold);
                double vx = rx + 330;
                Gfx.Text(dc, "\u25C0", vx - 120, y, 17, 0xFF93C5FD, 0.5, Gfx.UiBold);
                Gfx.Text(dc, value, vx, y, 17, col, 0.5, Gfx.Ui);
                Gfx.Text(dc, "\u25B6", vx + 120, y, 17, 0xFF93C5FD, 0.5, Gfx.UiBold);
                AddHit(rr, label.Trim() + " " + value, sel, () => Sel = rowI, () => { Sel = rowI; TapKey(Key.Right); });
                AddArrowHits(rr, vx - 120, vx + 120, label.Trim(), row, sel, () => Sel = rowI);
            }
            // the chosen one's details, and the stage
            int show = focusSeat >= 0 && Pick[focusSeat] >= 0 ? Pick[focusSeat] : -1;
            double iy = gy + 2 * ch + 8;
            dc.DrawRoundedRectangle(Gfx.B(0xD0131A2A), Gfx.P(0xFF2C3A55, 1), new Rect(gx, iy, cols * cw - 8, 220), 12, 12);
            if (show >= 0)
            {
                BFighterDef d = BrawlData.Fighters[show];
                Gfx.Text(dc, d.Name, gx + 16, iy + 10, 22, 0xFFFDE047, 0, Gfx.UiBold);
                Gfx.Text(dc, d.Group + "  \u00B7  " + d.From, gx + 16, iy + 40, 15, 0xFF93C5FD, 0, Gfx.Ui);
                Wrap(dc, d.Blurb, gx + 16, iy + 64, cols * cw - 40, 15, 0xFFE5E7EB);
                Gfx.Text(dc, "Specials: " + string.Join(", ", d.SpecialNames), gx + 16, iy + 128, 14, 0xFFB8BFCC, 0, Gfx.Ui);
                string stats = "Weight " + Stars(d.Weight, 75, 135) + "    Speed " + Stars(d.Run, 5.5, 11) + "    Jumps " + (d.Jumps + 0);
                Gfx.Text(dc, stats, gx + 16, iy + 152, 14, 0xFFB8BFCC, 0, Gfx.Ui);
            }
            else
            {
                BStageDef s = StagePick >= 0 ? BrawlData.Stages[StagePick] : null;
                Gfx.Text(dc, s != null ? s.Name : "Any stage", gx + 16, iy + 10, 22, 0xFFFDE047, 0, Gfx.UiBold);
                if (s != null) { Gfx.Text(dc, s.From, gx + 16, iy + 40, 15, 0xFF93C5FD, 0, Gfx.Ui); Wrap(dc, s.Blurb, gx + 16, iy + 64, cols * cw - 40, 15, 0xFFE5E7EB); }
                else Wrap(dc, "A stage picked at random when the fight starts.", gx + 16, iy + 64, cols * cw - 40, 15, 0xFFE5E7EB);
            }
            Gfx.Text(dc, "Up / Down choose a row, Left / Right change it, Enter starts. Click a fighter to give it to the player you're on.", 40, Height - 34, 15, 0xFF8B95A7, 0, Gfx.Ui);
        }

        string WhoText(int seat)
        {
            if (Who[seat] == 0) return "nobody (off)";
            if (Who[seat] == 2) return "the computer: " + LevelNames[Level[seat]];
            int h = HumanIndex(seat);
            string ctl = h == 0 ? (Humans == 1 ? "any keys" : "the left keys") : h == 1 ? "the right keys" : "controller " + (h + 1);
            if (h <= 1 && KartPads.Read(h).On) ctl += " or controller " + (h + 1);
            return "a person (" + ctl + ")";
        }

        static string Stars(double v, double lo, double hi)
        {
            int n = (int)Math.Round(1 + 4 * Math.Max(0, Math.Min(1, (v - lo) / (hi - lo))));
            return new string('\u25CF', n) + new string('\u25CB', 5 - n);
        }

        static void Wrap(DrawingContext dc, string text, double x, double y, double width, double size, uint color)
        {
            string line = "";
            foreach (string w in text.Split(' '))
            {
                string t = line.Length == 0 ? w : line + " " + w;
                if (Gfx.Width(t, size, Gfx.Ui) > width && line.Length > 0) { Gfx.Text(dc, line, x, y, size, color, 0, Gfx.Ui); y += size * 1.4; line = w; }
                else line = t;
            }
            if (line.Length > 0) Gfx.Text(dc, line, x, y, size, color, 0, Gfx.Ui);
        }

        void DrawLadder(DrawingContext dc)
        {
            Gfx.Text(dc, "The ladder", 150, 18, 34, 0xFFFDE047, 0, Gfx.UiBold);
            Gfx.Text(dc, "Six matches against the computer, each a little harder: two at once in round 3, and a giant at the top.", 40, 66, 17, 0xFFE5E7EB, 0, Gfx.Ui);
            BFighterDef d = BrawlData.Fighters[LadderPick];
            View.Portrait(dc, d, 900, 560, 300 * Math.Min(1.25, 90 / d.Height * 1.1), T, false, BS.Idle);
            Wrap(dc, d.Blurb, 700, 590, 520, 16, 0xFFE5E7EB);
            string[] labels = { "Fighter", "Difficulty", "" };
            string[] values = { d.Name, LevelNames[LadderDiff], "" };
            for (int row = 0; row < 3; row++)
            {
                double y = 140 + row * 46;
                int rowI = row;
                if (row == 2)
                {
                    var br = new Rect(40, y + 10, 260, 44);
                    bool lit = Sel == 2 || Hovered(br);
                    dc.DrawRoundedRectangle(Gfx.B(lit ? 0xFFEA580C : 0xC0131A2A), Gfx.P(lit ? 0xFFFDBA74 : 0xFF2C3A55, 2), br, 22, 22);
                    Gfx.Text(dc, "Climb!  \u25B6", br.X + br.Width / 2, br.Y + 9, 22, 0xFFFFFFFF, 0.5, Gfx.UiBold);
                    AddHit(br, "Climb", Sel == 2, () => Sel = 2, () => { Audio.Ui("select"); StartLadder(); });
                    continue;
                }
                var rr = new Rect(30, y - 4, 560, 36);
                if (Sel == row) dc.DrawRoundedRectangle(Gfx.B(0xFF1E3A8A), Gfx.P(0xFF60A5FA, 1.5), rr, 8, 8);
                Gfx.Text(dc, labels[row], 40, y, 20, 0xFF93C5FD, 0, Gfx.UiBold);
                Gfx.Text(dc, "\u25C0", 240, y, 20, 0xFF93C5FD, 0.5, Gfx.UiBold);
                Gfx.Text(dc, values[row], 360, y, 20, 0xFFFFFFFF, 0.5, Gfx.Ui);
                Gfx.Text(dc, "\u25B6", 480, y, 20, 0xFF93C5FD, 0.5, Gfx.UiBold);
                AddHit(rr, labels[row] + " " + values[row], Sel == row, () => Sel = rowI, () => { Sel = rowI; TapKey(Key.Right); });
                AddArrowHits(rr, 240, 480, labels[row], row, Sel == row, () => Sel = rowI);
            }
            double by = 340;
            Gfx.Text(dc, "Best climbs", 40, by, 20, 0xFF93C5FD, 0, Gfx.UiBold);
            if (Scores != null)
            {
                var top = Scores.Top(Id, "ladder-" + LadderDiff, 5);
                int i = 0;
                foreach (ScoreEntry e in top) { Gfx.Text(dc, (i + 1) + ".  " + e.Player + "   " + Gfx.N(e.Score), 40, by + 32 + i * 26, 16, 0xFFE5E7EB, 0, Gfx.Ui); i++; }
                if (i == 0) Gfx.Text(dc, "None yet on " + LevelNames[LadderDiff] + ".", 40, by + 32, 16, 0xFF8B95A7, 0, Gfx.Ui);
            }
        }

        // ================================================================ an App Hub update
        /// <summary>Closing for an App Hub update: the match in progress is kept (who, which stage, the rules, everyone's
        /// lives and damage, the time left; the ladder's round and score) and starts again from there afterwards.</summary>
        public override IDictionary<string, string> SaveForUpdate()
        {
            if (Autopilot || State != Phase.Play || Sim == null || Step == Stage.Results || Sim.Decided) return null;
            var d = new Dictionary<string, string>();
            Func<int, string> s = v => v.ToString(CultureInfo.InvariantCulture);
            d["brawl.stage"] = s(StageIndex);
            d["brawl.ladder"] = Ladder ? "1" : "0";
            if (Ladder)
            {
                d["brawl.ladder.pick"] = s(LadderPick); d["brawl.ladder.diff"] = s(LadderDiff); d["brawl.ladder.round"] = s(LadderRound);
                d["brawl.ladder.score"] = LadderScore.ToString(CultureInfo.InvariantCulture);
                d["brawl.ladder.foes"] = string.Join(",", ladderFoes.Select(x => s(x)).ToArray());
            }
            d["brawl.lives"] = s(Lives); d["brawl.minutes"] = s(Minutes); d["brawl.surprises"] = Surprises ? "1" : "0";
            d["brawl.time"] = s(Sim.TimeLeft);
            var who = new List<string>();
            for (int i = 0; i < Sim.F.Length; i++)
            {
                BFighter f = Sim.F[i];
                int seat = seatOf[i];
                who.Add(s(seat) + ":" + f.D.Id + ":" + (bots[i] != null ? "cpu" + bots[i].Level : "person") + ":" + s(f.Stocks) + ":" + f.Dmg.ToString("0.#", CultureInfo.InvariantCulture) + ":" + s(f.KOs) + ":" + s(f.Falls + f.SDs));
            }
            d["brawl.fighters"] = string.Join(";", who.ToArray());
            return d;
        }

        public override bool ResumeAfterUpdate(IDictionary<string, string> saved)
        {
            if (saved == null) return false;
            try
            {
                string fs = UpdateResume.Get(saved, "brawl.fighters");
                if (string.IsNullOrEmpty(fs)) return false;
                Ladder = UpdateResume.Get(saved, "brawl.ladder") == "1";
                Lives = UpdateResume.Int(saved, "brawl.lives", 3); Minutes = UpdateResume.Int(saved, "brawl.minutes", 0);
                Surprises = UpdateResume.Get(saved, "brawl.surprises") != "0";
                var rows = fs.Split(';').Select(r => r.Split(':')).Where(r => r.Length >= 5).ToList();
                if (Ladder)
                {
                    LadderPick = UpdateResume.Int(saved, "brawl.ladder.pick", 0); LadderDiff = UpdateResume.Int(saved, "brawl.ladder.diff", 1);
                    LadderRound = UpdateResume.Int(saved, "brawl.ladder.round", 0); LadderScore = UpdateResume.Int(saved, "brawl.ladder.score", 0);
                    ladderFoes = (UpdateResume.Get(saved, "brawl.ladder.foes") ?? "").Split(',').Where(x => x.Length > 0).Select(x => int.Parse(x, CultureInfo.InvariantCulture)).ToArray();
                    if (ladderFoes.Length < LadderRounds + 1) return false;
                }
                else
                {
                    for (int s = 0; s < 4; s++) Who[s] = 0;
                    foreach (string[] r in rows)
                    {
                        int seat = int.Parse(r[0], CultureInfo.InvariantCulture);
                        if (seat < 0 || seat > 3) continue;
                        int fi = BrawlData.FighterIndex(r[1]);
                        if (fi < 0) return false;
                        Pick[seat] = fi;
                        if (r[2].StartsWith("cpu")) { Who[seat] = 2; int lv = int.Parse(r[2].Substring(3), CultureInfo.InvariantCulture); Level[seat] = Array.IndexOf(BotLevels, lv) >= 0 ? Array.IndexOf(BotLevels, lv) : 1; }
                        else Who[seat] = 1;
                    }
                    StagePick = UpdateResume.Int(saved, "brawl.stage", 0);
                }
                NewGame();
                if (Sim.F.Length != rows.Count) return false;
                for (int i = 0; i < rows.Count; i++)
                {
                    string[] r = rows[i];
                    BFighter f = Sim.F[i];
                    f.Stocks = Math.Max(1, int.Parse(r[3], CultureInfo.InvariantCulture));
                    f.Dmg = double.Parse(r[4], CultureInfo.InvariantCulture);
                    if (r.Length >= 7) { f.KOs = int.Parse(r[5], CultureInfo.InvariantCulture); f.Falls = int.Parse(r[6], CultureInfo.InvariantCulture); }
                }
                if (Sim.TimeMode) Sim.TimeLeft = Math.Max(600, UpdateResume.Int(saved, "brawl.time", Sim.TimeLeft));
                Toast("Back where you were before the update");
                return State == Phase.Play;
            }
            catch (FormatException) { return false; }
        }

        // ================================================================ snapshots and tests
        public string SnapshotScene(string show, double demo, int level)
        {
            Rng = new Random(5);
            switch (show)
            {
                case "Title": case "Select": case "Help": case "Ladder": case "Keys": case "Lineup": case "Moves": case "Swings":
                    Begin();
                    Menu = show == "Title" ? Screen.Title : show == "Select" ? Screen.Select : show == "Help" ? Screen.Help : show == "Keys" ? Screen.Keys : show == "Lineup" ? Screen.Lineup : show == "Moves" ? Screen.Moves : show == "Swings" ? Screen.Swings : Screen.Ladder;
                    if (show == "Moves") movesOf = Math.Max(0, Math.Min(BrawlData.Fighters.Count - 1, level - 1));
                    if (show == "Swings") movesOf = Math.Max(0, Math.Min(BrawlSignatures.AttackIds.Length - 1, level - 1));
                    if (show == "Keys") { Keys.Assign(0, BrawlKeys.Attack, Key.J); KeyRow = BrawlKeys.Shield; KeyCol = 1; KeyListening = true; }
                    if (show == "Select") { Who[2] = 2; Who[3] = 1; Pick[2] = 7; Pick[3] = 9; Sel = 4; StagePick = level >= 1 && level <= BrawlData.Stages.Count ? level - 1 : 0; }
                    for (int i = 0; i < 30; i++) Update(1.0 / 60);
                    return "menu " + Menu;
            }
            Autopilot = true;
            StagePick = level >= 1 && level <= BrawlData.Stages.Count ? level - 1 : 0;
            for (int s = 0; s < 4; s++) { Who[s] = show == "Fight2" ? (s < 2 ? 2 : 0) : 2; }
            DemoAll = show != "Fight2";
            if (show == "Cast") { for (int s = 0; s < 4; s++) Pick[s] = (s * 3 + level) % BrawlData.Fighters.Count; }
            NewGame();
            switch (show)
            {
                case "Countdown": Simulate(0.3); Autopilot = false; Step = Stage.Countdown; stageT = 0.2; break;
                case "Pause": case "Pause2":
                    // people at the keyboard (Pause: one, Pause2: two and the computer), stopped a few seconds in
                    Autopilot = false; DemoAll = false;
                    for (int s = 0; s < 4; s++) Who[s] = show == "Pause" ? (s == 0 ? 1 : s == 1 ? 2 : 0) : (s < 2 ? 1 : s == 2 ? 2 : 0);
                    NewGame(); Step = Stage.Fight;
                    for (int i = 0; i < 120; i++) Update(1.0 / 60);
                    Pause();
                    break;
                case "Results":
                    Simulate(demo > 0 ? demo : 200);
                    for (int i = 0; i < 60 * 300 && Step != Stage.Results; i++) Update(1.0 / 60);
                    for (int i = 0; i < 60; i++) Update(1.0 / 60);
                    break;
                case "Ladder1":
                    Autopilot = false; Ladder = true; LadderPick = 0; StartLadder(); Autopilot = true; Step = Stage.LadderCard; stageT = 0.4; Update(1.0 / 60); break;
                case "Giant":
                    Autopilot = false; Ladder = true; LadderPick = 6; StartLadder(); LadderRound = LadderRounds - 1; NewGame(); Autopilot = true; Simulate(demo > 0 ? demo : 6); break;
                case "Stampede":
                    // the mesa's javelinas: -Level 1 running through the middle, 2 the warning (dust and snorting)
                    StagePick = 0; NewGame(); Simulate(demo > 0 ? demo : 3);
                    for (int i = 0; i < 60 * 600; i++)
                    {
                        if (Sim.Hazard == "javelina" && (level == 2 ? Sim.HazPhase == 1 && Sim.HazT < 40 : Sim.HazPhase == 2 && (Sim.HazX - Sim.HazDir * 100) * Sim.HazDir > -40)) break;
                        Update(1.0 / 60);
                    }
                    break;
                default: Simulate(demo > 0 ? demo : 8); break;
            }
            return "stage " + Sim.Stage.Name + ", " + Step + ", frame " + Sim.Frame + ", " + string.Join(", ", Sim.F.Select(f => f.D.Name + " " + f.Percent + "% x" + f.Stocks).ToArray());
        }
    }
}
