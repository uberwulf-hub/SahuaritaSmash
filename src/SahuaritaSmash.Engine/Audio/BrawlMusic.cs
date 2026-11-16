// Classic Games - Sahuarita Smash!, part 8: recorded music (2026-10-29, Andrew made a song for the menu, each stage and the
// victory in Suno). The songs live in Music\Brawl next to ClassicGames.ps1 as role.mp3 (menu, mesa, copper, server, moon,
// lab, victory) with music.txt saying where each loops and how loud it plays (tools\Import-BrawlMusic.ps1 writes both).
// Windows' own Media Foundation decodes them on a background thread (nothing to install); until a song is ready, or
// when it's missing or can't be decoded, the game plays its built-in tune, then crossfades to the recorded one.
// Decoded songs are kept as 16-bit mono at their own rate (a three-minute song is about 17 MB); only the last few
// asked for are kept. Tests never decode the real files: they hand-make songs with Song.FromPcm.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace ClassicGames
{
    public static class BrawlMusic
    {
        public static readonly string[] Roles = { "menu", "mesa", "copper", "server", "moon", "lab", "orchard", "victory", "select", "results", "defeat", "hold" };   // select, results, defeat, hold: built-in tunes until there are songs (Music.BrawlFill)
        /// <summary>Where the songs are (a folder decoded by Windows' Media Foundation, as in App Hub; null = none, the
        /// built-in tunes play). Source, when set, takes its place (Android: the app's assets and the phone's decoder).</summary>
        public static string Folder;
        public static IMusicSource Source;
        /// <summary>Tests and snapshots: never decode real files.</summary>
        public static bool Enabled = true;

        public sealed class Entry { public string Role, File; public double LoopStart, LoopEnd, Gain = 1; }

        static readonly object sync = new object();
        static Dictionary<string, Entry> index;
        static readonly Dictionary<string, Song> ready = new Dictionary<string, Song>();
        static readonly List<string> order = new List<string>();
        static readonly HashSet<string> loading = new HashSet<string>(), failed = new HashSet<string>();
        /// <summary>How many decoded songs are kept: all of them (App Hub kept 3, so going back to a screen often meant
        /// decoding its song again, with the built-in tune playing meanwhile and then crossfading: Andrew, 2026-11-15,
        /// "the music doesn't seem to transition quite right"). Loops are trimmed at their end, which nothing plays.</summary>
        public const int Keep = 12;
        /// <summary>The order songs are decoded in when the game starts: the menu first, then the rest.</summary>
        public static readonly string[] PreloadOrder = { "menu", "select", "mesa", "copper", "server", "moon", "lab", "orchard", "victory", "defeat", "results", "hold" };

        static IMusicSource Current { get { return Source ?? (string.IsNullOrEmpty(Folder) ? null : new FolderMusic(Folder)); } }

        /// <summary>The stage's song role.</summary>
        public static string RoleFor(BStageDef s) { return s.Id; }

        /// <summary>music.txt: "role file loopStart loopEnd gain" a line ("#" comments; seconds; loopEnd 0 = plays once).</summary>
        public static Dictionary<string, Entry> ReadIndex(string text)
        {
            var d = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);
            foreach (string raw in (text ?? "").Split('\n'))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;
                string[] p = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (p.Length < 2) continue;
                var e = new Entry { Role = p[0], File = p[1] };
                double v;
                if (p.Length > 2 && double.TryParse(p[2], NumberStyles.Float, CultureInfo.InvariantCulture, out v)) e.LoopStart = v;
                if (p.Length > 3 && double.TryParse(p[3], NumberStyles.Float, CultureInfo.InvariantCulture, out v)) e.LoopEnd = v;
                if (p.Length > 4 && double.TryParse(p[4], NumberStyles.Float, CultureInfo.InvariantCulture, out v)) e.Gain = Math.Max(0.05, Math.Min(4, v));
                if (e.File.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || e.File.IndexOfAny(new[] { '\\', '/', ':' }) >= 0 || e.File.Contains("..")) continue;   // a name, never a path (\ and : named too: Android and Linux allow them in names)
                d[e.Role] = e;
            }
            return d;
        }

        static Entry Find(string role)
        {
            IMusicSource src = Current;
            if (!Enabled || src == null) return null;
            if (index == null)
            {
                try { string text = src.ReadIndex(); index = text != null ? ReadIndex(text) : new Dictionary<string, Entry>(); }
                catch (Exception) { index = new Dictionary<string, Entry>(); }
            }
            Entry e;
            return index.TryGetValue(role, out e) && src.Exists(e.File) ? e : null;
        }

        /// <summary>The recorded song for this role if it is decoded; otherwise null, and it starts decoding it (in the
        /// background) when there is one.</summary>
        public static Song Get(string role)
        {
            lock (sync)
            {
                Song s;
                if (ready.TryGetValue(role, out s)) { order.Remove(role); order.Add(role); return s; }
                if (loading.Contains(role) || failed.Contains(role)) return null;
            }
            Entry e = Find(role);
            if (e == null) return null;
            lock (sync) loading.Add(role);
            var t = new Thread(() => Load(e)) { IsBackground = true, Name = "Sahuarita Smash! music", Priority = ThreadPriority.BelowNormal };
            t.Start();
            return null;
        }
        /// <summary>Decodes every song there is, one after another on one background thread (menu first), so changing
        /// screens and stages never waits for one. Safe to call more than once.</summary>
        public static void PreloadAll()
        {
            var todo = new List<Entry>();
            lock (sync)
            {
                foreach (string r in PreloadOrder)
                {
                    if (ready.ContainsKey(r) || loading.Contains(r) || failed.Contains(r)) continue;
                    Entry e = Find(r);
                    if (e == null) continue;
                    loading.Add(r); todo.Add(e);
                }
            }
            if (todo.Count == 0) return;
            var t = new Thread(() => { foreach (Entry e in todo) Load(e); }) { IsBackground = true, Name = "Sahuarita Smash! music", Priority = ThreadPriority.BelowNormal };
            t.Start();
        }
        /// <summary>Decoded songs kept now (the checks).</summary>
        public static int ReadyCount { get { lock (sync) return ready.Count; } }

        /// <summary>Starts decoding these (the next stage, the victory) so they are ready in time.</summary>
        public static void Want(params string[] roles) { foreach (string r in roles) Get(r); }
        public static bool Has(string role) { return Find(role) != null; }

        static void Load(Entry e)
        {
            Song s = null;
            try
            {
                int rate;
                short[] pcm = Current.DecodeMono16(e.File, out rate);
                bool loop = e.LoopEnd > e.LoopStart + 1;
                int a = loop ? (int)(e.LoopStart * rate) : 0, b = loop ? Math.Min(pcm.Length, (int)(e.LoopEnd * rate)) : pcm.Length;
                if (loop && b < pcm.Length) Array.Resize(ref pcm, b);   // after the loop's end nothing ever plays
                s = Song.FromPcm("Sahuarita Smash: " + e.Role, pcm, rate, loop, a, b, e.Gain);
            }
            catch (Exception ex) { try { CrashLog.Write("Sahuarita Smash! music " + e.Role, ex, false); } catch (Exception) { } }
            lock (sync)
            {
                loading.Remove(e.Role);
                if (s == null) { failed.Add(e.Role); return; }
                ready[e.Role] = s; order.Remove(e.Role); order.Add(e.Role);
                while (order.Count > Keep) { ready.Remove(order[0]); order.RemoveAt(0); }
            }
        }

        /// <summary>The seconds around a recorded song's loop as it really plays (the import tool's preview, and the
        /// tests): from 'before' seconds ahead of the loop's end, through the seam, on for 'after' seconds.</summary>
        public static float[] RenderSeam(Song s, double before, double after)
        {
            var p = new SongPlayer();
            p.StartAt(s, Math.Max(0, s.LoopB - before * s.PcmRate));
            int n = (int)((before + after) * Audio.Rate);
            var o = new float[n];
            for (int i = 0; i < n; i++) o[i] = (float)p.Next();
            return o;
        }

        /// <summary>A 16-bit mono WAV file (the import tool's previews).</summary>
        public static void WriteWav(string path, float[] a, int rate)
        {
            using (var w = new BinaryWriter(File.Create(path)))
            {
                w.Write(new[] { 'R', 'I', 'F', 'F' }); w.Write(36 + a.Length * 2); w.Write(new[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
                w.Write(16); w.Write((short)1); w.Write((short)1); w.Write(rate); w.Write(rate * 2); w.Write((short)2); w.Write((short)16);
                w.Write(new[] { 'd', 'a', 't', 'a' }); w.Write(a.Length * 2);
                foreach (float v in a) w.Write((short)Math.Max(-32767, Math.Min(32767, v * 32767)));
            }
        }

        /// <summary>The import tool: a decoded song made playable with its loop (no game, no sound device).</summary>
        public static Song Build(string role, short[] pcm, int rate, double loopStart, double loopEnd, double gain)
        {
            bool loop = loopEnd > loopStart + 1;
            return Song.FromPcm("Sahuarita Smash: " + role, pcm, rate, loop, (int)(loopStart * rate), loop ? (int)(loopEnd * rate) : pcm.Length, gain);
        }

        /// <summary>Tests: forget everything (and the index).</summary>
        public static void Reset() { lock (sync) { ready.Clear(); order.Clear(); loading.Clear(); failed.Clear(); index = null; } }
        /// <summary>Tests: put a hand-made song in place of a decoded one.</summary>
        public static void Put(string role, Song s) { lock (sync) { ready[role] = s; order.Remove(role); order.Add(role); } }
    }

    /// <summary>Finds where a song can loop (tools\Import-BrawlMusic.ps1 runs it once and writes the answer in music.txt):
    /// a start after the intro and an end before the song's ending whose rhythm (the loudness and its jumps, 10 ms at a
    /// time) matches best for three seconds either side, then lined up to the sample on the waveform. Also how loud to
    /// play it so every song sits at the same level.</summary>
    public static class BrawlLoop
    {
        public sealed class Result { public double Start, End, Score, Gain, Rms, Peak, Bpm, Wave; }
        public const double TargetRms = 0.13;

        public static Result Find(short[] pcm, int rate, bool loop) { return Find(pcm, rate, loop, 0); }

        /// <summary>expectedBpm: the tempo the song was asked for (0 = any): the beat is looked for within 8% of it, so a
        /// song can't be heard at two thirds or half its speed.</summary>
        public static Result Find(short[] pcm, int rate, bool loop, double expectedBpm)
        {
            int hop = Math.Max(1, rate / 100), n = pcm.Length / hop;
            // three bands (bass, middle, treble) through simple filters, their loudness every 10 ms
            var bands = new double[3][];
            for (int b = 0; b < 3; b++) bands[b] = new double[n];
            double k1 = 1 - Math.Exp(-2 * Math.PI * 200.0 / rate), k2 = 1 - Math.Exp(-2 * Math.PI * 2000.0 / rate);
            double l1 = 0, l2 = 0, peak = 0;
            var all = new double[n];
            for (int k = 0; k < n; k++)
            {
                double s0 = 0, s1 = 0, s2 = 0, sa = 0;
                for (int j = 0; j < hop; j++)
                {
                    double x = pcm[k * hop + j] / 32768.0;
                    peak = Math.Max(peak, Math.Abs(x));
                    l1 += (x - l1) * k1; l2 += (x - l2) * k2;
                    double lo = l1, mid = l2 - l1, hi = x - l2;
                    s0 += lo * lo; s1 += mid * mid; s2 += hi * hi; sa += x * x;
                }
                bands[0][k] = Math.Sqrt(s0 / hop); bands[1][k] = Math.Sqrt(s1 / hop); bands[2][k] = Math.Sqrt(s2 / hop); all[k] = Math.Sqrt(sa / hop);
            }
            var r = new Result { Peak = peak };
            if (!loop || n < 3000)
            {
                double s2 = 0; foreach (double e in all) s2 += e * e;
                r.Rms = Math.Sqrt(s2 / Math.Max(1, n));
                r.Gain = Gain(r.Rms, peak);
                return r;
            }
            // features: each band's loudness and its rises (beats), squashed (log) so loud and quiet parts both count
            var feat = new double[6][];
            for (int b = 0; b < 3; b++)
            {
                feat[b] = new double[n]; feat[b + 3] = new double[n];
                for (int k = 1; k < n; k++)
                {
                    feat[b][k] = Math.Log(1e-4 + bands[b][k]);
                    feat[b + 3][k] = Math.Max(0, Math.Log(1e-4 + bands[b][k]) - Math.Log(1e-4 + bands[b][k - 1]));
                }
            }
            // the beat: the lag (70-180 beats a minute) where the rises repeat best
            var onset = new double[n];
            for (int k = 0; k < n; k++) onset[k] = feat[3][k] + feat[4][k] + feat[5][k];
            double bestAc = double.MinValue, period = 50;
            double lagLo = expectedBpm > 0 ? 6000 / (expectedBpm * 1.08) : 34, lagHi = expectedBpm > 0 ? 6000 / (expectedBpm * 0.92) : 86;
            for (double lag = lagLo; lag <= lagHi; lag += 0.02)
            {
                double ac = 0; int L = (int)lag; double fr = lag - L;
                for (int k = 0; k + L + 1 < n; k += 2) ac += onset[k] * (onset[k + L] * (1 - fr) + onset[k + L + 1] * fr);
                ac /= Math.Pow(lag, 0.15);   // a slight lean toward the faster (the half-tempo match is nearly as good)
                if (ac > bestAc) { bestAc = ac; period = lag; }
            }
            r.Bpm = 6000.0 / period;
            double bar = period * 4;
            // where the music really ends (before a fade-out or the final chord): the last second still at least half as loud as usual
            var sm = new double[n];
            double acc = 0;
            for (int k = 0; k < n; k++) { acc += all[k]; if (k >= 100) acc -= all[k - 100]; sm[k] = acc / Math.Min(k + 1, 100); }
            var sorted = (double[])sm.Clone(); Array.Sort(sorted);
            double median = sorted[n / 2];
            int endF = n - 1;
            while (endF > n / 2 && sm[endF] < median * 0.8) endF--;
            const int W = 300;
            int a0 = 300, a1 = Math.Min(2400, n / 3), bMin = Math.Max(a1 + 1500, endF - 1600), bMax = endF - 150;
            if (bMax <= bMin) bMin = Math.Max(a0 + 1500, bMax - 800);
            double best = double.MinValue; int ba = a0, bb = bMax;
            for (int a = a0; a <= a1; a++)
            {
                if (a - W < 0) continue;
                // only whole bars apart, so the beat carries on across the seam
                for (int kb = (int)Math.Ceiling((bMin - a) / bar); a + kb * bar <= bMax; kb++)
                {
                    int b = (int)Math.Round(a + kb * bar);
                    if (b + W >= n || b < bMin) continue;
                    double sc = 0;
                    for (int f = 0; f < 6; f++) sc += Ncc(feat[f], a - W, b - W, 2 * W);
                    sc /= 6;
                    sc += (b - a) / (double)n * 0.05;   // a longer loop is better when it's nearly as good
                    if (sc > best) { best = sc; ba = a; bb = b; }
                }
            }
            // the end, a few hundredths either way, where the rhythm lines up best (the tempo is never exact)
            { double bs = double.MinValue; int b2 = bb; for (int d = -4; d <= 4; d++) { int b = bb + d; if (b + W >= n) continue; double sc = 0; for (int f = 3; f < 6; f++) sc += Ncc1(feat[f], ba - W, b - W, 2 * W); if (sc > bs) { bs = sc; b2 = b; } } bb = b2; }
            // line it up to the sample: the start shifted so the waveforms just before the two points match best
            int A = ba * hop, B = bb * hop, len = Math.Min(4096, A - 1), bestD = 0; double bestC = double.MinValue;
            for (int d = -hop; d <= hop; d++)
            {
                int a = A + d;
                if (a - len < 0 || a >= pcm.Length) continue;
                double c = 0, ea = 0, eb = 0;
                for (int j = 1; j <= len; j++) { double x = pcm[a - j], y = pcm[B - j]; c += x * y; ea += x * x; eb += y * y; }
                c /= Math.Sqrt(ea * eb) + 1e-9;
                if (c > bestC) { bestC = c; bestD = d; }
            }
            r.Start = (A + bestD) / (double)rate; r.End = B / (double)rate; r.Score = best; r.Wave = bestC;
            double s3 = 0; for (int k = ba; k < bb; k++) s3 += all[k] * all[k];
            r.Rms = Math.Sqrt(s3 / Math.Max(1, bb - ba));
            r.Gain = Gain(r.Rms, peak);
            return r;
        }

        static double Gain(double rms, double peak) { return Math.Max(0.1, Math.Min(Math.Min(3, TargetRms / Math.Max(1e-4, rms)), 0.92 / Math.Max(1e-4, peak))); }

        /// <summary>Ncc on every frame (Ncc looks at every other one).</summary>
        static double Ncc1(double[] f, int i, int j, int len)
        {
            double ma = 0, mb = 0;
            for (int k = 0; k < len; k++) { ma += f[i + k]; mb += f[j + k]; }
            ma /= len; mb /= len;
            double c = 0, ea = 0, eb = 0;
            for (int k = 0; k < len; k++) { double x = f[i + k] - ma, y = f[j + k] - mb; c += x * y; ea += x * x; eb += y * y; }
            return c / (Math.Sqrt(ea * eb) + 1e-12);
        }

        /// <summary>Normalised correlation of f[i..i+len) with f[j..j+len).</summary>
        static double Ncc(double[] f, int i, int j, int len)
        {
            double ma = 0, mb = 0;
            for (int k = 0; k < len; k += 2) { ma += f[i + k]; mb += f[j + k]; }
            ma /= len / 2; mb /= len / 2;
            double c = 0, ea = 0, eb = 0;
            for (int k = 0; k < len; k += 2) { double x = f[i + k] - ma, y = f[j + k] - mb; c += x * y; ea += x * x; eb += y * y; }
            return c / (Math.Sqrt(ea * eb) + 1e-12);
        }
    }

    // ------------------------------------------------------------------ Media Foundation (just what decoding needs)
    // Classic COM interop doesn't inherit: each interface lists its base's methods first (IMFAttributes' 30), as
    // placeholders where they aren't called. The same as tools\HiveSampleTool.cs.
    [ComImport, Guid("44ae0fa8-ea31-4109-8d2e-4cae4997c555"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IMFMediaTypeB
    {
        void GetItem(); void GetItemType(); void CompareItem(); void Compare();
        [PreserveSig] int GetUINT32([In] ref Guid key, out int value);
        void GetUINT64(); void GetDouble(); void GetGUID(); void GetStringLength(); void GetString(); void GetAllocatedString();
        void GetBlobSize(); void GetBlob(); void GetAllocatedBlob(); void GetUnknown(); void SetItem(); void DeleteItem(); void DeleteAllItems();
        [PreserveSig] int SetUINT32([In] ref Guid key, int value);
        void SetUINT64(); void SetDouble();
        [PreserveSig] int SetGUID([In] ref Guid key, [In] ref Guid value);
        void SetString(); void SetBlob(); void SetUnknown(); void LockStore(); void UnlockStore(); void GetCount(); void GetItemByIndex(); void CopyAllItems();
        void GetMajorType(); void IsCompressedFormat(); void IsEqual(); void GetRepresentation(); void FreeRepresentation();
    }

    [ComImport, Guid("045FA593-8799-42b8-BC8D-8968C6453507"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IMFMediaBufferB
    {
        [PreserveSig] int Lock(out IntPtr buffer, out int maxLength, out int currentLength);
        [PreserveSig] int Unlock();
        void GetCurrentLength(); void SetCurrentLength(); void GetMaxLength();
    }

    [ComImport, Guid("c40a00f2-b93a-4d80-ae8c-5a1c634f58e4"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IMFSampleB
    {
        void GetItem(); void GetItemType(); void CompareItem(); void Compare(); void GetUINT32(); void GetUINT64(); void GetDouble(); void GetGUID();
        void GetStringLength(); void GetString(); void GetAllocatedString(); void GetBlobSize(); void GetBlob(); void GetAllocatedBlob(); void GetUnknown();
        void SetItem(); void DeleteItem(); void DeleteAllItems(); void SetUINT32(); void SetUINT64(); void SetDouble(); void SetGUID(); void SetString();
        void SetBlob(); void SetUnknown(); void LockStore(); void UnlockStore(); void GetCount(); void GetItemByIndex(); void CopyAllItems();
        void GetSampleFlags(); void SetSampleFlags(); void GetSampleTime(); void SetSampleTime(); void GetSampleDuration(); void SetSampleDuration();
        void GetBufferCount(); void GetBufferByIndex();
        [PreserveSig] int ConvertToContiguousBuffer(out IMFMediaBufferB buffer);
    }

    [ComImport, Guid("70ae66f2-c809-4e4f-8915-bdcb406b7993"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IMFSourceReaderB
    {
        void GetStreamSelection();
        [PreserveSig] int SetStreamSelection(int streamIndex, [MarshalAs(UnmanagedType.Bool)] bool selected);
        void GetNativeMediaType();
        [PreserveSig] int GetCurrentMediaType(int streamIndex, out IMFMediaTypeB mediaType);
        [PreserveSig] int SetCurrentMediaType(int streamIndex, IntPtr reserved, IMFMediaTypeB mediaType);
        void SetCurrentPosition();
        [PreserveSig] int ReadSample(int streamIndex, int controlFlags, out int actualStreamIndex, out int streamFlags, out long timestamp, out IMFSampleB sample);
    }

    public static class MfDecoder
    {
        [DllImport("mfplat.dll")] static extern int MFStartup(int version, int flags);
        [DllImport("mfplat.dll")] static extern int MFShutdown();
        [DllImport("mfplat.dll")] static extern int MFCreateMediaType(out IMFMediaTypeB type);
        [DllImport("mfreadwrite.dll", CharSet = CharSet.Unicode)] static extern int MFCreateSourceReaderFromURL(string url, IntPtr attributes, out IMFSourceReaderB reader);

        const int MfVersion = 0x00020070, FirstAudio = unchecked((int)0xFFFFFFFD), AllStreams = unchecked((int)0xFFFFFFFE), EndOfStream = 2;
        static Guid MajorType = new Guid("48eba18e-f8c9-4687-bf11-0a74c9f96a8f"), SubType = new Guid("f7e34c9a-42e8-4714-b74b-cb29d72c35e5");
        static Guid AudioType = new Guid("73647561-0000-0010-8000-00AA00389B71"), Float = new Guid("00000003-0000-0010-8000-00AA00389B71");
        static Guid Channels = new Guid("37e48bf5-645e-4c5b-89de-ada9e29b696a"), SampleRate = new Guid("5faeeae7-0290-4c31-9e8a-c534f68d9dba");

        static void Ok(int hr, string what) { if (hr < 0) throw new IOException(what + " failed: 0x" + hr.ToString("X8")); }

        /// <summary>Decodes an audio file (MP3) with Media Foundation to 16-bit mono at its own rate.</summary>
        public static short[] DecodeMono16(string path, out int rate)
        {
            Ok(MFStartup(MfVersion, 0), "MFStartup");
            try
            {
                IMFSourceReaderB reader;
                Ok(MFCreateSourceReaderFromURL(Path.GetFullPath(path), IntPtr.Zero, out reader), "Opening " + Path.GetFileName(path));
                try
                {
                    Ok(reader.SetStreamSelection(AllStreams, false), "SetStreamSelection");
                    Ok(reader.SetStreamSelection(FirstAudio, true), "SetStreamSelection");
                    IMFMediaTypeB want;
                    Ok(MFCreateMediaType(out want), "MFCreateMediaType");
                    Ok(want.SetGUID(ref MajorType, ref AudioType), "SetGUID");
                    Ok(want.SetGUID(ref SubType, ref Float), "SetGUID");
                    Ok(reader.SetCurrentMediaType(FirstAudio, IntPtr.Zero, want), "SetCurrentMediaType");
                    IMFMediaTypeB got;
                    Ok(reader.GetCurrentMediaType(FirstAudio, out got), "GetCurrentMediaType");
                    int ch;
                    Ok(got.GetUINT32(ref Channels, out ch), "channels");
                    Ok(got.GetUINT32(ref SampleRate, out rate), "sample rate");
                    ch = Math.Max(1, ch);
                    var o = new List<short>(rate * 200);
                    float[] f = new float[0];
                    while (true)
                    {
                        int idx, flags; long ts; IMFSampleB s;
                        Ok(reader.ReadSample(FirstAudio, 0, out idx, out flags, out ts, out s), "ReadSample");
                        if (s != null)
                        {
                            IMFMediaBufferB mb;
                            Ok(s.ConvertToContiguousBuffer(out mb), "ConvertToContiguousBuffer");
                            IntPtr p; int max, len;
                            Ok(mb.Lock(out p, out max, out len), "Lock");
                            if (f.Length < len / 4) f = new float[len / 4];
                            Marshal.Copy(p, f, 0, len / 4);
                            mb.Unlock();
                            for (int i = 0; i + ch - 1 < len / 4; i += ch)
                            {
                                double m = 0;
                                for (int c = 0; c < ch; c++) m += f[i + c];
                                m /= ch;
                                o.Add((short)Math.Max(-32767, Math.Min(32767, m * 32767)));
                            }
                            Marshal.ReleaseComObject(mb); Marshal.ReleaseComObject(s);
                        }
                        if ((flags & EndOfStream) != 0) break;
                    }
                    return o.ToArray();
                }
                finally { Marshal.ReleaseComObject(reader); }
            }
            finally { MFShutdown(); }
        }
    }
}

namespace ClassicGames
{
    /// <summary>Where the recorded songs come from and how they are decoded (one per platform).</summary>
    public interface IMusicSource
    {
        /// <summary>music.txt's text (null: no songs).</summary>
        string ReadIndex();
        bool Exists(string file);
        /// <summary>The whole song as 16-bit mono at its own rate. Throws when it can't be decoded.</summary>
        short[] DecodeMono16(string file, out int rate);
    }

    /// <summary>Songs in a folder, decoded by Windows' Media Foundation (App Hub's way).</summary>
    public sealed class FolderMusic : IMusicSource
    {
        readonly string folder;
        public FolderMusic(string folder) { this.folder = folder; }
        public string ReadIndex() { string f = Path.Combine(folder, "music.txt"); return File.Exists(f) ? File.ReadAllText(f) : null; }
        public bool Exists(string file) { return File.Exists(Path.Combine(folder, file)); }
        public short[] DecodeMono16(string file, out int rate) { return MfDecoder.DecodeMono16(Path.Combine(folder, file), out rate); }
    }
}
