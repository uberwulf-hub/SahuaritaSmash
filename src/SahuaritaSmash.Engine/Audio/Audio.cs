// Classic Games - sound (from App Hub's Classic Games, Engine\Audio.cs): a small software mixer (a background thread
// fills short blocks for the platform's sound output, IAudioOut: waveOut on Windows, AudioTrack on Android, nothing in
// the checks, so sounds start within about 50 ms and the game loop never waits for audio), with an NES-style synthesizer for the
// music (two square channels, a triangle bass and noise drums) and sound effects made in code. No sound files.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace ClassicGames
{
    internal struct Ev { public int Start, Len; public double Freq; public char Drum; }

    /// <summary>
    /// A tune in a small text notation. Four tracks: square 1 (lead), square 2 (harmony), triangle (bass), noise (drums).
    /// Tokens are separated by spaces; each lasts one step (a sixteenth note by default) or ":n" steps.
    /// Notes: C4, F#3, Bb5. Rest: ".". Drums (noise track): k kick, s snare, h hat, o open hat, t tom.
    /// "[ ... ]x3" repeats a group. Each track loops on its own length, so a one-bar drum loop can sit under an
    /// eight-bar melody.
    /// </summary>
    public sealed class Song
    {
        public string Name { get; private set; }
        public double Bpm { get; set; }
        public int StepsPerBeat { get; set; }
        public bool Loop { get; set; }
        public double Duty1 = 0.5, Duty2 = 0.25;
        public double Gain = 1.0;
        /// <summary>Gentler instruments (Spaceareum): rounded, filtered squares, a smooth triangle, softer drums and
        /// slower note starts and ends, so nothing clicks.</summary>
        public bool Soft;
        /// <summary>Tunes with the same SyncGroup (and tempo) crossfade in step: the new one starts at the same place in
        /// the bar as the one going out, so its layers come in on the beat.</summary>
        public string SyncGroup;
        /// <summary>A recorded song (Sahuarita Smash's, BrawlMusic.cs): 16-bit mono at PcmRate, looping LoopA..LoopB (in
        /// samples) after playing its intro once. Null: a song played from notes.</summary>
        internal short[] Pcm; internal int PcmRate, LoopA, LoopB;
        public bool Recorded { get { return Pcm != null; } }

        public static Song FromPcm(string name, short[] pcm, int rate, bool loop, int loopA, int loopB, double gain)
        {
            var s = new Song(name, 120, loop, "", "", "", "");
            s.Pcm = pcm; s.PcmRate = Math.Max(8000, rate); s.Gain = gain;
            s.LoopB = Math.Max(1, Math.Min(pcm.Length, loopB > 0 ? loopB : pcm.Length));
            s.LoopA = Math.Max(0, Math.Min(s.LoopB - s.PcmRate, loopA));
            if (s.LoopA < 0) s.LoopA = 0;
            return s;
        }
        public int TrackCount { get; private set; }
        internal Ev[][] Tracks = new Ev[7][];
        internal int[][] StartAt = new int[7][];
        internal int[] Lengths = new int[7];
        internal static bool IsDrumTrack(int t, int count) { return count == 4 ? t == 3 : t >= 3 && t <= 5; }

        public Song(string name, double bpm, bool loop, string lead, string harmony, string bass, string drums)
            : this(name, bpm, loop, new[] { lead, harmony, bass, drums }) { }

        Song(string name, double bpm, bool loop, string[] src)
        {
            Name = name; Bpm = bpm; Loop = loop; StepsPerBeat = 4; TrackCount = src.Length;
            for (int t = 0; t < src.Length; t++)
            {
                int len;
                List<Ev> evs = Parse(src[t] ?? "", IsDrumTrack(t, src.Length), out len);
                Tracks[t] = evs.ToArray(); Lengths[t] = len;
                int[] at = new int[Math.Max(1, len)];
                for (int i = 0; i < at.Length; i++) at[i] = -1;
                for (int i = 0; i < Tracks[t].Length; i++) at[Tracks[t][i].Start] = i;
                StartAt[t] = at;
            }
        }

        public int TrackLength(int track) { return Lengths[track]; }
        public int Steps { get { int m = 0; foreach (int l in Lengths) m = Math.Max(m, l); return m; } }
        public double Seconds { get { return Pcm != null && PcmRate > 0 ? Pcm.Length / (double)PcmRate : Steps * 60.0 / (Bpm * StepsPerBeat); } }

        public static double NoteFreq(int midi) { return 440.0 * Math.Pow(2, (midi - 69) / 12.0); }

        public static int Midi(string name)
        {
            int[] pcs = { 9, 11, 0, 2, 4, 5, 7 };   // A B C D E F G
            char l = char.ToUpperInvariant(name[0]);
            if (l < 'A' || l > 'G') throw new FormatException("not a note: " + name);
            int pc = pcs[l - 'A'], i = 1;
            if (i < name.Length && name[i] == '#') { pc++; i++; }
            else if (i < name.Length && name[i] == 'b') { pc--; i++; }
            int oct = int.Parse(name.Substring(i), CultureInfo.InvariantCulture);
            return 12 * (oct + 1) + pc;
        }

        static List<string> Expand(string src)
        {
            string s = src.Replace("[", " [ ").Replace("]", " ]").Replace("|", " ");
            string[] raw = s.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var stack = new Stack<List<string>>();
            var cur = new List<string>();
            foreach (string tok in raw)
            {
                if (tok == "[") { stack.Push(cur); cur = new List<string>(); }
                else if (tok.StartsWith("]"))
                {
                    int n = 2;
                    if (tok.Length > 2 && (tok[1] == 'x' || tok[1] == '*')) n = int.Parse(tok.Substring(2), CultureInfo.InvariantCulture);
                    var outer = stack.Pop();
                    for (int r = 0; r < n; r++) outer.AddRange(cur);
                    cur = outer;
                }
                else cur.Add(tok);
            }
            if (stack.Count > 0) throw new FormatException("unclosed [ in song");
            return cur;
        }

        static List<Ev> Parse(string src, bool drums, out int length)
        {
            var evs = new List<Ev>();
            int pos = 0;
            foreach (string tok in Expand(src))
            {
                string name = tok; int len = 1;
                int c = tok.IndexOf(':');
                if (c > 0) { name = tok.Substring(0, c); len = int.Parse(tok.Substring(c + 1), CultureInfo.InvariantCulture); }
                if (name != ".")
                {
                    var e = new Ev { Start = pos, Len = len };
                    if (drums) e.Drum = name[0]; else e.Freq = NoteFreq(Midi(name));
                    evs.Add(e);
                }
                pos += len;
            }
            length = pos;
            return evs;
        }
    }

    /// <summary>One synth channel: square (any duty), 4-bit triangle, or noise drums.</summary>
    internal sealed class Chan
    {
        const double Rate = Audio.Rate;
        readonly int kind;   // 0 square, 1 triangle, 2 drums
        double freq, phase, duty = 0.5, env, prevNoise, drumPhase, lpq, lpn;
        int gate, age;
        bool on, soft;
        char drum;
        uint lfsr = 1;
        public double Level = 1;

        public Chan(int kind) { this.kind = kind; }

        public void NoteOn(double f, int gateSamples, double duty) { NoteOn(f, gateSamples, duty, false); }
        public void NoteOn(double f, int gateSamples, double duty, bool soft)
        {
            // a soft note carries on from where the wave is (starting it again at 0 while the last note still sounds clicks)
            if (kind != 1 && !(soft && on && env > 0.001)) phase = 0;
            freq = f; gate = gateSamples; age = 0; on = true; this.duty = duty; this.soft = soft;
        }
        public void Hit(char d) { Hit(d, false); }
        public void Hit(char d, bool soft) { drum = d; age = 0; on = true; drumPhase = 0; this.soft = soft; }
        public void Off() { on = false; env = 0; }

        double Noise()
        {
            uint bit = (lfsr ^ (lfsr >> 1)) & 1u;
            lfsr = (lfsr >> 1) | (bit << 14);
            return (lfsr & 1u) != 0 ? 1.0 : -1.0;
        }

        public double Next()
        {
            if (!on) return 0;
            double t = age / Rate;
            age++;
            if (kind == 2 && soft) return SoftDrum(t) * Level;
            if (kind == 2)
            {
                double v;
                switch (drum)
                {
                    case 'k': drumPhase += (48 + 120 * Math.Exp(-t * 32)) / Rate; v = Math.Sin(2 * Math.PI * drumPhase) * Math.Exp(-t * 13) * 0.42; if (t > 0.3) on = false; break;
                    case 's': drumPhase += 185 / Rate; v = Noise() * Math.Exp(-t * 20) * 0.17 + (drumPhase % 1 < 0.5 ? 1 : -1) * Math.Exp(-t * 40) * 0.08; if (t > 0.25) on = false; break;
                    case 'h': { double n = Noise(); v = (n - prevNoise) * 0.5 * Math.Exp(-t * 75) * 0.12; prevNoise = n; if (t > 0.06) on = false; break; }
                    case 'o': { double n = Noise(); v = (n - prevNoise) * 0.5 * Math.Exp(-t * 14) * 0.09; prevNoise = n; if (t > 0.3) on = false; break; }
                    case 't': drumPhase += (95 + 70 * Math.Exp(-t * 18)) / Rate; v = Math.Sin(2 * Math.PI * drumPhase) * Math.Exp(-t * 9) * 0.32; if (t > 0.35) on = false; break;
                    default: v = 0; on = false; break;
                }
                return v * Level;
            }
            // gate, then a short release; squares get a soft attack and a decay to a sustain level
            double target;
            if (gate > 0) { gate--; target = kind == 0 ? 0.55 + 0.45 * Math.Exp(-t * 9) : 1.0; }
            else target = 0;
            double speed = soft ? (gate > 0 ? 0.006 : 0.0025) : (gate > 0 ? (age < 160 ? 0.03 : 0.02) : 0.0035);   // soft: about 4 ms on, 9 ms off
            env += (target - env) * speed;
            if (gate <= 0 && env < 0.0008) { on = false; return 0; }
            phase += freq / Rate; if (phase >= 1) phase -= Math.Floor(phase);
            double w;
            double tri = phase < 0.5 ? 4 * phase - 1 : 3 - 4 * phase;          // -1..1
            if (kind == 0)
            {
                if (soft)
                {
                    // a filtered square blended with a triangle: round, like a soft flute or an ocarina
                    lpq += ((phase < duty ? 1 : -1) - lpq) * 0.18;
                    w = lpq * 0.6 + tri * 0.55;
                }
                else w = phase < duty ? 1 : -1;
            }
            else w = soft ? tri : Math.Floor((tri + 1) * 7.5 + 0.5) / 7.5 - 1;   // 16 steps, like the NES (soft: smooth)
            return w * env * (kind == 0 ? (soft ? 0.1 : 0.11) : 0.24) * Level;
        }

        /// <summary>Gentle drums: low-passed noise, soft starts, a little quieter.</summary>
        double SoftDrum(double t)
        {
            double v, att = Math.Min(1, age / 90.0);   // 2 ms on
            switch (drum)
            {
                case 'k': drumPhase += (48 + 100 * Math.Exp(-t * 30)) / Rate; v = Math.Sin(2 * Math.PI * drumPhase) * Math.Exp(-t * 12) * 0.36; if (t > 0.32) on = false; break;
                case 's': { lpn += (Noise() - lpn) * 0.25; drumPhase += 185 / Rate; v = lpn * Math.Exp(-t * 22) * 0.2 + Math.Sin(2 * Math.PI * drumPhase) * Math.Exp(-t * 35) * 0.06; if (t > 0.25) on = false; break; }
                case 'h': { lpn += (Noise() - lpn) * 0.45; double n = lpn; v = (n - prevNoise) * 0.5 * Math.Exp(-t * 80) * 0.07; prevNoise = n; if (t > 0.06) on = false; break; }
                case 'o': { lpn += (Noise() - lpn) * 0.45; double n = lpn; v = (n - prevNoise) * 0.5 * Math.Exp(-t * 16) * 0.05; prevNoise = n; if (t > 0.3) on = false; break; }
                case 't': drumPhase += (95 + 60 * Math.Exp(-t * 18)) / Rate; v = Math.Sin(2 * Math.PI * drumPhase) * Math.Exp(-t * 9) * 0.26; if (t > 0.35) on = false; break;
                default: v = 0; on = false; break;
            }
            return v * att;
        }
    }

    /// <summary>Plays one Song: steps through its tracks and drives four channels.</summary>
    internal sealed class SongPlayer
    {
        public Song Song;
        public double Pos;          // in steps
        public bool Paused;
        public double Tempo = 1;
        int lastStep = -1;
        readonly Chan[] ch = { new Chan(0), new Chan(0), new Chan(1), new Chan(2) };

        public void Start(Song s) { Song = s; Pos = 0; lastStep = -1; foreach (Chan c in ch) c.Off(); Paused = false; }
        /// <summary>An industrial tune joining another in step: from the same place in the bar, its long notes already
        /// sounding (they swell in), so its layers arrive on the beat.</summary>
        public void StartSynced(Song s, double pos)
        {
            Start(s);
            if (s.Steps <= 0) return;
            Pos = pos % s.Steps; lastStep = (int)Math.Floor(Pos);
        }
        /// <summary>Starts a song part-way through (coming back to a tune carries on where it was).</summary>
        public void StartAt(Song s, double pos) { Start(s); Pos = pos; lastStep = (int)Math.Floor(pos) - 1; }
        public void Stop() { Song = null; foreach (Chan c in ch) c.Off(); }

        public double Next()
        {
            Song s = Song;
            if (s == null || Paused) return 0;
            if (s.Pcm != null) return NextRecorded(s);
            int step = (int)Pos;
            if (step != lastStep)
            {
                for (int st = lastStep + 1; st <= step; st++) Trigger(s, st);
                lastStep = step;
            }
            double v = ch[0].Next() + ch[1].Next() + ch[2].Next() + ch[3].Next();
            Pos += s.Bpm * Tempo * s.StepsPerBeat / (60.0 * Audio.Rate);
            if (!s.Loop && Pos >= s.Steps + s.StepsPerBeat) Song = null;   // a beat of release after a jingle
            return v * s.Gain;
        }

        /// <summary>A recorded song: Pos counts its own samples (read between two, so any rate plays at the right pitch);
        /// the last 60 ms before the loop's end fade into the 60 ms before its start, so the seam doesn't click.</summary>
        double NextRecorded(Song s)
        {
            short[] a = s.Pcm;
            int i = (int)Pos; double fr = Pos - i;
            double v = Pick(a, i, fr);
            if (s.Loop)
            {
                int span = s.LoopB - s.LoopA, xf = Math.Min(s.PcmRate * 6 / 100, span / 4);
                if (Pos >= s.LoopB - xf && xf > 0)
                {
                    double k = (Pos - (s.LoopB - xf)) / xf;
                    v = v * (1 - k) + Pick(a, i - span, fr) * k;
                }
                Pos += s.PcmRate * Tempo / (double)Audio.Rate;
                if (Pos >= s.LoopB) Pos -= span;
            }
            else
            {
                Pos += s.PcmRate / (double)Audio.Rate;
                if (Pos >= a.Length - 1) Song = null;
            }
            return v * (1.0 / 32768) * s.Gain;
        }
        static double Pick(short[] a, int i, double fr)
        {
            if (i < 0 || i >= a.Length) return 0;
            double x = a[i], y = i + 1 < a.Length ? a[i + 1] : x;
            return x + (y - x) * fr;
        }

        void Trigger(Song s, int step)
        {
            double samplesPerStep = 60.0 * Audio.Rate / (s.Bpm * Tempo * s.StepsPerBeat);
            for (int t = 0; t < 4; t++)
            {
                int len = s.Lengths[t];
                if (len == 0 || (!s.Loop && step >= len)) continue;
                int i = s.StartAt[t][step % len];
                if (i < 0) continue;
                Ev e = s.Tracks[t][i];
                if (t == 3) ch[3].Hit(e.Drum, s.Soft);
                else ch[t].NoteOn(e.Freq, (int)(e.Len * samplesPerStep * (e.Len == 1 ? 0.75 : 0.88)), t == 0 ? s.Duty1 : s.Duty2, s.Soft);
            }
        }
    }

    public static class Audio
    {
        public const int Rate = 44100;
        const int BufSamples = 441, BufCount = 5;   // 10 ms buffers, about 50 ms queued
        static readonly object sync = new object();

        public static bool MusicOn = true, SoundsOn = true;
        public static double MusicVolume = 0.7, SoundVolume = 0.8;

        static SongPlayer main = new SongPlayer();
        static readonly SongPlayer sting = new SongPlayer();
        // crossfades: the tune playing comes in along mainL (0..1) at Ease(mainL); the tunes on their way out share what
        // is left, 1 - Ease(mainL), each in proportion to how loud it was when this crossfade began (G0). So all the tunes
        // together are always exactly one tune's level: never two at full volume, no dip, no jump, even when the music
        // changes again half-way through a crossfade.
        sealed class Fader { public SongPlayer P; public double G0; }
        static readonly List<Fader> outgoing = new List<Fader>();
        static double mainL = 1, mainStep, outBase = 1;
        static int mainId;
        static double OutK { get { return outBase > 1e-6 ? Math.Max(0, (1 - Ease(mainL)) / outBase) : 0; } }
        static double InvEase(double g)
        {
            double lo = 0, hi = 1;
            for (int i = 0; i < 30; i++) { double m = (lo + hi) / 2; if (Ease(m) < g) lo = m; else hi = m; }
            return (lo + hi) / 2;
        }
        static long sampleClock;
        // where each tune was when it last faded out (and when), to carry on from there if it comes back soon
        static readonly Dictionary<Song, double[]> leftAt = new Dictionary<Song, double[]>();
        /// <summary>Tests: each mixed block's (tune coming in, total of every tune's level) when TraceFades is on.</summary>
        public static bool TraceFades;
        public static readonly List<double[]> FadeTrace = new List<double[]>();
        static double Ease(double l) { return l <= 0 ? 0 : (l >= 1 ? 1 : l * l * (3 - 2 * l)); }
        struct Shot { public float[] Data; public int Pos; public float Vol; public bool Music; public bool Loop; public int Id; public float Lp, L1, L2; public int Group, Fade; public double FPos; public float Step, VolT; public bool Glide; }
        // Hive Purge's voices share a bus with a soft limiter, so a room full of guns and aliens never clips
        static double limG = 1;
        const double LimitAt = 0.7;
        /// <summary>Tests: how far the limiter on Hive Purge's sounds is turning them down now (1 = not at all).</summary>
        public static double HiveLimiter { get { lock (sync) return limG; } }
        const int StealFade = 260;   // a voice taken for a new sound fades out over about 6 ms
        static readonly List<Shot> shots = new List<Shot>();
        static readonly Dictionary<string, float[]> cache = new Dictionary<string, float[]>();
        static int nextId = 1;
        static double lp;

        /// <summary>True when a sound device is open (false in tests and on PCs without sound).</summary>
        public static bool Live { get; private set; }
        /// <summary>About how long after a call the sound is heard, in seconds.</summary>
        public static double Latency { get { return Live ? BufSamples * BufCount / (double)Rate + 0.01 : 0; } }
        public static string LastError { get; private set; }
        /// <summary>How many buffers have gone to the sound device (to check the audio thread keeps going).</summary>
        public static long BuffersPlayed;

        // ------------------------------------------------------------------ the sound output
        static IAudioOut output;
        static Thread thread;
        static volatile bool running;

        /// <summary>Starts the sound thread on the platform's output (null: stay silent, for the checks and for a PC or
        /// phone without sound). Safe to call more than once.</summary>
        public static bool Start(IAudioOut device)
        {
            if (running || device == null) return Live;
            try
            {
                if (!device.Open(Rate, BufSamples)) { LastError = device.Error ?? "the sound output didn't open"; return false; }
                output = device;
                running = true; Live = true;
                thread = new Thread(Run) { IsBackground = true, Priority = ThreadPriority.Highest, Name = "Sahuarita Smash audio" };
                thread.Start();
                return true;
            }
            catch (Exception ex) { LastError = ex.Message; Live = false; return false; }
        }

        public static void Shutdown()
        {
            if (!running) return;
            running = false;
            try { thread.Join(500); } catch (Exception) { }
            try { output.Close(); } catch (Exception) { }
            Live = false;
        }

        static void Run()
        {
            var buf = new float[BufSamples];
            var pcm = new short[BufSamples];
            int failures = 0;
            bool loopLogged = false;
            // the output's Write waits while its queue is full, which paces this loop. A mistake while mixing must never
            // end the app (an exception on this thread would close it at once): it is written down, that block is
            // silent and the sound carries on; one that keeps happening turns the sound off instead.
            while (running)
            {
                try { Submit(buf, pcm, ref failures); }
                catch (Exception ex)
                {
                    if (!loopLogged) { loopLogged = true; CrashLog.Write("the sound", ex, false); }
                    failures++;
                    Thread.Sleep(10);
                }
                if (failures > 200) { CrashLog.Write("the sound (turned off after it kept failing)", new InvalidOperationException(LastError ?? "the mixer kept failing"), false); running = false; Live = false; }
            }
        }

        /// <summary>Tests: the next block mixed by MixSafely fails, as a bug in the mixer would.</summary>
        public static bool FailNextMixForTests;
        /// <summary>How many blocks failed to mix since the start (each written to the error log the first time).</summary>
        public static int MixFailures;
        /// <summary>Mixes a block as the sound thread does: a mistake is written down (once), the block is silence and the
        /// sounds playing are let go (the music carries on). Returns false when it failed. Never throws.</summary>
        public static bool MixSafely(float[] buf)
        {
            try
            {
                if (FailNextMixForTests) { FailNextMixForTests = false; throw new InvalidOperationException("a pretend mixing failure (test)"); }
                Fill(buf);
                return true;
            }
            catch (Exception ex)
            {
                if (MixFailures++ == 0) CrashLog.Write("mixing the sound", ex, false);
                LastError = ex.Message;
                Array.Clear(buf, 0, buf.Length);
                try { lock (sync) shots.Clear(); } catch (Exception) { }
                return false;
            }
        }

        static void Submit(float[] buf, short[] pcm, ref int failures)
        {
            if (MixSafely(buf)) failures = 0; else failures++;
            for (int k = 0; k < buf.Length; k++) pcm[k] = (short)(buf[k] * 32000);
            output.Write(pcm);
            BuffersPlayed++;
        }

        /// <summary>Mixes the next block of sound. Called by the audio thread (or by tests when there is no device).</summary>
        public static void Fill(float[] buf)
        {
            lock (sync)
            {
                double mv = MusicOn ? MusicVolume : 0, sv = SoundsOn ? SoundVolume : 0;
                for (int i = 0; i < buf.Length; i++)
                {
                    double duck = sting.Song != null ? 0.3 : 1.0;
                    double music;
                    if (mainStep == 0 && outgoing.Count == 0) music = main.Next() * duck;
                    else
                    {
                        if (mainStep != 0) { mainL += mainStep; if (mainL >= 1) { mainL = 1; mainStep = 0; } }
                        music = main.Next() * Ease(mainL) * duck;
                        double k = OutK;
                        for (int f = outgoing.Count - 1; f >= 0; f--)
                        {
                            Fader fd = outgoing[f];
                            if (k <= 0 || fd.P.Song == null) { Remember(fd.P); outgoing.RemoveAt(f); continue; }
                            music += fd.P.Next() * fd.G0 * k * duck;
                        }
                    }
                    music += sting.Next();
                    sampleClock++;
                    double fx = 0, hive = 0;
                    bool anyHive = false;
                    for (int s = shots.Count - 1; s >= 0; s--)
                    {
                        Shot sh = shots[s];
                        double v;
                        if (sh.Step != 0)
                        {
                            // a voice played a little higher or lower: read between the samples
                            int a = (int)sh.FPos; double fr = sh.FPos - a;
                            float x0 = sh.Data[a], x1 = a + 1 < sh.Data.Length ? sh.Data[a + 1] : 0f;
                            v = (x0 + (x1 - x0) * fr) * sh.Vol;
                        }
                        else v = sh.Data[sh.Pos] * sh.Vol;
                        if (sh.Glide) sh.Vol += (sh.VolT - sh.Vol) * 0.0015f;
                        if (sh.Group != 0)
                        {
                            // Hive Purge's voices: far away sounds duller (two gentle low-passes); a stolen one fades out
                            if (sh.Lp > 0) { sh.L1 += (float)((v - sh.L1) * sh.Lp); sh.L2 += (sh.L1 - sh.L2) * sh.Lp; v = sh.L2; }
                            if (sh.Fade > 0) { v *= sh.Fade / (double)StealFade; sh.Fade--; if (sh.Fade == 0) { shots.RemoveAt(s); continue; } }
                        }
                        if (sh.Music) music += v; else if (sh.Group != 0) { hive += v; anyHive = true; } else fx += v;
                        if (sh.Step != 0)
                        {
                            sh.FPos += sh.Step; sh.Pos = (int)sh.FPos;
                            if (sh.FPos >= sh.Data.Length - 1) { if (sh.Loop) { sh.FPos = 0; sh.Pos = 0; } else { shots.RemoveAt(s); continue; } }
                        }
                        else
                        {
                            sh.Pos++;
                            if (sh.Pos >= sh.Data.Length) { if (sh.Loop) sh.Pos = 0; else { shots.RemoveAt(s); continue; } }
                        }
                        shots[s] = sh;
                    }
                    if (anyHive || limG < 1)
                    {
                        // the limiter: quick to turn down a peak (about 0.1 ms), slow to come back (about 50 ms)
                        double lvl = Math.Abs(hive * sv), want = lvl > LimitAt ? LimitAt / lvl : 1;
                        limG += (want - limG) * (want < limG ? 0.3 : 0.0005);
                        if (limG > 0.99999) limG = 1;
                        fx += hive * limG;
                    }
                    double x = music * mv + fx * sv;
                    lp += (x - lp) * 0.7;                      // a gentle low-pass takes the edge off the squares
                    double y = lp;
                    if (y > 0.8) y = 0.8 + (y - 0.8) / (1 + (y - 0.8) * 4);
                    else if (y < -0.8) y = -0.8 + (y + 0.8) / (1 - (y + 0.8) * 4);
                    buf[i] = (float)Math.Max(-1, Math.Min(1, y));
                }
                if (TraceFades)
                {
                    double total = Ease(mainL), k = OutK;
                    foreach (Fader fd in outgoing) total += fd.G0 * k;
                    FadeTrace.Add(new[] { Ease(mainL), total, outgoing.Count, mainId });
                }
            }
        }

        static void Remember(SongPlayer p)
        {
            if (p.Song != null && p.Song.Loop) leftAt[p.Song] = new[] { p.Pos, sampleClock };
        }

        /// <summary>Changes the music smoothly: the tune playing fades out while the new one fades in over the given
        /// seconds. A looping tune heard in the last 90 seconds carries on where it was. Calling it again for the tune
        /// already playing (or coming in) does nothing.</summary>
        public static void Crossfade(Song s, double seconds)
        {
            lock (sync)
            {
                if (s == null) return;
                if (main.Song == s && !main.Paused) return;
                SongPlayer before = main;
                double step = 1.0 / Math.Max(1, seconds * Rate);
                // every tune now going out keeps its present level as its share
                double k = OutK;
                foreach (Fader fd in outgoing) fd.G0 *= k;
                if (main.Song != null && Ease(mainL) > 0.001) { outgoing.Add(new Fader { P = main, G0 = Ease(mainL) }); Remember(main); }
                mainId++;
                // a tune that was on its way out comes back from the level it is at
                Fader back = null;
                foreach (Fader fd in outgoing) if (fd.P.Song == s) back = fd;
                if (back != null) { outgoing.Remove(back); main = back.P; mainL = InvEase(Math.Min(1, back.G0)); }
                else
                {
                    main = new SongPlayer();
                    double[] at;
                    Song was = before.Song;
                    if (s.SyncGroup != null && was != null && was.SyncGroup == s.SyncGroup && was.Bpm == s.Bpm && !before.Paused) { main.Tempo = before.Tempo; main.StartSynced(s, before.Pos); }
                    else if (s.Loop && leftAt.TryGetValue(s, out at) && sampleClock - (long)at[1] < 90L * Rate && s.Steps > 0) main.StartAt(s, at[0] % s.Steps);
                    else main.Start(s);
                    mainL = 0;
                }
                // the rest share what's left of one tune's level (a quiet fourth one just stops: it can hardly be heard)
                while (outgoing.Count > 3) { Fader q = outgoing[0]; foreach (Fader x in outgoing) if (x.G0 < q.G0) q = x; Remember(q.P); outgoing.Remove(q); }
                double sum = 0; foreach (Fader fd in outgoing) sum += fd.G0;
                outBase = 1 - Ease(mainL);
                if (sum > 1e-6) foreach (Fader fd in outgoing) fd.G0 *= outBase / sum;
                mainStep = step;
            }
        }

        /// <summary>Tests: where the tune now playing is (in steps).</summary>
        public static double MusicPosition { get { lock (sync) return main.Song != null ? main.Pos : -1; } }
        /// <summary>Tests: how many tunes are fading out.</summary>
        public static int Fading { get { lock (sync) return outgoing.Count; } }

        /// <summary>For tests: mixes the given number of seconds without a device and returns the samples.</summary>
        public static float[] Render(double seconds)
        {
            var all = new float[(int)(seconds * Rate)];
            var buf = new float[BufSamples];
            for (int p = 0; p < all.Length; p += BufSamples)
            {
                Fill(buf);
                Array.Copy(buf, 0, all, p, Math.Min(BufSamples, all.Length - p));
            }
            return all;
        }

        // ------------------------------------------------------------------ music
        static void NoFades() { outgoing.Clear(); mainL = 1; mainStep = 0; outBase = 1; mainId++; }
        public static void Play(Song s) { lock (sync) { sting.Stop(); NoFades(); main.Tempo = 1; main.Start(s); } }
        public static void Play(Song s, double tempo) { lock (sync) { sting.Stop(); NoFades(); main.Start(s); main.Tempo = tempo; } }
        public static void SetTempo(double tempo) { lock (sync) main.Tempo = tempo; }
        public static void StopMusic() { lock (sync) { NoFades(); main.Stop(); sting.Stop(); for (int i = shots.Count - 1; i >= 0; i--) if (shots[i].Music) shots.RemoveAt(i); } }
        public static void PauseMusic(bool paused) { lock (sync) { main.Paused = paused; sting.Paused = paused; foreach (Fader fd in outgoing) fd.P.Paused = paused; } }
        /// <summary>A short tune over the music (which dips while it plays), e.g. a level-up.</summary>
        public static void Stinger(Song s) { lock (sync) sting.Start(s); }
        /// <summary>Stops the music and plays a tune once (game over, high score).</summary>
        public static void Jingle(Song s) { lock (sync) { sting.Stop(); NoFades(); main.Start(s); main.Tempo = 1; } }
        /// <summary>True while a stinger plays over the music.</summary>
        public static bool StingerPlaying { get { return sting.Song != null; } }
        public static Song Current { get { return main.Song; } }
        public static bool MusicPlaying { get { return main.Song != null; } }

        public static bool ToggleMusic() { MusicOn = !MusicOn; return MusicOn; }

        // ------------------------------------------------------------------ sound effects
        static float[] Get(string name)
        {
            float[] d;
            lock (cache) { if (cache.TryGetValue(name, out d)) return d; }
            d = ClassicGames.Sfx.Make(name);
            lock (cache) cache[name] = d;
            return d;
        }

        /// <summary>Plays a sound effect (see Sfx.Names). Unknown names are ignored.</summary>
        public static void Sfx(string name) { Sfx(name, 1.0); }
        public static void Sfx(string name, double volume) { Sfx(name, volume, 1.0); }
        /// <summary>A sound effect a little higher or lower (pitch 0.9-1.1, in five steps), so a sound heard again and
        /// again (digging) doesn't sound the same every time.</summary>
        public static void Sfx(string name, double volume, double pitch)
        {
            float[] d;
            int pk = (int)Math.Round((pitch - 1) / 0.03);
            pk = Math.Max(-3, Math.Min(3, pk));
            try { d = pk == 0 ? Get(name) : Pitched(name, pk); } catch (Exception) { return; }
            lock (sync)
            {
                // the same effect started again right away replaces the older copy (rapid clicks stay clean)
                for (int i = shots.Count - 1; i >= 0; i--) if (shots[i].Data == d && shots[i].Pos < 600) shots.RemoveAt(i);
                if (shots.Count > 24) shots.RemoveAt(0);
                shots.Add(new Shot { Data = d, Vol = (float)volume, Music = false });
            }
        }
        /// <summary>A sound that is one of a kind of voice (Hive Purge's creatures and machines): at most maxVoices of the
        /// same group sound at once (the oldest fades out quickly to make room), and muffle (0-1) dulls it with distance.</summary>
        public static void Voice(string name, double volume, double muffle, int group, int maxVoices) { Voice(name, volume, muffle, group, maxVoices, 1, 1); }
        /// <summary>The same, played at a pitch (1 = as made, 1.05 = 5% higher and shorter) and tone (1 = as made, less
        /// = a little duller), so a sound heard again and again never sounds quite the same.</summary>
        public static void Voice(string name, double volume, double muffle, int group, int maxVoices, double pitch, double tone)
        {
            float[] d;
            try { d = Get(name); } catch (Exception) { return; }
            if (d == null || d.Length < 2) return;
            if (group == 0) group = 1;
            float lp = muffle <= 0.01 ? 0 : (float)Math.Max(0.06, 1 - 0.92 * Math.Min(1, muffle));
            if (tone < 0.999) lp = lp > 0 ? (float)Math.Max(0.06, lp * tone) : (float)Math.Max(0.06, tone);
            float step = Math.Abs(pitch - 1) < 1e-4 ? 0f : (float)Math.Max(0.5, Math.Min(2, pitch));
            lock (sync)
            {
                int count = 0, oldest = -1;
                for (int i = 0; i < shots.Count; i++)
                    if (shots[i].Group == group && shots[i].Fade == 0)
                    {
                        count++;
                        if (oldest < 0 || shots[i].Pos > shots[oldest].Pos) oldest = i;
                    }
                if (count >= Math.Max(1, maxVoices) && oldest >= 0) { Shot o = shots[oldest]; o.Fade = StealFade; shots[oldest] = o; }
                if (shots.Count > 32)
                {
                    // too many at once: the quietest creature or machine voice goes first, gently
                    int q = -1;
                    for (int i = 0; i < shots.Count; i++) if (shots[i].Group != 0 && shots[i].Fade == 0 && (q < 0 || shots[i].Vol < shots[q].Vol)) q = i;
                    if (q >= 0) { Shot o = shots[q]; o.Fade = StealFade; shots[q] = o; } else shots.RemoveAt(0);
                }
                shots.Add(new Shot { Data = d, Vol = (float)volume, Music = false, Group = group, Lp = lp, Step = step });
            }
        }
        /// <summary>Changes a loop's volume smoothly (over about 15 ms), e.g. the minigun's motor as it spins up.</summary>
        public static void SetLoopVolume(int id, double volume)
        {
            if (id == 0) return;
            lock (sync)
                for (int i = 0; i < shots.Count; i++)
                    if (shots[i].Id == id) { Shot s = shots[i]; s.VolT = (float)volume; s.Glide = true; shots[i] = s; }
        }
        /// <summary>Tests: how many voices of a group are sounding (not counting ones fading out).</summary>
        public static int Voices(int group) { lock (sync) { int n = 0; foreach (Shot s in shots) if (s.Group == group && s.Fade == 0) n++; return n; } }
        /// <summary>Makes sounds ahead of time on a background thread, so the first time one plays doesn't stall the game.
        /// One thread, below normal priority, in the order asked (2026-10-24: Hive Purge's 155 takes take seconds to
        /// make, up to 0.8 s each; on a 4-core laptop they used to be made on the game's thread mid-fight, a freeze
        /// each time a new creature called out).</summary>
        public static void Preload(IList<string> names)
        {
            lock (makeQueue)
            {
                foreach (string n in names) if (n != null && !IsReady(n)) makeQueue.Enqueue(n);
                if (maker == null && makeQueue.Count > 0)
                {
                    maker = new Thread(MakeLoop) { IsBackground = true, Priority = ThreadPriority.BelowNormal, Name = "Classic Games sound maker" };
                    maker.Start();
                }
            }
        }
        static readonly Queue<string> makeQueue = new Queue<string>();
        static Thread maker;
        static void MakeLoop()
        {
            while (true)
            {
                string n;
                lock (makeQueue)
                {
                    if (makeQueue.Count == 0) { maker = null; return; }
                    n = makeQueue.Dequeue();
                }
                try { Get(n); } catch (Exception) { }
            }
        }
        /// <summary>The sound is made already (playing it now costs nothing).</summary>
        public static bool IsReady(string name) { lock (cache) return cache.ContainsKey(name); }
        /// <summary>Sounds still waiting to be made in the background.</summary>
        public static int Preloading { get { lock (makeQueue) return makeQueue.Count + (maker != null ? 1 : 0); } }

        static float[] Pitched(string name, int pk)
        {
            string key = name + "@" + pk;
            float[] d;
            lock (cache) { if (cache.TryGetValue(key, out d)) return d; }
            float[] src = Get(name);
            double f = 1 + pk * 0.03;
            d = new float[(int)(src.Length / f)];
            for (int i = 0; i < d.Length; i++)
            {
                double p = i * f; int a = (int)p; double fr = p - a;
                float x0 = src[Math.Min(src.Length - 1, a)], x1 = src[Math.Min(src.Length - 1, a + 1)];
                d[i] = (float)(x0 + (x1 - x0) * fr);
            }
            lock (cache) cache[key] = d;
            return d;
        }

        /// <summary>Menu sounds: move, select, back, pause, unpause.</summary>
        public static void Ui(string name) { Sfx("ui-" + name, 0.6); }

        /// <summary>A musical note on the music channel (Beat Runner's action notes, march beats). voice: lead, bell, bass, march.</summary>
        public static void Note(int midi, string voice, double volume)
        {
            float[] d;
            try { d = Get("note:" + voice + ":" + midi); } catch (Exception) { return; }
            lock (sync)
            {
                if (shots.Count > 24) shots.RemoveAt(0);
                shots.Add(new Shot { Data = d, Vol = (float)volume, Music = true });
            }
        }

        public static int StartLoop(string name, double volume)
        {
            float[] d;
            try { d = Get(name); } catch (Exception) { return 0; }
            lock (sync) { int id = nextId++; shots.Add(new Shot { Data = d, Vol = (float)volume, Loop = true, Id = id }); return id; }
        }
        public static void StopLoop(int id)
        {
            if (id == 0) return;
            lock (sync) for (int i = shots.Count - 1; i >= 0; i--) if (shots[i].Id == id) shots.RemoveAt(i);
        }
        public static void StopEffects() { lock (sync) for (int i = shots.Count - 1; i >= 0; i--) if (!shots[i].Music) shots.RemoveAt(i); }
    }

    /// <summary>The sound effects, made from short synthesized pieces.</summary>
    public static class Sfx
    {
        const int Rate = Audio.Rate;
        public static readonly string[] Names = new[] {
            "ui-move", "ui-select", "ui-back", "ui-pause", "ui-unpause", "tick",
            "rotate", "hold", "lock", "harddrop", "line1", "line2", "line3", "line4",
            "crash", "break", "spring", "block",
            "chomp1", "chomp2", "eatghost", "fruit", "death", "pellet",
            "eat", "bonus", "bump",
            "shoot", "alienboom", "playerboom", "ufo", "ufoboom",
            "paddle", "wall", "brick1", "brick2", "brick3", "brick4", "hard", "powerup", "lose", "extralife",
            "hop", "splat", "splash",
            "cat-boing", "cat-flip", "cat-boom", "cat-bigboom", "cat-pop", "cat-launch", "cat-throw", "cat-pea", "cat-bonk", "cat-swish",
            "cat-chirp1", "cat-chirp2", "cat-chirp3", "cat-sparkle", "cat-crate", "cat-fuse", "cat-puff", "cat-thwip", "cat-whistle",
            "cat-drop", "cat-thud", "cat-bounce", "cat-tick", "cat-plank", "cat-beetle", "cat-step",
            "kart-hop", "kart-drift", "kart-spark1", "kart-spark2", "kart-turbo", "kart-boost", "kart-bump", "kart-wall", "kart-chest",
            "kart-roll", "kart-item", "kart-throw", "kart-drop", "kart-shield", "kart-block", "kart-bull", "kart-firework", "kart-hit",
            "kart-fall", "kart-respawn", "kart-lap", "kart-finish", "kart-count", "kart-go", "kart-pop", "kart-out", "kart-paella",
            "kart-engine1", "kart-engine2", "kart-engine3",
            "kart-voice0", "kart-voice1", "kart-voice2", "kart-voice3", "kart-voice4", "kart-voice5", "kart-voice6", "kart-voice7",
            "kart-voice8", "kart-voice9", "kart-voice10", "kart-voice11",
            "bloom-boop", "bloom-boophit", "bloom-star", "bloom-rainbow", "bloom-glitter", "bloom-bubble", "bloom-pop", "bloom-sun", "bloom-sunhit",
            "bloom-glowcharge", "bloom-glowboom", "bloom-switchw", "bloom-door", "bloom-doorclose", "bloom-lift", "bloom-liftstop", "bloom-switch",
            "bloom-noway", "bloom-squish", "bloom-boing", "bloom-land", "bloom-pickup", "bloom-weapon", "bloom-key", "bloom-powerup", "bloom-secret",
            "bloom-grumble", "bloom-bossroar", "bloom-trollouch", "bloom-giggle1", "bloom-giggle2", "bloom-giggle3", "bloom-bossgiggle", "bloom-cheerpop",
            "bloom-pillow", "bloom-swipe", "bloom-pebble", "bloom-throw", "bloom-sneeze", "bloom-storm", "bloom-snowpop", "bloom-sneezepop", "bloom-grumppop",
            "bloom-ouch", "bloom-ouch2", "bloom-tired", "bloom-heehee",
            "space-dig", "space-thunk", "space-clink", "space-leaf", "space-jump", "space-swim", "space-jet", "space-land", "space-hook",
            "space-ouch", "space-pop", "space-beam", "space-gulp", "space-swing", "space-noenergy", "space-blast", "space-throw", "space-staff",
            "space-vita", "space-clunk", "space-place", "space-dooropen", "space-doorclose", "space-chest", "space-beep", "space-hit", "space-bosspop",
            "space-coin", "space-pickup", "space-chart", "space-alarm", "space-meteor", "space-bossroar", "space-zap", "space-rumble", "space-peep",
            "space-dash", "space-shards", "space-boing", "space-craft", "space-launch", "space-switchon", "space-scan", "space-levelup", "space-skillup" }.Concat(BrawlSfx.Names).ToArray();

        static float[] Buf(double seconds) { return new float[(int)(seconds * Rate)]; }
        /// <summary>A soft bell tick over the whole buffer: a sine with a faint, quicker octave, eased in (no click) and
        /// fading out to silence at the end.</summary>
        static void SoftTick(float[] b, double freq, double vol)
        {
            int n = b.Length;
            for (int i = 0; i < n; i++)
            {
                double t = i / (double)Rate;
                double env = (1 - Math.Exp(-t / 0.004)) * Math.Exp(-t * 34) * (1 - i / (double)n);
                double s = Math.Sin(2 * Math.PI * freq * t) + 0.22 * Math.Sin(2 * Math.PI * freq * 2 * t) * Math.Exp(-t * 40);
                b[i] += (float)(s * env * vol);
            }
        }

        // a square / triangle sweep from f0 to f1 with an exponential decay
        static void Sweep(float[] b, double start, double len, double f0, double f1, double duty, double decay, double vol, bool tri)
        {
            int s0 = (int)(start * Rate), n = (int)(len * Rate);
            double ph = 0;
            for (int i = 0; i < n && s0 + i < b.Length; i++)
            {
                double t = (double)i / Rate, k = t / len;
                double f = f0 * Math.Pow(f1 / f0, k);
                ph += f / Rate; ph -= Math.Floor(ph);
                double w = tri ? (ph < 0.5 ? 4 * ph - 1 : 3 - 4 * ph) : (ph < duty ? 1 : -1);
                double env = Math.Exp(-t * decay) * Math.Min(1, i / 60.0) * Math.Min(1, (n - i) / 200.0);
                b[s0 + i] += (float)(w * env * vol);
            }
        }
        static void Noise(float[] b, double start, double len, double decay, double vol, double smooth, int seed)
        {
            var r = new Random(seed);
            int s0 = (int)(start * Rate), n = (int)(len * Rate);
            double l = 0, hold = 0; int every = Math.Max(1, (int)smooth);
            for (int i = 0; i < n && s0 + i < b.Length; i++)
            {
                if (i % every == 0) hold = r.NextDouble() * 2 - 1;
                l += (hold - l) * 0.5;
                double t = (double)i / Rate;
                double env = Math.Exp(-t * decay) * Math.Min(1, (n - i) / 200.0);
                b[s0 + i] += (float)(l * env * vol);
            }
        }
        /// <summary>A soft triangle note with a rounded start (12 ms) and a long gentle fade, so it never clicks.</summary>
        static void Bell(float[] b, double start, double len, int midi, double vol)
        {
            double f = Song.NoteFreq(midi);
            int s0 = (int)(start * Rate), n = (int)(len * Rate), att = (int)(0.012 * Rate);
            for (int i = 0; i < n && s0 + i < b.Length; i++)
            {
                double ph = f * i / Rate; ph -= Math.Floor(ph);
                double tri = ph < 0.5 ? 4 * ph - 1 : 3 - 4 * ph;
                double t = (double)i / n;
                double env = Math.Min(1, (double)i / att) * Math.Pow(1 - t, 2.2);
                b[s0 + i] += (float)(tri * env * vol);
            }
        }
        static void Notes(float[] b, double start, double each, double duty, double vol, params int[] midis)
        {
            for (int i = 0; i < midis.Length; i++)
            {
                double f = Song.NoteFreq(midis[i]);
                Sweep(b, start + i * each, each * 1.6, f, f, duty, 9, vol, false);
            }
        }

        public static float[] Make(string name)
        {
            float[] b;
            if (name.StartsWith("note:")) return MakeNote(name);
            if (name.StartsWith("brawl-")) return BrawlSfx.Make(name);   // Sahuarita Smash's own (BrawlSfx.cs)
            switch (name)
            {
                // menu buttons: soft rounded triangle "tocks" (Andrew, 2026-10-19: "a little softer"; they were bright squares)
                // the hover / move sound (Andrew, 2026-10-29: "a little more pleasing"): a soft round bell tick, a sine at E5
                // with a faint octave, eased in over 4 ms and fading out (it was a triangle "tock")
                case "ui-move": b = Buf(0.12); SoftTick(b, 659.25, 0.15); break;
                case "ui-select": b = Buf(0.15); Sweep(b, 0, 0.06, 660, 660, 0.5, 22, 0.20, true); Sweep(b, 0.05, 0.10, 988, 988, 0.5, 26, 0.18, true); break;
                case "ui-back": b = Buf(0.14); Sweep(b, 0, 0.06, 587, 587, 0.5, 22, 0.19, true); Sweep(b, 0.05, 0.09, 440, 440, 0.5, 26, 0.18, true); break;
                case "ui-pause": b = Buf(0.22); Notes(b, 0, 0.06, 0.5, 0.14, 84, 79, 76, 72); break;
                case "ui-unpause": b = Buf(0.22); Notes(b, 0, 0.06, 0.5, 0.14, 72, 76, 79, 84); break;
                case "tick": b = Buf(0.03); Sweep(b, 0, 0.03, 1800, 1200, 0.5, 120, 0.07, false); Noise(b, 0, 0.015, 200, 0.05, 1, 3); break;
                case "rotate": b = Buf(0.06); Sweep(b, 0, 0.06, 700, 1100, 0.25, 45, 0.12, false); break;
                case "hold": b = Buf(0.14); Sweep(b, 0, 0.14, 300, 900, 0.5, 15, 0.12, false); break;
                case "lock": b = Buf(0.1); Sweep(b, 0, 0.1, 160, 90, 0.5, 30, 0.35, true); Noise(b, 0, 0.04, 80, 0.12, 4, 5); break;
                case "harddrop": b = Buf(0.16); Noise(b, 0, 0.16, 25, 0.25, 6, 9); Sweep(b, 0, 0.12, 220, 70, 0.5, 20, 0.35, true); break;
                case "line1": b = Buf(0.25); Notes(b, 0, 0.06, 0.5, 0.15, 76, 81); break;
                case "line2": b = Buf(0.3); Notes(b, 0, 0.06, 0.5, 0.15, 76, 81, 84); break;
                case "line3": b = Buf(0.35); Notes(b, 0, 0.06, 0.5, 0.15, 76, 81, 84, 88); break;
                case "line4": b = Buf(0.55); Notes(b, 0, 0.055, 0.25, 0.16, 72, 76, 79, 84, 88, 91, 96); break;
                case "crash": b = Buf(0.55); Noise(b, 0, 0.5, 6, 0.4, 3, 11); Sweep(b, 0, 0.45, 600, 80, 0.5, 5, 0.18, false); break;
                case "break": b = Buf(0.3); Noise(b, 0, 0.3, 12, 0.45, 8, 13); Noise(b, 0, 0.08, 40, 0.3, 1, 14); break;
                case "spring": b = Buf(0.32); Sweep(b, 0, 0.32, 200, 1400, 0.25, 4, 0.14, false); break;
                case "block": b = Buf(0.15); Sweep(b, 0, 0.15, 1600, 900, 0.125, 18, 0.14, false); break;
                case "chomp1": b = Buf(0.07); Sweep(b, 0, 0.07, 500, 250, 0.5, 10, 0.3, true); break;
                case "chomp2": b = Buf(0.07); Sweep(b, 0, 0.07, 280, 520, 0.5, 10, 0.3, true); break;
                case "pellet": b = Buf(0.2); Sweep(b, 0, 0.2, 300, 900, 0.5, 6, 0.3, true); break;
                case "eatghost": b = Buf(0.4); Sweep(b, 0, 0.4, 180, 1800, 0.5, 2, 0.3, true); break;
                case "fruit": b = Buf(0.4); Notes(b, 0, 0.05, 0.5, 0.14, 79, 83, 86, 91, 95); break;
                case "death":
                    b = Buf(1.3);
                    for (int i = 0; i < 9; i++) Sweep(b, i * 0.12, 0.12, 900 - i * 80, 500 - i * 45, 0.5, 4, 0.13, false);
                    Sweep(b, 1.08, 0.2, 200, 60, 0.5, 6, 0.15, false);
                    break;
                case "eat": b = Buf(0.1); Sweep(b, 0, 0.1, 500, 1000, 0.5, 18, 0.14, false); break;
                case "bonus": b = Buf(0.3); Notes(b, 0, 0.05, 0.25, 0.14, 84, 88, 91, 96); break;
                case "bump": b = Buf(0.1); Sweep(b, 0, 0.1, 150, 80, 0.5, 30, 0.3, true); break;
                case "shoot": b = Buf(0.18); Sweep(b, 0, 0.18, 1400, 250, 0.125, 10, 0.13, false); break;
                case "alienboom": b = Buf(0.25); Noise(b, 0, 0.25, 14, 0.4, 5, 21); break;
                case "playerboom": b = Buf(0.9); Noise(b, 0, 0.9, 4, 0.45, 12, 22); Sweep(b, 0, 0.6, 300, 50, 0.5, 4, 0.2, true); break;
                case "ufo":
                    {
                        b = Buf(0.32);
                        double ph = 0;
                        for (int i = 0; i < b.Length; i++)
                        {
                            double t = (double)i / Rate, f = 520 + 260 * Math.Sin(2 * Math.PI * t / 0.32);
                            ph += f / Rate; ph -= Math.Floor(ph);
                            b[i] = (float)((ph < 0.5 ? 4 * ph - 1 : 3 - 4 * ph) * 0.15);
                        }
                        break;
                    }
                case "ufoboom": b = Buf(0.7); for (int i = 0; i < 6; i++) Sweep(b, i * 0.1, 0.12, 1600 - i * 150, 1200 - i * 120, 0.25, 8, 0.12, false); Noise(b, 0, 0.3, 10, 0.3, 4, 23); break;
                case "paddle": b = Buf(0.08); Sweep(b, 0, 0.08, 440, 440, 0.5, 30, 0.3, true); break;
                case "wall": b = Buf(0.05); Sweep(b, 0, 0.05, 330, 330, 0.5, 40, 0.25, true); break;
                case "brick1": b = Buf(0.08); Sweep(b, 0, 0.08, 523, 523, 0.25, 30, 0.13, false); break;
                case "brick2": b = Buf(0.08); Sweep(b, 0, 0.08, 659, 659, 0.25, 30, 0.13, false); break;
                case "brick3": b = Buf(0.08); Sweep(b, 0, 0.08, 784, 784, 0.25, 30, 0.13, false); break;
                case "brick4": b = Buf(0.08); Sweep(b, 0, 0.08, 1047, 1047, 0.25, 30, 0.13, false); break;
                case "hard": b = Buf(0.1); Sweep(b, 0, 0.1, 2400, 2300, 0.125, 25, 0.1, false); Sweep(b, 0, 0.1, 3100, 3000, 0.5, 30, 0.05, false); break;
                case "powerup": b = Buf(0.35); Notes(b, 0, 0.045, 0.25, 0.14, 72, 79, 84, 88, 91, 96); break;
                case "lose": b = Buf(0.5); Sweep(b, 0, 0.5, 700, 110, 0.5, 3, 0.15, false); break;
                case "hop": b = Buf(0.07); Sweep(b, 0, 0.07, 320, 720, 0.25, 25, 0.12, false); break;
                case "splat": b = Buf(0.35); Noise(b, 0, 0.3, 14, 0.4, 5, 31); Sweep(b, 0, 0.25, 260, 60, 0.5, 8, 0.3, true); break;
                case "splash": b = Buf(0.6); Noise(b, 0, 0.6, 5, 0.35, 2, 32); Noise(b, 0.05, 0.4, 9, 0.2, 9, 33); break;
                case "extralife": b = Buf(0.5); Notes(b, 0, 0.07, 0.5, 0.14, 84, 88, 91, 96, 91, 96); break;
                // Caterpillar Catastrophe
                case "cat-boing": b = Buf(0.22); Sweep(b, 0, 0.22, 180, 720, 0.5, 8, 0.32, true); break;
                case "cat-flip": b = Buf(0.35); Sweep(b, 0, 0.35, 260, 1300, 0.25, 4, 0.12, false); Sweep(b, 0.05, 0.25, 200, 800, 0.5, 6, 0.2, true); break;
                case "cat-boom": b = Buf(0.8); Noise(b, 0, 0.8, 5, 0.42, 10, 41); Sweep(b, 0, 0.5, 160, 40, 0.5, 5, 0.3, true); break;
                case "cat-bigboom": b = Buf(1.3); Noise(b, 0, 1.3, 3, 0.45, 16, 42); Noise(b, 0, 0.15, 25, 0.2, 2, 43); Sweep(b, 0, 0.9, 120, 30, 0.5, 3, 0.3, true); break;
                case "cat-pop": b = Buf(0.25); Noise(b, 0, 0.25, 16, 0.35, 4, 44); Sweep(b, 0, 0.12, 400, 120, 0.5, 18, 0.2, true); break;
                case "cat-launch": b = Buf(0.45); Noise(b, 0, 0.45, 6, 0.2, 2, 45); Sweep(b, 0, 0.4, 300, 900, 0.5, 5, 0.08, false); break;
                case "cat-throw": b = Buf(0.18); Noise(b, 0, 0.18, 14, 0.18, 3, 46); Sweep(b, 0, 0.15, 500, 250, 0.5, 12, 0.08, false); break;
                case "cat-pea": b = Buf(0.12); Sweep(b, 0, 0.1, 900, 300, 0.5, 25, 0.18, true); Noise(b, 0, 0.05, 60, 0.2, 1, 47); break;
                case "cat-bonk": b = Buf(0.2); Sweep(b, 0, 0.2, 520, 260, 0.5, 14, 0.35, true); Noise(b, 0, 0.03, 80, 0.2, 1, 48); break;
                case "cat-swish": b = Buf(0.15); Noise(b, 0, 0.15, 18, 0.16, 2, 49); break;
                case "cat-chirp1": b = Buf(0.16); Sweep(b, 0, 0.07, 1200, 1700, 0.25, 12, 0.11, false); Sweep(b, 0.08, 0.08, 1500, 2100, 0.25, 12, 0.11, false); break;
                case "cat-chirp2": b = Buf(0.2); Sweep(b, 0, 0.1, 1800, 1100, 0.25, 10, 0.11, false); Sweep(b, 0.1, 0.1, 1400, 1900, 0.25, 10, 0.11, false); break;
                case "cat-chirp3": b = Buf(0.22); Notes(b, 0, 0.05, 0.25, 0.1, 88, 91, 86, 93); break;
                case "cat-sparkle": b = Buf(0.5); Notes(b, 0, 0.04, 0.125, 0.1, 96, 100, 103, 108, 103, 108); break;
                case "cat-crate": b = Buf(0.4); Notes(b, 0, 0.06, 0.5, 0.14, 79, 84, 88, 91); break;
                case "cat-fuse": b = Buf(0.5); Noise(b, 0, 0.5, 2, 0.12, 1, 50); break;
                case "cat-puff": b = Buf(0.5); Noise(b, 0, 0.3, 10, 0.2, 6, 51); Notes(b, 0.05, 0.07, 0.25, 0.1, 84, 79, 76, 88); break;
                case "cat-thwip": b = Buf(0.15); Sweep(b, 0, 0.15, 2000, 500, 0.25, 15, 0.12, false); break;
                case "cat-whistle": b = Buf(0.9); Sweep(b, 0, 0.9, 1800, 600, 0.5, 1, 0.1, true); break;
                case "cat-drop": b = Buf(0.6); Sweep(b, 0, 0.6, 1400, 300, 0.5, 2, 0.12, true); break;
                case "cat-thud": b = Buf(0.18); Sweep(b, 0, 0.18, 140, 60, 0.5, 18, 0.4, true); Noise(b, 0, 0.06, 40, 0.15, 6, 52); break;
                case "cat-bounce": b = Buf(0.1); Sweep(b, 0, 0.1, 300, 160, 0.5, 30, 0.3, true); break;
                case "cat-tick": b = Buf(0.06); Sweep(b, 0, 0.06, 1600, 1600, 0.5, 40, 0.12, false); break;
                case "cat-plank": b = Buf(0.25); Sweep(b, 0, 0.08, 200, 150, 0.5, 30, 0.35, true); Sweep(b, 0.1, 0.08, 220, 160, 0.5, 30, 0.35, true); break;
                case "cat-beetle": b = Buf(0.3); for (int i = 0; i < 4; i++) Sweep(b, i * 0.07, 0.05, 700 + i * 90, 900 + i * 90, 0.25, 30, 0.1, false); break;
                case "cat-step": b = Buf(0.04); Sweep(b, 0, 0.04, 260, 200, 0.5, 60, 0.18, true); break;
                // Jose Cart 67
                case "kart-hop": b = Buf(0.12); Sweep(b, 0, 0.12, 300, 650, 0.5, 14, 0.25, true); break;
                case "kart-drift": b = Buf(0.35); Noise(b, 0, 0.35, 5, 0.16, 2, 61); Sweep(b, 0, 0.3, 900, 700, 0.125, 6, 0.04, false); break;
                case "kart-spark1": b = Buf(0.15); Notes(b, 0, 0.04, 0.125, 0.09, 88, 93); break;
                case "kart-spark2": b = Buf(0.2); Notes(b, 0, 0.04, 0.125, 0.1, 91, 96, 100); break;
                case "kart-turbo": b = Buf(0.5); Noise(b, 0, 0.5, 4, 0.22, 2, 62); Sweep(b, 0, 0.45, 200, 900, 0.5, 3, 0.08, false); break;
                case "kart-boost": b = Buf(0.7); Noise(b, 0, 0.7, 3, 0.25, 3, 63); Sweep(b, 0, 0.6, 150, 600, 0.5, 2.5, 0.09, false); break;
                case "kart-bump": b = Buf(0.15); Sweep(b, 0, 0.15, 180, 90, 0.5, 20, 0.35, true); Noise(b, 0, 0.05, 50, 0.15, 3, 64); break;
                case "kart-wall": b = Buf(0.2); Noise(b, 0, 0.2, 18, 0.3, 4, 65); Sweep(b, 0, 0.15, 140, 70, 0.5, 18, 0.3, true); break;
                case "kart-chest": b = Buf(0.35); Noise(b, 0, 0.06, 40, 0.2, 2, 66); Notes(b, 0.04, 0.05, 0.25, 0.11, 84, 88, 91, 96); break;
                case "kart-roll": b = Buf(0.05); Sweep(b, 0, 0.05, 1500, 1500, 0.25, 50, 0.07, false); break;
                case "kart-item": b = Buf(0.3); Notes(b, 0, 0.06, 0.5, 0.12, 79, 84, 91); break;
                case "kart-throw": b = Buf(0.22); Noise(b, 0, 0.2, 14, 0.18, 2, 67); Sweep(b, 0, 0.2, 700, 300, 0.5, 10, 0.07, false); break;
                case "kart-drop": b = Buf(0.25); Sweep(b, 0, 0.25, 500, 140, 0.5, 9, 0.25, true); break;
                case "kart-shield":
                    b = Buf(0.5);
                    for (int i = 0; i < 6; i++) { Noise(b, i * 0.07, 0.04, 60, 0.25, 1, 68 + i); Sweep(b, i * 0.07, 0.04, 2400, 2200, 0.5, 60, 0.05, false); }
                    break;
                case "kart-block": b = Buf(0.3); Sweep(b, 0, 0.3, 1800, 1700, 0.125, 9, 0.1, false); Noise(b, 0, 0.05, 50, 0.2, 1, 75); break;
                case "kart-bull": b = Buf(0.9); Sweep(b, 0, 0.5, 110, 150, 0.5, 2, 0.35, true); Sweep(b, 0.3, 0.6, 150, 95, 0.5, 2, 0.35, true); Noise(b, 0, 0.9, 3, 0.12, 6, 76); break;
                case "kart-firework": b = Buf(1.0); Sweep(b, 0, 0.45, 600, 2400, 0.5, 1, 0.07, true); Noise(b, 0.45, 0.5, 7, 0.4, 3, 77); Notes(b, 0.5, 0.05, 0.125, 0.06, 96, 100, 103, 108); break;
                case "kart-hit": b = Buf(0.55); Noise(b, 0, 0.12, 30, 0.3, 3, 78); for (int i = 0; i < 4; i++) Sweep(b, 0.05 + i * 0.1, 0.1, 900 - i * 120, 700 - i * 120, 0.25, 10, 0.09, false); break;
                case "kart-fall": b = Buf(0.9); Sweep(b, 0, 0.9, 900, 120, 0.5, 1.5, 0.12, true); break;
                case "kart-respawn": b = Buf(0.5); Notes(b, 0, 0.08, 0.25, 0.1, 72, 76, 79, 84); break;
                case "kart-lap": b = Buf(0.3); Notes(b, 0, 0.07, 0.5, 0.13, 84, 84, 91); break;
                case "kart-finish": b = Buf(0.6); Notes(b, 0, 0.06, 0.25, 0.13, 79, 84, 88, 91, 96); break;
                case "kart-count": b = Buf(0.3); Sweep(b, 0, 0.3, 660, 660, 0.5, 5, 0.16, false); break;
                case "kart-go": b = Buf(0.6); Sweep(b, 0, 0.6, 1320, 1320, 0.5, 3, 0.16, false); break;
                case "kart-pop": b = Buf(0.2); Noise(b, 0, 0.2, 25, 0.4, 1, 79); break;
                case "kart-out": b = Buf(0.6); Sweep(b, 0, 0.6, 600, 150, 0.5, 3, 0.14, false); break;
                case "kart-paella": b = Buf(0.5); for (int i = 0; i < 5; i++) Sweep(b, i * 0.1, 0.1, 900, 1300, 0.25, 15, 0.07, false); break;
                case "kart-engine1": b = Engine(70); break;
                case "kart-engine2": b = Engine(95); break;
                case "kart-engine3": b = Engine(125); break;
                // Bloom: soft, toy-like sounds (boops, twinkles, bubble pops, giggles); nothing scary
                case "bloom-boop": b = Buf(0.16); Sweep(b, 0, 0.16, 520, 300, 0.5, 14, 0.3, true); break;
                case "bloom-boophit": b = Buf(0.2); Sweep(b, 0, 0.18, 300, 700, 0.5, 10, 0.28, true); Notes(b, 0.05, 0.05, 0.25, 0.07, 88, 93); break;
                case "bloom-star": b = Buf(0.22); Sweep(b, 0, 0.12, 1700, 900, 0.25, 18, 0.1, false); Notes(b, 0.03, 0.045, 0.125, 0.06, 96, 103); break;
                case "bloom-rainbow": b = Buf(0.5); Noise(b, 0, 0.2, 14, 0.16, 2, 101); Notes(b, 0.02, 0.05, 0.25, 0.08, 84, 88, 91, 96, 100); break;
                case "bloom-glitter": b = Buf(0.09); Sweep(b, 0, 0.07, 2400, 1500, 0.25, 30, 0.07, false); Noise(b, 0, 0.05, 50, 0.08, 1, 102); break;
                case "bloom-bubble": b = Buf(0.35); Sweep(b, 0, 0.3, 180, 520, 0.5, 6, 0.32, true); break;
                case "bloom-pop": b = Buf(0.4); Sweep(b, 0, 0.1, 900, 260, 0.5, 20, 0.3, true); Noise(b, 0, 0.3, 12, 0.2, 2, 103); Notes(b, 0.08, 0.05, 0.25, 0.06, 91, 96, 100); break;
                case "bloom-sun": b = Buf(0.08); Sweep(b, 0, 0.07, 1300, 1900, 0.125, 25, 0.07, false); break;
                case "bloom-sunhit": b = Buf(0.12); Sweep(b, 0, 0.1, 1600, 800, 0.25, 25, 0.06, false); break;
                case "bloom-glowcharge": b = Buf(0.9); Sweep(b, 0, 0.85, 200, 1600, 0.5, 1.5, 0.12, true); Notes(b, 0.5, 0.06, 0.25, 0.05, 84, 88, 91, 96); break;
                case "bloom-glowboom": b = Buf(1.2); Noise(b, 0, 1.0, 3, 0.25, 6, 104); Notes(b, 0, 0.07, 0.25, 0.08, 72, 76, 79, 84, 88, 91, 96, 100); break;
                case "bloom-switchw": b = Buf(0.12); Sweep(b, 0, 0.05, 500, 700, 0.5, 30, 0.12, false); Sweep(b, 0.06, 0.05, 700, 900, 0.5, 30, 0.12, false); break;
                case "bloom-door": b = Buf(0.6); Noise(b, 0, 0.55, 3, 0.14, 8, 105); Sweep(b, 0, 0.5, 140, 220, 0.5, 2, 0.12, true); break;
                case "bloom-doorclose": b = Buf(0.6); Noise(b, 0, 0.5, 3, 0.14, 8, 106); Sweep(b, 0, 0.5, 220, 120, 0.5, 2, 0.12, true); Sweep(b, 0.45, 0.12, 110, 70, 0.5, 20, 0.25, true); break;
                case "bloom-lift": b = Buf(0.5); Sweep(b, 0, 0.5, 90, 110, 0.5, 1, 0.18, true); Noise(b, 0, 0.5, 1, 0.05, 20, 107); break;
                case "bloom-liftstop": b = Buf(0.15); Sweep(b, 0, 0.15, 160, 90, 0.5, 20, 0.3, true); break;
                case "bloom-switch": b = Buf(0.25); Sweep(b, 0, 0.04, 1200, 1200, 0.5, 40, 0.12, false); Notes(b, 0.05, 0.06, 0.5, 0.1, 79, 86); break;
                case "bloom-noway": b = Buf(0.3); Sweep(b, 0, 0.12, 330, 300, 0.5, 8, 0.2, true); Sweep(b, 0.14, 0.14, 260, 220, 0.5, 8, 0.2, true); break;
                case "bloom-squish": b = Buf(0.25); Sweep(b, 0, 0.22, 240, 120, 0.5, 8, 0.25, true); Noise(b, 0, 0.1, 30, 0.1, 4, 108); break;
                case "bloom-boing": b = Buf(0.45); Sweep(b, 0, 0.42, 150, 800, 0.5, 4, 0.3, true); Sweep(b, 0, 0.42, 300, 1600, 0.25, 6, 0.05, false); break;
                case "bloom-land": b = Buf(0.1); Sweep(b, 0, 0.1, 140, 70, 0.5, 30, 0.3, true); break;
                case "bloom-pickup": b = Buf(0.18); Notes(b, 0, 0.05, 0.25, 0.1, 88, 93, 100); break;
                case "bloom-weapon": b = Buf(0.5); Notes(b, 0, 0.06, 0.5, 0.11, 79, 83, 86, 91, 95, 98); break;
                case "bloom-key": b = Buf(0.6); Notes(b, 0, 0.08, 0.125, 0.1, 91, 98, 103, 110); break;
                case "bloom-powerup": b = Buf(0.6); Notes(b, 0, 0.05, 0.25, 0.1, 72, 79, 84, 88, 91, 96, 100, 103); break;
                case "bloom-secret": b = Buf(1.0); Notes(b, 0, 0.09, 0.125, 0.09, 84, 91, 96, 100, 103, 108, 103, 108); Noise(b, 0.5, 0.4, 6, 0.04, 1, 109); break;
                case "bloom-grumble": b = Buf(0.45); Sweep(b, 0, 0.2, 180, 140, 0.5, 4, 0.22, true); Sweep(b, 0.2, 0.22, 160, 120, 0.5, 5, 0.22, true); break;
                case "bloom-bossroar": b = Buf(0.9); Sweep(b, 0, 0.85, 90, 70, 0.5, 1.5, 0.32, true); Sweep(b, 0, 0.85, 135, 105, 0.25, 2, 0.06, false); break;
                case "bloom-trollouch": b = Buf(0.2); Sweep(b, 0, 0.18, 700, 450, 0.5, 8, 0.18, true); break;
                case "bloom-giggle1": b = Buf(0.6); for (int i = 0; i < 5; i++) Sweep(b, i * 0.1, 0.08, 700 + i * 60, 900 + i * 60, 0.5, 10, 0.16, true); break;
                case "bloom-giggle2": b = Buf(0.6); for (int i = 0; i < 4; i++) Sweep(b, i * 0.12, 0.09, 520 + i * 40, 760 + i * 40, 0.25, 9, 0.1, false); break;
                case "bloom-giggle3": b = Buf(0.6); for (int i = 0; i < 6; i++) Sweep(b, i * 0.08, 0.06, 1100 - i * 50, 1400 - i * 50, 0.5, 12, 0.14, true); break;
                case "bloom-bossgiggle": b = Buf(1.4); for (int i = 0; i < 8; i++) Sweep(b, i * 0.15, 0.12, 220 + (i % 2) * 60, 330 + (i % 2) * 60, 0.5, 6, 0.3, true); break;
                case "bloom-cheerpop": b = Buf(0.5); Noise(b, 0, 0.12, 30, 0.2, 1, 110); Notes(b, 0.04, 0.05, 0.25, 0.08, 91, 95, 98, 103); break;
                case "bloom-pillow": b = Buf(0.25); Noise(b, 0, 0.22, 12, 0.28, 6, 111); break;
                case "bloom-swipe": b = Buf(0.18); Noise(b, 0, 0.16, 16, 0.2, 2, 112); Sweep(b, 0, 0.12, 600, 300, 0.5, 14, 0.06, false); break;
                case "bloom-pebble": b = Buf(0.15); Sweep(b, 0, 0.1, 900, 500, 0.5, 20, 0.12, true); Noise(b, 0, 0.04, 60, 0.12, 1, 113); break;
                case "bloom-throw": b = Buf(0.22); Noise(b, 0, 0.2, 12, 0.16, 3, 114); Sweep(b, 0, 0.18, 400, 650, 0.5, 8, 0.07, false); break;
                case "bloom-sneeze": b = Buf(0.5); Sweep(b, 0, 0.22, 500, 900, 0.5, 2, 0.12, true); Noise(b, 0.22, 0.25, 10, 0.3, 2, 115); break;
                case "bloom-storm": b = Buf(0.8); Noise(b, 0, 0.75, 2, 0.2, 10, 116); Sweep(b, 0, 0.7, 120, 80, 0.5, 2, 0.15, true); break;
                case "bloom-snowpop": b = Buf(0.2); Noise(b, 0, 0.18, 18, 0.25, 3, 117); break;
                case "bloom-sneezepop": b = Buf(0.25); Noise(b, 0, 0.22, 14, 0.2, 2, 118); Sweep(b, 0, 0.1, 800, 500, 0.5, 20, 0.05, false); break;
                case "bloom-grumppop": b = Buf(0.35); Noise(b, 0, 0.3, 9, 0.3, 5, 119); Sweep(b, 0, 0.2, 260, 120, 0.5, 9, 0.2, true); break;
                case "bloom-ouch": b = Buf(0.22); Sweep(b, 0, 0.2, 620, 420, 0.5, 8, 0.16, true); break;
                case "bloom-ouch2": b = Buf(0.35); Sweep(b, 0, 0.14, 700, 560, 0.5, 6, 0.18, true); Sweep(b, 0.15, 0.18, 520, 380, 0.5, 6, 0.18, true); break;
                case "bloom-tired": b = Buf(1.2); Sweep(b, 0, 1.1, 520, 180, 0.5, 1.2, 0.18, true); Notes(b, 0.3, 0.2, 0.5, 0.06, 79, 76, 72); break;
                case "bloom-heehee": b = Buf(0.35); for (int i = 0; i < 3; i++) Sweep(b, i * 0.1, 0.08, 900 + i * 100, 1200 + i * 100, 0.5, 12, 0.12, true); break;
                // Spaceareum
                case "space-dig": b = Buf(0.08); Noise(b, 0, 0.07, 45, 0.22, 3, 201); Sweep(b, 0, 0.05, 260, 150, 0.5, 40, 0.12, true); break;
                case "space-thunk": b = Buf(0.1); Sweep(b, 0, 0.09, 200, 110, 0.5, 30, 0.28, true); Noise(b, 0, 0.04, 70, 0.1, 5, 202); break;
                case "space-clink": b = Buf(0.12); Sweep(b, 0, 0.1, 1800, 1500, 0.25, 35, 0.08, false); Sweep(b, 0.01, 0.08, 2600, 2400, 0.5, 40, 0.04, false); break;
                case "space-leaf": b = Buf(0.1); Noise(b, 0, 0.09, 30, 0.12, 1, 203); break;
                case "space-jump": b = Buf(0.12); Sweep(b, 0, 0.11, 300, 620, 0.5, 14, 0.12, false); break;
                case "space-swim": b = Buf(0.25); Noise(b, 0, 0.22, 10, 0.12, 8, 204); Sweep(b, 0, 0.2, 200, 420, 0.5, 8, 0.08, true); break;
                case "space-jet": b = Buf(0.12); Noise(b, 0, 0.11, 8, 0.1, 2, 205); break;
                case "space-land": b = Buf(0.08); Sweep(b, 0, 0.07, 150, 80, 0.5, 40, 0.22, true); break;
                case "space-hook": b = Buf(0.18); Sweep(b, 0, 0.16, 900, 1500, 0.25, 12, 0.07, false); Noise(b, 0, 0.1, 30, 0.06, 1, 206); break;
                case "space-ouch": b = Buf(0.22); Sweep(b, 0, 0.2, 640, 400, 0.5, 9, 0.16, true); break;
                case "space-pop": b = Buf(0.3); Sweep(b, 0, 0.08, 1000, 300, 0.5, 25, 0.28, true); Noise(b, 0, 0.18, 20, 0.16, 2, 207); Notes(b, 0.06, 0.04, 0.25, 0.06, 91, 96, 100); break;
                case "space-beam": b = Buf(0.6); Notes(b, 0, 0.06, 0.5, 0.08, 72, 76, 79, 84, 88, 91, 96); Sweep(b, 0, 0.55, 200, 1600, 0.125, 2, 0.05, false); break;
                case "space-gulp": b = Buf(0.3); Sweep(b, 0, 0.12, 300, 500, 0.5, 10, 0.18, true); Sweep(b, 0.14, 0.12, 320, 540, 0.5, 10, 0.18, true); break;
                case "space-swing": b = Buf(0.14); Noise(b, 0, 0.13, 14, 0.16, 2, 208); Sweep(b, 0, 0.12, 700, 300, 0.5, 16, 0.04, false); break;
                case "space-noenergy": b = Buf(0.2); Sweep(b, 0, 0.09, 330, 300, 0.5, 8, 0.12, true); Sweep(b, 0.1, 0.09, 260, 230, 0.5, 8, 0.12, true); break;
                case "space-blast": b = Buf(0.16); Sweep(b, 0, 0.15, 1500, 300, 0.25, 14, 0.1, false); Noise(b, 0, 0.05, 60, 0.06, 1, 209); break;
                case "space-throw": b = Buf(0.2); Noise(b, 0, 0.18, 12, 0.14, 3, 210); Sweep(b, 0, 0.16, 400, 700, 0.5, 8, 0.06, false); break;
                case "space-staff": b = Buf(0.3); Sweep(b, 0, 0.28, 500, 1200, 0.5, 6, 0.1, true); Notes(b, 0.02, 0.05, 0.25, 0.05, 84, 91, 96); break;
                case "space-vita": b = Buf(0.8); Notes(b, 0, 0.08, 0.25, 0.1, 76, 81, 84, 88, 93, 96); break;
                case "space-clunk": b = Buf(0.12); Sweep(b, 0, 0.1, 160, 140, 0.5, 30, 0.22, true); Noise(b, 0, 0.05, 80, 0.12, 6, 211); break;
                case "space-place": b = Buf(0.07); Sweep(b, 0, 0.06, 420, 260, 0.5, 40, 0.18, true); break;
                case "space-dooropen": b = Buf(0.25); Sweep(b, 0, 0.22, 220, 380, 0.5, 6, 0.14, true); Noise(b, 0, 0.2, 10, 0.05, 6, 212); break;
                case "space-doorclose": b = Buf(0.25); Sweep(b, 0, 0.18, 380, 200, 0.5, 6, 0.14, true); Sweep(b, 0.18, 0.06, 140, 90, 0.5, 30, 0.2, true); break;
                case "space-chest": b = Buf(0.3); Sweep(b, 0, 0.12, 250, 330, 0.5, 10, 0.16, true); Notes(b, 0.12, 0.05, 0.25, 0.07, 84, 88, 91); break;
                case "space-beep": b = Buf(0.16); Sweep(b, 0, 0.06, 1320, 1320, 0.5, 15, 0.09, false); Sweep(b, 0.07, 0.07, 1760, 1760, 0.5, 15, 0.09, false); break;
                case "space-hit": b = Buf(0.1); Sweep(b, 0, 0.09, 700, 250, 0.5, 25, 0.18, true); Noise(b, 0, 0.04, 70, 0.08, 1, 213); break;
                case "space-bosspop": b = Buf(1.4); Noise(b, 0, 1.2, 3, 0.28, 5, 214); Notes(b, 0.1, 0.08, 0.25, 0.08, 72, 76, 79, 84, 88, 91, 96, 100, 103, 108); break;
                case "space-coin": b = Buf(0.18); Sweep(b, 0, 0.05, 1976, 1976, 0.5, 20, 0.08, false); Sweep(b, 0.05, 0.12, 2637, 2637, 0.5, 18, 0.08, false); break;
                case "space-pickup": b = Buf(0.1); Sweep(b, 0, 0.08, 800, 1300, 0.25, 22, 0.07, false); break;
                case "space-chart": b = Buf(0.9); Notes(b, 0, 0.1, 0.125, 0.09, 79, 84, 88, 91, 96, 100, 103); break;
                case "space-alarm": b = Buf(0.9); for (int i = 0; i < 3; i++) { Sweep(b, i * 0.3, 0.14, 880, 880, 0.5, 4, 0.1, false); Sweep(b, i * 0.3 + 0.14, 0.14, 660, 660, 0.5, 4, 0.1, false); } break;
                case "space-meteor": b = Buf(0.8); Sweep(b, 0, 0.5, 1500, 200, 0.25, 3, 0.07, false); Noise(b, 0.45, 0.35, 8, 0.26, 6, 215); break;
                case "space-bossroar": b = Buf(1.0); Sweep(b, 0, 0.95, 110, 70, 0.5, 1.4, 0.3, true); Sweep(b, 0, 0.9, 165, 105, 0.25, 2, 0.05, false); break;
                case "space-zap": b = Buf(0.14); Sweep(b, 0, 0.13, 1200, 2200, 0.25, 18, 0.06, false); break;
                case "space-rumble": b = Buf(0.4); Noise(b, 0, 0.38, 4, 0.16, 20, 216); break;
                case "space-peep": b = Buf(0.2); Sweep(b, 0, 0.18, 1600, 2400, 0.5, 10, 0.06, false); break;
                case "space-dash": b = Buf(0.3); Noise(b, 0, 0.28, 6, 0.16, 2, 217); Sweep(b, 0, 0.25, 300, 900, 0.5, 5, 0.06, true); break;
                case "space-shards": b = Buf(0.4); for (int i = 0; i < 6; i++) Sweep(b, i * 0.04, 0.12, 2000 + i * 150, 1500 + i * 150, 0.5, 20, 0.05, false); break;
                case "space-boing": b = Buf(0.2); Sweep(b, 0, 0.18, 200, 700, 0.5, 8, 0.16, true); break;
                case "space-craft": b = Buf(0.45); Noise(b, 0, 0.12, 30, 0.12, 4, 218); Notes(b, 0.1, 0.06, 0.25, 0.08, 84, 88, 91, 96); break;
                case "space-launch": b = Buf(1.4); Noise(b, 0, 1.35, 1.5, 0.26, 6, 219); Sweep(b, 0, 1.3, 80, 300, 0.5, 1, 0.2, true); break;
                case "space-switchon": b = Buf(0.35); Sweep(b, 0, 0.05, 300, 300, 0.5, 30, 0.2, true); Notes(b, 0.06, 0.07, 0.25, 0.08, 79, 86); break;
                case "space-scan": b = Buf(0.45); Sweep(b, 0, 0.4, 600, 2400, 0.125, 2, 0.06, false); Sweep(b, 0.2, 0.2, 2400, 600, 0.125, 4, 0.04, false); break;
                // a level-up: a gentle rising arpeggio on soft triangles that settles on a held chord (its own little tune)
                case "space-levelup": b = Buf(1.5); Bell(b, 0, 0.5, 67, 0.13); Bell(b, 0.11, 0.5, 71, 0.13); Bell(b, 0.22, 0.5, 74, 0.13); Bell(b, 0.36, 1.05, 79, 0.15); Bell(b, 0.36, 1.05, 83, 0.09); Bell(b, 0.36, 1.05, 86, 0.06); break;
                // a skill level: three soft notes going up
                case "space-skillup": b = Buf(0.9); Bell(b, 0, 0.4, 76, 0.12); Bell(b, 0.1, 0.4, 81, 0.12); Bell(b, 0.2, 0.62, 88, 0.12); break;
                default:
                    if (name.StartsWith("kart-voice")) { b = Voice(int.Parse(name.Substring(10), CultureInfo.InvariantCulture)); break; }
                    throw new ArgumentException("unknown sound " + name);
            }
            if (name.StartsWith("space-")) b = Soften(b, name == "space-launch" || name == "space-bossroar" || name == "space-alarm" ? 0.2 : 0.16);
            return b;
        }

        /// <summary>Spaceareum's gentler sound effects: a low-pass filter (takes the hiss off noise and the buzz off
        /// squares), a 2 ms fade in and 15 ms fade out (no clicks), a faint 110 ms echo, and no louder than peak.</summary>
        public static float[] Soften(float[] b, double peak)
        {
            int echo = (int)(0.11 * Rate), tail = (int)(0.02 * Rate);
            var o = new float[b.Length + echo + tail];
            double lp1 = 0, lp2 = 0;
            for (int i = 0; i < o.Length; i++)
            {
                double x = i < b.Length ? b[i] : 0;
                lp1 += (x - lp1) * 0.3; lp2 += (lp1 - lp2) * 0.45;       // two gentle poles, about 2-3 kHz
                o[i] = (float)lp2;
            }
            for (int i = o.Length - 1; i >= echo; i--) o[i] += o[i - echo] * 0.16f;
            int fin = (int)(0.002 * Rate), fout = (int)(0.015 * Rate);
            for (int i = 0; i < fin && i < o.Length; i++) o[i] *= (float)i / fin;
            for (int i = 0; i < fout && i < o.Length; i++) o[o.Length - 1 - i] *= (float)i / fout;
            float m = 0;
            foreach (float v in o) m = Math.Max(m, Math.Abs(v));
            if (m > peak) { float k = (float)(peak / m); for (int i = 0; i < o.Length; i++) o[i] *= k; }
            return o;
        }

        /// <summary>A kart engine's hum that loops without a click (a whole number of cycles).</summary>
        static float[] Engine(double f)
        {
            int cycles = (int)Math.Round(0.4 * f);
            int n = (int)Math.Round(cycles * Rate / f);
            var b = new float[n];
            for (int i = 0; i < n; i++)
            {
                double ph = (double)i * cycles / n;
                double p = ph - Math.Floor(ph);
                double saw = 2 * p - 1, sq = p < 0.3 ? 1 : -1;
                double ph2 = (double)i * cycles * 2 / n; double p2 = ph2 - Math.Floor(ph2);
                b[i] = (float)((saw * 0.6 + sq * 0.4) * 0.07 + (p2 < 0.5 ? 0.02 : -0.02));
            }
            return b;
        }

        /// <summary>Each racer's little synthesized "voice" (no recorded voices). The coqui says co-QUI.</summary>
        static float[] Voice(int ch)
        {
            float[] b = Buf(0.45);
            switch (ch)
            {
                case 9: Sweep(b, 0, 0.09, 1050, 1000, 0.5, 6, 0.13, false); Sweep(b, 0.13, 0.22, 1500, 2300, 0.5, 5, 0.13, false); break;   // co-QUI
                case 6: Sweep(b, 0, 0.42, 130, 105, 0.5, 2.5, 0.3, true); break;                                                          // a low friendly moo
                case 3: Sweep(b, 0, 0.18, 700, 900, 0.25, 4, 0.1, false); Sweep(b, 0.2, 0.22, 420, 330, 0.25, 4, 0.12, false); break;     // hee-haw
                case 11: Sweep(b, 0, 0.3, 500, 900, 0.5, 4, 0.1, false); Sweep(b, 0.15, 0.25, 900, 600, 0.25, 6, 0.06, false); break;   // a lynx's mrrow
                case 10: for (int i = 0; i < 3; i++) Sweep(b, i * 0.1, 0.08, 500 + i * 200, 900 + i * 200, 0.5, 12, 0.18, true); break;  // blub-blub
                default:
                    {
                        // people: a cheerful three-note "words" pattern, pitched for each racer
                        int[] baseNote = { 74, 79, 69, 0, 64, 62, 0, 67, 81 };
                        int m = baseNote[Math.Max(0, Math.Min(8, ch))];
                        Notes(b, 0, 0.09, 0.25, 0.1, m, m + 4, m + 7);
                        break;
                    }
            }
            return b;
        }

        // "note:voice:midi": a single plucked note for music played by the game itself
        static float[] MakeNote(string name)
        {
            string[] p = name.Split(':');
            string voice = p[1]; int midi = int.Parse(p[2], CultureInfo.InvariantCulture);
            double f = Song.NoteFreq(midi);
            float[] b;
            switch (voice)
            {
                case "bell": b = Buf(0.6); Sweep(b, 0, 0.6, f, f, 0.125, 6, 0.12, false); Sweep(b, 0, 0.4, f * 2, f * 2, 0.5, 9, 0.05, false); break;
                case "bass": b = Buf(0.22); Sweep(b, 0, 0.22, f, f, 0.5, 6, 0.42, true); break;
                case "march": b = Buf(0.16); Sweep(b, 0, 0.16, f, f * 0.97, 0.5, 9, 0.5, true); break;
                default: b = Buf(0.42); Sweep(b, 0, 0.42, f, f, 0.25, 5, 0.15, false); break;   // lead
            }
            return b;
        }
    }
}

namespace ClassicGames
{
    /// <summary>Where the mixed sound goes: one per platform (Windows: waveOut; Android: AudioTrack).</summary>
    public interface IAudioOut
    {
        /// <summary>Opens the device for 16-bit mono at this rate, blocks of this many samples. False: no sound.</summary>
        bool Open(int rate, int blockSamples);
        /// <summary>Queues one block; waits while the device's queue is full (that paces the sound thread).</summary>
        void Write(short[] block);
        void Close();
        /// <summary>Why Open failed (null when it didn't).</summary>
        string Error { get; }
    }
}
