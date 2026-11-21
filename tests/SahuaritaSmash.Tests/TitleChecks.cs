// The title screen (group "title", Test 003 round 1): the same seven items with the same words in the same order, in
// two columns; every item at least 78 picture units tall (about 44 phone pixels); Up / Down go through them as before
// and Left / Right jump between the columns; a tap on an item opens it.
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
        public static void TitleChecks()
        {
            Audio.MusicOn = false;
            string dir = Path.Combine(Data.Dir, "title-checks");
            Directory.CreateDirectory(dir);
            var g = new BrawlGame { Scores = new ScoreBook(Path.Combine(dir, "scores.txt")), Player = "Kim", Rng = new Random(5) };
            g.KeysFileOverride = Path.Combine(dir, "keys.txt");
            g.SnapshotScene("Title", 0, 1);
            Func<System.Collections.Generic.List<HitArea>> draw = () =>
            {
                using (var dv = new DrawingVisual((int)g.Width, (int)g.Height))
                using (DrawingContext dc = dv.RenderOpen()) SahuaritaSmash.Views.GameView.RenderGame(g, dc);
                return g.MouseHits.Where(h => h.Item >= 0).OrderBy(h => h.Item).ToList();
            };
            var items = draw();
            string[] want = { "Fight (2 to 4 players)", "The ladder (on your own)", "Practice (try your moves)", "How to play", "Controls (change the keys)", "Play online (with others)", "Quit" };
            Check("title: the same seven items, words and order", items.Select(h => h.Label).SequenceEqual(want), string.Join(" | ", items.Select(h => h.Label)));
            Check("title: every item is at least 78 units tall and 78 wide", items.All(h => h.Area.Height >= BrawlGame.KitButtonH && h.Area.Height >= 78 && h.Area.Width >= 78), string.Join(", ", items.Select(h => h.Area.Height)));
            bool twoColumns = items.Take(BrawlGame.TitleLeftColumn).All(h => h.Area.X == items[0].Area.X) && items.Skip(BrawlGame.TitleLeftColumn).All(h => h.Area.X > items[0].Area.Right);
            bool apart = true;
            for (int i = 0; i < items.Count; i++) for (int j = i + 1; j < items.Count; j++) if (items[i].Area.Intersects(items[j].Area)) apart = false;
            Check("title: two columns, nothing overlapping, all on the screen", twoColumns && apart && items.All(h => h.Area.Right <= g.Width && h.Area.Bottom <= g.Height), "");
            var back = g.MouseHits.FirstOrDefault(h => h.Label == "Back");
            Check("title: the Back button is at least 78 units tall and wide", back != null && back.Area.Height >= 78 && back.Area.Width >= 78, back == null ? "none" : back.Area.ToString());
            // keys: Up / Down as before (through all seven, wrapping); Left / Right between the columns
            g.Sel = 0; g.Press(Key.Down); g.KeyUp(Key.Down); int down = g.Sel;
            g.Sel = 0; g.Press(Key.Up); g.KeyUp(Key.Up); int up = g.Sel;
            g.Sel = 0; g.Press(Key.Right); g.KeyUp(Key.Right); int right0 = g.Sel;
            g.Sel = 3; g.Press(Key.Right); g.KeyUp(Key.Right); int right3 = g.Sel;
            g.Sel = 5; g.Press(Key.Left); g.KeyUp(Key.Left); int left5 = g.Sel;
            Check("title: Up / Down go through the items as before, Left / Right switch columns", down == 1 && up == 6 && right0 == 4 && right3 == 6 && left5 == 1,
                "down " + down + " up " + up + " right0 " + right0 + " right3 " + right3 + " left5 " + left5);
            // a tap opens the item (How to play)
            g.Sel = 0; items = draw();
            var help = items.First(h => h.Label == "How to play");
            var c = new Point(help.Area.X + help.Area.Width / 2, help.Area.Y + help.Area.Height / 2);
            g.PointerMove(c); g.PointerDown(c, false); g.PointerUp(c, false);
            Check("title: tapping How to play opens it", g.Menu == BrawlGame.Screen.Help, g.Menu.ToString());
        }
    }
}
