// The sound plumbing new in this app (App Hub had waveOut built in): the mixer's thread feeds an IAudioOut, and the
// recorded songs come from an IMusicSource. Checked with a pretend output and a pretend song: no sound is played.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace ClassicGames
{
    public static partial class Tests
    {
        sealed class FakeOut : IAudioOut
        {
            public int Blocks, BlockSize; public double Peak; public bool Closed;
            public string Error { get { return null; } }
            public bool Open(int rate, int block) { BlockSize = block; return rate == Audio.Rate; }
            public void Write(short[] b)
            {
                Blocks++;
                foreach (short v in b) Peak = Math.Max(Peak, Math.Abs(v / 32768.0));
                Thread.Sleep(2);   // a real device waits while its queue is full
            }
            public void Close() { Closed = true; }
        }

        sealed class FakeSongs : IMusicSource
        {
            public int Decoded;
            public string ReadIndex() { return "menu menu.mp3 1 3 1.0\nmesa C:\\evil.mp3 0 0 1\n"; }
            public bool Exists(string file) { return file == "menu.mp3"; }
            public short[] DecodeMono16(string file, out int rate)
            {
                Decoded++; rate = 22050;
                var a = new short[rate * 4];
                for (int i = 0; i < a.Length; i++) a[i] = (short)(8000 * Math.Sin(i * 2 * Math.PI * 440 / rate));
                return a;
            }
        }

        public static void SoundOutChecks()
        {
            var o = new FakeOut();
            bool started = Audio.Start(o);
            bool was = Audio.SoundsOn; Audio.SoundsOn = true;
            Audio.Sfx("brawl-go", 1.0);
            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < 3000 && (o.Blocks < 30 || o.Peak < 0.01)) Thread.Sleep(10);
            Audio.Shutdown();
            Audio.SoundsOn = was;
            Check("sound: the mixer's thread sends blocks to the platform's output, with the sounds in them, and closes it", started && o.Blocks >= 30 && o.Peak > 0.01 && o.Closed && o.BlockSize > 0, o.Blocks + " blocks, peak " + o.Peak.ToString("0.000"));
            Check("sound: with no output the game stays silent and nothing starts", !Audio.Start(null) && !Audio.Live, "");

            var src = new FakeSongs();
            bool en = BrawlMusic.Enabled; IMusicSource before = BrawlMusic.Source;
            try
            {
                BrawlMusic.Reset(); BrawlMusic.Enabled = true; BrawlMusic.Source = src;
                Song s = BrawlMusic.Get("menu");
                sw.Restart();
                while (s == null && sw.ElapsedMilliseconds < 3000) { Thread.Sleep(10); s = BrawlMusic.Get("menu"); }
                Check("sound: a recorded song comes from the platform's source, decoded in the background, looping where music.txt says", s != null && s.Recorded && s.PcmRate == 22050 && s.LoopA == 22050 && s.LoopB == 66150 && src.Decoded == 1, s == null ? "null" : s.PcmRate + " " + s.LoopA + " " + s.LoopB);
                Check("sound: a song music.txt names by a path is never opened", BrawlMusic.Get("mesa") == null && !BrawlMusic.Has("mesa"), "");
            }
            finally { BrawlMusic.Reset(); BrawlMusic.Source = before; BrawlMusic.Enabled = en; }
        }
    }
}
