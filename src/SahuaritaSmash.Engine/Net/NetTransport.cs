// Network play, part 1: how bytes get between devices. A transport connects this device to the others in one game
// room: peer 1 is the host, the rest are numbered as they join. Messages arrive whole and in order (TCP on the home
// network, a WebSocket through the relay on the internet). Everything is polled from the game's own thread: the
// transports' background threads only queue events, never touch the game.
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace ClassicGames
{
    public enum NetEventKind { Joined, Left, Data, Closed }

    public struct NetEvent
    {
        public NetEventKind Kind;
        public int Peer;
        public byte[] Data;
        /// <summary>Closed: why, in words for people.</summary>
        public string Reason;
    }

    public interface INetTransport
    {
        /// <summary>This device's peer number (1 = the host; 0 until connected).</summary>
        int MyId { get; }
        bool IsHost { get; }
        /// <summary>The 4-letter room code (internet) or the host's address (home network), for the lobby.</summary>
        string Address { get; }
        /// <summary>Sends to one peer, or to everyone else when to is 0.</summary>
        void Send(int to, byte[] data);
        bool Poll(out NetEvent e);
        void Close();
    }

    /// <summary>A pretend network for the checks: rooms in memory, with a delay (and jitter) in steps of Pump.</summary>
    public sealed class LoopbackRoom
    {
        sealed class Msg { public int From, To; public byte[] Data; public long Due; }
        readonly List<Peer> peers = new List<Peer>();
        readonly List<Msg> wire = new List<Msg>();
        readonly Random rng;
        public int Delay, Jitter;
        long now;
        int nextId = 1;

        public LoopbackRoom(int delay, int jitter, int seed) { Delay = delay; Jitter = jitter; rng = new Random(seed); }

        public sealed class Peer : INetTransport
        {
            internal LoopbackRoom Room; internal int Id; internal bool Gone;
            internal readonly Queue<NetEvent> Inbox = new Queue<NetEvent>();
            public int MyId { get { return Id; } }
            public bool IsHost { get { return Id == 1; } }
            public string Address { get { return "LOOP"; } }
            public void Send(int to, byte[] data) { if (!Gone) Room.Queue(Id, to, data); }
            public bool Poll(out NetEvent e) { if (Inbox.Count > 0) { e = Inbox.Dequeue(); return true; } e = default(NetEvent); return false; }
            public void Close() { Room.Leave(this); }
        }

        public Peer Join()
        {
            var p = new Peer { Room = this, Id = nextId++ };
            foreach (Peer o in peers) if (!o.Gone) { o.Inbox.Enqueue(new NetEvent { Kind = NetEventKind.Joined, Peer = p.Id }); p.Inbox.Enqueue(new NetEvent { Kind = NetEventKind.Joined, Peer = o.Id }); }
            peers.Add(p);
            return p;
        }

        void Queue(int from, int to, byte[] data)
        {
            long last = 0;
            foreach (Msg m in wire) if (m.From == from) last = Math.Max(last, m.Due);   // in order, like TCP
            foreach (Peer p in peers)
            {
                if (p.Gone || p.Id == from || (to != 0 && p.Id != to)) continue;
                long due = Math.Max(last, now + Delay + (Jitter > 0 ? rng.Next(Jitter + 1) : 0));
                wire.Add(new Msg { From = from, To = p.Id, Data = (byte[])data.Clone(), Due = due });
            }
        }

        /// <summary>One tick of pretend time: messages that are due arrive.</summary>
        public void Pump()
        {
            now++;
            for (int i = 0; i < wire.Count; i++)
            {
                Msg m = wire[i];
                if (m.Due > now) continue;
                Peer p = peers.Find(x => x.Id == m.To);
                if (p != null && !p.Gone) p.Inbox.Enqueue(new NetEvent { Kind = NetEventKind.Data, Peer = m.From, Data = m.Data });
                wire.RemoveAt(i--);
            }
        }

        void Leave(Peer p)
        {
            if (p.Gone) return;
            p.Gone = true;
            foreach (Peer o in peers)
            {
                if (o.Gone) continue;
                if (p.Id == 1) { o.Gone = true; o.Inbox.Enqueue(new NetEvent { Kind = NetEventKind.Closed, Reason = "The host left the game." }); }
                else o.Inbox.Enqueue(new NetEvent { Kind = NetEventKind.Left, Peer = p.Id });
            }
        }
    }
}
