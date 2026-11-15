// Network play on the home network: the host listens on TCP (the first free port of 47871-47879) and passes messages
// between the others, like the relay does; other devices find it by asking on UDP 47870 ("who's hosting?"), or by
// typing its address. Frames on the wire: 4-byte length, then a 4-byte peer id (sending: who it's for, 0 = everyone
// else; arriving: who it's from), then the packet. Control frames have peer id -1: "W" + your id (welcome),
// "J"/"L" + id (joined / left). Only private-network addresses are answered.
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ClassicGames
{
    public sealed class LanTransport : INetTransport
    {
        public const int DiscoveryPort = 47870, FirstPort = 47871, LastPort = 47879, MaxPeers = 6;
        /// <summary>The platform's way to hear the home network's "who's hosting?" while hosting (Android drops those
        /// broadcasts unless the app holds a multicast lock): true when hosting starts, false when it ends.</summary>
        public static Action<bool> HoldBroadcasts;
        const string Ask = "SSMASH?", Answer = "SSMASH!";

        readonly ConcurrentQueue<NetEvent> inbox = new ConcurrentQueue<NetEvent>();
        volatile int myId;
        volatile bool closed;
        string address = "";
        // host
        TcpListener listener; UdpClient answerer; string hostName = ""; bool holding;
        readonly Dictionary<int, Conn> conns = new Dictionary<int, Conn>();
        int nextId = 2;
        // guest
        Conn toHost;

        public int MyId { get { return myId; } }
        public bool IsHost { get { return myId == 1; } }
        public string Address { get { return address; } }
        public int Port { get; private set; }

        sealed class Conn
        {
            public int Id; public TcpClient Tcp; public NetworkStream Stream; public readonly object WriteLock = new object();
            public void Write(int peer, byte[] data)
            {
                var b = new byte[8 + data.Length];
                BitConverter.GetBytes(4 + data.Length).CopyTo(b, 0);
                BitConverter.GetBytes(peer).CopyTo(b, 4);
                Buffer.BlockCopy(data, 0, b, 8, data.Length);
                lock (WriteLock) Stream.Write(b, 0, b.Length);
            }
        }

        LanTransport() { }

        // ---------------------------------------------------------------- hosting
        /// <summary>Hosts a game on the home network (name = shown to the others when they look for games).</summary>
        public static LanTransport Host(string name, bool loopbackOnly = false)
        {
            var t = new LanTransport { hostName = (name ?? "").Replace("|", " ") };
            IPAddress bind = loopbackOnly ? IPAddress.Loopback : IPAddress.Any;
            for (int port = FirstPort; port <= LastPort && t.listener == null; port++)
            {
                try { var l = new TcpListener(bind, port); l.Start(); t.listener = l; t.Port = port; }
                catch (SocketException) { }
            }
            if (t.listener == null) { t.Fail("Couldn't start a game on this device (the game's network ports are busy)."); return t; }
            t.myId = 1;
            t.address = (loopbackOnly ? "127.0.0.1" : LocalAddress() ?? "this device") + (t.Port != FirstPort ? ":" + t.Port : "");
            new Thread(t.AcceptLoop) { IsBackground = true, Name = "Smash host" }.Start();
            {
                try
                {
                    t.answerer = new UdpClient(new IPEndPoint(loopbackOnly ? IPAddress.Loopback : IPAddress.Any, DiscoveryPort)) { EnableBroadcast = true };
                    if (!loopbackOnly && HoldBroadcasts != null) { try { HoldBroadcasts(true); t.holding = true; } catch (Exception) { } }
                    new Thread(t.AnswerLoop) { IsBackground = true, Name = "Smash answers" }.Start();
                }
                catch (SocketException) { }   // another game on this device answers already: typing the address still works
            }
            return t;
        }

        void AcceptLoop()
        {
            while (!closed)
            {
                TcpClient c;
                try { c = listener.AcceptTcpClient(); } catch (Exception) { return; }
                if (!Private(((IPEndPoint)c.Client.RemoteEndPoint).Address)) { c.Close(); continue; }
                Conn conn; int[] others;
                lock (conns)
                {
                    if (conns.Count + 1 >= MaxPeers) { c.Close(); continue; }
                    c.NoDelay = true;
                    conn = new Conn { Id = nextId++, Tcp = c, Stream = c.GetStream() };
                    others = conns.Keys.ToArray();
                    conns[conn.Id] = conn;
                }
                try
                {
                    conn.Write(-1, Control('W', conn.Id));
                    conn.Write(-1, Control('J', 1));
                    foreach (int o in others) conn.Write(-1, Control('J', o));
                }
                catch (Exception) { }
                foreach (int o in others) Forward(o, -1, Control('J', conn.Id));
                inbox.Enqueue(new NetEvent { Kind = NetEventKind.Joined, Peer = conn.Id });
                Conn me = conn;
                new Thread(() => HostRead(me)) { IsBackground = true, Name = "Smash peer " + me.Id }.Start();
            }
        }

        void HostRead(Conn c)
        {
            try
            {
                int to; byte[] data;
                while (!closed && ReadFrame(c.Stream, out to, out data))
                {
                    if (to == 0 || to == 1) inbox.Enqueue(new NetEvent { Kind = NetEventKind.Data, Peer = c.Id, Data = data });
                    if (to == 0) { int[] ids; lock (conns) ids = conns.Keys.ToArray(); foreach (int o in ids) if (o != c.Id) Forward(o, c.Id, data); }
                    else if (to > 1) Forward(to, c.Id, data);
                }
            }
            catch (Exception) { }
            bool was;
            lock (conns) was = conns.Remove(c.Id);
            try { c.Tcp.Close(); } catch (Exception) { }
            if (!was || closed) return;
            inbox.Enqueue(new NetEvent { Kind = NetEventKind.Left, Peer = c.Id });
            int[] rest; lock (conns) rest = conns.Keys.ToArray();
            foreach (int o in rest) Forward(o, -1, Control('L', c.Id));
        }

        void Forward(int to, int from, byte[] data)
        {
            Conn c; lock (conns) conns.TryGetValue(to, out c);
            if (c == null) return;
            try { c.Write(from, data); } catch (Exception) { try { c.Tcp.Close(); } catch (Exception) { } }
        }

        void AnswerLoop()
        {
            byte[] reply = Encoding.UTF8.GetBytes(Answer + "|" + NetProtocol.Version + "|" + Port + "|" + hostName);
            while (!closed)
            {
                try
                {
                    var from = new IPEndPoint(IPAddress.Any, 0);
                    byte[] q = answerer.Receive(ref from);
                    if (Encoding.UTF8.GetString(q) == Ask && Private(from.Address)) answerer.Send(reply, reply.Length, from);
                }
                catch (Exception) { if (closed) return; Thread.Sleep(50); }
            }
        }

        // ---------------------------------------------------------------- joining
        /// <summary>Joins the game at this address ("192.168.1.20" or "192.168.1.20:47872").</summary>
        public static LanTransport Join(string address)
        {
            var t = new LanTransport();
            string host = (address ?? "").Trim(); int port = FirstPort;
            int colon = host.LastIndexOf(':');
            if (colon > 0 && int.TryParse(host.Substring(colon + 1), out int p)) { port = p; host = host.Substring(0, colon); }
            t.address = address;
            new Thread(() =>
            {
                try
                {
                    var c = new TcpClient { NoDelay = true };
                    if (!c.ConnectAsync(host, port).Wait(5000)) throw new TimeoutException();
                    t.toHost = new Conn { Id = 1, Tcp = c, Stream = c.GetStream() };
                    int from; byte[] data;
                    while (!t.closed && ReadFrame(t.toHost.Stream, out from, out data))
                    {
                        if (from != -1) { t.inbox.Enqueue(new NetEvent { Kind = NetEventKind.Data, Peer = from, Data = data }); continue; }
                        if (data.Length < 5) continue;
                        int id = BitConverter.ToInt32(data, 1);
                        if (data[0] == 'W') t.myId = id;
                        else if (data[0] == 'J') t.inbox.Enqueue(new NetEvent { Kind = NetEventKind.Joined, Peer = id });
                        else if (data[0] == 'L') t.inbox.Enqueue(new NetEvent { Kind = NetEventKind.Left, Peer = id });
                    }
                    t.Fail(t.myId == 0 ? "That game is full or has ended." : "The host left the game.");
                }
                catch (Exception) { t.Fail(t.myId == 0 ? "Couldn't connect to that game. Check the address, and that both are on the same home network." : "The connection to the game was lost."); }
            }) { IsBackground = true, Name = "Smash guest" }.Start();
            return t;
        }

        /// <summary>A game found on the home network.</summary>
        public sealed class Found { public string Name, Address; public int Version; public bool SameVersion { get { return Version == NetProtocol.Version; } } }

        /// <summary>Asks the home network who is hosting (waits up to timeoutMs). Never throws.</summary>
        public static List<Found> Look(int timeoutMs, bool loopbackOnly = false)
        {
            var found = new List<Found>();
            try
            {
                using (var u = new UdpClient(0) { EnableBroadcast = true })
                {
                    byte[] q = Encoding.UTF8.GetBytes(Ask);
                    if (loopbackOnly) u.Send(q, q.Length, new IPEndPoint(IPAddress.Loopback, DiscoveryPort));
                    else foreach (IPAddress b in Broadcasts()) u.Send(q, q.Length, new IPEndPoint(b, DiscoveryPort));
                    var until = DateTime.UtcNow.AddMilliseconds(timeoutMs);
                    while (DateTime.UtcNow < until)
                    {
                        if (u.Available == 0) { Thread.Sleep(20); continue; }
                        var from = new IPEndPoint(IPAddress.Any, 0);
                        string[] p = Encoding.UTF8.GetString(u.Receive(ref from)).Split(new[] { '|' }, 4);
                        if (p.Length < 4 || p[0] != Answer) continue;
                        int.TryParse(p[1], out int v); int.TryParse(p[2], out int port);
                        string addr = from.Address + (port != FirstPort ? ":" + port : "");
                        if (!found.Any(f => f.Address == addr)) found.Add(new Found { Name = p[3], Address = addr, Version = v });
                    }
                }
            }
            catch (Exception) { }
            return found;
        }

        // ---------------------------------------------------------------- shared
        static byte[] Control(char kind, int id) { var b = new byte[5]; b[0] = (byte)kind; BitConverter.GetBytes(id).CopyTo(b, 1); return b; }

        static bool ReadFrame(NetworkStream s, out int peer, out byte[] data)
        {
            peer = 0; data = null;
            var head = new byte[8];
            if (!ReadAll(s, head, 4)) return false;
            int len = BitConverter.ToInt32(head, 0);
            if (len < 4 || len > 1 << 20) return false;
            var body = new byte[len];
            if (!ReadAll(s, body, len)) return false;
            peer = BitConverter.ToInt32(body, 0);
            data = new byte[len - 4];
            Buffer.BlockCopy(body, 4, data, 0, data.Length);
            return true;
        }

        static bool ReadAll(Stream s, byte[] b, int n)
        {
            int got = 0;
            while (got < n) { int r = s.Read(b, got, n - got); if (r <= 0) return false; got += r; }
            return true;
        }

        public void Send(int to, byte[] data)
        {
            if (closed || myId == 0) return;
            if (myId == 1)
            {
                if (to == 0) { int[] ids; lock (conns) ids = conns.Keys.ToArray(); foreach (int o in ids) Forward(o, 1, data); }
                else Forward(to, 1, data);
                return;
            }
            try { toHost.Write(to, data); } catch (Exception) { Fail("The connection to the game was lost."); }
        }

        public bool Poll(out NetEvent e) { return inbox.TryDequeue(out e); }

        void Fail(string reason)
        {
            if (closed) return;
            closed = true;
            inbox.Enqueue(new NetEvent { Kind = NetEventKind.Closed, Reason = reason });
        }

        public void Close()
        {
            closed = true;
            try { if (listener != null) listener.Stop(); } catch (Exception) { }
            try { if (answerer != null) answerer.Close(); } catch (Exception) { }
            if (holding) { holding = false; try { HoldBroadcasts(false); } catch (Exception) { } }
            lock (conns) { foreach (Conn c in conns.Values) try { c.Tcp.Close(); } catch (Exception) { } conns.Clear(); }
            try { if (toHost != null) toHost.Tcp.Close(); } catch (Exception) { }
        }

        // ---------------------------------------------------------------- addresses
        /// <summary>Home networks only (and this device): 10.x, 172.16-31.x, 192.168.x, 169.254.x, 127.x, IPv6 local.</summary>
        public static bool Private(IPAddress a)
        {
            if (a.IsIPv4MappedToIPv6) a = a.MapToIPv4();
            if (IPAddress.IsLoopback(a)) return true;
            if (a.AddressFamily == AddressFamily.InterNetworkV6) return a.IsIPv6LinkLocal || a.IsIPv6SiteLocal || (a.GetAddressBytes()[0] & 0xFE) == 0xFC;
            byte[] b = a.GetAddressBytes();
            return b[0] == 10 || (b[0] == 172 && b[1] >= 16 && b[1] <= 31) || (b[0] == 192 && b[1] == 168) || (b[0] == 169 && b[1] == 254);
        }

        static IEnumerable<UnicastIPAddressInformation> Ups()
        {
            NetworkInterface[] all;
            try { all = NetworkInterface.GetAllNetworkInterfaces(); } catch (Exception) { yield break; }
            foreach (NetworkInterface n in all)
            {
                if (n.OperationalStatus != OperationalStatus.Up || n.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                IPInterfaceProperties props; try { props = n.GetIPProperties(); } catch (Exception) { continue; }
                foreach (UnicastIPAddressInformation u in props.UnicastAddresses)
                    if (u.Address.AddressFamily == AddressFamily.InterNetwork && Private(u.Address)) yield return u;
            }
        }

        /// <summary>This device's home-network address (shown in the host's lobby, for typing on another device).</summary>
        public static string LocalAddress() { var u = Ups().FirstOrDefault(); return u != null ? u.Address.ToString() : null; }

        static List<IPAddress> Broadcasts()
        {
            var list = new List<IPAddress> { IPAddress.Broadcast };
            foreach (UnicastIPAddressInformation u in Ups())
            {
                try
                {
                    byte[] a = u.Address.GetAddressBytes(), m = u.IPv4Mask.GetAddressBytes();
                    for (int i = 0; i < 4; i++) a[i] = (byte)(a[i] | ~m[i]);
                    var b = new IPAddress(a);
                    if (!list.Contains(b)) list.Add(b);
                }
                catch (Exception) { }
            }
            return list;
        }
    }
}
