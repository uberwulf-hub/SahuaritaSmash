// Network play, part 3: one match on several devices, kept in step ("lockstep"). The match engine gives the same match
// for the same buttons (BrawlSim.Step, its own seeded randomness, DMath's sine and cosine), so devices send each
// other only their people's buttons and every device runs the whole match itself:
//   - a person's buttons for frame N are sent when the device reaches frame N - Delay, so they have time to arrive;
//   - a device steps frame N only when it has every person's buttons for N (computer players are worked out on each
//     device, from the same match, so they agree);
//   - every 120 frames each device sends a checksum of its match to the host, which compares them (they never
//     should differ; if they do, the match stops with a plain message instead of going on differently);
//   - when someone leaves, the host hands their fighter to a computer player from a frame everyone can still reach.
using System;
using System.Collections.Generic;

namespace ClassicGames
{
    /// <summary>Who fights where, and the rules: what the host sends in its Start message.</summary>
    public sealed class NetMatchSetup
    {
        public sealed class Seat
        {
            /// <summary>The peer whose person plays it, or 0 for a computer player.</summary>
            public int Owner;
            public int Fighter, BotLevel = 3;
            public string Name = "";
        }
        public int Stage, Lives = 3, Minutes, Delay = 4;
        public bool Surprises = true;
        public uint Seed = 1;
        /// <summary>Which match of the room this is (1, 2, ... a rematch is the next number). Every match message
        /// carries it, so a late message from the match before is never taken for one of this match's.</summary>
        public int Round = 1;
        public readonly List<Seat> Seats = new List<Seat>();

        public byte[] ToPacket()
        {
            var w = new NetWriter(NetProtocol.Start).I(Stage).I(Lives).I(Minutes).I(Delay).Bool(Surprises).U(Seed).I(Seats.Count);
            foreach (Seat s in Seats) w.I(s.Owner).I(s.Fighter).I(s.BotLevel).S(s.Name);
            w.I(Round);
            return w.Done();
        }

        public static NetMatchSetup FromPacket(NetReader r)
        {
            var m = new NetMatchSetup { Stage = r.I(), Lives = r.I(), Minutes = r.I(), Delay = r.I(), Surprises = r.Bool(), Seed = r.U() };
            int n = r.I();
            if (n < 2 || n > 4) throw new FormatException("seats");
            for (int i = 0; i < n; i++) m.Seats.Add(new Seat { Owner = r.I(), Fighter = r.I(), BotLevel = r.I(), Name = r.S() });
            m.Round = r.I();
            m.Check();
            return m;
        }

        void Check()
        {
            if (Stage < 0 || Stage >= BrawlData.Stages.Count || Lives < 1 || Lives > 9 || Minutes < 0 || Minutes > 9 || Delay < 1 || Delay > 30) throw new FormatException("rules");
            foreach (Seat s in Seats) if (s.Fighter < 0 || s.Fighter >= BrawlData.Fighters.Count || s.BotLevel < 1 || s.BotLevel > 9) throw new FormatException("seat");
        }

        /// <summary>The match itself, the same on every device.</summary>
        public BrawlSim Build()
        {
            var defs = new List<BFighterDef>();
            foreach (Seat s in Seats) defs.Add(BrawlData.Fighters[s.Fighter]);
            var sim = new BrawlSim(BrawlData.Stages[Stage], defs, Lives, Minutes, Seed);
            sim.Surprises = Surprises;
            return sim;
        }
    }

