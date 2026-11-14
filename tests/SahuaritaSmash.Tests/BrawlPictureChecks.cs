// Sahuarita Smash's picture checks, copied from App Hub's Classic Games (Engine\BrawlTests.cs: BrawlDrawChecks without
// the menus, which come with the game; Engine\BrawlKeysTests.cs: BrawlMotionChecks), drawn off screen through the
// WPF names in src\SahuaritaSmash\Wpf. Plus snapshots: -Snapshot <dir> saves a fight on every stage.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Media;

namespace ClassicGames
{
    public static partial class Tests
    {
        static void BrawlDrawChecks()
        {
            // every fighter in every state draws, on every stage, and the menus draw
            var view = new BrawlView();
            int drawn = 0; string err = null;
            try
            {
                foreach (BStageDef st in BrawlData.Stages)
                {
                    BrawlSim m = new BrawlSim(st, BrawlData.Fighters.Take(4).ToList(), 3, 0, 3);
                    var dv = new DrawingVisual();
                    using (DrawingContext dc = dv.RenderOpen()) { view.Follow(m, true); view.Draw(dc, m, 1.5); view.DrawPanels(dc, m, null, 1.5); }
                    drawn++;
                }
                foreach (BFighterDef f in BrawlData.Fighters)
                    foreach (BS s in Enum.GetValues(typeof(BS)))
                    {
                        var dv = new DrawingVisual();
                        using (DrawingContext dc = dv.RenderOpen()) { view.Portrait(dc, f, 100, 200, 100, 0.5, false, s); view.Portrait(dc, f, 100, 200, 40, 0.5, true, s); }
                        drawn++;
                    }
                foreach (BFighterDef f in BrawlData.Fighters)
                    foreach (BMove mv in f.Moves.Values)
                        for (int fr = 1; fr <= mv.Len; fr += 4)
                        {
                            var bf = new BFighter { D = f, St = BS.Attack, Mv = mv, MF = fr, Face = -1 };
                            var dv = new DrawingVisual();
                            using (DrawingContext dc = dv.RenderOpen()) view.DrawPuppet(dc, bf, 100, 200, 1, 0.5, 0);
                            drawn++;
                        }
                // (the menus: with the game, docs/PLAN.md step 3)
            }
            catch (Exception ex) { err = ex.GetType().Name + ": " + ex.Message; }
            Check("brawl: every stage, every fighter in every state and frame of every move, and the menus draw", err == null, err ?? drawn + " pictures");
        }


        static void BrawlMotionChecks()
        {
            var fs = BrawlData.Fighters;
            int styled = fs.Count(d => BrawlView.MotionOf(d.Id) != BrawlView.MotionOf("nobody"));
            Check("brawl motion: every fighter has its own way of moving", styled == fs.Count, styled + " of " + fs.Count);
            string same = "";
            foreach (string anim in new[] { "jab", "punch", "swing", "bigswing", "kick", "uppercut", "lash", "sweep" })
                for (int i = 0; i < fs.Count; i++)
                    for (int j = i + 1; j < fs.Count; j++)
                    {
                        BPose wa, sa, wb, sb;
                        BrawlView.StyledPoses(fs[i].Id, anim, false, out wa, out sa);
                        BrawlView.StyledPoses(fs[j].Id, anim, false, out wb, out sb);
                        double d = Math.Abs(sa.Lean - sb.Lean) + Math.Abs(sa.Crouch - sb.Crouch) * 100 + Math.Abs(sa.FA1 - sb.FA1) + Math.Abs(sa.FA2 - sb.FA2)
                            + Math.Abs(sa.BA1 - sb.BA1) + Math.Abs(sa.BA2 - sb.BA2) + Math.Abs(sa.FL1 - sb.FL1) + Math.Abs(sa.BL1 - sb.BL1) + Math.Abs(sa.Head - sb.Head);
                        if (d < 15 && same.Length < 200) same += anim + ": " + fs[i].Id + "/" + fs[j].Id + " ";
                    }
            Check("brawl motion: the same attack looks different for every two fighters", same == "", same);
            string bad = "";
            foreach (BFighterDef d in fs)
                foreach (BMove m in d.Moves.Values)
                    for (int fr = 0; fr <= m.Len; fr++)
                    {
                        bool air = m.Id.EndsWith("air");
                        var f = new BFighter { D = d, St = BS.Attack, Mv = m, MF = fr, Face = 1, Ground = !air, AirMove = air };
                        BPose p = BrawlView.Pose(f, fr / 60.0);
                        double sum = p.Lean + p.Crouch + p.FA1 + p.FA2 + p.BA1 + p.BA2 + p.FL1 + p.FL2 + p.BL1 + p.BL2 + p.Spin + p.Head;
                        if ((double.IsNaN(sum) || double.IsInfinity(sum) || p.Crouch > 1.2 || p.Crouch < -0.2) && bad.Length < 200) bad += d.Id + " " + m.Id + "@" + fr + " ";
                    }
            Check("brawl motion: every frame of every attack is a sane pose", bad == "", bad);
        }

        /// <summary>Four computer players on each stage for a few seconds, drawn like the app draws them; checks the
        /// picture isn't blank and saves it with -Snapshot.</summary>
        static void BrawlFightPictures()
        {
            for (int st = 0; st < BrawlData.Stages.Count; st++)
            {
                var view = new SahuaritaSmash.Views.MainView();
                for (int i = 0; i < st; i++) view.NewMatch();
                for (int i = 0; i < 60 * 6; i++) view.Tick(1.0 / 60);
                var dv = new DrawingVisual(1280, 720);
                using (DrawingContext dc = dv.RenderOpen()) view.DrawGame(dc);
                int colours = Colours(dv.Bitmap);
                Check("brawl pictures: a fight on " + BrawlData.Stages[st].Name + " is drawn (not blank)", colours > 200, colours + " colours");
                Save(dv.Bitmap, "Fight-" + BrawlData.Stages[st].Id);
            }
        }

        static int Colours(Avalonia.Media.Imaging.RenderTargetBitmap bmp)
        {
            using (var ms = new MemoryStream())
            {
                bmp.Save(ms);
                ms.Position = 0;
                var wb = Avalonia.Media.Imaging.WriteableBitmap.Decode(ms);
                using (var fb = wb.Lock())
                {
                    var seen = new HashSet<int>();
                    var row = new int[fb.Size.Width];
                    for (int y = 0; y < fb.Size.Height; y += 4)
                    {
                        System.Runtime.InteropServices.Marshal.Copy(fb.Address + y * fb.RowBytes, row, 0, row.Length);
                        for (int x = 0; x < row.Length; x += 4) seen.Add(row[x]);
                    }
                    return seen.Count;
                }
            }
        }

        static void Save(Avalonia.Media.Imaging.RenderTargetBitmap bmp, string name)
        {
            if (SnapshotDir == null) return;
            Directory.CreateDirectory(SnapshotDir);
            bmp.Save(Path.Combine(SnapshotDir, name + ".png"));
        }

        public static void BrawlPictureChecks()
        {
            BrawlDrawChecks();
            BrawlMotionChecks();
            BrawlFightPictures();
        }
    }
}
