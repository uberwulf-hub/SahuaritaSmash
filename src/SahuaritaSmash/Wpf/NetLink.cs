// Network play isn't in Sahuarita Smash! (Sauce Edition) yet (docs/PLAN.md, "Later"). Game keeps App Hub's hooks for it
// (Game.Net, null here), so this is just enough of App Hub's NetLink (Engine\NetPlay.cs there) for them to compile.
using Avalonia.Input;

namespace ClassicGames
{
    public sealed class NetLink
    {
        public bool Lost, SharesRoundPanel, HostWaits;
        public object Round;
        public void BeforeUpdate(double dt) { }
        public void AfterUpdate(double dt) { }
        public void Draw(DrawingContext dc) { }
        public void DrawOver(DrawingContext dc) { }
        public void DrawRound(DrawingContext dc) { }
        public void DrawLost(DrawingContext dc) { }
        public bool RoundKey(Key k, bool repeat) { return false; }
        public bool Key(Key k, bool repeat) { return false; }
        public void Banner(string text) { }
    }
}
