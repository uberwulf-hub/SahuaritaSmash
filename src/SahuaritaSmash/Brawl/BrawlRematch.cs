// Sahuarita Smash! (Sauce Edition): after an online match's results. The host gets two big buttons: Rematch (the same
// fighters on the same stage with a new seed, straight into the countdown on every device: NetSession.Rematch) and
// Back to the lobby (where people can swap fighters). The others see "Waiting for the host: Rematch or Lobby" and a
// Leave button of their own. Each match of a room has a number that its messages carry (NetMatchSetup.Round), so a
// late message from the match before is never mixed into the rematch.
using System;
using Avalonia;
using Avalonia.Input;

namespace ClassicGames
{
    public sealed partial class BrawlGame
    {
        /// <summary>Keys on the online results: the host R rematch, Enter the lobby; anyone Q leaves.</summary>
        void OnlineResultsKey(Key k)
        {
            if (Online == null) return;
            if (k == Key.Q) { Audio.Ui("back"); LeaveOnline("You left the game."); return; }
            if (!Online.IsHost) return;
            if (k == Key.R && Online.CanRematch) { Audio.Ui("select"); Online.Rematch((uint)Rng.Next(1, int.MaxValue)); }
            else if (k == Key.Enter || k == Key.Space) { Audio.Ui("select"); Online.ToLobby(); }
        }

        /// <summary>The buttons under the online results (each 80 game units tall: 44+ pixels on a phone).</summary>
        void DrawOnlineResultsButtons(DrawingContext dc)
        {
            if (Online == null) return;
            double y = Height - BigButton - 14;
            if (Online.IsHost)
            {
                bool again = Online.CanRematch;
                var rm = new Rect(Width / 2 - 160, y, 340, BigButton);
                var lb = new Rect(Width / 2 + 200, y, 400, BigButton);
                ResultsButton(dc, rm, again ? "Rematch" : "Rematch (not enough players)", TouchMode ? null : "R", again ? 0xFF059669 : 0xFF1A1E27, again ? 0xFF6EE7B7 : 0xFF353C4B);
                if (again) AddKeyHit(rm, "Rematch", Key.R);
                ResultsButton(dc, lb, "Back to the lobby", TouchMode ? null : "Enter", 0xFF1E3A8A, 0xFF60A5FA);
                AddKeyHit(lb, "Back to the lobby", Key.Enter);
                return;
            }
            Gfx.Text(dc, "Waiting for the host: Rematch or Lobby", Width / 2 + 160, y - 34, 20, 0xFF93C5FD, 0.5, Gfx.UiBold);
            var lv = new Rect(Width / 2 + 20, y, 280, BigButton);
            ResultsButton(dc, lv, "Leave", TouchMode ? null : "Q", 0xFF3A2430, 0xFFFCA5A5);
            AddKeyHit(lv, "Leave", Key.Q);
        }

        void ResultsButton(DrawingContext dc, Rect r, string label, string key, uint fill, uint edge)
        {
            bool lit = Hovered(r);
            dc.DrawRoundedRectangle(Gfx.B(lit ? Gfx.Mix(fill, 0xFFFFFFFF, 0.15) : fill), Gfx.P(edge, lit ? 3 : 2), r, 22, 22);
            Gfx.Text(dc, label, r.X + r.Width / 2 - (key != null ? 14 : 0), r.Y + BigButton / 2 - 15, 24, 0xFFFFFFFF, 0.5, Gfx.UiBold);
            if (key != null) Gfx.Text(dc, key, r.Right - 20, r.Y + BigButton / 2 - 10, 16, 0xFFBFDBFE, 1, Gfx.Ui);
        }

        /// <summary>The checks: show the results now (as when the match ends), without playing it out.</summary>
        internal void ShowResultsForTest() { if (Sim != null) { Step = Stage.Results; stageT = 1; resultsDone = true; } }
        /// <summary>The checks: play in this room (a pretend network), as hosting or joining does.</summary>
        internal void GoOnlineForTest(NetSession s) { Go(s); }
    }
}
