// Network play, part 4: a game room from start to finish, the same over the home network and the internet. The host
// keeps the lobby (who's in, their fighters, the stage and rules, computer players) and starts matches; everyone
// else says hello, picks a fighter and waits. A guest from another game or another version is refused with words for
// people. Poll() is called every frame on the game's own thread.
using System;
using System.Collections.Generic;
using System.Linq;

namespace ClassicGames
{
    public sealed class NetSession
    {
        public enum Phase { Connecting, Lobby, Playing, Closed }

        public sealed class Player { public int Peer; public string Name = ""; public int Fighter; }

        public readonly INetTransport Net;
        public readonly bool Internet;
        public readonly string MyName;
        public Phase State = Phase.Connecting;
        /// <summary>Why the room closed (words for people), once State is Closed.</summary>
        public string Reason;
        public readonly List<Player> Players = new List<Player>();
        // the host's choices (the guests see them in the lobby)
        public int Stage, Lives = 3, Minutes, Cpus, CpuLevel = 3;
        public bool Surprises = true;
        public int MyFighter;
        public NetMatch Match;
        /// <summary>Raised when a match starts (the game sets itself up from Match.Setup).</summary>
        public event Action MatchStarted;
        /// <summary>Raised when the host sends everyone back to the lobby.</summary>
        public event Action BackToLobby;
        bool helloSent;
        public const int MaxPlayers = 4;

        public NetSession(INetTransport net, bool internet, string name, int fighter)
        {
            Net = net; Internet = internet; MyName = string.IsNullOrWhiteSpace(name) ? "Player" : name.Trim(); MyFighter = fighter;
        }

        public bool IsHost { get { return Net.IsHost; } }
        public Player Me { get { return Players.FirstOrDefault(p => p.Peer == Net.MyId); } }
        /// <summary>The input delay: a little more over the internet, where messages take longer.</summary>
        public int Delay { get { return Internet ? 7 : 4; } }

        public void Poll()
        {
            if (State == Phase.Closed) return;
            if (!helloSent && Net.MyId != 0)
            {
                helloSent = true;
                if (Net.IsHost) { Players.Add(new Player { Peer = 1, Name = MyName, Fighter = MyFighter }); State = Phase.Lobby; }
                else Net.Send(1, new NetWriter(NetProtocol.Hello).S(NetProtocol.Game).I(NetProtocol.Version).S(MyName).I(MyFighter).Done());
            }
            NetEvent e;
            int guard = 0;
            while (guard++ < 500 && Net.Poll(out e))
            {
                switch (e.Kind)
                {
                    case NetEventKind.Closed: Close(e.Reason ?? "The game ended."); return;
                    case NetEventKind.Left:
                        if (IsHost)
                        {
                            Players.RemoveAll(p => p.Peer == e.Peer);
                            if (Match != null) Match.PeerLeft(e.Peer);
                            SendLobby();
                        }
                        break;
                    case NetEventKind.Data:
                        try { Handle(e.Peer, e.Data); } catch (Exception) { }   // a broken packet is dropped
                        break;
                }
            }
            if (IsHost && Match != null && Match.Broken != null) { string why = Match.Broken; Net.Send(0, new NetWriter(NetProtocol.End).S(why).Done()); EndMatch(); Reason = why; }
        }

        void Handle(int from, byte[] data)
        {
            var r = new NetReader(data);
            switch (r.Kind)
            {
                case NetProtocol.Hello:
                    {
                        if (!IsHost) return;
                        string game = r.S(); int version = r.I(); string name = r.S(); int fighter = r.I();
                        string no = game != NetProtocol.Game ? "That code is for a different game."
                            : version != NetProtocol.Version ? (version < NetProtocol.Version ? "That device has an older Sahuarita Smash!: update it to join." : "This game is older than yours: the host needs to update.")
                            : Players.Count >= MaxPlayers ? "That game is full (4 players)."
                            : State == Phase.Playing ? "That game is in the middle of a match: try again when it's back in the lobby."
                            : null;
                        if (no != null) { Net.Send(from, new NetWriter(NetProtocol.Refuse).S(no).Done()); return; }
                        if (Players.Any(p => p.Peer == from)) return;
                        Players.Add(new Player { Peer = from, Name = Unique(Clean(name)), Fighter = Math.Max(0, Math.Min(BrawlData.Fighters.Count - 1, fighter)) });
                        SendLobby();
                        break;
                    }
                case NetProtocol.Refuse: if (from == 1) Close(r.S()); break;
                case NetProtocol.Lobby:
                    {
                        if (from != 1) return;
                        Stage = r.I(); Lives = r.I(); Minutes = r.I(); Surprises = r.Bool(); Cpus = r.I(); CpuLevel = r.I();
                        int n = r.I();
                        if (n < 0 || n > MaxPlayers) return;
                        Players.Clear();
                        for (int i = 0; i < n; i++) Players.Add(new Player { Peer = r.I(), Name = r.S(), Fighter = r.I() });
                        if (State == Phase.Connecting) State = Phase.Lobby;
                        break;
                    }
                case NetProtocol.Pick:
                    {
                        if (!IsHost) return;
                        Player p = Players.FirstOrDefault(x => x.Peer == from);
                        int f = r.I();
                        if (p != null && f >= 0 && f < BrawlData.Fighters.Count) { p.Fighter = f; SendLobby(); }
                        break;
                    }
                case NetProtocol.Start:
                    {
                        if (from != 1) return;
                        Begin(NetMatchSetup.FromPacket(r));
                        break;
                    }
                case NetProtocol.End:
                    {
                        if (from != 1) return;
                        string why = r.S();
                        EndMatch();
                        if (!string.IsNullOrEmpty(why)) Reason = why;
                        break;
                    }
                default:
                    if (Match != null) Match.Receive(from, r);
                    break;
            }
        }