    public sealed class NetMatch
    {
        public readonly NetMatchSetup Setup;
        public readonly BrawlSim Sim;
        public readonly int Me;
        readonly BrawlBot[] bots;
        readonly int[] owner;
        // each seat's buttons by frame (packed); frames below Delay are empty for everyone
        readonly Dictionary<int, int>[] known;
        readonly int[] nextLocal;
        readonly BrInput[] step;
        /// <summary>Sends a packet to everyone else (the session gives it the transport's Send(0, ...)).</summary>
        public Action<byte[]> SendAll;
        /// <summary>Host only: the checksums devices reported, by frame.</summary>
        readonly Dictionary<int, Dictionary<int, long>> checks = new Dictionary<int, Dictionary<int, long>>();
        public const int CheckEvery = 120;
        /// <summary>Set when the match can't go on (a device's match differs from the host's): words for people.</summary>
        public string Broken;
        /// <summary>Frames waited for someone's buttons (for the "waiting for ..." note and the checks).</summary>
        public int Stalls;
        /// <summary>Messages from an earlier match of the room that arrived late and were left out (the checks).</summary>
        public int Stale;
        /// <summary>The checks: every frame's buttons as stepped (frame, then each seat's packed buttons; computer
        /// players' seats are -1, since replaying works them out again).</summary>
        public Action<int, int[]> Stepped;

        public NetMatch(NetMatchSetup setup, int me)
        {
            Setup = setup; Me = me;
            Sim = setup.Build();
            int n = setup.Seats.Count;
            bots = new BrawlBot[n]; owner = new int[n]; known = new Dictionary<int, int>[n]; nextLocal = new int[n]; step = new BrInput[n];
            for (int i = 0; i < n; i++)
            {
                owner[i] = setup.Seats[i].Owner;
                known[i] = new Dictionary<int, int>();
                nextLocal[i] = setup.Delay;
                if (owner[i] == 0) bots[i] = new BrawlBot(i, setup.Seats[i].BotLevel);
            }
        }

        /// <summary>This device's seats (people playing here).</summary>
        public IEnumerable<int> MySeats() { for (int i = 0; i < owner.Length; i++) if (owner[i] == Me) yield return i; }
        public bool IsMine(int seat) { return owner[seat] == Me; }
        public bool IsBot(int seat) { return owner[seat] == 0; }

        /// <summary>This device's person's buttons now, for one of its seats: scheduled for frame Sim.Frame + Delay
        /// (and any frame before that not yet sent), so they reach the others in time.</summary>
        public void Local(int seat, BrInput input)
        {
            if (owner[seat] != Me) return;
            int packed = NetProtocol.Pack(input);
            int upto = Sim.Frame + Setup.Delay;
            if (nextLocal[seat] > upto) return;
            int first = nextLocal[seat], count = upto - first + 1;
            var w = new NetWriter(NetProtocol.Inputs).I(Setup.Round).I(seat).I(first).I(count);
            for (int f = first; f <= upto; f++) { known[seat][f] = packed; w.I(packed); }
            nextLocal[seat] = upto + 1;
            if (SendAll != null) SendAll(w.Done());
        }

        /// <summary>A match packet from another device (inputs, checksums, a seat handed to the computer).</summary>
        public void Receive(int from, NetReader r)
        {
            if (r.Kind != NetProtocol.Inputs && r.Kind != NetProtocol.Check && r.Kind != NetProtocol.Drop) return;
            if (r.I() != Setup.Round) { Stale++; return; }   // from the match before a rematch: not this one's
            switch (r.Kind)
            {
                case NetProtocol.Inputs:
                    {
                        int seat = r.I(), first = r.I(), count = r.I();
                        if (seat < 0 || seat >= owner.Length || owner[seat] != from || count < 0 || count > 600) return;   // only a seat's own device speaks for it
                        for (int k = 0; k < count; k++) known[seat][first + k] = r.I();
                        break;
                    }
                case NetProtocol.Check:
                    {
                        int frame = r.I(); long sum = r.L();
                        if (Me == 1) Compare(from, frame, sum);
                        break;
                    }
                case NetProtocol.Drop:
                    {
                        int seat = r.I(), fromFrame = r.I();
                        if (from == 1) Hand(seat, fromFrame);
                        break;
                    }
            }
        }

