// Network play's checks (no real network: LoopbackRoom pretends, with delay and jitter): the same math everywhere,
// the packets, three devices playing one match and staying exactly in step, someone leaving mid-match, and a device
// whose match drifts being caught.
using System;
using System.Collections.Generic;
using System.Linq;

namespace ClassicGames
{
    public static partial class Tests
    {
        public static void NetChecks()
        {
            // DMath: as accurate as the platform's, over every angle a match uses
            double worst = 0;
            for (double x = -50; x <= 50; x += 0.0137) worst = Math.Max(worst, Math.Max(Math.Abs(DMath.Sin(x) - Math.Sin(x)), Math.Abs(DMath.Cos(x) - Math.Cos(x))));
            Check("net: DMath's sine and cosine match the usual ones to 1e-14", worst < 1e-14, worst.ToString("E2"));

            // buttons survive the trip, sticks rounded the same way on every device
            var b = new BrInput { X = -0.42, Y = 1, Attack = true, Shield = true, Grab = true };
            BrInput back = NetProtocol.Unpack(NetProtocol.Pack(b));
            Check("net: buttons and stick survive the trip (the stick in 255 steps)", back.Attack && back.Shield && back.Grab && !back.Jump && Math.Abs(back.X + 0.42) < 0.005 && back.Y == 1 && NetProtocol.Unpack(NetProtocol.Pack(back)).X == back.X, back.X + " " + back.Y);

            var setup = new NetMatchSetup { Stage = 0, Lives = 3, Delay = 3, Seed = 1234 };
            setup.Seats.Add(new NetMatchSetup.Seat { Owner = 1, Fighter = 0, Name = "Kim" });
            setup.Seats.Add(new NetMatchSetup.Seat { Owner = 2, Fighter = 4, Name = "Ben" });
            setup.Seats.Add(new NetMatchSetup.Seat { Owner = 3, Fighter = 7, Name = "Sam" });
            setup.Seats.Add(new NetMatchSetup.Seat { Owner = 0, Fighter = 9, BotLevel = 6 });
            NetMatchSetup same = NetMatchSetup.FromPacket(new NetReader(setup.ToPacket()));
            Check("net: the start message carries the whole setup", same.Seats.Count == 4 && same.Seats[2].Name == "Sam" && same.Seats[3].Owner == 0 && same.Seed == 1234 && same.Delay == 3, "");
            bool refused = false;
            try { var bad = setup.ToPacket(); bad[1] = 99; NetMatchSetup.FromPacket(new NetReader(bad)); } catch (FormatException) { refused = true; }
            Check("net: a start message with a stage that doesn't exist is refused", refused, "");

            // three devices, one computer player, a laggy jittery network
            var r1 = Play(setup, 3000, 3, 4, -1, false);
            Check("net: three devices play 3000 frames and every device's match is exactly the same (checksums)", r1.Same && r1.Frame == 3000 && r1.Broken == null, r1.Info);
            Check("net: someone's buttons sometimes arrive late and the match waits for them, then catches up", r1.Stalls > 0, r1.Info);
            // the same match replayed on one computer from the same buttons gives the same result
            Check("net: replaying the same buttons on one computer gives the same match", r1.Replay == r1.Sum, r1.Info);

            // the third device leaves half-way: a computer player takes over its fighter, the others stay in step
            var r2 = Play(setup, 3000, 3, 4, 1500, false);
            Check("net: someone leaving mid-match: the computer plays their fighter and the others stay in step", r2.Same && r2.Frame == 3000 && r2.Broken == null, r2.Info);

            // a device whose match drifts is caught by the host's checksums and the match stops
            var r3 = Play(setup, 1200, 2, 2, -1, true);
            Check("net: a device whose match drifted is caught (the host stops the match with a message)", r3.Broken != null, r3.Info);
        }

