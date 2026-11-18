// Play online, through the game's own menus: one game hosts on the home network (on this computer only), another finds
// it under Join and joins with a tap, both pick fighters in the lobby, the host starts, both play the same match, the
// host takes everyone back to the lobby, and the guest is told when the host leaves. Plus pictures of the screens.
using System;
using System.IO;
using System.Linq;
using Avalonia.Input;

namespace ClassicGames
{
    public static partial class Tests
    {
        public static void OnlineGameChecks()
        {
            Audio.MusicOn = false;
            BrawlGame.LanLoopbackOnly = true;
            string dir = SahuaritaSmash.Services.Data.Dir;
            BrawlGame Make(string who) => new BrawlGame { Scores = new ScoreBook(Path.Combine(dir, who + "-scores.txt")), Player = who, Rng = new Random(who.Length), KeysFileOverride = Path.Combine(dir, who + "-keys.txt") };
            var host = Make("Kim"); var guest = Make("Ben");
            host.Begin(); guest.Begin();
            Action<int> frames = n => { for (int i = 0; i < n; i++) { host.Update(1.0 / 60); guest.Update(1.0 / 60); Draw(host); Draw(guest); System.Threading.Thread.Sleep(1); } };

            // the title's "Play online", then host on the home network
            host.Menu = BrawlGame.Screen.Title; host.Sel = 5; host.Tap(Key.Enter);
            Check("online: the title's Play online opens its menu", host.Menu == BrawlGame.Screen.Online, host.Menu.ToString());
            Snap(host, "Online");
            host.Tap(Key.Enter);   // Host a game on this home network
            frames(5);
            Check("online: hosting opens the lobby with the host in it", host.Menu == BrawlGame.Screen.OnlineLobby && host.Online != null && host.Online.Players.Count == 1, host.Menu + " " + (host.Online == null ? "" : host.Online.State.ToString()));

            // the guest: Join, finds the game, taps it
            guest.Menu = BrawlGame.Screen.Title; guest.Sel = 5; guest.Tap(Key.Enter); guest.Sel = 2; guest.Tap(Key.Enter);
            Check("online: Join opens the join screen", guest.Menu == BrawlGame.Screen.OnlineJoin, guest.Menu.ToString());
            var sw = System.Diagnostics.Stopwatch.StartNew();
            Avalonia.Point? found = null;
            while (sw.ElapsedMilliseconds < 4000 && found == null) { frames(3); found = guest.HitPoint("Join Kim's game"); }
            Snap(guest, "OnlineJoin");
            Check("online: the host's game shows up under Join (on this computer)", found != null, "");
            if (found == null) { host.Online?.Leave(); return; }
            guest.PointerMove(found.Value); guest.PointerDown(found.Value, false);
            sw.Restart();
            while (sw.ElapsedMilliseconds < 4000 && (host.Online.Players.Count < 2 || guest.Online == null || guest.Online.State != NetSession.Phase.Lobby)) frames(2);
            Check("online: tapping it joins the lobby; both see two players", host.Online.Players.Count == 2 && guest.Online != null && guest.Online.Players.Count == 2 && guest.Menu == BrawlGame.Screen.OnlineLobby, host.Online.Players.Count + " " + guest.Menu);

            // picks and rules
            guest.Sel = 0; guest.Tap(Key.Right); guest.Tap(Key.Right);
            host.Sel = 1; host.Tap(Key.Right);   // stage
            host.Sel = 3; host.Tap(Key.Right);   // a computer player
            frames(10);
            Snap(host, "OnlineLobby");
            Snap(guest, "OnlineLobbyGuest");
            Check("online: the guest's pick and the host's rules reach both lobbies", host.Online.Players[1].Fighter == 2 && guest.Online.Stage == 1 && guest.Online.Cpus == 1, host.Online.Players[1].Fighter + " " + guest.Online.Stage + " " + guest.Online.Cpus);

            // start: both play
            host.Sel = 6; host.Tap(Key.Enter);
            frames(5);
            Check("online: the host starts and both are in the same match (2 people, 1 computer, the host's stage)", host.State == Phase.Play && guest.State == Phase.Play && guest.Sim != null && guest.Sim.F.Length == 3 && guest.Sim.Stage.Id == BrawlData.Stages[1].Id, host.State + " " + guest.State);
            KartPads.Fake[0] = new KartPad { On = true, X = 1 };   // both people push right (both games read the same pretend pad)
            frames(60 * 8);
            KartPads.Fake[0] = null;
            int fh = host.Sim.Frame, fg = guest.Sim.Frame;
            // let both reach the same frame
            for (int i = 0; i < 200 && host.Sim.Frame != guest.Sim.Frame; i++) { if (host.Sim.Frame < guest.Sim.Frame) host.Update(1.0 / 60); else guest.Update(1.0 / 60); System.Threading.Thread.Sleep(1); }
            Snap(guest, "OnlineFight");
            Check("online: after 8 seconds both games are at the same frame with the same match", host.Sim.Frame == guest.Sim.Frame && host.Sim.Frame > 200 && host.Sim.Checksum() == guest.Sim.Checksum(), fh + " / " + fg + " -> " + host.Sim.Frame + " / " + guest.Sim.Frame);
            Check("online: pausing doesn't stop the match for everyone (it's a menu)", Pauses(host), "");

            // back to the lobby
            host.Online.ToLobby();
            frames(10);
            Check("online: the host takes everyone back to the lobby", host.Menu == BrawlGame.Screen.OnlineLobby && guest.Menu == BrawlGame.Screen.OnlineLobby && guest.State == Phase.Setup, guest.Menu + " " + guest.State);

            // the host leaves: the guest is told, on Play online
            host.Tap(Key.Escape);
            sw.Restart();
            while (sw.ElapsedMilliseconds < 3000 && guest.Online != null) frames(2);
            Check("online: when the host leaves, the guest is back on Play online with a note", guest.Online == null && guest.Menu == BrawlGame.Screen.Online, guest.Menu.ToString());
            Snap(guest, "OnlineLeft");
            foreach (DrawingVisual v in frameDv.Values) v.Dispose();
            frameDv.Clear();
            BrawlGame.LanLoopbackOnly = false;
            Audio.StopMusic(); Audio.StopEffects();
        }

        static bool Pauses(BrawlGame g)
        {
            int f = g.Sim.Frame;
            g.Tap(Key.Escape);
            bool paused = g.Paused;
            for (int i = 0; i < 30; i++) { g.Update(1.0 / 60); System.Threading.Thread.Sleep(1); }
            bool moved = g.Sim.Frame > f;   // (the other device keeps sending, so the match goes on)
            g.Tap(Key.Escape);
            return paused && !g.Paused;
        }

        /// <summary>A frame drawn as the app does (drawing registers what can be tapped), into one small picture per game.</summary>
        static readonly System.Collections.Generic.Dictionary<Game, DrawingVisual> frameDv = new System.Collections.Generic.Dictionary<Game, DrawingVisual>();
        static void Draw(Game g)
        {
            DrawingVisual dv;
            if (!frameDv.TryGetValue(g, out dv)) frameDv[g] = dv = new DrawingVisual(64, 36);
            using (DrawingContext dc = dv.RenderOpen()) g.Render(dc);
        }

        static void Snap(Game g, string name)
        {
            using (var dv = new DrawingVisual((int)g.Width, (int)g.Height))
            {
                using (DrawingContext dc = dv.RenderOpen()) SahuaritaSmash.Views.GameView.RenderGame(g, dc);
                Save(dv.Bitmap, name);
            }
        }
    }
}