        /// <summary>Host: someone left; their fighter is played by the computer from the first frame nobody has
        /// stepped yet without them (the frame after the last buttons that came from them).</summary>
        public void PeerLeft(int peer)
        {
            if (Me != 1) return;
            for (int seat = 0; seat < owner.Length; seat++)
            {
                if (owner[seat] != peer) continue;
                int last = Setup.Delay - 1;
                foreach (int f in known[seat].Keys) if (f > last) last = f;
                int fromFrame = last + 1;
                Hand(seat, fromFrame);
                if (SendAll != null) SendAll(new NetWriter(NetProtocol.Drop).I(Setup.Round).I(seat).I(fromFrame).Done());
            }
        }

        readonly Dictionary<int, int> handAt = new Dictionary<int, int>();
        void Hand(int seat, int fromFrame)
        {
            if (seat < 0 || seat >= owner.Length || handAt.ContainsKey(seat)) return;
            handAt[seat] = fromFrame;
        }

        /// <summary>Whether frame Sim.Frame can be stepped: every person's buttons for it are here.</summary>
        public bool CanStep
        {
            get
            {
                if (Broken != null || Sim.Over && Sim.OverT > 600) return false;
                int f = Sim.Frame;
                for (int i = 0; i < owner.Length; i++)
                {
                    if (owner[i] == 0 || f < Setup.Delay) continue;
                    int at;
                    if (handAt.TryGetValue(i, out at) && f >= at) continue;
                    if (!known[i].ContainsKey(f)) return false;
                }
                return true;
            }
        }

        /// <summary>The seat whose buttons the match is waiting for (-1 when none).</summary>
        public int WaitingFor()
        {
            int f = Sim.Frame;
            if (f < Setup.Delay) return -1;
            for (int i = 0; i < owner.Length; i++)
            {
                int at;
                if (owner[i] == 0 || (handAt.TryGetValue(i, out at) && f >= at)) continue;
                if (!known[i].ContainsKey(f)) return i;
            }
            return -1;
        }

        /// <summary>Steps one frame if it can; false while waiting for someone's buttons.</summary>
        public bool TryStep()
        {
            if (!CanStep) { Stalls++; return false; }
            int f = Sim.Frame;
            for (int i = 0; i < owner.Length; i++)
            {
                int at;
                if (owner[i] != 0 && handAt.TryGetValue(i, out at) && f >= at) { owner[i] = 0; bots[i] = new BrawlBot(i, 5); }
                if (owner[i] == 0) { step[i] = bots[i].Think(Sim); continue; }
                int p;
                step[i] = f < Setup.Delay || !known[i].TryGetValue(f, out p) ? new BrInput() : NetProtocol.Unpack(p);
                known[i].Remove(f - 1);
            }
            if (Stepped != null)
            {
                var rec = new int[owner.Length];
                for (int i = 0; i < owner.Length; i++) rec[i] = owner[i] == 0 ? -1 : NetProtocol.Pack(step[i]);
                Stepped(f, rec);
            }
            Sim.Step(step);
            if (Sim.Frame % CheckEvery == 0)
            {
                long sum = Sim.Checksum();
                if (Me == 1) Compare(1, Sim.Frame, sum);
                else if (SendAll != null) SendAll(new NetWriter(NetProtocol.Check).I(Setup.Round).I(Sim.Frame).L(sum).Done());
            }
            return true;
        }

        void Compare(int from, int frame, long sum)
        {
            Dictionary<int, long> d;
            if (!checks.TryGetValue(frame, out d)) checks[frame] = d = new Dictionary<int, long>();
            d[from] = sum;
            long mine;
            if (d.TryGetValue(1, out mine))
                foreach (var kv in d)
                    if (kv.Value != mine) { Broken = "The games on two devices stopped matching, so this match has to end. (Make sure everyone has the same version.)"; return; }
            if (checks.Count > 20) { int oldest = int.MaxValue; foreach (int k in checks.Keys) oldest = Math.Min(oldest, k); checks.Remove(oldest); }
        }
    }
}
