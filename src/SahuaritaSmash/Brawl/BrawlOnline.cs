// Sahuarita Smash! (Sauce Edition): playing with others on other devices (Andrew, 2026-11-15: "network play, like Race
// Track Mayhem"). The title's "Play online" opens: host on this home network (the others find it, or type its address),
// host over the internet (a 4-letter code through Race Track Mayhem's relay, Engine\Net\RelayTransport.cs), or join.
// The lobby: everyone picks a fighter; the host picks the stage, lives, computer players and starts. A match is the
// game's own (BrawlSim on every device, kept in step by Engine\Net\NetMatch.cs); its results go back to the lobby.
// Everything is touch-friendly (letter pad for codes, arrows to tap) and works with keys and controllers.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Avalonia;
using Avalonia.Input;
using Avalonia.Media;

namespace ClassicGames
{
    public sealed partial class BrawlGame
    {
        /// <summary>The room being played in (null when not online).</summary>
        public NetSession Online;
        /// <summary>The relay to use (null = Race Track Mayhem's); the checks point it at one on this computer.</summary>
        public static string RelayUrl;
        /// <summary>The checks: host on this computer only.</summary>
        public static bool LanLoopbackOnly;
        string onlineNote = "";
        string joinText = "";
        List<LanTransport.Found> lanFound = new List<LanTransport.Found>();
        volatile bool looking;
        double lookAt;
        static readonly string PadKeys = "ABCDEFGHJKLMNPQRSTUVWXYZ0123456789.";

