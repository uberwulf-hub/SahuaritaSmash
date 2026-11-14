// Sound on Android: the mixer's blocks go to an AudioTrack (streaming, 16-bit mono; Write waits while its buffer is full,
// which paces the mixer's thread), and the recorded songs come from the app's assets (Music/Brawl), decoded by the
// phone's own MediaCodec into 16-bit mono.
using System;
using System.Collections.Generic;
using System.IO;
using Android.Content.Res;
using Android.Media;
using ClassicGames;

namespace SahuaritaSmash.Android;

sealed class TrackOut : IAudioOut
{
    AudioTrack track;
    public string Error { get; private set; }

    public bool Open(int rate, int blockSamples)
    {
        try
        {
            int min = AudioTrack.GetMinBufferSize(rate, ChannelOut.Mono, Encoding.Pcm16bit);
            int size = Math.Max(min, blockSamples * 2 * 4);   // about 40 ms, or what the phone needs at least
            track = new AudioTrack.Builder()
                .SetAudioAttributes(new AudioAttributes.Builder().SetUsage(AudioUsageKind.Game).SetContentType(AudioContentType.Music).Build())
                .SetAudioFormat(new AudioFormat.Builder().SetEncoding(Encoding.Pcm16bit).SetSampleRate(rate).SetChannelMask(ChannelOut.Mono).Build())
                .SetBufferSizeInBytes(size)
                .SetTransferMode(AudioTrackMode.Stream)
                .Build();
            track.Play();
            return true;
        }
        catch (Exception ex) { Error = ex.Message; return false; }
    }

    public void Write(short[] block) { var t = track; if (t != null) t.Write(block, 0, block.Length); }

    public void Close()
    {
        var t = track; track = null;
        if (t == null) return;
        try { t.Stop(); } catch (Exception) { }
        t.Release();
    }
}

sealed class AssetMusic : IMusicSource
{
    const int InfoOutputFormatChanged = -2;   // MediaCodec.INFO_OUTPUT_FORMAT_CHANGED
    const string Dir = "Music/Brawl";
    readonly AssetManager assets;
    readonly HashSet<string> files;

    public AssetMusic(AssetManager assets)
    {
        this.assets = assets;
        files = new HashSet<string>(assets.List(Dir) ?? new string[0], StringComparer.OrdinalIgnoreCase);
    }

    public string ReadIndex()
    {
        if (!files.Contains("music.txt")) return null;
        using (var r = new StreamReader(assets.Open(Dir + "/music.txt"))) return r.ReadToEnd();
    }

    public bool Exists(string file) { return files.Contains(file); }

    public short[] DecodeMono16(string file, out int rate)
    {
        using (AssetFileDescriptor fd = assets.OpenFd(Dir + "/" + file))
        using (var ex = new MediaExtractor())
        {
            ex.SetDataSource(fd.FileDescriptor, fd.StartOffset, fd.Length);
            int trackNo = -1; MediaFormat fmt = null;
            for (int i = 0; i < ex.TrackCount; i++)
            {
                MediaFormat f = ex.GetTrackFormat(i);
                if ((f.GetString(MediaFormat.KeyMime) ?? "").StartsWith("audio/")) { trackNo = i; fmt = f; break; }
            }
            if (trackNo < 0) throw new InvalidDataException(file + " has no sound in it");
            ex.SelectTrack(trackNo);
            rate = fmt.GetInteger(MediaFormat.KeySampleRate);
            int channels = fmt.GetInteger(MediaFormat.KeyChannelCount);
            var codec = MediaCodec.CreateDecoderByType(fmt.GetString(MediaFormat.KeyMime));
            var outSamples = new List<short>(rate * 60 * 4);
            try
            {
                codec.Configure(fmt, null, null, MediaCodecConfigFlags.None);
                codec.Start();
                var info = new MediaCodec.BufferInfo();
                bool inputDone = false, outputDone = false;
                while (!outputDone)
                {
                    if (!inputDone)
                    {
                        int inIx = codec.DequeueInputBuffer(10000);
                        if (inIx >= 0)
                        {
                            var buf = codec.GetInputBuffer(inIx);
                            int n = ex.ReadSampleData(buf, 0);
                            if (n < 0) { codec.QueueInputBuffer(inIx, 0, 0, 0, MediaCodecBufferFlags.EndOfStream); inputDone = true; }
                            else { codec.QueueInputBuffer(inIx, 0, n, ex.SampleTime, MediaCodecBufferFlags.None); ex.Advance(); }
                        }
                    }
                    int outIx = codec.DequeueOutputBuffer(info, 10000);
                    if (outIx == InfoOutputFormatChanged)
                    {
                        MediaFormat of = codec.OutputFormat;
                        rate = of.GetInteger(MediaFormat.KeySampleRate);
                        channels = of.GetInteger(MediaFormat.KeyChannelCount);
                    }
                    else if (outIx >= 0)
                    {
                        if (info.Size > 0)
                        {
                            var ob = codec.GetOutputBuffer(outIx);
                            ob.Position(info.Offset); ob.Limit(info.Offset + info.Size);
                            var bytes = new byte[info.Size];
                            ob.Get(bytes);
                            int ch = Math.Max(1, channels);
                            for (int i = 0; i + 2 * ch <= bytes.Length; i += 2 * ch)
                            {
                                int sum = 0;
                                for (int c = 0; c < ch; c++) sum += (short)(bytes[i + 2 * c] | (bytes[i + 2 * c + 1] << 8));
                                outSamples.Add((short)(sum / ch));
                            }
                        }
                        codec.ReleaseOutputBuffer(outIx, false);
                        if ((info.Flags & MediaCodecBufferFlags.EndOfStream) != 0) outputDone = true;
                    }
                }
            }
            finally { try { codec.Stop(); } catch (Exception) { } codec.Release(); }
            return outSamples.ToArray();
        }
    }
}