        /// <summary>The lobby: hello, refusals, picks, the host's rules, a match on every device, back to the lobby. Over
        /// the pretend network, then the real home-network connection on this computer only (127.0.0.1).</summary>
        public static void NetSessionChecks()
        {
            var room = new LoopbackRoom(1, 1, 5);
            var host = new NetSession(room.Join(), false, "Kim", 0);
            var ben = new NetSession(room.Join(), false, "Kim", 4);
            Func<int, bool> pump = n => { for (int i = 0; i < n; i++) { room.Pump(); host.Poll(); ben.Poll(); } return true; };
            pump(10);
            Check("net lobby: a guest says hello and both see two players (a name already taken gets a number)", host.Players.Count == 2 && ben.Players.Count == 2 && ben.State == NetSession.Phase.Lobby && host.Players[1].Name == "Kim 2", string.Join(",", host.Players.Select(p => p.Name)));
            ben.Pick(7); host.Stage = 2; host.Lives = 2; host.Cpus = 1; host.CpuLevel = 6; host.SendLobby();
            pump(10);
            Check("net lobby: picks and the host's rules reach everyone", ben.Players.First(p => p.Peer == 2).Fighter == 7 && ben.Stage == 2 && ben.Lives == 2 && ben.Cpus == 1, "");
            int started = 0; ben.MatchStarted += () => started++;
            host.StartMatch(4242);
            pump(10);
            Check("net lobby: the host starts and both devices have the same match (2 people, 1 computer)", started == 1 && ben.Match != null && ben.Match.Setup.Seats.Count == 3 && ben.Match.Setup.Seed == 4242 && ben.Match.Sim.Stage.Id == host.Match.Sim.Stage.Id, "");
            for (int t = 0; t < 900; t++)
            {
                room.Pump(); host.Poll(); ben.Poll();
                foreach (var s in new[] { host, ben }) { foreach (int seat in s.Match.MySeats()) s.Match.Local(seat, new BrInput { X = t % 120 < 60 ? 1 : -1, Attack = t % 17 == 0 }); for (int k = 0; k < 2 && s.Match.Sim.Frame < 600; k++) if (!s.Match.TryStep()) break; }
            }
            Check("net lobby: the match runs the same on both", host.Match.Sim.Frame == 600 && ben.Match.Sim.Frame == 600 && host.Match.Sim.Checksum() == ben.Match.Sim.Checksum(), host.Match.Sim.Frame + " " + ben.Match.Sim.Frame);
            int back = 0; ben.BackToLobby += () => back++;
            host.ToLobby(); pump(10);
            Check("net lobby: the host sends everyone back to the lobby", back == 1 && ben.State == NetSession.Phase.Lobby && ben.Match == null, "");
            var other = new NetSession(room.Join(), false, "Eve", 0);
            // a device of another game says hello: refused with words
            other.Net.Send(1, new NetWriter(NetProtocol.Hello).S("RaceTrackMayhem").I(1).S("Eve").I(0).Done());
            for (int i = 0; i < 10; i++) { room.Pump(); host.Poll(); ben.Poll(); other.Net.Poll(out NetEvent _); }
            var oldie = new NetSession(room.Join(), false, "Old", 0);
            oldie.Net.Send(1, new NetWriter(NetProtocol.Hello).S(NetProtocol.Game).I(NetProtocol.Version - 1).S("Old").I(0).Done());
            string refusal = null;
            for (int i = 0; i < 10 && refusal == null; i++)
            {
                room.Pump(); host.Poll();
                while (oldie.Net.Poll(out NetEvent e)) if (e.Kind == NetEventKind.Data && e.Data[0] == NetProtocol.Refuse) refusal = new NetReader(e.Data).S();
            }
            Check("net lobby: another game or an older version is refused, in plain words", host.Players.Count == 2 && refusal != null && refusal.Contains("older"), refusal ?? "none");
            ben.Leave(); pump(5);
            Check("net lobby: someone leaving is taken off the host's list", host.Players.Count == 1, host.Players.Count.ToString());
            host.Leave();

            // the real home-network connection, on this computer only
            var lan = LanTransport.Host("Kim's PC", true);
            var lh = new NetSession(lan, false, "Kim", 0);
            var found = LanTransport.Look(300, true);
            var lg = new NetSession(LanTransport.Join("127.0.0.1:" + lan.Port), false, "Ben", 3);
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < 4000 && (lh.Players.Count < 2 || lg.State != NetSession.Phase.Lobby)) { lh.Poll(); lg.Poll(); System.Threading.Thread.Sleep(10); }
            Check("net home network: a guest joins the host over TCP (on this computer)", lh.Players.Count == 2 && lg.State == NetSession.Phase.Lobby && lg.Players.Count == 2, lh.Players.Count + " " + lg.State + " " + lg.Reason);
            lh.Cpus = 0; lh.StartMatch(99);
            sw.Restart();
            while (sw.ElapsedMilliseconds < 4000 && lg.Match == null) { lh.Poll(); lg.Poll(); System.Threading.Thread.Sleep(5); }
            while (sw.ElapsedMilliseconds < 15000 && lg.Match != null && (lh.Match.Sim.Frame < 300 || lg.Match.Sim.Frame < 300))
            {
                lh.Poll(); lg.Poll();
                foreach (var s in new[] { lh, lg }) { foreach (int seat in s.Match.MySeats()) s.Match.Local(seat, new BrInput { X = 1, Attack = s.Match.Sim.Frame % 23 == 0 }); for (int k = 0; k < 2 && s.Match.Sim.Frame < 300; k++) if (!s.Match.TryStep()) break; }
                System.Threading.Thread.Sleep(1);
            }
            Check("net home network: a match over TCP stays the same on both", lg.Match != null && lh.Match.Sim.Frame == 300 && lg.Match.Sim.Frame == 300 && lh.Match.Sim.Checksum() == lg.Match.Sim.Checksum(), (lg.Match == null ? "no match" : lh.Match.Sim.Frame + " " + lg.Match.Sim.Frame));
            lh.Leave();
            sw.Restart();
            while (sw.ElapsedMilliseconds < 3000 && lg.State != NetSession.Phase.Closed) { lg.Poll(); System.Threading.Thread.Sleep(10); }
            Check("net home network: when the host leaves, the guest is told", lg.State == NetSession.Phase.Closed && lg.Reason != null, lg.Reason ?? "");
            Check("net home network: asking who's hosting finds the host, with its name, port and version", found.Count == 1 && found[0].Name == "Kim's PC" && found[0].SameVersion && found[0].Address.EndsWith(lan.Port == LanTransport.FirstPort ? "127.0.0.1" : ":" + lan.Port), found.Count + " found " + string.Join(",", found.Select(f => f.Name + "@" + f.Address)));
            lg.Leave();
        }