        /// <summary>The name others see: the player's name, or on Windows the account's first name.</summary>
        string OnlineName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(Player) && Player != "Player") return Player;
                try { string u = Environment.UserName; if (!string.IsNullOrWhiteSpace(u) && OperatingSystem.IsWindows()) return char.ToUpper(u[0]) + u.Substring(1, Math.Min(u.Length, 12) - 1); } catch (Exception) { }
                return "Player";
            }
        }

        bool OnlineScreen { get { return Menu == Screen.Online || Menu == Screen.OnlineJoin || Menu == Screen.OnlineLobby; } }

        // ---------------------------------------------------------------- starting and leaving
        void Go(NetSession s)
        {
            LeaveOnline(null);
            Online = s;
            s.MatchStarted += OnlineMatchStarted;
            s.BackToLobby += OnlineBackToLobby;
            Menu = Screen.OnlineLobby; Sel = 0; onlineNote = "";
        }

        void HostHome() { Go(new NetSession(LanTransport.Host(OnlineName + "'s game", LanLoopbackOnly), false, OnlineName, Pick[0] >= 0 ? Pick[0] : 0)); }
        void HostInternet() { Go(new NetSession(RelayTransport.Host(RelayUrl), true, OnlineName, Pick[0] >= 0 ? Pick[0] : 0)); }

        void JoinTyped()
        {
            string t = joinText.Trim();
            if (t.Length == 4 && t.All(char.IsLetter)) Go(new NetSession(RelayTransport.Join(RelayUrl, t), true, OnlineName, Pick[0] >= 0 ? Pick[0] : 0));
            else if (t.Length >= 7 && t.Count(c => c == '.') == 3) Go(new NetSession(LanTransport.Join(t), false, OnlineName, Pick[0] >= 0 ? Pick[0] : 0));
            else { onlineNote = "Type the 4-letter code from the host's screen (or a home-network address like 192.168.1.20)."; Audio.Ui("back"); }
        }

        /// <summary>Leaves the room (if any) and shows why on "Play online".</summary>
        void LeaveOnline(string why)
        {
            if (Online != null)
            {
                Online.MatchStarted -= OnlineMatchStarted; Online.BackToLobby -= OnlineBackToLobby;
                if (Online.State != NetSession.Phase.Closed) Online.Leave();
                Online = null;
            }
            if (Net != null) { Net = null; if (State == Phase.Play) { if (Paused) Resume(); State = Phase.Setup; Audio.StopEffects(); SetupMusic(); } }
            if (why != null) { onlineNote = why; Menu = Screen.Online; Sel = 0; }
        }

        void OnlineMatchStarted()
        {
            Net = new NetLink
            {
                Width = Width, Height = Height,
                Pump = OnlinePump,
                Leave = () => LeaveOnline("You left the game."),
                Status = OnlineStatus,
                BackHit = r => AddKeyHit(r, "Back", Key.Enter),
            };
            Ladder = false; Practice = false;
            NewGame();
        }

        void OnlineBackToLobby()
        {
            Net = null;
            if (State == Phase.Play) { State = Phase.Setup; Audio.StopEffects(); SetupMusic(); }
            Menu = Screen.OnlineLobby; Sel = 0;
            if (Online != null && Online.Reason != null) onlineNote = Online.Reason;
        }

        /// <summary>Every frame while online (menus and matches): the room's messages, and what happens if it closed.</summary>
        void OnlinePump()
        {
            if (Online == null) return;
            Online.Poll();
            if (Online.State != NetSession.Phase.Closed) return;
            string why = Online.Reason ?? "The online game ended.";
            if (State == Phase.Play && Net != null) { Net.Lost = true; Net.LostText = why; Online.MatchStarted -= OnlineMatchStarted; Online.BackToLobby -= OnlineBackToLobby; Online = null; Net.Leave = () => LeaveOnline(why); return; }
            LeaveOnline(why);
        }

        string OnlineStatus()
        {
            if (Online == null || Online.Match == null) return null;
            NetMatch m = Online.Match;
            int w = m.WaitingFor();
            if (w >= 0 && m.Stalls > 0 && Step == Stage.Fight && waitT > 0.4) return "Waiting for " + m.Setup.Seats[w].Name + "...";
            return null;
        }
        double waitT;

        /// <summary>The online match set up from the host's Start message (Reset calls this instead of its own).</summary>
        void ResetOnline()
        {
            NetMatch m = Online.Match;
            Sim = m.Sim;
            int n = Sim.F.Length;
            bots = new BrawlBot[n];
            seatOf = Enumerable.Range(0, n).ToArray();
            Names = m.Setup.Seats.Select(s => s.Name).ToArray();
            for (int i = 0; i < n; i++)
            {
                if (m.IsBot(i)) bots[i] = new BrawlBot(i, m.Setup.Seats[i].BotLevel);   // only says "a computer player" to the results; NetMatch plays them
                if (i < 4) View.Tags[i] = m.IsBot(i) ? "CPU" : m.IsMine(i) ? "YOU" : "P" + (i + 1);
            }
            StageIndex = m.Setup.Stage;
            Array.Clear(View.Colours, 0, View.Colours.Length);   // online: the usual colours (BrawlColours.cs)
            View.Fx.Clear(); View.Follow(Sim, true);
            Step = Stage.Countdown; stageT = 0; acc = 0; resultsDone = false; waitT = 0;
        }

        /// <summary>One online step: this device's people's buttons go out, the frame is played once everyone's are in.
        /// False while waiting for someone.</summary>
        bool OnlineStep()
        {
            NetMatch m = Online.Match;
            foreach (int seat in m.MySeats()) m.Local(seat, Read(seat));
            if (m.TryStep()) { waitT = 0; return true; }
            waitT += 1.0 / 60;
            return false;
        }

        // ---------------------------------------------------------------- keys
        bool OnlineTakesKey(Key k)
        {
            if (Menu != Screen.OnlineJoin) return false;
            return (k >= Key.A && k <= Key.Z) || (k >= Key.D0 && k <= Key.D9) || (k >= Key.NumPad0 && k <= Key.NumPad9) || k == Key.OemPeriod || k == Key.Decimal || k == Key.Back;
        }

        void OnlineKey(Key k)
        {
            switch (Menu)
            {
                case Screen.Online:
                    if (IsUp(k) || IsDown(k)) { Sel = MenuMove(k, Sel, 4); return; }
                    if (k != Key.Enter && k != Key.Space) return;
                    Audio.Ui("select");
                    if (Sel == 0) HostHome();
                    else if (Sel == 1) HostInternet();
                    else if (Sel == 2) { Menu = Screen.OnlineJoin; Sel = 0; joinText = ""; onlineNote = ""; lookAt = 0; }
                    else { Menu = Screen.Title; Sel = 5; }
                    return;
                case Screen.OnlineJoin:
                    {
                        char c = KeyChar(k);
                        if (c != '\0') { if (joinText.Length < 21) { joinText += c; Audio.Ui("move"); } return; }
                        if (k == Key.Back) { if (joinText.Length > 0) joinText = joinText.Substring(0, joinText.Length - 1); return; }
                        if (k == Key.Enter) { Audio.Ui("select"); JoinTyped(); }
                        return;
                    }
                case Screen.OnlineLobby:
                    LobbyKey(k);
                    return;
            }
        }

        static char KeyChar(Key k)
        {
            if (k >= Key.A && k <= Key.Z) { char c = (char)('A' + (k - Key.A)); return c == 'I' || c == 'O' ? '\0' : c; }
            if (k >= Key.D0 && k <= Key.D9) return (char)('0' + (k - Key.D0));
            if (k >= Key.NumPad0 && k <= Key.NumPad9) return (char)('0' + (k - Key.NumPad0));
            if (k == Key.OemPeriod || k == Key.Decimal) return '.';
            return '\0';
        }

        // lobby rows: 0 your fighter; host: 1 stage, 2 lives, 3 computer players, 4 their level, 5 surprises, 6 start
        int LobbyRows { get { return Online != null && Online.IsHost ? 7 : 1; } }

        void LobbyKey(Key k)
        {
            if (Online == null) return;
            if (IsUp(k) || IsDown(k)) { Sel = MenuMove(k, Sel, LobbyRows); return; }
            int d = IsLeft(k) ? -1 : IsRight(k) ? 1 : 0;
            if (d != 0) { LobbyChange(Sel, d); return; }
            if ((k == Key.Enter || k == Key.Space) && Online.IsHost)
            {
                if (Sel == 6 || Sel == 0) { if (Online.CanStart) { Audio.Ui("select"); Online.StartMatch((uint)Rng.Next(1, int.MaxValue)); } }
                else LobbyChange(Sel, 1);
            }
        }

        void LobbyChange(int row, int d)
        {
            NetSession s = Online;
            if (s == null) return;
            Audio.Ui("move");
            int nf = BrawlData.Fighters.Count, ns = BrawlData.Stages.Count;
            switch (row)
            {
                case 0: s.Pick((s.MyFighter + d + nf) % nf); Pick[0] = s.MyFighter; return;
                case 1: s.Stage = (s.Stage + d + ns) % ns; break;
                case 2: s.Lives = Math.Max(1, Math.Min(5, s.Lives + d)); break;
                case 3: s.Cpus = Math.Max(0, Math.Min(NetSession.MaxPlayers - s.Players.Count, s.Cpus + d)); break;
                case 4: s.CpuLevel = BotLevels[(Array.IndexOf(BotLevels, s.CpuLevel) + d + BotLevels.Length) % BotLevels.Length]; break;
                case 5: s.Surprises = !s.Surprises; break;
                default: return;
            }
            s.SendLobby();
        }

        bool OnlineBack()
        {
            if (Menu == Screen.Online) { Menu = Screen.Title; Sel = 5; return true; }
            if (Menu == Screen.OnlineJoin) { Menu = Screen.Online; Sel = 2; return true; }
            LeaveOnline("You left the game.");
            Menu = Screen.Online; Sel = 0;
            return true;
        }

        void OnlineTickSetup(double dt)
        {
            if (Online != null) OnlinePump();
            if (Menu == Screen.OnlineJoin && !looking && T >= lookAt)
            {
                looking = true; lookAt = T + 3;
                bool lb = LanLoopbackOnly;
                new Thread(() => { var f = LanTransport.Look(700, lb); lanFound = f; looking = false; }) { IsBackground = true, Name = "Smash look" }.Start();
            }
        }

        // ---------------------------------------------------------------- drawing
        void DrawOnline(DrawingContext dc)
        {
            Gfx.Panel(dc, new Rect(60, 40, Width - 120, Height - 80), 0xF0101522, 0xFF60A5FA);
            Gfx.Text(dc, "Play online", Width / 2, 60, 34, 0xFFFDE047, 0.5, Gfx.UiBold);
            Gfx.Text(dc, "Play with others on their own computers, phones and tablets. You'll show up as " + OnlineName + ".", Width / 2, 112, 17, 0xFFE5E7EB, 0.5, Gfx.Ui);
            DrawMenu(dc, Width / 2, 170, new[] { "Host a game on this home network", "Host a game over the internet", "Join a game", "Back" }, Sel, i => Sel = i);
            string[] help = {
                "Home network: everyone on the same Wi-Fi. The others see your game under Join (or type the address it shows).",
                "Internet: for family in other houses. You get a 4-letter code; the others type it under Join.",
                "Windows may ask whether the game can use the network the first time you host at home: choose Allow." };
            double y = 430;
            foreach (string h in help) { Gfx.Text(dc, h, Width / 2, y, 15, 0xFF8C93A5, 0.5, Gfx.Ui); y += 26; }
            if (onlineNote.Length > 0) Gfx.Text(dc, onlineNote, Width / 2, Height - 100, 17, 0xFFFCA5A5, 0.5, Gfx.UiBold);
        }

        void DrawJoin(DrawingContext dc)
        {
            Gfx.Panel(dc, new Rect(60, 40, Width - 120, Height - 80), 0xF0101522, 0xFF60A5FA);
            Gfx.Text(dc, "Join a game", Width / 2, 56, 34, 0xFFFDE047, 0.5, Gfx.UiBold);
            // left: games found on the home network
            Gfx.Text(dc, "On this home network", 110, 112, 20, 0xFFE6E9F0, 0, Gfx.UiBold);
            double y = 148;
            var found = lanFound;
            if (found.Count == 0) Gfx.Text(dc, looking ? "Looking..." : "No games found yet (still looking).", 110, y, 16, 0xFF8C93A5, 0, Gfx.Ui);
            foreach (LanTransport.Found f in found.Take(6))
            {
                var r = new Rect(100, y, 440, 52);
                bool lit = Hovered(r);
                dc.DrawRoundedRectangle(Gfx.B(lit ? 0xFF1E3A8A : 0xFF13161D), Gfx.P(lit ? 0xFF60A5FA : 0xFF2C3A55, 1.5), r, 10, 10);
                Gfx.Text(dc, f.Name, r.X + 16, r.Y + 6, 18, 0xFFFFFFFF, 0, Gfx.UiBold);
                Gfx.Text(dc, f.SameVersion ? f.Address : "a different version: update both devices", r.X + 16, r.Y + 29, 13, f.SameVersion ? 0xFF8C93A5 : 0xFFFCA5A5, 0, Gfx.Ui);
                LanTransport.Found pick = f;
                AddHit(r, "Join " + f.Name, false, null, () => { Audio.Ui("select"); Go(new NetSession(LanTransport.Join(pick.Address), false, OnlineName, Pick[0] >= 0 ? Pick[0] : 0)); });
                y += 60;
            }
            // right: the code (or an address) and a pad to type it with
            double px = 620;
            Gfx.Text(dc, "Over the internet: type the host's code", px, 112, 20, 0xFFE6E9F0, 0, Gfx.UiBold);
            var box = new Rect(px, 146, 520, 56);
            dc.DrawRoundedRectangle(Gfx.B(0xFF0B0D12), Gfx.P(0xFF60A5FA, 2), box, 10, 10);
            Gfx.Text(dc, joinText.Length > 0 ? joinText : "ABCD", px + 20, 156, 28, joinText.Length > 0 ? 0xFFFFFFFF : 0xFF3A4150, 0, Gfx.UiBold);
            double k = 48, gx = px, gy = 218;
            for (int i = 0; i < PadKeys.Length; i++)
            {
                var r = new Rect(gx + (i % 10) * (k + 4), gy + (i / 10) * (k + 4), k, k);
                bool lit = Hovered(r);
                dc.DrawRoundedRectangle(Gfx.B(lit ? 0xFF1E3A8A : 0xFF1A1E27), Gfx.P(0xFF353C4B, 1), r, 8, 8);
                Gfx.Text(dc, PadKeys[i].ToString(), r.X + k / 2, r.Y + 11, 20, 0xFFE6E9F0, 0.5, Gfx.UiBold);
                char c = PadKeys[i];
                AddHit(r, "Pad " + c, false, null, () => { if (joinText.Length < 21) { joinText += c; Audio.Ui("move"); } });
            }
            double by = gy + 4 * (k + 4) + 8;
            var del = new Rect(px, by, 150, 50); var go = new Rect(px + 170, by, 350, 50);
            dc.DrawRoundedRectangle(Gfx.B(Hovered(del) ? 0xFF3A2430 : 0xFF1A1E27), Gfx.P(0xFF353C4B, 1), del, 25, 25);
            Gfx.Text(dc, "⌫  Delete", del.X + del.Width / 2, del.Y + 13, 18, 0xFFE6E9F0, 0.5, Gfx.UiBold);
            AddKeyHit(del, "Delete", Key.Back);
            dc.DrawRoundedRectangle(Gfx.B(Hovered(go) ? 0xFF2563EB : 0xFF1E3A8A), Gfx.P(0xFF60A5FA, 2), go, 25, 25);
            Gfx.Text(dc, "Join  ▶", go.X + go.Width / 2, go.Y + 12, 20, 0xFFFFFFFF, 0.5, Gfx.UiBold);
            AddKeyHit(go, "Join", Key.Enter);
            Gfx.Text(dc, "Or type a home-network address the host's screen shows (like 192.168.1.20).", px, by + 64, 14, 0xFF8C93A5, 0, Gfx.Ui);
            if (onlineNote.Length > 0) Gfx.Text(dc, onlineNote, Width / 2, Height - 92, 16, 0xFFFCA5A5, 0.5, Gfx.UiBold);
        }

        void DrawLobby(DrawingContext dc)
        {
            Gfx.Panel(dc, new Rect(60, 40, Width - 120, Height - 80), 0xF0101522, 0xFF60A5FA);
            NetSession s = Online;
            if (s == null) return;
            if (s.State == NetSession.Phase.Connecting)
            {
                Gfx.Text(dc, "Connecting...", Width / 2, 300, 32, 0xFFE6E9F0, 0.5, Gfx.UiBold);
                return;
            }
            string where = s.Internet ? "Internet game  ·  code " + s.Net.Address : "Home network game  ·  " + s.Net.Address;
            Gfx.Text(dc, where, Width / 2, 54, 30, 0xFFFDE047, 0.5, Gfx.UiBold);
            Gfx.Text(dc, s.IsHost ? (s.Internet ? "Others tap Play online > Join a game and type the code." : "Others tap Play online > Join a game: it shows up there.") : "Waiting for the host to start the match.", Width / 2, 98, 16, 0xFFB8BFCC, 0.5, Gfx.Ui);
            // the players (and computer players) as cards
            int cpus = s.Cpus;
            int count = Math.Min(4, s.Players.Count + cpus);
            double cw = 220, gap = 20, x0 = Width / 2 - (count * cw + (count - 1) * gap) / 2;
            for (int i = 0; i < count; i++)
            {
                bool cpu = i >= s.Players.Count;
                NetSession.Player p = cpu ? null : s.Players[i];
                var r = new Rect(x0 + i * (cw + gap), 130, cw, 230);
                bool me = p != null && p.Peer == s.Net.MyId;
                dc.DrawRoundedRectangle(Gfx.B(0xFF13161D), Gfx.P(me ? 0xFFFDE047 : 0xFF2C3A55, me ? 3 : 1.5), r, 14, 14);
                if (!cpu) View.Portrait(dc, BrawlData.Fighters[p.Fighter], r.X + cw / 2, r.Y + 170, 120, T + i, false, BS.Idle);
                else Gfx.Text(dc, "?", r.X + cw / 2, r.Y + 50, 80, 0xFF3A4150, 0.5, Gfx.UiBold);
                Gfx.Text(dc, cpu ? "Computer" : p.Name + (me ? " (you)" : ""), r.X + cw / 2, r.Y + 182, 18, 0xFFFFFFFF, 0.5, Gfx.UiBold);
                Gfx.Text(dc, cpu ? LevelNames[Math.Max(0, Array.IndexOf(BotLevels, s.CpuLevel))] + ", picks at random" : BrawlData.Fighters[p.Fighter].Name, r.X + cw / 2, r.Y + 204, 14, 0xFF8C93A5, 0.5, Gfx.Ui);
            }
            // rows
            var labels = new List<string> { "Your fighter" }; var values = new List<string> { BrawlData.Fighters[s.MyFighter].Name };
            if (s.IsHost)
            {
                labels.AddRange(new[] { "Stage", "Lives", "Computer players", "Their level", "Stage surprises" });
                values.AddRange(new[] { BrawlData.Stages[s.Stage].Name, s.Lives.ToString(), s.Cpus.ToString(), LevelNames[Math.Max(0, Array.IndexOf(BotLevels, s.CpuLevel))], s.Surprises ? "On" : "Off" });
            }
            else
            {
                Gfx.Text(dc, "Stage: " + BrawlData.Stages[s.Stage].Name + "   ·   Lives: " + s.Lives + "   ·   Computer players: " + s.Cpus, Width / 2, 420, 17, 0xFFB8BFCC, 0.5, Gfx.Ui);
            }
            double ry = 384;
            double rx = s.IsHost ? 170 : Width / 2 - 270;
            for (int row = 0; row < labels.Count; row++)
            {
                double col = s.IsHost && row >= 3 ? 1 : 0, rowY = ry + (s.IsHost ? (row % 3) * 46 : 0);
                if (!s.IsHost) rowY = 450;
                var rr = new Rect(rx + col * 480, rowY, 440, 40);
                bool sel = Sel == row;
                dc.DrawRoundedRectangle(Gfx.B(sel ? 0xFF1B2D50 : 0xFF13161D), Gfx.P(sel ? 0xFF3B82F6 : 0xFF252A35, 1.5), rr, 8, 8);
                Gfx.Text(dc, labels[row], rr.X + 14, rr.Y + 9, 17, 0xFFB8BFCC, 0, Gfx.Ui);
                Gfx.Text(dc, "◀", rr.X + 220, rr.Y + 9, 17, 0xFF60A5FA, 0.5, Gfx.UiBold);
                Gfx.Text(dc, values[row], rr.X + 320, rr.Y + 9, 17, 0xFFFFFFFF, 0.5, Gfx.UiBold);
                Gfx.Text(dc, "▶", rr.X + 420, rr.Y + 9, 17, 0xFF60A5FA, 0.5, Gfx.UiBold);
                int rowI = row;
                AddHit(rr, labels[row] + " " + values[row], sel, () => Sel = rowI, () => { Sel = rowI; LobbyChange(rowI, 1); }).Item = row;
                AddArrowHits(rr, rr.X + 220, rr.X + 420, labels[row], row, sel, () => Sel = rowI);
            }
            if (s.IsHost)
            {
                var start = new Rect(Width / 2 - 160, Height - 120, 320, 56);
                bool sel = Sel == 6 || Hovered(start);
                dc.DrawRoundedRectangle(Gfx.B(s.CanStart ? (sel ? 0xFF2563EB : 0xFF1E3A8A) : 0xFF1A1E27), Gfx.P(0xFF60A5FA, 2), start, 28, 28);
                Gfx.Text(dc, s.CanStart ? "Start the match  ▶" : "Waiting for players", start.X + start.Width / 2, start.Y + 14, 22, 0xFFFFFFFF, 0.5, Gfx.UiBold);
                AddHit(start, "Start", Sel == 6, () => Sel = 6, () => { if (s.CanStart) { Audio.Ui("select"); s.StartMatch((uint)Rng.Next(1, int.MaxValue)); } });
            }
            if (onlineNote.Length > 0) Gfx.Text(dc, onlineNote, Width / 2, Height - 150, 16, 0xFFFCA5A5, 0.5, Gfx.UiBold);
        }
    }
}
