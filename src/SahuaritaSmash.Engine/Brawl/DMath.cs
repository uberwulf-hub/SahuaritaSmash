// Sine and cosine that give exactly the same result on every computer and phone (network play runs the match on each
// device from the same buttons, so every number must agree to the last bit). Math.Sin / Math.Cos come from each
// platform's C library and may differ in the last bit between Windows and Android; these use only +, -, * and Round,
// which IEEE 754 makes exact everywhere. Accurate to about 1e-15 for the angles a match uses.
using System;

namespace ClassicGames
{
    public static class DMath
    {
        // pi / 2 split in two (Cody-Waite), so k * pi/2 is taken off without losing bits
        const double PioHi = 1.5707963267341256, PioLo = 6.077100506506192e-11, InvPio = 0.6366197723675814;

        public static double Sin(double x) { return SinCos(x, 0); }
        public static double Cos(double x) { return SinCos(x, 1); }

        static double SinCos(double x, int shift)
        {
            if (double.IsNaN(x) || double.IsInfinity(x)) return double.NaN;
            double k = Math.Round(x * InvPio);
            double r = (x - k * PioHi) - k * PioLo;          // |r| <= pi/4
            int q = (int)(((long)k + shift) & 3);
            double r2 = r * r;
            switch (q)
            {
                case 0: return SinPoly(r, r2);
                case 1: return CosPoly(r2);
                case 2: return -SinPoly(r, r2);
                default: return -CosPoly(r2);
            }
        }

        // Taylor series to r^17 / r^18: on |r| <= pi/4 the next term is below 1e-17
        static double SinPoly(double r, double r2)
        {
            return r * (1 + r2 * (-1.0 / 6 + r2 * (1.0 / 120 + r2 * (-1.0 / 5040 + r2 * (1.0 / 362880 + r2 * (-1.0 / 39916800
                + r2 * (1.0 / 6227020800 + r2 * (-1.0 / 1307674368000 + r2 * (1.0 / 355687428096000)))))))));
        }
        static double CosPoly(double r2)
        {
            return 1 + r2 * (-0.5 + r2 * (1.0 / 24 + r2 * (-1.0 / 720 + r2 * (1.0 / 40320 + r2 * (-1.0 / 3628800
                + r2 * (1.0 / 479001600 + r2 * (-1.0 / 87178291200 + r2 * (1.0 / 20922789888000))))))));
        }
    }
}
