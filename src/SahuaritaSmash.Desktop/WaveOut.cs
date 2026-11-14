// The sound output on Windows: winmm waveOut (part of Windows), as App Hub's Classic Games uses it (Engine\Audio.cs
// there): a few short buffers queued, refilled as the device hands each one back, so sounds start within about 50 ms.
using System;
using System.Runtime.InteropServices;
using System.Threading;
using ClassicGames;

namespace SahuaritaSmash.Desktop;

sealed class WaveOut : IAudioOut
{
    [StructLayout(LayoutKind.Sequential)]
    struct WaveFormat { public short Tag, Channels; public int SamplesPerSec, AvgBytesPerSec; public short BlockAlign, BitsPerSample, Size; }
    [StructLayout(LayoutKind.Sequential)]
    struct WaveHdr { public IntPtr Data; public int BufferLength, BytesRecorded; public IntPtr User; public int Flags, Loops; public IntPtr Next, Reserved; }
    [DllImport("winmm.dll")] static extern int waveOutOpen(out IntPtr h, int device, ref WaveFormat fmt, IntPtr callback, IntPtr instance, int flags);
    [DllImport("winmm.dll")] static extern int waveOutPrepareHeader(IntPtr h, IntPtr hdr, int size);
    [DllImport("winmm.dll")] static extern int waveOutUnprepareHeader(IntPtr h, IntPtr hdr, int size);
    [DllImport("winmm.dll")] static extern int waveOutWrite(IntPtr h, IntPtr hdr, int size);
    [DllImport("winmm.dll")] static extern int waveOutReset(IntPtr h);
    [DllImport("winmm.dll")] static extern int waveOutClose(IntPtr h);

    const int BufCount = 5;     // with 10 ms blocks: about 50 ms queued
    const int WhdrDone = 1;
    IntPtr wave;
    IntPtr[] hdrs, datas;
    bool[] used;
    AutoResetEvent signal;
    int hdrSize, flagsOffset, blockBytes, next;
    volatile bool open;

    public string Error { get; private set; }

    public bool Open(int rate, int blockSamples)
    {
        try
        {
            var fmt = new WaveFormat { Tag = 1, Channels = 1, SamplesPerSec = rate, AvgBytesPerSec = rate * 2, BlockAlign = 2, BitsPerSample = 16 };
            signal = new AutoResetEvent(false);
            int r = waveOutOpen(out wave, -1, ref fmt, signal.SafeWaitHandle.DangerousGetHandle(), IntPtr.Zero, 0x00050000);   // CALLBACK_EVENT
            if (r != 0) { Error = "waveOutOpen " + r; return false; }
            hdrSize = Marshal.SizeOf<WaveHdr>();
            flagsOffset = (int)Marshal.OffsetOf<WaveHdr>("Flags");
            blockBytes = blockSamples * 2;
            hdrs = new IntPtr[BufCount]; datas = new IntPtr[BufCount]; used = new bool[BufCount];
            for (int i = 0; i < BufCount; i++)
            {
                datas[i] = Marshal.AllocHGlobal(blockBytes);
                hdrs[i] = Marshal.AllocHGlobal(hdrSize);
                Marshal.StructureToPtr(new WaveHdr { Data = datas[i], BufferLength = blockBytes }, hdrs[i], false);
                waveOutPrepareHeader(wave, hdrs[i], hdrSize);
            }
            open = true;
            return true;
        }
        catch (Exception ex) { Error = ex.Message; return false; }
    }

    /// <summary>Waits until the next buffer is free (never sent, or the device is done with it), fills it and sends it.</summary>
    public void Write(short[] block)
    {
        if (!open) return;
        int i = next;
        while (open && used[i] && (Marshal.ReadInt32(hdrs[i], flagsOffset) & WhdrDone) == 0) signal.WaitOne(20);
        if (!open) return;
        Marshal.Copy(block, 0, datas[i], Math.Min(block.Length, blockBytes / 2));
        waveOutWrite(wave, hdrs[i], hdrSize);
        used[i] = true;
        next = (i + 1) % BufCount;
    }

    public void Close()
    {
        if (!open) return;
        open = false;
        try
        {
            waveOutReset(wave);
            for (int i = 0; i < BufCount; i++) { waveOutUnprepareHeader(wave, hdrs[i], hdrSize); Marshal.FreeHGlobal(hdrs[i]); Marshal.FreeHGlobal(datas[i]); }
            waveOutClose(wave);
        }
        catch (Exception) { }
    }
}
