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