        /// <summary>The internet connection, against Race Track Mayhem's relay running on this computer (its local.mjs, the
        /// same room rules as the Cloudflare one). Only when SMASH_RELAY_URL is set (e.g. ws://127.0.0.1:24690).</summary>
        public static void NetRelayChecks()
        {
            string url = Environment.GetEnvironmentVariable("SMASH_RELAY_URL");
            if (string.IsNullOrEmpty(url)) { Console.WriteLine("   (skipped: set SMASH_RELAY_URL to a relay on this computer)"); return; }
            var host = new NetSession(RelayTransport.Host(url), true, "Kim", 0);
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < 5000 && (host.State != NetSession.Phase.Lobby || host.Net.Address.Length != 4)) { host.Poll(); System.Threading.Thread.Sleep(10); }
            string code = host.Net.Address;
            Check("net relay: hosting gets a 4-letter code", code.Length == 4 && host.State == NetSession.Phase.Lobby, code + " " + host.State + " " + host.Reason);
            var guest = new NetSession(RelayTransport.Join(url, code.ToLowerInvariant()), true, "Ben", 5);
            sw.Restart();
            while (sw.ElapsedMilliseconds < 5000 && (host.Players.Count < 2 || guest.State != NetSession.Phase.Lobby)) { host.Poll(); guest.Poll(); System.Threading.Thread.Sleep(10); }
            Check("net relay: a guest joins with the code (any case) and both see the lobby", host.Players.Count == 2 && guest.Players.Count == 2, host.Players.Count + " " + guest.State + " " + guest.Reason);
            host.Cpus = 1; host.StartMatch(7);
            sw.Restart();
            while (sw.ElapsedMilliseconds < 5000 && guest.Match == null) { host.Poll(); guest.Poll(); System.Threading.Thread.Sleep(5); }
            while (sw.ElapsedMilliseconds < 20000 && guest.Match != null && (host.Match.Sim.Frame < 400 || guest.Match.Sim.Frame < 400))
            {
                host.Poll(); guest.Poll();
                foreach (var s in new[] { host, guest }) { foreach (int seat in s.Match.MySeats()) s.Match.Local(seat, new BrInput { X = -1, Special = s.Match.Sim.Frame % 41 == 0 }); for (int k = 0; k < 2 && s.Match.Sim.Frame < 400; k++) if (!s.Match.TryStep()) break; }
                System.Threading.Thread.Sleep(1);
            }
            Check("net relay: a match through the relay stays the same on both", guest.Match != null && host.Match.Sim.Frame == 400 && guest.Match.Sim.Frame == 400 && host.Match.Sim.Checksum() == guest.Match.Sim.Checksum(), guest.Match == null ? "no match" : host.Match.Sim.Frame + " " + guest.Match.Sim.Frame);
            var lost = new NetSession(RelayTransport.Join(url, "ZZZZ"), true, "Eve", 0);
            sw.Restart();
            while (sw.ElapsedMilliseconds < 5000 && lost.State != NetSession.Phase.Closed) { lost.Poll(); System.Threading.Thread.Sleep(10); }
            Check("net relay: a code with no game says so, in this game's words", lost.State == NetSession.Phase.Closed && lost.Reason != null && lost.Reason.Contains("game") && !lost.Reason.Contains("race"), lost.Reason ?? "");
            host.Leave();
            sw.Restart();
            while (sw.ElapsedMilliseconds < 5000 && guest.State != NetSession.Phase.Closed) { guest.Poll(); System.Threading.Thread.Sleep(10); }
            Check("net relay: when the host leaves, the guest is told", guest.State == NetSession.Phase.Closed, guest.Reason ?? "");
        }

        sealed class PlayResult { public bool Same; public int Frame, Stalls; public long Sum, Replay; public string Broken, Info; }

        static PlayResult Play(NetMatchSetup setup, int target, int delay, int jitter, int leaveAt, bool drift)
        {
            var room = new LoopbackRoom(delay, jitter, 77);
            var peers = new[] { room.Join(), room.Join(), room.Join() };
            var matches = new NetMatch[3];
            for (int d = 0; d < 3; d++)
            {
                var p = peers[d];
                matches[d] = new NetMatch(NetMatchSetup.FromPacket(new NetReader(setup.ToPacket())), p.MyId) { SendAll = data => p.Send(0, data) };
            }
            var record = new List<int[]>();
            matches[0].Stepped = (f, ins) => record.Add(ins);
            var rngs = new[] { new Random(1), new Random(2), new Random(3) };
            var held = new BrInput[3];
            bool gone = false;
            for (int t = 0; t < target * 3; t++)
            {
                room.Pump();
                if (t == leaveAt) { peers[2].Close(); gone = true; }
                if (drift && t == 400) matches[1].Sim.F[0].Dmg += 1;   // a pretend bug on device 2
                bool all = true;
                for (int d = 0; d < 3; d++)
                {
                    if (gone && d == 2) continue;
                    NetMatch m = matches[d];
                    NetEvent e;
                    while (peers[d].Poll(out e))
                    {
                        if (e.Kind == NetEventKind.Data) { try { m.Receive(e.Peer, new NetReader(e.Data)); } catch (Exception) { } }
                        else if (e.Kind == NetEventKind.Left) m.PeerLeft(e.Peer);
                    }
                    // each person presses something new now and then
                    if (rngs[d].Next(6) == 0)
                    {
                        Random r = rngs[d];
                        held[d] = new BrInput { X = r.Next(3) - 1, Y = r.Next(5) == 0 ? 1 : 0, Attack = r.Next(4) == 0, Special = r.Next(9) == 0, Shield = r.Next(12) == 0, Jump = r.Next(7) == 0 };
                    }
                    foreach (int seat in m.MySeats()) m.Local(seat, held[d]);
                    // up to two frames a tick, as the game does to catch up; never past the target
                    for (int k = 0; k < 2 && m.Sim.Frame < target; k++) if (!m.TryStep()) break;
                    if (m.Sim.Frame < target) all = false;
                }
                if (all || matches[0].Broken != null) break;
            }
            var live = gone ? matches.Take(2).ToArray() : matches;
            var sums = live.Select(m => m.Sim.Frame == target ? m.Sim.Checksum() : -1).ToArray();
            // the same buttons replayed on one computer, computer players worked out again
            var sim = setup.Build();
            var bots = setup.Seats.Select((s, i) => s.Owner == 0 ? new BrawlBot(i, s.BotLevel) : null).ToArray();
            var ins = new BrInput[setup.Seats.Count];
            foreach (int[] rec in record)
            {
                for (int i = 0; i < ins.Length; i++)
                {
                    if (rec[i] == -1) { if (bots[i] == null) bots[i] = new BrawlBot(i, 5); ins[i] = bots[i].Think(sim); }
                    else ins[i] = NetProtocol.Unpack(rec[i]);
                }
                sim.Step(ins);
            }
            var res = new PlayResult
            {
                Frame = live.Min(m => m.Sim.Frame), Sum = sums[0], Same = sums.All(x => x == sums[0] && x != -1), Stalls = live.Sum(m => m.Stalls),
                Broken = matches[0].Broken, Replay = sim.Frame == target ? sim.Checksum() : -2,
            };
            res.Info = "frame " + res.Frame + ", stalls " + res.Stalls + ", checksums " + string.Join(" ", sums) + ", replay " + res.Replay + (res.Broken != null ? ", stopped" : "");
            return res;
        }
    }
}
