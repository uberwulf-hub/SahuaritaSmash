// Network play over the internet: through the relay server Race Track Mayhem uses (server/relay in that repository,
// on Cloudflare; it passes messages along inside "rooms" without looking at them, so both games share it; this
// game's Hello tells them apart). The host gets a 4-letter code; the others join with it. Everyone connects out, so
// no router or firewall changes are needed.
//   wss://<relay>/host?v=1   wss://<relay>/join/CODE?v=1
//   text:   {"t":"welcome","id":N,"code":"ABCD","peers":[..]}  {"t":"join","id":N}  {"t":"leave","id":N}  {"t":"error","msg":".."}
//   binary: 4-byte little-endian peer id (sending: who it's for, 0 = everyone else; arriving: who it's from),
//           1 channel byte (0 here), then the packet.
using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ClassicGames
{
    public sealed class RelayTransport : INetTransport
    {
        /// <summary>The relay Race Track Mayhem uses (Andrew's, on Cloudflare's free plan).</summary>
        public const string DefaultRelay = "wss://race-track-mayhem-relay.andrewsaulls.workers.dev";
        public const int RelayProtocol = 1;

        readonly ClientWebSocket ws = new ClientWebSocket();
        readonly ConcurrentQueue<NetEvent> inbox = new ConcurrentQueue<NetEvent>();
        readonly BlockingCollection<byte[]> outbox = new BlockingCollection<byte[]>();
        readonly CancellationTokenSource stop = new CancellationTokenSource();
        volatile int myId;
        volatile string code = "";
        volatile bool closed;

        public int MyId { get { return myId; } }
        public bool IsHost { get { return myId == 1; } }
        public string Address { get { return code; } }

        RelayTransport() { }

        /// <summary>Opens a new room (this device hosts). Connects in the background; Poll gives Closed if it fails.</summary>
        public static RelayTransport Host(string relay) { var t = new RelayTransport(); t.Start((relay ?? DefaultRelay) + "/host?v=" + RelayProtocol); return t; }
        /// <summary>Joins the room with this code.</summary>
        public static RelayTransport Join(string relay, string roomCode)
        {
            var t = new RelayTransport();
            t.Start((relay ?? DefaultRelay) + "/join/" + Uri.EscapeDataString((roomCode ?? "").Trim().ToUpperInvariant()) + "?v=" + RelayProtocol);
            return t;
        }

        void Start(string url)
        {
            ws.Options.KeepAliveInterval = TimeSpan.FromSeconds(15);
            Task.Run(async () =>
            {
                try
                {
                    using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(12)))
                    using (var both = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token, stop.Token))
                        await ws.ConnectAsync(new Uri(url), both.Token);
                }
                catch (Exception) { Fail("Couldn't reach the game server. Check the internet connection and try again."); return; }
                var sender = Task.Run(SendLoop);
                await ReceiveLoop();
            });
        }

        async Task SendLoop()
        {
            try
            {
                foreach (byte[] b in outbox.GetConsumingEnumerable(stop.Token))
                    await ws.SendAsync(new ArraySegment<byte>(b), WebSocketMessageType.Binary, true, stop.Token);
            }
            catch (Exception) { }
        }

        async Task ReceiveLoop()
        {
            var buf = new byte[64 * 1024];
            var msg = new System.IO.MemoryStream();
            try
            {
                while (!stop.IsCancellationRequested && ws.State == WebSocketState.Open)
                {
                    WebSocketReceiveResult r = await ws.ReceiveAsync(new ArraySegment<byte>(buf), stop.Token);
                    if (r.MessageType == WebSocketMessageType.Close)
                    {
                        Fail(Words(ws.CloseStatusDescription) ?? "The game ended.");
                        return;
                    }
                    msg.Write(buf, 0, r.Count);
                    if (msg.Length > 1 << 20) { Fail("The game server sent too much."); return; }
                    if (!r.EndOfMessage) continue;
                    byte[] data = msg.ToArray(); msg.SetLength(0);
                    if (r.MessageType == WebSocketMessageType.Text) Control(Encoding.UTF8.GetString(data));
                    else if (data.Length >= 5)
                    {
                        int from = BitConverter.ToInt32(data, 0);
                        var packet = new byte[data.Length - 5];
                        Buffer.BlockCopy(data, 5, packet, 0, packet.Length);
                        inbox.Enqueue(new NetEvent { Kind = NetEventKind.Data, Peer = from, Data = packet });
                    }
                }
            }
            catch (Exception) { }
            Fail("The connection to the game was lost.");
        }

        void Control(string text)
        {
            try
            {
                using (JsonDocument d = JsonDocument.Parse(text))
                {
                    JsonElement e = d.RootElement;
                    string t = e.TryGetProperty("t", out JsonElement te) ? te.GetString() : "";
                    switch (t)
                    {
                        case "welcome":
                            code = e.TryGetProperty("code", out JsonElement c) ? c.GetString() ?? "" : "";
                            myId = e.GetProperty("id").GetInt32();
                            if (e.TryGetProperty("peers", out JsonElement ps)) foreach (JsonElement p in ps.EnumerateArray()) inbox.Enqueue(new NetEvent { Kind = NetEventKind.Joined, Peer = p.GetInt32() });
                            break;
                        case "join": inbox.Enqueue(new NetEvent { Kind = NetEventKind.Joined, Peer = e.GetProperty("id").GetInt32() }); break;
                        case "leave": inbox.Enqueue(new NetEvent { Kind = NetEventKind.Left, Peer = e.GetProperty("id").GetInt32() }); break;
                        case "error": Fail(Words(e.TryGetProperty("msg", out JsonElement m) ? m.GetString() : null) ?? "The game server said no."); break;
                    }
                }
            }
            catch (Exception) { }
        }

        /// <summary>The relay talks about races (it's Race Track Mayhem's): say game instead.</summary>
        static string Words(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            return s.Replace("race", "game").Replace("Race", "Game");
        }

        void Fail(string reason)
        {
            if (closed) return;
            closed = true;
            inbox.Enqueue(new NetEvent { Kind = NetEventKind.Closed, Reason = reason });
        }

        public void Send(int to, byte[] data)
        {
            if (closed || myId == 0) return;
            var b = new byte[data.Length + 5];
            BitConverter.GetBytes(to).CopyTo(b, 0);
            Buffer.BlockCopy(data, 0, b, 5, data.Length);
            outbox.Add(b);
        }

        public bool Poll(out NetEvent e) { return inbox.TryDequeue(out e); }

        public void Close()
        {
            closed = true;
            try { ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None).Wait(500); } catch (Exception) { }
            stop.Cancel();
            try { ws.Dispose(); } catch (Exception) { }
        }
    }
}
