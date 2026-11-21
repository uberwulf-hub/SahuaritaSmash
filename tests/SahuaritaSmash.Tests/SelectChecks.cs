// The fighter select screen (group "select", Test 003 round 2): every tappable thing at least 78 x 78 picture units;
// all four seats, the stage strip, the colour chips, Fight! and the 12 fighters are there; the keys do what they did
// (Up / Down rows, Left / Right change, C colour); taps change the same things the old arrows did; a tapped fighter goes
// to the seat with the gold outline.
using System;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Input;
using SahuaritaSmash.Services;

namespace ClassicGames
{
    public static partial class Tests
    {
        public static void SelectChecks()
        {
            Audio.MusicOn = false;
            string dir = Path.Combine(Data.Dir, "select-checks");
            Directory.CreateDirectory(dir);
            var g = new BrawlGame { Scores = new ScoreBook(Path.Combine(dir, "scores.txt")), Player = "Kim", Rng = new Random(5) };
            g.KeysFileOverride = Path.Combine(dir, "keys.txt");
            g.UseUnlocksInMemory();
            g.SnapshotScene("Select", 0, 1);
            Func<System.Collections.Generic.List<HitArea>> draw = () =>
            {
                using (var dv = new DrawingVisual((int)g.Width, (int)g.Height))
                using (DrawingContext dc = dv.RenderOpen()) SahuaritaSmash.Views.GameView.RenderGame(g, dc);
                return g.MouseHits.ToList();
            };
            Func<string, bool> tap = label =>
            {
                var h = draw().LastOrDefault(x => x.Label == label);
                if (h == null) return false;
                var c = new Point(h.Area.X + h.Area.Width / 2, h.Area.Y + h.Area.Height / 2);
                g.PointerMove(c); g.PointerDown(c, false); g.PointerUp(c, false);
                return true;
            };
            var hits = draw();
            var small = hits.Where(h => h.Area.Width < 78 || h.Area.Height < 78).ToList();
            Check("select: every tappable thing is at least 78 x 78", small.Count == 0, string.Join("; ", small.Select(h => h.Label + " " + h.Area)));
            bool all = Enumerable.Range(1, 4).All(s => hits.Any(h => h.Label == "Player " + s + " card"))
                && BrawlData.Fighters.All(d => hits.Any(h => h.Label == d.Name))
                && hits.Any(h => h.Label == "Fight") && hits.Any(h => h.Label == "Stage <") && hits.Any(h => h.Label == "Rules >")
                && hits.Any(h => h.Label.StartsWith("Stage surprises ")) && hits.Count(h => h.Label.StartsWith("Colour ")) == BrawlUnlocks.Colours
                && hits.Count(h => h.Label == "played by <") == 4;
            Check("select: four seats, the stage strip, colours, Fight! and every fighter", all, string.Join(", ", hits.Select(h => h.Label)));
            Check("select: everything is on the screen", hits.All(h => h.Area.X >= 0 && h.Area.Y >= 0 && h.Area.Right <= g.Width && h.Area.Bottom <= g.Height), "");
            // every name on its tile at 18 or more: on one line at 20, or split into two lines that fit at 18
            var tooSmall = BrawlData.Fighters.Where(d =>
            {
                if (Gfx.Width(d.Name, 20, Gfx.UiBold) <= 104) return false;
                string[] w = d.Name.Split(' ');
                for (int k = 1; k < w.Length; k++)
                    if (Math.Max(Gfx.Width(string.Join(" ", w, 0, k), BrawlGame.TileNameMin, Gfx.UiBold), Gfx.Width(string.Join(" ", w, k, w.Length - k), BrawlGame.TileNameMin, Gfx.UiBold)) <= 106) return false;
                return true;
            }).Select(d => d.Name).ToList();
            Check("select: every fighter's tile name is 18 units or more (two lines when long)", BrawlGame.TileNameMin >= 18 && tooSmall.Count == 0, string.Join(", ", tooSmall));
            // keys, as before
            g.Sel = 0; int p0 = g.Pick[0], n = BrawlData.Fighters.Count;
            g.Press(Key.Right); g.KeyUp(Key.Right);
            bool keyRight = g.Pick[0] == (p0 + 1 >= n ? -1 : p0 + 1);
            g.Press(Key.Down); g.KeyUp(Key.Down);
            bool keyDown = g.Sel == 1;
            int who = g.Who[0]; g.Press(Key.Right); g.KeyUp(Key.Right);
            bool keyWho = g.Who[0] != who || who == 2;
            g.Sel = 11; g.Press(Key.Up); g.KeyUp(Key.Up);
            bool keyUp = g.Sel == 10;
            Check("select: Up / Down choose a row and Left / Right change it, as before", keyRight && keyDown && keyWho && keyUp, keyRight + " " + keyDown + " " + keyWho + " " + keyUp);
            // taps
            g.Sel = 0; int p1 = g.Pick[1];
            tap("Player 2 <");
            bool tapArrow = g.Sel == 2 && g.Pick[1] == (p1 - 1 < -1 ? n - 1 : p1 - 1);
            g.Sel = 4;
            tap(BrawlData.Fighters[5].Name);
            bool tapTile = g.Pick[2] == 5;
            var p4 = draw().First(h => h.Label.StartsWith("Player 4 ") && h.Label != "Player 4 card" && !h.Label.EndsWith("<") && !h.Label.EndsWith(">"));
            tap(p4.Label);
            bool tapSeat = g.Sel == 6;
            tap(BrawlData.Fighters[3].Name);
            bool tapTile4 = g.Pick[3] == 3;
            bool surprises = g.Surprises;
            tap("Stage surprises " + (surprises ? "On" : "Off"));
            bool tapToggle = g.Surprises != surprises && g.Sel == 10;
            Check("select: the arrows change the row, a fighter goes to the seat with the gold outline, a seat's middle chooses that seat, the toggle flips",
                tapArrow && tapTile && tapSeat && tapTile4 && tapToggle, tapArrow + " " + tapTile + " " + tapSeat + " " + tapTile4 + " " + tapToggle);
        }
    }
}
