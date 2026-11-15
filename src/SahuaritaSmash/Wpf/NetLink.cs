// The game base's hooks for network play (App Hub's Game.Net): while a match is played online the game keeps going
// behind the pause menu, Q leaves the game (back to "Play online", not out of the app), a lost connection shows a
// panel, and a small banner says who's online and whom the match is waiting for. BrawlOnline.cs fills it in.
using System;
using Avalonia;
using Avalonia.Input;

namespace ClassicGames
{
    public sealed class NetLink
    {
        public bool Lost, SharesRoundPanel, HostWaits;
        public object Round;
        /// <summary>Why the connection was lost (shown on the panel).</summary>
        public string LostText = "";
        public Action Pump, Leave;
        /// <summary>Registers the lost panel's Back button with the game (so a tap works too).</summary>
        public Action<Rect> BackHit;
        /// <summary>The banner: what to say at the top while playing (null = nothing).</summary>
        public Func<string> Status;
        public double Width = 1280, Height = 720;
        string banner; double bannerT;

        public void BeforeUpdate(double dt) { if (Pump != null) Pump(); if (bannerT > 0) bannerT -= dt; }
        public void AfterUpdate(double dt) { }
        public void Draw(DrawingContext dc)
        {
            string s = Status != null ? Status() : null;
            if (bannerT > 0 && banner != null) s = banner;
            if (string.IsNullOrEmpty(s)) return;
            double w = Gfx.Width(s, 16, Gfx.UiBold) + 32;
            dc.DrawRoundedRectangle(Gfx.B(0xC0101522), Gfx.P(0x8060A5FA, 1), new Rect((Width - w) / 2, 8, w, 30), 15, 15);
            Gfx.Text(dc, s, Width / 2, 13, 16, 0xFFBFDBFE, 0.5, Gfx.UiBold);
        }
        public void DrawOver(DrawingContext dc) { }
        public void DrawRound(DrawingContext dc) { }
        public void DrawLost(DrawingContext dc)
        {
            dc.DrawRectangle(Gfx.B(0xA0000000), null, new Rect(0, 0, Width, Height));
            var r = new Rect(Width / 2 - 300, Height / 2 - 110, 600, 220);
            Gfx.Panel(dc, r);
            Gfx.Text(dc, "The online game ended", Width / 2, r.Y + 24, 30, 0xFFE6E9F0, 0.5, Gfx.UiBold);
            Gfx.Text(dc, LostText, Width / 2, r.Y + 84, 17, 0xFFB8BFCC, 0.5, Gfx.Ui);
            var b = new Rect(Width / 2 - 90, r.Y + 150, 180, 44);
            dc.DrawRoundedRectangle(Gfx.B(0xFF1E3A8A), Gfx.P(0xFF60A5FA, 2), b, 22, 22);
            Gfx.Text(dc, "Back", Width / 2, b.Y + 10, 19, 0xFFFFFFFF, 0.5, Gfx.UiBold);
            if (BackHit != null) BackHit(b);
        }
        public bool RoundKey(Key k, bool repeat) { return false; }
        /// <summary>Keys while online: on the lost panel, Enter / Esc goes back.</summary>
        public bool Key(Key k, bool repeat)
        {
            if (!Lost || repeat) return false;
            if ((k == Avalonia.Input.Key.Enter || k == Avalonia.Input.Key.Escape || k == Avalonia.Input.Key.Space) && Leave != null) Leave();
            return true;
        }
        public void Banner(string text) { banner = text; bannerT = 3; }
    }
}
