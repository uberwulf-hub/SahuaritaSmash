// Checks shared with App Hub's other games' sound tests (HiveSoundTests.cs there): counting clicks in a sound.
using System;

namespace ClassicGames
{
    public static partial class Tests
    {
        /// <summary>Clicks: a sudden kink in the wave (its second difference) bigger than limit that stands out 8 times
        /// from the wave's kinks in the 5 ms around it, or any jump between samples above 0.12. A smooth low buzz or
        /// noise doesn't count; a note or a sound starting or stopping on a hard edge does.</summary>
        public static int CountClicks(float[] a, double limit, out double worst)
        {
            worst = 0;
            int n = a.Length, clicks = 0, w = 220;
            if (n < 4) return 0;
            var sq = new double[n + 1];
            for (int i = 1; i < n - 1; i++) { double c = a[i + 1] - 2.0 * a[i] + a[i - 1]; sq[i + 1] = sq[i] + c * c; }
            for (int i = 1; i < n - 1; i++)
            {
                double jump = Math.Abs(a[i] - a[i - 1]);
                if (jump > 0.12) { clicks++; worst = Math.Max(worst, jump); continue; }
                double c = Math.Abs(a[i + 1] - 2.0 * a[i] + a[i - 1]);
                if (c <= limit) continue;
                int lo = Math.Max(1, i - w), hi = Math.Min(n - 2, i + w);
                double sum = sq[hi + 1] - sq[lo] - c * c;
                double rms = Math.Sqrt(Math.Max(0, sum) / Math.Max(1, hi - lo));
                if (c > 8 * rms) { clicks++; worst = Math.Max(worst, c); }
            }
            return clicks;
        }
    }
}
