// Classic Games - Sahuarita Smash's recorded music checks (part of -Test, group "brawl"): music.txt is read safely, a
// recorded song plays at its own rate, loops after its intro without a click, a jingle stops at its end, the loop finder
// finds the beat and a loop of whole bars in a made-up song, and the game falls back to its built-in tunes when there
// are no songs, then takes the recorded one when it is ready. No real files are decoded and no sound is played.
using System;
using System.Collections.Generic;
using System.Linq;

namespace ClassicGames
{
    public static partial class Tests
    {
        static void BrawlMusicChecks()
        {
            // the index: names only, never paths; numbers read the same everywhere
            var ix = BrawlMusic.ReadIndex("# a comment\r\nmenu menu.mp3 17.05 42.7 1.41\nmesa ..\\..\\evil.mp3 1 2 1\nlab C:\\x.mp3 1 2 1\nvictory victory.mp3 0 0 1.6\nbroken\n");
            Check("brawl music: music.txt gives each song its file, loop and volume, and refuses anything that isn't a plain name",
                ix.Count == 2 && ix["menu"].File == "menu.mp3" && Math.Abs(ix["menu"].LoopStart - 17.05) < 1e-9 && Math.Abs(ix["menu"].LoopEnd - 42.7) < 1e-9 && ix["victory"].LoopEnd == 0 && !ix.ContainsKey("mesa") && !ix.ContainsKey("lab"),
                string.Join(",", ix.Keys));

            // a made-up recorded song: 22050 a second, a tone that swells and fades so every place sounds different
            int rate = 22050;
            var pcm = new short[rate * 6];
            for (int i = 0; i < pcm.Length; i++) { double t = i / (double)rate; pcm[i] = (short)(Math.Sin(2 * Math.PI * 330 * t) * (0.3 + 0.2 * Math.Sin(t * 1.7)) * 30000); }
            // a loop whose length isn't a whole number of the tone's waves (the worst case for a seam)
            Song s = Song.FromPcm("test", pcm, rate, true, (int)(2.0 * rate), (int)(5.0013 * rate), 1);
            float[] seam = BrawlMusic.RenderSeam(s, 1, 1);
            double worst;
            int clicks = CountClicks(seam, 0.02, out worst);
            Check("brawl music: a recorded song loops back after its intro without a click", clicks == 0, clicks + " clicks, worst " + worst.ToString("0.000"));
            // its own rate plays at the right pitch: 330 Hz stays 330 Hz at 44100 a second
            int crossings = 0; for (int i = 1; i < Audio.Rate / 2; i++) if (seam[i - 1] < 0 && seam[i] >= 0) crossings++;
            Check("brawl music: a song recorded at another rate plays at the right pitch", Math.Abs(crossings - 165) <= 2, crossings + " rising crossings in half a second (165 expected)");
            // a jingle (no loop) stops at its end
            Song j = Song.FromPcm("jingle", pcm.Take(rate).ToArray(), rate, false, 0, 0, 1);
            var p = new SongPlayer(); p.Start(j);
            int played = 0; while (p.Song != null && played < Audio.Rate * 3) { p.Next(); played++; }
            Check("brawl music: a victory jingle plays once and stops", p.Song == null && Math.Abs(played - Audio.Rate) < 100, played.ToString());

            // the loop finder on a made-up song: an intro, 60 s of a 120 bpm beat with a tune over it, then a fade
            int r2 = 16000;
            var song = new short[r2 * 72];
            var rng = new Random(3);
            for (int i = 0; i < song.Length; i++)
            {
                double t = i / (double)r2, v;
                if (t < 6) v = (rng.NextDouble() - 0.5) * 0.15 + Math.Sin(2 * Math.PI * 220 * t) * 0.1;   // an intro unlike the rest
                else
                {
                    double tb = (t - 6) % 0.5, bar = (t - 6) % 2.0;
                    double kick = tb < 0.08 ? Math.Sin(2 * Math.PI * 60 * tb) * (1 - tb / 0.08) * 0.8 : 0;
                    double note = new[] { 262, 330, 392, 330 }[(int)(bar / 0.5)];
                    v = kick + Math.Sin(2 * Math.PI * note * t) * 0.15;
                    if (t > 66) v *= Math.Max(0, 1 - (t - 66) / 6);                                            // the ending fades
                }
                song[i] = (short)(Math.Max(-1, Math.Min(1, v)) * 32000);
            }
            BrawlLoop.Result lr = BrawlLoop.Find(song, r2, true);
            double bars = (lr.End - lr.Start) / 2.0;
            Check("brawl music: the loop finder hears the beat and loops whole bars, after the intro and before the fade",
                Math.Abs(lr.Bpm - 120) < 3 && Math.Abs(bars - Math.Round(bars)) < 0.03 && lr.Start >= 6 && lr.End <= 66 && lr.End - lr.Start >= 30 && lr.Gain > 0,
                lr.Bpm.ToString("0") + " bpm, " + lr.Start.ToString("0.00") + "-" + lr.End.ToString("0.00") + " s (" + bars.ToString("0.00") + " bars)");

            // recorded songs sit at about the loudness of the built-in tunes (their volume in music.txt aims at TargetRms)
            var tp = new SongPlayer(); tp.Start(Music.For("Brawl").Theme);
            double sum = 0; int nn = Audio.Rate * 8; for (int i = 0; i < nn; i++) { double v = tp.Next(); sum += v * v; }
            double builtRms = Math.Sqrt(sum / nn);
            Check("brawl music: recorded songs play about as loud as the built-in tunes", BrawlLoop.TargetRms > builtRms * 0.6 && BrawlLoop.TargetRms < builtRms * 1.6, "built-in " + builtRms.ToString("0.000") + ", recorded aim " + BrawlLoop.TargetRms);

            // the game: no songs = the built-in tunes; a song ready = it takes over
            bool was = BrawlMusic.Enabled; string folder = BrawlMusic.Folder;
            try
            {
                BrawlMusic.Reset(); BrawlMusic.Enabled = false;
                var g = new BrawlGame { Player = "Kim", Rng = new Random(2) };
                g.Begin();
                for (int i = 0; i < 5; i++) g.Update(1.0 / 60);
                bool builtIn = Audio.Current == g.Tunes.Theme;
                BrawlMusic.Put("menu", s);
                for (int i = 0; i < 5; i++) g.Update(1.0 / 60);
                Check("brawl music: without songs the menu plays its built-in tune, and a recorded one takes over once it's ready", builtIn && Audio.Current == s, builtIn + " " + (Audio.Current == s));
                Check("brawl music: tests and snapshots never decode the real songs", !BrawlMusic.Has("menu"), "");
            }
            finally { BrawlMusic.Reset(); BrawlMusic.Enabled = was; BrawlMusic.Folder = folder; Audio.StopMusic(); }
        }
    }
}