        static string Clean(string s)
        {
            s = new string((s ?? "").Where(c => !char.IsControl(c)).ToArray()).Trim();
            if (s.Length > 16) s = s.Substring(0, 16);
            return s.Length == 0 ? "Player" : s;
        }
        string Unique(string name)
        {
            string n = name; int k = 2;
            while (Players.Any(p => string.Equals(p.Name, n, StringComparison.OrdinalIgnoreCase))) n = name + " " + k++;
            return n;
        }

        // ---------------------------------------------------------------- the host's lobby
        public void SendLobby()
        {
            if (!IsHost || State == Phase.Closed) return;
            Cpus = Math.Max(0, Math.Min(MaxPlayers - Players.Count, Cpus));
            if (Players.Count + Cpus < 2 && Players.Count < MaxPlayers) Cpus = Math.Max(Cpus, 2 - Players.Count);
            var w = new NetWriter(NetProtocol.Lobby).I(Stage).I(Lives).I(Minutes).Bool(Surprises).I(Cpus).I(CpuLevel).I(Players.Count);
            foreach (Player p in Players) w.I(p.Peer).S(p.Name).I(p.Fighter);
            Net.Send(0, w.Done());
        }

        /// <summary>This device's person picks a fighter (the host updates the lobby; a guest asks the host).</summary>
        public void Pick(int fighter)
        {
            MyFighter = fighter;
            if (IsHost) { Player me = Me; if (me != null) { me.Fighter = fighter; SendLobby(); } }
            else Net.Send(1, new NetWriter(NetProtocol.Pick).I(fighter).Done());
        }

        public bool CanStart { get { return IsHost && State == Phase.Lobby && Players.Count + Math.Max(Cpus, 2 - Players.Count) >= 2; } }

        /// <summary>Host: starts a match with everyone in the lobby plus the computer players.</summary>
        public void StartMatch(uint seed)
        {
            if (!CanStart) return;
            SendLobby();
            var setup = new NetMatchSetup { Stage = Stage, Lives = Lives, Minutes = Minutes, Surprises = Surprises, Delay = Delay, Seed = seed };
            foreach (Player p in Players) setup.Seats.Add(new NetMatchSetup.Seat { Owner = p.Peer, Fighter = p.Fighter, Name = p.Name });
            var rng = new Random((int)seed);
            for (int i = 0; i < Cpus && setup.Seats.Count < MaxPlayers; i++)
            {
                int f = rng.Next(BrawlData.Fighters.Count);
                setup.Seats.Add(new NetMatchSetup.Seat { Owner = 0, Fighter = f, BotLevel = CpuLevel, Name = BrawlData.Fighters[f].Name });
            }
            Net.Send(0, setup.ToPacket());
            Begin(setup);
        }

        void Begin(NetMatchSetup setup)
        {
            Match = new NetMatch(setup, Net.MyId) { SendAll = d => Net.Send(0, d) };
            State = Phase.Playing;
            Reason = null;
            if (MatchStarted != null) MatchStarted();
        }

        /// <summary>Host: everyone back to the lobby (after the results, or when a match can't go on).</summary>
        public void ToLobby()
        {
            if (!IsHost || State != Phase.Playing) return;
            Net.Send(0, new NetWriter(NetProtocol.End).S("").Done());
            EndMatch();
        }

        void EndMatch()
        {
            if (State != Phase.Playing) return;
            State = Phase.Lobby;
            if (IsHost) SendLobby();
            if (BackToLobby != null) BackToLobby();
            Match = null;
        }

        void Close(string reason)
        {
            if (State == Phase.Closed) return;
            State = Phase.Closed; Reason = reason;
            try { Net.Close(); } catch (Exception) { }
        }

        /// <summary>This device leaves (the host leaving ends the room for everyone).</summary>
        public void Leave() { Close("You left the game."); }
    }
}
